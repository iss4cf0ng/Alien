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
    public partial class frmWGET : Form
    {
        private clsfnFileMgr m_fileMgr { get; init; }
        private frmControlPanel m_frmCtrl { get; init; }

        public frmWGET(clsfnFileMgr fileMgr, frmControlPanel frmCtrl)
        {
            InitializeComponent();

            m_fileMgr = fileMgr;
            m_frmCtrl = frmCtrl;
        }

        void fnSetup()
        {
            toolStripStatusLabel1.Text = "Ready";
            toolStripComboBox1.SelectedIndex = 2;
        }

        private void frmWGET_Load(object sender, EventArgs e)
        {
            fnSetup();
        }

        private async void toolStripButton1_Click(object sender, EventArgs e)
        {

        }

        private async void toolStripButton2_Click(object sender, EventArgs e)
        {

        }
    }
}
