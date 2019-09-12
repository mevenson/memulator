using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Memulator
{
    enum OffsetRegisterIndex
    {
        m_X = 0,
        m_Y = 1,
        m_U = 2,
        m_S = 3
    }

    class Cpu
    {
        public Socket MySocket = null;

        private bool breakpointsEnabled = false;
        public bool BreakpointsEnabled
        {
            get { return breakpointsEnabled; }
            set { breakpointsEnabled = value; }
        }

        private int breakpointCount = 0;
        public int BreakpointCount
        {
            get { return breakpointCount; }
            set { breakpointCount = value; }
        }

        private List<ushort> breakpointAddress = new List<ushort>();
        public List<ushort> BreakpointAddress
        {
            get { return breakpointAddress; }
            set { breakpointAddress = value; }
        }

        private List<ushort> callStack = new List<ushort>();
        public List<ushort> CallStack
        {
            get { return callStack; }
            set { callStack = value; }
        }

        private ushort currentIP;
        public ushort CurrentIP
        {
            get { return currentIP; }
            set { currentIP = value; }
        }

        private ushort nextIPToStopOn = 0xFFFF;
        public ushort NextIPToStopOn
        {
            get { return nextIPToStopOn; }
            set { nextIPToStopOn = value; }
        }

        private bool singleStepMode = false;
        public bool SingleStepMode
        {
            get { return singleStepMode; }
            set { singleStepMode = value; }
        }

        private bool showDebugOutput = false;
        public bool ShowDebugOutput
        {
            get { return showDebugOutput; }
            set { showDebugOutput = value; }
        }

        // Program counter used by 6800
        private ushort programCounter = 0x000;
        public ushort ProgramCounter
        {
            get { return programCounter; }
            set { programCounter = value; }
        }

        // Program counter used by 6809
        private ushort ip;
        public ushort IP
        {
            get { return ip; }
            set { ip = value; }
        }

        ushort[] offsetRegisters = new ushort[4];
        public ushort[] OffsetRegisters
        {
            get { return offsetRegisters; }
            set { offsetRegisters = value; }
        }

        private volatile bool running = false;
        public bool Running
        {
            get { return running; }
            set { running = value; }
        }


        public static int traceSize = 65536;
        public static bool traceFull;

        public static TraceEntry [] clsTraceBuffer = new TraceEntry[traceSize];
        public string [] DebugLine = new string[traceSize];

        public bool Switch1B = false;
        public bool Switch1C = false;
        public bool Switch1D = false;
        public bool RamE000  = false;
        public bool RamE800  = false;
        public bool RamF000  = false;
        public bool RomE000  = false;
        public bool RomE800  = false;
        public bool RomF000  = false;
        
        public bool IrqAsserted = false;
        
        public volatile uint InterruptRegister = 0;

        public long TraceIndex;

        private volatile bool inWait;
        public bool InWait
        {
            get { return inWait; }
            set { inWait = value; }
        }

        private volatile bool inSync;
        public bool InSync
        {
            get { return inSync; }
            set { inSync = value; }
        }

        public Int64 TotalCycles      = 0;
        public Int64 CyclesThisPeriod = 0;

        static Memory memory;
        public Memory Memory
        {
            get { return memory; }
            set { memory = value; }
        }

        bool resetPressed = false;
        public bool ResetPressed
        {
            get { return resetPressed; }
            set { resetPressed = value; }
        }

        bool nmiPressed = false;
        public bool NmiPressed
        {
            get { return nmiPressed; }
            set { nmiPressed = value; }
        }

        bool irqPressed = false;
        public bool IrqPressed
        {
            get { return irqPressed; }
            set { irqPressed = value; }
        }

        public virtual void CoreDump()
        {
        }

        public virtual void Run()
        {
        }

        public virtual void WriteToFirst64K (ushort m, byte b)
        {
        }

        public virtual byte ReadFromFirst64K(ushort m)
        {
            return 0x00;
        }
    }
}
