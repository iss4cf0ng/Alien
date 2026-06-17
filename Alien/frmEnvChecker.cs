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
    public partial class frmEnvChecker : Form
    {
        /// <summary>
        /// Check prerequiste programs and environment variables
        /// </summary>

        public frmEnvChecker()
        {
            InitializeComponent();
        }

        async Task<bool> fnbCheckEnvVariables()
        {


            return true;
        }

        async Task<bool> fnbCheckPayloads()
        {


            return true;
        }

        async void fnSetup()
        {

        }

        private void frmEnvChecker_Load(object sender, EventArgs e)
        {
            fnSetup();
        }
    }
}
