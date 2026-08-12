<%

try {
    var psi = new System.Diagnostics.ProcessStartInfo();
    psi.FileName = "tasklist.exe";
    psi.Arguments = "/NH /FO CSV";
    psi.RedirectStandardOutput = true;
    psi.UseShellExecute = false;
    psi.CreateNoWindow = true;

    var p = System.Diagnostics.Process.Start(psi);
    var output = p.StandardOutput.ReadToEnd();
    p.WaitForExit();

    var processes = [];
    var lines = output.Split(new String[] { "\r\n", "\n" }, System.StringSplitOptions.RemoveEmptyEntries);

    for (var i = 0; i < lines.length; i++) {
        var line = lines[i];
        if (line.length > 0) {
            var firstComma = line.indexOf(',');
            if (firstComma > 0) {
                var procName = line.substring(0, firstComma).replace(/"/g, "").trim();
                if (procName.length > 0) {
                    processes.push(procName);
                }
            }
        }
    }

    var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
    Response.Write(serializer.Serialize(processes));

} catch (e) {
    var errSerializer = new System.Web.Script.Serialization.JavaScriptSerializer();
    Response.Write(errSerializer.Serialize({ "error": e.message }));
}

%>