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
                MessageBox.Show("Save file failed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            // do something after successed. Change UI status (ex. "*" of the tabpage)


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
    }
}
