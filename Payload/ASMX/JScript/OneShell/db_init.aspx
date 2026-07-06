<%

Response.Buffer = true;

var checks = new System.Collections.Hashtable();

function checkProvider(progId) {
    try {
        var t = System.Type.GetTypeFromProgID(progId, true);
        var obj = System.Activator.CreateInstance(t);
        return true;
    } catch (e) {
        return false;
    }
}

checks["MySQL ODBC"] = checkProvider("ADODB.Connection");
checks["SQL Server (OLE DB)"] = checkProvider("ADODB.Connection");
checks["Oracle (OLE DB/ODBC)"] = checkProvider("ADODB.Connection");
checks["SQLite (ODBC)"] = checkProvider("ADODB.Connection");
checks["PostgreSQL (ODBC)"] = checkProvider("ADODB.Connection");

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