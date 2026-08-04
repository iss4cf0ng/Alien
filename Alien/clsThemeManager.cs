using ICSharpCode.TextEditor.Document;
using ICSharpCode.TextEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Alien
{
    internal class clsThemeManager
    {
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

        /// <summary>
        /// 
        /// </summary>
        public class AppTheme
        {
            public bool IsDefault { get; set; } = false;

            public Color FormBackColor { get; set; }
            public Color ControlBackColor { get; set; }
            public Color ForeColor { get; set; }
            public Color BorderColor { get; set; }
            public Color AccentColor { get; set; }
            public Color ButtonBackColor { get; set; }
            public Color ButtonForeColor { get; set; }
            public Color ButtonDangerColor { get; set; }
            public Color ButtonSuccessColor { get; set; }
            public Color TextBoxBackColor { get; set; }
            public Color SelectionBackColor { get; set; }
            public Color SelectionForeColor { get; set; }
            public Color GridColor { get; set; }
            public Color HeaderBackColor { get; set; }
            public Color HeaderForeColor { get; set; }
            public Color DisabledBackColor { get; set; }
            public Color DisabledForeColor { get; set; }

            public bool UseCustomDraw { get; set; }
        }

        public class ThemeColorTable : ProfessionalColorTable
        {
            public override Color ToolStripDropDownBackground => ThemeManager.Current.ControlBackColor;
            public override Color ImageMarginGradientBegin => ThemeManager.Current.ControlBackColor;
            public override Color ImageMarginGradientMiddle => ThemeManager.Current.ControlBackColor;
            public override Color ImageMarginGradientEnd => ThemeManager.Current.ControlBackColor;
            public override Color ImageMarginRevealedGradientBegin => ThemeManager.Current.ControlBackColor;
            public override Color ImageMarginRevealedGradientMiddle => ThemeManager.Current.ControlBackColor;
            public override Color ImageMarginRevealedGradientEnd => ThemeManager.Current.ControlBackColor;
            public override Color MenuItemSelected => ThemeManager.Current.SelectionBackColor;
            public override Color MenuItemSelectedGradientBegin => ThemeManager.Current.SelectionBackColor;
            public override Color MenuItemSelectedGradientEnd => ThemeManager.Current.SelectionBackColor;
            public override Color MenuItemPressedGradientBegin => ThemeManager.Current.SelectionBackColor;
            public override Color MenuItemPressedGradientMiddle => ThemeManager.Current.SelectionBackColor;
            public override Color MenuItemPressedGradientEnd => ThemeManager.Current.SelectionBackColor;
            public override Color MenuBorder => ThemeManager.Current.BorderColor;
            public override Color SeparatorDark => ThemeManager.Current.BorderColor;
            public override Color SeparatorLight => ThemeManager.Current.BorderColor;
            public override Color ToolStripBorder => ThemeManager.Current.BorderColor;
            public override Color ToolStripGradientBegin => ThemeManager.Current.ControlBackColor;
            public override Color ToolStripGradientMiddle => ThemeManager.Current.ControlBackColor;
            public override Color ToolStripGradientEnd => ThemeManager.Current.ControlBackColor;
        }

        public class clsThemeItem
        {
            public string Name { get; set; } = "";
            public AppTheme Theme { get; set; } = default;
        }

        public static class ThemeItemManager
        {
            public static List<clsThemeItem> _Themes { get; } =
            [
                new()
                {
                    Name = "Default",
                    Theme = Themes.Default,
                },
                new()
                {
                    Name = "Light",
                    Theme = Themes.Light,
                },
                new()
                {
                    Name = "Gray",
                    Theme = Themes.Gray,
                },
                new()
                {
                    Name = "Dark",
                    Theme = Themes.Dark,
                },
                new()
                {
                    Name = "Uplink",
                    Theme = Themes.Uplink,
                },
                new()
                {
                    Name = "DarkLime",
                    Theme = Themes.DarkLime,
                },
                new()
                {
                    Name = "DarkYellow",
                    Theme = Themes.DarkYellow,
                },
            ];
        }

        /// <summary>
        /// 
        /// </summary>
        public static class Themes
        {
            public static readonly AppTheme Default = new AppTheme
            {
                IsDefault = true,

                FormBackColor = SystemColors.Control,
                ControlBackColor = SystemColors.Control,
                ForeColor = SystemColors.ControlText,
                BorderColor = SystemColors.ActiveBorder,
                AccentColor = SystemColors.Highlight,
                ButtonBackColor = SystemColors.Control,
                ButtonForeColor = SystemColors.ControlText,
                TextBoxBackColor = SystemColors.Window,
                SelectionBackColor = SystemColors.Highlight,
                SelectionForeColor = SystemColors.HighlightText,
                HeaderBackColor = SystemColors.Control,
                HeaderForeColor = SystemColors.ControlText,
                DisabledBackColor = SystemColors.Control,
                DisabledForeColor = SystemColors.GrayText,

                UseCustomDraw = false
            };

            public static readonly AppTheme Gray = new AppTheme
            {
                FormBackColor = Color.FromArgb(37, 37, 38),
                ControlBackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                BorderColor = Color.FromArgb(70, 70, 70),
                AccentColor = Color.DeepSkyBlue,
                ButtonBackColor = Color.FromArgb(0, 122, 204),
                ButtonForeColor = Color.White,
                ButtonDangerColor = Color.Firebrick,
                ButtonSuccessColor = Color.SeaGreen,
                TextBoxBackColor = Color.FromArgb(30, 30, 30),
                SelectionBackColor = Color.FromArgb(0, 122, 204),
                SelectionForeColor = Color.White,
                GridColor = Color.FromArgb(70, 70, 70),
                HeaderBackColor = Color.FromArgb(60, 60, 60),
                HeaderForeColor = Color.White,
                DisabledBackColor = Color.FromArgb(80, 80, 80),
                DisabledForeColor = Color.Gray,

                UseCustomDraw = true
            };


            public static readonly AppTheme Dark = new AppTheme
            {
                FormBackColor = Color.FromArgb(18, 18, 18),
                ControlBackColor = Color.FromArgb(25, 25, 25),
                ForeColor = Color.FromArgb(230, 230, 230),
                BorderColor = Color.FromArgb(60, 60, 60),
                AccentColor = Color.DeepSkyBlue,
                ButtonBackColor = Color.FromArgb(45, 45, 45),
                ButtonForeColor = Color.White,
                ButtonDangerColor = Color.DarkRed,
                ButtonSuccessColor = Color.DarkGreen,
                TextBoxBackColor = Color.FromArgb(20, 20, 20),
                SelectionBackColor = Color.FromArgb(0, 90, 160),
                SelectionForeColor = Color.White,
                GridColor = Color.FromArgb(50, 50, 50),
                HeaderBackColor = Color.FromArgb(35, 35, 35),
                HeaderForeColor = Color.White,
                DisabledBackColor = Color.FromArgb(45, 45, 45),
                DisabledForeColor = Color.Gray,

                UseCustomDraw = true,
            };

            /// <summary>
            /// 
            /// </summary>
            public static readonly AppTheme Light = new AppTheme
            {
                FormBackColor = Color.White,
                ControlBackColor = Color.White,
                ForeColor = Color.Black,
                BorderColor = Color.Silver,
                AccentColor = Color.DodgerBlue,
                ButtonBackColor = Color.DodgerBlue,
                ButtonForeColor = Color.White,
                ButtonDangerColor = Color.Red,
                ButtonSuccessColor = Color.Green,
                TextBoxBackColor = Color.White,
                SelectionBackColor = Color.DodgerBlue,
                SelectionForeColor = Color.White,
                GridColor = Color.Gainsboro,
                HeaderBackColor = Color.Gainsboro,
                HeaderForeColor = Color.Black,
                DisabledBackColor = Color.LightGray,
                DisabledForeColor = Color.Gray,

                UseCustomDraw = true,
            };

            /// <summary>
            /// 
            /// </summary>
            public static readonly AppTheme DarkLime = new AppTheme
            {
                FormBackColor = Color.Black,
                ControlBackColor = Color.FromArgb(10, 10, 10),
                ForeColor = Color.Lime,
                BorderColor = Color.FromArgb(0, 120, 0),
                AccentColor = Color.Lime,
                ButtonBackColor = Color.FromArgb(20, 60, 20),
                ButtonForeColor = Color.Lime,
                ButtonDangerColor = Color.DarkRed,
                ButtonSuccessColor = Color.Green,
                TextBoxBackColor = Color.Black,
                SelectionBackColor = Color.FromArgb(0, 80, 0),
                SelectionForeColor = Color.Lime,
                GridColor = Color.FromArgb(0, 80, 0),
                HeaderBackColor = Color.FromArgb(15, 15, 15),
                HeaderForeColor = Color.Lime,
                DisabledBackColor = Color.FromArgb(30, 30, 30),
                DisabledForeColor = Color.DarkGreen,

                UseCustomDraw = true,
            };

            public static readonly AppTheme DarkYellow = new AppTheme
            {
                FormBackColor = Color.Black,
                ControlBackColor = Color.FromArgb(15, 15, 15),
                ForeColor = Color.Gold,
                BorderColor = Color.FromArgb(100, 80, 0),
                AccentColor = Color.Yellow,
                ButtonBackColor = Color.FromArgb(60, 50, 0),
                ButtonForeColor = Color.Yellow,
                ButtonDangerColor = Color.DarkRed,
                ButtonSuccessColor = Color.DarkGreen,
                TextBoxBackColor = Color.Black,
                SelectionBackColor = Color.FromArgb(100, 80, 0),
                SelectionForeColor = Color.Yellow,
                GridColor = Color.FromArgb(80, 60, 0),
                HeaderBackColor = Color.FromArgb(30, 25, 0),
                HeaderForeColor = Color.Yellow,
                DisabledBackColor = Color.FromArgb(40, 40, 40),
                DisabledForeColor = Color.DarkGoldenrod,

                UseCustomDraw = true,
            };

            public static readonly AppTheme Uplink = new AppTheme
            {
                FormBackColor = Color.Black,
                ControlBackColor = Color.FromArgb(10, 10, 10),
                ForeColor = Color.FromArgb(150, 210, 255),
                BorderColor = Color.FromArgb(0, 80, 150),
                AccentColor = Color.Cyan,
                ButtonBackColor = Color.FromArgb(0, 30, 70),
                ButtonForeColor = Color.Cyan,
                ButtonDangerColor = Color.DarkRed,
                ButtonSuccessColor = Color.Teal,
                TextBoxBackColor = Color.Black,
                SelectionBackColor = Color.FromArgb(0, 80, 140),
                SelectionForeColor = Color.White,
                GridColor = Color.FromArgb(0, 50, 100),
                HeaderBackColor = Color.FromArgb(5, 15, 30),
                HeaderForeColor = Color.Cyan,
                DisabledBackColor = Color.FromArgb(25, 25, 25),
                DisabledForeColor = Color.Gray,

                UseCustomDraw = true,
            };
        }

        internal class TabZeroHook : NativeWindow
        {
            private readonly TabControl _tab;
            private const int WM_ERASEBKGND = 0x0014;

            public TabZeroHook(TabControl tab)
            {
                _tab = tab;
                AssignHandle(tab.Handle);
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_ERASEBKGND && _tab.TabCount == 0)
                {
                    try
                    {
                        using (Graphics g = Graphics.FromHdc(m.WParam))
                        {
                            using (Brush b = new SolidBrush(ThemeManager.Current.ControlBackColor))
                            {
                                g.FillRectangle(b, _tab.ClientRectangle);
                            }
                        }
                        m.Result = (IntPtr)1;
                        return;
                    }
                    catch
                    {

                    }
                }

                base.WndProc(ref m);
            }
        }

        private class TabControlEraseHook : NativeWindow
        {
            private readonly TabControl _tab;
            private const int WM_ERASEBKGND = 0x0014;

            public TabControlEraseHook(TabControl tab)
            {
                _tab = tab;
                AssignHandle(tab.Handle);
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_ERASEBKGND && _tab.DrawMode == TabDrawMode.OwnerDrawFixed)
                {
                    try
                    {
                        using (Graphics g = Graphics.FromHdc(m.WParam))
                        {
                            using (Brush b = new SolidBrush(ThemeManager.Current.ControlBackColor))
                            {
                                if (_tab.TabCount == 0)
                                {
                                    g.FillRectangle(b, _tab.ClientRectangle);
                                    m.Result = (IntPtr)1;
                                    return;
                                }

                                int headerHeight = _tab.DisplayRectangle.Top;
                                if (headerHeight > 0)
                                {
                                    Rectangle headerRect = new Rectangle(0, 0, _tab.Width, headerHeight);
                                    g.FillRectangle(b, headerRect);
                                }

                                Rectangle lastTab = _tab.GetTabRect(_tab.TabCount - 1);
                                if (lastTab.Right < _tab.Width)
                                {
                                    Rectangle leftover = new Rectangle(
                                        lastTab.Right,
                                        lastTab.Top,
                                        _tab.Width - lastTab.Right,
                                        lastTab.Height);
                                    g.FillRectangle(b, leftover);
                                }
                            }
                        }
                        m.Result = (IntPtr)1;
                        return;
                    }
                    catch
                    {
                        
                    }
                }

                base.WndProc(ref m);
            }
        }

        public static class ThemeManager
        {
            public static AppTheme Current { get; private set; } = Themes.Default;
            private static readonly Dictionary<Control, (Color Back, Color Fore)> OriginalColors = new();
            private static readonly Dictionary<TabControl, TabControlEraseHook> EraseHooks = new();

            public static event EventHandler? ThemeChanged;

            public static string CurrentName { get; private set; } = "Default";

            public static void SetTheme(AppTheme theme)
            {
                if (theme == null)
                    return;

                Current = theme;
                ThemeChanged?.Invoke(
                    null,
                    EventArgs.Empty
                );

                foreach (Form form in Application.OpenForms)
                {
                    Apply(form);
                }
            }

            public static void SetTheme(string name)
            {
                var item = ThemeItemManager._Themes.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

                if (item == null)
                {
                    SetTheme(Themes.Default);
                    CurrentName = "Default";
                    return;
                }

                CurrentName = item.Name;

                SetTheme(item.Theme);
            }

            public static void Apply(Control root)
            {
                if (root == null)
                    return;

                root.SuspendLayout();

                ApplyRecursive(root);

                root.ResumeLayout(true);
                root.Invalidate();
            }

            public static void ApplyRange(Control.ControlCollection controls)
            {
                foreach (var ctrl in controls)
                    Apply((Control)ctrl);
            }
            public static void ApplyRange(Control[] controls)
            {
                foreach (var ctrl in controls)
                    Apply(ctrl);
            }

            private static void SaveOriginalColor(Control c)
            {
                if (OriginalColors.ContainsKey(c))
                    return;

                OriginalColors[c] = (c.BackColor, c.ForeColor);

                c.Disposed += (_, __) => OriginalColors.Remove(c);
            }

            private static void SetBackColorIfUnmodified(Control c, Color themedColor)
            {
                SaveOriginalColor(c);

                if (c.BackColor == OriginalColors[c].Back)
                    c.BackColor = themedColor;
            }

            private static void SetForeColorIfUnmodified(Control c, Color themedColor)
            {
                SaveOriginalColor(c);

                if (c.ForeColor == OriginalColors[c].Fore)
                    c.ForeColor = themedColor;
            }

            private static void RestoreDefault(Control c)
            {
                if (OriginalColors.ContainsKey(c))
                {
                    c.BackColor = OriginalColors[c].Back;
                    c.ForeColor = OriginalColors[c].Fore;
                }

                if (c is SplitContainer sc)
                {
                    RestoreSplitContainerDefault(sc);
                }

                if (c is ToolStrip ts)
                {
                    RestoreToolStripDefault(ts);
                }

                if (c is ListView lv)
                {
                    lv.OwnerDraw = false;
                    lv.DrawItem -= ListView_DrawItem;
                    lv.DrawSubItem -= ListView_DrawSubItem;
                    lv.DrawColumnHeader -= ListView_DrawColumnHeader;
                }

                if (c is TabControl tabc)
                {
                    RestoreTabControlDefault(tabc);
                }

                foreach (Control child in c.Controls)
                {
                    RestoreDefault(child);
                }
            }

            private static void RestoreSplitContainerDefault(SplitContainer sc)
            {
                sc.BackColor = SystemColors.Control;

                sc.Panel1.BackColor = SystemColors.Control;
                sc.Panel1.ForeColor = SystemColors.ControlText;

                sc.Panel2.BackColor = SystemColors.Control;
                sc.Panel2.ForeColor = SystemColors.ControlText;

                sc.SplitterWidth = 4;
            }

            private static void RestoreToolStripDefault(ToolStrip strip)
            {
                strip.BackColor = SystemColors.Control;
                strip.ForeColor = SystemColors.ControlText;

                strip.RenderMode = ToolStripRenderMode.System;

                foreach (ToolStripItem item in strip.Items)
                {
                    RestoreToolStripItemDefault(item);
                }
            }

            private static void RestoreTabControlDefault(TabControl tab)
            {
                tab.DrawMode = TabDrawMode.Normal;

                tab.DrawItem -= TabControl_DrawItem;
                tab.Paint -= TabControl_Paint;
            }


            private static void RestoreToolStripItemDefault(ToolStripItem item)
            {
                item.BackColor = Color.Empty;
                item.ForeColor = Color.Empty;

                if (item is ToolStripDropDownItem dropDown)
                {
                    dropDown.DropDown.BackColor = SystemColors.Control;
                    dropDown.DropDown.ForeColor = SystemColors.ControlText;

                    foreach (ToolStripItem child in dropDown.DropDownItems)
                    {
                        RestoreToolStripItemDefault(child);
                    }
                }
            }

            private static void ApplyRecursive(Control control)
            {
                if (Current.IsDefault)
                {
                    RestoreDefault(control);
                    return;
                }

                SaveOriginalColor(control);

                if (control.Tag != null &&
                    control.Tag.ToString() == "NoTheme")
                    return;

                if (control.ContextMenuStrip != null)
                {
                    ApplyContextMenuStrip(control.ContextMenuStrip);
                }

                switch (control)
                {
                    case WebBrowser wb:
                        ApplyWebBrowser(wb);
                        break;
                    case TabControl tc:
                        ApplyTabControl(tc);
                        break;
                    case BaseForm f:
                        ApplyBaseForm(f);
                        break;
                    case Form f:
                        ApplyForm(f);
                        break;
                    case Button b:
                        ApplyButton(b);
                        break;
                    case Label l:
                        ApplyLabel(l);
                        break;
                    case TextBox tb:
                        ApplyTextBox(tb);
                        break;
                    case TabPage tp:
                        ApplyTabPage(tp);
                        break;
                    case RichTextBox rtb:
                        ApplyRichTextBox(rtb);
                        break;
                    case ComboBox cb:
                        ApplyComboBox(cb);
                        break;
                    case CheckBox chk:
                        ApplyCheckBox(chk);
                        break;
                    case RadioButton rb:
                        ApplyRadioButton(rb);
                        break;
                    case CheckedListBox clb:
                        ApplyCheckedListBox(clb);
                        break;
                    case ListBox lb:
                        ApplyListBox(lb);
                        break;
                    case ListView lv:
                        ApplyListView(lv);
                        break;
                    case TreeView tv:
                        ApplyTreeView(tv);
                        break;
                    case Panel p:
                        ApplyPanel(p);
                        break;
                    case GroupBox gb:
                        ApplyGroupBox(gb);
                        break;
                    case DataGridView dgv:
                        ApplyDataGridView(dgv);
                        break;
                    case NumericUpDown nud:
                        ApplyNumericUpDown(nud);
                        break;
                    case DateTimePicker dt:
                        ApplyDateTimePicker(dt);
                        break;
                    case ProgressBar pb:
                        ApplyProgressBar(pb);
                        break;
                    case MenuStrip ms:
                        ApplyMenuStrip(ms);
                        break;
                    case StatusStrip ss:
                        ApplyStatusStrip(ss);
                        break;
                    case ToolStrip ts:
                        ApplyToolStrip(ts);
                        break;
                    default:
                        ApplyDefault(control);
                        break;
                    case SplitContainer sc:
                        ApplySplitContainer(sc);
                        break;
                    case TextEditorControl editor:
                        ApplyTextEditor(editor);
                        break;
                }

                foreach (Control child in control.Controls)
                {
                    ApplyRecursive(child);
                }

                if (control is SplitContainer split)
                {
                    ApplyRecursive(split.Panel1);
                    ApplyRecursive(split.Panel2);
                }
            }

            private static void ApplyDefault(Control c)
            {
                SetBackColorIfUnmodified(c, Current.ControlBackColor);
                SetForeColorIfUnmodified(c, Current.ForeColor);
            }

            private static void ApplyForm(Form f)
            {
                f.BackColor = Current.FormBackColor;
                f.ForeColor = Current.ForeColor;
            }

            private static void ApplyBaseForm(BaseForm f)
            {
                f.BackColor = Current.FormBackColor;
                f.ForeColor = Current.ForeColor;
            }

            private static void ApplyPanel(Panel p)
            {
                SetBackColorIfUnmodified(p, Current.ControlBackColor);
                SetForeColorIfUnmodified(p, Current.ForeColor);
            }

            private static void ApplyGroupBox(GroupBox gb)
            {
                SetBackColorIfUnmodified(gb, Current.ControlBackColor);
                SetForeColorIfUnmodified(gb, Current.ForeColor);
            }

            private static void ApplyLabel(Label l)
            {
                SaveOriginalColor(l);

                if (l.BackColor == OriginalColors[l].Back)
                    l.BackColor = Color.Transparent;

                SetForeColorIfUnmodified(l, Current.ForeColor);
            }

            private static void ApplyButton(Button b)
            {
                b.FlatStyle = FlatStyle.Flat;
                b.FlatAppearance.BorderSize = 0;

                SaveOriginalColor(b);

                if (b.Tag != null)
                {
                    switch (b.Tag.ToString())
                    {
                        case "Danger":
                            b.BackColor = Current.ButtonDangerColor;
                            break;

                        case "Success":
                            b.BackColor = Current.ButtonSuccessColor;
                            break;

                        default:
                            if (b.BackColor == OriginalColors[b].Back)
                                b.BackColor = Current.ButtonBackColor;
                            break;
                    }
                }
                else
                {
                    if (b.BackColor == OriginalColors[b].Back)
                        b.BackColor = Current.ButtonBackColor;
                }

                SetForeColorIfUnmodified(b, Current.ButtonForeColor);

                b.Cursor = Cursors.Hand;
            }

            private static void ApplySplitContainer(SplitContainer sc)
            {
                sc.BackColor = Current.BorderColor;
                sc.Panel1.BackColor = Current.ControlBackColor;
                sc.Panel1.ForeColor = Current.ForeColor;
                sc.Panel2.BackColor = Current.ControlBackColor;
                sc.Panel2.ForeColor = Current.ForeColor;

                sc.SplitterWidth = 1;
            }

            private static void ApplyTextEditor(TextEditorControl editor)
            {
                var strategy = editor.Document.HighlightingStrategy as DefaultHighlightingStrategy;
                if (strategy == null)
                    return;

                var bgCol = new HighlightColor(Current.ForeColor, Current.TextBoxBackColor, false, false);

                strategy.SetColorFor("Default", bgCol);
                strategy.SetColorFor("Digits", bgCol);
                strategy.SetColorFor("LineNumbers", bgCol);
            }

            private static void ApplyTextBox(TextBox tb)
            {
                SetBackColorIfUnmodified(tb, Current.TextBoxBackColor);
                SetForeColorIfUnmodified(tb, Current.ForeColor);

                tb.BorderStyle = BorderStyle.FixedSingle;
            }

            private static void ApplyRichTextBox(RichTextBox tb)
            {
                if (Current.IsDefault)
                    return;

                SetBackColorIfUnmodified(tb, Current.TextBoxBackColor);
                SetForeColorIfUnmodified(tb, Current.ForeColor);
            }

            private static void ApplyComboBox(ComboBox cb)
            {
                SetBackColorIfUnmodified(cb, Current.ControlBackColor);
                SetForeColorIfUnmodified(cb, Current.ForeColor);

                cb.FlatStyle = FlatStyle.Flat;
            }

            private static void ApplyCheckBox(CheckBox c)
            {
                SaveOriginalColor(c);

                if (c.BackColor == OriginalColors[c].Back)
                    c.BackColor = Color.Transparent;

                SetForeColorIfUnmodified(c, Current.ForeColor);
            }

            private static void ApplyRadioButton(RadioButton r)
            {
                SaveOriginalColor(r);

                if (r.BackColor == OriginalColors[r].Back)
                    r.BackColor = Color.Transparent;

                SetForeColorIfUnmodified(r, Current.ForeColor);
            }

            private static void ApplyListBox(ListBox lb)
            {
                SetBackColorIfUnmodified(lb, Current.ControlBackColor);
                SetForeColorIfUnmodified(lb, Current.ForeColor);

                lb.BorderStyle = BorderStyle.FixedSingle;
            }

            private static void ApplyCheckedListBox(CheckedListBox clb)
            {
                SetBackColorIfUnmodified(clb, Current.ControlBackColor);
                SetForeColorIfUnmodified(clb, Current.ForeColor);

                clb.BorderStyle = BorderStyle.FixedSingle;
            }

            private static void ApplyNumericUpDown(NumericUpDown n)
            {
                SetBackColorIfUnmodified(n, Current.ControlBackColor);
                SetForeColorIfUnmodified(n, Current.ForeColor);
            }

            private static void ApplyDateTimePicker(DateTimePicker d)
            {
                d.CalendarForeColor = Current.ForeColor;
                d.CalendarMonthBackground = Current.ControlBackColor;
            }

            private static void ApplyProgressBar(ProgressBar p)
            {
                SetBackColorIfUnmodified(p, Current.ControlBackColor);
            }

            private static void ApplyListView(ListView lv)
            {
                SetBackColorIfUnmodified(lv, Current.ControlBackColor);
                SetForeColorIfUnmodified(lv, Current.ForeColor);

                lv.HideSelection = false;

                if (Current.UseCustomDraw &&
                    lv.View == View.Details)
                {
                    lv.OwnerDraw = true;

                    SetupListViewDraw(lv);
                }
                else
                {
                    lv.OwnerDraw = false;

                    lv.DrawColumnHeader -= ListView_DrawColumnHeader;
                    lv.DrawItem -= ListView_DrawItem;
                    lv.DrawSubItem -= ListView_DrawSubItem;
                }

                foreach (ListViewItem item in lv.Items)
                {
                    if (item.BackColor == Color.Empty ||
                       item.BackColor == lv.BackColor)
                    {
                        item.BackColor =
                            lv.BackColor;
                    }


                    if (item.ForeColor == Color.Empty ||
                       item.ForeColor == lv.ForeColor)
                    {
                        item.ForeColor =
                            lv.ForeColor;
                    }
                }
            }

            private static void ListView_LargeIcon_DrawItem(object sender, DrawListViewItemEventArgs e)
            {
                ListView lv = (ListView)sender;

                Color back = e.Item.Selected ? Current.SelectionBackColor : Current.ControlBackColor;
                Color fore = e.Item.Selected ? Current.SelectionForeColor : Current.ForeColor;

                using Brush b = new SolidBrush(back);

                e.Graphics.FillRectangle(b, e.Bounds);

                if (e.Item.ImageIndex >= 0 && lv.LargeImageList != null)
                {
                    Image img = lv.LargeImageList.Images[e.Item.ImageIndex];
                    Rectangle imgRect = new Rectangle(e.Bounds.Left + (e.Bounds.Width - img.Width) / 2, e.Bounds.Top + 5, img.Width, img.Height);

                    e.Graphics.DrawImage(img, imgRect);
                }

                TextRenderer.DrawText(
                    e.Graphics,
                    e.Item.Text,
                    lv.Font,
                    new Rectangle(e.Bounds.Left, e.Bounds.Bottom - 25, e.Bounds.Width, 25),
                    fore,
                    TextFormatFlags.HorizontalCenter |
                    TextFormatFlags.VerticalCenter
                );
            }

            private static void SetupListViewDraw(ListView lv)
            {
                lv.OwnerDraw = true;

                lv.DrawColumnHeader -= ListView_DrawColumnHeader;
                lv.DrawColumnHeader += ListView_DrawColumnHeader;

                lv.DrawItem -= ListView_DrawItem;
                lv.DrawItem += ListView_DrawItem;

                lv.DrawSubItem -= ListView_DrawSubItem;
                lv.DrawSubItem += ListView_DrawSubItem;
            }

            private static void ApplyWebBrowser(WebBrowser wb)
            {
                
            }

            private static void ApplyTabControl(TabControl tab)
            {
                SetBackColorIfUnmodified(tab, Current.ControlBackColor);
                SetForeColorIfUnmodified(tab, Current.ForeColor);

                foreach (TabPage page in tab.TabPages)
                {
                    page.BackColor = Current.ControlBackColor;
                    page.ForeColor = Current.ForeColor;
                    ApplyTabPage(page);
                }

                if (!Current.UseCustomDraw)
                {
                    tab.DrawMode = TabDrawMode.Normal;
                    tab.DrawItem -= TabControl_DrawItem;

                    if (EraseHooks.ContainsKey(tab))
                    {
                        EraseHooks[tab].ReleaseHandle();
                        EraseHooks.Remove(tab);
                    }
                    return;
                }

                tab.DrawMode = TabDrawMode.OwnerDrawFixed;

                tab.DrawItem -= TabControl_DrawItem;
                tab.DrawItem += TabControl_DrawItem;

                if (!EraseHooks.ContainsKey(tab))
                {
                    EraseHooks[tab] = new TabControlEraseHook(tab);
                }
            }

            private static void TabControl_Paint(object sender, PaintEventArgs e)
            {
                TabControl tab = (TabControl)sender;

                if (tab.TabCount == 0)
                {
                    using Brush empty = new SolidBrush(Current.ControlBackColor);
                    e.Graphics.FillRectangle(empty, tab.ClientRectangle);
                    return;
                }

                Rectangle lastTabRect = tab.GetTabRect(tab.TabCount - 1);

                Rectangle leftover = new Rectangle(
                    lastTabRect.Right,
                    0,
                    Math.Max(tab.Width - lastTabRect.Right, 0),
                    lastTabRect.Bottom + 4);

                if (leftover.Width <= 0)
                    return;

                using Brush fill = new SolidBrush(Current.ControlBackColor);
                e.Graphics.FillRectangle(fill, leftover);
            }

            private static void ListView_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
            {
                ListView lv = (ListView)sender;

                using Brush back = new SolidBrush(Current.HeaderBackColor);

                Rectangle fillRect = e.Bounds;

                if (e.ColumnIndex == lv.Columns.Count - 1 && lv.Columns.Count > 0)
                {
                    int remaining = lv.ClientSize.Width - e.Bounds.Left;

                    if (remaining > e.Bounds.Width)
                    {
                        fillRect = new Rectangle(e.Bounds.Left, e.Bounds.Top, remaining, e.Bounds.Height);
                    }
                }

                e.Graphics.FillRectangle(back, fillRect);

                TextRenderer.DrawText(
                    e.Graphics,
                    e.Header.Text,
                    e.Font,
                    e.Bounds,
                    Current.HeaderForeColor,
                    TextFormatFlags.Left |
                    TextFormatFlags.VerticalCenter
                );
            }

            private static void ListView_DrawItem(object sender, DrawListViewItemEventArgs e)
            {
                e.DrawDefault = false;
            }

            private static void ListView_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
            {
                ListView lv = (ListView)sender;

                Color back = e.Item.Selected ? Current.SelectionBackColor : Current.ControlBackColor;
                Color fore = e.Item.Selected ? Current.SelectionForeColor : Current.ForeColor;

                using Brush b = new SolidBrush(back);

                e.Graphics.FillRectangle(b, e.Bounds);

                int x = e.Bounds.Left + 4;

                if (e.ColumnIndex == 0 && lv.SmallImageList != null && !string.IsNullOrEmpty(e.Item.ImageKey))
                {
                    Image img = null;

                    if (lv.SmallImageList.Images.ContainsKey(e.Item.ImageKey))
                    {
                        img = lv.SmallImageList.Images[e.Item.ImageKey];
                    }


                    if (img != null)
                    {
                        int y = e.Bounds.Top + (e.Bounds.Height - img.Height) / 2;
                        e.Graphics.DrawImage(img, x, y);

                        x += img.Width + 5;
                    }
                }

                TextRenderer.DrawText(
                    e.Graphics,
                    e.SubItem.Text,
                    lv.Font,
                    new Rectangle(x, e.Bounds.Top, e.Bounds.Width - (x - e.Bounds.Left), e.Bounds.Height),
                    fore,
                    TextFormatFlags.Left |
                    TextFormatFlags.VerticalCenter
                );
            }

            private static void ApplyTreeView(TreeView tv)
            {
                SetBackColorIfUnmodified(tv, Current.ControlBackColor);
                SetForeColorIfUnmodified(tv, Current.ForeColor);

                tv.BorderStyle = BorderStyle.FixedSingle;
                tv.HideSelection = false;
            }

            private static void ApplyDataGridView(DataGridView dgv)
            {
                SaveOriginalColor(dgv);

                if (dgv.BackColor != OriginalColors[dgv].Back)
                    return;

                dgv.BackgroundColor = Current.ControlBackColor;
                dgv.BorderStyle = BorderStyle.FixedSingle;
                dgv.GridColor = Current.GridColor;
                dgv.EnableHeadersVisualStyles = false;

                // Header
                dgv.ColumnHeadersDefaultCellStyle.BackColor = Current.HeaderBackColor;
                dgv.ColumnHeadersDefaultCellStyle.ForeColor = Current.HeaderForeColor;
                dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = Current.HeaderBackColor;

                // Row
                dgv.DefaultCellStyle.BackColor = Current.ControlBackColor;
                dgv.DefaultCellStyle.ForeColor = Current.ForeColor;
                dgv.DefaultCellStyle.SelectionBackColor = Current.SelectionBackColor;
                dgv.DefaultCellStyle.SelectionForeColor = Current.SelectionForeColor;

                // Alternating row
                dgv.AlternatingRowsDefaultCellStyle.BackColor =
                    Color.FromArgb(
                        Math.Max(Current.ControlBackColor.R - 5, 0),
                        Math.Max(Current.ControlBackColor.G - 5, 0),
                        Math.Max(Current.ControlBackColor.B - 5, 0)
                    );

                dgv.RowHeadersDefaultCellStyle.BackColor = Current.HeaderBackColor;

                dgv.RowHeadersDefaultCellStyle.ForeColor = Current.HeaderForeColor;
            }

            private static void ApplyTabPage(TabPage tp)
            {
                SetBackColorIfUnmodified(tp, Current.ControlBackColor);
                SetForeColorIfUnmodified(tp, Current.ForeColor);
            }

            private static void ApplyToolStripMenuItem(ToolStripMenuItem item)
            {
                item.BackColor = Current.ControlBackColor;
                item.ForeColor = Current.ForeColor;
                item.Checked = item.Checked;
                item.CheckOnClick = item.CheckOnClick;

                if (item.DropDown != null)
                {
                    item.DropDown.BackColor = Current.ControlBackColor;
                    item.DropDown.ForeColor = Current.ForeColor;
                    item.DropDown.Renderer = new ToolStripProfessionalRenderer(new ThemeColorTable());
                }

                foreach (ToolStripItem child in item.DropDownItems)
                {
                    if (child is ToolStripMenuItem subItem)
                    {
                        ApplyToolStripMenuItem(subItem);
                    }
                    else
                    {
                        ApplyToolStripItem(child);
                    }
                }
            }

            private static void ApplyContextMenuStrip(ContextMenuStrip menu)
            {
                menu.BackColor = Current.ControlBackColor;
                menu.ForeColor = Current.ForeColor;

                menu.Renderer = new ToolStripProfessionalRenderer(new ThemeColorTable());

                foreach (ToolStripItem item in menu.Items)
                {
                    ApplyToolStripItem(item);
                }
            }

            private static void TabControl_DrawItem(object sender, DrawItemEventArgs e)
            {
                try
                {
                    TabControl tab = (TabControl)sender;

                    bool selected = e.Index == tab.SelectedIndex;

                    Rectangle rect = e.Bounds;

                    using Brush bg = new SolidBrush(Current.ControlBackColor);

                    e.Graphics.FillRectangle(bg, rect);

                    Color textColor = Current.ForeColor;

                    // Selected Tab
                    if (selected)
                    {
                        textColor = Current.AccentColor;
                        Rectangle highlight = new Rectangle(rect.Left + 8, rect.Bottom - 3, rect.Width - 16, 3);

                        using Brush accent = new SolidBrush(Current.AccentColor);

                        e.Graphics.FillRectangle(accent, highlight);
                    }

                    TextRenderer.DrawText(
                        e.Graphics,
                        tab.TabPages[e.Index].Text,
                        tab.Font,
                        rect,
                        textColor,
                        TextFormatFlags.HorizontalCenter |
                        TextFormatFlags.VerticalCenter
                    );
                }
                catch
                {

                }
            }

            private static void ApplyMenuStrip(MenuStrip menu)
            {
                menu.BackColor = Current.ControlBackColor;
                menu.ForeColor = Current.ForeColor;

                foreach (ToolStripItem item in menu.Items)
                {
                    ApplyToolStripItem(item);
                }
            }

            private static void ApplyToolStrip(ToolStrip strip)
            {
                strip.BackColor = Current.ControlBackColor;
                strip.ForeColor = Current.ForeColor;

                strip.Renderer = new ToolStripProfessionalRenderer(new ThemeColorTable());

                foreach (ToolStripItem item in strip.Items)
                {
                    ApplyToolStripItem(item);
                }
            }

            private static void ApplyStatusStrip(StatusStrip strip)
            {
                strip.BackColor = Current.ControlBackColor;

                strip.ForeColor = Current.ForeColor;

                foreach (ToolStripItem item in strip.Items)
                {
                    ApplyToolStripItem(item);
                }
            }

            private static void ApplyToolStripItem(ToolStripItem item)
            {
                item.BackColor = Current.ControlBackColor;
                item.ForeColor = Current.ForeColor;

                if (item is ToolStripDropDownItem dropDown)
                {
                    dropDown.DropDown.BackColor = Current.ControlBackColor;
                    dropDown.DropDown.ForeColor = Current.ForeColor;
                    dropDown.DropDown.Renderer = new ToolStripProfessionalRenderer(new ThemeColorTable());

                    foreach (ToolStripItem child in dropDown.DropDownItems)
                    {
                        ApplyToolStripItem(child);
                    }
                }
            }

            private static void ApplyToolStripDropDownButton(ToolStripDropDownButton button)
            {
                button.BackColor = Current.ControlBackColor;
                button.ForeColor = Current.ForeColor;
                button.DropDown.BackColor = Current.ControlBackColor;

                button.DropDown.ForeColor = Current.ForeColor;
                button.DropDown.Renderer = new ToolStripProfessionalRenderer(new ThemeColorTable());

                foreach (ToolStripItem item in button.DropDownItems)
                {
                    ApplyToolStripItem(item);
                }
            }
        }
    }
}
