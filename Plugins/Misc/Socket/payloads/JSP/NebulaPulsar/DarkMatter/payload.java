import java.io.*;
import java.net.InetSocketAddress;
import java.net.Socket;
import java.nio.charset.StandardCharsets;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

public class payload {
    public payload() {}

    private byte[] fnHexStringToByteArray(String szHexStr) {
        if (szHexStr == null || szHexStr.trim().isEmpty()) {
            return new byte[0];
        }

        String szClean = szHexStr.toLowerCase().replaceAll("[\\\\,ox\\s\\r\\n]", "");

        int nLen = szClean.length();
        if (nLen % 2 != 0) {
            szClean = szClean + "0";
            nLen++;
        }

        byte[] abResult = new byte[nLen / 2];
        for (int i = 0; i < nLen; i += 2) {
            String szByteHex = szClean.substring(i, i + 2);
            abResult[i / 2] = (byte) Integer.parseInt(szByteHex, 16);
        }

        return abResult;
    }

    private String fnExtractJsonValue(String json, String key) {
        String pattern = "\"" + key + "\"\\s*:\\s*\"?([^\",}]+)\"?";
        Pattern r = Pattern.compile(pattern);
        Matcher m = r.matcher(json);
        if (m.find()) {
            return m.group(1).trim();
        }
        return "";
    }

    private String fnUnescapeData(String str) {
        if (str == null || str.isEmpty()) return "";
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < str.length(); i++) {
            char c = str.charAt(i);
            if (c == '\\' && i + 1 < str.length()) {
                char next = str.charAt(i + 1);
                if (next == 'n') { sb.append('\n'); i++; }
                else if (next == 'r') { sb.append('\r'); i++; }
                else if (next == 't') { sb.append('\t'); i++; }
                else if (next == '\\') { sb.append('\\'); i++; }
                else if (next == '"') { sb.append('"'); i++; }
                else { sb.append(c); }
            } else {
                sb.append(c);
            }
        }
        return sb.toString();
    }

    public String Execute(Object param) throws Exception {
        try {
            if (!(param instanceof java.util.Map)) {
                return "ERROR: Invalid parameter type. Expected Map.";
            }
            java.util.Map<?, ?> mapParam = (java.util.Map<?, ?>) param;
            String szJson = (String) mapParam.get("json");

            if (szJson == null || szJson.isEmpty()) {
                return "ERROR: JSON data is empty.";
            }

            String host = fnExtractJsonValue(szJson, "host");
            String portStr = fnExtractJsonValue(szJson, "port");
            String dataType = fnExtractJsonValue(szJson, "type");
            String rawData = fnExtractJsonValue(szJson, "data");

            if (host.isEmpty() || portStr.isEmpty()) {
                return "[-] ERROR: Missing target host or port.";
            }

            int port = Integer.parseInt(portStr);
            byte[] sendBuffer;

            if (dataType.equalsIgnoreCase("hex")) {
                sendBuffer = fnHexStringToByteArray(rawData);
            } else {
                String unescapedData = fnUnescapeData(rawData);
                sendBuffer = unescapedData.getBytes(StandardCharsets.UTF_8);
            }

            Socket socket = new Socket();
            try {
                socket.connect(new InetSocketAddress(host, port), 3000);
                socket.setSoTimeout(3000);

                OutputStream out = socket.getOutputStream();
                InputStream in = socket.getInputStream();

                out.write(sendBuffer);
                out.flush();

                byte[] receiveBuffer = new byte[4096];
                int bytesRead = in.read(receiveBuffer);

                if (bytesRead > 0) {
                    String responseText = new String(receiveBuffer, 0, bytesRead, StandardCharsets.UTF_8);
                    return "[+] RESPONSE:\n" + responseText;
                } else {
                    return "[+] SUCCESS: Packet transmitted, but no data returned from host.";
                }
            } catch (java.net.SocketTimeoutException te) {
                return "[-] ERROR: Connection Timeout (3000ms).";
            } finally {
                try { socket.close(); } catch (Exception ignored) {}
            }
        } catch (Exception ex) {
            return "[-] EXCEPTION: " + ex.getMessage();
        }
    }
}