<%

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

for (var dbName in dbDrivers) {
    if (dbDrivers.hasOwnProperty(dbName)) {
        var driverClass = dbDrivers[dbName];
        var available = 0;
        
        try {
            java.lang.Class.forName(driverClass, false, java.lang.Thread.currentThread().getContextClassLoader());
            available = 1;
        } catch (e) {
            available = 0;
        }
        
        aResult.push(dbName + ":" + available);
    }
}

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

Echo(aResult.join(",") + ",");

%>