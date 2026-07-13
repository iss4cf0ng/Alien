namespace Alien
{
    partial class frmScanHost
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
            button1 = new Button();
            label1 = new Label();
            textBox1 = new TextBox();
            textBox2 = new TextBox();
            label2 = new Label();
            label3 = new Label();
            numericUpDown1 = new NumericUpDown();
            ((System.ComponentModel.ISupportInitialize)numericUpDown1).BeginInit();
            SuspendLayout();
            // 
            // button1
            // 
            button1.Location = new Point(12, 111);
            button1.Name = "button1";
            button1.Size = new Size(364, 42);
            button1.TabIndex = 0;
            button1.Text = "Go";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(48, 15);
            label1.Name = "label1";
            label1.Size = new Size(29, 19);
            label1.TabIndex = 1;
            label1.Text = "IP :";
            // 
            // textBox1
            // 
            textBox1.Location = new Point(83, 12);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(293, 27);
            textBox1.TabIndex = 2;
            // 
            // textBox2
            // 
            textBox2.Location = new Point(83, 45);
            textBox2.Name = "textBox2";
            textBox2.Size = new Size(293, 27);
            textBox2.TabIndex = 4;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(32, 48);
            label2.Name = "label2";
            label2.Size = new Size(45, 19);
            label2.TabIndex = 3;
            label2.Text = "Port :";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(12, 80);
            label3.Name = "label3";
            label3.Size = new Size(65, 19);
            label3.TabIndex = 5;
            label3.Text = "Thread :";
            // 
            // numericUpDown1
            // 
            numericUpDown1.Location = new Point(83, 78);
            numericUpDown1.Name = "numericUpDown1";
            numericUpDown1.Size = new Size(293, 27);
            numericUpDown1.TabIndex = 6;
            numericUpDown1.Value = new decimal(new int[] { 3, 0, 0, 0 });
            // 
            // frmScanHost
            // 
            AutoScaleDimensions = new SizeF(9F, 19F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(388, 165);
            Controls.Add(numericUpDown1);
            Controls.Add(label3);
            Controls.Add(textBox2);
            Controls.Add(label2);
            Controls.Add(textBox1);
            Controls.Add(label1);
            Controls.Add(button1);
            Font = new Font("Microsoft JhengHei UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 136);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Margin = new Padding(4);
            MaximizeBox = false;
            Name = "frmScanHost";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "frmScanHost";
            Load += frmScanHost_Load;
            ((System.ComponentModel.ISupportInitialize)numericUpDown1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button button1;
        private Label label1;
        private TextBox textBox1;
        private TextBox textBox2;
        private Label label2;
        private Label label3;
        private NumericUpDown numericUpDown1;
    }
}