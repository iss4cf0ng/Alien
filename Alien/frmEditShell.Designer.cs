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
            label3 = new Label();
            comboBox1 = new ComboBox();
            groupBox2 = new GroupBox();
            groupBox3 = new GroupBox();
            label4 = new Label();
            checkBox1 = new CheckBox();
            comboBox2 = new ComboBox();
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
            // 
            // button1
            // 
            button1.Location = new Point(12, 502);
            button1.Name = "button1";
            button1.Size = new Size(195, 47);
            button1.TabIndex = 2;
            button1.Text = "Test Shell";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // button2
            // 
            button2.Location = new Point(213, 502);
            button2.Name = "button2";
            button2.Size = new Size(195, 47);
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
            groupBox1.Controls.Add(label3);
            groupBox1.Controls.Add(comboBox1);
            groupBox1.Controls.Add(label1);
            groupBox1.Controls.Add(textBox1);
            groupBox1.Controls.Add(label2);
            groupBox1.Controls.Add(textBox2);
            groupBox1.Location = new Point(12, 12);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(474, 143);
            groupBox1.TabIndex = 7;
            groupBox1.TabStop = false;
            groupBox1.Text = "Shell";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(6, 89);
            label3.Name = "label3";
            label3.Size = new Size(86, 19);
            label3.TabIndex = 7;
            label3.Text = "Language :";
            // 
            // comboBox1
            // 
            comboBox1.FormattingEnabled = true;
            comboBox1.Location = new Point(98, 86);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new Size(270, 27);
            comboBox1.TabIndex = 6;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(groupBox3);
            groupBox2.Controls.Add(checkBox1);
            groupBox2.Location = new Point(12, 161);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(474, 335);
            groupBox2.TabIndex = 8;
            groupBox2.TabStop = false;
            groupBox2.Text = "Cryptography Tamper";
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(comboBox2);
            groupBox3.Controls.Add(label4);
            groupBox3.Location = new Point(8, 55);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new Size(460, 274);
            groupBox3.TabIndex = 10;
            groupBox3.TabStop = false;
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
            checkBox1.Location = new Point(8, 26);
            checkBox1.Name = "checkBox1";
            checkBox1.Size = new Size(63, 23);
            checkBox1.TabIndex = 9;
            checkBox1.Text = "RAW";
            checkBox1.UseVisualStyleBackColor = true;
            checkBox1.CheckedChanged += checkBox1_CheckedChanged;
            // 
            // comboBox2
            // 
            comboBox2.FormattingEnabled = true;
            comboBox2.Location = new Point(79, 20);
            comboBox2.Name = "comboBox2";
            comboBox2.Size = new Size(375, 27);
            comboBox2.TabIndex = 2;
            // 
            // frmEditShell
            // 
            AutoScaleDimensions = new SizeF(9F, 19F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(498, 561);
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
    }
}