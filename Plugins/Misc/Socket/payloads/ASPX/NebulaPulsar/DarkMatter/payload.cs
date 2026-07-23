using System;
using System.Text;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public class payload
{
    public string Execute(object param)
    {
        try
        {
            if (!(param is Dictionary<string, object> mapParam))
                return "ERROR: Invalid parameter type. Expected Dictionary.";

            if (!mapParam.TryGetValue("json", out var jsonValue) || string.IsNullOrEmpty(jsonValue?.ToString()))
                return "ERROR: JSON data is empty.";

            string szJson = jsonValue.ToString();
            
            string host = fnExtractJsonValue(szJson, "host");
            string portStr = fnExtractJsonValue(szJson, "port");
            string dataType = fnExtractJsonValue(szJson, "type");
            string rawData = fnExtractJsonValue(szJson, "data");

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(portStr))
            {
                return "[-] ERROR: Missing target host or port.";
            }

            int port = int.Parse(portStr);
            byte[] sendBuffer;
            if (dataType.ToLower() == "hex")
            {
                sendBuffer = fnHexStringToByteArray(rawData);
            }
            else
            {
                string unescapedData = Regex.Unescape(rawData);
                sendBuffer = Encoding.UTF8.GetBytes(unescapedData);
            }

            using (TcpClient client = new TcpClient())
            {
                var result = client.BeginConnect(host, port, null, null);
                var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3));
                
                if (!success)
                {
                    return "[-] ERROR: Connection Timeout (3000ms).";
                }
                client.EndConnect(result);

                using (NetworkStream stream = client.GetStream())
                {
                    stream.Write(sendBuffer, 0, sendBuffer.Length);

                    byte[] receiveBuffer = new byte[4096];
                    client.ReceiveTimeout = 3000;
                    
                    int bytesRead = stream.Read(receiveBuffer, 0, receiveBuffer.Length);
                    if (bytesRead > 0)
                    {
                        string responseText = Encoding.UTF8.GetString(receiveBuffer, 0, bytesRead);
                        return "[+] RESPONSE:\n" + responseText;
                    }
                    else
                    {
                        return "[+] SUCCESS: Packet transmitted, but no data returned from host.";
                    }
                }
            }
        }
        catch (Exception ex)
        {
            return "[-] EXCEPTION: " + ex.Message;
        }
    }

    private string fnExtractJsonValue(string json, string key)
    {
        string pattern = "\"" + key + "\"\\s*:\\s*\"?([^\",}]+)\"?";
        Match match = Regex.Match(json, pattern);
        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }
        return string.Empty;
    }

    private byte[] fnHexStringToByteArray(string szHexStr)
    {
        if (string.IsNullOrEmpty(szHexStr) || string.IsNullOrEmpty(szHexStr.Trim()))
        {
            return new byte[0];
        }

        string szClean = Regex.Replace(szHexStr.ToLower(), @"[\\,ox\s\r\n]", "");

        int nLen = szClean.Length;
        if (nLen % 2 != 0)
        {
            szClean = szClean + "0";
            nLen++;
        }

        byte[] abResult = new byte[nLen / 2];
        for (int i = 0; i < nLen; i += 2)
        {
            string szByteHex = szClean.Substring(i, 2);
            abResult[i / 2] = Convert.ToByte(szByteHex, 16);
        }

        return abResult;
    }
}
