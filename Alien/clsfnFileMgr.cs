using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alien
{
    internal class clsfnFileMgr : clsWeb
    {
        public string m_szCurrentPath { get; set; }

        public clsfnFileMgr(clsVictim victim) : base(victim)
        {
            
        }

        public string fnGetCurrentPath()
        {
            return string.Empty;
        }
    }
}
