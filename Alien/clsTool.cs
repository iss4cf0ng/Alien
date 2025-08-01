using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alien
{
    public class clsTool
    {
        public clsTool() { }

        public static List<ListViewItem> fnExtractListViewSelectedItems(ListView lv) => lv.SelectedItems.Cast<ListViewItem>().ToList();
        
        /// <summary>
        /// Normalize bytes length.
        /// </summary>
        /// <param name="nSize">Bytes length</param>
        /// <returns>String value of normalized bytes length.</returns>
        public static string fnSizeNormalization(long nSize)
        {
            if (nSize < 0)
                return "[INVALID_SIZE]";

            string[] asSize = { "B", "KB", "MB", "GB", "TB" };
            double dSize = nSize;
            int nIndex = 0;

            while (dSize >= 1024 && nIndex < asSize.Length - 1)
            {
                dSize /= 1024;
                nIndex++;
            }

            return string.Format("{0:0.##} {1}", dSize, asSize[nIndex]);
        }

        /// <summary>
        /// Convert base64 string into Image object.
        /// </summary>
        /// <param name="szB64str">Image base64 string value.</param>
        /// <returns>Image object.</returns>
        public static Image fnimgB64strToImage(string szB64str)
        {
            byte[] abBuffer = Convert.FromBase64String(szB64str);
            using (MemoryStream ms = new MemoryStream(abBuffer))
            {
                return Image.FromStream(ms);
            }
        }
    }
}
