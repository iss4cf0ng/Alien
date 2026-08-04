using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Alien
{
    public partial class frmRename : BaseForm
    {
        private clsfnFileMgr? m_fileMgr { get; init; } = null;
        private clsfnWinReg? m_winReg { get; init; } = null;

        private string? m_szFilePath { get; init; } = null;

        private string? m_szRegDir { get; init; } = null;
        private string? m_szRegName { get; init; } = null;

        private bool m_bRegKey { get; init; }

        public frmRename(clsfnWinReg winReg, bool bKey, string szRegDir, string szRegName)
        {
            InitializeComponent();

            m_winReg = winReg;
            m_bRegKey = bKey;
            m_szRegDir = szRegDir;
            m_szRegName = szRegName;
        }

        public frmRename(clsfnFileMgr fileMgr, string szSrcPath)
        {
            InitializeComponent();

            m_fileMgr = fileMgr;
            m_szFilePath = szSrcPath;
        }

        void fnSetup()
        {
            Text = "Rename";
        }

        private void frmRename_Load(object sender, EventArgs e)
        {
            fnSetup();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(textBox1.Text))
                    throw new Exception("New name is null or empty.");

                if (m_fileMgr != null)
                {
                    if (string.IsNullOrEmpty(m_szFilePath))
                        throw new Exception("Path is null or empty.");

                    string? szDirName = Path.GetDirectoryName(m_szFilePath);
                    if (string.IsNullOrEmpty(szDirName))
                        throw new Exception("Directory name is null or empty.");

                    bool bVal = await m_fileMgr.fnbMove(m_szFilePath, Path.Combine(szDirName, textBox1.Text).Replace("\\", "/"));
                    if (!bVal)
                        throw new Exception("Failed to rename: " + m_szFilePath);

                    MessageBox.Show("Renamed successfully.", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    DialogResult = DialogResult.OK;
                }
                else if (m_winReg != null)
                {
                    if (string.IsNullOrEmpty(m_szRegDir))
                        throw new Exception("Registry path is null or empty.");

                    if (string.IsNullOrEmpty(m_szRegName))
                        throw new Exception("Name is null or empty");

                    string szSrcPath = Path.Combine(m_szRegDir, m_szRegName);
                    string szDstPath = Path.Combine(m_szRegDir, textBox1.Text);

                    bool bVal = m_bRegKey ? await m_winReg.fnbRenameKey(szSrcPath, szDstPath) : await m_winReg.fnbRenameValue(m_szRegDir, m_szRegName, textBox1.Text);
                    if (!bVal)
                        throw new Exception("Failed to rename: " + m_szRegName);

                    MessageBox.Show("Renamed successfully.", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    DialogResult = DialogResult.OK;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
