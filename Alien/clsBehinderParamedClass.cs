using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alien
{
    public class clsBehinderParamedClass
    {
        public clsBehinderParamedClass()
        {

        }

        public static byte[] fnGetParamedClass(string szClassPath, string szPlaceHolder, string szNewValue)
        {
            byte[] abClass = File.ReadAllBytes(szClassPath);
            byte[] abPlaceHolder = Encoding.UTF8.GetBytes(szPlaceHolder);
            byte[] abNewValue = Encoding.UTF8.GetBytes(szNewValue);

            byte[] abSearchTarget = new byte[abPlaceHolder.Length + 2];
            abSearchTarget[0] = (byte)((abPlaceHolder.Length >> 8) & 0xFF);
            abSearchTarget[1] = (byte)(abPlaceHolder.Length & 0xFF);
            Buffer.BlockCopy(abPlaceHolder, 0, abSearchTarget, 2, abPlaceHolder.Length);

            int nIndex = fnFindBytesPattern(abClass, abSearchTarget);
            if (nIndex == -1)
                throw new Exception("Placeholder not found in Bytecode.");

            byte[] abNewLengthHeader = new byte[2];
            abNewLengthHeader[0] = (byte)((abNewValue.Length >> 8) & 0xFF);
            abNewLengthHeader[1] = (byte)(abNewValue.Length & 0xFF);

            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(abClass, 0, nIndex);
                ms.Write(abNewValue, 0, 2);
                ms.Write(abNewValue, 0, abNewValue.Length);

                int nIdx = nIndex + abSearchTarget.Length;
                ms.Write(abClass, nIdx, abClass.Length - nIdx);

                byte[] abResult = ms.ToArray();

                abResult[7] = 50;

                return abResult;
            }
        }

        private static int fnFindBytesPattern(byte[] abSource, byte[] abPattern)
        {
            for (int i = 0; i <= abSource.Length - abPattern.Length; i++)
            {
                bool bMatch = true;
                for (int j = 0; j < abPattern.Length; j++)
                {
                    if (abSource[i + j] != abPattern[j])
                    {
                        bMatch = false;
                        break;
                    }

                    if (bMatch)
                        return i;
                }
            }

            return -1;
        }
    }
}
