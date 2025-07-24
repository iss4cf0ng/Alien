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

        public static string fnszGenerateRandomStr(int nLength = 10)
        {
            const string szPattern = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            StringBuilder sb = new StringBuilder(nLength);
            for (int i = 0; i < nLength; i++)
                sb.Append(szPattern[new Random().Next(0, szPattern.Length)]);

            return sb.ToString();
        }

        public static string fnszStre2b64(string szData) => Convert.ToBase64String(Encoding.UTF8.GetBytes(szData));
        public static string fnszB64d2str(string szData) => Encoding.UTF8.GetString(Convert.FromBase64String(szData));
        public static string fnszLs2b64str(List<string> lInput, string szSplitter = ",") => string.Join(szSplitter, lInput.Select(x => fnszStre2b64(x)));
        public static List<string> fnlsB64d2str(string szInput, string szSpliter = ",") => szInput.Split(szSpliter).Select(x => fnszB64d2str(x)).ToList();
    }
}
