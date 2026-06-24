<%

// 1. 定義 Java 經典的資料庫驅動類別對照表
var dbDrivers = {
    "MySQL (Legacy)": "com.mysql.jdbc.Driver",
    "MySQL (Modern)": "com.mysql.cj.jdbc.Driver",
    "PostgreSQL": "org.postgresql.Driver",
    "SQLite": "org.sqlite.JDBC",
    "Oracle": "oracle.jdbc.driver.OracleDriver",
    "Microsoft SQL Server": "com.microsoft.sqlserver.jdbc.SQLServerDriver",
    "Apache Derby": "org.apache.derby.jdbc.EmbeddedDriver",
    "H2 Database": "org.h2.Driver",
    "HSQLDB": "org.hsqldb.jdbc.JDBCDriver",
    "DB2": "com.ibm.db2.jcc.DB2Driver",
    "Sybase": "com.sybase.jdbc4.jdbc.SybDriver"
};

var aResult = [];

// 2. 遍歷檢查每個驅動類別是否能被當前 JVM 載入
for (var dbName in dbDrivers) {
    if (dbDrivers.hasOwnProperty(dbName)) {
        var driverClass = dbDrivers[dbName];
        var available = 0;
        
        try {
            // 利用當前執行緒的 ClassLoader 嘗試去撈這個類別
            java.lang.Class.forName(driverClass, false, java.lang.Thread.currentThread().getContextClassLoader());
            available = 1;
        } catch (e) {
            // 找不到類別 (ClassNotFoundException) 代表沒加載此驅動
            available = 0;
        }
        
        aResult.push(dbName + ":" + available);
    }
}

// 3. 【進階加碼】Java 常見的快取與記憶體資料庫連線物件檢查 (如 Redis, MongoDB)
var nosqlChecks = {
    "Jedis (Redis)": "redis.clients.jedis.Jedis",
    "Lettuce (Redis)": "io.lettuce.core.RedisClient",
    "MongoDB Java Driver": "com.mongodb.client.MongoClient"
};

for (var nosqlName in nosqlChecks) {
    if (nosqlChecks.hasOwnProperty(nosqlName)) {
        var cls = nosqlChecks[nosqlName];
        var av = 0;
        try {
            java.lang.Class.forName(cls, false, java.lang.Thread.currentThread().getContextClassLoader());
            av = 1;
        } catch (e) { av = 0; }
        
        aResult.push(nosqlName + ":" + av);
    }
}

// 4. 輸出結果，完美對齊 PHP 的 "格式:,格式:," 結尾
Echo(aResult.join(",") + ",");

%>