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
    /// 宗门灵脉建造、五行风水相生、资源挂机产出/消耗核算与门人排班逻辑（统一采用 SIG 命名）
    /// </summary>
    public static class SectBuildLogic
    {
        private static readonly string[] SurnamePool = { "林", "苏", "叶", "顾", "萧", "白", "沈", "陆", "秦", "洛", "楚", "韩" };
        private static readonly string[] GivenNamePool = { "清风", "明月", "玄尘", "紫菱", "长歌", "知秋", "若水", "怀瑾", "星河", "寒山", "青羽", "云舒" };

        public static double GetSlotEfficiencyMultiplier(SectGridSlotData slot, BuildingConfig cfg)
        {
            if (slot == null || cfg == null) return 1.0;
            var bal = SIGConfigTable.Current.Balance;
            ElementType vein = slot.VeinElement;
            ElementType bldElem = cfg.GetElement();

            double wuxingDelta = 0.0;
            if (EnumTextMap.IsGenerating(vein, bldElem))
            {
                wuxingDelta = bal.WuXingGenerateBonus;
            }
            else if (vein != ElementType.None && vein == bldElem)
            {
                wuxingDelta = bal.WuXingSameBonus;
            }
            else if (EnumTextMap.IsOvercoming(vein, bldElem))
            {
                wuxingDelta = bal.WuXingOvercomePenalty;
            }

            return Math.Max(0.4, slot.BaseAura * (1.0 + wuxingDelta));
        }

        public static string GetWuXingRelationTag(ElementType vein, ElementType bldElem)
        {
            if (vein == ElementType.None || bldElem == ElementType.None) return "中正平和";
            if (EnumTextMap.IsGenerating(vein, bldElem)) return "五行相生(+25%)";
            if (vein == bldElem) return "五行同源(+12%)";
            if (EnumTextMap.IsOvercoming(vein, bldElem)) return "灵脉相克(-15%)";
            return "平顺无碍";
        }

        public static int GetCountOfBuilding(SectManageData sect, string bldId)
        {
            if (sect == null || sect.GridSlots == null) return 0;
            return sect.GridSlots.Count(s => s.Building != null && s.Building.ConfigId == bldId);
        }

        public static int GetMaxDiscipleCap(SectManageData sect)
        {
            int cap = SIGConfigTable.Current.Balance.BaseDiscipleCap;
            if (sect == null || sect.GridSlots == null) return cap;

            foreach (var slot in sect.GridSlots)
            {
                if (slot.Building == null || slot.Building.IsUnderConstruction) continue;
                var cfg = SIGConfigTable.GetBuilding(slot.Building.ConfigId);
                if (cfg != null && cfg.DiscipleCapBonus > 0)
                {
                    cap += cfg.DiscipleCapBonus + (slot.Building.Level - 1);
                }
            }
            return cap;
        }

        public static double GetTotalSectAuraBonus(SectManageData sect)
        {
            double bonus = 0.0;
            if (sect == null) return bonus;

            if (sect.GridSlots != null)
            {
                foreach (var slot in sect.GridSlots)
                {
                    if (slot.Building == null || slot.Building.IsUnderConstruction) continue;
                    var cfg = SIGConfigTable.GetBuilding(slot.Building.ConfigId);
                    if (cfg != null && cfg.AuraBonusRate > 0)
                    {
                        double mult = GetSlotEfficiencyMultiplier(slot, cfg);
                        double lvScale = 1.0 + (slot.Building.Level - 1) * 0.35;
                        bonus += cfg.AuraBonusRate * lvScale * mult;
                    }
                }
            }

            var fm = ArtifactFormationLogic.ComputeFormationSummary(sect);
            bonus += fm.QiRatePctAdd;
            return bonus;
        }

        public static double GetTotalBarrierMaxHp(SectManageData sect)
        {
            double maxHp = 350.0;
            if (sect == null) return maxHp;

            if (sect.GridSlots != null)
            {
                foreach (var slot in sect.GridSlots)
                {
                    if (slot.Building == null || slot.Building.IsUnderConstruction) continue;
                    var cfg = SIGConfigTable.GetBuilding(slot.Building.ConfigId);
                    if (cfg != null && cfg.DefenseBarrierBonus > 0)
                    {
                        double mult = GetSlotEfficiencyMultiplier(slot, cfg);
                        double lvScale = 1.0 + (slot.Building.Level - 1) * 0.45;
                        maxHp += cfg.DefenseBarrierBonus * lvScale * mult;
                    }
                }
            }

            var fm = ArtifactFormationLogic.ComputeFormationSummary(sect);
            maxHp = (maxHp + fm.BarrierFlatAdd) * (1.0 + fm.BarrierPctAdd);
            return Math.Round(maxHp, 0);
        }

        public static double GetDefenseTowerSupportDamage(SIGSaveData save)
        {
            if (save == null || save.Sect == null) return 0.0;
            double dmg = 0.0;
            foreach (var slot in save.Sect.GridSlots)
            {
                if (slot.Building == null || slot.Building.IsUnderConstruction) continue;
                if (slot.Building.ConfigId == "BLD_TOWER")
                {
                    var cfg = SIGConfigTable.GetBuilding("BLD_TOWER");
                    double mult = GetSlotEfficiencyMultiplier(slot, cfg);
                    dmg += (28.0 + (slot.Building.Level - 1) * 18.0) * mult;
                }
            }
            int guardCount = save.Disciples.Count(d => d.AssignedJob == DiscipleJob.Guard);
            return dmg * (1.0 + guardCount * 0.15);
        }

        public static void RefreshSectDerivedCaps(SIGSaveData save)
        {
            if (save == null || save.Sect == null) return;

            int hallLv = 1;
            foreach (var slot in save.Sect.GridSlots)
            {
                if (slot.Building != null && !slot.Building.IsUnderConstruction && slot.Building.ConfigId == "BLD_HALL")
                {
                    hallLv = Math.Max(hallLv, slot.Building.Level);
                }
            }
            save.Sect.SectLevel = hallLv;

            double newMaxBarrier = GetTotalBarrierMaxHp(save.Sect);
            save.Sect.BarrierMaxHp = newMaxBarrier;
            if (save.Sect.BarrierCurHp > newMaxBarrier)
            {
                save.Sect.BarrierCurHp = newMaxBarrier;
            }
        }

        public static Dictionary<ResourceType, double> CalculateResourceRatesPerSec(SIGSaveData save)
        {
            var rates = new Dictionary<ResourceType, double>();
            foreach (ResourceType rt in Enum.GetValues(typeof(ResourceType)))
            {
                rates[rt] = 0.0;
            }
            if (save == null || save.Sect == null) return rates;

            int farmWorkers = save.Disciples.Count(d => d.AssignedJob == DiscipleJob.Farm);
            int lumberWorkers = save.Disciples.Count(d => d.AssignedJob == DiscipleJob.Lumber);
            int mineWorkers = save.Disciples.Count(d => d.AssignedJob == DiscipleJob.Mine);

            foreach (var slot in save.Sect.GridSlots)
            {
                if (!slot.IsUnlocked || slot.Building == null || slot.Building.IsUnderConstruction) continue;
                var cfg = SIGConfigTable.GetBuilding(slot.Building.ConfigId);
                if (cfg == null) continue;

                double slotMult = GetSlotEfficiencyMultiplier(slot, cfg);
                double lvMult = 1.0 + (slot.Building.Level - 1) * 0.45;

                double workerBonus = 1.0;
                if (cfg.Id == "BLD_FARM") workerBonus += farmWorkers * 0.35;
                else if (cfg.Id == "BLD_LUMBER") workerBonus += lumberWorkers * 0.35;
                else if (cfg.Id == "BLD_MINE") workerBonus += mineWorkers * 0.35;

                ResourceType primaryRes;
                if (!string.IsNullOrEmpty(cfg.ProduceResourceKey) &&
                    Enum.TryParse(cfg.ProduceResourceKey, true, out primaryRes))
                {
                    rates[primaryRes] += cfg.BaseOutputPerSec * lvMult * slotMult * workerBonus;
                }

                ResourceType secRes;
                if (!string.IsNullOrEmpty(cfg.SecondaryResourceKey) &&
                    Enum.TryParse(cfg.SecondaryResourceKey, true, out secRes))
                {
                    rates[secRes] += cfg.SecondaryOutputPerSec * lvMult * slotMult * workerBonus;
                }
            }

            return rates;
        }

        /// <summary>
        /// UI-UX-04：计算每秒均摊消耗速率（未辟谷门人月度口粮均摊 + 开启自动循环的丹房/器坊均摊耗材）
        /// </summary>
        public static Dictionary<ResourceType, double> CalculateResourceDrainPerSec(SIGSaveData save)
        {
            var drain = new Dictionary<ResourceType, double>();
            foreach (ResourceType rt in Enum.GetValues(typeof(ResourceType)))
            {
                drain[rt] = 0.0;
            }
            if (save == null || save.Sect == null) return drain;

            double secPerMonth = Math.Max(1.0, SIGConfigTable.Current.Balance.SecondsPerSIGMonth);
            int mortalEaters = save.Disciples.Count(d => d.Realm < RealmTier.Foundation);
            if (mortalEaters > 0)
            {
                drain[ResourceType.SpiritFood] += (mortalEaters * SIGConfigTable.Current.Balance.DiscipleMonthlyFoodCost) / secPerMonth;
            }

            foreach (var slot in save.Sect.GridSlots)
            {
                if (!slot.IsUnlocked || slot.Building == null || slot.Building.IsUnderConstruction) continue;
                var bld = slot.Building;
                if (string.IsNullOrEmpty(bld.ActiveRecipeId)) continue;
                if (!bld.AutoRepeatRecipe && bld.CraftProgressSec <= 0.001) continue;

                var rcp = SIGConfigTable.GetRecipe(bld.ActiveRecipeId);
                if (rcp == null) continue;

                double spd = Math.Max(0.1, CraftAndItemLogic.GetEffectiveSpeedMult(save, slot, bld));
                double cycleSec = Math.Max(0.5, rcp.CraftTimeSec / spd);

                if (rcp.CostSpiritStone > 0) drain[ResourceType.SpiritStone] += rcp.CostSpiritStone / cycleSec;
                if (rcp.CostHerb > 0) drain[ResourceType.SpiritHerb] += rcp.CostHerb / cycleSec;
                if (rcp.CostOre > 0) drain[ResourceType.SpiritOre] += rcp.CostOre / cycleSec;
                if (rcp.CostWood > 0) drain[ResourceType.SpiritWood] += rcp.CostWood / cycleSec;
                if (rcp.CostMonsterCore > 0) drain[ResourceType.MonsterCore] += rcp.CostMonsterCore / cycleSec;
            }

            return drain;
        }

        /// <summary>
        /// UI-UX-04：生成单项资源收支构成明细（供顶部资源格悬停/点击输出至底部状态栏）
        /// </summary>
        public static string BuildResourceBreakdownText(SIGSaveData save, ResourceType rt)
        {
            if (save == null || save.Sect == null) return "";
            var prod = CalculateResourceRatesPerSec(save);
            var drain = CalculateResourceDrainPerSec(save);
            double cur = save.Sect.GetResource(rt);
            double p = prod[rt];
            double d = drain[rt];
            double net = p - d;

            string drainSrc = (rt == ResourceType.SpiritFood) ? "门人口粮均摊" : "百艺开炉均摊";
            if (rt == ResourceType.MonsterCore)
            {
                return string.Format("【{0}收支明细】 当前库存: {1:F0}  |  主要来源: 秘境妖邪掉落  |  百艺开炉均摊消耗: -{2:F2}/s",
                    EnumTextMap.ResCn(rt), Math.Floor(cur), d);
            }
            return string.Format("【{0}收支明细】 当前存量: {1:F0}  |  建筑灵脉产出: +{2:F2}/s  |  {3}: -{4:F2}/s  |  预估净速率: {5}{6:F2}/s",
                EnumTextMap.ResCn(rt), Math.Floor(cur), p, drainSrc, d, net >= 0 ? "+" : "", net);
        }

        /// <summary>
        /// UI-UX-03：计算资材短缺差额及按当前净产速预计集齐所需秒数
        /// </summary>
        public static string FormatShortfallDetail(SIGSaveData save, double reqStone = 0, double reqWood = 0, double reqOre = 0, double reqHerb = 0, double reqCore = 0)
        {
            if (save == null || save.Sect == null) return "";
            var prod = CalculateResourceRatesPerSec(save);
            var drain = CalculateResourceDrainPerSec(save);

            var parts = new List<string>();
            double maxEtaSec = 0.0;
            bool hasUnsourcedShortfall = false;

            AppendOneShortfall(save, ResourceType.SpiritStone, reqStone, prod[ResourceType.SpiritStone] - drain[ResourceType.SpiritStone], parts, ref maxEtaSec, ref hasUnsourcedShortfall);
            AppendOneShortfall(save, ResourceType.SpiritWood, reqWood, prod[ResourceType.SpiritWood] - drain[ResourceType.SpiritWood], parts, ref maxEtaSec, ref hasUnsourcedShortfall);
            AppendOneShortfall(save, ResourceType.SpiritOre, reqOre, prod[ResourceType.SpiritOre] - drain[ResourceType.SpiritOre], parts, ref maxEtaSec, ref hasUnsourcedShortfall);
            AppendOneShortfall(save, ResourceType.SpiritHerb, reqHerb, prod[ResourceType.SpiritHerb] - drain[ResourceType.SpiritHerb], parts, ref maxEtaSec, ref hasUnsourcedShortfall);
            AppendOneShortfall(save, ResourceType.MonsterCore, reqCore, prod[ResourceType.MonsterCore] - drain[ResourceType.MonsterCore], parts, ref maxEtaSec, ref hasUnsourcedShortfall);

            if (parts.Count == 0) return "";
            var sb = new StringBuilder();
            sb.Append("还差 ");
            sb.Append(string.Join("、", parts.ToArray()));
            if (!hasUnsourcedShortfall && maxEtaSec > 0.1)
            {
                sb.AppendFormat("（按当前净产速约 {0:F0} 秒集齐）", Math.Ceiling(maxEtaSec));
            }
            else if (hasUnsourcedShortfall)
            {
                sb.Append("（部分缺口需增建采集建筑或前往秘境获取）");
            }
            return sb.ToString();
        }

        private static void AppendOneShortfall(SIGSaveData save, ResourceType rt, double req, double netRate,
            List<string> parts, ref double maxEtaSec, ref bool hasUnsourcedShortfall)
        {
            if (req <= 0) return;
            double cur = save.Sect.GetResource(rt);
            if (cur >= req) return;
            double diff = Math.Ceiling(req - cur);
            parts.Add(string.Format("{0} {1:F0}", EnumTextMap.ResCn(rt), diff));
            if (netRate > 0.02)
            {
                double eta = diff / netRate;
                if (eta > maxEtaSec) maxEtaSec = eta;
            }
            else
            {
                hasUnsourcedShortfall = true;
            }
        }

        public static void GetBuildOrUpgradeCost(BuildingConfig cfg, int targetLevel,
            out double stone, out double wood, out double ore, out double buildSec)
        {
            double scale = Math.Pow(1.65, Math.Max(0, targetLevel - 1));
            stone = Math.Round(cfg.CostSpiritStone * scale, 0);
            wood = Math.Round(cfg.CostWood * scale, 0);
            ore = Math.Round(cfg.CostOre * scale, 0);
            buildSec = Math.Round(cfg.BuildTimeSec * (1.0 + (targetLevel - 1) * 0.5), 1);
        }

        public static bool StartConstruct(SIGSaveData save, int slotIndex, string buildingConfigId, SIGEventBus bus, out string message)
        {
            if (save == null || slotIndex < 0 || slotIndex >= save.Sect.GridSlots.Count)
            {
                message = "无效的灵脉地块编号。";
                return false;
            }

            var slot = save.Sect.GridSlots[slotIndex];
            if (!slot.IsUnlocked)
            {
                message = "该灵脉地块尚未勘探解锁，请先开辟灵脉（右键或点击开辟按钮）。";
                return false;
            }
            if (slot.Building != null)
            {
                message = "该地块已有建筑，双击可直接升阶，如需改建请先拆除。";
                return false;
            }

            var cfg = SIGConfigTable.GetBuilding(buildingConfigId);
            if (cfg == null)
            {
                message = "未找到对应的建筑配表。";
                return false;
            }

            if (save.Sect.SectLevel < cfg.MinSectLevel)
            {
                message = string.Format("建造【{0}】需掌门大殿达到 Lv.{1}。", cfg.NameCn, cfg.MinSectLevel);
                return false;
            }

            if (cfg.MaxCount > 0 && GetCountOfBuilding(save.Sect, cfg.Id) >= cfg.MaxCount)
            {
                message = string.Format("【{0}】在宗门内已达建造上限 ({1}/{1})。", cfg.NameCn, cfg.MaxCount);
                return false;
            }

            double stone, wood, ore, buildSec;
            GetBuildOrUpgradeCost(cfg, 1, out stone, out wood, out ore, out buildSec);

            if (save.Sect.GetResource(ResourceType.SpiritStone) < stone ||
                save.Sect.GetResource(ResourceType.SpiritWood) < wood ||
                save.Sect.GetResource(ResourceType.SpiritOre) < ore)
            {
                message = string.Format("营造【{0}】资材不足：{1}", cfg.NameCn, FormatShortfallDetail(save, stone, wood, ore));
                return false;
            }

            save.Sect.AddResource(ResourceType.SpiritStone, -stone);
            save.Sect.AddResource(ResourceType.SpiritWood, -wood);
            save.Sect.AddResource(ResourceType.SpiritOre, -ore);

            string defaultRecipe = "";
            if (cfg.Id == "BLD_ALCHEMY") defaultRecipe = "RCP_HEAL_1";
            else if (cfg.Id == "BLD_FORGE") defaultRecipe = "RCP_SWORD_1";

            slot.Building = new BuildingInstanceData
            {
                InstanceId = "BLD_" + Guid.NewGuid().ToString("N").Substring(0, 8),
                ConfigId = cfg.Id,
                Level = 1,
                IsUnderConstruction = true,
                BuildProgressSec = 0.0,
                BuildTotalSec = buildSec,
                ActiveRecipeId = defaultRecipe,
                AutoRepeatRecipe = true
            };

            string wuxingTag = GetWuXingRelationTag(slot.VeinElement, cfg.GetElement());
            message = string.Format("已在地块 #{0}({1}·{2}脉) 动工营造【{3}】({4})，工期 {5:F1}s。",
                slotIndex + 1, slot.ZoneName, EnumTextMap.ElemCn(slot.VeinElement), cfg.NameCn, wuxingTag, buildSec);
            if (bus != null)
            {
                bus.PublishLog(save.GetImmortalDateText(), LogCategory.Sect, message);
                bus.NotifyStateChanged();
            }
            return true;
        }

        public static bool StartUpgrade(SIGSaveData save, int slotIndex, SIGEventBus bus, out string message)
        {
            if (save == null || slotIndex < 0 || slotIndex >= save.Sect.GridSlots.Count)
            {
                message = "无效地块。";
                return false;
            }

            var slot = save.Sect.GridSlots[slotIndex];
            if (slot.Building == null)
            {
                message = "该地块尚无建筑可升级。";
                return false;
            }
            if (slot.Building.IsUnderConstruction)
            {
                message = "该建筑正在施工营造中，请稍候竣工。";
                return false;
            }

            var cfg = SIGConfigTable.GetBuilding(slot.Building.ConfigId);
            if (cfg == null)
            {
                message = "建筑配置缺失。";
                return false;
            }
            if (slot.Building.Level >= cfg.MaxLevel)
            {
                message = string.Format("【{0}】已达满级 Lv.{1}。", cfg.NameCn, cfg.MaxLevel);
                return false;
            }
            if (cfg.Id != "BLD_HALL" && slot.Building.Level >= save.Sect.SectLevel + 1)
            {
                message = "附属建筑等级不可超过【掌门大殿】等级+1，请先升阶掌门大殿。";
                return false;
            }

            int nextLv = slot.Building.Level + 1;
            double stone, wood, ore, buildSec;
            GetBuildOrUpgradeCost(cfg, nextLv, out stone, out wood, out ore, out buildSec);

            if (save.Sect.GetResource(ResourceType.SpiritStone) < stone ||
                save.Sect.GetResource(ResourceType.SpiritWood) < wood ||
                save.Sect.GetResource(ResourceType.SpiritOre) < ore)
            {
                message = string.Format("升阶【{0} Lv.{1}】资材不足：{2}", cfg.NameCn, nextLv, FormatShortfallDetail(save, stone, wood, ore));
                return false;
            }

            save.Sect.AddResource(ResourceType.SpiritStone, -stone);
            save.Sect.AddResource(ResourceType.SpiritWood, -wood);
            save.Sect.AddResource(ResourceType.SpiritOre, -ore);

            slot.Building.Level = nextLv;
            slot.Building.IsUnderConstruction = true;
            slot.Building.BuildProgressSec = 0.0;
            slot.Building.BuildTotalSec = buildSec;

            message = string.Format("开始将【{0}】扩建升阶至 Lv.{1}（工期 {2:F1}s）。", cfg.NameCn, nextLv, buildSec);
            if (bus != null)
            {
                bus.PublishLog(save.GetImmortalDateText(), LogCategory.Sect, message);
                bus.NotifyStateChanged();
            }
            return true;
        }

        public static bool DemolishBuilding(SIGSaveData save, int slotIndex, SIGEventBus bus, out string message)
        {
            if (save == null || slotIndex < 0 || slotIndex >= save.Sect.GridSlots.Count)
            {
                message = "无效地块。";
                return false;
            }

            var slot = save.Sect.GridSlots[slotIndex];
            if (slot.Building == null)
            {
                message = "该地块无建筑可拆除。";
                return false;
            }
            if (slot.Building.ConfigId == "BLD_HALL")
            {
                message = "【掌门大殿】乃宗门道统根本，不可拆除！";
                return false;
            }

            var cfg = SIGConfigTable.GetBuilding(slot.Building.ConfigId);
            string name = cfg != null ? cfg.NameCn : slot.Building.ConfigId;
            double refundStone = cfg != null ? Math.Round(cfg.CostSpiritStone * 0.6, 0) : 20;
            double refundWood = cfg != null ? Math.Round(cfg.CostWood * 0.6, 0) : 15;

            save.Sect.AddResource(ResourceType.SpiritStone, refundStone);
            save.Sect.AddResource(ResourceType.SpiritWood, refundWood);
            slot.Building = null;
            RefreshSectDerivedCaps(save);

            message = string.Format("已拆除地块 #{0} 的【{1}】，回收灵石 +{2}、灵木 +{3}。",
                slotIndex + 1, name, refundStone, refundWood);
            if (bus != null)
            {
                bus.PublishLog(save.GetImmortalDateText(), LogCategory.Sect, message);
                bus.NotifyStateChanged();
            }
            return true;
        }

        public static bool UnlockGridSlot(SIGSaveData save, int slotIndex, SIGEventBus bus, out string message)
        {
            if (save == null || slotIndex < 0 || slotIndex >= save.Sect.GridSlots.Count)
            {
                message = "无效地块。";
                return false;
            }

            var slot = save.Sect.GridSlots[slotIndex];
            if (slot.IsUnlocked)
            {
                message = "该灵脉地块已处于开辟状态。";
                return false;
            }

            int unlockedCount = save.Sect.GridSlots.Count(s => s.IsUnlocked);
            double cost = Math.Round(SIGConfigTable.Current.Balance.UnlockSlotBaseStoneCost *
                                     Math.Pow(1.25, Math.Max(0, unlockedCount - 14)), 0);

            if (save.Sect.GetResource(ResourceType.SpiritStone) < cost)
            {
                message = string.Format("开辟灵脉地块 #{0} 资材不足：{1}", slotIndex + 1, FormatShortfallDetail(save, cost));
                return false;
            }

            save.Sect.AddResource(ResourceType.SpiritStone, -cost);
            slot.IsUnlocked = true;

            message = string.Format("消耗 {0} 灵石劈山引脉，成功开辟地块 #{1}（{2}·{3}脉）！",
                cost, slotIndex + 1, slot.ZoneName, EnumTextMap.ElemCn(slot.VeinElement));
            if (bus != null)
            {
                bus.PublishLog(save.GetImmortalDateText(), LogCategory.Sect, message);
                bus.NotifyStateChanged();
            }
            return true;
        }

        public static double GetNextRecruitStoneCost(SIGSaveData save)
        {
            int curCount = save != null && save.Disciples != null ? save.Disciples.Count : 0;
            return Math.Round(SIGConfigTable.Current.Balance.RecruitBaseStoneCost * Math.Pow(1.45, curCount), 0);
        }

        public static bool RecruitDisciple(SIGSaveData save, Random rng, SIGEventBus bus, out string message)
        {
            int maxCap = GetMaxDiscipleCap(save.Sect);
            if (save.Disciples.Count >= maxCap)
            {
                message = string.Format("门人容量已满 ({0}/{1})，请先建造或升阶【弟子洞府】/【掌门大殿】。", save.Disciples.Count, maxCap);
                return false;
            }

            double stoneCost = GetNextRecruitStoneCost(save);
            if (save.Sect.GetResource(ResourceType.SpiritStone) < stoneCost)
            {
                message = string.Format("招募新弟子资材不足：{0}", FormatShortfallDetail(save, stoneCost));
                return false;
            }

            save.Sect.AddResource(ResourceType.SpiritStone, -stoneCost);

            string fullName = SurnamePool[rng.Next(SurnamePool.Length)] + GivenNamePool[rng.Next(GivenNamePool.Length)];
            ElementType elem = (ElementType)rng.Next(1, 6);
            int qualityRoll = rng.Next(100);
            int rootQuality = qualityRoll < 10 ? 5 : (qualityRoll < 32 ? 4 : (qualityRoll < 70 ? 3 : 2));

            var disc = new CharacterData
            {
                Id = "DISC_" + Guid.NewGuid().ToString("N").Substring(0, 6),
                Name = fullName,
                IsSectLeader = false,
                Realm = RealmTier.QiCondensation,
                SubStage = 1,
                AgeYears = rng.Next(15, 23),
                AssignedJob = DiscipleJob.Cultivate,
                EquippedTechniqueId = "TECH_BASIC",
                GatherSkillLv = rng.Next(1, 3),
                AlchemySkillLv = rng.Next(1, 3),
                ForgeSkillLv = rng.Next(1, 3),
                Innate = new InnateStatsData
                {
                    Constitution = rng.Next(7, 15),
                    Strength = rng.Next(7, 15),
                    Comprehension = rng.Next(7, 15),
                    Agility = rng.Next(7, 15),
                    Fortune = rng.Next(7, 15),
                    RootElement = elem,
                    RootQuality = rootQuality
                }
            };

            CultivationLogic.RecalculateStats(disc, save.Sect);
            disc.BattleStats.CurHp = disc.BattleStats.MaxHp;
            disc.BattleStats.CurMp = disc.BattleStats.MaxMp;
            save.Disciples.Add(disc);

            message = string.Format("仙缘降临！新弟子【{0}】（{1}·{2}）拜入山门！",
                disc.Name, EnumTextMap.ElemCn(elem), EnumTextMap.RootQualityCn(rootQuality));
            if (bus != null)
            {
                bus.PublishLog(save.GetImmortalDateText(), LogCategory.Sect, message);
                bus.NotifyStateChanged();
            }
            return true;
        }

        public static bool AssignDiscipleJob(SIGSaveData save, string discipleId, DiscipleJob newJob, SIGEventBus bus, out string message)
        {
            var disc = save.Disciples.FirstOrDefault(d => d.Id == discipleId);
            if (disc == null)
            {
                message = "未找到指定门人弟子。";
                return false;
            }
            if (disc.AssignedJob == newJob)
            {
                message = string.Format("门人【{0}】当前已在执行 {1}。", disc.Name, EnumTextMap.JobCn(newJob));
                return true;
            }

            disc.AssignedJob = newJob;
            message = string.Format("已指派门人【{0}】前往执行：{1}。", disc.Name, EnumTextMap.JobCn(newJob));
            if (bus != null)
            {
                bus.PublishLog(save.GetImmortalDateText(), LogCategory.Sect, message);
                bus.NotifyStateChanged();
            }
            return true;
        }

        public static void TickSectEconomy(SIGSaveData save, double simDeltaSec, SIGEventBus bus)
        {
            if (save == null || simDeltaSec <= 0) return;

            bool anyCompleted = false;
            foreach (var slot in save.Sect.GridSlots)
            {
                if (slot.Building != null && slot.Building.IsUnderConstruction)
                {
                    slot.Building.BuildProgressSec += simDeltaSec;
                    if (slot.Building.BuildProgressSec >= slot.Building.BuildTotalSec)
                    {
                        slot.Building.IsUnderConstruction = false;
                        slot.Building.BuildProgressSec = slot.Building.BuildTotalSec;
                        anyCompleted = true;

                        var cfg = SIGConfigTable.GetBuilding(slot.Building.ConfigId);
                        string name = cfg != null ? cfg.NameCn : slot.Building.ConfigId;
                        if (bus != null)
                        {
                            bus.PublishLog(save.GetImmortalDateText(), LogCategory.Sect,
                                string.Format("【宗门营造竣工】地块 #{0} 的【{1} Lv.{2}】已落成投产！",
                                    slot.SlotIndex + 1, name, slot.Building.Level));
                        }
                    }
                }
            }

            if (anyCompleted)
            {
                RefreshSectDerivedCaps(save);
                CultivationLogic.RecalculateAllCharacters(save);
            }

            var rates = CalculateResourceRatesPerSec(save);
            foreach (var kv in rates)
            {
                if (kv.Value > 0.0)
                {
                    save.Sect.AddResource(kv.Key, kv.Value * simDeltaSec);
                }
            }

            if (save.Sect.BarrierCurHp < save.Sect.BarrierMaxHp)
            {
                double regen = save.Sect.BarrierMaxHp * 0.015 * simDeltaSec;
                save.Sect.BarrierCurHp = Math.Min(save.Sect.BarrierMaxHp, save.Sect.BarrierCurHp + regen);
            }
        }

        public static void SettleMonthlySectUpkeep(SIGSaveData save, SIGEventBus bus)
        {
            if (save == null) return;

            double ageDelta = 1.0 / 12.0;
            save.Leader.AgeYears += ageDelta;
            foreach (var d in save.Disciples)
            {
                d.AgeYears += ageDelta;
            }

            int mortalEaters = save.Disciples.Count(d => d.Realm < RealmTier.Foundation);
            double foodNeed = mortalEaters * SIGConfigTable.Current.Balance.DiscipleMonthlyFoodCost;
            if (foodNeed > 0)
            {
                double curFood = save.Sect.GetResource(ResourceType.SpiritFood);
                if (curFood >= foodNeed)
                {
                    save.Sect.AddResource(ResourceType.SpiritFood, -foodNeed);
                }
                else
                {
                    save.Sect.SetResource(ResourceType.SpiritFood, 0);
                    if (bus != null)
                    {
                        bus.PublishLog(save.GetImmortalDateText(), LogCategory.Sect,
                            "【宗门预警】灵谷库存告急！请扩建【神农灵田】或增派农耕弟子。");
                    }
                }
            }
        }
    }
}
