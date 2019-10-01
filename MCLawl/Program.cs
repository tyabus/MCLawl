using System;
using System.IO;
using System.Threading;

namespace Starter
{
    class Program
    {
        static void Main(string[] args)
        {
            if (File.Exists("MCLawl_.dll"))
            {
                openServer(args);
            }
            else
            {
                Console.WriteLine("Can't find MCLawl_.dll!");
                Thread.Sleep(2500);
                Environment.Exit(1);
            }
        }

        static void openServer(string[] args)
        {
            MCLawl.Gui.Program.Main(args);
        }
    }
}
