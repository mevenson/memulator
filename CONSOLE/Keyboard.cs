using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.IO;

using System.Threading;

using System.Runtime.InteropServices;

namespace Memulator
{
    class Keyboard : IODevice
    {
        bool _spin;
        Dictionary<ConsoleKey, KeyboardMapEntry> keyboardMap;


        //List<byte> keyInpBuffer = new List<byte>();
        //List<byte> keyOutBuffer = new List<byte>();

        public Keyboard()
        {
            _spin = false;
            keyboardMap = new Dictionary<ConsoleKey, KeyboardMapEntry>();

            LoadKeyboardMap();
            _bInterruptEnabled  = Program._CConsole._bInterruptEnabled;
            m_nInterruptMask    = Program._CConsole.m_nInterruptMask;
            m_sBaseAddress      = Program._CConsole.m_sBaseAddress;

            if (Program._cpu != null && Program._cpu.Running)
                Program._cpu.InterruptRegister &= ~m_nInterruptMask;
        }

        class KeyboardMapEntry
        {
            public string normal;
            public string shifted;
            public string control;
            public string both;
            public string alt;
        }

        void AddNewKeyboardMapEntry(ConsoleKey k)
        {
            KeyboardMapEntry kbme;

            kbme = new KeyboardMapEntry(); keyboardMap.Add(k, kbme);
            keyboardMap[k].normal  = Program.GetConfigurationAttribute(Program._configSection + "/KeyBoardMap/" + k.ToString(), "Normal",  "");
            keyboardMap[k].shifted = Program.GetConfigurationAttribute(Program._configSection + "/KeyBoardMap/" + k.ToString(), "Shifted", "");
            keyboardMap[k].control = Program.GetConfigurationAttribute(Program._configSection + "/KeyBoardMap/" + k.ToString(), "Control", "");
            keyboardMap[k].both    = Program.GetConfigurationAttribute(Program._configSection + "/KeyBoardMap/" + k.ToString(), "Both",    "");
            keyboardMap[k].alt     = Program.GetConfigurationAttribute(Program._configSection + "/KeyBoardMap/" + k.ToString(), "Alt",     "");
        }
        void LoadKeyboardMap()
        {
            AddNewKeyboardMapEntry(ConsoleKey.UpArrow);
            AddNewKeyboardMapEntry(ConsoleKey.DownArrow);
            AddNewKeyboardMapEntry(ConsoleKey.RightArrow);
            AddNewKeyboardMapEntry(ConsoleKey.LeftArrow);
            AddNewKeyboardMapEntry(ConsoleKey.Home);
            AddNewKeyboardMapEntry(ConsoleKey.End);
            AddNewKeyboardMapEntry(ConsoleKey.PageUp);
            AddNewKeyboardMapEntry(ConsoleKey.PageDown);
            AddNewKeyboardMapEntry(ConsoleKey.Insert);
            AddNewKeyboardMapEntry(ConsoleKey.Delete);
        }

        // This is ONLY called from StuffKeyboard

        void StoreCharacter (byte nChar)
        {
            // keyOutBuffer.Add(nChar);
            Program._CConsole.keyboardQueue.Add(nChar);
            Program._CConsole._ACIA_CONSStatusRegister |= Program._CConsole.ACIA_RDRF;

            //if ((Program._CConsole._ACIA_CONSStatusRegister & Program._CConsole.ACIA_RDRF) == Program._CConsole.ACIA_RDRF) // if there is already an unprocessed status
            //{
            //    lock (Program._CConsole._bMovingCharacter)                  // anytime we are about to modify _nKeyboardBufferCount or _nKeyboardGetPointer we need a lock
            //    {
            //        if (Program._CConsole._nKeyboardBufferCount == 0)       // Are there any characters in the buffer
            //            Program._CConsole._nKeyboardPutPointer = 0;         // Buffer is empty - reset the pointer

            //        Program._CConsole._cKeyboardBuffer[Program._CConsole._nKeyboardPutPointer++] = nChar;
            //        Program._CConsole._nKeyboardBufferCount++;
            //    }
            //}
            //else
            //{
            //    Program._CConsole._ACIA_CONSDataRegister = nChar;
            //    Program._CConsole._ACIA_CONSStatusRegister |= Program._CConsole.ACIA_RDRF;
            //}

            if (Program._CConsole._bInterruptEnabled)
            {
                if ((Program._CConsole._ACIA_CONSCommandRegister & Program._CConsole.ACIA_RDRF_IRQ_ENABLED) != 0)
                {
                    Program._CConsole._bCanAssertConsoleRCVInterrupt = true;
                    Program._CConsole._ACIA_CONSStatusRegister |= Program._CConsole.ACIA_IRQ;

                    //if (Program._CConsole._nCanAssertConsoleXMTInterrupt | Program._CConsole._bCanAssertConsoleRCVInterrupt)

                    if (Program._CConsole._bCanAssertConsoleRCVInterrupt)
                        SetInterrupt(_spin);    //*Program._CConsole._pnInterruptRegister |= Program._CConsole._nInterruptMask;
                    else
                        ClearInterrupt();       //*Program._CConsole._pnInterruptRegister &= ~Program._CConsole._nInterruptMask;

                    if ((Program._cpu.InWait || Program._cpu.InSync) && Program._cpuThread.ThreadState == ThreadState.Suspended)
                    {
                        try
                        {
                            Program._cpuThread.Resume();
                        }
                        catch (ThreadStateException e)
                        {
                            // do nothing if thread is not suspended
                        }
                    }
                }
            }
        }

        public void StuffKeyboard (String strTheString)
        {
            int nLength = strTheString.Length;
            bool bControl = false;
            byte nChar;

            lock (Program._CConsole._bMovingCharacter)                  // anytime we are about to modify _nKeyboardBufferCount or _nKeyboardGetPointer we need a lock
            {
                for (int i = 0; i < nLength; i++)
                {
                    nChar = (byte)strTheString[i];
                    if (nChar == '^')
                    {
                        if (!bControl)
                            bControl = true;
                        else
                        {
                            StoreCharacter(nChar);
                            bControl = false;
                        }
                    }
                    else
                    {
                        if (bControl)
                        {
                            nChar = (byte)(nChar & 0x1f);
                            StoreCharacter(nChar);
                            bControl = false;
                        }
                        else
                        {
                            StoreCharacter(nChar);
                            bControl = false;

                        }
                        bControl = false;
                    }
                }
            }
        }

        ConfigEditor.memulatorConfigEditor pMemulatorConfigEditorDlg = null;

        public void ProcessKeystroke(ConsoleKeyInfo cki)
        {
            bool bStoreChar = true;

            bool bShiftKeyState     = (cki.Modifiers & ConsoleModifiers.Shift)  != 0;
            bool bControlKeyState   = (cki.Modifiers & ConsoleModifiers.Control)!= 0;
        	bool bAltKeyState       = (cki.Modifiers & ConsoleModifiers.Alt)    != 0;

            byte nChar = (byte)cki.KeyChar;
            if (bControlKeyState)
                nChar = (byte)(nChar & 0x1f);

            lock (Program._CConsole._bMovingCharacter)                  // anytime we are about to modify _nKeyboardBufferCount or _nKeyboardGetPointer we need a lock
            {
                if (cki.KeyChar == 0 && Program._cpu.Running)
                {
                    // Then this is a non ASCII key pressed
                    switch (cki.Key)
                    {
                        // special case handlers for program control keys like Quit, RESET, etc.
                        //
                        //       F1     Show Configuration Editor
                        //  CTRL-F1     Kill emulation
                        //  CTRL-F2     Reset emulation
                        //
                        //  CTRL-F3     Start save output to Console Dump File
                        //  CTRL-F4     Stop  save output to Console Dump File
                        //  CTRL-F5     Flush save output to Console Dump File
                        //  CTRL-F6     Reload drive maps from configuration file (Windows)
                        //       F6     Reload drive maps from configuration file (Linux)
                        //  CTRL-F7     Start Trace
                        //       F8     Stuff OS9 format date time string into keyboard buffer
                        //  CTRL-F9     Toggle DMAF-3 access logging
                        //       F11    Stuff FLEX date time format into keyboard buffer
                        //  CTRL-F11    Stuff UniFLEX date time format into keyboard buffer
                        //       F12    Toggle Debug output

                        case ConsoleKey.F1:
                            if (cki.Modifiers == ConsoleModifiers.Control)
                            {
                                bStoreChar = false;

                                // kill any threads that may be running on any open IO cards.

                                if (Program._CDMAF3 != null && Program._CDMAF3.DMAF3Timer != null)
                                    Program._CDMAF3.DMAF3Timer.Stop();

                                if (Program._CDMAF3 != null && Program._CDMAF3.DMAF3InterruptDelayTimer != null)
                                    Program._CDMAF3.DMAF3InterruptDelayTimer.Stop();

                                // if the cpu is in wait or sync - resume it so it can exit

                                if ((Program._cpu.InWait || Program._cpu.InSync) && Program._cpuThread.ThreadState == ThreadState.Suspended)
                                {
                                    try
                                    {
                                        Program._cpuThread.Resume();
                                    }
                                    catch (ThreadStateException e)
                                    {
                                        // do nothing if thread is not suspended
                                    }
                                }

                                Program._cpu.Running = false;
                            }
                            else
                            {
                                bStoreChar = false;

                                // open configuation editor.

                                if (pMemulatorConfigEditorDlg == null)
                                    pMemulatorConfigEditorDlg = new ConfigEditor.memulatorConfigEditor(Program.configFileName, Program._cpu.Running);

                                pMemulatorConfigEditorDlg.ShowDialog();
                            }
                            break;

                        case ConsoleKey.F2:
                            if (cki.Modifiers == ConsoleModifiers.Control)
                            {
                                bStoreChar = false;
                                Program._cpu.ResetPressed = true;

                                if ((Program._cpu.InWait || Program._cpu.InSync) && Program._cpuThread.ThreadState == ThreadState.Suspended)
                                {
                                    try
                                    {
                                        Program._cpuThread.Resume();
                                    }
                                    catch (ThreadStateException e)
                                    {
                                        // do nothing if thread is not suspended
                                    }
                                }
                            }
                            else
                                Program._cpu.CoreDump();
                            break;

                        case ConsoleKey.F3:
                            if (((Program.Platform == OSPlatform.Windows && cki.Modifiers == ConsoleModifiers.Control)) || Program.Platform == OSPlatform.Linux)
                            {
                                bStoreChar = false;
                                Program._theConsole.Terminal.saveOutput = true;
                            }
                            break;

                        case ConsoleKey.F4:
                            if (((Program.Platform == OSPlatform.Windows && cki.Modifiers == ConsoleModifiers.Control)) || Program.Platform == OSPlatform.Linux)
                            {
                                bStoreChar = false;
                                Program._theConsole.Terminal.saveOutput = false;
                            }
                            break;

                        case ConsoleKey.F5:
                            if (((Program.Platform == OSPlatform.Windows && cki.Modifiers == ConsoleModifiers.Control)) || Program.Platform == OSPlatform.Linux)
                            {
                                bStoreChar = false;

                                FileStream fs = File.Open(Program.ConsoleDumpFile, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                                fs.Write(Program._theConsole.Terminal.Outbuffer.ToArray(), 0, Program._theConsole.Terminal.Outbuffer.Count);
                                fs.Close();
                                Program._theConsole.Terminal.Outbuffer = new List<byte>();
                            }
                            bStoreChar = false;
                            break;

                        case ConsoleKey.F6:     // Load drives from configuration.xml file
                            if (((Program.Platform == OSPlatform.Windows && cki.Modifiers == ConsoleModifiers.Control)) || Program.Platform == OSPlatform.Linux)
                            {
                                bStoreChar = false;
                                Program.LoadDrives();
                            }
                            break;

                        case ConsoleKey.F7:     // toggle trace
                            if (cki.Modifiers == ConsoleModifiers.Control)
                            {
                                bStoreChar = false;
                                Program.TraceEnabled = !Program.TraceEnabled;
                            }
                            break;

//                        case ConsoleKey.F7:     // do UniFLEX date format
//							{
//                                bStoreChar = false;
//                                DateTime dt = DateTime.Now;
//                                StuffKeyboard
//                                    (
//                                    	// date [ [MM-DD[-YY]] HH:MM[:SS] ]
//                                        String.Format("{0}-{1}-{2} {3}:{4}:{5}\r",
//                                            dt.Month.ToString("00"),
//                                            dt.Day.ToString("00"),
//                                            (dt.Year - 2000).ToString("00"),
//                                            dt.Hour.ToString("00"),
//                                            dt.Minute.ToString("00"),
//                                            dt.Second.ToString("00")
//                                        )
//                                    );
//                            }
//                            break;

                        case ConsoleKey.F8:
                            {
                                bStoreChar = false;
                                DateTime dt = DateTime.Now;
                                StuffKeyboard
                                    (
                                        String.Format("{0}/{1}/{2} {3}:{4}:{5}\r",      // do OS9 date format (yy/mm/dd hh:mm:ss)
                                            (dt.Year - 2000).ToString("00"),
                                            dt.Month.ToString("00"),
                                            dt.Day.ToString("00"),
                                            dt.Hour.ToString("00"),
                                            dt.Minute.ToString("00"),
                                            dt.Second.ToString("00")
                                        )
                                    );
                            }
                            break;

                        case ConsoleKey.F9:
                            Program.DMAF3AccessLogging = !Program.DMAF3AccessLogging;
                            break;

                        case ConsoleKey.F11:
                            if (cki.Modifiers == ConsoleModifiers.Control)          // do UniFLEX date format
                            {
                                bStoreChar = false;
                                DateTime dt = DateTime.Now;
                                StuffKeyboard
                                    (
                                    // date [ [MM-DD[-YY]] HH:MM[:SS] ]
                                        String.Format("{0}-{1}-{2} {3}:{4}:{5}\r",
                                            dt.Month.ToString("00"),
                                            dt.Day.ToString("00"),
                                            (dt.Year - 2000).ToString("00"),
                                            dt.Hour.ToString("00"),
                                            dt.Minute.ToString("00"),
                                            dt.Second.ToString("00")
                                        )
                                    );
                            }
                            else                                                    // do FLEX date format
                            {
                                bStoreChar = false;
                                DateTime dt = DateTime.Now;
                                StuffKeyboard
                                    (
                                        String.Format("{0}/{1}/{2} {3}/{4}/{5}\r",
                                            dt.Month.ToString("00"),
                                            dt.Day.ToString("00"),
                                            (dt.Year - 2000).ToString("00"),
                                            dt.Hour.ToString("00"),
                                            dt.Minute.ToString("00"),
                                            dt.Second.ToString("00")
                                        )
                                    );
                            }
                            break;

                        case ConsoleKey.F12:
                            bStoreChar = false;
                            Program._cpu.ShowDebugOutput = !Program._cpu.ShowDebugOutput;
                            break;

                        case ConsoleKey.Backspace:
							nChar = 0x08;
							break;

                        // Handle all other non ASCII keys.

                        default:
                            bStoreChar = false;
                            if (keyboardMap.ContainsKey(cki.Key))
                            {
                                if (bControlKeyState && bShiftKeyState)
                                {
                                    StuffKeyboard(keyboardMap[cki.Key].both);
                                }
                                else if (bControlKeyState)
                                {
                                    StuffKeyboard(keyboardMap[cki.Key].control);
                                }
                                else if (bShiftKeyState)
                                {
                                    StuffKeyboard(keyboardMap[cki.Key].shifted);
                                }
                                else if (bAltKeyState)
                                {
                                    StuffKeyboard(keyboardMap[cki.Key].alt);
                                }
                                else
                                {
                                    StuffKeyboard(keyboardMap[cki.Key].normal);
                                }
                            }
                            break;
                    }
                }

                // this is where we send the character to the ACIA

                if (bStoreChar)
                {
                    lock (Program._CConsole._bMovingCharacter)                  // anytime we are about to modify _nKeyboardBufferCount or _nKeyboardGetPointer we need a lock
                    {
                        if (nChar == 0x0a)
                            nChar = 0x0d;

                        Program._CConsole.keyboardQueue.Add(nChar);
                        Program._CConsole._ACIA_CONSStatusRegister |= Program._CConsole.ACIA_RDRF;

                        //if ((Program._CConsole._ACIA_CONSStatusRegister & Program._CConsole.ACIA_RDRF) == Program._CConsole.ACIA_RDRF)
                        //{
                        //    if (Program._CConsole._nKeyboardBufferCount == 0)       // Already Characters in the buffer?
                        //        Program._CConsole._nKeyboardPutPointer = 0;         // Buffer is empty - reset the pointer

                        //    Program._CConsole._cKeyboardBuffer[Program._CConsole._nKeyboardPutPointer++] = nChar;
                        //    Program._CConsole._nKeyboardBufferCount++;
                        //}
                        //else
                        //{
                        //    Program._CConsole._ACIA_CONSDataRegister = nChar;
                        //    Program._CConsole._ACIA_CONSStatusRegister |= Program._CConsole.ACIA_RDRF;
                        //}

                        if (Program._CConsole._bInterruptEnabled)
                        {
                            if ((Program._CConsole._ACIA_CONSCommandRegister & Program._CConsole.ACIA_RDRF_IRQ_ENABLED) != 0)
                            {
                                Program._CConsole._bCanAssertConsoleRCVInterrupt = true;
                                Program._CConsole._ACIA_CONSStatusRegister |= Program._CConsole.ACIA_IRQ;
                                SetInterrupt(_spin);
                            }
                        }
                    }
                }
            }
        }
    }
}
