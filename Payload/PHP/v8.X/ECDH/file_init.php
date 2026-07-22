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
    $aResult = array();

    // First try WMIC
    $szOutput = @shell_exec('wmic logicaldisk get name 2>NUL');

    if (!empty(trim($szOutput)))
    {
        preg_match_all('/[A-Z]:/i', $szOutput, $matches);
        $aResult = $matches[0];
    }
    else
    {
        // Check whether PowerShell exists
        $szPSCheck = @shell_exec('powershell -Command "Write-Output OK" 2>NUL');

        if (trim($szPSCheck) === 'OK')
        {
            $szOutput = @shell_exec(
                'powershell -NoProfile -Command "(Get-PSDrive -PSProvider FileSystem).Name" 2>NUL'
            );

            $asDrives = preg_split('/\r\n|\r|\n/', trim($szOutput));

            foreach ($asDrives as $drive)
            {
                $drive = trim($drive);

                if ($drive !== '')
                {
                    $aResult[] = $drive . ':';
                }
            }
        }
    }

    echo(join(',', $aResult));
}

?>