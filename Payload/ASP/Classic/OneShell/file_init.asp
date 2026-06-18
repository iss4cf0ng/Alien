<%

Dim fso, drives, d, currentDir, result

Set fso = Server.CreateObject("Scripting.FileSystemObject")

' Current directory
currentDir = Server.MapPath(".")

result = currentDir & "|"

Set drives = fso.Drives

Dim first
first = True

For Each d In drives
    If first = False Then result = result & ","
    result = result & d.DriveLetter & ":"
    first = False
Next

Response.Write result

%>