using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;

using System.Threading;

namespace Memulator
{
    class console
    {
        //private Cpu _cpu;
        //public Cpu Cpu
        //{
        //    get { return _cpu; }
        //    set { _cpu = value; }
        //}

        private TS950 _terminal = new TS950();
        public TS950 Terminal
        {
            get { return _terminal; }
            set { _terminal = value; }
        }

        public static Keyboard _keyboard = new Keyboard();

        public void run()
        {
            _terminal = new TS950();

            int timeToWaitForCpuTpStart = 3;

            for (int i = 0; i < timeToWaitForCpuTpStart; i++)
            {
                if (Program._cpu.Running)
                    break;

                Thread.Sleep(1000);
            }

            if (Program._cpu.Running)
            {
                while (Program._cpu.Running)
                {
                    if (Console.KeyAvailable)
                    {
                        ConsoleKeyInfo cki = Console.ReadKey(true);
                        _keyboard.ProcessKeystroke(cki);
                    }
                }
            }
            else
                Console.WriteLine("CPU thread failed to start in " + timeToWaitForCpuTpStart.ToString() + " seconds");
        }
    }
}
