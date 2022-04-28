using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;

using System.Threading;

namespace Memulator
{
    class ConsoleACIA : IODevice
    {
        bool _spin = false;

        public byte ACIA_RDRF   = 0x01;
        public byte ACIA_TDRE   = 0x02;
        public byte ACIA_DCD    = 0x04;
        public byte ACIA_CTS    = 0x08;
        public byte ACIA_FE     = 0x10;
        public byte ACIA_OVRN   = 0x20;
        public byte ACIA_PE     = 0x40;
        public byte ACIA_IRQ    = 0x80;

        public byte ACIA_TDRE_IRQ_ENABLED = 0x20;
        public byte ACIA_RDRF_IRQ_ENABLED = 0x80;

        public bool _bACIAHasBeenReset = false;

        int _nRow = 0;

        public volatile byte _ACIA_CONSStatusRegister;
        public volatile byte _ACIA_CONSDataRegister;
        public volatile byte _ACIA_CONSCommandRegister;

        public volatile bool _bCanAssertConsoleRCVInterrupt;
        public volatile bool _bCanAssertConsoleXMTInterrupt;

        public volatile int _nKeyboardBufferCount;
        public volatile object _bMovingCharacter = new object();
        public volatile int _nKeyboardGetPointer;
        public volatile int _nKeyboardPutPointer;

        public volatile byte[] _cKeyboardBuffer = new byte[8192];

        public List<byte> keyboardQueue = new List<byte>();

        public override void Init (int nWhichController, byte [] sMemoryBase, ushort sBaseAddress, int nRow, bool bInterruptEnabled)
        {
            // Set up addresses and references

            _nRow = nRow;
            base.Init (nWhichController, sMemoryBase, sBaseAddress, nRow, bInterruptEnabled);

            _ACIA_CONSStatusRegister = ACIA_TDRE;
        }

        public override void Write (ushort m, byte b)
        {
            lock (_bMovingCharacter)                // anytime we are about to modify _nKeyboardBufferCount or _nKeyboardGetPointer we need a lock
            {
                if (Program._cpu.CurrentIP >= 0xA600 && Program._cpu.CurrentIP <= 0xB000)
                {
                }
                switch (m & 0x0001)
                {
                    // interogate the command bytes and set up the COMx port accordingly
                    //
                    //  0x35 = counter/16, 8 bit, no parity, 1 stopbit, rts,  en xmit interrupt 
                    //
                    //  Use the combination of m_siPort[i].sBaudRate and the
                    //      m_n9600
                    //      m_n4800
                    //      m_nBR
                    // flags to determine the baud rate
                    //
                    //   w .... ..00  counter/1
                    //   w .... ..01  counter/16
                    //   w .... ..10  counter/64
                    //   w .... ..11  master reset
                    //   w ...0 00..  7 bit, even parity, 2 stopbit
                    //   w ...0 01..  7 bit,  odd parity, 2 stopbit
                    //   w ...0 10..  7 bit, even parity, 1 stopbit
                    //   w ...0 11..  7 bit,  odd parity, 1 stopbit
                    //   w ...1 00..  8 bit,   no parity, 2 stopbit
                    //   w ...1 01..  8 bit,   no parity, 1 stopbit
                    //   w ...1 10..  8 bit, even parity, 1 stopbit
                    //   w ...1 11..  8 bit,  odd parity, 1 stopbit
                    //   w .00. ....   rts = low    disable xmit interrupt
                    //   w .01. ....   rts = low    enable  xmit interrupt
                    //   w .10. ....   rts = high   disable xmit interrupt
                    //   w .11. ....   rts = low    disable xmit intrpt and xmit brk-level
                    //   w 1... ....   enable receive interrupt

                    //  status bits
                    //
                    //   r .......1  rcve data reg full
                    //   r ......1.  xmit data reg empty
                    //   r .....1..  -data carrier detect (or has been down)
                    //   r ....1...  -clear to send
                    //   r ...1....  framing error
                    //   r ..1.....  receiver overrun
                    //   r .1......  parity error
                    //   r 1.......  interrupt request

                    case 0:
                        _ACIA_CONSCommandRegister = b;

                        if ((b & 0x03) == 0x03)
                        {
                            _bCanAssertConsoleXMTInterrupt = false;
                            _bCanAssertConsoleRCVInterrupt = false;
                            ClearInterrupt();

                            _ACIA_CONSStatusRegister = 0;

                            if (((b & 0x40) == 0x00) && ((b & 0x20) == 0x20))
                            {
                                if (_bACIAHasBeenReset)
                                {
                                    _ACIA_CONSStatusRegister |= ACIA_IRQ;
                                    if (_bInterruptEnabled)
                                    {
                                        //if ((_ACIA_CONSStatusRegister & ACIA_IRQ) == ACIA_IRQ)
                                        //{
                                        _bCanAssertConsoleXMTInterrupt = true;
                                        SetInterrupt(_spin);
                                        if (Program._cpu != null)
                                        {
                                            if ((Program._cpu.InWait || Program._cpu.InSync) && Program.CpuThread.ThreadState == ThreadState.Suspended)
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
                                        //}
                                    }
                                }
                            }
                            _bACIAHasBeenReset = true;

                            if (b == 0x37)
                            {
                                _bACIAHasBeenReset = false;
                                _ACIA_CONSStatusRegister = (byte)(ACIA_TDRE & ~ACIA_DCD);
                            }
                        }
                        else
                        {
                            _ACIA_CONSStatusRegister = (byte)(ACIA_TDRE & ~ACIA_DCD);

                            if ((_ACIA_CONSCommandRegister & ACIA_TDRE_IRQ_ENABLED) != 0) //        && ((m_ACIA_CONSCommandRegister & 0x40) == 0))
                            {
                                _ACIA_CONSStatusRegister |= ACIA_IRQ;
                                if (_bInterruptEnabled)
                                {
                                    //if ((m_ACIA_CONSStatusRegister & ACIA_IRQ) == ACIA_IRQ)
                                    //{
                                    _bCanAssertConsoleXMTInterrupt = true;
                                    SetInterrupt(_spin);
                                    if (Program._cpu != null)
                                    {
                                        if ((Program._cpu.InWait || Program._cpu.InSync) && Program.CpuThread.ThreadState == ThreadState.Suspended)
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
                                    //}
                                }
                            }
                            else
                            {
                                _bCanAssertConsoleXMTInterrupt = false;
                                if (_bCanAssertConsoleXMTInterrupt | _bCanAssertConsoleRCVInterrupt)
                                {
                                    SetInterrupt(_spin);
                                    if (Program._cpu != null)
                                    {
                                        if ((Program._cpu.InWait || Program._cpu.InSync) && Program.CpuThread.ThreadState == ThreadState.Suspended)
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
                                else
                                    ClearInterrupt();

                                if (!_bCanAssertConsoleRCVInterrupt && !_bCanAssertConsoleXMTInterrupt)
                                    _ACIA_CONSStatusRegister &= (byte)~ACIA_IRQ;
                            }

                            _ACIA_CONSStatusRegister |= (byte)(ACIA_DCD | ACIA_CTS);     // Set no CTS and no DCD after setup until first status read
                        }
                        break;

                    case 1:
                        _ACIA_CONSStatusRegister &= (byte)~ACIA_TDRE;
                        Program._theConsole.Terminal.PutCharacter(b);       // This is where we actually put out the character to the screen device
                        //if (Program.debugMode)
                        //{
                        //    if (Program._cpu.MySocket != null)
                        //    {
                        //        byte[] buffer = new byte[1];
                        //        buffer[0] = b;
                        //        Program._cpu.MySocket.Send(buffer);
                        //    }
                        //}
                        _ACIA_CONSStatusRegister |= ACIA_TDRE;

                        if (((_ACIA_CONSCommandRegister & ACIA_TDRE_IRQ_ENABLED) != 0) && ((_ACIA_CONSCommandRegister & 0x40) == 0))
                        {
                            _ACIA_CONSStatusRegister |= ACIA_IRQ;

                            if (_bInterruptEnabled)
                            {
                                _bCanAssertConsoleXMTInterrupt = true;
                                SetInterrupt(_spin);
                                if (Program._cpu != null)
                                {
                                    if ((Program._cpu.InWait || Program._cpu.InSync) && Program.CpuThread.ThreadState == ThreadState.Suspended)
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
                }
            }
        }

        public override byte Read (ushort m)
        {
            byte d = 0x00;
            
            lock(_bMovingCharacter)                // anytime we are about to modify _nKeyboardBufferCount or _nKeyboardGetPointer we need a lock
            {

                switch (m & 0x0001)
                {
                    case 0:
                        if (keyboardQueue.Count > 0)
                            _ACIA_CONSStatusRegister |= ACIA_RDRF;

                        //if (_nKeyboardBufferCount > 0)
                        //    _ACIA_CONSStatusRegister |= ACIA_RDRF;

                        lock (Program._cpu.buildingDebugLineLock)
                        {
                        _ACIA_CONSStatusRegister &= (byte)~(ACIA_DCD | ACIA_CTS);    // signal CTS and DCD true after status read

                        d = _ACIA_CONSStatusRegister;
                        }
                        break;

                    case 1:

                        if (keyboardQueue.Count > 0)
                        {
                            // retrieve the data from the queue

                            _ACIA_CONSDataRegister = keyboardQueue[0];
                            d = keyboardQueue[0];
                            keyboardQueue.RemoveAt(0);
                        }
                        else
                        {
                            // if there is nothing left in the queue - just return the last character read.
                            d = _ACIA_CONSDataRegister;
                        }

                        //d = _ACIA_CONSDataRegister;                     // get the character from the ACIA data register
                        lock (Program._cpu.buildingDebugLineLock)
                        {
                        _ACIA_CONSStatusRegister &= (byte)~ACIA_RDRF;   // clear RDRF flag in ACIA command register
                        _bCanAssertConsoleRCVInterrupt = false;         // turn off ACIA interrupt
                        }

                        if (_bCanAssertConsoleXMTInterrupt || _bCanAssertConsoleRCVInterrupt)
                        {
                            SetInterrupt(_spin);
                            if (Program._cpu != null)
                            {
                                if ((Program._cpu.InWait || Program._cpu.InSync) && Program.CpuThread.ThreadState == ThreadState.Suspended)
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
                        else
                            ClearInterrupt ();

                        // if it's got stuff - turn the RDRF back on

                        if (keyboardQueue.Count > 0)
                        {
                            _ACIA_CONSDataRegister = keyboardQueue[0];
                            _ACIA_CONSStatusRegister |= ACIA_RDRF;
                        }

                        //if (_nKeyboardBufferCount > 0)              // see if there are any chars in the buffer
                        //{
                        //    _ACIA_CONSDataRegister = _cKeyboardBuffer[_nKeyboardGetPointer++];
                        //    _ACIA_CONSStatusRegister |= ACIA_RDRF;

                        //    if (--_nKeyboardBufferCount == 0)  // decrement count and check for empty
                        //    {
                        //        _nKeyboardGetPointer = 0;      // if empty - reset pointer to top
                        //    }
                        //    else
                        //    {
                        //        if ((_ACIA_CONSCommandRegister & ACIA_RDRF_IRQ_ENABLED) != 0)
                        //        {
                        //            _ACIA_CONSStatusRegister |= ACIA_IRQ;
                        //            if (_bInterruptEnabled)
                        //            {
                        //                _bCanAssertConsoleRCVInterrupt = true;
                        //                SetInterrupt(_spin);
                        //                if (Program._cpu != null)
                        //                {
                        //                    if ((Program._cpu.InWait || Program._cpu.InSync) && Program._cpuThread.ThreadState == ThreadState.Suspended)
                        //                    {
                        //                        try
                        //                        {
                        //                            Program._cpuThread.Resume();
                        //                        }
                        //                        catch (ThreadStateException e)
                        //                        {
                        //                            // do nothing if thread is not suspended
                        //                        }
                        //                    }
                        //                }
                        //            }
                        //        }
                        //    }
                        //}

                        if (!_bCanAssertConsoleRCVInterrupt && !_bCanAssertConsoleXMTInterrupt)
                        {
                            // both interrupts are not allowed now - so mask it off

                            lock (Program._cpu.buildingDebugLineLock)
                            {
                            _ACIA_CONSStatusRegister &= (byte)~ACIA_IRQ;
                            }
                        }
                        break;
                }
            }
            return (d);
        }
        public override byte Peek(ushort m)
        {
            byte d = 0x00;
            lock (_bMovingCharacter)                // anytime we are about to modify _nKeyboardBufferCount or _nKeyboardGetPointer we need a lock
            {
                switch (m & 0x0001)
                {
                    case 0:
                        d = _ACIA_CONSStatusRegister;
                        break;
                    case 1:
                        if (keyboardQueue.Count > 0)
                        {
                            _ACIA_CONSDataRegister = keyboardQueue[0];
                            d = keyboardQueue[0];
                        }
                        else
                        {
                            d = _ACIA_CONSDataRegister;
                        }

                        break;
                }
            }

            return (d);
        }

        public override void  SetInterrupt(bool spin)
        {
            if (_bCanAssertConsoleXMTInterrupt || _bCanAssertConsoleRCVInterrupt)
                base.SetInterrupt(_spin);
        }
    }
}
