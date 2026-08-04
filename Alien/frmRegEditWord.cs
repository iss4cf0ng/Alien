using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Globalization;

namespace Alien
{
    public partial class frmRegEditWord : BaseForm
    {
        private clsfnWinReg m_winReg { get; init; }

        private string m_szBasePath { get; init; }
        private string m_szName { get; init; }
        private string m_szType { get; init; }
        private ulong m_nValue { get; init; }

        public frmRegEditWord(clsfnWinReg winReg, string szBasePath, string szName, string szType, ulong nValue)
        {
            InitializeComponent();

            m_winReg = winReg;

            m_szBasePath = szBasePath;
            m_szName = szName;
            m_szType = szType;
            m_nValue = nValue;
        }

        bool fnbValidate(string szValue)
        {
            if (radioButton1.Checked)
                return uint.TryParse(szValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _);
            else
                return uint.TryParse(szValue, out _);
        }

        void fnSetup()
        {
            textBox1.ReadOnly = true;
            textBox1.Text = m_szName;

            radioButton2.Checked = true;

            textBox2.Text = m_nValue.ToString();
        }

        private void frmRegEditWord_Load(object sender, EventArgs e)
        {
            fnSetup();
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (!radioButton1.Checked)
                return;

            if (uint.TryParse(textBox2.Text, out uint nValue))
            {
                textBox2.Text = nValue.ToString("X");
            }
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (!radioButton2.Checked)
                return;

            if (uint.TryParse(textBox2.Text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint nValue))
            {
                textBox2.Text = nValue.ToString();
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            string szValue = textBox2.Text.Trim();
            if (string.IsNullOrEmpty(szValue))
                return;

            bool bValid = fnbValidate(szValue);

            textBox2.BackColor = bValid ? Color.White : Color.LightPink;
        }

        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar))
                return;

            if (radioButton1.Checked)
            {
                bool bIsHex = char.IsDigit(e.KeyChar) || (e.KeyChar >= 'a' && e.KeyChar <= 'f') || (e.KeyChar >= 'A' && e.KeyChar <= 'F');
                e.Handled = !bIsHex;
            }
            else
            {
                e.Handled = !char.IsDigit(e.KeyChar);
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            string szValue = textBox2.Text;

            bool bVal = await m_winReg.fnbSetValue(m_szBasePath, m_szName, m_szType, szValue);
            if (bVal)
            {
                MessageBox.Show("Cannot set value.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            MessageBox.Show("Set value successfully.", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);

            Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
