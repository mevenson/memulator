﻿using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;

namespace Memulator
{
    class CPrinter : IODevice
    {
        public override void Init(int nWhichController, byte[] sMemoryBase, ushort sBaseAddress, int nRow, bool bInterruptEnabled)
        {
        }
    }
}