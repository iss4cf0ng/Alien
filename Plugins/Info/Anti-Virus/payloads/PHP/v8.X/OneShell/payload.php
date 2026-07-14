<?php

header('Content-Type: application/json; charset=utf-8');

exec('tasklist /NH /FO CSV', $outputLines);

$processes = [];

foreach ($outputLines as $line) {
    $data = str_getcsv($line);
    
    if (isset($data[0])) {
        $processes[] = trim($data[0]); 
    }
}

echo json_encode($processes);

?>