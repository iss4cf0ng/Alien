using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;

namespace Alien
{
    public partial class frmEncoder : BaseForm
    {
        public frmEncoder()
        {
            InitializeComponent();

            Text = "Encoder";
        }

        public string fnComputeHash(string text, HashAlgorithm algorithm)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            byte[] hash = algorithm.ComputeHash(bytes);

            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        private string CRC32(string input)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input);

            uint crc = 0xffffffff;

            foreach (byte b in bytes)
            {
                crc ^= b;

                for (int i = 0; i < 8; i++)
                {
                    if ((crc & 1) != 0)
                        crc = (crc >> 1) ^ 0xEDB88320;
                    else
                        crc >>= 1;
                }
            }

            crc ^= 0xffffffff;

            return crc.ToString("X8");
        }

        private string fnSHA224(string text)
        {
            using (var sha = SHA256.Create())
            {
                byte[] hash =
                    sha.ComputeHash(
                        Encoding.UTF8.GetBytes(text));

                return BitConverter
                    .ToString(hash, 0, 28)
                    .Replace("-", "")
                    .ToLower();
            }
        }

        void fnSetup()
        {
            textBox1.PlaceholderText = "Plain text";

            textBox2.PlaceholderText = "Encode this text";
            textBox3.PlaceholderText = "Decode this text";
            textBox4.PlaceholderText = "Encode this text";
            textBox5.PlaceholderText = "Decode this text";
        }

        private void frmEncoder_Load(object sender, EventArgs e)
        {
            fnSetup();
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            try
            {
                textBox3.Text = Convert.ToBase64String(Encoding.UTF8.GetBytes(textBox2.Text));
            }
            catch (Exception ex)
            {
                textBox3.Text = ex.Message;
            }
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            try
            {
                textBox2.Text = Encoding.UTF8.GetString(Convert.FromBase64String(textBox3.Text));
            }
            catch (Exception ex)
            {
                textBox2.Text = ex.Message;
            }
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            try
            {
                textBox5.Text = HttpUtility.UrlEncode(textBox4.Text);
            }
            catch (Exception ex)
            {
                textBox5.Text = ex.Message;
            }
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            try
            {
                textBox4.Text = HttpUtility.UrlDecode(textBox5.Text);
            }
            catch (Exception ex)
            {
                textBox4.Text = ex.Message;
            }
        }

        private async void textBox1_TextChanged(object sender, EventArgs e)
        {
            try
            {
                string text = textBox1.Text;

                if (string.IsNullOrEmpty(text))
                {
                    listView1.Items.Clear();
                    return;
                }

                var dicValue = await Task.Run(() =>
                {
                    Dictionary<string, string> dic = new Dictionary<string, string>();

                    dic.Add("MD5", fnComputeHash(text, MD5.Create()));
                    dic.Add("SHA1", fnComputeHash(text, SHA1.Create()));
                    dic.Add("SHA224", fnSHA224(text));

                    dic.Add("SHA256", fnComputeHash(text, SHA256.Create()));
                    dic.Add("SHA384", fnComputeHash(text, SHA384.Create()));
                    dic.Add("SHA512", fnComputeHash(text, SHA512.Create()));

                    dic.Add("CRC32", CRC32(text));

                    return dic;
                });


                listView1.Items.Clear();

                foreach (var item in dicValue)
                {
                    ListViewItem lvi = new ListViewItem(item.Key);
                    lvi.SubItems.Add(item.Value);

                    listView1.Items.Add(lvi);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            try
            {
                textBox7.Text = Convert.ToHexString(Encoding.UTF8.GetBytes(textBox6.Text));
            }
            catch (Exception ex)
            {
                textBox7.Text = ex.Message;
            }
        }

        private void toolStripButton6_Click(object sender, EventArgs e)
        {
            try
            {
                textBox6.Text = Encoding.UTF8.GetString(Convert.FromHexString(textBox7.Text));
            }
            catch (Exception ex)
            {
                textBox6.Text = ex.Message;
            }
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            foreach (ListViewItem item in listView1.SelectedItems)
            {
                if (!string.IsNullOrEmpty(sb.ToString()))
                    sb.Append("\n");

                sb.Append(item.SubItems[1].Text);
            }

            Clipboard.SetText(sb.ToString());
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            listView1.Items.Clear();
        }
    }
}
