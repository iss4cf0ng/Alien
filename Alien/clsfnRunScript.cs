using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alien
{
    public class clsfnRunScript
    {
        private clsWeb m_web { get; init; }

        public clsfnRunScript(clsWeb web)
        {
            m_web = web;
        }

        public class clsHttpResponse
        {
            public string status { get; set; }
            public string action { get; set; }
            public int http_code { get; set; }
            public string data { get; set; }
        }

        public async Task<string> fnszRunScript(string szCode)
        {
            try
            {
                string szPost = await m_web.fnszSendPayload("eval", new string[] { szCode });
                return szPost;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return string.Empty;
            }
        }

        public async Task<clsHttpResponse> fnHttpGET(string szUrl)
        {
            string szResp = await m_web.fnszSendPayload("http", new string[] { "get", szUrl });
            var resp = JsonConvert.DeserializeObject<clsHttpResponse>(szResp);
            if (resp == null)
                throw new Exception("JSON deserialization is failed.");

            if (resp.status == "error")
                throw new Exception(resp.data);

            return resp;
        }

        public async Task<clsHttpResponse> fnHttpPOST(string szUrl, string szData)
        {
            string szResp = await m_web.fnszSendPayload("http", new string[] { "post", szUrl, szData });
            var resp = JsonConvert.DeserializeObject<clsHttpResponse>(szResp);
            if (resp == null)
                throw new Exception("JSON deserialization is failed.");

            if (resp.status == "error")
                throw new Exception(resp.data);

            return resp;
        }
    }
}
