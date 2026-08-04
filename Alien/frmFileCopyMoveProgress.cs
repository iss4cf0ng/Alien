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
    public partial class frmFileCopyMoveProgress : BaseForm
    {
        private int m_nThread { get; set; } = 10;
        private clsfnFileMgr m_fileMgr { get; init; }
        private string m_szDirName { get; init; }

        private bool m_bRunning { get; set; } = false;

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
                    if (!m_bRunning)
                        break;

                    lsTask.Add(Task.Run(async () =>
                    {
                        await semaphore.WaitAsync();

                        try
                        {
                            string szNewPath = Path.Combine(m_szDirName, Path.GetFileName(entry.szEntryPath)).Replace("\\", "/");

                            Invoke(() => $"{entry.szEntryPath} => {szNewPath}");
                            if (m_fileMgr.m_moveClipboard)
                                await m_fileMgr.fnbMove(entry.szEntryPath, szNewPath);
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

            Invoke(() =>
            {
                label1.Text = m_bRunning ? "Done." : "Interrupted.";
                Close();
            });
        }

        void fnSetup()
        {
            label1.Text = "Loading...";

            progressBar1.Value = 0;
            progressBar1.Maximum = m_fileMgr.m_dirClipboard.Count + m_fileMgr.m_fileClipboard.Count;

            m_bRunning = true;

            Task.Run(() =>
            {
                Thread.Sleep(1000);
                fnStart();
            });
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
