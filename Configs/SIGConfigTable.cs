using System;
using System.Collections.Generic;
using System.Linq;
using SIGTool.Datas;

namespace SIGTool.Configs
{
    /// <summary>
    /// 全局静态配置表索引仓库与内置默认数据生成器（命名统一为 SIGConfigTable）
    /// </summary>
    public static class SIGConfigTable
    {
        public static SIGConfigRoot Current { get; private set; }

        private static Dictionary<RealmTier, RealmConfig> _realmMap = new Dictionary<RealmTier, RealmConfig>();
        private static Dictionary<string, BuildingConfig> _buildingMap = new Dictionary<string, BuildingConfig>();
        private static Dictionary<string, ItemConfig> _itemMap = new Dictionary<string, ItemConfig>();
        private static Dictionary<string, RecipeConfig> _recipeMap = new Dictionary<string, RecipeConfig>();
        private static Dictionary<string, TechniqueConfig> _techMap = new Dictionary<string, TechniqueConfig>();
        private static Dictionary<string, EnemyTemplateConfig> _enemyMap = new Dictionary<string, EnemyTemplateConfig>();
        private static Dictionary<string, CombatStageConfig> _stageMap = new Dictionary<string, CombatStageConfig>();
        private static Dictionary<string, ArtifactTemplateConfig> _artifactTplMap = new Dictionary<string, ArtifactTemplateConfig>();
        private static Dictionary<string, AcupointNodeConfig> _acupointMap = new Dictionary<string, AcupointNodeConfig>();
        private static Dictionary<string, WorldFactionConfig> _factionMap = new Dictionary<string, WorldFactionConfig>();
        private static Dictionary<string, MarketListingConfig> _marketMap = new Dictionary<string, MarketListingConfig>();
        private static Dictionary<string, EncounterEventConfig> _encounterMap = new Dictionary<string, EncounterEventConfig>();

        static SIGConfigTable()
        {
            ApplyConfig(CreateDefaultConfig());
        }

        public static void ApplyConfig(SIGConfigRoot root)
        {
            if (root == null) root = CreateDefaultConfig();
            if (root.ArtifactTemplates == null || root.ArtifactTemplates.Count == 0)
            {
                root.ArtifactTemplates = CreateDefaultArtifactTemplates();
            }
            if (root.Acupoints == null || root.Acupoints.Count == 0)
            {
                root.Acupoints = CreateDefaultAcupoints();
            }
            if (root.WorldFactions == null || root.WorldFactions.Count == 0)
            {
                root.WorldFactions = CreateDefaultWorldFactions();
            }
            if (root.MarketListings == null || root.MarketListings.Count == 0)
            {
                root.MarketListings = CreateDefaultMarketListings();
            }
            if (root.EncounterEvents == null || root.EncounterEvents.Count == 0)
            {
                root.EncounterEvents = CreateDefaultEncounterEvents();
            }
            Current = root;

            _realmMap = new Dictionary<RealmTier, RealmConfig>();
            if (root.Realms != null)
            {
                foreach (var r in root.Realms)
                {
                    _realmMap[r.GetRealmTier()] = r;
                }
            }

            _buildingMap = (root.Buildings ?? new List<BuildingConfig>()).ToDictionary(x => x.Id, x => x);
            _itemMap = (root.Items ?? new List<ItemConfig>()).ToDictionary(x => x.Id, x => x);
            _recipeMap = (root.Recipes ?? new List<RecipeConfig>()).ToDictionary(x => x.Id, x => x);
            _techMap = (root.Techniques ?? new List<TechniqueConfig>()).ToDictionary(x => x.Id, x => x);
            _enemyMap = (root.Enemies ?? new List<EnemyTemplateConfig>()).ToDictionary(x => x.Id, x => x);
            _stageMap = (root.Stages ?? new List<CombatStageConfig>()).ToDictionary(x => x.Id, x => x);
            _artifactTplMap = (root.ArtifactTemplates ?? new List<ArtifactTemplateConfig>()).ToDictionary(x => x.Id, x => x);
            _acupointMap = (root.Acupoints ?? new List<AcupointNodeConfig>()).ToDictionary(x => x.Id, x => x);
            _factionMap = (root.WorldFactions ?? new List<WorldFactionConfig>()).ToDictionary(x => x.Id, x => x);
            _marketMap = (root.MarketListings ?? new List<MarketListingConfig>()).ToDictionary(x => x.Id, x => x);
            _encounterMap = (root.EncounterEvents ?? new List<EncounterEventConfig>()).ToDictionary(x => x.Id, x => x);
        }

        public static RealmConfig GetRealm(RealmTier tier)
        {
            RealmConfig cfg;
            if (_realmMap.TryGetValue(tier, out cfg)) return cfg;
            return _realmMap.Values.FirstOrDefault() ?? new RealmConfig();
        }

        public static BuildingConfig GetBuilding(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            BuildingConfig cfg;
            return _buildingMap.TryGetValue(id, out cfg) ? cfg : null;
        }

        public static ItemConfig GetItem(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            ItemConfig cfg;
            return _itemMap.TryGetValue(id, out cfg) ? cfg : null;
        }

        public static RecipeConfig GetRecipe(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            RecipeConfig cfg;
            return _recipeMap.TryGetValue(id, out cfg) ? cfg : null;
        }

        public static TechniqueConfig GetTechnique(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            TechniqueConfig cfg;
            return _techMap.TryGetValue(id, out cfg) ? cfg : null;
        }

        public static EnemyTemplateConfig GetEnemy(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            EnemyTemplateConfig cfg;
            return _enemyMap.TryGetValue(id, out cfg) ? cfg : null;
        }

        public static CombatStageConfig GetStage(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            CombatStageConfig cfg;
            return _stageMap.TryGetValue(id, out cfg) ? cfg : null;
        }

        public static ArtifactTemplateConfig GetArtifactTemplate(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            ArtifactTemplateConfig cfg;
            return _artifactTplMap.TryGetValue(id, out cfg) ? cfg : null;
        }

        public static AcupointNodeConfig GetAcupoint(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            AcupointNodeConfig cfg;
            return _acupointMap.TryGetValue(id, out cfg) ? cfg : null;
        }

        public static WorldFactionConfig GetWorldFaction(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            WorldFactionConfig cfg;
            return _factionMap.TryGetValue(id, out cfg) ? cfg : null;
        }

        public static MarketListingConfig GetMarketListing(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            MarketListingConfig cfg;
            return _marketMap.TryGetValue(id, out cfg) ? cfg : null;
        }

        public static EncounterEventConfig GetEncounterEvent(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            EncounterEventConfig cfg;
            return _encounterMap.TryGetValue(id, out cfg) ? cfg : null;
        }

        public static List<WorldFactionConfig> CreateDefaultWorldFactions()
        {
            return new List<WorldFactionConfig>
            {
                new WorldFactionConfig
                {
                    Id = "FAC_QINGYUN", NameCn = "青云剑派", ShortName = "青云派", AlignmentKey = "Righteous",
                    SpecialtyTag = "庚金剑道", InitProsperity = 65, InitRelation = 10, DiplomacyStoneCost = 110,
                    FriendlyBonusDesc = "交好:全宗攻+6% | 盟友:全宗攻+12%",
                    Desc = "雄踞东洲太白剑峰的正道大宗，门下剑修辈出，常年求购玄铁精矿。"
                },
                new WorldFactionConfig
                {
                    Id = "FAC_BAICAO", NameCn = "百草丹谷", ShortName = "百草谷", AlignmentKey = "Righteous",
                    SpecialtyTag = "灵植丹道", InitProsperity = 60, InitRelation = 15, DiplomacyStoneCost = 100,
                    FriendlyBonusDesc = "交好:灵草+12%/成丹+4% | 盟友:灵草+22%/成丹+8%",
                    Desc = "传承千年的杏林丹道世家，掌控南疆大片百草药园，兴盛时可平抑丹材价格。"
                },
                new WorldFactionConfig
                {
                    Id = "FAC_TIANJI", NameCn = "天机商盟", ShortName = "天机盟", AlignmentKey = "Neutral",
                    SpecialtyTag = "九州商贸", InitProsperity = 70, InitRelation = 5, DiplomacyStoneCost = 130,
                    FriendlyBonusDesc = "交好:坊市买-6%/卖+6% | 盟友:坊市买-12%/卖+12%",
                    Desc = "遍布各大修仙坊市的中立商会，精通天机推演与灵材调拨，交好可显著降低坊市折损。"
                },
                new WorldFactionConfig
                {
                    Id = "FAC_BEIMING", NameCn = "北溟玄宫", ShortName = "北溟宫", AlignmentKey = "Righteous",
                    SpecialtyTag = "聚灵阵道", InitProsperity = 58, InitRelation = 0, DiplomacyStoneCost = 120,
                    FriendlyBonusDesc = "交好:全宗灵气+10% | 盟友:全宗灵气+20%",
                    Desc = "隐居北溟寒海的阵道名门，擅长引动天地水脉潮汐滋养护山聚灵阵。"
                },
                new WorldFactionConfig
                {
                    Id = "FAC_XUEYING", NameCn = "血影魔宗", ShortName = "血影宗", AlignmentKey = "Demonic",
                    SpecialtyTag = "魔渊煞器", InitProsperity = 52, InitRelation = -25, DiplomacyStoneCost = 140,
                    FriendlyBonusDesc = "震慑/缓和(>=0):免除魔患滋扰，秘境妖丹掉率+12%",
                    Desc = "盘踞九幽裂隙边缘的魔道巨擘，声势过盛时会搅动周边妖兽躁动，亦可通过演武威慑令其收敛。"
                }
            };
        }

        public static List<MarketListingConfig> CreateDefaultMarketListings()
        {
            return new List<MarketListingConfig>
            {
                new MarketListingConfig { Id = "MKT_RES_HERB", NameCn = "百草灵叶包 (灵草×25)", ShortName = "灵草×25", CategoryTag = "Herb", ResourceKey = "SpiritHerb", ResourceBatchQty = 25, BaseBuyStone = 55, BaseSellStone = 38, QuarterMaxStock = 16, Desc = "标准二十五株装炼丹灵草，受百草丰年与丹材荒岁行情影响极大。" },
                new MarketListingConfig { Id = "MKT_RES_WOOD", NameCn = "铁桦灵木捆 (灵木×40)", ShortName = "灵木×40", CategoryTag = "Material", ResourceKey = "SpiritWood", ResourceBatchQty = 40, BaseBuyStone = 48, BaseSellStone = 32, QuarterMaxStock = 16, Desc = "宗门营造与法器制木刚需大宗资材。" },
                new MarketListingConfig { Id = "MKT_RES_ORE", NameCn = "玄铁矿髓箱 (玄铁×40)", ShortName = "玄铁×40", CategoryTag = "Material", ResourceKey = "SpiritOre", ResourceBatchQty = 40, BaseBuyStone = 56, BaseSellStone = 38, QuarterMaxStock = 16, Desc = "百炼器坊与护山剑台核心精矿，各大剑宗常年竞购。" },
                new MarketListingConfig { Id = "MKT_RES_FOOD", NameCn = "金穗灵谷担 (灵谷×50)", ShortName = "灵谷×50", CategoryTag = "Herb", ResourceKey = "SpiritFood", ResourceBatchQty = 50, BaseBuyStone = 36, BaseSellStone = 24, QuarterMaxStock = 20, Desc = "保障门人弟子月度口粮的基础灵米，口粮告急时可应急采购。" },
                new MarketListingConfig { Id = "MKT_RES_CORE", NameCn = "秘境妖丹匣 (妖丹×3)", ShortName = "妖丹×3", CategoryTag = "Core", ResourceKey = "MonsterCore", ResourceBatchQty = 3, BaseBuyStone = 88, BaseSellStone = 62, QuarterMaxStock = 10, Desc = "炼制破境灵丹与祭炼词条阵宝的高阶稀缺灵材。" },
                new MarketListingConfig { Id = "MKT_ITM_JUQI", NameCn = "聚气丹 (成丹×1)", ShortName = "聚气丹", CategoryTag = "Pill", ItemId = "ITEM_JUQI_PILL", ItemBatchQty = 1, BaseBuyStone = 28, BaseSellStone = 19, QuarterMaxStock = 15, Desc = "炼气修士常备吐纳灵丹，丹材涨价时抛售成丹利润丰厚。" },
                new MarketListingConfig { Id = "MKT_ITM_HEAL1", NameCn = "回春丹 (成丹×1)", ShortName = "回春丹", CategoryTag = "Pill", ItemId = "ITEM_HEAL_PILL_1", ItemBatchQty = 1, BaseBuyStone = 22, BaseSellStone = 15, QuarterMaxStock = 18, Desc = "秘境历练常备疗伤灵丹，群妖躁动之年尤为抢手。" },
                new MarketListingConfig { Id = "MKT_ITM_ZHUJI", NameCn = "筑基灵丹 (宝丹×1)", ShortName = "筑基丹", CategoryTag = "Pill", ItemId = "ITEM_ZHUJI_PILL", ItemBatchQty = 1, BaseBuyStone = 195, BaseSellStone = 135, QuarterMaxStock = 4, Desc = "冲击筑基大道的珍稀护脉宝丹，每季坊市流通数量极少。" },
                new MarketListingConfig { Id = "MKT_ITM_JIUZHUAN", NameCn = "九转培元丹 (成丹×1)", ShortName = "培元丹", CategoryTag = "Pill", ItemId = "ITEM_JIUZHUAN_PILL", ItemBatchQty = 1, BaseBuyStone = 130, BaseSellStone = 90, QuarterMaxStock = 8, Desc = "中阶精进气海灵丹，可快速充盈掌门冲穴与破境所需真元。" },
                new MarketListingConfig { Id = "MKT_ITM_JIEDAN", NameCn = "紫金结丹丸 (宝丹×1)", ShortName = "结丹丸", CategoryTag = "Pill", ItemId = "ITEM_JIEDAN_PILL", ItemBatchQty = 1, BaseBuyStone = 520, BaseSellStone = 360, QuarterMaxStock = 2, Desc = "凝结无瑕金丹的压轴奇珍，古迹现世年份溢价明显。" }
            };
        }

        public static List<EncounterEventConfig> CreateDefaultEncounterEvents()
        {
            return new List<EncounterEventConfig>
            {
                // ==================== 大境界里程碑专属奇遇 ====================
                new EncounterEventConfig
                {
                    Id = "ENC_REALM_FOUNDATION", TitleCn = "紫府初开·百宗观礼", TriggerTypeKey = "RealmMilestone", TriggerRealmKey = "Foundation",
                    MinYear = 1, Weight = 0, ChainStep = 1, ChainTotal = 1,
                    NarrativeDesc = "掌门成功铸就大道金基，山门瑞气千条！青云剑派与百草丹谷皆遣执事携贺礼登门观礼，欲探我宗后续立道方略。",
                    Options = new List<EncounterOptionConfig>
                    {
                        new EncounterOptionConfig
                        {
                            OptionTitle = "大开山门讲法论道", RiskTag = "稳妥·耗60灵石", PreviewGainText = "声望+45 / 青云与百草好感+18 / 全宗灵气+12%(12月)",
                            CostSpiritStone = 60, BaseSuccessRate = 1.0, SuccessReputation = 45,
                            TargetFactionId = "FAC_BAICAO", SuccessRelationDelta = 18,
                            SuccessBuffName = "紫府论道余韵", SuccessBuffMonths = 12, SuccessTempAuraAdd = 0.12,
                            SuccessOutcomeText = "讲道三日宾主尽欢，周边正道皆赞太虚云隐宗气象恢弘！"
                        },
                        new EncounterOptionConfig
                        {
                            OptionTitle = "邀青云剑修切磋演武", RiskTag = "成率80%", PreviewGainText = "胜则全宗攻伐+10%(12月) / 灵石+160 / 妖丹+3",
                            BaseSuccessRate = 0.80, SuccessStone = 160, SuccessCore = 3, SuccessReputation = 30,
                            TargetFactionId = "FAC_QINGYUN", SuccessRelationDelta = 15,
                            SuccessBuffName = "试剑锋芒", SuccessBuffMonths = 12, SuccessTempAtkAdd = 0.10,
                            SuccessOutcomeText = "掌门以新成筑基真元略胜半招，青云执事抚掌叹服并赠剑道资材！",
                            FailHpLossRatio = 0.20, FailOutcomeText = "剑气交锋稍逊半筹受了轻伤，但仍获观礼宾客敬重。"
                        },
                        new EncounterOptionConfig
                        {
                            OptionTitle = "低调谢客闭关敛息", RiskTag = "100%无耗", PreviewGainText = "掌门修为+520 / 培元丹×1",
                            BaseSuccessRate = 1.0, SuccessQiGrant = 520, SuccessItemId = "ITEM_JIUZHUAN_PILL", SuccessItemQty = 1,
                            SuccessOutcomeText = "谢绝繁文缛节潜心稳固筑基道果，气海修为精进一大截。"
                        }
                    }
                },
                new EncounterEventConfig
                {
                    Id = "ENC_REALM_GOLDENCORE", TitleCn = "金丹霞举·九霄星煞", TriggerTypeKey = "RealmMilestone", TriggerRealmKey = "GoldenCore",
                    MinYear = 1, Weight = 0, ChainStep = 1, ChainTotal = 1,
                    NarrativeDesc = "金丹成象引动九霄星辰罡煞垂落主峰！若借天威洗练本命阵宝或可一步登天，亦可以护山阵稳妥引导滋养灵脉。",
                    Options = new List<EncounterOptionConfig>
                    {
                        new EncounterOptionConfig
                        {
                            OptionTitle = "只身逆迎星煞淬炼重宝", RiskTag = "高险·成率72%", PreviewGainText = "赐3阶随机词条阵宝×1 / 掌门修为+2200 / 声望+80",
                            BaseSuccessRate = 0.72, SuccessArtifactTier = 3, SuccessQiGrant = 2200, SuccessReputation = 80,
                            SuccessOutcomeText = "漫天星煞尽数熔入炉鼎，一件流淌星辉的三阶镇派阵宝破空出世！",
                            FailHpLossRatio = 0.35, FailOutcomeText = "星煞狂暴反噬震伤经脉，所幸金丹无恙，急需调息养伤。"
                        },
                        new EncounterOptionConfig
                        {
                            OptionTitle = "启护山阵引星辉入脉", RiskTag = "稳妥·耗120灵石", PreviewGainText = "全宗灵气+18%与攻+8%(18月) / 灵草+60 / 妖丹+5",
                            CostSpiritStone = 120, BaseSuccessRate = 1.0, SuccessHerb = 60, SuccessCore = 5,
                            SuccessBuffName = "星辉灌脉", SuccessBuffMonths = 18, SuccessTempAuraAdd = 0.18, SuccessTempAtkAdd = 0.08,
                            SuccessOutcomeText = "星辉化作涓涓甘霖浸润全宗二十四处灵脉，未来年半吐纳一日千里！"
                        },
                        new EncounterOptionConfig
                        {
                            OptionTitle = "拓印金丹道纹赠天机盟", RiskTag = "100%稳赚", PreviewGainText = "灵石+480 / 天机商盟好感+25",
                            BaseSuccessRate = 1.0, SuccessStone = 480, TargetFactionId = "FAC_TIANJI", SuccessRelationDelta = 25,
                            SuccessOutcomeText = "天机商盟奉上厚礼收购观摩拓本，本宗在九州商路地位大涨。"
                        }
                    }
                },
                new EncounterEventConfig
                {
                    Id = "ENC_REALM_NASCENT", TitleCn = "元婴神游·太虚遗蜕", TriggerTypeKey = "RealmMilestone", TriggerRealmKey = "NascentSoul",
                    MinYear = 1, Weight = 0, ChainStep = 1, ChainTotal = 1,
                    NarrativeDesc = "元婴初成神游太虚裂隙，竟感应到一具上古化神大能坐化遗蜕，身畔悬浮半卷仙诀与古镜虚影。",
                    Options = new List<EncounterOptionConfig>
                    {
                        new EncounterOptionConfig
                        {
                            OptionTitle = "强施神识夺取古仙至宝", RiskTag = "极险·成率68%", PreviewGainText = "赐4阶仙品潜力阵宝×1 / 灵石+900",
                            BaseSuccessRate = 0.68, SuccessArtifactTier = 4, SuccessStone = 900, SuccessReputation = 120,
                            SuccessOutcomeText = "神识破开万载残禁，成功携回上古洞天四阶至宝！",
                            FailHpLossRatio = 0.40, FailOutcomeText = "残存禁制凌厉反击震退元婴，虽未夺宝亦记下几分大道玄机。"
                        },
                        new EncounterOptionConfig
                        {
                            OptionTitle = "恭敬稽首参悟坐化道韵", RiskTag = "100%稳妥", PreviewGainText = "掌门修为+8500 / 凝婴丹×1 / 灵气+22%(24月)",
                            BaseSuccessRate = 1.0, SuccessQiGrant = 8500, SuccessItemId = "ITEM_NINGYING_PILL", SuccessItemQty = 1,
                            SuccessBuffName = "古仙道韵", SuccessBuffMonths = 24, SuccessTempAuraAdd = 0.22,
                            SuccessOutcomeText = "遗蜕化作清风消散，留下一枚古丹与浩瀚道韵萦绕主峰！"
                        }
                    }
                },

                // ==================== 连续多阶段奇遇链 A（共 3 环：寒门散修 -> 古洞舆图 -> 云梦丹室） ====================
                new EncounterEventConfig
                {
                    Id = "ENC_CHAIN_HERMIT_1", TitleCn = "寒门散修叩门求药", TriggerTypeKey = "RandomMonthly",
                    MinYear = 1, Weight = 14.0, ChainStep = 1, ChainTotal = 3,
                    NarrativeDesc = "一名身负妖毒的云游散修跌撞至山门石阶，怀揣半卷模糊兽皮残图，恳求赐予灵草救命。",
                    Options = new List<EncounterOptionConfig>
                    {
                        new EncounterOptionConfig
                        {
                            OptionTitle = "赐百草灵叶倾力救治", RiskTag = "善缘·耗22灵草", PreviewGainText = "声望+20 / 结下因果(4个月后触发后续机缘)",
                            CostHerb = 22, BaseSuccessRate = 1.0, SuccessReputation = 20,
                            NextChainEventId = "ENC_CHAIN_HERMIT_2", NextChainDelayMonths = 4,
                            SuccessOutcomeText = "散修服药拔除妖毒，叩首立誓伤愈查明残图来历后必登门厚报！"
                        },
                        new EncounterOptionConfig
                        {
                            OptionTitle = "出资买下兽皮残卷参详", RiskTag = "稳妥·耗55灵石", PreviewGainText = "掌门修为+240 / 玄铁+30",
                            CostSpiritStone = 55, BaseSuccessRate = 1.0, SuccessQiGrant = 240, SuccessOre = 30,
                            SuccessOutcomeText = "散修得灵石自行下山购药，你从残卷边角参悟出一套吐纳小窍门。"
                        },
                        new EncounterOptionConfig
                        {
                            OptionTitle = "严守门规婉言遣离", RiskTag = "无消耗", PreviewGainText = "灵石+25(山门巡查拾遗)",
                            BaseSuccessRate = 1.0, SuccessStone = 25,
                            SuccessOutcomeText = "执事弟子礼送散修离去，山门清净无波。"
                        }
                    }
                },
                new EncounterEventConfig
                {
                    Id = "ENC_CHAIN_HERMIT_2", TitleCn = "故人携古洞舆图来报", TriggerTypeKey = "ChainFollowup",
                    MinYear = 1, Weight = 0, ChainStep = 2, ChainTotal = 3,
                    NarrativeDesc = "数月前救治的散修气色大好重返山门，查明那兽皮乃【云梦丹师古洞】外围禁制图，特邀本宗共探遗泽！",
                    Options = new List<EncounterOptionConfig>
                    {
                        new EncounterOptionConfig
                        {
                            OptionTitle = "筹备破禁灵符亲探古洞", RiskTag = "成率82%·耗75灵石", PreviewGainText = "灵草+45 / 妖丹+3 / 开启第3环核心丹室",
                            CostSpiritStone = 75, BaseSuccessRate = 0.82, SuccessHerb = 45, SuccessCore = 3,
                            NextChainEventId = "ENC_CHAIN_HERMIT_3", NextChainDelayMonths = 4,
                            SuccessOutcomeText = "顺利破开外围迷雾阵收获颇丰，更发现通往地底核心丹室的封印石门！",
                            FailHpLossRatio = 0.18, FailOutcomeText = "古洞外围剑气残阵激发受阻，只得暂退回山重整旗鼓。"
                        },
                        new EncounterOptionConfig
                        {
                            OptionTitle = "将舆图转售天机商盟变现", RiskTag = "100%稳赚", PreviewGainText = "灵石+240 / 天机商盟好感+15",
                            BaseSuccessRate = 1.0, SuccessStone = 240, TargetFactionId = "FAC_TIANJI", SuccessRelationDelta = 15,
                            SuccessOutcomeText = "天机商盟高价收下古洞坐标，本宗兵不血刃大获灵石充盈库藏。"
                        }
                    }
                },
                new EncounterEventConfig
                {
                    Id = "ENC_CHAIN_HERMIT_3", TitleCn = "云梦秘藏·地心丹室", TriggerTypeKey = "ChainFollowup",
                    MinYear = 1, Weight = 0, ChainStep = 3, ChainTotal = 3,
                    NarrativeDesc = "云梦古洞地心石门缓缓开启，古丹炉内尚有丹香袅袅，石台上更镇着一件灵光流转的护阵古宝，然守护石傀已然苏醒！",
                    Options = new List<EncounterOptionConfig>
                    {
                        new EncounterOptionConfig
                        {
                            OptionTitle = "力战石傀尽取古洞核心秘藏", RiskTag = "成率75%", PreviewGainText = "赐2阶词条阵宝×1 / 筑基丹×1 / 灵石+220",
                            BaseSuccessRate = 0.75, SuccessArtifactTier = 2, SuccessItemId = "ITEM_ZHUJI_PILL", SuccessItemQty = 1, SuccessStone = 220,
                            SuccessOutcomeText = "掌门剑气纵横击碎石傀，将炉中筑基宝丹与石台古宝尽数收入囊中！",
                            FailHpLossRatio = 0.28, FailOutcomeText = "石傀坚逾精金，激战后仅抢出少许丹灰便被迫撤离古洞。"
                        },
                        new EncounterOptionConfig
                        {
                            OptionTitle = "避开石傀收取偏殿药圃心得", RiskTag = "100%稳妥", PreviewGainText = "灵草+70 / 掌门修为+680 / 灵气+12%(10月)",
                            BaseSuccessRate = 1.0, SuccessHerb = 70, SuccessQiGrant = 680,
                            SuccessBuffName = "云梦丹诀启迪", SuccessBuffMonths = 10, SuccessTempAuraAdd = 0.12,
                            SuccessOutcomeText = "稳妥采尽万年药圃与石壁丹诀，满载而归！"
                        }
                    }
                },

                // ==================== 连续多阶段奇遇链 B（共 2 环：地肺异火 -> 火髓成晶） ====================
                new EncounterEventConfig
                {
                    Id = "ENC_CHAIN_VEIN_1", TitleCn = "后山地肺异火暗涌", TriggerTypeKey = "RandomMonthly",
                    MinYear = 1, Weight = 12.0, ChainStep = 1, ChainTotal = 2,
                    NarrativeDesc = "矿脉执事急报：后山灵矿深处忽有赤红地火涌动，矿温骤升，若妥善引流或能孕育罕见火髓灵晶。",
                    Options = new List<EncounterOptionConfig>
                    {
                        new EncounterOptionConfig
                        {
                            OptionTitle = "耗材布设锁火导脉阵", RiskTag = "投资·耗65灵石", PreviewGainText = "玄铁+35 / 3个月后孕育第2环【地肺火髓】",
                            CostSpiritStone = 65, BaseSuccessRate = 1.0, SuccessOre = 35,
                            NextChainEventId = "ENC_CHAIN_VEIN_2", NextChainDelayMonths = 3,
                            SuccessOutcomeText = "锁火阵稳稳束缚地肺炎流，矿窟深处隐有赤晶凝结之兆！"
                        },
                        new EncounterOptionConfig
                        {
                            OptionTitle = "引北溟寒泉封堵火脉", RiskTag = "100%无耗", PreviewGainText = "玄铁+50 / 灵石+70 / 北溟玄宫好感+10",
                            BaseSuccessRate = 1.0, SuccessOre = 50, SuccessStone = 70,
                            TargetFactionId = "FAC_BEIMING", SuccessRelationDelta = 10,
                            SuccessOutcomeText = "水火相激化作大块百炼玄铁，后山隐患彻底平息。"
                        }
                    }
                },
                new EncounterEventConfig
                {
                    Id = "ENC_CHAIN_VEIN_2", TitleCn = "地肺火髓凝结成晶", TriggerTypeKey = "ChainFollowup",
                    MinYear = 1, Weight = 0, ChainStep = 2, ChainTotal = 2,
                    NarrativeDesc = "历经三月地脉温养，锁火大阵中央已凝成一团炽烈夺目的【赤阳地火髓】，既可吞纳淬体，亦可入炉铸宝！",
                    Options = new List<EncounterOptionConfig>
                    {
                        new EncounterOptionConfig
                        {
                            OptionTitle = "掌门入阵吞纳火髓洗髓", RiskTag = "成率76%", PreviewGainText = "掌门修为+580 / 全宗攻伐+12%(12月)",
                            BaseSuccessRate = 0.76, SuccessQiGrant = 580,
                            SuccessBuffName = "赤阳真火淬体", SuccessBuffMonths = 12, SuccessTempAtkAdd = 0.12,
                            SuccessOutcomeText = "地火真精贯入四肢百骸，掌门真元暴涨且剑气附带炽烈锋芒！",
                            FailHpLossRatio = 0.25, FailOutcomeText = "火髓燥烈难驯灼伤经络，幸得护体真元及时宣泄余热。"
                        },
                        new EncounterOptionConfig
                        {
                            OptionTitle = "送入百炼器坊铸造阵宝", RiskTag = "100%必成", PreviewGainText = "赐2阶词条阵宝×1 / 妖丹+3",
                            BaseSuccessRate = 1.0, SuccessArtifactTier = 2, SuccessCore = 3,
                            SuccessOutcomeText = "器坊炉火冲霄三日不绝，顺利铸成一件火灵充沛的二阶词条法宝！"
                        }
                    }
                },

                // ==================== 常驻随机演化奇遇 ====================
                new EncounterEventConfig
                {
                    Id = "ENC_RND_MERCHANT", TitleCn = "天机行商秘匣奇珍", TriggerTypeKey = "RandomMonthly",
                    MinYear = 1, Weight = 15.0, ChainStep = 1, ChainTotal = 1,
                    NarrativeDesc = "天机商盟一支驼铃灵舟途经山门避风，管事呈上一只封印斑驳的古修石匣与数批急售丹药。",
                    Options = new List<EncounterOptionConfig>
                    {
                        new EncounterOptionConfig
                        {
                            OptionTitle = "出资盲开古修封印石匣", RiskTag = "博弈·耗110灵石·成率74%", PreviewGainText = "赐2阶词条阵宝×1 / 培元丹×1",
                            CostSpiritStone = 110, BaseSuccessRate = 0.74, SuccessArtifactTier = 2, SuccessItemId = "ITEM_JIUZHUAN_PILL", SuccessItemQty = 1,
                            TargetFactionId = "FAC_TIANJI", SuccessRelationDelta = 10,
                            SuccessOutcomeText = "石匣禁制应声而解，内藏一件完好古宝与一瓶九转培元丹，大赚特赚！",
                            FailHpLossRatio = 0.0, FailOutcomeText = "石匣开启后灵气早已流失大半，仅余几块寻常碎玉。"
                        },
                        new EncounterOptionConfig
                        {
                            OptionTitle = "以富余灵草置换急需灵石", RiskTag = "稳妥·耗30灵草", PreviewGainText = "灵石+115 / 聚气丹×2",
                            CostHerb = 30, BaseSuccessRate = 1.0, SuccessStone = 115, SuccessItemId = "ITEM_JUQI_PILL", SuccessItemQty = 2,
                            SuccessOutcomeText = "行商正缺百草补给，爽快奉上高额灵石与两枚聚气丹。"
                        },
                        new EncounterOptionConfig
                        {
                            OptionTitle = "灵茶款待探听九州商情", RiskTag = "100%无耗", PreviewGainText = "天机商盟好感+12 / 声望+15",
                            BaseSuccessRate = 1.0, SuccessReputation = 15, TargetFactionId = "FAC_TIANJI", SuccessRelationDelta = 12,
                            SuccessOutcomeText = "管事感念礼遇，临别详告近期各大宗门资材缺口动向。"
                        }
                    }
                },
                new EncounterEventConfig
                {
                    Id = "ENC_RND_SECT_DISPUTE", TitleCn = "边界伴生灵矿争端", TriggerTypeKey = "RandomMonthly",
                    MinYear = 2, Weight = 13.0, ChainStep = 1, ChainTotal = 1,
                    NarrativeDesc = "苍梧山脉交界新现一处露天伴生灵矿，青云剑派巡山弟子正与血影魔宗斥候拔剑对峙，形势一触即发。",
                    Options = new List<EncounterOptionConfig>
                    {
                        new EncounterOptionConfig
                        {
                            OptionTitle = "率剑卫驰援青云震慑魔修", RiskTag = "成率78%", PreviewGainText = "灵石+150 / 玄铁+55 / 青云好感+18 / 声望+28",
                            BaseSuccessRate = 0.78, SuccessStone = 150, SuccessOre = 55, SuccessReputation = 28,
                            TargetFactionId = "FAC_QINGYUN", SuccessRelationDelta = 18,
                            SuccessOutcomeText = "两宗剑气合璧惊退魔宗斥候，青云长老分予本宗半数伴生灵矿！",
                            FailHpLossRatio = 0.22, FailOutcomeText = "魔修埋伏阴毒煞雷，掌门护住门人全身而退但略受震荡。"
                        },
                        new EncounterOptionConfig
                        {
                            OptionTitle = "遣精锐绕后暗采核心妖晶", RiskTag = "奇袭·成率70%", PreviewGainText = "灵石+190 / 妖丹+4",
                            BaseSuccessRate = 0.70, SuccessStone = 190, SuccessCore = 4,
                            SuccessOutcomeText = "趁两派僵持之际悄然掘走矿心伴生妖晶，神不知鬼不觉！",
                            FailHpLossRatio = 0.16, FailOutcomeText = "矿脉余波震塌甬道，未能深入矿心便匆匆撤回。"
                        },
                        new EncounterOptionConfig
                        {
                            OptionTitle = "加固护山阵严守中立边界", RiskTag = "100%稳妥", PreviewGainText = "掌门修为+320 / 声望+12",
                            BaseSuccessRate = 1.0, SuccessQiGrant = 320, SuccessReputation = 12,
                            SuccessOutcomeText = "山门清静无虞，趁外宗纷争潜心吐纳精进修为。"
                        }
                    }
                }
            };
        }

        private static List<AcupointNodeConfig> CreateDefaultAcupoints()
        {
            return new List<AcupointNodeConfig>
            {
                // 1. 督脉·乾阳（6穴：主攻伐与功法倍率乘区）
                new AcupointNodeConfig { Id = "ACU_DU_1", NameCn = "长强穴", ShortName = "1.长强", BranchKey = "DuMai", OrderIndex = 1, PrerequisiteId = "", MinRealmKey = "QiCondensation", CostQi = 160, CostSpiritStone = 45, CostHerb = 12, CostMonsterCore = 0, BaseSuccessRate = 0.90, FlatAtk = 12, TechMultAdd = 0.03, Desc = "督脉起穴，贯通尾闾真阳，强化杀伐锋芒与主修功法运转。" },
                new AcupointNodeConfig { Id = "ACU_DU_2", NameCn = "命门穴", ShortName = "2.命门", BranchKey = "DuMai", OrderIndex = 2, PrerequisiteId = "ACU_DU_1", MinRealmKey = "QiCondensation", CostQi = 320, CostSpiritStone = 85, CostHerb = 22, CostMonsterCore = 1, BaseSuccessRate = 0.82, FlatAtk = 20, FlatHp = 60, TechMultAdd = 0.04, Desc = "生命真火所系，培固元阳并放大主修功法威能。" },
                new AcupointNodeConfig { Id = "ACU_DU_3", NameCn = "至阳穴", ShortName = "3.至阳", BranchKey = "DuMai", OrderIndex = 3, PrerequisiteId = "ACU_DU_2", MinRealmKey = "Foundation", CostQi = 850, CostSpiritStone = 180, CostHerb = 40, CostMonsterCore = 2, BaseSuccessRate = 0.74, FlatAtk = 38, TechMultAdd = 0.05, Desc = "背脊纯阳枢纽，剑气法诀运转无碍。" },
                new AcupointNodeConfig { Id = "ACU_DU_4", NameCn = "大椎穴", ShortName = "4.大椎", BranchKey = "DuMai", OrderIndex = 4, PrerequisiteId = "ACU_DU_3", MinRealmKey = "Foundation", CostQi = 1600, CostSpiritStone = 320, CostHerb = 65, CostMonsterCore = 4, BaseSuccessRate = 0.66, FlatAtk = 62, FlatDef = 18, TechMultAdd = 0.06, Desc = "诸阳之会，统摄周身阳脉，显著拔高功法乘区。" },
                new AcupointNodeConfig { Id = "ACU_DU_5", NameCn = "玉枕穴", ShortName = "5.玉枕", BranchKey = "DuMai", OrderIndex = 5, PrerequisiteId = "ACU_DU_4", MinRealmKey = "GoldenCore", CostQi = 4200, CostSpiritStone = 650, CostHerb = 110, CostMonsterCore = 8, BaseSuccessRate = 0.58, FlatAtk = 105, FlatMp = 90, TechMultAdd = 0.07, Desc = "后脑天关险隘，冲破后神识清明、道法倍增。" },
                new AcupointNodeConfig { Id = "ACU_DU_6", NameCn = "百会穴", ShortName = "6.百会", BranchKey = "DuMai", OrderIndex = 6, PrerequisiteId = "ACU_DU_5", MinRealmKey = "NascentSoul", CostQi = 12000, CostSpiritStone = 1400, CostHerb = 220, CostMonsterCore = 15, BaseSuccessRate = 0.50, FlatAtk = 180, FlatMp = 180, TechMultAdd = 0.10, Desc = "顶门天灵极窍，一穴开而督脉全通，直引九天罡气入体。" },

                // 2. 任脉·坤阴（6穴：主气海吐纳、法力与寿元）
                new AcupointNodeConfig { Id = "ACU_REN_1", NameCn = "会阴穴", ShortName = "1.会阴", BranchKey = "RenMai", OrderIndex = 1, PrerequisiteId = "", MinRealmKey = "QiCondensation", CostQi = 150, CostSpiritStone = 40, CostHerb = 14, CostMonsterCore = 0, BaseSuccessRate = 0.90, FlatMp = 35, QiRatePctAdd = 0.06, LifespanAdd = 8, Desc = "任脉海底源头，聚敛阴灵清气，加快日常打坐吐纳。" },
                new AcupointNodeConfig { Id = "ACU_REN_2", NameCn = "关元穴", ShortName = "2.关元", BranchKey = "RenMai", OrderIndex = 2, PrerequisiteId = "ACU_REN_1", MinRealmKey = "QiCondensation", CostQi = 300, CostSpiritStone = 80, CostHerb = 24, CostMonsterCore = 1, BaseSuccessRate = 0.82, FlatHp = 80, QiRatePctAdd = 0.08, LifespanAdd = 12, Desc = "玄关藏元之所，固本培元，延年益寿。" },
                new AcupointNodeConfig { Id = "ACU_REN_3", NameCn = "气海穴", ShortName = "3.气海", BranchKey = "RenMai", OrderIndex = 3, PrerequisiteId = "ACU_REN_2", MinRealmKey = "Foundation", CostQi = 800, CostSpiritStone = 170, CostHerb = 45, CostMonsterCore = 2, BaseSuccessRate = 0.74, FlatMp = 80, QiRatePctAdd = 0.10, TechMultAdd = 0.03, LifespanAdd = 18, Desc = "丹田真气生发之海，吞吐天地灵机效率大增。" },
                new AcupointNodeConfig { Id = "ACU_REN_4", NameCn = "膻中穴", ShortName = "4.膻中", BranchKey = "RenMai", OrderIndex = 4, PrerequisiteId = "ACU_REN_3", MinRealmKey = "Foundation", CostQi = 1500, CostSpiritStone = 300, CostHerb = 70, CostMonsterCore = 4, BaseSuccessRate = 0.66, FlatMp = 130, QiRatePctAdd = 0.12, LifespanAdd = 25, Desc = "中丹田宗气枢机，气机流转绵密不绝。" },
                new AcupointNodeConfig { Id = "ACU_REN_5", NameCn = "天突穴", ShortName = "5.天突", BranchKey = "RenMai", OrderIndex = 5, PrerequisiteId = "ACU_REN_4", MinRealmKey = "GoldenCore", CostQi = 4000, CostSpiritStone = 620, CostHerb = 120, CostMonsterCore = 8, BaseSuccessRate = 0.58, FlatMp = 200, QiRatePctAdd = 0.15, TechMultAdd = 0.04, LifespanAdd = 35, Desc = "咽喉十二重楼要冲，吐故纳新如鲸吞灵雾。" },
                new AcupointNodeConfig { Id = "ACU_REN_6", NameCn = "承浆穴", ShortName = "6.承浆", BranchKey = "RenMai", OrderIndex = 6, PrerequisiteId = "ACU_REN_5", MinRealmKey = "NascentSoul", CostQi = 11500, CostSpiritStone = 1350, CostHerb = 240, CostMonsterCore = 15, BaseSuccessRate = 0.50, FlatMp = 320, QiRatePctAdd = 0.20, LifespanAdd = 60, Desc = "玉池琼浆玄窍，与督脉百会相接即可结成任督小周天。" },

                // 3. 冲脉·血海（6穴：主气血上限与护体防御）
                new AcupointNodeConfig { Id = "ACU_CHONG_1", NameCn = "气冲穴", ShortName = "1.气冲", BranchKey = "ChongMai", OrderIndex = 1, PrerequisiteId = "", MinRealmKey = "QiCondensation", CostQi = 160, CostSpiritStone = 45, CostHerb = 15, CostMonsterCore = 0, BaseSuccessRate = 0.88, FlatHp = 110, FlatDef = 6, Desc = "十二经血海之始，气血充盈，强化道躯抗击能力。" },
                new AcupointNodeConfig { Id = "ACU_CHONG_2", NameCn = "大赫穴", ShortName = "2.大赫", BranchKey = "ChongMai", OrderIndex = 2, PrerequisiteId = "ACU_CHONG_1", MinRealmKey = "QiCondensation", CostQi = 340, CostSpiritStone = 90, CostHerb = 26, CostMonsterCore = 1, BaseSuccessRate = 0.80, FlatHp = 190, FlatDef = 12, Desc = "精血赫盛，护体罡气愈发凝实。" },
                new AcupointNodeConfig { Id = "ACU_CHONG_3", NameCn = "商曲穴", ShortName = "3.商曲", BranchKey = "ChongMai", OrderIndex = 3, PrerequisiteId = "ACU_CHONG_2", MinRealmKey = "Foundation", CostQi = 900, CostSpiritStone = 190, CostHerb = 48, CostMonsterCore = 2, BaseSuccessRate = 0.72, FlatHp = 340, FlatDef = 22, Desc = "中焦血脉盘曲之窍，受创恢复与耐战力显著增强。" },
                new AcupointNodeConfig { Id = "ACU_CHONG_4", NameCn = "幽门穴", ShortName = "4.幽门", BranchKey = "ChongMai", OrderIndex = 4, PrerequisiteId = "ACU_CHONG_3", MinRealmKey = "Foundation", CostQi = 1700, CostSpiritStone = 340, CostHerb = 75, CostMonsterCore = 4, BaseSuccessRate = 0.64, FlatHp = 560, FlatDef = 36, Desc = "冲脉入胸要户，稳固五脏心神不惧妖王凶威。" },
                new AcupointNodeConfig { Id = "ACU_CHONG_5", NameCn = "通谷穴", ShortName = "5.通谷", BranchKey = "ChongMai", OrderIndex = 5, PrerequisiteId = "ACU_CHONG_4", MinRealmKey = "GoldenCore", CostQi = 4500, CostSpiritStone = 680, CostHerb = 130, CostMonsterCore = 8, BaseSuccessRate = 0.56, FlatHp = 950, FlatDef = 64, TechMultAdd = 0.04, Desc = "辟谷纳气、肉身渐趋金刚不坏。" },
                new AcupointNodeConfig { Id = "ACU_CHONG_6", NameCn = "阴交穴", ShortName = "6.阴交", BranchKey = "ChongMai", OrderIndex = 6, PrerequisiteId = "ACU_CHONG_5", MinRealmKey = "NascentSoul", CostQi = 12500, CostSpiritStone = 1450, CostHerb = 250, CostMonsterCore = 16, BaseSuccessRate = 0.48, FlatHp = 1650, FlatDef = 115, LifespanAdd = 45, Desc = "诸阴交汇总枢，冲脉大成后可在斗法中汲取生机。" },

                // 4. 带脉·天枢（6穴：主身法速度、暴击闪避与大境界渡劫率）
                new AcupointNodeConfig { Id = "ACU_DAI_1", NameCn = "带脉穴", ShortName = "1.带脉", BranchKey = "DaiMai", OrderIndex = 1, PrerequisiteId = "", MinRealmKey = "QiCondensation", CostQi = 170, CostSpiritStone = 50, CostHerb = 12, CostMonsterCore = 0, BaseSuccessRate = 0.88, FlatSpeed = 5, DodgeRateAdd = 0.015, Desc = "环腰束纵诸经，身法轻灵，抢占斗法先手。" },
                new AcupointNodeConfig { Id = "ACU_DAI_2", NameCn = "五枢穴", ShortName = "2.五枢", BranchKey = "DaiMai", OrderIndex = 2, PrerequisiteId = "ACU_DAI_1", MinRealmKey = "QiCondensation", CostQi = 350, CostSpiritStone = 95, CostHerb = 24, CostMonsterCore = 1, BaseSuccessRate = 0.80, FlatSpeed = 7, CritRateAdd = 0.02, BreakthroughRateAdd = 0.02, Desc = "五气灵枢流转，敏锐洞察敌阵破绽并稳固破境心境。" },
                new AcupointNodeConfig { Id = "ACU_DAI_3", NameCn = "维道穴", ShortName = "3.维道", BranchKey = "DaiMai", OrderIndex = 3, PrerequisiteId = "ACU_DAI_2", MinRealmKey = "Foundation", CostQi = 920, CostSpiritStone = 200, CostHerb = 46, CostMonsterCore = 2, BaseSuccessRate = 0.72, FlatSpeed = 9, DodgeRateAdd = 0.025, BreakthroughRateAdd = 0.025, Desc = "维系阴阳气机不散，显著降低渡劫真元走岔风险。" },
                new AcupointNodeConfig { Id = "ACU_DAI_4", NameCn = "居髎穴", ShortName = "4.居髎", BranchKey = "DaiMai", OrderIndex = 4, PrerequisiteId = "ACU_DAI_3", MinRealmKey = "Foundation", CostQi = 1750, CostSpiritStone = 350, CostHerb = 72, CostMonsterCore = 4, BaseSuccessRate = 0.64, FlatSpeed = 12, CritRateAdd = 0.03, DodgeRateAdd = 0.02, Desc = "步罡踏斗枢窍，身如惊鸿，剑招暴击倍增。" },
                new AcupointNodeConfig { Id = "ACU_DAI_5", NameCn = "章门穴", ShortName = "5.章门", BranchKey = "DaiMai", OrderIndex = 5, PrerequisiteId = "ACU_DAI_4", MinRealmKey = "GoldenCore", CostQi = 4600, CostSpiritStone = 700, CostHerb = 125, CostMonsterCore = 9, BaseSuccessRate = 0.56, FlatSpeed = 15, CritRateAdd = 0.035, BreakthroughRateAdd = 0.035, Desc = "脏会章门，收束周天万象，渡劫抗雷威能大涨。" },
                new AcupointNodeConfig { Id = "ACU_DAI_6", NameCn = "期门穴", ShortName = "6.期门", BranchKey = "DaiMai", OrderIndex = 6, PrerequisiteId = "ACU_DAI_5", MinRealmKey = "NascentSoul", CostQi = 13000, CostSpiritStone = 1500, CostHerb = 260, CostMonsterCore = 16, BaseSuccessRate = 0.48, FlatSpeed = 20, CritRateAdd = 0.045, DodgeRateAdd = 0.035, BreakthroughRateAdd = 0.05, Desc = "十二经循环终始之门，打通后天人交感，仙途坦荡。" }
            };
        }

        private static List<ArtifactTemplateConfig> CreateDefaultArtifactTemplates()
        {
            return new List<ArtifactTemplateConfig>
            {
                new ArtifactTemplateConfig { Id = "ART_TPL_METAL_BELL", NameCn = "太白庚金钟", ShortName = "庚金钟", ElementKey = "Metal", Tier = 1, BaseAtk = 15, BaseDef = 8, BaseHp = 40, Desc = "庚金精铁铸就的镇阵古钟，兼具锋锐与护体。" },
                new ArtifactTemplateConfig { Id = "ART_TPL_WOOD_LOTUS", NameCn = "青帝碧玉莲", ShortName = "碧玉莲", ElementKey = "Wood", Tier = 1, BaseAtk = 8, BaseDef = 6, BaseHp = 135, Desc = "蕴含生生造化木灵之气，温养全宗修士气血。" },
                new ArtifactTemplateConfig { Id = "ART_TPL_WATER_PEARL", NameCn = "北溟玄水珠", ShortName = "玄水珠", ElementKey = "Water", Tier = 2, BaseAtk = 24, BaseDef = 14, BaseHp = 185, Desc = "深海寒髓凝结之宝珠，镇压水脉可大幅稳固阵基。" },
                new ArtifactTemplateConfig { Id = "ART_TPL_FIRE_SEAL", NameCn = "离火焚天印", ShortName = "焚天印", ElementKey = "Fire", Tier = 2, BaseAtk = 42, BaseDef = 10, BaseHp = 110, Desc = "引地肺真火入印，杀伐凌厉，极契离宫与生土阵位。" },
                new ArtifactTemplateConfig { Id = "ART_TPL_EARTH_CAULDRON", NameCn = "玄黄镇岳鼎", ShortName = "镇岳鼎", ElementKey = "Earth", Tier = 3, BaseAtk = 26, BaseDef = 38, BaseHp = 380, Desc = "厚土玄黄重器，镇守中宫天枢或四隅土位稳若泰山。" },
                new ArtifactTemplateConfig { Id = "ART_TPL_YINYANG_MIRROR", NameCn = "太虚阴阳镜", ShortName = "阴阳镜", ElementKey = "Metal", Tier = 4, BaseAtk = 78, BaseDef = 42, BaseHp = 460, Desc = "上古洞天传承至宝，照彻乾坤，攻守兼备。" }
            };
        }

        public static SIGConfigRoot CreateDefaultConfig()
        {
            var cfg = new SIGConfigRoot();
            cfg.Balance = new GlobalBalanceConfig
            {
                SecondsPerSIGMonth = 20.0,
                MaxOfflineHours = 24.0,
                BaseDiscipleCap = 3,
                WuXingGenerateBonus = 0.25,
                WuXingSameBonus = 0.12,
                WuXingOvercomePenalty = -0.15,
                RaidIntervalMonths = 12,
                DiscipleMonthlyFoodCost = 5.0,
                RecruitBaseStoneCost = 120.0,
                UnlockSlotBaseStoneCost = 140.0
            };

            cfg.Realms = new List<RealmConfig>
            {
                new RealmConfig { RealmKey = "Mortal", NameCn = "凡胎境", MaxSubStage = 3, BaseMaxQi = 180, SubStageQiGrowth = 1.45, BaseQiPerSec = 3.2, BaseHp = 120, BaseMp = 50, BaseAtk = 16, BaseDef = 6, BaseLifespanYears = 85, BreakthroughBaseRate = 0.90, RecommendedPillId = "" },
                new RealmConfig { RealmKey = "QiCondensation", NameCn = "炼气期", MaxSubStage = 4, BaseMaxQi = 550, SubStageQiGrowth = 1.55, BaseQiPerSec = 6.0, BaseHp = 260, BaseMp = 110, BaseAtk = 34, BaseDef = 14, BaseLifespanYears = 130, BreakthroughBaseRate = 0.72, RecommendedPillId = "ITEM_ZHUJI_PILL" },
                new RealmConfig { RealmKey = "Foundation", NameCn = "筑基期", MaxSubStage = 4, BaseMaxQi = 2400, SubStageQiGrowth = 1.60, BaseQiPerSec = 16.0, BaseHp = 680, BaseMp = 240, BaseAtk = 82, BaseDef = 36, BaseLifespanYears = 220, BreakthroughBaseRate = 0.58, RecommendedPillId = "ITEM_JIEDAN_PILL" },
                new RealmConfig { RealmKey = "GoldenCore", NameCn = "金丹期", MaxSubStage = 4, BaseMaxQi = 11000, SubStageQiGrowth = 1.65, BaseQiPerSec = 45.0, BaseHp = 1850, BaseMp = 520, BaseAtk = 210, BaseDef = 92, BaseLifespanYears = 450, BreakthroughBaseRate = 0.45, RecommendedPillId = "ITEM_NINGYING_PILL" },
                new RealmConfig { RealmKey = "NascentSoul", NameCn = "元婴期", MaxSubStage = 4, BaseMaxQi = 52000, SubStageQiGrowth = 1.70, BaseQiPerSec = 135.0, BaseHp = 4800, BaseMp = 1100, BaseAtk = 520, BaseDef = 230, BaseLifespanYears = 1000, BreakthroughBaseRate = 0.35, RecommendedPillId = "ITEM_NINGYING_PILL" },
                new RealmConfig { RealmKey = "SpiritSevering", NameCn = "化神期", MaxSubStage = 4, BaseMaxQi = 240000, SubStageQiGrowth = 1.75, BaseQiPerSec = 420.0, BaseHp = 12500, BaseMp = 2400, BaseAtk = 1280, BaseDef = 580, BaseLifespanYears = 2500, BreakthroughBaseRate = 0.28, RecommendedPillId = "ITEM_NINGYING_PILL" },
                new RealmConfig { RealmKey = "Tribulation", NameCn = "渡劫期", MaxSubStage = 4, BaseMaxQi = 1100000, SubStageQiGrowth = 1.80, BaseQiPerSec = 1300.0, BaseHp = 32000, BaseMp = 5200, BaseAtk = 3100, BaseDef = 1400, BaseLifespanYears = 6000, BreakthroughBaseRate = 0.22, RecommendedPillId = "ITEM_NINGYING_PILL" },
                new RealmConfig { RealmKey = "Immortal", NameCn = "真仙境", MaxSubStage = 1, BaseMaxQi = 9999999, SubStageQiGrowth = 1.0, BaseQiPerSec = 4500.0, BaseHp = 88000, BaseMp = 12000, BaseAtk = 8500, BaseDef = 3800, BaseLifespanYears = 99999, BreakthroughBaseRate = 1.0, RecommendedPillId = "" }
            };

            cfg.Buildings = new List<BuildingConfig>
            {
                new BuildingConfig { Id = "BLD_HALL", NameCn = "掌门大殿", ShortName = "掌门殿", CategoryKey = "Core", ElementKey = "Earth", MaxLevel = 6, MaxCount = 1, MinSectLevel = 1, CostSpiritStone = 100, CostWood = 80, CostOre = 60, BuildTimeSec = 10, ProduceResourceKey = "SpiritStone", BaseOutputPerSec = 0.8, SecondaryResourceKey = "SectReputation", SecondaryOutputPerSec = 0.08, AuraBonusRate = 0.08, DiscipleCapBonus = 1, DefenseBarrierBonus = 250, Desc = "宗门气运中枢。升阶提高宗门等级、门人容量、灵石供奉与大阵上限。" },
                new BuildingConfig { Id = "BLD_FARM", NameCn = "神农灵田", ShortName = "神农田", CategoryKey = "Gather", ElementKey = "Wood", MaxLevel = 6, MaxCount = 4, MinSectLevel = 1, CostSpiritStone = 40, CostWood = 35, CostOre = 10, BuildTimeSec = 6, ProduceResourceKey = "SpiritFood", BaseOutputPerSec = 1.1, SecondaryResourceKey = "SpiritHerb", SecondaryOutputPerSec = 0.45, Desc = "培育金穗灵谷与百草灵叶，保障全宗口粮与炼丹药材供给。" },
                new BuildingConfig { Id = "BLD_LUMBER", NameCn = "青木灵林", ShortName = "青木林", CategoryKey = "Gather", ElementKey = "Wood", MaxLevel = 6, MaxCount = 3, MinSectLevel = 1, CostSpiritStone = 45, CostWood = 20, CostOre = 25, BuildTimeSec = 6, ProduceResourceKey = "SpiritWood", BaseOutputPerSec = 1.0, Desc = "采伐铁桦灵木，提供宗门扩建与木系法器精材。" },
                new BuildingConfig { Id = "BLD_MINE", NameCn = "玄铁灵矿", ShortName = "玄铁矿", CategoryKey = "Gather", ElementKey = "Metal", MaxLevel = 6, MaxCount = 3, MinSectLevel = 1, CostSpiritStone = 55, CostWood = 45, CostOre = 15, BuildTimeSec = 7, ProduceResourceKey = "SpiritOre", BaseOutputPerSec = 0.85, SecondaryResourceKey = "SpiritStone", SecondaryOutputPerSec = 0.45, Desc = "勘探地脉矿髓，持续产出玄铁矿与伴生灵石。" },
                new BuildingConfig { Id = "BLD_QUARTERS", NameCn = "弟子洞府", ShortName = "弟子府", CategoryKey = "Living", ElementKey = "Water", MaxLevel = 5, MaxCount = 4, MinSectLevel = 1, CostSpiritStone = 70, CostWood = 60, CostOre = 40, BuildTimeSec = 8, AuraBonusRate = 0.04, DiscipleCapBonus = 2, Desc = "门人清修静室，每座及每次升阶均可扩充门人弟子招募上限。" },
                new BuildingConfig { Id = "BLD_ARRAY", NameCn = "九曲聚灵阵", ShortName = "聚灵阵", CategoryKey = "Cultivation", ElementKey = "Water", MaxLevel = 5, MaxCount = 3, MinSectLevel = 1, CostSpiritStone = 120, CostWood = 50, CostOre = 70, BuildTimeSec = 10, AuraBonusRate = 0.18, Desc = "汇聚天地灵机，大幅提升掌门与全宗弟子的吐纳修为速率。" },
                new BuildingConfig { Id = "BLD_ALCHEMY", NameCn = "九转丹房", ShortName = "九转丹房", CategoryKey = "Craft", ElementKey = "Fire", MaxLevel = 5, MaxCount = 2, MinSectLevel = 1, CostSpiritStone = 110, CostWood = 70, CostOre = 80, BuildTimeSec = 11, Desc = "引地火温养丹炉，挂机自动炼制修为灵丹、破境宝丹与战时回春丹。" },
                new BuildingConfig { Id = "BLD_FORGE", NameCn = "百炼器坊", ShortName = "百炼器坊", CategoryKey = "Craft", ElementKey = "Fire", MaxLevel = 5, MaxCount = 2, MinSectLevel = 1, CostSpiritStone = 120, CostWood = 65, CostOre = 95, BuildTimeSec = 12, Desc = "锻造飞剑、宝甲与随机词条阵宝，显著强化门人斗法战力。" },
                new BuildingConfig { Id = "BLD_TOWER", NameCn = "护山诛邪剑台", ShortName = "诛邪剑台", CategoryKey = "Defense", ElementKey = "Metal", MaxLevel = 5, MaxCount = 4, MinSectLevel = 1, CostSpiritStone = 95, CostWood = 55, CostOre = 85, BuildTimeSec = 9, DefenseBarrierBonus = 320, Desc = "镇守山门要冲，提升护山大阵结界耐久并在兽潮防卫战中齐射御敌。" }
            };

            cfg.Items = new List<ItemConfig>
            {
                new ItemConfig { Id = "ITEM_JUQI_PILL", NameCn = "聚气丹", ShortName = "聚气丹", CategoryKey = "CultivationPill", ElementKey = "Wood", MaxStack = 999, PriceSpiritStone = 25, QiGrant = 280, Desc = "服用后立即精进 280 点气海修为。" },
                new ItemConfig { Id = "ITEM_JIUZHUAN_PILL", NameCn = "九转培元丹", ShortName = "培元丹", CategoryKey = "CultivationPill", ElementKey = "Fire", MaxStack = 999, PriceSpiritStone = 120, QiGrant = 1600, Desc = "高阶灵丹，服用后立即精进 1600 点气海修为。" },
                new ItemConfig { Id = "ITEM_ZHUJI_PILL", NameCn = "筑基灵丹", ShortName = "筑基丹", CategoryKey = "BreakthroughPill", ElementKey = "Wood", MaxStack = 99, PriceSpiritStone = 180, BreakthroughRateBonus = 0.25, Desc = "冲击筑基期时自动消耗1枚，突破成功率 +25%。" },
                new ItemConfig { Id = "ITEM_JIEDAN_PILL", NameCn = "紫金结丹丸", ShortName = "结丹丸", CategoryKey = "BreakthroughPill", ElementKey = "Fire", MaxStack = 99, PriceSpiritStone = 480, BreakthroughRateBonus = 0.28, Desc = "冲击金丹期时自动消耗1枚，突破成功率 +28%。" },
                new ItemConfig { Id = "ITEM_NINGYING_PILL", NameCn = "九天凝婴丹", ShortName = "凝婴丹", CategoryKey = "BreakthroughPill", ElementKey = "Water", MaxStack = 99, PriceSpiritStone = 1200, BreakthroughRateBonus = 0.30, Desc = "冲击元婴及更高境界时自动消耗1枚，突破成功率 +30%。" },
                new ItemConfig { Id = "ITEM_HEAL_PILL_1", NameCn = "回春丹", ShortName = "回春丹", CategoryKey = "CombatPill", ElementKey = "Wood", MaxStack = 999, PriceSpiritStone = 20, HealHpRatio = 0.42, RestoreMpRatio = 0.15, Desc = "瞬间恢复 42% 最大气血与 15% 法力（可绑定修行或战时快捷栏）。" },
                new ItemConfig { Id = "ITEM_HEAL_PILL_2", NameCn = "大还金丹", ShortName = "大还丹", CategoryKey = "CombatPill", ElementKey = "Water", MaxStack = 999, PriceSpiritStone = 85, HealHpRatio = 0.70, RestoreMpRatio = 0.35, Desc = "高阶救命宝丹，瞬间恢复 70% 最大气血与 35% 法力。" },

                new ItemConfig { Id = "EQ_SWORD_1", NameCn = "青锋灵剑", ShortName = "青锋剑", CategoryKey = "Weapon", ElementKey = "Metal", MaxStack = 99, PriceSpiritStone = 90, AtkBonus = 22, SpeedBonus = 6, Desc = "一阶百炼飞剑，攻击 +22，速度 +6。" },
                new ItemConfig { Id = "EQ_SWORD_2", NameCn = "赤炎离火剑", ShortName = "离火剑", CategoryKey = "Weapon", ElementKey = "Fire", MaxStack = 99, PriceSpiritStone = 280, AtkBonus = 68, SpeedBonus = 12, Desc = "二阶地火神兵，攻击 +68，速度 +12。" },
                new ItemConfig { Id = "EQ_SWORD_3", NameCn = "太虚星辰剑", ShortName = "太虚剑", CategoryKey = "Weapon", ElementKey = "Metal", MaxStack = 99, PriceSpiritStone = 900, AtkBonus = 195, SpeedBonus = 22, Desc = "三阶镇宗仙剑，攻击 +195，速度 +22。" },

                new ItemConfig { Id = "EQ_ARMOR_1", NameCn = "灵丝道袍", ShortName = "灵丝袍", CategoryKey = "Armor", ElementKey = "Wood", MaxStack = 99, PriceSpiritStone = 85, DefBonus = 14, HpBonus = 140, Desc = "一阶护身法衣，防御 +14，气血上限 +140。" },
                new ItemConfig { Id = "EQ_ARMOR_2", NameCn = "玄铁重甲", ShortName = "玄铁甲", CategoryKey = "Armor", ElementKey = "Earth", MaxStack = 99, PriceSpiritStone = 260, DefBonus = 45, HpBonus = 420, Desc = "二阶玄铁宝甲，防御 +45，气血上限 +420。" },

                new ItemConfig { Id = "TAL_ICE_BLADE", NameCn = "玄冰剑符", ShortName = "玄冰符", CategoryKey = "Talisman", ElementKey = "Water", MaxStack = 99, PriceSpiritStone = 160, AtkBonus = 12, ActiveSkillName = "玄冰斩", ActiveSkillMultiplier = 1.85, ActiveSkillMpCost = 28, CooldownTurns = 2, Desc = "主动法宝：消耗28法力施展【玄冰斩】，造成 185% 水系剑气伤害。" },
                new ItemConfig { Id = "TAL_TURTLE_SHIELD", NameCn = "八卦玄龟甲", ShortName = "玄龟甲", CategoryKey = "Talisman", ElementKey = "Earth", MaxStack = 99, PriceSpiritStone = 220, DefBonus = 18, ActiveSkillName = "玄龟护体", ActiveSkillMultiplier = 1.10, ActiveSkillMpCost = 35, ShieldMaxHpRatio = 0.35, CooldownTurns = 3, Desc = "主动法宝：消耗35法力凝结 35% 最大气血护盾并震击目标。" },
                new ItemConfig { Id = "TAL_FIRE_FAN", NameCn = "五火七禽扇", ShortName = "七禽扇", CategoryKey = "Talisman", ElementKey = "Fire", MaxStack = 99, PriceSpiritStone = 520, AtkBonus = 48, ActiveSkillName = "五火焚天", ActiveSkillMultiplier = 2.45, ActiveSkillMpCost = 55, CooldownTurns = 3, Desc = "高阶法宝：消耗55法力席卷烈焰，造成 245% 毁灭性火系伤害。" }
            };

            cfg.Recipes = new List<RecipeConfig>
            {
                new RecipeConfig { Id = "RCP_JUQI", NameCn = "炼制·聚气丹", ShortName = "聚气丹方", MaxMasteryLv = 5, RequiredBuildingId = "BLD_ALCHEMY", MinBuildingLevel = 1, OutputItemId = "ITEM_JUQI_PILL", OutputCount = 2, CraftTimeSec = 10, CostSpiritStone = 12, CostHerb = 8, BaseSuccessRate = 0.92, Desc = "消耗灵草与灵石，每炉成丹2枚聚气丹。" },
                new RecipeConfig { Id = "RCP_HEAL_1", NameCn = "炼制·回春丹", ShortName = "回春丹方", MaxMasteryLv = 5, RequiredBuildingId = "BLD_ALCHEMY", MinBuildingLevel = 1, OutputItemId = "ITEM_HEAL_PILL_1", OutputCount = 2, CraftTimeSec = 9, CostSpiritStone = 10, CostHerb = 6, BaseSuccessRate = 0.95, Desc = "常备战时恢复药，保障秘境挂机续航。" },
                new RecipeConfig { Id = "RCP_ZHUJI", NameCn = "炼制·筑基灵丹", ShortName = "筑基丹方", MaxMasteryLv = 5, RequiredBuildingId = "BLD_ALCHEMY", MinBuildingLevel = 2, OutputItemId = "ITEM_ZHUJI_PILL", OutputCount = 1, CraftTimeSec = 22, CostSpiritStone = 80, CostHerb = 28, CostMonsterCore = 2, BaseSuccessRate = 0.78, Desc = "炼气圆满冲击筑基必备灵丹。" },
                new RecipeConfig { Id = "RCP_JIUZHUAN", NameCn = "炼制·九转培元丹", ShortName = "培元丹方", MaxMasteryLv = 5, RequiredBuildingId = "BLD_ALCHEMY", MinBuildingLevel = 3, OutputItemId = "ITEM_JIUZHUAN_PILL", OutputCount = 1, CraftTimeSec = 25, CostSpiritStone = 95, CostHerb = 36, CostMonsterCore = 3, BaseSuccessRate = 0.80, Desc = "筑基与金丹期快速增进气海修为。" },
                new RecipeConfig { Id = "RCP_JIEDAN", NameCn = "炼制·紫金结丹丸", ShortName = "结丹丸方", MaxMasteryLv = 5, RequiredBuildingId = "BLD_ALCHEMY", MinBuildingLevel = 3, OutputItemId = "ITEM_JIEDAN_PILL", OutputCount = 1, CraftTimeSec = 35, CostSpiritStone = 220, CostHerb = 65, CostMonsterCore = 6, BaseSuccessRate = 0.70, Desc = "凝结金丹的核心破境圣药。" },

                new RecipeConfig { Id = "RCP_SWORD_1", NameCn = "锻造·青锋灵剑", ShortName = "青锋剑谱", MaxMasteryLv = 5, RequiredBuildingId = "BLD_FORGE", MinBuildingLevel = 1, OutputItemId = "EQ_SWORD_1", OutputCount = 1, CraftTimeSec = 14, CostSpiritStone = 35, CostOre = 22, CostWood = 10, BaseSuccessRate = 0.90, Desc = "百炼玄铁锻打一阶飞剑。" },
                new RecipeConfig { Id = "RCP_ARMOR_1", NameCn = "缝制·灵丝道袍", ShortName = "灵丝袍谱", MaxMasteryLv = 5, RequiredBuildingId = "BLD_FORGE", MinBuildingLevel = 1, OutputItemId = "EQ_ARMOR_1", OutputCount = 1, CraftTimeSec = 14, CostSpiritStone = 30, CostWood = 20, CostHerb = 12, BaseSuccessRate = 0.90, Desc = "轻便坚韧的护身道袍。" },
                new RecipeConfig { Id = "RCP_TAL_ICE", NameCn = "祭炼·玄冰剑符", ShortName = "玄冰符谱", MaxMasteryLv = 5, RequiredBuildingId = "BLD_FORGE", MinBuildingLevel = 2, OutputItemId = "TAL_ICE_BLADE", OutputCount = 1, CraftTimeSec = 20, CostSpiritStone = 75, CostOre = 30, CostMonsterCore = 3, BaseSuccessRate = 0.82, Desc = "封入妖丹寒气的主动攻击法宝。" },
                new RecipeConfig { Id = "RCP_SWORD_2", NameCn = "锻造·赤炎离火剑", ShortName = "离火剑谱", MaxMasteryLv = 5, RequiredBuildingId = "BLD_FORGE", MinBuildingLevel = 2, OutputItemId = "EQ_SWORD_2", OutputCount = 1, CraftTimeSec = 26, CostSpiritStone = 120, CostOre = 55, CostMonsterCore = 4, BaseSuccessRate = 0.78, Desc = "二阶主力飞剑，大幅提升攻击面板。" },
                new RecipeConfig { Id = "RCP_TAL_SHIELD", NameCn = "祭炼·八卦玄龟甲", ShortName = "玄龟甲谱", MaxMasteryLv = 5, RequiredBuildingId = "BLD_FORGE", MinBuildingLevel = 2, OutputItemId = "TAL_TURTLE_SHIELD", OutputCount = 1, CraftTimeSec = 24, CostSpiritStone = 100, CostOre = 48, CostMonsterCore = 4, BaseSuccessRate = 0.80, Desc = "战斗中撑起厚重灵盾抵御强敌。" },
                new RecipeConfig { Id = "RCP_TAL_FAN", NameCn = "祭炼·五火七禽扇", ShortName = "七禽扇谱", MaxMasteryLv = 5, RequiredBuildingId = "BLD_FORGE", MinBuildingLevel = 3, OutputItemId = "TAL_FIRE_FAN", OutputCount = 1, CraftTimeSec = 36, CostSpiritStone = 260, CostOre = 85, CostWood = 60, CostMonsterCore = 8, BaseSuccessRate = 0.72, Desc = "三阶杀伐至宝，爆发极高火系伤害。" }
            };

            cfg.Techniques = new List<TechniqueConfig>
            {
                new TechniqueConfig { Id = "TECH_BASIC", NameCn = "《太虚吐纳诀》", ShortName = "太虚诀", ElementKey = "None", MaxLevel = 10, BaseUpgradeStoneCost = 40, QiRateMultiplier = 1.05, AtkMultiplier = 1.05, HpMultiplier = 1.05, DefMultiplier = 1.05, Desc = "中正平和的入门心法，每升1层额外提升吐纳与攻防属性。" },
                new TechniqueConfig { Id = "TECH_METAL_SWORD", NameCn = "《太白庚金剑典》", ShortName = "庚金剑典", ElementKey = "Metal", MaxLevel = 10, BaseUpgradeStoneCost = 75, QiRateMultiplier = 1.15, AtkMultiplier = 1.28, HpMultiplier = 1.00, DefMultiplier = 1.05, Desc = "剑修杀伐真解，主修大幅提高攻击力（庚金灵根额外共鸣）。" },
                new TechniqueConfig { Id = "TECH_WOOD_LIFE", NameCn = "《青帝长生功》", ShortName = "青帝功", ElementKey = "Wood", MaxLevel = 10, BaseUpgradeStoneCost = 75, QiRateMultiplier = 1.25, AtkMultiplier = 1.05, HpMultiplier = 1.28, DefMultiplier = 1.10, Desc = "生机绵延不绝，显著提升吐纳与气血上限（青木灵根额外共鸣）。" },
                new TechniqueConfig { Id = "TECH_FIRE_BLAZE", NameCn = "《焚天离火诀》", ShortName = "离火诀", ElementKey = "Fire", MaxLevel = 10, BaseUpgradeStoneCost = 75, QiRateMultiplier = 1.20, AtkMultiplier = 1.24, HpMultiplier = 1.08, DefMultiplier = 1.02, Desc = "真元炽烈霸道，兼顾修行吐纳与高额攻击（离火灵根额外共鸣）。" },
                new TechniqueConfig { Id = "TECH_EARTH_WALL", NameCn = "《厚土不动明王诀》", ShortName = "不动诀", ElementKey = "Earth", MaxLevel = 10, BaseUpgradeStoneCost = 75, QiRateMultiplier = 1.15, AtkMultiplier = 1.06, HpMultiplier = 1.22, DefMultiplier = 1.32, Desc = "不动如山，大幅强化防御与气血承受力（戊土灵根额外共鸣）。" },
                new TechniqueConfig { Id = "TECH_WATER_TIDE", NameCn = "《沧溟玄水真经》", ShortName = "沧溟经", ElementKey = "Water", MaxLevel = 10, BaseUpgradeStoneCost = 85, QiRateMultiplier = 1.30, AtkMultiplier = 1.12, HpMultiplier = 1.12, DefMultiplier = 1.12, Desc = "海纳百川，吐纳速率拔群且四维均衡提升（玄水灵根额外共鸣）。" }
            };

            cfg.Enemies = new List<EnemyTemplateConfig>
            {
                new EnemyTemplateConfig { Id = "ENM_WOLF", NameCn = "青风妖狼", ElementKey = "Wood", IsBoss = false, MaxHp = 135, Attack = 20, Defense = 5, Speed = 98, DropSpiritStone = 14, DropHerb = 4, DropOre = 2, DropMonsterCore = 0.5 },
                new EnemyTemplateConfig { Id = "ENM_SNAKE", NameCn = "碧鳞毒蟒", ElementKey = "Water", IsBoss = false, MaxHp = 175, Attack = 25, Defense = 7, Speed = 92, DropSpiritStone = 18, DropHerb = 6, DropOre = 2, DropMonsterCore = 0.8 },
                new EnemyTemplateConfig { Id = "BOSS_MIST_TIGER", NameCn = "【妖王】云纹白额虎", ElementKey = "Metal", IsBoss = true, MaxHp = 390, Attack = 38, Defense = 12, Speed = 105, DropSpiritStone = 55, DropHerb = 12, DropOre = 10, DropMonsterCore = 2.0, DropSpecialItemId = "EQ_SWORD_1", DropSpecialChance = 0.25 },
                new EnemyTemplateConfig { Id = "ENM_GOLEM", NameCn = "赤铜矿傀", ElementKey = "Earth", IsBoss = false, MaxHp = 380, Attack = 52, Defense = 22, Speed = 85, DropSpiritStone = 35, DropHerb = 4, DropOre = 12, DropMonsterCore = 1.2 },
                new EnemyTemplateConfig { Id = "ENM_BAT", NameCn = "地火炎蝠", ElementKey = "Fire", IsBoss = false, MaxHp = 310, Attack = 62, Defense = 15, Speed = 112, DropSpiritStone = 38, DropHerb = 8, DropOre = 9, DropMonsterCore = 1.5 },
                new EnemyTemplateConfig { Id = "BOSS_FLAME_LIZARD", NameCn = "【妖王】熔岩赤甲龙蜥", ElementKey = "Fire", IsBoss = true, MaxHp = 980, Attack = 92, Defense = 34, Speed = 108, DropSpiritStone = 140, DropHerb = 22, DropOre = 28, DropMonsterCore = 4.0, DropSpecialItemId = "ITEM_ZHUJI_PILL", DropSpecialChance = 0.30 },
                new EnemyTemplateConfig { Id = "ENM_SWORD_WRAITH", NameCn = "残剑剑灵", ElementKey = "Metal", IsBoss = false, MaxHp = 880, Attack = 128, Defense = 45, Speed = 118, DropSpiritStone = 85, DropHerb = 12, DropOre = 24, DropMonsterCore = 2.5 },
                new EnemyTemplateConfig { Id = "BOSS_SWORD_GUARD", NameCn = "【镇守】古修紫金剑傀", ElementKey = "Metal", IsBoss = true, MaxHp = 2450, Attack = 215, Defense = 82, Speed = 122, DropSpiritStone = 320, DropHerb = 45, DropOre = 60, DropMonsterCore = 8.0, DropSpecialItemId = "EQ_SWORD_3", DropSpecialChance = 0.18 },
                new EnemyTemplateConfig { Id = "ENM_ABYSS_DEMON", NameCn = "九幽影魔", ElementKey = "Water", IsBoss = false, MaxHp = 2100, Attack = 290, Defense = 105, Speed = 128, DropSpiritStone = 190, DropHerb = 35, DropOre = 40, DropMonsterCore = 5.0 },
                new EnemyTemplateConfig { Id = "BOSS_ABYSS_LORD", NameCn = "【魔尊】九幽裂天魔猿", ElementKey = "Earth", IsBoss = true, MaxHp = 6500, Attack = 480, Defense = 190, Speed = 132, DropSpiritStone = 780, DropHerb = 95, DropOre = 110, DropMonsterCore = 16.0, DropSpecialItemId = "ITEM_NINGYING_PILL", DropSpecialChance = 0.35 }
            };

            cfg.Stages = new List<CombatStageConfig>
            {
                new CombatStageConfig { Id = "STG_MIST_VALLEY", NameCn = "一阶秘境·迷雾灵谷", ShortName = "迷雾灵谷", StageTier = 1, RecommendedRealmKey = "QiCondensation", WaveCount = 3, NormalEnemyIds = new List<string> { "ENM_WOLF", "ENM_SNAKE" }, BossEnemyId = "BOSS_MIST_TIGER", NextUnlockStageId = "STG_FLAME_CAVE", ClearReputationReward = 15, Desc = "云雾缭绕的外山灵谷，首领有概率掉落青锋剑与随机词条阵宝。" },
                new CombatStageConfig { Id = "STG_FLAME_CAVE", NameCn = "二阶秘境·赤炎地火窟", ShortName = "赤炎火窟", StageTier = 2, RecommendedRealmKey = "Foundation", WaveCount = 3, NormalEnemyIds = new List<string> { "ENM_GOLEM", "ENM_BAT" }, BossEnemyId = "BOSS_FLAME_LIZARD", NextUnlockStageId = "STG_SWORD_TOMB", ClearReputationReward = 35, Desc = "地火奔涌的矿脉深窟，盛产玄铁矿、筑基灵丹与二阶词条法宝。" },
                new CombatStageConfig { Id = "STG_SWORD_TOMB", NameCn = "三阶秘境·万古剑冢", ShortName = "万古剑冢", StageTier = 3, RecommendedRealmKey = "GoldenCore", WaveCount = 4, NormalEnemyIds = new List<string> { "ENM_SWORD_WRAITH", "ENM_BAT" }, BossEnemyId = "BOSS_SWORD_GUARD", NextUnlockStageId = "STG_ABYSS_RIFT", ClearReputationReward = 80, Desc = "上古剑修遗留的杀伐秘境，首领高概率掉落高品随机词条法宝。" },
                new CombatStageConfig { Id = "STG_ABYSS_RIFT", NameCn = "四阶绝地·九幽魔渊", ShortName = "九幽魔渊", StageTier = 4, RecommendedRealmKey = "NascentSoul", WaveCount = 4, NormalEnemyIds = new List<string> { "ENM_ABYSS_DEMON", "ENM_SWORD_WRAITH" }, BossEnemyId = "BOSS_ABYSS_LORD", NextUnlockStageId = "", ClearReputationReward = 180, Desc = "魔气滔天的九幽裂隙，盘踞着上古魔尊，掉落九天凝婴丹与仙品阵宝。" }
            };

            cfg.ArtifactTemplates = CreateDefaultArtifactTemplates();
            cfg.Acupoints = CreateDefaultAcupoints();
            cfg.WorldFactions = CreateDefaultWorldFactions();
            cfg.MarketListings = CreateDefaultMarketListings();
            cfg.EncounterEvents = CreateDefaultEncounterEvents();
            return cfg;
        }
    }
}
