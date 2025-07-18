using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alien
{
    public class clsVictim
    {
        private clsSqlite m_sqlConn { get; set; }
        private stShellConfig m_ShellConfig { get; set; }

        public string ShellID { get { return m_ShellConfig.ID; } }
        public string ShellURL { get { return m_ShellConfig.szUrl; } }

        public clsVictim(clsSqlite sqlConn, stShellConfig config)
        {
            m_sqlConn = sqlConn;
            m_ShellConfig = config;
        }
    }
}
