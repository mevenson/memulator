using System;
using System.Collections;
using System.Collections.Generic;
//using System.Linq;
using System.Text;

using System.IO;
using System.Runtime.InteropServices;

namespace Memulator
{
    class TS950
    {
        private byte previousCharacter = 0xff;

        private int SCR_NORMAL_WHITE_ON_GREEN   = 0;   //    case '0':   // Normal White on green
        //private int SCR_BLANK                   = 1;   //    case '1':   // Invisible
        //private int SCR_BLINK                   = 2;   //    case '2':   // Blink
        //private int SCR_REVERSE_VIDEO           = 4;   //    case '4':   // Reverse (green on white)
        //private int SCR_UNDERLINE               = 8;   //    case '8':   // Underline

        enum CursorState
        {
            CUR_OFF                     = 0,   //    case '0':   // Cursor not displayed
            CUR_BLINK_BLOCK             = 1,   //    case '1':   // Blinking block cursor
            CUR_STEADY_BLOCK            = 2,   //    case '2':   // Steady block cursor
            CUR_BLINK_UNDERLINE         = 3,   //    case '3':   // Blinking underline cursor
            CUR_STEADY_UNDERLINE        = 4    //    case '4':   // Steady underline cursor
        }

        private int SCR_HALF_INTENSITY_OFF      = 0;
        private int SCR_HALF_INTENSITY_ON       = 128;

        byte[,] m_caAttribute = new byte[50, 82];

        public bool saveOutput = false;
        
        enum PositionState
        {
            GETROW  = 0,
            GETCOL  = 1
        }

        enum ScreenDimensions
        {
            MAX_LINES   = 24,
            MAX_COLUMNS = 80
        }

        int m_nRow, m_nCol;

        byte [] szUserLine = new byte [81];

        int m_nUserLineIndex;
        int m_nGetCursorState;
        bool bInEscape;
        bool bLoadingCursor;
        bool bSendLineState;
        bool bSetVideoDotAttributes;
        bool bSetVideoGAttributes;
        bool bSettingDuplex;
        bool bLoadingUserLine;

        //int  m_nCursorRow , m_nCursorCol;
        //bool m_nCursorVisible;
        //bool m_nCursorChanging;
        int m_nHalfIntensity;

        int  m_nCurrentAttribute;
        int  m_nCursorAttribute;

        ConsoleColor m_clrNormalBackColor  = ConsoleColor.Black;
        ConsoleColor m_clrNormalTextColor  = ConsoleColor.White;
        ConsoleColor m_clrReverseBackColor = ConsoleColor.White;
        ConsoleColor m_clrReverseTextColor = ConsoleColor.Black;

        //ConsoleColor m_clrHighlightTextColor = ConsoleColor.Red;

        private static char[] spaces = new char[(int)ScreenDimensions.MAX_COLUMNS];

        private List<byte> _outbuffer = new List<byte>();
        public List<byte> Outbuffer
        {
            get { return _outbuffer; }
            set { _outbuffer = value; }
        }


        public TS950()
        {
            Console.SetWindowSize((int)ScreenDimensions.MAX_COLUMNS, (int)ScreenDimensions.MAX_LINES + 1);
            if (Program.Platform == OSPlatform.Linux)
            {
                Console.BackgroundColor = m_clrNormalBackColor;
                Console.ForegroundColor = m_clrNormalTextColor;
                Console.Clear();
            }
            if (Program.Platform == OSPlatform.Windows)
                Console.SetBufferSize((int)ScreenDimensions.MAX_COLUMNS, (int)ScreenDimensions.MAX_LINES + 1);

            Console.TreatControlCAsInput = true;

            for (int i = 0; i < (int)ScreenDimensions.MAX_COLUMNS; i++)
                spaces[i] = ' ';
        }

        private void memset (byte [] dest, byte c, int count)
        {
            for (int i = 0; i < dest.Length && i < count; i++)
            {
                dest[i] = c;
            }
        }

        private void memcpy(byte[] dst, byte[] src, int count)
        {
            for (int i = 0; i < dst.Length && i < src.Length && i < count; i++)
            {
                dst[i] = src[i];
            }
        }

        // this is used by the linux version

		private void SetCursorPosition (int col, int row)
		{
            if (Program.Platform == OSPlatform.Windows)
                Console.SetCursorPosition(col, row);
            else
            {
                if (row < (int)ScreenDimensions.MAX_LINES)
                    Console.SetCursorPosition(col, row);
                else
                    Console.SetCursorPosition(col, (int)ScreenDimensions.MAX_LINES - 1);
            }
		}

        public void PutCharacter(byte c)
        {
            if (c == 0)
                return;

            bool bDisplayable = false;
            byte[] nChar = new byte[2];

            nChar[0] = c;
            nChar[1] = 0x00;

            //m_nCursorRow = m_nRow;
            //m_nCursorCol = m_nCol;

            //if (m_nCursorCol > 79)
            //{
            //    m_nCursorCol = 0;
            //    m_nCursorRow++;
            //}

            try
            {
                if (!bInEscape)
                {
                    if (saveOutput && (c != 0x01B))
                        _outbuffer.Add(c);

                    switch (c)
                    {
                        case 0x08:
                            if (m_nCol > 0)
                            {
                                m_nCol--;
                                SetCursorPosition(m_nCol, m_nRow);
                            }

                            //if (Program.cpu.m_nCaptureActive && (Program.m_fpCaptureFile != NULL))
                            //    fprintf(Program.m_fpCaptureFile, "%c", c);
                            break;

                        case 0x1B:
                            bInEscape = true;
                            if (saveOutput)
                            {
                                _outbuffer.Add((byte)'E'); _outbuffer.Add((byte)'S'); _outbuffer.Add((byte)'C');
                            }

                            //if (Program.m_nCaptureActive && (Program.m_fpCaptureFile != NULL) && Program.m_nSaveCursorPositioningCharacters)
                            //    fprintf(Program.m_fpCaptureFile, "%c", c);
                            break;

                        case 0x06:  // Home
                            m_nRow = 0;
                            m_nCol = 0;
                            SetCursorPosition(m_nCol, m_nRow);
                            //if (Program.m_nCaptureActive && (Program.m_fpCaptureFile != NULL) && Program.m_nSaveCursorPositioningCharacters)
                            //    fprintf(Program.m_fpCaptureFile, "%c", c);
                            break;

                        case 0x13:
                        case 0x15:
                        case 0x00:
                            break;

                        case 0x0A:
                            if (previousCharacter != 0x0d)
                            {
                                // handle linefeed without carriage return

                                m_nCol = 0;
                                SetCursorPosition(m_nCol, m_nRow);
                            }

                            m_nRow++;

                            if (m_nRow > (int)ScreenDimensions.MAX_LINES - 1)
                            {
                                m_nRow = (int)ScreenDimensions.MAX_LINES - 1;
                                if (Program.Platform == OSPlatform.Windows)
                                    Console.MoveBufferArea(0, 1, 80, 23, 0, 0);
                                else
                                    Console.Write('\n');
                            }
                            SetCursorPosition(m_nCol, m_nRow);

                            //if (Program.m_nCaptureActive && (Program.m_fpCaptureFile != NULL) && Program.m_nSaveLineFeeds)
                            //    fprintf(Program.m_fpCaptureFile, "%c", c);

                            break;

                        case 0x0B:
                            if (m_nRow > 0)
                                --m_nRow;

                            SetCursorPosition(m_nCol, m_nRow);

                            //if (Program.m_nCaptureActive && (Program.m_fpCaptureFile != NULL) && Program.m_nSaveCursorPositioningCharacters)
                            //    fprintf(Program.m_fpCaptureFile, "%c", c);
                            break;


                        case 0x0D:
                            m_nCol = 0;
                            SetCursorPosition(m_nCol, m_nRow);

                            //if (Program.m_nCaptureActive && (Program.m_fpCaptureFile != NULL) && Program.m_nSaveCarriageReturns)
                            //    fprintf(Program.m_fpCaptureFile, "%c", c);

                            break;

                        // clear screen - reset all attributes to 0 and characters to space

                        case 0x1A:
                            Console.BackgroundColor = m_clrNormalBackColor;
                            Console.ForegroundColor = m_clrNormalTextColor;

                            m_nRow = m_nCol = 0;
                            SetCursorPosition(m_nCol, m_nRow);

                            m_nCurrentAttribute = SCR_NORMAL_WHITE_ON_GREEN;
                            m_nGetCursorState = (int)PositionState.GETROW;
                            bLoadingCursor = false;
                            bInEscape = false;

                            Console.Clear();

                            //if (Program.m_nCaptureActive && (Program.m_fpCaptureFile != NULL) && Program.m_nSaveCursorPositioningCharacters)
                            //    fprintf(Program.m_fpCaptureFile, "%c", c);

                            break;

                        default:
                            if (c >= 0x20 && c < 0x80)
                            {
                                bDisplayable = true;

                                //if (Program.m_nCaptureActive && (Program.m_fpCaptureFile != NULL))
                                //    fprintf(Program.m_fpCaptureFile, "%c", c);

                            }
                            break;
                    }

                    // WE HAVE PROBLEMS HERE ON THE LAST LINE

                    if (bDisplayable)
                    {
					    if (m_nRow < (int)ScreenDimensions.MAX_LINES)
					    {
                    	    SetCursorPosition(m_nCol, m_nRow);
					    }

                        Console.BackgroundColor = m_clrNormalBackColor;
                        Console.ForegroundColor = m_clrNormalTextColor;

                        // this gets executed when we are about to output a character and we have already written to thlast position on the screen

                        if (m_nCol == (int)ScreenDimensions.MAX_COLUMNS - 1 && m_nRow == (int)ScreenDimensions.MAX_LINES)
                        {
                            // advance to the next line BEFORE we output the character and scroll the page if required.

                            if (m_nRow > (int)ScreenDimensions.MAX_LINES - 1)
                            {
                                m_nRow = (int)ScreenDimensions.MAX_LINES - 1;
                                if (Program.Platform == OSPlatform.Windows)
                                    Console.MoveBufferArea(0, 1, 80, 23, 0, 0);
                                else
                                    Console.Write('\n');
                            }
                            m_nCol = 0;
                        }

                        if (m_nRow < (int)ScreenDimensions.MAX_LINES)
                        {
                            switch (c)
                            {
                                // characters to filter out or extra special handling on.
                                // these are being filtered

                                case (byte)0x00:
                                case (byte)0x13:
                                case (byte)0x15:
                                    break;

                                // These can go through

                                default:
                                    //Console.Write(Convert.ToChar(b));       
                                    Console.Write(Convert.ToChar(c));
                                    break;
                            }
                        }

					    if (m_nRow < (int)ScreenDimensions.MAX_LINES)
					    {
                    	    SetCursorPosition(m_nCol, m_nRow);
					    }

                        if (m_nCol < (int)ScreenDimensions.MAX_COLUMNS - 1)
                        {
                            m_nCol++;
						    if (m_nRow < (int)ScreenDimensions.MAX_LINES)
                        	    SetCursorPosition(m_nCol, m_nRow);
						    else
							    SetCursorPosition(m_nCol, (int)ScreenDimensions.MAX_LINES - 1);
                        }

                        if (m_nCol == (int)ScreenDimensions.MAX_COLUMNS - 1)
                        {
                            m_nRow++;
                            m_nCol = 0;
                        }
                    }

                    previousCharacter = c;
                }

                //  Handle Escape Sequences

                else
                {
                    if (bLoadingCursor)
                    {
                        if (c < 0x20)
                            c = 0x20;

                        switch (m_nGetCursorState)
                        {
                            case (int)PositionState.GETROW:
                                if (c > 0x20 + (int)ScreenDimensions.MAX_LINES - 1)
                                    c = (byte)(0x20 + (int)ScreenDimensions.MAX_LINES - 1);

                                m_nRow = c - 0x20;
                                m_nGetCursorState = (int)PositionState.GETCOL;

                                if (saveOutput)
                                {
                                    _outbuffer.Add(c);
                                }

                                break;
                            case (int)PositionState.GETCOL:
                                if (c > 0x20 + 79)
                                    c = 0x20 + 79;

                                m_nCol = c - 0x20;
                                m_nGetCursorState = (int)PositionState.GETROW;
                                bLoadingCursor = false;
                                bInEscape = false;
                                SetCursorPosition(m_nCol, m_nRow);

                                if (saveOutput)
                                {
                                    _outbuffer.Add(c);
                                    _outbuffer.Add(0x0A);
                                }
                                break;
                        }
                    }
                    else
                    {
                        if (bSendLineState)
                        {
                            bSendLineState = false;
                            bInEscape = false;

                            switch (c)
                            {
                                // NOT YET IMPLEMEMTED

                                case (byte)'0':   // Send User Line
                                case (byte)'1':   // Send Status Line
                                    break;
                            }
                        }
                        else
                        {
                            if (bSetVideoDotAttributes)
                            {
                                bSetVideoDotAttributes = false;
                                bInEscape = false;

                                if (c >= '0' && c <= '4')
                                    m_nCursorAttribute = c & 0x07;

                                //switch (c)
                                //{
                                //    // NOT YET IMPLEMEMTED
                                //
                                //    case '0':   // Cursor not displayed
                                //    case '1':   // Blinking block cursor
                                //    case '2':   // Steady block cursor
                                //    case '3':   // Blinking underline cursor
                                //    case '4':   // Steady underline cursor
                                //        break;
                                //}
                            }
                            else
                            {
                                if (bSetVideoGAttributes)
                                {
                                    bSetVideoGAttributes = false;
                                    bInEscape = false;

                                    m_nCurrentAttribute = c & 0x0f;

                                    //switch (c)
                                    //{
                                    //    case '0':   // Normal White on green
                                    //    case '1':   // Blank
                                    //    case '2':   // Blink
                                    //    case '3':   // Invisible blink
                                    //    case '4':   // Reverse (green on white)
                                    //    case '5':   // Invisible reverse
                                    //    case '6':   // Reverse blink
                                    //    case '7':   // Invisible reverse blink
                                    //    case '8':   // Underline
                                    //    case '9':   // Invisible underline
                                    //    case ':':   // Underline Blink
                                    //    case ';':   // Invisible underline blink
                                    //    case '<':   // Underline reverse
                                    //    case '=':   // Invisible underline revers
                                    //    case '>':   // Underline reverse blink
                                    //    case '?':   // Invisible underline reverse blink
                                    //        break;
                                    //}
                                }
                                else
                                {
                                    if (bSettingDuplex)
                                    {
                                        bSettingDuplex = false;
                                        bInEscape = false;
                                        switch (c)
                                        {
                                            // NOT YET IMPLEMEMTED

                                            case (byte)'H':   // Half duplex
                                            case (byte)'F':   // Full duplex
                                                break;
                                        }
                                    }
                                    else
                                    {
                                        if (bLoadingUserLine)
                                        {
                                            if (c == '\r')
                                            {
                                                bLoadingUserLine = false;
                                                bInEscape = false;
                                            }
                                            else
                                            {
                                                szUserLine[m_nUserLineIndex++] = c;
                                                if (m_nUserLineIndex > 79)
                                                    m_nUserLineIndex = 79;
                                            }
                                        }
                                        else
                                        {

                                            // FIRST Stage of ESCAPE sequence

                                            if (saveOutput)
                                                _outbuffer.Add(c);

                                            switch (c)
                                            {
                                                case (byte)'=':

                                                    // Load the cursor

                                                    bLoadingCursor = true;
                                                    m_nGetCursorState = (int)PositionState.GETROW;
                                                    break;

                                                case (byte)':':
                                                case (byte)';':
                                                case (byte)'+':
                                                case (byte)',':
                                                case (byte)'*':

                                                    // Clear the screen

                                                    Console.BackgroundColor = m_clrNormalBackColor;
                                                    Console.ForegroundColor = m_clrNormalTextColor;

                                                    m_nRow = m_nCol = 0;
                                                    m_nCurrentAttribute = SCR_NORMAL_WHITE_ON_GREEN;
                                                    m_nGetCursorState = (int)PositionState.GETROW;
                                                    bLoadingCursor = false;
                                                    bInEscape = false;

                                                    Console.Clear();

                                                    break;

                                                case (byte)'f':
                                                    memset(szUserLine, (byte)'\0', 81);
                                                    m_nUserLineIndex = 0;
                                                    bLoadingUserLine = true;
                                                    break;

                                                // NOT YET IMPLEMEMTED

                                                case (byte)'j':   // Reverse linefeed
                                                case (byte)'O':   // Insert Character
                                                case (byte)'W':   // Delete Character
                                                    bInEscape = false;
                                                    break;

                                                case (byte)'E':   // Insert Line
                                                    if (m_nRow < (int)ScreenDimensions.MAX_LINES - 1)
                                                    {
                                                        if (Program.Platform == OSPlatform.Windows)
                                                            Console.MoveBufferArea(0, m_nRow, 80, (int)ScreenDimensions.MAX_LINES - m_nRow - 1, 0, m_nRow + 1);
                                                        else
                                                            Console.Write('\n');
                                                    }
                                                    bInEscape = false;
                                                    break;

                                                case (byte)'R':   // Delete Line
                                                    if (Program.Platform == OSPlatform.Windows)
                                                        Console.MoveBufferArea(0, m_nRow + 1, 80, (int)ScreenDimensions.MAX_LINES - m_nRow - 1, 0, m_nRow);
                                                    else
                                                        Console.Write('\n');
                                                    bInEscape = false;
                                                    break;

                                                case (byte)'T':   // 54 Erase to end of line (SPACES)
                                                case (byte)'t':   // 74 Erase to end of line (NULLS)
                                                    SetCursorPosition(m_nCol, m_nRow);
                                                    Console.BackgroundColor = m_clrNormalBackColor;
                                                    Console.ForegroundColor = m_clrNormalTextColor;
                                                    Console.Write(spaces, 0, (int)ScreenDimensions.MAX_COLUMNS - m_nCol);
                                                    SetCursorPosition(m_nCol, m_nRow);
                                                    bInEscape = false;
                                                    break;

                                                case (byte)'Y':   // Erase to end of page (SPACES)
                                                case (byte)'y':   // Erase to end of page (NULLS)
                                                    SetCursorPosition(m_nCol, m_nRow);
                                                    Console.BackgroundColor = m_clrNormalBackColor;
                                                    Console.ForegroundColor = m_clrNormalTextColor;
                                                    Console.Write(spaces, 0, (int)ScreenDimensions.MAX_COLUMNS - m_nCol);
                                                    for (int i = m_nRow + 1; i < (int)ScreenDimensions.MAX_LINES; i++)
                                                    {
                                                        SetCursorPosition(0, i);
                                                        Console.BackgroundColor = m_clrNormalBackColor;
                                                        Console.ForegroundColor = m_clrNormalTextColor;
                                                        Console.Write(spaces, 0, (int)ScreenDimensions.MAX_COLUMNS);
                                                    }
                                                    SetCursorPosition(m_nCol, m_nRow);

                                                    bInEscape = false;
                                                    break;

                                                case (byte)'.':   // Video Attributes to follow
                                                    bSetVideoDotAttributes = true;
                                                    break;

                                                case (byte)'G':   // Video Attributes to follow
                                                    bSetVideoGAttributes = true;
                                                    break;

                                                // NOT YET IMPLEMEMTED

                                                case (byte)'d':   // Normal Video (white on green)
                                                    bInEscape = false;
                                                    m_clrNormalBackColor = ConsoleColor.White;
                                                    m_clrNormalTextColor = ConsoleColor.Black;
                                                    m_clrReverseBackColor = ConsoleColor.Black;
                                                    m_clrReverseTextColor = ConsoleColor.White;
                                                    break;

                                                // NOT YET IMPLEMEMTED

                                                case (byte)'b':   // Reverse video (green on white)
                                                    bInEscape = false;
                                                    m_clrNormalBackColor = ConsoleColor.Black;
                                                    m_clrNormalTextColor = ConsoleColor.White;
                                                    m_clrReverseBackColor = ConsoleColor.White;
                                                    m_clrReverseTextColor = ConsoleColor.Black;
                                                    break;

                                                // NOT YET IMPLEMEMTED

                                                case (byte)'n':   // Screen on
                                                    bInEscape = false;
                                                    break;

                                                // NOT YET IMPLEMEMTED

                                                case (byte)'o':   // Screen blank
                                                    bInEscape = false;
                                                    break;

                                                // NOT YET IMPLEMEMTED

                                                case (byte)'$':   // Graphics on
                                                    bInEscape = false;
                                                    break;

                                                // NOT YET IMPLEMEMTED

                                                case (byte)'%':   // Graphics off
                                                    bInEscape = false;
                                                    break;

                                                // NOT YET IMPLEMEMTED

                                                case (byte)')':   // Half intensity on
                                                    m_nHalfIntensity = SCR_HALF_INTENSITY_ON;
                                                    bInEscape = false;
                                                    break;

                                                // NOT YET IMPLEMEMTED

                                                case (byte)'(':   // Half intensity off
                                                    m_nHalfIntensity = SCR_HALF_INTENSITY_OFF;
                                                    bInEscape = false;
                                                    break;

                                                case (byte)'Z':   // Send XXX Line mode follows
                                                    bSendLineState = true;
                                                    break;

                                                default:
                                                    m_nGetCursorState = (int)PositionState.GETROW;
                                                    bLoadingCursor = false;
                                                    bInEscape = false;
                                                    break;
                                            }   // End of switch
                                        }
                                    }
                                }
                            }
                        }
                    }
                    //if (Program.m_nCaptureActive && (Program.m_fpCaptureFile != NULL) && Program.m_nSaveCursorPositioningCharacters)
                    //    fprintf(Program.m_fpCaptureFile, "%c", c);
                }
            }
            catch
            {
            }

            //// Put a cursor on the screen

            //if (!m_nCursorChanging)
            //{
            //    m_nCursorRow = m_nRow;
            //    m_nCursorCol = m_nCol;

            //    if (m_nCursorCol > 79)
            //    {
            //        m_nCursorCol = 0;
            //        m_nCursorRow++;
            //    }
            //}
        }
    }
}
