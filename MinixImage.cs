using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Xml;
using System.Windows.Forms;

using System.Diagnostics;

namespace Memulator
{
    public class MinixImage
    {
        /* Tables sizes */

        #region Constants
        public const int NR_ZONE_NUMS =  9;	/* # zone numbers in an inode */
        public const int NR_BUFS      = 30;	/* # blocks in the buffer cache */
        public const int NR_BUF_HASH  = 32;	/* size of buf hash table; MUST BE POWER OF 2*/
        public const int NR_FDS       = 20;	/* max file descriptors per process */
        public const int NR_FILPS     = 66;	/* # slots in filp table */
        public const int I_MAP_SLOTS  =  6;	/* max # of blocks in the inode bit map */
        public const int ZMAP_SLOTS   =  8;	/* max # of blocks in the zone bit map */
        public const int NR_INODES    = 32;	/* # slots in "in core" inode table */
        public const int NR_SUPERS    =  7;	/* # slots in super block table */
        public const int NAME_SIZE    = 14;    /* # bytes in a directory component */

        // Inode table.  This table holds inodes that are currently in use.  In some
        // cases they have been opened by an open() or creat() system call, in other
        // cases the file system itself needs the inode for one reason or another,
        // such as to search a directory for a path name.
        // The first part of the struct holds fields that are present on the
        // disk; the second part holds fields not present on the disk.
        // The disk inode part is also declared in "type.h" as 'd_inode'.

        /* Flag bits for i_mode in the inode. */

        public ushort I_TYPE            = 0xF000; // octal = 0170000	/* this field gives inode type */
        public ushort I_SOCKET          = 0xC000; // octal = 0140000    /* Socket */
        public ushort I_SYMBLINK        = 0xA000; // octal = 0120000    /* Symbolic Link */
        public ushort I_REGULAR         = 0x8000; // octal = 0100000	/* regular file, not dir or special */
        public ushort I_BLOCK_SPECIAL   = 0x6000; // octal = 0060000	/* block special file */
        public ushort I_DIRECTORY       = 0x4000; // octal = 0040000	/* file is a directory */
        public ushort I_CHAR_SPECIAL    = 0x2000; // octal = 0020000	/* character special file */
        public ushort i_NAMEDPIPE       = 0x1000; // octal = 0010000    /* FIFO / Named Pipe */

        public const int I_SET_UID_BIT     = 0x0800; // octal = 0004000	/* set effective uid on exec */
        public const int I_SET_GID_BIT     = 0x0400; // octal = 0002000	/* set effective gid on exec */
        public const int I_STICKY_BIT      = 0x0200; // octal = 0001000    /* Sticky Bit */

        public const int ALL_MODES         = 0x0DFF; // octal = 0006777	/* all bits for user, group and others */
        public const int RWX_MODES         = 0x01FF; // octal = 0000777	/* mode bits for RWX only */

        public const int S_IRUSR           = 0x0100; // octal = 0000400	/* mode bits for Rwx protection bit User */;
        public const int S_IWUSR           = 0x0080; // octal = 0000200	/* mode bits for rWx protection bit User */;
        public const int S_IXUSR           = 0x0040; // octal = 0000100	/* mode bits for rwX protection bit User */;

        public const int S_IRGRP           = 0x0020; // octal = 0000040	/* mode bits for Rwx protection bit Group */;
        public const int S_IWGRP           = 0x0010; // octal = 0000020	/* mode bits for rWx protection bit Group */;
        public const int S_IXGRP           = 0x0008; // octal = 0000010	/* mode bits for rwX protection bit Group */;

        public const int S_IROTH           = 0x0004; // octal = 0000004	/* mode bits for Rwx protection bit Other */;
        public const int S_IWOTH           = 0x0002; // octal = 0000002	/* mode bits for rWx protection bit Other */;
        public const int S_IXOTH           = 0x0001; // octal = 0000001	/* mode bits for rwX protection bit Other */;

        public const int R_BIT             = 0x0004; // octal = 0000004	/* Rwx protection bit */
        public const int W_BIT             = 0x0002; // octal = 0000002	/* rWx protection bit */
        public const int X_BIT             = 0x0001; // octal = 0000001	/* rwX protection bit */
        public const int I_NOT_ALLOC       = 0x0000; // octal = 0000000	/* this inode is free */

        /* Field values.  Note that CLEAN and DIRTY are defined in "const.h" */

        public const int NO_PIPE = 0;	/* i_pipe is NO_PIPE if inode is not a pipe */
        public const int I_PIPE = 1;	/* i_pipe is I_PIPE if inode is a pipe */
        public const int NO_MOUNT = 0;	/* i_mount is NO_MOUNT if file not mounted on */
        public const int I_MOUNT = 1;	/* i_mount is I_MOUNT if file mounted on */
        public const int NO_SEEK = 0;	/* i_seek = NO_SEEK if last op was not SEEK */
        public const int ISEEK = 1;	/* i_seek = ISEEK if last op was SEEK */

        #endregion

        #region Classes
        // example partial inode block from the 360K minix root diskette image
        //
        // i_mode   i_uid   i_size       i_modtime      i_gid   i_nlinks    zone numbers for direct, ind, and dbl ind
        //                                                                    0   offset    1      2      3      4      5      6     ind   dblind
        // 41 FF    00 02   00 00 00 90  25 1B F7 CE    02      09          00 07 (1C00)  00 00  00 00  00 00  00 00  00 00  00 00  00 00  00 00 <- this is a directory with RWX for all three goups
        // 41 ED    00 02   00 00 01 60  25 1D 2A F2    02      02          00 08 (2000)  00 00  00 00  00 00  00 00  00 00  00 00  00 00  00 00 <- this is a directory with RWX for rwxr_xr_x
        // 81 ED    00 02   00 00 09 72  22 29 E9 7B    02      01          00 09 (2400)  00 0A  00 0B  00 00  00 00  00 00  00 00  00 00  00 00 <- this is a regular file with RWX for rwxr_xr_x
        // 81 ED    00 02   00 00 0D 2A  22 29 E9 7B    02      01          00 0C (3000)  00 0D  00 0E  00 0F  00 00  00 00  00 00  00 00  00 00 <- this is a regular file with RWX for rwxr_xr_x

        public class Inode
        {
            public ushort i_mode;                               // file type, protection, etc.                  File type and RWX bit
            public ushort i_uid;                                // user id of the file's owner                  Identifies the user who owns the file
            public ulong  i_size;                               // current file size in bytes                   Number of bytes in the file
            public ulong  i_modtime;                            // when was file data last changed              In seconds, since Jan. 1, 1970
            public byte   i_gid;                                // group number                                 
            public byte   i_nlinks;                             // how many links to this file 
            public ushort [] i_zone = new ushort[NR_ZONE_NUMS]; // zone numbers for direct, ind, and dbl ind    Zone numbers for the first 7 data sones in the file
                                                                //                                              ind, and dbl ind are only used for files larger than 7 zones

            /* The following items are not present on the disk. */

            public ushort i_dev;    /* which device is the inode on */
            public ushort i_num;    /* inode number on its (minor) device */
            public ushort i_count;  /* # times inode used; 0 means slot is free */
            public char i_dirt;     /* CLEAN or DIRTY */
            public char i_pipe;     /* set to I_PIPE if pipe */
            public char i_mount;    /* this bit is set if file mounted on */
            public char i_seek;     /* set on LSEEK, cleared on READ/WRITE */
        }

        // Super block table.  The root file system and every mounted file system
        // has an entry here.  The entry holds information about the sizes of the bit
        // maps and inodes.  The s_ninodes field gives the number of inodes available
        // for files and directories, including the root directory.  Inode 0 is 
        // on the disk, but not used.  Thus s_ninodes = 4 means that 5 bits will be
        // used in the bit map, bit 0, which is always 1 and not used, and bits 1-4
        // for files and directories.  The disk layout is:
        // 
        //      Item        # blocks
        //    boot block      1
        //    super block     1
        //    inode map       s_imap_blocks
        //    zone map        s_zmap_blocks
        //    inodes          (s_ninodes + 1 + INODES_PER_BLOCK - 1)/INODES_PER_BLOCK
        //    unused          whatever is needed to fill out the current zone
        //    data zones      (s_nzones - s_firstdatazone) << s_log_zone_size
        // 
        // A super_block slot is free if s_dev == NO_DEV. 
        // 

        // this is the super block from the 360K minix root diskette image
        //  s_ninodes   s_nzones    s_imap_blocks   s_zmap_blocks   s_firstdatazone s_log_zone_size s_max_size  s_magic
        //  00 5D       01 00       00 01           00 01           00 07           00 00           10 08 1C 00 13 7F
        //
        public class SuperBlock
        {
            public ushort s_ninodes;          // # usable inodes on the minor device                        -- inode_nr     00 5D -- 93 iNodes
            public ushort s_nzones;           // number of zones total device size, including bit maps etc  -- zone_nr      01 00 -- 256
            public ushort s_imap_blocks;      // # of blocks used by inode bit map                          -- unshort      00 01 -- 0x0400 bytes offset = 0x800
            public ushort s_zmap_blocks;      // # of blocks used by zone bit map                           -- unshort      00 01 -- 0x0400 bytes offset = 0xC00
                                              //    the iNodes lie between the end of the zmap_blocks
                                              //    and the data blocks in this case starting at offset     -- inodes start at 0x1000 for 0x0C00 bytes
                                              //    0x1000 to offset 0x1C00 (3 blocks)
            public ushort s_firstdatazone;    // number of first data zone                                  -- zone_nr      00 07 -- offset = 7 * 0x400 = 0x1C00
            public ushort s_log_zone_size;    // log2 of blocks/zone                                        -- short int    00 00 
            public ulong  s_max_size;         // maximum file size on this device                           -- file_pos     10 08 1C 00  == 268,966,912 (256 MB)
            public ushort s_magic;            // magic number to recognize super-blocks                     -- short        13 7F
        }

        // The directory entry associates a filename with an inode. The first two bytes of the directory entry
        // indicate the inode, the remainder is the filename. The root directpory is node 1. 32 directory
        // entries will fit in a 1K block which can exist anywhere on the data zone. Hard entries are made
        // when two entries point to the same node. Soft links are stored in the data zone indexed by an
        // inode entry. A hard link uses only a directory entry, whereas a soft link uses a directory entry,
        // an inode and at least one data zone.
        //
        //      The inode in the directory entry is 1 based - NOT 0 based.
        //      so inode 3 is the third inode - NOT the fourth.

        public class minix_dir_entry
        {
            public ushort inode;
            public string name;
        }
        #endregion

        public List<Inode> inodes = new List<Inode>();
        public SuperBlock superBlock = new SuperBlock();

        private XmlDocument xmlDoc = new XmlDocument();
        public string currentlyOpenedImageFileName = "";
        public string strDescription = "";

        private ASCIIEncoding ascii = new ASCIIEncoding();

        fileformat ff = fileformat.fileformat_UNKNOWN;

        private FileStream fs = null;
        public MinixImage(FileStream _fs, fileformat _ff)
        {
            ff = _ff;

            if (_fs != null)
            {
                byte[] super_block = new byte[512];
                fs = _fs;

                fs.Seek(0x400, SeekOrigin.Begin);           // seek start of super block
                fs.Read(super_block, 0, 512);


                if (ff == fileformat.fileformat_MINIX_68K)
                {
                    superBlock.s_ninodes        = (ushort)(super_block[0] * 256 + super_block[1]);
                    superBlock.s_nzones         = (ushort)(super_block[2] * 256 + super_block[3]);
                    superBlock.s_imap_blocks    = (ushort)(super_block[4] * 256 + super_block[5]);
                    superBlock.s_zmap_blocks    = (ushort)(super_block[6] * 256 + super_block[7]);
                    superBlock.s_firstdatazone  = (ushort)(super_block[8] * 256 + super_block[9]);
                    superBlock.s_log_zone_size  = (ushort)(super_block[10] * 256 + super_block[11]);
                    superBlock.s_max_size       = (ushort)(super_block[12] * 256 * 256 * 256 + super_block[13] * 256 * 256 + super_block[14] * 256 + super_block[15]);
                    superBlock.s_magic          = (ushort)(super_block[16] * 256 + super_block[17]);
                }
                else if (ff == fileformat.fileformat_MINIX_IBM)
                {
                    superBlock.s_ninodes        = (ushort)(super_block[1 ] * 256 + super_block[0]);
                    superBlock.s_nzones         = (ushort)(super_block[3 ] * 256 + super_block[2]);
                    superBlock.s_imap_blocks    = (ushort)(super_block[5 ] * 256 + super_block[4]);
                    superBlock.s_zmap_blocks    = (ushort)(super_block[7 ] * 256 + super_block[6]);
                    superBlock.s_firstdatazone  = (ushort)(super_block[9 ] * 256 + super_block[8]);
                    superBlock.s_log_zone_size  = (ushort)(super_block[11] * 256 + super_block[10]);
                    superBlock.s_max_size       = (ushort)(super_block[15] * 256 * 256 * 256 + super_block[14] * 256 * 256 + super_block[13] * 256 + super_block[12]);
                    superBlock.s_magic          = (ushort)(super_block[17] * 256 + super_block[16]);
                }
            }
        }

        public void LoadInodeMap ()
        {
            int iNodeStart = 0x1000;
            fs.Seek(iNodeStart, SeekOrigin.Begin);      // poistion to the iNode Map

            int iNodesSize = (superBlock.s_firstdatazone * 1024) - 0x1000;

            byte[] iNodesBlock = new byte[iNodesSize];
            fs.Read(iNodesBlock, 0, iNodesSize);
            for (int i = 0; i < iNodesSize / 32; i++)        // 32 bytes per iNode entry
            {
                Inode inode = new Inode();
                if (ff == fileformat.fileformat_MINIX_68K)
                { 
                    inode.i_mode    = (ushort)(iNodesBlock[i * 32] * 256 + iNodesBlock[i * 32 + 1]);
                    inode.i_uid     = (ushort)(iNodesBlock[i * 32 + 2] * 256 + iNodesBlock[i * 32 + 3]);
                    inode.i_size    = (ushort)(iNodesBlock[i * 32 + 4] * 256 * 256 * 256 + iNodesBlock[i * 32 + 5] * 256 * 256 + iNodesBlock[i * 32 + 6] * 256 + iNodesBlock[i * 32 + 7]);
                    inode.i_modtime = (ushort)(iNodesBlock[i * 32 + 8] * 256 * 256 * 256 + iNodesBlock[i * 32 + 9] * 256 * 256 + iNodesBlock[i * 32 + 10] * 256 + iNodesBlock[i * 32 + 11]);
                    inode.i_gid     = (byte)iNodesBlock[i * 32 + 12];
                    inode.i_nlinks  = (byte)iNodesBlock[i * 32 + 13];
                    for (int j = 0; j < NR_ZONE_NUMS; j++)
                    {
                        inode.i_zone[j] = (ushort)(iNodesBlock[(i * 32 + 14) + (j * 2)] * 256 + iNodesBlock[(i * 32 + 15) + (j * 2)]);
                    }
                }
                else if (ff == fileformat.fileformat_MINIX_IBM)
                {
                    inode.i_mode    = (ushort)(iNodesBlock[i * 32 + 1] * 256 + iNodesBlock[i * 32 + 0]);
                    inode.i_uid     = (ushort)(iNodesBlock[i * 32 + 3] * 256 + iNodesBlock[i * 32 + 2]);
                    inode.i_size    = (ushort)(iNodesBlock[i * 32 + 7] * 256 * 256 * 256 + iNodesBlock[i * 32 + 6] * 256 * 256 + iNodesBlock[i * 32 + 5] * 256 + iNodesBlock[i * 32 + 4]);
                    inode.i_modtime = (ushort)(iNodesBlock[i * 32 + 11] * 256 * 256 * 256 + iNodesBlock[i * 32 + 10] * 256 * 256 + iNodesBlock[i * 32 + 9] * 256 + iNodesBlock[i * 32 + 8]);
                    inode.i_gid     = (byte)iNodesBlock[i * 32 + 12];
                    inode.i_nlinks  = (byte)iNodesBlock[i * 32 + 13];
                    for (int j = 0; j < NR_ZONE_NUMS; j++)
                    {
                        inode.i_zone[j] = (ushort)(iNodesBlock[(i * 32 + 15) + (j * 2)] * 256 + iNodesBlock[(i * 32 + 14) + (j * 2)]);
                    }
                }

                inodes.Add(inode);
            }
        }

        void ParseMinixDirectory(string rootPath, ref XmlNode currentNode, FileStream fs, string strCurrentPath, int iNodeIndex, int level)
        {
            // traverse the inodes and add to the xml document recursively. iNode is passed in as one based

            List<minix_dir_entry> dirEntries = new List<minix_dir_entry>();

            int mode = inodes[iNodeIndex - 1].i_mode;

            if ((mode & I_SOCKET) == I_SOCKET)          { }             // 0xC000
            else if ((mode & I_SYMBLINK) == I_SYMBLINK) { }             // 0xA000
            else if ((mode & I_REGULAR) == I_REGULAR)       // 0x8000
            {
                //// just add it to the xml document and loop
                ////
                ////      first we need the name of the file and the the inode that coresponds to this file in the iNode Map
                ////      we have the iNode - we need to get the filename
                ////      current node in the xml document has the name of the file

                //for (int zoneIndex = 0; zoneIndex < 7; zoneIndex++)
                //{
                //    if (inodes[iNodeIndex - 1].i_zone[zoneIndex] != 0)
                //    {
                //        // this is a directory we are reading - so process it one entry at a time (there are up to 32 per 1K block
                //        // if we get to the end of the 32 entries per block - go to next inode for this directory

                //        long offsetIntoFile = inodes[iNodeIndex - 1].i_zone[zoneIndex] * 1024;

                //        // we are now pointing at the offset into the file where the file data starts

                //    }
                //    else
                //        break;
                //}
            }
            else if ((mode & I_BLOCK_SPECIAL) == I_BLOCK_SPECIAL) { }   // 0x6000
            else if ((mode & I_DIRECTORY) == I_DIRECTORY)
            {
                // this is a directory -- add it to the xml document and resurse.
                //      the inode has the zone for the directory information starting at i_zone[0] through i_zone[6]
                //      i_zone[7] points to more znoes and i_zone[0] points to even more

                for (int zoneIndex = 0; zoneIndex < 7; zoneIndex++)
                {
                    if (inodes[iNodeIndex - 1].i_zone[zoneIndex] != 0)
                    {
                        // this is a directory we are reading - so process it one entry at a time (there are up to 32 per 1K block
                        // if we get to the end of the 32 entries per block - go to next inode for this directory

                        long offsetIntoFile = inodes[iNodeIndex - 1].i_zone[zoneIndex] * 1024;

                        for (int j = 0; j < 32; j++)
                        {
                            byte[] direntryInode = new byte[2];
                            byte[] direntryName = new byte[15];
                            fs.Seek(offsetIntoFile, SeekOrigin.Begin);
                            direntryName[14] = 0x00;                   // make sure it is terminated

                            minix_dir_entry dir_entry = new minix_dir_entry();
                            fs.Read(direntryInode, 0, 2);
                            fs.Read(direntryName, 0, 14);

                            // the inode in the directory points to the inode map entry for this file
                            // a value of 0000 for the inode is and empty entry - skip it.

                            if (ff == fileformat.fileformat_MINIX_68K)
                                dir_entry.inode = (ushort)(direntryInode[0] * 256 + direntryInode[1]);
                            else if (ff == fileformat.fileformat_MINIX_IBM)
                                dir_entry.inode = (ushort)(direntryInode[1] * 256 + direntryInode[0]);

                            if (dir_entry.inode != 0)
                            {
                                dir_entry.name = ascii.GetString(direntryName);
                                dir_entry.name = dir_entry.name.Replace("\0", "");
                                dir_entry.name = dir_entry.name.Replace("[", "left_bracket");
                                dir_entry.name = dir_entry.name.Replace("]", "right_bracket");

                                if (dir_entry.name != "." && dir_entry.name != ".." && dir_entry.name != "" && !dir_entry.name.StartsWith("?"))
                                {
                                    dirEntries.Add(dir_entry);
                                    XmlNode fileNode = xmlDoc.CreateElement(dir_entry.name);

                                    XmlAttribute attribute  = xmlDoc.CreateAttribute("RealName");   attribute.Value  = dir_entry.name;                                fileNode.Attributes.Append(attribute);
                                    XmlAttribute attribute1 = xmlDoc.CreateAttribute("iNode");      attribute1.Value = dir_entry.inode.ToString();                    fileNode.Attributes.Append(attribute1);
                                    XmlAttribute attribute2 = xmlDoc.CreateAttribute("ByteCount");  attribute2.Value = inodes[dir_entry.inode - 1].i_size.ToString(); fileNode.Attributes.Append(attribute2);
                                    XmlAttribute attribute3 = xmlDoc.CreateAttribute("Mode");       attribute3.Value = inodes[dir_entry.inode - 1].i_mode.ToString(); fileNode.Attributes.Append(attribute3);

                                    XmlNode newNode = currentNode.AppendChild(fileNode);

                                    // pass the iNode in as one based - when used as index it will have 1 subtracted from it

                                    ParseMinixDirectory(rootPath, ref newNode, fs, strCurrentPath, dir_entry.inode, ++level);
                                }
                            }

                            offsetIntoFile += 16;
                        }
                    }
                    else
                        break;
                }
            }
            else if ((mode & I_CHAR_SPECIAL) == I_CHAR_SPECIAL) { }     // 0x2000
            else if ((mode & i_NAMEDPIPE) == i_NAMEDPIPE) { }           // 0x1000
            else
            {
                MessageBox.Show("Unknown iNode mode");
            }
        }

        private void LoadMinixSystemInformationRecord(ref XmlNode currentNode, ref string strDescription, string path, ulong dwTotalFileSize)
        {
            LoadInodeMap();

            string extension = Path.GetExtension(path);
            string rootPath = path.Replace(@"\", "/");

            // incase the diskette image filename has no extension

            if (extension.Length > 0)
                rootPath = rootPath.Replace(extension, "");

            string currentWorkingDirectory = Directory.GetCurrentDirectory();

            string disketteFileLocation = Path.GetDirectoryName(fs.Name);
            string disketteDirectoryName = Path.GetFileNameWithoutExtension(fs.Name);

            string[] paths = { @".\ExtractedFiles", (disketteFileLocation.Replace(currentWorkingDirectory, "")).TrimStart('\\'), disketteDirectoryName };

            string fullPath = Path.Combine(paths);

            int level = 0;
            int iNode = 1;  // iNodes are 1 based

            ParseMinixDirectory(rootPath, ref currentNode, fs, fullPath, iNode, level);
        }

        private void ProcessMinixImageFile(string filename, string path, XmlNode currentNode)
        {
            strDescription = "";
            ulong dwTotalFileSize = 0;

            FileInfo fi = new FileInfo(path);
            DateTime dtCreate = fi.CreationTime;
            DateTime dtLastAccessed = fi.LastAccessTime;
            DateTime dtLastModified = fi.LastWriteTime;

            dwTotalFileSize = (ulong)fs.Length;

            LoadMinixSystemInformationRecord(ref currentNode, ref strDescription, path, dwTotalFileSize);

            XmlAttribute attribute = xmlDoc.CreateAttribute("RealName");    attribute.Value = filename; currentNode.Attributes.Append(attribute);
            XmlAttribute attribute1 = xmlDoc.CreateAttribute("iNode");      attribute1.Value = "0";     currentNode.Attributes.Append(attribute1);
            XmlAttribute attribute2 = xmlDoc.CreateAttribute("ByteCount");  attribute2.Value = "0";     currentNode.Attributes.Append(attribute2);
            XmlAttribute attribute3 = xmlDoc.CreateAttribute("Mode");       attribute3.Value = "0";     currentNode.Attributes.Append(attribute3);

            xmlDoc.AppendChild(currentNode);

            Console.WriteLine(strDescription);
        }

        bool debugging = false;

        [Conditional("DEBUG")]
        private void SetDebugging ()
        {
            debugging = true;
        }

        public byte [] RetrieveFile (int iNodeIndex, long byteCount)
        {
            byte [] fileContent = new byte[byteCount];

            // get the iNode content from the NodeMap.

            Inode inode = inodes[iNodeIndex];

            // traverse the zones gathering data up to byte count amount.

            long offsetGathered = 0;
            long remainingBytesToGather = (int)byteCount;

            for (int i = 0; i < 7; i++)
            {
                // traverse the first seven zones. If we still have not gathered enough bytes,
                // we will have to use the indirect zone and possibly the double indirect zones

                if (inode.i_zone[i] > 0)
                {
                    long fileOffset = inode.i_zone[i] * 1024;
                    fs.Seek(fileOffset, SeekOrigin.Begin);
                    int bytesRead = fs.Read(fileContent, (int)offsetGathered, (int)(remainingBytesToGather > 1024 ? 1024 : remainingBytesToGather));
                    remainingBytesToGather -= bytesRead;
                    offsetGathered += bytesRead;

                    if (remainingBytesToGather <= 0)
                        break;
                }
                else
                    break;
            }
            if (inode.i_zone[7] > 0)
            {
                // we have mode zones to get. The zone pointed to by inode.i_zone[7] has a list
                // of up to 512 more zones to load
                //
                //      get the offset to the addition list of zones to load

                long fileOffset = inode.i_zone[7] * 1024;

                // load the zones into an array of zones.

                List<int> zones = new List<int>();

                fs.Seek(fileOffset, SeekOrigin.Begin);
                for (int i = 0; i < 512; i++)
                {
                    byte[] zoneBytes = new byte[2];

                    fs.Read(zoneBytes, 0, 2);
                    int zone = zoneBytes[0] * 256 + zoneBytes[1];

                    // see if little endian

                    if (ff == fileformat.fileformat_MINIX_IBM)
                        zone = zoneBytes[1] * 256 + zoneBytes[0];

                    if (zone > 0)
                    {
                        zones.Add(zone);
                    }
                    else
                        break;
                }

                foreach (int additionalZone in zones)
                {
                    fileOffset = additionalZone * 1024;
                    fs.Seek(fileOffset, SeekOrigin.Begin);
                    int bytesRead = fs.Read(fileContent, (int)offsetGathered, (int)(remainingBytesToGather > 1024 ? 1024 : remainingBytesToGather));
                    remainingBytesToGather -= bytesRead;
                    offsetGathered += bytesRead;

                    if (remainingBytesToGather <= 0)
                        break;
                }
            }

            return fileContent;
        }

        public XmlDocument LoadMinixDisketteImageFile(string cDrivePathName)
        {
            //SetDebugging();

            xmlDoc = null;

            //if (debugging)
            {
                xmlDoc = new XmlDocument();

                XmlNode docNode = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
                xmlDoc.AppendChild(docNode);

                string filename = Path.GetFileName(cDrivePathName);
                string rootNodeElementName = filename.Replace(" ", "_").Replace("(", "_").Replace(")", "_");
                bool isLetter = !String.IsNullOrEmpty(rootNodeElementName) && Char.IsLetter(rootNodeElementName[0]);
                if (!isLetter)
                    rootNodeElementName = "_" + rootNodeElementName;

                XmlNode rootNode = xmlDoc.CreateElement(rootNodeElementName);
                currentlyOpenedImageFileName = cDrivePathName;

                ProcessMinixImageFile(filename, cDrivePathName, rootNode);
            }
            return xmlDoc;
        }

        public void ExportSingleMinixFile(string fullTargetDirectoryPath, string innerSourcePath, NodeAttributes tag, string fullPath)
        {
            byte[] fileContent = RetrieveFile(tag.iNode - 1, tag.byteCount);
            ExportSingleMinixFile(fullTargetDirectoryPath, innerSourcePath, tag, fullPath, fileContent);
        }

        public void ExportSingleMinixFile(string fullTargetDirectoryPath, string innerSourcePath, NodeAttributes tag , string fullPath, byte [] fileContent)
        {
            if (!Directory.Exists(fullTargetDirectoryPath))
                Directory.CreateDirectory(fullTargetDirectoryPath);

            string fileString = ascii.GetString(fileContent);

            try
            {
                if (fs != null)
                {
                    uint nByteCount = (uint)tag.byteCount;

                    // TODO: Get strCurrentPath, strFname, fdnCurrent, blk, nFileSize, fs from tag for call to virtualFloppyManipulationRoutines.ExtractMinixFile

                    string currentPath = Path.GetDirectoryName(innerSourcePath);
                    string strFilename = Path.GetFileName(fullPath);

                    try
                    {
                        using (BinaryWriter bw = new BinaryWriter(File.Open(Path.Combine(fullTargetDirectoryPath, strFilename), FileMode.Create, FileAccess.Write)))
                        {
                            if (bw != null)
                            {
                                bw.Write(fileContent, 0, fileContent.Length);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
