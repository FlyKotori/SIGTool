using System;
using System.Collections.Generic;

namespace SIGTool.Datas
{
    /// <summary>
    /// 界面皮肤枚举（EXT-02）：
    /// StandardSlate   = 标准暗蓝配色皮肤
    /// MinimalDarkGray = 极简深灰办公皮肤（纯灰度暗色工作台，无彩色干扰）
    /// CleanOfficeLight= 清爽白底办公皮肤（纯灰度亮色表格风，无彩色干扰）
    /// </summary>
    public enum UiThemeMode
    {
        StandardSlate = 0,
        MinimalDarkGray = 1,
        CleanOfficeLight = 2
    }

    /// <summary>
    /// 客户端全局界面、老板键与布局比例记忆设置（持久化至 Saves/ClientSettings.json，重开新局不丢失）
    /// 注意：严格保持纯数据定义，不依赖任何窗体视图层组件。
    /// </summary>
    public class ClientSettingsData
    {
        public string ThemeMode { get; set; } = "MinimalDarkGray";

        public bool EnableBossHotkey { get; set; } = true;
        public int BossHotkeyModifiers { get; set; } = 1;
        public int BossHotkeyVirtualKey { get; set; } = 81;

        public bool CloseButtonMinimizesToTray { get; set; } = false;
        public bool UseMinimalWindowTitle { get; set; } = true;

        // UI-UX-06：主窗体尺寸与各分栏拖拽比例持久化记忆
        public int WindowWidth { get; set; } = 1260;
        public int WindowHeight { get; set; } = 820;
        public bool IsWindowMaximized { get; set; } = false;
        public Dictionary<string, double> SplitRatios { get; set; } = new Dictionary<string, double>();

        public UiThemeMode GetThemeModeEnum()
        {
            UiThemeMode parsed;
            if (!string.IsNullOrEmpty(ThemeMode) && Enum.TryParse(ThemeMode, true, out parsed))
            {
                return parsed;
            }
            return UiThemeMode.MinimalDarkGray;
        }

        public void SetThemeModeEnum(UiThemeMode mode)
        {
            ThemeMode = mode.ToString();
        }

        public double GetSplitRatio(string key, double defaultRatio)
        {
            if (string.IsNullOrEmpty(key) || SplitRatios == null) return defaultRatio;
            double v;
            if (SplitRatios.TryGetValue(key, out v) && v >= 0.12 && v <= 0.88)
            {
                return v;
            }
            return defaultRatio;
        }

        public void SetSplitRatio(string key, double ratio)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (SplitRatios == null) SplitRatios = new Dictionary<string, double>();
            if (ratio >= 0.12 && ratio <= 0.88)
            {
                SplitRatios[key] = Math.Round(ratio, 4);
            }
        }
    }
}
