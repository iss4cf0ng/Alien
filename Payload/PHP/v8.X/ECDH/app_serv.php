<?php

@ini_set('display_errors', '0');
@set_time_limit(0);

$win = (false !== strpos(strtolower(PHP_OS), 'win'));

function command_exists($cmd): bool {
    if (false !== strpos(strtolower(PHP_OS), 'win')) {
        $where = shell_exec("where {$cmd} 2>NUL");
        return !empty(trim((string)$where));
    }
    $return = shell_exec("which {$cmd} 2>/dev/null");
    return !empty($return);
}

function clean_value($v) {
    if ($v === null) return '';
    return trim((string)$v);
}

function get_windows_applications(): array {
    $apps = [];

    if (command_exists('powershell')) {
        $ps_script = '[Console]::OutputEncoding = [Text.Encoding]::UTF8; '
                   . 'Get-ChildItem "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall", "HKLM:\Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall" -ErrorAction SilentlyContinue '
                   . '| ForEach-Object { try { Get-ItemProperty $_.PSPath -ErrorAction Stop } catch {} } '
                   . '| Where-Object DisplayName '
                   . '| Select-Object DisplayName, DisplayVersion, Publisher, InstallDate '
                   . '| ConvertTo-Json -Compress';

        $encoded_cmd = base64_encode(iconv('UTF-8', 'UTF-16LE', $ps_script));
        $raw_output  = shell_exec("powershell -NoProfile -ExecutionPolicy Bypass -EncodedCommand {$encoded_cmd} < NUL");

        if (!empty($raw_output)) {
            $clean_raw = str_replace("\x00", '', $raw_output);
            $clean_raw = preg_replace('/[\x00-\x1F\x7F\xEF\xBB\xBF]/', '', $clean_raw);

            $start = strpos($clean_raw, '[');
            $end   = strrpos($clean_raw, ']');

            if ($start !== false && $end !== false && $end > $start) {
                $json_str = substr($clean_raw, $start, $end - $start + 1);
                $data     = json_decode($json_str, true, 512, JSON_INVALID_UTF8_SUBSTITUTE);

                if (is_array($data)) {
                    $rows = isset($data[0]) && is_array($data[0]) ? $data : [$data];
                    foreach ($rows as $row) {
                        $name = clean_value($row['DisplayName'] ?? '');
                        if ($name !== '') {
                            $apps[] = [
                                'name'      => $name,
                                'version'   => clean_value($row['DisplayVersion'] ?? ''),
                                'vendor'    => clean_value($row['Publisher'] ?? ''),
                                'installed' => clean_value($row['InstallDate'] ?? ''),
                                'source'    => 'powershell_registry'
                            ];
                        }
                    }
                    if (!empty($apps)) return $apps;
                }
            }
        }
    }

    if (command_exists('wmic')) {
        $wmic_out = shell_exec('wmic product get Name,Version,Vendor,InstallDate /format:csv 2>NUL');
        if (!empty($wmic_out)) {
            $lines = explode("\n", trim($wmic_out));
            foreach ($lines as $line) {
                $line = trim($line);
                if (empty($line) || strspn($line, 'Node,') === 5) continue;

                $cols = explode(',', $line);
                if (count($cols) >= 5) {
                    $name = clean_value($cols[2] ?? '');
                    if ($name !== '') {
                        $apps[] = [
                            'name'      => $name,
                            'version'   => clean_value($cols[4] ?? ''),
                            'vendor'    => clean_value($cols[3] ?? ''),
                            'installed' => clean_value($cols[1] ?? ''),
                            'source'    => 'wmic'
                        ];
                    }
                }
            }
        }
    }

    return $apps;
}

function get_windows_services(): array {
    $services = [];

    if (command_exists('powershell')) {
        $ps_script = '[Console]::OutputEncoding = [Text.Encoding]::UTF8; '
                   . 'Get-Service | ForEach-Object { @{ N = $_.Name; D = $_.DisplayName; S = $_.Status.ToString(); M = $_.StartType.ToString() } } '
                   . '| ConvertTo-Json -Compress';

        $encoded_cmd = base64_encode(iconv('UTF-8', 'UTF-16LE', $ps_script));
        $raw_output  = shell_exec("powershell -NoProfile -ExecutionPolicy Bypass -EncodedCommand {$encoded_cmd} < NUL");

        if (!empty($raw_output)) {
            $clean_raw = str_replace("\x00", '', $raw_output);
            $clean_raw = preg_replace('/[\x00-\x1F\x7F\xEF\xBB\xBF]/', '', $clean_raw);

            $start = strpos($clean_raw, '[');
            $end   = strrpos($clean_raw, ']');

            if ($start !== false && $end !== false && $end > $start) {
                $json_str = substr($clean_raw, $start, $end - $start + 1);
                $data     = json_decode($json_str, true, 512, JSON_INVALID_UTF8_SUBSTITUTE);

                if (is_array($data)) {
                    $rows = isset($data[0]) && is_array($data[0]) ? $data : [$data];
                    foreach ($rows as $row) {
                        $name = clean_value($row['N'] ?? $row['Name'] ?? '');
                        if ($name !== '') {
                            $status_raw = strtolower(clean_value($row['S'] ?? $row['Status'] ?? ''));
                            $services[] = [
                                'name'         => $name,
                                'display_name' => clean_value($row['D'] ?? $row['DisplayName'] ?? ''),
                                'status'       => ($status_raw === 'running') ? 'running' : 'stopped',
                                'start_type'   => clean_value($row['M'] ?? $row['StartType'] ?? ''),
                                'source'       => 'powershell'
                            ];
                        }
                    }
                    if (!empty($services)) return $services;
                }
            }
        }
    }

    if (command_exists('wmic')) {
        $wmic_out = shell_exec('wmic service get Name,DisplayName,State,StartMode /format:csv 2>NUL');
        if (!empty($wmic_out)) {
            $lines = explode("\n", trim($wmic_out));
            foreach ($lines as $line) {
                $line = trim($line);
                if (empty($line) || strspn($line, 'Node,') === 5) continue;

                $cols = explode(',', $line);
                if (count($cols) >= 5) {
                    $name = clean_value($cols[2] ?? '');
                    if ($name !== '') {
                        $status_raw = strtolower(clean_value($cols[4] ?? ''));
                        $services[] = [
                            'name'         => $name,
                            'display_name' => clean_value($cols[1] ?? ''),
                            'status'       => ($status_raw === 'running') ? 'running' : 'stopped',
                            'start_type'   => clean_value($cols[3] ?? ''),
                            'source'       => 'wmic'
                        ];
                    }
                }
            }
            if (!empty($services)) return $services;
        }
    }

    if (command_exists('sc')) {
        $sc_out = shell_exec('sc query state= all type= service 2>NUL');
        if (!empty($sc_out)) {
            $curr_name = '';
            $curr_disp = '';
            $curr_stat = 'stopped';

            $lines = explode("\n", $sc_out);
            foreach ($lines as $line) {
                $line = trim($line);
                if (strpos($line, 'SERVICE_NAME:') === 0) {
                    if ($curr_name !== '') {
                        $services[] = [
                            'name'         => $curr_name,
                            'display_name' => $curr_disp ?: $curr_name,
                            'status'       => $curr_stat,
                            'start_type'   => 'unknown',
                            'source'       => 'sc'
                        ];
                    }
                    $curr_name = clean_value(substr($line, 13));
                    $curr_disp = '';
                    $curr_stat = 'stopped';
                } elseif (strpos($line, 'DISPLAY_NAME:') === 0) {
                    $curr_disp = clean_value(substr($line, 13));
                } elseif (strpos($line, 'STATE') === 0 && strpos($line, 'RUNNING') !== false) {
                    $curr_stat = 'running';
                }
            }
            
            if ($curr_name !== '') {
                $services[] = [
                    'name'         => $curr_name,
                    'display_name' => $curr_disp ?: $curr_name,
                    'status'       => $curr_stat,
                    'start_type'   => 'unknown',
                    'source'       => 'sc'
                ];
            }
        }
    }

    return $services;
}

function get_unix_like_applications(): array {
    $apps = [];

    if (command_exists('dpkg-query')) {
        $output = [];
        exec("dpkg-query -W -f='\${Package}\t\${Version}\t\${Maintainer}\n' 2>/dev/null", $output);
        foreach ($output as $line) {
            $parts = explode("\t", trim($line));
            if (count($parts) >= 2) {
                $apps[] = [
                    'name'    => clean_value($parts[0]),
                    'version' => clean_value($parts[1]),
                    'vendor'  => clean_value($parts[2] ?? ''),
                    'source'  => 'dpkg'
                ];
            }
        }
    }
    elseif (command_exists('rpm')) {
        $output = [];
        exec("rpm -qa --qf '%{NAME}\t%{VERSION}-%{RELEASE}\t%{VENDOR}\n' 2>/dev/null", $output);
        foreach ($output as $line) {
            $parts = explode("\t", trim($line));
            if (count($parts) >= 2) {
                $apps[] = [
                    'name'    => clean_value($parts[0]),
                    'version' => clean_value($parts[1]),
                    'vendor'  => clean_value($parts[2] ?? ''),
                    'source'  => 'rpm'
                ];
            }
        }
    }

    if (command_exists('brew')) {
        $output = [];
        exec("brew list --versions 2>/dev/null", $output);
        foreach ($output as $line) {
            $parts = explode(" ", trim($line));
            if (count($parts) >= 2) {
                $apps[] = [
                    'name'    => clean_value($parts[0]),
                    'version' => clean_value($parts[1]),
                    'vendor'  => 'Homebrew',
                    'source'  => 'homebrew'
                ];
            }
        }
    }

    if (command_exists('snap')) {
        $output = [];
        exec("snap list 2>/dev/null", $output);
        array_shift($output);
        foreach ($output as $line) {
            $cols = preg_split('/\s+/', trim($line));
            if (count($cols) >= 2) {
                $apps[] = [
                    'name'    => clean_value($cols[0]),
                    'version' => clean_value($cols[1]),
                    'vendor'  => clean_value($cols[4] ?? ''),
                    'source'  => 'snap'
                ];
            }
        }
    }

    return $apps;
}

function get_unix_like_services(): array {
    $services = [];

    if (command_exists('systemctl')) {
        $output = [];
        exec("systemctl list-units --type=service --all --no-pager --no-legend 2>/dev/null", $output);

        foreach ($output as $line) {
            $cols = preg_split('/\s+/', trim($line), 5);
            if (count($cols) >= 4) {
                $services[] = [
                    'name'         => clean_value(str_replace('.service', '', $cols[0])),
                    'display_name' => clean_value($cols[4] ?? $cols[0]),
                    'status'       => ($cols[2] === 'active') ? 'running' : 'stopped',
                    'source'       => 'systemd'
                ];
            }
        }
    }
    elseif (command_exists('service')) {
        $output = [];
        exec("service --status-all 2>/dev/null", $output);

        foreach ($output as $line) {
            if (preg_match('/\[\s*([\+\-\?])\s*\]\s+(.+)/', trim($line), $matches)) {
                $status_flag = $matches[1];
                $name        = trim($matches[2]);

                $services[] = [
                    'name'         => clean_value($name),
                    'display_name' => clean_value($name),
                    'status'       => ($status_flag === '+') ? 'running' : 'stopped',
                    'source'       => 'sysvinit'
                ];
            }
        }
    }
    elseif (command_exists('launchctl')) {
        $output = [];
        exec("launchctl list 2>/dev/null", $output);
        array_shift($output);

        foreach ($output as $line) {
            $cols = preg_split('/\s+/', trim($line), 3);
            if (count($cols) >= 3) {
                $pid   = $cols[0];
                $label = $cols[2];

                $services[] = [
                    'name'         => clean_value($label),
                    'display_name' => clean_value($label),
                    'status'       => ($pid !== '-' && is_numeric($pid)) ? 'running' : 'stopped',
                    'source'       => 'launchd'
                ];
            }
        }
    }

    return $services;
}

$result = [
    'success'     => false,
    'system_type' => $win ? 'windows' : 'unix_like',
    'os_raw'      => PHP_OS,
    'error'       => '',
    'data'        => [
        'applications' => [],
        'services'     => []
    ]
];

try {
    if ($win) {
        $result['data']['applications'] = get_windows_applications();
        $result['data']['services']     = get_windows_services();
    } else {
        $result['data']['applications'] = get_unix_like_applications();
        $result['data']['services']     = get_unix_like_services();
    }

    $result['success'] = true;

} catch (Exception $e) {
    $result['error'] = $e->getMessage();
}

header('Content-Type: application/json; charset=utf-8');
echo json_encode($result,
    JSON_PRETTY_PRINT |
    JSON_UNESCAPED_UNICODE |
    JSON_UNESCAPED_SLASHES
);

?>