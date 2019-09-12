using System;
using System.Collections.Generic;
using System.Threading;

using System.IO.Ports;

namespace Memulator
{
    class MPS : IODevice
    {
        class SERIAL_INFO
        {
            public ushort sAddress;
            public string strPort;
            public short sBaudRate;
            public int nRTSControl;
            public int nInputBufferSize;
            public int nOutputBufferSize;
        };

        enum FILE_TYPE
        {
            FILE_TYPE_UNKNOWN   = 0x0000,
            FILE_TYPE_DISK      = 0x0001,
            FILE_TYPE_CHAR      = 0x0002,
            FILE_TYPE_PIPE      = 0x0003,
            FILE_TYPE_REMOTE    = 0x8000
        }

        enum PARITY
        {
            NOPARITY       = 0,
            ODDPARITY      = 1,
            EVENPARITY     = 2,
            MARKPARITY     = 3,
            SPACEPARITY    = 4
        }

        enum ACIA_STATUS
        {
            ACIA_RDRF       = 0x01,
            ACIA_TDRE       = 0x02,
            ACIA_DCD        = 0x04,
            ACIA_CTS        = 0x08,
            ACIA_FE         = 0x10,
            ACIA_OVRN       = 0x20,
            ACIA_PE         = 0x40,
            ACIA_IRQ        = 0x80
        }

        enum ACIA_IRQ
        {
            ACIA_TDRE_IRQ_ENABLED = 0x20,
            ACIA_RDRF_IRQ_ENABLED = 0x80
        }

        public class ACIAConfig
        {
            public int _address          = 0;
            public int _rtsControl       = 0;
            public int _inputBufferSize  = 0;
            public int _outputBufferSize = 0;

            public int _baudRate         = 9600;
            public int _parity           = 0;
            public int _stopBits         = 1;
            public int _dataBits         = 8;
            public bool _interuptEnabled = false;
            public string _portName = "";
        }

        public List<ACIAConfig> aciaConfigurations = new List<ACIAConfig>();

        int m_nCurrentLogicalPort;
        int         m_nRow;

        bool[] m_nMPSTXInterrupt = new bool[16];
        bool[] m_nMPSRXInterrupt = new bool[16];

        Thread  [] m_hCommWatchThread = new Thread[16];
        volatile bool [] m_nAbortCommThread = new bool[16];

        volatile SerialPort [] m_hCommPort = new SerialPort[16];

        long [] m_nBytesWritten = new long[16];
        bool [] m_nResetACIA = new bool[16];

        bool [] m_nSettingRXInterrupt  = new bool[16];
        bool [] m_nSettingTXInterrupt  = new bool[16];
        bool [] m_nAllowTXInterrupt    = new bool[16];
        bool [] m_nAllowRXInterrupt    = new bool[16];

        //DCB m_dcb[16];

        byte [] m_ACIA_CommandRegister = new byte[16];      // need a separate byte to hold write value
        byte [] m_ACIA_StatusRegister = new byte[16];
        byte [] m_ACIA_DataRegister = new byte[16];
        bool [] m_nInterruptEnabledArray = new bool[16];
        uint [] m_nInterruptMaskArray = new uint[16];

        //SERIAL_INFO [] m_siPort = new SERIAL_INFO[16];      // allow 4 MP-S cards in addition to console

        public override void Init(int nWhichController, byte[] sMemoryBase, ushort sBaseAddress, int nRow, bool bInterruptEnabled)
        {
            for (int i = 0; i < 16; i++)
            {
                m_hCommPort[i] = new SerialPort();
            }

            m_ACIA_StatusRegister[nWhichController] = (byte)ACIA_STATUS.ACIA_TDRE;

            m_nSettingRXInterrupt[nWhichController] = false;
            m_nSettingTXInterrupt[nWhichController] = false;

            m_nMPSTXInterrupt[nWhichController] = false;
            m_nMPSRXInterrupt[nWhichController] = false;

            m_nInterruptEnabledArray[nWhichController] = bInterruptEnabled;
            m_nInterruptMaskArray[nWhichController]    = m_nInterruptMask;

            string board_port = nRow.ToString() + "_" + ((nWhichController % 4) + 1).ToString();

            string baudRate         = Program.GetConfigurationAttribute(Program._configSection + "/SerialPorts/Board_Port", "BaudRate",         board_port, "9600");
            string parity           = Program.GetConfigurationAttribute(Program._configSection + "/SerialPorts/Board_Port", "Parity",           board_port, "0");
            string stopBits         = Program.GetConfigurationAttribute(Program._configSection + "/SerialPorts/Board_Port", "StopBits",         board_port, "1");
            string dataBits         = Program.GetConfigurationAttribute(Program._configSection + "/SerialPorts/Board_Port", "DataBits",         board_port, "8");
            string interruptEnabled = Program.GetConfigurationAttribute(Program._configSection + "/SerialPorts/Board_Port", "InterruptEnabled", board_port, "0");
            string portName         = Program.GetConfigurationAttribute(Program._configSection + "/SerialPorts/Board_Port", "PortName",         board_port, "");

            int _address          = sBaseAddress + ((nWhichController % 4) * 2);

            // these should be added to the Config screen

            int _rtsControl = 0;
            int _inputBufferSize  = 32768;
            int _outputBufferSize = 32768;

            if (baudRate.Length > 0 && parity.Length > 0 && stopBits.Length > 0 && dataBits.Length > 0 && interruptEnabled.Length > 0)
            {
                ACIAConfig conf = new ACIAConfig();

                conf._baudRate        = Convert.ToInt16(baudRate);
                conf._parity          = Convert.ToInt16(parity);
                conf._stopBits        = Convert.ToInt16(stopBits);
                conf._dataBits        = Convert.ToInt16(dataBits);
                conf._interuptEnabled = Convert.ToInt16(interruptEnabled) == 0 ? false : true;

                conf._address          = _address;
                conf._rtsControl       = _rtsControl;
                conf._inputBufferSize  = _inputBufferSize ;
                conf._outputBufferSize = _outputBufferSize;
                conf._portName         = portName;

                aciaConfigurations.Add(conf);
            }

            //ClearInterrupt(nWhichController);
        }

        public int LocateSerialPort(ushort m)
        {
            int portNumber = -1;

            for (int i = 0; i < 16; i++)
            {
                if (aciaConfigurations[i]._address == (m & 0xFFFE))
                {
                    portNumber = i;
                    break;
                }
            }
            return portNumber;
        }

        public override void Write(ushort m, byte b)
        {
            int nIndex = LocateSerialPort(m);

            // if we have a valid port and we can use the m_siPort[nIndex] 
            // structure to access the port and the handle will be in m_hCommPort[nIndex];

            if (nIndex != -1)
            {
                if ((m & 0x0001) == 0)      // command port address
                {
                    bool fSuccess;

                    m_ACIA_CommandRegister[nIndex] = b;

                    // interogate the command bytes and set up the COMx port accordingly
                    //
                    //  Use the combination of m_siPort[i].sBaudRate and the
                    //      m_n9600
                    //      m_n4800
                    //      m_nBR
                    // flags to determine the baud rate
                    //
                    //   w ......00  counter/1
                    //   w ......01  counter/16
                    //   w ......10  counter/64
                    //   w ......11  master reset
                    //   w ...000..  7 bit, even parity, 2 stopbit
                    //   w ...001..  7 bit,  odd parity, 2 stopbit
                    //   w ...010..  7 bit, even parity, 1 stopbit
                    //   w ...011..  7 bit,  odd parity, 1 stopbit
                    //   w ...100..  8 bit,   no parity, 2 stopbit
                    //   w ...101..  8 bit,   no parity, 1 stopbit
                    //   w ...110..  8 bit, even parity, 1 stopbit
                    //   w ...111..  8 bit,  odd parity, 1 stopbit
                    //   w .00.....   rts,  dis/en xmit interrupt
                    //   w .01.....   rts,  en/dis xmit interrupt
                    //   w .10.....  -rts,  dis/en xmit interrupt
                    //   w .11.....   rts,  dis/en xmit intrpt, xmit brk-level
                    //   w 1.......   en/-dis receive interrupt

                    if ((b & 0x03) == 0x03)     // ACIA RSET bits are set
                        m_nResetACIA[nIndex] = true;
                    else
                    {
                        Parity nParity = Parity.None;
                        StopBits nStopBits = StopBits.One;

                        int nBaudRate = 9600, nDataBits = 8, nDivisor = 1;

                        m_nResetACIA[nIndex] = false;
                        if ((b & 0x10) != 0)
                            nDataBits = 8;
                        else
                            nDataBits = 7;
                        switch ((b & 0x1C) >> 2)
                        {
                            case 0: nParity = Parity.Even; nStopBits = StopBits.Two;  break; //   w ...000..  7 bit, even parity, 2 stopbit
                            case 1: nParity = Parity.Odd;  nStopBits = StopBits.Two;  break; //   w ...001..  7 bit,  odd parity, 2 stopbit
                            case 2: nParity = Parity.Even; nStopBits = StopBits.None; break; //   w ...010..  7 bit, even parity, 1 stopbit
                            case 3: nParity = Parity.Odd;  nStopBits = StopBits.None; break; //   w ...011..  7 bit,  odd parity, 1 stopbit
                            case 4: nParity = Parity.None; nStopBits = StopBits.Two;  break; //   w ...100..  8 bit,   no parity, 2 stopbit
                            case 5: nParity = Parity.None; nStopBits = StopBits.None; break; //   w ...101..  8 bit,   no parity, 1 stopbit
                            case 6: nParity = Parity.Even; nStopBits = StopBits.None; break; //   w ...110..  8 bit, even parity, 1 stopbit
                            case 7: nParity = Parity.Odd;  nStopBits = StopBits.None; break; //   w ...111..  8 bit,  odd parity, 1 stopbit
                        }

                        switch (b & 0x03)
                        {
                            case 0: nDivisor = 1;  break;      //   w ......00  counter/1
                            case 1: nDivisor = 16; break;      //   w ......01  counter/16
                            case 2: nDivisor = 64; break;      //   w ......02  counter/64
                        }

                        nBaudRate = aciaConfigurations[nIndex]._baudRate;
                        switch (nBaudRate)
                        {
                            case 110:
                            case 300:
                            case 1200:
                                break;
                            case 150:
                                nBaudRate = 9600;
                                break;
                            case 600:
                                nBaudRate = 4800;
                                break;
                            default:
                                nBaudRate = 9600;
                                break;
                        }

                        nBaudRate *= 16;

                        if (Program.SwitchLowHigh)
                            nBaudRate *= 4;

                        nBaudRate = nBaudRate / nDivisor;

                        m_hCommPort[nIndex].PortName = aciaConfigurations[nIndex]._portName;

                        // do not allow None or OnePointFive - SerialPort doe like it.

                        if (nStopBits == StopBits.None)
                            nStopBits = StopBits.One;
                        else if (nStopBits == StopBits.OnePointFive)
                            nStopBits = StopBits.Two;

                        m_hCommPort[nIndex].BaudRate = nBaudRate;
                        m_hCommPort[nIndex].DataBits = nDataBits;
                        m_hCommPort[nIndex].Parity   = nParity;
                        m_hCommPort[nIndex].StopBits = nStopBits;

                        m_hCommPort[nIndex].DataReceived += new SerialDataReceivedEventHandler(ACIA_DataReceived);

                        try
                        {
                            m_hCommPort[nIndex].Open();             // fSuccess = SetCommState(m_hCommPort[nIndex], &m_dcb[nIndex]);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }

                        int nClrFunction;
                        int nSetFunction;

                        //if (m_siPort[nIndex].nRTSControl != 0)
                        //{
                        //    nClrFunction = SETDTR;
                        //    nSetFunction = CLRDTR;
                        //}
                        //else
                        //{
                        //    nClrFunction = CLRRTS;
                        //    nSetFunction = SETRTS;
                        //}

                        //switch (b & 0x60)
                        //{
                        //    case 0x00:
                        //        EscapeCommFunction(m_hCommPort[nIndex], nClrFunction);
                        //        m_nAllowTXInterrupt[nIndex] = FALSE;
                        //        m_nMPSTXInterrupt[nIndex] = FALSE;
                        //        if (m_nMPSTXInterrupt[nIndex] | m_nMPSRXInterrupt[nIndex])
                        //            SetInterrupt(nIndex);
                        //        else
                        //            ClearInterrupt(nIndex);
                        //        break;
                        //    case 0x20:
                        //        EscapeCommFunction(m_hCommPort[nIndex], nClrFunction);
                        //        m_nAllowTXInterrupt[nIndex] = TRUE;
                        //        if (m_nInterruptEnabledArray[nIndex])
                        //        {
                        //            m_nMPSTXInterrupt[nIndex] = TRUE;
                        //            SetInterrupt(nIndex);
                        //        }
                        //        m_ACIA_StatusRegister[nIndex] |= ACIA_IRQ;
                        //        break;
                        //    case 0x40:
                        //        EscapeCommFunction(m_hCommPort[nIndex], nSetFunction);
                        //        m_nAllowTXInterrupt[nIndex] = FALSE;
                        //        m_nMPSTXInterrupt[nIndex] = FALSE;
                        //        if (m_nMPSTXInterrupt[nIndex] | m_nMPSRXInterrupt[nIndex])
                        //            SetInterrupt(nIndex);
                        //        else
                        //            ClearInterrupt(nIndex);
                        //        break;
                        //    case 0x60:
                        //        EscapeCommFunction(m_hCommPort[nIndex], nClrFunction);
                        //        m_nAllowTXInterrupt[nIndex] = FALSE;
                        //        m_nMPSTXInterrupt[nIndex] = FALSE;
                        //        if (m_nMPSTXInterrupt[nIndex] | m_nMPSRXInterrupt[nIndex])
                        //            SetInterrupt(nIndex);
                        //        else
                        //            ClearInterrupt(nIndex);
                        //        break;
                        //}

                        //if (b & 0x80)
                        //    m_nAllowRXInterrupt[nIndex] = TRUE;
                        //else
                        //    m_nAllowRXInterrupt[nIndex] = FALSE;
                    }
                }
                else                        // data port address
                {
                    // write data to the file. 

                    while (m_nSettingTXInterrupt[nIndex])
                        if (m_nAbortCommThread[nIndex])
                            break;


                    if (m_nMPSRXInterrupt[nIndex] == false)
                    {
                        byte status = (byte)(ACIA_STATUS.ACIA_IRQ | ACIA_STATUS.ACIA_TDRE);
                        m_ACIA_StatusRegister[nIndex] &= (byte)(~status);
                    }
                    else
                    {
                        byte status = (byte)ACIA_STATUS.ACIA_TDRE;
                        m_ACIA_StatusRegister[nIndex] &= (byte)(~status);
                    }

                    m_nSettingTXInterrupt[nIndex] = true;
                    m_nMPSTXInterrupt[nIndex] = false;
                    //if (m_nMPSTXInterrupt[nIndex] | m_nMPSRXInterrupt[nIndex])
                    //    SetInterrupt(nIndex);
                    //else
                    //    ClearInterrupt(nIndex);
                    m_nSettingTXInterrupt[nIndex] = false;

                    //WriteFile(m_hCommPort[nIndex], &b, 1, &m_nBytesWritten[nIndex], &m_stOverlapped[nIndex]);
                    byte[] ba = new byte[1];
                    ba[0] = b;
                    m_hCommPort[nIndex].Write(ba, 0, 1);
                }
            }
        }

        void ACIA_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
