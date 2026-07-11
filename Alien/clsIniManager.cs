using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace Alien
{
    internal sealed class clsIniManager
    {
        private readonly string m_szPath;
        private readonly Dictionary<string, Dictionary<string, string>> m_dicData = new(StringComparer.OrdinalIgnoreCase);
        
        public clsIniManager(string szPath)
        {
            m_szPath = szPath;
            if (File.Exists(szPath))
                fnLoad();
        }

        private void fnLoad()
        {
            string? szSection = null;

            foreach (string szRawLine in File.ReadAllLines(m_szPath, Encoding.UTF8))
            {
                string szLine = szRawLine.Trim();

                if (string.IsNullOrEmpty(szLine))
                    continue;

                if (szLine.StartsWith(";") || szLine.StartsWith("#"))
                    continue;

                if (szLine.StartsWith('[') && szLine.EndsWith(']'))
                {
                    szSection = szLine[1..^1].Trim();

                    if (!m_dicData.ContainsKey(szSection))
                        m_dicData[szSection] = new(StringComparer.OrdinalIgnoreCase);

                    continue;
                }

                int index = szLine.IndexOf('=');

                if (index < 0 || szSection == null)
                    continue;

                string key = szLine[..index].Trim();
                string value = szLine[(index + 1)..].Trim();

                m_dicData[szSection][key] = value;
            }
        }

        public void fnSave()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var section in m_dicData)
            {
                sb.AppendLine($"[{section.Key}]");

                foreach (var kv in section.Value)
                    sb.AppendLine($"{kv.Key}={kv.Value}");

                sb.AppendLine();
            }

            File.WriteAllText(m_szPath, sb.ToString(), Encoding.UTF8);
        }

        public string ReadString(string section, string key, string defaultValue = "")
        {
            if (m_dicData.TryGetValue(section, out var sec) &&
                sec.TryGetValue(key, out var value))
            {
                return value;
            }

            return defaultValue;
        }

        public int ReadInt(string section, string key, int defaultValue = 0)
        {
            return int.TryParse(ReadString(section, key), out int value)
                ? value
                : defaultValue;
        }

        public bool ReadBool(string section, string key, bool defaultValue = false)
        {
            return bool.TryParse(ReadString(section, key), out bool value)
                ? value
                : defaultValue;
        }

        public double ReadDouble(string section, string key, double defaultValue = 0)
        {
            return double.TryParse(ReadString(section, key), out double value)
                ? value
                : defaultValue;
        }

        public void Write(string section, string key, object value)
        {
            if (!m_dicData.TryGetValue(section, out var sec))
            {
                sec = new(StringComparer.OrdinalIgnoreCase);
                m_dicData[section] = sec;
            }

            sec[key] = value.ToString() ?? "";

            fnSave();
        }

        public bool SectionExists(string section)
        {
            return m_dicData.ContainsKey(section);
        }

        public bool KeyExists(string section, string key)
        {
            return m_dicData.TryGetValue(section, out var sec) && sec.ContainsKey(key);
        }
    }
}
