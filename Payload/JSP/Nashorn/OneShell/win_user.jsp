<%

// session_auditor.java (高階會話與帳戶審計引擎 - 雙重降級防禦版)

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
    // 1. 核心路由：為 6 大核心指標分發 PowerShell 與 WMIC 備用參數
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

// 直接將最終結果以漂亮的 JSON 格式回傳給 C# 端
Echo(JSON.stringify(responseJson));

// ────────────────────────────────────────────────────────
// 🛠️ 雙重策略核心業務邏輯
// ────────────────────────────────────────────────────────

function getData(psQuery, wmicClass) {
    // 策略 A：嘗試呼叫 PowerShell 獲取高品質 JSON
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
    
    // 策略 B：若不支援或被攔截，降級使用 WMIC 命令行解析
    return parseWmic(wmicClass);
}

function hasPowerShell() {
    try {
        // 利用 ProcessBuilder 測試 powershell 是否可用
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
        // 🔥 完美對齊你的 PHP 管道指令：將資料轉為壓縮 JSON 輸出
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
        if (jsonStr === "") return [];
        
        // 解析產出的 JSON
        var parsed = JSON.parse(jsonStr);
        if (Array.isArray(parsed)) {
            return parsed;
        } else {
            return [parsed]; // 確保格式永遠為陣列，對齊 PHP
        }
    } catch (e) {
        return [];
    }
}

function parseWmic(wmicClass) {
    var rows = [];
    try {
        // 降級方案：使用 wmic 輸出 list 格式
        var pb = new java.lang.ProcessBuilder(["cmd.exe", "/c", "wmic path " + wmicClass + " get /format:list"]);
        pb.redirectErrorStream(true);
        var process = pb.start();
        
        var is = process.getInputStream();
        // wmic 在中文 Windows 通常輸出 MS950 或者是帶有 BOM 的 UTF-16
        // 使用 BufferedReader 讀取，並交由 Java String 自動相容
        var reader = new java.io.BufferedReader(new java.io.InputStreamReader(is, "MS950"));
        var line = "";
        var current = {};
        
        while ((line = reader.readLine()) != null) {
            // 清理不可見字元與多餘空格 (複刻 clean_line 與 clean_value)
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
            
            // 剔除控制字元
            k = k.replace(/[\x00-\x1F\x7F-\x9F]/g, "");
            v = v.replace(/[\x00-\x1F\x7F-\x9F]/g, "");
            
            if (k === "") continue;
            current[k] = v;
        }
        
        if (Object.keys(current).length > 0) {
            rows.push(current);
        }
    } catch (e) {
        // 靜默失敗，回傳空陣列
    }
    return rows;
}

function flattenObject(item) {
    var out = {};
    for (var k in item) {
        if (item.hasOwnProperty(k)) {
            var v = item[k];
            if (v !== null && typeof v === 'object') {
                // 如果是巢狀物件，安全序列化為 JSON 字串
                out[k] = JSON.stringify(v);
            } else {
                // 純文字直接清洗掉可能干擾顯示的怪異控制字元
                var strV = (v === null) ? "" : String(v);
                out[k] = strV.replace(/[\x00-\x1F\x7F-\x9F]/g, "").trim();
            }
        }
    }
    return out;
}

%>