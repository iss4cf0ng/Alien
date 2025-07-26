using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.ComponentModel;
using System.Data;
using System.Security.Cryptography.X509Certificates;

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
                    "Group",
                    "URL",
                    "Password",
                    "Encoding",
                    "Language",
                    "Method",
                    "Type",
                    "CreateDate",
                    "LastModified",
                    "LastAccessed",
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
                szUrl = dr["URL"].ToString(),
                szPassword = dr["Password"].ToString(),
                szEncoding = dr["Encoding"].ToString(),
                language = (Language)Enum.Parse(typeof(Language), dr["Language"].ToString()),
                payloadType = (PayloadType)Enum.Parse(typeof(PayloadType), dr["Type"].ToString()),
                szMethod = dr["Method"].ToString(),
                dtCreateDate = DateTime.Parse(dr["CreateDate"].ToString()),
                dtLastModified = DateTime.Parse(dr["LastModified"].ToString()),
                dtLastAccessed = DateTime.Parse(dr["LastAccessed"].ToString()),
            };

            return config;
        }

        #endregion
        #region Shell

        public bool ShellExists(string szUrl)
        {
            try
            {
                string szQuery = $"SELECT 1 FROM `Shell` WHERE URL = \"{szUrl}\";";
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

        public bool SaveShell(stShellConfig config)
        {
            try
            {
                bool bShellExists = ShellExists(config.szUrl);
                string szQuery = "";
                DateTime dt = DateTime.Now;
                string szDt = dt.ToString("F");

                if (bShellExists)
                {
                    //Update shell config.

                    szQuery = $"UPDATE \"Shell\" SET " +
                        $"URL=\"{config.szUrl}\"," +
                        $"Password=\"{config.szPassword}\"," +
                        $"Encoding=\"{config.szEncoding}\"," +
                        $"Language=\"{config.language}\"," +
                        $"Method=\"{config.szMethod}\"," +
                        $"Type=\"{config.payloadType}\"," +
                        $"LastModified=\"{DateTime.Now.ToString("F")}\" " +
                        $"WHERE ID=\"{config.ID}\";";
                }
                else
                {
                    //Add shell config.

                    szQuery = $"INSERT INTO \"Shell\" (" +

                        "ID," +
                        $"URL," +
                        $"Password," +
                        $"Encoding," +
                        $"Language," +
                        $"Method," +
                        $"Type," +
                        $"CreateDate," +
                        $"LastModified," +
                        $"LastAccessed" +

                        $") VALUES (" +

                        $"\"{config.ID}\"," +
                        $"\"{config.szUrl}\"," +
                        $"\"{config.szPassword}\"," +
                        $"\"{config.szEncoding}\"," +
                        $"\"{config.language.ToString()}\"," +
                        $"\"{config.szMethod}\"," +
                        $"\"{config.payloadType}\"," +
                        $"\"{szDt}\"," +
                        $"\"{szDt}\"," +
                        $"\"{szDt}\"" +

                        $");";
                }

                fnSqlQuery(szQuery); //Execute write operation.

                return ShellExists(config.szUrl); //Check successed.
            }
            catch (Exception ex)
            {
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

        public stShellConfig fnGetShellConfig(string szUrl)
        {
            string szQuery = $"SELECT * FROM \"Shell\" WHERE \"URL\" = \"{szUrl}\";";
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
            if (szGroupName == "All")
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

        #endregion
    }
}
