using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Alien
{
    public partial class frmCometDiagram : Form
    {
        private clsSqlite m_sqlConn { get; init; }
        private clsVictim m_victim { get; init; }

        public frmCometDiagram(clsSqlite sqlConn, clsVictim victim)
        {
            InitializeComponent();

            m_sqlConn = sqlConn;
            m_victim = victim;
        }

        async void fnSetup()
        {
            TopologyGraph graph = new TopologyGraph();

            var configs = m_victim.m_ShellConfig.lsCometShellID
                .Select(x => m_sqlConn.fnGetShellConfig(x))
                .ToList();

            configs.Add(m_victim.m_ShellConfig);

            // Nodes
            foreach (var config in configs)
            {
                graph.nodes.Add(new TopologyNode
                {
                    id = config.szUrl.ToString(),
                    name = config.szUrl.Split('/')[2],
                    type = "host"
                });
            }

            // Chain edge
            for (int i = 0; i < configs.Count - 1; i++)
            {
                graph.edges.Add(new TopologyEdge
                {
                    from = configs[i].szUrl.ToString(),
                    to = configs[i + 1].szUrl.ToString(),
                    label = configs[i + 1].payloadType.ToString() + (configs[i + 1].bEHEnable ? $"({configs[i + 1].szEventHorizonScript})" : string.Empty)
                });
            }

            string json = JsonSerializer.Serialize(graph, new JsonSerializerOptions { WriteIndented = true });

            await webView21.EnsureCoreWebView2Async();

            webView21.CoreWebView2.WebMessageReceived += (sender, e) =>
            {
                string data = e.TryGetWebMessageAsString();
                MessageBox.Show(data);
            };

            webView21.NavigationCompleted += async (s, e) =>
            {
                await webView21.ExecuteScriptAsync($"drawGraph({json})");
            };

            string szPath = Path.Combine(Application.StartupPath, "Tools", "topo", "graph.html");

            webView21.Source = new Uri(szPath);
        }

        private void frmCometDiagram_Load(object sender, EventArgs e)
        {
            fnSetup();
        }

        public class TopologyGraph
        {
            public List<TopologyNode> nodes { get; set; } = new();
            public List<TopologyEdge> edges { get; set; } = new();
        }

        public class TopologyNode
        {
            public string id { get; set; }
            public string name { get; set; }
            public string type { get; set; }
        }
        public class TopologyEdge
        {
            public string from { get; set; }
            public string to { get; set; }
            public string label { get; set; }
        }
    }
}
