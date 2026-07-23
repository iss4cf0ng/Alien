<?php

header('Content-Type: application/json; charset=utf-8');

$processes = array();
exec('tasklist /NH /FO CSV 2>&1', $outputLines, $returnVar);

if ($returnVar === 0 && is_array($outputLines)) {
    foreach ($outputLines as $line) {
        if (trim($line) === '') {
            continue;
        }
        
        $data = str_getcsv($line);
        if (isset($data[0]) && trim($data[0]) !== '') {
            $processes[] = trim($data[0]); 
        }
    }
}

if (defined('JSON_UNESCAPED_UNICODE')) {
    echo json_encode($processes, JSON_UNESCAPED_UNICODE);
} else {
    echo json_encode($processes);
}

?>