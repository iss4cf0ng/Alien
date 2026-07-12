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
    public partial class frmAbout : Form
    {
        private const string m_szREADME = "" +
            "Alien webshell management tool v5.0.0 by iss4cf0ng (ISSAC)\n" +
            "GitHub: https://github.com/iss4cf0ng/Alien\n" +
            "\n" +
            "Please do not use this tool for illegal purposes!\n" +
            "\n" +
            "The previous releases (version 1 to 4) were removed by the author due to my early limited development experience.\n" +
            "Alien v5.0.0 is much more powerful then the previous versions.\n" +
            "Hope you like this tool, thanks for checking it out!";

        public frmAbout()
        {
            InitializeComponent();

            Text = "About";
        }

        void fnSetup()
        {
            richTextBox1.Text = m_szREADME;
        }

        private void frmAbout_Load(object sender, EventArgs e)
        {
            fnSetup();
        }
    }
}
