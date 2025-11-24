using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Alien
{
    public class clsfnDb
    {
        private clsWeb m_web { get; set; }

        public string m_szConnString { get; set; }
        public clsSqlite m_sqlConn { get; set; }

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
                    "Source",
                    "Username",
                    "Password",
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

        public struct stDbConfig
        {
            public string szID { get; set; }

            public enDatabase enDbType { get; set; }

            public string szSource { get; set; }
            public string szUsername { get; set; }
            public string szPassword { get; set; }
            public string szConnString { get; set; }

            public DateTime dtCreationDate { get; set; }
            public DateTime dtLastUsed { get; set; }
        }

        public clsfnDb(clsWeb web, string szDbFileName)
        {
            m_web = web;
            m_szDbFileName = szDbFileName;

            m_szDbDirectory = Path.Combine(web.m_victim.m_szPortfolio, "Database");
            if (!Directory.Exists(m_szDbDirectory))
                Directory.CreateDirectory(m_szDbDirectory);

            m_sqlConn = new clsSqlite(m_szDbFilePath, m_dicTable);
        }

        #region Local Function

        public bool fnbDbExists(string szSource)
        {
            string szQuery = $"SELECT 1 FROM \"Database\" WHERE \"Source\"=\"{szSource}\";";
            DataTable dt = clsTool.fnSqlQuery(m_sqlConn.m_sqlConn, szQuery);

            return (int)dt.Rows[0][0] == 1;
        }
        
        private bool fnbDbWriteValidate(stDbConfig config)
        {
            if (!fnbDbExists(config.szSource))
                return false;

            stDbConfig x = fnGetDbConfig(config.szSource);
            bool bRet = string.Equals(x.szID, config.szID)
                && string.Equals(x.szSource, config.szSource)
                && string.Equals(x.szUsername, config.szUsername)
                && string.Equals(x.szPassword, config.szPassword)
                && DateTime.Equals(x.dtCreationDate, config.dtCreationDate)
                && DateTime.Equals(x.dtLastUsed, config.dtLastUsed);

            return bRet;
        }

        public bool fnbSaveDatabase(stDbConfig config)
        {
            string szQuery = string.Empty;
            if (fnbDbExists(config.szSource))
            {
                szQuery = $"INSERT INTO Database(" +
                    $"\"ID\"," +
                    $"\"DbType\"," +
                    $"\"ConnString\"," +
                    $"\"Source\"," +
                    $"\"Username\"," +
                    $"\"Password\"," +
                    $"\"CreationDate\"," +
                    $"\"LastUsed\"" +
                    $") VALUES(" +
                    $"\"{Guid.NewGuid().ToString()}\"," +
                    $"\"{Enum.GetName(config.enDbType)}\"," +
                    $"\"{config.szConnString}\"," +
                    $"\"{config.szSource}\"," +
                    $"\"{config.szUsername}\"," +
                    $"\"{config.szPassword}\"," +
                    $"\"{config.dtCreationDate.ToString("F")}\"," +
                    $"\"{config.dtLastUsed.ToString("F")}\"" +
                    $");";
            }
            else
            {
                szQuery = $"UPDATE \"Database\" SET " +
                    $"\"DbType\" = \"{Enum.GetName(config.enDbType)}\"," +
                    $"\"ConnString\" = \"{config.szConnString}\"," +
                    $"\"Source\" = \"{config.szSource}\"," +
                    $"\"Username\" = \"{config.szUsername}\"," +
                    $"\"Password\" = \"{config.szPassword}\"," +
                    $"\"CreationDate\" = \"{config.dtCreationDate.ToString("F")}\"," +
                    $"\"LastUsed\" = \"{config.dtLastUsed.ToString("F")}\"," +
                    $"WHERE \"ID\" = \"{config.szID}\";";
            }

            DataTable dt = clsTool.fnSqlQuery(m_sqlConn.m_sqlConn, szQuery);

            return fnbDbWriteValidate(config);
        }

        public stDbConfig fnGetDbConfig(string szSource)
        {
            var ls = fnGetAllDbConfig();
            stDbConfig config = ls.Where(x => string.Equals(x.szSource, szSource)).ToList().First();

            return config;
        }

        public List<stDbConfig> fnGetAllDbConfig()
        {
            List<stDbConfig> ls = new List<stDbConfig>();

            string szQuery = $"SELECT * FROM \"Database\";";
            DataTable dt = clsTool.fnSqlQuery(m_sqlConn.m_sqlConn, szQuery);
            foreach (DataRow dr in dt.Rows)
            {
                string szID = (string)dr["ID"];
                enDatabase enDb = (enDatabase)Enum.Parse(typeof(enDatabase), (string)dr["DbType"]);
                string szConnStr = (string)dr["ConnString"];
                string szSource = (string)dr["Source"];
                string szUsername = (string)dr["Username"];
                string szPassword = (string)dr["Password"];
                DateTime dtCreation = DateTime.Parse((string)dr["CreationDate"]);
                DateTime dtLastUsed = DateTime.Parse((string)dr["LastUsed"]);

                ls.Add(new stDbConfig()
                {
                    szID = szID,

                    enDbType = enDb,

                    szSource = szSource,
                    szConnString = szConnStr,
                    szUsername = szUsername,
                    szPassword = szPassword,

                    dtCreationDate = dtCreation,
                    dtLastUsed = dtLastUsed,
                });
            }

            return ls;
        }

        #endregion

        #region Remote Function

        public async Task<string> fnDbInit(stDbConfig config)
        {
            string szContent = await m_web.fnszSendPayload("db_init", new string[]
                {
                    config.szSource,
                    config.szUsername,
                    config.szPassword,

                });

            return string.Empty;
        }

        public async Task<string> fnSqlExec(stDbConfig config)
        {

            return string.Empty;
        }

        #endregion
    }
}
