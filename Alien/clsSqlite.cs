using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.ComponentModel;
using System.Data;
using System.Security.Cryptography.X509Certificates;
using System.Globalization;

namespace Alien
{
    public class clsSqlite
    {
        public string m_szFileName { get; set; }
        public string m_szConnString { get; set; }

        private Dictionary<string, string[]> m_dicTable = new Dictionary<string, string[]>
        {
            {
                "Shell",
                new string[]
                {
                    "ID",
                    "GroupName",
                    "URL",
                    "Password",
                    "Encoding",
                    "Language",
                    "Method",
                    "Type",
                    "CreateDate",
                    "LastModified",
                    "LastAccessed",

                    "Description",
                    "UserAgent",
                    "EHEnable", // Tamper
                    "EventHorizonScript",
                    "EventHorizonConfig",
                    "DCEnable",
                    "DriftingComet", // Pivoting

                    "Cookie",
                    "ExtraPost",
                    "ExtraPostPosition",
                    "Timeout",
                }
            },
            {
                "Group",
                new string[]
                {
                    "Name",
                    "CreationDate",
                }
            },
            {
                "Log",
                new string[]
                {
                    "Name",
                    "CreateDate",
                    "LastModified",
                    "LastAccessed",
                }
            }
        };

        public SQLiteConnection m_sqlConn { get; set; }

        public clsSqlite(SQLiteConnection sqlConn)
        {
            m_sqlConn = sqlConn;
        }

        public clsSqlite(string szFileName, Dictionary<string, string[]> dicTable = null)
        {
            m_szFileName = szFileName;

            if (!File.Exists(m_szFileName) && !CreateDB(dicTable == null ? m_dicTable : dicTable))
                throw new Exception("CreateDB() error.");
            else
            {
                m_szConnString = $"Data Source=\"{m_szFileName}\";Compress=True";
                m_sqlConn = new SQLiteConnection(m_szConnString);
                m_sqlConn.Open();
            }
        }

        #region Basic

        public bool CreateDB(Dictionary<string, string[]> dicTable)
        {
            try
            {
                m_szConnString = $"Data Source={m_szFileName};Compress=True;";
                m_sqlConn = new SQLiteConnection(m_szConnString);
                m_sqlConn.Open();

                foreach (string szKey in dicTable.Keys)
                {
                    if (!CreateTable(szKey, dicTable[szKey]))
                        throw new Exception("CreateTable() error.");
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "CreateDB", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public bool CreateTable(string szTableName, string[] aszColumns)
        {
            try
            {
                string szQuery = $"CREATE TABLE \"{szTableName}\" ({string.Join(",", aszColumns.Select(x => $"{x} TEXT"))})";
                DataTable dt = fnSqlQuery(szQuery);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "CreateTable()", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public DataTable fnSqlQuery(string szQuery)
        {
            DataTable dt = new DataTable();
            using (SQLiteCommand sqlCmd = new SQLiteCommand(szQuery, m_sqlConn))
            {
                try
                {
                    using (IDataReader idr = sqlCmd.ExecuteReader())
                    {
                        dt.Load(idr);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "fnSqlQuery()", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            return dt;
        }

        #endregion
        #region Tool

        private stShellConfig fnDataRowToStruct(DataRow dr)
        {
            stShellConfig config = new stShellConfig()
            {
                ID = dr["ID"].ToString(),
                szGroupName = dr["GroupName"].ToString(),
                szUrl = dr["URL"].ToString(),
                szPassword = dr["Password"].ToString(),
                szEncoding = dr["Encoding"].ToString(),
                language = (enLanguage)Enum.Parse(typeof(enLanguage), dr["Language"].ToString()),
                payloadType = (enPayloadType)Enum.Parse(typeof(enPayloadType), dr["Type"].ToString()),
                szMethod = dr["Method"].ToString(),
                dtCreateDate = DateTime.Parse(dr["CreateDate"].ToString()),
                dtLastModified = DateTime.Parse(dr["LastModified"].ToString()),
                dtLastAccessed = DateTime.Parse(dr["LastAccessed"].ToString()),

                szDescription = dr["Description"].ToString(),
                szUserAgent = dr["UserAgent"].ToString(),

                bEHEnable = dr["EHEnable"].ToString() == "1",
                szEventHorizonScript = dr["EventHorizonScript"].ToString(),
                szEventHorizonConfig = dr["EventHorizonConfig"].ToString(),

                szDriftingComet = dr["DriftingComet"].ToString(),

                nTimeout = int.Parse(dr["Timeout"].ToString()),
                szCookie = dr["Cookie"].ToString(),
                szExtraPost = dr["ExtraPost"].ToString(),
                nExtraPostPosition = int.Parse(dr["ExtraPostPosition"].ToString()),
            };

            return config;
        }

        #endregion
        #region Shell

        public bool ShellExists(string szID)
        {
            try
            {
                string szQuery = $"SELECT 1 FROM `Shell` WHERE ID = \"{szID}\";";
                DataTable dtResult = fnSqlQuery(szQuery);
                if (dtResult.Rows.Count == 0)
                    return false;

                int nVal = int.Parse(dtResult.Rows[0][0].ToString());

                return nVal == 1;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);

                return false;
            }
        }

        public bool fnbUpdateShellLastUsed(stShellConfig config)
        {
            try
            {
                bool bShellExists = ShellExists(config.ID);
                string szQuery = "";
                string szDt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

                string SqlEscape(string value) => value?.Replace("'", "''") ?? "";

                szQuery = $"UPDATE \"Shell\" SET " +
                        $"LastAccessed='{szDt}' " +
                        $"WHERE ID='{SqlEscape(config.ID)}';";

                fnSqlQuery(szQuery);

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool SaveShell(stShellConfig config)
        {
            try
            {
                bool bShellExists = ShellExists(config.ID);
                string szQuery = "";
                string szDt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

                // Helper local function to make string SQL-safe for SQLite
                string SqlEscape(string value) => value?.Replace("'", "''") ?? "";

                if (bShellExists)
                {
                    szQuery = $"UPDATE \"Shell\" SET " +
                        $"GroupName='{SqlEscape(config.szGroupName)}'," +
                        $"URL='{SqlEscape(config.szUrl)}'," +
                        $"Password='{SqlEscape(config.szPassword)}'," +
                        $"Encoding='{SqlEscape(config.szEncoding)}'," +
                        $"Language='{SqlEscape(config.language.ToString())}'," +
                        $"Method='{SqlEscape(config.szMethod)}'," +
                        $"Type='{SqlEscape(Enum.GetName(typeof(enPayloadType), config.payloadType))}'," +
                        $"Description='{SqlEscape(config.szDescription)}'," +
                        $"UserAgent='{SqlEscape(config.szUserAgent)}'," +
                        $"EHEnable='{(config.bEHEnable ? 1 : 0)}'," +
                        $"EventHorizonScript='{SqlEscape(config.szEventHorizonScript)}'," +
                        $"EventHorizonConfig='{SqlEscape(config.szEventHorizonConfig)}'," +
                        $"DriftingComet='{SqlEscape(config.szDriftingComet)}'," +
                        $"LastModified='{szDt}'," +
                        $"Timeout='{config.nTimeout.ToString()}'," +
                        $"Cookie='{SqlEscape(config.szCookie)}'," +
                        $"ExtraPost='{SqlEscape(config.szExtraPost)}'," +
                        $"ExtraPostPosition='{SqlEscape(config.nExtraPostPosition.ToString())}' " +
                        $"WHERE ID='{SqlEscape(config.ID)}';";
                }
                else
                {
                    szQuery = $"INSERT INTO \"Shell\" (" +
                        "ID, GroupName, URL, Password, Encoding, Language, Method, Type, CreateDate, LastModified, LastAccessed, " +
                        "Description, UserAgent," +
                        "EHEnable, EventHorizonScript, EventHorizonConfig," +
                        "DriftingComet," +
                        "Timeout, Cookie, ExtraPost, ExtraPostPosition" +
                        ") VALUES (" +
                        $"'{SqlEscape(config.ID)}'," +
                        $"'{SqlEscape(config.szGroupName)}'," +
                        $"'{SqlEscape(config.szUrl)}'," +
                        $"'{SqlEscape(config.szPassword)}'," +
                        $"'{SqlEscape(config.szEncoding)}'," +
                        $"'{SqlEscape(config.language.ToString())}'," +
                        $"'{SqlEscape(config.szMethod)}'," +
                        $"'{SqlEscape(Enum.GetName(typeof(enPayloadType), config.payloadType))}'," +
                        $"'{szDt}'," +
                        $"'{szDt}'," +
                        $"'{szDt}'," +
                        $"'{SqlEscape(config.szDescription)}'," +
                        $"'{SqlEscape(config.szUserAgent)}'," +
                        $"{(config.bEHEnable ? 1 : 0)}," +
                        $"'{SqlEscape(config.szEventHorizonScript)}'," +
                        $"'{SqlEscape(config.szEventHorizonConfig)}'," +
                        $"'{SqlEscape(config.szDriftingComet)}'," +
                        $"'{SqlEscape(config.nTimeout.ToString())}'," +
                        $"'{SqlEscape(config.szCookie)}'," +
                        $"'{SqlEscape(config.szExtraPost)}'," +
                        $"'{SqlEscape(config.nExtraPostPosition.ToString())}'" +
                        ");";
                }

                fnSqlQuery(szQuery); // Execute write operation.

                return ShellExists(config.ID); // Check success.
            }
            catch (Exception ex)
            {
                // Tip: Consider logging 'ex.Message' here during development so syntax errors don't hide!
                return false;
            }
        }

        public bool fnbDeleteShell(string szID)
        {
            try
            {
                string szQuery = $"DELETE FROM \"Shell\" WHERE \"ID\" = \"{szID}\";";
                fnSqlQuery(szQuery);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }

        public stShellConfig fnGetShellConfig(string szID)
        {
            string szQuery = $"SELECT * FROM \"Shell\" WHERE \"ID\" = \"{szID}\";";
            DataTable dt = fnSqlQuery(szQuery);
            DataRow dr = dt.Rows[0];

            stShellConfig config = fnDataRowToStruct(dr);

            return config;
        }

        public List<stShellConfig> fnGetAllShellConfig()
        {
            List<stShellConfig> lsConfig = new List<stShellConfig>();
            string szQuery = $"SELECT * FROM \"Shell\";";
            DataTable dt = fnSqlQuery(szQuery);
            foreach (DataRow dr in dt.Rows)
            {
                stShellConfig config = fnDataRowToStruct(dr);
                lsConfig.Add(config);
            }

            return lsConfig;
        }

        public List<stShellConfig> fnGetShellWithGroupName(string szGroupName)
        {
            if (szGroupName == "_All")
                return fnGetAllShellConfig();

            List<stShellConfig> lsConfig = new List<stShellConfig>();
            string szQuery = $"SELECT * FROM \"Shell\" WHERE \"GroupName\" = \"{szGroupName}\";";
            DataTable dt = fnSqlQuery(szQuery);
            foreach (DataRow dr in dt.Rows)
            {
                var config = fnDataRowToStruct(dr);
                lsConfig.Add(config);
            }

            return lsConfig;
        }

        public bool fnbGroupExists(string szGroupName)
        {
            return fnGetShellWithGroupName(szGroupName).Count > 0;
        }

        public void fnAddGroup(string szGroupName)
        {
            if (string.IsNullOrEmpty(szGroupName))
                throw new Exception("Group name cannot be null or empty!");

            if (fnbGroupExists(szGroupName))
                throw new Exception("Group name already exists!");

            const string szInvalid = "!@#$%^&*()-_+=/?`<>'\";~";
            foreach (var c in szInvalid)
            {
                if (szGroupName.Contains(c))
                    throw new Exception("Group name should not contain invalid chars: " + szInvalid);
            }

            string szDt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            string szQuery = $"INSERT INTO \"Group\" (Name, CreationDate) VALUES (\"{szGroupName}\", \"{szDt}\");";

            fnSqlQuery(szQuery);
        }

        public void fnRenameGroup(string szOldGroupName, string szNewGroupName)
        {
            if (szOldGroupName == "_All" || szOldGroupName == "_Orphan")
                throw new Exception("This group is not allowed to rename: " + szOldGroupName);

            if (fnbGroupExists(szNewGroupName))
                throw new Exception("Group name already exists!");

            const string szInvalid = "!@#$%^&*()-_+=/?`<>'\";~";
            foreach (var c in szInvalid)
            {
                if (szNewGroupName.Contains(c))
                    throw new Exception("Group name should not contain invalid chars: " + szInvalid);
            }

            var shells = fnGetShellWithGroupName(szOldGroupName);
            foreach (var shell in shells)
            {
                var config = shell;
                config.szGroupName = szNewGroupName;

                SaveShell(config);
            }

            fnDeleteGroup(szOldGroupName);
            fnAddGroup(szNewGroupName);
        }

        public void fnDeleteGroup(string szGroupName)
        {
            if (szGroupName == "_All" || szGroupName == "_Orphan")
                throw new Exception("This group is not allowed to delete: " + szGroupName);

            if (!fnbGroupExists(szGroupName))
                throw new Exception("Cannot find group name: " + szGroupName);

            // Delete all shells
            fnSqlQuery($"DELETE FROM \"Shell\" WHERE \"GroupName\"=\"{szGroupName}\";");

            // Delete group name
            fnSqlQuery($"DELETE FROM \"Group\" WHERE \"Name\"=\"{szGroupName}\";");
        }

        #endregion
    }
}
