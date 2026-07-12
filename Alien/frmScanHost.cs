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
    public partial class frmScanHost : Form
    {
        public string m_szHosts { get; set; } = string.Empty;
        public string m_szPorts { get; set; } = string.Empty;
        public int m_nThread { get; set; } = 3;

        public frmScanHost()
        {
            InitializeComponent();
        }

        void fnSetup()
        {

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
