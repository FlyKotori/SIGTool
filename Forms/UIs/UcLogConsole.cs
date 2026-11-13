using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using SIGTool.Modules.Core;

namespace SIGTool.Forms.UIs
{
    /// <summary>
    /// UI-UX-04：紧凑版分类过滤与锁屏纪事录控制台（紧凑小按钮、无彩色 Emoji）
    /// </summary>
    public class UcLogConsole : UserControl, IThemeableView
    {
        private enum LogFilterTab
        {
            All = 0,
            Cultivation = 1,
            SectAndCraft = 2,
            Combat = 3,
            Artifact = 4,
            System = 5
        }

        private readonly SIGEventBus _eventBus;

        private GroupBox _grpContainer;
        private readonly Dictionary<LogFilterTab, Button> _filterButtons = new Dictionary<LogFilterTab, Button>();
        private TextBox _txtKeyword;
        private Button _btnPauseScroll;
        private TextBox _txtLogs;

        private LogFilterTab _activeFilter = LogFilterTab.All;
        private bool _isScrollLocked = false;
        private int _bufferedWhileLocked = 0;

        public UcLogConsole(SIGEventBus eventBus)
        {
            _eventBus = eventBus;
            this.DoubleBuffered = true;
            this.Dock = DockStyle.Fill;

            BuildUI();
            BindBusEvents();
            ApplyTheme();
            RebuildFilteredView();
        }

        private void BuildUI()
        {
            _grpContainer = new GroupBox
            {
                Text = "【宗门天机纪事录】",
                Dock = DockStyle.Fill,
                Font = UiStyleHelper.BoldFont,
                Padding = new Padding(6, 2, 6, 5)
            };

            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0)
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var bar = UiStyleHelper.CreateCleanToolbarStrip(DockStyle.Fill, 34);

            AddFilterButton(bar, LogFilterTab.All, "全部");
            AddFilterButton(bar, LogFilterTab.Cultivation, "修仙");
            AddFilterButton(bar, LogFilterTab.SectAndCraft, "经营/百艺");
            AddFilterButton(bar, LogFilterTab.Combat, "斗法");
            AddFilterButton(bar, LogFilterTab.Artifact, "阵宝");
            AddFilterButton(bar, LogFilterTab.System, "天机");

            var lblSearch = new Label
            {
                AutoSize = true,
                Font = UiStyleHelper.SmallFont,
                Margin = new Padding(8, 4, 2, 0),
                Text = "检索:"
            };

            _txtKeyword = new TextBox
            {
                Width = 96,
                Font = UiStyleHelper.SmallFont,
                Margin = new Padding(2, 2, 6, 1)
            };
            _txtKeyword.TextChanged += (s, e) => RebuildFilteredView();

            _btnPauseScroll = UiStyleHelper.CreateButton("锁屏阅读", (s, e) => ToggleScrollLock(), UiButtonRole.Default, true);

            var btnClear = UiStyleHelper.CreateButton("清空", (s, e) =>
            {
                _bufferedWhileLocked = 0;
                if (_eventBus != null) _eventBus.ClearLogs();
                UiFeedbackService.PushInfo("已清空当前天机纪事录。");
            }, UiButtonRole.Default, true);

            bar.Controls.AddRange(new Control[] { lblSearch, _txtKeyword, _btnPauseScroll, btnClear });

            _txtLogs = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = UiStyleHelper.MonoFont
            };

            tbl.Controls.Add(bar, 0, 0);
            tbl.Controls.Add(_txtLogs, 0, 1);
            _grpContainer.Controls.Add(tbl);
            this.Controls.Add(_grpContainer);
        }

        private void AddFilterButton(FlowLayoutPanel bar, LogFilterTab tab, string label)
        {
            var btn = UiStyleHelper.CreateButton(label, (s, e) =>
            {
                _activeFilter = tab;
                RefreshFilterHighlights();
                RebuildFilteredView();
            }, UiButtonRole.Default, true);
            _filterButtons[tab] = btn;
            bar.Controls.Add(btn);
        }

        private void BindBusEvents()
        {
            if (_eventBus == null) return;
            _eventBus.OnLogAdded += HandleLogAdded;
            _eventBus.OnLogsCleared += HandleLogsCleared;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _eventBus != null)
            {
                _eventBus.OnLogAdded -= HandleLogAdded;
                _eventBus.OnLogsCleared -= HandleLogsCleared;
            }
            base.Dispose(disposing);
        }

        private void ToggleScrollLock()
        {
            _isScrollLocked = !_isScrollLocked;
            if (!_isScrollLocked)
            {
                _bufferedWhileLocked = 0;
                UpdateLockButtonVisual();
                RebuildFilteredView();
                UiFeedbackService.PushInfo("已恢复纪事录实时滚动。");
            }
            else
            {
                UpdateLockButtonVisual();
                UiFeedbackService.PushInfo("已锁定纪事录滚动（再次点击恢复实时刷新）。");
            }
        }

        private void UpdateLockButtonVisual()
        {
            if (_btnPauseScroll == null) return;
            var p = UiStyleHelper.Current;
            if (_isScrollLocked)
            {
                _btnPauseScroll.Text = _bufferedWhileLocked > 0
                    ? string.Format("▶ 锁屏中(+{0})", _bufferedWhileLocked)
                    : "▶ 锁屏中";
                _btnPauseScroll.BackColor = p.SpeedActiveBg;
                _btnPauseScroll.ForeColor = p.SpeedActiveFg;
                _btnPauseScroll.FlatAppearance.BorderColor = p.SpeedActiveBg;
            }
            else
            {
                _btnPauseScroll.Text = "锁屏阅读";
                UiStyleHelper.ApplyButtonRoleVisuals(_btnPauseScroll, UiButtonRole.Default);
            }
        }

        private void HandleLogsCleared()
        {
            if (this.IsDisposed) return;
            _txtLogs.Clear();
            UpdateLockButtonVisual();
        }

        private void HandleLogAdded(SIGLogEntry entry)
        {
            if (this.IsDisposed || entry == null) return;
            if (!MatchesFilter(entry)) return;

            if (_isScrollLocked)
            {
                _bufferedWhileLocked++;
                UpdateLockButtonVisual();
                return;
            }

            if (_txtLogs.TextLength > 18000)
            {
                RebuildFilteredView();
                return;
            }

            _txtLogs.AppendText(entry.FormatLine() + Environment.NewLine);
        }

        private bool MatchesFilter(SIGLogEntry entry)
        {
            if (entry == null) return false;
            switch (_activeFilter)
            {
                case LogFilterTab.Cultivation:
                    if (entry.Category != LogCategory.Cultivation) return false;
                    break;
                case LogFilterTab.SectAndCraft:
                    if (entry.Category != LogCategory.Sect && entry.Category != LogCategory.Craft) return false;
                    break;
                case LogFilterTab.Combat:
                    if (entry.Category != LogCategory.Combat) return false;
                    break;
                case LogFilterTab.Artifact:
                    if (entry.Category != LogCategory.Artifact) return false;
                    break;
                case LogFilterTab.System:
                    if (entry.Category != LogCategory.System) return false;
                    break;
            }

            string kw = _txtKeyword != null ? _txtKeyword.Text.Trim() : "";
            if (!string.IsNullOrEmpty(kw))
            {
                if ((entry.Message ?? "").IndexOf(kw, StringComparison.OrdinalIgnoreCase) < 0 &&
                    SIGLogEntry.CategoryCn(entry.Category).IndexOf(kw, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return false;
                }
            }
            return true;
        }

        public void RebuildFilteredView()
        {
            if (_txtLogs == null || _eventBus == null) return;
            var sb = new StringBuilder();
            var list = _eventBus.Logs;
            for (int i = 0; i < list.Count; i++)
            {
                if (MatchesFilter(list[i]))
                {
                    sb.AppendLine(list[i].FormatLine());
                }
            }
            _txtLogs.Text = sb.ToString();
            if (!_isScrollLocked)
            {
                _txtLogs.SelectionStart = _txtLogs.TextLength;
                _txtLogs.ScrollToCaret();
            }
        }

        private void RefreshFilterHighlights()
        {
            var p = UiStyleHelper.Current;
            foreach (var kv in _filterButtons)
            {
                bool active = (kv.Key == _activeFilter);
                var btn = kv.Value;
                if (active)
                {
                    btn.BackColor = p.SpeedActiveBg;
                    btn.ForeColor = p.SpeedActiveFg;
                    btn.FlatAppearance.BorderColor = p.SpeedActiveBg;
                }
                else
                {
                    UiStyleHelper.ApplyButtonRoleVisuals(btn, UiButtonRole.Default);
                }
            }
        }

        public void ApplyTheme()
        {
            var p = UiStyleHelper.Current;
            this.BackColor = p.BgWindow;
            if (_grpContainer != null)
            {
                _grpContainer.BackColor = p.BgWindow;
                _grpContainer.ForeColor = p.TextPrimary;
            }
            if (_txtLogs != null)
            {
                _txtLogs.BackColor = p.BgLogBox;
                _txtLogs.ForeColor = p.TextPrimary;
            }
            if (_txtKeyword != null)
            {
                _txtKeyword.BackColor = p.BgCard;
                _txtKeyword.ForeColor = p.TextPrimary;
            }
            RefreshFilterHighlights();
            UpdateLockButtonVisual();
        }
    }
}
