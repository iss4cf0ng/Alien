using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alien
{
    public class clsTamper
    {
        private string m_szTamperName { get; set; }

        public clsTamper(string szTamperName)
        {
            m_szTamperName = szTamperName;
        }

        public string fnszGetPayload(string szPayload)
        {
            //todo: Process payload string data with tamper script.
            return string.Empty;
        }
    }
}