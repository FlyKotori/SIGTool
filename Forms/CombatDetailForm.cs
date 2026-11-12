using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using SIGTool.Configs;
using SIGTool.Datas;
using SIGTool.Forms.UIs;
using SIGTool.Modules.Core;
using SIGTool.Modules.Logics;

namespace SIGTool.Forms
{
    /// <summary>
    /// 独立非模态斗法演武监控窗（v0.6：移除战丹槽旁重复【换】按钮，统一采用双击服丹与右键改绑）
    /// </summary>
    public class CombatDetailForm : Form, IThemeableView
    {
        private readonly SIGEngine _engine;
        private readonly InteractiveSlotButton[] _btnCombatPills = new InteractiveSlotButton[3];
        private Label _lblStatus;
        private TextBox _txtSnapshot;
        private TextBox _txtBattleLogs;
        private UcStatusFeedbackStrip _localStatusStrip;
        private ContextMenuStrip _pillPopupMenu;
        private string _lastTail = "";

        public CombatDetailForm(SIGEngine engine)
        {
            _engine = engine;
            this.DoubleBuffered = true;
            this.Text = "独立演武战报窗 - 秘境连战监控";
            this.Size = new Size(800, 540);
            this.MinimumSize = new Size(620, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = UiStyleHelper.NormalFont;

            BuildUI();
            UiStyleHelper.ApplyThemeRecursive(this);
            RefreshView();
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
            var pnlTop = UiStyleHelper.CreateCleanToolbarStrip(DockStyle.Top, 32);
            pnlTop.Padding = new Padding(6, 2, 6, 2);

            pnlTop.Controls.Add(UiStyleHelper.CreateButton("法宝主动技", (s, e) => TriggerSkill("SKILL_TALISMAN"), UiButtonRole.PrimaryBlue, true));
            pnlTop.Controls.Add(UiStyleHelper.CreateButton("诛仙剑阵", (s, e) => TriggerSkill("SKILL_QI_BURST"), UiButtonRole.WarningAmber, true));

            for (int i = 0; i < 3; i++)
            {
                int slotIdx = i;
                var pillBtn = new InteractiveSlotButton
                {
                    Text = string.Format("[战丹{0}]", i + 1),
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

                pillBtn.DoubleClick += (s, e) =>
                {
                    string msg;
                    bool ok = CombatLogic.UseQuickCombatPillSlot(_engine.Save, slotIdx, _engine.EventBus, out msg);
                    ReportCombatFeedback(ok, msg);
                    RefreshView();
                };
                pillBtn.MouseUp += (s, e) =>
                {
                    if (e.Button == MouseButtons.Right)
                    {
                        ShowQuickCombatPillMenu(slotIdx, pillBtn, e.Location);
                    }
                };
                _btnCombatPills[i] = pillBtn;
                pnlTop.Controls.Add(pillBtn);
            }

            pnlTop.Controls.Add(UiStyleHelper.CreateButton("暂停 / 继续", (s, e) =>
            {
                int nextSpd = _engine.SpeedMultiplier == 0 ? 1 : 0;
                _engine.SetSpeed(nextSpd);
                ReportCombatFeedback(true, nextSpd == 0 ? "已暂停时间推演 (0x)。" : "已恢复常速推演 (1x)。");
                RefreshView();
            }, UiButtonRole.Default, true));

            _lblStatus = new Label
            {
                Dock = DockStyle.Top,
                Height = 26,
                Padding = new Padding(8, 3, 8, 2),
                Font = UiStyleHelper.BoldFont,
                Text = "当前战况加载中..."
            };

            _localStatusStrip = new UcStatusFeedbackStrip(
                subscribeGlobalBus: false,
                defaultHintText: "战丹按钮: 双击服用 / 右键绑定");

            var splitBody = UiStyleHelper.CreateProportionalSplit(Orientation.Horizontal, 0.34, 105, 150, "CombatForm.Body");

            _txtSnapshot = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = UiStyleHelper.MonoFont
            };

            _txtBattleLogs = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = UiStyleHelper.MonoFont
            };

            splitBody.Panel1.Controls.Add(_txtSnapshot);
            splitBody.Panel2.Controls.Add(_txtBattleLogs);

            this.Controls.Add(splitBody);
            this.Controls.Add(_localStatusStrip);
            this.Controls.Add(_lblStatus);
            this.Controls.Add(pnlTop);
        }

        private void ReportCombatFeedback(bool ok, string msg)
        {
            _localStatusStrip.SetFeedback(ok ? UiFeedbackLevel.Success : UiFeedbackLevel.Blocked, msg);
            UiFeedbackService.ReportAction(ok, msg);
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
                _pillPopupMenu.Items.Add(new ToolStripMenuItem("立即服用此槽战丹", null, (s, e) =>
                {
                    string msg;
                    bool ok = CombatLogic.UseQuickCombatPillSlot(_engine.Save, slotIdx, _engine.EventBus, out msg);
                    ReportCombatFeedback(ok, msg);
                    RefreshView();
                }));
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
                        ReportCombatFeedback(true, string.Format("已将战丹快捷槽 {0} 绑定为【{1}】。", slotIdx + 1, it.NameCn));
                        RefreshView();
                    })
                {
                    Checked = (curId == capturedId)
                };
                _pillPopupMenu.Items.Add(mi);
            }

            _pillPopupMenu.Items.Add(new ToolStripSeparator());
            _pillPopupMenu.Items.Add(new ToolStripMenuItem("置空此槽", null, (s, e) =>
            {
                ld.QuickCombatPills[slotIdx] = "";
                ReportCombatFeedback(true, string.Format("已置空战丹快捷槽 {0}。", slotIdx + 1));
                RefreshView();
            }));

            UiStyleHelper.StyleContextMenu(_pillPopupMenu);
            _pillPopupMenu.Show(anchor, pt);
        }

        public void ApplyTheme()
        {
            var p = UiStyleHelper.Current;
            if (_lblStatus != null) _lblStatus.ForeColor = p.AccentCrimson;
            if (_txtSnapshot != null)
            {
                _txtSnapshot.BackColor = p.BgCard;
                _txtSnapshot.ForeColor = p.TextPrimary;
            }
            if (_txtBattleLogs != null)
            {
                _txtBattleLogs.BackColor = p.BgBattleLogBox;
                _txtBattleLogs.ForeColor = p.TextBattleLog;
            }
        }

        private void TriggerSkill(string code)
        {
            string msg;
            bool ok = CombatLogic.ManualLeaderAction(_engine.Save, code, _engine.Rng, _engine.EventBus, out msg);
            ReportCombatFeedback(ok, msg);
            RefreshView();
        }

        public void RefreshView()
        {
            if (this.IsDisposed || _engine.Save == null) return;
            var save = _engine.Save;
            var cs = save.Combat;
            save.Leader.EnsureQuickSlotsInitialized();

            for (int i = 0; i < 3; i++)
            {
                string pid = save.Leader.QuickCombatPills[i];
                var item = SIGConfigTable.GetItem(pid);
                string prefix = (i == 0) ? "[自动1]" : string.Format("[丹{0}]", i + 1);
                string txt = item != null
                    ? string.Format("{0}{1}({2})", prefix, item.GetShortLabel(), save.Sect.GetItemCount(pid))
                    : string.Format("{0}空", prefix);
                if (_btnCombatPills[i].Text != txt) _btnCombatPills[i].Text = txt;
            }

            _lblStatus.Text = cs.IsInBattle
                ? string.Format("■ 【{0}】 波次 {1}/{2} | 回合:{3} | 护山结界:{4:F0}/{5:F0}",
                    cs.StageName, cs.CurrentWave, cs.TotalWaves, cs.TurnCount,
                    save.Sect.BarrierCurHp, save.Sect.BarrierMaxHp)
                : string.Format("休整待命中（累计胜场 {0}）", cs.TotalVictories);

            var snap = new StringBuilder();
            snap.AppendLine("【我方参战】");
            foreach (var a in cs.Allies)
            {
                snap.AppendLine(string.Format("  - {0,-12} HP:{1,5:F0}/{2,-5:F0} MP:{3,4:F0}/{4,-4:F0} 吸血:{5,4:P0} ATB:{6,3:F0}%",
                    a.Name, a.CurHp, a.MaxHp, a.CurMp, a.MaxMp, a.LifestealRate, Math.Min(100, a.ActionGauge)));
            }
            snap.AppendLine("【敌方妖邪】");
            foreach (var e in cs.Enemies)
            {
                snap.AppendLine(string.Format("  - {0,-12} HP:{1,5:F0}/{2,-5:F0} 攻:{3,-5:F0} ATB:{4,3:F0}%",
                    e.Name, e.CurHp, e.MaxHp, e.Attack, Math.Min(100, e.ActionGauge)));
            }
            string snapStr = snap.ToString();
            if (_txtSnapshot.Text != snapStr) _txtSnapshot.Text = snapStr;

            string tail = cs.RecentBattleLogs.Count > 0
                ? cs.RecentBattleLogs.Count + "|" + cs.RecentBattleLogs[cs.RecentBattleLogs.Count - 1]
                : "NONE";
            if (_lastTail != tail)
            {
                _lastTail = tail;
                var logSb = new StringBuilder();
                int start = Math.Max(0, cs.RecentBattleLogs.Count - 50);
                for (int i = start; i < cs.RecentBattleLogs.Count; i++) logSb.AppendLine(cs.RecentBattleLogs[i]);
                _txtBattleLogs.Text = logSb.ToString();
                _txtBattleLogs.SelectionStart = _txtBattleLogs.Text.Length;
                _txtBattleLogs.ScrollToCaret();
            }
        }
    }
}
