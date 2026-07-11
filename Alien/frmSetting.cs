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
    public partial class frmSetting : Form
    {
        public frmSetting()
        {
            InitializeComponent();
        }

        void fnSetup()
        {
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox1.DisplayMember = nameof(clsLanguage.clsLanguageItem.Name);
            comboBox1.ValueMember = nameof(clsLanguage.clsLanguageItem.Culture);
            comboBox1.DataSource = clsLanguage.clsLanguageManager.Languages;
        }

        private void frmSetting_Load(object sender, EventArgs e)
        {
            fnSetup();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string? szLanguage = comboBox1.SelectedValue!.ToString();
            if (!string.IsNullOrEmpty(szLanguage))
            {
                new clsIniManager("config.ini").Write("General", "Language", szLanguage);
            }
        }
    }
}
