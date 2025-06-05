using System.Text.Json;

namespace TTvActionHub.BackEnds.Audio;

using System.Collections.Concurrent;
using System.Speech.Synthesis;
using LibVLCSharp.Shared;
using AudioItems;
using Logs;

public sealed class AudioBackEnd
{
    public static string BackEndName => "AudioBackEnd";
    public string AudioDirectory { get; } = Path.Combine(Directory.GetCurrentDirectory(), ".synth");
    public string CurrentPlayingUri { get; private set; } = string.Empty;
    public bool IsPlaying => _mediaPlayer.IsPlaying;

    public int Volume
    {
        get => _mediaPlayer.Volume;
        set => _mediaPlayer.Volume = Math.Clamp(value, 0, 100);
    }
    
    public SynthesizerParameters VoiceParameters { get; set; }

    private readonly CancellationTokenSource _serviceCancellationToken;
    private TaskCompletionSource<bool> _soundCompletionSource;
    private readonly ConcurrentQueue<Uri> _soundQueue = new();
    private readonly HashSet<string> _bannedWords; 
        
    private readonly MediaPlayer _mediaPlayer;
    private readonly LibVLC _libVlc;
    private readonly Task _workerTask;
    
    private const string BannedWordsFileName = "badwords.json";
    private const string CensoredPlaceholder = "[filtered]";
    
    public AudioBackEnd()
    {
        Core.Initialize();
        _libVlc = new LibVLC();
        _mediaPlayer = new MediaPlayer(_libVlc);
        
        if (!Directory.Exists(AudioDirectory))
        {
            Directory.CreateDirectory(AudioDirectory);
        }
        _serviceCancellationToken = new CancellationTokenSource();
        _soundCompletionSource = new TaskCompletionSource<bool>();
        _soundCompletionSource.TrySetResult(true);
        _mediaPlayer.EndReached += OnPlaybackEndReachedHandler;
        _mediaPlayer.EncounteredError += OnPlaybackEncounteredErrorHandler;
        VoiceParameters = new SynthesizerParameters
            { Age = VoiceAge.Adult, Gender = VoiceGender.Female, Rate = 2, Volume = 50 };
        _workerTask = Task.Run(ProcessSoundQueueAsync, _serviceCancellationToken.Token);
        _bannedWords = LoadBannedWords();
    }

    
    private void OnPlaybackEncounteredErrorHandler(object? sender, EventArgs e)
    {
        _soundCompletionSource.TrySetException(new Exception($"{BackEndName} failed to play audio"));
        Logger.Log(LogType.Error, BackEndName, $"Error during playback for: {CurrentPlayingUri}");
    }

    private void OnPlaybackEndReachedHandler(object? sender, EventArgs e)
    {
        if (!_soundCompletionSource!.TrySetResult(true))
            _soundCompletionSource.TrySetException(new Exception($"{BackEndName} failed to set stop state"));
        Logger.Log(LogType.Info, BackEndName, $"Playback finished for: {CurrentPlayingUri}");
    }

    public Task PlaySoundAsync(Uri audioSourceUri)
    {
        _soundQueue.Enqueue(audioSourceUri);
        return Task.CompletedTask;
    }

    public Task VoiceTextAsync(string text)
    {
        var filteredText = FilterText(text);
        if (!string.IsNullOrWhiteSpace(filteredText)) // Если после фильтрации текст пуст
            return Task.Run(() =>
            {
                try
                {
                    using var synthesizer = new SpeechSynthesizer();
                    try
                    {
                        synthesizer.SelectVoiceByHints(VoiceParameters.Gender, VoiceParameters.Age);
                    }
                    catch (Exception ex) 
                    {
                        Logger.Log(LogType.Warning, BackEndName,
                            $"Could not select voice by hints ({VoiceParameters.Gender}/{VoiceParameters.Age}). Using default. Error: {ex.Message}");
                    }

                    synthesizer.Rate = Math.Clamp(VoiceParameters.Rate, -10, 10);
                    synthesizer.Volume = Math.Clamp(VoiceParameters.Volume, 0, 100);

                    var tempFilePath = Path.Combine(AudioDirectory, $"tts_{Path.GetRandomFileName()}.wav");
                    synthesizer.SetOutputToWaveFile(tempFilePath);
                    synthesizer.Speak(filteredText);
                    synthesizer.SetOutputToNull(); 

                    Logger.Log(LogType.Info, BackEndName, $"Generated TTS audio to: {tempFilePath}");
                    _soundQueue.Enqueue(new Uri(tempFilePath, UriKind.Absolute));
                }
                catch (Exception ex)
                {
                    Logger.Log(LogType.Error, BackEndName, "Error during text-to-speech synthesis:", ex);
                }
            }, _serviceCancellationToken.Token);
        Logger.Log(LogType.Info, BackEndName, "Text became empty after filtering and will not be voiced.");
        return Task.CompletedTask;
    }
    
    public void SkipSound()
    {
        if (!IsPlaying)
        {
            Logger.Log(LogType.Info, BackEndName,  "Nothing to skip. Ignoring...");
            return;
        }
        Logger.Log(LogType.Info, BackEndName, $"Skipping playback for {CurrentPlayingUri}");
        _soundCompletionSource.TrySetResult(false);
        _mediaPlayer.Stop();
    }
    
    private async Task ProcessSoundQueueAsync()
    {
        while (!_serviceCancellationToken.IsCancellationRequested)
            if (_soundCompletionSource.Task.IsCompleted && _soundQueue.TryDequeue(out var audioUri))
            {
                // if for some reason audioUri is null. Skipping
                if (string.IsNullOrEmpty(audioUri.OriginalString)) continue;
                try
                {
                    await ProcessSoundUri(audioUri);
                }
                catch (Exception ex)
                {
                    Logger.Log(LogType.Error, BackEndName, "Error processing sound request:", ex);
                }
            }
            else
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100), _serviceCancellationToken.Token);
            }

        Logger.Log(LogType.Info, BackEndName, "Sound queue processing stopped");
    }
    
    private async Task ProcessSoundUri(Uri audioUri)
    {
        CurrentPlayingUri = audioUri.OriginalString;
        try
        {
            if (audioUri.IsFile)
            {
                var path = audioUri.LocalPath;
                if (!File.Exists(path)) throw new FileNotFoundException($"File not found: {path}");
                using var media = new Media(_libVlc, audioUri);
                await PlayMediaAsync(media);
            }
            else
            {
                using var media = new Media(_libVlc, audioUri.OriginalString, FromType.FromLocation);
                await media.Parse(MediaParseOptions.ParseNetwork);
                await PlayMediaAsync(media);
            }
        }
        catch (Exception e)
        {
            Logger.Log(LogType.Error, BackEndName, "Error occurred during processing sound uri: ", e);
        }
    }
    
    private async Task PlayMediaAsync(Media media)
    {
        try
        {
            _soundCompletionSource = new TaskCompletionSource<bool>();
            _mediaPlayer.Play(media.SubItems.Count > 0 ? media.SubItems.First() : media);
            Logger.Log(LogType.Info, BackEndName, $"Playback started for: {CurrentPlayingUri}");

            var completedTask = await Task.WhenAny(_soundCompletionSource.Task,
                Task.Delay(Timeout.Infinite, _serviceCancellationToken.Token));
            if (completedTask == _soundCompletionSource.Task)
            {
                if (completedTask.IsFaulted)
                {
                    var ex = completedTask.Exception.InnerException;
                    if (ex != null) throw ex;
                }
                else if (_soundCompletionSource.Task.Result == false)
                {
                    Logger.Log(LogType.Info, BackEndName, "Playback cancelled externally");
                }
            }
            else if (completedTask.IsCanceled)
            {
                Logger.Log(LogType.Info, BackEndName, "Playback cancelled via CancellationToken");
            }
        }
        catch (Exception ex)
        {
            Logger.Log(LogType.Error, BackEndName, $"Error during PlayMediaAsync for {CurrentPlayingUri}", ex);
            throw;
        }
    }
    
    private static HashSet<string> LoadBannedWords()
    {
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), BannedWordsFileName);
        if (!File.Exists(filePath))
        {
            Logger.Log(LogType.Warning, BackEndName, $"File with banned words not found at '{filePath}'");
            Logger.Log(LogType.Warning, BackEndName, $"Example file will be generated at '{filePath}'");
            try
            {
                var result = JsonSerializer.Serialize<IEnumerable<string>>(["example", "example2"]);
                File.WriteAllText(filePath, result);
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, BackEndName, "Unable to generate example file due to error:", ex);
            }
            return [];
        }

        try
        {
            var jsonContext = File.ReadAllText(filePath);
            var deserializedContext = JsonSerializer.Deserialize<List<string>>(jsonContext);
            if (deserializedContext is not { Count: > 0 })
            {
                Logger.Log(LogType.Warning, BackEndName, "File with banned words is empty. Ignoring...");
                return [];
            }

            var result = deserializedContext.Where(w => !string.IsNullOrWhiteSpace(w)).Select(w => w.Trim()).ToHashSet();
            Logger.Log(LogType.Info, BackEndName, $"Successfully read {result.Count} words.");
            return result;
        }
        catch (Exception ex)
        {
            Logger.Log(LogType.Error, BackEndName, "Unable to read example file:", ex);
            return [];
        }
        
    }

    private string FilterText(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || _bannedWords.Count == 0) return text;
        foreach (var pattern in _bannedWords.Where(pattern => System.Text.RegularExpressions.Regex.IsMatch(text, pattern,
                     System.Text.RegularExpressions.RegexOptions.IgnoreCase)))
        {
            text = System.Text.RegularExpressions.Regex.Replace(text, pattern, CensoredPlaceholder, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
        return text;
    }
}