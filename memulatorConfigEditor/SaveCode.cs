        #region statemap
        //Dictionary<string, States> statemap = new Dictionary<string, States>
        //{
        //    {"REGEDIT4"                                                                                                        , States.regeditSignature             },
        //    {@"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu]"                                                , States.rootKey                      },
        //    {@"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\6800 Board Configuration]"                       , States.BoardConfiguration6800       },
        //    {@"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\6809 Board Configuration]"                       , States.BoardConfiguration6809       },
        //    {@"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\6800SerialPort]"                                 , States.SerialPort6800               },
        //    {@"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\6809SerialPort]"                                 , States.SerialPort6809               },
        //    {@"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\6800SerialPort8274]"                             , States.SerialPort68008274           },
        //    {@"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\6809SerialPort8274]"                             , States.SerialPort68098274           },
        //    {@"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\AHODevice]"                                      , States.AHODevice                    },
        //    {@"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\AHODevice\AHO]"                                  , States.AHODevice_AHO                },
        //    {@"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\BreakPoints]"                                    , States.BreakPoints                  },
        //    {@"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\Capture]"                                        , States.Capture                      },
        //    {@"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\Directories]"                                    , States.Directories                  },
        //    {@"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\Floppy Create]"                                  , States.FloppyCreate                 },
        //    {@"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\FloppyMaint]"                                    , States.FloppyMaint                  },
        //    {@"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\General]"                                        , States.General                      },
        //    {@"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\KeyboardMap]"                                    , States.KeyboardMap                  },
        //    {@"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\Options]"                                        , States.Options                      },
        //    {@"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\Printer]"                                        , States.Printer                      },
        //    {@"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\Recent File List]"                               , States.RecentFileList               },
        //    {@"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\Settings]"                                       , States.Settings                     },
        //    {@"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\Winchester Drive Configurations]"                , States.WinchesterDriveConfigurations},
        //    {@"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\Winchester Drive Configurations\CMI 5619 (19Mb)]", States.CMI5619                      },
        //    {@"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\Winchester Drive Configurations\CMI 5640 (40Mb)]", States.CMI5640                      },
        //    {@"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\Winchester Drive Configurations\D-514 (RMS)]"    , States.D514                         },
        //    {@"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\Winchester Drive Configurations\D-526 (RMS)]"    , States.D526                         },
        //    {@"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\Winchester Drive Configurations\RMS 506]"        , States.RMS506                       },
        //    {@"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\Winchester Drive Configurations\RMS 509]"        , States.RMS509                       },
        //    {@"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\Winchester Drive Configurations\RMS 512]"        , States.RMS512                       },
        //    {@"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\Winchester Drive Configurations\RMS 518]"        , States.RMS518                       },
        //    {@"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\Winchester Drive Configurations\RO 201]"         , States.RO201                        },
        //    {@"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\Winchester Drive Configurations\RO 202]"         , States.RO202                        },
        //    {@"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\Winchester Drive Configurations\RO 203]"         , States.RO203                        },
        //    {@"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\Winchester Drive Configurations\RO 204]"         , States.RO204                        },
        //    {@"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\Winchester Drive Configurations\Rodime RO5090]"  , States.RO5090                       },
        //    {@"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\Winchester Drive Configurations\ST 412]"         , States.ST412                        },
        //    {@"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\Winchester Drive Configurations\ST 506]"         , States.ST506                        },
        //    {@"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\Winchester Drive Configurations\TMS 503]"        , States.TMS503                       }
        //};
        #endregion

        #region unused string variables
        //string rootKey                          = @"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu]"                                                ;
        //string BoardConfiguration6800           = @"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\6800 Board Configuration]"                       ;
        //string BoardConfiguration6809           = @"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\6809 Board Configuration]"                       ;
        //string SerialPort6800                   = @"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\6800SerialPort]"                                 ;
        //string SerialPort6809                   = @"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\6809SerialPort]"                                 ;
        //string SerialPort68008274               = @"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\6800SerialPort8274]"                             ;
        //string SerialPort68098274               = @"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\6809SerialPort8274]"                             ;
        //string AHODevice                        = @"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\AHODevice]"                                      ;
        //string AHODevice_AHO                    = @"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\AHODevice\AHO]"                                  ;
        //string BreakPoints                      = @"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\BreakPoints]"                                    ;
        //string Capture                          = @"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\Capture]"                                        ;
        //string Directories                      = @"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\Directories]"                                    ;
        //string FloppyCreate                     = @"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\Floppy Create]"                                  ;
        //string FloppyMaint                      = @"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\FloppyMaint]"                                    ;
        //string General                          = @"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\General]"                                        ;
        //string KeyboardMap                      = @"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\KeyboardMap]"                                    ;
        //string Options                          = @"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\Options]"                                        ;
        //string Printer                          = @"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\Printer]"                                        ;
        //string RecentFileList                   = @"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\Recent File List]"                               ;
        //string Settings                         = @"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\Settings]"                                       ;
        //string WinchesterDriveConfigurations    = @"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\Winchester Drive Configurations]"                ;
        //string CMI5619                          = @"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\Winchester Drive Configurations\CMI 5619 (19Mb)]";
        //string CMI5640                          = @"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\Winchester Drive Configurations\CMI 5640 (40Mb)]";
        //string D514                             = @"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\Winchester Drive Configurations\D-514 (RMS)]"    ;
        //string D526                             = @"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\Winchester Drive Configurations\D-526 (RMS)]"    ;
        //string RMS506                           = @"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\Winchester Drive Configurations\RMS 506]"        ;
        //string RMS509                           = @"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\Winchester Drive Configurations\RMS 509]"        ;
        //string RMS512                           = @"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\Winchester Drive Configurations\RMS 512]"        ;
        //string RMS518                           = @"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\Winchester Drive Configurations\RMS 518]"        ;
        //string RO201                            = @"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\Winchester Drive Configurations\RO 201]"         ;
        //string RO202                            = @"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\Winchester Drive Configurations\RO 202]"         ;
        //string RO203                            = @"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\Winchester Drive Configurations\RO 203]"         ;
        //string RO204                            = @"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\Winchester Drive Configurations\RO 204]"         ;
        //string RO5090                           = @"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\Winchester Drive Configurations\Rodime RO5090]"  ;
        //string ST412                            = @"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\Winchester Drive Configurations\ST 412]"         ;
        //string ST506                            = @"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\Winchester Drive Configurations\ST 506]"         ;
        //string TMS503                           = @"[HKEY_CURRENT_USER\Software\EvensonConsultingServices\SWTPCemu\Winchester Drive Configurations\TMS 503]"        ;
        #endregion

#region serial scan version
//States currentState = States.unknown;
//using (StreamReader stream = new StreamReader(File.OpenRead(configFileName)))
//{
//    bool validConfigFile = false;
//    string line = "";

//    while ((line = stream.ReadLine()) != null)
//    {
//        if (line.Trim() != "")
//        {
//            if (statemap.ContainsKey(line))
//            {
//                currentState = statemap[line];
//            }

//            switch (currentState)
//            {
//                case States.unknown:
//                    break;
//                case States.regeditSignature:
//                    validConfigFile = true;
//                    break;
//                case States.rootKey:
//                    if (validConfigFile)
//                    {
//                    }
//                    break;
//                case States.BoardConfiguration6800:
//                    if (validConfigFile)
//                    {
//                        currentState = GetCfgFileBoardInfo(stream, ProcessorType.M6800);
//                    }
//                    break;
//                case States.BoardConfiguration6809:
//                    if (validConfigFile)
//                    {
//                        currentState = GetCfgFileBoardInfo(stream, ProcessorType.M6809);
//                    }
//                    break;
//                case States.SerialPort6800:
//                    if (validConfigFile)
//                    {
//                    }
//                    break;
//                case States.SerialPort6809:
//                    if (validConfigFile)
//                    {
//                    }
//                    break;
//                case States.SerialPort68008274:
//                    if (validConfigFile)
//                    {
//                    }
//                    break;
//                case States.SerialPort68098274:
//                    if (validConfigFile)
//                    {
//                    }
//                    break;
//                case States.AHODevice:
//                    if (validConfigFile)
//                    {
//                    }
//                    break;
//                case States.AHODevice_AHO:
//                    if (validConfigFile)
//                    {
//                    }
//                    break;
//                case States.BreakPoints:
//                    if (validConfigFile)
//                    {
//                    }
//                    break;
//                case States.Capture:
//                    if (validConfigFile)
//                    {
//                    }
//                    break;
//                case States.Directories:
//                    if (validConfigFile)
//                    {
//                    }
//                    break;
//                case States.FloppyCreate:
//                    if (validConfigFile)
//                    {
//                    }
//                    break;
//                case States.FloppyMaint:
//                    if (validConfigFile)
//                    {
//                    }
//                    break;
//                case States.General:
//                    if (validConfigFile)
//                    {
//                        if (line.StartsWith("\"Processor\"=dword:"))
//                        {
//                            RegistryEntry regEntry = GetRegistryKeyTypeValue(line);
//                            int processor = Convert.ToInt16(regEntry.value);

//                            memulatorConfigEditor.ConfigSection = string .Format ("config{0}", processor == 0 ? "6800" : "6809");

//                            for (int i = 0; i < _stBoardInfo680x[processor].Length; i++)
//                            {
//                                memulatorConfigEditor._stBoardInfo[i] = _stBoardInfo680x[processor][i];
//                            }
//                        }
//                    }
//                    break;
//                case States.KeyboardMap:
//                    if (validConfigFile)
//                    {
//                    }
//                    break;
//                case States.Options:
//                    if (validConfigFile)
//                    {
//                    }
//                    break;
//                case States.Printer:
//                    if (validConfigFile)
//                    {
//                    }
//                    break;
//                case States.RecentFileList:
//                    if (validConfigFile)
//                    {
//                    }
//                    break;
//                case States.Settings:
//                    if (validConfigFile)
//                    {
//                    }
//                    break;
//                case States.WinchesterDriveConfigurations:
//                    if (validConfigFile)
//                    {
//                    }
//                    break;
//                case States.CMI5619:
//                    if (validConfigFile)
//                    {
//                    }
//                    break;
//                case States.CMI5640:
//                    if (validConfigFile)
//                    {
//                    }
//                    break;
//                case States.D514:
//                    if (validConfigFile)
//                    {
//                    }
//                    break;
//                case States.D526:
//                    if (validConfigFile)
//                    {
//                    }
//                    break;
//                case States.RMS506:
//                    if (validConfigFile)
//                    {
//                    }
//                    break;
//                case States.RMS509:
//                    if (validConfigFile)
//                    {
//                    }
//                    break;
//                case States.RMS512:
//                    if (validConfigFile)
//                    {
//                    }
//                    break;
//                case States.RMS518:
//                    if (validConfigFile)
//                    {
//                    }
//                    break;
//                case States.RO201:
//                    if (validConfigFile)
//                    {
//                    }
//                    break;
//                case States.RO202:
//                    if (validConfigFile)
//                    {
//                    }
//                    break;
//                case States.RO203:
//                    if (validConfigFile)
//                    {
//                    }
//                    break;
//                case States.RO204:
//                    if (validConfigFile)
//                    {
//                    }
//                    break;
//                case States.RO5090:
//                    if (validConfigFile)
//                    {
//                    }
//                    break;
//                case States.ST412:
//                    if (validConfigFile)
//                    {
//                    }
//                    break;
//                case States.ST506:
//                    if (validConfigFile)
//                    {
//                    }
//                    break;
//                case States.TMS503:
//                    if (validConfigFile)
//                    {
//                    }
//                    break;
//                default:
//                    break;
//            }
//        }
//    }
//}
#endregion

#region GetCfgFileBoardInfo
        //private States GetCfgFileBoardInfo(StreamReader stream, ProcessorType processor)
        //{
        //    int nRow = 0;
        //    string line = "";
        //    States currentState = States.unknown;

        //    while ((line = stream.ReadLine()) != null)
        //    {
        //        if (statemap.ContainsKey(line))
        //        {
        //            currentState = statemap[line];
        //            break;
        //        }
        //        else
        //        {
        //            RegistryEntry regEntry = GetRegistryKeyTypeValue(line);

        //            string[] keyParts = regEntry.key.Split(' ');
        //            nRow = Convert.ToInt32(keyParts[0].Replace("Board", ""));

        //            switch (keyParts[2])
        //            {
        //                case "Type":
        //                    {
        //                        _stBoardInfo680x[(int)processor][nRow] = new memulatorConfigEditor.BoardInfoClass();      // only initialize it when we see the type

        //                        int boardType = Convert.ToUInt16(regEntry.value, 16);
        //                        _stBoardInfo680x[(int)processor][nRow].cDeviceType = (byte)boardType;
        //                    }
        //                    break;
        //                case "Addr":
        //                    {
        //                        int boardAddr = Convert.ToUInt16(regEntry.value, 16);
        //                        _stBoardInfo680x[(int)processor][nRow].sBaseAddress = (ushort)boardAddr;
        //                    }
        //                    break;
        //                case "Size":
        //                    {
        //                        int boardSize = Convert.ToUInt16(regEntry.value);
        //                        _stBoardInfo680x[(int)processor][nRow].sNumberOfBytes = (ushort)boardSize;
        //                    }
        //                    break;
        //                case "GUID":
        //                    {
        //                        _stBoardInfo680x[(int)processor][nRow].strGuid = regEntry.value;
        //                    }
        //                    break;
        //                case "Intr":
        //                    {
        //                        int flag = Convert.ToInt32(regEntry.value);
        //                        _stBoardInfo680x[(int)processor][nRow].bInterruptEnabled = flag == 0 ? false : true;
        //                    }
        //                    break;
        //            }

        //            if (_stBoardInfo680x[(int)processor][nRow].cDeviceType == 0)
        //            {
        //                _stBoardInfo680x[(int)processor][nRow] = null;
        //                break;
        //            }
        //        }
        //    }

        //    return currentState;
        //}

#endregion
