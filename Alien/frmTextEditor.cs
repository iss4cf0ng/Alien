using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using ICSharpCode.TextEditor;
using static Alien.clsThemeManager;

namespace Alien
{
    public partial class frmTextEditor : Form
    {
        private frmControlPanel? m_frmControl { get; init; }
        private TabPage? draggedTab { get; set; } = null;

        public frmTextEditor(frmControlPanel frmControl)
        {
            InitializeComponent();

            m_frmControl = frmControl;

            Text = "Text Editor";
        }

        public class clsTabColor
        {
            public Color BackColor { get; set; }
            public Color ForeColor { get; set; }
        }

        #region Tool

        (TabPage? page, TextEditorControlEx? editorEx, TextBox? tbPath, TextBox? tbSearch) fnGetTabControl(TabPage? page = null)
        {
            if (page == null)
                page = tabControl1.SelectedTab;

            if (page == null)
                return (null, null, null, null);

            Control.ControlCollection ctrls = page.Controls;
            TextEditorControlEx editorEx = (TextEditorControlEx)ctrls[0];
            TextBox tbPath = (TextBox)ctrls[1];
            TextBox tbSearch = (TextBox)ctrls[2];

            ThemeManager.Apply(page);

            return (page, editorEx, tbPath, tbSearch);
        }

        private bool fnbIsModified(TabPage? page = null)
        {
            if (page == null)
                page = tabControl1.SelectedTab;

            if (page == null)
            {
                MessageBox.Show("Not selected tabpage.", "fnbIsModified()", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return page.Text.Contains("*");
        }

        #endregion

        public void fnShowContent(string szFilePath, string szFileContent)
        {
            TabPage page = new TabPage();
            page.Text = Path.GetFileName(szFilePath);

            TextEditorControlEx editor = new TextEditorControlEx();
            TextBox tbPath = new TextBox();
            TextBox tbSearch = new TextBox();

            tabControl1.TabPages.Add(page);
            page.Controls.AddRange(new Control[]
            {
                editor,
                tbPath,
                tbSearch,
            });

            ThemeManager.ApplyRange(page.Controls);

            tbPath.Dock = DockStyle.Top;
            tbSearch.Dock = DockStyle.Bottom;
            editor.Dock = DockStyle.Fill;
            editor.BringToFront();

            tbPath.Text = szFilePath;
            editor.Text = szFileContent;

            tbSearch.Tag = 0;

            editor.TextChanged += TextEditorEx_TextChange;
            tbSearch.KeyDown += TextboxSearch_KeyDown;
        }

        void fnSetup()
        {
            tabControl1.TabPages.Clear();

            tabControl1.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl1.Padding = new Point(30, 5);

            ThemeManager.ApplyRange(new Control[] {menuStrip1,statusStrip1});

            new TabZeroHook(tabControl1);

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
                            break;
                        case Keys.S:
                            //Save

                            if (fnbIsModified())
                            {
                                var ctrls = fnGetTabControl();


                                string szFilePath = ctrls.tbPath.Text;
                                string szContent = ctrls.editorEx.Text;

                                if (m_frmControl == null)
                                    return;

                                if (await m_frmControl.fnbFileWrite(szFilePath, szContent))
                                {
                                    page.Text = page.Text.Replace("*", string.Empty);

                                    if (!string.Equals(Path.GetFileName(szFilePath), page.Text))
                                        page.Text = Path.GetFileName(szFilePath);
                                }
                                else
                                {
                                    MessageBox.Show("Failed to save: " + szFilePath, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            }

                            break;
                    }
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

        private void frmTextEditor_Load(object sender, EventArgs e)
        {
            fnSetup();
        }

        private void TextEditorEx_TextChange(object sender, EventArgs e)
        {
            var ctrls = fnGetTabControl();

            if (!ctrls.page.Text.Contains("*"))
                ctrls.page.Text = $"*{ctrls.page.Text}";
        }

        private void TextboxSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;

                TextBox tbSearch = (TextBox)sender;
                string szWord = tbSearch.Text;

                if (string.IsNullOrEmpty(szWord))
                    return;

                var ctrls = fnGetTabControl();
                if (ctrls.editorEx == null)
                    return;

                string szSourceText = ctrls.editorEx.Text;

                int nStartIdx = 0;
                if (tbSearch.Tag != null && int.TryParse(tbSearch.Tag.ToString(), out int nParseIdx))
                    nStartIdx = nParseIdx;

                if (nStartIdx > szSourceText.Length)
                    nStartIdx = 0;

                int nIdx = szSourceText.IndexOf(szWord, nStartIdx, StringComparison.CurrentCultureIgnoreCase);
                if (nIdx == -1 && nStartIdx > 0)
                    nIdx = szSourceText.IndexOf(szWord, 0, StringComparison.CurrentCultureIgnoreCase);

                if (nIdx != -1)
                {
                    ctrls.editorEx.Focus();

                    var document = ctrls.editorEx.Document;
                    var startPosition = document.OffsetToPosition(nIdx);
                    var endPosition = document.OffsetToPosition(nIdx + szWord.Length);

                    ctrls.editorEx.ActiveTextAreaControl.SelectionManager.SetSelection(startPosition, endPosition);
                    ctrls.editorEx.ActiveTextAreaControl.Caret.Position = endPosition;

                    tbSearch.Tag = nIdx + szWord.Length;
                    tbSearch.Focus();
                }
            }
        }

        // Save.Remote
        private async void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            TabPage? page = tabControl1.SelectedTab;
            if (page == null)
                return;

            if (fnbIsModified())
            {
                var ctrls = fnGetTabControl(page);
                string szFilePath = ctrls.tbPath.Text;
                string szContent = ctrls.editorEx.Text;

                if (Owner == null)
                    return;

                frmControlPanel f = (frmControlPanel)Owner;
                if (await f.fnbFileWrite(szFilePath, szContent))
                    page.Text = page.Text.Replace("*", string.Empty);
            }
        }

        // Save.Local
        private void toolStripMenuItem5_Click(object sender, EventArgs e)
        {
            TabPage? page = tabControl1.SelectedTab;
            if (page == null)
                return;

            var ctrls = fnGetTabControl(page);

            string szFilePath = ctrls.tbPath.Text;
            string szContent = ctrls.editorEx.Text;

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.FileName = Path.GetFileName(szFilePath);

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(sfd.FileName, szContent);
            }
        }

        // SaveAll.Remote
        private async void toolStripMenuItem6_Click(object sender, EventArgs e)
        {
            foreach (TabPage page in tabControl1.TabPages)
            {
                var ctrls = fnGetTabControl(page);
                string szFilePath = ctrls.tbPath.Text;
                string szContent = ctrls.editorEx.Text;

                if (Owner == null)
                    return;

                frmControlPanel f = (frmControlPanel)Owner;
                if (await f.fnbFileWrite(szFilePath, szContent))
                    page.Text = page.Text.Replace("*", string.Empty);
            }
        }

        // SaveAll.Local
        private void toolStripMenuItem7_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                string szDir = fbd.SelectedPath;
                if (!Directory.Exists(szDir))
                {
                    MessageBox.Show("Directory does not exist: " + szDir, "Not Found!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                Dictionary<string, string> dicContent = tabControl1.TabPages.Cast<TabPage>().Select(x => fnGetTabControl(x)).ToDictionary(ctrls => ctrls.tbPath.Text, ctrls => ctrls.editorEx.Text);

                _ = Task.Run(() =>
                {
                    try
                    {
                        foreach (var szPath in dicContent.Keys)
                        {
                            string szFileName = Path.GetFileName(szPath);
                            string szSavePath = Path.Combine(szDir, szFileName);

                            File.WriteAllText(szSavePath, dicContent[szPath]);
                        }

                        MessageBox.Show("All files are saved!", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                });
            }
        }
    }
}
