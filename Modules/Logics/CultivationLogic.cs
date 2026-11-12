using System;
using System.Linq;
using SIGTool.Configs;
using SIGTool.Datas;
using SIGTool.Modules.Core;

namespace SIGTool.Modules.Logics
{
    /// <summary>
    /// 修仙吐纳、功法参悟升层、护山大阵词条汇总加成、属性差值预览、装备替换与破境渡劫逻辑
    /// </summary>
    public static class CultivationLogic
    {
        public static void RecalculateStats(CharacterData ch, SectManageData sect)
        {
            if (ch == null) return;
            ch.EnsureQuickSlotsInitialized();

            var rCfg = SIGConfigTable.GetRealm(ch.Realm);
            var tech = SIGConfigTable.GetTechnique(ch.EquippedTechniqueId) ?? SIGConfigTable.GetTechnique("TECH_BASIC");
            int techLv = ch.GetTechniqueLevel(tech != null ? tech.Id : "TECH_BASIC");
            double techLvBonus = 1.0 + (techLv - 1) * 0.04;

            int sub = Math.Max(1, Math.Min(rCfg.MaxSubStage, ch.SubStage));
            double stageFactor = Math.Pow(rCfg.SubStageQiGrowth, sub - 1);
            double statStageMult = 1.0 + (sub - 1) * 0.24;

            double rootCapMult = 0.85 + ch.Innate.RootQuality * 0.12;
            ch.MaxQi = Math.Round(rCfg.BaseMaxQi * stageFactor * rootCapMult, 0);
            ch.MaxLifespanYears = rCfg.BaseLifespanYears + ch.Innate.Constitution * 3.0;

            double elemMatchBonus = 1.0;
            if (tech != null && tech.GetElement() != ElementType.None)
            {
                if (tech.GetElement() == ch.Innate.RootElement) elemMatchBonus = 1.15;
                else if (EnumTextMap.IsGenerating(ch.Innate.RootElement, tech.GetElement())) elemMatchBonus = 1.10;
            }

            double techHpMult = tech != null ? tech.HpMultiplier * elemMatchBonus * techLvBonus : 1.0;
            double techAtkMult = tech != null ? tech.AtkMultiplier * elemMatchBonus * techLvBonus : 1.0;
            double techDefMult = tech != null ? tech.DefMultiplier * elemMatchBonus * techLvBonus : 1.0;

            double rawHp = (rCfg.BaseHp * statStageMult + ch.Innate.Constitution * 14.0) * techHpMult;
            double rawMp = (rCfg.BaseMp * statStageMult + ch.Innate.Comprehension * 7.0);
            double rawAtk = (rCfg.BaseAtk * statStageMult + ch.Innate.Strength * 2.6) * techAtkMult;
            double rawDef = (rCfg.BaseDef * statStageMult + ch.Innate.Constitution * 1.3) * techDefMult;
            double rawSpd = 92.0 + ch.Innate.Agility * 1.8 + (int)ch.Realm * 5.0;

            string[] eqIds = { ch.EquippedWeaponId, ch.EquippedArmorId, ch.EquippedTalismanId };
            foreach (string eqId in eqIds)
            {
                var item = SIGConfigTable.GetItem(eqId);
                if (item != null)
                {
                    rawHp += item.HpBonus;
                    rawAtk += item.AtkBonus;
                    rawDef += item.DefBonus;
                    rawSpd += item.SpeedBonus;
                }
            }

            // SYS-01 叠加护山大阵镶嵌法宝词条与连携阵势增益（掌门享100%大阵加持，门人享65%大阵余荫）
            var fm = ArtifactFormationLogic.ComputeFormationSummary(sect);
            double fmShare = ch.IsSectLeader ? 1.0 : 0.65;
            rawHp = (rawHp + fm.FlatHp * fmShare) * (1.0 + fm.PctHp * fmShare);
            rawAtk = (rawAtk + fm.FlatAtk * fmShare) * (1.0 + fm.PctAtk * fmShare);
            rawDef = (rawDef + fm.FlatDef * fmShare) * (1.0 + fm.PctDef * fmShare);
            rawSpd += fm.FlatSpeed * fmShare;

            if (ch.BattleStats == null) ch.BattleStats = new BattleStatsData();
            double hpRatio = ch.BattleStats.MaxHp > 0 ? ch.BattleStats.CurHp / ch.BattleStats.MaxHp : 1.0;
            double mpRatio = ch.BattleStats.MaxMp > 0 ? ch.BattleStats.CurMp / ch.BattleStats.MaxMp : 1.0;

            ch.BattleStats.MaxHp = Math.Round(rawHp, 0);
            ch.BattleStats.MaxMp = Math.Round(rawMp, 0);
            ch.BattleStats.Attack = Math.Round(rawAtk, 1);
            ch.BattleStats.Defense = Math.Round(rawDef, 1);
            ch.BattleStats.Speed = Math.Round(rawSpd, 1);
            ch.BattleStats.CritRate = Math.Min(0.75, 0.05 + ch.Innate.Fortune * 0.008 + fm.CritRateAdd * fmShare);
            ch.BattleStats.DodgeRate = Math.Min(0.55, 0.03 + ch.Innate.Agility * 0.005 + fm.DodgeRateAdd * fmShare);
            ch.BattleStats.LifestealRate = Math.Min(0.50, fm.LifestealRateAdd * fmShare);

            ch.BattleStats.CurHp = Math.Max(1.0, Math.Min(ch.BattleStats.MaxHp, Math.Round(ch.BattleStats.MaxHp * hpRatio, 0)));
            ch.BattleStats.CurMp = Math.Max(0.0, Math.Min(ch.BattleStats.MaxMp, Math.Round(ch.BattleStats.MaxMp * mpRatio, 0)));
        }

        public static void RecalculateAllCharacters(SIGSaveData save)
        {
            if (save == null) return;
            RecalculateStats(save.Leader, save.Sect);
            if (save.Disciples != null)
            {
                foreach (var d in save.Disciples)
                {
                    RecalculateStats(d, save.Sect);
                }
            }
        }

        public static double GetQiRatePerSec(CharacterData ch, SectManageData sect)
        {
            if (ch == null) return 0.0;
            var rCfg = SIGConfigTable.GetRealm(ch.Realm);
            var tech = SIGConfigTable.GetTechnique(ch.EquippedTechniqueId);
            int techLv = ch.GetTechniqueLevel(tech != null ? tech.Id : "");
            double techLvMult = 1.0 + (techLv - 1) * 0.06;

            double rootMult = 0.65 + ch.Innate.RootQuality * 0.22;
            double compMult = 1.0 + ch.Innate.Comprehension * 0.025;
            double techMult = (tech != null ? tech.QiRateMultiplier : 1.0) * techLvMult;

            if (tech != null && tech.GetElement() != ElementType.None && tech.GetElement() == ch.Innate.RootElement)
            {
                techMult *= 1.18;
            }

            double sectAuraBonus = 1.0 + SectBuildLogic.GetTotalSectAuraBonus(sect);
            double jobMult = (!ch.IsSectLeader && ch.AssignedJob != DiscipleJob.Cultivate) ? 0.35 : 1.0;

            return rCfg.BaseQiPerSec * rootMult * compMult * techMult * sectAuraBonus * jobMult;
        }

        public static string PreviewTechniqueDelta(CharacterData ch, SectManageData sect, string candidateTechId)
        {
            if (ch == null || string.IsNullOrEmpty(candidateTechId)) return "";
            if (ch.EquippedTechniqueId == candidateTechId) return "【当前主修中】";

            string origTech = ch.EquippedTechniqueId;
            double curQiSpd = GetQiRatePerSec(ch, sect);
            double curAtk = ch.BattleStats.Attack;
            double curDef = ch.BattleStats.Defense;
            double curHp = ch.BattleStats.MaxHp;

            ch.EquippedTechniqueId = candidateTechId;
            RecalculateStats(ch, sect);
            double nextQiSpd = GetQiRatePerSec(ch, sect);
            double dQi = nextQiSpd - curQiSpd;
            double dAtk = ch.BattleStats.Attack - curAtk;
            double dDef = ch.BattleStats.Defense - curDef;
            double dHp = ch.BattleStats.MaxHp - curHp;

            ch.EquippedTechniqueId = origTech;
            RecalculateStats(ch, sect);

            return string.Format("切换差值 ➔ 吐纳:{0}  攻:{1}  防:{2}  血:{3}",
                FormatSigned(dQi, "F1") + "/s",
                FormatSigned(dAtk, "F1"),
                FormatSigned(dDef, "F1"),
                FormatSigned(dHp, "F0"));
        }

        public static string PreviewEquipmentDelta(CharacterData ch, string candidateItemId)
        {
            var cand = SIGConfigTable.GetItem(candidateItemId);
            if (ch == null || cand == null) return "";

            ItemCategory cat = cand.GetCategory();
            string curId = "";
            if (cat == ItemCategory.Weapon) curId = ch.EquippedWeaponId;
            else if (cat == ItemCategory.Armor) curId = ch.EquippedArmorId;
            else if (cat == ItemCategory.Talisman) curId = ch.EquippedTalismanId;
            else return "";

            if (curId == candidateItemId) return "【当前已穿戴同款装备】";
            var cur = SIGConfigTable.GetItem(curId);

            double dAtk = cand.AtkBonus - (cur != null ? cur.AtkBonus : 0);
            double dDef = cand.DefBonus - (cur != null ? cur.DefBonus : 0);
            double dHp = cand.HpBonus - (cur != null ? cur.HpBonus : 0);
            double dSpd = cand.SpeedBonus - (cur != null ? cur.SpeedBonus : 0);

            return string.Format("替换差值 ➔ 攻:{0}  防:{1}  血:{2}  速:{3}",
                FormatSigned(dAtk, "F0"), FormatSigned(dDef, "F0"), FormatSigned(dHp, "F0"), FormatSigned(dSpd, "F0"));
        }

        private static string FormatSigned(double val, string fmt)
        {
            if (Math.Abs(val) < 0.05) return "±0";
            return (val > 0 ? "+" : "") + val.ToString(fmt);
        }

        public static double GetTechniqueUpgradeStoneCost(CharacterData ch, TechniqueConfig tech)
        {
            if (ch == null || tech == null) return 9999;
            int lv = ch.GetTechniqueLevel(tech.Id);
            return Math.Round(tech.BaseUpgradeStoneCost * Math.Pow(1.45, lv - 1), 0);
        }

        public static bool UpgradeTechniqueLevel(SIGSaveData save, CharacterData ch, string techId, SIGEventBus bus, out string message)
        {
            var tech = SIGConfigTable.GetTechnique(techId);
            if (save == null || ch == null || tech == null)
            {
                message = "未选择有效功法。";
                return false;
            }

            int curLv = ch.GetTechniqueLevel(tech.Id);
            if (curLv >= tech.MaxLevel)
            {
                message = string.Format("{0} 已臻至满层 (Lv.{1}/{1})！", tech.NameCn, tech.MaxLevel);
                return false;
            }

            double cost = GetTechniqueUpgradeStoneCost(ch, tech);
            if (save.Sect.GetResource(ResourceType.SpiritStone) < cost)
            {
                message = string.Format("参悟 {0} 资材不足：{1}", tech.NameCn, SectBuildLogic.FormatShortfallDetail(save, cost));
                return false;
            }

            save.Sect.AddResource(ResourceType.SpiritStone, -cost);
            ch.TechniqueLevels[tech.Id] = curLv + 1;
            RecalculateStats(ch, save.Sect);

            message = string.Format("【{0}】消耗 {1:F0} 灵石参悟 {2}，功法精进至 Lv.{3}/{4}！",
                ch.Name, cost, tech.NameCn, curLv + 1, tech.MaxLevel);
            if (bus != null)
            {
                bus.PublishLog(save.GetImmortalDateText(), LogCategory.Cultivation, message);
                bus.NotifyStateChanged();
            }
            return true;
        }

        public static double GetBreakthroughSuccessRate(CharacterData ch, SectManageData sect, out string bonusPillId)
        {
            bonusPillId = "";
            if (ch == null) return 0.0;

            var rCfg = SIGConfigTable.GetRealm(ch.Realm);
            if (ch.SubStage < rCfg.MaxSubStage) return 1.0;

            var fm = ArtifactFormationLogic.ComputeFormationSummary(sect);
            double rate = rCfg.BreakthroughBaseRate + ch.Innate.Comprehension * 0.012 + ch.Innate.Fortune * 0.005 + fm.BreakthroughRateAdd;

            if (!string.IsNullOrEmpty(rCfg.RecommendedPillId) && sect != null && sect.GetItemCount(rCfg.RecommendedPillId) > 0)
            {
                var pillCfg = SIGConfigTable.GetItem(rCfg.RecommendedPillId);
                if (pillCfg != null)
                {
                    rate += pillCfg.BreakthroughRateBonus;
                    bonusPillId = pillCfg.Id;
                }
            }
            return Math.Min(0.98, Math.Max(0.10, rate));
        }

        public static void TickCultivation(SIGSaveData save, double simDeltaSec, SIGEventBus bus)
        {
            if (save == null || simDeltaSec <= 0) return;

            var leader = save.Leader;
            if (leader.CurQi < leader.MaxQi)
            {
                double gain = GetQiRatePerSec(leader, save.Sect) * simDeltaSec;
                leader.CurQi = Math.Min(leader.MaxQi, leader.CurQi + gain);
            }

            if (!save.Combat.IsInBattle)
            {
                leader.BattleStats.CurHp = Math.Min(leader.BattleStats.MaxHp, leader.BattleStats.CurHp + leader.BattleStats.MaxHp * 0.05 * simDeltaSec);
                leader.BattleStats.CurMp = Math.Min(leader.BattleStats.MaxMp, leader.BattleStats.CurMp + leader.BattleStats.MaxMp * 0.08 * simDeltaSec);
            }

            foreach (var disc in save.Disciples)
            {
                double gain = GetQiRatePerSec(disc, save.Sect) * simDeltaSec;
                disc.CurQi = Math.Min(disc.MaxQi, disc.CurQi + gain);

                var rCfg = SIGConfigTable.GetRealm(disc.Realm);
                if (disc.CurQi >= disc.MaxQi && disc.SubStage < rCfg.MaxSubStage)
                {
                    disc.CurQi = 0;
                    disc.SubStage++;
                    RecalculateStats(disc, save.Sect);
                    disc.BattleStats.CurHp = disc.BattleStats.MaxHp;
                    if (bus != null)
                    {
                        bus.PublishLog(save.GetImmortalDateText(), LogCategory.Cultivation,
                            string.Format("门人【{0}】潜修水到渠成，晋入【{1}】！",
                                disc.Name, EnumTextMap.RealmFullCn(disc.Realm, disc.SubStage, rCfg.MaxSubStage)));
                    }
                }
            }
        }

        public static bool TryBreakthrough(SIGSaveData save, CharacterData ch, Random rng, SIGEventBus bus, out string message)
        {
            if (save == null || ch == null) { message = "角色数据无效。"; return false; }
            if (ch.Realm >= RealmTier.Immortal) { message = string.Format("【{0}】已证道真仙极境！", ch.Name); return false; }
            if (ch.CurQi < ch.MaxQi - 0.01)
            {
                message = string.Format("【{0}】气海尚未盈满（{1:F0}/{2:F0}），无法冲击瓶颈。", ch.Name, ch.CurQi, ch.MaxQi);
                return false;
            }

            var rCfg = SIGConfigTable.GetRealm(ch.Realm);
            if (ch.SubStage < rCfg.MaxSubStage)
            {
                ch.CurQi = 0;
                ch.SubStage++;
                RecalculateStats(ch, save.Sect);
                ch.BattleStats.CurHp = ch.BattleStats.MaxHp;
                ch.BattleStats.CurMp = ch.BattleStats.MaxMp;

                message = string.Format("真元贯通经脉！【{0}】顺利突破至【{1}】！",
                    ch.Name, EnumTextMap.RealmFullCn(ch.Realm, ch.SubStage, rCfg.MaxSubStage));
                if (bus != null) { bus.PublishLog(save.GetImmortalDateText(), LogCategory.Cultivation, message); bus.NotifyStateChanged(); }
                return true;
            }

            string pillUsedId;
            double winRate = GetBreakthroughSuccessRate(ch, save.Sect, out pillUsedId);
            string pillNote = "";
            if (!string.IsNullOrEmpty(pillUsedId))
            {
                var pCfg = SIGConfigTable.GetItem(pillUsedId);
                save.Sect.AddItem(pillUsedId, -1);
                pillNote = pCfg != null ? string.Format("（已耗1枚【{0}】护脉）", pCfg.NameCn) : "";
            }

            if (rng.NextDouble() <= winRate)
            {
                ch.Realm = (RealmTier)((int)ch.Realm + 1);
                ch.SubStage = 1;
                ch.CurQi = 0;
                RecalculateStats(ch, save.Sect);
                ch.BattleStats.CurHp = ch.BattleStats.MaxHp;
                ch.BattleStats.CurMp = ch.BattleStats.MaxMp;
                save.Sect.AddResource(ResourceType.SectReputation, 30.0 * (int)ch.Realm);

                var nextCfg = SIGConfigTable.GetRealm(ch.Realm);
                message = string.Format("★天劫散尽！【{0}】{1}成功破入【{2}】，寿元增至 {3:F0} 载！",
                    ch.Name, pillNote, EnumTextMap.RealmFullCn(ch.Realm, ch.SubStage, nextCfg.MaxSubStage), ch.MaxLifespanYears);
                if (bus != null) { bus.PublishLog(save.GetImmortalDateText(), LogCategory.Cultivation, message); bus.NotifyStateChanged(); }
                return true;
            }
            else
            {
                ch.CurQi = Math.Round(ch.MaxQi * 0.65, 0);
                ch.BattleStats.CurHp = Math.Max(10.0, Math.Round(ch.BattleStats.CurHp * 0.5, 0));
                message = string.Format("[受挫] 渡劫未果（判定率 {0:P0}）！{1}气海散逸 35%，请重蓄真元再试。", winRate, pillNote);
                if (bus != null) { bus.PublishLog(save.GetImmortalDateText(), LogCategory.Cultivation, message); bus.NotifyStateChanged(); }
                return false;
            }
        }

        public static bool UseCultivationPill(SIGSaveData save, CharacterData ch, string itemId, SIGEventBus bus, out string message)
        {
            if (save == null || ch == null) { message = "目标角色为空。"; return false; }
            if (string.IsNullOrEmpty(itemId)) { message = "该快捷丹药槽尚未配置丹药，请先点击【切换】选择丹药。"; return false; }
            if (save.Sect.GetItemCount(itemId) <= 0)
            {
                var emptyCfg = SIGConfigTable.GetItem(itemId);
                message = string.Format("储物阁中【{0}】库存为 0，请前往丹房炼制。", emptyCfg != null ? emptyCfg.NameCn : itemId);
                return false;
            }

            var item = SIGConfigTable.GetItem(itemId);
            if (item == null || (item.QiGrant <= 0 && item.HealHpRatio <= 0))
            {
                message = "该物品不可作为日常修为/恢复丹药直接服用（破境丹在冲击大境界时自动消耗）。";
                return false;
            }

            save.Sect.AddItem(itemId, -1);
            if (item.QiGrant > 0) ch.CurQi = Math.Min(ch.MaxQi, ch.CurQi + item.QiGrant);
            if (item.HealHpRatio > 0) ch.BattleStats.CurHp = Math.Min(ch.BattleStats.MaxHp, ch.BattleStats.CurHp + ch.BattleStats.MaxHp * item.HealHpRatio);
            if (item.RestoreMpRatio > 0) ch.BattleStats.CurMp = Math.Min(ch.BattleStats.MaxMp, ch.BattleStats.CurMp + ch.BattleStats.MaxMp * item.RestoreMpRatio);

            message = string.Format("【{0}】服用1枚【{1}】（剩余库存 {2}）。", ch.Name, item.NameCn, save.Sect.GetItemCount(itemId));
            if (bus != null) { bus.PublishLog(save.GetImmortalDateText(), LogCategory.Cultivation, message); bus.NotifyStateChanged(); }
            return true;
        }

        public static bool EquipTechnique(SIGSaveData save, CharacterData ch, string techId, SIGEventBus bus, out string message)
        {
            var tech = SIGConfigTable.GetTechnique(techId);
            if (ch == null || tech == null) { message = "未找到对应功法。"; return false; }

            ch.EquippedTechniqueId = tech.Id;
            RecalculateStats(ch, save.Sect);
            message = string.Format("【{0}】已转修主功法：{1}（Lv.{2}/{3}）。",
                ch.Name, tech.NameCn, ch.GetTechniqueLevel(tech.Id), tech.MaxLevel);
            if (bus != null) { bus.PublishLog(save.GetImmortalDateText(), LogCategory.Cultivation, message); bus.NotifyStateChanged(); }
            return true;
        }

        public static bool EquipOrSwapSlot(SIGSaveData save, CharacterData ch, ItemCategory slotCategory, string newItemId, SIGEventBus bus, out string message)
        {
            if (save == null || ch == null) { message = "角色无效。"; return false; }

            string oldId = "";
            if (slotCategory == ItemCategory.Weapon) oldId = ch.EquippedWeaponId;
            else if (slotCategory == ItemCategory.Armor) oldId = ch.EquippedArmorId;
            else if (slotCategory == ItemCategory.Talisman) oldId = ch.EquippedTalismanId;
            else { message = "不支持的装备槽位。"; return false; }

            if (oldId == newItemId)
            {
                message = "装备未变更。";
                return true;
            }

            if (!string.IsNullOrEmpty(newItemId))
            {
                if (save.Sect.GetItemCount(newItemId) <= 0)
                {
                    message = "储物阁中该装备库存不足。";
                    return false;
                }
                save.Sect.AddItem(newItemId, -1);
            }

            if (!string.IsNullOrEmpty(oldId))
            {
                save.Sect.AddItem(oldId, 1);
            }

            if (slotCategory == ItemCategory.Weapon) ch.EquippedWeaponId = newItemId ?? "";
            else if (slotCategory == ItemCategory.Armor) ch.EquippedArmorId = newItemId ?? "";
            else if (slotCategory == ItemCategory.Talisman) ch.EquippedTalismanId = newItemId ?? "";

            RecalculateStats(ch, save.Sect);
            var newCfg = SIGConfigTable.GetItem(newItemId);
            message = string.IsNullOrEmpty(newItemId)
                ? string.Format("【{0}】已卸下{1}归入储物阁。", ch.Name, EnumTextMap.ItemCategoryCn(slotCategory))
                : string.Format("【{0}】已穿戴【{1}】，综合战力 {2:F0}。", ch.Name, newCfg != null ? newCfg.NameCn : newItemId, ch.EstimateCombatPower());

            if (bus != null) { bus.PublishLog(save.GetImmortalDateText(), LogCategory.Cultivation, message); bus.NotifyStateChanged(); }
            return true;
        }
    }
}
