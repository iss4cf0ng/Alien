<%

function safeTrim(str) {
    if (str == null)
        return "";

    return String(str).replace(/^\s+|\s+$/g, "");
}

function runCommand(cmd) {
    var outList = [];
    try {
        var shell = new System.Diagnostics.Process();
        shell.StartInfo.FileName = "cmd.exe";
        shell.StartInfo.Arguments = "/u /c " + cmd;
        shell.StartInfo.UseShellExecute = false;
        shell.StartInfo.RedirectStandardOutput = true;
        shell.StartInfo.CreateNoWindow = true;
        
        shell.Start();
        
        var reader = shell.StandardOutput;
        while (!reader.EndOfStream) {
            var line = reader.ReadLine();
            outList.push(String(line)); 
        }
        shell.WaitForExit();
    } catch(e) {
        
    }
    return outList;
}

function hasPowerShell() {
    var output = runCommand("powershell -NoProfile -Command \"$PSVersionTable\" 2>NUL");
    return (output.length > 0);
}

function cleanJsonValue(v) {
    if (v == null) return "";
    var str = String(v);
    
    str = str.replace(/\\/g, "\\\\")
             .replace(/"/g, "\\\"")
             .replace(/\n/g, "\\n")
             .replace(/\r/g, "\\r")
             .replace(/\t/g, "\\t");
             
    var regex = /[\x00-\x1F\x7F]/g;
    return safeTrim(str.replace(regex, ""));
}

function parseWmic(wmicClass) {
    var output = runCommand("wmic path " + wmicClass + " get /format:list 2>NUL");
    
    if (output.length == 0) {
        return "[]";
    }
    
    var jsonResult = "[";
    var currentObject = "";
    var isFirstProp = true;
    var isFirstObj = true;
    
    for (var i = 0; i < output.length; i++) {
        var line = safeTrim(String(output[i])); 
        
        line = line.replace(/\uFEFF/g, "");
        line = line.replace(/\uEFBBBF/g, "");
        
        if (line == "") {
            if (currentObject != "") {
                if (!isFirstObj) jsonResult += ",";
                jsonResult += "{" + currentObject + "}";
                currentObject = "";
                isFirstProp = true;
                isFirstObj = false;
            }
        } else {
            var eqIndex = line.indexOf("=");
            if (eqIndex > 0) {
                var k = safeTrim(line.substring(0, eqIndex));
                var v = safeTrim(line.substring(eqIndex + 1));
                
                if (k != "") {
                    if (!isFirstProp) currentObject += ",";
                    currentObject += "\"" + cleanJsonValue(k) + "\":\"" + cleanJsonValue(v) + "\"";
                    isFirstProp = false;
                }
            }
        }
    }
    
    if (currentObject != "") {
        if (!isFirstObj) jsonResult += ",";
        jsonResult += "{" + currentObject + "}";
    }
    
    jsonResult += "]";
    return jsonResult;
}

function runPowerShell(query) {
    var cmd = "powershell -NoProfile -ExecutionPolicy Bypass -Command \"[Console]::OutputEncoding = [Text.Encoding]::UTF8; $data = @(" + query + "); if ($data.Count -gt 0) { $data | ConvertTo-Json -Depth 3 -Compress } else { '[]' }\"";
    var output = runCommand(cmd);
    
    if (output.length == 0) {
        return "";
    }
    
    var fullString = "";
    for (var i = 0; i < output.length; i++) {
        fullString += String(output[i]); 
    }
    
    fullString = safeTrim(fullString);
    
    if (fullString.indexOf("{") == 0) {
        fullString = "[" + fullString + "]";
    }
    
    return fullString;
}

function getData(psQuery, wmicClass) {
    var dataStr = "";
    
    if (hasPowerShell()) {
        dataStr = runPowerShell(psQuery);
    }
    
    if (dataStr == "" || dataStr == "[]" || dataStr == "null") {
        dataStr = parseWmic(wmicClass);
    }
    
    return dataStr;
}

function directives(val, name) {
    return "\"" + name + "\":" + (val == "" ? "[]" : val);
}

var success = "false";
var errMsg = "";
var dataBlock = "null";

try {
    var psApps = "Get-ChildItem 'HKLM:\\Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall', 'HKLM:\\Software\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall' -ErrorAction SilentlyContinue | ForEach-Object { try { Get-ItemProperty $_.PSPath -ErrorAction Stop } catch {} } | Where-Object DisplayName | Select-Object @{N='name';E={$_.DisplayName}}, @{N='version';E={$_.DisplayVersion}}, @{N='vendor';E={$_.Publisher}}, @{N='installed';E={$_.InstallDate}}";
    var applications = getData(psApps, "Win32_Product");

    var psServ = "Get-Service -ErrorAction SilentlyContinue | ForEach-Object { @{ name = $_.Name; display_name = $_.DisplayName; status = if ($_.Status -eq 'Running') { 'running' } else { 'stopped' }; start_type = $_.StartType.ToString() } }";
    var services = getData(psServ, "Win32_Service");

    var userAccounts = getData("Get-CimInstance Win32_UserAccount", "Win32_UserAccount");
    var userProfiles = getData("Get-CimInstance Win32_UserProfile", "Win32_UserProfile");
    var groups       = getData("Get-CimInstance Win32_Group", "Win32_Group");
    var groupUsers   = getData("Get-CimInstance Win32_GroupUser", "Win32_GroupUser");
    var loggedOn     = getData("Get-CimInstance Win32_LoggedOnUser", "Win32_LoggedOnUser");
    var logonSession = getData("Get-CimInstance Win32_LogonSession", "Win32_LogonSession");
    
    dataBlock = "{" +
        directives(applications, "applications") + "," +
        directives(services, "services") + "," +
        directives(userAccounts, "user_accounts") + "," +
        directives(userProfiles, "user_profiles") + "," +
        directives(groups, "groups") + "," +
        directives(groupUsers, "group_users") + "," +
        directives(loggedOn, "logged_on") + "," +
        directives(logonSession, "logon_session") +
    "}";
    
    success = "true";
} catch(ex) {
    errMsg = cleanJsonValue(ex.message);
}

var finalResponse = "{" +
    "\"success\":" + success + "," +
    "\"error\":" + (errMsg == "" ? "null" : "\"" + errMsg + "\"") + "," +
    "\"data\":" + dataBlock +
"}";

Response.ContentType = "application/json";
Response.Charset = "utf-8";
Response.Write(finalResponse);

%>