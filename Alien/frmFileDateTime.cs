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
    public partial class frmFileDateTime : Form
    {
        public frmFileDateTime()
        {
            InitializeComponent();
        }

        void fnSetup()
        {

        }

        private void frmFileDateTime_Load(object sender, EventArgs e)
        {
            fnSetup();
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            DateTime dt = dateTimePicker1.Value;
            long unixTimestamp = ((DateTimeOffset)dt).ToUnixTimeSeconds();
            string szDt = unixTimestamp.ToString();

            textBox1.Text = szDt;
        }
    }
}
