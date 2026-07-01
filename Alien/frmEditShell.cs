using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Alien
{
    public partial class frmEditShell : Form
    {
        public clsTamper m_tamper { get; init; }
        public clsSqlite m_sqlConn { get; init; }
        public stShellConfig m_stShellConfig { get; init; }
        private bool m_bNewShell { get; init; }
        private List<string> m_lsGroupName { get; init; }

        private Dictionary<string, enLanguage> m_dicLang = clsWeb.m_dicSuffix.ToDictionary(x => x.Value, x => x.Key);

        private string[] m_asEncoding =
        {
            "ASCII",
            "UTF-8",
            "Big5", //Traditional Chinese
            "GB2312", //Simplified Chinese
            "GBK",
            "ISO-8859-1",
            "Windows-1252",
            "Shift_JIS", //Japanese
            "EUC-JP", //Japanese
            "EUC-KR", //Korean
        };

        public frmEditShell(clsTamper tamper, clsSqlite sqlConn, stShellConfig config, bool bNewShell, List<string> lsGroupName)
        {
            InitializeComponent();

            m_tamper = tamper;
            m_sqlConn = sqlConn;
            m_stShellConfig = config;
            m_bNewShell = bNewShell;
            m_lsGroupName = lsGroupName;
        }

        void fnSetup()
        {
            //Validate
            if (m_sqlConn == null)
            {
                MessageBox.Show("m_sqlConn is NULL.", "NULL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            //Controls init
            foreach (string szName in Enum.GetNames(typeof(enLanguage)))
                comboBox1.Items.Add(szName);

            if (comboBox1.Items.Count > 0)
                comboBox1.SelectedIndex = 0;

            foreach (string szEncoding in m_asEncoding)
                comboBox5.Items.Add(szEncoding);

            comboBox5.SelectedIndex = 0;

            // Load groups

            foreach (string szName in m_lsGroupName)
                comboBox6.Items.Add(szName);

            comboBox6.SelectedIndex = 1; // _Orphan

            string szTamperDirPath = Path.Combine(Application.StartupPath, "Tamper\\Obfuscators");
            if (Directory.Exists(szTamperDirPath))
            {
                comboBox2.DropDownStyle = ComboBoxStyle.DropDownList;
                textEditorControl1.Text = string.Empty;

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

                comboBox6.Text = m_stShellConfig.szGroupName;
                comboBox5.Text = m_stShellConfig.szEncoding;
                comboBox1.Text = m_stShellConfig.language.ToString();
                comboBox4.Text = m_stShellConfig.szMethod;
                comboBox3.Text = m_stShellConfig.payloadType.ToString();
            }
        }

        private void frmEditShell_Load(object sender, EventArgs e)
        {
            fnSetup();
        }

        //Test
        private async void button1_Click(object sender, EventArgs e)
        {
            stShellConfig config = new stShellConfig()
            {
                szUrl = textBox1.Text,
                szPassword = textBox2.Text,
                szEncoding = comboBox5.Text,
                szMethod = comboBox4.Text,
                language = (enLanguage)Enum.Parse(typeof(enLanguage), comboBox1.Text),
                payloadType = (enPayloadType)Enum.Parse(typeof(enPayloadType), comboBox3.Text),
            };

            clsVictim victim = new clsVictim(m_sqlConn, config, false);
            clsWeb web = new clsWeb(victim, m_tamper);

            string szPattern = clsEzData.fnszGenerateRandomStr();
            string szResp = await web.fnszSendPayload("test", new string[] { szPattern });

            if (string.Equals(szPattern, szResp))
                MessageBox.Show("Congrats! Webshell is valid", "OK!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            else
                MessageBox.Show("Oops! Webshell or the configuration is invalid...", "Oops!", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        //Save
        private void button2_Click(object sender, EventArgs e)
        {
            stShellConfig config = new stShellConfig();
            config.ID = m_bNewShell ? Guid.NewGuid().ToString() : m_stShellConfig.ID;
            config.szGroupName = comboBox6.Text;
            config.szUrl = textBox1.Text;
            config.szPassword = textBox2.Text;
            config.szEncoding = comboBox5.Text;
            config.language = (enLanguage)Enum.Parse(typeof(enLanguage), comboBox1.Text);
            config.szMethod = comboBox4.Text;
            config.payloadType = (enPayloadType)Enum.Parse(typeof(enPayloadType), comboBox3.Text);

            if (m_sqlConn.SaveShell(config))
            {
                MessageBox.Show("Save webshell successfully.", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Close();
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

        private void button3_Click(object sender, EventArgs e)
        {
            frmEditGroup f = new frmEditGroup(m_sqlConn);

            f.ShowDialog();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBox1.Text))
                return;

            string szExtension = textBox1.Text.Split('?').First().Split('.').Last().ToLower();
            if (!m_dicLang.ContainsKey(szExtension))
                return;

            if (comboBox1.Text == Enum.GetName(typeof(enLanguage), m_dicLang[szExtension]))
                return;

            try
            {
                for (int i = 0; i < comboBox1.Items.Count; i++)
                {
                    string? szLang = Enum.GetName(typeof(enLanguage), m_dicLang[szExtension]);
                    if (string.IsNullOrEmpty(szLang))
                        continue;

                    if (string.Equals(comboBox1.Items[i]?.ToString(), szLang))
                    {
                        comboBox1.SelectedIndex = i;
                        break;
                    }
                }
            }
            catch
            {

            }
        }

        private async void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            string szScriptName = comboBox2.Text;
            if (string.IsNullOrEmpty(szScriptName))
                return;

            string? szJson = await m_tamper.fnGetExample(szScriptName);
            if (string.IsNullOrEmpty(szJson))
                return;

            textEditorControl1.Text = szJson;
        }
    }
}
