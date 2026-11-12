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
    /// 通用修士全量属性详情窗（v0.6 紧凑无Emoji按钮与纯灰度皮肤适配）
    /// </summary>
    public class FormCharacterSheet : Form, IThemeableView
    {
        private readonly SIGEngine _engine;
        private CharacterData _character;
        private TextBox _txtFullSheet;

        public FormCharacterSheet(SIGEngine engine, CharacterData character)
        {
            _engine = engine;
            _character = character ?? engine.Save.Leader;

            this.DoubleBuffered = true;
            this.Text = string.Format("道躯全属性法卷 - 【{0}】", _character.Name);
            this.Size = new Size(580, 520);
            this.MinimumSize = new Size(480, 380);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = UiStyleHelper.NormalFont;

            BuildUI();
            UiStyleHelper.ApplyThemeRecursive(this);
            RefreshSheet();
        }

        private void BuildUI()
        {
            var pnlBottom = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 32,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(8, 2, 8, 2)
            };
            pnlBottom.Controls.Add(UiStyleHelper.CreateButton("关闭", (s, e) => this.Close(), UiButtonRole.Default, true));
            pnlBottom.Controls.Add(UiStyleHelper.CreateButton("刷新数值", (s, e) => RefreshSheet(), UiButtonRole.PrimaryBlue, true));

            _txtFullSheet = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = UiStyleHelper.MonoFont
            };

            var wrap = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 10, 10, 4) };
            wrap.Controls.Add(_txtFullSheet);

            this.Controls.Add(wrap);
            this.Controls.Add(pnlBottom);
        }

        public void ApplyTheme()
        {
            var p = UiStyleHelper.Current;
            if (_txtFullSheet != null)
            {
                _txtFullSheet.BackColor = p.BgCard;
                _txtFullSheet.ForeColor = p.TextPrimary;
            }
        }

        public void SetCharacter(CharacterData ch)
        {
            if (ch == null) return;
            _character = ch;
            this.Text = string.Format("道躯全属性法卷 - 【{0}】", _character.Name);
            RefreshSheet();
        }

        public void RefreshSheet()
        {
            if (this.IsDisposed || _engine.Save == null || _character == null) return;
            var ch = _character;
            var sect = _engine.Save.Sect;
            CultivationLogic.RecalculateStats(ch, sect);

            var rCfg = SIGConfigTable.GetRealm(ch.Realm);
            var tech = SIGConfigTable.GetTechnique(ch.EquippedTechniqueId);
            int techLv = ch.GetTechniqueLevel(ch.EquippedTechniqueId);
            double qiRate = CultivationLogic.GetQiRatePerSec(ch, sect);
            double sectAura = SectBuildLogic.GetTotalSectAuraBonus(sect);
            double remainLife = Math.Max(0.0, ch.MaxLifespanYears - ch.AgeYears);
            var fm = ArtifactFormationLogic.ComputeFormationSummary(sect);

            var wItem = SIGConfigTable.GetItem(ch.EquippedWeaponId);
            var aItem = SIGConfigTable.GetItem(ch.EquippedArmorId);
            var tItem = SIGConfigTable.GetItem(ch.EquippedTalismanId);
            string wpn = wItem != null ? wItem.NameCn : "未佩剑";
            string arm = aItem != null ? aItem.NameCn : "未着甲";
            string tal = tItem != null ? tItem.NameCn : "未祭炼";

            var sb = new StringBuilder();
            sb.AppendLine(string.Format("================ 【{0} · 全量道躯法卷】 ================", ch.Name));
            sb.AppendLine(string.Format("身份定位：{0}      当前职责：{1}",
                ch.IsSectLeader ? "一宗之主" : "宗门门人",
                ch.IsSectLeader ? "统御全宗 / 秘境领队" : EnumTextMap.JobCn(ch.AssignedJob)));
            sb.AppendLine(string.Format("修真境界：{0}      综合战力：{1:F0}",
                EnumTextMap.RealmFullCn(ch.Realm, ch.SubStage, rCfg.MaxSubStage), ch.EstimateCombatPower()));
            sb.AppendLine(string.Format("骨龄寿元：{0:F1} 岁 / 寿限 {1:F0} 载 （剩余寿元 {2:F1} 年）",
                ch.AgeYears, ch.MaxLifespanYears, remainLife));
            sb.AppendLine();
            sb.AppendLine("【一、 先天根骨与灵根资质】");
            sb.AppendLine(string.Format("  五行灵根：{0} · {1}",
                EnumTextMap.ElemCn(ch.Innate.RootElement), EnumTextMap.RootQualityCn(ch.Innate.RootQuality)));
            sb.AppendLine(string.Format("  体    质：{0,-4} (影响气血上限与防御、寿限)", ch.Innate.Constitution));
            sb.AppendLine(string.Format("  力    量：{0,-4} (影响基础攻击力)", ch.Innate.Strength));
            sb.AppendLine(string.Format("  悟    性：{0,-4} (影响吐纳效率、法力上限与大境界渡劫率)", ch.Innate.Comprehension));
            sb.AppendLine(string.Format("  身    法：{0,-4} (影响出手速度与身法闪避率)", ch.Innate.Agility));
            sb.AppendLine(string.Format("  机    缘：{0,-4} (影响暴击率与渡劫天眷)", ch.Innate.Fortune));
            sb.AppendLine();
            sb.AppendLine("【二、 后天完整战斗面板（已计入装备与九宫护山大阵词条）】");
            sb.AppendLine(string.Format("  气血(HP)：{0:F0} / {1:F0}      法力(MP)：{2:F0} / {3:F0}",
                ch.BattleStats.CurHp, ch.BattleStats.MaxHp, ch.BattleStats.CurMp, ch.BattleStats.MaxMp));
            sb.AppendLine(string.Format("  攻击(ATK)：{0,-8:F1}          防御(DEF)：{1:F1}",
                ch.BattleStats.Attack, ch.BattleStats.Defense));
            sb.AppendLine(string.Format("  速度(SPD)：{0,-8:F1}          斗法吸血：{1:P1}",
                ch.BattleStats.Speed, ch.BattleStats.LifestealRate));
            sb.AppendLine(string.Format("  暴击 / 闪避：{0:P1} / {1:P1}      护山阵享：{2}",
                ch.BattleStats.CritRate, ch.BattleStats.DodgeRate,
                ch.IsSectLeader ? string.Format("100% (在阵{0}/9件)", fm.SlottedCount) : string.Format("65% (在阵{0}/9件)", fm.SlottedCount)));
            sb.AppendLine();
            sb.AppendLine("【三、 修炼吐纳效率拆解与百艺造诣】");
            sb.AppendLine(string.Format("  气海容量：{0:F0} / {1:F0}      实时吐纳：+{2:F2} 气海/秒",
                ch.CurQi, ch.MaxQi, qiRate));
            sb.AppendLine(string.Format("  主修功法：{0} (Lv.{1}/{2})  |  宗门灵气总加成：+{3:P0}",
                tech != null ? tech.NameCn : "无", techLv, tech != null ? tech.MaxLevel : 10, sectAura));
            sb.AppendLine(string.Format("  百艺造诣：农采 Lv.{0}   |   丹道 Lv.{1}   |   器道 Lv.{2}",
                ch.GatherSkillLv, ch.AlchemySkillLv, ch.ForgeSkillLv));
            sb.AppendLine(string.Format("  穿戴法器：剑[{0}]  衣[{1}]  宝[{2}]", wpn, arm, tal));

            _txtFullSheet.Text = sb.ToString();
            _txtFullSheet.SelectionStart = 0;
        }
    }
}
