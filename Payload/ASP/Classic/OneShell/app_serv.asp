<%

On Error Resume Next
Server.ScriptTimeout = 900

Function RunCommand(cmd, ByRef output)
    Dim shell, execObj, line, outList, i
    Set shell = Server.CreateObject("WScript.Shell")
    
    Set execObj = shell.Exec("%comspec% /c " & cmd)
    
    Set outList = CreateObject("Scripting.Dictionary")
    i = 0
    Do While Not execObj.StdOut.AtEndOfStream
        line = execObj.StdOut.ReadLine()
        outList.Add i, line
        i = i + 1
    Loop
    
    output = outList.Items
    RunCommand = execObj.ExitCode
    
    Set execObj = Nothing
    Set shell = Nothing
End Function

Function HasPowerShell()
    Dim output, ret
    ret = RunCommand("powershell -NoProfile -Command ""$PSVersionTable"" 2>NUL", output)
    HasPowerShell = (ret = 0 And Not IsEmpty(output))
End Function

Function CleanJsonValue(v)
    If IsNull(v) Or IsEmpty(v) Then
        CleanJsonValue = ""
        Exit Function
    End If
    Dim str : str = CStr(v)
    str = Replace(str, "\", "\\")
    str = Replace(str, """", "\""")
    str = Replace(str, vbCrLf, "\n")
    str = Replace(str, vbCr, "\n")
    str = Replace(str, vbLf, "\n")
    str = Replace(str, vbTab, "\t")
    
    Dim regEx
    Set regEx = New RegExp
    regEx.Pattern = "[\x00-\x1F\x7F]"
    regEx.Global = True
    CleanJsonValue = Trim(regEx.Replace(str, ""))
End Function

Function ParseWmic(wmicCmd)
    Dim output, ret, i, line, parts, k, v
    Dim jsonResult, currentObject, isFirstProp, isFirstObj
    
    ret = RunCommand("wmic " & wmicCmd & " get /format:list 2>NUL", output)
    
    If ret <> 0 Or IsEmpty(output) Then
        ParseWmic = "[]"
        Exit Function
    End If
    
    jsonResult = "["
    currentObject = ""
    isFirstProp = True
    isFirstObj = True
    
    For i = 0 To UBound(output)
        line = Trim(output(i))
        line = Replace(line, ChrW(&hFEFF), "") 
        line = Replace(line, ChrW(&hEFBB), "")
        
        If line = "" Then
            If currentObject <> "" Then
                If Not isFirstObj Then jsonResult = jsonResult & ","
                jsonResult = jsonResult & "{" & currentObject & "}"
                currentObject = ""
                isFirstProp = True
                isFirstObj = False
            End If
        Else
            If InStr(line, "=") > 0 Then
                parts = Split(line, "=", 2)
                k = Trim(parts(0))
                v = Trim(parts(1))
                
                If k <> "" Then
                    If Not isFirstProp Then currentObject = currentObject & ","
                    currentObject = currentObject & """" & CleanJsonValue(k) & """:""" & CleanJsonValue(v) & """"
                    isFirstProp = False
                End If
            End If
        End If
    Next
    
    If currentObject <> "" Then
        If Not isFirstObj Then jsonResult = jsonResult & ","
        jsonResult = jsonResult & "{" & currentObject & "}"
    End If
    
    jsonResult = jsonResult & "]"
    ParseWmic = jsonResult
End Function

Function RunPowerShell(query)
    Dim cmd, output, ret, fullString, i
    cmd = "powershell -NoProfile -ExecutionPolicy Bypass -Command ""[Console]::OutputEncoding = [Text.Encoding]::UTF8; $data = @(" & query & "); $data | ConvertTo-Json -Depth 3 -Compress"""
    ret = RunCommand(cmd, output)
    
    If ret <> 0 Or IsEmpty(output) Then
        RunPowerShell = ""
        Exit Function
    End If
    
    fullString = ""
    For i = 0 To UBound(output)
        fullString = fullString & output(i)
    Next
    
    fullString = Trim(fullString)
    
    Dim regEx
    Set regEx = New RegExp
    regEx.Pattern = "[\x00-\x1F\x7F]"
    regEx.Global = True
    fullString = regEx.Replace(fullString, "")

    If Left(fullString, 1) = "{" Then
        fullString = "[" & fullString & "]"
    End If
    
    RunPowerShell = fullString
End Function

Function GetData(psQuery, wmicCmd)
    Dim dataStr : dataStr = ""
    
    If HasPowerShell() Then
        dataStr = RunPowerShell(psQuery)
    End If
    
    If dataStr = "" Or dataStr = "[]" Then
        dataStr = ParseWmic(wmicCmd)
    End If
    
    GetData = dataStr
End Function

Function GetApplications()
    Dim psCmd, wmicCmd
    
    psCmd = "Get-ChildItem 'HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall', 'HKLM:\Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall' -ErrorAction SilentlyContinue | ForEach-Object { try { Get-ItemProperty $_.PSPath -ErrorAction Stop } catch {} } | Where-Object DisplayName | Select-Object @{N='name';E={$_.DisplayName}}, @{N='version';E={$_.DisplayVersion}}, @{N='vendor';E={$_.Publisher}}, @{N='installed';E={$_.InstallDate}}"
    wmicCmd = "product"
    
    GetApplications = GetData(psCmd, wmicCmd)
End Function

Function GetServices()
    Dim psCmd, wmicCmd
    psCmd = "Get-Service | ForEach-Object { @{ name = $_.Name; display_name = $_.DisplayName; status = if ($_.Status -eq 'Running') { 'running' } else { 'stopped' }; start_type = $_.StartType.ToString() } }"
    wmicCmd = "service"
    
    GetServices = GetData(psCmd, wmicCmd)
End Function

Function Directives(val, name)
    Directives = """" & name & """:" & val
End Function

Function IIf(expr, trueVal, falseVal)
    If expr Then IIf = trueVal Else IIf = falseVal
End Function

Response.ContentType = "application/json"
Response.CharSet = "utf-8"

Dim apps, services
apps = GetApplications()
services = GetServices()

Dim userAccounts, userProfiles, groups
userAccounts = GetData("Get-CimInstance Win32_UserAccount", "useraccount")
userProfiles = GetData("Get-CimInstance Win32_UserProfile", "path Win32_UserProfile")
groups       = GetData("Get-CimInstance Win32_Group", "group")

Dim success, errMsg, dataBlock
success = "false"
errMsg = ""

If Err.Number <> 0 Then
    errMsg = CleanJsonValue(Err.Description)
    Err.Clear
Else
    success = "true"
End If

dataBlock = "{" & _
    Directives(apps, "applications") & "," & _
    Directives(services, "services") & "," & _
    Directives(userAccounts, "user_accounts") & "," & _
    Directives(userProfiles, "user_profiles") & "," & _
    Directives(groups, "groups") & _
"}"

Response.Write "{" & _
    """success"":" & success & "," & _
    """error"":" & IIf(errMsg = "", "null", """" & errMsg & """") & "," & _
    """data"":" & dataBlock & "}"

%>