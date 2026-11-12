using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SIGTool.Configs;
using SIGTool.Datas;

namespace SIGTool.Modules.Core
{
    /// <summary>
    /// 客户端界面主题、老板键与分栏布局记忆持久化服务（独立存储于 Saves/ClientSettings.json）
    /// </summary>
    public static class ClientSettingsService
    {
        public static ClientSettingsData Current { get; private set; } = new ClientSettingsData();

        public static string GetSettingsFilePath()
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Saves");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, "ClientSettings.json");
        }

        public static ClientSettingsData Load()
        {
            try
            {
                string path = GetSettingsFilePath();
                if (!File.Exists(path))
                {
                    Current = new ClientSettingsData();
                    Save(Current);
                    return Current;
                }

                string raw = File.ReadAllText(path, Encoding.UTF8);
                var loaded = SimpleJsonSerializer.Deserialize<ClientSettingsData>(raw);
                Current = loaded ?? new ClientSettingsData();
                Sanitize(Current);
                return Current;
            }
            catch
            {
                Current = new ClientSettingsData();
                return Current;
            }
        }

        public static bool Save(ClientSettingsData settings)
        {
            try
            {
                if (settings == null) settings = Current ?? new ClientSettingsData();
                Sanitize(settings);
                Current = settings;

                string path = GetSettingsFilePath();
                string json = SimpleJsonSerializer.Serialize(settings, true);
                File.WriteAllText(path, json, new UTF8Encoding(false));
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void Sanitize(ClientSettingsData s)
        {
            if (string.IsNullOrEmpty(s.ThemeMode))
            {
                s.ThemeMode = UiThemeMode.MinimalDarkGray.ToString();
            }
            if (s.BossHotkeyVirtualKey <= 0 || s.BossHotkeyVirtualKey > 255)
            {
                s.BossHotkeyModifiers = 1;
                s.BossHotkeyVirtualKey = 81;
            }
            bool isFunctionKey = (s.BossHotkeyVirtualKey >= 112 && s.BossHotkeyVirtualKey <= 123);
            if (!isFunctionKey && (s.BossHotkeyModifiers & 0x07) == 0)
            {
                s.BossHotkeyModifiers = 1;
            }
            if (s.WindowWidth < 1020 || s.WindowWidth > 4096) s.WindowWidth = 1260;
            if (s.WindowHeight < 680 || s.WindowHeight > 2400) s.WindowHeight = 820;
            if (s.SplitRatios == null) s.SplitRatios = new Dictionary<string, double>();
        }
    }
}
