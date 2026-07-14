using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Alien
{
    public partial class frmFileImage : Form
    {
        private clsVictim m_victim { get; init; }
        private int m_nImageCount { get; init; }
        private ImageList m_ImageList { get; init; }
        private string m_szImgDir { get; init; }

        public frmFileImage(clsVictim victim, int nImageCount)
        {
            InitializeComponent();

            m_victim = victim;
            m_nImageCount = nImageCount;

            m_ImageList = new ImageList();
            m_ImageList.ColorDepth = ColorDepth.Depth32Bit;
            m_ImageList.ImageSize = new Size(200, 200);

            m_szImgDir = Path.Combine(victim.m_szPortfolio, "Images");
        }

        private struct stImageEntity
        {
            public string szFilePath;
            public Image img;
        }
        private stImageEntity fnGetItemTag(ListViewItem item) => (stImageEntity)item.Tag;

        public void fnAddImage(string szFilePath, Image img)
        {
            stImageEntity entity = new stImageEntity()
            {
                szFilePath = szFilePath,
                img = img,
            };

            string szFileName = Path.GetFileName(szFilePath);

            if (!m_ImageList.Images.ContainsKey(szFilePath))
            {
                m_ImageList.Images.Add(img);
                m_ImageList.Images.SetKeyName(m_ImageList.Images.Count - 1, szFilePath);

                ListViewItem item = new ListViewItem(szFileName);
                item.ImageKey = szFilePath;
                item.Tag = entity;

                listView1.Items.Add(item);

                toolStripProgressBar1.Increment(1);
            }
        }

        private void fnShowImage(ListViewItem item)
        {
            var entity = fnGetItemTag(item);

            TabPage page = new TabPage();
            page.Text = Path.GetFileName(entity.szFilePath);
            tabControl1.TabPages.Add(page);

            PictureBox pb = new PictureBox();
            pb.SizeMode = PictureBoxSizeMode.Zoom;

            page.Controls.Add(pb);
            pb.Dock = DockStyle.Fill;

            tabControl1.SelectedTab = page;

            pb.Image = entity.img;
            pb.Refresh();
        }

        private void fnSaveImage(ListViewItem item) => fnSaveImage(new List<stImageEntity>() { fnGetItemTag(item) });
        private void fnSaveImage(List<ListViewItem> lItem) => fnSaveImage(lItem.Select(x => fnGetItemTag(x)).ToList());
        private void fnSaveImage(List<stImageEntity> lfe)
        {
            if (lfe.Count == 0)
            {
                MessageBox.Show("List is empty.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (lfe.Count == 1)
            {
                //Single File
                var entity = lfe[0];

                SaveFileDialog sfd = new SaveFileDialog();
                sfd.FileName = Path.GetFileName(entity.szFilePath);

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    entity.img.Save(sfd.FileName);
                    MessageBox.Show("Action successfully.", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                //Multiple File
                FolderBrowserDialog fbd = new FolderBrowserDialog();
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    foreach (var entity in lfe)
                    {
                        string szSaveFilePath = Path.Combine(fbd.SelectedPath, Path.GetFileName(entity.szFilePath));
                        entity.img.Save(szSaveFilePath);
                    }
                }
            }
        }

        private void fnSetup()
        {
            listView1.View = View.LargeIcon;
            listView1.LargeImageList = m_ImageList;

            toolStripProgressBar1.Maximum = m_nImageCount;
            toolStripProgressBar1.Value = 0;

            timer1.Start();
        }

        private void frmFileImage_Load(object sender, EventArgs e)
        {
            fnSetup();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = $"Image[{listView1.Items.Count}] | Progress[{listView1.Items.Count}/{m_nImageCount}]";
        }

        private void frmFileImage_FormClosed(object sender, FormClosedEventArgs e)
        {
            timer1.Stop();
        }

        //Image.Show
        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            if (tabControl1.SelectedIndex != 0)
                return;

            foreach (ListViewItem item in listView1.SelectedItems)
                fnShowImage(item);
        }
        //Image.ShowAll
        private void toolStripMenuItem5_Click(object sender, EventArgs e)
        {
            if (tabControl1.SelectedIndex != 0)
                return;

            foreach (ListViewItem item in listView1.Items)
                fnShowImage(item);
        }
        //Image.Close
        private void toolStripMenuItem6_Click(object sender, EventArgs e)
        {
            if (tabControl1.SelectedIndex > 0)
                tabControl1.TabPages.Remove(tabControl1.SelectedTab);
        }
        //Image.CloseAll
        private void toolStripMenuItem7_Click(object sender, EventArgs e)
        {
            if (tabControl1.TabPages.Count > 1)
                for (int i = 1; i < tabControl1.TabPages.Count; i++)
                    tabControl1.TabPages.Remove(tabControl1.TabPages[i]);
        }

        private void tabControl1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Modifiers == Keys.Control)
            {
                switch (e.KeyCode)
                {
                    case Keys.W:
                        if (tabControl1.SelectedIndex == 0)
                            return;

                        if (tabControl1.SelectedTab != null)
                            tabControl1.TabPages.Remove(tabControl1.SelectedTab);

                        break;
                    case Keys.S:
                        if (tabControl1.SelectedIndex == 0)
                        {
                            //Save ALl
                            fnSaveImage(listView1.SelectedItems.Cast<ListViewItem>().ToList());
                        }
                        else
                        {
                            //Save image in selected page.
                            PictureBox pb = (PictureBox)tabControl1.SelectedTab.Controls[0];
                            SaveFileDialog sfd = new SaveFileDialog();
                            if (sfd.ShowDialog() == DialogResult.OK)
                            {
                                pb.Image.Save(sfd.FileName);
                                MessageBox.Show("Action successfully.", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }

                        break;
                }
            }
        }

        private void listView1_DoubleClick(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listView1.SelectedItems)
                fnShowImage(item);
        }
        //Show
        private void toolStripMenuItem8_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listView1.SelectedItems)
                fnShowImage(item);
        }
        //Show All
        private void toolStripMenuItem9_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listView1.Items)
                fnShowImage(item);
        }
        //Save
        private void toolStripMenuItem10_Click(object sender, EventArgs e)
        {
            fnSaveImage(listView1.SelectedItems.Cast<ListViewItem>().ToList());
        }
        //Save All
        private void toolStripMenuItem11_Click(object sender, EventArgs e)
        {
            fnSaveImage(listView1.Items.Cast<ListViewItem>().ToList());
        }
        //Copy Name
        private void toolStripMenuItem13_Click(object sender, EventArgs e)
        {
            var lObj = listView1.SelectedItems.Cast<ListViewItem>().Select(x => fnGetItemTag(x)).ToList();
            var lsName = lObj.Select(x => Path.GetFileName(x.szFilePath));

            Clipboard.SetText(string.Join(Environment.NewLine, lsName));
        }
        //Copy Path
        private void toolStripMenuItem12_Click(object sender, EventArgs e)
        {
            var lObj = listView1.SelectedItems.Cast<ListViewItem>().Select(x => fnGetItemTag(x)).ToList();
            var lsName = lObj.Select(x => x.szFilePath);

            Clipboard.SetText(string.Join(Environment.NewLine, lsName));
        }

        // Save
        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (tabControl1.SelectedIndex == 0)
                return;


        }

        // Save All
        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            
        }

        // Open Folder
        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            if (!Directory.Exists(m_szImgDir))
                Directory.CreateDirectory(m_szImgDir);

            Process.Start(new ProcessStartInfo
            {
                FileName = m_szImgDir,
                UseShellExecute = true,
            });
        }
    }
}
