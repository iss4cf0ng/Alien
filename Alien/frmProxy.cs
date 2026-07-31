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
    public partial class frmProxy : BaseForm
    {
        private clsIniManager m_iniMgr { get; init; }

        public frmProxy()
        {
            InitializeComponent();

            Text = "Proxy";
            m_iniMgr = new clsIniManager("config.ini");
        }

        void fnSetup()
        {
            try
            {
                string szURL = m_iniMgr.ReadString("Proxy", "URL");
                string szUsername = m_iniMgr.ReadString("Proxy", "Username");
                string szPassword = m_iniMgr.ReadString("Proxy", "Password");
                bool bEnable = m_iniMgr.ReadBool("Proxy", "Enable");

                radioButton1.Checked = bEnable;
                radioButton2.Checked = !bEnable;

                textBox1.Text = szURL;
                textBox2.Text = szUsername;
                textBox3.Text = szPassword;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void frmProxy_Load(object sender, EventArgs e)
        {
            fnSetup();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                bool bEnable = radioButton1.Checked;
                string szURL = textBox1.Text;
                string szUser = textBox2.Text;
                string szPass = textBox3.Text;

                m_iniMgr.Write("Proxy", "Enable", bEnable ? "True" : "False");
                m_iniMgr.Write("Proxy", "URL", szURL);
                m_iniMgr.Write("Proxy", "Username", szUser);
                m_iniMgr.Write("Proxy", "Password", szPass);

                MessageBox.Show("Proxy configuration is saved", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
