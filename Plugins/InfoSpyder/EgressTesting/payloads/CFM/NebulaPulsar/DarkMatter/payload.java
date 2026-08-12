import java.net.InetSocketAddress;
import java.net.Socket;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.List;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

public class payload {
    public payload() {
        // nothing to do!
    }

    public String Execute(Object param) throws Exception {
        List<String> targets = new ArrayList<>();
        
        String jsonConfig = param != null ? param.toString() : "";
        
        Pattern targetPattern = Pattern.compile("\"([^\"]+?)\"");
        Matcher matcher = targetPattern.matcher(jsonConfig);
        boolean foundTargetsKey = false;
        
        while (matcher.find()) {
            String val = matcher.group(1);
            if (foundTargetsKey) {
                if (!val.equals("targets")) {
                    targets.add(val);
                }
            } else if (val.equals("targets")) {
                foundTargetsKey = true;
            }
        }
        
        if (targets.isEmpty()) {
            targets.add("8.8.8.8:53");
        }

        StringBuilder sb = new StringBuilder();
        sb.append("[");

        for (int i = 0; i < targets.size(); i++) {
            String target = targets.get(i);
            String trimmedTarget = target.trim();
            if (trimmedTarget.isEmpty()) continue;

            String[] parts = trimmedTarget.split(":");
            String host = parts[0];
            int port = 80;
            if (parts.length > 1) {
                try {
                    port = Integer.parseInt(parts[1]);
                } catch (NumberFormatException e) {
                    port = 80;
                }
            }

            String status = "closed";
            String reason = "Connection timeout or filtered";
            double latency = 0;
            String protocol = (port == 443 ? "HTTPS/TCP" : (port == 53 ? "DNS/UDP-TCP" : "TCP"));

            long startTime = System.nanoTime();

            Socket socket = null;
            try {
                socket = new Socket();
                socket.connect(new InetSocketAddress(host, port), 1500);
                long endTime = System.nanoTime();
                latency = Math.round((endTime - startTime) / 1_000_000.0 * 100.0) / 100.0;
                status = "open";
                reason = "Connected successfully";
            } catch (Exception e) {
                reason = e.getMessage() != null ? e.getMessage() : "Connection timeout or filtered";
            } finally {
                if (socket != null) {
                    try {
                        socket.close();
                    } catch (Exception ignored) {}
                }
            }

            sb.append("{");
            sb.append("\"target\":\"").append(escapeJson(trimmedTarget)).append("\",");
            sb.append("\"status\":\"").append(escapeJson(status)).append("\",");
            sb.append("\"protocol\":\"").append(escapeJson(protocol)).append("\",");
            sb.append("\"latency\":").append(latency).append(",");
            sb.append("\"reason\":\"").append(escapeJson(reason)).append("\"");
            sb.append("}");

            if (i < targets.size() - 1) {
                sb.append(",");
            }
        }

        sb.append("]");
        return sb.toString();
    }

    private String escapeJson(String str) {
        if (str == null) return "";
        return str.replace("\\", "\\\\").replace("\"", "\\\"").replace("\r", "").replace("\n", "");
    }
}