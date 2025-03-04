using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitchController.Logs.BackEnds
{
    internal class ConsoleFileLogger : ILogger, IDisposable
    {
        private string _filepath;
        private StreamWriter _writer;
        private object _lock = new object();
        private bool _disposed = false;

        public ConsoleFileLogger()
        {
            var dir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
            Directory.CreateDirectory(dir);
            _filepath = Path.Combine(dir, DateTime.Now.ToString("yyyy-MM-dd-HH.mm") + ".txt");
            _writer = new(new FileStream(_filepath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                AutoFlush = true
            };

        }

        private async Task InternalLogAsync(string message)
        {
            string formatted = message + Environment.NewLine;
            try
            {
                lock (_lock)
                {
                    _writer.Write(formatted);
                }
                await Task.Run(() => Console.WriteLine(message));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Logger error]: {ex}");
            }
        }

        public Task Info(string message)
        {
            return InternalLogAsync($"[INFO] {message}.");
        }

        public Task Error(string message, string? err = null)
        {
            return InternalLogAsync($"[ERR] {message} {err ?? ""}.");
        }

        public Task Warn(string message)
        {
            return InternalLogAsync($"[WARN] {message}.");
        }

        public Task Log(string type, string name, string message, string? err = null)
        {
            return InternalLogAsync($"[{name}:{type}] {message} {err ?? ""}.");
        }


        public void Dispose()
        {
            if (!_disposed)
            {
                _writer?.Dispose();
                _disposed = true;
            }
        }

    }
}
