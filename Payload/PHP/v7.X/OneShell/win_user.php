<?php

@ini_set('display_errors','0');
@set_time_limit(0);

function has_powershell(): bool {
    $out = [];
    $code = 0;

    exec("powershell -Command \"Get-Host\" 2>NUL", $out, $code);
    return $code === 0;
}

function clean_value($v) {
    return trim(preg_replace('/[^\PC\s]/u', '', (string)$v));
}

function flatten($item): array {

    $out = [];

    foreach ($item as $k => $v) {

        if (is_array($v)) {
            // convert nested objects safely
            $out[$k] = json_encode($v, JSON_UNESCAPED_UNICODE | JSON_UNESCAPED_SLASHES);
        }
        else {
            $out[$k] = clean_value($v);
        }
    }

    return $out;
}

function run_powershell($query): array {

    $cmd = "powershell -NoProfile -Command \"{$query} | ConvertTo-Json -Depth 3 -Compress\"";

    $output = [];
    $code = 0;

    exec($cmd, $output, $code);

    if ($code !== 0 || empty($output)) {
        return [];
    }

    $json = implode("", $output);
    $data = json_decode($json, true);

    if ($data === null) {
        return [];
    }

    if (isset($data[0]) && is_array($data[0])) {
        return $data;
    }

    return [$data];
}

function clean_line($line) {
    $line = mb_convert_encoding($line, 'UTF-8', 'auto');
    $line = preg_replace('/\xEF\xBB\xBF/', '', $line);
    return trim($line);
}

function parse_wmic($class): array {

    $output = [];
    $code = 0;

    exec("wmic path {$class} get /format:list", $output, $code);

    if ($code !== 0 || empty($output)) {
        return [];
    }

    $rows = [];
    $current = [];

    foreach ($output as $line) {

        $line = clean_line($line);

        if ($line === '') {

            if (!empty($current)) {
                ksort($current);
                $rows[] = $current;
            }

            $current = [];
            continue;
        }

        if (strpos($line, '=') === false) {
            continue;
        }

        [$k, $v] = explode('=', $line, 2);

        $k = clean_value($k);
        $v = clean_value($v);

        if ($k === '') continue;

        $current[$k] = $v;
    }

    if (!empty($current)) {
        ksort($current);
        $rows[] = $current;
    }

    return $rows;
}

function get_data($ps_query, $wmic_class): array {

    if (has_powershell()) {

        $data = run_powershell($ps_query);

        if (!empty($data)) {

            $clean = [];

            foreach ($data as $row) {
                $clean[] = flatten($row);
            }

            return $clean;
        }
    }

    return parse_wmic($wmic_class);
}

$result = [
    'success' => false,
    'error' => '',
    'data' => null
];

try {

    $result['data'] = [
        'user_accounts' => get_data("Get-CimInstance Win32_UserAccount", "Win32_UserAccount"),
        'user_profiles' => get_data("Get-CimInstance Win32_UserProfile", "Win32_UserProfile"),
        'groups'        => get_data("Get-CimInstance Win32_Group", "Win32_Group"),
        'group_users'   => get_data("Get-CimInstance Win32_GroupUser", "Win32_GroupUser"),
        'logged_on'     => get_data("Get-CimInstance Win32_LoggedOnUser", "Win32_LoggedOnUser"),
        'logon_session' => get_data("Get-CimInstance Win32_LogonSession", "Win32_LogonSession")
    ];

    $result['success'] = true;

} catch (Exception $e) {
    $result['error'] = $e->getMessage();
}

header('Content-Type: application/json; charset=utf-8');
echo json_encode($result,
    JSON_PRETTY_PRINT |
    JSON_UNESCAPED_UNICODE |
    JSON_UNESCAPED_SLASHES
);

?>