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
        private clsfnDb m_db { get; set; }
        private frmControlPanel m_frmCtrl { get; set; }

        private clsSqlite m_sqlDb { get { return m_frmCtrl.m_dbMgr.m_sqlConn; } }

        public frmDbEdit(clsfnDb db, frmControlPanel frmCtrl)
        {
            InitializeComponent();

            m_db = db;
            m_frmCtrl = frmCtrl;
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

                    if (string.IsNullOrEmpty(textBox2.Text))
                    {
                        textBox2.Text = "root";

                    }

                    break;
                case enDatabase.MySQLi:
                    textBox3.Enabled = true;

                    break;
                case enDatabase.Access:
                    textBox3.Enabled = false;

                    break;
                case enDatabase.SqlServer:
                    textBox3.Enabled = true;

                    break;
                case enDatabase.Sqlite:

                    break;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            textBox4.UseSystemPasswordChar = !checkBox1.Checked;
        }

        private void button2_Click(object sender, EventArgs e)
        {

        }
    }
}
