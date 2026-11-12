using System;
using System.IO;
using System.Text;

namespace SIGTool.Configs
{
    /// <summary>
    /// JSON 配置读取与默认配置文件写出工具（统一使用 SIGConfig.json）
    /// </summary>
    public static class ConfigLoader
    {
        public static string DefaultConfigFilePath
        {
            get
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string cfgDir = Path.Combine(baseDir, "Configs");
                if (Directory.Exists(cfgDir))
                {
                    return Path.Combine(cfgDir, "SIGConfig.json");
                }
                return Path.Combine(baseDir, "SIGConfig.json");
            }
        }

        public static bool LoadOrInitConfig(string filePath, out string message)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    filePath = DefaultConfigFilePath;
                }

                if (!File.Exists(filePath))
                {
                    var defCfg = SIGConfigTable.CreateDefaultConfig();
                    SaveConfigToFile(defCfg, filePath);
                    SIGConfigTable.ApplyConfig(defCfg);
                    message = string.Format("未检测到外部配表，已自动生成默认配表：{0}", Path.GetFileName(filePath));
                    return true;
                }

                string rawJson = File.ReadAllText(filePath, Encoding.UTF8);
                var loaded = SimpleJsonSerializer.Deserialize<SIGConfigRoot>(rawJson);
                if (loaded == null)
                {
                    message = "配表反序列化结果为空，已回退使用内置默认配表。";
                    SIGConfigTable.ApplyConfig(SIGConfigTable.CreateDefaultConfig());
                    return false;
                }

                SIGConfigTable.ApplyConfig(loaded);
                message = string.Format("成功加载配表：{0}（境界 {1} 阶 / 建筑 {2} 种 / 配方 {3} 项 / 阵宝胚 {4} 种）",
                    Path.GetFileName(filePath),
                    loaded.Realms != null ? loaded.Realms.Count : 0,
                    loaded.Buildings != null ? loaded.Buildings.Count : 0,
                    loaded.Recipes != null ? loaded.Recipes.Count : 0,
                    loaded.ArtifactTemplates != null ? loaded.ArtifactTemplates.Count : 0);
                return true;
            }
            catch (Exception ex)
            {
                SIGConfigTable.ApplyConfig(SIGConfigTable.CreateDefaultConfig());
                message = string.Format("读取配表异常（已回退内置默认配表）：{0}", ex.Message);
                return false;
            }
        }

        public static void SaveConfigToFile(SIGConfigRoot root, string filePath)
        {
            string dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string header = "// ============================================================================\r\n" +
                            "// SIGTool - 外部热加载数值配表 (SIGConfig.json)\r\n" +
                            "// 支持 // 行注释；修改数值后可在主界面点击【配表】立即热重载生效。\r\n" +
                            "// ============================================================================\r\n";
            string body = SimpleJsonSerializer.Serialize(root, true);
            File.WriteAllText(filePath, header + body, new UTF8Encoding(false));
        }
    }
}
