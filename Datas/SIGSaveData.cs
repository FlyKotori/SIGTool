using System;
using System.Collections.Generic;

namespace SIGTool.Datas
{
    /// <summary>
    /// 顶层存档聚合数据（包含仙历时钟、掌门、门人、宗门基建、法宝大阵与战斗进度）
    /// 命名已全面统一为 SIG 前缀。
    /// </summary>
    public class SIGSaveData
    {
        public string SaveVersion { get; set; } = "0.4.0";
        public long LastSaveUtcTicks { get; set; } = 0L;

        public double TotalPlaySeconds { get; set; } = 0.0;
        public int SIGYear { get; set; } = 1;
        public int SIGMonth { get; set; } = 1;
        public double MonthProgressSec { get; set; } = 0.0;

        public CharacterData Leader { get; set; } = new CharacterData();
        public List<CharacterData> Disciples { get; set; } = new List<CharacterData>();
        public SectManageData Sect { get; set; } = new SectManageData();
        public CombatStateData Combat { get; set; } = new CombatStateData();

        public string GetImmortalDateText()
        {
            string seasonCn = "春";
            if (SIGMonth >= 4 && SIGMonth <= 6) seasonCn = "夏";
            else if (SIGMonth >= 7 && SIGMonth <= 9) seasonCn = "秋";
            else if (SIGMonth >= 10) seasonCn = "冬";
            return string.Format("仙历第 {0} 年 {1} 月 ({2})", SIGYear, SIGMonth, seasonCn);
        }
    }

    /// <summary>
    /// 离线挂机收益结算报告单（不入盘，仅用于上线弹窗与日志汇总）
    /// </summary>
    public class OfflineGainSummary
    {
        public double RawOfflineSeconds { get; set; }
        public double EffectiveSimulatedSeconds { get; set; }
        public double LeaderQiGained { get; set; }
        public int MonthsAdvanced { get; set; }
        public int ExpeditionWins { get; set; }
        public Dictionary<ResourceType, double> ResourceGains { get; set; } = new Dictionary<ResourceType, double>();
    }
}
