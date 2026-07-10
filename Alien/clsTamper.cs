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
            m_client = new HttpClient() { Timeout = TimeSpan.FromSeconds(10) };

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

        public static string fnMergePayloadToOne(stShellConfig config, string szPayload, string[] asParams, enLanguage lang)
        {
            for (int i = 0; i < asParams.Length; i++)
            {
                if (lang == enLanguage.PHP)
                {
                    szPayload = szPayload.Replace($"$_POST['z{i}']", $"\"{asParams[i]}\"");
                }
                else if (lang == enLanguage.ASP)
                {
                    szPayload = szPayload.Replace($"Request.Form(\"z{i}\")", $"\"{asParams[i]}\"", StringComparison.OrdinalIgnoreCase);
                    szPayload = szPayload.Replace($"Request(\"z{i}\")", $"\"{asParams[i]}\"", StringComparison.OrdinalIgnoreCase);

                    szPayload = szPayload.Replace($"Request.Form('z{i}')", $"\"{asParams[i]}\"", StringComparison.OrdinalIgnoreCase);
                }
                else if (lang == enLanguage.ASPX || lang == enLanguage.ASMX || lang == enLanguage.ASHX)
                {
                    szPayload = szPayload.Replace($"System.Web.HttpContext.Current.Request.Form[\"z{i}\"]", $"\"{asParams[i]}\"");
                    szPayload = szPayload.Replace($"System.Web.HttpContext.Current.Request.Item[\"z{i}\"]", $"\"{asParams[i]}\"");
                    szPayload = szPayload.Replace($"Request.Form[\"z{i}\"]", $"\"{asParams[i]}\"");
                    szPayload = szPayload.Replace($"Request.Item[\"z{i}\"]", $"\"{asParams[i]}\"");
                    //Request.Item["z0"]
                }
                else if (lang == enLanguage.JSP || lang == enLanguage.JSPX)
                {
                    if (config.payloadType == enPayloadType.OneShell)
                    {
                        szPayload = szPayload.Replace($"request.getParameter(\"z{i}\")", $"\"{asParams[i]}\"");
                        szPayload = szPayload.Replace($"paramMap.get(\"z{i}\")[0]", $"\"{asParams[i]}\"");
                        szPayload = szPayload.Replace($"paramMap.get(\"z{i}\")", $"\"{asParams[i]}\"");
                    }
                    else if (config.payloadType == enPayloadType.DarkMatter)
                    {

                    }
                }
                else if (lang == enLanguage.CFM)
                {
                    if (config.payloadType == enPayloadType.OneShell)
                    {
                        szPayload = szPayload.Replace($"request.getParameter(\"z{i}\")", $"\"{asParams[i]}\"");
                    }
                    else if (config.payloadType == enPayloadType.DarkMatter)
                    {

                    }
                }
                else if (lang == enLanguage.Perl)
                {
                    szPayload = szPayload.Replace($"$q->param('z{i}')", $"\'{asParams[i]}\'");
                    szPayload = szPayload.Replace($"$q->param(\"z{i}\")", $"\"{asParams[i]}\"");
                }
                else if (lang == enLanguage.Ruby)
                {
                    szPayload = szPayload.Replace($"$_POST['z{i}']", $"\'{asParams[i]}\'");
                    szPayload = szPayload.Replace($"$_POST[\"z{i}\"]", $"\"{asParams[i]}\"");
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

        public async Task<string?> fnObfuscate(string szScriptName, string szPayload, Dictionary<string, object> dicParams = null)
        {
            return await fnSendRequest("obfuscate", szScriptName, szPayload, dicParams);
        }

        public async Task<string?> fnDeobfuscate(string szScriptName, string szObfuscatedPayload, Dictionary<string, object> dicParams = null)
        {
            return await fnSendRequest("deobfuscate", szScriptName, szObfuscatedPayload, dicParams);
        }

        public async Task<string?> fnGetExample(string szScriptName)
        {
            Dictionary<string, object> dicParam = new Dictionary<string, object>();

            return await fnSendRequest("example", szScriptName, string.Empty, dicParam);
        }

        public async Task<string?> fnBuild(string szScriptName, Dictionary<string, object> dicParam)
        {
            return await fnSendRequest("build", szScriptName, string.Empty, dicParam);
        }

        public async Task<string?> fnGetObfuscator(string szScriptName, Dictionary<string, object> dicParam)
        {
            return await fnSendRequest("obfuscator", szScriptName, string.Empty, dicParam);
        }

        public async Task<List<string>?> fnGetAvailableScript(string szScriptName)
        {
            string? szResp = await fnSendRequest("available", szScriptName, string.Empty, new Dictionary<string, object>());
            if (string.IsNullOrEmpty(szResp))
                return new List<string>();

            return szResp.Replace(" ", string.Empty).Split(',').ToList();
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
                    MessageBox.Show("Backend server return error: " + "\n" + szContent, $"HTTP {(int)resp.StatusCode} {resp.StatusCode.ToString()}", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    return string.Empty;
                }

                string szJsonResult = await resp.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(szJsonResult);

                return doc.RootElement.GetProperty("result").GetString();
            }
            catch (HttpRequestException ex)
            {
                throw new Exception("Could not connect to the Python backend server. Please check your configuration.", ex);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return string.Empty;
            }
        }
    }
}