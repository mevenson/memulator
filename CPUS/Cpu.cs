using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.InteropServices;

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
        public int processorType = 0;
        public bool _bUseCircularBufferTraceFile = false;

        protected byte   _aReg = 0x00;
        protected byte   _bReg = 0x00;
        protected ushort _xReg = 0x0000;
		
        protected byte   _ccr;
        protected ushort _sp = 0;

        public byte AReg
        {
            get { return _aReg; }
            set { _aReg = value; }
        }
        public byte BReg
        {
            get { return _bReg; }
            set { _bReg = value; }
        }
        public ushort XReg
        {
            get { return _xReg; }
            set { _xReg = value; }
        }

        public Socket MySocket = null;

        private bool traceEnabled = false;
        public bool TraceEnabled { get => traceEnabled; set => traceEnabled = value; }
        [StructLayout(LayoutKind.Explicit)]
        public struct regs
        {
            [FieldOffset(0)]
            public byte lo;
            [FieldOffset(1)]
            public byte hi;
        };
        protected regs _dReg = new regs();
        public ushort Xreg;
        public ushort Yreg;
        public ushort Ureg;
        public ushort Sreg;
        protected ushort m_SP;        // Stack Pointer (6800)
        protected byte m_DP;        // Direct Page Register         6809 only
        protected byte m_CCR;       // Condition Code Register
        protected ushort m_D
        {
            get { return (ushort)((ushort)(_dReg.hi * 256) + (ushort)(_dReg.lo)); }
            set { _dReg.hi = (byte)(value / 256); _dReg.lo = (byte)(value % 256); }
        }
        public object buildingDebugLineLock = new object();
        private bool _buildingDebugLine;
        public bool BuildingDebugLine
        {
            get { return _buildingDebugLine; }
            set { _buildingDebugLine = value; }
        }

        private bool breakpointsEnabled = false;
        public bool BreakpointsEnabled
        {
            get { return breakpointsEnabled; }
            set { breakpointsEnabled = value; }
        }

        private bool _excludeExcludeTraceRangeEnabled = false;
        public bool ExcludeExcludeTraceRangeEnabled
        {
            get { return _excludeExcludeTraceRangeEnabled; }
            set { _excludeExcludeTraceRangeEnabled = value; }
        }

        private List<ushort> traceExclusionAddress = new List<ushort>();
        public List<ushort> TraceExclusionAddress
        {
            get { return traceExclusionAddress; }
            set { traceExclusionAddress = value; }
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

        private bool _excludeSingleStepEnabled = false;
        public bool ExcludeSingleStepEnabled
        {
            get { return _excludeSingleStepEnabled; }
            set { _excludeSingleStepEnabled = value; }
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

        public virtual void ForceSingleStep ()
        {

        }
        public static int traceSize = 65536;
        public static bool traceHasWrapped;

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

        public virtual void DumpTraceBuffer()
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
