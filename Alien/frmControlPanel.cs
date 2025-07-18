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

        public frmControlPanel(clsVictim victim)
        {
            InitializeComponent();

            m_victim = victim;
        }

        void fnSetup()
        {

        }

        private void frmControlPanel_Load(object sender, EventArgs e)
        {
            fnSetup();
        }
    }
}
