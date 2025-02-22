using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitchController.Logger
{
    internal class ConsoleLogger
    {

        public static void Error(string message)
        {
            Console.WriteLine($"[ERR] {message}");
        }

        public static void Info(string message)
        {
            Console.WriteLine($"[INFO] {message}");
        }

        public static void Warn(string message)
        {
            Console.WriteLine($"[WARN] {message}");
        }

        public static void External(string name, string message, string type)
        {
            Console.WriteLine($"[{name} - {type}] {message}");
        }
    }
}
