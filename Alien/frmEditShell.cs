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
    public partial class frmEditShell : Form
    {
        public clsSqlite m_sqlConn;
        public stShellConfig m_stShellConfig;

        public frmEditShell(clsSqlite sqlConn, stShellConfig config)
        {
            InitializeComponent();

            m_sqlConn = sqlConn;
            m_stShellConfig = config;
        }

        /// <summary>
        /// Save shell config.
        /// </summary>
        void SaveShell()
        {
            stShellConfig config = new stShellConfig()
            {
                szUrl = textBox1.Text,
                szPassword = textBox2.Text,
            };

            if (!m_sqlConn.SaveShell(config))
            {
                MessageBox.Show("SaveShell() error", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void fnSetup()
        {
            //Validate
            if (m_sqlConn == null)
            {
                MessageBox.Show("m_sqlConn is NULL.", "NULL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            //Controls init
            foreach (string szName in Enum.GetNames(typeof(Language)))
                comboBox1.Items.Add(szName);
            foreach (string szName in Enum.GetNames(typeof(PayloadType)))
                comboBox3.Items.Add(szName);

            comboBox1.SelectedIndex = 0;
            comboBox3.SelectedIndex = 0;

            if (string.IsNullOrEmpty(m_stShellConfig.szUrl))
            {
                //Add shell

            }
            else
            {
                //Edit shell
                textBox1.Text = m_stShellConfig.szUrl;
                textBox2.Text = m_stShellConfig.szPassword;
            }
        }

        private void frmEditShell_Load(object sender, EventArgs e)
        {
            fnSetup();
        }

        //Test
        private void button1_Click(object sender, EventArgs e)
        {

        }
        //Save
        private void button2_Click(object sender, EventArgs e)
        {
            stShellConfig config = new stShellConfig();
            config.szUrl = textBox1.Text;
            config.szPassword = textBox2.Text;
            config.language = (Language)Enum.Parse(typeof(Language), comboBox1.Text);

            if (m_sqlConn.SaveShell(config))
            {
                MessageBox.Show("Save webshell successfully.", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Failed to save shell!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            groupBox3.Enabled = !checkBox1.Checked;
        }
    }
}
