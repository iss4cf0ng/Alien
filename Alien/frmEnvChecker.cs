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
    public partial class frmEnvChecker : Form
    {
        /// <summary>
        /// Check prerequiste programs and environment variables
        /// </summary>

        public frmEnvChecker()
        {
            InitializeComponent();

            Text = "Checking Your Environment...";
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
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "python",
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


            return true;
        }

        private async Task fnSetup()
        {
            fnAddLog("Checking environment variables...");

            bool envOK = await Task.Run(() => fnbCheckEnvVariables());

            if (!envOK)
            {
                DialogResult = DialogResult.No;
                await Task.Delay(3000);
                return;
            }

            fnAddLog("=> OK");

            fnAddLog("Checking payloads...");

            bool payloadOK = await Task.Run(() => fnbCheckPayloads());

            if (!payloadOK)
            {
                DialogResult = DialogResult.No;
                await Task.Delay(3000);
                return;
            }

            fnAddLog("=> OK");
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
