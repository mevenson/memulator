using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;

using System.IO;

namespace Memulator
{
    class PCStream : IODevice
    {
        // we can only transition the file mode from 0 to any of the file modes below. This is a safety precaution. Once the file
        // mode is set it cannot change until a port reset which insures that all open files have been closed and we are ready
        // to initiate a new stream transfer. Bottom line - when done with the stream - re-init it with command 0x00 to the
        // command/comtrol port. 
        //
        // if the file is not yet open, characters go into filename once the file name has been terminated, the next character
        // specifies the the mode - read, write, append or delete
        //
        //      0x00 = not set
        //      0x01 = read binary
        //      0x02 = read text
        //      0x03 = write binary
        //      0x04 = write text
        //      0x05 = append binary
        //      0x06 = append text
        //      0x07 = delete
        //      0x08 = close
        //      0x10 = return directory listing
        //
        //      assuming the board is at address $8060:
        //
        //          to get directory from PC"
        //
        //              write 0x00 to $8060         -- this insures that the port board is in an initialized state
        //              write 0x03 to $8060         -- set up for specifying the root to start getting directory at
        //              <path>     to $8061         -- one byte at a time - if just getting directory of current path - skip
        //              write 0x00 to $8061         -- terminates setting path
        //              write 0x10 tp $8061         -- command to read directory
        //
        //                  now read data returned from PCStream until status = 0x04 (End of Data)
        //
        //              read $8060                  -- get status
        //              while status != 0x00
        //              {
        //                  read $8061              -- read byte from stream
        //                  read $8060              -- get status again
        //              }
        //              write 0x00 to $8060         -- this insures that the port board is in an initialized state

        string m_strPath;
        string m_strDirectoryListing;
        int m_nDirectoryCharacterPointer;

        byte m_cCommand;
        byte m_cStatus;
        byte m_cData;

        byte m_cLastCharacterRead;

        int m_nDataPortMode;
        int m_nFileMode;
        bool m_bWriting;
        bool m_bFilenameIsSet;
        bool m_bConvertLineFeedToCarriageReturnOnRead;
        bool m_bThrowAwayLineFeedsOnRead;
        bool m_bAddLineFeedOnCarriageReturn;
        bool m_bExpandTabToSpaces;
        bool m_bExpanding = false;

        string currentDirectory;
        string applicationDirectory;

        StreamReader m_fpReaderText;
        StreamWriter m_fpWriterText;
        BinaryReader m_fpReaderBinary;
        BinaryWriter m_fpWriterBinary;

        StreamWriter m_fpLogFile;
        bool logActivity;

        public PCStream ()
        {
            logActivity = false;

            m_fpReaderText = null;
            m_fpWriterText = null;
            m_fpReaderBinary = null;
            m_fpWriterBinary = null;

            m_bWriting = false;
            m_bFilenameIsSet                         = false;
            m_bConvertLineFeedToCarriageReturnOnRead = false;
            m_bThrowAwayLineFeedsOnRead              = false;
            m_bAddLineFeedOnCarriageReturn           = true;
            m_bExpandTabToSpaces                     = false;
            m_nFileMode                              = 0;
            m_nDataPortMode                          = 0;
            m_nDirectoryCharacterPointer             = 0;

            m_strDirectoryListing = "";
            currentDirectory = Directory.GetCurrentDirectory();
            applicationDirectory = currentDirectory;

            if (logActivity)
            {
                m_fpLogFile = new StreamWriter(File.Open(@"D:\C#_PCStreamLogFile.txt", FileMode.Append, FileAccess.Write));
            }
        }

        ~PCStream()
        {
            //if (m_fpLogFile != null)
            //    m_fpLogFile.Close();
        }

        public void LogActivity(string activityMessage)
        {
            if (logActivity && m_fpLogFile != null)
            {
                m_fpLogFile.Write(string.Format("{0}", activityMessage));
                m_fpLogFile.Flush();
            }
        }

        public override void Init(int nWhichController, byte[] sMemoryBase, ushort sBaseAddress, int nRow, bool bInterruptEnabled)
        {
            m_strPath = "";

            m_fpReaderText = null;
            m_fpWriterText = null;
            m_fpReaderBinary = null;
            m_fpWriterBinary = null;

            m_bFilenameIsSet = false;
            m_bConvertLineFeedToCarriageReturnOnRead = false;
            m_bThrowAwayLineFeedsOnRead              = false;
            m_bAddLineFeedOnCarriageReturn           = true;
            m_bExpandTabToSpaces                     = false;
            m_bWriting                               = false;
            m_nFileMode                              = 0;
            m_nDataPortMode                          = 0;
            m_nDirectoryCharacterPointer             = 0;

            m_strDirectoryListing = "";
        }

        private void CloseAllOpenFiles ()
        {
            if (m_fpReaderText   != null) { m_fpReaderText.Close();   m_fpReaderText   = null; }
            if (m_fpWriterText   != null) { m_fpWriterText.Close();   m_fpWriterText   = null; }
            if (m_fpReaderBinary != null) { m_fpReaderBinary.Close(); m_fpReaderBinary = null; }
            if (m_fpWriterBinary != null) { m_fpWriterBinary.Close(); m_fpWriterBinary = null; }
        }

        enum PORT
        {
            CommandStatus = 0,
            Data = 1
        }

        public override void Write(ushort register, byte b)
        {
            bool nWriteCommandControl = false;

            string activityMessage = string.Format("Write to {0} with {1}\r\n", register.ToString("X4"), b.ToString("X2"));
            LogActivity(activityMessage);

            switch (register & 0x07)
            {
                case (ushort)PORT.CommandStatus:       // 0x00:  // CONTROL PORT
                    m_cCommand = b;

                    // Commands with the high order bit clear are for setting up the read side of things.
                    // To set up the write end of the business, send commands with the high order bit set.
                    //
                    // In order to be able to write to the data port as both a command type register and 
                    // a data type register, we will have a Command that will set the mode. To set for writing 
                    // to the command/control register of the dataport send command 0x03. To set for writing 
                    // data to the data stream the command is 0x04. This only makes sense for using the port 
                    // to write to streams since all data written to the data port while reading a stream is 
                    // command/control info. The default after a reset is to write to the command/control register.

                    nWriteCommandControl = (m_cCommand & 0x80) == 0 ? false : true;       // if command has high order bit set do write settings
                                                                    // else do read settings
                    switch (m_cCommand)
                    {
                        case 0x00:                                  // 0x00 command = reset Path and reinit Port - applies to both read and write
                            m_strPath = "";
                            CloseAllOpenFiles();

                            // now reset everything else

                            m_bFilenameIsSet = false;
                            if (!nWriteCommandControl)
                            {
                                m_bConvertLineFeedToCarriageReturnOnRead = false;
                                m_bThrowAwayLineFeedsOnRead = false;
                                m_bAddLineFeedOnCarriageReturn = true;
                            }
                            m_bExpanding = false;
                            m_bWriting = false;
                            m_nFileMode     = 0;
                            m_nDataPortMode = 0;
                            m_cStatus       = 0x00;
                            break;

                        case 0x01:                                  // 0x01 command - when reading - convert Line feed to carriage return - no affect on writing
                            if (!nWriteCommandControl)
                                m_bConvertLineFeedToCarriageReturnOnRead = true;
                            break;

                        case 0x02:
                            if (!nWriteCommandControl)          // 0x02 command - when reading - discard line feeds on read and add line feed to carriage return
                            {
                                m_bThrowAwayLineFeedsOnRead = true;
                            }
                            break;

                            // this is used to set up filename - setting m_strPath

                        case 0x03:                                  //  set data port mode when writing to writing to controller control registers
                            m_nDataPortMode = 0;                    //  for specifying the filename to write to.
                            break;

                            // this actually allows writing to the PC file system

                        case 0x04:
                            m_nDataPortMode = 1;                    // set up for actually writng to the actual data stream (file opened for write)
                            break;                                  // this command must be sent before actual writing to PC is allowed
                                                                    // otherwise writing will continue to go to command/control port
                        case 0x05:
                            if (nWriteCommandControl)               // only allow space compression/expansion on write.
                            {
                                // if we are are writing to the PC file stream - expand tab/count to actuall spaces.

                                m_bExpandTabToSpaces = true;
                            }
                            break;

                        case 0x06:
                            if (nWriteCommandControl)               // only allow add lf to cr on write.
                                m_bAddLineFeedOnCarriageReturn = true;
                            break;

                        default:
                            break;
                    }
                    break;

                // if the file is not yet open, characters go into filename
                // once the file name has been terminated, the next character
                // specifies the the mode - read, write, append or delete
                //
                //      0x00 = not set
                //      0x01 = read binary
                //      0x02 = read text
                //      0x03 = write binary
                //      0x04 = write text
                //      0x05 = append binary
                //      0x06 = append text
                //      0x07 = delete
                //      0x08 = close
                //      0x09 = get current working directory
                //      0x10 = return directory listing
                //      0x11 = change working directory
                //      0x12 = reset current working directory to the applications current working directory

                case (ushort)PORT.Data:                     // 0x01:
                    m_cData = b;
                    if (m_nDataPortMode == 0)               // writing to controller in command/control mode. (!0 = data mode)
                    {
                        if (!m_bFilenameIsSet)
                        {
                            if (m_cData == 0x00)            // when the user terminates the string set the flag that the filename has been set
                            {
                                m_bFilenameIsSet = true;
                            }
                            else
                                m_strPath += (char)m_cData;

                            m_cStatus = 0x00;
                        }
                        else
                        {
                            // we can only transition the file mode from 0 to any of the file modes below. This is a safety precaution. Once the file
                            // mode is set it cannot change until a port reset which insures that all open files have been closed and we are ready
                            // to initiate a new stream transfer. Bottom line - when done with the stream - re-init it with command 0x00 to the
                            // command/comtrol port. 

                            if (m_nFileMode == 0)           // can only transition file mode from 0
                            {
                                m_nFileMode = b;
                                m_cStatus = 0x00;

                                //open the file specified and prepare for data reading/writing or delete or close a file or get a directory listing

                                string strMode = "";
                                switch (m_nFileMode)
                                {
                                    case 0x01:
                                        strMode = "rb";
                                        break;

                                    case 0x02:
                                        strMode = "r";
                                        break;

                                    case 0x03:
                                        strMode = "wb";
                                        break;

                                    case 0x04:
                                        strMode = "w";
                                        break;

                                    case 0x05:
                                        strMode = "ab";
                                        break;

                                    case 0x06:
                                        strMode = "a";
                                        break;

                                    case 0x07:          // delete file
                                        {
                                            CloseAllOpenFiles();

                                            File.Delete(m_strPath);
                                            m_strPath = "";
                                            m_bFilenameIsSet = false;
                                            m_bConvertLineFeedToCarriageReturnOnRead = false;
                                            m_bThrowAwayLineFeedsOnRead = false;
                                            m_bWriting = false;
                                            m_nFileMode = 0;
                                            m_nDataPortMode = 0;
                                            m_cStatus = 0x00;
                                        }
                                        break;

                                    case 0x08:          // close file
                                        {
                                            CloseAllOpenFiles();
                                            m_strPath = "";
                                            m_bFilenameIsSet = false;
                                            m_bConvertLineFeedToCarriageReturnOnRead = false;
                                            m_bThrowAwayLineFeedsOnRead = false;
                                            m_bWriting = false;
                                            m_nFileMode = 0;
                                            m_nDataPortMode = 0;
                                            m_cStatus = 0x00;
                                        }
                                        break;

                                    case 0x10:          // do directory of path in strPath
                                        break;

                                    case 0x11:          // change working directory
                                        break;

                                    case 0x12:          // reset current working directory to application's working directory
                                        break;

                                    default:
                                        m_cStatus = 0x01;
                                        break;
                                }
                                if (m_nFileMode < 0x07)
                                {
                                    m_cStatus = 0x02;       // default to failure;
                                    try
                                    {
                                        if (m_nFileMode != 0)
                                        {
                                            if ((!m_strPath.StartsWith(@"\\")) && (m_strPath[1] != ':'))
                                            {
                                                // relative path

                                                m_strPath = Path.Combine(currentDirectory, m_strPath);
                                            }
                                            
                                            m_cLastCharacterRead = 0x00;

                                            switch (m_nFileMode)
                                            {
                                                case 0x01:      // read binary
                                                    if ((m_fpReaderBinary = new BinaryReader(File.Open(m_strPath, FileMode.Open, FileAccess.Read))) != null) m_cStatus = 0x00;
                                                    m_bWriting = false;
                                                    break;
                                                case 0x02:      // read text
                                                    if ((m_fpReaderText = new StreamReader(File.Open(m_strPath, FileMode.Open, FileAccess.Read))) != null) m_cStatus = 0x00;
                                                    m_bWriting = false;
                                                    break;
                                                case 0x03:      // write binary
                                                    if (File.Exists(m_strPath))
                                                        File.Delete(m_strPath);
                                                    if ((m_fpWriterBinary = new BinaryWriter(File.Open(m_strPath, FileMode.Create, FileAccess.Write))) != null) m_cStatus = 0x00;
                                                    m_bWriting = true;
                                                    break;
                                                case 0x04:      // write text
                                                    if (File.Exists(m_strPath))
                                                        File.Delete(m_strPath);
                                                    if ((m_fpWriterText = new StreamWriter(File.Open(m_strPath, FileMode.Create, FileAccess.Write))) != null) m_cStatus = 0x00;
                                                    m_bWriting = true;
                                                    break;
                                                case 0x05:      // append binary
                                                    if ((m_fpWriterBinary = new BinaryWriter(File.Open(m_strPath, FileMode.Append, FileAccess.Write))) != null) m_cStatus = 0x00;
                                                    m_bWriting = true;
                                                    break;
                                                case 0x06:      // append text
                                                    if ((m_fpWriterText = new StreamWriter(File.Open(m_strPath, FileMode.Append, FileAccess.Write))) != null) m_cStatus = 0x00;
                                                    m_bWriting = true;
                                                    break;
                                            }
                                        }
                                    }
                                    catch
                                    {

                                    }
                                }
                                else if (m_nFileMode == 0x09)
                                {
                                    m_strDirectoryListing = currentDirectory;
                                    m_nDirectoryCharacterPointer = 0;
                                }
                                else if (m_nFileMode == 0x10)
                                {
                                    m_strDirectoryListing = "";
                                    m_nDirectoryCharacterPointer = 0;

                                    // get a directory listing

                                    try
                                    {
                                        if (m_strPath.Length == 0)
                                            m_strPath = currentDirectory;
                                        
                                        m_strDirectoryListing = string.Format("Directory of: {0}\r\n", currentDirectory);

                                        string[] directories = Directory.GetDirectories(m_strPath, "*");
                                        foreach (string directory in directories)
                                        {
                                            DirectoryInfo di = new DirectoryInfo(directory);
                                            string strFileTimeLastModified = di.LastWriteTime.ToString("MM/dd/yyyy HH:mm:ss");

                                            string strDirEntry = string.Format("D|{0}|{1}|{2}\r\n", di.FullName.Replace(currentDirectory, "").PadRight(16), 0.ToString().PadLeft(8), strFileTimeLastModified);
                                            m_strDirectoryListing += strDirEntry;

                                        }

                                        string[] files = Directory.GetFiles(m_strPath, "*");
                                        foreach (string file in files)
                                        {
                                            FileInfo fi = new FileInfo(file);
                                            string strFileTimeLastModified = fi.LastWriteTime.ToString("MM/dd/yyyy HH:mm:ss");

                                            string strDirEntry = string.Format("F|{0}|{1}|{2}\r\n", Path.GetFileName(fi.FullName).PadRight(16), (fi.Length.ToString()).PadLeft(8), strFileTimeLastModified);
                                            m_strDirectoryListing += strDirEntry;
                                        }
                                    }
                                    catch
                                    {
                                        m_cStatus = 0x02;       // error getting diectory
                                    }
                                }
                                else if (m_nFileMode == 0x11)
                                {
                                    // change the default working directory for dir and file read/write

                                    if (m_strPath.Length == 0)
                                        m_strPath = currentDirectory;
                                    else
                                    {
                                        if ((m_strPath.StartsWith(@"\\")) || (m_strPath[1] == ':'))
                                        {
                                            // this is a unc path or an absolute drive path
                                            currentDirectory = m_strPath;
                                        }
                                        else
                                        {
                                            // relative path

                                            currentDirectory += string.Format(@"\{0}", m_strPath);
                                        }
                                    }
                                }
                                else if (m_nFileMode == 0x12)
                                {
                                    // reset current working directory to application's working directory

                                    currentDirectory = applicationDirectory;
                                }
                            }
                        }
                    }
                    else
                    {
                        // write byte to file.
                        if (m_fpWriterBinary != null)
                        {
                            m_fpWriterBinary.Write(m_cData);
                            m_cStatus = 0x00;
                        }
                        else if (m_fpWriterText != null)
                        {
                            if (m_bExpandTabToSpaces)
                            {
                                if (m_bExpanding)
                                {
                                    for (int i = 0; i < m_cData; i++)
                                        m_fpWriterText.Write(' ');
                                    m_bExpanding = false;
                                }
                                else if (m_cData == 0x09)
                                {
                                    m_bExpanding = true;
                                }
                            }
                            else
                            {
                                m_fpWriterText.Write((char)m_cData);
                            }
                            m_cStatus = 0x00;

                        }
                    }
                    break;

                default:
                    break;
            }

        }

        public override byte Read(ushort register)
        {
            string activityMessage = string.Format("Read from {0} ", register.ToString("X4"));
            LogActivity(activityMessage);

            byte value = 0xff;
            bool endOfFileReached = false;

            switch (register & 0x07)
            {
                case (ushort)PORT.CommandStatus:       // 0x00:                  // STATUS PORT
                    value = m_cStatus;
                    break;

                case (ushort)PORT.Data:                 // 0x01:
                    if (m_nFileMode <= 0x06)
                    {
                        //      0x01 = read binary
                        //      0x02 = read text

                        // get the data from the binary or text stream for processing

                        switch (m_nFileMode)
                        {
                            case 0x01:
                                if (m_fpReaderBinary.BaseStream.Position != m_fpReaderBinary.BaseStream.Length)
                                    m_cData = m_fpReaderBinary.ReadByte();
                                else
                                {
                                    endOfFileReached = true;
                                    m_cStatus = 4;          // signal end of file.
                                }
                                break;
                            case 0x02:
                                if (!m_fpReaderText.EndOfStream)
                                    m_cData = (byte)m_fpReaderText.Read();
                                else
                                {
                                    endOfFileReached = true;
                                    m_cStatus = 4;          // signal end of file.
                                }
                                break;
                        }

                        if (!endOfFileReached)
                        {
                            m_cStatus = 0;      // default to success

                            // if it's text mode and the character is a line feed throw it away
                            if ((m_nFileMode < 5) && ((m_nFileMode % 2) == 0) && (m_cData == '\n'))
                            {
                                if (m_bConvertLineFeedToCarriageReturnOnRead)
                                {
                                    // if we are staring at a line feed and the previous cahracter is not carriage return - convert this LF to CR
                                    // otherwise, we need to just discard it by just getting the next character from the stream.

                                    if (m_cLastCharacterRead != 0x0d)
                                        value = 0x0d;
                                    else
                                    {
                                        // we are processing a line feed and we were told to discard them - so get next non-lf character

                                        while ((m_cData = (byte)m_fpReaderText.Read()) == '\n')
                                        {
                                            if (m_fpReaderText.EndOfStream)
                                            {
                                                endOfFileReached = true;
                                                m_cStatus = 4;          // signal end of file.
                                                break;
                                            }
                                        }
                                        value = m_cData;    // return next non LF character
                                    }
                                }

                                // the character we are about to return is a line feed - if we need to discard them, we need to get the next character form the stream

                                else if (m_bThrowAwayLineFeedsOnRead)
                                {
                                    while ((m_cData = (byte)m_fpReaderText.Read()) == '\n')
                                    {
                                        if (m_fpReaderText.EndOfStream)
                                        {
                                            endOfFileReached = true;
                                            m_cStatus = 4;          // signal end of file.
                                            break;
                                        }
                                    }
                                    value = m_cData;    // return next non LF character
                                }
                                else
                                    value = m_cData;
                            }
                            else
                                value = m_cData;

                            m_cLastCharacterRead = m_cData;
                        }
                        else
                        {
                            m_nFileMode = 0;        // reset the filemode so we can send another
                            value = 0x00;
                            m_cStatus = 4;          // signal end of file.
                        }
                    }
                    else
                    {
                        if ((m_nFileMode == 0x10) || (m_nFileMode == 0x09))            // Get directory listing
                        {
                            if (m_strDirectoryListing.Length > m_nDirectoryCharacterPointer)
                            {
                                value = (byte)m_strDirectoryListing[m_nDirectoryCharacterPointer++];
                            }
                            else    // we are done
                            {
                                m_strDirectoryListing= "";
                                m_nDirectoryCharacterPointer = 0;

                                m_nFileMode = 0;        // reset the filemode so we can send another
                                value = 0x00;
                                m_cStatus = 4;          // signal end of file.
                            }
                        }
                    }
                    break;

                default:
                    break;

            }

            activityMessage = string.Format("{0}\r\n", value.ToString("X2"));
            LogActivity(activityMessage);

            return value;
        }

        public override byte Peek(ushort register)
        {
            string activityMessage = string.Format("Read from {0} ", register.ToString("X4"));
            LogActivity(activityMessage);
            byte value = 0xff;
            bool endOfFileReached = false;
            switch (register & 0x07)
            {
                case (ushort)PORT.CommandStatus:       // 0x00:                  // STATUS PORT
                    value = m_cStatus;
                    break;
                case (ushort)PORT.Data:                 // 0x01:
                    if (m_nFileMode <= 0x06)
                    {
                        switch (m_nFileMode)
                        {
                            case 0x01:
                                if (m_fpReaderBinary.BaseStream.Position != m_fpReaderBinary.BaseStream.Length)
                                {
                                    long position = m_fpReaderBinary.BaseStream.Position;
                                    m_cData = m_fpReaderBinary.ReadByte();
                                    m_fpReaderBinary.BaseStream.Seek(position, SeekOrigin.Begin);
                                }
                                else
                                {
                                    endOfFileReached = true;
                                }
                                break;
                            case 0x02:
                                if (!m_fpReaderText.EndOfStream)
                                {
                                    long position = m_fpReaderBinary.BaseStream.Position;
                                    m_cData = (byte)m_fpReaderText.Read();
                                    m_fpReaderBinary.BaseStream.Seek(position, SeekOrigin.Begin);
                                }
                                else
                                {
                                    endOfFileReached = true;
                                }
                                break;
                        }
                        if (!endOfFileReached)
                        {
                            if ((m_nFileMode < 5) && ((m_nFileMode % 2) == 0) && (m_cData == '\n'))
                            {
                                if (m_bConvertLineFeedToCarriageReturnOnRead)
                                {
                                    if (m_cLastCharacterRead != 0x0d)
                                        value = 0x0d;
                                    else
                                    {
                                        long position = m_fpReaderBinary.BaseStream.Position;
                                        while ((m_cData = (byte)m_fpReaderText.Read()) == '\n')
                                        {
                                            if (m_fpReaderText.EndOfStream)
                                            {
                                                endOfFileReached = true;
                                                break;
                                            }
                                        }
                                        m_fpReaderBinary.BaseStream.Seek(position, SeekOrigin.Begin);
                                        value = m_cData;    // return next non LF character
                                    }
                                }
                                else if (m_bThrowAwayLineFeedsOnRead)
                                {
                                    long position = m_fpReaderBinary.BaseStream.Position;
                                    while ((m_cData = (byte)m_fpReaderText.Read()) == '\n')
                                    {
                                        if (m_fpReaderText.EndOfStream)
                                        {
                                            endOfFileReached = true;
                                            break;
                                        }
                                    }
                                    m_fpReaderBinary.BaseStream.Seek(position, SeekOrigin.Begin);
                                    value = m_cData;    // return next non LF character
                                }
                                else
                                    value = m_cData;
                            }
                            else
                                value = m_cData;
                            m_cLastCharacterRead = m_cData;
                        }
                        else
                        {
                            value = 0x00;
                        }
                    }
                    else
                    {
                        if ((m_nFileMode == 0x10) || (m_nFileMode == 0x09))            // Get directory listing
                        {
                            if (m_strDirectoryListing.Length > m_nDirectoryCharacterPointer)
                            {
                                value = (byte)m_strDirectoryListing[m_nDirectoryCharacterPointer++];
                            }
                            else    // we are done
                            {
                                m_strDirectoryListing = "";
                                m_nDirectoryCharacterPointer = 0;
                                value = 0x00;
                            }
                        }
                    }
                    break;
                default:
                    break;
            }
            activityMessage = string.Format("{0}\r\n", value.ToString("X2"));
            LogActivity(activityMessage);
            return value;
        }
        //public override void Configure(int nBaseAddress, CString szBaseAddress, int sizeofBaseAddress)
        //{

        //}
    }
}
