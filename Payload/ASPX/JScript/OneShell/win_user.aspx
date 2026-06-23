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
    var output = runCommand("powershell -Command \"Get-Host\" 2>NUL");
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
    var output = runCommand("wmic path " + wmicClass + " get /format:list");
    
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
    var cmd = "powershell -NoProfile -Command \"" + query + " | ConvertTo-Json -Depth 3 -Compress\"";
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
    
    if (dataStr == "" || dataStr == "[]") {
        dataStr = parseWmic(wmicClass);
    }
    
    return dataStr;
}

function directives(val, name) {
    return "\"" + name + "\":" + val;
}

var success = "false";
var errMsg = "";
var dataBlock = "null";

try {
    var userAccounts = getData("Get-CimInstance Win32_UserAccount", "Win32_UserAccount");
    var userProfiles = getData("Get-CimInstance Win32_UserProfile", "Win32_UserProfile");
    var groups       = getData("Get-CimInstance Win32_Group", "Win32_Group");
    var groupUsers   = getData("Get-CimInstance Win32_GroupUser", "Win32_GroupUser");
    var loggedOn     = getData("Get-CimInstance Win32_LoggedOnUser", "Win32_LoggedOnUser");
    var logonSession = getData("Get-CimInstance Win32_LogonSession", "Win32_LogonSession");
    
    dataBlock = "{" +
        "\"user_accounts\":" + userAccounts + "," +
        directives(userProfiles, "user_profiles") + "," +
        "\"groups\":"         + groups       + "," +
        "\"group_users\":"     + groupUsers   + "," +
        "\"logged_on\":"       + loggedOn     + "," +
        "\"logon_session\":"   + logonSession +
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
Response.Write(finalResponse);

%>