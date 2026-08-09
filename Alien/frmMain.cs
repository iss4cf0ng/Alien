using Microsoft.VisualBasic;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using static Alien.clsThemeManager;

namespace Alien
{
    public partial class frmMain : Form
    {
        private const string m_szName = "Alien";
        private const string m_szVersion = "v5.1.0";
        private const string m_szAuthor = "iss4cf0ng/ISSAC";

        private clsTamper m_tamper { get; set; }
        private clsIniManager m_iniMgr { get; init; }

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

            ThemeManager.Apply(this);

            m_iniMgr = new clsIniManager("config.ini");
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
                item.SubItems.Add(config.dtCreateDate.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                item.SubItems.Add(config.dtLastModified.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                item.SubItems.Add(config.dtLastAccessed.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));

                item.ImageKey = "unknown";

                clsVictim victim = new clsVictim(m_sqlConn, config, false);
                victim.fnbBuildPortfolio();
                clsWeb web = new clsWeb(victim, m_tamper, m_sqlConn);

                item.Tag = web;

                listView1.Items.Add(item);
            }

            fnUpdateState();
        }

        void fnLoadGroup()
        {
            TreeNode node = treeView1.Nodes[0];
            node.Nodes.Clear();

            node.Nodes.Add("_All");
            node.Nodes.Add("_Orphan");

            foreach (var group in m_sqlConn.fnGetGroups())
                node.Nodes.Add(group);

            node.Expand();
            treeView1.Refresh();
        }

        void fnUpdateState()
        {
            Text = $"{m_szName} {m_szVersion} by {m_szAuthor} | Tamper sever: {m_tamper.m_szPyServerUri} | Selected[{listView1.SelectedItems.Count}]";
            toolStripStatusLabel1.Text = $"Shell[{listView1.Items.Count}]";
            toolStripStatusLabel3.Text = "iss4cf0ng/ISSAC";
        }

        async Task fnSetup()
        {
            int nPort = m_iniMgr.ReadInt("General", "Port");
            string szPyExec = m_iniMgr.ReadString("General", "Python");
            m_tamper = new clsTamper($"http://127.0.0.1:{nPort}", szPyExec, "EventHorizon\\server.py");
            if (new frmEnvChecker(m_tamper, m_iniMgr).ShowDialog() != DialogResult.OK)
            {
                MessageBox.Show("Failed to check environment!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                return;
            }

            toolStripStatusLabel1.Text = string.Empty;
            toolStripStatusLabel4.Text = string.Empty;

            clsSqlite sqlConn = new clsSqlite("data.sqlite");
            m_sqlConn = sqlConn;

            ListViewColumnSorter lvwSorter = new ListViewColumnSorter();
            listView1.ListViewItemSorter = lvwSorter;

            int nIdx = listView1.Columns.Count - 1;
            ListViewHeaderChanger.SortOrder defaultOrder = ListViewHeaderChanger.SortOrder.Descending;

            lvwSorter.SortColumn = nIdx;
            lvwSorter.Order = defaultOrder == ListViewHeaderChanger.SortOrder.Descending ? SortOrder.Descending : SortOrder.Ascending;

            listView1.Sort();
            listView1.SetSortArrow(nIdx, defaultOrder);

            listView1.ColumnClick += (s, e) =>
            {
                if (e.Column == lvwSorter.SortColumn)
                {
                    if (lvwSorter.Order == SortOrder.Ascending)
                        lvwSorter.Order = SortOrder.Descending;
                    else
                        lvwSorter.Order = SortOrder.Ascending;
                }
                else
                {
                    lvwSorter.SortColumn = e.Column;
                    lvwSorter.Order = SortOrder.Ascending;
                }

                listView1.Sort();

                ListViewHeaderChanger.SortOrder arrowOrder = lvwSorter.Order == SortOrder.Ascending ? ListViewHeaderChanger.SortOrder.Ascending : ListViewHeaderChanger.SortOrder.Descending;
                listView1.SetSortArrow(e.Column, arrowOrder);
            };

            TreeNode node = new TreeNode("Group");
            node.Nodes.Add(new TreeNode("_All"));
            node.Nodes.Add(new TreeNode("_Orphan"));

            treeView1.Nodes.Add(node);

            treeView1.ExpandAll();

            toolStripStatusLabel4.Text = "Loading shells...";

            fnLoadShell();

            // Add groups
            fnLoadGroup();

            toolStripStatusLabel4.Text = "Action successfully";
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            await fnSetup();
        }

        //Control Panel
        private async void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            bool bDoHttpGet = m_iniMgr.ReadBool("General", "DoHttpGet");
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

                //await web.fnbTestWebConnection(true) && await web.fnbTestShellConnection()
                bool bValidate = true;
                if (bDoHttpGet)
                    bValidate = await web.fnbTestWebConnection();

                bValidate = bValidate && await web.fnbTestShellConnection();

                if (bValidate)
                {
                    string szDomain = item.SubItems[1].Text.Split('/')[2];

                    frmControlPanel f = new frmControlPanel(web, m_iniMgr);
                    f.Text = $"{szDomain} | " +
                        $"{Enum.GetName(typeof(enLanguage), web.m_victim.ShellLanguage)} | " +
                        $"{web.m_victim.m_ShellConfig.szMethod} | " +
                        $"{Enum.GetName(typeof(enPayloadType), web.m_victim.ShellPayloadType)} | " +
                        (web.m_victim.m_ShellConfig.bEHEnable ? " | " + web.m_victim.m_ShellConfig.szEventHorizonScript : string.Empty);

                    f.Show();

                    item.ImageKey = "yes";

                    m_sqlConn.fnbUpdateShellLastUsed(web.m_victim.m_ShellConfig);
                }
                else
                {
                    item.ImageKey = "no";
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
            fnLoadGroup();
        }

        private void toolStripMenuItem8_Click(object sender, EventArgs e)
        {
            frmEditShell f = new frmEditShell(m_tamper, m_sqlConn, new stShellConfig(), true, m_lsGroupName);
            f.StartPosition = FormStartPosition.CenterScreen;
            f.Text = "Add Shell";

            f.ShowDialog();

            fnLoadShell();
            fnLoadGroup();
        }

        private void listView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
            {
                fnLoadShell();
                fnLoadGroup();
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
                fnLoadGroup();
            }
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            frmSetting f = new frmSetting();
            f.ShowDialog();
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
            fnLoadGroup();
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
            foreach (ListViewItem item in listView1.SelectedItems)
            {
                try
                {
                    if (item.Tag == null)
                    {
                        MessageBox.Show("Item tag is null!", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        continue;
                    }

                    clsWeb web = fnGetVictimTag(item);
                    string szDir = web.m_victim.m_szPortfolio;

                    if (!Directory.Exists(szDir))
                    {
                        MessageBox.Show("Directory does not exist: " + szDir, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        continue;
                    }

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = szDir,
                        UseShellExecute = true,
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
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
            fnLoadGroup();
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            fnUpdateState();
        }

        private void toolStripMenuItem7_Click(object sender, EventArgs e)
        {
            frmBuilder f = new frmBuilder(m_tamper);
            f.StartPosition = FormStartPosition.CenterScreen;

            f.Show();
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (m_tamper != null)
                m_tamper.Dispose();
        }

        private void toolStripMenuItem6_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listView1.SelectedItems)
            {
                frmCometDiagram f = new frmCometDiagram(m_sqlConn, fnGetVictimTag(item).m_victim);
                f.Show();
            }
        }

        private void toolStripMenuItem9_Click(object sender, EventArgs e)
        {
            frmEditGroup f = new frmEditGroup(m_sqlConn);
            f.ShowDialog();

            fnLoadShell();
            fnLoadGroup();
        }

        private void treeView1_DoubleClick(object sender, EventArgs e)
        {
            TreeNode? node = treeView1.SelectedNode;
            if (node == null)
                return;

            if (node.Parent == null)
            {
                MessageBox.Show("Group count: " + node.Nodes.Count, "Nihahahaha", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            else
            {
                frmEditGroup f = new frmEditGroup(m_sqlConn);
                f.ShowDialog();

                fnLoadShell();
                fnLoadGroup();
            }
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            frmProxy f = new frmProxy();
            f.ShowDialog();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {

        }

        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                var groups = m_sqlConn.fnGetGroups();
                string szPattern = textBox2.Text;

                groups.Insert(0, "_Orphan");
                groups.Insert(0, "_All");

                TreeNode nodeGroup = treeView1.Nodes[0];

                try
                {
                    List<string> matchedNames = groups.Where(x => Regex.IsMatch(x, szPattern, RegexOptions.IgnoreCase)).ToList();

                    nodeGroup.Nodes.Clear();
                    nodeGroup.Nodes.AddRange(matchedNames.Select(x => new TreeNode(x)).ToArray());
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async void toolStripMenuItem10_Click(object sender, EventArgs e)
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

                bool bDoHttpGet = m_iniMgr.ReadBool("General", "DoHttpGet");

                foreach (clsWeb web in dic.Keys)
                {
                    lsTask.Add(Task.Run(async () =>
                    {
                        await semaphore.WaitAsync();

                        try
                        {
                            bool bAlive = true;
                            if (bDoHttpGet)
                                bAlive = await web.fnbTestWebConnection(false);

                            bAlive = bAlive && await web.fnbTestShellConnection(false);

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

        private void toolStripMenuItem11_Click(object sender, EventArgs e)
        {
            frmEncoder f = new frmEncoder();
            f.Show();
        }
    }

    public static class ListViewHeaderChanger
    {
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, ref HDITEM lParam);

        private const uint LVM_GETHEADER = 0x101F;
        private const uint HDM_GETITEM = 0x120B;
        private const uint HDM_SETITEM = 0x120C;

        private const int HDI_FORMAT = 0x0004;
        private const int HDF_SORTUP = 0x0400;
        private const int HDF_SORTDOWN = 0x0200;

        [StructLayout(LayoutKind.Sequential)]
        private struct HDITEM
        {
            public int mask;
            public int cxy;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pszText;
            public IntPtr hbm;
            public int cchTextMax;
            public int fmt;
            public IntPtr lParam;
            public int iImage;
            public int iOrder;
            public uint type;
            public IntPtr pvFilter;
            public uint state;
        }

        public enum SortOrder
        {
            None,
            Ascending,
            Descending
        }

        public static void SetSortArrow(this ListView listView, int columnIndex, SortOrder order)
        {
            HDITEM dummy = new HDITEM();
            IntPtr headerWnd = SendMessage(listView.Handle, LVM_GETHEADER, IntPtr.Zero, ref dummy);

            for (int i = 0; i < listView.Columns.Count; i++)
            {
                HDITEM item = new HDITEM();
                item.mask = HDI_FORMAT;

                SendMessage(headerWnd, HDM_GETITEM, new IntPtr(i), ref item);

                item.fmt &= ~(HDF_SORTUP | HDF_SORTDOWN);

                if (i == columnIndex)
                {
                    if (order == SortOrder.Ascending)
                        item.fmt |= HDF_SORTUP;
                    else if (order == SortOrder.Descending)
                        item.fmt |= HDF_SORTDOWN;
                }

                SendMessage(headerWnd, HDM_SETITEM, new IntPtr(i), ref item);
            }
        }
    }

    public class ListViewColumnSorter : IComparer
    {
        public int SortColumn { get; set; } = 0;
        public SortOrder Order { get; set; } = SortOrder.None;

        public int Compare(object x, object y)
        {
            ListViewItem itemX = (ListViewItem)x;
            ListViewItem itemY = (ListViewItem)y;

            string textX = itemX.SubItems.Count > SortColumn ? itemX.SubItems[SortColumn].Text : "";
            string textY = itemY.SubItems.Count > SortColumn ? itemY.SubItems[SortColumn].Text : "";

            int compareResult;

            if (DateTime.TryParse(textX, out DateTime dateX) && DateTime.TryParse(textY, out DateTime dateY))
            {
                compareResult = DateTime.Compare(dateX, dateY);
            }
            else
            {
                compareResult = string.Compare(textX, textY, StringComparison.OrdinalIgnoreCase);
            }

            if (Order == SortOrder.Ascending)
                return compareResult;
            else if (Order == SortOrder.Descending)
                return -compareResult;
            else
                return 0;
        }
    }
}
