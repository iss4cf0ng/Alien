# obfuscators/BASE64.py

import base64

def build(payload, script, **kwargs):

    return ''

def obfuscate(payload, raw=False, encoding='utf-8'):
    return base64.b64encode(payload).decode(encoding)

def obfuscate(payload, raw=False, encoding='utf-8'):
    return base64.b64decode(payload).decode(encoding)
