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
        private bool m_bNewShell { get; set; }

        private string[] m_asEncoding =
        {
            "ASCII",
            "UTF-8",
            "Big5",
            "GB2312",
            "GBK",
            "ISO-8859-1",
            "Windows-1252",
            "Shift_JIS",
            "EUC-JP",
            "EUC-KR",
        };

        public frmEditShell(clsSqlite sqlConn, stShellConfig config, bool bNewShell)
        {
            InitializeComponent();

            m_sqlConn = sqlConn;
            m_stShellConfig = config;
            m_bNewShell = bNewShell;
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

            if (comboBox1.Items.Count > 0)
                comboBox1.SelectedIndex = 0;

            foreach (string szEncoding in m_asEncoding)
                comboBox5.Items.Add(szEncoding);

            comboBox5.SelectedIndex = 0;

            string szTamperDirPath = Path.Combine(Application.StartupPath, "Tamper");
            if (Directory.Exists(szTamperDirPath))
            {
                foreach (string szFilePath in Directory.GetFiles(szTamperDirPath))
                    comboBox2.Items.Add(Path.GetFileNameWithoutExtension(szFilePath));

                if (comboBox2.Items.Count > 0)
                    comboBox2.SelectedIndex = 0;
                else if (comboBox2.Items.Count == 0)
                    MessageBox.Show("Not tamper script exists!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                MessageBox.Show("Directory not found: " + szTamperDirPath, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

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
            config.ID = m_bNewShell ? Guid.NewGuid().ToString() : m_stShellConfig.ID;
            config.szUrl = textBox1.Text;
            config.szPassword = textBox2.Text;
            config.szEncoding = comboBox5.Text;
            config.language = (Language)Enum.Parse(typeof(Language), comboBox1.Text);
            config.szMethod = comboBox4.Text;
            config.payloadType = (PayloadType)Enum.Parse(typeof(PayloadType), comboBox3.Text);

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

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboBox4.Items.Clear();
            string szDirPath = Path.Combine(new string[] { Application.StartupPath, "Payload", comboBox1.Text });
            if (!Directory.Exists(szDirPath))
                MessageBox.Show("Directory not found: " + szDirPath, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            foreach (string szName in Directory.GetDirectories(szDirPath))
                comboBox4.Items.Add(Path.GetFileName(szName));

            if (comboBox4.Items.Count > 0)
                comboBox4.SelectedIndex = 0;
        }

        private void comboBox4_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboBox3.Items.Clear();
            string szDirPath = Path.Combine(new string[] { Application.StartupPath, "Payload", comboBox1.Text, comboBox4.Text });
            if (!Directory.Exists(szDirPath))
                MessageBox.Show("Directory not found: " + szDirPath, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            foreach (string szName in Directory.GetDirectories(szDirPath))
                comboBox3.Items.Add(Path.GetFileName(szName));

            if (comboBox3.Items.Count > 0)
                comboBox3.SelectedIndex = 0;
        }
    }
}
