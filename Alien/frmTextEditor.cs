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

namespace Alien
{
    public partial class frmTextEditor : BaseForm
    {
        public frmTextEditor()
        {
            InitializeComponent();

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
            frmControlPanel f = (frmControlPanel)ParentForm;
            foreach (TabPage page in tabControl1.TabPages)
                tabControl1.TabPages.Remove(page);

            tabControl1.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl1.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl1.Padding = new Point(18, 5);
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

        private async void tabControl1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Modifiers == Keys.Control)
            {
                TabPage? page = tabControl1.SelectedTab;
                if (page == null)
                    return;

                switch (e.KeyCode)
                {
                    case Keys.W: //Close page.
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
                    case Keys.S: //Save
                        if (fnbIsModified())
                        {
                            var ctrls = fnGetTabControl();
                            string szFilePath = ctrls.tbPath.Text;
                            string szContent = ctrls.editorEx.Text;

                            if (Owner == null)
                                return;

                            frmControlPanel f = (frmControlPanel)Owner;
                            if (await f.fnbFileWrite(szFilePath, szContent))
                            {
                                page.Text = page.Text.Replace("*", string.Empty);

                                if (!string.Equals(Path.GetFileName(szFilePath), page.Text))
                                    page.Text = Path.GetFileName(szFilePath);
                            }
                        }

                        break;
                }
            }
        }

        private void tabControl1_DrawItem(object sender, DrawItemEventArgs e)
        {
            TabControl tab = (TabControl)sender;
            if (tab == null)
                return;

            TabPage page = tab.TabPages[e.Index];
            Rectangle rect = tab.GetTabRect(e.Index);

            bool bIsSelected = e.Index == tab.SelectedIndex;

            // White background, black text
            Color backColor = Color.White;
            Color foreColor = Color.Black;

            if (page.Tag is clsTabColor tc)
            {
                backColor = tc.BackColor;
                foreColor = tc.ForeColor;
            }

            using (SolidBrush bg = new SolidBrush(backColor))
                e.Graphics.FillRectangle(bg, rect);

            TextRenderer.DrawText(
                e.Graphics,
                page.Text,
                page.Font,
                new Rectangle(rect.X + 6, rect.Y + 4, rect.Width - 20, rect.Height),
                foreColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter
            );

            Rectangle closeRect = new Rectangle(
                rect.Right - 15,
                rect.Top + (rect.Height - 10) / 2,
                10,
                10
            );

            using (Pen pen = new Pen(Color.Red, 2))
            {
                e.Graphics.DrawLine(pen, closeRect.Left, closeRect.Top, closeRect.Right, closeRect.Bottom);
                e.Graphics.DrawLine(pen, closeRect.Right, closeRect.Top, closeRect.Left, closeRect.Bottom);
            }
        }

        private void tabControl1_MouseDown(object sender, MouseEventArgs e)
        {
            TabControl tab = (TabControl)sender;

            for (int i = 0; i < tab.TabPages.Count; i++)
            {
                Rectangle rect = tab.GetTabRect(i);

                Rectangle closeRect = new Rectangle(
                    rect.Right - 15,
                    rect.Top + (rect.Height - 10) / 2,
                    10,
                    10
                );

                if (closeRect.Contains(e.Location))
                {
                    tab.TabPages.RemoveAt(i);
                    return;
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
