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
        /// Generate random string with specified length.
        /// </summary>
        /// <param name="nLength">Length of random string.</param>
        /// <returns>Random string.</returns>
        public static string fnszGenerateRandomStr(int nLength = 10)
        {
            const string szPattern = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            StringBuilder sb = new StringBuilder(nLength);
            for (int i = 0; i < nLength; i++)
                sb.Append(new Random(szPattern.Length).Next());

            return sb.ToString();
        }
    }
}
