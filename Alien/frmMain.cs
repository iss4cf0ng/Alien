namespace Alien
{
    public partial class frmMain : Form
    {
        public Sqlite m_sqlConn;

        public frmMain()
        {
            InitializeComponent();
        }

        void fnLoadShell()
        {
            List<stShellConfig> lsConfig = m_sqlConn.fnGetAllShellConfig();
            foreach (var config in lsConfig)
            {
                ListViewItem item = new ListViewItem("ID");
                item.SubItems.Add(config.szUrl);
                item.SubItems.Add(config.language.ToString());
                item.SubItems.Add(config.dtCreateDate.ToString("F"));
                item.SubItems.Add(config.dtLastModified.ToString("F"));
                item.SubItems.Add(config.dtLastAccessed.ToString("F"));

                listView1.Items.Add(item);
            }

            toolStripStatusLabel1.Text = $"Shell[{listView1.Items.Count}]";
        }

        void setup()
        {
            Sqlite sqlConn = new Sqlite("data.sqlite");
            m_sqlConn = sqlConn;

            fnLoadShell();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            setup();
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listView1.SelectedItems)
            {
                frmFileMgr f = new frmFileMgr();
                f.StartPosition = FormStartPosition.CenterScreen;
                f.Text = "File Manager";

                f.Show();
            }
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            frmEditShell f = new frmEditShell();
            f.StartPosition = FormStartPosition.CenterScreen;
            f.Text = "Edit Shell";

            f.m_sqlConn = m_sqlConn;

            f.ShowDialog();
        }

        private void toolStripMenuItem8_Click(object sender, EventArgs e)
        {
            frmEditShell f = new frmEditShell();
            f.StartPosition = FormStartPosition.CenterScreen;
            f.Text = "Add Shell";

            f.m_sqlConn = m_sqlConn;

            f.ShowDialog();
        }
    }
}
