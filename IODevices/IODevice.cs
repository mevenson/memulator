using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;

namespace Memulator
{
    class IODevice : IDisposable
    {
        public uint     m_nInterruptMask;
        public bool     _bInterruptEnabled = false;
        public ushort   m_sBaseAddress;

        private string spinString = "\\|/-|-/-";
        private int spinIndex = 0;

        public void Dispose ()
        {
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
            //if (spin)
            //    Spin();

            if (_bInterruptEnabled && Program._cpu != null && Program._cpu.Running)
            {
                Program._cpu.InterruptRegister |= m_nInterruptMask;
            }
        }

        public virtual void ClearInterrupt()
        {
            if (Program._cpu != null && Program._cpu.Running)
                Program._cpu.InterruptRegister &= ~m_nInterruptMask;
        }

        public virtual void Init(int nWhichController, byte[] sMemoryBase, ushort sBaseAddress, int nRow, bool bInterruptEnabled) 
        {
            _bInterruptEnabled = bInterruptEnabled;

            m_nInterruptMask        = (uint)(0x0001 << nRow);
            m_sBaseAddress          = sBaseAddress;

            ClearInterrupt ();
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
