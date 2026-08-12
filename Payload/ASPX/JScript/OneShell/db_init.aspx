<%

Response.Buffer = true;

var checks = new System.Collections.Hashtable();

function checkOleDbProvider(providerName) {
    try {
        var conn = System.Activator.CreateInstance(System.Type.GetTypeFromProgID("ADODB.Connection", true));
        conn.Provider = providerName;
        return true;
    } catch (e) {
        return false;
    }
}

checks["MySQL ODBC"] = checkOleDbProvider("MSDASQL");
checks["SQL Server (OLE DB)"] = checkOleDbProvider("SQLOLEDB");
checks["Oracle (OLE DB/ODBC)"] = checkOleDbProvider("OraOLEDB.Oracle");
checks["SQLite (ODBC)"] = checkOleDbProvider("MSDASQL");
checks["PostgreSQL (ODBC)"] = checkOleDbProvider("MSDASQL");

checks["Redis"] = false;
checks["MongoDB"] = false;

var output =
    "MySQL ODBC:" + (checks["MySQL ODBC"] ? 1 : 0) + "," +
    "SQL Server (OLE DB):" + (checks["SQL Server (OLE DB)"] ? 1 : 0) + "," +
    "Oracle (OLE DB/ODBC):" + (checks["Oracle (OLE DB/ODBC)"] ? 1 : 0) + "," +
    "SQLite (ODBC):" + (checks["SQLite (ODBC)"] ? 1 : 0) + "," +
    "PostgreSQL (ODBC):" + (checks["PostgreSQL (ODBC)"] ? 1 : 0) + "," +
    "Redis:" + (checks["Redis"] ? 1 : 0) + "," +
    "MongoDB:" + (checks["MongoDB"] ? 1 : 0);

Response.Write(output);

%>