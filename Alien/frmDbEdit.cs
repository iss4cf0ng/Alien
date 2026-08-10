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
    public partial class frmDbEdit : BaseForm
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

        void fnUpdateConnURL()
        {
            var tmpConfig = new clsfnDb.stDbConfig()
            {
                enDbType = (enDatabase)Enum.Parse(typeof(enDatabase), comboBox1.Text),
                szSource = textBox2.Text,
                szUsername = textBox3.Text,
                szPassword = textBox4.Text,
            };

            textBox1.Text = clsfnDb.fnBuildConnURL(tmpConfig);
        }

        void fnSetup()
        {
            Text = "Configuration";

            foreach (string szName in Enum.GetNames(typeof(enDatabase)))
                comboBox1.Items.Add(szName);

            comboBox1.SelectedIndex = 0;

            if (string.IsNullOrEmpty(m_dbConfig.szID))
                return;

            // Edit

            for (int i = 0; i < comboBox1.Items.Count; i++)
            {
                if ((int)m_dbConfig.enDbType == i)
                {
                    comboBox1.SelectedIndex = i;
                    break;
                }
            }

            textBox1.Text = m_dbConfig.szConnString;
            textBox2.Text = m_dbConfig.szSource;
            textBox3.Text = m_dbConfig.szUsername;
            textBox4.Text = m_dbConfig.szPassword;
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
                    textBox3.Text = string.IsNullOrEmpty(textBox3.Text)? "sa" : textBox3.Text;

                    break;
                case enDatabase.PostgreSQL:
                    textBox3.Enabled = true;

                    break;
                case enDatabase.SQLite:

                    break;
            }

            fnUpdateConnURL();
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

            config.szConnString = clsfnDb.fnBuildConnURL(config);

            bool bRet = await m_db.fnDbTest(config);
            if (bRet)
                MessageBox.Show("Connect test successfully.", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
            else
                MessageBox.Show("Connect testing is failed.", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var config = new clsfnDb.stDbConfig()
            {
                enDbType = (enDatabase)Enum.Parse(typeof(enDatabase), comboBox1.Text),
                szID = m_dbConfig.szID ?? Guid.NewGuid().ToString(),
                szSource = textBox2.Text,
                szUsername = textBox3.Text,
                szPassword = textBox4.Text,

                dtCreationDate = DateTime.Now,
                dtLastUsed = DateTime.Now,

            };

            config.szConnString = clsfnDb.fnBuildConnURL(config);

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

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            fnUpdateConnURL();
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            fnUpdateConnURL();
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            fnUpdateConnURL();
        }
    }
}
