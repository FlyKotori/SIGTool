using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SIGTool.Configs;
using SIGTool.Datas;
using SIGTool.Modules.Logics;

namespace SIGTool.Modules.Core
{
    /// <summary>
    /// 存档序列化与新开局宗门数据工厂（统一采用 SIG 命名，零外部引用依赖）
    /// </summary>
    public static class SaveLoadService
    {
        public static string GetSaveFilePath(int slotIndex)
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Saves");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, string.Format("SaveSlot_{0}.json", slotIndex));
        }

        public static bool SaveToDisk(SIGSaveData data, int slotIndex, out string message)
        {
            try
            {
                if (data == null) { message = "存档数据为空。"; return false; }
                data.LastSaveUtcTicks = DateTime.UtcNow.Ticks;
                string path = GetSaveFilePath(slotIndex);
                string json = SimpleJsonSerializer.Serialize(data, true);
                File.WriteAllText(path, json, new UTF8Encoding(false));

                message = string.Format("存档已保存至槽位 {0} ({1})", slotIndex, Path.GetFileName(path));
                return true;
            }
            catch (Exception ex)
            {
                message = string.Format("保存存档失败：{0}", ex.Message);
                return false;
            }
        }

        public static SIGSaveData LoadFromDisk(int slotIndex, out string message)
        {
            try
            {
                string path = GetSaveFilePath(slotIndex);
                if (!File.Exists(path))
                {
                    message = string.Format("槽位 {0} 尚无存档，已创建新宗门开局。", slotIndex);
                    return null;
                }

                string raw = File.ReadAllText(path, Encoding.UTF8);
                var loaded = SimpleJsonSerializer.Deserialize<SIGSaveData>(raw);
                if (loaded == null) { message = "存档解析为空。"; return null; }

                EnsureCollectionsInitialized(loaded);
                message = string.Format("成功读取槽位 {0} 存档。", slotIndex);
                return loaded;
            }
            catch (Exception ex)
            {
                message = string.Format("读取存档异常：{0}", ex.Message);
                return null;
            }
        }

        public static void EnsureCollectionsInitialized(SIGSaveData save)
        {
            if (save.SIGYear < 1) save.SIGYear = 1;
            if (save.SIGMonth < 1 || save.SIGMonth > 12) save.SIGMonth = 1;

            if (save.Leader == null) save.Leader = new CharacterData();
            save.Leader.EnsureQuickSlotsInitialized();

            if (save.Disciples == null) save.Disciples = new List<CharacterData>();
            foreach (var d in save.Disciples) d.EnsureQuickSlotsInitialized();

            if (save.Sect == null) save.Sect = new SectManageData();
            if (save.Sect.Resources == null) save.Sect.Resources = new Dictionary<string, double>();
            if (save.Sect.ItemInventory == null) save.Sect.ItemInventory = new Dictionary<string, int>();
            if (save.Sect.RecipeMastery == null) save.Sect.RecipeMastery = new Dictionary<string, int>();
            if (save.Sect.GridSlots == null) save.Sect.GridSlots = new List<SectGridSlotData>();

            ArtifactFormationLogic.EnsureFormationInitialized(save.Sect);
            WorldEvolutionLogic.EnsureWorldInitialized(save);

            if (save.Combat == null) save.Combat = new CombatStateData();
            if (save.Combat.UnlockedStageIds == null) save.Combat.UnlockedStageIds = new List<string>();
            if (save.Combat.Allies == null) save.Combat.Allies = new List<CombatUnitRuntime>();
            if (save.Combat.Enemies == null) save.Combat.Enemies = new List<CombatUnitRuntime>();
            if (save.Combat.RecentBattleLogs == null) save.Combat.RecentBattleLogs = new List<string>();
            if (save.Combat.UnlockedStageIds.Count == 0) save.Combat.UnlockedStageIds.Add("STG_MIST_VALLEY");
        }

        public static SIGSaveData CreateNewSIGSave()
        {
            var save = new SIGSaveData
            {
                SaveVersion = "0.4.0",
                LastSaveUtcTicks = DateTime.UtcNow.Ticks,
                SIGYear = 1,
                SIGMonth = 1,
                MonthProgressSec = 0.0
            };

            save.Leader = new CharacterData
            {
                Id = "CHAR_LEADER",
                Name = "云霄掌门",
                IsSectLeader = true,
                Realm = RealmTier.QiCondensation,
                SubStage = 1,
                AgeYears = 18.0,
                CurQi = 60.0,
                EquippedTechniqueId = "TECH_WATER_TIDE",
                EquippedWeaponId = "EQ_SWORD_1",
                EquippedArmorId = "EQ_ARMOR_1",
                EquippedTalismanId = "TAL_ICE_BLADE",
                QuickCultivationPills = new List<string> { "ITEM_JUQI_PILL", "ITEM_JIUZHUAN_PILL", "ITEM_HEAL_PILL_1" },
                QuickCombatPills = new List<string> { "ITEM_HEAL_PILL_1", "ITEM_HEAL_PILL_2", "" },
                AutoHealHpRatio = 0.45,
                Innate = new InnateStatsData
                {
                    Constitution = 11,
                    Strength = 11,
                    Comprehension = 13,
                    Agility = 11,
                    Fortune = 12,
                    RootElement = ElementType.Water,
                    RootQuality = 4
                }
            };

            save.Disciples.Add(new CharacterData
            {
                Id = "DISC_01",
                Name = "凌沧(大师兄)",
                IsSectLeader = false,
                Realm = RealmTier.QiCondensation,
                SubStage = 1,
                AgeYears = 19.0,
                AssignedJob = DiscipleJob.Guard,
                EquippedTechniqueId = "TECH_METAL_SWORD",
                GatherSkillLv = 1,
                ForgeSkillLv = 2,
                AlchemySkillLv = 1,
                Innate = new InnateStatsData
                {
                    Constitution = 10,
                    Strength = 13,
                    Comprehension = 9,
                    Agility = 11,
                    Fortune = 9,
                    RootElement = ElementType.Metal,
                    RootQuality = 3
                }
            });

            save.Disciples.Add(new CharacterData
            {
                Id = "DISC_02",
                Name = "苏青萝(小师妹)",
                IsSectLeader = false,
                Realm = RealmTier.Mortal,
                SubStage = 3,
                AgeYears = 16.0,
                AssignedJob = DiscipleJob.Farm,
                EquippedTechniqueId = "TECH_WOOD_LIFE",
                GatherSkillLv = 2,
                AlchemySkillLv = 2,
                ForgeSkillLv = 1,
                Innate = new InnateStatsData
                {
                    Constitution = 9,
                    Strength = 8,
                    Comprehension = 12,
                    Agility = 10,
                    Fortune = 12,
                    RootElement = ElementType.Wood,
                    RootQuality = 4
                }
            });

            save.Sect.SectName = "太虚云隐宗";
            save.Sect.SectLevel = 1;
            save.Sect.SetResource(ResourceType.SpiritStone, 320);
            save.Sect.SetResource(ResourceType.SpiritWood, 150);
            save.Sect.SetResource(ResourceType.SpiritOre, 150);
            save.Sect.SetResource(ResourceType.SpiritFood, 140);
            save.Sect.SetResource(ResourceType.SpiritHerb, 45);
            save.Sect.SetResource(ResourceType.MonsterCore, 4);
            save.Sect.SetResource(ResourceType.SectReputation, 20);

            save.Sect.AddItem("ITEM_HEAL_PILL_1", 5);
            save.Sect.AddItem("ITEM_JUQI_PILL", 3);

            ElementType[] pattern = new ElementType[]
            {
                ElementType.Earth, ElementType.Fire, ElementType.Water, ElementType.Fire, ElementType.Metal, ElementType.Wood,
                ElementType.Wood, ElementType.Fire, ElementType.Earth, ElementType.Metal, ElementType.Water, ElementType.Wood,
                ElementType.Wood, ElementType.Metal, ElementType.Water, ElementType.Earth, ElementType.Fire, ElementType.Metal,
                ElementType.Earth, ElementType.Wood, ElementType.Metal, ElementType.Water, ElementType.Fire, ElementType.Earth
            };

            for (int idx = 0; idx < 24; idx++)
            {
                int r = idx / 6;
                int c = idx % 6;
                string zone = r == 0 ? "天枢主峰(高灵)" : (r <= 2 ? "百工中院(中灵)" : "灵谷外麓(低灵)");
                double aura = r == 0 ? 1.30 : (r <= 2 ? 1.12 : 1.00);
                bool unlocked = idx < 14;

                save.Sect.GridSlots.Add(new SectGridSlotData
                {
                    SlotIndex = idx,
                    Row = r,
                    Col = c,
                    ZoneName = zone,
                    VeinElement = pattern[idx],
                    BaseAura = aura,
                    IsUnlocked = unlocked,
                    Building = null
                });
            }

            save.Sect.GridSlots[0].Building = new BuildingInstanceData { InstanceId = "INST_INIT_HALL", ConfigId = "BLD_HALL", Level = 1, IsUnderConstruction = false };
            save.Sect.GridSlots[6].Building = new BuildingInstanceData { InstanceId = "INST_INIT_FARM", ConfigId = "BLD_FARM", Level = 1, IsUnderConstruction = false };
            save.Sect.GridSlots[12].Building = new BuildingInstanceData { InstanceId = "INST_INIT_LUMBER", ConfigId = "BLD_LUMBER", Level = 1, IsUnderConstruction = false };
            save.Sect.GridSlots[4].Building = new BuildingInstanceData { InstanceId = "INST_INIT_TOWER", ConfigId = "BLD_TOWER", Level = 1, IsUnderConstruction = false };

            // SYS-01 初始化九宫护山大阵并赠送 2 件开山传承随机词条阵宝（1件镇守中宫天枢，1件置于库中供体验镶嵌对比）
            ArtifactFormationLogic.EnsureFormationInitialized(save.Sect);
            WorldEvolutionLogic.EnsureWorldInitialized(save);
            var initRng = new Random(Guid.NewGuid().GetHashCode());
            var starterCore = ArtifactFormationLogic.GenerateRandomArtifact(save.Sect, initRng, 2, 2);
            var starterSpare = ArtifactFormationLogic.GenerateRandomArtifact(save.Sect, initRng, 1, 1);
            save.Sect.ArtifactVault.Add(starterCore);
            save.Sect.ArtifactVault.Add(starterSpare);
            save.Sect.FormationEyes[4].EquippedArtifactInstanceId = starterCore.InstanceId;
            starterCore.SlottedEyeIndex = 4;

            save.Combat.UnlockedStageIds.Add("STG_MIST_VALLEY");
            save.Combat.MonthsUntilNextRaid = 12;
            save.Combat.AutoRepeatExpedition = true;

            EnsureCollectionsInitialized(save);
            WorldEvolutionLogic.RefreshMarketPricesAndStock(save, initRng, null, silentLog: true);
            WorldEvolutionLogic.AppendChronicle(save, "太虚云隐宗重立山门，九州各大宗门与天机坊市恢复往来。");
            return save;
        }
    }
}
