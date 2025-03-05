using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TTvActionHub.Logs.BackEnds
{
    internal class ConsoleLogger
    {
        public void Error(string message, string? err = null)
        {
            Console.WriteLine($"[ERR] {message} {err ?? ""}.");
        }

        public void Log(string type, string name, string message, string? err = null)
        {
            Console.WriteLine($"[{name}:{type}] {message} {err ?? ""}.");
        }

        public void Info(string message)
        {
            Console.WriteLine($"[INFO] {message}.");
        }

        public void Warn(string message)
        {
            Console.WriteLine($"[WARN] {message}.");
        }
    }
}
