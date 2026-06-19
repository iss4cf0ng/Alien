# obfuscators/ROT13.py

import codecs

def build(payload, script, **kwargs):

    return ''

def obfuscate(payload, raw=False, **kwargs):
    return codecs.encode(payload, 'rot_13')

def deobfuscate(payload, raw=False, **kwargs):
    return codecs.encode(payload, 'rot_13')