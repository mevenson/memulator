using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;

namespace Memulator
{
    class TraceEntry
    {
        public  ushort sExecutionTrace;
        public  ushort sStackTrace;
        public  byte   cTable;
        public  byte   cOpcode;
        public  byte   cPostByte;
        public  ushort sOperand;
        public  ushort sValue;
        public  byte   cRegisterDAT;
        public  byte   cRegisterOperandDAT;
        public  byte   cRegisterDP;
        public  byte   cRegisterA;
        public  byte   cRegisterB;
        public  ushort sRegisterX;
        public  ushort sRegisterY;
        public  ushort sRegisterU;
        public  byte   cRegisterCCR;
        public  bool   sRegisterInterrupts;
        public  uint   nPostIncerment;
        public  uint   nPreDecrement;
    }
}
