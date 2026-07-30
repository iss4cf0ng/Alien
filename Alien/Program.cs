using System.Globalization;
using System.Threading;
using static Alien.clsThemeManager;

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
            CultureInfo? culture = null;
            try
            {
                culture = new CultureInfo(new clsIniManager("config.ini").ReadString("General", "Language", "en"));
            }
            catch
            {
                culture = new CultureInfo("en");
            }

            CultureInfo.DefaultThreadCurrentUICulture = culture;

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            try
            {
                ThemeManager.SetTheme(new clsIniManager("config.ini").ReadString("General", "Theme", "Default"));
            }
            catch (Exception ex)
            {
                ThemeManager.SetTheme("Default");
            }

            Application.Run(new frmMain());
        }
    }
}