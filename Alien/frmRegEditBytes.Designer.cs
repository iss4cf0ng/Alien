namespace Alien
{
    partial class frmRegEditBytes
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
            label2 = new Label();
            panel1 = new Panel();
            button1 = new Button();
            button2 = new Button();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 9);
            label1.Name = "label1";
            label1.Size = new Size(58, 19);
            label1.TabIndex = 0;
            label1.Text = "Name :";
            // 
            // textBox1
            // 
            textBox1.Location = new Point(12, 31);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(626, 27);
            textBox1.TabIndex = 1;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 77);
            label2.Name = "label2";
            label2.Size = new Size(48, 19);
            label2.TabIndex = 2;
            label2.Text = "Data :";
            // 
            // panel1
            // 
            panel1.Location = new Point(12, 99);
            panel1.Name = "panel1";
            panel1.Size = new Size(626, 227);
            panel1.TabIndex = 3;
            // 
            // button1
            // 
            button1.Location = new Point(268, 332);
            button1.Name = "button1";
            button1.Size = new Size(182, 43);
            button1.TabIndex = 4;
            button1.Text = "Save";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // button2
            // 
            button2.Location = new Point(456, 332);
            button2.Name = "button2";
            button2.Size = new Size(182, 43);
            button2.TabIndex = 5;
            button2.Text = "Cancel";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // frmRegEditBytes
            // 
            AutoScaleDimensions = new SizeF(9F, 19F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(650, 387);
            Controls.Add(button2);
            Controls.Add(button1);
            Controls.Add(panel1);
            Controls.Add(label2);
            Controls.Add(textBox1);
            Controls.Add(label1);
            Font = new Font("Microsoft JhengHei UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 136);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Margin = new Padding(4);
            MaximizeBox = false;
            Name = "frmRegEditBytes";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "frmRegEditBytes";
            Load += frmRegEditBytes_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private TextBox textBox1;
        private Label label2;
        private Panel panel1;
        private Button button1;
        private Button button2;
    }
}