using System;
using System.Collections.Generic;

namespace SIGTool.Datas
{
    /// <summary>
    /// 自动化测试书单步指令配置（存放于 QA/TestSpec_*.json，零外部依赖反射反序列化）
    /// </summary>
    public class AutoTestStepSpec
    {
        public string StepId { get; set; } = "";
        public string Action { get; set; } = "";
        public string TargetKey { get; set; } = "";
        public int IntParam { get; set; } = 0;
        public double Amount { get; set; } = 0.0;
        public bool BoolParam { get; set; } = false;
        public string OutputFile { get; set; } = "";
        public string Desc { get; set; } = "";
    }

    /// <summary>
    /// 自动化测试书内存态与 UI 指标断言项
    /// </summary>
    public class AutoTestAssertionSpec
    {
        public string MetricPath { get; set; } = "";
        public string Op { get; set; } = ">="; // ==, !=, >=, <=, >, <
        public double Expected { get; set; } = 0.0;
        public string Desc { get; set; } = "";
    }

    /// <summary>
    /// 自动化测试书根对象（TestSpec.json）
    /// </summary>
    public class AutoTestSpecRoot
    {
        public string SpecVersion { get; set; } = "0.9.2";
        public string Title { get; set; } = "SIGTool 全系统回归测试书";
        public int RandomSeed { get; set; } = 20261113;
        public List<AutoTestStepSpec> Steps { get; set; } = new List<AutoTestStepSpec>();
        public List<AutoTestAssertionSpec> Assertions { get; set; } = new List<AutoTestAssertionSpec>();
    }

    /// <summary>
    /// 单步执行结果记录
    /// </summary>
    public class AutoTestStepResult
    {
        public string StepId { get; set; } = "";
        public string Action { get; set; } = "";
        public bool Ok { get; set; } = true;
        public string Detail { get; set; } = "";
        public long ElapsedMs { get; set; } = 0L;
    }

    /// <summary>
    /// 单条断言判定结果记录
    /// </summary>
    public class AutoTestAssertionResult
    {
        public string MetricPath { get; set; } = "";
        public string Op { get; set; } = "";
        public double Actual { get; set; } = 0.0;
        public double Expected { get; set; } = 0.0;
        public bool Passed { get; set; } = false;
        public string Desc { get; set; } = "";
    }

    /// <summary>
    /// 测试结束时导出的关键内存态快照（供 Web 端 AI 直接读取核验）
    /// </summary>
    public class AutoTestMemorySnapshot
    {
        public int SIGYear { get; set; }
        public int SIGMonth { get; set; }
        public string LeaderRealmText { get; set; } = "";
        public double LeaderCurQi { get; set; }
        public double LeaderMaxQi { get; set; }
        public double LeaderCombatPower { get; set; }
        public double LeaderAttack { get; set; }
        public int UnlockedAcupoints { get; set; }
        public double BarrierCurHp { get; set; }
        public double BarrierMaxHp { get; set; }
        public double SpiritStone { get; set; }
        public double SpiritHerb { get; set; }
        public int ArtifactVaultCount { get; set; }
        public string WorldTrendCn { get; set; } = "";
        public int PendingEncounters { get; set; }
        public int ResolvedEncounters { get; set; }
        public int TruncatedButtonCount { get; set; }
        public int NonGrayscaleColorCount { get; set; }
        public int HeaderBarrierDigitsDiffPixels { get; set; }
        public int HeaderRightRegionDiffPixels { get; set; }
    }

    /// <summary>
    /// 自动化测试最终输出总报告（LatestReport.json）
    /// </summary>
    public class AutoTestReportRoot
    {
        public string ToolVersion { get; set; } = "SIGTool v0.9.2";
        public string SpecTitle { get; set; } = "";
        public string TimestampLocal { get; set; } = "";
        public bool BuildSucceeded { get; set; } = true;
        public bool AllPassed { get; set; } = false;
        public long TotalElapsedMs { get; set; } = 0L;
        public string SummaryMessage { get; set; } = "";
        public AutoTestMemorySnapshot MemorySnapshot { get; set; } = new AutoTestMemorySnapshot();
        public List<AutoTestStepResult> StepResults { get; set; } = new List<AutoTestStepResult>();
        public List<AutoTestAssertionResult> AssertionResults { get; set; } = new List<AutoTestAssertionResult>();
        public List<string> Screenshots { get; set; } = new List<string>();
        public List<string> LayoutWarnings { get; set; } = new List<string>();
    }
}
