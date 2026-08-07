using Be.Windows.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using static Alien.clsThemeManager;

namespace Alien
{
    public partial class frmFileHexEditor : Form
    {
        private frmControlPanel m_frmCtrl { get; init; }
        private clsWeb m_web { get { return m_frmCtrl.m_web; } }
        private TabPage? draggedTab { get; set; } = null;

        private int _dragTabIndex = -1;
        private bool _dragging = false;

        public frmFileHexEditor(frmControlPanel frmCtrl)
        {
            InitializeComponent();

            m_frmCtrl = frmCtrl;
        }

        public struct stFile
        {
            public TabPage page { get; init; }
            public HexBox hexBox { get; init; }
            public ToolStripStatusLabel label { get; init; }

            public string szFilePath { get; init; }
            public string szFileName { get { return Path.GetFileName(szFilePath); } }

            public stFile(TabPage page, HexBox hb, ToolStripStatusLabel label, string szFilePath)
            {
                this.page = page;
                hexBox = hb;
                this.label = label;

                this.szFilePath = szFilePath;
            }
        }

        private int GetTabIndexAt(Point p)
        {
            for (int i = 0; i < tabControl1.TabPages.Count; i++)
            {
                if (tabControl1.GetTabRect(i).Contains(p))
                    return i;
            }

            return -1;
        }

        public void fnShowFile(string szFilePath, byte[] abFileData)
        {
            TabPage page = new TabPage();
            HexBox hb = new HexBox();
            StatusStrip ss = new StatusStrip();
            ToolStripStatusLabel label = new ToolStripStatusLabel();

            ss.Items.Add(label);

            stFile file = new stFile(page, hb, label, szFilePath);
            page.Tag = file;
            page.Text = file.szFileName;

            tabControl1.TabPages.Add(page);

            page.Controls.Add(ss);
            page.Controls.Add(hb);

            ThemeManager.ApplyRange(page.Controls);

            label.Font = Font;

            hb.Dock = DockStyle.Fill;
            hb.StringViewVisible = true;
            hb.LineInfoVisible = true;
            hb.VScrollBarVisible = true;
            hb.Font = new Font("Courier New", Font.Size);
            hb.BringToFront();

            DynamicByteProvider provider = new DynamicByteProvider(abFileData);
            hb.ByteProvider = provider;

            label.Text = $"Length: {abFileData.Length}";

            provider.Changed += (s, e) =>
            {
                if (!page.Text.Contains("*"))
                    page.Text += "*";
            };
            hb.KeyDown += (s, e) =>
            {
                if (e.Modifiers == Keys.Control)
                {
                    if (e.KeyCode == Keys.W)
                    {
                        TabPage? page = tabControl1.SelectedTab;
                        if (page == null)
                            return;

                        if (page.Text.Contains("*"))
                        {
                            DialogResult dr = MessageBox.Show("The data is modified. Close anyway?", "Wait!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                            if (dr != DialogResult.Yes)
                                return;
                        }

                        int nIdx = tabControl1.SelectedIndex;
                        if (nIdx < 0)
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
                    else if (e.KeyCode == Keys.S)
                    {
                        _ = Task.Run(async () =>
                        {
                            byte[] abData = provider.Bytes.ToArray();
                            if (!await m_frmCtrl.m_fileMgr.fnbFileWriteAllBytes(file.szFilePath, abData))
                            {
                                MessageBox.Show("Save file failed: " + file.szFilePath, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }

                            Invoke(() => page.Text = page.Text.Replace("*", string.Empty));
                        });
                    }
                }
            };
        }

        void fnSetup()
        {
            tabControl1.TabPages.Clear();

            tabControl1.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl1.Padding = new Point(30, 5);

            new TabZeroHook(tabControl1);

            ThemeManager.Apply(toolStrip1);

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

                // X button
                Rectangle closeRect = fnGetCloseRect(e.Index);

                using (Pen pen = new Pen(ThemeManager.Current.ForeColor, 2))
                {
                    e.Graphics.DrawLine(pen, closeRect.Left + 4, closeRect.Top + 4, closeRect.Right - 4, closeRect.Bottom - 4);
                    e.Graphics.DrawLine(pen, closeRect.Right - 4, closeRect.Top + 4, closeRect.Left + 4, closeRect.Bottom - 4);
                }
            };
            tabControl1.MouseDown += (s, e) =>
            {
                int nIdx = fnGetTabIndexAt(e.Location);
                if (nIdx == -1)
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

        private void frmFileHexEditor_Load(object sender, EventArgs e)
        {
            fnSetup();
        }

        private async void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            TabPage? page = tabControl1.SelectedTab;
            if (page == null || page.Tag == null)
                return;

            var file = (stFile)page.Tag;
            if (file.hexBox.ByteProvider == null)
                return;

            byte[] abData = ((DynamicByteProvider)file.hexBox.ByteProvider).Bytes.ToArray();
            if (!await m_frmCtrl.m_fileMgr.fnbFileWriteAllBytes(file.szFilePath, abData))
                MessageBox.Show("Save file failed: " + file.szFilePath, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            page.Text = page.Text.Replace("*", string.Empty);
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            TabPage? page = tabControl1.SelectedTab;
            if (page == null || page.Tag == null)
                return;

            var file = (stFile)page.Tag;
            if (file.hexBox.ByteProvider == null)
                return;

            byte[] abData = ((DynamicByteProvider)file.hexBox.ByteProvider).Bytes.ToArray();

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.FileName = file.szFileName;
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    File.WriteAllBytes(sfd.FileName, abData);
                    MessageBox.Show("Save file successfully.", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void toolStripMenuItem5_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                List<stFile> files = tabControl1.TabPages.Cast<TabPage>().Where(x => x != null && x.Tag != null).Select(x => (stFile)x.Tag).ToList();
                Dictionary<string, byte[]> dicBytes = files.ToDictionary(x => x.szFilePath, x => ((DynamicByteProvider)x.hexBox.ByteProvider).Bytes.ToArray());

                _ = Task.Run(() =>
                {
                    try
                    {
                        foreach (string szPath in dicBytes.Keys)
                        {
                            string szFileName = Path.GetFileName(szPath);
                            string szSavePath = Path.Combine(fbd.SelectedPath, szFileName);

                            File.WriteAllBytes(szSavePath, dicBytes[szPath]);
                        }

                        MessageBox.Show("All files are saved.", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                });
            }
        }

        private async void toolStripMenuItem6_Click(object sender, EventArgs e)
        {
            foreach (TabPage page in tabControl1.TabPages)
            {
                if (page.Tag == null)
                    continue;

                var file = (stFile)page.Tag;
                if (file.hexBox.ByteProvider == null)
                    continue;

                byte[] abData = ((DynamicByteProvider)file.hexBox.ByteProvider).Bytes.ToArray();
                if (!await m_frmCtrl.m_fileMgr.fnbFileWriteAllBytes(file.szFilePath, abData))
                    MessageBox.Show("Save file failed: " + file.szFilePath, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                page.Text = page.Text.Replace("*", string.Empty);
            }
        }

        private void tabControl1_KeyDown(object sender, KeyEventArgs e)
        {

        }
    }
}
