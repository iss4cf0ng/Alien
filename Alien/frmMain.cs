using System.Text.RegularExpressions;

namespace Alien
{
    public partial class frmMain : Form
    {
        public clsSqlite m_sqlConn;

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

        void fnLoadShell(List<stShellConfig> lsConfig = null)
        {
            //todo: Dispose all exist clsWeb

            listView1.Items.Clear();

            if (lsConfig == null)
                lsConfig = m_sqlConn.fnGetAllShellConfig();

            foreach (var config in lsConfig)
            {
                ListViewItem item = new ListViewItem(config.ID);
                item.SubItems.Add(config.szUrl);
                item.SubItems.Add(config.language.ToString());
                item.SubItems.Add(config.dtCreateDate.ToString("F"));
                item.SubItems.Add(config.dtLastModified.ToString("F"));
                item.SubItems.Add(config.dtLastAccessed.ToString("F"));

                clsVictim victim = new clsVictim(m_sqlConn, config, false);
                victim.fnbBuildPortfolio();
                clsWeb web = new clsWeb(victim);

                item.Tag = web;


                listView1.Items.Add(item);
            }

            toolStripStatusLabel1.Text = $"Shell[{listView1.Items.Count}]";
        }

        void fnSetup()
        {
            clsSqlite sqlConn = new clsSqlite("data.sqlite");
            m_sqlConn = sqlConn;

            TreeNode node = new TreeNode("Group");
            node.Nodes.Add(new TreeNode("All"));

            treeView1.Nodes.Add(node);
            treeView1.ExpandAll();

            fnLoadShell();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            fnSetup();
        }

        //Control Panel
        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listView1.SelectedItems)
            {
                frmControlPanel f = new frmControlPanel(fnGetVictimTag(item));
                f.Show();
            }
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            frmEditShell f = new frmEditShell(m_sqlConn, new stShellConfig(), true);
            f.StartPosition = FormStartPosition.CenterScreen;
            f.Text = "Add Shell";

            f.m_sqlConn = m_sqlConn;

            f.ShowDialog();

            fnLoadShell();
        }

        private void toolStripMenuItem8_Click(object sender, EventArgs e)
        {
            frmEditShell f = new frmEditShell(m_sqlConn, new stShellConfig(), true);
            f.StartPosition = FormStartPosition.CenterScreen;
            f.Text = "Add Shell";

            f.m_sqlConn = m_sqlConn;

            f.ShowDialog();

            fnLoadShell();
        }

        private void listView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
            {
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
            frmEditShell f = new frmEditShell(m_sqlConn, web.m_victim.m_ShellConfig, false);
            f.StartPosition = FormStartPosition.CenterScreen;
            f.Text = "Edit Shell";

            f.m_sqlConn = m_sqlConn;

            f.ShowDialog();
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

        }
    }
}
