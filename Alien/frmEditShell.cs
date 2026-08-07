using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Alien
{
    public partial class frmEditShell : BaseForm
    {
        public clsTamper m_tamper { get; init; }
        public clsSqlite m_sqlConn { get; init; }
        public stShellConfig m_stShellConfig { get; init; }
        private bool m_bNewShell { get; init; }
        private List<string> m_lsGroupName { get; init; }

        private Dictionary<string, enLanguage> m_dicLang = clsWeb.m_dicSuffix.ToDictionary(x => x.Value, x => x.Key);

        private string[] m_asEncoding =
        {
            "ASCII",
            "UTF-8",
            "Unicode",
            "Big5",                 //Traditional Chinese
            "Big5-HKSCS",
            "GB2312",               //Simplified Chinese
            "GBK",
            "GB18030",
            "ISO-8859-1",
            "Shift_JIS",            //Japanese
            "EUC-JP",               //Japanese
            "ISO-2022-JP",
            "EUC-KR",               //Korean
        };

        public frmEditShell(clsTamper tamper, clsSqlite sqlConn, stShellConfig config, bool bNewShell, List<string> lsGroupName)
        {
            InitializeComponent();

            m_tamper = tamper;
            m_sqlConn = sqlConn;
            m_stShellConfig = config;
            m_bNewShell = bNewShell;
            m_lsGroupName = lsGroupName;
        }

        void fnUpdateComets()
        {
            List<ListViewItem> items = listView1.Items.Cast<ListViewItem>().ToList();
            for (int i = 0; i < items.Count; i++)
                items[i].Text = (i + 1).ToString();

            listView1.Refresh();
        }

        void fnLoadComet(List<stShellConfig>? lsConfig = null)
        {
            listView1.Items.Clear();

            Task.Run(() =>
            {
                var configs = lsConfig ?? m_sqlConn.fnGetAllShellConfig();

                for (int i = 0; i < configs.Count; i++)
                {
                    var config = configs[i];
                    string? szScript = Enum.GetName(typeof(enLanguage), config.language);
                    if (string.IsNullOrEmpty(szScript) || config.payloadType != enPayloadType.OneShell || string.Equals(textBox1.Text, config.szUrl, StringComparison.CurrentCultureIgnoreCase))
                        continue;

                    clsVictim victim = new clsVictim(m_sqlConn, config, false);
                    clsWeb web = new clsWeb(victim, m_tamper, m_sqlConn);

                    ListViewItem item = new ListViewItem((i + 1).ToString());
                    item.SubItems.Add(config.szUrl);
                    item.SubItems.Add(szScript);
                    item.Tag = web;

                    Invoke(() => listView1.Items.Add(item));
                }
            });

        }

        void fnSetup()
        {
            //Validate
            if (m_sqlConn == null)
                throw new Exception("m_sqlConn is NULL.");

            //Controls init
            foreach (string szName in Enum.GetNames(typeof(enLanguage)))
                comboBox1.Items.Add(szName);

            if (comboBox1.Items.Count > 0)
                comboBox1.SelectedIndex = 0;

            foreach (string szEncoding in m_asEncoding)
                comboBox5.Items.Add(szEncoding);

            comboBox5.SelectedIndex = 0;

            comboBox7.SelectedIndex = 0;
            comboBox7.DropDownStyle = ComboBoxStyle.DropDownList;

            // Load groups

            var groups = new List<string>() { "_Orphan", };
            groups.AddRange(m_sqlConn.fnGetGroups());

            foreach (string szName in groups)
                comboBox6.Items.Add(szName);

            comboBox6.SelectedIndex = 0; // _Orphan

            string szTamperDirPath = Path.Combine(Application.StartupPath, "EventHorizon\\Obfuscators");
            if (Directory.Exists(szTamperDirPath))
            {
                comboBox2.DropDownStyle = ComboBoxStyle.DropDownList;
                textEditorControl1.Text = string.Empty;

                foreach (string szFilePath in Directory.GetFiles(szTamperDirPath))
                    comboBox2.Items.Add(Path.GetFileNameWithoutExtension(szFilePath));

                if (comboBox2.Items.Count > 0)
                    comboBox2.SelectedIndex = 0;
                else if (comboBox2.Items.Count == 0)
                    MessageBox.Show("Not tamper script exists!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                MessageBox.Show("Directory not found: " + szTamperDirPath, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            listView1.ListViewItemSorter = new ListViewNumericComparer(0);

            if (string.IsNullOrEmpty(m_stShellConfig.szUrl))
            {
                //Add shell

            }
            else
            {
                //Edit shell
                textBox1.Text = m_stShellConfig.szUrl;
                textBox2.Text = m_stShellConfig.szPassword;

                comboBox6.Text = m_stShellConfig.szGroupName;
                comboBox5.Text = m_stShellConfig.szEncoding;
                comboBox1.Text = m_stShellConfig.language.ToString();
                comboBox4.Text = m_stShellConfig.szMethod;
                comboBox3.Text = m_stShellConfig.payloadType.ToString();

                textBox4.Text = m_stShellConfig.szUserAgent;
                numericUpDown1.Value = m_stShellConfig.nTimeout;
                textBox6.Text = m_stShellConfig.szCookie;
                textBox5.Text = m_stShellConfig.szExtraPost;
                comboBox7.SelectedIndex = m_stShellConfig.nExtraPostPosition;

                checkBox1.Checked = m_stShellConfig.bEHEnable;
                textBox3.Text = m_stShellConfig.szDescription;
                textBox4.Text = m_stShellConfig.szUserAgent;
                comboBox2.Text = m_stShellConfig.szEventHorizonScript;
                textEditorControl1.Text = m_stShellConfig.szEventHorizonConfig;

                var lsComet = m_stShellConfig.lsCometShellID.Select(x => m_sqlConn.fnGetShellConfig(x)).ToList();
                fnLoadComet(lsComet);

                listView1.Sort();

                groupBox3.Enabled = checkBox1.Checked;
                textBox5.Enabled = !checkBox1.Checked && listView1.Items.Count == 0;
            }
        }

        private void frmEditShell_Load(object sender, EventArgs e)
        {
            fnSetup();
        }

        //Test
        private async void button1_Click(object sender, EventArgs e)
        {
            List<string> lsID = listView1.Items.Cast<ListViewItem>().Where(x => x.Tag != null).Select(x => (clsWeb)x.Tag).Select(x => x.m_victim.ShellID).ToList();

            if (!Uri.IsWellFormedUriString(textBox1.Text, UriKind.Absolute))
            {
                MessageBox.Show("Invalid URL: " + textBox1.Text, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            stShellConfig config = new stShellConfig()
            {
                szUrl = textBox1.Text,
                szPassword = textBox2.Text,
                szEncoding = comboBox5.Text,
                szMethod = comboBox4.Text,
                language = (enLanguage)Enum.Parse(typeof(enLanguage), comboBox1.Text),
                payloadType = (enPayloadType)Enum.Parse(typeof(enPayloadType), comboBox3.Text),

                szDescription = textBox3.Text,
                szUserAgent = textBox4.Text,
                bEHEnable = checkBox1.Checked,
                szEventHorizonScript = comboBox2.Text,
                szEventHorizonConfig = textEditorControl1.Text,

                szDriftingComet = JsonSerializer.Serialize(lsID),

                nTimeout = (int)numericUpDown1.Value,
                szCookie = textBox6.Text,
                szExtraPost = textBox5.Text,
                nExtraPostPosition = comboBox7.SelectedIndex,
            };

            clsVictim victim = new clsVictim(m_sqlConn, config, false);
            clsWeb web = new clsWeb(victim, m_tamper, m_sqlConn);

            string szPattern = clsEzData.fnszGenerateRandomStr();
            string szResp = await web.fnszSendPayload("test", new string[] { szPattern });

            if (string.Equals(szPattern, szResp))
                MessageBox.Show("Congrats! Webshell is valid", "OK!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            else
                MessageBox.Show("Oops! Webshell or the configuration is invalid...", "Oops!", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        //Save
        private void button2_Click(object sender, EventArgs e)
        {
            List<string> lsID = listView1.Items.Cast<ListViewItem>().Where(x => x.Tag != null).Select(x => (clsWeb)x.Tag).Select(x => x.m_victim.ShellID).ToList();

            if (!Uri.IsWellFormedUriString(textBox1.Text, UriKind.Absolute))
            {
                MessageBox.Show("Invalid URL: " + textBox1.Text, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            stShellConfig config = new stShellConfig();
            config.ID = m_bNewShell ? Guid.NewGuid().ToString() : m_stShellConfig.ID;
            config.szGroupName = comboBox6.Text;
            config.szUrl = textBox1.Text;
            config.szPassword = textBox2.Text;
            config.szEncoding = comboBox5.Text;
            config.language = (enLanguage)Enum.Parse(typeof(enLanguage), comboBox1.Text);
            config.szMethod = comboBox4.Text;
            config.payloadType = (enPayloadType)Enum.Parse(typeof(enPayloadType), comboBox3.Text);
            config.szDescription = textBox3.Text;
            config.szUserAgent = textBox4.Text;
            config.bEHEnable = checkBox1.Checked;
            config.szEventHorizonScript = comboBox2.Text;
            config.szEventHorizonConfig = textEditorControl1.Text;
            config.szDriftingComet = JsonSerializer.Serialize(lsID);
            config.nTimeout = (int)numericUpDown1.Value;
            config.szCookie = textBox6.Text;
            config.szExtraPost = textBox5.Text;
            config.nExtraPostPosition = comboBox7.SelectedIndex;

            if (m_sqlConn.SaveShell(config))
            {
                MessageBox.Show("Save webshell successfully.", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Close();
            }
            else
            {
                MessageBox.Show("Failed to save shell!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            groupBox3.Enabled = checkBox1.Checked;
            textBox5.Enabled = !checkBox1.Checked && listView1.Items.Count == 0;
        }

        private async void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboBox4.Items.Clear();
            string szDirPath = Path.Combine(new string[] { Application.StartupPath, "Payload", comboBox1.Text });
            if (!Directory.Exists(szDirPath))
                MessageBox.Show("Directory not found: " + szDirPath, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            foreach (string szName in Directory.GetDirectories(szDirPath))
                comboBox4.Items.Add(Path.GetFileName(szName));

            if (comboBox4.Items.Count > 0)
                comboBox4.SelectedIndex = 0;

            // EventHorizon
            string? szLang = comboBox1.Text;
            if (string.IsNullOrEmpty(szLang))
                return;

            var scripts = await m_tamper.fnGetAvailableTamper(szLang);
            if (scripts == null)
                return;

            string szOriginal = comboBox2.Text;

            comboBox2.Items.Clear();
            foreach (var script in scripts)
                comboBox2.Items.Add(script);

            if (comboBox2.Items.Count == 0)
                return;

            if (string.IsNullOrEmpty(szOriginal))
                comboBox2.SelectedIndex = 0;
            else if (scripts.Contains(szOriginal))
                comboBox2.Text = szOriginal;
        }

        private void comboBox4_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboBox3.Items.Clear();
            string szDirPath = Path.Combine(new string[] { Application.StartupPath, "Payload", comboBox1.Text, comboBox4.Text });
            if (!Directory.Exists(szDirPath))
                MessageBox.Show("Directory not found: " + szDirPath, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            foreach (string szName in Directory.GetDirectories(szDirPath))
                comboBox3.Items.Add(Path.GetFileName(szName));

            if (comboBox3.Items.Count > 0)
                comboBox3.SelectedIndex = 0;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            frmEditGroup f = new frmEditGroup(m_sqlConn);

            f.ShowDialog();

            comboBox6.Items.Clear();

            var groups = new List<string>() { "_Orphan", };
            groups.AddRange(m_sqlConn.fnGetGroups());

            foreach (string szName in groups)
                comboBox6.Items.Add(szName);

            comboBox6.Text = m_sqlConn.fnGetShellConfig(m_stShellConfig.ID).szGroupName;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBox1.Text))
                return;

            string szExtension = textBox1.Text.Split('?').First().Split('.').Last().ToLower();
            if (!m_dicLang.ContainsKey(szExtension))
                return;

            if (comboBox1.Text == Enum.GetName(typeof(enLanguage), m_dicLang[szExtension]))
                return;

            try
            {
                for (int i = 0; i < comboBox1.Items.Count; i++)
                {
                    string? szLang = Enum.GetName(typeof(enLanguage), m_dicLang[szExtension]);
                    if (string.IsNullOrEmpty(szLang))
                        continue;

                    if (string.Equals(comboBox1.Items[i]?.ToString(), szLang))
                    {
                        comboBox1.SelectedIndex = i;
                        break;
                    }
                }
            }
            catch
            {

            }
        }

        private async void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            string szScriptName = comboBox2.Text;
            if (string.IsNullOrEmpty(szScriptName))
                return;

            string? szJson = await m_tamper.fnGetExample(szScriptName);
            if (string.IsNullOrEmpty(szJson))
                return;

            textEditorControl1.Text = szJson;
            textEditorControl1.Refresh();

            string? szHelp = await m_tamper.fnGetHelp(szScriptName);
            if (string.IsNullOrEmpty(szHelp))
                return;

            richTextBox1.Text = szHelp;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            MessageBox.Show("The User-Agent will be randomly selected if it is empty.", "What is this?", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void listView1_DoubleClick(object sender, EventArgs e)
        {
            ListViewItem? item = listView1.SelectedItems.Cast<ListViewItem>().FirstOrDefault();

            if (item == null)
                return;

            List<ListViewItem> otherItems = listView1.Items.Cast<ListViewItem>()
                .Where(x => x != item)
                .OrderBy(x => int.Parse(x.Text))
                .ToList();

            string input = Interaction.InputBox("New Order", "Please enter an integer: ");
            if (!int.TryParse(input, out int k) || k < 1)
            {
                MessageBox.Show("Invalid integer: " + input);
                return;
            }

            int insertIndex = Math.Clamp(k - 1, 0, otherItems.Count);
            otherItems.Insert(insertIndex, item);

            Dictionary<ListViewItem, int> dicOrder = new Dictionary<ListViewItem, int>();

            listView1.BeginUpdate();
            try
            {
                for (int i = 0; i < otherItems.Count; i++)
                {
                    int newOrder = i + 1;
                    ListViewItem currentItem = otherItems[i];

                    currentItem.Text = newOrder.ToString();

                    dicOrder.Add(currentItem, newOrder);
                }

            }
            finally
            {
                listView1.EndUpdate();
                listView1.Sort();
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            fnLoadComet();
        }

        private async void button6_Click(object sender, EventArgs e)
        {
            int nThread = int.Parse(Interaction.InputBox("Thread count:", "Check Alive", "3"));
            if (nThread <= 0)
                throw new Exception("Invalid number.");

            Dictionary<clsWeb, ListViewItem> dic = new Dictionary<clsWeb, ListViewItem>();
            foreach (ListViewItem item in listView1.Items)
            {
                if (item.Tag == null)
                    continue;

                dic.Add((clsWeb)item.Tag, item);
            }

            progressBar1.Value = 0;
            progressBar1.Maximum = dic.Count;

            var semaphore = new SemaphoreSlim(nThread);
            List<Task> lsTask = new List<Task>();

            foreach (clsWeb web in dic.Keys)
            {
                lsTask.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync();

                    try
                    {
                        bool bAlive = await web.fnbTestWebConnection(false) && await web.fnbTestShellConnection(false);
                        Invoke(() =>
                        {
                            if (!bAlive)
                                dic.Remove(web);
                        });
                    }
                    finally
                    {
                        semaphore.Release();

                        Invoke(() => progressBar1.Increment(1));
                    }
                }));
            }

            await Task.WhenAll(lsTask);

            fnLoadComet(dic.Keys.Select(x => x.m_victim.m_ShellConfig).ToList());

            MessageBox.Show("Completed, please check.", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void listView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Modifiers == Keys.Control)
            {
                foreach (ListViewItem item in listView1.Items)
                    item.Selected = true;
            }
            else
            {
                if (e.KeyCode == Keys.Delete)
                {
                    foreach (ListViewItem item in listView1.SelectedItems)
                        listView1.Items.Remove(item);

                    listView1.Refresh();

                    fnUpdateComets();
                }
            }
        }

        public class ListViewNumericComparer : IComparer
        {
            private int _column;

            public ListViewNumericComparer(int column)
            {
                _column = column;
            }

            public int Compare(object x, object y)
            {
                ListViewItem itemX = x as ListViewItem;
                ListViewItem itemY = y as ListViewItem;

                if (itemX == null || itemY == null)
                    return 0;

                if (double.TryParse(itemX.SubItems[_column].Text, out double numX) &&
                    double.TryParse(itemY.SubItems[_column].Text, out double numY))
                {
                    return numX.CompareTo(numY);
                }

                return string.Compare(itemX.SubItems[_column].Text, itemY.SubItems[_column].Text);
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            List<string> lsID = listView1.Items.Cast<ListViewItem>().Where(x => x.Tag != null).Select(x => (clsWeb)x.Tag).Select(x => x.m_victim.ShellID).ToList();
            stShellConfig config = new stShellConfig()
            {
                szUrl = textBox1.Text,
                szPassword = textBox2.Text,
                szEncoding = comboBox5.Text,
                szMethod = comboBox4.Text,
                language = (enLanguage)Enum.Parse(typeof(enLanguage), comboBox1.Text),
                payloadType = (enPayloadType)Enum.Parse(typeof(enPayloadType), comboBox3.Text),

                szDescription = textBox3.Text,
                szUserAgent = textBox4.Text,
                bEHEnable = checkBox1.Checked,
                szEventHorizonScript = comboBox2.Text,
                szEventHorizonConfig = textEditorControl1.Text,

                szDriftingComet = JsonSerializer.Serialize(lsID),

                nTimeout = (int)numericUpDown1.Value,
                szCookie = textBox6.Text,
                szExtraPost = textBox5.Text,
                nExtraPostPosition = comboBox7.SelectedIndex,
            };

            clsVictim victim = new clsVictim(m_sqlConn, config, false);

            frmCometDiagram f = new frmCometDiagram(m_sqlConn, victim);
            f.Show();
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool bOneShell = comboBox3.Text.Equals("OneShell") || comboBox3.Text.Equals("DarkMatter");
            groupBox2.Enabled = bOneShell;
            groupBox5.Enabled = bOneShell;
        }
    }
}
