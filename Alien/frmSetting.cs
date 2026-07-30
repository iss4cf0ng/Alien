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
    public partial class frmSetting : BaseForm
    {
        private clsIniManager m_iniMgr { get; init; }

        public frmSetting()
        {
            InitializeComponent();

            Text = "Setting";
            m_iniMgr = new clsIniManager("config.ini");
        }

        void fnSetup()
        {
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox1.DisplayMember = nameof(clsLanguage.clsLanguageItem.Name);
            comboBox1.ValueMember = nameof(clsLanguage.clsLanguageItem.Culture);
            comboBox1.DataSource = clsLanguage.clsLanguageManager.Languages;
            comboBox1.SelectedValue = m_iniMgr.ReadString("General", "Language");

            comboBox2.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox2.DisplayMember = nameof(clsThemeManager.clsThemeItem.Name);
            comboBox2.ValueMember = nameof(clsThemeManager.clsThemeItem.Theme);
            comboBox2.DataSource = clsThemeManager.ThemeItemManager._Themes;
            comboBox2.Text = m_iniMgr.ReadString("General", "Theme");

            checkBox1.Checked = m_iniMgr.ReadBool("General", "DoHttpGet");
        }

        private void frmSetting_Load(object sender, EventArgs e)
        {
            fnSetup();
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            string? szLanguage = comboBox1.SelectedValue!.ToString();
            if (!string.IsNullOrEmpty(szLanguage))
                m_iniMgr.Write("General", "Language", szLanguage);

            string? szTheme = comboBox2.Text!.ToString();
            if (!string.IsNullOrEmpty(szTheme))
                m_iniMgr.Write("General", "Theme", szTheme);

            m_iniMgr.Write("General", "DoHttpGet", checkBox1.Checked ? "True" : "False");

            MessageBox.Show("All the configuration is saved, please restart the application.", "Nice!", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
