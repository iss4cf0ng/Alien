using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alien
{
    internal class clsfnDb
    {
        private clsWeb m_web { get; set; }

        public string m_szConnString { get; set; }
        private clsSqlite m_sqlConn { get; set; }

        private string m_szDbFileName { get; set; }
        private string m_szDbDirectory { get; set; }
        private string m_szDbFilePath { get { return Path.Combine(m_szDbDirectory, m_szDbFileName); } }

        private Dictionary<string, string[]> m_dicTable = new Dictionary<string, string[]>()
        {
            {
                "Database",
                new string[]
                {
                    "DbType",
                    "ConnString",
                    "CreationDate",
                    "LastUsed",
                }
            },
            {
                "Logs",
                new string[]
                {
                    "MsgType",
                    "Message",
                    "CreationDate",
                }
            }
        };

        public clsfnDb(clsWeb web, string szDbFileName)
        {
            m_web = web;
            m_szDbFileName = szDbFileName;

            m_szDbDirectory = Path.Combine(web.m_victim.m_szPortfolio, "Database");
            if (!Directory.Exists(m_szDbDirectory))
                Directory.CreateDirectory(m_szDbDirectory);

            m_sqlConn = new clsSqlite(m_szDbFilePath, m_dicTable);
        }

        public bool fnbTestRemoteConnection()
        {
            try
            {


                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
