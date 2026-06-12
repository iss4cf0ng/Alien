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
    public partial class frmDbEdit : Form
    {
        private clsfnDb m_db { get; init; }
        private frmControlPanel m_frmCtrl { get; init; }
        private clsfnDb.stDbConfig m_dbConfig { get; set; }

        private clsSqlite m_sqlDb { get { return m_frmCtrl.m_dbMgr.m_sqlConn; } }

        public frmDbEdit(clsfnDb db, frmControlPanel frmCtrl)
        {
            InitializeComponent();

            m_db = db;
            m_frmCtrl = frmCtrl;
        }

        public frmDbEdit(clsfnDb db, frmControlPanel frmCtrl, clsfnDb.stDbConfig dbConfig)
        {
            InitializeComponent();

            m_db = db;
            m_frmCtrl = frmCtrl;
            m_dbConfig = dbConfig;
        }

        void fnSetup()
        {
            foreach (string szName in Enum.GetNames(typeof(enDatabase)))
                comboBox1.Items.Add(szName);

            comboBox1.SelectedIndex = 0;
        }

        private void frmDbEdit_Load(object sender, EventArgs e)
        {
            fnSetup();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var en = (enDatabase)Enum.Parse(typeof(enDatabase), comboBox1.Text);
            switch (en)
            {
                case enDatabase.DSN:

                    break;
                case enDatabase.MySQL:
                    textBox3.Enabled = true;

                    textBox3.Text = string.IsNullOrEmpty(textBox3.Text) ? "root" : textBox3.Text;

                    break;
                case enDatabase.Access:
                    textBox3.Enabled = false;

                    break;
                case enDatabase.SQLServer:
                    textBox3.Enabled = true;

                    break;
                case enDatabase.PostgreSQL:

                    break;
                case enDatabase.SQLite:

                    break;
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            var config = new clsfnDb.stDbConfig()
            {
                enDbType = (enDatabase)Enum.Parse(typeof(enDatabase), comboBox1.Text),

                szSource = textBox2.Text,
                szUsername = textBox3.Text,
                szPassword = textBox4.Text,
            };

            config.szConnString = clsfnDb.fnBuildConnStr(config);
            MessageBox.Show(config.szConnString);

            bool bRet = await m_db.fnDbTest(config);
            if (bRet)
                MessageBox.Show("Connect test successfully.", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
            else
                MessageBox.Show("Connect testing is failed.", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            textBox4.UseSystemPasswordChar = !checkBox1.Checked;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var config = new clsfnDb.stDbConfig()
            {
                enDbType = (enDatabase)Enum.Parse(typeof(enDatabase), comboBox1.Text),
                szID = Guid.NewGuid().ToString(),
                szSource = textBox2.Text,
                szUsername = textBox3.Text,
                szPassword = textBox4.Text,

                dtCreationDate = DateTime.Now,
                dtLastUsed = DateTime.Now,

            };

            config.szConnString = clsfnDb.fnBuildConnStr(config);

            if (m_db.fnbSaveDatabase(config))
            {
                m_frmCtrl.fnDbInit();
                MessageBox.Show("Database config is saved.", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Save config failed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
