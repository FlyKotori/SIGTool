using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using SIGTool.Datas;
using SIGTool.Forms.UIs;
using SIGTool.Modules.Core;

namespace SIGTool.Forms
{
    /// <summary>
    /// 系统托盘与全局双向老板键（一键隐藏 / 一键唤起）配置弹窗（EXT-01，纯灰度图标兼容）
    /// </summary>
    public class FormTrayHotkeySettings : Form, IThemeableView
    {
        private CheckBox _chkEnableHotkey;
        private TextBox _txtHotkeyCapture;
        private CheckBox _chkCloseToTray;
        private CheckBox _chkMinimalTitle;
        private Label _lblCaptureHint;

        private int _capturedModifiers;
        private int _capturedVk;

        public FormTrayHotkeySettings()
        {
            var cur = ClientSettingsService.Current;
            _capturedModifiers = cur.BossHotkeyModifiers;
            _capturedVk = cur.BossHotkeyVirtualKey;

            this.DoubleBuffered = true;
            this.Text = "老板键与系统托盘设置 (EXT-01)";
            this.Size = new Size(490, 310);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = UiStyleHelper.NormalFont;

            BuildUI(cur);
            UiStyleHelper.ApplyThemeRecursive(this);
        }

        private void BuildUI(ClientSettingsData cur)
        {
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(14, 12, 14, 10),
                ColumnCount = 1,
                RowCount = 6
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            _chkEnableHotkey = new CheckBox
            {
                Text = "启用全局双向老板键（一键隐藏至托盘 / 后台无焦点一键唤起）",
                Checked = cur.EnableBossHotkey,
                AutoSize = true,
                Dock = DockStyle.Fill
            };

            var pnlCaptureRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = false,
                Margin = new Padding(0)
            };

            var lblKeyTitle = new Label
            {
                Text = "当前全局热键：",
                AutoSize = true,
                Margin = new Padding(0, 6, 6, 0)
            };

            _txtHotkeyCapture = new TextBox
            {
                Width = 165,
                ReadOnly = true,
                Font = UiStyleHelper.BoldFont,
                TextAlign = HorizontalAlignment.Center,
                Margin = new Padding(0, 3, 8, 0),
                Text = BossHotkeyHelper.FormatHotkeyText(_capturedModifiers, _capturedVk)
            };
            _txtHotkeyCapture.KeyDown += OnCaptureBoxKeyDown;

            var btnResetDefault = UiStyleHelper.CreateButton("恢复默认 (Alt+Q)", (s, e) =>
            {
                _capturedModifiers = BossHotkeyHelper.MOD_ALT;
                _capturedVk = (int)Keys.Q;
                _txtHotkeyCapture.Text = BossHotkeyHelper.FormatHotkeyText(_capturedModifiers, _capturedVk);
            });

            pnlCaptureRow.Controls.AddRange(new Control[] { lblKeyTitle, _txtHotkeyCapture, btnResetDefault });

            _lblCaptureHint = new Label
            {
                Dock = DockStyle.Fill,
                Text = "提示：点击上方热键框后直接按下组合键即可修改（普通按键需搭配 Alt/Ctrl/Shift，F1~F12 可单按）。隐藏后任务栏与 Alt+Tab 均不留痕，再按同一热键立即唤起。",
                ForeColor = UiStyleHelper.TextMuted
            };

            _chkCloseToTray = new CheckBox
            {
                Text = "点击主窗口右上角 [×] 时隐藏至系统托盘继续挂机（可通过托盘菜单彻底退出）",
                Checked = cur.CloseButtonMinimizesToTray,
                AutoSize = true,
                Dock = DockStyle.Fill
            };

            _chkMinimalTitle = new CheckBox
            {
                Text = "使用极简办公窗口标题（仅显示 \"SIGTool\"，隐藏修仙副标题文案）",
                Checked = cur.UseMinimalWindowTitle,
                AutoSize = true,
                Dock = DockStyle.Fill
            };

            var pnlBottom = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 36,
                FlowDirection = FlowDirection.RightToLeft,
                Margin = new Padding(0)
            };

            var btnCancel = UiStyleHelper.CreateButton("取消", (s, e) =>
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            });

            var btnSave = UiStyleHelper.CreateButton("保存并生效", (s, e) =>
            {
                var settings = ClientSettingsService.Current;
                settings.EnableBossHotkey = _chkEnableHotkey.Checked;
                settings.BossHotkeyModifiers = _capturedModifiers;
                settings.BossHotkeyVirtualKey = _capturedVk;
                settings.CloseButtonMinimizesToTray = _chkCloseToTray.Checked;
                settings.UseMinimalWindowTitle = _chkMinimalTitle.Checked;
                ClientSettingsService.Save(settings);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }, UiButtonRole.PrimaryBlue);

            pnlBottom.Controls.AddRange(new Control[] { btnCancel, btnSave });

            mainLayout.Controls.Add(_chkEnableHotkey, 0, 0);
            mainLayout.Controls.Add(pnlCaptureRow, 0, 1);
            mainLayout.Controls.Add(_lblCaptureHint, 0, 2);
            mainLayout.Controls.Add(_chkCloseToTray, 0, 3);
            mainLayout.Controls.Add(_chkMinimalTitle, 0, 4);
            mainLayout.Controls.Add(pnlBottom, 0, 5);

            this.Controls.Add(mainLayout);
        }

        private void OnCaptureBoxKeyDown(object sender, KeyEventArgs e)
        {
            e.SuppressKeyPress = true;
            e.Handled = true;

            Keys key = e.KeyCode;
            if (key == Keys.ControlKey || key == Keys.ShiftKey || key == Keys.Menu ||
                key == Keys.LWin || key == Keys.RWin)
            {
                return;
            }

            int mods = 0;
            if (e.Alt) mods |= BossHotkeyHelper.MOD_ALT;
            if (e.Control) mods |= BossHotkeyHelper.MOD_CONTROL;
            if (e.Shift) mods |= BossHotkeyHelper.MOD_SHIFT;

            bool isFnKey = (key >= Keys.F1 && key <= Keys.F12);
            if (mods == 0 && !isFnKey)
            {
                mods = BossHotkeyHelper.MOD_ALT;
            }

            _capturedModifiers = mods;
            _capturedVk = (int)key;
            _txtHotkeyCapture.Text = BossHotkeyHelper.FormatHotkeyText(_capturedModifiers, _capturedVk);
        }

        public void ApplyTheme()
        {
            var p = UiStyleHelper.Current;
            if (_lblCaptureHint != null) _lblCaptureHint.ForeColor = p.TextMuted;
            if (_txtHotkeyCapture != null)
            {
                _txtHotkeyCapture.BackColor = p.BgSubtle;
                _txtHotkeyCapture.ForeColor = p.AccentBlue;
            }
        }
    }

    public static class BossHotkeyHelper
    {
        public const int WM_HOTKEY = 0x0312;
        public const int HOTKEY_ID_BOSS = 0x5167;

        public const int MOD_ALT = 0x0001;
        public const int MOD_CONTROL = 0x0002;
        public const int MOD_SHIFT = 0x0004;
        public const int MOD_NOREPEAT = 0x4000;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        public static bool RegisterBossKey(IntPtr hWnd, int modifiers, int vk)
        {
            if (hWnd == IntPtr.Zero || vk <= 0) return false;
            UnregisterHotKey(hWnd, HOTKEY_ID_BOSS);

            uint flags = (uint)(modifiers & 0x07) | MOD_NOREPEAT;
            if (RegisterHotKey(hWnd, HOTKEY_ID_BOSS, flags, (uint)vk))
            {
                return true;
            }
            return RegisterHotKey(hWnd, HOTKEY_ID_BOSS, (uint)(modifiers & 0x07), (uint)vk);
        }

        public static void UnregisterBossKey(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return;
            UnregisterHotKey(hWnd, HOTKEY_ID_BOSS);
        }

        public static void ForceBringToFront(Form form)
        {
            if (form == null || form.IsDisposed) return;
            if (form.Handle != IntPtr.Zero)
            {
                SetForegroundWindow(form.Handle);
            }
            form.Activate();
            form.BringToFront();
            form.Focus();
        }

        public static string FormatHotkeyText(int modifiers, int vk)
        {
            var sb = new StringBuilder();
            if ((modifiers & MOD_CONTROL) != 0) sb.Append("Ctrl+");
            if ((modifiers & MOD_ALT) != 0) sb.Append("Alt+");
            if ((modifiers & MOD_SHIFT) != 0) sb.Append("Shift+");

            Keys k = (Keys)vk;
            string keyName = k.ToString();
            if (k >= Keys.D0 && k <= Keys.D9)
            {
                keyName = ((int)(k - Keys.D0)).ToString();
            }
            sb.Append(keyName);
            return sb.ToString();
        }

        /// <summary>
        /// 生成纯中性灰度 16x16 静态托盘图标（严格 R == G == B，彻底无色彩干扰）
        /// </summary>
        public static Icon CreateStaticCleanTrayIcon()
        {
            using (var bmp = new Bitmap(16, 16))
            {
                using (var g = Graphics.FromImage(bmp))
                using (var bgBrush = new SolidBrush(Color.FromArgb(64, 64, 64)))
                using (var borderPen = new Pen(Color.FromArgb(164, 164, 164), 1f))
                using (var lineTop = new Pen(Color.FromArgb(238, 238, 238), 2f))
                using (var lineBot = new Pen(Color.FromArgb(172, 172, 172), 2f))
                {
                    g.Clear(Color.Transparent);
                    g.FillRectangle(bgBrush, 1, 1, 14, 14);
                    g.DrawRectangle(borderPen, 1, 1, 13, 13);
                    g.DrawLine(lineTop, 4, 6, 12, 6);
                    g.DrawLine(lineBot, 4, 10, 10, 10);
                }

                IntPtr hIcon = bmp.GetHicon();
                try
                {
                    return (Icon)Icon.FromHandle(hIcon).Clone();
                }
                finally
                {
                    DestroyIcon(hIcon);
                }
            }
        }
    }
}
