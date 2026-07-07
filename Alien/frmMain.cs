using Microsoft.VisualBasic;
using System.Text.RegularExpressions;

namespace Alien
{
    public partial class frmMain : Form
    {
        private const string m_szName = "Alien";
        private const string m_szVersion = "v5.0.0";
        private const string m_szAuthor = "iss4cf0ng/ISSAC";

        private clsTamper m_tamper { get; set; }

        private List<string> m_lsGroupName
        {
            get
            {
                if (treeView1.Nodes.Count == 0)
                    return new List<string>();

                return treeView1.Nodes[0].Nodes.Cast<TreeNode>().Select(n => n.Text).ToList();
            }
        }

        public clsSqlite m_sqlConn;
        private string[] m_generalGroup =
        {
            "_All",
            "_Orphan",
        };

        public frmMain()
        {
            InitializeComponent();
        }

        #region Tool

        private clsWeb fnGetVictimTag(ListViewItem item) => (clsWeb)item.Tag;
        private List<clsWeb> fnGetVictimList(ListView lv) => lv.SelectedItems.Cast<ListViewItem>().Select(x => fnGetVictimTag(x)).ToList();
        private List<stShellConfig> fnSearchShell(string szPattern)
        {
            List<stShellConfig> lsConfig = m_sqlConn.fnGetAllShellConfig();
            List<stShellConfig> lsResult = new List<stShellConfig>();

            foreach (var config in lsConfig)
            {
                if (
                    Regex.IsMatch(config.szUrl, szPattern)
                    || Regex.IsMatch(config.ID, szPattern)
                    || Regex.IsMatch(config.language.ToString(), szPattern)
                    || Regex.IsMatch(config.szEncoding.ToString(), szPattern)
                )
                {
                    lsResult.Add(config);
                }
            }

            return lsResult;
        }

        #endregion

        /// <summary>
        /// Load shell from SQLite database and display them in listview.
        /// </summary>
        /// <param name="lsConfig"></param>
        void fnLoadShell(List<stShellConfig> lsConfig = null)
        {
            //todo: Dispose all exist clsWeb

            listView1.Items.Clear();

            if (lsConfig == null)
                lsConfig = m_sqlConn.fnGetAllShellConfig();

            foreach (var config in lsConfig)
            {
                // ListView

                ListViewItem item = new ListViewItem(config.ID);
                item.SubItems.Add(config.szUrl);
                item.SubItems.Add(config.language.ToString());
                item.SubItems.Add(config.szDescription);
                item.SubItems.Add(config.dtCreateDate.ToString("F"));
                item.SubItems.Add(config.dtLastModified.ToString("F"));
                item.SubItems.Add(config.dtLastAccessed.ToString("F"));

                item.ImageKey = "unknown";

                clsVictim victim = new clsVictim(m_sqlConn, config, false);
                victim.fnbBuildPortfolio();
                clsWeb web = new clsWeb(victim, m_tamper);

                item.Tag = web;

                listView1.Items.Add(item);

                // TreeView
                TreeNode node = treeView1.Nodes[0];
                var lsGroupName = m_lsGroupName;
                if (!string.IsNullOrEmpty(config.szGroupName) && !lsGroupName.Contains(config.szGroupName))
                {
                    TreeNode nodeNew = new TreeNode(config.szGroupName);
                    node.Nodes.Add(nodeNew);
                }
            }

            fnUpdateState();
        }

        void fnUpdateState()
        {
            treeView1.Refresh();
            listView1.Refresh();

            Text = $"{m_szName} {m_szVersion} by {m_szAuthor} | Selected[{listView1.SelectedItems.Count}]";
            toolStripStatusLabel1.Text = $"Shell[{listView1.Items.Count}]";
            toolStripStatusLabel3.Text = "iss4cf0ng/ISSAC";
        }

        async Task fnSetup()
        {
            toolStripStatusLabel1.Text = string.Empty;
            toolStripStatusLabel4.Text = string.Empty;

            clsSqlite sqlConn = new clsSqlite("data.sqlite");
            m_sqlConn = sqlConn;

            TreeNode node = new TreeNode("Group");
            node.Nodes.Add(new TreeNode("_All"));
            node.Nodes.Add(new TreeNode("_Orphan"));

            treeView1.Nodes.Add(node);
            treeView1.ExpandAll();

            toolStripStatusLabel4.Text = "Loading...";

            fnLoadShell();

            m_tamper = new clsTamper("http://127.0.0.1:8000", "python", "Tamper\\server.py");
            await m_tamper.fnInitializeServerAsync();

            toolStripStatusLabel4.Text = "Action successfully";

            return;

            string szInput = "ABCDEFGHIJK";
            var parameters = new Dictionary<string, object>
            {
                { "key", "123" }
            };

            string szPayload = await m_tamper.fnObfuscate("RC4", szInput, parameters);

            Visible = false;
            MessageBox.Show(szPayload);
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            await fnSetup();
        }

        //Control Panel
        private async void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listView1.SelectedItems)
            {
                toolStripStatusLabel4.Text = "Loading...";

                clsWeb web = fnGetVictimTag(item);
                if (web.m_tamper == null)
                    web.m_tamper = m_tamper;

                frmControlPanel? frmOpened = clsTool.fnFindForm<frmControlPanel>(web);
                if (frmOpened != null)
                {
                    frmOpened.BringToFront();
                    continue;
                }

                if (await web.fnbTestWebConnection() && await web.fnbTestShellConnection())
                {
                    string szDomain = item.SubItems[1].Text.Split('/')[2];

                    frmControlPanel f = new frmControlPanel(web);
                    f.Text = $"{szDomain} | " +
                        $"{Enum.GetName(typeof(enLanguage), web.m_victim.ShellLanguage)} | " +
                        $"{Enum.GetName(typeof(enPayloadType), web.m_victim.ShellPayloadType)} | " +
                        $"{web.m_victim.m_ShellConfig.szMethod}" + (web.m_victim.m_ShellConfig.bEHEnable ? " | " + web.m_victim.m_ShellConfig.szEventHorizonScript : string.Empty);

                    f.Show();
                }

                toolStripStatusLabel4.Text = "Action successfully";
            }
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            frmEditShell f = new frmEditShell(m_tamper, m_sqlConn, new stShellConfig(), true, m_lsGroupName);
            f.StartPosition = FormStartPosition.CenterScreen;
            f.Text = "Add Shell";

            f.ShowDialog();

            fnLoadShell();
        }

        private void toolStripMenuItem8_Click(object sender, EventArgs e)
        {
            frmEditShell f = new frmEditShell(m_tamper, m_sqlConn, new stShellConfig(), true, m_lsGroupName);
            f.StartPosition = FormStartPosition.CenterScreen;
            f.Text = "Add Shell";

            f.ShowDialog();

            fnLoadShell();
        }

        private void listView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
            {
                fnLoadShell();
            }
            else if (e.KeyCode == Keys.Enter)
            {
                List<clsWeb> lc = fnGetVictimList(listView1);
                if (lc.Count == 0)
                    return;
                else if (lc.Count > 1)
                    MessageBox.Show("Multiple shells selected, the first shell will be automatically chosen.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                clsWeb web = lc[0];
                frmEditShell f = new frmEditShell(m_tamper, m_sqlConn, web.m_victim.m_ShellConfig, false, m_lsGroupName);
                f.StartPosition = FormStartPosition.CenterScreen;
                f.Text = "Edit Shell";

                f.ShowDialog();

                fnLoadShell();
            }
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            frmSetting f = new frmSetting();
            f.Show();
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            frmAbout f = new frmAbout();
            f.Show();
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            List<clsWeb> lc = fnGetVictimList(listView1);
            if (lc.Count == 0)
                return;
            else if (lc.Count > 1)
                MessageBox.Show("Multiple shells selected, the first shell will be automatically chosen.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            clsWeb web = lc[0];
            frmEditShell f = new frmEditShell(m_tamper, m_sqlConn, web.m_victim.m_ShellConfig, false, m_lsGroupName);
            f.StartPosition = FormStartPosition.CenterScreen;
            f.Text = "Edit Shell";

            f.ShowDialog();

            fnLoadShell();
        }

        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            List<string> lsID = listView1.SelectedItems.Cast<ListViewItem>().Select(x => x.Text).ToList();
            if (lsID.Count == 0)
                return;

            DialogResult dr = MessageBox.Show($"Delete {lsID.Count} shell{(lsID.Count > 1 ? "s" : string.Empty)}, are you sure?", "Sure?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dr == DialogResult.Yes)
            {
                foreach (string szID in lsID)
                    m_sqlConn.fnbDeleteShell(szID);

                fnLoadShell();
            }
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                fnLoadShell(fnSearchShell(textBox1.Text));
            }
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            TreeNode node = treeView1.SelectedNode;
            if (node.Parent == null)
                return;

            var ls = m_sqlConn.fnGetShellWithGroupName(node.Text);
            fnLoadShell(ls);
        }

        private void toolStripMenuItem5_Click(object sender, EventArgs e)
        {

        }

        private void listView1_DoubleClick(object sender, EventArgs e)
        {
            List<clsWeb> lc = fnGetVictimList(listView1);
            if (lc.Count == 0)
                return;
            else if (lc.Count > 1)
                MessageBox.Show("Multiple shells selected, the first shell will be automatically chosen.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            clsWeb web = lc[0];
            frmEditShell f = new frmEditShell(m_tamper, m_sqlConn, web.m_victim.m_ShellConfig, false, m_lsGroupName);
            f.StartPosition = FormStartPosition.CenterScreen;
            f.Text = "Edit Shell";

            f.ShowDialog();

            fnLoadShell();
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            fnUpdateState();
        }

        private async void toolStripButton1_Click(object sender, EventArgs e)
        {
            try
            {
                toolStripStatusLabel4.Text = "Loading...";

                int nThread = int.Parse(Interaction.InputBox("Thread count:", "Check Alive", "3"));
                if (nThread <= 0)
                    throw new Exception("Invalid number.");

                toolStripProgressBar1.Value = 0;
                toolStripProgressBar1.Maximum = listView1.Items.Count;

                Dictionary<clsWeb, ListViewItem> dic = new Dictionary<clsWeb, ListViewItem>();
                foreach (ListViewItem item in listView1.Items)
                    dic.Add(fnGetVictimTag(item), item);

                var semaphore = new SemaphoreSlim(nThread);
                List<Task> lsTask = new List<Task>();

                foreach (clsWeb web in dic.Keys)
                {
                    lsTask.Add(Task.Run(async () =>
                    {
                        await semaphore.WaitAsync();

                        try
                        {
                            bool bAlive = await web.fnbTestWebConnection(false) && await web.fnbTestShellConnection(false);
                            Invoke(() => dic[web].ImageKey = bAlive ? "yes" : "no");
                        }
                        finally
                        {
                            semaphore.Release();

                            Invoke(() => toolStripProgressBar1.Increment(1));
                        }
                    }));
                }

                await Task.WhenAll(lsTask);

                MessageBox.Show("Completed, please check.", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                toolStripStatusLabel4.Text = "Tasks are finished";
            }
        }

        private void toolStripMenuItem7_Click(object sender, EventArgs e)
        {
            frmBuilder f = new frmBuilder(m_tamper);
            f.StartPosition = FormStartPosition.CenterScreen;

            f.Show();
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            m_tamper.Dispose();
        }
    }
}
