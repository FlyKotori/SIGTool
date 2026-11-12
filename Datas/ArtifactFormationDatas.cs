using System;
using System.Collections.Generic;

namespace SIGTool.Datas
{
    /// <summary>
    /// 单条法宝随机副词条数据（SYS-01）
    /// </summary>
    public class ArtifactAffixData
    {
        public ArtifactAffixType AffixType { get; set; } = ArtifactAffixType.AtkFlat;
        public double Value { get; set; } = 0.0;

        public string FormatAffixLine(double resonanceMult = 1.0)
        {
            double eff = Value * resonanceMult;
            switch (AffixType)
            {
                case ArtifactAffixType.AtkFlat:
                    return string.Format("攻击 +{0:F1}", eff);
                case ArtifactAffixType.AtkPct:
                    return string.Format("攻击加成 +{0:P1}", eff);
                case ArtifactAffixType.DefFlat:
                    return string.Format("防御 +{0:F1}", eff);
                case ArtifactAffixType.DefPct:
                    return string.Format("防御加成 +{0:P1}", eff);
                case ArtifactAffixType.HpFlat:
                    return string.Format("气血上限 +{0:F0}", eff);
                case ArtifactAffixType.HpPct:
                    return string.Format("气血加成 +{0:P1}", eff);
                case ArtifactAffixType.SpeedFlat:
                    return string.Format("出手速度 +{0:F1}", eff);
                case ArtifactAffixType.CritRatePct:
                    return string.Format("暴击率 +{0:P1}", eff);
                case ArtifactAffixType.DodgeRatePct:
                    return string.Format("闪避率 +{0:P1}", eff);
                case ArtifactAffixType.LifestealPct:
                    return string.Format("斗法吸血 +{0:P1}", eff);
                case ArtifactAffixType.QiRatePct:
                    return string.Format("吐纳速率 +{0:P1}", eff);
                case ArtifactAffixType.BarrierHpFlat:
                    return string.Format("护山结界 +{0:F0}", eff);
                default:
                    return string.Format("{0} +{1:F2}", AffixType, eff);
            }
        }
    }

    /// <summary>
    /// 随机词条法宝实体实例（每件拥有独立 ID、五行、品阶、基础白值与 1~4 条随机副词条）
    /// </summary>
    public class ArtifactInstanceData
    {
        public string InstanceId { get; set; } = "";
        public string TemplateId { get; set; } = "";
        public string NameCn { get; set; } = "";
        public string ShortName { get; set; } = "";
        public ElementType Element { get; set; } = ElementType.Metal;
        public ArtifactRarity Rarity { get; set; } = ArtifactRarity.Common;
        public int Tier { get; set; } = 1;

        public double BaseAtk { get; set; } = 0.0;
        public double BaseDef { get; set; } = 0.0;
        public double BaseHp { get; set; } = 0.0;

        public List<ArtifactAffixData> Affixes { get; set; } = new List<ArtifactAffixData>();

        /// <summary>防误拆保护锁</summary>
        public bool IsLockedProtect { get; set; } = false;

        /// <summary>当前镶嵌的护山大阵阵眼索引（-1 表示未上阵在库中，0~8 表示对应九宫阵眼）</summary>
        public int SlottedEyeIndex { get; set; } = -1;

        public string GetDisplayShortTitle()
        {
            string baseShort = !string.IsNullOrEmpty(ShortName) ? ShortName : NameCn;
            return IsLockedProtect ? ("[锁]" + baseShort) : baseShort;
        }
    }

    /// <summary>
    /// 护山大阵 3×3 九宫阵眼槽位数据（0~8 共九宫方位，各自具备五行灵枢属性）
    /// </summary>
    public class FormationEyeSlotData
    {
        public int EyeIndex { get; set; } = 0;
        public int Row { get; set; } = 0;
        public int Col { get; set; } = 0;
        public string PalaceName { get; set; } = ""; // 乾宫 / 坎宫 / 中宫天枢 等
        public ElementType AffinityElement { get; set; } = ElementType.Earth;
        public bool IsUnlocked { get; set; } = false;
        public double UnlockStoneCost { get; set; } = 180.0;
        public double UnlockCoreCost { get; set; } = 2.0;

        /// <summary>当前镇压在此阵眼中的法宝 InstanceId（空串代表未镶嵌）</summary>
        public string EquippedArtifactInstanceId { get; set; } = "";
    }

    /// <summary>
    /// 护山大阵实时汇总增益快照（不入盘，由逻辑层实时聚合供修炼、宗门结界与斗法读取）
    /// </summary>
    public class FormationTotalBonusSummary
    {
        public int SlottedCount { get; set; }
        public int DistinctElementCount { get; set; }
        public bool HasSanCaiSynergy { get; set; }     // >=3件：三才护山阵
        public bool HasWuXingSynergy { get; set; }     // 集齐5行：五行混元大阵
        public bool HasJiuGongSynergy { get; set; }    // 9件满阵：九宫太虚仙阵

        public double FlatAtk { get; set; }
        public double PctAtk { get; set; }
        public double FlatDef { get; set; }
        public double PctDef { get; set; }
        public double FlatHp { get; set; }
        public double PctHp { get; set; }
        public double FlatSpeed { get; set; }
        public double CritRateAdd { get; set; }
        public double DodgeRateAdd { get; set; }
        public double LifestealRateAdd { get; set; }
        public double QiRatePctAdd { get; set; }
        public double BarrierFlatAdd { get; set; }
        public double BarrierPctAdd { get; set; }
        public double BreakthroughRateAdd { get; set; }
    }
}
