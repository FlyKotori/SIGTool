using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SIGTool.Configs;
using SIGTool.Datas;
using SIGTool.Modules.Logics;

namespace SIGTool.Forms.UIs
{
    public enum AlertTargetKind
    {
        LeaderBreakthrough = 0,
        DiscipleBreakthrough = 1,
        IdleWorkshop = 2,
        EmptyVeinSlot = 3,
        EmptyFormationEye = 4,
        IdleExpedition = 5,
        LowFoodWarning = 6,
        ReadyAcupoint = 7,
        PendingEncounter = 8
    }

    public class AlertJumpRequest
    {
        public AlertTargetKind Kind { get; set; }
        public int TargetTabIndex { get; set; }
        public int SubIndex { get; set; } = -1;
        public string EntityId { get; set; } = "";
    }

    /// <summary>
    /// UI-UX-06：顶栏下方「宗门待办与瓶颈速览条」——自动汇总跨页签可执行事项，点击胶囊一键切页并定位目标
    /// </summary>
    public class UcAlertPinStrip : UserControl, IThemeableView
    {
        private readonly Action<AlertJumpRequest> _onJumpRequested;
        private readonly FlowLayoutPanel _flow;
        private readonly Label _lblPrefix;
        private readonly Label _lblAllClear;
        private readonly Panel _bottomBorder;

        private string _lastSignature = "";

        public UcAlertPinStrip(Action<AlertJumpRequest> onJumpRequested)
        {
            _onJumpRequested = onJumpRequested;
            this.DoubleBuffered = true;
            this.Dock = DockStyle.Top;
            this.Height = 26;
            this.Padding = new Padding(8, 1, 8, 0);

            _flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = false,
                AutoScroll = false,
                BackColor = UiStyleHelper.Current.BgSubtle,
                Margin = new Padding(0)
            };

            _lblPrefix = new Label
            {
                AutoSize = true,
                Font = UiStyleHelper.SmallBoldFont,
                Margin = new Padding(0, 4, 6, 0),
                Text = "待办巡检:"
            };

            _lblAllClear = new Label
            {
                AutoSize = true,
                Font = UiStyleHelper.SmallFont,
                Margin = new Padding(0, 4, 0, 0),
                Text = "各项吐纳、灵脉、炉火与护山大阵均运转正常。"
            };

            _bottomBorder = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1
            };

            _flow.Controls.Add(_lblPrefix);
            _flow.Controls.Add(_lblAllClear);
            this.Controls.Add(_flow);
            this.Controls.Add(_bottomBorder);

            ApplyTheme();
        }

        public void ApplyTheme()
        {
            var p = UiStyleHelper.Current;
            this.BackColor = p.BgSubtle;
            if (_flow != null) _flow.BackColor = p.BgSubtle;
            _lblPrefix.ForeColor = p.TextPrimary;
            _lblAllClear.ForeColor = p.TextMuted;
            _bottomBorder.BackColor = p.BorderSubtle;

            for (int i = 0; i < _flow.Controls.Count; i++)
            {
                var btn = _flow.Controls[i] as Button;
                if (btn != null)
                {
                    var tag = btn.Tag as ButtonRoleTag;
                    UiStyleHelper.ApplyButtonRoleVisuals(btn, tag != null ? tag.Role : UiButtonRole.Default);
                }
            }
        }

        public void RefreshAlerts(SIGSaveData save)
        {
            if (save == null || save.Sect == null || save.Leader == null) return;

            var items = new List<KeyValuePair<string, AlertJumpRequest>>();
            var roles = new List<UiButtonRole>();

            // 1. 掌门气海已满可破境
            if (save.Leader.CurQi >= save.Leader.MaxQi - 0.05 && save.Leader.Realm < RealmTier.Immortal)
            {
                items.Add(new KeyValuePair<string, AlertJumpRequest>(
                    "[★ 掌门可破境]",
                    new AlertJumpRequest { Kind = AlertTargetKind.LeaderBreakthrough, TargetTabIndex = 0 }));
                roles.Add(UiButtonRole.WarningAmber);
            }

            // 1a-world. 存在待抉择的非阻塞天机奇遇
            if (save.World != null && save.World.PendingEncounters != null && save.World.PendingEncounters.Count > 0)
            {
                var firstEnc = SIGConfigTable.GetEncounterEvent(save.World.PendingEncounters[0].EventId);
                string encTitle = firstEnc != null ? firstEnc.TitleCn : "天机机缘";
                items.Add(new KeyValuePair<string, AlertJumpRequest>(
                    string.Format("[★ 奇遇({0}):{1}]", save.World.PendingEncounters.Count, encTitle),
                    new AlertJumpRequest { Kind = AlertTargetKind.PendingEncounter, TargetTabIndex = 6 }));
                roles.Add(UiButtonRole.PrimaryBlue);
            }

            // 1b. 掌门存在可立即冲击的奇经穴位
            var readyAcu = MeridianLogic.FindFirstReadyAcupoint(save);
            if (readyAcu != null)
            {
                items.Add(new KeyValuePair<string, AlertJumpRequest>(
                    string.Format("[● 可冲穴:{0}]", readyAcu.NameCn),
                    new AlertJumpRequest { Kind = AlertTargetKind.ReadyAcupoint, TargetTabIndex = 5, EntityId = readyAcu.Id }));
                roles.Add(UiButtonRole.PositiveGreen);
            }

            // 2. 门人弟子大境界满气海待破境
            var readyDisciples = save.Disciples.Where(d => d.CurQi >= d.MaxQi - 0.1 && d.Realm < RealmTier.Immortal).ToList();
            if (readyDisciples.Count > 0)
            {
                items.Add(new KeyValuePair<string, AlertJumpRequest>(
                    string.Format("[★ 门人可破境×{0}]", readyDisciples.Count),
                    new AlertJumpRequest { Kind = AlertTargetKind.DiscipleBreakthrough, TargetTabIndex = 2, EntityId = readyDisciples[0].Id }));
                roles.Add(UiButtonRole.WarningAmber);
            }

            // 3. 已建丹房/器坊停炉空置
            var idleWorkshops = save.Sect.GridSlots.Where(s =>
                s.IsUnlocked && s.Building != null && !s.Building.IsUnderConstruction &&
                (s.Building.ConfigId == "BLD_ALCHEMY" || s.Building.ConfigId == "BLD_FORGE") &&
                string.IsNullOrEmpty(s.Building.ActiveRecipeId)).ToList();
            if (idleWorkshops.Count > 0)
            {
                items.Add(new KeyValuePair<string, AlertJumpRequest>(
                    string.Format("[▲ 工坊停炉×{0}]", idleWorkshops.Count),
                    new AlertJumpRequest { Kind = AlertTargetKind.IdleWorkshop, TargetTabIndex = 2, SubIndex = idleWorkshops[0].SlotIndex }));
                roles.Add(UiButtonRole.PrimaryBlue);
            }

            // 4. 护山九宫阵眼空置且库中有闲置词条法宝
            ArtifactFormationLogic.EnsureFormationInitialized(save.Sect);
            var emptyEyes = save.Sect.FormationEyes.Where(e => e.IsUnlocked && string.IsNullOrEmpty(e.EquippedArtifactInstanceId)).ToList();
            int freeArts = save.Sect.ArtifactVault.Count(a => a.SlottedEyeIndex < 0);
            if (emptyEyes.Count > 0 && freeArts > 0)
            {
                items.Add(new KeyValuePair<string, AlertJumpRequest>(
                    string.Format("[● 空阵眼可镶×{0}]", Math.Min(emptyEyes.Count, freeArts)),
                    new AlertJumpRequest { Kind = AlertTargetKind.EmptyFormationEye, TargetTabIndex = 4, SubIndex = emptyEyes[0].EyeIndex }));
                roles.Add(UiButtonRole.PositiveGreen);
            }

            // 5. 灵谷口粮预警
            int eaters = save.Disciples.Count(d => d.Realm < RealmTier.Foundation);
            if (eaters > 0 && save.Sect.GetResource(ResourceType.SpiritFood) < eaters * SIGConfigTable.Current.Balance.DiscipleMonthlyFoodCost * 1.5)
            {
                items.Add(new KeyValuePair<string, AlertJumpRequest>(
                    "[▲ 灵谷偏低]",
                    new AlertJumpRequest { Kind = AlertTargetKind.LowFoodWarning, TargetTabIndex = 1 }));
                roles.Add(UiButtonRole.DangerRed);
            }

            // 6. 已开辟空置灵脉地块（且灵石够基础营造）
            var emptyVeins = save.Sect.GridSlots.Where(s => s.IsUnlocked && s.Building == null).ToList();
            if (emptyVeins.Count > 0 && save.Sect.GetResource(ResourceType.SpiritStone) >= 45)
            {
                items.Add(new KeyValuePair<string, AlertJumpRequest>(
                    string.Format("[○ 空灵地×{0}]", emptyVeins.Count),
                    new AlertJumpRequest { Kind = AlertTargetKind.EmptyVeinSlot, TargetTabIndex = 1, SubIndex = emptyVeins[0].SlotIndex }));
                roles.Add(UiButtonRole.Default);
            }

            // 7. 秘境休整未出征
            if (!save.Combat.IsInBattle && !(save.Combat.AutoRepeatExpedition && save.Combat.RestCooldownSec > 0))
            {
                items.Add(new KeyValuePair<string, AlertJumpRequest>(
                    "[▶ 秘境待出征]",
                    new AlertJumpRequest { Kind = AlertTargetKind.IdleExpedition, TargetTabIndex = 3 }));
                roles.Add(UiButtonRole.Default);
            }

            var sigSb = new StringBuilder();
            for (int i = 0; i < items.Count; i++)
            {
                sigSb.Append(items[i].Key).Append('|');
            }
            string sig = sigSb.ToString();
            if (sig == _lastSignature) return;
            _lastSignature = sig;

            _flow.SuspendLayout();
            for (int i = _flow.Controls.Count - 1; i >= 0; i--)
            {
                var ctrl = _flow.Controls[i];
                if (ctrl != _lblPrefix && ctrl != _lblAllClear)
                {
                    _flow.Controls.RemoveAt(i);
                    ctrl.Dispose();
                }
            }

            _lblAllClear.Visible = (items.Count == 0);

            for (int i = 0; i < items.Count; i++)
            {
                var req = items[i].Value;
                var btn = UiStyleHelper.CreateButton(items[i].Key, (s, e) =>
                {
                    if (_onJumpRequested != null) _onJumpRequested(req);
                }, roles[i], true);
                _flow.Controls.Add(btn);
            }
            _flow.ResumeLayout();
        }
    }
}
