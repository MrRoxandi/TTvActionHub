using System.Collections.Concurrent;
using System.Text;
using TTvActionHub.Logs;

namespace TTvActionHub.BackEnds;

internal partial class MainLogger : ILogger, IAsyncDisposable, IDisposable
{
    private readonly ConcurrentQueue<string> _logQueue = new();
    private readonly ConcurrentQueue<string> _lastLogsForDisplay = new();
    private readonly Task _processingTask;
    private readonly CancellationTokenSource _cts = new();
    private readonly StreamWriter _writer;

    private const int MaxDisplayLogs = 350;
    private bool _disposed;

    public MainLogger()
    {
        var dir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
        Directory.CreateDirectory(dir);
        var filepath = Path.Combine(dir, $"{DateTime.Now:yyyy-MM-dd-HH.mm}.txt");
        var fileStream = new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.Read);
        _writer = new StreamWriter(fileStream) { AutoFlush = true };
        _processingTask = Task.Run(ProcessLogQueueAsync);
    }

    private async Task ProcessLogQueueAsync()
    {
        while (!_cts.IsCancellationRequested)
            try
            {
                await Task.Delay(100, _cts.Token);
                if (_logQueue.IsEmpty) continue;

                var batch = new StringBuilder();
                while (_logQueue.TryDequeue(out var log)) batch.Append(log);
                await _writer.WriteAsync(batch.ToString());
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CRITICAL LOGGER Error]: Failed to write logs. {ex}");
            }

        await FlushRemainingLogsAsync();
    }

    private async Task FlushRemainingLogsAsync()
    {
        try
        {
            var batch = new StringBuilder();
            while (_logQueue.TryDequeue(out var message)) batch.Append(message);
            if (batch.Length > 0) await _writer.WriteAsync(batch.ToString());
            await _writer.FlushAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CRITICAL LOGGER Error]: Failed to flush remaining logs. {ex}");
        }
    }

    private void QueueLog(string message, Exception? err = null)
    {
        var timestamp = DateTime.Now;
        var consoleMessage = new StringBuilder();
        var fileMessage = new StringBuilder();

        consoleMessage.Append($"[{timestamp:HH:mm:ss}]: ");
        fileMessage.Append($"[{timestamp:HH:mm:ss}]: ");

        if (err is null)
        {
            consoleMessage.Append(message);
            fileMessage.Append(message).AppendLine();
        }
        else
        {
            consoleMessage.Append($"{message} {err.Message} (full trace in file)");
            fileMessage.Append(message).AppendLine()
                .Append($"\tError message: {err.Message}").AppendLine()
                .Append($"\tStack trace: {err.StackTrace}").AppendLine();
        }

        _logQueue.Enqueue(fileMessage.ToString());
        _lastLogsForDisplay.Enqueue(consoleMessage.ToString());
        while (_lastLogsForDisplay.Count > MaxDisplayLogs) _lastLogsForDisplay.TryDequeue(out _);
    }

    public Task Info(string message)
    {
        QueueLog($"[Info] {message}.");
        return Task.CompletedTask;
    }

    public Task Error(string message, Exception? err = null)
    {
        QueueLog($"[ERR] {message}.", err);
        return Task.CompletedTask;
    }

    public Task Warn(string message)
    {
        QueueLog($"[WARN] {message}.");
        return Task.CompletedTask;
    }

    public Task Log(string type, string name, string message, Exception? err = null)
    {
        QueueLog($"[{name}:{type}] {message}", err);
        return Task.CompletedTask;
    }

    public IEnumerable<string> GetLastLogs()
    {
        return _lastLogsForDisplay.ToArray();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        await _cts.CancelAsync();
        await _processingTask;
        _cts.Dispose();
        await _writer.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}