<?php

error_reporting(0);
header('Content-Type: application/json');

function http_post($url, $data, &$http_code)
{
    $ch = curl_init($url);

    curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
    curl_setopt($ch, CURLOPT_POST, true);
    curl_setopt($ch, CURLOPT_POSTFIELDS, $data);

    curl_setopt($ch, CURLOPT_HTTPHEADER, [
        'Content-Type: application/x-www-form-urlencoded',
        'Content-Length: ' . strlen($data)
    ]);

    curl_setopt($ch, CURLOPT_TIMEOUT, 15);

    $response = curl_exec($ch);

    $http_code = curl_getinfo($ch, CURLINFO_HTTP_CODE);

    curl_close($ch);

    return $response;
}

$result = [
    'status' => 'error',
    'http_code' => null,
    'data' => null
];

$url  = base64_decode($_POST['z0'] ?? '');
$data = base64_decode($_POST['z1'] ?? '');

if (!$url) {
    $result['data'] = 'Missing URL';
    die("error");
}

$http_code = 0;
$body = http_post($url, $data, $http_code);

// Output JSON
echo json_encode($body);

?>