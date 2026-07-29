using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Alien
{
    public partial class frmEnvChecker : BaseForm
    {
        /// <summary>
        /// Check prerequiste programs and environment variables
        /// </summary>

        private clsTamper m_tamper { get; init; }
        private clsIniManager m_iniMgr { get; init; }

        public frmEnvChecker(clsTamper tamper, clsIniManager iniMgr)
        {
            InitializeComponent();

            Text = "Checking Your Environment...";
            m_tamper = tamper;
            m_iniMgr = iniMgr;
        }

        void fnAddLog(string szMsg)
        {
            Invoke(() =>
            {
                richTextBox1.AppendText(szMsg);
                richTextBox1.AppendText(Environment.NewLine);
            });
        }

        bool fnbCheckEnvVariables()
        {
            try
            {
                string szExec = m_iniMgr.ReadString("General", "Python");

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = szExec,
                        Arguments = "--version",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                if (process.ExitCode == 0)
                    return true;
                else
                    throw new Exception("Cannot fine Python in your machine! Please install Python or check the environment variables.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                fnAddLog(ex.Message);
            }

            return false;
        }

        bool fnbCheckPayloads()
        {
            if (!Directory.Exists("Payload"))
                return false;



            return true;
        }

        private async Task fnSetup()
        {
            progressBar1.Maximum = 3;

            fnAddLog("Checking environment variables...");

            bool envOK = await Task.Run(() => fnbCheckEnvVariables());

            if (!envOK)
            {
                DialogResult = DialogResult.No;
                await Task.Delay(3000);
                return;
            }

            fnAddLog("=> OK");
            progressBar1.Increment(1);

            fnAddLog("Checking payloads...");

            bool payloadOK = await Task.Run(() => fnbCheckPayloads());

            if (!payloadOK)
            {
                DialogResult = DialogResult.No;
                await Task.Delay(3000);
                return;
            }

            fnAddLog("=> OK");
            progressBar1.Increment(1);

            fnAddLog("Language => " + m_iniMgr.ReadString("General", "Language"));

            fnAddLog("Starting the tamper script server...");

            await m_tamper.fnInitializeServerAsync();

            fnAddLog("=> OK");
            progressBar1.Increment(1);

            fnAddLog("==========[ NICE! ]==========");
            fnAddLog("Starting the application, please wait...");

            await Task.Delay(3000);

            DialogResult = DialogResult.OK;

            return;
        }

        private async void frmEnvChecker_Load(object sender, EventArgs e)
        {
            await fnSetup();
        }
    }
}
