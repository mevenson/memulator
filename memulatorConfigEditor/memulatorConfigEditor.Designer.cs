namespace ConfigEditor
{
    partial class memulatorConfigEditor
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(memulatorConfigEditor));
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.fIleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.groupBoxGlobal = new System.Windows.Forms.GroupBox();
            this.buttonBrowseStatisticsFile = new System.Windows.Forms.Button();
            this.textBoxStatisticsFile = new System.Windows.Forms.TextBox();
            this.labelStatisticsFile = new System.Windows.Forms.Label();
            this.buttonBrowseCoreDumpFile = new System.Windows.Forms.Button();
            this.textBoxCoreDumpFile = new System.Windows.Forms.TextBox();
            this.labelCoreDumpFile = new System.Windows.Forms.Label();
            this.buttonBrowseROMFile = new System.Windows.Forms.Button();
            this.textBoxROMFile = new System.Windows.Forms.TextBox();
            this.labelROMFile = new System.Windows.Forms.Label();
            this.buttonBrowseTraceFile = new System.Windows.Forms.Button();
            this.textBoxTraceFile = new System.Windows.Forms.TextBox();
            this.checkBoxEnableTrace = new System.Windows.Forms.CheckBox();
            this.checkBoxAllowMultiSector = new System.Windows.Forms.CheckBox();
            this.textBoxWinchesterInterruptDelay = new System.Windows.Forms.TextBox();
            this.labelWinchesterInterruptDelay = new System.Windows.Forms.Label();
            this.groupBoxProcessorBoard = new System.Windows.Forms.GroupBox();
            this.radioButtonGeneric = new System.Windows.Forms.RadioButton();
            this.radioButtonMP_09 = new System.Windows.Forms.RadioButton();
            this.radioButtonMPU_1 = new System.Windows.Forms.RadioButton();
            this.groupBoxProcessor = new System.Windows.Forms.GroupBox();
            this.radioButton6800 = new System.Windows.Forms.RadioButton();
            this.radioButton6809 = new System.Windows.Forms.RadioButton();
            this.labelProcessorBoard = new System.Windows.Forms.Label();
            this.checkBoxSW1D = new System.Windows.Forms.CheckBox();
            this.checkBoxSW1C = new System.Windows.Forms.CheckBox();
            this.checkBoxSW1B = new System.Windows.Forms.CheckBox();
            this.label3 = new System.Windows.Forms.Label();
            this.labelROM = new System.Windows.Forms.Label();
            this.labelRAM = new System.Windows.Forms.Label();
            this.labelBaudRate = new System.Windows.Forms.Label();
            this.checkBoxF000ROM = new System.Windows.Forms.CheckBox();
            this.checkBoxE800ROM = new System.Windows.Forms.CheckBox();
            this.checkBoxE000ROM = new System.Windows.Forms.CheckBox();
            this.checkBoxF000RAM = new System.Windows.Forms.CheckBox();
            this.checkBoxE800RAM = new System.Windows.Forms.CheckBox();
            this.checkBoxE000RAM = new System.Windows.Forms.CheckBox();
            this.labelProcessor = new System.Windows.Forms.Label();
            this.checkBoxLowHigh = new System.Windows.Forms.CheckBox();
            this.checkBox600_4800 = new System.Windows.Forms.CheckBox();
            this.checkBox150_9600 = new System.Windows.Forms.CheckBox();
            this.labelProcessorJumpers = new System.Windows.Forms.Label();
            this.buttonBrowseConsoleDumpFile = new System.Windows.Forms.Button();
            this.textBoxConsoleDumpFile = new System.Windows.Forms.TextBox();
            this.labelConsoleDumpFile = new System.Windows.Forms.Label();
            this.tabControlBoardsAndStorage = new System.Windows.Forms.TabControl();
            this.tabPageBoard = new System.Windows.Forms.TabPage();
            this.buttonEditBoard = new System.Windows.Forms.Button();
            this.buttonRemoveBoard = new System.Windows.Forms.Button();
            this.buttonAddBoard = new System.Windows.Forms.Button();
            this.listViewBoards = new System.Windows.Forms.ListView();
            this.Type = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.Address = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.Size = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.GUID = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.IRQ = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.tabPageFloppy = new System.Windows.Forms.TabPage();
            this.buttonFloppyDown = new System.Windows.Forms.Button();
            this.buttonFloppyUp = new System.Windows.Forms.Button();
            this.buttonEditFloppy = new System.Windows.Forms.Button();
            this.buttonRemoveFloppy = new System.Windows.Forms.Button();
            this.buttonAddFloppy = new System.Windows.Forms.Button();
            this.listViewFloppy = new System.Windows.Forms.ListView();
            this.FloppyDriveNumber = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.FloppyFileAndPath = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.FloppyFormat = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.tabPagePIADisks = new System.Windows.Forms.TabPage();
            this.buttonPIADiskDown = new System.Windows.Forms.Button();
            this.buttonPIADiskUp = new System.Windows.Forms.Button();
            this.buttonEditPIADisk = new System.Windows.Forms.Button();
            this.buttonRemovePIADisk = new System.Windows.Forms.Button();
            this.buttonAddPIADisk = new System.Windows.Forms.Button();
            this.listViewPIADisks = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.tabPageTTLDisks = new System.Windows.Forms.TabPage();
            this.buttonTTLDiskDown = new System.Windows.Forms.Button();
            this.buttonTTLDiskUp = new System.Windows.Forms.Button();
            this.buttonEditTTLDisk = new System.Windows.Forms.Button();
            this.buttonRemoveTTLDisk = new System.Windows.Forms.Button();
            this.buttonAddTTLDisk = new System.Windows.Forms.Button();
            this.listViewTTLDisks = new System.Windows.Forms.ListView();
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader6 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.tabPageWinchester = new System.Windows.Forms.TabPage();
            this.buttonWinchesterDown = new System.Windows.Forms.Button();
            this.buttonWinchesterUP = new System.Windows.Forms.Button();
            this.buttonEditWinchester = new System.Windows.Forms.Button();
            this.buttonRemoveWinchester = new System.Windows.Forms.Button();
            this.buttonAddWinchester = new System.Windows.Forms.Button();
            this.listViewWinchester = new System.Windows.Forms.ListView();
            this.WinchesterDriveNumber = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.WinchesterTypeName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.WinchesterPathAndFile = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.WinchesterCylinders = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.WinchesterHeads = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.WinchesterSectorsPerTrack = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.WinchesterBytesPerSector = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.tabPageCDS = new System.Windows.Forms.TabPage();
            this.buttonCDSDown = new System.Windows.Forms.Button();
            this.buttonCDSUp = new System.Windows.Forms.Button();
            this.listViewCDS = new System.Windows.Forms.ListView();
            this.CDSDriveNumber = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.CDSTypeName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.CDSPathAndFile = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.CDSCylinders = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.CDSHeads = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.CDSSectorsPerTrack = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.CDSBytesPerSector = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.buttonEditCDS = new System.Windows.Forms.Button();
            this.buttonRemoveCDS = new System.Windows.Forms.Button();
            this.buttonAddCDS = new System.Windows.Forms.Button();
            this.tabPageKeyboardLayout = new System.Windows.Forms.TabPage();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.listViewKeyboardMap = new System.Windows.Forms.ListView();
            this.Key = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.Normal = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.Shifted = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.Control = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.Alt = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.Both = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.menuStrip.SuspendLayout();
            this.groupBoxGlobal.SuspendLayout();
            this.groupBoxProcessorBoard.SuspendLayout();
            this.groupBoxProcessor.SuspendLayout();
            this.tabControlBoardsAndStorage.SuspendLayout();
            this.tabPageBoard.SuspendLayout();
            this.tabPageFloppy.SuspendLayout();
            this.tabPagePIADisks.SuspendLayout();
            this.tabPageTTLDisks.SuspendLayout();
            this.tabPageWinchester.SuspendLayout();
            this.tabPageCDS.SuspendLayout();
            this.tabPageKeyboardLayout.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip
            // 
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fIleToolStripMenuItem});
            this.menuStrip.Location = new System.Drawing.Point(0, 0);
            this.menuStrip.Name = "menuStrip";
            this.menuStrip.Size = new System.Drawing.Size(661, 24);
            this.menuStrip.TabIndex = 0;
            this.menuStrip.Text = "menuStrip";
            // 
            // fIleToolStripMenuItem
            // 
            this.fIleToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator1,
            this.saveToolStripMenuItem,
            this.toolStripSeparator3,
            this.exitToolStripMenuItem});
            this.fIleToolStripMenuItem.Name = "fIleToolStripMenuItem";
            this.fIleToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fIleToolStripMenuItem.Text = "&File";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(177, 6);
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.saveToolStripMenuItem.Text = "&Save";
            this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(177, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.exitToolStripMenuItem.Text = "E&xit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // groupBoxGlobal
            // 
            this.groupBoxGlobal.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBoxGlobal.Controls.Add(this.buttonBrowseStatisticsFile);
            this.groupBoxGlobal.Controls.Add(this.textBoxStatisticsFile);
            this.groupBoxGlobal.Controls.Add(this.labelStatisticsFile);
            this.groupBoxGlobal.Controls.Add(this.buttonBrowseCoreDumpFile);
            this.groupBoxGlobal.Controls.Add(this.textBoxCoreDumpFile);
            this.groupBoxGlobal.Controls.Add(this.labelCoreDumpFile);
            this.groupBoxGlobal.Controls.Add(this.buttonBrowseROMFile);
            this.groupBoxGlobal.Controls.Add(this.textBoxROMFile);
            this.groupBoxGlobal.Controls.Add(this.labelROMFile);
            this.groupBoxGlobal.Controls.Add(this.buttonBrowseTraceFile);
            this.groupBoxGlobal.Controls.Add(this.textBoxTraceFile);
            this.groupBoxGlobal.Controls.Add(this.checkBoxEnableTrace);
            this.groupBoxGlobal.Controls.Add(this.checkBoxAllowMultiSector);
            this.groupBoxGlobal.Controls.Add(this.textBoxWinchesterInterruptDelay);
            this.groupBoxGlobal.Controls.Add(this.labelWinchesterInterruptDelay);
            this.groupBoxGlobal.Controls.Add(this.groupBoxProcessorBoard);
            this.groupBoxGlobal.Controls.Add(this.groupBoxProcessor);
            this.groupBoxGlobal.Controls.Add(this.labelProcessorBoard);
            this.groupBoxGlobal.Controls.Add(this.checkBoxSW1D);
            this.groupBoxGlobal.Controls.Add(this.checkBoxSW1C);
            this.groupBoxGlobal.Controls.Add(this.checkBoxSW1B);
            this.groupBoxGlobal.Controls.Add(this.label3);
            this.groupBoxGlobal.Controls.Add(this.labelROM);
            this.groupBoxGlobal.Controls.Add(this.labelRAM);
            this.groupBoxGlobal.Controls.Add(this.labelBaudRate);
            this.groupBoxGlobal.Controls.Add(this.checkBoxF000ROM);
            this.groupBoxGlobal.Controls.Add(this.checkBoxE800ROM);
            this.groupBoxGlobal.Controls.Add(this.checkBoxE000ROM);
            this.groupBoxGlobal.Controls.Add(this.checkBoxF000RAM);
            this.groupBoxGlobal.Controls.Add(this.checkBoxE800RAM);
            this.groupBoxGlobal.Controls.Add(this.checkBoxE000RAM);
            this.groupBoxGlobal.Controls.Add(this.labelProcessor);
            this.groupBoxGlobal.Controls.Add(this.checkBoxLowHigh);
            this.groupBoxGlobal.Controls.Add(this.checkBox600_4800);
            this.groupBoxGlobal.Controls.Add(this.checkBox150_9600);
            this.groupBoxGlobal.Controls.Add(this.labelProcessorJumpers);
            this.groupBoxGlobal.Controls.Add(this.buttonBrowseConsoleDumpFile);
            this.groupBoxGlobal.Controls.Add(this.textBoxConsoleDumpFile);
            this.groupBoxGlobal.Controls.Add(this.labelConsoleDumpFile);
            this.groupBoxGlobal.Location = new System.Drawing.Point(13, 38);
            this.groupBoxGlobal.Name = "groupBoxGlobal";
            this.groupBoxGlobal.Size = new System.Drawing.Size(636, 317);
            this.groupBoxGlobal.TabIndex = 0;
            this.groupBoxGlobal.TabStop = false;
            this.groupBoxGlobal.Text = "Global";
            // 
            // buttonBrowseStatisticsFile
            // 
            this.buttonBrowseStatisticsFile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonBrowseStatisticsFile.Location = new System.Drawing.Point(543, 211);
            this.buttonBrowseStatisticsFile.Name = "buttonBrowseStatisticsFile";
            this.buttonBrowseStatisticsFile.Size = new System.Drawing.Size(75, 23);
            this.buttonBrowseStatisticsFile.TabIndex = 31;
            this.buttonBrowseStatisticsFile.Text = "Browse";
            this.buttonBrowseStatisticsFile.UseVisualStyleBackColor = true;
            this.buttonBrowseStatisticsFile.Click += new System.EventHandler(this.buttonBrowseStatisticsFile_Click);
            // 
            // textBoxStatisticsFile
            // 
            this.textBoxStatisticsFile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxStatisticsFile.Location = new System.Drawing.Point(124, 212);
            this.textBoxStatisticsFile.Name = "textBoxStatisticsFile";
            this.textBoxStatisticsFile.Size = new System.Drawing.Size(394, 20);
            this.textBoxStatisticsFile.TabIndex = 30;
            // 
            // labelStatisticsFile
            // 
            this.labelStatisticsFile.AutoSize = true;
            this.labelStatisticsFile.Location = new System.Drawing.Point(23, 215);
            this.labelStatisticsFile.Name = "labelStatisticsFile";
            this.labelStatisticsFile.Size = new System.Drawing.Size(68, 13);
            this.labelStatisticsFile.TabIndex = 29;
            this.labelStatisticsFile.Text = "Statistics File";
            // 
            // buttonBrowseCoreDumpFile
            // 
            this.buttonBrowseCoreDumpFile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonBrowseCoreDumpFile.Location = new System.Drawing.Point(543, 237);
            this.buttonBrowseCoreDumpFile.Name = "buttonBrowseCoreDumpFile";
            this.buttonBrowseCoreDumpFile.Size = new System.Drawing.Size(75, 23);
            this.buttonBrowseCoreDumpFile.TabIndex = 34;
            this.buttonBrowseCoreDumpFile.Text = "Browse";
            this.buttonBrowseCoreDumpFile.UseVisualStyleBackColor = true;
            this.buttonBrowseCoreDumpFile.Click += new System.EventHandler(this.buttonBrowseCoreDumpFile_Click);
            // 
            // textBoxCoreDumpFile
            // 
            this.textBoxCoreDumpFile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxCoreDumpFile.Location = new System.Drawing.Point(124, 238);
            this.textBoxCoreDumpFile.Name = "textBoxCoreDumpFile";
            this.textBoxCoreDumpFile.Size = new System.Drawing.Size(394, 20);
            this.textBoxCoreDumpFile.TabIndex = 33;
            // 
            // labelCoreDumpFile
            // 
            this.labelCoreDumpFile.AutoSize = true;
            this.labelCoreDumpFile.Location = new System.Drawing.Point(23, 241);
            this.labelCoreDumpFile.Name = "labelCoreDumpFile";
            this.labelCoreDumpFile.Size = new System.Drawing.Size(79, 13);
            this.labelCoreDumpFile.TabIndex = 32;
            this.labelCoreDumpFile.Text = "Core Dump File";
            // 
            // buttonBrowseROMFile
            // 
            this.buttonBrowseROMFile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonBrowseROMFile.Location = new System.Drawing.Point(542, 58);
            this.buttonBrowseROMFile.Name = "buttonBrowseROMFile";
            this.buttonBrowseROMFile.Size = new System.Drawing.Size(75, 23);
            this.buttonBrowseROMFile.TabIndex = 6;
            this.buttonBrowseROMFile.Text = "Browse";
            this.buttonBrowseROMFile.UseVisualStyleBackColor = true;
            this.buttonBrowseROMFile.Click += new System.EventHandler(this.buttonBrowseROMFile_Click);
            // 
            // textBoxROMFile
            // 
            this.textBoxROMFile.Location = new System.Drawing.Point(124, 60);
            this.textBoxROMFile.Name = "textBoxROMFile";
            this.textBoxROMFile.Size = new System.Drawing.Size(394, 20);
            this.textBoxROMFile.TabIndex = 5;
            // 
            // labelROMFile
            // 
            this.labelROMFile.AutoSize = true;
            this.labelROMFile.Location = new System.Drawing.Point(26, 60);
            this.labelROMFile.Name = "labelROMFile";
            this.labelROMFile.Size = new System.Drawing.Size(51, 13);
            this.labelROMFile.TabIndex = 4;
            this.labelROMFile.Text = "ROM File";
            // 
            // buttonBrowseTraceFile
            // 
            this.buttonBrowseTraceFile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonBrowseTraceFile.Location = new System.Drawing.Point(543, 271);
            this.buttonBrowseTraceFile.Name = "buttonBrowseTraceFile";
            this.buttonBrowseTraceFile.Size = new System.Drawing.Size(75, 23);
            this.buttonBrowseTraceFile.TabIndex = 37;
            this.buttonBrowseTraceFile.Text = "Browse";
            this.buttonBrowseTraceFile.UseVisualStyleBackColor = true;
            this.buttonBrowseTraceFile.Click += new System.EventHandler(this.buttonBrowseTraceFile_Click);
            // 
            // textBoxTraceFile
            // 
            this.textBoxTraceFile.Location = new System.Drawing.Point(124, 272);
            this.textBoxTraceFile.Name = "textBoxTraceFile";
            this.textBoxTraceFile.Size = new System.Drawing.Size(394, 20);
            this.textBoxTraceFile.TabIndex = 36;
            // 
            // checkBoxEnableTrace
            // 
            this.checkBoxEnableTrace.AutoSize = true;
            this.checkBoxEnableTrace.Location = new System.Drawing.Point(24, 272);
            this.checkBoxEnableTrace.Name = "checkBoxEnableTrace";
            this.checkBoxEnableTrace.Size = new System.Drawing.Size(90, 17);
            this.checkBoxEnableTrace.TabIndex = 35;
            this.checkBoxEnableTrace.Text = "Enable Trace";
            this.checkBoxEnableTrace.UseVisualStyleBackColor = true;
            this.checkBoxEnableTrace.CheckedChanged += new System.EventHandler(this.checkBoxEnableTrace_CheckedChanged);
            // 
            // checkBoxAllowMultiSector
            // 
            this.checkBoxAllowMultiSector.AutoSize = true;
            this.checkBoxAllowMultiSector.Location = new System.Drawing.Point(24, 294);
            this.checkBoxAllowMultiSector.Name = "checkBoxAllowMultiSector";
            this.checkBoxAllowMultiSector.Size = new System.Drawing.Size(110, 17);
            this.checkBoxAllowMultiSector.TabIndex = 38;
            this.checkBoxAllowMultiSector.Text = "Allow Multi Sector";
            this.checkBoxAllowMultiSector.UseVisualStyleBackColor = true;
            // 
            // textBoxWinchesterInterruptDelay
            // 
            this.textBoxWinchesterInterruptDelay.Location = new System.Drawing.Point(166, 160);
            this.textBoxWinchesterInterruptDelay.Name = "textBoxWinchesterInterruptDelay";
            this.textBoxWinchesterInterruptDelay.Size = new System.Drawing.Size(29, 20);
            this.textBoxWinchesterInterruptDelay.TabIndex = 25;
            // 
            // labelWinchesterInterruptDelay
            // 
            this.labelWinchesterInterruptDelay.AutoSize = true;
            this.labelWinchesterInterruptDelay.Location = new System.Drawing.Point(23, 162);
            this.labelWinchesterInterruptDelay.Name = "labelWinchesterInterruptDelay";
            this.labelWinchesterInterruptDelay.Size = new System.Drawing.Size(133, 13);
            this.labelWinchesterInterruptDelay.TabIndex = 24;
            this.labelWinchesterInterruptDelay.Text = "Winchester Interrupt Delay";
            // 
            // groupBoxProcessorBoard
            // 
            this.groupBoxProcessorBoard.Controls.Add(this.radioButtonGeneric);
            this.groupBoxProcessorBoard.Controls.Add(this.radioButtonMP_09);
            this.groupBoxProcessorBoard.Controls.Add(this.radioButtonMPU_1);
            this.groupBoxProcessorBoard.Location = new System.Drawing.Point(352, 19);
            this.groupBoxProcessorBoard.Name = "groupBoxProcessorBoard";
            this.groupBoxProcessorBoard.Size = new System.Drawing.Size(265, 32);
            this.groupBoxProcessorBoard.TabIndex = 3;
            this.groupBoxProcessorBoard.TabStop = false;
            this.groupBoxProcessorBoard.Visible = false;
            // 
            // radioButtonGeneric
            // 
            this.radioButtonGeneric.AutoSize = true;
            this.radioButtonGeneric.Location = new System.Drawing.Point(197, 9);
            this.radioButtonGeneric.Name = "radioButtonGeneric";
            this.radioButtonGeneric.Size = new System.Drawing.Size(62, 17);
            this.radioButtonGeneric.TabIndex = 2;
            this.radioButtonGeneric.TabStop = true;
            this.radioButtonGeneric.Text = "Generic";
            this.radioButtonGeneric.UseVisualStyleBackColor = true;
            this.radioButtonGeneric.Visible = false;
            this.radioButtonGeneric.CheckedChanged += new System.EventHandler(this.radioButtonGeneric_CheckedChanged);
            // 
            // radioButtonMP_09
            // 
            this.radioButtonMP_09.AutoSize = true;
            this.radioButtonMP_09.Location = new System.Drawing.Point(13, 9);
            this.radioButtonMP_09.Name = "radioButtonMP_09";
            this.radioButtonMP_09.Size = new System.Drawing.Size(59, 17);
            this.radioButtonMP_09.TabIndex = 0;
            this.radioButtonMP_09.TabStop = true;
            this.radioButtonMP_09.Text = "MP_09";
            this.radioButtonMP_09.UseVisualStyleBackColor = true;
            this.radioButtonMP_09.Visible = false;
            this.radioButtonMP_09.CheckedChanged += new System.EventHandler(this.radioButtonMP_09_CheckedChanged);
            // 
            // radioButtonMPU_1
            // 
            this.radioButtonMPU_1.AutoSize = true;
            this.radioButtonMPU_1.Location = new System.Drawing.Point(104, 9);
            this.radioButtonMPU_1.Name = "radioButtonMPU_1";
            this.radioButtonMPU_1.Size = new System.Drawing.Size(61, 17);
            this.radioButtonMPU_1.TabIndex = 1;
            this.radioButtonMPU_1.TabStop = true;
            this.radioButtonMPU_1.Text = "MPU_1";
            this.radioButtonMPU_1.UseVisualStyleBackColor = true;
            this.radioButtonMPU_1.Visible = false;
            this.radioButtonMPU_1.CheckedChanged += new System.EventHandler(this.radioButtonMPU_1_CheckedChanged);
            // 
            // groupBoxProcessor
            // 
            this.groupBoxProcessor.Controls.Add(this.radioButton6800);
            this.groupBoxProcessor.Controls.Add(this.radioButton6809);
            this.groupBoxProcessor.Location = new System.Drawing.Point(124, 19);
            this.groupBoxProcessor.Name = "groupBoxProcessor";
            this.groupBoxProcessor.Size = new System.Drawing.Size(135, 32);
            this.groupBoxProcessor.TabIndex = 1;
            this.groupBoxProcessor.TabStop = false;
            // 
            // radioButton6800
            // 
            this.radioButton6800.AutoSize = true;
            this.radioButton6800.Location = new System.Drawing.Point(15, 9);
            this.radioButton6800.Name = "radioButton6800";
            this.radioButton6800.Size = new System.Drawing.Size(49, 17);
            this.radioButton6800.TabIndex = 0;
            this.radioButton6800.Text = "6800";
            this.radioButton6800.UseVisualStyleBackColor = true;
            this.radioButton6800.CheckedChanged += new System.EventHandler(this.radioButton6800_CheckedChanged);
            // 
            // radioButton6809
            // 
            this.radioButton6809.AutoSize = true;
            this.radioButton6809.Location = new System.Drawing.Point(73, 9);
            this.radioButton6809.Name = "radioButton6809";
            this.radioButton6809.Size = new System.Drawing.Size(49, 17);
            this.radioButton6809.TabIndex = 1;
            this.radioButton6809.Text = "6809";
            this.radioButton6809.UseVisualStyleBackColor = true;
            this.radioButton6809.CheckedChanged += new System.EventHandler(this.radioButton6809_CheckedChanged);
            // 
            // labelProcessorBoard
            // 
            this.labelProcessorBoard.AutoSize = true;
            this.labelProcessorBoard.Location = new System.Drawing.Point(266, 31);
            this.labelProcessorBoard.Name = "labelProcessorBoard";
            this.labelProcessorBoard.Size = new System.Drawing.Size(85, 13);
            this.labelProcessorBoard.TabIndex = 2;
            this.labelProcessorBoard.Text = "Processor Board";
            this.labelProcessorBoard.Visible = false;
            // 
            // checkBoxSW1D
            // 
            this.checkBoxSW1D.AutoSize = true;
            this.checkBoxSW1D.Location = new System.Drawing.Point(380, 140);
            this.checkBoxSW1D.Name = "checkBoxSW1D";
            this.checkBoxSW1D.Size = new System.Drawing.Size(58, 17);
            this.checkBoxSW1D.TabIndex = 23;
            this.checkBoxSW1D.Text = "SW1D";
            this.checkBoxSW1D.UseVisualStyleBackColor = true;
            // 
            // checkBoxSW1C
            // 
            this.checkBoxSW1C.AutoSize = true;
            this.checkBoxSW1C.Location = new System.Drawing.Point(259, 140);
            this.checkBoxSW1C.Name = "checkBoxSW1C";
            this.checkBoxSW1C.Size = new System.Drawing.Size(57, 17);
            this.checkBoxSW1C.TabIndex = 22;
            this.checkBoxSW1C.Text = "SW1C";
            this.checkBoxSW1C.UseVisualStyleBackColor = true;
            // 
            // checkBoxSW1B
            // 
            this.checkBoxSW1B.AutoSize = true;
            this.checkBoxSW1B.Location = new System.Drawing.Point(138, 140);
            this.checkBoxSW1B.Name = "checkBoxSW1B";
            this.checkBoxSW1B.Size = new System.Drawing.Size(57, 17);
            this.checkBoxSW1B.TabIndex = 21;
            this.checkBoxSW1B.Text = "SW1B";
            this.checkBoxSW1B.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(23, 140);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(89, 13);
            this.label3.TabIndex = 20;
            this.label3.Text = "Processor Switch";
            // 
            // labelROM
            // 
            this.labelROM.AutoSize = true;
            this.labelROM.Location = new System.Drawing.Point(482, 126);
            this.labelROM.Name = "labelROM";
            this.labelROM.Size = new System.Drawing.Size(32, 13);
            this.labelROM.TabIndex = 19;
            this.labelROM.Text = "ROM";
            // 
            // labelRAM
            // 
            this.labelRAM.AutoSize = true;
            this.labelRAM.Location = new System.Drawing.Point(482, 109);
            this.labelRAM.Name = "labelRAM";
            this.labelRAM.Size = new System.Drawing.Size(31, 13);
            this.labelRAM.TabIndex = 15;
            this.labelRAM.Text = "RAM";
            // 
            // labelBaudRate
            // 
            this.labelBaudRate.AutoSize = true;
            this.labelBaudRate.Location = new System.Drawing.Point(482, 91);
            this.labelBaudRate.Name = "labelBaudRate";
            this.labelBaudRate.Size = new System.Drawing.Size(58, 13);
            this.labelBaudRate.TabIndex = 11;
            this.labelBaudRate.Text = "Baud Rate";
            // 
            // checkBoxF000ROM
            // 
            this.checkBoxF000ROM.AutoSize = true;
            this.checkBoxF000ROM.Location = new System.Drawing.Point(380, 123);
            this.checkBoxF000ROM.Name = "checkBoxF000ROM";
            this.checkBoxF000ROM.Size = new System.Drawing.Size(50, 17);
            this.checkBoxF000ROM.TabIndex = 18;
            this.checkBoxF000ROM.Text = "F000";
            this.checkBoxF000ROM.UseVisualStyleBackColor = true;
            // 
            // checkBoxE800ROM
            // 
            this.checkBoxE800ROM.AutoSize = true;
            this.checkBoxE800ROM.Location = new System.Drawing.Point(259, 123);
            this.checkBoxE800ROM.Name = "checkBoxE800ROM";
            this.checkBoxE800ROM.Size = new System.Drawing.Size(51, 17);
            this.checkBoxE800ROM.TabIndex = 17;
            this.checkBoxE800ROM.Text = "E800";
            this.checkBoxE800ROM.UseVisualStyleBackColor = true;
            // 
            // checkBoxE000ROM
            // 
            this.checkBoxE000ROM.AutoSize = true;
            this.checkBoxE000ROM.Location = new System.Drawing.Point(138, 123);
            this.checkBoxE000ROM.Name = "checkBoxE000ROM";
            this.checkBoxE000ROM.Size = new System.Drawing.Size(51, 17);
            this.checkBoxE000ROM.TabIndex = 16;
            this.checkBoxE000ROM.Text = "E000";
            this.checkBoxE000ROM.UseVisualStyleBackColor = true;
            // 
            // checkBoxF000RAM
            // 
            this.checkBoxF000RAM.AutoSize = true;
            this.checkBoxF000RAM.Location = new System.Drawing.Point(380, 107);
            this.checkBoxF000RAM.Name = "checkBoxF000RAM";
            this.checkBoxF000RAM.Size = new System.Drawing.Size(50, 17);
            this.checkBoxF000RAM.TabIndex = 14;
            this.checkBoxF000RAM.Text = "F000";
            this.checkBoxF000RAM.UseVisualStyleBackColor = true;
            // 
            // checkBoxE800RAM
            // 
            this.checkBoxE800RAM.AutoSize = true;
            this.checkBoxE800RAM.Location = new System.Drawing.Point(259, 107);
            this.checkBoxE800RAM.Name = "checkBoxE800RAM";
            this.checkBoxE800RAM.Size = new System.Drawing.Size(51, 17);
            this.checkBoxE800RAM.TabIndex = 13;
            this.checkBoxE800RAM.Text = "E800";
            this.checkBoxE800RAM.UseVisualStyleBackColor = true;
            // 
            // checkBoxE000RAM
            // 
            this.checkBoxE000RAM.AutoSize = true;
            this.checkBoxE000RAM.Location = new System.Drawing.Point(138, 107);
            this.checkBoxE000RAM.Name = "checkBoxE000RAM";
            this.checkBoxE000RAM.Size = new System.Drawing.Size(51, 17);
            this.checkBoxE000RAM.TabIndex = 12;
            this.checkBoxE000RAM.Text = "E000";
            this.checkBoxE000RAM.UseVisualStyleBackColor = true;
            // 
            // labelProcessor
            // 
            this.labelProcessor.AutoSize = true;
            this.labelProcessor.Location = new System.Drawing.Point(23, 31);
            this.labelProcessor.Name = "labelProcessor";
            this.labelProcessor.Size = new System.Drawing.Size(54, 13);
            this.labelProcessor.TabIndex = 0;
            this.labelProcessor.Text = "Processor";
            // 
            // checkBoxLowHigh
            // 
            this.checkBoxLowHigh.AutoSize = true;
            this.checkBoxLowHigh.Location = new System.Drawing.Point(380, 91);
            this.checkBoxLowHigh.Name = "checkBoxLowHigh";
            this.checkBoxLowHigh.Size = new System.Drawing.Size(83, 17);
            this.checkBoxLowHigh.TabIndex = 10;
            this.checkBoxLowHigh.Text = "LOW/HIGH";
            this.checkBoxLowHigh.UseVisualStyleBackColor = true;
            // 
            // checkBox600_4800
            // 
            this.checkBox600_4800.AutoSize = true;
            this.checkBox600_4800.Location = new System.Drawing.Point(259, 91);
            this.checkBox600_4800.Name = "checkBox600_4800";
            this.checkBox600_4800.Size = new System.Drawing.Size(73, 17);
            this.checkBox600_4800.TabIndex = 9;
            this.checkBox600_4800.Text = "600/4800";
            this.checkBox600_4800.UseVisualStyleBackColor = true;
            // 
            // checkBox150_9600
            // 
            this.checkBox150_9600.AutoSize = true;
            this.checkBox150_9600.Location = new System.Drawing.Point(138, 91);
            this.checkBox150_9600.Name = "checkBox150_9600";
            this.checkBox150_9600.Size = new System.Drawing.Size(73, 17);
            this.checkBox150_9600.TabIndex = 8;
            this.checkBox150_9600.Text = "150/9600";
            this.checkBox150_9600.UseVisualStyleBackColor = true;
            // 
            // labelProcessorJumpers
            // 
            this.labelProcessorJumpers.AutoSize = true;
            this.labelProcessorJumpers.Location = new System.Drawing.Point(23, 91);
            this.labelProcessorJumpers.Name = "labelProcessorJumpers";
            this.labelProcessorJumpers.Size = new System.Drawing.Size(96, 13);
            this.labelProcessorJumpers.TabIndex = 7;
            this.labelProcessorJumpers.Text = "Processor Jumpers";
            // 
            // buttonBrowseConsoleDumpFile
            // 
            this.buttonBrowseConsoleDumpFile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonBrowseConsoleDumpFile.Location = new System.Drawing.Point(543, 185);
            this.buttonBrowseConsoleDumpFile.Name = "buttonBrowseConsoleDumpFile";
            this.buttonBrowseConsoleDumpFile.Size = new System.Drawing.Size(75, 23);
            this.buttonBrowseConsoleDumpFile.TabIndex = 28;
            this.buttonBrowseConsoleDumpFile.Text = "Browse";
            this.buttonBrowseConsoleDumpFile.UseVisualStyleBackColor = true;
            this.buttonBrowseConsoleDumpFile.Click += new System.EventHandler(this.buttonBrowseConsoleDumpFile_Click);
            // 
            // textBoxConsoleDumpFile
            // 
            this.textBoxConsoleDumpFile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxConsoleDumpFile.Location = new System.Drawing.Point(124, 186);
            this.textBoxConsoleDumpFile.Name = "textBoxConsoleDumpFile";
            this.textBoxConsoleDumpFile.Size = new System.Drawing.Size(394, 20);
            this.textBoxConsoleDumpFile.TabIndex = 27;
            // 
            // labelConsoleDumpFile
            // 
            this.labelConsoleDumpFile.AutoSize = true;
            this.labelConsoleDumpFile.Location = new System.Drawing.Point(23, 189);
            this.labelConsoleDumpFile.Name = "labelConsoleDumpFile";
            this.labelConsoleDumpFile.Size = new System.Drawing.Size(95, 13);
            this.labelConsoleDumpFile.TabIndex = 26;
            this.labelConsoleDumpFile.Text = "Console Dump File";
            // 
            // tabControlBoardsAndStorage
            // 
            this.tabControlBoardsAndStorage.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControlBoardsAndStorage.Controls.Add(this.tabPageBoard);
            this.tabControlBoardsAndStorage.Controls.Add(this.tabPageFloppy);
            this.tabControlBoardsAndStorage.Controls.Add(this.tabPagePIADisks);
            this.tabControlBoardsAndStorage.Controls.Add(this.tabPageTTLDisks);
            this.tabControlBoardsAndStorage.Controls.Add(this.tabPageWinchester);
            this.tabControlBoardsAndStorage.Controls.Add(this.tabPageCDS);
            this.tabControlBoardsAndStorage.Controls.Add(this.tabPageKeyboardLayout);
            this.tabControlBoardsAndStorage.Location = new System.Drawing.Point(13, 373);
            this.tabControlBoardsAndStorage.Name = "tabControlBoardsAndStorage";
            this.tabControlBoardsAndStorage.SelectedIndex = 0;
            this.tabControlBoardsAndStorage.Size = new System.Drawing.Size(636, 241);
            this.tabControlBoardsAndStorage.TabIndex = 1;
            // 
            // tabPageBoard
            // 
            this.tabPageBoard.Controls.Add(this.buttonEditBoard);
            this.tabPageBoard.Controls.Add(this.buttonRemoveBoard);
            this.tabPageBoard.Controls.Add(this.buttonAddBoard);
            this.tabPageBoard.Controls.Add(this.listViewBoards);
            this.tabPageBoard.Location = new System.Drawing.Point(4, 22);
            this.tabPageBoard.Name = "tabPageBoard";
            this.tabPageBoard.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageBoard.Size = new System.Drawing.Size(628, 215);
            this.tabPageBoard.TabIndex = 0;
            this.tabPageBoard.Text = "Boards";
            this.tabPageBoard.UseVisualStyleBackColor = true;
            // 
            // buttonEditBoard
            // 
            this.buttonEditBoard.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonEditBoard.Location = new System.Drawing.Point(550, 35);
            this.buttonEditBoard.Name = "buttonEditBoard";
            this.buttonEditBoard.Size = new System.Drawing.Size(75, 23);
            this.buttonEditBoard.TabIndex = 3;
            this.buttonEditBoard.Text = "&Edit";
            this.buttonEditBoard.UseVisualStyleBackColor = true;
            this.buttonEditBoard.Click += new System.EventHandler(this.buttonEditBoard_Click);
            // 
            // buttonRemoveBoard
            // 
            this.buttonRemoveBoard.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonRemoveBoard.Location = new System.Drawing.Point(550, 181);
            this.buttonRemoveBoard.Name = "buttonRemoveBoard";
            this.buttonRemoveBoard.Size = new System.Drawing.Size(75, 23);
            this.buttonRemoveBoard.TabIndex = 2;
            this.buttonRemoveBoard.Text = "&Remove";
            this.buttonRemoveBoard.UseVisualStyleBackColor = true;
            this.buttonRemoveBoard.Click += new System.EventHandler(this.buttonRemoveBoard_Click);
            // 
            // buttonAddBoard
            // 
            this.buttonAddBoard.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonAddBoard.Location = new System.Drawing.Point(547, 6);
            this.buttonAddBoard.Name = "buttonAddBoard";
            this.buttonAddBoard.Size = new System.Drawing.Size(75, 23);
            this.buttonAddBoard.TabIndex = 1;
            this.buttonAddBoard.Text = "&Add";
            this.buttonAddBoard.UseVisualStyleBackColor = true;
            this.buttonAddBoard.Click += new System.EventHandler(this.buttonAddBoard_Click);
            // 
            // listViewBoards
            // 
            this.listViewBoards.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listViewBoards.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.Type,
            this.Address,
            this.Size,
            this.GUID,
            this.IRQ});
            this.listViewBoards.FullRowSelect = true;
            this.listViewBoards.HideSelection = false;
            this.listViewBoards.Location = new System.Drawing.Point(4, 6);
            this.listViewBoards.MultiSelect = false;
            this.listViewBoards.Name = "listViewBoards";
            this.listViewBoards.Size = new System.Drawing.Size(543, 198);
            this.listViewBoards.Sorting = System.Windows.Forms.SortOrder.Ascending;
            this.listViewBoards.TabIndex = 0;
            this.listViewBoards.UseCompatibleStateImageBehavior = false;
            this.listViewBoards.View = System.Windows.Forms.View.Details;
            this.listViewBoards.SelectedIndexChanged += new System.EventHandler(this.listViewBoards_SelectedIndexChanged);
            this.listViewBoards.DoubleClick += new System.EventHandler(this.listViewBoards_DoubleClick);
            // 
            // Type
            // 
            this.Type.Text = "Type";
            this.Type.Width = 101;
            // 
            // Address
            // 
            this.Address.Text = "Address";
            this.Address.Width = 88;
            // 
            // Size
            // 
            this.Size.Text = "Size";
            this.Size.Width = 49;
            // 
            // GUID
            // 
            this.GUID.Text = "GUID";
            this.GUID.Width = 225;
            // 
            // IRQ
            // 
            this.IRQ.Text = "IRQ";
            // 
            // tabPageFloppy
            // 
            this.tabPageFloppy.Controls.Add(this.buttonFloppyDown);
            this.tabPageFloppy.Controls.Add(this.buttonFloppyUp);
            this.tabPageFloppy.Controls.Add(this.buttonEditFloppy);
            this.tabPageFloppy.Controls.Add(this.buttonRemoveFloppy);
            this.tabPageFloppy.Controls.Add(this.buttonAddFloppy);
            this.tabPageFloppy.Controls.Add(this.listViewFloppy);
            this.tabPageFloppy.Location = new System.Drawing.Point(4, 22);
            this.tabPageFloppy.Name = "tabPageFloppy";
            this.tabPageFloppy.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageFloppy.Size = new System.Drawing.Size(628, 215);
            this.tabPageFloppy.TabIndex = 1;
            this.tabPageFloppy.Text = "Floppies";
            this.tabPageFloppy.UseVisualStyleBackColor = true;
            this.tabPageFloppy.Enter += new System.EventHandler(this.tabPageFloppy_Enter);
            // 
            // buttonFloppyDown
            // 
            this.buttonFloppyDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonFloppyDown.Image = ((System.Drawing.Image)(resources.GetObject("buttonFloppyDown.Image")));
            this.buttonFloppyDown.Location = new System.Drawing.Point(568, 111);
            this.buttonFloppyDown.Name = "buttonFloppyDown";
            this.buttonFloppyDown.Size = new System.Drawing.Size(35, 23);
            this.buttonFloppyDown.TabIndex = 9;
            this.buttonFloppyDown.UseVisualStyleBackColor = true;
            this.buttonFloppyDown.Click += new System.EventHandler(this.buttonFloppyDown_Click);
            // 
            // buttonFloppyUp
            // 
            this.buttonFloppyUp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonFloppyUp.Image = ((System.Drawing.Image)(resources.GetObject("buttonFloppyUp.Image")));
            this.buttonFloppyUp.Location = new System.Drawing.Point(568, 81);
            this.buttonFloppyUp.Name = "buttonFloppyUp";
            this.buttonFloppyUp.Size = new System.Drawing.Size(35, 23);
            this.buttonFloppyUp.TabIndex = 8;
            this.buttonFloppyUp.UseVisualStyleBackColor = true;
            this.buttonFloppyUp.Click += new System.EventHandler(this.buttonFloppyUp_Click);
            // 
            // buttonEditFloppy
            // 
            this.buttonEditFloppy.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonEditFloppy.Location = new System.Drawing.Point(550, 35);
            this.buttonEditFloppy.Name = "buttonEditFloppy";
            this.buttonEditFloppy.Size = new System.Drawing.Size(75, 23);
            this.buttonEditFloppy.TabIndex = 7;
            this.buttonEditFloppy.Text = "&Edit";
            this.buttonEditFloppy.UseVisualStyleBackColor = true;
            this.buttonEditFloppy.Click += new System.EventHandler(this.buttonEditFloppy_Click);
            // 
            // buttonRemoveFloppy
            // 
            this.buttonRemoveFloppy.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonRemoveFloppy.Location = new System.Drawing.Point(550, 181);
            this.buttonRemoveFloppy.Name = "buttonRemoveFloppy";
            this.buttonRemoveFloppy.Size = new System.Drawing.Size(75, 23);
            this.buttonRemoveFloppy.TabIndex = 6;
            this.buttonRemoveFloppy.Text = "&Remove";
            this.buttonRemoveFloppy.UseVisualStyleBackColor = true;
            this.buttonRemoveFloppy.Click += new System.EventHandler(this.buttonRemoveFloppy_Click);
            // 
            // buttonAddFloppy
            // 
            this.buttonAddFloppy.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonAddFloppy.Location = new System.Drawing.Point(550, 6);
            this.buttonAddFloppy.Name = "buttonAddFloppy";
            this.buttonAddFloppy.Size = new System.Drawing.Size(75, 23);
            this.buttonAddFloppy.TabIndex = 5;
            this.buttonAddFloppy.Text = "&Add";
            this.buttonAddFloppy.UseVisualStyleBackColor = true;
            this.buttonAddFloppy.Click += new System.EventHandler(this.buttonAddFloppy_Click);
            // 
            // listViewFloppy
            // 
            this.listViewFloppy.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listViewFloppy.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.FloppyDriveNumber,
            this.FloppyFileAndPath,
            this.FloppyFormat});
            this.listViewFloppy.FullRowSelect = true;
            this.listViewFloppy.HideSelection = false;
            this.listViewFloppy.Location = new System.Drawing.Point(4, 6);
            this.listViewFloppy.MultiSelect = false;
            this.listViewFloppy.Name = "listViewFloppy";
            this.listViewFloppy.Size = new System.Drawing.Size(543, 198);
            this.listViewFloppy.TabIndex = 4;
            this.listViewFloppy.UseCompatibleStateImageBehavior = false;
            this.listViewFloppy.View = System.Windows.Forms.View.Details;
            this.listViewFloppy.SelectedIndexChanged += new System.EventHandler(this.listViewFloppy_SelectedIndexChanged);
            this.listViewFloppy.DoubleClick += new System.EventHandler(this.listViewFloppy_DoubleClick);
            // 
            // FloppyDriveNumber
            // 
            this.FloppyDriveNumber.Text = "Drive";
            this.FloppyDriveNumber.Width = 46;
            // 
            // FloppyFileAndPath
            // 
            this.FloppyFileAndPath.Text = "Path";
            this.FloppyFileAndPath.Width = 403;
            // 
            // FloppyFormat
            // 
            this.FloppyFormat.Text = "Format";
            this.FloppyFormat.Width = 49;
            // 
            // tabPagePIADisks
            // 
            this.tabPagePIADisks.Controls.Add(this.buttonPIADiskDown);
            this.tabPagePIADisks.Controls.Add(this.buttonPIADiskUp);
            this.tabPagePIADisks.Controls.Add(this.buttonEditPIADisk);
            this.tabPagePIADisks.Controls.Add(this.buttonRemovePIADisk);
            this.tabPagePIADisks.Controls.Add(this.buttonAddPIADisk);
            this.tabPagePIADisks.Controls.Add(this.listViewPIADisks);
            this.tabPagePIADisks.Location = new System.Drawing.Point(4, 22);
            this.tabPagePIADisks.Name = "tabPagePIADisks";
            this.tabPagePIADisks.Size = new System.Drawing.Size(628, 215);
            this.tabPagePIADisks.TabIndex = 4;
            this.tabPagePIADisks.Text = "PIA Disks";
            this.tabPagePIADisks.UseVisualStyleBackColor = true;
            this.tabPagePIADisks.Enter += new System.EventHandler(this.tabPagePIADisks_Enter);
            // 
            // buttonPIADiskDown
            // 
            this.buttonPIADiskDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonPIADiskDown.Image = ((System.Drawing.Image)(resources.GetObject("buttonPIADiskDown.Image")));
            this.buttonPIADiskDown.Location = new System.Drawing.Point(568, 111);
            this.buttonPIADiskDown.Name = "buttonPIADiskDown";
            this.buttonPIADiskDown.Size = new System.Drawing.Size(35, 23);
            this.buttonPIADiskDown.TabIndex = 15;
            this.buttonPIADiskDown.UseVisualStyleBackColor = true;
            this.buttonPIADiskDown.Click += new System.EventHandler(this.buttonPIADiskDown_Click);
            // 
            // buttonPIADiskUp
            // 
            this.buttonPIADiskUp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonPIADiskUp.Image = ((System.Drawing.Image)(resources.GetObject("buttonPIADiskUp.Image")));
            this.buttonPIADiskUp.Location = new System.Drawing.Point(568, 81);
            this.buttonPIADiskUp.Name = "buttonPIADiskUp";
            this.buttonPIADiskUp.Size = new System.Drawing.Size(35, 23);
            this.buttonPIADiskUp.TabIndex = 14;
            this.buttonPIADiskUp.UseVisualStyleBackColor = true;
            this.buttonPIADiskUp.Click += new System.EventHandler(this.buttonPIADiskUP_Click);
            // 
            // buttonEditPIADisk
            // 
            this.buttonEditPIADisk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonEditPIADisk.Location = new System.Drawing.Point(550, 35);
            this.buttonEditPIADisk.Name = "buttonEditPIADisk";
            this.buttonEditPIADisk.Size = new System.Drawing.Size(75, 23);
            this.buttonEditPIADisk.TabIndex = 13;
            this.buttonEditPIADisk.Text = "&Edit";
            this.buttonEditPIADisk.UseVisualStyleBackColor = true;
            this.buttonEditPIADisk.Click += new System.EventHandler(this.buttonEditPIADisk_Click);
            // 
            // buttonRemovePIADisk
            // 
            this.buttonRemovePIADisk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonRemovePIADisk.Location = new System.Drawing.Point(550, 181);
            this.buttonRemovePIADisk.Name = "buttonRemovePIADisk";
            this.buttonRemovePIADisk.Size = new System.Drawing.Size(75, 23);
            this.buttonRemovePIADisk.TabIndex = 12;
            this.buttonRemovePIADisk.Text = "&Remove";
            this.buttonRemovePIADisk.UseVisualStyleBackColor = true;
            this.buttonRemovePIADisk.Click += new System.EventHandler(this.buttonRemovePIADisk_Click);
            // 
            // buttonAddPIADisk
            // 
            this.buttonAddPIADisk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonAddPIADisk.Location = new System.Drawing.Point(550, 6);
            this.buttonAddPIADisk.Name = "buttonAddPIADisk";
            this.buttonAddPIADisk.Size = new System.Drawing.Size(75, 23);
            this.buttonAddPIADisk.TabIndex = 11;
            this.buttonAddPIADisk.Text = "&Add";
            this.buttonAddPIADisk.UseVisualStyleBackColor = true;
            this.buttonAddPIADisk.Click += new System.EventHandler(this.buttonAddPIADisk_Click);
            // 
            // listViewPIADisks
            // 
            this.listViewPIADisks.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listViewPIADisks.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3});
            this.listViewPIADisks.FullRowSelect = true;
            this.listViewPIADisks.HideSelection = false;
            this.listViewPIADisks.Location = new System.Drawing.Point(4, 6);
            this.listViewPIADisks.MultiSelect = false;
            this.listViewPIADisks.Name = "listViewPIADisks";
            this.listViewPIADisks.Size = new System.Drawing.Size(543, 198);
            this.listViewPIADisks.TabIndex = 10;
            this.listViewPIADisks.UseCompatibleStateImageBehavior = false;
            this.listViewPIADisks.View = System.Windows.Forms.View.Details;
            this.listViewPIADisks.SelectedIndexChanged += new System.EventHandler(this.listViewPIADisks_SelectedIndexChanged);
            this.listViewPIADisks.DoubleClick += new System.EventHandler(this.listViewPIADisks_DoubleClick);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Drive";
            this.columnHeader1.Width = 46;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Path";
            this.columnHeader2.Width = 403;
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "Format";
            this.columnHeader3.Width = 49;
            // 
            // tabPageTTLDisks
            // 
            this.tabPageTTLDisks.Controls.Add(this.buttonTTLDiskDown);
            this.tabPageTTLDisks.Controls.Add(this.buttonTTLDiskUp);
            this.tabPageTTLDisks.Controls.Add(this.buttonEditTTLDisk);
            this.tabPageTTLDisks.Controls.Add(this.buttonRemoveTTLDisk);
            this.tabPageTTLDisks.Controls.Add(this.buttonAddTTLDisk);
            this.tabPageTTLDisks.Controls.Add(this.listViewTTLDisks);
            this.tabPageTTLDisks.Location = new System.Drawing.Point(4, 22);
            this.tabPageTTLDisks.Name = "tabPageTTLDisks";
            this.tabPageTTLDisks.Size = new System.Drawing.Size(628, 215);
            this.tabPageTTLDisks.TabIndex = 5;
            this.tabPageTTLDisks.Text = "TTL Disks";
            this.tabPageTTLDisks.UseVisualStyleBackColor = true;
            this.tabPageTTLDisks.Enter += new System.EventHandler(this.tabPageTTLDisks_Enter);
            // 
            // buttonTTLDiskDown
            // 
            this.buttonTTLDiskDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonTTLDiskDown.Image = ((System.Drawing.Image)(resources.GetObject("buttonTTLDiskDown.Image")));
            this.buttonTTLDiskDown.Location = new System.Drawing.Point(568, 111);
            this.buttonTTLDiskDown.Name = "buttonTTLDiskDown";
            this.buttonTTLDiskDown.Size = new System.Drawing.Size(35, 23);
            this.buttonTTLDiskDown.TabIndex = 15;
            this.buttonTTLDiskDown.UseVisualStyleBackColor = true;
            this.buttonTTLDiskDown.Click += new System.EventHandler(this.buttonTTLDiskDown_Click);
            // 
            // buttonTTLDiskUp
            // 
            this.buttonTTLDiskUp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonTTLDiskUp.Image = ((System.Drawing.Image)(resources.GetObject("buttonTTLDiskUp.Image")));
            this.buttonTTLDiskUp.Location = new System.Drawing.Point(568, 81);
            this.buttonTTLDiskUp.Name = "buttonTTLDiskUp";
            this.buttonTTLDiskUp.Size = new System.Drawing.Size(35, 23);
            this.buttonTTLDiskUp.TabIndex = 14;
            this.buttonTTLDiskUp.UseVisualStyleBackColor = true;
            this.buttonTTLDiskUp.Click += new System.EventHandler(this.buttonTTLDiskUp_Click);
            // 
            // buttonEditTTLDisk
            // 
            this.buttonEditTTLDisk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonEditTTLDisk.Location = new System.Drawing.Point(550, 35);
            this.buttonEditTTLDisk.Name = "buttonEditTTLDisk";
            this.buttonEditTTLDisk.Size = new System.Drawing.Size(75, 23);
            this.buttonEditTTLDisk.TabIndex = 13;
            this.buttonEditTTLDisk.Text = "&Edit";
            this.buttonEditTTLDisk.UseVisualStyleBackColor = true;
            this.buttonEditTTLDisk.Click += new System.EventHandler(this.buttonEditTTLDisk_Click);
            // 
            // buttonRemoveTTLDisk
            // 
            this.buttonRemoveTTLDisk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonRemoveTTLDisk.Location = new System.Drawing.Point(550, 181);
            this.buttonRemoveTTLDisk.Name = "buttonRemoveTTLDisk";
            this.buttonRemoveTTLDisk.Size = new System.Drawing.Size(75, 23);
            this.buttonRemoveTTLDisk.TabIndex = 12;
            this.buttonRemoveTTLDisk.Text = "&Remove";
            this.buttonRemoveTTLDisk.UseVisualStyleBackColor = true;
            this.buttonRemoveTTLDisk.Click += new System.EventHandler(this.buttonRemoveTTLDisk_Click);
            // 
            // buttonAddTTLDisk
            // 
            this.buttonAddTTLDisk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonAddTTLDisk.Location = new System.Drawing.Point(550, 6);
            this.buttonAddTTLDisk.Name = "buttonAddTTLDisk";
            this.buttonAddTTLDisk.Size = new System.Drawing.Size(75, 23);
            this.buttonAddTTLDisk.TabIndex = 11;
            this.buttonAddTTLDisk.Text = "&Add";
            this.buttonAddTTLDisk.UseVisualStyleBackColor = true;
            this.buttonAddTTLDisk.Click += new System.EventHandler(this.buttonAddTTLDisk_Click);
            // 
            // listViewTTLDisks
            // 
            this.listViewTTLDisks.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listViewTTLDisks.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader4,
            this.columnHeader5,
            this.columnHeader6});
            this.listViewTTLDisks.FullRowSelect = true;
            this.listViewTTLDisks.HideSelection = false;
            this.listViewTTLDisks.Location = new System.Drawing.Point(4, 6);
            this.listViewTTLDisks.MultiSelect = false;
            this.listViewTTLDisks.Name = "listViewTTLDisks";
            this.listViewTTLDisks.Size = new System.Drawing.Size(543, 198);
            this.listViewTTLDisks.TabIndex = 10;
            this.listViewTTLDisks.UseCompatibleStateImageBehavior = false;
            this.listViewTTLDisks.View = System.Windows.Forms.View.Details;
            this.listViewTTLDisks.SelectedIndexChanged += new System.EventHandler(this.listViewTTLDisks_SelectedIndexChanged);
            this.listViewTTLDisks.DoubleClick += new System.EventHandler(this.listViewTTLDisks_DoubleClick);
            // 
            // columnHeader4
            // 
            this.columnHeader4.Text = "Drive";
            this.columnHeader4.Width = 46;
            // 
            // columnHeader5
            // 
            this.columnHeader5.Text = "Path";
            this.columnHeader5.Width = 403;
            // 
            // columnHeader6
            // 
            this.columnHeader6.Text = "Format";
            this.columnHeader6.Width = 49;
            // 
            // tabPageWinchester
            // 
            this.tabPageWinchester.Controls.Add(this.buttonWinchesterDown);
            this.tabPageWinchester.Controls.Add(this.buttonWinchesterUP);
            this.tabPageWinchester.Controls.Add(this.buttonEditWinchester);
            this.tabPageWinchester.Controls.Add(this.buttonRemoveWinchester);
            this.tabPageWinchester.Controls.Add(this.buttonAddWinchester);
            this.tabPageWinchester.Controls.Add(this.listViewWinchester);
            this.tabPageWinchester.Location = new System.Drawing.Point(4, 22);
            this.tabPageWinchester.Name = "tabPageWinchester";
            this.tabPageWinchester.Size = new System.Drawing.Size(628, 215);
            this.tabPageWinchester.TabIndex = 2;
            this.tabPageWinchester.Text = "DMAF3 Winchester";
            this.tabPageWinchester.UseVisualStyleBackColor = true;
            // 
            // buttonWinchesterDown
            // 
            this.buttonWinchesterDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonWinchesterDown.Image = ((System.Drawing.Image)(resources.GetObject("buttonWinchesterDown.Image")));
            this.buttonWinchesterDown.Location = new System.Drawing.Point(568, 111);
            this.buttonWinchesterDown.Name = "buttonWinchesterDown";
            this.buttonWinchesterDown.Size = new System.Drawing.Size(35, 23);
            this.buttonWinchesterDown.TabIndex = 11;
            this.buttonWinchesterDown.UseVisualStyleBackColor = true;
            this.buttonWinchesterDown.Click += new System.EventHandler(this.buttonWinchesterDown_Click);
            // 
            // buttonWinchesterUP
            // 
            this.buttonWinchesterUP.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonWinchesterUP.Image = ((System.Drawing.Image)(resources.GetObject("buttonWinchesterUP.Image")));
            this.buttonWinchesterUP.Location = new System.Drawing.Point(568, 81);
            this.buttonWinchesterUP.Name = "buttonWinchesterUP";
            this.buttonWinchesterUP.Size = new System.Drawing.Size(35, 23);
            this.buttonWinchesterUP.TabIndex = 10;
            this.buttonWinchesterUP.UseVisualStyleBackColor = true;
            this.buttonWinchesterUP.Click += new System.EventHandler(this.buttonWinchesterUP_Click);
            // 
            // buttonEditWinchester
            // 
            this.buttonEditWinchester.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonEditWinchester.Location = new System.Drawing.Point(550, 35);
            this.buttonEditWinchester.Name = "buttonEditWinchester";
            this.buttonEditWinchester.Size = new System.Drawing.Size(75, 23);
            this.buttonEditWinchester.TabIndex = 7;
            this.buttonEditWinchester.Text = "&Edit";
            this.buttonEditWinchester.UseVisualStyleBackColor = true;
            this.buttonEditWinchester.Click += new System.EventHandler(this.buttonEditWinchester_Click);
            // 
            // buttonRemoveWinchester
            // 
            this.buttonRemoveWinchester.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonRemoveWinchester.Location = new System.Drawing.Point(550, 181);
            this.buttonRemoveWinchester.Name = "buttonRemoveWinchester";
            this.buttonRemoveWinchester.Size = new System.Drawing.Size(75, 23);
            this.buttonRemoveWinchester.TabIndex = 6;
            this.buttonRemoveWinchester.Text = "&Remove";
            this.buttonRemoveWinchester.UseVisualStyleBackColor = true;
            this.buttonRemoveWinchester.Click += new System.EventHandler(this.buttonRemoveWinchester_Click);
            // 
            // buttonAddWinchester
            // 
            this.buttonAddWinchester.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonAddWinchester.Location = new System.Drawing.Point(550, 6);
            this.buttonAddWinchester.Name = "buttonAddWinchester";
            this.buttonAddWinchester.Size = new System.Drawing.Size(75, 23);
            this.buttonAddWinchester.TabIndex = 5;
            this.buttonAddWinchester.Text = "&Add";
            this.buttonAddWinchester.UseVisualStyleBackColor = true;
            this.buttonAddWinchester.Click += new System.EventHandler(this.buttonAddWinchester_Click);
            // 
            // listViewWinchester
            // 
            this.listViewWinchester.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listViewWinchester.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.WinchesterDriveNumber,
            this.WinchesterTypeName,
            this.WinchesterPathAndFile,
            this.WinchesterCylinders,
            this.WinchesterHeads,
            this.WinchesterSectorsPerTrack,
            this.WinchesterBytesPerSector});
            this.listViewWinchester.FullRowSelect = true;
            this.listViewWinchester.HideSelection = false;
            this.listViewWinchester.Location = new System.Drawing.Point(4, 6);
            this.listViewWinchester.MultiSelect = false;
            this.listViewWinchester.Name = "listViewWinchester";
            this.listViewWinchester.Size = new System.Drawing.Size(543, 198);
            this.listViewWinchester.TabIndex = 4;
            this.listViewWinchester.UseCompatibleStateImageBehavior = false;
            this.listViewWinchester.View = System.Windows.Forms.View.Details;
            // 
            // WinchesterDriveNumber
            // 
            this.WinchesterDriveNumber.Text = "Drive";
            this.WinchesterDriveNumber.Width = 38;
            // 
            // WinchesterTypeName
            // 
            this.WinchesterTypeName.Text = "Type Name";
            this.WinchesterTypeName.Width = 103;
            // 
            // WinchesterPathAndFile
            // 
            this.WinchesterPathAndFile.Text = "Path";
            this.WinchesterPathAndFile.Width = 186;
            // 
            // WinchesterCylinders
            // 
            this.WinchesterCylinders.Text = "Cylinders";
            this.WinchesterCylinders.Width = 56;
            // 
            // WinchesterHeads
            // 
            this.WinchesterHeads.Text = "Heads";
            this.WinchesterHeads.Width = 45;
            // 
            // WinchesterSectorsPerTrack
            // 
            this.WinchesterSectorsPerTrack.Text = "SPT";
            this.WinchesterSectorsPerTrack.Width = 36;
            // 
            // WinchesterBytesPerSector
            // 
            this.WinchesterBytesPerSector.Text = "BPS";
            this.WinchesterBytesPerSector.Width = 40;
            // 
            // tabPageCDS
            // 
            this.tabPageCDS.Controls.Add(this.buttonCDSDown);
            this.tabPageCDS.Controls.Add(this.buttonCDSUp);
            this.tabPageCDS.Controls.Add(this.listViewCDS);
            this.tabPageCDS.Controls.Add(this.buttonEditCDS);
            this.tabPageCDS.Controls.Add(this.buttonRemoveCDS);
            this.tabPageCDS.Controls.Add(this.buttonAddCDS);
            this.tabPageCDS.Location = new System.Drawing.Point(4, 22);
            this.tabPageCDS.Name = "tabPageCDS";
            this.tabPageCDS.Size = new System.Drawing.Size(628, 215);
            this.tabPageCDS.TabIndex = 3;
            this.tabPageCDS.Text = "DMAF3 CDS";
            this.tabPageCDS.UseVisualStyleBackColor = true;
            // 
            // buttonCDSDown
            // 
            this.buttonCDSDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCDSDown.Image = ((System.Drawing.Image)(resources.GetObject("buttonCDSDown.Image")));
            this.buttonCDSDown.Location = new System.Drawing.Point(568, 111);
            this.buttonCDSDown.Name = "buttonCDSDown";
            this.buttonCDSDown.Size = new System.Drawing.Size(35, 23);
            this.buttonCDSDown.TabIndex = 14;
            this.buttonCDSDown.UseVisualStyleBackColor = true;
            this.buttonCDSDown.Click += new System.EventHandler(this.buttonCDSDown_Click);
            // 
            // buttonCDSUp
            // 
            this.buttonCDSUp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCDSUp.Image = ((System.Drawing.Image)(resources.GetObject("buttonCDSUp.Image")));
            this.buttonCDSUp.Location = new System.Drawing.Point(568, 81);
            this.buttonCDSUp.Name = "buttonCDSUp";
            this.buttonCDSUp.Size = new System.Drawing.Size(35, 23);
            this.buttonCDSUp.TabIndex = 13;
            this.buttonCDSUp.UseVisualStyleBackColor = true;
            this.buttonCDSUp.Click += new System.EventHandler(this.buttonCDSUp_Click);
            // 
            // listViewCDS
            // 
            this.listViewCDS.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listViewCDS.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.CDSDriveNumber,
            this.CDSTypeName,
            this.CDSPathAndFile,
            this.CDSCylinders,
            this.CDSHeads,
            this.CDSSectorsPerTrack,
            this.CDSBytesPerSector});
            this.listViewCDS.FullRowSelect = true;
            this.listViewCDS.HideSelection = false;
            this.listViewCDS.Location = new System.Drawing.Point(4, 6);
            this.listViewCDS.MultiSelect = false;
            this.listViewCDS.Name = "listViewCDS";
            this.listViewCDS.Size = new System.Drawing.Size(543, 198);
            this.listViewCDS.TabIndex = 12;
            this.listViewCDS.UseCompatibleStateImageBehavior = false;
            this.listViewCDS.View = System.Windows.Forms.View.Details;
            // 
            // CDSDriveNumber
            // 
            this.CDSDriveNumber.Text = "Drive";
            this.CDSDriveNumber.Width = 38;
            // 
            // CDSTypeName
            // 
            this.CDSTypeName.Text = "Type Name";
            this.CDSTypeName.Width = 103;
            // 
            // CDSPathAndFile
            // 
            this.CDSPathAndFile.Text = "Path";
            this.CDSPathAndFile.Width = 186;
            // 
            // CDSCylinders
            // 
            this.CDSCylinders.Text = "Cylinders";
            this.CDSCylinders.Width = 56;
            // 
            // CDSHeads
            // 
            this.CDSHeads.Text = "Heads";
            this.CDSHeads.Width = 45;
            // 
            // CDSSectorsPerTrack
            // 
            this.CDSSectorsPerTrack.Text = "SPT";
            this.CDSSectorsPerTrack.Width = 36;
            // 
            // CDSBytesPerSector
            // 
            this.CDSBytesPerSector.Text = "BPS";
            this.CDSBytesPerSector.Width = 40;
            // 
            // buttonEditCDS
            // 
            this.buttonEditCDS.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonEditCDS.Location = new System.Drawing.Point(550, 35);
            this.buttonEditCDS.Name = "buttonEditCDS";
            this.buttonEditCDS.Size = new System.Drawing.Size(75, 23);
            this.buttonEditCDS.TabIndex = 7;
            this.buttonEditCDS.Text = "&Edit";
            this.buttonEditCDS.UseVisualStyleBackColor = true;
            this.buttonEditCDS.Click += new System.EventHandler(this.buttonEditCDS_Click);
            // 
            // buttonRemoveCDS
            // 
            this.buttonRemoveCDS.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonRemoveCDS.Location = new System.Drawing.Point(550, 181);
            this.buttonRemoveCDS.Name = "buttonRemoveCDS";
            this.buttonRemoveCDS.Size = new System.Drawing.Size(75, 23);
            this.buttonRemoveCDS.TabIndex = 6;
            this.buttonRemoveCDS.Text = "&Remove";
            this.buttonRemoveCDS.UseVisualStyleBackColor = true;
            this.buttonRemoveCDS.Click += new System.EventHandler(this.buttonRemoveCDS_Click);
            // 
            // buttonAddCDS
            // 
            this.buttonAddCDS.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonAddCDS.Location = new System.Drawing.Point(550, 6);
            this.buttonAddCDS.Name = "buttonAddCDS";
            this.buttonAddCDS.Size = new System.Drawing.Size(75, 23);
            this.buttonAddCDS.TabIndex = 5;
            this.buttonAddCDS.Text = "&Add";
            this.buttonAddCDS.UseVisualStyleBackColor = true;
            this.buttonAddCDS.Click += new System.EventHandler(this.buttonAddCDS_Click);
            // 
            // tabPageKeyboardLayout
            // 
            this.tabPageKeyboardLayout.Controls.Add(this.button1);
            this.tabPageKeyboardLayout.Controls.Add(this.button2);
            this.tabPageKeyboardLayout.Controls.Add(this.button3);
            this.tabPageKeyboardLayout.Controls.Add(this.listViewKeyboardMap);
            this.tabPageKeyboardLayout.Location = new System.Drawing.Point(4, 22);
            this.tabPageKeyboardLayout.Name = "tabPageKeyboardLayout";
            this.tabPageKeyboardLayout.Size = new System.Drawing.Size(628, 215);
            this.tabPageKeyboardLayout.TabIndex = 6;
            this.tabPageKeyboardLayout.Text = "Keyboard Layout";
            this.tabPageKeyboardLayout.UseVisualStyleBackColor = true;
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.Location = new System.Drawing.Point(550, 35);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 2;
            this.button1.Text = "&Edit";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // button2
            // 
            this.button2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button2.Location = new System.Drawing.Point(550, 181);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 3;
            this.button2.Text = "&Remove";
            this.button2.UseVisualStyleBackColor = true;
            // 
            // button3
            // 
            this.button3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button3.Location = new System.Drawing.Point(550, 6);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(75, 23);
            this.button3.TabIndex = 1;
            this.button3.Text = "&Add";
            this.button3.UseVisualStyleBackColor = true;
            // 
            // listViewKeyboardMap
            // 
            this.listViewKeyboardMap.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listViewKeyboardMap.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.Key,
            this.Normal,
            this.Shifted,
            this.Control,
            this.Alt,
            this.Both});
            this.listViewKeyboardMap.FullRowSelect = true;
            this.listViewKeyboardMap.HideSelection = false;
            this.listViewKeyboardMap.Location = new System.Drawing.Point(4, 6);
            this.listViewKeyboardMap.MultiSelect = false;
            this.listViewKeyboardMap.Name = "listViewKeyboardMap";
            this.listViewKeyboardMap.Size = new System.Drawing.Size(543, 198);
            this.listViewKeyboardMap.TabIndex = 0;
            this.listViewKeyboardMap.UseCompatibleStateImageBehavior = false;
            this.listViewKeyboardMap.View = System.Windows.Forms.View.Details;
            // 
            // Key
            // 
            this.Key.Text = "Key";
            this.Key.Width = 165;
            // 
            // Normal
            // 
            this.Normal.Text = "Normal";
            this.Normal.Width = 70;
            // 
            // Shifted
            // 
            this.Shifted.Text = "Shifted";
            this.Shifted.Width = 70;
            // 
            // Control
            // 
            this.Control.Text = "Control";
            this.Control.Width = 70;
            // 
            // Alt
            // 
            this.Alt.Text = "Alt";
            this.Alt.Width = 70;
            // 
            // Both
            // 
            this.Both.Text = "Both";
            this.Both.Width = 70;
            // 
            // memulatorConfigEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(661, 626);
            this.Controls.Add(this.tabControlBoardsAndStorage);
            this.Controls.Add(this.groupBoxGlobal);
            this.Controls.Add(this.menuStrip);
            this.MainMenuStrip = this.menuStrip;
            this.MaximizeBox = false;
            this.Name = "memulatorConfigEditor";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "memulator Configuration Editor";
            this.Load += new System.EventHandler(this.MemulatorConfigEditor_Load);
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.groupBoxGlobal.ResumeLayout(false);
            this.groupBoxGlobal.PerformLayout();
            this.groupBoxProcessorBoard.ResumeLayout(false);
            this.groupBoxProcessorBoard.PerformLayout();
            this.groupBoxProcessor.ResumeLayout(false);
            this.groupBoxProcessor.PerformLayout();
            this.tabControlBoardsAndStorage.ResumeLayout(false);
            this.tabPageBoard.ResumeLayout(false);
            this.tabPageFloppy.ResumeLayout(false);
            this.tabPagePIADisks.ResumeLayout(false);
            this.tabPageTTLDisks.ResumeLayout(false);
            this.tabPageWinchester.ResumeLayout(false);
            this.tabPageCDS.ResumeLayout(false);
            this.tabPageKeyboardLayout.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.ToolStripMenuItem fIleToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.GroupBox groupBoxGlobal;
        private System.Windows.Forms.Button buttonBrowseConsoleDumpFile;
        private System.Windows.Forms.TextBox textBoxConsoleDumpFile;
        private System.Windows.Forms.Label labelConsoleDumpFile;
        private System.Windows.Forms.Label labelProcessorJumpers;
        private System.Windows.Forms.CheckBox checkBoxLowHigh;
        private System.Windows.Forms.CheckBox checkBox600_4800;
        private System.Windows.Forms.CheckBox checkBox150_9600;
        private System.Windows.Forms.CheckBox checkBoxF000RAM;
        private System.Windows.Forms.CheckBox checkBoxE800RAM;
        private System.Windows.Forms.CheckBox checkBoxE000RAM;
        private System.Windows.Forms.RadioButton radioButton6809;
        private System.Windows.Forms.RadioButton radioButton6800;
        private System.Windows.Forms.Label labelProcessor;
        private System.Windows.Forms.Label labelROM;
        private System.Windows.Forms.Label labelRAM;
        private System.Windows.Forms.Label labelBaudRate;
        private System.Windows.Forms.CheckBox checkBoxF000ROM;
        private System.Windows.Forms.CheckBox checkBoxE800ROM;
        private System.Windows.Forms.CheckBox checkBoxE000ROM;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox checkBoxSW1D;
        private System.Windows.Forms.CheckBox checkBoxSW1C;
        private System.Windows.Forms.CheckBox checkBoxSW1B;
        private System.Windows.Forms.RadioButton radioButtonMPU_1;
        private System.Windows.Forms.RadioButton radioButtonMP_09;
        private System.Windows.Forms.Label labelProcessorBoard;
        private System.Windows.Forms.GroupBox groupBoxProcessorBoard;
        private System.Windows.Forms.GroupBox groupBoxProcessor;
        private System.Windows.Forms.TextBox textBoxWinchesterInterruptDelay;
        private System.Windows.Forms.Label labelWinchesterInterruptDelay;
        private System.Windows.Forms.CheckBox checkBoxAllowMultiSector;
        private System.Windows.Forms.Button buttonBrowseTraceFile;
        private System.Windows.Forms.TextBox textBoxTraceFile;
        private System.Windows.Forms.CheckBox checkBoxEnableTrace;
        private System.Windows.Forms.TabControl tabControlBoardsAndStorage;
        private System.Windows.Forms.TabPage tabPageBoard;
        private System.Windows.Forms.TabPage tabPageFloppy;
        private System.Windows.Forms.TabPage tabPageWinchester;
        private System.Windows.Forms.TabPage tabPageCDS;
        private System.Windows.Forms.Button buttonBrowseROMFile;
        private System.Windows.Forms.TextBox textBoxROMFile;
        private System.Windows.Forms.Label labelROMFile;
        private System.Windows.Forms.ListView listViewBoards;
        private System.Windows.Forms.ColumnHeader Type;
        private System.Windows.Forms.ColumnHeader Address;
        private System.Windows.Forms.ColumnHeader Size;
        private System.Windows.Forms.ColumnHeader GUID;
        private System.Windows.Forms.ColumnHeader IRQ;
        private System.Windows.Forms.Button buttonEditBoard;
        private System.Windows.Forms.Button buttonRemoveBoard;
        private System.Windows.Forms.Button buttonAddBoard;
        private System.Windows.Forms.Button buttonEditFloppy;
        private System.Windows.Forms.Button buttonRemoveFloppy;
        private System.Windows.Forms.Button buttonAddFloppy;
        private System.Windows.Forms.ListView listViewFloppy;
        private System.Windows.Forms.ColumnHeader FloppyDriveNumber;
        private System.Windows.Forms.ColumnHeader FloppyFileAndPath;
        private System.Windows.Forms.ColumnHeader FloppyFormat;
        private System.Windows.Forms.Button buttonEditWinchester;
        private System.Windows.Forms.Button buttonRemoveWinchester;
        private System.Windows.Forms.Button buttonAddWinchester;
        private System.Windows.Forms.ListView listViewWinchester;
        private System.Windows.Forms.ColumnHeader WinchesterDriveNumber;
        private System.Windows.Forms.ColumnHeader WinchesterTypeName;
        private System.Windows.Forms.ColumnHeader WinchesterPathAndFile;
        private System.Windows.Forms.ColumnHeader WinchesterCylinders;
        private System.Windows.Forms.ColumnHeader WinchesterHeads;
        private System.Windows.Forms.Button buttonEditCDS;
        private System.Windows.Forms.Button buttonRemoveCDS;
        private System.Windows.Forms.Button buttonAddCDS;
        private System.Windows.Forms.Button buttonFloppyDown;
        private System.Windows.Forms.Button buttonFloppyUp;
        private System.Windows.Forms.Button buttonWinchesterDown;
        private System.Windows.Forms.Button buttonWinchesterUP;
        private System.Windows.Forms.ColumnHeader WinchesterSectorsPerTrack;
        private System.Windows.Forms.ColumnHeader WinchesterBytesPerSector;
        private System.Windows.Forms.Button buttonCDSDown;
        private System.Windows.Forms.Button buttonCDSUp;
        private System.Windows.Forms.ListView listViewCDS;
        private System.Windows.Forms.ColumnHeader CDSDriveNumber;
        private System.Windows.Forms.ColumnHeader CDSTypeName;
        private System.Windows.Forms.ColumnHeader CDSPathAndFile;
        private System.Windows.Forms.ColumnHeader CDSCylinders;
        private System.Windows.Forms.ColumnHeader CDSHeads;
        private System.Windows.Forms.ColumnHeader CDSSectorsPerTrack;
        private System.Windows.Forms.ColumnHeader CDSBytesPerSector;
        private System.Windows.Forms.TabPage tabPagePIADisks;
        private System.Windows.Forms.TabPage tabPageTTLDisks;
        private System.Windows.Forms.Button buttonPIADiskDown;
        private System.Windows.Forms.Button buttonPIADiskUp;
        private System.Windows.Forms.Button buttonEditPIADisk;
        private System.Windows.Forms.Button buttonRemovePIADisk;
        private System.Windows.Forms.Button buttonAddPIADisk;
        private System.Windows.Forms.ListView listViewPIADisks;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.Button buttonTTLDiskDown;
        private System.Windows.Forms.Button buttonTTLDiskUp;
        private System.Windows.Forms.Button buttonEditTTLDisk;
        private System.Windows.Forms.Button buttonRemoveTTLDisk;
        private System.Windows.Forms.Button buttonAddTTLDisk;
        private System.Windows.Forms.ListView listViewTTLDisks;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.ColumnHeader columnHeader5;
        private System.Windows.Forms.ColumnHeader columnHeader6;
        private System.Windows.Forms.RadioButton radioButtonGeneric;
        private System.Windows.Forms.TabPage tabPageKeyboardLayout;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.ListView listViewKeyboardMap;
        private System.Windows.Forms.ColumnHeader Key;
        private System.Windows.Forms.ColumnHeader Normal;
        private System.Windows.Forms.ColumnHeader Shifted;
        private System.Windows.Forms.ColumnHeader Control;
        private System.Windows.Forms.ColumnHeader Alt;
        private System.Windows.Forms.ColumnHeader Both;
        private System.Windows.Forms.Button buttonBrowseStatisticsFile;
        private System.Windows.Forms.TextBox textBoxStatisticsFile;
        private System.Windows.Forms.Label labelStatisticsFile;
        private System.Windows.Forms.Button buttonBrowseCoreDumpFile;
        private System.Windows.Forms.TextBox textBoxCoreDumpFile;
        private System.Windows.Forms.Label labelCoreDumpFile;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button3;
    }
}

