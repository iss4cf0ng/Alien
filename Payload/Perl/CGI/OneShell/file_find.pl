use strict;
use warnings;
use CGI;
use MIME::Base64;
use JSON;
use File::Find;
use File::Spec;

sub to_regex {
    my ($string) = @_;
    $string =~ s/^\s+|\s+$//g; # trim

    if ($string =~ m/^([\/#~]).*\1[a-imsuxADSUX]*$/) {
        my $delimiter = $1;
        
        my $pattern = $string;
        $pattern =~ s/^\\$delimiter//;

        my $has_i = ($string =~ m/$delimiter[a-imsuxADSUX]*i[a-imsuxADSUX]*$/) ? 1 : 0;
        
        eval { qr/$pattern/; 1 } or return qr/.*/;
        return $has_i ? qr/$pattern/i : qr/$pattern/;
    }

    if ($string =~ m/\*/ || $string =~ m/\?/) {
        my $escaped = quotemeta($string);
        $escaped =~ s/\\\*/.*/g;
        $escaped =~ s/\\\?/./g;
        return qr/^$escaped$/i;
    }

    if ($string =~ m/[\.\\\\\+\*\?\^\$\[\]\(\)\{\}<>=\!\|:\-]/) {
        my $res = eval { qr/$string/; 1 };
        return qr/$string/ if $res;
    }

    my $escaped = quotemeta($string);
    return qr/$escaped/i;
}

sub fn_get_file_permission {
    my ($file) = @_;
    my @stat_info = lstat($file);
    return 'u---------' unless @stat_info;
    my $perms = $stat_info[2];

    my $info = 'u';
    my $type = $perms & 0xF000;

    if    ($type == 0xC000) { $info = 's'; } # socket
    elsif ($type == 0xA000) { $info = 'l'; } # symbolic link
    elsif ($type == 0x8000) { $info = 'r'; } # regular file
    elsif ($type == 0x6000) { $info = 'b'; } # block special
    elsif ($type == 0x4000) { $info = 'd'; } # directory
    elsif ($type == 0x2000) { $info = 'c'; } # character special
    elsif ($type == 0x1000) { $info = 'p'; } # FIFO pipe

    # Owner
    $info .= ($perms & 0x0100) ? 'r' : '-';
    $info .= ($perms & 0x0080) ? 'w' : '-';
    $info .= ($perms & 0x0040) ? (($perms & 0x0800) ? 's' : 'x') : (($perms & 0x0800) ? 'S' : '-');

    # Group
    $info .= ($perms & 0x0020) ? 'r' : '-';
    $info .= ($perms & 0x0010) ? 'w' : '-';
    $info .= ($perms & 0x0008) ? (($perms & 0x0400) ? 's' : 'x') : (($perms & 0x0400) ? 'S' : '-');

    # World
    $info .= ($perms & 0x0004) ? 'r' : '-';
    $info .= ($perms & 0x0002) ? 'w' : '-';
    $info .= ($perms & 0x0001) ? (($perms & 0x0200) ? 't' : 'x') : (($perms & 0x0200) ? 'T' : '-');

    return $info;
}

sub fn_datetime_conversion {
    my ($timestamp) = @_;
    my ($sec,$min,$hour,$mday,$mon,$year,$wday,$yday,$isdst) = localtime($timestamp);
    return sprintf("%04d-%02d-%02d %02d:%02d:%02d", $year + 1900, $mon + 1, $mday, $hour, $min, $sec);
}

sub main {
    my $q = CGI->new;
    my $regex_str = decode_base64($q->param('z0') // '');
    my $dirs_str  = decode_base64($q->param('z1') // '');

    my $regex = to_regex($regex_str);
    my $json  = JSON->new->utf8;

    my @dirs = split(/,/, $dirs_str);
    my @target_dirs;

    foreach my $dir (@dirs) {
        $dir =~ s/^\s+|\s+$//g; # trim
        if (-d $dir) {
            push @target_dirs, $dir;
        }
    }

    if (!@target_dirs) {
        print $json->encode({
            status => JSON::false,
            msg    => 'Cannot find any valid directory'
        });
        return;
    }

    my @results;

    eval {
        find({
            wanted => sub {
                my $filename = $_;
                
                return if $filename eq '.' || $filename eq '..';

                if ($filename =~ /$regex/) {
                    my $real_path = File::Spec->rel2abs($File::Find::name);
                    my @stat_info = lstat($filename);
                    return unless @stat_info;

                    push @results, {
                        name          => $filename,
                        path          => $real_path,
                        type          => (-d _) ? 'Directory' : 'File',
                        permission    => fn_get_file_permission($filename),
                        created       => fn_datetime_conversion($stat_info[10]), # ctime
                        last_modified => fn_datetime_conversion($stat_info[9]),  # mtime
                        last_accessed => fn_datetime_conversion($stat_info[8])   # atime
                    };
                }
            },
            no_chdir => 0,
        }, @target_dirs);
        1;
    } or do {
        my $error = $@ || 'Unknown scanning error';
        $error =~ s/ at .* line \d+.*//s;
        print $json->encode({
            status => JSON::false,
            msg    => $error
        });
        return;
    };

    print $json->encode({
        status  => JSON::true,
        results => \@results
    });
}

main();