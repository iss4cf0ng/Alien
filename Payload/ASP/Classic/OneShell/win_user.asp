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
    ret = RunCommand("powershell -Command ""Get-Host"" 2>NUL", output)
    HasPowerShell = (ret = 0)
End Function

' Escapes control characters, quotes, and backslashes for safe JSON output
Function CleanJsonValue(v)
    If IsNull(v) Then
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

Function ParseWmic(wmicClass)
    Dim output, ret, i, line, parts, k, v
    Dim jsonResult, currentObject, isFirstProp, isFirstObj
    
    ret = RunCommand("wmic path " & wmicClass & " get /format:list", output)
    
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
        line = Replace(line, ChrM(&hEFBBBF), "")
        
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
    
    ' Catch any trailing data block
    If currentObject <> "" Then
        If Not isFirstObj Then jsonResult = jsonResult & ","
        jsonResult = jsonResult & "{" & currentObject & "}"
    End If
    
    jsonResult = jsonResult & "]"
    ParseWmic = jsonResult
End Function

' Executes queries in powershell and pulls the compressed raw JSON string out directly
Function RunPowerShell(query)
    Dim cmd, output, ret, fullString, i
    cmd = "powershell -NoProfile -Command """ & query & " | ConvertTo-Json -Depth 3 -Compress"""
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
    
    ' Force output array encapsulation if PowerShell returns a single raw object response
    If Left(fullString, 1) = "{" Then
        fullString = "[" & fullString & "]"
    End If
    
    RunPowerShell = fullString
End Function

' Decision controller: Tries PowerShell, drops back to WMIC if failing or empty
Function GetData(psQuery, wmicClass)
    Dim dataStr : dataStr = ""
    
    If HasPowerShell() Then
        dataStr = RunPowerShell(psQuery)
    End If
    
    ' Fallback to WMIC manual parser if String is Empty
    If dataStr = "" Or dataStr = "[]" Then
        dataStr = ParseWmic(wmicClass)
    End If
    
    GetData = dataStr
End Function

Response.ContentType = "application/json"
Response.CharSet = "utf-8"

Dim success, errMsg, dataBlock
success = "false"
errMsg = ""

Dim userAccounts, userProfiles, groups, groupUsers, loggedOn, logonSession

userAccounts = GetData("Get-CimInstance Win32_UserAccount", "Win32_UserAccount")
userProfiles = GetData("Get-CimInstance Win32_UserProfile", "Win32_UserProfile")
groups = GetData("Get-CimInstance Win32_Group", "Win32_Group")
groupUsers = GetData("Get-CimInstance Win32_GroupUser", "Win32_GroupUser")
loggedOn = GetData("Get-CimInstance Win32_LoggedOnUser", "Win32_LoggedOnUser")
logonSession = GetData("Get-CimInstance Win32_LogonSession", "Win32_LogonSession")

If Err.Number <> 0 Then
    errMsg = CleanJsonValue(Err.Description)
    Err.Clear
Else
    success = "true"
End If

dataBlock = "{" & _
    """user_accounts"":" & userAccounts & "," & _
    Directives(userProfiles, "user_profiles") & "," & _
    """groups"":"       & groups       & "," & _
    """group_users"":"   & groupUsers   & "," & _
    """logged_on"":"     & loggedOn     & "," & _
    """logon_session"":" & logonSession & _
"}"

Function Directives(val, name)
    Directives = """" & name & """:" & val
End Function

Response.Write "{" & _
    """success"":" & success & "," & _
    """error"":" & IIf(errMsg = "", "null", """" & errMsg & """") & "," & _
    """data"":" & dataBlock & "}"

Function IIf(expr, trueVal, falseVal)
    If expr Then IIf = trueVal Else IIf = falseVal
End Function

%>