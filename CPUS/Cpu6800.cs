using System;
using System.Text;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;

namespace Memulator
{
    class Cpu6800 : Cpu
    {
        public bool allow00NOP = false;

        public Cpu6800()
        {
            processorType = 6800;
            traceHasWrapped = false;
            for (int i = 0; i < traceSize; i++)
            {
                clsTraceBuffer[i] = new TraceEntry();
            }

            allow00NOP = Program.GetConfigurationAttribute("Global/ProcessorBoard", "Allow00NOP", 0) == 0 ? false : true;
            BreakpointsEnabled = Program.GetConfigurationAttribute("Global/DebugInfo/BreakPoints", "enabled"   , 0) == 1 ? true : false;
            TraceEnabled        = Program.GetConfigurationAttribute("Global/Trace"                , "Enabled"   , 0) == 0 ? false : true;
        }

        public enum AddressingModes
        {
            AM_ILLEGAL       = 0x0080,  // Invalid Op Code

            AM_DIRECT_6800   = 0x0040,  // direct addressing
            AM_RELATIVE_6800 = 0x0020,  // relative addressing
            AM_EXTENDED_6800 = 0x0010,  // extended addressing allowed
            AM_IMM16_6800    = 0x0008,  // 16 bit immediate
            AM_IMM8_6800     = 0x0004,  // 8 bit immediate
            AM_INDEXED_6800  = 0x0002,  // indexed addressing allowed
            AM_INHERENT_6800 = 0x0001   // inherent addressing
        }

        private byte CCR_HALFCARRY    = 0x20;
        private byte CCR_INTERRUPT    = 0x10;
        private byte CCR_NEGATIVE     = 0x08;
        private byte CCR_ZERO         = 0x04;
        private byte CCR_OVERFLOW     = 0x02;
        private byte CCR_CARRY = 0x01;

        public enum ExecutionStates
        {
            GETOPCODE        = 0,
            GETOPERAND       = 1,
            EXECOPCODE       = 2
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct _6800_OPCTABLEENTRY             
        {
            [MarshalAsAttribute(UnmanagedType.LPWStr)]
            public string mneumonic;
            [MarshalAsAttribute(UnmanagedType.U1)]
            public byte opval;
            [MarshalAsAttribute(UnmanagedType.U2)]
            public AddressingModes attribute;
            [MarshalAsAttribute(UnmanagedType.U4)]
            public int numbytes;
            [MarshalAsAttribute(UnmanagedType.LPArray)]
            public int [] ccr_rules;
            [MarshalAsAttribute(UnmanagedType.U4)]
            public int cycles;
            [MarshalAsAttribute(UnmanagedType.U8)]
            public long lCount;
        };

        _6800_OPCTABLEENTRY [] opctbl = new _6800_OPCTABLEENTRY [256];

        public _6800_OPCTABLEENTRY[] Opctbl { get => opctbl; set => opctbl = value; }

        private void SetOpCodeTableEntry(int index, string mneunonic, byte OpCode, AddressingModes AddrMode, int numbytes, int cycles, int H, int I, int N, int Z, int V, int C, int lcount)
        {
            opctbl[index].mneumonic     = mneunonic;
            opctbl[index].opval         = OpCode;
            opctbl[index].attribute     = AddrMode;
            opctbl[index].numbytes      = numbytes;
            opctbl[index].cycles        = cycles;
            opctbl[index].ccr_rules[0]  = H;
            opctbl[index].ccr_rules[1]  = I;
            opctbl[index].ccr_rules[2]  = N;
            opctbl[index].ccr_rules[3]  = Z;
            opctbl[index].ccr_rules[4]  = V;
            opctbl[index].ccr_rules[5]  = C;
            opctbl[index].lCount        = lcount;
        }
        private void BuildOpCodeTable()
        {
            for (int i = 0; i < opctbl.Length; i++)
            {
                opctbl[i].ccr_rules = new int[6];
            }
            if (allow00NOP)
                SetOpCodeTableEntry( 0, "NOP  ", 0x00, AddressingModes.AM_INHERENT_6800,   1,  2,   0,  0,  0,  0,  0,  0, 0);
            else
            	SetOpCodeTableEntry( 0, "~~~~~", 0x00, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry(  1, "NOP  ",    0x01, AddressingModes.AM_INHERENT_6800,   1,  2,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry(  2, "~~~~~",    0x02, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry(  3, "~~~~~",    0x03, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry(  4, "~~~~~",    0x04, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry(  5, "~~~~~",    0x05, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry(  6, "TAP  ",    0x06, AddressingModes.AM_INHERENT_6800,   1,  2,  12, 12, 12, 12, 12, 12, 0);
            SetOpCodeTableEntry(  7, "TPA  ",    0x07, AddressingModes.AM_INHERENT_6800,   1,  2,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry(  8, "INX  ",    0x08, AddressingModes.AM_INHERENT_6800,   1,  4,   0,  0,  0, 15,  0,  0, 0);
            SetOpCodeTableEntry(  9, "DEX  ",    0x09, AddressingModes.AM_INHERENT_6800,   1,  4,   0,  0,  0, 15,  0,  0, 0);
            SetOpCodeTableEntry( 10, "CLV  ",    0x0A, AddressingModes.AM_INHERENT_6800,   1,  2,   0,  0,  0,  0, 14,  0, 0);
            SetOpCodeTableEntry( 11, "SEV  ",    0x0B, AddressingModes.AM_INHERENT_6800,   1,  2,   0,  0,  0,  0, 13,  0, 0);
            SetOpCodeTableEntry( 12, "CLC  ",    0x0C, AddressingModes.AM_INHERENT_6800,   1,  2,   0,  0,  0,  0,  0, 14, 0);
            SetOpCodeTableEntry( 13, "SEC  ",    0x0D, AddressingModes.AM_INHERENT_6800,   1,  2,   0,  0,  0,  0,  0, 13, 0);
            SetOpCodeTableEntry( 14, "CLI  ",    0x0E, AddressingModes.AM_INHERENT_6800,   1,  2,   0, 14,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 15, "SEI  ",    0x0F, AddressingModes.AM_INHERENT_6800,   1,  2,   0, 13,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 16, "SBA  ",    0x10, AddressingModes.AM_INHERENT_6800,   1,  2,   0,  0, 15, 15, 15, 15, 0);
            SetOpCodeTableEntry( 17, "CBA  ",    0x11, AddressingModes.AM_INHERENT_6800,   1,  2,   0,  0, 15, 15, 15, 15, 0);
            SetOpCodeTableEntry( 18, "~~~~~",    0x12, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 19, "~~~~~",    0x13, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 20, "~~~~~",    0x14, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 21, "~~~~~",    0x15, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 22, "TAB  ",    0x16, AddressingModes.AM_INHERENT_6800,   1,  2,   0,  0, 15, 15, 14,  0, 0);
            SetOpCodeTableEntry( 23, "TBA  ",    0x17, AddressingModes.AM_INHERENT_6800,   1,  2,   0,  0, 15, 15, 14,  0, 0);
            SetOpCodeTableEntry( 24, "~~~~~",    0x18, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 25, "DAA  ",    0x19, AddressingModes.AM_INHERENT_6800,   1,  2,   0,  0, 15, 15, 15,  3, 0);
            SetOpCodeTableEntry( 26, "~~~~~",    0x1A, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 27, "ABA  ",    0x1B, AddressingModes.AM_INHERENT_6800,   1,  2,  15,  0, 15, 15, 15, 15, 0);
            SetOpCodeTableEntry( 28, "~~~~~",    0x1C, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 29, "~~~~~",    0x1D, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 30, "~~~~~",    0x1E, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 31, "~~~~~",    0x1F, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 32, "BRA  ",    0x20, AddressingModes.AM_RELATIVE_6800,   2,  4,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 33, "~~~~~",    0x21, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 34, "BHI  ",    0x22, AddressingModes.AM_RELATIVE_6800,   2,  4,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 35, "BLS  ",    0x23, AddressingModes.AM_RELATIVE_6800,   2,  4,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 36, "BCC  ",    0x24, AddressingModes.AM_RELATIVE_6800,   2,  4,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 37, "BCS  ",    0x25, AddressingModes.AM_RELATIVE_6800,   2,  4,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 38, "BNE  ",    0x26, AddressingModes.AM_RELATIVE_6800,   2,  4,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 39, "BEQ  ",    0x27, AddressingModes.AM_RELATIVE_6800,   2,  4,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 40, "BVC  ",    0x28, AddressingModes.AM_RELATIVE_6800,   2,  4,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 41, "BVS  ",    0x29, AddressingModes.AM_RELATIVE_6800,   2,  4,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 42, "BPL  ",    0x2A, AddressingModes.AM_RELATIVE_6800,   2,  4,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 43, "BMI  ",    0x2B, AddressingModes.AM_RELATIVE_6800,   2,  4,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 44, "BGE  ",    0x2C, AddressingModes.AM_RELATIVE_6800,   2,  4,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 45, "BLT  ",    0x2D, AddressingModes.AM_RELATIVE_6800,   2,  4,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 46, "BGT  ",    0x2E, AddressingModes.AM_RELATIVE_6800,   2,  4,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 47, "BLE  ",    0x2F, AddressingModes.AM_RELATIVE_6800,   2,  4,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 48, "TSX  ",    0x30, AddressingModes.AM_INHERENT_6800,   1,  4,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 49, "INS  ",    0x31, AddressingModes.AM_INHERENT_6800,   1,  4,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 50, "PUL A",    0x32, AddressingModes.AM_INHERENT_6800,   1,  4,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 51, "PUL B",    0x33, AddressingModes.AM_INHERENT_6800,   1,  4,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 52, "DES  ",    0x34, AddressingModes.AM_INHERENT_6800,   1,  4,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 53, "TXS  ",    0x35, AddressingModes.AM_INHERENT_6800,   1,  4,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 54, "PSH A",    0x36, AddressingModes.AM_INHERENT_6800,   1,  4,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 55, "PSH B",    0x37, AddressingModes.AM_INHERENT_6800,   1,  4,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 56, "~~~~~",    0x38, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 57, "RTS  ",    0x39, AddressingModes.AM_INHERENT_6800,   1,  5,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 58, "~~~~~",    0x3A, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 59, "RTI  ",    0x3B, AddressingModes.AM_INHERENT_6800,   1,  10, 10, 10, 10, 10, 10, 10, 0);
            SetOpCodeTableEntry( 60, "~~~~~",    0x3C, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 61, "~~~~~",    0x3D, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 62, "WAI  ",    0x3E, AddressingModes.AM_INHERENT_6800,   1,  9,   0, 11,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 63, "SWI  ",    0x3F, AddressingModes.AM_INHERENT_6800,   1,  12,  0, 13,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 64, "NEG A",    0x40, AddressingModes.AM_INHERENT_6800,   1,  2,   0,  0, 15, 15,  1,  2, 0);
            SetOpCodeTableEntry( 65, "~~~~~",    0x41, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 66, "~~~~~",    0x42, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 67, "COM A",    0x43, AddressingModes.AM_INHERENT_6800,   1,  2,   0,  0, 15, 15, 14, 13, 0);
            SetOpCodeTableEntry( 68, "LSR A",    0x44, AddressingModes.AM_INHERENT_6800,   1,  2,   0,  0, 14, 15,  6, 15, 0);
            SetOpCodeTableEntry( 69, "~~~~~",    0x45, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 70, "ROR A",    0x46, AddressingModes.AM_INHERENT_6800,   1,  2,   0,  0, 15, 15,  6, 15, 0);
            SetOpCodeTableEntry( 71, "ASR A",    0x47, AddressingModes.AM_INHERENT_6800,   1,  2,   0,  0, 15, 15,  6, 15, 0);
            SetOpCodeTableEntry( 72, "ASL A",    0x48, AddressingModes.AM_INHERENT_6800,   1,  2,   0,  0, 15, 15,  6, 15, 0);
            SetOpCodeTableEntry( 73, "ROL A",    0x49, AddressingModes.AM_INHERENT_6800,   1,  2,   0,  0, 15, 15,  6, 15, 0);
            SetOpCodeTableEntry( 74, "DEC A",    0x4A, AddressingModes.AM_INHERENT_6800,   1,  2,   0,  0, 15, 15,  4,  0, 0);
            SetOpCodeTableEntry( 75, "~~~~~",    0x4B, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 76, "INC A",    0x4C, AddressingModes.AM_INHERENT_6800,   1,  2,   0,  0, 15, 15,  5,  0, 0);
            SetOpCodeTableEntry( 77, "TST A",    0x4D, AddressingModes.AM_INHERENT_6800,   1,  2,   0,  0, 15, 15, 14, 14, 0);
            SetOpCodeTableEntry( 78, "~~~~~",    0x4E, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 79, "CLR A",    0x4F, AddressingModes.AM_INHERENT_6800,   1,  2,   0,  0, 14, 13, 14, 14, 0);
            SetOpCodeTableEntry( 80, "NEG B",    0x50, AddressingModes.AM_INHERENT_6800,   1,  2,   0,  0, 15, 15,  1,  2, 0);
            SetOpCodeTableEntry( 81, "~~~~~",    0x51, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 82, "~~~~~",    0x52, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 83, "COM B",    0x53, AddressingModes.AM_INHERENT_6800,   1,  2,   0,  0, 15, 15, 14, 13, 0);
            SetOpCodeTableEntry( 84, "LSR B",    0x54, AddressingModes.AM_INHERENT_6800,   1,  2,   0,  0, 14, 15,  6, 15, 0);
            SetOpCodeTableEntry( 85, "~~~~~",    0x55, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 86, "ROR B",    0x56, AddressingModes.AM_INHERENT_6800,   1,  2,   0,  0, 15, 15,  6, 15, 0);
            SetOpCodeTableEntry( 87, "ASR B",    0x57, AddressingModes.AM_INHERENT_6800,   1,  2,   0,  0, 15, 15,  6, 15, 0);
            SetOpCodeTableEntry( 88, "ASL B",    0x58, AddressingModes.AM_INHERENT_6800,   1,  2,   0,  0, 15, 15,  6, 15, 0);
            SetOpCodeTableEntry( 89, "ROL B",    0x59, AddressingModes.AM_INHERENT_6800,   1,  2,   0,  0, 15, 15,  6, 15, 0);
            SetOpCodeTableEntry( 90, "DEC B",    0x5A, AddressingModes.AM_INHERENT_6800,   1,  2,   0,  0, 15, 15,  4,  0, 0);
            SetOpCodeTableEntry( 91, "~~~~~",    0x5B, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 92, "INC B",    0x5C, AddressingModes.AM_INHERENT_6800,   1,  2,   0,  0, 15, 15,  5,  0, 0);
            SetOpCodeTableEntry( 93, "TST B",    0x5D, AddressingModes.AM_INHERENT_6800,   1,  2,   0,  0, 15, 15, 14, 14, 0);
            SetOpCodeTableEntry( 94, "~~~~~",    0x5E, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 95, "CLR B",    0x5F, AddressingModes.AM_INHERENT_6800,   1,  2,   0,  0, 14, 13, 14, 14, 0);
            SetOpCodeTableEntry( 96, "NEG  ",    0x60, AddressingModes.AM_INDEXED_6800,    2,  7,   0,  0, 15, 15,  1,  2, 0);
            SetOpCodeTableEntry( 97, "~~~~~",    0x61, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 98, "~~~~~",    0x62, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry( 99, "COM  ",    0x63, AddressingModes.AM_INDEXED_6800,    2,  7,   0,  0, 15, 15, 14, 13, 0);
            SetOpCodeTableEntry(100, "LSR  ",    0x64, AddressingModes.AM_INDEXED_6800,    2,  7,   0,  0, 14, 15,  6, 15, 0);
            SetOpCodeTableEntry(101, "~~~~~",    0x65, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry(102, "ROR  ",    0x66, AddressingModes.AM_INDEXED_6800,    2,  7,   0,  0, 15, 15,  6, 15, 0);
            SetOpCodeTableEntry(103, "ASR  ",    0x67, AddressingModes.AM_INDEXED_6800,    2,  7,   0,  0, 15, 15,  6, 15, 0);
            SetOpCodeTableEntry(104, "ASL  ",    0x68, AddressingModes.AM_INDEXED_6800,    2,  7,   0,  0, 15, 15,  6, 15, 0);
            SetOpCodeTableEntry(105, "ROL  ",    0x69, AddressingModes.AM_INDEXED_6800,    2,  7,   0,  0, 15, 15,  6, 15, 0);
            SetOpCodeTableEntry(106, "DEC  ",    0x6A, AddressingModes.AM_INDEXED_6800,    2,  7,   0,  0, 15, 15,  4,  0, 0);
            SetOpCodeTableEntry(107, "~~~~~",    0x6B, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry(108, "INC  ",    0x6C, AddressingModes.AM_INDEXED_6800,    2,  7,   0,  0, 15, 15,  5,  0, 0);
            SetOpCodeTableEntry(109, "TST  ",    0x6D, AddressingModes.AM_INDEXED_6800,    2,  7,   0,  0, 15, 15, 14, 14, 0);
            SetOpCodeTableEntry(110, "JMP  ",    0x6E, AddressingModes.AM_INDEXED_6800,    2,  4,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry(111, "CLR  ",    0x6F, AddressingModes.AM_INDEXED_6800,    2,  7,   0,  0, 14, 13, 14, 14, 0);
            SetOpCodeTableEntry(112, "NEG  ",    0x70, AddressingModes.AM_EXTENDED_6800,   3,  6,   0,  0, 15, 15,  1,  2, 0);
            SetOpCodeTableEntry(113, "~~~~~",    0x71, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry(114, "~~~~~",    0x72, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry(115, "COM  ",    0x73, AddressingModes.AM_EXTENDED_6800,   3,  6,   0,  0, 15, 15, 14, 13, 0);
            SetOpCodeTableEntry(116, "LSR  ",    0x74, AddressingModes.AM_EXTENDED_6800,   3,  6,   0,  0, 14, 15,  6, 15, 0);
            SetOpCodeTableEntry(117, "~~~~~",    0x75, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry(118, "ROR  ",    0x76, AddressingModes.AM_EXTENDED_6800,   3,  6,   0,  0, 15, 15,  6, 15, 0);
            SetOpCodeTableEntry(119, "ASR  ",    0x77, AddressingModes.AM_EXTENDED_6800,   3,  6,   0,  0, 15, 15,  6, 15, 0);
            SetOpCodeTableEntry(120, "ASL  ",    0x78, AddressingModes.AM_EXTENDED_6800,   3,  6,   0,  0, 15, 15,  6, 15, 0);
            SetOpCodeTableEntry(121, "ROL  ",    0x79, AddressingModes.AM_EXTENDED_6800,   3,  6,   0,  0, 15, 15,  6, 15, 0);
            SetOpCodeTableEntry(122, "DEC  ",    0x7A, AddressingModes.AM_EXTENDED_6800,   3,  6,   0,  0, 15, 15,  4,  0, 0);
            SetOpCodeTableEntry(123, "~~~~~",    0x7B, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry(124, "INC  ",    0x7C, AddressingModes.AM_EXTENDED_6800,   3,  6,   0,  0, 15, 15,  5,  0, 0);
            SetOpCodeTableEntry(125, "TST  ",    0x7D, AddressingModes.AM_EXTENDED_6800,   3,  6,   0,  0, 15, 15, 14, 14, 0);
            SetOpCodeTableEntry(126, "JMP  ",    0x7E, AddressingModes.AM_EXTENDED_6800,   3,  3,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry(127, "CLR  ",    0x7F, AddressingModes.AM_EXTENDED_6800,   3,  6,   0,  0, 14, 13, 14, 14, 0);
            SetOpCodeTableEntry(128, "SUB A",    0x80, AddressingModes.AM_IMM8_6800,       2,  2,   0,  0, 15, 15, 15, 15, 0);
            SetOpCodeTableEntry(129, "CMP A",    0x81, AddressingModes.AM_IMM8_6800,       2,  2,   0,  0, 15, 15, 15, 15, 0);
            SetOpCodeTableEntry(130, "SBC A",    0x82, AddressingModes.AM_IMM8_6800,       2,  2,   0,  0, 15, 15, 15, 15, 0);
            SetOpCodeTableEntry(131, "~~~~~",    0x83, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry(132, "AND A",    0x84, AddressingModes.AM_IMM8_6800,       2,  2,   0,  0, 15, 15, 14,  0, 0);
            SetOpCodeTableEntry(133, "BIT A",    0x85, AddressingModes.AM_IMM8_6800,       2,  2,   0,  0, 15, 15, 14,  0, 0);
            SetOpCodeTableEntry(134, "LDA A",    0x86, AddressingModes.AM_IMM8_6800,       2,  2,   0,  0, 15, 15, 14,  0, 0);
            SetOpCodeTableEntry(135, "~~~~~",    0x87, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry(136,  "EOR A",    0x88,AddressingModes. AM_IMM8_6800,      2,  2,   0,  0, 15, 15, 14,  0, 0);
            SetOpCodeTableEntry(137,  "ADC A",    0x89,AddressingModes. AM_IMM8_6800,      2,  2,  15,  0, 15, 15, 15, 15, 0);
            SetOpCodeTableEntry(138,  "ORA A",    0x8A,AddressingModes. AM_IMM8_6800,      2,  2,   0,  0, 15, 15, 14,  0, 0);
            SetOpCodeTableEntry(139,  "ADD A",    0x8B,AddressingModes. AM_IMM8_6800,      2,  2,  15,  0, 15, 15, 15, 15, 0);
            SetOpCodeTableEntry(140,  "CPX  ",    0x8C,AddressingModes. AM_IMM16_6800,     3,  3,   0,  0,  7, 15,  8,  0, 0);
            SetOpCodeTableEntry(141,  "BSR  ",    0x8D,AddressingModes. AM_RELATIVE_6800,  2,  6,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry(142, "LDS  ",    0x8E, AddressingModes.AM_IMM16_6800,      3,  3,   0,  0,  9, 15, 14,  0, 0);
            SetOpCodeTableEntry(143, "~~~~~",    0x8F, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry(144, "SUB A",    0x90, AddressingModes.AM_DIRECT_6800,     2,  3,   0,  0, 15, 15, 15, 15, 0);
            SetOpCodeTableEntry(145, "CMP A",    0x91, AddressingModes.AM_DIRECT_6800,     2,  3,   0,  0, 15, 15, 15, 15, 0);
            SetOpCodeTableEntry(146, "SBC A",    0x92, AddressingModes.AM_DIRECT_6800,     2,  3,   0,  0, 15, 15, 15, 15, 0);
            SetOpCodeTableEntry(147, "~~~~~",    0x93, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry(148, "AND A",    0x94, AddressingModes.AM_DIRECT_6800,     2,  3,   0,  0, 15, 15, 14,  0, 0);
            SetOpCodeTableEntry(149, "BIT A",    0x95, AddressingModes.AM_DIRECT_6800,     2,  3,   0,  0, 15, 15, 14,  0, 0);
            SetOpCodeTableEntry(150, "LDA A",    0x96, AddressingModes.AM_DIRECT_6800,     2,  3,   0,  0, 15, 15, 14,  0, 0);
            SetOpCodeTableEntry(151, "STA A",    0x97, AddressingModes.AM_DIRECT_6800,     2,  4,   0,  0, 15, 15, 14,  0, 0);
            SetOpCodeTableEntry(152, "EOR A",    0x98, AddressingModes.AM_DIRECT_6800,     2,  3,   0,  0, 15, 15, 14,  0, 0);
            SetOpCodeTableEntry(153, "ADC A",    0x99, AddressingModes.AM_DIRECT_6800,     2,  3,  15,  0, 15, 15, 15, 15, 0);
            SetOpCodeTableEntry(154, "ORA A",    0x9A, AddressingModes.AM_DIRECT_6800,     2,  3,   0,  0, 15, 15, 14,  0, 0);
            SetOpCodeTableEntry(155, "ADD A",    0x9B, AddressingModes.AM_DIRECT_6800,     2,  3,  15,  0, 15, 15, 15, 15, 0);
            SetOpCodeTableEntry(156, "CPX  ",    0x9C, AddressingModes.AM_DIRECT_6800,     2,  4,   0,  0,  7, 15,  8,  0, 0);
            SetOpCodeTableEntry(157, "~~~~~",    0x9D, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry(158, "LDS  ",    0x9E, AddressingModes.AM_DIRECT_6800,     2,  4,   0,  0,  9, 15, 14,  0, 0);
            SetOpCodeTableEntry(159, "STS  ",    0x9F, AddressingModes.AM_DIRECT_6800,     2,  5,   0,  0,  9, 15, 14,  0, 0);
            SetOpCodeTableEntry(160, "SUB A",    0xA0, AddressingModes.AM_INDEXED_6800,    2,  5,   0,  0, 15, 15, 15, 15, 0);
            SetOpCodeTableEntry(161, "CMP A",    0xA1, AddressingModes.AM_INDEXED_6800,    2,  5,   0,  0, 15, 15, 15, 15, 0);
            SetOpCodeTableEntry(162, "SBC A",    0xA2, AddressingModes.AM_INDEXED_6800,    2,  5,   0,  0, 15, 15, 15, 15, 0);
            SetOpCodeTableEntry(163, "~~~~~",    0xA3, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry(164, "AND A",    0xA4, AddressingModes.AM_INDEXED_6800,    2,  5,   0,  0, 15, 15, 14,  0, 0);
            SetOpCodeTableEntry(165, "BIT A",    0xA5, AddressingModes.AM_INDEXED_6800,    2,  5,   0,  0, 15, 15, 14,  0, 0);
            SetOpCodeTableEntry(166, "LDA A",    0xA6, AddressingModes.AM_INDEXED_6800,    2,  5,   0,  0, 15, 15, 14,  0, 0);
            SetOpCodeTableEntry(167, "STA A",    0xA7, AddressingModes.AM_INDEXED_6800,    2,  5,   0,  0, 15, 15, 14,  0, 0);
            SetOpCodeTableEntry(168, "EOR A",    0xA8, AddressingModes.AM_INDEXED_6800,    2,  5,   0,  0, 15, 15, 14,  0, 0);
            SetOpCodeTableEntry(169, "ADC A",    0xA9, AddressingModes.AM_INDEXED_6800,    2,  5,  15,  0, 15, 15, 15, 15, 0);
            SetOpCodeTableEntry(170, "ORA A",    0xAA, AddressingModes.AM_INDEXED_6800,    2,  5,   0,  0, 15, 15, 14,  0, 0);
            SetOpCodeTableEntry(171, "ADD A",    0xAB, AddressingModes.AM_INDEXED_6800,    2,  5,  15,  0, 15, 15, 15, 15, 0);
            SetOpCodeTableEntry(172, "CPX  ",    0xAC, AddressingModes.AM_INDEXED_6800,    2,  6,   0,  0,  7, 15,  8,  0, 0);
            SetOpCodeTableEntry(173, "JSR  ",    0xAD, AddressingModes.AM_INDEXED_6800,    2,  8,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry(174, "LDS  ",    0xAE, AddressingModes.AM_INDEXED_6800,    2,  6,   0,  0,  9, 15, 14,  0, 0);
            SetOpCodeTableEntry(175, "STS  ",    0xAF, AddressingModes.AM_INDEXED_6800,    2,  7,   0,  0,  9, 15, 14,  0, 0);
            SetOpCodeTableEntry(176, "SUB A",    0xB0, AddressingModes.AM_EXTENDED_6800,   3,  4,   0,  0, 15, 15, 15, 15, 0);
            SetOpCodeTableEntry(177, "CMP A",    0xB1, AddressingModes.AM_EXTENDED_6800,   3,  4,   0,  0, 15, 15, 15, 15, 0);
            SetOpCodeTableEntry(178, "SBC A",    0xB2, AddressingModes.AM_EXTENDED_6800,   3,  4,   0,  0, 15, 15, 15, 15, 0);
            SetOpCodeTableEntry(179, "~~~~~",    0xB3, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry(180, "AND A",    0xB4, AddressingModes.AM_EXTENDED_6800,   3,  4,   0,  0, 15, 15, 14,  0, 0);
            SetOpCodeTableEntry(181, "BIT A",    0xB5, AddressingModes.AM_EXTENDED_6800,   3,  4,   0,  0, 15, 15, 14,  0, 0);
            SetOpCodeTableEntry(182, "LDA A",    0xB6, AddressingModes.AM_EXTENDED_6800,   3,  4,   0,  0, 15, 15, 14,  0, 0);
            SetOpCodeTableEntry(183, "STA A",    0xB7, AddressingModes.AM_EXTENDED_6800,   3,  5,   0,  0, 15, 15, 14,  0, 0);
            SetOpCodeTableEntry(184, "EOR A",    0xB8, AddressingModes.AM_EXTENDED_6800,   3,  4,   0,  0, 15, 15, 14,  0, 0);
            SetOpCodeTableEntry(185, "ADC A",    0xB9, AddressingModes.AM_EXTENDED_6800,   3,  4,  15,  0, 15, 15, 15, 15, 0);
            SetOpCodeTableEntry(186, "ORA A",    0xBA, AddressingModes.AM_EXTENDED_6800,   3,  4,   0,  0, 15, 15, 14,  0, 0);
            SetOpCodeTableEntry(187, "ADD A",    0xBB, AddressingModes.AM_EXTENDED_6800,   3,  4,  15,  0, 15, 15, 15, 15, 0);
            SetOpCodeTableEntry(188, "CPX  ",    0xBC, AddressingModes.AM_EXTENDED_6800,   3,  5,   0,  0,  7, 15,  8,  0, 0);
            SetOpCodeTableEntry(189, "JSR  ",    0xBD, AddressingModes.AM_EXTENDED_6800,   3,  9,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry(190, "LDS  ",    0xBE, AddressingModes.AM_EXTENDED_6800,   3,  5,   0,  0,  9, 15, 14,  0, 0);
            SetOpCodeTableEntry(191, "STS  ",    0xBF, AddressingModes.AM_EXTENDED_6800,   3,  6,   0,  0,  9, 15, 14,  0, 0);
            SetOpCodeTableEntry(192, "SUB B",    0xC0, AddressingModes.AM_IMM8_6800,       2,  2,   0,  0, 15, 15, 15, 15, 0);
            SetOpCodeTableEntry(193, "CMP B",    0xC1, AddressingModes.AM_IMM8_6800,       2,  2,   0,  0, 15, 15, 15, 15, 0);
            SetOpCodeTableEntry(194, "SBC B",    0xC2, AddressingModes.AM_IMM8_6800,       2,  2,   0,  0, 15, 15, 15, 15, 0);
            SetOpCodeTableEntry(195, "~~~~~",    0xC3, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry(196, "AND B",    0xC4, AddressingModes.AM_IMM8_6800,       2,  2,   0,  0, 15, 15, 14,  0, 0);
            SetOpCodeTableEntry(197, "BIT B",    0xC5, AddressingModes.AM_IMM8_6800,       2,  2,   0,  0, 15, 15, 14,  0, 0);
            SetOpCodeTableEntry(198, "LDA B",    0xC6, AddressingModes.AM_IMM8_6800,       2,  2,   0,  0, 15, 15, 14,  0, 0);
            SetOpCodeTableEntry(199, "~~~~~",    0xC7, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry(200, "EOR B",    0xC8, AddressingModes.AM_IMM8_6800,       2,  2,   0,  0, 15, 15, 14,  0, 0);
            SetOpCodeTableEntry(201, "ADC B",    0xC9, AddressingModes.AM_IMM8_6800,       2,  2,  15,  0, 15, 15, 15, 15, 0);
            SetOpCodeTableEntry(202, "ORA B",    0xCA, AddressingModes.AM_IMM8_6800,       2,  2,   0,  0, 15, 15, 14,  0, 0);
            SetOpCodeTableEntry(203, "ADD B",    0xCB, AddressingModes.AM_IMM8_6800,       2,  2,  15,  0, 15, 15, 15, 15, 0);
            SetOpCodeTableEntry(204, "~~~~~",    0xCC, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry(205, "~~~~~",    0xCD, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry(206, "LDX  ",    0xCE, AddressingModes.AM_IMM16_6800,      3,  3,   0,  0,  9, 15, 14,  0, 0);
            SetOpCodeTableEntry(207, "~~~~~",    0xCF, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry(208, "SUB B",    0xD0, AddressingModes.AM_DIRECT_6800,     2,  3,   0,  0, 15, 15, 15, 15, 0);
            SetOpCodeTableEntry(209, "CMP B",    0xD1, AddressingModes.AM_DIRECT_6800,     2,  3,   0,  0, 15, 15, 15, 15, 0);
            SetOpCodeTableEntry(210, "SBC B",    0xD2, AddressingModes.AM_DIRECT_6800,     2,  3,   0,  0, 15, 15, 15, 15, 0);
            SetOpCodeTableEntry(211, "~~~~~",    0xD3, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry(212, "AND B",    0xD4, AddressingModes.AM_DIRECT_6800,     2,  3,   0,  0, 15, 15, 14,  0, 0);
            SetOpCodeTableEntry(213, "BIT B",    0xD5, AddressingModes.AM_DIRECT_6800,     2,  3,   0,  0, 15, 15, 14,  0, 0);
            SetOpCodeTableEntry(214, "LDA B",    0xD6, AddressingModes.AM_DIRECT_6800,     2,  4,   0,  0, 15, 15, 14,  0, 0);
            SetOpCodeTableEntry(215, "STA B",    0xD7, AddressingModes.AM_DIRECT_6800,     2,  3,   0,  0, 15, 15, 14,  0, 0);
            SetOpCodeTableEntry(216, "EOR B",    0xD8, AddressingModes.AM_DIRECT_6800,     2,  3,   0,  0, 15, 15, 14,  0, 0);
            SetOpCodeTableEntry(217, "ADC B",    0xD9, AddressingModes.AM_DIRECT_6800,     2,  3,  15,  0, 15, 15, 15, 15, 0);
            SetOpCodeTableEntry(218, "ORA B",    0xDA, AddressingModes.AM_DIRECT_6800,     2,  3,   0,  0, 15, 15, 14,  0, 0);
            SetOpCodeTableEntry(219, "ADD B",    0xDB, AddressingModes.AM_DIRECT_6800,     2,  3,  15,  0, 15, 15, 15, 15, 0);
            SetOpCodeTableEntry(220, "~~~~~",    0xDC, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry(221, "~~~~~",    0xDD, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry(222, "LDX  ",    0xDE, AddressingModes.AM_DIRECT_6800,     2,  4,   0,  0,  9, 15, 14,  0, 0);
            SetOpCodeTableEntry(223, "STX  ",    0xDF, AddressingModes.AM_DIRECT_6800,     2,  5,   0,  0,  9, 15, 14,  0, 0);
            SetOpCodeTableEntry(224, "SUB B",    0xE0, AddressingModes.AM_INDEXED_6800,    2,  5,   0,  0, 15, 15, 15, 15, 0);
            SetOpCodeTableEntry(225, "CMP B",    0xE1, AddressingModes.AM_INDEXED_6800,    2,  5,   0,  0, 15, 15, 15, 15, 0);
            SetOpCodeTableEntry(226, "SBC B",    0xE2, AddressingModes.AM_INDEXED_6800,    2,  5,   0,  0, 15, 15, 15, 15, 0);
            SetOpCodeTableEntry(227, "~~~~~",    0xE3, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry(228, "AND B",    0xE4, AddressingModes.AM_INDEXED_6800,    2,  5,   0,  0, 15, 15, 14,  0, 0);
            SetOpCodeTableEntry(229, "BIT B",    0xE5, AddressingModes.AM_INDEXED_6800,    2,  5,   0,  0, 15, 15, 14,  0, 0);
            SetOpCodeTableEntry(230, "LDA B",    0xE6, AddressingModes.AM_INDEXED_6800,    2,  5,   0,  0, 15, 15, 14,  0, 0);
            SetOpCodeTableEntry(231, "STA B",    0xE7, AddressingModes.AM_INDEXED_6800,    2,  6,   0,  0, 15, 15, 14,  0, 0);
            SetOpCodeTableEntry(232, "EOR B",    0xE8, AddressingModes.AM_INDEXED_6800,    2,  5,   0,  0, 15, 15, 14,  0, 0);
            SetOpCodeTableEntry(233, "ADC B",    0xE9, AddressingModes.AM_INDEXED_6800,    2,  5,  15,  0, 15, 15, 15, 15, 0);
            SetOpCodeTableEntry(234, "ORA B",    0xEA, AddressingModes.AM_INDEXED_6800,    2,  5,   0,  0, 15, 15, 14,  0, 0);
            SetOpCodeTableEntry(235, "ADD B",    0xEB, AddressingModes.AM_INDEXED_6800,    2,  5,  15,  0, 15, 15, 15, 15, 0);
            SetOpCodeTableEntry(236, "~~~~~",    0xEC, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry(237, "~~~~~",    0xED, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry(238, "LDX  ",    0xEE, AddressingModes.AM_INDEXED_6800,    2,  6,   0,  0,  9, 15, 14,  0, 0);
            SetOpCodeTableEntry(239, "STX  ",    0xEF, AddressingModes.AM_INDEXED_6800,    2,  7,   0,  0,  9, 15, 14,  0, 0);
            SetOpCodeTableEntry(240, "SUB B",    0xF0, AddressingModes.AM_EXTENDED_6800,   3,  4,   0,  0, 15, 15, 15, 15, 0);
            SetOpCodeTableEntry(241, "CMP B",    0xF1, AddressingModes.AM_EXTENDED_6800,   3,  4,   0,  0, 15, 15, 15, 15, 0);
            SetOpCodeTableEntry(242, "SBC B",    0xF2, AddressingModes.AM_EXTENDED_6800,   3,  4,   0,  0, 15, 15, 15, 15, 0);
            SetOpCodeTableEntry(243, "~~~~~",    0xF3, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry(244, "AND B",    0xF4, AddressingModes.AM_EXTENDED_6800,   3,  4,   0,  0, 15, 15, 14,  0, 0);
            SetOpCodeTableEntry(245, "BIT B",    0xF5, AddressingModes.AM_EXTENDED_6800,   3,  4,   0,  0, 15, 15, 14,  0, 0);
            SetOpCodeTableEntry(246, "LDA B",    0xF6, AddressingModes.AM_EXTENDED_6800,   3,  4,   0,  0, 15, 15, 14,  0, 0);
            SetOpCodeTableEntry(247, "STA B",    0xF7, AddressingModes.AM_EXTENDED_6800,   3,  5,   0,  0, 15, 15, 14,  0, 0);
            SetOpCodeTableEntry(248, "EOR B",    0xF8, AddressingModes.AM_EXTENDED_6800,   3,  4,   0,  0, 15, 15, 14,  0, 0);
            SetOpCodeTableEntry(249, "ADC B",    0xF9, AddressingModes.AM_EXTENDED_6800,   3,  4,  15,  0, 15, 15, 15, 15, 0);
            SetOpCodeTableEntry(250, "ORA B",    0xFA, AddressingModes.AM_EXTENDED_6800,   3,  4,   0,  0, 15, 15, 14,  0, 0);
            SetOpCodeTableEntry(251, "ADD B",    0xFB, AddressingModes.AM_EXTENDED_6800,   3,  4,  15,  0, 15, 15, 15, 15, 0);
            SetOpCodeTableEntry(252, "~~~~~",    0xFC, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry(253, "~~~~~",    0xFD, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0);
            SetOpCodeTableEntry(254, "LDX  ",    0xFE, AddressingModes.AM_EXTENDED_6800,   3,  5,   0,  0,  9, 15, 14,  0, 0);
            SetOpCodeTableEntry(255, "STX  ",    0xFF, AddressingModes.AM_EXTENDED_6800,   3,  6,   0,  0,  9, 15, 14,  0, 0);
        }                       
                                
        private byte _opCode;

        private ushort _cf = 0;
        private ushort _vf = 0;
        private ushort _hf = 0;
        private ushort _nf = 0;

        private int cycles;
        private ushort _operand;

        private static string _coreDumpFile;
        private static string _statDumpFile;

        public override void CoreDump()
        {
            int nDatOffset;
            ulong page;
            int i, j;
            try
            {
                DateTime ltime = DateTime.Now;
                string szDate = ltime.ToString("yyyyMMdd");
                string szTime = ltime.ToString("HHmmss");
                _statDumpFile = Program.GetConfigurationAttribute("Global/Statistics", "filename", "");
                if (_statDumpFile.Length > 0)
                {
                    string dirName = Path.GetDirectoryName(_statDumpFile);
                    if (!dirName.Contains(":") && !dirName.StartsWith(@"\\") && !dirName.StartsWith("//"))
                    {
                        _statDumpFile = Path.Combine(Program.dataDir, _statDumpFile);
                    }
                    _statDumpFile = _statDumpFile.Replace("{date}", szDate);
                    _statDumpFile = _statDumpFile.Replace("{time}", szTime);
                    string[] pszTableName = new string[3];
                    pszTableName[0] = "MC6800";
                    using (TextWriter fp = new StreamWriter(File.Open(_statDumpFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
                    {
                        if (fp != null)
                        {
                            fp.Write(string.Format("A = {0} B = {1} X = {2} CCR = {3}\r\nIP= {4} SP= {5}\r\n\r\n",
                                AReg.ToString("X2"),
                                BReg.ToString("X2"),
                                OffsetRegisters[(int)OffsetRegisterIndex.m_X].ToString("X4"),
                                _ccr.ToString("X2"),
                                IP.ToString("X4"),
                                OffsetRegisters[(int)OffsetRegisterIndex.m_S].ToString("X4")));
                            fp.Write("\r\n");
                            for (i = 0; i < 256; i++)
                            {
                                AddressingModes m = opctbl[i].attribute;
                                if (m != AddressingModes.AM_ILLEGAL)
                                {
                                    long nCPUTime;
                                    int table = 0;
                                    nCPUTime = 0;
                                    fp.Write("insert into test values ('{0}', '0x{1}', '0x{2}', {3}, {4})\r\n",
                                            opctbl[i].mneumonic,
                                            table.ToString("X2"),
                                            ((byte)i).ToString("X2"),
                                            opctbl[i].lCount.ToString(),
                                            nCPUTime.ToString());
                                }
                            }
                        }
                    }
                }
                _coreDumpFile = Program.GetConfigurationAttribute("Global/CoreDump", "filename", "");
                if (_coreDumpFile.Length > 0)
                {
                    string dirName = Path.GetDirectoryName(_coreDumpFile);
                    if (!dirName.Contains(":") && !dirName.StartsWith(@"\\") && !dirName.StartsWith("//"))
                    {
                        _coreDumpFile = Path.Combine(Program.dataDir, _coreDumpFile);
                    }
                    _coreDumpFile = _coreDumpFile.Replace("{date}", szDate);
                    _coreDumpFile = _coreDumpFile.Replace("{time}", szTime);
                    using (BinaryWriter fp = new BinaryWriter(File.Open(_coreDumpFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
                    {
                        fp.Write(Memory.MemorySpace, 0, 65536);
                    }
                }
            }
            catch (Exception e)
            {
                //MessageBox.Show(e.Message);
            }
        }
        byte ReadMemByte(ushort sLogicalAddress)
        {
            return (Memory.MemorySpace[sLogicalAddress]);
        }
        private byte ReadOnlyROMorRAM (ushort _operand)
        {
            byte memoryContents = 0xff;
            if ((Memory.DeviceMap[_operand] == (int)Devices.DEVICE_RAM) || (Memory.DeviceMap[_operand] == (int)Devices.DEVICE_ROM))
            {
                memoryContents = ReadMemByte(_operand);
            }
            else
                memoryContents = Memory.PeekMemoryByte(_operand);
            return memoryContents;
        }
        private byte LoadOnlyROMorRAM(ushort _operand)
        {
            byte memoryContents = 0xff;
            if ((Memory.DeviceMap[_operand] == (int)Devices.DEVICE_RAM) || (Memory.DeviceMap[_operand] == (int)Devices.DEVICE_ROM))
            {
                memoryContents = ReadMemByte(_operand);
            }
            else
                memoryContents = Memory.PeekMemoryByte(_operand);
            return memoryContents;
        }
        public string BuildDebugLine(AddressingModes mode, ushort _currentIP, ushort _programCounter, bool m_IncludeHex = true)
        {
            ushort nOffset;
            string szDebugLine = "";
            string szMemoryContents = "";
            string szFinishedLine = "";
            lock (buildingDebugLineLock)
            {
                szMemoryContents = "";
                switch (mode)
                {
                    case AddressingModes.AM_INHERENT_6800:
                        szDebugLine = String.Format("{0} {1}       {2}", _currentIP.ToString("X4"), _opCode.ToString("X2"), opctbl[_opCode].mneumonic);
                        break;
                    case AddressingModes.AM_DIRECT_6800:
                        if (
                            _opCode == 0x9C ||
                            _opCode == 0x9E ||
                            _opCode == 0x9F ||
                            _opCode == 0xDE ||
                            _opCode == 0xDF
                           )
                        {
                            szDebugLine = String.Format("{0} {1} {3}    {2} ${3}     (${4})", _currentIP.ToString("X4"), _opCode.ToString("X2"), opctbl[_opCode].mneumonic, _operand.ToString("X2"), (LoadOnlyROMorRAM(_operand) * 256 + LoadOnlyROMorRAM((ushort)(_operand + 1))).ToString("X4"));
                            szMemoryContents = string.Format("(${0})", (ReadOnlyROMorRAM(_operand) * 256 + ReadOnlyROMorRAM((ushort)(_operand + 1))).ToString("X4"));
                        }
                        else
                        {
                            szDebugLine = String.Format("{0} {1} {3}    {2} ${3}     (${4})", _currentIP.ToString("X4"), _opCode.ToString("X2"), opctbl[_opCode].mneumonic, _operand.ToString("X2"), LoadOnlyROMorRAM(_operand).ToString("X2"));
                            szMemoryContents = string.Format("(${0})", ReadOnlyROMorRAM(_operand).ToString("X2"));
                        }
                        break;
                    case AddressingModes.AM_RELATIVE_6800:
                        nOffset = _operand;
                        if (_operand > 127)
                            nOffset += 0xFF00;
                        szDebugLine = String.Format("{0} {1} {3}    {2} ${3}     (${4})", _currentIP.ToString("X4"), _opCode.ToString("X2"), opctbl[_opCode].mneumonic, _operand.ToString("X2"), ((_programCounter + nOffset) & 0xFFFF).ToString("X4"));
                        szMemoryContents = string.Format("(${0})", ((IP + nOffset) & 0xFFFF).ToString("X4"));
                        break;
                    case AddressingModes.AM_EXTENDED_6800:
                        if (
                            _opCode == 0xBC || //  CPX
                            _opCode == 0xBE || //  LDS
                            _opCode == 0xBF || //  STS
                            _opCode == 0xFE || //  LDX
                            _opCode == 0xFF    //  STX
                           )
                        {
                            szDebugLine = String.Format("{0} {1} {3}  {2} ${3}   (${4})", _currentIP.ToString("X4"), _opCode.ToString("X2"), opctbl[_opCode].mneumonic, _operand.ToString("X4"), (LoadOnlyROMorRAM(_operand) * 256 + LoadOnlyROMorRAM((ushort)(_operand + 1))).ToString("X4"));
                            szMemoryContents = string.Format("(${0})", (ReadOnlyROMorRAM(_operand) * 256 + ReadOnlyROMorRAM((ushort)(_operand + 1))).ToString("X4"));
                        }
                        else
                        {
                            if (
                                _opCode == 0x7E || //  JMP
                                _opCode == 0xBD    //  JSR
                                )
                            {
                                szDebugLine = String.Format("{0} {1} {3}  {2} ${3}", _currentIP.ToString("X4"), _opCode.ToString("X2"), opctbl[_opCode].mneumonic, _operand.ToString("X4"));
                            }
                            else
                            {
                                szDebugLine = String.Format("{0} {1} {3}  {2} ${3}   (${4})", _currentIP.ToString("X4"), _opCode.ToString("X2"), opctbl[_opCode].mneumonic, _operand.ToString("X4"), LoadOnlyROMorRAM((ushort)(_operand)).ToString("X2"));
                                szMemoryContents = string.Format("(${0})", ReadOnlyROMorRAM(_operand).ToString("X2"));
                            }
                        }
                        break;
                    case AddressingModes.AM_IMM16_6800:
                        szDebugLine = String.Format("{0} {1} {3}  {2} #${3}", _currentIP.ToString("X4"), _opCode.ToString("X2"), opctbl[_opCode].mneumonic, _operand.ToString("X4"));
                        break;
                    case AddressingModes.AM_IMM8_6800:
                        szDebugLine = String.Format("{0} {1} {3}    {2} #${3}", _currentIP.ToString("X4"), _opCode.ToString("X2"), opctbl[_opCode].mneumonic, _operand.ToString("X2"));
                        break;
                    case AddressingModes.AM_INDEXED_6800:
                        if (
                            _opCode == 0xAC || //  CPX
                            _opCode == 0xAE || //  LDS
                            _opCode == 0xAF || //  STS
                            _opCode == 0xEE || //  LDX
                            _opCode == 0xEF    //  STX
                           )
                        {
                            szDebugLine = String.Format("{0} {1} {3}    {2} ${3},X   (${4})", _currentIP.ToString("X4"), _opCode.ToString("X2"), opctbl[_opCode].mneumonic, _operand.ToString("X2"), (LoadOnlyROMorRAM((ushort)(_operand + _xReg)) * 256 + LoadOnlyROMorRAM((ushort)(_operand + _xReg + 1))).ToString("X4"));
                            szMemoryContents = string.Format("(${0})", _operand.ToString("X4"));
                        }
                        else
                        {
                            if (
                                _opCode == 0x6E || //  JMP
                                _opCode == 0xAD    //  JSR
                               )
                            {
                                szDebugLine = String.Format("{0} {1} {3}    {2} ${3},X", _currentIP.ToString("X4"), _opCode.ToString("X2"), opctbl[_opCode].mneumonic, _operand.ToString("X2"));
                                szMemoryContents = string.Format("(${0})", _operand.ToString("X4"));
                            }
                            else
                            {
                                szDebugLine = String.Format("{0} {1} {3}    {2} ${3},X   (${4})", _currentIP.ToString("X4"), _opCode.ToString("X2"), opctbl[_opCode].mneumonic, _operand.ToString("X2"), LoadOnlyROMorRAM((ushort)(_operand + _xReg)).ToString("X2"));
                                szMemoryContents = string.Format("(${0})", _operand.ToString("X4"));
                            }
                        }
                        break;
                }
                szFinishedLine = string.Format("{0}{1}{2}{3}", _currentIP.ToString("X4"), _opCode.ToString("X2"), _operand.ToString("X4"), szDebugLine);
                if (m_IncludeHex)
                {
                    while (szFinishedLine.Length < 37)
                        szFinishedLine += " ";
                }
                else
                {
                    while (szFinishedLine.Length < 24)
                        szFinishedLine += " ";
                }
                szFinishedLine += szMemoryContents;
                string szRegisterContents =
                    string.Format
                    (
                        "{0} {1} {2} {3} {4} {5}",
                            clsTraceBuffer[TraceIndex].sStackTrace.ToString("X4"),
                            clsTraceBuffer[TraceIndex].cRegisterA.ToString("X2"),
                            clsTraceBuffer[TraceIndex].cRegisterB.ToString("X2"),
                            clsTraceBuffer[TraceIndex].sRegisterX.ToString("X4"),
                            clsTraceBuffer[TraceIndex].cRegisterCCR.ToString("X2"),
                            clsTraceBuffer[TraceIndex].sRegisterInterrupts.ToString()
                    );
            }
            return szDebugLine;
        }

        // HINZVC values:
        //
        //   0 No Affect
        //   1 (Bit V) Test: Result = 10000000?
        //   2 (Bit C) Test: Result = 00000000?
        //   3 (Bit C) Test: Decimal value of most significant BCD character greater than 9?
        //                   (Not cleared if previously set.)
        //   4 (Bit V) Test: Operand = 10000000 prior to execution?
        //   5 (Bit V) Test: Operand = 00000001 prior to execution?
        //   6 (Bit V) Test: Set equal of result of N exclusive or C after shift has occurred.
        //   7 (Bit N) Test: Sign bit of most significant byte of result = 1?
        //   8 (Bit V) Test: 2's complement overflow from subtraction od LS bytes?
        //   9 (Bit N) Test: Result less than zero? (Bit 15 = 1)
        //  10 (All)   Load condition code register from stack
        //  11 (Bit I) Set when interrupt occurrs. If prviously set, a Non-maskable interrupt is
        //             required to exit the wait state.
        //  12 (All)   Set according to the contents of Accumlator A
        //  13 (All)   Set always
        //  14 (All)   Reset always
        //  15 (All)   Test and set if true, cleared otherwise.
        //  16 (All)   same as 15 for 16 bit

        void SetCCR(ushort after)
        {
            SetCCR(after, (ushort)0);
        }

        void SetCCR (ushort after, ushort before)
        {
            int i;
            byte b;
            ushort s;
            int [] nRules = new int [6];

            Array.Copy (opctbl[_opCode].ccr_rules, nRules, nRules.Length);

            for (i = 0; i < 6; i++)
            {
                switch (nRules[i])
                {
                    case  0: // No Affect
                        break;

                    case  1: // (Bit V) Test: Result = 10000000?
                             //               ONLY USED FOR NEG     00 - M -> M
                             //                             NEG A   00 - A -> A
                             //                             NEG B   00 - B -> B
                        if (after == 0x80)
                            _ccr |= CCR_OVERFLOW;
                        else
                            _ccr &= (byte)~CCR_OVERFLOW;
                        break;

                    case  2: // (Bit C) Test: Result = 00000000?
                             //               ONLY USED FOR NEG     00 - M -> M
                             //                             NEG A   00 - A -> A
                             //                             NEG B   00 - B -> B

                        if (after == 0x00)
                            _ccr &= (byte)~CCR_CARRY;
                        else
                            _ccr |= CCR_CARRY;

                        break;

                    case  3: // (Bit C) Test: Decimal value of most significant BCD character > 9?
                             //               (Not cleared if previously set.)
                             //               ONLY USED FOR DAA
                        if (_cf == 1)
                            _ccr |= (byte)CCR_CARRY;
                        else
                            _ccr &= (byte)~CCR_CARRY;
                        break;

                    case  4: // (Bit V) Test: Operand = 10000000 prior to execution?
                             //               ONLY USED FOR DEC     M - 1 -> M
                             //                             DEC A   A - 1 -> A
                             //                             DEC B   B - 1 -> B
                        if (before == 0x80)
                            _ccr |= CCR_OVERFLOW;
                        else
                            _ccr &= (byte)~CCR_OVERFLOW;
                        break;

                    case  5: // (Bit V) Test: Operand = 01111111 prior to execution?
                             //               ONLY USED FOR INC     M + 1 -> M
                             //                             INC A   A + 1 -> A
                             //                             INC B   B + 1 -> B

                        if (before == 0x7F)
                            _ccr |= CCR_OVERFLOW;
                        else
                            _ccr &= (byte)~CCR_OVERFLOW;
                        break;

                    case  6: // (Bit V) Test: Set equal to result of N ^ C after shift has occurred.
                             //               ONLY USED FOR SHIFTS and ROTATES
                             //         The NEGATIVE bit in CCR will already be set
                             //         The Shifts and Rotates will have already set m_CF
                        {

                            int nNegative = (_ccr & CCR_NEGATIVE) == 0 ? 0 : 1;

                            if ((_cf ^ nNegative) == 1)
                                _ccr |= CCR_OVERFLOW;
                            else
                                _ccr &= (byte)~CCR_OVERFLOW;
                        }
                        break;

                    case  7: // (Bit N) Test: Sign bit of most significant byte of result = 1?
                             //               ONLY USED BY CPX

                        if ((after & 0x8000) == 0x8000)
                            _ccr |= CCR_NEGATIVE;
                        else
                            _ccr &= (byte)~CCR_NEGATIVE;
                        break;

                    case  8: // (Bit V) Test: 2's complement overflow from subtraction od LS bytes?
                             //               ONLY USED BY CPX

                        s = (ushort)((before & (ushort)0x00ff) - (_operand & (ushort)0x00ff));
                        if ((s & 0x0100) == 0x0100)
                            _ccr |= CCR_OVERFLOW;
                        else
                            _ccr &= (byte)~CCR_OVERFLOW;
                        break;

                    case  9: // (Bit N) Test: Result less than zero? (Bit 15 = 1)

                        if ((after & 0x8000) != 0)
                            _ccr |= CCR_NEGATIVE;
                        else
                            _ccr &= (byte)~CCR_NEGATIVE;
                        break;

                    case 10: // (All)   Load condition code register from stack
                             //         The instruction execution will have set CCR

                        break;

                    case 11: // (Bit I) Set when interrupt occurrs. If previously set, a Non-maskable interrupt is
                             //         required to exit the wait state.
                             //         The instruction execution will have set CCR

                        break;

                    case 12: // (All)   Set according to the contents of Accumlator A
                             //         The instruction execution will have set CCR (ONLY USED BY TAP)

                        break;

                    case 13: // (All)   Set always

                        b = (byte)(0x01 << (5 - i));
                        _ccr |= b;
                        break;

                    case 14: // (All)   Reset always

                        b = (byte)(0x01 << (5 - i));
                        _ccr &= (byte)~b;
                        break;

                    case 15: // (All)   Test and set if true, cleared otherwise. 8 bit

                        switch (i)
                        {
                            case 0: // H    m_HF will be set by instruction execution

                                if (_hf == 1)
                                    _ccr |= CCR_HALFCARRY;
                                else
                                    _ccr &= (byte)~CCR_HALFCARRY;
                                break;

                            case 1: // I    Only set by SWI in case 13

                                break;

                            case 2: // N    

                                if ((after & 0x0080) == 0x80)
                                    _ccr |= CCR_NEGATIVE;
                                else
                                    _ccr &= (byte)~CCR_NEGATIVE;
                                break;

                            case 3: // Z

                                if (after == 0)
                                    _ccr |= CCR_ZERO;
                                else
                                    _ccr &= (byte)~CCR_ZERO;
                                break;

                            case 4: // V    Only on Add's, Compare's, DAA, and Subtracts

                                if (_vf == 1)
                                    _ccr |= CCR_OVERFLOW;
                                else
                                    _ccr &= (byte)~CCR_OVERFLOW;
                                break;

                            case 5: // C    m_CF will be set by instruction execution

                                if (_cf == 1)
                                    _ccr |= CCR_CARRY;
                                else
                                    _ccr &= (byte)~CCR_CARRY;
                                break;
                        }
                        break;

                    case 16: // (All)   Test and set if true, cleared otherwise. (16 bit)

                        switch (i)
                        {
                            case 0: // H    m_HF will be set by instruction execution

                                if (_hf == 1)
                                    _ccr |= CCR_HALFCARRY;
                                else
                                    _ccr &= (byte)~CCR_HALFCARRY;
                                break;

                            case 1: // I    Only set by SWI in case 13

                                break;

                            case 2: // N    

                                if ((after & 0x8000) == 0x8000)
                                    _ccr |= CCR_NEGATIVE;
                                else
                                    _ccr &= (byte)~CCR_NEGATIVE;
                                break;

                            case 3: // Z

                                if (after == 0)
                                    _ccr |= CCR_ZERO;
                                else
                                    _ccr &= (byte)~CCR_ZERO;
                                break;

                            case 4: // V    Only on Add's, Compare's, DAA, and Subtracts

                                if (_vf == 1)
                                    _ccr |= CCR_OVERFLOW;
                                else
                                    _ccr &= (byte)~CCR_OVERFLOW;
                                break;

                            case 5: // C    m_CF will be set by instruction execution

                                if (_cf == 1)
                                    _ccr |= CCR_CARRY;
                                else
                                    _ccr &= (byte)~CCR_CARRY;
                                break;
                        }
                        break;
                }
            }
        }

        private void DecimalAdjustAccumulator ()
        {
            byte CF, UB, HF, LB;

            CF = (byte)(_ccr & CCR_CARRY);
            HF = (byte)((_ccr & CCR_HALFCARRY) == 0 ? 0 : 2);
            UB = (byte)((_aReg & 0xf0) >> 4);
            LB = (byte)(_aReg & 0x0f);

            _cf = 0;

            switch (CF + HF)
            {
                case 0:         // Carry clear - Halfcarry clear
                    while (true)
                    {
                        if ((UB >= 0 && UB <= 9) && (LB >= 0 && LB <= 9))
                        {
                            _cf = 0;
                            SetCCR(_aReg);
                            break;
                        }
                        if ((UB >= 0 && UB <= 8) && (LB >= 10 && LB <= 15))
                        {
                            _aReg += 0x06;
                            _cf = 0;
                            SetCCR(_aReg);
                            break;
                        }
                        if ((UB >= 10 && UB <= 15) && (LB >= 0 && LB <= 9))
                        {
                            _aReg += 0x60;
                            _cf = 1;
                            SetCCR(_aReg);
                            break;
                        }
                        if ((UB >= 9 && UB <= 15) && (LB >= 10 && LB <= 15))
                        {
                            _aReg += 0x66;
                            _cf = 1;
                            SetCCR(_aReg);
                            break;
                        }
                        break;
                    }
                    break;

                case 1:         // Carry set - Halfcarry clear
                    while (true)
                    {
                        if ((UB >= 0 && UB <= 2) && (LB >= 0 && LB <= 9))
                        {
                            _aReg += 0x60;
                            _cf = 1;
                            SetCCR(_aReg);
                            break;
                        }
                        if ((UB >= 0 && UB <= 2) && (LB >= 10 && LB <= 15))
                        {
                            _aReg += 0x66;
                            _cf = 1;
                            SetCCR(_aReg);
                            break;
                        }
                        break;
                    }
                    break;
                case 2:         // Carry clear - Halfcarry set
                    while (true)
                    {
                        if ((UB >= 0 && UB <= 9) && (LB >= 0 && LB <= 3))
                        {
                            _aReg += 0x06;
                            _cf = 0;
                            SetCCR(_aReg);
                            break;
                        }
                        if ((UB >= 10 && UB <= 15) && (LB >= 0 && LB <= 3))
                        {
                            _aReg += 0x66;
                            _cf = 1;
                            SetCCR(_aReg);
                            break;
                        }
                        break;
                    }
                    break;
                case 3:         // Carry set - Halfcarry set
                    if ((UB >= 0 && UB <= 3) && (LB >= 0 && LB <= 3))
                    {
                        _aReg += 0x66;
                        _cf = 1;
                        SetCCR(_aReg);
                    }
                    break;
            }
        }

        #region Arithmetic and Logical Register Operations

        private byte SubtractRegister (byte cReg, byte cOperand)
        {
            //  R - M -> R

            byte c;

            if (cReg < cOperand)
                _cf = 1;
            else
                _cf = 0;

            c = (byte)(cReg - cOperand);

            if (
                ((cReg & 0x80) == 0x80 && (cOperand & 0x80) == 0x00 && (c & 0x80) == 0x00) ||
                ((cReg & 0x80) == 0x00 && (cOperand & 0x80) == 0x80 && (c & 0x80) == 0x80)
               )
                _vf = 1;
            else
                _vf = 0;

            SetCCR (c);

            return (c);
        }

        private byte CompareRegister (byte cReg, byte cOperand)
        {
            //  R - M

            byte c;

            if (cReg < cOperand)
                _cf = 1;
            else
                _cf = 0;
    
            c = (byte)(cReg - cOperand);

            if (
                ((cReg & 0x80) == 0x80 && (cOperand & 0x80) == 0x00 && (c & 0x80) == 0x00) ||
                ((cReg & 0x80) == 0x00 && (cOperand & 0x80) == 0x80 && (c & 0x80) == 0x80)
               )
                _vf = 1;
            else
                _vf = 0;

            SetCCR (c);

            return (cReg);
        }

        private byte SubtractWithCarryRegister (byte cReg, byte cOperand)
        {
            //  R - M - C -> R

            byte c;

            if (cReg < (cOperand + (_ccr & CCR_CARRY)))
                _cf = 1;
            else
                _cf = 0;

            c = (byte)(cReg - cOperand - (_ccr & CCR_CARRY));

            if (
                ((cReg & 0x80) == 0x80 && (cOperand & 0x80) == 0x00 && (c & 0x80) == 0x00) ||
                ((cReg & 0x80) == 0x00 && (cOperand & 0x80) == 0x80 && (c & 0x80) == 0x80)
               )
                _vf = 1;
            else
                _vf = 0;

            SetCCR (c);

            return (c);
        }

        private byte AndRegister (byte cReg, byte cOperand)
        {
            cReg &= cOperand;

            SetCCR (cReg);

            return (cReg);
        }

        private void BitRegister (byte cReg, byte cOperand)
        {
            byte c;

            c = (byte)(cReg & cOperand);
            SetCCR (c);
        }

        private byte ExclusiveOrRegister (byte cReg, byte cOperand)
        {
            cReg ^= cOperand;
            SetCCR (cReg);

            return (cReg);
        }

        private byte AddWithCarryRegister (byte cReg, byte cOperand)
        {
            //  R + M + C -> R

            byte c;

            if ((cReg + cOperand + (_ccr & CCR_CARRY)) > 255)
                _cf = 1;
            else
                _cf = 0;

            if (((cReg & 0x0f) + (cOperand & 0x0f) + (_ccr & CCR_CARRY)) > 15)
                _hf = 1;
            else
                _hf = 0;

            c = (byte)(cReg + cOperand + (_ccr & CCR_CARRY));
            if (
                ((cReg & 0x80) == 0x80 && (cOperand & 0x80) == 0x80 && (c & 0x80) == 0x00) ||
                ((cReg & 0x80) == 0x00 && (cOperand & 0x80) == 0x00 && (c & 0x80) == 0x80)
               )
                _vf = 1;
            else
                _vf = 0;

            SetCCR (c);

            return (c);
        }

        private byte OrRegister (byte cReg, byte cOperand)
        {
            cReg |= cOperand;
            SetCCR (cReg);

            return (cReg);
        }

        private byte AddRegister (byte cReg, byte cOperand)
        {
            byte c;

            if ((cReg + cOperand) > 255)
                _cf = 1;
            else
                _cf = 0;

            if (((cReg & 0x0f) + (cOperand & 0x0f)) > 15)
                _hf = 1;
            else
                _hf = 0;

            c = (byte)(cReg + cOperand);
            if (
                ((cReg & 0x80) == 0x80 && (cOperand & 0x80) == 0x80 && (c & 0x80) == 0x00) ||
                ((cReg & 0x80) == 0x00 && (cOperand & 0x80) == 0x00 && (c & 0x80) == 0x80)
               )
                _vf = 1;
            else
                _vf = 0;

            SetCCR (c);

            return (c);
        }
        #endregion

        #region Instruction Executors
        private void Execute6800Inherent ()
        {
            byte  cReg;

            switch (_opCode)
            {
                case 0x01:     //    "NOP  "
                    break;

                case 0x06:     //    "TAP  "
                    _ccr = (byte)(_aReg | 0xC0);
                    break;

                case 0x07:     //    "TPA  "
                    _aReg = _ccr;
                    break;

                case 0x08:     //    "INX  "
                    _xReg++;
                    SetCCR(_xReg);
                    break;

                case 0x09:     //    "DEX  "
                    _xReg--;
                    SetCCR(_xReg);
                    break;

                case 0x0A:     //    "CLV  "
                    _ccr &= (byte)~CCR_OVERFLOW;
                    break;

                case 0x0B:     //    "SEV  "
                    _ccr |= CCR_OVERFLOW;
                    break;

                case 0x0C:     //    "CLC  "
                    _ccr &= (byte)~CCR_CARRY;
                    break;

                case 0x0D:     //    "SEC  "
                    _ccr |= CCR_CARRY;
                    break;

                case 0x0E:     //    "CLI  "
                    _ccr &= (byte)~CCR_INTERRUPT;
                    break;

                case 0x0F:     //    "SEI  "
                    _ccr |= CCR_INTERRUPT;
                    break;

                case 0x10:     //    "SBA  "
                    if (_aReg < _bReg)
                        _cf = 1;
                    else
                        _cf = 0;

                    cReg = (byte)(_aReg - _bReg);
                    if (
                        ((_aReg & 0x80) == 0x80 && (_bReg & 0x80) == 0x00 && (cReg & 0x80) == 0x00) ||
                        ((_aReg & 0x80) == 0x00 && (_bReg & 0x80) == 0x80 && (cReg & 0x80) == 0x80)
                       )
                        _vf = 1;
                    else
                        _vf = 0;

                    _aReg = cReg;
                    SetCCR(_aReg);
                    break;

                case 0x11:     //    "CBA  "
                    if (_aReg < _bReg)
                        _cf = 1;
                    else
                        _cf = 0;

                    cReg = (byte)(_aReg - _bReg);
                    if (
                        ((_aReg & 0x80) == 0x80 && (_bReg & 0x80) == 0x00 && (cReg & 0x80) == 0x00) ||
                        ((_aReg & 0x80) == 0x00 && (_bReg & 0x80) == 0x80 && (cReg & 0x80) == 0x80)
                       )
                        _vf = 1;
                    else
                        _vf = 0;

                    SetCCR (cReg);
                    break;

                case 0x16:     //    "TAB  "
                    _bReg = _aReg;
                    SetCCR(_bReg);
                    break;

                case 0x17:     //    "TBA  "
                    _aReg = _bReg;
                    SetCCR(_aReg);
                    break;

                case 0x19:     //    "DAA  "
                    DecimalAdjustAccumulator ();
                    break;

                case 0x1B:     //    "ABA  "
                    if ((_aReg + _bReg) > 255)
                        _cf = 1;
                    else
                        _cf = 0;

                    if (((_aReg & 0x0f) + (_bReg & 0x0f)) > 15)
                        _hf = 1;
                    else
                        _hf = 0;

                    cReg = (byte)(_aReg + _bReg);
                    if (
                        ((_aReg & 0x80) == 0x80 && (_bReg & 0x80) == 0x80 && (cReg & 0x80) == 0x00) ||
                        ((_aReg & 0x80) == 0x00 && (_bReg & 0x80) == 0x00 && (cReg & 0x80) == 0x80)
                       )
                        _vf = 1;
                    else
                        _vf = 0;

                    _aReg = cReg;
                    SetCCR(_aReg);
                    break;

                case 0x30:     //    "TSX  "
                    _xReg = (ushort)(_sp + 1);
                    break;

                case 0x31:     //    "INS  "
                    _sp++;
                    break;

                case 0x32:     //    "PUL A"
                    _aReg = Memory.LoadMemoryByte(++_sp);
                    break;

                case 0x33:     //    "PUL B"
                    _bReg = Memory.LoadMemoryByte(++_sp);
                    break;

                case 0x34:     //    "DES  "
                    _sp--;
                    break;

                case 0x35:     //    "TXS  "
                    _sp = (ushort)(_xReg - 1);
                    break;

                case 0x36:     //    "PSH A"
                    Memory.StoreMemoryByte(_aReg, _sp--);
                    break;

                case 0x37:     //    "PSH B"
                    Memory.StoreMemoryByte(_bReg, _sp--);
                    break;

                case 0x39:     //    "RTS  "
                    ProgramCounter = (ushort)(Memory.MemorySpace[++_sp] * 256);
                    ProgramCounter = (ushort)(ProgramCounter + Memory.MemorySpace[++_sp]);
                    break;

                case 0x3B:     //    "RTI  "
                    _ccr = Memory.MemorySpace[++_sp];
                    _bReg = Memory.MemorySpace[++_sp];
                    _aReg = Memory.MemorySpace[++_sp];
                    _xReg = (ushort)(Memory.MemorySpace[++_sp] * 256);
                    _xReg += (ushort)(Memory.MemorySpace[++_sp]);
                    ProgramCounter = (ushort)(Memory.MemorySpace[++_sp] * 256);
                    ProgramCounter += (ushort)(Memory.MemorySpace[++_sp]);
                    break;

                case 0x3E:     //    "WAI  "
                    Memory.MemorySpace[_sp--] = (byte)(ProgramCounter % 256);
                    Memory.MemorySpace[_sp--] = (byte)(ProgramCounter / 256);
                    Memory.MemorySpace[_sp--] = (byte)(_xReg % 256);
                    Memory.MemorySpace[_sp--] = (byte)(_xReg / 256);
                    Memory.MemorySpace[_sp--] = _aReg;
                    Memory.MemorySpace[_sp--] = _bReg;
                    Memory.MemorySpace[_sp--] = _ccr;
                    InWait = true;
                    Program.CpuThread.Suspend();
                    break;

                case 0x3F:     //    "SWI  "
                    Memory.MemorySpace[_sp--] = (byte)(ProgramCounter % 256);
                    Memory.MemorySpace[_sp--] = (byte)(ProgramCounter / 256);
                    Memory.MemorySpace[_sp--] = (byte)(_xReg % 256);
                    Memory.MemorySpace[_sp--] = (byte)(_xReg / 256);
                    Memory.MemorySpace[_sp--] = _aReg;
                    Memory.MemorySpace[_sp--] = _bReg;
                    Memory.MemorySpace[_sp--] = _ccr;
                    _ccr |= CCR_INTERRUPT;
                    ProgramCounter = Memory.LoadMemoryWord(0xFFFA);
                    break;

                case 0x40:     //    "NEG R"
                    _aReg = (byte)(0x00 - _aReg);
                    SetCCR(_aReg);
                    break;

                case 0x43:     //    "COM R"
                    _aReg = (byte)(0xFF - _aReg);
                    SetCCR(_aReg);
                    break;

                case 0x44:     //    "LSR R"
                    cReg = _aReg;
                    _cf = (byte)(_aReg & 0x01);
                    _aReg = (byte)((_aReg >> 1) & 0x7F);        // always set bit 7 to 0
                    SetCCR(_aReg, cReg);
                    break;

                case 0x46:     //    "ROR R"
                    cReg = _aReg;
                    _cf = (byte)(_aReg & 0x01);
                    _aReg = (byte)(_aReg >> 1);

                    // set bit 7 = old carry flag

                    if ((_ccr & CCR_CARRY) == CCR_CARRY)
                        _aReg = (byte)(_aReg | 0x80);
                    else
                        _aReg = (byte)(_aReg & 0x7F);
                    SetCCR(_aReg, cReg);
                    break;

                case 0x47:     //    "ASR R"
                    cReg = _aReg;
                    _cf = (byte)(_aReg & 0x01);
                    _aReg = (byte)(_aReg >> 1);
                    _aReg = (byte)(_aReg | (cReg & 0x80));      // preserve the sign bit
                    SetCCR(_aReg, cReg);
                    break;

                case 0x48:     //    "ASL R"
                    cReg = _aReg;
                    if ((_aReg & 0x80) == 0x80)
                        _cf = 1;
                    else
                        _cf = 0;
                    _aReg = (byte)(_aReg << 1);
                    SetCCR(_aReg, cReg);
                    break;

                case 0x49:     //    "ROL R"
                    cReg = _aReg;
                    if ((_aReg & 0x80) == 0x80)
                        _cf = 1;
                    else
                        _cf = 0;
                    _aReg = (byte)(_aReg << 1);
                    _aReg |= (byte)(_ccr & CCR_CARRY);
                    SetCCR(_aReg, cReg);
                    break;

                case 0x4A:     //    "DEC R"
                    cReg = _aReg;
                    _aReg = (byte)(_aReg - 1);
                    SetCCR(_aReg, cReg);
                    break;

                case 0x4C:     //    "INC R"
                    cReg = _aReg;
                    _aReg = (byte)(_aReg + 1);
                    SetCCR(_aReg, cReg);
                    break;

                case 0x4D:     //    "TST R"
                    SetCCR(_aReg);
                    break;

                case 0x4F:     //    "CLR R"
                    _aReg = 0x00;
                    SetCCR(_aReg);
                    break;

                case 0x50:     //    "NEG R"
                    _bReg = (byte)(0x00 - _bReg);
                    SetCCR(_bReg);
                    break;

                case 0x53:     //    "COM R"
                    _bReg = (byte)(0xFF - _bReg);
                    SetCCR(_bReg);
                    break;

                case 0x54:     //    "LSR R"
                    cReg = _bReg;
                    _cf = (byte)(_bReg & 0x01);
                    _bReg = (byte)((_bReg >> 1) & 0x7F);        // always set bit 7 to 0
                    SetCCR(_bReg, cReg);
                    break;

                case 0x56:     //    "ROR R"
                    cReg = _aReg;
                    _cf = (byte)(_bReg & 0x01);
                    _bReg = (byte)(_bReg >> 1);

                    // set bit 7 = old carry flag

                    if ((_ccr & CCR_CARRY) == CCR_CARRY)
                        _bReg = (byte)(_bReg | 0x80);
                    else
                        _bReg = (byte)(_bReg & 0x7F);
                    SetCCR(_bReg, cReg);
                    break;

                case 0x57:     //    "ASR R"
                    cReg = _bReg;
                    _cf = (byte)(_bReg & 0x01);
                    _bReg = (byte)(_bReg >> 1);
                    _bReg = (byte)(_bReg | (cReg & 0x80));      // preserve the sign bit
                    SetCCR(_bReg, cReg);
                    break;

                case 0x58:     //    "ASL R"
                    cReg = _bReg;
                    if ((_bReg & 0x80) == 0x80)
                        _cf = 1;
                    else
                        _cf = 0;
                    _bReg = (byte)(_bReg << 1);
                    SetCCR(_bReg, cReg);
                    break;

                case 0x59:     //    "ROL R"
                    cReg = _bReg;
                    if ((_bReg & 0x80) == 0x80)
                        _cf = 1;
                    else
                        _cf = 0;
                    _bReg = (byte)(_bReg << 1);
                    _bReg |= (byte)(_ccr & CCR_CARRY);
                    SetCCR(_bReg, cReg);
                    break;

                case 0x5A:     //    "DEC R"
                    cReg = _bReg;
                    _bReg = (byte)(_bReg - 1);
                    SetCCR(_bReg, cReg);
                    break;

                case 0x5C:     //    "INC R"
                    cReg = _bReg;
                    _bReg = (byte)(_bReg + 1);
                    SetCCR(_bReg, cReg);
                    break;

                case 0x5D:     //    "TST R"
                    SetCCR(_bReg);
                    break;

                case 0x5F:     //    "CLR R"
                    _bReg = 0x00;
                    SetCCR(_bReg);
                    break;
            }
        }

        private void Execute6800Extended ()
        {
            ushort sData;
            byte  cData;
            byte  cReg;
            ushort sBefore;
            ushort sResult;

            switch (_opCode)
            {
                case 0x70:     //    "NEG  "
                    cData = Memory.LoadMemoryByte(_operand);
                    cData = (byte)(0x00 - cData);
                    Memory.StoreMemoryByte (cData, _operand);
                    SetCCR (cData);
                    break;

                case 0x73:     //    "COM  "
                    cData = Memory.LoadMemoryByte(_operand);
                    cData = (byte)(0xFF - cData);
                    Memory.StoreMemoryByte (cData, _operand);
                    SetCCR (cData);
                    break;

                case 0x74:     //    "LSR  "
                    cData = Memory.LoadMemoryByte(_operand);
                    cReg = cData;
                    _cf = (byte)(cData & 0x01);
                    cData = (byte)((cData >> 1) & 0x7F);        // always set bit 7 to 0
                    Memory.StoreMemoryByte (cData, _operand);
                    SetCCR (cData, cReg);
                    break;

                case 0x76:     //    "ROR  "
                    cData = Memory.LoadMemoryByte(_operand);
                    cReg = cData;
                    _cf = (byte)(cData & 0x01);
                    cData = (byte)(cData >> 1);

                    // set bit 7 = old carry flag

                    if ((_ccr & CCR_CARRY) == CCR_CARRY)
                        cData = (byte)(cData | 0x80);
                    else
                        cData = (byte)(cData & 0x7F);
                    Memory.StoreMemoryByte (cData, _operand);
                    SetCCR (cData, cReg);
                    break;

                case 0x77:     //    "ASR  "
                    cData = Memory.LoadMemoryByte(_operand);
                    cReg = cData;
                    _cf = (byte)(cData & 0x01);
                    cData  = (byte)(cData >> 1);
                    cData = (byte)(cData | (cReg & 0x80));      // preserve the sign bit
                    Memory.StoreMemoryByte (cData, _operand);
                    SetCCR (cData, cReg);
                    break;

                case 0x78:     //    "ASL  "
                    cData = Memory.LoadMemoryByte(_operand);
                    cReg = cData;
                    if ((cData & 0x80) == 0x80)
                        _cf = 1;
                    else
                        _cf = 0;
                    cData  = (byte)(cData << 1);
                    Memory.StoreMemoryByte (cData, _operand);
                    SetCCR (cData, cReg);
                    break;

                case 0x79:     //    "ROL  "
                    cData = Memory.LoadMemoryByte(_operand);
                    cReg = cData;
                    if ((cData & 0x80) == 0x80)
                        _cf = 1;
                    else
                        _cf = 0;
                    cData  = (byte)(cData << 1);
                    cData |= (byte)(_ccr & CCR_CARRY);
                    Memory.StoreMemoryByte (cData, _operand);
                    SetCCR (cData, cReg);
                    break;

                case 0x7A:     //    "DEC  "
                    cData = Memory.LoadMemoryByte(_operand);
                    cReg = cData;
                    cData -= 1;
                    Memory.StoreMemoryByte (cData, _operand);
                    SetCCR (cData, cReg);
                    break;

                case 0x7C:     //    "INC  "
                    cData = Memory.LoadMemoryByte(_operand);
                    cReg = cData;
                    cData += 1;
                    Memory.StoreMemoryByte (cData, _operand);
                    SetCCR (cData, cReg);
                    break;

                case 0x7D:     //    "TST  "
                    cData = Memory.LoadMemoryByte(_operand);
                    SetCCR (cData);
                    break;

                case 0x7E:     //    "JMP  "
                    ProgramCounter = _operand;
                    break;

                case 0x7F:     //    "CLR  "
                    cData = 0x00;
                    Memory.StoreMemoryByte (cData, _operand);
                    SetCCR (cData);
                    break;

                case 0xBC:     //    "CPX  "
                    sData = Memory.LoadMemoryWord(_operand);
                    sBefore = _xReg;
                    sResult = (ushort)(_xReg - sData);
                    SetCCR (sResult, sBefore);
                    break;

                case 0xBD:     //    "JSR  "

                    // First save current IP

                    Memory.MemorySpace[_sp--] = (byte)(ProgramCounter % 256);
                    Memory.MemorySpace[_sp--] = (byte)(ProgramCounter / 256);

                    // Then load new IP

                    ProgramCounter = _operand;

                    break;

                case 0xBE:     //    "LDS  "
                    _sp = Memory.LoadMemoryWord(_operand);
                    SetCCR (_sp);
                    break;

                case 0xBF:     //    "STS  "    
                    Memory.StoreMemoryWord(_sp, _operand);
                    SetCCR (_sp);
                    break;

                case 0xFE:     //    "LDX  "
                    _xReg = Memory.LoadMemoryWord(_operand);
                    SetCCR(_xReg);
                    break;

                case 0xFF:     //    "STX  "
                    Memory.StoreMemoryWord(_xReg, _operand);
                    SetCCR(_xReg);
                    break;

                // Register A and B Extended Instructions

                case 0xB0:     //    "SUB R"
                    cData = Memory.LoadMemoryByte(_operand);
                    _aReg = SubtractRegister(_aReg, (byte)cData);
                    break;
                case 0xB1:     //    "CMP R"
                    cData = Memory.LoadMemoryByte(_operand);
                    CompareRegister(_aReg, (byte)cData);
                    break;
                case 0xB2:     //    "SBC R"
                    cData = Memory.LoadMemoryByte(_operand);
                    _aReg = SubtractWithCarryRegister(_aReg, (byte)cData);
                    break;
                case 0xB4:     //    "AND R"
                    cData = Memory.LoadMemoryByte(_operand);
                    _aReg = AndRegister(_aReg, (byte)cData);
                    break;
                case 0xB5:     //    "BIT R"
                    cData = Memory.LoadMemoryByte(_operand);
                    BitRegister(_aReg, (byte)cData);
                    break;
                case 0xB6:     //    "LDA R"
                    _aReg = Memory.LoadMemoryByte(_operand);
                    SetCCR(_aReg);
                    break;
                case 0xB7:     //    "STA R"
                    Memory.StoreMemoryByte(_aReg, _operand);
                    SetCCR(_aReg);
                    break;
                case 0xB8:     //    "EOR R"
                    cData = Memory.LoadMemoryByte(_operand);
                    _aReg = ExclusiveOrRegister(_aReg, (byte)cData);
                    break;
                case 0xB9:     //    "ADC R"
                    cData = Memory.LoadMemoryByte(_operand);
                    _aReg = AddWithCarryRegister(_aReg, (byte)cData);
                    break;
                case 0xBA:     //    "ORA R"
                    cData = Memory.LoadMemoryByte(_operand);
                    _aReg = OrRegister(_aReg, (byte)cData);
                    break;
                case 0xBB:     //    "ADD R"
                    cData = Memory.LoadMemoryByte(_operand);
                    _aReg = AddRegister(_aReg, (byte)cData);
                    break;
                case 0xF0:     //    "SUB R"
                    cData = Memory.LoadMemoryByte(_operand);
                    _bReg = SubtractRegister(_bReg, (byte)cData);
                    break;
                case 0xF1:     //    "CMP R"
                    cData = Memory.LoadMemoryByte(_operand);
                    CompareRegister(_bReg, (byte)cData);
                    break;
                case 0xF2:     //    "SBC R"
                    cData = Memory.LoadMemoryByte(_operand);
                    _bReg = SubtractWithCarryRegister(_bReg, (byte)cData);
                    break;
                case 0xF4:     //    "AND R"
                    cData = Memory.LoadMemoryByte(_operand);
                    _bReg = AndRegister(_bReg, (byte)cData);
                    break;
                case 0xF5:     //    "BIT R"
                    cData = Memory.LoadMemoryByte(_operand);
                    BitRegister(_bReg, (byte)cData);
                    break;
                case 0xF6:     //    "LDA R"
                    _bReg = Memory.LoadMemoryByte(_operand);
                    SetCCR(_bReg);
                    break;
                case 0xF7:     //    "STA R"
                    Memory.StoreMemoryByte(_bReg, _operand);
                    SetCCR(_bReg);
                    break;
                case 0xF8:     //    "EOR R"
                    cData = Memory.LoadMemoryByte(_operand);
                    _bReg = ExclusiveOrRegister(_bReg, (byte)cData);
                    break;
                case 0xF9:     //    "ADC R"
                    cData = Memory.LoadMemoryByte(_operand);
                    _bReg = AddWithCarryRegister(_bReg, (byte)cData);
                    break;
                case 0xFA:     //    "ORA R"
                    cData = Memory.LoadMemoryByte(_operand);
                    _bReg = OrRegister(_bReg, (byte)cData);
                    break;
                case 0xFB:     //    "ADD R"
                    cData = Memory.LoadMemoryByte(_operand);
                    _bReg = AddRegister(_bReg, (byte)cData);
                    break;
            }
        }

        private void Execute6800Indexed ()
        {
            ushort sOperandPtr;
            ushort sData;
            byte  cData;
            byte  cReg;
            ushort sBefore;
            ushort sResult;

            sOperandPtr = (ushort)(_operand + _xReg);

            switch (_opCode)
            {
                case 0x60:     //    "NEG  "
                    cData = Memory.LoadMemoryByte(sOperandPtr);
                    cData = (byte)(0x00 - cData);
                    Memory.StoreMemoryByte (cData, sOperandPtr);
                    SetCCR (cData);
                    break;

                case 0x63:     //    "COM  "
                    cData = Memory.LoadMemoryByte(sOperandPtr);
                    cData = (byte)(0xFF - cData);
                    Memory.StoreMemoryByte (cData, sOperandPtr);
                    SetCCR (cData);
                    break;

                case 0x64:     //    "LSR  "
                    cData = Memory.LoadMemoryByte(sOperandPtr);
                    cReg = cData;
                    _cf = (byte)(cData & 0x01);
                    cData = (byte)((cData >> 1) & 0x7F);        // always set bit 7 to 0
                    Memory.StoreMemoryByte (cData, sOperandPtr);
                    SetCCR (cData, cReg);
                    break;

                case 0x66:     //    "ROR  "
                    cData = Memory.LoadMemoryByte(sOperandPtr);
                    cReg = cData;
                    _cf = (byte)(cData & 0x01);
                    cData = (byte)(cData >> 1);

                    // set bit 7 = old carry flag

                    if ((_ccr & CCR_CARRY) == CCR_CARRY)
                        cData = (byte)(cData | 0x80);
                    else
                        cData = (byte)(cData & 0x7F);
                    Memory.StoreMemoryByte (cData, sOperandPtr);
                    SetCCR (cData, cReg);
                    break;

                case 0x67:     //    "ASR  "
                    cData = Memory.LoadMemoryByte(sOperandPtr);
                    cReg = cData;
                    _cf = (byte)(cData & 0x01);
                    cData = (byte)(cData >> 1);
                    cData = (byte)(cData | (cReg & 0x80));      // preserve the sign bit
                    Memory.StoreMemoryByte (cData, sOperandPtr);
                    SetCCR (cData, cReg);
                    break;

                case 0x68:     //    "ASL  "
                    cData = Memory.LoadMemoryByte(sOperandPtr);
                    cReg = cData;
                    if ((cData & 0x80) == 0x80)
                        _cf = 1;
                    else
                        _cf = 0;
                    cData  = (byte)(cData << 1);
                    Memory.StoreMemoryByte (cData, sOperandPtr);
                    SetCCR (cData, cReg);
                    break;

                case 0x69:     //    "ROL  "
                    cData = Memory.LoadMemoryByte(sOperandPtr);
                    cReg = cData;
                    if ((cData & 0x80) == 0x80)
                        _cf = 1;
                    else
                        _cf = 0;
                    cData  = (byte)(cData << 1);
                    cData |= (byte)(_ccr & CCR_CARRY);
                    Memory.StoreMemoryByte (cData, sOperandPtr);
                    SetCCR (cData, cReg);
                    break;

                case 0x6A:     //    "DEC  "
                    cData = Memory.LoadMemoryByte(sOperandPtr);
                    cReg = cData;
                    cData -= 1;
                    Memory.StoreMemoryByte (cData, sOperandPtr);
                    SetCCR (cData, cReg);
                    break;

                case 0x6C:     //    "INC  "
                    cData = Memory.LoadMemoryByte(sOperandPtr);
                    cReg = cData;
                    cData += 1;
                    Memory.StoreMemoryByte (cData, sOperandPtr);
                    SetCCR (cData, cReg);
                    break;

                case 0x6D:     //    "TST  "
                    cData = Memory.LoadMemoryByte(sOperandPtr);
                    SetCCR (cData);
                    break;

                case 0x6E:     //    "JMP  "
                    ProgramCounter = sOperandPtr;
                    break;

                case 0x6F:     //    "CLR  "
                    cData = 0x00;
                    Memory.StoreMemoryByte (cData, sOperandPtr);
                    SetCCR (cData);
                    break;

                case 0xAC:     //    "CPX  "
                    sData = Memory.LoadMemoryWord(sOperandPtr);
                    sBefore = _xReg;
                    sResult = (ushort)(_xReg - sData);
                    SetCCR (sResult, sBefore);
                    break;

                case 0xAD:     //    "JSR  "

                    // First save current IP

                    Memory.MemorySpace[_sp--] = (byte)(ProgramCounter % 256);
                    Memory.MemorySpace[_sp--] = (byte)(ProgramCounter / 256);

                    // Then load new IP

                    ProgramCounter = sOperandPtr;

                    break;

                case 0xAE:     //    "LDS  "
                    sData = Memory.LoadMemoryWord(sOperandPtr);
                    _sp = sData;
                    SetCCR (_sp);
                    break;

                case 0xAF:     //    "STS  "
                    Memory.StoreMemoryWord(_sp, sOperandPtr);
                    SetCCR (_sp);
                    break;

                case 0xEE:     //    "LDX  "
                    sData = Memory.LoadMemoryWord(sOperandPtr);
                    _xReg = sData;
                    SetCCR(_xReg);
                    break;

                case 0xEF:     //    "STX  "
                    Memory.StoreMemoryWord(_xReg, sOperandPtr);
                    SetCCR(_xReg);
                    break;

                case 0xA0:     //    "SUB R"
                    cData = Memory.LoadMemoryByte(sOperandPtr);
                    _aReg = SubtractRegister(_aReg, (byte)cData);
                    break;
                case 0xA1:     //    "CMP R"
                    cData = Memory.LoadMemoryByte(sOperandPtr);
                    _aReg = CompareRegister(_aReg, (byte)cData);
                    break;
                case 0xA2:     //    "SBC R"
                    cData = Memory.LoadMemoryByte(sOperandPtr);
                    _aReg = SubtractWithCarryRegister(_aReg, (byte)cData);
                    break;
                case 0xA4:     //    "AND R"
                    cData = Memory.LoadMemoryByte(sOperandPtr);
                    _aReg = AndRegister(_aReg, (byte)cData);
                    break;
                case 0xA5:     //    "BIT R"
                    cData = Memory.LoadMemoryByte(sOperandPtr);
                    BitRegister(_aReg, (byte)cData);
                    break;
                case 0xA6:     //    "LDA R"
                    _aReg = Memory.LoadMemoryByte(sOperandPtr);
                    SetCCR(_aReg);
                    break;
                case 0xA7:     //    "STA R"
                    Memory.StoreMemoryByte(_aReg, sOperandPtr);
                    SetCCR(_aReg);
                    break;
                case 0xA8:     //    "EOR R"
                    cData = Memory.LoadMemoryByte(sOperandPtr);
                    _aReg = ExclusiveOrRegister(_aReg, (byte)cData);
                    break;
                case 0xA9:     //    "ADC R"
                    cData = Memory.LoadMemoryByte(sOperandPtr);
                    _aReg = AddWithCarryRegister(_aReg, (byte)cData);
                    break;
                case 0xAA:     //    "ORA R"
                    cData = Memory.LoadMemoryByte(sOperandPtr);
                    _aReg = OrRegister(_aReg, (byte)cData);
                    break;
                case 0xAB:     //    "ADD R"
                    cData = Memory.LoadMemoryByte(sOperandPtr);
                    _aReg = AddRegister(_aReg, (byte)cData);
                    break;
                case 0xE0:     //    "SUB R"
                    cData = Memory.LoadMemoryByte(sOperandPtr);
                    _bReg = SubtractRegister(_bReg, (byte)cData);
                    break;
                case 0xE1:     //    "CMP R"
                    cData = Memory.LoadMemoryByte(sOperandPtr);
                    _bReg = CompareRegister(_bReg, (byte)cData);
                    break;
                case 0xE2:     //    "SBC R"
                    cData = Memory.LoadMemoryByte(sOperandPtr);
                    _bReg = SubtractWithCarryRegister(_bReg, (byte)cData);
                    break;
                case 0xE4:     //    "AND R"
                    cData = Memory.LoadMemoryByte(sOperandPtr);
                    _bReg = AndRegister(_bReg, (byte)cData);
                    break;
                case 0xE5:     //    "BIT R"
                    cData = Memory.LoadMemoryByte(sOperandPtr);
                    BitRegister(_bReg, (byte)cData);
                    break;
                case 0xE6:     //    "LDA R"
                    _bReg = Memory.LoadMemoryByte(sOperandPtr);
                    SetCCR(_bReg);
                    break;
                case 0xE7:     //    "STA R"
                    Memory.StoreMemoryByte(_bReg, sOperandPtr);
                    SetCCR(_bReg);
                    break;
                case 0xE8:     //    "EOR R"
                    cData = Memory.LoadMemoryByte(sOperandPtr);
                    _bReg = ExclusiveOrRegister(_bReg, (byte)cData);
                    break;
                case 0xE9:     //    "ADC R"
                    cData = Memory.LoadMemoryByte(sOperandPtr);
                    _bReg = AddWithCarryRegister(_bReg, (byte)cData);
                    break;
                case 0xEA:     //    "ORA R"
                    cData = Memory.LoadMemoryByte(sOperandPtr);
                    _bReg = OrRegister(_bReg, (byte)cData);
                    break;
                case 0xEB:     //    "ADD R"
                    cData = Memory.LoadMemoryByte(sOperandPtr);
                    _bReg = AddRegister(_bReg, (byte)cData);
                    break;
            }
        }

        private void Execute6800Direct ()
        {
            byte  cData;
            ushort sData;
            ushort sResult;
            ushort sBefore;

            switch (_opCode)
            {
                case 0x9C:  // CPX
                    sData = Memory.LoadMemoryWord(_operand);
                    sBefore = _xReg;
                    sResult = (ushort)(_xReg - sData);
                    SetCCR (sResult, sBefore);
                    break;

                case 0x9E:  // LDS
                    sData = Memory.LoadMemoryWord(_operand);
                    _sp = sData;
                    SetCCR (_sp);
                    break;

                case 0x9F:  // STS
                    Memory.StoreMemoryWord(_sp, _operand);
                    SetCCR (_sp);
                    break;

                case 0xDE:  // LDX
                    sData = Memory.LoadMemoryWord(_operand);
                    _xReg = sData;
                    SetCCR(_xReg);
                    break;

                case 0xDF:  // STX
                    Memory.StoreMemoryWord(_xReg, _operand);
                    SetCCR(_xReg);
                    break;

                case 0x90: //    "SUB R"
                    cData = Memory.LoadMemoryByte(_operand);
                    _aReg = SubtractRegister(_aReg, (byte)cData);
                    break;
                case 0x91: //    "CMP R"
                    cData = Memory.LoadMemoryByte(_operand);
                    _aReg = CompareRegister(_aReg, (byte)cData);
                    break;
                case 0x92: //    "SBC R"
                    cData = Memory.LoadMemoryByte(_operand);
                    _aReg = SubtractWithCarryRegister(_aReg, (byte)cData);
                    break;
                case 0x94: //    "AND R"
                    cData = Memory.LoadMemoryByte(_operand);
                    _aReg = AndRegister(_aReg, (byte)cData);
                    break;
                case 0x95: //    "BIT R"
                    cData = Memory.LoadMemoryByte(_operand);
                    BitRegister(_aReg, (byte)cData);
                    break;
                case 0x96: //    "LDA R"
                    _aReg = Memory.LoadMemoryByte(_operand);
                    SetCCR(_aReg);
                    break;
                case 0x97: //    "STA R"
                    Memory.StoreMemoryByte(_aReg, _operand);
                    SetCCR(_aReg);
                    break;
                case 0x98: //    "EOR R"
                    cData = Memory.LoadMemoryByte(_operand);
                    _aReg = ExclusiveOrRegister(_aReg, (byte)cData);
                    break;
                case 0x99: //    "ADC R"
                    cData = Memory.LoadMemoryByte(_operand);
                    _aReg = AddWithCarryRegister(_aReg, (byte)cData);
                    break;
                case 0x9A: //    "ORA R"
                    cData = Memory.LoadMemoryByte(_operand);
                    _aReg = OrRegister(_aReg, (byte)cData);
                    break;
                case 0x9B: //    "ADD R"
                    cData = Memory.LoadMemoryByte(_operand);
                    _aReg = AddRegister(_aReg, (byte)cData);
                    break;
                case 0xD0: //    "SUB R"
                    cData = Memory.LoadMemoryByte(_operand);
                    _bReg = SubtractRegister(_bReg, (byte)cData);
                    break;
                case 0xD1: //    "CMP R"
                    cData = Memory.LoadMemoryByte(_operand);
                    _bReg = CompareRegister(_bReg, (byte)cData);
                    break;
                case 0xD2: //    "SBC R"
                    cData = Memory.LoadMemoryByte(_operand);
                    _bReg = SubtractWithCarryRegister(_bReg, (byte)cData);
                    break;
                case 0xD4: //    "AND R"
                    cData = Memory.LoadMemoryByte(_operand);
                    _bReg = AndRegister(_bReg, (byte)cData);
                    break;
                case 0xD5: //    "BIT R"
                    cData = Memory.LoadMemoryByte(_operand);
                    BitRegister(_bReg, (byte)cData);
                    break;
                case 0xD6: //    "LDA R"
                    _bReg = Memory.LoadMemoryByte(_operand);
                    SetCCR(_bReg);
                    break;
                case 0xD7: //    "STA R"
                    Memory.StoreMemoryByte(_bReg, _operand);
                    SetCCR(_bReg);
                    break;
                case 0xD8: //    "EOR R"
                    cData = Memory.LoadMemoryByte(_operand);
                    _bReg = ExclusiveOrRegister(_bReg, (byte)cData);
                    break;
                case 0xD9: //    "ADC R"
                    cData = Memory.LoadMemoryByte(_operand);
                    _bReg = AddWithCarryRegister(_bReg, (byte)cData);
                    break;
                case 0xDA: //    "ORA R"
                    cData = Memory.LoadMemoryByte(_operand);
                    _bReg = OrRegister(_bReg, (byte)cData);
                    break;
                case 0xDB: //    "ADD R"
                    cData = Memory.LoadMemoryByte(_operand);
                    _bReg = AddRegister(_bReg, (byte)cData);
                    break;
            }
        }

        private void Execute6800Immediate8 ()
        {
            switch (_opCode)
            {
                case 0x80: //    "SUB R", AM_IMM8_6800,       0,  0, 15, 15, 15, 15, 
                    _aReg = SubtractRegister(_aReg, (byte)_operand);
                    break;
                case 0x81: //    "CMP R", AM_IMM8_6800,       0,  0, 15, 15, 15, 15, 
                    _aReg = CompareRegister(_aReg, (byte)_operand);
                    break;
                case 0x82: //    "SBC R", AM_IMM8_6800,       0,  0, 15, 15, 15, 15, 
                    _aReg = SubtractWithCarryRegister(_aReg, (byte)_operand);
                    break;
                case 0x84: //    "AND R", AM_IMM8_6800,       0,  0, 15, 15, 14,  0, 
                    _aReg = AndRegister(_aReg, (byte)_operand);
                    break;
                case 0x85: //    "BIT R", AM_IMM8_6800,       0,  0, 15, 15, 14,  0, 
                    BitRegister(_aReg, (byte)_operand);
                    break;
                case 0x86: //    "LDA R", AM_IMM8_6800,       0,  0, 15, 15, 14,  0, 
                    _aReg = (byte)_operand;
                    SetCCR(_aReg);
                    break;
                case 0x88: //    "EOR R", AM_IMM8_6800,       0,  0, 15, 15, 14,  0, 
                    _aReg = ExclusiveOrRegister(_aReg, (byte)_operand);
                    break;
                case 0x89: //    "ADC R", AM_IMM8_6800,       15,  0, 15, 15, 15, 15, 
                    _aReg = AddWithCarryRegister(_aReg, (byte)_operand);
                    break;
                case 0x8A: //    "ORA R", AM_IMM8_6800,       0,  0, 15, 15, 14,  0, 
                    _aReg = OrRegister(_aReg, (byte)_operand);
                    break;
                case 0x8B: //    "ADD R", AM_IMM8_6800,       15,  0, 15, 15, 15, 15, 
                    _aReg = AddRegister(_aReg, (byte)_operand);
                    break;
                case 0xC0: //    "SUB R", AM_IMM8_6800,       0,  0, 15, 15, 15, 15, 
                    _bReg = SubtractRegister(_bReg, (byte)_operand);
                    break;
                case 0xC1: //    "CMP R", AM_IMM8_6800,       0,  0, 15, 15, 15, 15, 
                    _bReg = CompareRegister(_bReg, (byte)_operand);
                    break;
                case 0xC2: //    "SBC R", AM_IMM8_6800,       0,  0, 15, 15, 15, 15, 
                    _bReg = SubtractWithCarryRegister(_bReg, (byte)_operand);
                    break;
                case 0xC4: //    "AND R", AM_IMM8_6800,       0,  0, 15, 15, 14,  0, 
                    _bReg = AndRegister(_bReg, (byte)_operand);
                    break;
                case 0xC5: //    "BIT R", AM_IMM8_6800,       0,  0, 15, 15, 14,  0, 
                    BitRegister(_bReg, (byte)_operand);
                    break;
                case 0xC6: //    "LDA R", AM_IMM8_6800,       0,  0, 15, 15, 14,  0, 
                    _bReg = (byte)_operand;
                    SetCCR(_bReg);
                    break;
                case 0xC8: //    "EOR R", AM_IMM8_6800,       0,  0, 15, 15, 14,  0, 
                    _bReg = ExclusiveOrRegister(_bReg, (byte)_operand);
                    break;
                case 0xC9: //    "ADC R", AM_IMM8_6800,       15,  0, 15, 15, 15, 15, 
                    _bReg = AddWithCarryRegister(_bReg, (byte)_operand);
                    break;
                case 0xCA: //    "ORA R", AM_IMM8_6800,       0,  0, 15, 15, 14,  0, 
                    _bReg = OrRegister(_bReg, (byte)_operand);
                    break;
                case 0xCB: //    "ADD R", AM_IMM8_6800,       15,  0, 15, 15, 15, 15, 
                    _bReg = AddRegister(_bReg, (byte)_operand);
                    break;
            }
        }

        private void Execute6800Immediate16 ()
        {
            ushort sResult;
            ushort sBefore;

            switch (_opCode)
            {
                case 0x8C:  // CPX
                    sBefore = _xReg;
                    sResult = (ushort)(_xReg - _operand);
                    SetCCR (sResult, sBefore);
                    break;

                case 0x8E:  // LDS
                    _sp = _operand;
                    SetCCR (_sp);
                    break;

                case 0xCE:  // LDX
                    _xReg = _operand;
                    SetCCR(_xReg);
                    break;
            }
        }

        private void Execute6800Relative ()
        {
            bool bDoBranch = false;

            int nNegative = (_ccr & CCR_NEGATIVE) == 0 ? 0 : 1;
            int nZero     = (_ccr & CCR_ZERO)     == 0 ? 0 : 1;;
            int nOverflow = (_ccr & CCR_OVERFLOW) == 0 ? 0 : 1; ;
            int nCarry    = (_ccr & CCR_CARRY)    == 0 ? 0 : 1;;

            switch (_opCode)
            {
                case 0x8D:      // BSR
                    Memory.MemorySpace[_sp--] = (byte)(ProgramCounter % 256);      // push return address on the stack
                    Memory.MemorySpace[_sp--] = (byte)(ProgramCounter / 256);
                    bDoBranch = true;
                    break;

                case 0x20:      // BRA
                    bDoBranch = true;
                    break;

                case 0x22:      // BHI
                    if ((nCarry | nZero) == 0)
                        bDoBranch = true;
                    break;

                case 0x23:      // BLS
                    if ((nCarry | nZero) == 1)
                        bDoBranch = true;
                    break;

                case 0x24:      // BCC
                    if (nCarry == 0)
                        bDoBranch = true;
                    break;

                case 0x25:      // BCS
                    if (nCarry == 1)
                        bDoBranch = true;
                    break;

                case 0x26:      // BNE
                    if (nZero == 0)
                        bDoBranch = true;
                    break;

                case 0x27:      // BEQ
                    if (nZero == 1)
                        bDoBranch = true;
                    break;

                case 0x28:      // BVC
                    if (nOverflow == 0)
                        bDoBranch = true;
                    break;

                case 0x29:      // BVS
                    if (nOverflow == 1)
                        bDoBranch = true;
                    break;

                case 0x2A:      // BPL
                    if (nNegative == 0)
                        bDoBranch = true;
                    break;

                case 0x2B:      // BMI
                    if (nNegative == 1)
                        bDoBranch = true;
                    break;

                case 0x2C:      // BGE
                    if ((nNegative ^ nOverflow) == 0)
                        bDoBranch = true;
                    break;

                case 0x2D:      // BLT
                    if ((nNegative ^ nOverflow) == 1)
                        bDoBranch = true;
                    break;

                case 0x2E:      // BGT
                    if ((nZero | (nNegative ^ nOverflow)) == 0)
                        bDoBranch = true;
                    break;

                case 0x2F:      // BLE
                    if ((nZero | (nNegative ^ nOverflow)) == 1)
                        bDoBranch = true;
                    break;
            }

            if (bDoBranch == true)
            {
                if (_operand > 127)
                    _operand += 0xFF00;

                ProgramCounter = (ushort)(ProgramCounter + _operand);
            }
        }
        #endregion

        public override byte ReadFromFirst64K (ushort m)
        {
            // always read IO from first 64K segment

            return (Memory.MemorySpace[m]);
        }

        public override void WriteToFirst64K (ushort m, byte b)
        {
            Memory.MemorySpace[m] = b;
        }

        private int counter;
        public override void Run()
        {
            BuildOpCodeTable();

            Thread consoleThread = new Thread(Program._theConsole.run);
            //Program._theConsole.Cpu = this;
                                
            // set up registers to power on state.
                                
            ProgramCounter   = (ushort)(Memory.MemorySpace[0xfffe] * 256 + Memory.MemorySpace[0xffff]);
            _aReg = 0x00;       
            _bReg = 0x00;       
            _xReg = 0x0000;     
                                
            // and start the console
                                
            consoleThread.Start();

            Running = true;
            while (Running)    
            {
                //if (Program.debugMode)
                //{
                //    if ((counter++ % 1000000) == 0)
                //    {
                //        try
                //        {
                //            byte[] buffer = new byte[1024];
                //            try
                //            {
                //                //Console.WriteLine("Waiting for data");
                //                int size = MySocket.Receive(buffer);
                //                if (size > 1)
                //                {
                //                    switch (buffer[0])
                //                    {
                //                        case (byte)'K':
                //                            console._keyboard.StuffKeyboard(Encoding.ASCII.GetString(buffer, 1, size - 1));
                //                            break;
                //                    }
                //                }

                //            }
                //            catch
                //            {
                //                //Console.WriteLine("Nothing to read");
                //            }
                //        }
                //        catch
                //        {
                //            //Console.WriteLine("Nothing to accept");
                //        }
                //    }
                //}

                //m_nIRQAsserted = m_nInterruptRegister;

                ////
                ////      New for the 6800 in Version 5.01
                ////

                //if ((m_nResetPressed | m_nNMIPressed | m_nIRQPressed) != 0)
                //{
                //    if (m_nResetPressed)
                //    {
                //        m_IP  = LoadMemoryWord(0xFFFE);
                //        m_A   = 0x00;
                //        m_B   = 0x00;
                //        m_CCR = 0xD0;
                //        m_X   = 0x0000;
                //        m_SP  = 0x0000;
                //        m_nResetPressed = FALSE;
                //        m_nIRQAsserted = FALSE;
                //        m_bInWait = false;

                //        m_nTraceIndex = 0;
                //        m_nTraceFull = TRUE;
                //    }

                //    if (m_nNMIPressed)
                //    {
                //        m_Memory[m_SP--] = m_IP % 256;
                //        m_Memory[m_SP--] = m_IP / 256;
                //        m_Memory[m_SP--] = m_X % 256;
                //        m_Memory[m_SP--] = m_X / 256;
                //        m_Memory[m_SP--] = m_A;
                //        m_Memory[m_SP--] = m_B;
                //        m_Memory[m_SP--] = m_CCR;
                //        m_CCR |= CCR_INTERRUPT;
                //        m_IP = LoadMemoryWord(0xFFFC);
                //        m_nNMIPressed = FALSE;
                //        m_nIRQPressed = FALSE;
                //        m_bInWait = false;
                //    }

                //    if (m_nIRQPressed && ((m_CCR & CCR_INTERRUPT) == 0))
                //    {
                //        if (!m_bInWait)
                //        {
                //            m_Memory[m_SP--] = m_IP % 256;
                //            m_Memory[m_SP--] = m_IP / 256;
                //            m_Memory[m_SP--] = m_X % 256;
                //            m_Memory[m_SP--] = m_X / 256;
                //            m_Memory[m_SP--] = m_A;
                //            m_Memory[m_SP--] = m_B;
                //            m_Memory[m_SP--] = m_CCR;
                //        }
                //        m_CCR |= CCR_INTERRUPT;
                //        m_IP = LoadMemoryWord(0xFFF8);
                //        m_nIRQPressed = FALSE;
                //        m_nNMIPressed = FALSE;
                //        m_bInWait = false;
                //    }
                //}

                ////
                //// see if there are any IRQ's pending
                ////
                ////      New for the 6800 in Version 5.01

                //if (Program.m_nTraceEnabled)
                //    clsTraceBuffer[m_nTraceIndex].sRegisterInterrupts = m_nIRQAsserted;

                //if (m_nIRQAsserted != 0)
                //{
                //    if ((m_CCR & CCR_INTERRUPT) == 0)
                //    {
                //        if (!m_bInWait)
                //        {
                //            m_Memory[m_SP--] = m_IP % 256;
                //            m_Memory[m_SP--] = m_IP / 256;
                //            m_Memory[m_SP--] = m_X % 256;
                //            m_Memory[m_SP--] = m_X / 256;
                //            m_Memory[m_SP--] = m_A;
                //            m_Memory[m_SP--] = m_B;
                //            m_Memory[m_SP--] = m_CCR;
                //        }
                //        m_CCR |= CCR_INTERRUPT;
                //        m_IP = LoadMemoryWord(0xFFF8);
                //        m_nIRQPressed = FALSE;
                //        m_nNMIPressed = FALSE;
                //        m_bInWait = false;
                //    }
                //}

                ////
                ////      New for the 6800 in Version 5.01
                ////

                //// See if any breakpoints are set

                if (Program._cpu.BreakpointsEnabled)
                {
                    for (int i = 0; i < Program._cpu.BreakpointAddress.Count; i++)
                    {
                        if (Program._cpu.BreakpointAddress[i] == ProgramCounter)
                        {
                            //if (Program.debugMode)
                            //{
                            //    string registers = string.Format("REGS: IP {0} SP {1} A {2} B {3} X {4} CC {5}\n", ProgramCounter.ToString("X4"), _sp.ToString("X4"), _aReg.ToString("X2"), _bReg.ToString("X4"), _xReg.ToString("X4"), _ccr.ToString("X2"));
                            //    byte[] buffer = Encoding.ASCII.GetBytes(registers);
                            //    Program._cpu.MySocket.Send(buffer);
                            //}
                            SingleStepMode = true;
                            break;
                        }
                    }
                }

                if (ResetPressed)
                {
                    ProgramCounter = (ushort)(Memory.MemorySpace[0xfffe] * 256 + Memory.MemorySpace[0xffff]);
                    ResetPressed = false;
                }

                // do instruction execution here

                ushort sCurrentIP = ProgramCounter;

                _opCode = Memory.MemorySpace[ProgramCounter++];

                AddressingModes attribute = opctbl[_opCode].attribute;

                // update it's count

                opctbl[_opCode].lCount++;
                cycles = opctbl[_opCode].cycles;

                //  Get the Operand

                switch (opctbl[_opCode].numbytes)
                {
                    case 1:
                        _operand = 0;
                        break;
                    case 2:
                        _operand = Memory.MemorySpace[ProgramCounter++];
                        break;
                    case 3:
                        _operand  = (ushort)(Memory.MemorySpace[ProgramCounter++] * 256);
                        _operand += Memory.MemorySpace[ProgramCounter++];
                        break;
                }

                _cf = 0;
                _hf = 0;
                _vf = 0;
                _nf = 0;

                switch (attribute)
                {
                    case AddressingModes.AM_INHERENT_6800:
                        Execute6800Inherent ();
                        break;

                    case AddressingModes.AM_DIRECT_6800:
                        Execute6800Direct ();
                        break;

                    case AddressingModes.AM_RELATIVE_6800:
                        Execute6800Relative ();
                        break;

                    case AddressingModes.AM_EXTENDED_6800:
                        Execute6800Extended ();
                        break;

                    case AddressingModes.AM_IMM16_6800:
                        Execute6800Immediate16 ();
                        break;

                    case AddressingModes.AM_IMM8_6800:
                        Execute6800Immediate8 ();
                        break;

                    case AddressingModes.AM_INDEXED_6800:
                        Execute6800Indexed();
                        break;

                    case AddressingModes.AM_ILLEGAL:
                        Running = false;
                        Console.WriteLine("Illegal Addressing Mode detected - aborting - Press any Key");
                        Console.WriteLine("Press any key to exit application");
                        Console.ReadKey();
                        break;
                }

                TotalCycles += cycles;
                CyclesThisPeriod += cycles;
            }                   
        }                       
    }
}
