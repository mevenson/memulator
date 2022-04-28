using System;
using System.Collections;
using System.Text;

using System.IO;
using System.Windows.Forms;
using System.Xml;
using System.Collections.Generic;

namespace Memulator
{
    public enum fileformat
    {
        fileformat_UNKNOWN = -1,
        fileformat_OS9,         // mode where track 0 has same number of sectors as the rest of the disk.
        fileformat_OS9_IMA,     // special mode where track zero (both sides) is always only 10 sectors     (Not Yet Implemented)
        fileformat_FLEX,        // mode where track 0 has same number of sectors as the rest of the disk.
        fileformat_FLEX_IMA,    // special mode where track zero (both sides) is always only 10 sectors     (Not Yet Implemented)
        fileformat_UniFLEX,
        fileformat_FLEX_IDE,
        fileformat_MINIX_68K,
        fileformat_MINIX_IBM,
        fileformat_IMD,
        fileformat_miniFLEX
    }

    [Serializable]
    public class NodeAttributes
    {
        // these are used by all formats

        public int byteCount;
        public int fileAttributes;

        // this is for OS-9

        public int fileDesriptorSector;

        // these are for UniFLEX

        public int fdnIndex;
        public int blk;

        // these are used by minix

        public int iNode;
        public int mode;
    }

    // Path descriptor oprions (part of the OS-9 SIR)

    #region OS9 disk structure classes

    public class OS9_DD_PATH_OPTS
    {
        public byte [] pd_dtp   = new byte[1];      // 0x3F     IT.DTP RMB 1 DEVICE TYPE(0=SCF 1=RBF 2=PIPE 3=SBF)
        public byte [] pd_drv   = new byte[1];      // 0x40     IT.DRV RMB 1 DRIVE NUMBER
        public byte [] pd_stp   = new byte[1];      // 0x41     IT.STP RMB 1 STEP RATE (see table below)
                                                    //                          Step Code   FD1771     FD179X Family
                                                    //                                      5"   8"   5"   8"
                                                    //                              0       40ms 20ms 30ms 15ms
                                                    //                              1       20ms 10ms 20ms 10ms
                                                    //                              2       12ms  6ms 12ms  6ms
                                                    //                              3       12ms  6ms  6ms  3ms

        public byte [] pd_typ   = new byte[1];      // 0x42     IT.TYP RMB 1 DEVICE TYPE(See RBFMAN path descriptor)
                                                    //                          bit 0   0 = 5"
                                                    //                                  1 = 8"
                                                    //                          bit 6   0 = Standard OS9 format
                                                    //                                  1 = Non-standard format
                                                    //                          bit 7   0 = Floppy disk
                                                    //                                  1 = Hard disk

        public byte [] pd_dns   = new byte[1];      // 0x43     IT.DNS RMB 1 MEDIA DENSITY(0 - SINGLE, 1-DOUBLE)
                                                    //                          bit 0   0 = Single density (FM)
                                                    //                                  1 = Double density (MFM)
                                                    //                          bit 1   0 = Single track density (5" 48 TPI)
                                                    //                                  1 = Double track density (5" 96 TPI)

        public byte [] pd_cyl   = new byte[2];      // 0x44     IT.CYL RMB 2 NUMBER OF CYLINDERS(TRACKS)
        public byte [] pd_sid   = new byte[1];      // 0x46     IT.SID RMB 1 NUMBER OF SURFACES(SIDES)
        public byte [] pd_vfy   = new byte[1];      // 0x47     IT.VFY RMB 1 0 = VERIFY DISK WRITES
        public byte [] pd_sct   = new byte[2];      // 0x48     IT.SCT RMB 2 Default Sectors/Track
        public byte [] pd_t0s   = new byte[2];      // 0x4A     IT.T0S RMB 2 Default Sectors/Track (Track 0)
        public byte [] pd_ilv   = new byte[1];      // 0x4C     IT.ILV RMB 1 SECTOR INTERLEAVE FACTOR
        public byte [] pd_sas   = new byte[1];      // 0x4D     IT.SAS RMB 1 SEGMENT ALLOCATION SIZE
        public byte [] pd_tfm   = new byte[1];      // 0x4E
        public byte [] pd_exten = new byte[2];      // 0x4F
        public byte [] pd_stoff = new byte[1];      // 0x51
        public byte [] pd_att   = new byte[1];      // 0x52     File attributes (D S PE PW PR E W R)
        public byte [] pd_fd    = new byte[3];      // 0x53     File descriptor PSN (physical sector #)
        public byte [] pd_dfd   = new byte[3];      // 0x56     Directory file descriptor PSN
        public byte [] pd_dcp   = new byte[4];      // 0x59     File’s directory entry pointer
        public byte [] pd_dvt   = new byte[2];      // 0x5D     Address of device table entry
    }

    public class OS9_ID_SECTOR
    {
        //                                                                                  EmuOS9Boot.dsk
        public byte[] cTOT = new byte[3];      // Total Number of sector on media           0x00 00 16 66 
        public byte[] cTKS = new byte[1];      // Number of sector per track                0x03 24 
        public byte[] cMAP = new byte[2];      // Number of bytes in allocation map         0x04 01 CD 
        public byte[] cBIT = new byte[2];      // Number of sectors per cluster             0x06 00 02  
        public byte[] cDIR = new byte[3];      // Starting sector of root directory         0x08 00 00 03 
        public byte[] cOWN = new byte[2];      // Owners user number                        0x0B 00 00 
        public byte[] cATT = new byte[1];      // Disk attributes                           0x0D FF 
        public byte[] cDSK = new byte[2];      // Disk Identification                       0x0E 00 00  
        public byte[] cFMT = new byte[1];      // Disk Format: density, number of sides     0x10 03 
        public byte[] cSPT = new byte[2];      // Number of sectors per track               0x11 00 24 
        public byte[] cRES = new byte[2];      // Reserved for future use                   0x13 00 00 
        public byte[] cBT  = new byte[3];      // Starting sector of bootstrap file         0x15 00 08 6D  
        public byte[] cBSZ = new byte[2];      // Size of bootstrap file (in bytes)         0x18 1F F3 
        public byte[] cDAT = new byte[5];      // Time of creation Y:M:D:H:M                0x1A 62 0B 17 0A 28
        public byte[] cNAM = new byte[32];     // Volume name (last char has sign bit set)  0x1F 45 6D 75 4F 73 39 44 69 73 EB 00 00 00 00 00 00 
                                               //                                           0x2F 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
        // these are new 

        //public byte[] cDD_OPT       = new byte[32];                                   //  0x3F 32 bytes of DD_OPT Path descriptor options
        public OS9_DD_PATH_OPTS cOPTS = new OS9_DD_PATH_OPTS();                         //  All zeros 0x3F through 0x5E (see above class)

        // these are used by OS9/68K - on EmuOS9Boot.Dsk these are all zeros

        public byte[] cDD_RES       = new byte[1];                                      //   $5F 1 Reserved
        public byte[] cDD_SYNC      = new byte[4];                                      //   $60 4 DD_SYNC Media integrity code
        public byte[] cDD_MapLSN    = new byte[4];                                      //   $64 4 DD_MapLSN Bitmap starting sector number(0=LSN 1)
        public byte[] cDD_LSNSize   = new byte[2];                                      //   $68 2 DD_LSNSize Media logical sector size(0=256)
        public byte[] cDD_VersID    = new byte[2];                                      //   $6A 2 DD_VersID Sector 0 Version ID

    };

    class OS9_DIR_ENTRY
    {
        public byte[] cNAM = new byte[29];
        public byte cSCT;       // logical sector number of File Descriptor
        public byte cSCT1;
        public byte cSCT2;
    };

    class OS9DirEntry
    {
        public string attributes;
        public string strDirectoryName;
        public int nFileDescriptorSector;       // This is actually the LSN of the file descriptor
        public int nAttributes;
    };

    class OS9_SEG_ENTRY
    {
        public int nSector;
        public int nSize;
    };

    public class OS9_FILE_DESCRIPTOR
    {
        public byte cATT;       // File Attributes                              0

        public byte cOWN;       // File Owner's User ID                         1
        public byte cOWN1;      //                                              2

        public byte cDAT;       // Date and Time Last Modified Y:M:D:H:M        3
        public byte cDAT1;      //                                              4
        public byte cDAT2;      //                                              5
        public byte cDAT3;      //                                              6
        public byte cDAT4;      //                                              7

        public byte cLNK;       // Link Count                                   8

        public byte cSIZ;       // File Size                                    9
        public byte cSIZ1;      //                                              A
        public byte cSIZ2;      //                                              B
        public byte cSIZ3;      //                                              C

        public byte cDCR;       // Date Created Y M D                           D
        public byte cDCR1;      //                                              E
        public byte cDCR2;      //                                              F

        public byte[] cSEG = new byte[240];                // segment list     10
        public ArrayList alSEGArray = new ArrayList();
    };

    public class OS9_BYTES_TO_WRITE
    {
        public int fileOffset;
        public List<byte> content = new List<byte>();
    }

    #endregion

    //#region UniFLEX disk structure classes

    //public class dir_entry
    //{
    //    public byte[] m_fdn_number = new byte[2];
    //    public byte[] m_fdn_name = new byte[14];

    //    public int nfdnNumber;
    //}

    //public class blockList
    //{
    //    public byte[] block = new byte[3];
    //}
    //public class fdn
    //{
    //    //unsigned char m_ffwdl[2]  ;// rmb 2 forward list link               0x00
    //    //unsigned char m_fstat     ;// rmb 1 * see below *                   0x02
    //    //unsigned char m_fdevic[2] ;// rmb 2 device where fdn resides        0x03
    //    //unsigned char m_fnumbr[2] ;// rmb 2 fdn number (device address)     0x05
    //    //unsigned char m_frefct    ;// rmb 1 reference count                 0x07
    //    //unsigned char m_fmode     ;// rmb 1 * see below *                   0x08
    //    //unsigned char m_facces    ;// rmb 1 * see below *                   0x09
    //    //unsigned char m_fdirlc    ;// rmb 1 directory entry count           0x0A
    //    //unsigned char m_fouid[2]  ;// rmb 2 owner's user id                 0x0B
    //    //unsigned char m_fsize[4]  ;// rmb 4 file size                       0x0D
    //    //unsigned char m_ffmap[48] ;// rmb MAPSIZ*DSKADS file map            0x10

    //    public byte[]       m_mode   = new byte[1];         //  file mode
    //    public byte[]       m_perms  = new byte[1];         //  file security
    //    public byte[]       m_links  = new byte[1];         //  number of links
    //    public byte[]       m_owner  = new byte[2];         //  file owner's ID
    //    public byte[]       m_size   = new byte[4];         //  file size
    //    public blockList[]  m_blks   = new blockList[13];   //  block list
    //    public byte[]       m_time   = new byte[4];         //  file time
    //    public byte[]       m_fd_pad = new byte[12];        //  padding
    //}

    //public class UniFLEX_SIR
    //{
    //    public byte[] m_supdt  = new byte[1];        //rmb 1       sir update flag                                         0x0200        -> 00 
    //    public byte[] m_swprot = new byte[1];        //rmb 1       mounted read only flag                                  0x0201        -> 00 
    //    public byte[] m_slkfr  = new byte[1];        //rmb 1       lock for free list manipulation                         0x0202        -> 00 
    //    public byte[] m_slkfdn = new byte[1];        //rmb 1       lock for fdn list manipulation                          0x0203        -> 00 
    //    public byte[] m_sintid = new byte[4];        //rmb 4       initializing system identifier                          0x0204        -> 00 
    //    public byte[] m_scrtim = new byte[4];        //rmb 4       creation time                                           0x0208        -> 11 44 F3 FC
    //    public byte[] m_sutime = new byte[4];        //rmb 4       date of last update                                     0x020C        -> 11 44 F1 51
    //    public byte[] m_sszfdn = new byte[2];        //rmb 2       size in blocks of fdn list                              0x0210        -> 00 4A          = 74
    //    public byte[] m_ssizfr = new byte[3];        //rmb 3       size in blocks of volume                                0x0212        -> 00 08 1F       = 2079
    //    public byte[] m_sfreec = new byte[3];        //rmb 3       total free blocks                                       0x0215        -> 00 04 9C       = 
    //    public byte[] m_sfdnc  = new byte[2];        //rmb 2       free fdn count                                          0x0218        -> 01 B0
    //    public byte[] m_sfname = new byte[14];       //rmb 14      file system name                                        0x021A        -> 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
    //    public byte[] m_spname = new byte[14];       //rmb 14      file system pack name                                   0x0228        -> 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
    //    public byte[] m_sfnumb = new byte[2];        //rmb 2       file system number                                      0x0236        -> 00 00
    //    public byte[] m_sflawc = new byte[2];        //rmb 2       flawed block count                                      0x0238        -> 00 00
    //    public byte[] m_sdenf  = new byte[1];        //rmb 1       density flag - 0=single                                 0x023A        -> 01
    //    public byte[] m_ssidf  = new byte[1];        //rmb 1       side flag - 0=single                                    0x023B        -> 01
    //    public byte[] m_sswpbg = new byte[3];        //rmb 3       swap starting block number                              0x023C        -> 00 08 20
    //    public byte[] m_sswpsz = new byte[2];        //rmb 2       swap block count                                        0x023F        -> 01 80
    //    public byte[] m_s64k   = new byte[1];        //rmb 1       non-zero if swap block count is multiple of 64K         0x0241        -> 00
    //    public byte[] m_swinc  = new byte[11];       //rmb 11      Winchester configuration info                           0x0242        -> 00 00 00 00 00 00 2A 00 99 00 9A
    //    public byte[] m_sspare = new byte[11];       //rmb 11      spare bytes - future use                                0x024D        -> 00 9B 00 9C 00 9D 00 9E 00 9F 00
    //    public byte[] m_snfdn  = new byte[1];        //rmb 1       number of in core fdns                                  0x0258        -> A0           *snfdn * 2 = 320
    //    public byte[] m_scfdn  = new byte[512];      //rmb CFDN*2  in core free fdns                                       0x0259        variable (*snfdn * 2)
    //    public byte[] m_snfree = new byte[1];        //rmb 1       number of in core free blocks                           0x03B9        -> 03
    //    public byte[] m_sfree  = new byte[16384];    //rmb         CDBLKS*DSKADS in core free blocks                       0x03BA        -> 

    //    public fdn[] m_fdn = new fdn[592];
    //}

    //#endregion

    #region FLEX disk structure classes

    // [Serializable()]
    public class RAW_SIR
    {
        public RAW_SIR()
        {
            Array.Clear(caVolumeLabel, 0, caVolumeLabel.Length);
            cVolumeNumberHi     = 0x00;
            cVolumeNumberLo     = 0x00;
            cFirstUserTrack     = 0x00;
            cFirstUserSector    = 0x00;
            cLastUserTrack      = 0x00;
            cLastUserSector     = 0x00;
            cTotalSectorsHi     = 0x00;
            cTotalSectorsLo     = 0x00;
            cMonth              = 0x00;
            cDay                = 0x00;
            cYear               = 0x00;
            cMaxTrack           = 0x00;
            cMaxSector          = 0x00;
        }

        public byte[] caVolumeLabel = new byte[VirtualFloppyManipulationRoutines.sizeofVolumeLabel];    // $50 - $5A
        public byte cVolumeNumberHi;                    // $5B
        public byte cVolumeNumberLo;                    // $5C
        public byte cFirstUserTrack;                    // $5D
        public byte cFirstUserSector;                   // $5E
        public byte cLastUserTrack;                     // $5F
        public byte cLastUserSector;                    // $60
        public byte cTotalSectorsHi;                    // $61
        public byte cTotalSectorsLo;                    // $62
        public byte cMonth;                             // $63
        public byte cDay;                               // $64
        public byte cYear;                              // $65
        public byte cMaxTrack;                          // $66
        public byte cMaxSector;                         // $67
    }

    public class FLEX_HIER_DIR_HEADER
    {
        public byte parentTrack = 0;
        public byte parentSector = 0;
        public byte[] name = new byte[8];
        public byte[] unused = new byte[2];
    }

    public class RAW_DIR_SECTOR
    {
        public FLEX_HIER_DIR_HEADER header;
        public DIR_ENTRY [] dirEntries = new DIR_ENTRY[10];
    }

    public class DIR_ENTRY
    {
        public DIR_ENTRY()
        {
            Array.Clear(caFileName, 0, caFileName.Length);              // 0x00 - 0x07
            Array.Clear(caFileExtension, 0, caFileExtension.Length);    // 0x08 - 0x0A

            cAttributes     = 0x00;                                     // 0x0B
            cUnused1        = 0x00;                                     // 0x0C
            cStartTrack     = 0x00;                                     // 0x0D
            cStartSector    = 0x00;                                     // 0x0E
            cEndTrack       = 0x00;                                     // 0x0F
            cEndSector      = 0x00;                                     // 0x10
            cTotalSectorsHi = 0x00;                                     // 0x11
            cTotalSectorsLo = 0x00;                                     // 0x12
            cRandomFileInd  = 0x00;                                     // 0x13
            cUnused2        = 0x00;                                     // 0x14
            cMonth          = 0x00;                                     // 0x15
            cDay            = 0x00;                                     // 0x16
            cYear           = 0x00;                                     // 0x17

            isHierDirectoryEntry = false;
        }

        public byte[] caFileName = new byte[8];
        public byte[] caFileExtension = new byte[3];
        public byte cAttributes;
        public byte cUnused1;
        public byte cStartTrack;
        public byte cStartSector;
        public byte cEndTrack;
        public byte cEndSector;
        public byte cTotalSectorsHi;
        public byte cTotalSectorsLo;
        public byte cRandomFileInd;
        public byte cUnused2;
        public byte cMonth;
        public byte cDay;
        public byte cYear;

        public bool isHierDirectoryEntry;
    }

    #endregion

    static public class XmlDocumentExtensions
    {
        static public void IterateThroughAllNodes(this XmlDocument doc, Action<XmlNode> elementVisitor)
        {
            if (doc != null && elementVisitor != null)
            {
                foreach (XmlNode node in doc.ChildNodes)
                {
                    doIterateNode(node, elementVisitor);
                }
            }
        }

        static private void doIterateNode(XmlNode node, Action<XmlNode> elementVisitor)
        {
            elementVisitor(node);

            foreach (XmlNode childNode in node.ChildNodes)
            {
                doIterateNode(childNode, elementVisitor);
            }
        }
    }

    public class VirtualFloppyManipulationRoutines
    {
        public int numberOfFileImported = 0;

        #region Properties and variables

        public MinixImage minixImage = new MinixImage(null, fileformat.fileformat_UNKNOWN);

        private bool m_nExpandTabs = true;
        public bool ExpandTabs
        {
            get { return m_nExpandTabs; }
            set { m_nExpandTabs = value; }
        }

        private bool m_nAddLinefeed = false;
        public bool AddLinefeed
        {
            get { return m_nAddLinefeed; }
            set { m_nAddLinefeed = value; }
        }

        private bool m_nCompactBinary = true;
        public bool CompactBinary
        {
            get { return m_nCompactBinary; }
            set { m_nCompactBinary = value; }
        }

        private bool m_nStripLinefeed = true;
        public bool StripLinefeed
        {
            get { return m_nStripLinefeed; }
            set { m_nStripLinefeed = value; }
        }

        private bool m_nCompressSpaces = true;
        public bool CompressSpaces
        {
            get { return m_nCompressSpaces; }
            set { m_nCompressSpaces = value; }
        }

        private bool m_nConvertLfOnly = true;
        public bool ConvertLfOnly
        {
            get { return m_nConvertLfOnly; }
            set { m_nConvertLfOnly = value; }
        }

        private bool m_nConvertLfOnlyToCrLf = false;
        public bool ConvertLfOnlyToCrLf
        {
            get { return m_nConvertLfOnlyToCrLf; }
            set { m_nConvertLfOnlyToCrLf = value; }
        }

        private bool m_nConvertLfOnlyToCr = true;

        public bool ConvertLfOnlyToCr
        {
            get { return m_nConvertLfOnlyToCr; }
            set { m_nConvertLfOnlyToCr = value; }
        }

        private long m_lPartitionBias = -1;
        private int m_nSectorBias = -1;

        private fileformat m_nCurrentFileFormat;

        public const int sizeofVolumeLabel = 11;
        public const int sizeofDirEntry = 24;
        public const int sizeofSystemInformationRecord = 24;

        // all of the variables that begin with os9 were added when support for handling OS9 images was added

        public string os9VolumeName             = "";
        public string os9CreationDate           = "";
        public int os9TotalDirectories          = 0;
        public int os9TotalFiles                = 0;
        public int os9TotalSectors              = 0;
        public int os9RemainingSectors          = 0;
        public int os9BytesInAllocationMap      = 0;
        public int os9UnusedAllocationBits      = 0;
        public int os9UsedAllocationBits        = 0;
        public int os9SectorsPerCluster         = 1;
        public int os9SystemRequiredSectors     = 0;
        public int os9StartingRootSector        = 0;
        public int os9DiskAttributes            = 0;

        public string uniFLEXVolumeName             = "";
        public string uniFLEXCreationDate           = "";
        public uint   uniFLEXTotalDirectories      = 0;
        public uint   uniFLEXTotalFiles            = 0;
        public uint   uniFLEXTotalSectors          = 0;
        public uint   uniFLEXRemainingSectors      = 0;
        public uint   uniFLEXBytesInAllocationMap  = 0;
        public uint   uniFLEXTotalFreeBlocks       = 0;
        public uint   uniFLEXUsedAllocationBits    = 0;
        public uint   uniFLEXSectorsPerCluster     = 1;
        public uint   uniFLEXSystemRequiredSectors = 0;
        public uint   uniFLEXStartingRootSector    = 0;
        public uint   uniFLEXDiskAttributes        = 0;

        /// <summary>
        /// allocationArray
        /// 
        ///     the bit mapped allocation array from the image expanded into a list that can be accessed with the LSN
        ///     to see if a cluster is used or not. True means it is in use - false means that it is available to be
        ///     allocated.
        ///     
        /// </summary>
        public List<bool> os9AllocationArray = new List<bool>();

        private long currentPosition = 0;
        public long CurrentPosition
        {
            get
            {
                currentPosition = currentlyOpenedImageFileStream.Position;
                return currentPosition;
            }
        }

        public int sectorSize = 256;
        public int nLSNBlockSize = 256;
        public int m_nTotalSectors = 0;

        public int SectorBias
        {
            get { return m_nSectorBias; }
            set { m_nSectorBias = value; }
        }

        public fileformat CurrentFileFormat
        {
            get { return m_nCurrentFileFormat; }
            set { m_nCurrentFileFormat = value; }
        }

        private long lCurrentDirOffset = 0L, lOffset;
        private DIR_ENTRY stDirEntry;
        private byte[] szTargetFileTitle = new byte[8];
        private byte[] szTargetFileExt = new byte[3];


        public long PartitionBias
        {
            get { return m_lPartitionBias; }
            set { m_lPartitionBias = value; }
        }

        public string currentlyOpenedImageFileName = "";
        public FileStream currentlyOpenedImageFileStream = null;

        bool logFloppyAccess = false;
        string os9FloppyWritesFile = "";

        #endregion

        public void ReloadOptions()
        {
            // reload options from config file in case they were saved (with Apply) in the Option Dialog and them the user pressed cancel.

            m_nExpandTabs       = Program.GetConfigurationAttribute("Global/FileMaintenance/FileExport/ExpandTabs", "enabled", "0") == "1" ? true : false;
            m_nAddLinefeed      = Program.GetConfigurationAttribute("Global/FileMaintenance/FileExport/AddLinefeed", "enabled", "0") == "1" ? true : false;
            m_nCompactBinary    = Program.GetConfigurationAttribute("Global/FileMaintenance/BinaryFile/CompactBinary", "enabled", "0") == "1" ? true : false;
            m_nStripLinefeed    = Program.GetConfigurationAttribute("Global/FileMaintenance/FileImport/StripLinefeed", "enabled", "0") == "1" ? true : false;
            m_nCompressSpaces   = Program.GetConfigurationAttribute("Global/FileMaintenance/FileImport/CompressSpaces", "enabled", "0") == "1" ? true : false;

            ConvertLfOnly = Program.GetConfigurationAttribute("Global/FileMaintenance/FileImport/ConvertLfOnly", "enabled", "0") == "1" ? true : false;
            if (ConvertLfOnly)
            {
                ConvertLfOnlyToCrLf = Program.GetConfigurationAttribute("Global/FileMaintenance/FileImport/ConvertLfOnlyToCrLf", "enabled", "0") == "1" ? true : false;
                ConvertLfOnlyToCr = Program.GetConfigurationAttribute("Global/FileMaintenance/FileImport/ConvertLfOnlyToCr", "enabled", "0") == "1" ? true : false;
            }
            else
            {
                ConvertLfOnlyToCrLf = false;
                ConvertLfOnlyToCr = false;
            }
            logFloppyAccess = Program.GetConfigurationAttribute("Global/FileMaintenance", "LogOS9FloppyWrites", "N") == "Y" ? true : false;
            os9FloppyWritesFile = Program.GetConfigurationAttribute("Global/FileMaintenance", "os9FloppyWritesFile", "");
        }

        public VirtualFloppyManipulationRoutines(string cDrivePathName, FileStream m_fp)
        {
            ReloadOptions();

            currentlyOpenedImageFileName = cDrivePathName;
            currentlyOpenedImageFileStream = m_fp;

            fileformat ff = GetFileFormat();

            if (ff == fileformat.fileformat_MINIX_68K || ff == fileformat.fileformat_MINIX_IBM)
                minixImage = new MinixImage(m_fp, ff);
        }

        #region Generic File Access
        public void Seek(long offset, SeekOrigin origin)
        {
            currentlyOpenedImageFileStream.Seek(offset, origin);
        }

        public void Read(byte[] buffer, int offset, int count)
        {
            currentlyOpenedImageFileStream.Read(buffer, offset, count);
        }

        #endregion

        public UniFLEX_SIR ReadUNIFLEX_SIR(FileStream fs)
        {
            long currentPosition = fs.Position;

            UniFLEX_SIR drive_SIR = new UniFLEX_SIR();

            fs.Seek(512, SeekOrigin.Begin);         // fseek (fs, 512, SEEK_SET);

            drive_SIR.m_supdt[0]    = (byte)fs.ReadByte();  // fread (&drive_SIR.m_supdt,  1,   1, fs);    //rmb 1       sir update flag                                         0x0200        -> 00 
            drive_SIR.m_swprot[0]   = (byte)fs.ReadByte();  // fread (&drive_SIR.m_swprot, 1,   1, fs);    //rmb 1       mounted read only flag                                  0x0201        -> 00 
            drive_SIR.m_slkfr[0]    = (byte)fs.ReadByte();  // fread (&drive_SIR.m_slkfr,  1,   1, fs);    //rmb 1       lock for free list manipulation                         0x0202        -> 00 
            drive_SIR.m_slkfdn[0]   = (byte)fs.ReadByte();  // fread (&drive_SIR.m_slkfdn, 1,   1, fs);    //rmb 1       lock for fdn list manipulation                          0x0203        -> 00 

            fs.Read(drive_SIR.m_sintid, 0, 4);              // fread (&drive_SIR.m_sintid, 1,   4, fs);    //rmb 4       initializing system identifier                          0x0204        -> 00 

            fs.Read(drive_SIR.m_scrtim, 0, 4);              // fread (&drive_SIR.m_scrtim, 1,   4, fs);    //rmb 4       creation time                                           0x0208        -> 11 44 F3 FC
            fs.Read(drive_SIR.m_sutime, 0, 4);              // fread (&drive_SIR.m_sutime, 1,   4, fs);    //rmb 4       date of last update                                     0x020C        -> 11 44 F1 51
            fs.Read(drive_SIR.m_sszfdn, 0, 2);              // fread (&drive_SIR.m_sszfdn, 1,   2, fs);    //rmb 2       size in blocks of fdn list                              0x0210        -> 00 4A          = 74
            fs.Read(drive_SIR.m_ssizfr, 0, 3);              // fread (&drive_SIR.m_ssizfr, 1,   3, fs);    //rmb 3       size in blocks of volume                                0x0212        -> 00 08 1F       = 2079
            fs.Read(drive_SIR.m_sfreec, 0, 3);              // fread (&drive_SIR.m_sfreec, 1,   3, fs);    //rmb 3       total free blocks                                       0x0215        -> 00 04 9C       = 
            fs.Read(drive_SIR.m_sfdnc,  0, 2);              // fread (&drive_SIR.m_sfdnc,  1,   2, fs);    //rmb 2       free fdn count                                          0x0218        -> 01 B0
            fs.Read(drive_SIR.m_sfname, 0, 14);             // fread (&drive_SIR.m_sfname, 1,  14, fs);    //rmb 14      file system name                                        0x021A        -> 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
            fs.Read(drive_SIR.m_spname, 0, 14);             // fread (&drive_SIR.m_spname, 1,  14, fs);    //rmb 14      file system pack name                                   0x0228        -> 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
            fs.Read(drive_SIR.m_sfnumb, 0, 2);              // fread (&drive_SIR.m_sfnumb, 1,   2, fs);    //rmb 2       file system number                                      0x0236        -> 00 00
            fs.Read(drive_SIR.m_sflawc, 0, 2);              // fread (&drive_SIR.m_sflawc, 1,   2, fs);    //rmb 2       flawed block count                                      0x0238        -> 00 00


            drive_SIR.m_sdenf[0] = (byte)fs.ReadByte();     // fread (&drive_SIR.m_sdenf,  1,   1, fs);    //rmb 1       density flag - 0=single                                 0x023A        -> 01
            drive_SIR.m_ssidf[0] = (byte)fs.ReadByte();     // fread (&drive_SIR.m_ssidf,  1,   1, fs);    //rmb 1       side flag - 0=single                                    0x023B        -> 01

            fs.Read(drive_SIR.m_sswpbg, 0, 3);              // fread (&drive_SIR.m_sswpbg, 1,   3, fs);    //rmb 3       swap starting block number                              0x023C        -> 00 08 20
            fs.Read(drive_SIR.m_sswpsz, 0, 2);              // fread (&drive_SIR.m_sswpsz, 1,   2, fs);    //rmb 2       swap block count                                        0x023F        -> 01 80

            drive_SIR.m_s64k[0] = (byte)fs.ReadByte();      // fread (&drive_SIR.m_s64k,   1,   1, fs);    //rmb 1       non-zero if swap block count is multiple of 64K         0x0241        -> 00

            fs.Read(drive_SIR.m_swinc, 0, 11);              // fread (&drive_SIR.m_swinc,  1,  11, fs);    //rmb 11      Winchester configuration info                           0x0242        -> 00 00 00 00 00 00 2A 00 99 00 9A
            fs.Read(drive_SIR.m_sspare, 0, 11);             // fread (&drive_SIR.m_sspare, 1,  11, fs);    //rmb 11      spare bytes - future use                                0x024D        -> 00 9B 00 9C 00 9D 00 9E 00 9F 00

            drive_SIR.m_snfdn[0] = (byte)fs.ReadByte();     // fread (&drive_SIR.m_snfdn,  1,   1, fs);    //rmb 1       number of in core fdns                                  0x0278        -> A0     *snfdn * 2 = 320

            fs.Read(drive_SIR.m_scfdn, 0, 160);             // fread (&drive_SIR.m_scfdn,  1, 160, fs);    //rmb CFDN*2  in core free fdns                                       0x0279        variable (*snfdn * 2)

            drive_SIR.m_snfree[0] = (byte)fs.ReadByte();    // fread (&drive_SIR.m_snfree, 1,   1, fs);    //rmb 1       number of in core free blocks                           0x03B9        -> 03

            fs.Read(drive_SIR.m_sfree, 0, 300);             // fread (&drive_SIR.m_sfree,  1, 300, fs);    //rmb         CDBLKS*DSKADS in core free blocks                       0x03BA        -> 

            fs.Seek(currentPosition, SeekOrigin.Begin);

            return drive_SIR;
        }

        public uint ConvertToInt16(byte[] value)
        {
            return (uint)(value[0] * 256) + (uint)(value[1]);
        }

        public uint ConvertToInt24(byte[] value)
        {
            return (uint)(value[0] * 65536) + (uint)(value[1] * 256) + (uint)(value[2]);
        }

        public uint ConvertToInt32(byte[] value)
        {
            return (uint)(value[0] * 16777216) + (uint)(value[1] * 65536) + (uint)(value[2] * 256) + (uint)(value[3]);
        }

        /// <summary>
        /// Currently unused
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        string ConvertDateTime(byte[] value)
        {
            long fileTime = (value[0] * 16777216) + (value[1] * 65536) + (value[2] * 256) + (value[3]);
            DateTime t = UNIXtoDateTime(fileTime);

            int year = t.Year;
            int month = t.Month;
            int day = t.Day;
            int hour = t.Hour;
            int minute = t.Minute;
            int second = t.Second;

            //if (year < 100)
            //    year += 1900;
            //else 
            //{
            //    year -= 100;
            //    year += 2000;
            //}

            string strDateTime = string.Format("{0}/{1}/{2} {3}:{4}:{5}", month.ToString("00"), day.ToString("00"), year.ToString("0000"), hour.ToString("00"), minute.ToString("00"), second.ToString("00"));

            return strDateTime;
        }

        /*
        public string ConvertDateTime(byte[] value)
        {
            int fileTime = (value[0] * 16777216) + (value[1] * 65536) + (value[2] * 256) + (value[3]);

            //tm *t = new tm;
            //int terr = localtime_s (t, &fileTime);

            //int year   = t->tm_year;
            //int month  = t->tm_mon + 1;
            //int day    = t->tm_mday;
            //int hour   = t->tm_hour;
            //int minute = t->tm_min;
            //int second = t->tm_sec;

            //if (year < 100)
            //    year += 1900;
            //else 
            //{
            //    year -= 100;
            //    year += 2000;
            //}

            string strDateTime = "";
            //strDateTime.Format ("%02d/%02d/%04d %02d:%02d:%02d", month, day, year, hour, minute, second);

            //delete t;

            return strDateTime;
        }
        */

        // Valid FLEX diskette image formats supported by SWTPC DC-4 and DMAF disk controllers (DSK sizes are simply FLEX max sector * FLEX max track * 1 (number of cylinders)
        //
        //      description                         IMA size    DSK size    cyl hd  sector per side track 0     sectors per side (the rest) 
        //      ----------------------------------- -------     -------     --- --- --------------------------- --------------------------- 
        //                                                                              FLEX max sector             FLEX max track
        //          5 1/4" 80 track                                                     ---------------             --------------
        //																	                                                how to calculate the IMA size.
        //																	                                                ------------------------------------------
        //      DS/DD with SD (FM) cylinder 0.      733184      737280      80  2   10      36                  18      79  ((79 * 18 * 2) + (10 * 2)) * 256 = IMA size
        //      DS/SD with SD (FM) cylinder 0.      409600      409600      80  2   10      20                  10      79  ((79 * 10 * 2) + (10 * 2)) * 256 = IMA size
        //      SS/DD with SD (FM) cylinder 0.      366592      368640      80  1   10      18                  18      79  ((79 * 18 * 1) + (10 * 1)) * 256 = IMA size
        //      SS/SD with SD (FM) cylinder 0.      204800      204800      80  1   10      10                  10      79  ((79 * 10 * 1) + (10 * 1)) * 256 = IMA size
        //
        //          5 1/4" 40 track 
        //
        //      DS/DD with SD (FM) cylinder 0.      364544      368640      40  2   10      36                  18      39  ((39 * 18 * 2) + (10 * 2)) * 256 = IMA size
        //      DS/SD with SD (FM) cylinder 0       204800      204800      40  2   10      20                  10      39  ((39 * 10 * 2) + (10 * 2)) * 256 = IMA size
        //      SS/DD with SD (FM) cylinder 0.      182272      184320      40  1   10      18                  18      39  ((39 * 18 * 1) + (10 * 1)) * 256 = IMA size
        //      SS/SD with SD (FM) cylinder 0.      102400      102400      40  1   10      10                  10      39  ((39 * 10 * 1) + (10 * 1)) * 256 = IMA size
        //
        //          5 1/4" 35 track
        //
        //      DS/DD with SD (FM) cylinder 0.      318464      322560      35  2   10      36                  18      34  ((34 * 18 * 2) + (10 * 2)) * 256 = IMA size
        //      DS/SD with SD (FM) cylinder 0       179200      179200      35  2   10      20                  10      34  ((34 * 10 * 2) + (10 * 2)) * 256 = IMA size
        //      SS/DD with SD (FM) cylinder 0.      159232      161280      35  1   10      18                  18      34  ((34 * 18 * 1) + (10 * 1)) * 256 = IMA size
        //      SS/SD with SD (FM) cylinder 0.       89600       89600      35  1   10      10                  10      34  ((34 * 10 * 1) + (10 * 1)) * 256 = IMA size
        //
        //          8" 77 track FLEX DMAF-2
        //
        //      DS/DD with SD (FM) cylinder 0.     1019392     1025024      77  2   15      52                  26      76  ((76 * 26 * 2) + (15 * 2)) * 256 = IMA size
        //      DS/SD with SD (FM) cylinder 0       591360      591360      77  2   15      30                  15      76  ((76 * 15 * 2) + (15 * 2)) * 256 = IMA size 
        //      SS/DD with SD (FM) cylinder 0.      509696      512512      77  1   15      26                  26      76  ((76 * 26 * 1) + (15 * 1)) * 256 = IMA size 
        //      SS/SD with SD (FM) cylinder 0.      295680      295680      77  1   15      15                  15      76  ((76 * 15 * 1) + (15 * 1)) * 256 = IMA size

        public enum ValidFLEXGeometries
        {
            UNKNOWN = 0,
            SSSD35T,
            SSDD35T,
            DSSD35T,
            DSDD35T,
            SSSD40T,
            SSDD40T,
            DSSD40T,
            DSDD40T,
            SSSD77T,
            SSDD77T,
            DSSD77T,
            DSDD77T,
            SSSD80T,
            SSDD80T,
            DSSD80T,
            DSDD80T,
        }

        // these are the public variable that are availabe and are set by GetFLEXGeometry

        public fileformat currentFileFileFormat = fileformat.fileformat_UNKNOWN;
        public bool singleSided = true;
        public bool isFiveInch = true;
        public int currentDiskDiameter = 0;
        public int maxSector = 0;
        public int maxTrack = 0;
        public int sectorOnTrackZero = 0;
        public int sectorsToEndOfDirectory = 0;
        public bool trackZeroIsBigEnough = false;
        public int sectorsToAddToFreeChain = 0;
        public string getGeometryErrorMessage = "";

        public ValidFLEXGeometries currentDiskGeometry = ValidFLEXGeometries.UNKNOWN;

        public void GetFLEXGeometry()
        {
            currentDiskDiameter = 0;
            currentDiskGeometry = ValidFLEXGeometries.UNKNOWN;
            getGeometryErrorMessage = "";

            if (currentFileFileFormat == fileformat.fileformat_FLEX || currentFileFileFormat == fileformat.fileformat_FLEX_IMA)
            {
                // only do this for FLEX .DSK files

                FileStream m_fp = currentlyOpenedImageFileStream;

                m_fp.Seek(PartitionBias + 0x0310 - (sectorSize * SectorBias), SeekOrigin.Begin);
                RAW_SIR stSystemInformationRecord = ReadRAW_FLEX_SIR(m_fp);

                // use the sector and track count to calculate the expected file size for a .DSK file

                maxSector = stSystemInformationRecord.cMaxSector;
                maxTrack = stSystemInformationRecord.cMaxTrack;

                if (maxSector == 10 || maxSector == 20 || maxSector == 15)
                {
                    getGeometryErrorMessage = "This appears to be a single density image so no converion id required.";
                }
                //else      // commented out so we can set geometry on single density.
                {
                    if ((maxSector * maxTrack * 256) != m_fp.Length)
                    {
                        // get the number of sectors on track 0 - start at sector 5 to position to the start of the directory

                        sectorOnTrackZero = 5;
                        sectorsToEndOfDirectory = 5;
                        byte[] trackLinkBytes = new byte[17];

                        for (int i = 5; i < maxSector; i++)
                        {
                            m_fp.Seek(i * 256, SeekOrigin.Begin);
                            m_fp.Read(trackLinkBytes, 0, 17);
                            if (trackLinkBytes[0] == 0)
                            {
                                sectorOnTrackZero++;
                                if (trackLinkBytes[16] != 0)
                                {
                                    sectorsToEndOfDirectory++;
                                }
                            }
                            else
                                break;
                        }

                        // see if the directory is has more sectors than track 0 on an IMA image can handle

                        sectorsToAddToFreeChain = 0;
                        trackZeroIsBigEnough = false;

                        if (sectorsToEndOfDirectory <= 20)
                        {
                            trackZeroIsBigEnough = true;
                        }
                        else
                        {
                            // set up to move the extra directory sectors to the end of the free chain.

                            sectorsToAddToFreeChain = sectorsToEndOfDirectory - 20;
                            trackZeroIsBigEnough = true;
                        }

                        //if (trackZeroIsBigEnough)   // this will always be true
                        {
                            // see if this could be a modified image that had sectors added to track 0 to make it orthogonal

                            if ((maxSector * (maxTrack + 1) * 256) == m_fp.Length)      // if the calculated file size = actaul file image size, this is either sssd or dssd or modified ssdd or modified dsdd
                            {
                                // could be, but also could be Single Density diskette original

                                if (maxTrack == 34 || maxTrack == 39 || maxTrack == 76 || maxTrack == 79)   // these are the valid number of tracks for an actual diskette
                                {
                                    if (maxTrack != 76) // 5 1/4"
                                    {
                                        currentDiskDiameter = 5;

                                        // these are the valid number of sectors on an actual 5 1/4" diskettes (72 is QUAD density)
                                        if (maxSector == 10 || maxSector == 20)
                                        {
                                            // this is single density
                                            if (maxSector == 10)
                                            {
                                                // this is single sided
                                                switch (maxTrack)
                                                {
                                                    case 34:
                                                        currentDiskGeometry = ValidFLEXGeometries.SSSD35T;
                                                        break;
                                                    case 39:
                                                        currentDiskGeometry = ValidFLEXGeometries.SSSD40T;
                                                        break;
                                                    case 76:
                                                        currentDiskGeometry = ValidFLEXGeometries.SSSD77T;
                                                        break;
                                                    case 79:
                                                        currentDiskGeometry = ValidFLEXGeometries.SSSD80T;
                                                        break;
                                                }
                                            }
                                            else
                                            {
                                                // this is double sided
                                                switch (maxTrack)
                                                {
                                                    case 34:
                                                        currentDiskGeometry = ValidFLEXGeometries.DSSD35T;
                                                        break;
                                                    case 39:
                                                        currentDiskGeometry = ValidFLEXGeometries.DSSD40T;
                                                        break;
                                                    case 76:
                                                        currentDiskGeometry = ValidFLEXGeometries.DSSD77T;
                                                        break;
                                                    case 79:
                                                        currentDiskGeometry = ValidFLEXGeometries.DSSD80T;
                                                        break;
                                                }
                                            }
                                        }
                                        else if (maxSector == 18 || maxSector == 36)
                                        {
                                            // this is double density
                                            if (maxSector == 18)
                                            {
                                                // this is single sided
                                                switch (maxTrack)
                                                {
                                                    case 34:
                                                        currentDiskGeometry = ValidFLEXGeometries.SSDD35T;
                                                        break;
                                                    case 39:
                                                        currentDiskGeometry = ValidFLEXGeometries.SSDD40T;
                                                        break;
                                                    case 76:
                                                        currentDiskGeometry = ValidFLEXGeometries.SSDD77T;
                                                        break;
                                                    case 79:
                                                        currentDiskGeometry = ValidFLEXGeometries.SSDD80T;
                                                        break;
                                                }
                                            }
                                            else
                                            {
                                                // this is double sided
                                                switch (maxTrack)
                                                {
                                                    case 34:
                                                        currentDiskGeometry = ValidFLEXGeometries.DSDD35T;
                                                        break;
                                                    case 39:
                                                        currentDiskGeometry = ValidFLEXGeometries.DSDD40T;
                                                        break;
                                                    case 76:
                                                        currentDiskGeometry = ValidFLEXGeometries.DSDD77T;
                                                        break;
                                                    case 79:
                                                        currentDiskGeometry = ValidFLEXGeometries.DSDD80T;
                                                        break;
                                                }
                                            }
                                        }
                                    }
                                    else                // 8"
                                    {
                                        currentDiskDiameter = 8;

                                        // these are the valid number of sectors on an actual 8" diskettes
                                        if (maxSector == 15 || maxSector == 30)
                                        {
                                            if (maxSector == 15)
                                            {
                                                // this is single sided
                                                switch (maxTrack)
                                                {
                                                    case 34:
                                                        currentDiskGeometry = ValidFLEXGeometries.SSSD35T;
                                                        break;
                                                    case 39:
                                                        currentDiskGeometry = ValidFLEXGeometries.SSSD40T;
                                                        break;
                                                    case 76:
                                                        currentDiskGeometry = ValidFLEXGeometries.SSSD77T;
                                                        break;
                                                    case 79:
                                                        currentDiskGeometry = ValidFLEXGeometries.SSSD80T;
                                                        break;
                                                }
                                            }
                                            else
                                            {
                                                // this is double sided
                                                switch (maxTrack)
                                                {
                                                    case 34:
                                                        currentDiskGeometry = ValidFLEXGeometries.DSSD35T;
                                                        break;
                                                    case 39:
                                                        currentDiskGeometry = ValidFLEXGeometries.DSSD40T;
                                                        break;
                                                    case 76:
                                                        currentDiskGeometry = ValidFLEXGeometries.DSSD77T;
                                                        break;
                                                    case 79:
                                                        currentDiskGeometry = ValidFLEXGeometries.DSSD80T;
                                                        break;
                                                }
                                            }
                                        }
                                        else if (maxSector == 26 || maxSector == 52)
                                        {
                                            // this is double density
                                            if (maxSector == 26)
                                            {
                                                // this is single sided
                                                switch (maxTrack)
                                                {
                                                    case 34:
                                                        currentDiskGeometry = ValidFLEXGeometries.SSDD35T;
                                                        break;
                                                    case 39:
                                                        currentDiskGeometry = ValidFLEXGeometries.SSDD40T;
                                                        break;
                                                    case 76:
                                                        currentDiskGeometry = ValidFLEXGeometries.SSDD77T;
                                                        break;
                                                    case 79:
                                                        currentDiskGeometry = ValidFLEXGeometries.SSDD80T;
                                                        break;
                                                }
                                            }
                                            else
                                            {
                                                // this is double sided
                                                switch (maxTrack)
                                                {
                                                    case 34:
                                                        currentDiskGeometry = ValidFLEXGeometries.DSDD35T;
                                                        break;
                                                    case 39:
                                                        currentDiskGeometry = ValidFLEXGeometries.DSDD40T;
                                                        break;
                                                    case 76:
                                                        currentDiskGeometry = ValidFLEXGeometries.DSDD77T;
                                                        break;
                                                    case 79:
                                                        currentDiskGeometry = ValidFLEXGeometries.DSDD80T;
                                                        break;
                                                }
                                            }
                                        }
                                    }
                                }
                                
                                if (currentDiskDiameter != 0 && currentDiskGeometry != ValidFLEXGeometries.UNKNOWN)
                                {
                                    switch (maxSector)
                                    {
                                        case 18:
                                            isFiveInch = true;
                                            singleSided = true;
                                            break;
                                        case 26:
                                            isFiveInch = false;
                                            singleSided = true;
                                            break;
                                        case 36:
                                            singleSided = false;
                                            break;
                                        case 52:
                                            isFiveInch = false;
                                            singleSided = false;
                                            break;

                                    }
                                }
                                else
                                {
                                    string message = string.Format("    Cylinders: {0}\r\n    Sectors on track 0: {1}\r\n    Max Sectors: {2}", maxTrack + 1, sectorOnTrackZero, maxSector);
                                    getGeometryErrorMessage = string.Format("This iamge does not match any FLEX standard formats\r\n\r\n{0}", message);
                                }
                            }
                            else
                                getGeometryErrorMessage = "This .DSK image does not seem to be valid. The calcutated expected size does not match the actual size.";
                        }
                    }
                }
            }

            if (currentFileFileFormat == fileformat.fileformat_FLEX_IMA)
                getGeometryErrorMessage = "This image is already in .IMA format. A direct copy will be made.";
            else if (currentFileFileFormat == fileformat.fileformat_FLEX_IDE)
                getGeometryErrorMessage = "We cannot convert IDE images to .IMA.";
            else if (currentFileFileFormat != fileformat.fileformat_FLEX)
                getGeometryErrorMessage = "This will only convert FLEX .DSK images to FLEX .IMA images.";
            else
                getGeometryErrorMessage = "Click the Convert button to create a converted file";
        }

        private fileformat GetFileFormat()
        {
            return GetFileFormat(currentlyOpenedImageFileStream);
        }

        #region IMD classes, variables and enums
        public enum IMDHeadValues
        {
            // This value indicates the side of the  disk  on  which  this  track
            // occurs (0 or 1).
            // 
            // Since HEAD can only be 0 or 1,  ImageDisk uses the upper  bits  of
            // this byte to indicate the presense of optional items in the  track
            // data:

            // Bit 7 (0x80) = Sector Cylinder Map
            // Bit 6 (0x40) = Sector Head     Map
        }

        public enum IMDModeValues
        {
            //  This value indicates the data transfer rate and density  in  which
            //  the original track was recorded:
            
            // 00 = 500 kbps FM   \   Note:   kbps indicates transfer rate,
            // 01 = 300 kbps FM    >          not the data rate, which is
            // 02 = 250 kbps FM   /           1/2 for FM encoding.
            // 03 = 500 kbps MFM
            // 04 = 300 kbps MFM
            // 05 = 250 kbps MFM
        }

        public enum IMDSectorSizes
        {
            //  The Sector Size value indicates the actual size of the sector data
            //  occuring on the track:

            // 00 =  128 bytes/sector
            // 01 =  256 bytes/sector
            // 02 =  512 bytes/sector
            // 03 = 1024 bytes/sector
            // 04 = 2048 bytes/sector
            // 05 = 4096 bytes/sector
            // 06 = 8192 bytes/sector
        }

        public enum IMDSectorAttribute
        {
            unavailable                         = 0x00,    // 00      Sector data unavailable - could not be read
            normal                              = 0x01,    // 01 .... Normal data: (Sector Size) bytes follow
            compressedTheSame                   = 0x02,    // 02 xx   Compressed: All bytes in sector have same value (xx)
            normalWithDeletedDataAddressMark    = 0x03,    // 03 .... Normal data with "Deleted-Data address mark"
            compressedWIthDeletedDataAddressMark= 0x04,    // 04 xx   Compressed  with "Deleted-Data address mark"
            normalWithError                     = 0x05,    // 05 .... Normal data read with data error
            compressedWithReadError             = 0x06,    // 06 xx   Compressed  read with data error
            normalDeletedDataWithError          = 0x07,    // 07 .... Deleted data read with data error
            compressedDeletedDataWithError      = 0x08     // 08 xx   Compressed, Deleted read with data error
        }

        public class IMDSectorDataRecord
        {
            public byte dataRecordAttribute;    // sector data attribute                                    01
            public byte [] dataBytes;           // sector data records              * number of sectors     
        }

        public class IMDTrack
        {
            public byte mode;                   //1 byte Mode value                 (0 - 5)                 02 <- 02 = 250 kbps FM
            public byte cylinder;               //1 byte Cylinder                   (0 - n)                 00 <- cynlinder
            public byte head;                   //1 byte Head                       (0 - 1)   (see Note)    00 <- head 0 and no bit 7  or 6 set so no sector cylinder map(optional) * number of sectors
            public byte numberOfSectorsInTrack; //1 byte number of sectors in track (1 - n)                 0A <- 10 sectors on this track
            public byte sectorSize;             //1 byte sector size                (0 - 6)                 01 <- 128 * 2^n where n is 1 = 256 bytes per sector
            public List<byte> numberingMap;    //sector numbering map              * number of sectors     01 03 05 07 09 02 04 06 08 0A <- this is the order of the sectors
            public List<byte> cylinderMap;      //sector cylinder map(optional)     * number of sectors     xx <- head value indicates that this optional field is missing
            public List<byte> headMap;          //sector head map(optional)         * number of sectors     xx <- head value indicates that this optional field is missing
            public List<IMDSectorDataRecord> dataRecords;
        }
        #endregion

        List<IMDTrack> imdTracks = new List<IMDTrack>();

        public void ConvertIMDtoIMA (FileStream fs, string newFIlename)
        {
            long fileLength = fs.Length;

            imdTracks = new List<IMDTrack>();
            IMDTrack imdTrack = new IMDTrack();

            string errorMessage = "";

            // read past the header - it ends with 0x1A.

            for (int i = 0; i < fileLength; i++)
            {
                byte b = (byte)fs.ReadByte();
                if (b == 0x1A)
                {
                    // we have reached the end of the header
                    break;
                }
            }

            // we found the 0x1A before we read the whole file - keep goin
            while (fs.Position < fileLength)
            {
                imdTrack = new IMDTrack();
                imdTrack.mode = (byte)fs.ReadByte();
                imdTrack.cylinder = (byte)fs.ReadByte();
                imdTrack.head = (byte)fs.ReadByte();
                imdTrack.numberOfSectorsInTrack = (byte)fs.ReadByte();
                imdTrack.sectorSize = (byte)fs.ReadByte();

                imdTrack.numberingMap = new List<byte>();
                for (int i = 0; i < imdTrack.numberOfSectorsInTrack; i++)
                {
                    imdTrack.numberingMap.Add((byte)fs.ReadByte());
                }

                if ((imdTrack.head & 0x80) == 0x80)
                {
                    // we need to read in the cyliner map
                    imdTrack.cylinderMap = new List<byte>();
                    for (int i = 0; i < imdTrack.numberOfSectorsInTrack; i++)
                    {
                        imdTrack.cylinderMap.Add((byte)fs.ReadByte());
                    }
                }
                if ((imdTrack.head & 0x40) == 0x40)
                {
                    // we need to read in the head map
                    imdTrack.headMap = new List<byte>();
                    for (int i = 0; i < imdTrack.numberOfSectorsInTrack; i++)
                    {
                        imdTrack.headMap.Add((byte)fs.ReadByte());
                    }
                }

                int bytesPerSector = (1 << imdTrack.sectorSize) * 128;
                imdTrack.dataRecords = new List<IMDSectorDataRecord>();      // create a new List to hold the data records for this track

                // now get the dta records and add them to the track

                for (int currentSector = 0; currentSector < imdTrack.numberOfSectorsInTrack; currentSector++)
                {
                    // create a data record for each sector in the track.

                    IMDSectorDataRecord dataRecord = new IMDSectorDataRecord();     // create a data record to add to the list
                    dataRecord.dataRecordAttribute = (byte)fs.ReadByte();           // put the attribute in the dataRecord

                    errorMessage = "";
                    switch (dataRecord.dataRecordAttribute)
                    {
                        // if the dat read was normal, add the data bytes to the datarecord list od data bytes.

                        case (int)IMDSectorAttribute.unavailable:
                            errorMessage = "Unhandled: Sector Unavailable";
                            break;

                        case (int)IMDSectorAttribute.normal:
                            {
                                dataRecord.dataBytes = new byte[bytesPerSector];
                                for (int i = 0; i < bytesPerSector; i++)
                                {
                                    dataRecord.dataBytes[i] = (byte)fs.ReadByte();
                                }
                                errorMessage = "";
                            }
                            break;

                        case (int)IMDSectorAttribute.compressedTheSame:
                            {
                                dataRecord.dataBytes = new byte[bytesPerSector];    // get the byte to replicate for compressed sector
                                byte dataByte = (byte)fs.ReadByte();
                                for (int i = 0; i < bytesPerSector; i++)
                                {
                                    dataRecord.dataBytes[i] = dataByte;
                                    errorMessage = "";
                                }
                            }
                            break;

                        case (int)IMDSectorAttribute.normalWithDeletedDataAddressMark:
                            errorMessage = "Unhandled: Sector Normal With Deleted Data Address Mark";
                            break;

                        case (int)IMDSectorAttribute.compressedWIthDeletedDataAddressMark:
                            errorMessage = "Unhandled: Sector Compressed WIth Deleted Data Address Mark";
                            break;

                        case (int)IMDSectorAttribute.normalWithError:
                            errorMessage = "Unhandled: Sector Normal With Error";
                            break;

                        case (int)IMDSectorAttribute.compressedWithReadError:
                            errorMessage = "Unhandled: Sector Compressed With Read Error";
                            break;

                        case (int)IMDSectorAttribute.normalDeletedDataWithError:
                            errorMessage = "Unhandled: Sector Normal Deleted Data With Error";
                            break;

                        case (int)IMDSectorAttribute.compressedDeletedDataWithError:
                            errorMessage = "Unhandled: Sector Compressed Deleted Data With Error";
                            break;

                        default:
                            errorMessage = "Unhandled: Sector With Undocumented Attribute";
                            break;
                    }
                    if (errorMessage.Length > 0)
                    {
                        MessageBox.Show(errorMessage);
                        break;
                    }

                    imdTrack.dataRecords.Add(dataRecord);
                }
                imdTracks.Add(imdTrack);

                if (errorMessage.Length > 0)
                {
                    break;
                }
            }

            // we now have all the sectors from all of the track in imdTracks.

            using (BinaryWriter imaFIle = new BinaryWriter(File.Open(newFIlename, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite)))
            {
                foreach (IMDTrack track in imdTracks)
                {
                    int minSector = 256;
                    int maxSector = 0;

                    for (int i = 0; i < track.numberOfSectorsInTrack; i++) if (track.numberingMap[i] < minSector) minSector = track.numberingMap[i];
                    for (int i = 0; i < track.numberOfSectorsInTrack; i++) if (track.numberingMap[i] > maxSector) maxSector = track.numberingMap[i];

                    // now that we know our limits - lets find and write the sectors to the file.

                    for (int i = minSector; i <= maxSector; i++)
                    {
                        // first find the sector record in the list

                        for (int j = 0; j < track.numberingMap.Count; j++)
                        {
                            if (track.numberingMap[j] == i)
                            {
                                // we found it - write corresponding data bytes to the file. and break;

                                imaFIle.Write(track.dataRecords[j].dataBytes, 0, (1 << track.sectorSize) * 128);
                                imaFIle.Flush();
                                break;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Determine the file format OS9 - FLEX or UniFLEX
        /// </summary>
        /// <param name="fs"></param>
        /// <returns></returns>
        public fileformat GetFileFormat(FileStream fs)
        {
            fileformat ff = fileformat.fileformat_UNKNOWN;

            if (fs != null)
            {
                // save current position so we can restore it later.

                long currentPosition = fs.Position;
                long fileLength = fs.Length;        // int fd = _fileno (fs);   long fileLength = _filelength(fd);

                byte[] imdBytes = new byte[4];

                fs.Seek(0, SeekOrigin.Begin);
                fs.Read(imdBytes, 0, 4);
                ASCIIEncoding ascii = new ASCIIEncoding();
                string imdString = ascii.GetString(imdBytes);
                fs.Seek(currentPosition, SeekOrigin.Begin);

                if (imdString == "IMD ")
                {
                    // read past the header - it ends with 0x1A.

                    for (int i = 0; i < fileLength; i++)
                    {
                        byte b = (byte)fs.ReadByte();
                        if (b == 0x1A)
                        {
                            // we have reached the end of the header
                            break;
                        }
                    }

                    if (fs.Position != fileLength)
                        ff = fileformat.fileformat_IMD;
                }
                else
                {
                    // First Check for OS9 format

                    OS9_ID_SECTOR stIDSector = new OS9_ID_SECTOR();

                    fs.Seek(0, SeekOrigin.Begin);

                    stIDSector.cTOT[0] = (byte)fs.ReadByte();         // fread (&stIDSector.cTOT[0], 1, 1, fs);   // Total Number of sector on media
                    stIDSector.cTOT[1] = (byte)fs.ReadByte();         // fread (&stIDSector.cTOT[1], 1, 1, fs);
                    stIDSector.cTOT[2] = (byte)fs.ReadByte();         // fread(&stIDSector.cTOT[2], 1, 1, fs);
                    stIDSector.cTKS[0] = (byte)fs.ReadByte();         // fread (&stIDSector.cTKS[0], 1, 1, fs);   // Sectors Per Track (not track 0)

                    fs.Seek(16, SeekOrigin.Begin);                    // fseek(fs, 16, SEEK_SET);

                    stIDSector.cFMT[0] = (byte)fs.ReadByte();         // fread (&stIDSector.cFMT[0], 1, 1, fs);     // Disk Format Byte
                    stIDSector.cSPT[0] = (byte)fs.ReadByte();         // fread (&stIDSector.cSPT[0], 1, 1, fs);     // Sectors per track on track 0 high byte
                    stIDSector.cSPT[1] = (byte)fs.ReadByte();         // fread (&stIDSector.cSPT[1], 1, 1, fs);     // Sectors per track on track 0 low  byte

                    fs.Seek(104, SeekOrigin.Begin);                   // Get the LSNSize from 68K Os9 disk

                    stIDSector.cDD_LSNSize[0] = (byte)fs.ReadByte();  // fread (&stIDSector.cSPT[1], 1, 1, fs);     // Sectors per track on track 0 low  byte
                    stIDSector.cDD_LSNSize[1] = (byte)fs.ReadByte();  // fread (&stIDSector.cSPT[1], 1, 1, fs);     // Sectors per track on track 0 low  byte

                    nLSNBlockSize = stIDSector.cDD_LSNSize[1] + (stIDSector.cDD_LSNSize[0] * 256);

                    // these are used by OS9/68K - on EmuOS9Boot.Dsk these are all zeros if nLSNBlockSize = 0

                    sectorSize = 256;                   // default sector size if 
                    if (nLSNBlockSize == 0)
                        nLSNBlockSize = 256;

                    //if (nLSNBlockSize != 0)
                    //{
                    //    sectorSize = nLSNBlockSize;
                    //}

                    // There's no point in going any further if the file size if not right

                    byte bSectorsPerracks = stIDSector.cTKS[0];
                    int  nSectorsPerTrack = stIDSector.cSPT[1] + (stIDSector.cSPT[0] * 256);
                    int nTotalSectors = stIDSector.cTOT[2] + (stIDSector.cTOT[1] * 256) + (stIDSector.cTOT[0] * 65536);

                    // get disk size based on reported number of sectors in the first three bytes of the SIR

                    long nDSKDiskSize = (long)(nTotalSectors * nLSNBlockSize);
                    long nIMAdiskSize = 0;

                    if (nDSKDiskSize == (fileLength & 0xFFFFFF00))
                    {
                        ff = fileformat.fileformat_OS9;
                        SectorBias = 0;
                        m_lPartitionBias = 0;

                        currentFileFileFormat = ff;
                    }
                    else
                    {
                        sectorSize = 256;

                        //let's see if this is a 128 byte sector diskette

                        //{
                        //    get current offset so we can set it back when we are done

                        //    long currentOffset = fs.Position;

                        //    the System Information Record on a mini FLEX diskette is at
                        //    offset 0x0080 in the diskette image file.


                        //      an example from the DISK31_2.DSK file looks like this:

                        //     00 00 00 00 00 00 00 00  00 00 00 00 00 00 00 00  00 00 00 00 00 07 0A 22  12 01 EF 00 00 00 00 00
                        //     00 00 00 00 00 00 00 00  00 00 00 00 00 00 00 00  00 00 00 00 00 00 00 00  00 00 00 00 00 00 00 00
                        //     00 00 00 00 00 00 00 00  00 00 00 00 00 00 00 00  00 00 00 00 00 00 00 00  00 00 00 00 00 00 00 00
                        //     00 00 00 00 00 00 00 00  00 00 00 00 00 00 00 00  00 00 00 00 00 00 00 00  00 00 00 00 00 00 00 00


                        //     the one from the MFLXSYS.DSK looks like this:

                        //     00 00 00 00 00 00 00 00  00 00 00 00 00 00 00 00  00 00 00 00 00 10 0D 10  0B 00 3D 00 00 00 00 00
                        //     00 00 00 00 00 00 00 00  00 00 00 00 00 00 00 00  00 00 00 00 00 00 00 00  00 00 00 00 00 00 00 00
                        //     00 00 00 00 00 00 00 00  00 00 00 00 00 00 00 00  00 00 00 00 00 00 00 00  00 00 00 00 00 00 00 00
                        //     00 00 00 00 00 00 00 00  00 00 00 00 00 00 00 00  00 00 00 00 00 00 00 00  00 00 00 00 00 00 00 00


                        //      The directory starts at offset 0x0100.The first 8 bytes of the directory sector are unused except
                        //      for sector linkage. Each entry consists of 24 bytes.

                        //      A single sided 35 track diskette
                        //          35 tracks * 17 sectors per track = 595 sectors * 128 bytes per sector = 76, 160 bytes
                        //          605 sectors = 77, 440
                        //          614 sectors = 78, 592


                        //      A single sided 40 track diskette will be 35 tracks * 18 sectors per track * 128 bytes per sector =


                        //     return position to where we were when we got here

                        //    fs.Seek(currentOffset, SeekOrigin.Begin);
                        //}

                        // not OS-9, see if this image conforms to a valid FLEX diskette image

                        int nMaxSector;
                        int nMaxTrack;

                        SectorBias = 1;

                        RAW_SIR stSystemInformationRecord = ReadRAW_FLEX_SIR(fs);

                        nMaxSector = stSystemInformationRecord.cMaxSector;
                        nMaxTrack = stSystemInformationRecord.cMaxTrack;
                        nTotalSectors = stSystemInformationRecord.cTotalSectorsHi * 256 + stSystemInformationRecord.cTotalSectorsLo;

                        nDSKDiskSize = (long)(nMaxTrack + 1) * (long)nMaxSector * (long)sectorSize;           // Track is 0 based, sector is 1 based
                        nIMAdiskSize = 0;
                        switch (nMaxSector)
                        {
                            // these are the only valid max sector values for IMA (10 or 18 = single side, 20 or 36 = double sided)

                            case 10:
                                nIMAdiskSize = (long)(((nMaxTrack) * (long)nMaxSector * (long)sectorSize) + (sectorSize * 10));           // single sided
                                break;
                            case 20:
                                nIMAdiskSize = (long)(((nMaxTrack) * (long)nMaxSector * (long)sectorSize) + (sectorSize * 10 * 2));       // double sided
                                break;
                            case 18:
                                nIMAdiskSize = (long)(((nMaxTrack) * (long)nMaxSector * (long)sectorSize) + (sectorSize * 10));           // single sided
                                break;
                            case 36:
                                nIMAdiskSize = (long)(((nMaxTrack) * (long)nMaxSector * (long)sectorSize) + (sectorSize * 10 * 2));       // double sided
                                break;

                            // 8" images

                            case 15:        // 8" single sided single density
                                nIMAdiskSize = (long)(((nMaxTrack + 1) * (long)nMaxSector * (long)sectorSize));
                                break;
                            case 26:        // 8" single sided double density
                                nIMAdiskSize = (long)(((nMaxTrack) * (long)nMaxSector * (long)sectorSize) + (sectorSize * 15));
                                break;
                            case 52:        // 8" double sided double density
                                nIMAdiskSize = (long)(((nMaxTrack) * (long)nMaxSector * (long)sectorSize) + (sectorSize * 15 * 2));
                                break;

                            // special GoTek images
                            case 255:        // 
                                nIMAdiskSize = (long)(((nMaxTrack) * (long)nMaxSector * (long)sectorSize) + (sectorSize * 15 * 2));
                                break;
                        }

                        if (fileLength != nIMAdiskSize)
                        {
                            if (nDSKDiskSize == (fileLength & 0xFFFFFF00))
                            {
                                ff = fileformat.fileformat_FLEX;
                                SectorBias = 1;
                                m_lPartitionBias = 0;

                                currentFileFileFormat = ff;

                                GetFLEXGeometry();
                            }
                            else if (nIMAdiskSize == (fileLength & 0xFFFFFF00))
                            {
                                ff = fileformat.fileformat_FLEX_IMA;
                                SectorBias = 1;
                                m_lPartitionBias = 0;

                                currentFileFileFormat = ff;
                            }
                            else
                            {
                                // not OS-9 or FLEX, see if this image conforms to a valid UniFLEX diskette image

                        /* 
                         * These are the 8" geometries for UniFLEX images.
                         * 
                         *    315,392 bytes       77 tracks, single-side, sector size: 512, sectors/track: 8
                         *    630.784 bytes       77 tracks, dual-sided,  sector size: 512, sectors/track: 8
                         *  1,261,568 bytes       77 tracks, dual-sided,  sector size: 512, sectors/track: 16.
                         *
                         */

                        UniFLEX_SIR drive_SIR = ReadUNIFLEX_SIR(fs);

                                uint nFDNSize = ConvertToInt16(drive_SIR.m_sszfdn);
                                uint nVolumeSize = ConvertToInt24(drive_SIR.m_ssizfr);
                                uint nSwapSize = ConvertToInt16(drive_SIR.m_sswpsz);

                                nDSKDiskSize = (nVolumeSize + nSwapSize + 1) * 512;
                                if (nDSKDiskSize == (fileLength & 0xFFFFFF00) || ((nVolumeSize + nSwapSize) * 512) == (fileLength & 0xFFFFFF00))
                                {
                                    ff = fileformat.fileformat_UniFLEX;
                                    currentFileFileFormat = ff;
                                }
                                else
                                {
                                    // see if this an IDE drive with multiple partitions

                                    if ((fileLength % 256) > 0)
                                    {
                                        // could be - get the drive info and see if it makes sense

                                        byte[] cInfoSize = new byte[2];
                                        uint nInfoSize = 0;

                                        fs.Seek(-2, SeekOrigin.End);            // (fs, -2, SEEK_END);
                                        fs.Read(cInfoSize, 0, 2);               // fread (cInfoSize, 1, 2, fs);

                                        nInfoSize = ConvertToInt16(cInfoSize);
                                        if (nInfoSize == (fileLength % 256))
                                        {
                                            // Not FLEX, OS-9 or UniFLEX, assume it is FLEX IDE (multiple partitions) format used on
                                            // the driver for the PIA IDE interface board if we get this far.

                                            ff = fileformat.fileformat_FLEX_IDE;
                                            SectorBias = 0;
                                            m_lPartitionBias = 0;

                                            currentFileFileFormat = ff;
                                        }
                                    }
                                    else
                                    {
                                        // see if this is a minix floppy
                                        //
                                        //      read the two bytes at 0x0410 - if it is 0x13, 0x7F - this is probably a MINIX diskette image

                                        byte[] magic = new byte[2];
                                        fs.Seek(0x410, SeekOrigin.Begin);       // seek to magic word
                                        fs.Read(magic, 0, 2);                   // get magic bytes

                                        if (magic[0] == 0x13 && magic[1] == 0x7F)
                                        {
                                            sectorSize = 512;

                                            ff = fileformat.fileformat_MINIX_68K;       // big endian
                                            currentFileFileFormat = ff;
                                        }
                                        else if (magic[1] == 0x13 && magic[0] == 0x7F)
                                        {
                                            sectorSize = 512;

                                            ff = fileformat.fileformat_MINIX_IBM;       // little endian
                                            currentFileFileFormat = ff;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            // this is already an IMA disk image (probably 8" for DMAF controller

                            ff = fileformat.fileformat_FLEX_IMA;
                            SectorBias = 1;
                            m_lPartitionBias = 0;

                            currentFileFileFormat = ff;
                        }
                        fs.Seek(currentPosition, SeekOrigin.Begin);
                    }
                }

                fs.Seek(currentPosition, SeekOrigin.Begin);
            }

            m_nCurrentFileFormat = ff;

            return ff;
        }

        #region Used to handle FLEX images

        #region functions to support HIER MakeDir

        //  called by the main dialog to create an HIER directory below the parent passed in
        //  as DIR_ENTRY. This will be the tag for the currently selected directory in the 
        //  list view.
        //
        //      operations will be performed on the currently loaded drive

        public void MakeHIERDirectory(DIR_ENTRY dirEntry)
        {
            // First make sure that we have at least 4 sectors remaining to create the directory
            // with 4 sectors allocated to it.
        }

        /*
        **      get_current - get the current directory pointer from sir
        */

        private void GetCurrentHIERDirectoryPointer ()
        {

        }

        /*
        **      open_file ()
        **
        **      opens the main work file note: extension forced to 
        **      upper case.
        */

        private void OpenHIERFile ()
        {

        }

        /*
        **      copy pointer to current directory into fcb
        */

        private void CopyHIERPointer()
        {

        }

        /*
        **      copy in directory name into new directory
        */

        private void CopyHIERName ()
        {

        }

        /*
        **      pad - pad out directory to four sectors
        */

        private void PadHIERDirectory ()
        {

        }

        #endregion

        public long CalcFileOffset(int nMaxSector, byte cTrack, byte cSector)
        {
            //if (CurrentFileFormat == fileformat.fileformat_FLEX_IDE)
            //    return (((cTrack * (nMaxSector + 1)) + (cSector - SectorBias)) * 256) + m_lPartitionBias;
            //else
            //    return (((cTrack * nMaxSector) + (cSector - SectorBias)) * 256) + m_lPartitionBias;
            long offset = 0;
            switch (CurrentFileFormat)
            {
                case fileformat.fileformat_FLEX_IDE:
                    offset = (((cTrack * (nMaxSector + 1)) + (cSector - SectorBias)) * sectorSize) + m_lPartitionBias;
                    break;

                case fileformat.fileformat_FLEX_IMA:
                    if (cTrack == 0)
                    {
                        // these are ALWAYS single density sectors for IMA format

                        offset = ((cSector - SectorBias) * sectorSize) + m_lPartitionBias;
                    }
                    else
                    {
                        // if this image is  formatted as an IMA image, then track zero will have
                        // either 10 or 20 sectors depending on whether it is single or double
                        // sided. We can tell if it is single or double sided if the maximum
                        // number of sectors is > 1
                        //
                        //  valid IMA formats:
                        //
                        //  tracks      sides   density     sectors/track   filesize                     blank filename
                        //  --------    -----   -------     -------------   --------------------------   --------------
                        //  35          1       single      10              35 * 1 * 10 * 256 =   89600  SSSD35T.IMA
                        //  35          2       single      20              35 * 2 * 10 * 256 =  179200  DSSD35T.IMA
                        //  35          1       double      18       2560 + 34 * 1 * 18 * 256 =  159232  SSDD35T.IMA
                        //  35          2       double      36       5120 + 34 * 2 * 18 * 256 =  318464  DSDD35T.IMA
                        //  40          1       single      10              40 * 1 * 10 * 256 =  102400  SSSD40T.IMA
                        //  40          2       single      20              40 * 2 * 10 * 256 =  204800  DSSD40T.IMA <-
                        //  40          1       double      18       2560 + 39 * 1 * 18 * 256 =  182272  SSDD40T.IMA
                        //  40          2       double      36       5120 + 39 * 2 * 18 * 256 =  364544  DSDD40T.IMA
                        //  80          1       single      10              80 * 1 * 10 * 256 =  204800  SSSD80T.IMA <-
                        //  80          2       single      20              80 * 2 * 10 * 256 =  409600  DSSD80T.IMA
                        //  80          1       double      18       2560 + 79 * 1 * 18 * 256 =  366592  SSDD80T.IMA
                        //  80          2       double      36       5120 + 79 * 2 * 18 * 256 =  733184  DSDD80T.IMA
                        //  77          1       single      15              77 * 1 * 15 * 256 =  295680  SSSD77T.IMA 8" SSSD
                        //  77          2       double      52       7680 + 77 * 2 * 52 * 256 = 2057728  DSDD77T.IMA 8" DSDD 52 sectors per track
                        // 
                        //      Since these are the only valid IMA formats - we can assume double sided if the max sectors for
                        //  this images is > 18 (because sectors are 1 based not 0)
                        //
                        //  80 track single sides single density is the same size as 40 track double sided single density. The 
                        //  only way to tell the difference is by the max sectors per track. If it's 10 - this is single sided
                        //  single density 80 track otherwise it is double single density sided 40 track

                        switch (nMaxSector)
                        {
                            // these are the only valid max sector values for IMA (10, 15 or 18 = single side, 20, 30 or 36 = double sided)

                            case 10:
                                offset = ((((cTrack - 1) * (nMaxSector)) + (cSector - SectorBias)) * sectorSize) + m_lPartitionBias + (sectorSize * 10);
                                break;
                            case 20:
                                offset = ((((cTrack - 1) * (nMaxSector)) + (cSector - SectorBias)) * sectorSize) + m_lPartitionBias + (sectorSize * 20);
                                break;
                            case 18:
                                offset = ((((cTrack - 1) * (nMaxSector)) + (cSector - SectorBias)) * sectorSize) + m_lPartitionBias + (sectorSize * 10);
                                break;
                            case 36:
                                offset = ((((cTrack - 1) * (nMaxSector)) + (cSector - SectorBias)) * sectorSize) + m_lPartitionBias + (sectorSize * 20);
                                break;

                            // 8" images

                            case 15:    // SSSD
                                offset = ((((cTrack - 1) * (nMaxSector)) + (cSector - SectorBias)) * sectorSize) + m_lPartitionBias + (sectorSize * 15);
                                break;
                            case 26:    // SSDD
                                offset = ((((cTrack - 1) * (nMaxSector)) + (cSector - SectorBias)) * sectorSize) + m_lPartitionBias + (sectorSize * 15);
                                break;
                            case 30:    // DSSD
                                offset = ((((cTrack - 1) * (nMaxSector)) + (cSector - SectorBias)) * sectorSize) + m_lPartitionBias + (sectorSize * 30);
                                break;
                            case 52:    // DSDD
                                offset = ((((cTrack - 1) * (nMaxSector)) + (cSector - SectorBias)) * sectorSize) + m_lPartitionBias + (sectorSize * 30);
                                break;
                        }
                    }
                    break;

                case fileformat.fileformat_OS9:
                    offset = (((cTrack * nMaxSector) + (cSector - SectorBias)) * sectorSize) + m_lPartitionBias;
                    break;

                case fileformat.fileformat_OS9_IMA:
                    offset = (((cTrack * nMaxSector) + (cSector - SectorBias)) * sectorSize) + m_lPartitionBias;
                    break;

                default:
                    offset = (((cTrack * nMaxSector) + (cSector - SectorBias)) * sectorSize) + m_lPartitionBias;
                    break;
            }

            return offset;
        }

        public byte[] ReadFLEX_SECTOR(FileStream fs, byte track, byte sector, bool restorePosition)
        {
            byte[] sectorData = new byte[256];

            long currentPosition = fs.Position;

            RAW_SIR stSystemInformationRecord = ReadRAW_FLEX_SIR();
            long offset = CalcFileOffset(stSystemInformationRecord.cMaxSector, track, sector);

            if (restorePosition)
                fs.Seek(currentPosition, SeekOrigin.Begin);

            fs.Seek(offset, SeekOrigin.Begin);
            fs.Read(sectorData, 0, 256);

            return sectorData;
        }

        public void WriteFLEX_SECTOR(FileStream fs, byte track, byte sector, byte[] sectorData, bool restorePosition)
        {
            long currentPosition = fs.Position;

            RAW_SIR stSystemInformationRecord = ReadRAW_FLEX_SIR();
            long offset = CalcFileOffset(stSystemInformationRecord.cMaxSector, track, sector);

            fs.Seek(offset, SeekOrigin.Begin);
            fs.Write(sectorData, 0, 256);

            if (restorePosition)
                fs.Seek(currentPosition, SeekOrigin.Begin);
        }

        public RAW_DIR_SECTOR ReadFLEX_DIR_SECTOR (FileStream fs, byte track, byte sector, bool restorePosition)
        {
            long currentPosition = fs.Position;

            RAW_SIR stSystemInformationRecord = ReadRAW_FLEX_SIR();
            long offset = CalcFileOffset(stSystemInformationRecord.cMaxSector, track, sector);

            RAW_DIR_SECTOR rawDirSector = new RAW_DIR_SECTOR();

            fs.Seek(offset + 4, SeekOrigin.Begin);      // position to parent track linkage byte

            rawDirSector.header = new FLEX_HIER_DIR_HEADER();

            rawDirSector.header.parentTrack  = (byte)fs.ReadByte();
            rawDirSector.header.parentSector = (byte)fs.ReadByte();
            fs.Read(rawDirSector.header.name, 0, 8);
            fs.Read(rawDirSector.header.unused, 0, 2);

            for (int i = 0; i < 10; i++)
            {
                rawDirSector.dirEntries[i] = new DIR_ENTRY();

                fs.Read(rawDirSector.dirEntries[i].caFileName, 0, 8);
                fs.Read(rawDirSector.dirEntries[i].caFileExtension, 0, 3);

                rawDirSector.dirEntries[i].cAttributes = (byte)fs.ReadByte();
                rawDirSector.dirEntries[i].cUnused1 = (byte)fs.ReadByte();
                rawDirSector.dirEntries[i].cStartTrack = (byte)fs.ReadByte();
                rawDirSector.dirEntries[i].cStartSector = (byte)fs.ReadByte();
                rawDirSector.dirEntries[i].cEndTrack = (byte)fs.ReadByte();
                rawDirSector.dirEntries[i].cEndSector = (byte)fs.ReadByte();
                rawDirSector.dirEntries[i].cTotalSectorsHi = (byte)fs.ReadByte();
                rawDirSector.dirEntries[i].cTotalSectorsLo = (byte)fs.ReadByte();
                rawDirSector.dirEntries[i].cRandomFileInd = (byte)fs.ReadByte();
                rawDirSector.dirEntries[i].cUnused2 = (byte)fs.ReadByte();
                rawDirSector.dirEntries[i].cMonth = (byte)fs.ReadByte();
                rawDirSector.dirEntries[i].cDay = (byte)fs.ReadByte();
                rawDirSector.dirEntries[i].cYear = (byte)fs.ReadByte();
            }

            if (restorePosition)
                fs.Seek(currentPosition, SeekOrigin.Begin);

            return rawDirSector;
        }

        public DIR_ENTRY ReadFLEX_DIR_ENTRY(FileStream fs, bool restorePosition)
        {
            long currentPosition = fs.Position;

            DIR_ENTRY dirEntry = new DIR_ENTRY();

            fs.Read(dirEntry.caFileName, 0, 8);
            fs.Read(dirEntry.caFileExtension, 0, 3);

            dirEntry.cAttributes = (byte)fs.ReadByte();
            dirEntry.cUnused1 = (byte)fs.ReadByte();
            dirEntry.cStartTrack = (byte)fs.ReadByte();
            dirEntry.cStartSector = (byte)fs.ReadByte();
            dirEntry.cEndTrack = (byte)fs.ReadByte();
            dirEntry.cEndSector = (byte)fs.ReadByte();
            dirEntry.cTotalSectorsHi = (byte)fs.ReadByte();
            dirEntry.cTotalSectorsLo = (byte)fs.ReadByte();
            dirEntry.cRandomFileInd = (byte)fs.ReadByte();
            dirEntry.cUnused2 = (byte)fs.ReadByte();
            dirEntry.cMonth = (byte)fs.ReadByte();
            dirEntry.cDay = (byte)fs.ReadByte();
            dirEntry.cYear = (byte)fs.ReadByte();

            if (restorePosition)
                fs.Seek(currentPosition, SeekOrigin.Begin);

            return dirEntry;
        }

        public void WriteFLEX_DIR_ENTRY(FileStream fs, DIR_ENTRY dirEntry, bool restorePosition)
        {
            // we are actually pointing at the empty or new directory entry that caused
            // us to stop looking for a file in the disectory or that caused us to stop
            // searching for an empty one because we found it.

            long currentPosition = fs.Position;

            // GetFileFormat does not change the Position in fs.

            GetFileFormat();

            fs.Write(dirEntry.caFileName, 0, 8);
            fs.Write(dirEntry.caFileExtension, 0, 3);

            fs.WriteByte(dirEntry.cAttributes);
            fs.WriteByte(dirEntry.cUnused1);
            fs.WriteByte(dirEntry.cStartTrack);
            fs.WriteByte(dirEntry.cStartSector);
            fs.WriteByte(dirEntry.cEndTrack);
            fs.WriteByte(dirEntry.cEndSector);
            fs.WriteByte(dirEntry.cTotalSectorsHi);
            fs.WriteByte(dirEntry.cTotalSectorsLo);
            fs.WriteByte(dirEntry.cRandomFileInd);
            fs.WriteByte(dirEntry.cUnused2);
            fs.WriteByte(dirEntry.cMonth);
            fs.WriteByte(dirEntry.cDay);
            fs.WriteByte(dirEntry.cYear);

            if (restorePosition)
                fs.Seek(currentPosition, SeekOrigin.Begin);

        }

        public RAW_SIR ReadRAW_FLEX_SIR()
        {
            currentlyOpenedImageFileStream.Seek(PartitionBias + 0x0310 - (sectorSize * SectorBias), SeekOrigin.Begin);
            return ReadRAW_FLEX_SIR(currentlyOpenedImageFileStream);
        }

        public RAW_SIR ReadRAW_FLEX_SIR(FileStream fs)
        {
            long currentPosition = fs.Position;

            RAW_SIR systemInformationRecord = new RAW_SIR();

            if (m_lPartitionBias >= 0)
                fs.Seek(m_lPartitionBias + 0x0310 - (sectorSize * SectorBias), SeekOrigin.Begin);     // fseek(m_fp, m_lPartitionBias + 0x0310 - (sectorSize * m_nSectorBias), SEEK_SET);
            else
                fs.Seek(0x0210, SeekOrigin.Begin);                                               // fseek (fs, 0x0210, SEEK_SET);

            fs.Read(systemInformationRecord.caVolumeLabel, 0, 11);                              // fread (stSystemInformationRecord.caVolumeLabel      , 1, sizeof (stSystemInformationRecord.caVolumeLabel), fs);  // $50 - $5A
            systemInformationRecord.cVolumeNumberHi = (byte)fs.ReadByte();                      // fread (&stSystemInformationRecord.cVolumeNumberHi   , 1, 1, fs);      // $5B
            systemInformationRecord.cVolumeNumberLo = (byte)fs.ReadByte();                      // fread (&stSystemInformationRecord.cVolumeNumberLo   , 1, 1, fs);      // $5C
            systemInformationRecord.cFirstUserTrack = (byte)fs.ReadByte();                      // fread (&stSystemInformationRecord.cFirstUserTrack   , 1, 1, fs);      // $5D
            systemInformationRecord.cFirstUserSector = (byte)fs.ReadByte();                     // fread (&stSystemInformationRecord.cFirstUserSector  , 1, 1, fs);      // $5E
            systemInformationRecord.cLastUserTrack = (byte)fs.ReadByte();                       // fread (&stSystemInformationRecord.cLastUserTrack    , 1, 1, fs);      // $5F
            systemInformationRecord.cLastUserSector = (byte)fs.ReadByte();                      // fread (&stSystemInformationRecord.cLastUserSector   , 1, 1, fs);      // $60
            systemInformationRecord.cTotalSectorsHi = (byte)fs.ReadByte();                      // fread (&stSystemInformationRecord.cTotalSectorsHi   , 1, 1, fs);      // $61
            systemInformationRecord.cTotalSectorsLo = (byte)fs.ReadByte();                      // fread (&stSystemInformationRecord.cTotalSectorsLo   , 1, 1, fs);      // $62
            systemInformationRecord.cMonth = (byte)fs.ReadByte();                               // fread (&stSystemInformationRecord.cMonth            , 1, 1, fs);      // $63
            systemInformationRecord.cDay = (byte)fs.ReadByte();                                 // fread (&stSystemInformationRecord.cDay              , 1, 1, fs);      // $64
            systemInformationRecord.cYear = (byte)fs.ReadByte();                                // fread (&stSystemInformationRecord.cYear             , 1, 1, fs);      // $65
            systemInformationRecord.cMaxTrack = (byte)fs.ReadByte();                            // fread (&stSystemInformationRecord.cMaxTrack         , 1, 1, fs);      // $66
            systemInformationRecord.cMaxSector = (byte)fs.ReadByte();                           // fread (&stSystemInformationRecord.cMaxSector        , 1, 1, fs);      // $67

            fs.Seek(currentPosition, SeekOrigin.Begin);

            return systemInformationRecord;
        }

        /// <summary>
        /// update the FLEX System Information Record whenever we write a file to the disk or delete a file from the disk.
        /// this is done to update the information that reflects the number of sectors remaining on the disk and the new 
        /// free chain start and end locations
        /// </summary>
        /// <param name="fs"></param>
        /// <param name="systemInformationRecord"></param>
        /// <param name="restorePosition"></param>
        public void WriteRAW_FLEX_SIR(FileStream fs, RAW_SIR systemInformationRecord, bool restorePosition)
        {
            GetFileFormat();

            long currentPosition = fs.Position;

            if (m_lPartitionBias >= 0)
                fs.Seek(m_lPartitionBias + 0x0310 - (sectorSize * SectorBias), SeekOrigin.Begin);     // fseek(m_fp, m_lPartitionBias + 0x0310 - (sectorSize * m_nSectorBias), SEEK_SET);
            else
                fs.Seek(0x0210, SeekOrigin.Begin);                                                  // fseek (fs, 0x0210, SEEK_SET);

            fs.Write(systemInformationRecord.caVolumeLabel, 0, 11);
            fs.WriteByte(systemInformationRecord.cVolumeNumberHi);
            fs.WriteByte(systemInformationRecord.cVolumeNumberLo);
            fs.WriteByte(systemInformationRecord.cFirstUserTrack);
            fs.WriteByte(systemInformationRecord.cFirstUserSector);
            fs.WriteByte(systemInformationRecord.cLastUserTrack);
            fs.WriteByte(systemInformationRecord.cLastUserSector);
            fs.WriteByte(systemInformationRecord.cTotalSectorsHi);
            fs.WriteByte(systemInformationRecord.cTotalSectorsLo);
            fs.WriteByte(systemInformationRecord.cMonth);
            fs.WriteByte(systemInformationRecord.cDay);
            fs.WriteByte(systemInformationRecord.cYear);
            fs.WriteByte(systemInformationRecord.cMaxTrack);
            fs.WriteByte(systemInformationRecord.cMaxSector);

            if (restorePosition)
                fs.Seek(currentPosition, SeekOrigin.Begin);
        }

        public bool DeleteFLEXFile(RAW_SIR stSystemInformationRecord, DIR_ENTRY stDirEntry, int nMaxSector, bool restorePosition)
        {
            GetFileFormat();

            bool fileExists = true;

            try
            {
                long currentPosition = CurrentPosition;

                byte nFileStartTrack;
                byte nFileStartSector;
                byte nFileEndTrack;
                byte nFileEndSector;

                byte nFirstUserTrack;
                byte nFirstUserSector;
                byte nLastUserTrack;
                byte nLastUserSector;

                byte[] caLastUserSector = new byte[sectorSize];

                int nFileTotalSectors;

                // We must delete first. Do this by marking the first char of the filename, and linking it's
                // sectors back into the free chain. Don't forget to add the sector files count to the total
                // remaining sectors

                // back up the file position to the dir entry we are going to delete

                currentlyOpenedImageFileStream.Seek(-sizeofDirEntry, SeekOrigin.Current);       // fseek(m_fp, 0L - (long)sizeof(stDirEntry), SEEK_CUR);
                stDirEntry.caFileName[0] = 0xFF;
                WriteFLEX_DIR_ENTRY(currentlyOpenedImageFileStream, stDirEntry, false);         // fwrite(&stDirEntry, 1, sizeof(stDirEntry), m_fp);

                // get the linkage info we will need to put the sectors that this file owned back into the free chain.

                nFileStartTrack     = stDirEntry.cStartTrack;                                   // where the sectors start for this file
                nFileStartSector    = stDirEntry.cStartSector;
                nFileEndTrack       = stDirEntry.cEndTrack;                                     // where they end for this file
                nFileEndSector      = stDirEntry.cEndSector;

                nFirstUserTrack     = stSystemInformationRecord.cFirstUserTrack;                // the track that contains the first sector of the free chain
                nFirstUserSector    = stSystemInformationRecord.cFirstUserSector;               // the sector where the free chain starts
                nLastUserTrack      = stSystemInformationRecord.cLastUserTrack;                 // the track that contains the last sector of thr free chain
                nLastUserSector     = stSystemInformationRecord.cLastUserSector;                // the last sector of the free chain

                // Link this file's sector list into the free chain
                //
                //  start by positioning the file position to the sector that is the last sector of the free chain and read it into memory

                long lOffset = CalcFileOffset(nMaxSector, nLastUserTrack, nLastUserSector);     // lOffset = ((nLastUserTrack * nMaxSector) + (nLastUserSector - 1)) * sectorSize; <- was already commented out
                currentlyOpenedImageFileStream.Seek(lOffset, SeekOrigin.Begin);                 // fseek(m_fp, lOffset, SEEK_SET);
                currentlyOpenedImageFileStream.Read(caLastUserSector, 0, sectorSize);           // fread(caLastUserSector, sectorSize, 1, m_fp);

                // now we need to change the linkage to point to the first track/sector of the file we are deleting

                caLastUserSector[0] = stDirEntry.cStartTrack;
                caLastUserSector[1] = stDirEntry.cStartSector;

                // now position back to the last track/sector of the free chain and write the modified sector out

                currentlyOpenedImageFileStream.Seek(lOffset, SeekOrigin.Begin);                 // lOffset still points to the sector that is the last sector of the free chain
                currentlyOpenedImageFileStream.Write(caLastUserSector, 0, sectorSize);          // fwrite(caLastUserSector, sectorSize, 1, m_fp);

                // update the remaining free sectors count. the dir entry has the total sector count for the file we are deleting

                nFileTotalSectors = (int)(stDirEntry.cTotalSectorsHi * sectorSize + stDirEntry.cTotalSectorsLo);

                m_nTotalSectors = (int)(stSystemInformationRecord.cTotalSectorsHi * 256 + stSystemInformationRecord.cTotalSectorsLo);
                m_nTotalSectors += nFileTotalSectors;

                // Update the SIR

                stSystemInformationRecord.cTotalSectorsHi = (byte)(m_nTotalSectors / 256);
                stSystemInformationRecord.cTotalSectorsLo = (byte)(m_nTotalSectors % 256);
                stSystemInformationRecord.cLastUserTrack = stDirEntry.cEndTrack;
                stSystemInformationRecord.cLastUserSector = stDirEntry.cEndSector;

                currentlyOpenedImageFileStream.Seek(m_lPartitionBias + 0x0310 - (sectorSize * SectorBias), SeekOrigin.Begin);   // fseek(m_fp, m_lPartitionBias + 0x0310 - (sectorSize * m_nSectorBias), SEEK_SET);
                WriteRAW_FLEX_SIR(currentlyOpenedImageFileStream, stSystemInformationRecord, false);                               // fwrite(&stSystemInformationRecord, sizeof(stSystemInformationRecord), 1, m_fp);

                // Now unlink the now current last user sector by setting it's linkage bytes to 0x00 0x00

                nLastUserTrack = stSystemInformationRecord.cLastUserTrack;
                nLastUserSector = stSystemInformationRecord.cLastUserSector;

                lOffset = CalcFileOffset(nMaxSector, nLastUserTrack, nLastUserSector);              // get the offset to the sector we are going to update
                currentlyOpenedImageFileStream.Seek(lOffset, SeekOrigin.Begin);                     // position to it
                currentlyOpenedImageFileStream.Read(caLastUserSector, 0, sectorSize);               // read it in
                caLastUserSector[0] = 0x00;                                                         // set linkage as the last sector
                caLastUserSector[1] = 0x00;
                currentlyOpenedImageFileStream.Seek(lOffset, SeekOrigin.Begin);                     // position back to this sector in the buffer
                currentlyOpenedImageFileStream.Write(caLastUserSector, 0, sectorSize);              // update the sector

                currentlyOpenedImageFileStream.Flush();                                             // flush change to disk

                fileExists = false;

                if (restorePosition)
                    currentlyOpenedImageFileStream.Seek(currentPosition, SeekOrigin.Begin);
            }
            catch
            {
                MessageBox.Show("Unable to delete file.");
            }

            return fileExists;
        }

        public List<DIR_ENTRY> GetFLEXDirectoryList(RAW_SIR stSystemInformationRecord)
        {
            List<DIR_ENTRY> directoryEntries = new List<DIR_ENTRY>();

            bool nDirectoryEnd = false;
            bool nFileExists = false;

            byte cNextDirTrack = 0;
            byte cNextDirSector = 5;

            byte[] caDirHeader = new byte[16];

            byte[] szFileTitle = new byte[8];
            byte[] szFileExt = new byte[3];

            int nMaxSector = (int)stSystemInformationRecord.cMaxSector;
            m_nTotalSectors = (int)(stSystemInformationRecord.cTotalSectorsHi * 256 + stSystemInformationRecord.cTotalSectorsLo);

            Array.Clear(szTargetFileTitle, 0, szTargetFileTitle.Length);
            Array.Clear(szTargetFileExt, 0, szTargetFileExt.Length);

            DIR_ENTRY stDirEntry = new DIR_ENTRY();

            for (cNextDirTrack = 0, cNextDirSector = 5; !nDirectoryEnd && !nFileExists;)
            {
                // if the sector in the linkage track and sector is 0 - this is the last sector in the directory - processint and then leave

                if (cNextDirSector == 0)
                    nDirectoryEnd = true;

                if (!nDirectoryEnd)
                {
                    lOffset = CalcFileOffset(nMaxSector, cNextDirTrack, cNextDirSector);       // lOffset = ((cNextDirTrack * nMaxSector) + (cNextDirSector - 1)) * 256; <- already commented out
                    currentlyOpenedImageFileStream.Seek(lOffset, SeekOrigin.Begin);                                      // fseek (m_fp, lOffset, SEEK_SET);
                    currentlyOpenedImageFileStream.Read(caDirHeader, 0, caDirHeader.Length);                             // fread (&caDirHeader, 1, sizeof (caDirHeader), m_fp);
                    cNextDirTrack = caDirHeader[0];
                    cNextDirSector = caDirHeader[1];

                    lCurrentDirOffset = lOffset + caDirHeader.Length;

                    for (int i = 0; i < 10; i++)
                    {
                        lCurrentDirOffset = currentlyOpenedImageFileStream.Position;                                     // lCurrentDirOffset = ftell (m_fp);
                        stDirEntry = ReadFLEX_DIR_ENTRY(currentlyOpenedImageFileStream, false);                               // fread (&stDirEntry,  1, sizeof (stDirEntry), m_fp);
                        if (stDirEntry.caFileName[0] != '\0')
                        {
                            if ((stDirEntry.caFileName[0] & 0x80) != 0x80)
                            {
                                // add this entry to the list

                                directoryEntries.Add(stDirEntry);
                            }
                        }
                        else
                        {
                            nDirectoryEnd = true;
                            break;
                        }
                    }
                }
            }

            return directoryEntries;
        }

        public DIR_ENTRY FindFLEXDirEntry(RAW_SIR stSystemInformationRecord, string cSourceFileTitle, string cSourceFileExt, out bool fileFound)
        {
            return FindFLEXDirEntry(currentlyOpenedImageFileStream, stSystemInformationRecord, cSourceFileTitle, cSourceFileExt, out fileFound);
        }

        /// <summary>
        /// Find the directory entry for the file specified in cSourceFileTitle.cSourceFileExt returning the DIR_ENTRY and setting the
        /// output parameter to true or false based on whether or not we found the file.
        /// </summary>
        /// <param name="m_fp"></param>
        /// <param name="stSystemInformationRecord"></param>
        /// <param name="cSourceFileTitle"></param>
        /// <param name="cSourceFileExt"></param>
        /// <param name="fileFound"></param>
        /// <returns></returns>
        public DIR_ENTRY FindFLEXDirEntry(FileStream m_fp, RAW_SIR stSystemInformationRecord, string cSourceFileTitle, string cSourceFileExt, out bool fileFound)
        {
            bool nDirectoryEnd = false;
            bool nFileExists = false;

            byte cNextDirTrack = 0;
            byte cNextDirSector = 5;

            byte[] caDirHeader = new byte[16];

            byte[] szFileTitle = new byte[8];
            byte[] szFileExt = new byte[3];

            int nMaxSector = (int)stSystemInformationRecord.cMaxSector;
            m_nTotalSectors = (int)(stSystemInformationRecord.cTotalSectorsHi * 256 + stSystemInformationRecord.cTotalSectorsLo);

            Array.Clear(szTargetFileTitle, 0, szTargetFileTitle.Length);
            Array.Clear(szTargetFileExt, 0, szTargetFileExt.Length);

            ASCIIEncoding.ASCII.GetBytes(cSourceFileTitle, 0, cSourceFileTitle.Length, szTargetFileTitle, 0);
            ASCIIEncoding.ASCII.GetBytes(cSourceFileExt, 0, cSourceFileExt.Length, szTargetFileExt, 0);

            DIR_ENTRY stDirEntry = new DIR_ENTRY();

            for (cNextDirTrack = 0, cNextDirSector = 5; !nDirectoryEnd && !nFileExists;)
            {
                // if the sector in the linkage track and sector is 0 - this is the last sector in the directory - processint and then leave

                if (cNextDirSector == 0)
                    nDirectoryEnd = true;

                if (!nDirectoryEnd)
                {
                    lOffset = CalcFileOffset(nMaxSector, cNextDirTrack, cNextDirSector);       // lOffset = ((cNextDirTrack * nMaxSector) + (cNextDirSector - 1)) * 256; <- already commented out
                    m_fp.Seek(lOffset, SeekOrigin.Begin);                                      // fseek (m_fp, lOffset, SEEK_SET);
                    m_fp.Read(caDirHeader, 0, caDirHeader.Length);                             // fread (&caDirHeader, 1, sizeof (caDirHeader), m_fp);
                    cNextDirTrack = caDirHeader[0];
                    cNextDirSector = caDirHeader[1];

                    lCurrentDirOffset = lOffset + caDirHeader.Length;

                    for (int i = 0; i < 10; i++)
                    {
                        lCurrentDirOffset = m_fp.Position;                                     // lCurrentDirOffset = ftell (m_fp);
                        stDirEntry = ReadFLEX_DIR_ENTRY(m_fp, false);                               // fread (&stDirEntry,  1, sizeof (stDirEntry), m_fp);
                        if (stDirEntry.caFileName[0] != '\0')
                        {
                            if ((stDirEntry.caFileName[0] & 0x80) != 0x80)
                            {
                                // We are now pointing at a Directory Entry

                                for (int fnIndex = 0; fnIndex < 8; fnIndex++) szFileTitle[fnIndex] = stDirEntry.caFileName[fnIndex];
                                for (int extIndex = 0; extIndex < 3; extIndex++) szFileExt[extIndex] = stDirEntry.caFileExtension[extIndex];

                                if (ASCIIEncoding.ASCII.GetString(szFileTitle) == ASCIIEncoding.ASCII.GetString(szTargetFileTitle) && ASCIIEncoding.ASCII.GetString(szFileExt) == ASCIIEncoding.ASCII.GetString(szTargetFileExt))
                                {
                                    // Found File's Directory Entry

                                    nFileExists = true;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            nDirectoryEnd = true;
                            break;
                        }
                    }
                }
            }

            fileFound = nFileExists;

            return stDirEntry;
        }

        public class LinkageTrackSector
        {
            public byte track;
            public byte sector;
        }

        public List<LinkageTrackSector> freeChain = new List<LinkageTrackSector>();

        /// <summary>
        /// calculate how many bytes are left in the free chain
        /// </summary>
        /// <param name="m_fp"></param>
        /// <returns></returns>
        /// 
        ///     A byproduct of calculating the number of bytes left on the disk is to build a list of track/sector combinations in the free chain
        ///     When this function exits the freeChain public variable will be refreshed with the free chain track/sector list
        ///     
        public int DataBytesRemainingOnFLEXImage(FileStream m_fp)
        {
            int bytesRemaining = 0;
            byte[] caBuffer = new byte[sectorSize];

            RAW_SIR stSystemInformationRecord = ReadRAW_FLEX_SIR(m_fp);

            // stSystemInformationRecord.cFirstUserTrack and stSystemInformationRecord.cFirstUserSector pointot the start of the free chain

            int nUserTrack = stSystemInformationRecord.cFirstUserTrack;
            int nUserSector = stSystemInformationRecord.cFirstUserSector;

            int nMaxSector = (int)stSystemInformationRecord.cMaxSector;

            freeChain.Clear();

            // set the first entry in the free chain

            LinkageTrackSector freeChainStart = new LinkageTrackSector();
            freeChainStart.track = stSystemInformationRecord.cFirstUserTrack;
            freeChainStart.sector = stSystemInformationRecord.cFirstUserSector;
            freeChain.Add(freeChainStart);

            while ((nUserTrack > 0) && (nUserTrack < 256) && (nUserSector > 0) && (nUserSector < 256))
            {
                LinkageTrackSector freeChainEntry = new LinkageTrackSector();

                // read the sector from the diskette to get the linkage bytes

                long lOffset = CalcFileOffset(nMaxSector, (byte)nUserTrack, (byte)nUserSector);

                m_fp.Seek(lOffset, SeekOrigin.Begin);
                m_fp.Read(caBuffer, 0, 2);                  // the first two bytes are the linkage to the next sector in the free chain

                nUserTrack = caBuffer[0];
                nUserSector = caBuffer[1];

                freeChainEntry.track = caBuffer[0];
                freeChainEntry.sector = caBuffer[1];

                freeChain.Add(freeChainStart);

                bytesRemaining += 252;
            }

            return bytesRemaining;
        }

        /// <summary>
        /// Get the number of sectors for the directory on track 0
        /// </summary>
        public int DirectorySectorsOnTrackZero()
        {
            int sectors = 0;

            bool nDirectoryEnd = false;
            byte cNextDirTrack = 0;
            byte cNextDirSector = 5;

            byte[] linkageBytes = new byte[2];

            FileStream m_fp = currentlyOpenedImageFileStream;
            RAW_SIR stSystemInformationRecord = ReadRAW_FLEX_SIR();

            long currentPosition = m_fp.Position;
            int nMaxSector = (int)stSystemInformationRecord.cMaxSector;

            for (cNextDirTrack = 0, cNextDirSector = 5; !nDirectoryEnd;)
            {
                if (cNextDirTrack == 0 && cNextDirSector != 0)
                {
                    sectors++;

                    lOffset = CalcFileOffset(nMaxSector, cNextDirTrack, cNextDirSector);
                    m_fp.Seek(lOffset, SeekOrigin.Begin);

                    m_fp.Read(linkageBytes, 0, 2);

                    cNextDirTrack = linkageBytes[0];
                    cNextDirSector = linkageBytes[1];
                }
                else
                    break;
            }

            m_fp.Seek(currentPosition, SeekOrigin.Begin);

            return sectors;
        }

        public int DirectorySectorsTotal()
        {
            int sectors = 0;

            bool nDirectoryEnd = false;
            byte cNextDirTrack = 0;
            byte cNextDirSector = 5;

            byte[] linkageBytes = new byte[2];

            FileStream m_fp = currentlyOpenedImageFileStream;
            RAW_SIR stSystemInformationRecord = ReadRAW_FLEX_SIR();

            long currentPosition = m_fp.Position;
            int nMaxSector = (int)stSystemInformationRecord.cMaxSector;

            for (cNextDirTrack = 0, cNextDirSector = 5; !nDirectoryEnd;)
            {
                if (cNextDirSector != 0)
                {
                    sectors++;

                    lOffset = CalcFileOffset(nMaxSector, cNextDirTrack, cNextDirSector);
                    m_fp.Seek(lOffset, SeekOrigin.Begin);

                    m_fp.Read(linkageBytes, 0, 2);

                    cNextDirTrack = linkageBytes[0];
                    cNextDirSector = linkageBytes[1];
                }
                else
                    break;
            }

            m_fp.Seek(currentPosition, SeekOrigin.Begin);

            return sectors;
        }

        /// <summary>
        /// 
        /// See if the filename already exists on the virtual floppy image
        /// 
        /// </summary>
        /// <param name="m_fp"></param>
        /// <param name="stSystemInformationRecord"></param>
        /// <param name="cSourceFileTitle"></param>
        /// <param name="cSourceFileExt"></param>
        /// <returns></returns>
        public bool CheckIfFLEXFileExists(FileStream m_fp, RAW_SIR stSystemInformationRecord, string cSourceFileTitle, string cSourceFileExt)
        {
            DialogResult nAfxReturn;

            bool nDirectoryEnd = false;
            bool nFileExists = false;

            byte cNextDirTrack = 0;
            byte cNextDirSector = 5;

            byte[] caDirHeader = new byte[16];

            byte[] szFileTitle = new byte[8];
            byte[] szFileExt = new byte[3];

            int nMaxSector = (int)stSystemInformationRecord.cMaxSector;
            m_nTotalSectors = (int)(stSystemInformationRecord.cTotalSectorsHi * 256 + stSystemInformationRecord.cTotalSectorsLo);

            Array.Clear(szTargetFileTitle, 0, szTargetFileTitle.Length);
            Array.Clear(szTargetFileExt, 0, szTargetFileExt.Length);

            ASCIIEncoding.ASCII.GetBytes(cSourceFileTitle, 0, cSourceFileTitle.Length, szTargetFileTitle, 0);
            ASCIIEncoding.ASCII.GetBytes(cSourceFileExt, 0, cSourceFileExt.Length, szTargetFileExt, 0);

            // iterate through the sectors of the directory 

            for (cNextDirTrack = 0, cNextDirSector = 5; !nDirectoryEnd;)
            {
                // the end of the directory is marked with a link to sector 0. Once we reach this, we have traversed the entire directory

                // if the sector in the linkage track and sector is 0 - this is the last sector in the directory - processint and then leave

                if (cNextDirSector == 0)
                    nDirectoryEnd = true;

                if (!nDirectoryEnd)
                {
                    // not done looking yet - start by calculating the offset to the current directory sector

                    lOffset = CalcFileOffset(nMaxSector, cNextDirTrack, cNextDirSector);       // lOffset = ((cNextDirTrack * nMaxSector) + (cNextDirSector - 1)) * 256; <- already commented out
                    m_fp.Seek(lOffset, SeekOrigin.Begin);                                       // fseek (m_fp, lOffset, SEEK_SET);

                    // read in the directory header

                    m_fp.Read(caDirHeader, 0, caDirHeader.Length);                              // fread (&caDirHeader, 1, sizeof (caDirHeader), m_fp);

                    // get the linkage to the next track and sector in the directory

                    cNextDirTrack = caDirHeader[0];
                    cNextDirSector = caDirHeader[1];

                    // bump the offset to the first dir entry in this dir sector

                    lCurrentDirOffset = lOffset + caDirHeader.Length;

                    // iterate through all 10 dir entries in the sector

                    bool directorySectorIsFull = false;
                    bool foundDeletedEntry = false;

                    long firstDeletedEntryPosition = 0;

                    for (int i = 0; i < 10; i++)
                    {
                        lCurrentDirOffset = m_fp.Position;                                      // lCurrentDirOffset = ftell (m_fp);
                        stDirEntry = ReadFLEX_DIR_ENTRY(m_fp, false);                           // fread (&stDirEntry,  1, sizeof (stDirEntry), m_fp);

                        // stDirEntry now has a directory entry in it

                        if (stDirEntry.caFileName[0] != '\0')                   // unused entries are started with a 0x00
                        {
                            if ((stDirEntry.caFileName[0] & 0x80) != 0x80)      // deleted entries have the high order bit set in the first byte
                            {
                                // we are not yet at the end of the directory and this one is not deleted

                                // memset (szFileTitle, '\0', sizeof (szFileTitle));
                                // memset (szFileExt,   '\0', sizeof (szFileExt));

                                // We are now pointing at a Directory Entry - extract the filename and extension from the dir entry

                                for (int fnIndex = 0; fnIndex < 8; fnIndex++) szFileTitle[fnIndex] = stDirEntry.caFileName[fnIndex];            // memcpy (szFileTitle, stDirEntry.caFileName,      8);
                                for (int extIndex = 0; extIndex < 3; extIndex++) szFileExt[extIndex] = stDirEntry.caFileExtension[extIndex];    // memcpy (szFileExt,   stDirEntry.caFileExtension, 3);

                                // compare the filename and extension in the dir entry to the filename and extension we are trying to save.

                                if (ASCIIEncoding.ASCII.GetString(szFileTitle) == ASCIIEncoding.ASCII.GetString(szTargetFileTitle) && ASCIIEncoding.ASCII.GetString(szFileExt) == ASCIIEncoding.ASCII.GetString(szTargetFileExt))
                                {
                                    // File already exists

                                    string message = "";

                                    if (ASCIIEncoding.ASCII.GetString(szFileExt).Length == 0)
                                        message = string.Format("Do you wish to delete existing file [{0}]?", ASCIIEncoding.ASCII.GetString(szFileTitle).Replace("\0", ""));
                                    else
                                        message = string.Format("Do you wish to delete existing file [{0}.{1}]?", ASCIIEncoding.ASCII.GetString(szFileTitle).Replace("\0", ""), ASCIIEncoding.ASCII.GetString(szFileExt).Replace("\0", ""));

                                    nAfxReturn = MessageBox.Show(message, "File Already Exists", MessageBoxButtons.YesNo);
                                    if (nAfxReturn == DialogResult.Yes)
                                    {
                                        DeleteFLEXFile(stSystemInformationRecord, stDirEntry, nMaxSector, false);
                                    }
                                    else
                                    {
                                        nFileExists = true;
                                        break;
                                    }

                                    // In either case = we're done looking at the directory

                                    nDirectoryEnd = true;
                                    break;
                                }
                            }
                            else
                            {
                                // we found a deleted entry

                                if (!foundDeletedEntry)
                                {
                                    foundDeletedEntry = true;
                                    firstDeletedEntryPosition = lCurrentDirOffset;
                                }
                            }
                        }
                        else
                        {
                            nDirectoryEnd = true;
                            break;
                        }
                    }
                }
            }

            // if we get here and file is not found, we need to extend the directory because this is only called
            // when we are writing a new file to the disk. The Write file routine expects the lcurrentDirOffset
            // to be set to the next available directory entry. If we do not allocate another sector to the
            // directory the entry in the unexpanded directory will get overwritten.
            //
            //  lCurrentDirOffset % 256 will equal 0xE8 if there are no entries available in this last sector.
            //  if the directory entry filename's first character is not 0x00 then we did not fins an empty
            //  entry in the current directory - we need to expand it.

            if (cNextDirSector == 0 && nDirectoryEnd && (lCurrentDirOffset % 256) == 0xE8)
            {
                // we are pointing to the last dir entry in the last dir sector. if the file name is not
                // deleted or empty - we need to expand the directory by one sector

                if ((stDirEntry.caFileName[0] & 0x80) != 0x80 && (stDirEntry.caFileName[0] != 0x00))
                {
                    long currentDirectorySectorOffset = lCurrentDirOffset & 0xffffff00; // point to the start of the last directory sector
                    long currentPosition = m_fp.Position;

                    // get the linkage bytes from the next available sector in the free chain

                    LinkageTrackSector freeChainStart = new LinkageTrackSector();
                    freeChainStart.track = stSystemInformationRecord.cFirstUserTrack;
                    freeChainStart.sector = stSystemInformationRecord.cFirstUserSector;
                    long offsetToFreeChainSector = CalcFileOffset(nMaxSector, freeChainStart.track, freeChainStart.sector);
                    m_fp.Seek(offsetToFreeChainSector, SeekOrigin.Begin);
                    byte[] linkageBytes = new byte[2];
                    m_fp.Read(linkageBytes, 0, 2);

                    // use these bytes to link the last sector in the directory to this newly allocated sector

                    m_fp.Seek(currentDirectorySectorOffset, SeekOrigin.Begin);
                    byte[] newLinkageBytes = new byte[2];
                    newLinkageBytes[0] = stSystemInformationRecord.cFirstUserTrack;
                    newLinkageBytes[1] = stSystemInformationRecord.cFirstUserSector;
                    m_fp.Write(newLinkageBytes, 0, 2);
                    m_fp.Flush();

                    // we now have this new sector linked into the directory. Now remove it from the free chanin
                    // by setting it's linkage to 0 and updating the SIR free chain start track and sector and the
                    // number of available sectors.

                    m_fp.Seek(offsetToFreeChainSector, SeekOrigin.Begin);
                    byte[] nullLinkageBytes = new byte[2];
                    nullLinkageBytes[0] = 0x00;
                    nullLinkageBytes[1] = 0x00;
                    m_fp.Write(nullLinkageBytes, 0, 2);
                    m_fp.Flush();

                    // now update the SIR with new free chain start and end values

                    stSystemInformationRecord.cFirstUserTrack = linkageBytes[0];
                    stSystemInformationRecord.cFirstUserSector = linkageBytes[1];

                    // now update the available sectors count

                    m_nTotalSectors -= 1;

                    stSystemInformationRecord.cTotalSectorsHi = (byte)(m_nTotalSectors / 256);
                    stSystemInformationRecord.cTotalSectorsLo = (byte)(m_nTotalSectors % 256);

                    // write out the new SIR

                    WriteRAW_FLEX_SIR(m_fp, stSystemInformationRecord, true);
                    m_fp.Flush();

                    // make sure we set lCurrentDirOffset to point to the first available directory entry in this new sector

                    lCurrentDirOffset = offsetToFreeChainSector + 16;

                    // when done - restore the file poition

                    m_fp.Seek(currentPosition, SeekOrigin.Begin);
                }
            }

            return nFileExists;
        }

        private void WriteFileToFLEXImage(string dialogConfigType, string virtualFloppyFileName, byte[] fileContent, FileStream m_fp)
        {
            numberOfFileImported++;

            // TODO: Add your control notification handler code here

            byte[] caBuffer = new byte[sectorSize];

            bool nInvalidFileName;

            byte nFileStartTrack = 0;
            byte nFileStartSector = 0;
            byte nFileEndTrack = 0;
            byte nFileEndSector = 0;

            int nFileTotalSectors;

            bool nSaveFile = true;
            bool nDiskIsFull = false;
            bool nTreatAsBinary = true;

            FileStream fp = null;           //FILE *fp;           // For the file we'll be importing (SOURCE)

            bool nWarnedUser = false;
            RAW_SIR stSystemInformationRecord;

            //bool nWarnedUser = false;
            //if ((pApp->m_nCompactBinary || pApp->m_nCompressSpaces) && !nWarnedUser)
            //{
            //    DialogResult nAfxReturn;

            //    nWarnedUser = 1;

            //    nAfxReturn = AfxMessageBox 
            //        (
            //            "Binary Compaction, and Space Compression have not\r\n"
            //            "yet been implemented. If you proceed the file will\r\n"
            //            "be copied from the PC to the virtual diskette in\r\n"
            //            "252 byte blocks.\r\n"
            //            "\r\n"
            //            "                   Do you wish to continue?",
            //            MB_YESNO
            //        );

            //    if (nAfxReturn != IDYES)
            //        return;

            //}

            byte[] pszFileBuffer = new byte[8192];        //memset (pszFileBuffer, '\0', 8192);

            nInvalidFileName = true;        // until proven otherwise

            string cSourceFileTitle = "";
            string cSourceFileExt = "";

            // split the filename on the "." to separate the extension from the filename
            // so we can make sure the lengths are valid. Must be 8.3 format for FLEX

            string[] virtualFLoppyFileNameParts = virtualFloppyFileName.Split('.');
            if (virtualFLoppyFileNameParts.Length > 0)
                cSourceFileTitle = virtualFLoppyFileNameParts[0];
            if (virtualFLoppyFileNameParts.Length > 1)
                cSourceFileExt = virtualFLoppyFileNameParts[1];

            // do not leave here until a valid file name is entered or the user escapes out of the dialog.

            //while (nInvalidFileName)
            //{
            //    if (cSourceFileTitle.Length > 8 || cSourceFileExt.Length > 3 || cSourceFileTitle.Length == 0 || cSourceFileExt.Length == 0)
            //    {
            //        // Get an 8.3 filename

            //        string csDefault = string.Format("{0}.{1}", cSourceFileTitle, cSourceFileExt);       // sprintf((char*)szTargetFileName, "%.8s.%.3s", (LPCTSTR)cSourceFileTitle, (LPCTSTR)cSourceFileExt);

            //        frmDialogValidFilename pValidFileDlg = new frmDialogValidFilename(); ;
            //        pValidFileDlg.m_EditValidFilename = csDefault;
            //        DialogResult nDlgReturn = pValidFileDlg.ShowDialog();
            //        if (nDlgReturn == DialogResult.OK)
            //        {

            //            virtualFLoppyFileNameParts = pValidFileDlg.m_EditValidFilename.Split('.');
            //            if (virtualFLoppyFileNameParts.Length > 0)
            //                cSourceFileTitle = virtualFLoppyFileNameParts[0];
            //            if (virtualFLoppyFileNameParts.Length > 1)
            //                cSourceFileExt = virtualFLoppyFileNameParts[1];
            //        }
            //        else
            //            break;
            //    }
            //    else
            //        nInvalidFileName = false;
            //}

            // do we have a valid file name or did the user cancel out?

            if (!nInvalidFileName)
            {
                // get and save the current position in the virtual floppy image.

                long currentPosition = m_fp.Position;

                // We now have a valid target filename we can use

                cSourceFileTitle = cSourceFileTitle.ToUpper();
                cSourceFileExt = cSourceFileExt.ToUpper();

                if (m_fp != null)
                {
                    // getting the remaining data size remsining also rebuilds the freeChain List for this image

                    int bytesRemaining = DataBytesRemainingOnFLEXImage(m_fp);

                    if (bytesRemaining >= fileContent.Length)
                    {
                        // First get the System Information Record

                        stSystemInformationRecord = ReadRAW_FLEX_SIR(m_fp);

                        // See if the filename.ext is in use on target virtual floppy and ask to delete if it is
                        //
                        //      No else required for this if - CheckIfFLEXFileExists handles asking if the file
                        //      should be replaced. If the user answers yes to "file exists - do you want to
                        //      delete the file", the file will be deleted from the FLEX diskette and CheckIfFLEXFileExists
                        //      will return false indicating that the file does not exist. If the user selects no - the
                        //      original file is left and nothing happends and CheckIfFLEXFileExists returns true
                        //      indicating that the file already exists and the user wishes to retain it.
                        //

                        if (!CheckIfFLEXFileExists(m_fp, stSystemInformationRecord, cSourceFileTitle, cSourceFileExt))
                        {
                            // The file either didn't exist or we successfully deleted it - go on

                            // Now we can actually copy the file over and fix up
                            // the SIR and Directory;
                            //
                            //      set the start track and sector to the first track and sector of the free chain
                            //

                            nFileStartTrack = stSystemInformationRecord.cFirstUserTrack;        
                            nFileStartSector = stSystemInformationRecord.cFirstUserSector;
                            nFileTotalSectors = 0;

                            // position in virtual floppy to first user sector in the free chain

                            int nSequence = 1;
                            byte nUserTrack  = nFileStartTrack;         // stSystemInformationRecord.cFirstUserTrack;
                            byte nUserSector = nFileStartSector;        // stSystemInformationRecord.cFirstUserSector;

                            int nMaxSector = (int)stSystemInformationRecord.cMaxSector;

                            // quick sanity check on the track and sector values

                            if ((nUserTrack > 0) && (nUserSector > 0))                                      //if ((nUserTrack > 0) && (nUserTrack < 256) && (nUserSector > 0) && (nUserSector < 256))
                            {
                                // read the sector from the diskette to get the linkage bytes

                                long lOffset = CalcFileOffset(nMaxSector, nUserTrack, nUserSector);         // lOffset = ((nUserTrack * nMaxSector) + (nUserSector - 1)) * 256; <- was already commented out
                                m_fp.Seek(lOffset, SeekOrigin.Begin);                                       // fseek(m_fp, lOffset, SEEK_SET);
                                m_fp.Read(caBuffer, 0, 2);                                                  // fread(caBuffer, 1, 2, m_fp);       // get sector linkage

                                if (m_nStripLinefeed || m_nCompressSpaces)
                                {
                                    // see if this may be a binary file

                                    if (((fileContent[0] < 0x20) || (fileContent[0] > 0x7F)) && (fileContent[0] != 0x0D))
                                    {
                                        DialogResult afxRet;

                                        string cSourceFileName = m_fp.Name;
                                        string strMessage = string.Format("{0} may be a binary file", virtualFloppyFileName);

                                        afxRet = MessageBox.Show("Do you wish to treat it as such?", strMessage, MessageBoxButtons.YesNo);

                                        if (afxRet == DialogResult.No)
                                            nTreatAsBinary = false;
                                    }
                                    else
                                        nTreatAsBinary = false;
                                }

                                if (nTreatAsBinary)
                                {
                                    int srcIndex = 0;

                                    // Loop on Importing file - this version does not respect the
                                    // Space Compression, Strip Linefeeds and Binary Compaction 
                                    // option flags.

                                    Array.Clear(caBuffer, 4, 252);                                              // memset(&caBuffer[4], '\0', 252);
                                    while ((srcIndex < fileContent.Length) && !nDiskIsFull)                     // while (((nCount = fread(&caBuffer[4], 1, 252, fp)) > 0) && !nDiskIsFull)
                                    {
                                        // copy all 252 bytes at once

                                        if ((fileContent.Length - srcIndex) >= 252)
                                        {
                                            Array.Copy(fileContent, srcIndex, caBuffer, 4, 252);
                                            srcIndex += 252;
                                        }
                                        else
                                        {
                                            Array.Copy(fileContent, srcIndex, caBuffer, 4, fileContent.Length - srcIndex);
                                            srcIndex = fileContent.Length;
                                        }

                                        nFileEndTrack = nUserTrack;
                                        nFileEndSector = nUserSector;

                                        // suff the sector sequence number into the buffer. No need to mess with the sector linkage - it's already set from code above and below

                                        caBuffer[2] = (byte)(nSequence / 256);
                                        caBuffer[3] = (byte)(nSequence % 256);

                                        // position the file pointer and write the sector to the diskette image file.

                                        m_fp.Seek(lOffset, SeekOrigin.Begin);                                   // fseek(m_fp, lOffset, SEEK_SET);
                                        m_fp.Write(caBuffer, 0, sectorSize);                                    // fwrite(caBuffer, 1, 256, m_fp);

                                        // position to next sector in free chain

                                        nUserTrack = caBuffer[0];
                                        nUserSector = caBuffer[1];
                                        nFileTotalSectors++;

                                        // get next sector in the free chain so we have the linkage bytes set up

                                        if ((nUserSector != 0) && (nUserTrack != 0))
                                        {
                                            lOffset = CalcFileOffset(nMaxSector, nUserTrack, nUserSector);              // lOffset = ((nUserTrack * nMaxSector) + (nUserSector - 1)) * 256; <- already commented out
                                            m_fp.Seek(lOffset, SeekOrigin.Begin);                                       // fseek(m_fp, lOffset, SEEK_SET);
                                            Array.Clear(caBuffer, 0, sectorSize);                                       // memset(caBuffer, '\0', 256);
                                            m_fp.Read(caBuffer, 0, 2);                                                  // fread(caBuffer, 1, 2, m_fp);       // get sector linkage
                                            nSequence++;
                                        }
                                        else
                                        {
                                            nDiskIsFull = true;
                                            DialogResult afxReturn = MessageBox.Show("Do you want to save partial file?", "Disk is FULL", MessageBoxButtons.YesNo);
                                            if (afxReturn != DialogResult.Yes)
                                                nSaveFile = false;
                                        }
                                        Array.Clear(caBuffer, 4, 252);                                              // memset(&caBuffer[4], '\0', 252);
                                    }
                                }
                                else
                                {
                                    // we have to copy the file one byte at a time

                                    byte cPrevChar = 0x00;

                                    int srcIndex = 0;
                                    while ((srcIndex < fileContent.Length) && !nDiskIsFull)                         // while (((nCount = fread(&c, 1, 1, fp)) > 0) && !nDiskIsFull)
                                    {
                                        byte c = fileContent[srcIndex++];
                                        int nCount = 1;

                                        int nBytesWritten = 0;
                                        Array.Clear(caBuffer, 4, 252);                                              // memset(&caBuffer[4], '\0', 252);

                                        while (nBytesWritten < 252)
                                        {
                                            if (nCount > 0)
                                            {
                                                if (m_nStripLinefeed)
                                                {
                                                    // if this is a line feed char and we are stripping linefeeds
                                                    // then ignore the character, unless the previous character
                                                    // was not a carriage return. Then we convert the linefeed to
                                                    // a carriage return

                                                    if (c != 0x0a)
                                                    {
                                                        caBuffer[nBytesWritten + 4] = c;
                                                        nBytesWritten++;
                                                    }
                                                    else
                                                    {
                                                        if (cPrevChar != 0x0d)
                                                        {
                                                            caBuffer[nBytesWritten + 4] = 0x0d;
                                                            nBytesWritten++;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    caBuffer[nBytesWritten + 4] = c;
                                                    nBytesWritten++;
                                                }
                                            }
                                            else
                                                break;

                                            cPrevChar = c;

                                            if (nBytesWritten < 252)
                                            {
                                                // nCount = fread(&c, 1, 1, fp);

                                                if (srcIndex < fileContent.Length)
                                                {
                                                    c = fileContent[srcIndex++];
                                                    nCount = 1;
                                                }
                                                else
                                                    nCount = 0;
                                            }
                                        }

                                        // save this linkage info in case this is the last sector of the file

                                        nFileEndTrack = nUserTrack;
                                        nFileEndSector = nUserSector;

                                        // set the file sequence number in the sector

                                        caBuffer[2] = (byte)(nSequence / 256);
                                        caBuffer[3] = (byte)(nSequence % 256);

                                        // write the sector back to the image file

                                        m_fp.Seek(lOffset, SeekOrigin.Begin);                       // fseek(m_fp, lOffset, SEEK_SET);
                                        m_fp.Write(caBuffer, 0, sectorSize);                        // fwrite(caBuffer, 1, 256, m_fp);

                                        // position to next sector in free chain

                                        nUserTrack = caBuffer[0];
                                        nUserSector = caBuffer[1];
                                        nFileTotalSectors++;

                                        if ((nUserSector != 0) && (nUserTrack != 0))
                                        {
                                            lOffset = CalcFileOffset(nMaxSector, nUserTrack, nUserSector);          // lOffset = ((nUserTrack * nMaxSector) + (nUserSector - 1)) * 256; <- already commented out
                                            m_fp.Seek(lOffset, SeekOrigin.Begin);                                   // fseek(m_fp, lOffset, SEEK_SET);
                                            Array.Clear(caBuffer, 0, sectorSize);                                   // memset(caBuffer, '\0', 256);
                                            m_fp.Read(caBuffer, 0, 2);                                              // fread(caBuffer, 1, 2, m_fp);       // get sector linkage
                                            nSequence++;
                                        }
                                        else
                                        {
                                            nDiskIsFull = true;
                                            DialogResult afxReturn = MessageBox.Show("Do you want to save partial file?", "Disk is FULL", MessageBoxButtons.YesNo);
                                            if (afxReturn != DialogResult.Yes)
                                                nSaveFile = false;
                                        }
                                        Array.Clear(caBuffer, 4, 252);                                              // memset(&caBuffer[4], '\0', 252);
                                    }
                                }
                            }
                            else
                            {
                                string strSIRMessage = "Unknown System Information Record Error";

                                //if (((nUserTrack == 0) || (nUserTrack >= 256)) && ((nUserSector == 0) || (nUserSector >= 256)))
                                if (nUserTrack == 0 && nUserSector == 0)
                                {
                                    strSIRMessage = string.Format("Both the next user track and the next user sector in the SIR contain invlaid links [T=0x{0}] [S=0x{1}]", nUserTrack.ToString("X2"), nUserSector.ToString("X2"));
                                }
                                //else if ((nUserTrack == 0) || (nUserTrack >= 256))
                                else if (nUserTrack == 0)
                                {
                                    strSIRMessage = string.Format("The next user track in the SIR contains an invlaid link [0x{0}]", nUserTrack.ToString("X2"));
                                }
                                //else if ((nUserSector == 0) || (nUserSector >= 256))
                                else if (nUserSector == 0)
                                {
                                    strSIRMessage = string.Format("The next user sector in the SIR contains an invlaid link [0x{0}]", nUserSector.ToString("X2"));
                                }
                                MessageBox.Show(strSIRMessage);
                                nSaveFile = false;
                            }

                            // If we saved the file (successfully that is) we need to update the SIR and add the
                            // file to the directory

                            if (nSaveFile)
                            {
                                // We have to break the link on the last sector of the newly added file; Every time
                                // we consume a sector on the diskette as we write the file (import it) we will update
                                // these two values with the values of the track and sector we just wrote. These values
                                // start out as NextUserTrack and NextUserSector from the SIR, so they should NEVER
                                // be 00 or > FF. If they are we really screwed the pooch.

                                //if (((nFileEndTrack > 0) && (nFileEndTrack < 256)) && ((nFileEndSector > 0) && (nFileEndSector < 256)))
                                if ((nFileEndTrack > 0) && (nFileEndSector > 0))
                                {
                                    long lOffset = CalcFileOffset(nMaxSector, nFileEndTrack, nFileEndSector);                      // lOffset = ((nFileEndTrack * nMaxSector) + (nFileEndSector - 1)) * 256; <- already commented out
                                    m_fp.Seek(lOffset, SeekOrigin.Begin);                                                           // fseek (m_fp, lOffset, SEEK_SET);
                                    caBuffer[0] = 0x00;
                                    caBuffer[1] = 0x00;
                                    m_fp.Write(caBuffer, 0, 2);                                                                     // fwrite (caBuffer, 1, 2, m_fp);       // break sector linkage on last sector

                                    // File has been imported - update SIR and find a place in the directory;
                                    // First User Track and Sector will be the ones left in nUserTrack and
                                    // nUserSector.and the sectors used is in nFileTotalSectors.

                                    m_nTotalSectors = stSystemInformationRecord.cTotalSectorsHi * 256 + stSystemInformationRecord.cTotalSectorsLo;
                                    m_nTotalSectors -= nFileTotalSectors;

                                    // Update the SIR with the total number of sectors remaining and the next user track and sector information (new start of the free chain)

                                    stSystemInformationRecord.cTotalSectorsHi = (byte)(m_nTotalSectors / 256);
                                    stSystemInformationRecord.cTotalSectorsLo = (byte)(m_nTotalSectors % 256);
                                    stSystemInformationRecord.cFirstUserTrack = nUserTrack;
                                    stSystemInformationRecord.cFirstUserSector = nUserSector;

                                    m_fp.Seek(m_lPartitionBias + 0x0310 - (sectorSize * SectorBias), SeekOrigin.Begin);               // fseek (m_fp, m_lPartitionBias + 0x0310 - (sectorSize * m_nSectorBias), SEEK_SET);
                                    WriteRAW_FLEX_SIR(m_fp, stSystemInformationRecord, false);                                        // fwrite (&stSystemInformationRecord, sizeof (stSystemInformationRecord), 1, m_fp);

                                    // SIR has been fixed - stick new file in the directory - we already know it 
                                    // doesn't exist so all we need is either a deleted entry or an empty one.
                                    // Since we anticipated this - we remembered the one that got us to the end
                                    // during our search for an existing file.

                                    if (lCurrentDirOffset != 0L)    // get set by FindFLEXDirEntry and CheckIfFLEXFileExists
                                    {
                                        DateTime ltime = DateTime.Now;
                                        string szDate = ltime.ToString("yyyyMMdd");
                                        string szTime = ltime.ToString("HHmmss");

                                        m_fp.Seek(lCurrentDirOffset, SeekOrigin.Begin);                     // fseek (m_fp, lCurrentDirOffset, SEEK_SET);

                                        stDirEntry = new DIR_ENTRY();                                       // memset (&stDirEntry, '\0', sizeof (stDirEntry));

                                        Array.Copy(szTargetFileTitle, 0, stDirEntry.caFileName, 0, 8);      // memcpy (stDirEntry.caFileName,      szTargetFileTitle, 8);
                                        Array.Copy(szTargetFileExt, 0, stDirEntry.caFileExtension, 0, 3);   // memcpy (stDirEntry.caFileExtension, szTargetFileExt,   3);

                                        stDirEntry.cStartTrack = (byte)nFileStartTrack;
                                        stDirEntry.cStartSector = (byte)nFileStartSector;
                                        stDirEntry.cEndTrack = (byte)nFileEndTrack;
                                        stDirEntry.cEndSector = (byte)nFileEndSector;

                                        stDirEntry.cTotalSectorsHi = (byte)(nFileTotalSectors / 256);
                                        stDirEntry.cTotalSectorsLo = (byte)(nFileTotalSectors % 256);

                                        stDirEntry.cMonth = (byte)(ltime.Month);                            // stDirEntry.cMonth   = (byte) s.tm_mon;
                                        stDirEntry.cDay = (byte)(ltime.Day);                                // stDirEntry.cDay     = (byte) s.tm_mday;
                                        stDirEntry.cYear = (byte)(ltime.Year - 1900);                       // stDirEntry.cYear    = (byte) s.tm_year;

                                        WriteFLEX_DIR_ENTRY(m_fp, stDirEntry, false);                       // fwrite (&stDirEntry,  1, sizeof (stDirEntry), m_fp);
                                        m_fp.Flush();                                                       // fflush (m_fp);

                                    }
                                    else
                                        MessageBox.Show("OOPS - We just screwed up");
                                }
                                else
                                {
                                    string strSIRMessage = "Unknown Linkage Error";

                                    //if (((nFileEndTrack == 0) || (nFileEndTrack >= 256)) && ((nFileEndSector == 0) || (nFileEndSector >= 256)))
                                    if (nFileEndTrack == 0 && nFileEndSector == 0)
                                    {
                                        strSIRMessage = string.Format("Both the File End Track and the File End sector contain invlaid links [T=0x%02X] [S=0x%02X]", nFileEndTrack, nFileEndSector);
                                    }
                                    //if ((nFileEndTrack == 0) || (nFileEndTrack >= 256))
                                    if (nFileEndTrack == 0)
                                    {
                                        strSIRMessage = string.Format("The File End Track contains an invlaid link [0x%02X]", nFileEndTrack);
                                    }
                                    //else if ((nFileEndSector == 0) || (nFileEndSector >= 256))
                                    else if (nFileEndSector == 0)
                                    {
                                        strSIRMessage = string.Format("The File End Sector contains an invlaid link [0x%02X]", nFileEndSector);
                                    }
                                    MessageBox.Show(strSIRMessage);
                                }
                            }

                            m_fp.Seek(currentPosition, SeekOrigin.Begin);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Insufficient space on disk image file.");
                    }
                }
            }

        }

        #endregion

        #region Used to handle UniFLEX images

        private void WriteFileToUniFLEXImage(string dialogConfigType, string virtualFloppyFileName, byte[] fileContent, FileStream m_fp)
        {
            MessageBox.Show("Not yet implemented");
        }

        #endregion

        #region Called only from The main program (FloppyMaintDialog)

        public void WriteFileToImage(string dialogConfigType, string virtualFloppyFileName, byte[] fileContent)
        {
            WriteFileToImage(dialogConfigType, virtualFloppyFileName, fileContent, currentlyOpenedImageFileStream);
        }

        public void WriteFileToImage(string dialogConfigType, string virtualFloppyFileName, byte[] fileContent, string targetDirectory = "")
        {
            WriteFileToImage(dialogConfigType, virtualFloppyFileName, fileContent, currentlyOpenedImageFileStream, targetDirectory);
        }

        #endregion region

        // This called from both the FloppyMaintDialog and locally.

        /// <summary>
        /// 
        /// Writes a file from the PC to the virtual Floppy that is currently open
        /// 
        /// </summary>
        /// <param name="dialogConfigType"></param>
        /// <param name="virtualFloppyFileName"></param>
        /// <param name="fileContent"></param>
        /// <param name="m_fp"></param>
        public void WriteFileToImage(string dialogConfigType, string virtualFloppyFileName, byte[] fileContent, FileStream m_fp, string targetDirectory = "")
        {
            GetFileFormat();

            switch (CurrentFileFormat)
            {
                case fileformat.fileformat_FLEX:
                case fileformat.fileformat_FLEX_IDE:
                case fileformat.fileformat_FLEX_IMA:
                    WriteFileToFLEXImage(dialogConfigType, virtualFloppyFileName, fileContent, m_fp);
                    break;

                case fileformat.fileformat_OS9:
                    WriteFileToOS9Image(dialogConfigType, virtualFloppyFileName, fileContent, m_fp, targetDirectory);
                    break;

                case fileformat.fileformat_UniFLEX:
                    WriteFileToUniFLEXImage(dialogConfigType, virtualFloppyFileName, fileContent, m_fp);
                    break;

                default:
                    MessageBox.Show("Cannot write - Unknown image file format");
                    break;
            }
        }

        #region Used to Handle OS9 Images

        bool overrideFileSizeMisMatch = false;
        bool supressErrors = false;

        ArrayList alErrors = new ArrayList();

        public string strDescription = "";

        // to calculate where the cluster is in the allocation map, we need to divide it by 8
        // since there are 8 clusters mapped per byte. The lower number cluster's mapping bit
        // is in the higher order bit.
        //
        //      First Byte              Second Byte                 etc
        //      ------------------      -------------------         ---
        //      1111 1111               1111 1111
        //      ^^^^ ^^^^               ^^^^ ^^^^
        //      |||| ||||__  LSN 7      |||| ||||__  LSN 15
        //      |||| |||___  LSN 6      |||| |||___  LSN 14
        //      |||| ||____  LSN 5      |||| ||____  LSN 13
        //      |||| |_____  LSN 4      |||| |_____  LSN 12         ...
        //      ||||_______  LSN 3      ||||_______  LSN 11
        //      |||________  LSN 2      |||________  LSN 10
        //      ||_________  LSN 1      ||_________  LSN  9
        //      |__________  LSN 0      |__________  LSN  8

        private void MarkSegEntryUnallocated(int sector, int size)
        {
            int clusterSectorIsIn = sector / os9SectorsPerCluster;

            int startAllocationMapByteOffset = clusterSectorIsIn / 8;
            int startAllocationMapBitOffset = clusterSectorIsIn % 8;

            for (int i = startAllocationMapBitOffset, clustersUnallocated = 0; clustersUnallocated != size; i++)
            {
                switch (i)
                {
                    case 0: allocationBitMap[startAllocationMapByteOffset] &= 0x7f; break;  // turn off bit 7 0111 1111 to make this cluster un-allocated
                    case 1: allocationBitMap[startAllocationMapByteOffset] &= 0xbf; break;  // turn off bit 6 1011 1111
                    case 2: allocationBitMap[startAllocationMapByteOffset] &= 0xdf; break;  // turn off bit 5 1101 1111
                    case 3: allocationBitMap[startAllocationMapByteOffset] &= 0xef; break;  // turn off bit 4 1110 1111
                    case 4: allocationBitMap[startAllocationMapByteOffset] &= 0xf7; break;  // turn off bit 3 1111 0111
                    case 5: allocationBitMap[startAllocationMapByteOffset] &= 0xfb; break;  // turn off bit 2 1111 1011
                    case 6: allocationBitMap[startAllocationMapByteOffset] &= 0xfd; break;  // turn off bit 1 1111 1101

                    // turn off bit 0 and point to the next byte in the map and reset i (set to -1 becasue the for loop is going to increment it)

                    case 7:
                        allocationBitMap[startAllocationMapByteOffset] &= 0xfe;
                        startAllocationMapByteOffset++;
                        i = -1;
                        break;
                }
                clustersUnallocated++;  // this will get us out of the loop once we have un marked them all
            }
        }

        private void MarkSegEntryAllocated(int sector, int size)
        {
            int clusterSectorIsIn = sector / os9SectorsPerCluster;

            int startAllocationMapByteOffset = clusterSectorIsIn / 8;
            int startAllocationMapBitOffset = clusterSectorIsIn % 8;

            for (int i = startAllocationMapBitOffset, clustersAllocated = 0; clustersAllocated != size; i++)
            {
                switch (i)
                {
                    case 0: allocationBitMap[startAllocationMapByteOffset] |= 0x80; break;  // turn on bit 7 0111 1111 to make this cluster allocated
                    case 1: allocationBitMap[startAllocationMapByteOffset] |= 0x40; break;  // turn on bit 6 1011 1111
                    case 2: allocationBitMap[startAllocationMapByteOffset] |= 0x20; break;  // turn on bit 5 1101 1111
                    case 3: allocationBitMap[startAllocationMapByteOffset] |= 0x10; break;  // turn on bit 4 1110 1111
                    case 4: allocationBitMap[startAllocationMapByteOffset] |= 0x08; break;  // turn on bit 3 1111 0111
                    case 5: allocationBitMap[startAllocationMapByteOffset] |= 0x04; break;  // turn on bit 2 1111 1011
                    case 6: allocationBitMap[startAllocationMapByteOffset] |= 0x02; break;  // turn on bit 1 1111 1101

                    // turn on bit 0 and point to the next byte in the map and reset i (set to -1 becasue the for loop is going to increment it)

                    case 7:
                        allocationBitMap[startAllocationMapByteOffset] |= 0x01;
                        startAllocationMapByteOffset++;
                        i = -1;
                        break;
                }
                clustersAllocated++;  // this will get us out of the loop once we have un marked them all
            }
        }

        /// <summary>
        /// FindNextAvailableCluster ()
        /// </summary>
        /// <returns>Next Available Cluster in allocationBitMap or 0 if allocaitonBitMap has no free clusters</returns>
        private int FindNextAvailableCluster ()
        {
            bool availableClusterFound = false;
            int nextAvailableCluster = 0;   // NEVER AVAILABLE as the default

            for (int byteIndex = 0; (byteIndex < os9BytesInAllocationMap) && (nextAvailableCluster == 0); byteIndex++)
            {
                int bitIndex = 0;
                for (int bitMask = 0x80; (bitMask > 0) && (nextAvailableCluster == 0); bitMask = bitMask >> 1, bitIndex++)
                {
                    // find a bit in th byte that is 0 (0 = available)

                    if ((allocationBitMap[byteIndex] & bitMask) == 0)
                    {
                        // we found an allocaiton entry that is available for use.

                        nextAvailableCluster = byteIndex * 8;
                        nextAvailableCluster += bitIndex;
                        availableClusterFound = true;
                        break;
                    }
                }
            }

            if (!availableClusterFound)
                nextAvailableCluster = 0;

            return nextAvailableCluster;
        }

        private void LogDiskWrites(List<OS9_BYTES_TO_WRITE> bytesToWriteList)
        {
            if (logFloppyAccess)
            {
                using (StreamWriter sw = new StreamWriter(File.Open(os9FloppyWritesFile, FileMode.Append, FileAccess.Write)))
                {
                    foreach (OS9_BYTES_TO_WRITE bytesToWrite in bytesToWriteList)
                    {
                        sw.WriteLine(string.Format("Offset {0}: count: {1}", bytesToWrite.fileOffset.ToString("X8"), bytesToWrite.content.Count.ToString("X4")));
                        for (int i = 0; i < bytesToWrite.content.Count; i++)
                        {
                            if ((i % 32) == 0)                                          // output index as X4
                            {
                                if (i != 0)
                                    sw.Write("\n");

                                sw.Write(string.Format("{0} ", (i + bytesToWrite.fileOffset).ToString("X8")));
                            }

                            if (((i % 4) == 0) && ((i % 32) != 0))                      // output a space between every 4 bytes
                                sw.Write(" ");

                            if (((i % 8) == 0) && ((i % 32) != 0))                      // output another space between every 8 bytes
                                sw.Write(" ");

                            sw.Write(string.Format("{0} ", bytesToWrite.content[i].ToString("X2")));
                        }
                        sw.Write("\n");
                    }
                }
            }
            else
                return;
        }


        /// <summary>
        /// WriteByteArrayToOS9Image (List<OS9_BYTES_TO_WRITE> bytesToWriteList)
        /// </summary>
        /// <param name="bytesToWriteList"></param>
        /// 
        ///     This is where we actually modify the diskette image file. This is the only place
        ///     where we actually modify the content of the OS9 diskette image. Until this is 
        ///     called the OS9 image is still intact.
        ///     
        ///     It is a good idea to reload the OS9 image's system information record after calling this and
        ///     before using any information from the previous load.
        ///     
        ///         LoadOS9DisketteImageFile (cDrivePathName);
        ///     
        public void WriteByteArrayToOS9Image (List<OS9_BYTES_TO_WRITE> bytesToWriteList)
        {
            LogDiskWrites(bytesToWriteList);

            int currentPosition = (int)currentlyOpenedImageFileStream.Position;

            foreach (OS9_BYTES_TO_WRITE btw in bytesToWriteList)
            {
                currentlyOpenedImageFileStream.Seek((long)btw.fileOffset, SeekOrigin.Begin);
                currentlyOpenedImageFileStream.Write(btw.content.ToArray(), 0, btw.content.Count);
                currentlyOpenedImageFileStream.Flush();
            }

            currentlyOpenedImageFileStream.Seek((long)currentPosition, SeekOrigin.Begin);
        }

        /// <summary>
        /// Delete a file from the OS9 diskette image .dsk file
        /// </summary>
        /// <param name="treeNode" description="the node in the xml nodes that contain the information about the file we need to delete"></param>
        /// 
        ///     Deleteing a file will modify the following areas of the OS9 diskette image in memory so we must be sure to save them to the actual file
        ///     
        ///         The Allocation bit map that starts at offset 0x0100 for nBytesInAllocationMap bytes
        /// 

        private byte [] DateTimeToOS9DateTime (DateTime dateTime)
        {
            byte[] returnArray = new byte[5];

            if (dateTime.Year >= 2000)
                returnArray[0] = (byte)(dateTime.Year - 2000);
            else
                returnArray[0] = (byte)(dateTime.Year - 1900);
            returnArray[1] = (byte)dateTime.Month;
            returnArray[2] = (byte)dateTime.Day;
            returnArray[3] = (byte)dateTime.Hour;
            returnArray[4] = (byte)dateTime.Minute;

            return returnArray;
        }

        bool fileAlreadyExists = false;

        private bool OS9FileExists(XmlNode inXmlNode, string fullPath)
        {
            fileAlreadyExists = false;

            // this code is untested and still does not work.

          //string nodeToSearchFor = fullPath.Replace(@"\", "/").Replace(" ", "_");
            string nodeToSearchFor = fullPath.Replace("\\", "/").Replace(" ", "_").Replace("(", "_").Replace(")", "_");
            bool isLetter = !String.IsNullOrEmpty(nodeToSearchFor) && Char.IsLetter(nodeToSearchFor[0]);
            if (!isLetter)
                nodeToSearchFor = "_" + nodeToSearchFor;

            XmlNodeList nodeList = inXmlNode.ChildNodes;

            xmlDoc.IterateThroughAllNodes(delegate (XmlNode node) 
            {
                // this happens on every node in the xml document

                string nodeName = node.Name;

                // work our way back up the node adding the node's parent to the beginning of the name name we are going to comapre against
                //
                //      unfortunately, there is no way to break out of the iteration once the match is found - but it is what it is.

                while ((node.ParentNode != null) && node.ParentNode.Name != "#document")
                {
                    node = node.ParentNode;
                    nodeName = node.Name + "/" + nodeName;
                }

                if (string.Compare(nodeName, nodeToSearchFor, true) == 0)
                {
                    fileAlreadyExists = true;
                }
            }
            );

            return fileAlreadyExists;
        }

        public List<OS9_BYTES_TO_WRITE> CreateOS9Directory(TreeNode treeNode, XmlDocument xmlDocument, string newDirectoryName)
        {
            XmlNode rootNode = xmlDoc.ChildNodes[0];
            bool directoryAlreadyExists = OS9FileExists(rootNode , Path.Combine(treeNode.FullPath, newDirectoryName));

            OS9_BYTES_TO_WRITE bytesToWrite = new OS9_BYTES_TO_WRITE();
            List<OS9_BYTES_TO_WRITE> bytesToWriteBackToFile = new List<OS9_BYTES_TO_WRITE>();

            if (!directoryAlreadyExists)
            {
                // save the current position so we can re-position to where we were when we entered

                int currentPosition = (int)currentlyOpenedImageFileStream.Position;

                OS9_FILE_DESCRIPTOR parentFileDescriptor = new OS9_FILE_DESCRIPTOR();

                // default to the root directory

                int parentFirstFileDescriptorLSN = os9StartingRootSector;
                int parentFileDescriptorOffset = parentFirstFileDescriptorLSN * nLSNBlockSize;

                // if the text and the full path of the node are the same - the root node is selected, otherwise this is NOT the root
                //  so we need to calculate the offset and override the default.

                if (treeNode.Text != treeNode.FullPath)
                {
                    // if this is not going into the root - get the LSN of where it is going and calculate a new offset

                    string nodeToSearchFor = treeNode.FullPath.Replace("\\", "/").Replace(" ", "_").Replace("(", "_").Replace(")", "_");
                    bool isLetter = !String.IsNullOrEmpty(nodeToSearchFor) && Char.IsLetter(nodeToSearchFor[0]);
                    if (!isLetter)
                        nodeToSearchFor = "_" + nodeToSearchFor;

                    XmlNode xmlNode = xmlDocument.SelectSingleNode(nodeToSearchFor);
                    if (xmlNode != null)
                    {
                        // if there is one - there will only be one - so we can safely assume that nodes[0] is the node we are looking at
                        //
                        //      At this point both the treeNode and the xmlNodes[0] point to the information for the selected tree node and the 
                        //      information for the corresponding node in the xml document
                        //
                        //  The next step is to use the LSN from the xmlNodes[0] to point to the fileOffset to the File Descriptor for what
                        //  is about to become a proud parent.

                        parentFirstFileDescriptorLSN = Int32.Parse(xmlNode.Attributes["FileDesriptorSector"].Value);
                        parentFileDescriptorOffset = parentFirstFileDescriptorLSN * nLSNBlockSize;
                    }
                }

                /// Once we get here we do not need treeNode or xmlDocument any more
                /// 

                // now that we are pointing at the proper offset into the file for the file descriptor we need to modify - get it.

                GetOS9FileDescriptor(ref parentFileDescriptor, parentFileDescriptorOffset);        // this is the file descriptor for the parent that this new directory will live in

                //  We will use the parent file descriptor to locate the parent directory file where this new directory will need to be inserted,
                //  but first we need to allocate a cluster for the new directory's file descriptor so it can be added to the parent when we add
                //  the entry. We will not actually write to the parentFileDescriptorOffset, we will merely use it to locate the actual parent's
                //  directory entries. The only time we would make any changes to the data at the parentFileDescriptorOffset is if we need to add
                //  any clusters to the parent's file descriptor if we need to ad any segments to the file.

                //int firstFileDescriptorLSN = FindNextAvailableCluster();

                // we need to multiply the result by os9SectorsPerCluster since we point to sectors, but allocate clusters

                int firstFileDescriptorLSN = FindNextAvailableCluster() * os9SectorsPerCluster;     // ToDo: This needs to be tested thoughly
                if (firstFileDescriptorLSN != 0)
                {
                    #region Build new directory's file descriptor

                    MarkSegEntryAllocated(firstFileDescriptorLSN, 1);        // allocate a cluster for the new directory's file descriptor.
                    DateTime now = DateTime.Now;

                    int fileDescriptorOffset = firstFileDescriptorLSN * nLSNBlockSize;

                    // this will cause the file size to cleared when we save changes to disk. 
                    //
                    //      Start wil building a properly formatted file descriptor for a directory
                    //
                    //                                                76543210
                    //            cATT    0x00   File Attributes     (dsewrewr)                      
                    //            cOWN    0x01   File Owner's User ID                      
                    //            cOWN1   0x02                                             
                    //            cDAT    0x03   Date and Time Last Modified Y:M:D:H:M     
                    //            cDAT1   0x04                                             
                    //            cDAT2   0x05                                             
                    //            cDAT3   0x06                                             
                    //            cDAT4   0x07                                             
                    //            cLNK    0x08   Link Count                                
                    //            cSIZ    0x09   File Size                                 
                    //            cSIZ1   0x0A                                             
                    //            cSIZ2   0x0B                                             
                    //            cSIZ3   0x0C                                             
                    //            cDCR    0x0D   Date Created Y M D                        
                    //            cDCR1   0x0E                                             
                    //            cDCR2   0x0F                                             
                    //            cSEG    0x10   segment list - 48 entries of 5 bytes each (3 bytes of LSN and 2 bytes of LSN count)
                    //          

                    bytesToWrite = new OS9_BYTES_TO_WRITE();
                    bytesToWrite.fileOffset = fileDescriptorOffset;           // point to first byte in FD to write to

                    // first zero out the block just allocated

                    for (int i = 0; i < sectorSize * os9SectorsPerCluster; i++)
                    {
                        bytesToWrite.content.Add(0x00);
                    }
                    bytesToWriteBackToFile.Add(bytesToWrite);

                    // create a new block for the real data to write

                    bytesToWrite = new OS9_BYTES_TO_WRITE();
                    bytesToWrite.fileOffset = fileDescriptorOffset;           // point to first byte in FD to write to

                    // add the attributes and ownerid (3 bytes starting at offset 0)

                    bytesToWrite.content = new List<byte> { 0xBF, 0, 0 };    // d-ewrewr, owner id = 0x0000

                    // now the Modified Date and Time starting at offset 0x03 for 5 bytes

                    byte[] os9DateTime = DateTimeToOS9DateTime(now);
                    for (int i = 0; i < 5; i++)
                        bytesToWrite.content.Add(os9DateTime[i]);

                    // this will go in offset 8

                    bytesToWrite.content.Add(0x01);                             // link count

                    // set the initial file size (64) - 32 bytes for each directory entry in the directory (file)
                    //
                    //      every directory has a minimum of 2 files . and .. so - two files at 0x20 bytes each 
                    //      is 0x40 bytes for the initial directory size.
                    //
                    //      this is 4 bytes and specifies the number of bytes that are in this file (which is really a list of files)
                    //      When first created it should = the number of bytes actually in current use - so 64 or 0x40
                    //

                    bytesToWrite.content.Add(0x00);
                    bytesToWrite.content.Add(0x00);
                    bytesToWrite.content.Add(0x00);
                    bytesToWrite.content.Add(0x40);

                    // date created is just a date - no time (this starts at offset 0x0D for 3 bytes)

                    for (int i = 0; i < 3; i++)
                        bytesToWrite.content.Add(os9DateTime[i]);

                    //  and finally - the initial segment list - which is unlikely to ever change. This starts at offset 0x10 (16) in the sector
                    //  for 3 bytes
                    //
                    //      first - get a cluster to be the directories first data segment

                    //int directoryFileLSN = FindNextAvailableCluster();

                    // if the os9SectorsPerCluster is not 1 then the file descriptor is going to share the remainder of it's sectors with the file data.
                    // so if os9SectorsPerCluster is 2 then the first data directoryFileLSN will be the firstFileDescriptorLSN + 1, otherwise we need
                    // to allocate another sector for the data
                    //
                    // we need to multiply the result by os9SectorsPerCluster since we point to sectors, but allocate clusters

                    int directoryFileLSN = firstFileDescriptorLSN + 1;              // default to multiple sectors per cluster

                    if (os9SectorsPerCluster == 1)
                    {
                        directoryFileLSN = FindNextAvailableCluster();

                        // if we had to allocate a new cluster for the file data, mark it as allocated

                        MarkSegEntryAllocated(directoryFileLSN, 1);        // allocate a cluster for the new directory's file

                        OS9_BYTES_TO_WRITE anotherbytesToWrite = new OS9_BYTES_TO_WRITE();
                        anotherbytesToWrite.fileOffset = directoryFileLSN * sectorSize * os9SectorsPerCluster;           // point to first byte in FD to write to

                        // first zero out the block just allocated

                        for (int i = 0; i < sectorSize * os9SectorsPerCluster; i++)
                        {
                            anotherbytesToWrite.content.Add(0x00);
                        }
                        bytesToWriteBackToFile.Add(anotherbytesToWrite);
                    }

                    if (directoryFileLSN != 0)
                    {
                        bytesToWrite.content.Add((byte)(directoryFileLSN / 65536));
                        bytesToWrite.content.Add((byte)((directoryFileLSN % 65536) / 256));
                        bytesToWrite.content.Add((byte)(directoryFileLSN % 256));
                    }

                    // now write the number of sectors we are giving to the directory for file space (requires two bytes) starting at offset 0x13 (19) in the sector

                    bytesToWrite.content.Add(0x00);
                    bytesToWrite.content.Add(0x01);

                    // and just be safe - we should zero out the rest of the cluster (starting at offset 21 (0x15) in the sector

                    for (int i = 21; i < nLSNBlockSize * os9SectorsPerCluster; i++)     // ToDo: this needs to be thoughly tested
                        bytesToWrite.content.Add(0x00);

                    // FD built - set up to write it to the image - this will add this block of bytes to the array of blocks to write

                    bytesToWriteBackToFile.Add(bytesToWrite);

                    #endregion

                    #region Build new directory's file information (the contents of the directory file)
                    //  Now build the actual directory file informaition - the .. and the . file entries
                    //
                    //           29 bytes of high order bit terminated string of file name                                                                                                                                                 
                    //           ----------------------------------------------------------
                    // 0x2E 0xAE 0x00 0x00 0x00 0x00 0x00 0x00   0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00   0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00   0x00 0x00 0x00 0x00 0x00  // 0x00  0x00  0x03  <- LSN of parent
                    // 0xAE 0x00 0x00 0x00 0x00 0x00 0x00 0x00   0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00   0x00 0x00 0x00 0x00 0x00 0x00 0x00 0x00   0x00 0x00 0x00 0x00 0x00  // 0x00  0x00  0x03  <- LSN of self

                    bytesToWrite = new OS9_BYTES_TO_WRITE();
                    bytesToWrite.fileOffset = directoryFileLSN * nLSNBlockSize * os9SectorsPerCluster;
                    bytesToWrite.content = new List<byte> { 0x2E, 0xAE, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
                    bytesToWrite.content.Add((byte)(parentFirstFileDescriptorLSN / 65536));
                    bytesToWrite.content.Add((byte)((parentFirstFileDescriptorLSN % 65536) / 256));
                    bytesToWrite.content.Add((byte)(parentFirstFileDescriptorLSN % 256));

                    List<byte> dotEntry = new List<byte> { 0xAE, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
                    foreach (byte b in dotEntry)
                        bytesToWrite.content.Add(b);

                    bytesToWrite.content.Add((byte)(firstFileDescriptorLSN / 65536));
                    bytesToWrite.content.Add((byte)((firstFileDescriptorLSN % 65536) / 256));
                    bytesToWrite.content.Add((byte)(firstFileDescriptorLSN % 256));

                    // Now clear out the rest of the directory file so all of the directory entries are available for use.
                    //
                    //      start at entry number 3 (1 based) since . and .. are already done.
                    //      each sector will hold sectorSize / 32 entries which for 256 bytes sectors = 32 entries

                    for (int dirEntryIndex = 2; dirEntryIndex < ((sectorSize * os9SectorsPerCluster) / 32); dirEntryIndex++)
                    {
                        // we really only need to do the first byte of each entry, but since this needs to be a contiguous block of data starting at the bytesToWrite.offset, we have to do all 32 bytes

                        for (int dirEntryBytesIndex = 0; dirEntryBytesIndex < 32; dirEntryBytesIndex++)
                            bytesToWrite.content.Add((byte)0x00);
                    }
                    bytesToWriteBackToFile.Add(bytesToWrite);
                    #endregion

                    #region Add the new directoy as an entry in the parent's directory file

                    // use the parentFileDescriptorOffset to traverse the parent directoriy's directory entries.

                    int parentDirectoryFileLSN = 0;
                    int parentDirectorySectorCount = 0;
                    int parentDirectoryFileOffset = 0;

                    // there are up to 48 segments that can be assigned in the first file descriptor sector (that's 240 bytes)
                    //
                    //      Each segment entry consists of 3 bytes to define the LSN that contains the data and 2 bytes to define the number of clusters in the segment.
                    //      if the data for the file is not in contiguous clusters, there will be multiple segments that will tell us what LSN the data is in.
                    //

                    bool emptySlotFound = false;

                    int totalSegmentsAvailable = 48;

                    for (int i = 0; (i < totalSegmentsAvailable) && (emptySlotFound == false); i++)
                    {
                        parentDirectoryFileLSN     = (parentFileDescriptor.cSEG[i * 5 + 0] * 65536) + (parentFileDescriptor.cSEG[i * 5 + 1] * 256) + (parentFileDescriptor.cSEG[i * 5 + 2]);
                        parentDirectorySectorCount = (parentFileDescriptor.cSEG[i * 5 + 3] * 256) + (parentFileDescriptor.cSEG[i * 5 + 4]);

                        // get the pointer in the image of where the data is for this file (directory).

                        parentDirectoryFileOffset = parentDirectoryFileLSN * nLSNBlockSize;

                        // if it is 0, then we are out of clusters in this segment - move on to next segment in the file descriptor

                        if (parentDirectoryFileOffset != 0)
                        {
                            // we will need these when we find an empty slot

                            int currentDirEntryOffset = 0;
                            byte[] dirEntryBuffer = new byte[32];

                            //  we have parentDirectorySectorCount assigned to this segment and each segment has os9SectorsPerCluster * sectorSize bytes in it and each directory entry is 32 bytes
                            //  so - here we "do the math" and come up the number of available slots in this segment.

                            int numberOfDirectoryEntries = (parentDirectorySectorCount * sectorSize) / 32;

                            //  Now we need to insert this new directory into the parent' directory file. For this we will need to get the name of the new directory and the path to insert it into.
                            //  This information is in the treeNode passed in and the parentFileDescriptor and in the passed in parameter.
                            //
                            //      Using the parent file descriptor - find an available direntry in the parent's directory file. Do this by traversing the files' segments looking for either
                            //      0x00 or 0xE5 at the start on the directory entry.
                            //
                            //      This for loop goes through ALL directory entries in this segment looking for an empty slot to insert a new directory

                            for (int entryIndex = 0; entryIndex < numberOfDirectoryEntries; entryIndex++)
                            {
                                // see if this directory entry is available to put our new directory into

                                currentDirEntryOffset = parentDirectoryFileOffset + (entryIndex * 32);
                                currentlyOpenedImageFileStream.Seek(parentDirectoryFileOffset + (entryIndex * 32), SeekOrigin.Begin);
                                if (currentlyOpenedImageFileStream.Read(dirEntryBuffer, 0, 32) == 32)
                                {
                                    if (dirEntryBuffer[0] == 0x00 || dirEntryBuffer[0] == 0xE5)
                                    {
                                        // we found an empty entry - use it.

                                        emptySlotFound = true;
                                        break;
                                    }
                                }
                            }

                            //  if emptySlotFound is true then we found one and can continue using this slot whose offset into the image files is currentDirEntryOffset. 
                            //  If not then we need to go to the next segment in the file descruptor.

                            if (emptySlotFound)
                            {
                                // since we might be making the directory file larger - we need to change the size if we are

                                int nDirSize = (parentFileDescriptor.cSIZ * 16777216) + (parentFileDescriptor.cSIZ1 * 65536) + (parentFileDescriptor.cSIZ2 * 256) + parentFileDescriptor.cSIZ3;
                                int newSizeRequired = (currentDirEntryOffset + 32) - parentDirectoryFileOffset;

                                if (newSizeRequired > nDirSize)
                                {
                                    // we have made the file bigger - we will need to update this when we write our changes back to the disk

                                    bytesToWrite = new OS9_BYTES_TO_WRITE();
                                    bytesToWrite.fileOffset = parentFileDescriptorOffset + 0x09;     // this is where cSIZ starts for 4 bytes

                                    bytesToWrite.content.Add((byte)(newSizeRequired / 4294967296));
                                    bytesToWrite.content.Add((byte)((newSizeRequired % 4294967296 ) / 65536));
                                    bytesToWrite.content.Add((byte)((newSizeRequired % 65536) / 256));
                                    bytesToWrite.content.Add((byte)(newSizeRequired  % 256));

                                    bytesToWriteBackToFile.Add(bytesToWrite);
                                }

                                // we have the position in the file (parent directory) of where to stick this new directory entry (currentDirEntryOffset)

                                bytesToWrite = new OS9_BYTES_TO_WRITE();
                                bytesToWrite.fileOffset = currentDirEntryOffset;

                                // now we need to get the name of the new directory into the dirEntryBuffer and then add it to the bytesToWrite.content 

                                byte[] bytes = Encoding.ASCII.GetBytes(newDirectoryName);
                                bytes[bytes.Length - 1] = (byte)(bytes[bytes.Length - 1] | 0x80);       // turn on high order bit

                                foreach (byte b in bytes)
                                {
                                    bytesToWrite.content.Add(b);
                                }

                                for (int bytesToPad = 29 - bytes.Length; bytesToPad > 0; bytesToPad--)
                                {
                                    bytesToWrite.content.Add(0x00);
                                }

                                // now write the LSN of the new directory file descriptor - file directory entry points to the file descriptor of the new file and the
                                // file descriptor of the new file points to the file data segments.

                                bytesToWrite.content.Add((byte)(firstFileDescriptorLSN / 65536));
                                bytesToWrite.content.Add((byte)((firstFileDescriptorLSN % 65536) / 256));
                                bytesToWrite.content.Add((byte)(firstFileDescriptorLSN % 256));

                                bytesToWriteBackToFile.Add(bytesToWrite);

                                // And always update the Allocation Bit Map - be sure to always start with a clean OS9_BYTES_TO_WRITE

                                bytesToWrite = new OS9_BYTES_TO_WRITE();
                                bytesToWrite.fileOffset = 0x0100;           // allocation bit map is ALWAYS at 0x0100
                                foreach (byte b in allocationBitMap)
                                {
                                    bytesToWrite.content.Add(b);
                                }
                                bytesToWriteBackToFile.Add(bytesToWrite);

                                // all done
                            }
                        }
                        else
                        {
                            MessageBox.Show("no more clusters available on this image");

                            // make sure we do not modify the diskette image file.

                            bytesToWriteBackToFile = new List<OS9_BYTES_TO_WRITE>();
                            break;
                        }
                    }

                    #endregion

                    if (!emptySlotFound)
                    {
                        MessageBox.Show("Unable to locate an empty slot in the parent directory to enter this directory entry");

                        // make sure we do not modify the diskette image file.

                        bytesToWriteBackToFile = new List<OS9_BYTES_TO_WRITE>();
                    }
                }

                // and finally - put the file offset back to where we were when we entered.

                currentlyOpenedImageFileStream.Seek((long)currentPosition, SeekOrigin.Begin);

                // entries in the bytesToWriteBackToFile
                //
                //      [0] first:  file descriptor for new directory
                //      [1] second: actual start cluster of file (new directory) this will contain the . and .. directories initally
                //      [2] third:  the actual directory entry for the parent - the directory name in the first 29 bytes (high order bit terminated) and the LSN in the last 3 bytes
                //      [3] fourth: the modified allocation Bit Map
            }
            else
                MessageBox.Show("Directory already exists");

            return bytesToWriteBackToFile;
        }

        public List<OS9_BYTES_TO_WRITE> DeleteOS9File(XmlNode node)
        {
            OS9_BYTES_TO_WRITE bytesToWrite = new OS9_BYTES_TO_WRITE();
            List<OS9_BYTES_TO_WRITE> bytesToWriteBackToFile = new List<OS9_BYTES_TO_WRITE>();

            // save the current position so we can re-position to where we were when we entered

            int currentPosition = (int)currentlyOpenedImageFileStream.Position;

            //MessageBox.Show("Not yet implemented");

            // to delete a file from the OS9 image file we need to first reclaim the clusters from the allocation map
            // to do this we need to find the cluster that contains the list of clusters for this file. Then we will
            // need to mark this directory entry as deleted.

            OS9_FILE_DESCRIPTOR fd = new OS9_FILE_DESCRIPTOR();

            // get the file descriptor LSN for the file we need to delete and then calculate the offset into the file for this cluster

            int firstFileDescriptorLSN = Int32.Parse(node.Attributes["FileDesriptorSector"].Value);
            int nFileDescriptorOffset = firstFileDescriptorLSN * nLSNBlockSize;

            if (firstFileDescriptorLSN != os9StartingRootSector)
            { 

                // now get and use the offset to read the File Descriptor for this file

                GetOS9FileDescriptor(ref fd, nFileDescriptorOffset);

                //  we now have the list of LSNs that are allocated to this file - as well as the LSN that contains the list.
                //  the fd.alSEGArray is just such list. There are up to 48 entries in this list that has 3 bytes (24 bit) 
                //  LSN then 2 byte (16 bit) cluster count,

                // this will cause the file size to cleared when we save changes to disk

                bytesToWrite = new OS9_BYTES_TO_WRITE();
                bytesToWrite.fileOffset = nFileDescriptorOffset + 0x09;           // point to file size in file's File Descriptor cSIZ
                bytesToWrite.content = new List<byte> { 0, 0, 0, 0 };
                bytesToWriteBackToFile.Add(bytesToWrite);

                for (int index = 0; index < fd.alSEGArray.Count; index++)
                {
                    OS9_SEG_ENTRY segEntry = (OS9_SEG_ENTRY)fd.alSEGArray[index];
                    if (segEntry.nSector != 0)
                    {
                        // this will cause the cluster count in the to cleared when we save changes to disk

                        bytesToWrite = new OS9_BYTES_TO_WRITE();
                        bytesToWrite.fileOffset = (nFileDescriptorOffset + 0x10) + (index * 5) + 3;     // point to segment list - 5 bytes per segment entry - + 3 to get to cluster count
                        bytesToWrite.content = new List<byte> { 0, 0 };                                 // clear out the cluster count for this segment entry
                        bytesToWriteBackToFile.Add(bytesToWrite);

                        // this will make the cluster available once more

                        MarkSegEntryUnallocated(segEntry.nSector, segEntry.nSize);
                    }
                    else
                        break;
                }

                // now we need to mark this file descriptor cluster available also.

                MarkSegEntryUnallocated(firstFileDescriptorLSN, 1);

                //  File clusters have been reclaimed - now handle cleaning up the directory that the file is/was in
                //
                //  and finally the entry for this file needs to be deleted from the directory
                //
                //      to remove the file entry from the directory it lives in, we need to get the
                //      parent node so we can get the file descriptor sector for the directory.

                XmlNode parentNode = node.ParentNode;

                int directoryFileDescriptorLSN = Int32.Parse(parentNode.Attributes["FileDesriptorSector"].Value);
                int directoryFileDescriptorOffset = directoryFileDescriptorLSN * nLSNBlockSize;

                // must create a new fd for the directory

                fd = new OS9_FILE_DESCRIPTOR();
                GetOS9FileDescriptor(ref fd, directoryFileDescriptorOffset);

                // fd now points to the file descriptor for the parent diectory
                //
                //      use this file descriptor to find the actual directory file that contains a list of file descriptors
                //
                //          fd.cSEG contains the list of LSNs and sizes for each cluster in the directory file in the format 
                //
                //              xx xx xx yy yy where xx xx xx is the LSN of the starting file cluster 
                //                             and      yy yy is the number of clusters in the segment
                //

                for (int index = 0; index < fd.alSEGArray.Count; index++)
                {
                    // enumerate the segments inside the directory

                    System.Collections.IEnumerator cSegEnumerator = fd.alSEGArray.GetEnumerator();

                    // dir size is the actual size (number of bytes used) of the directory (directories are just a special type of file)

                    int nDirSize = (fd.cSIZ * 16777216) + (fd.cSIZ1 * 65536) + (fd.cSIZ2 * 256) + fd.cSIZ3;

                    OS9_SEG_ENTRY segEntry = (OS9_SEG_ENTRY)fd.alSEGArray[index];
                    int nDirSector = segEntry.nSector;
                    int nBlockSize = (segEntry.nSize);

                    if (segEntry.nSector != 0)
                    {
                        // see if the file directory entry is in this cluster

                        ASCIIEncoding ascii = new ASCIIEncoding();
                        OS9_DIR_ENTRY dirEntry = new OS9_DIR_ENTRY();

                        int nMaxDirRecords = nDirSize / 32;

                        if ((nBlockSize * 8) < nMaxDirRecords)
                            nMaxDirRecords = nBlockSize * 8;

                        // position file pointer to this directory cluster 

                        long nCurrentOffset = (nDirSector) * nLSNBlockSize;
                        currentlyOpenedImageFileStream.Seek(nCurrentOffset, SeekOrigin.Begin);

                        for (int i = 0; i < nMaxDirRecords; i++)
                        {
                            // read in the filename

                            currentlyOpenedImageFileStream.Read(dirEntry.cNAM, 0, dirEntry.cNAM.Length);

                            // now get the LSN of the file's file descriptor

                            dirEntry.cSCT  = (byte)currentlyOpenedImageFileStream.ReadByte();
                            dirEntry.cSCT1 = (byte)currentlyOpenedImageFileStream.ReadByte();
                            dirEntry.cSCT2 = (byte)currentlyOpenedImageFileStream.ReadByte();

                            // search for the file we are deleteing in the OS9 directory

                            bool bFoundTheEnd = false;
                            byte[] filename = new byte[41];

                            for (int j = 0; j < dirEntry.cNAM.Length; j++)
                            {
                                if (bFoundTheEnd)
                                    filename[j] = 0x00;
                                else
                                {
                                    filename[j] = (byte)(dirEntry.cNAM[j] & (byte)0x7F);
                                    if (filename[j] == 0x00)
                                    {
                                        bFoundTheEnd = true;
                                    }
                                }
                            }

                            if ((dirEntry.cNAM[0] != 0xE5) && (dirEntry.cNAM[0] != 0x00))
                            {
                                // See if this is the filename we are looking for and check that the dir entry is not empty or deleted
                                //
                                //      If it is - delete it by setting the first byte = 0xE5
                                // 

                                string fileName = ASCIIEncoding.ASCII.GetString(filename).Trim('\0');
                                if (fileName == node.Name)
                                {
                                    //      Here is where we will delete the file in the directory by setting the first byte to 0xE5 AND 
                                    //      mark the file descriptor cluster(s) for this file as available in the allocation map

                                    // we have found the one we are looking for - set to deleted and reclaim the cluster(s)

                                    int clusterToFree = (dirEntry.cSCT * 65536) + (dirEntry.cSCT1 * 256) + dirEntry.cSCT2;

                                    dirEntry.cNAM[0] = 0x00;
                                    MarkSegEntryUnallocated(clusterToFree, 1);

                                    bytesToWrite = new OS9_BYTES_TO_WRITE();
                                    bytesToWrite.fileOffset = (int)currentlyOpenedImageFileStream.Position - 32;

                                    foreach (byte b in dirEntry.cNAM)
                                    {
                                        bytesToWrite.content.Add(b);
                                    }
                                    bytesToWriteBackToFile.Add(bytesToWrite);

                                    bytesToWrite = new OS9_BYTES_TO_WRITE();

                                    // And always update the Allocation Bit Map

                                    bytesToWrite.fileOffset = 0x0100;           // allocation bit map is ALWAYS at 0x0100
                                    foreach (byte b in allocationBitMap)
                                    {
                                        bytesToWrite.content.Add(b);
                                    }
                                    bytesToWriteBackToFile.Add(bytesToWrite);

                                    // all done

                                    break;
                                }
                            }
                        }
                    }
                    else
                        break;
                }
            }
            else
            {
                MessageBox.Show("Deleting the root directory is not allowed");
            }
            currentlyOpenedImageFileStream.Seek((long)currentPosition, SeekOrigin.Begin);

            return bytesToWriteBackToFile;
        }

        private List<OS9_SEG_ENTRY> GetAllocatedSegmentsForFileData (int fileSize, int fileDescriptorLSN)
        {
            List<OS9_SEG_ENTRY> segEntryList = new List<OS9_SEG_ENTRY>();

            // use filesize, sectorSize and sectorsPerCluster to get a list of segments for this file.
            //
            //  clusters consist of 1 or more Logical Sector Numbers. We allocate disk space based on clusters
            //  but we do the writing to Logical Sector Numbers which are sequential within the cluster. Since
            //  the file descriptor is only one sector of the cluster it is in, we need to use the remaining 
            //  sectors for data. The number of sectors available for data in the file descriptor cluster is
            //  determined by the number of sectors in the cluster. The formula for determining this is:
            //
            //      os9SectorsPerCluster - 1
            //
            //  So when we are determinig how many more clsuters we need, we need to subtract this from the total 
            //  fileSize before we get the number of additional clusters required.

            int additionalSectorsRequiredToHoldFile = (fileSize / sectorSize) - (os9SectorsPerCluster - 1);
            if ((fileSize % sectorSize) != 0)
                additionalSectorsRequiredToHoldFile++;

            int totalClustersRequired = additionalSectorsRequiredToHoldFile / os9SectorsPerCluster;
            if (additionalSectorsRequiredToHoldFile % os9SectorsPerCluster != 0)
            {
                // we will need an additional cluster to hold overflow if the number of bytes exceed that
                // which can be contained in the remaining sectors of the file descriptor

                if (additionalSectorsRequiredToHoldFile > (os9SectorsPerCluster - 1))
                    totalClustersRequired++;
                else
                {
                    // this is will handle the case when the file's data will fit inside of the sectors remaining
                    // in the file's descriptor cluster.
                    //
                    //      we are basically done except for building the segment list

                    totalClustersRequired = 0;      // this will keep us from allocating any more clusters in this function

                    //OS9_SEG_ENTRY segEntry = new OS9_SEG_ENTRY();
                    //segEntry.nSector = fileDescriptorLSN + 1;
                    //segEntry.nSize = additionalSectorsRequiredToHoldFile;

                    //segEntryList.Add(segEntry);
                }
            }

            int previousFileDataCluster = 0;
            int previousFileDataLSNCount = 0;
            int remainingAdditionalClustersRequired = totalClustersRequired;

            int currentFileDataLSN = 0;                 // this will be non-zero when it is time to use it if we used sectors from the filedescriptor

            bool fileDataFitInFileDescriptor = false;

            if (os9SectorsPerCluster > 1)
            {
                // we are going to use the remaining sectors of the file escriptor cluster

                currentFileDataLSN = fileDescriptorLSN + 1;

                previousFileDataCluster = (fileDescriptorLSN / os9SectorsPerCluster);
                previousFileDataLSNCount = os9SectorsPerCluster - 1;
                remainingAdditionalClustersRequired--;

                if (totalClustersRequired <= 0)
                {
                    // no additional sectors are required, so we need to build the segment list with the logical sector numbers in 
                    // the file descriptor cluster starting at the second sector.

                    OS9_SEG_ENTRY segEntry = new OS9_SEG_ENTRY();

                    segEntry.nSector = fileDescriptorLSN + 1;
                    segEntry.nSize = os9SectorsPerCluster - 1;

                    segEntryList.Add(segEntry);

                    fileDataFitInFileDescriptor = true;
                }
            }

            int bytesAllocatedForFileOnDisk = previousFileDataLSNCount * nLSNBlockSize;

            // regardless of the sectors per cluster, we need to allocate the remaining number of sectors required.

            while ((remainingAdditionalClustersRequired-- > 0) || (bytesAllocatedForFileOnDisk < fileSize))
            {
                // grab a cluster for some data

                int fileDataCluster = FindNextAvailableCluster();
                bytesAllocatedForFileOnDisk += os9SectorsPerCluster * sectorSize;

                // calculate what the first LSN is of this cluster

                int fileDataClusterLSN = fileDataCluster * os9SectorsPerCluster;

                // mark the cluster as allocated

                MarkSegEntryAllocated(fileDataClusterLSN, 1);

                // see if this newly allocated cluster is contiguoua with the previous one

                if (fileDataCluster != (previousFileDataCluster + 1))
                {
                    // this is a non-conguous cluster - start a new segment unless this is the first one . If it is,
                    // just start a new segment

                    if (previousFileDataCluster == 0)
                    {
                        // this is the first one and there is only 1 sector per cluster so there is no current segment to close.
                        // if this was not 0 then either we set it to the first sector past the file descriptor sector in the
                        // file descriptor cluster, or we have allocated a non-contiguous cluster and need to add the segment
                        // we have been building to the list

                        currentFileDataLSN = fileDataCluster * os9SectorsPerCluster;
                        previousFileDataCluster = fileDataCluster;      // this is the one we will check to see if we are contiguous

                        previousFileDataLSNCount = os9SectorsPerCluster;
                    }
                    else
                    {
                        // this is not the first cluster's sector in the list - we have a segment that we were working on
                        // close it out by adding the segment to the list and reset the count

                        OS9_SEG_ENTRY segEntry = new OS9_SEG_ENTRY();
                        segEntry.nSector = currentFileDataLSN;
                        segEntry.nSize   = previousFileDataLSNCount;

                        segEntryList.Add(segEntry);

                        // start up a new segment

                        previousFileDataCluster = fileDataCluster;
                        currentFileDataLSN = fileDataCluster * os9SectorsPerCluster;
                        previousFileDataLSNCount = os9SectorsPerCluster;
                    }
                }
                else
                {
                    // this cluster is contiguous with the previous cluster and we are still gathering clusters for this 
                    // segment that starts with previousFileDataLSN.

                    previousFileDataLSNCount += os9SectorsPerCluster;
                    previousFileDataCluster = fileDataCluster;
                }
            }

            if (!fileDataFitInFileDescriptor && previousFileDataLSNCount > 0)
            {
                // flush the last segment

                OS9_SEG_ENTRY segEntry = new OS9_SEG_ENTRY();
                segEntry.nSector = currentFileDataLSN;
                segEntry.nSize = previousFileDataLSNCount;

                segEntryList.Add(segEntry);
            }

            return segEntryList;
        }

        private bool IsAlphaNumeric (byte c)
        {
            bool isAlphaNumeric = false;

            if ((c >= 0x20 && c <= 0x5F) || (c >= 0x61 && c <= 0x7E))
            {
                isAlphaNumeric = true;
            }

            return isAlphaNumeric;
        }

        private void WriteFileToOS9Image(string dialogConfigType, string virtualFloppyFileName, byte[] fileContent, FileStream m_fp, string targetDirectory)
        {
            //MessageBox.Show("Not yet implemented");

            //  Steps to write a file to the OS9 diskette image file from the PC. We are going to
            //  gather all of the writes to the List<OS9_BYTES_TO_WRITE> list and then do all of
            //  the writes to the image at one time. So all writes will be buffered into this List
            //  during this process. "write" below means adding an OS9_BYTES_TO_WRITE entry to the
            //  list until the very last step where we call WriteByteArrayToOS9Image with the list.
            //
            //X 1.  First make sure there enough clusters left on the image to hold the file.
            //
            //          This has already been done by the caller. 
            //
            //  2.  Get the File descriptor for the directory the file is being written to.
            //

            string parentDirectoryName = Path.GetDirectoryName(targetDirectory).Replace("\\", "/").Replace(" ", "_").Replace("(", "_").Replace(")", "_");
            int parentFirstFileDescriptorLSN = 0;
            int parentFileDescriptorOffset = 0;

            XmlNode xmlNode = xmlDoc.SelectSingleNode(parentDirectoryName);
            if (xmlNode != null)
            {
                // if there is one - there will only be one - so we can safely assume that nodes[0] is the node we are looking at
                //
                //      At this point both the treeNode and the xmlNodes[0] point to the information for the selected tree node and the 
                //      information for the corresponding node in the xml document
                //
                //  The next step is to use the LSN from the xmlNodes[0] to point to the fileOffset to the File Descriptor for what
                //  is about to become a proud parent.

                parentFirstFileDescriptorLSN = Int32.Parse(xmlNode.Attributes["FileDesriptorSector"].Value);
                parentFileDescriptorOffset = parentFirstFileDescriptorLSN * nLSNBlockSize;
            }

            //  3.  Make sure there is a slot for a new directory entry
            //      a.  if there is not - extend the size of the directory by adding a cluster to 
            //      the directory's file descriptor and marking
            //          that cluster as used in the allocation bit map
            //

            bool emptySlotFound = false;
            int totalSegmentsAvailable = 48;

            int parentDirectoryFileLSN = 0;
            int parentDirectorySectorCount = 0;

            OS9_FILE_DESCRIPTOR parentFileDescriptor = new OS9_FILE_DESCRIPTOR();

            GetOS9FileDescriptor(ref parentFileDescriptor, parentFileDescriptorOffset);        // this is the file descriptor for the parent that this new directory will live in
            List<OS9_BYTES_TO_WRITE> bytesToWriteBackToFile = new List<OS9_BYTES_TO_WRITE>();

            for (int i = 0; (i < totalSegmentsAvailable) && (emptySlotFound == false); i++)
            {
                // there are 5 bytes to each entry in the sefment table - 3 byte LSN and 2 byte size.

                parentDirectoryFileLSN = (parentFileDescriptor.cSEG[i * 5 + 0] * 65536) + (parentFileDescriptor.cSEG[i * 5 + 1] * 256) + (parentFileDescriptor.cSEG[i * 5 + 2]);
                parentDirectorySectorCount = (parentFileDescriptor.cSEG[i * 5 + 3] * 256) + (parentFileDescriptor.cSEG[i * 5 + 4]);

                // get the pointer in the image of where the data is for this file (directory).

                int parentDirectoryFileOffset = parentDirectoryFileLSN * nLSNBlockSize;

                // if it is 0, then we are out of clusters in this segment - move on to next segment in the file descriptor

                if (parentDirectoryFileOffset != 0)
                {
                    // we will need these when we find an empty slot

                    int currentDirEntryOffset = 0;
                    byte[] dirEntryBuffer = new byte[32];

                    //  we have parentDirectorySectorCount assigned to this segment and each segment has os9SectorsPerCluster * sectorSize bytes in it and each directory entry is 32 bytes
                    //  so - here we "do the math" and come up the number of available slots in this segment.

                    int numberOfDirectoryEntries = (parentDirectorySectorCount * sectorSize) / 32;

                    //  Now we need to insert this new directory into the parent' directory file. For this we will need to get the name of the new directory and the path to insert it into.
                    //  This information is in the treeNode passed in and the parentFileDescriptor and in the passed in parameter.
                    //
                    //      Using the parent file descriptor - find an available direntry in the parent's directory file. Do this by traversing the files' segments looking for either
                    //      0x00 or 0xE5 at the start on the directory entry.
                    //
                    //      This for loop goes through ALL directory entries in this segment looking for an empty slot to insert a new directory

                    for (int entryIndex = 0; entryIndex < numberOfDirectoryEntries; entryIndex++)
                    {
                        // see if this directory entry is available to put our new directory into

                        currentDirEntryOffset = parentDirectoryFileOffset + (entryIndex * 32);
                        currentlyOpenedImageFileStream.Seek(parentDirectoryFileOffset + (entryIndex * 32), SeekOrigin.Begin);
                        if (currentlyOpenedImageFileStream.Read(dirEntryBuffer, 0, 32) == 32)
                        {
                            if (dirEntryBuffer[0] == 0x00 || dirEntryBuffer[0] == 0xE5)
                            {
                                // we found an empty entry - use it.

                                emptySlotFound = true;
                                break;
                            }
                        }
                    }

                    //  if emptySlotFound is true then we found one and can continue using this slot whose offset into the image files is currentDirEntryOffset. 
                    //  If not then we need to go to the next segment in the file descruptor.

                    OS9_BYTES_TO_WRITE bytesToWrite = new OS9_BYTES_TO_WRITE();

                    if (emptySlotFound)
                    {
                        // since we might be making the directory file larger - we need to change the size if we are

                        //first get the actual number of bytes in this directory's file descriptor

                        int nDirSize = (parentFileDescriptor.cSIZ * 1677216) + (parentFileDescriptor.cSIZ1 * 65536) + (parentFileDescriptor.cSIZ2 * 256) + parentFileDescriptor.cSIZ3;
                        int newSizeRequired = (currentDirEntryOffset + 32) - parentDirectoryFileOffset;

                        if (newSizeRequired > nDirSize)
                        {
                            // we have made the file bigger - we will need to update this when we write our changes back to the disk

                            bytesToWrite = new OS9_BYTES_TO_WRITE();
                            bytesToWrite.fileOffset = parentFileDescriptorOffset + 0x09;     // this is where cSIZ starts for 4 bytes

                            bytesToWrite.content.Add((byte)(newSizeRequired / 4294967296));
                            bytesToWrite.content.Add((byte)((newSizeRequired % 4294967296) / 65536));
                            bytesToWrite.content.Add((byte)((newSizeRequired % 65536) / 256));
                            bytesToWrite.content.Add((byte)(newSizeRequired % 256));

                            bytesToWriteBackToFile.Add(bytesToWrite);
                        }

                        // we have the position in the file (parent directory) of where to stick this new file entry (currentDirEntryOffset)

                        bytesToWrite = new OS9_BYTES_TO_WRITE();
                        bytesToWrite.fileOffset = currentDirEntryOffset;

                        // now we need to get the name of the new file into the dirEntryBuffer and then add it to the bytesToWrite.content 

                        byte[] bytes = Encoding.ASCII.GetBytes(virtualFloppyFileName);
                        bytes[bytes.Length - 1] = (byte)(bytes[bytes.Length - 1] | 0x80);       // turn on high order bit

                        foreach (byte b in bytes)
                        {
                            bytesToWrite.content.Add(b);
                        }

                        for (int bytesToPad = 29 - bytes.Length; bytesToPad > 0; bytesToPad--)
                        {
                            bytesToWrite.content.Add(0x00);
                        }

            //  4.  grab a cluster for the new file's file descriptor and add it and the filename 
            //      to the directory entry (last byte of file name needs high order bit set)
            //
                        // we have finished writing the directory entry into the parent's directory for this file. Now it's time to grab some 
                        // clusters to store the file into and build the file's file descriptor

                        // get a cluster to save the file's file descriptor to. If the sectorsPerCluster is > 1 we will use the
                        // sectors after the file descriptor to start storing the file's data bytes. File descriptors are always
                        // only one sector.

                        int fileDescriptorLSN = FindNextAvailableCluster() * os9SectorsPerCluster;
                        if (fileDescriptorLSN != 0)
                        {
                            // add the file's file descriptor LSN to the directory entry as the last 3 bytes

                            MarkSegEntryAllocated(fileDescriptorLSN, 1);        // allocate a cluster for the new file's file descriptor.

                            OS9_BYTES_TO_WRITE yetanotherbytesToWrite = new OS9_BYTES_TO_WRITE();
                            yetanotherbytesToWrite.fileOffset = fileDescriptorLSN * sectorSize * os9SectorsPerCluster;           // point to first byte in FD to write to
                            for (int j = 0; j < sectorSize * os9SectorsPerCluster; j++)
                            {
                                yetanotherbytesToWrite.content.Add(0x00);
                            }
                            bytesToWriteBackToFile.Add(yetanotherbytesToWrite);

                            DateTime now = DateTime.Now;

                            //  now write the LSN of the new file's file descriptor - file directory entry points to the file descriptor of the new 
                            //  file and the file descriptor of the new file points to the file data segments.

                            bytesToWrite.content.Add((byte)(fileDescriptorLSN / 65536));
                            bytesToWrite.content.Add((byte)((fileDescriptorLSN % 65536) / 256));
                            bytesToWrite.content.Add((byte)(fileDescriptorLSN % 256));

                            bytesToWriteBackToFile.Add(bytesToWrite);
                        }

                        // here is where we write the file's data to the image into the clusters we will allocate for the file. As
                        // more clsuters are required, we will add them to the image and the file descriptor's segment list in the 
                        // file descriptor.

                        // TODO - use firstFileDescriptorLSN to start writing data if sectorsPerCluster is = 1, otherwise get more clusters
                        //        to start writing data to and add these clusters (with count) to the file's file descriptor.

                        List<OS9_SEG_ENTRY> dataSegments = GetAllocatedSegmentsForFileData(fileContent.Length, fileDescriptorLSN);

                        OS9_BYTES_TO_WRITE anotherbytesToWrite = new OS9_BYTES_TO_WRITE();

                        // first zero out the block just allocated in GetAllocatedSegmentsForFileData

                        foreach (OS9_SEG_ENTRY segentry in dataSegments)
                        {
                            anotherbytesToWrite.fileOffset = segentry.nSector * sectorSize;           // point to first byte in FD to write to
                            for (int j = 0; j < sectorSize * os9SectorsPerCluster; j++)
                            {
                                anotherbytesToWrite.content.Add(0x00);
                            }
                            bytesToWriteBackToFile.Add(anotherbytesToWrite);
                        }
                        // we now have a segment list we can use to write to the image with.

                        int fileContentIndex = 0;

                        int totalBytesAllocatedOnDisk = 0;
                        foreach (OS9_SEG_ENTRY segentry in dataSegments)
                        {
                            totalBytesAllocatedOnDisk += segentry.nSize * sectorSize;
                        }

            //  5.  write the contents of the file to the OS9_BYTES_TO_WRITE list one cluster at 
            //      a  time getting new clusters from the allocaiton bit map. Keep a list of all
            //      clusters being allocated so we can add them to the file's file descriptor and 
            //      mark them used later.
            //

                        bool fileIsBinary = false;

                        if (totalBytesAllocatedOnDisk >= fileContent.Length)            // one last sanity check before we commit
                        {
                            foreach (OS9_SEG_ENTRY segentry in dataSegments)
                            {
                                int bytesAvailableInThisSegment = segentry.nSize * sectorSize;

                                bytesToWrite = new OS9_BYTES_TO_WRITE();
                                bytesToWrite.fileOffset = segentry.nSector * sectorSize;

                                // This is where we actually read the source byte array and copy to the target byte array

                                while (fileContentIndex < fileContent.Length)
                                {
                                    if ((bytesAvailableInThisSegment > 0) && (fileContentIndex < fileContent.Length))
                                    {
                                        // let's see if this is a text file - if it is - honor the handling of CR/LF
                                        //
                                        //      0x87CD is signature byte for a module for 6809 OS9
                                        //      0x4AFC is signature byte for a module for 68K  OS9

                                        if ((fileContent[0] == 0x87 && fileContent[1] == 0xCD) || (fileContent[0] == 0x4A && fileContent[1] == 0xFC))
                                            fileIsBinary = true;
                                        else
                                        {
                                            if (!IsAlphaNumeric(fileContent[0]))
                                            {
                                                fileIsBinary = true;
                                            }
                                        }

                                        if (!fileIsBinary)
                                        {
                                            if (ConvertLfOnly || ConvertLfOnlyToCrLf || ConvertLfOnlyToCr)
                                            {
                                                int x = 0;
                                            }
                                        }

                                        bytesToWrite.content.Add(fileContent[fileContentIndex]);
                                        bytesAvailableInThisSegment--;
                                    }
                                    else
                                        break;

                                    fileContentIndex++;
                                }
                                bytesToWriteBackToFile.Add(bytesToWrite);
                            }

                            DateTime now = DateTime.Now;
                            byte[] os9DateTime = DateTimeToOS9DateTime(now);

                            if (dataSegments.Count <= 48)
                            {
                                // Now we need to write these segment entries to the cSEG portion of the file descriptor.

                                bytesToWrite = new OS9_BYTES_TO_WRITE();

                                // build the new file's file descriptor - start with the 16 bytes of the file descriptor

                                bytesToWrite.fileOffset = fileDescriptorLSN * nLSNBlockSize;

                                // this will cause the file size to cleared when we save changes to disk. 
                                //
                                //      Start wil building a properly formatted file descriptor for a directory
                                //
                                //                                                76543210
                                //            cATT    0x00   File Attributes     (dsewrewr)                      
                                //                              bit 7:	D  - Directory
                                //                              bit 6:	S  - Shared
                                //                              bit 5:	PE - Public Execute
                                //                              bit 4:	PW - Public Write
                                //                              bit 3:	PR - Public Read
                                //                              bit 2:	E  - Execute
                                //                              bit 1:	W  - Write
                                //                              bit 0:	R  - Read
                                //            cOWN    0x01   File Owner's User ID                      
                                //            cOWN1   0x02                                             
                                //            cDAT    0x03   Date and Time Last Modified Y:M:D:H:M     
                                //            cDAT1   0x04                                             
                                //            cDAT2   0x05                                             
                                //            cDAT3   0x06                                             
                                //            cDAT4   0x07                                             
                                //            cLNK    0x08   Link Count                                
                                //            cSIZ    0x09   File Size                                 
                                //            cSIZ1   0x0A                                             
                                //            cSIZ2   0x0B                                             
                                //            cSIZ3   0x0C                                             
                                //            cDCR    0x0D   Date Created Y M D                        
                                //            cDCR1   0x0E                                             
                                //            cDCR2   0x0F                                             
                                //            cSEG    0x10   segment list - 48 entries of 5 bytes each (3 bytes of LSN and 2 bytes of LSN count)
                                //          

                                // read the file here to see if it is an executable

                                byte fileAttributeBits = 0x1B;  // Public Write | Public Write | Write | Read

                                if (fileIsBinary)
                                {
                                    fileAttributeBits |= 0x24;  // Public Execute | Execute
                                }

                                bytesToWrite.content.Add((byte)fileAttributeBits);                              // File Attributes     --ewrewr                 0x00
                                bytesToWrite.content.Add((byte)0x00);                                           // File Owner's User ID                         0x01
                                bytesToWrite.content.Add((byte)0x00);                                           //                                              0x02
                                bytesToWrite.content.Add((byte)os9DateTime[0]);                                 // Date and Time Last Modified Y:M:D:H:M        0x03
                                bytesToWrite.content.Add((byte)os9DateTime[1]);                                 //                                              0x04
                                bytesToWrite.content.Add((byte)os9DateTime[2]);                                 //                                              0x05
                                bytesToWrite.content.Add((byte)os9DateTime[3]);                                 //                                              0x06
                                bytesToWrite.content.Add((byte)os9DateTime[4]);                                 //                                              0x07
                                bytesToWrite.content.Add((byte)0x01);                                           // Link Count                                   0x08
                                bytesToWrite.content.Add((byte)(fileContent.Length / 4294967296));              // File Size                                    0x09
                                bytesToWrite.content.Add((byte)((fileContent.Length % 4294967296) / 65536));    //                                              0x0A
                                bytesToWrite.content.Add((byte)((fileContent.Length % 65536) / 256));           //                                              0x0B
                                bytesToWrite.content.Add((byte)(fileContent.Length % 256));                     //                                              0x0C
                                bytesToWrite.content.Add((byte)os9DateTime[0]);                                 // Date Created Y M D                           0x0D
                                bytesToWrite.content.Add((byte)os9DateTime[1]);                                 //                                              0x0E
                                bytesToWrite.content.Add((byte)os9DateTime[2]);                                 //                                              0x0F

                                // we are now positioned at the cSEG portion of the file descriptor

                                foreach (OS9_SEG_ENTRY segentry in dataSegments)
                                {
                                    // first write the LSN

                                    bytesToWrite.content.Add((byte)(segentry.nSector / 65536));
                                    bytesToWrite.content.Add((byte)((segentry.nSector % 65536) / 256));
                                    bytesToWrite.content.Add((byte)(segentry.nSector % 256));

                                    // now write the size

                                    bytesToWrite.content.Add((byte)(segentry.nSize / 256));
                                    bytesToWrite.content.Add((byte)(segentry.nSize % 256));
                                }
                                bytesToWriteBackToFile.Add(bytesToWrite);

                                // And always update the Allocation Bit Map - be sure to always start with a clean OS9_BYTES_TO_WRITE

                                bytesToWrite = new OS9_BYTES_TO_WRITE();
                                bytesToWrite.fileOffset = 0x0100;           // allocation bit map is ALWAYS at 0x0100
                                foreach (byte b in allocationBitMap)
                                {
                                    bytesToWrite.content.Add(b);
                                }
                                bytesToWriteBackToFile.Add(bytesToWrite);
                            }
                            else
                            {

                                MessageBox.Show("To many segments allocated for file content");

                                // don't modify the diskette image if we are in error.

                                bytesToWriteBackToFile = new List<OS9_BYTES_TO_WRITE>();
                            }
                        }
                        else
                        {
                            MessageBox.Show("Insufficient disk space allocated for file content");

                            // don't modify the diskette image if we are in error.

                            bytesToWriteBackToFile = new List<OS9_BYTES_TO_WRITE>();
                        }

                        // all done
                    }
                }
                else
                {
                    //  we ran out of clusters in the previous segment of this file descriptor entry for this directory
                    //  and we are trying to see if we can fins a slot in the next segment when we discovered that there
                    //  are no more segments - we need to allocate one and add it to the parentFileDescriptor.

                    //  a.  allocate a new cluster
                    //  b.  mark it allocated
                    //  c.  clear out the sector data
                    //  d   add it to the parentFileDescriptor at offset parentDirectoryFileLSN * nLSNBlockSize
                    //      this is where we found the empty secment in the file descriptor.
                    //  e.  Add this new segment and the data we just wrote to the file descriptor to
                    //      the bytes to write to the disk array.
                    //  f.  back up the index and try again

                    int nextavailableCluster = FindNextAvailableCluster() * os9SectorsPerCluster;     // ToDo: This needs to be tested thoughly
                    if (nextavailableCluster != 0)
                    {
                        MarkSegEntryAllocated(nextavailableCluster, 1);        // allocate a cluster for the new segment

                        OS9_BYTES_TO_WRITE newSegmentBytesToWrite = new OS9_BYTES_TO_WRITE();
                        newSegmentBytesToWrite.fileOffset = nextavailableCluster * sectorSize * os9SectorsPerCluster;

                        // first zero out the block just allocated

                        for (int newSegmentBytesToWriteIndex = 0; newSegmentBytesToWriteIndex < sectorSize * os9SectorsPerCluster; newSegmentBytesToWriteIndex++)
                        {
                            newSegmentBytesToWrite.content.Add(0x00);
                        }
                        bytesToWriteBackToFile.Add(newSegmentBytesToWrite);


                        //  before we do this - we should check the previous entry to see if we can just add to it's count
                        //  or do we need to add another non-contiguous segment. The the previous entry is the previous
                        //  sector (remember - we allocate cluster, but point to LSN's (sectors), so if the cluster size 
                        //  is not 1 sector then multiply accordinglt before checking.
                        //
                        //  TODO:   the above - for now just add a segment and count of 1

                        OS9_BYTES_TO_WRITE parentSegmentBytesToWrite = new OS9_BYTES_TO_WRITE();

                        parentSegmentBytesToWrite.fileOffset = parentFileDescriptorOffset + 16 + (i * 5);    // add in the offset to the cSEG and the offset to this empty cSEG entry
                        parentSegmentBytesToWrite.content.Add((byte)(nextavailableCluster / 65536));
                        parentSegmentBytesToWrite.content.Add((byte)(nextavailableCluster / 256));
                        parentSegmentBytesToWrite.content.Add((byte)(nextavailableCluster % 256));

                        // only one sector at a time for now

                        parentSegmentBytesToWrite.content.Add(0x00);
                        parentSegmentBytesToWrite.content.Add(0x01);

                        bytesToWriteBackToFile.Add(parentSegmentBytesToWrite);

                        // fix up stuff in memory that we will use at the top of the loop here

                        parentFileDescriptor.cSEG[i * 5 + 0] = (byte)(nextavailableCluster / 65536);
                        parentFileDescriptor.cSEG[i * 5 + 1] = (byte)(nextavailableCluster / 256);
                        parentFileDescriptor.cSEG[i * 5 + 2] = (byte)(nextavailableCluster % 256);

                        parentFileDescriptor.cSEG[i * 5 + 3] = 0x00;
                        parentFileDescriptor.cSEG[i * 5 + 4] = 0x01;

                        // now update the file descriptor's link count and file size for the parent directory of this file
                        //
                        //      this is 1 byte for the lkink count at offset 8 into the file descriptor
                        //      and     4 bytes for the new size.
                        //
                        //      since we have not yet implemented adding to existing segment or allocating multiple clusters 
                        //      at a time for the segments - these will be hard coded to adding 1 and 0x0100 respectively to the existing values.

                        OS9_BYTES_TO_WRITE linkCountAndSizeBytesToWrite = new OS9_BYTES_TO_WRITE();
                        linkCountAndSizeBytesToWrite.fileOffset = parentFileDescriptorOffset + 8;    // add in offset to cLNK byte

                        int newDirSize = ((parentFileDescriptor.cSIZ * 1677216) + (parentFileDescriptor.cSIZ1 * 65536) + (parentFileDescriptor.cSIZ2 * 256) + parentFileDescriptor.cSIZ3) + 0x0100;

                        parentFileDescriptor.cLNK  = (byte)(parentFileDescriptor.cLNK + 1);
                        parentFileDescriptor.cSIZ  = (byte)(newDirSize / 1677216);
                        parentFileDescriptor.cSIZ1 = (byte)((newDirSize % 1677216) / 65536);
                        parentFileDescriptor.cSIZ2 = (byte)((newDirSize % 65536) / 256);
                        parentFileDescriptor.cSIZ3 = (byte)(newDirSize % 256);

                        linkCountAndSizeBytesToWrite.content.Add(parentFileDescriptor.cLNK);
                        linkCountAndSizeBytesToWrite.content.Add(parentFileDescriptor.cSIZ);
                        linkCountAndSizeBytesToWrite.content.Add(parentFileDescriptor.cSIZ1);
                        linkCountAndSizeBytesToWrite.content.Add(parentFileDescriptor.cSIZ2);
                        linkCountAndSizeBytesToWrite.content.Add(parentFileDescriptor.cSIZ3);

                        bytesToWriteBackToFile.Add(linkCountAndSizeBytesToWrite);

                        // now back up one and check this entry again

                        i--;
                    }
                    else
                    {
                        // we really have run out of cluster to allocate

                        MessageBox.Show("no more clusters available on this image");

                        // make sure we do not modify the diskette image file.

                        bytesToWriteBackToFile = new List<OS9_BYTES_TO_WRITE>();
                        break;
                    }
                }
            }

            //  6.  At this point the List<OS9_BYTES_TO_WRITE> list should contain entries for
            //      a.  the new file size for the directory that this file entry is being added to  (4 bytes)
            //      b.  the new directory entry for the file                                        (32 bytes)
            //      c.  the sectors that make up the file content                                   (fileContent.Length)
            //      d.  the the new file's file descriptor and cSEG list
            //      e.  the new allocation bit map bytes to write
            //
            //  7.  Once all of the data we have gathered that needs to be written to the image, call
            //      WriteByteArrayToOS9Image with the List of OS9_BYTES_TO_WRITE entries.
            //  

            WriteByteArrayToOS9Image(bytesToWriteBackToFile);
        }

        private void ProcessOS9ImageFile(string filename, string path, XmlDocument doc, XmlNode currentNode)
        {
            ulong dwTotalFileSize = 0;
            strDescription = "";

            try
            {
                FileInfo fi = new FileInfo(path);

                FileStream fs = currentlyOpenedImageFileStream;

                DateTime dtCreate = fi.CreationTime;
                DateTime dtLastAccessed = fi.LastAccessTime;
                DateTime dtLastModified = fi.LastWriteTime;

                dwTotalFileSize = (ulong)fs.Length;

                //fs.Close();

                int iRc = -1;

                // Get the directory into pszDescription

                //fileformat ff = GetFileFormat(path, dwTotalFileSize);
                fileformat ff = GetFileFormat();
                if (ff == fileformat.fileformat_OS9)
                {
                    PhysicalOS9Geometry physicalParameters = GetPhysicalOS9Geometry(fs);

                    // re-read the file info data

                    fi = new FileInfo(path);
                    //fs = fi.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                    dwTotalFileSize = (ulong)fs.Length;

                    //fs.Close();

                    iRc = LoadOS9SystemInformationRecord(doc, currentNode, ref strDescription, path, dwTotalFileSize);

                    XmlAttribute attribute = doc.CreateAttribute("RealName"); attribute.Value = filename; currentNode.Attributes.Append(attribute);

                    string strAttributes = String.Format
                        (
                        "{0}{1}{2}{3}{4}{5}{6}{7}",
                        (os9DiskAttributes & 0x80) == 0 ? " " : "d",
                        (os9DiskAttributes & 0x40) == 0 ? "-" : "s",
                        (os9DiskAttributes & 0x20) == 0 ? "-" : "e",
                        (os9DiskAttributes & 0x10) == 0 ? "-" : "w",
                        (os9DiskAttributes & 0x08) == 0 ? "-" : "r",
                        (os9DiskAttributes & 0x04) == 0 ? "-" : "e",
                        (os9DiskAttributes & 0x02) == 0 ? "-" : "w",
                        (os9DiskAttributes & 0x01) == 0 ? "-" : "r"
                        );


                    XmlAttribute attribute1 = doc.CreateAttribute("FileDesriptorSector"); attribute1.Value = os9StartingRootSector.ToString(); currentNode.Attributes.Append(attribute1);
                    XmlAttribute attribute2 = doc.CreateAttribute("ByteCount")          ; attribute2.Value = 0.ToString()                    ; currentNode.Attributes.Append(attribute2);
                    XmlAttribute attribute3 = doc.CreateAttribute("Attributes")         ; attribute3.Value = strAttributes                   ; currentNode.Attributes.Append(attribute3);
                    XmlAttribute attribute4 = doc.CreateAttribute("attributes")         ; attribute4.Value = os9DiskAttributes.ToString()    ; currentNode.Attributes.Append(attribute4);

                    doc.AppendChild(currentNode);

                    //if (swOut == null)
                    Console.WriteLine(strDescription);
                    //else
                    //    swOut.WriteLine(strDescription);
                }
                else
                {
                    string strErrorMessage = string.Format("Can not read directory from: {0}", path);
                    alErrors.Add(strErrorMessage);
                    return;
                }

            }
            catch (Exception e)
            {
                if (!supressErrors)
                {
                    //if (swOut == null)
                    MessageBox.Show(e.Message);
                    Console.WriteLine(e.Message);
                    //else
                    //    swOut.WriteLine(e.Message);
                }
            }
        }

        List<byte> allocationBitMap = new List<byte>();

        private int LoadOS9SystemInformationRecord(XmlDocument doc, XmlNode currentNode, ref string strDescription, string path, ulong dwTotalFileSize)
        {
            Console.WriteLine(path);

            int nRow = -1;

            OS9_ID_SECTOR stIDSector = new OS9_ID_SECTOR();

            FileInfo fi = new FileInfo(path);
            FileStream fs = currentlyOpenedImageFileStream;

            DateTime dtCreate = fi.CreationTime;
            DateTime dtLastAccessed = fi.LastAccessTime;
            DateTime dtLastModified = fi.LastWriteTime;

            allocationBitMap = new List<byte>();

            fs.Seek(0, SeekOrigin.Begin);

            stIDSector.cTOT[0] = (byte)fs.ReadByte();  // Total Number of sector on media
            stIDSector.cTOT[1] = (byte)fs.ReadByte();
            stIDSector.cTOT[2] = (byte)fs.ReadByte();

            stIDSector.cTKS[0] = (byte)fs.ReadByte();  // Sectors Per Track 0

            stIDSector.cMAP[0] = (byte)fs.ReadByte();  // Number of bytes in allocation map
            stIDSector.cMAP[1] = (byte)fs.ReadByte();

            stIDSector.cBIT[0] = (byte)fs.ReadByte();  // Number of sectors per cluster
            stIDSector.cBIT[1] = (byte)fs.ReadByte();

            stIDSector.cDIR[0] = (byte)fs.ReadByte();  // Starting sector of root directory
            stIDSector.cDIR[1] = (byte)fs.ReadByte();
            stIDSector.cDIR[2] = (byte)fs.ReadByte();

            stIDSector.cOWN[0] = (byte)fs.ReadByte();  // Owners user number
            stIDSector.cOWN[1] = (byte)fs.ReadByte();

            stIDSector.cATT[0] = (byte)fs.ReadByte();  // Disk attributes

            stIDSector.cDSK[0] = (byte)fs.ReadByte();  // Disk Identification
            stIDSector.cDSK[1] = (byte)fs.ReadByte();

            stIDSector.cFMT[0] = (byte)fs.ReadByte();  // Disk Format: density, number of sides
            //
            //  bit 0:    0 = single sided
            //            1 = double sided
            //  bit 1:    0 = single density
            //            1 = double density
            //  bit 2:    0 = single track (48 TPI)
            //            1 = double track (96 TPI)
            //
            stIDSector.cSPT[0] = (byte)fs.ReadByte();  // Number of sectors per track
            stIDSector.cSPT[1] = (byte)fs.ReadByte();

            stIDSector.cRES[0] = (byte)fs.ReadByte();  // Reserved for future use
            stIDSector.cRES[1] = (byte)fs.ReadByte();

            stIDSector.cBT[0] = (byte)fs.ReadByte();   // Starting sector of bootstrap file
            stIDSector.cBT[1] = (byte)fs.ReadByte();
            stIDSector.cBT[2] = (byte)fs.ReadByte();

            stIDSector.cBSZ[0] = (byte)fs.ReadByte();  // Size of bootstrap file (in bytes)
            stIDSector.cBSZ[1] = (byte)fs.ReadByte();

            stIDSector.cDAT[0] = (byte)fs.ReadByte();  // Time of creation Y:M:D:H:M
            stIDSector.cDAT[1] = (byte)fs.ReadByte();
            stIDSector.cDAT[2] = (byte)fs.ReadByte();
            stIDSector.cDAT[3] = (byte)fs.ReadByte();
            stIDSector.cDAT[4] = (byte)fs.ReadByte();

            fs.Read(stIDSector.cNAM, 0, stIDSector.cNAM.Length);
            StringBuilder sbVolumeName = new StringBuilder(stIDSector.cNAM.Length);
            for (int i = 0; i < stIDSector.cNAM.Length; i++)
            {
                if (stIDSector.cNAM[i] == 0x00)
                    sbVolumeName.Append(" ");
                else
                    sbVolumeName.Append((char)(stIDSector.cNAM[i] & 0x7f));
            }
            string strVolumeName = sbVolumeName.ToString().Trim();

            strVolumeName = strVolumeName.Replace("\0", " ");
            os9VolumeName = strVolumeName;

            // get the new ones including the 68K only ones

            stIDSector.cOPTS.pd_dtp   [0] = (byte)fs.ReadByte();      // 0x3F
            stIDSector.cOPTS.pd_drv   [0] = (byte)fs.ReadByte();      // 0x40
            stIDSector.cOPTS.pd_stp   [0] = (byte)fs.ReadByte();      // 0x41
            stIDSector.cOPTS.pd_typ   [0] = (byte)fs.ReadByte();      // 0x42
            stIDSector.cOPTS.pd_dns   [0] = (byte)fs.ReadByte();      // 0x43
            stIDSector.cOPTS.pd_cyl   [0] = (byte)fs.ReadByte();      // 0x44
            stIDSector.cOPTS.pd_cyl   [1] = (byte)fs.ReadByte();      // 0x45
            stIDSector.cOPTS.pd_sid   [0] = (byte)fs.ReadByte();      // 0x46
            stIDSector.cOPTS.pd_vfy   [0] = (byte)fs.ReadByte();      // 0x47
            stIDSector.cOPTS.pd_sct   [0] = (byte)fs.ReadByte();      // 0x48
            stIDSector.cOPTS.pd_sct   [1] = (byte)fs.ReadByte();      // 0x49
            stIDSector.cOPTS.pd_t0s   [0] = (byte)fs.ReadByte();      // 0x4A
            stIDSector.cOPTS.pd_t0s   [1] = (byte)fs.ReadByte();      // 0x4B
            stIDSector.cOPTS.pd_ilv   [0] = (byte)fs.ReadByte();      // 0x4C
            stIDSector.cOPTS.pd_sas   [0] = (byte)fs.ReadByte();      // 0x4D
            stIDSector.cOPTS.pd_tfm   [0] = (byte)fs.ReadByte();      // 0x4E
            stIDSector.cOPTS.pd_exten [0] = (byte)fs.ReadByte();      // 0x4F
            stIDSector.cOPTS.pd_exten [1] = (byte)fs.ReadByte();      // 0x50
            stIDSector.cOPTS.pd_stoff [0] = (byte)fs.ReadByte();      // 0x51
            stIDSector.cOPTS.pd_att   [0] = (byte)fs.ReadByte();      // 0x52
            stIDSector.cOPTS.pd_fd    [0] = (byte)fs.ReadByte();      // 0x53
            stIDSector.cOPTS.pd_fd    [1] = (byte)fs.ReadByte();      // 0x54
            stIDSector.cOPTS.pd_fd    [2] = (byte)fs.ReadByte();      // 0x55
            stIDSector.cOPTS.pd_dfd   [0] = (byte)fs.ReadByte();      // 0x56
            stIDSector.cOPTS.pd_dfd   [1] = (byte)fs.ReadByte();      // 0x57
            stIDSector.cOPTS.pd_dfd   [2] = (byte)fs.ReadByte();      // 0x58
            stIDSector.cOPTS.pd_dcp   [0] = (byte)fs.ReadByte();      // 0x59
            stIDSector.cOPTS.pd_dcp   [1] = (byte)fs.ReadByte();      // 0x5A
            stIDSector.cOPTS.pd_dcp   [2] = (byte)fs.ReadByte();      // 0x5B
            stIDSector.cOPTS.pd_dcp   [3] = (byte)fs.ReadByte();      // 0x5C
            stIDSector.cOPTS.pd_dvt   [0] = (byte)fs.ReadByte();      // 0x5D
            stIDSector.cOPTS.pd_dvt   [1] = (byte)fs.ReadByte();      // 0x5E

            // these are used by OS9/68K - on EmuOS9Boot.Dsk these are all zeros

            stIDSector.cDD_RES      [0] = (byte)fs.ReadByte();        // $5F 1 Reserved
            stIDSector.cDD_SYNC     [0] = (byte)fs.ReadByte();        // $60 4 DD_SYNC Media integrity code
            stIDSector.cDD_SYNC     [1] = (byte)fs.ReadByte();        // $61 4 DD_SYNC Media integrity code
            stIDSector.cDD_SYNC     [2] = (byte)fs.ReadByte();        // $62 4 DD_SYNC Media integrity code
            stIDSector.cDD_SYNC     [3] = (byte)fs.ReadByte();        // $63 4 DD_SYNC Media integrity code
            stIDSector.cDD_MapLSN   [0] = (byte)fs.ReadByte();        // $64 4 DD_MapLSN Bitmap starting sector number(0=LSN 1)
            stIDSector.cDD_MapLSN   [1] = (byte)fs.ReadByte();        // $65 4 DD_MapLSN Bitmap starting sector number(0=LSN 1)
            stIDSector.cDD_MapLSN   [2] = (byte)fs.ReadByte();        // $66 4 DD_MapLSN Bitmap starting sector number(0=LSN 1)
            stIDSector.cDD_MapLSN   [3] = (byte)fs.ReadByte();        // $67 4 DD_MapLSN Bitmap starting sector number(0=LSN 1)
            stIDSector.cDD_LSNSize  [0] = (byte)fs.ReadByte();        // $68 2 DD_LSNSize Media logical sector size(0=256)
            stIDSector.cDD_LSNSize  [1] = (byte)fs.ReadByte();        // $69 2 DD_LSNSize Media logical sector size(0=256)
            stIDSector.cDD_VersID   [0] = (byte)fs.ReadByte();        // $6A 2 DD_VersID Sector 0 Version ID
            stIDSector.cDD_VersID   [1] = (byte)fs.ReadByte();        // $6B 2 DD_VersID Sector 0 Version ID

            int nLSNBlockSize = stIDSector.cDD_LSNSize[1] + (stIDSector.cDD_LSNSize[0] * 256);

            if (nLSNBlockSize == 0)
                nLSNBlockSize = 256;

            int nNumberOfTracks         = stIDSector.cTKS [0];
            int nBytesInAllocationMap   = (stIDSector.cMAP[0] * 256) + stIDSector.cMAP[1];
            int nSectorsPerCluster      = (stIDSector.cBIT[0] * 256) + stIDSector.cBIT[1];       // this is only used for the allocation map 
            int nStartingRootSector     = (stIDSector.cDIR[0] * 65536) + (stIDSector.cDIR[1] * 256) + stIDSector.cDIR[2];
            int nOwnersUserNumber       = (stIDSector.cOWN[0] * 256) + stIDSector.cOWN[1];
            int nDiskAttributes         = stIDSector.cATT[0];
            int nDiskID                 = (stIDSector.cDSK[0] * 256) + stIDSector.cDSK[1];
            int nFormatDensitySides     = stIDSector.cFMT[0];
            int nSectorsPerTrack        = (stIDSector.cSPT[0] * 256) + stIDSector.cSPT[1];
            int nReservedForFutureUse   = (stIDSector.cRES[0] * 256) + stIDSector.cRES[1];
            int nBootstrapStartSector   = (stIDSector.cBT [0] * 65536) + (stIDSector.cBT[1] * 256) + stIDSector.cBT[2];
            int nSizeOfBootstrap        = (stIDSector.cBSZ[0] * 256) + stIDSector.cBSZ[1];

            // Time of creation Y:M:D:H:M

            int nCreateYear     = stIDSector.cDAT[0] % 100;
            int nCreateMonth    = stIDSector.cDAT[1] % 100;
            int nCreateDay      = stIDSector.cDAT[2] % 100;
            int nCreateHour     = stIDSector.cDAT[3] % 100;
            int nCreateMinute   = stIDSector.cDAT[4] % 100;

            string strCreationDateTime = String.Format
                (
                "{0}/{1}/{2} {3}:{4}",
                nCreateMonth.ToString("00"),
                nCreateDay.ToString("00"),
                nCreateYear.ToString("00"),
                nCreateHour.ToString("00"),
                nCreateMinute.ToString("00")
                );

            os9CreationDate         = strCreationDateTime;
            os9BytesInAllocationMap = nBytesInAllocationMap;
            os9StartingRootSector   = nStartingRootSector;
            os9DiskAttributes       = nDiskAttributes;

            // There's no point in going any further if the file size if not right

            int nTotalSectors = stIDSector.cTOT[2] + (stIDSector.cTOT[1] * 256) + (stIDSector.cTOT[0] * 65536);
            os9TotalSectors = nTotalSectors;
            ulong nDiskSize = (ulong)(nTotalSectors * nLSNBlockSize);

            // There's no point in going any further if the file size if not right

            if (nDiskSize == (dwTotalFileSize & 0xFFFFFF00) || overrideFileSizeMisMatch)
            {
                // first figure out how many of the bits in the allocation map have been used.

                fs.Seek(0x0100, SeekOrigin.Begin);  // start of the allocation map (LSN 1)

                os9UsedAllocationBits    = 0;
                os9UnusedAllocationBits  = 0;
                os9SystemRequiredSectors = 0;

                // need to handle last byte properly - not all bits may be used.
                //
                //      we can determine the number of bits used in the last byte by the following method:
                //
                //          divide the number of sectors on the diskette by the sectors per cluster. This
                //          give us the number of allocation bits required.
                //
                //          now divide that number by the 8 bits per byte. This will be the number of bytes 
                //          we will need, But if this number * 8 does not equal the number of bits required
                //          we will need an extra byte. 
                //
                //          The number of bits used in this extra byte will be the difference between the
                //          number of bits required and 8 * the number of bytes (without the extra byte)

                int numberOfBitsRequired  = nTotalSectors / nSectorsPerCluster;
                int numberOfBytesRequired = numberOfBitsRequired / 8;
                int numberOfBitsUsedInLastByte = 8;

                if (numberOfBytesRequired * 8 != numberOfBitsRequired)
                {
                    // we need to adjust the number of bytes and calculate the number of bits required in the last byte.

                    numberOfBytesRequired++;
                    numberOfBitsUsedInLastByte = numberOfBitsRequired - ((numberOfBytesRequired - 1) * 8);
                }

                for (int index = 0; index < nBytesInAllocationMap; index++)
                {
                    // the bits are used from left to right. the high order bits are the lower LSN

                    byte allocationByte = (byte)fs.ReadByte();

                    // add this entry to the map

                    allocationBitMap.Add(allocationByte);

                    for (int allocationBitIndex = 0; allocationBitIndex < 8; allocationBitIndex++)
                    {
                        if (index == nBytesInAllocationMap - 1)
                        {
                            // this is the last byte - it may not be fully utilized

                            if (allocationBitIndex >= numberOfBitsUsedInLastByte)
                            {
                                // if we have done the last bits in the last byte - we are finished - break out of the loop

                                break;
                            }
                        }

                        if ((allocationByte & 0x80) == 0)
                        {
                            os9UnusedAllocationBits++;
                            os9AllocationArray.Add(false);
                        }
                        else
                        {
                            os9UsedAllocationBits++;
                            os9AllocationArray.Add(true);
                        }

                        allocationByte <<= 1;
                    }
                }

                int totalBitsInAllocationMap = os9UsedAllocationBits + os9UnusedAllocationBits;

                os9SectorsPerCluster = nSectorsPerCluster;
                os9RemainingSectors  = os9UnusedAllocationBits * os9SectorsPerCluster;

                /* 
                    The following table describes the contents of the file descriptor.

                    Name        Relative    Size    Use
                                Address     Bytes 
                    ----------- --------    ------- -----------------------------------------------------
                    FD.ATT      $00           1     File attributes: D S PE PW PR E W R (see next chart)
                    FD.OWN      $01           2     Owner’s user ID
                    FD.DAT      $03           5     Date last modified (Y M D H M)
                    FD.LNK      $08           1     Link count
                    FD.SIZ      $09           4     the size (number of bytes)
                    FD.CREAT    $0D           3     Date created (Y M D)
                    FD.SEG      $10         240     Segment list (see next chart)

                    FD.ATT. The attribute byte contains the file permission bits. When set the bits indicate the following
                        Bit 7 Directory
                        Bit 6 Single user
                        Bit 5 Public execute
                        Bit 4 Public write
                        Bit 3 Public read
                        Bit 2 Execute
                        Bit 1 Write
                        Bit 0 Read

                    FD.SEG. The segment list consists of a maximum of 48 5-byte entries that have the size and address of 
                            each file block in logical order. Each entry has the block’s 3-byte LSN and 2-byte size (in 
                            sectors). The entry following the last segment is zero.

                    After creation, a file has no data segments allocated to it until the first write. (Write operations 
                    past the current end-of-file cause sectors to be added to the file. The first write is always past the end-of-file.)

                */

                fs.Seek((nStartingRootSector * nLSNBlockSize) + 16, SeekOrigin.Begin);  // start of the root directory file descriptor + 16 = starting root directory sector for 3 bytes
                int rootDirectoryStartSector = ((byte)fs.ReadByte() * 65536) + ((byte)fs.ReadByte() * 256) + (byte)fs.ReadByte();
                int rootDirectorySize        = ((byte)fs.ReadByte() * 256) + (byte)fs.ReadByte();

                os9SystemRequiredSectors = rootDirectoryStartSector + rootDirectorySize;

                // 
                strDescription = String.Format
                (
                    "----------------------------------------------------------\r\n" +
                    "OS9 formatted diskette: {14}\r\n" +
                    "    Created:  {15,2}/{16,2}/{17,4} {18,2}:{19,2}:{20,2}\r\n" +
                    "    Modified: {21,2}/{22,2}/{23,4} {24,2}:{25,2}:{26,2}\r\n" +
                    "----------------------------------------------------------\r\n\r\n" +
                    "   TotalSectors          = {0,6}\r\n" +
                    "   TKS                   = {1,6}\r\n" +
                    "   BytesInAllocationMap  = {2,6}\r\n" +
                    "   SectorsPerCluster     = {3,6}\r\n" +
                    "   StartingRootSector    = {4,6}\r\n" +
                    "   OwnersUserNumber      = {5,6}\r\n" +
                    "   DiskAttributes        = {6,6}\r\n" +
                    "   DiskID                = {7,6}\r\n" +
                    "   FormatDensitySides    = {8,6}\r\n" +
                    "   SectorsPerTrack       = {9,6}\r\n" +
                    "   ReservedForFutureUse  = {10,6}\r\n" +
                    "   BootstrapStartSector  = {11,6}\r\n" +
                    "   SizeOfBootstrap       = {12,6}\r\n" +
                    "   Time of creation      = {13}\r\n\r\n",
                        nTotalSectors,
                        nNumberOfTracks,
                        nBytesInAllocationMap,
                        nSectorsPerCluster,
                        nStartingRootSector,
                        nOwnersUserNumber,
                        nDiskAttributes,
                        nDiskID,
                        nFormatDensitySides,
                        nSectorsPerTrack,
                        nReservedForFutureUse,
                        nBootstrapStartSector,
                        nSizeOfBootstrap,
                        strCreationDateTime,
                        path,
                        dtCreate.Month.ToString("00"),
                        dtCreate.Day.ToString("00"),
                        dtCreate.Year.ToString(),
                        dtCreate.Hour.ToString("00"),
                        dtCreate.Minute.ToString("00"),
                        dtCreate.Second.ToString("00"),
                        dtLastModified.Month.ToString("00"),
                        dtLastModified.Day.ToString("00"),
                        dtLastModified.Year.ToString(),
                        dtLastModified.Hour.ToString("00"),
                        dtLastModified.Minute.ToString("00"),
                        dtLastModified.Second.ToString("00")
                );

                int nFileDescriptorOffset = nStartingRootSector * nLSNBlockSize;

                // get the root directory file descriptor

                OS9_FILE_DESCRIPTOR fd = new OS9_FILE_DESCRIPTOR();
                GetOS9FileDescriptor(fs, ref fd, nFileDescriptorOffset);

                int nAttributes = fd.cATT;
                string strAttributes = String.Format
                    (
                    "{0}{1}{2}{3}{4}{5}{6}{7}",
                    (fd.cATT & 0x80) == 0 ? " " : "d",
                    (fd.cATT & 0x40) == 0 ? "-" : "s",
                    (fd.cATT & 0x20) == 0 ? "-" : "e",
                    (fd.cATT & 0x10) == 0 ? "-" : "w",
                    (fd.cATT & 0x08) == 0 ? "-" : "r",
                    (fd.cATT & 0x04) == 0 ? "-" : "e",
                    (fd.cATT & 0x02) == 0 ? "-" : "w",
                    (fd.cATT & 0x01) == 0 ? "-" : "r"
                    );

                int nOwner = (fd.cOWN * 65536) + (fd.cOWN1 * 256) + fd.cOWN;
                string strLastModified = String.Format
                    (
                    "{0}/{1}/{2} {3}{4}",
                    fd.cDAT.ToString("00"),
                    fd.cDAT1.ToString("00"),
                    fd.cDAT2.ToString("00"),
                    fd.cDAT3.ToString("00"),
                    fd.cDAT4.ToString("00")
                    );

                int nByteCount = (fd.cSIZ * 1677216) + (fd.cSIZ1 * 65536) + (fd.cSIZ2 * 256) + fd.cSIZ3;

                // This will process the root dir. The function is responsible for recuring into any directories
                // that it finds

                strDescription += "Volume Name: " + strVolumeName + "\r\n\r\n";
                strDescription += "Owner Last modified attributes sector bytecount name\r\n";
                strDescription += "----- ---- -------- ---------- ------ --------- ----------\r\n";
                nRow = 0;

                string strParent = "";
                ProcessOS9Directory(doc, currentNode, strParent, ref nRow, ref strDescription, strParent, fd, fs, nSectorsPerTrack);
            }
            else
            {
                ulong nDifference;
                if (dwTotalFileSize > nDiskSize)
                    nDifference = dwTotalFileSize - nDiskSize;
                else
                    nDifference = nDiskSize - dwTotalFileSize;

                strDescription = String.Format
                    (
                    "Size of file and System Information Record data do not match:\r\n" +
                    "File Size: {0,10}\r\n" +
                    "IDS Size:  {1,10}\r\n" +
                    "Difference:{2,10}\r\n",
                    dwTotalFileSize,
                    nDiskSize,
                    nDifference
                    );
            }

            //fs.Close();

            return (nRow);
        }

        public void SaveOS9ImageFiles(OS9_FILE_DESCRIPTOR fd, int nAttributes, string currentPath, string strFilename, int firstFileDescriptorLSN, long nByteCount)
        {
            SaveOS9ImageFiles(currentlyOpenedImageFileStream, fd, nAttributes, currentPath, strFilename, firstFileDescriptorLSN, nByteCount);
        }

        public void SaveOS9ImageFiles(FileStream fs, OS9_FILE_DESCRIPTOR fd, int nAttributes, string currentPath, string strFilename, int firstFileDescriptorLSN, long nByteCount)
        {
            // here is where we can build the file path and read the file from the diskette image and write it to disk
            if (true)
            {
                string currentWorkingDirectory = Directory.GetCurrentDirectory();

                string disketteFileLocation = Path.GetDirectoryName(fs.Name);
                string disketteDirectoryName = Path.GetFileNameWithoutExtension(fs.Name);

                string[] paths = { (disketteFileLocation.Replace(currentWorkingDirectory, "")).TrimStart('\\'), disketteDirectoryName, currentPath };

                string fullPath = Path.Combine(paths);

                if (!Directory.Exists(fullPath))
                    Directory.CreateDirectory(fullPath);

                // if this is a directory - create it on the PC

                if (((nAttributes & 0x80) == 0x80) && (firstFileDescriptorLSN > 2))
                {
                    fullPath = Path.Combine(fullPath, strFilename);
                    if (!Directory.Exists(fullPath))
                        Directory.CreateDirectory(fullPath);
                }
                else
                {

                    byte[] buffer = new byte[nLSNBlockSize];

                    string fileWithFullPath = Path.Combine(fullPath, strFilename.Replace("*", "@"));
                    try
                    {
                        BinaryWriter bw = new BinaryWriter(File.Open(fileWithFullPath, FileMode.OpenOrCreate, FileAccess.Write));

                        // The nByteCount has the file size and each segment array has a start sector and number of contiguous sectors in it.
                        //
                        //      read bytes until either one of these two is exhausted. If we run out of sectors in this segment array entry
                        //      but we still have bytes to read, go to next segment array entry

                        long bytesRemaining = nByteCount;

                        for (int index = 0; index < fd.alSEGArray.Count; index++)
                        {
                            OS9_SEG_ENTRY segEntry = (OS9_SEG_ENTRY)fd.alSEGArray[index];

                            int sectorsRemainingInSegment = segEntry.nSize;
                            int sectorToRead = segEntry.nSector;

                            while (sectorsRemainingInSegment > 0)
                            {
                                // here is where we read from the open file stream (fs) and write to the new file (bw)

                                long position = fs.Position;        // remember current position

                                long bytesToRead = nLSNBlockSize;
                                if (bytesRemaining < nLSNBlockSize)
                                {
                                    bytesToRead = bytesRemaining;
                                }

                                fs.Seek(sectorToRead * nLSNBlockSize, SeekOrigin.Begin);
                                fs.Read(buffer, 0, (int)bytesToRead);
                                bw.Write(buffer, 0, (int)bytesToRead);
                                bytesRemaining -= bytesToRead;

                                fs.Seek(position, SeekOrigin.Begin);

                                ++sectorToRead;
                                --sectorsRemainingInSegment;
                            }
                        }
                        bw.Close();
                    }
                    catch (Exception e)
                    {
                        // left in for debugging purposes

                        Console.WriteLine(string.Format("{0} - {1}", fileWithFullPath, e.Message));
                    }

                    // -----------------------
                }
            }
        }

        public void GetOS9FileDescriptor(ref OS9_FILE_DESCRIPTOR fd, int nFileDescriptorOffset)
        {
            GetOS9FileDescriptor(currentlyOpenedImageFileStream, ref fd, nFileDescriptorOffset);
        }

        public void GetOS9FileDescriptor(FileStream fs, ref OS9_FILE_DESCRIPTOR fd, int nFileDescriptorOffset)
        {
            fs.Seek(nFileDescriptorOffset, SeekOrigin.Begin);

            fd.cATT     = (byte)fs.ReadByte();       // File Attributes

            fd.cOWN     = (byte)fs.ReadByte();       // File Owner's User ID
            fd.cOWN1    = (byte)fs.ReadByte();

            fd.cDAT     = (byte)fs.ReadByte();       // Date and Time Last Modified Y:M:D:H:M
            fd.cDAT1    = (byte)fs.ReadByte();
            fd.cDAT2    = (byte)fs.ReadByte();
            fd.cDAT3    = (byte)fs.ReadByte();
            fd.cDAT4    = (byte)fs.ReadByte();

            fd.cLNK     = (byte)fs.ReadByte();       // Link COunt

            fd.cSIZ     = (byte)fs.ReadByte();       // File Size
            fd.cSIZ1    = (byte)fs.ReadByte();
            fd.cSIZ2    = (byte)fs.ReadByte();
            fd.cSIZ3    = (byte)fs.ReadByte();

            fd.cDCR     = (byte)fs.ReadByte();       // Date Created Y M D
            fd.cDCR1    = (byte)fs.ReadByte();
            fd.cDCR2    = (byte)fs.ReadByte();

            fs.Read(fd.cSEG, 0, fd.cSEG.Length);    // segment list

            for (int i = 0; i < 48; i++)
            {
                int nSector = (fd.cSEG[i * 5 + 0] * 65536) + (fd.cSEG[i * 5 + 1] * 256) + fd.cSEG[i * 5 + 2];
                int nSize   = (fd.cSEG[i * 5 + 3] * 256) + fd.cSEG[i * 5 + 4];

                OS9_SEG_ENTRY cSEGEntry = new OS9_SEG_ENTRY();

                cSEGEntry.nSector = nSector;
                cSEGEntry.nSize = nSize;

                fd.alSEGArray.Add(cSEGEntry);
            }
        }

        private void ProcessOS9DirSegment(XmlDocument doc, XmlNode currentNode, ref ArrayList aDirEntries, ref int nRow, FileStream fs, ref string strDescription, long nCurrentOffset, int nBlockSize, int nDirSize, int nSectorsPerTrack, string currentPath)
        {
            ASCIIEncoding ascii = new ASCIIEncoding();
            OS9_DIR_ENTRY dirEntry = new OS9_DIR_ENTRY();

            int nMaxDirRecords = nDirSize / 32;

            if ((nBlockSize * 8) < nMaxDirRecords)
                nMaxDirRecords = nBlockSize * 8;

            for (int i = 0; i < nMaxDirRecords; i++)
            {
                bool readPastEndOfFile = false;
                if (nCurrentOffset < fs.Length)
                { 
                    long newPosition = fs.Seek(nCurrentOffset, SeekOrigin.Begin);                       // position to the actual data portion of the directory (not the file descriptor)
                    int numberOfBytesRead = fs.Read(dirEntry.cNAM, 0, dirEntry.cNAM.Length);            // read in the name of the next file/directory
                    dirEntry.cSCT = (byte)fs.ReadByte();                                                // get the LSN of this file/directory's file descriptor
                    dirEntry.cSCT1 = (byte)fs.ReadByte();
                    dirEntry.cSCT2 = (byte)fs.ReadByte();

                    bool bFoundTheEnd = false;
                    for (int j = 0; j < dirEntry.cNAM.Length; j++)
                    {
                        if (bFoundTheEnd)
                            dirEntry.cNAM[j] = 0x00;
                        else
                        {
                            dirEntry.cNAM[j] &= 0x7F;
                            if (dirEntry.cNAM[j] == 0x00)
                            {
                                bFoundTheEnd = true;
                            }
                        }
                    }
                }
                else
                    readPastEndOfFile = true;

                if (!readPastEndOfFile)
                {
                    if ((dirEntry.cNAM[0] != 0xE5) && (dirEntry.cNAM[0] != 0x00))
                    {
                        int nLogicalSector = (dirEntry.cSCT * 65536) + (dirEntry.cSCT1 * 256) + dirEntry.cSCT2;

                        // make sure the LSN that holds the file descriptor is on this disk somewhere.

                        if (nLogicalSector > 0 && nLogicalSector < os9TotalSectors)
                        {
                            string strFilename = ascii.GetString(dirEntry.cNAM);
                            strFilename = strFilename.Replace("\0", "");

                            if (strFilename != "." && strFilename != ".." && strFilename != "")
                            {
                                OS9_FILE_DESCRIPTOR fd = new OS9_FILE_DESCRIPTOR();
                                int nFileDescriptorOffset = ((dirEntry.cSCT * 65536) + (dirEntry.cSCT1 * 256) + dirEntry.cSCT2) * nLSNBlockSize;

                                GetOS9FileDescriptor(fs, ref fd, nFileDescriptorOffset);

                                int nAttributes = fd.cATT;
                                string strAttributes = String.Format
                                    (
                                    "{0}{1}{2}{3}{4}{5}{6}{7}",
                                    (nAttributes & 0x80) == 0 ? "-" : "d",
                                    (nAttributes & 0x40) == 0 ? "-" : "s",
                                    (nAttributes & 0x20) == 0 ? "-" : "e",
                                    (nAttributes & 0x10) == 0 ? "-" : "w",
                                    (nAttributes & 0x08) == 0 ? "-" : "r",
                                    (nAttributes & 0x04) == 0 ? "-" : "e",
                                    (nAttributes & 0x02) == 0 ? "-" : "w",
                                    (nAttributes & 0x01) == 0 ? "-" : "r"
                                    );

                                // If this is a directory then save it to the ArrayList for later processing
                                // of dirs within dirs recursively

                                if (((nAttributes & 0x80) == 0x80) && (nLogicalSector > 2))
                                {
                                    OS9DirEntry tempDirEntry = new OS9DirEntry();

                                    tempDirEntry.strDirectoryName = strFilename;
                                    tempDirEntry.nFileDescriptorSector = nLogicalSector;
                                    tempDirEntry.attributes = strAttributes;
                                    tempDirEntry.nAttributes = nAttributes;

                                    aDirEntries.Add(tempDirEntry);
                                }

                                int nOwner = (fd.cOWN * 256) + fd.cOWN1;
                                int nYear = fd.cDAT % 100;
                                int nMonth = fd.cDAT1 % 100;
                                int nDay = fd.cDAT2 % 100;
                                int nHour = fd.cDAT3 % 100;
                                int nMinute = fd.cDAT4 % 100;

                                string strLastModified = String.Format
                                    (
                                    "{0}/{1}/{2} {3}{4}",
                                    nYear.ToString("00"),
                                    nMonth.ToString("00"),
                                    nDay.ToString("00"),
                                    nHour.ToString("00"),
                                    nMinute.ToString("00")
                                    );

                                int nSector = (dirEntry.cSCT * 65536) + (dirEntry.cSCT1 * 256) + dirEntry.cSCT2;
                                int nByteCount = (fd.cSIZ * 16777216) + (fd.cSIZ1 * 65536) + (fd.cSIZ2 * 256) + fd.cSIZ3;

                                string strLine = String.Format
                                    (
                                    "{0,5} {1}  {2} {3,6} {4,10} {5}\r\n",
                                    nOwner,
                                    strLastModified,
                                    strAttributes,
                                    nSector.ToString("X"),
                                    nByteCount.ToString("X"),
                                    strFilename
                                    );

                                strDescription = strDescription + strLine;

                                // This is where we have reached the file level - last node on this branch.

                                // Instead of saving the file to disk - add the file to the xml documnet node
                                //
                                //      SaveOS9ImageFiles(fs, fd, nAttributes, currentPath, strFilename, nLogicalSector, nByteCount);

                                // first see if this is a directory or a file

                                if ((nAttributes & 0x80) == 0)     // high order bit is set for directories
                                {
                                    // this is a file - 

                                    XmlNode fileNode = doc.CreateElement(strFilename);

                                    XmlAttribute attribute = doc.CreateAttribute("RealName"); attribute.Value = strFilename; fileNode.Attributes.Append(attribute);

                                    XmlAttribute attribute1 = doc.CreateAttribute("FileDesriptorSector"); attribute1.Value = nLogicalSector.ToString(); fileNode.Attributes.Append(attribute1);
                                    XmlAttribute attribute2 = doc.CreateAttribute("ByteCount"); attribute2.Value = nByteCount.ToString(); fileNode.Attributes.Append(attribute2);
                                    XmlAttribute attribute3 = doc.CreateAttribute("Attributes"); attribute3.Value = strAttributes; fileNode.Attributes.Append(attribute3);
                                    XmlAttribute attribute4 = doc.CreateAttribute("attributes"); attribute4.Value = nAttributes.ToString(); fileNode.Attributes.Append(attribute4);

                                    currentNode.AppendChild(fileNode);
                                    os9TotalFiles++;
                                }

                                nRow++;
                            }
                        }
                        else    // if LSN is not a valid logical sector number - treat it as a file with no File descriptor
                        {
                            // we have a directory entry that is pointing to it's file descriptor at sector 0
                            // this is not valid - show error message

                            string strFilename = ascii.GetString(dirEntry.cNAM);
                            strFilename = strFilename.Replace("\0", "");

                            if (strFilename != "." && strFilename != ".." && strFilename != "")
                            {
                                OS9_FILE_DESCRIPTOR fd = new OS9_FILE_DESCRIPTOR();
                                int nFileDescriptorOffset = ((dirEntry.cSCT * 65536) + (dirEntry.cSCT1 * 256) + dirEntry.cSCT2) * nLSNBlockSize;

                                int nAttributes = 0;
                                string strAttributes = String.Format
                                    (
                                    "{0}{1}{2}{3}{4}{5}{6}{7}",
                                    (nAttributes & 0x80) == 0 ? "-" : "d",
                                    (nAttributes & 0x40) == 0 ? "-" : "s",
                                    (nAttributes & 0x20) == 0 ? "-" : "e",
                                    (nAttributes & 0x10) == 0 ? "-" : "w",
                                    (nAttributes & 0x08) == 0 ? "-" : "r",
                                    (nAttributes & 0x04) == 0 ? "-" : "e",
                                    (nAttributes & 0x02) == 0 ? "-" : "w",
                                    (nAttributes & 0x01) == 0 ? "-" : "r"
                                    );

                                // since we cannot read the file descriptor, we will treat this as a file and not a directory

                                int nOwner = 0;
                                int nYear = 70;
                                int nMonth = 1;
                                int nDay = 1;
                                int nHour = 0;
                                int nMinute = 0;

                                string strLastModified = String.Format
                                    (
                                    "{0}/{1}/{2} {3}{4}",
                                    nYear.ToString("00"),
                                    nMonth.ToString("00"),
                                    nDay.ToString("00"),
                                    nHour.ToString("00"),
                                    nMinute.ToString("00")
                                    );

                                int nSector = 0;
                                int nByteCount = 0;

                                string strLine = String.Format
                                    (
                                    "{0,5} {1}  {2} {3,6} {4,10} {5}",
                                    nOwner,
                                    strLastModified,
                                    strAttributes,
                                    nSector.ToString("X"),
                                    nByteCount.ToString("X"),
                                    strFilename
                                    );

                                strDescription = strDescription + strLine + " - invalid file descriptor LSN\r\n";

                                // this will always be treated as a file

                                XmlNode fileNode = doc.CreateElement(strFilename);

                                XmlAttribute attribute = doc.CreateAttribute("RealName"); attribute.Value = strFilename; fileNode.Attributes.Append(attribute);

                                XmlAttribute attribute1 = doc.CreateAttribute("FileDesriptorSector"); attribute1.Value = nLogicalSector.ToString(); fileNode.Attributes.Append(attribute1);
                                XmlAttribute attribute2 = doc.CreateAttribute("ByteCount"); attribute2.Value = nByteCount.ToString(); fileNode.Attributes.Append(attribute2);
                                XmlAttribute attribute3 = doc.CreateAttribute("Attributes"); attribute3.Value = strAttributes; fileNode.Attributes.Append(attribute3);
                                XmlAttribute attribute4 = doc.CreateAttribute("attributes"); attribute4.Value = nAttributes.ToString(); fileNode.Attributes.Append(attribute4);

                                currentNode.AppendChild(fileNode);
                                os9TotalFiles++;

                                nRow++;
                            }

                            MessageBox.Show(string.Format("There is a file [{0}/{1}] that has an invalid fdn lsn of 0x{2}", currentNode.Name, strFilename, nLogicalSector.ToString("X4")));
                            //break;
                        }


                    }

                    nCurrentOffset += 32;
                }
                else
                    break;
            }
        }

        int debugCount = 0;

        private void ProcessOS9Directory(XmlDocument doc, XmlNode currentNode, string currentPath, ref int nRow, ref string strDescription, string strParent, OS9_FILE_DESCRIPTOR fd, FileStream fs, int nSectorsPerTrack)
        {
            debugCount++;

            if (debugCount == 3)
            {
                string messgae = "stop here";
            }

            try
            {
                // this function is recursive (it calls itself) - the recursion ends and we start folding back when we reach a file instead of a directory.

                string thisCurrentPath = Path.Combine(currentPath, strParent).Replace("\\", "/");

                ArrayList aDirEntries = new ArrayList();

                System.Collections.IEnumerator alSEGArrayEnumerator = fd.alSEGArray.GetEnumerator();
                int nDirSize = (fd.cSIZ * 16777216) + (fd.cSIZ1 * 65536) + (fd.cSIZ2 * 256) + fd.cSIZ3; // the number of sectors in the directory segment

                while (alSEGArrayEnumerator.MoveNext())
                {
                    OS9_SEG_ENTRY cSEGEntry = (OS9_SEG_ENTRY)alSEGArrayEnumerator.Current;
                    int nDirSector = cSEGEntry.nSector;
                    int nBlockSize = (cSEGEntry.nSize);

                    if (nDirSector > 0)
                    {
                        long nCurrentOffset = (nDirSector) * nLSNBlockSize;

                        try
                        {

                            ProcessOS9DirSegment(doc, currentNode, ref aDirEntries, ref nRow, fs, ref strDescription, nCurrentOffset, nBlockSize, nDirSize, nSectorsPerTrack, thisCurrentPath);
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show(string.Format("{0} error in ProcessOS9Directory:ProcessOS9DirSegment", e.Message));
                            break;
                        }
                        // subtract the size we justed up from the Directory size
                        //
                        //  32 bytes per entry, 8 entries per block and nBlockSize blocks 

                        nDirSize -= (32 * nBlockSize * 8);
                    }
                }

                // recurse the directories that lie beneath this one.
                //
                //      when we leave the foreach - we will have processed the directories for this branch

                foreach (OS9DirEntry dirEntry in aDirEntries)
                {
                    try
                    {
                        int nFileDescriptorOffset = dirEntry.nFileDescriptorSector * nLSNBlockSize;

                        string strFullPath = String.Format("{0}/{1}", thisCurrentPath, dirEntry.strDirectoryName);
                        if (strParent.Length > 0)
                            strFullPath = String.Format("/{0}/{1}", thisCurrentPath, dirEntry.strDirectoryName);

                        string strLine = String.Format("\r\n\r\n{0}\r\n\r\n", strFullPath);
                        strDescription += strLine;

                        OS9_FILE_DESCRIPTOR fd1 = new OS9_FILE_DESCRIPTOR();
                        try
                        {
                            GetOS9FileDescriptor(fs, ref fd1, nFileDescriptorOffset);

                            // add new node to the xmlDocument

                            int nByteCount = (fd.cSIZ * 16777216) + (fd.cSIZ1 * 65536) + (fd.cSIZ2 * 256) + fd.cSIZ3;

                            try
                            {
                                XmlNode dirNode = doc.CreateElement(dirEntry.strDirectoryName);

                                XmlAttribute attribute = doc.CreateAttribute("RealName"); attribute.Value = dirEntry.strDirectoryName; dirNode.Attributes.Append(attribute);

                                XmlAttribute attribute1 = doc.CreateAttribute("FileDesriptorSector"); attribute1.Value = dirEntry.nFileDescriptorSector.ToString(); dirNode.Attributes.Append(attribute1);
                                XmlAttribute attribute2 = doc.CreateAttribute("ByteCount"); attribute2.Value = nByteCount.ToString(); dirNode.Attributes.Append(attribute2);
                                XmlAttribute attribute3 = doc.CreateAttribute("Attributes"); attribute3.Value = dirEntry.attributes; dirNode.Attributes.Append(attribute3);
                                XmlAttribute attribute4 = doc.CreateAttribute("attributes"); attribute4.Value = dirEntry.nAttributes.ToString(); dirNode.Attributes.Append(attribute4);

                                currentNode.AppendChild(dirNode);
                                os9TotalDirectories++;

                                // this will get the directories that are in this directory
                                //
                                //      on return from ProcessOS9DirSegment aDirEntries will contain an array of directory names with their starting file descriptor sector

                                try
                                {
                                    ProcessOS9Directory(doc, dirNode, thisCurrentPath, ref nRow, ref strDescription, dirEntry.strDirectoryName, fd1, fs, nSectorsPerTrack);
                                }
                                catch (Exception e)
                                {
                                    MessageBox.Show(string.Format("{0} error in ProcessOS9Directory:ProcessOS9Directory\r\ndirectory name: [{1}]", e.Message, dirEntry.strDirectoryName));
                                    break;
                                }
                            }
                            catch (Exception e)
                            {
                                MessageBox.Show(string.Format("{0} error in ProcessOS9Directory:doc.CreateElement(dirEntry.strDirectoryName)\r\ndirectory name: [{1}]", e.Message, dirEntry.strDirectoryName));
                                break;
                            }
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show(string.Format("{0} error in ProcessOS9Directory:GetOS9FileDescriptor\r\ndirectory name: [{1}]", e.Message, dirEntry.strDirectoryName));
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(string.Format("{0} error in ProcessOS9Directory foreach loop", e.Message));
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(string.Format("{0} error in ProcessOS9Directory", e.Message));
            }
        }

        private XmlDocument xmlDoc = new XmlDocument();

        public class PhysicalOS9Geometry
        {
            public int sectorsPerTrack;
            public int numberOfTracks;
            public int numberOfCylinders;
            public int sectorsOnTrackZeroSideZero;
            public byte formatByte;
            public int sectorsOnTrackZeroSideOne;
            public bool doubleDensity = false;
            public bool doubleSided = false;
        }

        public PhysicalOS9Geometry physicalParameters = new PhysicalOS9Geometry();

        public PhysicalOS9Geometry GetPhysicalOS9Geometry (FileStream fs)
        {
            // byte[] cTOT = new byte[3];      // Total Number of sector on media           0x00 00 16 66 
            // byte[] cTKS = new byte[1];      // Number of sector per track                0x03 24 
            // byte[] cMAP = new byte[2];      // Number of bytes in allocation map         0x04 01 CD 
            // byte[] cBIT = new byte[2];      // Number of sectors per cluster             0x06 00 02  
            // byte[] cDIR = new byte[3];      // Starting sector of root directory         0x08 00 00 03 
            // byte[] cOWN = new byte[2];      // Owners user number                        0x0B 00 00 
            // byte[] cATT = new byte[1];      // Disk attributes                           0x0D FF 
            // byte[] cDSK = new byte[2];      // Disk Identification                       0x0E 00 00  
            // byte[] cFMT = new byte[1];      // Disk Format: density, number of sides     0x10 03 
            // byte[] cSPT = new byte[2];      // Number of sectors per track               0x11 00 24 
            // byte[] cRES = new byte[2];      // Reserved for future use                   0x13 00 00 
            // byte[] cBT = new byte[3];      // Starting sector of bootstrap file         0x15 00 08 6D  
            // byte[] cBSZ = new byte[2];      // Size of bootstrap file (in bytes)         0x18 1F F3 
            // byte[] cDAT = new byte[5];      // Time of creation Y:M:D:H:M                0x1A 62 0B 17 0A 28
            // byte[] cNAM = new byte[32];     // Volume name (last char has sign bit set)  0x1F 45 6D 75 4F 73 39 44 69 73 EB 00 00 00 00 00 00 

            // $00 3 DD_TOT Total number of sectors on media
            // $03 1 DD_TKS Track size in sectors
            // $04 2 DD_MAP Number of bytes in allocation map
            // $06 2 DD_BIT Number of sectors/bit (cluster size)
            // $08 3 DD_DIR LSN of root directory file descriptor
            // $0B 2 DD_OWN Owner ID
            // $0D 1 DD_ATT Attributes
            // $0E 2 DD_DSK Disk ID
            // $10 1 DD_FMT Disk Format; density/sides
            //          Bit 0: 0 = single side
            //                 1 = double side
            //          Bit 1: 0 = single density (FM)
            //                 1 = double density (MFM)
            //          Bit 2: 1 = double track (96 TPI/135 TPI)
            //          Bit 3: 1 = quad track density (192 TPI)
            //          Bit 4: 1 = octal track density (384 TPI)
            // $11 2 DD_SPT Sectors/track (two byte value DD_TKS)
            // $13 2 DD_RES Reserved for future use
            // $15 3 DD_BT System bootstrap LSN
            // $18 2 DD_BSZ Size of system bootstrap
            // $1A 5 DD_DAT Creation date
            // $1F 32 DD_NAM Volume name
            // $3F 32 DD_OPT Path descriptor options
            // $5F 1 Reserved
            // $60 4 DD_SYNC Media integrity code
            // $64 4 DD_MapLSN Bitmap starting sector number (0=LSN 1)
            // $68 2 DD_LSNSize Media logical sector size (0=256)
            // $6A 2 DD_VersID Sector 0 Version ID            

            // save current position

            long currentPosition = fs.Position;

            physicalParameters = new PhysicalOS9Geometry();

            int nTotalSectors = 0;
            byte[] temp = new byte[1];
            byte[] nbrofsectorspertrack = new byte[1];
            byte[] formatbyte = new byte[1];

            fs.Seek(0, SeekOrigin.Begin);

            fs.Read(temp, 0, 1);                    // offset 0
            nTotalSectors = (temp[0] * 65536);
            fs.Read(temp, 0, 1);                    // offset 1
            nTotalSectors += (temp[0] * 256);
            fs.Read(temp, 0, 1);                    // offset 2
            nTotalSectors += temp[0];

            fs.Read(nbrofsectorspertrack, 0, 1);   // offset 3
            physicalParameters.sectorsPerTrack = nbrofsectorspertrack[0];

            // get disk format byte

            fs.Seek(16, SeekOrigin.Begin);
            fs.Read(formatbyte, 0, 1);
            physicalParameters.formatByte = formatbyte[0];

            if ((physicalParameters.formatByte & 0x01) == 0x01)
            {
                physicalParameters.doubleSided = true;
            }

            if ((physicalParameters.formatByte & 0x02) == 0x02)
            {
                physicalParameters.doubleDensity = true;
            }

            physicalParameters.numberOfTracks = nTotalSectors / physicalParameters.sectorsPerTrack;

            physicalParameters.sectorsOnTrackZeroSideZero = nTotalSectors - (physicalParameters.numberOfTracks * physicalParameters.sectorsPerTrack);
            if (physicalParameters.sectorsOnTrackZeroSideZero == 0)
                physicalParameters.sectorsOnTrackZeroSideZero = physicalParameters.sectorsPerTrack;

            // now convert it to cylinders
            // leave it actual tracks for following calculation

            if (nTotalSectors != physicalParameters.sectorsPerTrack * physicalParameters.numberOfTracks)
            {
                // all tracks do not have the same number of sectors

                physicalParameters.numberOfTracks++;
            }

            // calculate number of sectors on track zero side one

            if (physicalParameters.doubleSided)
            {
                physicalParameters.numberOfCylinders = physicalParameters.numberOfTracks / 2;

                int sectorsOnTrackOneToEnd = (physicalParameters.numberOfTracks - 1) * physicalParameters.sectorsPerTrack;
                int numberOfSectorsOnTrackZero = nTotalSectors - sectorsOnTrackOneToEnd;

                if (numberOfSectorsOnTrackZero == physicalParameters.sectorsPerTrack)
                    physicalParameters.sectorsOnTrackZeroSideZero = numberOfSectorsOnTrackZero;

                physicalParameters.sectorsOnTrackZeroSideOne = physicalParameters.sectorsPerTrack;
            }
            else
            {
                int sectorsOnTrackOneToEnd = (physicalParameters.numberOfTracks - 1) * physicalParameters.sectorsPerTrack;
                int numberOfSectorsOnTrackZero = nTotalSectors - sectorsOnTrackOneToEnd;

                physicalParameters.sectorsOnTrackZeroSideZero = numberOfSectorsOnTrackZero;
                physicalParameters.sectorsOnTrackZeroSideOne = 0;
            }

            // return to position befor call

            fs.Seek(currentPosition, SeekOrigin.Begin);

            return physicalParameters;
        }

        // using the filename selected by the user - load the directory structure for the tree view in the main dialog with directories as hierachical nodes and the files the end nodes on the brach
        //
        //      We will communicate back to the dialog this file structure via and XML document that can be used to build the directory tree structure for the tree view
        //      as well as be a source of information about the files and directories on the OS9 diskette image  file.

        public XmlDocument LoadOS9DisketteImageFile (string cDrivePathName)
        {
            // always start with a new, clean and polished document

            xmlDoc = new XmlDocument();

            os9VolumeName       = "";
            os9CreationDate     = "";
            os9TotalDirectories = 0;
            os9TotalFiles       = 0;
            os9TotalSectors     = 0;
            os9RemainingSectors = 0;

            string filename = Path.GetFileName(cDrivePathName);

            XmlNode docNode = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
            xmlDoc.AppendChild(docNode);

            // we need to do this because xml does not allow ( or ) or spaces in element names. Nor does it allow emelement names to begin with a number.
            // Element names nust begin with either a letter (a-z, A-Z) or and underscore (_). That's it. So first we will replace spaces and parenthesis
            // with underscore (_). Then we will make sure that the Element name only begins with with either a letter (a-z, A-Z) or and underscore (_).
            //

            string rootNodeElementName = filename.Replace(" ", "_").Replace("(", "_").Replace(")", "_");
            bool isLetter = !String.IsNullOrEmpty(rootNodeElementName) && Char.IsLetter(rootNodeElementName[0]);
            if (!isLetter)
                rootNodeElementName = "_" + rootNodeElementName;

            XmlNode rootNode = xmlDoc.CreateElement(rootNodeElementName);

            currentlyOpenedImageFileName = cDrivePathName;
            ProcessOS9ImageFile(filename, cDrivePathName, xmlDoc, rootNode);

            return xmlDoc;
        }

        #endregion

        #region UniFLEX

        // Handle UniFLEX dsk file

        public class blk
        {
            public byte[] m_blk = new byte[3]; //  block
        };

        public class fdn
        {
            public byte[] m_mode   = new byte[1];    //  file mode              // public byte[]       m_mode   = new byte[1];         //  file mode
            public byte[] m_perms  = new byte[1];    //  file security          // public byte[]       m_perms  = new byte[1];         //  file security
            public byte[] m_links  = new byte[1];    //  number of links        // public byte[]       m_links  = new byte[1];         //  number of links
            public byte[] m_owner  = new byte[2];    //  file owner's ID        // public byte[]       m_owner  = new byte[2];         //  file owner's ID
            public byte[] m_size   = new byte[4];    //  file size              // public byte[]       m_size   = new byte[4];         //  file size
            public blk[]  m_blks   = new blk[13];    //  block list             // public blockList[]  m_blks   = new blockList[13];   //  block list
            public byte[] m_time   = new byte[4];    //  file time              // public byte[]       m_time   = new byte[4];         //  file time
            public byte[] m_fd_pad = new byte[12];   //  padding                // public byte[]       m_fd_pad = new byte[12];        //  padding

            public fdn()
            {
                m_mode   = new byte[1];    //  file mode
                m_perms  = new byte[1];    //  file security
                m_links  = new byte[1];    //  number of links
                m_owner  = new byte[2];    //  file owner's ID
                m_size   = new byte[4];    //  file size
                m_blks   = new blk[13];    //  block list
                m_time   = new byte[4];    //  file time
                m_fd_pad = new byte[12];   //  padding
            }
        };

        public class UniFLEX_SIR
        {
            public byte[] m_supdt  = new byte[1];   //rmb 1       sir update flag                                         0x0200        -> 00 
            public byte[] m_swprot = new byte[1];   //rmb 1       mounted read only flag                                  0x0201        -> 00 
            public byte[] m_slkfr  = new byte[1];   //rmb 1       lock for free list manipulation                         0x0202        -> 00 
            public byte[] m_slkfdn = new byte[1];   //rmb 1       lock for fdn list manipulation                          0x0203        -> 00 
            public byte[] m_sintid = new byte[4];   //rmb 4       initializing system identifier                          0x0204        -> 00 
            public byte[] m_scrtim = new byte[4];   //rmb 4       creation time                                           0x0208        -> 11 44 F3 FC
            public byte[] m_sutime = new byte[4];   //rmb 4       date of last update                                     0x020C        -> 11 44 F1 51
            public byte[] m_sszfdn = new byte[2];   //rmb 2       size in blocks of fdn list                              0x0210        -> 00 4A          = 74
            public byte[] m_ssizfr = new byte[3];   //rmb 3       size in blocks of volume                                0x0212        -> 00 08 1F       = 2079
            public byte[] m_sfreec = new byte[3];   //rmb 3       total free blocks                                       0x0215        -> 00 04 9C       = 
            public byte[] m_sfdnc  = new byte[2];   //rmb 2       free fdn count                                          0x0218        -> 01 B0
            public byte[] m_sfname = new byte[15];  //rmb 14      file system name                                        0x021A        -> 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
            public byte[] m_spname = new byte[15];  //rmb 14      file system pack name                                   0x0228        -> 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
            public byte[] m_sfnumb = new byte[2];   //rmb 2       file system number                                      0x0236        -> 00 00
            public byte[] m_sflawc = new byte[2];   //rmb 2       flawed block count                                      0x0238        -> 00 00
            public byte[] m_sdenf  = new byte[1];   //rmb 1       density flag - 0=single                                 0x023A        -> 01
            public byte[] m_ssidf  = new byte[1];   //rmb 1       side flag - 0=single                                    0x023B        -> 01
            public byte[] m_sswpbg = new byte[3];   //rmb 3       swap starting block number                              0x023C        -> 00 08 20
            public byte[] m_sswpsz = new byte[2];   //rmb 2       swap block count                                        0x023F        -> 01 80
            public byte[] m_s64k   = new byte[1];   //rmb 1       non-zero if swap block count is multiple of 64K         0x0241        -> 00
            public byte[] m_swinc  = new byte[11];  //rmb 11      Winchester configuration info                           0x0242        -> 00 00 00 00 00 00 2A 00 99 00 9A
            public byte[] m_sspare = new byte[11];  //rmb 11      spare bytes - future use                                0x024D        -> 00 9B 00 9C 00 9D 00 9E 00 9F 00
            public byte[] m_snfdn  = new byte[1];   //rmb 1       number of in core fdns                                  0x0258        -> A0           *snfdn * 2 = 320
            public byte[] m_scfdn  = new byte[160]; //rmb CFDN*2  in core free fdns                                       0x0259        variable (*snfdn * 2)
            public byte[] m_snfree = new byte[1];   //rmb 1       number of in core free blocks                           0x03B9        -> 03
            public byte[] m_sfree  = new byte[300]; //rmb         CDBLKS*DSKADS in core free blocks                       0x03BA        -> 

            public fdn[] m_fdn;
        };

        UniFLEX_SIR m_drive_SIR = new UniFLEX_SIR();
        fdn m_rootDirectory = new fdn();

        int m_nAuxIndex2 = 0;
        int m_nAuxIndex3 = 0;

        blk[] m_auxblocks1 = new blk[128];            //  auxillary block list for block 10, 11 and 12
        blk[] m_auxblocks2 = new blk[128];            //  auxillary block list for block 11 and 12
        blk[] m_auxblocks3 = new blk[128];            //  auxillary block list for block 12

        uint m_nFDNCount;

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
            FLOCK = 0x01,         //  %00000001,  //fdn is locked
            FMOD = 0x02,         //  %00000010,  //fdn has been modified
            FTEXT = 0x04,         //  %00000100,  //this is a text segment
            FMNT = 0x08,         //  %00001000,  //fdn is mounted on
            FWLCK = 0x10          //  %00010000   //task awaiting lock
        };

        //* mode codes

        enum fdn_mode_codes
        {
            FBUSY = 0x01,         //  %00000001 fdn is used (busy)
            FSBLK = 0x02,         //  %00000010 block special file
            FSCHR = 0x04,         //  %00000100 character special file
            FSDIR = 0x08,         //  %00001000 directory type file
            FPRDF = 0x10,         //  %00010000 pipe read flag
            FPWRF = 0x20          //  %00100000 pipe write flag
        };

        //* access codes

        enum fdn_access_codes
        {
            FACUR = 0x01,         //  %00000001 user read
            FACUW = 0x02,         //  %00000010 user write
            FACUE = 0x04,         //  %00000100 user execute
            FACOR = 0x08,         //  %00001000 other read
            FACOW = 0x10,         //  %00010000 other write
            FACOE = 0x20,         //  %00100000 other execute
            FXSET = 0x40          //  %01000000 uid execute set
        };


        DateTime UNIXtoDateTime(long seconds)
        {
            double secs = Convert.ToDouble(seconds);
            DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(secs);

            return System.TimeZone.CurrentTimeZone.ToLocalTime(dt);
        }

        string GetFileDateTime(fdn fdnCurrent)
        {
            return ConvertDateTime(fdnCurrent.m_time);
        }

        //
        //  Boot          - Block     0                                0x00000000 - 0x000001FF     00/01 - 00/01
        //  SIR           - Block     1                                0x00000200 - 0x000003FF     00/02 - 00/02
        //  FDN space     - Blocks    2         through   75 (0x004B)  0x00000400 - 0x000097FF     00/03 - 01/20
        //  Volume space  - Blocks   76 (0x4C)  through 2079 (0x081F)  0x00009800 - 0x00103DFF     02/01 - 3F/20
        //  Swap space    - Blocks 2080 (0x820) through 2463 (0x099F)  0x00103E00 - 0x00133FFF     40/01 - 4C/20
        //  *
        //  * System Parameters - adjust accordingly
        //  *
        //
        //  DSKADS      equ     3   disk address size in bytes
        //  DIRSIZ      equ    14   directory entry size (name)
        //  SIGCNT      equ    12   number of system signals
        //  MAPSIZ      equ    13   file map size in fdn
        //  PAGSIZ      equ  4096   smallest allocated memory page
        //  BUFSIZ      equ   512   buffer size
        //  MAXPAG      equ   256   max 4K segments in mainframe
        //  PRCSIZ      equ   256   max size of terminal line
        //  SMAPSZ      equ   256   size of swap allocation area
        //  CBSIZE      equ    32   clist buffer size
        //  CFDN        equ    50   max in core fdns
        //  CDBLKS      equ   100   max in core disk blocks
        //  MAXPAGES    equ    16   Max 4K Pages in 64K
        //  RESTM       equ     9   max system residence time (ticks)
        //  MAXPIP      equ  4096   max data in a pipe (don't go over 5120!)
        //  DPLCNT      equ    10   data pool buffer count
        //  DPLSIZ      equ     9   data pool buffer size
        //  EXCSIZ      equ     8   size of exec name entry
        //  UNFILS      equ    16   max open files / user

        string GetImageTitle(string arg)
        {
            string strDriveFileTitle = Path.GetFileName(arg);
            //Directory.CreateDirectory(strDriveFileTitle);

            return strDriveFileTitle;
        }

        void GetAuxBlocksLevel1(int nAuxIndex, fdn fdnCurrent, FileStream fs, uint blk)
        {
            if (nAuxIndex == 0)
            {
                try
                {
                    uint blk_number = ConvertToInt24(fdnCurrent.m_blks[blk].m_blk);

                    long f = fs.Position;

                    fs.Seek(blk_number * 512, SeekOrigin.Begin);
                    for (int i = 0; i < 128; i++)
                    {
                        m_auxblocks1[i] = new blk();
                        fs.Read(m_auxblocks1[i].m_blk, 0, 3);
                    }
                    fs.Seek(f, SeekOrigin.Begin);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                    //ConsoleOut(e.Message, true);
                    //Console.WriteLine(e.Message);
                }
            }
        }

        void GetAuxBlocksLevel2(int nAuxIndex1, int nAuxIndex2, fdn fdnCurrent, FileStream fs, uint blk)
        {
            if (m_nAuxIndex2 == 0)
            {
                try
                {
                    GetAuxBlocksLevel1(nAuxIndex1, fdnCurrent, fs, blk);

                    uint blk_number = ConvertToInt24(m_auxblocks1[nAuxIndex1].m_blk);

                    long f = fs.Position;
                    fs.Seek(blk_number * 512, SeekOrigin.Begin);
                    for (int i = 0; i < 128; i++)
                    {
                        m_auxblocks2[i] = new blk();
                        fs.Read(m_auxblocks2[i].m_blk, 0, 3);
                    }
                    fs.Seek(f, SeekOrigin.Begin);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                    //ConsoleOut(e.Message, true);
                    //Console.WriteLine(e.Message);
                }
            }
        }

        void GetAuxBlocksLevel3(int nAuxIndex1, int nAuxIndex2, int nAuxIndex3, fdn fdnCurrent, FileStream fs, uint blk)
        {
            if (m_nAuxIndex3 == 0)
            {
                try
                {
                    GetAuxBlocksLevel2(nAuxIndex1, nAuxIndex2, fdnCurrent, fs, blk);

                    uint blk_number = ConvertToInt24(m_auxblocks2[nAuxIndex2].m_blk);

                    long f = fs.Position;
                    fs.Seek(blk_number * 512, SeekOrigin.Begin);
                    for (int i = 0; i < 128; i++)
                    {
                        m_auxblocks3[i] = new blk();
                        fs.Read(m_auxblocks3[i].m_blk, 0, 3);
                    }
                    fs.Seek(f, SeekOrigin.Begin);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                    //ConsoleOut(e.Message, true);
                    //Console.WriteLine(e.Message);
                }
            }
        }

        string FormatMode(byte mode)
        {
            string strMode = "";

            //enum fdn_mode_codes
            //{
            //    FBUSY   = 0x01,         //  %00000001 fdn is used (busy)
            //    FSBLK   = 0x02,         //  %00000010 block special file
            //    FSCHR   = 0x04,         //  %00000100 character special file
            //    FSDIR   = 0x08,         //  %00001000 directory type file
            //    FPRDF   = 0x10,         //  %00010000 pipe read flag
            //    FPWRF   = 0x20          //  %00100000 pipe write flag
            //};

            if ((mode & (byte)fdn_mode_codes.FSDIR) == (byte)fdn_mode_codes.FSDIR)
                strMode += "d";
            else if ((mode & (byte)fdn_mode_codes.FSBLK) == (byte)fdn_mode_codes.FSBLK)
                strMode += "b";
            else if ((mode & (byte)fdn_mode_codes.FSCHR) == (byte)fdn_mode_codes.FSCHR)
                strMode += "c";
            else if ((mode & (byte)fdn_mode_codes.FPRDF) == (byte)fdn_mode_codes.FPRDF)
                strMode += "r";
            else if ((mode & (byte)fdn_mode_codes.FPWRF) == (byte)fdn_mode_codes.FPWRF)
                strMode += "w";
            else strMode += "-";

            return strMode;
        }

        string FormatPerms(byte perms)
        {
            string strPerms = "";

            //enum fdn_perms
            //{
            //    UREAD   = 0x01,   // owner read
            //    UWRITE  = 0x02,   // owner write
            //    UEXEC   = 0x04,   // owner execute
            //    OREAD   = 0x08,   // other read
            //    OWRITE  = 0x10,   // other write
            //    OEXEC   = 0x20,   // other execute
            //    SUID    = 0x40    // set user ID for execute
            //}

            if ((perms & (byte)fdn_perms.UREAD) == (byte)fdn_perms.UREAD) strPerms += "r"; else strPerms += "-";
            if ((perms & (byte)fdn_perms.UWRITE) == (byte)fdn_perms.UWRITE) strPerms += "w"; else strPerms += "-";
            if ((perms & (byte)fdn_perms.UEXEC) == (byte)fdn_perms.UEXEC) strPerms += "x"; else strPerms += "-";
            if ((perms & (byte)fdn_perms.OREAD) == (byte)fdn_perms.OREAD) strPerms += "r"; else strPerms += "-";
            if ((perms & (byte)fdn_perms.OWRITE) == (byte)fdn_perms.OWRITE) strPerms += "w"; else strPerms += "-";
            if ((perms & (byte)fdn_perms.OEXEC) == (byte)fdn_perms.OEXEC) strPerms += "x"; else strPerms += "-";
            if ((perms & (byte)fdn_perms.SUID) == (byte)fdn_perms.SUID) strPerms += "x"; else strPerms += "-";

            return strPerms;
        }

        void LoadSIRAndFDNList(FileStream fs)
        {
            fs.Seek(512, SeekOrigin.Begin);

            fs.Read(m_drive_SIR.m_supdt , 0,   1);    //rmb 1       sir update flag                                         0x0200        -> 00 
            fs.Read(m_drive_SIR.m_swprot, 0,   1);    //rmb 1       mounted read only flag                                  0x0201        -> 00 
            fs.Read(m_drive_SIR.m_slkfr , 0,   1);    //rmb 1       lock for free list manipulation                         0x0202        -> 00 
            fs.Read(m_drive_SIR.m_slkfdn, 0,   1);    //rmb 1       lock for fdn list manipulation                          0x0203        -> 00 
            fs.Read(m_drive_SIR.m_sintid, 0,   4);    //rmb 4       initializing system identifier                          0x0204        -> 00 
            fs.Read(m_drive_SIR.m_scrtim, 0,   4);    //rmb 4       creation time                                           0x0208        -> 11 44 F3 FC
            fs.Read(m_drive_SIR.m_sutime, 0,   4);    //rmb 4       date of last update                                     0x020C        -> 11 44 F1 51
            fs.Read(m_drive_SIR.m_sszfdn, 0,   2);    //rmb 2       size in blocks of fdn list                              0x0210        -> 00 4A          = 74
            fs.Read(m_drive_SIR.m_ssizfr, 0,   3);    //rmb 3       size in blocks of volume                                0x0212        -> 00 08 1F       = 2079
            fs.Read(m_drive_SIR.m_sfreec, 0,   3);    //rmb 3       total free blocks                                       0x0215        -> 00 04 9C       = 
            fs.Read(m_drive_SIR.m_sfdnc , 0,   2);    //rmb 2       free fdn count                                          0x0218        -> 01 B0
            fs.Read(m_drive_SIR.m_sfname, 0,  14);    //rmb 14      file system name                                        0x021A        -> 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
            fs.Read(m_drive_SIR.m_spname, 0,  14);    //rmb 14      file system pack name                                   0x0228        -> 00 00 00 00 00 00 00 00 00 00 00 00 00 00 
            fs.Read(m_drive_SIR.m_sfnumb, 0,   2);    //rmb 2       file system number                                      0x0236        -> 00 00
            fs.Read(m_drive_SIR.m_sflawc, 0,   2);    //rmb 2       flawed block count                                      0x0238        -> 00 00
            fs.Read(m_drive_SIR.m_sdenf , 0,   1);    //rmb 1       density flag - 0=single                                 0x023A        -> 01
            fs.Read(m_drive_SIR.m_ssidf , 0,   1);    //rmb 1       side flag - 0=single                                    0x023B        -> 01
            fs.Read(m_drive_SIR.m_sswpbg, 0,   3);    //rmb 3       swap starting block number                              0x023C        -> 00 08 20
            fs.Read(m_drive_SIR.m_sswpsz, 0,   2);    //rmb 2       swap block count                                        0x023F        -> 01 80
            fs.Read(m_drive_SIR.m_s64k  , 0,   1);    //rmb 1       non-zero if swap block count is multiple of 64K         0x0241        -> 00
            fs.Read(m_drive_SIR.m_swinc , 0,  11);    //rmb 11      Winchester configuration info                           0x0242        -> 00 00 00 00 00 00 2A 00 99 00 9A
            fs.Read(m_drive_SIR.m_sspare, 0,  11);    //rmb 11      spare bytes - future use                                0x024D        -> 00 9B 00 9C 00 9D 00 9E 00 9F 00
            fs.Read(m_drive_SIR.m_snfdn , 0,   1);    //rmb 1       number of in core fdns                                  0x0258        -> A0           *snfdn * 2 = 320
            fs.Read(m_drive_SIR.m_scfdn , 0, 160);    //rmb CFDN*2  in core free fdns                                       0x0259        variable (*snfdn * 2)
            fs.Read(m_drive_SIR.m_snfree, 0,   1);    //rmb 1       number of in core free blocks                           0x03B9        -> 03
            fs.Read(m_drive_SIR.m_sfree , 0, 300);    //rmb         CDBLKS*DSKADS in core free blocks                       0x03BA        -> 

            uint nFDNSize = ConvertToInt16(m_drive_SIR.m_sszfdn);
            m_nFDNCount = (nFDNSize * 512) / 64;

            m_drive_SIR.m_fdn = new fdn[m_nFDNCount];
            for (int i = 0; i < m_nFDNCount; i++)
            {
                try
                {
                    m_drive_SIR.m_fdn[i] = new fdn();
                }
                catch (Exception e)
                {
                    //if (!supressErrors)
                    {
                        MessageBox.Show(e.Message);
                        //ConsoleOut(e.Message, true);
                        //if (swOut == null)
                        //{
                        //    Console.WriteLine(e.Message);
                        //}
                        //else
                        //    swOut.WriteLine(e.Message);
                    }
                }
            }

            fs.Seek(0x00000400, SeekOrigin.Begin);
            for (uint j = 0; j < m_nFDNCount; j++)
            {
                try
                {
                    fs.Read(m_drive_SIR.m_fdn[j].m_mode , 0, 1);     //  file mode
                    fs.Read(m_drive_SIR.m_fdn[j].m_perms, 0, 1);     //  file security
                    fs.Read(m_drive_SIR.m_fdn[j].m_links, 0, 1);     //  number of links
                    fs.Read(m_drive_SIR.m_fdn[j].m_owner, 0, 2);     //  file owner's ID
                    fs.Read(m_drive_SIR.m_fdn[j].m_size , 0, 4);     //  file size
                    for (int i = 0; i < 13; i++)
                    {
                        m_drive_SIR.m_fdn[j].m_blks[i] = new blk();
                        fs.Read(m_drive_SIR.m_fdn[j].m_blks[i].m_blk, 0, 3); //  block list
                    }
                    fs.Read(m_drive_SIR.m_fdn[j].m_time, 0, 4);     //  file time
                    fs.Read(m_drive_SIR.m_fdn[j].m_fd_pad, 0, 12);     //  padding
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                    //ConsoleOut(e.Message, true);
                    //if (swOut == null)
                    //{
                    //    Console.WriteLine(e.Message);
                    //}
                    //else
                    //    swOut.WriteLine(e.Message);
                }
            }
        }

        void PrintImageHeaderInfo(string strPath)
        {
            ASCIIEncoding ascii = new ASCIIEncoding();

            string strFilePackName = ascii.GetString(m_drive_SIR.m_spname);
            string strFileSystemName = ascii.GetString(m_drive_SIR.m_sfname);

            strFilePackName = strFilePackName.Replace('\0', ' ');
            strFilePackName = strFilePackName.Trim();

            strFileSystemName = strFileSystemName.Replace('\0', ' ');
            strFileSystemName = strFileSystemName.Trim();

            string strDisketteCreation = ConvertDateTime(m_drive_SIR.m_scrtim);
            string strLastUpdate = ConvertDateTime(m_drive_SIR.m_sutime);

            uint nFileSystemNumber      = ConvertToInt16(m_drive_SIR.m_sfnumb);
            uint nFlawedBlockCount      = ConvertToInt16(m_drive_SIR.m_sflawc);
            uint nFDNListBlockCount     = ConvertToInt16(m_drive_SIR.m_sszfdn);
            uint nVolumeBlockCount      = ConvertToInt24(m_drive_SIR.m_ssizfr);
            uint nTotalFreeBlocks       = ConvertToInt24(m_drive_SIR.m_sfreec);
            uint nFreeFDNCount          = ConvertToInt16(m_drive_SIR.m_sfdnc);
            uint nSwapStartBlockNumber  = ConvertToInt24(m_drive_SIR.m_sswpbg);
            uint nSwapBlockCount        = ConvertToInt16(m_drive_SIR.m_sswpsz);

            uint nNumberOfInCoreFDNs = m_drive_SIR.m_snfdn[0];
            uint nNumberOfInCoreFreeBlocks = m_drive_SIR.m_snfree[0];

            strDescription = string.Format
                (
                "----------------------------------------------------------\r\n" +
                "UniFLEX formatted diskette: {0}\r\n" +
                "    Created:  {3}\r\n" +
                "    Modified: {4}\r\n" +
                "----------------------------------------------------------\r\n\r\n" +
                "Diskette Image Name:           {0}\n\n" +
                "File Pack Name:                {1}\n" +
                "File System Name:              {2}\n" +
                "Diskette Creation Date:        {3}\n" +
                "Last Updated:                  {4}\n\n" +
                "File System Number:            {5}\n" +
                "Flawed Block Count:            {6}\n" +
                "FDN List Block Count:          {7}\n" +
                "Volume Block Count:            {8}\n" +
                "Total Free Blocks:             {9}\n" +
                "Free FDN Count:                {10}\n" +
                "Swap Start Block Number:       {11}\n" +
                "Swap Block Count:              {12}\n" +
                "Number Of In Core FDNs:        {13}\n" +
                "Number Of In Core Free Blocks: {14}\n\n",
                    strPath,
                    strFilePackName,
                    strFileSystemName,
                    strDisketteCreation,
                    strLastUpdate,
                    nFileSystemNumber,
                    nFlawedBlockCount,
                    nFDNListBlockCount,
                    nVolumeBlockCount,
                    nTotalFreeBlocks,
                    nFreeFDNCount,
                    nSwapStartBlockNumber,
                    nSwapBlockCount,
                    nNumberOfInCoreFDNs,
                    nNumberOfInCoreFreeBlocks
                    );

            uniFLEXVolumeName      = strFileSystemName;
            uniFLEXCreationDate    = strDisketteCreation;
            uniFLEXTotalFreeBlocks = nTotalFreeBlocks;
        }

        uint CopyBlock(int blk_number, BinaryWriter bw, FileStream fs, uint nSize)
        {
            try
            {
                if (blk_number > 0)
                {
                    long f = fs.Position;       // long f = ftell(fpImage);

                    fs.Seek(blk_number * 512, SeekOrigin.Begin);        // fseek(fpImage, blk_number * 512, SEEK_SET);
                    byte[] buffer = new byte[512];

                    fs.Read(buffer, 0, 512);                            // fread(buffer, 1, 512, fpImage);
                    fs.Seek(f, SeekOrigin.Begin);                       // fseek(fpImage, f, SEEK_SET);

                    if (nSize < 512)
                    {
                        bw.Write(buffer, 0, (int)nSize);                     // fwrite(buffer, 1, nSize, fpFile);
                        nSize = 0;
                    }
                    else
                    {
                        bw.Write(buffer, 0, 512);                       // fwrite(buffer, 1, 512, fpFile);
                        nSize -= 512;
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                //ConsoleOut(e.Message, true);
                //Console.WriteLine(e.Message);
            }

            return nSize;
        }

        bool createAndExtractFiles = false;

        /// <summary>
        /// ExtractUniFLEXFile
        /// </summary>
        /// <param name="rootPath"></param>
        /// <param name="doc"></param>
        /// <param name="currentNode"></param>
        /// <param name="fs"></param>
        /// <param name="strCurrentPath"></param>
        /// <param name="directory"></param>
        /// <param name="level"></param>

        public void ExtractUniFLEXFile (string strCurrentPath, string strFname, fdn fdnCurrent, uint blk, uint nFileSize, FileStream fs)
        {
            BinaryWriter bw = new BinaryWriter(File.Open(Path.Combine(strCurrentPath, strFname), FileMode.OpenOrCreate, FileAccess.Write));      // int errFile = fopen_s(&fpFile, strname, "wb");
            if (bw != null)
            {
                int m_nAuxIndex1 = 0;
                int m_nAuxIndex2 = 0;
                int m_nAuxIndex3 = 0;

                int nBlockCount = 0;

                int nLinks = fdnCurrent.m_links[0];
                for (blk = 0; blk < 13; blk++)
                {
                    //
                    //      first 10 blocks allow for a filesize of               10 * 512 bytes  =         5,120 bytes
                    //      block 10        allows for an additional             128 * 512 bytes  =        65,536 bytes
                    //      block 11        allows for an additional       128 * 128 * 512 bytes  =     8,388,608 bytes
                    //      block 12        allows for an additional 128 * 128 * 128 * 512 bytes  = 1,073,741,824 bytes
                    //                                                                              -------------
                    //                                                                              1,082,201,088 bytes

                    uint blk_number = ConvertToInt24(fdnCurrent.m_blks[blk].m_blk);

                    switch (blk)
                    {
                        case 10:        // this is a pointer to a set of 128 blks.
                            GetAuxBlocksLevel1(m_nAuxIndex1, fdnCurrent, fs, blk);
                            blk_number = ConvertToInt24(m_auxblocks1[m_nAuxIndex1].m_blk);
                            m_nAuxIndex1++;
                            if (m_nAuxIndex1 < 128)
                                blk--;
                            else
                                m_nAuxIndex1 = 0;
                            break;

                        case 11:        // this is a pointer to a block of 128 pointers to pointers to 128 blocks
                            GetAuxBlocksLevel2(m_nAuxIndex1, m_nAuxIndex2, fdnCurrent, fs, blk);
                            blk_number = ConvertToInt24(m_auxblocks2[m_nAuxIndex2].m_blk);
                            m_nAuxIndex2++;
                            if (m_nAuxIndex2 < 128)
                                blk--;
                            else
                            {
                                m_nAuxIndex1++;
                                m_nAuxIndex2 = 0;

                                if (m_nAuxIndex1 < 128)
                                {
                                    GetAuxBlocksLevel2(m_nAuxIndex1, m_nAuxIndex2, fdnCurrent, fs, blk);
                                    blk--;
                                }
                                else
                                    m_nAuxIndex1 = 0;
                            }
                            break;

                        case 12:        // this is a pointer to a block of 128 pointers to pointers to 128 blocks to pointers to 128 blocks -> 1,082,201,080 byte file size
                            GetAuxBlocksLevel3(m_nAuxIndex1, m_nAuxIndex2, m_nAuxIndex3, fdnCurrent, fs, blk);
                            blk_number = ConvertToInt24(m_auxblocks3[m_nAuxIndex3].m_blk);
                            m_nAuxIndex3++;
                            if (m_nAuxIndex3 < 128)
                                blk--;
                            else
                            {
                                m_nAuxIndex2++;
                                m_nAuxIndex3 = 0;

                                if (m_nAuxIndex2 < 128)
                                {
                                    GetAuxBlocksLevel3(m_nAuxIndex1, m_nAuxIndex2, m_nAuxIndex3, fdnCurrent, fs, blk);
                                    blk--;
                                }
                                else
                                {
                                    m_nAuxIndex1++;
                                    m_nAuxIndex2 = 0;
                                    m_nAuxIndex3 = 0;

                                    if (m_nAuxIndex1 < 128)
                                    {
                                        GetAuxBlocksLevel3(m_nAuxIndex1, m_nAuxIndex2, m_nAuxIndex3, fdnCurrent, fs, blk);
                                        blk--;
                                    }
                                    else
                                        m_nAuxIndex1 = 0;
                                }
                            }
                            break;

                        default:
                            break;
                    }

                    // This is where we actual transfer the file from the diskette image to the PC file system.

                    //if (m_bVerbose)
                    //{
                    //    if ((nBlockCount % 4) == 0)
                    //        fprintf(fpIndex, "\n                ");
                    //    fprintf(fpIndex, "0x%08X ", blk_number * 512);
                    //}

                    nFileSize = CopyBlock((int)blk_number, bw, fs, nFileSize);
                    nBlockCount++;
                    if (nFileSize == 0)
                    {
                        break;
                    }
                }
                bw.Close();

                //fprintf(fpIndex, "\n\n");
            }
        }

        // Recursive routine to parse the directories and dump the files from the .dsk image to the PC file structure
        //
        //      Call initially with the fdn *directory pointing to the root directory
        //
        void ParseUniFLEXDirectory(string rootPath, ref XmlDocument doc, ref XmlNode currentNode, FileStream fs, string strCurrentPath, fdn directory, int level, int fdnIndex)
        {
            level++;

            uint nSize = ConvertToInt32(directory.m_size);
            byte[] fblk = new byte[2];
            byte[] fname = new byte[15];

            //  Each FDN can have up to 13 block pointers. The first 10 point to actual data blocks and the remaining three are used
            //  to expand the number of blocks that can be assigned to a file by pointer to other blocks that contain FDNs for the 
            //  file. The first of the three (the 11th block pointer) is a list of pointers to an additional 128 FDNs for this file.
            //  The next one takes a level of indicrection further, and the last one takes yet anothe level of indirection further 
            //  than that.

            //  So let's start out by looping on the 13 blocks in the base FDN for this file. The auxillary blocks will be handled 
            //  inside this loop by decrementing the value of this looping index as necessary.

            uint nDirEntry = 0;
            for (int k = 0; k < 13; k++)
            {
                // clear the name of the file so it will be terminated correctly

                for (int i = 0; i < fname.Length; i++)
                    fname[i] = 0x00;

                //  get the next block number from the FDN list for the root directory. This is what we will be traversing recursively
                //  to find directories and files. Each block number is a three byte value (24 bits).

                uint blk = ConvertToInt24(directory.m_blks[k].m_blk);

                if (blk > 0)
                {
                    // if there are any more blocks to process seek to the block pointed to by this FDN entry on the disk

                    fs.Seek(blk * 512, SeekOrigin.Begin);

                    //  Since we know this is a directory we are looking at, we can assume that there are (filesize / 16) entries in the file.
                    //  (a directory is just a special kind of file with 16 bytes per entry).

                    int entry = 0;
                    while (nDirEntry < (nSize / 16))                      //for (uint entry = 0; entry < (nSize / 16); entry++)
                    {
                        try
                        {
                            nDirEntry++;

                            if (++entry > 31)
                                break;

                            fs.Read(fblk, 0, 2);                                // the first 2 bytes are the fdn_block number of the file in the directory
                            uint fdn_block_number = ConvertToInt16(fblk);       // these numbers are 1 based (not 0)

                            if (fdn_block_number > 0)
                            {
                                fs.Read(fname, 0, 14);                          // the next 14 bytes are the name of the file

                                // see if this is a file or a directory or a special block or character mode device

                                if (fdn_block_number < m_nFDNCount)
                                {
                                    fdn fdnCurrent = m_drive_SIR.m_fdn[fdn_block_number - 1];       // get a pointer to the FDN for this file

                                    uint nFileSize = ConvertToInt32(fdnCurrent.m_size);             // get the file size so we only copy the bytes and not the padding

                                    string strFileDateTime = GetFileDateTime(fdnCurrent);         // get the date, mode and permissions
                                    string strMode = FormatMode(fdnCurrent.m_mode[0]);
                                    string strPerms = FormatPerms(fdnCurrent.m_perms[0]);

                                    for (int i = 0; i < fname.Length; i++)
                                    {
                                        if (fname[i] == 0x00)
                                        {
                                            fname[i] = 0x20;
                                        }
                                    }

                                    ASCIIEncoding ascii = new ASCIIEncoding();
                                    string strFname = ascii.GetString(fname);
                                    strFname = strFname.Trim();

                                    // build the line to output the filename and the file particulars to the description along with the appeopriate leading spaces for each level
                                    // of directory hierarchy. Do not show the . and .. directories

                                    if (strFname != "." && strFname != "..")
                                    {
                                        string line = "";

                                        //// place a line before each new dirextory

                                        //if ((fdnCurrent.m_mode[0] & (byte)fdn_mode_codes.FSDIR) == (byte)fdn_mode_codes.FSDIR)
                                        //{
                                        //    line += "\n";
                                        //}

                                        for (int lev = 1; lev < level; lev++)
                                            line += "    ";
                                        line += string.Format("{0} ", strFname.PadRight(14));

                                        // now space over for the file particulars
                                        for (int lev = 4; lev > level; --lev)
                                            line += "    ";
                                        line += string.Format("{0}{1} {2} {3,10}        ", strMode, strPerms, strFileDateTime, nFileSize);

                                        // add this line to the description

                                        strDescription += string.Format("{0}\n", line);
                                    }

                                    // there are certain characters that are not allowed in the name of an XML node
                                    // so let's just fix them all here and be done with it.

                                    string nameForXMLNode = strFname;

                                    if (strFname.Contains("+"))
                                    {
                                        nameForXMLNode = strFname.Replace("+", "_");
                                    }

                                    if (strFname.Contains("$"))
                                    {
                                        nameForXMLNode = nameForXMLNode.Replace("$", "_");
                                    }

                                    if (strFname.Contains("*"))
                                    {
                                        nameForXMLNode = nameForXMLNode.Replace("*", "_");
                                    }

                                    if (strFname.Contains("?"))
                                    {
                                        nameForXMLNode = nameForXMLNode.Replace("?", "_");
                                    }

                                    if (strFname.Contains(" "))
                                    {
                                        nameForXMLNode = nameForXMLNode.Replace(" ", "_");      // MDE: added .Replace(" ", "_") to the following line 2021/02/12 - spaces not allowed in xml element names
                                    }

                                    //  if it is a directory - add this node to the tree, optionally create the directory 
                                    //  in the extracted files directory for this diskette image and then recurse back 
                                    //  into this routine

                                    if (((fdnCurrent.m_perms[0] & 0x80) == 0x80) && !strFname.StartsWith("."))
                                    {
                                        MessageBox.Show("OOPS - fdnCurrent.m_perms[0] has the high order bit set - this is bad");
                                    }


                                    if ((fdnCurrent.m_mode[0] & (byte)fdn_mode_codes.FSDIR) == (byte)fdn_mode_codes.FSDIR)
                                    {
                                        fdnCurrent.m_perms[0] |= 0x80;      // turn on hight order bit to say this is a direrctory. UniFLEX does not use this bit so we can hijack it.

                                        // if it is a directory - recurse back into this routine

                                        if (strFname != "." && strFname != "..")
                                        {
                                            try
                                            {
                                                string strDirname = string.Format("{0}/{1}", strCurrentPath.Replace(@"\", "/"), strFname);

                                                // add to tree as node and set new currect node

                                                //XmlNode dirNode = doc.CreateElement(strDirname.Replace(rootPath, ""));

                                                XmlNode dirNode = doc.CreateElement(nameForXMLNode);
                                                XmlAttribute attribute = doc.CreateAttribute("RealName"); attribute.Value = strFname; dirNode.Attributes.Append(attribute);

                                                // these still need work. Maybe look into using the data in fdnCurrent

                                                /*
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

                                                 */
                                                string strAttributes = String.Format
                                                    (
                                                    "{0}{1}{2}{3}{4}{5}{6}{7}",
                                                    (fdnCurrent.m_perms[0] & 0x80) == 0 ? " " : "d",
                                                    (fdnCurrent.m_perms[0] & 0x40) == 0 ? "-" : "u",
                                                    (fdnCurrent.m_perms[0] & 0x20) == 0 ? "-" : "e",
                                                    (fdnCurrent.m_perms[0] & 0x10) == 0 ? "-" : "w",
                                                    (fdnCurrent.m_perms[0] & 0x08) == 0 ? "-" : "r",
                                                    (fdnCurrent.m_perms[0] & 0x04) == 0 ? "-" : "e",
                                                    (fdnCurrent.m_perms[0] & 0x02) == 0 ? "-" : "w",
                                                    (fdnCurrent.m_perms[0] & 0x01) == 0 ? "-" : "r"
                                                    );

                                                uint size = ConvertToInt32(fdnCurrent.m_size);
                                                string m_blk = fdnCurrent.m_blks[0].m_blk[0].ToString();

                                                allFileDescriptorNodes.Add(fdnCurrent);

                                                XmlAttribute attribute1 = doc.CreateAttribute("blk")            ; attribute1.Value = m_blk                                  ; dirNode.Attributes.Append(attribute1);
                                                XmlAttribute attribute2 = doc.CreateAttribute("ByteCount")      ; attribute2.Value = size.ToString()                        ; dirNode.Attributes.Append(attribute2);
                                                XmlAttribute attribute3 = doc.CreateAttribute("Attributes")     ; attribute3.Value = strAttributes                          ; dirNode.Attributes.Append(attribute3);
                                                XmlAttribute attribute4 = doc.CreateAttribute("attributes")     ; attribute4.Value = fdnCurrent.m_perms[0].ToString()       ; dirNode.Attributes.Append(attribute4);
                                                XmlAttribute attribute5 = doc.CreateAttribute("fdnIndex")       ; attribute5.Value = allFileDescriptorNodes.Count.ToString(); dirNode.Attributes.Append(attribute5);

                                                XmlNode newNode = currentNode.AppendChild(dirNode);

                                                // now create directory in the extracted files directory for this diskette image

                                                //if (createAndExtractFiles)
                                                {
                                                    if (!Directory.Exists(strDirname) && createAndExtractFiles)
                                                        Directory.CreateDirectory(strDirname);
                                                }

                                                if (!strFname.StartsWith("?"))
                                                {
                                                    // and recurse back into this routine

                                                    long f = fs.Position;

                                                    ParseUniFLEXDirectory(rootPath, ref doc, ref newNode, fs, strDirname, fdnCurrent, level, allFileDescriptorNodes.Count - 1);
                                                    fs.Seek(f, SeekOrigin.Begin);
                                                }
                                                else
                                                    break;
                                            }
                                            catch (Exception eCreateDir)
                                            {
                                                MessageBox.Show(eCreateDir.Message);
                                            }
                                        }
                                    }
                                    else            // if this is not a directory or a special device descriptor, add this file name to the tree as a leaf and optionally extract the file
                                    {
                                        string filename = "";
                                        XmlNode fileNode;
                                        XmlAttribute attribute;

                                        try
                                        {
                                            // if this is not a directory or a special device descriptor, extract the file

                                            uniFLEXTotalFiles++;    // only count real files

                                            // add to tree

                                            filename = Path.Combine(strCurrentPath, strFname).Replace(@"\", "/");
                                            fileNode = doc.CreateElement(nameForXMLNode);

                                            attribute = doc.CreateAttribute("RealName"); attribute.Value = strFname; fileNode.Attributes.Append(attribute);

                                            // these still need work. Maybe look into using the data in fdnCurrent

                                            /*
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

                                             */
                                            string strAttributes = String.Format
                                                (
                                                "{0}{1}{2}{3}{4}{5}{6}{7}",
                                                (fdnCurrent.m_perms[0] & 0x80) == 0 ? " " : "d",
                                                (fdnCurrent.m_perms[0] & 0x40) == 0 ? "-" : "u",
                                                (fdnCurrent.m_perms[0] & 0x20) == 0 ? "-" : "e",
                                                (fdnCurrent.m_perms[0] & 0x10) == 0 ? "-" : "w",
                                                (fdnCurrent.m_perms[0] & 0x08) == 0 ? "-" : "r",
                                                (fdnCurrent.m_perms[0] & 0x04) == 0 ? "-" : "e",
                                                (fdnCurrent.m_perms[0] & 0x02) == 0 ? "-" : "w",
                                                (fdnCurrent.m_perms[0] & 0x01) == 0 ? "-" : "r"
                                                );

                                            uint size = ConvertToInt32(fdnCurrent.m_size);
                                            string m_blk = fdnCurrent.m_blks[0].m_blk[0].ToString();

                                            allFileDescriptorNodes.Add(fdnCurrent);

                                            XmlAttribute attribute1 = doc.CreateAttribute("blk")            ; attribute1.Value = m_blk                                  ; fileNode.Attributes.Append(attribute1);
                                            XmlAttribute attribute2 = doc.CreateAttribute("ByteCount")      ; attribute2.Value = size.ToString()                        ; fileNode.Attributes.Append(attribute2);
                                            XmlAttribute attribute3 = doc.CreateAttribute("Attributes")     ; attribute3.Value = strAttributes                          ; fileNode.Attributes.Append(attribute3);
                                            XmlAttribute attribute4 = doc.CreateAttribute("attributes")     ; attribute4.Value = fdnCurrent.m_perms[0].ToString()       ; fileNode.Attributes.Append(attribute4);
                                            XmlAttribute attribute5 = doc.CreateAttribute("fdnIndex")       ; attribute5.Value = allFileDescriptorNodes.Count.ToString(); fileNode.Attributes.Append(attribute5);

                                            currentNode.AppendChild(fileNode);

                                            // if createAndExtractFiles is true - Create and Extract the file

                                            if (createAndExtractFiles)
                                            {
                                                //Console.WriteLine
                                                //    (
                                                //        string.Format(@"strCurrentPath: {0} strFname: {1} fdnCurrent: {2} blk: {3} nFileSize: {4} fs: {5}",
                                                //                strCurrentPath, 
                                                //                strFname, 
                                                //                fdnCurrent.ToString(), 
                                                //                blk.ToString(), 
                                                //                nFileSize.ToString(), 
                                                //                fs.ToString()
                                                //        )
                                                //    );
                                                ExtractUniFLEXFile(strCurrentPath, strFname, fdnCurrent, Convert.ToUInt32(m_blk), nFileSize, fs);
                                            }
                                        }
                                        catch (Exception eCreateFile)
                                        {
                                            MessageBox.Show(eCreateFile.Message);
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show(e.Message);
                            //ConsoleOut(e.Message, true);
                            //Console.WriteLine(e.Message);
                        }
                    }
                }
            }
            //level--;
        }

        public List<fdn> allFileDescriptorNodes = new List<fdn>();

        public int GetUniFLEXDirectory(XmlDocument doc, XmlNode currentNode, ref string strDescription, string path, ulong dwTotalFileSize)
        {
            int nRc = 1;

            FileInfo fi = new FileInfo(path);
            FileStream fs = currentlyOpenedImageFileStream;

            LoadSIRAndFDNList(fs);

            string strCurrentPath = GetImageTitle(path);

            if ((m_drive_SIR.m_fdn[0].m_mode[0] & (byte)fdn_mode_codes.FSDIR) == (byte)fdn_mode_codes.FSDIR)
            {
                // this is a directory - let's do it

                m_rootDirectory = m_drive_SIR.m_fdn[0];

                // traverse the fdn list top to bottom looking for directories

                string currentWorkingDirectory = Directory.GetCurrentDirectory();

                string disketteFileLocation = Path.GetDirectoryName(fs.Name);
                string disketteDirectoryName = Path.GetFileNameWithoutExtension(fs.Name);

                string[] paths = { @".\ExtractedFiles", (disketteFileLocation.Replace(currentWorkingDirectory, "")).TrimStart('\\'), disketteDirectoryName };

                string fullPath = Path.Combine(paths);

                if (!Directory.Exists(fullPath))
                    Directory.CreateDirectory(fullPath);

                PrintImageHeaderInfo(path);

                allFileDescriptorNodes.Add(m_rootDirectory);
                ParseUniFLEXDirectory(strCurrentPath, ref doc, ref currentNode, fs, fullPath, m_rootDirectory, 0, allFileDescriptorNodes.Count - 1);
            }
            return (nRc);
        }

        private void LoadUniFLEXSystemInformationRecord(ref XmlDocument doc, ref XmlNode currentNode, ref string strDescription, string path, ulong dwTotalFileSize)
        {
            allFileDescriptorNodes.Clear();

            if (currentlyOpenedImageFileStream != null)
            {
                LoadSIRAndFDNList(currentlyOpenedImageFileStream);

                string strCurrentPath = GetImageTitle(path);

                if ((m_drive_SIR.m_fdn[0].m_mode[0] & (byte)fdn_mode_codes.FSDIR) == (byte)fdn_mode_codes.FSDIR)
                {
                    // this is a directory - let's do it

                    m_rootDirectory = m_drive_SIR.m_fdn[0];

                    // traverse the fdn list top to bottom looking for directories

                    string currentWorkingDirectory = Directory.GetCurrentDirectory();

                    string disketteFileLocation = Path.GetDirectoryName(currentlyOpenedImageFileStream.Name);
                    string disketteDirectoryName = Path.GetFileNameWithoutExtension(currentlyOpenedImageFileStream.Name);

                    string[] paths = { @".\ExtractedFiles", (disketteFileLocation.Replace(currentWorkingDirectory, "")).TrimStart('\\'), disketteDirectoryName };

                    string fullPath = Path.Combine(paths);

                    //if (createAndExtractFiles)
                    {
                        if (!Directory.Exists(fullPath))
                            Directory.CreateDirectory(fullPath);
                    }

                    PrintImageHeaderInfo(path);

                    string extension = Path.GetExtension(path);

                    string rootPath = path.Replace(@"\", "/").Replace(extension, "");

                    // This is the recursive routine that will traverse the directory structure to build the tree (and extract the files for now).

                    allFileDescriptorNodes.Add(m_rootDirectory);
                    ParseUniFLEXDirectory(rootPath, ref doc, ref currentNode, currentlyOpenedImageFileStream, fullPath, m_rootDirectory, 0, allFileDescriptorNodes.Count - 1);
                }
            }
        }

        private void ProcessUniFLEXImageFile(string filename, string path, XmlDocument doc, XmlNode currentNode)
        {
            ulong dwTotalFileSize = 0;
            strDescription = "";

            try
            {
                FileInfo fi = new FileInfo(path);

                FileStream fs = currentlyOpenedImageFileStream;

                DateTime dtCreate = fi.CreationTime;
                DateTime dtLastAccessed = fi.LastAccessTime;
                DateTime dtLastModified = fi.LastWriteTime;

                dwTotalFileSize = (ulong)fs.Length;

                // Get the directory into pszDescription

                //fileformat ff = GetFileFormat(path, dwTotalFileSize);
                fileformat ff = GetFileFormat();
                if (ff == fileformat.fileformat_UniFLEX)
                {
                    // re-read the file info data

                    fi = new FileInfo(path);

                    dwTotalFileSize = (ulong)fs.Length;

                    LoadUniFLEXSystemInformationRecord(ref doc, ref currentNode, ref strDescription, path, dwTotalFileSize);

                    XmlAttribute attribute1 = doc.CreateAttribute("RealName")   ; attribute1.Value = filename; currentNode.Attributes.Append(attribute1);

                    string strAttributes = String.Format
                        (
                        "{0}{1}{2}{3}{4}{5}{6}{7}",
                        (uniFLEXDiskAttributes & 0x80) == 0 ? " " : "d",
                        (uniFLEXDiskAttributes & 0x40) == 0 ? "-" : "s",
                        (uniFLEXDiskAttributes & 0x20) == 0 ? "-" : "e",
                        (uniFLEXDiskAttributes & 0x10) == 0 ? "-" : "w",
                        (uniFLEXDiskAttributes & 0x08) == 0 ? "-" : "r",
                        (uniFLEXDiskAttributes & 0x04) == 0 ? "-" : "e",
                        (uniFLEXDiskAttributes & 0x02) == 0 ? "-" : "w",
                        (uniFLEXDiskAttributes & 0x01) == 0 ? "-" : "r"
                        );


                    doc.AppendChild(currentNode);

                    Console.WriteLine(strDescription);
                }
                else
                {
                    string strErrorMessage = string.Format("Can not read directory from: {0}", path);
                    alErrors.Add(strErrorMessage);
                    return;
                }

            }
            catch (Exception e)
            {
                if (!supressErrors)
                {
                    MessageBox.Show(e.Message);
                    Console.WriteLine(e.Message);
                }
            }
        }

        public XmlDocument LoadUniFLEXDisketteImageFile(string cDrivePathName)
        {
            // always start with a new, clean and polished document

            xmlDoc = new XmlDocument();

            uniFLEXVolumeName       = "";
            uniFLEXCreationDate     = "";
            uniFLEXTotalDirectories = 0;
            uniFLEXTotalFiles       = 0;
            uniFLEXTotalSectors     = 0;
            uniFLEXRemainingSectors = 0;

            string filename = Path.GetFileName(cDrivePathName);

            XmlNode docNode = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
            xmlDoc.AppendChild(docNode);

            // we need to do this because xml does not allow ( or ) or spaces in element names. Nor does it allow emelement names to begin with a number.
            // Element names nust begin with either a letter (a-z, A-Z) or and underscore (_). That's it. So first we will replace spaces and parenthesis
            // with underscore (_). Then we will make sure that the Element name only begins with with either a letter (a-z, A-Z) or and underscore (_).
            //

            string rootNodeElementName = filename.Replace(" ", "_").Replace("(", "_").Replace(")", "_");
            bool isLetter = !String.IsNullOrEmpty(rootNodeElementName) && Char.IsLetter(rootNodeElementName[0]);
            if (!isLetter)
                rootNodeElementName = "_" + rootNodeElementName;

            XmlNode rootNode = xmlDoc.CreateElement(rootNodeElementName);

            currentlyOpenedImageFileName = cDrivePathName;
            ProcessUniFLEXImageFile(filename, cDrivePathName, xmlDoc, rootNode);

            return xmlDoc;
        }

        #endregion
    }
}
