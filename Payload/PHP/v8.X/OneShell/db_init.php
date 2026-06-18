<?php

@ini_set('display_errors','0');
@set_time_limit(0);

$checks = [
    'MySQLi' => extension_loaded('mysqli'),
    'MySQL' => extension_loaded('mysql'),
    'PDO' => class_exists('PDO'),
    'PDO MySQL' => extension_loaded('pdo_mysql'),
    'PDO PostgreSQL' => extension_loaded('pdo_pgsql'),
    'PDO SQLite' => extension_loaded('pdo_sqlite'),
    'PostgreSQL' => extension_loaded('pgsql'),
    'SQLite3' => extension_loaded('sqlite3'),
    'Redis' => extension_loaded('redis'),
    'MongoDB' => extension_loaded('mongodb'),
    'Oracle (OCI8)' => extension_loaded('oci8'),
    'Microsoft SQL Server' => extension_loaded('sqlsrv'),
    'ODBC' => extension_loaded('pdo_odbc'),
];

foreach ($checks as $db => $available) {
    echo "{$db}:" . (int)$available . ",";
}

?>