namespace Alien
{
    partial class frmEncoder
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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmEncoder));
            tabControl1 = new TabControl();
            tabPage1 = new TabPage();
            splitContainer1 = new SplitContainer();
            textBox2 = new TextBox();
            textBox3 = new TextBox();
            toolStrip1 = new ToolStrip();
            toolStripButton1 = new ToolStripButton();
            toolStripButton2 = new ToolStripButton();
            tabPage4 = new TabPage();
            splitContainer3 = new SplitContainer();
            textBox6 = new TextBox();
            textBox7 = new TextBox();
            toolStrip3 = new ToolStrip();
            toolStripButton5 = new ToolStripButton();
            toolStripButton6 = new ToolStripButton();
            tabPage2 = new TabPage();
            splitContainer2 = new SplitContainer();
            textBox4 = new TextBox();
            textBox5 = new TextBox();
            toolStrip2 = new ToolStrip();
            toolStripButton3 = new ToolStripButton();
            toolStripButton4 = new ToolStripButton();
            tabPage3 = new TabPage();
            listView1 = new ListView();
            columnHeader1 = new ColumnHeader();
            columnHeader2 = new ColumnHeader();
            contextMenuStrip1 = new ContextMenuStrip(components);
            toolStripMenuItem1 = new ToolStripMenuItem();
            toolStripMenuItem2 = new ToolStripMenuItem();
            textBox1 = new TextBox();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            toolStrip1.SuspendLayout();
            tabPage4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer3).BeginInit();
            splitContainer3.Panel1.SuspendLayout();
            splitContainer3.Panel2.SuspendLayout();
            splitContainer3.SuspendLayout();
            toolStrip3.SuspendLayout();
            tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer2).BeginInit();
            splitContainer2.Panel1.SuspendLayout();
            splitContainer2.Panel2.SuspendLayout();
            splitContainer2.SuspendLayout();
            toolStrip2.SuspendLayout();
            tabPage3.SuspendLayout();
            contextMenuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Controls.Add(tabPage4);
            tabControl1.Controls.Add(tabPage2);
            tabControl1.Controls.Add(tabPage3);
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.Location = new Point(0, 0);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(1001, 461);
            tabControl1.SizeMode = TabSizeMode.Fixed;
            tabControl1.TabIndex = 0;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(splitContainer1);
            tabPage1.Controls.Add(toolStrip1);
            tabPage1.Location = new Point(4, 28);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(993, 429);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "Base64";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(3, 29);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(textBox2);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(textBox3);
            splitContainer1.Size = new Size(987, 397);
            splitContainer1.SplitterDistance = 453;
            splitContainer1.TabIndex = 1;
            // 
            // textBox2
            // 
            textBox2.Dock = DockStyle.Fill;
            textBox2.Location = new Point(0, 0);
            textBox2.Multiline = true;
            textBox2.Name = "textBox2";
            textBox2.Size = new Size(453, 397);
            textBox2.TabIndex = 1;
            // 
            // textBox3
            // 
            textBox3.Dock = DockStyle.Fill;
            textBox3.Location = new Point(0, 0);
            textBox3.Multiline = true;
            textBox3.Name = "textBox3";
            textBox3.Size = new Size(530, 397);
            textBox3.TabIndex = 1;
            // 
            // toolStrip1
            // 
            toolStrip1.Font = new Font("Microsoft JhengHei UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 136);
            toolStrip1.Items.AddRange(new ToolStripItem[] { toolStripButton1, toolStripButton2 });
            toolStrip1.Location = new Point(3, 3);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new Size(987, 26);
            toolStrip1.TabIndex = 0;
            toolStrip1.Text = "toolStrip1";
            // 
            // toolStripButton1
            // 
            toolStripButton1.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripButton1.Image = (Image)resources.GetObject("toolStripButton1.Image");
            toolStripButton1.ImageTransparentColor = Color.Magenta;
            toolStripButton1.Name = "toolStripButton1";
            toolStripButton1.Size = new Size(64, 23);
            toolStripButton1.Text = "Encode";
            toolStripButton1.Click += toolStripButton1_Click;
            // 
            // toolStripButton2
            // 
            toolStripButton2.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripButton2.Image = (Image)resources.GetObject("toolStripButton2.Image");
            toolStripButton2.ImageTransparentColor = Color.Magenta;
            toolStripButton2.Name = "toolStripButton2";
            toolStripButton2.Size = new Size(66, 23);
            toolStripButton2.Text = "Decode";
            toolStripButton2.Click += toolStripButton2_Click;
            // 
            // tabPage4
            // 
            tabPage4.Controls.Add(splitContainer3);
            tabPage4.Controls.Add(toolStrip3);
            tabPage4.Location = new Point(4, 24);
            tabPage4.Name = "tabPage4";
            tabPage4.Size = new Size(993, 433);
            tabPage4.TabIndex = 3;
            tabPage4.Text = "Hex";
            tabPage4.UseVisualStyleBackColor = true;
            // 
            // splitContainer3
            // 
            splitContainer3.Dock = DockStyle.Fill;
            splitContainer3.Location = new Point(0, 26);
            splitContainer3.Name = "splitContainer3";
            // 
            // splitContainer3.Panel1
            // 
            splitContainer3.Panel1.Controls.Add(textBox6);
            // 
            // splitContainer3.Panel2
            // 
            splitContainer3.Panel2.Controls.Add(textBox7);
            splitContainer3.Size = new Size(993, 407);
            splitContainer3.SplitterDistance = 455;
            splitContainer3.TabIndex = 3;
            // 
            // textBox6
            // 
            textBox6.Dock = DockStyle.Fill;
            textBox6.Location = new Point(0, 0);
            textBox6.Multiline = true;
            textBox6.Name = "textBox6";
            textBox6.Size = new Size(455, 407);
            textBox6.TabIndex = 1;
            // 
            // textBox7
            // 
            textBox7.Dock = DockStyle.Fill;
            textBox7.Location = new Point(0, 0);
            textBox7.Multiline = true;
            textBox7.Name = "textBox7";
            textBox7.Size = new Size(534, 407);
            textBox7.TabIndex = 1;
            // 
            // toolStrip3
            // 
            toolStrip3.Font = new Font("Microsoft JhengHei UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 136);
            toolStrip3.Items.AddRange(new ToolStripItem[] { toolStripButton5, toolStripButton6 });
            toolStrip3.Location = new Point(0, 0);
            toolStrip3.Name = "toolStrip3";
            toolStrip3.Size = new Size(993, 26);
            toolStrip3.TabIndex = 2;
            toolStrip3.Text = "toolStrip3";
            // 
            // toolStripButton5
            // 
            toolStripButton5.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripButton5.Image = (Image)resources.GetObject("toolStripButton5.Image");
            toolStripButton5.ImageTransparentColor = Color.Magenta;
            toolStripButton5.Name = "toolStripButton5";
            toolStripButton5.Size = new Size(64, 23);
            toolStripButton5.Text = "Encode";
            toolStripButton5.Click += toolStripButton5_Click;
            // 
            // toolStripButton6
            // 
            toolStripButton6.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripButton6.Image = (Image)resources.GetObject("toolStripButton6.Image");
            toolStripButton6.ImageTransparentColor = Color.Magenta;
            toolStripButton6.Name = "toolStripButton6";
            toolStripButton6.Size = new Size(66, 23);
            toolStripButton6.Text = "Decode";
            toolStripButton6.Click += toolStripButton6_Click;
            // 
            // tabPage2
            // 
            tabPage2.Controls.Add(splitContainer2);
            tabPage2.Controls.Add(toolStrip2);
            tabPage2.Location = new Point(4, 24);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(993, 433);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "URL";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // splitContainer2
            // 
            splitContainer2.Dock = DockStyle.Fill;
            splitContainer2.Location = new Point(3, 29);
            splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            splitContainer2.Panel1.Controls.Add(textBox4);
            // 
            // splitContainer2.Panel2
            // 
            splitContainer2.Panel2.Controls.Add(textBox5);
            splitContainer2.Size = new Size(987, 401);
            splitContainer2.SplitterDistance = 453;
            splitContainer2.TabIndex = 3;
            // 
            // textBox4
            // 
            textBox4.Dock = DockStyle.Fill;
            textBox4.Location = new Point(0, 0);
            textBox4.Multiline = true;
            textBox4.Name = "textBox4";
            textBox4.Size = new Size(453, 401);
            textBox4.TabIndex = 2;
            // 
            // textBox5
            // 
            textBox5.Dock = DockStyle.Fill;
            textBox5.Location = new Point(0, 0);
            textBox5.Multiline = true;
            textBox5.Name = "textBox5";
            textBox5.Size = new Size(530, 401);
            textBox5.TabIndex = 2;
            // 
            // toolStrip2
            // 
            toolStrip2.Font = new Font("Microsoft JhengHei UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 136);
            toolStrip2.Items.AddRange(new ToolStripItem[] { toolStripButton3, toolStripButton4 });
            toolStrip2.Location = new Point(3, 3);
            toolStrip2.Name = "toolStrip2";
            toolStrip2.Size = new Size(987, 26);
            toolStrip2.TabIndex = 2;
            toolStrip2.Text = "toolStrip2";
            // 
            // toolStripButton3
            // 
            toolStripButton3.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripButton3.Image = (Image)resources.GetObject("toolStripButton3.Image");
            toolStripButton3.ImageTransparentColor = Color.Magenta;
            toolStripButton3.Name = "toolStripButton3";
            toolStripButton3.Size = new Size(64, 23);
            toolStripButton3.Text = "Encode";
            toolStripButton3.Click += toolStripButton3_Click;
            // 
            // toolStripButton4
            // 
            toolStripButton4.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripButton4.Image = (Image)resources.GetObject("toolStripButton4.Image");
            toolStripButton4.ImageTransparentColor = Color.Magenta;
            toolStripButton4.Name = "toolStripButton4";
            toolStripButton4.Size = new Size(66, 23);
            toolStripButton4.Text = "Decode";
            toolStripButton4.Click += toolStripButton4_Click;
            // 
            // tabPage3
            // 
            tabPage3.Controls.Add(listView1);
            tabPage3.Controls.Add(textBox1);
            tabPage3.Location = new Point(4, 24);
            tabPage3.Name = "tabPage3";
            tabPage3.Size = new Size(993, 433);
            tabPage3.TabIndex = 2;
            tabPage3.Text = "Hashing";
            tabPage3.UseVisualStyleBackColor = true;
            // 
            // listView1
            // 
            listView1.Columns.AddRange(new ColumnHeader[] { columnHeader1, columnHeader2 });
            listView1.ContextMenuStrip = contextMenuStrip1;
            listView1.Dock = DockStyle.Fill;
            listView1.FullRowSelect = true;
            listView1.Location = new Point(0, 27);
            listView1.Name = "listView1";
            listView1.Size = new Size(993, 406);
            listView1.TabIndex = 1;
            listView1.UseCompatibleStateImageBehavior = false;
            listView1.View = View.Details;
            // 
            // columnHeader1
            // 
            columnHeader1.Text = "Algorithm";
            columnHeader1.Width = 200;
            // 
            // columnHeader2
            // 
            columnHeader2.Text = "Value";
            columnHeader2.Width = 600;
            // 
            // contextMenuStrip1
            // 
            contextMenuStrip1.Font = new Font("Microsoft JhengHei UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 136);
            contextMenuStrip1.Items.AddRange(new ToolStripItem[] { toolStripMenuItem1, toolStripMenuItem2 });
            contextMenuStrip1.Name = "contextMenuStrip1";
            contextMenuStrip1.Size = new Size(181, 74);
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new Size(180, 24);
            toolStripMenuItem1.Text = "Copy";
            toolStripMenuItem1.Click += toolStripMenuItem1_Click;
            // 
            // toolStripMenuItem2
            // 
            toolStripMenuItem2.Name = "toolStripMenuItem2";
            toolStripMenuItem2.Size = new Size(180, 24);
            toolStripMenuItem2.Text = "Clear";
            toolStripMenuItem2.Click += toolStripMenuItem2_Click;
            // 
            // textBox1
            // 
            textBox1.Dock = DockStyle.Top;
            textBox1.Location = new Point(0, 0);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(993, 27);
            textBox1.TabIndex = 0;
            textBox1.TextChanged += textBox1_TextChanged;
            // 
            // frmEncoder
            // 
            AutoScaleDimensions = new SizeF(9F, 19F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1001, 461);
            Controls.Add(tabControl1);
            Font = new Font("Microsoft JhengHei UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 136);
            Margin = new Padding(4);
            Name = "frmEncoder";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "frmEncoder";
            Load += frmEncoder_Load;
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            tabPage1.PerformLayout();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel1.PerformLayout();
            splitContainer1.Panel2.ResumeLayout(false);
            splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            tabPage4.ResumeLayout(false);
            tabPage4.PerformLayout();
            splitContainer3.Panel1.ResumeLayout(false);
            splitContainer3.Panel1.PerformLayout();
            splitContainer3.Panel2.ResumeLayout(false);
            splitContainer3.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer3).EndInit();
            splitContainer3.ResumeLayout(false);
            toolStrip3.ResumeLayout(false);
            toolStrip3.PerformLayout();
            tabPage2.ResumeLayout(false);
            tabPage2.PerformLayout();
            splitContainer2.Panel1.ResumeLayout(false);
            splitContainer2.Panel1.PerformLayout();
            splitContainer2.Panel2.ResumeLayout(false);
            splitContainer2.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer2).EndInit();
            splitContainer2.ResumeLayout(false);
            toolStrip2.ResumeLayout(false);
            toolStrip2.PerformLayout();
            tabPage3.ResumeLayout(false);
            tabPage3.PerformLayout();
            contextMenuStrip1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private TabControl tabControl1;
        private TabPage tabPage1;
        private SplitContainer splitContainer1;
        private ToolStrip toolStrip1;
        private ToolStripButton toolStripButton1;
        private ToolStripButton toolStripButton2;
        private TabPage tabPage2;
        private TabPage tabPage3;
        private SplitContainer splitContainer2;
        private ToolStrip toolStrip2;
        private ToolStripButton toolStripButton3;
        private ToolStripButton toolStripButton4;
        private ListView listView1;
        private ColumnHeader columnHeader1;
        private ColumnHeader columnHeader2;
        private ContextMenuStrip contextMenuStrip1;
        private ToolStripMenuItem toolStripMenuItem1;
        private ToolStripMenuItem toolStripMenuItem2;
        private TextBox textBox1;
        private TextBox textBox2;
        private TextBox textBox3;
        private TextBox textBox4;
        private TextBox textBox5;
        private TabPage tabPage4;
        private SplitContainer splitContainer3;
        private TextBox textBox6;
        private TextBox textBox7;
        private ToolStrip toolStrip3;
        private ToolStripButton toolStripButton5;
        private ToolStripButton toolStripButton6;
    }
}