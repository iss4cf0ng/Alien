using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Alien
{
    public class clsIniMgr
    {
        private string m_szIniFilePath { get; set; }

        public clsIniMgr(string szIniFilePath)
        {
            m_szIniFilePath = szIniFilePath;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern long WritePrivateProfileString(string szSection, string szKey, string szValue, string szFilePath);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern int GetPrivateProfileString(string szSectiom, string szKey, string szDefaultValue, StringBuilder sbRetVal, int nSize, string szFilePath);

        public string fnszRead(string szSection, string szKey)
        {
            var sbRetVal = new StringBuilder(255);
            GetPrivateProfileString(szSection, szKey, string.Empty, sbRetVal, sbRetVal.Length, m_szIniFilePath);
            return sbRetVal.ToString();
        }

        public void fnWrite(string szSection, string szKey, string szValue)
        {
            WritePrivateProfileString(szSection, szKey, szValue, m_szIniFilePath);
        }
    }
}
