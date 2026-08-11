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
using static Alien.clsThemeManager;

namespace Alien
{
    public partial class frmFileImage : Form
    {
        private clsVictim m_victim { get; init; }
        private int m_nImageCount { get; init; }
        private ImageList m_ImageList { get; init; }
        private TabPage? draggedTab { get; set; } = null;
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

        /// <summary>
        /// 
        /// </summary>
        private struct stImageEntity
        {
            public string szFilePath;
            public Image img;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private stImageEntity fnGetItemTag(ListViewItem item) => (stImageEntity)item.Tag;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="szFilePath"></param>
        /// <param name="img"></param>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
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

            ThemeManager.ApplyRange(page.Controls);
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

            tabControl1.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl1.Padding = new Point(30, 5);

            new TabZeroHook(tabControl1);

            ThemeManager.ApplyRange(new Control[]
            {
                listView1,
                statusStrip1,
                toolStrip1,
            });

            tabControl1.DrawItem += (s, e) =>
            {
                using (Brush bg = new SolidBrush(ThemeManager.Current.ControlBackColor))
                {
                    if (tabControl1.TabCount == 0)
                    {
                        e.Graphics.FillRectangle(bg, tabControl1.ClientRectangle);
                        return;
                    }

                    if (e.Index == tabControl1.TabCount - 1)
                    {
                        Rectangle lastTabRect = tabControl1.GetTabRect(e.Index);
                        if (lastTabRect.Right < tabControl1.Width)
                        {
                            Rectangle leftover = new Rectangle(
                                lastTabRect.Right,
                                lastTabRect.Top,
                                tabControl1.Width - lastTabRect.Right,
                                lastTabRect.Height);

                            e.Graphics.FillRectangle(bg, leftover);
                        }
                    }
                }

                if (e.Index < 0 || e.Index >= tabControl1.TabPages.Count)
                    return;

                TabPage page = tabControl1.TabPages[e.Index];
                Rectangle rect = tabControl1.GetTabRect(e.Index);

                bool selected = e.Index == tabControl1.SelectedIndex;

                // tab background
                using (Brush bg = new SolidBrush(ThemeManager.Current.ControlBackColor))
                {
                    e.Graphics.FillRectangle(bg, rect);
                }

                // selected highlight
                if (selected)
                {
                    using (Brush accent = new SolidBrush(ThemeManager.Current.AccentColor))
                    {
                        e.Graphics.FillRectangle(accent, new Rectangle(rect.Left + 5, rect.Bottom - 3, rect.Width - 10, 3));
                    }
                }

                // text
                TextRenderer.DrawText(
                    e.Graphics,
                    page.Text,
                    e.Font,
                    rect,
                    selected ? ThemeManager.Current.AccentColor : ThemeManager.Current.ForeColor,
                    TextFormatFlags.HorizontalCenter |
                    TextFormatFlags.VerticalCenter
                );

                if (e.Index == 0)
                    return;

                // X button
                Rectangle closeRect = fnGetCloseRect(e.Index);

                using (Pen pen = new Pen(ThemeManager.Current.ForeColor, 2))
                {
                    e.Graphics.DrawLine(pen, closeRect.Left + 4, closeRect.Top + 4, closeRect.Right - 4, closeRect.Bottom - 4);
                    e.Graphics.DrawLine(pen, closeRect.Right - 4, closeRect.Top + 4, closeRect.Left + 4, closeRect.Bottom - 4);
                }
            };
            tabControl1.KeyDown += async (s, e) =>
            {
                if (e.Modifiers == Keys.Control)
                {
                    TabPage? page = tabControl1.SelectedTab;
                    if (page == null)
                        return;

                    switch (e.KeyCode)
                    {
                        case Keys.W:
                            //Close page.

                            {
                                if (page.Text.Contains("*"))
                                {
                                    DialogResult dr = MessageBox.Show("The data is modified. Close anyway?", "Wait!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                                    if (dr != DialogResult.Yes)
                                        return;
                                }

                                int nIdx = tabControl1.SelectedIndex;
                                if (nIdx <= 0)
                                    return;

                                if (tabControl1.TabPages.Count > 1)
                                {
                                    if (nIdx > 0 && nIdx == tabControl1.TabPages.Count - 1)
                                        tabControl1.SelectedTab = tabControl1.TabPages[nIdx - 1];
                                    else
                                        tabControl1.SelectedTab = tabControl1.TabPages[nIdx + 1];
                                }

                                tabControl1.TabPages.Remove(page);
                            }
                            break;
                    }
                }
            };
            tabControl1.MouseDown += (s, e) =>
            {
                int nIdx = fnGetTabIndexAt(e.Location);
                if (nIdx == -1 || nIdx == 0)
                    return;

                if (fnGetCloseRect(nIdx).Contains(e.Location))
                {
                    tabControl1.TabPages.RemoveAt(nIdx);
                    return;
                }

                if (e.Button != MouseButtons.Left)
                    return;

                draggedTab = tabControl1.TabPages[nIdx];

                tabControl1.DoDragDrop(draggedTab, DragDropEffects.Move);
            };

            tabControl1.DragOver += (s, e) =>
            {
                e.Effect = DragDropEffects.Move;
            };

            tabControl1.DragDrop += (s, e) =>
            {
                Point p = tabControl1.PointToClient(new Point(e.X, e.Y));
                int nIdx = fnGetTabIndexAt(p);

                if (nIdx < 0 || draggedTab == null)
                    return;

                int oldIdx = tabControl1.TabPages.IndexOf(draggedTab);

                if (oldIdx == -1 || oldIdx == nIdx)
                    return;

                tabControl1.TabPages.Remove(draggedTab);

                if (nIdx > oldIdx)
                    nIdx--;

                nIdx = Math.Max(0, Math.Min(nIdx, tabControl1.TabPages.Count));

                tabControl1.TabPages.Insert(nIdx, draggedTab);

                tabControl1.SelectedTab = draggedTab;

                draggedTab = null;
            };

            tabControl1.DragLeave += (s, e) =>
            {
                draggedTab = null;
            };

            timer1.Start();
        }

        private int fnGetTabIndexAt(Point p)
        {
            for (int i = 0; i < tabControl1.TabPages.Count; i++)
            {
                if (tabControl1.GetTabRect(i).Contains(p))
                    return i;
            }
            return -1;
        }

        private Rectangle fnGetCloseRect(int i)
        {
            Rectangle tabRect = tabControl1.GetTabRect(i);

            return new Rectangle(
                tabRect.Right - 20,
                tabRect.Top + 4,
                15,
                15);
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
            {
                MessageBox.Show("Invalid page.", "NO", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            //Save image in selected page.
            try
            {
                PictureBox pb = (PictureBox)tabControl1.SelectedTab.Controls[0];
                SaveFileDialog sfd = new SaveFileDialog();
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    pb.Image.Save(sfd.FileName);
                    MessageBox.Show("Action successfully.", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Save All
        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            try
            {
                fnSaveImage(listView1.SelectedItems.Cast<ListViewItem>().ToList());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
