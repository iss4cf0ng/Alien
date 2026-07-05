using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Alien
{
    public partial class frmFileCopyMoveProgress : Form
    {
        private int m_nThread { get; set; } = 10;
        private clsfnFileMgr m_fileMgr { get; init; }
        private string m_szDirName { get; init; }

        public frmFileCopyMoveProgress(clsfnFileMgr fileMgr, string szDirName)
        {
            InitializeComponent();

            m_fileMgr = fileMgr;
            m_szDirName = szDirName;
        }

        async void fnStart()
        {
            try
            {
                var semaphore = new SemaphoreSlim(m_nThread);
                List<Task> lsTask = new List<Task>();

                var lsEntry = m_fileMgr.m_dirClipboard.Concat(m_fileMgr.m_fileClipboard);
                foreach (var entry in lsEntry)
                {
                    lsTask.Add(Task.Run(async () =>
                    {
                        await semaphore.WaitAsync();

                        try
                        {
                            string szNewPath = Path.Combine(m_szDirName, Path.GetFileName(entry.szEntryPath));

                            Invoke(() => $"{entry.szEntryPath} => {szNewPath}");
                            if (m_fileMgr.m_moveClipboard)
                                await m_fileMgr.fnbMove(entry.szEntryName, szNewPath);
                            else
                                await m_fileMgr.fnbCopy(entry.szEntryPath, szNewPath);
                        }
                        finally
                        {
                            semaphore.Release();

                            Invoke(() =>
                            {
                                progressBar1.Increment(1);
                            });
                        }
                    }));
                }

                await Task.WhenAll(lsTask);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            Invoke(() => label1.Text = "Done.");
        }

        void fnSetup()
        {
            label1.Text = "Loading...";

            progressBar1.Value = 0;
            progressBar1.Maximum = m_fileMgr.m_dirClipboard.Count + m_fileMgr.m_fileClipboard.Count;

            Thread.Sleep(2000);

            Task.Run(() => fnStart());
        }

        private void frmFileCopyMoveProgress_Load(object sender, EventArgs e)
        {
            fnSetup();
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }
    }
}
