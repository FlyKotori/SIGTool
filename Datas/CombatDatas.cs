using System;
using System.Collections.Generic;

namespace SIGTool.Datas
{
    /// <summary>
    /// 战斗场上单个参战单位快照
    /// </summary>
    public class CombatUnitRuntime
    {
        public string UnitId { get; set; } = "";
        public string Name { get; set; } = "";
        public bool IsPlayerSide { get; set; } = true;
        public bool IsBoss { get; set; } = false;
        public ElementType Element { get; set; } = ElementType.None;

        public double MaxHp { get; set; } = 100.0;
        public double CurHp { get; set; } = 100.0;
        public double MaxMp { get; set; } = 50.0;
        public double CurMp { get; set; } = 50.0;

        public double Attack { get; set; } = 15.0;
        public double Defense { get; set; } = 5.0;
        public double Speed { get; set; } = 100.0;
        public double CritRate { get; set; } = 0.08;
        public double DodgeRate { get; set; } = 0.05;
        public double LifestealRate { get; set; } = 0.0;

        public double ActionGauge { get; set; } = 0.0; // 0 ~ 100 ATB行动条
        public double ShieldHp { get; set; } = 0.0;
        public int SkillCooldownTurns { get; set; } = 0;

        public string EquippedTalismanId { get; set; } = "";
        public string AutoPillItemId { get; set; } = "";
        public double AutoHealRatio { get; set; } = 0.45;
        public string SourceTemplateId { get; set; } = "";

        public bool IsAlive
        {
            get { return CurHp > 0.001; }
        }
    }

    /// <summary>
    /// 战斗与秘境挂机状态数据
    /// </summary>
    public class CombatStateData
    {
        public bool IsInBattle { get; set; } = false;
        public CombatMode Mode { get; set; } = CombatMode.IdleExpedition;
        public string StageId { get; set; } = "STG_MIST_VALLEY";
        public string StageName { get; set; } = "迷雾灵谷";

        public bool AutoRepeatExpedition { get; set; } = true;
        public int CurrentWave { get; set; } = 1;
        public int TotalWaves { get; set; } = 3;
        public int TurnCount { get; set; } = 0;
        public double BattleElapsedSec { get; set; } = 0.0;

        public double RestCooldownSec { get; set; } = 0.0;

        // 宗门护山兽潮倒计时（仙历月）
        public int MonthsUntilNextRaid { get; set; } = 12;
        public int RaidWaveLevel { get; set; } = 1;

        public int TotalVictories { get; set; } = 0;
        public int TotalDefeats { get; set; } = 0;

        public List<string> UnlockedStageIds { get; set; } = new List<string>();
        public List<CombatUnitRuntime> Allies { get; set; } = new List<CombatUnitRuntime>();
        public List<CombatUnitRuntime> Enemies { get; set; } = new List<CombatUnitRuntime>();
        public List<string> RecentBattleLogs { get; set; } = new List<string>();

        public void AppendBattleLog(string line)
        {
            if (RecentBattleLogs == null) RecentBattleLogs = new List<string>();
            RecentBattleLogs.Add(line);
            if (RecentBattleLogs.Count > 120)
            {
                RecentBattleLogs.RemoveRange(0, RecentBattleLogs.Count - 120);
            }
        }
    }
}
