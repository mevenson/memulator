﻿using System;

using System.Collections.Generic;
using System.Text;

using System.Xml;
using System.IO;
using System.Threading;

using System.Reflection;

using System.Runtime.InteropServices;

using System.Runtime.Remoting.Channels.Ipc;
using System.Security.Permissions;

// TODO:
//
//      1)	Add a S1 folder in the options
//          Will do.
//
//      2)	Add a Load S1 in the ON / OFF section
//          Will do.
//
//      3)	Add an "Ignore CRC" while loading S1 files (and report CRC errors). Useful when one byte needs to be changed for testing althoug you can do it with MIKBUG.
//          The S1 loader currently ignores the checksum at the end of the line
//
//      4)	Fix Single Step potential bug: under TSC, click Single Step, type something on the keyboard, switch to Normal: program hanged and no control anymore.
//          After clicking the single step button, you have give focus back to the input window by clicking inside of it before you type on the keyboard.
//
//      5)	Fix bug which prevents from pasting text code under BASIC to simulate a program being typed. Crashes after first line number. Strange...
//          I will investigate this and correct.
//

namespace IPCRemoteObject
{
    // Remote object.
    public class RemoteObject : MarshalByRefObject
    {
        private int callCount = 0;

        public int GetCount()
        {
            Console.WriteLine("GetCount has been called.");
            callCount++;
            return (callCount);
        }
    }
}

namespace Memulator
{
    enum ROMLoaderStates
    {
        GETSTX      = 0,
        GETADDR     = 1,
        GETLENGTH   = 2,
        GETDATA     = 3,
        GETXFER     = 4
    }
    enum ROMFileFormat
    {
        INV_FORMAT = -1,
        STX_FORMAT = 0,
        S37_FORMAT = 1,
        S19_FORMAT = 2
    }
    enum Devices
    {
        DEVICE_INVALID_ADDR = -1,

        DEVICE_RAM      =  0,   // address is RAM
        DEVICE_ROM      =  1,   // address is ROM

        // All IO devices must be specified as > 1

        DEVICE_CONS     =  2,   // address belongs to console
        DEVICE_MPS      =  3,   // address belongs to a non console MP-S
        DEVICE_FDC      =  4,   // address belongs to floppy controller
        DEVICE_MPT      =  5,   // address belongs to Timer card
        DEVICE_DMAF1    =  6,   // address belongs to DMAF1
        DEVICE_DMAF2    =  7,   // address belongs to DMAF2
        DEVICE_DMAF3    =  8,   // address belongs to DMAF3
        DEVICE_PRT      =  9,   // address belongs to Printer
        DEVICE_MPL      = 10,   // address belongs to non printer MP-L
        DEVICE_DAT      = 11,   // address belongs to DAT outside ROM space
        DEVICE_IOP      = 12,
        DEVICE_MPID     = 13,   // address belongs to MP-ID Timer card
        DEVICE_PIAIDE   = 14,   // address belongs to a PIA that is hooked up to an IDE drive
        DEVICE_TTLIDE   = 15,   // address belongs to a PIA that is hooked up to an IDE drive
        DEVICE_8274     = 16,   // address belongs to a PIA that is hooked up to an IDE drive
        DEVICE_PCSTREAM = 17,   // An interface to talk to a PC Stream.

        DEVICE_AHO = 0x80   // Use Component Object Model to access hardware
    }
    enum CPUBoardTypes
    {
        MP_09   = 0,
        MPU_1   = 1,
        GENERIC = 2,
        SSB     = 3
    }
    enum ROMFileFormats
    {
        INV_FORMAT  = -1,
        STX_FORMAT  =  0,
        S37_FORMAT  =  1,
        S19_FORMAT  =  2
    }
    //enum DiskFormats
    //{
    //    DISK_FORMAT_UNKNOWN  = 0,
    //    DISK_FORMAT_FLEX     = 1,
    //    DISK_FORMAT_FLEX_IMA = 2,
    //    DISK_FORMAT_OS9      = 3,
    //    DISK_FORMAT_OS9_IMA  = 4,
    //    DISK_FORMAT_UNIFLEX  = 5,
    //    DISK_FORMAT_MF_FDOS  = 6
    //}
    enum DiskFormats
    {
        DISK_FORMAT_UNKNOWN = 0,
        DISK_FORMAT_FLEX = 1,
        DISK_FORMAT_FLEX_IMA = 2,
        DISK_FORMAT_OS9 = 3,
        DISK_FORMAT_UNIFLEX = 4,
        DISK_FORMAT_MINIFLEX = 5
    }

    enum DriveCounts
    {
        NUMBER_OF_PIAIDE_DRIVES = 2,
        NUMBER_OF_TTLIDE_DRIVES = 2
    }

    class Program
    {
        [SecurityPermission(SecurityAction.Demand)]
        public static void ServerMain(string[] args)
        {
            // Create the server channel.
            IpcChannel serverChannel = new IpcChannel("localhost:9090");

            // Register the server channel.
            System.Runtime.Remoting.Channels.ChannelServices.RegisterChannel(serverChannel, false);

            // Show the name of the channel.
            Console.WriteLine("The name of the channel is {0}.", serverChannel.ChannelName);

            // Show the priority of the channel.
            Console.WriteLine("The priority of the channel is {0}.", serverChannel.ChannelPriority);

            // Show the URIs associated with the channel.
            System.Runtime.Remoting.Channels.ChannelDataStore channelData = (System.Runtime.Remoting.Channels.ChannelDataStore) serverChannel.ChannelData;
            foreach (string uri in channelData.ChannelUris)
            {
                Console.WriteLine("The channel URI is {0}.", uri);
            }

            // Expose an object for remote calls.
            System.Runtime.Remoting.RemotingConfiguration.RegisterWellKnownServiceType(typeof(IPCRemoteObject.RemoteObject), "RemoteObject.rem", System.Runtime.Remoting.WellKnownObjectMode.Singleton);

            // Parse the channel's URI.
            string[] urls = serverChannel.GetUrlsForUri("RemoteObject.rem");
            if (urls.Length > 0)
            {
                string objectUrl = urls[0];
                string objectUri;
                string channelUri = serverChannel.Parse(objectUrl, out objectUri);

                Console.WriteLine("The object URI is {0}.", objectUri);
                Console.WriteLine("The channel URI is {0}.", channelUri);
                Console.WriteLine("The object URL is {0}.", objectUrl);
            }

            // Wait for the user prompt.

            Console.WriteLine("Press ENTER to exit the server.");
            Console.ReadLine();
            Console.WriteLine("The server is exiting.");
        }

        #region variables

        public static String [] m_strPIAIDEFilename = new string[(int)DriveCounts.NUMBER_OF_PIAIDE_DRIVES];
        public static String[] m_strTTLIDEFilename = new string[(int)DriveCounts.NUMBER_OF_TTLIDE_DRIVES];

        public static FileStream[] m_fpPIAIDE = new FileStream[(int)DriveCounts.NUMBER_OF_PIAIDE_DRIVES];
        public static FileStream[] m_fpTTLIDE = new FileStream[(int)DriveCounts.NUMBER_OF_TTLIDE_DRIVES];

        public static DiskFormats[] m_nPIAIDEDiskFormat = new DiskFormats[(int)DriveCounts.NUMBER_OF_PIAIDE_DRIVES];
        public static DiskFormats[] m_nTTLIDEDiskFormat = new DiskFormats[(int)DriveCounts.NUMBER_OF_TTLIDE_DRIVES];

        public static String[] m_strPIAIDEDriveInfo = new string[(int)DriveCounts.NUMBER_OF_PIAIDE_DRIVES];
        public static String[] m_strTTLIDEDriveInfo = new string[(int)DriveCounts.NUMBER_OF_TTLIDE_DRIVES];

        private static string _consoleDumpFile = "";
        public static string ConsoleDumpFile
        {
            get { return Program._consoleDumpFile; }
            set { Program._consoleDumpFile = value; }
        }

        private static string _traceFilePath = "";
        public static string TraceFilePath
        {
            get { return Program._traceFilePath; }
            set { Program._traceFilePath = value; }
        }

        private static Thread _cpuThread;
        public static Thread CpuThread
        {
            get { return Program._cpuThread; }
            set { Program._cpuThread = value; }
        }

        public static int m_nProcessorBoard;
        private static bool _bTraceEnabled;

        public static bool TraceEnabled
        {
            get { return Program._bTraceEnabled; }
            set { Program._bTraceEnabled = value; }
        }

        private static bool _bDMAF2AccessLogging;
        public static bool DMAF2AccessLogging
        {
            get { return _bDMAF2AccessLogging; }
            set { _bDMAF2AccessLogging = value; }
        }

        private static bool _bDMAF3AccessLogging;
        public static bool DMAF3AccessLogging
        {
            get{ return _bDMAF3AccessLogging; }
            set{ _bDMAF3AccessLogging = value; }
        }

        public static Cpu _cpu;
        public static console _theConsole;

        public static string configFileName = "configuration.xml";
        public static string dataDir = ".\\";
        //public static string commonAppDir = ".\\";
        //public static string userAppDir = ".\\";

        private static bool m_nEnableScratchPad;
        private static int _nTotalBoardsInstalled = 0;
        //private static int _nEnableScratchPad     = 0;

        private static BoardInfoClass[] _stBoardInfo = new BoardInfoClass[32];

        internal static BoardInfoClass[] BoardInfo
        {
            get { return Program._stBoardInfo; }
            set { Program._stBoardInfo = value; }
        }

        private static bool _b9600 = false;

        public static bool Switch9600
        {
            get { return Program._b9600; }
            set { Program._b9600 = value; }
        }
        private static bool _b4800 = false;

        public static bool Switch4800
        {
            get { return Program._b4800; }
            set { Program._b4800 = value; }
        }
        private static bool _bLowHigh = false;

        public static bool SwitchLowHigh
        {
            get { return Program._bLowHigh; }
            set { Program._bLowHigh = value; }
        }

        public static ConsoleACIA _CConsole;
        public static CPrinter _CPrinter;
        public static FD_2 _CFD_2;
        public static DMAF2 _CDMAF2;
        public static DMAF3 _CDMAF3;
        public static ACIA _CACIA;
        public static MPT _CMPT;
        public static MPID _CMPID;
        public static PIAIDE _CPIAIDE;
        public static TTLIDE _CTTLIDE;
        public static PCStream _CPCStream;

        private static bool bAllowMultiSectorTransfers = false;

        private static bool[] m_nWriteProtected = new bool[4];
        private static bool[] m_nDriveOpen = new bool[4];

        public static bool[] WriteProtected
        {
            get { return Program.m_nWriteProtected; }
            set { Program.m_nWriteProtected = value; }
        }

        public static bool[] DriveOpen
        {
            get { return Program.m_nDriveOpen; }
            set { Program.m_nDriveOpen = value; }
        }

        public static bool AllowMultiSectorTransfers
        {
            get { return bAllowMultiSectorTransfers; }
            set { bAllowMultiSectorTransfers = value; }
        }

        private static string[] driveImagePaths = new string[4];

        public static string[] DriveImagePaths
        {
            get { return Program.driveImagePaths; }
            set { Program.driveImagePaths = value; }
        }
        private static string[] driveImageFormats = new string[4];

        public static string[] DriveImageFormats
        {
            get { return Program.driveImageFormats; }
            set { Program.driveImageFormats = value; }
        }

        private static FileStream[] _floppyDriveStream = new FileStream[4];
        private static VirtualFloppyManipulationRoutines[] _virtualFloppyManipulationRoutines = new VirtualFloppyManipulationRoutines[4];
        private static DiskFormats[] m_nDiskFormat = new DiskFormats[4];

        private static byte[] m_nSectorsPerTrack = new byte[4];
        private static byte[] m_nNumberOfCylinders = new byte[4];
        private static byte[] m_nFormatByte = new byte[4];
        private static int[] m_nSectorsOnTrackZero = new int[4];
        private static int[] m_nSectorsOnTrackZeroSideZero = new int[4];
        private static int[] m_nSectorsOnTrackZeroSideOne = new int[4];
        private static int[] m_nNumberOfTracks = new int[4];
        private static int[] m_nIsDoubleSided = new int[4];

        public static int[] IsDoubleSided
        {
            get { return Program.m_nIsDoubleSided; }
            set { Program.m_nIsDoubleSided = value; }
        }
        public static int[] NumberOfTracks
        {
            get { return Program.m_nNumberOfTracks; }
            set { Program.m_nNumberOfTracks = value; }
        }
        public static FileStream[] FloppyDriveStream
        {
            get { return Program._floppyDriveStream; }
            set { Program._floppyDriveStream = value; }
        }

        public static VirtualFloppyManipulationRoutines[] VirtualFloppyManipulationRoutines 
        { 
            get => _virtualFloppyManipulationRoutines; 
            set => _virtualFloppyManipulationRoutines = value; 
        }

        public static int[] SectorsOnTrackZero { get => m_nSectorsOnTrackZero; set => m_nSectorsOnTrackZero = value; }

        public static int[] SectorsOnTrackZeroSideZero
        {
            get { return Program.m_nSectorsOnTrackZeroSideZero; }
            set { Program.m_nSectorsOnTrackZeroSideZero = value; }
        }
        public static int[] SectorsOnTrackZeroSideOne
        {
            get { return Program.m_nSectorsOnTrackZeroSideOne; }
            set { Program.m_nSectorsOnTrackZeroSideOne = value; }
        }

        public static byte[] FormatByte
        {
            get { return Program.m_nFormatByte; }
            set { Program.m_nFormatByte = value; }
        }
        public static byte[] SectorsPerTrack
        {
            get { return Program.m_nSectorsPerTrack; }
            set { Program.m_nSectorsPerTrack = value; }
        }
        public static DiskFormats[] DiskFormat
        {
            get { return Program.m_nDiskFormat; }
            set { Program.m_nDiskFormat = value; }
        }

        public static bool enableDMAF2ActivityLogChecked = false;
        public static bool enableFD2ActivityLogChecked = false;

        public static string activityLogFileDMAF2 = "";
        public static string activityLogFileDMAF3 = "";
        public static string activityLogFileFD2 = "";

        #endregion

        private static string _configSection = "";
        public static string ConfigSection
        {
            get { return Program._configSection; }
            set { Program._configSection = value; }
        }

        private static void ShowAverageSpeed() 
        {
            long lAverage = 0L;
            long lCount   = 0L;

            while (!_cpu.Running) ; // wait for cpu to start

            while (_cpu.Running)
            {
                if (!_cpu.InWait && !_cpu.InSync)
                {
                    lAverage += _cpu.CyclesThisPeriod;
                    _cpu.CyclesThisPeriod = 0;

                    Int64 average = lAverage / ++lCount;

                    Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
                    Console.Title = "memulator " + a.GetName().Version.ToString(4) + "        Emulation Speed: ~" + (average / 2).ToString("#,#") + " HZ " + (Program.TraceEnabled ? "Trace Enabled" : "") + (Program.DMAF3AccessLogging ? "DMAF3 TE" : "");
                    if (lCount > 5)
                    {
                        lAverage = lAverage / lCount;
                        lCount = 1L;
                    }
                }

                Thread.Sleep(2000);
            }
        }

        static bool LoadROM (Memory memory)
        {
            bool success = false;
            string romFilename = dataDir + GetConfigurationAttribute(ConfigSection+"/romfile", "filename", "");

            romFilename = romFilename.Replace("\\", "/");
            ROMLoaderStates nState = ROMLoaderStates.GETSTX;

            int nAddr       = 0;
            int nXferAddr   = 0;
            int nSize       = 0;

            byte [] buffer;
            byte [] buff = new byte[1024];                                            

            if (romFilename != dataDir)
            {
				try
				{
                    using (BinaryReader br = new BinaryReader(File.Open(romFilename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                    {
                        ROMFileFormat nFormat = ROMFileFormat.INV_FORMAT;

                        // see what type of file this is

                        buffer = br.ReadBytes(1);
                        if (buffer[0] == 0x02)
                        {
                            nFormat = ROMFileFormat.STX_FORMAT;
                            nState = ROMLoaderStates.GETADDR;
                        }
                        else if (buffer[0] == 'S')
                        {
                            buffer = br.ReadBytes(1);

                            if (buffer[0] == '1')
                                nFormat = ROMFileFormat.S19_FORMAT;
                            else if (buffer[0] == '3')
                                nFormat = ROMFileFormat.S37_FORMAT;

                            nState = ROMLoaderStates.GETLENGTH;
                        }

                        bool eof = false;
                        switch (nFormat)
                        {
                            case ROMFileFormat.STX_FORMAT:
                                eof = false;
                                while (!eof)
                                {
                                    switch (nState)
                                    {
                                        case ROMLoaderStates.GETSTX:
                                            buffer = br.ReadBytes(1);
                                            if (buffer.Length > 0)
                                            {
                                                switch (buffer[0])
                                                {
                                                    case 0x02:
                                                        nState = ROMLoaderStates.GETADDR;
                                                        break;
                                                    case 0x16:
                                                        nState = ROMLoaderStates.GETXFER;
                                                        break;
                                                }
                                            }
                                            else
                                            {
                                                success = true;
                                                eof = true;
                                            }
                                            break;

                                        case ROMLoaderStates.GETXFER:
                                            buffer = br.ReadBytes(2);
                                            nXferAddr = (buffer[0] * 256) + buffer[1];
                                            nState = ROMLoaderStates.GETSTX;
                                            break;

                                        case ROMLoaderStates.GETADDR:
                                            buffer = br.ReadBytes(2);
                                            nAddr = (buffer[0] * 256) + buffer[1];
                                            nState = ROMLoaderStates.GETLENGTH;
                                            break;

                                        case ROMLoaderStates.GETLENGTH:
                                            buffer = br.ReadBytes(1);
                                            nSize = buffer[0];
                                            nState = ROMLoaderStates.GETDATA;
                                            break;

                                        case ROMLoaderStates.GETDATA:
                                            buffer = br.ReadBytes(nSize);
                                            if (buffer.Length == nSize)
                                            {
                                                for (int i = 0; i < nSize; i++)
                                                    memory.MemorySpace[nAddr + i] = buffer[i];

                                                if (m_nEnableScratchPad)
                                                {
                                                    if (nAddr < 0xA000 || nAddr > 0xA3FF)
                                                    {
                                                        for (int i = 0; i < nSize; i++)
                                                        {
                                                            memory.DeviceMap[nAddr + i] = (byte)Devices.DEVICE_ROM;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    for (int i = 0; i < nSize; i++)
                                                    {
                                                        memory.DeviceMap[nAddr + i] = (byte)Devices.DEVICE_ROM;
                                                    }
                                                }

                                                nState = ROMLoaderStates.GETSTX;
                                            }
                                            else
                                            {
                                                success = true;
                                                eof = true;
                                            }
                                            break;
                                    }
                                }
                                break;

                            case ROMFileFormat.S19_FORMAT:
                                eof = false;
                                while (!eof)
                                {
                                    switch (nState)
                                    {
                                        case ROMLoaderStates.GETSTX:
                                            buffer = br.ReadBytes(2);
                                            if (buffer.Length == 2)
                                            {
                                                if (ASCIIEncoding.ASCII.GetString(buffer) == "S1")
                                                    nState = ROMLoaderStates.GETLENGTH;
                                                else if (ASCIIEncoding.ASCII.GetString(buffer) == "S9")
                                                    nState = ROMLoaderStates.GETXFER;
                                                else if (ASCIIEncoding.ASCII.GetString(buffer) == "SX")
                                                {
                                                    for (int i = 0; i < 1024 && !eof; i++)
                                                    {
                                                        byte[] b = br.ReadBytes(1);
                                                        if (b[0] == 0x0a || b[0] == 0x0d)
                                                        {
                                                            if (b[0] == 0x0d && br.PeekChar() == 0x0a)
                                                                b = br.ReadBytes(1);
                                                            break;
                                                        }
                                                        else
                                                        {
                                                            buff[i] = b[0];
                                                        }
                                                    }
                                                    buffer = buff;
                                                    //memset ((byte*) buffer, '\0', sizeof (buffer));
                                                    //fgets ((char*) buffer, sizeof (buffer), fp);
                                                }
                                            }
                                            else
                                            {
                                                success = true;
                                                eof = true;
                                            }
                                            break;

                                        case ROMLoaderStates.GETLENGTH:
                                            buffer = br.ReadBytes(2);
                                            nSize = Convert.ToInt32(ASCIIEncoding.ASCII.GetString(buffer), 16); // sscanf ((const char*) buffer, "%2X", &nSize);
                                            nState = ROMLoaderStates.GETADDR;
                                            break;

                                        case ROMLoaderStates.GETADDR:
                                            buffer = br.ReadBytes(4);
                                            nAddr = Convert.ToInt32(ASCIIEncoding.ASCII.GetString(buffer), 16);
                                            nState = ROMLoaderStates.GETDATA;
                                            break;

                                        case ROMLoaderStates.GETDATA:
                                            {
                                                int nBtesRead = 0;
                                                for (int i = 0; i < 1024 && !eof; i++)
                                                {
                                                    byte[] b = br.ReadBytes(1);
                                                    if (b[0] == 0x0a || b[0] == 0x0d)
                                                    {
                                                        if (b[0] == 0x0d && br.PeekChar() == 0x0a)
                                                            b = br.ReadBytes(1);
                                                        break;
                                                    }
                                                    else
                                                    {
                                                        buff[i] = b[0];
                                                        nBtesRead++;
                                                    }
                                                }
                                                buffer = buff;

                                                if (nBtesRead == (nSize - 2) * 2)               // subtract out the checksum byte
                                                {
                                                    for (uint i = 0; i < (nSize - 3); i++)      // do not store the checksum byte
                                                    {
                                                        byte[] thebyte = new byte[2];
                                                        thebyte.Initialize();                   // memset ((byte*) thebyte, '\0', sizeof (thebyte));

                                                        // memcpy ((byte*) thebyte, &buffer[i * 2], 2);

                                                        thebyte[0] = buffer[i * 2];
                                                        thebyte[1] = buffer[i * 2 + 1];

                                                        if (nAddr + i == 0xF07F)
                                                        {
                                                        }
                                                        memory.MemorySpace[nAddr + i] = Convert.ToByte(Encoding.ASCII.GetString(thebyte), 16);  //sscanf ((const char*) thebyte, "%2X", &m_Memory[nAddr + i]);
                                                        memory.DeviceMap[nAddr + i] = (byte)Devices.DEVICE_ROM;
                                                    }
                                                    nState = ROMLoaderStates.GETSTX;
                                                }
                                            }
                                            break;

                                        case ROMLoaderStates.GETXFER:
                                            //memset ((byte*) buffer, '\0', sizeof (buffer));
                                            buffer = br.ReadBytes(2);        // fread ((byte*) buffer, sizeof( char ), 2, fp);
                                            nSize = Convert.ToInt32(ASCIIEncoding.ASCII.GetString(buffer), 16); // sscanf ((const char*) buffer, "%2X", &nSize);
                                            buffer = br.ReadBytes(nSize * 2);
                                            if (buffer.Length == nSize * 2)
                                                nState = ROMLoaderStates.GETSTX;
                                            else
                                            {
                                                success = false;
                                                eof = true;
                                            }
                                            break;
                                    }
                                }
                                break;

                                #region S37 ROM FIle Format
                                //case ROMFileFormat.S37_FORMAT:
                                //    eof = false;
                                //    while (!eof)
                                //    {
                                //        switch (nState)
                                //        {
                                //            case  ROMLoaderStates.GETSTX:
                                //                memset ((byte*) buffer, '\0', sizeof (buffer));
                                //                fread (buffer, sizeof( char ), 2, fp);
                                //                if (strncmp ((const char*) buffer, "S3", 2) == 0)
                                //                    nState = ROMLoaderStates.GETLENGTH;
                                //                else if (strncmp ((const char*) buffer, "S7", 2) == 0)
                                //                    nState = ROMLoaderStates.GETXFER;
                                //                else if (strncmp ((const char*) buffer, "SX", 2) == 0)
                                //                {
                                //                    memset ((byte*) buffer, '\0', sizeof (buffer));
                                //                    fgets ((char*) buffer, sizeof (buffer), fp);
                                //                }
                                //                break;

                                //            case ROMLoaderStates.GETLENGTH:
                                //                memset ((byte*) buffer, '\0', sizeof (buffer));
                                //                fread ((byte*) buffer, sizeof( char ), 2, fp);
                                //                sscanf ((const char*) buffer, "%2X", &nSize);
                                //                nState = ROMLoaderStates.GETADDR;
                                //                break;

                                //            case ROMLoaderStates.GETADDR:
                                //                memset ((byte*) buffer, '\0', sizeof (buffer));
                                //                fread ((byte*) buffer, sizeof( char ), 8, fp);
                                //                sscanf ((const char*) buffer, "%8X", &nAddr);
                                //                nState = ROMLoaderStates.GETDATA;
                                //                break;

                                //            case ROMLoaderStates.GETDATA:
                                //                memset ((byte*) buffer, '\0', sizeof (buffer));
                                //                fgets ((char*) buffer, sizeof (buffer), fp);
                                //                for (uint i = 0; i < (nSize - 5); i ++)
                                //                {
                                //                    char byte[3];
                                //                    memset ((byte*) byte, '\0', sizeof (byte));
                                //                    memcpy ((byte*) byte, &buffer[i * 2], 2);
                                //                    sscanf ((const char*) byte, "%2X", &m_Memory[nAddr + i]);
                                //                    m_DeviceMap[nAddr + i] = DEVICE_ROM;
                                //                }
                                //                nState = ROMLoaderStates.GETSTX;
                                //                break;

                                //            case ROMLoaderStates.GETXFER:
                                //                memset ((byte*) buffer, '\0', sizeof (buffer));
                                //                fread ((byte*) buffer, sizeof( char ), 2, fp);
                                //                nState = ROMLoaderStates.GETSTX;
                                //                break;
                                //        }
                                //    }
                                //    break;
                                #endregion
                        }
                    }
				}
				catch (Exception e)
				{
					Console.WriteLine(e.Message);
                }
            }
            return success;
        }

        private static void InitializeHardware(Memory memory)
        {            
            _nTotalBoardsInstalled = 0;

            int nRow, i;
            int nMPS = 0;

            // First do the Processor Board stuff

            _b9600    = GetConfigurationAttribute("Global/ProcessorJumpers", "J_150_9600", 0) == 0 ? false : true;
            _b4800    = GetConfigurationAttribute("Global/ProcessorJumpers", "J_600_4800", 0) == 0 ? false : true;
            _bLowHigh = GetConfigurationAttribute("Global/ProcessorJumpers", "J_LOW_HIGH", 0) == 0 ? false : true;

            string processorBoard = GetConfigurationAttribute("Global/ProcessorBoard", "Board",               "GENERIC");
            m_nEnableScratchPad   = GetConfigurationAttribute("Global/ProcessorBoard", "EnableScratchPadRAM", 0) == 0 ? false : true;

            switch (processorBoard)
            {
                case "MP_09":
                    m_nProcessorBoard = 0;
                    break;
                case "MPU_1":
                    m_nProcessorBoard = 1;
                    break;
                case "GENERIC":
                    m_nProcessorBoard = 2;
                    break;
                case "SSB":
                    m_nProcessorBoard = 3;
                    break;
            }

            for (nRow = 0; nRow < 16; nRow++)
            {
                _stBoardInfo[nRow] = new BoardInfoClass();

                // set default to 10 sector on track 0 side 0 for FD2 driver in OS9

                _stBoardInfo[nRow].nBoardId = nRow;
                _stBoardInfo[nRow].nSectorsOnTrack0Side0ForOS9 = 10;        // default to 10 sector on trac 0 side 0 for OS9 driver
                _stBoardInfo[nRow].sBoardTypeName = GetConfigurationAttribute(ConfigSection + "/BoardConfiguration/Board", "Type", nRow.ToString(), "");

                _stBoardInfo[nRow].cDeviceType = (byte)GetBoardType(nRow, 0);
                if (_stBoardInfo[nRow].cDeviceType != 0)
                {
                    _stBoardInfo[nRow].sBaseAddress      = (ushort)GetConfigurationAttributeHex(ConfigSection + "/BoardConfiguration/Board", "Addr", nRow.ToString(), 0);
                    _stBoardInfo[nRow].sNumberOfBytes    = (ushort)GetConfigurationAttributeHex(ConfigSection + "/BoardConfiguration/Board", "Size", nRow.ToString(), 0);

                    _stBoardInfo[nRow].strGuid           =         GetConfigurationAttribute(ConfigSection + "/BoardConfiguration/Board", "GUID", nRow.ToString(), "");
                    _stBoardInfo[nRow].bInterruptEnabled =         GetConfigurationAttribute(ConfigSection + "/BoardConfiguration/Board", "IRQ",  nRow.ToString(), 0) == 0 ? false : true;

                    // set up the device map for this board

                    for (i = 0; i < _stBoardInfo[nRow].sNumberOfBytes; i++)
                        memory.DeviceMap[_stBoardInfo[nRow].sBaseAddress + i] = _stBoardInfo[nRow].cDeviceType;

                    switch (_stBoardInfo[nRow].cDeviceType)
                    {
                        case (int)Devices.DEVICE_RAM:
                        case (int)Devices.DEVICE_ROM:
                            break;

                        case (int)Devices.DEVICE_CONS:
                            _CConsole = new ConsoleACIA();
                            if (_CConsole != null)
                            {
                                _CConsole._bInterruptEnabled = false;
                                _CConsole.Init(0, memory.MemorySpace, _stBoardInfo[nRow].sBaseAddress, nRow, _stBoardInfo[nRow].bInterruptEnabled);

                                // creating a Console takes some extra steps because of it's interaction
                                // with the View.

                                //m_pView.m_CConsole = m_CConsole;
                            }
                            _nTotalBoardsInstalled++;
                            break;

                        case (int)Devices.DEVICE_MPS:

                            // Only create one instance of the class. The class itself can manage
                            // up to 16 ports

                            if (_CACIA == null)
                                _CACIA = new ACIA();

                            if (_CACIA != null)
                            {
                                for (int ports = 0; ports < _stBoardInfo[nRow].sNumberOfBytes / 2; ports++)
                                {
                                    if (nMPS < 16)
                                    {
                                        _CACIA.Init(nMPS++, memory.MemorySpace, _stBoardInfo[nRow].sBaseAddress, nRow, _stBoardInfo[nRow].bInterruptEnabled);
                                    }
                                }
                            }
                            _nTotalBoardsInstalled++;
                            break;

                        case (int)Devices.DEVICE_PCSTREAM:
                            _CPCStream = new PCStream();
                            if (_CPCStream != null)
                            {
                                _CPCStream.Init(0, memory.MemorySpace, _stBoardInfo[nRow].sBaseAddress, nRow, _stBoardInfo[nRow].bInterruptEnabled);
                            }
                            _nTotalBoardsInstalled++;
                            break;

                        case (int)Devices.DEVICE_FDC:
                            _CFD_2 = new FD_2();
                            if (_CFD_2 != null)
                            {
                                _stBoardInfo[nRow].nSectorsOnTrack0Side0ForOS9 = GetConfigurationAttribute(ConfigSection + "/BoardConfiguration/Board", "SectorsOnTrack0Side0ForOS9", nRow.ToString(), 10);
                                _CFD_2.Init(0, memory.MemorySpace, _stBoardInfo[nRow].sBaseAddress, nRow, _stBoardInfo[nRow].bInterruptEnabled);
                            }
                            _nTotalBoardsInstalled++;
                            break;

                        case (int)Devices.DEVICE_MPT:
                            _CMPT = new MPT();
                            if (_CMPT != null)
                            {
                                _CMPT.Init(0, memory.MemorySpace, _stBoardInfo[nRow].sBaseAddress, nRow, _stBoardInfo[nRow].bInterruptEnabled);
                            }
                            _nTotalBoardsInstalled++;
                            break;

                        case (int)Devices.DEVICE_MPID:
                            _CMPID = new MPID();
                            if (_CMPID != null)
                            {
                                _CMPID.Init(0, memory.MemorySpace, _stBoardInfo[nRow].sBaseAddress, nRow, _stBoardInfo[nRow].bInterruptEnabled);
                            }
                            _nTotalBoardsInstalled++;
                            break;

                        case (int)Devices.DEVICE_DMAF1:
                        case (int)Devices.DEVICE_DMAF2:
                            _CDMAF2 = new DMAF2();
                            if (_CDMAF2 != null)
                            {
                                _CDMAF2.Init(0, memory.MemorySpace, _stBoardInfo[nRow].sBaseAddress, nRow, _stBoardInfo[nRow].bInterruptEnabled);
                            }
                            _nTotalBoardsInstalled++;
                            break;

                        case (int)Devices.DEVICE_DMAF3:
                            _CDMAF3 = new DMAF3();
                            if (_CDMAF3 != null)
                            {
                                _CDMAF3.Init(0, memory.MemorySpace, _stBoardInfo[nRow].sBaseAddress, nRow, _stBoardInfo[nRow].bInterruptEnabled);
                            }
                            _nTotalBoardsInstalled++;
                            break;

                        case (int)Devices.DEVICE_PRT:
                            _CPrinter = new CPrinter();
                            if (_CPrinter != null)
                            {
                                _CPrinter.Init(0, memory.MemorySpace, _stBoardInfo[nRow].sBaseAddress, nRow, _stBoardInfo[nRow].bInterruptEnabled);
                            }
                            _nTotalBoardsInstalled++;
                            break;

                        case (int)Devices.DEVICE_MPL:
                        case (int)Devices.DEVICE_DAT:
                        case (int)Devices.DEVICE_IOP:
                            _nTotalBoardsInstalled++;
                            break;

                        case (int)Devices.DEVICE_PIAIDE:
                            _CPIAIDE = new PIAIDE();
                            if (_CPIAIDE != null)
                            {
                                _CPIAIDE.Init(0, memory.MemorySpace, _stBoardInfo[nRow].sBaseAddress, nRow, _stBoardInfo[nRow].bInterruptEnabled);
                            }
                            _nTotalBoardsInstalled++;
                            break;

                        case (int)Devices.DEVICE_TTLIDE:
                            _CTTLIDE = new TTLIDE();
                            if (_CTTLIDE != null)
                            {
                                _CTTLIDE.Init(0, memory.MemorySpace, _stBoardInfo[nRow].sBaseAddress, nRow, _stBoardInfo[nRow].bInterruptEnabled);
                            }
                            _nTotalBoardsInstalled++;
                            break;

                        default:
                            //if ((st6800BoardInfo[nRow].cDeviceType & DEVICE_AHO) == DEVICE_AHO)
                            //{
                            //    AHOInit (nRow, st6800BoardInfo[nRow].bInterruptEnabled);

                            //    for (i = 0; i < st6800BoardInfo[nRow].sNumberOfBytes; i++)
                            //        m_DeviceMap[st6800BoardInfo[nRow].sBaseAddress + i] = st6800BoardInfo[nRow].cDeviceType;

                            //}
                            _nTotalBoardsInstalled++;
                            break;
                    }
                }
                else
                {
                    _stBoardInfo[nRow] = null;
                    break;
                }
            }
        }

        // used by VirtualFloppyManipulationRoutines GetFileFormat to update
        // the Program.DiskFormat for a given drive

        public static void SetFileFormat(fileformat ff, byte drive)
        {
            switch (ff)
            {
                case fileformat.fileformat_FLEX:
                    m_nDiskFormat[drive] = DiskFormats.DISK_FORMAT_FLEX;
                    break;
                case fileformat.fileformat_FLEX_IMA:
                    m_nDiskFormat[drive] = DiskFormats.DISK_FORMAT_FLEX_IMA;
                    break;
                case fileformat.fileformat_OS9:
                    m_nDiskFormat[drive] = DiskFormats.DISK_FORMAT_OS9;
                    break;
                case fileformat.fileformat_UniFLEX:
                    m_nDiskFormat[drive] = DiskFormats.DISK_FORMAT_UNIFLEX;
                    break;
                case fileformat.fileformat_miniFLEX:
                    m_nDiskFormat[drive] = DiskFormats.DISK_FORMAT_MINIFLEX;
                    break;
                case fileformat.fileformat_UNKNOWN:
                    m_nDiskFormat[drive] = DiskFormats.DISK_FORMAT_UNKNOWN;
                    break;
            }
        }

        public static void LoadDrives(bool initialLoad = false)
        {
            long fileLength;

            bool[] drivePathChanged   = new bool[4];
            bool[] driveFormatChanged = new bool[4];

            for (int i = 0; i < 4; i++)
            {
                drivePathChanged[i] = false;
                driveFormatChanged[i] = false;

                string imagePath = "";
                bool fileFound = false;

                if ((GetConfigurationAttribute(ConfigSection + "/FloppyDisks/Disk", "Path", i.ToString(), "").StartsWith(@"\\")) || (GetConfigurationAttribute(ConfigSection + "/FloppyDisks/Disk", "Path", i.ToString(), "").Contains(@":")))
                    imagePath = GetConfigurationAttribute(ConfigSection + "/FloppyDisks/Disk", "Path", i.ToString(), "");
                else
                    imagePath = dataDir + GetConfigurationAttribute(ConfigSection + "/FloppyDisks/Disk", "Path", i.ToString(), "");

                string imageFormat = GetConfigurationAttribute(ConfigSection + "/FloppyDisks/Disk", "Format", i.ToString(), "");
                string imageFilename = Path.GetFileName(imagePath);

                bool fileExists = false;

                switch (i)
                {
                    case 0:
                        if (imageFilename.Length > 0)
                        {
                            try
                            {
                                //_mainForm.Drive0Image = global::SWTPCemuApp.Properties.Resources.drive0_closed;
                                //if ((File.GetAttributes(imagePath) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                                //    _mainForm.Drive0StatusImage = global::SWTPCemuApp.Properties.Resources.reddot;
                                //else
                                //    _mainForm.Drive0StatusImage = global::SWTPCemuApp.Properties.Resources.greendot;
                                //_mainForm.Drive0ActivityImage = global::SWTPCemuApp.Properties.Resources.greydot;
                                //_mainForm.Drive0Tag = imagePath;
                                //_mainForm.Drive0Name = Path.GetFileNameWithoutExtension(imagePath);
                                fileFound = true;
                            }
                            catch
                            {
                                //_mainForm.Drive0Image = global::SWTPCemuApp.Properties.Resources.drive0_open;
                                //_mainForm.Drive0Name = "";
                            }
                        }
                        else
                        {
                            //_mainForm.Drive0Image = global::SWTPCemuApp.Properties.Resources.drive0_open;
                            //_mainForm.Drive0Name = "";
                        }
                        break;
                    case 1:
                        if (imageFilename.Length > 0)
                        {
                            try
                            {
                                //_mainForm.Drive1Image = global::SWTPCemuApp.Properties.Resources.drive1_closed;
                                //if ((File.GetAttributes(imagePath) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                                //    _mainForm.Drive1StatusImage = global::SWTPCemuApp.Properties.Resources.reddot;
                                //else
                                //    _mainForm.Drive1StatusImage = global::SWTPCemuApp.Properties.Resources.greendot;
                                //_mainForm.Drive1ActivityImage = global::SWTPCemuApp.Properties.Resources.greydot;
                                //_mainForm.Drive1Tag = imagePath;
                                //_mainForm.Drive1Name = Path.GetFileNameWithoutExtension(imagePath);
                                fileFound = true;
                            }
                            catch
                            {
                                //_mainForm.Drive1Image = global::SWTPCemuApp.Properties.Resources.drive1_open;
                                //_mainForm.Drive1Name = "";
                            }
                        }
                        else
                        {
                            //_mainForm.Drive1Image = global::SWTPCemuApp.Properties.Resources.drive1_open;
                            //_mainForm.Drive1Name = "";
                        }
                        break;
                    case 2:
                        if (imageFilename.Length > 0)
                        {
                            try
                            {
                                //_mainForm.Drive2Image = global::SWTPCemuApp.Properties.Resources.drive2_closed;
                                //if ((File.GetAttributes(imagePath) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                                //    _mainForm.Drive2StatusImage = global::SWTPCemuApp.Properties.Resources.reddot;
                                //else
                                //    _mainForm.Drive2StatusImage = global::SWTPCemuApp.Properties.Resources.greendot;
                                //_mainForm.Drive2ActivityImage = global::SWTPCemuApp.Properties.Resources.greydot;
                                //_mainForm.Drive2Tag = imagePath;
                                //_mainForm.Drive2Name = Path.GetFileNameWithoutExtension(imagePath);
                                fileFound = true;
                            }
                            catch
                            {
                                //_mainForm.Drive2Image = global::SWTPCemuApp.Properties.Resources.drive2_open;
                                //_mainForm.Drive2Name = "";
                            }
                        }
                        else
                        {
                            //_mainForm.Drive2Image = global::SWTPCemuApp.Properties.Resources.drive2_open;
                            //_mainForm.Drive2Name = "";
                        }
                        break;
                    case 3:
                        if (imageFilename.Length > 0)
                        {
                            try
                            {
                                //_mainForm.Drive3Image = global::SWTPCemuApp.Properties.Resources.drive3_closed;
                                //if ((File.GetAttributes(imagePath) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                                //    _mainForm.Drive3StatusImage = global::SWTPCemuApp.Properties.Resources.reddot;
                                //else
                                //    _mainForm.Drive3StatusImage = global::SWTPCemuApp.Properties.Resources.greendot;
                                //_mainForm.Drive3ActivityImage = global::SWTPCemuApp.Properties.Resources.greydot;
                                //_mainForm.Drive3Tag = imagePath;
                                //_mainForm.Drive3Name = Path.GetFileNameWithoutExtension(imagePath);
                                fileFound = true;
                            }
                            catch
                            {
                                //_mainForm.Drive3Image = global::SWTPCemuApp.Properties.Resources.drive3_open;
                                //_mainForm.Drive3Name = "";
                            }
                        }
                        else
                        {
                            //_mainForm.Drive3Image = global::SWTPCemuApp.Properties.Resources.drive3_open;
                            //_mainForm.Drive3Name = "";
                        }
                        break;
                }

                if (fileFound)
                {
                    fileExists = File.Exists(imagePath);

                    if (fileExists)
                    {
                        if (driveImagePaths[i] != imagePath)
                        {
                            driveImagePaths[i] = imagePath;
                            drivePathChanged[i] = true;
                            if (_floppyDriveStream[i] != null)
                            {
                                _floppyDriveStream[i].Close();
                                _floppyDriveStream[i] = null;
                            }
                        }

                        if (driveImageFormats[i] != imageFormat)
                        {
                            if (driveImagePaths[i] != dataDir)
                            {
                                driveImageFormats[i] = imageFormat;
                                driveFormatChanged[i] = true;
                            }
                        }

                        if (initialLoad)
                            driveFormatChanged[i] = true;

                        if ((driveImagePaths[i] != dataDir && _floppyDriveStream[i] == null) || driveFormatChanged[i])
                        {
                            if (driveFormatChanged[i])
                            {
                                switch (driveImageFormats[i])
                                {
                                    case "FLEX":
                                        m_nDiskFormat[i] = DiskFormats.DISK_FORMAT_FLEX;
                                        break;
                                    case "FLEX_IMA":
                                        m_nDiskFormat[i] = DiskFormats.DISK_FORMAT_FLEX_IMA;
                                        break;
                                    case "OS9":
                                        m_nDiskFormat[i] = DiskFormats.DISK_FORMAT_OS9;
                                        break;
                                    case "UNIFLEX":
                                        m_nDiskFormat[i] = DiskFormats.DISK_FORMAT_UNIFLEX;
                                        break;
                                    case "MINIFLEX":
                                        m_nDiskFormat[i] = DiskFormats.DISK_FORMAT_MINIFLEX;
                                        break;
                                    default:
                                        Console.WriteLine("Improper disk format specified in configuration file");
                                        break;
                                }
                            }

                            if (drivePathChanged[i] || initialLoad)
                            {
                                m_nIsDoubleSided[i] = 11;

                                try
                                {
                                    FileStream fs = File.Open(driveImagePaths[i], FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                                    _floppyDriveStream[i] = fs;

                                    _virtualFloppyManipulationRoutines[i] = new VirtualFloppyManipulationRoutines(DriveImagePaths[i], fs);

                                    fileLength = fs.Length;

                                    if ((m_nDiskFormat[i] == DiskFormats.DISK_FORMAT_FLEX) || (m_nDiskFormat[i] == DiskFormats.DISK_FORMAT_UNIFLEX) || (m_nDiskFormat[i] == DiskFormats.DISK_FORMAT_FLEX_IMA))
                                    {
                                        if (m_nDiskFormat[i] == DiskFormats.DISK_FORMAT_FLEX)
                                        {
                                            byte[] nbroftracks = new byte[1];
                                            byte[] nbrofsectorspertrack = new byte[1];

                                            _floppyDriveStream[i].Seek(0x0226, SeekOrigin.Begin);
                                            _floppyDriveStream[i].Read(nbroftracks, 0, 1);           // this actually gets the MAX TRACK value
                                            m_nNumberOfTracks[i] = nbroftracks[0];
                                            m_nNumberOfTracks[i]++;                         // This converts it to Number of Tracks
                                            _floppyDriveStream[i].Read(nbrofsectorspertrack, 0, 1);
                                            m_nSectorsPerTrack[i] = nbrofsectorspertrack[0];
                                            m_nSectorsOnTrackZero[i] = nbrofsectorspertrack[0];
                                            //m_nSectorsOnTrackZeroSideOne[i] = nbrofsectorspertrack[0];
                                        }
                                        else if (m_nDiskFormat[i] == DiskFormats.DISK_FORMAT_FLEX_IMA)
                                        {
                                            //m_nSectorsOnTrackZeroSideOne[i] = 10;

                                            // if this image is  formatted as an IMA image, then track zero will have either 10, 15, 20  or 30 sectors
                                            // depending on whether it is single or double sided or 5 1/4" or 8". We can tell if it is single or double
                                            // sided if the maximum number of sectors is > 1
                                            //
                                            //  valid IMA formats:
                                            //
                                            //          5 1/4" drive geometries
                                            //
                                            //  tracks      sides   density     sectors/track 0 sectors/track   filesize                     blank filename
                                            //  --------    -----   -------     -------------   -------------   --------------------------   --------------
                                            //  35          1       single      10              10              35 * 1 * 10 * 256 =   89600  SSSD35T.IMA
                                            //  35          2       single      20              20              35 * 2 * 10 * 256 =  179200  DSSD35T.IMA
                                            //  35          1       double      10              18       2560 + 34 * 1 * 18 * 256 =  159232  SSDD35T.IMA
                                            //  35          2       double      20              36       5120 + 34 * 2 * 18 * 256 =  318464  DSDD35T.IMA
                                            //  40          1       single      10              10              40 * 1 * 10 * 256 =  102400  SSSD40T.IMA
                                            //  40          2       single      20              20              40 * 2 * 10 * 256 =  204800  DSSD40T.IMA <-
                                            //  40          1       double      10              18       2560 + 39 * 1 * 18 * 256 =  182272  SSDD40T.IMA
                                            //  40          2       double      20              36       5120 + 39 * 2 * 18 * 256 =  364544  DSDD40T.IMA
                                            //  80          1       single      10              10              80 * 1 * 10 * 256 =  204800  SSSD80T.IMA <-
                                            //  80          2       single      20              20              80 * 2 * 10 * 256 =  409600  DSSD80T.IMA
                                            //  80          1       double      10              18       2560 + 79 * 1 * 18 * 256 =  366592  SSDD80T.IMA
                                            //  80          2       double      20              36       5120 + 79 * 2 * 18 * 256 =  733184  DSDD80T.IMA
                                            //
                                            //          8" drive geometries
                                            //
                                            //  tracks      sides   density     sectors/track 0 sectors/track   filesize                     blank filename
                                            //  --------    -----   -------     -------------   -------------   --------------------------   --------------
                                            //  77          1       single      15              15              77 * 1 * 15 * 256 =  295680  SSSD77T.IMA
                                            //  77          2       single      30              30              77 * 2 * 15 * 256 =  591360  DSSD77T.IMA
                                            //  77          1       double      15              26       3840 + 76 * 1 * 26 * 256 =  516352  SSSD77T.IMA
                                            //  77          2       double      30              52       3840 + 76 * 2 * 26 * 256 = 1032704  DSSD77T.IMA
                                            //
                                            //      Since these are the only valid IMA formats - we can assume double sided if the max sectors for
                                            //  this images is > 18 (because sectors are 1 based not 0 based)
                                            //
                                            //  80 track single sides single density is the same size as 40 track double sided single density. The 
                                            //  only way to tell the difference is by the max sectors per track. If it's 10 - this is single sided
                                            //  single density 80 track otherwise it is double single density sided 40 track

                                            byte[] maxTrack = new byte[1];
                                            byte[] nbrofsectorspertrack = new byte[1];

                                            _floppyDriveStream[i].Seek(0x0226, SeekOrigin.Begin);
                                            _floppyDriveStream[i].Read(maxTrack, 0, 1);             // this actually gets the MAX TRACK value
                                            m_nNumberOfTracks[i] = maxTrack[0];                     // read the value from the image
                                            m_nNumberOfTracks[i]++;                                 // This converts it to Number of Tracks
                                            _floppyDriveStream[i].Read(nbrofsectorspertrack, 0, 1);
                                            m_nSectorsPerTrack[i] = nbrofsectorspertrack[0];

                                            if (m_nSectorsPerTrack[i] > 18)
                                                m_nIsDoubleSided[i] = 1;
                                            else
                                                m_nIsDoubleSided[i] = 0;

                                            long fileSizeMinusTrackZero = (m_nNumberOfTracks[i] - 1) * m_nSectorsPerTrack[i] * 256;
                                            long sizeOfTrackZero = fileLength - fileSizeMinusTrackZero;
                                            int numberOfSectorsOnTrackZero = (int)sizeOfTrackZero / 256;

                                            // if the number of sectors on track zero = the number of sectors on the rest of the tracks
                                            // that is OK

                                            if (numberOfSectorsOnTrackZero != m_nSectorsPerTrack[i])
                                                m_nSectorsOnTrackZero[i] = numberOfSectorsOnTrackZero;
                                            else
                                                m_nSectorsOnTrackZero[i] = numberOfSectorsOnTrackZero;
                                        }
                                        else if (m_nDiskFormat[i] == DiskFormats.DISK_FORMAT_UNIFLEX)
                                        {

                                            m_nIsDoubleSided[i] = -1;        // -1 means it's not set yet, 0 = not double sided, 1 = double sided

                                            // there are 1,261,568 bytes on a double sided double density 8" uniflex diskette
                                            // there are   630,784 bytes on a single sided double density 8" uniflex diskette
                                            // there are   630,784 bytes on a double sided single density 8" uniflex diskette
                                            // there are   315,392 bytes on a single sided single density 8" uniflex diskette

                                            switch (fileLength)
                                            {
                                                case 1261568:                                   // double sided double density 8" uniflex diskette
                                                    m_nNumberOfTracks[i] = 77;     // always 77 tracks
                                                    m_nSectorsPerTrack[i] = 32;     // 16 X 2 = 32 sectors
                                                    break;

                                                case 630784:                                    // single sided double density 8" uniflex diskette or double sided single density 8" uniflex diskette
                                                    m_nNumberOfTracks[i] = 77;     // always 77 tracks
                                                    m_nSectorsPerTrack[i] = 16;     // Either way there are only 16 sectors per track (8 X 2 or 16 X 1 = 16 sectors)
                                                    break;

                                                case 315392:                                    // 315,392 bytes on a single sided single density 8" uniflex diskette
                                                    m_nNumberOfTracks[i] = 77;     // always 77 tracks
                                                    m_nSectorsPerTrack[i] = 8;      // 8 X 1 = 8 sectors
                                                    break;

                                                default:
                                                    m_nNumberOfTracks[i] = 77;     // maximum if size is incorrect
                                                    m_nSectorsPerTrack[i] = 32;
                                                    break;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        _virtualFloppyManipulationRoutines[i].physicalParameters = _virtualFloppyManipulationRoutines[i].GetPhysicalOS9Geometry(fs);

                                        m_nSectorsPerTrack[i] = (byte)_virtualFloppyManipulationRoutines[i].physicalParameters.sectorsPerTrack;
                                        m_nNumberOfCylinders[i] = (byte)_virtualFloppyManipulationRoutines[i].physicalParameters.numberOfCylinders;

                                        if (_virtualFloppyManipulationRoutines[i].physicalParameters.doubleSided)
                                            m_nNumberOfTracks[i] = (byte)_virtualFloppyManipulationRoutines[i].physicalParameters.numberOfCylinders * 2;
                                        else
                                            m_nNumberOfTracks[i] = (byte)_virtualFloppyManipulationRoutines[i].physicalParameters.numberOfCylinders;

                                        m_nSectorsOnTrackZeroSideZero[i] = (byte)_virtualFloppyManipulationRoutines[i].physicalParameters.sectorsOnTrackZeroSideZero;
                                        m_nFormatByte[i] = (byte)_virtualFloppyManipulationRoutines[i].physicalParameters.formatByte;
                                        m_nSectorsOnTrackZeroSideOne[i] = (byte)_virtualFloppyManipulationRoutines[i].physicalParameters.sectorsOnTrackZeroSideOne;

                                        // this is for OS - 9

                                        int nTotalSectors = 0;
                                        byte[] temp = new byte[1];
                                        byte[] nbrofsectorspertrack = new byte[1];
                                        byte[] formatbyte = new byte[1];

                                        _floppyDriveStream[i].Seek(0, SeekOrigin.Begin);
                                        _floppyDriveStream[i].Read(temp, 0, 1);                    // offset 0
                                        nTotalSectors = (temp[0] * 65536);
                                        _floppyDriveStream[i].Read(temp, 0, 1);                    // offset 1
                                        nTotalSectors += (temp[0] * 256);
                                        _floppyDriveStream[i].Read(temp, 0, 1);                    // offset 2
                                        nTotalSectors += temp[0];
                                        _floppyDriveStream[i].Read(nbrofsectorspertrack, 0, 1);   // offset 3
                                        m_nSectorsPerTrack[i] = nbrofsectorspertrack[0];

                                        // leave it actual tracks for following calculation

                                        try
                                        {
                                            m_nNumberOfTracks[i] = nTotalSectors / m_nSectorsPerTrack[i];
                                            m_nSectorsOnTrackZeroSideZero[i] = nTotalSectors - (m_nNumberOfTracks[i] * m_nSectorsPerTrack[i]);
                                            m_nSectorsOnTrackZero[i] = nTotalSectors % m_nSectorsPerTrack[i];
                                            if (m_nSectorsOnTrackZero[i] == 0)
                                                m_nSectorsOnTrackZero[i] = m_nSectorsPerTrack[i];

                                            // now convert it to cylinders

                                            m_nNumberOfTracks[i] = (m_nNumberOfTracks[i] + 1) / 2;

                                            // get disk format byte

                                            _floppyDriveStream[i].Seek(16, SeekOrigin.Begin);
                                            _floppyDriveStream[i].Read(formatbyte, 0, 1);
                                            m_nFormatByte[i] = formatbyte[0];

                                            // calculate number of sectors on track zero side one

                                            m_nSectorsOnTrackZeroSideOne[i] = (m_nNumberOfTracks[i] - 1) * m_nSectorsPerTrack[i];
                                            if ((m_nFormatByte[i] & 0x0001) != 0)   // double sided
                                            {
                                                m_nSectorsOnTrackZeroSideOne[i] *= 2;
                                            }
                                            m_nSectorsOnTrackZeroSideOne[i] = m_nSectorsOnTrackZeroSideOne[i] + m_nSectorsOnTrackZeroSideZero[i];
                                            m_nSectorsOnTrackZeroSideOne[i] = nTotalSectors - m_nSectorsOnTrackZeroSideOne[i];
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine(string.Format("Invalid diskette image format for disk drive number {0}\r\n", i));
                                            Console.WriteLine(string.Format("{0}\r\n", ex.Message));
                                            Console.WriteLine("Press any key to continue.");
                                            Console.ReadLine();
                                            Console.Clear();
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(string.Format("{0}\r\n", e.Message));
                                    Console.WriteLine("Press any key to continue.");
                                    Console.ReadLine();
                                    Console.Clear();
                                }
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine(string.Format("File Not Found for DRIVE {0}: {1}\r\n", i, imagePath));
                        Console.WriteLine("Press any key to continue.");
                        Console.ReadLine();
                        Console.Clear();
                    }
                }
            }
        }

        private static void LoadBreakPoints()
        {
            _cpu.BreakpointAddress.Clear();

            string breakPoints = GetConfigurationAttribute("Global/DebugInfo/BreakPoints", "values", "");
            string[] breakPointList = breakPoints.Split(',');
            foreach (string bp in breakPointList)
            {
                uint result = 0;
                if (uint.TryParse(bp, System.Globalization.NumberStyles.HexNumber, null, out result))
                {
                    _cpu.BreakpointAddress.Add((ushort)result);
                }
            }
            _cpu.BreakpointsEnabled = GetConfigurationAttribute("Global/DebugInfo/BreakPoints", "enabled", 0) == 1 ? true : false;
            _cpu.ExcludeSingleStepEnabled = GetConfigurationAttribute("Global/DebugInfo/Exclude", "enabled", 0) == 1 ? true : false;

            //if (_cpu.BreakpointsEnabled)
            //    _mainForm.Invoke(new Action(() => _mainForm.btnButtonEnableBreakPoints_Click(null, null)));

            // Get these also:  <Exclude     enabled="0" values="CD03-CDFF,D406-D4FF" />

            string exclude = GetConfigurationAttribute("Global/DebugInfo/Exclude", "values", "");
            string[] excludeList = exclude.Split(',');
            foreach (string e in excludeList)
            {
                string start = "";
                string end = "";

                string[] addresses = e.Split('-');
                if (addresses.Length == 2)
                {
                    start = addresses[0];
                    end = addresses[1];
                }
                else if (addresses.Length == 1)
                {
                    start = addresses[0];
                    end = addresses[0];
                }

                //AddressPair ap = new AddressPair();
                //if ((ushort.TryParse(start, System.Globalization.NumberStyles.HexNumber, null, out ap.start)) && (ushort.TryParse(end, System.Globalization.NumberStyles.HexNumber, null, out ap.end)))
                //    _excludedAddressRangesFromSingleStep.Add(ap);
            }
        }

        private static void LoadTraceExclusions ()
        {
            _cpu.TraceExclusionAddress.Clear();
            _cpu.ExcludeExcludeTraceRangeEnabled = GetConfigurationAttribute("Global/DebugInfo/ExcludeTraceRange", "enabled", 0) == 1 ? true : false;

            string exclude = GetConfigurationAttribute("Global/DebugInfo/ExcludeTraceRange", "values", "");
            string[] excludeList = exclude.Split(',');
            foreach (string e in excludeList)
            {
                string start = "";
                string end = "";

                string[] addresses = e.Split('-');
                if (addresses.Length == 2)
                {
                    start = addresses[0];
                    end = addresses[1];
                }
                else if (addresses.Length == 1)
                {
                    start = addresses[0];
                    end = addresses[0];
                }

                //AddressPair ap = new AddressPair();
                //if ((ushort.TryParse(start, System.Globalization.NumberStyles.HexNumber, null, out ap.start)) && (ushort.TryParse(end, System.Globalization.NumberStyles.HexNumber, null, out ap.end)))
                //    _excludedAddressRangesFromTrace.Add(ap);
            }
        }

        private static void LoadDebugInfo()
        {
            LoadBreakPoints();
            LoadTraceExclusions();
        }

        static void StartCPU ()
        {
            try
            {
                if (LoadROM(_cpu.Memory))
                {
                    // create a thread to monitor the speed we are emulating at.

                    Thread showSpeed = new Thread(ShowAverageSpeed);
                    showSpeed.Start();

                    // ROM is loaded

                    InitializeHardware(_cpu.Memory);

                    // Load the drives

                    try
                    {
                        LoadDrives(true);
                    }
                    catch (Exception loadDrivesException)
                    {
                        Console.WriteLine(loadDrivesException.Message);
                        Console.ReadKey();
                    }

                    // Create a console to use for operator input/output

                    _theConsole = new console();

                    // create and start the cpu.

                    LoadDebugInfo();

                    _cpuThread = new Thread(_cpu.Run);      // set the thread entry point
                    _cpuThread.Start();                     // start the thread
                }
                else
                {
                    Console.WriteLine("Unable to load rom file");
                    Console.ReadKey();

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.ReadKey();
            }

            Thread.Sleep(10);      // give everything time to start up before starting idle loop
        }

        private static List<Memory.MemoryBoard> memoryBoards = new List<Memory.MemoryBoard>();
        internal static List<Memory.MemoryBoard> MemoryBoards
        {
            get { return Program.memoryBoards; }
            set { Program.memoryBoards = value; }
        }

        static void Start6800()
        {
            _cpu = new Cpu6800();
            if (memoryBoards.Count == 0)
            {
                _cpu.Memory = new Memory(65536, 0);
            }
            else
                _cpu.Memory = new Memory(memoryBoards);

            StartCPU();
        }

        static void Start6809()
        {
            _cpu = new Cpu6809();
            if (memoryBoards.Count == 0)
            {
                _cpu.Memory = new Memory(65536 * 16, 0);
            }
            else
                _cpu.Memory = new Memory(memoryBoards);

            StartCPU();
        }

        public static void SaveConfigurationAttribute(XmlDocument doc, string xpath, string attribute, string value)
        {
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
                        if (value != valueNode.Value)
                            valueNode.Value = value;
                    }
                    else
                    {
                        XmlAttribute attr = doc.CreateAttribute(attribute);
                        attr.Value = value;
                        node.Attributes.Append(attr);
                    }
                }
            }
            else
            {
                // need to add this xpath node to the keyboard map

                string[] uriParts = xpath.Split('/');
                string name = uriParts[uriParts.Length - 1];

                XmlNode finalNode = configurationNode;
                XmlNode previousNode = finalNode;
                for (int i = 0; i < uriParts.Length - 1; i++)
                {
                    finalNode = finalNode.SelectSingleNode(uriParts[i]);
                    if (finalNode == null)
                    {
                        XmlNode newNode = doc.CreateNode(XmlNodeType.Element, uriParts[i], "");
                        previousNode.AppendChild(newNode);

                        finalNode = previousNode.SelectSingleNode(uriParts[i]);
                    }
                    previousNode = finalNode;
                }

                if (finalNode != null)
                {
                    XmlNode newNode = doc.CreateNode(XmlNodeType.Element, name, "");
                    XmlAttribute attr = doc.CreateAttribute(attribute);
                    attr.Value = value;

                    newNode.Attributes.Append(attr);
                    finalNode.AppendChild(newNode);
                }
            }
        }

        public static string GetConfigurationAttribute(string xpath, string attribute, string defaultvalue)
        {
            string value = defaultvalue;

            try
            {
                FileStream xmlDocStream = File.OpenRead(configFileName);
                XmlReader reader = XmlReader.Create(xmlDocStream);

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
                xmlDocStream.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return value;
        }

        // Modified to allow numbers to be specified as eothe decimal or hex if preceeded with "0x" or "0X"
        public static int GetConfigurationAttribute(string xpath, string attribute, int defaultvalue)
        {
            int value = defaultvalue;

            try
            {
                FileStream xmlDocStream = File.OpenRead(configFileName);
                XmlReader reader = XmlReader.Create(xmlDocStream);

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
                                    if (strvalue.StartsWith("0x") || strvalue.StartsWith("0X"))
                                    {
                                        value = Convert.ToInt32(strvalue, 16);
                                    }
                                    else
                                        Int32.TryParse(strvalue, out value);
                                }
                            }
                        }
                    }
                    reader.Close();
                }
                xmlDocStream.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return value;
        }

        public static string GetConfigurationAttribute(string xpath, string attribute, string ordinal, string defaultvalue)
        {
            string value = defaultvalue;
            bool foundOrdinal = false;

            try
            {
                FileStream xmlDocStream = File.OpenRead(configFileName);
                XmlReader reader = XmlReader.Create(xmlDocStream);

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
                xmlDocStream.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return value;
        }

        // Modified to allow numbers to be specified as eothe decimal or hex if preceeded with "0x" or "0X"
        public static int GetConfigurationAttribute(string xpath, string attribute, string ordinal, int defaultvalue)
        {
            int value = defaultvalue;
            bool foundOrdinal = false;

            FileStream xmlDocStream = File.OpenRead(configFileName);
            XmlReader reader = XmlReader.Create(xmlDocStream);

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
                                            if (strvalue.StartsWith("0x") || strvalue.StartsWith("0X"))
                                            {
                                                value = Convert.ToInt32(strvalue, 16);
                                            }
                                            else
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
            xmlDocStream.Close();
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

            string strBoardType = GetConfigurationAttribute(ConfigSection + "/BoardConfiguration/Board", "Type", nRow.ToString(), "");
            switch (strBoardType)
            {
                case "CONS":     boardtype =  2; break;
                case "MPS":      boardtype =  3; break;
                case "FD2":      boardtype =  4; break;
                case "MPT":      boardtype =  5; break;
                case "DMAF1":    boardtype =  6; break;
                case "DMAF2":    boardtype =  7; break;
                case "DMAF3":    boardtype =  8; break;
                case "PRT":      boardtype =  9; break;
                case "MPL":      boardtype = 10; break;
                case "DAT":      boardtype = 11; break;
                case "IOP":      boardtype = 12; break;
                case "MPID":     boardtype = 13; break;
                case "PIAIDE":   boardtype = 14; break;
                case "TTLIDE":   boardtype = 15; break;
                case "8274":     boardtype = 16; break;
                case "PCSTREAM": boardtype = 17; break;
                default: boardtype = 0; break;
            }

            return boardtype;
        }

        private static object lockTestObject = new object();

        /// <summary>
        /// The main entry point for the application.
        /// </summary>

        private static int doEventsInterval = 0;

       public static void DoEvents()
        {
            //if ((++doEventsInterval % 100) == 0)
            //    Application.DoEvents();
        }

        private static OSPlatform _platform;
        public static OSPlatform Platform { get => _platform; set => _platform = value; }
        public static void GetOSPlatform()
        {
            OSPlatform osPlatform = OSPlatform.Create("Other Platform");
            // Check if it's windows 
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            osPlatform = isWindows ? OSPlatform.Windows : osPlatform;
            // Check if it's osx 
            bool isOSX = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            osPlatform = isOSX ? OSPlatform.OSX : osPlatform;
            // Check if it's Linux 
            bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            osPlatform = isLinux ? OSPlatform.Linux : osPlatform;
            Platform = osPlatform;
        }

        // Satisfies rule: MarkWindowsFormsEntryPointsWithStaThread. <- this should allow us to use windows forms 
        [STAThread]
        static void Main(string[] args)
        {
            /*
                #if !WIN32
                        // Catch SIGWINCH to handle terminal resizing

                        UnixSignal[] signals = new UnixSignal [] {new UnixSignal (Signum.SIGWINCH)};

                        Thread signal_thread = new Thread 
                        (delegate () 
                            {
                                while (true) 
                                {
                                    // Wait for a signal to be delivered

                                    int index = UnixSignal.WaitAny (signals, -1);
                                    Signum signal = signals [index].Signum;
                                    Libc.writeInt64 (pipeFds[1], 2);
                                }
                            }
                        );
                        signal_thread.IsBackground = false;
                        signal_thread.Start ();
                #endif
            */

            GetOSPlatform();

            // must do this first to see if user is overriding the configuration filename

            configFileName = "configuration.xml";    // set default in case user has not made a preference on the command line

            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).Replace("\\", "/") + "/EvensonConsultingServices/SWTPCmemulator/";
            string commonAppDir = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData).Replace("\\", "/") + "/EvensonConsultingServices/SWTPCmemulator/";
            string userAppDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).Replace("\\", "/") + "/EvensonConsultingServices/SWTPCmemulator/";

            System.Diagnostics.Debug.WriteLine(string.Format("User User Profile Folder:        {0}", userAppDir));
            System.Diagnostics.Debug.WriteLine(string.Format("Application Data Folder:         {0}", appDataFolder));
            System.Diagnostics.Debug.WriteLine(string.Format("Common Application Data Fildeor: {0}", commonAppDir));

            // this logic gives precedence to the User AppDir over the App Data folder over the Common AppDir. If npn of these exist - uses execution directory as dataDir.

            if (Directory.Exists(userAppDir))
            {
                dataDir = userAppDir;
            }
            else if (Directory.Exists(appDataFolder))
            {
                dataDir = appDataFolder;
            }
            else if (Directory.Exists(commonAppDir))
            {
                dataDir = commonAppDir;
            }
            else
            {
                dataDir = "./";
            }

            System.Diagnostics.Debug.WriteLine(string.Format("Data Directory: {0}", dataDir));

            string defaultConfigFilename = dataDir + "defaultConfiguration.txt";
            if (File.Exists(defaultConfigFilename))
            {
                configFileName = File.ReadAllText(defaultConfigFilename).TrimEnd();
            }

            foreach (string arg in args)
            {
                if (arg.ToLower().StartsWith("-configfile="))
                {
                    configFileName = arg.Replace("-configfile=", "");
                }
                else if (arg.ToLower().StartsWith("-dma3accesslogging="))
                {
                    DMAF3AccessLogging = true;
                    activityLogFileDMAF3 = arg.ToLower().Replace("-dma3accesslogging=", "");
                }
                else if (arg.ToLower().StartsWith("-dma2accesslogging="))
                {
                    DMAF2AccessLogging = true;
                    activityLogFileDMAF3 = arg.ToLower().Replace("-dma2accesslogging=", "");
                }
                //else if (arg.StartsWith("-debugMode"))
                //{
                //    debugMode = true;
                //}
                //else if (arg.StartsWith("-verbose"))
                //{
                //    verbose = true;
                //}
            }

            //if (debugMode)
            //{
            //    // create the socket
            //    listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            //    // bind the listening socket to the port
            //    byte[] localHost = new byte[4] { 127, 0, 0, 1 };
            //    IPAddress hostIP = new IPAddress(localHost);
            //    IPEndPoint ep = new IPEndPoint(hostIP, 1410);
            //    listenSocket.Bind(ep);

            //    // Console.WriteLine("Listening");
            //    // start listening
            //    listenSocket.Listen(1024);
            //}

            if (File.Exists(dataDir + "CONFIGFILES/" + configFileName))
                configFileName = dataDir + "CONFIGFILES/" + configFileName;

            //if (verbose)
            //    Console.WriteLine("Loading ConfigFile: {0}", configFileName);

            _consoleDumpFile = GetConfigurationAttribute("Global/ConsoleDump", "filename", string.Format("{0}consoledump.txt", dataDir));

            _traceFilePath = GetConfigurationAttribute("Global/Trace", "Path", string.Format("{0}TraceFile.txt", dataDir));
            _bTraceEnabled = GetConfigurationAttribute("Global/Trace", "Enabled", 0) == 0 ? false : true;

            string ProcessorBoardCpu = GetConfigurationAttribute("Global/ProcessorBoard", "CPU", "6800");
            ConfigSection = "config" + ProcessorBoardCpu;

            for (int i = 0; ; i++)
            {
                Memory.MemoryBoard mb = new Memory.MemoryBoard();

                mb.baseAddress = (ushort)GetConfigurationAttributeHex("Global/Memory/Board", "BaseAddress", i.ToString(), 0);
                mb.page = (byte)GetConfigurationAttributeHex("Global/Memory/Board", "ExtendedPage", i.ToString(), 0);
                string size = GetConfigurationAttribute("Global/Memory/Board", "Size", i.ToString(), "");

                if (size.Length > 0)
                {
                    uint uintSize;
                    bool addBoard = true;

                    if (!UInt32.TryParse(size, out uintSize))
                    {
                        // see if it ends with "K" or "M"
                        if (size.EndsWith("K"))
                        {
                            if (UInt32.TryParse(size.Substring(0, size.Length - 1), out uintSize))
                            {
                                uintSize = uintSize * 1024;
                            }
                            else
                            {
                                addBoard = false;
                            }
                        }
                        else if (size.EndsWith("M"))
                        {
                            if (UInt32.TryParse(size.Substring(0, size.Length - 1), out uintSize))
                            {
                                uintSize = uintSize * 1024 * 1024;
                            }
                            else
                            {
                                addBoard = false;
                            }
                        }
                        else
                        {
                            addBoard = false;
                        }
                    }

                    if (addBoard)
                    {
                        mb.size = uintSize;
                        memoryBoards.Add(mb);
                    }
                    else
                        Console.WriteLine("Houston - we have a problem");
                }
                else
                    break;
            }

            switch (ProcessorBoardCpu)
            {
                case "6800":
                    Start6800();
                    break;

                case "6809":
                    Start6809();
                    break;

                default:
                    Console.WriteLine("Invalid or no processor specified in configuration.xml file");
                    Console.WriteLine("Press any key to exit");
                    Console.ReadKey();
                    break;
            }

            // Stay in the Main Thread with sleep cycles while _cpu is running

            int timeToWaitForCpuTpStart = 3;

            for (int i = 0; i < timeToWaitForCpuTpStart; i++)
            {
                if (_cpu.Running)
                    break;

                Thread.Sleep(1000);
            }

            int waitBeforeExit = 5;

            if (_cpu.Running)
            {
                while (_cpu.Running)
                {
                    Thread.Sleep(1000);
                }
                waitBeforeExit = 0;
            }
            else
            {
                Console.WriteLine("CPU thread failed to start in " + timeToWaitForCpuTpStart.ToString() + " seconds");
            }

            while (--waitBeforeExit > 0)
            {
                Thread.Sleep(1000);
            }
        }
    }
}
