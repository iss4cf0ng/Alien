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
    public partial class frmRegEditMultiString : BaseForm
    {
        private clsfnWinReg m_winReg { get; init; }

        private string m_szBasePath { get; init; }
        private string m_szName { get; init; }
        private string m_szType { get; init; }
        private string[] m_asData { get; init; }

        public frmRegEditMultiString(clsfnWinReg winReg, string szBasePath, string szName, string szType, string[] asData)
        {
            InitializeComponent();

            m_winReg = winReg;

            m_szBasePath = szBasePath;

            m_szName = szName;
            m_szType = szType;
            m_asData = asData;
        }

        void fnSetup()
        {
            textBox1.ReadOnly = true;
            textBox1.Text = m_szName;

            textBox2.Text = string.Join(Environment.NewLine, m_asData);
        }

        private void frmRegEditMultiString_Load(object sender, EventArgs e)
        {
            fnSetup();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            string szValue = string.Join(",", textBox2.Text.Trim().Split(Environment.NewLine).Where(x => !string.IsNullOrEmpty(x)));

            bool bVal = await m_winReg.fnbSetValue(m_szBasePath, m_szName, m_szType, szValue);
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
