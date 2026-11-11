using System;
using System.Text;
using System.Windows.Forms;
using S7ProjectParser;

namespace S7ProjectParser.GUI
{
    /// <summary>
    /// 主窗口：左上为"工程"分组框（路径 + 打开/关闭），右上为"变量"分组框
    /// （符号 + 查找 / 数据类型 / 绝对地址），下方为带类型图标的变量树，底部为状态标签。
    ///
    /// 调用习惯与 TiaProjectParser 的 TiaProjectExplorer 一致：
    /// 树由 S7ProjectExplorer.Open 打开后从 GetRootObjects() 的节点树直接构建
    /// （S7 解析模型全量在内存中，一次性建树，无懒加载占位）；
    /// 树以**站点名称**为根节点（多站点工程有多个根，无站点名时回退设备名），
    /// 每个站点根下挂 DB 块节点与符号区域节点；节点名称只显示变量名
    /// （数组元素带下标）；选中节点时在变量分组框显示数据类型与通讯绝对地址
    /// （s7netplus 点号语法，如 "DB1.DBX8.0"）。
    /// STEP 7 符号表地址（"DB1:8.0"）是解析模型内部格式，不对外显示。
    /// 在"符号"框中键入路径回车可定位树节点（容忍根名首尾引号的差异）。
    /// </summary>
    public partial class MainForm : Form
    {
        /// <summary>工程解析器（using/Dispose 语义；未打开时为 null）。</summary>
        private S7ProjectExplorer _explorer;

        /// <summary>全局状态文本（非设备节点被选中时状态栏恢复显示它）。</summary>
        private string _lastGlobalStatus = "未打开工程";

        public MainForm()
        {
            InitializeComponent();
            tbSymbol.KeyDown += tbSymbol_KeyDown;
        }

        public MainForm(string initialPath)
            : this()
        {
            if (!string.IsNullOrEmpty(initialPath))
                OpenProject(initialPath);
        }

        private void setStatus(string status)
        {
            lbStatus.Text = status;
            statusStrip1.Refresh();
        }

        /// <summary>设置全局状态（同时记录，供节点选中时恢复显示）。</summary>
        private void setGlobalStatus(string status)
        {
            _lastGlobalStatus = status;
            setStatus(status);
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "打开 STEP 7 工程文件";
                dialog.Filter = "STEP 7 工程 (*.s7p)|*.s7p|所有文件 (*.*)|*.*";
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                OpenProject(dialog.FileName);
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            CloseExplorer();
            treeView1.Nodes.Clear();
            tbSymbol.Text = "";
            tbDataType.Text = "";
            tbAddress.Text = "";
            setGlobalStatus("未打开工程");
        }

        private void CloseExplorer()
        {
            if (_explorer != null)
            {
                _explorer.Dispose();
                _explorer = null;
            }
        }

        private void OpenProject(string path)
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                setGlobalStatus("正在打开工程: " + path + " ...");
                Refresh();

                CloseExplorer();
                _explorer = S7ProjectExplorer.Open(path);

                BuildRootTree();

                int deviceCount = _explorer.GetRootObjects().Count;
                int leafCount = 0;
                int warningCount = _explorer.Project.Warnings.Count;
                foreach (PlcDevice device in _explorer.Project.Devices)
                {
                    warningCount += device.Warnings.Count;
                }
                foreach (S7Node root in _explorer.GetRootObjects())
                {
                    foreach (S7Node node in _explorer.EnumerateTree(root, 100))
                    {
                        if (node.Kind == S7NodeKind.Variable) leafCount++;
                    }
                }
                setGlobalStatus(string.Format("已打开 {0}: {1} 个设备, {2} 个变量, {3} 条警告",
                    _explorer.ProjectName, deviceCount, leafCount, warningCount));
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "打开工程失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                setGlobalStatus("打开失败: " + path);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        /// <summary>
        /// 构建整棵树：每个站点一个根节点（以站点名称命名，无站点名称时回退
        /// 设备名），其下为 DB 块节点、符号区域节点与警告节点，变量子树一次建全。
        /// </summary>
        private void BuildRootTree()
        {
            treeView1.BeginUpdate();
            treeView1.Nodes.Clear();

            foreach (S7Node root in _explorer.GetRootObjects())
            {
                PlcDevice device = root.Source as PlcDevice;
                string stationText = (device != null && !string.IsNullOrEmpty(device.StationName))
                    ? device.StationName
                    : root.Name;
                TreeNode stationNode = new TreeNode(stationText);
                stationNode.Tag = root;
                SetImageKey(stationNode, "FolderTopPanel");
                treeView1.Nodes.Add(stationNode);

                foreach (S7Node child in root.Children)
                {
                    stationNode.Nodes.Add(BuildTreeNode(child));
                }
            }

            // 单站点时直接展开根节点，让数据块/区域立即可见
            if (treeView1.Nodes.Count == 1)
            {
                treeView1.Nodes[0].Expand();
            }

            treeView1.EndUpdate();
        }

        /// <summary>按 S7Node 递归构建 TreeNode（变量子树一次建全）。</summary>
        private TreeNode BuildTreeNode(S7Node node)
        {
            TreeNode treeNode = new TreeNode(node.Name);
            treeNode.Tag = node;

            switch (node.Kind)
            {
                case S7NodeKind.Datablock:
                    SetImageKey(treeNode, "Datablock");
                    break;
                case S7NodeKind.Struct:
                    SetImageKey(treeNode, "Structure");
                    break;
                case S7NodeKind.Variable:
                case S7NodeKind.Array:
                    SetImageKeyBySoftdatatype(treeNode, node.Kind == S7NodeKind.Array
                        ? ArrayElementSoftdatatype(node)
                        : node.Softdatatype);
                    break;
                default:
                    SetImageKey(treeNode, "Default");
                    break;
            }

            foreach (S7Node child in node.Children)
            {
                treeNode.Nodes.Add(BuildTreeNode(child));
            }
            return treeNode;
        }

        /// <summary>数组节点的图标按第一个叶子后代的软数据类型取。</summary>
        private static uint ArrayElementSoftdatatype(S7Node node)
        {
            foreach (S7Node child in node.Children)
            {
                if (child.Kind == S7NodeKind.Variable) return child.Softdatatype;
                if (child.IsContainer)
                {
                    uint sd = ArrayElementSoftdatatype(child);
                    if (sd != Softdatatype.S7COMMP_SOFTDATATYPE_UDEFINED) return sd;
                }
            }
            return Softdatatype.S7COMMP_SOFTDATATYPE_UDEFINED;
        }

        // 变量树一次性建全，无懒加载占位；AfterExpand 仍由设计器订阅，保留空实现。
        private void treeView1_AfterExpand(object sender, TreeViewEventArgs e)
        {
        }

        private void tbSymbol_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            string symbol = tbSymbol.Text.Trim();
            if (symbol.Length == 0) return;

            TreeNode found = FindNodeBySymbol(treeView1.Nodes, symbol);
            if (found == null)
            {
                setStatus("找不到变量: " + symbol);
                return;
            }
            treeView1.SelectedNode = found;
            found.EnsureVisible();
            treeView1.Focus();
        }

        /// <summary>在树中按符号名查找节点（容忍 "DB1.X" 全路径与块内路径两种写法）。</summary>
        private TreeNode FindNodeBySymbol(TreeNodeCollection nodes, string symbol)
        {
            string key = symbol.Trim('"');
            foreach (TreeNode node in nodes)
            {
                S7Node info = node.Tag as S7Node;
                if (info != null && !string.IsNullOrEmpty(info.Symbol))
                {
                    string full = FullSymbolOf(node, info);
                    if (info.Symbol == symbol || info.Symbol.Trim('"') == key
                        || full == symbol || full == key
                        || info.Symbol.EndsWith("." + key, StringComparison.Ordinal))
                    {
                        return node;
                    }
                }

                TreeNode f = FindNodeBySymbol(node.Nodes, symbol);
                if (f != null) return f;
            }
            return null;
        }

        /// <summary>变量的完整符号路径（带 DB/区域根前缀，如 "DB1.SAMPLE_ARRAY[3]"、"MArea.PLC2_BOOL1"）。</summary>
        private static string FullSymbolOf(TreeNode treeNode, S7Node info)
        {
            TreeNode parent = treeNode.Parent;
            while (parent != null)
            {
                S7Node p = parent.Tag as S7Node;
                if (p != null && p.Kind == S7NodeKind.Datablock)
                {
                    int dbNumber;
                    if (S7ProjectExplorer.TryExtractDbNumber(p.Name, out dbNumber))
                    {
                        return "DB" + dbNumber + "." + info.Symbol;
                    }
                    return info.Symbol;
                }
                if (p != null && p.Kind == S7NodeKind.SymbolArea)
                {
                    var area = p.Source as PlcSymbolArea;
                    if (area != null)
                    {
                        switch (area.Name)
                        {
                            case "Inputs": return "IArea." + info.Symbol;
                            case "Outputs": return "QArea." + info.Symbol;
                            case "Merker": return "MArea." + info.Symbol;
                            case "S7Timers": return "S7Timers." + info.Symbol;
                            case "S7Counters": return "S7Counters." + info.Symbol;
                        }
                    }
                    return info.Symbol;
                }
                parent = parent.Parent;
            }
            return info.Symbol;
        }

        /// <summary>按软数据类型取树图标键。</summary>
        private static void SetImageKeyBySoftdatatype(TreeNode tn, uint softdatatype)
        {
            string key;
            switch (softdatatype)
            {
                case Softdatatype.S7COMMP_SOFTDATATYPE_BOOL:
                case Softdatatype.S7COMMP_SOFTDATATYPE_BBOOL:
                    key = "Boolean";
                    break;
                case Softdatatype.S7COMMP_SOFTDATATYPE_BYTE:
                case Softdatatype.S7COMMP_SOFTDATATYPE_WORD:
                case Softdatatype.S7COMMP_SOFTDATATYPE_DWORD:
                case Softdatatype.S7COMMP_SOFTDATATYPE_LWORD:
                    key = "Binary2";
                    break;
                case Softdatatype.S7COMMP_SOFTDATATYPE_CHAR:
                case Softdatatype.S7COMMP_SOFTDATATYPE_WCHAR:
                    key = "Char";
                    break;
                case Softdatatype.S7COMMP_SOFTDATATYPE_INT:
                case Softdatatype.S7COMMP_SOFTDATATYPE_DINT:
                case Softdatatype.S7COMMP_SOFTDATATYPE_SINT:
                case Softdatatype.S7COMMP_SOFTDATATYPE_USINT:
                case Softdatatype.S7COMMP_SOFTDATATYPE_UINT:
                case Softdatatype.S7COMMP_SOFTDATATYPE_UDINT:
                case Softdatatype.S7COMMP_SOFTDATATYPE_LINT:
                case Softdatatype.S7COMMP_SOFTDATATYPE_ULINT:
                    key = "Integer2";
                    break;
                case Softdatatype.S7COMMP_SOFTDATATYPE_REAL:
                case Softdatatype.S7COMMP_SOFTDATATYPE_LREAL:
                    key = "Number2";
                    break;
                case Softdatatype.S7COMMP_SOFTDATATYPE_STRING:
                case Softdatatype.S7COMMP_SOFTDATATYPE_WSTRING:
                    key = "Text";
                    break;
                case Softdatatype.S7COMMP_SOFTDATATYPE_DATE:
                    key = "Date";
                    break;
                case Softdatatype.S7COMMP_SOFTDATATYPE_TIMEOFDAY:
                    key = "Time";
                    break;
                case Softdatatype.S7COMMP_SOFTDATATYPE_TIME:
                case Softdatatype.S7COMMP_SOFTDATATYPE_S5TIME:
                    key = "Timer";
                    break;
                case Softdatatype.S7COMMP_SOFTDATATYPE_DATEANDTIME:
                    key = "Datetime";
                    break;
                case Softdatatype.S7COMMP_SOFTDATATYPE_POINTER:
                case Softdatatype.S7COMMP_SOFTDATATYPE_ANY:
                    key = "Any";
                    break;
                case Softdatatype.S7COMMP_SOFTDATATYPE_STRUCT:
                    key = "Structure";
                    break;
                default:
                    key = "Tag";
                    break;
            }
            SetImageKey(tn, key);
        }

        private static void SetImageKey(TreeNode tn, string key)
        {
            tn.ImageKey = key;
            tn.SelectedImageKey = tn.ImageKey;
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node == null) return;
            S7Node info = e.Node.Tag as S7Node;
            if (info == null)
            {
                tbSymbol.Text = "";
                tbDataType.Text = "";
                tbAddress.Text = "";
                setStatus(_lastGlobalStatus);
                return;
            }
            tbSymbol.Text = info.Symbol != null ? FullSymbolOf(e.Node, info) : "";
            tbDataType.Text = info.DataType ?? "";
            tbAddress.Text = info.ComAddress ?? "";

            // 选中设备节点：状态栏显示该设备的详细信息；其他节点恢复工程总体统计
            setStatus(info.Kind == S7NodeKind.Device ? BuildDeviceStatus(info) : _lastGlobalStatus);
        }

        /// <summary>设备详细信息（状态栏单行文本：设备名/站点/CPU/订货号/固件/槽位）。</summary>
        private string BuildDeviceStatus(S7Node deviceNode)
        {
            PlcDevice device = deviceNode.Source as PlcDevice;
            if (device == null) return _lastGlobalStatus;

            var sb = new StringBuilder();
            sb.Append(device.Name);
            if (!string.IsNullOrEmpty(device.StationName))
            {
                sb.Append(" | 站点: ").Append(device.StationName);
            }
            if (!string.IsNullOrEmpty(device.CpuName))
            {
                sb.Append(" | CPU: ").Append(device.CpuName);
            }
            if (!string.IsNullOrEmpty(device.CpuMlfb))
            {
                sb.Append(" (").Append(device.CpuMlfb).Append(")");
            }
            if (!string.IsNullOrEmpty(device.CpuFirmware))
            {
                sb.Append(" | 固件: ").Append(device.CpuFirmware);
            }
            if (device.Slot > 0)
            {
                sb.Append(" | 槽位: ").Append(device.Slot.ToString());
            }
            return sb.ToString();
        }
    }
}
