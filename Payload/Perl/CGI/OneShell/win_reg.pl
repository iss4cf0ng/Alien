use strict;
use warnings;
use CGI;
use MIME::Base64;
use JSON;
use File::Temp qw(tempfile);
use Encode qw(encode);

sub run_reg {
    my ($cmd) = @_;
    my @output = `$cmd 2>&1`;
    my $ret = $? >> 8;
    return ($ret, \@output);
}

sub validate_path {
    my ($path) = @_;
    return $path =~ /^HKEY_(LOCAL_MACHINE|CURRENT_USER|USERS|CLASSES_ROOT|CURRENT_CONFIG)\\[A-Za-z0-9_\\-]+$/ ? 1 : 0;
}

sub validate_value_name {
    my ($name) = @_;
    return $name =~ /^[A-Za-z0-9 _\\-]+$/ ? 1 : 0;
}

sub win_escape {
    my ($arg) = @_;
    $arg =~ s/"/""/g;
    return qq("$arg");
}

sub registry_value_to_bytes {
    my ($value, $type) = @_;
    
    if ($type eq 'REG_DWORD') {
        $value =~ s/^0x//i;
        my $num = hex($value);
        return pack("V", $num); # 32-bit little endian
    }
    elsif ($type eq 'REG_QWORD') {
        $value =~ s/^0x//i;
        my $num = hex($value);
        return pack("Q<", $num); 
    }
    elsif ($type eq 'REG_BINARY') {
        $value =~ s/[^A-Fa-f0-9]//g;
        return pack("H*", $value);
    }
    else {
        my $raw_bytes = $value;
        utf8::encode($raw_bytes) if utf8::is_utf8($raw_bytes);
        return $raw_bytes;
    }
}

sub scan_hives {
    my ($hives_ref) = @_;
    my %result;
    for my $hive (@$hives_ref) {
        my ($ret, $out) = run_reg("reg query " . win_escape($hive));
        $result{$hive} = ($ret == 0) ? JSON::true : JSON::false;
    }
    return \%result;
}

sub scan_registry {
    my ($base_path) = @_;
    my ($ret, $output) = run_reg("reg query " . win_escape($base_path));

    my %result = (
        success => ($ret == 0) ? JSON::true : JSON::false,
        error   => undef,
        subkeys => [],
        values  => []
    );

    if ($ret != 0) {
        $result{error} = join("", @$output);
        return \%result;
    }

    my $first_key_seen = 0;
    for my $line (@$output) {
        $line =~ s/\r?\n$//; # rtrim
        next if $line eq '';

        if ($line =~ /^HKEY_/) {
            if (!$first_key_seen) {
                $first_key_seen = 1;
            } else {
                push @{$result{subkeys}}, $line;
            }
            next;
        }

        if ($line =~ /^\s*(.*?)\s+(REG_\w+)\s+(.*)$/) {
            my $name = $1;
            my $type = $2;
            my $val  = $3;
            $name =~ s/^\s+|\s+$//g;

            push @{$result{values}}, {
                name => $name,
                type => $type,
                data => encode_base64(registry_value_to_bytes($val, $type), '')
            };
        }
    }
    return \%result;
}

sub set_value {
    my ($path, $name, $type, $data) = @_;
    
    my %allowed = map { $_ => 1 } qw(REG_SZ REG_EXPAND_SZ REG_DWORD REG_QWORD REG_BINARY REG_MULTI_SZ);
    return { success => JSON::false, error => 'Invalid type' } unless $allowed{$type};
    return { success => JSON::false, error => 'Invalid path or name' } unless (validate_path($path) && validate_value_name($name));

    if ($type eq 'REG_DWORD' || $type eq 'REG_QWORD') {
        $data = ($data =~ /^\d+$/) ? $data : hex($data);
    }
    elsif ($type eq 'REG_BINARY') {
        $data = unpack("H*", decode_base64($data));
    }
    elsif ($type eq 'REG_MULTI_SZ') {
        $data =~ s/,/\\0/g;
    }

    my $cmd = sprintf('reg add %s /v %s /t %s /d %s /f', win_escape($path), win_escape($name), win_escape($type), win_escape($data));
    my ($ret, $out) = run_reg($cmd);
    my $ok = (join("", @$out) =~ /ERROR/i) ? 0 : 1;

    return { success => $ok ? JSON::true : JSON::false, output => $out };
}

sub delete_key {
    my ($path) = @_;
    return { success => JSON::false, error => 'Invalid path' } unless validate_path($path);
    my ($ret, $out) = run_reg("reg delete " . win_escape($path) . " /f");
    return { success => ($ret == 0) ? JSON::true : JSON::false, output => $out };
}

sub delete_value {
    my ($path, $name) = @_;
    return { success => JSON::false, error => 'Invalid input' } unless (validate_path($path) && validate_value_name($name));
    my ($ret, $out) = run_reg("reg delete " . win_escape($path) . " /v " . win_escape($name) . " /f");
    return { success => JSON::true, output => $out };
}

sub rename_value {
    my ($path, $old_name, $new_name) = @_;
    return { success => JSON::false, error => 'Invalid input' } unless (validate_path($path) && validate_value_name($old_name) && validate_value_name($new_name));

    my $scan = scan_registry($path);
    my $value_data;
    for my $v (@{$scan->{values}}) {
        if ($v->{name} eq $old_name) { $value_data = $v; last; }
    }
    return { success => JSON::false, error => 'Value not found' } unless $value_data;

    my $decoded = decode_base64($value_data->{data});
    my $set = set_value($path, $new_name, $value_data->{type}, $decoded);
    return $set unless $set->{success};

    return delete_value($path, $old_name);
}

sub rename_key {
    my ($old_path, $new_path) = @_;
    return { success => JSON::false, error => 'Invalid source path' } unless validate_path($old_path);

    my ($ret, $out) = run_reg("reg copy " . win_escape($old_path) . " " . win_escape($new_path) . " /s /f");
    if (join("", @$out) =~ /ERROR/i) {
        return { success => JSON::false, output => $out };
    }
    my ($ret2, $out2) = run_reg("reg delete " . win_escape($old_path) . " /f");
    push @$out, @$out2;
    return { success => JSON::true, output => $out };
}

sub export_key {
    my ($path) = @_;
    return { success => JSON::false, error => 'Invalid path' } unless validate_path($path);

    my ($fh, $tmp) = tempfile(TEMPLATE => 'reg_XXXX', SUFFIX => '.reg', TMPDIR => 1);
    close($fh);

    my ($ret, $out) = run_reg("reg export " . win_escape($path) . " " . win_escape($tmp) . " /y");
    if ($ret != 0 || !-e $tmp) {
        return { success => JSON::false, output => $out };
    }

    open(my $rfh, '<', $tmp);
    binmode($rfh);
    local $/;
    my $content = <$rfh>;
    close($rfh);
    unlink($tmp);

    return { success => JSON::true, data => encode_base64($content, '') };
}

sub import_file {
    my ($content) = @_;
    my ($fh, $tmp) = tempfile(TEMPLATE => 'reg_XXXX', SUFFIX => '.reg', TMPDIR => 1);
    binmode($fh);
    print $fh $content;
    close($fh);

    my ($ret, $out) = run_reg("reg import " . win_escape($tmp));
    unlink($tmp);

    return { success => ($ret == 0) ? JSON::true : JSON::false, output => $out };
}

sub main {
    my $q = CGI->new;
    my $json = JSON->new->utf8->canonical(1);

    my $action = decode_base64($q->param('z0') // '');
    $action =~ s/^\s+|\s+$//g;

    my @hives = ('HKEY_CLASSES_ROOT','HKEY_CURRENT_USER','HKEY_LOCAL_MACHINE','HKEY_USERS','HKEY_CURRENT_CONFIG');

    if ($action eq 'hive') {
        print $json->encode(scan_hives(\@hives));
    }
    elsif ($action eq 'scan') {
        my $base_path = decode_base64($q->param('z2') // '');
        print $json->encode(scan_registry($base_path));
    }
    elsif ($action eq 'set') {
        print $json->encode(set_value(decode_base64($q->param('z2')), decode_base64($q->param('z3')), decode_base64($q->param('z4')), decode_base64($q->param('z5'))));
    }
    elsif ($action eq 'del_key') {
        print $json->encode(delete_key(decode_base64($q->param('z2'))));
    }
    elsif ($action eq 'del_value') {
        print $json->encode(delete_value(decode_base64($q->param('z2')), decode_base64($q->param('z3'))));
    }
    elsif ($action eq 'rename_key') {
        print $json->encode(rename_key(decode_base64($q->param('z2')), decode_base64($q->param('z3'))));
    }
    elsif ($action eq 'rename_value') {
        print $json->encode(rename_value(decode_base64($q->param('z2')), decode_base64($q->param('z3')), decode_base64($q->param('z4'))));
    }
    elsif ($action eq 'new_key') {
        print $json->encode({ success => delete_key(decode_base64($q->param('z2')))->{success} });
    }
    elsif ($action eq 'export') {
        print $json->encode(export_key(decode_base64($q->param('z2'))));
    }
    elsif ($action eq 'import') {
        print $json->encode(import_file(decode_base64($q->param('z2'))));
    }
    else {
        print $json->encode({ success => JSON::false, error => 'Unknown action', subkeys => [], values => [] });
    }
}

main();