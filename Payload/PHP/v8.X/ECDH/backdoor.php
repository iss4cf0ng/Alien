<?php

session_start();
header("Content-Type: application/json");

function init_keys()
{
    // ECDH keypair
    if (!isset($_SESSION['ecdh_priv'])) {

        $config = [
            "curve_name" => "prime256v1",
            "private_key_type" => OPENSSL_KEYTYPE_EC
        ];

        $res = openssl_pkey_new($config);
        openssl_pkey_export($res, $priv);

        $pub = openssl_pkey_get_details($res)['key'];

        $_SESSION['ecdh_priv'] = $priv;
        $_SESSION['ecdh_pub']  = $pub;
    }

    // signing key (server identity)
    if (!isset($_SESSION['sign_priv'])) {

        $config = [
            "private_key_bits" => 2048,
            "private_key_type" => OPENSSL_KEYTYPE_RSA
        ];

        $res = openssl_pkey_new($config);
        openssl_pkey_export($res, $priv);

        $pub = openssl_pkey_get_details($res)['key'];

        $_SESSION['sign_priv'] = $priv;
        $_SESSION['sign_pub']  = $pub;
    }

    if (!isset($_SESSION['aes_key'])) {
        $_SESSION['aes_key'] = null;
    }

    if (!isset($_SESSION['last_seq'])) {
        $_SESSION['last_seq'] = 0;
    }

    if (!isset($_SESSION['seq_window'])) {
        $_SESSION['seq_window'] = [];
    }
}

init_keys();

function jsonOut($data)
{
    echo json_encode($data);
    exit;
}

function aes_encrypt($key, $data)
{
    $iv = random_bytes(12);

    $cipher = openssl_encrypt(
        json_encode($data),
        "aes-256-gcm",
        $key,
        OPENSSL_RAW_DATA,
        $iv,
        $tag
    );

    return $iv . $tag . $cipher;
}

function aes_decrypt($key, $raw)
{
    $iv = substr($raw, 0, 12);
    $tag = substr($raw, 12, 16);
    $cipher = substr($raw, 28);

    $plain = openssl_decrypt(
        $cipher,
        "aes-256-gcm",
        $key,
        OPENSSL_RAW_DATA,
        $iv,
        $tag
    );

    return json_decode($plain, true);
}

if ($_SERVER['REQUEST_METHOD'] === 'GET') {

    $serverPub = $_SESSION['ecdh_pub'];

    openssl_sign(
        $serverPub,
        $sig,
        $_SESSION['sign_priv'],
        OPENSSL_ALGO_SHA256
    );

    jsonOut([
        "serverPubKey" => $serverPub,
        "signature" => base64_encode($sig)
    ]);
}

if ($_SERVER['REQUEST_METHOD'] === 'POST' && $_SESSION['aes_key'] === null) {

    $clientPub = file_get_contents("php://input");

    $serverPriv = $_SESSION['ecdh_priv'];

    $clientKey = openssl_pkey_get_public($clientPub);
    $serverKey = openssl_pkey_get_private($serverPriv);

    $secret = openssl_pkey_derive($clientKey, $serverKey, 32);

    $_SESSION['aes_key'] = hash_hkdf(
        "sha256",
        $secret,
        32,
        "secure-channel"
    );

    jsonOut(["status" => "secure channel established"]);
}

if ($_SERVER['REQUEST_METHOD'] === 'POST' && $_SESSION['aes_key']) {

    $key = $_SESSION['aes_key'];

    $raw = file_get_contents("php://input");

    $req = aes_decrypt($key, $raw);

    if (!isset($req['seq'], $req['cmd'])) {
        jsonOut(["error" => "invalid request"]);
    }

    $seq = (int)$req['seq'];

    $window = &$_SESSION['seq_window'];

    if ($seq <= $_SESSION['last_seq'] - 50) {
        echo json_encode(["error" => "too old"]);
        exit;
    }

    if (in_array($seq, $window, true)) {
        echo json_encode(["error" => "replay detected"]);
        exit;
    }

    $window[] = $seq;

    if (count($window) > 50) {
        array_shift($window);
    }

    if ($seq > $_SESSION['last_seq']) {
        $_SESSION['last_seq'] = $seq;
    }

    switch ($req['cmd']) {

        case "ping":
            $resp = ["cmd" => "pong", "seq" => $seq];
            break;

        case "echo":
            $resp = ["echo" => $req['data'] ?? null];
            break;

        default:
            $resp = ["error" => "unknown cmd"];
    }

    header("Content-Type: application/octet-stream");
    echo aes_encrypt($key, $resp);
    exit;
}

jsonOut(["error" => "invalid"]);