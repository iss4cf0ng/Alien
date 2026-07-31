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
    public partial class frmCometDiagram : BaseForm
    {
        private clsSqlite m_sqlConn { get; init; }
        private clsVictim m_victim { get; init; }

        public frmCometDiagram(clsSqlite sqlConn, clsVictim victim)
        {
            InitializeComponent();

            m_sqlConn = sqlConn;
            m_victim = victim;

            Text = "Comets";
        }

        async void fnSetup()
        {
            try
            {
                textBox1.ReadOnly = true;
                textBox1.Text = m_victim.ShellURL;

                TopologyGraph graph = new TopologyGraph();

                var configs = m_victim.m_ShellConfig.lsCometShellID.Select(x => m_sqlConn.fnGetShellConfig(x)).ToList();
                configs.Add(m_victim.m_ShellConfig);

                // Nodes

                graph.nodes.Add(new TopologyNode
                {
                    id = "USER",
                    name = "You",
                    type = "malware",
                });

                foreach (var config in configs)
                {
                    graph.nodes.Add(new TopologyNode
                    {
                        id = config.szUrl.ToString(),
                        name = config.szUrl.Split('/')[2] + $"({config.language.ToString()})",
                        type = "host"
                    });
                }

                // Chain edge
                graph.edges.Add(new TopologyEdge
                {
                    from = graph.nodes[0].id,
                    to = configs[0].szUrl.ToString(),
                    label = configs[0].payloadType.ToString() + (configs[0].bEHEnable ? $"({configs[0].szEventHorizonScript})" : string.Empty)
                });

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
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
