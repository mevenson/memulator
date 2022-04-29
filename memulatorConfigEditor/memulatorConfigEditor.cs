using System;
using System.Collections.Generic;
using System.Windows.Forms;

using System.IO;
using System.Xml;

namespace ConfigEditor
{
    enum Devices
    {
        DEVICE_INVALID_ADDR = -1,

        DEVICE_RAM = 0,   // address is RAM
        DEVICE_ROM = 1,   // address is ROM

        // All IO devices must be specified as > 1

        DEVICE_CONS = 2,   // address belongs to console
        DEVICE_MPS = 3,   // address belongs to a non console MP-S
        DEVICE_FDC = 4,   // address belongs to floppy controller
        DEVICE_MPT = 5,   // address belongs to Timer card
        DEVICE_DMAF1 = 6,   // address belongs to DMAF1
        DEVICE_DMAF2 = 7,   // address belongs to DMAF2
        DEVICE_DMAF3 = 8,   // address belongs to DMAF3
        DEVICE_PRT = 9,   // address belongs to Printer
        DEVICE_MPL = 10,   // address belongs to non printer MP-L
        DEVICE_DAT = 11,   // address belongs to DAT outside ROM space
        DEVICE_IOP = 12,
        DEVICE_MPID = 13,   // address belongs to MP-ID Timer card
        DEVICE_PIAIDE = 14,   // address belongs to a PIA that is hooked up to an IDE drive
        DEVICE_TTLIDE = 15,   // address belongs to a PIA that is hooked up to an IDE drive
        DEVICE_8274 = 16,   // address belongs to a PIA that is hooked up to an IDE drive
        DEVICE_PCSTREAM = 17,   // An interface to talk to a PC Stream.

        DEVICE_AHO = 0x80   // Use Component Object Model to access hardware
    }

    public partial class memulatorConfigEditor : Form
    {
        public class BoardInfoClass
        {
            public byte cDeviceType;
            public ushort sBaseAddress;
            public ushort sNumberOfBytes;
            public bool bInterruptEnabled;
            public String strGuid;

            //IAHODevicePtr   ahoDevice;
        }

        public class ACIAConfig
        {
            public int _baudRate = 9600;
            public int _parity = 0;
            public int _stopBits = 1;
            public int _dataBits = 8;
            public bool _interuptEnabled = false;
        }

        public class SupportedWinchesterDrive
        {
            public int bytesPerSector;
            public int cylinders;
            public int heads;
            public int sectorsPerTrack;
        }

        public Dictionary<string, SupportedWinchesterDrive> supportedWinchesterDrives = new Dictionary<string, SupportedWinchesterDrive>
        {
            {"CMI 5619 (19Mb)"  , new SupportedWinchesterDrive {bytesPerSector=0x200, cylinders=0x132, heads=6, sectorsPerTrack=0x11}},
            {"CMI 5640 (40Mb)"  , new SupportedWinchesterDrive {bytesPerSector=0x200, cylinders=0x280, heads=6, sectorsPerTrack=0x11}},
            {"D-514 (RMS)]"     , new SupportedWinchesterDrive {bytesPerSector=0x200, cylinders=0x132, heads=4, sectorsPerTrack=0x11}},
            {"D-526 (RMS)]"     , new SupportedWinchesterDrive {bytesPerSector=0x200, cylinders=0x132, heads=8, sectorsPerTrack=0x11}},
            {"RMS 506]"         , new SupportedWinchesterDrive {bytesPerSector=0x200, cylinders=0x099, heads=4, sectorsPerTrack=0x11}},
            {"RMS 509]"         , new SupportedWinchesterDrive {bytesPerSector=0x200, cylinders=0x0d7, heads=4, sectorsPerTrack=0x11}},
            {"RMS 512]"         , new SupportedWinchesterDrive {bytesPerSector=0x200, cylinders=0x099, heads=8, sectorsPerTrack=0x11}},
            {"RMS 518]"         , new SupportedWinchesterDrive {bytesPerSector=0x200, cylinders=0x0d7, heads=8, sectorsPerTrack=0x11}},
            {"RO 201]"          , new SupportedWinchesterDrive {bytesPerSector=0x200, cylinders=0x141, heads=2, sectorsPerTrack=0x11}},
            {"RO 202]"          , new SupportedWinchesterDrive {bytesPerSector=0x200, cylinders=0x141, heads=4, sectorsPerTrack=0x11}},
            {"RO 203]"          , new SupportedWinchesterDrive {bytesPerSector=0x200, cylinders=0x141, heads=6, sectorsPerTrack=0x11}},
            {"RO 204]"          , new SupportedWinchesterDrive {bytesPerSector=0x200, cylinders=0x141, heads=8, sectorsPerTrack=0x11}},
            {"Rodime RO5090]"   , new SupportedWinchesterDrive {bytesPerSector=0x200, cylinders=0x4c8, heads=7, sectorsPerTrack=0x11}},
            {"ST 412]"          , new SupportedWinchesterDrive {bytesPerSector=0x200, cylinders=0x132, heads=4, sectorsPerTrack=0x11}},
            {"ST 506]"          , new SupportedWinchesterDrive {bytesPerSector=0x200, cylinders=0x099, heads=4, sectorsPerTrack=0x11}},
            {"TMS 503]"         , new SupportedWinchesterDrive {bytesPerSector=0x200, cylinders=0x099, heads=4, sectorsPerTrack=0x11}},
        };

        public class MPSBoard
        {
            public int _boardNumber = 0;
            public List<ACIAConfig> aciaConfigurations = new List<ACIAConfig>();
        }

        public static List<MPSBoard> mpsBoards = new List<MPSBoard>();
        public static BoardInfoClass[] _stBoardInfo = new BoardInfoClass[32];

        static public List<string> _deviceNames = new List<string>();

        static string configFileName = "";
        static string _configSection = "";
        public static string ConfigSection
        {
            get { return memulatorConfigEditor._configSection; }
            set { memulatorConfigEditor._configSection = value; }
        }
        static bool   _allowMultipleSector = false;

        static string   _consoleDumpFile = "";
        static bool     _bTraceEnabled   = false;
        static string   _traceFilePath   = "";
        
        static bool     _b9600           = false;
        static bool     _b4800           = false;
        static bool     _bLowHigh        = false;

        static bool     _bEnableScratchPad = false;
        static string   _processorBoard = "";

        static string   _processorBoardCpu = "";

        static bool cpuRunning = false;

        static public string dataDir = "";

        public memulatorConfigEditor(string _configFileFullName, bool _cpuRunning)
        {
            // this logic gives precedence to the common AppDir over the User AppDir. If neither exist - uses execution directory as dataDir.

            dataDir = Memulator.Program.dataDir;

            cpuRunning = _cpuRunning;

            InitializeComponent();
            SetProcessorBoardCheckBoxes();

            _deviceNames.Add("RAM");        // address is RAM
            _deviceNames.Add("ROM");        // address is ROM
            _deviceNames.Add("CONS");       // address belongs to console
            _deviceNames.Add("MPS");        // address belongs to a non console MP-S
            _deviceNames.Add("FD2");        // address belongs to floppy controller
            _deviceNames.Add("MPT");        // address belongs to Timer card
            _deviceNames.Add("DMAF1");      // address belongs to DMAF1
            _deviceNames.Add("DMAF2");      // address belongs to DMAF2
            _deviceNames.Add("DMAF3");      // address belongs to DMAF3
            _deviceNames.Add("PRT");        // address belongs to Printer
            _deviceNames.Add("MPL");        // address belongs to non printer MP-L
            _deviceNames.Add("DAT");        // address belongs to DAT outside ROM space
            _deviceNames.Add("IOP");
            _deviceNames.Add("MPID");       // address belongs to MP-ID Timer card
            _deviceNames.Add("PIAIDE");     // address belongs to a PIA that is hooked up to an IDE drive
            _deviceNames.Add("TTLIDE");     // address belongs to a PIA that is hooked up to an IDE drive
            _deviceNames.Add("8274");       // address belongs to a PIA that is hooked up to an IDE drive
            _deviceNames.Add("PCSTREAM");   // An interface to talk to a PC Stream.

            configFileName = _configFileFullName;
            LoadFromXmlFile();

            if (cpuRunning)
            {
                radioButton6800.Enabled = false;
                radioButton6809.Enabled = false;
                radioButtonMPU_1.Enabled = false;
                radioButtonMP_09.Enabled = false;
                radioButtonGeneric.Enabled = false;

                textBoxROMFile.Enabled = false;
                buttonBrowseROMFile.Enabled = false;

                checkBoxE000RAM.Enabled = false;
                checkBoxE000ROM.Enabled = false;
                checkBoxE800RAM.Enabled = false;
                checkBoxE800ROM.Enabled = false;
                checkBoxF000RAM.Enabled = false;
                checkBoxF000ROM.Enabled = false;

                checkBoxSW1B.Enabled = false;
                checkBoxSW1C.Enabled = false;
                checkBoxSW1D.Enabled = false;

                listViewBoards.Enabled = false;

                buttonAddBoard.Enabled = false;
                buttonEditBoard.Enabled = false;
                buttonRemoveBoard.Enabled = false;
            }
        }

        #region ToolStrip Menu Item Handler Code

        private void Save(string filename)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;

            XmlWriter writer;
            writer = XmlWriter.Create(filename, settings);

            writer.WriteStartElement("configuration");
            {
                writer.WriteStartElement("Global");
                {
                    // do global stuff

                    if (textBoxConsoleDumpFile.Text.Length > 0)
                    {
                        writer.WriteStartElement("ConsoleDump");
                        {
                            writer.WriteAttributeString("filename", textBoxConsoleDumpFile.Text);
                            writer.WriteEndElement();
                        }
                    }

                    writer.WriteStartElement("ProcessorBoard");
                    {
                        writer.WriteAttributeString("CPU", _processorBoardCpu);
                        writer.WriteAttributeString("Board", _processorBoard);
                        if (_processorBoardCpu == "6800")
                            writer.WriteAttributeString("EnableScratchPadRAM", _bEnableScratchPad ? "1" : "0");
                        writer.WriteEndElement();
                    }

                    writer.WriteStartElement("Trace");
                    {
                        writer.WriteAttributeString("Enabled", _bTraceEnabled ? "1" : "0");
                        writer.WriteAttributeString("Path", _traceFilePath);
                        writer.WriteEndElement();
                    }

                    writer.WriteStartElement("WinchesterInterruptDelay");
                    {
                        writer.WriteAttributeString("value", textBoxWinchesterInterruptDelay.Text);
                        writer.WriteEndElement();
                    }

                    if (textBoxStatisticsFile.Text.Length > 0)
                    {
                        writer.WriteStartElement("Statistics");
                        {
                            writer.WriteAttributeString("filename", textBoxStatisticsFile.Text);
                            writer.WriteEndElement();
                        }
                    }

                    if (textBoxCoreDumpFile.Text.Length > 0)
                    {
                        writer.WriteStartElement("CoreDump");
                        {
                            writer.WriteAttributeString("filename", textBoxCoreDumpFile.Text);
                            writer.WriteEndElement();
                        }
                    }

                    writer.WriteStartElement("AllowMultipleSector");
                    {
                        writer.WriteAttributeString("value", _allowMultipleSector ? "1" : "0");
                        writer.WriteEndElement();
                    }

                    if (_processorBoardCpu == "6809")
                    {
                        writer.WriteStartElement("ProcessorJumpers");
                        {
                            writer.WriteAttributeString("J_150_9600", _b9600 ? "1" : "0");
                            writer.WriteAttributeString("J_600_4800", _b4800 ? "1" : "0");
                            writer.WriteAttributeString("J_LOW_HIGH", _bLowHigh ? "1" : "0");

                            writer.WriteAttributeString("E000_RAM", checkBoxE000RAM.Checked == true ? "1" : "0");
                            writer.WriteAttributeString("E800_RAM", checkBoxE800RAM.Checked == true ? "1" : "0");
                            writer.WriteAttributeString("F000_RAM", checkBoxF000RAM.Checked == true ? "1" : "0");
                            writer.WriteAttributeString("E000_ROM", checkBoxE000ROM.Checked == true ? "1" : "0");
                            writer.WriteAttributeString("E800_ROM", checkBoxE800ROM.Checked == true ? "1" : "0");
                            writer.WriteAttributeString("F000_ROM", checkBoxF000ROM.Checked == true ? "1" : "0");
                            writer.WriteEndElement();
                        }

                        writer.WriteStartElement("ProcessorSwitch");
                        {
                            writer.WriteAttributeString("SW1B", checkBoxSW1B.Checked == true ? "1" : "0");
                            writer.WriteAttributeString("SW1C", checkBoxSW1C.Checked == true ? "1" : "0");
                            writer.WriteAttributeString("SW1D", checkBoxSW1D.Checked == true ? "1" : "0");
                            writer.WriteEndElement();
                        }
                    }

                    // done with global stuff - close the global section

                    writer.WriteEndElement();
                }

                // now do the processor specific stuff

                writer.WriteStartElement("config" + _processorBoardCpu);
                {
                    writer.WriteStartElement("romfile");
                    {
                        writer.WriteAttributeString("filename", textBoxROMFile.Text);
                        writer.WriteEndElement();
                    }

                    if (_stBoardInfo.Length > 0)
                    {
                        writer.WriteStartElement("BoardConfiguration");
                        int i = 0;
                        for (int index = 0; index < listViewBoards.Items.Count; index++)
                        {
                            writer.WriteStartElement("Board");
                            {
                                writer.WriteAttributeString("ID", i++.ToString());
                                writer.WriteAttributeString("Type", listViewBoards.Items[index].SubItems[0].Text);
                                writer.WriteAttributeString("Addr", listViewBoards.Items[index].SubItems[1].Text.Replace("0x", ""));
                                writer.WriteAttributeString("Size", listViewBoards.Items[index].SubItems[2].Text.Replace("0x", ""));
                                writer.WriteAttributeString("GUID", "");
                                writer.WriteAttributeString("IRQ", listViewBoards.Items[index].SubItems[4].Text == "Yes" ? "1" : "0");
                                writer.WriteEndElement();
                            }
                        }
                        writer.WriteEndElement();
                    }

                    if (mpsBoards.Count > 0)
                    {
                        writer.WriteStartElement("SerialPorts");
                        foreach (MPSBoard b in mpsBoards)
                        {
                            int portNumber = 0;
                            foreach (ACIAConfig conf in b.aciaConfigurations)
                            {
                                writer.WriteStartElement("Board_Port");
                                {
                                    writer.WriteAttributeString("ID",               b._boardNumber.ToString() + "_" + (portNumber + 1).ToString());
                                    writer.WriteAttributeString("BaudRate",         conf._baudRate.ToString());
                                    writer.WriteAttributeString("StopBits",         conf._stopBits.ToString());
                                    writer.WriteAttributeString("DataBits",         conf._dataBits.ToString());
                                    writer.WriteAttributeString("Parity",           conf._parity.ToString());
                                    writer.WriteAttributeString("InterruptEnabled", conf._interuptEnabled ? "1" : "0");

                                    portNumber++;
                                }
                                writer.WriteEndElement();
                            }
                        }
                        writer.WriteEndElement();
                    }

                    bool listHasEntries = false;
                    for (int index = 0; index < listViewFloppy.Items.Count; index++)
                    {
                        if (listViewFloppy.Items[index].SubItems[1].Text.Length > 0)
                        {
                            listHasEntries = true;
                            break;
                        }
                    }

                    if (listHasEntries)
                    {
                        writer.WriteStartElement("FloppyDisks");
                        for (int floppyindex = 0; floppyindex < listViewFloppy.Items.Count; floppyindex++)
                        {
                            string id = listViewFloppy.Items[floppyindex].SubItems[0].Text;
                            string path = listViewFloppy.Items[floppyindex].SubItems[1].Text;
                            string format = listViewFloppy.Items[floppyindex].SubItems[2].Text;

                            if (path != "")
                            {
                                writer.WriteStartElement("Disk");
                                {
                                    writer.WriteAttributeString("ID", floppyindex.ToString());
                                    writer.WriteAttributeString("Path", path);
                                    writer.WriteAttributeString("Format", format);
                                    writer.WriteEndElement();
                                }
                            }
                        }
                        writer.WriteEndElement();
                    }

                    if (listViewWinchester.Items.Count > 0)
                    {
                        writer.WriteStartElement("WinchesterDrives");
                        for (int winindex = 0; winindex < listViewWinchester.Items.Count; winindex++)
                        {
                            string id = listViewWinchester.Items[winindex].SubItems[0].Text;
                            string typename = listViewWinchester.Items[winindex].SubItems[1].Text;
                            string path = listViewWinchester.Items[winindex].SubItems[2].Text;
                            string cylinders = listViewWinchester.Items[winindex].SubItems[3].Text;
                            string heads = listViewWinchester.Items[winindex].SubItems[4].Text;
                            string SectorsPerTrack = listViewWinchester.Items[winindex].SubItems[5].Text;
                            string BytesPerSector = listViewWinchester.Items[winindex].SubItems[6].Text;

                            if (path != "")
                            {
                                writer.WriteStartElement("Disk");
                                {
                                    writer.WriteAttributeString("ID", winindex.ToString());
                                    writer.WriteAttributeString("TypeName", typename);
                                    writer.WriteAttributeString("Path", path);
                                    writer.WriteAttributeString("Cylinders", cylinders);
                                    writer.WriteAttributeString("Heads", heads);
                                    writer.WriteAttributeString("SectorsPerTrack", SectorsPerTrack);
                                    writer.WriteAttributeString("BytesPerSector", BytesPerSector);
                                    writer.WriteEndElement();
                                }
                            }
                        }
                        writer.WriteEndElement();
                    }

                    if (listViewCDS.Items.Count > 0)
                    {
                        writer.WriteStartElement("CDSDrives");
                        for (int cdsindex = 0; cdsindex < listViewCDS.Items.Count; cdsindex++)
                        {
                            string id = listViewCDS.Items[cdsindex].SubItems[0].Text;
                            string typename = listViewCDS.Items[cdsindex].SubItems[1].Text;
                            string path = listViewCDS.Items[cdsindex].SubItems[2].Text;
                            string cylinders = listViewCDS.Items[cdsindex].SubItems[3].Text;
                            string heads = listViewCDS.Items[cdsindex].SubItems[4].Text;
                            string SectorsPerTrack = listViewCDS.Items[cdsindex].SubItems[5].Text;
                            string BytesPerSector = listViewCDS.Items[cdsindex].SubItems[6].Text;

                            if (path != "")
                            {
                                writer.WriteStartElement("Disk");
                                {
                                    writer.WriteAttributeString("ID", cdsindex.ToString());
                                    writer.WriteAttributeString("TypeName", typename);
                                    writer.WriteAttributeString("Path", path);
                                    writer.WriteAttributeString("Cylinders", cylinders);
                                    writer.WriteAttributeString("Heads", heads);
                                    writer.WriteAttributeString("SectorsPerTrack", SectorsPerTrack);
                                    writer.WriteAttributeString("BytesPerSector", BytesPerSector);
                                    writer.WriteEndElement();
                                }
                            }
                        }
                        writer.WriteEndElement();
                    }

                    listHasEntries = false;
                    for (int index = 0; index < listViewPIADisks.Items.Count; index++)
                    {
                        if (listViewPIADisks.Items[index].SubItems[1].Text.Length > 0)
                        {
                            listHasEntries = true;
                            break;
                        }
                    }

                    if (listHasEntries)
                    {
                        writer.WriteStartElement("PIAIDEDisks");
                        for (int piaindex = 0; piaindex < listViewPIADisks.Items.Count; piaindex++)
                        {
                            string id = listViewPIADisks.Items[piaindex].SubItems[0].Text;
                            string path = listViewPIADisks.Items[piaindex].SubItems[1].Text;
                            string format = listViewPIADisks.Items[piaindex].SubItems[2].Text;

                            if (path != "")
                            {
                                writer.WriteStartElement("Disk");
                                {
                                    writer.WriteAttributeString("ID", piaindex.ToString());
                                    writer.WriteAttributeString("Path", path);
                                    writer.WriteAttributeString("Format", format);
                                    writer.WriteEndElement();
                                }
                            }
                        }
                        writer.WriteEndElement();
                    }

                    listHasEntries = false;
                    for (int index = 0; index < listViewTTLDisks.Items.Count; index++)
                    {
                        if (listViewTTLDisks.Items[index].SubItems[1].Text.Length > 0)
                        {
                            listHasEntries = true;
                            break;
                        }
                    }

                    if (listHasEntries)
                    {
                        writer.WriteStartElement("TTLIDEDisks");
                        for (int ttlindex = 0; ttlindex < listViewTTLDisks.Items.Count; ttlindex++)
                        {
                            string id = listViewTTLDisks.Items[ttlindex].SubItems[0].Text;
                            string path = listViewTTLDisks.Items[ttlindex].SubItems[1].Text;
                            string format = listViewTTLDisks.Items[ttlindex].SubItems[2].Text;

                            if (path != "")
                            {
                                writer.WriteStartElement("Disk");
                                {
                                    writer.WriteAttributeString("ID", ttlindex.ToString());
                                    writer.WriteAttributeString("Path", path);
                                    writer.WriteAttributeString("Format", format);
                                    writer.WriteEndElement();
                                }
                            }
                        }
                        writer.WriteEndElement();
                    }

                }

                writer.WriteStartElement("KeyBoardMap");
                {
                    for (int keyIndex = 0; keyIndex < listViewKeyboardMap.Items.Count; keyIndex++)
                    {
                        writer.WriteStartElement(listViewKeyboardMap.Items[keyIndex].SubItems[0].Text);
                        {
                            writer.WriteAttributeString("Normal", listViewKeyboardMap.Items[keyIndex].SubItems[1].Text);
                            writer.WriteAttributeString("Shifted", listViewKeyboardMap.Items[keyIndex].SubItems[2].Text);
                            writer.WriteAttributeString("Control", listViewKeyboardMap.Items[keyIndex].SubItems[3].Text);
                            writer.WriteAttributeString("Alt", listViewKeyboardMap.Items[keyIndex].SubItems[4].Text);
                            writer.WriteAttributeString("Both", listViewKeyboardMap.Items[keyIndex].SubItems[5].Text);
                            writer.WriteEndElement();
                        }
                    }
                }
                writer.WriteEndElement();

                //Close the  configuration section
                writer.WriteEndElement();
            }

            // close the document
            writer.WriteEndDocument();

            writer.Close();
        }
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Hide();
        }
        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.InitialDirectory = dataDir;
            DialogResult dr = sfd.ShowDialog();
            if (dr == DialogResult.OK)
            {
                Save(sfd.FileName);
                configFileName = sfd.FileName;
                this.Text = "memulator Configuration Editor - " + configFileName.Replace(dataDir, "");
            }
        }
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Save(configFileName);
        }

        public void SetBoardInfoInList()
        {
            int slot = 0;
            foreach (BoardInfoClass bi in _stBoardInfo)
            {
                if (bi != null)
                {
                    if (bi.cDeviceType <= 17)
                    {
                        ListViewItem iBoard = listViewBoards.Items.Add(_deviceNames[bi.cDeviceType]);

                        iBoard.Tag = slot++;

                        iBoard.SubItems.Add("0x" + bi.sBaseAddress.ToString("X4"));
                        iBoard.SubItems.Add("0x" + bi.sNumberOfBytes.ToString("X2"));
                        iBoard.SubItems.Add(bi.strGuid);
                        iBoard.SubItems.Add((bi.bInterruptEnabled == false) ? "No" : "Yes");
                    }
                }
            }
        }

        private void LoadFromCfgFile()
        {
            ConvertCfgToXml converter = new ConvertCfgToXml();

            converter.ConvertFile(configFileName);
            _processorBoardCpu = converter.ProcessorName;

            _b9600    = converter.B9600;        checkBox150_9600.Checked = _b9600;
            _b4800    = converter.B4800;        checkBox600_4800.Checked = _b4800;
            _bLowHigh = converter.BLowHigh;     checkBoxLowHigh.Checked  = _bLowHigh;

            checkBoxAllowMultiSector.Checked = converter.AllowMultipleSector;
            textBoxROMFile.Text = converter.Romfile;

            _bEnableScratchPad = converter.EnableScratchPad;
            _allowMultipleSector = converter.AllowMultipleSector;

            checkBoxEnableTrace.Checked = converter.TraceEnabled;
            textBoxTraceFile.Text = converter.TraceFilePath;

            textBoxCoreDumpFile.Text = converter.CoreDumpFile;
            textBoxWinchesterInterruptDelay.Text = converter.WinchesterInterruptDelay.ToString();
            textBoxStatisticsFile.Text = converter.StatisticsFile;

            checkBoxE000RAM.Checked = converter.RAME000;
            checkBoxE800RAM.Checked = converter.RAME800;
            checkBoxF000RAM.Checked = converter.RAMF000;
            checkBoxE000ROM.Checked = converter.ROME000;
            checkBoxE800ROM.Checked = converter.ROME800;
            checkBoxF000ROM.Checked = converter.ROMF000;

            checkBoxSW1B.Checked = converter.SwitchSW1B;
            checkBoxSW1C.Checked = converter.SwitchSW1C;
            checkBoxSW1D.Checked = converter.SwitchSW1D;

            switch (converter.ProcessorBoard)
            {
                case (int)ConvertCfgToXml.CPUBoardTypes.MP_09:
                    if (converter.Processor == 1)
                    {
                        radioButton6809.Checked = true;
                        radioButtonMP_09.Checked = true;
                        _processorBoard = "MP_09";
                    }
                    else
                    {
                        radioButton6800.Checked = true;
                        radioButtonGeneric.Checked = true;
                        _processorBoard = "MP_2";
                    }
                    break;
                case (int)ConvertCfgToXml.CPUBoardTypes.MPU_1:
                    radioButtonMPU_1.Checked = true;
                    _processorBoard = "MPU_1";
                    break;
                case (int)ConvertCfgToXml.CPUBoardTypes.GENERIC:
                    radioButtonGeneric.Checked = true;
                    _processorBoard = "GENERIC";
                    break;
                case (int)ConvertCfgToXml.CPUBoardTypes.SSB:
                    _processorBoard = "SSB";
                    break;
            }
            converter.GetCfgFileBoardInfo();
            SetBoardInfoInList();

            for (int i = 0; i < converter.FloppyInformation.Count; i++)
            {
                ListViewItem iFloppy = listViewFloppy.Items.Add(i.ToString());
                iFloppy.SubItems.Add(converter.FloppyInformation[i].path);
                iFloppy.SubItems.Add(converter.FloppyInformation[i].format);
            }

            for (int i = 0; i < converter.PIAInformation.Count; i++)
            {
                ListViewItem iPIADisks = listViewPIADisks.Items.Add(i.ToString());
                iPIADisks.SubItems.Add(converter.PIAInformation[i].path);
                iPIADisks.SubItems.Add(converter.PIAInformation[i].format);
            }

            for (int i = 0; i < converter.TTLInformation.Count; i++)
            {
                ListViewItem iTTLDisks = listViewTTLDisks.Items.Add(i.ToString());
                iTTLDisks.SubItems.Add(converter.TTLInformation[i].path);
                iTTLDisks.SubItems.Add(converter.TTLInformation[i].format);
            }

            for (int i = 0; i < converter.WinchesterInformation.Count; i++)
            {
                ListViewItem iWinchester = listViewWinchester.Items.Add(i.ToString());
                iWinchester.SubItems.Add(converter.WinchesterInformation[i].winchesterDriveType);
                iWinchester.SubItems.Add(converter.WinchesterInformation[i].winchesterDrivePathName);
                iWinchester.SubItems.Add(converter.WinchesterInformation[i].nCylinders.ToString());
                iWinchester.SubItems.Add(converter.WinchesterInformation[i].nHeads.ToString());
                iWinchester.SubItems.Add(converter.WinchesterInformation[i].nSectorsPerTrack.ToString());
                iWinchester.SubItems.Add(converter.WinchesterInformation[i].nBytesPerSector.ToString());
            }

            listViewKeyboardMap.Items.Clear();

            string currentKey = "";

            ListViewItem iKeyMap = null;
            int currentSubItem = 0;
            int thisSubItem = 0;

            foreach (KeyValuePair<string, string> kvp in converter.KeyboardMap)
            {
                string key = "";

                if (kvp.Key.EndsWith("Normal"))
                {
                    key = kvp.Key.Replace("Normal", "");
                    thisSubItem = 0;
                }
                else if (kvp.Key.EndsWith("Shifted"))
                {
                    key = kvp.Key.Replace("Shifted", "");
                    thisSubItem = 1;
                }
                else if (kvp.Key.EndsWith("Control"))
                {
                    key = kvp.Key.Replace("Control", "");
                    thisSubItem = 2;
                }
                else if (kvp.Key.EndsWith("Both"))
                {
                    key = kvp.Key.Replace("Both", "");
                    thisSubItem = 4;
                }
                if (key != "")
                {
                    string keyState = kvp.Key.Replace(key, "");
                    if (currentKey != key)
                    {
                        iKeyMap = listViewKeyboardMap.Items.Add(kvp.Key);
                        iKeyMap.SubItems.Add(kvp.Value);
                        currentSubItem = 1;
                        currentKey = key;
                    }
                    else
                    {
                        if (thisSubItem == currentSubItem)
                        {
                            iKeyMap.SubItems.Add(kvp.Value);
                        }

                        if (thisSubItem == 2)
                        {
                            // then we need to fake an Alt entry

                            iKeyMap.SubItems.Add("");
                            currentSubItem++;
                        }
                        currentSubItem++;
                    }
                }
            }

            configFileName = configFileName + ".xml";
            this.Text = "memulator Configuration Editor - " + Path.GetFileName(configFileName);
        }

        private void LoadFromXmlFile()
        {
            _processorBoardCpu = GetConfigurationAttribute("Global/ProcessorBoard", "CPU", "6800");
            _configSection = "config" + _processorBoardCpu;

            _allowMultipleSector = GetConfigurationAttribute("Global/AllowMultipleSector", "value", "0") == "0" ? false : true;
            checkBoxAllowMultiSector.Checked = _allowMultipleSector;

            textBoxROMFile.Text = GetConfigurationAttribute(_configSection + "/romfile", "filename", "");

            _consoleDumpFile = GetConfigurationAttribute("Global/ConsoleDump", "filename", "consoledump.txt");
            _bTraceEnabled = GetConfigurationAttribute("Global/Trace", "Enabled", 0) == 0 ? false : true;
            _traceFilePath = GetConfigurationAttribute("Global/Trace", "Path", "");

            _b9600 = GetConfigurationAttribute("Global/ProcessorJumpers", "J_150_9600", 0) == 0 ? false : true;
            _b4800 = GetConfigurationAttribute("Global/ProcessorJumpers", "J_600_4800", 0) == 0 ? false : true;
            _bLowHigh = GetConfigurationAttribute("Global/ProcessorJumpers", "J_LOW_HIGH", 0) == 0 ? false : true;

            checkBoxE000RAM.Checked = GetConfigurationAttribute("Global/ProcessorJumpers", "E000_RAM", 0) == 0 ? false : true;
            checkBoxE800RAM.Checked = GetConfigurationAttribute("Global/ProcessorJumpers", "E800_RAM", 0) == 0 ? false : true;
            checkBoxF000RAM.Checked = GetConfigurationAttribute("Global/ProcessorJumpers", "F000_RAM", 0) == 0 ? false : true;
            checkBoxE000ROM.Checked = GetConfigurationAttribute("Global/ProcessorJumpers", "E000_ROM", 0) == 0 ? false : true;
            checkBoxE800ROM.Checked = GetConfigurationAttribute("Global/ProcessorJumpers", "E800_ROM", 0) == 0 ? false : true;
            checkBoxF000ROM.Checked = GetConfigurationAttribute("Global/ProcessorJumpers", "F000_ROM", 0) == 0 ? false : true;

            checkBoxSW1B.Checked = GetConfigurationAttribute("Global/ProcessorSwitch", "SW1B", 0) == 0 ? false : true;
            checkBoxSW1C.Checked = GetConfigurationAttribute("Global/ProcessorSwitch", "SW1C", 0) == 0 ? false : true;
            checkBoxSW1D.Checked = GetConfigurationAttribute("Global/ProcessorSwitch", "SW1D", 0) == 0 ? false : true;

            _processorBoard = GetConfigurationAttribute("Global/ProcessorBoard", "Board", "GENERIC");
            _bEnableScratchPad = GetConfigurationAttribute("Global/ProcessorBoard", "EnableScratchPadRAM", 0) == 0 ? false : true;

            textBoxWinchesterInterruptDelay.Text = GetConfigurationAttribute("Global/WinchesterInterruptDelay", "value", "0");
            textBoxStatisticsFile.Text = GetConfigurationAttribute("Global/Statistics", "filename", "");
            textBoxCoreDumpFile.Text = GetConfigurationAttribute("Global/CoreDump", "filename", "");

            // variables loaded - set up form controls

            textBoxConsoleDumpFile.Text = _consoleDumpFile;
            checkBoxEnableTrace.Checked = _bTraceEnabled;
            textBoxTraceFile.Text = _traceFilePath;

            checkBox150_9600.Checked = _b9600;
            checkBox600_4800.Checked = _b4800;
            checkBoxLowHigh.Checked = _bLowHigh;

            switch (_processorBoardCpu)
            {
                case "6800":
                    radioButton6800.Checked = true;
                    break;
                case "6809":
                    radioButton6809.Checked = true;
                    break;
            }

            switch (_processorBoard)
            {
                case "MP_09":
                    radioButtonMP_09.Checked = true;
                    break;
                case "MPU_1":
                    radioButtonMPU_1.Checked = true;
                    break;
                case "GENERIC":
                    radioButtonGeneric.Checked = true;
                    break;
                case "MP_2":
                    break;
            }

            // Fill in the Board Info Tab

            GetBoardInfo();
            SetBoardInfoInList();

            // Get any MPS Serial Port Data in the xml file
            //
            //    <SerialPorts>
            //          <Board ID="2" Port="1" BaudRate="9600"  StopBits="1" DataBits="8" Parity="None" InterruptEnabled="0"/>
            //          <Board ID="2" Port="2" BaudRate="19200" StopBits="1" DataBits="8" Parity="None" InterruptEnabled="0"/>
            //    </SerialPorts>

            #region mpsBoards

            mpsBoards.Clear();
            for (int boardNumber = 0; boardNumber < 8; boardNumber++)
            {
                if (_stBoardInfo[boardNumber] != null)
                {
                    if (_stBoardInfo[boardNumber].cDeviceType <= 17)
                    {
                        if (_deviceNames[_stBoardInfo[boardNumber].cDeviceType] == "MPS")
                        {
                            MPSBoard mps = new MPSBoard();
                            mps._boardNumber = boardNumber;

                            int numberOfPorts = Convert.ToInt16(_stBoardInfo[boardNumber].sNumberOfBytes / 2);

                            int boardID = boardNumber;
                            mps.aciaConfigurations.Clear();

                            for (int portNumber = 1; portNumber <= numberOfPorts; portNumber++)
                            {
                                string board_port = boardNumber.ToString() + "_" + portNumber.ToString();

                                string baudRate = GetConfigurationAttribute(_configSection + "/SerialPorts/Board_Port", "BaudRate", board_port, "9600");
                                string parity = GetConfigurationAttribute(_configSection + "/SerialPorts/Board_Port", "Parity", board_port, "0");
                                string stopBits = GetConfigurationAttribute(_configSection + "/SerialPorts/Board_Port", "StopBits", board_port, "1");
                                string dataBits = GetConfigurationAttribute(_configSection + "/SerialPorts/Board_Port", "DataBits", board_port, "8");
                                string interruptEnabled = GetConfigurationAttribute(_configSection + "/SerialPorts/Board_Port", "InterruptEnabled", board_port, "0");

                                if (baudRate.Length > 0 && parity.Length > 0 && stopBits.Length > 0 && dataBits.Length > 0 && interruptEnabled.Length > 0)
                                {
                                    ACIAConfig conf = new ACIAConfig();

                                    conf._baudRate = Convert.ToInt16(baudRate);
                                    conf._parity = Convert.ToInt16(parity);
                                    conf._stopBits = Convert.ToInt16(stopBits);
                                    conf._dataBits = Convert.ToInt16(dataBits);
                                    conf._interuptEnabled = Convert.ToInt16(interruptEnabled) == 0 ? false : true;

                                    mps.aciaConfigurations.Add(conf);
                                }
                            }

                            mpsBoards.Add(mps);
                        }
                    }
                }
                else
                {
                    break;
                }
            }
            #endregion

            // Fill in the Floppy Info Tab

            for (int i = 0; i < 4; i++)
            {
                string imagePath = GetConfigurationAttribute(_configSection + "/FloppyDisks/Disk", "Path", i.ToString(), "");
                string imageFormat = GetConfigurationAttribute(_configSection + "/FloppyDisks/Disk", "Format", i.ToString(), "");

                ListViewItem iFloppy = listViewFloppy.Items.Add(i.ToString());
                iFloppy.SubItems.Add(imagePath);
                iFloppy.SubItems.Add(imageFormat);
            }

            // Fill in the PIA Disks Tab

            for (int i = 0; i < 4; i++)
            {
                string imagePath = GetConfigurationAttribute(_configSection + "/PIAIDEDisks/Disk", "Path", i.ToString(), "");
                string imageFormat = GetConfigurationAttribute(_configSection + "/PIAIDEDisks/Disk", "Format", i.ToString(), "");

                ListViewItem iPIADisks = listViewPIADisks.Items.Add(i.ToString());
                iPIADisks.SubItems.Add(imagePath);
                iPIADisks.SubItems.Add(imageFormat);
            }

            // Fill in the TTL Disks Tab

            for (int i = 0; i < 4; i++)
            {
                string imagePath = GetConfigurationAttribute(_configSection + "/TTLIDEDisks/Disk", "Path", i.ToString(), "");
                string imageFormat = GetConfigurationAttribute(_configSection + "/TTLIDEDisks/Disk", "Format", i.ToString(), "");

                ListViewItem iTTLIDEDisks = listViewTTLDisks.Items.Add(i.ToString());
                iTTLIDEDisks.SubItems.Add(imagePath);
                iTTLIDEDisks.SubItems.Add(imageFormat);
            }

            // Fill in the Winchester Info Tab

            for (int nDrive = 0; nDrive < 4; nDrive++)
            {
                string winchesterDriveType = GetConfigurationAttribute(_configSection + "/WinchesterDrives/Disk", "TypeName", nDrive.ToString(), "");
                if (winchesterDriveType.Length > 0)
                {
                    string winchesterDrivePathName = GetConfigurationAttribute(_configSection + "/WinchesterDrives/Disk", "Path", nDrive.ToString(), "");
                    int nCylinders = GetConfigurationAttribute(_configSection + "/WinchesterDrives/Disk", "Cylinders", nDrive.ToString(), 0);
                    if (nCylinders > 0)
                    {
                        int nHeads = GetConfigurationAttribute(_configSection + "/WinchesterDrives/Disk", "Heads", nDrive.ToString(), 0);
                        int nSectorsPerTrack = GetConfigurationAttribute(_configSection + "/WinchesterDrives/Disk", "SectorsPerTrack", nDrive.ToString(), 0);
                        int nBytesPerSector = GetConfigurationAttribute(_configSection + "/WinchesterDrives/Disk", "BytesPerSector", nDrive.ToString(), 0);

                        ListViewItem iWinchester = listViewWinchester.Items.Add(nDrive.ToString());
                        iWinchester.SubItems.Add(winchesterDriveType);
                        iWinchester.SubItems.Add(winchesterDrivePathName);
                        iWinchester.SubItems.Add(nCylinders.ToString());
                        iWinchester.SubItems.Add(nHeads.ToString());
                        iWinchester.SubItems.Add(nSectorsPerTrack.ToString());
                        iWinchester.SubItems.Add(nBytesPerSector.ToString());
                    }
                }
            }

            // Fill in the CDS Info tab

            for (int nDrive = 0; nDrive < 4; nDrive++)
            {
                string CDSDriveType = GetConfigurationAttribute(_configSection + "/CDSDrives/Disk", "TypeName", nDrive.ToString(), "");
                if (CDSDriveType.Length > 0)
                {
                    string CDSDrivePathName = GetConfigurationAttribute(_configSection + "/CDSDrives/Disk", "Path", nDrive.ToString(), "");
                    int nCylinders = GetConfigurationAttribute(_configSection + "/CDSDrives/Disk", "Cylinders", nDrive.ToString(), 0);
                    if (nCylinders > 0)
                    {
                        int nHeads = GetConfigurationAttribute(_configSection + "/CDSDrives/Disk", "Heads", nDrive.ToString(), 0);
                        int nSectorsPerTrack = GetConfigurationAttribute(_configSection + "/CDSDrives/Disk", "SectorsPerTrack", nDrive.ToString(), 0);
                        int nBytesPerSector = GetConfigurationAttribute(_configSection + "/CDSDrives/Disk", "BytesPerSector", nDrive.ToString(), 0);

                        ListViewItem iCDS = listViewCDS.Items.Add(nDrive.ToString());
                        iCDS.SubItems.Add(CDSDriveType);
                        iCDS.SubItems.Add(CDSDrivePathName);
                        iCDS.SubItems.Add(nCylinders.ToString());
                        iCDS.SubItems.Add(nHeads.ToString());
                        iCDS.SubItems.Add(nSectorsPerTrack.ToString());
                        iCDS.SubItems.Add(nBytesPerSector.ToString());
                    }
                }
            }

            // Fill in the Keyboard Map

            using (Stream stream = File.OpenRead(configFileName))
            {
                XmlReader reader = XmlReader.Create(stream);
                if (reader != null)
                {
                    listViewKeyboardMap.Items.Clear();

                    XmlDocument doc = new XmlDocument();
                    if (doc != null)
                    {
                        doc.Load(reader);

                        XmlNode configurationNode = doc.SelectSingleNode("/configuration");
                        XmlNode node = configurationNode.SelectSingleNode(_configSection + "/KeyBoardMap");
                        if (node != null)
                        {
                            foreach (XmlNode childNode in node)
                            {
                                if (childNode != null)
                                {
                                    string value = "";
                                    XmlAttributeCollection coll = childNode.Attributes;
                                    if (coll != null)
                                    {
                                        ListViewItem iKeyMap = listViewKeyboardMap.Items.Add(childNode.Name);
                                        foreach (XmlAttribute valueNode in coll)
                                        {
                                            if (valueNode != null)
                                            {
                                                value = valueNode.Value;
                                                iKeyMap.SubItems.Add(value);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    reader.Close();
                }
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.InitialDirectory = dataDir;
            DialogResult dr = ofd.ShowDialog();

            if (dr == DialogResult.OK)
            {
                listViewBoards.Items.Clear();
                listViewFloppy.Items.Clear();
                listViewPIADisks.Items.Clear();
                listViewTTLDisks.Items.Clear();
                listViewWinchester.Items.Clear();
                listViewCDS.Items.Clear();

                configFileName = ofd.FileName;

                this.Text = "memulator Configuration Editor - " + Path.GetFileName(configFileName);

                if (Path.GetExtension(configFileName) != ".xml")
                {
                    LoadFromCfgFile();
                }
                else if (Path.GetExtension(configFileName) == ".xml")
                {
                    LoadFromXmlFile();
                }
            }
        }

        #endregion

        #region Extract data from XML Config file
        public static string GetConfigurationAttribute(string xpath, string attribute, string defaultvalue)
        {
            string value = defaultvalue;

            using (Stream stream = File.OpenRead(configFileName))
            {
                XmlReader reader = XmlReader.Create(stream);
                if (reader != null)
                {
                    XmlDocument doc = new XmlDocument();
                    if (doc != null)
                    {
                        doc.Load(reader);

                        XmlNode configurationNode = doc.SelectSingleNode("/configuration");
                        XmlNode node = configurationNode.SelectSingleNode(xpath);
                        if (node != null)
                        {
                            XmlAttributeCollection coll = node.Attributes;
                            if (coll != null)
                            {
                                XmlNode valueNode = coll.GetNamedItem(attribute);

                                if (valueNode != null)
                                    value = valueNode.Value;
                            }
                        }
                    }
                    reader.Close();
                }
            }
            return value;
        }
        public static int GetConfigurationAttribute(string xpath, string attribute, int defaultvalue)
        {
            int value = defaultvalue;

            using (Stream stream = File.OpenRead(configFileName))
            {
                XmlReader reader = XmlReader.Create(stream);
                if (reader != null)
                {
                    XmlDocument doc = new XmlDocument();
                    if (doc != null)
                    {
                        doc.Load(reader);

                        XmlNode configurationNode = doc.SelectSingleNode("/configuration");
                        XmlNode node = configurationNode.SelectSingleNode(xpath);
                        if (node != null)
                        {
                            XmlAttributeCollection coll = node.Attributes;
                            if (coll != null)
                            {
                                XmlNode valueNode = coll.GetNamedItem(attribute);
                                if (valueNode != null)
                                {
                                    string strvalue = valueNode.Value;
                                    Int32.TryParse(strvalue, out value);
                                }
                            }
                        }
                    }
                    reader.Close();
                }
            }
            return value;
        }
        public static string GetConfigurationAttribute(string xpath, string attribute, string ordinal, string defaultvalue)
        {
            string value = defaultvalue;
            bool foundOrdinal = false;

            using (Stream stream = File.OpenRead(configFileName))
            {
                XmlReader reader = XmlReader.Create(stream);
                if (reader != null)
                {
                    XmlDocument doc = new XmlDocument();
                    if (doc != null)
                    {
                        doc.Load(reader);

                        XmlNode configurationNode = doc.SelectSingleNode("/configuration");
                        XmlNode node = configurationNode.SelectSingleNode(xpath);
                        while (!foundOrdinal && node != null)
                        {
                            if (node != null)
                            {
                                XmlAttributeCollection coll = node.Attributes;
                                if (coll != null)
                                {
                                    foreach (XmlAttribute a in coll)
                                    {
                                        if (a.Name == "ID")
                                        {
                                            string index = a.Value;
                                            if (index == ordinal)
                                            {
                                                XmlNode valueNode = coll.GetNamedItem(attribute);

                                                if (valueNode != null)
                                                {
                                                    value = valueNode.Value;
                                                    foundOrdinal = true;
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                                if (!foundOrdinal)
                                    node = node.NextSibling;
                            }
                        }
                    }
                    reader.Close();
                }
            }

            return value;
        }        
        public static int GetConfigurationAttribute(string xpath, string attribute, string ordinal, int defaultvalue)
        {
            int value = defaultvalue;
            bool foundOrdinal = false;

            using (Stream stream = File.OpenRead(configFileName))
            {
                XmlReader reader = XmlReader.Create(stream);
                if (reader != null)
                {
                    XmlDocument doc = new XmlDocument();
                    if (doc != null)
                    {
                        doc.Load(reader);

                        XmlNode configurationNode = doc.SelectSingleNode("/configuration");
                        XmlNode node = configurationNode.SelectSingleNode(xpath);
                        while (!foundOrdinal && node != null)
                        {
                            XmlAttributeCollection coll = node.Attributes;
                            if (coll != null)
                            {
                                foreach (XmlAttribute a in coll)
                                {
                                    if (a.Name == "ID")
                                    {
                                        string index = a.Value;
                                        if (index == ordinal)
                                        {
                                            XmlNode valueNode = coll.GetNamedItem(attribute);

                                            if (valueNode != null)
                                            {
                                                string strvalue = valueNode.Value;
                                                Int32.TryParse(strvalue, out value);
                                                foundOrdinal = true;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                            if (!foundOrdinal)
                                node = node.NextSibling;
                        }
                    }
                    reader.Close();
                }
            }

            return value;
        }
        public static int GetConfigurationAttributeHex(string xpath, string attribute, string ordinal, int defaultValue)
        {
            int value = defaultValue;

            try
            {
                string strValue = GetConfigurationAttribute(xpath, attribute, ordinal, defaultValue.ToString("X4"));
                value = Convert.ToUInt16(strValue, 16);
            }
            catch
            {
            }

            return value;
        }
        public static byte GetBoardType(int nRow, byte defaultValue)
        {
            byte boardtype = defaultValue;

            string strBoardType = GetConfigurationAttribute(_configSection + "/BoardConfiguration/Board", "Type", nRow.ToString(), "");
            switch (strBoardType)
            {
                case "CONS": boardtype = 2; break;
                case "MPS": boardtype = 3; break;
                case "FD2": boardtype = 4; break;
                case "MPT": boardtype = 5; break;
                case "DMAF1": boardtype = 6; break;
                case "DMAF2": boardtype = 7; break;
                case "DMAF3": boardtype = 8; break;
                case "PRT": boardtype = 9; break;
                case "MPL": boardtype = 10; break;
                case "DAT": boardtype = 11; break;
                case "IOP": boardtype = 12; break;
                case "MPID": boardtype = 13; break;
                case "PIAIDE": boardtype = 14; break;
                case "TTLIDE": boardtype = 15; break;
                case "8274": boardtype = 16; break;
                case "PCSTREAM": boardtype = 17; break;
                default: boardtype = 0; break;
            }

            return boardtype;
        }
        private void GetBoardInfo()
        {
            int nRow;

            for (nRow = 0; nRow < 16; nRow++)
            {
                _stBoardInfo[nRow] = new BoardInfoClass();

                _stBoardInfo[nRow].cDeviceType = (byte)GetBoardType(nRow, 0);
                if (_stBoardInfo[nRow].cDeviceType != 0)
                {
                    _stBoardInfo[nRow].sBaseAddress = (ushort)GetConfigurationAttributeHex(_configSection + "/BoardConfiguration/Board", "Addr", nRow.ToString(), 0);

                    _stBoardInfo[nRow].sNumberOfBytes = (ushort)GetConfigurationAttribute(_configSection + "/BoardConfiguration/Board", "Size", nRow.ToString(), 0);
                    _stBoardInfo[nRow].strGuid = GetConfigurationAttribute(_configSection + "/BoardConfiguration/Board", "GUID", nRow.ToString(), "");
                    _stBoardInfo[nRow].bInterruptEnabled = GetConfigurationAttribute(_configSection + "/BoardConfiguration/Board", "IRQ", nRow.ToString(), 0) == 0 ? false : true;
                }
                else
                {
                    _stBoardInfo[nRow] = null;
                    break;
                }
            }
        }

        #endregion

        private void SetProcessorBoardCheckBoxes()
        {
            bool enableThem = false;

            if (radioButton6800.Checked)
            {
            }
            else
            {
                if (radioButton6809.Checked)
                {
                    if (radioButtonMP_09.Checked)
                    {
                        enableThem = true;
                    }
                }
                else
                {
                }
            }

            labelProcessorJumpers.Enabled = enableThem;
            checkBox150_9600.Enabled = enableThem;
            checkBox600_4800.Enabled = enableThem;
            checkBoxLowHigh.Enabled = enableThem;
            checkBoxE000RAM.Enabled = enableThem;
            checkBoxE000ROM.Enabled = enableThem;
            checkBoxE800RAM.Enabled = enableThem;
            checkBoxE800ROM.Enabled = enableThem;
            checkBoxF000RAM.Enabled = enableThem;
            checkBoxF000ROM.Enabled = enableThem;
            checkBoxSW1B.Enabled = enableThem;
            checkBoxSW1C.Enabled = enableThem;
            checkBoxSW1D.Enabled = enableThem;

            labelBaudRate.Enabled = enableThem;
            labelRAM.Enabled = enableThem;
            labelROM.Enabled = enableThem;

            labelWinchesterInterruptDelay.Enabled = enableThem;
            textBoxWinchesterInterruptDelay.Enabled = enableThem;

            textBoxTraceFile.Enabled = checkBoxEnableTrace.Checked;
            buttonBrowseTraceFile.Enabled = checkBoxEnableTrace.Checked;
        }

        #region Browse Button Handler Code
        private void buttonBrowseROMFile_Click(object sender, EventArgs e)
        {
            string path = "";
            if (textBoxROMFile.Text.Length > 0)
                path = Path.GetDirectoryName(textBoxROMFile.Text);

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.InitialDirectory = dataDir + path;
            ofd.FileName = Path.GetFileName(textBoxROMFile.Text);

            DialogResult dr = ofd.ShowDialog();
            if (dr == DialogResult.OK)
            {
                textBoxROMFile.Text = ofd.FileName.Replace(dataDir, "");
            }
        }

        private void buttonBrowseConsoleDumpFile_Click(object sender, EventArgs e)
        {
            string path = "";
            
            if (textBoxConsoleDumpFile.Text.Length > 0)
                path = Path.GetDirectoryName(textBoxConsoleDumpFile.Text);

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.InitialDirectory = dataDir + path;
            sfd.FileName = Path.GetFileName(textBoxConsoleDumpFile.Text);

            DialogResult dr = sfd.ShowDialog();
            if (dr == DialogResult.OK)
            {
                textBoxConsoleDumpFile.Text = sfd.FileName.Replace (dataDir, "");
            }
        }
        private void buttonBrowseStatisticsFile_Click(object sender, EventArgs e)
        {
            string path = "";
            
            if (textBoxStatisticsFile.Text.Length > 0)
                path = Path.GetDirectoryName(textBoxStatisticsFile.Text);

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.InitialDirectory = dataDir + path;
            sfd.FileName = Path.GetFileName(textBoxStatisticsFile.Text);

            DialogResult dr = sfd.ShowDialog();
            if (dr == DialogResult.OK)
            {
                textBoxStatisticsFile.Text = sfd.FileName.Replace(dataDir, "");
            }

        }
        private void buttonBrowseCoreDumpFile_Click(object sender, EventArgs e)
        {
            string path = "";
            
            if (textBoxCoreDumpFile.Text.Length > 0)
                path = Path.GetDirectoryName(textBoxCoreDumpFile.Text);

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.InitialDirectory = dataDir + path;
            sfd.FileName = Path.GetFileName(textBoxCoreDumpFile.Text);

            DialogResult dr = sfd.ShowDialog();
            if (dr == DialogResult.OK)
            {
                textBoxCoreDumpFile.Text = sfd.FileName.Replace(dataDir, "");
            }

        }
        private void buttonBrowseTraceFile_Click(object sender, EventArgs e)
        {
            string path = "";
            
            if (textBoxTraceFile.Text.Length > 0)
                path = Path.GetDirectoryName(textBoxTraceFile.Text);

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.InitialDirectory = dataDir + path;
            sfd.FileName = Path.GetFileName(textBoxTraceFile.Text);

            DialogResult dr = sfd.ShowDialog();
            if (dr == DialogResult.OK)
            {
                textBoxTraceFile.Text = sfd.FileName.Replace(dataDir, "");
            }
        }
        #endregion

        #region Radio Button Handler Code
        private void radioButton6809_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton6809.Checked)
            {
                labelProcessorBoard.Visible = radioButton6809.Checked;
                radioButtonMP_09.Visible = radioButton6809.Checked;
                radioButtonMPU_1.Visible = radioButton6809.Checked;
                radioButtonGeneric.Visible = radioButton6809.Checked;
                groupBoxProcessorBoard.Visible = radioButton6809.Checked;

                SetProcessorBoardCheckBoxes();
            }
        }
        private void radioButton6800_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton6800.Checked)
            {
                labelProcessorBoard.Visible = radioButton6809.Checked;
                radioButtonMP_09.Visible = radioButton6809.Checked;
                radioButtonMPU_1.Visible = radioButton6809.Checked;
                radioButtonGeneric.Visible = radioButton6809.Checked; 
                groupBoxProcessorBoard.Visible = radioButton6809.Checked;

                SetProcessorBoardCheckBoxes();
            }
        }
        private void radioButtonGeneric_CheckedChanged(object sender, EventArgs e)
        {
            SetProcessorBoardCheckBoxes();
        }
        private void radioButtonMP_09_CheckedChanged(object sender, EventArgs e)
        {
            SetProcessorBoardCheckBoxes();
        }
        private void radioButtonMPU_1_CheckedChanged(object sender, EventArgs e)
        {
            SetProcessorBoardCheckBoxes();
        }
        #endregion

        private void checkBoxEnableTrace_CheckedChanged(object sender, EventArgs e)
        {
            SetProcessorBoardCheckBoxes();
        }

        #region Board Selection Maintenance

        private void RetagBoards(string boardAddedName)
        {
            int slot = 0;

            foreach (ListViewItem item in listViewBoards.Items)
            {
                if (item.SubItems[0].Text == "MPS")     // see if we need to retag the mpsBoards
                {
                    foreach (MPSBoard mps in mpsBoards)
                    {
                        if (item.Tag != null)                                                       // no need to chack if this is the board we are adding
                        {
                            if ((mps._boardNumber == (int)item.Tag) && (mps._boardNumber != slot))  // if this one is about to change
                            {
                                mps._boardNumber = slot;                                            // assign the new slot number to this MPS board
                            }
                        }
                    }
                }

                if (item.Tag == null)   // if this is the board we just added - then it gets this slot
                {
                    if (item.SubItems[0].Text == "MPS")     // if this is an MPS board we are adding we need to assign the board number
                    {
                        MPSBoard b = new MPSBoard();
                        b._boardNumber = slot;

                        int numberOfPorts = Convert.ToInt16(item.SubItems[2].Text.Replace("0x", "")) / 2;
                        for (int i = 0; i < numberOfPorts; i++)
                        {
                            b.aciaConfigurations.Add(new ACIAConfig());
                        }
                        mpsBoards.Add(b);
                    }
                }
                item.Tag = slot++;
            }
        }

        private void buttonAddBoard_Click(object sender, EventArgs e)
        {
            frmBoardSelection bs = new frmBoardSelection();

            foreach (ListViewItem item in listViewBoards.Items)
            {
                bs.BoardsInstalled.Add(item.Text);
            }

            DialogResult dr = bs.ShowDialog(this);
            if (dr == DialogResult.OK)
            {
                ListViewItem iBoard = listViewBoards.Items.Add(bs.BoardName);
                iBoard.SubItems.Add("0x" + bs.BaseAddress.Replace("0x", ""));
                iBoard.SubItems.Add("0x" + bs.NumberOfBytes.Replace("0x", ""));
                iBoard.SubItems.Add(bs.GUID);
                iBoard.SubItems.Add((bs.InterruptEnabled == false) ? "No" : "Yes");

                RetagBoards(bs.BoardName);
            }
        }

        private void buttonEditBoard_Click(object sender, EventArgs e)
        {
            ListView.SelectedListViewItemCollection items = listViewBoards.SelectedItems;
            if (items.Count > 0)
            {
                ListViewItem item = items[0];

                int tag = (int)item.Tag;

                if (item.Text == "MPS")
                {
                    FrmAciaConfiguration bs = new FrmAciaConfiguration();
                    bs.BaseAddress = item.SubItems[1].Text.Replace("0x", "");
                    bs.NumberOfBytes = item.SubItems[2].Text.Replace("0x", "");

                    foreach (MPSBoard b in mpsBoards)
                    {
                        if (b._boardNumber == tag)
                        {
                            for (int i = 0; i < Convert.ToInt16(bs.NumberOfBytes) / 2; i++)
                            {
                                ACIAConfig conf = new ACIAConfig();

                                if (b.aciaConfigurations.Count > i)
                                {
                                    conf = b.aciaConfigurations[i];
                                }
                                bs.aciaConfigs.Add(conf);
                            }
                            break;
                        }
                    }

                    // Get the port setup for this board from xml file. Layout in xml is:
                    //
                    //<SerialPorts>
                    //  <Board ID="2" Port="1" BaudRate="9600"  StopBits="1" DataBits="8" Parity="None" InterruptEnabled="0"/>
                    //  <Board ID="2" Port="2" BaudRate="19200" StopBits="1" DataBits="8" Parity="None" InterruptEnabled="0"/>
                    //</SerialPorts>

                    DialogResult dr = bs.ShowDialog(this);
                    if (dr == DialogResult.OK)
                    {
                        // rip apart the aciaConfigs List in the dialog

                        // first we need to find the proper MPS board to update in the MPSBoards list

                        foreach (MPSBoard b in mpsBoards)
                        {
                            if (b._boardNumber == tag)
                            {
                                // Yea - we found it - first get rid of the old ones

                                b.aciaConfigurations.Clear();

                                // now add back in the ones from the dialog.

                                int numberOfPorts = Convert.ToInt16(bs.NumberOfBytes) / 2;
                                item.SubItems[2].Text = "0x" + (numberOfPorts * 2).ToString("00");

                                foreach (ACIAConfig c in bs.aciaConfigs)
                                {
                                    ACIAConfig conf = new ACIAConfig();
                                    conf = c;
                                    b.aciaConfigurations.Add(conf);

                                    if (--numberOfPorts == 0)
                                        break;
                                }
                            }
                        }
                    }
                }
                else
                {

                    frmBoardSelection bs = new frmBoardSelection();

                    bs.Text = "BoardSelection Edit";
                    bs.EditMode = true;
                    bs.BoardName = item.Text;
                    bs.BaseAddress = item.SubItems[1].Text.Replace("0x", "");
                    bs.NumberOfBytes = item.SubItems[2].Text.Replace("0x", "");
                    bs.InterruptEnabled = item.SubItems[4].Text == "Yes" ? true : false;

                    DialogResult dr = bs.ShowDialog(this);
                    if (dr == DialogResult.OK)
                    {
                        item.SubItems[1].Text = "0x" + bs.BaseAddress.Replace("0x", "");
                        item.SubItems[2].Text = "0x" + bs.NumberOfBytes.Replace("0x", "");
                        item.SubItems[4].Text = (bs.InterruptEnabled == false) ? "No" : "Yes";
                    }
                }
            }
        }

        private void listViewBoards_DoubleClick(object sender, EventArgs e)
        {
            buttonEditBoard_Click(sender, e);
        }

        private void buttonRemoveBoard_Click(object sender, EventArgs e)
        {
            ListView.SelectedListViewItemCollection items = listViewBoards.SelectedItems;
            if (items.Count > 0)
            {
                ListViewItem item = items[0];
                if (item.SubItems[0].Text == "MPS") // we are removing an MPS board so we have to get rid of the entry in mpsBoards
                {
                    for (int index = 0; index < mpsBoards.Count; index++)
                    {
                        MPSBoard mps = mpsBoards[index];
                        if (mps._boardNumber == (int)item.Tag)  // if this one is about to be removed
                        {
                            mpsBoards.RemoveAt(index);
                        }
                    }
                }
                item.Remove();
                RetagBoards(null);
            }
        }

        #endregion

        private void MoveDiskDown(ListView lv)
        {
            if (lv.SelectedIndices.Count == 1)
            {
                ListView.SelectedIndexCollection coll = lv.SelectedIndices;
                ListViewItem lvi1 = lv.Items[coll[0]];
                ListViewItem lvi2;
                if (lv.Items.Count > coll[0] + 1)
                {
                    lvi2 = lv.Items[coll[0] + 1];
                }
                else
                {
                    lvi2 = lv.Items.Add((coll[0] + 1).ToString());
                    lvi2.SubItems.Add("");
                    lvi2.SubItems.Add("");
                }

                // Now switch them

                string imageSave = lvi2.SubItems[1].Text;
                string formatSave = lvi2.SubItems[2].Text;

                lv.Items[coll[0] + 1].SubItems[1].Text = lvi1.SubItems[1].Text;
                lv.Items[coll[0] + 1].SubItems[2].Text = lvi1.SubItems[2].Text;

                lv.Items[coll[0]].SubItems[1].Text = imageSave;
                lv.Items[coll[0]].SubItems[2].Text = formatSave;

                lv.Items[coll[0] + 1].Selected = true;

            }
        }
        private void MoveDiskUp(ListView lv)
        {
            if (lv.SelectedIndices.Count == 1)
            {
                ListView.SelectedIndexCollection coll = lv.SelectedIndices;
                ListViewItem lvi1 = lv.Items[coll[0]];
                ListViewItem lvi2;

                lvi2 = lv.Items[coll[0] - 1];

                // Now switch them

                string imageSave = lvi2.SubItems[1].Text;
                string formatSave = lvi2.SubItems[2].Text;

                lv.Items[coll[0] - 1].SubItems[1].Text = lvi1.SubItems[1].Text;
                lv.Items[coll[0] - 1].SubItems[2].Text = lvi1.SubItems[2].Text;

                lv.Items[coll[0]].SubItems[1].Text = imageSave;
                lv.Items[coll[0]].SubItems[2].Text = formatSave;

                lv.Items[coll[0] - 1].Selected = true;
            }
        }
        private void RemoveDisk(ListView lv)
        {
            if (lv.SelectedIndices.Count == 1)
            {
                ListView.SelectedIndexCollection coll = lv.SelectedIndices;
                int indexToRemove = coll[0];

                lv.Items.RemoveAt(indexToRemove);
                for (int i = indexToRemove; i < lv.Items.Count; i++ )
                {
                    int driveNumber = Convert.ToInt16(lv.Items[i].Text);
                    lv.Items[i].SubItems[0].Text = (driveNumber - 1).ToString();
                }
            }

        }

        #region Floppy Selection Maintenance
        private void SetFloppyButtons()
        {
            if (listViewFloppy.Items.Count >= 4)
                buttonAddFloppy.Enabled = false;
            else
                buttonAddFloppy.Enabled = true;

            if (listViewFloppy.SelectedIndices.Count == 0)
            {
                // nothing selected so disable buttons that require a selection to be made

                buttonRemoveFloppy.Enabled = false;
                buttonEditFloppy.Enabled = false;
                buttonFloppyDown.Enabled = false;
                buttonFloppyUp.Enabled = false;
            }
            else
            {
                // if we get here - somthing is selected

                buttonEditFloppy.Enabled = true;
                buttonRemoveFloppy.Enabled = true;
                if (listViewFloppy.Items.Count > 1)
                {
                    if (listViewFloppy.SelectedIndices.Count == 1)
                    {
                        ListView.SelectedIndexCollection coll = listViewFloppy.SelectedIndices;

                        buttonFloppyDown.Enabled = true;
                        buttonFloppyUp.Enabled = true;
                        if (coll[0] == 0)
                            buttonFloppyUp.Enabled = false;
                        if (coll[0] == 3)
                            buttonFloppyDown.Enabled = false;
                    }
                }
            }
        }
        private void buttonAddFloppy_Click(object sender, EventArgs e)
        {
            frmDiskAddEdit frm = new frmDiskAddEdit();
            DialogResult r = frm.ShowDialog(this);
            if (r == DialogResult.OK)
            {
                ListViewItem iFloppy = listViewFloppy.Items.Add(listViewFloppy.Items.Count.ToString());
                iFloppy.SubItems.Add(frm.imagePath);
                iFloppy.SubItems.Add(frm.imageFormat);
                SetFloppyButtons();
            }
        }
        private void buttonEditFloppy_Click(object sender, EventArgs e)
        {
            ListView.SelectedListViewItemCollection items = listViewFloppy.SelectedItems;
            if (items.Count > 0)
            {
                ListViewItem item = items[0];
                frmDiskAddEdit frm = new frmDiskAddEdit();

                frm.imagePath = item.SubItems[1].Text;
                frm.imageFormat = item.SubItems[2].Text;

                DialogResult r = frm.ShowDialog(this);
                if (r == DialogResult.OK)
                {
                    item.SubItems[1].Text = frm.imagePath;
                    item.SubItems[2].Text = frm.imageFormat;
                }
            }
        }
        private void buttonRemoveFloppy_Click(object sender, EventArgs e)
        {
            RemoveDisk(listViewFloppy);
            SetFloppyButtons();
        }
        private void buttonFloppyUp_Click(object sender, EventArgs e)
        {
            MoveDiskUp(listViewFloppy);
        }
        private void buttonFloppyDown_Click(object sender, EventArgs e)
        {
            MoveDiskDown(listViewFloppy);
        }
        private void tabPageFloppy_Enter(object sender, EventArgs e)
        {
            SetFloppyButtons();
        }
        private void listViewFloppy_DoubleClick(object sender, EventArgs e)
        {
            buttonEditFloppy_Click(sender, e);
        }
        private void listViewFloppy_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetFloppyButtons();
        }
        #endregion

        #region PIADisk Selection Maintenance
        private void SetPIADiskButtons()
        {
            if (listViewPIADisks.Items.Count >= 4)
                buttonAddPIADisk.Enabled = false;
            else
                buttonAddPIADisk.Enabled = true;

            if (listViewPIADisks.SelectedIndices.Count == 0)
            {
                // nothing selected so disable buttons that require a selection to be made

                buttonRemovePIADisk.Enabled = false;
                buttonEditPIADisk.Enabled = false;
                buttonPIADiskDown.Enabled = false;
                buttonPIADiskUp.Enabled = false;
            }
            else
            {
                // if we get here - somthing is selected

                buttonEditPIADisk.Enabled = true;
                buttonRemovePIADisk.Enabled = true;
                if (listViewPIADisks.Items.Count > 1)
                {
                    buttonPIADiskDown.Enabled = true;
                    buttonPIADiskUp.Enabled = true;
                }
            }
        }
        private void buttonAddPIADisk_Click(object sender, EventArgs e)
        {
            frmDiskAddEdit frm = new frmDiskAddEdit();
            DialogResult r = frm.ShowDialog(this);
            if (r == DialogResult.OK)
            {
                ListViewItem iPIADisk = listViewPIADisks.Items.Add(listViewFloppy.Items.Count.ToString());
                iPIADisk.SubItems.Add(frm.imagePath);
                iPIADisk.SubItems.Add(frm.imageFormat);
                SetFloppyButtons();
            }
        }
        private void buttonEditPIADisk_Click(object sender, EventArgs e)
        {
            ListView.SelectedListViewItemCollection items = listViewPIADisks.SelectedItems;
            if (items.Count > 0)
            {
                ListViewItem item = items[0];
                frmDiskAddEdit frm = new frmDiskAddEdit();

                frm.imagePath = item.SubItems[1].Text;
                frm.imageFormat = item.SubItems[2].Text;

                DialogResult r = frm.ShowDialog(this);
                if (r == DialogResult.OK)
                {
                    item.SubItems[1].Text = frm.imagePath;
                    item.SubItems[2].Text = frm.imageFormat;
                }
            }
        }
        private void buttonRemovePIADisk_Click(object sender, EventArgs e)
        {
            RemoveDisk(listViewPIADisks);
            SetPIADiskButtons();
        }
        private void buttonPIADiskUP_Click(object sender, EventArgs e)
        {
            MoveDiskUp(listViewPIADisks);
        }
        private void buttonPIADiskDown_Click(object sender, EventArgs e)
        {
            MoveDiskDown(listViewPIADisks);
        }
        private void tabPagePIADisks_Enter(object sender, EventArgs e)
        {
            SetPIADiskButtons();
        }
        private void listViewPIADisks_DoubleClick(object sender, EventArgs e)
        {
            buttonEditPIADisk_Click(sender, e);
        }
        private void listViewPIADisks_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetPIADiskButtons();
        }
        #endregion

        #region TTLDisk Selection Maintenance
        private void SetTTLDiskButtons()
        {
            if (listViewTTLDisks.Items.Count >= 4)
                buttonAddTTLDisk.Enabled = false;
            else
                buttonAddTTLDisk.Enabled = true;

            if (listViewTTLDisks.SelectedIndices.Count == 0)
            {
                // nothing selected so disable buttons that require a selection to be made

                buttonRemoveTTLDisk.Enabled = false;
                buttonEditTTLDisk.Enabled = false;
                buttonTTLDiskDown.Enabled = false;
                buttonTTLDiskUp.Enabled = false;
            }
            else
            {
                // if we get here - somthing is selected

                buttonEditTTLDisk.Enabled = true;
                buttonRemoveTTLDisk.Enabled = true;
                if (listViewTTLDisks.Items.Count > 1)
                {
                    buttonTTLDiskDown.Enabled = true;
                    buttonTTLDiskUp.Enabled = true;
                }
            }
        }
        private void buttonAddTTLDisk_Click(object sender, EventArgs e)
        {
            frmDiskAddEdit frm = new frmDiskAddEdit();
            DialogResult r = frm.ShowDialog(this);
            if (r == DialogResult.OK)
            {
                ListViewItem iTTLDisk = listViewTTLDisks.Items.Add(listViewFloppy.Items.Count.ToString());
                iTTLDisk.SubItems.Add(frm.imagePath);
                iTTLDisk.SubItems.Add(frm.imageFormat);
                SetFloppyButtons();
            }
        }
        private void buttonEditTTLDisk_Click(object sender, EventArgs e)
        {
            ListView.SelectedListViewItemCollection items = listViewTTLDisks.SelectedItems;
            if (items.Count > 0)
            {
                ListViewItem item = items[0];
                frmDiskAddEdit frm = new frmDiskAddEdit();

                frm.imagePath = item.SubItems[1].Text;
                frm.imageFormat = item.SubItems[2].Text;

                DialogResult r = frm.ShowDialog(this);
                if (r == DialogResult.OK)
                {
                    item.SubItems[1].Text = frm.imagePath;
                    item.SubItems[2].Text = frm.imageFormat;
                }
            }
        }
        private void buttonRemoveTTLDisk_Click(object sender, EventArgs e)
        {
            RemoveDisk(listViewTTLDisks);
            SetPIADiskButtons();
        }
        private void buttonTTLDiskUp_Click(object sender, EventArgs e)
        {
            MoveDiskUp(listViewTTLDisks);
        }
        private void buttonTTLDiskDown_Click(object sender, EventArgs e)
        {
            MoveDiskDown(listViewTTLDisks);
        }
        private void tabPageTTLDisks_Enter(object sender, EventArgs e)
        {
            SetTTLDiskButtons();
        }
        private void listViewTTLDisks_DoubleClick(object sender, EventArgs e)
        {
            buttonEditTTLDisk_Click(sender, e);
        }
        private void listViewTTLDisks_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetTTLDiskButtons();
        }
        #endregion

        #region Winchester Disk Selection Maintenance

        private void SaveAddAndEditWinchesterParameters(int driveIndex)
        {
            if (driveIndex == -1)
            {
                // this is an add - open the xml file and add this drive to the Winchester Drives section
            }
            else
            {
                // this is an edit - open the xml file and change this drive to the Winchester Drives section
            }
        }

        private void buttonAddWinchester_Click(object sender, EventArgs e)
        {
            //MessageBox.Show("Not yet Implemented");

            frmDialogHardDriveParameters dlg = new frmDialogHardDriveParameters(dataDir);
            DialogResult r = dlg.ShowDialog();

            if (r == DialogResult.OK)
                SaveAddAndEditWinchesterParameters(-1);
        }

        private void buttonEditWinchester_Click(object sender, EventArgs e)
        {
            //MessageBox.Show("Not yet Implemented");

            // get the collection of selected items - really only interested in the first one.

            ListView.SelectedListViewItemCollection items = listViewWinchester.SelectedItems;

            // only proceed if there is a selected item

            if (items.Count > 0)
            {
                // get the first one

                ListViewItem item = items[0];
                frmDialogHardDriveParameters frm = new frmDialogHardDriveParameters(dataDir);

                // set up the variables in the Hard Drive Parameters Dialog Box.

                frm.name            = item.SubItems[1].Text;
                frm.imagePath       = item.SubItems[2].Text;
                frm.cylinders       = Convert.ToInt16(item.SubItems[3].Text);
                frm.heads           = Convert.ToInt16(item.SubItems[4].Text);
                frm.sectorsPerTrack = Convert.ToInt16(item.SubItems[5].Text);
                frm.bytesPerSector  = Convert.ToInt16(item.SubItems[6].Text);

                DialogResult r = frm.ShowDialog(this);
                if (r == DialogResult.OK)
                {
                    // user clicked the Save button - so save.

                    SaveAddAndEditWinchesterParameters(item.Index);
                }
            }
        }

        private void buttonRemoveWinchester_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Not yet Implemented");
        }

        private void buttonWinchesterUP_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Not yet Implemented");
        }

        private void buttonWinchesterDown_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Not yet Implemented");
        }
        #endregion

        #region CDS Disk Selection Maintenance
        private void buttonAddCDS_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Not yet Implemented");
        }

        private void buttonEditCDS_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Not yet Implemented");
        }

        private void buttonRemoveCDS_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Not yet Implemented");
        }

        private void buttonCDSUp_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Not yet Implemented");
        }

        private void buttonCDSDown_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Not yet Implemented");
        }
        #endregion

        private void listViewBoards_SelectedIndexChanged(object sender, EventArgs e)
        {
            MessageBox.Show("Not yet Implemented");
        }

        private void MemulatorConfigEditor_Load(object sender, EventArgs e)
        {

        }
    }
}
