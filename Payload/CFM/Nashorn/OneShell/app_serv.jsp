<%

(function() {
    response.setContentType("application/json");
    response.setCharacterEncoding("UTF-8");

    var System = java.lang.System;
    var Runtime = java.lang.Runtime;
    var BufferedReader = java.io.BufferedReader;
    var InputStreamReader = java.io.InputStreamReader;

    var osName = System.getProperty("os.name").toLowerCase();
    var isWindows = osName.indexOf("win") >= 0;

    function output(msg) {
        if (typeof echo === 'function') {
            echo(msg);
        } else {
            out.print(msg);
        }
    }

    function cleanValue(v) {
        if (v === null || v === undefined) return '';
        var str = String(v);
        str = str.replace(/\r/g, '').replace(/\n/g, '').replace(/\t/g, '');
        return str.replace(/[\x00-\x1F\x7F\uFEFF]/g, '').trim();
    }

    function execCmd(cmdArr) {
        var outputLines = [];
        try {
            var proc = Runtime.getRuntime().exec(cmdArr);
            var reader = new BufferedReader(new InputStreamReader(proc.getInputStream(), "UTF-8"));
            var line = "";
            while ((line = reader.readLine()) !== null) {
                outputLines.push(cleanValue(line));
            }
            reader.close();
            proc.waitFor();
        } catch (e) {}
        return outputLines;
    }

    function commandExists(cmd) {
        var checkCmd = isWindows ? ["cmd.exe", "/c", "where " + cmd] : ["sh", "-c", "which " + cmd];
        var res = execCmd(checkCmd);
        return res.length > 0 && res[0] !== "";
    }

    function runPowerShell(query) {
        var psCmd = '[Console]::OutputEncoding = [Text.Encoding]::UTF8; $data = @(' + query + '); if ($data.Count -gt 0) { $data | ConvertTo-Json -Depth 3 -Compress } else { "[]" }';
        var cmd = ["powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", psCmd];
        var lines = execCmd(cmd);
        var raw = lines.join("");

        if (!raw || raw === "[]" || raw === "null") return [];
        if (raw.indexOf("{") === 0) raw = "[" + raw + "]";

        try {
            return JSON.parse(raw);
        } catch(e) {
            return [];
        }
    }

    function parseWmic(wmicCmd) {
        var lines = execCmd(["cmd.exe", "/c", "wmic " + wmicCmd + " get /format:list"]);
        var rows = [];
        var current = {};

        for (var i = 0; i < lines.length; i++) {
            var line = lines[i];
            if (line === "") {
                if (Object.keys(current).length > 0) {
                    rows.push(current);
                    current = {};
                }
                continue;
            }
            var eqIdx = line.indexOf("=");
            if (eqIdx > 0) {
                var k = cleanValue(line.substring(0, eqIdx));
                var v = cleanValue(line.substring(eqIdx + 1));
                if (k !== "") current[k] = v;
            }
        }
        if (Object.keys(current).length > 0) rows.push(current);
        return rows;
    }

    function getWinData(psQuery, wmicCmd) {
        if (commandExists("powershell")) {
            var psData = runPowerShell(psQuery);
            if (psData && psData.length > 0) return psData;
        }
        if (commandExists("wmic")) return parseWmic(wmicCmd);
        return [];
    }

    function getUnixApplications() {
        var apps = [];

        if (commandExists("dpkg-query")) {
            var lines = execCmd(["sh", "-c", "dpkg-query -W -f='${Package}\t${Version}\t${Maintainer}\n' 2>/dev/null"]);
            for (var i = 0; i < lines.length; i++) {
                var parts = lines[i].split("\t");
                if (parts.length >= 2) {
                    apps.push({
                        name: cleanValue(parts[0]),
                        version: cleanValue(parts[1]),
                        vendor: cleanValue(parts[2] || ""),
                        source: "dpkg"
                    });
                }
            }
        } 
        else if (commandExists("rpm")) {
            var lines = execCmd(["sh", "-c", "rpm -qa --qf '%{NAME}\t%{VERSION}-%{RELEASE}\t%{VENDOR}\n' 2>/dev/null"]);
            for (var i = 0; i < lines.length; i++) {
                var parts = lines[i].split("\t");
                if (parts.length >= 2) {
                    apps.push({
                        name: cleanValue(parts[0]),
                        version: cleanValue(parts[1]),
                        vendor: cleanValue(parts[2] || ""),
                        source: "rpm"
                    });
                }
            }
        }

        if (commandExists("snap")) {
            var lines = execCmd(["sh", "-c", "snap list 2>/dev/null"]);
            lines.shift();
            for (var i = 0; i < lines.length; i++) {
                var cols = lines[i].split(/\s+/);
                if (cols.length >= 2) {
                    apps.push({
                        name: cleanValue(cols[0]),
                        version: cleanValue(cols[1]),
                        vendor: cleanValue(cols[4] || ""),
                        source: "snap"
                    });
                }
            }
        }

        return apps;
    }

    function getUnixServices() {
        var services = [];

        if (commandExists("systemctl")) {
            var lines = execCmd(["sh", "-c", "systemctl list-units --type=service --all --no-pager --no-legend 2>/dev/null"]);
            for (var i = 0; i < lines.length; i++) {
                var cols = lines[i].split(/\s+/);
                if (cols.length >= 4) {
                    var name = cols[0].replace(".service", "");
                    services.push({
                        name: cleanValue(name),
                        display_name: cleanValue(cols[4] || name),
                        status: (cols[2] === "active" ? "running" : "stopped"),
                        source: "systemd"
                    });
                }
            }
        }
        else if (commandExists("service")) {
            var lines = execCmd(["sh", "-c", "service --status-all 2>/dev/null"]);
            for (var i = 0; i < lines.length; i++) {
                var match = lines[i].match(/\[\s*([\+\-\?])\s*\]\s+(.+)/);
                if (match) {
                    services.push({
                        name: cleanValue(match[2]),
                        display_name: cleanValue(match[2]),
                        status: (match[1] === "+" ? "running" : "stopped"),
                        source: "sysvinit"
                    });
                }
            }
        }

        return services;
    }

    var result = {
        success: false,
        system_type: isWindows ? "windows" : "unix_like",
        os_raw: String(osName),
        error: "",
        data: {}
    };

    try {
        if (isWindows) {
            var psApps = "Get-ChildItem 'HKLM:\\Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\', 'HKLM:\\Software\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\' -ErrorAction SilentlyContinue | ForEach-Object { try { Get-ItemProperty $_.PSPath -ErrorAction Stop } catch {} } | Where-Object DisplayName | Select-Object @{N='name';E={$_.DisplayName}}, @{N='version';E={$_.DisplayVersion}}, @{N='vendor';E={$_.Publisher}}, @{N='installed';E={$_.InstallDate}}";
            var psServ = "Get-Service | ForEach-Object { @{ name = $_.Name; display_name = $_.DisplayName; status = if ($_.Status -eq 'Running') { 'running' } else { 'stopped' }; start_type = $_.StartType.ToString() } }";

            result.data = {
                applications: getWinData(psApps, "product"),
                services: getWinData(psServ, "service"),
                user_accounts: getWinData("Get-CimInstance Win32_UserAccount", "useraccount"),
                user_profiles: getWinData("Get-CimInstance Win32_UserProfile", "path Win32_UserProfile"),
                groups: getWinData("Get-CimInstance Win32_Group", "group")
            };
        } else {
            result.data = {
                applications: getUnixApplications(),
                services: getUnixServices(),
                user_accounts: [],
                user_profiles: [],
                groups: []
            };
        }
        result.success = true;
    } catch (e) {
        result.error = String(e.message || e);
    }

    output(JSON.stringify(result));
})();

%>