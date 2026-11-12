using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using SIGTool.Datas;
using SIGTool.Forms.UIs;
using SIGTool.Modules.Core;
using SIGTool.Modules.Logics;

namespace SIGTool.Forms
{
    /// <summary>
    /// SIG 主窗体（MainSIGForm v0.6）：
    /// • UI-UX-06：集成顶栏待办瓶颈胶囊速览条 UcAlertPinStrip（点击一键跳转目标页签并定位），并自动持久化记忆窗口尺寸与全部分栏拖拽比例；
    /// • EXT-02 纯灰度办公皮肤：深灰灰度 / 白底灰度严格 R==G==B 零彩色展示。
    /// </summary>
    public class MainSIGForm : Form, IThemeableView
    {
        private readonly SIGEngine _engine;
        private readonly Timer _uiTickTimer;
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private double _lastElapsedSec = 0.0;

        private TopResourceHeaderBar _headerBar;
        private UcAlertPinStrip _alertPinStrip;
        private TabControl _tabMain;
        private UcCultivationPanel _ucCultivation;
        private UcSectBuildPanel _ucSectBuild;
        private UcDiscipleCraftPanel _ucDiscipleCraft;
        private UcCombatExplorePanel _ucCombatExplore;
        private UcArtifactArrayPanel _ucArtifactArray;

        private UcLogConsole _ucLogConsole;
        private UcStatusFeedbackStrip _statusStrip;

        private FormStorageVault _vaultWindow;
        private FormCharacterSheet _charSheetWindow;
        private CombatDetailForm _combatPopupForm;
        private FormDevBalanceConsole _devConsoleWindow;

        private NotifyIcon _notifyIcon;
        private Icon _staticTrayIcon;
        private ContextMenuStrip _trayMenu;
        private ToolStripMenuItem _miToggleWindow;
        private ToolStripMenuItem _miThemeStandard;
        private ToolStripMenuItem _miThemeDarkGray;
        private ToolStripMenuItem _miThemeOfficeLight;

        private bool _isStealthHidden = false;
        private bool _hotkeyRegisteredOk = false;
        private bool _explicitExitRequested = false;
        private long _lastHotkeyToggleMs = -1000;

        private bool _restoreVaultOnWake = false;
        private bool _restoreCharSheetOnWake = false;
        private bool _restoreCombatOnWake = false;
        private bool _restoreDevConsoleOnWake = false;

        public MainSIGForm()
        {
            var clientSettings = ClientSettingsService.Load();
            UiStyleHelper.SetTheme(clientSettings.GetThemeModeEnum(), false);

            this.DoubleBuffered = true;
            this.KeyPreview = true;
            this.Size = new Size(clientSettings.WindowWidth, clientSettings.WindowHeight);
            this.MinimumSize = new Size(1020, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            if (clientSettings.IsWindowMaximized)
            {
                this.WindowState = FormWindowState.Maximized;
            }
            this.Font = UiStyleHelper.NormalFont;

            _engine = new SIGEngine();

            BuildMainLayout();
            InitializeSystemTray();
            BindEngineEvents();

            UiStyleHelper.OnThemeChanged += ApplyFullAppTheme;

            _engine.Initialize();
            ApplyFullAppTheme();
            UpdateWindowTitleAndHeaderUtilityLabels();
            RefreshActiveViews();

            _stopwatch.Start();
            _uiTickTimer = new Timer { Interval = 100 };
            _uiTickTimer.Tick += OnFrameTimerTick;
            _uiTickTimer.Start();
        }

        private void BuildMainLayout()
        {
            _headerBar = new TopResourceHeaderBar(
                _engine,
                OpenOrFocusVaultWindow,
                OpenOrFocusCombatWindow,
                CycleNextTheme,
                HideToSystemTray,
                OpenBossHotkeySettingsDialog,
                OpenOrFocusDevConsole);

            _alertPinStrip = new UcAlertPinStrip(HandleAlertJump);
            _statusStrip = new UcStatusFeedbackStrip(true);

            var splitWorkspace = UiStyleHelper.CreateProportionalSplit(
                Orientation.Horizontal, 0.78, 350, 110, "Main.Workspace");

            _tabMain = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = UiStyleHelper.BoldFont,
                Padding = new Point(12, 5),
                DrawMode = TabDrawMode.OwnerDrawFixed
            };
            _tabMain.DrawItem += OnTabMainDrawItem;

            var tab1 = new TabPage("1. 修仙吐纳 (掌门)");
            var tab2 = new TabPage("2. 宗门营造 (灵脉)");
            var tab3 = new TabPage("3. 门人百艺 (排班/丹器)");
            var tab4 = new TabPage("4. 秘境斗法 (历练/防卫)");
            var tab5 = new TabPage("5. 法宝大阵 (九宫/词条)");

            _ucCultivation = new UcCultivationPanel(_engine, OpenOrFocusCharSheetWindow);
            _ucSectBuild = new UcSectBuildPanel(_engine);
            _ucDiscipleCraft = new UcDiscipleCraftPanel(_engine, OpenOrFocusCharSheetWindow);
            _ucCombatExplore = new UcCombatExplorePanel(_engine, OpenOrFocusCombatWindow);
            _ucArtifactArray = new UcArtifactArrayPanel(_engine);

            tab1.Controls.Add(_ucCultivation);
            tab2.Controls.Add(_ucSectBuild);
            tab3.Controls.Add(_ucDiscipleCraft);
            tab4.Controls.Add(_ucCombatExplore);
            tab5.Controls.Add(_ucArtifactArray);

            _tabMain.TabPages.AddRange(new TabPage[] { tab1, tab2, tab3, tab4, tab5 });
            UiStyleHelper.SetToolTip(_tabMain, "快捷键切换页签：F1 修仙吐纳 / F2 宗门营造 / F3 门人百艺 / F4 秘境斗法 / F5 法宝大阵");
            _tabMain.SelectedIndexChanged += (s, e) =>
            {
                UpdateStatusShortcutHint();
                RefreshActiveViews();
            };

            _ucLogConsole = new UcLogConsole(_engine.EventBus);

            splitWorkspace.Panel1.Controls.Add(_tabMain);
            splitWorkspace.Panel2.Controls.Add(_ucLogConsole);

            this.Controls.Add(splitWorkspace);
            this.Controls.Add(_statusStrip);
            this.Controls.Add(_alertPinStrip);
            this.Controls.Add(_headerBar);

            UpdateStatusShortcutHint();
        }

        private void HandleAlertJump(AlertJumpRequest req)
        {
            if (req == null) return;
            if (req.TargetTabIndex >= 0 && req.TargetTabIndex < _tabMain.TabPages.Count)
            {
                _tabMain.SelectedIndex = req.TargetTabIndex;
            }

            switch (req.Kind)
            {
                case AlertTargetKind.DiscipleBreakthrough:
                    _ucDiscipleCraft.FocusDiscipleById(req.EntityId);
                    UiFeedbackService.PushInfo("已定位至满气海可破境门人（右键该行可立即冲击破境）。");
                    break;
                case AlertTargetKind.IdleWorkshop:
                    _ucDiscipleCraft.FocusWorkshopBySlot(req.SubIndex);
                    UiFeedbackService.PushInfo(string.Format("已定位至停炉工坊（地块 #{0}，双击或右键行即可装填配方）。", req.SubIndex + 1));
                    break;
                case AlertTargetKind.EmptyVeinSlot:
                    if (req.SubIndex >= 0) _ucSectBuild.FocusSlot(req.SubIndex);
                    UiFeedbackService.PushInfo(string.Format("已定位至空置灵脉地块 #{0}（右键地块或双击右侧建筑格即可动工）。", req.SubIndex + 1));
                    break;
                case AlertTargetKind.EmptyFormationEye:
                    if (req.SubIndex >= 0) _ucArtifactArray.FocusEye(req.SubIndex);
                    UiFeedbackService.PushInfo("已定位至空置护山阵眼（双击右侧词条法宝即可镇入）。");
                    break;
                case AlertTargetKind.LeaderBreakthrough:
                    UiFeedbackService.PushInfo("已切至掌门修行台，点击【★ 冲击破境】即可晋升。");
                    break;
                case AlertTargetKind.IdleExpedition:
                    UiFeedbackService.PushInfo("已切至秘境斗法台，双击任意已解锁秘境即可出征。");
                    break;
                case AlertTargetKind.LowFoodWarning:
                    UiFeedbackService.PushWarning("门人口粮偏低，建议增建【神农灵田】或改派门人前往百草灵农。");
                    break;
            }
            RefreshActiveViews();
        }

        private void OnTabMainDrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _tabMain.TabPages.Count) return;
            var p = UiStyleHelper.Current;
            bool selected = (e.Index == _tabMain.SelectedIndex);
            Rectangle rect = e.Bounds;

            using (var bgBrush = new SolidBrush(selected ? p.TabActiveBg : p.TabInactiveBg))
            {
                e.Graphics.FillRectangle(bgBrush, rect);
            }

            if (selected)
            {
                using (var accentPen = new Pen(p.AccentBlue, 2f))
                {
                    e.Graphics.DrawLine(accentPen, rect.Left + 2, rect.Top + 1, rect.Right - 2, rect.Top + 1);
                }
            }

            Color fg = selected ? p.TextPrimary : p.TextMuted;
            TextRenderer.DrawText(
                e.Graphics,
                _tabMain.TabPages[e.Index].Text,
                selected ? UiStyleHelper.BoldFont : UiStyleHelper.NormalFont,
                rect,
                fg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }

        private void InitializeSystemTray()
        {
            _staticTrayIcon = BossHotkeyHelper.CreateStaticCleanTrayIcon();

            _trayMenu = UiStyleHelper.CreateThemedContextMenu();

            _miToggleWindow = new ToolStripMenuItem("显示 / 隐藏窗口", null, (s, e) => ToggleStealthHideOrWake());
            _miThemeStandard = new ToolStripMenuItem("皮肤：标准暗蓝", null, (s, e) => SwitchToSpecificTheme(UiThemeMode.StandardSlate));
            _miThemeDarkGray = new ToolStripMenuItem("皮肤：深灰灰度 (纯灰度办公)", null, (s, e) => SwitchToSpecificTheme(UiThemeMode.MinimalDarkGray));
            _miThemeOfficeLight = new ToolStripMenuItem("皮肤：白底灰度 (纯灰度办公)", null, (s, e) => SwitchToSpecificTheme(UiThemeMode.CleanOfficeLight));

            var miHotkeyCfg = new ToolStripMenuItem("老板键与托盘设置...", null, (s, e) =>
            {
                WakeFromSystemTray();
                OpenBossHotkeySettingsDialog();
            });

            var miDevConsole = new ToolStripMenuItem("开发者数值平衡测试台 (F12)", null, (s, e) =>
            {
                WakeFromSystemTray();
                OpenOrFocusDevConsole();
            });

            var miQuickSave = new ToolStripMenuItem("立即保存存档 (Ctrl+S)", null, (s, e) =>
            {
                string msg;
                bool ok = _engine.SaveSIG(out msg);
                UiFeedbackService.ReportAction(ok, msg);
            });

            var miExit = new ToolStripMenuItem("保存并彻底退出", null, (s, e) => RequestExplicitExit());

            _trayMenu.Items.AddRange(new ToolStripItem[]
            {
                _miToggleWindow,
                new ToolStripSeparator(),
                _miThemeStandard,
                _miThemeDarkGray,
                _miThemeOfficeLight,
                new ToolStripSeparator(),
                miHotkeyCfg,
                miDevConsole,
                miQuickSave,
                new ToolStripSeparator(),
                miExit
            });

            _notifyIcon = new NotifyIcon
            {
                Icon = _staticTrayIcon,
                Text = "SIGTool",
                ContextMenuStrip = _trayMenu,
                Visible = true
            };

            _notifyIcon.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ToggleStealthHideOrWake();
                }
            };
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            RefreshGlobalBossHotkeyRegistration();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            BossHotkeyHelper.UnregisterBossKey(this.Handle);
            base.OnHandleDestroyed(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == BossHotkeyHelper.WM_HOTKEY && m.WParam.ToInt32() == BossHotkeyHelper.HOTKEY_ID_BOSS)
            {
                long nowMs = _stopwatch.ElapsedMilliseconds;
                if (nowMs - _lastHotkeyToggleMs >= 180)
                {
                    _lastHotkeyToggleMs = nowMs;
                    ToggleStealthHideOrWake();
                }
                return;
            }
            base.WndProc(ref m);
        }

        private void RefreshGlobalBossHotkeyRegistration()
        {
            if (!this.IsHandleCreated) return;
            var s = ClientSettingsService.Current;
            BossHotkeyHelper.UnregisterBossKey(this.Handle);

            if (s.EnableBossHotkey)
            {
                _hotkeyRegisteredOk = BossHotkeyHelper.RegisterBossKey(this.Handle, s.BossHotkeyModifiers, s.BossHotkeyVirtualKey);
            }
            else
            {
                _hotkeyRegisteredOk = false;
            }
            UpdateWindowTitleAndHeaderUtilityLabels();
        }

        public void ToggleStealthHideOrWake()
        {
            if (_isStealthHidden || !this.Visible || this.WindowState == FormWindowState.Minimized)
            {
                WakeFromSystemTray();
            }
            else
            {
                HideToSystemTray();
            }
        }

        public void HideToSystemTray()
        {
            if (_isStealthHidden) return;

            for (int i = Application.OpenForms.Count - 1; i >= 0; i--)
            {
                var openForm = Application.OpenForms[i];
                if (openForm != null && openForm != this && !openForm.IsDisposed && openForm.Modal)
                {
                    openForm.DialogResult = DialogResult.Cancel;
                    openForm.Close();
                }
            }

            _restoreVaultOnWake = (_vaultWindow != null && !_vaultWindow.IsDisposed && _vaultWindow.Visible);
            _restoreCharSheetOnWake = (_charSheetWindow != null && !_charSheetWindow.IsDisposed && _charSheetWindow.Visible);
            _restoreCombatOnWake = (_combatPopupForm != null && !_combatPopupForm.IsDisposed && _combatPopupForm.Visible);
            _restoreDevConsoleOnWake = (_devConsoleWindow != null && !_devConsoleWindow.IsDisposed && _devConsoleWindow.Visible);

            if (_restoreVaultOnWake) _vaultWindow.Hide();
            if (_restoreCharSheetOnWake) _charSheetWindow.Hide();
            if (_restoreCombatOnWake) _combatPopupForm.Hide();
            if (_restoreDevConsoleOnWake) _devConsoleWindow.Hide();

            _isStealthHidden = true;
            this.ShowInTaskbar = false;
            this.Hide();
        }

        public void WakeFromSystemTray()
        {
            _isStealthHidden = false;
            this.ShowInTaskbar = true;
            if (!this.Visible)
            {
                this.Show();
            }
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.WindowState = FormWindowState.Normal;
            }

            if (_restoreVaultOnWake && _vaultWindow != null && !_vaultWindow.IsDisposed)
            {
                _vaultWindow.Show(this);
            }
            if (_restoreCharSheetOnWake && _charSheetWindow != null && !_charSheetWindow.IsDisposed)
            {
                _charSheetWindow.Show(this);
            }
            if (_restoreCombatOnWake && _combatPopupForm != null && !_combatPopupForm.IsDisposed)
            {
                _combatPopupForm.Show(this);
            }
            if (_restoreDevConsoleOnWake && _devConsoleWindow != null && !_devConsoleWindow.IsDisposed)
            {
                _devConsoleWindow.Show(this);
            }

            BossHotkeyHelper.ForceBringToFront(this);
            RefreshActiveViews();
        }

        private void CycleNextTheme()
        {
            var next = UiStyleHelper.GetNextThemeMode(UiStyleHelper.Current.Mode);
            SwitchToSpecificTheme(next);
            UiFeedbackService.PushInfo("已切换界面皮肤为：" + UiStyleHelper.GetThemeShortCn(next));
        }

        private void SwitchToSpecificTheme(UiThemeMode targetMode)
        {
            var settings = ClientSettingsService.Current;
            settings.SetThemeModeEnum(targetMode);
            ClientSettingsService.Save(settings);
            UiStyleHelper.SetTheme(targetMode, true);
        }

        private void OpenBossHotkeySettingsDialog()
        {
            using (var dlg = new FormTrayHotkeySettings())
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    RefreshGlobalBossHotkeyRegistration();
                    if (ClientSettingsService.Current.EnableBossHotkey && !_hotkeyRegisteredOk)
                    {
                        string keyTxt = BossHotkeyHelper.FormatHotkeyText(
                            ClientSettingsService.Current.BossHotkeyModifiers,
                            ClientSettingsService.Current.BossHotkeyVirtualKey);
                        UiFeedbackService.PushWarning(string.Format("热键 [{0}] 已被系统或其他程序占用，建议打开老板键设置更换组合键。", keyTxt));
                    }
                    else
                    {
                        UiFeedbackService.PushSuccess("已保存托盘与老板键偏好设置。");
                    }
                }
            }
        }

        private void ApplyFullAppTheme()
        {
            if (this.IsDisposed) return;

            this.SuspendLayout();
            UiStyleHelper.ApplyThemeRecursive(this);
            if (_trayMenu != null) UiStyleHelper.StyleContextMenu(_trayMenu);
            _tabMain.Invalidate();
            this.ResumeLayout();

            if (_vaultWindow != null && !_vaultWindow.IsDisposed)
            {
                UiStyleHelper.ApplyThemeRecursive(_vaultWindow);
            }
            if (_charSheetWindow != null && !_charSheetWindow.IsDisposed)
            {
                UiStyleHelper.ApplyThemeRecursive(_charSheetWindow);
            }
            if (_combatPopupForm != null && !_combatPopupForm.IsDisposed)
            {
                UiStyleHelper.ApplyThemeRecursive(_combatPopupForm);
            }
            if (_devConsoleWindow != null && !_devConsoleWindow.IsDisposed)
            {
                UiStyleHelper.ApplyThemeRecursive(_devConsoleWindow);
            }

            UpdateWindowTitleAndHeaderUtilityLabels();
            RefreshActiveViews();
        }

        public void ApplyTheme()
        {
            this.BackColor = UiStyleHelper.Current.BgWindow;
        }

        private void UpdateWindowTitleAndHeaderUtilityLabels()
        {
            var s = ClientSettingsService.Current;
            string hotkeyStr = BossHotkeyHelper.FormatHotkeyText(s.BossHotkeyModifiers, s.BossHotkeyVirtualKey);

            string targetTitle = s.UseMinimalWindowTitle
                ? "SIGTool"
                : "SIGTool v0.7 - 挂机修仙 · 宗门营造 · 秘境斗法 · 九宫阵宝 (.NET Framework 4.7.2)";
            if (this.Text != targetTitle) this.Text = targetTitle;

            if (_headerBar != null)
            {
                _headerBar.UpdateDesktopUtilityLabels(hotkeyStr, _hotkeyRegisteredOk);
            }

            if (_miToggleWindow != null)
            {
                _miToggleWindow.Text = s.EnableBossHotkey
                    ? string.Format("显示 / 隐藏主窗口 ({0})", hotkeyStr)
                    : "显示 / 隐藏主窗口";
            }
            if (_miThemeStandard != null) _miThemeStandard.Checked = (UiStyleHelper.Current.Mode == UiThemeMode.StandardSlate);
            if (_miThemeDarkGray != null) _miThemeDarkGray.Checked = (UiStyleHelper.Current.Mode == UiThemeMode.MinimalDarkGray);
            if (_miThemeOfficeLight != null) _miThemeOfficeLight.Checked = (UiStyleHelper.Current.Mode == UiThemeMode.CleanOfficeLight);
        }

        private void BindEngineEvents()
        {
            _engine.EventBus.OnStateChanged += () =>
            {
                if (this.IsDisposed) return;
                if (!_isStealthHidden)
                {
                    RefreshActiveViews();
                }
                if (_vaultWindow != null && !_vaultWindow.IsDisposed && _vaultWindow.Visible)
                {
                    _vaultWindow.RefreshVaultItems();
                }
                if (_charSheetWindow != null && !_charSheetWindow.IsDisposed && _charSheetWindow.Visible)
                {
                    _charSheetWindow.RefreshSheet();
                }
                if (_devConsoleWindow != null && !_devConsoleWindow.IsDisposed && _devConsoleWindow.Visible)
                {
                    _devConsoleWindow.RefreshBaselineInfo();
                }
            };

            _engine.EventBus.OnOfflineReportReady += summary =>
            {
                UiFeedbackService.PushSuccess(string.Format(
                    "闭关归来：离线推演 {0:F0} 秒（+{1} 仙历月），掌门修为 +{2:F0}，秘境胜场 +{3}（详情已记入纪事录）。",
                    summary.EffectiveSimulatedSeconds, summary.MonthsAdvanced, summary.LeaderQiGained, summary.ExpeditionWins));
            };
        }

        private void OnFrameTimerTick(object sender, EventArgs e)
        {
            double nowSec = _stopwatch.Elapsed.TotalSeconds;
            double dt = nowSec - _lastElapsedSec;
            _lastElapsedSec = nowSec;

            _engine.Tick(dt);

            if (!_isStealthHidden && this.WindowState != FormWindowState.Minimized)
            {
                RefreshActiveViews();
            }
        }

        private void RefreshActiveViews()
        {
            if (_isStealthHidden) return;
            _headerBar.RefreshHeader();
            _alertPinStrip.RefreshAlerts(_engine.Save);

            switch (_tabMain.SelectedIndex)
            {
                case 0:
                    _ucCultivation.RefreshView();
                    break;
                case 1:
                    _ucSectBuild.RefreshView();
                    break;
                case 2:
                    _ucDiscipleCraft.RefreshView();
                    break;
                case 3:
                    _ucCombatExplore.RefreshView();
                    break;
                case 4:
                    _ucArtifactArray.RefreshView();
                    break;
            }

            if (_combatPopupForm != null && !_combatPopupForm.IsDisposed && _combatPopupForm.Visible)
            {
                _combatPopupForm.RefreshView();
            }
        }

        private void OpenOrFocusVaultWindow()
        {
            if (_vaultWindow == null || _vaultWindow.IsDisposed)
            {
                _vaultWindow = new FormStorageVault(_engine, _engine.Save.Leader, VaultFilterMode.All, false);
                _vaultWindow.Show(this);
            }
            else
            {
                if (!_vaultWindow.Visible) _vaultWindow.Show(this);
                _vaultWindow.RefreshVaultItems();
                _vaultWindow.BringToFront();
                _vaultWindow.Focus();
            }
        }

        private void OpenOrFocusCharSheetWindow(CharacterData ch)
        {
            if (ch == null) ch = _engine.Save.Leader;
            if (_charSheetWindow == null || _charSheetWindow.IsDisposed)
            {
                _charSheetWindow = new FormCharacterSheet(_engine, ch);
                _charSheetWindow.Show(this);
            }
            else
            {
                if (!_charSheetWindow.Visible) _charSheetWindow.Show(this);
                _charSheetWindow.SetCharacter(ch);
                _charSheetWindow.BringToFront();
                _charSheetWindow.Focus();
            }
        }

        private void OpenOrFocusCombatWindow()
        {
            if (_combatPopupForm == null || _combatPopupForm.IsDisposed)
            {
                _combatPopupForm = new CombatDetailForm(_engine);
                _combatPopupForm.Show(this);
            }
            else
            {
                if (!_combatPopupForm.Visible) _combatPopupForm.Show(this);
                _combatPopupForm.BringToFront();
                _combatPopupForm.Focus();
            }
        }

        private void OpenOrFocusDevConsole()
        {
            if (_devConsoleWindow == null || _devConsoleWindow.IsDisposed)
            {
                _devConsoleWindow = new FormDevBalanceConsole(_engine);
                _devConsoleWindow.Show(this);
            }
            else
            {
                if (!_devConsoleWindow.Visible) _devConsoleWindow.Show(this);
                _devConsoleWindow.RefreshBaselineInfo();
                _devConsoleWindow.BringToFront();
                _devConsoleWindow.Focus();
            }
        }

        private void UpdateStatusShortcutHint()
        {
            if (_statusStrip == null || _tabMain == null) return;
            string tabHint;
            switch (_tabMain.SelectedIndex)
            {
                case 0:
                    tabHint = "F1-F5:切页 | 1-3:服丹 P:破境 | ←↑↓→/Enter:选功法 | Space:暂停 B:储物 C:属性 F12:测试台";
                    break;
                case 1:
                    tabHint = "F1-F5:切页 | ←↑↓→/Enter:选建筑(Ctrl+方向键:选灵脉) | Space:暂停 B:储物 F12:测试台";
                    break;
                case 2:
                    tabHint = "F1-F5:切页 | R:招募 M:百艺总谱 | Space:暂停 1-3:服丹 B:储物 C:属性 F12:测试台";
                    break;
                case 3:
                    tabHint = "F1-F5:切页 | Space:出征/撤退 1-3:战丹 Q/E:神通 | ←↑↓→/Enter:选秘境 | V:战报 F12:测试台";
                    break;
                case 4:
                    tabHint = "F1-F5:切页 | ←↑↓→/Enter:选法宝(Ctrl+方向键:选阵眼) | Space:暂停 B:储物 F12:测试台";
                    break;
                default:
                    tabHint = "F1-F5:切页 | 1-3:服丹 | Space:暂停 | B:储物 C:属性 F12:测试台";
                    break;
            }
            _statusStrip.SetRightHint(tabHint);
        }

        /// <summary>
        /// EXT-03 全键盘快捷键操作流（统一拦截并抑制系统默认提示音，自动避让文本输入框焦点）
        /// </summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // 1. 无论焦点何在均可响应的功能键与 Ctrl 组合热键
            if (keyData == Keys.F12)
            {
                OpenOrFocusDevConsole();
                return true;
            }
            if (keyData >= Keys.F1 && keyData <= Keys.F5)
            {
                int targetTab = (int)(keyData - Keys.F1);
                if (targetTab >= 0 && targetTab < _tabMain.TabPages.Count)
                {
                    _tabMain.SelectedIndex = targetTab;
                }
                return true;
            }
            if (keyData == (Keys.Control | Keys.S))
            {
                string m;
                bool ok = _engine.SaveSIG(out m);
                UiFeedbackService.ReportAction(ok, m);
                return true;
            }
            if (keyData == (Keys.Control | Keys.L))
            {
                string m;
                bool ok = _engine.LoadSIGSlot(_engine.CurrentSaveSlot, out m);
                UiFeedbackService.ReportAction(ok, m);
                return true;
            }
            if (keyData == (Keys.Control | Keys.R))
            {
                string m;
                bool ok = _engine.ReloadConfig(out m);
                UiFeedbackService.ReportAction(ok, m);
                return true;
            }

            // 2. Esc：若输入框聚焦则移出焦点；否则依次关闭顶层非模态子窗口
            if (keyData == Keys.Escape)
            {
                if (UiStyleHelper.IsTextInputFocused(this.ActiveControl))
                {
                    _tabMain.Focus();
                    return true;
                }
                if (_devConsoleWindow != null && !_devConsoleWindow.IsDisposed && _devConsoleWindow.Visible)
                {
                    _devConsoleWindow.Close();
                    return true;
                }
                if (_combatPopupForm != null && !_combatPopupForm.IsDisposed && _combatPopupForm.Visible)
                {
                    _combatPopupForm.Close();
                    return true;
                }
                if (_charSheetWindow != null && !_charSheetWindow.IsDisposed && _charSheetWindow.Visible)
                {
                    _charSheetWindow.Close();
                    return true;
                }
                if (_vaultWindow != null && !_vaultWindow.IsDisposed && _vaultWindow.Visible)
                {
                    _vaultWindow.Close();
                    return true;
                }
                return false;
            }

            // 3. 若当前正在搜索框/下拉框等输入组件中打字，则放行全部单字母与数字键
            if (UiStyleHelper.IsTextInputFocused(this.ActiveControl))
            {
                return base.ProcessCmdKey(ref msg, keyData);
            }

            // 4. 数字键 1 / 2 / 3：秘境斗法页或交战中优先服用战丹槽 1~3，其余页服用掌门修行丹药槽 1~3
            if (keyData == Keys.D1 || keyData == Keys.NumPad1 ||
                keyData == Keys.D2 || keyData == Keys.NumPad2 ||
                keyData == Keys.D3 || keyData == Keys.NumPad3)
            {
                int slotIdx = (keyData == Keys.D1 || keyData == Keys.NumPad1) ? 0
                            : ((keyData == Keys.D2 || keyData == Keys.NumPad2) ? 1 : 2);
                if (_tabMain.SelectedIndex == 3 || (_engine.Save != null && _engine.Save.Combat.IsInBattle))
                {
                    _ucCombatExplore.TriggerCombatPillByHotkey(slotIdx);
                }
                else
                {
                    _ucCultivation.TriggerQuickPillByHotkey(slotIdx);
                }
                RefreshActiveViews();
                return true;
            }

            // 5. Space：斗法页切换【出征选中秘境 / 撤回山门】，其余页切换【全局暂停 0x / 恢复原流速】
            if (keyData == Keys.Space)
            {
                if (_tabMain.SelectedIndex == 3)
                {
                    _ucCombatExplore.ToggleExpeditionOrRetreatByHotkey();
                }
                else
                {
                    int spd = _engine.TogglePauseOrResume();
                    UiFeedbackService.PushInfo(spd == 0
                        ? "已暂停宗门时间推演 (0x)。"
                        : string.Format("已恢复宗门时间推演 ({0}x)。", spd));
                }
                RefreshActiveViews();
                return true;
            }

            // 6. [ 与 ] 键：快捷降速 / 升速档位（0x ↔ 1x ↔ 2x ↔ 5x）
            if (keyData == Keys.OemOpenBrackets)
            {
                int spd = _engine.StepSpeedGear(-1);
                UiFeedbackService.PushInfo(string.Format("时间流速已调至 {0}x。", spd));
                RefreshActiveViews();
                return true;
            }
            if (keyData == Keys.OemCloseBrackets)
            {
                int spd = _engine.StepSpeedGear(1);
                UiFeedbackService.PushInfo(string.Format("时间流速已调至 {0}x。", spd));
                RefreshActiveViews();
                return true;
            }

            // 7. 通用单键窗口与工具热键（B=储物阁, C=掌门全属性, V=独立战报窗, T=切换皮肤）
            if (keyData == Keys.B)
            {
                OpenOrFocusVaultWindow();
                return true;
            }
            if (keyData == Keys.C)
            {
                OpenOrFocusCharSheetWindow(_engine.Save != null ? _engine.Save.Leader : null);
                return true;
            }
            if (keyData == Keys.V)
            {
                OpenOrFocusCombatWindow();
                return true;
            }
            if (keyData == Keys.T)
            {
                CycleNextTheme();
                return true;
            }

            // 8. 页签上下文专属单键（P=冲击破境, R=招募弟子, M=百艺总谱, Q/E=斗法神通）
            if (_tabMain.SelectedIndex == 0 && keyData == Keys.P)
            {
                _ucCultivation.TriggerBreakthroughByHotkey();
                return true;
            }
            if (_tabMain.SelectedIndex == 2 && keyData == Keys.R)
            {
                _ucDiscipleCraft.TriggerRecruitByHotkey();
                return true;
            }
            if (_tabMain.SelectedIndex == 2 && keyData == Keys.M)
            {
                _ucDiscipleCraft.OpenRecipeCompendiumByHotkey();
                return true;
            }
            if (_tabMain.SelectedIndex == 3 && keyData == Keys.Q)
            {
                _ucCombatExplore.TriggerTacticalSkillByHotkey("SKILL_TALISMAN");
                return true;
            }
            if (_tabMain.SelectedIndex == 3 && keyData == Keys.E)
            {
                _ucCombatExplore.TriggerTacticalSkillByHotkey("SKILL_QI_BURST");
                return true;
            }

            // 9. 紧凑格子键盘方向导航与回车/菜单键执行（当前焦点不在 DataGridView 表格上时生效）
            Control leaf = UiStyleHelper.UnwrapActiveLeafControl(this.ActiveControl);
            if (!(leaf is DataGridView))
            {
                Keys baseKey = keyData & Keys.KeyCode;
                if (baseKey == Keys.Left || baseKey == Keys.Right || baseKey == Keys.Up || baseKey == Keys.Down ||
                    keyData == Keys.Enter || keyData == Keys.Apps || keyData == (Keys.F10 | Keys.Shift))
                {
                    bool handled = false;
                    switch (_tabMain.SelectedIndex)
                    {
                        case 0: handled = _ucCultivation.HandleKeyboardNavigation(keyData); break;
                        case 1: handled = _ucSectBuild.HandleKeyboardNavigation(keyData); break;
                        case 3: handled = _ucCombatExplore.HandleKeyboardNavigation(keyData); break;
                        case 4: handled = _ucArtifactArray.HandleKeyboardNavigation(keyData); break;
                    }
                    if (handled) return true;
                }
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void RequestExplicitExit()
        {
            _explicitExitRequested = true;
            this.Close();
        }

        private void PersistWindowLayoutSettings()
        {
            var s = ClientSettingsService.Current;
            if (s == null) return;
            s.IsWindowMaximized = (this.WindowState == FormWindowState.Maximized);
            if (this.WindowState == FormWindowState.Normal)
            {
                s.WindowWidth = this.Width;
                s.WindowHeight = this.Height;
            }
            else if (this.RestoreBounds.Width >= 1000 && this.RestoreBounds.Height >= 660)
            {
                s.WindowWidth = this.RestoreBounds.Width;
                s.WindowHeight = this.RestoreBounds.Height;
            }
            ClientSettingsService.Save(s);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing &&
                !_explicitExitRequested &&
                ClientSettingsService.Current.CloseButtonMinimizesToTray)
            {
                PersistWindowLayoutSettings();
                e.Cancel = true;
                HideToSystemTray();
                return;
            }

            PersistWindowLayoutSettings();

            if (_uiTickTimer != null) _uiTickTimer.Stop();
            UiStyleHelper.OnThemeChanged -= ApplyFullAppTheme;
            BossHotkeyHelper.UnregisterBossKey(this.Handle);

            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }
            if (_trayMenu != null)
            {
                _trayMenu.Dispose();
                _trayMenu = null;
            }
            if (_staticTrayIcon != null)
            {
                _staticTrayIcon.Dispose();
                _staticTrayIcon = null;
            }

            string saveMsg;
            _engine.SaveSIG(out saveMsg);
            base.OnFormClosing(e);
        }
    }
}
