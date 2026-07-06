use strict;
use warnings;
use CGI;
use JSON;
use Encode qw(decode);

# Prevent WMIC timeout
my $CMD_TIMEOUT = 5; 

sub run_cmd_with_timeout {
    my ($cmd) = @_;
    my @output;
    my $code = 0;

    eval {
        local $SIG{ALRM} = sub { die "TIMEOUT\n" };
        alarm($CMD_TIMEOUT);
        
        @output = `$cmd`;
        $code = $? >> 8;
        
        alarm(0);
    };

    if ($@) {
        if ($@ eq "TIMEOUT\n") {
            return (999, ["ERROR: Execution timed out after $CMD_TIMEOUT seconds."]);
        }
        return (999, ["ERROR: $@"]);
    }

    return ($code, \@output);
}

sub has_powershell {
    my ($code, $out) = run_cmd_with_timeout('powershell -NoProfile -Command "Get-Host" 2>NUL');
    return $code == 0 ? 1 : 0;
}

sub clean_value {
    my ($v) = @_;
    return '' unless defined $v;
    $v = "$v";
    $v =~ s/[^\p{L}\p{N}\p{P}\p{S}\s]//g;
    $v =~ s/^\s+|\s+$//g;
    return $v;
}

sub flatten {
    my ($item_ref) = @_;
    my %out;
    my $json_tool = JSON->new->utf8->canonical(1);
    while (my ($k, $v) = each %$item_ref) {
        if (ref($v) eq 'HASH' || ref($v) eq 'ARRAY') {
            $out{$k} = $json_tool->encode($v);
        } else {
            $out{$k} = clean_value($v);
        }
    }
    return \%out;
}

sub clean_line {
    my ($line) = @_;
    return '' unless defined $line;
    $line =~ s/^\xEF\xBB\xBF//;
    $line =~ s/\r?\n$//;
    eval { $line = decode('Active', $line); }; 
    $line =~ s/^\s+|\s+$//g if defined $line;
    return $line;
}

sub run_powershell {
    my ($query) = @_;
    my $cmd = qq(powershell -NoProfile -Command "$query | ConvertTo-Json -Depth 2 -Compress" 2>NUL);
    
    my ($code, $output) = run_cmd_with_timeout($cmd);
    return [] if ($code != 0 || !@$output);

    my $json_str = join("", @$output);
    my $json_tool = JSON->new->utf8;
    
    my $data;
    eval { $data = $json_tool->decode($json_str); 1; } or return [];
    return [] unless defined $data;

    if (ref($data) eq 'ARRAY') { return $data; } 
    elsif (ref($data) eq 'HASH') { return [$data]; }
    return [];
}

sub parse_wmic {
    my ($class) = @_;
    my $cmd = "wmic /node:localhost path $class get /format:list 2>NUL";
    
    my ($code, $output) = run_cmd_with_timeout($cmd);
    return [] if ($code != 0 || !@$output);

    my @rows;
    my %current;

    for my $line (@$output) {
        $line = clean_line($line);

        if ($line eq '') {
            if (%current) {
                my %copy = %current;
                push @rows, \%copy;
            }
            %current = ();
            next;
        }

        next if ($line !~ /=/);
        my ($k, $v) = split(/=/, $line, 2);

        $k = clean_value($k);
        $v = clean_value($v);
        next if $k eq '';

        $current{$k} = $v;
    }

    if (%current) {
        my %copy = %current;
        push @rows, \%copy;
    }

    return \@rows;
}

sub get_data {
    my ($ps_query, $wmic_class) = @_;

    if (has_powershell()) {
        my $data = run_powershell($ps_query);
        if (@$data) {
            my @clean;
            for my $row (@$data) {
                push @clean, flatten($row);
            }
            return \@clean;
        }
    }
    return parse_wmic($wmic_class);
}

sub main {
    my $json_tool = JSON->new->utf8->pretty(1)->canonical(1);

    my %result = (
        success => JSON::false,
        error   => '',
        data    => undef
    );

    eval {
        $result{data} = {
            user_accounts => get_data("Get-CimInstance Win32_UserAccount -Filter 'LocalAccount=True'", "Win32_UserAccount WHERE LocalAccount=True"),
            user_profiles => get_data("Get-CimInstance Win32_UserProfile", "Win32_UserProfile"),
            groups        => get_data("Get-CimInstance Win32_Group -Filter 'LocalAccount=True'", "Win32_Group WHERE LocalAccount=True"),
            group_users   => get_data("Get-CimInstance Win32_GroupUser", "Win32_GroupUser"),
            logged_on     => get_data("Get-CimInstance Win32_LoggedOnUser", "Win32_LoggedOnUser"),
            logon_session => get_data("Get-CimInstance Win32_LogonSession", "Win32_LogonSession")
        };
        $result{success} = JSON::true;
        1;
    } or do {
        $result{error} = $@ || "Unknown execution error";
    };

    print $json_tool->encode(\%result);
}

main();