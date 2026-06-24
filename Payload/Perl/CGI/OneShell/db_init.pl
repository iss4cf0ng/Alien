use strict;
use warnings;

my %checks = (
    'MySQLi (DBI::mysql)' => 'DBD::mysql',
    'PDO MySQL (DBI::mysql)' => 'DBD::mysql',
    'PDO PostgreSQL (DBI::Pg)' => 'DBD::Pg',
    'PDO SQLite (DBI::SQLite)' => 'DBD::SQLite',
    'PostgreSQL' => 'DBD::Pg',
    'SQLite3' => 'DBD::SQLite',
    'Redis' => 'Redis',
    'MongoDB' => 'MongoDB',
    'Oracle (OCI8)' => 'DBD::Oracle',
    'Microsoft SQL Server' => 'DBD::ODBC',
    'ODBC' => 'DBD::ODBC',
);

for my $db (keys %checks) {
    my $module = $checks{$db};

    my $available = eval "require $module; 1" ? 1 : 0;

    print "$db:$available,";
}