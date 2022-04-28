using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;

using System.IO;
using System.Threading;
using System.Diagnostics;

/*
    Bug Report:
           
                    SWTPCemuApp Version 6.1.3.26635
                   
    flex2_config.xml        - FIXED

        Starts with monitor and boots Flex.  Ran some commands to access filesystem on drive 0 and drive 1.  Looks good.  
        HOWEVER, if I execute the command "files 2" or "files 3" ("dir 2" or "dir 3") what's returned is garbage and I 
        can no longer access the disks on drive 0 and 1.  A directory listing on drive 0 and drive 1 returns "NOT FOUND".  
        The only way to clear this is power off or reset.

    flex9_fd2_config        - FIXED
        Starts with monitor but does not boot Flex9.  Locks up after U key.

    flex9_fd2_sbug_1.9.xml  - FIXED
        Starts with monitor but does not boot Flex9.  Locks up after U key.

    flex9_pt69s5_config.xml - FIXED
        Starts with monitor and boots Flex9.  Ran some commands to access filesystem.  Looks like the file system is 
        corrupted.  Occasionally get "Invalid Opcode".

    flex9_ttl_config.xml    - FIXED
        Starts with monitor but does not boot Flex9.  Locks up after U key.

    OS9_config              - FIXED (this ROM does not check NOT READY, so it hangs when reading an empty drive)
        Boots to OS9.  Ran some commands to access the filesystem.  Looks good.

    OS9_pt69s5_config       - FIXED (this ROM does check NOT READY)
        Boots to OS9.  Ran some commands to access the filesystem.  Looks good.

        
                SWTPCemuApp 6.1.3.18248 

    flex2_config.xml        - FIXED
        Starts with monitor and boots Flex.  Ran some commands to access filesystem on drive 0 and drive 1.  Looks good.  
        HOWEVER, if I execute the command "files 2" or "files 3" ("dir 2" or "dir 3") what's returned is garbage and I 
        can no longer access the disks on drive 0 and 1.  A directory listing on drive 0 and drive 1 returns "NOT FOUND".  
        The only way to clear this is power off or reset.

    flex9_fd2_config        - FIXED
        Starts with monitor and boots Flex9.  However the file system on the floppy is corrupt.  I didn't try to access 
        the drive the last time I tested this so it's probably been that way for some unknown amount of time.  HOWEVER, 
        if I execute the command "files 1" or "files 2" or "files 3" what's returned is garbage and I can no longer 
        access the disk on drive 0.  A directory listing on drive 0 returns "NOT FOUND".  The only way to clear this 
        is power off or reset.

    flex9_fd2_sbug_1.9.xml  - FIXED
        Starts with monitor and boots Flex9.  Ran some commands to access filesystem on drive 0.  Looks good.  
        HOWEVER, if I execute the command "files 2" or "files 3" what's returned is garbage and I can no longer 
        access the disk on drive 0.  A directory listing on drive 1 and drive 2 and drive 3 returns "NOT FOUND".  
        At Flex09 boot and after entering the date and time, the TIMERON command returns "The current time is 
        255.255.255".  I suspect that's because there isn't a timer card installed?  

    flex9_pt69s5_config.xml - FIXED
        Starts with monitor and boots Flex9.   Ran some commands to access filesystem on drive 0.  Looks good.  
        HOWEVER, if I execute the command "files 2" or "files 3" what's returned is garbage and I can no longer 
        access the disk on drive 0.  A directory listing on drive 1 and drive 2 and drive 3 returns "NOT FOUND".  
        The only way to clear this is power off or reset.    

    flex9_ttl_config.xml    - FIXED
        Starts with monitor and boots Flex9.   Ran some commands to access filesystem on drive 0.  Looks good.  
        HOWEVER, if I execute the command "files 2" or "files 3" what's returned is garbage and I can no longer 
        access the disk on drive 0.  A directory listing on drive 1 and drive 2 and drive 3 returns "NOT FOUND".  
        The only way to clear this is power off or reset.  

                    Summary 

    SWTPCemuApp version 6.1.3.18248 will run Flex and Flex09, but not OS9 or Uniflex.  
    There is a problem with Flex/Flex09 disk access that if you access a drive that 
    doesn't have a valid disk image mounted, you can no longer access any drive on 
    the system until reset or power off/on.

    SWTPCemuApp version 6.1.3.26635 will run Flex and OS9, but not Flex09 or Uniflex.  
    There is a problem with Flex disk access that if you access a drive that doesn't 
    have a valid disk image mounted, you can no longer access any drive on the system 
    until reset or power off/on.
 
        unable to reproduce.

    Occasionally when resetting the emulator a few times I get "Invalid OPCODE [72] encountered at address [0C082]".  
    It takes about 4 or 5 resets before I see the error.  The OPCODE and address values aren't the same between events.

*/
namespace Memulator
{
    class FD_2 : IODevice
    {
        //             STATUS REGISTER SUMMARY (WD2797)
        // 
        // ALL     TYPE I      READ        READ        READ        WRITE       WRITE
        // BIT     COMMANDS    ADDRESS     SECTOR      TRACK       SECTOR      TRACK
        // ----------------------------------------------------------------------------------
        // S7      NOT READY   NOT READY   NOT READY   NOT READY   NOT READY   NOT READY
        // S6      WRITE       0           0           0           WRITE       WRITE
        //         PROTECT                                         PROTECT     PROTECT
        // S5      HEAD LOADED 0           RECORD TYPE 0           0           0
        // S4      SEEK ERROR  RNF         RNF         0           RNF         0
        // S3      CRC ERROR   CRC ERROR   CRC ERROR   0           CRC ERROR   0
        // S2      TRACK 0     LOST DATA   LOST DATA   LOST DATA   LOST DATA   LOST DATA
        // S1      INDEX PULSE DRO         DRO         DRO         DRO         DRO
        // SO      BUSY        BUSY        BUSY        BUSY        BUSY        BUSY        
        // ----------------------------------------------------------------------------------

        bool _spin = false;

        //#define RD_FLOPPY       0
        //#define WT_FLOPPY       1
        //
        //#define FDC_DRVREG    0x8014        //  Which Drive is selected
        //#define FDC_STATREG   0x8018        //      READ
        //                                    //  0x01 = BUSY
        //                                    //  0x02 = DRQ
        //                                    //  0x04 = 
        //                                    //  0x08 = 
        //                                    //  0x10 = 
        //#define FDC_CMDREG    0x8018        //      WRITE
        //                                    //  0x0X = RESTORE
        //                                    //  0x1X = SEEK
        //                                    //  0x2X = STEP
        //                                    //  0x3X = STEP     W/TRACK UPDATE
        //                                    //  0x4X = STEP IN
        //                                    //  0x5X = STEP IN  W/TRACK UPDATE
        //                                    //  0x6X = STEP OUT
        //                                    //  0x7X = STEP OUT W/TRACK UPDATE
        //                                    //  0x8X = READ  SINGLE   SECTOR
        //                                    //  0x9X = READ  MULTIPLE SECTORS
        //                                    //  0xAX = WRITE SINGLE   SECTOR
        //                                    //  0xBX = WRITE MULTIPLE SECTORS
        //                                    //  0xCX = READ ADDRESS
        //                                    //  0xDX = FORCE INTERRUPT
        //                                    //  0xEX = READ TRACK
        //                                    //  0xFX = WRITE TRACK
        //
        //#define FDC_TRKREG    0x8019        //  Track Register
        //#define FDC_SECREG    0x801A        //  Sector Register
        //#define FDC_DATAREG   0x801B        //  Data Register

        private int FDC_BUSY            = 0x01;
        private int FDC_DRQ             = 0x02;
        private int FDC_TRKZ            = 0x04;
        private int FDC_CRCERR          = 0x08;
        private int FDC_SEEKERR         = 0x10;
        private int FDC_RNF             = 0x10;
        private int FDC_HDLOADED        = 0x20;
        private int FDC_WRTPROTECT      = 0x40;
        private int FDC_NOTREADY        = 0x80;

        private int DRV_DRQ             = 0x80;
        private int DRV_INTRQ           = 0x40;

        enum FDCRegisterOffsets
        {
            FDC_DRVREG_OFFSET   = 0,
            FDC_STATREG_OFFSET  = 4,
            FDC_CMDREG_OFFSET   = 4,
            FDC_TRKREG_OFFSET   = 5,
            FDC_SECREG_OFFSET   = 6,
            FDC_DATAREG_OFFSET  = 7
        }

        private int             m_statusReadsWithoutDataRegisterAccess;
        private int             m_nRow;
        //private volatile bool   m_nAbort = false;
        private int             m_nCurrentSideSelected = 0;

        private bool            m_nFDCWriting   = false;
        private bool            m_nFDCReading   = false;
        private bool            m_nReadingTrack = false;
        private bool            m_nWritingTrack = false;
        private int             m_nFDCReadPtr   = 0;
        private int             m_nFDCWritePtr  = 0;


        private int             m_nFDCTrack;
        private int             m_nFDCSector;
        private long            m_lFileOffset;

        private int             m_nBytesToTransfer;
        //private int             m_nStatusReads;

        private byte []         m_caReadBuffer  = new byte [65536];
        private byte []         m_caWriteBuffer = new byte [65536];

        private byte            m_FDC_DRVRegister;
        private byte            m_FDC_STATRegister;
        private byte            m_FDC_CMDRegister;
        private byte            m_FDC_TRKRegister;
        private byte            m_FDC_SECRegister;
        private byte            m_FDC_DATARegister;

        private Dictionary<int, string> registerDescription = new Dictionary<int, string>();

        public FD_2()
        {
            //  WD2797 Registers

            registerDescription.Add(0x0000, "Drive and side");
            registerDescription.Add(0x0004, "READ - STATUS / WRITE - COMMAND");
            registerDescription.Add(0x0005, "Track Register");
            registerDescription.Add(0x0006, "Sector Register");
            registerDescription.Add(0x0007, "Data Register");
        }

        public new void Dispose()
        {
            for (int drive = 0; drive < 4; drive++)
            {
                FileStream fs = Program.FloppyDriveStream[drive];
                if (fs != null)
                {
                    fs.Close();
                    Program.FloppyDriveStream[drive] = null;
                    Program.DriveImagePaths[drive] = null;
                    Program.DriveImageFormats[drive] = null;
                }
            }
        }

        class LogActivityValues
        {
            public int whichRegister;
            public byte value;
            public bool read;
        }

        LogActivityValues previousLogActivity = null;
        int duplicateCount = 0;

        // these are used ONLY for logging reads from the floppy controller

        int readBufferIndex = 0;
        byte[] readBuffer = new byte[65536];     // make big enough to haold a track of 255 sectors and then some

        #region Floppy Read Track Variables and enums
        // define the STATE for the read track state machine

        enum READ_TRACK_STATES
        {
            ReadPostIndexGap = 0,
            ReadIDRecord,       
            ReadIDRecordTrack,
            ReadIDRecordSide,
            ReadIDRecordSector,
            ReadIDRecordSectorLength,
            ReadIDRecordCRCHi,
            ReadIDRecordCRCLo,
            ReadGap2,
            ReadDataRecord,
            ReadDataBytes,
            ReadDataRecordCRCHi,
            ReadDataRecordCRCLo,
            ReadGap3
        }

        const int
            currentPostIndexGapSize = 8,
            currentGap2Size         = 17,
            currentGap3Size         = 33;

        int currentReadTrackState = (int)READ_TRACK_STATES.ReadPostIndexGap;
        int currentPostIndexGapIndex    = 0;
        int currentGap2Index            = 0;
        int currentGap3Index            = 0;
        int currentDataSectorNumber     = 1;

        byte[] IDRecordBytes = new byte[5];
        ushort crcID = 0;
        ushort crcData = 0;
        #endregion

        #region Floppy Write Track Variables enums and functions

        /* X^16 + X^12 + X^5+ 1 */

        ushort[] crc_table = new ushort[]
        {
            0x0000, 0x1021, 0x2042, 0x3063, 0x4084, 0x50a5,
            0x60c6, 0x70e7, 0x8108, 0x9129, 0xa14a, 0xb16b,
            0xc18c, 0xd1ad, 0xe1ce, 0xf1ef, 0x1231, 0x0210,
            0x3273, 0x2252, 0x52b5, 0x4294, 0x72f7, 0x62d6,
            0x9339, 0x8318, 0xb37b, 0xa35a, 0xd3bd, 0xc39c,
            0xf3ff, 0xe3de, 0x2462, 0x3443, 0x0420, 0x1401,
            0x64e6, 0x74c7, 0x44a4, 0x5485, 0xa56a, 0xb54b,
            0x8528, 0x9509, 0xe5ee, 0xf5cf, 0xc5ac, 0xd58d,
            0x3653, 0x2672, 0x1611, 0x0630, 0x76d7, 0x66f6,
            0x5695, 0x46b4, 0xb75b, 0xa77a, 0x9719, 0x8738,
            0xf7df, 0xe7fe, 0xd79d, 0xc7bc, 0x48c4, 0x58e5,
            0x6886, 0x78a7, 0x0840, 0x1861, 0x2802, 0x3823,
            0xc9cc, 0xd9ed, 0xe98e, 0xf9af, 0x8948, 0x9969,
            0xa90a, 0xb92b, 0x5af5, 0x4ad4, 0x7ab7, 0x6a96,
            0x1a71, 0x0a50, 0x3a33, 0x2a12, 0xdbfd, 0xcbdc,
            0xfbbf, 0xeb9e, 0x9b79, 0x8b58, 0xbb3b, 0xab1a,
            0x6ca6, 0x7c87, 0x4ce4, 0x5cc5, 0x2c22, 0x3c03,
            0x0c60, 0x1c41, 0xedae, 0xfd8f, 0xcdec, 0xddcd,
            0xad2a, 0xbd0b, 0x8d68, 0x9d49, 0x7e97, 0x6eb6,
            0x5ed5, 0x4ef4, 0x3e13, 0x2e32, 0x1e51, 0x0e70,
            0xff9f, 0xefbe, 0xdfdd, 0xcffc, 0xbf1b, 0xaf3a,
            0x9f59, 0x8f78, 0x9188, 0x81a9, 0xb1ca, 0xa1eb,
            0xd10c, 0xc12d, 0xf14e, 0xe16f, 0x1080, 0x00a1,
            0x30c2, 0x20e3, 0x5004, 0x4025, 0x7046, 0x6067,
            0x83b9, 0x9398, 0xa3fb, 0xb3da, 0xc33d, 0xd31c,
            0xe37f, 0xf35e, 0x02b1, 0x1290, 0x22f3, 0x32d2,
            0x4235, 0x5214, 0x6277, 0x7256, 0xb5ea, 0xa5cb,
            0x95a8, 0x8589, 0xf56e, 0xe54f, 0xd52c, 0xc50d,
            0x34e2, 0x24c3, 0x14a0, 0x0481, 0x7466, 0x6447,
            0x5424, 0x4405, 0xa7db, 0xb7fa, 0x8799, 0x97b8,
            0xe75f, 0xf77e, 0xc71d, 0xd73c, 0x26d3, 0x36f2,
            0x0691, 0x16b0, 0x6657, 0x7676, 0x4615, 0x5634,
            0xd94c, 0xc96d, 0xf90e, 0xe92f, 0x99c8, 0x89e9,
            0xb98a, 0xa9ab, 0x5844, 0x4865, 0x7806, 0x6827,
            0x18c0, 0x08e1, 0x3882, 0x28a3, 0xcb7d, 0xdb5c,
            0xeb3f, 0xfb1e, 0x8bf9, 0x9bd8, 0xabbb, 0xbb9a,
            0x4a75, 0x5a54, 0x6a37, 0x7a16, 0x0af1, 0x1ad0,
            0x2ab3, 0x3a92, 0xfd2e, 0xed0f, 0xdd6c, 0xcd4d,
            0xbdaa, 0xad8b, 0x9de8, 0x8dc9, 0x7c26, 0x6c07,
            0x5c64, 0x4c45, 0x3ca2, 0x2c83, 0x1ce0, 0x0cc1,
            0xef1f, 0xff3e, 0xcf5d, 0xdf7c, 0xaf9b, 0xbfba,
            0x8fd9, 0x9ff8, 0x6e17, 0x7e36, 0x4e55, 0x5e74,
            0x2e93, 0x3eb2, 0x0ed1, 0x1ef0
        };

        enum WRITE_TRACK_STATES
        {
            WaitForIDRecordMark = 0,
            WaitForIDRecordTrack,
            WaitForIDRecordSide,
            WaitForIDRecordSector,
            WaitForIDRecordSize,
            WaitForIDRecordWriteCRC,
            WaitForDataRecordMark,
            GettingDataRecord,
            GetLastFewBytes
        }

        int currentWriteTrackState = (int)WRITE_TRACK_STATES.WaitForIDRecordMark;
        int writeTrackTrack     = 0;
        int writeTrackSide      = 0;
        int writeTrackSector    = 0;
        int writeTrackSize      = 0;

        int writeTrackMinSector = 0;
        int writeTrackMaxSector = 0;

        byte[] writeTrackWriteBuffer = new byte[65536];
        int writeTrackWriteBufferIndex = 0;

        int writeTrackBytesWrittenToSector  = 0;
        int writeTrackBytesPerSector        = 0;
        int writeTrackBufferOffset          = 0;    // used to put sector data into the track buffer since the sector do not come in order
        int writeTrackBufferSize            = 0;
        int totalBytesThisTrack             = 0;    // initial declaration

        byte previousByte = 0x00;
        int sectorsInWriteTrackBuffer = 0;
        int lastFewBytesRead = 0;
        byte lastSectorAccessed = 1;

        [Conditional("DEBUG")]
        private void LogOffsetCalculations(int nDrive, long offset)
        {
            if (Program.enableFD2ActivityLogChecked)
            {
                using (StreamWriter sw = new StreamWriter(File.Open(Program.activityLogFileFD2, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)))
                {
                    sw.WriteLine(string.Format("drive: {0}, STZ: {1}, SPT: {2}, FMT: {3},  track: {4}, side: {5}, sector: {6}, calculated offset: {7}"
                        , nDrive
                        , Program.SectorsOnTrackZeroSideZero[nDrive].ToString("X2")
                        //, Program.SectorsOnTrackZeroSideOne[nDrive].ToString("X2")
                        , Program.SectorsPerTrack[nDrive].ToString("X2")
                        , Program.FormatByte[nDrive].ToString("x2")
                        , m_nFDCTrack.ToString("X2")
                        , m_nCurrentSideSelected.ToString("X1")
                        , m_nFDCSector.ToString("X2")
                        , offset.ToString("X8")));
                }
            }
            else
                return;
        }

        [Conditional("DEBUG")]
        void LogFloppyWriteTrack(long fileOffset, byte[] writeBuffer, int startIndex, int bytesToTransfer)
        {
            if (Program.enableFD2ActivityLogChecked)
            {
                using (StreamWriter sw = new StreamWriter(File.Open(Program.activityLogFileFD2, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)))
                {
                    sw.WriteLine(string.Format("Writing Track 0x{0}: with starting sector of 0x{1} to file offset 0x{2}", writeTrackTrack.ToString("X2"), writeTrackMinSector.ToString("X2"), fileOffset.ToString("X8")));
                    string asciiBytes = "";

                    for (int i = startIndex; i < bytesToTransfer; i++)
                    {
                        if ((i % 32) == 0)                                          // output index as X4
                        {
                            if (i != 0)
                            {
                                // output ascii

                                sw.Write(asciiBytes);
                                asciiBytes = "";

                                // and new line

                                sw.Write("\n");
                            }

                            sw.Write(string.Format("{0} ", i.ToString("X4")));
                        }

                        if (((i % 4) == 0) && ((i % 32) != 0))                      // output a space between every 4 bytes
                            sw.Write(" ");

                        if (((i % 8) == 0) && ((i % 32) != 0))                      // output another space between every 8 bytes
                            sw.Write(" ");

                        sw.Write(string.Format("{0} ", writeBuffer[i].ToString("X2")));

                        if ((writeBuffer[i] >= ' ') && writeBuffer[i] <= 0x7E)
                            asciiBytes += (char)writeBuffer[i];
                        else
                            asciiBytes += ".";
                    }

                    if (asciiBytes.Length > 0)
                        sw.Write(asciiBytes.ToString());

                    sw.Write("\n");
                    sw.Write("\n");
                }

            }
        }

        [Conditional("DEBUG")]
        void LogFloppyWrite(long fileOffset, string message, int bytesToTransfer)
        {
            if (Program.enableFD2ActivityLogChecked)
            {
                using (StreamWriter sw = new StreamWriter(File.Open(Program.activityLogFileFD2, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)))
                {
                    sw.WriteLine(string.Format("Writing: {0} (0x{1}) bytes - {2}", bytesToTransfer, bytesToTransfer.ToString("X4"), message));
                }
            }
        }

        [Conditional("DEBUG")]
        void LogFloppyWrite(long fileOffset, byte[] writeBuffer, int startIndex, int bytesToTransfer)
        {
            if (Program.enableFD2ActivityLogChecked)
            {
                using (StreamWriter sw = new StreamWriter(File.Open(Program.activityLogFileFD2, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)))
                {
                    sw.WriteLine("Writing: ");
                    string asciiBytes = "";

                    for (int i = startIndex; i < bytesToTransfer; i++)
                    {
                        if ((i % 32) == 0)                                          // output index as X4
                        {
                            if (i != 0)
                            {
                                // output ascii

                                sw.Write(asciiBytes);
                                asciiBytes = "";

                                // and new line

                                sw.Write("\n");
                            }

                            sw.Write(string.Format("{0} ", (i + fileOffset).ToString("X8")));
                        }

                        if (((i % 4) == 0) && ((i % 32) != 0))                      // output a space between every 4 bytes
                            sw.Write(" ");

                        if (((i % 8) == 0) && ((i % 32) != 0))                      // output another space between every 8 bytes
                            sw.Write(" ");

                        sw.Write(string.Format("{0} ", writeBuffer[i].ToString("X2")));

                        if ((writeBuffer[i] >= ' ') && writeBuffer[i] <= 0x7E)
                            asciiBytes += (char)writeBuffer[i];
                        else
                            asciiBytes += ".";
                    }

                    if (asciiBytes.Length > 0)
                        sw.Write(asciiBytes.ToString());

                    sw.Write("\n");
                    sw.Write("\n");
                }
            }
        }

        [Conditional("DEBUG")]
        void LogFloppyRead(byte[] readBuffer, int startIndex, int bytesTransferred)
        {
            if (Program.enableFD2ActivityLogChecked)
            {
                using (StreamWriter sw = new StreamWriter(File.Open(Program.activityLogFileFD2, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)))
                {
                    sw.WriteLine("Reading: ");
                    string asciiBytes = "";

                    for (int i = startIndex; i < bytesTransferred; i++)
                    {
                        if ((i % 32) == 0)                                          // output index as X4
                        {
                            if (i != 0)
                            {
                                // output ascii

                                sw.Write(asciiBytes);
                                asciiBytes = "";

                                // and new line

                                sw.Write("\n");
                            }

                            sw.Write(string.Format("{0} ", (i + m_lFileOffset).ToString("X8")));
                        }

                        if (((i % 4) == 0) && ((i % 32) != 0))                      // output a space between every 4 bytes
                            sw.Write(" ");

                        if (((i % 8) == 0) && ((i % 32) != 0))                      // output another space between every 8 bytes
                            sw.Write(" ");

                        sw.Write(string.Format("{0} ", readBuffer[i].ToString("X2")));

                        if ((readBuffer[i] >= ' ') && readBuffer[i] <= 0x7E)
                            asciiBytes += (char)readBuffer[i];
                        else
                            asciiBytes += ".";
                    }

                    if (asciiBytes.Length > 0)
                        sw.Write(asciiBytes.ToString());

                    sw.Write("\n");
                    sw.Write("\n");
                }
            }
        }

        bool logActivityReads = false;
        bool logActivityWrites= true;

        [Conditional("DEBUG")]
        private void LogFloppyActivity(int whichRegister, byte value, bool read = true)
        {
            if (Program.enableFD2ActivityLogChecked)
            {
                if ((logActivityReads && read) || (logActivityWrites && !read))
                {
                    LogActivityValues thisLogActivityValues = new LogActivityValues();
                    thisLogActivityValues.whichRegister = whichRegister;
                    thisLogActivityValues.value = value;
                    thisLogActivityValues.read = read;

                    bool thisIsTheFirst = false;
                    bool logIt = false;

                    if (previousLogActivity == null)
                    {
                        previousLogActivity = new LogActivityValues();
                        previousLogActivity.whichRegister = whichRegister;
                        previousLogActivity.value = value;
                        previousLogActivity.read = read;

                        thisIsTheFirst = true;
                        logIt = true;
                    }
                    else
                    {
                        // if this is not the first entry - do not check for duplicate

                        if (!thisIsTheFirst)
                        {
                            if (previousLogActivity.whichRegister == thisLogActivityValues.whichRegister && previousLogActivity.value == thisLogActivityValues.value && previousLogActivity.read == thisLogActivityValues.read)
                            {
                                // this is a duplicate - increment the counter
                                duplicateCount++;
                                if (duplicateCount >= 100000)
                                {
                                    // print it anyway
                                    using (StreamWriter sw = new StreamWriter(File.Open(Program.activityLogFileFD2, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)))
                                    {
                                        sw.WriteLine(string.Format(" The above line was duplicated {0} times", duplicateCount));

                                        previousLogActivity.whichRegister = whichRegister;
                                        previousLogActivity.value = value;
                                        previousLogActivity.read = read;

                                        duplicateCount = 0;
                                    }
                                }
                            }
                            else
                            {
                                if (duplicateCount != 0)
                                {
                                    // we have had duplicates - show it

                                    using (StreamWriter sw = new StreamWriter(File.Open(Program.activityLogFileFD2, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)))
                                    {
                                        sw.WriteLine(string.Format(" The above line was duplicated {0} time", duplicateCount));

                                        previousLogActivity.whichRegister = whichRegister;
                                        previousLogActivity.value = value;
                                        previousLogActivity.read = read;

                                        duplicateCount = 0;
                                    }
                                }
                                else
                                {
                                    previousLogActivity.whichRegister = whichRegister;
                                    previousLogActivity.value = value;
                                    previousLogActivity.read = read;

                                    logIt = true;
                                }
                            }
                        }
                    }

                    if (logIt)
                    {
                        using (StreamWriter sw = new StreamWriter(File.Open(Program.activityLogFileFD2, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)))
                        {
                            string description = "Unknown";

                            if (registerDescription.ContainsKey(whichRegister))
                            {
                                description = registerDescription[whichRegister];
                            }
                            sw.WriteLine(string.Format("{0}  {1} - {2} {3} (0x{4}) - {5}", Program._cpu.CurrentIP.ToString("X4"), read ? "read " : "write", value.ToString("X2"), read ? "from" : "to  ", whichRegister.ToString("X4"), description));
                        }
                    }
                }
            }
        }

        // uses the values in track and sector regists alomg with floppy format to calculate the offset to any sector
        //
        //      keep in mind that FLEX and UbiFLEX sectors start at 1 except for track 0 which has a sector 0 and no sector 1. 

        // used by read track to calculate ID and Data CRC values

        ushort CRCCCITT(byte[] data, int startIndex, int length, ushort seed, ushort final)
        {

            int count;
            uint crc = seed;
            uint temp;
            int dataindex = startIndex;

            for (count = 0; count < length; ++count)
            {
                temp = (data[dataindex++] ^ (crc >> 8)) & 0xff;
                crc = crc_table[temp] ^ (crc << 8);
            }

            return (ushort)(crc ^ final);
        }

        public void CalcFileOffset(int nDrive)
        {
            if (Program.DiskFormat[nDrive] == DiskFormats.DISK_FORMAT_UNKNOWN)
            {
                // this is only used if the file format is unknown when entering CalcFileOffset

                VirtualFloppyManipulationRoutines virtualFloppyManipulationRoutines = new VirtualFloppyManipulationRoutines(Program.DriveImagePaths[nDrive], Program.FloppyDriveStream[nDrive]);

                fileformat ff = virtualFloppyManipulationRoutines.GetFileFormat(Program.FloppyDriveStream[nDrive]);
                Program.LoadDrives();
            }

            if ((m_nFDCTrack == 0) && (m_nFDCSector == 0) && (m_nCurrentSideSelected == 0))
                m_lFileOffset = 0L;
            else
            {
                if (Program.DiskFormat[nDrive] == DiskFormats.DISK_FORMAT_MINIFLEX)
                {
                    m_lFileOffset = (long)((m_nFDCTrack * Program.SectorsPerTrack[nDrive]) + (m_nFDCSector - 1)) * 128L;
                }
                else if ((Program.DiskFormat[nDrive] == DiskFormats.DISK_FORMAT_FLEX) || (Program.DiskFormat[nDrive] == DiskFormats.DISK_FORMAT_UNIFLEX))
                {
                    m_lFileOffset = (long)((m_nFDCTrack * Program.SectorsPerTrack[nDrive]) + (m_nFDCSector - 1)) * 256L;
                }
                else if (Program.DiskFormat[nDrive] == DiskFormats.DISK_FORMAT_FLEX_IMA)
                {
                    if (m_nFDCTrack == 0)
                    {
                        // these are ALWAYS single density sectors for IMA format
                        //
                        //  we will not get here on track = 0 and sector = 0, so there is
                        //  no need to check for that. In here we always subtract 1 from
                        //  the sector number to make it zero based

                        m_lFileOffset = (long)((m_nFDCSector - 1) * 256);
                    }
                    else
                    {
                        // if this image is  formatted as an IMA image, then track zero will have
                        // either 10 or 20 sectors depending on whether it is single or double
                        // sided. We can tell if it is single or double sided if the maximum
                        // number of sectors is > 10
                        //
                        //  valid IMA formats:
                        //
                        //  tracks      sides   density     sectors/track   filesize                    blank filename
                        //  --------    -----   -------     -------------   --------------------------  --------------
                        //  35          1       single      10              35 * 1 * 10 * 256 =  89600  SSSD35T.IMA
                        //  35          2       single      20              35 * 2 * 10 * 256 = 179200  DSSD35T.IMA
                        //  35          1       double      18       2560 + 34 * 1 * 18 * 256 = 159232  SSDD35T.IMA
                        //  35          2       double      36       5120 + 34 * 2 * 18 * 256 = 318464  DSDD35T.IMA
                        //  40          1       single      10              40 * 1 * 10 * 256 = 102400  SSSD40T.IMA
                        //  40          2       single      20              40 * 2 * 10 * 256 = 204800  DSSD40T.IMA <-
                        //  40          1       double      18       2560 + 39 * 1 * 18 * 256 = 182272  SSDD40T.IMA
                        //  40          2       double      36       5120 + 39 * 2 * 18 * 256 = 364544  DSDD40T.IMA
                        //  80          1       single      10              80 * 1 * 10 * 256 = 204800  SSSD80T.IMA <-
                        //  80          2       single      20              80 * 2 * 10 * 256 = 409600  DSSD80T.IMA
                        //  80          1       double      18       2560 + 79 * 1 * 18 * 256 = 366592  SSDD80T.IMA
                        //  80          2       double      36       5120 + 79 * 2 * 18 * 256 = 733184  DSDD80T.IMA
                        //
                        //      Since these are the only valid IMA formats - we can assume double sided if the max sectors for
                        //  this images is > 18 (because sectors are 1 based not 0)
                        //
                        //  80 track single sides single density is the same size as 40 track double sided single density. The 
                        //  only way to tell the difference is by the max sectors per track. If it's 10 - this is single sided
                        //  single density 80 track otherwise it is double sided single density

                        int sectorBias = 1;
                        switch (Program.SectorsPerTrack[nDrive])
                        {
                            // these are the only valid max sector values for IMA (10 or 18 = single side, 20 or 36 = double sided)

                            case 10:
                                m_lFileOffset = ((((m_nFDCTrack - 1) * (Program.SectorsPerTrack[nDrive])) + (m_nFDCSector - sectorBias)) * 256) + (256 * 10);
                                break;
                            case 20:
                                m_lFileOffset = ((((m_nFDCTrack - 1) * (Program.SectorsPerTrack[nDrive])) + (m_nFDCSector - sectorBias)) * 256) + (256 * 20);
                                break;
                            case 18:
                                m_lFileOffset = ((((m_nFDCTrack - 1) * (Program.SectorsPerTrack[nDrive])) + (m_nFDCSector - sectorBias)) * 256) + (256 * 10);
                                break;
                            case 36:
                                m_lFileOffset = ((((m_nFDCTrack - 1) * (Program.SectorsPerTrack[nDrive])) + (m_nFDCSector - sectorBias)) * 256) + (256 * 20);
                                break;
                        }
                    }
                }
                else
                {
                    if (nDrive == 2)
                    {
                        int x = 1;
                    }

                    m_lFileOffset = 0;

                    int nSPT = Program.SectorsPerTrack[nDrive];

                    //  OS9 ALWAYS has m_nSectorsOnTrackZero sectors on track 0 side 0
                    //  and uses the side select before seeking to another cylinder.
                    //  track 0 side 1 has m_nSectorsPerTrack sectors. The sectors do
                    //  not start over when switching sides. if there are 16 sectors
                    //  per track, the each cylinder has sectors 0 - #1F (32 sectors).

                    VirtualFloppyManipulationRoutines.PhysicalOS9Geometry geometry = Program.VirtualFloppyManipulationRoutines[nDrive].physicalParameters; // this gets set on drive load in Program

                    switch (m_nFDCTrack)
                    {
                        // track 0

                        case 0:
                            switch (m_nCurrentSideSelected)
                            {
                                case 0:
                                    {
                                        int extraSectors = 0;
                                        if (m_nFDCSector > 10)
                                        {
                                            if (Program.SectorsOnTrackZeroSideZero[nDrive] != 10)
                                                extraSectors = Program.SectorsPerTrack[nDrive] - 10;
                                        }
                                        m_lFileOffset = (long)(m_nFDCSector) * 256L;
                                    }
                                    break;

                                default:
                                    {
                                        // if we are not on track 0 side 0, the track and sector calculated by the FD2 OS-9 driver may be
                                        // incorrect if this image does not have 10 sectors per track on track 0. If this is the case we
                                        // need to adjust the offset by the number of sectors that are over 10

                                        int extraSectors = 0;
                                        if (Program.SectorsOnTrackZeroSideZero[nDrive] != 10)
                                            extraSectors = Program.SectorsPerTrack[nDrive] - 10;

                                        m_lFileOffset = ((long)(Program.SectorsOnTrackZeroSideZero[nDrive] + (long)m_nFDCSector) * 256L) - (extraSectors * 256);
                                    }
                                    break;
                            }
                            break;

                        //case 1:
                        //    switch (m_nCurrentSideSelected)
                        //    {
                        //        case 0:
                        //            if (geometry.doubleSided)   // double sided
                        //            {
                        //                // take care of track 0
                        //                m_lFileOffset = (Program.SectorsOnTrackZeroSideZero[nDrive] + Program.SectorsOnTrackZeroSideOne[nDrive]) * 256L;

                        //                // now this track
                        //                m_lFileOffset += m_nFDCSector * 256L;

                        //            }
                        //            else                            // single sided
                        //            {
                        //                m_lFileOffset = Program.SectorsOnTrackZeroSideZero[nDrive] * 256L;
                        //                m_lFileOffset += m_nFDCSector * 256L;
                        //            }
                        //            break;

                        //        case 1:             // if the side selected is 1 then this has to be double sided so no need to check format byte

                        //            // take care of track 0
                        //            m_lFileOffset = (Program.SectorsOnTrackZeroSideZero[nDrive] + Program.SectorsOnTrackZeroSideOne[nDrive]) * 256L;

                        //            // now this track
                        //            m_lFileOffset += (long)(nSPT + m_nFDCSector) * 256L;
                        //            break;
                        //    }
                        //    break;

                        // not track 0 

                        default:
                            {
                                switch (m_nCurrentSideSelected)
                                {
                                    case 0:
                                        {
                                            if (nDrive == 2)
                                            {
                                                int x = 1;
                                            }

                                            if (geometry.doubleSided)   // double sided
                                            {
                                                int extraSectors = 0;
                                                if (Program.SectorsOnTrackZeroSideZero[nDrive] != 10)
                                                    extraSectors = (Program.SectorsPerTrack[nDrive] - 10);

                                                // take care of track 0
                                                m_lFileOffset = ((Program.SectorsOnTrackZeroSideZero[nDrive] + Program.SectorsOnTrackZeroSideOne[nDrive]) * 256L) - (extraSectors * 256);

                                                // now all of the other tracks up to but not including this this track
                                                m_lFileOffset += (long)(((m_nFDCTrack - 1) * nSPT * 2)) * 256L;

                                                // now this track
                                                m_lFileOffset += m_nFDCSector * 256L;

                                            }
                                            else                            // single sided
                                            {
                                                int extraSectors = 0;
                                                if (Program.SectorsOnTrackZeroSideZero[nDrive] != 10)
                                                    extraSectors = (Program.SectorsPerTrack[nDrive] - 10);

                                                m_lFileOffset = (Program.SectorsOnTrackZeroSideZero[nDrive] * 256L) - (extraSectors * 256);
                                                m_lFileOffset += (long)(((m_nFDCTrack - 1) * nSPT) + m_nFDCSector) * 256L;
                                            }
                                        }
                                        break;

                                    case 1:             // if the side selected is 1 then this has to be double sided so no need to check format byte
                                        {
                                            if (nDrive == 2)
                                            {
                                                int x = 1;
                                            }
                                            int extraSectors = 0;
                                            if (Program.SectorsOnTrackZeroSideZero[nDrive] != 10)
                                                extraSectors = Program.SectorsPerTrack[nDrive] - 10;

                                            // take care of track 0
                                            m_lFileOffset = ((Program.SectorsOnTrackZeroSideZero[nDrive] + Program.SectorsOnTrackZeroSideOne[nDrive]) * 256L) - (extraSectors * 256);

                                            // now all of the other tracks up to but not including this this track
                                            m_lFileOffset += (long)((m_nFDCTrack - 1) * nSPT * 2) * 256L;

                                            // now this track
                                            m_lFileOffset += (nSPT + m_nFDCSector) * 256L;
                                        }
                                        break;
                                }
                            }
                            break;
                    }
                }
            }
            LogOffsetCalculations(nDrive, m_lFileOffset);
        }

        private void WriteTrackToImage(int nDrive)
        {
            // There is data in the m_caWriteBuffer that needs to written to the diskette image. To do this we need to use the
            // information in the writeTrackID record to figure out where to write this buffer to.

            // writeTrackBufferSize is equal to how much data we need to save from m_caWriteBuffer

            // calculate where to save it. The offset into the files is 
            //
            //      first save the m_nFDCTrack and the m_nFDCSector because we need to 
            //      temporarily modify them to call CalcFileOffset

            int saveFDCTrack = m_nFDCTrack;
            int saveFDCSector = m_nFDCSector;

            m_nFDCTrack = writeTrackTrack;

            // Since CalcFileOffset depends on the FLEX convention of just incrementing the sector number
            // across sides and let's the disk driver sort out which side it is on, We need to be able to
            // interject this rule here.

            m_nFDCSector = writeTrackMinSector;

            CalcFileOffset(nDrive);
            LogFloppyWrite(m_lFileOffset, "writing Track", writeTrackWriteBufferIndex);

            // m_lFileOffset is now equal to the starting offset into the diskette image file of this track
            // so we can now save it to the file.

            int bytesToWrite = totalBytesThisTrack;

            if (Program.FloppyDriveStream[nDrive] != null)
            {
                Program.FloppyDriveStream[nDrive].Seek(m_lFileOffset, SeekOrigin.Begin);

                int caBufferOffset = 0;
                int writeTrackWriteBufferOffset = 0;

                if (m_nCurrentSideSelected == 1)
                {
                    caBufferOffset = writeTrackBufferSize - totalBytesThisTrack;
                }

                Program.FloppyDriveStream[nDrive].Write(m_caWriteBuffer, caBufferOffset, totalBytesThisTrack);

                LogFloppyWriteTrack(m_lFileOffset, writeTrackWriteBuffer, writeTrackWriteBufferOffset, writeTrackWriteBufferIndex);

                Program.FloppyDriveStream[nDrive].Flush();

                m_statusReadsWithoutDataRegisterAccess = 0;     // clear it for the next operation
                writeTrackBufferSize = 0;
                //totalBytesThisTrack = 0;    // reset after writing track to image file
                writeTrackWriteBufferIndex = 0;

                writeTrackMinSector = 0xff;
                writeTrackMaxSector = 0;
            }

            m_nFDCTrack = saveFDCTrack;
            m_nFDCSector = saveFDCSector;

            // and then we need to turn off writing track and writing and set the FDC stats to finished.

            lock (Program._cpu.buildingDebugLineLock)
            {
                m_FDC_STATRegister &= (byte)~FDC_BUSY;     // clear BUSY if data not read
                //activityLedColor = (int)ActivityLedColors.greydot;
                //Program._mainForm.SetFloppyActivity(nDrive, global::SWTPCemuApp.Properties.Resources.greydot);
                ClearInterrupt();
            }

            //totalBytesThisTrack = 0;    // reset before leaving routine to write track to image file
            m_nWritingTrack = false;
        }

        #endregion

        private bool CheckReady(int nDrive)
        {
            if (nDrive == 2)
            {
                int x = 1;
            }
            if (Program.DriveOpen[nDrive] == true || Program.FloppyDriveStream[nDrive] == null) // see if current drive is READY
                m_FDC_STATRegister |= (byte)FDC_NOTREADY;
            else
                m_FDC_STATRegister &= (byte)~FDC_NOTREADY;

            return (m_FDC_STATRegister & (byte)FDC_NOTREADY) == 0 ? true : false;
        }

        private bool CheckWriteProtect(int nDrive)
        {
            if (Program.WriteProtected[nDrive] == true)          // see if write protected
                m_FDC_STATRegister |= (byte)FDC_WRTPROTECT;
            else
                m_FDC_STATRegister &= (byte)~FDC_WRTPROTECT;

            return (m_FDC_STATRegister & (byte)FDC_WRTPROTECT) == 0 ? true : false;
        }

        private bool CheckReadyAndWriteProtect(int nDrive)
        {
            bool ready = CheckReady(nDrive);
            bool writeProtected = CheckWriteProtect(nDrive);

            return ready | writeProtected;
        }

        public override void Write (ushort m, byte b)
        {
            int nDrive         = m_FDC_DRVRegister & 0x03;
            int nType          = 1;
            int nWhichRegister = m - m_sBaseAddress;
            bool bMultisector;

            int activityLedColor = (int)ActivityLedColors.greydot;

            ClearInterrupt();

            bool driveReady = CheckReady(nDrive);
            bool writeProtected = CheckWriteProtect(nDrive);

            switch (nWhichRegister)
            {
                case (int)FDCRegisterOffsets.FDC_DATAREG_OFFSET:
                    {
                        m_FDC_DATARegister = b;
                        if (((m_FDC_STATRegister & FDC_BUSY) == FDC_BUSY) && m_nFDCWriting)
                        {
                            m_statusReadsWithoutDataRegisterAccess = 0;

                            if (!m_nWritingTrack)
                            { 
                                m_caWriteBuffer[m_nFDCWritePtr++] = b;
                                if (m_nFDCWritePtr == m_nBytesToTransfer)
                                {
                                    // Here is where we will seek and write dsk file

                                    CalcFileOffset(nDrive);

                                    if (Program.FloppyDriveStream[nDrive] != null)
                                    {
                                        Program.FloppyDriveStream[nDrive].Seek(m_lFileOffset, SeekOrigin.Begin);
                                        Program.FloppyDriveStream[nDrive].Write(m_caWriteBuffer, 0, m_nBytesToTransfer);

                                        LogFloppyWrite(m_lFileOffset, m_caWriteBuffer, 0, m_nBytesToTransfer);

                                        Program.FloppyDriveStream[nDrive].Flush();
                                    }

                                    m_FDC_STATRegister &= (byte)~(FDC_DRQ | FDC_BUSY);
                                    m_FDC_DRVRegister &= (byte)~DRV_DRQ;                // turn off high order bit in drive status register
                                    m_FDC_DRVRegister |= (byte)DRV_INTRQ;               // assert INTRQ at the DRV register when the operation is complete
                                    m_nFDCWriting = false;

                                    ClearInterrupt();
                                    activityLedColor = (int)ActivityLedColors.greydot;
                                }
                                else
                                {
                                    m_FDC_STATRegister |= (byte)FDC_DRQ;
                                    m_FDC_DRVRegister  |= (byte)DRV_DRQ;

                                    if (_bInterruptEnabled && (m_FDC_DRVRegister & (byte)DRV_INTRQ) == (byte)DRV_INTRQ)
                                    {
                                        SetInterrupt(_spin);
                                        if (Program._cpu != null)
                                        {
                                            if ((Program._cpu.InWait || Program._cpu.InSync) && Program.CpuThread.ThreadState == System.Threading.ThreadState.Suspended)
                                            {
                                                try
                                                {
                                                    Program.CpuThread.Resume();
                                                }
                                                catch (ThreadStateException e)
                                                {
                                                    // do nothing if thread is not suspended
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // this is where we will use a state machine to grab the track, side and sector information from the
                                // incoming data stream by detecting and parsing the ID Record and then wait for the data record to
                                // start building the track's worth of data to save to the image.

                                //      300 rpm SD      3125 bytes
                                //      300 rpm DD/QD   6250 bytes
                                //      360 rpm SD      5208 bytes (e.g., 8" drive)
                                //      360 rpm DD     10417 bytes (e.g., 8" drive)

                                // first a sanity check to keep from going on forever and ever

                                if (writeTrackWriteBufferIndex > 10417)
                                {
                                    // we should be done at this point so we need to write the track to the diskette image in case the
                                    // emulated host does not check the BUSY or DRQ for writing the next byte. Some emulated machine
                                    // programs that use write track do not poll the DRQ or BUSY bits - they just keep sending data
                                    // until an an interrupt occurs when the index pulse is detected. For these - we need to terminate
                                    // the write track operation by raising the IRQ ourselces. Since the status register is not 
                                    // being polled (which is where we would write the track to the image), we need to do it here

                                    m_statusReadsWithoutDataRegisterAccess = 1000;  // make it big enough to trigger the end

                                    // write the track to the image and clear everything so the next poll of the status register does not write anything.

                                    WriteTrackToImage(nDrive);

                                    if (_bInterruptEnabled && (m_FDC_DRVRegister & (byte)DRV_INTRQ) == (byte)DRV_INTRQ)
                                    {
                                        SetInterrupt(_spin);
                                        if (Program._cpu != null)
                                        {
                                            if ((Program._cpu.InWait || Program._cpu.InSync) && Program.CpuThread.ThreadState == System.Threading.ThreadState.Suspended)
                                            {
                                                try
                                                {
                                                    Program.CpuThread.Resume();
                                                }
                                                catch (ThreadStateException e)
                                                {
                                                    // do nothing if thread is not suspended
                                                }
                                            }
                                        }
                                    }
                                    writeTrackWriteBufferIndex = 0;     // rest it for next time
                                }
                                else
                                {
                                    // not at max number of bytes for an 8" DD track - so check if this track is finished.

                                    if (writeTrackTrack == 0)
                                    {
                                        // track zero for an FD-2 is only allowed to have FM data (single density. that is 10 sectors

                                        if (sectorsInWriteTrackBuffer == 10)        // Program.SectorsOnTrackZeroSideZero[nDrive])
                                        {
                                            // we just got the last 0xF7 to write the data CRC for the last sector, so let's get a few more bytes before we say we are complete

                                            currentWriteTrackState = (int)WRITE_TRACK_STATES.GetLastFewBytes;

                                            if (lastFewBytesRead == 7)
                                            {
                                                m_statusReadsWithoutDataRegisterAccess = 1000;  // make it big enough to trigger the end

                                                // write the track to the image and clear everything so the next poll of the status register does not write anything.

                                                WriteTrackToImage(nDrive);
                                                currentWriteTrackState = (int)WRITE_TRACK_STATES.WaitForIDRecordMark;

                                                if (_bInterruptEnabled && (m_FDC_DRVRegister & (byte)DRV_INTRQ) == (byte)DRV_INTRQ)
                                                {
                                                    SetInterrupt(_spin);
                                                    if (Program._cpu != null)
                                                    {
                                                        if ((Program._cpu.InWait || Program._cpu.InSync) && Program.CpuThread.ThreadState == System.Threading.ThreadState.Suspended)
                                                        {
                                                            try
                                                            {
                                                                Program.CpuThread.Resume();
                                                            }
                                                            catch (ThreadStateException e)
                                                            {
                                                                // do nothing if thread is not suspended
                                                            }
                                                        }
                                                    }
                                                }
                                                writeTrackWriteBufferIndex = 0;     // rest it for next time
                                            }
                                        }
                                    }
                                    else
                                    {
                                        //  here we need to use the sectors per track for this media that was read from offset 0x0227 in the image
                                        //  when the diskette image was loaded by Program.LoadDrives
                                        //
                                        //      For real media for the FD-2 controller these are the only legitimate sectors sizes
                                        //      on a 5 1/4" or 3 1/2" drive:
                                        //
                                        //              Track 0     NON Track zero  Program.SectorsPerTrack[nDrive];
                                        //      SSSD    10          10              10
                                        //      SSDD    10          18              18
                                        //      DSSD    20          10              20
                                        //      DSDD    20          18              36
                                        //

                                        int numberOfSectorsPerCylinder = Program.SectorsPerTrack[nDrive];
                                        int numberOfSectorsPerTrack = numberOfSectorsPerCylinder;

                                        if (numberOfSectorsPerTrack >= 20)
                                        {
                                            // then this is a double sided diskette image so we will only be writing
                                            // half the number of sectors specified in Program.SectorsPerTrack[nDrive]

                                            numberOfSectorsPerTrack = numberOfSectorsPerTrack / 2;
                                        }

                                        if (sectorsInWriteTrackBuffer == numberOfSectorsPerTrack)
                                        {
                                            // we just got the last 0xF7 to write the data CRC for the last sector, so let's get a few more bytes before we say we are complete

                                            currentWriteTrackState = (int)WRITE_TRACK_STATES.GetLastFewBytes;

                                            if (lastFewBytesRead == 7)
                                            {
                                                m_statusReadsWithoutDataRegisterAccess = 1000;  // make it big enough to trigger the end

                                                // write the track to the image and clear everything so the next poll of the status register does not write anything.

                                                WriteTrackToImage(nDrive);
                                                currentWriteTrackState = (int)WRITE_TRACK_STATES.WaitForIDRecordMark;

                                                if (_bInterruptEnabled && (m_FDC_DRVRegister & (byte)DRV_INTRQ) == (byte)DRV_INTRQ)
                                                {
                                                    SetInterrupt(_spin);
                                                    if (Program._cpu != null)
                                                    {
                                                        if ((Program._cpu.InWait || Program._cpu.InSync) && Program.CpuThread.ThreadState == System.Threading.ThreadState.Suspended)
                                                        {
                                                            try
                                                            {
                                                                Program.CpuThread.Resume();
                                                            }
                                                            catch (ThreadStateException e)
                                                            {
                                                                // do nothing if thread is not suspended
                                                            }
                                                        }
                                                    }
                                                }
                                                writeTrackWriteBufferIndex = 0;     // rest it for next time
                                            }
                                        }
                                    }
                                }

                                switch (currentWriteTrackState)
                                {
                                    case (int)WRITE_TRACK_STATES.WaitForIDRecordMark:
                                        {
                                            writeTrackWriteBuffer[writeTrackWriteBufferIndex++] = b;
                                            if (b == 0xFE)
                                            {
                                                // we are about to write the IDRecord mark, so get ready to save the track, side, sector and size information

                                                currentWriteTrackState = (int)WRITE_TRACK_STATES.WaitForIDRecordTrack;
                                            }
                                        }
                                        break;

                                    case (int)WRITE_TRACK_STATES.WaitForIDRecordTrack:
                                        {
                                            // we will only allow real diskette image number of tracks (up to 80 tracks)

                                            writeTrackWriteBuffer[writeTrackWriteBufferIndex++] = b;
                                            if (b < 80)
                                            {
                                                // this is a valid max track size

                                                writeTrackTrack = b;
                                                currentWriteTrackState = (int)WRITE_TRACK_STATES.WaitForIDRecordSide;
                                            }
                                            else
                                            {
                                                // if the track specified is > 79 then reset the state machine to look for another ID record mark

                                                currentWriteTrackState = (int)WRITE_TRACK_STATES.WaitForIDRecordMark;
                                            }
                                        }
                                        break;

                                    case (int)WRITE_TRACK_STATES.WaitForIDRecordSide:
                                        {
                                            // we will only allow real diskette image number of sides (0 or 1)

                                            writeTrackWriteBuffer[writeTrackWriteBufferIndex++] = b;
                                            if (b <= 1)
                                            {
                                                writeTrackSide = b;
                                                currentWriteTrackState = (int)WRITE_TRACK_STATES.WaitForIDRecordSector;
                                            }
                                            else
                                            {
                                                // if the side specified is > 1 then reset the state machine to look for another ID record mark

                                                currentWriteTrackState = (int)WRITE_TRACK_STATES.WaitForIDRecordMark;
                                            }
                                        }
                                        break;

                                    case (int)WRITE_TRACK_STATES.WaitForIDRecordSector:
                                        {
                                            // we will only allow real diskette image max number of sectors per track (up to 52 for 8" DD)

                                            writeTrackWriteBuffer[writeTrackWriteBufferIndex++] = b;
                                            if (b <= 52)
                                            {
                                                writeTrackSector = b;
                                                currentWriteTrackState = (int)WRITE_TRACK_STATES.WaitForIDRecordSize;

                                                // we will need these values to write the track to the image.

                                                if (b < writeTrackMinSector)
                                                    writeTrackMinSector = b;
                                                if (b > writeTrackMaxSector)
                                                    writeTrackMaxSector = b;
                                            }
                                            else
                                            {
                                                // if the sector specified is > 52 then reset the state machine to look for another ID record mark

                                                currentWriteTrackState = (int)WRITE_TRACK_STATES.WaitForIDRecordMark;
                                            }
                                        }
                                        break;

                                    case (int)WRITE_TRACK_STATES.WaitForIDRecordSize:
                                        {
                                            // we will only allow real diskette image record sizes (0, 1, 2, 3)

                                            writeTrackWriteBuffer[writeTrackWriteBufferIndex++] = b;
                                            if (b <= 3)
                                            {
                                                writeTrackSize = b;
                                                currentWriteTrackState = (int)WRITE_TRACK_STATES.WaitForIDRecordWriteCRC;
                                            }
                                            else
                                            {
                                                // if the sector record size is > 3 then reset the state machine to look for another ID record mark

                                                currentWriteTrackState = (int)WRITE_TRACK_STATES.WaitForIDRecordMark;
                                            }
                                        }
                                        break;

                                    case (int)WRITE_TRACK_STATES.WaitForIDRecordWriteCRC:
                                        {
                                            // this next byte MUST be 0xF7 to write CRC

                                            writeTrackWriteBuffer[writeTrackWriteBufferIndex++] = b;
                                            if (b == 0xF7)
                                            {
                                                currentWriteTrackState = (int)WRITE_TRACK_STATES.WaitForDataRecordMark;
                                            }
                                            else
                                            {
                                                // if the write CRC byte is not 0xF7 then reset the state machine to look for another ID record mark

                                                currentWriteTrackState = (int)WRITE_TRACK_STATES.WaitForIDRecordMark;
                                            }
                                        }
                                        break;

                                    case (int)WRITE_TRACK_STATES.WaitForDataRecordMark:
                                        {
                                            writeTrackWriteBuffer[writeTrackWriteBufferIndex++] = b;
                                            if (b == 0xFB)
                                            {
                                                // we have received the Data Record Mark. prepare to receive 2^size * 128 bytes of data
                                                currentWriteTrackState = (int)WRITE_TRACK_STATES.GettingDataRecord;

                                                // we are going to build the track in the m_caWriteBuffer. We need to use the sector number (FLEX sectors start at 1 except for track 0
                                                // which starts at sector 0 and has no sector 1.
                                                //
                                                //  Let's do this calcuation here so we only have to do it once for each sector. We can only write to one head at a time
                                                //  so we need to figure out which side this is on so we can adjust the sector number we use for calculating the offset.
                                                //  If the track is - and the sector number is > 10, we are doing the second side so we need to subtract 10 fomr the sector
                                                //  number to calculate the buffer offset (writeTrackBufferOffset). This value will also be used to set the number of bytes
                                                //  to write to the file when this track is written to the image. If the track is not track 0, we need to subtract the number
                                                //  of sectors per side for the second side.

                                                int sector = writeTrackSector;
                                                if (writeTrackTrack == 0 && writeTrackSector == 0)
                                                {
                                                    // adjust track 0 sector 0, because we are going to subtract 1 from it later, so it CANNOT be zero

                                                    sector = 1;
                                                }

                                                writeTrackBytesPerSector = 128 * (1 << writeTrackSize);

                                                writeTrackBufferOffset = writeTrackBytesPerSector * (sector - 1);
                                                writeTrackBytesWrittenToSector = 0;
                                            }
                                        }
                                        break;

                                    case (int)WRITE_TRACK_STATES.GettingDataRecord:
                                        {
                                            writeTrackWriteBuffer[writeTrackWriteBufferIndex++] = b;
                                            if (writeTrackBytesWrittenToSector < writeTrackBytesPerSector)
                                            {
                                                // still writing bytes to the sector

                                                // we use writeTrackBufferOffset because the sectors may be interleaved.
                                                // this value gets reset to where the sector should reside in the buffer
                                                // based on the sector value in the IDRecord

                                                m_caWriteBuffer[writeTrackBufferOffset] = b;
                                                writeTrackBufferOffset++;
                                                writeTrackBytesWrittenToSector++;

                                                totalBytesThisTrack++;  // added a byte to the buffer to write to the image

                                                // keep track of the max size of the buffer we need to write.

                                                if (writeTrackBufferSize < writeTrackBufferOffset)
                                                    writeTrackBufferSize = writeTrackBufferOffset;
                                            }
                                            else
                                            {
                                                // we are done writing this sector - set up for the next one - we will just ignore the writing of the CRC bytes
                                                // and set the next state to get the next ID record mark.

                                                currentWriteTrackState = (int)WRITE_TRACK_STATES.WaitForIDRecordMark;

                                                // increment the number of sectors in the m_caWriteBuffer

                                                sectorsInWriteTrackBuffer++;
                                            }
                                        }
                                        break;

                                    case (int)WRITE_TRACK_STATES.GetLastFewBytes:
                                        {
                                            writeTrackWriteBuffer[writeTrackWriteBufferIndex++] = b;
                                            lastFewBytesRead++;
                                        }
                                        break;
                                }
                            }
                        }
                        previousByte = b;
                    }
                    break;

                case (int)FDCRegisterOffsets.FDC_DRVREG_OFFSET:     // Ok to write to this register if drive is not ready or write protected
                    {
                        driveReady = CheckReady(b & (byte)0x03);
                        writeProtected = CheckWriteProtect(b & (byte)0x03);

                        if ((b & 0x40) == 0x40)         // we are selecting side 1
                            m_nCurrentSideSelected = 1;
                        else                            // we are selecting side 0
                            m_nCurrentSideSelected = 0;

                        // preserve the high order bit of m_FDC_DRVRegister since it is not writable by the processor (read only)
                        // and can only be set by the controller. The is the DRQ signal that is set by DRQ from the 2797. Also
                        // preserve bit 6 (the INTRQ signal from the 2797). These are here so the user can see these hardware
                        // bits with reading and clearing the status register. Reading the status register should clear INTRQ
                        // but not DRQ. reading the data regsiter should clear DRQ. Any write to the command register should also
                        // clear INTRQ.

                        if ((m_FDC_DRVRegister & 0x80) == 0x80) m_FDC_DRVRegister = (byte)(b | 0x80);   // preserve DRQ in DRV register
                        if ((m_FDC_DRVRegister & 0x40) == 0x40) m_FDC_DRVRegister = (byte)(b | 0x40);   // preserve INTRQ in DRV register

                        // now we can add in the bits that user can set by writing to the DRV register. but first we need to strip the drive
                        // select bits.

                        m_FDC_DRVRegister &= 0xFC;
                        m_FDC_DRVRegister |= (byte)(b & (byte)0x03);
                    }
                    break;

                    // Ok to write to this register if drive is not ready or write is protected
                    // but we cannot read from the drive if it is not ready, nor can we write
                    // to it

                case (int)FDCRegisterOffsets.FDC_CMDREG_OFFSET:     
                    {
                        // any write to the command register should clear the INTRQ signal presented to bit 6 of the DRV register

                        m_FDC_DRVRegister &= (byte)~DRV_INTRQ;      // de-assert INTRQ at the DRV register
                        ClearInterrupt();

                        m_FDC_CMDRegister = b;      // save command register write fro debugging purposes. The FD-2 emulation does not use it

                        m_nReadingTrack = m_nWritingTrack = m_nFDCReading = m_nFDCWriting = false;      // can't be read/writing if commanding

                        activityLedColor = (int)ActivityLedColors.greydot;

                        m_statusReadsWithoutDataRegisterAccess = 0;

                        switch (b & 0xF0)
                        {
                            // TYPE I

                            case 0x00:  //  0x0X = RESTORE
                                {
                                    m_nFDCTrack = 0;
                                    m_FDC_TRKRegister = 0;  // restore sets the track register in the chip to zero

                                    m_statusReadsWithoutDataRegisterAccess = 0;
                                    writeTrackBufferSize = 0;
                                    //totalBytesThisTrack = 0;    // reset on RESTORE command to floppy controller
                                    writeTrackWriteBufferIndex = 0;

                                    writeTrackMinSector = 0xff;
                                    writeTrackMaxSector = 0;
                                }
                                break;

                            case 0x10:  //  0x1X = SEEK
                                {
                                    m_nFDCTrack = m_FDC_DATARegister;
                                    m_FDC_TRKRegister = (byte)m_nFDCTrack;

                                    m_statusReadsWithoutDataRegisterAccess = 0;
                                    writeTrackBufferSize = 0;
                                    //totalBytesThisTrack = 0;    // reset on SEEK command to floppy controller
                                    writeTrackWriteBufferIndex = 0;

                                    writeTrackMinSector = 0xff;
                                    writeTrackMaxSector = 0;
                                }
                                break;

                            case 0x20:  //  0x2X = STEP
                                m_statusReadsWithoutDataRegisterAccess = 0;
                                writeTrackBufferSize = 0;
                                //totalBytesThisTrack = 0;    // reset on STEP command to floppy controller
                                writeTrackWriteBufferIndex = 0;

                                writeTrackMinSector = 0xff;
                                writeTrackMaxSector = 0;
                                break;

                            case 0x30:  //  0x3X = STEP W/TRACK UPDATE
                                m_statusReadsWithoutDataRegisterAccess = 0;
                                writeTrackBufferSize = 0;
                                //totalBytesThisTrack = 0;    // reset on STEP WITH TRACK UPDATE command to floppy controller
                                writeTrackWriteBufferIndex = 0;

                                writeTrackMinSector = 0xff;
                                writeTrackMaxSector = 0;
                                break;

                            case 0x40:  //  0x4X = STEP IN
                                {
                                    if (m_nFDCTrack < 79)
                                        m_nFDCTrack++;

                                    m_statusReadsWithoutDataRegisterAccess = 0;
                                    writeTrackBufferSize = 0;
                                    //totalBytesThisTrack = 0;    // reset on STEP IN command to floppy controller
                                    writeTrackWriteBufferIndex = 0;

                                    writeTrackMinSector = 0xff;
                                    writeTrackMaxSector = 0;
                                }
                                break;

                            case 0x50:  //  0x5X = STEP IN  W/TRACK UPDATE
                                {
                                    if (m_nFDCTrack < 79)
                                    {
                                        m_nFDCTrack++;
                                        m_FDC_TRKRegister = (byte)m_nFDCTrack;
                                    }
                                    m_statusReadsWithoutDataRegisterAccess = 0;
                                    writeTrackBufferSize = 0;
                                    //totalBytesThisTrack = 0;    // reset on STEP IN WITH TRACK UPDATE command to floppy controller
                                    writeTrackWriteBufferIndex = 0;

                                    writeTrackMinSector = 0xff;
                                    writeTrackMaxSector = 0;
                                }
                                break;

                            case 0x60:  //  0x6X = STEP OUT
                                {
                                    if (m_nFDCTrack > 0)
                                        --m_nFDCTrack;

                                    m_statusReadsWithoutDataRegisterAccess = 0;
                                    writeTrackBufferSize = 0;
                                    //totalBytesThisTrack = 0;    // reset on STEP OUT command to floppy controller
                                    writeTrackWriteBufferIndex = 0;

                                    writeTrackMinSector = 0xff;
                                    writeTrackMaxSector = 0;
                                }
                                break;

                            case 0x70:  //  0x7X = STEP OUT W/TRACK UPDATE
                                {
                                    if (m_nFDCTrack > 0)
                                    {
                                        --m_nFDCTrack;
                                        m_FDC_TRKRegister = (byte)m_nFDCTrack;
                                    }

                                    m_statusReadsWithoutDataRegisterAccess = 0;
                                    writeTrackBufferSize = 0;
                                    //totalBytesThisTrack = 0;    // reset on STEP OUT WITH TRACK UPDATE command to floppy controller
                                    writeTrackWriteBufferIndex = 0;

                                    writeTrackMinSector = 0xff;
                                    writeTrackMaxSector = 0;
                                }
                                break;

                            // TYPE II

                            case 0x80:  //  0x8X = READ  SINGLE   SECTOR
                            case 0x90:  //  0x9X = READ  MULTIPLE SECTORS
                            case 0xA0:  //  0xAX = WRITE SINGLE   SECTOR
                            case 0xB0:  //  0xBX = WRITE MULTIPLE SECTORS
                                {
                                    m_nReadingTrack = m_nWritingTrack = false;

                                    m_FDC_STATRegister |= (byte)FDC_HDLOADED;

                                    nType = 2;
                                    m_nFDCTrack = m_FDC_TRKRegister;

                                    bMultisector = false;
                                    m_nBytesToTransfer = 256;

                                    if ((b & 0x10) == 0x10)
                                    {
                                        if (Program.AllowMultiSectorTransfers)
                                        {
											bMultisector = true;
                                            m_nBytesToTransfer = (Program.SectorsPerTrack[nDrive] - m_nFDCSector) * 256;
                                        }
                                    }

                                    if ((b & 0x20) == 0x20)   // WRITE
                                    {
                                        activityLedColor = (int)ActivityLedColors.reddot;

                                        m_nFDCReading = false;
                                        m_nFDCWriting = true;
                                        m_nFDCWritePtr = 0;
                                        m_statusReadsWithoutDataRegisterAccess = 0;
                                        //m_nStatusReads = 0;
                                    }
                                    else                    // READ
                                    {
                                        activityLedColor = (int)ActivityLedColors.greendot;

                                        m_nFDCReading = true;
                                        readBufferIndex = 0;

                                        m_nFDCWriting = false;
                                        m_nFDCReadPtr = 0;
                                        m_statusReadsWithoutDataRegisterAccess = 0;
                                        //m_nStatusReads = 0;

                                        CalcFileOffset(nDrive);

                                        if (Program.FloppyDriveStream[nDrive] != null)
                                        {
                                            Program.FloppyDriveStream[nDrive].Seek(m_lFileOffset, SeekOrigin.Begin);
                                            Program.FloppyDriveStream[nDrive].Read(m_caReadBuffer, 0, m_nBytesToTransfer);

                                            LogFloppyRead(m_caReadBuffer, 0, m_nBytesToTransfer);   // ???

                                            m_FDC_DATARegister = m_caReadBuffer[0];
                                        }
                                    }

                                    m_statusReadsWithoutDataRegisterAccess = 0;
                                    writeTrackBufferSize = 0;
                                    //totalBytesThisTrack = 0;    // reset on 0x8X 0x9X 0xAX 0xBX command to floppy controller
                                    writeTrackWriteBufferIndex = 0;

                                    writeTrackMinSector = 0xff;
                                    writeTrackMaxSector = 0;
                                }
                                break;

                            // TYPE III

                            case 0xC0:  //  0xCX = READ ADDRESS
                                {
                                    //
                                    // Upon receipt of the Read Address command, the head is loaded and the BUSY Status Bit is set. The next
                                    // encountered ID field is then read in from the disk and the siz data bytes of the ID field are transferred 
                                    // to the Data Register, and an DRQ is generated for each byte. Those siz bytes are as follows:
                                    //
                                    //  TRACK ADDR      1 byte      m_FDC_TRKRegister
                                    //  ZEROS           1 byte      0x00
                                    //  SECTOR ADDR     1 byte      m_FDC_SECRegister
                                    //  SECTOR LENGTH   1 byte      256
                                    //  CRC             2 bytes     
                                    //
                                    // Although the CRC characters are transferred to the computer, the FD1771 chacks for validity and the CRC
                                    // error status bit is set if there is a CRC error. The sector address of the ID field is written into the
                                    // sector register. At the end of the operation, an interrupt is generated and the BUSY bit is cleared.
                                    //
                                    //  Each of the Type II commands contain a flag (b - bit 3) which in conjunction with the sector length
                                    // field contents of the ID determines the length (number of bytes) of the Data field.
                                    //
                                    //  For IBM compatibility, the b flag should be equal 1. The number of bytes in the Data field (sector)
                                    // is then 128 * 2 to the n where n = 0, 1, 2, 3
                                    //
                                    //      for b = 1
                                    //
                                    //          Sector Length Field     Number of bytes in sector
                                    //              00                      128
                                    //              01                      256
                                    //              02                      512
                                    //              03                     1024
                                    //
                                    //  When the b flag equals zero, the sector length field (n) multiplied by 16 determines the number of bytes
                                    //  in the sector Data field as shown below:
                                    //
                                    //      for b = 0
                                    //
                                    //          Sector Length Field     Number of bytes in sector
                                    //              01                       16
                                    //              02                       32
                                    //              03                       48
                                    //              04                       64
                                    //               .                     
                                    //               .                     
                                    //               .                     
                                    //              FF                     4080
                                    //              00                     4096
                                    //

                                    //lastSectorAccessed = m_FDC_SECRegister;     // save the last sector we accessed

                                    nType = 3;
                                    m_FDC_STATRegister |= (byte)FDC_HDLOADED;
                                    activityLedColor = (int)ActivityLedColors.greendot;

                                    m_nFDCReading = true;
                                    m_nFDCWriting = false;

                                    readBufferIndex = 0;

                                    m_caReadBuffer[0] = 0xFE;
                                    m_caReadBuffer[1] = m_FDC_TRKRegister;
                                    m_caReadBuffer[2] = (byte)m_nCurrentSideSelected;

                                    m_statusReadsWithoutDataRegisterAccess = 0;

                                    // determine what the value should be for the sector number returned. This is
                                    // determined by the the disk geometry. 

                                    if (Program.DiskFormat[nDrive] == DiskFormats.DISK_FORMAT_MINIFLEX)
                                    {
                                        m_caReadBuffer[3] = m_FDC_SECRegister;
                                        m_caReadBuffer[4] = 0x00;   // 256 byte sectors
                                    }
                                    else if ((Program.DiskFormat[nDrive] == DiskFormats.DISK_FORMAT_FLEX) || (Program.DiskFormat[nDrive] == DiskFormats.DISK_FORMAT_UNIFLEX))
                                    {
                                        byte maxSector = Program.SectorsPerTrack[nDrive];

                                        if (m_FDC_SECRegister < maxSector)
                                            m_caReadBuffer[3] = (byte)(m_FDC_SECRegister + 1);
                                        else
                                            m_caReadBuffer[3] = 1;

                                        m_caReadBuffer[4] = 0x01;   // 256 byte sectors

                                        m_FDC_SECRegister = m_caReadBuffer[3];
                                    }
                                    else if (Program.DiskFormat[nDrive] == DiskFormats.DISK_FORMAT_FLEX_IMA)
                                    {
                                        byte maxSector = Program.SectorsPerTrack[nDrive];
                                        byte minSector = 1;

                                        if (m_nCurrentSideSelected == 0)
                                        {
                                            if (m_nFDCTrack == 0)
                                            {
                                                maxSector = 10;
                                                minSector = 1;
                                            }
                                            else
                                            {
                                                maxSector = 10;     // set to 10 since we don't know if this is a single or double sided image
                                                minSector = 1;
                                            }
                                        }
                                        else
                                        {
                                            if (m_nFDCTrack == 0)
                                            {
                                                maxSector = 20;     // max sectors on cylinder 0
                                                minSector = 11;
                                            }
                                            else
                                            {
                                                maxSector = Program.SectorsPerTrack[nDrive];    // set to actual max sector since we know this is a double sided image
                                                minSector = 11;
                                            }
                                        }

                                        // say that we are over the last sector accessed so that it appears as though we are actually  rotating

                                        lastSectorAccessed++;
                                        if (lastSectorAccessed < maxSector)
                                            m_caReadBuffer[3] = (byte)(lastSectorAccessed);      
                                        else                                                        
                                            m_caReadBuffer[3] = minSector;

                                        lastSectorAccessed = m_caReadBuffer[3];     // do this in case it got set back to minSector

                                        m_caReadBuffer[4] = 0x01;   // 256 byte sectors
                                    }
                                    else
                                    {
                                        // this is an OS-9 diskette image

                                        m_caReadBuffer[3] = m_FDC_SECRegister;
                                        m_caReadBuffer[4] = 0x01;   // 256 byte sectors

                                        m_FDC_SECRegister = m_caReadBuffer[3];
                                    }

                                    // now calculate the CRC
                                    ushort readAddressCRC = CRCCCITT(m_caReadBuffer, 0, 5, 0xffff, 0);

                                    m_caReadBuffer[5] = (byte)(readAddressCRC / 256);
                                    m_caReadBuffer[6] = (byte)(readAddressCRC % 256);

                                    // we included the address mark in the buffer to SRC it into the CRC value. But we
                                    // do not return it to the user. So start at offset 1 in the buffer and set bytes 
                                    // to read and buffer pointer accordingly. 

                                    m_nFDCReadPtr = 1;
                                    m_nBytesToTransfer = 6;
                                    m_FDC_DATARegister = m_caReadBuffer[1];

                                    // do this so we can log whare we are when we are reading the address mark data
                                    // ------------------------------------------
                                    byte FDCSectorSave = (byte)m_nFDCSector;
                                    m_nFDCSector = m_caReadBuffer[3];
                                    CalcFileOffset(nDrive);
                                    m_nFDCSector = FDCSectorSave;
                                    // ------------------------------------------

                                    m_FDC_STATRegister |= (byte)FDC_BUSY;
                                    if (m_nFDCReading || m_nReadingTrack)
                                        activityLedColor = (int)ActivityLedColors.greendot;
                                    else if (m_nFDCWriting || m_nWritingTrack)
                                        activityLedColor = (int)ActivityLedColors.reddot;
                                    else
                                        activityLedColor = (int)ActivityLedColors.greydot;

                                    writeTrackBufferSize = 0;
                                    //totalBytesThisTrack = 0;    // reset on READ ADDRESS command to floppy controller
                                    writeTrackWriteBufferIndex = 1;

                                    writeTrackMinSector = 0xff;
                                    writeTrackMaxSector = 0;
                                }
                                break;

                            case 0xE0:  //  0xEX = READ TRACK
                                // Read Track is 1110010s where s is the O flag. 1 = synchronizes to address mark, 0 = do not synchronize to address mark.
                                if ((b & 0x04) == 0x04)     
                                {
                                    activityLedColor = (int)ActivityLedColors.greendot;

                                    m_nFDCReading = true;
                                    readBufferIndex = 0;

                                    m_nFDCWriting = false;
                                    m_nFDCReadPtr = 0;

                                    m_statusReadsWithoutDataRegisterAccess = 0;

                                    //
                                    // Upon receipt of the Read Track command, the head is loaded and the BUSY bit is set. Reading starts with
                                    // the leading edge of first encountered index mark and continues until the next index pulse. As each byte
                                    // is assembled it is transferred to the Data Register and the Data Request is generated for each byte. No
                                    // CRC checking is performed. Gaps are included in the input data stream. If bit O of the command regoster
                                    // is 0, the accumlation of bytes is synchronized to each Address Mark encountered. Upon completion of the
                                    // command, the interrupt is activated.
                                    //
                                    // Bit zero is the O flag. 1 = synchronizes to address mark, 0 = do not synchronize to address mark.
                                    //
                                    //      1:  set proper type, status bits and track register
                                    nType = 3;
                                    m_FDC_STATRegister |= (byte)FDC_HDLOADED;
                                    m_nFDCTrack = m_FDC_TRKRegister;

                                    //      2:  using the data in the track register calculate the offset to the start of the track
                                    //          and set number of bytes to transfer. The sector gets set to 0 if on track 0 and to 1
                                    //          if on any other track. The CalcFileOffset takes care of adjusting the sector number
                                    //          internally based on image file format.

                                    m_nFDCSector = m_nFDCTrack == 0 ? 0 : 1;

                                    CalcFileOffset(nDrive);

                                    m_nBytesToTransfer = m_nFDCTrack == 0 ? (Program.SectorsOnTrackZeroSideZero[nDrive]) * 256 : (Program.SectorsPerTrack[nDrive]) * 256;

                                    //      4:  read the entire track into memory
                                    if (Program.FloppyDriveStream[nDrive] != null)
                                    {
                                        Program.FloppyDriveStream[nDrive].Seek(m_lFileOffset, SeekOrigin.Begin);
                                        Program.FloppyDriveStream[nDrive].Read(m_caReadBuffer, 0, m_nBytesToTransfer);

                                        LogFloppyRead(m_caReadBuffer, 0, m_nBytesToTransfer);   // ???

                                        // start with a single byte of 0x00

                                        m_FDC_DATARegister = 0x00;
                                    }

                                    //      5:  indicate that this a track read and not a sector read.
                                    m_nReadingTrack = true;
                                    m_nWritingTrack = false;

                                    //      6:  during the reading while serviceing the DRQ, the sector pre and post bytes will be sent around each 256 byte boundary.
                                    //          A state machine will be used to do this, so start the stae machine at the beginning

                                    currentReadTrackState       = (int)READ_TRACK_STATES.ReadPostIndexGap;

                                    // set statemachine indexs to initial values.

                                    currentPostIndexGapIndex    = 0;
                                    currentGap2Index            = 0;
                                    currentGap3Index            = 0;
                                    currentDataSectorNumber     = 1;

                                    m_statusReadsWithoutDataRegisterAccess = 0;
                                    writeTrackBufferSize = 0;
                                    //totalBytesThisTrack = 0;    // reset on READ TRACK command to floppy controller
                                    writeTrackWriteBufferIndex = 0;

                                    writeTrackMinSector = 0xff;
                                    writeTrackMaxSector = 0;
                                }
                                break;

                            case 0xF0:  //  0xFX = WRITE TRACK
                                if (Program.DiskFormat[nDrive] == DiskFormats.DISK_FORMAT_FLEX_IMA)
                                {
                                    //
                                    //  Upon receipt of the Write Track command, the head is loaded and the BUSY bit is set. Writing starts with
                                    // the leading edge of first encountered index mark and continues until the next index pulse, at which time
                                    // an interrupt is generated. The Data Request is activated immediately upon receiving the command, but writing
                                    // will not start until after the first byte has been loaded into the Data Register. If the DR has not been
                                    // loaded by the time the index pulse is encountered the operaiton is terminated making the device Not Busy.
                                    // The Lost Data Status Bit is set and the interrupt is activated. If a byte is not present in the DR as 
                                    // needed, a byte of zeros is substituted. Address Marks and CRC characters are written on the disk by
                                    // detecting certain data byte patterns in the outgoing data stream as shown in the table below. The CRC
                                    // generator is initialized when any data byte from F8 to FE is about to be transferred from the DR to the DSR.
                                    //
                                    //      CONTROL BYTES FOR INITIALIZATION
                                    //
                                    //      data pattern    interpretation          clock mark
                                    //          F7          Write CRC character     FF
                                    //          F8          Data Address Mark       C7
                                    //          F9          Data Address Mark       C7
                                    //          FA          Data Address Mark       C7
                                    //          FB          Data Address Mark       C7
                                    //          FC          Data Address Mark       D7
                                    //          FD          Spare
                                    //          FE          ID Address Mark         C7
                                    //
                                    // The Write Track command will not execute if the \DINT input is grounded. Instead the write protect status
                                    // bit is set and the interrupt is activated. Note that 1 F7 pattern generates 2 CRC bytes.
                                    //

                                    // formatting a track only works if the image files has the SIR already set up.

                                    //  On each write track buffer command, reset the number of sectors in the buffer to 0.
                                    //  As each address ID record is written, this number will get incremented/.
                                    //  When the count is equal to the number of allowable sectors on the track being written, it is time
                                    //  to signle complete by dropping BUSY and Setting the interrupt and stop setting DRQ after writing
                                    //  the byte to the sector track buffer.

                                    sectorsInWriteTrackBuffer = 0;
                                    lastFewBytesRead = 0;

                                    m_nFDCReading = false;
                                    m_nReadingTrack = false;

                                    activityLedColor = (int)ActivityLedColors.reddot;

                                    m_nFDCWriting = true;
                                    m_nWritingTrack = true;

                                    //      1:  set proper type, status bits and track register

                                    nType = 3;
                                    m_FDC_STATRegister |= (byte)FDC_HDLOADED;
                                    m_nFDCTrack = m_FDC_TRKRegister;

                                    //      2:  reset the counters and indexes for a new track 

                                    m_statusReadsWithoutDataRegisterAccess = 0;
                                    writeTrackBufferSize = 0;
                                    totalBytesThisTrack = 0;    // reset on WRITE TRACK command to floppy controller
                                    writeTrackWriteBufferIndex = 0;

                                    writeTrackMinSector = 0xff;
                                    writeTrackMaxSector = 0;

                                    currentWriteTrackState = (int)WRITE_TRACK_STATES.WaitForIDRecordMark;
                                }
                                else
                                {
                                    nType = 3;
                                    m_FDC_STATRegister &= (byte)~FDC_HDLOADED;
                                    m_FDC_STATRegister &= (byte)~FDC_BUSY;
                                    activityLedColor = (int)ActivityLedColors.greydot;
                                }
                                break;

                            // TYPE IV

                            case 0xD0:  //  0xDX = FORCE INTERRUPT
                                {
                                    //
                                    // This command can be loaded into the command register at any time. If there is a current command under
                                    // execution (Busy Status Bit set), the command will be terminated and an interrupt will be generated
                                    // when the condition specified in the I0 through I3 field is detected.
                                    //
                                    //      I0  Not-Ready-To-Ready Transition
                                    //      I1  Ready-To-Not-Ready Transition
                                    //      I2  Every Index Pulse
                                    //      I3  Immediate Interrupt
                                    //
                                    // If they are all equal to 0, there is no interrupt generated but the current command is terminated
                                    // and the Busy Bit is cleared.
                                    //
                                    nType = 4;

                                    m_statusReadsWithoutDataRegisterAccess = 0;
                                    writeTrackBufferSize = 0;
                                    totalBytesThisTrack = 0;    // reset on FORCE INTERRUPT command to floppy controller
                                    writeTrackWriteBufferIndex = 0;

                                    writeTrackMinSector = 0xff;
                                    writeTrackMaxSector = 0;
                                }
                                break;
                        }

                        //  since this may get set as a result of issuing a read command without actually 
                        //  reading anything - just waiting for the controller to go not busy to do a CRC 
                        //  check of the sector only, we need some way to make sure this status gets cleared
                        //  after some time. Lets try by counting the number of times the status is checked
                        //  without a data read and after 256 status reads withiout a data read - we will 
                        //  set the status to not busy.

                        if (driveReady)
                            m_FDC_STATRegister = (byte)FDC_BUSY;
                        else
                            m_FDC_STATRegister |= (byte)FDC_BUSY;

                        if (m_nFDCReading || m_nReadingTrack)
                            activityLedColor = (int)ActivityLedColors.greendot;
                        else if (m_nFDCWriting || m_nWritingTrack)
                            activityLedColor = (int)ActivityLedColors.reddot;
                        else
                            activityLedColor = (int)ActivityLedColors.greydot;

                        m_statusReadsWithoutDataRegisterAccess = 0;

                        switch (nType)
                        {
                            case 1:
                                // 0x0X = RESTORE
                                // 0x1X = SEEK
                                // 0x2X = STEP
                                // 0x3X = STEP     W/TRACK UPDATE
                                // 0x4X = STEP IN
                                // 0x5X = STEP IN  W/TRACK UPDATE
                                // 0x6X = STEP OUT
                                // 0x7X = STEP OUT W/TRACK UPDATE
                                {
                                    m_FDC_STATRegister |= (byte)FDC_HDLOADED;
                                    if (m_nFDCTrack == 0)
                                        m_FDC_STATRegister |= (byte)FDC_TRKZ;
                                    else
                                        m_FDC_STATRegister &= (byte)~FDC_TRKZ;
                                }
                                break;

                                // handle type 2 and type 3 the same

                            case 2:
                                //  0x8X = READ  SINGLE   SECTOR
                                //  0x9X = READ  MULTIPLE SECTORS
                                //  0xAX = WRITE SINGLE   SECTOR
                                //  0xBX = WRITE MULTIPLE SECTORS

                            case 3:
                                //  0xCX = READ ADDRESS
                                //  0xEX = READ TRACK
                                //  0xFX = WRITE TRACK
                                {
                                    if (Program.FloppyDriveStream[nDrive] != null)
                                    {
                                        m_FDC_STATRegister |= (byte)FDC_DRQ;
                                        m_FDC_DRVRegister  |= (byte)DRV_DRQ;

                                        if (_bInterruptEnabled && (m_FDC_DRVRegister & (byte)DRV_INTRQ) == (byte)DRV_INTRQ)
                                        {
                                            SetInterrupt(_spin);
                                            if (Program._cpu != null)
                                            {
                                                if ((Program._cpu.InWait || Program._cpu.InSync) && Program.CpuThread.ThreadState == System.Threading.ThreadState.Suspended)
                                                {
                                                    try
                                                    {
                                                        Program.CpuThread.Resume();
                                                    }
                                                    catch (ThreadStateException e)
                                                    {
                                                        // do nothing if thread is not suspended
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        m_FDC_STATRegister &= (byte)~FDC_DRQ;
                                        m_FDC_DRVRegister &= (byte)~DRV_DRQ;
                                    }
                                }
                                break;

                            //  0xDX = FORCE INTERRUPT

                            case 4:
                                m_FDC_STATRegister &= (byte)~(
                                                            FDC_DRQ     |   // clear Data Request Bit
                                                            FDC_SEEKERR |   // clear SEEK Error Bit
                                                            FDC_CRCERR  |   // clear CRC Error Bit
                                                            FDC_RNF     |   // clear Record Not Found Bit
                                                            FDC_BUSY        // clear BUSY bit
                                                          );
                                m_FDC_DRVRegister &= (byte)~DRV_DRQ;        // turn off high order bit in drive status register
                                m_FDC_DRVRegister |= (byte)DRV_INTRQ;       // assert INTRQ at the DRV register on force interrupt
                                activityLedColor = (int)ActivityLedColors.greydot;
                                break;
                        }
                    }
                    break;

                case (int)FDCRegisterOffsets.FDC_TRKREG_OFFSET: // Ok to write to this register if drive is not ready or write protected
                    {
                        m_FDC_TRKRegister = b;
                        m_nFDCTrack = b;
                    }
                    break;

                case (int)FDCRegisterOffsets.FDC_SECREG_OFFSET:
                    {
                        m_FDC_SECRegister = b;
                        m_nFDCSector = b;
                    }
                    break;

                default:
                    {
                        Program._cpu.WriteToFirst64K(m, b);
                    }
                    break;
            }

            LogFloppyActivity((ushort)(m - m_sBaseAddress), b, false);

            if (previousActivityLedColor != activityLedColor)
            {
                previousActivityLedColor = activityLedColor;

                switch (activityLedColor)
                {
                    case (int)ActivityLedColors.greydot:
                        //Program._mainForm.SetFloppyActivity(nDrive, global::SWTPCemuApp.Properties.Resources.greydot);
                        break;
                    case (int)ActivityLedColors.greendot:
                        //Program._mainForm.SetFloppyActivity(nDrive, global::SWTPCemuApp.Properties.Resources.greendot);
                        break;
                    case (int)ActivityLedColors.reddot:
                        //Program._mainForm.SetFloppyActivity(nDrive, global::SWTPCemuApp.Properties.Resources.reddot);
                        break;
                    default:
                        //Program._mainForm.SetFloppyActivity(nDrive, global::SWTPCemuApp.Properties.Resources.greydot);
                        break;
                }
            }
        }

        enum ActivityLedColors
        {
            greydot = 0,
            greendot,
            reddot
        }

        int previousActivityLedColor = (int)ActivityLedColors.greydot;

        public override byte Read(ushort m)
        {
            byte d;
            int nWhichRegister = m - m_sBaseAddress;
            int nDrive         = m_FDC_DRVRegister & 0x03;           // get active drive

            bool driveReady = CheckReady(nDrive);
            bool writeProtected = CheckWriteProtect(nDrive);

            int activityLedColor = (int)ActivityLedColors.greydot;

            ClearInterrupt();

            switch (nWhichRegister)
            {
                case (int)FDCRegisterOffsets.FDC_DATAREG_OFFSET:
                    {
                        if (m_nFDCReading)
                        {
                            m_statusReadsWithoutDataRegisterAccess = 0;

                            if (!m_nReadingTrack)
                            {
                                // this is a regular sector read or a read address read - no state machine required
                                // the bytes are already in the m_caBuffer from executing the command write to the 
                                // command regiuster

                                m_FDC_DATARegister = m_caReadBuffer[m_nFDCReadPtr];
                                lock (Program._cpu.buildingDebugLineLock)
                                {
                                    m_nFDCReadPtr++;
                                }

                                // we just bumped the pointer. When it gets bumped past the number of bytes to read 
                                // this means that the caller is responding to what is the last DRQ in the data 
                                // stream. - it's time to stop sending DRQ's and signal NOT BUSY. kEEP IN MIND THAT
                                // m_nFDCReadPtr is zero based and m_nBytesToTransfer is a count.

                                byte[] temp = new byte[6];
                                bool weAreDone = false;

                                if (m_nBytesToTransfer == 6)
                                {
                                    // we are doing a read address so we need to check if the m_nFDCReadPtr > m_nBytesToTransfer
                                    // because the offset to read starts at 1 and not 0.

                                    if (m_nFDCReadPtr > m_nBytesToTransfer)
                                        weAreDone = true;
                                }
                                else
                                {
                                    if (m_nFDCReadPtr >= m_nBytesToTransfer)
                                        weAreDone = true;
                                }

                                if (weAreDone)
                                {
                                    // this is for debugging - not needed by the application
                                    //
                                    //if (m_nBytesToTransfer == 6)
                                    //{
                                    //    for (int i = 1; i < 7; i++)
                                    //    {
                                    //        temp[i - 1] = m_caReadBuffer[i];
                                    //    }
                                    //}

                                    // we are done with this sector - stop sending DRQ's and set NOT BUSY

                                    m_FDC_STATRegister &= (byte)~FDC_DRQ;
                                    m_FDC_STATRegister &= (byte)~FDC_BUSY;
                                    activityLedColor = (int)ActivityLedColors.greydot;

                                    // slao turn off the INTRQ signal that DRQ provides to the bus.

                                    m_FDC_DRVRegister &= (byte)~DRV_DRQ;
                                    m_FDC_DRVRegister |= (byte)DRV_INTRQ;               // assert INTRQ at the DRV register when the operation is complete

                                    if (_bInterruptEnabled && (m_FDC_DRVRegister & (byte)DRV_INTRQ) == (byte)DRV_INTRQ)
                                    {
                                        lock (Program._cpu.buildingDebugLineLock)
                                        {
                                            SetInterrupt(_spin);
                                            if (Program._cpu != null)
                                            {
                                                if ((Program._cpu.InWait || Program._cpu.InSync) && Program.CpuThread.ThreadState == System.Threading.ThreadState.Suspended)
                                                {
                                                    try
                                                    {
                                                        Program.CpuThread.Resume();
                                                    }
                                                    catch (ThreadStateException e)
                                                    {
                                                        // do nothing if thread is not suspended
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    // signal that there is data in the data register that needs to be read.

                                    m_FDC_STATRegister |= (byte)FDC_DRQ;
                                    m_FDC_DRVRegister  |= (byte)DRV_DRQ;

                                    // only interrupt when the command is complete

                                    if (_bInterruptEnabled && (m_FDC_DRVRegister & (byte)DRV_INTRQ) == (byte)DRV_INTRQ)
                                    {
                                        lock (Program._cpu.buildingDebugLineLock)
                                        {
                                            SetInterrupt(_spin);
                                            if (Program._cpu != null)
                                            {
                                                if ((Program._cpu.InWait || Program._cpu.InSync) && Program.CpuThread.ThreadState == System.Threading.ThreadState.Suspended)
                                                {
                                                    try
                                                    {
                                                        Program.CpuThread.Resume();
                                                    }
                                                    catch (ThreadStateException e)
                                                    {
                                                        // do nothing if thread is not suspended
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // we are doing a track read so we need to use a state machine to send the
                                //      post index Gap and then record id, Gap2, data field, Gap3 for each
                                //      of the sectors
                                //
                                //      The Post Index Gap is defined as 32 bytes of zeros or 0xFF
                                //      The ID Record is 6 bytes of id information preceeded by a byte of 0xFE
                                //
                                //          TRACK ADDR      1 byte      m_FDC_TRKRegister
                                //          SIDE            1 byte      
                                //          SECTOR ADDR     1 byte      m_FDC_SECRegister
                                //          SECTOR LENGTH   1 byte      256 (0 = 128, 1 = 256, 2 = 512, 3 = 1024)
                                //          CRC             2 bytes     
                                //
                                //      Gap2 is defined as 11 bytes of 0x00 followed by 6 bytes of 0xFF
                                //      the data bytes are 256 bytes of data preceeded by a dta mark of 0xFB
                                //      Gap3 is defined as  1 byte of 0xFF followed by 32 bytes of 0x00 or 0xFF
                                //
                                //
                                //  read track states                   next state                  FM                      MFM
                                //                                                              count   value               count   value
                                //      ReadPostIndexGap                ReadIDRecord            40      0xFF or 0x00        80      0x4E
                                //                                                               6      0x00                12      0x00
                                //                                                                                           3      0xF6 (writes C2)
                                //                                                               1      0xFC (index mark)    1      0xFC (index mark)
                                //                                                              26      0x00 or 0xFF        50      0x4E (both write bracketed field 26 times)
                                //                  Repeat for each sector
                                //  -----------------------
                                //  |                                                            6      0x00                12      0x00
                                //  |                                                                                        3      0xF5 (writes 0xA1's)
                                //  |   ReadIDRecord (0xFE)             ReadIDRecordTrack        1      0xFE                 1      0xFE
                                //  |       ReadIDRecordTrack           ReadIDRecordSide         1      track number         1      track number
                                //  |       ReadIDRecordSide            ReadIDRecordSector       1      side number (0 or 1) 1      side number (0 or 1)
                                //  |       ReadIDRecordSector          ReadIDRecordSectorLength 1      sector number        1      sector number
                                //  |       ReadIDRecordSectorLength    ReadIDRecordCRCHi        1      sector length        1      sector length (both 128 * 2^n where n = sector length
                                //  |       ReadIDRecordCRCHi           ReadIDRecordCRCLo        1      0xF7                 1      0xF7 (both write 2 byte CRC)
                                //  |       ReadIDRecordCRCLo           ReadGap2                
                                //  |   ReadGap2                        ReadDataRecord          11      0xFF or 0x00        22      0x4E
                                //  |                                                            6      0x00                12      0x00
                                //  |                                                                                        3      0xF5 (writes 0xA1's)
                                //  |   ReadDataRecord (0xFB)           ReadDataBytes            1      0xFB                 1      0xFB
                                //  |       ReadDataBytes               ReadDataRecordCRCHi    xxx      Data               xxx      Data where xxx is number of bytes per sector)
                                //  |       ReadDataRecordCRCHi         ReadDataRecordCRCLo      1      0xF7                 1      0xF7 (both write 2 byte CRC)
                                //  |       ReadDataRecordCRCLo         ReadGap3                27      0xFF or 0x00        54      x04E
                                //  |   ReadGap3                        ReadIDRecord
                                //  -----------------------
                                //
                                //      exit state machine after no more data to read.

                                // when we drop out of here the byte to return to the user should be in m_FDC_DATARegister

                                switch (currentReadTrackState)
                                {
                                    case (int)READ_TRACK_STATES.ReadPostIndexGap:
                                        if (currentPostIndexGapIndex < currentPostIndexGapSize)
                                        {
                                            m_FDC_DATARegister = 0x00;
                                            currentPostIndexGapIndex++;
                                        }
                                        else
                                        {
                                            currentPostIndexGapIndex = 0;
                                            currentReadTrackState = (int)READ_TRACK_STATES.ReadIDRecord;
                                        }
                                        break;

                                    case (int)READ_TRACK_STATES.ReadIDRecord:
                                        m_FDC_DATARegister = 0xFE;
                                        IDRecordBytes[0] = m_FDC_DATARegister;
                                        currentReadTrackState = (int)READ_TRACK_STATES.ReadIDRecordTrack;
                                        break;

                                    case (int)READ_TRACK_STATES.ReadIDRecordTrack:
                                        m_FDC_DATARegister = (byte)m_nFDCTrack;
                                        IDRecordBytes[1] = m_FDC_DATARegister;
                                        currentReadTrackState = (int)READ_TRACK_STATES.ReadIDRecordSide;
                                        break;

                                    case (int)READ_TRACK_STATES.ReadIDRecordSide:
                                        m_FDC_DATARegister = (byte)m_nCurrentSideSelected;
                                        IDRecordBytes[2] = m_FDC_DATARegister;
                                        currentReadTrackState = (int)READ_TRACK_STATES.ReadIDRecordSector;
                                        break;

                                    case (int)READ_TRACK_STATES.ReadIDRecordSector:
                                        if (m_nFDCTrack == 0 && currentDataSectorNumber == 1 && (Program.DiskFormat[nDrive] == DiskFormats.DISK_FORMAT_FLEX || Program.DiskFormat[nDrive] == DiskFormats.DISK_FORMAT_FLEX_IMA))
                                            m_FDC_DATARegister = 0x00;
                                        else
                                            m_FDC_DATARegister = (byte)currentDataSectorNumber;

                                        IDRecordBytes[3] = m_FDC_DATARegister;
                                        currentReadTrackState = (int)READ_TRACK_STATES.ReadIDRecordSectorLength;

                                        break;

                                    case (int)READ_TRACK_STATES.ReadIDRecordSectorLength:
                                        m_FDC_DATARegister = 0x01;      // 256 byte sectors
                                        IDRecordBytes[4] = m_FDC_DATARegister;
                                        currentReadTrackState = (int)READ_TRACK_STATES.ReadIDRecordCRCHi;
                                        break;

                                    case (int)READ_TRACK_STATES.ReadIDRecordCRCHi:
                                        crcID = CRCCCITT(IDRecordBytes, 0, IDRecordBytes.Length, 0xffff, 0);
                                        m_FDC_DATARegister = (byte)(crcID / 256);
                                        currentReadTrackState = (int)READ_TRACK_STATES.ReadIDRecordCRCLo;
                                        break;

                                    case (int)READ_TRACK_STATES.ReadIDRecordCRCLo:
                                        m_FDC_DATARegister = (byte)(crcID % 256);
                                        currentReadTrackState = (int)READ_TRACK_STATES.ReadGap2;
                                        break;

                                    case (int)READ_TRACK_STATES.ReadGap2:
                                        if (currentGap2Index < currentGap2Size)
                                        {
                                            if (currentGap2Index < currentGap2Size - 6)
                                                m_FDC_DATARegister = 0x00;
                                            else
                                                m_FDC_DATARegister = 0xFF;

                                            currentGap2Index++;
                                        }
                                        else
                                        {
                                            currentGap2Index = 0;
                                            currentReadTrackState = (int)READ_TRACK_STATES.ReadDataRecord;
                                        }
                                        break;

                                    case (int)READ_TRACK_STATES.ReadDataRecord:
                                        m_FDC_DATARegister = 0xFB;
                                        currentReadTrackState = (int)READ_TRACK_STATES.ReadDataBytes;
                                        break;

                                    case (int)READ_TRACK_STATES.ReadDataBytes:
                                        m_FDC_DATARegister = m_caReadBuffer[m_nFDCReadPtr];
                                        lock (Program._cpu.buildingDebugLineLock)
                                        {
                                            m_nFDCReadPtr++;
                                        }

                                        // see if we are done with this sector worth of data. If we are
                                        // move on to the next state - read the Data CRC.

                                        if (m_nFDCReadPtr % 256 == 0)
                                            currentReadTrackState = (int)READ_TRACK_STATES.ReadDataRecordCRCHi;
                                        break;

                                    case (int)READ_TRACK_STATES.ReadDataRecordCRCHi:
                                        {
                                            byte[] thisSector = new byte[257];
                                            thisSector[0] = 0xFB;

                                            // make a buffer with the data mark that we can pass to the CRC calculator

                                            for (int i = 0; i < 256; i++)
                                            {
                                                thisSector[i + 1] = m_caReadBuffer[i];
                                            }

                                            // calculate the CRC, pass back the hi byte and set the next state to pass back the lo byte

                                            crcData = CRCCCITT(thisSector, 0, thisSector.Length, 0xffff, 0);
                                            m_FDC_DATARegister = (byte)(crcData / 256);
                                            currentReadTrackState = (int)READ_TRACK_STATES.ReadDataRecordCRCLo;
                                        }
                                        break;

                                    case (int)READ_TRACK_STATES.ReadDataRecordCRCLo:
                                        m_FDC_DATARegister = (byte)(crcData % 256);
                                        currentReadTrackState = (int)READ_TRACK_STATES.ReadGap3;

                                        // now is the time to increment the current data sector number

                                        currentDataSectorNumber++;
                                        break;

                                    case (int)READ_TRACK_STATES.ReadGap3:
                                        if (currentGap3Index < currentGap3Size)
                                        {
                                            if (currentGap3Index == 0)
                                                m_FDC_DATARegister = 0x00;
                                            else
                                                m_FDC_DATARegister = 0xFF;

                                            currentGap3Index++;
                                        }
                                        else
                                        {
                                            currentGap3Index = 0;
                                            currentReadTrackState = (int)READ_TRACK_STATES.ReadIDRecord;
                                        }

                                        // after responding to DRQ on last byte of every GAP3 we need to see if we are done by comparing
                                        // the m_nFDCReadPtr to the number of bytes to read. If they are equal, we should not issue any
                                        // more DRQ's or interrupts and we should turn off the drive LED. Basically - shut it down.

                                        if (m_nFDCReadPtr == m_nBytesToTransfer)
                                        {
                                            lock (Program._cpu.buildingDebugLineLock)
                                            {
                                                m_FDC_STATRegister &= (byte)~(FDC_DRQ | FDC_BUSY);
                                                m_FDC_DRVRegister  &= (byte)~DRV_DRQ;            // turn off high order bit in drive status register
                                                m_FDC_DRVRegister |= (byte)DRV_INTRQ;            // assert INTRQ at the DRV register when the operation is complete

                                                m_nFDCReading = false;

                                                LogFloppyRead(readBuffer, 0, m_nBytesToTransfer);
                                                readBufferIndex = 0;

                                                ClearInterrupt();
                                                activityLedColor = (int)ActivityLedColors.greydot;

                                                // reset the state machine

                                                currentReadTrackState = (int)READ_TRACK_STATES.ReadPostIndexGap;
                                            }
                                        }
                                        else
                                        {
                                            m_FDC_STATRegister |= (byte)FDC_DRQ;
                                            m_FDC_DRVRegister  |= (byte)DRV_DRQ;

                                            if (_bInterruptEnabled && (m_FDC_DRVRegister & (byte)DRV_INTRQ) == (byte)DRV_INTRQ)
                                            {
                                                lock (Program._cpu.buildingDebugLineLock)
                                                {
                                                    SetInterrupt(_spin);
                                                    if (Program._cpu != null)
                                                    {
                                                        if ((Program._cpu.InWait || Program._cpu.InSync) && Program.CpuThread.ThreadState == System.Threading.ThreadState.Suspended)
                                                        {
                                                            try
                                                            {
                                                                Program.CpuThread.Resume();
                                                            }
                                                            catch (ThreadStateException e)
                                                            {
                                                                // do nothing if thread is not suspended
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        break;

                                    default:
                                        break;
                                }
                            }

                            // --------------------
                            d = m_FDC_DATARegister;

                            if (readBufferIndex < 65536)
                            {
                                readBuffer[readBufferIndex] = d;
                                readBufferIndex++;
                            }
                        }
                        else
                        {
                            d = m_FDC_DATARegister;
                            readBufferIndex = 0;
                        }
                    }
                    break;

                case (int)FDCRegisterOffsets.FDC_STATREG_OFFSET:
                    {
                        // see if we are reading the address mark data. we could also use the value in the m_FDC_CMDRegister.
                        // if it is m_FDC_CMDRegister & 0xC0 = 0xC0 then we are doing a read address.

                        if (m_nBytesToTransfer == 6)
                        {
                            // if we are - then see if we are done - checking for > because we start at offset 1

                            if ((m_nFDCReadPtr > m_nBytesToTransfer) || (m_statusReadsWithoutDataRegisterAccess > 3))
                            {
                                // we are done reading the requested number of bytes

                                lock (Program._cpu.buildingDebugLineLock)
                                {
                                    m_FDC_STATRegister &= (byte)~(FDC_DRQ | FDC_BUSY);
                                    m_FDC_DRVRegister  &= (byte)~DRV_DRQ;            // turn off high order bit in drive status register
                                    m_FDC_DRVRegister  |= (byte)DRV_INTRQ;           // assert INTRQ at the DRV register when the operation is complete

                                    m_nFDCReading = false;

                                    // we are using readBuffer here on purpose and not m_caReadBuffer - it is 0 based (no address
                                    // mark was stored here - just the 6 bytes of data)

                                    LogFloppyRead(readBuffer, 0, m_nBytesToTransfer);
                                    readBufferIndex = 0;

                                    ClearInterrupt();
                                    activityLedColor = (int)ActivityLedColors.greydot;
                                }
                            }
                        }
                        else
                        {
                            /* 2797 commands - contents of m_FDC_CMDRegister
                             * 
                                0x0X = RESTORE
                                0x1X = SEEK
                                0x2X = STEP
                                0x3X = STEP     W/TRACK UPDATE
                                0x4X = STEP IN
                                0x5X = STEP IN  W/TRACK UPDATE
                                0x6X = STEP OUT
                                0x7X = STEP OUT W/TRACK UPDATE
                                0x8X = READ  SINGLE   SECTOR
                                0x9X = READ  MULTIPLE SECTORS
                                0xAX = WRITE SINGLE   SECTOR
                                0xBX = WRITE MULTIPLE SECTORS
                                0xCX = READ ADDRESS
                                0xDX = FORCE INTERRUPT
                                0xEX = READ TRACK
                                0xFX = WRITE TRACK
                             *
                             */

                            // some debugging code - 
                            //
                            //// ignore write track, seek force restore and restore commands while debugging
                            //if (m_FDC_CMDRegister != 0xf4 && m_FDC_CMDRegister != 0x1b && m_FDC_CMDRegister != 0x0b && m_FDC_CMDRegister != 0xd4)
                            //{
                            //    if (m_nFDCReadPtr >= 0x00ff)
                            //    {
                            //        int x = 1;
                            //    }
                            //}

                            CheckReadyAndWriteProtect(nDrive);

                            if (!m_nFDCReading && !m_nFDCWriting)           // turn off BUSY if not read/writing
                            {
                                m_FDC_STATRegister &= (byte)~FDC_BUSY;
                                activityLedColor = (int)ActivityLedColors.greydot;
                            }


                            if ((m_statusReadsWithoutDataRegisterAccess > (m_nBytesToTransfer / 16)) && (m_nBytesToTransfer > 0) && m_nFDCReading)
                            {
                                lock (Program._cpu.buildingDebugLineLock)
                                {
                                    m_FDC_STATRegister &= (byte)~FDC_BUSY;     // clear BUSY if data not read
                                    activityLedColor = (int)ActivityLedColors.greydot;
                                    ClearInterrupt();
                                }
                            }

                            if (m_statusReadsWithoutDataRegisterAccess >= 16 && !m_nWritingTrack && m_nFDCWriting)
                            {
                                lock (Program._cpu.buildingDebugLineLock)
                                {
                                    m_FDC_STATRegister &= (byte)~FDC_BUSY;     // clear BUSY if data not read
                                    activityLedColor = (int)ActivityLedColors.greydot;
                                    ClearInterrupt();
                                }
                            }

                            // this is where we will get out of writing track by dumping the track to the file

                            if (m_statusReadsWithoutDataRegisterAccess >= 16 && m_nWritingTrack)
                            {
                                WriteTrackToImage(nDrive);
                                activityLedColor = (int)ActivityLedColors.greydot;
                            }

                            if (m_nFDCReadPtr > m_nBytesToTransfer)
                            {
                                // we are done reading the requested number of bytes

                                lock (Program._cpu.buildingDebugLineLock)
                                {
                                    m_FDC_STATRegister &= (byte)~(FDC_DRQ | FDC_BUSY);
                                    m_FDC_DRVRegister  &= (byte)~DRV_DRQ;            // turn off high order bit in drive status register
                                    m_FDC_DRVRegister  |= (byte)DRV_INTRQ;           // assert INTRQ at the DRV register when the operation is complete

                                    m_nFDCReading = false;

                                    readBufferIndex = 0;

                                    ClearInterrupt();
                                    activityLedColor = (int)ActivityLedColors.greydot;
                                }
                            }
                        }

                        /* status register bits
                            0x01 = BUSY
                            0x02 = DRQ
                            0x04 = 
                            0x08 = 
                            0x10 = 
                        */

                        d = m_FDC_STATRegister;                      // get controller status
                        m_statusReadsWithoutDataRegisterAccess++;

                        if (m_statusReadsWithoutDataRegisterAccess > 1)
                        {
                            // any read of the status register should clear this flag unless we just finished reading

                            m_FDC_DRVRegister &= (byte)~DRV_INTRQ;      // de-assert INTRQ at the DRV register
                            ClearInterrupt();
                            activityLedColor = (int)ActivityLedColors.greydot;
                        }
                    }
                    break;

                case (int)FDCRegisterOffsets.FDC_DRVREG_OFFSET:
                    {
                        m_statusReadsWithoutDataRegisterAccess++;
                        if ((m_statusReadsWithoutDataRegisterAccess > (m_nBytesToTransfer / 16)) && (m_nBytesToTransfer > 0) && m_nFDCReading)
                        {
                            lock (Program._cpu.buildingDebugLineLock)
                            {
                                m_FDC_STATRegister &= (byte)~FDC_BUSY;     // clear BUSY if data not read
                                activityLedColor = (int)ActivityLedColors.greydot;
                                ClearInterrupt();
                            }
                        }

                        // only return the bits that the user can read. Mask out out all but DRQ and INTRQ (bots 7 and 6)

                        d = (byte)(m_FDC_DRVRegister & 0xC0);
                    }
                    break;

                case (int)FDCRegisterOffsets.FDC_TRKREG_OFFSET:
                case (int)FDCRegisterOffsets.FDC_SECREG_OFFSET:
                    d = m_FDC_TRKRegister;                      // get Track Register
                    break;

                default:
                    d = Program._cpu.ReadFromFirst64K(m);   // memory read
                    break;
            }

            LogFloppyActivity((ushort)(m - m_sBaseAddress), d, true);

            if (previousActivityLedColor != activityLedColor)
            {
                previousActivityLedColor = activityLedColor;

                switch (activityLedColor)
                {
                    case (int)ActivityLedColors.greydot:
                        //Program._mainForm.SetFloppyActivity(nDrive, global::SWTPCemuApp.Properties.Resources.greydot);
                        break;
                    case (int)ActivityLedColors.greendot:
                        //Program._mainForm.SetFloppyActivity(nDrive, global::SWTPCemuApp.Properties.Resources.greendot);
                        break;
                    case (int)ActivityLedColors.reddot:
                        //Program._mainForm.SetFloppyActivity(nDrive, global::SWTPCemuApp.Properties.Resources.reddot);
                        break;
                    default:
                        //Program._mainForm.SetFloppyActivity(nDrive, global::SWTPCemuApp.Properties.Resources.greydot);
                        break;
                }
            }

            return (d);
        }

        public override byte Peek (ushort m)
        {
            byte d;
            int nWhichRegister = m - m_sBaseAddress;
            int nDrive = m_FDC_DRVRegister & 0x03;           // get active drive

            switch (nWhichRegister)
            {
                case (int)FDCRegisterOffsets.FDC_DATAREG_OFFSET:
                    d = m_FDC_DATARegister;
                    break;

                case (int)FDCRegisterOffsets.FDC_STATREG_OFFSET:

                    d = m_FDC_STATRegister;                             // get controller status

                    CheckReadyAndWriteProtect(nDrive);

                    if (!m_nFDCReading && !m_nFDCWriting)           // turn off BUSY if not read/writing
                    {
                        d = (byte)(d & (byte)~FDC_BUSY);
                        //activityLedColor = (int)ActivityLedColors.greydot;
                    }

                    if ((m_statusReadsWithoutDataRegisterAccess >= (m_nBytesToTransfer / 16)) && (m_nBytesToTransfer > 0) && m_nFDCReading)
                    {
                        d = (byte)(d & (byte)~FDC_BUSY);     // clear BUSY if data not read
                        //activityLedColor = (int)ActivityLedColors.greydot;
                    }
                    break;

                case (int)FDCRegisterOffsets.FDC_DRVREG_OFFSET:
                    //d = (byte)(m_FDC_DRVRegister | 0x40);
                    d = (byte)(m_FDC_DRVRegister);
                    break;

                case (int)FDCRegisterOffsets.FDC_TRKREG_OFFSET:
                case (int)FDCRegisterOffsets.FDC_SECREG_OFFSET:
                    d = m_FDC_TRKRegister;                      // get Track Register
                    break;

                default:
                    d = Program._cpu.ReadFromFirst64K(m);   // memory read
                    break;
            }
            return (d);
        }

        public override void Init(int nWhichController, byte[] sMemoryBase, ushort sBaseAddress, int nRow, bool bInterruptEnabled)
        {
            m_nRow = nRow;
            base.Init (nWhichController, sMemoryBase, sBaseAddress, nRow, bInterruptEnabled);
        }

        //void Configure (CString strBoardName, int nBaseAddress, char *szBaseAddress, int sizeofBaseAddress)
        //{
        //    int nReturn;
        //    CConfigFDC pDlg;

        //    pDlg.m_sAddress  = nBaseAddress;
        //    pDlg.m_nFlag     = _bInterruptEnabled;
    
        //    nReturn = pDlg.DoModal ();

        //    if (nReturn == IDOK)
        //    {
        //        sprintf_s (szBaseAddress, sizeofBaseAddress, "0x%04X", pDlg.m_sAddress);
        //        _bInterruptEnabled = pDlg.m_nFlag;
        //    }
        //}
    }
}
