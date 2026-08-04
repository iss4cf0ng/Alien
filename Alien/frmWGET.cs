using Microsoft.VisualBasic;
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
    public partial class frmWGET : BaseForm
    {
        private clsfnFileMgr m_fileMgr { get; init; }
        private frmControlPanel m_frmCtrl { get; init; }
        private string m_szSaveDir { get; init; }

        private bool m_bRun = false;

        public frmWGET(clsfnFileMgr fileMgr, frmControlPanel frmCtrl, string szSaveDir)
        {
            InitializeComponent();

            m_fileMgr = fileMgr;
            m_frmCtrl = frmCtrl;
            m_szSaveDir = szSaveDir;
        }

        void fnSetup()
        {
            toolStripStatusLabel1.Text = "Ready";
            toolStripComboBox1.SelectedIndex = 2;
        }

        private void frmWGET_Load(object sender, EventArgs e)
        {
            fnSetup();
        }

        private async void toolStripButton1_Click(object sender, EventArgs e)
        {
            try
            {
                toolStripStatusLabel1.Text = "Loading...";

                int nThread = int.Parse(toolStripComboBox1.Text);
                if (nThread <= 0)
                    throw new Exception("Invalid number.");

                List<string> lsURL = textBox1.Text.Split(Environment.NewLine)
                    .Select(x => x.Trim().Replace(" ", string.Empty).Replace(Environment.NewLine, string.Empty))
                    .Where(x => !string.IsNullOrEmpty(x)).ToList();

                toolStripProgressBar1.Value = 0;
                toolStripProgressBar1.Maximum = lsURL.Count;

                var semaphore = new SemaphoreSlim(nThread);
                List<Task> lsTask = new List<Task>();

                m_bRun = true;

                foreach (string szURL in lsURL)
                {
                    if (!m_bRun)
                    {
                        toolStripProgressBar1.Value = toolStripProgressBar1.Maximum;
                        break;
                    }

                    lsTask.Add(Task.Run(async () =>
                    {
                        await semaphore.WaitAsync();

                        try
                        {
                            var wget = await m_fileMgr.fnbWGET(szURL, m_szSaveDir);

                            ListViewItem item = new ListViewItem(szURL);
                            item.SubItems.Add(wget.Filename);
                            item.SubItems.Add(wget.Success ? "OK" : wget.Message);

                            Invoke(() => listView1.Items.Add(item));
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        finally
                        {
                            semaphore.Release();
                            Invoke(() => toolStripProgressBar1.Increment(1));
                        }
                    }));
                }

                await Task.WhenAll(lsTask);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            m_frmCtrl.fnFileMgrRefresh();
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            m_bRun = false;
        }
    }
}
