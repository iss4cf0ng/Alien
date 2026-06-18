<?php

header('Content-Type: application/json');

$connStr = base64_decode($_POST['z0'] ?? '');
$sql = base64_decode($_POST['z1'] ?? '');

function parseConn($str)
{
    $out = [];
    foreach (explode(';', $str) as $p) {
        if (strpos($p, '=') !== false) {
            [$k,$v] = explode('=', $p, 2);
            $out[trim($k)] = trim($v);
        }
    }
    return $out;
}

try {

    $cfg = parseConn($connStr);

    $host = $cfg['Server'] ?? $cfg['Host'] ?? 'localhost';
    $db = $cfg['Database'] ?? '';
    $user = $cfg['User Id'] ?? $cfg['UID'] ?? $cfg['Uid'] ?? '';
    $pass = $cfg['Password'] ?? $cfg['PWD'] ?? $cfg['Pwd'] ?? '';

    $mysqli = new mysqli($host, $user, $pass, $db);

    if ($mysqli->connect_error) {
        throw new Exception($mysqli->connect_error);
    }

    // connection test
    if (!$sql) {
        echo json_encode([
            "success" => true,
            "message" => "MySQL connection OK"
        ]);
        exit;
    }

    $result = $mysqli->query($sql);

    if (!$result) {
        throw new Exception($mysqli->error);
    }

    $rows = [];

    while ($row = $result->fetch_assoc()) {
        $rows[] = $row;
    }

    echo json_encode([
        "success" => true,
        "rowCount" => count($rows),
        "data" => $rows
    ]);

    $mysqli->close();

} catch (Exception $e) {

    echo json_encode([
        "success" => false,
        "error" => $e->getMessage()
    ]);
}

?>