using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alien
{
    public class Web
    {
        public string m_szURL { get; set; }
        public HttpClient m_clnt { get; set; }

        public Web(string szURL)
        {
            m_szURL = szURL;
            
            m_clnt = new HttpClient();
        }

        public async Task<string> HttpPOST(FormUrlEncodedContent formData)
        {
            HttpResponseMessage respMsg = await m_clnt.PostAsync(m_szURL, formData);
            string szRet = await respMsg.Content.ReadAsStringAsync();

            return szRet;
        }
    }
}
