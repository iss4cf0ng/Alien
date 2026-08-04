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

        public frmScanHost()
        {
            InitializeComponent();

            Text = "Scan Host";
        }

        void fnSetup()
        {
            textBox2.Text = "80-88,135,139,445,1433,3306,3389,8080,8088,8888";
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
