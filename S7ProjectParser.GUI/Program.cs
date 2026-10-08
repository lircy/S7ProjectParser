using System;
using System.Windows.Forms;

namespace S7ProjectParser.GUI
{
    internal static class Program
    {
        /// <summary>程序入口。可传入一个工程目录或 .s7p 文件路径作为首个参数自动打开。</summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string initialPath = null;
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                initialPath = args[1];
            }

            Application.Run(new MainForm(initialPath));
        }
    }
}
