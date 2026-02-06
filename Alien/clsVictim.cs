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
        public stShellConfig m_ShellConfig { get; set; }

        public string ShellID { get { return m_ShellConfig.ID; } }
        public string ShellURL { get { return m_ShellConfig.szUrl; } }
        public string ShellPassword { get { return m_ShellConfig.szPassword; } }
        public string ShellEncoding { get { return m_ShellConfig.szEncoding; } }
        public enLanguage ShellLanguage { get { return m_ShellConfig.language; } }
        public string ShellMethod { get { return m_ShellConfig.szMethod; } }
        public enPayloadType ShellPayloadType { get { return m_ShellConfig.payloadType; } }
        public string m_szShellDomain { get { return ShellURL.Split('/')[2]; } }
        public string m_szPortfolio { get { return Path.Combine(new string[] { Application.StartupPath, "Victim", m_szShellDomain }); } }
        public bool m_bUnixLike { get; set; }

        public clsVictim(clsSqlite sqlConn, stShellConfig config, bool bUnixLike)
        {
            m_sqlConn = sqlConn;
            m_ShellConfig = config;
            m_bUnixLike = bUnixLike;
        }

        public bool fnbBuildPortfolio()
        {
            try
            {
                if (!Directory.Exists(m_szPortfolio))
                    Directory.CreateDirectory(m_szPortfolio);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        #region FileMgr

        public void fnUploadFile(string szSrcFileName, string szDstFileName)
        {

        }

        public void fnDownloadFile(string szSrcFileName, string szDstFileName)
        {

        }

        #endregion
    }
}
