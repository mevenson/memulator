using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;

using System.IO;
using System.Threading;

namespace Memulator
{
    class TTLIDE : IODevice
    {
        bool _spin = false;

        #region defines

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

        #endregion

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

        #region variables

        int m_nRow;

        ushort m_TTLAddressSelectRegister;
        ushort m_TTLTaskRegister;
        ushort m_TTLAlternatRegister;

        byte m_TTLAddressSelectRegisterContents;
        byte [] m_TTLTaskRegisterContents = new byte[8];
        byte m_TTLAlternatRegisterContents;

        bool m_bReading;
        bool m_bWriting;
        bool m_bDrive0Selected;
        bool m_bDrive1Selected;
        byte m_cHiByteLatch;

        int m_nIDEAddressSelected;

        byte [] m_caReadBufferEvenBytes = new byte[65536];
        byte [] m_caReadBufferOddBytes  = new byte[65536];
        byte [] m_caWriteBuffer         = new byte[65536];

        uint m_nHead, m_nCylinder, m_nSector, m_nSectorCount, m_nErrorRegister;
        bool m_bLABMode;

        uint m_nStatusReads;

        int m_nIDEReadPtr;
        int m_nIDEWritePtr;

        bool m_nIDEReading;
        bool m_nIDEWriting;

        byte [] m_IDE_DATARegister = new byte[2];
        byte [] m_IDE_STATRegister = new byte[2];

        ulong [] m_TTLIDE_EndPosition = new ulong[2];

        ulong m_lFileOffset;

        byte  [] m_cNumberOfLBABlocks       = new byte [4];
        byte  [] m_cNumberOfCylinders       = new byte [4];
        byte  [] m_cNumberOfHeads           = new byte [1];
        byte  [] m_cNumberOfSectorsPerTrack = new byte [1];
        byte  [] m_cNumberOfBytesPerSector  = new byte [2];

        ulong [] m_nNumberOfLBABlocks       = new ulong[2];
        uint  [] m_nNumberOfCylinders       = new uint [2];
        uint  [] m_nNumberOfHeads           = new uint [2];
        uint  [] m_nNumberOfSectorsPerTrack = new uint [2];
        uint  [] m_nNumberOfBytesPerSector  = new uint [2];
        #endregion

        public TTLIDE ()
        {
            m_bReading          = false;
            m_bWriting          = false;
            m_bDrive0Selected   = true;
            m_bDrive1Selected   = false;

            m_nIDEAddressSelected = 0;
            m_nHead = m_nCylinder = m_nSector = m_nSectorCount = m_nErrorRegister = 0;
            m_bLABMode = false;

            m_nIDEReading = false;
            m_nIDEWriting = false;

            for (int i = 0; i < 2; i++)
            {
                Program.m_fpTTLIDE[i] = null;

                m_nNumberOfLBABlocks[i]        = 0;
                m_nNumberOfCylinders[i]        = 0;
                m_nNumberOfHeads[i]            = 0;
                m_nNumberOfSectorsPerTrack[i]  = 0;
                m_nNumberOfBytesPerSector[i]   = 0;

                m_IDE_STATRegister[i] = 0x00;
                m_TTLAlternatRegisterContents = 0x00;
            }
        }

        public override void SetInterrupt(bool _spin)
        {
            base.SetInterrupt(_spin);
        }

        public override void ClearInterrupt()
        {
            base.ClearInterrupt();
        }

        bool CalcFileOffset (int nDrive)
        {
            bool bValidSector = true;

            uint nPhysicalSector = m_nSector - 1;

            if (m_bLABMode)
            {
                ulong lBlockNumber = (m_nHead * 65536 * 256) + (m_nCylinder * 256) + m_nSector;

                if ((ulong)lBlockNumber < (ulong) m_nNumberOfLBABlocks[nDrive])
                    m_lFileOffset = lBlockNumber * 256;
                else
                {
                    m_lFileOffset = 0;
                    bValidSector = false;
                }

            }
            else
            {
                if ((Program.m_nTTLIDEDiskFormat[nDrive] == DiskFormats.DISK_FORMAT_FLEX) || (Program.m_nTTLIDEDiskFormat[nDrive] == DiskFormats.DISK_FORMAT_UNIFLEX))
                {
                    if (nPhysicalSector < m_nNumberOfSectorsPerTrack[nDrive] && (uint) m_nHead < m_nNumberOfHeads[nDrive] && (uint) m_nCylinder < m_nNumberOfCylinders[nDrive])
                    {
                        m_lFileOffset = (ulong) 
                        (
                            (
                                (m_nCylinder * m_nNumberOfSectorsPerTrack[nDrive] * m_nNumberOfHeads[nDrive]) + // calc the block offset to the correct cylinder
                                (m_nHead * m_nNumberOfSectorsPerTrack[nDrive]) +                                // add in the block offset to the start of this track
                                nPhysicalSector                                                                 // add in the block offset to the correct sector
                            ) * 
                            m_nNumberOfBytesPerSector[nDrive]                                                   // multiply * the number of bytes per sector
                        );
                    }
                    else
                    {
                        bValidSector = false;
                    }
                }
            }

            if (m_TTLIDE_EndPosition[nDrive] < m_lFileOffset)
                bValidSector = false;

            return (bValidSector);
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
                        {
                            m_nIDEReading = true;
                            m_nIDEWriting = false;
                            m_nIDEReadPtr = 0;

                            m_nStatusReads = 0;

                            bool bValidSector = CalcFileOffset (nDrive);

                            if (bValidSector)
                            {
                                if (Program.m_fpTTLIDE[nDrive] != null)
                                {
                                    Program.m_fpTTLIDE[nDrive].Seek((long)m_lFileOffset, SeekOrigin.Begin);                                                 //fseek(Program.m_fpTTLIDE[nDrive], m_lFileOffset, SEEK_SET);
                                    Program.m_fpTTLIDE[nDrive].Read(m_caReadBufferOddBytes, 0, (int)(m_nNumberOfBytesPerSector[nDrive] * m_nSectorCount));  //fread(m_caReadBufferOddBytes, m_nNumberOfBytesPerSector[nDrive] * m_nSectorCount, 1, Program.m_fpTTLIDE[nDrive]);
                                    m_caReadBufferEvenBytes.Initialize();                                                                                   // memset (m_caReadBufferEvenBytes, '\0', m_nNumberOfBytesPerSector[nDrive] * m_nSectorCount);

                                    m_IDE_DATARegister[nDrive] = m_caReadBufferOddBytes[0];
                                    m_IDE_STATRegister[nDrive] = (byte)(STSBSY | STSRDY | STSSKC | STSDRQ | STSIDX);
                                    m_TTLAlternatRegisterContents = 0x00;
                                    m_nErrorRegister = 0;
                                }
                            }
                            else
                            {
                                m_IDE_STATRegister[nDrive] = (byte)(STSRDY | STSERR);
                                m_nErrorRegister = ERRIDNF;
                            }
                        }
                        break;

                    case (int)CommandOpcodes.CMDWRITE:    //    0x30     WRITE BLOCK WITH RETRY.
                        {
                            m_nIDEReading = false;
                            m_nIDEWriting = true;
                            m_nIDEWritePtr = 0;

                            m_nStatusReads = 0;

                            m_IDE_STATRegister[nDrive] |= (byte)(STSRDY | STSDRQ);        // (STSBSY | STSRDY | STSSKC | STSDRQ);
                        }
                        break;

                    case (int)CommandOpcodes.CMDRDVER:    //    0x40     VERIFY DATA WITH RETRY.
                        m_IDE_STATRegister[nDrive] = (byte)(STSRDY | STSSKC);
                        break;

                    case (int)CommandOpcodes.CMDSEEK:     //    0x70     SEEK SECTOR
                        {
                            bool bValidSector = CalcFileOffset (nDrive);
                            if (bValidSector)
                            {
                                m_IDE_STATRegister[nDrive] = (byte)(STSRDY | STSSKC);
                                m_nErrorRegister = 0;
                            }
                            else
                            {
                                m_IDE_STATRegister[nDrive] = (byte)(STSRDY | STSERR);
                                m_nErrorRegister = ERRIDNF;
                            }
                        }
                        break;

                    case (int)CommandOpcodes.CMDSTOP:     //    0xE0     STOP DISK
                        m_IDE_STATRegister[nDrive] = (byte)(STSRDY | STSSKC);
                        break;

                    case (int)CommandOpcodes.CMDSTRT:     //    0xE1     START DISK
                        m_IDE_STATRegister[nDrive] = (byte)(STSRDY | STSSKC);
                        break;

                    case (int)CommandOpcodes.CMDIDENT:    //    0xEC     IDENTIFY DISK
                        {
                            // read the tail end of the image file and return the drive information in the sector buffer

                            //0140 107A B61693               lda      HDBUFR+3  ; Get number of heads
                            //0141 107D F61696               ldb      HDBUFR+6  ; Get number of sectors
                            //0142 1080 FD1012               std      SYHEDS    ; Save these for comparison
                            //0143 1083 B61691               lda      HDBUFR+1  ; Get low byte of cylinders
                            //0144 1086 B71011               sta      SYCYLB    ; Save it for comparison
                            //0145 1089 FC16CC               ldd      HDBUFR+60 ; Get the lba upper middle and low byte
                            //0146 108C F71014               stb      SYULBA    ; Save the upper middle lba sectors byte
                            //0147 108F B71015               sta      SYLLBA    ; Save the lower lba sectors byte
                            //
                            //    OFFSET    Description                             Example  
                            //    ------    --------------------------------------  -------------------------------
                            //    0         bit field:  bit 6: fixed disk,          0x0040  
                            //                          bit 7: removable medium  
                            //     
                            //    1         Default number of cylinders             16383  
                            //    3         Default number of heads                 16  
                            //    6         Default number of sectors per track     63  
                            //     
                            //    10-19     Serial number (in ASCII)                G8067TME  
                            //    23-26     Firmware revision (in ASCII)            GAK&1B0  
                            //    27-46     Model name (in ASCII)                   Maxtor 4G160J8  
                            //     
                            //    49        bit field: bit 9: LBA supported         0x2f00  
                            //     
                            //    53        bit field: bit 0: words 54-58 are valid 0x0007  
                            //    54        Current number of cylinders             16383  
                            //    55        Current number of heads                 16  
                            //    56        Current number of sectors per track     63  
                            //    57-58     Current LBA capacity                    16514064  
                            //     
                            //    60-61     Default LBA capacity                    268435455  
                            //     
                            //    82-83     Command sets supported                  7c69 4f09  
                            //     
                            //    85-86     Command sets enabled                    7c68 0e01  
                            //     
                            //    100-103   Maximum user LBA for 48-bit addressing  320173056  
                            //     
                            //    255       Checksum and signature (0xa5)           0x44a5  

                            m_caReadBufferEvenBytes.Initialize();           // memset (m_caReadBufferEvenBytes, '\0', 256);
                            m_caReadBufferOddBytes.Initialize();            // memset(m_caReadBufferOddBytes, '\0', 256);

                            m_caReadBufferEvenBytes[ 0] = 0x40;
                            m_caReadBufferEvenBytes[ 1] = (byte)(m_nNumberOfCylinders[nDrive] / 256);    // Hibyte of sectors
                            m_caReadBufferOddBytes[ 1]  = (byte)(m_nNumberOfCylinders[nDrive] % 256);     // low byte of sectors

                            m_caReadBufferEvenBytes[ 3] = (byte)(m_nNumberOfHeads[nDrive] / 256);
                            m_caReadBufferOddBytes[ 3]  = (byte)(m_nNumberOfHeads[nDrive] % 256);

                            m_caReadBufferEvenBytes[ 6] = (byte)(m_nNumberOfSectorsPerTrack[nDrive] / 256);
                            m_caReadBufferOddBytes[ 6]  = (byte)(m_nNumberOfSectorsPerTrack[nDrive] % 256);
                    
                            m_caReadBufferEvenBytes[49] = 0x00;
                            m_caReadBufferOddBytes[49]  = 0x2F;

                            m_caReadBufferEvenBytes[60] = (byte)(m_nNumberOfLBABlocks[nDrive]  / 256);
                            m_caReadBufferOddBytes[60]  = (byte)(m_nNumberOfLBABlocks[nDrive]  % 256);
                            m_caReadBufferEvenBytes[61] = (byte)(m_nNumberOfLBABlocks[nDrive]  >> 24);
                            m_caReadBufferOddBytes[61]  = (byte)(m_nNumberOfLBABlocks[nDrive]  >> 16);
                            m_caReadBufferEvenBytes[62] = (byte)((m_nNumberOfLBABlocks[nDrive] >> 24) >>  8);
                            m_caReadBufferOddBytes[62]  = (byte)((m_nNumberOfLBABlocks[nDrive] >> 24) >> 16);

                            m_IDE_STATRegister[nDrive] = (byte)(STSBSY | STSRDY | STSSKC | STSDRQ | STSIDX);
                            m_TTLAlternatRegisterContents = 0x00;

                            m_nIDEReadPtr = 0;
                            m_nIDEReading = true;
                        }
                        break;

                    case (int)CommandOpcodes.CMDSETPAR:   //    0x91     SET CYL/TRACK AND SECT/TRACK
                        m_IDE_STATRegister[nDrive] = (byte)(STSRDY | STSSKC);
                        break;
                }
            }
        }

        public override void Write(ushort m, byte b)
        {
            // The B side of the TTL is used to address the IDE controller.

            if (m == m_TTLAddressSelectRegister)
                m_nIDEAddressSelected = b;
            else if (m == m_TTLTaskRegister)
            {
                switch (m_nIDEAddressSelected)
                {
                    case 7:      //  DCA2|DCA1|DCA0|DCCS0|DCIOX    * ADD IN FOR FOR COMMAND         7
                        ExecuteIDECommand (b);
                        break;

                    case 6:       //  DCA2|DCA1|DCCS0|DCIOX         * ADD IN FOR HEAD               6

                        if ((b & 0x40) == 0x40)
                            m_bLABMode = true;
                        else
                            m_bLABMode = false;

                        m_nHead = (byte)(b & 0x0F);

                        break;

                    case 5:      //  DCA2|DCA0|DCCS0|DCIOX         * ADD IN FOR CYLINDER HIGH       5
                        m_nCylinder = (byte)((m_nCylinder & 0x00ff) + (b * 256));
                        break;

                    case 4:      //  DCA2|DCCS0|DCIOX              * ADD IN FOR CYLINDER LOW        4
                        m_nCylinder = (m_nCylinder & 0xff00) + b;
                        break;

                    case 3:      //  DCA1|DCA0|DCCS0|DCIOX         * ADD IN FOR SECTOR              3
                        m_nSector = b;
                        break;

                    case 2:      //  DCA1|DCCS0|DCIOX              * ADD IN FOR NUMBER OF SECTORS   2
                        m_nSectorCount = b;
                        break;

                    case 1:      //  DCA0|DCCS0|DCIOX              * ADD IN FOR ERROR REGISTER      1
                        m_nErrorRegister = b;
                        break;

                    case 0:     //  DCCS0|DCIOX                   * ADD IN FOR FOR DATA BUS         0
                        if (m_nIDEWriting)
                        {
                            int nDrive = -1;

                            if (m_bDrive0Selected)
                                nDrive = 0;
                            else if (m_bDrive1Selected)
                                nDrive = 1;

                            if (nDrive != -1)
                            {
                                m_IDE_STATRegister[nDrive] &= (byte)(~STSERR);

                                if (m_nIDEWriting)
                                {
                                    m_caWriteBuffer[m_nIDEWritePtr++] = b;

                                    if (m_nIDEWritePtr == (m_nNumberOfBytesPerSector[nDrive] * m_nSectorCount))
                                    {
                                        bool bValidSector = CalcFileOffset (nDrive);

                                        if (bValidSector)
                                        {
                                            if (Program.m_fpTTLIDE[nDrive] != null)
                                            {
                                                Program.m_fpTTLIDE[nDrive].Seek((long)m_lFileOffset, SeekOrigin.Begin);                                             //fseek(Program.m_fpTTLIDE[nDrive], m_lFileOffset, SEEK_SET);
                                                Program.m_fpTTLIDE[nDrive].Write(m_caWriteBuffer, 0, (int)(m_nNumberOfBytesPerSector[nDrive] * m_nSectorCount));    //fwrite(m_caWriteBuffer, 1, m_nNumberOfBytesPerSector[nDrive] * m_nSectorCount, Program.m_fpTTLIDE[nDrive]);
                                                Program.m_fpTTLIDE[nDrive].Flush();                                                                                 //fflush(Program.m_fpTTLIDE[nDrive]);
                                            }

                                            m_IDE_STATRegister[nDrive] &= (byte)(~(STSDRQ | STSBSY | STSERR));
                                            m_nErrorRegister = 0;
                                            m_nIDEWriting = false;
                                            ClearInterrupt ();
                                        }
                                        else
                                        {
                                            m_IDE_STATRegister[nDrive] = (byte)(STSRDY | STSERR);
                                            m_nErrorRegister = ERRIDNF;
                                        }
                                    } 
                                    else
                                        m_IDE_STATRegister[nDrive] |= STSDRQ;
                                }
                            }
                        }
                        break;
                }
            }
            else if (m == m_TTLAlternatRegister)
            {
                m_TTLAlternatRegisterContents = b;
            }
        }

        public override byte Read(ushort m)
        {
            // A 1 in the m_nPortXDirectionMask means the bit position is an output 
            // bit so only return the bits that are 0 in the m_nPortXDirectionMask.

            byte c = 0xff;

            if (m == m_TTLAddressSelectRegister)
            {
                //c = m_nIDEAddressSelected;
                c = m_cHiByteLatch ;
            }
            else if (m == m_TTLTaskRegister)
            {
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
                                    if (++m_nStatusReads > (m_nNumberOfBytesPerSector[nDrive] * m_nSectorCount * 2))
                                        m_IDE_STATRegister[nDrive] = STSRDY;
                                }
                                c = m_IDE_STATRegister[nDrive];
                            }
                            else
                                c = STSRDY;
                        }
                        break;

                    case 6:                 //  DCA2|DCA1|DCCS0|DCIOX         * ADD IN FOR HEAD 6
                        c = (byte)m_nHead;
                        break;

                    case 5:        //  DCA2|DCA0|DCCS0|DCIOX         * ADD IN FOR CYLINDER HIGH 5
                        c = (byte)(m_nCylinder / 256);
                        break;

                    case 4:        //  DCA2|DCCS0|DCIOX              * ADD IN FOR CYLINDER LOW 4
                        c = (byte)(m_nCylinder & 0x00ff);
                        break;

                    case 3:        //  DCA1|DCA0|DCCS0|DCIOX         * ADD IN FOR SECTOR 3
                        c = (byte)m_nSector;
                        break;

                    case 2:        //  DCA1|DCCS0|DCIOX              * ADD IN FOR NUMBER OF SECTORS 2
                        c = (byte)m_nSectorCount;
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
                                if (m_nIDEReading)
                                {
                                    m_cHiByteLatch = m_caReadBufferEvenBytes[m_nIDEReadPtr];
                                    m_IDE_DATARegister[nDrive] = m_caReadBufferOddBytes[m_nIDEReadPtr];

                                    lock (Program._cpu.buildingDebugLineLock)
                                    {
                                        m_nIDEReadPtr++;
                                    }
                                    if (m_nIDEReadPtr == (m_nNumberOfBytesPerSector[nDrive] * m_nSectorCount))
                                    {
                                        lock (Program._cpu.buildingDebugLineLock)
                                        {
                                        m_IDE_STATRegister[nDrive] &= (byte)(~(STSDRQ | STSBSY | STSERR));
                                        m_nIDEReading = false;
                                        ClearInterrupt();
                                        }
                                    }
                                    else
                                    {
                                        lock (Program._cpu.buildingDebugLineLock)
                                        {
                                        m_IDE_STATRegister[nDrive] |= STSDRQ;
                                        }
                                    }
                                }
                                c = m_IDE_DATARegister[nDrive];
                                m_nStatusReads = 0;
                            }
                        }
                        break;
                }
            }
            else if (m == m_TTLAlternatRegister)
            {
                c = m_TTLAlternatRegisterContents;
            }

            return (c);
        }

        public override byte Peek(ushort m)
        {
            byte c = 0xff;
            if (m == m_TTLAddressSelectRegister)
            {
                c = m_cHiByteLatch ;
            }
            else if (m == m_TTLTaskRegister)
            {
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
                                byte temp = m_IDE_STATRegister[nDrive];
                                if (!m_nIDEReading && !m_nIDEWriting)           // turn off BUSY if not read/writing
                                    temp  = (byte)(m_IDE_STATRegister[nDrive] & (byte)(~STSBSY));
                                else
                                {
                                    if (++m_nStatusReads > (m_nNumberOfBytesPerSector[nDrive] * m_nSectorCount * 2))
                                        temp = STSRDY;
                                }
                                c = temp;
                            }
                            else
                                c = STSRDY;
                        }
                        break;
                    case 6:                 //  DCA2|DCA1|DCCS0|DCIOX         * ADD IN FOR HEAD 6
                        c = (byte)m_nHead;
                        break;
                    case 5:        //  DCA2|DCA0|DCCS0|DCIOX         * ADD IN FOR CYLINDER HIGH 5
                        c = (byte)(m_nCylinder / 256);
                        break;
                    case 4:        //  DCA2|DCCS0|DCIOX              * ADD IN FOR CYLINDER LOW 4
                        c = (byte)(m_nCylinder & 0x00ff);
                        break;
                    case 3:        //  DCA1|DCA0|DCCS0|DCIOX         * ADD IN FOR SECTOR 3
                        c = (byte)m_nSector;
                        break;
                    case 2:        //  DCA1|DCCS0|DCIOX              * ADD IN FOR NUMBER OF SECTORS 2
                        c = (byte)m_nSectorCount;
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
                                byte temp = m_IDE_DATARegister[nDrive];
                                if (m_nIDEReading)
                                {
                                    m_cHiByteLatch = m_caReadBufferEvenBytes[m_nIDEReadPtr];
                                    temp = m_caReadBufferOddBytes[m_nIDEReadPtr];
                                    if (m_nIDEReadPtr != (m_nNumberOfBytesPerSector[nDrive] * m_nSectorCount))
                                    {
                                        temp = (byte)(m_IDE_STATRegister[nDrive] | STSDRQ);
                                    }
                                }
                                c = temp;
                            }
                        }
                        break;
                }
            }
            else if (m == m_TTLAlternatRegister)
            {
                c = m_TTLAlternatRegisterContents;
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
                string imageFormat             = Program.GetConfigurationAttribute(Program.ConfigSection + "/TTLIDEDisks/Disk", "Format", i.ToString(), "");
                Program.m_strTTLIDEFilename[i] = Program.dataDir + Program.GetConfigurationAttribute(Program.ConfigSection + "/TTLIDEDisks/Disk", "Path", i.ToString(), "");

                if (Program.m_strTTLIDEFilename[i].Length > Program.dataDir.Length)
                {
                    try
                    {
                        Program.m_fpTTLIDE[i] = File.Open(Program.m_strTTLIDEFilename[i], FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);    //int err = fopen_s (&Program.m_fpTTLIDE[i], Program.m_strTTLIDEFilename[i], "r+b");

                        byte[] cInfoSize = new byte[2];
                        uint nInfoSize = 0;

                        Program.m_fpTTLIDE[i].Seek(-2, SeekOrigin.End);                             // fseek(Program.m_fpTTLIDE[i], -2, SEEK_END);
                        long lPosition = Program.m_fpTTLIDE[i].Position;                            // long lPosition = ftell (Program.m_fpTTLIDE[i]);
                        m_TTLIDE_EndPosition[i] = (ulong)lPosition & 0xFFFFFF00;                    // m_TTLIDE_EndPosition[i] = ftell (Program.m_fpTTLIDE[i]) & 0xFFFFFF00;                

                        Program.m_fpTTLIDE[i].Read(cInfoSize, 1, 1);                                // fread (&cInfoSize[1], 1, 1, Program.m_fpTTLIDE[i]);
                        Program.m_fpTTLIDE[i].Read(cInfoSize, 0, 1);                                // fread (&cInfoSize[0], 1, 1, Program.m_fpTTLIDE[i]);

                        nInfoSize = (uint)(((cInfoSize[1] * 256) + cInfoSize[0]) & 0x00ffff);       // nInfoSize = *(uint*)cInfoSize & 0x00ffff;

                        Program.m_fpTTLIDE[i].Seek((int)(0 - nInfoSize), SeekOrigin.End);                  // fseek (Program.m_fpTTLIDE[i], 0 - nInfoSize, SEEK_END);
                        lPosition = Program.m_fpTTLIDE[i].Position;

                        Program.m_fpTTLIDE[i].Read(m_cNumberOfLBABlocks, 3, 1);                     // fread (&m_cNumberOfLBABlocks[3], 1, 1, Program.m_fpTTLIDE[i]);
                        Program.m_fpTTLIDE[i].Read(m_cNumberOfLBABlocks, 2, 1);                     // fread (&m_cNumberOfLBABlocks[2], 1, 1, Program.m_fpTTLIDE[i]);
                        Program.m_fpTTLIDE[i].Read(m_cNumberOfLBABlocks, 1, 1);                     // fread (&m_cNumberOfLBABlocks[1], 1, 1, Program.m_fpTTLIDE[i]);
                        Program.m_fpTTLIDE[i].Read(m_cNumberOfLBABlocks, 0, 1);                     // fread (&m_cNumberOfLBABlocks[0], 1, 1, Program.m_fpTTLIDE[i]);

                        Program.m_fpTTLIDE[i].Read(m_cNumberOfCylinders, 3, 1);                     // fread (&m_cNumberOfCylinders[3], 1, 1, Program.m_fpTTLIDE[i]);
                        Program.m_fpTTLIDE[i].Read(m_cNumberOfCylinders, 2, 1);                     // fread (&m_cNumberOfCylinders[2], 1, 1, Program.m_fpTTLIDE[i]);
                        Program.m_fpTTLIDE[i].Read(m_cNumberOfCylinders, 1, 1);                     // fread (&m_cNumberOfCylinders[1], 1, 1, Program.m_fpTTLIDE[i]);
                        Program.m_fpTTLIDE[i].Read(m_cNumberOfCylinders, 0, 1);                     // fread (&m_cNumberOfCylinders[0], 1, 1, Program.m_fpTTLIDE[i]);

                        Program.m_fpTTLIDE[i].Read(m_cNumberOfHeads, 0, 1);               // fread (&m_cNumberOfHeads,           1, 1, Program.m_fpTTLIDE[i]);
                        Program.m_fpTTLIDE[i].Read(m_cNumberOfSectorsPerTrack, 0, 1);               // fread (&m_cNumberOfSectorsPerTrack, 1, 1, Program.m_fpTTLIDE[i]);

                        Program.m_fpTTLIDE[i].Read(m_cNumberOfBytesPerSector, 1, 1);                // fread (&m_cNumberOfBytesPerSector[1],  1, 1, Program.m_fpTTLIDE[i]);
                        Program.m_fpTTLIDE[i].Read(m_cNumberOfBytesPerSector, 0, 1);                // fread (&m_cNumberOfBytesPerSector[0],  1, 1, Program.m_fpTTLIDE[i]);

                        if (nInfoSize > 14)
                        {
                            byte[] pDriveInfoString = new byte[nInfoSize - 14];                     // byte* pDriveInfoString = new byte[nInfoSize - 14 + 1];
                            if (pDriveInfoString != null)
                            {
                                pDriveInfoString.Initialize();                                          // memset(pDriveInfoString, '\0', nInfoSize - 14 + 1);
                                Program.m_fpTTLIDE[i].Read(pDriveInfoString, 0, (int)(nInfoSize - 14)); // fread(pDriveInfoString, 1, nInfoSize - 14, Program.m_fpTTLIDE[i]);
                                Program.m_strTTLIDEDriveInfo[i] = Encoding.ASCII.GetString(pDriveInfoString);
                            }
                            //delete [] pDriveInfoString;

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


                            Program.m_nTTLIDEDiskFormat[i] = Program.m_nTTLIDEDiskFormat[i];       // DiskFormats.DISK_FORMAT_FLEX;

                            m_IDE_STATRegister[i] = STSRDY;

                            Program.m_fpTTLIDE[i].Seek(0, SeekOrigin.Begin);                                                    //fseek(Program.m_fpTTLIDE[i], 0, SEEK_SET);
                        }
                        else
                        {
                            Console.WriteLine("TTLIDE drive is not a valid TTLIDE image");
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Unable to open TTLIDE drive");
                        Console.WriteLine(e.Message);
                    }
                }
                else
                    Program.m_strTTLIDEFilename[i] = "";
            }

            m_TTLAddressSelectRegister = sBaseAddress;
            m_TTLTaskRegister          = (ushort)(sBaseAddress + 1);
            m_TTLAlternatRegister      = (ushort)(sBaseAddress + 3);
        }
    }
}
