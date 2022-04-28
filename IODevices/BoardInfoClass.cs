using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;

namespace Memulator
{
    class BoardInfoClass
    {
        public int      nBoardId;
        public byte     cDeviceType;
        public ushort   sBaseAddress;
        public ushort   sNumberOfBytes;
        public bool     bInterruptEnabled;
        public String   strGuid;
        public int      nSectorsOnTrack0Side0ForOS9;
        public string   sBoardTypeName;

        //IAHODevicePtr   ahoDevice;
    }
}
