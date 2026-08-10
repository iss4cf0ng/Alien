using System.Data;
using System.Data.SQLite;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Forms;

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
                    SYSTEM_USER AS [current_user];"
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
        public Dictionary<enDatabase, string> m_dicShowDatabaseSQL = new Dictionary<enDatabase, string>()
        {
            {
                enDatabase.MySQL,
                @"SHOW DATABASES;"
            },
            {
                enDatabase.SQLServer,
                @"SELECT name FROM sys.databases ORDER BY name;"
            },
            {
                enDatabase.PostgreSQL,
                @"SELECT
                    datname AS name
                  FROM pg_database
                  ORDER BY datname;"
            },
            {
                enDatabase.Oracle,
                @"SELECT name FROM v$database;"
            },
            {
                enDatabase.SQLite,
                @"SELECT '$(DATABASE)' AS name;"
            },
            {
                enDatabase.Access,
                @"SELECT '$(DATABASE)' AS name;"
            },
            {
                enDatabase.DSN,
                @""
            }
        };
        private Dictionary<enDatabase, Func<string, string>> m_dicShowTablesSQL = new Dictionary<enDatabase, Func<string, string>>()
        {
            {
                enDatabase.MySQL,
                (db) =>
                    $"SELECT table_name AS name " +
                    $"FROM information_schema.tables " +
                    $"WHERE table_schema = '{db}' " +
                    $"ORDER BY table_name;"
            },
            {
                enDatabase.SQLServer,
                (db) =>
                    $"USE [{db}]; " +
                    $"SELECT name AS name " +
                    $"FROM sys.tables " +
                    $"ORDER BY name;"
            },
            {
                enDatabase.PostgreSQL,
                (db) =>
                    $"SELECT table_name AS name " +
                    $"FROM information_schema.tables " +
                    $"WHERE table_catalog = '{db}' " +
                    $"AND table_schema = 'public' " +
                    $"ORDER BY table_name;"
            },
            {
                enDatabase.Oracle,
                (db) =>
                    @"SELECT table_name AS name
                      FROM user_tables
                      ORDER BY table_name;"
            },
            {
                enDatabase.SQLite,
                (db) =>
                    @"SELECT name AS name
                      FROM sqlite_master
                      WHERE type='table'
                      ORDER BY name;"
            },
            {
                enDatabase.Access,
                (db) =>
                    @"SELECT Name AS name
                      FROM MSysObjects
                      WHERE Type=1
                      AND Name NOT LIKE 'MSys*'
                      ORDER BY Name;"
            },
            {
                enDatabase.DSN,
                (db) => ""
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

        public class clsSqlQueryExResult
        {
            public bool bSuccess { get; set; }
            public string szQuery { get; set; }
            public string szErrorMsg { get; set; }

            public DataTable dtOutput { get; set; }
        }

        #region Local Function

        /// <summary>
        /// Check database existence
        /// </summary>
        /// <param name="szSource"></param>
        /// <returns></returns>
        public bool fnbDbExists(string szSource)
        {
            string szQuery = "SELECT EXISTS(SELECT 1 FROM \"Database\" WHERE \"Source\" = @src);";

            using var cmd = new SQLiteCommand(szQuery, m_sqlConn.m_sqlConn);
            cmd.Parameters.AddWithValue("@src", szSource);

            object result = cmd.ExecuteScalar();

            return Convert.ToInt32(result) == 1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="szId"></param>
        /// <returns></returns>
        public stDbConfig fnGetDbConfig(string szId)
        {
            var ls = fnGetAllDbConfig();
            stDbConfig config = ls.Where(x => string.Equals(x.szID, szId)).ToList().First();

            return config;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static string fnPrintTable(DataTable dt)
        {
            if (dt == null || dt.Columns.Count == 0)
                return string.Empty;

            int[] nWidths = new int[dt.Columns.Count];

            // Calculate widths
            for (int i = 0; i < dt.Columns.Count; i++)
            {
                nWidths[i] = dt.Columns[i].ColumnName.Length;

                foreach (DataRow dr in dt.Rows)
                {
                    string szValue = dr[i]?.ToString() ?? string.Empty;
                    nWidths[i] = Math.Max(nWidths[i], szValue.Length);
                }
            }

            StringBuilder sb = new StringBuilder();

            string szSeparate = "+" + string.Join("+", nWidths.Select(w => new string('-', w + 2))) + "+";

            sb.AppendLine(szSeparate);

            // Header
            sb.Append("|");
            for (int i = 0; i < dt.Columns.Count; i++)
            {
                sb.Append(" ");
                sb.Append(dt.Columns[i].ColumnName.PadRight(nWidths[i]));
                sb.Append(" |");
            }

            sb.AppendLine();
            sb.AppendLine(szSeparate);

            // Rows
            foreach (DataRow dr in dt.Rows)
            {
                sb.Append("|");

                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    string szValue = dr[i]?.ToString() ?? string.Empty;

                    sb.Append(" ");
                    sb.Append(szValue.PadRight(nWidths[i]));
                    sb.Append(" |");
                }

                sb.AppendLine();
            }

            sb.AppendLine(szSeparate);

            return sb.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cfg"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public static string fnBuildConnURL(stDbConfig cfg)
        {
            string user = Uri.EscapeDataString(cfg.szUsername ?? "");
            string pass = Uri.EscapeDataString(cfg.szPassword ?? "");

            string Auth()
            {
                if (string.IsNullOrWhiteSpace(cfg.szUsername))
                    return "";

                return $"{user}:{pass}@";
            }


            switch (cfg.enDbType)
            {
                case enDatabase.DSN:
                    // Raw PDO DSN
                    return $"dsn://{cfg.szSource}" + (string.IsNullOrWhiteSpace(cfg.szUsername) ? "" : $";User={user};Password={pass}");

                case enDatabase.MySQL:
                    return
                        $"mysql://{Auth()}{cfg.szSource}/information_schema";

                case enDatabase.SQLServer:
                    return $"sqlsrv://{Auth()}{cfg.szSource}/master";

                case enDatabase.PostgreSQL:
                    return $"pgsql://{Auth()}{cfg.szSource}/postgres";

                case enDatabase.SQLite:
                    return $"sqlite://{cfg.szSource}";

                case enDatabase.ODBC:
                    return $"dsn://{cfg.szSource}";

                case enDatabase.Access:
                    {
                        string url = $"access://{cfg.szSource}";

                        if (!string.IsNullOrWhiteSpace(cfg.szPassword))
                            url += $";Password={Uri.EscapeDataString(cfg.szPassword)}";

                        return url;
                    }

                case enDatabase.Oracle:
                    return $"oracle://{Auth()}{cfg.szSource}";

                default:
                    throw new NotSupportedException($"Unsupported database type: {cfg.enDbType}");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dbType"></param>
        /// <param name="szDbName"></param>
        /// <param name="szTable"></param>
        /// <param name="nLimit"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public string fnBuildDataQuery(enDatabase dbType, string szDbName, string szTable, int nLimit = 100)
        {
            switch (dbType)
            {
                case enDatabase.MySQL:
                    return
                        $"SELECT * FROM `{szDbName}`.`{szTable}` LIMIT {nLimit};";

                case enDatabase.SQLServer:
                    return
                        $"SELECT TOP {nLimit} * " +
                        $"FROM [{szDbName}].[dbo].[{szTable}];";

                case enDatabase.PostgreSQL:
                    return
                        $"SELECT * " +
                        $"FROM \"{szDbName}\".\"{szTable}\" " +
                        $"LIMIT {nLimit};";

                case enDatabase.SQLite:
                    return
                        $"SELECT * " +
                        $"FROM \"{szTable}\" " +
                        $"LIMIT {nLimit};";

                case enDatabase.Access:
                    return
                        $"SELECT TOP {nLimit} * " +
                        $"FROM [{szTable}];";

                case enDatabase.Oracle:
                    return $"SELECT * " + $"FROM \"{szTable}\" " + $"FETCH FIRST {nLimit} ROWS ONLY;";

                default:
                    throw new NotSupportedException($"Unsupported database type: {dbType}");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="szSQL"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Get information of the remote database
        /// </summary>
        /// <returns></returns>
        public async Task<List<(string, bool)>> fnDbInit()
        {
            string szContent = await m_web.fnszSendPayload("db_init");
            List<(string, bool)> result = szContent.Trim('\n').Trim('\r').Split(',').Where(x => !string.IsNullOrEmpty(x)).Select(x => x.Split(':')).Select(x => (x.First(), Equals(x.Last(), "1"))).ToList();

            return result;
        }

        /// <summary>
        /// Convert Dictionary object into DataTable object
        /// </summary>
        /// <param name="objData"></param>
        /// <returns></returns>
        private DataTable fnConvertToTable(List<Dictionary<string, object>> objData)
        {
            DataTable dt = new DataTable();
            if (objData == null || objData.Count == 0)
                return dt;

            int emptyColIndex = 1;
            foreach (var key in objData.First().Keys)
            {
                string colName = key;
                if (string.IsNullOrWhiteSpace(colName))
                {
                    colName = "Column_" + emptyColIndex++;
                }

                string uniqueColName = colName;
                int dup = 1;
                while (dt.Columns.Contains(uniqueColName))
                {
                    uniqueColName = colName + "_" + dup++;
                }

                dt.Columns.Add(uniqueColName);
            }

            foreach (var dict in objData)
            {
                DataRow dr = dt.NewRow();
                int i = 0;
                foreach (var key in dict.Keys)
                {
                    dr[dt.Columns[i].ColumnName] = dict[key]?.ToString();
                    i++;
                }
                dt.Rows.Add(dr);
            }

            dt.AcceptChanges();
            return dt;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// <param name="szQuery"></param>
        /// <returns></returns>
        public async Task<string> fnszSqlExec(stDbConfig config, string szQuery)
        {
            string? szDb = Enum.GetName(typeof(enDatabase), config.enDbType);
            if (string.IsNullOrEmpty(szDb))
            {
                MessageBox.Show("Parse language enumerator error.");
                return string.Empty;
            }

            //string szPayload = $"db_{szDb.ToLower()}_query";
            string szResp = await m_web.fnszSendPayload("db_query", new string[]
            {
                config.szConnString,
                szQuery,
            });

            return szResp;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// <param name="szQuery"></param>
        /// <returns></returns>
        public async Task<DataTable> fnSqlQuery(stDbConfig config, string szQuery)
        {
            DataTable dt = new DataTable();
            string szResp = await fnszSqlExec(config, szQuery);

            if (string.IsNullOrEmpty(szResp))
                return dt;

            try
            {
                clsQueryResponse? result = JsonSerializer.Deserialize<clsQueryResponse>(szResp);
                if (result == null)
                    return dt;

                if (!result.success)
                {
                    MessageBox.Show(result.error + "\n" + szQuery, "SQL query error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return dt;
                }

                try
                {
                    dt = fnConvertToTable(result.data);
                }
                catch
                {

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + szResp, "HTTP response", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return dt;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// <param name="szQuery"></param>
        /// <returns></returns>
        public async Task<clsSqlQueryExResult?> fnSqlQueryEx(stDbConfig config, string szQuery)
        {
            DataTable dt = new DataTable();
            string szResp = await fnszSqlExec(config, szQuery);
            if (string.IsNullOrEmpty(szResp))
            {
                return new clsSqlQueryExResult()
                {
                    bSuccess = false,
                    szQuery = szQuery,
                    szErrorMsg = "Responsed result is null or empty.",
                    dtOutput = dt,
                };
            }

            try
            {
                clsQueryResponse? result = JsonSerializer.Deserialize<clsQueryResponse>(szResp);
                if (result == null)
                {
                    return new clsSqlQueryExResult()
                    {
                        bSuccess = false,
                        szQuery = szQuery,
                        szErrorMsg = "JSON deserialization is failed. Responsed result is invalid.",
                        dtOutput = dt,
                    };
                }

                return new clsSqlQueryExResult()
                {
                    bSuccess = result.success,
                    szQuery = szQuery,
                    szErrorMsg = result.error,
                    dtOutput = result.success ? fnConvertToTable(result.data) : dt
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + szResp, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public async Task<bool> fnDbTest(stDbConfig config)
        {
            string szQuery = "SELECT 1 AS val;";
            var result = await fnSqlQueryEx(config, szQuery);

            if (result == null)
                return false;

            if (!result.bSuccess)
                MessageBox.Show(result.szErrorMsg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            return result.bSuccess;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// <param name="szDbName"></param>
        /// <returns></returns>
        public async Task<List<string>> fnDbGetTables(stDbConfig config, string szDbName)
        {
            string fnGetSQL(enDatabase dbType, string dbName)
            {
                if (m_dicShowTablesSQL.TryGetValue(dbType, out var fn))
                {
                    return fn(dbName);
                }

                return string.Empty;
            }

            string szQuery = fnGetSQL(config.enDbType, szDbName);
            if (string.IsNullOrEmpty(szQuery))
                return new List<string>();

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
