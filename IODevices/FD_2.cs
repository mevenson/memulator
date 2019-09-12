using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;

using System.IO;
using System.Threading;

namespace Memulator
{
    class FD_2 : IODevice
    {

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

        private bool            m_nFDCWriting  = false;
        private bool            m_nFDCReading  = false;
        private int             m_nFDCReadPtr  = 0;
        private int             m_nFDCWritePtr = 0;


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

        public void CalcFileOffset (int nDrive)
        {
            if ((m_nFDCTrack == 0) && (m_nFDCSector == 0) && (m_nCurrentSideSelected == 0))
                m_lFileOffset = 0L;
            else
            {
                if (Program.DiskFormat[nDrive] == DiskFormats.DISK_FORMAT_MF_FDOS)
                {
                    m_lFileOffset = (long) ((m_nFDCTrack * Program.SectorsPerTrack[nDrive]) + (m_nFDCSector - 1)) * 128L;
                }
                else if ((Program.DiskFormat[nDrive] == DiskFormats.DISK_FORMAT_FLEX) || (Program.DiskFormat[nDrive] == DiskFormats.DISK_FORMAT_UNIFLEX))
                {
                    m_lFileOffset = (long) ((m_nFDCTrack * Program.SectorsPerTrack[nDrive]) + (m_nFDCSector - 1)) * 256L;
                }
                else
                {
                    //if (nDrive == 1)
                    //{
                    //    int x = 1;
                    //}

                    int nSPT    = Program.SectorsPerTrack[nDrive];
                    int nFormat = Program.FormatByte[nDrive];

                    //  OS9 ALWAYS has m_nSectorsOnTrackZero sectors on track 0 side 0
                    //  and uses the side select before seeking to another cylinder.
                    //  track 0 side 1 has m_nSectorsPerTrack sectors.

                    if (m_nFDCTrack == 0)
                        m_lFileOffset = (long) (m_nFDCSector) * 256L;
                    else
                    {
                        // start off with track 0 side 1 already added in if it's double sided. 
                        // We'll get to track 0 side 0 sectors later

                        if ((nFormat & 0x0001) != 0)   // double sided
                        {
                            m_lFileOffset = (long) nSPT * 256L; 

                            // is this is not side 0, add in this tracks side 0 sectors

                            if (m_nCurrentSideSelected != 0)
                                m_lFileOffset += (long) nSPT * 256L; 

                            // the rest of the math is done on cylinders

                            nSPT *= 2;
                        }
                        else
                        {
                            m_lFileOffset = 0L; 
                        }

                        // now add in all of the previous tracks sectors (except track 0)

                        m_lFileOffset += (long) (((m_nFDCTrack - 1) * nSPT) + m_nFDCSector) * 256L;
                    }

                    // if not track 0 side 0 - add in track 0 side 0 sectors 
                    // because it may have a different number of sectors

                    if (!((m_nFDCTrack == 0) && (m_nCurrentSideSelected == 0)))
                    {
                        m_lFileOffset += (long) Program.SectorsOnTrackZero[nDrive] * 256L;
                    }
                }
            }
        }

        public override void Write (ushort m, byte b)
        {
            int nDrive         = m_FDC_DRVRegister & 0x03;
            int nType          = 1;
            int nWhichRegister = m - m_sBaseAddress;
            bool bMultisector;

            switch (nWhichRegister)
            {
                case (int)FDCRegisterOffsets.FDC_DATAREG_OFFSET:
                    m_FDC_DATARegister = b;
                    if (((m_FDC_STATRegister & FDC_BUSY) == FDC_BUSY) && m_nFDCWriting)
                    {
                        m_caWriteBuffer[m_nFDCWritePtr++] = b;
                        if (m_nFDCWritePtr == m_nBytesToTransfer)
                        {
                            // Here is where we will seek and write dsk file

                            CalcFileOffset (nDrive);

                            if (Program.FloppyDriveStream[nDrive] != null)
                            {
                                Program.FloppyDriveStream[nDrive].Seek (m_lFileOffset, SeekOrigin.Begin);
                                Program.FloppyDriveStream[nDrive].Write (m_caWriteBuffer, 0, m_nBytesToTransfer);
                                Program.FloppyDriveStream[nDrive].Flush ();
                            }

                            //Program.pStaticFloppyActivityLight[nDrive]->SetBitmap (Program.m_hGreyDot);

                            m_FDC_STATRegister &= (byte)~(FDC_DRQ | FDC_BUSY);
                            m_FDC_DRVRegister &= (byte)~DRV_DRQ;        // turn off high order bit in drive status register
                            m_nFDCWriting = false;

                            ClearInterrupt ();
                        }
                        else
                        {
                            m_FDC_STATRegister |= (byte)FDC_DRQ;
                            m_FDC_DRVRegister  |= (byte)DRV_DRQ;

                            if (_bInterruptEnabled)
                            {
                                SetInterrupt (_spin);
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
                case (int)FDCRegisterOffsets.FDC_DRVREG_OFFSET:
                    if ((b & 0x40) == 0x40)         // we are selecting side 1
                        m_nCurrentSideSelected = 1;
                    else                            // we are selecting side 0
                        m_nCurrentSideSelected = 0;

                    m_FDC_DRVRegister = b;
                    break;
                case (int)FDCRegisterOffsets.FDC_CMDREG_OFFSET:
                    m_nFDCReading = m_nFDCWriting = false;      // can't be read/writing if commanding
                    m_statusReadsWithoutDataRegisterAccess = 0;

                    switch (b & 0xF0)
                    {
                        // TYPE I

                        case 0x00:  //  0x0X = RESTORE
                            m_nFDCTrack = 0;
		                    m_FDC_TRKRegister = 0;
                            break;

                        case 0x10:  //  0x1X = SEEK
                            m_nFDCTrack = m_FDC_DATARegister;
                            m_FDC_TRKRegister =(byte) m_nFDCTrack;
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
                                m_FDC_TRKRegister = (byte) m_nFDCTrack;
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
                                m_FDC_TRKRegister = (byte)m_nFDCTrack;
                            }
                            break;

                            // TYPE II

                        case 0x80:  //  0x8X = READ  SINGLE   SECTOR
                        case 0x90:  //  0x9X = READ  MULTIPLE SECTORS
                        case 0xA0:  //  0xAX = WRITE SINGLE   SECTOR
                        case 0xB0:  //  0xBX = WRITE MULTIPLE SECTORS

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
                                //Program.pStaticFloppyActivityLight[nDrive]->SetBitmap (Program.m_hRedDot);

                                m_nFDCReading = false;
                                m_nFDCWriting = true;
                                m_nFDCWritePtr = 0;
                                m_statusReadsWithoutDataRegisterAccess = 0;
                                //m_nStatusReads = 0;
                            }
                            else                    // READ
                            {
                                //Program.pStaticFloppyActivityLight[nDrive]->SetBitmap (Program.m_hGreenDot);

                                m_nFDCReading = true;
                                m_nFDCWriting = false;
                                m_nFDCReadPtr = 0;
                                m_statusReadsWithoutDataRegisterAccess = 0;
                                //m_nStatusReads = 0;

                                CalcFileOffset (nDrive);

                                if (Program.FloppyDriveStream[nDrive] != null)
                                {
                                    Program.FloppyDriveStream[nDrive].Seek (m_lFileOffset, SeekOrigin.Begin);
                                    Program.FloppyDriveStream[nDrive].Read (m_caReadBuffer, 0, m_nBytesToTransfer);

                                    m_FDC_DATARegister = m_caReadBuffer[0];
                                }
                            }
                            break;

                            // TYPE III

                        case 0xC0:  //  0xCX = READ ADDRESS
                            nType = 3;
                            break;
                        case 0xE0:  //  0xEX = READ TRACK
                            nType = 3;
                            break;
                        case 0xF0:  //  0xFX = WRITE TRACK
                            nType = 3;
                            break;

                            // TYPE IV

                        case 0xD0:  //  0xDX = FORCE INTERRUPT
                            nType = 4;
                            break;
                    }

                    //  since this may get set as a result of issuing a read command without actually 
                    //  reading anything - just waiting for the controller to go not busy to do a CRC 
                    //  check of the sector only, we need some way to make sure this status gets cleared
                    //  after some time. Lets try by counting the number of times the status is checked
                    //  without a data read and after 256 status reads withiout a data read - we will 
                    //  set the status to not busy.
                    
                    m_FDC_STATRegister = (byte)FDC_BUSY;
                    m_statusReadsWithoutDataRegisterAccess = 0;
                    //m_nStatusReads = 0;

                    // see if current drive is READY

                    if (Program.DriveOpen[nDrive] == true)
                        m_FDC_STATRegister |= (byte)FDC_NOTREADY;
                    else
                        m_FDC_STATRegister &= (byte)~FDC_NOTREADY;

                    switch (nType)
                    {
                        case 1:
                            m_FDC_STATRegister |= (byte)FDC_HDLOADED;
                            if (m_nFDCTrack == 0)
                                m_FDC_STATRegister |= (byte)FDC_TRKZ;
                            else
                                m_FDC_STATRegister &= (byte)~FDC_TRKZ;
                            break;

                        case 2:
                        case 3:
                            if (Program.FloppyDriveStream[nDrive] != null)
                            {
                                m_FDC_STATRegister |= (byte)FDC_DRQ;
                                m_FDC_DRVRegister |= (byte)DRV_DRQ;

                                if (_bInterruptEnabled)
                                {
                                    SetInterrupt (_spin);
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
                                m_FDC_STATRegister &= (byte)~FDC_DRQ;
                                m_FDC_DRVRegister &= (byte)~DRV_DRQ;
                            }
                            break;

                        case 4:
                            m_FDC_STATRegister &= (byte)~(
                                                        FDC_DRQ     |   // clear Data Request Bit
                                                        FDC_SEEKERR |   // clear SEEK Error Bit
                                                        FDC_CRCERR  |   // clear CRC Error Bit
                                                        FDC_RNF     |   // clear Record Not Found Bit
                                                        FDC_BUSY        // clear BUSY bit
                                                      );
                            m_FDC_DRVRegister  &= (byte)~DRV_DRQ;        // turn off high order bit in drive status register
                            break;
                    }
                    break;
                case (int)FDCRegisterOffsets.FDC_TRKREG_OFFSET:
                    m_FDC_TRKRegister = b;
                    m_nFDCTrack = b;
                    break;
                case (int)FDCRegisterOffsets.FDC_SECREG_OFFSET:
                    m_FDC_SECRegister = b;
                    m_nFDCSector = b;
                    break;

                default:
                    Program._cpu.WriteToFirst64K (m, b);
                    break;
            }
        }

        public override byte Read(ushort m)
        {
            byte d;
            int nWhichRegister = m - m_sBaseAddress;
            int nDrive         = m_FDC_DRVRegister & 0x03;           // get active drive

            switch (nWhichRegister)
            {
                case (int)FDCRegisterOffsets.FDC_DATAREG_OFFSET:
                    if (m_nFDCReading)
                    {
                        m_statusReadsWithoutDataRegisterAccess = 0;
                        //m_nStatusReads = 0;
                        m_FDC_DATARegister = m_caReadBuffer[m_nFDCReadPtr++];
                        if (m_nFDCReadPtr == m_nBytesToTransfer)
                        {
                            m_FDC_STATRegister &= (byte)~(FDC_DRQ | FDC_BUSY);
                            m_FDC_DRVRegister &= (byte)~DRV_DRQ;            // turn off high order bit in drive status register

                            m_nFDCReading = false;

                            //Program.pStaticFloppyActivityLight[nDrive]->SetBitmap (Program.m_hGreyDot);

                            ClearInterrupt ();
                        } 
                        // --------------------
                        else
                        {
                            m_FDC_STATRegister |= (byte)DRV_DRQ;
                            m_FDC_DRVRegister |= (byte)DRV_DRQ;
                            if (_bInterruptEnabled)
                            {
                                SetInterrupt(_spin);
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
                        d = m_FDC_DATARegister;
                    }
		            else
                    {
                        d = m_FDC_DATARegister;
                    }
                    break;
                case (int)FDCRegisterOffsets.FDC_STATREG_OFFSET:

                    m_statusReadsWithoutDataRegisterAccess++;

                    if (Program.DriveOpen[nDrive] == true)               // see if current drive is READY
                        m_FDC_STATRegister |= (byte)FDC_NOTREADY;
                    else
                        m_FDC_STATRegister &= (byte)~FDC_NOTREADY;

                    if (Program.WriteProtected[nDrive] == true)          // see if write protected
                        m_FDC_STATRegister |= (byte)FDC_WRTPROTECT;
                    else
                        m_FDC_STATRegister &= (byte)~FDC_WRTPROTECT;

		            if (!m_nFDCReading && !m_nFDCWriting)           // turn off BUSY if not read/writing
                        m_FDC_STATRegister &= (byte)~FDC_BUSY;
                    //else                                            // reading or writing - see if this is just a crc check?
                    //{
                    //    if (++m_statusReadsWithoutDataRegisterAccess > (m_nBytesToTransfer * 2))
                    //    {
                    //        m_FDC_STATRegister &= (byte)~FDC_BUSY;     // clear BUSY if data not read
                    //        ClearInterrupt();
                    //        //Program.pStaticFloppyActivityLight[nDrive]->SetBitmap (Program.m_hGreyDot);
                    //    }
                    //}

                    if ((m_statusReadsWithoutDataRegisterAccess >= (m_nBytesToTransfer / 16)) && (m_nBytesToTransfer > 0) && m_nFDCReading)
                    {
                        m_FDC_STATRegister &= (byte)~FDC_BUSY;     // clear BUSY if data not read
                        ClearInterrupt();
                    }

                    d = m_FDC_STATRegister;                      // get controller status
                    break;
                case (int)FDCRegisterOffsets.FDC_DRVREG_OFFSET:
                    d = (byte)(m_FDC_DRVRegister | 0x40);
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
