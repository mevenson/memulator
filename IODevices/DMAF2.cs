using System;
using System.Collections;
using System.Collections.Generic;
//using System.Linq;
using System.Text;

using System.Threading;
using System.IO;
using System.Diagnostics;

namespace Memulator
{
    class DMAF2 : DMAFDevice
    {
        public DMAF2()
        {
            latchRegisterIsInverted = true;

            m_nAbort = false;
            m_nCurrentSideSelected = 0;
            m_nCurrentDensitySelected = 0;

            m_nProcessorRunning = false;
            m_nDMAFRunning = false;
            m_nDMAFRate = 1;            // count at 1 MHZ
            m_nEnableT1C_Countdown = false;

            m_nFDCReading = m_nFDCWriting = false;
            m_nFDCReadPtr = 0;
            m_nFDCWritePtr = 0;

            for (int i = 0; i < m_DMAF_DMA_CHANNELRegister.Length; i++)
                m_DMAF_DMA_CHANNELRegister[i] = 0;

            for (int i = 0; i < m_DMAF_AT_TCRegister.Length; i++)
            {
                m_DMAF_AT_TCRegister[i] = new DMAF_AT_TC_REGISTER();
            }
            for (int i = 0; i < m_DMAF_DMA_ADDRESSRegister.Length; i++)
            {
                m_DMAF_DMA_ADDRESSRegister[i] = new DMAF_DMA_ADDRESS_REGISTER();
            }
            for (int i = 0; i < m_DMAF_DMA_DMCNTRegister.Length; i++)
            {
                m_DMAF_DMA_DMCNTRegister[i] = new DMAF_DMA_BYTECNT_REGISTER();
            }

            DMAF_AT_IFRRegister = 0;
            DMAF_AT_DTBRegister = 0x20;    // make sure Archive exception bit is OFF
            DMAF_AT_IERRegister = 0;

            //m_fpDMAActivity = null;
            m_nBoardInterruptRegister = 0;
            m_nInterruptingDevice = 0;
            DMAF_HLD_TOGGLERegister = 0;

            _DMAFInterruptDelayTimer = new MicroTimer();
            _DMAFInterruptDelayTimer.MicroTimerElapsed += new MicroTimer.MicroTimerElapsedEventHandler(DMAF_InterruptDelayTimerProc);

            _DMAFTimer = new MicroTimer();
            _DMAFTimer.MicroTimerElapsed += new MicroTimer.MicroTimerElapsedEventHandler(DMAF_TimerProc);

            SetRegisterDescriptions(0x0040);
        }

        #region Floppy Write Track Variables enums and functions

        /* X^16 + X^12 + X^5+ 1 */

        ushort[] crc_table = new ushort[]
        {
            0x0000, 0x1021, 0x2042, 0x3063, 0x4084, 0x50a5,
            0x60c6, 0x70e7, 0x8108, 0x9129, 0xa14a, 0xb16b,
            0xc18c, 0xd1ad, 0xe1ce, 0xf1ef, 0x1231, 0x0210,
            0x3273, 0x2252, 0x52b5, 0x4294, 0x72f7, 0x62d6,
            0x9339, 0x8318, 0xb37b, 0xa35a, 0xd3bd, 0xc39c,
            0xf3ff, 0xe3de, 0x2462, 0x3443, 0x0420, 0x1401,
            0x64e6, 0x74c7, 0x44a4, 0x5485, 0xa56a, 0xb54b,
            0x8528, 0x9509, 0xe5ee, 0xf5cf, 0xc5ac, 0xd58d,
            0x3653, 0x2672, 0x1611, 0x0630, 0x76d7, 0x66f6,
            0x5695, 0x46b4, 0xb75b, 0xa77a, 0x9719, 0x8738,
            0xf7df, 0xe7fe, 0xd79d, 0xc7bc, 0x48c4, 0x58e5,
            0x6886, 0x78a7, 0x0840, 0x1861, 0x2802, 0x3823,
            0xc9cc, 0xd9ed, 0xe98e, 0xf9af, 0x8948, 0x9969,
            0xa90a, 0xb92b, 0x5af5, 0x4ad4, 0x7ab7, 0x6a96,
            0x1a71, 0x0a50, 0x3a33, 0x2a12, 0xdbfd, 0xcbdc,
            0xfbbf, 0xeb9e, 0x9b79, 0x8b58, 0xbb3b, 0xab1a,
            0x6ca6, 0x7c87, 0x4ce4, 0x5cc5, 0x2c22, 0x3c03,
            0x0c60, 0x1c41, 0xedae, 0xfd8f, 0xcdec, 0xddcd,
            0xad2a, 0xbd0b, 0x8d68, 0x9d49, 0x7e97, 0x6eb6,
            0x5ed5, 0x4ef4, 0x3e13, 0x2e32, 0x1e51, 0x0e70,
            0xff9f, 0xefbe, 0xdfdd, 0xcffc, 0xbf1b, 0xaf3a,
            0x9f59, 0x8f78, 0x9188, 0x81a9, 0xb1ca, 0xa1eb,
            0xd10c, 0xc12d, 0xf14e, 0xe16f, 0x1080, 0x00a1,
            0x30c2, 0x20e3, 0x5004, 0x4025, 0x7046, 0x6067,
            0x83b9, 0x9398, 0xa3fb, 0xb3da, 0xc33d, 0xd31c,
            0xe37f, 0xf35e, 0x02b1, 0x1290, 0x22f3, 0x32d2,
            0x4235, 0x5214, 0x6277, 0x7256, 0xb5ea, 0xa5cb,
            0x95a8, 0x8589, 0xf56e, 0xe54f, 0xd52c, 0xc50d,
            0x34e2, 0x24c3, 0x14a0, 0x0481, 0x7466, 0x6447,
            0x5424, 0x4405, 0xa7db, 0xb7fa, 0x8799, 0x97b8,
            0xe75f, 0xf77e, 0xc71d, 0xd73c, 0x26d3, 0x36f2,
            0x0691, 0x16b0, 0x6657, 0x7676, 0x4615, 0x5634,
            0xd94c, 0xc96d, 0xf90e, 0xe92f, 0x99c8, 0x89e9,
            0xb98a, 0xa9ab, 0x5844, 0x4865, 0x7806, 0x6827,
            0x18c0, 0x08e1, 0x3882, 0x28a3, 0xcb7d, 0xdb5c,
            0xeb3f, 0xfb1e, 0x8bf9, 0x9bd8, 0xabbb, 0xbb9a,
            0x4a75, 0x5a54, 0x6a37, 0x7a16, 0x0af1, 0x1ad0,
            0x2ab3, 0x3a92, 0xfd2e, 0xed0f, 0xdd6c, 0xcd4d,
            0xbdaa, 0xad8b, 0x9de8, 0x8dc9, 0x7c26, 0x6c07,
            0x5c64, 0x4c45, 0x3ca2, 0x2c83, 0x1ce0, 0x0cc1,
            0xef1f, 0xff3e, 0xcf5d, 0xdf7c, 0xaf9b, 0xbfba,
            0x8fd9, 0x9ff8, 0x6e17, 0x7e36, 0x4e55, 0x5e74,
            0x2e93, 0x3eb2, 0x0ed1, 0x1ef0
        };

        enum WRITE_TRACK_STATES
        {
            WaitForIDRecordMark = 0,
            WaitForIDRecordTrack,
            WaitForIDRecordSide,
            WaitForIDRecordSector,
            WaitForIDRecordSize,
            WaitForIDRecordWriteCRC,
            WaitForDataRecordMark,
            GettingDataRecord,
            GetLastFewBytes
        }

        int currentWriteTrackState = (int)WRITE_TRACK_STATES.WaitForIDRecordMark;
        int writeTrackTrack = 0;
        int writeTrackSide = 0;
        int writeTrackSector = 0;
        int writeTrackSize = 0;

        int writeTrackMinSector = 0;
        int writeTrackMaxSector = 0;

        byte[] writeTrackWriteBuffer = new byte[65536];
        int writeTrackWriteBufferIndex = 0;

        int writeTrackBytesWrittenToSector = 0;
        int writeTrackBytesPerSector = 0;
        int writeTrackBufferOffset = 0;    // used to put sector data into the track buffer since the sector do not come in order
        int writeTrackBufferSize = 0;
        int totalBytesThisTrack = 0;    // initial declaration

        byte previousByte = 0x00;
        int sectorsInWriteTrackBuffer = 0;
        int lastFewBytesRead = 0;
        byte lastSectorAccessed = 1;

        private int m_statusReadsWithoutDataRegisterAccess;
        private int m_nCurrentSideSelected = 0;
        private bool m_nReadingTrack = false;
        private bool m_nWritingTrack = false;

        [Conditional("DEBUG")]
        private void LogOffsetCalculations(int nDrive, long offset)
        {
            if (Program.enableFD2ActivityLogChecked)
            {
                using (StreamWriter sw = new StreamWriter(File.Open(Program.activityLogFileFD2, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)))
                {
                    sw.WriteLine(string.Format("drive: {0}, STZ: {1}, SPT: {2}, FMT: {3},  track: {4}, side: {5}, sector: {6}, calculated offset: {7}"
                        , nDrive
                        , Program.SectorsOnTrackZeroSideZero[nDrive].ToString("X2")
                        //, Program.SectorsOnTrackZeroSideOne[nDrive].ToString("X2")
                        , Program.SectorsPerTrack[nDrive].ToString("X2")
                        , Program.FormatByte[nDrive].ToString("x2")
                        , m_nFDCTrack.ToString("X2")
                        , m_nCurrentSideSelected.ToString("X1")
                        , m_nFDCSector.ToString("X2")
                        , offset.ToString("X8")));
                }
            }
            else
                return;
        }

        [Conditional("DEBUG")]
        void LogFloppyWriteTrack(long fileOffset, byte[] writeBuffer, int startIndex, int bytesToTransfer)
        {
            if (Program.enableFD2ActivityLogChecked)
            {
                using (StreamWriter sw = new StreamWriter(File.Open(Program.activityLogFileFD2, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)))
                {
                    sw.WriteLine(string.Format("Writing Track 0x{0}: with starting sector of 0x{1} to file offset 0x{2}", writeTrackTrack.ToString("X2"), writeTrackMinSector.ToString("X2"), fileOffset.ToString("X8")));
                    string asciiBytes = "";

                    for (int i = startIndex; i < bytesToTransfer; i++)
                    {
                        if ((i % 32) == 0)                                          // output index as X4
                        {
                            if (i != 0)
                            {
                                // output ascii

                                sw.Write(asciiBytes);
                                asciiBytes = "";

                                // and new line

                                sw.Write("\n");
                            }

                            sw.Write(string.Format("{0} ", i.ToString("X4")));
                        }

                        if (((i % 4) == 0) && ((i % 32) != 0))                      // output a space between every 4 bytes
                            sw.Write(" ");

                        if (((i % 8) == 0) && ((i % 32) != 0))                      // output another space between every 8 bytes
                            sw.Write(" ");

                        sw.Write(string.Format("{0} ", writeBuffer[i].ToString("X2")));

                        if ((writeBuffer[i] >= ' ') && writeBuffer[i] <= 0x7E)
                            asciiBytes += (char)writeBuffer[i];
                        else
                            asciiBytes += ".";
                    }

                    if (asciiBytes.Length > 0)
                        sw.Write(asciiBytes.ToString());

                    sw.Write("\n");
                    sw.Write("\n");
                }

            }
        }

        [Conditional("DEBUG")]
        void LogFloppyWrite(long fileOffset, string message, int bytesToTransfer)
        {
            if (Program.enableFD2ActivityLogChecked)
            {
                using (StreamWriter sw = new StreamWriter(File.Open(Program.activityLogFileFD2, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)))
                {
                    sw.WriteLine(string.Format("Writing: {0} (0x{1}) bytes - {2}", bytesToTransfer, bytesToTransfer.ToString("X4"), message));
                }
            }
        }

        [Conditional("DEBUG")]
        void LogFloppyWrite(long fileOffset, byte[] writeBuffer, int startIndex, int bytesToTransfer)
        {
            if (Program.enableFD2ActivityLogChecked)
            {
                using (StreamWriter sw = new StreamWriter(File.Open(Program.activityLogFileFD2, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)))
                {
                    sw.WriteLine("Writing: ");
                    string asciiBytes = "";

                    for (int i = startIndex; i < bytesToTransfer; i++)
                    {
                        if ((i % 32) == 0)                                          // output index as X4
                        {
                            if (i != 0)
                            {
                                // output ascii

                                sw.Write(asciiBytes);
                                asciiBytes = "";

                                // and new line

                                sw.Write("\n");
                            }

                            sw.Write(string.Format("{0} ", (i + fileOffset).ToString("X8")));
                        }

                        if (((i % 4) == 0) && ((i % 32) != 0))                      // output a space between every 4 bytes
                            sw.Write(" ");

                        if (((i % 8) == 0) && ((i % 32) != 0))                      // output another space between every 8 bytes
                            sw.Write(" ");

                        sw.Write(string.Format("{0} ", writeBuffer[i].ToString("X2")));

                        if ((writeBuffer[i] >= ' ') && writeBuffer[i] <= 0x7E)
                            asciiBytes += (char)writeBuffer[i];
                        else
                            asciiBytes += ".";
                    }

                    if (asciiBytes.Length > 0)
                        sw.Write(asciiBytes.ToString());

                    sw.Write("\n");
                    sw.Write("\n");
                }
            }
        }

        [Conditional("DEBUG")]
        void LogFloppyRead(byte[] readBuffer, int startIndex, int bytesTransferred)
        {
            if (Program.enableFD2ActivityLogChecked)
            {
                using (StreamWriter sw = new StreamWriter(File.Open(Program.activityLogFileFD2, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)))
                {
                    sw.WriteLine("Reading: ");
                    string asciiBytes = "";

                    for (int i = startIndex; i < bytesTransferred; i++)
                    {
                        if ((i % 32) == 0)                                          // output index as X4
                        {
                            if (i != 0)
                            {
                                // output ascii

                                sw.Write(asciiBytes);
                                asciiBytes = "";

                                // and new line

                                sw.Write("\n");
                            }

                            sw.Write(string.Format("{0} ", (i + m_lFileOffset).ToString("X8")));
                        }

                        if (((i % 4) == 0) && ((i % 32) != 0))                      // output a space between every 4 bytes
                            sw.Write(" ");

                        if (((i % 8) == 0) && ((i % 32) != 0))                      // output another space between every 8 bytes
                            sw.Write(" ");

                        sw.Write(string.Format("{0} ", readBuffer[i].ToString("X2")));

                        if ((readBuffer[i] >= ' ') && readBuffer[i] <= 0x7E)
                            asciiBytes += (char)readBuffer[i];
                        else
                            asciiBytes += ".";
                    }

                    if (asciiBytes.Length > 0)
                        sw.Write(asciiBytes.ToString());

                    sw.Write("\n");
                    sw.Write("\n");
                }
            }
        }

        bool logActivityReads = false;
        bool logActivityWrites = true;

        //[Conditional("DEBUG")]
        //private void LogFloppyActivityFromFD2(int whichRegister, byte value, bool read = true)
        //{
        //    if (Program.enableFD2ActivityLogChecked)
        //    {
        //        if ((logActivityReads && read) || (logActivityWrites && !read))
        //        {
        //            LogActivityValues thisLogActivityValues = new LogActivityValues();
        //            thisLogActivityValues.whichRegister = whichRegister;
        //            thisLogActivityValues.value = value;
        //            thisLogActivityValues.read = read;

        //            bool thisIsTheFirst = false;
        //            bool logIt = false;

        //            if (previousLogActivity == null)
        //            {
        //                previousLogActivity = new LogActivityValues();
        //                previousLogActivity.whichRegister = whichRegister;
        //                previousLogActivity.value = value;
        //                previousLogActivity.read = read;

        //                thisIsTheFirst = true;
        //                logIt = true;
        //            }
        //            else
        //            {
        //                // if this is not the first entry - do not check for duplicate

        //                if (!thisIsTheFirst)
        //                {
        //                    if (previousLogActivity.whichRegister == thisLogActivityValues.whichRegister && previousLogActivity.value == thisLogActivityValues.value && previousLogActivity.read == thisLogActivityValues.read)
        //                    {
        //                        // this is a duplicate - increment the counter
        //                        duplicateCount++;
        //                        if (duplicateCount >= 100000)
        //                        {
        //                            // print it anyway
        //                            using (StreamWriter sw = new StreamWriter(File.Open(Program.activityLogFileFD2, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)))
        //                            {
        //                                sw.WriteLine(string.Format(" The above line was duplicated {0} times", duplicateCount));

        //                                previousLogActivity.whichRegister = whichRegister;
        //                                previousLogActivity.value = value;
        //                                previousLogActivity.read = read;

        //                                duplicateCount = 0;
        //                            }
        //                        }
        //                    }
        //                    else
        //                    {
        //                        if (duplicateCount != 0)
        //                        {
        //                            // we have had duplicates - show it

        //                            using (StreamWriter sw = new StreamWriter(File.Open(Program.activityLogFileFD2, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)))
        //                            {
        //                                sw.WriteLine(string.Format(" The above line was duplicated {0} time", duplicateCount));

        //                                previousLogActivity.whichRegister = whichRegister;
        //                                previousLogActivity.value = value;
        //                                previousLogActivity.read = read;

        //                                duplicateCount = 0;
        //                            }
        //                        }
        //                        else
        //                        {
        //                            previousLogActivity.whichRegister = whichRegister;
        //                            previousLogActivity.value = value;
        //                            previousLogActivity.read = read;

        //                            logIt = true;
        //                        }
        //                    }
        //                }
        //            }

        //            if (logIt)
        //            {
        //                using (StreamWriter sw = new StreamWriter(File.Open(Program.activityLogFileFD2, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)))
        //                {
        //                    string description = "Unknown";

        //                    if (registerDescription.ContainsKey(whichRegister))
        //                    {
        //                        description = registerDescription[whichRegister];
        //                    }
        //                    sw.WriteLine(string.Format("{0}  {1} - {2} {3} (0x{4}) - {5}", Program._cpu.CurrentIP.ToString("X4"), read ? "read " : "write", value.ToString("X2"), read ? "from" : "to  ", whichRegister.ToString("X4"), description));
        //                }
        //            }
        //        }
        //    }
        //}

        // uses the values in track and sector regists alomg with floppy format to calculate the offset to any sector
        //
        //      keep in mind that FLEX and UbiFLEX sectors start at 1 except for track 0 which has a sector 0 and no sector 1. 

        // used by read track to calculate ID and Data CRC values

        ushort CRCCCITT(byte[] data, int startIndex, int length, ushort seed, ushort final)
        {

            int count;
            uint crc = seed;
            uint temp;
            int dataindex = startIndex;

            for (count = 0; count < length; ++count)
            {
                temp = (data[dataindex++] ^ (crc >> 8)) & 0xff;
                crc = crc_table[temp] ^ (crc << 8);
            }

            return (ushort)(crc ^ final);
        }

        //  This is currently only used by DMA Write Track. Polled I/O write track is not yet tested
        //  and may eventually use this code also.
        //
        //      Enter with the bytes sent to the controller either by the DMA chip or polled I/O
        //
        //          The writeTrackWriteBuffer is used as the source for the data, and the
        //          un-interleaved sectors are written to m_caWriteBuffer which is the 
        //          buffer used to write the track to the diskette image.

        private void UnInterleaveWriteTrackBufferToBuffer (int cnt)
        {
            //using (BinaryWriter x = new BinaryWriter(File.Open(@"D:\writeTrackBuffer.bin", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite)))
            //{
            //    x.Write(writeTrackWriteBuffer, 0, cnt);
            //}

            int writeTrackBufferIndex = 0;

            writeTrackBufferOffset = 0;
            writeTrackBufferSize = 0;
            sectorsInWriteTrackBuffer = 0;
            totalBytesThisTrack = 0;

            for (int i = 0; i < cnt; i++)
            {
                byte b = writeTrackWriteBuffer[i];

                switch (currentWriteTrackState)
                {
                    case (int)WRITE_TRACK_STATES.WaitForIDRecordMark:
                        {
                            if (b == 0xFE)
                            {
                                // we are about to write the IDRecord mark, so get ready to save the track, side, sector and size information

                                currentWriteTrackState = (int)WRITE_TRACK_STATES.WaitForIDRecordTrack;
                            }
                        }
                        break;

                    case (int)WRITE_TRACK_STATES.WaitForIDRecordTrack:
                        {
                            // we will only allow real diskette image number of tracks (up to 80 tracks)

                            if (b < 80)
                            {
                                // this is a valid max track size

                                writeTrackTrack = b;
                                currentWriteTrackState = (int)WRITE_TRACK_STATES.WaitForIDRecordSide;
                            }
                            else
                            {
                                // if the track specified is > 79 then reset the state machine to look for another ID record mark

                                currentWriteTrackState = (int)WRITE_TRACK_STATES.WaitForIDRecordMark;
                            }
                        }
                        break;

                    case (int)WRITE_TRACK_STATES.WaitForIDRecordSide:
                        {
                            // we will only allow real diskette image number of sides (0 or 1)

                            if (b <= 1)
                            {
                                writeTrackSide = b;
                                currentWriteTrackState = (int)WRITE_TRACK_STATES.WaitForIDRecordSector;
                            }
                            else
                            {
                                // if the side specified is > 1 then reset the state machine to look for another ID record mark

                                currentWriteTrackState = (int)WRITE_TRACK_STATES.WaitForIDRecordMark;
                            }
                        }
                        break;

                    case (int)WRITE_TRACK_STATES.WaitForIDRecordSector:
                        {
                            // we will only allow real diskette image max number of sectors per track (up to 52 for 8" DD)

                            if (b <= 52)
                            {
                                writeTrackSector = b;
                                currentWriteTrackState = (int)WRITE_TRACK_STATES.WaitForIDRecordSize;

                                // we will need these values to write the track to the image.

                                if (b < writeTrackMinSector)
                                    writeTrackMinSector = b;
                                if (b > writeTrackMaxSector)
                                    writeTrackMaxSector = b;
                            }
                            else
                            {
                                // if the sector specified is > 52 then reset the state machine to look for another ID record mark

                                currentWriteTrackState = (int)WRITE_TRACK_STATES.WaitForIDRecordMark;
                            }
                        }
                        break;

                    case (int)WRITE_TRACK_STATES.WaitForIDRecordSize:
                        {
                            // we will only allow real diskette image record sizes (0, 1, 2, 3)

                            if (b <= 3)
                            {
                                writeTrackSize = b;
                                currentWriteTrackState = (int)WRITE_TRACK_STATES.WaitForIDRecordWriteCRC;
                            }
                            else
                            {
                                // if the sector record size is > 3 then reset the state machine to look for another ID record mark

                                currentWriteTrackState = (int)WRITE_TRACK_STATES.WaitForIDRecordMark;
                            }
                        }
                        break;

                    case (int)WRITE_TRACK_STATES.WaitForIDRecordWriteCRC:
                        {
                            // this next byte MUST be 0xF7 to write CRC

                            if (b == 0xF7)
                            {
                                currentWriteTrackState = (int)WRITE_TRACK_STATES.WaitForDataRecordMark;
                            }
                            else
                            {
                                // if the write CRC byte is not 0xF7 then reset the state machine to look for another ID record mark

                                currentWriteTrackState = (int)WRITE_TRACK_STATES.WaitForIDRecordMark;
                            }
                        }
                        break;

                    case (int)WRITE_TRACK_STATES.WaitForDataRecordMark:
                        {
                            if (b == 0xFB)
                            {
                                // we have received the Data Record Mark. prepare to receive 2^size * 128 bytes of data
                                currentWriteTrackState = (int)WRITE_TRACK_STATES.GettingDataRecord;

                                // we are going to build the track in the m_caWriteBuffer. We need to use the sector number (FLEX sectors start at 1 except for track 0
                                // which starts at sector 0 and has no sector 1.
                                //
                                //  Let's do this calcuation here so we only have to do it once for each sector. We can only write to one head at a time
                                //  so we need to figure out which side this is on so we can adjust the sector number we use for calculating the offset.
                                //  If the track is - and the sector number is > 10, we are doing the second side so we need to subtract 10 fomr the sector
                                //  number to calculate the buffer offset (writeTrackBufferOffset). This value will also be used to set the number of bytes
                                //  to write to the file when this track is written to the image. If the track is not track 0, we need to subtract the number
                                //  of sectors per side for the second side.
                                //
                                //      This should take care of de-interleaving the track as well.

                                int sector = writeTrackSector;
                                if (writeTrackTrack == 0 && writeTrackSector == 0)
                                {
                                    // adjust track 0 sector 0, because we are going to subtract 1 from it later, so it CANNOT be zero

                                    sector = 1;
                                }

                                writeTrackBytesPerSector = 128 * (1 << writeTrackSize);

                                writeTrackBufferIndex = writeTrackBytesPerSector * (sector - 1);
                                writeTrackBytesWrittenToSector = 0;
                            }
                        }
                        break;

                    case (int)WRITE_TRACK_STATES.GettingDataRecord:
                        {
                            if (writeTrackBytesWrittenToSector < writeTrackBytesPerSector)
                            {
                                // still writing bytes to the sector

                                // we use writeTrackBufferIndex because the sectors may be interleaved.
                                // this value gets reset to where the sector should reside in the buffer
                                // based on the sector value in the IDRecord

                                m_caWriteBuffer[writeTrackBufferIndex] = b;
                                writeTrackBufferIndex++;
                                writeTrackBytesWrittenToSector++;

                                totalBytesThisTrack++;  // added a byte to the buffer to write to the image

                                // keep track of the max size of the buffer we need to write.

                                if (writeTrackBufferSize < writeTrackBufferIndex)
                                    writeTrackBufferSize = writeTrackBufferIndex;
                            }
                            else
                            {
                                // we are done writing this sector - set up for the next one - we will just ignore the writing of the CRC bytes
                                // and set the next state to get the next ID record mark.

                                currentWriteTrackState = (int)WRITE_TRACK_STATES.WaitForIDRecordMark;

                                // increment the number of sectors in the m_caWriteBuffer

                                sectorsInWriteTrackBuffer++;
                            }
                        }
                        break;

                    case (int)WRITE_TRACK_STATES.GetLastFewBytes:
                        {
                            lastFewBytesRead++;
                        }
                        break;
                }
            }
        }

        // this is only used by Polled I/O Write Track

        private void WriteTrackToImage(int nDrive)
        {
            // There is data in the m_caWriteBuffer that needs to written to the diskette image. To do this we need to use the
            // information in the writeTrackID record to figure out where to write this buffer to.

            // writeTrackBufferSize is equal to how much data we need to save from m_caWriteBuffer

            // calculate where to save it. The offset into the files is 
            //
            //      first save the m_nFDCTrack and the m_nFDCSector because we need to 
            //      temporarily modify them to call CalcFileOffset

            int saveFDCTrack = m_nFDCTrack;
            int saveFDCSector = m_nFDCSector;

            m_nFDCTrack = writeTrackTrack;

            // Since CalcFileOffset depends on the FLEX convention of just incrementing the sector number
            // across sides and let's the disk driver sort out which side it is on, We need to be able to
            // interject this rule here.

            m_nFDCSector = writeTrackMinSector;

            CalcFileOffset(nDrive);
            LogFloppyWrite(m_lFileOffset, "writing Track", writeTrackWriteBufferIndex);

            // m_lFileOffset is now equal to the starting offset into the diskette image file of this track
            // so we can now save it to the file.

            int bytesToWrite = totalBytesThisTrack;

            if (Program.FloppyDriveStream[nDrive] != null)
            {
                Program.FloppyDriveStream[nDrive].Seek(m_lFileOffset, SeekOrigin.Begin);

                int caBufferOffset = 0;
                int writeTrackWriteBufferOffset = 0;

                if (m_nCurrentSideSelected == 1)
                {
                    caBufferOffset = writeTrackBufferSize - totalBytesThisTrack;
                }

                Program.FloppyDriveStream[nDrive].Write(m_caWriteBuffer, caBufferOffset, totalBytesThisTrack);

                LogFloppyWriteTrack(m_lFileOffset, writeTrackWriteBuffer, writeTrackWriteBufferOffset, writeTrackWriteBufferIndex);

                Program.FloppyDriveStream[nDrive].Flush();

                m_statusReadsWithoutDataRegisterAccess = 0;     // clear it for the next operation
                writeTrackBufferSize = 0;
                //totalBytesThisTrack = 0;    // reset after writing track to image file
                writeTrackWriteBufferIndex = 0;

                writeTrackMinSector = 0xff;
                writeTrackMaxSector = 0;
            }

            m_nFDCTrack = saveFDCTrack;
            m_nFDCSector = saveFDCSector;

            // and then we need to turn off writing track and writing and set the FDC stats to finished.

            lock (Program._cpu.buildingDebugLineLock)
            {
                unchecked
                {
                    m_DMAF_STATRegister &= (byte)~DMAF_StatusLines.DMAF_BUSY;     // clear BUSY if data not read
                }
                //activityLedColor = (int)ActivityLedColors.greydot;
                //Program._mainForm.SetFloppyActivity(nDrive, global::SWTPCemuApp.Properties.Resources.greydot);
                ClearInterrupt();
            }

            //totalBytesThisTrack = 0;    // reset before leaving routine to write track to image file
            m_nWritingTrack = false;
        }

        #endregion

        // call this to log any activity to any register on the DMAF-2
        //
        //      call with the register being read from or written to, the byte
        //      being read or written and a bool to indicate whether this is a
        //      read or a write operation (default if not specified = read)

        class LogActivityValues
        {
            public ushort instructionPointer;
            public ushort whichRegister;
            public byte value;
            public bool read;
        }

        LogActivityValues previousLogActivity = null;
        int duplicateCount = 0;

        private void LogDMAFActivity(ushort whichRegister, byte value, bool read = true)
        {
            if (Program.enableDMAF2ActivityLogChecked)
            {
                LogActivityValues thisLogActivityValues = new LogActivityValues();
                thisLogActivityValues.instructionPointer = Program._cpu.CurrentIP;
                thisLogActivityValues.whichRegister = whichRegister;
                thisLogActivityValues.value = value;
                thisLogActivityValues.read = read;

                bool thisIsTheFirst = false;
                bool logIt = false;

                if (previousLogActivity == null)
                {
                    previousLogActivity = new LogActivityValues();
                    previousLogActivity.instructionPointer = Program._cpu.CurrentIP;
                    previousLogActivity.whichRegister = whichRegister;
                    previousLogActivity.value = value;
                    previousLogActivity.read = read;

                    thisIsTheFirst = true;
                    logIt = true;
                }
                else
                {
                    // if this is not the first entry - do not check for duplicate

                    if (!thisIsTheFirst)
                    {
                        if (previousLogActivity.instructionPointer == thisLogActivityValues.instructionPointer && previousLogActivity.whichRegister == thisLogActivityValues.whichRegister && previousLogActivity.value == thisLogActivityValues.value && previousLogActivity.read == thisLogActivityValues.read)
                        {
                            // this is a duplicate - increment the counter
                            duplicateCount++;
                        }
                        else
                        {
                            if (duplicateCount != 0)
                            {
                                // we have had duplicates - show it

                                using (StreamWriter sw = new StreamWriter(File.Open(Program.activityLogFileDMAF2, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)))
                                {
                                    sw.WriteLine(string.Format(" The above line was duplicated {0} time", duplicateCount));

                                    previousLogActivity.instructionPointer = Program._cpu.CurrentIP;
                                    previousLogActivity.whichRegister = whichRegister;
                                    previousLogActivity.value = value;
                                    previousLogActivity.read = read;

                                    duplicateCount = 0;
                                }
                            }
                            else
                            {
                                previousLogActivity.instructionPointer = Program._cpu.CurrentIP; ;
                                previousLogActivity.whichRegister = whichRegister;
                                previousLogActivity.value = value;
                                previousLogActivity.read = read;

                                logIt = true;
                            }
                        }
                    }
                }

                if (logIt)
                {
                    using (StreamWriter sw = new StreamWriter(File.Open(Program.activityLogFileDMAF2, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)))
                    {
                        string description = "Unknown";

                        if (registerDescription.ContainsKey(whichRegister))
                        {
                            description = registerDescription[whichRegister];
                        }
                        sw.WriteLine(string.Format("{0}: {1} - {2} {3} (0x{4}) - {5}", Program._cpu.CurrentIP.ToString("X4"), read ? "read " : "write", value.ToString("X2"), read ? "from" : "to  ", whichRegister.ToString("X4"), description));
                    }
                }
            }
        }

        // when the timer fires, set the interrupt
        public void DMAF_InterruptDelayTimerProc(object state, MicroTimerEventArgs timerEventArgs)
        {
            _DMAFInterruptDelayTimer.Stop();
            SetInterrupt(_spin);
        }

        //public void DMAF_InterruptDelayTimerProc(object state)
        //{
        //    SetInterrupt();
        //}
        // OK to set the interrupt now - special processing for DMAF since there are so many interrupt sources.

        public override void SetInterrupt(bool spin)
        {
            base.SetInterrupt(spin);

            if ((m_nInterruptingDevice & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA1) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA1)
            {
                m_nInterruptingDevice &= ~(int)DeviceInterruptMask.DEVICE_INT_MASK_DMA1;

                m_DMAF_DMA_CHANNELRegister[0] |= 0x80;     // say channel is interrupting
                if (((m_DMAF_DMA_INTERRUPTRegister & 0x01) == 0x01) && ((m_DMAF_DMA_CHANNELRegister[0] & 0x80) == 0x80))
                    m_DMAF_DMA_INTERRUPTRegister |= 0x80;
            }
            if ((m_nInterruptingDevice & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA2) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA2)
            {
                m_nInterruptingDevice &= ~(int)DeviceInterruptMask.DEVICE_INT_MASK_DMA2;

                m_DMAF_DMA_CHANNELRegister[1] |= 0x80;     // say channel is interrupting
                if (((m_DMAF_DMA_INTERRUPTRegister & 0x02) == 0x02) && ((m_DMAF_DMA_CHANNELRegister[1] & 0x80) == 0x80))
                    m_DMAF_DMA_INTERRUPTRegister |= 0x80;
            }
            if ((m_nInterruptingDevice & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA3) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA3)
            {
                m_nInterruptingDevice &= ~(int)DeviceInterruptMask.DEVICE_INT_MASK_DMA3;

                m_DMAF_DMA_CHANNELRegister[2] |= 0x80;     // say channel is interrupting
                if (((m_DMAF_DMA_INTERRUPTRegister & 0x04) == 0x04) && ((m_DMAF_DMA_CHANNELRegister[2] & 0x80) == 0x80))
                    m_DMAF_DMA_INTERRUPTRegister |= 0x80;
            }
            if ((m_nInterruptingDevice & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA4) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA4)
            {
                m_nInterruptingDevice &= ~(int)DeviceInterruptMask.DEVICE_INT_MASK_DMA4;

                m_DMAF_DMA_CHANNELRegister[3] |= 0x80;     // say channel is interrupting
                if (((m_DMAF_DMA_INTERRUPTRegister & 0x08) == 0x08) && ((m_DMAF_DMA_CHANNELRegister[3] & 0x80) == 0x80))
                    m_DMAF_DMA_INTERRUPTRegister |= 0x80;
            }
            if ((m_nInterruptingDevice & (int)DeviceInterruptMask.DEVICE_INT_MASK_FDC) == (int)DeviceInterruptMask.DEVICE_INT_MASK_FDC)
            {
                m_nInterruptingDevice &= ~(int)DeviceInterruptMask.DEVICE_INT_MASK_FDC;
                #region VIA description
                //
                //  $F040   VIA_BDATA   bit 7   
                //                          6   
                //                          5   Archive Exception if 0
                //                          4   
                //                          3   
                //                          2   WD1797 is interrupting
                //                          1   
                //                          0   
                //
                //  VIA Interrupt Flag Register
                //
                //  $F04D   VIA_IFR     bit 7   device is interrupting through VIA if 1 (must be 1 to interrupt)
                //                          6   
                //                          5   Archive Timer 2 is interrupting
                //                          4   
                //                          3   write a 1 to reset CB 2 interrupt
                //                          2   
                //                          1   
                //                          0   
                //
                //  VIA Interrupt Enable Register (used by software to mask interrupts from devices)
                //
                //  $F04E   VIA_IER     bit 7   
                //                          6   
                //                          5   Archive Timer 2 interrupt enable bit
                //                          4   
                //                          3   Board interrupt enable bit (CB2 from VIA)
                //                          2   
                //                          1   
                //                          0   
                //
                //          lda     VIA+IFR         read VIA flags                            A50B B6      F04D LDA A $F04D
                //          bpl     not_via                                                   A50E 2A        27 BPL   $27  
                //          ldb     VIA+BDATA       read the DMF3 status lines                A510 F6      F040 LDA B $F040
                //          anda    VIA+IER                                                   A513 B4      F04E AND A $F04E
                //          bita    #%00100000      Archive timer 2?                          A516 85        20 BIT A #$20 
                //          lbne    ATint                                                     A518 1026    04C4 LBNE  $04C4
                //          bita    #%00001000      check for board int                       A51C 85        08 BIT A #$08 
                //          beq     via_err                                                   A51E 27        14 BEQ   $14  
                //          lda     #%00001000      clear the CB2 interrupt                   A520 86        08 LDA A #$08 
                //          sta     VIA+IFR         by stuffing the flag register             A522 B7      F04D STA A $F04D
                //          bitb    #%00100000      check for archive exception               A525 C5        20 BIT B #$20 
                //          lbeq    ATint                                                     A527 1027    04B5 LBEQ  $04B5
                //          bitb    #%00000100      look only at 1797 irq line                A52B C5        04 BIT B #$04 
                //          lbne    dmf3int         yea! branch if good disk irq              A52D 1026    F592 LBNE  $F592
                //          jmp     w5int           must be WD1000 completion interrupt                  
                //via_err   sta     VIA+IFR         reset spurious int                        A534 B7      F04D STA A $F04D
                //not_via   
                //irqha1    ldx     #inttab         point to table                            A537 8E      94AC LDX   #$94AC
                #endregion
                //DMAF_AT_IFRRegister |= 0x88;       // signal board interrupt through VIA Status register
                //DMAF_AT_DTBRegister |= 0x04;       // say it's the WD179x that's doing the interrupting
                //DMAF_AT_DTBRegister |= 0x20;       // clear the Archive Exception bit
            }

            if ((m_nInterruptingDevice & (int)DeviceInterruptMask.DEVICE_INT_MASK_WDC) == (int)DeviceInterruptMask.DEVICE_INT_MASK_WDC)
            {
                m_nInterruptingDevice &= ~(int)DeviceInterruptMask.DEVICE_INT_MASK_WDC;

                int nDrive = 0;
                switch (m_DMAF_DRVRegister & 0x0F)
                {
                    case 1: nDrive = 0; break;
                    case 2: nDrive = 1; break;
                    case 4: nDrive = 2; break;
                    case 8: nDrive = 3; break;
                }
            }
        }

        ulong LogicalToPhysicalAddress(byte latch, ushort sLogicalAddress)
        {
            return ((ulong)(latch & (byte)DMAF_ContollerLines.DMAF_EXTADDR_SELECT_LINES) << 16) + (ulong)sLogicalAddress;
        }

        int CalcFileOffset(int nDrive)
        {
            int nStatus = 0;
            int nSectorSize = 512;

            if (m_nFDCSector <= Program.SectorsPerTrack[nDrive])
            {
                if (m_nFDCTrack < Program.NumberOfTracks[nDrive])
                {
                    switch (Program.DiskFormat[nDrive])
                    {
                        case DiskFormats.DISK_FORMAT_UNIFLEX:
                            nSectorSize = 512;
                            break;
                        case DiskFormats.DISK_FORMAT_MINIFLEX:
                            nSectorSize = 128;
                            break;
                        default:
                            nSectorSize = 256;
                            break;
                    }

                    if ((m_nFDCTrack == 0) && (m_nFDCSector == 0) && (m_nCurrentSideSelected == 0))
                        m_lFileOffset = 0L;
                    else
                    {
                        if ((Program.DiskFormat[nDrive] == DiskFormats.DISK_FORMAT_FLEX) || (Program.DiskFormat[nDrive] == DiskFormats.DISK_FORMAT_UNIFLEX) || (Program.DiskFormat[nDrive] == DiskFormats.DISK_FORMAT_MINIFLEX))
                        {
                            if (m_nFDCSector == 0)
                                m_nFDCSector = 1;

                            long lBlock = (long)((m_nFDCTrack * Program.SectorsPerTrack[nDrive]) + (m_nFDCSector - 1));
                            m_lFileOffset = lBlock * (long)nSectorSize;
                        }
                        else if (Program.DiskFormat[nDrive] == DiskFormats.DISK_FORMAT_FLEX_IMA)
                        {
                            // this will need some work for real .IMA images with different number of sectors on track zero

                            int sectorPerTrack = Program.SectorsPerTrack[nDrive];
                            int sectorsOnTrack0 = Program.SectorsOnTrackZero[nDrive];

                            // if track 0 - this will take care of itself

                            long lBlock = (long)((m_nFDCTrack * Program.SectorsPerTrack[nDrive]) + (m_nFDCSector - 1));

                            // if this image has a different number of sectors on track than the rest of the image
                            // we need to do some extra calculations - unless we are on track 0

                            if (sectorPerTrack != sectorsOnTrack0 && m_nFDCTrack != 0)
                            {
                                lBlock = ((((m_nFDCTrack - 1) * (Program.SectorsPerTrack[nDrive])) + (m_nFDCSector - 1))) + sectorsOnTrack0;
                            }

                            m_lFileOffset = lBlock * (long)nSectorSize;
                        }
                        else if (Program.DiskFormat[nDrive] == DiskFormats.DISK_FORMAT_OS9)
                        {
                            int nSPT = Program.SectorsPerTrack[nDrive];
                            int nFormat = Program.FormatByte[nDrive];

                            //  OS9 ALWAYS has m_nSectorsOnTrackZero sectors on track 0 side 0
                            //  and uses the side select before seeking to another cylinder.
                            //  track 0 side 1 has m_nSectorsPerTrack sectors.

                            if (m_nFDCTrack == 0)
                                m_lFileOffset = (long)(m_nFDCSector) * (long)nSectorSize;
                            else
                            {
                                // start off with track 0 side 1 already added in if it's double sided. 
                                // We'll get to track 0 side 0 sectors later

                                if ((nFormat & 0x0001) != 0)   // double sided
                                {
                                    m_lFileOffset = (long)nSPT * (long)nSectorSize;

                                    // is this is not side 0, add in this tracks side 0 sectors

                                    if (m_nCurrentSideSelected != 0)
                                        m_lFileOffset += (long)nSPT * (long)nSectorSize;

                                    // the rest of the math is done on cylinders

                                    nSPT *= 2;
                                }
                                else
                                    m_lFileOffset = 0L;

                                // now add in all of the previous tracks sectors (except track 0)

                                m_lFileOffset += (long)(((m_nFDCTrack - 1) * nSPT) + m_nFDCSector) * (long)nSectorSize;
                            }

                            // if not track 0 side 0 - add in track 0 side 0 sectors 
                            // because it may have a different number of sectors

                            if (!((m_nFDCTrack == 0) && (m_nCurrentSideSelected == 0)))
                            {
                                m_lFileOffset += (long)Program.SectorsOnTrackZeroSideZero[nDrive] * (long)nSectorSize;
                            }
                        }
                    }
                }
                else
                    nStatus = (int)DMAF_StatusCodes.DMAF_RNF; // RECORD NOT FOUND
            }
            else
                nStatus = (int)DMAF_StatusCodes.DMAF_RNF; // RECORD NOT FOUND

            return nStatus;
        }

        int readBufferIndex = 0;

        void WriteFloppy(ushort m, byte b)
        {
            int nWhichRegister = m - m_sBaseAddress;

            ClearInterrupt();

            int nDrive = 0;
            int nSectorSize = 256;

            switch (m_DMAF_DRVRegister & 0x0F)
            {
                case 1:
                    nDrive = 0;
                    break;
                case 2:
                    nDrive = 1;
                    break;
                case 4:
                    nDrive = 2;
                    break;
                case 8:
                    nDrive = 3;
                    break;
            }

            int nType = 1;
            bool nMultisector;

            switch (Program.DiskFormat[nDrive])
            {
                case DiskFormats.DISK_FORMAT_UNIFLEX:
                    nSectorSize = 512;
                    break;
                default:
                    nSectorSize = 256;
                    break;
            }

            int activechannel = -1;

            // handle the interrupts in priority order - floppy is on channel 0 so it gets the highest priority

            switch (m_DMAF_DMA_PRIORITYRegister & 0x0F)
            {
                case 1:
                    activechannel = 0;      // Floppy
                    break;
                case 2:
                    activechannel = 1;      // WD1000
                    break;
                case 4:
                    activechannel = 2;
                    //AfxMessageBox ("DMA channel set to 2 - not supported");
                    break;
                case 8:
                    activechannel = 3;
                    //AfxMessageBox ("DMA channel set to 3 - not supported");
                    break;
            }

            byte latch = m_DMAF_LATCHRegister;
            ushort addr = 0;
            ushort cnt = 0;
            byte priority = m_DMAF_DMA_PRIORITYRegister;
            byte interrupt = m_DMAF_DMA_INTERRUPTRegister;
            byte chain = m_DMAF_DMA_CHAINRegister;

            if (activechannel == 0)
            {
                addr = (ushort)(m_DMAF_DMA_ADDRESSRegister[activechannel].hibyte * 256 + m_DMAF_DMA_ADDRESSRegister[activechannel].lobyte);
                cnt = (ushort)(m_DMAF_DMA_DMCNTRegister[activechannel].hibyte * 256 + m_DMAF_DMA_DMCNTRegister[activechannel].lobyte);
            }

            switch ((DMAF_OffsetRegisters)nWhichRegister)
            {
                case DMAF_OffsetRegisters.DMAF_DATAREG_OFFSET:                      // 0xF023
                    {
                        m_DMAF_DATARegister = b;
                        if (((m_DMAF_STATRegister & (byte)DMAF_StatusLines.DMAF_BUSY) == (byte)DMAF_StatusLines.DMAF_BUSY) && m_nFDCWriting)
                        {
                            m_statusReadsWithoutDataRegisterAccess = 0;

                            if (!m_nWritingTrack)
                            {
                                m_caWriteBuffer[m_nFDCWritePtr++] = b;
                                if (m_nFDCWritePtr == m_nBytesToTransfer)
                                {
                                    // Here is where we will seek and write dsk file

                                    int nStatus = CalcFileOffset(nDrive);
                                    if (nStatus == 0)
                                    {
                                        if (Program.FloppyDriveStream[nDrive] != null)
                                        {
                                            Program.FloppyDriveStream[nDrive].Seek(m_lFileOffset, SeekOrigin.Begin);
                                            Program.FloppyDriveStream[nDrive].Write(m_caWriteBuffer, 0, m_nBytesToTransfer);
                                            Program.FloppyDriveStream[nDrive].Flush();
                                        }

                                        //Program._mainForm.SetFloppyActivity(nDrive, global::SWTPCemuApp.Properties.Resources.greydot);

                                        m_DMAF_STATRegister &= (byte)(~((byte)DMAF_StatusLines.DMAF_DRQ | (byte)DMAF_StatusLines.DMAF_BUSY) & 0xFF);

                                        // turn off high order bit in drive status register

                                        m_DMAF_DRVRegister &= (byte)(~(byte)DMAF_ContollerLines.DRV_DRQ & (byte)0xFF);
                                        m_nFDCWriting = false;
                                    }
                                    else
                                        m_DMAF_STATRegister |= (byte)DMAF_StatusCodes.DMAF_RNF;        // RECORD NOT FOUND
                                }
                                else
                                {
                                    m_DMAF_STATRegister |= (byte)DMAF_StatusLines.DMAF_DRQ;
                                    m_DMAF_DRVRegister |= (byte)DMAF_ContollerLines.DRV_DRQ;

                                    if (_bInterruptEnabled && (m_DMAF_LATCHRegister & 0x10) == 0x10)
                                    {
                                        // the latch at F040 : (one's complement data sent to controller)
                                        //
                                        //      bit 0..3    is A16...A19 for DMA.
                                        //      bit 4       is INT enable for the FDC..
                                        //                      If set to '1' the FDC INT pin triggers an /IRQ
                                        //                      if this bit get written as a 1, it is stored as a 0
                                        //                      so having a 0 in the latch disables interrupts

                                        StartDMAFInterruptDelayTimer(m_nInterruptDelay, (int)DeviceInterruptMask.DEVICE_INT_MASK_FDC);
                                        if (Program._cpu != null)
                                        {
                                            if ((Program._cpu.InWait || Program._cpu.InSync) && Program.CpuThread.ThreadState == System.Threading.ThreadState.Suspended)
                                            {
                                                try
                                                {
                                                    Program.CpuThread.Resume();
                                                }
                                                catch (ThreadStateException e)
                                                {
                                                    // do nothing if thread is not suspended
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // This will only be used for Polled write track - NOT DMA write track

                                //  !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                                //  !!!!!!!!!!!!!! THIS NEEDS SOME SERIOUS WORK !!!!!!!!!!!!!! 
                                //  !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                                //  !!!!!!!!!!!!!!   IT PROBABLY DOES NOT WORK  !!!!!!!!!!!!!!
                                //  !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

                                // We probably need to just build the writeTrackWriteBuffer and let the UnInterleaveWriteTrackBufferToBuffer
                                // function do all the work, then copy code from the DMA write track code to do the actual write. That way 
                                // we will have common code to run the state machine to get the actual sector data in the proper order into
                                // the buffer we use to write to the image.

                                // this is where we will use a state machine to grab the track, side and sector information from the
                                // incoming data stream by detecting and parsing the ID Record and then wait for the data record to
                                // start building the track's worth of data to save to the image.

                                //      300 rpm SD      3125 bytes
                                //      300 rpm DD/QD   6250 bytes
                                //      360 rpm SD      5208 bytes (e.g., 8" drive)
                                //      360 rpm DD     10417 bytes (e.g., 8" drive)

                                // first a sanity check to keep from going on forever and ever

                                if (writeTrackWriteBufferIndex > 10417)
                                {
                                    // we should be done at this point so we need to write the track to the diskette image in case the
                                    // emulated host does not check the BUSY or DRQ for writing the next byte. Some emulated machine
                                    // programs that use write track do not poll the DRQ or BUSY bits - they just keep sending data
                                    // until an an interrupt occurs when the index pulse is detected. For these - we need to terminate
                                    // the write track operation by raising the IRQ ourselces. Since the status register is not 
                                    // being polled (which is where we would write the track to the image), we need to do it here

                                    m_statusReadsWithoutDataRegisterAccess = 1000;  // make it big enough to trigger the end

                                    // write the track to the image and clear everything so the next poll of the status register does not write anything.

                                    WriteTrackToImage(nDrive);

                                    if (_bInterruptEnabled)// && (m_DMAF_DRVRegister & (byte)DMAF_ContollerLines.DRV_INTRQ) == (byte)DMAF_ContollerLines.DRV_INTRQ)
                                    {
                                        SetInterrupt(_spin);
                                        if (Program._cpu != null)
                                        {
                                            if ((Program._cpu.InWait || Program._cpu.InSync) && Program.CpuThread.ThreadState == System.Threading.ThreadState.Suspended)
                                            {
                                                try
                                                {
                                                    Program.CpuThread.Resume();
                                                }
                                                catch (ThreadStateException e)
                                                {
                                                    // do nothing if thread is not suspended
                                                }
                                            }
                                        }
                                    }
                                    writeTrackWriteBufferIndex = 0;     // rest it for next time
                                }
                                else
                                {
                                    // not at max number of bytes for an 8" DD track - so check if this track is finished.

                                    if (writeTrackTrack == 0)
                                    {
                                        // track zero for an DMAF-2 is only allowed to have FM data (single density. that is 15 sectors for FLEX_IMA)
                                        //
                                        // we peobably need to adjust this code to check if we are double sided and use that information to see how
                                        // many sectors we are expecting in sectorsInWriteTrackBuffer 

                                        if (sectorsInWriteTrackBuffer == Program.SectorsOnTrackZero[nDrive])
                                        {
                                            // we just got the last 0xF7 to write the data CRC for the last sector, so let's get a few more bytes before we say we are complete

                                            currentWriteTrackState = (int)WRITE_TRACK_STATES.GetLastFewBytes;

                                            if (lastFewBytesRead == 7)
                                            {
                                                m_statusReadsWithoutDataRegisterAccess = 1000;  // make it big enough to trigger the end

                                                // write the track to the image and clear everything so the next poll of the status register does not write anything.

                                                WriteTrackToImage(nDrive);
                                                currentWriteTrackState = (int)WRITE_TRACK_STATES.WaitForIDRecordMark;

                                                if (_bInterruptEnabled) // && (m_DMAF_DRVRegister & (byte)DMAF_StatusLines.DRV_INTRQ) == (byte)DMAF_StatusLines.DRV_INTRQ)
                                                {
                                                    SetInterrupt(_spin);
                                                    if (Program._cpu != null)
                                                    {
                                                        if ((Program._cpu.InWait || Program._cpu.InSync) && Program.CpuThread.ThreadState == System.Threading.ThreadState.Suspended)
                                                        {
                                                            try
                                                            {
                                                                Program.CpuThread.Resume();
                                                            }
                                                            catch (ThreadStateException e)
                                                            {
                                                                // do nothing if thread is not suspended
                                                            }
                                                        }
                                                    }
                                                }
                                                writeTrackWriteBufferIndex = 0;     // rest it for next time
                                            }
                                        }
                                    }
                                    else
                                    {
                                        //  here we need to use the sectors per track for this media that was read from offset 0x0227 in the image
                                        //  when the diskette image was loaded by Program.LoadDrives
                                        //
                                        //      For real media for the DMAF-2 controller these are the only legitimate sectors sizes
                                        //      on a 8":
                                        //
                                        //              Track 0     NON Track zero  Program.SectorsPerTrack[nDrive];
                                        //      SSSD    15          15              15
                                        //      SSDD    15          26              26
                                        //      DSSD    30          15              30
                                        //      DSDD    30          26              52
                                        //

                                        int numberOfSectorsPerCylinder = Program.SectorsPerTrack[nDrive];
                                        int numberOfSectorsPerTrack = numberOfSectorsPerCylinder;

                                        if (Program.IsDoubleSided[nDrive] == 1)
                                        {
                                            // then this is a double sided diskette image so we will only be writing
                                            // half the number of sectors specified in Program.SectorsPerTrack[nDrive]

                                            numberOfSectorsPerTrack = numberOfSectorsPerTrack / 2;
                                        }

                                        if (sectorsInWriteTrackBuffer == numberOfSectorsPerTrack)
                                        {
                                            // we just got the last 0xF7 to write the data CRC for the last sector, so let's get a few more bytes before we say we are complete

                                            currentWriteTrackState = (int)WRITE_TRACK_STATES.GetLastFewBytes;

                                            if (lastFewBytesRead == 7)
                                            {
                                                m_statusReadsWithoutDataRegisterAccess = 1000;  // make it big enough to trigger the end

                                                // write the track to the image and clear everything so the next poll of the status register does not write anything.

                                                WriteTrackToImage(nDrive);
                                                currentWriteTrackState = (int)WRITE_TRACK_STATES.WaitForIDRecordMark;

                                                if (_bInterruptEnabled) // && (m_FDC_DRVRegister & (byte)DRV_INTRQ) == (byte)DRV_INTRQ)
                                                {
                                                    SetInterrupt(_spin);
                                                    if (Program._cpu != null)
                                                    {
                                                        if ((Program._cpu.InWait || Program._cpu.InSync) && Program.CpuThread.ThreadState == System.Threading.ThreadState.Suspended)
                                                        {
                                                            try
                                                            {
                                                                Program.CpuThread.Resume();
                                                            }
                                                            catch (ThreadStateException e)
                                                            {
                                                                // do nothing if thread is not suspended
                                                            }
                                                        }
                                                    }
                                                }
                                                writeTrackWriteBufferIndex = 0;     // rest it for next time
                                            }
                                        }
                                    }
                                }

                                switch (currentWriteTrackState)
                                {
                                    case (int)WRITE_TRACK_STATES.WaitForIDRecordMark:
                                        {
                                            writeTrackWriteBuffer[writeTrackWriteBufferIndex++] = b;
                                            if (b == 0xFE)
                                            {
                                                // we are about to write the IDRecord mark, so get ready to save the track, side, sector and size information

                                                currentWriteTrackState = (int)WRITE_TRACK_STATES.WaitForIDRecordTrack;
                                            }
                                        }
                                        break;

                                    case (int)WRITE_TRACK_STATES.WaitForIDRecordTrack:
                                        {
                                            // we will only allow real diskette image number of tracks (up to 80 tracks)

                                            writeTrackWriteBuffer[writeTrackWriteBufferIndex++] = b;
                                            if (b < 80)
                                            {
                                                // this is a valid max track size

                                                writeTrackTrack = b;
                                                currentWriteTrackState = (int)WRITE_TRACK_STATES.WaitForIDRecordSide;
                                            }
                                            else
                                            {
                                                // if the track specified is > 79 then reset the state machine to look for another ID record mark

                                                currentWriteTrackState = (int)WRITE_TRACK_STATES.WaitForIDRecordMark;
                                            }
                                        }
                                        break;

                                    case (int)WRITE_TRACK_STATES.WaitForIDRecordSide:
                                        {
                                            // we will only allow real diskette image number of sides (0 or 1)

                                            writeTrackWriteBuffer[writeTrackWriteBufferIndex++] = b;
                                            if (b <= 1)
                                            {
                                                writeTrackSide = b;
                                                currentWriteTrackState = (int)WRITE_TRACK_STATES.WaitForIDRecordSector;
                                            }
                                            else
                                            {
                                                // if the side specified is > 1 then reset the state machine to look for another ID record mark

                                                currentWriteTrackState = (int)WRITE_TRACK_STATES.WaitForIDRecordMark;
                                            }
                                        }
                                        break;

                                    case (int)WRITE_TRACK_STATES.WaitForIDRecordSector:
                                        {
                                            // we will only allow real diskette image max number of sectors per track (up to 52 for 8" DD)

                                            writeTrackWriteBuffer[writeTrackWriteBufferIndex++] = b;
                                            if (b <= 52)
                                            {
                                                writeTrackSector = b;
                                                currentWriteTrackState = (int)WRITE_TRACK_STATES.WaitForIDRecordSize;

                                                // we will need these values to write the track to the image.

                                                if (b < writeTrackMinSector)
                                                    writeTrackMinSector = b;
                                                if (b > writeTrackMaxSector)
                                                    writeTrackMaxSector = b;
                                            }
                                            else
                                            {
                                                // if the sector specified is > 52 then reset the state machine to look for another ID record mark

                                                currentWriteTrackState = (int)WRITE_TRACK_STATES.WaitForIDRecordMark;
                                            }
                                        }
                                        break;

                                    case (int)WRITE_TRACK_STATES.WaitForIDRecordSize:
                                        {
                                            // we will only allow real diskette image record sizes (0, 1, 2, 3)

                                            writeTrackWriteBuffer[writeTrackWriteBufferIndex++] = b;
                                            if (b <= 3)
                                            {
                                                writeTrackSize = b;
                                                currentWriteTrackState = (int)WRITE_TRACK_STATES.WaitForIDRecordWriteCRC;
                                            }
                                            else
                                            {
                                                // if the sector record size is > 3 then reset the state machine to look for another ID record mark

                                                currentWriteTrackState = (int)WRITE_TRACK_STATES.WaitForIDRecordMark;
                                            }
                                        }
                                        break;

                                    case (int)WRITE_TRACK_STATES.WaitForIDRecordWriteCRC:
                                        {
                                            // this next byte MUST be 0xF7 to write CRC

                                            writeTrackWriteBuffer[writeTrackWriteBufferIndex++] = b;
                                            if (b == 0xF7)
                                            {
                                                currentWriteTrackState = (int)WRITE_TRACK_STATES.WaitForDataRecordMark;
                                            }
                                            else
                                            {
                                                // if the write CRC byte is not 0xF7 then reset the state machine to look for another ID record mark

                                                currentWriteTrackState = (int)WRITE_TRACK_STATES.WaitForIDRecordMark;
                                            }
                                        }
                                        break;

                                    case (int)WRITE_TRACK_STATES.WaitForDataRecordMark:
                                        {
                                            writeTrackWriteBuffer[writeTrackWriteBufferIndex++] = b;
                                            if (b == 0xFB)
                                            {
                                                // we have received the Data Record Mark. prepare to receive 2^size * 128 bytes of data
                                                currentWriteTrackState = (int)WRITE_TRACK_STATES.GettingDataRecord;

                                                // we are going to build the track in the m_caWriteBuffer. We need to use the sector number (FLEX sectors start at 1 except for track 0
                                                // which starts at sector 0 and has no sector 1.
                                                //
                                                //  Let's do this calcuation here so we only have to do it once for each sector. We can only write to one head at a time
                                                //  so we need to figure out which side this is on so we can adjust the sector number we use for calculating the offset.
                                                //  If the track is - and the sector number is > 10, we are doing the second side so we need to subtract 10 fomr the sector
                                                //  number to calculate the buffer offset (writeTrackBufferOffset). This value will also be used to set the number of bytes
                                                //  to write to the file when this track is written to the image. If the track is not track 0, we need to subtract the number
                                                //  of sectors per side for the second side.

                                                int sector = writeTrackSector;
                                                if (writeTrackTrack == 0 && writeTrackSector == 0)
                                                {
                                                    // adjust track 0 sector 0, because we are going to subtract 1 from it later, so it CANNOT be zero

                                                    sector = 1;
                                                }

                                                writeTrackBytesPerSector = 128 * (1 << writeTrackSize);

                                                writeTrackBufferOffset = writeTrackBytesPerSector * (sector - 1);
                                                writeTrackBytesWrittenToSector = 0;
                                            }
                                        }
                                        break;

                                    case (int)WRITE_TRACK_STATES.GettingDataRecord:
                                        {
                                            writeTrackWriteBuffer[writeTrackWriteBufferIndex++] = b;
                                            if (writeTrackBytesWrittenToSector < writeTrackBytesPerSector)
                                            {
                                                // still writing bytes to the sector

                                                // we use writeTrackBufferOffset because the sectors may be interleaved.
                                                // this value gets reset to where the sector should reside in the buffer
                                                // based on the sector value in the IDRecord

                                                m_caWriteBuffer[writeTrackBufferOffset] = b;
                                                writeTrackBufferOffset++;
                                                writeTrackBytesWrittenToSector++;

                                                totalBytesThisTrack++;  // added a byte to the buffer to write to the image

                                                // keep track of the max size of the buffer we need to write.

                                                if (writeTrackBufferSize < writeTrackBufferOffset)
                                                    writeTrackBufferSize = writeTrackBufferOffset;
                                            }
                                            else
                                            {
                                                // we are done writing this sector - set up for the next one - we will just ignore the writing of the CRC bytes
                                                // and set the next state to get the next ID record mark.

                                                currentWriteTrackState = (int)WRITE_TRACK_STATES.WaitForIDRecordMark;

                                                // increment the number of sectors in the m_caWriteBuffer

                                                sectorsInWriteTrackBuffer++;
                                            }
                                        }
                                        break;

                                    case (int)WRITE_TRACK_STATES.GetLastFewBytes:
                                        {
                                            writeTrackWriteBuffer[writeTrackWriteBufferIndex++] = b;
                                            lastFewBytesRead++;
                                        }
                                        break;
                                }
                            }
                        }
                    }
                    break;

                case DMAF_OffsetRegisters.DMAF_DRVREG_OFFSET:                       // 0xF024
                    {
                        {
                            // does this regsiter gets inverted data (one's complement?

                            // latch at F024:
                            //      bit 0...3     xxxx xxXX   is drive select,                  
                            //          4         xxxX xxxx   is side select (0=side 0),        
                            //          5         xxXx xxxx   is dens select(0=DD),
                            //          7         Xxxx xxxx   is toggled to re-trigger the head load delay one-shot
                            m_DMAF_DRVRegister = (byte)~b;

                            if ((m_DMAF_DRVRegister & 0x10) == 0x10)        // we are selecting side 1
                                m_nCurrentSideSelected = 1;
                            else                                             // we are selecting side 0
                                m_nCurrentSideSelected = 0;

                            if ((m_DMAF_DRVRegister & 0x20) == 0x20)        // we are selecting single density 0 = double
                                m_nCurrentDensitySelected = 1;
                            else                                             // we are selecting double density
                                m_nCurrentDensitySelected = 0;
                        }
                    }
                    break;

                case DMAF_OffsetRegisters.DMAF_CMDREG_OFFSET:                       // 0xF020
                    {
                        m_DMAF_CMDRegister = b;        // save the command for later use internally

                        if (_bInterruptEnabled)
                            ClearInterrupt();

                        m_nReadingTrack = m_nWritingTrack = m_nFDCReading = m_nFDCWriting = false;      // can't be read/writing if commanding
                        m_statusReadsWithoutDataRegisterAccess = 0;

                        switch (b & 0xF0)
                        {
                            // TYPE I

                            case 0x00:  //  0x0X = RESTORE
                                m_nFDCTrack = 0;
                                m_DMAF_TRKRegister = 0;
                                break;

                            case 0x10:  //  0x1X = SEEK
                                m_nFDCTrack = m_DMAF_DATARegister;
                                m_DMAF_TRKRegister = (byte)m_nFDCTrack;
                                break;

                            case 0x20:  //  0x2X = STEP
                                break;

                            case 0x30:  //  0x3X = STEP     W/TRACK UPDATE
                                break;

                            case 0x40:  //  0x4X = STEP IN
                                if (m_nFDCTrack < 79)
                                    m_nFDCTrack++;
                                break;

                            case 0x50:  //  0x5X = STEP IN  W/TRACK UPDATE
                                if (m_nFDCTrack < 79)
                                {
                                    m_nFDCTrack++;
                                    m_DMAF_TRKRegister = (byte)m_nFDCTrack;
                                }
                                break;

                            case 0x60:  //  0x6X = STEP OUT
                                if (m_nFDCTrack > 0)
                                    --m_nFDCTrack;
                                break;

                            case 0x70:  //  0x7X = STEP OUT W/TRACK UPDATE
                                if (m_nFDCTrack > 0)
                                {
                                    --m_nFDCTrack;
                                    m_DMAF_TRKRegister = (byte)m_nFDCTrack;
                                }
                                break;

                            // TYPE II

                            case 0x80:  //  0x8X = READ  SINGLE   SECTOR
                            case 0x90:  //  0x9X = READ  MULTIPLE SECTORS
                            case 0xA0:  //  0xAX = WRITE SINGLE   SECTOR
                            case 0xB0:  //  0xBX = WRITE MULTIPLE SECTORS
                                {
                                    m_nReadingTrack = m_nWritingTrack = false;

                                    m_DMAF_STATRegister |= (byte)DMAF_StatusLines.DMAF_HDLOADED;

                                    nType = 2;
                                    m_nFDCTrack = m_DMAF_TRKRegister;

                                    nMultisector = false;
                                    m_nBytesToTransfer = nSectorSize;

                                    if ((b & 0x10) == 0x10)
                                    {
                                        if (m_nAllowMultiSectorTransfers)
                                        {
                                            nMultisector = true;
                                            m_nBytesToTransfer = (Program.SectorsPerTrack[nDrive] - m_nFDCSector + 1) * nSectorSize;
                                        }
                                    }

                                    if ((b & 0x20) == 0x20)   // WRITE
                                    {
                                        //Program._mainForm.SetFloppyActivity(nDrive, global::SWTPCemuApp.Properties.Resources.reddot);

                                        m_nFDCReading = false;
                                        m_nFDCWriting = true;
                                        m_nFDCWritePtr = 0;
                                        m_nStatusReads = 0;

                                        int nStatus = CalcFileOffset(nDrive);
                                        if (nStatus == 0)
                                        {
                                            if (Program.FloppyDriveStream[nDrive] != null)
                                            {
                                                m_DMAF_STATRegister &= (byte)(~((byte)DMAF_StatusLines.DMAF_DRQ | (byte)DMAF_StatusLines.DMAF_BUSY) & 0xFF);
                                                m_DMAF_DRVRegister &= (byte)(~((byte)DMAF_ContollerLines.DRV_DRQ) & 0xFF);        // turn off high order bit in drive status register
                                                m_nFDCWriting = false;

                                                // if the dma is active - write bytes to the disk from the buffer and reset the dma counter

                                                ulong lPhysicalAddress = LogicalToPhysicalAddress(latch, addr);
                                                if (latchRegisterIsInverted)
                                                {
                                                    lPhysicalAddress = lPhysicalAddress ^ 0x00000000000F0000;
                                                }

                                                if (activechannel == 0) // only handle channel 1 for floppy
                                                {
                                                    if (cnt > 0)
                                                    {
                                                        Program.FloppyDriveStream[nDrive].Seek(m_lFileOffset, SeekOrigin.Begin);

                                                        // see if we are doing Address Up or Down

                                                        if ((m_DMAF_DMA_CHANNELRegister[activechannel] & 0x08) == 0x08)
                                                            Program.FloppyDriveStream[nDrive].Write(Program._cpu.Memory.MemorySpace, (int)(lPhysicalAddress - cnt), cnt);
                                                        else
                                                            Program.FloppyDriveStream[nDrive].Write(Program._cpu.Memory.MemorySpace, (int)lPhysicalAddress, cnt);

                                                        Program.FloppyDriveStream[nDrive].Flush();

                                                        // see if we are doing Address Up or Down

                                                        ushort a = (ushort)(m_DMAF_DMA_ADDRESSRegister[activechannel].hibyte * 256 + m_DMAF_DMA_ADDRESSRegister[activechannel].lobyte);
                                                        if ((m_DMAF_DMA_CHANNELRegister[activechannel] & 0x08) == 0x08)
                                                            a -= cnt;
                                                        else
                                                            a += cnt;
                                                        m_DMAF_DMA_ADDRESSRegister[activechannel].hibyte = (byte)(a / 256);
                                                        m_DMAF_DMA_ADDRESSRegister[activechannel].lobyte = (byte)(a % 256);

                                                        m_DMAF_STATRegister &= (byte)(~((byte)DMAF_StatusLines.DMAF_DRQ | (byte)DMAF_StatusLines.DMAF_BUSY) & 0xFF);
                                                        m_DMAF_DRVRegister &= (byte)(~((byte)DMAF_ContollerLines.DRV_DRQ) & 0xFF);            // turn off high order bit in drive status register

                                                        m_nFDCReading = false;
                                                        m_nFDCWriting = false;
                                                    }
                                                    else
                                                    {
                                                        // must be doing polled IO

                                                        //AfxMessageBox ("Polled IO read not implemented");
                                                    }
                                                }
                                                //else
                                                //{
                                                //    if (activechannel != -1)
                                                //        //AfxMessageBox ("DMA attempting to use channel other than 0 for floppy during write");
                                                //}
                                            }
                                        }
                                        else
                                            m_DMAF_STATRegister |= (byte)DMAF_StatusCodes.DMAF_RNF;        //  RECORD NOT FOUND
                                    }
                                    else                    // READ
                                    {
                                        //Program._mainForm.SetFloppyActivity(nDrive, global::SWTPCemuApp.Properties.Resources.greendot);

                                        m_nFDCReading = true;
                                        m_nFDCWriting = false;
                                        m_nFDCReadPtr = 0;
                                        m_nStatusReads = 0;

                                        int nStatus = CalcFileOffset(nDrive);
                                        if (nStatus == 0)
                                        {
                                            if (Program.FloppyDriveStream[nDrive] != null)
                                            {
                                                Program.FloppyDriveStream[nDrive].Seek(m_lFileOffset, SeekOrigin.Begin);
                                                Program.FloppyDriveStream[nDrive].Read(m_caReadBuffer, 0, m_nBytesToTransfer);
                                                m_DMAF_DATARegister = m_caReadBuffer[0];

                                                // if the dma is active - get the bytes from memory, write them to the buffer and reset the dma counter

                                                if (activechannel == 0) // only handle channel 1 for now
                                                {
                                                    if (cnt > 0)
                                                    {
                                                        ulong lPhysicalAddress = LogicalToPhysicalAddress(latch, addr);
                                                        if (latchRegisterIsInverted)
                                                        {
                                                            lPhysicalAddress = lPhysicalAddress ^ 0x00000000000F0000;
                                                        }

                                                        for (int i = 0; i < cnt; i++)
                                                        {
                                                            if ((m_DMAF_DMA_CHANNELRegister[activechannel] & 0x08) == 0x08)
                                                                Program._cpu.Memory.MemorySpace[(int)lPhysicalAddress - i] = m_caReadBuffer[i];
                                                            else
                                                                Program._cpu.Memory.MemorySpace[(int)lPhysicalAddress + i] = m_caReadBuffer[i];
                                                        }

                                                        ushort a = (ushort)(m_DMAF_DMA_ADDRESSRegister[activechannel].hibyte * 256 + m_DMAF_DMA_ADDRESSRegister[activechannel].lobyte);
                                                        if ((m_DMAF_DMA_CHANNELRegister[activechannel] & 0x08) == 0x08)
                                                            a -= cnt;
                                                        else
                                                            a += cnt;

                                                        m_DMAF_DMA_ADDRESSRegister[activechannel].hibyte = (byte)(a / 256);
                                                        m_DMAF_DMA_ADDRESSRegister[activechannel].lobyte = (byte)(a % 256);

                                                        // turn off BUSY and DRQ in the floppy controller status register
                                                        // and DRQ in the board drive register

                                                        m_DMAF_STATRegister &= (byte)(~((byte)DMAF_StatusLines.DMAF_DRQ | (byte)DMAF_StatusLines.DMAF_BUSY) & 0xFF);
                                                        m_DMAF_DRVRegister &= (byte)(~(byte)DMAF_ContollerLines.DRV_DRQ & 0xFF);            // turn off high order bit in drive status register

                                                        m_nFDCReading = false;
                                                        m_nFDCWriting = false;
                                                    }
                                                    else
                                                    {
                                                        //AfxMessageBox ("Polled IO for write not implemented");
                                                    }
                                                }
                                                //else
                                                //{
                                                //    if (activechannel != -1)
                                                //        //AfxMessageBox ("DMA attempting to use channel other than 0 for floppy during read");
                                                //}
                                            }
                                        }
                                        else
                                            m_DMAF_STATRegister |= (byte)DMAF_StatusCodes.DMAF_RNF;        //  RECORD NOT FOUND
                                    }
                                }
                                break;

                            // TYPE III

                            case 0xC0:  //  0xCX = READ ADDRESS
                                nType = 3;
                                // Read Track is 1110010s where s is the O flag. 1 = synchronizes to address mark, 0 = do not synchronize to address mark.
                                if ((b & 0x04) == 0x04)
                                {
                                    //activityLedColor = (int)ActivityLedColors.greendot;

                                    m_nFDCReading = true;
                                    readBufferIndex = 0;

                                    m_nFDCWriting = false;
                                    m_nFDCReadPtr = 0;

                                    m_statusReadsWithoutDataRegisterAccess = 0;

                                    //
                                    // Upon receipt of the Read Track command, the head is loaded and the BUSY bit is set. Reading starts with
                                    // the leading edge of first encountered index mark and continues until the next index pulse. As each byte
                                    // is assembled it is transferred to the Data Register and the Data Request is generated for each byte. No
                                    // CRC checking is performed. Gaps are included in the input data stream. If bit O of the command regoster
                                    // is 0, the accumlation of bytes is synchronized to each Address Mark encountered. Upon completion of the
                                    // command, the interrupt is activated.
                                    //
                                    // Bit zero is the O flag. 1 = synchronizes to address mark, 0 = do not synchronize to address mark.
                                    //
                                    //      1:  set proper type, status bits and track register
                                    nType = 3;
                                    m_DMAF_STATRegister |= (byte)DMAF_StatusLines.DMAF_HDLOADED;
                                    m_nFDCTrack = m_DMAF_TRKRegister;

                                    //      2:  using the data in the track register calculate the offset to the start of the track
                                    //          and set number of bytes to transfer. The sector gets set to 0 if on track 0 and to 1
                                    //          if on any other track. The CalcFileOffset takes care of adjusting the sector number
                                    //          internally based on image file format.

                                    m_nFDCSector = m_nFDCTrack == 0 ? 0 : 1;

                                    CalcFileOffset(nDrive);

                                    m_nBytesToTransfer = m_nFDCTrack == 0 ? (Program.SectorsOnTrackZeroSideZero[nDrive]) * 256 : (Program.SectorsPerTrack[nDrive]) * 256;

                                    //      4:  read the entire track into memory
                                    if (Program.FloppyDriveStream[nDrive] != null)
                                    {
                                        Program.FloppyDriveStream[nDrive].Seek(m_lFileOffset, SeekOrigin.Begin);
                                        Program.FloppyDriveStream[nDrive].Read(m_caReadBuffer, 0, m_nBytesToTransfer);

                                        LogFloppyRead(m_caReadBuffer, 0, m_nBytesToTransfer);   // ???

                                        // start with a single byte of 0x00

                                        m_DMAF_DATARegister = 0x00;
                                    }

                                    //      5:  indicate that this a track read and not a sector read.
                                    m_nReadingTrack = true;
                                    m_nWritingTrack = false;

                                    //      6:  during the reading while serviceing the DRQ, the sector pre and post bytes will be sent around each 256 byte boundary.
                                    //          A state machine will be used to do this, so start the stae machine at the beginning

                                    currentReadTrackState = (int)READ_TRACK_STATES.ReadPostIndexGap;

                                    // set statemachine indexs to initial values.

                                    currentPostIndexGapIndex = 0;
                                    currentGap2Index = 0;
                                    currentGap3Index = 0;
                                    currentDataSectorNumber = 1;

                                    m_statusReadsWithoutDataRegisterAccess = 0;
                                    writeTrackBufferSize = 0;
                                    //totalBytesThisTrack = 0;    // reset on READ TRACK command to floppy controller
                                    writeTrackWriteBufferIndex = 0;

                                    writeTrackMinSector = 0xff;
                                    writeTrackMaxSector = 0;
                                }
                                break;

                            case 0xE0:  //  0xEX = READ TRACK
                                nType = 3;
                                //AfxMessageBox ("Unimplemented Floppy Command 0xEX");
                                break;

                            case 0xF0:  //  0xFX = WRITE TRACK (FORMAT)
                                if ((Program.DiskFormat[nDrive] == DiskFormats.DISK_FORMAT_UNIFLEX) || (Program.DiskFormat[nDrive] == DiskFormats.DISK_FORMAT_FLEX_IMA))
                                {
                                    int nStatus = 0;

                                    switch (Program.DiskFormat[nDrive])
                                    {
                                        case DiskFormats.DISK_FORMAT_UNIFLEX:
                                            nSectorSize = 512;
                                            break;
                                        case DiskFormats.DISK_FORMAT_MINIFLEX:
                                            nSectorSize = 128;
                                            break;
                                        default:
                                            nSectorSize = 256;
                                            break;
                                    }

                                    if ((m_nFDCTrack == 0) && ((m_nFDCSector - 1) == 0) && (m_nCurrentSideSelected == 0))
                                        m_lFileOffset = 0L;
                                    else
                                    {
                                        if ((Program.DiskFormat[nDrive] == DiskFormats.DISK_FORMAT_FLEX) || (Program.DiskFormat[nDrive] == DiskFormats.DISK_FORMAT_UNIFLEX) || (Program.DiskFormat[nDrive] == DiskFormats.DISK_FORMAT_MINIFLEX))
                                        {
                                            if (m_nCurrentSideSelected == 0)
                                            {
                                                if (m_nFDCTrack != 0 && Program.IsDoubleSided[nDrive] == -1)
                                                {
                                                    Program.IsDoubleSided[nDrive] = 0;
                                                    Program.NumberOfTracks[nDrive] = m_nFDCTrack + 1;
                                                    Program.SectorsPerTrack[nDrive] = (byte)m_nFormatSectorsPerSide;
                                                }

                                                if (Program.IsDoubleSided[nDrive] == 1)
                                                    m_lFileOffset = (long)((m_nFDCTrack * (m_nFormatSectorsPerSide * 2)) + (m_nFDCSector - 1)) * (long)nSectorSize;
                                                else
                                                    m_lFileOffset = (long)((m_nFDCTrack * m_nFormatSectorsPerSide) + (m_nFDCSector - 1)) * (long)nSectorSize;
                                            }
                                            else
                                            {
                                                Program.IsDoubleSided[nDrive] = 1;

                                                Program.NumberOfTracks[nDrive] = m_nFDCTrack + 1;
                                                Program.SectorsPerTrack[nDrive] = (byte)(m_nFormatSectorsPerSide * 2);

                                                m_lFileOffset = (long)((m_nFDCTrack * (m_nFormatSectorsPerSide * 2)) + m_nFormatSectorsPerSide + (m_nFDCSector - 1)) * (long)nSectorSize;
                                            }
                                        }
                                        else if (Program.DiskFormat[nDrive] == DiskFormats.DISK_FORMAT_FLEX_IMA)
                                        {
                                            //  !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                                            //  !!!!!!!!!!!!!! THIS NEEDS SOME SERIOUS WORK !!!!!!!!!!!!!! 
                                            //  !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                                            //  !!!!!!!!!!!!!!      IT DOES NOT WORK        !!!!!!!!!!!!!!
                                            //  !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                                            //  !!!!!!!!!!!!!!     MAYBE IT DOES WORK       !!!!!!!!!!!!!!
                                            //  !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

                                            // figure out where the buffer is so we can get the first byte to see if this is single or double density

                                            ulong lPhysicalAddress = LogicalToPhysicalAddress(latch, addr);
                                            if (latchRegisterIsInverted)
                                            {
                                                lPhysicalAddress = lPhysicalAddress ^ 0x00000000000F0000;
                                            }

                                            // ---------------------------------------------------------------------------------------------------
                                            //
                                            //      Use state machine to go through the data to build buffer to write to the diskette image
                                            //
                                            //          We do this by traversing the data pointed to by the address register and count
                                            //          registers in the DMA. lPhysicalAddress will be pointing to this data within the
                                            //          Program._cpu.Memory.MemorySpace[(int)((int)lPhysicalAddress)]
                                            //
                                            writeTrackWriteBufferIndex = 0;
                                            currentWriteTrackState = (int)WRITE_TRACK_STATES.WaitForIDRecordMark;

                                            // copy the memory from the emulated machine to the writeTrackWriteBuffer

                                            // we need to re-adjust the memory location in the emulator's address space on 4K boundaries for the DMA

                                            ushort a = addr;
                                            for (int i = 0; i < cnt; i++, a++)
                                            {
                                                //if (a >= 0x1000 && (a % 0x1000) == 0)
                                                {
                                                    lPhysicalAddress = LogicalToPhysicalAddress(latch, a);
                                                    if (latchRegisterIsInverted)
                                                    {
                                                        lPhysicalAddress = lPhysicalAddress ^ 0x00000000000F0000;
                                                    }
                                                }
                                                writeTrackWriteBuffer[i] = Program._cpu.Memory.MemorySpace[(int)((int)lPhysicalAddress)];
                                            }

                                            // this will un-interleave the write track buffer into the m_caWriteBuffer for writing to the image.

                                            UnInterleaveWriteTrackBufferToBuffer(cnt);

                                            // ---------------------------------------------------------------------------------------------------

                                            // Now we need to figure out where in the image this track needs to be written

                                            // default to single density

                                            m_nCurrentDensitySelected = 0;
                                            if (Program._cpu.Memory.MemorySpace[(int)((int)lPhysicalAddress)] == 0x4E)
                                            {
                                                m_nCurrentDensitySelected = 1;
                                            }

                                            // if we are on track 0, just multiply sector size by either 15 or 30 depenfing on which side

                                            if (m_nFDCTrack == 0)
                                            {
                                                // start at the beginning for track 0 unless we are on side 1

                                                if (m_nCurrentSideSelected == 0)
                                                    m_lFileOffset = 0;
                                                else
                                                    m_lFileOffset = (Program.SectorsOnTrackZero[nDrive] / 2) * nSectorSize;
                                            }
                                            else
                                            {
                                                // Since we are not on track 0, we need to know how many sectors are on CYLINDER 0. To
                                                // know that we need to know how many sides this image has.

                                                int isDoubleSided = Program.IsDoubleSided[nDrive];

                                                // this will take care of getting past track 0

                                                m_lFileOffset = Program.SectorsOnTrackZero[nDrive] * nSectorSize;

                                                // now add in the number of sectors on the other tracks before this one and we will be
                                                // positioned at the start of this track in the image. If we are doing side 1, we need
                                                // to move the offset up by half the number of bytes in the sectors on side 1.

                                                if (m_nCurrentSideSelected == 0)
                                                {
                                                    m_lFileOffset += (m_nFDCTrack - 1) * Program.SectorsPerTrack[nDrive] * nSectorSize;
                                                }
                                                else
                                                {
                                                    m_lFileOffset += (((m_nFDCTrack - 1) * Program.SectorsPerTrack[nDrive]) + (Program.SectorsPerTrack[nDrive] / 2)) * nSectorSize;
                                                }

                                                //
                                                //  Upon receipt of the Write Track command, the head is loaded and the BUSY bit is set. Writing starts with
                                                // the leading edge of first encountered index mark and continues until the next index pulse, at which time
                                                // an interrupt is generated. The Data Request is activated immediately upon receiving the command, but writing
                                                // will not start until after the first byte has been loaded into the Data Register. If the DR has not been
                                                // loaded by the time the index pulse is encountered the operaiton is terminated making the device Not Busy.
                                                // The Lost Data Status Bit is set and the interrupt is activated. If a byte is not present in the DR as 
                                                // needed, a byte of zeros is substituted. Address Marks and CRC characters are written on the disk by
                                                // detecting certain data byte patterns in the outgoing data stream as shown in the table below. The CRC
                                                // generator is initialized when any data byte from F8 to FE is about to be transferred from the DR to the DSR.
                                                //
                                                //      CONTROL BYTES FOR INITIALIZATION
                                                //
                                                //      data pattern    interpretation          clock mark
                                                //          F7          Write CRC character     FF
                                                //          F8          Data Address Mark       C7
                                                //          F9          Data Address Mark       C7
                                                //          FA          Data Address Mark       C7
                                                //          FB          Data Address Mark       C7
                                                //          FC          Data Address Mark       D7
                                                //          FD          Spare
                                                //          FE          ID Address Mark         C7
                                                //
                                                // The Write Track command will not execute if the \DINT input is grounded. Instead the write protect status
                                                // bit is set and the interrupt is activated. Note that 1 F7 pattern generates 2 CRC bytes.
                                                //

                                                // formatting a track only works if the image files has the SIR already set up.

                                                //  On each write track buffer command, reset the number of sectors in the buffer to 0.
                                                //  As each address ID record is written, this number will get incremented/.
                                                //  When the count is equal to the number of allowable sectors on the track being written, it is time
                                                //  to signle complete by dropping BUSY and Setting the interrupt and stop setting DRQ after writing
                                                //  the byte to the sector track buffer.

                                                sectorsInWriteTrackBuffer = 0;
                                                lastFewBytesRead = 0;

                                                m_nFDCReading = false;
                                                m_nReadingTrack = false;

                                                //activityLedColor = (int)ActivityLedColors.reddot;

                                                m_nFDCWriting = true;
                                                m_nWritingTrack = true;

                                                //      1:  set proper type, status bits and track register

                                                nType = 3;
                                                m_DMAF_STATRegister |= (byte)DMAF_StatusLines.DMAF_HDLOADED;
                                                m_nFDCTrack = m_DMAF_TRKRegister;

                                                currentWriteTrackState = (int)WRITE_TRACK_STATES.WaitForIDRecordMark;
                                            }
                                        }
                                        else if (Program.DiskFormat[nDrive] == DiskFormats.DISK_FORMAT_OS9)
                                        {
                                            int nSPT = m_nFormatSectorsPerSide;
                                            int nFormat = Program.FormatByte[nDrive];

                                            //  OS9 ALWAYS has m_nSectorsOnTrackZero sectors on track 0 side 0
                                            //  and uses the side select before seeking to another cylinder.
                                            //  track 0 side 1 has m_nFormatSectorsPerSide sectors.

                                            if (m_nFDCTrack == 0)
                                                m_lFileOffset = (long)(m_nFDCSector) * (long)nSectorSize;
                                            else
                                            {
                                                // start off with track 0 side 1 already added in if it's double sided. 
                                                // We'll get to track 0 side 0 sectors later

                                                if ((nFormat & 0x0001) != 0)   // double sided
                                                {
                                                    m_lFileOffset = (long)nSPT * (long)nSectorSize;

                                                    // is this is not side 0, add in this tracks side 0 sectors

                                                    if (m_nCurrentSideSelected != 0)
                                                        m_lFileOffset += (long)nSPT * (long)nSectorSize;

                                                    // the rest of the math is done on cylinders

                                                    nSPT *= 2;
                                                }
                                                else
                                                    m_lFileOffset = 0L;

                                                // now add in all of the previous tracks sectors (except track 0)

                                                m_lFileOffset += (long)(((m_nFDCTrack - 1) * nSPT) + m_nFDCSector) * (long)nSectorSize;
                                            }

                                            // if not track 0 side 0 - add in track 0 side 0 sectors 
                                            // because it may have a different number of sectors

                                            if (!((m_nFDCTrack == 0) && (m_nCurrentSideSelected == 0)))
                                            {
                                                m_lFileOffset += (long)Program.SectorsOnTrackZeroSideZero[nDrive] * (long)nSectorSize;
                                            }
                                        }
                                        else
                                        {
                                            nType = 3;
                                            unchecked
                                            {
                                                m_DMAF_STATRegister &= (byte)~(byte)DMAF_StatusLines.DMAF_HDLOADED;
                                                m_DMAF_STATRegister &= (byte)~(byte)DMAF_StatusLines.DMAF_BUSY;
                                            }
                                            //activityLedColor = (int)ActivityLedColors.greydot;
                                        }
                                    }

                                    if (nStatus == 0)
                                    {
                                        if (Program.FloppyDriveStream[nDrive] != null)
                                        {
                                            m_DMAF_STATRegister &= (byte)(~((byte)DMAF_StatusLines.DMAF_DRQ | (byte)DMAF_StatusLines.DMAF_BUSY) & 0xFF);
                                            m_DMAF_DRVRegister &= (byte)(~((byte)DMAF_ContollerLines.DRV_DRQ) & 0xFF);        // turn off high order bit in drive status register
                                            m_nFDCWriting = false;

                                            // if the dma is active - write bytes to the disk from the buffer and reset the dma counter
                                            // the DMA priority register must have channel 1 enabled to be able to do DMA transfer

                                            if ((activechannel == 0) && ((m_DMAF_DMA_PRIORITYRegister & 0x01) == (byte)0x01))   // only handle channel 1 for floppy and only if enabled
                                            {
                                                if (cnt > 0)
                                                {
                                                    int bytesToWriteSize = writeTrackBufferSize;
                                                    int bytesToWriteOffset = 0;

                                                    if (m_nCurrentSideSelected != 0)
                                                    {
                                                        // if we are doing the second side, we need to use the top half of the buffer

                                                        bytesToWriteSize = writeTrackBufferSize / 2;
                                                        bytesToWriteOffset = bytesToWriteSize;
                                                    }

                                                    Program.FloppyDriveStream[nDrive].Seek(m_lFileOffset, SeekOrigin.Begin);
                                                    Program.FloppyDriveStream[nDrive].Write(m_caWriteBuffer, bytesToWriteOffset, bytesToWriteSize);
                                                    Program.FloppyDriveStream[nDrive].Flush();

                                                    //using (BinaryWriter x = new BinaryWriter(File.Open(@"D:\m_caWriteBuffer.bin", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite)))
                                                    //{
                                                    //    x.Write(m_caWriteBuffer, 0, writeTrackBufferSize);
                                                    //}
                                                    ushort a = (ushort)(m_DMAF_DMA_ADDRESSRegister[activechannel].hibyte * 256 + m_DMAF_DMA_ADDRESSRegister[activechannel].lobyte);
                                                    if ((m_DMAF_DMA_CHANNELRegister[activechannel] & 0x08) == 0x08)
                                                        a -= cnt;
                                                    else
                                                        a += cnt;

                                                    m_DMAF_DMA_ADDRESSRegister[activechannel].hibyte = (byte)(a / 256);
                                                    m_DMAF_DMA_ADDRESSRegister[activechannel].lobyte = (byte)(a % 256);

                                                    m_DMAF_STATRegister &= (byte)(~((byte)DMAF_StatusLines.DMAF_DRQ | (byte)DMAF_StatusLines.DMAF_BUSY) & 0xFF);
                                                    m_DMAF_DRVRegister &= (byte)(~((byte)DMAF_ContollerLines.DRV_DRQ) & 0xFF);            // turn off high order bit in drive status register

                                                    m_nFDCReading = false;
                                                    m_nFDCWriting = false;
                                                }
                                                else
                                                {
                                                    // must be doing polled IO

                                                    //AfxMessageBox ("Polled IO read not implemented");
                                                }
                                            }
                                        }
                                    }
                                    else
                                        m_DMAF_STATRegister |= (byte)DMAF_StatusCodes.DMAF_RNF;        //  RECORD NOT FOUND
                                }
                                else
                                {
                                    if ((m_nFDCTrack == 0) && ((m_nFDCSector - 1) == 0) && (m_nCurrentSideSelected == 0))
                                        //AfxMessageBox ("Format command is only supported for UniFLEX");

                                        m_DMAF_STATRegister |= (byte)DMAF_StatusLines.DMAF_WRTPROTECT;     // report Write Protected Status
                                }
                                nType = 3;
                                break;

                            // TYPE IV

                            case 0xD0:  //  0xDX = FORCE INTERRUPT

                                // Ix = Interrupt Condition Flags
                                // I0 = 1 Not Ready to Ready Transition
                                // I1 = 1 Ready To Not Ready Transition
                                // I2 = 1 Index Pulse
                                // I3 = 1 Immediate Interrupt, Requires A Reset*
                                // I3-I0 = 0 Terminate With No Interrupt (INTRQ)

                                m_nInterruptConditionFlag = b & 0x0F;
                                nType = 4;

                                m_DMAF_STATRegister &= (byte)(~((byte)DMAF_StatusLines.DMAF_BUSY) & 0xFF);   // turn off busy
                                m_DMAF_DRVRegister &= (byte)(~((byte)DMAF_ContollerLines.DRV_DRQ) & 0xFF);   // turn off any DRQ hanging out there

                                break;

                            default:
                                //AfxMessageBox ("Unimplemented Floppy Command");
                                break;
                        }

                        // should ANY command cause BUSY? Maybe - if we clear it later if not.

                        m_DMAF_STATRegister |= (byte)DMAF_StatusLines.DMAF_BUSY;

                        // see if current drive is READY

                        if (Program.DriveOpen[nDrive] == true || Program.FloppyDriveStream[nDrive] == null)
                            m_DMAF_STATRegister |= (byte)DMAF_StatusLines.DMAF_NOTREADY;
                        else
                            m_DMAF_STATRegister &= (byte)(~((byte)DMAF_StatusLines.DMAF_NOTREADY) & 0xFF);

                        switch (nType)
                        {
                            case 1:
                                {
                                    m_DMAF_STATRegister |= (byte)DMAF_StatusLines.DMAF_HDLOADED;
                                    m_DMAF_STATRegister &= (byte)(~((byte)DMAF_StatusLines.DMAF_BUSY | (byte)DMAF_StatusLines.DMAF_DRQ) & 0xFF);

                                    if (m_nFDCTrack == 0)
                                        m_DMAF_STATRegister |= (byte)DMAF_StatusLines.DMAF_TRKZ;
                                    else
                                        m_DMAF_STATRegister &= (byte)(~((byte)DMAF_StatusLines.DMAF_TRKZ) & 0xFF);

                                    m_DMAF_DRVRegister &= (byte)(~((byte)DMAF_ContollerLines.DRV_DRQ) & 0xFF);

                                    if (_bInterruptEnabled && (m_DMAF_LATCHRegister & 0x10) == 0x10)
                                    {
                                        // the latch at F040 : (one's complement data sent to controller)
                                        //
                                        //      bit 0..3    is A16...A19 for DMA.
                                        //      bit 4       is INT enable for the FDC..
                                        //                      If set to '1' the FDC INT pin triggers an /IRQ
                                        //                      if this bit get written as a 1, it is stored as a 0
                                        //                      so having a 0 in the latch disables interrupts

                                        StartDMAFInterruptDelayTimer(m_nInterruptDelay, (int)DeviceInterruptMask.DEVICE_INT_MASK_FDC);
                                        if (Program._cpu != null)
                                        {
                                            if ((Program._cpu.InWait || Program._cpu.InSync) && Program.CpuThread.ThreadState == System.Threading.ThreadState.Suspended)
                                            {
                                                try
                                                {
                                                    Program.CpuThread.Resume();
                                                }
                                                catch (ThreadStateException e)
                                                {
                                                    // do nothing if thread is not suspended
                                                }
                                            }
                                        }
                                    }
                                }
                                break;

                            case 2:
                            case 3:
                                {
                                    if (Program.FloppyDriveStream[nDrive] != null)
                                    {
                                        if (cnt == 0)
                                        {
                                            // we get here during polled IO becasue will still be 0. If the DMA did
                                            // the work, the count will be the number of btyes it transferred.

                                            m_DMAF_STATRegister |= ((byte)DMAF_StatusLines.DMAF_DRQ | (byte)DMAF_StatusLines.DMAF_BUSY);
                                            m_DMAF_DRVRegister |= (byte)DMAF_ContollerLines.DRV_DRQ;

                                            if (_bInterruptEnabled && (m_DMAF_LATCHRegister & 0x10) == 0x10)
                                            {
                                                // the latch at F040 : (one's complement data sent to controller)
                                                //
                                                //      bit 0..3    is A16...A19 for DMA.
                                                //      bit 4       is INT enable for the FDC..
                                                //                      If set to '1' the FDC INT pin triggers an /IRQ
                                                //                      if this bit get written as a 1, it is stored as a 0
                                                //                      so having a 0 in the latch disables interrupts

                                                StartDMAFInterruptDelayTimer(m_nInterruptDelay, (int)DeviceInterruptMask.DEVICE_INT_MASK_FDC);
                                                if (Program._cpu != null)
                                                {
                                                    if ((Program._cpu.InWait || Program._cpu.InSync) && Program.CpuThread.ThreadState == System.Threading.ThreadState.Suspended)
                                                    {
                                                        try
                                                        {
                                                            Program.CpuThread.Resume();
                                                        }
                                                        catch (ThreadStateException e)
                                                        {
                                                            // do nothing if thread is not suspended
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            // if the DMA did the transfer - clear the DRQ bits

                                            if ((m_DMAF_STATRegister & (byte)DMAF_StatusLines.DMAF_WRTPROTECT) == 0)
                                                m_DMAF_STATRegister = (byte)DMAF_StatusCodes.DMAF_MOTOR_ON;
                                            else
                                                m_DMAF_STATRegister |= (byte)DMAF_StatusCodes.DMAF_MOTOR_ON;

                                            if (_bInterruptEnabled)
                                                ClearInterrupt();       // in case it is already set

                                            // clear all the error codes and clear the DMAF board DRQ signal

                                            m_DMAF_STATRegister &= (byte)(~((byte)DMAF_StatusCodes.DMAF_LOST_DATA | (byte)DMAF_StatusCodes.DMAF_CRCERR | (byte)DMAF_StatusCodes.DMAF_RNF | (byte)DMAF_StatusCodes.DMAF_RECORD_TYPE_SPIN_UP) & 0xFF);
                                            m_DMAF_DRVRegister &= (byte)(~((byte)DMAF_ContollerLines.DRV_DRQ) & 0xFF);

                                            // clear the DMA count registers

                                            m_DMAF_DMA_DMCNTRegister[0].hibyte = m_DMAF_DMA_DMCNTRegister[0].lobyte = 0;

                                            // the latch at $F040 has no control over the setting of the interrupt for the DMA
                                            // so do not check bit 4 in the latch at $F040. Instead we need to make sure the
                                            // m_DMAF_DMA_PRIORITYRegister has the channel enabled to do the transfer 

                                            if (_bInterruptEnabled && (m_DMAF_DMA_PRIORITYRegister & 0x01) == 0x01)
                                            {
                                                // the timer started by StartDMAFInterruptDelayTimer will set the interrupt when the timer expires.

                                                // the latch at F040 : (one's complement data sent to controller)
                                                //
                                                //      bit 0..3    is A16...A19 for DMA.
                                                //      bit 4       is INT enable for the FDC..
                                                //                      If set to '1' the FDC INT pin triggers an /IRQ
                                                //                      if this bit get written as a 1, it is stored as a 0
                                                //                      so having a 0 in the latch disables interrupts

                                                if ((m_DMAF_LATCHRegister & 0x10) == 0x10)
                                                {
                                                    // see if the interrupt control register on the DMA is allowing the DMA to interrupt

                                                    if ((m_DMAF_DMA_INTERRUPTRegister & 0x01) == 0x01)
                                                        StartDMAFInterruptDelayTimer(m_nInterruptDelay, (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA1 | (int)DeviceInterruptMask.DEVICE_INT_MASK_FDC);
                                                    else
                                                        StartDMAFInterruptDelayTimer(m_nInterruptDelay, (int)DeviceInterruptMask.DEVICE_INT_MASK_FDC);
                                                }
                                                else
                                                {
                                                    // we must always set this bit when DMA is finished whether we set the interrupt or not

                                                    m_DMAF_DMA_CHANNELRegister[0] |= 0x80;     // say channel is interrupting

                                                    // now see if we should set the interrupt register as well.

                                                    if ((m_DMAF_DMA_INTERRUPTRegister & 0x01) == 0x01)
                                                        StartDMAFInterruptDelayTimer(m_nInterruptDelay, (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA1);
                                                }

                                                if (Program._cpu != null)
                                                {
                                                    if ((Program._cpu.InWait || Program._cpu.InSync) && Program.CpuThread.ThreadState == System.Threading.ThreadState.Suspended)
                                                    {
                                                        try
                                                        {
                                                            Program.CpuThread.Resume();
                                                        }
                                                        catch (ThreadStateException e)
                                                        {
                                                            // do nothing if thread is not suspended
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        m_DMAF_STATRegister |= (byte)DMAF_StatusLines.DMAF_NOTREADY;
                                        m_DMAF_STATRegister &= (byte)(~((byte)DMAF_StatusLines.DMAF_DRQ) & 0xFF);
                                        m_DMAF_DRVRegister &= (byte)(~((byte)DMAF_ContollerLines.DRV_DRQ) & 0xFF);

										if ((m_DMAF_LATCHRegister & 0x10) == 0x10)
                                        	StartDMAFInterruptDelayTimer(m_nInterruptDelay, (int)DeviceInterruptMask.DEVICE_INT_MASK_FDC);
                                    }
                                }
                                break;

                            case 4:
                                {
                                    // used to include:
                                    //DMAF_StatusCodes.DMAF_CRCERR |   // clear CRC Error Bit
                                    //DMAF_StatusCodes.DMAF_RNF    |   // clear Record Not Found Bit

                                    m_DMAF_STATRegister &= (byte)(~((byte)(
                                                                DMAF_StatusLines.DMAF_DRQ |   // clear Data Request Bit
                                                                DMAF_StatusLines.DMAF_SEEKERR |   // clear SEEK Error Bit
                                                                DMAF_StatusLines.DMAF_BUSY       // clear BUSY bit
                                                             )) & 0xFF);
                                    m_DMAF_DRVRegister &= (byte)(~((byte)DMAF_ContollerLines.DRV_DRQ) & 0xFF);        // turn off high order bit in drive status register

                                    if (m_nInterruptConditionFlag != 0)
                                    {
                                        // if the m_nInterruptConditionFlag is 0 that means we should 'Terminate With No Interrupt (INTRQ)'
                                        // all other condition - we should do an immediate interrupt

                                        // Ix = Interrupt Condition Flags
                                        //
                                        // I0 = 1 Not Ready to Ready Transition
                                        // I1 = 1 Ready To Not Ready Transition
                                        // I2 = 1 Index Pulse
                                        // I3 = 1 Immediate Interrupt, Requires A Reset*
                                        // I3-I0 = 0 Terminate With No Interrupt (INTRQ)

                                        if (_bInterruptEnabled && (m_DMAF_LATCHRegister & 0x10) == 0x10)
                                        {
                                            // the latch at F040 : (one's complement data sent to controller)
                                            //
                                            //      bit 0..3    is A16...A19 for DMA.
                                            //      bit 4       is INT enable for the FDC..
                                            //                      If set to '1' the FDC INT pin triggers an /IRQ
                                            //                      if this bit get written as a 1, it is stored as a 0
                                            //                      so having a 0 in the latch disables interrupts

                                            StartDMAFInterruptDelayTimer(m_nInterruptDelay, (int)DeviceInterruptMask.DEVICE_INT_MASK_FDC);
                                            if (Program._cpu != null)
                                            {
                                                if ((Program._cpu.InWait || Program._cpu.InSync) && Program.CpuThread.ThreadState == System.Threading.ThreadState.Suspended)
                                                {
                                                    try
                                                    {
                                                        Program.CpuThread.Resume();
                                                    }
                                                    catch (ThreadStateException e)
                                                    {
                                                        // do nothing if thread is not suspended
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                        }
                    }
                    break;

                case DMAF_OffsetRegisters.DMAF_TRKREG_OFFSET:                       // 0xF021
                    m_DMAF_TRKRegister = b;
                    m_nFDCTrack = b;
                    break;

                case DMAF_OffsetRegisters.DMAF_SECREG_OFFSET:                       // 0xF022
                    m_DMAF_SECRegister = b;
                    m_nFDCSector = b;
                    break;

                case DMAF_OffsetRegisters.DMAF_HLD_TOGGLE_OFFSET:                   // 0xF050 - head load toggle
                    DMAF_HLD_TOGGLERegister = b;
                    break;
            }
        }

        void WriteDMA(ushort m, byte b)
        {
            int nWhichRegister = m - m_sBaseAddress;

            ClearInterrupt();

            if (latchRegisterIsInverted)
                b = (byte)~b;       // it comes in inverted on the DMAF-2.

            switch (nWhichRegister)
            {
                case (int)DMAF_OffsetRegisters.DMAF2_LATCH_OFFSET:

                    // the latch at F040 : (one's complement data sent to controller)
                    //
                    //      bit 0..3    is A16...A19 for DMA.
                    //      bit 4       is INT enable for the FDC..
                    //                      If set to '1' the FDC INT pin triggers an /IRQ
                    //                      if this bit get written as a 1, it is stored as a 0
                    //                      so having a 0 in the latch disables interrupts

                    // if we need to receive it un0inverted, invert it back since we have already inverted it

                    if (latchRegisterIsInverted)
                        m_DMAF_LATCHRegister = b;
                    else
                        m_DMAF_LATCHRegister = (byte)~b;
                    break;

                //*
                //*   DMAF 6844 DMA controller definitions (bytes must be one's complememnt
                //*

                case (int)DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET + 0:
                    m_DMAF_DMA_ADDRESSRegister[0].hibyte = b;
                    break;

                case (int)DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET + 1:
                    m_DMAF_DMA_ADDRESSRegister[0].lobyte = b;
                    break;

                case (int)DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET + 4:
                    m_DMAF_DMA_ADDRESSRegister[1].hibyte = b;
                    break;

                case (int)DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET + 5:
                    m_DMAF_DMA_ADDRESSRegister[1].lobyte = b;
                    break;

                case (int)DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET + 8:
                    m_DMAF_DMA_ADDRESSRegister[2].hibyte = b;
                    break;

                case (int)DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET + 9:
                    m_DMAF_DMA_ADDRESSRegister[2].lobyte = b;
                    break;

                case (int)DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET + 12:
                    m_DMAF_DMA_ADDRESSRegister[3].hibyte = b;
                    break;

                case (int)DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET + 13:
                    m_DMAF_DMA_ADDRESSRegister[3].lobyte = b;
                    break;

                case (int)DMAF_OffsetRegisters.DMAF_DMA_DMCNT_OFFSET + 0:
                    m_DMAF_DMA_DMCNTRegister[0].hibyte = b;
                    break;

                case (int)DMAF_OffsetRegisters.DMAF_DMA_DMCNT_OFFSET + 1:
                    m_DMAF_DMA_DMCNTRegister[0].lobyte = b;
                    break;

                case (int)DMAF_OffsetRegisters.DMAF_DMA_DMCNT_OFFSET + 4:
                    m_DMAF_DMA_DMCNTRegister[1].hibyte = b;
                    break;

                case (int)DMAF_OffsetRegisters.DMAF_DMA_DMCNT_OFFSET + 5:
                    m_DMAF_DMA_DMCNTRegister[1].lobyte = b;
                    break;

                case (int)DMAF_OffsetRegisters.DMAF_DMA_DMCNT_OFFSET + 8:
                    m_DMAF_DMA_DMCNTRegister[2].hibyte = b;
                    break;

                case (int)DMAF_OffsetRegisters.DMAF_DMA_DMCNT_OFFSET + 9:
                    m_DMAF_DMA_DMCNTRegister[2].lobyte = b;
                    break;

                case (int)DMAF_OffsetRegisters.DMAF_DMA_DMCNT_OFFSET + 12:
                    m_DMAF_DMA_DMCNTRegister[3].hibyte = b;
                    break;

                case (int)DMAF_OffsetRegisters.DMAF_DMA_DMCNT_OFFSET + 13:
                    m_DMAF_DMA_DMCNTRegister[3].lobyte = b;
                    break;

                // Channel Control Register (4 of them - 1 for each channel)
                //
                //      Bit     description
                //      ---     ------------------------------------------------
                //       7      DMA End Flag set during transfer of last byte
                //                           cleared on reading o the register
                //                           will set IRQ if enabled in the interrupt control register
                //       6      Busy / Ready Flag - read only status bit 1 = busy, 0 = done
                //       5      Not Used
                //       4      Not Used
                //       3      Address Up or Down (0 = address register increments)
                //       2      MCA - Mode Control A
                //       1      MCB - Mode Control B
                //              MCA     MCB     DMA Transfer Mode
                //               0       0      Mode 2
                //               0       1      Mode 3
                //               1       0      Mode 1
                //               1       1      Undefined
                //       0      Read / Write (0 = write)

                case (int)DMAF_OffsetRegisters.DMAF_DMA_CHANNEL_OFFSET:       m_DMAF_DMA_CHANNELRegister[0] = b; break;
                case (int)DMAF_OffsetRegisters.DMAF_DMA_CHANNEL_OFFSET + 1:   m_DMAF_DMA_CHANNELRegister[1] = b; break;
                case (int)DMAF_OffsetRegisters.DMAF_DMA_CHANNEL_OFFSET + 2:   m_DMAF_DMA_CHANNELRegister[2] = b; break;
                case (int)DMAF_OffsetRegisters.DMAF_DMA_CHANNEL_OFFSET + 3:   m_DMAF_DMA_CHANNELRegister[3] = b; break;

                // Priority Control Register
                //      Bit     description
                //      ---     ------------------------------------------------
                //       7      Rotate Control
                //       6      Not Used
                //       5      Not Used
                //       4      Not Used
                //       3      Request Enable 3
                //       2      Request Enable 2
                //       1      Request Enable 1
                //       0      Request Enable 0

                case (int)DMAF_OffsetRegisters.DMAF_DMA_PRIORITY_OFFSET:
                    {
                        m_DMAF_DMA_PRIORITYRegister = b;

                        if (_bInterruptEnabled)
                            ClearInterrupt();
                    }
                    break;

                // Interrupt Control Register
                //      Bit     description
                //      ---     ------------------------------------------------
                //       7      DEND IRQ Flag
                //       6      Not Used
                //       5      Not Used
                //       4      Not Used
                //       3      DEND IRQ Enable 3
                //       2      DEND IRQ Enable 2
                //       1      DEND IRQ Enable 1
                //       0      DEND IRQ Enable 0

                case (int)DMAF_OffsetRegisters.DMAF_DMA_INTERRUPT_OFFSET:
                    m_DMAF_DMA_INTERRUPTRegister = b;
                    break;

                // Data Chain Register
                //      Bit     description
                //      ---     ------------------------------------------------
                //       7      Not Used
                //       6      Not Used
                //       5      Not Used
                //       4      Not Used
                //       3      Two/Four Channel Select (2/4)
                //       2      Data Channel Select B
                //       1      Data Channel Select A
                //       0      Data Chain Enable

                case (int)DMAF_OffsetRegisters.DMAF_DMA_CHAIN_OFFSET:
                    m_DMAF_DMA_CHAINRegister = b;
                    break;

            }
        }

        public override void Write(ushort m, byte b)
        {
            DMAF_OffsetRegisters nWhichRegister = (DMAF_OffsetRegisters)(m - m_sBaseAddress);

            LogDMAFActivity((ushort)(m - m_sBaseAddress), b, false);

            if ((nWhichRegister >= DMAF_OffsetRegisters.DMAF_STATREG_OFFSET && nWhichRegister <= DMAF_OffsetRegisters.DMAF_DRVREG_OFFSET) || nWhichRegister == DMAF_OffsetRegisters.DMAF_HLD_TOGGLE_OFFSET)
                WriteFloppy(m, b);
            else if ((nWhichRegister >= DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET && nWhichRegister <= DMAF_OffsetRegisters.DMAF_DMA_CHAIN_OFFSET) || nWhichRegister == DMAF_OffsetRegisters.DMAF2_LATCH_OFFSET)
                WriteDMA(m, b);
            else
                Program._cpu.WriteToFirst64K(m, b);
        }

        byte ReadFloppy(ushort m)
        {
            byte d = 0xFF;

            ClearInterrupt();

            int nWhichRegister = m - m_sBaseAddress;
            int nDrive = 0;
            switch (m_DMAF_DRVRegister & 0x0F)
            {
                case 1:
                    nDrive = 0;
                    break;
                case 2:
                    nDrive = 1;
                    break;
                case 4:
                    nDrive = 2;
                    break;
                case 8:
                    nDrive = 3;
                    break;
            }

            switch ((DMAF_OffsetRegisters)nWhichRegister)
            {
                case DMAF_OffsetRegisters.DMAF_DATAREG_OFFSET:                          // 0xF023
                    {
                        if (m_nFDCReading)
                        {
                            //  this only gets executed for Polled I/O The DMA does all the reading of the data register
                            //  and sets the interrupt when finished

                            m_DMAF_DATARegister = m_caReadBuffer[m_nFDCReadPtr];
                            //if (!Program._cpu.BuildingDebugLine)
                            lock (Program._cpu.buildingDebugLineLock)
                            {
                                m_nFDCReadPtr++;

                                if (m_nFDCReadPtr == m_nBytesToTransfer)
                                {
                                    //if (!Program._cpu.BuildingDebugLine)
                                    //lock (Program._cpu.buildingDebugLineLock)
                                    {
                                        m_DMAF_STATRegister &= (byte)(~((byte)((byte)DMAF_StatusLines.DMAF_DRQ | (byte)DMAF_StatusLines.DMAF_BUSY)) & 0xFF);
                                        m_DMAF_DRVRegister &= (byte)(~((byte)DMAF_ContollerLines.DRV_DRQ) & 0xFF);            // turn off high order bit in drive status register

                                        m_nFDCReading = false;

                                        //Program._mainForm.SetFloppyActivity(nDrive, global::SWTPCemuApp.Properties.Resources.greydot);

                                        if (_bInterruptEnabled)
                                            ClearInterrupt((int)DeviceInterruptMask.DEVICE_INT_MASK_FDC);
                                    }
                                }
                                // --------------------
                                else
                                {
                                    m_DMAF_STATRegister |= (byte)(DMAF_ContollerLines.DRV_DRQ);
                                    m_DMAF_DRVRegister |= (byte)(DMAF_ContollerLines.DRV_DRQ);

                                    lock (Program._cpu.buildingDebugLineLock)
                                    {
                                        if (_bInterruptEnabled && (m_DMAF_LATCHRegister & 0x10) == 0x10)
                                        {
                                            // the latch at F040 : (one's complement data sent to controller)
                                            //
                                            //      bit 0..3    is A16...A19 for DMA.
                                            //      bit 4       is INT enable for the FDC..
                                            //                      If set to '1' the FDC INT pin triggers an /IRQ
                                            //                      if this bit get written as a 1, it is stored as a 0
                                            //                      so having a 0 in the latch disables interrupts

                                            StartDMAFInterruptDelayTimer(m_nInterruptDelay, (int)DeviceInterruptMask.DEVICE_INT_MASK_FDC);
                                            if (Program._cpu != null)
                                            {
                                                if ((Program._cpu.InWait || Program._cpu.InSync) && Program.CpuThread.ThreadState == System.Threading.ThreadState.Suspended)
                                                {
                                                    try
                                                    {
                                                        Program.CpuThread.Resume();
                                                    }
                                                    catch (ThreadStateException e)
                                                    {
                                                        // do nothing if thread is not suspended
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                // --------------------
                                d = m_DMAF_DATARegister;

                                // if the dma is active - put the byte in memory also and decrement the dma counter
                            }
                        }
                        else
                        {
                            d = m_DMAF_DATARegister;

                            // if the dma is active - put the byte in memory also and decrement the dma counter
                        }
                    }
                    break;

                case DMAF_OffsetRegisters.DMAF_STATREG_OFFSET:                          // 0xF020
                    {
                        if ((m_DMAF_CMDRegister & 0x10) == 0x10)
                        {
                            // last commadn was a seek - clear the busy bit;

                            m_DMAF_STATRegister &= (byte)(~((byte)DMAF_StatusLines.DMAF_BUSY) & 0xFF);
                        }

                        //if (!Program._cpu.BuildingDebugLine)
                        lock (Program._cpu.buildingDebugLineLock)
                        {
                            DMAF_AT_DTBRegister &= (byte)(~0x04 & 0xFF);                          // clear interrupting bit to VIA
                        }

                        if (Program.DriveOpen[nDrive] == true)               // see if current drive is READY
                            m_DMAF_STATRegister |= (byte)DMAF_StatusLines.DMAF_NOTREADY;
                        else
                            m_DMAF_STATRegister &= (byte)(~((byte)DMAF_StatusLines.DMAF_NOTREADY) & 0xFF);

                        if (Program.WriteProtected[nDrive] == true)          // see if write protected
                            m_DMAF_STATRegister |= (byte)DMAF_StatusLines.DMAF_WRTPROTECT;
                        else
                            m_DMAF_STATRegister &= (byte)(~((byte)DMAF_StatusLines.DMAF_WRTPROTECT) & 0xFF);

                        //if (!Program._cpu.BuildingDebugLine)
                        lock (Program._cpu.buildingDebugLineLock)
                        {
                            if (!m_nFDCReading && !m_nFDCWriting)           // turn off BUSY if not read/writing
                                m_DMAF_STATRegister &= (byte)(~((byte)DMAF_StatusLines.DMAF_BUSY) & 0xFF);
                            else
                            {
                                if (++m_nStatusReads > (m_nBytesToTransfer * 2))
                                {
                                    m_DMAF_STATRegister &= (byte)(~((byte)DMAF_StatusLines.DMAF_BUSY) & 0xFF);     // clear BUSY if data not read

                                    //Program._mainForm.SetFloppyActivity(nDrive, global::SWTPCemuApp.Properties.Resources.greydot);
                                }
                            }
                            if (_bInterruptEnabled)
                                ClearInterrupt((int)DeviceInterruptMask.DEVICE_INT_MASK_FDC);
                        }
                        d = m_DMAF_STATRegister;                      // get controller status
                    }
                    break;

                case DMAF_OffsetRegisters.DMAF_DRVREG_OFFSET:                           // 0xF024
                    d = (byte)~m_DMAF_DRVRegister; 
                    break;

                case DMAF_OffsetRegisters.DMAF_TRKREG_OFFSET:                         // 0xF021
                    d = m_DMAF_TRKRegister;                                            // get Track Register
                    break;

                case DMAF_OffsetRegisters.DMAF_SECREG_OFFSET:                         // 0xF022
                    d = m_DMAF_SECRegister;                                            // get Track Register
                    break;

                case DMAF_OffsetRegisters.DMAF_HLD_TOGGLE_OFFSET:                   // 0xF050 - head load toggle
                    d = DMAF_HLD_TOGGLERegister;
                    break;
            }

            return (d);
        }

        byte ReadDMA(ushort m)
        {
            byte d = 0xFF;
            int nWhichRegister = m - m_sBaseAddress;

            ClearInterrupt();

            switch ((DMAF_OffsetRegisters)nWhichRegister)
            {
                case DMAF_OffsetRegisters.DMAF2_LATCH_OFFSET:

                    if (latchRegisterIsInverted)
                        d = (byte)~m_DMAF_LATCHRegister;  // the Enable interrupt bit is in here somewhere
                    else
                        d = m_DMAF_LATCHRegister;
                    break;

                //*
                //*   DMAF 6844 DMA controller definitions
                //*

                case DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET + 0:                  // 0xF000
                    d = m_DMAF_DMA_ADDRESSRegister[0].hibyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET + 1:                  // 0xF001
                    d = m_DMAF_DMA_ADDRESSRegister[0].lobyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET + 4:                  // 0xF004
                    d = m_DMAF_DMA_ADDRESSRegister[1].hibyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET + 5:                  // 0xF005
                    d = m_DMAF_DMA_ADDRESSRegister[1].lobyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET + 8:                  // 0xF008
                    d = m_DMAF_DMA_ADDRESSRegister[2].hibyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET + 9:                  // 0xF009
                    d = m_DMAF_DMA_ADDRESSRegister[2].lobyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET + 12:                 // 0xF00C
                    d = m_DMAF_DMA_ADDRESSRegister[3].hibyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET + 13:                 // 0xF00D
                    d = m_DMAF_DMA_ADDRESSRegister[3].lobyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_DMCNT_OFFSET + 0:                    // 0xF002
                    d = m_DMAF_DMA_DMCNTRegister[0].hibyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_DMCNT_OFFSET + 1:                    // 0xF003
                    d = m_DMAF_DMA_DMCNTRegister[0].lobyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_DMCNT_OFFSET + 4:                    // 0xF006
                    d = m_DMAF_DMA_DMCNTRegister[1].hibyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_DMCNT_OFFSET + 5:                    // 0xF007
                    d = m_DMAF_DMA_DMCNTRegister[1].lobyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_DMCNT_OFFSET + 8:                    // 0xF00A
                    d = m_DMAF_DMA_DMCNTRegister[2].hibyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_DMCNT_OFFSET + 9:                    // 0xF00B
                    d = m_DMAF_DMA_DMCNTRegister[2].lobyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_DMCNT_OFFSET + 12:                   // 0xF00E
                    d = m_DMAF_DMA_DMCNTRegister[3].hibyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_DMCNT_OFFSET + 13:                   // 0xF00F
                    d = m_DMAF_DMA_DMCNTRegister[3].lobyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_CHANNEL_OFFSET:                      // 0xF010

                    if ((m_DMAF_DMA_CHANNELRegister[0] & 0x80) == 0x80)
                        base.ClearInterrupt();

                    d = m_DMAF_DMA_CHANNELRegister[0];     // this turns off the DEND flag if it is set
                    m_DMAF_DMA_INTERRUPTRegister &= 0x7F;  // must do the Interrupt Control Register IRQ flag as well.

                    // after setting the return value any read of this register should clear the DEND flag (interrupt pending)

                    m_DMAF_DMA_CHANNELRegister[0] &= 0x7F;     // this turns off the DEND flag if it is set

                    // make sure the interrupt is already set by the timer before we set the high order bit of the DMA status register

                    //if (
                    //    ((Program._cpu.InterruptRegister & m_nInterruptMask) == m_nInterruptMask) &&
                    //    ((m_DMAF_DMA_INTERRUPTRegister & 0x01) == 0x01) &&
                    //    ((m_nBoardInterruptRegister & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA1) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA1)
                    //   )
                    //{
                    //    // clear interrupt on read of this register
                    //    d &= 0x7F;      
                    //}

                    ////if (!Program._cpu.BuildingDebugLine)
                    //lock (Program._cpu.buildingDebugLineLock)
                    //{
                    //    m_DMAF_DMA_CHANNELRegister[0] = (byte)(m_DMAF_DMA_CHANNELRegister[0] | 0x80);     // see note above
                    //    m_DMAF_DMA_INTERRUPTRegister  = (byte)(m_DMAF_DMA_INTERRUPTRegister  | 0x80);     // see note above
                    //    ClearInterrupt((int)DeviceInterruptMask.DEVICE_INT_MASK_DMA1);
                    //}
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_CHANNEL_OFFSET + 1:                  // 0xF011
                    if ((m_DMAF_DMA_CHANNELRegister[1] & 0x80) == 0x80)
                        base.ClearInterrupt();

                    d = m_DMAF_DMA_CHANNELRegister[1];

                    // after setting the return value any read of this register should clear the DEND flag (interrupt pending)

                    m_DMAF_DMA_CHANNELRegister[1] &= 0x7F;     // this turns off the DEND flag if it is set
                    m_DMAF_DMA_INTERRUPTRegister &= 0x7F;   // must do the Interrupt Control Register IRQ flag as well.

                    // make sure the interrupt is already set by the timer before we set the high order bit of the DMA status register

                    if (
                        ((Program._cpu.InterruptRegister & m_nInterruptMask) == m_nInterruptMask) &&
                        ((m_DMAF_DMA_INTERRUPTRegister & 0x02) == 0x02) &&
                        ((m_nBoardInterruptRegister & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA2) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA2)
                       )
                    {
                        // clear interrupt on read of this register
                        d &= 0x7F;
                    }

                    //if (!Program._cpu.BuildingDebugLine)
                    lock (Program._cpu.buildingDebugLineLock)
                    {
                        m_DMAF_DMA_CHANNELRegister[1] = (byte)(m_DMAF_DMA_CHANNELRegister[1] | 0x80);
                        m_DMAF_DMA_INTERRUPTRegister = (byte)(m_DMAF_DMA_INTERRUPTRegister   | 0x80);
                        ClearInterrupt((int)DeviceInterruptMask.DEVICE_INT_MASK_DMA2);
                    }
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_CHANNEL_OFFSET + 2:                  // 0xF012
                    if ((m_DMAF_DMA_CHANNELRegister[2] & 0x80) == 0x80)
                        base.ClearInterrupt();

                    d = m_DMAF_DMA_CHANNELRegister[2];     // this turns off the DEND flag if it is set
                    m_DMAF_DMA_INTERRUPTRegister &= 0x7F;  // must do the Interrupt Control Register IRQ flag as well.

                    // after setting the return value any read of this register should clear the DEND flag (interrupt pending)

                    m_DMAF_DMA_CHANNELRegister[2] &= 0x7F;     // this turns off the DEND flag if it is set

                    // make sure the interrupt is already set by the timer before we set the high order bit of the DMA status register

                    if (
                        ((Program._cpu.InterruptRegister & m_nInterruptMask) == m_nInterruptMask) &&
                        ((m_DMAF_DMA_INTERRUPTRegister & 0x04) == 0x04) &&
                        ((m_nBoardInterruptRegister & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA3) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA3)
                       )
                    {
                        // clear interrupt on read of this register
                        d &= 0x7F;
                    }

                    //if (!Program._cpu.BuildingDebugLine)
                    lock (Program._cpu.buildingDebugLineLock)
                    {
                        m_DMAF_DMA_CHANNELRegister[2] = (byte)(m_DMAF_DMA_CHANNELRegister[2] | 0x80);
                        m_DMAF_DMA_INTERRUPTRegister = (byte)(m_DMAF_DMA_INTERRUPTRegister   | 0x80);
                        ClearInterrupt((int)DeviceInterruptMask.DEVICE_INT_MASK_DMA3);
                    }
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_CHANNEL_OFFSET + 3:                  // 0xF013
                    if ((m_DMAF_DMA_CHANNELRegister[3] & 0x80) == 0x80)
                        base.ClearInterrupt();

                    d = m_DMAF_DMA_CHANNELRegister[3];     // this turns off the DEND flag if it is set
                    m_DMAF_DMA_INTERRUPTRegister &= 0x7F;  // must do the Interrupt Control Register IRQ flag as well.

                    // after setting the return value any read of this register should clear the DEND flag (interrupt pending)

                    m_DMAF_DMA_CHANNELRegister[3] &= 0x7F;     // this turns off the DEND flag if it is set

                    // make sure the interrupt is already set by the timer before we set the high order bit of the DMA status register

                    if (
                        ((Program._cpu.InterruptRegister & m_nInterruptMask) == m_nInterruptMask) &&
                        ((m_DMAF_DMA_INTERRUPTRegister & 0x08) == 0x08) &&
                        ((m_nBoardInterruptRegister & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA4) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA4)
                       )
                    {
                        // clear interrupt on read of this register
                        d &= 0x7F;
                    }

                    //if (!Program._cpu.BuildingDebugLine)
                    lock (Program._cpu.buildingDebugLineLock)
                    {
                        m_DMAF_DMA_CHANNELRegister[3] = (byte)(m_DMAF_DMA_CHANNELRegister[3] | 0x80);
                        m_DMAF_DMA_INTERRUPTRegister = (byte)(m_DMAF_DMA_INTERRUPTRegister   | 0x80);
                        ClearInterrupt((int)DeviceInterruptMask.DEVICE_INT_MASK_DMA4);
                    }
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_PRIORITY_OFFSET:                     // 0xF014
                    d = m_DMAF_DMA_PRIORITYRegister;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_INTERRUPT_OFFSET:                    // 0xF015
                    d = m_DMAF_DMA_INTERRUPTRegister;
                    if (
                        ((Program._cpu.InterruptRegister & m_nInterruptMask) == m_nInterruptMask) &&
                        (
                            (((m_DMAF_DMA_INTERRUPTRegister & 0x01) == 0x01) && ((m_nBoardInterruptRegister & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA1) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA1)) ||
                            (((m_DMAF_DMA_INTERRUPTRegister & 0x02) == 0x02) && ((m_nBoardInterruptRegister & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA2) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA2)) ||
                            (((m_DMAF_DMA_INTERRUPTRegister & 0x04) == 0x04) && ((m_nBoardInterruptRegister & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA3) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA3)) ||
                            (((m_DMAF_DMA_INTERRUPTRegister & 0x08) == 0x08) && ((m_nBoardInterruptRegister & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA4) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA4))
                        )
                       )
                    {
                        // say we are interrupting

                        d |= 0x80;
                    }
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_CHAIN_OFFSET:                        // 0xF016
                    d = m_DMAF_DMA_CHAINRegister;
                    break;
            }

            return ((byte)~d);
        }

        public override byte Read(ushort m)
        {
            byte returnValue = 0xff;

            DMAF_OffsetRegisters nWhichRegister = (DMAF_OffsetRegisters)(m - m_sBaseAddress);

            if ((nWhichRegister >= DMAF_OffsetRegisters.DMAF_STATREG_OFFSET && nWhichRegister <= DMAF_OffsetRegisters.DMAF_DRVREG_OFFSET) || nWhichRegister == DMAF_OffsetRegisters.DMAF_HLD_TOGGLE_OFFSET)
                returnValue = ReadFloppy(m);
            else if ((nWhichRegister >= DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET && nWhichRegister <= DMAF_OffsetRegisters.DMAF_DMA_CHAIN_OFFSET) || nWhichRegister == DMAF_OffsetRegisters.DMAF2_LATCH_OFFSET)
                returnValue = ReadDMA(m);
            else
                returnValue = Program._cpu.ReadFromFirst64K(m);   // memory read

            LogDMAFActivity((ushort)(m - m_sBaseAddress), returnValue, true);

            return returnValue;
        }

        byte PeekFloppy(ushort m) 
        {
            byte d = 0xFF;

            int nWhichRegister = m - m_sBaseAddress;
            int nDrive = 0;
            switch (m_DMAF_DRVRegister & 0x0F)
            {
                case 1:
                    nDrive = 0;
                    break;
                case 2:
                    nDrive = 1;
                    break;
                case 4:
                    nDrive = 2;
                    break;
                case 8:
                    nDrive = 3;
                    break;
            }

            switch ((DMAF_OffsetRegisters)nWhichRegister)
            {
                case DMAF_OffsetRegisters.DMAF_DATAREG_OFFSET:
                    d = m_DMAF_DATARegister;
                    break;
                case DMAF_OffsetRegisters.DMAF_STATREG_OFFSET:
                    d = m_DMAF_STATRegister;
                    break;

                case DMAF_OffsetRegisters.DMAF_DRVREG_OFFSET:
                    d =(byte)~ m_DMAF_DRVRegister;
                    break;

                case DMAF_OffsetRegisters.DMAF_TRKREG_OFFSET:
                    d = m_DMAF_TRKRegister;                      // get Track Register
                    break;

                case DMAF_OffsetRegisters.DMAF_SECREG_OFFSET:
                    d = m_DMAF_SECRegister;                      // get Track Register
                    break;

                case DMAF_OffsetRegisters.DMAF_HLD_TOGGLE_OFFSET:       // 0x50 - head load toggle
                    d = DMAF_HLD_TOGGLERegister;
                    break;
            }

            return d; 
        }

        byte PeekDMA(ushort m) 
        {
            byte d = 0xFF;
            int nWhichRegister = m - m_sBaseAddress;

            switch ((DMAF_OffsetRegisters)nWhichRegister)
            {
                // the value gets complemented on exit

                case DMAF_OffsetRegisters.DMAF2_LATCH_OFFSET:
                    if (latchRegisterIsInverted)
                        d = m_DMAF_LATCHRegister;                    // the Enable interrupt bit is in here somewhere
                    else
                        d = (byte)~m_DMAF_LATCHRegister;
                    break;

                //*
                //*   DMAF 6844 DMA controller definitions
                //*

                case DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET + 0:                  // 0xF000
                    d = m_DMAF_DMA_ADDRESSRegister[0].hibyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET + 1:                  // 0xF001
                    d = m_DMAF_DMA_ADDRESSRegister[0].lobyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET + 4:                  // 0xF004
                    d = m_DMAF_DMA_ADDRESSRegister[1].hibyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET + 5:                  // 0xF005
                    d = m_DMAF_DMA_ADDRESSRegister[1].lobyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET + 8:                  // 0xF008
                    d = m_DMAF_DMA_ADDRESSRegister[2].hibyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET + 9:                  // 0xF009
                    d = m_DMAF_DMA_ADDRESSRegister[2].lobyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET + 12:                 // 0xF00C
                    d = m_DMAF_DMA_ADDRESSRegister[3].hibyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET + 13:                 // 0xF00D
                    d = m_DMAF_DMA_ADDRESSRegister[3].lobyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_DMCNT_OFFSET + 0:                    // 0xF002
                    d = m_DMAF_DMA_DMCNTRegister[0].hibyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_DMCNT_OFFSET + 1:                    // 0xF003
                    d = m_DMAF_DMA_DMCNTRegister[0].lobyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_DMCNT_OFFSET + 4:                    // 0xF006
                    d = m_DMAF_DMA_DMCNTRegister[1].hibyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_DMCNT_OFFSET + 5:                    // 0xF007
                    d = m_DMAF_DMA_DMCNTRegister[1].lobyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_DMCNT_OFFSET + 8:                    // 0xF00A
                    d = m_DMAF_DMA_DMCNTRegister[2].hibyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_DMCNT_OFFSET + 9:                    // 0xF00B
                    d = m_DMAF_DMA_DMCNTRegister[2].lobyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_DMCNT_OFFSET + 12:                   // 0xF00E
                    d = m_DMAF_DMA_DMCNTRegister[3].hibyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_DMCNT_OFFSET + 13:                   // 0xF00F
                    d = m_DMAF_DMA_DMCNTRegister[3].lobyte;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_CHANNEL_OFFSET:                      // 0xF010
                    d = m_DMAF_DMA_CHANNELRegister[0];

                    // make sure the interrupt is already set by the timer before we set the high order bit of the DMA status register

                    if (
                        ((Program._cpu.InterruptRegister & m_nInterruptMask) == m_nInterruptMask) &&
                        ((m_DMAF_DMA_INTERRUPTRegister & 0x01) == 0x01) &&
                        ((m_nBoardInterruptRegister & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA1) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA1)
                       )
                        d |= 0x80;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_CHANNEL_OFFSET + 1:                  // 0xF011
                    d = m_DMAF_DMA_CHANNELRegister[1];

                    // make sure the interrupt is already set by the timer before we set the high order bit of the DMA status register

                    if (
                        ((Program._cpu.InterruptRegister & m_nInterruptMask) == m_nInterruptMask) &&
                        ((m_DMAF_DMA_INTERRUPTRegister & 0x02) == 0x02) &&
                        ((m_nBoardInterruptRegister & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA2) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA2)
                       )
                        d |= 0x80;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_CHANNEL_OFFSET + 2:                  // 0xF012
                    d = m_DMAF_DMA_CHANNELRegister[2];

                    // make sure the interrupt is already set by the timer before we set the high order bit of the DMA status register

                    if (
                        ((Program._cpu.InterruptRegister & m_nInterruptMask) == m_nInterruptMask) &&
                        ((m_DMAF_DMA_INTERRUPTRegister & 0x04) == 0x04) &&
                        ((m_nBoardInterruptRegister & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA3) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA3)
                       )
                        d |= 0x80;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_CHANNEL_OFFSET + 3:                  // 0xF013
                    d = m_DMAF_DMA_CHANNELRegister[3];

                    // make sure the interrupt is already set by the timer before we set the high order bit of the DMA status register

                    if (
                        ((Program._cpu.InterruptRegister & m_nInterruptMask) == m_nInterruptMask) &&
                        ((m_DMAF_DMA_INTERRUPTRegister & 0x08) == 0x08) &&
                        ((m_nBoardInterruptRegister & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA4) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA4)
                       )
                        d |= 0x80;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_PRIORITY_OFFSET:                     // 0xF014
                    d = m_DMAF_DMA_PRIORITYRegister;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_INTERRUPT_OFFSET:                    // 0xF015
                    d = m_DMAF_DMA_INTERRUPTRegister;
                    if (
                        ((Program._cpu.InterruptRegister & m_nInterruptMask) == m_nInterruptMask) &&
                        (
                            (((m_DMAF_DMA_INTERRUPTRegister & 0x01) == 0x01) && ((m_nBoardInterruptRegister & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA1) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA1)) ||
                            (((m_DMAF_DMA_INTERRUPTRegister & 0x02) == 0x02) && ((m_nBoardInterruptRegister & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA2) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA2)) ||
                            (((m_DMAF_DMA_INTERRUPTRegister & 0x04) == 0x04) && ((m_nBoardInterruptRegister & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA3) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA3)) ||
                            (((m_DMAF_DMA_INTERRUPTRegister & 0x08) == 0x08) && ((m_nBoardInterruptRegister & (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA4) == (int)DeviceInterruptMask.DEVICE_INT_MASK_DMA4))
                        )
                       )
                        d |= 0x80;
                    break;

                case DMAF_OffsetRegisters.DMAF_DMA_CHAIN_OFFSET:                        // 0xF016
                    d = m_DMAF_DMA_CHAINRegister;
                    break;
            }
            
            // return  one's complement

            return (byte)~d;
        }

        // need Peek function for board Peek and individual Peek Functions for device Peeks
        public override byte Peek(ushort m)
        {
            DMAF_OffsetRegisters nWhichRegister = (DMAF_OffsetRegisters)(m - m_sBaseAddress);

            if ((nWhichRegister >= DMAF_OffsetRegisters.DMAF_STATREG_OFFSET && nWhichRegister <= DMAF_OffsetRegisters.DMAF_DRVREG_OFFSET) || nWhichRegister == DMAF_OffsetRegisters.DMAF_HLD_TOGGLE_OFFSET)
                return PeekFloppy(m);
            else if ((nWhichRegister >= DMAF_OffsetRegisters.DMAF_DMA_ADDRESS_OFFSET && nWhichRegister <= DMAF_OffsetRegisters.DMAF_DMA_CHAIN_OFFSET) || nWhichRegister == DMAF_OffsetRegisters.DMAF2_LATCH_OFFSET)
                return PeekDMA(m);
            else
                return Program._cpu.ReadFromFirst64K(m);   // memory read
        }

        public override void Init(int nWhichController, byte[] sMemoryBase, ushort sBaseAddress, int nRow, bool bInterruptEnabled)
        {
            m_nRow = nRow;
            base.Init(nWhichController, sMemoryBase, sBaseAddress, nRow, bInterruptEnabled);

            m_DMAF_STATRegister    = 0;    // 0x20 Status Register
            m_DMAF_CMDRegister     = 0;    // 0x20 Command Register
            m_DMAF_TRKRegister     = 0;    // 0x21 Track Register
            m_DMAF_SECRegister     = 0;    // 0x22 Sector Register
            m_DMAF_DATARegister    = 0;    // 0x23 Data Register
            m_DMAF_DRVRegister     = 0;    // 0x24 Which Drive is selected, side, density and head load one shot toggle.

            m_DMAF_LATCHRegister   = 0;    // 0x40 Latch

            for (int i = 0; i < 4; i++)
            {
                m_DMAF_DMA_ADDRESSRegister[i] = new DMAF_DMA_ADDRESS_REGISTER();
                m_DMAF_DMA_ADDRESSRegister[i].hibyte = 0x00;
                m_DMAF_DMA_ADDRESSRegister[i].lobyte = 0x00;

                m_DMAF_DMA_DMCNTRegister[i] = new DMAF_DMA_BYTECNT_REGISTER();
                m_DMAF_DMA_DMCNTRegister[i].hibyte = 0x00;
                m_DMAF_DMA_DMCNTRegister[i].lobyte = 0x00;
            }

            for (int i = 0; i < 4; i++)
            {
                m_DMAF_DMA_CHANNELRegister[i] = 0x00;
            }

            m_DMAF_DMA_PRIORITYRegister       = 0x00;
            m_DMAF_DMA_INTERRUPTRegister      = 0x00;
            m_DMAF_DMA_CHAINRegister          = 0x00;

            m_nAllowMultiSectorTransfers = Program.GetConfigurationAttribute("Global/AllowMultipleSector",      "value",  0) == 0 ? false : true;

            StartDMAFTimer(m_nDMAFRate);
            m_nDMAFRunning = true;
        }
    }
}
