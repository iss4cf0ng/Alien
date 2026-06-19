using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alien
{
    public class clsfnWinUser
    {
        private clsWeb m_web { get; init; }

        public clsfnWinUser(clsWeb web)
        {
            m_web = web;
        }

        public class clsApiResponse
        {
            [JsonProperty("success")]
            public bool Success { get; set; }

            [JsonProperty("error")]
            public string Error { get; set; }

            [JsonProperty("data")]
            public clsWmiData Data { get; set; }
        }
        public class clsWmiData
        {
            [JsonProperty("user_accounts")]
            public List<WmiRow> UserAccounts { get; set; }

            [JsonProperty("user_profiles")]
            public List<WmiRow> UserProfiles { get; set; }

            [JsonProperty("groups")]
            public List<WmiRow> Groups { get; set; }

            [JsonProperty("group_users")]
            public List<WmiRow> GroupUsers { get; set; }

            [JsonProperty("logged_on")]
            public List<WmiRow> LoggedOn { get; set; }

            [JsonProperty("logon_session")]
            public List<WmiRow> LogonSession { get; set; }
        }
        public class WmiRow
        {
            private Dictionary<string, string> _data = new Dictionary<string, string>();

            [JsonExtensionData]
            public IDictionary<string, JToken> Raw { get; set; }

            [JsonIgnore]
            public Dictionary<string, string> Data
            {
                get
                {
                    if (_data.Count == 0 && Raw != null)
                    {
                        foreach (var kv in Raw)
                        {
                            _data[kv.Key] = kv.Value?.ToString();
                        }
                    }
                    return _data;
                }
            }

            public string this[string key] => Data.ContainsKey(key) ? Data[key] : null;
        }

        public async Task<clsWmiData> fnGetData()
        {
            string szResp = await m_web.fnszSendPayload("win_user");
            if (string.IsNullOrEmpty(szResp))
                throw new Exception("HTTP response is null or empty");

            var result = JsonConvert.DeserializeObject<clsApiResponse>(szResp);
            if (result == null)
                throw new Exception("JSON deserialization is failed.");

            if (!result.Success)
                throw new Exception(result.Error);

            return result.Data;
        }
    }
}
