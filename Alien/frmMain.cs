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

        private clsVictim fnGetVictimTag(ListViewItem item) => (clsVictim)item.Tag;
        private List<clsVictim> fnGetVictimList(ListView lv) => lv.SelectedItems.Cast<ListViewItem>().Select(x => fnGetVictimTag(x)).ToList();

        #endregion

        void fnLoadShell()
        {
            listView1.Items.Clear();
            List<stShellConfig> lsConfig = m_sqlConn.fnGetAllShellConfig();
            foreach (var config in lsConfig)
            {
                ListViewItem item = new ListViewItem("ID");
                item.SubItems.Add(config.szUrl);
                item.SubItems.Add(config.language.ToString());
                item.SubItems.Add(config.dtCreateDate.ToString("F"));
                item.SubItems.Add(config.dtLastModified.ToString("F"));
                item.SubItems.Add(config.dtLastAccessed.ToString("F"));

                item.Tag = new clsVictim(m_sqlConn, config, false);

                listView1.Items.Add(item);
            }

            toolStripStatusLabel1.Text = $"Shell[{listView1.Items.Count}]";
        }

        void fnSetup()
        {
            clsSqlite sqlConn = new clsSqlite("data.sqlite");
            m_sqlConn = sqlConn;

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

            stShellConfig config = new stShellConfig()
            {

            };

            frmEditShell f = new frmEditShell(m_sqlConn, config);
            f.StartPosition = FormStartPosition.CenterScreen;
            f.Text = "Edit Shell";

            f.m_sqlConn = m_sqlConn;

            f.ShowDialog();
        }

        private void toolStripMenuItem8_Click(object sender, EventArgs e)
        {
            frmEditShell f = new frmEditShell(m_sqlConn, new stShellConfig());
            f.StartPosition = FormStartPosition.CenterScreen;
            f.Text = "Add Shell";

            f.m_sqlConn = m_sqlConn;

            f.ShowDialog();
        }

        private void listView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
            {
                fnLoadShell();
            }
        }
    }
}
