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
    /// EXT-04 时间快进结算单（不入盘，供开发者测试台与状态栏展示）
    /// </summary>
    public class FastForwardSummary
    {
        public int MonthsSimulated { get; set; }
        public double SimulatedSeconds { get; set; }
        public string StartDateText { get; set; } = "";
        public string EndDateText { get; set; } = "";
        public double LeaderQiDelta { get; set; }
        public int ExpeditionWinsDelta { get; set; }
        public int ExpeditionDefeatsDelta { get; set; }
        public int ArtifactsGainedDelta { get; set; }
        public Dictionary<ResourceType, double> ResourceDeltas { get; set; } = new Dictionary<ResourceType, double>();
    }

    /// <summary>
    /// EXT-04 渡劫突破蒙特卡洛实测报告
    /// </summary>
    public class BreakthroughMonteCarloReport
    {
        public int Trials { get; set; }
        public string TargetRealmName { get; set; } = "";
        public bool UsedPill { get; set; }
        public string PillName { get; set; } = "无";
        public double TheoreticalRate { get; set; }
        public double ObservedFirstTryRate { get; set; }
        public int FirstTrySuccessCount { get; set; }
        public double AvgAttemptsToSucceed { get; set; }
        public int P95AttemptsToSucceed { get; set; }
        public double AvgQiLostBeforeSuccess { get; set; }
        public string FormattedDetail { get; set; } = "";
    }

    /// <summary>
    /// EXT-04 秘境关卡无延迟千场模拟单行统计结果
    /// </summary>
    public class StageBattleMonteCarloRow
    {
        public string StageId { get; set; } = "";
        public string StageName { get; set; } = "";
        public int StageTier { get; set; }
        public int Trials { get; set; }
        public int Wins { get; set; }
        public double WinRate { get; set; }
        public double AvgTurns { get; set; }
        public double AvgElapsedSec { get; set; }
        public double AvgRemainHpRatio { get; set; }
        public double AvgPillsConsumed { get; set; }
        public double AvgStoneDrop { get; set; }
        public double AvgCoreDrop { get; set; }
        public double AvgArtifactDrop { get; set; }
    }

    /// <summary>
    /// EXT-04 开发者数值平衡测试与蒙特卡洛模拟核心逻辑（纯逻辑层，零界面依赖，模拟过程完全隔离不影响真实存档）
    /// </summary>
    public static class DevBalanceLogic
    {
        public static SIGSaveData CloneSaveSnapshot(SIGSaveData source)
        {
            if (source == null) return SaveLoadService.CreateNewSIGSave();
            string json = SimpleJsonSerializer.Serialize(source, false);
            var copy = SimpleJsonSerializer.Deserialize<SIGSaveData>(json) ?? SaveLoadService.CreateNewSIGSave();
            ArtifactFormationLogic.EnsureFormationInitialized(copy.Sect);
            SectBuildLogic.RefreshSectDerivedCaps(copy);
            CultivationLogic.RecalculateAllCharacters(copy);
            return copy;
        }

        public static void GrantAllResources(SIGSaveData save, double amount, SIGEventBus bus, out string message)
        {
            if (save == null || save.Sect == null)
            {
                message = "存档尚未初始化。";
                return;
            }

            foreach (ResourceType rt in Enum.GetValues(typeof(ResourceType)))
            {
                save.Sect.AddResource(rt, amount);
            }

            message = string.Format("[测试台] 已注入全品类宗门资源 +{0:F0}。", amount);
            if (bus != null)
            {
                bus.PublishLog(save.GetImmortalDateText(), LogCategory.System, message);
                bus.NotifyStateChanged();
            }
        }

        public static void GrantAllCatalogItems(SIGSaveData save, int countEach, SIGEventBus bus, out string message)
        {
            if (save == null || save.Sect == null)
            {
                message = "存档尚未初始化。";
                return;
            }

            int step = Math.Max(1, countEach);
            int types = 0;
            foreach (var item in SIGConfigTable.Current.Items)
            {
                save.Sect.AddItem(item.Id, step);
                types++;
            }

            message = string.Format("[测试台] 已向储物阁发放全图鉴 {0} 种丹药与法器，每种 +{1} 件。", types, step);
            if (bus != null)
            {
                bus.PublishLog(save.GetImmortalDateText(), LogCategory.System, message);
                bus.NotifyStateChanged();
            }
        }

        public static void SetLeaderRealmAndState(SIGSaveData save, RealmTier targetRealm, int subStage, bool fillMaxQi, SIGEventBus bus, out string message)
        {
            if (save == null || save.Leader == null)
            {
                message = "掌门数据未就绪。";
                return;
            }

            var rCfg = SIGConfigTable.GetRealm(targetRealm);
            int clampedSub = Math.Max(1, Math.Min(rCfg.MaxSubStage, subStage));
            save.Leader.Realm = targetRealm;
            save.Leader.SubStage = clampedSub;
            CultivationLogic.RecalculateAllCharacters(save);

            if (fillMaxQi)
            {
                save.Leader.CurQi = save.Leader.MaxQi;
            }
            else
            {
                save.Leader.CurQi = Math.Min(save.Leader.CurQi, save.Leader.MaxQi);
            }
            save.Leader.BattleStats.CurHp = save.Leader.BattleStats.MaxHp;
            save.Leader.BattleStats.CurMp = save.Leader.BattleStats.MaxMp;

            string realmText = EnumTextMap.RealmFullCn(targetRealm, clampedSub, rCfg.MaxSubStage);
            message = string.Format("[测试台] 已将掌门境界设为【{0}】{1}，战力 {2:F0}。",
                realmText, fillMaxQi ? "（气海已注满）" : "", save.Leader.EstimateCombatPower());
            if (bus != null)
            {
                bus.PublishLog(save.GetImmortalDateText(), LogCategory.System, message);
                bus.NotifyStateChanged();
            }
        }

        public static void UnlockAndMaxAllFacilities(SIGSaveData save, SIGEventBus bus, out string message)
        {
            if (save == null || save.Sect == null)
            {
                message = "存档尚未初始化。";
                return;
            }

            // 1. 掌门全功法满层 + 奇经24穴全部贯通
            if (save.Leader.TechniqueLevels == null) save.Leader.TechniqueLevels = new Dictionary<string, int>();
            foreach (var tech in SIGConfigTable.Current.Techniques)
            {
                save.Leader.TechniqueLevels[tech.Id] = tech.MaxLevel;
            }
            MeridianLogic.EnsureMeridianInitialized(save.Leader);
            foreach (var acu in SIGConfigTable.Current.Acupoints)
            {
                if (!save.Leader.UnlockedAcupointIds.Contains(acu.Id))
                {
                    save.Leader.UnlockedAcupointIds.Add(acu.Id);
                }
            }
            save.Leader.AcupointFailPity.Clear();

            // 2. 灵脉地块全解锁 + 已建建筑升至满阶
            int upgradedBlds = 0;
            foreach (var slot in save.Sect.GridSlots)
            {
                slot.IsUnlocked = true;
                if (slot.Building != null)
                {
                    var bCfg = SIGConfigTable.GetBuilding(slot.Building.ConfigId);
                    slot.Building.IsUnderConstruction = false;
                    slot.Building.BuildProgressSec = slot.Building.BuildTotalSec;
                    if (bCfg != null)
                    {
                        slot.Building.Level = bCfg.MaxLevel;
                    }
                    upgradedBlds++;
                }
            }

            // 3. 九宫护山阵眼全解锁 + 全秘境关卡解锁
            ArtifactFormationLogic.EnsureFormationInitialized(save.Sect);
            foreach (var eye in save.Sect.FormationEyes)
            {
                eye.IsUnlocked = true;
            }
            foreach (var stg in SIGConfigTable.Current.Stages)
            {
                if (!save.Combat.UnlockedStageIds.Contains(stg.Id))
                {
                    save.Combat.UnlockedStageIds.Add(stg.Id);
                }
            }

            SectBuildLogic.RefreshSectDerivedCaps(save);
            save.Sect.BarrierCurHp = save.Sect.BarrierMaxHp;
            CultivationLogic.RecalculateAllCharacters(save);

            message = string.Format("[测试台] 已贯通奇经全24穴、满级全部功法、解锁全灵脉/全九宫阵眼/全秘境，并将 {0} 座现有建筑升至满阶。", upgradedBlds);
            if (bus != null)
            {
                bus.PublishLog(save.GetImmortalDateText(), LogCategory.System, message);
                bus.NotifyStateChanged();
            }
        }

        public static void SpawnTestArtifactsFree(SIGSaveData save, Random rng, int forgeGrade, int count, SIGEventBus bus, out string message)
        {
            if (save == null || save.Sect == null)
            {
                message = "存档尚未初始化。";
                return;
            }

            ArtifactFormationLogic.EnsureFormationInitialized(save.Sect);
            int n = Math.Max(1, Math.Min(50, count));
            int grade = Math.Max(1, Math.Min(3, forgeGrade));
            if (save.Sect.ArtifactVault.Count + n > save.Sect.ArtifactVaultCap)
            {
                save.Sect.ArtifactVaultCap = save.Sect.ArtifactVault.Count + n + 20;
            }

            int immortalCount = 0;
            int treasureCount = 0;
            for (int i = 0; i < n; i++)
            {
                var inst = ArtifactFormationLogic.GenerateRandomArtifact(save.Sect, rng ?? new Random(), grade, 4);
                save.Sect.ArtifactVault.Add(inst);
                if (inst.Rarity == ArtifactRarity.Immortal) immortalCount++;
                else if (inst.Rarity == ArtifactRarity.Treasure) treasureCount++;
            }

            string gradeCn = grade == 3 ? "仙铸" : (grade == 2 ? "灵铸" : "凡铸");
            message = string.Format("[测试台] 已免耗材生成 {0} 件【{1}】随机词条阵宝（其中仙品 {2} 件、宝品 {3} 件）。",
                n, gradeCn, immortalCount, treasureCount);
            if (bus != null)
            {
                bus.PublishLog(save.GetImmortalDateText(), LogCategory.Artifact, message);
                bus.NotifyStateChanged();
            }
        }

        /// <summary>
        /// EXT-04 ③：渡劫突破千次实测统计（不影响真实存档）
        /// </summary>
        public static BreakthroughMonteCarloReport SimulateBreakthroughMonteCarlo(
            SIGSaveData liveSave,
            RealmTier realm,
            bool useRecommendedPill,
            int trials,
            Random rng)
        {
            int n = Math.Max(10, Math.Min(20000, trials));
            if (rng == null) rng = new Random();

            var sim = CloneSaveSnapshot(liveSave);
            var rCfg = SIGConfigTable.GetRealm(realm);
            sim.Leader.Realm = realm;
            sim.Leader.SubStage = rCfg.MaxSubStage;
            CultivationLogic.RecalculateStats(sim.Leader, sim.Sect);

            string recPillId = rCfg.RecommendedPillId ?? "";
            string pillName = "不使用破境丹";
            if (!string.IsNullOrEmpty(recPillId))
            {
                sim.Sect.ItemInventory[recPillId] = useRecommendedPill ? (n * 50) : 0;
                if (useRecommendedPill)
                {
                    var pCfg = SIGConfigTable.GetItem(recPillId);
                    pillName = pCfg != null
                        ? string.Format("{0} (+{1:P0})", pCfg.NameCn, pCfg.BreakthroughRateBonus)
                        : recPillId;
                }
            }

            string usedIdOut;
            double theoreticalRate = CultivationLogic.GetBreakthroughSuccessRate(sim.Leader, sim.Sect, out usedIdOut);

            int firstTrySuccesses = 0;
            var attemptsList = new List<int>(n);
            double totalQiLost = 0.0;
            double failQiLossOnce = Math.Round(sim.Leader.MaxQi * 0.35, 0);

            for (int i = 0; i < n; i++)
            {
                int attempts = 0;
                while (attempts < 500)
                {
                    attempts++;
                    bool ok = (rng.NextDouble() <= theoreticalRate);
                    if (attempts == 1 && ok) firstTrySuccesses++;
                    if (ok) break;
                    totalQiLost += failQiLossOnce;
                }
                attemptsList.Add(attempts);
            }

            attemptsList.Sort();
            int p95Idx = Math.Min(attemptsList.Count - 1, (int)Math.Ceiling(attemptsList.Count * 0.95) - 1);
            int p95 = attemptsList[Math.Max(0, p95Idx)];
            double obsRate = (double)firstTrySuccesses / n;
            double avgAttempts = attemptsList.Average();
            double avgQiLost = totalQiLost / n;

            var nextRealm = (int)realm < (int)RealmTier.Immortal ? (RealmTier)((int)realm + 1) : RealmTier.Immortal;
            string targetTitle = string.Format("{0}圆满 ➔ {1}", rCfg.NameCn, SIGConfigTable.GetRealm(nextRealm).NameCn);

            var sb = new StringBuilder();
            sb.AppendFormat("【渡劫突破 {0} 次独立实测报告】 目标：{1} | 护脉丹药：{2}", n, targetTitle, pillName).AppendLine();
            sb.AppendFormat("  • 面板条件：悟性 {0} / 气运 {1} / 大阵破境加成 +{2:P1} / 圆满气海 {3:F0}",
                sim.Leader.Innate.Comprehension,
                sim.Leader.Innate.Fortune,
                ArtifactFormationLogic.ComputeFormationSummary(sim.Sect).BreakthroughRateAdd,
                sim.Leader.MaxQi).AppendLine();
            sb.AppendFormat("  • 理论单次成功率：{0:P2}   |   实测首次成功率：{1:P2}（{2}/{3} 次）",
                theoreticalRate, obsRate, firstTrySuccesses, n).AppendLine();
            sb.AppendFormat("  • 成功所需平均冲击次数：{0:F2} 次（若备足护脉丹则期望消耗 {0:F2} 枚）", avgAttempts).AppendLine();
            sb.AppendFormat("  • 95% 分位保底冲击次数：{0} 次   |   成功前平均散逸修为损耗：{1:F0} 点气海", p95, avgQiLost);

            return new BreakthroughMonteCarloReport
            {
                Trials = n,
                TargetRealmName = targetTitle,
                UsedPill = useRecommendedPill,
                PillName = pillName,
                TheoreticalRate = theoreticalRate,
                ObservedFirstTryRate = obsRate,
                FirstTrySuccessCount = firstTrySuccesses,
                AvgAttemptsToSucceed = avgAttempts,
                P95AttemptsToSucceed = p95,
                AvgQiLostBeforeSuccess = avgQiLost,
                FormattedDetail = sb.ToString()
            };
        }

        /// <summary>
        /// EXT-04 ④：单秘境关卡无延迟 N 场真实 ATB 模拟（完全基于克隆快照，不污染真实存档）
        /// </summary>
        public static StageBattleMonteCarloRow SimulateStageMonteCarlo(
            SIGSaveData liveSave,
            string stageId,
            int trials,
            bool infiniteAutoHealPills,
            Random rng)
        {
            var stage = SIGConfigTable.GetStage(stageId);
            if (stage == null) return null;

            int n = Math.Max(1, Math.Min(5000, trials));
            if (rng == null) rng = new Random();

            var sim = CloneSaveSnapshot(liveSave);
            if (!sim.Combat.UnlockedStageIds.Contains(stage.Id))
            {
                sim.Combat.UnlockedStageIds.Add(stage.Id);
            }

            // 记录初始基准库存，便于每场无分配重置而不重复反序列化整份存档
            var baseInventory = new Dictionary<string, int>(sim.Sect.ItemInventory);
            sim.Sect.ArtifactVaultCap = Math.Max(sim.Sect.ArtifactVaultCap, sim.Sect.ArtifactVault.Count + n * 4 + 50);

            string leaderAutoPill = (sim.Leader.QuickCombatPills != null && sim.Leader.QuickCombatPills.Count > 0)
                ? sim.Leader.QuickCombatPills[0]
                : "";

            int wins = 0;
            long sumTurns = 0;
            double sumSec = 0.0;
            double sumRemainHpRatio = 0.0;
            long sumPillsUsed = 0;
            double sumStone = 0.0;
            double sumCores = 0.0;
            int sumArtifacts = 0;

            for (int i = 0; i < n; i++)
            {
                // 恢复每场满状态与丹药基准库存
                sim.Leader.BattleStats.CurHp = sim.Leader.BattleStats.MaxHp;
                sim.Leader.BattleStats.CurMp = sim.Leader.BattleStats.MaxMp;
                foreach (var d in sim.Disciples)
                {
                    d.BattleStats.CurHp = d.BattleStats.MaxHp;
                    d.BattleStats.CurMp = d.BattleStats.MaxMp;
                }

                sim.Sect.ItemInventory.Clear();
                foreach (var kv in baseInventory)
                {
                    sim.Sect.ItemInventory[kv.Key] = kv.Value;
                }

                int startPillStock = 0;
                if (!string.IsNullOrEmpty(leaderAutoPill))
                {
                    if (infiniteAutoHealPills)
                    {
                        sim.Sect.ItemInventory[leaderAutoPill] = 9999;
                    }
                    startPillStock = sim.Sect.GetItemCount(leaderAutoPill);
                }
                // 同时为护山剑卫门人补充测试丹药计数
                foreach (var g in sim.Disciples.Where(d => d.AssignedJob == DiscipleJob.Guard).Take(2))
                {
                    string gp = (g.QuickCombatPills != null && g.QuickCombatPills.Count > 0) ? g.QuickCombatPills[0] : "";
                    if (!string.IsNullOrEmpty(gp) && infiniteAutoHealPills && gp != leaderAutoPill)
                    {
                        sim.Sect.ItemInventory[gp] = 9999;
                    }
                }

                double stoneBefore = sim.Sect.GetResource(ResourceType.SpiritStone);
                double coreBefore = sim.Sect.GetResource(ResourceType.MonsterCore);
                int artBefore = sim.Sect.ArtifactVault.Count;
                int vicBefore = sim.Combat.TotalVictories;


                string startMsg;
                if (!CombatLogic.StartExpedition(sim, stage.Id, false, null, out startMsg))
                {
                    break;
                }

                int safetySteps = 0;
                while (sim.Combat.IsInBattle && safetySteps < 800)
                {
                    CombatLogic.TickCombat(sim, 0.5, rng, null);
                    safetySteps++;
                }

                bool won = (sim.Combat.TotalVictories > vicBefore);
                if (won) wins++;

                sumTurns += sim.Combat.TurnCount;
                sumSec += sim.Combat.BattleElapsedSec;

                var leaderUnit = sim.Combat.Allies.FirstOrDefault(u => u.UnitId == sim.Leader.Id);
                double hpRatio = (won && leaderUnit != null && leaderUnit.MaxHp > 0)
                    ? Math.Max(0.0, leaderUnit.CurHp / leaderUnit.MaxHp)
                    : 0.0;
                sumRemainHpRatio += hpRatio;

                if (!string.IsNullOrEmpty(leaderAutoPill))
                {
                    int endStock = sim.Sect.GetItemCount(leaderAutoPill);
                    if (startPillStock > endStock) sumPillsUsed += (startPillStock - endStock);
                }

                sumStone += Math.Max(0.0, sim.Sect.GetResource(ResourceType.SpiritStone) - stoneBefore);
                sumCores += Math.Max(0.0, sim.Sect.GetResource(ResourceType.MonsterCore) - coreBefore);
                sumArtifacts += Math.Max(0, sim.Sect.ArtifactVault.Count - artBefore);
            }

            return new StageBattleMonteCarloRow
            {
                StageId = stage.Id,
                StageName = stage.NameCn,
                StageTier = stage.StageTier,
                Trials = n,
                Wins = wins,
                WinRate = (double)wins / n,
                AvgTurns = (double)sumTurns / n,
                AvgElapsedSec = sumSec / n,
                AvgRemainHpRatio = sumRemainHpRatio / n,
                AvgPillsConsumed = (double)sumPillsUsed / n,
                AvgStoneDrop = sumStone / n,
                AvgCoreDrop = sumCores / n,
                AvgArtifactDrop = (double)sumArtifacts / n
            };
        }

        public static List<StageBattleMonteCarloRow> SimulateAllStagesSweep(
            SIGSaveData liveSave,
            int trialsPerStage,
            bool infiniteAutoHealPills,
            Random rng)
        {
            var list = new List<StageBattleMonteCarloRow>();
            if (SIGConfigTable.Current == null || SIGConfigTable.Current.Stages == null) return list;
            foreach (var stg in SIGConfigTable.Current.Stages)
            {
                var row = SimulateStageMonteCarlo(liveSave, stg.Id, trialsPerStage, infiniteAutoHealPills, rng);
                if (row != null) list.Add(row);
            }
            return list;
        }

        /// <summary>
        /// SYS-03 调试辅助：立即轮转一次九州岁运、刷新坊市行情并强制入队一条奇遇事件
        /// </summary>
        public static string TriggerTestEncounterAndShiftTrend(SIGSaveData save, Random rng, SIGEventBus bus)
        {
            if (save == null) return "存档为空。";
            WorldEvolutionLogic.EnsureWorldInitialized(save);
            save.World.MonthsUntilTrendShift = 1;
            WorldEvolutionLogic.AdvanceMonthlyWorldEvolution(save, rng, allowEncounterTrigger: false, bus: bus);
            bool spawned = WorldEvolutionLogic.TriggerRandomMonthlyEncounter(save, rng, bus);
            WorldEvolutionLogic.SyncSectWorldModifiers(save);
            if (bus != null) bus.NotifyStateChanged();
            return string.Format("已轮转岁运至【{0}】并刷新坊市行情{1}（当前待决奇遇 {2}/3）。",
                WorldEvolutionLogic.TrendNameCn(save.World.CurrentTrend),
                spawned ? "，且新触发 1 桩奇遇" : "",
                save.World.PendingEncounters.Count);
        }
    }
}
