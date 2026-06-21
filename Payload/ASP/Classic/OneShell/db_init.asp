<%

Response.Buffer = True

Function CheckProvider(progId)
    On Error Resume Next
    Dim obj
    Set obj = Server.CreateObject(progId)
    CheckProvider = (Err.Number = 0)
    Err.Clear
    Set obj = Nothing
    On Error GoTo 0
End Function

Dim checks
Set checks = Server.CreateObject("Scripting.Dictionary")

checks.Add "MySQL ODBC", CheckProvider("ADODB.Connection") ' generic test (ODBC via ADO)
checks.Add "SQL Server (OLE DB)", CheckProvider("ADODB.Connection")
checks.Add "Oracle (OLE DB/ODBC)", CheckProvider("ADODB.Connection")
checks.Add "SQLite (ODBC)", CheckProvider("ADODB.Connection")
checks.Add "PostgreSQL (ODBC)", CheckProvider("ADODB.Connection")
checks.Add "Redis", False ' no standard COM provider
checks.Add "MongoDB", False ' no standard COM provider

Dim key
For Each key In checks.Keys
    Echo key & ":" & Abs(CInt(checks(key))) & ","
Next

%>