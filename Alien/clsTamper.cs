using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Alien
{
    public class clsTamper : IDisposable
    {
        private readonly HttpClient m_client;
        private readonly string m_szPyServerUri;
        private Process? m_serverProcess = null;

        private string m_pyExecutable { get; init; }
        private string m_dirModule { get; init; }
        private string m_szServerPath { get; init; }

        public bool m_bIsReady { get; set; }

        public clsTamper(string szPySrvUri, string szPyExecutable, string szServer)
        {
            m_szPyServerUri = szPySrvUri;
            m_client = new HttpClient() { Timeout = TimeSpan.FromSeconds(2) };

            m_pyExecutable = szPyExecutable;
            m_dirModule = Path.Combine(Application.StartupPath, "Tamper");

            m_szServerPath = szServer;
            m_bIsReady = false;
        }

        public void Dispose()
        {
            m_client?.Dispose();
            if (m_serverProcess != null && !m_serverProcess.HasExited)
            {
                try 
                {
                    m_serverProcess.Kill();
                    m_serverProcess.Dispose(); 
                }
                catch
                {

                }
            }
        }

        public static string fnMergePayloadToOne(string szPayload, string[] asParams, enLanguage lang)
        {
            for (int i = 0; i < asParams.Length; i++)
            {
                //szPayload = szPayload.Replace($"z{i}", clsEzData.fnszStre2b64(asParams[i]));
                switch (lang)
                {
                    case enLanguage.PHP:
                        szPayload = szPayload.Replace($"$_POST['z{i}']", $"\"{clsEzData.fnszStre2b64(asParams[i])}\"");
                        break;
                }
            }

            return szPayload;
        }

        public async Task fnInitializeServerAsync()
        {
            if (await fnIsServerRunningAsync())
            {
                m_bIsReady = true;
                return;
            }

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"\"{m_szServerPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            };

            try
            {
                m_serverProcess = Process.Start(startInfo);

                int attempts = 0;
                while (!await fnIsServerRunningAsync())
                {
                    attempts++;
                    if (attempts > 30) // 3 seconds max wait
                    {
                        throw new Exception("Timeout waiting for Python server initialization.");
                    }

                    await Task.Delay(100);
                }

                m_bIsReady = true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed starting background bridge service: {ex.Message}", ex);
            }
        }

        private async Task<bool> fnIsServerRunningAsync()
        {
            try
            {
                using var response = await m_client.PostAsync($"{m_szPyServerUri}/obfuscate",
                    new StringContent("{}", Encoding.UTF8, "application/json"));
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string?> fnBuildPayload(string szScriptName, Dictionary<string, object> dicParams = null)
        {
            return await fnSendRequest("build", szScriptName, string.Empty, dicParams);
        }

        public async Task<string?> fnObfuscate(string szScriptName, string szPayload, Dictionary<string, object> dicParams = null)
        {
            return await fnSendRequest("obfuscate", szScriptName, szPayload, dicParams);
        }

        public async Task<string?> fnDeobfuscate(string szScriptName, string szObfuscatedPayload, Dictionary<string, object> dicParams = null)
        {
            return await fnSendRequest("deobfuscate", szScriptName, szObfuscatedPayload, dicParams);
        }

        private async Task<string?> fnSendRequest(string szEndPoint, string szScriptName, string szPayload, Dictionary<string, object> dicParams)
        {
            var req_body = new
            {
                script_name = szScriptName,
                payload = szPayload,
                parameters = dicParams ?? new Dictionary<string, object>()
            };

            string szJson = JsonSerializer.Serialize(req_body);
            using var content = new StringContent(szJson, Encoding.UTF8, "application/json");

            try
            {
                HttpResponseMessage resp = await m_client.PostAsync($"{m_szPyServerUri}/{szEndPoint}", content);
                if (!resp.IsSuccessStatusCode)
                {
                    string szContent = await resp.Content.ReadAsStringAsync();
                    throw new Exception($"Python engine error ({resp.StatusCode}): {szContent}");
                }

                string szJsonResult = await resp.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(szJsonResult);

                return doc.RootElement.GetProperty("result").GetString();
            }
            catch (HttpRequestException ex)
            {
                throw new Exception("Could not connect to the Python backend server. Please check your configuration.", ex);
            }
        }
    }
}