using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitchController.Logs.BackEnds
{
    internal class ConsoleFileLogger : ILogger
    {
        private string _filepath;

        public ConsoleFileLogger()
        {
            var dir = Directory.GetCurrentDirectory() + "\\Logs";
            Directory.CreateDirectory(dir);
            _filepath = dir + "\\" + DateTime.Now.ToString()
                .Replace(" ", string.Empty)
                .Replace(":", ".") + ".txt";
            File.Create(_filepath).Close();
        }

        public void Error(string message, string? err = null)
        {
            File.AppendAllTextAsync(_filepath, $"[ERR] {message} {err ?? ""}.");
            //var writer = File.AppendText(_filepath);
            //writer.WriteLine(_filepath, $"[ERR] {message} {err ?? ""}.");
            //writer.Close();
            Console.WriteLine($"[ERR] {message} {err ?? ""}.");
        }

        public void External(string type, string name, string message, string? err = null)
        {
            //var writer = File.AppendText(_filepath);
            //writer.WriteLine($"[{name}:{type}] {message} {err ?? ""}.");
            //writer.Close();
            File.AppendAllTextAsync(_filepath, $"[{name}:{type}] {message} {err ?? ""}.");
            Console.WriteLine($"[{name}:{type}] {message} {err ?? ""}.");
        }

        public void Info(string message)
        {
            //var writer = File.AppendText(_filepath);
            //writer.WriteLine($"[INFO] {message}.");
            //writer.Close();
            File.AppendAllTextAsync(_filepath, $"[INFO] {message}.");
            Console.WriteLine($"[INFO] {message}.");
        }

        public void Warn(string message)
        {
            //var writer = File.AppendText(_filepath);
            //writer.WriteLine($"[WARN] {message}.");
            //writer.Close();
            File.AppendAllTextAsync(_filepath, $"[WARN] {message}.");
            Console.WriteLine($"[WARN] {message}.");
        }
    }
}
