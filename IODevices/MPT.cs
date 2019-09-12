using System;
using System.Collections.Generic;
using System.Text;

using System.Threading;

namespace Memulator
{
    class MPT : IODevice
    {
        bool _spin = false;
        int m_nRow;

        ushort  m_MPTCtlPort;
        ushort  m_MPTOscPort;

        byte    m_MPTCtlPortRegister;
        byte    m_MPTOscPortRegister;

        Timer m_nMPTimer;

        public Timer MPTimer
        {
            get { return m_nMPTimer; }
            set { m_nMPTimer = value; }
        }
        bool    m_nMPTRunning;
        int     m_nMPTRate;

        bool m_nProcessorRunning;

        public MPT()
        {
            m_nProcessorRunning = false;
            m_nMPTRunning = false;
            m_nMPTRate = 10;

            TimerCallback tcb = MPT_TimerProc;

            m_nMPTimer = new Timer(tcb, null, 0, m_nMPTRate);
        }

        public void MPT_TimerProc (object state)
        {
            // if the processor is not running - do nothing because we may be unstable

            if (_bInterruptEnabled && Program._cpu.Running)
            {
                m_MPTOscPortRegister |= 0x80;
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
                else
                {
                    m_nMPTimer.Change(0, m_nMPTRate);   // this starts it for m_nMPTRate milliseconds
                }
            }
        }

        void StartMPTimer (int rate)
        {
            // Only the processor can start the timer

            if (_bInterruptEnabled)
            {
                m_nProcessorRunning = true;
                m_nMPTimer.Change(0, m_nMPTRate);
            }
        }

        void StopMPTimer ()
        {
            m_nMPTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public override byte Read (ushort m)
        {
            byte c;

            if (m == m_MPTCtlPort)
            {
                // reading this port clears any pending interrupt flag
    
                m_MPTOscPortRegister &= 0x7F;
                c = m_MPTCtlPortRegister;
                ClearInterrupt ();
            }
            else if (m == m_MPTOscPort)
                c = m_MPTOscPortRegister;
            else
                c = 0xff;

            return (c);
        }

        public override void Write(ushort m, byte c)
        {
            if (m == m_MPTCtlPort)
            {
                ClearInterrupt();
                if ((c & 0x80) != 0)
                {
                    m_nMPTRunning = false;
                    StopMPTimer ();
                }
                else    // set interrupt interval
                {
                    switch (c & 0x0f)
                    {
                        case 0x00:  //    1 microsecond
                        case 0x01:  //   10 microseconds
                        case 0x02:  //  100 microseconds
                        case 0x03:  //  no output
                            m_nMPTRate = 1;
                            break;
                        case 0x04:  //   10 milliseconds
                            m_nMPTRate = 10;
                            break;
                        case 0x05:  //  100 milliseconds
                            m_nMPTRate = 100;
                            break;
                        case 0x06:  //    1 second
                            m_nMPTRate = 1000;
                            break;
                        case 0x07:  //   10 seconds
                            m_nMPTRate = 10000;
                            break;
                        case 0x08:  //  100 seconds
                            m_nMPTRate = 100000;
                            break;
                        case 0x09:  //    1 minute
                            m_nMPTRate = 60000;
                            break;
                        case 0x0A:  //    1 hour
                            m_nMPTRate = 60000 * 60;
                            break;
                        case 0x0B:  //   10 minutes
                            m_nMPTRate = 60000 * 600;
                            break;
                        case 0x0C:  //  no output
                        case 0x0D:  //  no output
                            m_nMPTRate = 0;
                            break;
                        case 0x0E:  //   20 milliseconds
                            m_nMPTRate = 20;
                            break;
                        case 0x0F:  //  no output
                            break;
                    }
                    StartMPTimer (m_nMPTRate);
                    m_nMPTRunning = true;
                }
            }
        }

        public override void Init(int nWhichController, byte [] sMemoryBase, ushort sBaseAddress, int nRow, bool bInterruptEnabled)
        {
            m_nRow = nRow;
            base.Init(nWhichController, sMemoryBase, sBaseAddress, nRow, bInterruptEnabled);

            ClearInterrupt ();

            // Set up addresses and references

            m_MPTCtlPort = (ushort)(sBaseAddress + 2);
            m_MPTOscPort = (ushort)(sBaseAddress + 3);
        }
    }
}
