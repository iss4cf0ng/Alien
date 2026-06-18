<%

Response.Buffer = True
Response.ContentType = "text/html"

Function TestObj(progId)
    On Error Resume Next
    Dim o : Set o = Server.CreateObject(progId)
    If Err.Number = 0 Then
        TestObj = "AVAILABLE"
        Set o = Nothing
    Else
        TestObj = "NOT INSTALLED"
    End If
    Err.Clear
End Function

' Reliable function to get architecture even when Request.ServerVariables fails
Function GetArchitecture()
    On Error Resume Next
    Dim shell, arch
    arch = Request.ServerVariables("PROCESSOR_ARCHITECTURE")
    
    ' If server variables fail, grab it directly from the system environment
    If arch = "" Then
        Set shell = Server.CreateObject("WScript.Shell")
        arch = shell.ExpandEnvironmentStrings("%PROCESSOR_ARCHITECTURE%")
        Set shell = Nothing
    End If
    
    ' Fallback to checking the 64-bit program files path if shell is blocked
    If arch = "" Then
        Dim fso : Set fso = Server.CreateObject("Scripting.FileSystemObject")
        If fso.FolderExists("C:\Program Files (x86)") Then
            arch = "AMD64 (Simulated)"
        Else
            arch = "x86 (Simulated)"
        End If
        Set fso = Nothing
    End If
    
    If arch = "" Then arch = "UNKNOWN"
    GetArchitecture = arch
    Err.Clear
End Function

Dim item, fso
Set fso = Server.CreateObject("Scripting.FileSystemObject")

Response.Write "<table border='1' cellpadding='5' cellspacing='0'>"

' ASP ENGINE & SYSTEM
Response.Write "<tr><th colspan='2' align='left'>ASP ENGINE & SYSTEM</th></tr>"
Response.Write "<tr><td>Engine</td><td>" & ScriptEngine & " " & ScriptEngineMajorVersion & "." & ScriptEngineMinorVersion & "." & ScriptEngineBuildVersion & "</td></tr>"
Response.Write "<tr><td>Timeout</td><td>" & Server.ScriptTimeout & "</td></tr>"
Response.Write "<tr><td>SessionID</td><td>" & Session.SessionID & "</td></tr>"
Response.Write "<tr><td>Architecture</td><td>" & GetArchitecture() & "</td></tr>"

' CORE COM COMPONENTS
Response.Write "<tr><th colspan='2' align='left'>CORE COM COMPONENTS</th></tr>"
Response.Write "<tr><td>Scripting.FileSystemObject</td><td>" & TestObj("Scripting.FileSystemObject") & "</td></tr>"
Response.Write "<tr><td>Scripting.Dictionary</td><td>" & TestObj("Scripting.Dictionary") & "</td></tr>"
Response.Write "<tr><td>ADODB.Connection</td><td>" & TestObj("ADODB.Connection") & "</td></tr>"
Response.Write "<tr><td>ADODB.Recordset</td><td>" & TestObj("ADODB.Recordset") & "</td></tr>"
Response.Write "<tr><td>ADODB.Stream</td><td>" & TestObj("ADODB.Stream") & "</td></tr>"
Response.Write "<tr><td>MSXML2.DOMDocument.6.0</td><td>" & TestObj("MSXML2.DOMDocument.6.0") & "</td></tr>"
Response.Write "<tr><td>MSXML2.DOMDocument.3.0</td><td>" & TestObj("MSXML2.DOMDocument.3.0") & "</td></tr>"
Response.Write "<tr><td>MSXML2.ServerXMLHTTP.6.0</td><td>" & TestObj("MSXML2.ServerXMLHTTP.6.0") & "</td></tr>"
Response.Write "<tr><td>Microsoft.XMLHTTP</td><td>" & TestObj("Microsoft.XMLHTTP") & "</td></tr>"
Response.Write "<tr><td>WScript.Shell</td><td>" & TestObj("WScript.Shell") & "</td></tr>"
Response.Write "<tr><td>Shell.Application</td><td>" & TestObj("Shell.Application") & "</td></tr>"
Response.Write "<tr><td>CDO.Message</td><td>" & TestObj("CDO.Message") & "</td></tr>"

' SERVER VARIABLES
Response.Write "<tr><th colspan='2' align='left'>SERVER VARIABLES</th></tr>"
For Each item In Request.ServerVariables
    If Request.ServerVariables(item) <> "" Then
        Response.Write "<tr><td>" & item & "</td><td>" & Server.HTMLEncode(Request.ServerVariables(item)) & "</td></tr>"
    End If
Next

Response.Write "</table>"

%>