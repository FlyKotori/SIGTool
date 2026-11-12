using System;
using System.Collections.Generic;
using SIGTool.Configs;
using SIGTool.Datas;
using SIGTool.Modules.Logics;

namespace SIGTool.Modules.Core
{
    /// <summary>
    /// SIG 主控引擎（Facade 门面 + 实时挂机 Tick 调度器 + 离线挂机补偿计算）
    /// </summary>
    public class SIGEngine
    {
        public SIGSaveData Save { get; private set; }
        public SIGEventBus EventBus { get; private set; }
        public Random Rng { get; private set; }

        public int CurrentSaveSlot { get; set; } = 1;
        public int SpeedMultiplier { get; private set; } = 1; // 0=暂停, 1=1x, 2=2x, 5=5x

        private double _autoSaveAccumSec = 0.0;

        public SIGEngine()
        {
            EventBus = new SIGEventBus();
            Rng = new Random();
        }

        public void Initialize()
        {
            string cfgMsg;
            ConfigLoader.LoadOrInitConfig(ConfigLoader.DefaultConfigFilePath, out cfgMsg);

            string loadMsg;
            var loaded = SaveLoadService.LoadFromDisk(CurrentSaveSlot, out loadMsg);
            bool isNewSIG = (loaded == null);
            Save = loaded ?? SaveLoadService.CreateNewSIGSave();

            SectBuildLogic.RefreshSectDerivedCaps(Save);
            CultivationLogic.RecalculateAllCharacters(Save);

            if (isNewSIG)
            {
                Save.Leader.BattleStats.CurHp = Save.Leader.BattleStats.MaxHp;
                Save.Leader.BattleStats.CurMp = Save.Leader.BattleStats.MaxMp;
                foreach (var d in Save.Disciples)
                {
                    d.BattleStats.CurHp = d.BattleStats.MaxHp;
                    d.BattleStats.CurMp = d.BattleStats.MaxMp;
                }
            }

            EventBus.PublishLog(Save.GetImmortalDateText(), LogCategory.System, cfgMsg);
            EventBus.PublishLog(Save.GetImmortalDateText(), LogCategory.System, loadMsg);

            if (!isNewSIG && Save.LastSaveUtcTicks > 0)
            {
                double offlineSec = (DateTime.UtcNow - new DateTime(Save.LastSaveUtcTicks, DateTimeKind.Utc)).TotalSeconds;
                if (offlineSec >= 15.0)
                {
                    var summary = SimulateOfflineGains(offlineSec);
                    EventBus.PublishLog(Save.GetImmortalDateText(), LogCategory.System,
                        string.Format("【离线挂机结算】离线 {0:F0} 秒，掌门气海 +{1:F0}，秘境连胜 +{2} 次！",
                            summary.EffectiveSimulatedSeconds, summary.LeaderQiGained, summary.ExpeditionWins));
                    EventBus.NotifyOfflineReport(summary);
                }
            }

            EventBus.NotifyStateChanged();
        }

        public void SetSpeed(int speed)
        {
            if (speed != 0 && speed != 1 && speed != 2 && speed != 5) speed = 1;
            SpeedMultiplier = speed;
            EventBus.PublishLog(Save.GetImmortalDateText(), LogCategory.System,
                speed == 0 ? "已暂停时间流逝 (0x)。" : string.Format("时间流速已调整为 {0}x 倍速。", speed));
            EventBus.NotifyStateChanged();
        }

        public bool ReloadConfig(out string msg)
        {
            bool ok = ConfigLoader.LoadOrInitConfig(ConfigLoader.DefaultConfigFilePath, out msg);
            SectBuildLogic.RefreshSectDerivedCaps(Save);
            CultivationLogic.RecalculateAllCharacters(Save);
            EventBus.PublishLog(Save.GetImmortalDateText(), LogCategory.System, msg);
            EventBus.NotifyStateChanged();
            return ok;
        }

        public bool SaveSIG(out string msg)
        {
            bool ok = SaveLoadService.SaveToDisk(Save, CurrentSaveSlot, out msg);
            EventBus.PublishLog(Save.GetImmortalDateText(), LogCategory.System, msg);
            return ok;
        }

        public bool LoadSIGSlot(int slot, out string msg)
        {
            CurrentSaveSlot = slot;
            var loaded = SaveLoadService.LoadFromDisk(slot, out msg);
            if (loaded == null)
            {
                Save = SaveLoadService.CreateNewSIGSave();
                SectBuildLogic.RefreshSectDerivedCaps(Save);
                CultivationLogic.RecalculateAllCharacters(Save);
                Save.Leader.BattleStats.CurHp = Save.Leader.BattleStats.MaxHp;
                Save.Leader.BattleStats.CurMp = Save.Leader.BattleStats.MaxMp;
            }
            else
            {
                Save = loaded;
                SectBuildLogic.RefreshSectDerivedCaps(Save);
                CultivationLogic.RecalculateAllCharacters(Save);
            }

            EventBus.PublishLog(Save.GetImmortalDateText(), LogCategory.System, msg);
            EventBus.NotifyStateChanged();
            return true;
        }

        public void ResetNewSIG()
        {
            Save = SaveLoadService.CreateNewSIGSave();
            SectBuildLogic.RefreshSectDerivedCaps(Save);
            CultivationLogic.RecalculateAllCharacters(Save);
            Save.Leader.BattleStats.CurHp = Save.Leader.BattleStats.MaxHp;
            Save.Leader.BattleStats.CurMp = Save.Leader.BattleStats.MaxMp;
            EventBus.ClearLogs();
            EventBus.PublishLog(Save.GetImmortalDateText(), LogCategory.System, "已重开一世仙途，重建【太虚云隐宗】！");
            EventBus.NotifyStateChanged();
        }

        public void Tick(double realDeltaSec)
        {
            if (Save == null || realDeltaSec <= 0) return;
            if (realDeltaSec > 0.5) realDeltaSec = 0.5;

            _autoSaveAccumSec += realDeltaSec;
            if (_autoSaveAccumSec >= 60.0)
            {
                _autoSaveAccumSec = 0.0;
                string silentMsg;
                SaveLoadService.SaveToDisk(Save, CurrentSaveSlot, out silentMsg);
            }

            if (SpeedMultiplier <= 0) return;

            double simDt = realDeltaSec * SpeedMultiplier;
            AdvanceSimulationStep(simDt, allowRaidTrigger: true);
        }

        private void AdvanceSimulationStep(double simDt, bool allowRaidTrigger)
        {
            Save.TotalPlaySeconds += simDt;

            double secPerMonth = Math.Max(5.0, SIGConfigTable.Current.Balance.SecondsPerSIGMonth);
            Save.MonthProgressSec += simDt;
            while (Save.MonthProgressSec >= secPerMonth)
            {
                Save.MonthProgressSec -= secPerMonth;
                AdvanceOneImmortalMonth(allowRaidTrigger);
            }

            SectBuildLogic.TickSectEconomy(Save, simDt, EventBus);
            CraftAndItemLogic.TickWorkshops(Save, simDt, Rng, EventBus);
            CultivationLogic.TickCultivation(Save, simDt, EventBus);
            CombatLogic.TickCombat(Save, simDt, Rng, EventBus);
        }

        private void AdvanceOneImmortalMonth(bool allowRaidTrigger)
        {
            Save.SIGMonth++;
            if (Save.SIGMonth > 12)
            {
                Save.SIGMonth = 1;
                Save.SIGYear++;
                EventBus.PublishLog(Save.GetImmortalDateText(), LogCategory.System,
                    string.Format("岁序更替，步入仙历第 {0} 年！", Save.SIGYear));
            }

            SectBuildLogic.SettleMonthlySectUpkeep(Save, EventBus);

            if (allowRaidTrigger && Save.Combat.Mode != CombatMode.SectRaidDefense)
            {
                Save.Combat.MonthsUntilNextRaid--;
                if (Save.Combat.MonthsUntilNextRaid <= 0)
                {
                    Save.Combat.MonthsUntilNextRaid = SIGConfigTable.Current.Balance.RaidIntervalMonths;
                    CombatLogic.StartSectRaid(Save, EventBus);
                }
            }
        }

        public OfflineGainSummary SimulateOfflineGains(double rawOfflineSeconds)
        {
            double maxSec = SIGConfigTable.Current.Balance.MaxOfflineHours * 3600.0;
            double effSec = Math.Min(Math.Max(0.0, rawOfflineSeconds), maxSec);

            var summary = new OfflineGainSummary
            {
                RawOfflineSeconds = rawOfflineSeconds,
                EffectiveSimulatedSeconds = effSec
            };

            var beforeRes = new Dictionary<ResourceType, double>();
            foreach (ResourceType rt in Enum.GetValues(typeof(ResourceType)))
            {
                beforeRes[rt] = Save.Sect.GetResource(rt);
            }
            double beforeQi = Save.Leader.CurQi;
            int beforeWins = Save.Combat.TotalVictories;
            int beforeMonths = Save.SIGYear * 12 + Save.SIGMonth;

            EventBus.SuppressNotifications = true;
            try
            {
                double step = effSec > 3600.0 ? effSec / 3600.0 : 1.0;
                double remaining = effSec;
                while (remaining > 0.001)
                {
                    double dt = Math.Min(step, remaining);
                    AdvanceSimulationStep(dt, allowRaidTrigger: false);
                    remaining -= dt;
                }
            }
            finally
            {
                EventBus.SuppressNotifications = false;
            }

            summary.LeaderQiGained = Math.Max(0.0, Save.Leader.CurQi - beforeQi);
            summary.ExpeditionWins = Math.Max(0, Save.Combat.TotalVictories - beforeWins);
            summary.MonthsAdvanced = Math.Max(0, (Save.SIGYear * 12 + Save.SIGMonth) - beforeMonths);

            foreach (ResourceType rt in Enum.GetValues(typeof(ResourceType)))
            {
                double diff = Save.Sect.GetResource(rt) - beforeRes[rt];
                if (diff > 0.01) summary.ResourceGains[rt] = diff;
            }

            return summary;
        }
    }
}
