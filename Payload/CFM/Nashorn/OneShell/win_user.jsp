<%

var responseJson = {
    "success": false,
    "error": "",
    "data": {
        "user_accounts": [],
        "user_profiles": [],
        "groups": [],
        "group_users": [],
        "logged_on": [],
        "logon_session": []
    }
};

try {
    responseJson.data.user_accounts = getData("Get-CimInstance Win32_UserAccount", "Win32_UserAccount");
    responseJson.data.user_profiles = getData("Get-CimInstance Win32_UserProfile", "Win32_UserProfile");
    responseJson.data.groups        = getData("Get-CimInstance Win32_Group", "Win32_Group");
    responseJson.data.group_users   = getData("Get-CimInstance Win32_GroupUser", "Win32_GroupUser");
    responseJson.data.logged_on     = getData("Get-CimInstance Win32_LoggedOnUser", "Win32_LoggedOnUser");
    responseJson.data.logon_session = getData("Get-CimInstance Win32_LogonSession", "Win32_LogonSession");

    responseJson.success = true;
} catch (e) {
    responseJson.success = false;
    responseJson.error = e.message;
}

Echo(JSON.stringify(responseJson));

function getData(psQuery, wmicClass) {
    if (hasPowerShell()) {
        var psResult = runPowerShell(psQuery);
        if (psResult && psResult.length > 0) {
            var cleanList = [];
            for (var i = 0; i < psResult.length; i++) {
                cleanList.push(flattenObject(psResult[i]));
            }
            return cleanList;
        }
    }
    
    return parseWmic(wmicClass);
}

function hasPowerShell() {
    try {
        var pb = new java.lang.ProcessBuilder(["powershell.exe", "-NoProfile", "-Command", "Get-Host"]);
        var process = pb.start();
        var exitCode = process.waitFor();
        return (exitCode === 0);
    } catch (e) {
        return false;
    }
}

function runPowerShell(query) {
    try {
        var fullCommand = query + " | ConvertTo-Json -Depth 3 -Compress";
        var pb = new java.lang.ProcessBuilder(["powershell.exe", "-NoProfile", "-Command", fullCommand]);
        pb.redirectErrorStream(true);
        var process = pb.start();
        
        var is = process.getInputStream();
        var reader = new java.io.BufferedReader(new java.io.InputStreamReader(is, "UTF-8"));
        var line = "";
        var sb = new java.lang.StringBuilder();
        
        while ((line = reader.readLine()) != null) {
            sb.append(line);
        }
        process.waitFor();
        
        var jsonStr = sb.toString().trim();
        if (jsonStr === "")
            return [];
        
        var parsed = JSON.parse(jsonStr);
        if (Array.isArray(parsed)) {
            return parsed;
        } else {
            return [parsed];
        }
    } catch (e) {
        return [];
    }
}

function parseWmic(wmicClass) {
    var rows = [];
    try {
        var pb = new java.lang.ProcessBuilder(["cmd.exe", "/c", "wmic path " + wmicClass + " get /format:list"]);
        pb.redirectErrorStream(true);
        var process = pb.start();
        
        var is = process.getInputStream();
        var reader = new java.io.BufferedReader(new java.io.InputStreamReader(is, "MS950"));
        var line = "";
        var current = {};
        
        while ((line = reader.readLine()) != null) {
            line = line.replace(/^\xEF\xBB\xBF|\r|\n/g, "").trim();
            if (line === "") {
                if (Object.keys(current).length > 0) {
                    rows.push(current);
                }
                current = {};
                continue;
            }
            
            var eqIdx = line.indexOf("=");
            if (eqIdx === -1) continue;
            
            var k = line.substring(0, eqIdx).trim();
            var v = line.substring(eqIdx + 1).trim();
            
            k = k.replace(/[\x00-\x1F\x7F-\x9F]/g, "");
            v = v.replace(/[\x00-\x1F\x7F-\x9F]/g, "");
            
            if (k === "") continue;
            current[k] = v;
        }
        
        if (Object.keys(current).length > 0) {
            rows.push(current);
        }
    } catch (e) {

    }
    return rows;
}

function flattenObject(item) {
    var out = {};
    for (var k in item) {
        if (item.hasOwnProperty(k)) {
            var v = item[k];
            if (v !== null && typeof v === 'object') {
                out[k] = JSON.stringify(v);
            } else {
                var strV = (v === null) ? "" : String(v);
                out[k] = strV.replace(/[\x00-\x1F\x7F-\x9F]/g, "").trim();
            }
        }
    }
    return out;
}

%>