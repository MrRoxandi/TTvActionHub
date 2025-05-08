using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTvActionHub.Logs;

namespace TTvActionHub.BackEnds
{
    internal class MainLogger : ILogger, IDisposable
    {
        private readonly StreamWriter _writer;
        private readonly object _lock = new();
        private bool _disposed = false;
        private readonly List<string> _lastLogs = [];

        public MainLogger()
        {
            var dir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
            Directory.CreateDirectory(dir);
            var filepath = Path.Combine(dir, DateTime.Now.ToString("yyyy-MM-dd-HH.mm") + ".txt");
            _writer = new(new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                AutoFlush = true
            };

        }

        private async Task InternalLogAsync(string message, Exception? err = null)
        {
            string consoleMessage, fileMessage;
            if (err is null)
            {
                consoleMessage = $"{message}";
                fileMessage = message + Environment.NewLine;
            }
            else
            {
                consoleMessage = $"{message} {err.Message} (full trace in file)"; 
                fileMessage = $"{message}\n" +
                    $"\tError message: {err.Message}\n" +
                    $"\tStack trace: {err.StackTrace}\n";
            }
            try
            {
                lock (_lock)
                {
                    _writer.Write(fileMessage);
                    _lastLogs.Add(consoleMessage);

                    if (_lastLogs.Count > 200) 
                    {
                        _lastLogs.RemoveRange(0, _lastLogs.Count - 200);
                    }
                }
                await Task.CompletedTask; 
            }
            catch (ObjectDisposedException)
            {
                await Task.CompletedTask; 
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CRITICAL LOGGER ERROR]: Failed to write log. {ex}");
                await Task.CompletedTask; 
            }
        }

        public Task Info(string message)
        {
            return InternalLogAsync($"[INFO] {message}.");
        }

        public Task Error(string message, Exception? err = null)
        {
            return InternalLogAsync($"[ERR] {message}.", err);
        }

        public Task Warn(string message)
        {
            return InternalLogAsync($"[WARN] {message}.");
        }

        public Task Log(string type, string name, string message, Exception? err = null)
        {
            return InternalLogAsync($"[{name}:{type}] {message}", err);
        }

        public IEnumerable<string> GetLastLogs()
        {
            lock (_lock)
            {
                return [.. _lastLogs];
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _writer?.Dispose();
            _disposed = true;
        }

    }
}
