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
        private readonly ConcurrentQueue<Uri> _soundQueue = new();
        private readonly CancellationTokenSource _serviceCancellationToken = new();
        private TaskCompletionSource<bool> _soundCompletionSource = new();
        private Task? _workerTask;
        private string _currentPlayingFile = string.Empty; 
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
                _soundCompletionSource.TrySetException(new Exception($"{ServiceName} failed to play audio"));
                Logger.Log(LOGTYPE.ERROR, ServiceName, $"Error during playback for: {_currentPlayingFile}");
            }
        }

        private void OnPlaybackEndReached(object? sender, EventArgs e)
        {
            if (_playbackState == PlaybackState.Playing)
            {
                _playbackState = PlaybackState.Stopped;
                if (!_soundCompletionSource.TrySetResult(true))
                {
                    _soundCompletionSource.TrySetException(new Exception($"{ServiceName} failed to set stop state"));
                }
                Logger.Log(LOGTYPE.INFO, ServiceName, $"Playback finished for: {_currentPlayingFile}");
            }
        }

        public void Run()
        {
            _workerTask = Task.Run(ProcessSoundQueueAsync, _serviceCancellationToken.Token);
            _soundCompletionSource.TrySetResult(true);
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
            while (!_serviceCancellationToken.IsCancellationRequested)
            {
                if (_soundCompletionSource.Task.IsCompleted && _soundQueue.TryDequeue(out var audioUri))
                {
                    // if for some reason audioUri is null. Skipping
                    if (audioUri == null) continue;
                    try
                    {
                        // Handle audioUri here.
                        await Task.Run(() => ProcessSoundUri(audioUri));

                    } catch (Exception ex)
                    {
                        Logger.Log(LOGTYPE.ERROR, ServiceName, "Error processing sound reques:", ex);
                    }
                }
                else
                {
                    // Wait if we cant get audioUri from queue.
                    await Task.Delay(TimeSpan.FromMilliseconds(100), _serviceCancellationToken.Token);
                }
            }
            Logger.Log(LOGTYPE.INFO, ServiceName, "Sound queue processing stopped");
        }

        private async Task ProcessSoundUri(Uri audioUri)
        {
            string path = audioUri.LocalPath;
            _currentPlayingFile = audioUri.OriginalString;
            try
            {
                if (audioUri.IsFile)
                {
                    if (!File.Exists(path))
                    {
                        throw new FileNotFoundException($"File not found: {path}");
                    }
                    using var media = new Media(_libVLC!, audioUri);
                    await PlayMediaAsync(media);
                } else
                {
                    using var media = new Media(_libVLC!, audioUri.OriginalString, FromType.FromLocation);
                    await media.Parse(MediaParseOptions.ParseNetwork);
                    await PlayMediaAsync(media);
                }
            }
            catch (Exception e)
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, "Error ocured during processing ssound uri: ", e);
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
                //_mediaPlayer.Media = media;
                _playbackState = PlaybackState.Playing;
                _soundCompletionSource = new();
                if (media.SubItems.Count > 0)
                {
                    _mediaPlayer.Play(media.SubItems.First());
                } else
                    _mediaPlayer.Play(media);
                Logger.Log(LOGTYPE.INFO, ServiceName, $"Playback started for: {_currentPlayingFile}");

                var comletedTask = await Task.WhenAny(_soundCompletionSource.Task, Task.Delay(Timeout.Infinite, _serviceCancellationToken.Token));
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