using System;
using System.Collections.Generic;
using System.Text;
using S7ProjectParser;
// 命名空间 S7ProjectParser.Console 与 System.Console 同名冲突，控制台调用用 System.Console 限定

namespace S7ProjectParser.Console
{
    /// <summary>
    /// 无界面测试工具，调用序列与参考驱动 DriverTest 一致（去掉通讯部分）：
    ///   Open → Browse → 打印变量表 → Close
    /// 本库为文件解析器：变量表只有符号名 / 地址 / 类型三列
    /// （Browse 与驱动一致返回全部叶子条目 VarInfo，不支持的类型已被跳过）。
    /// 错误文本由 S7Project.ErrorText 提供（中文）。
    /// 用法：
    ///   S7ProjectParser.Console <工程目录或.s7p文件>
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            System.Console.OutputEncoding = Encoding.UTF8;

            if (args.Length < 1)
            {
                System.Console.WriteLine("用法: S7ProjectParser.Console <工程目录或.s7p文件>");
                System.Console.WriteLine();
                System.Console.WriteLine("示例: S7ProjectParser.Console .\\STEP7Sample1\\test");
                return 1;
            }

            string path = args[0];

            try
            {
                int res;

                // 1) 打开工程（参数为 STEP 7 工程路径）
                S7Project project = new S7Project();
                res = project.Open(path);
                if (res != 0)
                {
                    System.Console.WriteLine("Open - Error: " + S7Project.ErrorText(res));
                    return 1;
                }
                System.Console.WriteLine("Main - Open 完成");

                // 2) 浏览变量清单（与驱动的 Browse 对齐）
                System.Console.WriteLine("Main - Starte Browse...");
                List<VarInfo> vars = new List<VarInfo>();
                res = project.Browse(out vars);
                System.Console.WriteLine("Main - Browse res=" + res);
                if (res != 0)
                {
                    System.Console.WriteLine("Browse - Error: " + S7Project.ErrorText(res));
                    project.Close();
                    return 1;
                }

                // 3) 打印变量表（解析结果：SYMBOLIC-NAME / ACCESS-SEQUENCE / TYP）
                System.Console.WriteLine("====================== VARIABLENHAUSHALT ======================");
                System.Console.WriteLine("SYMBOLIC-NAME/ACCESS-SEQUENCE/TYP");
                for (int i = 0; i < vars.Count; i++)
                {
                    System.Console.WriteLine(String.Format("{0,-16} / {1,-24} / {2,-10}",
                        vars[i].Name, vars[i].AccessSequence, Softdatatype.Types[vars[i].Softdatatype]));
                }
                System.Console.WriteLine("===============================================================");

                try
                {
                    // 交互式控制台下暂停等待按键；输入被重定向（管道/文件）时跳过
                    System.Console.ReadKey();
                }
                catch (InvalidOperationException)
                {
                }
                // 5) 关闭工程
                project.Close();
                System.Console.WriteLine("Main - ENDE");
                return 0;
            }
            catch (Exception ex)
            {
                System.Console.Error.WriteLine("错误: " + ex.Message);
                return 1;
            }
        }
    }
}
