using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Alien
{
    public class clsfnLAN
    {
        private clsWeb m_web { get; init; }
        public bool m_bRunning { get; set; } = false;
        private CancellationTokenSource? m_cts;

        public Dictionary<string, List<int>> m_dicHost = new Dictionary<string, List<int>>();

        public clsfnLAN(clsWeb web)
        {
            m_web = web;
        }

        public async Task<string> fnInfo()
        {
            string szResp = await m_web.fnszSendPayload("lan_tools", new string[] { "info" });
            if (string.IsNullOrEmpty(szResp))
                throw new Exception("HTTP response is null or empty.");

            var json = JObject.Parse(szResp);
            string? szSubnet = json["subnet"]?.ToString();
            if (string.IsNullOrEmpty(szSubnet))
                throw new Exception("JSON deserialization was failed.");

            return szSubnet;
        }

        public async Task fnStart(List<string> lsIP, List<int> lnPort, Action<string, int> actCallback, Action actFinishCallBack, int nMaxThreads = 50)
        {
            if (m_bRunning)
                return;

            m_bRunning = true;
            m_cts = new CancellationTokenSource();
            var token = m_cts.Token;

            using (var semaphore = new SemaphoreSlim(nMaxThreads))
            {
                var tasks = new List<Task>();

                foreach (var ip in lsIP)
                {
                    if (!m_bRunning || token.IsCancellationRequested)
                        break;

                    foreach (int port in lnPort)
                    {
                        if (!m_bRunning || token.IsCancellationRequested)
                            break;

                        await semaphore.WaitAsync(token);

                        var task = Task.Run(async () =>
                        {
                            try
                            {
                                if (m_bRunning && !token.IsCancellationRequested)
                                {
                                    bool isOpen = await fnCheck(ip, port);
                                    if (isOpen && m_bRunning && !token.IsCancellationRequested)
                                    {
                                        actCallback?.Invoke(ip, port);
                                    }
                                }
                            }
                            catch
                            {

                            }
                            finally
                            {
                                semaphore.Release();
                            }
                        }, token);

                        tasks.Add(task);
                    }
                }

                await Task.WhenAll(tasks);
            }

            m_bRunning = false;
            actFinishCallBack?.Invoke();
        }

        public void fnStop()
        {
            m_bRunning = false;
            m_cts?.Cancel();
        }

        private async Task<bool> fnCheck(string szIP, int nPort)
        {
            try
            {
                string szResp = await m_web.fnszSendPayload("lan_tools", new string[] { "check", szIP, nPort.ToString() });
                //MessageBox.Show(szResp);
                if (string.IsNullOrEmpty(szResp))
                    return false;

                var json = JObject.Parse(szResp);
                if (json["open"] != null)
                {
                    return (bool)json["open"];
                }
            }
            catch
            {

            }
            return false;
        }

        public static List<string> fnParseIPRange(string szInput)
        {
            var lsResult = new List<string>();
            szInput = szInput.Trim();

            try
            {
                if (szInput.Contains("/"))
                {
                    string[] parts = szInput.Split('/');
                    if (parts.Length == 2 && IPAddress.TryParse(parts[0], out IPAddress ipAddress) && int.TryParse(parts[1], out int cidr))
                    {
                        if (cidr >= 0 && cidr <= 32)
                        {
                            uint ip = BitConverter.ToUInt32(ipAddress.GetAddressBytes().Reverse().ToArray(), 0);
                            uint mask = cidr == 0 ? 0 : uint.MaxValue << (32 - cidr);

                            uint startIp = ip & mask;
                            uint endIp = ip | ~mask;

                            for (uint i = startIp; i <= endIp; i++)
                            {
                                byte[] bytes = BitConverter.GetBytes(i).Reverse().ToArray();
                                lsResult.Add(new IPAddress(bytes).ToString());
                            }
                        }
                    }
                }
                else if (szInput.Contains("-"))
                {
                    string[] parts = szInput.Split('-');
                    if (parts.Length == 2)
                    {
                        string startIpStr = parts[0].Trim();
                        string endPart = parts[1].Trim();

                        if (IPAddress.TryParse(startIpStr, out IPAddress startIpAddr))
                        {
                            string[] ipSegments = startIpStr.Split('.');

                            if (IPAddress.TryParse(endPart, out IPAddress endIpAddr))
                            {
                                uint start = BitConverter.ToUInt32(startIpAddr.GetAddressBytes().Reverse().ToArray(), 0);
                                uint end = BitConverter.ToUInt32(endIpAddr.GetAddressBytes().Reverse().ToArray(), 0);
                                for (uint i = start; i <= end; i++)
                                {
                                    byte[] bytes = BitConverter.GetBytes(i).Reverse().ToArray();
                                    lsResult.Add(new IPAddress(bytes).ToString());
                                }
                            }
                            else if (int.TryParse(endPart, out int nEndLastByte) && nEndLastByte >= 0 && nEndLastByte <= 255)
                            {
                                int nStartLastByte = int.Parse(ipSegments[3]);
                                string szBaseNet = $"{ipSegments[0]}.{ipSegments[1]}.{ipSegments[2]}.";

                                for (int i = nStartLastByte; i <= nEndLastByte; i++)
                                {
                                    lsResult.Add(szBaseNet + i);
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (IPAddress.TryParse(szInput, out _))
                    {
                        lsResult.Add(szInput);
                    }
                }
            }
            catch
            {
                
            }

            return lsResult;
        }

        public static List<int> fnParsePortList(string input)
        {
            var ports = new List<int>();

            foreach (var item in input.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var part = item.Trim();

                if (part.Contains('-'))
                {
                    var range = part.Split('-', StringSplitOptions.RemoveEmptyEntries);

                    if (range.Length != 2 || !int.TryParse(range[0], out int start) || !int.TryParse(range[1], out int end))
                        continue;

                    if (start > end)
                        (start, end) = (end, start);

                    for (int i = start; i <= end; i++)
                    {
                        ports.Add(i);
                    }
                }
                else
                {
                    if (!int.TryParse(part, out int port))
                        continue;

                    ports.Add(port);
                }
            }

            return ports.Distinct().OrderBy(p => p).ToList();
        }
    }
}
