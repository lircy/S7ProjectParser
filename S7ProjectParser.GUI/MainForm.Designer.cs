namespace S7ProjectParser.GUI
{
    partial class MainForm
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btnClose = new System.Windows.Forms.Button();
            this.btnOpen = new System.Windows.Forms.Button();
            this.lbStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.treeView1 = new System.Windows.Forms.TreeView();
            this.imageListTreeIcons = new System.Windows.Forms.ImageList(this.components);
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.tbAddress = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.tbDataType = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.tbSymbol = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.btnClose);
            this.groupBox1.Controls.Add(this.btnOpen);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(171, 139);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "工程";
            // 
            // btnClose
            // 
            this.btnClose.Location = new System.Drawing.Point(85, 21);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(75, 23);
            this.btnClose.TabIndex = 4;
            this.btnClose.Text = "关闭";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // btnOpen
            // 
            this.btnOpen.Location = new System.Drawing.Point(9, 21);
            this.btnOpen.Name = "btnOpen";
            this.btnOpen.Size = new System.Drawing.Size(75, 23);
            this.btnOpen.TabIndex = 3;
            this.btnOpen.Text = "打开";
            this.btnOpen.UseVisualStyleBackColor = true;
            this.btnOpen.Click += new System.EventHandler(this.btnOpen_Click);
            // 
            // lbStatus
            // 
            this.lbStatus.Name = "lbStatus";
            this.lbStatus.Size = new System.Drawing.Size(785, 17);
            this.lbStatus.Spring = true;
            this.lbStatus.Text = "未打开工程";
            this.lbStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lbStatus});
            this.statusStrip1.Location = new System.Drawing.Point(0, 439);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(800, 22);
            this.statusStrip1.TabIndex = 8;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // treeView1
            // 
            this.treeView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.treeView1.HideSelection = false;
            this.treeView1.ImageIndex = 0;
            this.treeView1.ImageList = this.imageListTreeIcons;
            this.treeView1.Location = new System.Drawing.Point(12, 157);
            this.treeView1.Name = "treeView1";
            this.treeView1.SelectedImageIndex = 0;
            this.treeView1.ShowNodeToolTips = true;
            this.treeView1.Size = new System.Drawing.Size(776, 281);
            this.treeView1.TabIndex = 9;
            this.treeView1.AfterExpand += new System.Windows.Forms.TreeViewEventHandler(this.treeView1_AfterExpand);
            this.treeView1.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeView1_AfterSelect);
            // 
            // imageListTreeIcons
            // 
            this.imageListTreeIcons.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageListTreeIcons.ImageStream")));
            this.imageListTreeIcons.TransparentColor = System.Drawing.Color.Transparent;
            this.imageListTreeIcons.Images.SetKeyName(0, "Default");
            this.imageListTreeIcons.Images.SetKeyName(1, "Datablock");
            this.imageListTreeIcons.Images.SetKeyName(2, "Tag");
            this.imageListTreeIcons.Images.SetKeyName(3, "Boolean");
            this.imageListTreeIcons.Images.SetKeyName(4, "Boolean1");
            this.imageListTreeIcons.Images.SetKeyName(5, "Boolean2");
            this.imageListTreeIcons.Images.SetKeyName(6, "Any");
            this.imageListTreeIcons.Images.SetKeyName(7, "Binary");
            this.imageListTreeIcons.Images.SetKeyName(8, "Binary2");
            this.imageListTreeIcons.Images.SetKeyName(9, "Date");
            this.imageListTreeIcons.Images.SetKeyName(10, "Integer");
            this.imageListTreeIcons.Images.SetKeyName(11, "Integer2");
            this.imageListTreeIcons.Images.SetKeyName(12, "Number");
            this.imageListTreeIcons.Images.SetKeyName(13, "Number2");
            this.imageListTreeIcons.Images.SetKeyName(14, "Time");
            this.imageListTreeIcons.Images.SetKeyName(15, "Datetime");
            this.imageListTreeIcons.Images.SetKeyName(16, "Datetimezone");
            this.imageListTreeIcons.Images.SetKeyName(17, "Duration");
            this.imageListTreeIcons.Images.SetKeyName(18, "Timer");
            this.imageListTreeIcons.Images.SetKeyName(19, "Char");
            this.imageListTreeIcons.Images.SetKeyName(20, "Text");
            this.imageListTreeIcons.Images.SetKeyName(21, "Text2");
            this.imageListTreeIcons.Images.SetKeyName(22, "Text3");
            this.imageListTreeIcons.Images.SetKeyName(23, "Structure");
            this.imageListTreeIcons.Images.SetKeyName(24, "Currency");
            this.imageListTreeIcons.Images.SetKeyName(25, "Percentage");
            this.imageListTreeIcons.Images.SetKeyName(26, "UserDataType");
            this.imageListTreeIcons.Images.SetKeyName(27, "UserDefinedDataType");
            this.imageListTreeIcons.Images.SetKeyName(28, "CountCollection");
            this.imageListTreeIcons.Images.SetKeyName(29, "FolderOpened");
            this.imageListTreeIcons.Images.SetKeyName(30, "FolderClosed");
            this.imageListTreeIcons.Images.SetKeyName(31, "FolderTopPanel");
            this.imageListTreeIcons.Images.SetKeyName(32, "Indexer");
            this.imageListTreeIcons.Images.SetKeyName(33, "Library");
            this.imageListTreeIcons.Images.SetKeyName(34, "NamedSet");
            this.imageListTreeIcons.Images.SetKeyName(35, "TypeDefinition");
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.tbAddress);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.tbDataType);
            this.groupBox2.Controls.Add(this.label6);
            this.groupBox2.Controls.Add(this.tbSymbol);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Location = new System.Drawing.Point(189, 12);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(599, 139);
            this.groupBox2.TabIndex = 2;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "变量";
            // 
            // tbAddress
            // 
            this.tbAddress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbAddress.Location = new System.Drawing.Point(6, 110);
            this.tbAddress.Name = "tbAddress";
            this.tbAddress.ReadOnly = true;
            this.tbAddress.Size = new System.Drawing.Size(587, 23);
            this.tbAddress.TabIndex = 7;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 94);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(173, 17);
            this.label4.TabIndex = 7;
            this.label4.Text = "绝对地址 (Absolute address):";
            // 
            // tbDataType
            // 
            this.tbDataType.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbDataType.Location = new System.Drawing.Point(6, 71);
            this.tbDataType.Name = "tbDataType";
            this.tbDataType.ReadOnly = true;
            this.tbDataType.Size = new System.Drawing.Size(587, 23);
            this.tbDataType.TabIndex = 8;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(6, 55);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(127, 17);
            this.label6.TabIndex = 9;
            this.label6.Text = "数据类型 (Data type):";
            // 
            // tbSymbol
            // 
            this.tbSymbol.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbSymbol.Location = new System.Drawing.Point(6, 32);
            this.tbSymbol.Name = "tbSymbol";
            this.tbSymbol.Size = new System.Drawing.Size(587, 23);
            this.tbSymbol.TabIndex = 5;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 16);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(90, 17);
            this.label3.TabIndex = 0;
            this.label3.Text = "符号 (Symbol):";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 461);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.treeView1);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.statusStrip1);
            this.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.MinimumSize = new System.Drawing.Size(800, 500);
            this.Name = "MainForm";
            this.Text = "S7 Project Explorer (.NET 4.0) - STEP 7 变量浏览器";
            this.groupBox1.ResumeLayout(false);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button btnOpen;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox tbSymbol;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox tbAddress;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox tbDataType;
        private System.Windows.Forms.ToolStripStatusLabel lbStatus;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.TreeView treeView1;
        private System.Windows.Forms.ImageList imageListTreeIcons;
    }
}
