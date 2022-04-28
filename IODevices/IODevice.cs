using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.IO;

namespace Memulator
{
    public class IODevice : IDisposable
    {
        public uint     m_nInterruptMask;
        public bool     _bInterruptEnabled = false;
        public ushort   m_sBaseAddress;

        public int m_nInterruptingDevice = 0;
        public int m_nBoardInterruptRegister = 0;

        private string spinString = "\\|/-|-/-";
        private int spinIndex = 0;

        public void Dispose ()
        {
            // need to shut down the Micro Timers on the DMAF-2 and DMAF-3

            System.Type type = this.GetType();

            if (type == typeof(DMAF3))
            {
                if (Program._CDMAF3._DMAFInterruptDelayTimer != null) Program._CDMAF3._DMAFInterruptDelayTimer.Stop();
                if (Program._CDMAF3._DMAFTimer != null) Program._CDMAF3._DMAFTimer.Stop();

                Program._CDMAF3._DMAFInterruptDelayTimer = null;
                Program._CDMAF3._DMAFTimer = null;
            }
            else if (type == typeof(DMAF2))
            {
                if (Program._CDMAF2._DMAFInterruptDelayTimer != null) Program._CDMAF2._DMAFInterruptDelayTimer.Stop();
                if (Program._CDMAF2._DMAFTimer != null) Program._CDMAF2._DMAFTimer.Stop();

                Program._CDMAF2._DMAFInterruptDelayTimer = null;
                Program._CDMAF2._DMAFTimer = null;
            }
        }

        bool enableLogSetAndClearInterrupts = true;

        void LogSetAndClearInterrupts (bool set)
        {
            if (enableLogSetAndClearInterrupts && Program.enableDMAF2ActivityLogChecked)
            {
                using (StreamWriter sw = new StreamWriter(File.Open(Program.activityLogFileDMAF2, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)))
                {
                    sw.WriteLine(string.Format("{0} {1} interrupt for board #{2} device {3} board interrupt register {4} at {5} interruptEnabled is {6}"
                        , Program._cpu.CurrentIP.ToString("X4")
                        , set ? "setting " : "clearing"
                        , m_nInterruptMask.ToString("X4")
                        , m_nInterruptingDevice.ToString("X4")
                        , m_nBoardInterruptRegister.ToString("X4")
                        , m_sBaseAddress.ToString("X4")
                        , _bInterruptEnabled ? "true" : "false")); ;
                }
            }
        }

        public IODevice()
        {
            if (this.GetType() == typeof(Memulator.DMAF3))
            {
                spinString = "\\|/-|-/-";
            }
            else if (this.GetType() == typeof(Memulator.ConsoleACIA))
            {
                spinString = "12345678";
            }
            else
                spinString = "\\|/-|-/-";
        }

        public void Spin()
        {
            if (this.GetType() == typeof(Memulator.ConsoleACIA))
            {
            }

            Console.Write(spinString.Substring(spinIndex++, 1));
            Console.Write("\b");

            if (spinIndex > 7)
                spinIndex = 0;
        }

        public virtual void SetInterrupt(bool spin)
        {
            if (_bInterruptEnabled && Program._cpu != null && Program._cpu.Running)
            {
                Program._cpu.InterruptRegister |= m_nInterruptMask;
            }

            LogSetAndClearInterrupts(true);
        }

        public virtual void ClearInterrupt()
        {
            // clear the mask used by the processor

            if (Program._cpu != null && Program._cpu.Running)
                Program._cpu.InterruptRegister &= ~m_nInterruptMask;

            LogSetAndClearInterrupts(false);
        }

        public virtual void Init(int nWhichController, byte[] sMemoryBase, ushort sBaseAddress, int nRow, bool bInterruptEnabled) 
        {
            _bInterruptEnabled = bInterruptEnabled;

            m_nInterruptMask        = (uint)(0x0001 << nRow);
            m_sBaseAddress          = sBaseAddress;

            ClearInterrupt ();
        }

        public virtual byte Peek(ushort m)
        {
            //ushort x = m; 
            return (0xff);
        }

        public virtual byte Read(ushort m) 
        {
            //ushort x = m; 
    	    return (0xff);
        }

        public virtual void Write(ushort m, byte b) 
        {
            //ushort x = m; 
            //byte a = b;
        }
    }
}
