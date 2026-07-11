using System.Globalization;
using System.Threading;

namespace Alien
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var culture = new CultureInfo(new clsIniManager("config.ini").ReadString("General", "Language", "en"));

            CultureInfo.DefaultThreadCurrentUICulture = culture;

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new frmMain());
        }
    }
}