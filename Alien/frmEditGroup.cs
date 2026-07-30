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
    public partial class frmEditGroup : BaseForm
    {
        private clsSqlite m_sqlConn { get; init; }

        public frmEditGroup(clsSqlite sqlConn)
        {
            InitializeComponent();

            Text = "Group Manager";
            m_sqlConn = sqlConn;
        }

        private void fnSetup()
        {
            listView1.Items.Clear();

            foreach (var group in m_sqlConn.fnGetGroups())
            {
                int nCount = m_sqlConn.fnGetShellWithGroupName(group).Count;
                ListViewItem item = new ListViewItem(group);
                item.SubItems.Add(nCount.ToString());

                listView1.Items.Add(item);
            }

            toolStripStatusLabel1.Text = $"Group[{listView1.Items.Count}]";
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

            fnSetup();
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

            fnSetup();
        }

        // Delete
        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            var items = listView1.SelectedItems.Cast<ListViewItem>().ToList();
            if (items.Count == 0)
                return;

            ListViewItem item = items.First();
            if (DialogResult.Yes != MessageBox.Show($"Are you sure to delete \"{item.Text}\"? All the shells in this group will be moved to \"_Orphan\"!", "Wait!", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
                return;

            try
            {
                m_sqlConn.fnDeleteGroup(item.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            fnSetup();
        }

        // Select All
        private void toolStripMenuItem5_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listView1.Items)
                item.Selected = true;
        }

        // Unselect All
        private void toolStripMenuItem6_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listView1.Items)
                item.Selected = false;
        }
    }
}
