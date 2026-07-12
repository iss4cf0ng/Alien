using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alien
{
    internal class clsEzData
    {
        public clsEzData()
        {

        }

        private static readonly Random _random = new Random();
        private static readonly string[] _osPlatforms = new[]
        {
            "Windows NT 10.0; Win64; x64",
            "Windows NT 10.0; WOW64",
            "Macintosh; Intel Mac OS X 10_15_7",
            "Macintosh; Intel Mac OS X 11_12_0"
        };

        public static string fnszGenerateRandomStr(int nLength = 10, bool bStartsWithLetter = false)
        {
            if (nLength <= 0)
                return string.Empty;

            const string letters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string chars = letters + "0123456789";

            StringBuilder sb = new StringBuilder(nLength);

            if (bStartsWithLetter)
                sb.Append(letters[_random.Next(letters.Length)]);

            int startIndex = sb.Length;

            for (int i = startIndex; i < nLength; i++)
                sb.Append(chars[_random.Next(chars.Length)]);

            return sb.ToString();
        }

        public static string fnszStre2b64(string szData) => Convert.ToBase64String(Encoding.UTF8.GetBytes(szData));
        public static string fnszB64d2str(string szData) { try { return Encoding.UTF8.GetString(Convert.FromBase64String(szData)); } catch { return string.Empty; } }
        public static string fnszLs2b64str(List<string> lInput, string szSplitter = ",") => string.Join(szSplitter, lInput.Select(x => fnszStre2b64(x)));
        public static List<string> fnlsB64d2str(string szInput, string szSpliter = ",") => szInput.Split(szSpliter).Select(x => fnszB64d2str(x)).ToList();
    
        public static Image fnResizeImage(Image image, int nWidth, int nHeight)
        {
            Bitmap bmp = new Bitmap(nWidth, nHeight);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                g.DrawImage(image, 0, 0, nWidth, nHeight);
            }

            return bmp;
        }

        public static string fnRandomUserAgent()
        {
            string szOS = _osPlatforms[_random.Next(_osPlatforms.Length)];

            int nMainVersion = _random.Next(130, 146);
            int nBuildVersion = _random.Next(1000, 7000);
            int nPatchVersion = _random.Next(1, 200);

            int nBrowserType = _random.Next(0, 4);
            switch (nBrowserType)
            {
                case 0: // Chrome
                    return $"Mozilla/5.0 ({szOS}) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{nMainVersion}.0.{nBuildVersion}.{nPatchVersion} Safari/537.36";

                case 1: // Edge
                    return $"Mozilla/5.0 ({szOS}) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{nMainVersion}.0.{nBuildVersion}.{nPatchVersion} Safari/537.36 Edg/{nMainVersion}.0.{nBuildVersion}.{nPatchVersion}";

                case 2: // Firefox
                    int ffVersion = _random.Next(125, 140);
                    string ffOs = szOS.Contains("Windows") ? "Windows NT 10.0; Win64; x64" : szOS;
                    return $"Mozilla/5.0 ({ffOs}; rv:{ffVersion}.0) Gecko/20100101 Firefox/{ffVersion}.0";

                case 3: // Safari (Mac OS/IOS)
                    string safariOs = szOS.Contains("Macintosh") ? szOS : "Macintosh; Intel Mac OS X 10_15_7";
                    return $"Mozilla/5.0 ({safariOs}) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/{_random.Next(16, 19)}.{_random.Next(0, 5)} Safari/605.1.15";

                default:
                    return "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/135.0.0.0 Safari/537.36";
            }
        }
    }
}
