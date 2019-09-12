using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;

using System.Threading;

namespace Memulator
{
    class MPID : IODevice
    {
        enum Modes
        {
            PIA_MODE_DATA   = 0,
            PIA_MODE_PROG   = 1
        }

        enum Strobe
        {
            STROBE_LOW      = 0,
            STROBE_HIGH     = 1
        }

        int  m_nRow;
        bool m_nProcessorRunning;
        bool m_nMPIDRunning;
        int  m_nMPIDRate;

        bool m_bCanResetTimerInterrupt;

        byte m_MPIDStatusRegister;
        byte m_MPIDControlRegister1;
        byte m_MPIDControlRegister2;
        byte m_MPIDControlRegister3;

        ushort m_PIA_A_PRT_CMD;
        ushort m_PIA_A_PRT_DAT;

        byte m_cPIA_A_DataDirByte;

        Modes  m_bPIA_A_Mode;
        Strobe m_bPIA_A_Strobe;

        ushort m_PIA_B_PRT_CMD;
        ushort m_PIA_B_PRT_DAT;

        byte m_cPIA_B_DataDirByte;

        Modes  m_bPIA_B_Mode;
        Strobe m_bPIA_B_Strobe;

        bool [] m_RegsiterIsInterrupting = new bool[3];
        bool [] m_bCounterEnabled        = new bool[3];

        byte [] m_MPIDTimerMSB = new byte[3];
        byte [] m_MPIDTimerLSB = new byte[3];
        byte [] m_MPIDLatchMSB = new byte[3];
        byte [] m_MPIDLatchLSB = new byte[3];

        bool [] m_bTimerIsStarted = new bool[3];

        Timer m_nMPIDTimer;

        public MPID()
        {
            m_nProcessorRunning = false;

            m_nMPIDRunning  = false;
            m_nMPIDRate     = 100;               // 16.66666666 msec = 1/60th of a second

            m_MPIDControlRegister1 = 0;
            m_MPIDControlRegister2 = 0;
            m_MPIDControlRegister3 = 0;

            for (int i = 0; i < 3; i++)
            {
                m_MPIDTimerMSB[i] = 0;
                m_MPIDTimerLSB[i] = 0;
                m_RegsiterIsInterrupting[i] = false;
                m_bTimerIsStarted[i] = false;
            }

            m_bCanResetTimerInterrupt = false;
            m_MPIDStatusRegister = 0;

            m_bTimerIsStarted[0] = true;
            
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

            TimerCallback tcb = MPID_TimerProc;
            m_nMPIDTimer = new Timer(tcb, null, m_nMPIDRate, 0);

            StartMPIDTimer(m_nMPIDRate);
            m_nMPIDRunning = true;
        }

        //~CMPID()
        //{
        //    m_nProcessorRunning = false;
        //    //StopMPIDTimer ();
        //}

        //void CALLBACK MPID_TIMER (uint uTimerID, uint uMsg, DWORD_PTR dwUser, DWORD_PTR dw1, DWORD_PTR dw2)
        //{
        //    CMPID* pTimer = (CMPID*) dwUser;
        //    pTimer->MPID_TimerProc ();
        //}

        public void MPID_TimerProc(object state)
        {
            byte c, d;
            uint e;

            // if the processor is not running - do nothing because we may be unstable

            if (Program._cpu.Running)
            {
                if (m_nMPIDRunning)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        if ((m_MPIDControlRegister1 & 0x01) == 0x00)
                        {
                            if (m_bCounterEnabled[i])
                            {
                                c = m_MPIDTimerMSB[i];
                                d = m_MPIDTimerLSB[i];

                                e = (uint)((c * 256) + d);
                    
                                --e;
                    
                                c = (byte)(e / 256);
                                d = (byte)(e % 256);

                                m_MPIDTimerMSB[i] = c;
                                m_MPIDTimerLSB[i] = d;

                                m_RegsiterIsInterrupting[i] = true;
                            }
                        }
                    }
                }

                // if any of the timer interrupts are enabled, set interrupt bit in the processor.

                if ((m_MPIDControlRegister1 & 0x40) != 0 || (m_MPIDControlRegister2 & 0x40) != 0 || (m_MPIDControlRegister3 & 0x40) != 0)
                {
                    if (_bInterruptEnabled)
                    {
                        SetInterrupt(false);
                        m_MPIDStatusRegister |= 0x80;
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
        }

        void StartMPIDTimer (int rate)
        {
            //uint uResolution = 10;

            // Only the processor can start the timer

            m_nProcessorRunning = true;

            // so start it already

            // 16.66666666 msec = 1/60th of a second

            // then start it

            m_nMPIDTimer.Change(0, m_nMPIDRate);   // this starts it for m_nMPTRate milliseconds

            //m_nMPIDTimer = timeSetEvent(rate, uResolution, MPID_TIMER, (DWORD) this, TIME_PERIODIC);
            //if (m_nMPIDTimer == null)
            //    AfxMessageBox ("Failed to create MPID timer");
        }

        void StopMPIDTimer ()
        {
            try
            {
                if (m_nMPIDRunning)
                {
                    ClearInterrupt ();
                    m_nMPIDTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    m_nMPIDRunning = false;
                }
            }
            catch 
            {
                //AfxMessageBox ("Error trying to stop MPID Timer");
            }
        }

        public override byte Read (ushort m)
        {
            byte d = 0xFF;

            // remap to Other IO boards - MPID really starts at 0xE080 and diverts all addresses between 0xE000 through 0xE07F to the IO Slots

            if (m >= m_sBaseAddress + 0x80)
            {

                switch (m & 0x00F0)
                {
                    case 0x0080:        // PIA on MP-ID
                        switch (m & 0x0003)
                        {
                            case 0:     // data port A side
                                Program._cpu.WriteToFirst64K((ushort)(m + 1), (byte)(Program._cpu.Memory.MemorySpace[m_PIA_A_PRT_CMD] & 0x7F));      // turn off the ready bit
                                d = (byte)(Program._cpu.Memory.MemorySpace[m_PIA_A_PRT_DAT]);   // *m_PIA_A_PRTDatAddress;
                                break;

                            case 1:     // control / status port A side
                                d = (byte)(Program._cpu.Memory.MemorySpace[m_PIA_A_PRT_CMD] & 0x7F);    // always return ready
                                break;

                            case 2:     // data port B side
                                Program._cpu.WriteToFirst64K((ushort)(m + 1), (byte)(Program._cpu.Memory.MemorySpace[m_PIA_B_PRT_CMD] & 0x7F));      // turn off the ready bit
                                d = (byte)(Program._cpu.Memory.MemorySpace[m_PIA_B_PRT_CMD] & 0x7F);
                                break;

                            case 3:     // control / status port B side
                                d = (byte)(Program._cpu.Memory.MemorySpace[m_PIA_B_PRT_CMD] & 0x7F);    // always return ready
                                break;
                        }
                        break;

                    case 0x0090:        // 6840 on MP-ID
                        switch (m & 0x0007)
                        {
                            case 0:         // NOP
                                break;

                            case 1:         // Read the Status register

                                // get the status of the interrupt bits for each of the counters

                                if ((m_MPIDControlRegister1 & 0x40) != 0 && m_RegsiterIsInterrupting[0])
                                    m_MPIDStatusRegister |= 0x01;
                                else
                                    m_MPIDStatusRegister &= (byte)(~0x01 & 0xff);

                                if ((m_MPIDControlRegister1 & 0x40) != 0 && m_RegsiterIsInterrupting[1])
                                    m_MPIDStatusRegister |= 0x02;
                                else
                                    m_MPIDStatusRegister &= (byte)(~0x02 & 0xff);

                                if ((m_MPIDControlRegister1 & 0x40) != 0 && m_RegsiterIsInterrupting[2])
                                    m_MPIDStatusRegister |= 0x04;
                                else
                                    m_MPIDStatusRegister &= (byte)(~0x04 & 0xff);

                                d = m_MPIDStatusRegister;

                                // if any of them are set and the mask bit in the corresponding control register is enabled, set the master interrupt bit
                                // in the status register, otherwise - clear it. If we set it - signal the Counter register reads to clear the interrupt.

                                if ((m_MPIDStatusRegister & 0x07) != 0)
                                    m_MPIDStatusRegister |= 0x80;
                                else
                                {
                                    m_MPIDStatusRegister &= 0x07;
                                    ClearInterrupt();
                                }

                                m_bCanResetTimerInterrupt = true;
                                break;

                            case 2:         // Read Timer Number 1 counter
                                if (m_bCanResetTimerInterrupt)
                                {
                                    m_bCanResetTimerInterrupt = false;
                                    m_MPIDStatusRegister &= 0x7F;
                                    m_MPIDStatusRegister &= (byte)(~0x01 & 0xff);

                                    ClearInterrupt();
                                }
                                d = m_MPIDTimerMSB[0];
                                break;

                            case 3:         // Read Timer Number 1 counter LSB
                                d = m_MPIDTimerLSB[0];
                                break;

                            case 4:         // Read Timer Number 2 counter LSB
                                if (m_bCanResetTimerInterrupt)
                                {
                                    m_bCanResetTimerInterrupt = false;
                                    m_MPIDStatusRegister &= 0x7F;
                                    m_MPIDStatusRegister &= (byte)(~0x02 & 0xff);

                                    ClearInterrupt();
                                }
                                d = m_MPIDTimerMSB[1];
                                break;

                            case 5:         // Read Timer Number 2 counter LSB
                                d = m_MPIDTimerLSB[1];
                                break;

                            case 6:         // Read Timer Number 3 counter LSB
                                if (m_bCanResetTimerInterrupt)
                                {
                                    m_bCanResetTimerInterrupt = false;
                                    m_MPIDStatusRegister &= 0x7F;
                                    m_MPIDStatusRegister &= (byte)(~0x04 & 0xff);

                                    ClearInterrupt();
                                }
                                d = m_MPIDTimerMSB[2];
                                break;

                            case 7:         // Read Timer Number 3 counter LSB
                                d = m_MPIDTimerLSB[2];
                                break;
                        }
                        break;
                }
            }

            return (d);
        }

        public override void Write (ushort m, byte c)
        {
            int x = 0;
            switch (m & 0x00F0)
            {
                case 0x0080:        // PIA on MP-ID
                    switch (m & 0x0003)
                    {
                        case 0:     // data port A side
                            if (m_bPIA_A_Mode == Modes.PIA_MODE_DATA)
                                Program._cpu.Memory.MemorySpace[m_PIA_A_PRT_DAT] = c;
                            else
                                m_cPIA_A_DataDirByte = c;
                            break;

                        case 1:     // control / status port A side
                            if ((c & 0x04) != 0)
                                m_bPIA_A_Mode = Modes.PIA_MODE_DATA;
                            else
                                m_bPIA_A_Mode = Modes.PIA_MODE_PROG;

                            if ((c & 0x08) != 0)
                            {
                                if (m_bPIA_A_Strobe == Strobe.STROBE_LOW) // transition from low to high
                                {
                                    byte pia_A_Cmd = (byte)(Program._cpu.Memory.MemorySpace[m_PIA_A_PRT_DAT]);
                                }
                                m_bPIA_A_Strobe = Strobe.STROBE_HIGH;
                            }
                            else
                                m_bPIA_A_Strobe = Strobe.STROBE_LOW;
                            break;

                        case 2:     // data port B side
                            if (m_bPIA_B_Mode == Modes.PIA_MODE_DATA)
                                Program._cpu.Memory.MemorySpace[m_PIA_B_PRT_CMD] = c;
                            else
                                m_cPIA_B_DataDirByte = c;
                            break;

                        case 3:     // control / status port B side
                            if ((c & 0x04) != 0)
                                m_bPIA_B_Mode = Modes.PIA_MODE_DATA;
                            else
                                m_bPIA_B_Mode = Modes.PIA_MODE_PROG;

                            if ((c & 0x08) != 0)
                            {
                                if (m_bPIA_B_Strobe == Strobe.STROBE_LOW) // transition from low to high
                                {
                                    byte pia_B_Cmd = (byte)(Program._cpu.Memory.MemorySpace[m_PIA_B_PRT_CMD] & 0x7F);
                                }
                                m_bPIA_B_Strobe = Strobe.STROBE_HIGH;
                            }
                            else
                                m_bPIA_B_Strobe = Strobe.STROBE_LOW;
                            break;
                    }
                    break;

                case 0x0090:        // 6840 on MP-ID
                    //
                    //  This is how UniFLEX programs the MPID 6840
                    //
                    //WRITE: AD57: E091 01      select Control Register 1 as the register to write to when register 0x00 is written and mask T2 interrupt
                    //WRITE: AD5C: E090 80      Enable Timer 1 output enabled and allow all timers to operate
                    //WRITE: AD5F: E091 00      select Control Register 3 as the register to write to when register 0x00 is written and mask T2 interrupt
                    //WRITE: AD6F: E096 00      set counter value high byte in timer 3
                    //WRITE: AD6F: E097 0B      set counter value low byte in timer 3
                    //WRITE: AD74: E090 40      set interrupt enable flag for timer 3
                    //WRITE: AD79: E091 01      select Control Register 1 as the register to write to when register 0x00 is written and mask T2 interrupt
                    //WRITE: AD7C: E090 00      allow all timers to operate and mask interrupt for timer 1
                    //
                    //  So only timer 3 is set to interrupt the system and it is also the only one that has a counter value set

                    switch (m & 0x0007)
                    {
                        case 0:
                            if ((m_MPIDControlRegister2 & 0x01) != 0)  // writing to Control Register 1
                            {
                                m_bCanResetTimerInterrupt = true;
                                m_MPIDControlRegister1 = c;

                                if ((c & 0x01) != 0)
                                {
                                    for (int i = 0; i < 3; i++)
                                    {
                                        m_bCounterEnabled[i] = false;
                                        m_MPIDTimerMSB[i] = m_MPIDLatchMSB[i];
                                        m_MPIDTimerLSB[i] = m_MPIDLatchLSB[i];
                                        m_MPIDControlRegister1 &= (byte)(~0xC0 & 0xff);
                                        m_MPIDControlRegister2 &= (byte)(~0xC0 & 0xff);
                                        m_MPIDControlRegister2 &= (byte)(~0xC0 & 0xff);
                                    }

                                    ClearInterrupt ();
                                }
                                else
                                {
                                    for (int i = 0; i < 3; i++)
                                    {
                                        m_bCounterEnabled[i] = false;
                                    }
                                }
                            }
                            else                                // writing to Control Register 3
                            {
                                m_MPIDControlRegister3 = c;
                            }
                            break;

                        case 1:
                            m_MPIDControlRegister2 = c;
                            break;

                        case 2:
                            m_MPIDTimerMSB[0] = c;
                            m_MPIDLatchMSB[0] = c;
                            break;

                        case 3:
                            m_MPIDTimerLSB[0] = c;
                            m_MPIDLatchLSB[0] = c;
                            break;

                        case 4:
                            m_MPIDTimerMSB[1] = c;
                            m_MPIDLatchMSB[1] = c;
                            break;

                        case 5:
                            m_MPIDTimerLSB[1] = c;
                            m_MPIDLatchMSB[1] = c;
                            break;

                        case 6:
                            m_MPIDTimerMSB[2] = c;
                            m_MPIDLatchMSB[2] = c;
                            break;

                        case 7:
                            m_MPIDTimerLSB[2] = c;
                            m_MPIDLatchLSB[2] = c;
                            break;
                    }

                    break;
            }
        }

        public override void Init(int nWhichController, byte[] sMemoryBase, ushort sBaseAddress, int nRow, bool bInterruptEnabled)
        {
            m_nRow = nRow;
            base.Init (nWhichController, sMemoryBase, sBaseAddress, nRow, bInterruptEnabled);

            ClearInterrupt ();

            // Get the device types for forwarding reads and writes when the MP-ID is set to control the ports

            for (nRow = 0; nRow < 16; nRow++)
            {
                // re-setup the device maps that we screwed up with the MP-ID being mapped over the standard ports

                if (Program.BoardInfo[nRow] != null)
                {
                    if (Program.BoardInfo[nRow].cDeviceType != (byte)Devices.DEVICE_MPID)
                    {
                        for (int i = 0; i < Program.BoardInfo[nRow].sNumberOfBytes; i++)
                        {
                            Program._cpu.Memory.DeviceMap[Program.BoardInfo[nRow].sBaseAddress + i] = Program.BoardInfo[nRow].cDeviceType;
                        }
                    }
                }
                else
                    break;
            }

            m_PIA_A_PRT_CMD         = (ushort)(sBaseAddress + 1);
            m_PIA_A_PRT_DAT         = sBaseAddress;

            m_PIA_B_PRT_CMD         = (ushort)(sBaseAddress + 1);
            m_PIA_B_PRT_DAT         = sBaseAddress;
        }
    }
}
