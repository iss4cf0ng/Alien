import requests
import os
import json
import base64

from cryptography.hazmat.primitives.asymmetric import ec
from cryptography.hazmat.primitives.serialization import load_pem_public_key
from cryptography.hazmat.primitives import hashes
from cryptography.hazmat.primitives.kdf.hkdf import HKDF
from cryptography.hazmat.primitives.asymmetric import padding
from cryptography.hazmat.primitives.ciphers.aead import AESGCM


URL = "http://localhost/api.php"

resp = requests.get(URL).json()

server_pub_pem = resp["serverPubKey"].encode()
signature = base64.b64decode(resp["signature"])

server_pub = load_pem_public_key(server_pub_pem)

server_pub.verify(
    signature,
    server_pub_pem,
    padding.PKCS1v15(),
    hashes.SHA256()
)

print("[+] Server verified")

client_key = ec.generate_private_key(ec.SECP256R1())
client_pub = client_key.public_key()

client_pub_pem = client_pub.public_bytes(
    encoding=serialization.Encoding.PEM,
    format=serialization.PublicFormat.SubjectPublicKeyInfo
)

requests.post(URL, data=client_pub_pem)

server_pub_key = load_pem_public_key(server_pub_pem)

shared_secret = client_key.exchange(ec.ECDH(), server_pub_key)

aes_key = HKDF(
    algorithm=hashes.SHA256(),
    length=32,
    salt=None,
    info=b"secure-channel"
).derive(shared_secret)

print("[+] AES key derived")


seq = 1

def send(cmd, data=None):
    global seq

    payload = {
        "cmd": cmd,
        "seq": seq,
        "data": data
    }

    aesgcm = AESGCM(aes_key)

    nonce = os.urandom(12)
    cipher = aesgcm.encrypt(nonce, json.dumps(payload).encode(), None)

    tag = cipher[-16:]
    ct = cipher[:-16]

    packet = nonce + tag + ct

    resp = requests.post(URL, data=packet).content

    nonce = resp[:12]
    tag = resp[12:28]
    ct = resp[28:]

    result = aesgcm.decrypt(nonce, ct + tag, None)

    seq += 1

    return json.loads(result)

print(send("ping"))
print(send("echo", "hello world"))