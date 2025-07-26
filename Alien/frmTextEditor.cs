using ICSharpCode.TextEditor;
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
    public partial class frmTextEditor : Form
    {
        public frmTextEditor()
        {
            InitializeComponent();
        }

        #region Tool

        (TabPage page, TextEditorControlEx editorEx, TextBox tbPath, TextBox tbSearch) fnGetTabControl(TabPage page = null)
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

        private bool fnbIsModified(TabPage page = null)
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

        void fnSetup()
        {
            frmControlPanel f = (frmControlPanel)ParentForm;
            foreach (TabPage page in tabControl1.TabPages)
                tabControl1.TabPages.Remove(page);
        }

        private void frmTextEditor_Load(object sender, EventArgs e)
        {
            fnSetup();
        }

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

            editor.TextChanged += TextEditorEx_TextChange;
            tbSearch.KeyDown += TextboxSearch_KeyDown;
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

            }
        }

        private async void tabControl1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Modifiers == Keys.Control)
            {
                TabPage page = tabControl1.SelectedTab;
                if (page == null)
                    return;

                switch (e.KeyCode)
                {
                    case Keys.W: //Close page.
                        tabControl1.TabPages.Remove(page);
                        break;
                    case Keys.S: //Save
                        if (fnbIsModified())
                        {
                            var ctrls = fnGetTabControl();
                            string szFilePath = ctrls.tbPath.Text;
                            string szContent = ctrls.editorEx.Text;

                            frmControlPanel f = (frmControlPanel)Owner;
                            if (await f.fnbFileWrite(szFilePath, szContent))
                                page.Text = page.Text.Replace("*", string.Empty);
                        }

                        break;
                }
            }
        }
    }
}
