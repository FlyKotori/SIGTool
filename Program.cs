using System;
using System.Windows.Forms;
using SIGTool.Forms;

namespace SIGTool
{
    internal static class Program
    {
        /// <summary>
        /// 应用程序主入口点 (.NET Framework 4.7.2)
        /// • 常规双击启动：进入 MainSIGForm 正常桌面游玩与托盘挂机；
        /// • 自动化测试模式：SIGTool.exe --autotest [TestSpec.json] [--out ReportDir]
        /// </summary>
        [STAThread]
        private static int Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args != null && args.Length > 0)
            {
                bool autoTest = false;
                string specPath = "";
                string outDir = "";

                for (int i = 0; i < args.Length; i++)
                {
                    string a = args[i] ?? "";
                    if (string.Equals(a, "--autotest", StringComparison.OrdinalIgnoreCase))
                    {
                        autoTest = true;
                        if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                        {
                            specPath = args[++i];
                        }
                    }
                    else if (string.Equals(a, "--out", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                    {
                        outDir = args[++i];
                    }
                }

                if (autoTest)
                {
                    Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
                    return AutoTestRunner.RunFromCli(specPath, outDir);
                }
            }

            Application.Run(new MainSIGForm(false));
            return 0;
        }
    }
}
