using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Proxycord
{
    public class Log
    {
        public static void Info(string msg)
        {
            LogInternal(msg, "INFO", ConsoleColor.Cyan, ConsoleColor.White);
        }

        public static void Warn(string msg)
        {
            LogInternal(msg, "WARN", ConsoleColor.DarkYellow, ConsoleColor.Yellow);
        }

        public static void Error(string msg, Exception exception = null)
        {
            LogInternal(msg, "ERROR", ConsoleColor.DarkRed, ConsoleColor.Red);
        }

        private static void LogInternal(string msg, string tag, ConsoleColor tagColor, ConsoleColor msgColor)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write($"[{DateTime.Now}] ");
            Console.ForegroundColor = tagColor;
            Console.Write($"[{tag}] ");
            Console.ForegroundColor = msgColor;
            Console.WriteLine(msg);
        }
    }
}
