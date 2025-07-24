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

        void fnSetup()
        {
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
            TextEditorControlEx editor = new TextEditorControlEx();
            TextBox tb = new TextBox();
        }
    }
}
