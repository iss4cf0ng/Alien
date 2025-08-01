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
    public partial class frmEditGroup : Form
    {
        private clsSqlite m_sqlConn { get; set; }

        public frmEditGroup(clsSqlite sqlConn)
        {
            InitializeComponent();

            m_sqlConn = sqlConn;
        }

        private void fnDeleteGroup()
        {

        }

        private async void fnSetup()
        {
            listView1.Items.Clear();

            var lShell = m_sqlConn.fnGetAllShellConfig();
            foreach (var config in lShell)
            {
                ListViewItem item = listView1.FindItemWithText(config.szGroupName);
                if (item == null)
                {
                    item = new ListViewItem(config.szGroupName);
                    item.SubItems.Add("0");

                    listView1.Items.Add(item);
                }

                item.SubItems[1].Text = (int.Parse(item.SubItems[1].Text) + 1).ToString();
            }
        }

        private void frmEditGroup_Load(object sender, EventArgs e)
        {
            fnSetup();
        }

        //Refresh
        private void button1_Click(object sender, EventArgs e)
        {
            fnSetup();
        }
        //Delete
        private void button3_Click(object sender, EventArgs e)
        {

        }
        //Add
        private void button2_Click(object sender, EventArgs e)
        {

        }
        //Check All
        private void button4_Click(object sender, EventArgs e)
        {
            listView1.Items.Cast<ListViewItem>().Select(x => x.Checked = true);
        }
        //Uncheck All
        private void button5_Click(object sender, EventArgs e)
        {
            listView1.Items.Cast<ListViewItem>().Select(x => x.Checked = false);
        }
    }
}
