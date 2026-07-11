using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Alien
{
    public partial class frmBuilder : Form
    {
        private clsTamper m_tamper { get; init; }
        private List<string> m_lsNbPayload = new List<string>();

        public frmBuilder(clsTamper tamper)
        {
            InitializeComponent();

            Text = "Builder";
            m_tamper = tamper;
        }

        async Task fnUpdateTamperPayload(bool bShowError = true)
        {
            string szScriptName = comboBox1.Text;
            string szLanguage = comboBox2.Text;

            if (string.IsNullOrEmpty(szScriptName) || string.IsNullOrEmpty(szLanguage))
                return;

            var json = JsonConvert.DeserializeObject<Dictionary<string, object>>(textEditorControl1.Text);
            if (json == null)
            {
                if (bShowError)
                    MessageBox.Show("JSON deserialization is failed, please check your JSON!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                return;
            }

            try
            {
                json.Add("script", szLanguage);

                string? szPayload = await m_tamper.fnBuild(szScriptName, json);
                if (string.IsNullOrEmpty(szPayload))
                    szPayload = string.Empty;

                textEditorControl2.Text = szPayload;
                textEditorControl2.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void fnUpdateNbPayload()
        {
            try
            {
                textEditorControl3.Text = File.ReadAllText(m_lsNbPayload[comboBox3.SelectedIndex]).Replace("[NBPULSARDEADBEEF]", clsCrypto.fnGetMD5Last16(textBox4.Text));

                if (checkBox1.Checked)
                    textEditorControl3.Text = textEditorControl3.Text.Replace("\r", string.Empty).Replace("\n", string.Empty).Replace("\t", string.Empty).Replace("  ", string.Empty);

                textEditorControl3.Refresh();

                label5.Text = $"Length: {textEditorControl3.Text.Length}";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void fnSetup()
        {
            textEditorControl1.Text = string.Empty;
            textEditorControl2.Text = string.Empty;
            textEditorControl3.Text = string.Empty;

            label5.Text = string.Empty;

            string szTamperDirPath = Path.Combine(Application.StartupPath, "Tamper\\Obfuscators");
            if (Directory.Exists(szTamperDirPath))
            {
                comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;

                foreach (string szFilePath in Directory.GetFiles(szTamperDirPath))
                    comboBox1.Items.Add(Path.GetFileNameWithoutExtension(szFilePath));

                if (comboBox1.Items.Count > 0)
                    comboBox1.SelectedIndex = 0;
                else if (comboBox1.Items.Count == 0)
                    MessageBox.Show("Not tamper script exists!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                MessageBox.Show("Directory not found: " + szTamperDirPath, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            foreach (string szScript in Enum.GetNames(typeof(enLanguage)))
                comboBox2.Items.Add(szScript);

            comboBox2.DropDownStyle = ComboBoxStyle.DropDownList;
            if (comboBox2.Items.Count > 0)
                comboBox2.SelectedIndex = 0;

            string szNbPayload = Path.Combine(Application.StartupPath, "Builder", "NebulaPulsar");
            comboBox3.DropDownStyle = ComboBoxStyle.DropDownList;
            foreach (string payload in Directory.GetFiles(szNbPayload))
            {
                comboBox3.Items.Add(Path.GetFileName(payload).Split('.').Last().ToUpper());
                m_lsNbPayload.Add(payload);
            }

            comboBox3.SelectedIndex = 0;

            textBox4.Text = "iss4cf0ng";
        }

        private void frmBuilder_Load(object sender, EventArgs e)
        {
            fnSetup();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(textEditorControl1.Text);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(textEditorControl2.Text);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    File.WriteAllText(sfd.FileName, textEditorControl2.Text);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string szScriptName = comboBox1.Text;
            if (string.IsNullOrEmpty(szScriptName))
                return;

            string? szJson = await m_tamper.fnGetExample(szScriptName);
            if (string.IsNullOrEmpty(szJson))
                return;

            textEditorControl1.Text = szJson;
            textEditorControl1.Refresh();

            comboBox2.Items.Clear();

            List<string>? lsScript = await m_tamper.fnGetAvailableScript(szScriptName);
            if (lsScript == null || lsScript.Count == 0)
                return;

            foreach (string szScript in lsScript)
                comboBox2.Items.Add(szScript);

            comboBox2.SelectedIndex = 0;

            await fnUpdateTamperPayload();
        }

        private async void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textEditorControl1.Text))
                return;

            await fnUpdateTamperPayload();
        }

        private async void button4_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBox1.Text))
                return;

            try
            {
                var json = JsonConvert.DeserializeObject<Dictionary<string, object>>(textEditorControl1.Text);
                if (json == null)
                {
                    MessageBox.Show("JSON deserialization is failed, please check your JSON!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                textBox2.Text = await m_tamper.fnObfuscate(comboBox1.Text, textBox1.Text, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void button5_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBox2.Text))
                return;

            try
            {
                var json = JsonConvert.DeserializeObject<Dictionary<string, object>>(textEditorControl1.Text);
                if (json == null)
                {
                    MessageBox.Show("JSON deserialization is failed, please check your JSON!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                textBox3.Text = await m_tamper.fnDeobfuscate(comboBox1.Text, textBox2.Text, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void textEditorControl1_TextChanged(object sender, EventArgs e)
        {
            await fnUpdateTamperPayload(false);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(textEditorControl3.Text);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    File.WriteAllText(sfd.FileName, textEditorControl3.Text);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            fnUpdateNbPayload();
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            fnUpdateNbPayload();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            fnUpdateNbPayload();
        }
    }
}
