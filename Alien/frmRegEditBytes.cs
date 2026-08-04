using Be.Windows.Forms;
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
    public partial class frmRegEditBytes : BaseForm
    {
        private HexBox m_hexBox = new HexBox();

        private clsfnWinReg m_winReg { get; init; }

        private string m_szBasePath { get; init; }
        private string m_szName { get; init; }
        private string m_szType { get; init; }
        private byte[] m_abValue { get; init; }

        public frmRegEditBytes(clsfnWinReg winReg, string szBasePath, string szName, string szType, byte[] abValue)
        {
            InitializeComponent();

            m_winReg = winReg;

            m_szBasePath = szBasePath;
            m_szName = szName;
            m_szType = szType;
            m_abValue = abValue;
        }

        void fnSetup()
        {
            textBox1.ReadOnly = true;
            textBox1.Text = m_szName;

            panel1.Controls.Add(m_hexBox);
            m_hexBox.Dock = DockStyle.Fill;
            m_hexBox.StringViewVisible = true;
            m_hexBox.LineInfoVisible = true;
            m_hexBox.VScrollBarVisible = true;
            m_hexBox.Font = new Font("Courier New", Font.Size);

            DynamicByteProvider provider = new DynamicByteProvider(m_abValue);
            m_hexBox.ByteProvider = provider;
        }

        private void frmRegEditBytes_Load(object sender, EventArgs e)
        {
            fnSetup();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            byte[] abValue = ((DynamicByteProvider)m_hexBox.ByteProvider).Bytes.ToArray();
            if (abValue == null)
                return;

            bool bVal = await m_winReg.fnbSetValue(m_szBasePath, m_szName, m_szType, Convert.ToBase64String(abValue));
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
