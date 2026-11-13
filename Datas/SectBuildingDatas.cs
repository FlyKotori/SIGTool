using System;
using System.Collections.Generic;

namespace SIGTool.Datas
{
    /// <summary>
    /// 宗门建筑实例运行时数据
    /// </summary>
    public class BuildingInstanceData
    {
        public string InstanceId { get; set; } = "";
        public string ConfigId { get; set; } = "";
        public int Level { get; set; } = 1;

        public bool IsUnderConstruction { get; set; } = false;
        public double BuildProgressSec { get; set; } = 0.0;
        public double BuildTotalSec { get; set; } = 10.0;

        public string ActiveRecipeId { get; set; } = "";
        public double CraftProgressSec { get; set; } = 0.0;
        public bool AutoRepeatRecipe { get; set; } = true;
    }

    /// <summary>
    /// 宗门山门灵脉地块数据（6列 x 4行 = 24格灵脉）
    /// </summary>
    public class SectGridSlotData
    {
        public int SlotIndex { get; set; } = 0;
        public int Row { get; set; } = 0;
        public int Col { get; set; } = 0;
        public string ZoneName { get; set; } = "";
        public ElementType VeinElement { get; set; } = ElementType.Earth;
        public double BaseAura { get; set; } = 1.0;
        public bool IsUnlocked { get; set; } = true;
        public BuildingInstanceData Building { get; set; } = null;
    }

    /// <summary>
    /// 宗门经营全局状态（资源库、物品储物阁、配方熟练度、灵脉地块、随机词条法宝库与九宫护山大阵）
    /// </summary>
    public class SectManageData
    {
        public string SectName { get; set; } = "太虚云隐宗";
        public int SectLevel { get; set; } = 1;

        public double BarrierCurHp { get; set; } = 600.0;
        public double BarrierMaxHp { get; set; } = 600.0;

        public Dictionary<string, double> Resources { get; set; } = new Dictionary<string, double>();
        public Dictionary<string, int> ItemInventory { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> RecipeMastery { get; set; } = new Dictionary<string, int>();
        public List<SectGridSlotData> GridSlots { get; set; } = new List<SectGridSlotData>();

        // SYS-01 随机词条法宝库与 3x3 九宫护山大阵阵眼
        public int NextArtifactSeq { get; set; } = 1;
        public int ArtifactVaultCap { get; set; } = 60;
        public List<ArtifactInstanceData> ArtifactVault { get; set; } = new List<ArtifactInstanceData>();
        public List<FormationEyeSlotData> FormationEyes { get; set; } = new List<FormationEyeSlotData>();

        // SYS-03 修仙界演化与盟友气运同步缓存（供修炼/战斗面板零耦合实时读取）
        public double WorldAuraBonusCache { get; set; } = 0.0;
        public double WorldAtkPctBonusCache { get; set; } = 0.0;

        public double GetResource(ResourceType type)
        {
            if (Resources == null) Resources = new Dictionary<string, double>();
            string key = type.ToString();
            double val;
            return Resources.TryGetValue(key, out val) ? val : 0.0;
        }

        public void SetResource(ResourceType type, double value)
        {
            if (Resources == null) Resources = new Dictionary<string, double>();
            Resources[type.ToString()] = Math.Max(0.0, value);
        }

        public void AddResource(ResourceType type, double delta)
        {
            SetResource(type, GetResource(type) + delta);
        }

        public int GetItemCount(string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || ItemInventory == null) return 0;
            int count;
            return ItemInventory.TryGetValue(itemId, out count) ? count : 0;
        }

        public void AddItem(string itemId, int delta)
        {
            if (string.IsNullOrEmpty(itemId)) return;
            if (ItemInventory == null) ItemInventory = new Dictionary<string, int>();
            int next = Math.Min(999, GetItemCount(itemId) + delta);
            if (next <= 0)
            {
                ItemInventory.Remove(itemId);
            }
            else
            {
                ItemInventory[itemId] = next;
            }
        }

        public int GetRecipeMasteryLv(string recipeId)
        {
            if (string.IsNullOrEmpty(recipeId)) return 1;
            if (RecipeMastery == null) RecipeMastery = new Dictionary<string, int>();
            int lv;
            return RecipeMastery.TryGetValue(recipeId, out lv) ? Math.Max(1, lv) : 1;
        }

        public ArtifactInstanceData FindArtifactById(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId) || ArtifactVault == null) return null;
            for (int i = 0; i < ArtifactVault.Count; i++)
            {
                if (ArtifactVault[i].InstanceId == instanceId) return ArtifactVault[i];
            }
            return null;
        }
    }
}
