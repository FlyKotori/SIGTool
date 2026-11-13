using System;
using System.Collections.Generic;
using System.Linq;
using SIGTool.Configs;
using SIGTool.Datas;
using SIGTool.Modules.Core;

namespace SIGTool.Modules.Logics
{
    /// <summary>
    /// 秘境挂机连战、护山兽潮防卫、ATB行动条演算、阵宝吸血与秘境首领随机词条法宝掉落逻辑
    /// </summary>
    public static class CombatLogic
    {
        public static bool StartExpedition(SIGSaveData save, string stageId, bool autoRepeat, SIGEventBus bus, out string message)
        {
            if (save == null) { message = "存档未就绪。"; return false; }

            var stage = SIGConfigTable.GetStage(stageId);
            if (stage == null) { message = "未找到指定的秘境关卡。"; return false; }

            if (!save.Combat.UnlockedStageIds.Contains(stage.Id))
            {
                message = string.Format("尚未解锁【{0}】，请先通关前置秘境。", stage.NameCn);
                return false;
            }

            CultivationLogic.RecalculateAllCharacters(save);

            var cs = save.Combat;
            cs.IsInBattle = true;
            cs.Mode = CombatMode.IdleExpedition;
            cs.StageId = stage.Id;
            cs.StageName = stage.NameCn;
            cs.AutoRepeatExpedition = autoRepeat;
            cs.CurrentWave = 1;
            cs.TotalWaves = Math.Max(1, stage.WaveCount);
            cs.TurnCount = 0;
            cs.BattleElapsedSec = 0.0;
            cs.RestCooldownSec = 0.0;
            cs.RecentBattleLogs.Clear();

            BuildAllyParty(save);
            SpawnStageWaveEnemies(save, stage, 1);

            cs.AppendBattleLog(string.Format("=== 率队踏入【{0}】(第 1/{1} 波) ===", stage.NameCn, cs.TotalWaves));
            message = string.Format("已出征【{0}】（挂机连战：{1}）。", stage.NameCn, autoRepeat ? "开启" : "关闭");
            if (bus != null) { bus.PublishLog(save.GetImmortalDateText(), LogCategory.Combat, message); bus.NotifyStateChanged(); }
            return true;
        }

        public static void StartSectRaid(SIGSaveData save, SIGEventBus bus)
        {
            if (save == null) return;
            CultivationLogic.RecalculateAllCharacters(save);

            var cs = save.Combat;
            int raidLv = Math.Max(1, cs.RaidWaveLevel);
            cs.IsInBattle = true;
            cs.Mode = CombatMode.SectRaidDefense;
            cs.StageName = string.Format("第 {0} 轮护山兽潮攻城", raidLv);
            cs.CurrentWave = 1;
            cs.TotalWaves = 2;
            cs.TurnCount = 0;
            cs.BattleElapsedSec = 0.0;
            cs.RestCooldownSec = 0.0;
            cs.RecentBattleLogs.Clear();

            BuildAllyParty(save);
            SpawnRaidEnemies(save, raidLv, 1);

            string alert = string.Format("[警报] 【{0}】兵临山门，护山剑阵与迎战门人已列阵御敌！", cs.StageName);
            cs.AppendBattleLog(alert);
            if (bus != null) { bus.PublishLog(save.GetImmortalDateText(), LogCategory.Combat, alert); bus.NotifyRaidAlert(); bus.NotifyStateChanged(); }
        }

        public static bool RetreatCombat(SIGSaveData save, SIGEventBus bus, out string message)
        {
            if (save == null || !save.Combat.IsInBattle)
            {
                if (save != null)
                {
                    save.Combat.RestCooldownSec = 0.0;
                    save.Combat.AutoRepeatExpedition = false;
                }
                message = "当前已处于山门休整状态。";
                return false;
            }

            if (save.Combat.Mode == CombatMode.SectRaidDefense)
            {
                message = "护山兽潮正在猛攻山门大阵，不可弃宗撤退！";
                return false;
            }

            save.Combat.IsInBattle = false;
            save.Combat.AutoRepeatExpedition = false;
            save.Combat.RestCooldownSec = 0.0;
            save.Combat.AppendBattleLog("掌门掐诀施展遁光，率队安然撤回宗门。");

            message = "已安全撤回宗门休整。";
            if (bus != null) { bus.PublishLog(save.GetImmortalDateText(), LogCategory.Combat, message); bus.NotifyStateChanged(); }
            return true;
        }

        private static void BuildAllyParty(SIGSaveData save)
        {
            save.Combat.Allies.Clear();
            save.Leader.EnsureQuickSlotsInitialized();
            save.Combat.Allies.Add(CreateCombatUnitFromCharacter(save.Leader));

            var guards = save.Disciples.Where(d => d.AssignedJob == DiscipleJob.Guard).Take(2).ToList();
            foreach (var g in guards)
            {
                g.EnsureQuickSlotsInitialized();
                save.Combat.Allies.Add(CreateCombatUnitFromCharacter(g));
            }
        }

        private static CombatUnitRuntime CreateCombatUnitFromCharacter(CharacterData ch)
        {
            string autoPill = (ch.QuickCombatPills != null && ch.QuickCombatPills.Count > 0) ? ch.QuickCombatPills[0] : "";
            return new CombatUnitRuntime
            {
                UnitId = ch.Id,
                Name = ch.Name,
                IsPlayerSide = true,
                IsBoss = false,
                Element = ch.Innate.RootElement,
                MaxHp = ch.BattleStats.MaxHp,
                CurHp = Math.Max(ch.BattleStats.MaxHp * 0.5, ch.BattleStats.CurHp),
                MaxMp = ch.BattleStats.MaxMp,
                CurMp = Math.Max(ch.BattleStats.MaxMp * 0.5, ch.BattleStats.CurMp),
                Attack = ch.BattleStats.Attack,
                Defense = ch.BattleStats.Defense,
                Speed = ch.BattleStats.Speed,
                CritRate = ch.BattleStats.CritRate,
                DodgeRate = ch.BattleStats.DodgeRate,
                LifestealRate = ch.BattleStats.LifestealRate,
                ActionGauge = 25.0,
                ShieldHp = 0.0,
                SkillCooldownTurns = 0,
                EquippedTalismanId = ch.EquippedTalismanId,
                AutoPillItemId = autoPill,
                AutoHealRatio = ch.AutoHealHpRatio
            };
        }

        private static void SpawnStageWaveEnemies(SIGSaveData save, CombatStageConfig stage, int waveIndex)
        {
            save.Combat.Enemies.Clear();
            bool isFinalBossWave = (waveIndex >= stage.WaveCount) && !string.IsNullOrEmpty(stage.BossEnemyId);

            if (isFinalBossWave)
            {
                var bossCfg = SIGConfigTable.GetEnemy(stage.BossEnemyId);
                if (bossCfg != null) save.Combat.Enemies.Add(CreateEnemyUnit(bossCfg, 1.0, 0));
                if (stage.NormalEnemyIds != null && stage.NormalEnemyIds.Count > 0)
                {
                    var guardCfg = SIGConfigTable.GetEnemy(stage.NormalEnemyIds[0]);
                    if (guardCfg != null) save.Combat.Enemies.Add(CreateEnemyUnit(guardCfg, 0.9, 1));
                }
            }
            else
            {
                var pool = stage.NormalEnemyIds ?? new List<string>();
                int count = Math.Min(3, 1 + waveIndex);
                for (int i = 0; i < count && pool.Count > 0; i++)
                {
                    var eCfg = SIGConfigTable.GetEnemy(pool[i % pool.Count]);
                    if (eCfg != null) save.Combat.Enemies.Add(CreateEnemyUnit(eCfg, 1.0, i));
                }
            }
        }

        private static void SpawnRaidEnemies(SIGSaveData save, int raidLv, int waveIndex)
        {
            save.Combat.Enemies.Clear();
            double scale = Math.Pow(1.45, Math.Max(0, raidLv - 1));
            if (waveIndex == 1)
            {
                var w1 = SIGConfigTable.GetEnemy("ENM_WOLF");
                var w2 = SIGConfigTable.GetEnemy("ENM_SNAKE");
                if (w1 != null) save.Combat.Enemies.Add(CreateEnemyUnit(w1, scale * 1.15, 0));
                if (w2 != null) save.Combat.Enemies.Add(CreateEnemyUnit(w2, scale * 1.15, 1));
            }
            else
            {
                string bossId = raidLv >= 3 ? "BOSS_FLAME_LIZARD" : "BOSS_MIST_TIGER";
                var boss = SIGConfigTable.GetEnemy(bossId);
                if (boss != null) save.Combat.Enemies.Add(CreateEnemyUnit(boss, scale * 1.10, 0));
            }
        }

        private static CombatUnitRuntime CreateEnemyUnit(EnemyTemplateConfig cfg, double scale, int seq)
        {
            return new CombatUnitRuntime
            {
                UnitId = string.Format("{0}_{1}", cfg.Id, seq),
                SourceTemplateId = cfg.Id,
                Name = seq > 0 && !cfg.IsBoss ? string.Format("{0}#{1}", cfg.NameCn, seq + 1) : cfg.NameCn,
                IsPlayerSide = false,
                IsBoss = cfg.IsBoss,
                Element = cfg.GetElement(),
                MaxHp = Math.Round(cfg.MaxHp * scale, 0),
                CurHp = Math.Round(cfg.MaxHp * scale, 0),
                MaxMp = 100,
                CurMp = 100,
                Attack = Math.Round(cfg.Attack * scale, 1),
                Defense = Math.Round(cfg.Defense * scale, 1),
                Speed = cfg.Speed,
                CritRate = cfg.IsBoss ? 0.12 : 0.05,
                DodgeRate = 0.04,
                ActionGauge = 0.0
            };
        }

        public static void TickCombat(SIGSaveData save, double simDeltaSec, Random rng, SIGEventBus bus)
        {
            if (save == null || simDeltaSec <= 0) return;
            var cs = save.Combat;

            if (!cs.IsInBattle)
            {
                if (cs.AutoRepeatExpedition && cs.RestCooldownSec > 0)
                {
                    cs.RestCooldownSec -= simDeltaSec;
                    if (cs.RestCooldownSec <= 0)
                    {
                        cs.RestCooldownSec = 0;
                        save.Leader.BattleStats.CurHp = save.Leader.BattleStats.MaxHp;
                        save.Leader.BattleStats.CurMp = save.Leader.BattleStats.MaxMp;
                        string dummy;
                        StartExpedition(save, cs.StageId, true, null, out dummy);
                    }
                }
                return;
            }

            cs.BattleElapsedSec += simDeltaSec;

            var leaderAlly = cs.Allies.FirstOrDefault(u => u.UnitId == save.Leader.Id);
            if (leaderAlly != null && save.Leader.QuickCombatPills != null && save.Leader.QuickCombatPills.Count > 0)
            {
                leaderAlly.AutoPillItemId = save.Leader.QuickCombatPills[0];
            }

            var activeUnits = cs.Allies.Concat(cs.Enemies).Where(u => u.IsAlive).ToList();
            foreach (var u in activeUnits)
            {
                u.ActionGauge += u.Speed * 0.65 * simDeltaSec;
            }

            foreach (var actor in activeUnits.Where(u => u.IsAlive && u.ActionGauge >= 100.0).OrderByDescending(u => u.ActionGauge))
            {
                actor.ActionGauge -= 100.0;
                cs.TurnCount++;
                ExecuteUnitTurn(save, actor, rng, bus);

                if (!cs.Enemies.Any(e => e.IsAlive) || !cs.Allies.Any(a => a.IsAlive)) break;
            }

            if (cs.Mode == CombatMode.SectRaidDefense && cs.TurnCount > 0 && cs.TurnCount % 5 == 0 && cs.Enemies.Any(e => e.IsAlive))
            {
                double towerDmg = SectBuildLogic.GetDefenseTowerSupportDamage(save);
                if (towerDmg > 0)
                {
                    var target = cs.Enemies.First(e => e.IsAlive);
                    target.CurHp = Math.Max(0, target.CurHp - towerDmg);
                    cs.AppendBattleLog(string.Format("[剑阵]【诛邪剑台】万剑齐发，轰击 {0} 造成 {1:F0} 剑阵真伤！", target.Name, towerDmg));
                }
            }

            if (!cs.Enemies.Any(e => e.IsAlive)) HandleWaveCleared(save, rng, bus);
            else if (!cs.Allies.Any(a => a.IsAlive)) HandleBattleDefeat(save, bus);
        }

        private static void ExecuteUnitTurn(SIGSaveData save, CombatUnitRuntime actor, Random rng, SIGEventBus bus)
        {
            var cs = save.Combat;
            if (actor.SkillCooldownTurns > 0) actor.SkillCooldownTurns--;

            if (actor.IsPlayerSide && !string.IsNullOrEmpty(actor.AutoPillItemId))
            {
                if (actor.CurHp / Math.Max(1.0, actor.MaxHp) <= actor.AutoHealRatio &&
                    save.Sect.GetItemCount(actor.AutoPillItemId) > 0)
                {
                    var pill = SIGConfigTable.GetItem(actor.AutoPillItemId);
                    if (pill != null && pill.HealHpRatio > 0)
                    {
                        save.Sect.AddItem(pill.Id, -1);
                        double healVal = Math.Round(actor.MaxHp * pill.HealHpRatio, 0);
                        double mpVal = Math.Round(actor.MaxMp * pill.RestoreMpRatio, 0);
                        actor.CurHp = Math.Min(actor.MaxHp, actor.CurHp + healVal);
                        actor.CurMp = Math.Min(actor.MaxMp, actor.CurMp + mpVal);
                        cs.AppendBattleLog(string.Format("[服丹] {0} 自动触发战丹槽1【{1}】，恢复气血 +{2:F0}！", actor.Name, pill.NameCn, healVal));
                    }
                }
            }

            var targets = (actor.IsPlayerSide ? cs.Enemies : cs.Allies).Where(t => t.IsAlive).ToList();
            if (targets.Count == 0) return;
            var target = targets[0];

            double skillMult = 1.0;
            string actionLabel = "普攻";
            ElementType attackElem = actor.Element;

            if (actor.IsPlayerSide && !string.IsNullOrEmpty(actor.EquippedTalismanId) && actor.SkillCooldownTurns <= 0)
            {
                var tal = SIGConfigTable.GetItem(actor.EquippedTalismanId);
                if (tal != null && tal.ActiveSkillMultiplier > 0 && actor.CurMp >= tal.ActiveSkillMpCost)
                {
                    actor.CurMp -= tal.ActiveSkillMpCost;
                    actor.SkillCooldownTurns = Math.Max(1, tal.CooldownTurns);
                    skillMult = tal.ActiveSkillMultiplier;
                    actionLabel = string.Format("法宝技【{0}】", tal.ActiveSkillName);
                    if (tal.GetElement() != ElementType.None) attackElem = tal.GetElement();

                    if (tal.ShieldMaxHpRatio > 0)
                    {
                        double shieldGain = Math.Round(actor.MaxHp * tal.ShieldMaxHpRatio, 0);
                        actor.ShieldHp += shieldGain;
                        cs.AppendBattleLog(string.Format("[护盾] {0} 祭出【{1}】，凝结 {2:F0} 护盾！", actor.Name, tal.NameCn, shieldGain));
                    }
                }
            }
            else if (!actor.IsPlayerSide && actor.IsBoss && actor.SkillCooldownTurns <= 0)
            {
                actor.SkillCooldownTurns = 3;
                skillMult = 1.45;
                actionLabel = "凶威重击";
            }

            if (rng.NextDouble() < target.DodgeRate * 0.5)
            {
                cs.AppendBattleLog(string.Format("[闪避] {0} 施展{1}，被 {2} 闪避！", actor.Name, actionLabel, target.Name));
                return;
            }

            double rawDmg = Math.Max(actor.Attack * 0.20, actor.Attack * skillMult - target.Defense * 0.55);
            string elemTag = "";
            if (EnumTextMap.IsOvercoming(attackElem, target.Element))
            {
                rawDmg *= 1.22;
                elemTag = "[克制]";
            }

            bool isCrit = rng.NextDouble() < actor.CritRate;
            if (isCrit) rawDmg *= 1.55;

            double finalDmg = Math.Max(1.0, Math.Round(rawDmg, 0));

            if (!actor.IsPlayerSide && cs.Mode == CombatMode.SectRaidDefense && save.Sect.BarrierCurHp > 0)
            {
                double absorb = Math.Min(save.Sect.BarrierCurHp, Math.Round(finalDmg * 0.55, 0));
                save.Sect.BarrierCurHp -= absorb;
                finalDmg = Math.Max(1.0, finalDmg - absorb);
            }

            ApplyDamageToUnit(target, finalDmg);

            string leechNote = "";
            if (actor.IsAlive && actor.LifestealRate > 0.001)
            {
                double leech = Math.Max(1.0, Math.Round(finalDmg * actor.LifestealRate, 0));
                actor.CurHp = Math.Min(actor.MaxHp, actor.CurHp + leech);
                leechNote = string.Format(" [汲血+{0:F0}]", leech);
            }

            SyncLeaderHpMpSnapshot(save);

            cs.AppendBattleLog(string.Format("{0} ➔ {1} {2}{3}{4} -{5:F0}{6} (HP {7:F0}/{8:F0})",
                actor.Name, target.Name, actionLabel, elemTag, isCrit ? "[暴!]" : "", finalDmg, leechNote, target.CurHp, target.MaxHp));

            if (!target.IsAlive && !target.IsPlayerSide) GrantEnemyDrop(save, target, rng, cs, bus);
        }

        private static void ApplyDamageToUnit(CombatUnitRuntime target, double dmg)
        {
            if (target.ShieldHp > 0)
            {
                double absorbed = Math.Min(target.ShieldHp, dmg);
                target.ShieldHp -= absorbed;
                dmg -= absorbed;
            }
            target.CurHp = Math.Max(0.0, target.CurHp - dmg);
        }

        private static void GrantEnemyDrop(SIGSaveData save, CombatUnitRuntime deadEnemy, Random rng, CombatStateData cs, SIGEventBus bus)
        {
            var cfg = SIGConfigTable.GetEnemy(deadEnemy.SourceTemplateId);
            if (cfg == null) return;

            save.Sect.AddResource(ResourceType.SpiritStone, cfg.DropSpiritStone);
            save.Sect.AddResource(ResourceType.SpiritHerb, cfg.DropHerb);
            save.Sect.AddResource(ResourceType.SpiritOre, cfg.DropOre);

            double effCoreDrop = cfg.DropMonsterCore * WorldEvolutionLogic.GetMonsterCoreDropMultiplier(save);
            int cores = 0;
            if (effCoreDrop >= 1.0)
            {
                cores = (int)Math.Floor(effCoreDrop);
                if (rng.NextDouble() < (effCoreDrop - cores)) cores++;
            }
            else if (rng.NextDouble() < effCoreDrop) cores = 1;
            if (cores > 0) save.Sect.AddResource(ResourceType.MonsterCore, cores);

            string extraLoot = "";
            if (!string.IsNullOrEmpty(cfg.DropSpecialItemId) && rng.NextDouble() <= cfg.DropSpecialChance)
            {
                save.Sect.AddItem(cfg.DropSpecialItemId, 1);
                var spItem = SIGConfigTable.GetItem(cfg.DropSpecialItemId);
                extraLoot = string.Format(" ★掉落【{0}】!", spItem != null ? spItem.NameCn : cfg.DropSpecialItemId);
            }

            // SYS-01 秘境首领击败时概率掉落随机词条阵宝
            if (cfg.IsBoss && save.Sect.ArtifactVault.Count < save.Sect.ArtifactVaultCap)
            {
                var stg = SIGConfigTable.GetStage(cs.StageId);
                int stgTier = stg != null ? stg.StageTier : 1;
                double dropChance = 0.35 + stgTier * 0.06;
                if (rng.NextDouble() <= dropChance)
                {
                    int dropGrade = stgTier >= 4 ? 3 : (stgTier >= 3 ? 2 : 1);
                    var droppedArt = ArtifactFormationLogic.GenerateRandomArtifact(save.Sect, rng, dropGrade, stgTier);
                    save.Sect.ArtifactVault.Add(droppedArt);
                    extraLoot += string.Format(" ★获阵宝【{0}】({1}词条)!", droppedArt.NameCn, droppedArt.Affixes.Count);
                    if (bus != null)
                    {
                        bus.PublishLog(save.GetImmortalDateText(), LogCategory.Artifact,
                            string.Format("秘境机缘！斩杀 {0} 缴获随机词条阵宝【{1}】！", deadEnemy.Name, droppedArt.NameCn));
                    }
                }
            }

            cs.AppendBattleLog(string.Format("[斩获] 斩杀 {0}：石+{1:F0} 草+{2:F0} 铁+{3:F0}{4}{5}",
                deadEnemy.Name, cfg.DropSpiritStone, cfg.DropHerb, cfg.DropOre,
                cores > 0 ? string.Format(" 妖丹+{0}", cores) : "", extraLoot));
        }

        private static void HandleWaveCleared(SIGSaveData save, Random rng, SIGEventBus bus)
        {
            var cs = save.Combat;
            if (cs.CurrentWave < cs.TotalWaves)
            {
                cs.CurrentWave++;
                if (cs.Mode == CombatMode.SectRaidDefense) SpawnRaidEnemies(save, cs.RaidWaveLevel, cs.CurrentWave);
                else
                {
                    var stg = SIGConfigTable.GetStage(cs.StageId);
                    if (stg != null) SpawnStageWaveEnemies(save, stg, cs.CurrentWave);
                }
                cs.AppendBattleLog(string.Format("--- 推进至第 {0}/{1} 波 ---", cs.CurrentWave, cs.TotalWaves));
                return;
            }

            cs.IsInBattle = false;
            cs.TotalVictories++;

            if (cs.Mode == CombatMode.SectRaidDefense)
            {
                double repGain = 35.0 * cs.RaidWaveLevel;
                double stoneGain = 90.0 * cs.RaidWaveLevel;
                save.Sect.AddResource(ResourceType.SectReputation, repGain);
                save.Sect.AddResource(ResourceType.SpiritStone, stoneGain);
                cs.RaidWaveLevel++;
                cs.MonthsUntilNextRaid = SIGConfigTable.Current.Balance.RaidIntervalMonths;

                string winMsg = string.Format("★护山大捷！声望 +{0:F0}、灵石 +{1:F0}！", repGain, stoneGain);
                cs.AppendBattleLog(winMsg);
                if (bus != null) bus.PublishLog(save.GetImmortalDateText(), LogCategory.Combat, winMsg);
            }
            else
            {
                var stg = SIGConfigTable.GetStage(cs.StageId);
                if (stg != null)
                {
                    save.Sect.AddResource(ResourceType.SectReputation, stg.ClearReputationReward);
                    string unlockNote = "";
                    if (!string.IsNullOrEmpty(stg.NextUnlockStageId) && !cs.UnlockedStageIds.Contains(stg.NextUnlockStageId))
                    {
                        cs.UnlockedStageIds.Add(stg.NextUnlockStageId);
                        var nextStg = SIGConfigTable.GetStage(stg.NextUnlockStageId);
                        unlockNote = string.Format(" ★解锁新秘境【{0}】！", nextStg != null ? nextStg.NameCn : stg.NextUnlockStageId);
                    }
                    string winMsg = string.Format("★通关【{0}】！声望 +{1:F0}。{2}", stg.NameCn, stg.ClearReputationReward, unlockNote);
                    cs.AppendBattleLog(winMsg);
                    if (bus != null) bus.PublishLog(save.GetImmortalDateText(), LogCategory.Combat, winMsg);
                }

                if (cs.AutoRepeatExpedition)
                {
                    cs.RestCooldownSec = 2.5;
                    cs.AppendBattleLog("（自动连战：调息 2.5s 后再次进发……）");
                }
            }
        }

        private static void HandleBattleDefeat(SIGSaveData save, SIGEventBus bus)
        {
            var cs = save.Combat;
            cs.IsInBattle = false;
            cs.TotalDefeats++;
            cs.AutoRepeatExpedition = false;
            cs.RestCooldownSec = 0;

            save.Leader.BattleStats.CurHp = Math.Round(save.Leader.BattleStats.MaxHp * 0.35, 0);
            if (cs.Mode == CombatMode.SectRaidDefense)
            {
                cs.MonthsUntilNextRaid = SIGConfigTable.Current.Balance.RaidIntervalMonths;
                double lostStone = Math.Round(save.Sect.GetResource(ResourceType.SpiritStone) * 0.15, 0);
                save.Sect.AddResource(ResourceType.SpiritStone, -lostStone);
                string msg = string.Format("[失守] 山门防线失守，损耗灵石 {0:F0}。", lostStone);
                cs.AppendBattleLog(msg);
                if (bus != null) bus.PublishLog(save.GetImmortalDateText(), LogCategory.Combat, msg);
            }
            else
            {
                string msg = string.Format("败退回山：在【{0}】力战不支返回山门调息（自动连战已关闭）。", cs.StageName);
                cs.AppendBattleLog(msg);
                if (bus != null) bus.PublishLog(save.GetImmortalDateText(), LogCategory.Combat, msg);
            }
        }

        private static void SyncLeaderHpMpSnapshot(SIGSaveData save)
        {
            var leaderUnit = save.Combat.Allies.FirstOrDefault(u => u.UnitId == save.Leader.Id);
            if (leaderUnit != null)
            {
                save.Leader.BattleStats.CurHp = Math.Max(1.0, leaderUnit.CurHp);
                save.Leader.BattleStats.CurMp = Math.Max(0.0, leaderUnit.CurMp);
            }
        }

        public static bool UseQuickCombatPillSlot(SIGSaveData save, int slotIdx, SIGEventBus bus, out string message)
        {
            if (save == null) { message = "存档无效。"; return false; }
            save.Leader.EnsureQuickSlotsInitialized();
            if (slotIdx < 0 || slotIdx >= save.Leader.QuickCombatPills.Count) { message = "无效快捷槽。"; return false; }

            string pillId = save.Leader.QuickCombatPills[slotIdx];
            if (string.IsNullOrEmpty(pillId))
            {
                message = string.Format("战斗快捷丹药槽 {0} 尚未设置丹药，请点击【切换】配置。", slotIdx + 1);
                return false;
            }
            if (save.Sect.GetItemCount(pillId) <= 0)
            {
                var pEmpty = SIGConfigTable.GetItem(pillId);
                message = string.Format("【{0}】库存不足（0枚）。", pEmpty != null ? pEmpty.NameCn : pillId);
                return false;
            }

            var pill = SIGConfigTable.GetItem(pillId);
            if (pill == null) { message = "丹药配置不存在。"; return false; }

            if (!save.Combat.IsInBattle)
            {
                return CultivationLogic.UseCultivationPill(save, save.Leader, pillId, bus, out message);
            }

            var leaderUnit = save.Combat.Allies.FirstOrDefault(u => u.UnitId == save.Leader.Id && u.IsAlive);
            if (leaderUnit == null) { message = "掌门已退场，无法服丹。"; return false; }

            save.Sect.AddItem(pillId, -1);
            if (pill.HealHpRatio > 0)
            {
                double heal = Math.Round(leaderUnit.MaxHp * pill.HealHpRatio, 0);
                leaderUnit.CurHp = Math.Min(leaderUnit.MaxHp, leaderUnit.CurHp + heal);
            }
            if (pill.RestoreMpRatio > 0)
            {
                leaderUnit.CurMp = Math.Min(leaderUnit.MaxMp, leaderUnit.CurMp + leaderUnit.MaxMp * pill.RestoreMpRatio);
            }
            if (pill.QiGrant > 0)
            {
                save.Leader.CurQi = Math.Min(save.Leader.MaxQi, save.Leader.CurQi + pill.QiGrant);
            }
            SyncLeaderHpMpSnapshot(save);

            message = string.Format("掌门使用战丹槽{0}【{1}】（剩 {2} 枚）！", slotIdx + 1, pill.NameCn, save.Sect.GetItemCount(pillId));
            save.Combat.AppendBattleLog("[服丹] " + message);
            if (bus != null) bus.NotifyStateChanged();
            return true;
        }

        public static bool ManualLeaderAction(SIGSaveData save, string actionCode, Random rng, SIGEventBus bus, out string message)
        {
            if (save == null || !save.Combat.IsInBattle) { message = "当前未处于交战状态。"; return false; }

            var cs = save.Combat;
            var leaderUnit = cs.Allies.FirstOrDefault(u => u.UnitId == save.Leader.Id && u.IsAlive);
            if (leaderUnit == null) { message = "掌门已重伤退场。"; return false; }

            var target = cs.Enemies.FirstOrDefault(e => e.IsAlive);
            if (target == null) { message = "场上已无存活敌军。"; return false; }

            if (actionCode == "SKILL_QI_BURST")
            {
                double qiCost = Math.Max(40.0, Math.Round(save.Leader.MaxQi * 0.12, 0));
                if (save.Leader.CurQi < qiCost)
                {
                    message = string.Format("施展【太虚诛仙剑阵】需燃烧至少 {0:F0} 点当前气海修为！", qiCost);
                    return false;
                }
                save.Leader.CurQi -= qiCost;
                double burstDmg = Math.Round(leaderUnit.Attack * 2.90, 0);
                foreach (var enm in cs.Enemies.Where(e => e.IsAlive).ToList())
                {
                    ApplyDamageToUnit(enm, burstDmg);
                    if (!enm.IsAlive) GrantEnemyDrop(save, enm, rng, cs, bus);
                }
                message = string.Format("[剑阵] 掌门燃烧 {0:F0} 气海祭出【太虚诛仙剑阵】，全体轰击 {1:F0} 伤害！", qiCost, burstDmg);
                cs.AppendBattleLog(message);
                if (!cs.Enemies.Any(e => e.IsAlive)) HandleWaveCleared(save, rng, bus);
                if (bus != null) bus.NotifyStateChanged();
                return true;
            }

            var tal = SIGConfigTable.GetItem(leaderUnit.EquippedTalismanId);
            if (tal == null || tal.ActiveSkillMultiplier <= 0)
            {
                message = "掌门尚未穿戴主动本命法宝。";
                return false;
            }
            if (leaderUnit.CurMp < tal.ActiveSkillMpCost)
            {
                message = string.Format("法力不足（需 {0:F0} MP）。", tal.ActiveSkillMpCost);
                return false;
            }

            leaderUnit.CurMp -= tal.ActiveSkillMpCost;
            double dmg = Math.Max(10.0, Math.Round(leaderUnit.Attack * tal.ActiveSkillMultiplier - target.Defense * 0.4, 0));
            ApplyDamageToUnit(target, dmg);
            if (tal.ShieldMaxHpRatio > 0) leaderUnit.ShieldHp += Math.Round(leaderUnit.MaxHp * tal.ShieldMaxHpRatio, 0);
            SyncLeaderHpMpSnapshot(save);

            message = string.Format("[祭宝] 掌门催动【{0}·{1}】，重创 {2} -{3:F0}！", tal.NameCn, tal.ActiveSkillName, target.Name, dmg);
            cs.AppendBattleLog(message);

            if (!target.IsAlive) GrantEnemyDrop(save, target, rng, cs, bus);
            if (!cs.Enemies.Any(e => e.IsAlive)) HandleWaveCleared(save, rng, bus);
            if (bus != null) bus.NotifyStateChanged();
            return true;
        }
    }
}
