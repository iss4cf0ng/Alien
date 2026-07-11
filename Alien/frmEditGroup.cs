using Microsoft.VisualBasic;
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
        private clsSqlite m_sqlConn { get; init; }

        public frmEditGroup(clsSqlite sqlConn)
        {
            InitializeComponent();

            m_sqlConn = sqlConn;
        }

        private async void fnSetup()
        {
            listView1.Items.Clear();

            var lShell = m_sqlConn.fnGetAllShellConfig();
            foreach (var config in lShell)
            {
                ListViewItem? item = listView1.FindItemWithText(config.szGroupName);
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

        // Refresh
        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            fnSetup();
        }

        // Add
        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            try
            {
                string szName = Interaction.InputBox("Name: ", "New Group");
                m_sqlConn.fnAddGroup(szName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Rename
        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            try
            {
                var items = listView1.SelectedItems.Cast<ListViewItem>().ToList();
                if (items.Count == 0)
                    return;

                ListViewItem item = items.First();

                string szName = Interaction.InputBox("Name: ", "Rename Group");
                m_sqlConn.fnRenameGroup(item.Text, szName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Delete
        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            var items = listView1.SelectedItems.Cast<ListViewItem>().ToList();
            if (items.Count == 0)
                return;

            ListViewItem item = items.First();
            if (DialogResult.Yes != MessageBox.Show($"Are you sure to delete \"{item.Text}\"? All the shells in this group will be deleted!", "Wait!", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
                return;

            try
            {
                m_sqlConn.fnDeleteGroup(item.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Check All
        private void toolStripMenuItem5_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listView1.Items)
                item.Selected = true;
        }

        // Uncheck All
        private void toolStripMenuItem6_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listView1.Items)
                item.Selected = false;
        }
    }
}
