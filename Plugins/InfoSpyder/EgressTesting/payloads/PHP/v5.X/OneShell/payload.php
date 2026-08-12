<?php

@error_reporting(0);
@ini_set('display_errors', 0);

header('Content-Type: text/plain; charset=utf-8');

function execute_egress_test($targets) {
    $results = array();

    foreach ($targets as $target) {
        $target = trim($target);
        if (empty($target)) continue;

        $parts = explode(':', $target);
        $host = $parts[0];
        $port = isset($parts[1]) ? (int)$parts[1] : 80;

        $startTime = microtime(true);
        $status = "closed";
        $reason = "Connection timeout or filtered";
        $latency = 0;

        if (!filter_var($host, FILTER_VALIDATE_IP)) {
            $ip = @gethostbyname($host);
            if ($ip === $host) {
                $results[] = array(
                    'target' => $target,
                    'status' => 'closed',
                    'protocol' => ($port == 443 ? 'HTTPS/TCP' : ($port == 53 ? 'DNS/UDP-TCP' : 'TCP')),
                    'latency' => 0,
                    'reason' => 'DNS Resolution Failed'
                );
                continue;
            }
        }

        // 使用 1.5 秒超時，避免內網卡死
        $socket = @fsockopen($host, $port, $errno, $errstr, 1.5);

        if ($socket) {
            $latency = round((microtime(true) - $startTime) * 1000, 2);
            $status = "open";
            $reason = "Connected successfully";
            @fclose($socket);
        } else {
            if (!empty($errstr)) {
                $reason = $errstr;
            }
        }

        $results[] = array(
            'target' => $target,
            'status' => $status,
            'protocol' => ($port == 443 ? 'HTTPS/TCP' : ($port == 53 ? 'DNS/UDP-TCP' : 'TCP')),
            'latency' => $latency,
            'reason' => $reason
        );
    }

    return json_encode($results);
}

function main() {
    $z1 = isset($_POST['z1']) ? $_POST['z1'] : '';
    if (empty($z1)) {
        echo json_encode(array(array('target' => 'ERROR', 'status' => 'closed', 'reason' => 'Missing parameter z1')));
        return;
    }

    $decoded = base64_decode($z1, true);
    $config_raw = ($decoded !== false && $decoded !== '') ? $decoded : $z1;
    $config = json_decode($config_raw, true);
    
    if (!$config) {
        echo json_encode(array(array('target' => 'ERROR', 'status' => 'closed', 'reason' => 'Invalid JSON')));
        return;
    }

    $targets = (isset($config['targets']) && is_array($config['targets'])) ? $config['targets'] : array();
    if (empty($targets)) {
        echo json_encode(array(
            array('target' => '8.8.8.8:53', 'status' => 'closed', 'protocol' => 'TCP', 'latency' => 0, 'reason' => 'No targets provided')
        ));
        return;
    }

    $resultJson = execute_egress_test($targets);
    echo $resultJson;
}

main();

?>