using System;
using System.Collections.Generic;

namespace SIGTool.Datas
{
    /// <summary>
    /// SYS-03 修仙界全局岁运天象枚举（周期性轮转，联动宗门产出、坊市物价与奇遇概率）
    /// </summary>
    public enum WorldTrendType
    {
        BalancedNormal = 0,       // 风调雨顺：天地平和，产销均衡
        HerbHarvest = 1,          // 百草丰年：灵草/灵谷产出+22%，坊市草木丹材跌价(0.78x)
        PillMaterialShortage = 2, // 丹材荒岁：灵草产出-12%，坊市灵草与灵丹暴涨(1.45x)
        EarthVeinTide = 3,        // 地脉潮汐：玄铁/灵木产出+22%，全宗灵气吐纳+15%
        MonsterSurge = 4,         // 群妖躁动：秘境妖丹掉落+30%，坊市妖丹与法兵走高(1.35x)
        RelicEmergence = 5        // 古迹现世：上古洞府松动，奇遇触发率翻倍，破境丹与法宝涨价(1.30x)
    }

    /// <summary>
    /// SYS-03 周边修仙势力邦交关系档位
    /// </summary>
    public enum FactionRelationTier
    {
        Hostile = -2,  // 仇视 (<= -40)
        Cold = -1,     // 冷淡 (-39 ~ -10)
        Neutral = 0,   // 中立 (-9 ~ 34)
        Friendly = 1,  // 交好 (35 ~ 74)
        Allied = 2     // 盟友 (>= 75)
    }

    /// <summary>
    /// 周边宗门/世家动态演化运行时数据
    /// </summary>
    public class WorldFactionRuntimeData
    {
        public string FactionId { get; set; } = "";
        public int Prosperity { get; set; } = 55; // 15 ~ 100
        public int Relation { get; set; } = 0;    // -100 ~ 100
        public int DiplomacyCooldownMonths { get; set; } = 0;
        public string RecentTrendNote { get; set; } = "山门稳固，按常理吐纳经营";
    }

    /// <summary>
    /// 九州坊市单品实时行情与当季限购余量
    /// </summary>
    public class MarketSlotRuntimeData
    {
        public string ListingId { get; set; } = "";
        public double PriceMultiplier { get; set; } = 1.0; // 0.65 ~ 1.65
        public int RemainingBuyStock { get; set; } = 10;
    }

    /// <summary>
    /// 待玩家抉择的非阻塞奇遇事件条目（最多入队暂存 3 条，绝不弹窗打断挂机）
    /// </summary>
    public class PendingEncounterData
    {
        public string InstanceUid { get; set; } = "";
        public string EventId { get; set; } = "";
        public string TriggerDateText { get; set; } = "";
        public int ChainStep { get; set; } = 1;
        public int ChainTotal { get; set; } = 1;
    }

    /// <summary>
    /// 多阶段奇遇事件链的后续延迟触发排程
    /// </summary>
    public class ScheduledChainFollowupData
    {
        public string NextEventId { get; set; } = "";
        public int RemainingMonths { get; set; } = 3;
    }

    /// <summary>
    /// 奇遇抉择赋予的阶段性宗门气运增益
    /// </summary>
    public class WorldTempBuffData
    {
        public string BuffName { get; set; } = "";
        public int RemainingMonths { get; set; } = 6;
        public double AuraRateAdd { get; set; } = 0.0;
        public double AtkPctAdd { get; set; } = 0.0;
        public double HerbOutputMultAdd { get; set; } = 0.0;
    }

    /// <summary>
    /// SYS-03 修仙界动态演化、浮动坊市与奇遇事件链总状态（挂载于 SIGSaveData.World）
    /// </summary>
    public class WorldEvolutionStateData
    {
        public WorldTrendType CurrentTrend { get; set; } = WorldTrendType.BalancedNormal;
        public int MonthsUntilTrendShift { get; set; } = 12;
        public int MonthsUntilMarketRefresh { get; set; } = 3;

        public int NextEncounterSeq { get; set; } = 1;
        public int TotalResolvedEncounters { get; set; } = 0;

        public List<WorldFactionRuntimeData> Factions { get; set; } = new List<WorldFactionRuntimeData>();
        public List<MarketSlotRuntimeData> MarketSlots { get; set; } = new List<MarketSlotRuntimeData>();
        public List<PendingEncounterData> PendingEncounters { get; set; } = new List<PendingEncounterData>();
        public List<ScheduledChainFollowupData> ScheduledFollowups { get; set; } = new List<ScheduledChainFollowupData>();
        public List<WorldTempBuffData> TempBuffs { get; set; } = new List<WorldTempBuffData>();
        public List<string> TriggeredRealmMilestones { get; set; } = new List<string>();
        public List<string> WorldChronicles { get; set; } = new List<string>();
    }
}
