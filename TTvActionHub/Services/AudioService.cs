//using LibVLCSharp.Shared;
using LibVLCSharp.Shared;
using System.Collections.Concurrent;
using TTvActionHub.Logs;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace TTvActionHub.Services
{
    enum PlaybackState
    {
        Playing, Stopped, Paused
    }

    public class AudioService: IService, IDisposable 
    {
        private readonly HttpClient _httpClient = new();
        private MediaPlayer? _mediaPlayer; 
        private LibVLC? _libVLC; 
        private readonly ConcurrentQueue<Uri?> _soundQueue = new();
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
                Logger.Log(LOGTYPE.ERROR, ServiceName, "Unable to initilize service due to error:", ex);
                return;
            }
        }

        private void OnPlaybackEncounteredError(object? sender, EventArgs e)
        {
            if(_playbackState == PlaybackState.Playing)
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
            Logger.Log(LOGTYPE.INFO,  ServiceName, "Sound service is running");
        }

        public void Stop()
        {
            Logger.Log(LOGTYPE.INFO,  ServiceName, "Sound service is stopping");
            _serviceCancellationToken.Cancel();
            try
            {
                _workerTask?.Wait();
            }
            catch (AggregateException ex)
            {
                foreach (var innerEx in ex.InnerExceptions)
                {
                    Logger.Log(LOGTYPE.ERROR,  ServiceName, "Exception during sound processing:", innerEx);
                }
            }

            _mediaPlayer?.Stop();
            _mediaPlayer?.Dispose();
            _libVLC?.Dispose();
            _httpClient.Dispose();
        }

        public void StopPlaying()
        {
            _mediaPlayer?.Stop();
            _playbackState = PlaybackState.Stopped;
        }

        public void SkipSound()
        {
            if(_mediaPlayer?.IsPlaying != true)
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
            Logger.Log(LOGTYPE.INFO,  ServiceName, $"Setting volume to {volume}");
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
                    catch (OperationCanceledException )
                    {
                        Logger.Log(LOGTYPE.INFO, ServiceName, "Sound processing canceled");
                        break; // ?
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LOGTYPE.ERROR, ServiceName, "Error processing sound reques:", ex);
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
                Logger.Log(LOGTYPE.ERROR,  ServiceName, $"Error playing sound from disk: {path}", ex);
                throw; // Re-throw to allow handling higher up.
            }
        }

        private async Task InternalSoundFromUriAsync(Uri urlUri, CancellationToken token)
        {
            string url = urlUri.ToString();
            //if (!IsValidUrl(Path.GetExtension(url))) return;
            string tempFilePath = Path.GetTempFileName();

            try
            {
                Logger.Log(LOGTYPE.INFO,  ServiceName, $"Downloading audio from: {url}");
                using (var response = await _httpClient.GetAsync(url, token))
                {
                    response.EnsureSuccessStatusCode();
                    using (var stream = await response.Content.ReadAsStreamAsync(token))
                    using (var fileStream = File.Create(tempFilePath))
                    {
                        await stream.CopyToAsync(fileStream, token);
                    }
                }
                _currentPlayingFile = url;
                using var media = new Media(_libVLC!, new Uri(tempFilePath));
                await PlayMediaAsync(media, token);
            }
            catch (HttpRequestException ex)
            {
                Logger.Log(LOGTYPE.ERROR,  ServiceName, $"Error downloading audio from: {url}", ex);
                throw;
            }
            catch (OperationCanceledException)
            {
                Logger.Log(LOGTYPE.INFO,  ServiceName, "Download cancelled.");
            }
            catch (Exception ex)
            {
                Logger.Log(LOGTYPE.ERROR,  ServiceName, $"Error playing sound from URL: {url}", ex);
                throw;
            }
            finally
            {
                try
                {
                    if (File.Exists(tempFilePath))
                    {
                        File.Delete(tempFilePath);
                        Logger.Log(LOGTYPE.INFO,  ServiceName, $"Deleted temporary file: {tempFilePath}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(LOGTYPE.ERROR,  ServiceName, $"Error deleting temporary file: {tempFilePath}", ex);
                }
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
                _mediaPlayer.Play();
                Logger.Log(LOGTYPE.INFO, ServiceName, $"Playback started for: {_currentPlayingFile}");

                _soundCompletionSource = new();
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
                    _mediaPlayer.Stop();
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
                _mediaPlayer?.Stop();
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
                _httpClient.Dispose();  //Dispose of the http client
                _serviceCancellationToken.Dispose();       // Dispose of the cancellation token source.
            }
        }

        public string ServiceName { get => "AudioService"; }
    }
}