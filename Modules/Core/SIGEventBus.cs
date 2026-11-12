using System;
using System.Collections.Generic;
using SIGTool.Datas;

namespace SIGTool.Modules.Core
{
    public enum LogCategory
    {
        System = 0,
        Cultivation = 1,
        Sect = 2,
        Craft = 3,
        Combat = 4,
        Artifact = 5
    }

    public class SIGLogEntry
    {
        public string RealTimeText { get; set; }
        public string ImmortalDateText { get; set; }
        public LogCategory Category { get; set; }
        public string Message { get; set; }

        public string FormatLine()
        {
            return string.Format("[{0} | {1}] 【{2}】{3}",
                RealTimeText, ImmortalDateText, CategoryCn(Category), Message);
        }

        public static string CategoryCn(LogCategory cat)
        {
            switch (cat)
            {
                case LogCategory.Cultivation: return "修仙";
                case LogCategory.Sect: return "宗门";
                case LogCategory.Craft: return "百艺";
                case LogCategory.Combat: return "斗法";
                case LogCategory.Artifact: return "阵宝";
                default: return "天机";
            }
        }
    }

    /// <summary>
    /// 轻量级事件总线与修真日志中心（命名统一为 SIGEventBus）
    /// </summary>
    public class SIGEventBus
    {
        private readonly List<SIGLogEntry> _logs = new List<SIGLogEntry>(400);
        public const int MaxLogCapacity = 320;

        public bool SuppressNotifications { get; set; } = false;

        public event Action<SIGLogEntry> OnLogAdded;
        public event Action OnLogsCleared;
        public event Action OnStateChanged;
        public event Action<OfflineGainSummary> OnOfflineReportReady;
        public event Action OnRaidAlertTriggered;

        public IReadOnlyList<SIGLogEntry> Logs
        {
            get { return _logs; }
        }

        public void PublishLog(string immortalDateText, LogCategory category, string message)
        {
            if (SuppressNotifications) return;

            var entry = new SIGLogEntry
            {
                RealTimeText = DateTime.Now.ToString("HH:mm:ss"),
                ImmortalDateText = immortalDateText ?? "",
                Category = category,
                Message = message ?? ""
            };

            _logs.Add(entry);
            if (_logs.Count > MaxLogCapacity)
            {
                _logs.RemoveRange(0, _logs.Count - MaxLogCapacity);
            }

            var handler = OnLogAdded;
            if (handler != null) handler(entry);
        }

        public void ClearLogs()
        {
            _logs.Clear();
            var handler = OnLogsCleared;
            if (handler != null) handler();
        }

        public void NotifyStateChanged()
        {
            if (SuppressNotifications) return;
            var handler = OnStateChanged;
            if (handler != null) handler();
        }

        public void NotifyOfflineReport(OfflineGainSummary summary)
        {
            if (summary == null) return;
            var handler = OnOfflineReportReady;
            if (handler != null) handler(summary);
        }

        public void NotifyRaidAlert()
        {
            if (SuppressNotifications) return;
            var handler = OnRaidAlertTriggered;
            if (handler != null) handler();
        }
    }
}
