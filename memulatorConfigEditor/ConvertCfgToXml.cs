using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.IO;
using System.Xml;

namespace ConfigEditor
{
    public class ConvertCfgToXml
    {
        class RegistryEntry
        {
            public bool validEntry = false;
            public string key;
            public string type;
            public string value;
        }

        public class DiskInfo
        {
            public string path = "";
            public string format = "";
        }

        public class WinchesterInfo
        {
            public string winchesterDriveType = "";
            public string winchesterDrivePathName = "";
            public int nCylinders = 0;
            public int nHeads = 0;
            public int nSectorsPerTrack = 0;
            public int nBytesPerSector = 0;
        }

        public enum ProcessorTypes
        {
            M6800 = 0,
            M6809 = 1
        }

        public enum CPUBoardTypes
        {
            MP_09 = 0,
            MPU_1 = 1,
            GENERIC = 2,
            SSB = 3
        }

        enum DiskFormats
        {
            DISK_FORMAT_UNKNOWN = 0,
            DISK_FORMAT_FLEX = 1,
            DISK_FORMAT_FLEX_IMA = 2,
            DISK_FORMAT_OS9 = 3,
            DISK_FORMAT_UNIFLEX = 4,
            DISK_FORMAT_MINIFLEX = 5

            //DISK_FORMAT_FLEX = 0,
            //DISK_FORMAT_FLEX_IMA = 1,
            //DISK_FORMAT_OS9 = 2,
            //DISK_FORMAT_OS9_IMA = 3,
            //DISK_FORMAT_UNIFLEX = 4,
            //DISK_FORMAT_MF_FDOS = 5
        }

        enum States
        {
            unknown = -1,
            regeditSignature,
            rootKey,
            BoardConfiguration6800,
            BoardConfiguration6809,
            SerialPort6800,
            SerialPort6809,
            SerialPort68008274,
            SerialPort68098274,
            AHODevice,
            AHODevice_AHO,
            BreakPoints,
            Capture,
            Directories,
            FloppyCreate,
            FloppyMaint,
            General,
            KeyboardMap,
            Options,
            Printer,
            RecentFileList,
            Settings,
            WinchesterDriveConfigurations,
            CMI5619,
            CMI5640,
            D514,
            D526,
            RMS506,
            RMS509,
            RMS512,
            RMS518,
            RO201,
            RO202,
            RO203,
            RO204,
            RO5090,
            ST412,
            ST506,
            TMS503
        }

        #region Properties

        private Dictionary<string, string> keyboardMap = new Dictionary<string, string>();
        public Dictionary<string, string> KeyboardMap
        {
            get { return keyboardMap; }
            set { keyboardMap = value; }
        }

        string _processorName = "";
        public string ProcessorName
        {
            get { return _processorName; }
            set { _processorName = value; }
        }

        private static int _processor = 0;
        public int Processor
        {
            get { return ConvertCfgToXml._processor; }
            set { ConvertCfgToXml._processor = value; }
        }

        private static bool _allowMultipleSector = false;
        public bool AllowMultipleSector
        {
            get { return ConvertCfgToXml._allowMultipleSector; }
            set { ConvertCfgToXml._allowMultipleSector = value; }
        }

        private static string _romfile = "";
        public string Romfile
        {
            get { return ConvertCfgToXml._romfile; }
            set { ConvertCfgToXml._romfile = value; }
        }

        //private string _consoleDumpFile         = "";

        private string _coreDumpFile = "";
        public string CoreDumpFile
        {
            get { return _coreDumpFile; }
            set { _coreDumpFile = value; }
        }

        private bool _bTraceEnabled = false;
        public bool TraceEnabled
        {
            get { return _bTraceEnabled; }
            set { _bTraceEnabled = value; }
        }

        private string _traceFilePath = "";          
        public string TraceFilePath
        {
            get { return _traceFilePath; }
            set { _traceFilePath = value; }
        }

        private bool _b9600 = false;
        public bool B9600
        {
            get { return _b9600; }
            set { _b9600 = value; }
        }

        private bool _b4800 = false;
        public bool B4800
        {
            get { return _b4800; }
            set { _b4800 = value; }
        }

        private bool _bLowHigh = false;
        public bool BLowHigh
        {
            get { return _bLowHigh; }
            set { _bLowHigh = value; }
        }

        private bool _RAME000 = false;                  
        public bool RAME000
        {
            get { return _RAME000; }
            set { _RAME000 = value; }
        }

        private bool _RAME800 = false;                  
        public bool RAME800
        {
            get { return _RAME800; }
            set { _RAME800 = value; }
        }

        private bool _RAMF000 = false;                  
        public bool RAMF000
        {
            get { return _RAMF000; }
            set { _RAMF000 = value; }
        }

        private bool _ROME000 = false;                  
        public bool ROME000
        {
            get { return _ROME000; }
            set { _ROME000 = value; }
        }

        private bool _ROME800 = false;                  
        public bool ROME800
        {
            get { return _ROME800; }
            set { _ROME800 = value; }
        }

        private bool _ROMF000 = false;                  
        public bool ROMF000
        {
            get { return _ROMF000; }
            set { _ROMF000 = value; }
        }

        private bool _switchSW1B = false;               
        public bool SwitchSW1B
        {
            get { return _switchSW1B; }
            set { _switchSW1B = value; }
        }

        private bool _switchSW1C = false;               
        public bool SwitchSW1C
        {
            get { return _switchSW1C; }
            set { _switchSW1C = value; }
        }

        private bool _switchSW1D = false;               
        public bool SwitchSW1D
        {
            get { return _switchSW1D; }
            set { _switchSW1D = value; }
        }

        private bool _bEnableScratchPad = false;        
        public bool EnableScratchPad
        {
            get { return _bEnableScratchPad; }
            set { _bEnableScratchPad = value; }
        }

        private int _processorBoard = 0;            
        public int ProcessorBoard
        {
            get { return _processorBoard; }
            set { _processorBoard = value; }
        }

        private int _nWinchesterInterruptDelay = 1;
        public int WinchesterInterruptDelay
        {
            get { return _nWinchesterInterruptDelay; }
            set { _nWinchesterInterruptDelay = value; }
        }

        private string _statisticsFile = "";
        public string StatisticsFile
        {
            get { return _statisticsFile; }
            set { _statisticsFile = value; }
        }

        // these are only used for the cfg to xml conversion routines to store processor specific stuuf
        //  ---------------------------------------------------------------------------------------------

        public static List<memulatorConfigEditor.BoardInfoClass[]> _stBoardInfo680x = new List<memulatorConfigEditor.BoardInfoClass[]>(2);

        //  ---------------------------------------------------------------------------------------------

        string regeditSignature = "REGEDIT4";

        private List<WinchesterInfo> _winchesterInfo = new List<WinchesterInfo>();
        public List<WinchesterInfo> WinchesterInformation
        {
            get { return _winchesterInfo; }
            set { _winchesterInfo = value; }
        }

        private List<DiskInfo> _floppyInfo = new List<DiskInfo>();
        public List<DiskInfo> FloppyInformation
        {
            get { return _floppyInfo; }
            set { _floppyInfo = value; }
        }

        private List<DiskInfo> _piaInfo = new List<DiskInfo>();
        public List<DiskInfo> PIAInformation
        {
            get { return _piaInfo; }
            set { _piaInfo = value; }
        }

        private List<DiskInfo> _ttlInfo = new List<DiskInfo>();
        public List<DiskInfo> TTLInformation
        {
            get { return _ttlInfo; }
            set { _ttlInfo = value; }
        }

        private List<string> completeFileContents = new List<string>();

        #endregion

        public ConvertCfgToXml()
        {
            _stBoardInfo680x.Add(new memulatorConfigEditor.BoardInfoClass[32]);    // add one for the 6800 board config values for the cfg to xml conversion
            _stBoardInfo680x.Add(new memulatorConfigEditor.BoardInfoClass[32]);    // add one for the 6809 board config values for the cfg to xml conversion
        }

        private RegistryEntry GetRegistryKeyTypeValue(string line)
        {
            RegistryEntry regEntry = new RegistryEntry();
            regEntry.key = "";
            regEntry.type = "";
            regEntry.value = "";

            int startofType = -1;

            string state = "gettingkey";
            regEntry.key = "";


            for (int i = 0; i < line.Length; i++)
            {
                switch (state)
                {
                    case "gettingkey":
                        if (i != 0 && line[i] != '"')
                            regEntry.key += line[i];
                        else if (line[i] == '"')
                        {
                            if (line[i + 1] == '=')
                            {
                                state = "gettingtype";
                                startofType = i + 2;
                                if (line[startofType] == '"')
                                {
                                    regEntry.type = "string";
                                    state = "gettingValue";
                                }
                            }
                        }
                        break;

                    case "gettingtype":
                        if (line[i] != '=')
                        {
                            if (line[i] != ':')
                            {
                                regEntry.type += line[i];
                            }
                            else
                            {
                                state = "gettingValue";
                            }
                        }
                        break;

                    case "gettingValue":
                        if (regEntry.type != "string")
                        {
                            regEntry.value += line[i];
                        }
                        else
                        {
                            if (line[i] != '=')
                            {
                                regEntry.value += line[i];
                            }
                        }
                        break;
                }
            }

            if (regEntry.type == "string")
            {
                regEntry.value = regEntry.value.TrimStart('"');
                regEntry.value = regEntry.value.TrimEnd('"');
            }

            return regEntry;
        }
        private RegistryEntry GetRegistryInfoByName(string subkey, string valuename, string defaultValue)
        {
            RegistryEntry regEntry = new RegistryEntry();
            regEntry.validEntry = false;
            regEntry.key = "";
            regEntry.type = "";
            regEntry.value = "";

            string fullkeyname = @"HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu";
            string keyToFind = string.Format(@"[{0}\{1}]", fullkeyname, subkey);

            bool keyFound = false;
            foreach (string line in completeFileContents)
            {
                if (!keyFound)
                {
                    if (line == keyToFind)
                    {
                        keyFound = true;
                    }
                }
                else
                {
                    regEntry = GetRegistryKeyTypeValue(line);
                    if (regEntry.key == valuename)
                    {
                        regEntry.validEntry = true;
                        break;
                    }
                }
            }

            if (!regEntry.validEntry)
            {
                regEntry.value = defaultValue;
            }

            return regEntry;
        }
        
        public void GetCfgFileBoardInfo()
        {
            for (int nRow = 0; nRow < 32; nRow++)
            {
                string typeString = string.Format("Board{0} Device Type", nRow.ToString("00"));
                string addrString = string.Format("Board{0} Device Addr", nRow.ToString("00"));
                string sizeString = string.Format("Board{0} Device Size", nRow.ToString("00"));
                string guidString = string.Format("Board{0} Device GUID", nRow.ToString("00"));
                string intrString = string.Format("Board{0} Device Intr", nRow.ToString("00"));

                _stBoardInfo680x[(int)_processor][nRow] = new memulatorConfigEditor.BoardInfoClass();      // only initialize it when we see the type

                string type = GetRegistryInfoByName(string.Format("{0} Board Configuration", _processorName), typeString, "0").value;
                if (Convert.ToUInt16(type, 16) == 0)
                {
                    _stBoardInfo680x[_processor][nRow] = null;
                }
                else
                {
                    string addr = GetRegistryInfoByName(string.Format("{0} Board Configuration", _processorName), addrString, "0").value;
                    string size = GetRegistryInfoByName(string.Format("{0} Board Configuration", _processorName), sizeString, "0").value;
                    string guid = GetRegistryInfoByName(string.Format("{0} Board Configuration", _processorName), guidString, "").value;
                    string intr = GetRegistryInfoByName(string.Format("{0} Board Configuration", _processorName), intrString, "0").value;

                    _stBoardInfo680x[_processor][nRow].cDeviceType = (byte)Convert.ToUInt16(type, 16);
                    _stBoardInfo680x[_processor][nRow].sBaseAddress = (ushort)Convert.ToUInt16(addr, 16);
                    _stBoardInfo680x[_processor][nRow].sNumberOfBytes = (ushort)Convert.ToUInt16(size, 16);
                    _stBoardInfo680x[_processor][nRow].strGuid = guid;
                    _stBoardInfo680x[_processor][nRow].bInterruptEnabled = Convert.ToInt16(intr) == 0 ? false : true;
                }
            }

            // now move the right ones into the editor

            for (int i = 0; i < _stBoardInfo680x[(int)_processor].Length; i++)
            {
                memulatorConfigEditor._stBoardInfo[i] = _stBoardInfo680x[(int)_processor][i];
            }
        }

        private string GetFileFormatDescription(int floppyFormat)
        {
            string formatName = "";

            switch (floppyFormat)
            {
                case (int)DiskFormats.DISK_FORMAT_FLEX:
                    formatName = "FLEX";
                    break;
                case (int)DiskFormats.DISK_FORMAT_OS9:
                    formatName = "OS9";
                    break;
                case (int)DiskFormats.DISK_FORMAT_UNIFLEX:
                    formatName = "UNIFLEX";
                    break;
                case (int)DiskFormats.DISK_FORMAT_MINIFLEX:
                    formatName = "MF_FDOS";
                    break;
            }

            return formatName;
        }

        public void ConvertFile(string configFileName)
        {
            if (Path.GetExtension(configFileName) == ".cfg")
            {
                DialogResult r = MessageBox.Show("Would you like to convert this old style configuration file to the new style?", "Convert to XML", MessageBoxButtons.YesNo);
                if (r == DialogResult.Yes)
                {
                    using (StreamReader stream = new StreamReader(File.OpenRead(configFileName)))
                    {
                        string line;
                        while ((line = stream.ReadLine()) != null)
                        {
                            completeFileContents.Add(line);
                        }
                    }

                    _processor = Convert.ToInt16(GetRegistryInfoByName("General", "Processor", "0").value);
                    _processorName = "6800";
                    if (_processor != 0)
                    {
                        _processorName = "6809";
                    }

                    memulatorConfigEditor.ConfigSection = string.Format("config{0}", Convert.ToInt16(GetRegistryInfoByName("General", "Processor", "6800").value) == 0 ? "6800" : "6809");
                    _allowMultipleSector = Convert.ToInt16(GetRegistryInfoByName("Options", "Allow Multiple Sector", "0").value) == 0 ? false : true;
                    _romfile = GetRegistryInfoByName("Options", "Monitor File", "").value.Replace(@"\\", @"\");

                    //_consoleDumpFile = GetRegistryInfoByName("Global/ConsoleDump", "filename", "consoledump.txt");

                    _bTraceEnabled = Convert.ToInt16(GetRegistryInfoByName("General", "Trace Enabled", "0").value) == 0 ? false : true;
                    _traceFilePath = GetRegistryInfoByName("Options", "Trace File", "").value.Replace(@"\\", @"\");

                    _b9600 = Convert.ToInt16(GetRegistryInfoByName("Options", "150_9600", "0").value) == 0 ? false : true;
                    _b4800 = Convert.ToInt16(GetRegistryInfoByName("Options", "600_4800", "0").value) == 0 ? false : true;
                    _bLowHigh = Convert.ToInt16(GetRegistryInfoByName("Options", "LOW_HIGH", "0").value) == 0 ? false : true;

                    _coreDumpFile = GetRegistryInfoByName("Options", "Coredump File", "").value.Replace(@"\\", @"\");

                    _RAME000 = Convert.ToInt16(GetRegistryInfoByName("Options", "E000_RAM", "0").value) == 0 ? false : true;
                    _RAME800 = Convert.ToInt16(GetRegistryInfoByName("Options", "E800_RAM", "0").value) == 0 ? false : true;
                    _RAMF000 = Convert.ToInt16(GetRegistryInfoByName("Options", "F000_RAM", "0").value) == 0 ? false : true;
                    _ROME000 = Convert.ToInt16(GetRegistryInfoByName("Options", "E000_ROM", "0").value) == 0 ? false : true;
                    _ROME800 = Convert.ToInt16(GetRegistryInfoByName("Options", "E800_ROM", "0").value) == 0 ? false : true;
                    _ROMF000 = Convert.ToInt16(GetRegistryInfoByName("Options", "F000_ROM", "0").value) == 0 ? false : true;

                    _switchSW1B = Convert.ToInt16(GetRegistryInfoByName("Options", "SW 1 B", "0").value) == 0 ? false : true;
                    _switchSW1C = Convert.ToInt16(GetRegistryInfoByName("Options", "SW 1 C", "0").value) == 0 ? false : true;
                    _switchSW1D = Convert.ToInt16(GetRegistryInfoByName("Options", "SW 1 D", "0").value) == 0 ? false : true;

                    _processorBoard = Convert.ToInt16(GetRegistryInfoByName("General", "Processor Board", "0").value);
                    _bEnableScratchPad = Convert.ToInt16(GetRegistryInfoByName("General", "EnableScratchPad", "0").value) == 0 ? false : true;

                    _nWinchesterInterruptDelay = Convert.ToInt16(GetRegistryInfoByName("Global", "Winchester Interrupt Delay", "0").value);
                    _statisticsFile = GetRegistryInfoByName("Options", "Statistics File", "").value.Replace(@"\\", @"\");

                    for (int i = 0; i < 4; i++)
                    {
                        DiskInfo fi = new DiskInfo();
                        fi.format = "";

                        fi.path = GetRegistryInfoByName("General", string.Format("Drive {0} PathName", i), "").value.Replace(@"\\", @"\");
                        int floppyFormat = Convert.ToInt16(GetRegistryInfoByName("General", string.Format("Disk Format {0}", i), "0").value);

                        if (fi.path != "")
                        {
                            fi.format = GetFileFormatDescription(floppyFormat);
                            _floppyInfo.Add(fi);
                        }
                    }

                    for (int i = 0; i < 2; i++)
                    {
                        DiskInfo fi = new DiskInfo();
                        fi.path = GetRegistryInfoByName("General", string.Format("PIA IDE HardDrive Drive {0} PathName", i), "").value.Replace(@"\\", @"\");
                        if (fi.path != "")
                        {
                            fi.format = "FLEX";
                            _piaInfo.Add(fi);
                        }
                    }

                    for (int i = 0; i < 2; i++)
                    {
                        DiskInfo fi = new DiskInfo();
                        fi.path = GetRegistryInfoByName("General", string.Format("TTL IDE HardDrive Drive {0} PathName", i), "").value.Replace(@"\\", @"\");
                        if (fi.path != "")
                        {
                            fi.format = "FLEX";
                            _ttlInfo.Add(fi);
                        }
                    }

                    for (int nDrive = 0; nDrive < 4; nDrive++)
                    {
                        WinchesterInfo winchesterInfo = new WinchesterInfo();

                        winchesterInfo.winchesterDriveType = GetRegistryInfoByName("General", string.Format("Winchester Drive {0} TypeName", nDrive), "").value;
                        if (winchesterInfo.winchesterDriveType.Length > 0)
                        {
                            winchesterInfo.winchesterDrivePathName = GetRegistryInfoByName("General", string.Format("Winchester Drive {0} PathName", nDrive), "").value.Replace(@"\\", @"\");
                            winchesterInfo.nCylinders = Convert.ToUInt16(GetRegistryInfoByName(string.Format(@"Winchester Drive Configurations\{0}", winchesterInfo.winchesterDriveType), "Cylinders", "0").value, 16);
                            if (winchesterInfo.nCylinders > 0)
                            {
                                winchesterInfo.nHeads = Convert.ToUInt16(GetRegistryInfoByName(string.Format(@"Winchester Drive Configurations\{0}", winchesterInfo.winchesterDriveType), "Heads", "0").value, 16);
                                winchesterInfo.nSectorsPerTrack = Convert.ToUInt16(GetRegistryInfoByName(string.Format(@"Winchester Drive Configurations\{0}", winchesterInfo.winchesterDriveType), "Sectors Per Track", "0").value, 16);
                                winchesterInfo.nBytesPerSector = Convert.ToUInt16(GetRegistryInfoByName(string.Format(@"Winchester Drive Configurations\{0}", winchesterInfo.winchesterDriveType), "Bytes Per Sector", "0").value, 16);

                                _winchesterInfo.Add(winchesterInfo);
                            }
                        }
                    }

                    KeyboardMapping kbm = new KeyboardMapping();
                    foreach (KeyValuePair<string, string> kvp in kbm.keyboardMap)
                    {
                        string keyvalue = GetRegistryInfoByName("KeyboardMap", kvp.Key, kvp.Value).value;
                        keyboardMap.Add(kvp.Key, keyvalue);
                    }
                }
                else
                    MessageBox.Show("cfg files must be converted to xml configuration files before they can be used");
            }
            else
                MessageBox.Show("Currently we only support xml configuration files");
        }
    }
}