using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alien
{
    public class clsWeb
    {
        public clsVictim m_victim { get; set; }
        public HttpClient m_clnt { get; set; }

        public clsWeb(clsVictim victim)
        {
            m_victim = victim;
            m_clnt = new HttpClient();
        }

        public async Task<string> fnHttpPOST(string szPayloadData)
        {
            StringContent content = new StringContent(szPayloadData, Encoding.GetEncoding(m_victim.ShellEncoding), "text/plain");
            HttpResponseMessage resp = await m_clnt.PostAsync(m_victim.ShellURL, content);
            string szRespContent = await resp.Content.ReadAsStringAsync();

            if (resp.IsSuccessStatusCode)
            {
                return szRespContent;
            }
            else
            {
                frmMsgBox f = new frmMsgBox(resp.StatusCode.ToString(), szRespContent);
                return string.Empty;
            }
        }

        public async Task<bool> fnbTestShellConnection()
        {
            try
            {
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<string> fnszSendCommand(string szCommand, string[] asParams)
        {
            return string.Empty;
        }

        /// <summary>
        /// Read payload from file with specified language, method and payload type.
        /// </summary>
        /// <param name="szPayloadName">Payload name, also represents to file name.</param>
        /// <returns>Payload content</returns>
        private string fnGetPayload(string szPayloadName)
        {
            return string.Empty;
        }
    }
}
