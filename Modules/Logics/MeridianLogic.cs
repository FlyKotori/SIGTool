using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIGTool.Configs;
using SIGTool.Datas;
using SIGTool.Modules.Core;

namespace SIGTool.Modules.Logics
{
    /// <summary>
    /// SYS-02 经脉穴位树贯通、失败保底祝福、单脉贯通及任督小周天/乾坤大周天共鸣结算逻辑
    /// </summary>
    public static class MeridianLogic
    {
        public static string BranchNameCn(MeridianBranchType branch)
        {
            switch (branch)
            {
                case MeridianBranchType.DuMai: return "督脉·乾阳";
                case MeridianBranchType.RenMai: return "任脉·坤阴";
                case MeridianBranchType.ChongMai: return "冲脉·血海";
                case MeridianBranchType.DaiMai: return "带脉·天枢";
                default: return branch.ToString();
            }
        }

        public static string BranchShortCn(MeridianBranchType branch)
        {
            switch (branch)
            {
                case MeridianBranchType.DuMai: return "督脉";
                case MeridianBranchType.RenMai: return "任脉";
                case MeridianBranchType.ChongMai: return "冲脉";
                case MeridianBranchType.DaiMai: return "带脉";
                default: return "奇经";
            }
        }

        public static void EnsureMeridianInitialized(CharacterData ch)
        {
            if (ch == null) return;
            if (ch.UnlockedAcupointIds == null) ch.UnlockedAcupointIds = new List<string>();
            if (ch.AcupointFailPity == null) ch.AcupointFailPity = new Dictionary<string, int>();
        }

        public static bool IsUnlocked(CharacterData ch, string nodeId)
        {
            if (ch == null || string.IsNullOrEmpty(nodeId) || ch.UnlockedAcupointIds == null) return false;
            return ch.UnlockedAcupointIds.Contains(nodeId);
        }

        public static int GetFailPityCount(CharacterData ch, string nodeId)
        {
            if (ch == null || string.IsNullOrEmpty(nodeId) || ch.AcupointFailPity == null) return 0;
            int cnt;
            return ch.AcupointFailPity.TryGetValue(nodeId, out cnt) ? Math.Max(0, cnt) : 0;
        }

        public static double GetEffectiveImpulseRate(CharacterData ch, AcupointNodeConfig node)
        {
            if (node == null) return 0.0;
            int comp = (ch != null && ch.Innate != null) ? ch.Innate.Comprehension : 8;
            int pity = GetFailPityCount(ch, node.Id);
            double rate = node.BaseSuccessRate + comp * 0.008 + pity * 0.08;
            return Math.Min(1.00, Math.Max(0.15, rate));
        }

        public static bool CanAttemptImpulse(
            SIGSaveData save,
            CharacterData ch,
            AcupointNodeConfig node,
            out string blockReason)
        {
            blockReason = "";
            if (save == null || save.Sect == null || ch == null || node == null)
            {
                blockReason = "经脉数据尚未就绪。";
                return false;
            }

            EnsureMeridianInitialized(ch);
            if (IsUnlocked(ch, node.Id))
            {
                blockReason = string.Format("穴位【{0}】已彻底贯通。", node.NameCn);
                return false;
            }

            if (!string.IsNullOrEmpty(node.PrerequisiteId) && !IsUnlocked(ch, node.PrerequisiteId))
            {
                var pre = SIGConfigTable.GetAcupoint(node.PrerequisiteId);
                blockReason = string.Format("需先打通前置穴位【{0}】方可冲击【{1}】。",
                    pre != null ? pre.NameCn : node.PrerequisiteId, node.NameCn);
                return false;
            }

            RealmTier minRealm = node.GetMinRealm();
            if (ch.Realm < minRealm)
            {
                var rCfg = SIGConfigTable.GetRealm(minRealm);
                blockReason = string.Format("肉身道基不足：冲击【{0}】需达到【{1}】。",
                    node.NameCn, rCfg != null ? rCfg.NameCn : minRealm.ToString());
                return false;
            }

            if (ch.CurQi < node.CostQi - 0.01)
            {
                double diffQi = node.CostQi - ch.CurQi;
                double qiSpd = Math.Max(0.1, CultivationLogic.GetQiRatePerSec(ch, save.Sect));
                blockReason = string.Format("当前气海真元不足（需 {0:F0}，现 {1:F0}，约 {2:F0}s 吐纳集齐）。",
                    node.CostQi, ch.CurQi, diffQi / qiSpd);
                return false;
            }

            var sect = save.Sect;
            if (sect.GetResource(ResourceType.SpiritStone) < node.CostSpiritStone ||
                sect.GetResource(ResourceType.SpiritHerb) < node.CostHerb ||
                sect.GetResource(ResourceType.MonsterCore) < node.CostMonsterCore)
            {
                var lacks = new List<string>();
                if (sect.GetResource(ResourceType.SpiritStone) < node.CostSpiritStone)
                    lacks.Add(string.Format("灵石缺 {0:F0}", node.CostSpiritStone - sect.GetResource(ResourceType.SpiritStone)));
                if (sect.GetResource(ResourceType.SpiritHerb) < node.CostHerb)
                    lacks.Add(string.Format("灵草缺 {0:F0}", node.CostHerb - sect.GetResource(ResourceType.SpiritHerb)));
                if (sect.GetResource(ResourceType.MonsterCore) < node.CostMonsterCore)
                    lacks.Add(string.Format("妖丹缺 {0:F0}", node.CostMonsterCore - sect.GetResource(ResourceType.MonsterCore)));

                blockReason = string.Format("冲穴护脉灵材不足（{0}）。", string.Join("、", lacks.ToArray()));
                return false;
            }

            return true;
        }

        public static AcupointNodeConfig FindFirstReadyAcupoint(SIGSaveData save)
        {
            if (save == null || save.Leader == null || SIGConfigTable.Current == null || SIGConfigTable.Current.Acupoints == null)
                return null;

            string dummy;
            for (int i = 0; i < SIGConfigTable.Current.Acupoints.Count; i++)
            {
                var node = SIGConfigTable.Current.Acupoints[i];
                if (CanAttemptImpulse(save, save.Leader, node, out dummy))
                {
                    return node;
                }
            }
            return null;
        }

        public static bool TryImpulseAcupoint(
            SIGSaveData save,
            CharacterData ch,
            string nodeId,
            Random rng,
            SIGEventBus bus,
            out string message)
        {
            var node = SIGConfigTable.GetAcupoint(nodeId);
            if (!CanAttemptImpulse(save, ch, node, out message))
            {
                return false;
            }

            if (rng == null) rng = new Random();

            var prevSum = ComputeMeridianSummary(ch);

            // 扣除真元与护脉资材
            ch.CurQi = Math.Max(0.0, ch.CurQi - node.CostQi);
            if (node.CostSpiritStone > 0) save.Sect.AddResource(ResourceType.SpiritStone, -node.CostSpiritStone);
            if (node.CostHerb > 0) save.Sect.AddResource(ResourceType.SpiritHerb, -node.CostHerb);
            if (node.CostMonsterCore > 0) save.Sect.AddResource(ResourceType.MonsterCore, -node.CostMonsterCore);

            double effRate = GetEffectiveImpulseRate(ch, node);
            if (rng.NextDouble() <= effRate)
            {
                ch.UnlockedAcupointIds.Add(node.Id);
                ch.AcupointFailPity.Remove(node.Id);

                CultivationLogic.RecalculateStats(ch, save.Sect);
                var afterSum = ComputeMeridianSummary(ch);

                string milestoneNote = BuildMilestoneUnlockSuffix(prevSum, afterSum);
                message = string.Format("★冲穴功成！【{0}】贯通{1}要穴【{2}】（{3}/{4}）！{5}",
                    ch.Name,
                    BranchShortCn(node.GetBranch()),
                    node.NameCn,
                    afterSum.UnlockedCount,
                    afterSum.TotalCount,
                    milestoneNote);

                if (bus != null)
                {
                    bus.PublishLog(save.GetImmortalDateText(), LogCategory.Cultivation, message);
                    bus.NotifyStateChanged();
                }
                return true;
            }
            else
            {
                int nextPity = GetFailPityCount(ch, node.Id) + 1;
                ch.AcupointFailPity[node.Id] = nextPity;
                ch.BattleStats.CurHp = Math.Max(10.0, Math.Round(ch.BattleStats.CurHp * 0.92, 0));

                double nextRate = GetEffectiveImpulseRate(ch, node);
                message = string.Format("[冲穴未果] 冲击【{0}】受阻（本次成率 {1:P0}），经脉已蓄势松动，下次成功率提升至 {2:P0}！",
                    node.NameCn, effRate, nextRate);

                if (bus != null)
                {
                    bus.PublishLog(save.GetImmortalDateText(), LogCategory.Cultivation, message);
                    bus.NotifyStateChanged();
                }
                return false;
            }
        }

        /// <summary>
        /// 按批量步进连续冲击指定穴位（一旦成功贯通或资材/气海不足立即自动停手，绝不浪费灵材）
        /// </summary>
        public static bool TryImpulseBatchUntilUnlocked(
            SIGSaveData save,
            CharacterData ch,
            string nodeId,
            int maxTries,
            Random rng,
            SIGEventBus bus,
            out string message)
        {
            message = "";
            int limit = Math.Max(1, Math.Min(50, maxTries));
            int tried = 0;

            for (int i = 0; i < limit; i++)
            {
                string stepMsg;
                bool checkOk = CanAttemptImpulse(save, ch, SIGConfigTable.GetAcupoint(nodeId), out stepMsg);
                if (!checkOk)
                {
                    if (tried == 0) message = stepMsg;
                    else message = string.Format("连续冲穴 {0} 次仍未贯通（已因“{1}”停手，保底成功率已累加）。", tried, stepMsg);
                    return false;
                }

                tried++;
                bool unlocked = TryImpulseAcupoint(save, ch, nodeId, rng, bus, out stepMsg);
                if (unlocked)
                {
                    message = tried > 1
                        ? string.Format("连续催动真元 {0} 次，{1}", tried, stepMsg)
                        : stepMsg;
                    return true;
                }
                message = stepMsg;
            }

            return false;
        }

        private static string BuildMilestoneUnlockSuffix(MeridianBonusSummary before, MeridianBonusSummary after)
        {
            var notes = new List<string>();
            if (!before.DuMaiComplete && after.DuMaiComplete) notes.Add("【督脉小成：功法乘区+12%】");
            if (!before.RenMaiComplete && after.RenMaiComplete) notes.Add("【任脉小成：吐纳+18%/气海+15%】");
            if (!before.ChongMaiComplete && after.ChongMaiComplete) notes.Add("【冲脉小成：血防+15%/汲血+3%】");
            if (!before.DaiMaiComplete && after.DaiMaiComplete) notes.Add("【带脉小成：暴击+5%/渡劫+6%】");
            if (!before.HasXiaoZhouTian && after.HasXiaoZhouTian) notes.Add("★贯通【任督小周天】！");
            if (!before.HasDaZhouTian && after.HasDaZhouTian) notes.Add("★证得【乾坤大周天】圆满！");
            return notes.Count > 0 ? (" " + string.Join(" ", notes.ToArray())) : "";
        }

        public static MeridianBonusSummary ComputeMeridianSummary(CharacterData ch)
        {
            var sum = new MeridianBonusSummary();
            var allNodes = (SIGConfigTable.Current != null && SIGConfigTable.Current.Acupoints != null)
                ? SIGConfigTable.Current.Acupoints
                : new List<AcupointNodeConfig>();

            sum.TotalCount = allNodes.Count;
            if (ch == null || ch.UnlockedAcupointIds == null || ch.UnlockedAcupointIds.Count == 0 || allNodes.Count == 0)
            {
                return sum;
            }

            var unlockedSet = new HashSet<string>(ch.UnlockedAcupointIds);
            int duTotal = 0, renTotal = 0, chongTotal = 0, daiTotal = 0;

            for (int i = 0; i < allNodes.Count; i++)
            {
                var nd = allNodes[i];
                MeridianBranchType br = nd.GetBranch();
                switch (br)
                {
                    case MeridianBranchType.DuMai: duTotal++; break;
                    case MeridianBranchType.RenMai: renTotal++; break;
                    case MeridianBranchType.ChongMai: chongTotal++; break;
                    case MeridianBranchType.DaiMai: daiTotal++; break;
                }

                if (!unlockedSet.Contains(nd.Id)) continue;

                sum.UnlockedCount++;
                switch (br)
                {
                    case MeridianBranchType.DuMai: sum.DuMaiUnlocked++; break;
                    case MeridianBranchType.RenMai: sum.RenMaiUnlocked++; break;
                    case MeridianBranchType.ChongMai: sum.ChongMaiUnlocked++; break;
                    case MeridianBranchType.DaiMai: sum.DaiMaiUnlocked++; break;
                }

                sum.FlatHp += nd.FlatHp;
                sum.FlatMp += nd.FlatMp;
                sum.FlatAtk += nd.FlatAtk;
                sum.FlatDef += nd.FlatDef;
                sum.FlatSpeed += nd.FlatSpeed;
                sum.LifespanYearsAdd += nd.LifespanAdd;
                sum.TechMultiplierAdd += nd.TechMultAdd;
                sum.QiRatePctAdd += nd.QiRatePctAdd;
                sum.CritRateAdd += nd.CritRateAdd;
                sum.DodgeRateAdd += nd.DodgeRateAdd;
                sum.BreakthroughRateAdd += nd.BreakthroughRateAdd;
            }

            sum.DuMaiComplete = (duTotal > 0 && sum.DuMaiUnlocked >= duTotal);
            sum.RenMaiComplete = (renTotal > 0 && sum.RenMaiUnlocked >= renTotal);
            sum.ChongMaiComplete = (chongTotal > 0 && sum.ChongMaiUnlocked >= chongTotal);
            sum.DaiMaiComplete = (daiTotal > 0 && sum.DaiMaiUnlocked >= daiTotal);

            // 单脉全通专精加成
            if (sum.DuMaiComplete)
            {
                sum.TechMultiplierAdd += 0.12;
                sum.PctAtk += 0.10;
            }
            if (sum.RenMaiComplete)
            {
                sum.QiRatePctAdd += 0.18;
                sum.PctMaxQi += 0.15;
                sum.LifespanYearsAdd += 40.0;
            }
            if (sum.ChongMaiComplete)
            {
                sum.PctHp += 0.15;
                sum.PctDef += 0.15;
                sum.LifestealRateAdd += 0.03;
            }
            if (sum.DaiMaiComplete)
            {
                sum.CritRateAdd += 0.05;
                sum.FlatSpeed += 12.0;
                sum.BreakthroughRateAdd += 0.06;
            }

            // 任督二脉小周天 & 四脉乾坤大周天连携
            if (sum.DuMaiComplete && sum.RenMaiComplete)
            {
                sum.HasXiaoZhouTian = true;
                sum.QiRatePctAdd += 0.20;
                sum.TechMultiplierAdd += 0.10;
            }

            if (sum.DuMaiComplete && sum.RenMaiComplete && sum.ChongMaiComplete && sum.DaiMaiComplete)
            {
                sum.HasDaZhouTian = true;
                sum.PctHp += 0.18;
                sum.PctAtk += 0.18;
                sum.PctDef += 0.18;
                sum.QiRatePctAdd += 0.30;
                sum.BreakthroughRateAdd += 0.08;
                sum.LifespanYearsAdd += 150.0;
            }

            return sum;
        }

        public static string FormatCostLine(AcupointNodeConfig node)
        {
            if (node == null) return "";
            var parts = new List<string>();
            if (node.CostQi > 0) parts.Add(string.Format("真元 {0:F0}", node.CostQi));
            if (node.CostSpiritStone > 0) parts.Add(string.Format("灵石 {0:F0}", node.CostSpiritStone));
            if (node.CostHerb > 0) parts.Add(string.Format("灵草 {0:F0}", node.CostHerb));
            if (node.CostMonsterCore > 0) parts.Add(string.Format("妖丹 {0:F0}", node.CostMonsterCore));
            return string.Join(" / ", parts.ToArray());
        }
    }
}
