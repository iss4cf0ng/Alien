namespace Alien
{
    partial class frmBuilder
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
            label1 = new Label();
            comboBox1 = new ComboBox();
            textEditorControl1 = new ICSharpCode.TextEditor.TextEditorControl();
            button1 = new Button();
            tabControl1 = new TabControl();
            tabPage1 = new TabPage();
            textBox3 = new TextBox();
            textBox2 = new TextBox();
            button5 = new Button();
            button4 = new Button();
            textBox1 = new TextBox();
            button3 = new Button();
            button2 = new Button();
            label2 = new Label();
            comboBox2 = new ComboBox();
            groupBox2 = new GroupBox();
            textEditorControl2 = new ICSharpCode.TextEditor.TextEditorControl();
            groupBox1 = new GroupBox();
            tabPage2 = new TabPage();
            checkBox1 = new CheckBox();
            groupBox3 = new GroupBox();
            textEditorControl3 = new ICSharpCode.TextEditor.TextEditorControl();
            button7 = new Button();
            button6 = new Button();
            textBox4 = new TextBox();
            label4 = new Label();
            label3 = new Label();
            comboBox3 = new ComboBox();
            label5 = new Label();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            groupBox2.SuspendLayout();
            groupBox1.SuspendLayout();
            tabPage2.SuspendLayout();
            groupBox3.SuspendLayout();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(21, 10);
            label1.Margin = new Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new Size(56, 19);
            label1.TabIndex = 0;
            label1.Text = "Script :";
            // 
            // comboBox1
            // 
            comboBox1.FormattingEnabled = true;
            comboBox1.Location = new Point(85, 7);
            comboBox1.Margin = new Padding(4);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new Size(154, 27);
            comboBox1.TabIndex = 1;
            comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
            // 
            // textEditorControl1
            // 
            textEditorControl1.Dock = DockStyle.Fill;
            textEditorControl1.Font = new Font("Courier New", 10F);
            textEditorControl1.IsReadOnly = false;
            textEditorControl1.Location = new Point(3, 23);
            textEditorControl1.Margin = new Padding(4);
            textEditorControl1.Name = "textEditorControl1";
            textEditorControl1.Size = new Size(378, 232);
            textEditorControl1.TabIndex = 3;
            textEditorControl1.Text = "textEditorControl1";
            textEditorControl1.TextChanged += textEditorControl1_TextChanged;
            // 
            // button1
            // 
            button1.Location = new Point(529, 360);
            button1.Margin = new Padding(4);
            button1.Name = "button1";
            button1.Size = new Size(253, 52);
            button1.TabIndex = 4;
            button1.Text = "Save";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Controls.Add(tabPage2);
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.Location = new Point(0, 0);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(800, 451);
            tabControl1.TabIndex = 5;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(textBox3);
            tabPage1.Controls.Add(textBox2);
            tabPage1.Controls.Add(button5);
            tabPage1.Controls.Add(button4);
            tabPage1.Controls.Add(textBox1);
            tabPage1.Controls.Add(button3);
            tabPage1.Controls.Add(button2);
            tabPage1.Controls.Add(label2);
            tabPage1.Controls.Add(comboBox2);
            tabPage1.Controls.Add(groupBox2);
            tabPage1.Controls.Add(groupBox1);
            tabPage1.Controls.Add(button1);
            tabPage1.Controls.Add(label1);
            tabPage1.Controls.Add(comboBox1);
            tabPage1.Location = new Point(4, 28);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(792, 419);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "Event Horizon";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // textBox3
            // 
            textBox3.Location = new Point(611, 61);
            textBox3.Name = "textBox3";
            textBox3.Size = new Size(171, 27);
            textBox3.TabIndex = 15;
            // 
            // textBox2
            // 
            textBox2.Location = new Point(310, 61);
            textBox2.Name = "textBox2";
            textBox2.Size = new Size(171, 27);
            textBox2.TabIndex = 14;
            // 
            // button5
            // 
            button5.Location = new Point(489, 61);
            button5.Name = "button5";
            button5.Size = new Size(116, 27);
            button5.TabIndex = 13;
            button5.Text = "-> Decrypt ->";
            button5.UseVisualStyleBackColor = true;
            button5.Click += button5_Click;
            // 
            // button4
            // 
            button4.Location = new Point(188, 62);
            button4.Name = "button4";
            button4.Size = new Size(116, 27);
            button4.TabIndex = 12;
            button4.Text = "-> Encrypt ->";
            button4.UseVisualStyleBackColor = true;
            button4.Click += button4_Click;
            // 
            // textBox1
            // 
            textBox1.Location = new Point(11, 62);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(171, 27);
            textBox1.TabIndex = 11;
            // 
            // button3
            // 
            button3.Location = new Point(268, 360);
            button3.Margin = new Padding(4);
            button3.Name = "button3";
            button3.Size = new Size(253, 52);
            button3.TabIndex = 10;
            button3.Text = "Copy Webshell";
            button3.UseVisualStyleBackColor = true;
            button3.Click += button3_Click;
            // 
            // button2
            // 
            button2.Location = new Point(7, 360);
            button2.Margin = new Padding(4);
            button2.Name = "button2";
            button2.Size = new Size(253, 52);
            button2.TabIndex = 9;
            button2.Text = "Copy JSON";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(267, 10);
            label2.Margin = new Padding(4, 0, 4, 0);
            label2.Name = "label2";
            label2.Size = new Size(82, 19);
            label2.TabIndex = 7;
            label2.Text = "Language:";
            // 
            // comboBox2
            // 
            comboBox2.FormattingEnabled = true;
            comboBox2.Location = new Point(357, 7);
            comboBox2.Margin = new Padding(4);
            comboBox2.Name = "comboBox2";
            comboBox2.Size = new Size(154, 27);
            comboBox2.TabIndex = 8;
            comboBox2.SelectedIndexChanged += comboBox2_SelectedIndexChanged;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(textEditorControl2);
            groupBox2.Location = new Point(398, 95);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(384, 258);
            groupBox2.TabIndex = 6;
            groupBox2.TabStop = false;
            groupBox2.Text = "Webshell";
            // 
            // textEditorControl2
            // 
            textEditorControl2.Dock = DockStyle.Fill;
            textEditorControl2.Font = new Font("Courier New", 10F);
            textEditorControl2.IsReadOnly = false;
            textEditorControl2.Location = new Point(3, 23);
            textEditorControl2.Margin = new Padding(4);
            textEditorControl2.Name = "textEditorControl2";
            textEditorControl2.Size = new Size(378, 232);
            textEditorControl2.TabIndex = 3;
            textEditorControl2.Text = "textEditorControl2";
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(textEditorControl1);
            groupBox1.Location = new Point(8, 95);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(384, 258);
            groupBox1.TabIndex = 5;
            groupBox1.TabStop = false;
            groupBox1.Text = "Parameters (JSON)";
            // 
            // tabPage2
            // 
            tabPage2.Controls.Add(label5);
            tabPage2.Controls.Add(checkBox1);
            tabPage2.Controls.Add(groupBox3);
            tabPage2.Controls.Add(button7);
            tabPage2.Controls.Add(button6);
            tabPage2.Controls.Add(textBox4);
            tabPage2.Controls.Add(label4);
            tabPage2.Controls.Add(label3);
            tabPage2.Controls.Add(comboBox3);
            tabPage2.Location = new Point(4, 28);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(792, 419);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "NebulaPulsar";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // checkBox1
            // 
            checkBox1.AutoSize = true;
            checkBox1.Location = new Point(616, 9);
            checkBox1.Name = "checkBox1";
            checkBox1.Size = new Size(92, 23);
            checkBox1.TabIndex = 16;
            checkBox1.Text = "Minimize";
            checkBox1.UseVisualStyleBackColor = true;
            checkBox1.CheckedChanged += checkBox1_CheckedChanged;
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(textEditorControl3);
            groupBox3.Location = new Point(9, 41);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new Size(775, 277);
            groupBox3.TabIndex = 15;
            groupBox3.TabStop = false;
            groupBox3.Text = "Webshell";
            // 
            // textEditorControl3
            // 
            textEditorControl3.Dock = DockStyle.Fill;
            textEditorControl3.Font = new Font("Courier New", 10F);
            textEditorControl3.IsReadOnly = false;
            textEditorControl3.Location = new Point(3, 23);
            textEditorControl3.Name = "textEditorControl3";
            textEditorControl3.Size = new Size(769, 251);
            textEditorControl3.TabIndex = 0;
            textEditorControl3.Text = "textEditorControl3";
            // 
            // button7
            // 
            button7.Location = new Point(406, 357);
            button7.Name = "button7";
            button7.Size = new Size(378, 54);
            button7.TabIndex = 14;
            button7.Text = "Save";
            button7.UseVisualStyleBackColor = true;
            button7.Click += button7_Click;
            // 
            // button6
            // 
            button6.Location = new Point(9, 357);
            button6.Name = "button6";
            button6.Size = new Size(378, 54);
            button6.TabIndex = 13;
            button6.Text = "Copy";
            button6.UseVisualStyleBackColor = true;
            button6.Click += button6_Click;
            // 
            // textBox4
            // 
            textBox4.Location = new Point(333, 7);
            textBox4.Name = "textBox4";
            textBox4.Size = new Size(256, 27);
            textBox4.TabIndex = 12;
            textBox4.TextChanged += textBox4_TextChanged;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(285, 10);
            label4.Margin = new Padding(4, 0, 4, 0);
            label4.Name = "label4";
            label4.Size = new Size(41, 19);
            label4.TabIndex = 11;
            label4.Text = "Key :";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(9, 10);
            label3.Margin = new Padding(4, 0, 4, 0);
            label3.Name = "label3";
            label3.Size = new Size(82, 19);
            label3.TabIndex = 9;
            label3.Text = "Language:";
            // 
            // comboBox3
            // 
            comboBox3.FormattingEnabled = true;
            comboBox3.Location = new Point(99, 7);
            comboBox3.Margin = new Padding(4);
            comboBox3.Name = "comboBox3";
            comboBox3.Size = new Size(154, 27);
            comboBox3.TabIndex = 10;
            comboBox3.SelectedIndexChanged += comboBox3_SelectedIndexChanged;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(9, 321);
            label5.Name = "label5";
            label5.Size = new Size(51, 19);
            label5.TabIndex = 17;
            label5.Text = "label5";
            // 
            // frmBuilder
            // 
            AutoScaleDimensions = new SizeF(9F, 19F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 451);
            Controls.Add(tabControl1);
            Font = new Font("Microsoft JhengHei UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 136);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Margin = new Padding(4);
            MaximizeBox = false;
            Name = "frmBuilder";
            Text = "frmBuilder";
            Load += frmBuilder_Load;
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            tabPage1.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox1.ResumeLayout(false);
            tabPage2.ResumeLayout(false);
            tabPage2.PerformLayout();
            groupBox3.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Label label1;
        private ComboBox comboBox1;
        private ICSharpCode.TextEditor.TextEditorControl textEditorControl1;
        private Button button1;
        private TabControl tabControl1;
        private TabPage tabPage1;
        private Label label2;
        private ComboBox comboBox2;
        private GroupBox groupBox2;
        private ICSharpCode.TextEditor.TextEditorControl textEditorControl2;
        private GroupBox groupBox1;
        private TabPage tabPage2;
        private Button button3;
        private Button button2;
        private TextBox textBox3;
        private TextBox textBox2;
        private Button button5;
        private Button button4;
        private TextBox textBox1;
        private TextBox textBox4;
        private Label label4;
        private Label label3;
        private ComboBox comboBox3;
        private GroupBox groupBox3;
        private ICSharpCode.TextEditor.TextEditorControl textEditorControl3;
        private Button button7;
        private Button button6;
        private CheckBox checkBox1;
        private Label label5;
    }
}