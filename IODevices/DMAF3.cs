using System;
using System.Collections;
using System.Collections.Generic;
//using System.Linq;
using System.Text;

using System.Threading;
using System.IO;
using System.Diagnostics;

namespace Memulator
{
    class HARD_DRIVE_PARAMETERS
    {
        public int nCylinders;
        public int nHeads;
        public int nSectorsPerTrack;
        public int nBytesPerSector;
    }

    class DMAF3 : DMAFDevice
    {
        #region variables

        // These are devices that the DMAF-3 has that are not on the DMAF-2

        // CDS stuff

        Stream[] m_fpCDS = new Stream[(int)NumberOfDrives.NUMBER_OF_CDS_DRIVES];
        string[] m_cCDSDrivePathName = new string[(int)NumberOfDrives.NUMBER_OF_CDS_DRIVES];
        string[] m_cCDSDriveType = new string[(int)NumberOfDrives.NUMBER_OF_CDS_DRIVES];
        HARD_DRIVE_PARAMETERS[] m_hdpCDS = new HARD_DRIVE_PARAMETERS[(int)NumberOfDrives.NUMBER_OF_CDS_DRIVES];

        // Winchester (DMAF) stuff

        Stream[] m_fpWinchester = new Stream[(int)NumberOfDrives.NUMBER_OF_WINCHESTER_DRIVES];
        string[] m_cWinchesterDrivePathName = new string[(int)NumberOfDrives.NUMBER_OF_WINCHESTER_DRIVES];
        string[] m_cWinchesterDriveType = new string[(int)NumberOfDrives.NUMBER_OF_WINCHESTER_DRIVES];
        HARD_DRIVE_PARAMETERS[] m_hdpWinchester = new HARD_DRIVE_PARAMETERS[(int)NumberOfDrives.NUMBER_OF_WINCHESTER_DRIVES];
        int m_HDCylinder;
        int m_HDCHead;
        int m_HDSector;

        byte DMAF_WDC_DATARegister;       // WDC data register
        byte DMAF_WDC_ERRORRegister;      // WDC error register
        byte DMAF_WDC_PrecompRegister;    // WDC precomp register
        byte DMAF_WDC_SECCNTRegister;     // WDC sector count (during format)
        byte DMAF_WDC_SECNUMRegister;     // WDC sector number
        byte DMAF_WDC_CYLLORegister;      // WDC cylinder (low part)
        byte DMAF_WDC_CYLHIRegister;      // WDC cylinder (high part)
        byte DMAF_WDC_SDHRegister;        // WDC size/drive/head
        byte DMAF_WDC_STATUSRegister;     // WDC status register
        byte DMAF_WDC_CMDRegister;        // WDC command register

        byte DMAF_CDSCMD0Register;          // 0x0100
        byte DMAF_CDSADR0Register;          // 0x0101
        byte DMAF_CDSFLG0Register;          // 0x0103

        byte DMAF_CDSCMD1Register;          // 0x0300
        byte DMAF_CDSADR1Register;          // 0x0301
        byte DMAF_CDSFLG1Register;          // 0x0103

        #endregion

        public DMAF3()
        {
            latchRegisterIsInverted = false;

            m_nAbort = false;
            m_nCurrentSideSelected = 0;
            m_nCurrentDensitySelected = 0;

            m_nProcessorRunning = false;
            m_nDMAFRunning = false;
            m_nDMAFRate = 1;            // count at 1 MHZ
            m_nEnableT1C_Countdown = false;

            m_nFDCReading = m_nFDCWriting = false;
            m_nFDCReadPtr = 0;
            m_nFDCWritePtr = 0;

            DMAF_WDC_DATARegister      = 0;                                            // WDC data register
            DMAF_WDC_ERRORRegister     = 0;                                            // WDC error register
            DMAF_WDC_SECCNTRegister    = 0;                                            // WDC sector count (during format)
            DMAF_WDC_SECNUMRegister    = 0;                                            // WDC sector number
            DMAF_WDC_CYLLORegister     = 0;                                            // WDC cylinder (low part)
            DMAF_WDC_CYLHIRegister     = 0;                                            // WDC cylinder (high part)
            DMAF_WDC_SDHRegister       = 0;                                            // WDC size/drive/head
            DMAF_WDC_STATUSRegister    = (byte)(WDC_StatusCodes.DMAF_WDC_READY | WDC_StatusCodes.DMAF_WDC_SEEK_COMPLETE);  // WDC status register  - set ready and seek complete
            DMAF_WDC_CMDRegister       = 0;                                            // WDC command register

            DMAF_WD1000_RESRegister = 0;

            for (int i = 0; i < m_DMAF_DMA_CHANNELRegister.Length; i++)
                m_DMAF_DMA_CHANNELRegister[i] = 0;

            for (int i = 0; i < m_DMAF_AT_TCRegister.Length; i++)
                m_DMAF_AT_TCRegister[i] = new DMAF_AT_TC_REGISTER();

            for (int i = 0; i < m_DMAF_DMA_ADDRESSRegister.Length; i++)
                m_DMAF_DMA_ADDRESSRegister[i] = new DMAF_DMA_ADDRESS_REGISTER();

            for (int i = 0; i < m_DMAF_DMA_DMCNTRegister.Length; i++)
                m_DMAF_DMA_DMCNTRegister[i] = new DMAF_DMA_BYTECNT_REGISTER();

            DMAF_AT_IFRRegister = 0;
            DMAF_AT_DTBRegister = 0x20;    // make sure Archive exception bit is OFF
            DMAF_AT_IERRegister = 0;

            for (int nDrive = 0; nDrive < 4; nDrive++)
            {
                m_cWinchesterDriveType[nDrive] = Program.GetConfigurationAttribute(Program.ConfigSection + "/WinchesterDrives/Disk", "TypeName", nDrive.ToString(), "");
                if (m_cWinchesterDriveType[nDrive].Length > 0)
                {
                    m_cWinchesterDrivePathName[nDrive] = Program.GetConfigurationAttribute(Program.ConfigSection + "/WinchesterDrives/Disk", "Path", nDrive.ToString(), "");
                    if (!m_cWinchesterDrivePathName[nDrive].StartsWith(@"\\"))
                    {
                        // this is not a unc path - normalize it by changing \ to /
                        
                        if (!m_cWinchesterDrivePathName[nDrive].Contains(":"))
                        {
                            m_cWinchesterDrivePathName[nDrive] = Path.Combine(Program.dataDir, m_cWinchesterDrivePathName[nDrive].TrimStart('\\').TrimStart('/'));
                        }
                    }

                    if (m_cWinchesterDriveType[nDrive].Length > 0)
                    {
                        SetHardDriveParameters("WinchesterDrives", m_cWinchesterDriveType[nDrive], nDrive);
                        //if ((File.GetAttributes(m_cWinchesterDrivePathName[nDrive]) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                        //    Program._mainForm.SetWinchesterStatus(nDrive, global::SWTPCemuApp.Properties.Resources.reddot);
                        //else
                        //    Program._mainForm.SetWinchesterStatus(nDrive, global::SWTPCemuApp.Properties.Resources.greendot);
                        //Program._mainForm.SetWinchesterActivity(nDrive, global::SWTPCemuApp.Properties.Resources.greydot);
                        //Program._mainForm.SetWinchesterTag(nDrive, m_cWinchesterDrivePathName[nDrive]);
                    }
                }

                m_cWinchesterDriveType[nDrive] = Program.GetConfigurationAttribute(Program.ConfigSection + "/CDSDrives/Disk", "TypeName", nDrive.ToString(), "");
                if (m_cWinchesterDriveType[nDrive].Length > 0)
                {
                    m_cWinchesterDrivePathName[nDrive] = Program.dataDir + Program.GetConfigurationAttribute(Program.ConfigSection + "/CDSDrives/Disk", "Path", nDrive.ToString(), "");
                    if (m_cWinchesterDriveType[nDrive].Length > 0)
                        SetHardDriveParameters("CDSDrives", m_cWinchesterDriveType[nDrive], nDrive);
                }
            }

            //m_fpDMAActivity = null;
            m_nBoardInterruptRegister = 0;
            m_nInterruptingDevice = 0;
            DMAF_HLD_TOGGLERegister = 0;

            _DMAFInterruptDelayTimer = new MicroTimer();
            _DMAFInterruptDelayTimer.MicroTimerElapsed += new MicroTimer.MicroTimerElapsedEventHandler(DMAF_InterruptDelayTimerProc);

            _DMAFTimer = new MicroTimer();
            _DMAFTimer.MicroTimerElapsed += new MicroTimer.MicroTimerElapsedEventHandler(DMAF_TimerProc);

            SetRegisterDescriptions(0x0025);
        }

        // call this to log any activity to any register on the DMAF-3
        //
        //      call with the register being read from or written to, the byte
        //      being read or written and a bool to indicate whether this is a
        //      read or a write operation (default if not specified = read)

        class LogActivityValues
        {
            public ushort instructionPointer;
            public ushort whichRegister;
            public byte value;
            public bool read;
        }

        LogActivityValues previousLogActivity = null;
        int duplicateCount = 0;

        private void LogDMAFActivity(ushort whichRegister, byte value, bool read = true)
        {
            if (Program.DMAF3AccessLogging)
            {
                LogActivityValues thisLogActivityValues = new LogActivityValues();
                thisLogActivityValues.instructionPointer = Program._cpu.CurrentIP;
                thisLogActivityValues.whichRegister = whichRegister;
                thisLogActivityValues.value = value;
                thisLogActivityValues.read = read;

                bool thisIsTheFirst = false;
                bool logIt = false;

                if (previousLogActivity == null)
                {
                    previousLogActivity = new LogActivityValues();
                    previousLogActivity.instructionPointer = Program._cpu.CurrentIP;
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
                        if (previousLogActivity.instructionPointer == thisLogActivityValues.instructionPointer && previousLogActivity.whichRegister == thisLogActivityValues.whichRegister && previousLogActivity.value == thisLogActivityValues.value && previousLogActivity.read == thisLogActivityValues.read)
                        {
                            // this is a duplicate - increment the counter
                            duplicateCount++;
                        }
                        else
                        {
                            if (duplicateCount != 0)
                            {
                                // we have had duplicates - show it

                                using (StreamWriter sw = new StreamWriter(File.Open(Program.activityLogFileDMAF3, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)))
                                {
                                    sw.WriteLine(string.Format(" The above line was duplicated {0} time", duplicateCount));

                                    previousLogActivity.instructionPointer = Program._cpu.CurrentIP;
                                    previousLogActivity.whichRegister = whichRegister;
                                    previousLogActivity.value = value;
                                    previousLogActivity.read = read;

                                    duplicateCount = 0;
                                }
                            }
                            else
                            {
                                previousLogActivity.instructionPointer = Program._cpu.CurrentIP; ;
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
                    using (StreamWriter sw = new StreamWriter(File.Open(Program.activityLogFileDMAF3, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)))
                    {
                        string description = "Unknown";

                        if (registerDescription.ContainsKey(whichRegister))
                        {
                            description = registerDescription[whichRegister];
                        }
                        sw.WriteLine(string.Format("{0}: {1} - {2} {3} (0x{4}) - {5}", Program._cpu.CurrentIP.ToString("X4"), read ? "read " : "write", value.ToString("X2"), read ? "from" : "to  ", whichRegister.ToString("X4"), description));
                    }
                }
            }
        }

        void SetHardDriveParameters(String strDriveType, String strDriveName, int nDrive)
        {
            m_hdpWinchester[nDrive] = new HARD_DRIVE_PARAMETERS();

            int nCylinders = Program.GetConfigurationAttribute(Program.ConfigSection + "/" + strDriveType + "/Disk", "Cylinders", nDrive.ToString(), 0);
            if (nCylinders > 0)
            {
                m_hdpWinchester[nDrive].nCylinders = nCylinders;
                m_hdpWinchester[nDrive].nHeads = Program.GetConfigurationAttribute(Program.ConfigSection + "/" + strDriveType + "/Disk", "Heads", nDrive.ToString(), 0);
                m_hdpWinchester[nDrive].nSectorsPerTrack = Program.GetConfigurationAttribute(Program.ConfigSection + "/" + strDriveType + "/Disk", "SectorsPerTrack", nDrive.ToString(), 0);
                m_hdpWinchester[nDrive].nBytesPerSector = Program.GetConfigurationAttribute(Program.ConfigSection + "/" + strDriveType + "/Disk", "BytesPerSector", nDrive.ToString(), 0);
            }
        }

        // when the timer fires, set the interrupt
        public void DMAF_InterruptDelayTimerProc(object state, MicroTimerEventArgs timerEventArgs)
        {
            _DMAFInterruptDelayTimer.Stop();
            SetInterrupt(_spin);
        }

        //public void DMAF_InterruptDelayTimerProc(object state)
        //{
        //    SetInterrupt();
        //}
        // OK to set the interrupt now - special processing for DMAF since there are so many interrupt sources.

        public override void SetInterrupt(bool spin)
        {
            if ((m_nInterruptingDevice & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA1) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA1)
            {
                m_nInterruptingDevice &= ~(int)DeviceInterruptMask.DEVICE_INT_MASK_DMA1;

                m_DMAF_DMA_CHANNELRegister[0] |= 0x80;     // say channel is interrupting
                if (((m_DMAF_DMA_INTERRUPTRegister & 0x01) == 0x01) && ((m_DMAF_DMA_CHANNELRegister[0] & 0x80) == 0x80))
                    m_DMAF_DMA_INTERRUPTRegister |= 0x80;
            }
            if ((m_nInterruptingDevice & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA2) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA2)
            {
                m_nInterruptingDevice &= ~(int)DeviceInterruptMask.DEVICE_INT_MASK_DMA2;

                m_DMAF_DMA_CHANNELRegister[1] |= 0x80;     // say channel is interrupting
                if (((m_DMAF_DMA_INTERRUPTRegister & 0x02) == 0x02) && ((m_DMAF_DMA_CHANNELRegister[1] & 0x80) == 0x80))
                    m_DMAF_DMA_INTERRUPTRegister |= 0x80;

                DMAF_WDC_STATUSRegister |= (byte)(WDC_StatusCodes.DMAF_WDC_READY | WDC_StatusCodes.DMAF_WDC_SEEK_COMPLETE);   // set ready and seek complete
            }
            if ((m_nInterruptingDevice & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA3) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA3)
            {
                m_nInterruptingDevice &= ~(int)DeviceInterruptMask.DEVICE_INT_MASK_DMA3;

                m_DMAF_DMA_CHANNELRegister[2] |= 0x80;     // say channel is interrupting
                if (((m_DMAF_DMA_INTERRUPTRegister & 0x04) == 0x04) && ((m_DMAF_DMA_CHANNELRegister[2] & 0x80) == 0x80))
                    m_DMAF_DMA_INTERRUPTRegister |= 0x80;
            }
            if ((m_nInterruptingDevice & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA4) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA4)
            {
                m_nInterruptingDevice &= ~(int)DeviceInterruptMask.DEVICE_INT_MASK_DMA4;

                m_DMAF_DMA_CHANNELRegister[3] |= 0x80;     // say channel is interrupting
                if (((m_DMAF_DMA_INTERRUPTRegister & 0x08) == 0x08) && ((m_DMAF_DMA_CHANNELRegister[3] & 0x80) == 0x80))
                    m_DMAF_DMA_INTERRUPTRegister |= 0x80;
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
                DMAF_AT_IFRRegister |= 0x88;       // signal board interrupt through VIA Status register
                DMAF_AT_DTBRegister |= 0x04;       // say it's the WD179x that's doing the interrupting
                DMAF_AT_DTBRegister |= 0x20;       // clear the Archive Exception bit
            }

            if ((m_nInterruptingDevice & (int)DeviceInterruptMask.DEVICE_INT_MASK_WDC) == (int)DeviceInterruptMask.DEVICE_INT_MASK_WDC)
            {
                m_nInterruptingDevice &= ~(int)DeviceInterruptMask.DEVICE_INT_MASK_WDC;

                int nDrive = 0;
                switch (m_DMAF_DRVRegister & 0x0F)
                {
                    case 1: nDrive = 0; break;
                    case 2: nDrive = 1; break;
                    case 4: nDrive = 2; break;
                    case 8: nDrive = 3; break;
                }
            }

            base.SetInterrupt(spin);
        }

        ulong LogicalToPhysicalAddress(byte latch, ushort sLogicalAddress)
        {
            return ((ulong)(latch & (byte)DMAF_ContollerLines.DMAF_EXTADDR_SELECT_LINES) << 16) + (ulong)sLogicalAddress;
        }

        int CalcFileOffset(int nDrive)
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
                        case DiskFormats.DISK_FORMAT_MINIFLEX:
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
                        if ((Program.DiskFormat[nDrive] == DiskFormats.DISK_FORMAT_FLEX) || (Program.DiskFormat[nDrive] == DiskFormats.DISK_FORMAT_UNIFLEX) || (Program.DiskFormat[nDrive] == DiskFormats.DISK_FORMAT_MINIFLEX))
                        {
                            long lBlock = (long)((m_nFDCTrack * Program.SectorsPerTrack[nDrive]) + (m_nFDCSector - 1));
                            m_lFileOffset = lBlock * (long)nSectorSize;
                        }
                        else if (Program.DiskFormat[nDrive] == DiskFormats.DISK_FORMAT_FLEX_IMA)
                        {
                            // this will need some work for real .IMA images with different number of sectors on track zero

                            int sectorPerTrack = Program.SectorsPerTrack[nDrive];
                            int sectorsOnTrack0 = Program.SectorsOnTrackZero[nDrive];

                            // if track 0 - this will take care of itself

                            long lBlock = (long)((m_nFDCTrack * Program.SectorsPerTrack[nDrive]) + (m_nFDCSector - 1));

                            // if this image has a different number of sectors on track than the rest of the image
                            // we need to do some extra calculations - unless we are on track 0

                            if (sectorPerTrack != sectorsOnTrack0 && m_nFDCTrack != 0)
                            {
                                lBlock = ((((m_nFDCTrack - 1) * (Program.SectorsPerTrack[nDrive])) + (m_nFDCSector - 1))) + sectorsOnTrack0;
                            }

                            m_lFileOffset = lBlock * (long)nSectorSize;
                        }
                        else if (Program.DiskFormat[nDrive] == DiskFormats.DISK_FORMAT_OS9)
                        {
                            int nSPT = Program.SectorsPerTrack[nDrive];
                            int nFormat = Program.FormatByte[nDrive];

                            //  OS9 ALWAYS has m_nSectorsOnTrackZero sectors on track 0 side 0
                            //  and uses the side select before seeking to another cylinder.
                            //  track 0 side 1 has m_nSectorsPerTrack sectors.

                            if (m_nFDCTrack == 0)
                                m_lFileOffset = (long)(m_nFDCSector) * (long)nSectorSize;
                            else
                            {
                                // start off with track 0 side 1 already added in if it's double sided. 
                                // We'll get to track 0 side 0 sectors later

                                if ((nFormat & 0x0001) != 0)   // double sided
                                {
                                    m_lFileOffset = (long)nSPT * (long)nSectorSize;

                                    // is this is not side 0, add in this tracks side 0 sectors

                                    if (m_nCurrentSideSelected != 0)
                                        m_lFileOffset += (long)nSPT * (long)nSectorSize;

                                    // the rest of the math is done on cylinders

                                    nSPT *= 2;
                                }
                                else
                                    m_lFileOffset = 0L;

                                // now add in all of the previous tracks sectors (except track 0)

                                m_lFileOffset += (long)(((m_nFDCTrack - 1) * nSPT) + m_nFDCSector) * (long)nSectorSize;
                            }

                            // if not track 0 side 0 - add in track 0 side 0 sectors 
                            // because it may have a different number of sectors

                            if (!((m_nFDCTrack == 0) && (m_nCurrentSideSelected == 0)))
                            {
                                m_lFileOffset += (long)Program.SectorsOnTrackZeroSideZero[nDrive] * (long)nSectorSize;
                            }
                        }
                    }
                }
                else
                    nStatus = (int)DMAF_StatusCodes.DMAF_RNF; // RECORD NOT FOUND
            }
            else
                nStatus = (int)DMAF_StatusCodes.DMAF_RNF; // RECORD NOT FOUND

            return nStatus;
        }

        void CalcHDFileOffset(string strDriveType, WDC_COMMAND nFlag)
        {
            int nDriveType = -1;

            int nDrive = 0;
            int nHead = 0;
            int nCylinder = 0;
            int nSector = 0;

            int nCylinders = 0;
            int nHeads = 0;
            int nSectorsPerTrack = 0;
            int nBytesPerSector = 0;

            if (strDriveType == "CDS")
            {
                nDriveType = 1;

                nDrive = 0;
                nHead = 0;
                nCylinder = 0;
                nSector = 0;

                nCylinders = m_hdpCDS[nDrive].nCylinders;
                nHeads = m_hdpCDS[nDrive].nHeads;
                nSectorsPerTrack = m_hdpCDS[nDrive].nSectorsPerTrack;
                nBytesPerSector = m_hdpCDS[nDrive].nBytesPerSector;
            }
            else if (strDriveType == "Winchester")
            {
                nDriveType = 0;

                nDrive = (DMAF_WDC_SDHRegister >> 3) & 0x03;
                nHead = (DMAF_WDC_SDHRegister) & 0x07;
                nCylinder = ((int)(DMAF_WDC_CYLHIRegister & 0x03) * 256) + (int)DMAF_WDC_CYLLORegister;
                nSector = DMAF_WDC_SECNUMRegister - 1;

                nCylinders = m_hdpWinchester[nDrive].nCylinders;
                nHeads = m_hdpWinchester[nDrive].nHeads;
                nSectorsPerTrack = m_hdpWinchester[nDrive].nSectorsPerTrack;
                nBytesPerSector = m_hdpWinchester[nDrive].nBytesPerSector;
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
                        DMAF_WDC_STATUSRegister &= (byte)(~(byte)WDC_StatusCodes.DMAF_WDC_SEEK_COMPLETE & 0xFF);       // clear RECORD NOT FOUND
                }
                else
                {
                    if (nDriveType == 0)
                        DMAF_WDC_STATUSRegister |= (byte)(WDC_StatusCodes.DMAF_WDC_SEEK_COMPLETE);       // RECORD NOT FOUND
                }
            }
            else
            {
                if (nDriveType == 0)
                    DMAF_WDC_STATUSRegister |= (byte)(WDC_StatusCodes.DMAF_WDC_SEEK_COMPLETE);           // RECORD NOT FOUND
            }
        }

        void LogFloppyActivity(int nDrive, string strMessage, bool bIncludeIP)
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

        void WriteFloppy(ushort m, byte b)
        {
            int nWhichRegister = m - m_sBaseAddress;

            int nDrive = 0;
            int nSectorSize = 256;

            switch (m_DMAF_DRVRegister & 0x0F)
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

            // handle the interrupts in priority order - floppy is on channel 0 so it gets the highest priority

            switch (m_DMAF_DMA_PRIORITYRegister & 0x0F)
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

            byte latch = m_DMAF_LATCHRegister;
            ushort addr = 0;
            ushort cnt = 0;
            byte priority = m_DMAF_DMA_PRIORITYRegister;
            byte interrupt = m_DMAF_DMA_INTERRUPTRegister;
            byte chain = m_DMAF_DMA_CHAINRegister;

            if (activechannel == 0)
            {
                addr = (ushort)(m_DMAF_DMA_ADDRESSRegister[activechannel].hibyte * 256 + m_DMAF_DMA_ADDRESSRegister[activechannel].lobyte);
                cnt = (ushort)(m_DMAF_DMA_DMCNTRegister[activechannel].hibyte * 256 + m_DMAF_DMA_DMCNTRegister[activechannel].lobyte);
            }

            switch ((DMAF_OffsetRegisters)nWhichRegister)
            {
                case DMAF_OffsetRegisters.DMAF_DATAREG_OFFSET:                      // 0xF023
                    {
                        m_DMAF_DATARegister = b;
                        if (((m_DMAF_STATRegister & (byte)DMAF_StatusLines.DMAF_BUSY) == (byte)DMAF_StatusLines.DMAF_BUSY) && m_nFDCWriting)
                        {
                            m_caWriteBuffer[m_nFDCWritePtr++] = b;
                            if (m_nFDCWritePtr == m_nBytesToTransfer)
                            {
                                // Here is where we will seek and write dsk file

                                int nStatus = CalcFileOffset(nDrive);
                                if (nStatus == 0)
                                {
                                    if (Program.FloppyDriveStream[nDrive] != null)
                                    {
                                        Program.FloppyDriveStream[nDrive].Seek(m_lFileOffset, SeekOrigin.Begin);
                                        Program.FloppyDriveStream[nDrive].Write(m_caWriteBuffer, 0, m_nBytesToTransfer);
                                        Program.FloppyDriveStream[nDrive].Flush();
                                    }

                                    //Program._mainForm.SetFloppyActivity(nDrive, global::SWTPCemuApp.Properties.Resources.greydot);

                                    m_DMAF_STATRegister &= (byte)(~((byte)DMAF_StatusLines.DMAF_DRQ | (byte)DMAF_StatusLines.DMAF_BUSY) & 0xFF);

                                    // turn off high order bit in drive status register

                                    m_DMAF_DRVRegister &= (byte)(~(byte)DMAF_ContollerLines.DRV_DRQ & (byte)0xFF);
                                    m_nFDCWriting = false;
                                }
                                else
                                    m_DMAF_STATRegister |= (byte)DMAF_StatusCodes.DMAF_RNF;        // RECORD NOT FOUND
                            }
                            else
                            {
                                m_DMAF_STATRegister |= (byte)DMAF_StatusLines.DMAF_DRQ;
                                m_DMAF_DRVRegister |= (byte)DMAF_ContollerLines.DRV_DRQ;

                                if (_bInterruptEnabled) // && (m_DMAF_LATCHRegister & 0x10) == 0x10)
                                {
                                    // the latch at F040 : (one's complement data sent to controller)
                                    //
                                    //      bit 0..3    is A16...A19 for DMA.
                                    //      bit 4       is INT enable for the FDC..
                                    //                      If set to '1' the FDC INT pin triggers an /IRQ
                                    //                      if this bit get written as a 1, it is stored as a 0
                                    //                      so having a 0 in the latch disables interrupts

                                    StartDMAFInterruptDelayTimer(m_nInterruptDelay, (int)DeviceInterruptMask.DEVICE_INT_MASK_FDC);
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
                    break;

                case DMAF_OffsetRegisters.DMAF_DRVREG_OFFSET:                       // 0xF024
                    {
                        m_DMAF_DRVRegister = b;
                        if ((m_DMAF_DRVRegister & 0x10) == 0x10)        // we are selecting side 1
                            m_nCurrentSideSelected = 1;
                        else                                             // we are selecting side 0
                            m_nCurrentSideSelected = 0;

                        if ((m_DMAF_DRVRegister & 0x20) == 0x20)        // we are selecting single density
                            m_nCurrentDensitySelected = 0;
                        else                                             // we are selecting double density
                            m_nCurrentDensitySelected = 0;
                    }
                    break;

                case DMAF_OffsetRegisters.DMAF_CMDREG_OFFSET:                       // 0xF020
                    {
                        m_DMAF_CMDRegister = b;        // save the command for later use internally

                        if (_bInterruptEnabled)
                            ClearInterrupt();

                        m_nFDCReading = m_nFDCWriting = false;          // can't be read/writing if commanding

                        switch (b & 0xF0)
                        {
                            // TYPE I

                            case 0x00:  //  0x0X = RESTORE
                                m_nFDCTrack = 0;
                                m_DMAF_TRKRegister = 0;
                                break;

                            case 0x10:  //  0x1X = SEEK
                                m_nFDCTrack = m_DMAF_DATARegister;
                                m_DMAF_TRKRegister = (byte)m_nFDCTrack;
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
                                    m_DMAF_TRKRegister = (byte)m_nFDCTrack;
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
                                    m_DMAF_TRKRegister = (byte)m_nFDCTrack;
                                }
                                break;

                            // TYPE II

                            case 0x80:  //  0x8X = READ  SINGLE   SECTOR
                            case 0x90:  //  0x9X = READ  MULTIPLE SECTORS
                            case 0xA0:  //  0xAX = WRITE SINGLE   SECTOR
                            case 0xB0:  //  0xBX = WRITE MULTIPLE SECTORS

                                m_DMAF_STATRegister |= (byte)DMAF_StatusLines.DMAF_HDLOADED;

                                nType = 2;
                                m_nFDCTrack = m_DMAF_TRKRegister;

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
                                    //Program._mainForm.SetFloppyActivity(nDrive, global::SWTPCemuApp.Properties.Resources.reddot);

                                    m_nFDCReading = false;
                                    m_nFDCWriting = true;
                                    m_nFDCWritePtr = 0;
                                    m_nStatusReads = 0;

                                    int nStatus = CalcFileOffset(nDrive);
                                    if (nStatus == 0)
                                    {
                                        if (Program.FloppyDriveStream[nDrive] != null)
                                        {
                                            m_DMAF_STATRegister &= (byte)(~((byte)DMAF_StatusLines.DMAF_DRQ | (byte)DMAF_StatusLines.DMAF_BUSY) & 0xFF);
                                            m_DMAF_DRVRegister &= (byte)(~((byte)DMAF_ContollerLines.DRV_DRQ) & 0xFF);        // turn off high order bit in drive status register
                                            m_nFDCWriting = false;

                                            // if the dma is active - write bytes to the disk from the buffer and reset the dma counter

                                            ulong lPhysicalAddress = LogicalToPhysicalAddress(latch, addr);

                                            if (activechannel == 0) // only handle channel 1 for floppy
                                            {
                                                if (cnt > 0)
                                                {
                                                    //strActivityMessage.Format ("Writing %4d bytes to floppy %d track %02d sector %02d from offset %06X DSK offset 0x%08X\n", cnt, nDrive, m_nFDCTrack, m_nFDCSector, lPhysicalAddress, m_lFileOffset);
                                                    //LogFloppyActivity (nDrive, strActivityMessage);

                                                    Program.FloppyDriveStream[nDrive].Seek(m_lFileOffset, SeekOrigin.Begin);

                                                    // see if we are doing Address Up or Down

                                                    if ((m_DMAF_DMA_CHANNELRegister[activechannel] & 0x08) == 0x08)
                                                        Program.FloppyDriveStream[nDrive].Write(Program._cpu.Memory.MemorySpace, (int)(lPhysicalAddress - cnt), cnt);
                                                    else
                                                        Program.FloppyDriveStream[nDrive].Write(Program._cpu.Memory.MemorySpace, (int)lPhysicalAddress, cnt);

                                                    Program.FloppyDriveStream[nDrive].Flush();

                                                    // see if we are doing Address Up or Down

                                                    ushort a = (ushort)(m_DMAF_DMA_ADDRESSRegister[activechannel].hibyte * 256 + m_DMAF_DMA_ADDRESSRegister[activechannel].lobyte);
                                                    if ((m_DMAF_DMA_CHANNELRegister[activechannel] & 0x08) == 0x08)
                                                        a -= cnt;
                                                    else
                                                        a += cnt;
                                                    m_DMAF_DMA_ADDRESSRegister[activechannel].hibyte = (byte)(a / 256);
                                                    m_DMAF_DMA_ADDRESSRegister[activechannel].lobyte = (byte)(a % 256);

                                                    m_DMAF_STATRegister &= (byte)(~((byte)DMAF_StatusLines.DMAF_DRQ | (byte)DMAF_StatusLines.DMAF_BUSY) & 0xFF);
                                                    m_DMAF_DRVRegister &= (byte)(~((byte)DMAF_ContollerLines.DRV_DRQ) & 0xFF);            // turn off high order bit in drive status register

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
                                        m_DMAF_STATRegister |= (byte)DMAF_StatusCodes.DMAF_RNF;        //  RECORD NOT FOUND
                                }
                                else                    // READ
                                {
                                    //Program._mainForm.SetFloppyActivity(nDrive, global::SWTPCemuApp.Properties.Resources.greendot);

                                    m_nFDCReading = true;
                                    m_nFDCWriting = false;
                                    m_nFDCReadPtr = 0;
                                    m_nStatusReads = 0;

                                    int nStatus = CalcFileOffset(nDrive);
                                    if (nStatus == 0)
                                    {
                                        if (Program.FloppyDriveStream[nDrive] != null)
                                        {
                                            Program.FloppyDriveStream[nDrive].Seek(m_lFileOffset, SeekOrigin.Begin);
                                            Program.FloppyDriveStream[nDrive].Read(m_caReadBuffer, 0, m_nBytesToTransfer);
                                            m_DMAF_DATARegister = m_caReadBuffer[0];

                                            // if the dma is active - get the bytes from memory, write them to the buffer and reset the dma counter

                                            if (activechannel == 0) // only handle channel 1 for now
                                            {
                                                if (cnt > 0)
                                                {
                                                    ulong lPhysicalAddress = LogicalToPhysicalAddress(latch, addr);

                                                    //strActivityMessage.Format ("Reading %4d bytes from floppy %d track %02d sector %02d to   offset %06X DSK offset 0x%08X\n", cnt, nDrive, m_nFDCTrack, m_nFDCSector, lPhysicalAddress, m_lFileOffset);
                                                    //LogFloppyActivity (nDrive, strActivityMessage);

                                                    for (int i = 0; i < cnt; i++)
                                                    {
                                                        if ((m_DMAF_DMA_CHANNELRegister[activechannel] & 0x08) == 0x08)
                                                            Program._cpu.Memory.MemorySpace[(int)lPhysicalAddress - i] = m_caReadBuffer[i];
                                                        else
                                                            Program._cpu.Memory.MemorySpace[(int)lPhysicalAddress + i] = m_caReadBuffer[i];
                                                    }

                                                    ushort a = (ushort)(m_DMAF_DMA_ADDRESSRegister[activechannel].hibyte * 256 + m_DMAF_DMA_ADDRESSRegister[activechannel].lobyte);
                                                    if ((m_DMAF_DMA_CHANNELRegister[activechannel] & 0x08) == 0x08)
                                                        a -= cnt;
                                                    else
                                                        a += cnt;

                                                    m_DMAF_DMA_ADDRESSRegister[activechannel].hibyte = (byte)(a / 256);
                                                    m_DMAF_DMA_ADDRESSRegister[activechannel].lobyte = (byte)(a % 256);

                                                    // turn off BUSY and DRQ in the floppy controller status register
                                                    // and DRQ in the board drive register

                                                    m_DMAF_STATRegister &= (byte)(~((byte)DMAF_StatusLines.DMAF_DRQ | (byte)DMAF_StatusLines.DMAF_BUSY) & 0xFF);
                                                    m_DMAF_DRVRegister &= (byte)(~(byte)DMAF_ContollerLines.DRV_DRQ & 0xFF);            // turn off high order bit in drive status register

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
                                        m_DMAF_STATRegister |= (byte)DMAF_StatusCodes.DMAF_RNF;        //  RECORD NOT FOUND
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
                                        case DiskFormats.DISK_FORMAT_MINIFLEX:
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
                                        if ((Program.DiskFormat[nDrive] == DiskFormats.DISK_FORMAT_FLEX) || (Program.DiskFormat[nDrive] == DiskFormats.DISK_FORMAT_UNIFLEX) || (Program.DiskFormat[nDrive] == DiskFormats.DISK_FORMAT_MINIFLEX))
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
                                                    m_lFileOffset = (long)((m_nFDCTrack * (m_nFormatSectorsPerSide * 2)) + (m_nFDCSector - 1)) * (long)nSectorSize;
                                                else
                                                    m_lFileOffset = (long)((m_nFDCTrack * m_nFormatSectorsPerSide) + (m_nFDCSector - 1)) * (long)nSectorSize;
                                            }
                                            else
                                            {
                                                Program.IsDoubleSided[nDrive] = 1;

                                                Program.NumberOfTracks[nDrive] = m_nFDCTrack + 1;
                                                Program.SectorsPerTrack[nDrive] = (byte)(m_nFormatSectorsPerSide * 2);

                                                m_lFileOffset = (long)((m_nFDCTrack * (m_nFormatSectorsPerSide * 2)) + m_nFormatSectorsPerSide + (m_nFDCSector - 1)) * (long)nSectorSize;
                                            }
                                        }
                                        else if (Program.DiskFormat[nDrive] == DiskFormats.DISK_FORMAT_OS9)
                                        {
                                            int nSPT = m_nFormatSectorsPerSide;
                                            int nFormat = Program.FormatByte[nDrive];

                                            //  OS9 ALWAYS has m_nSectorsOnTrackZero sectors on track 0 side 0
                                            //  and uses the side select before seeking to another cylinder.
                                            //  track 0 side 1 has m_nFormatSectorsPerSide sectors.

                                            if (m_nFDCTrack == 0)
                                                m_lFileOffset = (long)(m_nFDCSector) * (long)nSectorSize;
                                            else
                                            {
                                                // start off with track 0 side 1 already added in if it's double sided. 
                                                // We'll get to track 0 side 0 sectors later

                                                if ((nFormat & 0x0001) != 0)   // double sided
                                                {
                                                    m_lFileOffset = (long)nSPT * (long)nSectorSize;

                                                    // is this is not side 0, add in this tracks side 0 sectors

                                                    if (m_nCurrentSideSelected != 0)
                                                        m_lFileOffset += (long)nSPT * (long)nSectorSize;

                                                    // the rest of the math is done on cylinders

                                                    nSPT *= 2;
                                                }
                                                else
                                                    m_lFileOffset = 0L;

                                                // now add in all of the previous tracks sectors (except track 0)

                                                m_lFileOffset += (long)(((m_nFDCTrack - 1) * nSPT) + m_nFDCSector) * (long)nSectorSize;
                                            }

                                            // if not track 0 side 0 - add in track 0 side 0 sectors 
                                            // because it may have a different number of sectors

                                            if (!((m_nFDCTrack == 0) && (m_nCurrentSideSelected == 0)))
                                            {
                                                m_lFileOffset += (long)Program.SectorsOnTrackZeroSideZero[nDrive] * (long)nSectorSize;
                                            }
                                        }
                                    }

                                    if (nStatus == 0)
                                    {
                                        if (Program.FloppyDriveStream[nDrive] != null)
                                        {
                                            m_DMAF_STATRegister &= (byte)(~((byte)DMAF_StatusLines.DMAF_DRQ | (byte)DMAF_StatusLines.DMAF_BUSY) & 0xFF);
                                            m_DMAF_DRVRegister &= (byte)(~((byte)DMAF_ContollerLines.DRV_DRQ) & 0xFF);        // turn off high order bit in drive status register
                                            m_nFDCWriting = false;

                                            // if the dma is active - write bytes to the disk from the buffer and reset the dma counter

                                            ulong lPhysicalAddress = LogicalToPhysicalAddress(latch, addr);

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

                                                        byte ucIrrelavent1 = Program._cpu.Memory.MemorySpace[(int)((int)lPhysicalAddress + nFormatBufferIndex++)];
                                                        byte ucIrrelavent2 = Program._cpu.Memory.MemorySpace[(int)((int)lPhysicalAddress + nFormatBufferIndex++)];
                                                        byte ucSectorNumber = Program._cpu.Memory.MemorySpace[(int)((int)lPhysicalAddress + nFormatBufferIndex++)];
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

                                                                //Program._mainForm.SetFloppyActivity(nDrive, global::SWTPCemuApp.Properties.Resources.reddot);

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

                                                    ushort a = (ushort)(m_DMAF_DMA_ADDRESSRegister[activechannel].hibyte * 256 + m_DMAF_DMA_ADDRESSRegister[activechannel].lobyte);
                                                    if ((m_DMAF_DMA_CHANNELRegister[activechannel] & 0x08) == 0x08)
                                                        a -= cnt;
                                                    else
                                                        a += cnt;
                                                    m_DMAF_DMA_ADDRESSRegister[activechannel].hibyte = (byte)(a / 256);
                                                    m_DMAF_DMA_ADDRESSRegister[activechannel].lobyte = (byte)(a % 256);

                                                    m_DMAF_STATRegister &= (byte)(~((byte)DMAF_StatusLines.DMAF_DRQ | (byte)DMAF_StatusLines.DMAF_BUSY) & 0xFF);
                                                    m_DMAF_DRVRegister &= (byte)(~((byte)DMAF_ContollerLines.DRV_DRQ) & 0xFF);            // turn off high order bit in drive status register

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
                                        m_DMAF_STATRegister |= (byte)DMAF_StatusCodes.DMAF_RNF;        //  RECORD NOT FOUND
                                }
                                else
                                {
                                    if ((m_nFDCTrack == 0) && ((m_nFDCSector - 1) == 0) && (m_nCurrentSideSelected == 0))
                                        //AfxMessageBox ("Format command is only supported for UniFLEX");

                                        m_DMAF_STATRegister |= (byte)DMAF_StatusLines.DMAF_WRTPROTECT;     // report Write Protected Status
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

                                m_DMAF_STATRegister &= (byte)(~((byte)DMAF_StatusLines.DMAF_BUSY) & 0xFF);   // turn off busy
                                m_DMAF_DRVRegister &= (byte)(~((byte)DMAF_ContollerLines.DRV_DRQ) & 0xFF);   // turn off any DRQ hanging out there

                                break;

                            default:
                                //AfxMessageBox ("Unimplemented Floppy Command");
                                break;
                        }

                        // should ANY command cause BUSY? Maybe - if we clear it later if not.

                        m_DMAF_STATRegister |= (byte)DMAF_StatusLines.DMAF_BUSY;

                        // see if current drive is READY

                        if (Program.DriveOpen[nDrive] == true || Program.FloppyDriveStream[nDrive] == null)
                            m_DMAF_STATRegister |= (byte)DMAF_StatusLines.DMAF_NOTREADY;
                        else
                            m_DMAF_STATRegister &= (byte)(~((byte)DMAF_StatusLines.DMAF_NOTREADY) & 0xFF);

                        switch (nType)
                        {
                            case 1:
                                {
                                    m_DMAF_STATRegister |= (byte)DMAF_StatusLines.DMAF_HDLOADED;
                                    m_DMAF_STATRegister &= (byte)(~((byte)DMAF_StatusLines.DMAF_BUSY | (byte)DMAF_StatusLines.DMAF_DRQ) & 0xFF);

                                    if (m_nFDCTrack == 0)
                                        m_DMAF_STATRegister |= (byte)DMAF_StatusLines.DMAF_TRKZ;
                                    else
                                        m_DMAF_STATRegister &= (byte)(~((byte)DMAF_StatusLines.DMAF_TRKZ) & 0xFF);

                                    m_DMAF_DRVRegister &= (byte)(~((byte)DMAF_ContollerLines.DRV_DRQ) & 0xFF);

                                    if (_bInterruptEnabled) // && (m_DMAF_LATCHRegister & 0x10) == 0x10)
                                    {
                                        // the latch at F025 :
                                        //
                                        //      bit 0..3    is A16...A19 for DMA.
                                        //      bit 4       is INT enable for the FDC..
                                        //                      If set to '1' the FDC INT pin triggers an /IRQ
                                        //                      if this bit get written as a 1, it is stored as a 0
                                        //                      so having a 0 in the latch disables interrupts

                                        StartDMAFInterruptDelayTimer(m_nInterruptDelay, (int)DeviceInterruptMask.DEVICE_INT_MASK_FDC);
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
                                break;

                            case 2:
                            case 3:
                                {
                                    if (Program.FloppyDriveStream[nDrive] != null)
                                    {
                                        if (cnt == 0)
                                        {
                                            // we get here during polled IO becasue will still be 0. If the DMA did
                                            // the work, the count will be the number of btyes it transferred.

                                            m_DMAF_STATRegister |= ((byte)DMAF_StatusLines.DMAF_DRQ | (byte)DMAF_StatusLines.DMAF_BUSY);
                                            m_DMAF_DRVRegister |= (byte)DMAF_ContollerLines.DRV_DRQ;

                                            if (_bInterruptEnabled) // && (m_DMAF_LATCHRegister & 0x10) == 0x10)
                                            {
                                                // the latch at F025 :
                                                //
                                                //      bit 0..3    is A16...A19 for DMA.
                                                //      bit 4       is INT enable for the FDC..
                                                //                      If set to '1' the FDC INT pin triggers an /IRQ
                                                //                      if this bit get written as a 1, it is stored as a 0
                                                //                      so having a 0 in the latch disables interrupts

                                                StartDMAFInterruptDelayTimer(m_nInterruptDelay, (int)DeviceInterruptMask.DEVICE_INT_MASK_FDC);
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
                                            // if the DMA did the transfer - clear the DRQ bits

                                            if ((m_DMAF_STATRegister & (byte)DMAF_StatusLines.DMAF_WRTPROTECT) == 0)
                                                m_DMAF_STATRegister = (byte)DMAF_StatusCodes.DMAF_MOTOR_ON;
                                            else
                                                m_DMAF_STATRegister |= (byte)DMAF_StatusCodes.DMAF_MOTOR_ON;

                                            if (_bInterruptEnabled)
                                                ClearInterrupt();       // in case it is already set

                                            // clear all the error codes and clear the DMAF board DRQ signal

                                            m_DMAF_STATRegister &= (byte)(~((byte)DMAF_StatusCodes.DMAF_LOST_DATA | (byte)DMAF_StatusCodes.DMAF_CRCERR | (byte)DMAF_StatusCodes.DMAF_RNF | (byte)DMAF_StatusCodes.DMAF_RECORD_TYPE_SPIN_UP) & 0xFF);
                                            m_DMAF_DRVRegister &= (byte)(~((byte)DMAF_ContollerLines.DRV_DRQ) & 0xFF);

                                            // clear the DMA count registers

                                            m_DMAF_DMA_DMCNTRegister[0].hibyte = m_DMAF_DMA_DMCNTRegister[0].lobyte = 0;

                                            // the latch at $F025 has no control over the setting of the interrupt for the DMA
                                            // so do not check bit 4 in the latch at $F040

                                            if (_bInterruptEnabled) // && (m_DMAF_LATCHRegister & 0x10) == 0x10)
                                            {
                                                // the timer started by StartDMAFInterruptDelayTimer will set the interrupt when the timer expires.

                                                // the latch at F025 : 
                                                //
                                                //      bit 0..3    is A16...A19 for DMA.
                                                //      bit 4       is INT enable for the FDC..
                                                //                      If set to '1' the FDC INT pin triggers an /IRQ
                                                //                      if this bit get written as a 1, it is stored as a 0
                                                //                      so having a 0 in the latch disables interrupts

                                                if ((m_DMAF_LATCHRegister & 0x10) == 0x10)
                                                {
                                                    // see if the interrupt control register on the DMA is allowing the DMA to interrupt

                                                    if ((m_DMAF_DMA_INTERRUPTRegister & 0x01) == 0x01)
                                                        StartDMAFInterruptDelayTimer(m_nInterruptDelay, (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA1 | (int)DeviceInterruptMask.DEVICE_INT_MASK_FDC);
                                                    else
                                                        StartDMAFInterruptDelayTimer(m_nInterruptDelay, (int)DeviceInterruptMask.DEVICE_INT_MASK_FDC);
                                                }
                                                else
                                                {
                                                    // we must always set this bit when DMA is finished whether we set the interrupt or not

                                                    m_DMAF_DMA_CHANNELRegister[0] |= 0x80;     // say channel is interrupting

                                                    // now see if we should set the interrupt register as well.

                                                    if ((m_DMAF_DMA_INTERRUPTRegister & 0x01) == 0x01)
                                                        StartDMAFInterruptDelayTimer(m_nInterruptDelay, (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA1);
                                                }

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
                                        m_DMAF_STATRegister |= (byte)DMAF_StatusLines.DMAF_NOTREADY;
                                        m_DMAF_STATRegister &= (byte)(~((byte)DMAF_StatusLines.DMAF_DRQ) & 0xFF);
                                        m_DMAF_DRVRegister &= (byte)(~((byte)DMAF_ContollerLines.DRV_DRQ) & 0xFF);

										if ((m_DMAF_LATCHRegister & 0x10) == 0x10)
                                        	StartDMAFInterruptDelayTimer(m_nInterruptDelay, (int)DeviceInterruptMask.DEVICE_INT_MASK_FDC);
                                    }
                                }
                                break;

                            case 4:
                                {
                                    // used to include:
                                    //DMAF_StatusCodes.DMAF_CRCERR |   // clear CRC Error Bit
                                    //DMAF_StatusCodes.DMAF_RNF    |   // clear Record Not Found Bit

                                    m_DMAF_STATRegister &= (byte)(~((byte)(
                                                                DMAF_StatusLines.DMAF_DRQ |   // clear Data Request Bit
                                                                DMAF_StatusLines.DMAF_SEEKERR |   // clear SEEK Error Bit
                                                                DMAF_StatusLines.DMAF_BUSY       // clear BUSY bit
                                                             )) & 0xFF);
                                    m_DMAF_DRVRegister &= (byte)(~((byte)DMAF_ContollerLines.DRV_DRQ) & 0xFF);        // turn off high order bit in drive status register

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

                                        if (_bInterruptEnabled) // && (m_DMAF_LATCHRegister & 0x10) == 0x10)
                                        {
                                            // the latch at F025 : 
                                            //
                                            //      bit 0..3    is A16...A19 for DMA.
                                            //      bit 4       is INT enable for the FDC..
                                            //                      If set to '1' the FDC INT pin triggers an /IRQ
                                            //                      if this bit get written as a 1, it is stored as a 0
                                            //                      so having a 0 in the latch disables interrupts

                                            StartDMAFInterruptDelayTimer(m_nInterruptDelay, (int)DeviceInterruptMask.DEVICE_INT_MASK_FDC);
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
                        }
                    }
                    break;

                case DMAF_OffsetRegisters.DMAF_TRKREG_OFFSET:                       // 0xF021
                    m_DMAF_TRKRegister = b;
                    m_nFDCTrack = b;
                    break;

                case DMAF_OffsetRegisters.DMAF_SECREG_OFFSET:                       // 0xF022
                    m_DMAF_SECRegister = b;
                    m_nFDCSector = b;
                    break;

                case DMAF_OffsetRegisters.DMAF_HLD_TOGGLE_OFFSET:                   // 0xF050 - head load toggle
                    DMAF_HLD_TOGGLERegister = b;
                    break;
            }
        }

        void WriteDMA(ushort m, byte b)
        {
            int nWhichRegister = m - m_sBaseAddress;

            switch (nWhichRegister)
            {
                case (int)DMAF_OffsetRegisters.DMAF3_LATCH_OFFSET:
                    m_DMAF_LATCHRegister = b;
                    break;

                //*
                //*   DMAF 6844 DMA controller definitions
                //*

                case (int)DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET + 0:
                    m_DMAF_DMA_ADDRESSRegister[0].hibyte = b;
                    break;

                case (int)DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET + 1:
                    m_DMAF_DMA_ADDRESSRegister[0].lobyte = b;
                    break;

                case (int)DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET + 4:
                    m_DMAF_DMA_ADDRESSRegister[1].hibyte = b;
                    break;

                case (int)DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET + 5:
                    m_DMAF_DMA_ADDRESSRegister[1].lobyte = b;
                    break;

                case (int)DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET + 8:
                    m_DMAF_DMA_ADDRESSRegister[2].hibyte = b;
                    break;

                case (int)DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET + 9:
                    m_DMAF_DMA_ADDRESSRegister[2].lobyte = b;
                    break;

                case (int)DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET + 12:
                    m_DMAF_DMA_ADDRESSRegister[3].hibyte = b;
                    break;

                case (int)DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET + 13:
                    m_DMAF_DMA_ADDRESSRegister[3].lobyte = b;
                    break;

                case (int)DMAF_OffsetRegisters.DMAF_DMA_DMCNT_OFFSET + 0:
                    m_DMAF_DMA_DMCNTRegister[0].hibyte = b;
                    break;

                case (int)DMAF_OffsetRegisters.DMAF_DMA_DMCNT_OFFSET + 1:
                    m_DMAF_DMA_DMCNTRegister[0].lobyte = b;
                    break;

                case (int)DMAF_OffsetRegisters.DMAF_DMA_DMCNT_OFFSET + 4:
                    m_DMAF_DMA_DMCNTRegister[1].hibyte = b;
                    break;

                case (int)DMAF_OffsetRegisters.DMAF_DMA_DMCNT_OFFSET + 5:
                    m_DMAF_DMA_DMCNTRegister[1].lobyte = b;
                    break;

                case (int)DMAF_OffsetRegisters.DMAF_DMA_DMCNT_OFFSET + 8:
                    m_DMAF_DMA_DMCNTRegister[2].hibyte = b;
                    break;

                case (int)DMAF_OffsetRegisters.DMAF_DMA_DMCNT_OFFSET + 9:
                    m_DMAF_DMA_DMCNTRegister[2].lobyte = b;
                    break;

                case (int)DMAF_OffsetRegisters.DMAF_DMA_DMCNT_OFFSET + 12:
                    m_DMAF_DMA_DMCNTRegister[3].hibyte = b;
                    break;

                case (int)DMAF_OffsetRegisters.DMAF_DMA_DMCNT_OFFSET + 13:
                    m_DMAF_DMA_DMCNTRegister[3].lobyte = b;
                    break;

                case (int)DMAF_OffsetRegisters.DMAF_DMA_CHANNEL_OFFSET:
                    m_DMAF_DMA_CHANNELRegister[0] = (byte)(b & 0x0F);
                    break;
                case (int)DMAF_OffsetRegisters.DMAF_DMA_CHANNEL_OFFSET + 1:
                    m_DMAF_DMA_CHANNELRegister[1] = (byte)(b & 0x0F);
                    break;
                case (int)DMAF_OffsetRegisters.DMAF_DMA_CHANNEL_OFFSET + 2:
                    m_DMAF_DMA_CHANNELRegister[2] = (byte)(b & 0x0F);
                    break;
                case (int)DMAF_OffsetRegisters.DMAF_DMA_CHANNEL_OFFSET + 3:
                    m_DMAF_DMA_CHANNELRegister[3] = (byte)(b & 0x0F);
                    break;

                case (int)DMAF_OffsetRegisters.DMAF_DMA_PRIORITY_OFFSET:
                    m_DMAF_DMA_PRIORITYRegister = b;
                    break;

                case (int)DMAF_OffsetRegisters.DMAF_DMA_INTERRUPT_OFFSET:
                    m_DMAF_DMA_INTERRUPTRegister = (byte)(b & 0x7F);
                    break;

                case (int)DMAF_OffsetRegisters.DMAF_DMA_CHAIN_OFFSET:
                    m_DMAF_DMA_CHAINRegister = b;
                    break;

            }
        }

        void WriteWD1000(ushort m, byte b)
        {
            string strDriveType = "Winchester";

            int nWhichRegister = m - m_sBaseAddress;
            int activechannel = -1;

            switch (m_DMAF_DMA_PRIORITYRegister & 0x0F)
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

            byte latch = m_DMAF_LATCHRegister;
            ushort addr = 0;
            ushort cnt = 0;
            byte priority = m_DMAF_DMA_PRIORITYRegister;
            byte interrupt = m_DMAF_DMA_INTERRUPTRegister;
            byte chain = m_DMAF_DMA_CHAINRegister;

            if (activechannel == 1)
            {
                addr = (ushort)(m_DMAF_DMA_ADDRESSRegister[activechannel].hibyte * 256 + m_DMAF_DMA_ADDRESSRegister[activechannel].lobyte);
                cnt = (ushort)(m_DMAF_DMA_DMCNTRegister[activechannel].hibyte * 256 + m_DMAF_DMA_DMCNTRegister[activechannel].lobyte);
            }

            int nDrive = (DMAF_WDC_SDHRegister >> 3) & 0x03;

            //  TODO: Check for valid drive attached to controller on this port
            //
            //  There should be a check here to make sure that m_hdpWinchester[nDrive] is not null
            //
            //      If it is null - pretend like there is no drive attached to the controller 
            //      with this drive number

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
            //LogFloppyActivity (nDrive, strActivityMessage);

            switch (nWhichRegister)
            {
                //  WD1000 5" Winchester interface

                case (int)DMAF_OffsetRegisters.DMAF_WDC_DATA_OFFSET:         // 0x30 - WDC data register
                    DMAF_WDC_DATARegister = b;
                    break;

                case (int)DMAF_OffsetRegisters.DMAF_WDC_PRECOMP_OFFSET:      // 0x31 - WDC error register
                    DMAF_WDC_PrecompRegister = b;
                    break;

                case (int)DMAF_OffsetRegisters.DMAF_WDC_SECCNT_OFFSET:       // 0x32 - sector count (during format)
                    DMAF_WDC_SECCNTRegister = b;
                    break;

                case (int)DMAF_OffsetRegisters.DMAF_WDC_SECNUM_OFFSET:       // 0x33 - WDC sector number
                    DMAF_WDC_SECNUMRegister = b;
                    break;

                case (int)DMAF_OffsetRegisters.DMAF_WDC_CYLLO_OFFSET:        // 0x34 - WDC cylinder (low part - C0-C7)
                    DMAF_WDC_CYLLORegister = b;
                    break;

                case (int)DMAF_OffsetRegisters.DMAF_WDC_CYLHI_OFFSET:        // 0x35 - WDC cylinder (high part - C8-C9)
                    DMAF_WDC_CYLHIRegister = b;
                    break;

                case (int)DMAF_OffsetRegisters.DMAF_WDC_SDH_OFFSET:          // 0x36 - WDC size/drive/head
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
                    DMAF_WDC_SDHRegister = b;
                    break;

                case (int)DMAF_OffsetRegisters.DMAF_WDC_CMD_OFFSET:          // 0x37 - WDC command register
                                                                               //    wd_seek    equ     %01110000      seek with 10us step rate 
                                                                               //    wd_read    equ     %00101000      read sector DMA          
                                                                               //    wd_write   equ     %00110000      write sector             
                                                                               //    wd_format  equ     %01010000      format track (SPECIAL USAGE)
                                                                               //    wd_restore equ     %00010110      restore with 3ms step rate
                    {
                        DMAF_WDC_STATUSRegister &= (byte)(~(byte)WDC_StatusCodes.DMAF_WDC_SEEK_COMPLETE & 0xFF);  // clear seek complete on any new command

                        nDrive = (DMAF_WDC_SDHRegister >> 3) & 0x03;

                        int nHead = (DMAF_WDC_SDHRegister) & 0x07;
                        int nCylinder = ((int)(DMAF_WDC_CYLHIRegister & 0x03) * 256) + (int)DMAF_WDC_CYLLORegister;

                        //int nSector   = (DMAF_WDC_CYLHIRegister >> 2) & 0x3F;

                        int nSector = DMAF_WDC_SECNUMRegister - 1;

                        int nCylinders = m_hdpWinchester[nDrive].nCylinders;
                        int nHeads = m_hdpWinchester[nDrive].nHeads;
                        int nSectorsPerTrack = m_hdpWinchester[nDrive].nSectorsPerTrack;

                        //int nBytesPerSector  = Program.m_hdpWinchester[nDrive].nBytesPerSector  ;

                        int nBytesPerSector = (DMAF_WDC_SDHRegister >> 5) & 0x03;
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
                                DMAF_WDC_STATUSRegister |= (byte)(WDC_StatusCodes.DMAF_WDC_READY | WDC_StatusCodes.DMAF_WDC_SEEK_COMPLETE);   // set ready and seek complete
                                m_fpWinchester[nDrive].Seek(0, SeekOrigin.Begin);
                                if ((m_DMAF_LATCHRegister & 0x10) != 0)
                                    StartDMAFInterruptDelayTimer(m_nInterruptDelay, (int)DeviceInterruptMask.DEVICE_INT_MASK_WDC);
                                break;

                            case 0x20:                  //    wd_read    equ     %00101000      read sector DMA
                                {
                                    if ((nCylinder < nCylinders) && (nHead < nHeads) && (nSector < nSectorsPerTrack))
                                    {
                                        CalcHDFileOffset(strDriveType, WDC_COMMAND.WDC_RDWR);
                                        if (m_fpWinchester[nDrive] != null)
                                        {
                                            m_fpWinchester[nDrive].Seek(m_lFileOffset, SeekOrigin.Begin);
                                            m_fpWinchester[nDrive].Read(m_caReadBuffer, 0, m_nBytesToTransfer);

                                            // if the dma is active - get the bytes from memory, write them to the buffer and reset the dma counter

                                            if (activechannel == 1)
                                            {
                                                if (cnt > 0 && activechannel == 1)
                                                {
                                                    //Program._mainForm.SetWinchesterActivity(nDrive, global::SWTPCemuApp.Properties.Resources.greendot);

                                                    ulong lPhysicalAddress = LogicalToPhysicalAddress(latch, addr);

                                                    //string strActivityMessage = string.Format ("Reading %4d bytes from winchester %d track %02d head %02d sector %02d to   offset %06X WDC offset 0x%08X\n", cnt, nDrive, nCylinder, nHead, nSector, lPhysicalAddress, m_lFileOffset);
                                                    //LogFloppyActivity (nDrive, strActivityMessage);

                                                    for (int i = 0; i < cnt; i++)
                                                    {
                                                        if ((m_DMAF_DMA_CHANNELRegister[activechannel] & 0x08) == 0x08)
                                                            Program._cpu.Memory.MemorySpace[(int)((int)lPhysicalAddress - i)] = m_caReadBuffer[i];
                                                        else
                                                            Program._cpu.Memory.MemorySpace[(int)((int)lPhysicalAddress + i)] = m_caReadBuffer[i];
                                                    }

                                                    ushort a = (ushort)(m_DMAF_DMA_ADDRESSRegister[activechannel].hibyte * 256 + m_DMAF_DMA_ADDRESSRegister[activechannel].lobyte);
                                                    if ((m_DMAF_DMA_CHANNELRegister[activechannel] & 0x08) == 0x08)
                                                        a -= cnt;
                                                    else
                                                        a += cnt;

                                                    m_DMAF_DMA_DMCNTRegister[activechannel].hibyte = 0;
                                                    m_DMAF_DMA_DMCNTRegister[activechannel].lobyte = 0;

                                                    m_DMAF_DMA_ADDRESSRegister[activechannel].hibyte = (byte)(a / 256);
                                                    m_DMAF_DMA_ADDRESSRegister[activechannel].lobyte = (byte)(a % 256);

                                                    DMAF_WDC_STATUSRegister &= (byte)(~((byte)(WDC_StatusCodes.DMAF_WDC_DATA_REQUEST | WDC_StatusCodes.DMAF_WDC_BUSY)) & 0xFF);
                                                    DMAF_WDC_STATUSRegister |= (byte)((byte)WDC_StatusCodes.DMAF_WDC_SEEK_COMPLETE & 0xFF);  // set seek complete

                                                    //Program._mainForm.SetWinchesterActivity(nDrive, global::SWTPCemuApp.Properties.Resources.greydot);

                                                    if (_bInterruptEnabled)
                                                    {
                                                        // delay setting the interrupt until the timer times out.

                                                        StartDMAFInterruptDelayTimer(m_nInterruptDelay, (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA2);
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

                                                DMAF_WDC_DATARegister = m_caReadBuffer[0];
                                                DMAF_WDC_STATUSRegister |= (byte)(WDC_StatusCodes.DMAF_WDC_DATA_REQUEST);
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
                                                    //Program._mainForm.SetWinchesterActivity(nDrive, global::SWTPCemuApp.Properties.Resources.reddot);
                                                    Thread.Sleep(threadSleepTime);

                                                    ulong lPhysicalAddress = LogicalToPhysicalAddress(latch, addr);

                                                    //CString strActivityMessage;
                                                    //strActivityMessage.Format ("Writing %4d bytes to winchester %d track %02d head %02d sector %02d from offset %06X WDC offset 0x%08X\n", cnt, nDrive, nCylinder, nHead, nSector, lPhysicalAddress, m_lFileOffset);
                                                    //LogFloppyActivity (nDrive, strActivityMessage);

                                                    m_fpWinchester[nDrive].Seek(m_lFileOffset, SeekOrigin.Begin);

                                                    if ((m_DMAF_DMA_CHANNELRegister[activechannel] & 0x08) == 0x08)
                                                        m_fpWinchester[nDrive].Write(Program._cpu.Memory.MemorySpace, (int)(lPhysicalAddress - cnt), (int)cnt);
                                                    else
                                                        m_fpWinchester[nDrive].Write(Program._cpu.Memory.MemorySpace, (int)(lPhysicalAddress), (int)cnt);

                                                    m_fpWinchester[nDrive].Flush();

                                                    ushort a = (ushort)(m_DMAF_DMA_ADDRESSRegister[activechannel].hibyte * 256 + m_DMAF_DMA_ADDRESSRegister[activechannel].lobyte);
                                                    if ((m_DMAF_DMA_CHANNELRegister[activechannel] & 0x08) == 0x08)
                                                        a -= cnt;
                                                    else
                                                        a += cnt;

                                                    m_DMAF_DMA_DMCNTRegister[activechannel].hibyte = 0;
                                                    m_DMAF_DMA_DMCNTRegister[activechannel].lobyte = 0;

                                                    m_DMAF_DMA_ADDRESSRegister[activechannel].hibyte = (byte)(a / 256);
                                                    m_DMAF_DMA_ADDRESSRegister[activechannel].lobyte = (byte)(a % 256);

                                                    DMAF_WDC_STATUSRegister &= (byte)(~((byte)(WDC_StatusCodes.DMAF_WDC_DATA_REQUEST | WDC_StatusCodes.DMAF_WDC_BUSY)) & 0xFF);
                                                    DMAF_WDC_STATUSRegister |= (byte)((byte)WDC_StatusCodes.DMAF_WDC_SEEK_COMPLETE & 0xFF);  // set seek complete

                                                    //Program._mainForm.SetWinchesterActivity(nDrive, global::SWTPCemuApp.Properties.Resources.greydot);
                                                    Thread.Sleep(threadSleepTime);

                                                    if (_bInterruptEnabled)
                                                    {
                                                        // delay setting the interrupt until the timer times out.

                                                        StartDMAFInterruptDelayTimer(m_nInterruptDelay, (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA2);
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

                                                //DMAF_WDC_DATARegister = m_caReadBuffer[0];
                                                DMAF_WDC_STATUSRegister |= (byte)(WDC_StatusCodes.DMAF_WDC_DATA_REQUEST);
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
                                    //Program._mainForm.SetWinchesterActivity(nDrive, global::SWTPCemuApp.Properties.Resources.reddot);
                                    Thread.Sleep(threadSleepTime);

                                    byte[] pFormatBuffer = new byte[nBytesPerSector * DMAF_WDC_SECCNTRegister];
                                    memset(pFormatBuffer, (byte)'\0', (int)(nBytesPerSector * DMAF_WDC_SECCNTRegister));

                                    // calc offset to beginning of the track

                                    CalcHDFileOffset(strDriveType, WDC_COMMAND.WDC_FORMAT);

                                    long length = m_fpWinchester[nDrive].Length;

                                    //strActivityMessage.Format ("Writing %4d bytes to winchester %d cylinder %02d head %02d sector %02d DSK offset 0x%08X\n", cnt, nDrive, nCylinder, nHead, nSector, m_lFileOffset);
                                    //LogFloppyActivity (nDrive, strActivityMessage);

                                    if (length < (m_lFileOffset + (nBytesPerSector * DMAF_WDC_SECCNTRegister)))
                                    {
                                        // close file for "r+b" and reopen for append - write the bytes - close and reopen for "r+b"

                                        m_fpWinchester[nDrive].Close();
                                        m_fpWinchester[nDrive] = File.Open(m_cWinchesterDrivePathName[nDrive], FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                                        m_fpWinchester[nDrive].Write(pFormatBuffer, 0, nBytesPerSector * DMAF_WDC_SECCNTRegister);

                                        m_fpWinchester[nDrive].Close();
                                        m_fpWinchester[nDrive] = File.Open(m_cWinchesterDrivePathName[nDrive], FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                                    }
                                    else
                                    {
                                        m_fpWinchester[nDrive].Seek(m_lFileOffset, SeekOrigin.Begin);
                                        m_fpWinchester[nDrive].Write(pFormatBuffer, 0, nBytesPerSector * DMAF_WDC_SECCNTRegister);
                                        m_fpWinchester[nDrive].Flush();
                                    }

                                    DMAF_WDC_STATUSRegister |= (byte)(WDC_StatusCodes.DMAF_WDC_READY | WDC_StatusCodes.DMAF_WDC_SEEK_COMPLETE);   // set ready and seek complete

                                    m_DMAF_DMA_DMCNTRegister[activechannel].hibyte = 0;
                                    m_DMAF_DMA_DMCNTRegister[activechannel].lobyte = 0;

                                    if (_bInterruptEnabled)
                                        StartDMAFInterruptDelayTimer(m_nInterruptDelay, (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA2);
                                }
                                break;

                            case 0x60:
                                //AfxMessageBox ("Unimplemented WDC Command 0x6X");
                                break;

                            case 0x70:                  //    wd_seek    equ     %01110000      seek with 10us step rate

                                CalcHDFileOffset(strDriveType, WDC_COMMAND.WDC_SEEK);
                                m_fpWinchester[nDrive].Seek(m_lFileOffset, SeekOrigin.Begin);
                                DMAF_WDC_STATUSRegister |= (byte)(WDC_StatusCodes.DMAF_WDC_READY | WDC_StatusCodes.DMAF_WDC_SEEK_COMPLETE);   // set ready and seek complete

                                if (_bInterruptEnabled) 
                                    StartDMAFInterruptDelayTimer(m_nInterruptDelay, (int)DeviceInterruptMask.DEVICE_INT_MASK_WDC);

                                break;

                            case 0x00:
                                break;

                            default:
                                //AfxMessageBox ("Unimplemented WDC Command");
                                break;
                        }
                    }
                    break;

                case (int)DMAF_OffsetRegisters.DMAF_WD1000_RES_OFFSET:        // 0x51 - winchester software reset

                    DMAF_WD1000_RESRegister = b;

                    DMAF_WDC_DATARegister = 0;                                             // WDC data register
                    DMAF_WDC_ERRORRegister = 0;                                             // WDC error register
                    DMAF_WDC_SECNUMRegister = 0;                                             // WDC sector number
                    DMAF_WDC_CYLLORegister = 0;                                             // WDC cylinder (low part)
                    DMAF_WDC_CYLHIRegister = 0;                                             // WDC cylinder (high part)
                    DMAF_WDC_SDHRegister = 0;                                             // WDC size/drive/head
                    DMAF_WDC_STATUSRegister = (byte)(WDC_StatusCodes.DMAF_WDC_READY | WDC_StatusCodes.DMAF_WDC_SEEK_COMPLETE);   // set WDC status register to Ready and not busy
                    DMAF_WDC_CMDRegister = 0;                                             // WDC command register
                    break;

            }
        }

        void WriteTape(ushort m, byte b)
        {
            int nWhichRegister = m - m_sBaseAddress;
            int activechannel = -1;

            switch (m_DMAF_DMA_PRIORITYRegister & 0x0F)
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

            byte latch = m_DMAF_LATCHRegister;
            ushort addr = 0;
            ushort cnt = 0;
            byte priority = m_DMAF_DMA_PRIORITYRegister;
            byte interrupt = m_DMAF_DMA_INTERRUPTRegister;
            byte chain = m_DMAF_DMA_CHAINRegister;

            if (activechannel >= 0)
            {
                addr = (ushort)(m_DMAF_DMA_ADDRESSRegister[activechannel].hibyte * 256 + m_DMAF_DMA_ADDRESSRegister[activechannel].lobyte);
                cnt = (ushort)(m_DMAF_DMA_DMCNTRegister[activechannel].hibyte * 256 + m_DMAF_DMA_DMCNTRegister[activechannel].lobyte);
            }

            //CString strActivityMessage;
            //strActivityMessage.Format ("Writing Address 0x%04X with 0x%02X addr: 0x%04X latch: 0x%02X cnt: %d priority: 0x%02X interrupt: 0x%02X chain: 0x%02X\n", m, b, addr, latch, cnt, priority, interrupt, chain);
            //LogFloppyActivity (nDrive, strActivityMessage);

            switch ((DMAF_OffsetRegisters)nWhichRegister)
            {
                case DMAF_OffsetRegisters.DMAF_AT_DTB_OFFSET:           // 0x40 - B-Side Data Register  DMAF status lines
                    DMAF_AT_DTBRegister = b;
                    break;

                case DMAF_OffsetRegisters.DMAF_AT_DTA_OFFSET:           // 0x41 - A-Side Data Register
                    DMAF_AT_DTARegister = b;
                    break;

                case DMAF_OffsetRegisters.DMAF_AT_DRB_OFFSET:           // 0x42 - B-Side Direction Register
                    DMAF_AT_DRBRegister = b;
                    break;

                case DMAF_OffsetRegisters.DMAF_AT_DRA_OFFSET:           // 0x43 - A-Side Direction Register
                    DMAF_AT_DRARegister = b;
                    break;

                case DMAF_OffsetRegisters.DMAF_AT_T1C_OFFSET:           // 0x44 - Timer 1 Counter Register
                    // writing to the counters actually writes to the latches
                    //
                    m_DMAF_AT_TLRegister.lobyte = b;
                    break;

                case DMAF_OffsetRegisters.DMAF_AT_T1C_OFFSET + 1:       // 0x45 - Timer 1 Counter Register
                    // writing to the counters actually writes to the latches
                    //
                    m_DMAF_AT_TLRegister.hibyte = b;

                    // writing to the high order byte causes the latch to load to the counter

                    m_DMAF_AT_TCRegister[0].hibyte = m_DMAF_AT_TLRegister.hibyte;
                    m_DMAF_AT_TCRegister[0].lobyte = m_DMAF_AT_TLRegister.lobyte;
                    DMAF_AT_IFRRegister = (byte)(DMAF_AT_IFRRegister & ~0x40);
                    m_nEnableT1C_Countdown = true;
                    break;

                case DMAF_OffsetRegisters.DMAF_AT_T1L_OFFSET:           // 0x46 - Timer 1 Latches
                    m_DMAF_AT_TLRegister.lobyte = b;
                    break;
                case DMAF_OffsetRegisters.DMAF_AT_T1L_OFFSET + 1:       // 0x47 - Timer 1 Latches
                    m_DMAF_AT_TLRegister.hibyte = b;
                    DMAF_AT_IFRRegister = (byte)(DMAF_AT_IFRRegister & ~0x40);
                    m_nEnableT1C_Countdown = true;
                    break;

                case DMAF_OffsetRegisters.DMAF_AT_T2C_OFFSET:           // 0x48 - Timer 2 Counter Register
                    m_DMAF_AT_TCRegister[1].lobyte = b;
                    break;
                case DMAF_OffsetRegisters.DMAF_AT_T2C_OFFSET + 1:       // 0x49 - Timer 2 Counter Register
                    m_DMAF_AT_TCRegister[1].hibyte = b;
                    break;

                case DMAF_OffsetRegisters.DMAF_AT_VSR_OFFSET:           // 0x4A - Shift Register
                    DMAF_AT_VSRRegister = b;
                    break;

                case DMAF_OffsetRegisters.DMAF_AT_ACR_OFFSET:           // 0x4B - Auxillary Control Register
                    DMAF_AT_ACRRegister = b;
                    break;

                case DMAF_OffsetRegisters.DMAF_AT_PCR_OFFSET:           // 0x4C - Peripheral Control Register
                    DMAF_AT_PCRRegister = b;
                    break;

                case DMAF_OffsetRegisters.DMAF_AT_IFR_OFFSET:           // 0x4D - Interrupt Flag Register
                    DMAF_AT_IFRRegister = (byte)(DMAF_AT_IFRRegister & ~b);
                    break;

                case DMAF_OffsetRegisters.DMAF_AT_IER_OFFSET:           // 0x4E - Interrupt Enable Register
                    //
                    //  If bit 7 is not set we want to clear bits, not set them, otherwise we want to set bits.
                    //
                    if ((b & 0x80) == 0x00)
                        DMAF_AT_IERRegister = (byte)(DMAF_AT_IERRegister & ~(b | 0x80));
                    else
                        DMAF_AT_IERRegister = (byte)(DMAF_AT_IERRegister | (b & 0x7f));
                    break;

                case DMAF_OffsetRegisters.DMAF_AT_DXA_OFFSET:           // 0x4F - A-Side Data Register
                    DMAF_AT_DXARegister = b;
                    break;

                case DMAF_OffsetRegisters.DMAF_AT_ATR_OFFSET:           // 0x52 - archive RESET
                    DMAF_AT_ATRRegister = b;
                    break;

                case DMAF_OffsetRegisters.DMAF_AT_DMC_OFFSET:           // 0x53 - archive DMA clear
                    DMAF_AT_DMCRegister = b;
                    break;

                case DMAF_OffsetRegisters.DMAF_AT_DMP_OFFSET:           // 0x60 - archive DMA preset
                    DMAF_AT_DMPRegister = b;
                    break;
            }
        }

        void WriteCDS(ushort m, byte b)
        {
            int nWhichRegister = m - m_sBaseAddress;
            int activechannel = -1;

            switch (m_DMAF_DMA_PRIORITYRegister & 0x0F)
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

            byte latch = m_DMAF_LATCHRegister;
            ushort addr = 0;
            ushort cnt = 0;
            byte priority = m_DMAF_DMA_PRIORITYRegister;
            byte interrupt = m_DMAF_DMA_INTERRUPTRegister;
            byte chain = m_DMAF_DMA_CHAINRegister;

            if (activechannel >= 0)
            {
                addr = (ushort)(m_DMAF_DMA_ADDRESSRegister[activechannel].hibyte * 256 + m_DMAF_DMA_ADDRESSRegister[activechannel].lobyte);
                cnt = (ushort)(m_DMAF_DMA_DMCNTRegister[activechannel].hibyte * 256 + m_DMAF_DMA_DMCNTRegister[activechannel].lobyte);
            }

            //CString strActivityMessage;
            //strActivityMessage.Format ("Writing Address 0x%04X with 0x%02X addr: 0x%04X latch: 0x%02X cnt: %d priority: 0x%02X interrupt: 0x%02X chain: 0x%02X\n", m, b, addr, latch, cnt, priority, interrupt, chain);
            //LogFloppyActivity (nDrive, strActivityMessage);

            switch ((DMAF_OffsetRegisters)nWhichRegister)
            {
                case DMAF_OffsetRegisters.DMAF_CDSCMD0_OFFSET:          // 0x0100      
                    DMAF_CDSCMD0Register = b;
                    break;

                case DMAF_OffsetRegisters.DMAF_CDSFLG0_OFFSET:          // 0x0103 - marksman flag (1xxx xxxx = yes)
                    DMAF_CDSFLG0Register = b;
                    break;

                case DMAF_OffsetRegisters.DMAF_CDSCMD1_OFFSET:          // 0x0300      
                    DMAF_CDSCMD1Register = b;
                    break;

                case DMAF_OffsetRegisters.DMAF_CDSFLG1_OFFSET:          // 0x0303 - marksman flag (1xxx xxxx = yes)
                    DMAF_CDSFLG1Register = b;
                    break;
            }
        }

        public override void Write(ushort m, byte b)
        {
            DMAF_OffsetRegisters nWhichRegister = (DMAF_OffsetRegisters)(m - m_sBaseAddress);

            LogDMAFActivity((ushort)(m - m_sBaseAddress), b, false);

            if ((nWhichRegister >= DMAF_OffsetRegisters.DMAF_STATREG_OFFSET && nWhichRegister <= DMAF_OffsetRegisters.DMAF_DRVREG_OFFSET) || nWhichRegister == DMAF_OffsetRegisters.DMAF_HLD_TOGGLE_OFFSET)
                WriteFloppy(m, b);
            else if ((nWhichRegister >= DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET && nWhichRegister <= DMAF_OffsetRegisters.DMAF_DMA_CHAIN_OFFSET) || nWhichRegister == DMAF_OffsetRegisters.DMAF3_LATCH_OFFSET)
                WriteDMA(m, b);
            else if ((nWhichRegister >= DMAF_OffsetRegisters.DMAF_WDC_DATA_OFFSET && nWhichRegister <= DMAF_OffsetRegisters.DMAF_WDC_CMD_OFFSET) || nWhichRegister == DMAF_OffsetRegisters.DMAF_WD1000_RES_OFFSET)
                WriteWD1000(m, b);
            else if ((nWhichRegister >= DMAF_OffsetRegisters.DMAF_AT_DTB_OFFSET && nWhichRegister <= DMAF_OffsetRegisters.DMAF_AT_DXA_OFFSET) || nWhichRegister == DMAF_OffsetRegisters.DMAF_AT_ATR_OFFSET || nWhichRegister == DMAF_OffsetRegisters.DMAF_AT_DMC_OFFSET || nWhichRegister == DMAF_OffsetRegisters.DMAF_AT_DMP_OFFSET)
                WriteTape(m, b);
            else if (nWhichRegister == DMAF_OffsetRegisters.DMAF_CDSCMD0_OFFSET || nWhichRegister == DMAF_OffsetRegisters.DMAF_CDSFLG0_OFFSET || nWhichRegister == DMAF_OffsetRegisters.DMAF_CDSCMD1_OFFSET || nWhichRegister == DMAF_OffsetRegisters.DMAF_CDSFLG1_OFFSET)
                WriteCDS(m, b);
            else
                Program._cpu.WriteToFirst64K(m, b);
        }

        byte ReadFloppy(ushort m)
        {
            byte d = 0xFF;

            int nWhichRegister = m - m_sBaseAddress;
            int nDrive = 0;
            switch (m_DMAF_DRVRegister & 0x0F)
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

            switch ((DMAF_OffsetRegisters)nWhichRegister)
            {
                case DMAF_OffsetRegisters.DMAF_DATAREG_OFFSET:
                    if (m_nFDCReading)
                    {
                        m_DMAF_DATARegister = m_caReadBuffer[m_nFDCReadPtr];
                        //if (!Program._cpu.BuildingDebugLine)
                        lock (Program._cpu.buildingDebugLineLock)
                        {
                            m_nFDCReadPtr++;

                            if (m_nFDCReadPtr == m_nBytesToTransfer)
                            {
                                //if (!Program._cpu.BuildingDebugLine)
                                //lock (Program._cpu.buildingDebugLineLock)
                                {
                                    m_DMAF_STATRegister &= (byte)(~((byte)((byte)DMAF_StatusLines.DMAF_DRQ | (byte)DMAF_StatusLines.DMAF_BUSY)) & 0xFF);
                                    m_DMAF_DRVRegister &= (byte)(~((byte)DMAF_ContollerLines.DRV_DRQ) & 0xFF);            // turn off high order bit in drive status register

                                    m_nFDCReading = false;

                                    //Program._mainForm.SetFloppyActivity(nDrive, global::SWTPCemuApp.Properties.Resources.greydot);

                                    ClearInterrupt((int)DeviceInterruptMask.DEVICE_INT_MASK_FDC);
                                }
                            }
                            // --------------------
                            else
                            {
                                m_DMAF_STATRegister |= (byte)(DMAF_ContollerLines.DRV_DRQ);
                                m_DMAF_DRVRegister |= (byte)(DMAF_ContollerLines.DRV_DRQ);

                                //if (!Program._cpu.BuildingDebugLine)
                                lock (Program._cpu.buildingDebugLineLock)
                                {
                                    if (_bInterruptEnabled)
                                    {
                                        StartDMAFInterruptDelayTimer(m_nInterruptDelay, (int)DeviceInterruptMask.DEVICE_INT_MASK_FDC);
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
                            // --------------------
                            d = m_DMAF_DATARegister;

                            // if the dma is active - put the byte in memory also and decrement the dma counter
                        }
                    }
                    else
                    {
                        d = m_DMAF_DATARegister;

                        // if the dma is active - put the byte in memory also and decrement the dma counter
                    }
                    break;

                case DMAF_OffsetRegisters.DMAF_STATREG_OFFSET:

                    //if (!Program._cpu.BuildingDebugLine)
                    lock (Program._cpu.buildingDebugLineLock)
                    {
                        DMAF_AT_DTBRegister &= (byte)(~0x04 & 0xFF);                          // clear interrupting bit to VIA
                    }

                    if (Program.DriveOpen[nDrive] == true)               // see if current drive is READY
                        m_DMAF_STATRegister |= (byte)DMAF_StatusLines.DMAF_NOTREADY;
                    else
                        m_DMAF_STATRegister &= (byte)(~((byte)DMAF_StatusLines.DMAF_NOTREADY) & 0xFF);

                    if (Program.WriteProtected[nDrive] == true)          // see if write protected
                        m_DMAF_STATRegister |= (byte)DMAF_StatusLines.DMAF_WRTPROTECT;
                    else
                        m_DMAF_STATRegister &= (byte)(~((byte)DMAF_StatusLines.DMAF_WRTPROTECT) & 0xFF);

                    //if (!Program._cpu.BuildingDebugLine)
                    lock (Program._cpu.buildingDebugLineLock)
                    {
                        if (!m_nFDCReading && !m_nFDCWriting)           // turn off BUSY if not read/writing
                            m_DMAF_STATRegister &= (byte)(~((byte)DMAF_StatusLines.DMAF_BUSY) & 0xFF);
                        else
                        {
                            if (++m_nStatusReads > (m_nBytesToTransfer * 2))
                            {
                                m_DMAF_STATRegister &= (byte)(~((byte)DMAF_StatusLines.DMAF_BUSY) & 0xFF);     // clear BUSY if data not read

                                //Program._mainForm.SetFloppyActivity(nDrive, global::SWTPCemuApp.Properties.Resources.greydot);
                            }
                        }
                        ClearInterrupt((int)DeviceInterruptMask.DEVICE_INT_MASK_FDC);
                    }
                    d = m_DMAF_STATRegister;                      // get controller status
                    break;

                case DMAF_OffsetRegisters.DMAF_DRVREG_OFFSET:
                    d = (byte)(~((byte)m_DMAF_DRVRegister | 0x40) & 0xFF);
                    break;

                case DMAF_OffsetRegisters.DMAF_TRKREG_OFFSET:
                    d = m_DMAF_TRKRegister;                      // get Track Register
                    break;

                case DMAF_OffsetRegisters.DMAF_SECREG_OFFSET:
                    d = m_DMAF_SECRegister;                      // get Track Register
                    break;

                case DMAF_OffsetRegisters.DMAF_HLD_TOGGLE_OFFSET:       // 0x50 - head load toggle
                    d = DMAF_HLD_TOGGLERegister;
                    break;

            }

            //strActivityMessage.Format (" value: 0x%02X\n", d);
            //LogFloppyActivity (nDrive, strActivityMessage, false);

            return (d);
        }

        byte ReadDMA(ushort m)
        {
            byte d = 0xFF;
            int nWhichRegister = m - m_sBaseAddress;

            //CString strActivityMessage;

            //strActivityMessage.Format ("Reading DMA Register Address 0x%04X", m);
            //LogFloppyActivity (-1, strActivityMessage);

            switch ((DMAF_OffsetRegisters)nWhichRegister)
            {
                case DMAF_OffsetRegisters.DMAF3_LATCH_OFFSET:
                    d = m_DMAF_LATCHRegister;                    // the Enable interrupt bit is in here somewhere
                    break;

                //*
                //*   DMAF 6844 DMA controller definitions
                //*

                case DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET + 0:                  // 0xF000
                    d = m_DMAF_DMA_ADDRESSRegister[0].hibyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET + 1:                  // 0xF001
                    d = m_DMAF_DMA_ADDRESSRegister[0].lobyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET + 4:                  // 0xF004
                    d = m_DMAF_DMA_ADDRESSRegister[1].hibyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET + 5:                  // 0xF005
                    d = m_DMAF_DMA_ADDRESSRegister[1].lobyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET + 8:                  // 0xF008
                    d = m_DMAF_DMA_ADDRESSRegister[2].hibyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET + 9:                  // 0xF009
                    d = m_DMAF_DMA_ADDRESSRegister[2].lobyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET + 12:                 // 0xF00C
                    d = m_DMAF_DMA_ADDRESSRegister[3].hibyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET + 13:                 // 0xF00D
                    d = m_DMAF_DMA_ADDRESSRegister[3].lobyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_DMCNT_OFFSET + 0:                    // 0xF002
                    d = m_DMAF_DMA_DMCNTRegister[0].hibyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_DMCNT_OFFSET + 1:                    // 0xF003
                    d = m_DMAF_DMA_DMCNTRegister[0].lobyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_DMCNT_OFFSET + 4:                    // 0xF006
                    d = m_DMAF_DMA_DMCNTRegister[1].hibyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_DMCNT_OFFSET + 5:                    // 0xF007
                    d = m_DMAF_DMA_DMCNTRegister[1].lobyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_DMCNT_OFFSET + 8:                    // 0xF00A
                    d = m_DMAF_DMA_DMCNTRegister[2].hibyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_DMCNT_OFFSET + 9:                    // 0xF00B
                    d = m_DMAF_DMA_DMCNTRegister[2].lobyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_DMCNT_OFFSET + 12:                   // 0xF00E
                    d = m_DMAF_DMA_DMCNTRegister[3].hibyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_DMCNT_OFFSET + 13:                   // 0xF00F
                    d = m_DMAF_DMA_DMCNTRegister[3].lobyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_CHANNEL_OFFSET:                      // 0xF010
                    d = m_DMAF_DMA_CHANNELRegister[0];

                    // after setting the return value any read of this register should clear the DEND flag (interrupt pending)

                    m_DMAF_DMA_CHANNELRegister[0] &= 0x7F;

                    // make sure the interrupt is already set by the timer before we set the high order bit of the DMA status register

                    if (
                        ((Program._cpu.InterruptRegister & m_nInterruptMask) == m_nInterruptMask) &&
                        ((m_DMAF_DMA_INTERRUPTRegister & 0x01) == 0x01) &&
                        ((m_nBoardInterruptRegister & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA1) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA1)
                       )
                        d |= 0x80;

                    //if (!Program._cpu.BuildingDebugLine)
                    lock (Program._cpu.buildingDebugLineLock)
                    {
                        m_DMAF_DMA_CHANNELRegister[0] = (byte)(m_DMAF_DMA_CHANNELRegister[0] & 0x7F);
                        m_DMAF_DMA_INTERRUPTRegister = (byte)(m_DMAF_DMA_INTERRUPTRegister & 0x7F);
                        ClearInterrupt((int)DeviceInterruptMask.DEVICE_INT_MASK_DMA1);
                    }
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_CHANNEL_OFFSET + 1:                  // 0xF011
                    d = m_DMAF_DMA_CHANNELRegister[1];

                    // after setting the return value any read of this register should clear the DEND flag (interrupt pending)

                    m_DMAF_DMA_CHANNELRegister[1] &= 0x7F;

                    // make sure the interrupt is already set by the timer before we set the high order bit of the DMA status register

                    if (
                        ((Program._cpu.InterruptRegister & m_nInterruptMask) == m_nInterruptMask) &&
                        ((m_DMAF_DMA_INTERRUPTRegister & 0x02) == 0x02) &&
                        ((m_nBoardInterruptRegister & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA2) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA2)
                       )
                        d |= 0x80;

                    //if (!Program._cpu.BuildingDebugLine)
                    lock (Program._cpu.buildingDebugLineLock)
                    {
                        m_DMAF_DMA_CHANNELRegister[1] = (byte)(m_DMAF_DMA_CHANNELRegister[1] & 0x7F);
                        m_DMAF_DMA_INTERRUPTRegister = (byte)(m_DMAF_DMA_INTERRUPTRegister & 0x7F);
                        ClearInterrupt((int)DeviceInterruptMask.DEVICE_INT_MASK_DMA2);
                    }
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_CHANNEL_OFFSET + 2:                  // 0xF012
                    d = m_DMAF_DMA_CHANNELRegister[2];

                    // after setting the return value any read of this register should clear the DEND flag (interrupt pending)

                    m_DMAF_DMA_CHANNELRegister[2] &= 0x7F;

                    // make sure the interrupt is already set by the timer before we set the high order bit of the DMA status register

                    if (
                        ((Program._cpu.InterruptRegister & m_nInterruptMask) == m_nInterruptMask) &&
                        ((m_DMAF_DMA_INTERRUPTRegister & 0x04) == 0x04) &&
                        ((m_nBoardInterruptRegister & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA3) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA3)
                       )
                        d |= 0x80;
                    //if (!Program._cpu.BuildingDebugLine)
                    lock (Program._cpu.buildingDebugLineLock)
                    {
                        m_DMAF_DMA_CHANNELRegister[2] = (byte)(m_DMAF_DMA_CHANNELRegister[2] & 0x7F);
                        m_DMAF_DMA_INTERRUPTRegister = (byte)(m_DMAF_DMA_INTERRUPTRegister & 0x7F);
                        ClearInterrupt((int)DeviceInterruptMask.DEVICE_INT_MASK_DMA3);
                    }
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_CHANNEL_OFFSET + 3:                  // 0xF013
                    d = m_DMAF_DMA_CHANNELRegister[3];

                    // after setting the return value any read of this register should clear the DEND flag (interrupt pending)

                    m_DMAF_DMA_CHANNELRegister[3] &= 0x7F;

                    // make sure the interrupt is already set by the timer before we set the high order bit of the DMA status register

                    if (
                        ((Program._cpu.InterruptRegister & m_nInterruptMask) == m_nInterruptMask) &&
                        ((m_DMAF_DMA_INTERRUPTRegister & 0x08) == 0x08) &&
                        ((m_nBoardInterruptRegister & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA4) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA4)
                       )
                        d |= 0x80;

                    //if (!Program._cpu.BuildingDebugLine)
                    lock (Program._cpu.buildingDebugLineLock)
                    {
                        m_DMAF_DMA_CHANNELRegister[3] = (byte)(m_DMAF_DMA_CHANNELRegister[3] & 0x7F);
                        m_DMAF_DMA_INTERRUPTRegister = (byte)(m_DMAF_DMA_INTERRUPTRegister & 0x7F);
                        ClearInterrupt((int)DeviceInterruptMask.DEVICE_INT_MASK_DMA4);
                    }
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_PRIORITY_OFFSET:                     // 0xF014
                    d = m_DMAF_DMA_PRIORITYRegister;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_INTERRUPT_OFFSET:                    // 0xF015
                    d = m_DMAF_DMA_INTERRUPTRegister;
                    if (
                        ((Program._cpu.InterruptRegister & m_nInterruptMask) == m_nInterruptMask) &&
                        (
                            (((m_DMAF_DMA_INTERRUPTRegister & 0x01) == 0x01) && ((m_nBoardInterruptRegister & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA1) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA1)) ||
                            (((m_DMAF_DMA_INTERRUPTRegister & 0x02) == 0x02) && ((m_nBoardInterruptRegister & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA2) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA2)) ||
                            (((m_DMAF_DMA_INTERRUPTRegister & 0x04) == 0x04) && ((m_nBoardInterruptRegister & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA3) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA3)) ||
                            (((m_DMAF_DMA_INTERRUPTRegister & 0x08) == 0x08) && ((m_nBoardInterruptRegister & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA4) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA4))
                        )
                       )
                    {
                        // say we are interrupting

                        d |= 0x80;
                    }
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_CHAIN_OFFSET:                        // 0xF016
                    d = m_DMAF_DMA_CHAINRegister;
                    break;
            }

            return (d);
        }

        byte ReadWD1000(ushort m)
        {
            byte d = 0xFF;
            int nWhichRegister = m - m_sBaseAddress;
            int activechannel = -1;

            switch (m_DMAF_DMA_PRIORITYRegister & 0x0F)
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

            byte latch = m_DMAF_LATCHRegister;
            ushort addr = 0;
            ushort cnt = 0;
            byte priority = m_DMAF_DMA_PRIORITYRegister;
            byte interrupt = m_DMAF_DMA_INTERRUPTRegister;
            byte chain = m_DMAF_DMA_CHAINRegister;

            if (activechannel == 1)
            {
                addr = (ushort)(m_DMAF_DMA_ADDRESSRegister[activechannel].hibyte * 256 + m_DMAF_DMA_ADDRESSRegister[activechannel].lobyte);
                cnt = (ushort)(m_DMAF_DMA_DMCNTRegister[activechannel].hibyte * 256 + m_DMAF_DMA_DMCNTRegister[activechannel].lobyte);
            }

            int nDrive = (DMAF_WDC_SDHRegister >> 3) & 0x03;

            //CString strActivityMessage;
            //strActivityMessage.Format ("Reading Address 0x%04X", m);
            //LogFloppyActivity (nDrive, strActivityMessage);

            switch ((DMAF_OffsetRegisters)nWhichRegister)
            {
                // Winchester WD1000 5" controller

                case DMAF_OffsetRegisters.DMAF_WDC_DATA_OFFSET:         // 0x30 - WDC data register
                    d = DMAF_WDC_DATARegister;
                    break;

                case DMAF_OffsetRegisters.DMAF_WDC_ERROR_OFFSET:        // 0x31 - WDC error register
                                                                          //    wd_error   equ     wd1000+1       error register (read only)
                                                                          //    *                                 bit 7 bad block detect
                                                                          //    *                                 bit 6 CRC error, data field
                                                                          //    *                                 bit 5 CRC error, ID field
                                                                          //    *                                 bit 4 ID not found
                                                                          //    *                                 bit 3 unused
                                                                          //    *                                 bit 2 Aborted Command
                                                                          //    *                                 bit 1 TR000 (track zero) error
                                                                          //    *                                 bit 0 DAM not found
                    d = DMAF_WDC_ERRORRegister;
                    break;

                case DMAF_OffsetRegisters.DMAF_WDC_SECCNT_OFFSET:       // 0x32 - sector count (during format)
                    d = DMAF_WDC_SECCNTRegister;
                    break;

                case DMAF_OffsetRegisters.DMAF_WDC_SECNUM_OFFSET:       // 0x33 - WDC sector number
                    d = DMAF_WDC_SECNUMRegister;
                    break;

                case DMAF_OffsetRegisters.DMAF_WDC_CYLLO_OFFSET:        // 0x34 - WDC cylinder (low part)
                    d = DMAF_WDC_CYLLORegister;
                    break;

                case DMAF_OffsetRegisters.DMAF_WDC_CYLHI_OFFSET:        // 0x35 - WDC cylinder (high part)
                    d = DMAF_WDC_CYLHIRegister;
                    break;

                case DMAF_OffsetRegisters.DMAF_WDC_SDH_OFFSET:          // 0x36 - WDC size/drive/head
                    d = DMAF_WDC_SDHRegister;
                    break;

                case DMAF_OffsetRegisters.DMAF_WDC_STATUS_OFFSET:       // 0x37 - WDC status register
                    d = DMAF_WDC_STATUSRegister;
                    ClearInterrupt((int)DeviceInterruptMask.DEVICE_INT_MASK_WDC);
                    break;

                case DMAF_OffsetRegisters.DMAF_WD1000_RES_OFFSET:        // 0x51 - winchester software reset
                    d = DMAF_WD1000_RESRegister;
                    break;

            }

            //strActivityMessage.Format (" value: 0x%02X\n", d);
            //LogFloppyActivity (nDrive, strActivityMessage, false);

            return (d);
        }

        byte ReadTape(ushort m)
        {
            byte d = 0xFF;
            int nWhichRegister = m - m_sBaseAddress;
            switch ((DMAF_OffsetRegisters)nWhichRegister)
            {
                case DMAF_OffsetRegisters.DMAF_AT_DTB_OFFSET:           // 0x40 - B-Side Data Register
                    d = DMAF_AT_DTBRegister;
                    break;

                case DMAF_OffsetRegisters.DMAF_AT_DTA_OFFSET:           // 0x41 - A-Side Data Register
                    d = DMAF_AT_DTARegister;
                    break;

                case DMAF_OffsetRegisters.DMAF_AT_DRB_OFFSET:           // 0x42 - B-Side Direction Register
                    d = DMAF_AT_DRBRegister;
                    break;

                case DMAF_OffsetRegisters.DMAF_AT_DRA_OFFSET:           // 0x43 - A-Side Direction Register
                    d = DMAF_AT_DRARegister;
                    break;

                case DMAF_OffsetRegisters.DMAF_AT_T1C_OFFSET:           // 0x44 - Timer 1 Counter Register
                    d = m_DMAF_AT_TCRegister[0].lobyte;
                    break;
                case DMAF_OffsetRegisters.DMAF_AT_T1C_OFFSET + 1:       // 0x45 - Timer 1 Counter Register
                    d = m_DMAF_AT_TCRegister[0].hibyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_AT_T1L_OFFSET:           // 0x46 - Timer 1 Latches
                    d = m_DMAF_AT_TLRegister.lobyte;
                    break;
                case DMAF_OffsetRegisters.DMAF_AT_T1L_OFFSET + 1:       // 0x47 - Timer 1 Latches
                    d = m_DMAF_AT_TLRegister.hibyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_AT_T2C_OFFSET:           // 0x48 - Timer 2 Counter Register
                    d = m_DMAF_AT_TCRegister[1].lobyte;
                    break;
                case DMAF_OffsetRegisters.DMAF_AT_T2C_OFFSET + 1:       // 0x49 - Timer 2 Counter Register
                    d = m_DMAF_AT_TCRegister[1].hibyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_AT_VSR_OFFSET:           // 0x4A - Shift Register
                    d = DMAF_AT_VSRRegister;
                    break;

                case DMAF_OffsetRegisters.DMAF_AT_ACR_OFFSET:           // 0x4B - Auxillary Control Register
                    d = DMAF_AT_ACRRegister;
                    break;

                case DMAF_OffsetRegisters.DMAF_AT_PCR_OFFSET:           // 0x4C - Peripheral Control Register
                    d = DMAF_AT_PCRRegister;
                    break;

                case DMAF_OffsetRegisters.DMAF_AT_IFR_OFFSET:           // 0x4D - Interrupt Flag Register
                    d = DMAF_AT_IFRRegister;
                    break;

                case DMAF_OffsetRegisters.DMAF_AT_IER_OFFSET:           // 0x4E - Interrupt Enable Register
                    d = DMAF_AT_IERRegister;
                    break;

                case DMAF_OffsetRegisters.DMAF_AT_DXA_OFFSET:           // 0x4F - A-Side Data Register
                    d = DMAF_AT_DXARegister;
                    break;

                case DMAF_OffsetRegisters.DMAF_AT_ATR_OFFSET:           // 0x52 - archive RESET
                    d = DMAF_AT_ATRRegister;
                    break;

                case DMAF_OffsetRegisters.DMAF_AT_DMC_OFFSET:           // 0x53 - archive DMA clear
                    d = DMAF_AT_DMCRegister;
                    break;

                case DMAF_OffsetRegisters.DMAF_AT_DMP_OFFSET:           // 0x60 - archive DMA preset
                    d = DMAF_AT_DMPRegister;
                    break;

            }
            return (d);
        }

        byte ReadCDS(ushort m)
        {
            byte d = 0xFF;
            int nWhichRegister = m - m_sBaseAddress;

            switch ((DMAF_OffsetRegisters)nWhichRegister)
            {
                case DMAF_OffsetRegisters.DMAF_CDSCMD0_OFFSET:          // 0x0100      
                    d = DMAF_CDSCMD0Register;
                    break;

                case DMAF_OffsetRegisters.DMAF_CDSFLG0_OFFSET:          // 0x0103 - marksman flag (1xxx xxxx = yes)
                    d = DMAF_CDSFLG0Register;
                    break;

                case DMAF_OffsetRegisters.DMAF_CDSCMD1_OFFSET:          // 0x0300      
                    d = DMAF_CDSCMD1Register;
                    break;

                case DMAF_OffsetRegisters.DMAF_CDSFLG1_OFFSET:          // 0x0303 - marksman flag (1xxx xxxx = yes)
                    d = DMAF_CDSFLG1Register;
                    break;
            }
            return (d);
        }

        public override byte Read(ushort m)
        {
            byte returnValue = 0xff;

            DMAF_OffsetRegisters nWhichRegister = (DMAF_OffsetRegisters)(m - m_sBaseAddress);

            if ((nWhichRegister >= DMAF_OffsetRegisters.DMAF_STATREG_OFFSET && nWhichRegister <= DMAF_OffsetRegisters.DMAF_DRVREG_OFFSET) || nWhichRegister == DMAF_OffsetRegisters.DMAF_HLD_TOGGLE_OFFSET)
                returnValue = ReadFloppy(m);
            else if ((nWhichRegister >= DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET && nWhichRegister <= DMAF_OffsetRegisters.DMAF_DMA_CHAIN_OFFSET) || nWhichRegister == DMAF_OffsetRegisters.DMAF3_LATCH_OFFSET)
                returnValue = ReadDMA(m);
            else if ((nWhichRegister >= DMAF_OffsetRegisters.DMAF_WDC_DATA_OFFSET && nWhichRegister <= DMAF_OffsetRegisters.DMAF_WDC_CMD_OFFSET) || nWhichRegister == DMAF_OffsetRegisters.DMAF_WD1000_RES_OFFSET)
                returnValue = ReadWD1000(m);
            else if ((nWhichRegister >= DMAF_OffsetRegisters.DMAF_AT_DTB_OFFSET && nWhichRegister <= DMAF_OffsetRegisters.DMAF_AT_DXA_OFFSET) || nWhichRegister == DMAF_OffsetRegisters.DMAF_AT_ATR_OFFSET || nWhichRegister == DMAF_OffsetRegisters.DMAF_AT_DMC_OFFSET || nWhichRegister == DMAF_OffsetRegisters.DMAF_AT_DMP_OFFSET)
                returnValue = ReadTape(m);
            else if (nWhichRegister == DMAF_OffsetRegisters.DMAF_CDSCMD0_OFFSET || nWhichRegister == DMAF_OffsetRegisters.DMAF_CDSFLG0_OFFSET || nWhichRegister == DMAF_OffsetRegisters.DMAF_CDSCMD1_OFFSET || nWhichRegister == DMAF_OffsetRegisters.DMAF_CDSFLG1_OFFSET)
                returnValue = ReadCDS(m);
            else
                returnValue = Program._cpu.ReadFromFirst64K(m);   // memory read

            LogDMAFActivity((ushort)(m - m_sBaseAddress), returnValue, true);

            return returnValue;
        }

        byte PeekFloppy(ushort m) 
        {
            byte d = 0xFF;

            int nWhichRegister = m - m_sBaseAddress;
            int nDrive = 0;
            switch (m_DMAF_DRVRegister & 0x0F)
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

            switch ((DMAF_OffsetRegisters)nWhichRegister)
            {
                case DMAF_OffsetRegisters.DMAF_DATAREG_OFFSET:
                    d = m_DMAF_DATARegister;
                    break;
                case DMAF_OffsetRegisters.DMAF_STATREG_OFFSET:
                    d = m_DMAF_STATRegister;
                    break;

                case DMAF_OffsetRegisters.DMAF_DRVREG_OFFSET:
                    d = (byte)(~((byte)m_DMAF_DRVRegister | 0x40) & 0xFF);
                    break;

                case DMAF_OffsetRegisters.DMAF_TRKREG_OFFSET:
                    d = m_DMAF_TRKRegister;                      // get Track Register
                    break;

                case DMAF_OffsetRegisters.DMAF_SECREG_OFFSET:
                    d = m_DMAF_SECRegister;                      // get Track Register
                    break;

                case DMAF_OffsetRegisters.DMAF_HLD_TOGGLE_OFFSET:       // 0x50 - head load toggle
                    d = DMAF_HLD_TOGGLERegister;
                    break;
            }

            return d; 
        }

        byte PeekDMA(ushort m) 
        {
            byte d = 0xFF;
            int nWhichRegister = m - m_sBaseAddress;

            switch ((DMAF_OffsetRegisters)nWhichRegister)
            {
                case DMAF_OffsetRegisters.DMAF3_LATCH_OFFSET:
                    d = m_DMAF_LATCHRegister;                    // the Enable interrupt bit is in here somewhere
                    break;

                //*
                //*   DMAF 6844 DMA controller definitions
                //*

                case DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET + 0:                  // 0xF000
                    d = m_DMAF_DMA_ADDRESSRegister[0].hibyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET + 1:                  // 0xF001
                    d = m_DMAF_DMA_ADDRESSRegister[0].lobyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET + 4:                  // 0xF004
                    d = m_DMAF_DMA_ADDRESSRegister[1].hibyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET + 5:                  // 0xF005
                    d = m_DMAF_DMA_ADDRESSRegister[1].lobyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET + 8:                  // 0xF008
                    d = m_DMAF_DMA_ADDRESSRegister[2].hibyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET + 9:                  // 0xF009
                    d = m_DMAF_DMA_ADDRESSRegister[2].lobyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET + 12:                 // 0xF00C
                    d = m_DMAF_DMA_ADDRESSRegister[3].hibyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET + 13:                 // 0xF00D
                    d = m_DMAF_DMA_ADDRESSRegister[3].lobyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_DMCNT_OFFSET + 0:                    // 0xF002
                    d = m_DMAF_DMA_DMCNTRegister[0].hibyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_DMCNT_OFFSET + 1:                    // 0xF003
                    d = m_DMAF_DMA_DMCNTRegister[0].lobyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_DMCNT_OFFSET + 4:                    // 0xF006
                    d = m_DMAF_DMA_DMCNTRegister[1].hibyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_DMCNT_OFFSET + 5:                    // 0xF007
                    d = m_DMAF_DMA_DMCNTRegister[1].lobyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_DMCNT_OFFSET + 8:                    // 0xF00A
                    d = m_DMAF_DMA_DMCNTRegister[2].hibyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_DMCNT_OFFSET + 9:                    // 0xF00B
                    d = m_DMAF_DMA_DMCNTRegister[2].lobyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_DMCNT_OFFSET + 12:                   // 0xF00E
                    d = m_DMAF_DMA_DMCNTRegister[3].hibyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_DMCNT_OFFSET + 13:                   // 0xF00F
                    d = m_DMAF_DMA_DMCNTRegister[3].lobyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_CHANNEL_OFFSET:                      // 0xF010
                    d = m_DMAF_DMA_CHANNELRegister[0];

                    // make sure the interrupt is already set by the timer before we set the high order bit of the DMA status register

                    if (
                        ((Program._cpu.InterruptRegister & m_nInterruptMask) == m_nInterruptMask) &&
                        ((m_DMAF_DMA_INTERRUPTRegister & 0x01) == 0x01) &&
                        ((m_nBoardInterruptRegister & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA1) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA1)
                       )
                        d |= 0x80;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_CHANNEL_OFFSET + 1:                  // 0xF011
                    d = m_DMAF_DMA_CHANNELRegister[1];

                    // make sure the interrupt is already set by the timer before we set the high order bit of the DMA status register

                    if (
                        ((Program._cpu.InterruptRegister & m_nInterruptMask) == m_nInterruptMask) &&
                        ((m_DMAF_DMA_INTERRUPTRegister & 0x02) == 0x02) &&
                        ((m_nBoardInterruptRegister & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA2) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA2)
                       )
                        d |= 0x80;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_CHANNEL_OFFSET + 2:                  // 0xF012
                    d = m_DMAF_DMA_CHANNELRegister[2];

                    // make sure the interrupt is already set by the timer before we set the high order bit of the DMA status register

                    if (
                        ((Program._cpu.InterruptRegister & m_nInterruptMask) == m_nInterruptMask) &&
                        ((m_DMAF_DMA_INTERRUPTRegister & 0x04) == 0x04) &&
                        ((m_nBoardInterruptRegister & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA3) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA3)
                       )
                        d |= 0x80;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_CHANNEL_OFFSET + 3:                  // 0xF013
                    d = m_DMAF_DMA_CHANNELRegister[3];

                    // make sure the interrupt is already set by the timer before we set the high order bit of the DMA status register

                    if (
                        ((Program._cpu.InterruptRegister & m_nInterruptMask) == m_nInterruptMask) &&
                        ((m_DMAF_DMA_INTERRUPTRegister & 0x08) == 0x08) &&
                        ((m_nBoardInterruptRegister & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA4) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA4)
                       )
                        d |= 0x80;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_PRIORITY_OFFSET:                     // 0xF014
                    d = m_DMAF_DMA_PRIORITYRegister;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_INTERRUPT_OFFSET:                    // 0xF015
                    d = m_DMAF_DMA_INTERRUPTRegister;
                    if (
                        ((Program._cpu.InterruptRegister & m_nInterruptMask) == m_nInterruptMask) &&
                        (
                            (((m_DMAF_DMA_INTERRUPTRegister & 0x01) == 0x01) && ((m_nBoardInterruptRegister & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA1) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA1)) ||
                            (((m_DMAF_DMA_INTERRUPTRegister & 0x02) == 0x02) && ((m_nBoardInterruptRegister & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA2) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA2)) ||
                            (((m_DMAF_DMA_INTERRUPTRegister & 0x04) == 0x04) && ((m_nBoardInterruptRegister & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA3) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA3)) ||
                            (((m_DMAF_DMA_INTERRUPTRegister & 0x08) == 0x08) && ((m_nBoardInterruptRegister & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA4) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA4))
                        )
                       )
                        d |= 0x80;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_CHAIN_OFFSET:                        // 0xF016
                    d = m_DMAF_DMA_CHAINRegister;
                    break;
            }
            return (d);
        }

        byte PeekWD1000(ushort m) 
        {
            byte d = 0xFF;
            int nWhichRegister = m - m_sBaseAddress;
            int activechannel = -1;

            switch (m_DMAF_DMA_PRIORITYRegister & 0x0F)
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

            byte latch = m_DMAF_LATCHRegister;
            ushort addr = 0;
            ushort cnt = 0;
            byte priority = m_DMAF_DMA_PRIORITYRegister;
            byte interrupt = m_DMAF_DMA_INTERRUPTRegister;
            byte chain = m_DMAF_DMA_CHAINRegister;

            if (activechannel == 1)
            {
                addr = (ushort)(m_DMAF_DMA_ADDRESSRegister[activechannel].hibyte * 256 + m_DMAF_DMA_ADDRESSRegister[activechannel].lobyte);
                cnt = (ushort)(m_DMAF_DMA_DMCNTRegister[activechannel].hibyte * 256 + m_DMAF_DMA_DMCNTRegister[activechannel].lobyte);
            }

            int nDrive = (DMAF_WDC_SDHRegister >> 3) & 0x03;

            switch ((DMAF_OffsetRegisters)nWhichRegister)
            {
                // Winchester WD1000 5" controller

                case DMAF_OffsetRegisters.DMAF_WDC_DATA_OFFSET:         // 0x30 - WDC data register
                    d = DMAF_WDC_DATARegister;
                    break;

                case DMAF_OffsetRegisters.DMAF_WDC_ERROR_OFFSET:        // 0x31 - WDC error register
                                                                          //    wd_error   equ     wd1000+1       error register (read only)
                                                                          //    *                                 bit 7 bad block detect
                                                                          //    *                                 bit 6 CRC error, data field
                                                                          //    *                                 bit 5 CRC error, ID field
                                                                          //    *                                 bit 4 ID not found
                                                                          //    *                                 bit 3 unused
                                                                          //    *                                 bit 2 Aborted Command
                                                                          //    *                                 bit 1 TR000 (track zero) error
                                                                          //    *                                 bit 0 DAM not found
                    d = DMAF_WDC_ERRORRegister;
                    break;

                case DMAF_OffsetRegisters.DMAF_WDC_SECCNT_OFFSET:       // 0x32 - sector count (during format)
                    d = DMAF_WDC_SECCNTRegister;
                    break;

                case DMAF_OffsetRegisters.DMAF_WDC_SECNUM_OFFSET:       // 0x33 - WDC sector number
                    d = DMAF_WDC_SECNUMRegister;
                    break;

                case DMAF_OffsetRegisters.DMAF_WDC_CYLLO_OFFSET:        // 0x34 - WDC cylinder (low part)
                    d = DMAF_WDC_CYLLORegister;
                    break;

                case DMAF_OffsetRegisters.DMAF_WDC_CYLHI_OFFSET:        // 0x35 - WDC cylinder (high part)
                    d = DMAF_WDC_CYLHIRegister;
                    break;

                case DMAF_OffsetRegisters.DMAF_WDC_SDH_OFFSET:          // 0x36 - WDC size/drive/head
                    d = DMAF_WDC_SDHRegister;
                    break;

                case DMAF_OffsetRegisters.DMAF_WDC_STATUS_OFFSET:       // 0x37 - WDC status register
                    d = DMAF_WDC_STATUSRegister;
                    break;

                case DMAF_OffsetRegisters.DMAF_WD1000_RES_OFFSET:        // 0x51 - winchester software reset
                    d = DMAF_WD1000_RESRegister;
                    break;

            }
            return (d);
        }
        byte PeekTape(ushort m) 
        {
            byte d = 0xFF;
            int nWhichRegister = m - m_sBaseAddress;
            switch ((DMAF_OffsetRegisters)nWhichRegister)
            {
                case DMAF_OffsetRegisters.DMAF_AT_DTB_OFFSET:           // 0x40 - B-Side Data Register
                    d = DMAF_AT_DTBRegister;
                    break;

                case DMAF_OffsetRegisters.DMAF_AT_DTA_OFFSET:           // 0x41 - A-Side Data Register
                    d = DMAF_AT_DTARegister;
                    break;

                case DMAF_OffsetRegisters.DMAF_AT_DRB_OFFSET:           // 0x42 - B-Side Direction Register
                    d = DMAF_AT_DRBRegister;
                    break;

                case DMAF_OffsetRegisters.DMAF_AT_DRA_OFFSET:           // 0x43 - A-Side Direction Register
                    d = DMAF_AT_DRARegister;
                    break;

                case DMAF_OffsetRegisters.DMAF_AT_T1C_OFFSET:           // 0x44 - Timer 1 Counter Register
                    d = m_DMAF_AT_TCRegister[0].lobyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_AT_T1C_OFFSET + 1:       // 0x45 - Timer 1 Counter Register
                    d = m_DMAF_AT_TCRegister[0].hibyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_AT_T1L_OFFSET:           // 0x46 - Timer 1 Latches
                    d = m_DMAF_AT_TLRegister.lobyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_AT_T1L_OFFSET + 1:       // 0x47 - Timer 1 Latches
                    d = m_DMAF_AT_TLRegister.hibyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_AT_T2C_OFFSET:           // 0x48 - Timer 2 Counter Register
                    d = m_DMAF_AT_TCRegister[1].lobyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_AT_T2C_OFFSET + 1:       // 0x49 - Timer 2 Counter Register
                    d = m_DMAF_AT_TCRegister[1].hibyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_AT_VSR_OFFSET:           // 0x4A - Shift Register
                    d = DMAF_AT_VSRRegister;
                    break;

                case DMAF_OffsetRegisters.DMAF_AT_ACR_OFFSET:           // 0x4B - Auxillary Control Register
                    d = DMAF_AT_ACRRegister;
                    break;

                case DMAF_OffsetRegisters.DMAF_AT_PCR_OFFSET:           // 0x4C - Peripheral Control Register
                    d = DMAF_AT_PCRRegister;
                    break;

                case DMAF_OffsetRegisters.DMAF_AT_IFR_OFFSET:           // 0x4D - Interrupt Flag Register
                    d = DMAF_AT_IFRRegister;
                    break;

                case DMAF_OffsetRegisters.DMAF_AT_IER_OFFSET:           // 0x4E - Interrupt Enable Register
                    d = DMAF_AT_IERRegister;
                    break;

                case DMAF_OffsetRegisters.DMAF_AT_DXA_OFFSET:           // 0x4F - A-Side Data Register
                    d = DMAF_AT_DXARegister;
                    break;

                case DMAF_OffsetRegisters.DMAF_AT_ATR_OFFSET:           // 0x52 - archive RESET
                    d = DMAF_AT_ATRRegister;
                    break;

                case DMAF_OffsetRegisters.DMAF_AT_DMC_OFFSET:           // 0x53 - archive DMA clear
                    d = DMAF_AT_DMCRegister;
                    break;

                case DMAF_OffsetRegisters.DMAF_AT_DMP_OFFSET:           // 0x60 - archive DMA preset
                    d = DMAF_AT_DMPRegister;
                    break;

            }
            return (d);
        }

        byte PeekCDS(ushort m) 
        {
            byte d = 0xFF;
            int nWhichRegister = m - m_sBaseAddress;

            switch ((DMAF_OffsetRegisters)nWhichRegister)
            {
                case DMAF_OffsetRegisters.DMAF_CDSCMD0_OFFSET:          // 0x0100      
                    d = DMAF_CDSCMD0Register;
                    break;

                case DMAF_OffsetRegisters.DMAF_CDSFLG0_OFFSET:          // 0x0103 - marksman flag (1xxx xxxx = yes)
                    d = DMAF_CDSFLG0Register;
                    break;

                case DMAF_OffsetRegisters.DMAF_CDSCMD1_OFFSET:          // 0x0300      
                    d = DMAF_CDSCMD1Register;
                    break;

                case DMAF_OffsetRegisters.DMAF_CDSFLG1_OFFSET:          // 0x0303 - marksman flag (1xxx xxxx = yes)
                    d = DMAF_CDSFLG1Register;
                    break;
            }
            return (d);
        }

        // need Peek function for board Peek and individual Peek Functions for device Peeks
        public override byte Peek(ushort m)
        {
            DMAF_OffsetRegisters nWhichRegister = (DMAF_OffsetRegisters)(m - m_sBaseAddress);

            if ((nWhichRegister >= DMAF_OffsetRegisters.DMAF_STATREG_OFFSET && nWhichRegister <= DMAF_OffsetRegisters.DMAF_DRVREG_OFFSET) || nWhichRegister == DMAF_OffsetRegisters.DMAF_HLD_TOGGLE_OFFSET)
                return PeekFloppy(m);
            else if ((nWhichRegister >= DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET && nWhichRegister <= DMAF_OffsetRegisters.DMAF_DMA_CHAIN_OFFSET) || nWhichRegister == DMAF_OffsetRegisters.DMAF3_LATCH_OFFSET)
                return PeekDMA(m);
            else if ((nWhichRegister >= DMAF_OffsetRegisters.DMAF_WDC_DATA_OFFSET && nWhichRegister <= DMAF_OffsetRegisters.DMAF_WDC_CMD_OFFSET) || nWhichRegister == DMAF_OffsetRegisters.DMAF_WD1000_RES_OFFSET)
                return PeekWD1000(m);
            else if ((nWhichRegister >= DMAF_OffsetRegisters.DMAF_AT_DTB_OFFSET && nWhichRegister <= DMAF_OffsetRegisters.DMAF_AT_DXA_OFFSET) || nWhichRegister == DMAF_OffsetRegisters.DMAF_AT_ATR_OFFSET || nWhichRegister == DMAF_OffsetRegisters.DMAF_AT_DMC_OFFSET || nWhichRegister == DMAF_OffsetRegisters.DMAF_AT_DMP_OFFSET)
                return PeekTape(m);
            else if (nWhichRegister == DMAF_OffsetRegisters.DMAF_CDSCMD0_OFFSET || nWhichRegister == DMAF_OffsetRegisters.DMAF_CDSFLG0_OFFSET || nWhichRegister == DMAF_OffsetRegisters.DMAF_CDSCMD1_OFFSET || nWhichRegister == DMAF_OffsetRegisters.DMAF_CDSFLG1_OFFSET)
                return PeekCDS(m);
            else
                return Program._cpu.ReadFromFirst64K(m);   // memory read
        }

        public override void Init(int nWhichController, byte[] sMemoryBase, ushort sBaseAddress, int nRow, bool bInterruptEnabled)
        {
            m_nRow = nRow;
            base.Init(nWhichController, sMemoryBase, sBaseAddress, nRow, bInterruptEnabled);

            m_DMAF_DRVRegister              = 0;

            m_DMAF_STATRegister             = 0;
            m_DMAF_CMDRegister              = 0;
            m_DMAF_TRKRegister              = 0;
            m_DMAF_SECRegister              = 0;
            m_DMAF_DATARegister             = 0;

            m_DMAF_LATCHRegister            = 0;

            for (int i = 0; i < 4; i++)
            {
                m_DMAF_DMA_ADDRESSRegister[i] = new DMAF_DMA_ADDRESS_REGISTER();
                m_DMAF_DMA_ADDRESSRegister[i].hibyte = 0x00;
                m_DMAF_DMA_ADDRESSRegister[i].lobyte = 0x00;

                m_DMAF_DMA_DMCNTRegister[i] = new DMAF_DMA_BYTECNT_REGISTER();
                m_DMAF_DMA_DMCNTRegister[i].hibyte = 0x00;
                m_DMAF_DMA_DMCNTRegister[i].lobyte = 0x00;
            }

            for (int i = 0; i < 4; i++)
            {
                m_DMAF_DMA_CHANNELRegister[i] = 0x00;
            }

            m_DMAF_DMA_PRIORITYRegister       = 0x00;
            m_DMAF_DMA_INTERRUPTRegister      = 0x00;
            m_DMAF_DMA_CHAINRegister          = 0x00;

            DMAF_CDSCMD0Register  = 0x00;
            DMAF_CDSFLG0Register  = 0x00;
            DMAF_CDSCMD1Register  = 0x00;
            DMAF_CDSFLG1Register  = 0x00;

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

            StartDMAFTimer(m_nDMAFRate);
            m_nDMAFRunning = true;
        }
    }
}
