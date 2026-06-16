using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Alien
{
    public class clsfnReg
    {
        private clsWeb m_web { get; init; }

        public clsfnReg(clsWeb web)
        {
            m_web = web;
        }

        public async Task<Dictionary<string, bool>> fnHives()
        {
            string szResp = await m_web.fnszSendPayload("win_reg", new string[] { "hive" });
            var result = JsonConvert.DeserializeObject<Dictionary<string, bool>>(szResp);

            if (result == null)
                return new Dictionary<string, bool>();

            return result;
        }
    }
}
