using System;
using static AnsiHelper;

namespace FilesUpdaterLib
{
    public static class ConsoleHelper
    {
        public static void WriteLine(string d)
        {
            Console.WriteLine(AnsiColors.DarkGray + $"[{DateTime.Now.ToString("HH:mmmm:ss")}] " + d);
        }

        public static void Write(string d)
        {
            Console.Write(AnsiColors.DarkGray + $"[{DateTime.Now.ToString("HH:mmmm:ss")}] " + d);
        }
    }
}