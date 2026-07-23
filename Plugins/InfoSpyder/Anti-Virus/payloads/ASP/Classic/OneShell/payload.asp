<%

Response.ContentType = "application/json"
Response.CharSet = "utf-8"

Dim objShell, objExec, strOutput, strLine
Dim arrLines, i, match, processName
Dim jsonResult, isFirst

jsonResult = "["
isFirst = True

On Error Resume Next

Set objShell = Server.CreateObject("WScript.Shell")
Set objExec = objShell.Exec("C:\Windows\System32\tasklist.exe /NH /FO CSV")

strOutput = objExec.StdOut.ReadAll

If Err.Number = 0 And Len(strOutput) > 0 Then
    arrLines = Split(strOutput, vbCrLf)
    
    For i = 0 To UBound(arrLines)
        strLine = Trim(arrLines(i))
        
        If Len(strLine) > 0 Then
            If Left(strLine, 1) = """" Then
                Dim secondQuotePos
                secondQuotePos = InStr(2, strLine, """")
                
                If secondQuotePos > 2 Then
                    processName = Mid(strLine, 2, secondQuotePos - 2)
                    processName = UCase(Trim(processName))
                    
                    If Not isFirst Then
                        jsonResult = jsonResult & ","
                    Else
                        isFirst = False
                    End If
                    
                    jsonResult = jsonResult & """" & processName & """"
                End If
            End If
        End If
    Next
End If

jsonResult = jsonResult & "]"

Response.Write jsonResult

Set objExec = Nothing
Set objShell = Nothing

%>