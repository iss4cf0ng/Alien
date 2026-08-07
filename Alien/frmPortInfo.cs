using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Alien
{
    public partial class frmPortInfo : BaseForm
    {
        private string m_szIP { get; init; }
        private List<int> m_lnPort { get; init; }
        private clsIniManager m_iniMgr { get; init; }
        private Dictionary<int, string> m_dicPortService = new();

        public frmPortInfo(string szIP, List<int> lnPort)
        {
            InitializeComponent();

            m_szIP = szIP;
            m_lnPort = lnPort;
            Text = szIP;

            m_iniMgr = new clsIniManager("config.ini");
        }

        /// <summary>
        /// Load JSON into the dictionary.
        /// </summary>
        void fnLoadJson()
        {
            string szJsonPath = m_iniMgr.ReadString("General", "Ports");
            if (string.IsNullOrEmpty(szJsonPath))
            {
                MessageBox.Show("JSON file not set.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!File.Exists(szJsonPath))
            {
                MessageBox.Show("JSON file not found: " + szJsonPath, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string szJson = File.ReadAllText(szJsonPath);
            var json = JsonSerializer.Deserialize<Dictionary<int, string>>(szJson);
            if (json == null)
            {
                MessageBox.Show("JSON deserialization failed", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            m_dicPortService = json;
        }

        void fnSetup()
        {
            fnLoadJson();
            
            ListViewColumnSorter lvwSorter = new ListViewColumnSorter();
            listView1.ListViewItemSorter = lvwSorter;

            int nIdx = 0;
            ListViewHeaderChanger.SortOrder defaultOrder = ListViewHeaderChanger.SortOrder.Ascending;

            lvwSorter.SortColumn = nIdx;
            lvwSorter.Order = defaultOrder == ListViewHeaderChanger.SortOrder.Ascending ? SortOrder.Ascending : SortOrder.Descending;

            foreach (int nPort in m_lnPort)
            {
                ListViewItem item = new ListViewItem(nPort.ToString());
                item.SubItems.Add(m_dicPortService.ContainsKey(nPort) ? m_dicPortService[nPort] : "Unknown");

                listView1.Items.Add(item);
            }

            listView1.Sort();
            listView1.SetSortArrow(nIdx, defaultOrder);
        }

        private void frmPortInfo_Load(object sender, EventArgs e)
        {
            fnSetup();
        }

        public class ListViewColumnSorter : IComparer
        {
            public int SortColumn { get; set; } = 0;
            public SortOrder Order { get; set; } = SortOrder.None;

            public int Compare(object x, object y)
            {
                ListViewItem itemX = (ListViewItem)x;
                ListViewItem itemY = (ListViewItem)y;

                string textX = itemX.SubItems.Count > SortColumn ? itemX.SubItems[SortColumn].Text : "";
                string textY = itemY.SubItems.Count > SortColumn ? itemY.SubItems[SortColumn].Text : "";

                int compareResult;

                if (DateTime.TryParse(textX, out DateTime dateX) && DateTime.TryParse(textY, out DateTime dateY))
                {
                    compareResult = DateTime.Compare(dateX, dateY);
                }
                else
                {
                    compareResult = string.Compare(textX, textY, StringComparison.OrdinalIgnoreCase);
                }

                if (Order == SortOrder.Ascending)
                    return compareResult;
                else if (Order == SortOrder.Descending)
                    return -compareResult;
                else
                    return 0;
            }
        }
    }
}
