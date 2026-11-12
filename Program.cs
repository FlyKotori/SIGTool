using System;
using System.Windows.Forms;
using SIGTool.Forms;

namespace SIGTool
{
    internal static class Program
    {
        /// <summary>
        /// 应用程序主入口点 (.NET Framework 4.7.2)
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainSIGForm());
        }
    }
}
