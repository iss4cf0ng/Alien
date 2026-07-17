using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Security.Cryptography;
using System.Windows.Forms;

namespace Alien
{
    internal class clsCrypto
    {
        public clsCrypto()
        {

        }

        public static string fnGetMD5Last16(string szInput)
        {
            byte[] abInput = Encoding.UTF8.GetBytes(szInput);
            byte[] abHash = MD5.HashData(abInput);

            string szHash = Convert.ToHexString(abHash).ToLower();

            return szHash[^16..];
        }

        public static byte[] fnXorEncrypt(byte[] abBuffer, byte[] abKey)
        {
            byte[] abResult = new byte[abBuffer.Length];
            
            for (int i = 0; i < abBuffer.Length; i++)
            {
                int nIdx = (i + 1) & 15;
                abResult[i] = (byte)(abBuffer[i] ^ abKey[nIdx]);
            }

            return abResult;
        }

        public static byte[] fnAesEncrypt(byte[] abBuffer, byte[] abKey)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = abKey;
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.PKCS7;

                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                {
                    return encryptor.TransformFinalBlock(abBuffer, 0, abBuffer.Length);
                }
            }
        }

        public static byte[] fnAesDecrypt(byte[] abBuffer, byte[] abKey)
        {
            int nExcessBytes = abBuffer.Length % 16;
            byte[] abCleanBuffer = abBuffer;

            if (nExcessBytes != 0)
            {
                int nCleanLength = abBuffer.Length - nExcessBytes;
                abCleanBuffer = new byte[nCleanLength];
                Buffer.BlockCopy(abBuffer, 0, abCleanBuffer, 0, nCleanLength);
            }

            using (Aes aes = Aes.Create())
            {
                aes.Key = abKey;
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.None;

                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                {
                    byte[] abDecryptedRaw = decryptor.TransformFinalBlock(abCleanBuffer, 0, abCleanBuffer.Length);

                    if (abDecryptedRaw.Length == 0)
                        return abDecryptedRaw;

                    int nPaddingLength = abDecryptedRaw[abDecryptedRaw.Length - 1];

                    if (nPaddingLength >= 1 && nPaddingLength <= 16)
                    {
                        int nRealLength = abDecryptedRaw.Length - nPaddingLength;
                        byte[] abRealResult = new byte[nRealLength];
                        Buffer.BlockCopy(abDecryptedRaw, 0, abRealResult, 0, nRealLength);
                        return abRealResult;
                    }

                    return abDecryptedRaw;
                }
            }
        }

    }
}