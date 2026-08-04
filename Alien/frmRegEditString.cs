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
    public partial class frmRegEditString : BaseForm
    {
        private clsfnWinReg m_winReg { get; init; }
        private string m_szPath { get; init; }
        private string m_szName { get; init; }
        private string m_szType { get; init; }
        private string m_szValue { get; init; }

        public frmRegEditString(clsfnWinReg winReg, string szPath, string szName, string szType, string szValue)
        {
            InitializeComponent();

            m_winReg = winReg;

            m_szPath = szPath;
            m_szName = szName;
            m_szType = szType;
            m_szValue = szValue;
        }

        void fnSetup()
        {
            textBox1.ReadOnly = true;
            textBox1.Text = m_szName;

            textBox2.Text = m_szValue;
        }

        private void frmRegEditString_Load(object sender, EventArgs e)
        {
            fnSetup();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            string szValue = textBox2.Text;

            bool bVal = await m_winReg.fnbSetValue(m_szPath, m_szName, m_szType, szValue);
            if (!bVal)
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
