use strict;
use warnings;
use JSON;

my $IS_WINDOWS = ($^O =~ /MSWin32/i) ? 1 : 0;

sub command_exists {
    my ($cmd) = @_;
    my $check_cmd = $IS_WINDOWS ? "where $cmd 2>NUL" : "which $cmd 2>/dev/null";
    my $out = `$check_cmd`;
    return (defined $out && length($out) > 0) ? 1 : 0;
}

sub clean_value {
    my ($v) = @_;
    return '' unless defined $v;
    $v = "$v";
    $v =~ s/\x00//g;
    $v =~ s/^\x{FEFF}//;
    $v =~ s/[\x00-\x1F\x7F]//g;
    $v =~ s/^\s+|\s+$//g;
    return $v;
}

sub flatten_data {
    my ($item) = @_;
    return {} unless ref($item) eq 'HASH';
    
    my %out;
    while (my ($k, $v) = each %$item) {
        if (ref($v) eq 'HASH' || ref($v) eq 'ARRAY') {
            $out{$k} = encode_json($v);
        } else {
            $out{$k} = clean_value($v);
        }
    }
    return \%out;
}

sub run_powershell {
    my ($query) = @_;
    my $cmd = sprintf(
        'powershell -NoProfile -ExecutionPolicy Bypass -Command "[Console]::OutputEncoding = [Text.Encoding]::UTF8; $data = @(%s); if ($data.Count -gt 0) { $data | ConvertTo-Json -Depth 3 -Compress } else { \'[]\' }"',
        $query
    );

    my $raw = `$cmd 2>NUL`;
    return [] unless defined $raw;

    $raw =~ s/^\s+|\s+$//g;
    $raw =~ s/^\x{FEFF}//;
    return [] if $raw eq '' || $raw eq 'null';

    if ($raw =~ /^\{.*\}$/s) {
        $raw = '[' . $raw . ']';
    }

    my $data;
    eval {
        $data = decode_json($raw);
    };
    return [] if $@ || ref($data) ne 'ARRAY';

    return $data;
}

sub parse_wmic {
    my ($wmic_cmd) = @_;
    my $cmd = "wmic $wmic_cmd get /format:list 2>NUL";
    my @raw_lines = `$cmd`;
    return [] unless @raw_lines;

    my @rows;
    my %current;

    for my $line (@raw_lines) {
        $line =~ s/[\r\n]//g;
        $line =~ s/^\xEF\xBB\xBF//;
        $line =~ s/^\s+|\s+$//g;

        if ($line eq '') {
            if (%current) {
                push @rows, { %current };
                %current = ();
            }
            next;
        }

        if ($line =~ /^([^=]+)=(.*)$/) {
            my $k = clean_value($1);
            my $v = clean_value($2);
            $current{$k} = $v if $k ne '';
        }
    }
    push @rows, { %current } if %current;

    return \@rows;
}

sub get_windows_data {
    my ($ps_query, $wmic_cmd) = @_;

    if (command_exists('powershell')) {
        my $data = run_powershell($ps_query);
        if (@$data) {
            my @flattened = map { flatten_data($_) } @$data;
            return \@flattened;
        }
    }

    if (command_exists('wmic')) {
        return parse_wmic($wmic_cmd);
    }

    return [];
}

sub get_unix_applications {
    my @apps;

    if (command_exists('dpkg-query')) {
        my @lines = `dpkg-query -W -f='\${Package}\t\${Version}\t\${Maintainer}\n' 2>/dev/null`;
        for my $line (@lines) {
            chomp $line;
            my @parts = split(/\t/, $line);
            if (@parts >= 2) {
                push @apps, {
                    'name'    => clean_value($parts[0]),
                    'version' => clean_value($parts[1]),
                    'vendor'  => clean_value($parts[2] // ''),
                    'source'  => 'dpkg'
                };
            }
        }
    }
    elsif (command_exists('rpm')) {
        my @lines = `rpm -qa --qf '%{NAME}\t%{VERSION}-%{RELEASE}\t%{VENDOR}\n' 2>/dev/null`;
        for my $line (@lines) {
            chomp $line;
            my @parts = split(/\t/, $line);
            if (@parts >= 2) {
                push @apps, {
                    'name'    => clean_value($parts[0]),
                    'version' => clean_value($parts[1]),
                    'vendor'  => clean_value($parts[2] // ''),
                    'source'  => 'rpm'
                };
            }
        }
    }

    if (command_exists('snap')) {
        my @lines = `snap list 2>/dev/null`;
        shift @lines;
        for my $line (@lines) {
            chomp $line;
            my @cols = split(/\s+/, clean_value($line));
            if (@cols >= 2) {
                push @apps, {
                    'name'    => clean_value($cols[0]),
                    'version' => clean_value($cols[1]),
                    'vendor'  => clean_value($cols[4] // ''),
                    'source'  => 'snap'
                };
            }
        }
    }

    return \@apps;
}

sub get_unix_services {
    my @services;

    if (command_exists('systemctl')) {
        my @lines = `systemctl list-units --type=service --all --no-pager --no-legend 2>/dev/null`;
        for my $line (@lines) {
            chomp $line;
            my @cols = split(/\s+/, clean_value($line), 5);
            if (@cols >= 4) {
                my $s_name = $cols[0];
                $s_name =~ s/\.service$//;
                push @services, {
                    'name'         => clean_value($s_name),
                    'display_name' => clean_value($cols[4] // $s_name),
                    'status'       => ($cols[2] eq 'active') ? 'running' : 'stopped',
                    'source'       => 'systemd'
                };
            }
        }
    }
    elsif (command_exists('service')) {
        my @lines = `service --status-all 2>/dev/null`;
        for my $line (@lines) {
            if ($line =~ /\[\s*([\+\-\?])\s*\]\s+(.+)/) {
                my $flag = $1;
                my $s_name = clean_value($2);
                push @services, {
                    'name'         => $s_name,
                    'display_name' => $s_name,
                    'status'       => ($flag eq '+') ? 'running' : 'stopped',
                    'source'       => 'sysvinit'
                };
            }
        }
    }
    elsif (command_exists('launchctl')) {
        my @lines = `launchctl list 2>/dev/null`;
        shift @lines;
        for my $line (@lines) {
            chomp $line;
            my @cols = split(/\s+/, clean_value($line), 3);
            if (@cols >= 3) {
                my $pid = $cols[0];
                my $label = $cols[2];
                push @services, {
                    'name'         => clean_value($label),
                    'display_name' => clean_value($label),
                    'status'       => ($pid ne '-' && $pid =~ /^\d+$/) ? 'running' : 'stopped',
                    'source'       => 'launchd'
                };
            }
        }
    }

    return \@services;
}

sub main {
    print "Content-Type: application/json; charset=utf-8\n\n";

    my %result = (
        'success'     => \0,
        'system_type' => $IS_WINDOWS ? 'windows' : 'unix_like',
        'os_raw'      => $^O,
        'error'       => '',
        'data'        => {}
    );

    eval {
        if ($IS_WINDOWS) {
            my $ps_apps = "Get-ChildItem 'HKLM:\\Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\', 'HKLM:\\Software\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\' -ErrorAction SilentlyContinue | ForEach-Object { try { Get-ItemProperty \$_.PSPath -ErrorAction Stop } catch {} } | Where-Object DisplayName | Select-Object @{N='name';E={\$_.DisplayName}}, @{N='version';E={\$_.DisplayVersion}}, @{N='vendor';E={\$_.Publisher}}, @{N='installed';E={\$_.InstallDate}}";
            my $ps_serv = "Get-Service | ForEach-Object { @{ name = \$_.Name; display_name = \$_.DisplayName; status = if (\$_.Status -eq 'Running') { 'running' } else { 'stopped' }; start_type = \$_.StartType.ToString() } }";

            $result{'data'} = {
                'applications'  => get_windows_data($ps_apps, "product"),
                'services'      => get_windows_data($ps_serv, "service"),
                'user_accounts' => get_windows_data("Get-CimInstance Win32_UserAccount", "useraccount"),
                'user_profiles' => get_windows_data("Get-CimInstance Win32_UserProfile", "path Win32_UserProfile"),
                'groups'        => get_windows_data("Get-CimInstance Win32_Group", "group")
            };
        } else {
            $result{'data'} = {
                'applications'  => get_unix_applications(),
                'services'      => get_unix_services(),
                'user_accounts' => [],
                'user_profiles' => [],
                'groups'        => []
            };
        }

        $result{'success'} = \1;
        1;
    } or do {
        my $err = $@ || 'Unknown Error';
        $result{'error'} = clean_value($err);
    };

    my $json_builder = JSON->new->utf8->pretty->canonical;
    print $json_builder->encode(\%result);
}

main();