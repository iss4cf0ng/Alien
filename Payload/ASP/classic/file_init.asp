<%
Dim fso, currentDir
Set fso = Server.CreateObject("Scripting.FileSystemObject")

currentDir = fso.GetAbsolutePathName(".")
Response.Write currentDir & "|"

If InStr(currentDir, "/") > 0 Then
    Response.Write "/"
Else
    Dim shell, execObj, output, lines, line
    Set shell = Server.CreateObject("WScript.Shell")

    On Error Resume Next
    Set execObj = shell.Exec("cmd /c wmic logicaldisk get name")

    If Err.Number = 0 Then
        output = execObj.StdOut.ReadAll()

        lines = Split(output, vbCrLf)
        Dim drives
        drives = ""

        For Each line In lines
            line = Trim(line)

            If InStr(line, ":") > 0 Then
                If drives <> "" Then drives = drives & ","
                drives = drives & line
            End If
        Next

        Response.Write drives
    End If
    On Error GoTo 0
End If

Set shell = Nothing
Set fso = Nothing
%>