<%

Response.CharSet = "utf-8"
Response.ContentType = "text/plain"

Function Base64Decode(ByVal vIn)
    Dim oXML, oNode
    Set oXML = Server.CreateObject("MSXML2.DOMDocument.3.0")
    Set oNode = oXML.CreateElement("base64")
    oNode.dataType = "bin.base64"
    oNode.text = vIn
    Base64Decode = Stream_BinaryToString(oNode.nodeTypedValue)
    Set oNode = Nothing
    Set oXML = Nothing
End Function

Function Stream_BinaryToString(ByVal Binary)
    Dim oStream
    Set oStream = Server.CreateObject("ADODB.Stream")
    oStream.Type = 1
    oStream.Open
    oStream.Write Binary
    oStream.Position = 0
    oStream.Type = 2
    oStream.Charset = "utf-8"
    Stream_BinaryToString = oStream.ReadText
    oStream.Close
    Set oStream = Nothing
End Function

Function ParseTargets(jsonStr)
    Dim targets()
    Dim count
    count = 0
    
    Dim p1, p2, subStr, items, i, item
    p1 = InStr(jsonStr, """targets""")
    If p1 > 0 Then
        p2 = InStr(p1, jsonStr, "]")
        If p2 > p1 Then
            subStr = Mid(jsonStr, p1, p2 - p1 + 1)
            items = Split(subStr, ",")
            For i = 0 To UBound(items)
                item = items(i)
                item = Replace(item, """", "")
                item = Replace(item, "[", "")
                item = Replace(item, "]", "")
                item = Replace(item, "{", "")
                item = Replace(item, "}", "")
                item = Replace(item, "targets", "")
                item = Replace(item, ":", "")
                item = Trim(item)
                If item <> "" Then
                    ReDim Preserve targets(count)
                    targets(count) = item
                    count = count + 1
                End If
            Next
        End If
    End If
    
    If count = 0 Then
        ReDim targets(0)
        targets(0) = "8.8.8.8:53"
    End If
    
    ParseTargets = targets
End Function

Function ExecuteEgressTest(targets)
    Dim jsonResult
    jsonResult = "["
    
    Dim i, target, parts, host, port, protocol, status, reason, latency
    Dim startTime, endTime, http
    
    For i = 0 To UBound(targets)
        target = Trim(targets(i))
        If target <> "" Then
            parts = Split(target, ":")
            host = parts(0)
            If UBound(parts) >= 1 Then
                port = CInt(parts(1))
            Else
                port = 80
            End If
            
            status = "closed"
            reason = "Connection timeout or filtered"
            latency = 0
            
            If port = 443 Then
                protocol = "HTTPS/TCP"
            ElseIf port = 53 Then
                protocol = "DNS/UDP-TCP"
            Else
                protocol = "TCP"
            End If
            
            On Error Resume Next
            Set http = Server.CreateObject("MSXML2.ServerXMLHTTP.6.0")
            If Err.Number <> 0 Then
                Set http = Server.CreateObject("MSXML2.ServerXMLHTTP.3.0")
            End If
            
            If Err.Number = 0 Then
                http.setTimeouts 1000, 1000, 1000, 1000
                
                Dim testUrl
                If port = 443 Then
                    testUrl = "https://" & host & ":" & port & "/"
                ElseIf port = 80 Then
                    testUrl = "http://" & host & ":" & port & "/"
                Else
                    testUrl = "http://" & host & ":" & port & "/"
                End If
                
                startTime = Timer
                http.open "GET", testUrl, False
                http.send
                
                If Err.Number = 0 Then
                    latency = Round((Timer - startTime) * 1000, 2)
                    status = "open"
                    reason = "Connected successfully (HTTP Status: " & http.status & ")"
                Else
                    reason = "Filter or Timeout (" & Err.Description & ")"
                End If
                Err.Clear
            Else
                reason = "XMLHTTP object creation failed"
            End If
            Set http = Nothing
            On Error Goto 0
            
            If jsonResult <> "[" Then jsonResult = jsonResult & ","
            
            jsonResult = jsonResult & "{" & _
                """target"":""" & target & """," & _
                """status"":""" & status & """," & _
                """protocol"":""" & protocol & """," & _
                """latency"":" & latency & "," & _
                """reason"":""" & Replace(reason, """", "\""") & """" & _
            "}"
        End If
    Next
    
    jsonResult = jsonResult & "]"
    ExecuteEgressTest = jsonResult
End Function

' Main
Dim z1, configRaw, targets
z1 = Request.Form("z1")

If z1 = "" Then
    Response.Write "[{""target"":""ERROR"",""status"":""closed"",""reason"":""Missing parameter z1""}]"
    Response.End
End If

On Error Resume Next
configRaw = Base64Decode(z1)
If Err.Number <> 0 Or configRaw = "" Then
    configRaw = z1
End If
On Error Goto 0

targets = ParseTargets(configRaw)
Response.Write ExecuteEgressTest(targets)

%>