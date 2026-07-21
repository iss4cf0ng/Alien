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

namespace Alien
{
    public partial class frmFileHexEditor : Form
    {
        private frmControlPanel m_frmCtrl { get; init; }
        private clsWeb m_web { get { return m_frmCtrl.m_web; } }

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
                                MessageBox.Show("Save file failed: " + file.szFilePath, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

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
            tabControl1.Padding = new Point(30, 3);
            tabControl1.DrawItem += (s, e) =>
            {
                if (e.Index < 0 || e.Index >= tabControl1.TabPages.Count)
                    return;

                TabPage page = tabControl1.TabPages[e.Index];
                Rectangle tabRect = tabControl1.GetTabRect(e.Index);

                using (Brush bgBrush = new SolidBrush(SystemColors.Window))
                {
                    e.Graphics.FillRectangle(bgBrush, tabRect);
                }

                TextRenderer.DrawText(
                    e.Graphics,
                    page.Text,
                    Font,
                    new Rectangle(
                        tabRect.X + 4,
                        tabRect.Y + 4,
                        tabRect.Width - 20,
                        tabRect.Height),
                    Color.Black);

                Rectangle closeRect = new Rectangle(
                    tabRect.Right - 15,
                    tabRect.Top + 4,
                    10,
                    10);

                e.Graphics.DrawString(
                    "×",
                    Font,
                    Brushes.Red,
                    closeRect.Location);
            };
            tabControl1.MouseDown += (s, e) =>
            {
                for (int i = 0; i < tabControl1.TabPages.Count; i++)
                {
                    Rectangle tabRect = tabControl1.GetTabRect(i);

                    Rectangle closeRect = new Rectangle(
                        tabRect.Right - 15,
                        tabRect.Top + 4,
                        10,
                        10);

                    if (closeRect.Contains(e.Location))
                    {
                        TabPage page = tabControl1.TabPages[i];

                        // Optional:
                        // ask user to save if modified

                        tabControl1.TabPages.Remove(page);
                        page.Dispose();

                        return;
                    }
                }

                _dragTabIndex = GetTabIndexAt(e.Location);
            };
            tabControl1.MouseMove += (s, e) =>
            {
                if (e.Button != MouseButtons.Left)
                    return;

                if (_dragTabIndex < 0 ||
                    _dragTabIndex >= tabControl1.TabPages.Count)
                    return;

                int hoverIndex = GetTabIndexAt(e.Location);

                if (hoverIndex < 0 ||
                    hoverIndex >= tabControl1.TabPages.Count ||
                    hoverIndex == _dragTabIndex)
                    return;

                TabPage draggedPage = tabControl1.TabPages[_dragTabIndex];

                tabControl1.TabPages.Remove(draggedPage);
                tabControl1.TabPages.Insert(hoverIndex, draggedPage);

                tabControl1.SelectedTab = draggedPage;

                _dragTabIndex = hoverIndex;
            };
            tabControl1.MouseUp += (s, e) =>
            {
                _dragging = false;
                _dragTabIndex = -1;
            };
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
