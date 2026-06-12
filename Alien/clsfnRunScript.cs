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

        public async Task<string> fnszRunScript(string szCode)
        {
            string szPost = await m_web.fnszSendPayload("run_script", new string[] { szCode });
            return szPost;
        }
    }
}
