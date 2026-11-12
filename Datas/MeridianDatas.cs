using System;
using System.Collections.Generic;

namespace SIGTool.Datas
{
    /// <summary>
    /// SYS-02 奇经脉络分支枚举（督脉 / 任脉 / 冲脉 / 带脉）
    /// </summary>
    public enum MeridianBranchType
    {
        DuMai = 0,    // 督脉·乾阳：主攻伐与功法倍率乘区
        RenMai = 1,   // 任脉·坤阴：主气海上限、吐纳速率与寿元
        ChongMai = 2, // 冲脉·血海：主气血、护体防御与斗法汲血
        DaiMai = 3    // 带脉·天枢：主速度、暴击闪避与大境界渡劫率
    }

    /// <summary>
    /// SYS-02 角色经脉穴位与周天共鸣实时汇总快照（不入盘，由逻辑层实时聚合供修炼与战斗读取）
    /// </summary>
    public class MeridianBonusSummary
    {
        public int UnlockedCount { get; set; }
        public int TotalCount { get; set; }

        public int DuMaiUnlocked { get; set; }
        public int RenMaiUnlocked { get; set; }
        public int ChongMaiUnlocked { get; set; }
        public int DaiMaiUnlocked { get; set; }

        public bool DuMaiComplete { get; set; }
        public bool RenMaiComplete { get; set; }
        public bool ChongMaiComplete { get; set; }
        public bool DaiMaiComplete { get; set; }

        public bool HasXiaoZhouTian { get; set; } // 任督二脉全通
        public bool HasDaZhouTian { get; set; }   // 四脉24穴全通

        public double FlatHp { get; set; }
        public double FlatMp { get; set; }
        public double FlatAtk { get; set; }
        public double FlatDef { get; set; }
        public double FlatSpeed { get; set; }
        public double LifespanYearsAdd { get; set; }

        public double PctHp { get; set; }
        public double PctAtk { get; set; }
        public double PctDef { get; set; }
        public double PctMaxQi { get; set; }

        /// <summary>功法效果独立乘区加成（如 +0.18 表示主修功法攻防血与吐纳倍率额外 ×1.18）</summary>
        public double TechMultiplierAdd { get; set; }

        public double QiRatePctAdd { get; set; }
        public double CritRateAdd { get; set; }
        public double DodgeRateAdd { get; set; }
        public double LifestealRateAdd { get; set; }
        public double BreakthroughRateAdd { get; set; }
    }
}
