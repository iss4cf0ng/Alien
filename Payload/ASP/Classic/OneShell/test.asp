<%

Function Base64Decode(str)
    Dim xml, node, stream

    Set xml = CreateObject("MSXML2.DOMDocument.3.0")
    Set node = xml.createElement("b64")
    node.dataType = "bin.base64"
    node.text = str

    Set stream = CreateObject("ADODB.Stream")
    stream.Type = 1
    stream.Open
    stream.Write node.nodeTypedValue
    stream.Position = 0
    stream.Type = 2
    stream.Charset = "utf-8"

    Base64Decode = stream.ReadText

    stream.Close
    Set stream = Nothing
    Set node = Nothing
    Set xml = Nothing
End Function

Response.Write Base64Decode(Request.Form("z0"))

%>