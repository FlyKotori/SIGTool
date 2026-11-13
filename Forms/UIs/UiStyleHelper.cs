using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using SIGTool.Datas;
using SIGTool.Modules.Core;

namespace SIGTool.Forms.UIs
{
    public enum UiButtonRole
    {
        Default = 0,
        PrimaryBlue = 1,
        PositiveGreen = 2,
        WarningAmber = 3,
        DangerRed = 4
    }

    public enum GaugeBarRole
    {
        HpJade = 0,
        QiBlue = 1
    }

    public interface IThemeableView
    {
        void ApplyTheme();
    }

    /// <summary>
    /// 启用 WinForms 原生 StandardDoubleClick 能力的平面交互按钮（确保网格槽位与紧凑格子可靠触发双击事件）
    /// </summary>
    public class InteractiveSlotButton : Button
    {
        public InteractiveSlotButton()
        {
            this.SetStyle(ControlStyles.StandardClick | ControlStyles.StandardDoubleClick, true);
            this.DoubleBuffered = true;
        }
    }

    /// <summary>
    /// 单套主题调色板定义：
    /// • StandardSlate   = 标准暗蓝彩色语义皮肤
    /// • MinimalDarkGray = 纯灰度深色办公皮肤（严格 R == G == B，零彩色干扰，靠明度与对比区分层级）
    /// • CleanOfficeLight= 纯灰度白底办公皮肤（严格 R == G == B，零彩色干扰，靠明度与对比区分层级）
    /// </summary>
    public sealed class UiThemePalette
    {
        public UiThemeMode Mode { get; set; }
        public string DisplayNameCn { get; set; }

        public bool IsGrayscale
        {
            get { return Mode == UiThemeMode.MinimalDarkGray || Mode == UiThemeMode.CleanOfficeLight; }
        }

        public Color BgWindow { get; set; }
        public Color BgCard { get; set; }
        public Color BgSubtle { get; set; }
        public Color BgHeader { get; set; }
        public Color BgFooter { get; set; }
        public Color BgLogBox { get; set; }
        public Color BgBattleLogBox { get; set; }

        public Color TextHeader { get; set; }
        public Color TextHeaderSub { get; set; }
        public Color TextPrimary { get; set; }
        public Color TextMuted { get; set; }
        public Color TextBattleLog { get; set; }
        public Color BorderStrong { get; set; }
        public Color BorderSubtle { get; set; }

        public Color AccentBlue { get; set; }
        public Color AccentJade { get; set; }
        public Color AccentGold { get; set; }
        public Color AccentCrimson { get; set; }

        public Color GridHeaderBg { get; set; }
        public Color GridSelectionBg { get; set; }
        public Color GridSelectionFg { get; set; }
        public Color TabActiveBg { get; set; }
        public Color TabInactiveBg { get; set; }

        public Color GaugeTrackBg { get; set; }
        public Color GaugeHpFill { get; set; }
        public Color GaugeQiFill { get; set; }
        public Color GaugeOverlayText { get; set; }

        public Color TileSelectedBorder { get; set; }
        public Color TileSelectedBg { get; set; }
        public Color TileSelectedFg { get; set; }
        public Color TileEquippedBorder { get; set; }
        public Color TileEquippedBg { get; set; }
        public Color TileEquippedFg { get; set; }
        public Color TileLockedBorder { get; set; }
        public Color TileLockedBg { get; set; }
        public Color TileLockedFg { get; set; }
        public Color TileNormalBorder { get; set; }
        public Color TileNormalBg { get; set; }
        public Color TileNormalFg { get; set; }
        public Color TileRightTopBg { get; set; }
        public Color TileRightTopFg { get; set; }
        public Color TileRightBotBg { get; set; }
        public Color TileRightBotFg { get; set; }

        public Color SlotLockedBg { get; set; }
        public Color SlotEmptySelBg { get; set; }
        public Color SlotConstructBg { get; set; }
        public Color SlotConstructFg { get; set; }
        public Color SlotBuiltBg { get; set; }
        public Color SlotBuiltSelBg { get; set; }
        public Color SlotBuiltFg { get; set; }

        public Color SpeedActiveBg { get; set; }
        public Color SpeedActiveFg { get; set; }

        public void GetButtonColors(UiButtonRole role, out Color bg, out Color fg, out Color border)
        {
            if (Mode == UiThemeMode.MinimalDarkGray)
            {
                // 纯灰度深色：通过灰阶深浅与边框亮度区分角色（R == G == B）
                switch (role)
                {
                    case UiButtonRole.PrimaryBlue:
                        bg = Color.FromArgb(64, 64, 64);
                        fg = Color.FromArgb(242, 242, 242);
                        border = Color.FromArgb(146, 146, 146);
                        break;
                    case UiButtonRole.PositiveGreen:
                        bg = Color.FromArgb(56, 56, 56);
                        fg = Color.FromArgb(232, 232, 232);
                        border = Color.FromArgb(128, 128, 128);
                        break;
                    case UiButtonRole.WarningAmber:
                        bg = Color.FromArgb(60, 60, 60);
                        fg = Color.FromArgb(236, 236, 236);
                        border = Color.FromArgb(136, 136, 136);
                        break;
                    case UiButtonRole.DangerRed:
                        bg = Color.FromArgb(52, 52, 52);
                        fg = Color.FromArgb(228, 228, 228);
                        border = Color.FromArgb(122, 122, 122);
                        break;
                    default:
                        bg = Color.FromArgb(44, 44, 44);
                        fg = Color.FromArgb(212, 212, 212);
                        border = Color.FromArgb(88, 88, 88);
                        break;
                }
            }
            else if (Mode == UiThemeMode.CleanOfficeLight)
            {
                // 纯灰度白底：通过纸面灰阶层次与深灰边框区分角色（R == G == B）
                switch (role)
                {
                    case UiButtonRole.PrimaryBlue:
                        bg = Color.FromArgb(226, 226, 226);
                        fg = Color.FromArgb(18, 18, 18);
                        border = Color.FromArgb(120, 120, 120);
                        break;
                    case UiButtonRole.PositiveGreen:
                        bg = Color.FromArgb(234, 234, 234);
                        fg = Color.FromArgb(24, 24, 24);
                        border = Color.FromArgb(140, 140, 140);
                        break;
                    case UiButtonRole.WarningAmber:
                        bg = Color.FromArgb(230, 230, 230);
                        fg = Color.FromArgb(22, 22, 22);
                        border = Color.FromArgb(132, 132, 132);
                        break;
                    case UiButtonRole.DangerRed:
                        bg = Color.FromArgb(236, 236, 236);
                        fg = Color.FromArgb(26, 26, 26);
                        border = Color.FromArgb(136, 136, 136);
                        break;
                    default:
                        bg = Color.FromArgb(246, 246, 246);
                        fg = Color.FromArgb(36, 36, 36);
                        border = Color.FromArgb(186, 186, 186);
                        break;
                }
            }
            else
            {
                switch (role)
                {
                    case UiButtonRole.PrimaryBlue:
                        bg = Color.FromArgb(219, 234, 254);
                        fg = Color.FromArgb(30, 64, 175);
                        border = Color.FromArgb(147, 197, 253);
                        break;
                    case UiButtonRole.PositiveGreen:
                        bg = Color.FromArgb(220, 252, 231);
                        fg = Color.FromArgb(22, 101, 52);
                        border = Color.FromArgb(134, 239, 172);
                        break;
                    case UiButtonRole.WarningAmber:
                        bg = Color.FromArgb(254, 243, 199);
                        fg = Color.FromArgb(146, 64, 14);
                        border = Color.FromArgb(252, 211, 77);
                        break;
                    case UiButtonRole.DangerRed:
                        bg = Color.FromArgb(254, 226, 226);
                        fg = Color.FromArgb(153, 27, 27);
                        border = Color.FromArgb(252, 165, 165);
                        break;
                    default:
                        bg = Color.FromArgb(236, 241, 248);
                        fg = TextPrimary;
                        border = BorderStrong;
                        break;
                }
            }
        }
    }

    internal sealed class ButtonRoleTag
    {
        public UiButtonRole Role;
        public ButtonRoleTag(UiButtonRole role) { Role = role; }
    }

    internal sealed class ThemedMenuColorTable : ProfessionalColorTable
    {
        private readonly UiThemePalette _p;
        public ThemedMenuColorTable(UiThemePalette p) { _p = p; }
        public override Color ToolStripDropDownBackground { get { return _p.BgCard; } }
        public override Color ImageMarginGradientBegin { get { return _p.BgSubtle; } }
        public override Color ImageMarginGradientMiddle { get { return _p.BgSubtle; } }
        public override Color ImageMarginGradientEnd { get { return _p.BgSubtle; } }
        public override Color MenuBorder { get { return _p.BorderStrong; } }
        public override Color MenuItemBorder { get { return _p.TileSelectedBorder; } }
        public override Color MenuItemSelected { get { return _p.GridSelectionBg; } }
        public override Color SeparatorDark { get { return _p.BorderSubtle; } }
        public override Color SeparatorLight { get { return _p.BgCard; } }
    }

    /// <summary>
    /// 通用 UI 配色、纯灰度双办公皮肤引擎、紧凑 GrowAndShrink 按钮体系与持久化比例分栏控制器
    /// </summary>
    public static class UiStyleHelper
    {
        public static readonly Font SmallFont = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Regular);
        public static readonly Font SmallBoldFont = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Bold);
        public static readonly Font NormalFont = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular);
        public static readonly Font BoldFont = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
        public static readonly Font HeaderFont = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
        public static readonly Font MonoFont = new Font("Consolas", 9.2F, FontStyle.Regular);
        public static readonly Font SmallMonoBold = new Font("Consolas", 8.5F, FontStyle.Bold);
        public static readonly Font SmallMonoReg = new Font("Consolas", 8.2F, FontStyle.Regular);

        private static readonly UiThemePalette _paletteStandard = BuildStandardSlatePalette();
        private static readonly UiThemePalette _paletteDarkGray = BuildMinimalDarkGrayPalette();
        private static readonly UiThemePalette _paletteOfficeLight = BuildCleanOfficeLightPalette();

        public static UiThemePalette Current { get; private set; } = _paletteDarkGray;

        public static event Action OnThemeChanged;

        public static Color BgWindow { get { return Current.BgWindow; } }
        public static Color BgCard { get { return Current.BgCard; } }
        public static Color BgHeader { get { return Current.BgHeader; } }
        public static Color TextPrimary { get { return Current.TextPrimary; } }
        public static Color TextMuted { get { return Current.TextMuted; } }
        public static Color AccentBlue { get { return Current.AccentBlue; } }
        public static Color AccentJade { get; set; } = _paletteDarkGray.AccentJade;
        public static Color AccentGold { get { return Current.AccentGold; } }
        public static Color AccentCrimson { get { return Current.AccentCrimson; } }

        public static Color GetPositiveColor()
        {
            return Current.AccentJade;
        }

        public static void SetTheme(UiThemeMode mode, bool raiseEvent = true)
        {
            switch (mode)
            {
                case UiThemeMode.MinimalDarkGray:
                    Current = _paletteDarkGray;
                    break;
                case UiThemeMode.CleanOfficeLight:
                    Current = _paletteOfficeLight;
                    break;
                default:
                    Current = _paletteStandard;
                    break;
            }
            AccentJade = Current.AccentJade;
            if (raiseEvent && OnThemeChanged != null)
            {
                OnThemeChanged();
            }
        }

        public static UiThemeMode GetNextThemeMode(UiThemeMode cur)
        {
            switch (cur)
            {
                case UiThemeMode.StandardSlate: return UiThemeMode.MinimalDarkGray;
                case UiThemeMode.MinimalDarkGray: return UiThemeMode.CleanOfficeLight;
                default: return UiThemeMode.StandardSlate;
            }
        }

        public static string GetThemeShortCn(UiThemeMode mode)
        {
            switch (mode)
            {
                case UiThemeMode.MinimalDarkGray: return "深灰灰度";
                case UiThemeMode.CleanOfficeLight: return "白底灰度";
                default: return "标准暗蓝";
            }
        }

        public static ContextMenuStrip CreateThemedContextMenu()
        {
            var menu = new ContextMenuStrip
            {
                Font = NormalFont,
                ShowImageMargin = false
            };
            StyleContextMenu(menu);
            return menu;
        }

        public static void StyleContextMenu(ContextMenuStrip menu)
        {
            if (menu == null) return;
            var p = Current;
            menu.Renderer = new ToolStripProfessionalRenderer(new ThemedMenuColorTable(p));
            menu.BackColor = p.BgCard;
            menu.ForeColor = p.TextPrimary;
            StyleMenuItemsRecursive(menu.Items, p);
        }

        private static void StyleMenuItemsRecursive(ToolStripItemCollection items, UiThemePalette p)
        {
            if (items == null) return;
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                item.BackColor = p.BgCard;
                item.ForeColor = item.Enabled ? p.TextPrimary : p.TextMuted;
                var mi = item as ToolStripMenuItem;
                if (mi != null && mi.DropDownItems.Count > 0)
                {
                    mi.DropDown.BackColor = p.BgCard;
                    mi.DropDown.ForeColor = p.TextPrimary;
                    StyleMenuItemsRecursive(mi.DropDownItems, p);
                }
            }
        }

        /// <summary>
        /// UI-UX-01 & UI-UX-06：创建支持自适应比例且自动持久化记忆用户拖拽位置的 SplitContainer
        /// </summary>
        public static SplitContainer CreateProportionalSplit(Orientation orientation, double initialRatio, int minPanel1 = 220, int minPanel2 = 220, string persistKey = null)
        {
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = orientation,
                SplitterWidth = 5
            };
            ConfigureProportionalSplit(split, initialRatio, minPanel1, minPanel2, persistKey);
            return split;
        }

        public static void ConfigureProportionalSplit(SplitContainer split, double initialRatio, int minPanel1 = 220, int minPanel2 = 220, string persistKey = null)
        {
            if (split == null) return;
            double savedOrInit = ClientSettingsService.Current != null
                ? ClientSettingsService.Current.GetSplitRatio(persistKey, initialRatio)
                : initialRatio;
            double ratio = Math.Max(0.15, Math.Min(0.85, savedOrInit));
            bool suppressTrack = false;

            Action applyRatio = () =>
            {
                if (split.IsDisposed) return;
                int total = (split.Orientation == Orientation.Vertical) ? split.Width : split.Height;
                int required = minPanel1 + minPanel2 + split.SplitterWidth + 16;
                if (total < required) return;

                suppressTrack = true;
                try
                {
                    if (split.Panel1MinSize != minPanel1) split.Panel1MinSize = minPanel1;
                    if (split.Panel2MinSize != minPanel2) split.Panel2MinSize = minPanel2;

                    int target = (int)Math.Round(total * ratio);
                    int maxAllowed = total - minPanel2 - split.SplitterWidth;
                    target = Math.Max(minPanel1, Math.Min(maxAllowed, target));
                    if (target > 0 && Math.Abs(split.SplitterDistance - target) > 2)
                    {
                        split.SplitterDistance = target;
                    }
                }
                catch
                {
                }
                finally
                {
                    suppressTrack = false;
                }
            };

            split.HandleCreated += (s, e) => applyRatio();
            split.SizeChanged += (s, e) => applyRatio();
            split.SplitterMoved += (s, e) =>
            {
                if (suppressTrack || split.IsDisposed) return;
                int total = (split.Orientation == Orientation.Vertical) ? split.Width : split.Height;
                if (total >= minPanel1 + minPanel2 + 20)
                {
                    double userRatio = (double)split.SplitterDistance / total;
                    if (userRatio >= 0.12 && userRatio <= 0.88)
                    {
                        ratio = userRatio;
                        if (!string.IsNullOrEmpty(persistKey) && ClientSettingsService.Current != null)
                        {
                            ClientSettingsService.Current.SetSplitRatio(persistKey, ratio);
                        }
                    }
                }
            };
        }

        public static FlowLayoutPanel CreateCleanToolbarStrip(DockStyle dock = DockStyle.Top, int height = 30)
        {
            var flp = new FlowLayoutPanel
            {
                Dock = dock,
                Height = height,
                WrapContents = false,
                AutoScroll = false,
                Margin = new Padding(0),
                Padding = new Padding(1, 1, 1, 1),
                BackColor = Current.BgWindow
            };
            EnableDoubleBuffer(flp);
            return flp;
        }

        public static Color GetElementColor(ElementType elem)
        {
            if (Current.IsGrayscale)
            {
                return Current.TextPrimary;
            }
            switch (elem)
            {
                case ElementType.Metal: return Color.FromArgb(175, 130, 25);
                case ElementType.Wood: return Color.FromArgb(28, 138, 72);
                case ElementType.Water: return Color.FromArgb(30, 110, 195);
                case ElementType.Fire: return Color.FromArgb(200, 58, 45);
                case ElementType.Earth: return Color.FromArgb(135, 95, 55);
                default: return TextMuted;
            }
        }

        /// <summary>
        /// 紧凑无膨胀按钮（GrowAndShrink 消除 WinForms 默认 75px 最小宽度浪费）
        /// </summary>
        public static Button CreateButton(string text, EventHandler onClick, UiButtonRole role = UiButtonRole.Default, bool extraCompact = false)
        {
            var btn = new Button
            {
                Text = text,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlatStyle = FlatStyle.Flat,
                Padding = extraCompact ? new Padding(3, 0, 3, 0) : new Padding(4, 1, 4, 1),
                Margin = extraCompact ? new Padding(1, 1, 1, 1) : new Padding(2, 1, 2, 1),
                Font = extraCompact ? SmallFont : NormalFont,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false,
                Tag = new ButtonRoleTag(role)
            };
            btn.FlatAppearance.BorderSize = 1;
            ApplyButtonRoleVisuals(btn, role);
            if (onClick != null) btn.Click += onClick;
            return btn;
        }

        public static void ApplyButtonRoleVisuals(Button btn, UiButtonRole role)
        {
            if (btn == null) return;
            Color bg, fg, border;
            Current.GetButtonColors(role, out bg, out fg, out border);
            btn.BackColor = bg;
            btn.ForeColor = fg;
            btn.FlatAppearance.BorderColor = border;
        }

        public static DataGridView CreateDoubleBufferedGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                Font = NormalFont,
                EnableHeadersVisualStyles = false
            };
            grid.ColumnHeadersDefaultCellStyle.Font = BoldFont;
            grid.RowTemplate.Height = 24;

            PropertyInfo prop = typeof(DataGridView).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
            if (prop != null) prop.SetValue(grid, true, null);

            ApplyGridTheme(grid);
            return grid;
        }

        public static void ApplyGridTheme(DataGridView grid)
        {
            if (grid == null) return;
            var p = Current;
            grid.BackgroundColor = p.BgCard;
            grid.GridColor = p.BorderSubtle;
            grid.DefaultCellStyle.BackColor = p.BgCard;
            grid.DefaultCellStyle.ForeColor = p.TextPrimary;
            grid.DefaultCellStyle.SelectionBackColor = p.GridSelectionBg;
            grid.DefaultCellStyle.SelectionForeColor = p.GridSelectionFg;
            grid.ColumnHeadersDefaultCellStyle.BackColor = p.GridHeaderBg;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = p.TextPrimary;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = p.GridHeaderBg;
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = p.TextPrimary;

            for (int c = 0; c < grid.Columns.Count; c++)
            {
                var btnCol = grid.Columns[c] as DataGridViewButtonColumn;
                if (btnCol != null)
                {
                    btnCol.FlatStyle = FlatStyle.Flat;
                    btnCol.DefaultCellStyle.BackColor = p.BgSubtle;
                    btnCol.DefaultCellStyle.ForeColor = p.AccentBlue;
                    btnCol.DefaultCellStyle.SelectionBackColor = p.GridSelectionBg;
                    btnCol.DefaultCellStyle.SelectionForeColor = p.GridSelectionFg;
                }
                var cmbCol = grid.Columns[c] as DataGridViewComboBoxColumn;
                if (cmbCol != null)
                {
                    cmbCol.FlatStyle = FlatStyle.Flat;
                    cmbCol.DefaultCellStyle.BackColor = p.BgCard;
                    cmbCol.DefaultCellStyle.ForeColor = p.TextPrimary;
                }
            }
            grid.Invalidate();
        }

        public static void ApplyThemeRecursive(Control root)
        {
            if (root == null || root.IsDisposed) return;
            var p = Current;

            if (root is Form)
            {
                root.BackColor = p.BgWindow;
                root.ForeColor = p.TextPrimary;
            }
            else if (root is GroupBox || root is TabPage)
            {
                root.BackColor = p.BgWindow;
                root.ForeColor = p.TextPrimary;
            }
            else if (root is SplitContainer)
            {
                var sc = (SplitContainer)root;
                sc.BackColor = p.BorderSubtle;
                sc.Panel1.BackColor = p.BgWindow;
                sc.Panel2.BackColor = p.BgWindow;
            }
            else if (root is DataGridView)
            {
                ApplyGridTheme((DataGridView)root);
            }
            else if (root is Button)
            {
                var btn = (Button)root;
                var roleTag = btn.Tag as ButtonRoleTag;
                if (roleTag != null)
                {
                    ApplyButtonRoleVisuals(btn, roleTag.Role);
                }
            }
            else if (root is CheckBox)
            {
                root.ForeColor = p.TextPrimary;
            }
            else if (root is ComboBox)
            {
                root.BackColor = p.BgCard;
                root.ForeColor = p.TextPrimary;
            }
            else if (root is TextBox)
            {
                var tb = (TextBox)root;
                tb.BackColor = p.BgCard;
                tb.ForeColor = p.TextPrimary;
            }
            else if (root is FlowLayoutPanel)
            {
                root.BackColor = p.BgWindow;
            }
            else if (root is TreeView)
            {
                var tv = (TreeView)root;
                tv.BackColor = p.BgCard;
                tv.ForeColor = p.TextPrimary;
                tv.LineColor = p.BorderStrong;
            }

            for (int i = 0; i < root.Controls.Count; i++)
            {
                ApplyThemeRecursive(root.Controls[i]);
            }

            var themeable = root as IThemeableView;
            if (themeable != null)
            {
                themeable.ApplyTheme();
            }
        }

        private static readonly ToolTip _sharedToolTip = new ToolTip
        {
            ShowAlways = true,
            InitialDelay = 260,
            ReshowDelay = 100,
            AutoPopDelay = 14000
        };

        /// <summary>
        /// EXT-03 统一悬浮提示绑定（按钮文字保持简洁无键位角标，快捷键说明统一收纳于悬浮框与底部状态栏）
        /// </summary>
        public static void SetToolTip(Control ctrl, string tipText)
        {
            if (ctrl == null) return;
            _sharedToolTip.SetToolTip(ctrl, tipText ?? "");
        }

        public static Control UnwrapActiveLeafControl(Control root)
        {
            Control cur = root;
            while (cur is ContainerControl)
            {
                var cc = (ContainerControl)cur;
                if (cc.ActiveControl == null || cc.ActiveControl == cur) break;
                cur = cc.ActiveControl;
            }
            return cur;
        }

        /// <summary>
        /// 判断当前焦点是否位于文本输入框/下拉框/表格单元格编辑态，防止单键快捷键干扰打字输入
        /// </summary>
        public static bool IsTextInputFocused(Control activeRoot)
        {
            Control leaf = UnwrapActiveLeafControl(activeRoot);
            if (leaf == null) return false;
            if (leaf is TextBoxBase || leaf is UpDownBase) return true;
            if (leaf is ComboBox) return true;
            var dgv = leaf as DataGridView;
            if (dgv != null && dgv.IsCurrentCellInEditMode) return true;
            return false;
        }

        public static void EnableDoubleBuffer(Control ctrl)
        {
            if (ctrl == null) return;
            PropertyInfo prop = typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
            if (prop != null) prop.SetValue(ctrl, true, null);
        }

        /// <summary>
        /// UI 防跳字协议：计算非负整数显示所需字符位数（至少 minDigits 位）
        /// </summary>
        public static int DigitWidth(double maxVal, int minDigits = 1)
        {
            long v = (long)Math.Max(0.0, Math.Round(Math.Abs(maxVal)));
            int d = 1;
            while (v >= 10)
            {
                v /= 10;
                d++;
            }
            return Math.Max(minDigits, d);
        }

        /// <summary>
        /// UI 防跳字协议：按固定字符槽宽右对齐整数（配合等宽字体可保证数值增减时像素宽度恒定不变）
        /// </summary>
        public static string PadInt(double val, int width)
        {
            long v = (long)Math.Round(val);
            return v.ToString().PadLeft(Math.Max(1, width), ' ');
        }

        /// <summary>
        /// UI 防跳字协议：按上限位数固定“当前/上限”总槽宽（如 " 605/1220"），彻底消除回血/充气时右侧文字横跳
        /// </summary>
        public static string FormatFixedPair(double cur, double max, int minDigits = 3)
        {
            int w = DigitWidth(Math.Max(cur, max), minDigits);
            string curStr = ((long)Math.Round(cur)).ToString().PadLeft(w, ' ');
            string maxStr = ((long)Math.Round(max)).ToString().PadLeft(w, ' ');
            return curStr + "/" + maxStr;
        }

        /// <summary>
        /// UI 防跳字协议：仅在文本实质变化时写入 Control.Text，避免无意义重排与重绘
        /// </summary>
        public static void SetTextIfChanged(Control ctrl, string newText)
        {
            if (ctrl == null) return;
            string target = newText ?? "";
            if (!string.Equals(ctrl.Text, target, StringComparison.Ordinal))
            {
                ctrl.Text = target;
            }
        }

        /// <summary>
        /// UI 防跳字协议：仅在单元格显示文本实质变化时更新 DataGridViewCell.Value，抑制 10Hz 整行闪烁
        /// </summary>
        public static void SetCellIfChanged(DataGridViewCell cell, string newText)
        {
            if (cell == null) return;
            string target = newText ?? "";
            string cur = cell.Value != null ? cell.Value.ToString() : "";
            if (!string.Equals(cur, target, StringComparison.Ordinal))
            {
                cell.Value = target;
            }
        }

        public static string FormatNum(double val)
        {
            if (Math.Abs(val) >= 100000000.0) return string.Format("{0:F2}亿", val / 100000000.0);
            if (Math.Abs(val) >= 10000.0) return string.Format("{0:F2}万", val / 10000.0);
            return string.Format("{0:F0}", Math.Floor(val));
        }

        private static UiThemePalette BuildStandardSlatePalette()
        {
            var p = new UiThemePalette
            {
                Mode = UiThemeMode.StandardSlate,
                DisplayNameCn = "标准暗蓝",
                BgWindow = Color.FromArgb(244, 246, 249),
                BgCard = Color.FromArgb(255, 255, 255),
                BgSubtle = Color.FromArgb(238, 242, 247),
                BgHeader = Color.FromArgb(30, 41, 59),
                BgFooter = Color.FromArgb(252, 253, 255),
                BgLogBox = Color.FromArgb(250, 251, 253),
                BgBattleLogBox = Color.FromArgb(24, 28, 36),
                TextHeader = Color.White,
                TextHeaderSub = Color.FromArgb(226, 232, 240),
                TextPrimary = Color.FromArgb(30, 41, 59),
                TextMuted = Color.FromArgb(100, 116, 139),
                TextBattleLog = Color.FromArgb(226, 232, 240),
                BorderStrong = Color.FromArgb(195, 205, 218),
                BorderSubtle = Color.FromArgb(220, 227, 236),
                AccentBlue = Color.FromArgb(37, 99, 235),
                AccentJade = Color.FromArgb(16, 135, 100),
                AccentGold = Color.FromArgb(180, 115, 20),
                AccentCrimson = Color.FromArgb(190, 45, 55),
                GridHeaderBg = Color.FromArgb(235, 240, 246),
                GridSelectionBg = Color.FromArgb(219, 234, 254),
                GridSelectionFg = Color.FromArgb(30, 58, 138),
                TabActiveBg = Color.FromArgb(255, 255, 255),
                TabInactiveBg = Color.FromArgb(228, 234, 242),
                GaugeTrackBg = Color.FromArgb(226, 232, 240),
                GaugeHpFill = Color.FromArgb(134, 225, 168),
                GaugeQiFill = Color.FromArgb(147, 197, 253),
                GaugeOverlayText = Color.FromArgb(20, 28, 38),
                TileSelectedBorder = Color.FromArgb(37, 99, 235),
                TileSelectedBg = Color.FromArgb(219, 234, 254),
                TileSelectedFg = Color.FromArgb(30, 58, 138),
                TileEquippedBorder = Color.FromArgb(22, 163, 74),
                TileEquippedBg = Color.FromArgb(220, 252, 231),
                TileEquippedFg = Color.FromArgb(20, 83, 45),
                TileLockedBorder = Color.FromArgb(203, 213, 225),
                TileLockedBg = Color.FromArgb(241, 245, 249),
                TileLockedFg = Color.FromArgb(148, 163, 184),
                TileNormalBorder = Color.FromArgb(186, 198, 214),
                TileNormalBg = Color.White,
                TileNormalFg = Color.FromArgb(30, 41, 59),
                TileRightTopBg = Color.FromArgb(236, 240, 246),
                TileRightTopFg = Color.FromArgb(71, 85, 105),
                TileRightBotBg = Color.FromArgb(228, 233, 240),
                TileRightBotFg = Color.FromArgb(115, 128, 146),
                SlotLockedBg = Color.FromArgb(226, 232, 240),
                SlotEmptySelBg = Color.FromArgb(239, 246, 255),
                SlotConstructBg = Color.FromArgb(254, 249, 195),
                SlotConstructFg = Color.FromArgb(133, 77, 14),
                SlotBuiltBg = Color.FromArgb(240, 253, 244),
                SlotBuiltSelBg = Color.FromArgb(220, 252, 231),
                SlotBuiltFg = Color.FromArgb(20, 83, 45),
                SpeedActiveBg = Color.FromArgb(56, 189, 248),
                SpeedActiveFg = Color.FromArgb(15, 23, 42)
            };
            AccentJade = p.AccentJade;
            return p;
        }

        /// <summary>
        /// 纯灰度深灰办公皮肤（全部色值严格 R == G == B，无任何视觉彩色）
        /// </summary>
        private static UiThemePalette BuildMinimalDarkGrayPalette()
        {
            return new UiThemePalette
            {
                Mode = UiThemeMode.MinimalDarkGray,
                DisplayNameCn = "深灰灰度",
                BgWindow = Color.FromArgb(30, 30, 30),
                BgCard = Color.FromArgb(38, 38, 38),
                BgSubtle = Color.FromArgb(46, 46, 46),
                BgHeader = Color.FromArgb(22, 22, 22),
                BgFooter = Color.FromArgb(34, 34, 34),
                BgLogBox = Color.FromArgb(26, 26, 26),
                BgBattleLogBox = Color.FromArgb(24, 24, 24),
                TextHeader = Color.FromArgb(230, 230, 230),
                TextHeaderSub = Color.FromArgb(176, 176, 176),
                TextPrimary = Color.FromArgb(218, 218, 218),
                TextMuted = Color.FromArgb(142, 142, 142),
                TextBattleLog = Color.FromArgb(202, 202, 202),
                BorderStrong = Color.FromArgb(88, 88, 88),
                BorderSubtle = Color.FromArgb(58, 58, 58),
                AccentBlue = Color.FromArgb(242, 242, 242),
                AccentJade = Color.FromArgb(226, 226, 226),
                AccentGold = Color.FromArgb(196, 196, 196),
                AccentCrimson = Color.FromArgb(238, 238, 238),
                GridHeaderBg = Color.FromArgb(48, 48, 48),
                GridSelectionBg = Color.FromArgb(76, 76, 76),
                GridSelectionFg = Color.FromArgb(246, 246, 246),
                TabActiveBg = Color.FromArgb(50, 50, 50),
                TabInactiveBg = Color.FromArgb(33, 33, 33),
                GaugeTrackBg = Color.FromArgb(44, 44, 44),
                GaugeHpFill = Color.FromArgb(86, 86, 86),
                GaugeQiFill = Color.FromArgb(106, 106, 106),
                GaugeOverlayText = Color.FromArgb(240, 240, 240),
                TileSelectedBorder = Color.FromArgb(228, 228, 228),
                TileSelectedBg = Color.FromArgb(68, 68, 68),
                TileSelectedFg = Color.FromArgb(248, 248, 248),
                TileEquippedBorder = Color.FromArgb(156, 156, 156),
                TileEquippedBg = Color.FromArgb(52, 52, 52),
                TileEquippedFg = Color.FromArgb(232, 232, 232),
                TileLockedBorder = Color.FromArgb(54, 54, 54),
                TileLockedBg = Color.FromArgb(33, 33, 33),
                TileLockedFg = Color.FromArgb(104, 104, 104),
                TileNormalBorder = Color.FromArgb(74, 74, 74),
                TileNormalBg = Color.FromArgb(42, 42, 42),
                TileNormalFg = Color.FromArgb(214, 214, 214),
                TileRightTopBg = Color.FromArgb(36, 36, 36),
                TileRightTopFg = Color.FromArgb(176, 176, 176),
                TileRightBotBg = Color.FromArgb(32, 32, 32),
                TileRightBotFg = Color.FromArgb(132, 132, 132),
                SlotLockedBg = Color.FromArgb(33, 33, 33),
                SlotEmptySelBg = Color.FromArgb(62, 62, 62),
                SlotConstructBg = Color.FromArgb(56, 56, 56),
                SlotConstructFg = Color.FromArgb(226, 226, 226),
                SlotBuiltBg = Color.FromArgb(46, 46, 46),
                SlotBuiltSelBg = Color.FromArgb(68, 68, 68),
                SlotBuiltFg = Color.FromArgb(234, 234, 234),
                SpeedActiveBg = Color.FromArgb(218, 218, 218),
                SpeedActiveFg = Color.FromArgb(20, 20, 20)
            };
        }

        /// <summary>
        /// 纯灰度清爽白底办公皮肤（全部色值严格 R == G == B，无任何视觉彩色）
        /// </summary>
        private static UiThemePalette BuildCleanOfficeLightPalette()
        {
            return new UiThemePalette
            {
                Mode = UiThemeMode.CleanOfficeLight,
                DisplayNameCn = "白底灰度",
                BgWindow = Color.FromArgb(244, 244, 244),
                BgCard = Color.FromArgb(255, 255, 255),
                BgSubtle = Color.FromArgb(236, 236, 236),
                BgHeader = Color.FromArgb(250, 250, 250),
                BgFooter = Color.FromArgb(248, 248, 248),
                BgLogBox = Color.FromArgb(253, 253, 253),
                BgBattleLogBox = Color.FromArgb(250, 250, 250),
                TextHeader = Color.FromArgb(26, 26, 26),
                TextHeaderSub = Color.FromArgb(84, 84, 84),
                TextPrimary = Color.FromArgb(30, 30, 30),
                TextMuted = Color.FromArgb(112, 112, 112),
                TextBattleLog = Color.FromArgb(38, 38, 38),
                BorderStrong = Color.FromArgb(182, 182, 182),
                BorderSubtle = Color.FromArgb(216, 216, 216),
                AccentBlue = Color.FromArgb(16, 16, 16),
                AccentJade = Color.FromArgb(38, 38, 38),
                AccentGold = Color.FromArgb(62, 62, 62),
                AccentCrimson = Color.FromArgb(12, 12, 12),
                GridHeaderBg = Color.FromArgb(238, 238, 238),
                GridSelectionBg = Color.FromArgb(214, 214, 214),
                GridSelectionFg = Color.FromArgb(12, 12, 12),
                TabActiveBg = Color.FromArgb(255, 255, 255),
                TabInactiveBg = Color.FromArgb(234, 234, 234),
                GaugeTrackBg = Color.FromArgb(232, 232, 232),
                GaugeHpFill = Color.FromArgb(196, 196, 196),
                GaugeQiFill = Color.FromArgb(176, 176, 176),
                GaugeOverlayText = Color.FromArgb(20, 20, 20),
                TileSelectedBorder = Color.FromArgb(32, 32, 32),
                TileSelectedBg = Color.FromArgb(222, 222, 222),
                TileSelectedFg = Color.FromArgb(12, 12, 12),
                TileEquippedBorder = Color.FromArgb(108, 108, 108),
                TileEquippedBg = Color.FromArgb(236, 236, 236),
                TileEquippedFg = Color.FromArgb(24, 24, 24),
                TileLockedBorder = Color.FromArgb(214, 214, 214),
                TileLockedBg = Color.FromArgb(246, 246, 246),
                TileLockedFg = Color.FromArgb(150, 150, 150),
                TileNormalBorder = Color.FromArgb(190, 190, 190),
                TileNormalBg = Color.FromArgb(255, 255, 255),
                TileNormalFg = Color.FromArgb(32, 32, 32),
                TileRightTopBg = Color.FromArgb(242, 242, 242),
                TileRightTopFg = Color.FromArgb(72, 72, 72),
                TileRightBotBg = Color.FromArgb(234, 234, 234),
                TileRightBotFg = Color.FromArgb(116, 116, 116),
                SlotLockedBg = Color.FromArgb(238, 238, 238),
                SlotEmptySelBg = Color.FromArgb(224, 224, 224),
                SlotConstructBg = Color.FromArgb(230, 230, 230),
                SlotConstructFg = Color.FromArgb(36, 36, 36),
                SlotBuiltBg = Color.FromArgb(242, 242, 242),
                SlotBuiltSelBg = Color.FromArgb(220, 220, 220),
                SlotBuiltFg = Color.FromArgb(22, 22, 22),
                SpeedActiveBg = Color.FromArgb(46, 46, 46),
                SpeedActiveFg = Color.FromArgb(250, 250, 250)
            };
        }
    }

    public class SmoothGaugeBar : Panel, IThemeableView
    {
        private double _ratio = 0.0;
        private string _overlayText = "";
        private GaugeBarRole _role = GaugeBarRole.QiBlue;

        public SmoothGaugeBar(GaugeBarRole role = GaugeBarRole.QiBlue)
        {
            _role = role;
            this.DoubleBuffered = true;
            this.Height = 22;
            this.Margin = new Padding(2);
            ApplyTheme();
        }

        public GaugeBarRole Role
        {
            get { return _role; }
            set { _role = value; Invalidate(); }
        }

        public void ApplyTheme()
        {
            this.BackColor = UiStyleHelper.Current.GaugeTrackBg;
            Invalidate();
        }

        public void SetProgress(double ratio, string text)
        {
            double clamped = Math.Max(0.0, Math.Min(1.0, double.IsNaN(ratio) ? 0.0 : ratio));
            if (Math.Abs(_ratio - clamped) < 0.001 && _overlayText == text) return;
            _ratio = clamped;
            _overlayText = text ?? "";
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            var rect = this.ClientRectangle;
            if (rect.Width <= 2 || rect.Height <= 2) return;

            var p = UiStyleHelper.Current;
            Color fillCol = (_role == GaugeBarRole.HpJade) ? p.GaugeHpFill : p.GaugeQiFill;

            int fillW = (int)Math.Round((rect.Width - 2) * _ratio);
            if (fillW > 0)
            {
                using (var brush = new SolidBrush(fillCol))
                {
                    g.FillRectangle(brush, 1, 1, fillW, rect.Height - 2);
                }
            }

            using (var pen = new Pen(p.BorderStrong))
            {
                g.DrawRectangle(pen, 0, 0, rect.Width - 1, rect.Height - 1);
            }

            if (!string.IsNullOrEmpty(_overlayText))
            {
                TextRenderer.DrawText(g, _overlayText, UiStyleHelper.SmallMonoBold, rect,
                    p.GaugeOverlayText,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
        }
    }

    /// <summary>
    /// 架构级零闪烁双缓冲状态横幅控件：
    /// 替代高频刷新区域内的原生 Label（不触发父容器 PerformLayout、不使用 Color.Transparent、支持等宽字体无抖动重绘）
    /// </summary>
    public class DoubleBufferedStatusLabel : Panel, IThemeableView
    {
        private string _displayText = "";
        private readonly bool _hasBorder;
        private readonly bool _useCardBg;

        public ContentAlignment TextAlign { get; set; } = ContentAlignment.MiddleLeft;

        public DoubleBufferedStatusLabel(Font font = null, bool hasBorder = true, bool useCardBg = true)
        {
            _hasBorder = hasBorder;
            _useCardBg = useCardBg;
            this.DoubleBuffered = true;
            this.ResizeRedraw = true;
            this.Font = font ?? UiStyleHelper.SmallMonoReg;
            this.Padding = new Padding(6, 4, 6, 4);
            ApplyTheme();
        }

        public override string Text
        {
            get { return _displayText; }
            set
            {
                string next = value ?? "";
                if (string.Equals(_displayText, next, StringComparison.Ordinal)) return;
                _displayText = next;
                Invalidate();
            }
        }

        public void ApplyTheme()
        {
            var p = UiStyleHelper.Current;
            this.BackColor = _useCardBg ? p.BgCard : p.BgWindow;
            this.ForeColor = _useCardBg ? p.TextPrimary : p.TextMuted;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            var rect = this.ClientRectangle;
            if (rect.Width <= 2 || rect.Height <= 2) return;

            var p = UiStyleHelper.Current;
            Color bg = _useCardBg ? p.BgCard : this.BackColor;
            using (var bgBrush = new SolidBrush(bg))
            {
                g.FillRectangle(bgBrush, rect);
            }

            if (_hasBorder)
            {
                using (var pen = new Pen(p.BorderSubtle))
                {
                    g.DrawRectangle(pen, 0, 0, rect.Width - 1, rect.Height - 1);
                }
            }

            if (string.IsNullOrEmpty(_displayText)) return;

            var inner = new Rectangle(
                this.Padding.Left,
                this.Padding.Top,
                Math.Max(2, rect.Width - this.Padding.Horizontal),
                Math.Max(2, rect.Height - this.Padding.Vertical));

            TextFormatFlags flags = TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.WordBreak;
            if (TextAlign == ContentAlignment.MiddleLeft || TextAlign == ContentAlignment.MiddleCenter)
            {
                if (!_displayText.Contains("\n"))
                {
                    flags = TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter;
                }
            }
            TextRenderer.DrawText(g, _displayText, this.Font, inner, this.ForeColor, flags);
        }
    }

}
