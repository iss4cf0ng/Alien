import java.io.BufferedReader;
import java.io.InputStreamReader;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

public class payload {
    public payload() {
        // nothing to do!
    }

    public String Execute(Object param) throws Exception {
        List<String> processes = new ArrayList<>();
        
        ProcessBuilder processBuilder = new ProcessBuilder("cmd.exe", "/c", "tasklist /NH /FO CSV");
        processBuilder.redirectErrorStream(true);
        Process process = processBuilder.start();
        
        try (BufferedReader reader = new BufferedReader(
                new InputStreamReader(process.getInputStream(), StandardCharsets.UTF_8))) {
            
            String line;
            while ((line = reader.readLine()) != null) {
                String trimmedLine = line.trim();
                if (trimmedLine.isEmpty()) {
                    continue;
                }
                
                String processName = fnParseCsvFirstColumn(trimmedLine);
                
                if (!processName.isEmpty()) {
                    processes.add(processName);
                }
            }
        }
        process.waitFor();

        return fnBuildJsonArray(processes);
    }

    private String fnParseCsvFirstColumn(String csvLine) {
        if (csvLine.startsWith("\"")) {
            int nextQuote = csvLine.indexOf("\"", 1);
            if (nextQuote != -1) {
                return csvLine.substring(1, nextQuote).trim();
            }
        } else {
            String[] parts = csvLine.split(",");
            if (parts.length > 0) {
                return parts[0].trim();
            }
        }
        return "";
    }

    private String fnBuildJsonArray(List<String> list) {
        StringBuilder sb = new StringBuilder();
        sb.append("[");
        for (int i = 0; i < list.size(); i++) {
            String escaped = list.get(i).replace("\\", "\\\\").replace("\"", "\\\"");
            sb.append("\"").append(escaped).append("\"");
            if (i < list.size() - 1) {
                sb.append(",");
            }
        }
        sb.append("]");
        return sb.toString();
    }

    private String fnGetJsonValue(String json, String key) {
        Pattern pattern = Pattern.compile("\"" + key + "\"\\s*:\\s*\"(.*?)\"");
        Matcher matcher = pattern.matcher(json);
        if (matcher.find()) {
            return matcher.group(1);
        }
        
        pattern = Pattern.compile("\"" + key + "\"\\s*:\\s*([^,\\}\\]]+)");
        matcher = pattern.matcher(json);
        if (matcher.find()) {
            return matcher.group(1).trim();
        }

        return "";
    }
}
