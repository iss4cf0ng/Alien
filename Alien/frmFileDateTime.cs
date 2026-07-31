using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Alien
{
    public partial class frmFileDateTime : BaseForm
    {
        private clsfnFileMgr m_fileMgr { get; init; }
        private string m_szFilePath { get; init; }
        private DateTime m_dtOriginal { get; init; }

        public frmFileDateTime(clsfnFileMgr fileMgr, string szFilePath, DateTime dtOriginal)
        {
            InitializeComponent();

            m_fileMgr = fileMgr;
            m_szFilePath = szFilePath;

            Text = "Last Modified & Accessed";
            m_dtOriginal = dtOriginal;
        }

        void fnSetup()
        {
            dateTimePicker1.Format = DateTimePickerFormat.Custom;
            dateTimePicker1.CustomFormat = "yyyy-MM-dd HH:mm:ss";

            dateTimePicker1.Value = m_dtOriginal;
        }

        private void frmFileDateTime_Load(object sender, EventArgs e)
        {
            fnSetup();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            try
            {
                long unixTimestamp = long.Parse(textBox1.Text);
                await m_fileMgr.fnSetTimestamp(m_szFilePath, unixTimestamp);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            MessageBox.Show("Set timestamp successfully.", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
        }

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            DateTime dt = dateTimePicker1.Value;
            long unixTimestamp = ((DateTimeOffset)dt).ToUnixTimeSeconds();
            string szDt = unixTimestamp.ToString();

            textBox1.Text = szDt;
        }
    }
}
