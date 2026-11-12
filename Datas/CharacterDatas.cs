using System;
using System.Collections.Generic;

namespace SIGTool.Datas
{
    /// <summary>
    /// 角色先天根骨属性数据（创建角色或招募弟子时确定，平时收纳于全属性详情窗）
    /// </summary>
    public class InnateStatsData
    {
        public int Constitution { get; set; } = 8;
        public int Strength { get; set; } = 8;
        public int Comprehension { get; set; } = 8;
        public int Agility { get; set; } = 8;
        public int Fortune { get; set; } = 8;
        public ElementType RootElement { get; set; } = ElementType.Wood;
        public int RootQuality { get; set; } = 3; // 1~5: 杂灵根~天灵根
    }

    /// <summary>
    /// 角色后天派生战斗与生存数值面板
    /// </summary>
    public class BattleStatsData
    {
        public double MaxHp { get; set; } = 120.0;
        public double CurHp { get; set; } = 120.0;
        public double MaxMp { get; set; } = 60.0;
        public double CurMp { get; set; } = 60.0;
        public double Attack { get; set; } = 18.0;
        public double Defense { get; set; } = 8.0;
        public double Speed { get; set; } = 100.0;
        public double CritRate { get; set; } = 0.08;
        public double DodgeRate { get; set; } = 0.05;
        public double LifestealRate { get; set; } = 0.0;
    }

    /// <summary>
    /// 修士个体运行时数据（涵盖掌门与门人弟子）
    /// </summary>
    public class CharacterData
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public bool IsSectLeader { get; set; } = false;

        public RealmTier Realm { get; set; } = RealmTier.QiCondensation;
        public int SubStage { get; set; } = 1;

        public double AgeYears { get; set; } = 18.0;
        public double MaxLifespanYears { get; set; } = 100.0;

        public double CurQi { get; set; } = 0.0;
        public double MaxQi { get; set; } = 1000.0;

        public InnateStatsData Innate { get; set; } = new InnateStatsData();
        public BattleStatsData BattleStats { get; set; } = new BattleStatsData();

        public DiscipleJob AssignedJob { get; set; } = DiscipleJob.Cultivate;

        public string EquippedTechniqueId { get; set; } = "TECH_BASIC";
        public Dictionary<string, int> TechniqueLevels { get; set; } = new Dictionary<string, int>();

        public string EquippedWeaponId { get; set; } = "";
        public string EquippedArmorId { get; set; } = "";
        public string EquippedTalismanId { get; set; } = "";

        public List<string> QuickCultivationPills { get; set; } = new List<string> { "ITEM_JUQI_PILL", "ITEM_JIUZHUAN_PILL", "ITEM_HEAL_PILL_1" };
        public List<string> QuickCombatPills { get; set; } = new List<string> { "ITEM_HEAL_PILL_1", "ITEM_HEAL_PILL_2", "" };
        public double AutoHealHpRatio { get; set; } = 0.45;

        public int GatherSkillLv { get; set; } = 1;
        public int AlchemySkillLv { get; set; } = 1;
        public int ForgeSkillLv { get; set; } = 1;

        // SYS-02 经脉穴位树：已贯通穴位 ID 列表与冲穴失败保底祝福计数
        public List<string> UnlockedAcupointIds { get; set; } = new List<string>();
        public Dictionary<string, int> AcupointFailPity { get; set; } = new Dictionary<string, int>();

        public int GetTechniqueLevel(string techId)
        {
            if (string.IsNullOrEmpty(techId)) return 1;
            if (TechniqueLevels == null) TechniqueLevels = new Dictionary<string, int>();
            int lv;
            return TechniqueLevels.TryGetValue(techId, out lv) ? Math.Max(1, lv) : 1;
        }

        public void EnsureQuickSlotsInitialized()
        {
            if (QuickCultivationPills == null) QuickCultivationPills = new List<string>();
            while (QuickCultivationPills.Count < 3) QuickCultivationPills.Add("");
            if (QuickCombatPills == null) QuickCombatPills = new List<string>();
            while (QuickCombatPills.Count < 3) QuickCombatPills.Add("");
            if (TechniqueLevels == null) TechniqueLevels = new Dictionary<string, int>();
            if (UnlockedAcupointIds == null) UnlockedAcupointIds = new List<string>();
            if (AcupointFailPity == null) AcupointFailPity = new Dictionary<string, int>();
        }

        public double EstimateCombatPower()
        {
            if (BattleStats == null) return 0.0;
            return Math.Round(
                BattleStats.MaxHp * 0.22 +
                BattleStats.MaxMp * 0.15 +
                BattleStats.Attack * 3.2 +
                BattleStats.Defense * 2.5 +
                BattleStats.Speed * 0.6 +
                BattleStats.CritRate * 200.0 +
                BattleStats.LifestealRate * 260.0, 0);
        }
    }
}
