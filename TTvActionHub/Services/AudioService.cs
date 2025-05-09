using LibVLCSharp.Shared;
using System.Collections.Concurrent;
using TTvActionHub.Logs;

namespace TTvActionHub.Services
{
    internal enum PlaybackState
    {
        Playing = 0, Stopped = 1, Paused = 2
    }

    public sealed partial class AudioService : IService, IDisposable
    {
        private CancellationTokenSource? _serviceCancellationToken;
        private TaskCompletionSource<bool>? _soundCompletionSource;
        private readonly ConcurrentQueue<Uri> _soundQueue = new();
        private PlaybackState _playbackState = PlaybackState.Stopped;
        private string _currentPlayingFile = string.Empty;
        private MediaPlayer? _mediaPlayer;
        private Task? _workerTask;
        private LibVLC? _libVlc;

        public event EventHandler<ServiceStatusEventArgs>? StatusChanged;

        private void OnPlaybackEncounteredError(object? sender, EventArgs e)
        {
            if (_playbackState != PlaybackState.Playing) return;
            _playbackState = PlaybackState.Stopped;
            _soundCompletionSource?.TrySetException(new Exception($"{ServiceName} failed to play audio"));
            Logger.Log(LogType.Error, ServiceName, $"Error during playback for: {_currentPlayingFile}");
        }

        private void OnPlaybackEndReached(object? sender, EventArgs e)
        {
            if (_playbackState != PlaybackState.Playing) return;
            _playbackState = PlaybackState.Stopped;
            if (!_soundCompletionSource!.TrySetResult(true))
            {
                _soundCompletionSource.TrySetException(new Exception($"{ServiceName} failed to set stop state"));
            }
            Logger.Log(LogType.Info, ServiceName, $"Playback finished for: {_currentPlayingFile}");
        }

        public void Run()
        {
            try
            {
                Core.Initialize();
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, ServiceName, "Unable to initialize service due to error:", ex);
                OnStatusChanged(false, "Unable to initialize service due to error. Check logs");
                return;
            }
            _libVlc = new();
            _mediaPlayer = new(_libVlc);
            _mediaPlayer.EndReached += OnPlaybackEndReached;
            _mediaPlayer.EncounteredError += OnPlaybackEncounteredError;
            _serviceCancellationToken = new();
            _soundCompletionSource = new();
            _workerTask = Task.Run(ProcessSoundQueueAsync, _serviceCancellationToken.Token);
            _soundCompletionSource.TrySetResult(true);
            Logger.Log(LogType.Info, ServiceName, "Sound service is running");
            OnStatusChanged(true);
            IsRunning = true;
        }

        public void Stop()
        {
            Logger.Log(LogType.Info, ServiceName, "Sound service is stopping");
            _serviceCancellationToken?.Cancel();
            try
            {
                _workerTask?.GetAwaiter().GetResult();
            }
            catch (AggregateException ex)
            {
                foreach (var innerEx in ex.InnerExceptions)
                {
                    if (!_soundCompletionSource?.Task.Result ?? true) 
                        Logger.Log(LogType.Error, ServiceName, "Exception during sound processing:", innerEx);
                }
            }
            _mediaPlayer!.EndReached -= OnPlaybackEndReached;
            _mediaPlayer!.EncounteredError -= OnPlaybackEncounteredError;
            _mediaPlayer?.Stop();
            _mediaPlayer?.Dispose();
            _libVlc?.Dispose();
            OnStatusChanged(false);
            IsRunning = false;
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
                Logger.Log(LogType.Warning, ServiceName, "Nothing to skip right now");
                return;
            }
            Logger.Log(LogType.Info, ServiceName, $"Skipping playback for {_currentPlayingFile}");
            _soundCompletionSource!.TrySetResult(false);
            _playbackState = PlaybackState.Stopped;
            _mediaPlayer?.Stop();
        }

        public float GetVolume() => (_mediaPlayer?.Volume ?? 0) / (float)100.0;

        public void SetVolume(float volume)
        {
            switch (volume)
            {
                case < 0:
                    throw new ArgumentOutOfRangeException(nameof(volume), "Minimum value for volume is 0.0");
                case > 1:
                    throw new ArgumentOutOfRangeException(nameof(volume), "Maximum value for volume is 1.0");
                default:
                    Logger.Log(LogType.Info, ServiceName, $"Setting volume to {volume}");
                    _mediaPlayer!.Volume = (int)(volume * 100);
                    break;
            }
        }

        public Task PlaySoundAsync(Uri audioSourceUri)
        {
            ArgumentNullException.ThrowIfNull(audioSourceUri, nameof(audioSourceUri));
            _soundQueue.Enqueue(audioSourceUri);
            return Task.CompletedTask;
        }

        private async Task ProcessSoundQueueAsync()
        {
            while (!_serviceCancellationToken!.IsCancellationRequested)
            {
                if (_soundCompletionSource!.Task.IsCompleted && _soundQueue.TryDequeue(out var audioUri))
                {
                    // if for some reason audioUri is null. Skipping
                    if (string.IsNullOrEmpty(audioUri.OriginalString)) continue;
                    try
                    {
                        // Handle audioUri here.
                        await ProcessSoundUri(audioUri);

                    } catch (Exception ex)
                    {
                        Logger.Log(LogType.Error, ServiceName, "Error processing sound request:", ex);
                    }
                }
                else
                {
                    // Wait if we cant get audioUri from queue.
                    await Task.Delay(TimeSpan.FromMilliseconds(100), _serviceCancellationToken.Token);
                }
            }
            Logger.Log(LogType.Info, ServiceName, "Sound queue processing stopped");
        }

        private async Task ProcessSoundUri(Uri audioUri)
        {
            var path = audioUri.LocalPath;
            _currentPlayingFile = audioUri.OriginalString;
            try
            {
                if (audioUri.IsFile)
                {
                    if (!File.Exists(path))
                    {
                        throw new FileNotFoundException($"File not found: {path}");
                    }
                    using var media = new Media(_libVlc!, audioUri);
                    await PlayMediaAsync(media);
                } else
                {
                    using var media = new Media(_libVlc!, audioUri.OriginalString, FromType.FromLocation);
                    await media.Parse(MediaParseOptions.ParseNetwork);
                    await PlayMediaAsync(media);
                }
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Error, ServiceName, "Error occurred during processing sound uri: ", e);
            }
            
        }

        private async Task PlayMediaAsync(Media media)
        {
            if (_mediaPlayer == null)
            {
                throw new InvalidOperationException("Media player is not initialized");
            }
            try
            {
                _playbackState = PlaybackState.Playing;
                _soundCompletionSource = new();
                _mediaPlayer.Play(media.SubItems.Count > 0 ? media.SubItems.First() : media);
                Logger.Log(LogType.Info, ServiceName, $"Playback started for: {_currentPlayingFile}");

                var completedTask = await Task.WhenAny(_soundCompletionSource.Task, Task.Delay(Timeout.Infinite, _serviceCancellationToken!.Token));
                if (completedTask == _soundCompletionSource.Task)
                {
                    if (completedTask.IsFaulted)
                    {
                        var ex = completedTask.Exception.InnerException;
                        if (ex != null) throw ex;
                    }
                    else if (_soundCompletionSource.Task.Result == false)
                    {
                        Logger.Log(LogType.Info, ServiceName, "Playback cancelled externally");
                    }
                }
                else if (completedTask.IsCanceled)
                {
                    Logger.Log(LogType.Info, ServiceName, "Playback cancelled via CancellationToken");
                    _playbackState = PlaybackState.Stopped;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, ServiceName, $"Error during PlayMediaAsync for {_currentPlayingFile}", ex);
                throw;
            }
            finally
            {
                _playbackState = PlaybackState.Stopped;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing) return;
            Stop();
            _serviceCancellationToken?.Dispose();
        }

        public string ServiceName => "AudioService";

        public bool IsRunning { get; private set; }

        private void OnStatusChanged(bool isRunning, string? message = null)
        {
            try
            {
                StatusChanged?.Invoke(this, new ServiceStatusEventArgs(ServiceName, isRunning, message));
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, ServiceName, "Error invoking StatusChanged event handler.", ex);
            }

        }
    }
}