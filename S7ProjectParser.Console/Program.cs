using System;
using System.Text;
using S7ProjectParser;
// 命名空间 S7ProjectParser.Console 与 System.Console 同名冲突，控制台调用用 System.Console 限定

namespace S7ProjectParser.Console
{
    /// <summary>
    /// 无界面测试工具，调用习惯与 TiaProjectParser 的 TiaProjectExplorer 一致：
    ///   Open（静态工厂，异常报错）→ using 内遍历变量树 → Dispose
    /// 打印每个设备的变量表（SYMBOLIC-NAME / ACCESS-SEQUENCE / TYP：
    /// 完整符号路径 / 通讯绝对地址 / 软数据类型，不支持的类型跳过）。
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

            try
            {
                using (S7ProjectExplorer explorer = S7ProjectExplorer.Open(args[0]))
                {
                    System.Console.WriteLine("Main - Open 完成: " + explorer.ProjectName);
                    System.Console.WriteLine("Main - Starte Browse...");

                    int total = 0;
                    foreach (S7Node device in explorer.GetRootObjects())
                    {
                        System.Console.WriteLine("== " + device.Name + " ==");
                        System.Console.WriteLine("====================== VARIABLENHAUSHALT ======================");
                        System.Console.WriteLine("SYMBOLIC-NAME/ACCESS-SEQUENCE/TYP");
                        foreach (S7Node child in device.Children)
                        {
                            string prefix = RootPrefixOf(child);
                            if (prefix == null) continue; // 警告节点等
                            foreach (S7Node node in explorer.EnumerateTree(child, 20))
                            {
                                if (node.Kind != S7NodeKind.Variable) continue;
                                if (node.Softdatatype == Softdatatype.S7COMMP_SOFTDATATYPE_UDEFINED) continue;
                                string typeName;
                                if (!Softdatatype.Types.TryGetValue(node.Softdatatype, out typeName))
                                {
                                    typeName = "0x" + node.Softdatatype.ToString("X");
                                }
                                System.Console.WriteLine(String.Format("{0,-16} / {1,-24} / {2,-10}",
                                    prefix + "." + node.Symbol, node.ComAddress, typeName));
                                total++;
                            }
                        }
                        System.Console.WriteLine("===============================================================");
                    }
                    System.Console.WriteLine("Main - Browse 共 " + total + " 条");
                }

                try
                {
                    // 交互式控制台下暂停等待按键；输入被重定向（管道/文件）时跳过
                    System.Console.ReadKey();
                }
                catch (InvalidOperationException)
                {
                }
                System.Console.WriteLine("Main - ENDE");
                return 0;
            }
            catch (Exception ex)
            {
                System.Console.Error.WriteLine("错误: " + ex.Message);
                return 1;
            }
        }

        /// <summary>块/区域节点的变量表根前缀（DB1、IArea、MArea 等）；非变量容器返回 null。</summary>
        private static string RootPrefixOf(S7Node node)
        {
            if (node.Kind == S7NodeKind.Datablock)
            {
                int dbNumber;
                return S7ProjectExplorer.TryExtractDbNumber(node.Name, out dbNumber)
                    ? "DB" + dbNumber
                    : null;
            }
            if (node.Kind == S7NodeKind.SymbolArea)
            {
                var area = node.Source as PlcSymbolArea;
                if (area == null) return null;
                switch (area.Name)
                {
                    case "Inputs": return "IArea";
                    case "Outputs": return "QArea";
                    case "Merker": return "MArea";
                    case "S7Timers": return "S7Timers";
                    case "S7Counters": return "S7Counters";
                    default: return null;
                }
            }
            return null;
        }
    }
}
