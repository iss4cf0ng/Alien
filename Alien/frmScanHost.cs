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
    public partial class frmScanHost : BaseForm
    {
        public string m_szHosts { get; set; } = string.Empty;
        public string m_szPorts { get; set; } = string.Empty;
        public int m_nThread { get; set; } = 3;

        private clsIniManager m_iniMgr { get; init; }

        public frmScanHost(clsIniManager iniMgr)
        {
            InitializeComponent();

            Text = "Scan Host";

            m_iniMgr = iniMgr;
        }

        void fnSetup()
        {
            textBox2.Text = m_iniMgr.ReadString("ScanPort", "Ports");
        }

        private void frmScanHost_Load(object sender, EventArgs e)
        {
            fnSetup();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            m_szHosts = textBox1.Text;
            m_szPorts = textBox2.Text;
            m_nThread = (int)numericUpDown1.Value;

            DialogResult = DialogResult.OK;
        }
    }
}
