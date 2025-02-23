using NAudio.Vorbis;
using NAudio.Wave;
using System.Collections.Concurrent;
using TwitchController.Logs;

namespace TwitchController.Services
{
    public class AudioService : IService, IDisposable
    {
        private readonly HttpClient _httpClient = new();
        private readonly WaveOutEvent _waveOut = new();
        private readonly ConcurrentQueue<(string? url, string? path)> _soundQueue = new();
        private readonly CancellationTokenSource _cts = new();
        private Task _workerTask;

        private static IWaveProvider GetAudioReader(Stream stream, string fileExtension) => fileExtension switch
        {
            ".mp3" => new Mp3FileReader(stream),
            ".wav" => new WaveFileReader(stream),
            ".ogg" => new VorbisWaveReader(stream),
            _ => throw new Exception("Format of this audio file is not supporting")
        };

        public AudioService()
        {
            _workerTask = Task.Run(ProcessSoundQueueAsync, _cts.Token);
        }

        public void Run()
        {
            Logger.External(LOGTYPE.INFO, ServiceName(), "Sound service is running");
        }

        public void Stop()
        {
            Logger.External(LOGTYPE.INFO, ServiceName(), "Sound service is stopping");
            _cts.Cancel();

            // Wait for worker task to complete
            try
            {
                _workerTask.Wait();
            }
            catch (AggregateException ex)
            {
                // Log exceptions from the worker task
                foreach (var innerEx in ex.InnerExceptions)
                {
                    Logger.External(LOGTYPE.ERROR, ServiceName(), "Exception during sound processing:", innerEx.Message);
                }
            }


            _waveOut.Stop();
            _waveOut.Dispose();
            _httpClient.Dispose();
        }

        public void StopPlaying()
        {
            _waveOut.Stop();
        }

        public Task PlaySoundFromDiskAsync(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));
            }

            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"File not found: {path}");
            }

            _soundQueue.Enqueue((null, path));
            return Task.CompletedTask;
        }

        public Task PlaySoundFromUrlAsync(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException("URL cannot be null or empty.", nameof(url));
            }

            _soundQueue.Enqueue((url, null));
            return Task.CompletedTask;
        }

        private async Task ProcessSoundQueueAsync()
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                if (_soundQueue.TryDequeue(out var soundRequest))
                {
                    string? url = soundRequest.url;
                    string? path = soundRequest.path;

                    try
                    {
                        if (path != null)
                        {
                            await PlaySoundFromDiskInternalAsync(path);
                        }
                        else if (url != null)
                        {
                            await PlaySoundFromUrlInternalAsync(url);
                        }

                    }
                    catch (Exception ex)
                    {
                        Logger.External(LOGTYPE.ERROR, ServiceName(), "Error processing sound request.", ex.Message);
                    }
                }
                else
                {
                    await Task.Delay(100, _cts.Token); // Wait if queue is empty
                }
            }
        }

        private async Task PlaySoundFromDiskInternalAsync(string path)
        {
            try
            {
                string fileExtension = Path.GetExtension(path).ToLowerInvariant();

                using (var audioStream = File.OpenRead(path))
                {
                    await PlayAudioStreamAsync(audioStream, fileExtension);
                }
            }
            catch (Exception ex)
            {
                Logger.External(LOGTYPE.ERROR, ServiceName(), $"Error playing sound from disk: {path}", ex.Message);
                throw; // Re-throw to allow handling higher up.
            }
        }

        private async Task PlaySoundFromUrlInternalAsync(string url)
        {
            string tempFilePath = Path.GetTempFileName();
            string? fileExtension = null;

            try
            {
                Logger.External(LOGTYPE.INFO, ServiceName(), $"Downloading audio from: {url}");
                using (var response = await _httpClient.GetAsync(url, _cts.Token))
                {
                    response.EnsureSuccessStatusCode();
                    fileExtension = Path.GetExtension(url).ToLowerInvariant();
                    using (var stream = await response.Content.ReadAsStreamAsync(_cts.Token))
                    using (var fileStream = File.Create(tempFilePath))
                    {
                        await stream.CopyToAsync(fileStream, _cts.Token);
                    }
                }

                using (var audioStream = File.OpenRead(tempFilePath))
                {
                    await PlayAudioStreamAsync(audioStream, fileExtension);
                }
            }
            catch (HttpRequestException ex)
            {
                Logger.External(LOGTYPE.ERROR, ServiceName(), $"Error downloading audio from: {url}", ex.Message);
                throw;
            }
            catch (OperationCanceledException)
            {
                Logger.External(LOGTYPE.INFO, ServiceName(), "Download cancelled.");
            }
            catch (Exception ex)
            {
                Logger.External(LOGTYPE.ERROR, ServiceName(), $"Error playing sound from URL: {url}", ex.Message);
                throw;
            }
            finally
            {
                try
                {
                    if (File.Exists(tempFilePath))
                    {
                        File.Delete(tempFilePath);
                        Logger.External(LOGTYPE.INFO, ServiceName(), $"Deleted temporary file: {tempFilePath}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.External(LOGTYPE.ERROR, ServiceName(), $"Error deleting temporary file: {tempFilePath}", ex.Message);
                }
            }
        }


        private async Task PlayAudioStreamAsync(Stream audioStream, string? fileExtension)
        {
            if (string.IsNullOrEmpty(fileExtension))
            {
                throw new ArgumentException("File extension cannot be null or empty.", nameof(fileExtension));
            }

            // Get WaveOutEvent from the pool
            IWaveProvider? reader = null;
            try
            {
                reader = GetAudioReader(audioStream, fileExtension);
                _waveOut.Init(reader);
                _waveOut.Play();
                Logger.External(LOGTYPE.INFO, ServiceName(), "Playback started");

                var tcs = new TaskCompletionSource<object>();
                bool isCompleted = false; // Add a flag

                _waveOut.PlaybackStopped += (sender, args) =>
                {
                    if (isCompleted) return;  // Prevent multiple executions

                    isCompleted = true; // Set the flag
                    if (args.Exception != null)
                    {
                        Logger.External(LOGTYPE.ERROR, ServiceName(), "Error during playback", args.Exception.Message);
                        tcs.TrySetException(args.Exception);
                    }
                    else
                    {
                        Logger.External(LOGTYPE.INFO, ServiceName(), "Playback finished successfully");
                        tcs.TrySetResult(true);
                    }
                };

                // Wait for playback to complete or be cancelled.
                await Task.WhenAny(tcs.Task, Task.Delay(Timeout.Infinite, _cts.Token));

                if (_cts.Token.IsCancellationRequested)
                {
                    Logger.External(LOGTYPE.INFO, ServiceName(), "Playback cancelled");
                    _waveOut.Stop();
                }
                else if (tcs.Task.IsFaulted)
                {
                    var ex = tcs.Task.Exception;
                    if (ex != null && ex.InnerException != null) throw ex.InnerException;
                }
            }
            finally
            {
                // Ensure resources are cleaned up even if an exception occurs.
                _waveOut.Stop();

                if (reader != null)
                {
                    if (reader is IDisposable disposableReader)
                    {
                        disposableReader.Dispose();
                    }
                    reader = null;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Stop(); // Ensure the worker task and WaveOutEvent instances are stopped.
                _httpClient.Dispose();  //Dispose of the http client
                _cts.Dispose();       // Dispose of the cancellation token source.
            }
        }

        public string ServiceName() => "AudioService";
    }
}