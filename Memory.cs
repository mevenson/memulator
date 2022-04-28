using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;

namespace Memulator
{
    class Memory
    {
        public ushort ADDRESS_MASK = 0x0FFF;
        public ulong DAT_MASK = 0x000FF000;
        public ushort m_DAT_BASE;

        private byte[] _memorySpace;
        private bool[] _isInstalled;
        private int     _startsAt = 0;

        private byte[] _deviceMap = new byte[65536];

        public class MemoryBoard
        {
            public ushort baseAddress;
            public byte page;
            public uint size;
        }

        public byte[] DeviceMap
        {
            get { return _deviceMap; }
            set { _deviceMap = value; }
        }

        public Memory(List<MemoryBoard> memoryBoards)
        {
            _memorySpace = new byte[65536 * 16];
            _isInstalled = new bool[65536 * 16];

            for (int i = 0; i < 65536 * 16; i++)
            {
                _memorySpace[i] = 0xff;
                _isInstalled[i] = false;
            }

            uint minAddress = 0;
            uint maxAddress = 0;

            foreach (MemoryBoard mb in memoryBoards)
            {
                uint address = mb.baseAddress;

                if (mb.page > 0)
                    address = address + (uint)(mb.page * 65536);

                if (address < minAddress)
                    minAddress = address;

                if (address + mb.size > maxAddress)
                    maxAddress = address + mb.size;

                for (int i = 0; i < mb.size; i++)
                    _isInstalled[address + i] = true;
            }
            _startsAt = (int)minAddress;


            // for now
            //_memorySpace = new byte[65536];
            //_startsAt = 0;
            //

            Program._cpu.Switch1B = Program._cpu.Switch1C = Program._cpu.Switch1D = false;
            Program._cpu.RamE000 = Program._cpu.RamE800 = Program._cpu.RamF000 = false;
            Program._cpu.RomE000 = Program._cpu.RomE800 = Program._cpu.RomF000 = false;
        }

        public Memory(int size, int startsAt)
        {
            _memorySpace = new byte[size];
            _isInstalled = new bool[size];

            _memorySpace = new byte[size];
            _startsAt = startsAt;

            for (int i = 0; i < size; i++)
            {
                _memorySpace[i] = 0xff;
                _isInstalled[i] = true;
            }

            Program._cpu.Switch1B = Program._cpu.Switch1C = Program._cpu.Switch1D = false;
            Program._cpu.RamE000  = Program._cpu.RamE800  = Program._cpu.RamF000   = false;
            Program._cpu.RomE000  = Program._cpu.RomE800  = Program._cpu.RomF000   = false;
        }
        public ulong[] m_MemoryDAT = new ulong[16];

        public byte[] MemorySpace
        {
            get { return _memorySpace; }
            set { _memorySpace = value; }
        }

        public int StartsAt
        {
            get { return _startsAt; }
            set { _startsAt = value; }
        }

        public bool IsRAM(ushort m)
        {
            return (_deviceMap[m] == (int)Devices.DEVICE_RAM);
        }

        public bool IsCONS(ushort m)
        {
            return (_deviceMap[m] == (int)Devices.DEVICE_CONS);
        }

        public bool IsROM(ushort m)
        {
            return (_deviceMap[m] == (int)Devices.DEVICE_ROM);
        }

        public void StoreMemoryByte(byte b, ushort m)
        {
            switch (_deviceMap[m])
            {
                case (int)Devices.DEVICE_RAM:
                    if (_isInstalled[m])
                        MemorySpace[m] = b;
                    break;

                case (int)Devices.DEVICE_ROM:
                case (int)Devices.DEVICE_DAT:
                    if (m >= m_DAT_BASE)
                    {
                        int nIndex = m & 0x000F;

                        // Don't do this anymore - we now take care of this the LogicalToPhysical address translation code

                        //if (nIndex > 0x000D)      // ROM in 1st 64K ONLY
                        //    b &= 0x0F;

                        m_MemoryDAT[nIndex] = (ulong)((b ^ 0x0F) << 12); // we need to flip the lower 4 bits and move them over 12 bits
                    }
                    else
                    {
                        string message = string.Format("Attempted write to ROM from address: {0}, to address {1}, existing data: {2} new data: {3} - data not written\n"
                            , Program._cpu.CurrentIP.ToString("X4")
                            , m.ToString("X4")
                            , MemorySpace[m].ToString("X2")
                            , b.ToString("X2"));
                        System.Diagnostics.Debug.WriteLine(message);
                        Program._cpu.ForceSingleStep();
                    }
                    break;

                case (int)Devices.DEVICE_CONS:
                    if (Program._CConsole != null)
                        Program._CConsole.Write(m, b);
                    break;

                case (int)Devices.DEVICE_MPS:
                    if (Program._CACIA != null)
                        Program._CACIA.Write(m, b);
                    break;

                case (int)Devices.DEVICE_PCSTREAM:
                    if (Program._CPCStream != null)
                        Program._CPCStream.Write(m, b);
                    break;

                case (int)Devices.DEVICE_FDC:
                    if (Program._CFD_2 != null)
                        Program._CFD_2.Write(m, b);
                    break;

                case (int)Devices.DEVICE_DMAF2:
                    if (Program._CDMAF2 != null)
                        Program._CDMAF2.Write(m, b);
                    break;

                case (int)Devices.DEVICE_DMAF3:
                    if (Program._CDMAF3 != null)
                        Program._CDMAF3.Write(m, b);
                    break;

                case (int)Devices.DEVICE_MPT:
                    if (Program._CMPT != null)
                        Program._CMPT.Write(m, b);
                    break;

                case (int)Devices.DEVICE_MPID:
                    if (Program._CMPID != null)
                        Program._CMPID.Write(m, b);
                    break;

                case (int)Devices.DEVICE_DMAF1:
                    break;

                case (int)Devices.DEVICE_PRT:
                    if (Program._CPrinter != null)
                        Program._CPrinter.Write(m, b);
                    break;

                case (int)Devices.DEVICE_MPL:
                    break;

                case (int)Devices.DEVICE_PIAIDE:
                    if (Program._CPIAIDE != null)
                        Program._CPIAIDE.Write(m, b);
                    break;

                case (int)Devices.DEVICE_TTLIDE:
                    if (Program._CTTLIDE != null)
                        Program._CTTLIDE.Write(m, b);
                    break;

                default:
                    //if ((m_DeviceMap[m] & DEVICE_AHO) == DEVICE_AHO)
                    //{
                    //    // if the Device Type maps to an AHO type interface we need
                    //    // to extract the offset into the AHO device table to get
                    //    // the GUID for the device. We can have up to 128 different
                    //    // AHO devices assigned. 

                    //    int nAHODeviceType = m_DeviceMap[m] & 0x7f;
                    //    AHOWrite(nAHODeviceType, m, b);
                    //}
                    break;
            }
        }

        public void StoreMemoryWord(ushort w, ushort m)
        {
            StoreMemoryByte((byte)(w / 256), m);
            StoreMemoryByte((byte)(w % 256), (ushort)(m + 1));
        }
        public byte PeekMemoryByte (ushort m)
        {
            byte d = 0xff;
            switch (_deviceMap[m])
            {
                case (int)Devices.DEVICE_RAM:
                    if (_isInstalled[m])
                        d = MemorySpace[m];
                    break;
                case (int)Devices.DEVICE_ROM:
                case (int)Devices.DEVICE_DAT:            // need to read ROM if device is DAT
                    d = MemorySpace[m];
                    break;
                case (int)Devices.DEVICE_CONS:
                    if (Program._CConsole != null)
                        d = Program._CConsole.Peek(m);
                    break;
                case (int)Devices.DEVICE_MPS:
                    if (Program._CACIA != null)
                        d = Program._CACIA.Peek(m);
                    break;
                case (int)Devices.DEVICE_PCSTREAM:
                    if (Program._CPCStream != null)
                        d = Program._CPCStream.Peek(m);
                    break;
                case (int)Devices.DEVICE_FDC:
                    if (Program._CFD_2 != null)
                        d = Program._CFD_2.Peek(m);
                    else
                        d = 0xff;
                    break;
                case (int)Devices.DEVICE_DMAF2:
                    if (Program._CDMAF2 != null)
                        d = Program._CDMAF2.Peek(m);
                    else
                        d = 0xff;
                    break;
                case (int)Devices.DEVICE_DMAF3:
                    if (Program._CDMAF3 != null)
                        d = Program._CDMAF3.Peek(m);
                    else
                        d = 0xff;
                    break;
                case (int)Devices.DEVICE_MPT:
                    if (Program._CMPT != null)
                        d = Program._CMPT.Peek(m);
                    break;
                case (int)Devices.DEVICE_MPID:
                    if (Program._CMPID != null)
                        d = Program._CMPID.Peek(m);
                    break;
                case (int)Devices.DEVICE_DMAF1:
                    break;
                case (int)Devices.DEVICE_PRT:
                    if (Program._CPrinter != null)
                        d = Program._CPrinter.Peek(m);
                    break;
                case (int)Devices.DEVICE_MPL:
                    break;
                case (int)Devices.DEVICE_PIAIDE:
                    if (Program._CPIAIDE != null)
                        d = Program._CPIAIDE.Peek(m);
                    break;
                case (int)Devices.DEVICE_TTLIDE:
                    if (Program._CTTLIDE != null)
                        d = Program._CTTLIDE.Peek(m);
                    break;
                default:
                    break;
            }
            return (d);
        }

        public byte LoadMemoryByte(ushort m)
        {
            byte d = 0xff;

            switch (_deviceMap[m])
            {
                case (int)Devices.DEVICE_RAM:
                    if (_isInstalled[m])
                        d = MemorySpace[m];
                    break;

                case (int)Devices.DEVICE_ROM:
                case (int)Devices.DEVICE_DAT:            // need to read ROM if device is DAT
                    d = MemorySpace[m];
                    break;

                case (int)Devices.DEVICE_CONS:
                    if (Program._CConsole != null)
                        d = Program._CConsole.Read(m);
                    break;

                case (int)Devices.DEVICE_MPS:
                    if (Program._CACIA != null)
                        d = Program._CACIA.Read(m);
                    break;

                case (int)Devices.DEVICE_PCSTREAM:
                    if (Program._CPCStream != null)
                        d = Program._CPCStream.Read(m);
                    break;

                case (int)Devices.DEVICE_FDC:
                    if (Program._CFD_2 != null)
                        d = Program._CFD_2.Read(m);
                    else
                        d = 0xff;
                    break;

                case (int)Devices.DEVICE_DMAF2:
                    if (Program._CDMAF2 != null)
                        d = Program._CDMAF2.Read(m);
                    else
                        d = 0xff;
                    break;

                case (int)Devices.DEVICE_DMAF3:
                    if (Program._CDMAF3 != null)
                        d = Program._CDMAF3.Read(m);
                    else
                        d = 0xff;
                    break;

                case (int)Devices.DEVICE_MPT:
                    if (Program._CMPT != null)
                        d = Program._CMPT.Read(m);
                    break;

                case (int)Devices.DEVICE_MPID:
                    if (Program._CMPID != null)
                        d = Program._CMPID.Read(m);
                    break;

                case (int)Devices.DEVICE_DMAF1:
                    break;

                case (int)Devices.DEVICE_PRT:
                    if (Program._CPrinter != null)
                        d = Program._CPrinter.Read(m);
                    break;

                case (int)Devices.DEVICE_MPL:
                    break;

                case (int)Devices.DEVICE_PIAIDE:
                    if (Program._CPIAIDE != null)
                        d = Program._CPIAIDE.Read(m);
                    break;

                case (int)Devices.DEVICE_TTLIDE:
                    if (Program._CTTLIDE != null)
                        d = Program._CTTLIDE.Read(m);
                    break;


                default:
                    //if ((m_DeviceMap[m] & (int)Devices.DEVICE_AHO) == (int)Devices.DEVICE_AHO)
                    //{
                    //    // if the Device Type maps to an AHO type interface we need
                    //    // to extract the offset into the AHO device table to get
                    //    // the GUID for the device. We can have up to 128 different
                    //    // AHO devices assigned. 
                    //
                    //    int nAHODeviceType = m_DeviceMap[m] & 0x7f;
                    //    d = AHORead (nAHODeviceType, m);
                    //}
                    break;
            }

            return (d);
        }

        public ushort LoadMemoryWord(ushort sAddr)
        {
            ushort d;

            d = (ushort)(LoadMemoryByte(sAddr) * 256);
            d += (ushort)(LoadMemoryByte((ushort)(sAddr + 1)));

            return (d);
        }

        public ulong LogicalToPhysicalAddress(ushort sLogicalAddress, out bool bValidMemory)
        {
            bValidMemory = true;

            //  To determine which physical 4K page we are in, we get the upper 4 bits from the DAT being addressed by the upper 4 bits of
            //  the address bus. Then we need the 4 bits from the extra memory address lines in the latch. these bits need to wind up in 
            //  the upper 4 bits of the 20 bit address that we will generate. 
            //
            //  Thelower 4 bits from the DAT will specify the translated address lines A15 - A12. In order for the translation to 
            //  work we need to use only the lower 12 bits of the address bus when adding it to the page variable. Since the DAT
            //  can not be changed during the execution of an instruction, we only need to do this once per instruction.

            // Set the default so that if we do not do any translation because the address is in the DAT or the IO or ROM space we will
            // just return the Logical Address as the Physical Address.

            ulong lPhysicalAddress = sLogicalAddress;       // default the physical address to the logical address

            if (Program.m_nProcessorBoard == (int)CPUBoardTypes.MP_09)
            {
                // always access the DAT at the logical address

                if ((sLogicalAddress & 0xFF00) != 0xFF00)   // <- NOT DAT
                {
                    //  Otherwise pass the logical address through the DAT to generate a possible physical address

                    lPhysicalAddress = (ulong)((m_MemoryDAT[sLogicalAddress >> 12] & DAT_MASK) + (ulong)(sLogicalAddress & ADDRESS_MASK));

                    //  See what the address would be if it were to stay in the first 64K address space.
                    //  Only use the lower 4 bits of the DAT to reassign logical address to any 4K page
                    //  in the lower 64K address space - We will call this 'lRemappedAddress'

                    ulong lRemappedAddress = lPhysicalAddress & 0x0000FFFF;

                    if (lRemappedAddress >= 0xE000)
                    {
                        // see if we are accessing valid memory

                        if (!Program._cpu.Switch1B && (lRemappedAddress > 0xDFFF) && (lRemappedAddress < 0xE800) && (_deviceMap[lRemappedAddress] == (int)Devices.DEVICE_RAM)) // MP-09 IC-1
                            bValidMemory = false;

                        if (!Program._cpu.Switch1C && (lRemappedAddress > 0xE7FF) && (lRemappedAddress < 0xF000) && (_deviceMap[lRemappedAddress] == (int)Devices.DEVICE_RAM))   // MP-09 IC-2
                            bValidMemory = false;

                        if (!Program._cpu.Switch1D && (lRemappedAddress > 0xEFFF) && (lRemappedAddress < 0xF800) && (_deviceMap[lRemappedAddress] == (int)Devices.DEVICE_RAM))   // MP-09 IC-3
                            bValidMemory = false;
                    }

                    //  Now see if the remapped address is an address that cannot be moved out of the first 64K. If any
                    //  of these situations are true - stay in the first 64K of memory - that is  - if it's R ROM or RAM
                    //  in the range of 0xE000 through 0xFFFF and the appropriate switch is set to make it valid memory
                    //  or it is an IO address.

                    if (((lRemappedAddress >= 0xE000) && (lRemappedAddress <= 0xFFFF)) || _deviceMap[lRemappedAddress] > 1) // this should improve performance for RAM address access. Not sure it does any good.
                    {
                        if
                        (
                          (
                            ((lRemappedAddress >= 0xE000) && (lRemappedAddress <= 0xE7FF) && Program._cpu.Switch1B) ||      // MP-09 IC-1
                            ((lRemappedAddress >= 0xE800) && (lRemappedAddress <= 0xEFFF) && Program._cpu.Switch1C) ||      // MP-09 IC-2
                            ((lRemappedAddress >= 0xF000) && (lRemappedAddress <= 0xF7FF) && Program._cpu.Switch1D) ||      // MP-09 IC-3
                            ((lRemappedAddress >= 0xF800) && (lRemappedAddress <= 0xFFFF))                                  // MP-09 IC-4 (SBUG) always on
                          ) ||
                          (
                             _deviceMap[lRemappedAddress] > (byte)Devices.DEVICE_ROM                                         // if it not RAM or ROM (must be IO)
                          )
                        )
                        {
                            lPhysicalAddress = lRemappedAddress;
                        }
                    }
                }
            }

            if (!_isInstalled[lPhysicalAddress])
                bValidMemory = false;

            return (lPhysicalAddress);
        }

        //ulong LogicalToPhysicalAddress (ushort sLogicalAddress, bool& bValidMemory)
        //{
        //    long test = TestLogicalToPhysicalAddress (sLogicalAddress, bValidMemory);
        //    return (test);
        //
        //    //  To determine which 64K page we are in, we get the upper 4 bits from the DAT being addressed by the upper 4 bits of
        //    //  the address bus. these bits need to wind up in the upper 4 bits of the 20 bit address that we will generate. The
        //    //  lower 4 bits from the DAT will specify the translated address lines A15 - A12. In order for the translation to 
        //    //  work we need to use only the lower 12 bits of the address bus when adding it to the page variable. Since the DAT
        //    //  can not be changed during the execution of an instruction, we only need to do this once per instruction.
        //
        //    // Set the default so that if we do not do any translation because the address is in the DAT or the IO or ROM space we will
        //    // just return the Logical Address as the Physical Address.
        //
        //    long lPhysicalAddress = sLogicalAddress;      
        //
        //    // See if we are trying to translate an address that should NOT be translated (i.e. the DAT address space)
        //
        //    if (sLogicalAddress >= 0xE000)
        //    {
        //        // The memory between E000 and FEFF can be maped to any 4K block, but cannot be mapped outside the lower 64K
        //        //
        //        //  Since the logical address being accessed is in the ROM or IO space, we need to make sure
        //        //  that it is not in the FF00-FFFF range since that address is always pointing to the DAT on 
        //        //  writes and ROM on reads.
        //        //
        //        //      The DAT cannot be mapped out of the first 64K or to any other 4K block, but the memory 
        //        //      between 0xE000 and 0xFF00 can be mapped to another 4K block.
        //        //
        //        //      m_DeviceMap holds the type of memory or IO at the given address. This is a 64K block of memory that 
        //        //      is used to map the RAM, IO, ROM and DAT to specific addresses ranges when they are initialized The 
        //        //      DAT space is mapped to 'DEVICE_DAT', the DMAF3 to 'DEVICE_DMAF3', the console to 'DEVICE_CONS', etc.
        //
        //        long lRemappedAddress = (((m_MemoryDAT[sLogicalAddress >> 12] & DAT_MASK) + (sLogicalAddress & ADDRESS_MASK))) & 0x0000FFFF;
        //
        //        // are we in the DAT address space - if we are, leave the physical address equal to the logical address, otherwise
        //        // translate it fully unless it's in the IO or ROM space
        //
        //        if (m_DeviceMap[sLogicalAddress] != DEVICE_DAT)
        //        //if ((m_DeviceMap[sLogicalAddress] != DEVICE_DAT) && (m_DeviceMap[lRemappedAddress] != DEVICE_DAT))
        //        {
        //            //  NO - then we are not accessing the DAT memory space here - do translation. Rstrict translation 
        //            //  to the first 64K of memory if the translated address is still in the range range of $E000 - $FEFF
        //
        //            if (lRemappedAddress < 0xE000)
        //                lPhysicalAddress = (m_MemoryDAT[sLogicalAddress >> 12] & DAT_MASK) + (sLogicalAddress & ADDRESS_MASK);
        //            else
        //            {
        //                if (lRemappedAddress > 0xF7FF && (m_MemoryDAT[sLogicalAddress >> 12] & DAT_MASK) > 0x0000F000 && (m_MemoryDAT[sLogicalAddress >> 12] & DAT_MASK) < 0x000F0000 )
        //                    lPhysicalAddress = (m_MemoryDAT[sLogicalAddress >> 12] & DAT_MASK) + (sLogicalAddress & ADDRESS_MASK);
        //                else
        //                    lPhysicalAddress = lRemappedAddress;
        //            }
        //        }
        //    }
        //    else
        //    {
        //        // All addresses between 0x0000 and 0xDFFF can be mapped to any 4K block in the full 1 MB address range. So just
        //        // do the translation if this is the case.
        //
        //        lPhysicalAddress = (m_MemoryDAT[sLogicalAddress >> 12] & DAT_MASK) + (sLogicalAddress & ADDRESS_MASK);
        //    }
        //
        //    if (test != lPhysicalAddress)
        //    {
        //        int x = 1;
        //    }
        //
        //    return (lPhysicalAddress);
        //}
        //
    }
}
