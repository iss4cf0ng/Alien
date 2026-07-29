using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Alien.clsThemeManager;

namespace Alien
{
    public class BaseForm : Form
    {
        public BaseForm()
        {
            if (IsInDesignMode())
                return;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (IsInDesignMode())
                return;

            ThemeManager.ThemeChanged += ThemeChanged;
            ThemeManager.Apply(this);
        }

        private void ThemeChanged(object sender, EventArgs e)
        {
            if (IsDisposed)
                return;

            if (IsInDesignMode())
                return;

            ThemeManager.Apply(this);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ThemeManager.ThemeChanged -= ThemeChanged;
            }

            base.Dispose(disposing);
        }

        private static bool IsInDesignMode()
        {
            return LicenseManager.UsageMode == LicenseUsageMode.Designtime;
        }
    }
}
