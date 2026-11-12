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
    /// SYS-01 随机词条法宝生成、分解与 3×3 九宫护山大阵共鸣/连携逻辑
    /// </summary>
    public static class ArtifactFormationLogic
    {
        /// <summary>
        /// 确保宗门 3x3 九宫护山大阵（乾/坎/艮/震/中宫/巽/离/坤/兑）及法宝槽位双向引用完整一致
        /// </summary>
        public static void EnsureFormationInitialized(SectManageData sect)
        {
            if (sect == null) return;
            if (sect.ArtifactVault == null) sect.ArtifactVault = new List<ArtifactInstanceData>();
            if (sect.ArtifactVaultCap < 30) sect.ArtifactVaultCap = 60;
            if (sect.NextArtifactSeq < 1) sect.NextArtifactSeq = sect.ArtifactVault.Count + 1;

            if (sect.FormationEyes == null || sect.FormationEyes.Count != 9)
            {
                sect.FormationEyes = CreateDefaultNinePalaceEyes();
            }

            // 校验并同步法宝 SlottedEyeIndex 与阵眼 EquippedArtifactInstanceId 双向关联
            foreach (var art in sect.ArtifactVault)
            {
                if (art.Affixes == null) art.Affixes = new List<ArtifactAffixData>();
                art.SlottedEyeIndex = -1;
            }

            for (int i = 0; i < sect.FormationEyes.Count; i++)
            {
                var eye = sect.FormationEyes[i];
                eye.EyeIndex = i;
                if (!eye.IsUnlocked || string.IsNullOrEmpty(eye.EquippedArtifactInstanceId))
                {
                    eye.EquippedArtifactInstanceId = "";
                    continue;
                }

                var matched = sect.FindArtifactById(eye.EquippedArtifactInstanceId);
                if (matched == null || matched.SlottedEyeIndex >= 0)
                {
                    eye.EquippedArtifactInstanceId = "";
                }
                else
                {
                    matched.SlottedEyeIndex = i;
                }
            }
        }

        public static List<FormationEyeSlotData> CreateDefaultNinePalaceEyes()
        {
            return new List<FormationEyeSlotData>
            {
                new FormationEyeSlotData { EyeIndex = 0, Row = 0, Col = 0, PalaceName = "乾宫·西北", AffinityElement = ElementType.Metal, IsUnlocked = false, UnlockStoneCost = 180, UnlockCoreCost = 2 },
                new FormationEyeSlotData { EyeIndex = 1, Row = 0, Col = 1, PalaceName = "坎宫·正北", AffinityElement = ElementType.Water, IsUnlocked = true,  UnlockStoneCost = 0,   UnlockCoreCost = 0 },
                new FormationEyeSlotData { EyeIndex = 2, Row = 0, Col = 2, PalaceName = "艮宫·东北", AffinityElement = ElementType.Earth, IsUnlocked = false, UnlockStoneCost = 220, UnlockCoreCost = 3 },
                new FormationEyeSlotData { EyeIndex = 3, Row = 1, Col = 0, PalaceName = "震宫·正东", AffinityElement = ElementType.Wood,  IsUnlocked = true,  UnlockStoneCost = 0,   UnlockCoreCost = 0 },
                new FormationEyeSlotData { EyeIndex = 4, Row = 1, Col = 1, PalaceName = "中宫·天枢", AffinityElement = ElementType.Earth, IsUnlocked = true,  UnlockStoneCost = 0,   UnlockCoreCost = 0 },
                new FormationEyeSlotData { EyeIndex = 5, Row = 1, Col = 2, PalaceName = "巽宫·东南", AffinityElement = ElementType.Wood,  IsUnlocked = false, UnlockStoneCost = 260, UnlockCoreCost = 4 },
                new FormationEyeSlotData { EyeIndex = 6, Row = 2, Col = 0, PalaceName = "离宫·正南", AffinityElement = ElementType.Fire,  IsUnlocked = true,  UnlockStoneCost = 0,   UnlockCoreCost = 0 },
                new FormationEyeSlotData { EyeIndex = 7, Row = 2, Col = 1, PalaceName = "坤宫·西南", AffinityElement = ElementType.Earth, IsUnlocked = false, UnlockStoneCost = 320, UnlockCoreCost = 5 },
                new FormationEyeSlotData { EyeIndex = 8, Row = 2, Col = 2, PalaceName = "兑宫·正西", AffinityElement = ElementType.Metal, IsUnlocked = true,  UnlockStoneCost = 0,   UnlockCoreCost = 0 }
            };
        }

        /// <summary>
        /// 计算阵眼灵脉与法宝五行的共鸣倍率（中宫天枢额外 +10%，相生 +30%，同源 +20%，相克 -15%）
        /// </summary>
        public static double GetEyeResonanceMultiplier(FormationEyeSlotData eye, ArtifactInstanceData art, out string relationTag)
        {
            relationTag = "气机平和(x1.00)";
            if (eye == null || art == null) return 1.0;

            double mult = (eye.EyeIndex == 4) ? 1.10 : 1.00;
            string corePrefix = (eye.EyeIndex == 4) ? "天枢核心+" : "";

            if (EnumTextMap.IsGenerating(eye.AffinityElement, art.Element))
            {
                mult += 0.30;
                relationTag = string.Format("{0}阵脉相生(x{1:F2})", corePrefix, mult);
            }
            else if (eye.AffinityElement != ElementType.None && eye.AffinityElement == art.Element)
            {
                mult += 0.20;
                relationTag = string.Format("{0}五行同源(x{1:F2})", corePrefix, mult);
            }
            else if (EnumTextMap.IsOvercoming(eye.AffinityElement, art.Element))
            {
                mult -= 0.15;
                relationTag = string.Format("{0}五行相冲(x{1:F2})", corePrefix, mult);
            }
            else
            {
                relationTag = string.Format("{0}气机平顺(x{1:F2})", corePrefix, mult);
            }

            return Math.Max(0.50, mult);
        }

        /// <summary>
        /// 实时汇总九宫护山大阵全部镶嵌法宝词条及连携阵势加成
        /// </summary>
        public static FormationTotalBonusSummary ComputeFormationSummary(SectManageData sect)
        {
            var sum = new FormationTotalBonusSummary();
            if (sect == null || sect.FormationEyes == null || sect.ArtifactVault == null) return sum;

            var slottedPairs = new List<KeyValuePair<FormationEyeSlotData, ArtifactInstanceData>>();
            var elemSet = new HashSet<ElementType>();

            for (int i = 0; i < sect.FormationEyes.Count; i++)
            {
                var eye = sect.FormationEyes[i];
                if (!eye.IsUnlocked || string.IsNullOrEmpty(eye.EquippedArtifactInstanceId)) continue;
                var art = sect.FindArtifactById(eye.EquippedArtifactInstanceId);
                if (art == null) continue;

                slottedPairs.Add(new KeyValuePair<FormationEyeSlotData, ArtifactInstanceData>(eye, art));
                if (art.Element != ElementType.None) elemSet.Add(art.Element);
            }

            sum.SlottedCount = slottedPairs.Count;
            sum.DistinctElementCount = elemSet.Count;
            sum.HasSanCaiSynergy = (sum.SlottedCount >= 3);
            sum.HasWuXingSynergy = (sum.DistinctElementCount >= 5);
            sum.HasJiuGongSynergy = (sum.SlottedCount >= 9);

            // 连携阵势固定奖励
            if (sum.HasSanCaiSynergy)
            {
                sum.BarrierPctAdd += 0.25;
                sum.PctDef += 0.12;
            }
            if (sum.HasWuXingSynergy)
            {
                sum.QiRatePctAdd += 0.25;
                sum.PctAtk += 0.15;
                sum.PctHp += 0.15;
            }
            double fullArrayAmp = 1.0;
            if (sum.HasJiuGongSynergy)
            {
                fullArrayAmp = 1.20;
                sum.BreakthroughRateAdd += 0.08;
            }

            for (int i = 0; i < slottedPairs.Count; i++)
            {
                var eye = slottedPairs[i].Key;
                var art = slottedPairs[i].Value;
                string dummyTag;
                double mult = GetEyeResonanceMultiplier(eye, art, out dummyTag) * fullArrayAmp;

                sum.FlatAtk += art.BaseAtk * mult;
                sum.FlatDef += art.BaseDef * mult;
                sum.FlatHp += art.BaseHp * mult;

                if (art.Affixes == null) continue;
                for (int k = 0; k < art.Affixes.Count; k++)
                {
                    var af = art.Affixes[k];
                    double val = af.Value * mult;
                    switch (af.AffixType)
                    {
                        case ArtifactAffixType.AtkFlat: sum.FlatAtk += val; break;
                        case ArtifactAffixType.AtkPct: sum.PctAtk += val; break;
                        case ArtifactAffixType.DefFlat: sum.FlatDef += val; break;
                        case ArtifactAffixType.DefPct: sum.PctDef += val; break;
                        case ArtifactAffixType.HpFlat: sum.FlatHp += val; break;
                        case ArtifactAffixType.HpPct: sum.PctHp += val; break;
                        case ArtifactAffixType.SpeedFlat: sum.FlatSpeed += val; break;
                        case ArtifactAffixType.CritRatePct: sum.CritRateAdd += val; break;
                        case ArtifactAffixType.DodgeRatePct: sum.DodgeRateAdd += val; break;
                        case ArtifactAffixType.LifestealPct: sum.LifestealRateAdd += val; break;
                        case ArtifactAffixType.QiRatePct: sum.QiRatePctAdd += val; break;
                        case ArtifactAffixType.BarrierHpFlat: sum.BarrierFlatAdd += val; break;
                    }
                }
            }

            return sum;
        }

        /// <summary>
        /// 随机生成一件带随机品阶与不重复副词条的阵宝实例
        /// </summary>
        public static ArtifactInstanceData GenerateRandomArtifact(SectManageData sect, Random rng, int forgeGrade, int maxAllowedTier = 4)
        {
            var templates = SIGConfigTable.Current.ArtifactTemplates;
            var candidates = templates.Where(t => t.Tier <= Math.Max(1, maxAllowedTier)).ToList();
            if (candidates.Count == 0) candidates = templates;

            // 高档位祭炼优先抽取更高阶胚体
            ArtifactTemplateConfig tpl;
            if (forgeGrade >= 3)
            {
                var high = candidates.Where(t => t.Tier >= 2).ToList();
                tpl = (high.Count > 0 ? high : candidates)[rng.Next(high.Count > 0 ? high.Count : candidates.Count)];
            }
            else
            {
                tpl = candidates[rng.Next(candidates.Count)];
            }

            ArtifactRarity rarity = RollRarity(rng, forgeGrade);
            double rarityScale = 1.0;
            switch (rarity)
            {
                case ArtifactRarity.Spirit: rarityScale = 1.25; break;
                case ArtifactRarity.Treasure: rarityScale = 1.58; break;
                case ArtifactRarity.Immortal: rarityScale = 2.05; break;
            }

            int seq = sect != null ? sect.NextArtifactSeq++ : rng.Next(1000, 9999);
            string rarityPrefix = EnumTextMap.RarityCn(rarity);

            var inst = new ArtifactInstanceData
            {
                InstanceId = string.Format("ART_{0:D4}", seq),
                TemplateId = tpl.Id,
                NameCn = string.Format("{0}·{1}", rarityPrefix, tpl.NameCn),
                ShortName = tpl.GetShortLabel(),
                Element = tpl.GetElement(),
                Rarity = rarity,
                Tier = tpl.Tier,
                BaseAtk = Math.Round(tpl.BaseAtk * rarityScale, 1),
                BaseDef = Math.Round(tpl.BaseDef * rarityScale, 1),
                BaseHp = Math.Round(tpl.BaseHp * rarityScale, 0),
                Affixes = new List<ArtifactAffixData>(),
                IsLockedProtect = (rarity >= ArtifactRarity.Treasure),
                SlottedEyeIndex = -1
            };

            int affixCount = (int)rarity; // 凡1 / 灵2 / 宝3 / 仙4
            var pool = (ArtifactAffixType[])Enum.GetValues(typeof(ArtifactAffixType));
            var available = new List<ArtifactAffixType>(pool);

            double tierFactor = 1.0 + (tpl.Tier - 1) * 0.42;
            for (int i = 0; i < affixCount && available.Count > 0; i++)
            {
                int pickIdx = rng.Next(available.Count);
                ArtifactAffixType chosen = available[pickIdx];
                available.RemoveAt(pickIdx);

                double rollRatio = 0.75 + rng.NextDouble() * 0.50; // 0.75 ~ 1.25 浮动极品度
                double val = ComputeAffixBaseValue(chosen, tierFactor, rarityScale, rollRatio);
                inst.Affixes.Add(new ArtifactAffixData
                {
                    AffixType = chosen,
                    Value = val
                });
            }

            return inst;
        }

        private static ArtifactRarity RollRarity(Random rng, int forgeGrade)
        {
            int r = rng.Next(100);
            if (forgeGrade >= 3)
            {
                return r < 28 ? ArtifactRarity.Immortal : ArtifactRarity.Treasure;
            }
            if (forgeGrade == 2)
            {
                if (r < 8) return ArtifactRarity.Immortal;
                if (r < 40) return ArtifactRarity.Treasure;
                return ArtifactRarity.Spirit;
            }
            // forgeGrade == 1
            if (r < 2) return ArtifactRarity.Immortal;
            if (r < 14) return ArtifactRarity.Treasure;
            if (r < 48) return ArtifactRarity.Spirit;
            return ArtifactRarity.Common;
        }

        private static double ComputeAffixBaseValue(ArtifactAffixType type, double tierFactor, double rarityScale, double rollRatio)
        {
            double mult = tierFactor * rarityScale * rollRatio;
            switch (type)
            {
                case ArtifactAffixType.AtkFlat: return Math.Round(10.0 * mult, 1);
                case ArtifactAffixType.AtkPct: return Math.Round(0.045 * mult, 3);
                case ArtifactAffixType.DefFlat: return Math.Round(6.5 * mult, 1);
                case ArtifactAffixType.DefPct: return Math.Round(0.045 * mult, 3);
                case ArtifactAffixType.HpFlat: return Math.Round(75.0 * mult, 0);
                case ArtifactAffixType.HpPct: return Math.Round(0.055 * mult, 3);
                case ArtifactAffixType.SpeedFlat: return Math.Round(4.2 * (0.8 + rarityScale * 0.4) * rollRatio, 1);
                case ArtifactAffixType.CritRatePct: return Math.Round(0.025 * (0.7 + rarityScale * 0.45) * rollRatio, 3);
                case ArtifactAffixType.DodgeRatePct: return Math.Round(0.020 * (0.7 + rarityScale * 0.45) * rollRatio, 3);
                case ArtifactAffixType.LifestealPct: return Math.Round(0.028 * (0.75 + rarityScale * 0.4) * rollRatio, 3);
                case ArtifactAffixType.QiRatePct: return Math.Round(0.060 * mult, 3);
                case ArtifactAffixType.BarrierHpFlat: return Math.Round(120.0 * mult, 0);
                default: return Math.Round(5.0 * mult, 1);
            }
        }

        public static void GetForgeGradeCost(int forgeGrade, out double stone, out double ore, out double core, out int minSectOrForgeLv)
        {
            if (forgeGrade >= 3)
            {
                stone = 560; ore = 180; core = 10; minSectOrForgeLv = 3;
            }
            else if (forgeGrade == 2)
            {
                stone = 220; ore = 80; core = 4; minSectOrForgeLv = 2;
            }
            else
            {
                stone = 80; ore = 35; core = 1; minSectOrForgeLv = 1;
            }
        }

        public static bool ForgeArtifactManual(SIGSaveData save, int forgeGrade, Random rng, SIGEventBus bus, out string message)
        {
            if (save == null || save.Sect == null) { message = "宗门数据无效。"; return false; }
            EnsureFormationInitialized(save.Sect);

            if (save.Sect.ArtifactVault.Count >= save.Sect.ArtifactVaultCap)
            {
                message = string.Format("法宝库已满（{0}/{1}），请先分解冗余低阶法宝。", save.Sect.ArtifactVault.Count, save.Sect.ArtifactVaultCap);
                return false;
            }

            double stone, ore, core;
            int minLv;
            GetForgeGradeCost(forgeGrade, out stone, out ore, out core, out minLv);

            int maxForgeLv = 0;
            foreach (var s in save.Sect.GridSlots)
            {
                if (s.Building != null && !s.Building.IsUnderConstruction && s.Building.ConfigId == "BLD_FORGE")
                {
                    maxForgeLv = Math.Max(maxForgeLv, s.Building.Level);
                }
            }

            if (minLv > 1 && save.Sect.SectLevel < minLv && maxForgeLv < (minLv - 1))
            {
                message = string.Format("该档位注灵祭炼需【掌门大殿 Lv.{0}】或【百炼器坊 Lv.{1}】。", minLv, minLv - 1);
                return false;
            }

            if (save.Sect.GetResource(ResourceType.SpiritStone) < stone ||
                save.Sect.GetResource(ResourceType.SpiritOre) < ore ||
                save.Sect.GetResource(ResourceType.MonsterCore) < core)
            {
                message = string.Format("注灵祭炼资材不足：{0}", SectBuildLogic.FormatShortfallDetail(save, stone, 0, ore, 0, core));
                return false;
            }

            save.Sect.AddResource(ResourceType.SpiritStone, -stone);
            save.Sect.AddResource(ResourceType.SpiritOre, -ore);
            save.Sect.AddResource(ResourceType.MonsterCore, -core);

            int maxAllowedTier = forgeGrade >= 3 ? 4 : (forgeGrade == 2 ? 3 : 2);
            var created = GenerateRandomArtifact(save.Sect, rng, forgeGrade, maxAllowedTier);
            save.Sect.ArtifactVault.Add(created);

            message = string.Format("阵宝出世！成功祭炼【{0}】({1}系 · {2}条副词条)！",
                created.NameCn, EnumTextMap.ElemShortCn(created.Element), created.Affixes.Count);
            if (bus != null)
            {
                bus.PublishLog(save.GetImmortalDateText(), LogCategory.Artifact, message);
                bus.NotifyStateChanged();
            }
            return true;
        }

        public static bool UnlockFormationEye(SIGSaveData save, int eyeIndex, SIGEventBus bus, out string message)
        {
            if (save == null || save.Sect == null) { message = "存档无效。"; return false; }
            EnsureFormationInitialized(save.Sect);
            if (eyeIndex < 0 || eyeIndex >= save.Sect.FormationEyes.Count) { message = "无效阵眼方位。"; return false; }

            var eye = save.Sect.FormationEyes[eyeIndex];
            if (eye.IsUnlocked)
            {
                message = string.Format("【{0}】已处于激活开放状态。", eye.PalaceName);
                return false;
            }

            if (save.Sect.GetResource(ResourceType.SpiritStone) < eye.UnlockStoneCost ||
                save.Sect.GetResource(ResourceType.MonsterCore) < eye.UnlockCoreCost)
            {
                message = string.Format("开辟【{0}】资材不足：{1}",
                    eye.PalaceName, SectBuildLogic.FormatShortfallDetail(save, eye.UnlockStoneCost, 0, 0, 0, eye.UnlockCoreCost));
                return false;
            }

            save.Sect.AddResource(ResourceType.SpiritStone, -eye.UnlockStoneCost);
            save.Sect.AddResource(ResourceType.MonsterCore, -eye.UnlockCoreCost);
            eye.IsUnlocked = true;

            message = string.Format("成功引动地脉开辟护山阵眼【{0}】（{1}灵枢）！", eye.PalaceName, EnumTextMap.ElemCn(eye.AffinityElement));
            if (bus != null)
            {
                bus.PublishLog(save.GetImmortalDateText(), LogCategory.Artifact, message);
                bus.NotifyStateChanged();
            }
            return true;
        }

        public static bool SlotArtifactIntoEye(SIGSaveData save, int eyeIndex, string artifactInstanceId, SIGEventBus bus, out string message)
        {
            if (save == null || save.Sect == null) { message = "存档无效。"; return false; }
            EnsureFormationInitialized(save.Sect);
            if (eyeIndex < 0 || eyeIndex >= save.Sect.FormationEyes.Count) { message = "无效阵眼编号。"; return false; }

            var eye = save.Sect.FormationEyes[eyeIndex];
            if (!eye.IsUnlocked)
            {
                message = string.Format("阵眼【{0}】尚未开辟，请先点击【开辟阵眼】。", eye.PalaceName);
                return false;
            }

            var art = save.Sect.FindArtifactById(artifactInstanceId);
            if (art == null)
            {
                message = "未找到选中的法宝实例。";
                return false;
            }

            // 若该法宝原本已在其他阵眼，先从原阵眼移出
            if (art.SlottedEyeIndex >= 0 && art.SlottedEyeIndex < save.Sect.FormationEyes.Count && art.SlottedEyeIndex != eyeIndex)
            {
                save.Sect.FormationEyes[art.SlottedEyeIndex].EquippedArtifactInstanceId = "";
            }

            // 若目标阵眼已有旧法宝，卸回法宝库
            if (!string.IsNullOrEmpty(eye.EquippedArtifactInstanceId) && eye.EquippedArtifactInstanceId != art.InstanceId)
            {
                var prevArt = save.Sect.FindArtifactById(eye.EquippedArtifactInstanceId);
                if (prevArt != null) prevArt.SlottedEyeIndex = -1;
            }

            eye.EquippedArtifactInstanceId = art.InstanceId;
            art.SlottedEyeIndex = eyeIndex;

            SectBuildLogic.RefreshSectDerivedCaps(save);
            CultivationLogic.RecalculateAllCharacters(save);

            string relTag;
            GetEyeResonanceMultiplier(eye, art, out relTag);
            message = string.Format("已将【{0}】镇入【{1}】（{2}），掌门当前战力 {3:F0}！",
                art.NameCn, eye.PalaceName, relTag, save.Leader.EstimateCombatPower());
            if (bus != null)
            {
                bus.PublishLog(save.GetImmortalDateText(), LogCategory.Artifact, message);
                bus.NotifyStateChanged();
            }
            return true;
        }

        public static bool UnslotEye(SIGSaveData save, int eyeIndex, SIGEventBus bus, out string message)
        {
            if (save == null || save.Sect == null) { message = "存档无效。"; return false; }
            EnsureFormationInitialized(save.Sect);
            if (eyeIndex < 0 || eyeIndex >= save.Sect.FormationEyes.Count) { message = "无效阵眼。"; return false; }

            var eye = save.Sect.FormationEyes[eyeIndex];
            if (string.IsNullOrEmpty(eye.EquippedArtifactInstanceId))
            {
                message = "该阵眼当前未镶嵌法宝。";
                return false;
            }

            var art = save.Sect.FindArtifactById(eye.EquippedArtifactInstanceId);
            string artName = art != null ? art.NameCn : eye.EquippedArtifactInstanceId;
            if (art != null) art.SlottedEyeIndex = -1;
            eye.EquippedArtifactInstanceId = "";

            SectBuildLogic.RefreshSectDerivedCaps(save);
            CultivationLogic.RecalculateAllCharacters(save);

            message = string.Format("已从【{0}】取回法宝【{1}】。", eye.PalaceName, artName);
            if (bus != null)
            {
                bus.PublishLog(save.GetImmortalDateText(), LogCategory.Artifact, message);
                bus.NotifyStateChanged();
            }
            return true;
        }

        public static void GetSalvageRefund(ArtifactInstanceData art, out double stone, out double ore, out double core)
        {
            int r = (int)art.Rarity;
            stone = 25 * r * art.Tier;
            ore = 12 * r * art.Tier;
            core = r >= 3 ? (r - 2) : 0;
        }

        public static bool SalvageSingleArtifact(SIGSaveData save, string artifactId, SIGEventBus bus, out string message)
        {
            if (save == null || save.Sect == null) { message = "存档无效。"; return false; }
            var art = save.Sect.FindArtifactById(artifactId);
            if (art == null) { message = "未选中有效法宝。"; return false; }
            if (art.SlottedEyeIndex >= 0) { message = "该法宝正在护山大阵镇压中，请先卸下再分解。"; return false; }
            if (art.IsLockedProtect) { message = "该法宝处于锁定保护状态，如需拆解请先在右键菜单中解锁。"; return false; }

            double st, or, cr;
            GetSalvageRefund(art, out st, out or, out cr);
            save.Sect.ArtifactVault.Remove(art);
            save.Sect.AddResource(ResourceType.SpiritStone, st);
            save.Sect.AddResource(ResourceType.SpiritOre, or);
            if (cr > 0) save.Sect.AddResource(ResourceType.MonsterCore, cr);

            message = string.Format("已熔炼拆解【{0}】，回收灵石 +{1:F0}、玄铁 +{2:F0}{3}。",
                art.NameCn, st, or, cr > 0 ? string.Format("、妖丹 +{0:F0}", cr) : "");
            if (bus != null)
            {
                bus.PublishLog(save.GetImmortalDateText(), LogCategory.Artifact, message);
                bus.NotifyStateChanged();
            }
            return true;
        }

        public static int BatchSalvageUnlockedCommon(SIGSaveData save, SIGEventBus bus, out string message)
        {
            if (save == null || save.Sect == null) { message = "存档无效。"; return 0; }
            var toRemove = save.Sect.ArtifactVault
                .Where(a => a.SlottedEyeIndex < 0 && !a.IsLockedProtect && a.Rarity == ArtifactRarity.Common)
                .ToList();

            if (toRemove.Count == 0)
            {
                message = "库中没有可分解的「未上阵且未锁定」凡品法宝。";
                return 0;
            }

            double sumStone = 0, sumOre = 0;
            foreach (var a in toRemove)
            {
                double st, or, cr;
                GetSalvageRefund(a, out st, out or, out cr);
                sumStone += st;
                sumOre += or;
                save.Sect.ArtifactVault.Remove(a);
            }

            save.Sect.AddResource(ResourceType.SpiritStone, sumStone);
            save.Sect.AddResource(ResourceType.SpiritOre, sumOre);
            message = string.Format("批量熔炼完成：共拆解 {0} 件凡品法宝，回收灵石 +{1:F0}、玄铁 +{2:F0}。",
                toRemove.Count, sumStone, sumOre);
            if (bus != null)
            {
                bus.PublishLog(save.GetImmortalDateText(), LogCategory.Artifact, message);
                bus.NotifyStateChanged();
            }
            return toRemove.Count;
        }

        /// <summary>
        /// 预览若将指定法宝镶嵌至当前选中的阵眼（替换原有法宝），掌门战力、攻防气血、吐纳与护山结界的净差值
        /// </summary>
        public static string PreviewSlotDeltaText(SIGSaveData save, int targetEyeIdx, ArtifactInstanceData candidate)
        {
            if (save == null || save.Sect == null || candidate == null) return "";
            EnsureFormationInitialized(save.Sect);
            if (targetEyeIdx < 0 || targetEyeIdx >= save.Sect.FormationEyes.Count) return "";

            var eye = save.Sect.FormationEyes[targetEyeIdx];
            if (eye.EquippedArtifactInstanceId == candidate.InstanceId)
            {
                return string.Format("【当前已镇守于 {0}】", eye.PalaceName);
            }

            double beforePow = save.Leader.EstimateCombatPower();
            double beforeAtk = save.Leader.BattleStats.Attack;
            double beforeDef = save.Leader.BattleStats.Defense;
            double beforeHp = save.Leader.BattleStats.MaxHp;
            double beforeQi = CultivationLogic.GetQiRatePerSec(save.Leader, save.Sect);
            double beforeBar = SectBuildLogic.GetTotalBarrierMaxHp(save.Sect);

            // 记录现场以便无损还原
            string prevInTargetEye = eye.EquippedArtifactInstanceId;
            int candOrigEye = candidate.SlottedEyeIndex;
            bool origUnlocked = eye.IsUnlocked;

            eye.IsUnlocked = true;
            if (candOrigEye >= 0 && candOrigEye < save.Sect.FormationEyes.Count)
                save.Sect.FormationEyes[candOrigEye].EquippedArtifactInstanceId = "";
            if (!string.IsNullOrEmpty(prevInTargetEye))
            {
                var prevArt = save.Sect.FindArtifactById(prevInTargetEye);
                if (prevArt != null) prevArt.SlottedEyeIndex = -1;
            }
            eye.EquippedArtifactInstanceId = candidate.InstanceId;
            candidate.SlottedEyeIndex = targetEyeIdx;

            CultivationLogic.RecalculateStats(save.Leader, save.Sect);
            double dPow = save.Leader.EstimateCombatPower() - beforePow;
            double dAtk = save.Leader.BattleStats.Attack - beforeAtk;
            double dDef = save.Leader.BattleStats.Defense - beforeDef;
            double dHp = save.Leader.BattleStats.MaxHp - beforeHp;
            double dQi = CultivationLogic.GetQiRatePerSec(save.Leader, save.Sect) - beforeQi;
            double dBar = SectBuildLogic.GetTotalBarrierMaxHp(save.Sect) - beforeBar;

            // 严格还原现场
            eye.IsUnlocked = origUnlocked;
            eye.EquippedArtifactInstanceId = prevInTargetEye;
            if (!string.IsNullOrEmpty(prevInTargetEye))
            {
                var prevArt = save.Sect.FindArtifactById(prevInTargetEye);
                if (prevArt != null) prevArt.SlottedEyeIndex = targetEyeIdx;
            }
            candidate.SlottedEyeIndex = candOrigEye;
            if (candOrigEye >= 0 && candOrigEye < save.Sect.FormationEyes.Count)
            {
                save.Sect.FormationEyes[candOrigEye].EquippedArtifactInstanceId = candidate.InstanceId;
            }
            CultivationLogic.RecalculateStats(save.Leader, save.Sect);

            var sb = new StringBuilder();
            sb.AppendFormat("镇入【{0}】差值 ➔ 战力:{1}  攻:{2}  防:{3}  血:{4}  吐纳:{5}/s  结界:{6}",
                eye.PalaceName,
                FmtDelta(dPow, "F0"), FmtDelta(dAtk, "F1"), FmtDelta(dDef, "F1"),
                FmtDelta(dHp, "F0"), FmtDelta(dQi, "F1"), FmtDelta(dBar, "F0"));
            return sb.ToString();
        }

        private static string FmtDelta(double val, string fmt)
        {
            if (Math.Abs(val) < 0.05) return "±0";
            return (val > 0 ? "+" : "") + val.ToString(fmt);
        }
    }
}
