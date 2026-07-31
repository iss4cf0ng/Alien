namespace Alien
{
    partial class frmTextEditor
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmTextEditor));
            menuStrip1 = new MenuStrip();
            toolStripMenuItem1 = new ToolStripMenuItem();
            toolStripMenuItem2 = new ToolStripMenuItem();
            toolStripMenuItem4 = new ToolStripMenuItem();
            toolStripMenuItem5 = new ToolStripMenuItem();
            toolStripMenuItem3 = new ToolStripMenuItem();
            toolStripMenuItem6 = new ToolStripMenuItem();
            toolStripMenuItem7 = new ToolStripMenuItem();
            statusStrip1 = new StatusStrip();
            tabControl1 = new TabControl();
            tabPage1 = new TabPage();
            tabPage2 = new TabPage();
            menuStrip1.SuspendLayout();
            tabControl1.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            resources.ApplyResources(menuStrip1, "menuStrip1");
            menuStrip1.Items.AddRange(new ToolStripItem[] { toolStripMenuItem1 });
            menuStrip1.Name = "menuStrip1";
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.DropDownItems.AddRange(new ToolStripItem[] { toolStripMenuItem2, toolStripMenuItem3 });
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            resources.ApplyResources(toolStripMenuItem1, "toolStripMenuItem1");
            // 
            // toolStripMenuItem2
            // 
            toolStripMenuItem2.DropDownItems.AddRange(new ToolStripItem[] { toolStripMenuItem4, toolStripMenuItem5 });
            toolStripMenuItem2.Name = "toolStripMenuItem2";
            resources.ApplyResources(toolStripMenuItem2, "toolStripMenuItem2");
            // 
            // toolStripMenuItem4
            // 
            toolStripMenuItem4.Name = "toolStripMenuItem4";
            resources.ApplyResources(toolStripMenuItem4, "toolStripMenuItem4");
            toolStripMenuItem4.Click += toolStripMenuItem4_Click;
            // 
            // toolStripMenuItem5
            // 
            toolStripMenuItem5.Name = "toolStripMenuItem5";
            resources.ApplyResources(toolStripMenuItem5, "toolStripMenuItem5");
            toolStripMenuItem5.Click += toolStripMenuItem5_Click;
            // 
            // toolStripMenuItem3
            // 
            toolStripMenuItem3.DropDownItems.AddRange(new ToolStripItem[] { toolStripMenuItem6, toolStripMenuItem7 });
            toolStripMenuItem3.Name = "toolStripMenuItem3";
            resources.ApplyResources(toolStripMenuItem3, "toolStripMenuItem3");
            // 
            // toolStripMenuItem6
            // 
            toolStripMenuItem6.Name = "toolStripMenuItem6";
            resources.ApplyResources(toolStripMenuItem6, "toolStripMenuItem6");
            toolStripMenuItem6.Click += toolStripMenuItem6_Click;
            // 
            // toolStripMenuItem7
            // 
            toolStripMenuItem7.Name = "toolStripMenuItem7";
            resources.ApplyResources(toolStripMenuItem7, "toolStripMenuItem7");
            toolStripMenuItem7.Click += toolStripMenuItem7_Click;
            // 
            // statusStrip1
            // 
            resources.ApplyResources(statusStrip1, "statusStrip1");
            statusStrip1.Name = "statusStrip1";
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Controls.Add(tabPage2);
            resources.ApplyResources(tabControl1, "tabControl1");
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            // 
            // tabPage1
            // 
            resources.ApplyResources(tabPage1, "tabPage1");
            tabPage1.Name = "tabPage1";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            resources.ApplyResources(tabPage2, "tabPage2");
            tabPage2.Name = "tabPage2";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // frmTextEditor
            // 
            resources.ApplyResources(this, "$this");
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.Control;
            Controls.Add(tabControl1);
            Controls.Add(statusStrip1);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            Name = "frmTextEditor";
            Load += frmTextEditor_Load;
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            tabControl1.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private MenuStrip menuStrip1;
        private StatusStrip statusStrip1;
        private ToolStripMenuItem toolStripMenuItem1;
        private ToolStripMenuItem toolStripMenuItem2;
        private ToolStripMenuItem toolStripMenuItem4;
        private ToolStripMenuItem toolStripMenuItem5;
        private ToolStripMenuItem toolStripMenuItem3;
        private ToolStripMenuItem toolStripMenuItem6;
        private ToolStripMenuItem toolStripMenuItem7;
        private TabControl tabControl1;
        private TabPage tabPage1;
        private TabPage tabPage2;
    }
}