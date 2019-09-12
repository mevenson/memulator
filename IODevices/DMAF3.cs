using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;

using System.Threading;
using System.IO;

namespace Memulator
{
    #region Enumerators
    enum WDC_COMMAND
    {
        WDC_RDWR    = 0,
        WDC_FORMAT  = 1,
        WDC_SEEK    = 2
    }

    enum DMAF3_StatusLines
    {
        DMAF3_BUSY       = 0x01,   // all types
        DMAF3_DRQ        = 0x02,   // all types
        DMAF3_TRKZ       = 0x04,   // types 1 and 4
        DMAF3_SEEKERR    = 0x10,   // tyeps 1 and 4
        DMAF3_HDLOADED   = 0x20,   // types 1 and 4
        DMAF3_WRTPROTECT = 0x40,   // all types
        DMAF3_NOTREADY   = 0x80    // types 1 and 4
    }

    /*
        DMAF3_StatusLines.DMAF3_DRQ    |   // 0x02  clear Data Request Bit
        DMAF3_StatusLines.DMAF3_SEEKERR|   // 0x10  clear SEEK Error Bit
        DMAF3_StatusCodes.DMAF3_CRCERR |   // 0x08  clear CRC Error Bit
        DMAF3_StatusCodes.DMAF3_RNF    |   // 0x10  clear Record Not Found Bit
        DMAF3_StatusLines.DMAF3_BUSY       // 0x01  clear BUSY bit
     */
    enum DMAF3_StatusCodes
    {
        DMAF3_LOST_DATA             = 0x04,    // types 2 and 3
        DMAF3_CRCERR                = 0x08,    // types 2 and 3
        DMAF3_RNF                   = 0x10,    // types 2 and 3
        DMAF3_RECORD_TYPE_SPIN_UP   = 0x20,    // types 2 and 3
        DMAF3_MOTOR_ON              = 0x80     // types 2 and 3
    }

    // The controller board itself

    enum DMAF3_ContollerLines
    {
        DRV_DRQ                     = 0x80,
        DMAF3_EXTADDR_SELECT_LINES  = 0x0F    // extended address select lines
    }

    enum WDC_ErrorCodes
    {
        DMAF3_WDC_ERROR_BAD_BLOCK   = 0x80,  //    *                                 bit 7 bad block detect
        DMAF3_WDC_ERROR_DATA_CRCR   = 0x40,  //    *                                 bit 6 CRC error, data field
        DMAF3_WDC_ERROR_ID_CRC      = 0x20,  //    *                                 bit 5 CRC error, ID field
        DMAF3_WDC_ERROR_ID_NOTFOUND = 0x10,  //    *                                 bit 4 ID not found
        DMAF3_WDC_ERROR_UNUSED      = 0x08,  //    *                                 bit 3 unused
        DMAF3_WDC_ERROR_ABORTED     = 0x04,  //    *                                 bit 2 Aborted Command
        DMAF3_WDC_ERROR_TRK_00      = 0x02,  //    *                                 bit 1 TR000 (track zero) error
        DMAF3_WDC_ERROR_DAM         = 0x01   //    *                                 bit 0 DAM not found
    }

    enum WDC_StatusCodes
    {
        DMAF3_WDC_BUSY              = 0x80,   //    *                                 bit 7 busy
        DMAF3_WDC_READY             = 0x40,   //    *                                 bit 6 ready
        DMAF3_WDC_WRITE_FAULT       = 0x20,   //    *                                 bit 5 write fault
        DMAF3_WDC_SEEK_COMPLETE     = 0x10,   //    *                                 bit 4 seek complete
        DMAF3_WDC_DATA_REQUEST      = 0x08,   //    *                                 bit 3 data request
        DMAF3_WDC_UNUSED_1          = 0x04,   //    *                                 bit 2 unused
        DMAF3_WDC_UNUSED_2          = 0x02,   //    *                                 bit 1 unused
        DMAF3_WDC_ERROR             = 0x01    //    *                                 bit 0 error (code in wd_error
    }

    //
    //   DMAF3 6844 DMA controller definitions (these all get complemeted data for DMAF2 NOT DMAF3)
    //

    enum DMAF3_OffsetRegisters
    {
        // DMA registers

        DMAF3_DMA_ADDRESS_OFFSET     = 0x00,// DMA address register (zero)
        DMAF3_DMA_DMCNT_OFFSET       = 0x02,// DMA count register (zero)
        DMAF3_DMA_CHANNEL_OFFSET     = 0x10,// DMA channel control register
        DMAF3_DMA_PRIORITY_OFFSET    = 0x14,// DMA priority control register
        DMAF3_DMA_INTERRUPT_OFFSET   = 0x15,// DMA interrupt control register
        DMAF3_DMA_CHAIN_OFFSET       = 0x16,// DMA data chain register

        //  WD2797 Registers
        
        DMAF3_STATREG_OFFSET         =0x20, //      READ
                                            //  0x01 = BUSY
                                            //  0x02 = DRQ
                                            //  0x04 = LOST DATA/BYTE
                                            //  0x08 = CRC ERROR
                                            //  0x10 = RNF (Record Not Found
                                            //  0x20 = RECORD TYPE/SPIN UP
                                            //  0x40 = WRITE PROTECT
                                            //  0x80 = MOTOR ON
        DMAF3_CMDREG_OFFSET          = 0x20,//      WRITE
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
        DMAF3_TRKREG_OFFSET          = 0x21,//  Track Register
        DMAF3_SECREG_OFFSET          = 0x22,//  Sector Register
        DMAF3_DATAREG_OFFSET         = 0x23,//  Data Register
        DMAF3_DRVREG_OFFSET          = 0x24,//  Which Drive is selected

        //  Latch for Extended memory access

        DMAF3_LATCH_OFFSET           = 0x25,// extended address latch

        //  Winchester Registers

        DMAF3_WDC_DATA_OFFSET        = 0x30,// WDC data register
        DMAF3_WDC_ERROR_OFFSET       = 0x31,// WDC error register
        DMAF3_WDC_PRECOMP_OFFSET     = 0x31,// WDC error register
        DMAF3_WDC_SECCNT_OFFSET      = 0x32,// WDC sector count (during format)
        DMAF3_WDC_SECNUM_OFFSET      = 0x33,// WDC sector number
        DMAF3_WDC_CYLLO_OFFSET       = 0x34,// WDC cylinder (low part)
        DMAF3_WDC_CYLHI_OFFSET       = 0x35,// WDC cylinder (high part)
        DMAF3_WDC_SDH_OFFSET         = 0x36,// WDC size/drive/head
        DMAF3_WDC_STATUS_OFFSET      = 0x37,// WDC status register
        DMAF3_WDC_CMD_OFFSET         = 0x37,// WDC command register

        //  The 6522 VIA chips is at these addresses

        DMAF3_AT_DTB_OFFSET          = 0x40,// B-Side Data Register
        DMAF3_AT_DTA_OFFSET          = 0x41,// A-Side Data Register
        DMAF3_AT_DRB_OFFSET          = 0x42,// B-Side Direction Register
        DMAF3_AT_DRA_OFFSET          = 0x43,// A-Side Direction Register
        DMAF3_AT_T1C_OFFSET          = 0x44,// Timer 1 Counter Register
        DMAF3_AT_T1L_OFFSET          = 0x46,// Timer 1 Latches
        DMAF3_AT_T2C_OFFSET          = 0x48,// Timer 2 Counter Register
        DMAF3_AT_VSR_OFFSET          = 0x4A,// Shift Register
        DMAF3_AT_ACR_OFFSET          = 0x4B,// Auxillary Control Register
        DMAF3_AT_PCR_OFFSET          = 0x4C,// Peripheral Control Register
        DMAF3_AT_IFR_OFFSET          = 0x4D,// Interrupt Flag Register
        DMAF3_AT_IER_OFFSET          = 0x4E,// Interrupt Enable Register
        DMAF3_AT_DXA_OFFSET          = 0x4F,// A-Side Data Register

        DMAF3_HLD_TOGGLE_OFFSET      = 0x50,// 0x50 head load toggle
        DMAF_WD1000_RES_OFFSET       = 0x51,// 0x51 winchester software reset
        DMAF3_AT_ATR_OFFSET          = 0x52,// archive RESET (DOESN'T WORK BECAUSE OF TIMING CONSTRAINTS)
        DMAF3_AT_DMC_OFFSET          = 0x53,// archive DMA clear
        DMAF3_AT_DMP_OFFSET          = 0x60,// archive DMA preset

        // CDSdev  fdb $F100 Drive 0
        //         fdb $F300 Drive 1

        // CDS drive 0
    
        DMAF3_CDSCMD0_OFFSET         = 0x0100,// CDSCMD rmb 1 disk command register                              
        DMAF3_CDSADR0_OFFSET         = 0x0101,// CDSADR rmb 2 disk address register                              
        DMAF3_CDSFLG0_OFFSET         = 0x0103,// CDSFLG rmb 1 disk irq status register                           

        // CDS drive 1

        DMAF3_CDSCMD1_OFFSET         = 0x0300,// CDSCMD rmb 1 disk command register                              
        DMAF3_CDSADR1_OFFSET         = 0x0301,// CDSADR rmb 2 disk address register                              
        DMAF3_CDSFLG1_OFFSET         = 0x0303 // CDSFLG rmb 1 disk irq status register                           
    }

    enum NumberOfDrives
    {
        NUMBER_OF_CDS_DRIVES = 4,
        NUMBER_OF_WINCHESTER_DRIVES = 4
    }
    #endregion
    
    class HARD_DRIVE_PARAMETERS
    {
        public int nCylinders;
        public int nHeads;
        public int nSectorsPerTrack;
        public int nBytesPerSector;
    }

    class DMAF3 : IODevice
    {
        #region variables

        bool _spin = false;

        MicroTimer _DMAF3Timer;
        public MicroTimer DMAF3Timer
        {
            get { return _DMAF3Timer; }
            set { _DMAF3Timer = value; }
        }

        MicroTimer _DMAF3InterruptDelayTimer;
        public MicroTimer DMAF3InterruptDelayTimer
        {
            get { return _DMAF3InterruptDelayTimer; }
            set { _DMAF3InterruptDelayTimer = value; }
        }

        // CDS stuff

        Stream [] m_fpCDS                       = new Stream[(int)NumberOfDrives.NUMBER_OF_CDS_DRIVES];
        string [] m_cCDSDrivePathName           = new string[(int)NumberOfDrives.NUMBER_OF_CDS_DRIVES];
        string [] m_cCDSDriveType               = new string[(int)NumberOfDrives.NUMBER_OF_CDS_DRIVES];
        HARD_DRIVE_PARAMETERS [] m_hdpCDS       = new HARD_DRIVE_PARAMETERS[(int)NumberOfDrives.NUMBER_OF_CDS_DRIVES];

        // Winchester (DMAF3) stuff

        Stream [] m_fpWinchester                = new Stream[(int)NumberOfDrives.NUMBER_OF_WINCHESTER_DRIVES];
        string [] m_cWinchesterDrivePathName    = new string[(int)NumberOfDrives.NUMBER_OF_WINCHESTER_DRIVES];
        string [] m_cWinchesterDriveType        = new string[(int)NumberOfDrives.NUMBER_OF_WINCHESTER_DRIVES];
        HARD_DRIVE_PARAMETERS [] m_hdpWinchester= new HARD_DRIVE_PARAMETERS[(int)NumberOfDrives.NUMBER_OF_WINCHESTER_DRIVES];


        int           m_nRow;
        volatile bool m_nAbort;
        bool          m_nAllowMultiSectorTransfers = false;

        int     m_nFDCTrack;
        int     m_nFDCSector;
        bool    m_nFDCReading;
        int     m_nFDCReadPtr;
        bool    m_nFDCWriting;
        int     m_nFDCWritePtr;
        long    m_lFileOffset;
        int     m_nCurrentSideSelected;     // used for OS9 disk format only
        int     m_nCurrentDensitySelected;
        int     m_nInterruptConditionFlag;  // used to store interrupt mode

        int    m_nBytesToTransfer;
        int    m_nStatusReads;
        int    m_nFormatSectorsPerSide;

        bool   m_nProcessorRunning;
        bool   m_nDMAF3Running;
        int    m_nDMAF3Rate;
        bool   m_nEnableT1C_Countdown;

        int    m_HDCylinder;
        int    m_HDCHead;
        int    m_HDSector;

        byte[] m_caReadBuffer = new byte[16384];
        byte[] m_caWriteBuffer= new byte[16384];

        byte   m_DMAF3_DRVRegister;
        byte   m_DMAF3_STATRegister;
        byte   m_DMAF3_CMDRegister;
        byte   m_DMAF3_TRKRegister;
        byte   m_DMAF3_SECRegister;
        byte   m_DMAF3_DATARegister;

        byte   m_DMAF3_LATCHRegister;

        TextWriter m_fpDMAActivity = null;
        //FILE   *m_fpDMAActivity;

        class DMAF3_DMA_ADDRESS_REGISTER
        {
            public  byte lobyte;
            public  byte hibyte;
        };

        class DMAF3_DMA_BYTECNT_REGISTER
        {
            public  byte lobyte;
            public  byte hibyte;
        };

        DMAF3_DMA_ADDRESS_REGISTER [] m_DMAF3_DMA_ADDRESSRegister = new DMAF3_DMA_ADDRESS_REGISTER[4];
        DMAF3_DMA_BYTECNT_REGISTER [] m_DMAF3_DMA_DMCNTRegister   = new DMAF3_DMA_BYTECNT_REGISTER[4];

        byte[] m_DMAF3_DMA_CHANNELRegister = new byte[4];

        byte   m_DMAF3_DMA_PRIORITYRegister;
        byte   m_DMAF3_DMA_INTERRUPTRegister;
        byte   m_DMAF3_DMA_CHAINRegister;

        byte   DMAF3_WDC_DATARegister;       // WDC data register
        byte   DMAF3_WDC_ERRORRegister;      // WDC error register
        byte   DMAF3_WDC_PrecompRegister;    // WDC precomp register
        byte   DMAF3_WDC_SECCNTRegister;     // WDC sector count (during format)
        byte   DMAF3_WDC_SECNUMRegister;     // WDC sector number
        byte   DMAF3_WDC_CYLLORegister;      // WDC cylinder (low part)
        byte   DMAF3_WDC_CYLHIRegister;      // WDC cylinder (high part)
        byte   DMAF3_WDC_SDHRegister;        // WDC size/drive/head
        byte   DMAF3_WDC_STATUSRegister;     // WDC status register
        byte   DMAF3_WDC_CMDRegister;        // WDC command register

        //  The 6522 VIA chips is at these addresses

        byte   DMAF3_AT_DTBRegister;         // 0x40 - B-Side Data Register
        byte   DMAF3_AT_DTARegister;         // 0x41 - A-Side Data Register
        byte   DMAF3_AT_DRBRegister;         // 0x42 - B-Side Direction Register
        byte   DMAF3_AT_DRARegister;         // 0x43 - A-Side Direction Register

        // --- These are 16 bit registers for the DMAF 3 Timer (2 16 bit counter registers and 1 latch register
        // ----------------------------------------------------------------------------------------------------

        class DMAF3_AT_TC_REGISTER                    // T1C Register (2 bytes each)
        {
            public byte lobyte;
            public byte hibyte;
        };
        DMAF3_AT_TC_REGISTER [] m_DMAF3_AT_TCRegister = new DMAF3_AT_TC_REGISTER[2];

        class DMAF3_AT_TL_REGISTER                    // Timer 1 Latches
        {
            public byte lobyte;
            public byte hibyte;
        };
        DMAF3_AT_TL_REGISTER m_DMAF3_AT_TLRegister = new DMAF3_AT_TL_REGISTER();

        // ----------------------------------------------------------------------------------------------------

        byte   DMAF3_AT_VSRRegister;          // 0x4A - Shift Register
        byte   DMAF3_AT_ACRRegister;          // 0x4B - Auxillary Control Register
        byte   DMAF3_AT_PCRRegister;          // 0x4C - Peripheral Control Register
        byte   DMAF3_AT_IFRRegister;          // 0x4D - Interrupt Flag Register
        byte   DMAF3_AT_IERRegister;          // 0x4E - Interrupt Enable Register
        byte   DMAF3_AT_DXARegister;          // 0x4F - A-Side Data Register

        //
        //  latches used exclusively by the archive tape backup and winchester interfaces
        //
        //    auxdecode  equ     board+$50      74LS139 for misc. decodes (all four lines strobed with lda label)

        // Winchester

        byte   DMAF3_HLD_TOGGLERegister;       // 0x50 head load toggle
        byte   DMAF_WD1000_RESRegister;        // 0x51 winchester software reset

        // Tape

        byte   DMAF3_AT_ATRRegister;           // 0x52    archive RESET (DOESN'T WORK BECAUSE OF TIMING CONSTRAINTS)
        byte   DMAF3_AT_DMCRegister;           // 0x53    archive DMA clear
        byte   DMAF3_AT_DMPRegister;           // 0x60    archive DMA preset

        byte   DMAF3_CDSCMD0Register;          // 0x0100
        byte   DMAF3_CDSADR0Register;          // 0x0101
        byte   DMAF3_CDSFLG0Register;          // 0x0103

        byte   DMAF3_CDSCMD1Register;          // 0x0300
        byte   DMAF3_CDSADR1Register;          // 0x0301
        byte   DMAF3_CDSFLG1Register;          // 0x0103

        class dir_entry
        {
            public byte [] m_fdn_number = new byte[2];
            public byte [] m_fdn_name   = new byte[14];

            int nfdnNumber;
        };

        class fdn
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

            public byte     m_mode;                     //  file mode
            public byte     m_perms;                    //  file security
            public byte     m_links;                    //  number of links
            public byte []  m_owner = new byte[2];      //  file owner's ID
            public byte []  m_size  = new byte[4];      //  file size
            public byte [,] m_blks  = new byte[13,3];   //  block list
            public byte []  m_time  = new byte[4];      //  file time
            public byte []  m_fd_pad= new byte[12];     //  padding
        };

        class UniFLEX_SIR
        {
            public byte     m_supdt                     ;//rmb 1       sir update flag                                         0x0200        -> 00 
            public byte     m_swprot                    ;//rmb 1       mounted read only flag                                  0x0201        -> 00 
            public byte     m_slkfr                     ;//rmb 1       lock for free list manipulation                         0x0202        -> 00 
            public byte     m_slkfdn                    ;//rmb 1       lock for fdn list manipulation                          0x0203        -> 00 
            public byte []  m_sintid = new byte[4]      ;//rmb 4       initializing system identifier                          0x0204        -> 00 
            public byte []  m_scrtim = new byte[4]      ;//rmb 4       creation time                                           0x0208        -> 11 44 F3 FC
            public byte []  m_sutime = new byte[4]      ;//rmb 4       date of last update                                     0x020C        -> 11 44 F1 51
            public byte []  m_sszfdn = new byte[2]      ;//rmb 2       size in blocks of fdn list                              0x0210        -> 00 4A          = 74
            public byte []  m_ssizfr = new byte[3]      ;//rmb 3       size in blocks of volume                                0x0212        -> 00 08 1F       = 2079
            public byte []  m_sfreec = new byte[3]      ;//rmb 3       total free blocks                                       0x0215        -> 00 04 9C       = 
            public byte []  m_sfdnc  = new byte[2]      ;//rmb 2       free fdn count                                          0x0218        -> 01 B0
            public byte []  m_sfname = new byte[14]     ;//rmb 14      file system name                                        0x021A        -> 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
            public byte []  m_spname = new byte[14]     ;//rmb 14      file system pack name                                   0x0228        -> 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
            public byte []  m_sfnumb = new byte[2]      ;//rmb 2       file system number                                      0x0236        -> 00 00
            public byte []  m_sflawc = new byte[2]      ;//rmb 2       flawed block count                                      0x0238        -> 00 00
            public byte     m_sdenf                     ;//rmb 1       density flag - 0=single                                 0x023A        -> 01
            public byte     m_ssidf                     ;//rmb 1       side flag - 0=single                                    0x023B        -> 01
            public byte []  m_sswpbg = new byte[3]      ;//rmb 3       swap starting block number                              0x023C        -> 00 08 20
            public byte []  m_sswpsz = new byte[2]      ;//rmb 2       swap block count                                        0x023F        -> 01 80
            public byte     m_s64k                      ;//rmb 1       non-zero if swap block count is multiple of 64K         0x0241        -> 00
            public byte []  m_swinc  = new byte[11]     ;//rmb 11      Winchester configuration info                           0x0242        -> 00 00 00 00 00 00 2A 00 99 00 9A
            public byte []  m_sspare = new byte[11]     ;//rmb 11      spare bytes - future use                                0x024D        -> 00 9B 00 9C 00 9D 00 9E 00 9F 00
            public byte     m_snfdn                     ;//rmb 1       number of in core fdns                                  0x0258        -> A0           *snfdn * 2 = 320
            public byte []  m_scfdn  = new byte[512]    ;//rmb CFDN*2  in core free fdns                                       0x0259        variable (*snfdn * 2)
            public byte []  m_snfree                    ;//rmb 1       number of in core free blocks                           0x03B9        -> 03
            public byte []  m_sfree  = new byte[16384]  ;//rmb         CDBLKS*DSKADS in core free blocks                       0x03BA        -> 

            public fdn []   m_fdn    = new fdn[592];
        };

        UniFLEX_SIR [] m_drive_SIRs = new UniFLEX_SIR[4];

        enum DeviceInterruptMask
        {
            DEVICE_INT_MASK_DMA1  =   1,
            DEVICE_INT_MASK_DMA2  =   2,
            DEVICE_INT_MASK_DMA3  =   4,
            DEVICE_INT_MASK_DMA4  =   8,
            DEVICE_INT_MASK_FDC   =  16,
            DEVICE_INT_MASK_WDC   =  32,
            DEVICE_INT_MASK_CDS   =  64,
            DEVICE_INT_MASK_TAPE  = 128,
            DEVICE_INT_MASK_TIMER = 256
        }

        int m_nInterruptDelay    = 1;
        int m_nBoardInterruptRegister;
        int m_nInterruptingDevice;

        // permissions

        enum fdn_perms
        {
            UREAD   = 0x01,   // owner read
            UWRITE  = 0x02,   // owner write
            UEXEC   = 0x04,   // owner execute
            OREAD   = 0x08,   // other read
            OWRITE  = 0x10,   // other write
            OEXEC   = 0x20,   // other execute
            SUID    = 0x40    // set user ID for execute
        };

        //* stat codes

        enum fdn_stat_codes
        {
            FLOCK   = 0x01,         //  %00000001,  //fdn is locked
            FMOD    = 0x02,         //  %00000010,  //fdn has been modified
            FTEXT   = 0x04,         //  %00000100,  //this is a text segment
            FMNT    = 0x08,         //  %00001000,  //fdn is mounted on
            FWLCK   = 0x10          //  %00010000   //task awaiting lock
        };

        //* mode codes

        enum fdn_mode_codes
        {
            FBUSY   = 0x01,         //  %00000001 fdn is used (busy)
            FSBLK   = 0x02,         //  %00000010 block special file
            FSCHR   = 0x04,         //  %00000100 character special file
            FSDIR   = 0x08,         //  %00001000 directory type file
            FPRDF   = 0x10,         //  %00010000 pipe read flag
            FPWRF   = 0x20          //  %00100000 pipe write flag
        };

        //* access codes

        enum fdn_access_codes
        {
            FACUR   = 0x01,         //  %00000001 user read
            FACUW   = 0x02,         //  %00000010 user write
            FACUE   = 0x04,         //  %00000100 user execute
            FACOR   = 0x08,         //  %00001000 other read
            FACOW   = 0x10,         //  %00010000 other write
            FACOE   = 0x20,         //  %00100000 other execute
            FXSET   = 0x40          //  %01000000 uid execute set
        };

        #endregion

        public DMAF3()
        {
            m_nAbort                    = false;
            m_nCurrentSideSelected      = 0;
            m_nCurrentDensitySelected   = 0;

            m_nProcessorRunning     = false;
            m_nDMAF3Running         = false;
            m_nDMAF3Rate            = 1;            // count at 1 MHZ
            m_nEnableT1C_Countdown  = false;

            m_nFDCReading  = m_nFDCWriting = false;
            m_nFDCReadPtr  = 0;
            m_nFDCWritePtr = 0;

            DMAF3_WDC_DATARegister      = 0;                                            // WDC data register
            DMAF3_WDC_ERRORRegister     = 0;                                            // WDC error register
            DMAF3_WDC_SECCNTRegister    = 0;                                            // WDC sector count (during format)
            DMAF3_WDC_SECNUMRegister    = 0;                                            // WDC sector number
            DMAF3_WDC_CYLLORegister     = 0;                                            // WDC cylinder (low part)
            DMAF3_WDC_CYLHIRegister     = 0;                                            // WDC cylinder (high part)
            DMAF3_WDC_SDHRegister       = 0;                                            // WDC size/drive/head
            DMAF3_WDC_STATUSRegister    = (byte)(WDC_StatusCodes.DMAF3_WDC_READY | WDC_StatusCodes.DMAF3_WDC_SEEK_COMPLETE);  // WDC status register  - set ready and seek complete
            DMAF3_WDC_CMDRegister       = 0;                                            // WDC command register

            DMAF_WD1000_RESRegister     = 0;

            for (int i = 0; i < m_DMAF3_AT_TCRegister.Length; i++ )
            {
                m_DMAF3_AT_TCRegister[i] = new DMAF3_AT_TC_REGISTER();
            }
            for (int i = 0; i < m_DMAF3_DMA_ADDRESSRegister.Length; i++)
            {
                m_DMAF3_DMA_ADDRESSRegister[i] = new DMAF3_DMA_ADDRESS_REGISTER();
            }
            for (int i = 0; i < m_DMAF3_DMA_DMCNTRegister.Length; i++)
            {
                m_DMAF3_DMA_DMCNTRegister[i] = new DMAF3_DMA_BYTECNT_REGISTER();
            }

            DMAF3_AT_IFRRegister = 0;
            DMAF3_AT_DTBRegister = 0x20;    // make sure Archive exception bit is OFF
            DMAF3_AT_IERRegister = 0;

            for (int nDrive = 0; nDrive < 4; nDrive++)
            {
                m_cWinchesterDriveType[nDrive] = Program.GetConfigurationAttribute(Program._configSection + "/WinchesterDrives/Disk", "TypeName", nDrive.ToString(), "");
                if (m_cWinchesterDriveType[nDrive].Length > 0)
                {
                    m_cWinchesterDrivePathName[nDrive] = Program.dataDir + Program.GetConfigurationAttribute(Program._configSection + "/WinchesterDrives/Disk", "Path", nDrive.ToString(), "");
                    if (m_cWinchesterDriveType[nDrive].Length > 0)
                        SetHardDriveParameters("WinchesterDrives", m_cWinchesterDriveType[nDrive], nDrive);
                }

                m_cWinchesterDriveType[nDrive] = Program.GetConfigurationAttribute(Program._configSection + "/CDSDrives/Disk", "TypeName", nDrive.ToString(), "");
                if (m_cWinchesterDriveType[nDrive].Length > 0)
                {
                    m_cWinchesterDrivePathName[nDrive] = Program.dataDir + Program.GetConfigurationAttribute(Program._configSection + "/CDSDrives/Disk", "Path", nDrive.ToString(), "");
                    if (m_cWinchesterDriveType[nDrive].Length > 0)
                        SetHardDriveParameters("CDSDrives", m_cWinchesterDriveType[nDrive], nDrive);
                }
            }

            //string dmaf3ActivityLogDirectory = Program.GetConfigurationAttribute(Program._configSection, "DMAF3ActivityLogDirectory", "");
            //if (dmaf3ActivityLogDirectory != "")
            //    m_fpDMAActivity = new StreamWriter(File.Open(Path.Combine(dmaf3ActivityLogDirectory, "DMAF3Activity.txt"), FileMode.Append, FileAccess.Write, FileShare.ReadWrite));

            m_nBoardInterruptRegister = 0;
            m_nInterruptingDevice = 0;
            DMAF3_HLD_TOGGLERegister = 0;

            //#ifdef _DEBUG
            //#ifdef _LOG_DMA_ACTIVITY
            //    CString strDMAActivity;
            //    strDMAActivity.Format ("%s\\dma_activity.txt", Program.m_strTraceDirectory);
            //    fopen_s (&m_fpDMAActivity, strDMAActivity, "w");
            //#endif
            //#endif

            //  public Timer(TimerCallback callback, Object state, int dueTime, int period)
            /*
                Parameters
                callback
                Type: System.Threading.TimerCallback 
                A TimerCallback delegate representing a method to be executed. 

                state
                Type: System.Object 
                An object containing information to be used by the callback method, or null. 

                dueTime
                Type: System.Int32 
                The amount of time to delay before callback is invoked, in milliseconds. Specify Timeout.Infinite to prevent the timer from starting. Specify zero (0) to start the timer immediately. 

                period
                Type: System.Int32 
                The time interval between invocations of callback, in milliseconds. Specify Timeout.Infinite to disable periodic signaling. 

            */

            //TimerCallback tcbDMAF3InterruptDelayTimer = DMAF3_InterruptDelayTimerProc;
            //TimerCallback tcbDMAF3Timer               = DMAF3_TimerProc;
            //m_nDMAF3InterruptDelayTimer = new Timer(tcbDMAF3InterruptDelayTimer, null, Timeout.Infinite, Timeout.Infinite);
            //m_nDMAF3Timer               = new Timer(tcbDMAF3Timer, null, Timeout.Infinite, Timeout.Infinite);

            _DMAF3InterruptDelayTimer = new MicroTimer(); 
            _DMAF3InterruptDelayTimer.MicroTimerElapsed += new MicroTimer.MicroTimerElapsedEventHandler(DMAF3_InterruptDelayTimerProc);

            _DMAF3Timer               = new MicroTimer();
            _DMAF3Timer.MicroTimerElapsed += new MicroTimer.MicroTimerElapsedEventHandler(DMAF3_TimerProc);
        }

        private void memset(byte[] dest, byte c, int count)
        {
            for (int i = 0; i < dest.Length && i < count; i++)
            {
                dest[i] = c;
            }
        }

        private void memcpy(byte[] dst, byte[] src, int count)
        {
            for (int i = 0; i < dst.Length && i < src.Length && i < count; i++)
            {
                dst[i] = src[i];
            }
        }

        void SetHardDriveParameters(String strDriveType, String strDriveName, int nDrive)
        {
            m_hdpWinchester[nDrive] = new HARD_DRIVE_PARAMETERS();

            int nCylinders = Program.GetConfigurationAttribute(Program._configSection + "/" + strDriveType + "/Disk", "Cylinders", nDrive.ToString(), 0);
            if (nCylinders > 0)
            {
                m_hdpWinchester[nDrive].nCylinders = nCylinders;
                m_hdpWinchester[nDrive].nHeads           = Program.GetConfigurationAttribute(Program._configSection + "/" + strDriveType + "/Disk", "Heads",           nDrive.ToString(), 0);
                m_hdpWinchester[nDrive].nSectorsPerTrack = Program.GetConfigurationAttribute(Program._configSection + "/" + strDriveType + "/Disk", "SectorsPerTrack", nDrive.ToString(), 0);
                m_hdpWinchester[nDrive].nBytesPerSector  = Program.GetConfigurationAttribute(Program._configSection + "/" + strDriveType + "/Disk", "BytesPerSector",  nDrive.ToString(), 0);
            }
        }

        //~CDMAF3()
        //{
        //    int i;

        //    for (i = 0; i < NUMBER_OF_WINCHESTER_DRIVES; i++)
        //    {
        //        if (Program.m_fpWinchester[i] != null)
        //        {
        //            fclose (Program.m_fpWinchester[i]);
        //            Program.m_fpWinchester[i] = null;
        //        }
        //    }

        //    for (i = 0; i < NUMBER_OF_CDS_DRIVES; i++)
        //    {
        //        if (Program.m_fpCDS[i] != null)
        //        {
        //            fclose (Program.m_fpCDS[i]);
        //            Program.m_fpCDS[i] = null;
        //        }
        //    }

        //    for (i = 0; i < NUMBER_OF_FLOPPY_DRIVES; i++)
        //    {
        //        if (Program.DriveStream[i] != null)
        //        {
        //            fclose (Program.DriveStream[i]);
        //            Program.DriveStream[i] = null;
        //        }
        //    }

        //    //if (m_fpDMAActivity != null)
        //    //    fclose (m_fpDMAActivity);

        //    m_nProcessorRunning = false;
        //    StopDMAF3Timer ();
        //}

        //void CALLBACK DMAF3_INTERRUPT_DELAY_TIMER (uint uTimerID, uint uMsg, DWORD_PTR dwUser, DWORD_PTR dw1, DWORD_PTR dw2)
        //{
        //    CDMAF3* pTimer = (CDMAF3*) dwUser;
        //    pTimer->DMAF3_InterruptDelayTimerProc ();
        //}

        // Start the interrupt delay to set the interrupt

        void StartDMAF3InterruptDelayTimer (int rate, int nDeviceInterruptMask)
        {
            // wait for any pending interrupt to clear before setting a new one.

            //while (m_nDMAF3InterruptDelayTimer != null)
            //    Sleep (0);

            m_nInterruptingDevice     |= nDeviceInterruptMask;
            m_nBoardInterruptRegister |= nDeviceInterruptMask;

            // start a one-shot timer to introduce a delay before we set the interrupt on a WDC read or write command.

            SetInterrupt(_spin);
            return;

            //m_nDMAF3InterruptDelayTimer.Change(0, rate);    // this should start the timer
            _DMAF3InterruptDelayTimer.Interval = rate;
            _DMAF3InterruptDelayTimer.Start();
        }

        // when the timer fires, set the interrupt
        public void DMAF3_InterruptDelayTimerProc(object state, MicroTimerEventArgs timerEventArgs)
        {
            _DMAF3InterruptDelayTimer.Stop();
            SetInterrupt(_spin);
        }

        //public void DMAF3_InterruptDelayTimerProc(object state)
        //{
        //    SetInterrupt();
        //}
        // OK to set the interrupt now - special processing for DMAF3 since there are so many interrupt sources.

        public override void SetInterrupt(bool spin)
        {
            if ((m_nInterruptingDevice & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA1) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA1)
            {
                m_nInterruptingDevice &= ~(int)DeviceInterruptMask.DEVICE_INT_MASK_DMA1;

                m_DMAF3_DMA_CHANNELRegister[0] |= 0x80;     // say channel is interrupting
                if (((m_DMAF3_DMA_INTERRUPTRegister & 0x01) == 0x01) && ((m_DMAF3_DMA_CHANNELRegister[0] & 0x80) == 0x80))
                    m_DMAF3_DMA_INTERRUPTRegister |= 0x80;
            }
            if ((m_nInterruptingDevice & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA2) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA2)
            {
                m_nInterruptingDevice &= ~(int)DeviceInterruptMask.DEVICE_INT_MASK_DMA2;

                m_DMAF3_DMA_CHANNELRegister[1] |= 0x80;     // say channel is interrupting
                if (((m_DMAF3_DMA_INTERRUPTRegister & 0x02) == 0x02) && ((m_DMAF3_DMA_CHANNELRegister[1] & 0x80) == 0x80))
                    m_DMAF3_DMA_INTERRUPTRegister |= 0x80;

                DMAF3_WDC_STATUSRegister |= (byte)(WDC_StatusCodes.DMAF3_WDC_READY | WDC_StatusCodes.DMAF3_WDC_SEEK_COMPLETE);   // set ready and seek complete
            }
            if ((m_nInterruptingDevice & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA3) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA3)
            {
                m_nInterruptingDevice &= ~(int)DeviceInterruptMask.DEVICE_INT_MASK_DMA3;

                m_DMAF3_DMA_CHANNELRegister[2] |= 0x80;     // say channel is interrupting
                if (((m_DMAF3_DMA_INTERRUPTRegister & 0x04) == 0x04) && ((m_DMAF3_DMA_CHANNELRegister[2] & 0x80) == 0x80))
                    m_DMAF3_DMA_INTERRUPTRegister |= 0x80;
            }
            if ((m_nInterruptingDevice & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA4) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA4)
            {
                m_nInterruptingDevice &= ~(int)DeviceInterruptMask.DEVICE_INT_MASK_DMA4;

                m_DMAF3_DMA_CHANNELRegister[3] |= 0x80;     // say channel is interrupting
                if (((m_DMAF3_DMA_INTERRUPTRegister & 0x08) == 0x08) && ((m_DMAF3_DMA_CHANNELRegister[3] & 0x80) == 0x80))
                    m_DMAF3_DMA_INTERRUPTRegister |= 0x80;
            }
            if ((m_nInterruptingDevice & (int)DeviceInterruptMask.DEVICE_INT_MASK_FDC) == (int)DeviceInterruptMask.DEVICE_INT_MASK_FDC)
            {
                m_nInterruptingDevice &= ~(int)DeviceInterruptMask.DEVICE_INT_MASK_FDC;
                #region VIA description
                //
                //  $F040   VIA_BDATA   bit 7   
                //                          6   
                //                          5   Archive Exception if 0
                //                          4   
                //                          3   
                //                          2   WD1797 is interrupting
                //                          1   
                //                          0   
                //
                //  VIA Interrupt Flag Register
                //
                //  $F04D   VIA_IFR     bit 7   device is interrupting through VIA if 1 (must be 1 to interrupt)
                //                          6   
                //                          5   Archive Timer 2 is interrupting
                //                          4   
                //                          3   write a 1 to reset CB 2 interrupt
                //                          2   
                //                          1   
                //                          0   
                //
                //  VIA Interrupt Enable Register (used by software to mask interrupts from devices)
                //
                //  $F04E   VIA_IER     bit 7   
                //                          6   
                //                          5   Archive Timer 2 interrupt enable bit
                //                          4   
                //                          3   Board interrupt enable bit (CB2 from VIA)
                //                          2   
                //                          1   
                //                          0   
                //
                //          lda     VIA+IFR         read VIA flags                            A50B B6      F04D LDA A $F04D
                //          bpl     not_via                                                   A50E 2A        27 BPL   $27  
                //          ldb     VIA+BDATA       read the DMF3 status lines                A510 F6      F040 LDA B $F040
                //          anda    VIA+IER                                                   A513 B4      F04E AND A $F04E
                //          bita    #%00100000      Archive timer 2?                          A516 85        20 BIT A #$20 
                //          lbne    ATint                                                     A518 1026    04C4 LBNE  $04C4
                //          bita    #%00001000      check for board int                       A51C 85        08 BIT A #$08 
                //          beq     via_err                                                   A51E 27        14 BEQ   $14  
                //          lda     #%00001000      clear the CB2 interrupt                   A520 86        08 LDA A #$08 
                //          sta     VIA+IFR         by stuffing the flag register             A522 B7      F04D STA A $F04D
                //          bitb    #%00100000      check for archive exception               A525 C5        20 BIT B #$20 
                //          lbeq    ATint                                                     A527 1027    04B5 LBEQ  $04B5
                //          bitb    #%00000100      look only at 1797 irq line                A52B C5        04 BIT B #$04 
                //          lbne    dmf3int         yea! branch if good disk irq              A52D 1026    F592 LBNE  $F592
                //          jmp     w5int           must be WD1000 completion interrupt                  
                //via_err   sta     VIA+IFR         reset spurious int                        A534 B7      F04D STA A $F04D
                //not_via   
                //irqha1    ldx     #inttab         point to table                            A537 8E      94AC LDX   #$94AC
                #endregion
                DMAF3_AT_IFRRegister |= 0x88;       // signal board interrupt through VIA Status register
                DMAF3_AT_DTBRegister |= 0x04;       // say it's the WD179x that's doing the interrupting
                DMAF3_AT_DTBRegister |= 0x20;       // clear the Archive Exception bit
            }

            if ((m_nInterruptingDevice & (int)DeviceInterruptMask.DEVICE_INT_MASK_WDC) == (int)DeviceInterruptMask.DEVICE_INT_MASK_WDC)
            {
                m_nInterruptingDevice &= ~(int)DeviceInterruptMask.DEVICE_INT_MASK_WDC;

                int nDrive = 0;
                switch (m_DMAF3_DRVRegister & 0x0F)
                {
                    case 1: nDrive = 0; break;
                    case 2: nDrive = 1; break;
                    case 4: nDrive = 2; break;
                    case 8: nDrive = 3; break;
                }
            }

            base.SetInterrupt(spin);
        }

        void ClearInterrupt (int nDeviceInterruptMask)
        {
            m_nBoardInterruptRegister &= ~nDeviceInterruptMask;

            if (m_nBoardInterruptRegister == 0)
                base.ClearInterrupt();
        }

        public void DMAF3_TimerProc (object sender, MicroTimerEventArgs timerEventArgs)
        {
            byte c, d;
            uint e;

            // if the processor is not running - do nothing because we may be unstable

            if (m_nProcessorRunning)
            {
                if (m_nDMAF3Running)
                {
                    if (m_nEnableT1C_Countdown)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            c = m_DMAF3_AT_TCRegister[i].hibyte;
                            d = m_DMAF3_AT_TCRegister[i].lobyte;

                            e = (uint)((c * 256) + d);
                
                            --e;

                            c = (byte)(e / 256);
                            d = (byte)(e % 256);

                            if (e == 0)
                            {
                                m_DMAF3_AT_TCRegister[i].hibyte = m_DMAF3_AT_TLRegister.hibyte;
                                m_DMAF3_AT_TCRegister[i].lobyte = m_DMAF3_AT_TLRegister.lobyte;

                                DMAF3_AT_IFRRegister = (byte)(DMAF3_AT_IFRRegister | 0x40);
                                if ((DMAF3_AT_IERRegister & (byte)0x40) != 0)
                                {
                                    DMAF3_AT_IFRRegister = (byte)(DMAF3_AT_IFRRegister | 0x80);

                                    if (_bInterruptEnabled)
                                    //if (_bInterruptEnabled && (m_DMAF3_LATCHRegister & 0x10) == 0)
                                    {
                                        StartDMAF3InterruptDelayTimer(m_nInterruptDelay, (int)DeviceInterruptMask.DEVICE_INT_MASK_TIMER);
                                        if (Program._cpu != null)
                                        {
                                            if ((Program._cpu.InWait || Program._cpu.InSync) && Program._cpuThread.ThreadState == ThreadState.Suspended)
                                            {
                                                try
                                                {
                                                    Program._cpuThread.Resume();
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
                                m_DMAF3_AT_TCRegister[i].hibyte = c;
                                m_DMAF3_AT_TCRegister[i].lobyte = d;
                            }
                        }
                        //m_nDMAF3Timer.Change(0, m_nDMAF3Rate);  // restart the timer <- doesn't require restart - it just runs and runs and runs.......
                    }
                }

                //// if any of the timer interrupts are enabled, set interrupt bit in the processor.
                //
                //if ((m_DMAF3ControlRegister[0] & 0x40) || (m_DMAF3ControlRegister[1] & 0x40) || (m_DMAF3ControlRegister[2] & 0x40))
                //{
                //    if (_bInterruptEnabled)
                //    {
                //        StartDMAF3InterruptDelayTimer (m_nInterruptDelay);
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

        void StartDMAF3Timer (int rate)
        {
            // Only the processor can start the timer

            m_nProcessorRunning = true;

            // so start it already

            //m_nDMAF3Timer.Change(Timeout.Infinite, Timeout.Infinite);   //  timeKillEvent (m_nDMAF3Timer);
            //m_nDMAF3Timer.Change(0, rate);                              //  m_nDMAF3Timer = timeSetEvent(rate, 1, DMAF3_TIMER, (DWORD) this, TIME_PERIODIC);
            _DMAF3Timer.Interval = rate;
            _DMAF3Timer.Start();
        }

        void StopDMAF3Timer ()
        {
            if (_DMAF3Timer != null)
            {
                ClearInterrupt((int)DeviceInterruptMask.DEVICE_INT_MASK_TIMER);
                //m_nDMAF3Timer.Change(Timeout.Infinite, Timeout.Infinite);           //timeKillEvent (m_nDMAF3Timer);
                //m_nDMAF3Timer = null;
                m_fpDMAActivity.Close();
                _DMAF3Timer.Stop();
            }
        }

        ulong LogicalToPhysicalAddress (byte latch, ushort sLogicalAddress)
        {
            return ((ulong)(latch & (byte)DMAF3_ContollerLines.DMAF3_EXTADDR_SELECT_LINES) << 16) + (ulong)sLogicalAddress;
        }

        int CalcFileOffset (int nDrive)
        {
            int nStatus = 0;
            int nSectorSize = 512;

            if (m_nFDCSector <= Program.SectorsPerTrack[nDrive])
            {
                if (m_nFDCTrack < Program.NumberOfTracks[nDrive])
                {
                    switch (Program.DiskFormat[nDrive])
                    {
                        case DiskFormats.DISK_FORMAT_UNIFLEX:
                            nSectorSize = 512;
                            break;
                        case DiskFormats.DISK_FORMAT_MF_FDOS:
                            nSectorSize = 128;
                            break;
                        default:
                            nSectorSize = 256;
                            break;
                    }

                    if ((m_nFDCTrack == 0) && (m_nFDCSector == 0) && (m_nCurrentSideSelected == 0))
                        m_lFileOffset = 0L;
                    else
                    {
                        if ((Program.DiskFormat[nDrive] == DiskFormats.DISK_FORMAT_FLEX) || (Program.DiskFormat[nDrive] == DiskFormats.DISK_FORMAT_UNIFLEX) || (Program.DiskFormat[nDrive] == DiskFormats.DISK_FORMAT_MF_FDOS))
                        {
                            long lBlock = (long) ((m_nFDCTrack * Program.SectorsPerTrack[nDrive]) + (m_nFDCSector - 1));
                            m_lFileOffset = lBlock * (long) nSectorSize;
                        }
                        else if (Program.DiskFormat[nDrive] == DiskFormats.DISK_FORMAT_OS9)
                        {
                            int nSPT    = Program.SectorsPerTrack[nDrive];
                            int nFormat = Program.FormatByte[nDrive];

                            //  OS9 ALWAYS has m_nSectorsOnTrackZero sectors on track 0 side 0
                            //  and uses the side select before seeking to another cylinder.
                            //  track 0 side 1 has m_nSectorsPerTrack sectors.

                            if (m_nFDCTrack == 0)
                                m_lFileOffset = (long) (m_nFDCSector) * (long)nSectorSize;
                            else
                            {
                                // start off with track 0 side 1 already added in if it's double sided. 
                                // We'll get to track 0 side 0 sectors later

                                if ((nFormat & 0x0001) != 0)   // double sided
                                {
                                    m_lFileOffset = (long) nSPT * (long)nSectorSize; 

                                    // is this is not side 0, add in this tracks side 0 sectors

                                    if (m_nCurrentSideSelected != 0)
                                        m_lFileOffset += (long) nSPT * (long)nSectorSize; 

                                    // the rest of the math is done on cylinders

                                    nSPT *= 2;
                                }
                                else
                                    m_lFileOffset = 0L; 

                                // now add in all of the previous tracks sectors (except track 0)

                                m_lFileOffset += (long) (((m_nFDCTrack - 1) * nSPT) + m_nFDCSector) * (long)nSectorSize;
                            }

                            // if not track 0 side 0 - add in track 0 side 0 sectors 
                            // because it may have a different number of sectors

                            if (!((m_nFDCTrack == 0) && (m_nCurrentSideSelected == 0)))
                            {
                                m_lFileOffset += (long)Program.SectorsOnTrackZero[nDrive] * (long)nSectorSize;
                            }
                        }
                    }
                }
                else
                    nStatus = (int)DMAF3_StatusCodes.DMAF3_RNF; // RECORD NOT FOUND
            }
            else
                nStatus = (int)DMAF3_StatusCodes.DMAF3_RNF; // RECORD NOT FOUND

            return nStatus;
        }

        void CalcHDFileOffset (string strDriveType, WDC_COMMAND nFlag)
        {
            int nDriveType = -1;

            int nDrive      = 0;
            int nHead       = 0;
            int nCylinder   = 0;
            int nSector     = 0;

            int nCylinders      = 0;
            int nHeads          = 0;
            int nSectorsPerTrack= 0;
            int nBytesPerSector = 0;

            if (strDriveType == "CDS")
            {
                nDriveType = 1;

                nDrive    = 0;
                nHead     = 0;
                nCylinder = 0;
                nSector   = 0;

                nCylinders       = m_hdpCDS[nDrive].nCylinders       ;
                nHeads           = m_hdpCDS[nDrive].nHeads           ;
                nSectorsPerTrack = m_hdpCDS[nDrive].nSectorsPerTrack ;
                nBytesPerSector  = m_hdpCDS[nDrive].nBytesPerSector  ;
            }
            else if (strDriveType == "Winchester")
            {
                nDriveType = 0;

                nDrive    = (DMAF3_WDC_SDHRegister >> 3) & 0x03;
                nHead     = (DMAF3_WDC_SDHRegister) & 0x07;
                nCylinder = ((int) (DMAF3_WDC_CYLHIRegister & 0x03) * 256) + (int) DMAF3_WDC_CYLLORegister;
                nSector   = DMAF3_WDC_SECNUMRegister - 1;

                nCylinders       = m_hdpWinchester[nDrive].nCylinders       ;
                nHeads           = m_hdpWinchester[nDrive].nHeads           ;
                nSectorsPerTrack = m_hdpWinchester[nDrive].nSectorsPerTrack ;
                nBytesPerSector  = m_hdpWinchester[nDrive].nBytesPerSector  ;
            }

            if (nSector < nSectorsPerTrack)
            {
                if (nCylinder < nCylinders)
                {
                    if ((nCylinder == 0) && (nSector == 0) && (nHead == 0))
                        m_lFileOffset = 0L;
                    else
                    {
                        switch (nFlag)
                        {
                            case WDC_COMMAND.WDC_RDWR:
                                m_lFileOffset = (((long)nCylinder * (long)nHeads * (long)nSectorsPerTrack) + ((long)nHead * (long)nSectorsPerTrack) + (long)nSector) * (long)nBytesPerSector;
                                break;
                            case WDC_COMMAND.WDC_FORMAT:    // sector value is number of sectors not position
                                m_lFileOffset = (((long)nCylinder * (long)nHeads * (long)nSectorsPerTrack) + ((long)nHead * (long)nSectorsPerTrack)) * (long)nBytesPerSector;
                                break;
                            case WDC_COMMAND.WDC_SEEK:      // seeks only seek to cylinder (the head and sector or not used)
                                m_lFileOffset = (long)nCylinder * (long)nHeads * (long)nSectorsPerTrack;
                                break;
                        }
                    }

                    if (nDriveType == 0)
                        DMAF3_WDC_STATUSRegister &= (byte)(~(byte)WDC_StatusCodes.DMAF3_WDC_SEEK_COMPLETE & 0xFF);       // clear RECORD NOT FOUND
                }
                else
                {
                    if (nDriveType == 0)
                        DMAF3_WDC_STATUSRegister |= (byte)(WDC_StatusCodes.DMAF3_WDC_SEEK_COMPLETE);       // RECORD NOT FOUND
                }
            }
            else
            {
                if (nDriveType == 0)
                    DMAF3_WDC_STATUSRegister |= (byte)(WDC_StatusCodes.DMAF3_WDC_SEEK_COMPLETE);           // RECORD NOT FOUND
            }
        }

        void LogFloppyActivity (int nDrive, string strMessage, bool bIncludeIP)
        {
            //if (nDrive >= 0)
            //{
            //    if (m_fpDMAActivity != null)
            //    {
            //        if (bIncludeIP)
            //        {
            //            if (Program._cpu != null)
            //                fprintf (m_fpDMAActivity, "0x%04X %s", Program._cpu.m_CurrentIP, strMessage);
            //        }
            //        else
            //            fprintf (m_fpDMAActivity, strMessage);
            //    }
            //}
            //else
            //{
            //    if (m_fpDMAActivity != null)
            //    {
            //        if (bIncludeIP)
            //        {
            //            if (Program._cpu != null)
            //                fprintf (m_fpDMAActivity, "0x%04X %s", Program._cpu.m_CurrentIP, strMessage);
            //        }
            //        else
            //            fprintf (m_fpDMAActivity, strMessage);
            //    }
            //}
        }

        void LogActivity(int nDrive, string strMessage, bool bIncludeIP)
        {
            if (nDrive >= 0)
            {
                if (m_fpDMAActivity != null)
                {
                    if (bIncludeIP)
                    {
                        if (Program._cpu != null)
                        {
                            m_fpDMAActivity.Write("0x{0} {1}", Program._cpu.CurrentIP.ToString("X4"), strMessage);
                        }
                    }
                    else
                    {
                        m_fpDMAActivity.Write(strMessage);
                    }
                }
                m_fpDMAActivity.Flush();
            }
            //else
            //{
            //    if (m_fpDMAActivity != null)
            //    {
            //        if (bIncludeIP)
            //        {
            //            if (Program._cpu != null)
            //                fprintf (m_fpDMAActivity, "0x%04X %s", Program._cpu.m_CurrentIP, strMessage);
            //        }
            //        else
            //            fprintf (m_fpDMAActivity, strMessage);
            //    }
            //}
        }
        void WriteFloppy (ushort m, byte b)
        {
            int nWhichRegister = m - m_sBaseAddress;

            int nDrive =  0;
            int nSectorSize = 256;

            switch (m_DMAF3_DRVRegister & 0x0F)
            {
                case 1:
                    nDrive = 0;
                    break;
                case 2:
                    nDrive = 1;
                    break;
                case 4:
                    nDrive = 2;
                    break;
                case 8:
                    nDrive = 3;
                    break;
            }

            int nType = 1;
            bool nMultisector;

            switch (Program.DiskFormat[nDrive])
            {
                case DiskFormats.DISK_FORMAT_UNIFLEX:
                    nSectorSize = 512;
                    break;
                default:
                    nSectorSize = 256;
                    break;
            }

            int activechannel = -1;

            switch (m_DMAF3_DMA_PRIORITYRegister & 0x0F)
            {
                case 1:
                    activechannel = 0;      // Floppy
                    break;
                case 2:
                    activechannel = 1;      // WD1000
                    break;
                case 4:
                    activechannel = 2;
                    //AfxMessageBox ("DMA channel set to 2 - not supported");
                    break;
                case 8:
                    activechannel = 3;
                    //AfxMessageBox ("DMA channel set to 3 - not supported");
                    break;
            }

            byte   latch     = m_DMAF3_LATCHRegister;
            ushort  addr      = 0;
            ushort  cnt       = 0;
            byte   priority  = m_DMAF3_DMA_PRIORITYRegister;
            byte   interrupt = m_DMAF3_DMA_INTERRUPTRegister;
            byte   chain     = m_DMAF3_DMA_CHAINRegister;

            if (activechannel == 0)
            {
                addr      = (ushort)(m_DMAF3_DMA_ADDRESSRegister[activechannel].hibyte * 256 + m_DMAF3_DMA_ADDRESSRegister[activechannel].lobyte);
                cnt       = (ushort)(m_DMAF3_DMA_DMCNTRegister[activechannel].hibyte   * 256 + m_DMAF3_DMA_DMCNTRegister[activechannel].lobyte);
            }

            //CString strActivityMessage;

            //strActivityMessage.Format ("Writing Address 0x%04X with 0x%02X addr: 0x%04X latch: 0x%02X cnt: %d priority: 0x%02X interrupt: 0x%02X chain: 0x%02X\n", m, b, addr, latch, cnt, priority, interrupt, chain);
            //LogFloppyActivity (nDrive, strActivityMessage);

            if (Program.DMAF3AccessLogging)
            {
                string strActivityMessage = string.Format("Writing Address 0x{0} with 0x{1} addr: 0x{2} latch: 0x{3} cnt: {4} priority: 0x{5} interrupt: 0x{6} chain: 0x{7}\n", m.ToString("X4"), b.ToString("X2"), addr.ToString("X4"), latch.ToString("X2"), cnt.ToString(), priority.ToString("X2"), interrupt.ToString("X2"), chain.ToString("X2"));
                LogActivity(nDrive, strActivityMessage, true);
            }

            switch ((DMAF3_OffsetRegisters)nWhichRegister)
            {
                case DMAF3_OffsetRegisters.DMAF3_DATAREG_OFFSET:                              // 0xF023
                    m_DMAF3_DATARegister = b;
                    if (((m_DMAF3_STATRegister & (byte)DMAF3_StatusLines.DMAF3_BUSY) == (byte)DMAF3_StatusLines.DMAF3_BUSY) && m_nFDCWriting)
                    {
                        m_caWriteBuffer[m_nFDCWritePtr++] = b;
                        if (m_nFDCWritePtr == m_nBytesToTransfer)
                        {
                            // Here is where we will seek and write dsk file

                            int nStatus = CalcFileOffset (nDrive);
                            if (nStatus == 0)
                            {
                                if (Program.FloppyDriveStream[nDrive] != null)
                                {
                                    Program.FloppyDriveStream[nDrive].Seek(m_lFileOffset, SeekOrigin.Begin);
                                    Program.FloppyDriveStream[nDrive].Write(m_caWriteBuffer, 0, m_nBytesToTransfer);
                                    Program.FloppyDriveStream[nDrive].Flush();
                                }

                                //Program.pStaticFloppyActivityLight[nDrive]->SetBitmap (Program.m_hGreyDot);
                                m_DMAF3_STATRegister &= (byte)(~((byte)DMAF3_StatusLines.DMAF3_DRQ | (byte)DMAF3_StatusLines.DMAF3_BUSY) & 0xFF);
                        
                                // turn off high order bit in drive status register

                                m_DMAF3_DRVRegister &= (byte)(~(byte)DMAF3_ContollerLines.DRV_DRQ & (byte)0xFF);
                                m_nFDCWriting = false;
                            }
                            else
                                m_DMAF3_STATRegister |= (byte)DMAF3_StatusCodes.DMAF3_RNF;        // RECORD NOT FOUND
                        }
                        else
                        {
                            m_DMAF3_STATRegister |= (byte)DMAF3_StatusLines.DMAF3_DRQ;
                            m_DMAF3_DRVRegister |= (byte)DMAF3_ContollerLines.DRV_DRQ;

                            if (_bInterruptEnabled)
                            {
                                StartDMAF3InterruptDelayTimer(m_nInterruptDelay, (int)DeviceInterruptMask.DEVICE_INT_MASK_FDC);
                                if (Program._cpu != null)
                                {
                                    if ((Program._cpu.InWait || Program._cpu.InSync) && Program._cpuThread.ThreadState == ThreadState.Suspended)
                                    {
                                        try
                                        {
                                            Program._cpuThread.Resume();
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

                case DMAF3_OffsetRegisters.DMAF3_DRVREG_OFFSET:                           // 0xF024
                    m_DMAF3_DRVRegister = b;
                    if ((m_DMAF3_DRVRegister & 0x10) == 0x10)        // we are selecting side 1
                        m_nCurrentSideSelected = 1;
                    else                                             // we are selecting side 0
                        m_nCurrentSideSelected = 0;

                    if ((m_DMAF3_DRVRegister & 0x20) == 0x20)        // we are selecting single density
                        m_nCurrentDensitySelected = 0;
                    else                                             // we are selecting double density
                        m_nCurrentDensitySelected = 0;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_CMDREG_OFFSET:                           // 0xF020
                    m_nFDCReading = m_nFDCWriting = false;          // can't be read/writing if commanding

                    switch (b & 0xF0)
                    {
                        // TYPE I

                        case 0x00:  //  0x0X = RESTORE
                            m_nFDCTrack = 0;
		                    m_DMAF3_TRKRegister = 0;
                            break;

                        case 0x10:  //  0x1X = SEEK
                            m_nFDCTrack = m_DMAF3_DATARegister;
		                    m_DMAF3_TRKRegister = (byte)m_nFDCTrack;
                            break;

                        case 0x20:  //  0x2X = STEP
                            break;

                        case 0x30:  //  0x3X = STEP     W/TRACK UPDATE
                            break;

                        case 0x40:  //  0x4X = STEP IN
                            if (m_nFDCTrack < 79)
                                m_nFDCTrack++;
                            break;

                        case 0x50:  //  0x5X = STEP IN  W/TRACK UPDATE
                            if (m_nFDCTrack < 79)
                            {
                                m_nFDCTrack++;
                                m_DMAF3_TRKRegister = (byte)m_nFDCTrack;
                            }
                            break;

                        case 0x60:  //  0x6X = STEP OUT
                            if (m_nFDCTrack > 0)
                                --m_nFDCTrack;
                            break;

                        case 0x70:  //  0x7X = STEP OUT W/TRACK UPDATE
                            if (m_nFDCTrack > 0)
                            {
                                --m_nFDCTrack;
                                m_DMAF3_TRKRegister = (byte)m_nFDCTrack;
                            }
                            break;

                            // TYPE II

                        case 0x80:  //  0x8X = READ  SINGLE   SECTOR
                        case 0x90:  //  0x9X = READ  MULTIPLE SECTORS
                        case 0xA0:  //  0xAX = WRITE SINGLE   SECTOR
                        case 0xB0:  //  0xBX = WRITE MULTIPLE SECTORS

                            m_DMAF3_STATRegister |= (byte)DMAF3_StatusLines.DMAF3_HDLOADED;

                            nType = 2;
				            m_nFDCTrack = m_DMAF3_TRKRegister;

                            nMultisector = false;
                            m_nBytesToTransfer = nSectorSize;

                            if ((b & 0x10) == 0x10)
                            {
                                if (m_nAllowMultiSectorTransfers)
                                {
                                    nMultisector = true;
                                    m_nBytesToTransfer = (Program.SectorsPerTrack[nDrive] - m_nFDCSector + 1) * nSectorSize;
                                }
                            }

                            if ((b & 0x20) == 0x20)   // WRITE
                            {
                                //Program.pStaticFloppyActivityLight[nDrive]->SetBitmap (Program.m_hRedDot);

                                m_nFDCReading = false;
                                m_nFDCWriting = true;
                                m_nFDCWritePtr = 0;
                                m_nStatusReads = 0;

                                int nStatus = CalcFileOffset (nDrive);
                                if (nStatus == 0)
                                {
                                    if (Program.FloppyDriveStream[nDrive] != null)
                                    {
                                        m_DMAF3_STATRegister &= (byte)(~((byte)DMAF3_StatusLines.DMAF3_DRQ | (byte)DMAF3_StatusLines.DMAF3_BUSY) & 0xFF);
                                        m_DMAF3_DRVRegister &= (byte)(~((byte)DMAF3_ContollerLines.DRV_DRQ) & 0xFF);        // turn off high order bit in drive status register
                                        m_nFDCWriting = false;

                                        // if the dma is active - write bytes to the disk from the buffer and reset the dma counter

                                        ulong lPhysicalAddress = LogicalToPhysicalAddress (latch, addr);

                                        if (activechannel == 0) // only handle channel 1 for floppy
                                        {
                                            if (cnt > 0)
                                            {
                                                //strActivityMessage.Format ("Writing %4d bytes to floppy %d track %02d sector %02d from offset %06X DSK offset 0x%08X\n", cnt, nDrive, m_nFDCTrack, m_nFDCSector, lPhysicalAddress, m_lFileOffset);
                                                //LogFloppyActivity (nDrive, strActivityMessage);

                                                if (Program.DMAF3AccessLogging)
                                                {
                                                    string strActivityMessage = string.Format("Writing {0} bytes to floppy {1} track 0x{2} sector 0x{3} from offset 0x{4} DSK offset 0x{5}\n", cnt.ToString(), nDrive.ToString(), m_nFDCTrack.ToString("X2"), m_nFDCSector.ToString("X2"), lPhysicalAddress.ToString("X6"), m_lFileOffset.ToString("X8"));
                                                    LogActivity(nDrive, strActivityMessage, true);
                                                }

                                                Program.FloppyDriveStream[nDrive].Seek(m_lFileOffset, SeekOrigin.Begin);

                                                if ((m_DMAF3_DMA_CHANNELRegister[activechannel] & 0x08) == 0x08)
                                                    Program.FloppyDriveStream[nDrive].Write(Program._cpu.Memory.MemorySpace, (int)(lPhysicalAddress - cnt), cnt);
                                                else
                                                    Program.FloppyDriveStream[nDrive].Write(Program._cpu.Memory.MemorySpace, (int)lPhysicalAddress, cnt);

                                                Program.FloppyDriveStream[nDrive].Flush();

                                                ushort a = (ushort)(m_DMAF3_DMA_ADDRESSRegister[activechannel].hibyte * 256 + m_DMAF3_DMA_ADDRESSRegister[activechannel].lobyte);
                                                if ((m_DMAF3_DMA_CHANNELRegister[activechannel] & 0x08) == 0x08)
                                                    a -= cnt;
                                                else
                                                    a += cnt;
                                                m_DMAF3_DMA_ADDRESSRegister[activechannel].hibyte = (byte)(a / 256);
                                                m_DMAF3_DMA_ADDRESSRegister[activechannel].lobyte = (byte)(a % 256);

                                                m_DMAF3_STATRegister &= (byte)(~((byte)DMAF3_StatusLines.DMAF3_DRQ | (byte)DMAF3_StatusLines.DMAF3_BUSY) & 0xFF);
                                                m_DMAF3_DRVRegister &= (byte)(~((byte)DMAF3_ContollerLines.DRV_DRQ) & 0xFF);            // turn off high order bit in drive status register

                                                m_nFDCReading = false;
                                                m_nFDCWriting = false;
                                            }
                                            else
                                            {
                                                // must be doing polled IO

                                                //AfxMessageBox ("Polled IO read not implemented");
                                            }
                                        }
                                        //else
                                        //{
                                        //    if (activechannel != -1)
                                        //        //AfxMessageBox ("DMA attempting to use channel other than 0 for floppy during write");
                                        //}
                                    }
                                }
                                else
                                    m_DMAF3_STATRegister |= (byte)DMAF3_StatusCodes.DMAF3_RNF;        //  RECORD NOT FOUND
                            }
                            else                    // READ
                            {
                                //Program.pStaticFloppyActivityLight[nDrive]->SetBitmap (Program.m_hGreenDot);

                                m_nFDCReading = true;
                                m_nFDCWriting = false;
                                m_nFDCReadPtr = 0;
                                m_nStatusReads = 0;

                                int nStatus = CalcFileOffset (nDrive);
                                if (nStatus == 0)
                                {
                                    if (Program.FloppyDriveStream[nDrive] != null)
                                    {
                                        Program.FloppyDriveStream[nDrive].Seek(m_lFileOffset, SeekOrigin.Begin);
                                        Program.FloppyDriveStream[nDrive].Read(m_caReadBuffer, 0, m_nBytesToTransfer);
                                        m_DMAF3_DATARegister = m_caReadBuffer[0];

                                        // if the dma is active - get the bytes from memory, write them to the buffer and reset the dma counter

                                        if (activechannel == 0) // only handle channel 1 for now
                                        {
                                            if (cnt > 0)
                                            {
                                                ulong lPhysicalAddress = LogicalToPhysicalAddress (latch, addr);

                                                //strActivityMessage.Format ("Reading %4d bytes from floppy %d track %02d sector %02d to   offset %06X DSK offset 0x%08X\n", cnt, nDrive, m_nFDCTrack, m_nFDCSector, lPhysicalAddress, m_lFileOffset);
                                                //LogFloppyActivity (nDrive, strActivityMessage);

                                                if (Program.DMAF3AccessLogging)
                                                {
                                                    string strActivityMessage = string.Format("Reading {0} bytes from floppy {1} track 0x{2} sector 0x{3} from offset 0x{4} DSK offset 0x{5}\n", cnt.ToString(), nDrive.ToString(), m_nFDCTrack.ToString("X2"), m_nFDCSector.ToString("X2"), lPhysicalAddress.ToString("X6"), m_lFileOffset.ToString("X8"));
                                                    LogActivity(nDrive, strActivityMessage, true);
                                                }

                                                for (int i = 0; i < cnt; i++)
                                                {
                                                    if ((m_DMAF3_DMA_CHANNELRegister[activechannel] & 0x08) == 0x08)
                                                        Program._cpu.Memory.MemorySpace[(int)lPhysicalAddress - i] = m_caReadBuffer[i];
                                                    else
                                                        Program._cpu.Memory.MemorySpace[(int)lPhysicalAddress + i] = m_caReadBuffer[i];
                                                }

                                                ushort a = (ushort)(m_DMAF3_DMA_ADDRESSRegister[activechannel].hibyte * 256 + m_DMAF3_DMA_ADDRESSRegister[activechannel].lobyte);
                                                if ((m_DMAF3_DMA_CHANNELRegister[activechannel] & 0x08) == 0x08)
                                                    a -= cnt;
                                                else
                                                    a += cnt;

                                                m_DMAF3_DMA_ADDRESSRegister[activechannel].hibyte = (byte)(a / 256);
                                                m_DMAF3_DMA_ADDRESSRegister[activechannel].lobyte = (byte)(a % 256);


                                                m_DMAF3_STATRegister &= (byte)(~((byte)DMAF3_StatusLines.DMAF3_DRQ | (byte)DMAF3_StatusLines.DMAF3_BUSY) & 0xFF);
                                                m_DMAF3_DRVRegister  &= (byte)(~(byte)DMAF3_ContollerLines.DRV_DRQ & 0xFF);            // turn off high order bit in drive status register

                                                m_nFDCReading = false;
                                                m_nFDCWriting = false;
                                            }
                                            else
                                            {
                                                //AfxMessageBox ("Polled IO for write not implemented");
                                            }
                                        }
                                        //else
                                        //{
                                        //    if (activechannel != -1)
                                        //        //AfxMessageBox ("DMA attempting to use channel other than 0 for floppy during read");
                                        //}
                                    }
                                }
                                else
                                    m_DMAF3_STATRegister |= (byte)DMAF3_StatusCodes.DMAF3_RNF;        //  RECORD NOT FOUND
                            }
                            break;

                            // TYPE III

                        case 0xC0:  //  0xCX = READ ADDRESS
                            nType = 3;
                            //AfxMessageBox ("Unimplemented Floppy Command 0xCX");
                            break;

                        case 0xE0:  //  0xEX = READ TRACK
                            nType = 3;
                            //AfxMessageBox ("Unimplemented Floppy Command 0xEX");
                            break;

                        case 0xF0:  //  0xFX = WRITE TRACK (FORMAT)
                            if (Program.DiskFormat[nDrive] == DiskFormats.DISK_FORMAT_UNIFLEX)
                            {
                                int nStatus = 0;

                                switch (Program.DiskFormat[nDrive])
                                {
                                    case DiskFormats.DISK_FORMAT_UNIFLEX:
                                        nSectorSize = 512;
                                        break;
                                    case DiskFormats.DISK_FORMAT_MF_FDOS:
                                        nSectorSize = 128;
                                        break;
                                    default:
                                        nSectorSize = 256;
                                        break;
                                }

                                if ((m_nFDCTrack == 0) && ((m_nFDCSector - 1) == 0) && (m_nCurrentSideSelected == 0))
                                    m_lFileOffset = 0L;
                                else
                                {
                                    if ((Program.DiskFormat[nDrive] == DiskFormats.DISK_FORMAT_FLEX) || (Program.DiskFormat[nDrive] == DiskFormats.DISK_FORMAT_UNIFLEX) || (Program.DiskFormat[nDrive] == DiskFormats.DISK_FORMAT_MF_FDOS))
                                    {
                                        if (m_nCurrentSideSelected == 0)
                                        {
                                            if (m_nFDCTrack != 0 && Program.IsDoubleSided[nDrive] == -1)
                                            {
                                                Program.IsDoubleSided[nDrive] = 0;
                                                Program.NumberOfTracks[nDrive] = m_nFDCTrack + 1;
                                                Program.SectorsPerTrack[nDrive] = (byte)m_nFormatSectorsPerSide;
                                            }

                                            if (Program.IsDoubleSided[nDrive] == 1)
                                                m_lFileOffset = (long) ((m_nFDCTrack * (m_nFormatSectorsPerSide * 2)) + (m_nFDCSector - 1)) * (long) nSectorSize;
                                            else
                                                m_lFileOffset = (long) ((m_nFDCTrack * m_nFormatSectorsPerSide) + (m_nFDCSector - 1)) * (long) nSectorSize;
                                        }
                                        else
                                        {
                                            Program.IsDoubleSided[nDrive] = 1;

                                            Program.NumberOfTracks[nDrive] = m_nFDCTrack + 1;
                                            Program.SectorsPerTrack[nDrive] = (byte)(m_nFormatSectorsPerSide * 2);

                                            m_lFileOffset = (long) ((m_nFDCTrack * (m_nFormatSectorsPerSide * 2)) + m_nFormatSectorsPerSide + (m_nFDCSector - 1)) * (long) nSectorSize;
                                        }
                                    }
                                    else if (Program.DiskFormat[nDrive] == DiskFormats.DISK_FORMAT_OS9)
                                    {
                                        int nSPT    = m_nFormatSectorsPerSide;
                                        int nFormat = Program.FormatByte[nDrive];

                                        //  OS9 ALWAYS has m_nSectorsOnTrackZero sectors on track 0 side 0
                                        //  and uses the side select before seeking to another cylinder.
                                        //  track 0 side 1 has m_nFormatSectorsPerSide sectors.

                                        if (m_nFDCTrack == 0)
                                            m_lFileOffset = (long) (m_nFDCSector) * (long)nSectorSize;
                                        else
                                        {
                                            // start off with track 0 side 1 already added in if it's double sided. 
                                            // We'll get to track 0 side 0 sectors later

                                            if ((nFormat & 0x0001) != 0)   // double sided
                                            {
                                                m_lFileOffset = (long) nSPT * (long)nSectorSize; 

                                                // is this is not side 0, add in this tracks side 0 sectors

                                                if (m_nCurrentSideSelected != 0)
                                                    m_lFileOffset += (long) nSPT * (long)nSectorSize; 

                                                // the rest of the math is done on cylinders

                                                nSPT *= 2;
                                            }
                                            else
                                                m_lFileOffset = 0L; 

                                            // now add in all of the previous tracks sectors (except track 0)

                                            m_lFileOffset += (long) (((m_nFDCTrack - 1) * nSPT) + m_nFDCSector) * (long)nSectorSize;
                                        }

                                        // if not track 0 side 0 - add in track 0 side 0 sectors 
                                        // because it may have a different number of sectors

                                        if (!((m_nFDCTrack == 0) && (m_nCurrentSideSelected == 0)))
                                        {
                                            m_lFileOffset += (long)Program.SectorsOnTrackZero[nDrive] * (long)nSectorSize;
                                        }
                                    }
                                }

                                if (nStatus == 0)
                                {
                                    if (Program.FloppyDriveStream[nDrive] != null)
                                    {
                                        m_DMAF3_STATRegister &= (byte)(~((byte)DMAF3_StatusLines.DMAF3_DRQ | (byte)DMAF3_StatusLines.DMAF3_BUSY) & 0xFF);
                                        m_DMAF3_DRVRegister &= (byte)(~((byte)DMAF3_ContollerLines.DRV_DRQ) & 0xFF);        // turn off high order bit in drive status register
                                        m_nFDCWriting = false;

                                        // if the dma is active - write bytes to the disk from the buffer and reset the dma counter

                                        ulong lPhysicalAddress = LogicalToPhysicalAddress (latch, addr);

                                        if (activechannel == 0) // only handle channel 1 for floppy
                                        {
                                            if (cnt > 0)
                                            {
                                                // the format of the format buffer is as follows:
                                                //
                                                //      It starts with a bunch of 0xFF for single density or 0x4E for double density 
                                                //      Then 6 0x00 for single density or 12 0x00 and 3 0xF6 bytes for double density.
                                                //      Then 1 0xFC
                                                //      Then more 0xFF for single density or 0x4E for double density 
                                                //
                                                //      ----------- This is the part that gets repeated for each sector
                                                //
                                                //      Then 6 more 0x00 for single density or 12 0x00 bytes and 3 0xF5 bytes for double density.
                                                //      Then 1 0xFE
                                                //
                                                //      following this initial set of sync bytes is the sector side and size info in the format of:
                                                //
                                                //          0x00 0x00 0x01 0x02
                                                //
                                                //      where the first  byte is the track
                                                //            the second byte is irrelevant (maybe density or side info)
                                                //            the third  byte is the sector number
                                                //            the fourth byte is the size of the data to follow (0x02 = 512 bytes)
                                                //
                                                //      Then 1 0xF7
                                                //      Then more 0xFF for single density or 0x4E for double density 
                                                //      Then 6 more 0x00 for single density or 12 0x00 bytes and 3 0xF5 bytes for double density.
                                                //      Then 1 0xFB
                                                //
                                                //      Then the data
                                                //
                                                //      Then 1 0xF7
                                                //
                                                //      ----------- Then next sector --- go to repeat

                                                //strActivityMessage.Format ("Writing %4d bytes to floppy %d track %02d sector %02d from offset %06X DSK offset 0x%08X\n", cnt, nDrive, m_nFDCTrack, m_nFDCSector, lPhysicalAddress, m_lFileOffset);
                                                //LogFloppyActivity (nDrive, strActivityMessage);

                                                if (Program.DMAF3AccessLogging)
                                                {
                                                    string strActivityMessage = string.Format("Writing {0} bytes to floppy {1} track 0x{2} sector 0x{3} from offset 0x{4} DSK offset 0x{5}\n", cnt.ToString(), nDrive.ToString(), m_nFDCTrack.ToString("X2"), m_nFDCSector.ToString("X2"), lPhysicalAddress.ToString("X6"), m_lFileOffset.ToString("X8"));
                                                    LogActivity(nDrive, strActivityMessage, true);
                                                }

                                                int nFormatBufferIndex = 0;
                                                bool bDoubleDensity = false;
                                                byte cSyncByte = Program._cpu.Memory.MemorySpace[lPhysicalAddress];

                                                if (cSyncByte == 0x4E)
                                                    bDoubleDensity = true;

                                                // Get past the index mark

                                                while (Program._cpu.Memory.MemorySpace[(int)((int)lPhysicalAddress + nFormatBufferIndex)] != 0xFC && nFormatBufferIndex < cnt)
                                                    nFormatBufferIndex++;

                                                // resset the number of sectors per side every time we format side 0 and then recalculate as we write side 0

                                                if (m_nCurrentSideSelected == 0)
                                                    m_nFormatSectorsPerSide = 0;

                                                while (nFormatBufferIndex < cnt)
                                                {
                                                    nFormatBufferIndex++;   // skip to next byte

                                                    while (Program._cpu.Memory.MemorySpace[(int)((int)lPhysicalAddress + nFormatBufferIndex)] != 0xFE && nFormatBufferIndex < cnt)
                                                        nFormatBufferIndex++;

                                                    nFormatBufferIndex++;       // skip the 0xFE

                                                    // we are now pointing at the real format info

                                                    byte ucIrrelavent1   = Program._cpu.Memory.MemorySpace[(int)((int)lPhysicalAddress + nFormatBufferIndex++)];
                                                    byte ucIrrelavent2   = Program._cpu.Memory.MemorySpace[(int)((int)lPhysicalAddress + nFormatBufferIndex++)];
                                                    byte ucSectorNumber  = Program._cpu.Memory.MemorySpace[(int)((int)lPhysicalAddress + nFormatBufferIndex++)];
                                                    byte ucSizeIndicator = Program._cpu.Memory.MemorySpace[(int)((int)lPhysicalAddress + nFormatBufferIndex++)];

                                                    if (Program._cpu.Memory.MemorySpace[(int)((int)lPhysicalAddress + nFormatBufferIndex++)] == 0xF7)    // suck up the 0xF7
                                                    {
                                                        // skip more cSyncBytes

                                                        while (Program._cpu.Memory.MemorySpace[(int)((int)lPhysicalAddress + nFormatBufferIndex)] != 0xFB && nFormatBufferIndex < cnt)
                                                            nFormatBufferIndex++;

                                                        // now we are pointing at the databytes

                                                        int nDataCount = 512;
                                                        if (ucSizeIndicator == 0x02)
                                                            nDataCount = 512;

                                                        nFormatBufferIndex++;
                                                        if (Program._cpu.Memory.MemorySpace[(int)lPhysicalAddress + nFormatBufferIndex + nDataCount] == 0xF7)
                                                        {
                                                            long fileLength = Program.FloppyDriveStream[nDrive].Length;

                                                            //Program.pStaticFloppyActivityLight[nDrive]->SetBitmap (Program.m_hRedDot);

                                                            if (m_nCurrentSideSelected == 0)
                                                                m_nFormatSectorsPerSide++;

                                                            if (fileLength < (m_lFileOffset + nDataCount))
                                                            {
                                                                // close file for "r+b" and reopen for append - write the bytes - close and reopen for "r+b"

                                                                Program.FloppyDriveStream[nDrive].Close();
                                                                Program.FloppyDriveStream[nDrive] = File.Open(Program.DriveImagePaths[nDrive], FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                                                                Program.FloppyDriveStream[nDrive].Write(Program._cpu.Memory.MemorySpace, (int)((int)lPhysicalAddress + nFormatBufferIndex), nDataCount);

                                                                Program.FloppyDriveStream[nDrive].Close();
                                                                Program.FloppyDriveStream[nDrive] = File.Open(Program.DriveImagePaths[nDrive], FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                                                            }
                                                            else
                                                            {
                                                                Program.FloppyDriveStream[nDrive].Seek(m_lFileOffset, SeekOrigin.Begin);
                                                                Program.FloppyDriveStream[nDrive].Write(Program._cpu.Memory.MemorySpace, (int)((int)lPhysicalAddress + nFormatBufferIndex), nDataCount);
                                                                Program.FloppyDriveStream[nDrive].Flush();
                                                            }

                                                            if (m_nCurrentSideSelected != 0)
                                                                m_lFileOffset += nDataCount; // + (m_nFormatSectorsPerSide * nDataCount));
                                                            else
                                                                m_lFileOffset += nDataCount;

                                                            nFormatBufferIndex += nDataCount;
                                                        }
                                                        else
                                                        {
                                                            //AfxMessageBox ("invalid format buffer format");
                                                            break;
                                                        }
                                                    }
                                                }

                                                ushort a = (ushort)(m_DMAF3_DMA_ADDRESSRegister[activechannel].hibyte * 256 + m_DMAF3_DMA_ADDRESSRegister[activechannel].lobyte);
                                                if ((m_DMAF3_DMA_CHANNELRegister[activechannel] & 0x08) == 0x08)
                                                    a -= cnt;
                                                else
                                                    a += cnt;
                                                m_DMAF3_DMA_ADDRESSRegister[activechannel].hibyte = (byte)(a / 256);
                                                m_DMAF3_DMA_ADDRESSRegister[activechannel].lobyte = (byte)(a % 256);

                                                m_DMAF3_STATRegister &= (byte)(~((byte)DMAF3_StatusLines.DMAF3_DRQ | (byte)DMAF3_StatusLines.DMAF3_BUSY) & 0xFF);
                                                m_DMAF3_DRVRegister &= (byte)(~((byte)DMAF3_ContollerLines.DRV_DRQ) & 0xFF);            // turn off high order bit in drive status register

                                                m_nFDCReading = false;
                                                m_nFDCWriting = false;
                                            }
                                            else
                                            {
                                                // must be doing polled IO

                                                //AfxMessageBox ("Polled IO read not implemented");
                                            }
                                        }
                                        //else
                                        //{
                                        //    if (activechannel != -1)
                                        //        //AfxMessageBox ("DMA attempting to use channel other than 0 for floppy during write");
                                        //}
                                    }
                                }
                                else
                                    m_DMAF3_STATRegister |= (byte)DMAF3_StatusCodes.DMAF3_RNF;        //  RECORD NOT FOUND
                            }
                            else
                            {
                                if ((m_nFDCTrack == 0) && ((m_nFDCSector - 1) == 0) && (m_nCurrentSideSelected == 0))
                                    //AfxMessageBox ("Format command is only supported for UniFLEX");

                                m_DMAF3_STATRegister |= (byte)DMAF3_StatusLines.DMAF3_WRTPROTECT;     // report Write Protected Status
                            }
                            nType = 3;
                            break;

                            // TYPE IV

                        case 0xD0:  //  0xDX = FORCE INTERRUPT
                    
                            // Ix = Interrupt Condition Flags
                            // I0 = 1 Not Ready to Ready Transition
                            // I1 = 1 Ready To Not Ready Transition
                            // I2 = 1 Index Pulse
                            // I3 = 1 Immediate Interrupt, Requires A Reset*
                            // I3-I0 = 0 Terminate With No Interrupt (INTRQ)

                            m_nInterruptConditionFlag = b & 0x0F;
                            nType = 4;
                            break;

                        default:
                            //AfxMessageBox ("Unimplemented Floppy Command");
                            break;
                    }

                    m_DMAF3_STATRegister |= (byte)DMAF3_StatusLines.DMAF3_BUSY;

                    // see if current drive is READY

                    if (Program.DriveOpen[nDrive] == true)
                        m_DMAF3_STATRegister |= (byte)DMAF3_StatusLines.DMAF3_NOTREADY;
                    else
                        m_DMAF3_STATRegister &= (byte)(~((byte)DMAF3_StatusLines.DMAF3_NOTREADY) & 0xFF);

                    switch (nType)
                    {
                        case 1:
                            m_DMAF3_STATRegister |= (byte)DMAF3_StatusLines.DMAF3_HDLOADED;
                            m_DMAF3_STATRegister &= (byte)(~((byte)DMAF3_StatusLines.DMAF3_BUSY | (byte)DMAF3_StatusLines.DMAF3_DRQ) & 0xFF);

                            if (m_nFDCTrack == 0)
                                m_DMAF3_STATRegister |= (byte)DMAF3_StatusLines.DMAF3_TRKZ;
                            else
                                m_DMAF3_STATRegister &= (byte)(~((byte)DMAF3_StatusLines.DMAF3_TRKZ) & 0xFF);

                            m_DMAF3_DRVRegister &= (byte)(~((byte)DMAF3_ContollerLines.DRV_DRQ) & 0xFF);

                            if (_bInterruptEnabled)
                            {
                                StartDMAF3InterruptDelayTimer(m_nInterruptDelay, (int)DeviceInterruptMask.DEVICE_INT_MASK_FDC);
                                if (Program._cpu != null)
                                {
                                    if ((Program._cpu.InWait || Program._cpu.InSync) && Program._cpuThread.ThreadState == ThreadState.Suspended)
                                    {
                                        try
                                        {
                                            Program._cpuThread.Resume();
                                        }
                                        catch (ThreadStateException e)
                                        {
                                            // do nothing if thread is not suspended
                                        }
                                    }
                                }
                            }
                            break;

                        case 2:
                        case 3:
                            if (Program.FloppyDriveStream[nDrive] != null)
                            {
                                if (cnt == 0)  
                                {
                                    // we get here during polled IO

                                    m_DMAF3_STATRegister |= ((byte)DMAF3_StatusLines.DMAF3_DRQ | (byte)DMAF3_StatusLines.DMAF3_BUSY);
                                    m_DMAF3_DRVRegister |= (byte)DMAF3_ContollerLines.DRV_DRQ;

                                    if (_bInterruptEnabled)
                                    {
                                        StartDMAF3InterruptDelayTimer(m_nInterruptDelay, (int)DeviceInterruptMask.DEVICE_INT_MASK_FDC);
                                        if (Program._cpu != null)
                                        {
                                            if ((Program._cpu.InWait || Program._cpu.InSync) && Program._cpuThread.ThreadState == ThreadState.Suspended)
                                            {
                                                try
                                                {
                                                    Program._cpuThread.Resume();
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
                                    // if the DMA did the transfer - clear the DRQ bits

                                    if ((m_DMAF3_STATRegister & (byte)DMAF3_StatusLines.DMAF3_WRTPROTECT) == 0)
                                        m_DMAF3_STATRegister = (byte)DMAF3_StatusCodes.DMAF3_MOTOR_ON;
                                    else
                                        m_DMAF3_STATRegister |= (byte)DMAF3_StatusCodes.DMAF3_MOTOR_ON;

                                    m_DMAF3_STATRegister &= (byte)(~((byte)DMAF3_StatusCodes.DMAF3_LOST_DATA | (byte)DMAF3_StatusCodes.DMAF3_CRCERR | (byte)DMAF3_StatusCodes.DMAF3_RNF | (byte)DMAF3_StatusCodes.DMAF3_RECORD_TYPE_SPIN_UP) & 0xFF);
                                    m_DMAF3_DRVRegister &= (byte)(~((byte)DMAF3_ContollerLines.DRV_DRQ) & 0xFF);

                                    m_DMAF3_DMA_DMCNTRegister[0].hibyte = m_DMAF3_DMA_DMCNTRegister[0].lobyte = 0;

                                    if (_bInterruptEnabled)
                                    {
                                        StartDMAF3InterruptDelayTimer(m_nInterruptDelay, (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA1 | (int)DeviceInterruptMask.DEVICE_INT_MASK_FDC);
                                        if (Program._cpu != null)
                                        {
                                            if ((Program._cpu.InWait || Program._cpu.InSync) && Program._cpuThread.ThreadState == ThreadState.Suspended)
                                            {
                                                try
                                                {
                                                    Program._cpuThread.Resume();
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
                                m_DMAF3_STATRegister |= (byte)DMAF3_StatusLines.DMAF3_NOTREADY;
                                m_DMAF3_STATRegister &= (byte)(~((byte)DMAF3_StatusLines.DMAF3_DRQ) & 0xFF);
                                m_DMAF3_DRVRegister  &= (byte)(~((byte)DMAF3_ContollerLines.DRV_DRQ) & 0xFF);

                                StartDMAF3InterruptDelayTimer(m_nInterruptDelay, (int)DeviceInterruptMask.DEVICE_INT_MASK_FDC);
                            }
                            break;

                        case 4:
                            // used to include:
                            //DMAF3_StatusCodes.DMAF3_CRCERR |   // clear CRC Error Bit
                            //DMAF3_StatusCodes.DMAF3_RNF    |   // clear Record Not Found Bit

                            m_DMAF3_STATRegister &= (byte)(~((byte)(
                                                        DMAF3_StatusLines.DMAF3_DRQ    |   // clear Data Request Bit
                                                        DMAF3_StatusLines.DMAF3_SEEKERR|   // clear SEEK Error Bit
                                                        DMAF3_StatusLines.DMAF3_BUSY       // clear BUSY bit
                                                     )) & 0xFF);
                            m_DMAF3_DRVRegister  &= (byte)(~((byte)DMAF3_ContollerLines.DRV_DRQ) & 0xFF);        // turn off high order bit in drive status register

                            if (m_nInterruptConditionFlag != 0)
                            {
                                // if the m_nInterruptConditionFlag is 0 that means we should 'Terminate With No Interrupt (INTRQ)'
                                // all other condition - we should do an immediate interrupt

                                // Ix = Interrupt Condition Flags
                                //
                                // I0 = 1 Not Ready to Ready Transition
                                // I1 = 1 Ready To Not Ready Transition
                                // I2 = 1 Index Pulse
                                // I3 = 1 Immediate Interrupt, Requires A Reset*
                                // I3-I0 = 0 Terminate With No Interrupt (INTRQ)

                                if (_bInterruptEnabled)
                                {
                                    StartDMAF3InterruptDelayTimer(m_nInterruptDelay, (int)DeviceInterruptMask.DEVICE_INT_MASK_FDC);
                                    if (Program._cpu != null)
                                    {
                                        if ((Program._cpu.InWait || Program._cpu.InSync) && Program._cpuThread.ThreadState == ThreadState.Suspended)
                                        {
                                            try
                                            {
                                                Program._cpuThread.Resume();
                                            }
                                            catch (ThreadStateException e)
                                            {
                                                // do nothing if thread is not suspended
                                            }
                                        }
                                    }
                                }
                            }
                            break;
                    }

                    break;

                case DMAF3_OffsetRegisters.DMAF3_TRKREG_OFFSET:                       // 0xF021
                    m_DMAF3_TRKRegister = b;
                    m_nFDCTrack = b;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_SECREG_OFFSET:                       // 0xF022
                    m_DMAF3_SECRegister = b;
                    m_nFDCSector = b;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_HLD_TOGGLE_OFFSET:                   // 0xF050 - head load toggle
                    DMAF3_HLD_TOGGLERegister = b;
                    break;

            }
        }

        void WriteDMA (ushort m, byte b)
        {
            int nWhichRegister = m - m_sBaseAddress;

            string strActivityMessage;

            //strActivityMessage = string.Format ("Writing DMA Register {0} Address 0x{1} with 0x{2}\n", nWhichRegister.ToString("X2"), m.ToString("X4"), b.ToString("X2"));
            //LogFloppyActivity (-1, strActivityMessage);

            if (Program.DMAF3AccessLogging)
            {
                strActivityMessage = string.Format("Writing DMA Register {0} Address 0x{1} with 0x{2}\n", nWhichRegister.ToString("X2"), m.ToString("X4"), b.ToString("X2"));
                LogActivity (-1, strActivityMessage, true);
            }

            switch (nWhichRegister)
            {
                case (int)DMAF3_OffsetRegisters.DMAF3_LATCH_OFFSET:
                    m_DMAF3_LATCHRegister = b;
                    break;

                //*
                //*   DMAF3 6844 DMA controller definitions
                //*

                case (int)DMAF3_OffsetRegisters.DMAF3_DMA_ADDRESS_OFFSET + 0:
                    m_DMAF3_DMA_ADDRESSRegister[0].hibyte = b;
                    break;

                case (int)DMAF3_OffsetRegisters.DMAF3_DMA_ADDRESS_OFFSET + 1:
                    m_DMAF3_DMA_ADDRESSRegister[0].lobyte = b;
                    break;

                case (int)DMAF3_OffsetRegisters.DMAF3_DMA_ADDRESS_OFFSET + 4:
                    m_DMAF3_DMA_ADDRESSRegister[1].hibyte = b;
                    break;

                case (int)DMAF3_OffsetRegisters.DMAF3_DMA_ADDRESS_OFFSET + 5:
                    m_DMAF3_DMA_ADDRESSRegister[1].lobyte = b;
                    break;

                case (int)DMAF3_OffsetRegisters.DMAF3_DMA_ADDRESS_OFFSET + 8:
                    m_DMAF3_DMA_ADDRESSRegister[2].hibyte = b;
                    break;

                case (int)DMAF3_OffsetRegisters.DMAF3_DMA_ADDRESS_OFFSET + 9:
                    m_DMAF3_DMA_ADDRESSRegister[2].lobyte = b;
                    break;

                case (int)DMAF3_OffsetRegisters.DMAF3_DMA_ADDRESS_OFFSET + 12:
                    m_DMAF3_DMA_ADDRESSRegister[3].hibyte = b;
                    break;

                case (int)DMAF3_OffsetRegisters.DMAF3_DMA_ADDRESS_OFFSET + 13:
                    m_DMAF3_DMA_ADDRESSRegister[3].lobyte = b;
                    break;

                case (int)DMAF3_OffsetRegisters.DMAF3_DMA_DMCNT_OFFSET + 0:
                    m_DMAF3_DMA_DMCNTRegister[0].hibyte = b;
                    break;

                case (int)DMAF3_OffsetRegisters.DMAF3_DMA_DMCNT_OFFSET + 1:
                    m_DMAF3_DMA_DMCNTRegister[0].lobyte = b;
                    break;

                case (int)DMAF3_OffsetRegisters.DMAF3_DMA_DMCNT_OFFSET + 4:
                    m_DMAF3_DMA_DMCNTRegister[1].hibyte = b;
                    break;

                case (int)DMAF3_OffsetRegisters.DMAF3_DMA_DMCNT_OFFSET + 5:
                    m_DMAF3_DMA_DMCNTRegister[1].lobyte = b;
                    break;

                case (int)DMAF3_OffsetRegisters.DMAF3_DMA_DMCNT_OFFSET + 8:
                    m_DMAF3_DMA_DMCNTRegister[2].hibyte = b;
                    break;

                case (int)DMAF3_OffsetRegisters.DMAF3_DMA_DMCNT_OFFSET + 9:
                    m_DMAF3_DMA_DMCNTRegister[2].lobyte = b;
                    break;

                case (int)DMAF3_OffsetRegisters.DMAF3_DMA_DMCNT_OFFSET + 12:
                    m_DMAF3_DMA_DMCNTRegister[3].hibyte = b;
                    break;

                case (int)DMAF3_OffsetRegisters.DMAF3_DMA_DMCNT_OFFSET + 13:
                    m_DMAF3_DMA_DMCNTRegister[3].lobyte = b;
                    break;

                case (int)DMAF3_OffsetRegisters.DMAF3_DMA_CHANNEL_OFFSET:
                    m_DMAF3_DMA_CHANNELRegister[0] = (byte)(b & 0x0F);
                    break;
                case (int)DMAF3_OffsetRegisters.DMAF3_DMA_CHANNEL_OFFSET + 1:
                    m_DMAF3_DMA_CHANNELRegister[1] = (byte)(b & 0x0F);
                    break;
                case (int)DMAF3_OffsetRegisters.DMAF3_DMA_CHANNEL_OFFSET + 2:
                    m_DMAF3_DMA_CHANNELRegister[2] = (byte)(b & 0x0F);
                    break;
                case (int)DMAF3_OffsetRegisters.DMAF3_DMA_CHANNEL_OFFSET + 3:
                    m_DMAF3_DMA_CHANNELRegister[3] = (byte)(b & 0x0F);
                    break;

                case (int)DMAF3_OffsetRegisters.DMAF3_DMA_PRIORITY_OFFSET:
                    m_DMAF3_DMA_PRIORITYRegister = b;
                    break;

                case (int)DMAF3_OffsetRegisters.DMAF3_DMA_INTERRUPT_OFFSET:
                    m_DMAF3_DMA_INTERRUPTRegister = (byte)(b & 0x7F);
                    break;

                case (int)DMAF3_OffsetRegisters.DMAF3_DMA_CHAIN_OFFSET:
                    m_DMAF3_DMA_CHAINRegister = b;
                    break;

            }
        }

        void WriteWD1000 (ushort m, byte b)
        {
            string strDriveType = "Winchester";

            int nWhichRegister = m - m_sBaseAddress;
            int activechannel = -1;

            switch (m_DMAF3_DMA_PRIORITYRegister & 0x0F)
            {
                case 1:
                    activechannel = 0;      // Floppy
                    break;
                case 2:
                    activechannel = 1;      // WD1000
                    break;
                case 4:
                    activechannel = 2;
                    //AfxMessageBox ("DMA channel set to 2 - not supported");
                    break;
                case 8:
                    activechannel = 3;
                    //AfxMessageBox ("DMA channel set to 3 - not supported");
                    break;
            }

            byte   latch     = m_DMAF3_LATCHRegister;
            ushort  addr      = 0;
            ushort  cnt       = 0;
            byte   priority  = m_DMAF3_DMA_PRIORITYRegister;
            byte   interrupt = m_DMAF3_DMA_INTERRUPTRegister;
            byte   chain     = m_DMAF3_DMA_CHAINRegister;

            if (activechannel == 1)
            {
                addr      = (ushort)(m_DMAF3_DMA_ADDRESSRegister[activechannel].hibyte * 256 + m_DMAF3_DMA_ADDRESSRegister[activechannel].lobyte);
                cnt       = (ushort)(m_DMAF3_DMA_DMCNTRegister[activechannel].hibyte   * 256 + m_DMAF3_DMA_DMCNTRegister[activechannel].lobyte);
            }

            int nDrive    = (DMAF3_WDC_SDHRegister >> 3) & 0x03;

            if (Program.DMAF3AccessLogging)
            {
                string strActivityMessage = string.Format 
                (
                    "Writing Address {0} with {1} addr: {2} latch: {3} cnt: {4} priority: {5} interrupt: {6} chain: {7}\n", 
                        m.ToString("X4"),
                        b.ToString("X2"),
                        addr.ToString("X4"),
                        latch.ToString("X2"),
                        cnt.ToString(),
                        priority.ToString("X2"),
                        interrupt.ToString("X2"),
                        chain.ToString("X2")
                );
                strActivityMessage = string.Format("Writing DMA Register {0} Address 0x{1} with 0x{2}\n", nWhichRegister.ToString("X2"), m.ToString("X4"), b.ToString("X2"));
                LogActivity(-1, strActivityMessage, true);
            }
            //LogFloppyActivity (nDrive, strActivityMessage);

            switch (nWhichRegister)
            {
                //  WD1000 5" Winchester interface

                case (int)DMAF3_OffsetRegisters.DMAF3_WDC_DATA_OFFSET:         // 0x30 - WDC data register
                    DMAF3_WDC_DATARegister = b;
                    break;

                case (int)DMAF3_OffsetRegisters.DMAF3_WDC_PRECOMP_OFFSET:      // 0x31 - WDC error register
                    DMAF3_WDC_PrecompRegister = b;
                    break;

                case (int)DMAF3_OffsetRegisters.DMAF3_WDC_SECCNT_OFFSET:       // 0x32 - sector count (during format)
                    DMAF3_WDC_SECCNTRegister = b;
                    break;

                case (int)DMAF3_OffsetRegisters.DMAF3_WDC_SECNUM_OFFSET:       // 0x33 - WDC sector number
                    DMAF3_WDC_SECNUMRegister = b;
                    break;

                case (int)DMAF3_OffsetRegisters.DMAF3_WDC_CYLLO_OFFSET:        // 0x34 - WDC cylinder (low part - C0-C7)
                    DMAF3_WDC_CYLLORegister = b;
                    break;

                case (int)DMAF3_OffsetRegisters.DMAF3_WDC_CYLHI_OFFSET:        // 0x35 - WDC cylinder (high part - C8-C9)
                    DMAF3_WDC_CYLHIRegister = b;
                    break;

                case (int)DMAF3_OffsetRegisters.DMAF3_WDC_SDH_OFFSET:          // 0x36 - WDC size/drive/head
                                                    //    wd_sdh     equ     wd1000+6       size/drive/head
                                                    //    *                                 bit 7 XX,
                                                    //    *                                 bit 6,5 sector size (256,512,128)
                                                    //    *                                 bit 4,3 drive select (0,1,2,3)
                                                    //    *                                 bit 2,1,0 head select (0-7)
                                                    //    wd_secsize equ     %00000000      256 byte sectors
                                                    //    wd_sz_512  equ     %00100000      512 byte sectors
                                                    //    wd_sel0    equ     %00000000      select drive zero
                                                    //    wd_sel1    equ     %00001000      select drive one
                                                    //    wd_sel2    equ     %00010000      select drive two
                                                    //    wd_sel3    equ     %00011000      select drive three
                    DMAF3_WDC_SDHRegister = b;
                    break;

                case (int)DMAF3_OffsetRegisters.DMAF3_WDC_CMD_OFFSET:          // 0x37 - WDC command register
                                                    //    wd_seek    equ     %01110000      seek with 10us step rate 
                                                    //    wd_read    equ     %00101000      read sector DMA          
                                                    //    wd_write   equ     %00110000      write sector             
                                                    //    wd_format  equ     %01010000      format track (SPECIAL USAGE)
                                                    //    wd_restore equ     %00010110      restore with 3ms step rate
                    {
                        DMAF3_WDC_STATUSRegister &= (byte)(~(byte)WDC_StatusCodes.DMAF3_WDC_SEEK_COMPLETE & 0xFF);  // clear seek complete on any new command

                        nDrive    = (DMAF3_WDC_SDHRegister >> 3) & 0x03;

                        int nHead     = (DMAF3_WDC_SDHRegister) & 0x07;
                        int nCylinder = ((int) (DMAF3_WDC_CYLHIRegister & 0x03) * 256) + (int) DMAF3_WDC_CYLLORegister;

                        //int nSector   = (DMAF3_WDC_CYLHIRegister >> 2) & 0x3F;

                        int nSector   = DMAF3_WDC_SECNUMRegister - 1;

                        int nCylinders       = m_hdpWinchester[nDrive].nCylinders       ;
                        int nHeads           = m_hdpWinchester[nDrive].nHeads           ;
                        int nSectorsPerTrack = m_hdpWinchester[nDrive].nSectorsPerTrack ;

                        //int nBytesPerSector  = Program.m_hdpWinchester[nDrive].nBytesPerSector  ;

                        int nBytesPerSector = (DMAF3_WDC_SDHRegister >> 5) & 0x03;
                        switch (nBytesPerSector)
                        {
                            case 0:
                                nBytesPerSector = 256;
                                break;
                            case 1:
                                nBytesPerSector = 512;
                                break;
                            case 2:
                                nBytesPerSector = 128;
                                break;
                            case 3:
                                nBytesPerSector = 1024;
                                break;
                        }

                        if (activechannel == -1)
                            m_nBytesToTransfer = nBytesPerSector;
                        else
                            m_nBytesToTransfer = cnt;


                        switch (b & 0xF0)
                        {
                                                        //    wd_status  equ     wd1000+7       status (read only)
                                                        //    *                                 bit 7 busy
                                                        //    *                                 bit 6 ready
                                                        //    *                                 bit 5 write fault
                                                        //    *                                 bit 4 seek complete
                                                        //    *                                 bit 3 data request
                                                        //    *                                 bit 2,1 unused
                                                        //    *                                 bit 0 error (code in wd_error

                            case 0x10:                  //    wd_restore equ     %00010110      restore with 3ms step rate
                                DMAF3_WDC_STATUSRegister |= (byte)(WDC_StatusCodes.DMAF3_WDC_READY | WDC_StatusCodes.DMAF3_WDC_SEEK_COMPLETE);   // set ready and seek complete
                                m_fpWinchester[nDrive].Seek(0, SeekOrigin.Begin);
                                if ((m_DMAF3_LATCHRegister & 0x10) != 0)
                                    StartDMAF3InterruptDelayTimer(m_nInterruptDelay, (int)DeviceInterruptMask.DEVICE_INT_MASK_WDC);
                                break;

                            case 0x20:                  //    wd_read    equ     %00101000      read sector DMA
                                {
                                    if ((nCylinder < nCylinders) && (nHead < nHeads) && (nSector < nSectorsPerTrack))
                                    {
                                        CalcHDFileOffset(strDriveType, WDC_COMMAND.WDC_RDWR);
                                        if (m_fpWinchester[nDrive] != null)
                                        {
                                            m_fpWinchester[nDrive].Seek(m_lFileOffset, SeekOrigin.Begin);
                                            m_fpWinchester[nDrive].Read (m_caReadBuffer, 0, m_nBytesToTransfer);

                                            // if the dma is active - get the bytes from memory, write them to the buffer and reset the dma counter

                                            if (activechannel == 1)
                                            {
                                                if (cnt > 0 && activechannel == 1)
                                                {
                                                    //Program.pStaticWinchesterActivityLight[nDrive]->SetBitmap (Program.m_hGreenDot);

                                                    ulong lPhysicalAddress = LogicalToPhysicalAddress(latch, addr);

                                                    //string strActivityMessage = string.Format("Reading %4d bytes from winchester %d track %02d head %02d sector %02d to   offset %06X WDC offset 0x%08X\n", cnt, nDrive, nCylinder, nHead, nSector, lPhysicalAddress, m_lFileOffset);
                                                    //LogFloppyActivity(nDrive, strActivityMessage);

                                                    if (Program.DMAF3AccessLogging)
                                                    {
                                                        string strActivityMessage = string.Format("Reading {0} bytes from winchester {1} cylinder 0x{2} head 0x{3} sector 0x{4} from offset 0x{5} DSK offset 0x{6}\n", cnt.ToString(), nDrive.ToString(), nCylinder.ToString("X2"), nHead.ToString("X2"), nSector.ToString("X2"), lPhysicalAddress.ToString("X6"), m_lFileOffset.ToString("X8"));
                                                        LogActivity (nDrive, strActivityMessage, true);
                                                    }   

                                                    for (int i = 0; i < cnt; i++)
                                                    {
                                                        if ((m_DMAF3_DMA_CHANNELRegister[activechannel] & 0x08) == 0x08)
                                                            Program._cpu.Memory.MemorySpace[(int)((int)lPhysicalAddress - i)] = m_caReadBuffer[i];
                                                        else
                                                            Program._cpu.Memory.MemorySpace[(int)((int)lPhysicalAddress + i)] = m_caReadBuffer[i];
                                                    }

                                                    ushort a = (ushort)(m_DMAF3_DMA_ADDRESSRegister[activechannel].hibyte * 256 + m_DMAF3_DMA_ADDRESSRegister[activechannel].lobyte);
                                                    if ((m_DMAF3_DMA_CHANNELRegister[activechannel] & 0x08) == 0x08)
                                                        a -= cnt;
                                                    else
                                                        a += cnt;

                                                    m_DMAF3_DMA_DMCNTRegister[activechannel].hibyte = 0;
                                                    m_DMAF3_DMA_DMCNTRegister[activechannel].lobyte = 0;

                                                    m_DMAF3_DMA_ADDRESSRegister[activechannel].hibyte = (byte)(a / 256);
                                                    m_DMAF3_DMA_ADDRESSRegister[activechannel].lobyte = (byte)(a % 256);

                                                    DMAF3_WDC_STATUSRegister &= (byte)(~((byte)(WDC_StatusCodes.DMAF3_WDC_DATA_REQUEST | WDC_StatusCodes.DMAF3_WDC_BUSY)) & 0xFF);
                                                    DMAF3_WDC_STATUSRegister |= (byte)((byte)WDC_StatusCodes.DMAF3_WDC_SEEK_COMPLETE & 0xFF);  // set seek complete

                                                    if (_bInterruptEnabled && (m_DMAF3_LATCHRegister & 0x10) == 0x10)
                                                    {
                                                        // delay setting the interrupt until the timer times out.

                                                        StartDMAF3InterruptDelayTimer(m_nInterruptDelay, (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA2);
                                                    }
                                                }
                                                //else
                                                //{
                                                //    //string strMessage = string.Format ("DMA attempting to use channel %d for WDC during read", activechannel);
                                                //    //AfxMessageBox (strMessage);
                                                //}
                                            }
                                            else
                                            {
                                                // Do polled IO - NOT DMA

                                                //AfxMessageBox ("Polled IO not implemented for Winchester Read");

                                                DMAF3_WDC_DATARegister = m_caReadBuffer[0];
                                                DMAF3_WDC_STATUSRegister |= (byte)(WDC_StatusCodes.DMAF3_WDC_DATA_REQUEST);
                                            }
                                        }
                                    }
                                }
                                break;

                            case 0x30:                  //    wd_write   equ     %00110000      write sector
                                {
                                    if ((nCylinder < nCylinders) && (nHead < nHeads) && (nSector < nSectorsPerTrack))
                                    {
                                        CalcHDFileOffset(strDriveType, WDC_COMMAND.WDC_RDWR);
                                        if (m_fpWinchester[nDrive] != null)
                                        {
                                            if (activechannel == 1)
                                            {
                                                if (cnt > 0 && activechannel == 1)
                                                {
                                                    //Program.pStaticWinchesterActivityLight[nDrive]->SetBitmap (Program.m_hRedDot);

                                                    ulong lPhysicalAddress = LogicalToPhysicalAddress (latch, addr);

                                                    //CString strActivityMessage;
                                                    //strActivityMessage.Format ("Writing %4d bytes to winchester %d track %02d head %02d sector %02d from offset %06X WDC offset 0x%08X\n", cnt, nDrive, nCylinder, nHead, nSector, lPhysicalAddress, m_lFileOffset);
                                                    //LogFloppyActivity (nDrive, strActivityMessage);

                                                    if (Program.DMAF3AccessLogging)
                                                    {
                                                        string strActivityMessage = string.Format("Writing {0} bytes to winchester {1} cylinder 0x{2} head 0x{3} sector 0x{4} from offset 0x{5} DSK offset 0x{6}\n", cnt.ToString(), nDrive.ToString(), nCylinder.ToString("X2"), nHead.ToString("x2"), nSector.ToString("X2"), lPhysicalAddress.ToString("X6"), m_lFileOffset.ToString("X8"));
                                                        LogActivity(nDrive, strActivityMessage, true);
                                                    }

                                                    m_fpWinchester[nDrive].Seek(m_lFileOffset, SeekOrigin.Begin);

                                                    if ((m_DMAF3_DMA_CHANNELRegister[activechannel] & 0x08) == 0x08)
                                                        m_fpWinchester[nDrive].Write(Program._cpu.Memory.MemorySpace, (int)(lPhysicalAddress - cnt), (int)cnt);
                                                    else
                                                        m_fpWinchester[nDrive].Write(Program._cpu.Memory.MemorySpace, (int)(lPhysicalAddress), (int)cnt);

                                                    m_fpWinchester[nDrive].Flush();

                                                    ushort a = (ushort)(m_DMAF3_DMA_ADDRESSRegister[activechannel].hibyte * 256 + m_DMAF3_DMA_ADDRESSRegister[activechannel].lobyte);
                                                    if ((m_DMAF3_DMA_CHANNELRegister[activechannel] & 0x08) == 0x08)
                                                        a -= cnt;
                                                    else
                                                        a += cnt;

                                                    m_DMAF3_DMA_DMCNTRegister[activechannel].hibyte = 0;
                                                    m_DMAF3_DMA_DMCNTRegister[activechannel].lobyte = 0;

                                                    m_DMAF3_DMA_ADDRESSRegister[activechannel].hibyte = (byte)(a / 256);
                                                    m_DMAF3_DMA_ADDRESSRegister[activechannel].lobyte = (byte)(a % 256);

                                                    DMAF3_WDC_STATUSRegister &= (byte)(~((byte)(WDC_StatusCodes.DMAF3_WDC_DATA_REQUEST | WDC_StatusCodes.DMAF3_WDC_BUSY)) & 0xFF);
                                                    DMAF3_WDC_STATUSRegister |= (byte)((byte)WDC_StatusCodes.DMAF3_WDC_SEEK_COMPLETE & 0xFF);  // set seek complete

                                                    if (_bInterruptEnabled && (m_DMAF3_LATCHRegister & 0x10) == 0x10)
                                                    {
                                                        // delay setting the interrupt until the timer times out.

                                                        StartDMAF3InterruptDelayTimer(m_nInterruptDelay, (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA2);
                                                    }
                                                }
                                                //else
                                                //{
                                                //    CString strMessage;
                                                //    strMessage.Format ("DMA attempting to use channel %d for WDC during read", activechannel);
                                                //    //AfxMessageBox (strMessage);
                                                //}
                                            }
                                            else
                                            {
                                                // Do polled IO - NOT DMA

                                                //AfxMessageBox ("Polled IO not implemented for Winchester Write");

                                                //DMAF3_WDC_DATARegister = m_caReadBuffer[0];
                                                DMAF3_WDC_STATUSRegister |= (byte)(WDC_StatusCodes.DMAF3_WDC_DATA_REQUEST);
                                            }
                                        }
                                    }
                                }
                                break;

                            case 0x40:
                                //AfxMessageBox ("Unimplemented WDC Command 0x4X");
                                break;

                            case 0x50:                  //    wd_format  equ     %01010000      format track (SPECIAL USAGE)
                                {
                                    //Program.pStaticWinchesterActivityLight[nDrive]->SetBitmap (Program.m_hRedDot);

                                    byte [] pFormatBuffer = new byte [nBytesPerSector * DMAF3_WDC_SECCNTRegister];
                                    memset (pFormatBuffer, (byte)'\0', (int)(nBytesPerSector * DMAF3_WDC_SECCNTRegister));

                                    // calc offset to beginning of the track

                                    CalcHDFileOffset(strDriveType, WDC_COMMAND.WDC_FORMAT);

                                    long length = m_fpWinchester[nDrive].Length;

                                    //strActivityMessage.Format ("Writing %4d bytes to winchester %d cylinder %02d head %02d sector %02d DSK offset 0x%08X\n", cnt, nDrive, nCylinder, nHead, nSector, m_lFileOffset);
                                    //LogFloppyActivity (nDrive, strActivityMessage);

                                    if (Program.DMAF3AccessLogging)
                                    {
                                        string strActivityMessage = string.Format("Writing {0} bytes to winchester {1} cylinder 0x{2} head 0x{3} sector 0x{4} from offset 0x{5} DSK offset 0x{6}\n", cnt.ToString(), nDrive.ToString(), nCylinder.ToString("X2"), nHead.ToString("x2"), nSector.ToString("X2"), m_lFileOffset.ToString("X8"));
                                        LogActivity(nDrive, strActivityMessage, true);
                                    }

                                    if (length < (m_lFileOffset + (nBytesPerSector * DMAF3_WDC_SECCNTRegister)))
                                    {
                                        // close file for "r+b" and reopen for append - write the bytes - close and reopen for "r+b"

                                        m_fpWinchester[nDrive].Close();
                                        m_fpWinchester[nDrive] = File.Open(m_cWinchesterDrivePathName[nDrive], FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                                        m_fpWinchester[nDrive].Write(pFormatBuffer, 0, nBytesPerSector * DMAF3_WDC_SECCNTRegister);

                                        m_fpWinchester[nDrive].Close();
                                        m_fpWinchester[nDrive] = File.Open(m_cWinchesterDrivePathName[nDrive], FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                                    }
                                    else
                                    {
                                        m_fpWinchester[nDrive].Seek(m_lFileOffset, SeekOrigin.Begin);
                                        m_fpWinchester[nDrive].Write(pFormatBuffer, 0, nBytesPerSector * DMAF3_WDC_SECCNTRegister);
                                        m_fpWinchester[nDrive].Flush();
                                    }

                                    DMAF3_WDC_STATUSRegister |= (byte)(WDC_StatusCodes.DMAF3_WDC_READY | WDC_StatusCodes.DMAF3_WDC_SEEK_COMPLETE);   // set ready and seek complete

                                    m_DMAF3_DMA_DMCNTRegister[activechannel].hibyte = 0;
                                    m_DMAF3_DMA_DMCNTRegister[activechannel].lobyte = 0;

                                    if (_bInterruptEnabled && (m_DMAF3_LATCHRegister & 0x10) == 0x10)
                                        StartDMAF3InterruptDelayTimer(m_nInterruptDelay, (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA2);
                                }
                                break;

                            case 0x60:
                                //AfxMessageBox ("Unimplemented WDC Command 0x6X");
                                break;

                            case 0x70:                  //    wd_seek    equ     %01110000      seek with 10us step rate

                                CalcHDFileOffset(strDriveType, WDC_COMMAND.WDC_SEEK);
                                m_fpWinchester[nDrive].Seek(m_lFileOffset, SeekOrigin.Begin);
                                DMAF3_WDC_STATUSRegister |= (byte)(WDC_StatusCodes.DMAF3_WDC_READY | WDC_StatusCodes.DMAF3_WDC_SEEK_COMPLETE);   // set ready and seek complete

                                if (_bInterruptEnabled && (m_DMAF3_LATCHRegister & 0x10) == 0x10)
                                    StartDMAF3InterruptDelayTimer(m_nInterruptDelay, (int)DeviceInterruptMask.DEVICE_INT_MASK_WDC);

                                break;

                            case 0x00:
                                break;

                            default:
                                //AfxMessageBox ("Unimplemented WDC Command");
                                break;
                        }
                    }
                    break;

                case (int)DMAF3_OffsetRegisters.DMAF_WD1000_RES_OFFSET:        // 0x51 - winchester software reset
            
                    DMAF_WD1000_RESRegister     = b;

                    DMAF3_WDC_DATARegister      = 0;                                             // WDC data register
                    DMAF3_WDC_ERRORRegister     = 0;                                             // WDC error register
                    DMAF3_WDC_SECNUMRegister    = 0;                                             // WDC sector number
                    DMAF3_WDC_CYLLORegister     = 0;                                             // WDC cylinder (low part)
                    DMAF3_WDC_CYLHIRegister     = 0;                                             // WDC cylinder (high part)
                    DMAF3_WDC_SDHRegister       = 0;                                             // WDC size/drive/head
                    DMAF3_WDC_STATUSRegister    = (byte)(WDC_StatusCodes.DMAF3_WDC_READY | WDC_StatusCodes.DMAF3_WDC_SEEK_COMPLETE);   // set WDC status register to Ready and not busy
                    DMAF3_WDC_CMDRegister       = 0;                                             // WDC command register
                    break;

            }
        }

        void WriteTape (ushort m, byte b)
        {
            int nWhichRegister = m - m_sBaseAddress;
            int activechannel = -1;

            switch (m_DMAF3_DMA_PRIORITYRegister & 0x0F)
            {
                case 1:
                    activechannel = 0;      // Floppy
                    break;
                case 2:
                    activechannel = 1;      // WD1000
                    break;
                case 4:
                    activechannel = 2;
                    //AfxMessageBox ("DMA channel set to 2 - not supported");
                    break;
                case 8:
                    activechannel = 3;
                    //AfxMessageBox ("DMA channel set to 3 - not supported");
                    break;
            }

            byte   latch     = m_DMAF3_LATCHRegister;
            ushort  addr      = 0;
            ushort  cnt       = 0;
            byte   priority  = m_DMAF3_DMA_PRIORITYRegister;
            byte   interrupt = m_DMAF3_DMA_INTERRUPTRegister;
            byte   chain     = m_DMAF3_DMA_CHAINRegister;

            if (activechannel >= 0)
            {
                addr      = (ushort)(m_DMAF3_DMA_ADDRESSRegister[activechannel].hibyte * 256 + m_DMAF3_DMA_ADDRESSRegister[activechannel].lobyte);
                cnt       = (ushort)(m_DMAF3_DMA_DMCNTRegister[activechannel].hibyte   * 256 + m_DMAF3_DMA_DMCNTRegister[activechannel].lobyte);
            }

            //CString strActivityMessage;
            //strActivityMessage.Format ("Writing Address 0x%04X with 0x%02X addr: 0x%04X latch: 0x%02X cnt: %d priority: 0x%02X interrupt: 0x%02X chain: 0x%02X\n", m, b, addr, latch, cnt, priority, interrupt, chain);
            //LogFloppyActivity (nDrive, strActivityMessage);

            if (Program.DMAF3AccessLogging)
            {
                int nDrive = 5;

                string strActivityMessage = string.Format("Writing Address 0x{0} with 0x{1} addr: 0x{2} latch: 0x{3} cnt: {4} priority: 0x{5} interrupt: 0x{6} chain: 0x{7}\n", m.ToString("X4"), b.ToString("X2"), addr.ToString("X4"), latch.ToString("X2"), cnt.ToString(), priority.ToString("X2"), interrupt.ToString("X2"), chain.ToString("X2"));
                LogActivity(nDrive, strActivityMessage, true);
            }

            switch ((DMAF3_OffsetRegisters)nWhichRegister)
            {
                case DMAF3_OffsetRegisters.DMAF3_AT_DTB_OFFSET:           // 0x40 - B-Side Data Register  DMAF3 status lines
                    DMAF3_AT_DTBRegister = b;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_AT_DTA_OFFSET:           // 0x41 - A-Side Data Register
                    DMAF3_AT_DTARegister = b;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_AT_DRB_OFFSET:           // 0x42 - B-Side Direction Register
                    DMAF3_AT_DRBRegister = b;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_AT_DRA_OFFSET:           // 0x43 - A-Side Direction Register
                    DMAF3_AT_DRARegister = b;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_AT_T1C_OFFSET:           // 0x44 - Timer 1 Counter Register
                    // writing to the counters actually writes to the latches
                    //
                    m_DMAF3_AT_TLRegister.lobyte = b;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_AT_T1C_OFFSET + 1:       // 0x45 - Timer 1 Counter Register
                    // writing to the counters actually writes to the latches
                    //
                    m_DMAF3_AT_TLRegister.hibyte = b;

                    // writing to the high order byte causes the latch to load to the counter

                    m_DMAF3_AT_TCRegister[0].hibyte = m_DMAF3_AT_TLRegister.hibyte;
                    m_DMAF3_AT_TCRegister[0].lobyte = m_DMAF3_AT_TLRegister.lobyte;
                    DMAF3_AT_IFRRegister = (byte)(DMAF3_AT_IFRRegister & ~0x40);
                    m_nEnableT1C_Countdown = true;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_AT_T1L_OFFSET:           // 0x46 - Timer 1 Latches
                    m_DMAF3_AT_TLRegister.lobyte = b;
                    break;
                case DMAF3_OffsetRegisters.DMAF3_AT_T1L_OFFSET + 1:       // 0x47 - Timer 1 Latches
                    m_DMAF3_AT_TLRegister.hibyte = b;
                    DMAF3_AT_IFRRegister = (byte)(DMAF3_AT_IFRRegister & ~0x40);
                    m_nEnableT1C_Countdown = true;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_AT_T2C_OFFSET:           // 0x48 - Timer 2 Counter Register
                    m_DMAF3_AT_TCRegister[1].lobyte = b;
                    break;
                case DMAF3_OffsetRegisters.DMAF3_AT_T2C_OFFSET + 1:       // 0x49 - Timer 2 Counter Register
                    m_DMAF3_AT_TCRegister[1].hibyte = b;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_AT_VSR_OFFSET:           // 0x4A - Shift Register
                    DMAF3_AT_VSRRegister = b;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_AT_ACR_OFFSET:           // 0x4B - Auxillary Control Register
                    DMAF3_AT_ACRRegister = b;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_AT_PCR_OFFSET:           // 0x4C - Peripheral Control Register
                    DMAF3_AT_PCRRegister = b;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_AT_IFR_OFFSET:           // 0x4D - Interrupt Flag Register
                    DMAF3_AT_IFRRegister = (byte)(DMAF3_AT_IFRRegister & ~b);
                    break;

                case DMAF3_OffsetRegisters.DMAF3_AT_IER_OFFSET:           // 0x4E - Interrupt Enable Register
                    //
                    //  If bit 7 is not set we want to clear bits, not set them, otherwise we want to set bits.
                    //
                    if ((b & 0x80) == 0x00)         
                        DMAF3_AT_IERRegister = (byte)(DMAF3_AT_IERRegister & ~(b | 0x80));
                    else
                        DMAF3_AT_IERRegister = (byte)(DMAF3_AT_IERRegister | (b & 0x7f));
                    break;

                case DMAF3_OffsetRegisters.DMAF3_AT_DXA_OFFSET:           // 0x4F - A-Side Data Register
                    DMAF3_AT_DXARegister = b;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_AT_ATR_OFFSET:           // 0x52 - archive RESET
                    DMAF3_AT_ATRRegister = b;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_AT_DMC_OFFSET:           // 0x53 - archive DMA clear
                    DMAF3_AT_DMCRegister = b;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_AT_DMP_OFFSET:           // 0x60 - archive DMA preset
                    DMAF3_AT_DMPRegister = b;
                    break;
            }
        }

        void WriteCDS (ushort m, byte b)
        {
            int nWhichRegister = m - m_sBaseAddress;
            int activechannel = -1;

            switch (m_DMAF3_DMA_PRIORITYRegister & 0x0F)
            {
                case 1:
                    activechannel = 0;      // Floppy
                    break;
                case 2:
                    activechannel = 1;      // WD1000
                    break;
                case 4:
                    activechannel = 2;
                    //AfxMessageBox ("DMA channel set to 2 - not supported");
                    break;
                case 8:
                    activechannel = 3;
                    //AfxMessageBox ("DMA channel set to 3 - not supported");
                    break;
            }

            byte   latch     = m_DMAF3_LATCHRegister;
            ushort  addr      = 0;
            ushort  cnt       = 0;
            byte   priority  = m_DMAF3_DMA_PRIORITYRegister;
            byte   interrupt = m_DMAF3_DMA_INTERRUPTRegister;
            byte   chain     = m_DMAF3_DMA_CHAINRegister;

            if (activechannel >= 0)
            {
                addr      = (ushort)(m_DMAF3_DMA_ADDRESSRegister[activechannel].hibyte * 256 + m_DMAF3_DMA_ADDRESSRegister[activechannel].lobyte);
                cnt       = (ushort)(m_DMAF3_DMA_DMCNTRegister[activechannel].hibyte   * 256 + m_DMAF3_DMA_DMCNTRegister[activechannel].lobyte);
            }

            //CString strActivityMessage;
            //strActivityMessage.Format ("Writing Address 0x%04X with 0x%02X addr: 0x%04X latch: 0x%02X cnt: %d priority: 0x%02X interrupt: 0x%02X chain: 0x%02X\n", m, b, addr, latch, cnt, priority, interrupt, chain);
            //LogFloppyActivity (nDrive, strActivityMessage);

            if (Program.DMAF3AccessLogging)
            {
                int nDrive = 6;

                string strActivityMessage = string.Format("Writing Address 0x{0} with 0x{1} addr: 0x{2} latch: 0x{3} cnt: {4} priority: 0x{5} interrupt: 0x{6} chain: 0x{7}\n", m.ToString("X4"), b.ToString("X2"), addr.ToString("X4"), latch.ToString("X2"), cnt.ToString(), priority.ToString("X2"), interrupt.ToString("X2"), chain.ToString("X2"));
                LogActivity(nDrive, strActivityMessage, true);
            }

            switch ((DMAF3_OffsetRegisters)nWhichRegister)
            {
                case DMAF3_OffsetRegisters.DMAF3_CDSCMD0_OFFSET:          // 0x0100      
                    DMAF3_CDSCMD0Register = b;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_CDSFLG0_OFFSET:          // 0x0103 - marksman flag (1xxx xxxx = yes)
                    DMAF3_CDSFLG0Register = b;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_CDSCMD1_OFFSET:          // 0x0300      
                    DMAF3_CDSCMD1Register = b;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_CDSFLG1_OFFSET:          // 0x0303 - marksman flag (1xxx xxxx = yes)
                    DMAF3_CDSFLG1Register = b;
                    break;
            }
        }

        public override void Write (ushort m, byte b)
        {
            DMAF3_OffsetRegisters nWhichRegister = (DMAF3_OffsetRegisters)(m - m_sBaseAddress);

            if ((nWhichRegister >= DMAF3_OffsetRegisters.DMAF3_STATREG_OFFSET && nWhichRegister <= DMAF3_OffsetRegisters.DMAF3_DRVREG_OFFSET) || nWhichRegister == DMAF3_OffsetRegisters.DMAF3_HLD_TOGGLE_OFFSET)
                WriteFloppy (m, b);
            else if ((nWhichRegister >= DMAF3_OffsetRegisters.DMAF3_DMA_ADDRESS_OFFSET && nWhichRegister <= DMAF3_OffsetRegisters.DMAF3_DMA_CHAIN_OFFSET) || nWhichRegister == DMAF3_OffsetRegisters.DMAF3_LATCH_OFFSET)
                WriteDMA (m, b);
            else if ((nWhichRegister >= DMAF3_OffsetRegisters.DMAF3_WDC_DATA_OFFSET && nWhichRegister <= DMAF3_OffsetRegisters.DMAF3_WDC_CMD_OFFSET) || nWhichRegister == DMAF3_OffsetRegisters.DMAF_WD1000_RES_OFFSET)
                WriteWD1000 (m, b);
            else if ((nWhichRegister >= DMAF3_OffsetRegisters.DMAF3_AT_DTB_OFFSET && nWhichRegister <= DMAF3_OffsetRegisters.DMAF3_AT_DXA_OFFSET) || nWhichRegister == DMAF3_OffsetRegisters.DMAF3_AT_ATR_OFFSET || nWhichRegister == DMAF3_OffsetRegisters.DMAF3_AT_DMC_OFFSET || nWhichRegister == DMAF3_OffsetRegisters.DMAF3_AT_DMP_OFFSET)
                WriteTape (m, b);
            else if (nWhichRegister == DMAF3_OffsetRegisters.DMAF3_CDSCMD0_OFFSET || nWhichRegister == DMAF3_OffsetRegisters.DMAF3_CDSFLG0_OFFSET || nWhichRegister == DMAF3_OffsetRegisters.DMAF3_CDSCMD1_OFFSET || nWhichRegister == DMAF3_OffsetRegisters.DMAF3_CDSFLG1_OFFSET)
                WriteCDS (m, b);
            else
                Program._cpu.WriteToFirst64K(m, b);
        }

        byte ReadFloppy (ushort m)
        {
            byte d = 0xFF;

            int nWhichRegister = m - m_sBaseAddress;
            int nDrive =  0;
            switch (m_DMAF3_DRVRegister & 0x0F)
            {
                case 1:
                    nDrive = 0;
                    break;
                case 2:
                    nDrive = 1;
                    break;
                case 4:
                    nDrive = 2;
                    break;
                case 8:
                    nDrive = 3;
                    break;
            }

            //string strActivityMessage = string.Format ("Reading Address 0x%04X", m);
            //LogFloppyActivity (nDrive, strActivityMessage);

            switch ((DMAF3_OffsetRegisters)nWhichRegister)
            {
                case DMAF3_OffsetRegisters.DMAF3_DATAREG_OFFSET:
                    if (m_nFDCReading)
                    {
                        m_DMAF3_DATARegister = m_caReadBuffer[m_nFDCReadPtr++];
                        if (m_nFDCReadPtr == m_nBytesToTransfer)
                        {
                            m_DMAF3_STATRegister &= (byte)(~((byte)((byte)DMAF3_StatusLines.DMAF3_DRQ | (byte)DMAF3_StatusLines.DMAF3_BUSY)) & 0xFF);
                            m_DMAF3_DRVRegister  &= (byte)(~((byte)DMAF3_ContollerLines.DRV_DRQ) & 0xFF);            // turn off high order bit in drive status register

                            m_nFDCReading = false;

                            //Program.pStaticFloppyActivityLight[nDrive]->SetBitmap (Program.m_hGreyDot);
                            ClearInterrupt((int)DeviceInterruptMask.DEVICE_INT_MASK_FDC);
                        } 
                        // --------------------
                        else
                        {
                            m_DMAF3_STATRegister |= (byte)(DMAF3_ContollerLines.DRV_DRQ);
                            m_DMAF3_DRVRegister |= (byte)(DMAF3_ContollerLines.DRV_DRQ);

                            if (_bInterruptEnabled)
                            {
                                StartDMAF3InterruptDelayTimer(m_nInterruptDelay, (int)DeviceInterruptMask.DEVICE_INT_MASK_FDC);
                                if (Program._cpu != null)
                                {
                                    if ((Program._cpu.InWait || Program._cpu.InSync) && Program._cpuThread.ThreadState == ThreadState.Suspended)
                                    {
                                        try
                                        {
                                            Program._cpuThread.Resume();
                                        }
                                        catch (ThreadStateException e)
                                        {
                                            // do nothing if thread is not suspended
                                        }
                                    }
                                }
                            }
                        } 
                        // --------------------
                        d = m_DMAF3_DATARegister;

                        // if the dma is active - put the byte in memory also and decrement the dma counter
                    }
		            else
                    {
                        d = m_DMAF3_DATARegister;

                        // if the dma is active - put the byte in memory also and decrement the dma counter
                    }
                    break;

                case DMAF3_OffsetRegisters.DMAF3_STATREG_OFFSET:

                    DMAF3_AT_DTBRegister &= (byte)(~0x04 & 0xFF);                          // clear interrupting bit to VIA

                    if (Program.DriveOpen[nDrive] == true)               // see if current drive is READY
                        m_DMAF3_STATRegister |= (byte)DMAF3_StatusLines.DMAF3_NOTREADY;
                    else
                        m_DMAF3_STATRegister &= (byte)(~((byte)DMAF3_StatusLines.DMAF3_NOTREADY) & 0xFF);

                    if (Program.WriteProtected[nDrive] == true)          // see if write protected
                        m_DMAF3_STATRegister |= (byte)DMAF3_StatusLines.DMAF3_WRTPROTECT;
                    else
                        m_DMAF3_STATRegister &= (byte)(~((byte)DMAF3_StatusLines.DMAF3_WRTPROTECT) & 0xFF);

		            if (!m_nFDCReading && !m_nFDCWriting)           // turn off BUSY if not read/writing
                        m_DMAF3_STATRegister &= (byte)(~((byte)DMAF3_StatusLines.DMAF3_BUSY) & 0xFF);
                    else
                    {
                        if (++m_nStatusReads > (m_nBytesToTransfer * 2))
                        {
                            m_DMAF3_STATRegister &= (byte)(~((byte)DMAF3_StatusLines.DMAF3_BUSY) & 0xFF);     // clear BUSY if data not read
                            //Program.pStaticFloppyActivityLight[nDrive]->SetBitmap (Program.m_hGreyDot);
                        }
                    }
                    ClearInterrupt((int)DeviceInterruptMask.DEVICE_INT_MASK_FDC);
                    d = m_DMAF3_STATRegister;                      // get controller status
                    break;

                case DMAF3_OffsetRegisters.DMAF3_DRVREG_OFFSET:
                    d = (byte)(~((byte)m_DMAF3_DRVRegister | 0x40) & 0xFF);
                    break;

                case DMAF3_OffsetRegisters.DMAF3_TRKREG_OFFSET:
                    d = m_DMAF3_TRKRegister;                      // get Track Register
                    break;

                case DMAF3_OffsetRegisters.DMAF3_SECREG_OFFSET:
                    d = m_DMAF3_SECRegister;                      // get Track Register
                    break;

                case DMAF3_OffsetRegisters.DMAF3_HLD_TOGGLE_OFFSET:       // 0x50 - head load toggle
                    d = DMAF3_HLD_TOGGLERegister;
                    break;

            }

            //strActivityMessage.Format (" value: 0x%02X\n", d);
            //LogFloppyActivity (nDrive, strActivityMessage, false);

            if (Program.DMAF3AccessLogging)
            {
                string strActivityMessage = string.Format("Reading Address 0x{0} value: 0x{1}\n", m.ToString("X4"), d.ToString("X2"));
                LogActivity(nDrive, strActivityMessage, true);
            }

            return (d);
        }

        byte ReadDMA (ushort m)
        {
            byte d = 0xFF;
            int nWhichRegister = m - m_sBaseAddress;

            //CString strActivityMessage;

            //strActivityMessage.Format ("Reading DMA Register Address 0x%04X", m);
            //LogFloppyActivity (-1, strActivityMessage);

            switch ((DMAF3_OffsetRegisters)nWhichRegister)
            {
                case DMAF3_OffsetRegisters.DMAF3_LATCH_OFFSET:
                    d = m_DMAF3_LATCHRegister;                    // the Enable interrupt bit is in here somewhere
                    break;

                //*
                //*   DMAF3 6844 DMA controller definitions
                //*

                case DMAF3_OffsetRegisters.DMAF3_DMA_ADDRESS_OFFSET + 0:                  // 0xF000
                    d = m_DMAF3_DMA_ADDRESSRegister[0].hibyte;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_DMA_ADDRESS_OFFSET + 1:                  // 0xF001
                    d = m_DMAF3_DMA_ADDRESSRegister[0].lobyte;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_DMA_ADDRESS_OFFSET + 4:                  // 0xF004
                    d = m_DMAF3_DMA_ADDRESSRegister[1].hibyte;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_DMA_ADDRESS_OFFSET + 5:                  // 0xF005
                    d = m_DMAF3_DMA_ADDRESSRegister[1].lobyte;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_DMA_ADDRESS_OFFSET + 8:                  // 0xF008
                    d = m_DMAF3_DMA_ADDRESSRegister[2].hibyte;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_DMA_ADDRESS_OFFSET + 9:                  // 0xF009
                    d = m_DMAF3_DMA_ADDRESSRegister[2].lobyte;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_DMA_ADDRESS_OFFSET + 12:                 // 0xF00C
                    d = m_DMAF3_DMA_ADDRESSRegister[3].hibyte;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_DMA_ADDRESS_OFFSET + 13:                 // 0xF00D
                    d = m_DMAF3_DMA_ADDRESSRegister[3].lobyte;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_DMA_DMCNT_OFFSET + 0:                    // 0xF002
                    d = m_DMAF3_DMA_DMCNTRegister[0].hibyte;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_DMA_DMCNT_OFFSET + 1:                    // 0xF003
                    d = m_DMAF3_DMA_DMCNTRegister[0].lobyte;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_DMA_DMCNT_OFFSET + 4:                    // 0xF006
                    d = m_DMAF3_DMA_DMCNTRegister[1].hibyte;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_DMA_DMCNT_OFFSET + 5:                    // 0xF007
                    d = m_DMAF3_DMA_DMCNTRegister[1].lobyte;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_DMA_DMCNT_OFFSET + 8:                    // 0xF00A
                    d = m_DMAF3_DMA_DMCNTRegister[2].hibyte;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_DMA_DMCNT_OFFSET + 9:                    // 0xF00B
                    d = m_DMAF3_DMA_DMCNTRegister[2].lobyte;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_DMA_DMCNT_OFFSET + 12:                   // 0xF00E
                    d = m_DMAF3_DMA_DMCNTRegister[3].hibyte;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_DMA_DMCNT_OFFSET + 13:                   // 0xF00F
                    d = m_DMAF3_DMA_DMCNTRegister[3].lobyte;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_DMA_CHANNEL_OFFSET:                      // 0xF010
                    d = m_DMAF3_DMA_CHANNELRegister[0];

                    // make sure the interrupt is already set by the timer before we set the high order bit of the DMA status register

                    if (
                        ((Program._cpu.InterruptRegister & m_nInterruptMask)                             == m_nInterruptMask) && 
                        ((m_DMAF3_DMA_INTERRUPTRegister     & 0x01)                                         == 0x01) &&
                        ((m_nBoardInterruptRegister         & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA1) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA1)
                       )
                        d |= 0x80;
                    m_DMAF3_DMA_CHANNELRegister[0] = (byte)(m_DMAF3_DMA_CHANNELRegister[0] & 0x7F);
                    m_DMAF3_DMA_INTERRUPTRegister  = (byte)(m_DMAF3_DMA_INTERRUPTRegister & 0x7F);
                    ClearInterrupt((int)DeviceInterruptMask.DEVICE_INT_MASK_DMA1);
                    break;

                case DMAF3_OffsetRegisters.DMAF3_DMA_CHANNEL_OFFSET + 1:                  // 0xF011
                    d = m_DMAF3_DMA_CHANNELRegister[1];

                    // make sure the interrupt is already set by the timer before we set the high order bit of the DMA status register

                    if (
                        ((Program._cpu.InterruptRegister & m_nInterruptMask) == m_nInterruptMask) && 
                        ((m_DMAF3_DMA_INTERRUPTRegister     & 0x02)             == 0x02) &&
                        ((m_nBoardInterruptRegister         & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA2) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA2)
                       )
                        d |= 0x80;
                    m_DMAF3_DMA_CHANNELRegister[1] = (byte)(m_DMAF3_DMA_CHANNELRegister[1] & 0x7F);
                    m_DMAF3_DMA_INTERRUPTRegister = (byte)(m_DMAF3_DMA_INTERRUPTRegister & 0x7F);
                    ClearInterrupt((int)DeviceInterruptMask.DEVICE_INT_MASK_DMA2);
                    break;

                case DMAF3_OffsetRegisters.DMAF3_DMA_CHANNEL_OFFSET + 2:                  // 0xF012
                    d = m_DMAF3_DMA_CHANNELRegister[2];

                    // make sure the interrupt is already set by the timer before we set the high order bit of the DMA status register

                    if (
                        ((Program._cpu.InterruptRegister & m_nInterruptMask) == m_nInterruptMask) && 
                        ((m_DMAF3_DMA_INTERRUPTRegister     & 0x04)             == 0x04) &&
                        ((m_nBoardInterruptRegister         & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA3) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA3)
                       )
                        d |= 0x80;
                    m_DMAF3_DMA_CHANNELRegister[2] = (byte)(m_DMAF3_DMA_CHANNELRegister[2] & 0x7F);
                    m_DMAF3_DMA_INTERRUPTRegister = (byte)(m_DMAF3_DMA_INTERRUPTRegister & 0x7F);
                    ClearInterrupt((int)DeviceInterruptMask.DEVICE_INT_MASK_DMA3);
                    break;

                case DMAF3_OffsetRegisters.DMAF3_DMA_CHANNEL_OFFSET + 3:                  // 0xF013
                    d = m_DMAF3_DMA_CHANNELRegister[3];

                    // make sure the interrupt is already set by the timer before we set the high order bit of the DMA status register

                    if (
                        ((Program._cpu.InterruptRegister & m_nInterruptMask) == m_nInterruptMask) && 
                        ((m_DMAF3_DMA_INTERRUPTRegister     & 0x08)             == 0x08) &&
                        ((m_nBoardInterruptRegister         & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA4) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA4)
                       )
                        d |= 0x80;
                    m_DMAF3_DMA_CHANNELRegister[3] = (byte)(m_DMAF3_DMA_CHANNELRegister[3] & 0x7F);
                    m_DMAF3_DMA_INTERRUPTRegister = (byte)(m_DMAF3_DMA_INTERRUPTRegister & 0x7F);
                    ClearInterrupt((int)DeviceInterruptMask.DEVICE_INT_MASK_DMA4);
                    break;

                case DMAF3_OffsetRegisters.DMAF3_DMA_PRIORITY_OFFSET:                     // 0xF014
                    d = m_DMAF3_DMA_PRIORITYRegister;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_DMA_INTERRUPT_OFFSET:                    // 0xF015
                    d = m_DMAF3_DMA_INTERRUPTRegister;
                    if (
                        ((Program._cpu.InterruptRegister & m_nInterruptMask) == m_nInterruptMask) && 
                        (
                            (((m_DMAF3_DMA_INTERRUPTRegister & 0x01) == 0x01) && ((m_nBoardInterruptRegister & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA1) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA1)) ||
                            (((m_DMAF3_DMA_INTERRUPTRegister & 0x02) == 0x02) && ((m_nBoardInterruptRegister & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA2) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA2)) ||
                            (((m_DMAF3_DMA_INTERRUPTRegister & 0x04) == 0x04) && ((m_nBoardInterruptRegister & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA3) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA3)) ||
                            (((m_DMAF3_DMA_INTERRUPTRegister & 0x08) == 0x08) && ((m_nBoardInterruptRegister & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA4) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA4))
                        )
                       )
                        d |= 0x80;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_DMA_CHAIN_OFFSET:                        // 0xF016
                    d = m_DMAF3_DMA_CHAINRegister;
                    break;
            }

            //strActivityMessage.Format (" value 0x%02X\n", d);
            //LogFloppyActivity (-1, strActivityMessage, false);

            if (Program.DMAF3AccessLogging)
            {
                string strActivityMessage = string.Format("Reading DMA Register Address 0x{0} value 0x{1}\n", m.ToString("X4"), d.ToString("X2"));
                LogActivity(-1, strActivityMessage, true);
            }

            return (d);
        }

        byte ReadWD1000 (ushort m)
        {
            byte d = 0xFF;
            int nWhichRegister = m - m_sBaseAddress;
            int activechannel = -1;

            switch (m_DMAF3_DMA_PRIORITYRegister & 0x0F)
            {
                case 1:
                    activechannel = 0;      // Floppy
                    break;
                case 2:
                    activechannel = 1;      // WD1000
                    break;
                case 4:
                    activechannel = 2;
                    //AfxMessageBox ("DMA channel set to 2 - not supported");
                    break;
                case 8:
                    activechannel = 3;
                    //AfxMessageBox ("DMA channel set to 3 - not supported");
                    break;
            }

            byte   latch     = m_DMAF3_LATCHRegister;
            ushort  addr      = 0;
            ushort  cnt       = 0;
            byte   priority  = m_DMAF3_DMA_PRIORITYRegister;
            byte   interrupt = m_DMAF3_DMA_INTERRUPTRegister;
            byte   chain     = m_DMAF3_DMA_CHAINRegister;

            if (activechannel == 1)
            {
                addr      = (ushort)(m_DMAF3_DMA_ADDRESSRegister[activechannel].hibyte * 256 + m_DMAF3_DMA_ADDRESSRegister[activechannel].lobyte);
                cnt       = (ushort)(m_DMAF3_DMA_DMCNTRegister[activechannel].hibyte   * 256 + m_DMAF3_DMA_DMCNTRegister[activechannel].lobyte);
            }

            int nDrive    = (DMAF3_WDC_SDHRegister >> 3) & 0x03;

            //CString strActivityMessage;
            //strActivityMessage.Format ("Reading Address 0x%04X", m);
            //LogFloppyActivity (nDrive, strActivityMessage);

            switch ((DMAF3_OffsetRegisters)nWhichRegister)
            {
                // Winchester WD1000 5" controller

                case DMAF3_OffsetRegisters.DMAF3_WDC_DATA_OFFSET:         // 0x30 - WDC data register
                    d = DMAF3_WDC_DATARegister;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_WDC_ERROR_OFFSET:        // 0x31 - WDC error register
                                                    //    wd_error   equ     wd1000+1       error register (read only)
                                                    //    *                                 bit 7 bad block detect
                                                    //    *                                 bit 6 CRC error, data field
                                                    //    *                                 bit 5 CRC error, ID field
                                                    //    *                                 bit 4 ID not found
                                                    //    *                                 bit 3 unused
                                                    //    *                                 bit 2 Aborted Command
                                                    //    *                                 bit 1 TR000 (track zero) error
                                                    //    *                                 bit 0 DAM not found
                    d = DMAF3_WDC_ERRORRegister;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_WDC_SECCNT_OFFSET:       // 0x32 - sector count (during format)
                    d = DMAF3_WDC_SECCNTRegister;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_WDC_SECNUM_OFFSET:       // 0x33 - WDC sector number
                    d = DMAF3_WDC_SECNUMRegister;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_WDC_CYLLO_OFFSET:        // 0x34 - WDC cylinder (low part)
                    d = DMAF3_WDC_CYLLORegister;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_WDC_CYLHI_OFFSET:        // 0x35 - WDC cylinder (high part)
                    d = DMAF3_WDC_CYLHIRegister;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_WDC_SDH_OFFSET:          // 0x36 - WDC size/drive/head
                    d = DMAF3_WDC_SDHRegister;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_WDC_STATUS_OFFSET:       // 0x37 - WDC status register
                    d = DMAF3_WDC_STATUSRegister;
                    ClearInterrupt((int)DeviceInterruptMask.DEVICE_INT_MASK_WDC);
                    break;

                case DMAF3_OffsetRegisters.DMAF_WD1000_RES_OFFSET:        // 0x51 - winchester software reset
                    d = DMAF_WD1000_RESRegister;
                    break;

            }

            //strActivityMessage.Format (" value: 0x%02X\n", d);
            //LogFloppyActivity (nDrive, strActivityMessage, false);

            if (Program.DMAF3AccessLogging)
            {
                string strActivityMessage = string.Format("Reading from WD1000 Address 0x{0}  value: 0x{1}\n", m.ToString("X4"), d.ToString("X2"));
                LogActivity(nDrive, strActivityMessage, true);
            }

            return (d);
        }

        byte ReadTape (ushort m)
        {
            byte d = 0xFF;
            int nWhichRegister = m - m_sBaseAddress;
            switch ((DMAF3_OffsetRegisters)nWhichRegister)
            {
                case DMAF3_OffsetRegisters.DMAF3_AT_DTB_OFFSET:           // 0x40 - B-Side Data Register
                    d = DMAF3_AT_DTBRegister;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_AT_DTA_OFFSET:           // 0x41 - A-Side Data Register
                    d = DMAF3_AT_DTARegister;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_AT_DRB_OFFSET:           // 0x42 - B-Side Direction Register
                    d = DMAF3_AT_DRBRegister;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_AT_DRA_OFFSET:           // 0x43 - A-Side Direction Register
                    d = DMAF3_AT_DRARegister;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_AT_T1C_OFFSET:           // 0x44 - Timer 1 Counter Register
                    d = m_DMAF3_AT_TCRegister[0].lobyte;
                    break;
                case DMAF3_OffsetRegisters.DMAF3_AT_T1C_OFFSET + 1:       // 0x45 - Timer 1 Counter Register
                    d = m_DMAF3_AT_TCRegister[0].hibyte;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_AT_T1L_OFFSET:           // 0x46 - Timer 1 Latches
                    d = m_DMAF3_AT_TLRegister.lobyte;
                    break;
                case DMAF3_OffsetRegisters.DMAF3_AT_T1L_OFFSET + 1:       // 0x47 - Timer 1 Latches
                    d = m_DMAF3_AT_TLRegister.hibyte;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_AT_T2C_OFFSET:           // 0x48 - Timer 2 Counter Register
                    d = m_DMAF3_AT_TCRegister[1].lobyte;
                    break;
                case DMAF3_OffsetRegisters.DMAF3_AT_T2C_OFFSET + 1:       // 0x49 - Timer 2 Counter Register
                    d = m_DMAF3_AT_TCRegister[1].hibyte;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_AT_VSR_OFFSET:           // 0x4A - Shift Register
                    d = DMAF3_AT_VSRRegister;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_AT_ACR_OFFSET:           // 0x4B - Auxillary Control Register
                    d = DMAF3_AT_ACRRegister;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_AT_PCR_OFFSET:           // 0x4C - Peripheral Control Register
                    d = DMAF3_AT_PCRRegister;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_AT_IFR_OFFSET:           // 0x4D - Interrupt Flag Register
                    d = DMAF3_AT_IFRRegister;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_AT_IER_OFFSET:           // 0x4E - Interrupt Enable Register
                    d = DMAF3_AT_IERRegister;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_AT_DXA_OFFSET:           // 0x4F - A-Side Data Register
                    d = DMAF3_AT_DXARegister;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_AT_ATR_OFFSET:           // 0x52 - archive RESET
                    d = DMAF3_AT_ATRRegister;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_AT_DMC_OFFSET:           // 0x53 - archive DMA clear
                    d = DMAF3_AT_DMCRegister;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_AT_DMP_OFFSET:           // 0x60 - archive DMA preset
                    d = DMAF3_AT_DMPRegister;
                    break;

            }
            return (d);
        }

        byte ReadCDS (ushort m)
        {
            byte d = 0xFF;
            int nWhichRegister = m - m_sBaseAddress;

            switch ((DMAF3_OffsetRegisters)nWhichRegister)
            {
                case DMAF3_OffsetRegisters.DMAF3_CDSCMD0_OFFSET:          // 0x0100      
                    d = DMAF3_CDSCMD0Register;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_CDSFLG0_OFFSET:          // 0x0103 - marksman flag (1xxx xxxx = yes)
                    d = DMAF3_CDSFLG0Register;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_CDSCMD1_OFFSET:          // 0x0300      
                    d = DMAF3_CDSCMD1Register;
                    break;

                case DMAF3_OffsetRegisters.DMAF3_CDSFLG1_OFFSET:          // 0x0303 - marksman flag (1xxx xxxx = yes)
                    d = DMAF3_CDSFLG1Register;
                    break;
            }
            return (d);
        }

        public override byte Read (ushort m)
        {
            DMAF3_OffsetRegisters nWhichRegister = (DMAF3_OffsetRegisters)(m - m_sBaseAddress);

            if ((nWhichRegister >= DMAF3_OffsetRegisters.DMAF3_STATREG_OFFSET && nWhichRegister <= DMAF3_OffsetRegisters.DMAF3_DRVREG_OFFSET) || nWhichRegister == DMAF3_OffsetRegisters.DMAF3_HLD_TOGGLE_OFFSET)
                return ReadFloppy (m);
            else if ((nWhichRegister >= DMAF3_OffsetRegisters.DMAF3_DMA_ADDRESS_OFFSET && nWhichRegister <= DMAF3_OffsetRegisters.DMAF3_DMA_CHAIN_OFFSET) || nWhichRegister == DMAF3_OffsetRegisters.DMAF3_LATCH_OFFSET)
                return ReadDMA (m);
            else if ((nWhichRegister >= DMAF3_OffsetRegisters.DMAF3_WDC_DATA_OFFSET && nWhichRegister <= DMAF3_OffsetRegisters.DMAF3_WDC_CMD_OFFSET) || nWhichRegister == DMAF3_OffsetRegisters.DMAF_WD1000_RES_OFFSET)
                return ReadWD1000 (m);
            else if ((nWhichRegister >= DMAF3_OffsetRegisters.DMAF3_AT_DTB_OFFSET && nWhichRegister <= DMAF3_OffsetRegisters.DMAF3_AT_DXA_OFFSET) || nWhichRegister == DMAF3_OffsetRegisters.DMAF3_AT_ATR_OFFSET || nWhichRegister == DMAF3_OffsetRegisters.DMAF3_AT_DMC_OFFSET || nWhichRegister == DMAF3_OffsetRegisters.DMAF3_AT_DMP_OFFSET)
                return ReadTape (m);
            else if (nWhichRegister == DMAF3_OffsetRegisters.DMAF3_CDSCMD0_OFFSET || nWhichRegister == DMAF3_OffsetRegisters.DMAF3_CDSFLG0_OFFSET || nWhichRegister == DMAF3_OffsetRegisters.DMAF3_CDSCMD1_OFFSET || nWhichRegister == DMAF3_OffsetRegisters.DMAF3_CDSFLG1_OFFSET)
                return ReadCDS (m);
            else
                return Program._cpu.ReadFromFirst64K(m);   // memory read
        }

        public override void Init(int nWhichController, byte[] sMemoryBase, ushort sBaseAddress, int nRow, bool bInterruptEnabled)
        {
            m_nRow = nRow;
            base.Init(nWhichController, sMemoryBase, sBaseAddress, nRow, bInterruptEnabled);

            m_DMAF3_DRVRegister              = 0;

            m_DMAF3_STATRegister             = 0;
            m_DMAF3_CMDRegister              = 0;
            m_DMAF3_TRKRegister              = 0;
            m_DMAF3_SECRegister              = 0;
            m_DMAF3_DATARegister             = 0;

            m_DMAF3_LATCHRegister            = 0;

            for (int i = 0; i < 4; i++)
            {
                m_DMAF3_DMA_ADDRESSRegister[i] = new DMAF3_DMA_ADDRESS_REGISTER();
                m_DMAF3_DMA_ADDRESSRegister[i].hibyte = 0x00;
                m_DMAF3_DMA_ADDRESSRegister[i].lobyte = 0x00;

                m_DMAF3_DMA_DMCNTRegister[i] = new DMAF3_DMA_BYTECNT_REGISTER();
                m_DMAF3_DMA_DMCNTRegister[i].hibyte = 0x00;
                m_DMAF3_DMA_DMCNTRegister[i].lobyte = 0x00;
            }

            for (int i = 0; i < 4; i++)
            {
                m_DMAF3_DMA_CHANNELRegister[i] = 0x00;
            }

            m_DMAF3_DMA_PRIORITYRegister       = 0x00;
            m_DMAF3_DMA_INTERRUPTRegister      = 0x00;
            m_DMAF3_DMA_CHAINRegister          = 0x00;

            DMAF3_CDSCMD0Register  = 0x00;
            DMAF3_CDSFLG0Register  = 0x00;
            DMAF3_CDSCMD1Register  = 0x00;
            DMAF3_CDSFLG1Register  = 0x00;

            m_nInterruptDelay            = Program.GetConfigurationAttribute("Global/WinchesterInterruptDelay", "value", 10);
            m_nAllowMultiSectorTransfers = Program.GetConfigurationAttribute("Global/AllowMultipleSector",      "value",  0) == 0 ? false : true;

            for (int nDrive = 0; nDrive < 4; nDrive++)
            {
                if (m_cWinchesterDrivePathName[nDrive] != null && m_cWinchesterDrivePathName[nDrive].Length > 0)
                {
                    if (m_fpWinchester[nDrive] == null)
                    {
                        m_fpWinchester[nDrive] = File.Open(m_cWinchesterDrivePathName[nDrive], FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                    }
                }
            }

            StartDMAF3Timer(m_nDMAF3Rate);
            m_nDMAF3Running = true;
        }
    }
}
