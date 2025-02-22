using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitchController.Logs.BackEnds
{
    internal class FileLogger : ILogger
    {
        private string _filepath;

        public FileLogger() {
            var dir = Directory.GetCurrentDirectory() + "\\Logs";
            Directory.CreateDirectory(dir);
            _filepath = dir + "\\" + DateTime.Now.ToString()
                .Replace(" ", string.Empty)
                .Replace(":", ".") + ".txt";
            File.Create(_filepath).Close();
        }

        public void Error(string message, string? err = null)
        {
            var writer = File.AppendText(_filepath); 
            writer.WriteLine(_filepath, $"[ERR] {message} {err ?? ""}.");
            writer.Close();
        }

        public void External(string type, string name, string message, string? err = null)
        {
            var writer = File.AppendText(_filepath);
            writer.WriteLine($"[{name}:{type}] {message} {err ?? ""}.");
            writer.Close();
        }

        public void Info(string message)
        {
            var writer = File.AppendText(_filepath);
            writer.WriteLine($"[INFO] {message}.");
            writer.Close();
        }

        public void Warn(string message)
        {
            var writer = File.AppendText(_filepath);
            writer.WriteLine($"[WARN] {message}.");
            writer.Close();
        }
    }
}
