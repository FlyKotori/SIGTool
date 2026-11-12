using System;
using System.Collections.Generic;

namespace SIGTool.Datas
{
    /// <summary>
    /// 五行灵脉、功法与阵眼属性枚举
    /// </summary>
    public enum ElementType
    {
        None = 0,
        Metal = 1,
        Wood = 2,
        Water = 3,
        Fire = 4,
        Earth = 5
    }

    /// <summary>
    /// 修仙大境界枚举
    /// </summary>
    public enum RealmTier
    {
        Mortal = 0,
        QiCondensation = 1,
        Foundation = 2,
        GoldenCore = 3,
        NascentSoul = 4,
        SpiritSevering = 5,
        Tribulation = 6,
        Immortal = 7
    }

    /// <summary>
    /// 宗门核心基础资源枚举
    /// </summary>
    public enum ResourceType
    {
        SpiritStone = 0,
        SpiritWood = 1,
        SpiritOre = 2,
        SpiritFood = 3,
        SpiritHerb = 4,
        MonsterCore = 5,
        SectReputation = 6
    }

    /// <summary>
    /// 宗门建筑职能分类
    /// </summary>
    public enum BuildingCategory
    {
        Core = 0,
        Gather = 1,
        Craft = 2,
        Cultivation = 3,
        Living = 4,
        Defense = 5
    }

    /// <summary>
    /// 门人弟子日常挂机职责
    /// </summary>
    public enum DiscipleJob
    {
        Cultivate = 0,
        Farm = 1,
        Lumber = 2,
        Mine = 3,
        Alchemy = 4,
        Forge = 5,
        Guard = 6
    }

    /// <summary>
    /// 物品与法器丹药分类
    /// </summary>
    public enum ItemCategory
    {
        Material = 0,
        CultivationPill = 1,
        BreakthroughPill = 2,
        CombatPill = 3,
        Weapon = 4,
        Armor = 5,
        Talisman = 6
    }

    /// <summary>
    /// 战斗模式类型
    /// </summary>
    public enum CombatMode
    {
        IdleExpedition = 0,
        BossChallenge = 1,
        SectRaidDefense = 2
    }

    /// <summary>
    /// 随机词条法宝品阶枚举（SYS-01：决定副词条条数 1~4 条与基础乘区）
    /// </summary>
    public enum ArtifactRarity
    {
        Common = 1,   // 凡品（1词条）
        Spirit = 2,   // 灵品（2词条）
        Treasure = 3, // 宝品（3词条）
        Immortal = 4  // 仙品（4词条）
    }

    /// <summary>
    /// 随机法宝副词条属性枚举（SYS-01）
    /// </summary>
    public enum ArtifactAffixType
    {
        AtkFlat = 0,
        AtkPct = 1,
        DefFlat = 2,
        DefPct = 3,
        HpFlat = 4,
        HpPct = 5,
        SpeedFlat = 6,
        CritRatePct = 7,
        DodgeRatePct = 8,
        LifestealPct = 9,
        QiRatePct = 10,
        BarrierHpFlat = 11
    }

    /// <summary>
    /// 枚举中文显示与五行生克关系工具类
    /// </summary>
    public static class EnumTextMap
    {
        public static string ElemCn(ElementType elem)
        {
            switch (elem)
            {
                case ElementType.Metal: return "庚金";
                case ElementType.Wood: return "青木";
                case ElementType.Water: return "玄水";
                case ElementType.Fire: return "离火";
                case ElementType.Earth: return "戊土";
                default: return "混元";
            }
        }

        public static string ElemShortCn(ElementType elem)
        {
            switch (elem)
            {
                case ElementType.Metal: return "金";
                case ElementType.Wood: return "木";
                case ElementType.Water: return "水";
                case ElementType.Fire: return "火";
                case ElementType.Earth: return "土";
                default: return "无";
            }
        }

        public static string RarityCn(ArtifactRarity rarity)
        {
            switch (rarity)
            {
                case ArtifactRarity.Common: return "凡品";
                case ArtifactRarity.Spirit: return "灵品";
                case ArtifactRarity.Treasure: return "宝品";
                case ArtifactRarity.Immortal: return "仙品";
                default: return "凡品";
            }
        }

        public static string RarityShortCn(ArtifactRarity rarity)
        {
            switch (rarity)
            {
                case ArtifactRarity.Common: return "凡";
                case ArtifactRarity.Spirit: return "灵";
                case ArtifactRarity.Treasure: return "宝";
                case ArtifactRarity.Immortal: return "仙";
                default: return "凡";
            }
        }

        public static string RootQualityCn(int quality)
        {
            switch (quality)
            {
                case 1: return "五灵杂根";
                case 2: return "下品灵根";
                case 3: return "中品真灵根";
                case 4: return "上品地灵根";
                case 5: return "极品天灵根";
                default: return "凡骨";
            }
        }

        public static string RealmBaseCn(RealmTier tier)
        {
            switch (tier)
            {
                case RealmTier.Mortal: return "凡胎境";
                case RealmTier.QiCondensation: return "炼气期";
                case RealmTier.Foundation: return "筑基期";
                case RealmTier.GoldenCore: return "金丹期";
                case RealmTier.NascentSoul: return "元婴期";
                case RealmTier.SpiritSevering: return "化神期";
                case RealmTier.Tribulation: return "渡劫期";
                case RealmTier.Immortal: return "真仙境";
                default: return tier.ToString();
            }
        }

        public static string SubStageCn(int subStage, int maxSubStage)
        {
            if (maxSubStage <= 1) return "圆满";
            if (subStage <= 1) return "初期";
            if (subStage == 2) return "中期";
            if (subStage == 3 && maxSubStage >= 4) return "后期";
            if (subStage >= maxSubStage) return "大圆满";
            return string.Format("第{0}重", subStage);
        }

        public static string RealmFullCn(RealmTier tier, int subStage, int maxSubStage = 4)
        {
            return string.Format("{0}·{1}", RealmBaseCn(tier), SubStageCn(subStage, maxSubStage));
        }

        public static string ResCn(ResourceType res)
        {
            switch (res)
            {
                case ResourceType.SpiritStone: return "灵石";
                case ResourceType.SpiritWood: return "灵木";
                case ResourceType.SpiritOre: return "玄铁";
                case ResourceType.SpiritFood: return "灵谷";
                case ResourceType.SpiritHerb: return "灵草";
                case ResourceType.MonsterCore: return "妖丹";
                case ResourceType.SectReputation: return "声望";
                default: return res.ToString();
            }
        }

        public static string JobCn(DiscipleJob job)
        {
            switch (job)
            {
                case DiscipleJob.Cultivate: return "打坐苦修";
                case DiscipleJob.Farm: return "灵田农耕";
                case DiscipleJob.Lumber: return "灵林采伐";
                case DiscipleJob.Mine: return "矿脉采掘";
                case DiscipleJob.Alchemy: return "丹房司火";
                case DiscipleJob.Forge: return "器坊铸造";
                case DiscipleJob.Guard: return "护山出战";
                default: return job.ToString();
            }
        }

        public static string BldCategoryCn(BuildingCategory cat)
        {
            switch (cat)
            {
                case BuildingCategory.Core: return "宗门核心";
                case BuildingCategory.Gather: return "资源采植";
                case BuildingCategory.Craft: return "丹器百艺";
                case BuildingCategory.Cultivation: return "聚灵悟道";
                case BuildingCategory.Living: return "洞府居所";
                case BuildingCategory.Defense: return "护山防卫";
                default: return cat.ToString();
            }
        }

        public static string ItemCategoryCn(ItemCategory cat)
        {
            switch (cat)
            {
                case ItemCategory.Material: return "天材地宝";
                case ItemCategory.CultivationPill: return "修为灵丹";
                case ItemCategory.BreakthroughPill: return "破境宝丹";
                case ItemCategory.CombatPill: return "战时恢复丹";
                case ItemCategory.Weapon: return "飞剑法兵";
                case ItemCategory.Armor: return "护体宝衣";
                case ItemCategory.Talisman: return "本命法宝";
                default: return cat.ToString();
            }
        }

        public static string CombatModeCn(CombatMode mode)
        {
            switch (mode)
            {
                case CombatMode.IdleExpedition: return "秘境挂机历练";
                case CombatMode.BossChallenge: return "秘境首领讨伐";
                case CombatMode.SectRaidDefense: return "宗门护山防卫";
                default: return mode.ToString();
            }
        }

        /// <summary>
        /// 判定五行相生（src 生 dst：金生水、水生木、木生火、火生土、土生金）
        /// </summary>
        public static bool IsGenerating(ElementType src, ElementType dst)
        {
            if (src == ElementType.None || dst == ElementType.None) return false;
            return (src == ElementType.Metal && dst == ElementType.Water) ||
                   (src == ElementType.Water && dst == ElementType.Wood) ||
                   (src == ElementType.Wood && dst == ElementType.Fire) ||
                   (src == ElementType.Fire && dst == ElementType.Earth) ||
                   (src == ElementType.Earth && dst == ElementType.Metal);
        }

        /// <summary>
        /// 判定五行相克（src 克 dst：金克木、木克土、土克水、水克火、火克金）
        /// </summary>
        public static bool IsOvercoming(ElementType src, ElementType dst)
        {
            if (src == ElementType.None || dst == ElementType.None) return false;
            return (src == ElementType.Metal && dst == ElementType.Wood) ||
                   (src == ElementType.Wood && dst == ElementType.Earth) ||
                   (src == ElementType.Earth && dst == ElementType.Water) ||
                   (src == ElementType.Water && dst == ElementType.Fire) ||
                   (src == ElementType.Fire && dst == ElementType.Metal);
        }
    }
}
