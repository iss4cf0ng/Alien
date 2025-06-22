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
    public class Sqlite
    {
        public string m_szFileName { get; set; }
        public string m_szConnString { get; set; }

        private Dictionary<string, string[]> m_dicTable = new Dictionary<string, string[]>
        {
            {
                "Shell",
                new string[]
                {
                    "URL",
                    "Password",
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

        public Sqlite(SQLiteConnection sqlConn)
        {
            m_sqlConn = sqlConn;
        }

        public Sqlite(string szFileName)
        {
            m_szFileName = szFileName;

            if (!File.Exists(m_szFileName) && !CreateDB())
                throw new Exception("CreateDB() error.");
            else
            {
                m_szConnString = $"Data Source=\"{m_szFileName}\"";
                m_sqlConn = new SQLiteConnection(m_szConnString);
                m_sqlConn.Open();
            }
        }

        #region Basic

        public bool CreateDB()
        {
            try
            {
                m_szConnString = $"Data Source=\"{m_szFileName}\"";
                m_sqlConn = new SQLiteConnection(m_szConnString);

                foreach (string szKey in m_dicTable.Keys)
                {
                    if (!CreateTable(szKey, m_dicTable[szKey]))
                        throw new Exception("CreateTable() error.");
                }

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool CreateTable(string szTableName, string[] aszColumns)
        {
            try
            {
                string szQuery = $"CREATE TABLE \"{szTableName}\" ({string.Join(",", aszColumns.Select(x => $"{x} TEXT"))})";
                DataTable dt = SqlQuery(szQuery);

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public DataTable SqlQuery(string szQuery)
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
                    MessageBox.Show(ex.Message, "SqlQuery()", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            return dt;
        }

        #endregion

        #region Shell

        public bool ShellExists(string szUrl)
        {
            try
            {
                string szQuery = $"SELECT 1 FROM `Shell` WHERE URL = \"{szUrl}\";";
                DataTable dtResult = SqlQuery(szQuery);
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

                    szQuery = $"";
                }
                else
                {
                    //Modify shell config.

                    szQuery = $"INSERT INTO \"Shell\" (" +

                        $"URL," +
                        $"Password," +
                        $"Type," +
                        $"CreateDate," +
                        $"LastModified," +
                        $"LastAccessed" +

                        $") VALUES (" +

                        $"\"{config.szUrl}\"," +
                        $"\"{config.szPassword}\"," +
                        $"\"{config.language.ToString()}\"," +
                        $"\"{szDt}\"," +
                        $"\"{szDt}\"," +
                        $"\"{szDt}\"" +

                        $");";
                }

                SqlQuery(szQuery); //Execute write operation.

                return ShellExists(config.szUrl); //Check successed.
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public stShellConfig fnGetShellConfig(string szUrl)
        {
            stShellConfig config = new stShellConfig();

            return config;
        }

        public List<stShellConfig> fnGetAllShellConfig()
        {
            List<stShellConfig> lsConfig = new List<stShellConfig>();


            return lsConfig;
        }

        #endregion
    }
}
