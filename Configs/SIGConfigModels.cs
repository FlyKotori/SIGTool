using System;
using System.Collections.Generic;
using SIGTool.Datas;

namespace SIGTool.Configs
{
    /// <summary>
    /// 全局数值平衡与时间常数配置（命名统一采用 SIG 前缀）
    /// </summary>
    public class GlobalBalanceConfig
    {
        public double SecondsPerSIGMonth { get; set; } = 20.0;
        public double MaxOfflineHours { get; set; } = 24.0;
        public int BaseDiscipleCap { get; set; } = 3;
        public double WuXingGenerateBonus { get; set; } = 0.25;
        public double WuXingSameBonus { get; set; } = 0.12;
        public double WuXingOvercomePenalty { get; set; } = -0.15;
        public int RaidIntervalMonths { get; set; } = 12;
        public double DiscipleMonthlyFoodCost { get; set; } = 5.0;
        public double RecruitBaseStoneCost { get; set; } = 120.0;
        public double UnlockSlotBaseStoneCost { get; set; } = 140.0;
    }

    /// <summary>
    /// 修仙大境界静态配表项
    /// </summary>
    public class RealmConfig
    {
        public string RealmKey { get; set; } = "QiCondensation";
        public string NameCn { get; set; } = "炼气期";
        public int MaxSubStage { get; set; } = 4;
        public double BaseMaxQi { get; set; } = 600.0;
        public double SubStageQiGrowth { get; set; } = 1.65;
        public double BaseQiPerSec { get; set; } = 4.5;
        public double BaseHp { get; set; } = 180.0;
        public double BaseMp { get; set; } = 80.0;
        public double BaseAtk { get; set; } = 24.0;
        public double BaseDef { get; set; } = 10.0;
        public double BaseLifespanYears { get; set; } = 120.0;
        public double BreakthroughBaseRate { get; set; } = 0.75;
        public string RecommendedPillId { get; set; } = "";

        public RealmTier GetRealmTier()
        {
            RealmTier parsed;
            return Enum.TryParse(RealmKey, true, out parsed) ? parsed : RealmTier.QiCondensation;
        }
    }

    /// <summary>
    /// 宗门建筑静态配表项
    /// </summary>
    public class BuildingConfig
    {
        public string Id { get; set; } = "";
        public string NameCn { get; set; } = "";
        public string ShortName { get; set; } = "";
        public string CategoryKey { get; set; } = "Gather";
        public string ElementKey { get; set; } = "Earth";
        public int MaxLevel { get; set; } = 5;
        public int MaxCount { get; set; } = 0;
        public int MinSectLevel { get; set; } = 1;

        public double CostSpiritStone { get; set; } = 50.0;
        public double CostWood { get; set; } = 30.0;
        public double CostOre { get; set; } = 20.0;
        public double BuildTimeSec { get; set; } = 8.0;

        public string ProduceResourceKey { get; set; } = "";
        public double BaseOutputPerSec { get; set; } = 0.0;
        public string SecondaryResourceKey { get; set; } = "";
        public double SecondaryOutputPerSec { get; set; } = 0.0;

        public double AuraBonusRate { get; set; } = 0.0;
        public int DiscipleCapBonus { get; set; } = 0;
        public double DefenseBarrierBonus { get; set; } = 0.0;
        public string Desc { get; set; } = "";

        public string GetShortLabel()
        {
            return !string.IsNullOrEmpty(ShortName) ? ShortName : NameCn;
        }

        public BuildingCategory GetCategory()
        {
            BuildingCategory parsed;
            return Enum.TryParse(CategoryKey, true, out parsed) ? parsed : BuildingCategory.Gather;
        }

        public ElementType GetElement()
        {
            ElementType parsed;
            return Enum.TryParse(ElementKey, true, out parsed) ? parsed : ElementType.None;
        }
    }

    /// <summary>
    /// 丹药、法器、装备与材料静态配表项
    /// </summary>
    public class ItemConfig
    {
        public string Id { get; set; } = "";
        public string NameCn { get; set; } = "";
        public string ShortName { get; set; } = "";
        public string CategoryKey { get; set; } = "Material";
        public string ElementKey { get; set; } = "None";
        public int MaxStack { get; set; } = 999;
        public int PriceSpiritStone { get; set; } = 20;

        public double QiGrant { get; set; } = 0.0;
        public double BreakthroughRateBonus { get; set; } = 0.0;
        public double HealHpRatio { get; set; } = 0.0;
        public double RestoreMpRatio { get; set; } = 0.0;

        public double AtkBonus { get; set; } = 0.0;
        public double DefBonus { get; set; } = 0.0;
        public double HpBonus { get; set; } = 0.0;
        public double SpeedBonus { get; set; } = 0.0;

        public string ActiveSkillName { get; set; } = "";
        public double ActiveSkillMultiplier { get; set; } = 0.0;
        public double ActiveSkillMpCost { get; set; } = 0.0;
        public double ShieldMaxHpRatio { get; set; } = 0.0;
        public int CooldownTurns { get; set; } = 2;
        public string Desc { get; set; } = "";

        public string GetShortLabel()
        {
            return !string.IsNullOrEmpty(ShortName) ? ShortName : NameCn;
        }

        public ItemCategory GetCategory()
        {
            ItemCategory parsed;
            return Enum.TryParse(CategoryKey, true, out parsed) ? parsed : ItemCategory.Material;
        }

        public ElementType GetElement()
        {
            ElementType parsed;
            return Enum.TryParse(ElementKey, true, out parsed) ? parsed : ElementType.None;
        }
    }

    /// <summary>
    /// 丹房/器坊百艺炼制配方静态配表项
    /// </summary>
    public class RecipeConfig
    {
        public string Id { get; set; } = "";
        public string NameCn { get; set; } = "";
        public string ShortName { get; set; } = "";
        public int MaxMasteryLv { get; set; } = 5;
        public string RequiredBuildingId { get; set; } = "BLD_ALCHEMY";
        public int MinBuildingLevel { get; set; } = 1;
        public string OutputItemId { get; set; } = "";
        public int OutputCount { get; set; } = 1;
        public double CraftTimeSec { get; set; } = 12.0;

        public double CostSpiritStone { get; set; } = 15.0;
        public double CostHerb { get; set; } = 10.0;
        public double CostOre { get; set; } = 0.0;
        public double CostWood { get; set; } = 0.0;
        public double CostMonsterCore { get; set; } = 0.0;

        public double BaseSuccessRate { get; set; } = 0.85;
        public string Desc { get; set; } = "";

        public string GetShortLabel()
        {
            return !string.IsNullOrEmpty(ShortName) ? ShortName : NameCn;
        }
    }

    /// <summary>
    /// 修仙功法静态配表项
    /// </summary>
    public class TechniqueConfig
    {
        public string Id { get; set; } = "";
        public string NameCn { get; set; } = "";
        public string ShortName { get; set; } = "";
        public string ElementKey { get; set; } = "None";
        public int MaxLevel { get; set; } = 10;
        public double BaseUpgradeStoneCost { get; set; } = 60.0;

        public double QiRateMultiplier { get; set; } = 1.0;
        public double AtkMultiplier { get; set; } = 1.0;
        public double HpMultiplier { get; set; } = 1.0;
        public double DefMultiplier { get; set; } = 1.0;
        public string Desc { get; set; } = "";

        public string GetShortLabel()
        {
            return !string.IsNullOrEmpty(ShortName) ? ShortName : NameCn;
        }

        public ElementType GetElement()
        {
            ElementType parsed;
            return Enum.TryParse(ElementKey, true, out parsed) ? parsed : ElementType.None;
        }
    }

    /// <summary>
    /// 妖兽与敌方修士模板静态配表项
    /// </summary>
    public class EnemyTemplateConfig
    {
        public string Id { get; set; } = "";
        public string NameCn { get; set; } = "";
        public string ElementKey { get; set; } = "None";
        public bool IsBoss { get; set; } = false;
        public double MaxHp { get; set; } = 150.0;
        public double Attack { get; set; } = 22.0;
        public double Defense { get; set; } = 6.0;
        public double Speed { get; set; } = 95.0;

        public double DropSpiritStone { get; set; } = 18.0;
        public double DropHerb { get; set; } = 4.0;
        public double DropOre { get; set; } = 3.0;
        public double DropMonsterCore { get; set; } = 1.0;
        public string DropSpecialItemId { get; set; } = "";
        public double DropSpecialChance { get; set; } = 0.0;

        public ElementType GetElement()
        {
            ElementType parsed;
            return Enum.TryParse(ElementKey, true, out parsed) ? parsed : ElementType.None;
        }
    }

    /// <summary>
    /// 秘境历练关卡静态配表项
    /// </summary>
    public class CombatStageConfig
    {
        public string Id { get; set; } = "";
        public string NameCn { get; set; } = "";
        public string ShortName { get; set; } = "";
        public int StageTier { get; set; } = 1;
        public string RecommendedRealmKey { get; set; } = "QiCondensation";
        public int WaveCount { get; set; } = 3;
        public List<string> NormalEnemyIds { get; set; } = new List<string>();
        public string BossEnemyId { get; set; } = "";
        public string NextUnlockStageId { get; set; } = "";
        public double ClearReputationReward { get; set; } = 15.0;
        public string Desc { get; set; } = "";

        public string GetShortLabel()
        {
            return !string.IsNullOrEmpty(ShortName) ? ShortName : NameCn;
        }

        public RealmTier GetRecommendedRealm()
        {
            RealmTier parsed;
            return Enum.TryParse(RecommendedRealmKey, true, out parsed) ? parsed : RealmTier.QiCondensation;
        }
    }

    /// <summary>
    /// 随机词条阵宝胚体静态配表项（SYS-01：用于秘境掉落与注灵祭炼生成随机词条法宝）
    /// </summary>
    public class ArtifactTemplateConfig
    {
        public string Id { get; set; } = "";
        public string NameCn { get; set; } = "";
        public string ShortName { get; set; } = "";
        public string ElementKey { get; set; } = "Metal";
        public int Tier { get; set; } = 1;
        public double BaseAtk { get; set; } = 0.0;
        public double BaseDef { get; set; } = 0.0;
        public double BaseHp { get; set; } = 0.0;
        public string Desc { get; set; } = "";

        public string GetShortLabel()
        {
            return !string.IsNullOrEmpty(ShortName) ? ShortName : NameCn;
        }

        public ElementType GetElement()
        {
            ElementType parsed;
            return Enum.TryParse(ElementKey, true, out parsed) ? parsed : ElementType.Metal;
        }
    }

    /// <summary>
    /// SYS-02 经脉穴位节点静态配表项
    /// </summary>
    public class AcupointNodeConfig
    {
        public string Id { get; set; } = "";
        public string NameCn { get; set; } = "";
        public string ShortName { get; set; } = "";
        public string BranchKey { get; set; } = "DuMai";
        public int OrderIndex { get; set; } = 1;
        public string PrerequisiteId { get; set; } = "";
        public string MinRealmKey { get; set; } = "Mortal";

        public double CostQi { get; set; } = 100.0;
        public double CostSpiritStone { get; set; } = 30.0;
        public double CostHerb { get; set; } = 10.0;
        public double CostMonsterCore { get; set; } = 0.0;
        public double BaseSuccessRate { get; set; } = 0.85;

        public double FlatHp { get; set; } = 0.0;
        public double FlatMp { get; set; } = 0.0;
        public double FlatAtk { get; set; } = 0.0;
        public double FlatDef { get; set; } = 0.0;
        public double FlatSpeed { get; set; } = 0.0;
        public double LifespanAdd { get; set; } = 0.0;

        public double TechMultAdd { get; set; } = 0.0;
        public double QiRatePctAdd { get; set; } = 0.0;
        public double CritRateAdd { get; set; } = 0.0;
        public double DodgeRateAdd { get; set; } = 0.0;
        public double BreakthroughRateAdd { get; set; } = 0.0;

        public string Desc { get; set; } = "";

        public string GetShortLabel()
        {
            return !string.IsNullOrEmpty(ShortName) ? ShortName : NameCn;
        }

        public MeridianBranchType GetBranch()
        {
            MeridianBranchType parsed;
            return Enum.TryParse(BranchKey, true, out parsed) ? parsed : MeridianBranchType.DuMai;
        }

        public RealmTier GetMinRealm()
        {
            RealmTier parsed;
            return Enum.TryParse(MinRealmKey, true, out parsed) ? parsed : RealmTier.Mortal;
        }
    }

    /// <summary>
    /// SIG 静态配置总根对象
    /// </summary>
    public class SIGConfigRoot
    {
        public GlobalBalanceConfig Balance { get; set; } = new GlobalBalanceConfig();
        public List<RealmConfig> Realms { get; set; } = new List<RealmConfig>();
        public List<BuildingConfig> Buildings { get; set; } = new List<BuildingConfig>();
        public List<ItemConfig> Items { get; set; } = new List<ItemConfig>();
        public List<RecipeConfig> Recipes { get; set; } = new List<RecipeConfig>();
        public List<TechniqueConfig> Techniques { get; set; } = new List<TechniqueConfig>();
        public List<EnemyTemplateConfig> Enemies { get; set; } = new List<EnemyTemplateConfig>();
        public List<CombatStageConfig> Stages { get; set; } = new List<CombatStageConfig>();
        public List<ArtifactTemplateConfig> ArtifactTemplates { get; set; } = new List<ArtifactTemplateConfig>();
        public List<AcupointNodeConfig> Acupoints { get; set; } = new List<AcupointNodeConfig>();
    }
}
