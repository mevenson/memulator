using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;

using System.IO;
using System.Threading;

namespace Memulator
{
    class PIAIDE : IODevice
    {
        bool _spin = false;

        static byte SETDDR      = 0x3A;             //* SET DATA REGISTER TO DIRECTION.
        static byte CLRDDR      = 0x3E;             //* RESTORE DATA REGISTER.
        //*
        //* IDE CONTROL PORT BITS.
        //*
        static byte DCIOX       = 0x03;            //  00000011       * ADD IN TO STOP READ AND WRITE.
        static byte DCIOR       = 0xFD;            //  11111101       * AND IN TO SET READ.
        static byte DCIOW       = 0xFE;            //  11111110       * AND IN TO SET WRITE.
        static byte DCCSX       = 0x60;            //  01100000       * ADD IN TO DE-SELECT.
        static byte DCCS0       = 0x40;            //  01000000       * ADD IN FOR SELECT0
        static byte DCCS1       = 0x20;            //  00100000       * ADD IN FOR SELECT1
        static byte DCA0        = 0x04;            //  00000100       * ADD IN FOR ADDRESS 0
        static byte DCA1        = 0x08;            //  00001000       * ADD IN FOR ADDRESS 1
        static byte DCA2        = 0x10;            //  00010000       * ADD IN FOR ADDRESS 2
        //*
        static byte IDECMD      = (byte)(DCA2|DCA1|DCA0|DCCS0|DCIOX);    //* ADD IN FOR FOR COMMAND 7
        static byte IDESTS      = (byte)(DCA2|DCA1|DCA0|DCCS0|DCIOX);    //* ADD IN FOR STATUS REGISTER 7
        static byte IDEHD       = (byte)(DCA2|DCA1|DCCS0|DCIOX);         //* ADD IN FOR HEAD 6
        static byte IDECYH      = (byte)(DCA2|DCA0|DCCS0|DCIOX);         //* ADD IN FOR CYLINDER HIGH 5
        static byte IDECYL      = (byte)(DCA2|DCCS0|DCIOX);              //* ADD IN FOR CYLINDER LOW 4
        static byte IDESEC      = (byte)(DCA1|DCA0|DCCS0|DCIOX);         //* ADD IN FOR SECTOR 3
        static byte IDENUM      = (byte)(DCA1|DCCS0|DCIOX);              //* ADD IN FOR NUMBER OF SECTORS 2
        static byte IDEERR      = (byte)(DCA0|DCCS0|DCIOX);              //* ADD IN FOR ERROR REGISTER 1
        static byte IDEDATA     = (byte)(DCCS0|DCIOX);                   //* ADD IN FOR FOR DATA BUS 0
        //*
        //* THE HEAD NUMBER (0..F) ALSO HAS THE MASK FOR MASTER/SLAVE
        //* I FIX THIS AT MASTER, I DO NOT THINK I WILL USE TWO DRIVES
        //* ON MY IDE INTERFACE.
        //*
        static byte IDEHDA      = 0x0F;            //  00001111       * HEAD NUMBER AND MASK
        static byte IDEHDO      = 0xA0;            //  10100000       * CHS HEAD NUMBER OR MASK
        static byte LBAHDO      = 0xE0;            //  11100000       * SETS LBA MODE
        //*
        //* THE RESET/IRQ REGISTER HAS TWO INTERESTING BITS
        //*
        static byte IDESRES     = 0x04;            //  00000100       //* SOFT RESET BIT
        static byte IDENIRQ     = 0x02;            //  00000010       //* 0 = IRQ ACTIVE
        //*
        //* STATUS BITS FROM IDESTS
        //*
        static byte STSBSY      = 0x80;            //  10000000       * BUSY FLAG
        static byte STSRDY      = 0x40;            //  01000000       * READY FLAG
        static byte STSWFT      = 0x20;            //  00100000       * WRITE ERROR
        static byte STSSKC      = 0x10;            //  00010000       * SEEK COMPLETE
        static byte STSDRQ      = 0x08;            //  00001000       * DATA REQUEST
        static byte STSCORR     = 0x04;            //  00000100       * ECC EXECUTED
        static byte STSIDX      = 0x02;            //  00000010       * INDEX FOUND
        static byte STSERR      = 0x01;            //  00000001       * ERROR FLAG
        //*
        //* ERROR BITS FROM IDEERR
        //*
        static byte ERRBBK      = 0x80;            //  10000000       * BAD BLOCK DETECTED.
        static byte ERRUNC      = 0x40;            //  01000000       * UNCORRECTABLE DATA ERROR.
        static byte ERRMC       = 0x20;            //  00100000       * MEDIA CHANGED.
        static byte ERRIDNF     = 0x10;            //  00010000       * ID NOT FOUND.
        static byte ERRMCR      = 0x08;            //  00001000       * MEDIA CHANGE RQUESTED.
        static byte ERRABRT     = 0x04;            //  00000100       * ABORTED COMMAND.
        static byte ERRTK0NF    = 0x02;            //  00000010       * TRACK 0 NOT FOUND.
        static byte ERRAMNF     = 0x01;            //  00000001       * ADDRESS MARK NOT FOUND.

        #region enums
        //*
        //* COMMAND OPCODES - must be enums for switch statement
        //*
        enum CommandOpcodes
        {
            CMDRECAL    = 0x10,             //* RECALIBRATE DISK (OPTIONAL)
            CMDREAD     = 0x20,             //* READ  BLOCK WITH RETRY.
            CMDWRITE    = 0x30,             //* WRITE BLOCK WITH RETRY.
            CMDRDVER    = 0x40,             //* VERIFY DATA WITH RETRY.
            CMDSEEK     = 0x70,             //* SEEK SECTOR
            CMDSTOP     = 0xE0,             //* STOP DISK
            CMDSTRT     = 0xE1,             //* START DISK
            CMDIDENT    = 0xEC,             //* IDENTIFY DISK
            CMDSETPAR   = 0x91              //* SET CYL/TRACK AND SECT/TRACK
        }
        #endregion

        int m_nRow;

        ushort m_PIACtlPortA;
        ushort m_PIADataPortA;
        ushort m_PIACtlPortB;
        ushort m_PIADataPortB;

        byte m_PIACtlPortRegisterA;
        byte m_PIADataPortRegisterA;
        byte m_PIACtlPortRegisterB;
        byte m_PIADataPortRegisterB;

        byte m_nPortADirectionMask;
        byte m_nPortBDirectionMask;

        bool m_bReading;
        bool m_bWriting;
        bool m_bDrive0Selected;
        bool m_bDrive1Selected;

        byte m_nIDEAddressSelected;

        byte [] m_caReadBuffer = new byte[65536];
        byte [] m_caWriteBuffer = new byte[65536];

        int m_nHead, m_nCylinder, m_nSector, m_nSectorCount, m_nErrorRegister;
        bool m_bLABMode;

        uint m_nStatusReads;

        int m_nIDEReadPtr;
        int m_nIDEWritePtr;

        bool m_nIDEReading;
        bool m_nIDEWriting;

        byte [] m_IDE_DATARegister = new byte[2];
        byte [] m_IDE_STATRegister = new byte[2];

        long m_lFileOffset;

        byte [] m_cNumberOfLBABlocks = new byte[4];
        byte [] m_cNumberOfCylinders = new byte[4];
        byte [] m_cNumberOfHeads = new byte[1];
        byte [] m_cNumberOfSectorsPerTrack = new byte[1];
        byte [] m_cNumberOfBytesPerSector = new byte[2];

        ulong [] m_nNumberOfLBABlocks = new ulong[2];
        uint  [] m_nNumberOfCylinders = new uint[2];
        uint  [] m_nNumberOfHeads = new uint[2];
        uint  [] m_nNumberOfSectorsPerTrack = new uint[2];
        uint  [] m_nNumberOfBytesPerSector = new uint[2];

        public PIAIDE()
        {
            m_bReading          = false;
            m_bWriting          = false;
            m_bDrive0Selected   = false;
            m_bDrive1Selected   = false;

            m_nIDEAddressSelected = 0;
            m_nHead = m_nCylinder = m_nSector = m_nSectorCount = m_nErrorRegister = 0;
            m_bLABMode = false;

            m_PIACtlPortRegisterA   = 0x00;
            m_PIADataPortRegisterA  = 0x00;
            m_PIACtlPortRegisterB   = 0x00;
            m_PIADataPortRegisterB  = 0x00;

            m_nIDEReading = false;
            m_nIDEWriting = false;

            m_bReading          = false;
            m_bWriting          = false;
            m_bDrive0Selected   = false;
            m_bDrive1Selected   = false;

            for (int i = 0; i < 2; i++)
            {
                Program.m_fpPIAIDE[i] = null;

                m_nNumberOfLBABlocks[i]        = 0;
                m_nNumberOfCylinders[i]        = 0;
                m_nNumberOfHeads[i]            = 0;
                m_nNumberOfSectorsPerTrack[i]  = 0;
                m_nNumberOfBytesPerSector[i]   = 0;

                m_IDE_STATRegister[i] = 0x00;
            }
        }

        void CalcFileOffset (int nDrive)
        {
            int nPhysicalSector = m_nSector - 1;

            if (m_bLABMode)
            {
                long lBlockNumber = (m_nHead * 65536 * 256) + (m_nCylinder * 256) + m_nSector;
                m_lFileOffset = lBlockNumber * 256;
            }
            else
            {
                if ((Program.m_nPIAIDEDiskFormat[nDrive] == DiskFormats.DISK_FORMAT_FLEX) || (Program.m_nPIAIDEDiskFormat[nDrive] == DiskFormats.DISK_FORMAT_UNIFLEX))
                {
                    m_lFileOffset = (long) 
                    (
                        (
                            (m_nCylinder * m_nNumberOfSectorsPerTrack[nDrive] * m_nNumberOfHeads[nDrive]) + // calc the block offset to the correct cylinder
                            (m_nHead * m_nNumberOfSectorsPerTrack[nDrive]) +                                // add in the block offset to the start of this track
                            nPhysicalSector                                                                 // add in the block offset to the correct sector
                        ) * 
                        m_nNumberOfBytesPerSector[nDrive]                                                   // multiply * the number of bytes per sector
                    );
                }
            }
        }

        void ExecuteIDECommand (byte b)
        {
            int nDrive = -1;

            if (m_bDrive0Selected)
                nDrive = 0;
            else if (m_bDrive1Selected)
                nDrive = 1;
            
            if (nDrive != -1)
            {
                switch (b)
                {
                    case (int)CommandOpcodes.CMDRECAL:    //    0x10     RECALIBRATE DISK (OPTIONAL)
                        m_IDE_STATRegister[nDrive] = (byte)(STSRDY | STSSKC);
                        break;

                    case (int)CommandOpcodes.CMDREAD:     //    0x20     READ  BLOCK WITH RETRY.
                        m_nIDEReading = true;
                        m_nIDEWriting = false;
                        m_nIDEReadPtr = 0;

                        m_nStatusReads = 0;

                        CalcFileOffset (nDrive);

                        // fill the sector buffer and set the first byte into the data register, then set the DRQ bit to signal data is available for reading

                        if (Program.m_fpPIAIDE[nDrive] != null)
                        {
                            Program.m_fpPIAIDE[nDrive].Seek((long)m_lFileOffset, SeekOrigin.Begin);                                             // fseek(Program.m_fpPIAIDE[nDrive], m_lFileOffset, SEEK_SET);
                            Program.m_fpPIAIDE[nDrive].Read(m_caReadBuffer, 0, (int)(m_nNumberOfBytesPerSector[nDrive] * m_nSectorCount));      //fread (m_caReadBuffer, m_nNumberOfBytesPerSector[nDrive] * m_nSectorCount, 1, Program.m_fpPIAIDE[nDrive]);

                            m_IDE_DATARegister[nDrive] = m_caReadBuffer[0];
                            m_IDE_STATRegister[nDrive] = (byte)(STSBSY | STSRDY | STSSKC | STSDRQ | STSIDX);
                        }
                        break;

                    case (int)CommandOpcodes.CMDWRITE:    //    0x30     WRITE BLOCK WITH RETRY.
                        m_nIDEReading = false;
                        m_nIDEWriting = true;
                        m_nIDEWritePtr = 0;

                        m_nStatusReads = 0;

                        m_IDE_STATRegister[nDrive] |= (byte)(STSRDY | STSDRQ);        // (STSBSY | STSRDY | STSSKC | STSDRQ);
                        break;

                    case (int)CommandOpcodes.CMDRDVER:    //    0x40     VERIFY DATA WITH RETRY.
                        m_IDE_STATRegister[nDrive] = (byte)(STSRDY | STSSKC);
                        break;

                    case (int)CommandOpcodes.CMDSEEK:     //    0x70     SEEK SECTOR
                        CalcFileOffset (nDrive);
                        m_IDE_STATRegister[nDrive] = (byte)(STSRDY | STSSKC);
                        break;

                    case (int)CommandOpcodes.CMDSTOP:     //    0xE0     STOP DISK
                        m_IDE_STATRegister[nDrive] = (byte)(STSRDY | STSSKC);
                        break;

                    case (int)CommandOpcodes.CMDSTRT:     //    0xE1     START DISK
                        m_IDE_STATRegister[nDrive] = (byte)(STSRDY | STSSKC);
                        break;

                    case (int)CommandOpcodes.CMDIDENT:    //    0xEC     IDENTIFY DISK
                        m_IDE_STATRegister[nDrive] = (byte)(STSRDY | STSSKC);
                        break;

                    case (int)CommandOpcodes.CMDSETPAR:   //    0x91     SET CYL/TRACK AND SECT/TRACK
                        m_IDE_STATRegister[nDrive] = (byte)(STSRDY | STSSKC);
                        break;
                }
            }
        }

        public override void Write(ushort m, byte b)
        {
            // The B side of the PIA is used to address the IDE controller.

            if (m == m_PIACtlPortA)
            {
                m_PIACtlPortRegisterA = b;
            }
            else if (m == m_PIADataPortA)
            {
                if (m_PIACtlPortRegisterA == SETDDR)
                {
                    m_nPortADirectionMask = b;
                }
                else if (m_PIACtlPortRegisterA == CLRDDR)
                {
                    switch (m_nIDEAddressSelected)
                    {
                        case 7:      //  DCA2|DCA1|DCA0|DCCS0|DCIOX    * ADD IN FOR FOR COMMAND 7
                            ExecuteIDECommand(b);
                            break;

                        case 6:       //  DCA2|DCA1|DCCS0|DCIOX         * ADD IN FOR HEAD 6

                            if ((b & 0x40) == 0x40)
                                m_bLABMode = true;
                            else
                                m_bLABMode = false;

                            m_nHead = b & 0x1F;

                            break;

                        case 5:      //  DCA2|DCA0|DCCS0|DCIOX         * ADD IN FOR CYLINDER HIGH 5
                            m_nCylinder = (m_nCylinder & 0x00ff) + (b * 256);
                            break;

                        case 4:      //  DCA2|DCCS0|DCIOX              * ADD IN FOR CYLINDER LOW 4
                            m_nCylinder = (m_nCylinder & 0xff00) + b;
                            break;

                        case 3:      //  DCA1|DCA0|DCCS0|DCIOX         * ADD IN FOR SECTOR 3
                            m_nSector = b;
                            break;

                        case 2:      //  DCA1|DCCS0|DCIOX              * ADD IN FOR NUMBER OF SECTORS 2
                            m_nSectorCount = b;
                            break;

                        case 1:      //  DCA0|DCCS0|DCIOX              * ADD IN FOR ERROR REGISTER 1
                            m_nErrorRegister = b;
                            break;

                        case 0:     //  DCCS0|DCIOX                   * ADD IN FOR FOR DATA BUS 0
                            if (m_nIDEWriting)
                            {
                                int nDrive = -1;

                                if (m_bDrive0Selected)
                                    nDrive = 0;
                                else if (m_bDrive1Selected)
                                    nDrive = 1;

                                if (nDrive != -1)
                                {
                                    m_caWriteBuffer[m_nIDEWritePtr++] = b;

                                    if (m_nIDEWritePtr == (m_nNumberOfBytesPerSector[nDrive] * m_nSectorCount))
                                    {
                                        CalcFileOffset(nDrive);

                                        if (Program.m_fpPIAIDE[nDrive] != null)
                                        {
                                            Program.m_fpPIAIDE[nDrive].Seek((long)m_lFileOffset, SeekOrigin.Begin);                                             // fseek(Program.m_fpPIAIDE[nDrive], m_lFileOffset, SEEK_SET);
                                            Program.m_fpPIAIDE[nDrive].Write(m_caWriteBuffer, 0, (int)(m_nNumberOfBytesPerSector[nDrive] * m_nSectorCount));    // fwrite (m_caWriteBuffer, 1, m_nNumberOfBytesPerSector[nDrive] * m_nSectorCount, Program.m_fpPIAIDE[nDrive]);
                                            Program.m_fpPIAIDE[nDrive].Flush();                                                                                 // fflush(Program.m_fpPIAIDE[nDrive]);
                                        }

                                        m_IDE_STATRegister[nDrive] &= (byte)(~(STSDRQ | STSBSY));
                                        m_nIDEWriting = false;
                                        ClearInterrupt();
                                    }
                                    else
                                        m_IDE_STATRegister[nDrive] |= STSDRQ;               // ask for more data cause the buffer ain't full yet
                                }
                            }
                            break;

                        default:
                            break;
                    }
                }
            }
            else if (m == m_PIACtlPortB)
            {
                m_PIACtlPortRegisterB = b;
            }
            else if (m == m_PIADataPortB)
            {
                if (m_PIACtlPortRegisterB == SETDDR)
                {
                    m_nPortBDirectionMask = b;
                }
                else if (m_PIACtlPortRegisterB == CLRDDR)
                {
                    m_PIADataPortRegisterB = b;

                    //DCIOX       00000011       //* ADD IN TO STOP READ AND WRITE.
                    //DCIOR       11111101       //* AND IN TO SET READ.
                    //DCIOW       11111110       //* AND IN TO SET WRITE.

                    //DCCSX       01100000       //* ADD IN TO DE-SELECT.
                    //DCCS0       01000000       //* ADD IN FOR SELECT0
                    //DCCS1       00100000       //* ADD IN FOR SELECT1

                    //DCA0        00000100       //* ADD IN FOR ADDRESS 0
                    //DCA1        00001000       //* ADD IN FOR ADDRESS 1
                    //DCA2        00010000       //* ADD IN FOR ADDRESS 2

                    m_bReading = (b & ~DCIOR) == 0 ? true : false;
                    m_bWriting = (b & ~DCIOW) == 0 ? true : false; ;

                    // set the current drive selected. If neither bits are set - select no drive. Always make sure when using this value that it's
                    // greater than 0. and then bias by subtracting 1;

                    m_bDrive0Selected = (b & DCCS0) == DCCS0 ? true : false;
                    m_bDrive1Selected = (b & DCCS1) == DCCS1 ? true : false;

                    m_nIDEAddressSelected = (byte)((b & (byte)(DCA0 | DCA1 | DCA2)) >> 2);
                    if (m_nIDEAddressSelected == 0)
                    {
                    }
                }
            }
        }

        public override byte Read(ushort m)
        {
            // A 1 in the m_nPortXDirectionMask means the bit position is an output 
            // bit so only return the bits that are 0 in the m_nPortXDirectionMask.

            byte c = m_PIADataPortRegisterA;

            if (m == m_PIACtlPortA)
                c = m_PIACtlPortRegisterA;
            else if (m == m_PIADataPortA)
            {
                byte nDirectionBits = (byte)~m_nPortADirectionMask;
                switch (m_nIDEAddressSelected)
                {
                    case 7:             //  DCA2|DCA1|DCA0|DCCS0|DCIOX    * ADD IN FOR FOR STATUS 7
                        {
                            int nDrive = -1;

                            if (m_bDrive0Selected)
                                nDrive = 0;
                            else if (m_bDrive1Selected)
                                nDrive = 1;

                            if (nDrive != -1)
                            {
                                if (!m_nIDEReading && !m_nIDEWriting)           // turn off BUSY if not read/writing
                                    m_IDE_STATRegister[nDrive] &= (byte)(~STSBSY);
                                else
                                {
                                    if (++m_nStatusReads > (m_nNumberOfBytesPerSector[nDrive] * m_nSectorCount * 2))      // turns out we really don't need this - just slows us down
                                        m_IDE_STATRegister[nDrive] = STSRDY;
                                }
                                c = m_IDE_STATRegister[nDrive];
                            }
                            else
                                c = STSRDY;
                        }
                        break;

                    case 6:                 //  DCA2|DCA1|DCCS0|DCIOX         * ADD IN FOR HEAD 6
                        c = (byte)(m_nHead & nDirectionBits);
                        break;

                    case 5:        //  DCA2|DCA0|DCCS0|DCIOX         * ADD IN FOR CYLINDER HIGH 5
                        c = (byte)((m_nCylinder / 256) & nDirectionBits);
                        break;

                    case 4:        //  DCA2|DCCS0|DCIOX              * ADD IN FOR CYLINDER LOW 4
                        c = (byte)((m_nCylinder & 0x00ff) & nDirectionBits);
                        break;

                    case 3:        //  DCA1|DCA0|DCCS0|DCIOX         * ADD IN FOR SECTOR 3
                        c = (byte)(m_nSector & nDirectionBits);
                        break;

                    case 2:        //  DCA1|DCCS0|DCIOX              * ADD IN FOR NUMBER OF SECTORS 2
                        c = (byte)(m_nSectorCount & nDirectionBits);
                        break;

                    case 1:        //  DCA0|DCCS0|DCIOX              * ADD IN FOR ERROR REGISTER 1
                        c = 0;
                        break;

                    case 0:       //  DCCS0|DCIOX                   * ADD IN FOR FOR DATA BUS 0
                        {
                            int nDrive = -1;

                            if (m_bDrive0Selected)
                                nDrive = 0;
                            else if (m_bDrive1Selected)
                                nDrive = 1;

                            // this is where we actually return the sector byte by byte until we have returned them all

                            if (nDrive != -1)
                            {
                                m_IDE_DATARegister[nDrive] = m_caReadBuffer[m_nIDEReadPtr];                     // get next byte from the sector buffer
                                lock (Program._cpu.buildingDebugLineLock)
                                {
                                    m_nIDEReadPtr++;
                                }
                                if (m_nIDEReadPtr == (m_nNumberOfBytesPerSector[nDrive] * m_nSectorCount))  // see if we are done
                                {
                                    lock (Program._cpu.buildingDebugLineLock)
                                    {
                                    m_IDE_STATRegister[nDrive] &= (byte)(~(STSDRQ | STSBSY));               // yes we are - set status to not busy and no more DRQ
                                    m_nIDEReading = false;                                                  // set that we are no longer reading
                                    ClearInterrupt();
                                    }
                                }
                                else
                                {
                                    lock (Program._cpu.buildingDebugLineLock)
                                    {
                                    m_IDE_STATRegister[nDrive] |= STSDRQ;                                   // if we are not done yet - set DRQ
                                    }
                                }

                                c = (byte)(m_IDE_DATARegister[nDrive] & nDirectionBits);                    // filter out the bits that are not set as output
                                m_nStatusReads = 0;
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
            else if (m == m_PIACtlPortB)
                c = m_PIACtlPortRegisterB;
            else if (m == m_PIADataPortB)
            {
                byte nDirectionBits = (byte)~m_nPortBDirectionMask;
                c = (byte)(m_PIADataPortRegisterB & nDirectionBits);
            }
            return (c);
        }
        public override byte Peek(ushort m)
        {
            byte c = m_PIADataPortRegisterA;
            if (m == m_PIACtlPortA)
                c = m_PIACtlPortRegisterA;
            else if (m == m_PIADataPortA)
            {
                byte nDirectionBits = (byte)~m_nPortADirectionMask;
                switch (m_nIDEAddressSelected)
                {
                    case 7:             //  DCA2|DCA1|DCA0|DCCS0|DCIOX    * ADD IN FOR FOR STATUS 7
                        {
                            int nDrive = -1;
                            if (m_bDrive0Selected)
                                nDrive = 0;
                            else if (m_bDrive1Selected)
                                nDrive = 1;
                            if (nDrive != -1)
                            {
                                c = m_IDE_STATRegister[nDrive];
                                if (!m_nIDEReading && !m_nIDEWriting)           // turn off BUSY if not read/writing
                                    c = (byte)(m_IDE_STATRegister[nDrive] & (byte)(~STSBSY));
                                else
                                {
                                    if (++m_nStatusReads > (m_nNumberOfBytesPerSector[nDrive] * m_nSectorCount * 2))      // turns out we really don't need this - just slows us down
                                    {
                                        c = (byte)(m_IDE_STATRegister[nDrive] & STSRDY);
                                    }
                                }
                            }
                            else
                                c = STSRDY;
                        }
                        break;
                    case 6:                 //  DCA2|DCA1|DCCS0|DCIOX         * ADD IN FOR HEAD 6
                        c = (byte)(m_nHead & nDirectionBits);
                        break;
                    case 5:        //  DCA2|DCA0|DCCS0|DCIOX         * ADD IN FOR CYLINDER HIGH 5
                        c = (byte)((m_nCylinder / 256) & nDirectionBits);
                        break;
                    case 4:        //  DCA2|DCCS0|DCIOX              * ADD IN FOR CYLINDER LOW 4
                        c = (byte)((m_nCylinder & 0x00ff) & nDirectionBits);
                        break;
                    case 3:        //  DCA1|DCA0|DCCS0|DCIOX         * ADD IN FOR SECTOR 3
                        c = (byte)(m_nSector & nDirectionBits);
                        break;
                    case 2:        //  DCA1|DCCS0|DCIOX              * ADD IN FOR NUMBER OF SECTORS 2
                        c = (byte)(m_nSectorCount & nDirectionBits);
                        break;
                    case 1:        //  DCA0|DCCS0|DCIOX              * ADD IN FOR ERROR REGISTER 1
                        c = 0;
                        break;
                    case 0:       //  DCCS0|DCIOX                   * ADD IN FOR FOR DATA BUS 0
                        {
                            int nDrive = -1;
                            if (m_bDrive0Selected)
                                nDrive = 0;
                            else if (m_bDrive1Selected)
                                nDrive = 1;
                            if (nDrive != -1)
                            {
                                c = m_caReadBuffer[m_nIDEReadPtr];                     // get next byte from the sector buffer
                                if (m_nIDEReadPtr == (m_nNumberOfBytesPerSector[nDrive] * m_nSectorCount))      // see if we are done
                                {
                                    c = (byte)(m_IDE_STATRegister[nDrive] & (byte)(~(STSDRQ | STSBSY)));        // yes we are - set status to not busy and no more DRQ
                                }
                                else
                                {
                                    c = (byte)(m_IDE_STATRegister[nDrive] |= STSDRQ);                           // if we are not done yet - set DRQ
                                }
                                c = (byte)(c & nDirectionBits);                        // filter out the bits that are not set as output
                            }
                        }
                        break;

                    default:
                        break;
                }
            }
            else if (m == m_PIACtlPortB)
                c = m_PIACtlPortRegisterB;
            else if (m == m_PIADataPortB)
            {
                byte nDirectionBits = (byte)~m_nPortBDirectionMask;
                c = (byte)(m_PIADataPortRegisterB & nDirectionBits);
            }

            return (c);
        }

        public override void Init(int nWhichController, byte[] sMemoryBase, ushort sBaseAddress, int nRow, bool bInterruptEnabled)
        {
            m_nRow = nRow;
            base.Init (nWhichController, sMemoryBase, sBaseAddress, nRow, bInterruptEnabled);

            ClearInterrupt ();

            // Set up addresses and references

            for (int i = 0; i < 2; i++)
            {
                string imageFormat             = Program.GetConfigurationAttribute(Program.ConfigSection + "/PIAIDEDisks/Disk", "Format", i.ToString(), "");
                Program.m_strPIAIDEFilename[i] = Program.dataDir + Program.GetConfigurationAttribute(Program.ConfigSection + "/PIAIDEDisks/Disk", "Path", i.ToString(), "");

                if (Program.m_strPIAIDEFilename[i].Length > Program.dataDir.Length)
                {
                    try
                    {
                        Program.m_fpPIAIDE[i] = File.Open(Program.m_strPIAIDEFilename[i], FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);

                        byte[] cInfoSize = new byte[2];
                        uint nInfoSize = 0;

                        Program.m_fpPIAIDE[i].Seek(-2, SeekOrigin.End);                             // fseek(Program.m_fpPIAIDE[i], -2, SEEK_END);
                        long lPosition = Program.m_fpPIAIDE[i].Position;                            // long lPosition = ftell (Program.m_fpPIAIDE[i]);

                        Program.m_fpPIAIDE[i].Read(cInfoSize, 1, 1);                                // fread (&cInfoSize[1], 1, 1, Program.m_fpPIAIDE[i]);
                        Program.m_fpPIAIDE[i].Read(cInfoSize, 0, 1);                                // fread (&cInfoSize[0], 1, 1, Program.m_fpPIAIDE[i]);

                        nInfoSize = (uint)(((cInfoSize[1] * 256) + cInfoSize[0]) & 0x00ffff);       // nInfoSize = *(uint*)cInfoSize & 0x00ffff;

                        Program.m_fpPIAIDE[i].Seek((int)(0 - nInfoSize), SeekOrigin.End);                  // fseek (Program.m_fpPIAIDE[i], 0 - nInfoSize, SEEK_END);
                        lPosition = Program.m_fpPIAIDE[i].Position;

                        Program.m_fpPIAIDE[i].Read(m_cNumberOfLBABlocks, 3, 1);                     // fread (&m_cNumberOfLBABlocks[3], 1, 1, Program.m_fpPIAIDE[i]);
                        Program.m_fpPIAIDE[i].Read(m_cNumberOfLBABlocks, 2, 1);                     // fread (&m_cNumberOfLBABlocks[2], 1, 1, Program.m_fpPIAIDE[i]);
                        Program.m_fpPIAIDE[i].Read(m_cNumberOfLBABlocks, 1, 1);                     // fread (&m_cNumberOfLBABlocks[1], 1, 1, Program.m_fpPIAIDE[i]);
                        Program.m_fpPIAIDE[i].Read(m_cNumberOfLBABlocks, 0, 1);                     // fread (&m_cNumberOfLBABlocks[0], 1, 1, Program.m_fpPIAIDE[i]);

                        Program.m_fpPIAIDE[i].Read(m_cNumberOfCylinders, 3, 1);                     // fread (&m_cNumberOfCylinders[3], 1, 1, Program.m_fpPIAIDE[i]);
                        Program.m_fpPIAIDE[i].Read(m_cNumberOfCylinders, 2, 1);                     // fread (&m_cNumberOfCylinders[2], 1, 1, Program.m_fpPIAIDE[i]);
                        Program.m_fpPIAIDE[i].Read(m_cNumberOfCylinders, 1, 1);                     // fread (&m_cNumberOfCylinders[1], 1, 1, Program.m_fpPIAIDE[i]);
                        Program.m_fpPIAIDE[i].Read(m_cNumberOfCylinders, 0, 1);                     // fread (&m_cNumberOfCylinders[0], 1, 1, Program.m_fpPIAIDE[i]);

                        Program.m_fpPIAIDE[i].Read(m_cNumberOfHeads, 0, 1);               // fread (&m_cNumberOfHeads,           1, 1, Program.m_fpPIAIDE[i]);
                        Program.m_fpPIAIDE[i].Read(m_cNumberOfSectorsPerTrack, 0, 1);               // fread (&m_cNumberOfSectorsPerTrack, 1, 1, Program.m_fpPIAIDE[i]);

                        Program.m_fpPIAIDE[i].Read(m_cNumberOfBytesPerSector, 1, 1);                // fread (&m_cNumberOfBytesPerSector[1],  1, 1, Program.m_fpPIAIDE[i]);
                        Program.m_fpPIAIDE[i].Read(m_cNumberOfBytesPerSector, 0, 1);                // fread (&m_cNumberOfBytesPerSector[0],  1, 1, Program.m_fpPIAIDE[i]);

                        if (nInfoSize >= 14)
                        {
                            byte[] pDriveInfoString = new byte[nInfoSize - 14];                     // byte* pDriveInfoString = new byte[nInfoSize - 14 + 1];
                            if (pDriveInfoString != null)
                            {
                                pDriveInfoString.Initialize();                                          // memset(pDriveInfoString, '\0', nInfoSize - 14 + 1);
                                Program.m_fpPIAIDE[i].Read(pDriveInfoString, 0, (int)(nInfoSize - 14)); // fread(pDriveInfoString, 1, nInfoSize - 14, Program.m_fpPIAIDE[i]);
                                Program.m_strPIAIDEDriveInfo[i] = Encoding.ASCII.GetString(pDriveInfoString);
                            }

                            m_nNumberOfLBABlocks[i] = (ulong)((ulong)m_cNumberOfLBABlocks[3] * 256 * 256 * 256) +        // m_nNumberOfLBABlocks[i]        = *(ulong*)m_cNumberOfLBABlocks;
                                                      (ulong)((ulong)m_cNumberOfLBABlocks[2] * 256 * 256) +
                                                      (ulong)((ulong)m_cNumberOfLBABlocks[1] * 256) +
                                                      (ulong)((ulong)m_cNumberOfLBABlocks[0]);

                            m_nNumberOfCylinders[i] = (uint)((uint)m_cNumberOfCylinders[3] * 256 * 256 * 256) +        // m_nNumberOfCylinders[i]        = *(uint*)m_cNumberOfCylinders;
                                                      (uint)((uint)m_cNumberOfCylinders[2] * 256 * 256) +
                                                      (uint)((uint)m_cNumberOfCylinders[1] * 256) +
                                                      (uint)((uint)m_cNumberOfCylinders[0]);

                            m_nNumberOfHeads[i] = (uint)((uint)m_cNumberOfHeads[0] & 0x000000ff);                   // m_nNumberOfHeads[i]            = *(uint*)m_cNumberOfHeads           & 0x000000ff;
                            m_nNumberOfSectorsPerTrack[i] = (uint)((uint)m_cNumberOfSectorsPerTrack[0] & 0x000000ff);   // m_nNumberOfSectorsPerTrack[i]  = *(uint*)m_cNumberOfSectorsPerTrack & 0x000000ff;
                            m_nNumberOfBytesPerSector[i] = (uint)((uint)m_cNumberOfBytesPerSector[1] * 256) +          // m_nNumberOfBytesPerSector[i]   = *(uint*)m_cNumberOfBytesPerSector  & 0x0000ffff;
                                                            (uint)((uint)m_cNumberOfBytesPerSector[0]);


                            Program.m_nPIAIDEDiskFormat[i] = Program.m_nPIAIDEDiskFormat[i];                            // DiskFormats.DISK_FORMAT_FLEX;

                            m_IDE_STATRegister[i] = STSRDY;

                            Program.m_fpPIAIDE[i].Seek(0, SeekOrigin.Begin);                                            //fseek(Program.m_fpPIAIDE[i], 0, SEEK_SET);
                        }
                        else
                        {
                            Console.WriteLine("PIAIDE drive is not a valid PIAIDE image");
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Unable to open PIAIDE drive");
                        Console.WriteLine(e.Message);
                    }
                }
                else
                    Program.m_strPIAIDEFilename[i] = "";
            }

            m_PIADataPortA = sBaseAddress;
            m_PIACtlPortA  = (ushort)(sBaseAddress + 1);
            m_PIADataPortB = (ushort)(sBaseAddress + 2);
            m_PIACtlPortB  = (ushort)(sBaseAddress + 3);
        }
    }
}
