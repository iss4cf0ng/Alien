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
            using (Aes aes = Aes.Create())
            {
                aes.Key = abKey;
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.PKCS7;

                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                {
                    return decryptor.TransformFinalBlock(abBuffer, 0, abBuffer.Length);
                }
            }
        }
    }
}