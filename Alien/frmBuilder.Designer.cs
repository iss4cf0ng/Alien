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
            textBox1 = new TextBox();
            textEditorControl1 = new ICSharpCode.TextEditor.TextEditorControl();
            button1 = new Button();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(99, 65);
            label1.Margin = new Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new Size(51, 19);
            label1.TabIndex = 0;
            label1.Text = "label1";
            // 
            // comboBox1
            // 
            comboBox1.FormattingEnabled = true;
            comboBox1.Location = new Point(240, 61);
            comboBox1.Margin = new Padding(4, 4, 4, 4);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new Size(154, 27);
            comboBox1.TabIndex = 1;
            // 
            // textBox1
            // 
            textBox1.Location = new Point(240, 96);
            textBox1.Margin = new Padding(4, 4, 4, 4);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(127, 27);
            textBox1.TabIndex = 2;
            // 
            // textEditorControl1
            // 
            textEditorControl1.Font = new Font("Courier New", 10F);
            textEditorControl1.IsReadOnly = false;
            textEditorControl1.Location = new Point(114, 131);
            textEditorControl1.Margin = new Padding(4, 4, 4, 4);
            textEditorControl1.Name = "textEditorControl1";
            textEditorControl1.Size = new Size(531, 215);
            textEditorControl1.TabIndex = 3;
            textEditorControl1.Text = "textEditorControl1";
            // 
            // button1
            // 
            button1.Location = new Point(114, 354);
            button1.Margin = new Padding(4, 4, 4, 4);
            button1.Name = "button1";
            button1.Size = new Size(316, 71);
            button1.TabIndex = 4;
            button1.Text = "button1";
            button1.UseVisualStyleBackColor = true;
            // 
            // frmBuilder
            // 
            AutoScaleDimensions = new SizeF(9F, 19F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1029, 570);
            Controls.Add(button1);
            Controls.Add(textEditorControl1);
            Controls.Add(textBox1);
            Controls.Add(comboBox1);
            Controls.Add(label1);
            Font = new Font("Microsoft JhengHei UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 136);
            Margin = new Padding(4, 4, 4, 4);
            Name = "frmBuilder";
            Text = "frmBuilder";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private ComboBox comboBox1;
        private TextBox textBox1;
        private ICSharpCode.TextEditor.TextEditorControl textEditorControl1;
        private Button button1;
    }
}