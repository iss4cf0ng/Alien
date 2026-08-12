using System;
using System.Text;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Web.Script.Serialization;

public class payload
{
    public class EgressResult
    {
        public string target { get; set; }
        public string status { get; set; }
        public string protocol { get; set; }
        public double latency { get; set; }
        public string reason { get; set; }
    }

    public string Execute(object param)
    {
        List<string> targets = new List<string>();

        string jsonConfig = param != null ? param.ToString() : "";
        try
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            Dictionary<string, object> config = serializer.Deserialize<Dictionary<string, object>>(jsonConfig);
            if (config != null && config.ContainsKey("targets"))
            {
                object[] rawTargets = (object[])config["targets"];
                foreach (var t in rawTargets)
                {
                    if (t != null) targets.Add(t.ToString());
                }
            }
        }
        catch
        {
            targets.Add("8.8.8.8:53");
        }

        if (targets.Count == 0)
        {
            targets.Add("8.8.8.8:53");
        }

        List<EgressResult> results = new List<EgressResult>();

        foreach (string target in targets)
        {
            string trimmedTarget = target.Trim();
            if (string.IsNullOrEmpty(trimmedTarget)) continue;

            string[] parts = trimmedTarget.Split(':');
            string host = parts[0];
            int port = 80;
            if (parts.Length > 1)
            {
                int.TryParse(parts[1], out port);
            }

            string status = "closed";
            string reason = "Connection timeout or filtered";
            double latency = 0;
            string protocol = (port == 443 ? "HTTPS/TCP" : (port == 53 ? "DNS/UDP-TCP" : "TCP"));

            DateTime startTime = DateTime.Now;

            try
            {
                using (TcpClient client = new TcpClient())
                {
                    IAsyncResult result = client.BeginConnect(host, port, null, null);
                    bool success = result.AsyncWaitHandle.WaitOne(1500, true);

                    if (success && client.Connected)
                    {
                        client.EndConnect(result);
                        latency = Math.Round((DateTime.Now - startTime).TotalMilliseconds, 2);
                        status = "open";
                        reason = "Connected successfully";
                    }
                    else
                    {
                        reason = "Connection timeout or filtered";
                    }
                }
            }
            catch (Exception ex)
            {
                reason = ex.Message;
            }

            results.Add(new EgressResult
            {
                target = trimmedTarget,
                status = status,
                protocol = protocol,
                latency = latency,
                reason = reason
            });
        }

        return fnBuildJsonArray(results);
    }

    private string fnBuildJsonArray(List<EgressResult> list)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("[");
        for (int i = 0; i < list.Count; i++)
        {
            EgressResult r = list[i];
            sb.Append("{");
            sb.Append("\"target\":\"").Append(EscapeJson(r.target)).Append("\",");
            sb.Append("\"status\":\"").Append(EscapeJson(r.status)).Append("\",");
            sb.Append("\"protocol\":\"").Append(EscapeJson(r.protocol)).Append("\",");
            sb.Append("\"latency\":").Append(r.latency).Append(",");
            sb.Append("\"reason\":\"").Append(EscapeJson(r.reason)).Append("\"");
            sb.Append("}");

            if (i < list.Count - 1)
            {
                sb.Append(",");
            }
        }
        sb.Append("]");
        return sb.ToString();
    }

    private string EscapeJson(string str)
    {
        if (string.IsNullOrEmpty(str)) return "";
        return str.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", "");
    }
}