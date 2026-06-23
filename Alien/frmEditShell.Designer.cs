namespace Alien
{
    partial class frmEditShell
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
            textBox1 = new TextBox();
            button1 = new Button();
            button2 = new Button();
            textBox2 = new TextBox();
            label2 = new Label();
            groupBox1 = new GroupBox();
            button3 = new Button();
            label8 = new Label();
            comboBox6 = new ComboBox();
            label7 = new Label();
            comboBox5 = new ComboBox();
            comboBox4 = new ComboBox();
            label6 = new Label();
            comboBox3 = new ComboBox();
            label5 = new Label();
            label3 = new Label();
            comboBox1 = new ComboBox();
            groupBox2 = new GroupBox();
            checkBox2 = new CheckBox();
            groupBox3 = new GroupBox();
            textEditorControl1 = new ICSharpCode.TextEditor.TextEditorControl();
            comboBox2 = new ComboBox();
            label4 = new Label();
            checkBox1 = new CheckBox();
            label9 = new Label();
            textBox3 = new TextBox();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            groupBox3.SuspendLayout();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(47, 23);
            label1.Name = "label1";
            label1.Size = new Size(45, 19);
            label1.TabIndex = 0;
            label1.Text = "URL: ";
            // 
            // textBox1
            // 
            textBox1.Location = new Point(98, 20);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(370, 27);
            textBox1.TabIndex = 1;
            textBox1.TextChanged += textBox1_TextChanged;
            // 
            // button1
            // 
            button1.Location = new Point(12, 548);
            button1.Name = "button1";
            button1.Size = new Size(231, 47);
            button1.TabIndex = 2;
            button1.Text = "Test Shell";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // button2
            // 
            button2.Location = new Point(249, 548);
            button2.Name = "button2";
            button2.Size = new Size(237, 47);
            button2.TabIndex = 3;
            button2.Text = "Save";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // textBox2
            // 
            textBox2.Location = new Point(98, 53);
            textBox2.Name = "textBox2";
            textBox2.Size = new Size(370, 27);
            textBox2.TabIndex = 5;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(8, 56);
            label2.Name = "label2";
            label2.Size = new Size(84, 19);
            label2.TabIndex = 4;
            label2.Text = "Password :";
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(textBox3);
            groupBox1.Controls.Add(label9);
            groupBox1.Controls.Add(button3);
            groupBox1.Controls.Add(label8);
            groupBox1.Controls.Add(comboBox6);
            groupBox1.Controls.Add(label7);
            groupBox1.Controls.Add(comboBox5);
            groupBox1.Controls.Add(comboBox4);
            groupBox1.Controls.Add(label6);
            groupBox1.Controls.Add(comboBox3);
            groupBox1.Controls.Add(label5);
            groupBox1.Controls.Add(label3);
            groupBox1.Controls.Add(comboBox1);
            groupBox1.Controls.Add(label1);
            groupBox1.Controls.Add(textBox1);
            groupBox1.Controls.Add(label2);
            groupBox1.Controls.Add(textBox2);
            groupBox1.Location = new Point(12, 12);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(474, 289);
            groupBox1.TabIndex = 7;
            groupBox1.TabStop = false;
            groupBox1.Text = "Shell";
            // 
            // button3
            // 
            button3.Location = new Point(368, 85);
            button3.Name = "button3";
            button3.Size = new Size(100, 28);
            button3.TabIndex = 16;
            button3.Text = "...";
            button3.UseVisualStyleBackColor = true;
            button3.Click += button3_Click;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(32, 89);
            label8.Name = "label8";
            label8.Size = new Size(60, 19);
            label8.TabIndex = 15;
            label8.Text = "Group :";
            // 
            // comboBox6
            // 
            comboBox6.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox6.FormattingEnabled = true;
            comboBox6.Location = new Point(98, 86);
            comboBox6.Name = "comboBox6";
            comboBox6.Size = new Size(264, 27);
            comboBox6.TabIndex = 14;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(10, 122);
            label7.Name = "label7";
            label7.Size = new Size(82, 19);
            label7.TabIndex = 13;
            label7.Text = "Encoding :";
            // 
            // comboBox5
            // 
            comboBox5.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox5.FormattingEnabled = true;
            comboBox5.Location = new Point(98, 119);
            comboBox5.Name = "comboBox5";
            comboBox5.Size = new Size(370, 27);
            comboBox5.TabIndex = 12;
            // 
            // comboBox4
            // 
            comboBox4.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox4.FormattingEnabled = true;
            comboBox4.Location = new Point(98, 185);
            comboBox4.Name = "comboBox4";
            comboBox4.Size = new Size(370, 27);
            comboBox4.TabIndex = 11;
            comboBox4.SelectedIndexChanged += comboBox4_SelectedIndexChanged;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(21, 188);
            label6.Name = "label6";
            label6.Size = new Size(71, 19);
            label6.TabIndex = 10;
            label6.Text = "Method :";
            // 
            // comboBox3
            // 
            comboBox3.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox3.FormattingEnabled = true;
            comboBox3.Location = new Point(98, 218);
            comboBox3.Name = "comboBox3";
            comboBox3.Size = new Size(370, 27);
            comboBox3.TabIndex = 9;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(43, 221);
            label5.Name = "label5";
            label5.Size = new Size(49, 19);
            label5.TabIndex = 8;
            label5.Text = "Type :";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(6, 155);
            label3.Name = "label3";
            label3.Size = new Size(86, 19);
            label3.TabIndex = 7;
            label3.Text = "Language :";
            // 
            // comboBox1
            // 
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox1.FormattingEnabled = true;
            comboBox1.Location = new Point(98, 152);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new Size(370, 27);
            comboBox1.TabIndex = 6;
            comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(checkBox2);
            groupBox2.Controls.Add(groupBox3);
            groupBox2.Controls.Add(checkBox1);
            groupBox2.Location = new Point(12, 307);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(474, 241);
            groupBox2.TabIndex = 8;
            groupBox2.TabStop = false;
            groupBox2.Text = "Cryptography Tamper";
            // 
            // checkBox2
            // 
            checkBox2.AutoSize = true;
            checkBox2.Location = new Point(10, 26);
            checkBox2.Name = "checkBox2";
            checkBox2.Size = new Size(74, 23);
            checkBox2.TabIndex = 11;
            checkBox2.Text = "Enable";
            checkBox2.UseVisualStyleBackColor = true;
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(textEditorControl1);
            groupBox3.Controls.Add(comboBox2);
            groupBox3.Controls.Add(label4);
            groupBox3.Location = new Point(8, 55);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new Size(460, 180);
            groupBox3.TabIndex = 10;
            groupBox3.TabStop = false;
            // 
            // textEditorControl1
            // 
            textEditorControl1.Font = new Font("Courier New", 10F);
            textEditorControl1.IsReadOnly = false;
            textEditorControl1.Location = new Point(6, 53);
            textEditorControl1.Name = "textEditorControl1";
            textEditorControl1.Size = new Size(448, 121);
            textEditorControl1.TabIndex = 3;
            textEditorControl1.Text = "textEditorControl1";
            // 
            // comboBox2
            // 
            comboBox2.FormattingEnabled = true;
            comboBox2.Location = new Point(79, 20);
            comboBox2.Name = "comboBox2";
            comboBox2.Size = new Size(375, 27);
            comboBox2.TabIndex = 2;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(6, 23);
            label4.Name = "label4";
            label4.Size = new Size(56, 19);
            label4.TabIndex = 1;
            label4.Text = "Script :";
            // 
            // checkBox1
            // 
            checkBox1.AutoSize = true;
            checkBox1.Location = new Point(87, 26);
            checkBox1.Name = "checkBox1";
            checkBox1.Size = new Size(58, 23);
            checkBox1.TabIndex = 9;
            checkBox1.Text = "Raw";
            checkBox1.UseVisualStyleBackColor = true;
            checkBox1.CheckedChanged += checkBox1_CheckedChanged;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(43, 256);
            label9.Name = "label9";
            label9.Size = new Size(49, 19);
            label9.TabIndex = 17;
            label9.Text = "Desc :";
            // 
            // textBox3
            // 
            textBox3.Location = new Point(98, 253);
            textBox3.Name = "textBox3";
            textBox3.Size = new Size(370, 27);
            textBox3.TabIndex = 18;
            // 
            // frmEditShell
            // 
            AutoScaleDimensions = new SizeF(9F, 19F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(498, 602);
            Controls.Add(groupBox2);
            Controls.Add(groupBox1);
            Controls.Add(button2);
            Controls.Add(button1);
            Font = new Font("Microsoft JhengHei UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 136);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Margin = new Padding(4);
            Name = "frmEditShell";
            Text = "frmEditShell";
            Load += frmEditShell_Load;
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            groupBox3.ResumeLayout(false);
            groupBox3.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Label label1;
        private TextBox textBox1;
        private Button button1;
        private Button button2;
        private TextBox textBox2;
        private Label label2;
        private GroupBox groupBox1;
        private Label label3;
        private ComboBox comboBox1;
        private GroupBox groupBox2;
        private GroupBox groupBox3;
        private Label label4;
        private CheckBox checkBox1;
        private ComboBox comboBox2;
        private ComboBox comboBox3;
        private Label label5;
        private ComboBox comboBox4;
        private Label label6;
        private Label label7;
        private ComboBox comboBox5;
        private Label label8;
        private ComboBox comboBox6;
        private Button button3;
        private CheckBox checkBox2;
        private ICSharpCode.TextEditor.TextEditorControl textEditorControl1;
        private TextBox textBox3;
        private Label label9;
    }
}