<?php

header("Content-Type: application/json");

function find_openssl_conf()
{
    $candidates = [
        "D:/xampp/apache/conf/openssl.cnf",
        "D:/xampp/php/extras/openssl/openssl.cnf",
        "C:/xampp/apache/conf/openssl.cnf",
        "C:/xampp/php/extras/openssl/openssl.cnf",
        "/etc/ssl/openssl.cnf",
        "/usr/lib/ssl/openssl.cnf",
        "/usr/local/etc/openssl/openssl.cnf",
        "/opt/homebrew/etc/openssl/openssl.cnf",
    ];

    $env = getenv("OPENSSL_CONF");
    if ($env && file_exists($env))
        return $env;

    $which = shell_exec("openssl version -d 2>/dev/null") ?? shell_exec("openssl version -d 2>nul");
    if ($which && preg_match('/OPENSSLDIR:\s*"([^"]+)"/', $which, $m)) {
        $path = rtrim($m[1], "/\\") . "/openssl.cnf";
        if (file_exists($path))
            return $path;
    }

    $phpDir = dirname(PHP_BINARY);
    foreach ([$phpDir, dirname($phpDir)] as $base) {
        foreach (["/extras/openssl/openssl.cnf", "/conf/openssl.cnf"] as $suffix) {
            if (file_exists($base . $suffix))
                return $base . $suffix;
        }
    }

    foreach ($candidates as $path) {
        if (file_exists($path))
            return $path;
    }

    return null;
}

$OPENSSL_CONF = find_openssl_conf();
if (!$OPENSSL_CONF) {
    http_response_code(500);
    echo json_encode(["error" => "openssl.cnf not found"]);

    exit;
}

function get_master_keys($conf)
{
    $priv = apcu_fetch("master_sign_priv");
    $pub  = apcu_fetch("master_sign_pub");

    if (!$priv || !$pub) {
        $res = openssl_pkey_new([
            "private_key_bits" => 2048,
            "private_key_type" => OPENSSL_KEYTYPE_RSA,
            "config" => $conf,
        ]);

        openssl_pkey_export($res, $priv, null, ["config" => $conf]);
        $pub = openssl_pkey_get_details($res)['key'];

        apcu_store("master_sign_priv", $priv);
        apcu_store("master_sign_pub",  $pub);
    }

    return [$priv, $pub];
}

define("TOKEN_KEY_LEN", 32);

function get_token_master_key()
{
    $key = apcu_fetch("token_master_key");
    if (!$key) {
        $key = random_bytes(32);
        apcu_store("token_master_key", $key);
    }
    return $key;
}

function seal_token($data)
{
    $key = get_token_master_key();
    $iv = random_bytes(12);
    $plain = json_encode($data);

    $cipher = openssl_encrypt(
        $plain, "aes-256-gcm", $key,
        OPENSSL_RAW_DATA, $iv, $tag
    );

    return base64_encode($iv . $tag . $cipher);
}

function open_token($token)
{
    $key = get_token_master_key();
    $raw = base64_decode($token);

    $iv = substr($raw, 0, 12);
    $tag = substr($raw, 12, 16);
    $cipher = substr($raw, 28);

    $plain = openssl_decrypt(
        $cipher, "aes-256-gcm", $key,
        OPENSSL_RAW_DATA, $iv, $tag
    );

    if ($plain === false)
        return null;

    return json_decode($plain, true);
}

function aes_encrypt($key, $data)
{
    $iv = random_bytes(12);
    $cipher = openssl_encrypt(
        json_encode($data), "aes-256-gcm", $key,
        OPENSSL_RAW_DATA, $iv, $tag
    );

    return $iv . $tag . $cipher;
}

function aes_decrypt($key, $raw)
{
    $iv = substr($raw, 0, 12);
    $tag = substr($raw, 12, 16);
    $cipher = substr($raw, 28);

    $plain = openssl_decrypt(
        $cipher, "aes-256-gcm", $key,
        OPENSSL_RAW_DATA, $iv, $tag
    );

    return json_decode($plain, true);
}

function jsonOut($data)
{
    echo json_encode($data);
    exit;
}

$method = $_SERVER['REQUEST_METHOD'];
$body = file_get_contents("php://input");

if ($method === 'GET') {

    [$signPriv, $signPub] = get_master_keys($OPENSSL_CONF);

    $res = openssl_pkey_new([
        "curve_name"       => "prime256v1",
        "private_key_type" => OPENSSL_KEYTYPE_EC,
        "config"           => $OPENSSL_CONF,
    ]);
    openssl_pkey_export($res, $ecdhPriv, null, ["config" => $OPENSSL_CONF]);
    $ecdhPub = trim(openssl_pkey_get_details($res)['key']);

    openssl_sign($ecdhPub, $sig, $signPriv, OPENSSL_ALGO_SHA256);

    $token = seal_token([
        "ecdh_priv" => $ecdhPriv,
        "created" => time(),
    ]);

    jsonOut([
        "signPubKey" => $signPub,
        "serverEcdhPub" => $ecdhPub,
        "signature" => base64_encode($sig),
        "handshakeToken" => $token,
    ]);
}

// POST with handshakeToken + clientPubKey + clientSignPub
// Mutual auth: client proves it holds the private key by signing its own pub key
if ($method === 'POST') {

    $req = json_decode($body, true);

    // Handshake
    if (isset($req['handshakeToken'], $req['clientEcdhPub'], $req['clientSignPub'], $req['clientSig'])) {
        $state = open_token($req['handshakeToken']);
        if (!$state)
            jsonOut(["error" => "invalid or tampered token"]);

        if (time() - $state['created'] > 300)
            jsonOut(["error" => "handshake token expired"]);

        $clientEcdhPub = trim($req['clientEcdhPub']);
        $clientSignPub = trim($req['clientSignPub']);
        $clientSig = base64_decode($req['clientSig']);

        $clientSignKey = openssl_pkey_get_public($clientSignPub);
        $valid = openssl_verify($clientEcdhPub, $clientSig, $clientSignKey, OPENSSL_ALGO_SHA256);

        if ($valid !== 1)
            jsonOut(["error" => "client authentication failed"]);

        $serverKey = openssl_pkey_get_private($state['ecdh_priv']);
        $clientKey = openssl_pkey_get_public($clientEcdhPub);
        $secret = openssl_pkey_derive($clientKey, $serverKey, 32);

        $aesKey = hash_hkdf("sha256", $secret, 32, "secure-channel");

        $sessionToken = seal_token([
            "aes_key" => base64_encode($aesKey),
            "last_seq" => 0,
            "seq_window" => [],
            "client_sign_pub" => $clientSignPub,  // remember who we authed
        ]);

        jsonOut(["status" => "secure channel established", "sessionToken" => $sessionToken]);
    }

    if (isset($req['sessionToken'], $req['payload'])) {

        $state = open_token($req['sessionToken']);
        if (!$state)
            jsonOut(["error" => "invalid session token"]);

        $aesKey = base64_decode($state['aes_key']);

        $raw = base64_decode($req['payload']);
        $cmd = aes_decrypt($aesKey, $raw);

        if (!isset($cmd['seq'], $cmd['cmd']))
            jsonOut(["error" => "invalid request"]);

        $seq = (int)$cmd['seq'];
        $window = $state['seq_window'];

        if ($seq <= $state['last_seq'] - 50)
            jsonOut(["error" => "too old"]);

        if (in_array($seq, $window, true))
            jsonOut(["error" => "replay detected"]);

        $window[] = $seq;
        if (count($window) > 50)
            array_shift($window);

        if ($seq > $state['last_seq'])
            $state['last_seq'] = $seq;

        switch ($cmd['cmd']) {
            case "ping":
                $resp = ["cmd" => "pong", "seq" => $seq];
                break;
            case "echo":
                $resp = ["echo" => $cmd['data'] ?? null];
                break;
            default:
                $resp = ["error" => "unknown cmd"];
        }

        $newToken = seal_token([
            "aes_key" => $state['aes_key'],
            "last_seq" => $state['last_seq'],
            "seq_window" => $window,
            "client_sign_pub" => $state['client_sign_pub'],
        ]);

        $encResp = base64_encode(aes_encrypt($aesKey, $resp));

        header("Content-Type: application/json");
        echo json_encode(["payload" => $encResp, "sessionToken" => $newToken]);

        exit;
    }
}

jsonOut(["error" => "invalid request"]);