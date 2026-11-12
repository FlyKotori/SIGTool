using System;
using System.Linq;
using SIGTool.Configs;
using SIGTool.Datas;
using SIGTool.Modules.Core;

namespace SIGTool.Modules.Logics
{
    /// <summary>
    /// 丹房与器坊配方自动挂机炼制、配方熟练度成长与手动单炉合成逻辑（统一采用 SIG 命名）
    /// </summary>
    public static class CraftAndItemLogic
    {
        public static bool CanAffordRecipe(SectManageData sect, RecipeConfig rcp)
        {
            if (sect == null || rcp == null) return false;
            return sect.GetResource(ResourceType.SpiritStone) >= rcp.CostSpiritStone &&
                   sect.GetResource(ResourceType.SpiritHerb) >= rcp.CostHerb &&
                   sect.GetResource(ResourceType.SpiritOre) >= rcp.CostOre &&
                   sect.GetResource(ResourceType.SpiritWood) >= rcp.CostWood &&
                   sect.GetResource(ResourceType.MonsterCore) >= rcp.CostMonsterCore;
        }

        public static void DeductRecipeCost(SectManageData sect, RecipeConfig rcp)
        {
            sect.AddResource(ResourceType.SpiritStone, -rcp.CostSpiritStone);
            sect.AddResource(ResourceType.SpiritHerb, -rcp.CostHerb);
            sect.AddResource(ResourceType.SpiritOre, -rcp.CostOre);
            sect.AddResource(ResourceType.SpiritWood, -rcp.CostWood);
            sect.AddResource(ResourceType.MonsterCore, -rcp.CostMonsterCore);
        }

        public static string FormatRecipeCostWithStock(SectManageData sect, RecipeConfig rcp)
        {
            if (rcp == null) return "";
            string s = "";
            if (rcp.CostSpiritStone > 0) s += string.Format("灵石:{0}/{1:F0}  ", rcp.CostSpiritStone, sect != null ? sect.GetResource(ResourceType.SpiritStone) : 0);
            if (rcp.CostHerb > 0) s += string.Format("灵草:{0}/{1:F0}  ", rcp.CostHerb, sect != null ? sect.GetResource(ResourceType.SpiritHerb) : 0);
            if (rcp.CostOre > 0) s += string.Format("玄铁:{0}/{1:F0}  ", rcp.CostOre, sect != null ? sect.GetResource(ResourceType.SpiritOre) : 0);
            if (rcp.CostWood > 0) s += string.Format("灵木:{0}/{1:F0}  ", rcp.CostWood, sect != null ? sect.GetResource(ResourceType.SpiritWood) : 0);
            if (rcp.CostMonsterCore > 0) s += string.Format("妖丹:{0}/{1:F0}  ", rcp.CostMonsterCore, sect != null ? sect.GetResource(ResourceType.MonsterCore) : 0);
            return s.Trim();
        }

        public static double GetEffectiveSpeedMult(SIGSaveData save, SectGridSlotData slot, BuildingInstanceData bld)
        {
            var bCfg = SIGConfigTable.GetBuilding(bld.ConfigId);
            double slotMult = SectBuildLogic.GetSlotEfficiencyMultiplier(slot, bCfg);
            double lvMult = 1.0 + (bld.Level - 1) * 0.25;

            int helpers = 0;
            if (bld.ConfigId == "BLD_ALCHEMY")
                helpers = save.Disciples.Count(d => d.AssignedJob == DiscipleJob.Alchemy);
            else if (bld.ConfigId == "BLD_FORGE")
                helpers = save.Disciples.Count(d => d.AssignedJob == DiscipleJob.Forge);

            return slotMult * lvMult * (1.0 + helpers * 0.30);
        }

        public static double GetEffectiveSuccessRate(SIGSaveData save, BuildingInstanceData bld, RecipeConfig rcp)
        {
            if (rcp == null) return 0.0;
            int bldLv = bld != null ? bld.Level : 1;
            int masteryLv = save != null && save.Sect != null ? save.Sect.GetRecipeMasteryLv(rcp.Id) : 1;
            int helperSkillSum = 0;
            if (save != null && save.Disciples != null)
            {
                if (rcp.RequiredBuildingId == "BLD_ALCHEMY")
                    helperSkillSum = save.Disciples.Where(d => d.AssignedJob == DiscipleJob.Alchemy).Sum(d => d.AlchemySkillLv);
                else if (rcp.RequiredBuildingId == "BLD_FORGE")
                    helperSkillSum = save.Disciples.Where(d => d.AssignedJob == DiscipleJob.Forge).Sum(d => d.ForgeSkillLv);
            }

            double rate = rcp.BaseSuccessRate + (bldLv - rcp.MinBuildingLevel) * 0.04 + (masteryLv - 1) * 0.03 + helperSkillSum * 0.025;
            return Math.Min(0.99, Math.Max(0.25, rate));
        }

        private static void GainRecipeMasteryOnSuccess(SIGSaveData save, RecipeConfig rcp)
        {
            if (save == null || save.Sect == null || rcp == null) return;
            if (save.Sect.RecipeMastery == null) save.Sect.RecipeMastery = new System.Collections.Generic.Dictionary<string, int>();
            int curLv = save.Sect.GetRecipeMasteryLv(rcp.Id);
            if (curLv < rcp.MaxMasteryLv)
            {
                save.Sect.RecipeMastery[rcp.Id] = curLv + 1;
            }
        }

        public static bool SetWorkshopRecipe(SIGSaveData save, int slotIndex, string recipeId, bool autoRepeat, SIGEventBus bus, out string message)
        {
            if (save == null || slotIndex < 0 || slotIndex >= save.Sect.GridSlots.Count)
            {
                message = "无效的工坊地块。";
                return false;
            }

            var slot = save.Sect.GridSlots[slotIndex];
            if (slot.Building == null || slot.Building.IsUnderConstruction)
            {
                message = "该地块尚无已落成的丹房或器坊。";
                return false;
            }

            if (string.IsNullOrEmpty(recipeId))
            {
                slot.Building.ActiveRecipeId = "";
                slot.Building.CraftProgressSec = 0;
                message = "工坊已暂停自动挂机炼制。";
                if (bus != null) bus.NotifyStateChanged();
                return true;
            }

            var rcp = SIGConfigTable.GetRecipe(recipeId);
            if (rcp == null || rcp.RequiredBuildingId != slot.Building.ConfigId)
            {
                message = "该配方与当前工坊类型不匹配。";
                return false;
            }

            if (slot.Building.Level < rcp.MinBuildingLevel)
            {
                var bCfg = SIGConfigTable.GetBuilding(slot.Building.ConfigId);
                message = string.Format("【{0}】至少需要 {1} Lv.{2} 方可开炉。",
                    rcp.NameCn, bCfg != null ? bCfg.NameCn : "工坊", rcp.MinBuildingLevel);
                return false;
            }

            slot.Building.ActiveRecipeId = rcp.Id;
            slot.Building.AutoRepeatRecipe = autoRepeat;
            slot.Building.CraftProgressSec = 0.0;

            message = string.Format("工坊（地块 #{0}）已切换配方：{1}（{2}）。",
                slotIndex + 1, rcp.NameCn, autoRepeat ? "循环开炉" : "单炉");
            if (bus != null)
            {
                bus.PublishLog(save.GetImmortalDateText(), LogCategory.Craft, message);
                bus.NotifyStateChanged();
            }
            return true;
        }

        public static void TickWorkshops(SIGSaveData save, double simDeltaSec, Random rng, SIGEventBus bus)
        {
            if (save == null || simDeltaSec <= 0) return;

            foreach (var slot in save.Sect.GridSlots)
            {
                var bld = slot.Building;
                if (bld == null || bld.IsUnderConstruction || string.IsNullOrEmpty(bld.ActiveRecipeId)) continue;

                var rcp = SIGConfigTable.GetRecipe(bld.ActiveRecipeId);
                if (rcp == null || bld.Level < rcp.MinBuildingLevel) continue;

                if (bld.CraftProgressSec <= 0.0001)
                {
                    if (!CanAffordRecipe(save.Sect, rcp)) continue;
                    DeductRecipeCost(save.Sect, rcp);
                    bld.CraftProgressSec = 0.01;
                }

                double speedMult = GetEffectiveSpeedMult(save, slot, bld);
                bld.CraftProgressSec += simDeltaSec * speedMult;

                if (bld.CraftProgressSec >= rcp.CraftTimeSec)
                {
                    bld.CraftProgressSec = 0.0;
                    double succRate = GetEffectiveSuccessRate(save, bld, rcp);
                    var outItem = SIGConfigTable.GetItem(rcp.OutputItemId);
                    string itemName = outItem != null ? outItem.NameCn : rcp.OutputItemId;

                    if (rng.NextDouble() <= succRate)
                    {
                        save.Sect.AddItem(rcp.OutputItemId, rcp.OutputCount);
                        if (rng.Next(100) < 35) GainRecipeMasteryOnSuccess(save, rcp);
                        if (bus != null)
                        {
                            bus.PublishLog(save.GetImmortalDateText(), LogCategory.Craft,
                                string.Format("丹器出炉！炼成【{0}】x{1}（库存：{2}）。",
                                    itemName, rcp.OutputCount, save.Sect.GetItemCount(rcp.OutputItemId)));
                        }
                    }
                    else
                    {
                        if (bus != null)
                        {
                            bus.PublishLog(save.GetImmortalDateText(), LogCategory.Craft,
                                string.Format("火候微差，炼制【{0}】未能成器（成率 {1:P0}）。", itemName, succRate));
                        }
                    }

                    if (!bld.AutoRepeatRecipe) bld.ActiveRecipeId = "";
                }
            }
        }

        public static bool CraftOnceManual(SIGSaveData save, string recipeId, Random rng, SIGEventBus bus, out string message)
        {
            var rcp = SIGConfigTable.GetRecipe(recipeId);
            if (save == null || rcp == null) { message = "无效配方。"; return false; }

            var matchingSlot = save.Sect.GridSlots.FirstOrDefault(s =>
                s.Building != null && !s.Building.IsUnderConstruction &&
                s.Building.ConfigId == rcp.RequiredBuildingId &&
                s.Building.Level >= rcp.MinBuildingLevel);

            if (matchingSlot == null)
            {
                var reqBld = SIGConfigTable.GetBuilding(rcp.RequiredBuildingId);
                message = string.Format("宗门内尚未建成【{0} Lv.{1}】，无法开炉。",
                    reqBld != null ? reqBld.NameCn : rcp.RequiredBuildingId, rcp.MinBuildingLevel);
                return false;
            }

            if (!CanAffordRecipe(save.Sect, rcp))
            {
                message = string.Format("耗材不足！所需：{0}", FormatRecipeCostWithStock(save.Sect, rcp));
                return false;
            }

            DeductRecipeCost(save.Sect, rcp);
            double succRate = GetEffectiveSuccessRate(save, matchingSlot.Building, rcp);
            var outItem = SIGConfigTable.GetItem(rcp.OutputItemId);
            string itemName = outItem != null ? outItem.NameCn : rcp.OutputItemId;

            if (rng.NextDouble() <= succRate)
            {
                save.Sect.AddItem(rcp.OutputItemId, rcp.OutputCount);
                GainRecipeMasteryOnSuccess(save, rcp);
                message = string.Format("掌门真火催动，一炉功成！获得【{0}】x{1}（熟练 Lv.{2}/{3}）。",
                    itemName, rcp.OutputCount, save.Sect.GetRecipeMasteryLv(rcp.Id), rcp.MaxMasteryLv);
                if (bus != null) { bus.PublishLog(save.GetImmortalDateText(), LogCategory.Craft, message); bus.NotifyStateChanged(); }
                return true;
            }
            else
            {
                message = string.Format("炉火震荡，手动祭炼【{0}】遗憾失败（成率 {1:P0}）。", itemName, succRate);
                if (bus != null) { bus.PublishLog(save.GetImmortalDateText(), LogCategory.Craft, message); bus.NotifyStateChanged(); }
                return false;
            }
        }
    }
}
