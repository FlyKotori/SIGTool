using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SIGTool.Datas;
using SIGTool.Modules.Core;
using SIGTool.Modules.Logics;

namespace SIGTool.Forms.UIs
{
    internal sealed class UcResourceCell : Panel
    {
        private readonly ResourceType _resourceType;
        private readonly string _shortTitle;
        private readonly bool _showRate;
        private string _amountText = "0";
        private string _rateText = "";
        private int _rateSign = 0;
        private bool _isHovered = false;

        public event Action<ResourceType> OnInspectRequested;

        public UcResourceCell(ResourceType rt, string shortTitle, bool showRate)
        {
            _resourceType = rt;
            _shortTitle = shortTitle;
            _showRate = showRate;
            this.DoubleBuffered = true;
            this.Dock = DockStyle.Fill;
            this.Margin = new Padding(2, 1, 2, 1);
            this.Cursor = Cursors.Hand;

            this.MouseEnter += (s, e) =>
            {
                _isHovered = true;
                Invalidate();
                if (OnInspectRequested != null) OnInspectRequested(_resourceType);
            };
            this.MouseLeave += (s, e) =>
            {
                _isHovered = false;
                Invalidate();
            };
            this.Click += (s, e) =>
            {
                if (OnInspectRequested != null) OnInspectRequested(_resourceType);
            };
        }

        public void UpdateValue(double amount, double netRatePerSec)
        {
            string newAmt = UiStyleHelper.FormatNum(amount);
            string newRate = "";
            int newSign = 0;

            if (_showRate)
            {
                if (netRatePerSec > 0.04)
                {
                    newRate = string.Format("+{0:F1}/s", netRatePerSec);
                    newSign = 1;
                }
                else if (netRatePerSec < -0.04)
                {
                    newRate = string.Format("{0:F1}/s", netRatePerSec);
                    newSign = -1;
                }
                else
                {
                    newRate = "+0.0/s";
                    newSign = 0;
                }
            }

            if (_amountText == newAmt && _rateText == newRate && _rateSign == newSign) return;
            _amountText = newAmt;
            _rateText = newRate;
            _rateSign = newSign;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            var rect = this.ClientRectangle;
            if (rect.Width <= 4 || rect.Height <= 4) return;

            var p = UiStyleHelper.Current;
            Color cellBg;
            Color cellBorder;
            Color nameFg = p.TextHeader;
            Color valFg = p.TextHeader;
            Color rateFg;

            if (p.Mode == UiThemeMode.CleanOfficeLight)
            {
                // 纯灰度白底：严格 R == G == B
                cellBg = _isHovered ? Color.FromArgb(230, 230, 230) : Color.FromArgb(244, 244, 244);
                cellBorder = _isHovered ? Color.FromArgb(90, 90, 90) : Color.FromArgb(204, 204, 204);
                rateFg = _rateSign > 0 ? Color.FromArgb(32, 32, 32)
                       : (_rateSign < 0 ? Color.FromArgb(16, 16, 16) : Color.FromArgb(128, 128, 128));
            }
            else if (p.Mode == UiThemeMode.MinimalDarkGray)
            {
                // 纯灰度深灰：严格 R == G == B
                cellBg = _isHovered ? Color.FromArgb(48, 48, 48) : Color.FromArgb(32, 32, 32);
                cellBorder = _isHovered ? Color.FromArgb(156, 156, 156) : Color.FromArgb(64, 64, 64);
                rateFg = _rateSign > 0 ? Color.FromArgb(212, 212, 212)
                       : (_rateSign < 0 ? Color.FromArgb(240, 240, 240) : Color.FromArgb(128, 128, 128));
            }
            else
            {
                cellBg = _isHovered ? Color.FromArgb(46, 62, 88) : Color.FromArgb(39, 52, 74);
                cellBorder = _isHovered ? Color.FromArgb(96, 165, 250) : Color.FromArgb(64, 82, 108);
                rateFg = _rateSign > 0 ? Color.FromArgb(110, 231, 183)
                       : (_rateSign < 0 ? Color.FromArgb(252, 165, 165) : Color.FromArgb(148, 163, 184));
            }

            using (var bgBrush = new SolidBrush(cellBg))
            using (var borderPen = new Pen(cellBorder))
            {
                g.FillRectangle(bgBrush, rect);
                g.DrawRectangle(borderPen, 0, 0, rect.Width - 1, rect.Height - 1);
            }

            var padRect = new Rectangle(4, 1, Math.Max(2, rect.Width - 8), Math.Max(2, rect.Height - 2));
            if (_showRate)
            {
                int topH = padRect.Height / 2 + 1;
                var topLine = new Rectangle(padRect.Left, padRect.Top, padRect.Width, topH);
                var botLine = new Rectangle(padRect.Left, padRect.Top + topH - 1, padRect.Width, padRect.Height - topH + 1);

                TextRenderer.DrawText(g, _shortTitle, UiStyleHelper.SmallBoldFont, topLine,
                    nameFg, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                TextRenderer.DrawText(g, _amountText, UiStyleHelper.SmallMonoBold, topLine,
                    valFg, TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                TextRenderer.DrawText(g, _rateText, UiStyleHelper.SmallMonoReg, botLine,
                    rateFg, TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }
            else
            {
                TextRenderer.DrawText(g, _shortTitle, UiStyleHelper.SmallBoldFont, padRect,
                    nameFg, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                TextRenderer.DrawText(g, _amountText, UiStyleHelper.SmallMonoBold, padRect,
                    valFg, TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }
        }
    }

    /// <summary>
    /// 顶部全局状态与快捷控制栏（v0.6：紧凑小按钮 + 右侧锚定防截断 TableLayoutPanel，确保【新局】等全部按钮在任意缩放下完整显示，无彩色 Emoji）
    /// </summary>
    public class TopResourceHeaderBar : UserControl, IThemeableView
    {
        private readonly SIGEngine _engine;
        private readonly Action _onOpenVaultWindow;
        private readonly Action _onOpenCombatWindow;
        private readonly Action _onCycleTheme;
        private readonly Action _onStealthHide;
        private readonly Action _onOpenBossSettings;

        private Label _lblSectAndDate;
        private Panel _bottomDivider;

        private Button _btnPause;
        private Button _btn1x;
        private Button _btn2x;
        private Button _btn5x;

        private Button _btnCycleTheme;
        private Button _btnStealthHide;
        private Button _btnBossSettings;
        private ComboBox _cmbSlot;

        private readonly Dictionary<ResourceType, UcResourceCell> _resCells = new Dictionary<ResourceType, UcResourceCell>();

        public TopResourceHeaderBar(
            SIGEngine engine,
            Action onOpenVaultWindow,
            Action onOpenCombatWindow,
            Action onCycleTheme,
            Action onStealthHide,
            Action onOpenBossSettings)
        {
            _engine = engine;
            _onOpenVaultWindow = onOpenVaultWindow;
            _onOpenCombatWindow = onOpenCombatWindow;
            _onCycleTheme = onCycleTheme;
            _onStealthHide = onStealthHide;
            _onOpenBossSettings = onOpenBossSettings;

            this.DoubleBuffered = true;
            this.Dock = DockStyle.Top;
            this.Height = 74;
            this.Padding = new Padding(8, 2, 8, 0);

            BuildLayout();
            ApplyTheme();
        }

        private void BuildLayout()
        {
            // 第一行采用左自适应省略 + 右侧紧凑按钮列，彻底杜绝【新局】被挤出屏幕
            var topRow = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 30,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0),
                BackColor = Color.Transparent
            };
            topRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            topRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            topRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            _lblSectAndDate = new Label
            {
                Dock = DockStyle.Fill,
                AutoEllipsis = true,
                Font = UiStyleHelper.HeaderFont,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(2, 0, 6, 0),
                Text = "【太虚云隐宗 Lv.1】 仙历第 1 年 1 月"
            };

            var topBtnStrip = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 2, 0, 0),
                BackColor = Color.Transparent
            };

            _btnPause = UiStyleHelper.CreateButton("0x", (s, e) =>
            {
                _engine.SetSpeed(0);
                UiFeedbackService.PushInfo("已暂停宗门时间推演 (0x)。");
            }, UiButtonRole.Default, true);

            _btn1x = UiStyleHelper.CreateButton("1x", (s, e) =>
            {
                _engine.SetSpeed(1);
                UiFeedbackService.PushInfo("已切换至常速推演 (1x)。");
            }, UiButtonRole.Default, true);

            _btn2x = UiStyleHelper.CreateButton("2x", (s, e) =>
            {
                _engine.SetSpeed(2);
                UiFeedbackService.PushInfo("已切换至倍速推演 (2x)。");
            }, UiButtonRole.Default, true);

            _btn5x = UiStyleHelper.CreateButton("5x", (s, e) =>
            {
                _engine.SetSpeed(5);
                UiFeedbackService.PushInfo("已切换至疾速推演 (5x)。");
            }, UiButtonRole.Default, true);

            var btnOpenVault = UiStyleHelper.CreateButton("储物阁", (s, e) =>
            {
                if (_onOpenVaultWindow != null) _onOpenVaultWindow();
            }, UiButtonRole.WarningAmber, true);
            btnOpenVault.Margin = new Padding(5, 1, 1, 1);

            var btnOpenCombat = UiStyleHelper.CreateButton("战报窗", (s, e) =>
            {
                if (_onOpenCombatWindow != null) _onOpenCombatWindow();
            }, UiButtonRole.DangerRed, true);

            _cmbSlot = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 64,
                Font = UiStyleHelper.SmallFont,
                Margin = new Padding(5, 2, 2, 1)
            };
            _cmbSlot.Items.AddRange(new object[] { "槽位1", "槽位2", "槽位3" });
            _cmbSlot.SelectedIndex = 0;
            _cmbSlot.SelectedIndexChanged += (s, e) =>
            {
                _engine.CurrentSaveSlot = _cmbSlot.SelectedIndex + 1;
            };

            var btnSave = UiStyleHelper.CreateButton("保存", (s, e) =>
            {
                string msg;
                bool ok = _engine.SaveSIG(out msg);
                UiFeedbackService.ReportAction(ok, msg);
            }, UiButtonRole.Default, true);

            var btnLoad = UiStyleHelper.CreateButton("读档", (s, e) =>
            {
                string msg;
                bool ok = _engine.LoadSIGSlot(_cmbSlot.SelectedIndex + 1, out msg);
                UiFeedbackService.ReportAction(ok, msg);
            }, UiButtonRole.Default, true);

            var btnReloadCfg = UiStyleHelper.CreateButton("配表", (s, e) =>
            {
                string msg;
                bool ok = _engine.ReloadConfig(out msg);
                UiFeedbackService.ReportAction(ok, msg);
            }, UiButtonRole.Default, true);

            var btnReset = UiStyleHelper.CreateButton("新局", (s, e) =>
            {
                var dr = MessageBox.Show("确定要重置当前进度并开启新宗门吗？（界面皮肤、分栏比例与老板键设置将保留）",
                    "重开确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dr == DialogResult.Yes)
                {
                    _engine.ResetNewSIG();
                    UiFeedbackService.PushSuccess("已重开新宗门进度。");
                }
            }, UiButtonRole.Default, true);

            topBtnStrip.Controls.AddRange(new Control[]
            {
                _btnPause, _btn1x, _btn2x, _btn5x,
                btnOpenVault, btnOpenCombat,
                _cmbSlot, btnSave, btnLoad, btnReloadCfg, btnReset
            });

            topRow.Controls.Add(_lblSectAndDate, 0, 0);
            topRow.Controls.Add(topBtnStrip, 1, 0);

            var secondRow = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 1, 0, 3),
                BackColor = Color.Transparent
            };

            var pnlDesktopTools = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent,
                Padding = new Padding(6, 5, 0, 0)
            };

            _btnCycleTheme = UiStyleHelper.CreateButton("皮肤:标准暗蓝", (s, e) =>
            {
                if (_onCycleTheme != null) _onCycleTheme();
            }, UiButtonRole.PrimaryBlue, true);

            _btnStealthHide = UiStyleHelper.CreateButton("隐藏(Alt+Q)", (s, e) =>
            {
                if (_onStealthHide != null) _onStealthHide();
            }, UiButtonRole.Default, true);

            _btnBossSettings = UiStyleHelper.CreateButton("老板键", (s, e) =>
            {
                if (_onOpenBossSettings != null) _onOpenBossSettings();
            }, UiButtonRole.Default, true);

            pnlDesktopTools.Controls.AddRange(new Control[] { _btnCycleTheme, _btnStealthHide, _btnBossSettings });

            var tblResGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 7,
                RowCount = 1,
                Margin = new Padding(0),
                BackColor = Color.Transparent
            };
            UiStyleHelper.EnableDoubleBuffer(tblResGrid);
            tblResGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            float[] weights = { 16.0f, 15.0f, 15.0f, 15.0f, 15.0f, 12.0f, 12.0f };
            for (int c = 0; c < 7; c++)
            {
                tblResGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, weights[c]));
            }

            AddResCell(tblResGrid, 0, ResourceType.SpiritStone, "灵石", true);
            AddResCell(tblResGrid, 1, ResourceType.SpiritWood, "灵木", true);
            AddResCell(tblResGrid, 2, ResourceType.SpiritOre, "玄铁", true);
            AddResCell(tblResGrid, 3, ResourceType.SpiritFood, "灵谷", true);
            AddResCell(tblResGrid, 4, ResourceType.SpiritHerb, "灵草", true);
            AddResCell(tblResGrid, 5, ResourceType.MonsterCore, "妖丹", false);
            AddResCell(tblResGrid, 6, ResourceType.SectReputation, "声望", false);

            _bottomDivider = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1
            };

            secondRow.Controls.Add(tblResGrid);
            secondRow.Controls.Add(pnlDesktopTools);

            this.Controls.Add(secondRow);
            this.Controls.Add(topRow);
            this.Controls.Add(_bottomDivider);
        }

        private void AddResCell(TableLayoutPanel tbl, int col, ResourceType rt, string label, bool showRate)
        {
            var cell = new UcResourceCell(rt, label, showRate);
            cell.OnInspectRequested += inspectedType =>
            {
                if (_engine.Save == null) return;
                UiFeedbackService.PushInfo(SectBuildLogic.BuildResourceBreakdownText(_engine.Save, inspectedType));
            };
            _resCells[rt] = cell;
            tbl.Controls.Add(cell, col, 0);
        }

        public void UpdateDesktopUtilityLabels(string hotkeyLabel, bool hotkeyRegisteredOk)
        {
            string themeTitle = "皮肤:" + UiStyleHelper.GetThemeShortCn(UiStyleHelper.Current.Mode);
            if (_btnCycleTheme.Text != themeTitle) _btnCycleTheme.Text = themeTitle;

            string hideTitle;
            if (!ClientSettingsService.Current.EnableBossHotkey)
            {
                hideTitle = "隐藏至托盘";
            }
            else if (!hotkeyRegisteredOk)
            {
                hideTitle = string.Format("隐藏({0}占用)", hotkeyLabel);
            }
            else
            {
                hideTitle = string.Format("隐藏({0})", hotkeyLabel);
            }
            if (_btnStealthHide.Text != hideTitle) _btnStealthHide.Text = hideTitle;
        }

        public void ApplyTheme()
        {
            var p = UiStyleHelper.Current;
            this.BackColor = p.BgHeader;
            if (_lblSectAndDate != null) _lblSectAndDate.ForeColor = p.TextHeader;
            if (_bottomDivider != null) _bottomDivider.BackColor = p.BorderStrong;
            foreach (var kv in _resCells)
            {
                kv.Value.Invalidate();
            }
            RefreshSpeedHighlight();
        }

        public void RefreshHeader()
        {
            var save = _engine.Save;
            if (save == null) return;

            RefreshSpeedHighlight();

            int discCap = SectBuildLogic.GetMaxDiscipleCap(save.Sect);
            string sectText = string.Format("【{0} Lv.{1}】{2} | 门人:{3}/{4} | 结界:{5:F0}/{6:F0} | 兽潮:{7}月",
                save.Sect.SectName, save.Sect.SectLevel, save.GetImmortalDateText(),
                save.Disciples.Count, discCap,
                save.Sect.BarrierCurHp, save.Sect.BarrierMaxHp,
                save.Combat.MonthsUntilNextRaid);
            if (_lblSectAndDate.Text != sectText) _lblSectAndDate.Text = sectText;

            var prod = SectBuildLogic.CalculateResourceRatesPerSec(save);
            var drain = SectBuildLogic.CalculateResourceDrainPerSec(save);

            foreach (var kv in _resCells)
            {
                ResourceType rt = kv.Key;
                double amt = save.Sect.GetResource(rt);
                double net = prod[rt] - drain[rt];
                kv.Value.UpdateValue(amt, net);
            }
        }

        private void RefreshSpeedHighlight()
        {
            int spd = _engine.SpeedMultiplier;
            HighlightSpeedButton(_btnPause, spd == 0);
            HighlightSpeedButton(_btn1x, spd == 1);
            HighlightSpeedButton(_btn2x, spd == 2);
            HighlightSpeedButton(_btn5x, spd == 5);
        }

        private void HighlightSpeedButton(Button btn, bool active)
        {
            if (btn == null) return;
            var p = UiStyleHelper.Current;
            if (active)
            {
                if (btn.BackColor != p.SpeedActiveBg) btn.BackColor = p.SpeedActiveBg;
                if (btn.ForeColor != p.SpeedActiveFg) btn.ForeColor = p.SpeedActiveFg;
                btn.FlatAppearance.BorderColor = p.SpeedActiveBg;
            }
            else
            {
                Color bg, fg, border;
                p.GetButtonColors(UiButtonRole.Default, out bg, out fg, out border);
                if (btn.BackColor != bg) btn.BackColor = bg;
                if (btn.ForeColor != fg) btn.ForeColor = fg;
                btn.FlatAppearance.BorderColor = border;
            }
        }
    }
}
