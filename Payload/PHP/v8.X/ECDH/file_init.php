<?php
$szCurrentDir = getcwd();
$bUnixLike = str_contains($szCurrentDir, '/');
echo($szCurrentDir);
echo('|');
if ($bUnixLike)
{
    echo('/');
}
else
{
    $szOutput = shell_exec('wmic logicaldisk get name');
    $asOutput = explode(' ', $szOutput);
    $aResult = array();

    for ($i = 0; $i < count($asOutput); $i++)
    {
        if (str_contains($asOutput[$i], ':'))
        {
            array_push($aResult, trim($asOutput[$i]));
        }
    }

    echo(join(',', $aResult));
}
?>