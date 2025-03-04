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
        private readonly CancellationTokenSource _serviceCancellationToken = new();
        private TaskCompletionSource<bool> _soundCompletionSource = new();
        private Task? _workerTask;

        private static IWaveProvider GetReader(Stream stream, string fileExtension) => fileExtension switch
        {
            ".mp3" => new Mp3FileReader(stream),
            ".wav" => new WaveFileReader(stream),
            ".ogg" => new VorbisWaveReader(stream),
            _ => throw new Exception("Format of this audio file is not supporting")
        };

        public void Run()
        {
            _workerTask = Task.Run(ProcessSoundQueueAsync, _serviceCancellationToken.Token);
            Logger.Log(LOGTYPE.INFO, ServiceName(), "Sound service is running");
        }

        public void Stop()
        {
            Logger.Log(LOGTYPE.INFO, ServiceName(), "Sound service is stopping");
            _serviceCancellationToken.Cancel();

            // Wait for worker task to complete
            try
            {
                _workerTask?.Wait();
            }
            catch (AggregateException ex)
            {
                // Log exceptions from the worker task
                foreach (var innerEx in ex.InnerExceptions)
                {
                    Logger.Log(LOGTYPE.ERROR, ServiceName(), "Exception during sound processing:", innerEx.Message);
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

        public void SkipSound()
        {
            if(_waveOut.PlaybackState != PlaybackState.Playing)
            {
                Logger.Log(LOGTYPE.WARNING, ServiceName(), "Nothing to skip right now");
                return;
            }
            _soundCompletionSource.TrySetResult(false);
        }

        public float GetVolume()
        {
            return _waveOut.Volume;
        }

        public void SetVolume(float volume)
        {
            if(volume < 0) throw new Exception("Minimun value for voleme is 0.0");
            if (volume > 1) throw new Exception("Maximum value for voleme is 1.0");
            Logger.Log(LOGTYPE.INFO, ServiceName(), $"Setting volume to {volume}");
            _waveOut.Volume = volume;
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
            while (!_serviceCancellationToken.Token.IsCancellationRequested)
            {
                if (_soundQueue.TryDequeue(out var soundRequest))
                {
                    string? url = soundRequest.url;
                    string? path = soundRequest.path;

                    try
                    {
                        if (path != null)
                        {
                            await InternalSoundFromDiskAsync(path);
                        }
                        else if (url != null)
                        {
                            await InternalSoundFromUrlAsync(url);
                        }

                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LOGTYPE.ERROR, ServiceName(), "Error processing sound request.", ex.Message);
                    }
                }
                else
                {
                    await Task.Delay(100, _serviceCancellationToken.Token); // Wait if queue is empty
                }
            }
        }

        private async Task InternalSoundFromDiskAsync(string path)
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
                Logger.Log(LOGTYPE.ERROR, ServiceName(), $"Error playing sound from disk: {path}", ex.Message);
                throw; // Re-throw to allow handling higher up.
            }
        }

        private async Task InternalSoundFromUrlAsync(string url)
        {
            string tempFilePath = Path.GetTempFileName();
            string? fileExtension = null;

            try
            {
                Logger.Log(LOGTYPE.INFO, ServiceName(), $"Downloading audio from: {url}");
                using (var response = await _httpClient.GetAsync(url, _serviceCancellationToken.Token).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    fileExtension = Path.GetExtension(url).ToLowerInvariant();
                    using (var stream = await response.Content.ReadAsStreamAsync(_serviceCancellationToken.Token).ConfigureAwait(false))
                    using (var fileStream = File.Create(tempFilePath))
                    {
                        await stream.CopyToAsync(fileStream, _serviceCancellationToken.Token).ConfigureAwait(false);
                    }
                }

                using (var audioStream = File.OpenRead(tempFilePath))
                {
                    await PlayAudioStreamAsync(audioStream, fileExtension).ConfigureAwait(false);
                }
            }
            catch (HttpRequestException ex)
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName(), $"Error downloading audio from: {url}", ex.Message);
                throw;
            }
            catch (OperationCanceledException)
            {
                Logger.Log(LOGTYPE.INFO, ServiceName(), "Download cancelled.");
            }
            catch (Exception ex)
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName(), $"Error playing sound from URL: {url}", ex.Message);
                throw;
            }
            finally
            {
                try
                {
                    if (File.Exists(tempFilePath))
                    {
                        File.Delete(tempFilePath);
                        Logger.Log(LOGTYPE.INFO, ServiceName(), $"Deleted temporary file: {tempFilePath}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(LOGTYPE.ERROR, ServiceName(), $"Error deleting temporary file: {tempFilePath}", ex.Message);
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
                reader = GetReader(audioStream, fileExtension);
                _waveOut.Init(reader);
                _waveOut.Play();
                Logger.Log(LOGTYPE.INFO, ServiceName(), "Playback started");

                _soundCompletionSource = new();
                bool isCompleted = false; // Add a flag

                _waveOut.PlaybackStopped += (sender, args) =>
                {
                    if (isCompleted) return;  // Prevent multiple executions

                    isCompleted = true; // Set the flag
                    if (args.Exception != null)
                    {
                        Logger.Log(LOGTYPE.ERROR, ServiceName(), "Error during playback", args.Exception.Message);
                        _soundCompletionSource.TrySetException(args.Exception);
                    }
                    else
                    {
                        _soundCompletionSource.TrySetResult(true);
                    }
                };

                // Wait for playback to complete or be cancelled.
                await Task.WhenAny(_soundCompletionSource.Task, Task.Delay(Timeout.Infinite, _serviceCancellationToken.Token));

                if (_soundCompletionSource.Task.IsFaulted)
                {
                    var ex = _soundCompletionSource.Task.Exception;
                    if (ex != null && ex.InnerException != null) throw ex.InnerException;
                } 
                else if(_soundCompletionSource.Task.Result == false)
                {
                    Logger.Log(LOGTYPE.INFO, ServiceName(), "Playback cancelled");
                    //_waveOut.Stop();
                } else
                {
                    Logger.Log(LOGTYPE.INFO, ServiceName(), "Playback finished successfully");
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
                _serviceCancellationToken.Dispose();       // Dispose of the cancellation token source.
            }
        }

        public string ServiceName() => "AudioService";
    }
}