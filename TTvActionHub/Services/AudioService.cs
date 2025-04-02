using LibVLCSharp.Shared;
using System.Collections.Concurrent;
using TTvActionHub.Logs;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace TTvActionHub.Services
{
    enum PlaybackState
    {
        Playing, Stopped, Paused
    }

    public class AudioService : IService, IDisposable
    {
        private MediaPlayer? _mediaPlayer;
        private LibVLC? _libVLC;
        private readonly ConcurrentQueue<Uri?> _soundQueue = new();
        private readonly CancellationTokenSource _serviceCancellationToken = new();
        private TaskCompletionSource<bool> _soundCompletionSource = new();
        private Task? _workerTask;
        private string _currentPlayingFile = string.Empty; // Теперь хранит URL или путь к файлу
        private PlaybackState _playbackState = PlaybackState.Stopped;

        public AudioService()
        {
            try
            {
                Core.Initialize();
                _libVLC = new();
                _mediaPlayer = new(_libVLC);
                _mediaPlayer.EndReached += OnPlaybackEndReached;
                _mediaPlayer.EncounteredError += OnPlaybackEncounteredError;
            }
            catch (Exception ex)
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, "Unable to initialize service due to error:", ex);
                return;
            }
        }

        private void OnPlaybackEncounteredError(object? sender, EventArgs e)
        {
            if (_playbackState == PlaybackState.Playing)
            {
                _playbackState = PlaybackState.Stopped;
                Logger.Log(LOGTYPE.ERROR, ServiceName, $"Error during playback for: {_currentPlayingFile}");
                _soundCompletionSource.TrySetException(new Exception($"{ServiceName} failed to play audio"));
            }
        }

        private void OnPlaybackEndReached(object? sender, EventArgs e)
        {
            if (_playbackState == PlaybackState.Playing)
            {
                _playbackState = PlaybackState.Stopped;
                Logger.Log(LOGTYPE.INFO, ServiceName, $"Playback finished for: {_currentPlayingFile}");
                _soundCompletionSource.TrySetResult(true);
            }
        }

        public void Run()
        {
            _workerTask = Task.Run(ProcessSoundQueueAsync, _serviceCancellationToken.Token);
            Logger.Log(LOGTYPE.INFO, ServiceName, "Sound service is running");
        }

        public void Stop()
        {
            Logger.Log(LOGTYPE.INFO, ServiceName, "Sound service is stopping");
            _serviceCancellationToken.Cancel();
            try
            {
                _workerTask?.Wait();
            }
            catch (AggregateException ex)
            {
                foreach (var innerEx in ex.InnerExceptions)
                {
                    Logger.Log(LOGTYPE.ERROR, ServiceName, "Exception during sound processing:", innerEx);
                }
            }

            _mediaPlayer?.Stop();
            _mediaPlayer?.Dispose();
            _libVLC?.Dispose();
        }

        public void StopPlaying()
        {
            _mediaPlayer?.Stop();
            _playbackState = PlaybackState.Stopped;
        }

        public void SkipSound()
        {
            if (_mediaPlayer?.IsPlaying != true)
            {
                Logger.Log(LOGTYPE.WARNING, ServiceName, "Nothing to skip right now");
                return;
            }
            Logger.Log(LOGTYPE.INFO, ServiceName, $"Skipping playback for {_currentPlayingFile}");
            _soundCompletionSource.TrySetResult(false);
            _playbackState = PlaybackState.Stopped;
            _mediaPlayer?.Stop();
        }

        public float GetVolume() => (_mediaPlayer?.Volume ?? 0) / (float)100.0;

        public void SetVolume(float volume)
        {
            if (volume < 0) throw new ArgumentOutOfRangeException(nameof(volume), "Minimum value for volume is 0.0");
            if (volume > 1) throw new ArgumentOutOfRangeException(nameof(volume), "Maximum value for volume is 1.0");
            Logger.Log(LOGTYPE.INFO, ServiceName, $"Setting volume to {volume}");
            _mediaPlayer!.Volume = (int)(volume * 100);
        }

        public Task PlaySoundAsync(Uri audioSourceUri)
        {
            if (audioSourceUri != null)
            {
                _soundQueue.Enqueue(audioSourceUri);
                return Task.CompletedTask;
            }
            throw new ArgumentNullException(nameof(audioSourceUri));
        }

        private async Task ProcessSoundQueueAsync()
        {
            while (!_serviceCancellationToken.Token.IsCancellationRequested)
            {
                if (_soundQueue.TryDequeue(out var audioSourceUri))
                {
                    if (audioSourceUri == null) continue;
                    try
                    {
                        if (audioSourceUri.IsFile)
                        {
                            await InternalSoundFromDiskAsync(audioSourceUri, _serviceCancellationToken.Token);
                        }
                        else
                        {
                            await InternalSoundFromUriAsync(audioSourceUri, _serviceCancellationToken.Token);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex is not OperationCanceledException)
                            Logger.Log(LOGTYPE.ERROR, ServiceName, "Error processing sound request:", ex);
                        else
                            Logger.Log(LOGTYPE.INFO, ServiceName, "Sound processing canceled");
                    }
                }
                else
                {
                    // Wait if queue is empty
                    await Task.Delay(100, _serviceCancellationToken.Token);
                }
            }
            Logger.Log(LOGTYPE.INFO, ServiceName, "Sound queue processing stopped");
        }

        private async Task InternalSoundFromDiskAsync(Uri pathUri, CancellationToken token)
        {
            string path = pathUri.LocalPath;
            try
            {
                if (!File.Exists(path))
                {
                    throw new FileNotFoundException($"File not found: {path}");
                }
                _currentPlayingFile = path;
                using var media = new Media(_libVLC!, pathUri);
                await PlayMediaAsync(media, token);
            }
            catch (Exception ex)
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, $"Error playing sound from disk: {path}", ex);
                throw;
            }
        }

        private async Task InternalSoundFromUriAsync(Uri urlUri, CancellationToken token)
        {
            string url = urlUri.ToString();
            try
            {
                _currentPlayingFile = url;
                using var media = new Media(_libVLC!, urlUri); // Создаем Media напрямую из URL
                await PlayMediaAsync(media, token);
            }
            catch (Exception ex)
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, $"Error playing sound from URL: {url}", ex);
                throw;
            }
        }


        private async Task PlayMediaAsync(Media media, CancellationToken token)
        {
            if (_mediaPlayer == null)
            {
                throw new InvalidOperationException("Media player is not initialized");
            }
            try
            {
                _mediaPlayer.Media = media;
                _playbackState = PlaybackState.Playing;
                _soundCompletionSource = new();
                _mediaPlayer.Play();
                Logger.Log(LOGTYPE.INFO, ServiceName, $"Playback started for: {_currentPlayingFile}");

                var comletedTask = await Task.WhenAny(_soundCompletionSource.Task, Task.Delay(Timeout.Infinite, token));
                if (comletedTask == _soundCompletionSource.Task)
                {
                    if (comletedTask.IsFaulted)
                    {
                        var ex = comletedTask.Exception.InnerException;
                        if (ex != null) throw ex;
                    }
                    else if (_soundCompletionSource.Task.Result == false)
                    {
                        Logger.Log(LOGTYPE.INFO, ServiceName, "Playback cancelled externally");
                    }
                }
                else if (comletedTask.IsCanceled)
                {
                    Logger.Log(LOGTYPE.INFO, ServiceName, "Playback cancelled via CancellationToken");
                    _playbackState = PlaybackState.Stopped;
                    //_mediaPlayer.Stop();
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, $"Error during PlayMediaAsync for {_currentPlayingFile}", ex);
                throw;
            }
            finally
            {
                _playbackState = PlaybackState.Stopped;
                //_mediaPlayer?.Stop();
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
                Stop();
                _serviceCancellationToken.Dispose();
            }
        }

        public string ServiceName { get => "AudioService"; }
    }
}