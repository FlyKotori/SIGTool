using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using SIGTool.Configs;
using SIGTool.Datas;
using SIGTool.Modules.Core;
using SIGTool.Modules.Logics;

namespace SIGTool.Forms.UIs
{
    /// <summary>
    /// 页签4（v0.6 无冗余按钮精简 + UI-UX-07 三列差值检查器 + 分栏比例记忆）：
    /// • 移除重复的【出征选中秘境】、【独立战报窗】及战丹槽旁的 3 个【换】按钮；
    /// • 秘境格子双击直接出征、右键菜单；战丹槽双击服用、右键就地改绑/置空。
    /// </summary>
    public class UcCombatExplorePanel : UserControl, IThemeableView
    {
        private readonly SIGEngine _engine;
        private readonly Action _onOpenCombatWindow;

        private UcCompactTileGrid _stageTileGrid;
        private CheckBox _chkAutoRepeat;
        private DoubleBufferedStatusLabel _lblBattleHeader;
        private FlowLayoutPanel _pnlTacticalBar;

        private DataGridView _gridAllies;
        private DataGridView _gridEnemies;
        private readonly InteractiveSlotButton[] _btnUseCombatPill = new InteractiveSlotButton[3];
        private TextBox _txtBattleLog;
        private ContextMenuStrip _pillPopupMenu;

        private string _lastLogTail = "";

        public UcCombatExplorePanel(SIGEngine engine, Action onOpenCombatWindow)
        {
            _engine = engine;
            _onOpenCombatWindow = onOpenCombatWindow;
            this.DoubleBuffered = true;
            this.Dock = DockStyle.Fill;
            this.Padding = new Padding(8);
            this.BackColor = UiStyleHelper.BgWindow;

            BuildUI();
            ApplyTheme();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _pillPopupMenu != null)
            {
                _pillPopupMenu.Dispose();
                _pillPopupMenu = null;
            }
            base.Dispose(disposing);
        }

        private void BuildUI()
        {
            var splitOuter = UiStyleHelper.CreateProportionalSplit(Orientation.Horizontal, 0.34, 155, 240, "Tab4.Outer");

            var grpStage = new GroupBox
            {
                Text = "【天下秘境选定（双击秘境格子直接出征 · 右键打开菜单）】",
                Dock = DockStyle.Fill,
                Font = UiStyleHelper.BoldFont,
                Padding = new Padding(6)
            };

            var tblStage = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0)
            };
            tblStage.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tblStage.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 165F));
            tblStage.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var pnlStageBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(4, 2, 2, 2)
            };
            _chkAutoRepeat = new CheckBox
            {
                Text = "自动连战循环",
                Checked = true,
                AutoSize = true,
                Font = UiStyleHelper.SmallFont,
                Margin = new Padding(2, 2, 2, 6)
            };
            _chkAutoRepeat.CheckedChanged += (s, e) =>
            {
                if (_engine.Save != null)
                {
                    _engine.Save.Combat.AutoRepeatExpedition = _chkAutoRepeat.Checked;
                    UiFeedbackService.PushInfo(_chkAutoRepeat.Checked ? "已开启秘境自动连战。" : "已关闭秘境自动连战。");
                }
            };

            var btnRetreat = UiStyleHelper.CreateButton("撤回山门", (s, e) => DoRetreat(), UiButtonRole.Default, true);
            UiStyleHelper.SetToolTip(btnRetreat, "撤离当前秘境返回宗门休整 — 本页快捷键: Space（交战时撤退 / 休整时出征选中秘境）");
            var btnRaid = UiStyleHelper.CreateButton("主动迎击兽潮", (s, e) =>
            {
                CombatLogic.StartSectRaid(_engine.Save, _engine.EventBus);
                UiFeedbackService.PushWarning("已主动敲响警钟，迎击山门外护山兽潮！");
                RefreshView();
            }, UiButtonRole.DangerRed, true);

            pnlStageBar.Controls.AddRange(new Control[] { _chkAutoRepeat, btnRetreat, btnRaid });

            _stageTileGrid = new UcCompactTileGrid(footerHeight: 68, tileWidth: 144, tileHeight: 40);
            _stageTileGrid.OnTileDoubleClicked += tile => StartStageById(tile.Id, _chkAutoRepeat.Checked);
            _stageTileGrid.ContextMenuBuilder = BuildStageContextMenu;

            tblStage.Controls.Add(_stageTileGrid, 0, 0);
            tblStage.Controls.Add(pnlStageBar, 1, 0);
            grpStage.Controls.Add(tblStage);

            var splitBattle = UiStyleHelper.CreateProportionalSplit(Orientation.Horizontal, 0.58, 150, 95, "Tab4.Battle");

            var tblMid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Margin = new Padding(0)
            };
            tblMid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tblMid.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            tblMid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tblMid.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));

            _lblBattleHeader = new DoubleBufferedStatusLabel(UiStyleHelper.SmallMonoBold, hasBorder: false, useCardBg: false)
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(2, 0, 2, 0),
                Text = "【当前战况】休整待命..."
            };

            var splitUnits = UiStyleHelper.CreateProportionalSplit(Orientation.Vertical, 0.52, 290, 270, "Tab4.Units");

            _gridAllies = UiStyleHelper.CreateDoubleBufferedGrid();
            _gridAllies.Columns.Add("ColName", "我方修士");
            _gridAllies.Columns.Add("ColHp", "气血/护盾");
            _gridAllies.Columns.Add("ColMpAtb", "法力 / 行动条");
            _gridAllies.Columns["ColName"].FillWeight = 32;
            _gridAllies.Columns["ColHp"].FillWeight = 36;
            _gridAllies.Columns["ColMpAtb"].FillWeight = 32;
            _gridAllies.Columns["ColHp"].DefaultCellStyle.Font = UiStyleHelper.SmallMonoReg;
            _gridAllies.Columns["ColMpAtb"].DefaultCellStyle.Font = UiStyleHelper.SmallMonoReg;

            _gridEnemies = UiStyleHelper.CreateDoubleBufferedGrid();
            _gridEnemies.Columns.Add("ColName", "敌方妖邪");
            _gridEnemies.Columns.Add("ColHp", "气血");
            _gridEnemies.Columns.Add("ColAtb", "行动条");
            _gridEnemies.Columns["ColName"].FillWeight = 42;
            _gridEnemies.Columns["ColHp"].FillWeight = 36;
            _gridEnemies.Columns["ColAtb"].FillWeight = 22;

            splitUnits.Panel1.Controls.Add(_gridAllies);
            splitUnits.Panel2.Controls.Add(_gridEnemies);

            _pnlTacticalBar = UiStyleHelper.CreateCleanToolbarStrip(DockStyle.Fill, 30);
            var btnSkillTal = UiStyleHelper.CreateButton("法宝主动技", (s, e) => DoManual("SKILL_TALISMAN"), UiButtonRole.PrimaryBlue, true);
            var btnSkillBurst = UiStyleHelper.CreateButton("诛仙剑阵", (s, e) => DoManual("SKILL_QI_BURST"), UiButtonRole.WarningAmber, true);
            UiStyleHelper.SetToolTip(btnSkillTal, "催动掌门当前佩戴本命法宝的主动神通 — 本页快捷键: Q");
            UiStyleHelper.SetToolTip(btnSkillBurst, "燃烧掌门当前气海修为施展全体高伤【太虚诛仙剑阵】 — 本页快捷键: E");
            _pnlTacticalBar.Controls.Add(btnSkillTal);
            _pnlTacticalBar.Controls.Add(btnSkillBurst);

            for (int i = 0; i < 3; i++)
            {
                int slotIdx = i;
                var pillBtn = new InteractiveSlotButton
                {
                    Text = string.Format("战丹{0}:空", i + 1),
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    FlatStyle = FlatStyle.Flat,
                    Font = UiStyleHelper.SmallFont,
                    Padding = new Padding(4, 0, 4, 0),
                    Margin = new Padding(4, 1, 1, 1),
                    Cursor = Cursors.Hand,
                    UseVisualStyleBackColor = false,
                    Tag = new ButtonRoleTag(UiButtonRole.PositiveGreen)
                };
                pillBtn.FlatAppearance.BorderSize = 1;
                UiStyleHelper.ApplyButtonRoleVisuals(pillBtn, UiButtonRole.PositiveGreen);

                pillBtn.DoubleClick += (s, e) => UseCombatPillSlot(slotIdx);
                pillBtn.MouseUp += (s, e) =>
                {
                    if (e.Button == MouseButtons.Right)
                    {
                        ShowQuickCombatPillMenu(slotIdx, pillBtn, e.Location);
                    }
                };
                UiStyleHelper.SetToolTip(pillBtn, string.Format("战斗快捷丹药槽 {0}（槽1亦用于低血量自动服丹）：双击服丹，右键改绑 — 快捷键: {0}", i + 1));
                _btnUseCombatPill[i] = pillBtn;
                _pnlTacticalBar.Controls.Add(pillBtn);
            }

            var lblPillTip = new Label
            {
                AutoSize = true,
                Font = UiStyleHelper.SmallFont,
                Margin = new Padding(6, 5, 0, 0),
                Text = "(战丹槽: 双击服丹 / 右键绑定)"
            };
            _pnlTacticalBar.Controls.Add(lblPillTip);

            tblMid.Controls.Add(_lblBattleHeader, 0, 0);
            tblMid.Controls.Add(splitUnits, 0, 1);
            tblMid.Controls.Add(_pnlTacticalBar, 0, 2);

            var grpLog = new GroupBox
            {
                Text = "【斗法实时战报】",
                Dock = DockStyle.Fill,
                Font = UiStyleHelper.BoldFont,
                Padding = new Padding(4)
            };

            _txtBattleLog = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = UiStyleHelper.MonoFont
            };
            grpLog.Controls.Add(_txtBattleLog);

            splitBattle.Panel1.Controls.Add(tblMid);
            splitBattle.Panel2.Controls.Add(grpLog);

            splitOuter.Panel1.Controls.Add(grpStage);
            splitOuter.Panel2.Controls.Add(splitBattle);
            this.Controls.Add(splitOuter);
        }

        private ContextMenuStrip BuildStageContextMenu(CompactTileData tile)
        {
            if (tile == null || _engine.Save == null) return null;
            var menu = UiStyleHelper.CreateThemedContextMenu();

            menu.Items.Add(new ToolStripMenuItem(
                string.Format("出征【{0}】(当前连战设置)", tile.ShortTitle),
                null,
                (s, e) => StartStageById(tile.Id, _chkAutoRepeat.Checked)));

            menu.Items.Add(new ToolStripMenuItem(
                string.Format("开启自动连战并出征【{0}】", tile.ShortTitle),
                null,
                (s, e) =>
                {
                    _chkAutoRepeat.Checked = true;
                    StartStageById(tile.Id, true);
                }));

            if (_engine.Save.Combat.IsInBattle)
            {
                menu.Items.Add(new ToolStripSeparator());
                menu.Items.Add(new ToolStripMenuItem("撤回山门休整", null, (s, e) => DoRetreat()));
            }

            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(new ToolStripMenuItem("打开独立战报监控窗", null, (s, e) =>
            {
                if (_onOpenCombatWindow != null) _onOpenCombatWindow();
            }));

            return menu;
        }

        private void ShowQuickCombatPillMenu(int slotIdx, Control anchor, Point pt)
        {
            if (_engine.Save == null) return;
            if (_pillPopupMenu != null) { _pillPopupMenu.Dispose(); _pillPopupMenu = null; }

            _pillPopupMenu = UiStyleHelper.CreateThemedContextMenu();
            var ld = _engine.Save.Leader;
            ld.EnsureQuickSlotsInitialized();
            string curId = ld.QuickCombatPills[slotIdx];

            if (!string.IsNullOrEmpty(curId))
            {
                _pillPopupMenu.Items.Add(new ToolStripMenuItem("立即服用此槽战丹", null, (s, e) => UseCombatPillSlot(slotIdx)));
                _pillPopupMenu.Items.Add(new ToolStripSeparator());
            }

            foreach (var it in SIGConfigTable.Current.Items)
            {
                var cat = it.GetCategory();
                if (cat != ItemCategory.CombatPill && cat != ItemCategory.CultivationPill) continue;
                int qty = _engine.Save.Sect.GetItemCount(it.Id);
                string capturedId = it.Id;

                var mi = new ToolStripMenuItem(
                    string.Format("绑定槽 {0}：{1} (×{2})", slotIdx + 1, it.NameCn, qty),
                    null,
                    (s, e) =>
                    {
                        ld.QuickCombatPills[slotIdx] = capturedId;
                        UiFeedbackService.PushSuccess(string.Format("已将战丹快捷槽 {0} 绑定为【{1}】。", slotIdx + 1, it.NameCn));
                        RefreshView();
                    })
                {
                    Checked = (curId == capturedId)
                };
                _pillPopupMenu.Items.Add(mi);
            }

            _pillPopupMenu.Items.Add(new ToolStripSeparator());
            _pillPopupMenu.Items.Add(new ToolStripMenuItem("置空此战丹槽", null, (s, e) =>
            {
                ld.QuickCombatPills[slotIdx] = "";
                UiFeedbackService.PushInfo(string.Format("已置空战丹快捷槽 {0}。", slotIdx + 1));
                RefreshView();
            }));

            UiStyleHelper.StyleContextMenu(_pillPopupMenu);
            _pillPopupMenu.Show(anchor, pt);
        }

        public void TriggerCombatPillByHotkey(int slotIdx)
        {
            if (slotIdx < 0 || slotIdx >= 3) return;
            UseCombatPillSlot(slotIdx);
        }

        public void TriggerTacticalSkillByHotkey(string code)
        {
            DoManual(code);
        }

        public void ToggleExpeditionOrRetreatByHotkey()
        {
            if (_engine.Save == null) return;
            if (_engine.Save.Combat.IsInBattle)
            {
                DoRetreat();
            }
            else
            {
                var sel = _stageTileGrid != null ? _stageTileGrid.SelectedData : null;
                string targetStageId = sel != null ? sel.Id : _engine.Save.Combat.StageId;
                if (string.IsNullOrEmpty(targetStageId) && SIGConfigTable.Current.Stages.Count > 0)
                {
                    targetStageId = SIGConfigTable.Current.Stages[0].Id;
                }
                if (!string.IsNullOrEmpty(targetStageId))
                {
                    StartStageById(targetStageId, _chkAutoRepeat != null && _chkAutoRepeat.Checked);
                }
            }
        }

        public bool HandleKeyboardNavigation(Keys keyData)
        {
            return _stageTileGrid != null && _stageTileGrid.HandleKeyboardNavigation(keyData);
        }

        public void ApplyTheme()
        {
            var p = UiStyleHelper.Current;
            this.BackColor = p.BgWindow;
            if (_lblBattleHeader != null) _lblBattleHeader.ForeColor = p.AccentCrimson;
            if (_pnlTacticalBar != null) _pnlTacticalBar.BackColor = p.BgSubtle;
            if (_txtBattleLog != null)
            {
                _txtBattleLog.BackColor = p.BgBattleLogBox;
                _txtBattleLog.ForeColor = p.TextBattleLog;
            }
            UiStyleHelper.ApplyGridTheme(_gridAllies);
            UiStyleHelper.ApplyGridTheme(_gridEnemies);
        }

        private void StartStageById(string stageId, bool autoRepeat)
        {
            string msg;
            bool ok = CombatLogic.StartExpedition(_engine.Save, stageId, autoRepeat, _engine.EventBus, out msg);
            UiFeedbackService.ReportAction(ok, msg);
            RefreshView();
        }

        private void DoRetreat()
        {
            string msg;
            bool ok = CombatLogic.RetreatCombat(_engine.Save, _engine.EventBus, out msg);
            UiFeedbackService.ReportAction(ok, msg);
            RefreshView();
        }

        private void DoManual(string code)
        {
            string msg;
            bool ok = CombatLogic.ManualLeaderAction(_engine.Save, code, _engine.Rng, _engine.EventBus, out msg);
            UiFeedbackService.ReportAction(ok, msg);
            RefreshView();
        }

        private void UseCombatPillSlot(int idx)
        {
            string msg;
            bool ok = CombatLogic.UseQuickCombatPillSlot(_engine.Save, idx, _engine.EventBus, out msg);
            UiFeedbackService.ReportAction(ok, msg);
            RefreshView();
        }

        public void RefreshView()
        {
            var save = _engine.Save;
            if (save == null) return;
            var cs = save.Combat;
            save.Leader.EnsureQuickSlotsInitialized();

            if (_chkAutoRepeat.Checked != cs.AutoRepeatExpedition)
                _chkAutoRepeat.Checked = cs.AutoRepeatExpedition;

            var stageTiles = new List<CompactTileData>();
            foreach (var st in SIGConfigTable.Current.Stages)
            {
                bool unlocked = cs.UnlockedStageIds.Contains(st.Id);
                bool activeFighting = cs.IsInBattle && cs.Mode == CombatMode.IdleExpedition && cs.StageId == st.Id;
                var bossCfg = SIGConfigTable.GetEnemy(st.BossEnemyId);

                var rows = new List<InspectorRowData>
                {
                    new InspectorRowData("推荐境界", EnumTextMap.RealmBaseCn(save.Leader.Realm), EnumTextMap.RealmBaseCn(st.GetRecommendedRealm()), unlocked ? 1 : -1),
                    new InspectorRowData("波数 / 声望", st.WaveCount + " 波", string.Format("通关 +{0:F0} 声望", st.ClearReputationReward), 1),
                    new InspectorRowData("镇守首领", bossCfg != null ? bossCfg.NameCn : "-", bossCfg != null ? string.Format("HP:{0:F0} 攻:{1:F0}", bossCfg.MaxHp, bossCfg.Attack) : "-", 0),
                    new InspectorRowData("首领掉落", "妖丹 / 灵石", "概率掉词条阵宝", 1)
                };

                stageTiles.Add(new CompactTileData
                {
                    Id = st.Id,
                    ShortTitle = (unlocked ? "" : "[未解]") + st.GetShortLabel(),
                    SubValueTop = "T" + st.StageTier,
                    SubValueBottom = st.WaveCount + "波",
                    InspectorTitle = string.Format("【{0}】 {1}", st.NameCn, unlocked ? (activeFighting ? "■ 交战中" : "（双击直接出征）") : "[需通关前置秘境]"),
                    InspectorRows = rows,
                    IsActiveEquipped = activeFighting,
                    IsLocked = !unlocked
                });
            }
            _stageTileGrid.SetItems(stageTiles);

            for (int i = 0; i < 3; i++)
            {
                string pid = save.Leader.QuickCombatPills[i];
                var item = SIGConfigTable.GetItem(pid);
                string prefix = (i == 0) ? "[自动槽1]" : string.Format("[战丹{0}]", i + 1);
                string txt = item != null
                    ? string.Format("{0}{1}({2})", prefix, item.GetShortLabel(), save.Sect.GetItemCount(pid))
                    : string.Format("{0}空", prefix);
                if (_btnUseCombatPill[i].Text != txt) _btnUseCombatPill[i].Text = txt;
            }

            string statusLine;
            if (cs.IsInBattle)
            {
                statusLine = string.Format("■ 交战中：【{0}】 第 {1}/{2} 波  |  累计胜场:{3}",
                    cs.StageName, cs.CurrentWave, cs.TotalWaves, cs.TotalVictories);
            }
            else if (cs.AutoRepeatExpedition && cs.RestCooldownSec > 0)
            {
                statusLine = string.Format("… 连战调息中（{0,4:F1}s 后再探【{1}】）  |  累计胜场:{2}",
                    cs.RestCooldownSec, cs.StageName, cs.TotalVictories);
            }
            else
            {
                statusLine = string.Format("休整待命（胜:{0} 败:{1}）—— 双击上方秘境格子即可出征挂机。",
                    cs.TotalVictories, cs.TotalDefeats);
            }
            if (_lblBattleHeader.Text != statusLine) _lblBattleHeader.Text = statusLine;

            SyncUnitGrid(_gridAllies, cs.Allies, true);
            SyncUnitGrid(_gridEnemies, cs.Enemies, false);

            string tail = cs.RecentBattleLogs.Count > 0
                ? cs.RecentBattleLogs.Count + "|" + cs.RecentBattleLogs[cs.RecentBattleLogs.Count - 1]
                : "EMPTY";
            if (_lastLogTail != tail)
            {
                _lastLogTail = tail;
                var sb = new StringBuilder();
                int start = Math.Max(0, cs.RecentBattleLogs.Count - 40);
                for (int i = start; i < cs.RecentBattleLogs.Count; i++)
                {
                    sb.AppendLine(cs.RecentBattleLogs[i]);
                }
                _txtBattleLog.Text = sb.ToString();
                _txtBattleLog.SelectionStart = _txtBattleLog.Text.Length;
                _txtBattleLog.ScrollToCaret();
            }
        }

        private void SyncUnitGrid(DataGridView grid, List<CombatUnitRuntime> units, bool isAlly)
        {
            if (grid.Rows.Count != units.Count)
            {
                grid.Rows.Clear();
                for (int i = 0; i < units.Count; i++) grid.Rows.Add("", "", "");
            }

            for (int i = 0; i < units.Count; i++)
            {
                var u = units[i];
                var row = grid.Rows[i];
                UiStyleHelper.SetCellIfChanged(row.Cells[0], u.Name + (u.IsAlive ? "" : "[退]"));
                if (isAlly)
                {
                    string sh = u.ShieldHp > 0 ? string.Format("+盾{0:F0}", u.ShieldHp) : "";
                    UiStyleHelper.SetCellIfChanged(row.Cells[1], UiStyleHelper.FormatFixedPair(u.CurHp, u.MaxHp, 3) + sh);
                    UiStyleHelper.SetCellIfChanged(row.Cells[2], string.Format("MP:{0} | {1,3:F0}%", UiStyleHelper.PadInt(u.CurMp, 3), Math.Min(100.0, u.ActionGauge)));
                }
                else
                {
                    UiStyleHelper.SetCellIfChanged(row.Cells[1], string.Format("{0} ({1})", UiStyleHelper.FormatFixedPair(u.CurHp, u.MaxHp, 3), EnumTextMap.ElemShortCn(u.Element)));
                    UiStyleHelper.SetCellIfChanged(row.Cells[2], string.Format("{0,3:F0}%", Math.Min(100.0, u.ActionGauge)));
                }
            }
        }
    }
}
