using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using S7ProjectParser;
// S7ProjectParser 既是命名空间又是类名，在命名空间外部用别名引用类型
using S7Parser = S7ProjectParser.S7ProjectParser;

namespace S7ProjectParser.GUI
{
    /// <summary>
    /// 主窗口：参照 S7CommPlusGUIBrowser 的布局与交互设计 ——
    /// 左上为"工程"分组框（路径 + 打开/关闭），右上为"变量"分组框
    /// （符号 + 查找 / 数据类型 / 绝对地址），下方为带类型图标的变量树，底部为状态标签。
    ///
    /// 与 GUIBrowser 相同的交互模式：
    ///   - 树通过 S7Project 解析接口构建（GetListOfDatablocks +
    ///     getTypeInfoByRelId 懒加载），不是直接遍历解析模型；
    ///   - 树以**站点名称**为根节点（多站点工程有多个根，无站点名时回退
    ///     设备名），每个站点根下挂 DB 块节点与符号区域节点；
    ///   - DB 块节点（Datablock 图标）与 5 个区域节点（Inputs/Outputs/Merker/
    ///     S7Timers/S7Counters，Default 图标）都先挂 "Loading..." 占位子节点，
    ///     展开时通过 AfterExpand 懒加载；
    ///   - 数组按 "[j]" / "[i,j]" 展开为元素节点；结构实例元素挂
    ///     "Loading..." 并在展开时按 relId 加载；
    /// 节点名称只显示变量名（数组元素带下标）；选中节点时在变量分组框
    /// 显示数据类型与通讯绝对地址（s7netplus 点号语法，如 "DB1.DBX8.0"）。
    /// STEP 7 符号表地址（"DB1:8.0"）是解析模型内部格式，不对外显示。
    /// </summary>
    public partial class MainForm : Form
    {
        /// <summary>工程解析器（打开 STEP 7 工程并输出变量表解析结果）。</summary>
        private readonly S7Project _project = new S7Project();

        /// <summary>全局状态文本（非设备节点被选中时状态栏恢复显示它）。</summary>
        private string _lastGlobalStatus = "未打开工程";

        private static readonly uint[] AreaRelIds =
        {
            S7Parser.RelIdInputs,
            S7Parser.RelIdOutputs,
            S7Parser.RelIdMerker,
            S7Parser.RelIdS7Timers,
            S7Parser.RelIdS7Counters
        };

        public MainForm()
        {
            InitializeComponent();
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
            string path = "";
            if (path == "")
            {
                _project.Close();
                using (var dialog = new OpenFileDialog())
                {
                    dialog.Title = "打开 STEP 7 工程文件";
                    dialog.Filter = "STEP 7 工程 (*.s7p)|*.s7p|所有文件 (*.*)|*.*";
                    if (dialog.ShowDialog(this) != DialogResult.OK) return;
                    path = dialog.FileName;
                }
            }
            OpenProject(path);
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            _project.Close();
            treeView1.Nodes.Clear();
            tbSymbol.Text = "";
            tbDataType.Text = "";
            tbAddress.Text = "";
            setGlobalStatus("未打开工程");
        }

        /// <summary>
        /// 在树中按符号名查找节点；遇到未展开的懒加载节点时先加载再继续
        /// （容忍根名首尾引号的差异）。
        /// </summary>
        private TreeNode FindNodeBySymbol(TreeNodeCollection nodes, string symbol)
        {
            string key = symbol.Trim('"');
            foreach (TreeNode node in nodes)
            {
                NodeInfo info = node.Tag as NodeInfo;
                if (info != null && info.IsVariable && !string.IsNullOrEmpty(info.Symbol)
                    && (info.Symbol == symbol || info.Symbol.Trim('"') == key
                        || info.Symbol.EndsWith("." + key, StringComparison.Ordinal)
                        || info.Symbol.EndsWith("." + symbol, StringComparison.Ordinal)))
                {
                    return node;
                }

                if (info != null && !info.ChildrenLoaded && info.RelId != 0
                    && node.Nodes.Count == 1 && node.Nodes[0].Text == "Loading...")
                {
                    LoadNodeChildren(node, info);
                    TreeNode found = FindNodeBySymbol(node.Nodes, symbol);
                    if (found != null) return found;
                    continue;
                }

                TreeNode f = FindNodeBySymbol(node.Nodes, symbol);
                if (f != null) return f;
            }
            return null;
        }

        private void OpenProject(string path)
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                setGlobalStatus("正在打开工程: " + path + " ...");
                Refresh();

                int result = _project.Open(path);
                if (result != 0)
                {
                    setGlobalStatus("打开失败: " + S7Project.ErrorText(result));
                    return;
                }

                BuildRootTree();

                int deviceCount = _project.GetDeviceCount();
                int leafCount = 0;
                int warningCount = _project.Project.Warnings.Count;
                foreach (PlcDevice device in _project.Project.Devices)
                {
                    leafCount += CountLeavesOfDevice(device);
                    warningCount += device.Warnings.Count;
                }
                setGlobalStatus(string.Format("已打开 {0}: {1} 个设备, {2} 个变量, {3} 条警告",
                    _project.Project.Name, deviceCount, leafCount, warningCount));
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
        /// 构建根树：每个站点一个根节点（以站点名称命名，多站点工程有多个根），
        /// 每个站点根节点下挂 DB 块节点与 5 个符号区域节点，
        /// 各带 "Loading..." 占位（与 GUIBrowser 一致）。
        /// </summary>
        private void BuildRootTree()
        {
            treeView1.BeginUpdate();
            treeView1.Nodes.Clear();

            PlcProject project = _project.Project;
            for (int deviceIndex = 0; deviceIndex < project.Devices.Count; deviceIndex++)
            {
                PlcDevice device = project.Devices[deviceIndex];
                _project.SetActiveDevice(deviceIndex);

                // 根节点 = 站点名称（无站点名称时回退设备名）；选中时状态栏显示设备详细信息
                string stationText = string.IsNullOrEmpty(device.StationName)
                    ? device.Name
                    : device.StationName;
                TreeNode stationNode = new TreeNode(stationText);
                stationNode.Tag = new NodeInfo
                {
                    DeviceIndex = deviceIndex,
                    IsDevice = true,
                    ChildrenLoaded = true
                };
                SetImageKey(stationNode, "FolderTopPanel");
                treeView1.Nodes.Add(stationNode);

                // 数据块节点（Tag = db_block_ti_relid，与 GUIBrowser 一致）
                List<S7Project.DatablockInfo> dbInfoList;
                _project.GetListOfDatablocks(out dbInfoList);
                foreach (S7Project.DatablockInfo db in dbInfoList)
                {
                    TreeNode dbNode = new TreeNode(db.db_name);
                    dbNode.Tag = new NodeInfo
                    {
                        IsVariable = true,
                        Symbol = "DB" + db.db_number.ToString(),
                        Address = "DB" + db.db_number.ToString() + ".DBB0",
                        DataType = "数据块",
                        DeviceIndex = deviceIndex,
                        RelId = db.db_block_ti_relid
                    };
                    SetImageKey(dbNode, "Datablock");
                    dbNode.Nodes.Add(new TreeNode("Loading..."));
                    stationNode.Nodes.Add(dbNode);
                }

                // 符号区域节点（固定 relId，与 GUIBrowser 一致）
                foreach (uint areaRelId in AreaRelIds)
                {
                    PlcSymbolArea area = GetAreaOfDevice(device, areaRelId);
                    if (area == null) continue;

                    string text = area.Name;
                    if (area.Variables.Count > 0)
                    {
                        text += " (" + area.Variables.Count.ToString() + " 个符号)";
                    }
                    TreeNode areaNode = new TreeNode(text);
                    areaNode.Tag = new NodeInfo
                    {
                        DeviceIndex = deviceIndex,
                        RelId = areaRelId
                    };
                    SetImageKey(areaNode, "Default");
                    areaNode.Nodes.Add(new TreeNode("Loading..."));
                    stationNode.Nodes.Add(areaNode);
                }

                if (device.Warnings.Count > 0)
                {
                    TreeNode warningNode = new TreeNode(string.Format("设备警告 ({0})", device.Warnings.Count));
                    warningNode.Tag = new NodeInfo
                    {
                        DeviceIndex = deviceIndex,
                        ChildrenLoaded = true
                    };
                    SetImageKey(warningNode, "Default");
                    stationNode.Nodes.Add(warningNode);
                }
            }

            _project.SetActiveDevice(0);

            // 工程级警告挂在第一个站点根节点下（总数同时显示在状态栏）
            if (project.Warnings.Count > 0 && treeView1.Nodes.Count > 0)
            {
                TreeNode warningNode = new TreeNode(string.Format("工程警告 ({0})", project.Warnings.Count));
                warningNode.Tag = new NodeInfo
                {
                    ChildrenLoaded = true
                };
                SetImageKey(warningNode, "Default");
                treeView1.Nodes[0].Nodes.Add(warningNode);
            }

            // 单站点时直接展开根节点，让数据块/区域立即可见（多站点时各根节点本就可见）
            if (treeView1.Nodes.Count == 1)
            {
                treeView1.Nodes[0].Expand();
            }

            treeView1.EndUpdate();
        }

        /// <summary>
        /// 懒加载节点展开（镜像 GUIBrowser 的 AfterExpand 流程）：
        /// 通过 getTypeInfoByRelId 取类型信息，按 VarnameList 建子节点，
        /// 数组按元素展开、结构元素再挂 "Loading..."。
        /// </summary>
        private void treeView1_AfterExpand(object sender, TreeViewEventArgs e)
        {
            NodeInfo info = e.Node.Tag as NodeInfo;
            if (info == null || info.RelId == 0 || info.ChildrenLoaded) return;
            if (e.Node.Nodes.Count != 1 || e.Node.Nodes[0].Text != "Loading...") return;
            LoadNodeChildren(e.Node, info);
        }

        private void LoadNodeChildren(TreeNode node, NodeInfo info)
        {
            try
            {
                _project.SetActiveDevice(info.DeviceIndex);
                PObject ti = _project.getTypeInfoByRelId(info.RelId);
                if (ti == null)
                {
                    node.Nodes.Clear();
                    info.ChildrenLoaded = true;
                    setStatus("无法取得类型信息: relId=0x" + info.RelId.ToString("X8"));
                    return;
                }

                node.Nodes.Clear();
                AddTypeInfoChildren(node, ti, info.DeviceIndex);
                info.ChildrenLoaded = true;
            }
            catch (Exception ex)
            {
                setStatus("加载节点失败: " + ex.Message);
            }
        }

        /// <summary>按类型信息（VarnameList + VartypeList）添加子节点。</summary>
        private void AddTypeInfoChildren(TreeNode parent, PObject ti, int deviceIndex)
        {
            if (ti.VarnameList == null || ti.VartypeList == null) return;
            List<PVartypeListElement> elements = ti.VartypeList.Elements;
            List<string> names = ti.VarnameList.Names;

            for (int i = 0; i < elements.Count && i < names.Count; i++)
            {
                PVartypeListElement el = elements[i];
                TreeNode child;
                if (el.OffsetInfoType.Is1Dim() || el.OffsetInfoType.IsMDim())
                {
                    child = BuildArrayNode(names[i], el, deviceIndex);
                }
                else if (el.OffsetInfoType.HasRelation())
                {
                    child = BuildStructInstanceNode(names[i], el, deviceIndex);
                }
                else
                {
                    child = BuildLeafNode(names[i], el, deviceIndex);
                }
                if (child != null) parent.Nodes.Add(child);
            }
        }

        /// <summary>叶子节点：仅变量名（数据类型与通讯绝对地址在选中时显示于变量分组框），图标按软数据类型。</summary>
        private TreeNode BuildLeafNode(string name, PVartypeListElement el, int deviceIndex)
        {
            PlcVariableNode model = el.SourceNode;
            string dataType = model != null ? model.DataType : "";
            string address = GetComAddressOf(model);

            TreeNode node = new TreeNode(name);
            node.Tag = new NodeInfo
            {
                IsVariable = true,
                Symbol = model != null ? model.Name : name,
                Address = address,
                DataType = dataType,
                DeviceIndex = deviceIndex,
                ChildrenLoaded = true
            };
            SetImageKeyBySoftdatatype(node, el.Softdatatype);
            return node;
        }

        /// <summary>结构实例节点：仅名称（数据类型在选中时显示），带 "Loading..." 占位（Tag = relId）。</summary>
        private TreeNode BuildStructInstanceNode(string name, PVartypeListElement el, int deviceIndex)
        {
            PlcVariableNode model = el.SourceNode;
            uint relId = ((IOffsetInfoType_Relation)el.OffsetInfoType).GetRelationId();
            string dataType = model != null ? model.DataType : "";

            TreeNode node = new TreeNode(name);
            node.Tag = new NodeInfo
            {
                IsVariable = true,
                Symbol = model != null ? model.Name : name,
                Address = GetFirstLeafComAddress(model),
                DataType = dataType,
                DeviceIndex = deviceIndex,
                RelId = relId,
                ModelNode = model
            };
            SetImageKey(node, "Structure");
            node.Nodes.Add(new TreeNode("Loading..."));
            return node;
        }

        /// <summary>
        /// 数组节点：按 "[j]" / "[i,j]" 展开元素（与 GUIBrowser 一致）。
        /// 元素信息取自解析模型（SourceNode.Children），每个元素都有独立
        /// 通讯绝对地址（选中时显示）；结构元素再挂 "Loading..."。
        /// </summary>
        private TreeNode BuildArrayNode(string name, PVartypeListElement el, int deviceIndex)
        {
            PlcVariableNode arrayModel = el.SourceNode;
            string dataType = arrayModel != null ? arrayModel.DataType : "";

            TreeNode node = new TreeNode(name);
            node.Tag = new NodeInfo
            {
                IsVariable = true,
                Symbol = arrayModel != null ? arrayModel.Name : name,
                Address = GetFirstLeafComAddress(arrayModel),
                DataType = dataType,
                DeviceIndex = deviceIndex,
                ChildrenLoaded = true,
                ModelNode = arrayModel
            };
            SetImageKeyBySoftdatatype(node, el.Softdatatype);

            if (arrayModel != null && arrayModel.Children != null && arrayModel.Children.Count > 0)
            {
                foreach (PlcVariableNode element in arrayModel.Children)
                {
                    node.Nodes.Add(BuildArrayElementNode(element, deviceIndex));
                }
            }
            else
            {
                // 防御：没有模型元素时按维度信息生成占位元素
                int count = el.OffsetInfoType.Is1Dim()
                    ? (int)((IOffsetInfoType_1Dim)el.OffsetInfoType).GetArrayElementCount()
                    : (int)((IOffsetInfoType_MDim)el.OffsetInfoType).GetArrayElementCount();
                for (int k = 0; k < count; k++)
                {
                    node.Nodes.Add(new TreeNode(name + "[" + k.ToString() + "]"));
                }
            }
            return node;
        }

        /// <summary>数组元素节点：叶子直接显示，结构元素挂 "Loading..." 懒加载。</summary>
        private TreeNode BuildArrayElementNode(PlcVariableNode element, int deviceIndex)
        {
            string display = LastSegmentName(element.Name);
            string dataType = element.DataType;

            TreeNode node = new TreeNode(display);
            NodeInfo info = new NodeInfo
            {
                IsVariable = true,
                Symbol = element.Name,
                Address = element.IsContainer ? GetFirstLeafComAddress(element) : GetComAddressOf(element),
                DataType = dataType,
                DeviceIndex = deviceIndex,
                ChildrenLoaded = true,
                ModelNode = element
            };
            node.Tag = info;

            if (element.IsContainer)
            {
                uint relId = _project.getTypeInfoRelIdOf(element);
                info.RelId = relId;
                SetImageKey(node, "Structure");
                if (relId != 0)
                {
                    info.ChildrenLoaded = false;
                    node.Nodes.Add(new TreeNode("Loading..."));
                }
                return node;
            }

            SetImageKeyBySoftdatatype(node, MapDataTypeToSoftdatatype(dataType));
            return node;
        }

        private static PlcSymbolArea GetAreaOfDevice(PlcDevice device, uint relId)
        {
            foreach (PlcSymbolArea area in device.SymbolAreas)
            {
                if (area.RelId == relId) return area;
            }
            return null;
        }

        /// <summary>
        /// 节点的通讯绝对地址（s7netplus 点号语法，如 "DB1.DBX8.0"）：
        /// 优先取解析模型格式化好的 ComAddress，缺失时按数据类型现场格式化。
        /// 模型中的 STEP 7 地址（node.Address，如 "DB1:8.0"）为内部格式，不直接显示。
        /// </summary>
        private static string GetComAddressOf(PlcVariableNode model)
        {
            if (model == null) return "";
            if (!string.IsNullOrEmpty(model.ComAddress)) return model.ComAddress;
            if (string.IsNullOrEmpty(model.Address)) return "";
            return ItemAddress.FormatComAddress(model.Address, MapDataTypeToSoftdatatype(model.DataType));
        }

        /// <summary>容器节点（数组/结构实例）的起始地址 = 第一个叶子后代的通讯绝对地址。</summary>
        private static string GetFirstLeafComAddress(PlcVariableNode node)
        {
            if (node == null) return "";
            if (!node.IsContainer) return GetComAddressOf(node);
            if (node.Children == null) return "";
            foreach (PlcVariableNode child in node.Children)
            {
                string address = GetFirstLeafComAddress(child);
                if (!string.IsNullOrEmpty(address)) return address;
            }
            return "";
        }

        /// <summary>名字的最后一段（去掉父级前缀）。</summary>
        private static string LastSegmentName(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            int dot = name.LastIndexOf('.');
            return dot >= 0 ? name.Substring(dot + 1) : name;
        }

        /// <summary>按软数据类型取树图标键（映射与 S7CommPlusGUIBrowser 一致）。</summary>
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

        /// <summary>模型数据类型文本 -> 软数据类型（用于数组元素图标）。</summary>
        private static uint MapDataTypeToSoftdatatype(string dataType)
        {
            string t = (dataType ?? "").Trim().ToUpperInvariant();
            if (t.StartsWith("STRING", StringComparison.Ordinal)) return Softdatatype.S7COMMP_SOFTDATATYPE_STRING;
            if (t == "STRUCT" || t.StartsWith("UDT ", StringComparison.Ordinal)
                || t.StartsWith("FB ", StringComparison.Ordinal))
            {
                return Softdatatype.S7COMMP_SOFTDATATYPE_STRUCT;
            }
            switch (t)
            {
                case "BOOL": return Softdatatype.S7COMMP_SOFTDATATYPE_BOOL;
                case "BYTE": return Softdatatype.S7COMMP_SOFTDATATYPE_BYTE;
                case "CHAR": return Softdatatype.S7COMMP_SOFTDATATYPE_CHAR;
                case "WORD": return Softdatatype.S7COMMP_SOFTDATATYPE_WORD;
                case "INT": return Softdatatype.S7COMMP_SOFTDATATYPE_INT;
                case "DWORD": return Softdatatype.S7COMMP_SOFTDATATYPE_DWORD;
                case "DINT": return Softdatatype.S7COMMP_SOFTDATATYPE_DINT;
                case "REAL": return Softdatatype.S7COMMP_SOFTDATATYPE_REAL;
                case "DATE": return Softdatatype.S7COMMP_SOFTDATATYPE_DATE;
                case "TIME_OF_DAY":
                case "TOD": return Softdatatype.S7COMMP_SOFTDATATYPE_TIMEOFDAY;
                case "TIME": return Softdatatype.S7COMMP_SOFTDATATYPE_TIME;
                case "S5TIME": return Softdatatype.S7COMMP_SOFTDATATYPE_S5TIME;
                case "DATE_AND_TIME":
                case "DT": return Softdatatype.S7COMMP_SOFTDATATYPE_DATEANDTIME;
                case "POINTER": return Softdatatype.S7COMMP_SOFTDATATYPE_POINTER;
                case "ANY": return Softdatatype.S7COMMP_SOFTDATATYPE_ANY;
                case "LREAL": return Softdatatype.S7COMMP_SOFTDATATYPE_LREAL;
                case "ULINT": return Softdatatype.S7COMMP_SOFTDATATYPE_ULINT;
                case "LINT": return Softdatatype.S7COMMP_SOFTDATATYPE_LINT;
                case "LWORD": return Softdatatype.S7COMMP_SOFTDATATYPE_LWORD;
                case "USINT": return Softdatatype.S7COMMP_SOFTDATATYPE_USINT;
                case "UINT": return Softdatatype.S7COMMP_SOFTDATATYPE_UINT;
                case "UDINT": return Softdatatype.S7COMMP_SOFTDATATYPE_UDINT;
                case "SINT": return Softdatatype.S7COMMP_SOFTDATATYPE_SINT;
                case "WCHAR": return Softdatatype.S7COMMP_SOFTDATATYPE_WCHAR;
                case "WSTRING": return Softdatatype.S7COMMP_SOFTDATATYPE_WSTRING;
                case "LTIME": return Softdatatype.S7COMMP_SOFTDATATYPE_LTIME;
                case "LTIME_OF_DAY":
                case "LTOD": return Softdatatype.S7COMMP_SOFTDATATYPE_LTOD;
                case "LDT": return Softdatatype.S7COMMP_SOFTDATATYPE_LDT;
                case "DTL": return Softdatatype.S7COMMP_SOFTDATATYPE_DTL;
                case "COUNTER":
                case "C_COUNTER": return Softdatatype.S7COMMP_SOFTDATATYPE_COUNTER;
                case "TIMER":
                case "T_TIMER": return Softdatatype.S7COMMP_SOFTDATATYPE_TIMER;
                default: return Softdatatype.S7COMMP_SOFTDATATYPE_UDEFINED;
            }
        }

        private static void SetImageKey(TreeNode tn, string key)
        {
            tn.ImageKey = key;
            tn.SelectedImageKey = tn.ImageKey;
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node == null) return;
            NodeInfo info = e.Node.Tag as NodeInfo;
            if (info == null)
            {
                tbSymbol.Text = "";
                tbDataType.Text = "";
                tbAddress.Text = "";
                setStatus(_lastGlobalStatus);
                return;
            }
            tbSymbol.Text = info.Symbol;
            tbDataType.Text = info.DataType;
            tbAddress.Text = info.Address;

            // 选中设备节点：状态栏显示该设备的详细信息；其他节点恢复工程总体统计
            setStatus(info.IsDevice ? BuildDeviceStatus(info.DeviceIndex) : _lastGlobalStatus);
        }

        /// <summary>设备详细信息（状态栏单行文本：硬件信息 + 数据块/符号/警告统计）。</summary>
        private string BuildDeviceStatus(int deviceIndex)
        {
            PlcProject project = _project.Project;
            if (project == null || deviceIndex < 0 || deviceIndex >= project.Devices.Count)
            {
                return _lastGlobalStatus;
            }
            PlcDevice device = project.Devices[deviceIndex];

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

            int symbolCount = 0;
            var areaCounts = new List<string>();
            foreach (PlcSymbolArea area in device.SymbolAreas)
            {
                symbolCount += area.Variables.Count;
                areaCounts.Add(area.Name + ": " + area.Variables.Count.ToString());
            }
            //sb.Append(" | 数据块 ").Append(device.Blocks.Count.ToString()).Append(" 个");
            //sb.Append(", 符号 ").Append(symbolCount.ToString()).Append(" 个 (")
            //  .Append(string.Join(", ", areaCounts.ToArray())).Append(")");
            //sb.Append(", 警告 ").Append(device.Warnings.Count.ToString()).Append(" 条");
            return sb.ToString();
        }

        private static int CountLeavesOfDevice(PlcDevice device)
        {
            int count = 0;
            foreach (PlcBlock block in device.Blocks)
            {
                count += CountLeaves(block.Variables);
            }
            foreach (PlcSymbolArea area in device.SymbolAreas)
            {
                count += area.Variables.Count;
            }
            return count;
        }

        private static int CountLeaves(List<PlcVariableNode> nodes)
        {
            int count = 0;
            foreach (PlcVariableNode node in nodes)
            {
                if (node.Children == null || node.Children.Count == 0) count++;
                else count += CountLeaves(node.Children);
            }
            return count;
        }

        /// <summary>
        /// 树节点携带的详细信息。RelId != 0 且 ChildrenLoaded == false 的节点
        /// 在展开时经 getTypeInfoByRelId 懒加载（与 GUIBrowser 的 Tag 用法一致）。
        /// </summary>
        private class NodeInfo
        {
            /// <summary>是否为可寻址的变量（设备/块/警告节点为 false）。</summary>
            public bool IsVariable;
            /// <summary>是否为设备节点（选中时状态栏显示设备详细信息）。</summary>
            public bool IsDevice;
            /// <summary>符号名（完整路径，如 "SAMPLE_UDT1.BOOL1"、"SAMPLE_ARRAY[3]"）。</summary>
            public string Symbol;
            /// <summary>通讯绝对地址（s7netplus 点号语法，如 "DB1.DBX36.0"），容器取首元素地址，非变量为空。</summary>
            public string Address;
            /// <summary>数据类型文本（选中节点时显示在"数据类型"框）。</summary>
            public string DataType;
            /// <summary>所属设备下标（懒加载前需 SetActiveDevice）。</summary>
            public int DeviceIndex;
            /// <summary>懒加载的类型信息 relId；0 表示无需懒加载。</summary>
            public uint RelId;
            /// <summary>子节点是否已构建（懒加载完成后置 true）。</summary>
            public bool ChildrenLoaded;
            /// <summary>对应的解析模型节点（扩展：提供地址/注释等信息）。</summary>
            public PlcVariableNode ModelNode;
        }
    }
}
