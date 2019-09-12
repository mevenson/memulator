using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;

namespace Memulator
{
    class BoardInfoClass
    {
        public byte     cDeviceType;
        public ushort   sBaseAddress;
        public ushort   sNumberOfBytes;
        public bool     bInterruptEnabled;
        public String   strGuid;

        //IAHODevicePtr   ahoDevice;
    }
}
