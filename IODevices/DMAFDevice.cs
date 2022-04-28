using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using System.Threading.Tasks;

namespace Memulator
{
    enum NumberOfDrives
    {
        NUMBER_OF_CDS_DRIVES = 4,
        NUMBER_OF_WINCHESTER_DRIVES = 4
    }

    class DMAFDevice : IODevice
    {
        #region Enumerators
        protected enum WDC_COMMAND
        {
            WDC_RDWR = 0,
            WDC_FORMAT = 1,
            WDC_SEEK = 2
        }

        protected enum DMAF_StatusLines
        {
            DMAF_BUSY = 0x01,   // all types
            DMAF_DRQ = 0x02,   // all types
            DMAF_TRKZ = 0x04,   // types 1 and 4
            DMAF_SEEKERR = 0x10,   // tyeps 1 and 4
            DMAF_HDLOADED = 0x20,   // types 1 and 4
            DMAF_WRTPROTECT = 0x40,   // all types
            DMAF_NOTREADY = 0x80    // types 1 and 4
        }

        /*
            DMAF_StatusLines.DMAF_DRQ    |   // 0x02  clear Data Request Bit
            DMAF_StatusLines.DMAF_SEEKERR|   // 0x10  clear SEEK Error Bit
            DMAF_StatusCodes.DMAF_CRCERR |   // 0x08  clear CRC Error Bit
            DMAF_StatusCodes.DMAF_RNF    |   // 0x10  clear Record Not Found Bit
            DMAF_StatusLines.DMAF_BUSY       // 0x01  clear BUSY bit
         */
        protected enum DMAF_StatusCodes
        {
            DMAF_LOST_DATA = 0x04,    // types 2 and 3
            DMAF_CRCERR = 0x08,    // types 2 and 3
            DMAF_RNF = 0x10,    // types 2 and 3
            DMAF_RECORD_TYPE_SPIN_UP = 0x20,    // types 2 and 3
            DMAF_MOTOR_ON = 0x80     // types 2 and 3
        }

        protected enum READ_TRACK_STATES
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
            currentGap2Size = 17,
            currentGap3Size = 33;

        protected int currentReadTrackState = (int)READ_TRACK_STATES.ReadPostIndexGap;
        protected int currentPostIndexGapIndex = 0;
        protected int currentGap2Index = 0;
        protected int currentGap3Index = 0;
        protected int currentDataSectorNumber = 1;

        protected byte[] IDRecordBytes = new byte[5];
        protected ushort crcID = 0;
        protected ushort crcData = 0;

        protected enum WRITE_TRACK_STATES
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

        protected int writeTrackTrack = 0;
        protected int writeTrackSide = 0;
        protected int writeTrackSector = 0;
        protected int writeTrackSize = 0;

        protected int writeTrackMinSector = 0;
        protected int writeTrackMaxSector = 0;

        protected byte[] writeTrackWriteBuffer = new byte[65536];
        protected int writeTrackWriteBufferIndex = 0;

        protected int writeTrackBytesWrittenToSector = 0;
        protected int writeTrackBytesPerSector = 0;
        protected int writeTrackBufferOffset = 0;    // used to put sector data into the track buffer since the sector do not come in order
        protected int writeTrackBufferSize = 0;
        protected int totalBytesThisTrack = 0;    // initial declaration

        protected byte previousByte = 0x00;
        protected int sectorsInWriteTrackBuffer = 0;
        protected int lastFewBytesRead = 0;
        protected byte lastSectorAccessed = 1;

        // The controller board itself

        protected enum DMAF_ContollerLines
        {
            DRV_DRQ = 0x80,
            DMAF_EXTADDR_SELECT_LINES = 0x0F    // extended address select lines
        }

        protected enum WDC_ErrorCodes
        {
            DMAF_WDC_ERROR_BAD_BLOCK    = 0x80,  //    *                                 bit 7 bad block detect
            DMAF_WDC_ERROR_DATA_CRCR    = 0x40,  //    *                                 bit 6 CRC error, data field
            DMAF_WDC_ERROR_ID_CRC       = 0x20,  //    *                                 bit 5 CRC error, ID field
            DMAF_WDC_ERROR_ID_NOTFOUND  = 0x10,  //    *                                 bit 4 ID not found
            DMAF_WDC_ERROR_UNUSED       = 0x08,  //    *                                 bit 3 unused
            DMAF_WDC_ERROR_ABORTED      = 0x04,  //    *                                 bit 2 Aborted Command
            DMAF_WDC_ERROR_TRK_00       = 0x02,  //    *                                 bit 1 TR000 (track zero) error
            DMAF_WDC_ERROR_DAM          = 0x01   //    *                                 bit 0 DAM not found
        }

        protected enum WDC_StatusCodes
        {
            DMAF_WDC_BUSY           = 0x80,   //    *                                 bit 7 busy
            DMAF_WDC_READY          = 0x40,   //    *                                 bit 6 ready
            DMAF_WDC_WRITE_FAULT    = 0x20,   //    *                                 bit 5 write fault
            DMAF_WDC_SEEK_COMPLETE  = 0x10,   //    *                                 bit 4 seek complete
            DMAF_WDC_DATA_REQUEST   = 0x08,   //    *                                 bit 3 data request
            DMAF_WDC_UNUSED_1       = 0x04,   //    *                                 bit 2 unused
            DMAF_WDC_UNUSED_2       = 0x02,   //    *                                 bit 1 unused
            DMAF_WDC_ERROR          = 0x01    //    *                                 bit 0 error (code in wd_error
        }

        //
        //   DMAF3 6844 DMA controller definitions (these all get complemeted data for DMAF2 NOT DMAF3)
        //

        protected enum DMAF_OffsetRegisters
        {
            // DMA registers

            DMAF_DMA_ADDRESS_OFFSET     = 0x00,     // DMA address register (zero)
            DMAF_DMA_DMCNT_OFFSET       = 0x02,     // DMA count register (zero)
            DMAF_DMA_CHANNEL_OFFSET     = 0x10,     // DMA channel control register
            DMAF_DMA_PRIORITY_OFFSET    = 0x14,     // DMA priority control register
            DMAF_DMA_INTERRUPT_OFFSET   = 0x15,     // DMA interrupt control register
            DMAF_DMA_CHAIN_OFFSET       = 0x16,     // DMA data chain register

            //  WD2797 Registers

            DMAF_STATREG_OFFSET         = 0x20,     //      READ
                                                    //  0x01 = BUSY
                                                    //  0x02 = DRQ
                                                    //  0x04 = LOST DATA/BYTE
                                                    //  0x08 = CRC ERROR
                                                    //  0x10 = RNF (Record Not Found
                                                    //  0x20 = RECORD TYPE/SPIN UP
                                                    //  0x40 = WRITE PROTECT
                                                    //  0x80 = MOTOR ON
            DMAF_CMDREG_OFFSET          = 0x20,     //      WRITE
                                                    //  0x0X = RESTORE
                                                    //  0x1X = SEEK
                                                    //  0x2X = STEP
                                                    //  0x3X = STEP     W/TRACK UPDATE
                                                    //  0x4X = STEP IN
                                                    //  0x5X = STEP IN  W/TRACK UPDATE
                                                    //  0x6X = STEP OUT
                                                    //  0x7X = STEP OUT W/TRACK UPDATE
                                                    //  0x8X = READ  SINGLE   SECTOR
                                                    //  0x9X = READ  MULTIPLE SECTORS
                                                    //  0xAX = WRITE SINGLE   SECTOR
                                                    //  0xBX = WRITE MULTIPLE SECTORS
                                                    //  0xCX = READ ADDRESS
                                                    //  0xDX = FORCE INTERRUPT
                                                    //  0xEX = READ TRACK
                                                    //  0xFX = WRITE TRACK
            DMAF_TRKREG_OFFSET          = 0x21,     //  Track Register
            DMAF_SECREG_OFFSET          = 0x22,     //  Sector Register
            DMAF_DATAREG_OFFSET         = 0x23,     //  Data Register
            DMAF_DRVREG_OFFSET          = 0x24,     //  Which Drive is selected

            DMAF2_LATCH_OFFSET          = 0x40,     // extended address latch
            DMAF3_LATCH_OFFSET          = 0x25,     // extended address latch

            //  Winchester Registers

            DMAF_WDC_DATA_OFFSET = 0x30,// WDC data register
            DMAF_WDC_ERROR_OFFSET = 0x31,// WDC error register
            DMAF_WDC_PRECOMP_OFFSET = 0x31,// WDC error register
            DMAF_WDC_SECCNT_OFFSET = 0x32,// WDC sector count (during format)
            DMAF_WDC_SECNUM_OFFSET = 0x33,// WDC sector number
            DMAF_WDC_CYLLO_OFFSET = 0x34,// WDC cylinder (low part)
            DMAF_WDC_CYLHI_OFFSET = 0x35,// WDC cylinder (high part)
            DMAF_WDC_SDH_OFFSET = 0x36,// WDC size/drive/head
            DMAF_WDC_STATUS_OFFSET = 0x37,// WDC status register
            DMAF_WDC_CMD_OFFSET = 0x37,// WDC command register

            //  The 6522 VIA chips is at these addresses

            DMAF_AT_DTB_OFFSET = 0x40,// B-Side Data Register
            DMAF_AT_DTA_OFFSET = 0x41,// A-Side Data Register
            DMAF_AT_DRB_OFFSET = 0x42,// B-Side Direction Register
            DMAF_AT_DRA_OFFSET = 0x43,// A-Side Direction Register
            DMAF_AT_T1C_OFFSET = 0x44,// Timer 1 Counter Register
            DMAF_AT_T1L_OFFSET = 0x46,// Timer 1 Latches
            DMAF_AT_T2C_OFFSET = 0x48,// Timer 2 Counter Register
            DMAF_AT_VSR_OFFSET = 0x4A,// Shift Register
            DMAF_AT_ACR_OFFSET = 0x4B,// Auxillary Control Register
            DMAF_AT_PCR_OFFSET = 0x4C,// Peripheral Control Register
            DMAF_AT_IFR_OFFSET = 0x4D,// Interrupt Flag Register
            DMAF_AT_IER_OFFSET = 0x4E,// Interrupt Enable Register
            DMAF_AT_DXA_OFFSET = 0x4F,// A-Side Data Register

            DMAF_HLD_TOGGLE_OFFSET = 0x50,// 0x50 head load toggle
            DMAF_WD1000_RES_OFFSET = 0x51,// 0x51 winchester software reset
            DMAF_AT_ATR_OFFSET = 0x52,// archive RESET (DOESN'T WORK BECAUSE OF TIMING CONSTRAINTS)
            DMAF_AT_DMC_OFFSET = 0x53,// archive DMA clear
            DMAF_AT_DMP_OFFSET = 0x60,// archive DMA preset

            // CDSdev  fdb $F100 Drive 0
            //         fdb $F300 Drive 1

            // CDS drive 0

            DMAF_CDSCMD0_OFFSET = 0x0100,// CDSCMD rmb 1 disk command register                              
            DMAF_CDSADR0_OFFSET = 0x0101,// CDSADR rmb 2 disk address register                              
            DMAF_CDSFLG0_OFFSET = 0x0103,// CDSFLG rmb 1 disk irq status register                           

            // CDS drive 1

            DMAF_CDSCMD1_OFFSET = 0x0300,// CDSCMD rmb 1 disk command register                              
            DMAF_CDSADR1_OFFSET = 0x0301,// CDSADR rmb 2 disk address register                              
            DMAF_CDSFLG1_OFFSET = 0x0303 // CDSFLG rmb 1 disk irq status register                           
        }

        #endregion

        #region common variables

        public bool latchRegisterIsInverted = false;

        protected Dictionary<ushort, string> registerDescription = new Dictionary<ushort, string>();

        protected int threadSleepTime = 1;
        protected bool _spin = false;

        public MicroTimer _DMAFTimer;
        public MicroTimer DMAFTimer
        {
            get { return _DMAFTimer; }
            set { _DMAFTimer = value; }
        }

        public MicroTimer _DMAFInterruptDelayTimer;
        public MicroTimer DMAFInterruptDelayTimer
        {
            get { return _DMAFInterruptDelayTimer; }
            set { _DMAFInterruptDelayTimer = value; }
        }

        protected int m_nRow;
        protected volatile bool m_nAbort;
        protected bool m_nAllowMultiSectorTransfers = false;

        protected int m_nFDCTrack;
        protected int m_nFDCSector;
        protected bool m_nFDCReading;
        protected int m_nFDCReadPtr;
        protected bool m_nFDCWriting;
        protected int m_nFDCWritePtr;
        protected long m_lFileOffset;
        protected int m_nCurrentSideSelected;     // used for OS9 disk format only
        protected int m_nCurrentDensitySelected;
        protected int m_nInterruptConditionFlag;  // used to store interrupt mode

        protected int m_nBytesToTransfer;
        protected int m_nStatusReads;
        protected int m_nFormatSectorsPerSide;

        protected bool m_nProcessorRunning;
        protected bool m_nDMAFRunning;
        protected int m_nDMAFRate;
        protected bool m_nEnableT1C_Countdown;

        protected byte[] m_caReadBuffer = new byte[16384];
        protected byte[] m_caWriteBuffer = new byte[16384];

        protected byte m_DMAF_DRVRegister = 0;
        protected byte m_DMAF_STATRegister = 0;
        protected byte m_DMAF_CMDRegister = 0;
        protected byte m_DMAF_TRKRegister = 0;
        protected byte m_DMAF_SECRegister = 0;
        protected byte m_DMAF_DATARegister = 0;

        protected byte m_DMAF_LATCHRegister;

        protected class DMAF_DMA_ADDRESS_REGISTER
        {
            public byte lobyte;
            public byte hibyte;
        };

        protected class DMAF_DMA_BYTECNT_REGISTER
        {
            public byte lobyte;
            public byte hibyte;
        };

        protected DMAF_DMA_ADDRESS_REGISTER[] m_DMAF_DMA_ADDRESSRegister = new DMAF_DMA_ADDRESS_REGISTER[4];
        protected DMAF_DMA_BYTECNT_REGISTER[] m_DMAF_DMA_DMCNTRegister = new DMAF_DMA_BYTECNT_REGISTER[4];

        protected byte[] m_DMAF_DMA_CHANNELRegister = new byte[4];

        protected byte m_DMAF_DMA_PRIORITYRegister;
        protected byte m_DMAF_DMA_INTERRUPTRegister;
        protected byte m_DMAF_DMA_CHAINRegister;

        //  The 6522 VIA chips is at these addresses

        protected byte DMAF_AT_DTBRegister;         // 0x40 - B-Side Data Register
        protected byte DMAF_AT_DTARegister;         // 0x41 - A-Side Data Register
        protected byte DMAF_AT_DRBRegister;         // 0x42 - B-Side Direction Register
        protected byte DMAF_AT_DRARegister;         // 0x43 - A-Side Direction Register

        // --- These are 16 bit registers for the DMAF 3 Timer (2 16 bit counter registers and 1 latch register
        // ----------------------------------------------------------------------------------------------------

        protected class DMAF_AT_TC_REGISTER                    // T1C Register (2 bytes each)
        {
            public byte lobyte;
            public byte hibyte;
        };
        protected DMAF_AT_TC_REGISTER[] m_DMAF_AT_TCRegister = new DMAF_AT_TC_REGISTER[2];

        protected class DMAF_AT_TL_REGISTER                    // Timer 1 Latches
        {
            public byte lobyte;
            public byte hibyte;
        };
        protected DMAF_AT_TL_REGISTER m_DMAF_AT_TLRegister = new DMAF_AT_TL_REGISTER();

        // ----------------------------------------------------------------------------------------------------

        protected byte DMAF_AT_VSRRegister;          // 0x4A - Shift Register
        protected byte DMAF_AT_ACRRegister;          // 0x4B - Auxillary Control Register
        protected byte DMAF_AT_PCRRegister;          // 0x4C - Peripheral Control Register
        protected byte DMAF_AT_IFRRegister;          // 0x4D - Interrupt Flag Register
        protected byte DMAF_AT_IERRegister;          // 0x4E - Interrupt Enable Register
        protected byte DMAF_AT_DXARegister;          // 0x4F - A-Side Data Register

        //
        //  latches used exclusively by the archive tape backup and winchester interfaces
        //
        //    auxdecode  equ     board+$50      74LS139 for misc. decodes (all four lines strobed with lda label)

        // Winchester

        protected byte DMAF_HLD_TOGGLERegister;       // 0x50 head load toggle
        protected byte DMAF_WD1000_RESRegister;       // 0x51 winchester software reset

        // Tape

        protected byte DMAF_AT_ATRRegister;           // 0x52    archive RESET (DOESN'T WORK BECAUSE OF TIMING CONSTRAINTS)
        protected byte DMAF_AT_DMCRegister;           // 0x53    archive DMA clear
        protected byte DMAF_AT_DMPRegister;           // 0x60    archive DMA preset


        protected class dir_entry
        {
            public byte[] m_fdn_number = new byte[2];
            public byte[] m_fdn_name = new byte[14];

            int nfdnNumber;
        };

        protected class fdn
        {
            //byte m_ffwdl[2]  ;// rmb 2 forward list link               0x00
            //byte m_fstat     ;// rmb 1 * see below *                   0x02
            //byte m_fdevic[2] ;// rmb 2 device where fdn resides        0x03
            //byte m_fnumbr[2] ;// rmb 2 fdn number (device address)     0x05
            //byte m_frefct    ;// rmb 1 reference count                 0x07
            //byte m_fmode     ;// rmb 1 * see below *                   0x08
            //byte m_facces    ;// rmb 1 * see below *                   0x09
            //byte m_fdirlc    ;// rmb 1 directory entry count           0x0A
            //byte m_fouid[2]  ;// rmb 2 owner's user id                 0x0B
            //byte m_fsize[4]  ;// rmb 4 file size                       0x0D
            //byte m_ffmap[48] ;// rmb MAPSIZ*DSKADS file map            0x10

            public byte m_mode;                         //  file mode
            public byte m_perms;                        //  file security
            public byte m_links;                        //  number of links
            public byte[] m_owner = new byte[2];       //  file owner's ID
            public byte[] m_size = new byte[4];       //  file size
            public byte[,] m_blks = new byte[13, 3];   //  block list
            public byte[] m_time = new byte[4];       //  file time
            public byte[] m_fd_pad = new byte[12];      //  padding
        };

        protected class UniFLEX_SIR
        {
            public byte m_supdt;                    //rmb 1       sir update flag                                         0x0200        -> 00 
            public byte m_swprot;                   //rmb 1       mounted read only flag                                  0x0201        -> 00 
            public byte m_slkfr;                    //rmb 1       lock for free list manipulation                         0x0202        -> 00 
            public byte m_slkfdn;                   //rmb 1       lock for fdn list manipulation                          0x0203        -> 00 
            public byte[] m_sintid = new byte[4];   //rmb 4       initializing system identifier                          0x0204        -> 00 
            public byte[] m_scrtim = new byte[4];   //rmb 4       creation time                                           0x0208        -> 11 44 F3 FC
            public byte[] m_sutime = new byte[4];   //rmb 4       date of last update                                     0x020C        -> 11 44 F1 51
            public byte[] m_sszfdn = new byte[2];   //rmb 2       size in blocks of fdn list                              0x0210        -> 00 4A          = 74
            public byte[] m_ssizfr = new byte[3];   //rmb 3       size in blocks of volume                                0x0212        -> 00 08 1F       = 2079
            public byte[] m_sfreec = new byte[3];   //rmb 3       total free blocks                                       0x0215        -> 00 04 9C       = 
            public byte[] m_sfdnc = new byte[2];    //rmb 2       free fdn count                                          0x0218        -> 01 B0
            public byte[] m_sfname = new byte[14];  //rmb 14      file system name                                        0x021A        -> 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
            public byte[] m_spname = new byte[14];  //rmb 14      file system pack name                                   0x0228        -> 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
            public byte[] m_sfnumb = new byte[2];   //rmb 2       file system number                                      0x0236        -> 00 00
            public byte[] m_sflawc = new byte[2];   //rmb 2       flawed block count                                      0x0238        -> 00 00
            public byte m_sdenf;                    //rmb 1       density flag - 0=single                                 0x023A        -> 01
            public byte m_ssidf;                    //rmb 1       side flag - 0=single                                    0x023B        -> 01
            public byte[] m_sswpbg = new byte[3];   //rmb 3       swap starting block number                              0x023C        -> 00 08 20
            public byte[] m_sswpsz = new byte[2];   //rmb 2       swap block count                                        0x023F        -> 01 80
            public byte m_s64k;                     //rmb 1       non-zero if swap block count is multiple of 64K         0x0241        -> 00
            public byte[] m_swinc = new byte[11];   //rmb 11      Winchester configuration info                           0x0242        -> 00 00 00 00 00 00 2A 00 99 00 9A
            public byte[] m_sspare = new byte[11];  //rmb 11      spare bytes - future use                                0x024D        -> 00 9B 00 9C 00 9D 00 9E 00 9F 00
            public byte m_snfdn;                    //rmb 1       number of in core fdns                                  0x0258        -> A0           *snfdn * 2 = 320
            public byte[] m_scfdn = new byte[512];  //rmb CFDN*2  in core free fdns                                       0x0259        variable (*snfdn * 2)
            public byte[] m_snfree;                 //rmb 1       number of in core free blocks                           0x03B9        -> 03
            public byte[] m_sfree = new byte[16384];//rmb         CDBLKS*DSKADS in core free blocks                       0x03BA        -> 

            public fdn[] m_fdn = new fdn[592];
        };

        protected UniFLEX_SIR[] m_drive_SIRs = new UniFLEX_SIR[4];

        protected enum DeviceInterruptMask
        {
            DEVICE_INT_MASK_DMA1 = 1,
            DEVICE_INT_MASK_DMA2 = 2,
            DEVICE_INT_MASK_DMA3 = 4,
            DEVICE_INT_MASK_DMA4 = 8,
            DEVICE_INT_MASK_FDC = 16,
            DEVICE_INT_MASK_WDC = 32,
            DEVICE_INT_MASK_CDS = 64,
            DEVICE_INT_MASK_TAPE = 128,
            DEVICE_INT_MASK_TIMER = 256
        }

        protected int m_nInterruptDelay = 1;
        protected int m_nBoardInterruptRegister;
        protected int m_nInterruptingDevice;

        // permissions

        protected enum fdn_perms
        {
            UREAD = 0x01,   // owner read
            UWRITE = 0x02,   // owner write
            UEXEC = 0x04,   // owner execute
            OREAD = 0x08,   // other read
            OWRITE = 0x10,   // other write
            OEXEC = 0x20,   // other execute
            SUID = 0x40    // set user ID for execute
        };

        //* stat codes

        protected enum fdn_stat_codes
        {
            FLOCK = 0x01,         //  %00000001,  //fdn is locked
            FMOD = 0x02,         //  %00000010,  //fdn has been modified
            FTEXT = 0x04,         //  %00000100,  //this is a text segment
            FMNT = 0x08,         //  %00001000,  //fdn is mounted on
            FWLCK = 0x10          //  %00010000   //task awaiting lock
        };

        //* mode codes

        protected enum fdn_mode_codes
        {
            FBUSY = 0x01,         //  %00000001 fdn is used (busy)
            FSBLK = 0x02,         //  %00000010 block special file
            FSCHR = 0x04,         //  %00000100 character special file
            FSDIR = 0x08,         //  %00001000 directory type file
            FPRDF = 0x10,         //  %00010000 pipe read flag
            FPWRF = 0x20          //  %00100000 pipe write flag
        };

        //* access codes

        protected enum fdn_access_codes
        {
            FACUR = 0x01,         //  %00000001 user read
            FACUW = 0x02,         //  %00000010 user write
            FACUE = 0x04,         //  %00000100 user execute
            FACOR = 0x08,         //  %00001000 other read
            FACOW = 0x10,         //  %00010000 other write
            FACOE = 0x20,         //  %00100000 other execute
            FXSET = 0x40          //  %01000000 uid execute set
        };

        #endregion

        protected void SetRegisterDescriptions (ushort latchAddress)
        {
            // set up register descriptions dictionary
            //
            // DMA registers

            registerDescription.Add(0x0000, "DMA address register 0 hi byte");
            registerDescription.Add(0x0001, "DMA address register 0 lo bytye");
            registerDescription.Add(0x0002, "DMA count register 0 hi byte");
            registerDescription.Add(0x0003, "DMA count register 0 lo byte");
            registerDescription.Add(0x0004, "DMA address register 1 hi byte");
            registerDescription.Add(0x0005, "DMA address register 1 lo byte");
            registerDescription.Add(0x0006, "DMA count register 1 hi byte");
            registerDescription.Add(0x0007, "DMA count register 1 lo byte");
            registerDescription.Add(0x0008, "DMA address register 2 hi byte");
            registerDescription.Add(0x0009, "DMA address register 2 lo byte");
            registerDescription.Add(0x000A, "DMA count register 2 hi byte");
            registerDescription.Add(0x000B, "DMA count register 2 lo byte");
            registerDescription.Add(0x000C, "DMA address register 3 hi byte");
            registerDescription.Add(0x000D, "DMA address register 3 lo byte");
            registerDescription.Add(0x000E, "DMA count register 3 hi byte");
            registerDescription.Add(0x000F, "DMA count register 3 lo byte");
            registerDescription.Add(0x0010, "DMA channel control register 0");
            registerDescription.Add(0x0011, "DMA channel control register 1");
            registerDescription.Add(0x0012, "DMA channel control register 2");
            registerDescription.Add(0x0013, "DMA channel control register 3");
            registerDescription.Add(0x0014, "DMA priority control register");
            registerDescription.Add(0x0015, "DMA interrupt control register");
            registerDescription.Add(0x0016, "DMA data chain register");

            //  WD2797 Registers

            registerDescription.Add(0x0020, "READ - STATUS / WRITE - COMMAND");
            registerDescription.Add(0x0021, "Track Register");
            registerDescription.Add(0x0022, "Sector Register");
            registerDescription.Add(0x0023, "Data Register");
            registerDescription.Add(0x0024, "Which Drive is selected");


            //  Latch for Extended memory access    

            registerDescription.Add(latchAddress, "extended address latch");

            // only do these if this is a DMAF-3

            if (latchAddress == 0x0025)
            {
                //  Winchester Registers


                registerDescription.Add(0x0030, "WDC data register");
                registerDescription.Add(0x0031, "WDC error register on read - WDC precomp register on write");
                registerDescription.Add(0x0032, "WDC sector count (during format)");
                registerDescription.Add(0x0033, "WDC sector number");
                registerDescription.Add(0x0034, "WDC cylinder (low part)");
                registerDescription.Add(0x0035, "WDC cylinder (high part)");
                registerDescription.Add(0x0036, "WDC size/drive/head");
                registerDescription.Add(0x0037, "READ - WDC status register / WRITE - WDC command register");

                //  The 6522 VIA chips is at these addresses

                registerDescription.Add(0x0040, "B-Side Data Register");
                registerDescription.Add(0x0041, "A-Side Data Register");
                registerDescription.Add(0x0042, "B-Side Direction Register");
                registerDescription.Add(0x0043, "A-Side Direction Register");
                registerDescription.Add(0x0044, "Timer 1 Counter Register lo byte");
                registerDescription.Add(0x0045, "Timer 1 Counter Register hi byte");
                registerDescription.Add(0x0046, "Timer 1 Latches lo byte");
                registerDescription.Add(0x0047, "Timer 1 Latches hi byte");
                registerDescription.Add(0x0048, "Timer 2 Counter Register lo byte");
                registerDescription.Add(0x0049, "Timer 2 Counter Register hi byte");
                registerDescription.Add(0x004A, "Shift Register");
                registerDescription.Add(0x004B, "Auxillary Control Register");
                registerDescription.Add(0x004C, "Peripheral Control Register");
                registerDescription.Add(0x004D, "Interrupt Flag Register");
                registerDescription.Add(0x004E, "Interrupt Enable Register");
                registerDescription.Add(0x004F, "A-Side Data Register");

                registerDescription.Add(0x0050, "head load toggle");
                registerDescription.Add(0x0051, "winchester software reset");
                registerDescription.Add(0x0052, "archive RESET (DOESN'T WORK BECAUSE OF TIMING CONSTRAINTS)");
                registerDescription.Add(0x0053, "archive DMA clear");
                registerDescription.Add(0x0060, "archive DMA preset");

                // CDSdev  fdb $F100 Drive 0
                //         fdb $F300 Drive 1

                // CDS drive 0

                registerDescription.Add(0x0100, "CDSCMD disk command register");
                registerDescription.Add(0x0101, "CDSADR disk address register");
                registerDescription.Add(0x0103, "CDSFLG disk irq status register");

                // CDS drive 1

                registerDescription.Add(0x0300, "CDSCMD disk command register");
                registerDescription.Add(0x0301, "CDSADR disk address register");
                registerDescription.Add(0x0303, "CDSFLG disk irq status register");
            }
        }

        protected new void Dispose()
        {
            _DMAFInterruptDelayTimer.Stop();
            _DMAFTimer.Stop();

            _DMAFInterruptDelayTimer = null;
            _DMAFTimer = null;
        }

        protected void memset(byte[] dest, byte c, int count)
        {
            for (int i = 0; i < dest.Length && i < count; i++)
            {
                dest[i] = c;
            }
        }

        protected void memcpy(byte[] dst, byte[] src, int count)
        {
            for (int i = 0; i < dst.Length && i < src.Length && i < count; i++)
            {
                dst[i] = src[i];
            }
        }

        // Start the interrupt delay to set the interrupt

        public void StartDMAFInterruptDelayTimer(int rate, int nDeviceInterruptMask)
        {
            m_nInterruptingDevice |= nDeviceInterruptMask;
            m_nBoardInterruptRegister |= nDeviceInterruptMask;

            // start a one-shot timer to introduce a delay before we set the interrupt on a WDC read or write command.

            SetInterrupt(_spin);

            _DMAFInterruptDelayTimer.Interval = rate;
            _DMAFInterruptDelayTimer.Start();

            return;

        }

        public void DMAF_TimerProc(object sender, MicroTimerEventArgs timerEventArgs)
        {
            byte c, d;
            uint e;

            // if the processor is not running - do nothing because we may be unstable

            if (m_nProcessorRunning)
            {
                if (m_nDMAFRunning)
                {
                    if (m_nEnableT1C_Countdown)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            c = m_DMAF_AT_TCRegister[i].hibyte;
                            d = m_DMAF_AT_TCRegister[i].lobyte;

                            e = (uint)((c * 256) + d);

                            --e;

                            c = (byte)(e / 256);
                            d = (byte)(e % 256);

                            if (e == 0)
                            {
                                m_DMAF_AT_TCRegister[i].hibyte = m_DMAF_AT_TLRegister.hibyte;
                                m_DMAF_AT_TCRegister[i].lobyte = m_DMAF_AT_TLRegister.lobyte;

                                DMAF_AT_IFRRegister = (byte)(DMAF_AT_IFRRegister | 0x40);
                                if ((DMAF_AT_IERRegister & (byte)0x40) != 0)
                                {
                                    DMAF_AT_IFRRegister = (byte)(DMAF_AT_IFRRegister | 0x80);

                                    // the latch at $F040 is only for the FDC and has no effect on the Timer
                                    // so do not check bit 4 in the latch

                                    if (_bInterruptEnabled)
                                    {
                                        StartDMAFInterruptDelayTimer(m_nInterruptDelay, (int)DeviceInterruptMask.DEVICE_INT_MASK_TIMER);
                                        if (Program._cpu != null)
                                        {
                                            if ((Program._cpu.InWait || Program._cpu.InSync) && Program.CpuThread.ThreadState == System.Threading.ThreadState.Suspended)
                                            {
                                                try
                                                {
                                                    Program.CpuThread.Resume();
                                                }
                                                catch (ThreadStateException tse)
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
                                m_DMAF_AT_TCRegister[i].hibyte = c;
                                m_DMAF_AT_TCRegister[i].lobyte = d;
                            }
                        }
                        //m_nDMAFTimer.Change(0, m_nDMAFRate);  // restart the timer <- doesn't require restart - it just runs and runs and runs.......
                    }
                }

                //// if any of the timer interrupts are enabled, set interrupt bit in the processor.
                //
                //if ((m_DMAFControlRegister[0] & 0x40) || (m_DMAFControlRegister[1] & 0x40) || (m_DMAFControlRegister[2] & 0x40))
                //{
                //    if (_bInterruptEnabled)
                //    {
                //        StartDMAFInterruptDelayTimer (m_nInterruptDelay);
                //        if (Program._cpu != null)
                //        {
                //            if ((Program._cpu.InWait || Program._cpu.InSync) && Program._cpuThread.ThreadState == ThreadState.Suspended)
                //            {
                //                try
                //                {
                //                    Program._cpuThread.Resume();
                //                }
                //                catch (ThreadStateException e)
                //                {
                //                    // do nothing if thread is not suspended
                //                }
                //            }
                //        }
                //    }
                //}
            }
        }

        public void StartDMAFTimer(int rate)
        {
            // Only the processor can start the timer

            m_nProcessorRunning = true;

            // so start it already

            //m_nDMAFTimer.Change(Timeout.Infinite, Timeout.Infinite);   //  timeKillEvent (m_nDMAFTimer);
            //m_nDMAFTimer.Change(0, rate);                              //  m_nDMAFTimer = timeSetEvent(rate, 1, DMAF_TIMER, (DWORD) this, TIME_PERIODIC);
            _DMAFTimer.Interval = rate;
            _DMAFTimer.Start();
        }

        public void ClearInterrupt(int nDeviceInterruptMask)
        {
            m_nBoardInterruptRegister &= ~nDeviceInterruptMask;

            if (m_nBoardInterruptRegister == 0)
                base.ClearInterrupt();
        }

        public void StopDMAFTimer()
        {
            if (_DMAFTimer != null)
            {
                ClearInterrupt((int)DeviceInterruptMask.DEVICE_INT_MASK_TIMER);
                //m_nDMAFTimer.Change(Timeout.Infinite, Timeout.Infinite);           //timeKillEvent (m_nDMAFTimer);
                //m_nDMAFTimer = null;
                _DMAFTimer.Stop();
            }
        }


    }
}
