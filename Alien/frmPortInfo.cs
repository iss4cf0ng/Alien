using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Alien
{
    public partial class frmPortInfo : Form
    {
        private string m_szIP { get; init; }
        private List<int> m_lnPort { get; init; }

        static readonly Dictionary<int, string> m_dicPortService = new()
        {
            { 20, "FTP-Data" },
            { 21, "FTP" },
            { 22, "SSH" },
            { 23, "Telnet" },
            { 25, "SMTP" },
            { 53, "DNS" },
            { 80, "HTTP" },
            { 110, "POP3" },
            { 143, "IMAP" },
            { 443, "HTTPS" },
            { 445, "SMB" },
            { 3306, "MySQL" },
            { 3389, "RDP" },
            { 5432, "PostgreSQL" },
            { 6379, "Redis" },
            { 8080, "HTTP-Alt" }
        };

        public frmPortInfo(string szIP, List<int> lnPort)
        {
            InitializeComponent();

            m_szIP = szIP;
            m_lnPort = lnPort;
            Text = szIP;
        }

        void fnSetup()
        {
            foreach (int nPort in m_lnPort)
            {
                ListViewItem item = new ListViewItem(nPort.ToString());
                item.SubItems.Add(m_dicPortService.ContainsKey(nPort) ? m_dicPortService[nPort] : "Unknown");

                listView1.Items.Add(item);
            }    
        }

        private void frmPortInfo_Load(object sender, EventArgs e)
        {
            fnSetup();
        }
    }
}
