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
    public partial class frmMsgBox : Form
    {
        private string m_szMsgTitle { get; set; }
        private string m_szMsgContent { get; set; }

        public frmMsgBox(string szTitle, string szContent)
        {
            InitializeComponent();

            m_szMsgTitle = szTitle;
            m_szMsgContent = szContent;
        }

        void fnSetup()
        {
            Text = m_szMsgTitle;
            richTextBox1.Text = m_szMsgContent;
        }

        private void frmMsgBox_Load(object sender, EventArgs e)
        {
            fnSetup();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(m_szMsgContent);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
