namespace Alien
{
    partial class frmSetting
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmSetting));
            label1 = new Label();
            comboBox1 = new ComboBox();
            tabControl1 = new TabControl();
            tabPage1 = new TabPage();
            checkBox1 = new CheckBox();
            toolStrip1 = new ToolStrip();
            toolStripButton1 = new ToolStripButton();
            comboBox2 = new ComboBox();
            label2 = new Label();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            toolStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // label1
            // 
            resources.ApplyResources(label1, "label1");
            label1.Name = "label1";
            // 
            // comboBox1
            // 
            comboBox1.FormattingEnabled = true;
            resources.ApplyResources(comboBox1, "comboBox1");
            comboBox1.Name = "comboBox1";
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPage1);
            resources.ApplyResources(tabControl1, "tabControl1");
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.SizeMode = TabSizeMode.Fixed;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(comboBox2);
            tabPage1.Controls.Add(label2);
            tabPage1.Controls.Add(checkBox1);
            tabPage1.Controls.Add(comboBox1);
            tabPage1.Controls.Add(label1);
            resources.ApplyResources(tabPage1, "tabPage1");
            tabPage1.Name = "tabPage1";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // checkBox1
            // 
            resources.ApplyResources(checkBox1, "checkBox1");
            checkBox1.Name = "checkBox1";
            checkBox1.UseVisualStyleBackColor = true;
            // 
            // toolStrip1
            // 
            resources.ApplyResources(toolStrip1, "toolStrip1");
            toolStrip1.Items.AddRange(new ToolStripItem[] { toolStripButton1 });
            toolStrip1.Name = "toolStrip1";
            // 
            // toolStripButton1
            // 
            toolStripButton1.DisplayStyle = ToolStripItemDisplayStyle.Text;
            resources.ApplyResources(toolStripButton1, "toolStripButton1");
            toolStripButton1.Name = "toolStripButton1";
            toolStripButton1.Click += toolStripButton1_Click;
            // 
            // comboBox2
            // 
            comboBox2.FormattingEnabled = true;
            resources.ApplyResources(comboBox2, "comboBox2");
            comboBox2.Name = "comboBox2";
            // 
            // label2
            // 
            resources.ApplyResources(label2, "label2");
            label2.Name = "label2";
            // 
            // frmSetting
            // 
            resources.ApplyResources(this, "$this");
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(tabControl1);
            Controls.Add(toolStrip1);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            MaximizeBox = false;
            Name = "frmSetting";
            Load += frmSetting_Load;
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            tabPage1.PerformLayout();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private ComboBox comboBox1;
        private TabControl tabControl1;
        private TabPage tabPage1;
        private ToolStrip toolStrip1;
        private ToolStripButton toolStripButton1;
        private CheckBox checkBox1;
        private ComboBox comboBox2;
        private Label label2;
    }
}