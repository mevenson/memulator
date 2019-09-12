using System;
using System.Collections.Generic;
using System.Text;

using System.Threading;
using System.Runtime.InteropServices;
using System.IO;

namespace Memulator
{
    class Cpu6809 : Cpu
    {
        public Cpu6809()
        {
            traceFull = false;
            for (int i = 0; i < traceSize; i++)
            {
                clsTraceBuffer[i] = new TraceEntry();
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        struct regs
        {
            [FieldOffset(0)]
            public byte lo;

            [FieldOffset(1)]
            public byte hi;
        };

        private regs _dReg = new regs();

        bool m_nStepOver;

        byte   m_cPostByte;
        byte   m_cPostRegister;
        ushort m_sIndirectPtr;

        int m_Cycles;
        int m_nPostCycles;
        int m_OperandSize;

        ushort      m_Predecrement;
        ushort      m_Postincrement;
        bool        m_Indirect;
        OffsetType  m_OffsetType;

        int m_nTable;
        byte  m_OpCode;
        ushort m_Operand;

        byte   m_DP;        // Direct Page Register         6809 only
        byte   m_CCR;       // Condition Code Register

        ushort m_D
        {
            get { return (ushort)((ushort)(_dReg.hi * 256) + (ushort)(_dReg.lo)); }
            set { _dReg.hi = (byte)(value / 256); _dReg.lo = (byte)(value % 256);  }
        }

        //ushort m_IP;        // Instruction Pointer

        ushort m_CF;
        ushort m_VF;
        ushort m_HF;
        ushort m_NF;

        ushort m_SP;        // Stack Pointer (6800)

        uint SYSTEM_STACK           = 0;
        uint USER_STACK             = 1;

        enum OffsetType
        {
            OFFSET_NONE           =  0,
            OFFSET_REGISTER_A     =  1,
            OFFSET_REGISTER_B     =  2,
            OFFSET_REGISTER_D     =  3,
            OFFSET_8_BIT          =  4,
            OFFSET_16_BIT         =  5,
            OFFSET_PCR_8          =  6,
            OFFSET_PCR_16         =  7,
            EXTENDED_INDIRECT     =  8,
            OFFSET_PREDECREMENT   =  9,
            OFFSET_POSTINCREMENT  = 10,
            OFFSET_5_BIT          = 11,
            OFFSET_INVALID        = -1
        }

        ushort MC6809_NORMAL        = 0;
        ushort MC6809_PAGE_2        = 1;
        ushort MC6809_PAGE_3        = 2;

        ushort GETSTX               = 0;
        ushort GETADDR              = 1;
        ushort GETLENGTH            = 2;
        ushort GETDATA              = 3;
        ushort GETXFER              = 4;

        enum PostBytesIndexes
        {
            POST_X  = 0,
            POST_Y  = 1,
            POST_U  = 2,
            POST_S  = 3
        }

        ushort MP09_DAT_BASE      = 0xFF00;
        ushort MPU1_DAT_BASE      = 0xF400;

        public enum AddressingModes
        {
            AM_PAGE3             = 0x0200,  // 6809 only
            AM_PAGE2             = 0x0100,  // 6809 only

            AM_ILLEGAL           = 0x0080,  // Invalid Op Code

            AM_DIRECT_6809       = 0x0040,  // direct addressing
            AM_RELATIVE_6809     = 0x0020,  // relative addressing
            AM_EXTENDED_6809     = 0x0010,  // extended addressing allowed
            AM_IMM16_6809        = 0x0008,  // 16 bit immediate
            AM_IMM8_6809         = 0x0004,  // 8 bit immediate
            AM_INDEXED_6809      = 0x0002,  // indexed addressing allowed
            AM_INHERENT_6809     = 0x0001,  // inherent addressing

            AM_DIRECT_PAGE2      = 0x0140,  // direct addressing
            AM_RELATIVE_PAGE2    = 0x0120,  // relative addressing
            AM_EXTENDED_PAGE2    = 0x0110,  // extended addressing allowed
            AM_IMM16_PAGE2       = 0x0108,  // 16 bit immediate
            AM_IMM8_PAGE2        = 0x0104,  // 8 bit immediate
            AM_INDEXED_PAGE2     = 0x0102,  // indexed addressing allowed
            AM_INHERENT_PAGE2    = 0x0101,  // inherent addressing

            AM_DIRECT_PAGE3      = 0x0240,  // direct addressing
            AM_RELATIVE_PAGE3    = 0x0220,  // relative addressing
            AM_EXTENDED_PAGE3    = 0x0210,  // extended addressing allowed
            AM_IMM16_PAGE3       = 0x0208,  // 16 bit immediate
            AM_IMM8_PAGE3        = 0x0204,  // 8 bit immediate
            AM_INDEXED_PAGE3     = 0x0202,  // indexed addressing allowed
            AM_INHERENT_PAGE3    = 0x0201   // inherent addressing
        }

        byte CCR_ENTIREFLAG         = 0x80;    // 6809 only
        byte CCR_FIRQMASK           = 0x40;    // 6809 only
        byte CCR_HALFCARRY          = 0x20;
        byte CCR_INTERRUPT          = 0x10;
        byte CCR_NEGATIVE           = 0x08;
        byte CCR_ZERO               = 0x04;
        byte CCR_OVERFLOW           = 0x02;
        byte CCR_CARRY              = 0x01;

        public enum ExecutionStates
        {
            GETOPCODE = 0,
            GETOPERAND = 1,
            EXECOPCODE = 2
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct _6809_OPCTABLEENTRY
        {
            [MarshalAsAttribute(UnmanagedType.U2)]
            public int table;
            [MarshalAsAttribute(UnmanagedType.LPWStr)]
            public string mneumonic;
            [MarshalAsAttribute(UnmanagedType.U1)]
            public byte opval;
            [MarshalAsAttribute(UnmanagedType.U2)]
            public AddressingModes attribute;
            [MarshalAsAttribute(UnmanagedType.U4)]
            public int numbytes;
            [MarshalAsAttribute(UnmanagedType.LPArray)]
            public int[] ccr_rules;
            [MarshalAsAttribute(UnmanagedType.U4)]
            public int cycles;
            [MarshalAsAttribute(UnmanagedType.U8)]
            public long lCount;
            [MarshalAsAttribute(UnmanagedType.U8)]
            public long lCpuTime;
        };

        _6809_OPCTABLEENTRY[,] opctbl = new _6809_OPCTABLEENTRY[3,256];

        private bool _resetPressed = false;

        private static string _coreDumpFile;
        private static string _statDumpFile;

        private void SetOpCodeTableEntry(int index, int table, string mneunonic, byte OpCode, AddressingModes AddrMode, int numbytes, int cycles, int H, int I, int N, int Z, int V, int C, int lcount, long lCPUTime)
        {
            opctbl[index / 256, index % 256].mneumonic = mneunonic;
            opctbl[index / 256, index % 256].table = table;
            opctbl[index / 256, index % 256].opval = OpCode;
            opctbl[index / 256, index % 256].attribute = AddrMode;
            opctbl[index / 256, index % 256].numbytes = numbytes;
            opctbl[index / 256, index % 256].cycles = cycles;
            opctbl[index / 256, index % 256].ccr_rules[0] = H;
            opctbl[index / 256, index % 256].ccr_rules[1] = I;
            opctbl[index / 256, index % 256].ccr_rules[2] = N;
            opctbl[index / 256, index % 256].ccr_rules[3] = Z;
            opctbl[index / 256, index % 256].ccr_rules[4] = V;
            opctbl[index / 256, index % 256].ccr_rules[5] = C;
            opctbl[index / 256, index % 256].lCount = lcount;
        }

        private void BuildOpCodeTable()
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 256; j++)
                    opctbl[i,j].ccr_rules = new int[6];
            }

            //                  idx tbl mneunonic OpCode  AddrMode                     numbytes cyl    H   I   N   Z,  V   C lcount lCPUTime
            SetOpCodeTableEntry(  0, 0, "NEG  ",    0x00, AddressingModes.AM_DIRECT_6809,     2,  6,   0,  0, 15, 15,  1,  2, 0,    0);   // MC6809 X);
            SetOpCodeTableEntry(  1, 0, "~~~~~",    0x01, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809 X);
            SetOpCodeTableEntry(  2, 0, "~~~~~",    0x02, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809 X);
            SetOpCodeTableEntry(  3, 0, "COM  ",    0x03, AddressingModes.AM_DIRECT_6809,     2,  6,   0,  0, 15, 15, 14, 13, 0,    0);   // MC6809 X);
            SetOpCodeTableEntry(  4, 0, "LSR  ",    0x04, AddressingModes.AM_DIRECT_6809,     2,  6,   0,  0, 14, 15,  0, 15, 0,    0);   // MC6809 X);
            SetOpCodeTableEntry(  5, 0, "~~~~~",    0x05, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809 X);
            SetOpCodeTableEntry(  6, 0, "ROR  ",    0x06, AddressingModes.AM_DIRECT_6809,     2,  6,   0,  0, 15, 15,  0, 15, 0,    0);   // MC6809);
            SetOpCodeTableEntry(  7, 0, "ASR  ",    0x07, AddressingModes.AM_DIRECT_6809,     2,  6,   0,  0, 15, 15,  0, 15, 0,    0);   // MC6809 X);
            SetOpCodeTableEntry(  8, 0, "ASL  ",    0x08, AddressingModes.AM_DIRECT_6809,     2,  6,   0,  0, 15, 15,  6, 15, 0,    0);   // MC6809 X);
            SetOpCodeTableEntry(  9, 0, "ROL  ",    0x09, AddressingModes.AM_DIRECT_6809,     2,  6,   0,  0, 15, 15,  6, 15, 0,    0);   // MC6809);
            SetOpCodeTableEntry( 10, 0, "DEC  ",    0x0A, AddressingModes.AM_DIRECT_6809,     2,  6,   0,  0, 15, 15,  4,  0, 0,    0);   // MC6809 X);
            SetOpCodeTableEntry( 11, 0, "~~~~~",    0x0B, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809 X);
            SetOpCodeTableEntry( 12, 0, "INC  ",    0x0C, AddressingModes.AM_DIRECT_6809,     2,  6,   0,  0, 15, 15,  5,  0, 0,    0);   // MC6809 X);
            SetOpCodeTableEntry( 13, 0, "TST  ",    0x0D, AddressingModes.AM_DIRECT_6809,     2,  6,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809);
            SetOpCodeTableEntry( 14, 0, "JMP  ",    0x0E, AddressingModes.AM_DIRECT_6809,     2,  3,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809);
            SetOpCodeTableEntry( 15, 0, "CLR  ",    0x0F, AddressingModes.AM_DIRECT_6809,     2,  6,   0,  0, 14, 13, 14, 14, 0,    0);   // MC6809 X);
            SetOpCodeTableEntry( 16, 1, "     ",    0x10, AddressingModes.AM_PAGE2,           0,  0,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry( 17, 2, "     ",    0x11, AddressingModes.AM_PAGE3,           0,  0,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry( 18, 0, "NOP  ",    0x12, AddressingModes.AM_INHERENT_6809,   1,  2,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry( 19, 0, "SYNC ",    0x13, AddressingModes.AM_INHERENT_6809,   1,  2,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry( 20, 0, "~~~~~",    0x14, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry( 21, 0, "~~~~~",    0x15, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry( 22, 0, "LBRA ",    0x16, AddressingModes.AM_RELATIVE_6809,   3,  5,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry( 23, 0, "LBSR ",    0x17, AddressingModes.AM_RELATIVE_6809,   3,  9,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry( 24, 0, "~~~~~",    0x18, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry( 25, 0, "DAA  ",    0x19, AddressingModes.AM_INHERENT_6809,   1,  2,   0,  0, 15, 15, 15,  3, 0,    0);   // MC6809 X
            SetOpCodeTableEntry( 26, 0, "ORCC ",    0x1A, AddressingModes.AM_IMM8_6809,       2,  3,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry( 27, 0, "~~~~~",    0x1B, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry( 28, 0, "ANDCC",    0x1C, AddressingModes.AM_IMM8_6809,       2,  3,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry( 29, 0, "SEX  ",    0x1D, AddressingModes.AM_INHERENT_6809,   1,  2,   0,  0, 15, 15,  0,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry( 30, 0, "EXG  ",    0x1E, AddressingModes.AM_INHERENT_6809,   2,  8,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry( 31, 0, "TFR  ",    0x1F, AddressingModes.AM_INHERENT_6809,   2,  6,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry( 32, 0, "BRA  ",    0x20, AddressingModes.AM_RELATIVE_6809,   2,  3,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry( 33, 0, "BRN  ",    0x21, AddressingModes.AM_RELATIVE_6809,   2,  3,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry( 34, 0, "BHI  ",    0x22, AddressingModes.AM_RELATIVE_6809,   2,  3,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry( 35, 0, "BLS  ",    0x23, AddressingModes.AM_RELATIVE_6809,   2,  3,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry( 36, 0, "BCC  ",    0x24, AddressingModes.AM_RELATIVE_6809,   2,  3,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry( 37, 0, "BCS  ",    0x25, AddressingModes.AM_RELATIVE_6809,   2,  3,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry( 38, 0, "BNE  ",    0x26, AddressingModes.AM_RELATIVE_6809,   2,  3,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry( 39, 0, "BEQ  ",    0x27, AddressingModes.AM_RELATIVE_6809,   2,  3,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry( 40, 0, "BVC  ",    0x28, AddressingModes.AM_RELATIVE_6809,   2,  3,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry( 41, 0, "BVS  ",    0x29, AddressingModes.AM_RELATIVE_6809,   2,  3,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry( 42, 0, "BPL  ",    0x2A, AddressingModes.AM_RELATIVE_6809,   2,  3,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry( 43, 0, "BMI  ",    0x2B, AddressingModes.AM_RELATIVE_6809,   2,  3,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry( 44, 0, "BGE  ",    0x2C, AddressingModes.AM_RELATIVE_6809,   2,  3,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry( 45, 0, "BLT  ",    0x2D, AddressingModes.AM_RELATIVE_6809,   2,  3,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry( 46, 0, "BGT  ",    0x2E, AddressingModes.AM_RELATIVE_6809,   2,  3,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry( 47, 0, "BLE  ",    0x2F, AddressingModes.AM_RELATIVE_6809,   2,  3,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry( 48, 0, "LEA X",    0x30, AddressingModes.AM_INDEXED_6809,    2,  4,   0,  0,  0, 16,  0,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry( 49, 0, "LEA Y",    0x31, AddressingModes.AM_INDEXED_6809,    2,  4,   0,  0,  0, 16,  0,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry( 50, 0, "LEA S",    0x32, AddressingModes.AM_INDEXED_6809,    2,  4,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry( 51, 0, "LEA U",    0x33, AddressingModes.AM_INDEXED_6809,    2,  4,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry( 52, 0, "PSH S",    0x34, AddressingModes.AM_INHERENT_6809,   2,  5,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809 
            SetOpCodeTableEntry( 53, 0, "PUL S",    0x35, AddressingModes.AM_INHERENT_6809,   2,  5,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809 
            SetOpCodeTableEntry( 54, 0, "PSH U",    0x36, AddressingModes.AM_INHERENT_6809,   2,  5,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809 
            SetOpCodeTableEntry( 55, 0, "PUL U",    0x37, AddressingModes.AM_INHERENT_6809,   2,  5,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809 
            SetOpCodeTableEntry( 56, 0, "~~~~~",    0x38, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry( 57, 0, "RTS  ",    0x39, AddressingModes.AM_INHERENT_6809,   1,  5,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry( 58, 0, "ABX  ",    0x3A, AddressingModes.AM_INHERENT_6809,   1,  3,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry( 59, 0, "RTI  ",    0x3B, AddressingModes.AM_INHERENT_6809,   1,  6,  10, 10, 10, 10, 10, 10, 0,    0);   // MC6809
            SetOpCodeTableEntry( 60, 0, "CWAI ",    0x3C, AddressingModes.AM_IMM8_6809,       2, 20,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry( 61, 0, "MUL  ",    0x3D, AddressingModes.AM_INHERENT_6809,   1, 11,   0,  0,  0, 16,  0, 17, 0,    0);   // MC6809 X
            SetOpCodeTableEntry( 62, 0, "~~~~~",    0x3E, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry( 63, 0, "SWI  ",    0x3F, AddressingModes.AM_INHERENT_6809,   1, 12,   0, 13,  0,  0,  0,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry( 64, 0, "NEG A",    0x40, AddressingModes.AM_INHERENT_6809,   1,  2,   0,  0, 15, 15,  1,  2, 0,    0);   // MC6809 X
            SetOpCodeTableEntry( 65, 0, "~~~~~",    0x41, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry( 66, 0, "~~~~~",    0x42, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry( 67, 0, "COM A",    0x43, AddressingModes.AM_INHERENT_6809,   1,  2,   0,  0, 15, 15, 14, 13, 0,    0);   // MC6809 X
            SetOpCodeTableEntry( 68, 0, "LSR A",    0x44, AddressingModes.AM_INHERENT_6809,   1,  2,   0,  0, 14, 15,  0, 15, 0,    0);   // MC6809 X
            SetOpCodeTableEntry( 69, 0, "~~~~~",    0x45, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry( 70, 0, "ROR A",    0x46, AddressingModes.AM_INHERENT_6809,   1,  2,   0,  0, 15, 15,  0, 15, 0,    0);   // MC6809
            SetOpCodeTableEntry( 71, 0, "ASR A",    0x47, AddressingModes.AM_INHERENT_6809,   1,  2,   0,  0, 15, 15,  0, 15, 0,    0);   // MC6809 X
            SetOpCodeTableEntry( 72, 0, "ASL A",    0x48, AddressingModes.AM_INHERENT_6809,   1,  2,   0,  0, 15, 15,  6, 15, 0,    0);   // MC6809 X
            SetOpCodeTableEntry( 73, 0, "ROL A",    0x49, AddressingModes.AM_INHERENT_6809,   1,  2,   0,  0, 15, 15,  6, 15, 0,    0);   // MC6809
            SetOpCodeTableEntry( 74, 0, "DEC A",    0x4A, AddressingModes.AM_INHERENT_6809,   1,  2,   0,  0, 15, 15,  4,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry( 75, 0, "~~~~~",    0x4B, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry( 76, 0, "INC A",    0x4C, AddressingModes.AM_INHERENT_6809,   1,  2,   0,  0, 15, 15,  5,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry( 77, 0, "TST A",    0x4D, AddressingModes.AM_INHERENT_6809,   1,  2,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry( 78, 0, "~~~~~",    0x4E, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry( 79, 0, "CLR A",    0x4F, AddressingModes.AM_INHERENT_6809,   1,  2,   0,  0, 14, 13, 14, 14, 0,    0);   // MC6809 X
            SetOpCodeTableEntry( 80, 0, "NEG B",    0x50, AddressingModes.AM_INHERENT_6809,   1,  2,   0,  0, 15, 15,  1,  2, 0,    0);   // MC6809 X
            SetOpCodeTableEntry( 81, 0, "~~~~~",    0x51, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry( 82, 0, "~~~~~",    0x52, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry( 83, 0, "COM B",    0x53, AddressingModes.AM_INHERENT_6809,   1,  2,   0,  0, 15, 15, 14, 13, 0,    0);   // MC6809 X
            SetOpCodeTableEntry( 84, 0, "LSR B",    0x54, AddressingModes.AM_INHERENT_6809,   1,  2,   0,  0, 14, 15,  0, 15, 0,    0);   // MC6809 X
            SetOpCodeTableEntry( 85, 0, "~~~~~",    0x55, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry( 86, 0, "ROR B",    0x56, AddressingModes.AM_INHERENT_6809,   1,  2,   0,  0, 15, 15,  0, 15, 0,    0);   // MC6809
            SetOpCodeTableEntry( 87, 0, "ASR B",    0x57, AddressingModes.AM_INHERENT_6809,   1,  2,   0,  0, 15, 15,  0, 15, 0,    0);   // MC6809 X
            SetOpCodeTableEntry( 88, 0, "ASL B",    0x58, AddressingModes.AM_INHERENT_6809,   1,  2,   0,  0, 15, 15,  6, 15, 0,    0);   // MC6809 X
            SetOpCodeTableEntry( 89, 0, "ROL B",    0x59, AddressingModes.AM_INHERENT_6809,   1,  2,   0,  0, 15, 15,  6, 15, 0,    0);   // MC6809
            SetOpCodeTableEntry( 90, 0, "DEC B",    0x5A, AddressingModes.AM_INHERENT_6809,   1,  2,   0,  0, 15, 15,  4,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry( 91, 0, "~~~~~",    0x5B, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry( 92, 0, "INC B",    0x5C, AddressingModes.AM_INHERENT_6809,   1,  2,   0,  0, 15, 15,  5,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry( 93, 0, "TST B",    0x5D, AddressingModes.AM_INHERENT_6809,   1,  2,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry( 94, 0, "~~~~~",    0x5E, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry( 95, 0, "CLR B",    0x5F, AddressingModes.AM_INHERENT_6809,   1,  2,   0,  0, 14, 13, 14, 14, 0,    0);   // MC6809 X
            SetOpCodeTableEntry( 96, 0, "NEG  ",    0x60, AddressingModes.AM_INDEXED_6809,    2,  7,   0,  0, 15, 15,  1,  2, 0,    0);   // MC6809 X
            SetOpCodeTableEntry( 97, 0, "~~~~~",    0x61, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry( 98, 0, "~~~~~",    0x62, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry( 99, 0, "COM  ",    0x63, AddressingModes.AM_INDEXED_6809,    2,  7,   0,  0, 15, 15, 14, 13, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(100, 0, "LSR  ",    0x64, AddressingModes.AM_INDEXED_6809,    2,  7,   0,  0, 14, 15,  0, 15, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(101, 0, "~~~~~",    0x65, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(102, 0, "ROR  ",    0x66, AddressingModes.AM_INDEXED_6809,    2,  7,   0,  0, 15, 15,  0, 15, 0,    0);   // MC6809
            SetOpCodeTableEntry(103, 0, "ASR  ",    0x67, AddressingModes.AM_INDEXED_6809,    2,  7,   0,  0, 15, 15,  0, 15, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(104, 0, "ASL  ",    0x68, AddressingModes.AM_INDEXED_6809,    2,  7,   0,  0, 15, 15,  6, 15, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(105, 0, "ROL  ",    0x69, AddressingModes.AM_INDEXED_6809,    2,  7,   0,  0, 15, 15,  6, 15, 0,    0);   // MC6809
            SetOpCodeTableEntry(106, 0, "DEC  ",    0x6A, AddressingModes.AM_INDEXED_6809,    2,  7,   0,  0, 15, 15,  4,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(107, 0, "~~~~~",    0x6B, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(108, 0, "INC  ",    0x6C, AddressingModes.AM_INDEXED_6809,    2,  7,   0,  0, 15, 15,  5,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(109, 0, "TST  ",    0x6D, AddressingModes.AM_INDEXED_6809,    2,  7,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry(110, 0, "JMP  ",    0x6E, AddressingModes.AM_INDEXED_6809,    2,  4,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry(111, 0, "CLR  ",    0x6F, AddressingModes.AM_INDEXED_6809,    2,  7,   0,  0, 14, 13, 14, 14, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(112, 0, "NEG  ",    0x70, AddressingModes.AM_EXTENDED_6809,   3,  6,   0,  0, 15, 15,  1,  2, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(113, 0, "~~~~~",    0x71, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(114, 0, "~~~~~",    0x72, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(115, 0, "COM  ",    0x73, AddressingModes.AM_EXTENDED_6809,   3,  6,   0,  0, 15, 15, 14, 13, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(116, 0, "LSR  ",    0x74, AddressingModes.AM_EXTENDED_6809,   3,  6,   0,  0, 14, 15,  0, 15, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(117, 0, "~~~~~",    0x75, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(118, 0, "ROR  ",    0x76, AddressingModes.AM_EXTENDED_6809,   3,  6,   0,  0, 15, 15,  0, 15, 0,    0);   // MC6809
            SetOpCodeTableEntry(119, 0, "ASR  ",    0x77, AddressingModes.AM_EXTENDED_6809,   3,  6,   0,  0, 15, 15,  0, 15, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(120, 0, "ASL  ",    0x78, AddressingModes.AM_EXTENDED_6809,   3,  6,   0,  0, 15, 15,  6, 15, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(121, 0, "ROL  ",    0x79, AddressingModes.AM_EXTENDED_6809,   3,  6,   0,  0, 15, 15,  6, 15, 0,    0);   // MC6809
            SetOpCodeTableEntry(122, 0, "DEC  ",    0x7A, AddressingModes.AM_EXTENDED_6809,   3,  6,   0,  0, 15, 15,  4,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(123, 0, "~~~~~",    0x7B, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(124, 0, "INC  ",    0x7C, AddressingModes.AM_EXTENDED_6809,   3,  6,   0,  0, 15, 15,  5,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(125, 0, "TST  ",    0x7D, AddressingModes.AM_EXTENDED_6809,   3,  6,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry(126, 0, "JMP  ",    0x7E, AddressingModes.AM_EXTENDED_6809,   3,  3,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry(127, 0, "CLR  ",    0x7F, AddressingModes.AM_EXTENDED_6809,   3,  6,   0,  0, 14, 13, 14, 14, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(128, 0, "SUB A",    0x80, AddressingModes.AM_IMM8_6809,       2,  2,   0,  0, 15, 15, 15, 15, 0,    0);   // MC6809
            SetOpCodeTableEntry(129, 0, "CMP A",    0x81, AddressingModes.AM_IMM8_6809,       2,  2,   0,  0, 15, 15, 15, 15, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(130, 0, "SBC A",    0x82, AddressingModes.AM_IMM8_6809,       2,  2,   0,  0, 15, 15, 15, 15, 0,    0);   // MC6809
            SetOpCodeTableEntry(131, 0, "SUB D",    0x83, AddressingModes.AM_IMM16_6809,      3,  4,   0,  0, 16, 16, 16, 16, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(132, 0, "AND A",    0x84, AddressingModes.AM_IMM8_6809,       2,  2,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(133, 0, "BIT A",    0x85, AddressingModes.AM_IMM8_6809,       2,  2,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(134, 0, "LDA A",    0x86, AddressingModes.AM_IMM8_6809,       2,  2,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(135, 0, "~~~~~",    0x87, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(136, 0, "EOR A",    0x88, AddressingModes.AM_IMM8_6809,       2,  2,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(137, 0, "ADC A",    0x89, AddressingModes.AM_IMM8_6809,       2,  2,  15,  0, 15, 15, 15, 15, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(138, 0, "ORA A",    0x8A, AddressingModes.AM_IMM8_6809,       2,  2,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry(139, 0, "ADD A",    0x8B, AddressingModes.AM_IMM8_6809,       2,  2,  15,  0, 15, 15, 15, 15, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(140, 0, "CMP X",    0x8C, AddressingModes.AM_IMM16_6809,      3,  3,   0,  0, 16, 16, 16, 16, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(141, 0, "BSR  ",    0x8D, AddressingModes.AM_RELATIVE_6809,   2,  6,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry(142, 0, "LDX  ",    0x8E, AddressingModes.AM_IMM16_6809,      3,  3,   0,  0, 16, 16, 14,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry(143, 0, "~~~~~",    0x8F, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(144, 0, "SUB A",    0x90, AddressingModes.AM_DIRECT_6809,     2,  4,   0,  0, 15, 15, 15, 15, 0,    0);   // MC6809
            SetOpCodeTableEntry(145, 0, "CMP A",    0x91, AddressingModes.AM_DIRECT_6809,     2,  4,   0,  0, 15, 15, 15, 15, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(146, 0, "SBC A",    0x92, AddressingModes.AM_DIRECT_6809,     2,  4,   0,  0, 15, 15, 15, 15, 0,    0);   // MC6809
            SetOpCodeTableEntry(147, 0, "SUB D",    0x93, AddressingModes.AM_DIRECT_6809,     2,  6,   0,  0, 16, 16, 16, 16, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(148, 0, "AND A",    0x94, AddressingModes.AM_DIRECT_6809,     2,  4,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(149, 0, "BIT A",    0x95, AddressingModes.AM_DIRECT_6809,     2,  4,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(150, 0, "LDA A",    0x96, AddressingModes.AM_DIRECT_6809,     2,  4,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(151, 0, "STA A",    0x97, AddressingModes.AM_DIRECT_6809,     2,  4,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry(152, 0, "EOR A",    0x98, AddressingModes.AM_DIRECT_6809,     2,  4,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(153, 0, "ADC A",    0x99, AddressingModes.AM_DIRECT_6809,     2,  4,  15,  0, 15, 15, 15, 15, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(154, 0, "ORA A",    0x9A, AddressingModes.AM_DIRECT_6809,     2,  4,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry(155, 0, "ADD A",    0x9B, AddressingModes.AM_DIRECT_6809,     2,  4,  15,  0, 15, 15, 15, 15, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(156, 0, "CMP X",    0x9C, AddressingModes.AM_DIRECT_6809,     2,  6,   0,  0, 16, 16, 16, 16, 0,    0);   // MC6809
            SetOpCodeTableEntry(157, 0, "JSR  ",    0x9D, AddressingModes.AM_DIRECT_6809,     2,  7,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry(158, 0, "LDX  ",    0x9E, AddressingModes.AM_DIRECT_6809,     2,  5,   0,  0, 16, 16, 14,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry(159, 0, "STX  ",    0x9F, AddressingModes.AM_DIRECT_6809,     2,  5,   0,  0, 16, 16, 14,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry(160, 0, "SUB A",    0xA0, AddressingModes.AM_INDEXED_6809,    2,  4,   0,  0, 15, 15, 15, 15, 0,    0);   // MC6809
            SetOpCodeTableEntry(161, 0, "CMP A",    0xA1, AddressingModes.AM_INDEXED_6809,    2,  4,   0,  0, 15, 15, 15, 15, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(162, 0, "SBC A",    0xA2, AddressingModes.AM_INDEXED_6809,    2,  4,   0,  0, 15, 15, 15, 15, 0,    0);   // MC6809
            SetOpCodeTableEntry(163, 0, "SUB D",    0xA3, AddressingModes.AM_INDEXED_6809,    2,  6,   0,  0, 16, 16, 16, 16, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(164, 0, "AND A",    0xA4, AddressingModes.AM_INDEXED_6809,    2,  4,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(165, 0, "BIT A",    0xA5, AddressingModes.AM_INDEXED_6809,    2,  4,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(166, 0, "LDA A",    0xA6, AddressingModes.AM_INDEXED_6809,    2,  4,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(167, 0, "STA A",    0xA7, AddressingModes.AM_INDEXED_6809,    2,  4,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry(168, 0, "EOR A",    0xA8, AddressingModes.AM_INDEXED_6809,    2,  4,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(169, 0, "ADC A",    0xA9, AddressingModes.AM_INDEXED_6809,    2,  4,  15,  0, 15, 15, 15, 15, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(170, 0, "ORA A",    0xAA, AddressingModes.AM_INDEXED_6809,    2,  4,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry(171, 0, "ADD A",    0xAB, AddressingModes.AM_INDEXED_6809,    2,  4,  15,  0, 15, 15, 15, 15, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(172, 0, "CMP X",    0xAC, AddressingModes.AM_INDEXED_6809,    2,  6,   0,  0, 16, 16, 16, 16, 0,    0);   // MC6809
            SetOpCodeTableEntry(173, 0, "JSR  ",    0xAD, AddressingModes.AM_INDEXED_6809,    2,  7,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry(174, 0, "LDX  ",    0xAE, AddressingModes.AM_INDEXED_6809,    2,  5,   0,  0, 16, 16, 14,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry(175, 0, "STX  ",    0xAF, AddressingModes.AM_INDEXED_6809,    2,  5,   0,  0, 16, 16, 14,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry(176, 0, "SUB A",    0xB0, AddressingModes.AM_EXTENDED_6809,   3,  5,   0,  0, 15, 15, 15, 15, 0,    0);   // MC6809
            SetOpCodeTableEntry(177, 0, "CMP A",    0xB1, AddressingModes.AM_EXTENDED_6809,   3,  5,   0,  0, 15, 15, 15, 15, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(178, 0, "SBC A",    0xB2, AddressingModes.AM_EXTENDED_6809,   3,  5,   0,  0, 15, 15, 15, 15, 0,    0);   // MC6809
            SetOpCodeTableEntry(179, 0, "SUB D",    0xB3, AddressingModes.AM_EXTENDED_6809,   3,  7,   0,  0, 16, 16, 16, 16, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(180, 0, "AND A",    0xB4, AddressingModes.AM_EXTENDED_6809,   3,  5,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(181, 0, "BIT A",    0xB5, AddressingModes.AM_EXTENDED_6809,   3,  5,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(182, 0, "LDA A",    0xB6, AddressingModes.AM_EXTENDED_6809,   3,  5,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(183, 0, "STA A",    0xB7, AddressingModes.AM_EXTENDED_6809,   3,  5,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry(184, 0, "EOR A",    0xB8, AddressingModes.AM_EXTENDED_6809,   3,  5,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(185, 0, "ADC A",    0xB9, AddressingModes.AM_EXTENDED_6809,   3,  5,  15,  0, 15, 15, 15, 15, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(186, 0, "ORA A",    0xBA, AddressingModes.AM_EXTENDED_6809,   3,  5,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry(187, 0, "ADD A",    0xBB, AddressingModes.AM_EXTENDED_6809,   3,  5,  15,  0, 15, 15, 15, 15, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(188, 0, "CMP X",    0xBC, AddressingModes.AM_EXTENDED_6809,   3,  7,   0,  0, 16, 16, 16, 16, 0,    0);   // MC6809
            SetOpCodeTableEntry(189, 0, "JSR  ",    0xBD, AddressingModes.AM_EXTENDED_6809,   3,  8,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry(190, 0, "LDX  ",    0xBE, AddressingModes.AM_EXTENDED_6809,   3,  6,   0,  0, 16, 16, 14,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry(191, 0, "STX  ",    0xBF, AddressingModes.AM_EXTENDED_6809,   3,  6,   0,  0, 16, 16, 14,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry(192, 0, "SUB B",    0xC0, AddressingModes.AM_IMM8_6809,       2,  2,   0,  0, 15, 15, 15, 15, 0,    0);   // MC6809
            SetOpCodeTableEntry(193, 0, "CMP B",    0xC1, AddressingModes.AM_IMM8_6809,       2,  2,   0,  0, 15, 15, 15, 15, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(194, 0, "SBC B",    0xC2, AddressingModes.AM_IMM8_6809,       2,  2,   0,  0, 15, 15, 15, 15, 0,    0);   // MC6809
            SetOpCodeTableEntry(195, 0, "ADD D",    0xC3, AddressingModes.AM_IMM16_6809,      3,  4,   0,  0, 16, 16, 16, 16, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(196, 0, "AND B",    0xC4, AddressingModes.AM_IMM8_6809,       2,  2,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(197, 0, "BIT B",    0xC5, AddressingModes.AM_IMM8_6809,       2,  2,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(198, 0, "LDA B",    0xC6, AddressingModes.AM_IMM8_6809,       2,  2,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(199, 0, "~~~~~",    0xC7, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(200, 0, "EOR B",    0xC8, AddressingModes.AM_IMM8_6809,       2,  2,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(201, 0, "ADC B",    0xC9, AddressingModes.AM_IMM8_6809,       2,  2,  15,  0, 15, 15, 15, 15, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(202, 0, "ORA B",    0xCA, AddressingModes.AM_IMM8_6809,       2,  2,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry(203, 0, "ADD B",    0xCB, AddressingModes.AM_IMM8_6809,       2,  2,  15,  0, 15, 15, 15, 15, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(204, 0, "LDD  ",    0xCC, AddressingModes.AM_IMM16_6809,      3,  3,   0,  0, 16, 16, 14,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(205, 0, "~~~~~",    0xCD, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(206, 0, "LDU  ",    0xCE, AddressingModes.AM_IMM16_6809,      3,  3,   0,  0, 16, 16, 14,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry(207, 0, "~~~~~",    0xCF, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(208, 0, "SUB B",    0xD0, AddressingModes.AM_DIRECT_6809,     2,  4,   0,  0, 15, 15, 15, 15, 0,    0);   // MC6809
            SetOpCodeTableEntry(209, 0, "CMP B",    0xD1, AddressingModes.AM_DIRECT_6809,     2,  4,   0,  0, 15, 15, 15, 15, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(210, 0, "SBC B",    0xD2, AddressingModes.AM_DIRECT_6809,     2,  4,   0,  0, 15, 15, 15, 15, 0,    0);   // MC6809
            SetOpCodeTableEntry(211, 0, "ADD D",    0xD3, AddressingModes.AM_DIRECT_6809,     2,  6,   0,  0, 16, 16, 16, 16, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(212, 0, "AND B",    0xD4, AddressingModes.AM_DIRECT_6809,     2,  4,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(213, 0, "BIT B",    0xD5, AddressingModes.AM_DIRECT_6809,     2,  4,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(214, 0, "LDA B",    0xD6, AddressingModes.AM_DIRECT_6809,     2,  4,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(215, 0, "STA B",    0xD7, AddressingModes.AM_DIRECT_6809,     2,  4,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry(216, 0, "EOR B",    0xD8, AddressingModes.AM_DIRECT_6809,     2,  4,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(217, 0, "ADC B",    0xD9, AddressingModes.AM_DIRECT_6809,     2,  4,  15,  0, 15, 15, 15, 15, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(218, 0, "ORA B",    0xDA, AddressingModes.AM_DIRECT_6809,     2,  4,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry(219, 0, "ADD B",    0xDB, AddressingModes.AM_DIRECT_6809,     2,  4,  15,  0, 15, 15, 15, 15, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(220, 0, "LDD  ",    0xDC, AddressingModes.AM_DIRECT_6809,     2,  5,   0,  0, 16, 16, 14,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(221, 0, "STD  ",    0xDD, AddressingModes.AM_DIRECT_6809,     2,  5,   0,  0, 16, 16, 14,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(222, 0, "LDU  ",    0xDE, AddressingModes.AM_DIRECT_6809,     2,  5,   0,  0, 16, 16, 14,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry(223, 0, "STU  ",    0xDF, AddressingModes.AM_DIRECT_6809,     2,  5,   0,  0, 16, 16, 14,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry(224, 0, "SUB B",    0xE0, AddressingModes.AM_INDEXED_6809,    2,  4,   0,  0, 15, 15, 15, 15, 0,    0);   // MC6809
            SetOpCodeTableEntry(225, 0, "CMP B",    0xE1, AddressingModes.AM_INDEXED_6809,    2,  4,   0,  0, 15, 15, 15, 15, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(226, 0, "SBC B",    0xE2, AddressingModes.AM_INDEXED_6809,    2,  4,   0,  0, 15, 15, 15, 15, 0,    0);   // MC6809
            SetOpCodeTableEntry(227, 0, "ADD D",    0xE3, AddressingModes.AM_INDEXED_6809,    2,  6,   0,  0, 16, 16, 16, 16, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(228, 0, "AND B",    0xE4, AddressingModes.AM_INDEXED_6809,    2,  4,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(229, 0, "BIT B",    0xE5, AddressingModes.AM_INDEXED_6809,    2,  4,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(230, 0, "LDA B",    0xE6, AddressingModes.AM_INDEXED_6809,    2,  4,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(231, 0, "STA B",    0xE7, AddressingModes.AM_INDEXED_6809,    2,  4,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry(232, 0, "EOR B",    0xE8, AddressingModes.AM_INDEXED_6809,    2,  4,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(233, 0, "ADC B",    0xE9, AddressingModes.AM_INDEXED_6809,    2,  4,  15,  0, 15, 15, 15, 15, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(234, 0, "ORA B",    0xEA, AddressingModes.AM_INDEXED_6809,    2,  4,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry(235, 0, "ADD B",    0xEB, AddressingModes.AM_INDEXED_6809,    2,  4,  15,  0, 15, 15, 15, 15, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(236, 0, "LDD  ",    0xEC, AddressingModes.AM_INDEXED_6809,    2,  5,   0,  0, 16, 16, 14,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(237, 0, "STD  ",    0xED, AddressingModes.AM_INDEXED_6809,    2,  5,   0,  0, 16, 16, 14,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(238, 0, "LDU  ",    0xEE, AddressingModes.AM_INDEXED_6809,    2,  5,   0,  0, 16, 16, 14,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry(239, 0, "STU  ",    0xEF, AddressingModes.AM_INDEXED_6809,    2,  5,   0,  0, 16, 16, 14,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry(240, 0, "SUB B",    0xF0, AddressingModes.AM_EXTENDED_6809,   3,  5,   0,  0, 15, 15, 15, 15, 0,    0);   // MC6809
            SetOpCodeTableEntry(241, 0, "CMP B",    0xF1, AddressingModes.AM_EXTENDED_6809,   3,  5,   0,  0, 15, 15, 15, 15, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(242, 0, "SBC B",    0xF2, AddressingModes.AM_EXTENDED_6809,   3,  5,   0,  0, 15, 15, 15, 15, 0,    0);   // MC6809
            SetOpCodeTableEntry(243, 0, "ADD D",    0xF3, AddressingModes.AM_EXTENDED_6809,   3,  7,   0,  0, 16, 16, 16, 16, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(244, 0, "AND B",    0xF4, AddressingModes.AM_EXTENDED_6809,   3,  5,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(245, 0, "BIT B",    0xF5, AddressingModes.AM_EXTENDED_6809,   3,  5,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(246, 0, "LDA B",    0xF6, AddressingModes.AM_EXTENDED_6809,   3,  5,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(247, 0, "STA B",    0xF7, AddressingModes.AM_EXTENDED_6809,   3,  5,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry(248, 0, "EOR B",    0xF8, AddressingModes.AM_EXTENDED_6809,   3,  5,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(249, 0, "ADC B",    0xF9, AddressingModes.AM_EXTENDED_6809,   3,  5,  15,  0, 15, 15, 15, 15, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(250, 0, "ORA B",    0xFA, AddressingModes.AM_EXTENDED_6809,   3,  5,   0,  0, 15, 15, 14,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry(251, 0, "ADD B",    0xFB, AddressingModes.AM_EXTENDED_6809,   3,  5,  15,  0, 15, 15, 15, 15, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(252, 0, "LDD  ",    0xFC, AddressingModes.AM_EXTENDED_6809,   3,  6,   0,  0, 16, 16, 14,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(253, 0, "STD  ",    0xFD, AddressingModes.AM_EXTENDED_6809,   3,  6,   0,  0, 16, 16, 14,  0, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(254, 0, "LDU  ",    0xFE, AddressingModes.AM_EXTENDED_6809,   3,  6,   0,  0, 16, 16, 14,  0, 0,    0);   // MC6809
            SetOpCodeTableEntry(255, 0, "STU  ",    0xFF, AddressingModes.AM_EXTENDED_6809,   3,  6,   0,  0, 16, 16, 14,  0, 0,    0);   // MC6809
    

            // PAGE 2 MC6809 OPCODES

            SetOpCodeTableEntry(256, 1, "~~~~~",    0x00, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(257, 1, "~~~~~",    0x01, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(258, 1, "~~~~~",    0x02, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(259, 1, "~~~~~",    0x03, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(260, 1, "~~~~~",    0x04, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(261, 1, "~~~~~",    0x05, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(262, 1, "~~~~~",    0x06, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(263, 1, "~~~~~",    0x07, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(264, 1, "~~~~~",    0x08, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(265, 1, "~~~~~",    0x09, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(266, 1, "~~~~~",    0x0A, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(267, 1, "~~~~~",    0x0B, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(268, 1, "~~~~~",    0x0C, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(269, 1, "~~~~~",    0x0D, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(270, 1, "~~~~~",    0x0E, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(271, 1, "~~~~~",    0x0F, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(272, 1, "~~~~~",    0x10, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(273, 1, "~~~~~",    0x11, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(274, 1, "~~~~~",    0x12, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(275, 1, "~~~~~",    0x13, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(276, 1, "~~~~~",    0x14, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(277, 1, "~~~~~",    0x15, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(278, 1, "~~~~~",    0x16, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(279, 1, "~~~~~",    0x17, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(280, 1, "~~~~~",    0x18, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(281, 1, "~~~~~",    0x19, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(282, 1, "~~~~~",    0x1A, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(283, 1, "~~~~~",    0x1B, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(284, 1, "~~~~~",    0x1C, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(285, 1, "~~~~~",    0x1D, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(286, 1, "~~~~~",    0x1E, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(287, 1, "~~~~~",    0x1F, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(288, 1, "~~~~~",    0x20, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(289, 1, "LBRN ",    0x21, AddressingModes.AM_RELATIVE_PAGE2,  4,  5,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2
            SetOpCodeTableEntry(290, 1, "LBHI ",    0x22, AddressingModes.AM_RELATIVE_PAGE2,  4,  5,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2
            SetOpCodeTableEntry(291, 1, "LBLS ",    0x23, AddressingModes.AM_RELATIVE_PAGE2,  4,  5,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2
            SetOpCodeTableEntry(292, 1, "LBCC ",    0x24, AddressingModes.AM_RELATIVE_PAGE2,  4,  5,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2
            SetOpCodeTableEntry(293, 1, "LBCS ",    0x25, AddressingModes.AM_RELATIVE_PAGE2,  4,  5,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2
            SetOpCodeTableEntry(294, 1, "LBNE ",    0x26, AddressingModes.AM_RELATIVE_PAGE2,  4,  5,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2
            SetOpCodeTableEntry(295, 1, "LBEQ ",    0x27, AddressingModes.AM_RELATIVE_PAGE2,  4,  5,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2
            SetOpCodeTableEntry(296, 1, "LBVC ",    0x28, AddressingModes.AM_RELATIVE_PAGE2,  4,  5,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2
            SetOpCodeTableEntry(297, 1, "LBVS ",    0x29, AddressingModes.AM_RELATIVE_PAGE2,  4,  5,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2
            SetOpCodeTableEntry(298, 1, "LBPL ",    0x2A, AddressingModes.AM_RELATIVE_PAGE2,  4,  5,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2
            SetOpCodeTableEntry(299, 1, "LBMI ",    0x2B, AddressingModes.AM_RELATIVE_PAGE2,  4,  5,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2
            SetOpCodeTableEntry(300, 1, "LBGE ",    0x2C, AddressingModes.AM_RELATIVE_PAGE2,  4,  5,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2
            SetOpCodeTableEntry(301, 1, "LBLT ",    0x2D, AddressingModes.AM_RELATIVE_PAGE2,  4,  5,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2
            SetOpCodeTableEntry(302, 1, "LBGT ",    0x2E, AddressingModes.AM_RELATIVE_PAGE2,  4,  5,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2
            SetOpCodeTableEntry(303, 1, "LBLE ",    0x2F, AddressingModes.AM_RELATIVE_PAGE2,  4,  5,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2
            SetOpCodeTableEntry(304, 1, "~~~~~",    0x30, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(305, 1, "~~~~~",    0x31, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(306, 1, "~~~~~",    0x32, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(307, 1, "~~~~~",    0x33, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(308, 1, "~~~~~",    0x34, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(309, 1, "~~~~~",    0x35, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(310, 1, "~~~~~",    0x36, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(311, 1, "~~~~~",    0x37, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(312, 1, "~~~~~",    0x38, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(313, 1, "~~~~~",    0x39, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(314, 1, "~~~~~",    0x3A, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(315, 1, "~~~~~",    0x3B, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(316, 1, "~~~~~",    0x3C, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(317, 1, "~~~~~",    0x3D, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(318, 1, "~~~~~",    0x3E, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(319, 1, "SWI2 ",    0x3F, AddressingModes.AM_INHERENT_PAGE2,  2, 20,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2
            SetOpCodeTableEntry(320, 1, "~~~~~",    0x40, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(321, 1, "~~~~~",    0x41, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(322, 1, "~~~~~",    0x42, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(323, 1, "~~~~~",    0x43, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(324, 1, "~~~~~",    0x44, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(325, 1, "~~~~~",    0x45, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(326, 1, "~~~~~",    0x46, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(327, 1, "~~~~~",    0x47, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(328, 1, "~~~~~",    0x48, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(329, 1, "~~~~~",    0x49, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(330, 1, "~~~~~",    0x4A, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(331, 1, "~~~~~",    0x4B, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(332, 1, "~~~~~",    0x4C, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(333, 1, "~~~~~",    0x4D, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(334, 1, "~~~~~",    0x4E, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(335, 1, "~~~~~",    0x4F, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(336, 1, "~~~~~",    0x50, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(337, 1, "~~~~~",    0x51, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(338, 1, "~~~~~",    0x52, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(339, 1, "~~~~~",    0x53, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(340, 1, "~~~~~",    0x54, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(341, 1, "~~~~~",    0x55, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(342, 1, "~~~~~",    0x56, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(343, 1, "~~~~~",    0x57, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(344, 1, "~~~~~",    0x58, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(345, 1, "~~~~~",    0x59, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(346, 1, "~~~~~",    0x5A, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(347, 1, "~~~~~",    0x5B, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(348, 1, "~~~~~",    0x5C, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(349, 1, "~~~~~",    0x5D, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(350, 1, "~~~~~",    0x5E, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(351, 1, "~~~~~",    0x5F, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(352, 1, "~~~~~",    0x60, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(353, 1, "~~~~~",    0x61, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(354, 1, "~~~~~",    0x62, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(355, 1, "~~~~~",    0x63, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(356, 1, "~~~~~",    0x64, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(357, 1, "~~~~~",    0x65, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(358, 1, "~~~~~",    0x66, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(359, 1, "~~~~~",    0x67, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(360, 1, "~~~~~",    0x68, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(361, 1, "~~~~~",    0x69, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(362, 1, "~~~~~",    0x6A, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(363, 1, "~~~~~",    0x6B, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(364, 1, "~~~~~",    0x6C, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(365, 1, "~~~~~",    0x6D, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(366, 1, "~~~~~",    0x6E, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(367, 1, "~~~~~",    0x6F, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(368, 1, "~~~~~",    0x70, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(369, 1, "~~~~~",    0x71, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(370, 1, "~~~~~",    0x72, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(371, 1, "~~~~~",    0x73, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(372, 1, "~~~~~",    0x74, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(373, 1, "~~~~~",    0x75, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(374, 1, "~~~~~",    0x76, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(375, 1, "~~~~~",    0x77, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(376, 1, "~~~~~",    0x78, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(377, 1, "~~~~~",    0x79, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(378, 1, "~~~~~",    0x7A, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(379, 1, "~~~~~",    0x7B, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(380, 1, "~~~~~",    0x7C, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(381, 1, "~~~~~",    0x7D, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(382, 1, "~~~~~",    0x7E, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(383, 1, "~~~~~",    0x7F, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(384, 1, "~~~~~",    0x80, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(385, 1, "~~~~~",    0x81, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(386, 1, "~~~~~",    0x82, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(387, 1, "CMP D",    0x83, AddressingModes.AM_IMM16_PAGE2,     4,  5,   0,  0, 16, 16, 16, 16, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(388, 1, "~~~~~",    0x84, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(389, 1, "~~~~~",    0x85, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(390, 1, "~~~~~",    0x86, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(391, 1, "~~~~~",    0x87, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(392, 1, "~~~~~",    0x88, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(393, 1, "~~~~~",    0x89, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(394, 1, "~~~~~",    0x8A, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(395, 1, "~~~~~",    0x8B, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(396, 1, "CMP Y",    0x8C, AddressingModes.AM_IMM16_PAGE2,     4,  5,   0,  0, 16, 16, 16, 16, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(397, 1, "~~~~~",    0x8D, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(398, 1, "LDY  ",    0x8E, AddressingModes.AM_IMM16_PAGE2,     4,  4,   0,  0, 16, 16, 14,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(499, 1, "~~~~~",    0x8F, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(400, 1, "~~~~~",    0x90, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(401, 1, "~~~~~",    0x91, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(402, 1, "~~~~~",    0x92, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(403, 1, "CMP D",    0x93, AddressingModes.AM_DIRECT_PAGE2,    3,  7,   0,  0, 16, 16, 16, 16, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(404, 1, "~~~~~",    0x94, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(405, 1, "~~~~~",    0x95, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(406, 1, "~~~~~",    0x96, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(407, 1, "~~~~~",    0x97, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(408, 1, "~~~~~",    0x98, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(409, 1, "~~~~~",    0x99, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(410, 1, "~~~~~",    0x9A, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(411, 1, "~~~~~",    0x9B, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(412, 1, "CMP Y",    0x9C, AddressingModes.AM_DIRECT_PAGE2,    3,  7,   0,  0, 16, 16, 16, 16, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(413, 1, "~~~~~",    0x9D, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(414, 1, "LDY  ",    0x9E, AddressingModes.AM_DIRECT_PAGE2,    3,  6,   0,  0, 16, 16, 14,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(415, 1, "STY  ",    0x9F, AddressingModes.AM_DIRECT_PAGE2,    3,  6,   0,  0, 16, 16, 14,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(416, 1, "~~~~~",    0xA0, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(417, 1, "~~~~~",    0xA1, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(418, 1, "~~~~~",    0xA2, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(419, 1, "CMP D",    0xA3, AddressingModes.AM_INDEXED_PAGE2,   3,  7,   0,  0, 16, 16, 16, 16, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(420, 1, "~~~~~",    0xA4, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(421, 1, "~~~~~",    0xA5, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(422, 1, "~~~~~",    0xA6, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(423, 1, "~~~~~",    0xA7, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(424, 1, "~~~~~",    0xA8, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(425, 1, "~~~~~",    0xA9, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(426, 1, "~~~~~",    0xAA, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(427, 1, "~~~~~",    0xAB, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(428, 1, "CMP Y",    0xAC, AddressingModes.AM_INDEXED_PAGE2,   3,  7,   0,  0, 16, 16, 16, 16, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(429, 1, "~~~~~",    0xAD, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(430, 1, "LDY  ",    0xAE, AddressingModes.AM_INDEXED_PAGE2,   3,  6,   0,  0, 16, 16, 14,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(431, 1, "STY  ",    0xAF, AddressingModes.AM_INDEXED_PAGE2,   3,  6,   0,  0, 16, 16, 14,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(432, 1, "~~~~~",    0xB0, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(433, 1, "~~~~~",    0xB1, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(434, 1, "~~~~~",    0xB2, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(435, 1, "CMP D",    0xB3, AddressingModes.AM_EXTENDED_PAGE2,  4,  8,   0,  0, 16, 16, 16, 16, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(436, 1, "~~~~~",    0xB4, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(437, 1, "~~~~~",    0xB5, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(438, 1, "~~~~~",    0xB6, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(439, 1, "~~~~~",    0xB7, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(440, 1, "~~~~~",    0xB8, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(441, 1, "~~~~~",    0xB9, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(442, 1, "~~~~~",    0xBA, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(443, 1, "~~~~~",    0xBB, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(444, 1, "CMP Y",    0xBC, AddressingModes.AM_EXTENDED_PAGE2,  4,  8,   0,  0, 16, 16, 16, 16, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(445, 1, "~~~~~",    0xBD, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(446, 1, "LDY  ",    0xBE, AddressingModes.AM_EXTENDED_PAGE2,  4,  7,   0,  0, 16, 16, 14,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(447, 1, "STY  ",    0xBF, AddressingModes.AM_EXTENDED_PAGE2,  4,  7,   0,  0, 16, 16, 14,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(448, 1, "~~~~~",    0xC0, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(449, 1, "~~~~~",    0xC1, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(450, 1, "~~~~~",    0xC2, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(451, 1, "~~~~~",    0xC3, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(452, 1, "~~~~~",    0xC4, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(453, 1, "~~~~~",    0xC5, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(454, 1, "~~~~~",    0xC6, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(455, 1, "~~~~~",    0xC7, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(456, 1, "~~~~~",    0xC8, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(457, 1, "~~~~~",    0xC9, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(458, 1, "~~~~~",    0xCA, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(459, 1, "~~~~~",    0xCB, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(460, 1, "~~~~~",    0xCC, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(461, 1, "~~~~~",    0xCD, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(462, 1, "LDS  ",    0xCE, AddressingModes.AM_IMM16_PAGE2,     4,  4,   0,  0, 16, 16, 14,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(463, 1, "~~~~~",    0xCF, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(464, 1, "~~~~~",    0xD0, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(465, 1, "~~~~~",    0xD1, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(466, 1, "~~~~~",    0xD2, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(467, 1, "~~~~~",    0xD3, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(468, 1, "~~~~~",    0xD4, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(469, 1, "~~~~~",    0xD5, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(470, 1, "~~~~~",    0xD6, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(471, 1, "~~~~~",    0xD7, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(472, 1, "~~~~~",    0xD8, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(473, 1, "~~~~~",    0xD9, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(474, 1, "~~~~~",    0xDA, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(475, 1, "~~~~~",    0xDB, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(476, 1, "~~~~~",    0xDC, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(477, 1, "~~~~~",    0xDD, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(478, 1, "LDS  ",    0xDE, AddressingModes.AM_DIRECT_PAGE2,    3,  6,   0,  0, 16, 16, 14,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(479, 1, "STS  ",    0xDF, AddressingModes.AM_DIRECT_PAGE2,    3,  6,   0,  0, 16, 16, 14,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(480, 1, "~~~~~",    0xE0, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(481, 1, "~~~~~",    0xE1, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(482, 1, "~~~~~",    0xE2, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(483, 1, "~~~~~",    0xE3, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(484, 1, "~~~~~",    0xE4, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(485, 1, "~~~~~",    0xE5, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(486, 1, "~~~~~",    0xE6, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(487, 1, "~~~~~",    0xE7, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(488, 1, "~~~~~",    0xE8, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(489, 1, "~~~~~",    0xE9, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(490, 1, "~~~~~",    0xEA, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(491, 1, "~~~~~",    0xEB, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(492, 1, "~~~~~",    0xEC, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(493, 1, "~~~~~",    0xED, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(494, 1, "LDS  ",    0xEE, AddressingModes.AM_INDEXED_PAGE2,   3,  6,   0,  0, 16, 16, 14,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(495, 1, "STS  ",    0xEF, AddressingModes.AM_INDEXED_PAGE2,   3,  6,   0,  0, 16, 16, 14,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(496, 1, "~~~~~",    0xF0, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(497, 1, "~~~~~",    0xF1, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(498, 1, "~~~~~",    0xF2, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(499, 1, "~~~~~",    0xF3, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(500, 1, "~~~~~",    0xF4, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(501, 1, "~~~~~",    0xF5, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(502, 1, "~~~~~",    0xF6, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(503, 1, "~~~~~",    0xF7, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(504, 1, "~~~~~",    0xF8, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(505, 1, "~~~~~",    0xF9, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(506, 1, "~~~~~",    0xFA, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(507, 1, "~~~~~",    0xFB, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(508, 1, "~~~~~",    0xFC, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(509, 1, "~~~~~",    0xFD, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(510, 1, "LDS  ",    0xFE, AddressingModes.AM_EXTENDED_PAGE2,  4,  7,   0,  0, 16, 16, 14,  0, 0,    0);   // PAGE 2 X
            SetOpCodeTableEntry(511, 1, "STS  ",    0xFF, AddressingModes.AM_EXTENDED_PAGE2,  4,  7,   0,  0, 16, 16, 14,  0, 0,    0);   // PAGE 2 X

            // PAGE 3 MC6809 OPCODES

            SetOpCodeTableEntry(512, 2, "~~~~~",    0x00, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(513, 2, "~~~~~",    0x01, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(514, 2, "~~~~~",    0x02, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(515, 2, "~~~~~",    0x03, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(516, 2, "~~~~~",    0x04, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(517, 2, "~~~~~",    0x05, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(518, 2, "~~~~~",    0x06, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(519, 2, "~~~~~",    0x07, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(520, 2, "~~~~~",    0x08, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(521, 2, "~~~~~",    0x09, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(522, 2, "~~~~~",    0x0A, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(523, 2, "~~~~~",    0x0B, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(524, 2, "~~~~~",    0x0C, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(525, 2, "~~~~~",    0x0D, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(526, 2, "~~~~~",    0x0E, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(527, 2, "~~~~~",    0x0F, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(528, 2, "~~~~~",    0x10, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(529, 2, "~~~~~",    0x11, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(530, 2, "~~~~~",    0x12, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(531, 2, "~~~~~",    0x13, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(532, 2, "~~~~~",    0x14, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(533, 2, "~~~~~",    0x15, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(534, 2, "~~~~~",    0x16, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(535, 2, "~~~~~",    0x17, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(536, 2, "~~~~~",    0x18, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(537, 2, "~~~~~",    0x19, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(538, 2, "~~~~~",    0x1A, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(539, 2, "~~~~~",    0x1B, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(540, 2, "~~~~~",    0x1C, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(541, 2, "~~~~~",    0x1D, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(542, 2, "~~~~~",    0x1E, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(543, 2, "~~~~~",    0x1F, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(544, 2, "~~~~~",    0x20, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(545, 2, "~~~~~",    0x21, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(546, 2, "~~~~~",    0x22, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(547, 2, "~~~~~",    0x23, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(548, 2, "~~~~~",    0x24, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(549, 2, "~~~~~",    0x25, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(550, 2, "~~~~~",    0x26, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(551, 2, "~~~~~",    0x27, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(552, 2, "~~~~~",    0x28, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(553, 2, "~~~~~",    0x29, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(554, 2, "~~~~~",    0x2A, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(555, 2, "~~~~~",    0x2B, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(556, 2, "~~~~~",    0x2C, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(557, 2, "~~~~~",    0x2D, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(558, 2, "~~~~~",    0x2E, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(559, 2, "~~~~~",    0x2F, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(560, 2, "~~~~~",    0x30, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(561, 2, "~~~~~",    0x31, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(562, 2, "~~~~~",    0x32, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(563, 2, "~~~~~",    0x33, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(564, 2, "~~~~~",    0x34, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(565, 2, "~~~~~",    0x35, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(566, 2, "~~~~~",    0x36, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(567, 2, "~~~~~",    0x37, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(568, 2, "~~~~~",    0x38, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(569, 2, "~~~~~",    0x39, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(570, 2, "~~~~~",    0x3A, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(571, 2, "~~~~~",    0x3B, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(572, 2, "~~~~~",    0x3C, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(573, 2, "~~~~~",    0x3D, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(574, 2, "~~~~~",    0x3E, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(575, 2, "SWI3 ",    0x3F, AddressingModes.AM_INHERENT_PAGE3,  2, 20,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3
            SetOpCodeTableEntry(576, 2, "~~~~~",    0x40, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(577, 2, "~~~~~",    0x41, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(578, 2, "~~~~~",    0x42, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(579, 2, "~~~~~",    0x43, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(580, 2, "~~~~~",    0x44, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(581, 2, "~~~~~",    0x45, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(582, 2, "~~~~~",    0x46, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(583, 2, "~~~~~",    0x47, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(584, 2, "~~~~~",    0x48, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(585, 2, "~~~~~",    0x49, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(586, 2, "~~~~~",    0x4A, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(587, 2, "~~~~~",    0x4B, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(588, 2, "~~~~~",    0x4C, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(589, 2, "~~~~~",    0x4D, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(590, 2, "~~~~~",    0x4E, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(591, 2, "~~~~~",    0x4F, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(592, 2, "~~~~~",    0x50, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(593, 2, "~~~~~",    0x51, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(594, 2, "~~~~~",    0x52, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(595, 2, "~~~~~",    0x53, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(596, 2, "~~~~~",    0x54, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(597, 2, "~~~~~",    0x55, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(598, 2, "~~~~~",    0x56, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(599, 2, "~~~~~",    0x57, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(600, 2, "~~~~~",    0x58, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(601, 2, "~~~~~",    0x59, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(602, 2, "~~~~~",    0x5A, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(603, 2, "~~~~~",    0x5B, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(604, 2, "~~~~~",    0x5C, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(605, 2, "~~~~~",    0x5D, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(606, 2, "~~~~~",    0x5E, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(607, 2, "~~~~~",    0x5F, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(608, 2, "~~~~~",    0x60, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(609, 2, "~~~~~",    0x61, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(610, 2, "~~~~~",    0x62, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(611, 2, "~~~~~",    0x63, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(612, 2, "~~~~~",    0x64, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(613, 2, "~~~~~",    0x65, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(614, 2, "~~~~~",    0x66, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(615, 2, "~~~~~",    0x67, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(616, 2, "~~~~~",    0x68, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(617, 2, "~~~~~",    0x69, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(618, 2, "~~~~~",    0x6A, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(619, 2, "~~~~~",    0x6B, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(620, 2, "~~~~~",    0x6C, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(621, 2, "~~~~~",    0x6D, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(622, 2, "~~~~~",    0x6E, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(623, 2, "~~~~~",    0x6F, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(624, 2, "~~~~~",    0x70, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(625, 2, "~~~~~",    0x71, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(626, 2, "~~~~~",    0x72, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(627, 2, "~~~~~",    0x73, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(628, 2, "~~~~~",    0x74, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(629, 2, "~~~~~",    0x75, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(630, 2, "~~~~~",    0x76, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(631, 2, "~~~~~",    0x77, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(632, 2, "~~~~~",    0x78, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(633, 2, "~~~~~",    0x79, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(634, 2, "~~~~~",    0x7A, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(635, 2, "~~~~~",    0x7B, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(636, 2, "~~~~~",    0x7C, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(637, 2, "~~~~~",    0x7D, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(638, 2, "~~~~~",    0x7E, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(639, 2, "~~~~~",    0x7F, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(640, 2, "~~~~~",    0x80, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(641, 2, "~~~~~",    0x81, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(642, 2, "~~~~~",    0x82, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(643, 2, "CMP U",    0x83, AddressingModes.AM_IMM16_PAGE3,     4,  5,   0,  0, 16, 16, 16, 16, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(644, 2, "~~~~~",    0x84, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(645, 2, "~~~~~",    0x85, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(646, 2, "~~~~~",    0x86, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(647, 2, "~~~~~",    0x87, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(648, 2, "~~~~~",    0x88, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(649, 2, "~~~~~",    0x89, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(650, 2, "~~~~~",    0x8A, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(651, 2, "~~~~~",    0x8B, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(652, 2, "CMP S",    0x8C, AddressingModes.AM_IMM16_PAGE3,     4,  5,   0,  0, 16, 16, 16, 16, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(653, 2, "~~~~~",    0x8D, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(654, 2, "~~~~~",    0x8E, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(655, 2, "~~~~~",    0x8F, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(656, 2, "~~~~~",    0x90, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(657, 2, "~~~~~",    0x91, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(658, 2, "~~~~~",    0x92, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(659, 2, "CMP U",    0x93, AddressingModes.AM_DIRECT_PAGE3,    3,  7,   0,  0, 16, 16, 16, 16, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(660, 2, "~~~~~",    0x94, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(661, 2, "~~~~~",    0x95, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(662, 2, "~~~~~",    0x96, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(663, 2, "~~~~~",    0x97, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(664, 2, "~~~~~",    0x98, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(665, 2, "~~~~~",    0x99, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(666, 2, "~~~~~",    0x9A, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(667, 2, "~~~~~",    0x9B, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(668, 2, "CMP S",    0x9C, AddressingModes.AM_DIRECT_PAGE3,    3,  7,   0,  0, 16, 16, 16, 16, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(669, 2, "~~~~~",    0x9D, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(670, 2, "~~~~~",    0x9E, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(671, 2, "~~~~~",    0x9F, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(672, 2, "~~~~~",    0xA0, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(673, 2, "~~~~~",    0xA1, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(674, 2, "~~~~~",    0xA2, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(675, 2, "CMP U",    0xA3, AddressingModes.AM_INDEXED_PAGE3,   3,  7,   0,  0, 16, 16, 16, 16, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(676, 2, "~~~~~",    0xA4, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(677, 2, "~~~~~",    0xA5, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(678, 2, "~~~~~",    0xA6, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(679, 2, "~~~~~",    0xA7, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(680, 2, "~~~~~",    0xA8, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(681, 2, "~~~~~",    0xA9, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(682, 2, "~~~~~",    0xAA, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(683, 2, "~~~~~",    0xAB, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(684, 2, "CMP S",    0xAC, AddressingModes.AM_INDEXED_PAGE3,   3,  7,   0,  0, 16, 16, 16, 16, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(685, 2, "~~~~~",    0xAD, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(686, 2, "~~~~~",    0xAE, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(687, 2, "~~~~~",    0xAF, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(688, 2, "~~~~~",    0xB0, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(689, 2, "~~~~~",    0xB1, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(690, 2, "~~~~~",    0xB2, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(691, 2, "CMP U",    0xB3, AddressingModes.AM_EXTENDED_PAGE3,  4,  8,   0,  0, 16, 16, 16, 16, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(692, 2, "~~~~~",    0xB4, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(693, 2, "~~~~~",    0xB5, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(694, 2, "~~~~~",    0xB6, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(695, 2, "~~~~~",    0xB7, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(696, 2, "~~~~~",    0xB8, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(697, 2, "~~~~~",    0xB9, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(698, 2, "~~~~~",    0xBA, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(699, 2, "~~~~~",    0xBB, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(700, 2, "CMP S",    0xBC, AddressingModes.AM_EXTENDED_PAGE3,  4,  8,   0,  0, 16, 16, 16, 16, 0,    0);   // MC6809 X
            SetOpCodeTableEntry(701, 2, "~~~~~",    0xBD, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(702, 2, "~~~~~",    0xBE, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(703, 2, "~~~~~",    0xBF, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(704, 2, "~~~~~",    0xC0, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(705, 2, "~~~~~",    0xC1, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(706, 2, "~~~~~",    0xC2, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(707, 2, "~~~~~",    0xC3, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(708, 2, "~~~~~",    0xC4, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(709, 2, "~~~~~",    0xC5, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(710, 2, "~~~~~",    0xC6, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(711, 2, "~~~~~",    0xC7, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(712, 2, "~~~~~",    0xC8, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(713, 2, "~~~~~",    0xC9, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(714, 2, "~~~~~",    0xCA, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(715, 2, "~~~~~",    0xCB, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(716, 2, "~~~~~",    0xCC, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(717, 2, "~~~~~",    0xCD, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(718, 2, "~~~~~",    0xCE, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(719, 2, "~~~~~",    0xCF, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(720, 2, "~~~~~",    0xD0, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(721, 2, "~~~~~",    0xD1, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(722, 2, "~~~~~",    0xD2, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(723, 2, "~~~~~",    0xD3, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(724, 2, "~~~~~",    0xD4, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(725, 2, "~~~~~",    0xD5, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(726, 2, "~~~~~",    0xD6, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(727, 2, "~~~~~",    0xD7, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(728, 2, "~~~~~",    0xD8, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(729, 2, "~~~~~",    0xD9, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(730, 2, "~~~~~",    0xDA, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(731, 2, "~~~~~",    0xDB, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(732, 2, "~~~~~",    0xDC, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(733, 2, "~~~~~",    0xDD, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(734, 2, "~~~~~",    0xDE, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(735, 2, "~~~~~",    0xDF, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(736, 2, "~~~~~",    0xE0, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(737, 2, "~~~~~",    0xE1, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(738, 2, "~~~~~",    0xE2, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(739, 2, "~~~~~",    0xE3, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(740, 2, "~~~~~",    0xE4, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(741, 2, "~~~~~",    0xE5, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(742, 2, "~~~~~",    0xE6, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(743, 2, "~~~~~",    0xE7, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(744, 2, "~~~~~",    0xE8, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(745, 2, "~~~~~",    0xE9, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(746, 2, "~~~~~",    0xEA, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(747, 2, "~~~~~",    0xEB, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(748, 2, "~~~~~",    0xEC, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(749, 2, "~~~~~",    0xED, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(750, 2, "~~~~~",    0xEE, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(751, 2, "~~~~~",    0xEF, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(752, 2, "~~~~~",    0xF0, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(753, 2, "~~~~~",    0xF1, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(754, 2, "~~~~~",    0xF2, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(755, 2, "~~~~~",    0xF3, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(756, 2, "~~~~~",    0xF4, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(757, 2, "~~~~~",    0xF5, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(758, 2, "~~~~~",    0xF6, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(759, 2, "~~~~~",    0xF7, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(760, 2, "~~~~~",    0xF8, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(761, 2, "~~~~~",    0xF9, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(762, 2, "~~~~~",    0xFA, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(763, 2, "~~~~~",    0xFB, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(764, 2, "~~~~~",    0xFC, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(765, 2, "~~~~~",    0xFD, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(766, 2, "~~~~~",    0xFE, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
            SetOpCodeTableEntry(767, 2, "~~~~~",    0xFF, AddressingModes.AM_ILLEGAL,         1,  1,   0,  0,  0,  0,  0,  0, 0,    0);   // PAGE 3 X
        }

        void LogDatWrite (int nIndex, byte b)
        {
            //bool bValidMemory;

            //ulong lPhysicalAddress = memory.LogicalToPhysicalAddress (m_CurrentIP, out bValidMemory);

            //if (m_fpDATWrites != null)
            //{
            //    // don't show the ones that are looking for memory

            //    if (m_CurrentIP != 0xFB6C)
            //        fprintf (m_fpDATWrites, "Logical Address: 0x%04X, Physical Address: 0x%05X 4KSegment: 0x%04X OldDATValue: 0x%05X NewDATValue: 0x%05X\n", m_CurrentIP, lPhysicalAddress, nIndex * 0x1000, m_MemoryDAT[nIndex], (b ^ 0x0F) << 12); 
            //}
        }

        void LogMemoryWrite (ushort sLogicalAddress, byte b)
        {
            //bool bValidMemory;

            //ulong lPhysicalAddress = memory.LogicalToPhysicalAddress (sLogicalAddress, out bValidMemory);

            //if (m_fpMEMWrites != null)
            //    fprintf (m_fpMEMWrites, "IP: 0x%04X  Physical Address: 0x%06X  Logical Address: 0x%04X  Byte: 0x%02X\n", m_CurrentIP, lPhysicalAddress, sLogicalAddress, b);
        }

        byte ReadMemByte (ushort sLogicalAddress)
        {
            bool bValidMemory;

            ulong lPhysicalAddress = Memory.LogicalToPhysicalAddress (sLogicalAddress, out bValidMemory);

            if (bValidMemory)
                return (Memory.MemorySpace[lPhysicalAddress]);
            else
                return (0xFF);
        }

        void WriteMemByte (ushort sLogicalAddress, byte b)
        {
            bool bValidMemory;

            ulong lPhysicalAddress = Memory.LogicalToPhysicalAddress (sLogicalAddress, out bValidMemory);

            if (bValidMemory)
            {
                //memory.MemorySpace[memory.LogicalToPhysicalAddress(sLogicalAddress, out bValidMemory)] = b;
                Memory.MemorySpace[lPhysicalAddress] = b;
            }
        }

        void StoreMemoryByte (byte b, ushort sLogicalAddress)
        {
            bool bValidMemory;

            ulong lPhysicalAddress = Memory.LogicalToPhysicalAddress(sLogicalAddress, out bValidMemory);

            if (lPhysicalAddress < 0x00010000 && Memory.IsRAM ((ushort) lPhysicalAddress))
            {
                // we are in the first 64k memory so we may be trying to access memory that is logically not RAM but may be physically RAM

                WriteMemByte (sLogicalAddress, b);
            }
            else
            {
                if (Memory.IsRAM (sLogicalAddress) || (lPhysicalAddress > 0x0000FFFF))
                    WriteMemByte (sLogicalAddress, b);
                else
                    Memory.StoreMemoryByte(b, sLogicalAddress);
            }
        }

        void StoreMemoryWord (ushort w, ushort sLogicalAddress)
        {
            StoreMemoryByte ((byte)(w / 256), sLogicalAddress);
            StoreMemoryByte ((byte)(w % 256), (ushort)(sLogicalAddress + 1));
        }

        byte LoadMemoryByte (ushort sLogicalAddress)
        {
            bool bValidMemory;
            byte d = 0xff;

            ulong lPhysicalAddress = Memory.LogicalToPhysicalAddress (sLogicalAddress, out bValidMemory);

            if (lPhysicalAddress < 0x00010000 && Memory.IsRAM ((ushort) lPhysicalAddress))
            {
                // we are in the first 64k memory so we may be trying to access memory that is logically not RAM but may be physically RAM

                d = ReadMemByte (sLogicalAddress);
            }
            else
            {
                if (Memory.IsRAM (sLogicalAddress) || (lPhysicalAddress > 0x0000FFFF))
                    d = ReadMemByte (sLogicalAddress);
                else
                    d = Memory.LoadMemoryByte(sLogicalAddress);
            }

	        #if ALLOW_CBL
		        m_pView->ShowCoolBlinkyLights (d, sLogicalAddress, m_MemoryDAT[sLogicalAddress >> 12] & DAT_MASK);
	        #endif

            return (d);
        }

        ushort LoadMemoryWord (ushort sAddr)
        {
            return (ushort)((LoadMemoryByte (sAddr) * 256) + (LoadMemoryByte ((ushort)(sAddr + 1))));
        }

        //
        // To use this the following member variables must be set up:
        //
        //      m_OpCode            what's the opcode
        //      m_nTable            which opcode table to use
        //      m_Operand           the effective address
        //      m_OperandSize       number of bytes of operand
        //      m_Indirect          is this an indirect indexing mode
        //      m_OffsetType        the type of indexing offset
        //      m_cPostRegister     which register is specified in the post byte
        //      m_Predecrement      does instruction indicate a pre decrement
        //      m_Postincrement     does instruction indicate a post increment
        //
        //  and the following parameters passed in:
        //
        //      mode                Addressing mode
        //      sCurrentIP          Address of instruction in m_Memory[sLogicalAddress]

        string BuildDebugLine (AddressingModes attribute, ushort sCurrentIP, ushort Xreg, ushort Yreg, ushort Ureg, ushort Sreg, bool m_IncludeHex)
        {
            ushort nOffset;
            string pszOpenIndirect  = "";
            string pszCloseIndirect = "";

            string szAddress        = "";
            string szDebugLine      = "";
            string szMemoryContents = "";
            string szOpcodeHex      = "";
            string szOperandHex     = "";
            string szPostByte       = "";

            string szFinishedLine   = "";

            szAddress = string.Format("{0} ", sCurrentIP.ToString("X4"));
    
            szPostByte = "   ";

            string strOperandDAT = "  ";
            switch (opctbl[m_nTable, m_OpCode].attribute)
            {
                case AddressingModes.AM_EXTENDED_PAGE2:   
                case AddressingModes.AM_EXTENDED_PAGE3:   
                case AddressingModes.AM_EXTENDED_6809:    
                case AddressingModes.AM_IMM16_PAGE2:      
                case AddressingModes.AM_IMM16_PAGE3:      
                case AddressingModes.AM_IMM16_6809:       
                case AddressingModes.AM_INDEXED_PAGE2:    
                case AddressingModes.AM_INDEXED_PAGE3:    
                case AddressingModes.AM_INDEXED_6809:
                    strOperandDAT = clsTraceBuffer[TraceIndex].cRegisterOperandDAT.ToString("X2");
                    break;
                default:
                    break;
            }

            if (m_nTable == MC6809_NORMAL)
            {
                szOpcodeHex = string.Format("{0}   ", LoadMemoryByte (sCurrentIP).ToString("X2"));

                if (attribute == AddressingModes.AM_INDEXED_6809)
                {
                    szPostByte = string.Format("{0} ", LoadMemoryByte ((ushort)(sCurrentIP + 1)).ToString("X2"));
                    switch (m_OperandSize)
                    {
                        case 0:     // no operand
                            szOperandHex = "     ";
                            break;
                        case 1:     // 1 byte operand
                            szOperandHex = string.Format("  {0} ", LoadMemoryByte ((ushort)(sCurrentIP + 2)).ToString("X2"));
                            break;
                        case 2:     // 2 byte operand
                            szOperandHex = string.Format("{0}{1} ", LoadMemoryByte ((ushort)(sCurrentIP + 2)).ToString("X2"), LoadMemoryByte ((ushort)(sCurrentIP + 3)).ToString("X2"));
                            break;
                    }
                }
                else
                {
                    switch (m_OperandSize)
                    {
                        case 0:     // no operand
                            szOperandHex = "     ";
                            break;
                        case 1:     // 1 byte operand
                            szOperandHex = string.Format("  {0} ", LoadMemoryByte ((ushort)(sCurrentIP + 1)).ToString("X2"));
                            break;
                        case 2:     // 2 byte operand
                            szOperandHex = string.Format("{0}{1} ", LoadMemoryByte ((ushort)(sCurrentIP + 1)).ToString("X2"), LoadMemoryByte ((ushort)(sCurrentIP + 2)).ToString("X2"));
                            break;
                    }
                }
            }
            else
            {
                szOpcodeHex = string.Format("{0}{1} ", LoadMemoryByte(sCurrentIP).ToString("X2"), LoadMemoryByte((ushort)(sCurrentIP + 1)).ToString("X2"));

                if (((int)attribute & 0x00FF) == (int)AddressingModes.AM_INDEXED_6809)
                {
                    szPostByte = string.Format("{0} ", LoadMemoryByte ((ushort)(sCurrentIP + 2)).ToString("X2"));
                    switch (m_OperandSize)
                    {
                        case 0:     // no operand
                            szOperandHex = "     ";
                            break;
                        case 1:     // 1 byte operand
                            szOperandHex = string.Format("  {0} ", LoadMemoryByte ((ushort)(sCurrentIP + 3)).ToString("X2"));
                            break;
                        case 2:     // 2 byte operand
                            szOperandHex = string.Format("{0}{1} ", LoadMemoryByte ((ushort)(sCurrentIP + 4)).ToString("X2"), LoadMemoryByte ((ushort)(sCurrentIP + 5)).ToString("X2"));
                            break;
                    }
                }
                else
                {
                    switch (m_OperandSize)
                    {
                        case 0:     // no operand
                            szOperandHex = "     ";
                            break;
                        case 1:     // 1 byte operand
                            szOperandHex = string.Format("  {0} ", LoadMemoryByte ((ushort)(sCurrentIP + 2)).ToString("X2"));
                            break;
                        case 2:     // 2 byte operand
                            szOperandHex = string.Format("{0}{1} ", LoadMemoryByte ((ushort)(sCurrentIP + 2)).ToString("X2"), LoadMemoryByte ((ushort)(sCurrentIP + 3)).ToString("X2"));
                            break;
                    }
                }
            }

            switch (attribute)
            {
                case AddressingModes.AM_INHERENT_PAGE3:     // SWI 3
                case AddressingModes.AM_INHERENT_PAGE2:     // SWI 2
                case AddressingModes.AM_INHERENT_6809:
                    if ((m_OpCode == 0x1F) || (m_OpCode == 0x1E))       // TFR  = 0x01F EXG = 0x1E
                    {
                        int i, r, r1, r2;

                        szDebugLine = string.Format("{0}  ", opctbl[m_nTable,m_OpCode].mneumonic);

                        r1 = (m_Operand & 0x00F0) >> 4;
                        r2 = (m_Operand & 0x000F);

                        for (i = 0, r = r1; i < 2; i++)
                        {
                            switch (r)
                            {
                                case 0x00: szDebugLine += "D";    break;
                                case 0x01: szDebugLine += "X";    break;
                                case 0x02: szDebugLine += "Y";    break;
                                case 0x03: szDebugLine += "U";    break;
                                case 0x04: szDebugLine += "S";    break;
                                case 0x05: szDebugLine += "???";  break;
                                case 0x06: szDebugLine += "???";  break;
                                case 0x07: szDebugLine += "PC";   break;
                                case 0x08: szDebugLine += "A";    break;
                                case 0x09: szDebugLine += "B";    break;
                                case 0x0A: szDebugLine += "CCR";  break;
                                case 0x0B: szDebugLine += "DPR";  break;
                                case 0x0C: szDebugLine += "???";  break;
                                case 0x0D: szDebugLine += "???";  break;
                                case 0x0E: szDebugLine += "???";  break;
                                case 0x0F: szDebugLine += "???";  break;
                            }
                            if (i == 0)
                                szDebugLine += ",";

                            r = r2;
                        }
                    }
                    else
                    {
                        if (
                            m_OpCode == 0x34 || // PSH S
                            m_OpCode == 0x35 || // PUL S
                            m_OpCode == 0x36 || // PSH U
                            m_OpCode == 0x37    // PUL U
                           )
                        {
                            // output the register list to push or pull

                            byte cMask = 0x80;
                            bool nNeedComma = false;

                            // sprintf (szDebugLine, "%s $%02X", 
                            //            opctbl[m_nTable,m_OpCode].mneumonic,
                            //            (byte) m_Operand
                            //        );
                            szDebugLine = string.Format("{0} ", opctbl[m_nTable,m_OpCode].mneumonic);

                            for (int i = 7; i >= 0; i--)
                            {
                                string szReg = "";

                                if (m_OpCode < 0x36)
                                    szReg = "CABDXYUP";
                                else
                                    szReg = "CABDXYSP";

                                if (((byte) m_Operand & cMask) != 0)
                                {
                                    if (nNeedComma)
                                        szDebugLine += ",";
                                    szDebugLine += szReg[i];    //.ElementAt(i);
                                    nNeedComma = true;
                                }
                                cMask = (byte)(cMask >> 1);
                            }
                        }
                        // the reset of the AM_INHERENT_ instructions
                        else
                        {
                            szDebugLine = string.Format("{0}", opctbl[m_nTable,m_OpCode].mneumonic);
                        }
                    }
                    break;

                case AddressingModes.AM_DIRECT_PAGE3:
                case AddressingModes.AM_DIRECT_PAGE2:
                case AddressingModes.AM_DIRECT_6809:

                    // These have 16 bit values to operate with

                    if (                        // AM_DIRECT_6809 AM_DIRECT_PAGE2 AM_DIRECT_PAGE3
                        m_OpCode == 0x93 ||     // SUB D          CMP D           CMP U
                        m_OpCode == 0x9C ||     // CMP X          CMP Y           CMP S
                        m_OpCode == 0x9E ||     // LDX            LDY             
                        m_OpCode == 0x9F ||     // STX            STY             
                        m_OpCode == 0xD3 ||     // ADD D 
                        m_OpCode == 0xDC ||     // LDD   
                        m_OpCode == 0xDD ||     // STD   
                        m_OpCode == 0xDE ||     // LDU            LDS             
                        m_OpCode == 0xDF        // STU            STS             
                       )
                    {
                        szDebugLine = string.Format("{0} ${1}",  opctbl[m_nTable,m_OpCode].mneumonic, m_Operand.ToString("X2"));
                        szMemoryContents = string.Format("(${0})", (ReadMemByte (m_Operand) * 256 + ReadMemByte ((ushort)(m_Operand + 1))).ToString("X4"));
                    }

                    // These are 8 bit values

                    else
                    {
                        szDebugLine = string.Format("{0} ${1}", opctbl[m_nTable,m_OpCode].mneumonic, m_Operand.ToString("X4"));
                        szMemoryContents = string.Format("(${0})", ReadMemByte (m_Operand).ToString("X2"));
                    }
                    break;

                case AddressingModes.AM_RELATIVE_6809:
                    if (m_OpCode == 0x16 || m_OpCode == 0x17)
                    {
                        nOffset = m_Operand;
                        szDebugLine = string.Format("{0} ${1}", opctbl[m_nTable,m_OpCode].mneumonic, m_Operand.ToString("X4"));
                        szMemoryContents = string.Format("(${0})", ((IP + nOffset) & 0xFFFF).ToString("X4"));
                    }
                    else
                    {
                        nOffset = m_Operand;
                        if ((m_Operand & 0x80) != 0x00)
                            nOffset |= 0xFF00;
                
                        szDebugLine = string.Format("{0} ${1}", opctbl[m_nTable,m_OpCode].mneumonic, m_Operand.ToString("X2"));
                        szMemoryContents = string.Format("(${0})", ((IP + nOffset) & 0xFFFF).ToString("X4"));
                    }
                    break;

                case AddressingModes.AM_RELATIVE_PAGE3:
                case AddressingModes.AM_RELATIVE_PAGE2:

                    nOffset = m_Operand;
                    szDebugLine = string.Format("{0} ${1}", opctbl[m_nTable,m_OpCode].mneumonic, m_Operand.ToString("X4"));
                    szMemoryContents = string.Format("(${0})", ((IP + nOffset) & 0xFFFF).ToString("X4"));
                    break;

                case AddressingModes.AM_EXTENDED_PAGE2:
                case AddressingModes.AM_EXTENDED_PAGE3:
                case AddressingModes.AM_EXTENDED_6809:
                    if (                    //  AM_EXTENED_6809 AM_EXTENDED_PAGE2 AM_EXTENDED_PAGE3
                        m_OpCode == 0xB3 || //  SUB D           CMP D             CMP U
                        m_OpCode == 0xBC || //  CMP X           CMP Y             CMP S
                        m_OpCode == 0xBE || //  LDX             LDY
                        m_OpCode == 0xBF || //  STX             STY
                        m_OpCode == 0xFE || //  LDU             LDS
                        m_OpCode == 0xFF    //  STU             STS
                       )
                    {
                        szDebugLine = string.Format("{0} ${1}", opctbl[m_nTable,m_OpCode].mneumonic, m_Operand.ToString("X4"));
                        szMemoryContents = string.Format("(${0})", (ReadMemByte (m_Operand) * 256 + ReadMemByte ((ushort)(m_Operand + 1))).ToString("X4"));
                    }
                    else
                    {
                        if (
                            m_OpCode == 0x7E || //  JMP
                            m_OpCode == 0xBD    //  JSR
                            )
                        {
                            szDebugLine = string.Format("{0} ${1}", opctbl[m_nTable,m_OpCode].mneumonic, m_Operand.ToString("X4"));
                        }
                        else
                        {
                            szDebugLine = string.Format("{0} ${1}", opctbl[m_nTable, m_OpCode].mneumonic, m_Operand.ToString("X4"));
                            szMemoryContents = string.Format("(${0})", ReadMemByte(m_Operand).ToString("X2"));
                        }
                    }
                    break;

                case AddressingModes.AM_IMM16_PAGE2:
                case AddressingModes.AM_IMM16_PAGE3:
                case AddressingModes.AM_IMM16_6809:
                    szDebugLine = string.Format("{0} #${1}", opctbl[m_nTable,m_OpCode].mneumonic, m_Operand.ToString("X4"));
                    break;

                case AddressingModes.AM_IMM8_PAGE3:
                case AddressingModes.AM_IMM8_PAGE2:
                case AddressingModes.AM_IMM8_6809:
                    szDebugLine = string.Format("{0} #${1}", opctbl[m_nTable,m_OpCode].mneumonic, m_Operand.ToString("X2"));
                    break;

                case AddressingModes.AM_INDEXED_PAGE3:
                case AddressingModes.AM_INDEXED_PAGE2:
                case AddressingModes.AM_INDEXED_6809:

                    if (m_Indirect)
                    {
                        pszOpenIndirect  = "[";
                        pszCloseIndirect = "]";
                    }

                    switch (m_OffsetType)
                    {
                        case OffsetType.OFFSET_NONE:
                            szDebugLine = string.Format("{0} {1},", opctbl[m_nTable,m_OpCode].mneumonic, pszOpenIndirect);
                            break;

                        case OffsetType.OFFSET_REGISTER_A:
                            szDebugLine = string.Format("{0} {1}A,", opctbl[m_nTable, m_OpCode].mneumonic, pszOpenIndirect);
                            break;

                        case OffsetType.OFFSET_REGISTER_B:
                            szDebugLine = string.Format("{0} {1}B,", opctbl[m_nTable, m_OpCode].mneumonic, pszOpenIndirect);
                            break;

                        case OffsetType.OFFSET_REGISTER_D:
                            szDebugLine = string.Format("{0} {1}D,", opctbl[m_nTable, m_OpCode].mneumonic, pszOpenIndirect);
                            break;

                        case OffsetType.OFFSET_PCR_8:
                            if (m_Indirect)
                            {
                                szMemoryContents = string.Format("(${0})", m_Operand.ToString("X4"));
                                szDebugLine = string.Format("{0} {1}${2},PCR", opctbl[m_nTable,m_OpCode].mneumonic, pszOpenIndirect, ((ushort) (m_sIndirectPtr - IP)).ToString("X2"));
                            }
                            else
                            {
                                szDebugLine = string.Format("{0} {1}${2},PCR", opctbl[m_nTable,m_OpCode].mneumonic, pszOpenIndirect, ((ushort) (m_Operand - IP)).ToString("X2"));
                            }
                            break;

                        case OffsetType.OFFSET_PCR_16:
                            if (m_Indirect)
                            {
                                szMemoryContents = string.Format("(${0})", m_Operand.ToString("X4"));
                                szDebugLine = string.Format("{0} {1}${2},PCR", opctbl[m_nTable,m_OpCode].mneumonic, pszOpenIndirect, ((ushort) (m_sIndirectPtr - IP)).ToString("X4"));
                            }
                            else
                            {
                                szDebugLine = string.Format("{0} {1}${2},PCR", opctbl[m_nTable,m_OpCode].mneumonic, pszOpenIndirect, ((ushort) (m_Operand - IP)).ToString("X4"));
                            }
                            break;

                        case OffsetType.EXTENDED_INDIRECT:
                            szDebugLine = string.Format("{0} [${1}", opctbl[m_nTable,m_OpCode].mneumonic, ((ushort) (m_sIndirectPtr)).ToString("X4"));
                            szMemoryContents = string.Format("(${0})", m_Operand.ToString("X4"));
                            break;

                        case OffsetType.OFFSET_8_BIT:
                            if (m_Indirect)
                            {
                                szMemoryContents = string.Format("(${0})", m_Operand.ToString("X4"));
                                szDebugLine = string.Format("{0} {1}${2},", opctbl[m_nTable, m_OpCode].mneumonic, pszOpenIndirect, ((ushort)(m_sIndirectPtr - OffsetRegisters[m_cPostRegister])).ToString("X2"));
                            }
                            else
                            {
                                szDebugLine = string.Format("{0} {1}${2},", opctbl[m_nTable,m_OpCode].mneumonic, pszOpenIndirect, ((ushort) (m_Operand - OffsetRegisters[m_cPostRegister])).ToString("X2"));
                            }
                            break;

                        case OffsetType.OFFSET_16_BIT:
                            if (m_Indirect)
                            {
                                szMemoryContents = string.Format("(${0})", m_Operand.ToString("X4"));
                                szDebugLine = string.Format("{0} {1}${2},", opctbl[m_nTable, m_OpCode].mneumonic, pszOpenIndirect, ((ushort)(m_sIndirectPtr - OffsetRegisters[m_cPostRegister])).ToString("X4"));
                            }
                            else
                            {
                                szDebugLine = string.Format("{0} {1}${2},", opctbl[m_nTable, m_OpCode].mneumonic, pszOpenIndirect, ((ushort)(m_Operand - OffsetRegisters[m_cPostRegister])).ToString("X4"));
                            }
                            break;

                        case OffsetType.OFFSET_PREDECREMENT:
                        case OffsetType.OFFSET_POSTINCREMENT:
                            if (m_Indirect)
                            {
                                szMemoryContents = string.Format("(${0})", m_Operand.ToString("X4"));
                            }
                            szDebugLine = string.Format("{0} {1},", opctbl[m_nTable,m_OpCode].mneumonic, pszOpenIndirect);
                            break;

                        case OffsetType.OFFSET_INVALID:
                        default:
                            if (m_Indirect)
                            {
                                szMemoryContents = string.Format("(${0})", m_Operand.ToString("X4"));
                                szDebugLine = string.Format("{0} {1}${2},", opctbl[m_nTable, m_OpCode].mneumonic, pszOpenIndirect, ((ushort)(m_sIndirectPtr - OffsetRegisters[m_cPostRegister])).ToString("X2"));
                            }
                            else
                            {
                                szDebugLine = string.Format("{0} {1}${2},", opctbl[m_nTable, m_OpCode].mneumonic, pszOpenIndirect, ((ushort)(m_Operand - OffsetRegisters[m_cPostRegister])).ToString("X2"));
                            }
                            break;
                    }

                    for (int i = 0; i < m_Predecrement; i++)
                        szDebugLine += "-";

                    if (
                        (m_OffsetType != OffsetType.EXTENDED_INDIRECT) &&
                        (m_OffsetType != OffsetType.OFFSET_PCR_8) &&
                        (m_OffsetType != OffsetType.OFFSET_PCR_16)
                       )
                    {
                        switch (m_cPostRegister)
                        {
                            case 0:
                                szDebugLine += "X";
                                break;
                            case 1:
                                szDebugLine += "Y";
                                break;
                            case 2:
                                szDebugLine += "U";
                                break;
                            default:
                                szDebugLine += "S";
                                break;
                        }

                        for (int i = 0; i < m_Postincrement; i++)
                            szDebugLine += "+";
                    }

                    szDebugLine += pszCloseIndirect;

                    if (m_OpCode != 0x6E && m_OpCode != 0xAD)
                    {
                        if (                    // AM_INDEXED_6809  AM_INDEXED_PAGE2 AM_INDEXED_PAGE3
                            m_OpCode == 0x30 || // LEA X            
                            m_OpCode == 0x31 || // LEA Y
                            m_OpCode == 0x32 || // LEA S
                            m_OpCode == 0x33 || // LEA U
                            m_OpCode == 0xA3 || // SUB D            CMP D            CMP U
                            m_OpCode == 0xAC || // CMP X            CMP Y            CMP S
                            m_OpCode == 0xAE || // LDX              LDY
                            m_OpCode == 0xAF || // STX              STY
                            m_OpCode == 0xE3 || // ADD D
                            m_OpCode == 0xEC || // LDD  
                            m_OpCode == 0xED || // STD  
                            m_OpCode == 0xEE || // LDU              LDS
                            m_OpCode == 0xEF || // STU              STS
                            m_OpCode == 0xFC || // LDD  
                            m_OpCode == 0xFD || // STD  
                            m_OpCode == 0xFE || // LDU  
                            m_OpCode == 0xFF    // STU  
                           )
                        {
                            if (m_Indirect)
                            {
                                szMemoryContents = string.Format("(${0})", ReadMemByte(m_Operand) * 256 + ReadMemByte((ushort)(m_Operand + 1)).ToString("X4"));
                            }
                            else
                                szMemoryContents = string.Format("(${0})", m_Operand.ToString("X4"));
                        }
                        else
                        {
                            szMemoryContents = string.Format("(${0})", ReadMemByte(m_Operand).ToString("X2"));
                        }
                    }
                    break;
            }

            szFinishedLine = string.Format("{0}{1}{2}{3}{4}", szAddress, szOpcodeHex, szPostByte, szOperandHex, szDebugLine);

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
                    "{0}   {1}   {2} {3} {4} {5} {6} {7} {8} {9} {10}",
                        clsTraceBuffer[TraceIndex].cRegisterDAT.ToString("X2"),
                        strOperandDAT,
                        clsTraceBuffer[TraceIndex].sStackTrace.ToString("X4"),
                        clsTraceBuffer[TraceIndex].cRegisterA.ToString("X2"),
                        clsTraceBuffer[TraceIndex].cRegisterB.ToString("X2"),
                        clsTraceBuffer[TraceIndex].sRegisterX.ToString("X4"),
                        clsTraceBuffer[TraceIndex].sRegisterY.ToString("X4"),
                        clsTraceBuffer[TraceIndex].sRegisterU.ToString("X4"),
                        clsTraceBuffer[TraceIndex].cRegisterDP.ToString("X2"),
                        clsTraceBuffer[TraceIndex].cRegisterCCR.ToString("X2"),
                        clsTraceBuffer[TraceIndex].sRegisterInterrupts.ToString()
                );

            string fullLine = string.Format("{0} ${1}", szFinishedLine.PadRight(50), szRegisterContents);
            return (fullLine);
        }

        void SaveState(AddressingModes attribute)
        {
            // save state if trace is enabled

            if (Program.TraceEnabled)
            {
                // fix any post incrementing or pre decrementing that may have happened

                ushort Xreg = OffsetRegisters[(int)OffsetRegisterIndex.m_X];
                ushort Yreg = OffsetRegisters[(int)OffsetRegisterIndex.m_Y];
                ushort Ureg = OffsetRegisters[(int)OffsetRegisterIndex.m_U];
                ushort Sreg = OffsetRegisters[(int)OffsetRegisterIndex.m_S];

                switch (m_cPostRegister)
                {
                    case 0:
                        Xreg = (ushort)(OffsetRegisters[(int)OffsetRegisterIndex.m_X] + m_Predecrement - m_Postincrement);
                        break;
                    case 1:
                        Yreg = (ushort)(OffsetRegisters[(int)OffsetRegisterIndex.m_Y] + m_Predecrement - m_Postincrement);
                        break;
                    case 2:
                        Ureg = (ushort)(OffsetRegisters[(int)OffsetRegisterIndex.m_U] + m_Predecrement - m_Postincrement);
                        break;
                    default:
                        Sreg = (ushort)(OffsetRegisters[(int)OffsetRegisterIndex.m_S] + m_Predecrement - m_Postincrement);
                        break;
                }

                clsTraceBuffer[TraceIndex].sExecutionTrace = CurrentIP;
                clsTraceBuffer[TraceIndex].cRegisterDAT = (byte)((Memory.m_MemoryDAT[IP >> 12] & Memory.DAT_MASK) >> 12);
                clsTraceBuffer[TraceIndex].cRegisterOperandDAT = (byte)((Memory.m_MemoryDAT[m_Operand >> 12] & Memory.DAT_MASK) >> 12);
                clsTraceBuffer[TraceIndex].cTable = (byte)m_nTable;
                clsTraceBuffer[TraceIndex].cOpcode = m_OpCode;
                clsTraceBuffer[TraceIndex].sOperand = m_Operand;
                clsTraceBuffer[TraceIndex].cRegisterA = _dReg.hi;
                clsTraceBuffer[TraceIndex].cRegisterB = _dReg.lo;
                clsTraceBuffer[TraceIndex].sRegisterX = Xreg;
                clsTraceBuffer[TraceIndex].sRegisterY = Yreg;
                clsTraceBuffer[TraceIndex].sRegisterU = Ureg;
                clsTraceBuffer[TraceIndex].cRegisterDP = m_DP;
                clsTraceBuffer[TraceIndex].cRegisterCCR = m_CCR;
                clsTraceBuffer[TraceIndex].sStackTrace = Sreg;
                clsTraceBuffer[TraceIndex].nPostIncerment = m_Postincrement;
                clsTraceBuffer[TraceIndex].nPostIncerment = m_Predecrement;

                DebugLine[TraceIndex] = BuildDebugLine(attribute, CurrentIP, Xreg, Yreg, Ureg, Sreg, true);

                bool bValidMemory = false;

                string szPhysicalOperand = "";
                if (m_OperandSize == 2 && (attribute == AddressingModes.AM_DIRECT_6809 || attribute == AddressingModes.AM_EXTENDED_6809))
                    szPhysicalOperand = Memory.LogicalToPhysicalAddress(m_Operand, out bValidMemory).ToString("X6");
                else
                {
                    if ((attribute & (AddressingModes)0x00FF) == AddressingModes.AM_INDEXED_6809)
                    {
                        szPhysicalOperand = Memory.LogicalToPhysicalAddress(m_Operand, out bValidMemory).ToString("X6");
                    }
                }

                TraceIndex++;

                if (TraceIndex > (long)(traceSize - 1))
                {
                    traceFull = true;
                    using (StreamWriter sw = new StreamWriter(File.Open(Program._traceFilePath, FileMode.Append, FileAccess.Write, FileShare.Write)))
                    {
                        foreach (string s in DebugLine)
                        {
                            sw.WriteLine(s);
                        }
                    }
                    TraceIndex = 0;
                }

                // see if we are single stepping - if we are, display debug line and suspend

                //if (SingleStepMode || ShowDebugOutput)
                //{
                //    if (attribute != AddressingModes.AM_ILLEGAL)
                //    {
                //        string szDebugLine;

                //        //szDebugLine = BuildDebugLine(attribute, m_CurrentIP, false);
                //        ShowDebug(attribute, m_szDebugLine[m_nTraceIndex], m_CurrentIP);
                //        if (SingleStepMode)
                //            Program._cpuThread.Suspend();

                //        if (m_nStepOver == true)
                //        {
                //            // save the state of the m_nSingleStepMode state
                //            // get the current Instruction Pointer

                //            NextIPToStopOn = IP;
                //            SingleStepMode = false;
                //        }
                //    }
                //}
            }
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
        //   6 (Bit V) Test: Set equal of result of b7 exclusive or b6 before shift has occurred.
        //*  7 (Bit N) Test: Sign bit of result = 1? (Bit 7 = 1)
        //   8 (Bit V) Test: 2's complement overflow from subtraction of LS bytes?
        //*  9 (Bit N) Test: Result less than zero? (Bit 15 = 1)
        //  10 (All)   Load condition code register from stack
        //  11 (Bit I) Set when interrupt occurrs. If prviously set, a Non-maskable interrupt is
        //             required to exit the wait state.
        //  12 (All)   Set according to the contents of Accumlator A
        //  13 (All)   Set always
        //  14 (All)   Reset always
        //  15 (All)   Test and set if true, cleared otherwise.
        //  16 (All)   same as 15 for 16 bit
        //
        //  * = no longer used

        #region Arithmetic and Logical Register Operations

        void SetCCR(ushort after)
        {
            SetCCR(after, null);
        }

        void SetCCR (ushort after, ushort? before)
        {
            int i;
            int b;
            ushort s;
            int [] nRules = new int[6];

            Array.Copy(opctbl[m_nTable,m_OpCode].ccr_rules, nRules, nRules.Length);
            //memcpy (nRules, opctbl[m_nTable,m_OpCode].ccr_rules, sizeof (nRules));

            for (i = 0; i < 6; i++)
            {
                switch (nRules[i])
                {
                    case  0: // No Affect
                        break;

                    case  1: // (Bit V) Test: orginal operand = 10000000?
                             //               ONLY USED FOR NEG     00 - M -> M
                             //                             NEG A   00 - A -> A
                             //                             NEG B   00 - B -> B
                        if (before == 0x80)
                            m_CCR |= (byte)CCR_OVERFLOW;
                        else
                            m_CCR &= (byte)~CCR_OVERFLOW;
                        break;

                    case  2: // (Bit C) Test: Result = 00000000?
                             //               ONLY USED FOR NEG     00 - M -> M
                             //                             NEG A   00 - A -> A
                             //                             NEG B   00 - B -> B

                        if (after == 0x00)
                            m_CCR &= (byte)~CCR_CARRY;
                        else
                            m_CCR |= (byte)CCR_CARRY;
                    
                        break;

                    case  3: // (Bit C) Test: Decimal value of most significant BCD character > 9?
                             //               (Not cleared if previously set.)
                             //               ONLY USED FOR DAA
                        if (m_CF == 1)
                            m_CCR |= (byte)CCR_CARRY;
                        else
                            m_CCR &= (byte)~CCR_CARRY;
                        break;

                    case  4: // (Bit V) Test: Operand = 10000000 prior to execution?
                             //               ONLY USED FOR DEC     M - 1 -> M
                             //                             DEC A   A - 1 -> A
                             //                             DEC B   B - 1 -> B
                        if (before == 0x80)
                            m_CCR |= (byte)CCR_OVERFLOW;
                        else
                            m_CCR &= (byte)~CCR_OVERFLOW;
                        break;

                    case  5: // (Bit V) Test: Operand = 01111111 prior to execution?
                             //               ONLY USED FOR INC     M + 1 -> M
                             //                             INC A   A + 1 -> A
                             //                             INC B   B + 1 -> B

                        if (before == 0x7F)
                            m_CCR |= (byte)CCR_OVERFLOW;
                        else
                            m_CCR &= (byte)~CCR_OVERFLOW;
                        break;

                    case  6: // (Bit V) Test: Set equal to result of b7 ^ b6 before shift has occurred.
                             //               ONLY USED FOR SHIFTS and ROTATES

                        {
                            int b7, b6;

                            b7 = (before & 0x80) == 0 ? 0 : 1;
                            b6 = (before & 0x40) == 0 ? 0 : 1;

                            if ((b7 ^ b6) == 1)
                                m_CCR |= (byte)CCR_OVERFLOW;
                            else
                                m_CCR &= (byte)~CCR_OVERFLOW;
                        }

                        break;

                    case  7: // (Bit N) Test: Sign bit of result = 1?

                        //AfxMessageBox ("OOPS - case 7 in SetCCR");

                        //if ((after & 0x0080) == 0x80)
                        //    m_CCR |= CCR_NEGATIVE;
                        //else
                        //    m_CCR &= ~CCR_NEGATIVE;
                        break;

                    case  8: // (Bit V) Test: 2's complement overflow from subtraction of LS bytes?

                        s = (ushort)((before & 0x00ff) - (m_Operand & 0x00ff));
                        if ((s & 0x0100) == 0x0100)
                            m_CCR |= (byte)CCR_OVERFLOW;
                        else
                            m_CCR &= (byte)~CCR_OVERFLOW;
                        break;

                    case  9: // (Bit N) Test: Result less than zero? (Bit 15 = 1)

                        //AfxMessageBox ("OOPS - case 9 in SetCCR");

                        //if ((after & 0x8000) != 0)
                        //    m_CCR |= CCR_NEGATIVE;
                        //else
                        //    m_CCR &= ~CCR_NEGATIVE;
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

                        b = 0x01 << (5 - i);
                        m_CCR |= (byte)b;
                        break;

                    case 14: // (All)   Reset always

                        b = 0x01 << (5 - i);
                        m_CCR &= (byte)~b;
                        break;

                    case 15: // (All)   Test and set if true, cleared otherwise. 8 bit

                        switch (i)
                        {
                            case 0: // H    m_HF will be set by instruction execution

                                if (m_HF == 1)
                                    m_CCR |= (byte)CCR_HALFCARRY;
                                else
                                    m_CCR &= (byte)~CCR_HALFCARRY;
                                break;

                            case 1: // I    Only set by SWI in case 13

                                break;

                            case 2: // N    

                                if ((after & 0x0080) == 0x80)
                                    m_CCR |= (byte)CCR_NEGATIVE;
                                else
                                    m_CCR &= (byte)~CCR_NEGATIVE;
                                break;

                            case 3: // Z

                                if (after == 0)
                                    m_CCR |= (byte)CCR_ZERO;
                                else
                                    m_CCR &= (byte)~CCR_ZERO;
                                break;

                            case 4: // V    Only on Add's, Compare's, DAA, and Subtracts

                                if (m_VF == 1)
                                    m_CCR |= (byte)CCR_OVERFLOW;
                                else
                                    m_CCR &= (byte)~CCR_OVERFLOW;
                                break;

                            case 5: // C    m_CF will be set by instruction execution

                                if (m_CF == 1)
                                    m_CCR |= (byte)CCR_CARRY;
                                else
                                    m_CCR &= (byte)~CCR_CARRY;
                                break;
                        }
                        break;

                    case 16: // (All)   Test and set if true, cleared otherwise. (16 bit)

                        switch (i)
                        {
                            case 0: // H    m_HF will be set by instruction execution

                                if (m_HF == 1)
                                    m_CCR |= (byte)CCR_HALFCARRY;
                                else
                                    m_CCR &= (byte)~CCR_HALFCARRY;
                                break;

                            case 1: // I    Only set by SWI in case 13

                                break;

                            case 2: // N    use nRules[i] = 9 instead

                                if ((after & 0x8000) == 0x8000)
                                    m_CCR |= (byte)CCR_NEGATIVE;
                                else
                                    m_CCR &= (byte)~CCR_NEGATIVE;
                                break;

                            case 3: // Z

                                if (after == 0)
                                    m_CCR |= (byte)CCR_ZERO;
                                else
                                    m_CCR &= (byte)~CCR_ZERO;
                                break;

                            case 4: // V    Only on Add's, Compare's, DAA, and Subtracts

                                if (m_VF == 1)
                                    m_CCR |= (byte)CCR_OVERFLOW;
                                else
                                    m_CCR &= (byte)~CCR_OVERFLOW;
                                break;

                            case 5: // C    m_CF will be set by instruction execution

                                if (m_CF == 1)
                                    m_CCR |= (byte)CCR_CARRY;
                                else
                                    m_CCR &= (byte)~CCR_CARRY;
                                break;
                        }
                        break;

                    case 17:    // used by MUL only
                        if ((after & 0x0080) == 0x0080)
                            m_CCR |= (byte)CCR_CARRY;
                        else
                            m_CCR &= (byte)~CCR_CARRY;
                        break;
                }
            }
        }

        void DecimalAdjustAccumulator ()
        {
            byte CF, UB, HF, LB;

            CF = (byte)(m_CCR & CCR_CARRY);
            HF = (byte)((m_CCR & CCR_HALFCARRY) == 0 ? 0 : 2);
            UB = (byte)((_dReg.hi & 0xf0) >> 4);
            LB = (byte)(_dReg.hi & 0x0f);

            m_CF = 0;

            switch (CF + HF)
            {
                case 0:         // Carry clear - Halfcarry clear
                    while (true)
                    {
                        if ((UB >= 0 && UB <= 9) && (LB >= 0 && LB <= 9))
                        {
                            m_CF = 0;
                            SetCCR (_dReg.hi);
                            break;
                        }
                        if ((UB >= 0 && UB <= 8) && (LB >= 10 && LB <= 15))
                        {
                            _dReg.hi += 0x06;
                            m_CF = 0;
                            SetCCR (_dReg.hi);
                            break;
                        }
                        if ((UB >= 10 && UB <= 15) && (LB >= 0 && LB <= 9))
                        {
                            _dReg.hi += 0x60;
                            m_CF = 1;
                            SetCCR (_dReg.hi);
                            break;
                        }
                        if ((UB >= 9 && UB <= 15) && (LB >= 10 && LB <= 15))
                        {
                            _dReg.hi += 0x66;
                            m_CF = 1;
                            SetCCR (_dReg.hi);
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
                            _dReg.hi += 0x60;
                            m_CF = 1;
                            SetCCR (_dReg.hi);
                            break;
                        }
                        if ((UB >= 0 && UB <= 2) && (LB >= 10 && LB <= 15))
                        {
                            _dReg.hi += 0x66;
                            m_CF = 1;
                            SetCCR (_dReg.hi);
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
                            _dReg.hi += 0x06;
                            m_CF = 0;
                            SetCCR (_dReg.hi);
                            break;
                        }
                        if ((UB >= 10 && UB <= 15) && (LB >= 0 && LB <= 3))
                        {
                            _dReg.hi += 0x66;
                            m_CF = 1;
                            SetCCR (_dReg.hi);
                            break;
                        }
                        break;
                    }
                    break;
                case 3:         // Carry set - Halfcarry set
                    if ((UB >= 0 && UB <= 3) && (LB >= 0 && LB <= 3))
                    {
                        _dReg.hi += 0x66;
                        m_CF = 1;
                        SetCCR (_dReg.hi);
                    }
                    break;
            }
        }

        byte SubtractRegister (byte cReg, byte cOperand)
        {
            //  R - M -> R

            byte c;

            if (cReg < cOperand)
                m_CF = 1;
            else
                m_CF = 0;
    
            c = (byte)(cReg - cOperand);
    
            if (
                ((cReg & 0x80) == 0x80 && (cOperand & 0x80) == 0x00 && (c & 0x80) == 0x00) ||
                ((cReg & 0x80) == 0x00 && (cOperand & 0x80) == 0x80 && (c & 0x80) == 0x80)
               )
                m_VF = 1;
            else
                m_VF = 0;

            SetCCR (c);

            return (c);
        }

        ushort SubtractRegister (ushort cReg, ushort cOperand)
        {
            //  R - M -> R

            ushort c;

            if (cReg < cOperand)
                m_CF = 1;
            else
                m_CF = 0;

            c = (ushort)(cReg - cOperand);

            if (
                ((cReg & 0x8000) == 0x8000 && (cOperand & 0x8000) == 0x0000 && (c & 0x8000) == 0x0000) ||
                ((cReg & 0x8000) == 0x0000 && (cOperand & 0x8000) == 0x8000 && (c & 0x8000) == 0x8000)
               )
                m_VF = 1;
            else
                m_VF = 0;

            SetCCR (c);

            return (c);
        }

        void CompareRegister (byte cReg, byte cOperand)
        {
            //  R - M

            byte c;

            if (cReg < cOperand)
                m_CF = 1;
            else
                m_CF = 0;

            c = (byte)(cReg - cOperand);

            if (
                ((cReg & 0x80) == 0x80 && (cOperand & 0x80) == 0x00 && (c & 0x80) == 0x00) ||
                ((cReg & 0x80) == 0x00 && (cOperand & 0x80) == 0x80 && (c & 0x80) == 0x80)
               )
                m_VF = 1;
            else
                m_VF = 0;

            SetCCR (c);
        }

        void CompareRegister (ushort cReg, ushort cOperand)
        {
            //  R - M

            ushort c;

            if (cReg < cOperand)
                m_CF = 1;
            else
                m_CF = 0;
    
            c = (ushort)(cReg - cOperand);
            if (
                ((cReg & 0x8000) == 0x8000 && (cOperand & 0x8000) == 0x0000 && (c & 0x8000) == 0x0000) ||
                ((cReg & 0x8000) == 0x0000 && (cOperand & 0x8000) == 0x8000 && (c & 0x8000) == 0x8000)
               )
                m_VF = 1;
            else
                m_VF = 0;

            SetCCR (c);
        }

        byte SubtractWithCarryRegister (byte cReg, byte cOperand)
        {
            //  R - M - C -> R

            byte c;

            if (cReg < (cOperand + (m_CCR & CCR_CARRY)))
                m_CF = 1;
            else
                m_CF = 0;

            c = (byte)(cReg - cOperand - (m_CCR & CCR_CARRY));

            if (
                ((cReg & 0x80) == 0x80 && (cOperand & 0x80) == 0x00 && (c & 0x80) == 0x00) ||
                ((cReg & 0x80) == 0x00 && (cOperand & 0x80) == 0x80 && (c & 0x80) == 0x80)
               )
                m_VF = 1;
            else
                m_VF = 0;

            SetCCR (c);

            return (c);
        }

        byte AndRegister (byte cReg, byte cOperand)
        {
            cReg &= cOperand;

            SetCCR (cReg);

            return (cReg);
        }

        void BitRegister (byte cReg, byte cOperand)
        {
            byte c;

            c = (byte)(cReg & cOperand);
            SetCCR (c);
        }

        byte ExclusiveOrRegister (byte cReg, byte cOperand)
        {
            cReg ^= cOperand;
            SetCCR (cReg);

            return (cReg);
        }

        byte AddWithCarryRegister (byte cReg, byte cOperand)
        {
            //  R + M + C -> R

            byte c;

            if ((cReg + cOperand + (m_CCR & CCR_CARRY)) > 255)
                m_CF = 1;
            else
                m_CF = 0;

            if (((cReg & 0x0f) + (cOperand & 0x0f) + (m_CCR & CCR_CARRY)) > 15)
                m_HF = 1;
            else
                m_HF = 0;

            c = (byte)(cReg + cOperand + (m_CCR & CCR_CARRY));
            if (
                ((cReg & 0x80) == 0x80 && (cOperand & 0x80) == 0x80 && (c & 0x80) == 0x00) ||
                ((cReg & 0x80) == 0x00 && (cOperand & 0x80) == 0x00 && (c & 0x80) == 0x80)
               )
                m_VF = 1;
            else
                m_VF = 0;

            SetCCR (c);

            return (c);
        }

        byte OrRegister (byte cReg, byte cOperand)
        {
            cReg |= cOperand;
            SetCCR (cReg);

            return (cReg);
        }

        byte AddRegister (byte cReg, byte cOperand)
        {
            byte c;

            if ((cReg + cOperand) > 255)
                m_CF = 1;
            else
                m_CF = 0;

            if (((cReg & 0x0f) + (cOperand & 0x0f)) > 15)
                m_HF = 1;
            else
                m_HF = 0;

            c = (byte)(cReg + cOperand);
            if (
                ((cReg & 0x80) == 0x80 && (cOperand & 0x80) == 0x80 && (c & 0x80) == 0x00) ||
                ((cReg & 0x80) == 0x00 && (cOperand & 0x80) == 0x00 && (c & 0x80) == 0x80)
               )
                m_VF = 1;
            else
                m_VF = 0;

            SetCCR (c);

            return (c);
        }

        ushort AddRegister (ushort cReg, ushort cOperand)
        {
            ushort c;

            if ((cReg + cOperand) > 0x0000FFFF)
                m_CF = 1;
            else
                m_CF = 0;

            //if (((cReg & 0x000f) + (cOperand & 0x000f)) > 15)
            //    m_HF = 1;
            //else
            //    m_HF = 0;

            c = (ushort)(cReg + cOperand);
            if (
                ((cReg & 0x8000) == 0x8000 && (cOperand & 0x8000) == 0x8000 && (c & 0x8000) == 0x0000) ||
                ((cReg & 0x8000) == 0x0000 && (cOperand & 0x8000) == 0x0000 && (c & 0x8000) == 0x8000)
               )
                m_VF = 1;
            else
                m_VF = 0;

            SetCCR (c);

            return (c);
        }

        #endregion

        #region Utility functions for Instruction Execution

        static void WordSwap(ref ushort r1, ref ushort r2)
        {
	        ushort t;

	        t = r1; r1 = r2; r2 = t;
        }

        static void ByteSwap (ref byte r1, ref byte r2)
        {
	        byte t;

	        t = r1; r1 = r2; r2 = t;
        }

        //byte GetByteRegisterReference (int r)      // this is suppose to return a pointer to a byte
        //{
        //    switch (r)
        //    {
        //        case 0x08:
        //            return (_dReg.hi);
        //        case 0x09:
        //            return (_dReg.lo);
        //        case 0x0A:
        //            return (m_CCR);
        //        default:
        //            return (m_DP);
        //    }
        //}

        //ushort GetWordRegisterReference (int r)    // this is suppose to return a pointer to a short
        //{
        //    switch (r)
        //    {
        //        case 0x00:
        //            return m_D;
        //        case 0x01:
        //            return m_X;
        //        case 0x02:
        //            return m_Y;
        //        case 0x03:
        //            return m_U;
        //        case 0x04:
        //            return m_S;
        //        default:
        //            return (ushort)m_IP;
        //    }
        //}

        void ExchangeWordRegisters(int r2, int r1)
        {
            ushort t = 0;

            switch (r2)
            {
                case 0x00:
                    switch (r1)
                    {
                        case 0x01:
                            t = m_D; m_D = OffsetRegisters[(int)OffsetRegisterIndex.m_X]; OffsetRegisters[(int)OffsetRegisterIndex.m_X] = t;
                            break;
                        case 0x02:
                            t = m_D; m_D = OffsetRegisters[(int)OffsetRegisterIndex.m_Y]; OffsetRegisters[(int)OffsetRegisterIndex.m_Y] = t;
                            break;
                        case 0x03:
                            t = m_D; m_D = OffsetRegisters[(int)OffsetRegisterIndex.m_U]; OffsetRegisters[(int)OffsetRegisterIndex.m_U] = t;
                            break;
                        case 0x04:
                            t = m_D; m_D = OffsetRegisters[(int)OffsetRegisterIndex.m_S]; OffsetRegisters[(int)OffsetRegisterIndex.m_S] = t;
                            break;
                        case 0x05:
                            t = m_D; m_D = IP; IP = t;
                            break;
                    }
                    break;
                //return m_D;
                case 0x01:
                    switch (r1)
                    {
                        case 0x00:
                            t = OffsetRegisters[(int)OffsetRegisterIndex.m_X]; OffsetRegisters[(int)OffsetRegisterIndex.m_X] = m_D; m_D = t;
                            break;
                        case 0x02:
                            t = OffsetRegisters[(int)OffsetRegisterIndex.m_X]; OffsetRegisters[(int)OffsetRegisterIndex.m_X] = OffsetRegisters[(int)OffsetRegisterIndex.m_Y]; OffsetRegisters[(int)OffsetRegisterIndex.m_Y] = t;
                            break;
                        case 0x03:
                            t = OffsetRegisters[(int)OffsetRegisterIndex.m_X]; OffsetRegisters[(int)OffsetRegisterIndex.m_X] = OffsetRegisters[(int)OffsetRegisterIndex.m_U]; OffsetRegisters[(int)OffsetRegisterIndex.m_U] = t;
                            break;
                        case 0x04:
                            t = OffsetRegisters[(int)OffsetRegisterIndex.m_X]; OffsetRegisters[(int)OffsetRegisterIndex.m_X] = OffsetRegisters[(int)OffsetRegisterIndex.m_S]; OffsetRegisters[(int)OffsetRegisterIndex.m_S] = t;
                            break;
                        case 0x05:
                            t = OffsetRegisters[(int)OffsetRegisterIndex.m_X]; OffsetRegisters[(int)OffsetRegisterIndex.m_X] = IP; IP = t;
                            break;
                    }
                    break;
                //return m_X;
                case 0x02:
                    switch (r1)
                    {
                        case 0x00:
                            t = OffsetRegisters[(int)OffsetRegisterIndex.m_Y]; OffsetRegisters[(int)OffsetRegisterIndex.m_Y] = m_D; m_D = t;
                            break;
                        case 0x01:
                            t = OffsetRegisters[(int)OffsetRegisterIndex.m_Y]; OffsetRegisters[(int)OffsetRegisterIndex.m_Y] = OffsetRegisters[(int)OffsetRegisterIndex.m_X]; OffsetRegisters[(int)OffsetRegisterIndex.m_X] = t;
                            break;
                        case 0x03:
                            t = OffsetRegisters[(int)OffsetRegisterIndex.m_Y]; OffsetRegisters[(int)OffsetRegisterIndex.m_Y] = OffsetRegisters[(int)OffsetRegisterIndex.m_U]; OffsetRegisters[(int)OffsetRegisterIndex.m_U] = t;
                            break;
                        case 0x04:
                            t = OffsetRegisters[(int)OffsetRegisterIndex.m_Y]; OffsetRegisters[(int)OffsetRegisterIndex.m_Y] = OffsetRegisters[(int)OffsetRegisterIndex.m_S]; OffsetRegisters[(int)OffsetRegisterIndex.m_S] = t;
                            break;
                        case 0x05:
                            t = OffsetRegisters[(int)OffsetRegisterIndex.m_Y]; OffsetRegisters[(int)OffsetRegisterIndex.m_Y] = IP; IP = t;
                            break;
                    }
                    break;
                //return m_Y;
                case 0x03:
                    switch (r1)
                    {
                        case 0x00:
                            t = OffsetRegisters[(int)OffsetRegisterIndex.m_U]; OffsetRegisters[(int)OffsetRegisterIndex.m_U] = m_D; m_D = t;
                            break;
                        case 0x01:
                            t = OffsetRegisters[(int)OffsetRegisterIndex.m_U]; OffsetRegisters[(int)OffsetRegisterIndex.m_U] = OffsetRegisters[(int)OffsetRegisterIndex.m_X]; OffsetRegisters[(int)OffsetRegisterIndex.m_X] = t;
                            break;
                        case 0x02:
                            t = OffsetRegisters[(int)OffsetRegisterIndex.m_U]; OffsetRegisters[(int)OffsetRegisterIndex.m_U] = OffsetRegisters[(int)OffsetRegisterIndex.m_Y]; OffsetRegisters[(int)OffsetRegisterIndex.m_Y] = t;
                            break;
                        case 0x04:
                            t = OffsetRegisters[(int)OffsetRegisterIndex.m_U]; OffsetRegisters[(int)OffsetRegisterIndex.m_U] = OffsetRegisters[(int)OffsetRegisterIndex.m_S]; OffsetRegisters[(int)OffsetRegisterIndex.m_S] = t;
                            break;
                        case 0x05:
                            t = OffsetRegisters[(int)OffsetRegisterIndex.m_U]; OffsetRegisters[(int)OffsetRegisterIndex.m_U] = IP; IP = t;
                            break;
                    }
                    break;
                //return m_U;
                case 0x04:
                    switch (r1)
                    {
                        case 0x00:
                            t = OffsetRegisters[(int)OffsetRegisterIndex.m_S]; OffsetRegisters[(int)OffsetRegisterIndex.m_S] = m_D; m_D = t;
                            break;
                        case 0x01:
                            t = OffsetRegisters[(int)OffsetRegisterIndex.m_S]; OffsetRegisters[(int)OffsetRegisterIndex.m_S] = OffsetRegisters[(int)OffsetRegisterIndex.m_X]; OffsetRegisters[(int)OffsetRegisterIndex.m_X] = t;
                            break;
                        case 0x02:
                            t = OffsetRegisters[(int)OffsetRegisterIndex.m_S]; OffsetRegisters[(int)OffsetRegisterIndex.m_S] = OffsetRegisters[(int)OffsetRegisterIndex.m_Y]; OffsetRegisters[(int)OffsetRegisterIndex.m_Y] = t;
                            break;
                        case 0x03:
                            t = OffsetRegisters[(int)OffsetRegisterIndex.m_S]; OffsetRegisters[(int)OffsetRegisterIndex.m_S] = OffsetRegisters[(int)OffsetRegisterIndex.m_U]; OffsetRegisters[(int)OffsetRegisterIndex.m_U] = t;
                            break;
                        case 0x05:
                            t = OffsetRegisters[(int)OffsetRegisterIndex.m_S]; OffsetRegisters[(int)OffsetRegisterIndex.m_S] = IP; IP = t;
                            break;
                    }
                    break;
                //return m_S;
                case 0x05:
                    switch (r1)
                    {
                        case 0x00:
                            t = IP; IP = m_D; m_D = t;
                            break;
                        case 0x01:
                            t = IP; IP = OffsetRegisters[(int)OffsetRegisterIndex.m_X]; OffsetRegisters[(int)OffsetRegisterIndex.m_X] = t;
                            break;
                        case 0x02:
                            t = IP; IP = OffsetRegisters[(int)OffsetRegisterIndex.m_Y]; OffsetRegisters[(int)OffsetRegisterIndex.m_Y] = t;
                            break;
                        case 0x03:
                            t = IP; IP = OffsetRegisters[(int)OffsetRegisterIndex.m_U]; OffsetRegisters[(int)OffsetRegisterIndex.m_U] = t;
                            break;
                        case 0x04:
                            t = IP; IP = OffsetRegisters[(int)OffsetRegisterIndex.m_S]; OffsetRegisters[(int)OffsetRegisterIndex.m_S] = t;
                            break;
                    }
                    break;
                //return (ushort)m_IP;
            }
        }

        void ExchangeByteRegisters(int r2, int r1)
        {
            switch (r2)
            {
                case 0x08:
                    switch (r1)
                    {
                        case 0x09:
                            ByteSwap(ref _dReg.hi, ref _dReg.lo);
                            break;
                        case 0x0A:
                            ByteSwap(ref _dReg.hi, ref m_CCR);
                            break;
                        case 0x0B:
                            ByteSwap(ref _dReg.hi, ref m_DP);
                            break;
                    }
                    break;
                //return (_dReg.hi);
                case 0x09:
                    switch (r1)
                    {
                        case 0x08:
                            ByteSwap(ref _dReg.lo, ref _dReg.hi);
                            break;
                        case 0x0A:
                            ByteSwap(ref _dReg.lo, ref m_CCR);
                            break;
                        case 0x0B:
                            ByteSwap(ref _dReg.lo, ref m_DP);
                            break;
                    }
                    break;
                //return (_dReg.lo);
                case 0x0A:
                    switch (r1)
                    {
                        case 0x08:
                            ByteSwap(ref m_CCR, ref _dReg.hi);
                            break;
                        case 0x09:
                            ByteSwap(ref m_CCR, ref _dReg.lo);
                            break;
                        case 0x0B:
                            ByteSwap(ref m_CCR, ref m_DP);
                            break;
                    }
                    break;
                //return (m_CCR);
                default:
                    switch (r1)
                    {
                        case 0x08:
                            ByteSwap(ref m_DP, ref _dReg.hi);
                            break;
                        case 0x09:
                            ByteSwap(ref m_DP, ref _dReg.lo);
                            break;
                        case 0x0A:
                            ByteSwap(ref m_DP, ref m_CCR);
                            break;
                    }
                    break;
                //return (m_DP);
            }
        }

        void ExchangeRegisters ()
        {
	        int	r1, r2;
	
            r1 = (m_Operand & 0x00F0) >> 4;
	        r2 = (m_Operand & 0x000F);

            // see if this is a 16 bit transfer

	        if (r1 <= 5) 
            {
                // make sure that the target is also 16 bit

		        if (r2 > 5) 
                {
			        return;     // invalid - do nothing
		        }

                ExchangeWordRegisters(r2, r1);
            } 

            // must be an 8 bit transfer - see if it's valid

            else if ((r1 >= 8) && (r2 <= 11))
            {
                // make sure that the target is also 8 bits

		        if ((r2 < 8) || (r2 > 11))
                {
			        return;     // invalid - do nothing
		        }

                ExchangeByteRegisters(r2, r1);

	        }
            else  
            {
		        return;         // invalid - do nothing
	        }
        }

        void TransferWordRegisters (int r2, int r1)
        {
            switch (r2)
            {
                case 0x00:
                    switch (r1)
                    {
                        case 0x01:
                            m_D = OffsetRegisters[(int)OffsetRegisterIndex.m_X];
                            break;
                        case 0x02:
                            m_D = OffsetRegisters[(int)OffsetRegisterIndex.m_Y];
                            break;
                        case 0x03:
                            m_D = OffsetRegisters[(int)OffsetRegisterIndex.m_U];
                            break;
                        case 0x04:
                            m_D = OffsetRegisters[(int)OffsetRegisterIndex.m_S];
                            break;
                        case 0x05:
                            m_D = IP;
                            break;
                    }
                    break;
                //return m_D;
                case 0x01:
                    switch (r1)
                    {
                        case 0x00:
                            OffsetRegisters[(int)OffsetRegisterIndex.m_X] = m_D;
                            break;
                        case 0x02:
                            OffsetRegisters[(int)OffsetRegisterIndex.m_X] = OffsetRegisters[(int)OffsetRegisterIndex.m_Y];
                            break;
                        case 0x03:
                            OffsetRegisters[(int)OffsetRegisterIndex.m_X] = OffsetRegisters[(int)OffsetRegisterIndex.m_U];
                            break;
                        case 0x04:
                            OffsetRegisters[(int)OffsetRegisterIndex.m_X] = OffsetRegisters[(int)OffsetRegisterIndex.m_S];
                            break;
                        case 0x05:
                            OffsetRegisters[(int)OffsetRegisterIndex.m_X] = IP;
                            break;
                    }
                    break;
                //return m_X;
                case 0x02:
                    switch (r1)
                    {
                        case 0x00:
                            OffsetRegisters[(int)OffsetRegisterIndex.m_Y] = m_D;
                            break;
                        case 0x01:
                            OffsetRegisters[(int)OffsetRegisterIndex.m_Y] = OffsetRegisters[(int)OffsetRegisterIndex.m_X];
                            break;
                        case 0x03:
                            OffsetRegisters[(int)OffsetRegisterIndex.m_Y] = OffsetRegisters[(int)OffsetRegisterIndex.m_U];
                            break;
                        case 0x04:
                            OffsetRegisters[(int)OffsetRegisterIndex.m_Y] = OffsetRegisters[(int)OffsetRegisterIndex.m_S];
                            break;
                        case 0x05:
                            OffsetRegisters[(int)OffsetRegisterIndex.m_Y] = IP;
                            break;
                    }
                    break;
                //return m_Y;
                case 0x03:
                    switch (r1)
                    {
                        case 0x00:
                            OffsetRegisters[(int)OffsetRegisterIndex.m_U] = m_D;
                            break;
                        case 0x01:
                            OffsetRegisters[(int)OffsetRegisterIndex.m_U] = OffsetRegisters[(int)OffsetRegisterIndex.m_X];
                            break;
                        case 0x02:
                            OffsetRegisters[(int)OffsetRegisterIndex.m_U] = OffsetRegisters[(int)OffsetRegisterIndex.m_Y];
                            break;
                        case 0x04:
                            OffsetRegisters[(int)OffsetRegisterIndex.m_U] = OffsetRegisters[(int)OffsetRegisterIndex.m_S];
                            break;
                        case 0x05:
                            OffsetRegisters[(int)OffsetRegisterIndex.m_U] = IP;
                            break;
                    }
                    break;
                //return m_U;
                case 0x04:
                    switch (r1)
                    {
                        case 0x00:
                            OffsetRegisters[(int)OffsetRegisterIndex.m_S] = m_D;
                            break;
                        case 0x01:
                            OffsetRegisters[(int)OffsetRegisterIndex.m_S] = OffsetRegisters[(int)OffsetRegisterIndex.m_X];
                            break;
                        case 0x02:
                            OffsetRegisters[(int)OffsetRegisterIndex.m_S] = OffsetRegisters[(int)OffsetRegisterIndex.m_Y];
                            break;
                        case 0x03:
                            OffsetRegisters[(int)OffsetRegisterIndex.m_S] = OffsetRegisters[(int)OffsetRegisterIndex.m_U];
                            break;
                        case 0x05:
                            OffsetRegisters[(int)OffsetRegisterIndex.m_S] = IP;
                            break;
                    }
                    break;
                //return m_S;
                case 0x05:
                    switch (r1)
                    {
                        case 0x00:
                            IP = m_D;
                            break;
                        case 0x01:
                            IP = OffsetRegisters[(int)OffsetRegisterIndex.m_X];
                            break;
                        case 0x02:
                            IP = OffsetRegisters[(int)OffsetRegisterIndex.m_Y];
                            break;
                        case 0x03:
                            IP = OffsetRegisters[(int)OffsetRegisterIndex.m_U];
                            break;
                        case 0x04:
                            IP = OffsetRegisters[(int)OffsetRegisterIndex.m_S];
                            break;
                    }
                    break;
                //return (ushort)m_IP;
            }
        }

        void TransferByteRegisters (int r2, int r1)
        {
            switch (r2)
            {
                case 0x08:
                    switch (r1)
                    {
                        case 0x09:
                            _dReg.hi = _dReg.lo;
                            break;
                        case 0x0A:
                            _dReg.hi = m_CCR;
                            break;
                        case 0x0B:
                            _dReg.hi = m_DP;
                            break;
                    }
                    break;
                //return (_dReg.hi);
                case 0x09:
                    switch (r1)
                    {
                        case 0x08:
                            _dReg.lo = _dReg.hi;
                            break;
                        case 0x0A:
                            _dReg.lo = m_CCR;
                            break;
                        case 0x0B:
                            _dReg.lo = m_DP;
                            break;
                    }
                    break;
                //return (_dReg.lo);
                case 0x0A:
                    switch (r1)
                    {
                        case 0x08:
                            m_CCR = _dReg.hi;
                            break;
                        case 0x09:
                            m_CCR = _dReg.lo;
                            break;
                        case 0x0B:
                            m_CCR = m_DP;
                            break;
                    }
                    break;
                //return (m_CCR);
                default:
                    switch (r1)
                    {
                        case 0x08:
                            m_DP = _dReg.hi;
                            break;
                        case 0x09:
                            m_DP = _dReg.lo;
                            break;
                        case 0x0A:
                            m_DP = m_CCR;
                            break;
                    }
                    break;
                //return (m_DP);
            }
        }

        void TransferRegisters ()
        {
	        int	r1, r2;

	        r1 = (m_Operand & 0x00F0) >> 4;
	        r2 = (m_Operand & 0x000F);

	        if (r1 <= 5) 
            {
		        if (r2 > 5) 
                {
			        return;     // invalid - do nothing
		        }

                TransferWordRegisters(r2, r1);

	        } 
            else if ((r1 >= 8) && (r2 <= 11))
            {
		        if ((r2 < 8) || (r2 > 11))
                {
			        return;     // invalid - do nothing
		        }

                TransferByteRegisters(r2, r1);

	        } 
            else  
            {
		        return;         // invalid - do nothing
	        }
        }

        void PushMemoryByte (ushort sLogicalAddress, byte b)
        {
            StoreMemoryByte (b, sLogicalAddress);
        }

        void PushOntoStack (byte cFlags, ref ushort sStack, int nWhichStack)
        {
	        if ((cFlags & 0x80) != 0)
            {
                PushMemoryByte (--sStack, (byte)(IP % 256));
                PushMemoryByte (--sStack, (byte)(IP / 256));

                //CallStack.Add(IP);
	        }
	
            if ((cFlags & 0x40) != 0)
            {
                if (nWhichStack == SYSTEM_STACK)
                {
                    PushMemoryByte (--sStack, (byte)(OffsetRegisters[(int)OffsetRegisterIndex.m_U] % 256));
                    PushMemoryByte(--sStack, (byte)(OffsetRegisters[(int)OffsetRegisterIndex.m_U] / 256));
                }
                else
                {
                    PushMemoryByte (--sStack, (byte)(OffsetRegisters[(int)OffsetRegisterIndex.m_S] % 256));
                    PushMemoryByte (--sStack, (byte)(OffsetRegisters[(int)OffsetRegisterIndex.m_S] / 256));
                }
	        }
	
            if ((cFlags & 0x20) != 0)
            {
                PushMemoryByte (--sStack, (byte)(OffsetRegisters[(int)OffsetRegisterIndex.m_Y] % 256));
                PushMemoryByte (--sStack, (byte)(OffsetRegisters[(int)OffsetRegisterIndex.m_Y] / 256));
	        }
	
            if ((cFlags & 0x10) != 0)
            {
                PushMemoryByte(--sStack, (byte)(OffsetRegisters[(int)OffsetRegisterIndex.m_X] % 256));
                PushMemoryByte(--sStack, (byte)(OffsetRegisters[(int)OffsetRegisterIndex.m_X] / 256));
	        }
	
            if ((cFlags & 0x08) != 0)
                PushMemoryByte (--sStack,  m_DP);

            if ((cFlags & 0x04) != 0)
                PushMemoryByte (--sStack,  _dReg.lo);
	
            if ((cFlags & 0x02) != 0)
                PushMemoryByte (--sStack,  _dReg.hi);
	
            if ((cFlags & 0x01) != 0)
                PushMemoryByte (--sStack,  m_CCR);
        }

        byte PullMemoryByte (ushort sLogicalAddress)
        {
            return LoadMemoryByte (sLogicalAddress);
        }

        void PullFromStack (byte cFlags, ref ushort sStack, int nWhichStack)
        {
            if ((cFlags & 0x01) != 0)
                m_CCR = LoadMemoryByte (sStack++);

	        if ((cFlags & 0x02) != 0)
                _dReg.hi = LoadMemoryByte (sStack++);

	        if ((cFlags & 0x04) != 0)
                _dReg.lo = LoadMemoryByte (sStack++);

	        if ((cFlags & 0x08) != 0)
                m_DP = LoadMemoryByte (sStack++);

	        if ((cFlags & 0x10) != 0)
            {
                OffsetRegisters[(int)OffsetRegisterIndex.m_X] = (ushort)(LoadMemoryByte(sStack++) << 8);
                OffsetRegisters[(int)OffsetRegisterIndex.m_X] += LoadMemoryByte(sStack++);
	        }
	
            if ((cFlags & 0x20) != 0)
            {
		        OffsetRegisters[(int)OffsetRegisterIndex.m_Y]  = (ushort)(LoadMemoryByte (sStack++) << 8);
                OffsetRegisters[(int)OffsetRegisterIndex.m_Y] += LoadMemoryByte (sStack++);
	        }
	
            if ((cFlags & 0x40) != 0)
            {
                if (nWhichStack == SYSTEM_STACK)
                {
		            OffsetRegisters[(int)OffsetRegisterIndex.m_U]  = (ushort)(LoadMemoryByte (sStack++) << 8);
                    OffsetRegisters[(int)OffsetRegisterIndex.m_U] += LoadMemoryByte (sStack++);
                }
                else
                {
		            OffsetRegisters[(int)OffsetRegisterIndex.m_S]  = (ushort)(LoadMemoryByte (sStack++) << 8);
                    OffsetRegisters[(int)OffsetRegisterIndex.m_S] += LoadMemoryByte (sStack++);
                }
	        }
	
            if ((cFlags & 0x80) != 0)
            {
		        IP  = (ushort)(LoadMemoryByte (sStack++) << 8);
                IP += LoadMemoryByte (sStack++);

                //if (CallStack.Count > 0)
                //    CallStack.RemoveAt(CallStack.Count - 1);
	        }
        }

        #endregion

        #region Instruction Execution by Addressing mode

        void IllegalInstruction ()
        {
            //CButton *pOnOffButton = (CButton *) m_pView->GetDlgItem (IDC_BUTTON_ON_OFF);
            //CButton *pDebugButton = (CButton *) m_pView->GetDlgItem (IDC_BUTTON_DEBUG);

            //char [] szMessage = new char[128];

            //pOnOffButton->SetWindowText ("&ON");
            //pDebugButton->SetWindowText ("&Debug");

            //Running = false;
            //m_nSingleStepMode  = false;

            //sprintf_s (
            //            szMessage, 
            //            sizeof (szMessage),
            //            "Invalid OPCODE [0x%02X] encountered at address [0x%02X%03X]", 
            //            m_OpCode,
            //            (m_MemoryDAT[(((m_CurrentIP) & 0xF000) >> 12) & 0x0F] >> 12),
            //            (m_IP - 1) & 0x0FFF
            //        );

            //AfxMessageBox (szMessage);

            // CoreDump ();
            // DumpTraceBuffers ();
        }

        void Execute6809Inherent ()
        {
            byte cReg;
            bool nOldCarry;

            switch (m_OpCode)
            {
                case 0x12:  // "NOP  ",    0x12, AM_INHERENT_6809,   1,  2,   0,  0,  0,  0,  0,  0, 0,    // MC6809
                    break;

                case 0x13:  // "SYNC ",    0x13, AM_INHERENT_6809,   1,  2,   0,  0,  0,  0,  0,  0, 0,    // MC6809
                    InSync = true;
                    if (!IrqAsserted)
                        Program._cpuThread.Suspend();
                    break;
                    
                case 0x19:  // "DAA  ",    0x19, AM_INHERENT_6809,   1,  2,   0,  0, 15, 15, 15,  3, 0,    // MC6809 X
                    DecimalAdjustAccumulator ();
                    break;

                case 0x1D:  // "SEX  ",    0x1D, AM_INHERENT_6809,   1,  2,   0,  0,  0,  0,  0,  0, 0,    // MC6809
                    if ((_dReg.lo & 0x80) == 0x80)
                    {
                        _dReg.hi = 0xFF;
                        m_CCR |= (byte)CCR_NEGATIVE;
                        m_CCR &= (byte)~CCR_ZERO;
                    }
                    else
                    {
                        _dReg.hi = 0x00;
                        m_CCR &= (byte)~CCR_NEGATIVE;
                        if (_dReg.lo == 0x00)
                            m_CCR |= (byte)CCR_ZERO;
                        else
                            m_CCR &= (byte)~CCR_ZERO;
                    }
                    break;

                case 0x1E:  // "EXG  ",    0x1E, AM_INHERENT_6809,   2,  8,   0,  0,  0,  0,  0,  0, 0,    // MC6809 X
                    ExchangeRegisters ();
                    break;

                case 0x1F:  // "TFR  ",    0x1F, AM_INHERENT_6809,   2,  6,   0,  0,  0,  0,  0,  0, 0,    // MC6809
                    TransferRegisters ();
                    break;

                case 0x34:  // "PSH S",    0x34, AM_INHERENT_6809,   2,  5,   0,  0,  0,  0,  0,  0, 0,    // MC6809 
        	        PushOntoStack ((byte) m_Operand, ref OffsetRegisters[(int)OffsetRegisterIndex.m_S], (int)SYSTEM_STACK);
                    break;

                case 0x35:  // "PUL S",    0x35, AM_INHERENT_6809,   2,  5,   0,  0,  0,  0,  0,  0, 0,    // MC6809 
        	        PullFromStack ((byte) m_Operand, ref OffsetRegisters[(int)OffsetRegisterIndex.m_S], (int)SYSTEM_STACK);
                    break;

                case 0x36:  // "PSH U",    0x36, AM_INHERENT_6809,   2,  5,   0,  0,  0,  0,  0,  0, 0,    // MC6809 
        	        PushOntoStack ((byte) m_Operand, ref OffsetRegisters[(int)OffsetRegisterIndex.m_U], (int)USER_STACK);
                    break;

                case 0x37:  // "PUL U",    0x37, AM_INHERENT_6809,   2,  5,   0,  0,  0,  0,  0,  0, 0,    // MC6809 
        	        PullFromStack ((byte) m_Operand, ref OffsetRegisters[(int)OffsetRegisterIndex.m_U], (int)USER_STACK);
                    break;
                    
                case 0x39:  // "RTS  ",    0x39, AM_INHERENT_6809,   1,  5,   0,  0,  0,  0,  0,  0, 0,    // MC6809 X
                    IP  = (ushort)(LoadMemoryByte (OffsetRegisters[(int)OffsetRegisterIndex.m_S]++) << 8);
                    IP += LoadMemoryByte (OffsetRegisters[(int)OffsetRegisterIndex.m_S]++);
                    
                    //if (CallStack.Count > 0)
                    //    CallStack.RemoveAt(CallStack.Count - 1);

                    break;

                case 0x3A:  // "ABX  ",    0x3A, AM_INHERENT_6809,   1,  3,   0,  0,  0,  0,  0,  0, 0,    // MC6809 X
                    OffsetRegisters[(int)OffsetRegisterIndex.m_X] += _dReg.lo;
                    break;
                    
                case 0x3B:  // "RTI  ",    0x3B, AM_INHERENT_6809,   1,  6,  10, 10, 10, 10, 10, 10, 0,    // MC6809
	                PullFromStack (0x01, ref OffsetRegisters[(int)OffsetRegisterIndex.m_S], (int)SYSTEM_STACK);
	                if ((m_CCR & CCR_ENTIREFLAG)  != 0)
                        PullFromStack (0xFE, ref OffsetRegisters[(int)OffsetRegisterIndex.m_S], (int)SYSTEM_STACK);        // get everything (CCR was already pulled)
                    else
                        PullFromStack (0x80, ref OffsetRegisters[(int)OffsetRegisterIndex.m_S], (int)SYSTEM_STACK);        // just get the PC and the CCR (CCR was already pulled)
                    break;

                case 0x3D:  // "MUL  ",    0x3D, AM_INHERENT_6809,   1, 11,   0,  0,  0, 16,  0, 17, 0,    // MC6809 X
                    m_D = (ushort)(_dReg.hi * _dReg.lo);
                    SetCCR(m_D);
                    break;
                    
                    
                case 0x3F:  // "SWI  ",    0x3F, AM_INHERENT_6809,   1, 12,   0, 13,  0,  0,  0,  0, 0,    // MC6809
                    m_CCR |= CCR_ENTIREFLAG;
                    PushOntoStack (0xFF, ref OffsetRegisters[(int)OffsetRegisterIndex.m_S], (int)SYSTEM_STACK);
                    m_CCR |= CCR_INTERRUPT;
                    m_CCR |= CCR_FIRQMASK;
                    IP = LoadMemoryWord (0xFFFA);
                    break;

                case 0x40:  // "NEG A",    0x40, AM_INHERENT_6809,   1,  2,   0,  0, 15, 15,  1,  2, 0,    // MC6809 X
                    cReg = _dReg.hi;
                    _dReg.hi = (byte) (0x00 - _dReg.hi);
                    SetCCR (_dReg.hi, cReg);
                    break;

                case 0x43:  // "COM A",    0x43, AM_INHERENT_6809,   1,  2,   0,  0, 15, 15, 14, 13, 0,    // MC6809 X
                    _dReg.hi = (byte) (0xFF - _dReg.hi);
                    SetCCR (_dReg.hi);
                    break;

                case 0x44:  // "LSR A",    0x44, AM_INHERENT_6809,   1,  2,   0,  0, 14, 15,  0, 15, 0,    // MC6809 X
                    m_CF = (byte)(_dReg.hi & 0x01);
                    _dReg.hi = (byte)((_dReg.hi >> 1) & 0x7F);
                    SetCCR (_dReg.hi);
                    break;

                case 0x46:  // "ROR A",    0x46, AM_INHERENT_6809,   1,  2,   0,  0, 15, 15,  0, 15, 0,    // MC6809
                    nOldCarry = (m_CCR & CCR_CARRY) == 0 ? false : true;
                    m_CF = (byte)(_dReg.hi & 0x01);
                    _dReg.hi = (byte)(_dReg.hi >> 1);
                    if (nOldCarry)
                        _dReg.hi = (byte)(_dReg.hi | 0x80);
                    else
                        _dReg.hi = (byte)(_dReg.hi & 0x7F);
                    SetCCR (_dReg.hi);
                    break;
                    
                case 0x47:  // "ASR A",    0x47, AM_INHERENT_6809,   1,  2,   0,  0, 15, 15,  0, 15, 0,    // MC6809 X
                    cReg = _dReg.hi;
                    m_CF = (byte)(_dReg.hi & 0x01);
                    _dReg.hi = (byte)(_dReg.hi >> 1);
                    _dReg.hi = (byte)(_dReg.hi | (cReg & 0x80));
                    SetCCR (_dReg.hi);
                    break;

                case 0x48:  // "ASL A",    0x48, AM_INHERENT_6809,   1,  2,   0,  0, 15, 15,  6, 15, 0,    // MC6809 X
                    cReg = _dReg.hi;
                    if ((_dReg.hi & 0x80) == 0x80)
                        m_CF = 1;
                    else
                        m_CF = 0;
                    _dReg.hi  = (byte)(_dReg.hi << 1);
                    SetCCR (_dReg.hi, cReg);
                    break;

                case 0x49:  // "ROL A",    0x49, AM_INHERENT_6809,   1,  2,   0,  0, 15, 15,  6, 15, 0,    // MC6809
                    cReg = _dReg.hi;
                    if ((_dReg.hi & 0x80) == 0x80)
                        m_CF = 1;
                    else
                        m_CF = 0;
                    _dReg.hi  = (byte)(_dReg.hi << 1);
                    _dReg.hi |= (byte)(m_CCR & CCR_CARRY);
                    SetCCR (_dReg.hi, cReg);
                    break;

                case 0x4A:  // "DEC A",    0x4A, AM_INHERENT_6809,   1,  2,   0,  0, 15, 15,  4,  0, 0,    // MC6809 X
                    cReg = _dReg.hi;
                    _dReg.hi = (byte)(_dReg.hi - 1);
                    SetCCR (_dReg.hi, cReg);
                    break;

                case 0x4C:  // "INC A",    0x4C, AM_INHERENT_6809,   1,  2,   0,  0, 15, 15,  5,  0, 0,    // MC6809 X
                    cReg = _dReg.hi;
                    _dReg.hi = (byte)(_dReg.hi + 1);
                    SetCCR (_dReg.hi, cReg);
                    break;

                case 0x4D:  // "TST A",    0x4D, AM_INHERENT_6809,   1,  2,   0,  0, 15, 15, 14,  0, 0,    // MC6809
                    SetCCR (_dReg.hi);
                    break;

                case 0x4F:  // "CLR A",    0x4F, AM_INHERENT_6809,   1,  2,   0,  0, 14, 13, 14, 14, 0,    // MC6809 X
                    _dReg.hi = 0x00;
                    SetCCR (_dReg.hi);
                    break;

                case 0x50:  // "NEG B",    0x50, AM_INHERENT_6809,   1,  2,   0,  0, 15, 15,  1,  2, 0,    // MC6809 X
                    cReg = _dReg.lo;
                    _dReg.lo = (byte) (0x00 - _dReg.lo);
                    SetCCR (_dReg.lo, cReg);
                    break;

                case 0x53:  // "COM B",    0x53, AM_INHERENT_6809,   1,  2,   0,  0, 15, 15, 14, 13, 0,    // MC6809 X
                    _dReg.lo = (byte) (0xFF - _dReg.lo);
                    SetCCR (_dReg.lo);
                    break;

                case 0x54:  // "LSR B",    0x54, AM_INHERENT_6809,   1,  2,   0,  0, 14, 15,  0, 15, 0,    // MC6809 X
                    m_CF = (byte)(_dReg.lo & 0x01);
                    _dReg.lo = (byte)((_dReg.lo >> 1) & 0x7F);
                    SetCCR (_dReg.lo);
                    break;
            
                case 0x56:  // "ROR B",    0x56, AM_INHERENT_6809,   1,  2,   0,  0, 15, 15,  0, 15, 0,    // MC6809
                    nOldCarry = (m_CCR & CCR_CARRY) == 0 ? false : true;
                    //nOldCarry = m_CCR & CCR_CARRY;
                    m_CF = (byte)(_dReg.lo & 0x01);
                    _dReg.lo = (byte)(_dReg.lo >> 1);
                    if (nOldCarry)
                        _dReg.lo = (byte)(_dReg.lo | 0x80);
                    else
                        _dReg.lo = (byte)(_dReg.lo & 0x7F);
                    SetCCR (_dReg.lo);
                    break;

                case 0x57:  // "ASR B",    0x57, AM_INHERENT_6809,   1,  2,   0,  0, 15, 15,  0, 15, 0,    // MC6809 X
                    cReg = _dReg.lo;
                    m_CF = (byte)(_dReg.lo & 0x01);
                    _dReg.lo = (byte)(_dReg.lo >> 1);
                    _dReg.lo = (byte)(_dReg.lo | (cReg & 0x80));
                    SetCCR (_dReg.lo);
                    break;

                case 0x58:  // "ASL B",    0x58, AM_INHERENT_6809,   1,  2,   0,  0, 15, 15,  6, 15, 0,    // MC6809 X
                    cReg = _dReg.lo;
                    if ((_dReg.lo & 0x80) == 0x80)
                        m_CF = 1;
                    else
                        m_CF = 0;
                    _dReg.lo  = (byte)(_dReg.lo << 1);
                    SetCCR (_dReg.lo, cReg);
                    break;

                case 0x59:  // "ROL B",    0x59, AM_INHERENT_6809,   1,  2,   0,  0, 15, 15,  6, 15, 0,    // MC6809
                    cReg = _dReg.lo;
                    if ((_dReg.lo & 0x80) == 0x80)
                        m_CF = 1;
                    else
                        m_CF = 0;
                    _dReg.lo  = (byte)(_dReg.lo << 1);
                    _dReg.lo |= (byte)(m_CCR & CCR_CARRY);
                    SetCCR (_dReg.lo, cReg);
                    break;

                case 0x5A:  // "DEC B",    0x5A, AM_INHERENT_6809,   1,  2,   0,  0, 15, 15,  4,  0, 0,    // MC6809 X
                    cReg = _dReg.lo;
                    _dReg.lo = (byte)(_dReg.lo - 1);
                    SetCCR (_dReg.lo, cReg);
                    break;
                    
                case 0x5C:  // "INC B",    0x5C, AM_INHERENT_6809,   1,  2,   0,  0, 15, 15,  5,  0, 0,    // MC6809 X
                    cReg = _dReg.lo;
                    _dReg.lo = (byte)(_dReg.lo + 1);
                    SetCCR (_dReg.lo, cReg);
                    break;

                case 0x5D:  // "TST B",    0x5D, AM_INHERENT_6809,   1,  2,   0,  0, 15, 15, 14,  0, 0,    // MC6809
                    SetCCR (_dReg.lo);
                    break;

                case 0x5F:  // "CLR B",    0x5F, AM_INHERENT_6809,   1,  2,   0,  0, 14, 13, 14, 14, 0,    // MC6809 X
                    _dReg.lo = 0x00;
                    SetCCR (_dReg.lo);
                    break;
            }
        }

        void Execute6809Extended ()
        {
            bool nOldCarry;

            ushort sData;
            byte  cData;
            byte  cReg;
            ushort sBefore;
            //ushort sResult;

            switch (m_OpCode)
            {
                case 0x70:  // "NEG  ",    0x70, AM_EXTENDED_6809,   3,  6,   0,  0, 15, 15,  1,  2, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    sBefore = cData;
                    cData = (byte) (0x00 - cData);
                    StoreMemoryByte (cData, m_Operand);
                    SetCCR (cData, sBefore);
                    break;

                case 0x73:  // "COM  ",    0x73, AM_EXTENDED_6809,   3,  6,   0,  0, 15, 15, 14, 13, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    cData = (byte) (0xFF - cData);
                    StoreMemoryByte (cData, m_Operand);
                    SetCCR (cData);
                    break;

                case 0x74:  // "LSR  ",    0x74, AM_EXTENDED_6809,   3,  6,   0,  0, 14, 15,  0, 15, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    m_CF = (byte)(cData & 0x01);
                    cData = (byte)((cData >> 1) & 0x7F);
                    StoreMemoryByte (cData, m_Operand);
                    SetCCR (cData);
                    break;

                case 0x76:  // "ROR  ",    0x76, AM_EXTENDED_6809,   3,  6,   0,  0, 15, 15,  0, 15, 0,    // MC6809
                    nOldCarry = (m_CCR & CCR_CARRY) == 0 ? false : true;
                    //nOldCarry = m_CCR & CCR_CARRY;
                    cData = LoadMemoryByte (m_Operand);
                    m_CF = (byte)(cData & 0x01);
                    cData = (byte)(cData >> 1);
                    if (nOldCarry)
                        cData = (byte)(cData | 0x80);
                    else
                        cData = (byte)(cData & 0x7F);
                    StoreMemoryByte (cData, m_Operand);
                    SetCCR (cData);
                    break;

                case 0x77:  // "ASR  ",    0x77, AM_EXTENDED_6809,   3,  6,   0,  0, 15, 15,  0, 15, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    cReg = cData;
                    m_CF = (byte)(cData & 0x01);
                    cData = (byte)(cData >> 1);
                    cData = (byte)(cData | (cReg & 0x80));
                    StoreMemoryByte (cData, m_Operand);
                    SetCCR (cData);
                    break;

                case 0x78:  // "ASL  ",    0x78, AM_EXTENDED_6809,   3,  6,   0,  0, 15, 15,  6, 15, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    cReg = cData;
                    if ((cData & 0x80) == 0x80)
                        m_CF = 1;
                    else
                        m_CF = 0;
                    cData  = (byte)(cData << 1);
                    StoreMemoryByte (cData, m_Operand);
                    SetCCR (cData, cReg);
                    break;

                case 0x79:  // "ROL  ",    0x79, AM_EXTENDED_6809,   3,  6,   0,  0, 15, 15,  6, 15, 0,    // MC6809
                    cData = LoadMemoryByte (m_Operand);
                    cReg = cData;
                    if ((cData & 0x80) == 0x80)
                        m_CF = 1;
                    else
                        m_CF = 0;
                    cData  = (byte)(cData << 1);
                    cData |= (byte)(m_CCR & CCR_CARRY);
                    StoreMemoryByte (cData, m_Operand);
                    SetCCR (cData, cReg);
                    break;

                case 0x7A:  // "DEC  ",    0x7A, AM_EXTENDED_6809,   3,  6,   0,  0, 15, 15,  4,  0, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    cReg = cData;
                    cData -= 1;
                    StoreMemoryByte (cData, m_Operand);
                    SetCCR (cData, cReg);
                    break;

                case 0x7C:  // "INC  ",    0x7C, AM_EXTENDED_6809,   3,  6,   0,  0, 15, 15,  5,  0, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    cReg = cData;
                    cData += 1;
                    StoreMemoryByte (cData, m_Operand);
                    SetCCR (cData, cReg);
                    break;

                case 0x7D:  // "TST  ",    0x7D, AM_EXTENDED_6809,   3,  6,   0,  0, 15, 15, 14,  0, 0,    // MC6809
                    cData = LoadMemoryByte (m_Operand);
                    SetCCR (cData);
                    break;

                case 0x7E:  // "JMP  ",    0x7E, AM_EXTENDED_6809,   3,  3,   0,  0,  0,  0,  0,  0, 0,    // MC6809
                    IP = m_Operand;
                    break;

                case 0x7F:  // "CLR  ",    0x7F, AM_EXTENDED_6809,   3,  6,   0,  0, 14, 13, 14, 14, 0,    // MC6809 X
                    cData = 0x00;
                    StoreMemoryByte (cData, m_Operand);
                    SetCCR (cData);
                    break;

                // Register A and B Extended Instructions

                case 0xB0:  // "SUB A",    0xB0, AM_EXTENDED_6809,   3,  5,   0,  0, 15, 15, 15, 15, 0,    // MC6809
                    cData = LoadMemoryByte (m_Operand);
                    _dReg.hi = SubtractRegister (_dReg.hi, (byte) cData);
                    break;
        
                case 0xB1:  // "CMP A",    0xB1, AM_EXTENDED_6809,   3,  5,   0,  0, 15, 15, 15, 15, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    CompareRegister (_dReg.hi, (byte) cData);
                    break;
        
                case 0xB2:  // "SBC A",    0xB2, AM_EXTENDED_6809,   3,  5,   0,  0, 15, 15, 15, 15, 0,    // MC6809
                    cData = LoadMemoryByte (m_Operand);
                    _dReg.hi = SubtractWithCarryRegister (_dReg.hi, (byte) cData);
                    break;

                case 0xB3:  // "SUB D",    0xB3, AM_EXTENDED_6809,   3,  7,   0,  0, 16, 16, 16, 16, 0,    // MC6809
                    sData = LoadMemoryWord (m_Operand);
                    m_D = SubtractRegister(m_D, sData);
                    break;

                case 0xB4:  // "AND A",    0xB4, AM_EXTENDED_6809,   3,  5,   0,  0, 15, 15, 14,  0, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    _dReg.hi = AndRegister (_dReg.hi, (byte) cData);
                    break;
        
                case 0xB5:  // "BIT A",    0xB5, AM_EXTENDED_6809,   3,  5,   0,  0, 15, 15, 14,  0, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    BitRegister (_dReg.hi, (byte) cData);
                    break;
        
                case 0xB6:  // "LDA A",    0xB6, AM_EXTENDED_6809,   3,  5,   0,  0, 15, 15, 14,  0, 0,    // MC6809 X
                    _dReg.hi = LoadMemoryByte (m_Operand);
                    SetCCR (_dReg.hi);
                    break;
        
                case 0xB7:  // "STA A",    0xB7, AM_EXTENDED_6809,   3,  5,   0,  0, 15, 15, 14,  0, 0,    // MC6809
                    StoreMemoryByte (_dReg.hi, m_Operand);
                    SetCCR (_dReg.hi);
                    break;

                case 0xB8:  // "EOR A",    0xB8, AM_EXTENDED_6809,   3,  5,   0,  0, 15, 15, 14,  0, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    _dReg.hi = ExclusiveOrRegister (_dReg.hi, (byte) cData);
                    break;
        
                case 0xB9:  // "ADC A",    0xB9, AM_EXTENDED_6809,   3,  5,  15,  0, 15, 15, 15, 15, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    _dReg.hi = AddWithCarryRegister (_dReg.hi, (byte) cData);
                    break;
        
                case 0xBA:  // "ORA A",    0xBA, AM_EXTENDED_6809,   3,  5,   0,  0, 15, 15, 14,  0, 0,    // MC6809
                    cData = LoadMemoryByte (m_Operand);
                    _dReg.hi = OrRegister (_dReg.hi, (byte) cData);
                    break;
        
                case 0xBB:  // "ADD A",    0xBB, AM_EXTENDED_6809,   3,  5,  15,  0, 15, 15, 15, 15, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    _dReg.hi = AddRegister (_dReg.hi, (byte) cData);
                    break;

                case 0xBC:  // "CMP X",    0xBC, AM_EXTENDED_6809,   3,  7,   0,  0, 16, 16, 16, 16, 0,    // MC6809
                    sData = LoadMemoryWord (m_Operand);
                    CompareRegister(OffsetRegisters[(int)OffsetRegisterIndex.m_X], sData);
                    //sBefore = offsetRegisters[(int)OffsetRegisterIndex.m_X];
                    //sResult = offsetRegisters[(int)OffsetRegisterIndex.m_X] - sData;
                    //if (offsetRegisters[(int)OffsetRegisterIndex.m_X] < sData)
                    //    m_CF = 1;
                    //else
                    //    m_CF = 0;
                    //SetCCR (sResult, sBefore);
                    break;

                case 0xBD:  // "JSR  ",    0xBD, AM_EXTENDED_6809,   3,  8,   0,  0,  0,  0,  0,  0, 0,    // MC6809

                    // First save current IP

                    WriteMemByte (--OffsetRegisters[(int)OffsetRegisterIndex.m_S], (byte)(IP % 256));
                    WriteMemByte (--OffsetRegisters[(int)OffsetRegisterIndex.m_S], (byte)(IP / 256));
                    //CallStack.Add(IP);

                    // Then load new IP
            
                    IP = m_Operand;
                    break;

                case 0xBE:  // "LDX  ",    0xBE, AM_EXTENDED_6809,   3,  6,   0,  0, 16, 16, 14,  0, 0,    // MC6809
                    OffsetRegisters[(int)OffsetRegisterIndex.m_X] = LoadMemoryWord(m_Operand);
                    SetCCR(OffsetRegisters[(int)OffsetRegisterIndex.m_X]);
                    break;

                case 0xBF:  // "STX  ",    0xBF, AM_EXTENDED_6809,   3,  6,   0,  0, 16, 16, 14,  0, 0,    // MC6809
                    StoreMemoryWord(OffsetRegisters[(int)OffsetRegisterIndex.m_X], m_Operand);
                    SetCCR(OffsetRegisters[(int)OffsetRegisterIndex.m_X]);
                    break;

                case 0xF0:  // "SUB B",    0xF0, AM_EXTENDED_6809,   3,  5,   0,  0, 15, 15, 15, 15, 0,    // MC6809
                    cData = LoadMemoryByte (m_Operand);
                    _dReg.lo = SubtractRegister (_dReg.lo, (byte) cData);
                    break;
        
                case 0xF1:  // "CMP B",    0xF1, AM_EXTENDED_6809,   3,  5,   0,  0, 15, 15, 15, 15, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    CompareRegister (_dReg.lo, (byte) cData);
                    break;
        
                case 0xF2:  // "SBC B",    0xF2, AM_EXTENDED_6809,   3,  5,   0,  0, 15, 15, 15, 15, 0,    // MC6809
                    cData = LoadMemoryByte (m_Operand);
                    _dReg.lo = SubtractWithCarryRegister (_dReg.lo, (byte) cData);
                    break;
        
                case 0xF3:// "ADD D",    0xF3, AM_EXTENDED_6809,     3,  7,   0,  0, 16, 16, 16, 16, 0,    // MC6809 X
                    sData = LoadMemoryWord (m_Operand);
                    m_D = AddRegister(m_D, sData);
                    break;

                case 0xF4:  // "AND B",    0xF4, AM_EXTENDED_6809,   3,  5,   0,  0, 15, 15, 14,  0, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    _dReg.lo = AndRegister (_dReg.lo, (byte) cData);
                    break;
        
                case 0xF5:  // "BIT B",    0xF5, AM_EXTENDED_6809,   3,  5,   0,  0, 15, 15, 14,  0, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    BitRegister (_dReg.lo, (byte) cData);
                    break;
        
                case 0xF6:  // "LDA B",    0xF6, AM_EXTENDED_6809,   3,  5,   0,  0, 15, 15, 14,  0, 0,    // MC6809 X
                    _dReg.lo = LoadMemoryByte (m_Operand);
                    SetCCR (_dReg.lo);
                    break;
        
                case 0xF7:  // "STA B",    0xF7, AM_EXTENDED_6809,   3,  5,   0,  0, 15, 15, 14,  0, 0,    // MC6809
                    StoreMemoryByte (_dReg.lo, m_Operand);
                    SetCCR (_dReg.lo);
                    break;
        
                case 0xF8:  // "EOR B",    0xF8, AM_EXTENDED_6809,   3,  5,   0,  0, 15, 15, 14,  0, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    _dReg.lo = ExclusiveOrRegister (_dReg.lo, (byte) cData);
                    break;
        
                case 0xF9:  // "ADC B",    0xF9, AM_EXTENDED_6809,   3,  5,  15,  0, 15, 15, 15, 15, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    _dReg.lo = AddWithCarryRegister (_dReg.lo, (byte) cData);
                    break;
        
                case 0xFA:  // "ORA B",    0xFA, AM_EXTENDED_6809,   3,  5,   0,  0, 15, 15, 14,  0, 0,    // MC6809
                    cData = LoadMemoryByte (m_Operand);
                    _dReg.lo = OrRegister (_dReg.lo, (byte) cData);
                    break;
        
                case 0xFB:  // "ADD B",    0xFB, AM_EXTENDED_6809,   3,  5,  15,  0, 15, 15, 15, 15, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    _dReg.lo = AddRegister (_dReg.lo, (byte) cData);
                    break;

                case 0xFC:  // "LDD  ",    0xFC, AM_EXTENDED_6809,   3,  6,   0,  0, 16, 16, 14,  0, 0,    // MC6809 X
                    m_D = LoadMemoryWord(m_Operand);
                    SetCCR(m_D);
                    break;

                case 0xFD:  // "STD  ",    0xFD, AM_EXTENDED_6809,   3,  6,   0,  0, 16, 16, 14,  0, 0,    // MC6809
                    StoreMemoryWord (m_D, m_Operand);
                    SetCCR(m_D);
                    break;

                case 0xFE:  // "LDU  ",    0xFE, AM_EXTENDED_6809,   3,  6,   0,  0, 16, 16, 14,  0, 0,    // MC6809
                    OffsetRegisters[(int)OffsetRegisterIndex.m_U] = LoadMemoryWord (m_Operand);
                    SetCCR (OffsetRegisters[(int)OffsetRegisterIndex.m_U]);
                    break;

                case 0xFF:  // "STU  ",    0xFF, AM_EXTENDED_6809,   3,  6,   0,  0, 16, 16, 14,  0, 0,    // MC6809
                    StoreMemoryWord (OffsetRegisters[(int)OffsetRegisterIndex.m_U], m_Operand);
                    SetCCR (OffsetRegisters[(int)OffsetRegisterIndex.m_U]);
                    break;

            }
        }

        void Execute6809Indexed ()
        {
            ushort sData;
            byte  cData;
            byte  cReg;
            ushort sBefore;
            //ushort sResult;
            bool nOldCarry;

            switch (m_OpCode)
            {
                case 0x30:  // "LEA X",    0x30, AM_INDEXED_6809,    2,  4,   0,  0,  0, 16,  0,  0, 0,    // MC6809 X
                    OffsetRegisters[(int)OffsetRegisterIndex.m_X] = m_Operand;
                    SetCCR(OffsetRegisters[(int)OffsetRegisterIndex.m_X]);
                    break;

                case 0x31:  // "LEA Y",    0x31, AM_INDEXED_6809,    2,  4,   0,  0,  0, 16,  0,  0, 0,    // MC6809 X
                    OffsetRegisters[(int)OffsetRegisterIndex.m_Y] = m_Operand;
                    SetCCR (OffsetRegisters[(int)OffsetRegisterIndex.m_Y]);
                    break;

                case 0x32:  // "LEA S",    0x32, AM_INDEXED_6809,    2,  4,   0,  0,  0,  0,  0,  0, 0,    // MC6809 X
                    OffsetRegisters[(int)OffsetRegisterIndex.m_S] = m_Operand;
                    break;
        
                case 0x33:  // "LEA U",    0x33, AM_INDEXED_6809,    2,  4,   0,  0,  0,  0,  0,  0, 0,    // MC6809 X
                    OffsetRegisters[(int)OffsetRegisterIndex.m_U] = m_Operand;
                    break;

                case 0x60:  // "NEG  ",    0x60, AM_INDEXED_6809,    2,  7,   0,  0, 15, 15,  1,  2, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    sBefore = cData;
                    cData = (byte) (0x00 - cData);
                    StoreMemoryByte (cData, m_Operand);
                    SetCCR (cData, sBefore);
                    break;

                case 0x63:  // "COM  ",    0x63, AM_INDEXED_6809,    2,  7,   0,  0, 15, 15, 14, 13, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    cData = (byte) (0xFF - cData);
                    StoreMemoryByte (cData, m_Operand);
                    SetCCR (cData);
                    break;

                case 0x64:  // "LSR  ",    0x64, AM_INDEXED_6809,    2,  7,   0,  0, 14, 15,  0, 15, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    m_CF = (byte)(cData & 0x01);
                    cData = (byte)((cData >> 1) & 0x7F);
                    StoreMemoryByte (cData, m_Operand);
                    SetCCR (cData);
                    break;

                case 0x66:  // "ROR  ",    0x66, AM_INDEXED_6809,    2,  7,   0,  0, 15, 15,  0, 15, 0,    // MC6809
                    nOldCarry = (m_CCR & CCR_CARRY) == 0 ? false : true;
                    //nOldCarry = m_CCR & CCR_CARRY;
                    cData = LoadMemoryByte (m_Operand);
                    m_CF = (byte)(cData & 0x01);
                    cData = (byte)(cData >> 1);
                    if (nOldCarry)
                        cData = (byte)(cData | 0x80);
                    else
                        cData = (byte)(cData & 0x7F);
                    StoreMemoryByte (cData, m_Operand);
                    SetCCR (cData);
                    break;

                case 0x67:  // "ASR  ",    0x67, AM_INDEXED_6809,    2,  7,   0,  0, 15, 15,  0, 15, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    cReg = cData;
                    m_CF = (byte)(cData & 0x01);
                    cData = (byte)(cData >> 1);
                    cData = (byte)(cData | (cReg & 0x80));
                    StoreMemoryByte (cData, m_Operand);
                    SetCCR (cData);
                    break;

                case 0x68:  // "ASL  ",    0x68, AM_INDEXED_6809,    2,  7,   0,  0, 15, 15,  6, 15, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    cReg = cData;
                    if ((cData & 0x80) == 0x80)
                        m_CF = 1;
                    else
                        m_CF = 0;
                    cData  = (byte)(cData << 1);
                    StoreMemoryByte (cData, m_Operand);
                    SetCCR (cData, cReg);
                    break;

                case 0x69:  // "ROL  ",    0x69, AM_INDEXED_6809,    2,  7,   0,  0, 15, 15,  6, 15, 0,    // MC6809
                    cData = LoadMemoryByte (m_Operand);
                    cReg = cData;
                    if ((cData & 0x80) == 0x80)
                        m_CF = 1;
                    else
                        m_CF = 0;
                    cData  = (byte)(cData << 1);
                    cData |= (byte)(m_CCR & CCR_CARRY);
                    StoreMemoryByte (cData, m_Operand);
                    SetCCR (cData, cReg);
                    break;

                case 0x6A:  // "DEC  ",    0x6A, AM_INDEXED_6809,    2,  7,   0,  0, 15, 15,  4,  0, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    cReg = cData;
                    cData -= 1;
                    StoreMemoryByte (cData, m_Operand);
                    SetCCR (cData, cReg);
                    break;

                case 0x6C:  // "INC  ",    0x6C, AM_INDEXED_6809,    2,  7,   0,  0, 15, 15,  5,  0, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    cReg = cData;
                    cData += 1;
                    StoreMemoryByte (cData, m_Operand);
                    SetCCR (cData, cReg);
                    break;

                case 0x6D:  // "TST  ",    0x6D, AM_INDEXED_6809,    2,  7,   0,  0, 15, 15, 14,  0, 0,    // MC6809
                    cData = LoadMemoryByte (m_Operand);
                    SetCCR (cData);
                    break;

                case 0x6E:  // "JMP  ",    0x6E, AM_INDEXED_6809,    2,  4,   0,  0,  0,  0,  0,  0, 0,    // MC6809
                    IP = m_Operand;
                    break;

                case 0x6F:  // "CLR  ",    0x6F, AM_INDEXED_6809,    2,  7,   0,  0, 14, 13, 14, 14, 0,    // MC6809 X
                    cData = 0x00;
                    StoreMemoryByte (cData, m_Operand);
                    SetCCR (cData);
                    break;

                case 0xA0:  // "SUB A",    0xA0, AM_INDEXED_6809,    2,  4,   0,  0, 15, 15, 15, 15, 0,    // MC6809
                    cData = LoadMemoryByte (m_Operand);
                    _dReg.hi = SubtractRegister (_dReg.hi, (byte) cData);
                    break;
        
                case 0xA1:  // "CMP A",    0xA1, AM_INDEXED_6809,    2,  4,   0,  0, 15, 15, 15, 15, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    CompareRegister (_dReg.hi, (byte) cData);
                    break;
        
                case 0xA2:  // "SBC A",    0xA2, AM_INDEXED_6809,    2,  4,   0,  0, 15, 15, 15, 15, 0,    // MC6809
                    cData = LoadMemoryByte (m_Operand);
                    _dReg.hi = SubtractWithCarryRegister (_dReg.hi, (byte) cData);
                    break;
        
                case 0xA3:  // "SUB D",    0xA3, AM_INDEXED_6809,    2,  6,   0,  0, 16, 16, 16, 16, 0,    // MC6809
                    sData  = LoadMemoryWord (m_Operand);
                    m_D = SubtractRegister(m_D, sData);
                    break;

                case 0xA4:  // "AND A",    0xA4, AM_INDEXED_6809,    2,  4,   0,  0, 15, 15, 14,  0, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    _dReg.hi = AndRegister (_dReg.hi, (byte) cData);
                    break;
        
                case 0xA5:  // "BIT A",    0xA5, AM_INDEXED_6809,    2,  4,   0,  0, 15, 15, 14,  0, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    BitRegister (_dReg.hi, (byte) cData);
                    break;
        
                case 0xA6:  // "LDA A",    0xA6, AM_INDEXED_6809,    2,  4,   0,  0, 15, 15, 14,  0, 0,    // MC6809 X
                    _dReg.hi = LoadMemoryByte (m_Operand);
                    SetCCR (_dReg.hi);
                    break;
        
                case 0xA7:  // "STA A",    0xA7, AM_INDEXED_6809,    2,  4,   0,  0, 15, 15, 14,  0, 0,    // MC6809
                    StoreMemoryByte (_dReg.hi, m_Operand);
                    SetCCR (_dReg.hi);
                    break;
        
                case 0xA8:  // "EOR A",    0xA8, AM_INDEXED_6809,    2,  4,   0,  0, 15, 15, 14,  0, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    _dReg.hi = ExclusiveOrRegister (_dReg.hi, (byte) cData);
                    break;
        
                case 0xA9:  // "ADC A",    0xA9, AM_INDEXED_6809,    2,  4,  15,  0, 15, 15, 15, 15, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    _dReg.hi = AddWithCarryRegister (_dReg.hi, (byte) cData);
                    break;
        
                case 0xAA:  // "ORA A",    0xAA, AM_INDEXED_6809,    2,  4,   0,  0, 15, 15, 14,  0, 0,    // MC6809
                    cData = LoadMemoryByte (m_Operand);
                    _dReg.hi = OrRegister (_dReg.hi, (byte) cData);
                    break;
        
                case 0xAB:  // "ADD A",    0xAB, AM_INDEXED_6809,    2,  4,  15,  0, 15, 15, 15, 15, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    _dReg.hi = AddRegister (_dReg.hi, (byte) cData);
                    break;

                case 0xAC:  // "CMP X",    0xAC, AM_INDEXED_6809,    2,  6,   0,  0, 16, 16, 16, 16, 0,    // MC6809
                    sData = LoadMemoryWord (m_Operand);
                    CompareRegister(OffsetRegisters[(int)OffsetRegisterIndex.m_X], sData);
                    //sBefore = offsetRegisters[(int)OffsetRegisterIndex.m_X];
                    //sResult = offsetRegisters[(int)OffsetRegisterIndex.m_X] - sData;
                    //if (offsetRegisters[(int)OffsetRegisterIndex.m_X] < sData)
                    //    m_CF = 1;
                    //else
                    //    m_CF = 0;
                    //SetCCR (sResult, sBefore);
                    break;

                case 0xAD:  // "JSR  ",    0xAD, AM_INDEXED_6809,    2,  7,   0,  0,  0,  0,  0,  0, 0,    // MC6809

                    // First save current IP

                    WriteMemByte (--OffsetRegisters[(int)OffsetRegisterIndex.m_S], (byte)(IP % 256));
                    WriteMemByte (--OffsetRegisters[(int)OffsetRegisterIndex.m_S], (byte)(IP / 256));
                    //CallStack.Add(IP);

                    // Then load new IP
            
                    IP = m_Operand;
                    break;

                case 0xAE:  // "LDX  ",    0xAE, AM_INDEXED_6809,    2,  5,   0,  0, 16, 16, 14,  0, 0,    // MC6809
                    OffsetRegisters[(int)OffsetRegisterIndex.m_X] = LoadMemoryWord(m_Operand);
                    SetCCR(OffsetRegisters[(int)OffsetRegisterIndex.m_X]);
                    break;

                case 0xAF:  // "STX  ",    0xAF, AM_INDEXED_6809,    2,  5,   0,  0, 16, 16, 14,  0, 0,    // MC6809
                    StoreMemoryWord(OffsetRegisters[(int)OffsetRegisterIndex.m_X], m_Operand);
                    SetCCR(OffsetRegisters[(int)OffsetRegisterIndex.m_X]);
                    break;

                case 0xE0:  // "SUB B",    0xE0, AM_INDEXED_6809,    2,  4,   0,  0, 15, 15, 15, 15, 0,    // MC6809
                    cData = LoadMemoryByte (m_Operand);
                    _dReg.lo = SubtractRegister (_dReg.lo, (byte) cData);
                    break;
        
                case 0xE1:  // "CMP B",    0xE1, AM_INDEXED_6809,    2,  4,   0,  0, 15, 15, 15, 15, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    CompareRegister (_dReg.lo, (byte) cData);
                    break;
        
                case 0xE2:  // "SBC B",    0xE2, AM_INDEXED_6809,    2,  4,   0,  0, 15, 15, 15, 15, 0,    // MC6809
                    cData = LoadMemoryByte (m_Operand);
                    _dReg.lo = SubtractWithCarryRegister (_dReg.lo, (byte) cData);
                    break;

                case 0xE3:  // "ADD D",    0xE3, AM_INDEXED_6809,    2,  6,   0,  0, 16, 16, 16, 16, 0,    // MC6809 X        
                    sData = LoadMemoryWord (m_Operand);
                    m_D = AddRegister(m_D, sData);
                    break;

                case 0xE4:  // "AND B",    0xE4, AM_INDEXED_6809,    2,  4,   0,  0, 15, 15, 14,  0, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    _dReg.lo = AndRegister (_dReg.lo, (byte) cData);
                    break;
        
                case 0xE5:  // "BIT B",    0xE5, AM_INDEXED_6809,    2,  4,   0,  0, 15, 15, 14,  0, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    BitRegister (_dReg.lo, (byte) cData);
                    break;
        
                case 0xE6:  // "LDA B",    0xE6, AM_INDEXED_6809,    2,  4,   0,  0, 15, 15, 14,  0, 0,    // MC6809 X
                    _dReg.lo = LoadMemoryByte (m_Operand);
                    SetCCR (_dReg.lo);
                    break;
        
                case 0xE7:  // "STA B",    0xE7, AM_INDEXED_6809,    2,  4,   0,  0, 15, 15, 14,  0, 0,    // MC6809
                    StoreMemoryByte (_dReg.lo, m_Operand);
                    SetCCR (_dReg.lo);
                    break;
        
                case 0xE8:  // "EOR B",    0xE8, AM_INDEXED_6809,    2,  4,   0,  0, 15, 15, 14,  0, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    _dReg.lo = ExclusiveOrRegister (_dReg.lo, (byte) cData);
                    break;
        
                case 0xE9:  // "ADC B",    0xE9, AM_INDEXED_6809,    2,  4,  15,  0, 15, 15, 15, 15, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    _dReg.lo = AddWithCarryRegister (_dReg.lo, (byte) cData);
                    break;
        
                case 0xEA:  // "ORA B",    0xEA, AM_INDEXED_6809,    2,  4,   0,  0, 15, 15, 14,  0, 0,    // MC6809
                    cData = LoadMemoryByte (m_Operand);
                    _dReg.lo = OrRegister (_dReg.lo, (byte) cData);
                    break;
        
                case 0xEB:  // "ADD B",    0xEB, AM_INDEXED_6809,    2,  4,  15,  0, 15, 15, 15, 15, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    _dReg.lo = AddRegister (_dReg.lo, (byte) cData);
                    break;

                case 0xEC:  // "LDD  ",    0xEC, AM_INDEXED_6809,    2,  5,   0,  0, 16, 16, 14,  0, 0,    // MC6809 X        
                    m_D = LoadMemoryWord(m_Operand);
                    SetCCR(m_D);
                    break;
        
                case 0xED:  // "STD  ",    0xED, AM_INDEXED_6809,    2,  5,   0,  0,  0,  0,  0,  0, 0,    // MC6809
                    StoreMemoryWord(m_D, m_Operand);
                    SetCCR(m_D);
                    break;

                case 0xEE:  // "LDU  ",    0xEE, AM_INDEXED_6809,    2,  5,   0,  0, 16, 16, 14,  0, 0,    // MC6809
                    OffsetRegisters[(int)OffsetRegisterIndex.m_U] = LoadMemoryWord (m_Operand);
                    SetCCR (OffsetRegisters[(int)OffsetRegisterIndex.m_U]);
                    break;

                case 0xEF:  // "STU  ",    0xEF, AM_INDEXED_6809,    2,  5,   0,  0, 16, 16, 14,  0, 0,    // MC6809
                    StoreMemoryWord (OffsetRegisters[(int)OffsetRegisterIndex.m_U], m_Operand);
                    SetCCR (OffsetRegisters[(int)OffsetRegisterIndex.m_U]);
                    break;
            }
        }

        void Execute6809Direct ()
        {
            byte  cData;
            byte  cReg;
            ushort sData;
            ushort sBefore;
            //ushort sResult;
            bool nOldCarry;

            m_Operand += (ushort)(m_DP * 256);

            switch (m_OpCode)
            {
                case 0x00:  // "NEG  ",    0x00, AM_DIRECT_6809,     2,  6,   0,  0, 15, 15,  1,  2, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    sBefore = cData;
                    cData = (byte) (0x00 - cData);
                    StoreMemoryByte (cData, m_Operand);
                    SetCCR (cData, sBefore);
                    break;

                case 0x03:  // "COM  ",    0x03, AM_DIRECT_6809,     2,  6,   0,  0, 15, 15, 14, 13, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    cData = (byte) (0xFF - cData);
                    StoreMemoryByte (cData, m_Operand);
                    SetCCR (cData);
                    break;

                case 0x04:  // "LSR  ",    0x04, AM_DIRECT_6809,     2,  6,   0,  0, 14, 15,  0, 15, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    m_CF = (byte)(cData & 0x01);
                    cData = (byte)((cData >> 1) & 0x7F);
                    StoreMemoryByte (cData, m_Operand);
                    SetCCR (cData);
                    break;

                case 0x06:  // "ROR  ",    0x06, AM_DIRECT_6809,     2,  6,   0,  0, 15, 15,  0, 15, 0,    // MC6809
                    nOldCarry = (m_CCR & CCR_CARRY) == 0 ? false : true;
                    //nOldCarry = m_CCR & CCR_CARRY;
                    cData = LoadMemoryByte (m_Operand);
                    m_CF = (byte)(cData & 0x01);
                    cData = (byte)(cData >> 1);
                    if (nOldCarry)
                        cData = (byte)(cData | 0x80);
                    else
                        cData = (byte)(cData & 0x7F);
                    StoreMemoryByte (cData, m_Operand);
                    SetCCR (cData);
                    break;

                case 0x07:  // "ASR  ",    0x07, AM_DIRECT_6809,     2,  6,   0,  0, 15, 15,  0, 15, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    cReg = cData;
                    m_CF = (byte)(cData & 0x01);
                    cData = (byte)(cData >> 1);
                    cData = (byte)(cData | (cReg & 0x80));
                    StoreMemoryByte (cData, m_Operand);
                    SetCCR (cData);
                    break;

                case 0x08:  // "ASL  ",    0x08, AM_DIRECT_6809,     2,  6,   0,  0, 15, 15,  6, 15, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    cReg = cData;
                    if ((cData & 0x80) == 0x80)
                        m_CF = 1;
                    else
                        m_CF = 0;
                    cData  = (byte)(cData << 1);
                    StoreMemoryByte (cData, m_Operand);
                    SetCCR (cData, cReg);
                    break;

                case 0x09:  // "ROL  ",    0x09, AM_DIRECT_6809,     2,  6,   0,  0, 15, 15,  6, 15, 0,    // MC6809
                    cData = LoadMemoryByte (m_Operand);
                    cReg = cData;
                    if ((cData & 0x80) == 0x80)
                        m_CF = 1;
                    else
                        m_CF = 0;
                    cData  = (byte)(cData << 1);
                    cData |= (byte)(m_CCR & CCR_CARRY);
                    StoreMemoryByte (cData, m_Operand);
                    SetCCR (cData, cReg);
                    break;

                case 0x0A:  // "DEC  ",    0x0A, AM_DIRECT_6809,     2,  6,   0,  0, 15, 15,  4,  0, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    cReg = cData;
                    cData -= 1;
                    StoreMemoryByte (cData, m_Operand);
                    SetCCR (cData, cReg);
                    break;

                case 0x0C:  // "INC  ",    0x0C, AM_DIRECT_6809,     2,  6,   0,  0, 15, 15,  5,  0, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    cReg = cData;
                    cData += 1;
                    StoreMemoryByte (cData, m_Operand);
                    SetCCR (cData, cReg);
                    break;

                case 0x0D:  // "TST  ",    0x0D, AM_DIRECT_6809,     2,  6,   0,  0, 15, 15, 14,  0, 0,    // MC6809
                    cData = LoadMemoryByte (m_Operand);
                    SetCCR (cData);
                    break;

                case 0x0E:  // "JMP  ",    0x0E, AM_DIRECT_6809,     2,  3,   0,  0,  0,  0,  0,  0, 0,    // MC6809
                    IP = m_Operand;
                    break;

                case 0x0F:  // "CLR  ",    0x0F, AM_DIRECT_6809,     2,  6,   0,  0, 14, 13, 14, 14, 0,    // MC6809 X
                    cData = 0x00;
                    StoreMemoryByte (cData, m_Operand);
                    SetCCR (cData);
                    break;

                case 0x90:  // "SUB A",    0x90, AM_DIRECT_6809,     2,  4,   0,  0, 15, 15, 15, 15, 0,    // MC6809
                    cData = LoadMemoryByte (m_Operand);
                    _dReg.hi = SubtractRegister (_dReg.hi, (byte) cData);
                    break;
        
                case 0x91:  // "CMP A",    0x91, AM_DIRECT_6809,     2,  4,   0,  0, 15, 15, 15, 15, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    CompareRegister (_dReg.hi, (byte) cData);
                    break;
        
                case 0x92:  // "SBC A",    0x92, AM_DIRECT_6809,     2,  4,   0,  0, 15, 15, 15, 15, 0,    // MC6809
                    cData = LoadMemoryByte (m_Operand);
                    _dReg.hi = SubtractWithCarryRegister (_dReg.hi, (byte) cData);
                    break;

                case 0x93:  // "SUB D",    0x93, AM_DIRECT_6809,     2,  6,   0,  0, 16, 16, 16, 16, 0,    // MC6809
                    sData  = LoadMemoryWord (m_Operand);
                    m_D = SubtractRegister(m_D, sData);
                    break;

                case 0x94:  // "AND A",    0x94, AM_DIRECT_6809,     2,  4,   0,  0, 15, 15, 14,  0, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    _dReg.hi = AndRegister (_dReg.hi, (byte) cData);
                    break;
        
                case 0x95:  // "BIT A",    0x95, AM_DIRECT_6809,     2,  4,   0,  0, 15, 15, 14,  0, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    BitRegister (_dReg.hi, (byte) cData);
                    break;
        
                case 0x96:  // "LDA A",    0x96, AM_DIRECT_6809,     2,  4,   0,  0, 15, 15, 14,  0, 0,    // MC6809 X
                    _dReg.hi = LoadMemoryByte (m_Operand);
                    SetCCR (_dReg.hi);
                    break;
        
                case 0x97:  // "STA A",    0x97, AM_DIRECT_6809,     2,  4,   0,  0, 15, 15, 14,  0, 0,    // MC6809
                    StoreMemoryByte (_dReg.hi, m_Operand);
                    SetCCR (_dReg.hi);
                    break;
        
                case 0x98:  // "EOR A",    0x98, AM_DIRECT_6809,     2,  4,   0,  0, 15, 15, 14,  0, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    _dReg.hi = ExclusiveOrRegister (_dReg.hi, (byte) cData);
                    break;
        
                case 0x99:  // "ADC A",    0x99, AM_DIRECT_6809,     2,  4,  15,  0, 15, 15, 15, 15, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    _dReg.hi = AddWithCarryRegister (_dReg.hi, (byte) cData);
                    break;
        
                case 0x9A:  // "ORA A",    0x9A, AM_DIRECT_6809,     2,  4,   0,  0, 15, 15, 14,  0, 0,    // MC6809
                    cData = LoadMemoryByte (m_Operand);
                    _dReg.hi = OrRegister (_dReg.hi, (byte) cData);
                    break;
        
                case 0x9B:  // "ADD A",    0x9B, AM_DIRECT_6809,     2,  4,  15,  0, 15, 15, 15, 15, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    _dReg.hi = AddRegister (_dReg.hi, (byte) cData);
                    break;

                case 0x9C:  // "CMP X",    0x9C, AM_DIRECT_6809,     2,  6,   0,  0, 16, 16, 16, 16, 0,    // MC6809
                    sData = LoadMemoryWord (m_Operand);
                    CompareRegister(OffsetRegisters[(int)OffsetRegisterIndex.m_X], sData);
                    //sBefore = offsetRegisters[(int)OffsetRegisterIndex.m_X];
                    //sResult = offsetRegisters[(int)OffsetRegisterIndex.m_X] - sData;
                    //if (offsetRegisters[(int)OffsetRegisterIndex.m_X] < sData)
                    //    m_CF = 1;
                    //else
                    //    m_CF = 0;
                    //SetCCR (sResult, sBefore);
                    break;
        
                case 0x9D:  // "JSR  ",    0x9D, AM_DIRECT_6809,     2,  7,   0,  0,  0,  0,  0,  0, 0,    // MC6809

                    // First save current IP

                    WriteMemByte (--OffsetRegisters[(int)OffsetRegisterIndex.m_S], (byte)(IP % 256));
                    WriteMemByte (--OffsetRegisters[(int)OffsetRegisterIndex.m_S], (byte)(IP / 256));
                    //CallStack.Add(IP);

                    // Then load new IP
            
                    IP = m_Operand;
                    break;

                case 0x9E:  // "LDX  ",    0x9E, AM_DIRECT_6809,     2,  5,   0,  0, 16, 16, 14,  0, 0,    // MC6809
                    OffsetRegisters[(int)OffsetRegisterIndex.m_X] = LoadMemoryWord(m_Operand);
                    SetCCR(OffsetRegisters[(int)OffsetRegisterIndex.m_X]);
                    break;

                case 0x9F:  // "STX  ",    0x9F, AM_DIRECT_6809,     2,  5,   0,  0, 16, 16, 14,  0, 0,    // MC6809
                    StoreMemoryWord(OffsetRegisters[(int)OffsetRegisterIndex.m_X], m_Operand);
                    SetCCR(OffsetRegisters[(int)OffsetRegisterIndex.m_X]);
                    break;

                case 0xD0:  // "SUB B",    0xD0, AM_DIRECT_6809,     2,  4,   0,  0, 15, 15, 15, 15, 0,    // MC6809
                    cData = LoadMemoryByte (m_Operand);
                    _dReg.lo = SubtractRegister (_dReg.lo, (byte) cData);
                    break;
        
                case 0xD1:  // "CMP B",    0xD1, AM_DIRECT_6809,     2,  4,   0,  0, 15, 15, 15, 15, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    CompareRegister (_dReg.lo, (byte) cData);
                    break;
        
                case 0xD2:  // "SBC B",    0xD2, AM_DIRECT_6809,     2,  4,   0,  0, 15, 15, 15, 15, 0,    // MC6809
                    cData = LoadMemoryByte (m_Operand);
                    _dReg.lo = SubtractWithCarryRegister (_dReg.lo, (byte) cData);
                    break;

                case 0xD3:  // "ADD D",    0xD3, AM_DIRECT_6809,     2,  6,   0,  0, 16, 16, 16, 16, 0,    // MC6809 X
                    sData = LoadMemoryWord (m_Operand);
                    m_D = AddRegister(m_D, sData);
                    break;

                case 0xD4:  // "AND B",    0xD4, AM_DIRECT_6809,     2,  4,   0,  0, 15, 15, 14,  0, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    _dReg.lo = AndRegister (_dReg.lo, (byte) cData);
                    break;
        
                case 0xD5:  // "BIT B",    0xD5, AM_DIRECT_6809,     2,  4,   0,  0, 15, 15, 14,  0, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    BitRegister (_dReg.lo, (byte) cData);
                    break;
        
                case 0xD6:  // "LDA B",    0xD6, AM_DIRECT_6809,     2,  4,   0,  0, 15, 15, 14,  0, 0,    // MC6809 X
                    _dReg.lo = LoadMemoryByte (m_Operand);
                    SetCCR (_dReg.lo);
                    break;
        
                case 0xD7:  // "STA B",    0xD7, AM_DIRECT_6809,     2,  4,   0,  0, 15, 15, 14,  0, 0,    // MC6809
                    StoreMemoryByte (_dReg.lo, m_Operand);
                    SetCCR (_dReg.lo);
                    break;
        
                case 0xD8:  // "EOR B",    0xD8, AM_DIRECT_6809,     2,  4,   0,  0, 15, 15, 14,  0, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    _dReg.lo = ExclusiveOrRegister (_dReg.lo, (byte) cData);
                    break;
        
                case 0xD9:  // "ADC B",    0xD9, AM_DIRECT_6809,     2,  4,  15,  0, 15, 15, 15, 15, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    _dReg.lo = AddWithCarryRegister (_dReg.lo, (byte) cData);
                    break;
        
                case 0xDA:  // "ORA B",    0xDA, AM_DIRECT_6809,     2,  4,   0,  0, 15, 15, 14,  0, 0,    // MC6809
                    cData = LoadMemoryByte (m_Operand);
                    _dReg.lo = OrRegister (_dReg.lo, (byte) cData);
                    break;
        
                case 0xDB:  // "ADD B",    0xDB, AM_DIRECT_6809,     2,  4,  15,  0, 15, 15, 15, 15, 0,    // MC6809 X
                    cData = LoadMemoryByte (m_Operand);
                    _dReg.lo = AddRegister (_dReg.lo, (byte) cData);
                    break;
        
                case 0xDC:  // "LDD  ",    0xDC, AM_DIRECT_6809,     2,  5,   0,  0, 16, 16, 14,  0, 0,    // MC6809 X
                    m_D = LoadMemoryWord(m_Operand);
                    SetCCR(m_D);
                    break;
        
                case 0xDD:  // "STD  ",    0xDD, AM_DIRECT_6809,     2,  5,   0,  0,  0,  0,  0,  0, 0,    // MC6809
                    StoreMemoryWord(m_D, m_Operand);
                    SetCCR(m_D);
                    break;

                case 0xDE:  // "LDU  ",    0xDE, AM_DIRECT_6809,     2,  5,   0,  0, 16, 16, 14,  0, 0,    // MC6809
                    OffsetRegisters[(int)OffsetRegisterIndex.m_U] = LoadMemoryWord (m_Operand);
                    SetCCR (OffsetRegisters[(int)OffsetRegisterIndex.m_U]);
                    break;

                case 0xDF:  // "STU  ",    0xDF, AM_DIRECT_6809,     2,  5,   0,  0, 16, 16, 14,  0, 0,    // MC6809
                    StoreMemoryWord (OffsetRegisters[(int)OffsetRegisterIndex.m_U], m_Operand);
                    SetCCR (OffsetRegisters[(int)OffsetRegisterIndex.m_U]);
                    break;
            }
        }

        void Execute6809Immediate8 ()
        {
            switch (m_OpCode)
            {
                case 0x1A: // "ORCC ",    0x1A, AM_IMM8_6809,       2,  3,   0,  0,  0,  0,  0,  0, 0,    // MC6809
                    m_CCR |= (byte)m_Operand;
                    break;

                case 0x1C: // "ANDCC",    0x1C, AM_IMM8_6809,       2,  3,   0,  0,  0,  0,  0,  0, 0,    // MC6809 X
                    m_CCR &= (byte)m_Operand;
                    break;

                case 0x3C:  // "CWAI ",    0x3C, AM_IMM8_6809,      2, 20,   0,  0,  0,  0,  0,  0, 0,    // MC6809
                    PushOntoStack (0xFF, ref OffsetRegisters[(int)OffsetRegisterIndex.m_S], (int)SYSTEM_STACK);
                    InWait = true;
                    m_CCR &= (byte)m_Operand;
                    if (!IrqAsserted)
                        Program._cpuThread.Suspend();
                    break;

                case 0x80: // "SUB A",    0x80, AM_IMM8_6809,       2,  2,   0,  0, 15, 15, 15, 15, 0,    // MC6809
                    _dReg.hi = SubtractRegister (_dReg.hi, (byte) m_Operand);
                    break;
                case 0x81: // "CMP A",    0x81, AM_IMM8_6809,       2,  2,   0,  0, 15, 15, 15, 15, 0,    // MC6809 X
                    CompareRegister (_dReg.hi, (byte) m_Operand);
                    break;
                case 0x82: // "SBC A",    0x82, AM_IMM8_6809,       2,  2,   0,  0, 15, 15, 15, 15, 0,    // MC6809
                    _dReg.hi = SubtractWithCarryRegister (_dReg.hi, (byte) m_Operand);
                    break;
                case 0x84: // "AND A",    0x84, AM_IMM8_6809,       2,  2,   0,  0, 15, 15, 14,  0, 0,    // MC6809 X
                    _dReg.hi = AndRegister (_dReg.hi, (byte) m_Operand);
                    break;
                case 0x85: // "BIT A",    0x85, AM_IMM8_6809,       2,  2,   0,  0, 15, 15, 14,  0, 0,    // MC6809 X
                    BitRegister (_dReg.hi, (byte) m_Operand);
                    break;
                case 0x86: // "LDA A",    0x86, AM_IMM8_6809,       2,  2,   0,  0, 15, 15, 14,  0, 0,    // MC6809 X
                    _dReg.hi = (byte) m_Operand;
                    SetCCR (_dReg.hi);
                    break;
                case 0x88: // "EOR A",    0x88, AM_IMM8_6809,       2,  2,   0,  0, 15, 15, 14,  0, 0,    // MC6809 X
                    _dReg.hi = ExclusiveOrRegister (_dReg.hi, (byte) m_Operand);
                    break;
                case 0x89: // "ADC A",    0x89, AM_IMM8_6809,       2,  2,  15,  0, 15, 15, 15, 15, 0,    // MC6809 X
                    _dReg.hi = AddWithCarryRegister (_dReg.hi, (byte) m_Operand);
                    break;
                case 0x8A: // "ORA A",    0x8A, AM_IMM8_6809,       2,  2,   0,  0, 15, 15, 14,  0, 0,    // MC6809
                    _dReg.hi = OrRegister (_dReg.hi, (byte) m_Operand);
                    break;
                case 0x8B: // "ADD A",    0x8B, AM_IMM8_6809,       2,  2,  15,  0, 15, 15, 15, 15, 0,    // MC6809 X
                    _dReg.hi = AddRegister (_dReg.hi, (byte) m_Operand);
                    break;
                case 0xC0: // "SUB B",    0xC0, AM_IMM8_6809,       2,  2,   0,  0, 15, 15, 15, 15, 0,    // MC6809
                    _dReg.lo = SubtractRegister (_dReg.lo, (byte) m_Operand);
                    break;
                case 0xC1: // "CMP B",    0xC1, AM_IMM8_6809,       2,  2,   0,  0, 15, 15, 15, 15, 0,    // MC6809 X
                    CompareRegister (_dReg.lo, (byte) m_Operand);
                    break;
                case 0xC2: // "SBC B",    0xC2, AM_IMM8_6809,       2,  2,   0,  0, 15, 15, 15, 15, 0,    // MC6809
                    _dReg.lo = SubtractWithCarryRegister (_dReg.lo, (byte) m_Operand);
                    break;
                case 0xC4: // "AND B",    0xC4, AM_IMM8_6809,       2,  2,   0,  0, 15, 15, 14,  0, 0,    // MC6809 X
                    _dReg.lo = AndRegister (_dReg.lo, (byte) m_Operand);
                    break;
                case 0xC5: // "BIT B",    0xC5, AM_IMM8_6809,       2,  2,   0,  0, 15, 15, 14,  0, 0,    // MC6809 X
                    BitRegister (_dReg.lo, (byte) m_Operand);
                    break;
                case 0xC6: // "LDA B",    0xC6, AM_IMM8_6809,       2,  2,   0,  0, 15, 15, 14,  0, 0,    // MC6809 X
                    _dReg.lo = (byte) m_Operand;
                    SetCCR (_dReg.lo);
                    break;
                case 0xC8: // "EOR B",    0xC8, AM_IMM8_6809,       2,  2,   0,  0, 15, 15, 14,  0, 0,    // MC6809 X
                    _dReg.lo = ExclusiveOrRegister (_dReg.lo, (byte) m_Operand);
                    break;
                case 0xC9: // "ADC B",    0xC9, AM_IMM8_6809,       2,  2,  15,  0, 15, 15, 15, 15, 0,    // MC6809 X
                    _dReg.lo = AddWithCarryRegister (_dReg.lo, (byte) m_Operand);
                    break;
                case 0xCA: // "ORA B",    0xCA, AM_IMM8_6809,       2,  2,   0,  0, 15, 15, 14,  0, 0,    // MC6809
                    _dReg.lo = OrRegister (_dReg.lo, (byte) m_Operand);
                    break;
                case 0xCB: // "ADD B",    0xCB, AM_IMM8_6809,       2,  2,  15,  0, 15, 15, 15, 15, 0,    // MC6809 X
                    _dReg.lo = AddRegister (_dReg.lo, (byte) m_Operand);
                    break;
            }
        }

        void Execute6809Immediate16 ()
        {
            switch (m_OpCode)
            {
                case 0x83:  // "SUB D",    0x83, AM_IMM16_6809,      3,  4,   0,  0, 16, 16, 16, 16, 0,    // MC6809
                    m_D = SubtractRegister(m_D, m_Operand);
                    break;
    
                case 0x8C:  // "CMP X",    0x8C, AM_IMM16_6809,      3,  3,   0,  0, 16, 16, 16, 16, 0,    // MC6809 X
                    CompareRegister(OffsetRegisters[(int)OffsetRegisterIndex.m_X], m_Operand);
                    break;

                case 0x8E:  // "LDX  ",    0x8E, AM_IMM16_6809,      3,  3,   0,  0, 16, 16, 14,  0, 0,    // MC6809
                    OffsetRegisters[(int)OffsetRegisterIndex.m_X] = m_Operand;
                    SetCCR(OffsetRegisters[(int)OffsetRegisterIndex.m_X]);
                    break;

                case 0xC3:  // "ADD D",    0xC3, AM_IMM16_6809,      3,  4,   0,  0, 16, 16, 16, 16, 0,    // MC6809 X
                    m_D = AddRegister(m_D, m_Operand);
                    break;
    
                case 0xCC:  // "LDD  ",    0xCC, AM_IMM16_6809,      3,  3,   0,  0, 16, 16, 14,  0, 0,    // MC6809 X
                    m_D = m_Operand;
                    SetCCR(m_D);
                    break;
    
                case 0xCE:  // "LDU  ",    0xCE, AM_IMM16_6809,      3,  3,   0,  0, 16, 16, 14,  0, 0,    // MC6809
                    OffsetRegisters[(int)OffsetRegisterIndex.m_U] = m_Operand;
                    SetCCR (OffsetRegisters[(int)OffsetRegisterIndex.m_U]);
                    break;
            }
        }

        void Execute6809Relative ()
        {
            bool nDoBranch   = false;
            bool nLongBranch = false;

            int m_nNegative = (m_CCR & CCR_NEGATIVE)   == 0 ? 0 : 1;
            int m_nZero     = (m_CCR & CCR_ZERO)       == 0 ? 0 : 1;;
            int m_nOverflow = (m_CCR & CCR_OVERFLOW)   == 0 ? 0 : 1;;
            int m_nCarry    = (m_CCR & CCR_CARRY)      == 0 ? 0 : 1;;

            switch (m_OpCode)
            {

                case 0x16:  // "LBRA ",    0x16, AM_RELATIVE_6809,   3,  5,   0,  0,  0,  0,  0,  0, 0,    // MC6809
                    nDoBranch   = true;
                    nLongBranch = true;
                    break;

                case 0x17:  // "LBSR ",    0x17, AM_RELATIVE_6809,   3,  9,   0,  0,  0,  0,  0,  0, 0,    // MC6809

                    WriteMemByte (--OffsetRegisters[(int)OffsetRegisterIndex.m_S], (byte)(IP % 256));
                    WriteMemByte (--OffsetRegisters[(int)OffsetRegisterIndex.m_S], (byte)(IP / 256));
                    //CallStack.Add(IP);

                    nDoBranch   = true;
                    nLongBranch = true;
                    break;

                case 0x20:  // "BRA  ",    0x20, AM_RELATIVE_6809,   2,  3,   0,  0,  0,  0,  0,  0, 0,    // MC6809
                    nDoBranch = true;
                    break;

                case 0x21:  // "BRN  ",    0x21, AM_RELATIVE_6809,   2,  3,   0,  0,  0,  0,  0,  0, 0,    // MC6809
                    break;

                case 0x22:  // "BHI  ",    0x22, AM_RELATIVE_6809,   2,  3,   0,  0,  0,  0,  0,  0, 0,    // MC6809
                    if ((m_nCarry | m_nZero) == 0)
                        nDoBranch = true;
                    break;

                case 0x23:  // "BLS  ",    0x23, AM_RELATIVE_6809,   2,  3,   0,  0,  0,  0,  0,  0, 0,    // MC6809
                    if ((m_nCarry | m_nZero) == 1)
                        nDoBranch = true;
                    break;

                case 0x24:  // "BCC  ",    0x24, AM_RELATIVE_6809,   2,  3,   0,  0,  0,  0,  0,  0, 0,    // MC6809
                    if (m_nCarry == 0)
                        nDoBranch = true;
                    break;

                case 0x25:  // "BCS  ",    0x25, AM_RELATIVE_6809,   2,  3,   0,  0,  0,  0,  0,  0, 0,    // MC6809
                    if (m_nCarry == 1)
                        nDoBranch = true;
                    break;

                case 0x26:  // "BNE  ",    0x26, AM_RELATIVE_6809,   2,  3,   0,  0,  0,  0,  0,  0, 0,    // MC6809
                    if (m_nZero == 0)
                        nDoBranch = true;
                    break;

                case 0x27:  // "BEQ  ",    0x27, AM_RELATIVE_6809,   2,  3,   0,  0,  0,  0,  0,  0, 0,    // MC6809
                    if (m_nZero == 1)
                        nDoBranch = true;
                    break;

                case 0x28:  // "BVC  ",    0x28, AM_RELATIVE_6809,   2,  3,   0,  0,  0,  0,  0,  0, 0,    // MC6809
                    if (m_nOverflow == 0)
                        nDoBranch = true;
                    break;

                case 0x29:  // "BVS  ",    0x29, AM_RELATIVE_6809,   2,  3,   0,  0,  0,  0,  0,  0, 0,    // MC6809
                    if (m_nOverflow == 1)
                        nDoBranch = true;
                    break;

                case 0x2A:  // "BPL  ",    0x2A, AM_RELATIVE_6809,   2,  3,   0,  0,  0,  0,  0,  0, 0,    // MC6809
                    if (m_nNegative == 0)
                        nDoBranch = true;
                    break;

                case 0x2B:  // "BMI  ",    0x2B, AM_RELATIVE_6809,   2,  3,   0,  0,  0,  0,  0,  0, 0,    // MC6809
                    if (m_nNegative == 1)
                        nDoBranch = true;
                    break;

                case 0x2C:  // "BGE  ",    0x2C, AM_RELATIVE_6809,   2,  3,   0,  0,  0,  0,  0,  0, 0,    // MC6809
                    if ((m_nNegative ^ m_nOverflow) == 0)
                        nDoBranch = true;
                    break;

                case 0x2D:  // "BLT  ",    0x2D, AM_RELATIVE_6809,   2,  3,   0,  0,  0,  0,  0,  0, 0,    // MC6809
                    if ((m_nNegative ^ m_nOverflow) == 1)
                        nDoBranch = true;
                    break;

                case 0x2E:  // "BGT  ",    0x2E, AM_RELATIVE_6809,   2,  3,   0,  0,  0,  0,  0,  0, 0,    // MC6809
                    if ((m_nZero | (m_nNegative ^ m_nOverflow)) == 0)
                        nDoBranch = true;
                    break;

                case 0x2F:  // "BLE  ",    0x2F, AM_RELATIVE_6809,   2,  3,   0,  0,  0,  0,  0,  0, 0,    // MC6809
                    if ((m_nZero | (m_nNegative ^ m_nOverflow)) == 1)
                        nDoBranch = true;
                    break;

                case 0x8D:  // "BSR  ",    0x8D, AM_RELATIVE_6809,   2,  6,   0,  0,  0,  0,  0,  0, 0,    // MC6809

                    WriteMemByte (--OffsetRegisters[(int)OffsetRegisterIndex.m_S], (byte)(IP % 256));
                    WriteMemByte (--OffsetRegisters[(int)OffsetRegisterIndex.m_S], (byte)(IP / 256));
                    //CallStack.Add(IP);

                    nDoBranch = true;
                    break;

            }

            if (nDoBranch == true)
            {
                if (!nLongBranch)
                {
                    if ((m_Operand & 0x80) != 0x00)
                        m_Operand |= 0xFF00;
                }
                IP = (ushort)(IP + m_Operand);
            }
        }

        void ExecutePage2Inherent ()
        {
            // "SWI2 ",    0x3F, AM_INHERENT_PAGE2,  2, 20,   0,  0,  0,  0,  0,  0, 0,    // PAGE 2

            m_CCR |= CCR_ENTIREFLAG;
            PushOntoStack (0xFF, ref OffsetRegisters[(int)OffsetRegisterIndex.m_S], (int)SYSTEM_STACK);
            IP = LoadMemoryWord (0xFFF4);
        }

        void ExecutePage3Inherent ()
        {
            // "SWI3 ",    0x3F, AM_INHERENT_PAGE3,  2, 20,   0,  0,  0,  0,  0,  0, 0,    // PAGE 3

            m_CCR |= CCR_ENTIREFLAG;
            PushOntoStack (0xFF, ref OffsetRegisters[(int)OffsetRegisterIndex.m_S], (int)SYSTEM_STACK);
            IP = LoadMemoryWord (0xFFF2);
        }

        void ExecutePage2Direct ()
        {
            ushort sData;

            m_Operand += (ushort)(m_DP * 256);

            switch (m_OpCode)
            {
                case 0x93:  // "CMP D",    0x93, AM_DIRECT_PAGE2,    3,  7,   0,  0, 16, 16, 16, 16, 0,    // PAGE 2 X
                    sData = LoadMemoryWord (m_Operand);
                    CompareRegister(m_D, sData);
                    break;

                case 0x9C:  // "CMP Y",    0x9C, AM_DIRECT_PAGE2,    3,  7,   0,  0, 16, 16, 16, 16, 0,    // PAGE 2 X
                    sData   = LoadMemoryWord (m_Operand);
                    CompareRegister (OffsetRegisters[(int)OffsetRegisterIndex.m_Y], sData);
                    break;
        
                case 0x9E:  // "LDY  ",    0x9E, AM_DIRECT_PAGE2,    3,  6,   0,  0, 16, 16, 14,  0, 0,    // PAGE 2 X
                    OffsetRegisters[(int)OffsetRegisterIndex.m_Y] = LoadMemoryWord (m_Operand);
                    SetCCR (OffsetRegisters[(int)OffsetRegisterIndex.m_Y]);
                    break;

                case 0x9F:  // "STY  ",    0x9F, AM_DIRECT_PAGE2,    3,  6,   0,  0, 16, 16, 14,  0, 0,    // PAGE 2 X
                    StoreMemoryWord (OffsetRegisters[(int)OffsetRegisterIndex.m_Y], m_Operand);
                    SetCCR (OffsetRegisters[(int)OffsetRegisterIndex.m_Y]);
                    break;

                case 0xDE:  // "LDS  ",    0xDE, AM_DIRECT_PAGE2,    3,  6,   0,  0, 16, 16, 14,  0, 0,    // PAGE 2 
                    OffsetRegisters[(int)OffsetRegisterIndex.m_S] = LoadMemoryWord (m_Operand);
                    SetCCR (OffsetRegisters[(int)OffsetRegisterIndex.m_S]);
                    break;

                case 0xDF:  // "STS  ",    0xDF, AM_DIRECT_PAGE2,    3,  6,   0,  0, 16, 16, 14,  0, 0,    // PAGE 2 
                    StoreMemoryWord (OffsetRegisters[(int)OffsetRegisterIndex.m_S], m_Operand);
                    SetCCR (OffsetRegisters[(int)OffsetRegisterIndex.m_S]);
                    break;
            }
        }

        void ExecutePage3Direct ()
        {
            ushort sData;

            m_Operand += (ushort)(m_DP * 256);

            switch (m_OpCode)
            {
                case 0x93:  // "CMP U",    0x93, AM_DIRECT_PAGE3,    3,  7,   0,  0, 16, 16, 16, 16, 0,    // PAGE 3 X
                    sData   = LoadMemoryWord (m_Operand);
                    CompareRegister (OffsetRegisters[(int)OffsetRegisterIndex.m_U], sData);
                    break;
        
                case 0x9C:  // "CMP S",    0x9C, AM_DIRECT_PAGE3,    3,  7,   0,  0, 16, 16, 16, 16, 0,    // PAGE 3 X
                    sData   = LoadMemoryWord (m_Operand);
                    CompareRegister (OffsetRegisters[(int)OffsetRegisterIndex.m_S], sData);
                    break;
            }
        }

        void ExecutePage2Relative ()
        {
            bool nDoBranch   = false;
            bool nLongBranch = false;

            int m_nNegative = (m_CCR & CCR_NEGATIVE)   == 0 ? 0 : 1;
            int m_nZero     = (m_CCR & CCR_ZERO)       == 0 ? 0 : 1;;
            int m_nOverflow = (m_CCR & CCR_OVERFLOW)   == 0 ? 0 : 1;;
            int m_nCarry    = (m_CCR & CCR_CARRY)      == 0 ? 0 : 1;;


            switch (m_OpCode)
            {
                case 0x21:  // "LBRN ",    0x21, AM_RELATIVE_PAGE2,  4,  5,   0,  0,  0,  0,  0,  0, 0,    // PAGE 2
                    break;

                case 0x22:  // "LBHI ",    0x22, AM_RELATIVE_PAGE2,  4,  5,   0,  0,  0,  0,  0,  0, 0,    // PAGE 2
                    if ((m_nCarry | m_nZero) == 0)
                        nDoBranch = true;
                    break;

                case 0x23:  // "LBLS ",    0x23, AM_RELATIVE_PAGE2,  4,  5,   0,  0,  0,  0,  0,  0, 0,    // PAGE 2
                    if ((m_nCarry | m_nZero) == 1)
                        nDoBranch = true;
                    break;

                case 0x24:  // "LBCC ",    0x24, AM_RELATIVE_PAGE2,  4,  5,   0,  0,  0,  0,  0,  0, 0,    // PAGE 2
                    if (m_nCarry == 0)
                        nDoBranch = true;
                    break;

                case 0x25:  // "LBCS ",    0x25, AM_RELATIVE_PAGE2,  4,  5,   0,  0,  0,  0,  0,  0, 0,    // PAGE 2
                    if (m_nCarry == 1)
                        nDoBranch = true;
                    break;

                case 0x26:  // "LBNE ",    0x26, AM_RELATIVE_PAGE2,  4,  5,   0,  0,  0,  0,  0,  0, 0,    // PAGE 2
                    if (m_nZero == 0)
                        nDoBranch = true;
                    break;

                case 0x27:  // "LBEQ ",    0x27, AM_RELATIVE_PAGE2,  4,  5,   0,  0,  0,  0,  0,  0, 0,    // PAGE 2
                    if (m_nZero == 1)
                        nDoBranch = true;
                    break;

                case 0x28:  // "LBVC ",    0x28, AM_RELATIVE_PAGE2,  4,  5,   0,  0,  0,  0,  0,  0, 0,    // PAGE 2
                    if (m_nOverflow == 0)
                        nDoBranch = true;
                    break;

                case 0x29:  // "LBVS ",    0x29, AM_RELATIVE_PAGE2,  4,  5,   0,  0,  0,  0,  0,  0, 0,    // PAGE 2
                    if (m_nOverflow == 1)
                        nDoBranch = true;
                    break;

                case 0x2A:  // "LBPL ",    0x2A, AM_RELATIVE_PAGE2,  4,  5,   0,  0,  0,  0,  0,  0, 0,    // PAGE 2
                    if (m_nNegative == 0)
                        nDoBranch = true;
                    break;

                case 0x2B:  // "LBMI ",    0x2B, AM_RELATIVE_PAGE2,  4,  5,   0,  0,  0,  0,  0,  0, 0,    // PAGE 2
                    if (m_nNegative == 1)
                        nDoBranch = true;
                    break;

                case 0x2C:  // "LBGE ",    0x2C, AM_RELATIVE_PAGE2,  4,  5,   0,  0,  0,  0,  0,  0, 0,    // PAGE 2
                    if ((m_nNegative ^ m_nOverflow) == 0)
                        nDoBranch = true;
                    break;

                case 0x2D:  // "LBLT ",    0x2D, AM_RELATIVE_PAGE2,  4,  5,   0,  0,  0,  0,  0,  0, 0,    // PAGE 2
                    if ((m_nNegative ^ m_nOverflow) == 1)
                        nDoBranch = true;
                    break;

                case 0x2E:  // "LBGT ",    0x2E, AM_RELATIVE_PAGE2,  4,  5,   0,  0,  0,  0,  0,  0, 0,    // PAGE 2
                    if ((m_nZero | (m_nNegative ^ m_nOverflow)) == 0)
                        nDoBranch = true;
                    break;

                case 0x2F:  // "LBLE ",    0x2F, AM_RELATIVE_PAGE2,  4,  5,   0,  0,  0,  0,  0,  0, 0,    // PAGE 2
                    if ((m_nZero | (m_nNegative ^ m_nOverflow)) == 1)
                        nDoBranch = true;
                    break;
            }

            if (nDoBranch == true)
            {
                IP = (ushort)(IP + m_Operand);
            }
        }

        void ExecutePage3Relative ()
        {
            // THERE AREN'T ANY OF THESE
        }

        void ExecutePage2Extended ()
        {
            ushort sData;

            switch (m_OpCode)
            {
                case 0xB3:  // "CMP D",    0xB3, AM_EXTENDED_PAGE2,  4,  8,   0,  0, 16, 16, 16, 16, 0,    // PAGE 2 X
                    sData = LoadMemoryWord (m_Operand);
                    CompareRegister(m_D, sData);
                    break;

                case 0xBC:  // "CMP Y",    0xBC, AM_EXTENDED_PAGE2,  4,  8,   0,  0, 16, 16, 16, 16, 0,    // PAGE 2 X
                    sData = LoadMemoryWord (m_Operand);
                    CompareRegister (OffsetRegisters[(int)OffsetRegisterIndex.m_Y], sData);
                    break;

                case 0xBE:  // "LDY  ",    0xBE, AM_EXTENDED_PAGE2,  4,  7,   0,  0, 16, 16, 14,  0, 0,    // PAGE 2 X
                    OffsetRegisters[(int)OffsetRegisterIndex.m_Y] = LoadMemoryWord (m_Operand);
                    SetCCR (OffsetRegisters[(int)OffsetRegisterIndex.m_Y]);
                    break;

                case 0xBF:  // "STY  ",    0xBF, AM_EXTENDED_PAGE2,  4,  7,   0,  0,  0,  0,  0,  0, 0,    // PAGE 2 
                    StoreMemoryWord (OffsetRegisters[(int)OffsetRegisterIndex.m_Y], m_Operand);
                    SetCCR (OffsetRegisters[(int)OffsetRegisterIndex.m_Y]);
                    break;

                case 0xFE:  // "LDS  ",    0xFE, AM_EXTENDED_PAGE2,  4,  7,   0,  0, 16, 16, 14,  0, 0,    // PAGE 2 X
                    OffsetRegisters[(int)OffsetRegisterIndex.m_S] = LoadMemoryWord (m_Operand);
                    SetCCR (OffsetRegisters[(int)OffsetRegisterIndex.m_S]);
                    break;

                case 0xFF:  // "STS  ",    0xFF, AM_EXTENDED_PAGE2,  4,  7,   0,  0, 16, 16, 14,  0, 0,    // PAGE 2 X
                    StoreMemoryWord (OffsetRegisters[(int)OffsetRegisterIndex.m_S], m_Operand);
                    SetCCR (OffsetRegisters[(int)OffsetRegisterIndex.m_S]);
                    break;
            }
        }

        void ExecutePage3Extended ()
        {
            ushort sData;

            switch (m_OpCode)
            {
                case 0xB3:  // "CMP U",    0xB3, AM_EXTENDED_PAGE3,  4,  8,   0,  0, 16, 16, 16, 16, 0,    // PAGE 3 X
                    sData = LoadMemoryWord (m_Operand);
                    CompareRegister (OffsetRegisters[(int)OffsetRegisterIndex.m_U], sData);
                    break;

                case 0xBC:  // "CMP S",    0xBC, AM_EXTENDED_PAGE3,  4,  8,   0,  0, 16, 16, 16, 16, 0,    // PAGE 3 X
                    sData = LoadMemoryWord (m_Operand);
                    CompareRegister (OffsetRegisters[(int)OffsetRegisterIndex.m_S], sData);
                    break;
            }
        }

        void ExecutePage2Immediate16 ()
        {
            switch (m_OpCode)
            {
                case 0x83:  // "CMP D",    0x83, AM_IMM16_PAGE2,     4,  5,   0,  0, 16, 16, 16, 16, 0,    // PAGE 2 X
                    CompareRegister(m_D, m_Operand);
                    break;

                case 0x8C:  // "CMP Y",    0x8C, AM_IMM16_PAGE2,     4,  5,   0,  0, 16, 16, 16, 16, 0,    // PAGE 2 X
                    CompareRegister (OffsetRegisters[(int)OffsetRegisterIndex.m_Y], m_Operand);
                    break;

                case 0x8E:  // "LDY  ",    0x8E, AM_IMM16_PAGE2,     4,  4,   0,  0, 16, 16, 14,  0, 0,    // PAGE 2 X
                    OffsetRegisters[(int)OffsetRegisterIndex.m_Y] = m_Operand;
                    SetCCR (OffsetRegisters[(int)OffsetRegisterIndex.m_Y]);
                    break;

                case 0xCE:  // "LDS  ",    0xCE, AM_IMM16_PAGE2,     4,  4,   0,  0, 16, 16, 14,  0, 0,    // PAGE 2 
                    OffsetRegisters[(int)OffsetRegisterIndex.m_S] = m_Operand;
                    SetCCR (OffsetRegisters[(int)OffsetRegisterIndex.m_S]);
                    break;
            }
        }

        void ExecutePage3Immediate16 ()
        {
            switch (m_OpCode)
            {
                case 0x83:  // "CMP U",    0x83, AM_IMM16_PAGE3,     4,  5,   0,  0, 16, 16, 16, 16, 0,    // PAGE 3 X
                    CompareRegister (OffsetRegisters[(int)OffsetRegisterIndex.m_U], m_Operand);
                    break;

                case 0x8C:  // "CMP S",    0x8C, AM_IMM16_PAGE3,     4,  5,   0,  0, 16, 16, 16, 16, 0,    // PAGE 3 X
                    CompareRegister (OffsetRegisters[(int)OffsetRegisterIndex.m_S], m_Operand);
                    break;
            }
        }

        void ExecutePage2Immediate8 ()
        {
            // THERE AREN'T ANY OF THESE
        }

        void ExecutePage3Immediate8 ()
        {
            // THERE AREN'T ANY OF THESE
        }

        void ExecutePage2Indexed ()
        {
            ushort sData;

            switch (m_OpCode)
            {
                case 0xA3:  // "CMP D",    0xA3, AM_INDEXED_PAGE2,   3,  7,   0,  0, 16, 16, 16, 16, 0,    // PAGE 2 X
                    sData = LoadMemoryWord (m_Operand);
                    CompareRegister(m_D, sData);
                    break;

                case 0xAC:  // "CMP Y",    0xAC, AM_INDEXED_PAGE2,   3,  7,   0,  0, 16, 16, 16, 16, 0,    // PAGE 2 X
                    sData = LoadMemoryWord (m_Operand);
                    CompareRegister (OffsetRegisters[(int)OffsetRegisterIndex.m_Y], sData);
                    break;

                case 0xAE:  // "LDY  ",    0xAE, AM_INDEXED_PAGE2,   3,  6,   0,  0, 16, 16, 14,  0, 0,    // PAGE 2 X
                    OffsetRegisters[(int)OffsetRegisterIndex.m_Y] = LoadMemoryWord (m_Operand);
                    SetCCR (OffsetRegisters[(int)OffsetRegisterIndex.m_Y]);
                    break;

                case 0xAF:  // "STY  ",    0xAF, AM_INDEXED_PAGE2,   3,  6,   0,  0, 16, 16, 14,  0, 0,    // PAGE 2 X
                    StoreMemoryWord (OffsetRegisters[(int)OffsetRegisterIndex.m_Y], m_Operand);
                    SetCCR (OffsetRegisters[(int)OffsetRegisterIndex.m_Y]);
                    break;

                case 0xEE:  // "LDS  ",    0xEE, AM_INDEXED_PAGE2,   3,  6,   0,  0, 16, 16, 14,  0, 0,    // PAGE 2 
                    OffsetRegisters[(int)OffsetRegisterIndex.m_S] = LoadMemoryWord (m_Operand);
                    SetCCR (OffsetRegisters[(int)OffsetRegisterIndex.m_S]);
                    break;

                case 0xEF:  // "STS  ",    0xEF, AM_INDEXED_PAGE2,   3,  6,   0,  0, 16, 16, 14,  0, 0,    // PAGE 2 
                    StoreMemoryWord (OffsetRegisters[(int)OffsetRegisterIndex.m_S], m_Operand);
                    SetCCR (OffsetRegisters[(int)OffsetRegisterIndex.m_S]);
                    break;
            }
        }

        void ExecutePage3Indexed ()
        {
            ushort sData;

            switch (m_OpCode)
            {
                case 0xA3:  // "CMP U",    0xA3, AM_INDEXED_PAGE3,   3,  7,   0,  0, 16, 16, 16, 16, 0,    // PAGE 3 X
                    sData = LoadMemoryWord (m_Operand);
                    CompareRegister (OffsetRegisters[(int)OffsetRegisterIndex.m_U], sData);
                    break;

                case 0xAC:  // "CMP S",    0xAC, AM_INDEXED_PAGE3,   3,  7,   0,  0, 16, 16, 16, 16, 0,    // PAGE 3 X
                    sData = LoadMemoryWord (m_Operand);
                    CompareRegister (OffsetRegisters[(int)OffsetRegisterIndex.m_S], sData);
                    break;
            }
        }

        #endregion

        //
        // To use the DumpTraceBuffers Proc the following member variables must be set up:
        //
        //      m_OpCode            what's the opcode
        //      m_nTable            which opcode table to use
        //      m_Operand           the effective address
        //      m_Indirect          is this an indirect indexing mode
        //      m_OffsetType        the type of indexing offset
        //      m_cPostRegister     which register is specified in the post byte
        //      m_Predecrement      does instruction indicate a pre decrement
        //      m_Postincrement     does instruction indicate a post increment
        //
        //  and the following parameters passed in:
        //
        //      mode                Addressing mode
        //      sCurrentIP          Address of instruction in m_Memory[sLogicalAddress]
        //
        //void DumpTraceBuffers ()
        //{
        //    long i, k;
        //    char szMessage[128];
        //    char szRegisterContents[128];

        //    char ff[3];

        //    // The last instruction executed will be at m_nTraceIndex - 1 and since we
        //    // want the listing to be the last TRACE_SIZE entries starting with the
        //    // oldest, we start outputting at m_nTraceIndex and wrap back around the
        //    // buffers to m_nTraceIndex - 1

        //    ff[0] = 0x0c;
        //    ff[1] = 0x00;
        //    ff[2] = 0x00;

        //    if (Program.m_nTraceEnabled)
        //    {
        //        time_t lTime;
        //        struct tm today;
        //        char szDate[16];
        //        char szTime[16];

        //        time (&lTime);
        //        localtime_s (&today, &lTime);

        //        sprintf_s (szDate, sizeof (szDate), "%04d%02d%02d", today.tm_year, today.tm_mon + 1, today.tm_mday);
        //        sprintf_s (szTime, sizeof (szTime), "%02d%02d%02d", today.tm_hour, today.tm_min,     today.tm_sec);

        //        CString strTraceFile = Program.m_TraceFile;
        //        strTraceFile.Replace ("<date>", szDate);
        //        strTraceFile.Replace ("<time>", szTime);

        //        FILE *fp = null;
        //        fopen_s (&fp, strTraceFile, "w");

        //        if (m_nTraceFull)
        //            k = TRACE_SIZE;
        //        else
        //        {
        //            k = m_nTraceIndex;
        //            m_nTraceIndex = 0;
        //        }

        //        fprintf (fp, "ADDR OP   PB      MNEUM OPER         IDAT ODAT SP   A  B  X    Y    U    DP CR Interrupts\n");
        //        fprintf (fp, "---- ---- -- ---- ----- ----         --   --   ---- -- -- ---- ---- ---- -- -- ----------\n");

        //        for (i = 0; i < k; i++)
        //        {
        //            ulong  cDatSave     = m_MemoryDAT[m_IP >> 12];

        //            ushort sIPSave      = m_IP        ;
        //            byte  cOpCodeSave  = m_OpCode    ;
        //            int            nTableSave   = m_nTable    ;
        //            ushort sSSave       = offsetRegisters[(int)OffsetRegisterIndex.m_S]         ;
        //            byte  cm_D.hiSave   = m_D.hi       ;
        //            byte  cm_D.loSave   = m_D.lo       ;
        //            ushort sXSave       = offsetRegisters[(int)OffsetRegisterIndex.m_X]         ;
        //            ushort sYSave       = offsetRegisters[(int)OffsetRegisterIndex.m_Y]         ;
        //            ushort sUSave       = offsetRegisters[(int)OffsetRegisterIndex.m_U]         ;
        //            byte  cDPSave      = m_DP        ;
        //            byte  cCCRSave     = m_CCR       ;
        //            ushort sOperandSave = m_Operand   ;

        //            m_DP  = clsTraceBuffer[m_nTraceIndex].cRegisterDP;
        //            m_MemoryDAT[m_IP >> 12] = clsTraceBuffer[m_nTraceIndex].cRegisterDAT;

        //            //if ((i % 50) == 0)
        //            //{
        //            //    if (i != 0)
        //            //    {
        //            //        fprintf (fp, ff);
        //            //        fprintf (fp, "\n");
        //            //    }
        //            //    else
        //            //        fprintf (fp, "\n");

        //            //    fprintf (fp, "ADDR OP   PB      MNEUM OPER         IDAT ODAT SP   A  B  X    Y    U    DP CR Interrupts\n");
        //            //    fprintf (fp, "---- ---- -- ---- ----- ----         --   --   ---- -- -- ---- ---- ---- -- -- ----------\n");
        //            //}

        //            m_nTable = 0;     // assume all start as MC6809 table instructions

        //            // Get the opcode

        //            m_IP        = clsTraceBuffer[m_nTraceIndex].sExecutionTrace;
        //            m_OpCode    = clsTraceBuffer[m_nTraceIndex].cOpcode;
        //            m_nTable    = clsTraceBuffer[m_nTraceIndex].cTable;
        //            offsetRegisters[(int)OffsetRegisterIndex.m_S]         = clsTraceBuffer[m_nTraceIndex].sStackTrace;
        //            m_D.hi       = clsTraceBuffer[m_nTraceIndex].cRegisterA;
        //            m_D.lo       = clsTraceBuffer[m_nTraceIndex].cRegisterB;
        //            offsetRegisters[(int)OffsetRegisterIndex.m_X]         = clsTraceBuffer[m_nTraceIndex].sRegisterX;
        //            offsetRegisters[(int)OffsetRegisterIndex.m_Y]         = clsTraceBuffer[m_nTraceIndex].sRegisterY;
        //            offsetRegisters[(int)OffsetRegisterIndex.m_U]         = clsTraceBuffer[m_nTraceIndex].sRegisterU;
        //            m_DP        = clsTraceBuffer[m_nTraceIndex].cRegisterDP;
        //            m_CCR       = clsTraceBuffer[m_nTraceIndex].cRegisterCCR;
        //            m_Operand   = clsTraceBuffer[m_nTraceIndex].sOperand;

        //            switch (opctbl[m_nTable,m_OpCode].attribute)
        //            {
        //                case AM_DIRECT_PAGE2:
        //                case AM_DIRECT_PAGE3:
        //                case AM_DIRECT_6809:
        //                case AM_RELATIVE_PAGE2:
        //                case AM_RELATIVE_PAGE3:
        //                case AM_RELATIVE_6809:
        //                case AM_EXTENDED_PAGE2:
        //                case AM_EXTENDED_PAGE3:
        //                case AM_EXTENDED_6809:
        //                case AM_IMM16_PAGE2:
        //                case AM_IMM16_PAGE3:
        //                case AM_IMM16_6809:
        //                case AM_IMM8_PAGE2:
        //                case AM_IMM8_PAGE3:
        //                case AM_IMM8_6809:
        //                case AM_INDEXED_PAGE2:
        //                case AM_INDEXED_PAGE3:
        //                case AM_INDEXED_6809:
        //                case AM_INHERENT_PAGE2:
        //                case AM_INHERENT_PAGE3:
        //                case AM_INHERENT_6809:
        //                    strcpy_s (szMessage, sizeof (szMessage), (char*) m_szDebugLine[m_nTraceIndex]);
        //                    break;

        //                default:
        //                    sprintf_s (szMessage, sizeof (szMessage), "%04X %02X   ", clsTraceBuffer[m_nTraceIndex].sExecutionTrace, clsTraceBuffer[m_nTraceIndex].cOpcode);
        //                    strcat_s (szMessage, sizeof (szMessage) - strlen (szMessage), "ILLEGAL INSTRUCTION");
        //                    break;
        //            }

        //            szMessage[37] = '\0';

        //            while (strlen (szMessage) < 37)
        //                strcat_s (szMessage, sizeof (szMessage) - strlen (szMessage), " ");

        //            CString strOperandDAT = "  ";

        //            switch (opctbl[m_nTable,m_OpCode].attribute)
        //            {
        //                case AM_DIRECT_PAGE2:
        //                    break;
        //                case AM_DIRECT_PAGE3:
        //                    break;
        //                case AM_DIRECT_6809:
        //                    break;
        //                case AM_RELATIVE_PAGE2:
        //                    break;
        //                case AM_RELATIVE_PAGE3:
        //                    break;
        //                case AM_RELATIVE_6809:
        //                    break;
        //                case AM_EXTENDED_PAGE2:
        //                    strOperandDAT.Format ("%02X", clsTraceBuffer[m_nTraceIndex].cRegisterOperandDAT);
        //                    break;
        //                case AM_EXTENDED_PAGE3:
        //                    strOperandDAT.Format ("%02X", clsTraceBuffer[m_nTraceIndex].cRegisterOperandDAT);
        //                    break;
        //                case AM_EXTENDED_6809:
        //                    strOperandDAT.Format ("%02X", clsTraceBuffer[m_nTraceIndex].cRegisterOperandDAT);
        //                    break;
        //                case AM_IMM16_PAGE2:
        //                    strOperandDAT.Format ("%02X", clsTraceBuffer[m_nTraceIndex].cRegisterOperandDAT);
        //                    break;
        //                case AM_IMM16_PAGE3:
        //                    strOperandDAT.Format ("%02X", clsTraceBuffer[m_nTraceIndex].cRegisterOperandDAT);
        //                    break;
        //                case AM_IMM16_6809:
        //                    strOperandDAT.Format ("%02X", clsTraceBuffer[m_nTraceIndex].cRegisterOperandDAT);
        //                    break;
        //                case AM_IMM8_PAGE2:
        //                    break;
        //                case AM_IMM8_PAGE3:
        //                    break;
        //                case AM_IMM8_6809:
        //                    break;
        //                case AM_INDEXED_PAGE2:
        //                    strOperandDAT.Format ("%02X", clsTraceBuffer[m_nTraceIndex].cRegisterOperandDAT);
        //                    break;
        //                case AM_INDEXED_PAGE3:
        //                    strOperandDAT.Format ("%02X", clsTraceBuffer[m_nTraceIndex].cRegisterOperandDAT);
        //                    break;
        //                case AM_INDEXED_6809:
        //                    strOperandDAT.Format ("%02X", clsTraceBuffer[m_nTraceIndex].cRegisterOperandDAT);
        //                    break;
        //                case AM_INHERENT_PAGE2:
        //                    break;
        //                case AM_INHERENT_PAGE3:
        //                    break;
        //                case AM_INHERENT_6809:
        //                    break;
        //            }

        //            sprintf_s (szRegisterContents, 
        //                        sizeof (szRegisterContents), 
        //                        "%02X   %s   %04X %02X %02X %04X %04X %04X %02X %02X %04X", 
        //                                clsTraceBuffer[m_nTraceIndex].cRegisterDAT,
        //                                strOperandDAT,
        //                                clsTraceBuffer[m_nTraceIndex].sStackTrace,
        //                                clsTraceBuffer[m_nTraceIndex].cRegisterA,
        //                                clsTraceBuffer[m_nTraceIndex].cRegisterB,
        //                                clsTraceBuffer[m_nTraceIndex].sRegisterX,
        //                                clsTraceBuffer[m_nTraceIndex].sRegisterY,
        //                                clsTraceBuffer[m_nTraceIndex].sRegisterU,
        //                                clsTraceBuffer[m_nTraceIndex].cRegisterDP,
        //                                clsTraceBuffer[m_nTraceIndex].cRegisterCCR,
        //                                clsTraceBuffer[m_nTraceIndex].sRegisterInterrupts
        //                    );

        //            strcat_s (szMessage, sizeof (szMessage) - strlen (szMessage), szRegisterContents);

        //            fprintf (fp, "%s\n", szMessage);
        //            m_nTraceIndex++;
        //            if (m_nTraceIndex == TRACE_SIZE)
        //                m_nTraceIndex = 0;

        //            m_IP        = sIPSave     ;
        //            m_OpCode    = cOpCodeSave ;
        //            m_nTable    = nTableSave  ;
        //            offsetRegisters[(int)OffsetRegisterIndex.m_S]         = sSSave      ;
        //            m_D.hi       = cm_D.hiSave  ;
        //            m_D.lo       = cm_D.loSave  ;
        //            offsetRegisters[(int)OffsetRegisterIndex.m_X]         = sXSave      ;
        //            offsetRegisters[(int)OffsetRegisterIndex.m_Y]         = sYSave      ;
        //            offsetRegisters[(int)OffsetRegisterIndex.m_U]         = sUSave      ;
        //            m_DP        = cDPSave     ;
        //            m_CCR       = cCCRSave    ;
        //            m_Operand   = sOperandSave;

        //            m_MemoryDAT[m_IP >> 12] = cDatSave;
        //        }
        //        fclose (fp);
        //    }
        //}

        public override void CoreDump ()
        {
            int nDatOffset;
            AddressingModes mode;
            ulong page;

            int i, j;

            DateTime ltime = DateTime.Now;
            string szDate = ltime.ToString("yyyyMMdd");
            string szTime = ltime.ToString("HHmmss");


            _statDumpFile = Program.GetConfigurationAttribute("Global/Statistics", "filename", string.Format("{0}stats_{{date}}_{{time}}.txt", Program.dataDir));
            _statDumpFile = _statDumpFile.Replace("{date}", szDate);
            _statDumpFile = _statDumpFile.Replace("{time}", szTime);

            string [] pszTableName = new string[3];
            pszTableName [0] = "MC6809";
            pszTableName [1] = "MC6809_PAGE_2";
            pszTableName [2] = "MC6809_PAGE_3";

            using (TextWriter fp = new StreamWriter(File.Open(_statDumpFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
            {
                if (fp != null)
                {
                    fp.Write("DAT =");
                    for (i = 0; i < 16; i++)
                    {
                        if (i > 0)
                            fp.Write(" ");

                        fp.Write(Memory.m_MemoryDAT[i].ToString("X5"));
                    }
                    fp.Write("\r\n\r\n");

                    fp.Write(string.Format("A = {0} B = {1} X = {2} CCR = {3}\r\nIP= {4} SP= {5}\r\n\r\n", 
                        _dReg.hi.ToString("X2"), 
                        _dReg.lo.ToString("X2"), 
                        OffsetRegisters[(int)OffsetRegisterIndex.m_X].ToString("X4"), 
                        m_CCR.ToString("X2"), 
                        IP.ToString("X4"), 
                        OffsetRegisters[(int)OffsetRegisterIndex.m_S].ToString("X4")));

                    // needs work - can not do this while running

                    ushort SReg = (ushort)(OffsetRegisters[(int)OffsetRegisterIndex.m_S] - (ushort)16);           //OffsetRegisters[(int)OffsetRegisterIndex.m_S] -= 16;
                    if (SReg > 16)
                    {
                        for (i = 0; i < 4; i++)
                        {
                            fp.Write(string.Format(" {0}", SReg.ToString("X4")));
                            for (j = 0; j < 16; j++)
                            {
                                nDatOffset = (SReg - 1) >> 12;
                                page = (Memory.m_MemoryDAT[nDatOffset] & Memory.DAT_MASK) >> 12;
                                fp.Write(string.Format(" {0}", Memory.MemorySpace[page + (ulong)(++SReg & Memory.ADDRESS_MASK)].ToString("X2")));
                            }
                            fp.Write("\r\n");
                        }
                    }

                    fp.Write("\r\n");

                    for (int table = 0; table < 3; table++)
                    {
                        //fprintf (fp, "\r\nTable %s\r\n", pszTableName[m_nTable]);

                        for (i = 0; i < 256; i++)
                        {
                            AddressingModes m = opctbl[table, i].attribute;
                            if (m != AddressingModes.AM_ILLEGAL)
                            {
                                long nCPUTime;

                                // fprintf (fp, "%s 0x%02X - %12ld\r\n", opctbl[table,i].mneumonic, (byte) i, opctbl[table,i].lCount);

                                //#ifdef __PROFILE_INSTR__
                                //                    if (opctbl[m_nTable,m_OpCode].lCount != 0)
                                //                        nCPUTime = opctbl[m_nTable,m_OpCode].lCPUTime / opctbl[m_nTable,m_OpCode].lCount;
                                //                    else
                                //#endif

                                nCPUTime = 0;

                                fp.Write("insert into test values ('{0}', '0x{1}', '0x{2}', {3}, {4})\r\n", 
                                        opctbl[table,i].mneumonic, 
                                        table.ToString("X2"),
                                        ((byte) i).ToString("X2"), 
                                        opctbl[table,i].lCount.ToString(),
                                        nCPUTime.ToString());
                            }
                        }
                    }
                }
            }

            _coreDumpFile = Program.GetConfigurationAttribute("Global/CoreDump", "filename", string.Format("{0}core_{{date}}_{{time}}.txt", Program.dataDir));
            _coreDumpFile = _coreDumpFile.Replace("{date}", szDate);
            _coreDumpFile = _coreDumpFile.Replace("{time}", szTime);

            using (BinaryWriter fp = new BinaryWriter(File.Open(_coreDumpFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
            {
                fp.Write(Memory.MemorySpace, 0, 65536 * 16);
            }
        }

        void ShowDebug (AddressingModes attribute, string lpszMessage, ushort sCurrentIP) 
        {
            List<string> pListDAT = new List<string>();

            string csMessage = lpszMessage + "\r\n";

            string szRegisterA  ;  // char szRegisterA[3];
            string szRegisterB  ;  // char szRegisterB[3];
            string szRegisterD  ;  // char szRegisterD[5];
            string szRegisterPC ;  // char szRegisterPC[5];
            string szRegisterSP ;  // char szRegisterSP[5];
            string szRegisterX  ;  // char szRegisterX[5];
            string szRegisterY  ;  // char szRegisterY[5];
            string szRegisterU  ;  // char szRegisterU[5];
            string szRegisterDP ;  // char szRegisterDP[3];
            string szRegisterCCR;  // char szRegisterCCR[3];
    
            string szPhysicalIP;
            string szPhysicalU;
            string szPhysicalS;
            string szPhysicalOperand;

            ushort x = OffsetRegisters[(int)OffsetRegisterIndex.m_X];
            ushort y = OffsetRegisters[(int)OffsetRegisterIndex.m_Y];
            ushort u = OffsetRegisters[(int)OffsetRegisterIndex.m_U];
            ushort s = OffsetRegisters[(int)OffsetRegisterIndex.m_S];

            if ((opctbl[m_nTable, m_OpCode].attribute & (AddressingModes)0x00FF) == AddressingModes.AM_INDEXED_6809)
            {
                switch (m_cPostRegister)
                {
                    case 0:
                        x -= m_Predecrement;
                        break;
                    case 1:
                        y -= m_Predecrement;
                        break;
                    case 2:
                        u -= m_Predecrement;
                        break;
                    default:
                        s -= m_Predecrement;
                        break;
                }
            }

            szRegisterA   = _dReg.hi     .ToString("X2");  //"%0.2X", 
            szRegisterB   = _dReg.lo     .ToString("X2");  //"%0.2X", 
            szRegisterPC  = sCurrentIP   .ToString("X4");  //"%0.4X", 
            szRegisterSP  = s            .ToString("X4");  //"%0.4X", 
            szRegisterX   = x            .ToString("X4");  //"%0.4X", 
            szRegisterY   = y            .ToString("X4");  //"%0.4X", 
            szRegisterU   = u            .ToString("X4");  //"%0.4X", 
            szRegisterD   = m_D          .ToString("X4");  //"%0.4X", 
            szRegisterDP  = m_DP         .ToString("X2");  //"%0.2X", 
            szRegisterCCR = m_CCR        .ToString("X2");  //"%0.2X", 

            bool bValidMemory;

            szPhysicalIP = Memory.LogicalToPhysicalAddress(sCurrentIP, out bValidMemory).ToString("X6");
            szPhysicalU  = Memory.LogicalToPhysicalAddress (u, out bValidMemory)        .ToString("X6");
            szPhysicalS  = Memory.LogicalToPhysicalAddress (s, out bValidMemory)        .ToString("X6");

            if (m_OperandSize == 2 && (attribute == AddressingModes.AM_DIRECT_6809 || attribute == AddressingModes.AM_EXTENDED_6809))
                szPhysicalOperand = Memory.LogicalToPhysicalAddress (m_Operand, out bValidMemory).ToString("X6");
            else
            {
                if (((ushort)attribute & 0x00FF) == (int)AddressingModes.AM_INDEXED_6809)
                {
                    szPhysicalOperand = Memory.LogicalToPhysicalAddress (m_Operand, out bValidMemory).ToString("X6");
                }
                else
                    szPhysicalOperand = "";
            }

            pListDAT.Clear();
            for (int i = 0; i < 16; i++)
            {
                string szDAT;

                szDAT = string.Format("{0} {1}", (0xFFF0 + i).ToString("X4"), (Memory.m_MemoryDAT[i] >> 12).ToString("X2"));
                pListDAT.Add(szDAT);
            }
        }

        // returns attr = AM_ILLEGAL if bogus post byte

        AddressingModes SetupPostByte(AddressingModes attribute)
        {
            AddressingModes attr;

            m_cPostByte = LoadMemoryByte (IP++);
            m_nPostCycles = 1;

            attr = attribute;

            m_OperandSize = 0;

            m_cPostRegister = (byte)((m_cPostByte & 0x60) >> 5);
            if ((m_cPostByte & 0x80) == 0x80)
            {
	            switch (m_cPostByte & 0x9f) 
                {
                        //  Post Increment

                    case 0x80:
                        m_Operand = OffsetRegisters[m_cPostRegister];   // GetRegisterReference ();
                        m_Postincrement = 1;
                        m_OffsetType  = OffsetType.OFFSET_POSTINCREMENT;
                        m_nPostCycles = 2;
                        break;

                        //  Post double increment

                    case 0x81:
                        m_Operand = OffsetRegisters[m_cPostRegister];   // GetRegisterReference ();
                        m_Postincrement = 2;
                        m_OffsetType = OffsetType.OFFSET_POSTINCREMENT;
                        m_nPostCycles   = 3;
                        break;
                    case 0x91:
                        m_sIndirectPtr = OffsetRegisters[m_cPostRegister];   // GetRegisterReference ();
                        m_Operand  = (ushort)(LoadMemoryByte (m_sIndirectPtr) * 256);
                        m_Operand += (ushort)(LoadMemoryByte ((ushort)(m_sIndirectPtr + 1)));
                        m_Postincrement = 2;
                        m_Indirect      = true;
                        m_OffsetType = OffsetType.OFFSET_POSTINCREMENT;
                        m_nPostCycles   = 6;
                        break;

                        //  Pre decrement

		            case 0x82:
			            OffsetRegisters[m_cPostRegister] -= 1;
                        m_Operand = OffsetRegisters[m_cPostRegister];   // GetRegisterReference ();
                        m_Predecrement = 1;
                        m_OffsetType = OffsetType.OFFSET_PREDECREMENT;
                        m_nPostCycles  = 2;
			            break;

                       // Pre double decrement

		            case 0x83:
                        OffsetRegisters[m_cPostRegister] -= 2;
                        m_Operand = OffsetRegisters[m_cPostRegister];   // GetRegisterReference ();
                        m_Predecrement = 2;
                        m_OffsetType = OffsetType.OFFSET_PREDECREMENT;
                        m_nPostCycles  = 3;
                        break;
                    case 0x93:
                        OffsetRegisters[m_cPostRegister] -= 2;
                        m_sIndirectPtr = OffsetRegisters[m_cPostRegister];   // GetRegisterReference ();
                        m_Operand  = (ushort)(LoadMemoryByte (m_sIndirectPtr) * 256);
                        m_Operand += (ushort)(LoadMemoryByte ((ushort)(m_sIndirectPtr + 1)));
                        m_Predecrement = 2;
                        m_Indirect     = true;
                        m_OffsetType = OffsetType.OFFSET_PREDECREMENT;
                        m_nPostCycles  = 6;
			            break;

                        // No Offset

                    case 0x84:
                        m_Operand = OffsetRegisters[m_cPostRegister];   // GetRegisterReference ();
                        m_OffsetType = OffsetType.OFFSET_NONE;
                        m_nPostCycles = 0;
                        break;
                    case 0x94:
                        m_sIndirectPtr = OffsetRegisters[m_cPostRegister];   // GetRegisterReference ();
                        m_Operand  = (ushort)(LoadMemoryByte (m_sIndirectPtr) * 256);
                        m_Operand += (ushort)(LoadMemoryByte ((ushort)(m_sIndirectPtr + 1)));
                        m_OffsetType = OffsetType.OFFSET_NONE;
                        m_Indirect    = true;
                        m_nPostCycles = 3;
                        break;

                        //  B - Register Offset

                    case 0x85:
                        m_Operand = _dReg.lo;
                        if ((m_Operand & 0x80) != 0x00)
                            m_Operand |= 0xFF00;
                        m_Operand += OffsetRegisters[m_cPostRegister];   // GetRegisterReference ();
                        m_nPostCycles = 1;
                        m_OffsetType = OffsetType.OFFSET_REGISTER_B;
                        break;
                    case 0x95:
                        m_sIndirectPtr = _dReg.lo;
                        if ((m_sIndirectPtr & 0x80) != 0x00)
                            m_sIndirectPtr |= 0xFF00;
                        m_sIndirectPtr += OffsetRegisters[m_cPostRegister];   // GetRegisterReference ();
                        m_Operand  = (ushort)(LoadMemoryByte (m_sIndirectPtr) * 256);
                        m_Operand += (ushort)(LoadMemoryByte ((ushort)(m_sIndirectPtr + 1)));
                        m_OffsetType = OffsetType.OFFSET_REGISTER_B;
                        m_Indirect    = true;
                        m_nPostCycles = 4;
                        break;

                        //  A - RegisterOffset

                    case 0x86:
                        m_Operand = _dReg.hi;
                         if ((m_Operand & 0x80) != 0x00)
                            m_Operand |= 0xFF00;
                        m_Operand += OffsetRegisters[m_cPostRegister];   // GetRegisterReference ();
                        m_OffsetType = OffsetType.OFFSET_REGISTER_A;
                        m_nPostCycles = 1;
                        break;
                    case 0x96:
                        m_sIndirectPtr = _dReg.hi;
                        if ((m_sIndirectPtr & 0x80) != 0x00)
                            m_sIndirectPtr |= 0xFF00;
                        m_sIndirectPtr += OffsetRegisters[m_cPostRegister];   // GetRegisterReference ();
                        m_Operand  = (ushort)(LoadMemoryByte (m_sIndirectPtr) * 256);
                        m_Operand += (ushort)(LoadMemoryByte ((ushort)(m_sIndirectPtr + 1)));
                        m_OffsetType = OffsetType.OFFSET_REGISTER_A;
                        m_Indirect    = true;
                        m_nPostCycles = 4;
                        break;

                        //  8 bit offset - get another byte

                    case 0x88:
                        m_Operand = LoadMemoryByte(IP++);
                        if ((m_Operand & 0x80) != 0x00)
                            m_Operand |= 0xFF00;
                        m_Operand += OffsetRegisters[m_cPostRegister];   // GetRegisterReference ();
                        m_OffsetType = OffsetType.OFFSET_8_BIT;
                        m_nPostCycles = 1;
                        m_OperandSize = 1;
                        break;
                    case 0x98:
                        m_sIndirectPtr = LoadMemoryByte (IP++);
                        if ((m_sIndirectPtr & 0x80) != 0x00)
                            m_sIndirectPtr |= 0xFF00;
                        m_sIndirectPtr += OffsetRegisters[m_cPostRegister];   // GetRegisterReference ();
                        m_Operand  = (ushort)(LoadMemoryByte (m_sIndirectPtr) * 256);
                        m_Operand += (ushort)(LoadMemoryByte ((ushort)(m_sIndirectPtr + 1)));
                        m_OffsetType = OffsetType.OFFSET_8_BIT;
                        m_Indirect    = true;
                        m_nPostCycles = 4;
                        m_OperandSize = 1;
                        break;

                        // 16 bit offset - get 2 more bytes

                    case 0x89:
                        m_Operand  = (ushort)(LoadMemoryByte (IP++) * 256);
                        m_Operand += LoadMemoryByte (IP++);
                        m_Operand += OffsetRegisters[m_cPostRegister];   // GetRegisterReference ();
                        m_OffsetType = OffsetType.OFFSET_16_BIT;
                        m_nPostCycles = 4;
                        m_OperandSize = 2;
                        break;
                    case 0x99:
                        m_sIndirectPtr  = (ushort)(LoadMemoryByte (IP++) * 256);
                        m_sIndirectPtr += LoadMemoryByte (IP++);
                        m_sIndirectPtr += OffsetRegisters[m_cPostRegister];   // GetRegisterReference ();
                        m_Operand  = (ushort)(LoadMemoryByte(m_sIndirectPtr) * 256);
                        m_Operand += (ushort)(LoadMemoryByte ((ushort)(m_sIndirectPtr + 1)));

                        m_OffsetType = OffsetType.OFFSET_16_BIT;
                        m_Indirect    = true;
                        m_nPostCycles = 7;
                        m_OperandSize = 2;
                        break;

                        //  D - Register Offset

                    case 0x8B:
                        m_Operand = m_D;
                        m_Operand += OffsetRegisters[m_cPostRegister];   // GetRegisterReference ();
                        m_OffsetType = OffsetType.OFFSET_REGISTER_D;
                        m_nPostCycles = 4;
                        break;
                    case 0x9B:
                        m_sIndirectPtr = m_D;
                        m_sIndirectPtr += OffsetRegisters[m_cPostRegister];   // GetRegisterReference ();
                        m_Operand  = (ushort)(LoadMemoryByte (m_sIndirectPtr) * 256);
                        m_Operand += (ushort)(LoadMemoryByte ((ushort)(m_sIndirectPtr + 1)));
                        m_OffsetType = OffsetType.OFFSET_REGISTER_D;
                        m_Indirect    = true;
                        m_nPostCycles = 7;
                        break;

                        //  Constant 8 bit offset from PC

                    case 0x8C:
                        m_Operand = LoadMemoryByte (IP++);
                        if ((m_Operand & 0x80) != 0x00)
                            m_Operand |= 0xFF00;
                        m_Operand += IP;
                        m_OffsetType = OffsetType.OFFSET_PCR_8;
                        m_nPostCycles = 1;
                        m_OperandSize = 1;
                        break;
                    case 0x9C:
                        m_sIndirectPtr = LoadMemoryByte (IP++);
                        if ((m_sIndirectPtr & 0x80) != 0x00)
                            m_sIndirectPtr |= 0xFF00;
                        m_sIndirectPtr += IP;
                        m_Operand  = (ushort)(LoadMemoryByte (m_sIndirectPtr) * 256);
                        m_Operand += (ushort)(LoadMemoryByte ((ushort)(m_sIndirectPtr + 1)));
                        m_OffsetType = OffsetType.OFFSET_PCR_8;
                        m_Indirect    = true;
                        m_nPostCycles = 4;
                        m_OperandSize = 1;
                        break;

                        //  Constant 16 bit offset from PC

                    case 0x8D:
                        m_Operand  =  (ushort)(LoadMemoryByte (IP++) * 256);
                        m_Operand +=  LoadMemoryByte (IP++);
                        m_Operand += IP;
                        m_OffsetType = OffsetType.OFFSET_PCR_16;
                        m_nPostCycles = 5;
                        m_OperandSize = 2;
                        break;
                    case 0x9D:
                        m_sIndirectPtr  = (ushort)(LoadMemoryByte (IP++) * 256);
                        m_sIndirectPtr += LoadMemoryByte (IP++);
                        m_sIndirectPtr += IP;
                        m_Operand  = (ushort)(LoadMemoryByte (m_sIndirectPtr) * 256);
                        m_Operand += (ushort)(LoadMemoryByte((ushort)(m_sIndirectPtr + 1)));

                        m_OffsetType = OffsetType.OFFSET_PCR_16;
                        m_Indirect    = true;
                        m_nPostCycles = 8;
                        m_OperandSize = 2;
                        break;

                        //  Extended Indirect

                    case 0x9F:
                        m_sIndirectPtr  = (ushort)(LoadMemoryByte (IP++) * 256);
                        m_sIndirectPtr += LoadMemoryByte (IP++);
                        m_Operand  = (ushort)(LoadMemoryByte (m_sIndirectPtr) * 256);
                        m_Operand += (ushort)(LoadMemoryByte((ushort)(m_sIndirectPtr + 1)));
                        m_OffsetType = OffsetType.EXTENDED_INDIRECT;
                        m_Indirect    = true;
                        m_nPostCycles = 5;
                        m_OperandSize = 2;
                        break;

                    default:
                        attr = AddressingModes.AM_ILLEGAL;
                        break;
	            }
            }
            else    // 5 bit non indirect offset
            {
                m_OffsetType = OffsetType.OFFSET_5_BIT;
                m_nPostCycles = 1;

                m_Operand  = (ushort)(m_cPostByte & 0x001F);

	            if ((m_Operand & 0x10) != 0) 
		            m_Operand |= 0xffe0;

                m_Operand += OffsetRegisters[m_cPostRegister];   // GetRegisterReference ();
            }

            return (attr);
        }

        AddressingModes MakeOperand(AddressingModes attribute)
        {
            AddressingModes attr;

            attr = attribute;

            if (((int)attribute & 0x00FF) == (int)AddressingModes.AM_INDEXED_6809)
            {
                attr = SetupPostByte (attribute);

                // SetUpPostByte will set attr = AM_ILLEGAL if post byte is bogus
                //
                if (attr == AddressingModes.AM_ILLEGAL)
                {
                    //String strMessage;
        
                    //strMessage.Format ("IP: %04X OPERAND: %04X", m_IP, m_Operand);
                    //AfxMessageBox (strMessage);
                }
            }
            else
            {
                int nNumBytes;

                // see if normal mode (not page 2 or 3 instruction)

                if (((int)attribute & 0xFF00) == 0)  
                    nNumBytes = opctbl[m_nTable,m_OpCode].numbytes;

                // page 2 or page 3 instruction

                else                            
                    nNumBytes = opctbl[m_nTable,m_OpCode].numbytes - 1;

                //  Get the Operand

                switch (nNumBytes)
                {
                    case 1:
                        m_Operand = 0;
                        m_OperandSize = 0;
                        break;
                    case 2:
                        m_Operand = LoadMemoryByte (IP++);
                        m_OperandSize = 1;
                        break;
                    case 3:
                        m_Operand  = (ushort)(LoadMemoryByte (IP++) * 256);
                        m_Operand += LoadMemoryByte (IP++);
                        m_OperandSize = 2;
                        break;
                }
            }

            if (((int)attribute & 0x00FF) == (int)AddressingModes.AM_INDEXED_6809)
                OffsetRegisters[m_cPostRegister] += (ushort)m_Postincrement;

            return (attr);
        }

        //ushort GetRegisterReference ()     // this is suppose to return a pointer to a short
        //{
        //    switch (m_cPostRegister)
        //    {
        //        case 0:
        //            return m_X;
        //            break;
        //        case 1:
        //            return m_Y;
        //            break;
        //        case 2:
        //            return m_U;
        //            break;
        //        default:
        //            return m_S;
        //            break;
        //    }
        //}

        void CheckForBreakPoint()
        {
            // See if any breakpoints are set

            //if (Program.m_nBreakpointsEnabled || m_nStepOver)
            //{
            //    if (m_IP == sNextIPToStopOn)
            //    {
            //        m_nStepOver = false;
            //        m_nSingleStepMode = true;
            //    }
            //    else
            //    {
            //        for (i = 0; i < Program.m_nBreakpointCount; i++)
            //        {
            //            if (Program.m_sBreakpointAddress[i] == m_IP)
            //            {
            //                CButton *pDebugButton = (CButton *) m_pView->GetDlgItem (IDC_BUTTON_DEBUG);
            //                pDebugButton->SetWindowText ("&Normal");
            //                m_nSingleStepMode = true;
            //                break;
            //            }
            //        }
            //    }
            //}

        }

        public override byte ReadFromFirst64K(ushort m)
        {
            // always read IO from first 64K segment

            return (Memory.MemorySpace[m]);
        }

        public override void WriteToFirst64K(ushort m, byte b)
        {
            Memory.MemorySpace[m] = b;
        }

        static int counter = 0;
        public override void Run()
        {
            //Console.Write("Building OpCode Table ... ");

            BuildOpCodeTable();         // before anything else - we need an OPCode Table
            
            //Console.Write("Done\n");
            
            m_nTable = MC6809_NORMAL;   // make sure the first instruction we get is from the first 256 instruction in the OPCode table

            //create a console thread ToString handle Keyboard and screen
            //
            //  This thread will create it's own keyboard and screen threads.
            //
            
            //Console.Write("Starting Console Thread ... ");
            
            Thread consoleThread = new Thread(Program._theConsole.run);
            //Program._theConsole.Cpu = this;
            consoleThread.Start();
            
            //Console.Write("Done\n");

            // Set any processor board switches and jumpers specified in the configuration.xml file
//            Console.Write("Getting Processor Settings ... ");
            switch (Program.m_nProcessorBoard)
            {
                case (int)CPUBoardTypes.MP_09:


                    Memory.m_DAT_BASE = MP09_DAT_BASE;

                    Switch1B = Program.GetConfigurationAttribute("Global/ProcessorSwitch", "SW1B", 0) == 0 ? false : true;
                    Switch1C = Program.GetConfigurationAttribute("Global/ProcessorSwitch", "SW1C", 0) == 0 ? false : true;
                    Switch1D = Program.GetConfigurationAttribute("Global/ProcessorSwitch", "SW1D", 0) == 0 ? false : true;

                    RamE000 = Program.GetConfigurationAttribute("Global/ProcessorJumpers", "E000_RAM", 0) == 0 ? false : true;
                    RamE800 = Program.GetConfigurationAttribute("Global/ProcessorJumpers", "E800_RAM", 0) == 0 ? false : true;
                    RamF000 = Program.GetConfigurationAttribute("Global/ProcessorJumpers", "F000_RAM", 0) == 0 ? false : true;
                    RomE000 = Program.GetConfigurationAttribute("Global/ProcessorJumpers", "E000_ROM", 0) == 0 ? false : true;
                    RomE800 = Program.GetConfigurationAttribute("Global/ProcessorJumpers", "E800_ROM", 0) == 0 ? false : true;
                    RomF000 = Program.GetConfigurationAttribute("Global/ProcessorJumpers", "F000_ROM", 0) == 0 ? false : true;
                    break;

                case (int)CPUBoardTypes.MPU_1:
                    Memory.m_DAT_BASE = MPU1_DAT_BASE;
                    break;

                default:
                    break;
            }

            m_nStepOver = false;
            //Console.Write("Done\n");
            int i, j;
            AddressingModes attribute;

            if (Program.m_nProcessorBoard == (int)CPUBoardTypes.MP_09 || Program.m_nProcessorBoard == (int)CPUBoardTypes.MPU_1)
            {
                for (i = 0; i < 256; i++)
                    Memory.DeviceMap[Memory.m_DAT_BASE + i] = (byte)Devices.DEVICE_DAT;
            }

            if (Program.m_nProcessorBoard == (int)CPUBoardTypes.MP_09 || Program.m_nProcessorBoard == (int)CPUBoardTypes.MPU_1)
            {
                for (i = 0; i < 16; i++)
                    Memory.m_MemoryDAT[i] = 0x0000F000;
            }
            else
            {
                for (i = 0; i < 16; i++)
                    Memory.m_MemoryDAT[i] = (ulong)(i * 0x1000);
            }

            //Program.m_pScreen->EnableBlinkingCursor (1);
            //m_nResetPressed = true;

            for (i = 0; i < 3; i++)
            {
                for (j = 0; j < 256; j++)
                {
                    opctbl[i, j].lCount = 0;
                }
            }

            //QueryPerformanceFrequency (&m_lFrequency);

            //m_nPeekFrequency = Program.m_nPeekFrequency;

            ResetPressed = true;
            Running = true;            // say that we are running
            //Console.WriteLine("Running");

            while (Running)
            {
                if (Program.debugMode)
                {
                    if ((counter++ % 1000000) == 0)
                    {
                        try
                        {
                            if (MySocket == null)
                            {
                                //Console.WriteLine("Trying to Accept connection");
                                MySocket = Program.listenSocket.Accept();
                            }

                            MySocket.ReceiveTimeout = 1;

                            byte[] buffer = new byte[1024];
                            try
                            {
                                //Console.WriteLine("Waiting for data");
                                int size = MySocket.Receive(buffer);
                                if (size > 1)
                                {
                                    switch (buffer[0])
                                    {
                                        case (byte)'K':
                                            console._keyboard.StuffKeyboard(Encoding.ASCII.GetString(buffer, 1, size - 1));
                                            break;
                                    }
                                }

                            }
                            catch
                            {
                                //Console.WriteLine("Nothing to read");
                            }
                        }
                        catch
                        {
                            //Console.WriteLine("Nothing to accept");
                        }
                    }
                }

                // check all hardware to see if any are generating an interrupt
                //
                //  We have to do this before we check for Reset being pressed because if
                //  reset is pressed, we will set it back to 0.
                //

                if (Program.TraceEnabled)
                    clsTraceBuffer[TraceIndex].sRegisterInterrupts = IrqAsserted;

                //#ifdef USE_MUTEX_FOR_PROCESSOR_6809
                //            ReleaseMutex(Program.m_hInterruptMaskMutex);
                //#endif
                //#endif

                // get a local copy of the interrupt bits in case it changes while we are executing the instruction

                IrqAsserted = InterruptRegister == 0 ? false : true;

                // see if the user has pressed the reset button

                if (ResetPressed)
                {
                    IP = LoadMemoryWord(0xFFFE);
                    _dReg.hi = 0x00;
                    _dReg.lo = 0x00;
                    m_D = 0x0000;
                    m_DP = 0x00;
                    m_CCR = (byte)(CCR_ENTIREFLAG | CCR_FIRQMASK | CCR_INTERRUPT);      // 0xD0
                    OffsetRegisters[(int)OffsetRegisterIndex.m_X] = 0x0000;
                    OffsetRegisters[(int)OffsetRegisterIndex.m_Y] = 0x0000;
                    OffsetRegisters[(int)OffsetRegisterIndex.m_S] = 0x0000;
                    OffsetRegisters[(int)OffsetRegisterIndex.m_U] = 0x0000;
                    ResetPressed = false;
                    IrqAsserted = false;
                    IrqPressed = false;
                    InWait = false;
                    InSync = false;

                    //m_nTraceIndex = 0;
                }

                // see if the user has pressed the NMI button

                if (NmiPressed)
                {
                    PushOntoStack(0xFF, ref OffsetRegisters[(int)OffsetRegisterIndex.m_S], (int)SYSTEM_STACK);
                    m_CCR |= (byte)(CCR_FIRQMASK | CCR_INTERRUPT);
                    IP = LoadMemoryWord(0xFFFC);
                    NmiPressed = false;
                    IrqPressed = false;
                    InWait = false;
                    InSync = false;
                }

                // see if we are recovering from a SYNC

                if (InSync)
                {
                    // ---------------------------------------------------------
                    // DON'T NEED ANY SPECIAL PROCESSING
                    // ---------------------------------------------------------
                    //// see if either the I or F flags are enabled
                    //
                    //if (((m_CCR & CCR_INTERRUPT) == 0) || ((m_CCR & CCR_FIRQMASK) == 0))
                    //{
                    //    // proceed normally with the IRQ
                    //}
                    //else
                    //{
                    //    // If they are masked, just treat SYNC as a NOP and execute the next instruction
                    //}

                    InSync = false;
                }

                // see if there are any IRQ's pending and interrupts are NOT masked

                if ((IrqAsserted) && ((m_CCR & CCR_INTERRUPT) == 0))
                {
                    if ((InterruptRegister & 0x0001) != 0)
                    {
                    }
                    if (!InWait)
                    {
                        //if ((m_nInterruptRegister & (uint)0xFFFB) != 0)        // ignore DMAF3 Interrupts
                        //{
                        //}

                        if ((m_CCR & CCR_ENTIREFLAG) != 0)
                            PushOntoStack(0xFF, ref OffsetRegisters[(int)OffsetRegisterIndex.m_S], (int)SYSTEM_STACK);
                        else
                            PushOntoStack(0x81, ref OffsetRegisters[(int)OffsetRegisterIndex.m_S], (int)SYSTEM_STACK);
                    }
                    m_CCR |= (byte)(CCR_FIRQMASK | CCR_INTERRUPT);
                    IP = LoadMemoryWord(0xFFF8);
                    IrqPressed = false;
                    NmiPressed = false;
                    InWait = false;
                }

                CheckForBreakPoint();
                if ((IP == 0xF231) || (IP == 0xF233) || (IP == 0xF234) || (IP == 0xF235))
                {
                    IP = IP;
                }

                if ((IP == 0xF221) || (IP == 0xF228) || (IP == 0xF22B) || (IP == 0xF22E) || (IP == 0xF231) || (IP == 0xF233) || (IP == 0xF3B5) || (IP == 0xF2D0))
                {
                    IP = IP;
                }

                if ((IP == 0xD6C0) || (IP == 0xD6C5) || (IP == 0xD6C7) || (IP == 0xD6C9) || (IP == 0xD6CB) || (IP == 0xD6CD) || (IP == 0xD6CF))
                {
                    IP = IP;
                }

                // save the current Instruction pointer

                CurrentIP = IP;

                // Get the next opcode to execute

                m_OpCode = LoadMemoryByte(IP++);
                attribute = opctbl[0, m_OpCode].attribute;
                m_nTable = opctbl[0, m_OpCode].table;

                // if this is not a page 0 opcode, get the next byte

                if (m_nTable != 0)
                {
                    m_OpCode = LoadMemoryByte(IP++);
                    attribute = opctbl[m_nTable, m_OpCode].attribute;
                }

                //QueryPerformanceCounter (&m_lStartTime);

                // update it's count

                opctbl[m_nTable, m_OpCode].lCount++;
                m_Cycles = opctbl[m_nTable, m_OpCode].cycles;

                // See if we need to get a post byte on indexed mode

                m_Indirect = false;
                m_Predecrement = 0;
                m_Postincrement = 0;
                m_OffsetType = OffsetType.OFFSET_INVALID;
                m_nPostCycles = 0;

                // MakeOperand will return attribute = AM_ILLEGAL if post byte is bogus

                attribute = MakeOperand(attribute);

                SaveState(attribute);

                //  Execute the instruction

                //// 
                //  0xA4CC = start of interrupt dispatcher,    0xA58E = last instruction of interrupt dispatcher
                //  0X693F = start of clock interrupt handler, 0x6A62 = last instruction of clock interrupt handler
                //
                switch (attribute)
                {
                    case AddressingModes.AM_INHERENT_6809:
                        Execute6809Inherent();
                        break;

                    case AddressingModes.AM_INHERENT_PAGE2:
                        ExecutePage2Inherent();
                        break;

                    case AddressingModes.AM_INHERENT_PAGE3:
                        ExecutePage3Inherent();
                        break;

                    case AddressingModes.AM_DIRECT_6809:
                        Execute6809Direct();
                        break;

                    case AddressingModes.AM_DIRECT_PAGE2:
                        ExecutePage2Direct();
                        break;

                    case AddressingModes.AM_DIRECT_PAGE3:
                        ExecutePage3Direct();
                        break;

                    case AddressingModes.AM_RELATIVE_6809:
                        Execute6809Relative();
                        break;

                    case AddressingModes.AM_RELATIVE_PAGE2:
                        ExecutePage2Relative();
                        break;

                    case AddressingModes.AM_RELATIVE_PAGE3:
                        ExecutePage3Relative();
                        break;

                    case AddressingModes.AM_EXTENDED_6809:
                        Execute6809Extended();
                        break;

                    case AddressingModes.AM_EXTENDED_PAGE2:
                        ExecutePage2Extended();
                        break;

                    case AddressingModes.AM_EXTENDED_PAGE3:
                        ExecutePage3Extended();
                        break;

                    case AddressingModes.AM_IMM16_6809:
                        Execute6809Immediate16();
                        break;

                    case AddressingModes.AM_IMM16_PAGE2:
                        ExecutePage2Immediate16();
                        break;

                    case AddressingModes.AM_IMM16_PAGE3:
                        ExecutePage3Immediate16();
                        break;

                    case AddressingModes.AM_IMM8_6809:
                        Execute6809Immediate8();
                        break;

                    case AddressingModes.AM_IMM8_PAGE2:
                        ExecutePage2Immediate8();
                        break;

                    case AddressingModes.AM_IMM8_PAGE3:
                        ExecutePage3Immediate8();
                        break;

                    case AddressingModes.AM_INDEXED_6809:
                        Execute6809Indexed();
                        break;

                    case AddressingModes.AM_INDEXED_PAGE2:
                        ExecutePage2Indexed();
                        break;

                    case AddressingModes.AM_INDEXED_PAGE3:
                        ExecutePage3Indexed();
                        break;

                    case AddressingModes.AM_ILLEGAL:

                        if (Program.TraceEnabled)
                        {
                            using (StreamWriter sw = new StreamWriter(File.Open(Program._traceFilePath, FileMode.Append, FileAccess.Write, FileShare.Write)))
                            {
                                for (int debugIndex = 0; debugIndex < TraceIndex; debugIndex++)
                                {
                                    sw.WriteLine(DebugLine[debugIndex]);
                                }
                            }
                            TraceIndex = 0;
                        }

                        ushort x = (ushort)((IP - 1) & (ushort)0x0FFF);

                        string message = string.Format("Invalid OPCODE [{0}] encountered at address [{1}{2}]", m_OpCode.ToString("X2"), (Memory.m_MemoryDAT[((CurrentIP & 0xF000) >> 12) & 0x0F] >> 12).ToString("X2"), x.ToString("X3"));
                        Console.WriteLine(message);
                        //CoreDump();

                        Console.WriteLine("Press any key to reset emulation");
                        ResetPressed = true;
                        Console.ReadKey();
                        break;
                }

                m_Cycles += m_nPostCycles;
                TotalCycles += m_Cycles;
                CyclesThisPeriod += m_Cycles;
            }

            //CoreDump ();
            //DumpTraceBuffers ();
        }
    }
}
 