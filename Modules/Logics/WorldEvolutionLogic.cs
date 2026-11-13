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
    /// SYS-03 动态修仙界演化、九州坊市物价浮动与非阻塞奇遇事件链核心逻辑
    /// </summary>
    public static class WorldEvolutionLogic
    {
        public const int MaxPendingEncounters = 3;
        public const int MaxChronicleLines = 24;

        public static string TrendNameCn(WorldTrendType trend)
        {
            switch (trend)
            {
                case WorldTrendType.HerbHarvest: return "百草丰年";
                case WorldTrendType.PillMaterialShortage: return "丹材荒岁";
                case WorldTrendType.EarthVeinTide: return "地脉潮汐";
                case WorldTrendType.MonsterSurge: return "群妖躁动";
                case WorldTrendType.RelicEmergence: return "古迹现世";
                default: return "风调雨顺";
            }
        }

        public static string TrendEffectDesc(WorldTrendType trend)
        {
            switch (trend)
            {
                case WorldTrendType.HerbHarvest:
                    return "灵草/灵谷产出 +22% | 坊市草药收购与采购跌至 ~0.78x";
                case WorldTrendType.PillMaterialShortage:
                    return "灵草产出 -12% | 坊市灵草与成丹暴涨至 ~1.45x（抛售灵丹暴利）";
                case WorldTrendType.EarthVeinTide:
                    return "玄铁/灵木产出 +22% | 全宗吐纳灵气额外 +15%";
                case WorldTrendType.MonsterSurge:
                    return "秘境妖丹掉率 +30% | 坊市妖丹与回春丹涨至 ~1.35x";
                case WorldTrendType.RelicEmergence:
                    return "奇遇机缘触发率 ×2.0 | 坊市破境宝丹涨至 ~1.30x";
                default:
                    return "九州灵机中正平和，各大宗门产销循常理运转";
            }
        }

        public static FactionRelationTier GetRelationTier(int relation)
        {
            if (relation >= 75) return FactionRelationTier.Allied;
            if (relation >= 35) return FactionRelationTier.Friendly;
            if (relation <= -40) return FactionRelationTier.Hostile;
            if (relation <= -10) return FactionRelationTier.Cold;
            return FactionRelationTier.Neutral;
        }

        public static string RelationTierCn(int relation)
        {
            switch (GetRelationTier(relation))
            {
                case FactionRelationTier.Allied: return "★盟友";
                case FactionRelationTier.Friendly: return "●交好";
                case FactionRelationTier.Cold: return "○冷淡";
                case FactionRelationTier.Hostile: return "▲仇视";
                default: return "─中立";
            }
        }

        public static void EnsureWorldInitialized(SIGSaveData save)
        {
            if (save == null) return;
            if (save.World == null) save.World = new WorldEvolutionStateData();
            var w = save.World;

            if (w.Factions == null) w.Factions = new List<WorldFactionRuntimeData>();
            if (w.MarketSlots == null) w.MarketSlots = new List<MarketSlotRuntimeData>();
            if (w.PendingEncounters == null) w.PendingEncounters = new List<PendingEncounterData>();
            if (w.ScheduledFollowups == null) w.ScheduledFollowups = new List<ScheduledChainFollowupData>();
            if (w.TempBuffs == null) w.TempBuffs = new List<WorldTempBuffData>();
            if (w.TriggeredRealmMilestones == null) w.TriggeredRealmMilestones = new List<string>();
            if (w.WorldChronicles == null) w.WorldChronicles = new List<string>();

            var facCfgs = (SIGConfigTable.Current != null && SIGConfigTable.Current.WorldFactions != null)
                ? SIGConfigTable.Current.WorldFactions
                : new List<WorldFactionConfig>();

            for (int i = 0; i < facCfgs.Count; i++)
            {
                var fc = facCfgs[i];
                if (!w.Factions.Any(x => x.FactionId == fc.Id))
                {
                    w.Factions.Add(new WorldFactionRuntimeData
                    {
                        FactionId = fc.Id,
                        Prosperity = fc.InitProsperity,
                        Relation = fc.InitRelation,
                        DiplomacyCooldownMonths = 0,
                        RecentTrendNote = "山门气象平稳，与周边互通有无"
                    });
                }
            }

            var mktCfgs = (SIGConfigTable.Current != null && SIGConfigTable.Current.MarketListings != null)
                ? SIGConfigTable.Current.MarketListings
                : new List<MarketListingConfig>();

            for (int i = 0; i < mktCfgs.Count; i++)
            {
                var mc = mktCfgs[i];
                if (!w.MarketSlots.Any(x => x.ListingId == mc.Id))
                {
                    w.MarketSlots.Add(new MarketSlotRuntimeData
                    {
                        ListingId = mc.Id,
                        PriceMultiplier = 1.0,
                        RemainingBuyStock = mc.QuarterMaxStock
                    });
                }
            }

            if (w.MonthsUntilTrendShift <= 0) w.MonthsUntilTrendShift = 12;
            if (w.MonthsUntilMarketRefresh <= 0) w.MonthsUntilMarketRefresh = 3;

            SyncSectWorldModifiers(save);
        }

        public static WorldFactionRuntimeData FindFaction(SIGSaveData save, string factionId)
        {
            if (save == null || save.World == null || save.World.Factions == null || string.IsNullOrEmpty(factionId))
                return null;
            for (int i = 0; i < save.World.Factions.Count; i++)
            {
                if (save.World.Factions[i].FactionId == factionId) return save.World.Factions[i];
            }
            return null;
        }

        public static void AppendChronicle(SIGSaveData save, string text)
        {
            if (save == null || save.World == null || string.IsNullOrEmpty(text)) return;
            if (save.World.WorldChronicles == null) save.World.WorldChronicles = new List<string>();
            string line = string.Format("[{0}] {1}", save.GetImmortalDateText(), text);
            save.World.WorldChronicles.Insert(0, line);
            if (save.World.WorldChronicles.Count > MaxChronicleLines)
            {
                save.World.WorldChronicles.RemoveRange(MaxChronicleLines, save.World.WorldChronicles.Count - MaxChronicleLines);
            }
        }

        /// <summary>
        /// 汇总当前岁运天象 + 盟友加成 + 阶段性奇遇气运，同步写入 SectManageData 缓存并重算角色面板
        /// </summary>
        public static void SyncSectWorldModifiers(SIGSaveData save)
        {
            if (save == null || save.Sect == null) return;
            double auraAdd = 0.0;
            double atkAdd = 0.0;

            if (save.World != null)
            {
                if (save.World.CurrentTrend == WorldTrendType.EarthVeinTide)
                {
                    auraAdd += 0.15;
                }

                var qingyun = FindFaction(save, "FAC_QINGYUN");
                if (qingyun != null)
                {
                    var tier = GetRelationTier(qingyun.Relation);
                    if (tier == FactionRelationTier.Allied) atkAdd += 0.12;
                    else if (tier == FactionRelationTier.Friendly) atkAdd += 0.06;
                }

                var beiming = FindFaction(save, "FAC_BEIMING");
                if (beiming != null)
                {
                    var tier = GetRelationTier(beiming.Relation);
                    if (tier == FactionRelationTier.Allied) auraAdd += 0.20;
                    else if (tier == FactionRelationTier.Friendly) auraAdd += 0.10;
                }

                if (save.World.TempBuffs != null)
                {
                    for (int i = 0; i < save.World.TempBuffs.Count; i++)
                    {
                        auraAdd += save.World.TempBuffs[i].AuraRateAdd;
                        atkAdd += save.World.TempBuffs[i].AtkPctAdd;
                    }
                }
            }

            bool changed = Math.Abs(save.Sect.WorldAuraBonusCache - auraAdd) > 0.0001 ||
                           Math.Abs(save.Sect.WorldAtkPctBonusCache - atkAdd) > 0.0001;
            save.Sect.WorldAuraBonusCache = auraAdd;
            save.Sect.WorldAtkPctBonusCache = atkAdd;

            if (changed)
            {
                CultivationLogic.RecalculateAllCharacters(save);
            }
        }

        public static double GetWorldResourceMultiplier(SIGSaveData save, ResourceType rt)
        {
            if (save == null || save.World == null) return 1.0;
            double mult = 1.0;
            var trend = save.World.CurrentTrend;

            if (rt == ResourceType.SpiritHerb)
            {
                if (trend == WorldTrendType.HerbHarvest) mult += 0.22;
                else if (trend == WorldTrendType.PillMaterialShortage) mult -= 0.12;

                var baicao = FindFaction(save, "FAC_BAICAO");
                if (baicao != null)
                {
                    var tier = GetRelationTier(baicao.Relation);
                    if (tier == FactionRelationTier.Allied) mult += 0.22;
                    else if (tier == FactionRelationTier.Friendly) mult += 0.12;
                }

                if (save.World.TempBuffs != null)
                {
                    for (int i = 0; i < save.World.TempBuffs.Count; i++)
                    {
                        mult += save.World.TempBuffs[i].HerbOutputMultAdd;
                    }
                }
            }
            else if (rt == ResourceType.SpiritFood)
            {
                if (trend == WorldTrendType.HerbHarvest) mult += 0.22;
            }
            else if (rt == ResourceType.SpiritWood || rt == ResourceType.SpiritOre)
            {
                if (trend == WorldTrendType.EarthVeinTide) mult += 0.22;
            }

            return Math.Max(0.5, mult);
        }

        public static double GetAlchemySuccessRateBonus(SIGSaveData save)
        {
            var baicao = FindFaction(save, "FAC_BAICAO");
            if (baicao == null) return 0.0;
            var tier = GetRelationTier(baicao.Relation);
            if (tier == FactionRelationTier.Allied) return 0.08;
            if (tier == FactionRelationTier.Friendly) return 0.04;
            return 0.0;
        }

        public static double GetMonsterCoreDropMultiplier(SIGSaveData save)
        {
            if (save == null || save.World == null) return 1.0;
            double mult = 1.0;
            if (save.World.CurrentTrend == WorldTrendType.MonsterSurge) mult += 0.30;
            var xueying = FindFaction(save, "FAC_XUEYING");
            if (xueying != null && xueying.Relation >= 0) mult += 0.12;
            return mult;
        }

        public static double GetTianjiTradeDiscount(SIGSaveData save)
        {
            var tianji = FindFaction(save, "FAC_TIANJI");
            if (tianji == null) return 0.0;
            var tier = GetRelationTier(tianji.Relation);
            if (tier == FactionRelationTier.Allied) return 0.12;
            if (tier == FactionRelationTier.Friendly) return 0.06;
            return 0.0;
        }

        /// <summary>
        /// 每仙历月推进修仙界动态演化（岁运轮转、季度坊市调价、宗门兴衰、延迟事件链与机缘触发）
        /// </summary>
        public static void AdvanceMonthlyWorldEvolution(SIGSaveData save, Random rng, bool allowEncounterTrigger, SIGEventBus bus)
        {
            if (save == null) return;
            EnsureWorldInitialized(save);
            if (rng == null) rng = new Random();
            var w = save.World;

            // 1. 递减阶段性奇遇 Buff 与外交冷却
            bool buffExpired = false;
            for (int i = w.TempBuffs.Count - 1; i >= 0; i--)
            {
                w.TempBuffs[i].RemainingMonths--;
                if (w.TempBuffs[i].RemainingMonths <= 0)
                {
                    w.TempBuffs.RemoveAt(i);
                    buffExpired = true;
                }
            }
            for (int i = 0; i < w.Factions.Count; i++)
            {
                if (w.Factions[i].DiplomacyCooldownMonths > 0)
                    w.Factions[i].DiplomacyCooldownMonths--;
            }

            // 2. 年度天道岁运更替与周边势力兴衰演化
            w.MonthsUntilTrendShift--;
            if (w.MonthsUntilTrendShift <= 0)
            {
                w.MonthsUntilTrendShift = 12;
                EvolveYearlyTrendAndFactions(save, rng, bus);
                RefreshMarketPricesAndStock(save, rng, bus, silentLog: true);
            }
            else
            {
                // 3. 季度坊市行情微调与补货（每 3 个月）
                w.MonthsUntilMarketRefresh--;
                if (w.MonthsUntilMarketRefresh <= 0)
                {
                    w.MonthsUntilMarketRefresh = 3;
                    RefreshMarketPricesAndStock(save, rng, bus, silentLog: false);
                }
            }

            if (buffExpired)
            {
                SyncSectWorldModifiers(save);
            }

            if (!allowEncounterTrigger) return;

            // 4. 检查到期的多阶段连续奇遇事件链后续
            for (int i = w.ScheduledFollowups.Count - 1; i >= 0; i--)
            {
                w.ScheduledFollowups[i].RemainingMonths--;
                if (w.ScheduledFollowups[i].RemainingMonths <= 0)
                {
                    string nextId = w.ScheduledFollowups[i].NextEventId;
                    w.ScheduledFollowups.RemoveAt(i);
                    EnqueueEncounterById(save, nextId, bus);
                }
            }

            // 5. 按概率触发岁月随机奇遇（队列未满时）
            if (w.PendingEncounters.Count < MaxPendingEncounters)
            {
                int fortune = (save.Leader != null && save.Leader.Innate != null) ? save.Leader.Innate.Fortune : 10;
                double chance = 0.085 + fortune * 0.003;
                if (w.CurrentTrend == WorldTrendType.RelicEmergence) chance *= 1.85;

                if (rng.NextDouble() < chance)
                {
                    TriggerRandomMonthlyEncounter(save, rng, bus);
                }
            }
        }

        private static void EvolveYearlyTrendAndFactions(SIGSaveData save, Random rng, SIGEventBus bus)
        {
            var w = save.World;
            WorldTrendType[] pool =
            {
                WorldTrendType.BalancedNormal,
                WorldTrendType.HerbHarvest,
                WorldTrendType.PillMaterialShortage,
                WorldTrendType.EarthVeinTide,
                WorldTrendType.MonsterSurge,
                WorldTrendType.RelicEmergence
            };
            WorldTrendType nextTrend = pool[rng.Next(pool.Length)];
            if (nextTrend == w.CurrentTrend && rng.NextDouble() < 0.75)
            {
                nextTrend = pool[(Array.IndexOf(pool, nextTrend) + 1 + rng.Next(pool.Length - 1)) % pool.Length];
            }
            w.CurrentTrend = nextTrend;

            string[] riseNotes =
            {
                "门中长老破境出关，声威大振，广收门徒",
                "新辟一处中型灵脉矿窟，宗门库藏充盈",
                "门下真传于九州大比夺魁，四方散修来投"
            };
            string[] dipNotes =
            {
                "护山灵脉偶发枯竭，正高价求购补脉资材",
                "精锐执事探索绝地折损，近期闭山休整",
                "门内丹器损耗颇巨，对外缩减灵材供给"
            };

            for (int i = 0; i < w.Factions.Count; i++)
            {
                var f = w.Factions[i];
                int delta = rng.Next(-9, 11);
                if (f.FactionId == "FAC_BAICAO" && nextTrend == WorldTrendType.HerbHarvest) delta += 6;
                if (f.FactionId == "FAC_BAICAO" && nextTrend == WorldTrendType.PillMaterialShortage) delta -= 6;
                if (f.FactionId == "FAC_XUEYING" && nextTrend == WorldTrendType.MonsterSurge) delta += 8;

                f.Prosperity = Math.Max(20, Math.Min(100, f.Prosperity + delta));
                if (delta >= 4) f.RecentTrendNote = riseNotes[rng.Next(riseNotes.Length)];
                else if (delta <= -4) f.RecentTrendNote = dipNotes[rng.Next(dipNotes.Length)];
                else f.RecentTrendNote = "山门运转平稳，按常例与周边坊市通商";
            }

            SyncSectWorldModifiers(save);

            string msg = string.Format("【九州岁运更易】今岁天象转为【{0}】（{1}）。",
                TrendNameCn(nextTrend), TrendEffectDesc(nextTrend));
            AppendChronicle(save, msg);
            if (bus != null)
            {
                bus.PublishLog(save.GetImmortalDateText(), LogCategory.System, msg);
            }
        }

        public static void RefreshMarketPricesAndStock(SIGSaveData save, Random rng, SIGEventBus bus, bool silentLog)
        {
            if (save == null || save.World == null) return;
            if (rng == null) rng = new Random();
            var w = save.World;
            var trend = w.CurrentTrend;

            var baicao = FindFaction(save, "FAC_BAICAO");
            var qingyun = FindFaction(save, "FAC_QINGYUN");
            double baicaoFactor = baicao != null ? (60 - baicao.Prosperity) * 0.0025 : 0.0;
            double qingyunFactor = qingyun != null ? (qingyun.Prosperity - 60) * 0.002 : 0.0;

            for (int i = 0; i < w.MarketSlots.Count; i++)
            {
                var slot = w.MarketSlots[i];
                var cfg = SIGConfigTable.GetMarketListing(slot.ListingId);
                if (cfg == null) continue;

                double mult = 1.0 + (rng.NextDouble() * 0.16 - 0.08);

                if (cfg.CategoryTag == "Herb")
                {
                    if (trend == WorldTrendType.HerbHarvest) mult -= 0.22;
                    else if (trend == WorldTrendType.PillMaterialShortage) mult += 0.44;
                    mult += baicaoFactor;
                }
                else if (cfg.CategoryTag == "Pill")
                {
                    if (trend == WorldTrendType.PillMaterialShortage) mult += 0.38;
                    else if (trend == WorldTrendType.HerbHarvest) mult -= 0.14;
                    else if (trend == WorldTrendType.RelicEmergence) mult += 0.26;
                    else if (trend == WorldTrendType.MonsterSurge && cfg.Id == "MKT_ITM_HEAL1") mult += 0.28;
                    mult += baicaoFactor;
                }
                else if (cfg.CategoryTag == "Core")
                {
                    if (trend == WorldTrendType.MonsterSurge) mult += 0.34;
                    else if (trend == WorldTrendType.RelicEmergence) mult += 0.18;
                }
                else if (cfg.CategoryTag == "Material")
                {
                    if (trend == WorldTrendType.EarthVeinTide) mult -= 0.18;
                    if (cfg.ResourceKey == "SpiritOre") mult += qingyunFactor;
                }

                slot.PriceMultiplier = Math.Round(Math.Max(0.65, Math.Min(1.65, mult)), 2);
                slot.RemainingBuyStock = cfg.QuarterMaxStock;
            }

            if (!silentLog)
            {
                string note = string.Format("九州坊市当季行情已刷新并补足货源（当前岁运：{0}）。", TrendNameCn(trend));
                AppendChronicle(save, note);
                if (bus != null)
                {
                    bus.PublishLog(save.GetImmortalDateText(), LogCategory.System, note);
                }
            }
        }

        public static double GetUnitBuyStone(SIGSaveData save, MarketListingConfig cfg, MarketSlotRuntimeData slot)
        {
            if (cfg == null) return 999999;
            double mult = slot != null ? slot.PriceMultiplier : 1.0;
            double discount = GetTianjiTradeDiscount(save);
            return Math.Max(1.0, Math.Round(cfg.BaseBuyStone * mult * (1.0 - discount), 0));
        }

        public static double GetUnitSellStone(SIGSaveData save, MarketListingConfig cfg, MarketSlotRuntimeData slot)
        {
            if (cfg == null) return 1.0;
            double mult = slot != null ? slot.PriceMultiplier : 1.0;
            double bonus = GetTianjiTradeDiscount(save);
            double buyPrice = GetUnitBuyStone(save, cfg, slot);
            double rawSell = Math.Round(cfg.BaseSellStone * mult * (1.0 + bonus), 0);
            // 保证卖价绝不高于同季买价（防止无限无损套利）
            return Math.Max(1.0, Math.Min(buyPrice - 1.0, rawSell));
        }

        public static int GetOwnedTradableBatches(SIGSaveData save, MarketListingConfig cfg)
        {
            if (save == null || save.Sect == null || cfg == null) return 0;
            if (!string.IsNullOrEmpty(cfg.ResourceKey))
            {
                ResourceType rt;
                if (!Enum.TryParse(cfg.ResourceKey, true, out rt) || cfg.ResourceBatchQty <= 0) return 0;
                return (int)Math.Floor(save.Sect.GetResource(rt) / cfg.ResourceBatchQty);
            }
            if (!string.IsNullOrEmpty(cfg.ItemId) && cfg.ItemBatchQty > 0)
            {
                return save.Sect.GetItemCount(cfg.ItemId) / cfg.ItemBatchQty;
            }
            return 0;
        }

        public static bool TryBuyMarketListing(SIGSaveData save, string listingId, int requestedBatch, SIGEventBus bus, out string message)
        {
            EnsureWorldInitialized(save);
            var cfg = SIGConfigTable.GetMarketListing(listingId);
            var slot = save.World.MarketSlots.FirstOrDefault(x => x.ListingId == listingId);
            if (cfg == null || slot == null)
            {
                message = "未找到该坊市货品配置。";
                return false;
            }
            if (slot.RemainingBuyStock <= 0)
            {
                message = string.Format("【{0}】当季坊市配额已售罄（余 {1} 个月补货）。", cfg.NameCn, save.World.MonthsUntilMarketRefresh);
                return false;
            }

            double unitPrice = GetUnitBuyStone(save, cfg, slot);
            double curStone = save.Sect.GetResource(ResourceType.SpiritStone);
            int maxAfford = (int)Math.Floor(curStone / unitPrice);
            if (maxAfford <= 0)
            {
                string lack = SectBuildLogic.FormatShortfallDetail(save, reqStone: unitPrice);
                message = string.Format("采购【{0}】灵石不足（单批需 {1:F0} 灵石，{2}）。", cfg.NameCn, unitPrice, lack);
                return false;
            }

            int count = Math.Max(1, Math.Min(requestedBatch, Math.Min(slot.RemainingBuyStock, maxAfford)));
            if (!string.IsNullOrEmpty(cfg.ItemId))
            {
                var itemCfg = SIGConfigTable.GetItem(cfg.ItemId);
                int curQty = save.Sect.GetItemCount(cfg.ItemId);
                int space = (itemCfg != null ? itemCfg.MaxStack : 999) - curQty;
                if (space <= 0)
                {
                    message = string.Format("储物阁【{0}】已达堆叠上限，无需继续采购。", cfg.NameCn);
                    return false;
                }
                count = Math.Min(count, Math.Max(1, space / Math.Max(1, cfg.ItemBatchQty)));
            }

            double totalCost = unitPrice * count;
            save.Sect.AddResource(ResourceType.SpiritStone, -totalCost);
            slot.RemainingBuyStock -= count;

            if (!string.IsNullOrEmpty(cfg.ResourceKey))
            {
                ResourceType rt;
                if (Enum.TryParse(cfg.ResourceKey, true, out rt))
                {
                    save.Sect.AddResource(rt, cfg.ResourceBatchQty * count);
                }
            }
            else if (!string.IsNullOrEmpty(cfg.ItemId))
            {
                save.Sect.AddItem(cfg.ItemId, cfg.ItemBatchQty * count);
            }

            message = string.Format("坊市采购成功：购入 {0} ×{1} 批，共耗灵石 {2:F0}（当季余量 {3}）。",
                cfg.NameCn, count, totalCost, slot.RemainingBuyStock);
            if (bus != null)
            {
                bus.PublishLog(save.GetImmortalDateText(), LogCategory.Sect, message);
                bus.NotifyStateChanged();
            }
            return true;
        }

        public static bool TrySellMarketListing(SIGSaveData save, string listingId, int requestedBatch, SIGEventBus bus, out string message)
        {
            EnsureWorldInitialized(save);
            var cfg = SIGConfigTable.GetMarketListing(listingId);
            var slot = save.World.MarketSlots.FirstOrDefault(x => x.ListingId == listingId);
            if (cfg == null || slot == null)
            {
                message = "未找到该坊市收购项。";
                return false;
            }

            int ownedBatches = GetOwnedTradableBatches(save, cfg);
            if (ownedBatches <= 0)
            {
                message = string.Format("库存不足，无法向坊市抛售【{0}】。", cfg.NameCn);
                return false;
            }

            int count = Math.Max(1, Math.Min(requestedBatch, ownedBatches));
            double unitSell = GetUnitSellStone(save, cfg, slot);
            double totalGain = unitSell * count;

            if (!string.IsNullOrEmpty(cfg.ResourceKey))
            {
                ResourceType rt;
                if (Enum.TryParse(cfg.ResourceKey, true, out rt))
                {
                    save.Sect.AddResource(rt, -cfg.ResourceBatchQty * count);
                }
            }
            else if (!string.IsNullOrEmpty(cfg.ItemId))
            {
                save.Sect.AddItem(cfg.ItemId, -cfg.ItemBatchQty * count);
            }

            save.Sect.AddResource(ResourceType.SpiritStone, totalGain);
            message = string.Format("坊市抛售成交：售出 {0} ×{1} 批，回笼灵石 +{2:F0}（行情 {3:F2}x）。",
                cfg.NameCn, count, totalGain, slot.PriceMultiplier);
            if (bus != null)
            {
                bus.PublishLog(save.GetImmortalDateText(), LogCategory.Sect, message);
                bus.NotifyStateChanged();
            }
            return true;
        }

        /// <summary>
        /// 向周边宗门遣使赠礼结交（魔宗则为演武震慑/通商缓和）
        /// </summary>
        public static bool TryFactionDiplomacy(SIGSaveData save, string factionId, SIGEventBus bus, out string message)
        {
            EnsureWorldInitialized(save);
            var cfg = SIGConfigTable.GetWorldFaction(factionId);
            var rt = FindFaction(save, factionId);
            if (cfg == null || rt == null)
            {
                message = "未找到目标势力信息。";
                return false;
            }

            if (rt.DiplomacyCooldownMonths > 0)
            {
                message = string.Format("与【{0}】近期已有邦交往来，尚需冷却 {1} 个仙历月。", cfg.NameCn, rt.DiplomacyCooldownMonths);
                return false;
            }
            if (rt.Relation >= 100)
            {
                message = string.Format("与【{0}】邦交已臻至生死同盟满分（100），无需再行厚礼。", cfg.NameCn);
                return false;
            }

            double cost = cfg.DiplomacyStoneCost;
            if (save.Sect.GetResource(ResourceType.SpiritStone) < cost)
            {
                string lack = SectBuildLogic.FormatShortfallDetail(save, reqStone: cost);
                message = string.Format("邦交遣使需备礼金 {0:F0} 灵石（{1}）。", cost, lack);
                return false;
            }

            save.Sect.AddResource(ResourceType.SpiritStone, -cost);
            int gain = cfg.AlignmentKey == "Demonic" ? 18 : 16;
            rt.Relation = Math.Min(100, rt.Relation + gain);
            rt.DiplomacyCooldownMonths = 6;
            rt.RecentTrendNote = cfg.AlignmentKey == "Demonic"
                ? "忌惮本宗护山锋芒并收受通商契金，近期收敛外域魔修"
                : "笑纳本宗厚礼，双方执事往来愈发密切";

            SyncSectWorldModifiers(save);

            message = string.Format("已向【{0}】遣使往来（耗 {1:F0} 灵石），邦交关系升至 {2} ({3})！",
                cfg.NameCn, cost, rt.Relation, RelationTierCn(rt.Relation));
            AppendChronicle(save, message);
            if (bus != null)
            {
                bus.PublishLog(save.GetImmortalDateText(), LogCategory.Sect, message);
                bus.NotifyStateChanged();
            }
            return true;
        }

        /// <summary>
        /// 当掌门晋升大境界时检查并入队专属里程碑奇遇
        /// </summary>
        public static void CheckRealmMilestoneEncounter(SIGSaveData save, RealmTier newRealm, SIGEventBus bus)
        {
            if (save == null) return;
            EnsureWorldInitialized(save);
            string key = newRealm.ToString();
            if (save.World.TriggeredRealmMilestones.Contains(key)) return;

            var evts = SIGConfigTable.Current != null ? SIGConfigTable.Current.EncounterEvents : null;
            if (evts == null) return;

            var match = evts.FirstOrDefault(e =>
                e.TriggerTypeKey == "RealmMilestone" &&
                string.Equals(e.TriggerRealmKey, key, StringComparison.OrdinalIgnoreCase));
            if (match == null) return;

            save.World.TriggeredRealmMilestones.Add(key);
            EnqueueEncounterById(save, match.Id, bus);
        }

        public static bool TriggerRandomMonthlyEncounter(SIGSaveData save, Random rng, SIGEventBus bus)
        {
            if (save == null) return false;
            EnsureWorldInitialized(save);
            if (save.World.PendingEncounters.Count >= MaxPendingEncounters) return false;

            var all = SIGConfigTable.Current != null ? SIGConfigTable.Current.EncounterEvents : null;
            if (all == null || all.Count == 0) return false;

            var activeIds = new HashSet<string>(save.World.PendingEncounters.Select(p => p.EventId));
            var candidates = all.Where(e =>
                e.TriggerTypeKey == "RandomMonthly" &&
                save.SIGYear >= e.MinYear &&
                !activeIds.Contains(e.Id) &&
                e.Weight > 0).ToList();

            if (candidates.Count == 0) return false;

            double totalWeight = candidates.Sum(c => c.Weight);
            double roll = (rng ?? new Random()).NextDouble() * totalWeight;
            EncounterEventConfig picked = candidates[candidates.Count - 1];
            double acc = 0.0;
            for (int i = 0; i < candidates.Count; i++)
            {
                acc += candidates[i].Weight;
                if (roll <= acc)
                {
                    picked = candidates[i];
                    break;
                }
            }

            return EnqueueEncounterById(save, picked.Id, bus);
        }

        public static bool EnqueueEncounterById(SIGSaveData save, string eventId, SIGEventBus bus)
        {
            if (save == null || string.IsNullOrEmpty(eventId)) return false;
            EnsureWorldInitialized(save);
            var cfg = SIGConfigTable.GetEncounterEvent(eventId);
            if (cfg == null) return false;

            if (save.World.PendingEncounters.Count >= MaxPendingEncounters)
            {
                // 若是连续奇遇链且队列已满，替换最旧的一条普通随机奇遇，确保事件链不断档
                if (cfg.TriggerTypeKey != "RandomMonthly")
                {
                    int idx = save.World.PendingEncounters.FindIndex(p =>
                    {
                        var ec = SIGConfigTable.GetEncounterEvent(p.EventId);
                        return ec == null || ec.TriggerTypeKey == "RandomMonthly";
                    });
                    if (idx >= 0) save.World.PendingEncounters.RemoveAt(idx);
                    else return false;
                }
                else
                {
                    return false;
                }
            }

            var pending = new PendingEncounterData
            {
                InstanceUid = "ENC_INST_" + (save.World.NextEncounterSeq++),
                EventId = cfg.Id,
                TriggerDateText = save.GetImmortalDateText(),
                ChainStep = cfg.ChainStep,
                ChainTotal = cfg.ChainTotal
            };
            save.World.PendingEncounters.Add(pending);

            string tag = cfg.ChainTotal > 1
                ? string.Format("奇遇链 {0}/{1}", cfg.ChainStep, cfg.ChainTotal)
                : (cfg.TriggerTypeKey == "RealmMilestone" ? "大道机缘" : "天机奇遇");

            string logMsg = string.Format("★【{0}】触发奇遇《{1}》，已收入【天下奇遇】待决台（绝不打断挂机，可随时前往抉择）。",
                tag, cfg.TitleCn);
            AppendChronicle(save, string.Format("触发奇遇《{0}》({1})，待掌门定夺。", cfg.TitleCn, tag));
            if (bus != null)
            {
                bus.PublishLog(save.GetImmortalDateText(), LogCategory.System, logMsg);
                bus.NotifyStateChanged();
            }
            return true;
        }

        public static bool CanChooseEncounterOption(SIGSaveData save, EncounterOptionConfig opt, out string blockReason)
        {
            blockReason = "";
            if (save == null || save.Sect == null || save.Leader == null || opt == null)
            {
                blockReason = "奇遇选项无效。";
                return false;
            }

            RealmTier minR = opt.GetMinRealm();
            if (save.Leader.Realm < minR)
            {
                blockReason = "需掌门达到 " + EnumTextMap.RealmBaseCn(minR);
                return false;
            }

            if (opt.CostSpiritStone > 0 && save.Sect.GetResource(ResourceType.SpiritStone) < opt.CostSpiritStone)
            {
                blockReason = string.Format("灵石不足 {0:F0}", opt.CostSpiritStone);
                return false;
            }
            if (opt.CostHerb > 0 && save.Sect.GetResource(ResourceType.SpiritHerb) < opt.CostHerb)
            {
                blockReason = string.Format("灵草不足 {0:F0}", opt.CostHerb);
                return false;
            }
            return true;
        }

        public static double GetEffectiveOptionSuccessRate(SIGSaveData save, EncounterOptionConfig opt)
        {
            if (opt == null) return 0.0;
            if (opt.BaseSuccessRate >= 0.999) return 1.0;
            int fortune = (save != null && save.Leader != null && save.Leader.Innate != null)
                ? save.Leader.Innate.Fortune
                : 10;
            double rate = opt.BaseSuccessRate + Math.Max(0, fortune - 8) * 0.008;
            return Math.Min(0.98, Math.Max(0.20, rate));
        }

        public static bool ResolveEncounterOption(
            SIGSaveData save,
            string instanceUid,
            int optionIndex,
            Random rng,
            SIGEventBus bus,
            out string message)
        {
            EnsureWorldInitialized(save);
            var w = save.World;
            var pending = string.IsNullOrEmpty(instanceUid)
                ? w.PendingEncounters.FirstOrDefault()
                : w.PendingEncounters.FirstOrDefault(x => x.InstanceUid == instanceUid);

            if (pending == null)
            {
                message = "当前没有待抉择的天机奇遇。";
                return false;
            }

            var evtCfg = SIGConfigTable.GetEncounterEvent(pending.EventId);
            if (evtCfg == null || evtCfg.Options == null || optionIndex < 0 || optionIndex >= evtCfg.Options.Count)
            {
                w.PendingEncounters.Remove(pending);
                message = "奇遇配置已失效，已自动移除。";
                return false;
            }

            var opt = evtCfg.Options[optionIndex];
            string block;
            if (!CanChooseEncounterOption(save, opt, out block))
            {
                message = string.Format("无法择定【{0}】：{1}。", opt.OptionTitle, block);
                return false;
            }

            if (opt.CostSpiritStone > 0) save.Sect.AddResource(ResourceType.SpiritStone, -opt.CostSpiritStone);
            if (opt.CostHerb > 0) save.Sect.AddResource(ResourceType.SpiritHerb, -opt.CostHerb);
            if (opt.CostCurQiRatio > 0)
            {
                save.Leader.CurQi = Math.Max(0.0, save.Leader.CurQi * (1.0 - opt.CostCurQiRatio));
            }

            w.PendingEncounters.Remove(pending);
            w.TotalResolvedEncounters++;

            if (rng == null) rng = new Random();
            double effRate = GetEffectiveOptionSuccessRate(save, opt);
            bool ok = rng.NextDouble() <= effRate;

            if (ok)
            {
                var gains = new List<string>();
                if (opt.SuccessStone > 0) { save.Sect.AddResource(ResourceType.SpiritStone, opt.SuccessStone); gains.Add(string.Format("灵石+{0:F0}", opt.SuccessStone)); }
                if (opt.SuccessHerb > 0) { save.Sect.AddResource(ResourceType.SpiritHerb, opt.SuccessHerb); gains.Add(string.Format("灵草+{0:F0}", opt.SuccessHerb)); }
                if (opt.SuccessOre > 0) { save.Sect.AddResource(ResourceType.SpiritOre, opt.SuccessOre); gains.Add(string.Format("玄铁+{0:F0}", opt.SuccessOre)); }
                if (opt.SuccessCore > 0) { save.Sect.AddResource(ResourceType.MonsterCore, opt.SuccessCore); gains.Add(string.Format("妖丹+{0:F0}", opt.SuccessCore)); }
                if (opt.SuccessReputation > 0) { save.Sect.AddResource(ResourceType.SectReputation, opt.SuccessReputation); gains.Add(string.Format("声望+{0:F0}", opt.SuccessReputation)); }
                if (opt.SuccessQiGrant > 0)
                {
                    save.Leader.CurQi = Math.Min(save.Leader.MaxQi, save.Leader.CurQi + opt.SuccessQiGrant);
                    gains.Add(string.Format("修为+{0:F0}", opt.SuccessQiGrant));
                }
                if (!string.IsNullOrEmpty(opt.SuccessItemId) && opt.SuccessItemQty > 0)
                {
                    save.Sect.AddItem(opt.SuccessItemId, opt.SuccessItemQty);
                    var it = SIGConfigTable.GetItem(opt.SuccessItemId);
                    gains.Add(string.Format("{0}×{1}", it != null ? it.NameCn : opt.SuccessItemId, opt.SuccessItemQty));
                }
                if (opt.SuccessArtifactTier > 0 && save.Sect.ArtifactVault.Count < save.Sect.ArtifactVaultCap)
                {
                    var art = ArtifactFormationLogic.GenerateRandomArtifact(save.Sect, rng, opt.SuccessArtifactTier, opt.SuccessArtifactTier);
                    save.Sect.ArtifactVault.Add(art);
                    gains.Add(string.Format("获阵宝【{0}】", art.NameCn));
                }
                if (!string.IsNullOrEmpty(opt.SuccessBuffName) && opt.SuccessBuffMonths > 0)
                {
                    w.TempBuffs.Add(new WorldTempBuffData
                    {
                        BuffName = opt.SuccessBuffName,
                        RemainingMonths = opt.SuccessBuffMonths,
                        AuraRateAdd = opt.SuccessTempAuraAdd,
                        AtkPctAdd = opt.SuccessTempAtkAdd
                    });
                    gains.Add(string.Format("气运【{0}】({1}月)", opt.SuccessBuffName, opt.SuccessBuffMonths));
                }
                if (!string.IsNullOrEmpty(opt.TargetFactionId) && opt.SuccessRelationDelta != 0)
                {
                    var fac = FindFaction(save, opt.TargetFactionId);
                    var fCfg = SIGConfigTable.GetWorldFaction(opt.TargetFactionId);
                    if (fac != null)
                    {
                        fac.Relation = Math.Max(-100, Math.Min(100, fac.Relation + opt.SuccessRelationDelta));
                        gains.Add(string.Format("{0}好感{1}{2}", fCfg != null ? fCfg.ShortName : opt.TargetFactionId, opt.SuccessRelationDelta > 0 ? "+" : "", opt.SuccessRelationDelta));
                    }
                }
                if (!string.IsNullOrEmpty(opt.NextChainEventId))
                {
                    w.ScheduledFollowups.Add(new ScheduledChainFollowupData
                    {
                        NextEventId = opt.NextChainEventId,
                        RemainingMonths = Math.Max(1, opt.NextChainDelayMonths)
                    });
                    gains.Add(string.Format("已埋下后续机缘({0}月后应验)", Math.Max(1, opt.NextChainDelayMonths)));
                }

                SyncSectWorldModifiers(save);

                string gainTail = gains.Count > 0 ? (" [" + string.Join(" / ", gains.ToArray()) + "]") : "";
                message = string.Format("★《{0}》抉择【{1}】功成：{2}{3}",
                    evtCfg.TitleCn, opt.OptionTitle, opt.SuccessOutcomeText, gainTail);
                AppendChronicle(save, message);
                if (bus != null)
                {
                    bus.PublishLog(save.GetImmortalDateText(), LogCategory.System, message);
                    bus.NotifyStateChanged();
                }
                return true;
            }
            else
            {
                if (opt.FailHpLossRatio > 0)
                {
                    save.Leader.BattleStats.CurHp = Math.Max(10.0, Math.Round(save.Leader.BattleStats.CurHp * (1.0 - opt.FailHpLossRatio), 0));
                }
                if (opt.FailReputationDelta != 0)
                {
                    save.Sect.AddResource(ResourceType.SectReputation, opt.FailReputationDelta);
                }
                message = string.Format("[奇遇受挫]《{0}》尝试【{1}】未果（成率 {2:P0}）：{3}",
                    evtCfg.TitleCn, opt.OptionTitle, effRate,
                    !string.IsNullOrEmpty(opt.FailOutcomeText) ? opt.FailOutcomeText : "机缘擦肩而过，掌门调息收敛真元。");
                AppendChronicle(save, message);
                if (bus != null)
                {
                    bus.PublishLog(save.GetImmortalDateText(), LogCategory.System, message);
                    bus.NotifyStateChanged();
                }
                return false;
            }
        }
    }
}
