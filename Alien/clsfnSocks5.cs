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
    public class clsfnSocks5
    {
        private clsWeb m_web { get; init; }
        private int m_nSessionId { get; set; }
        private TcpListener m_listener { get; set; }
        
        public bool m_bIsRunning = false;

        public event Action<string, int> OnConnected;
        public event Action<string, int> OnDisconnected;

        public clsfnSocks5(clsWeb web)
        {
            m_web = web;
            m_nSessionId = new Random().Next(1000, 9999);
        }

        public async Task<byte[]?> fnSendData(string szIP, int nPort, byte[] abData)
        {
            try
            {
                string szResp = await m_web.fnszSendPayload("proxy", new string[] { "forward", m_nSessionId.ToString(), szIP, nPort.ToString(), Convert.ToBase64String(abData) });
                if (string.IsNullOrEmpty(szResp)) return null;

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
            catch { }
            return null;
        }

        public async Task fnStartAsync(int nPort)
        {
            if (m_bIsRunning)
                return;

            try
            {
                m_listener = new TcpListener(IPAddress.Any, nPort);
                m_listener.Start();
                m_bIsRunning = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                while (m_bIsRunning)
                {
                    TcpClient user_client = await m_listener.AcceptTcpClientAsync().ConfigureAwait(false);
                    _ = Task.Run(() => fnHandleClientAsync(user_client));
                }
            }
            catch
            {

            }
        }

        public void fnStop()
        {
            m_bIsRunning = false;
            try
            {
                m_listener?.Stop();
            }
            catch { }
        }

        private async Task fnHandleClientAsync(TcpClient client)
        {
            client.NoDelay = true;

            string szTargetIP = "Unknown";
            int nPort = 0;
            bool bIsEventTriggered = false;

            try
            {
                using (client)
                using (NetworkStream ns = client.GetStream())
                {
                    byte[] hdr = new byte[2];
                    int nLen = await ns.ReadAsync(hdr, 0, 2).ConfigureAwait(false);
                    if (nLen < 2 || hdr[0] != 0x05)
                        return;

                    int nMethods = hdr[1];
                    byte[] abMethods = new byte[nMethods];
                    await ns.ReadAsync(abMethods, 0, nMethods).ConfigureAwait(false);

                    await ns.WriteAsync(new byte[] { 0x05, 0x00 }, 0, 2).ConfigureAwait(false);
                    await ns.FlushAsync().ConfigureAwait(false);

                    byte[] hdr2 = new byte[4];
                    nLen = await ns.ReadAsync(hdr2, 0, 4).ConfigureAwait(false);
                    if (nLen < 4 || hdr2[1] != 0x01)
                        return;

                    byte addressType = hdr2[3];

                    if (addressType == 0x01) // IPv4
                    {
                        byte[] abAddr = new byte[4];
                        await ns.ReadAsync(abAddr, 0, 4).ConfigureAwait(false);
                        szTargetIP = $"{abAddr[0]}.{abAddr[1]}.{abAddr[2]}.{abAddr[3]}";
                    }
                    else if (addressType == 0x03) // Domain Name
                    {
                        int nDomainLen = ns.ReadByte();
                        if (nDomainLen <= 0)
                            return;

                        byte[] abDomain = new byte[nDomainLen];
                        await ns.ReadAsync(abDomain, 0, nDomainLen).ConfigureAwait(false);
                        szTargetIP = Encoding.ASCII.GetString(abDomain);
                    }
                    else
                    {
                        return;
                    }

                    byte[] abPort = new byte[2];
                    await ns.ReadAsync(abPort, 0, 2).ConfigureAwait(false);
                    nPort = (abPort[0] << 8) | abPort[1];

                    OnConnected?.Invoke(szTargetIP, nPort);
                    bIsEventTriggered = true;

                    byte[] abSuccessResp = { 0x05, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
                    await ns.WriteAsync(abSuccessResp, 0, abSuccessResp.Length).ConfigureAwait(false);
                    await ns.FlushAsync().ConfigureAwait(false);

                    byte[] abPayloadBuf = new byte[8192];
                    while (m_bIsRunning && client.Connected)
                    {
                        int nPayloadLen = await ns.ReadAsync(abPayloadBuf, 0, abPayloadBuf.Length).ConfigureAwait(false);
                        if (nPayloadLen <= 0)
                            break;

                        byte[] abReqData = new byte[nPayloadLen];
                        Buffer.BlockCopy(abPayloadBuf, 0, abReqData, 0, nPayloadLen);

                        byte[]? abRespData = await fnSendData(szTargetIP, nPort, abReqData).ConfigureAwait(false);

                        if (abRespData != null && abRespData.Length > 0)
                        {
                            await ns.WriteAsync(abRespData, 0, abRespData.Length).ConfigureAwait(false);
                            await ns.FlushAsync().ConfigureAwait(false);
                        }
                    }
                }
            }
            catch
            {
                
            }
            finally
            {
                if (bIsEventTriggered)
                {
                    OnDisconnected?.Invoke(szTargetIP, nPort);
                }
            }
        }
    }
}
