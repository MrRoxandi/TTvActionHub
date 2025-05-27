namespace TTvActionHub.LuaTools.Services;
using TTvActionHub.BackEnds.Audio;

public static class Audio
{
    public static AudioBackEnd? audio;

    public static async Task PlaySound(string uri)
    {
        if (string.IsNullOrEmpty(uri)) throw new ArgumentNullException(nameof(uri));
        if (audio == null) throw new FieldAccessException("Audio service was not provided");
        await audio.PlaySoundAsync(new Uri(uri));
    }

    public static async Task PlayText(string text)
    {
        if (string.IsNullOrEmpty(text)) throw new ArgumentNullException(nameof(text));
        if (audio == null) throw new Exception("Audio service was not provided");
        await audio.VoiceTextAsync(text);
    }
    
    public static void SkipSound()
    {
        if (audio == null) throw new Exception("Audio service was not provided");
        audio.SkipSound();
    }
    
    public static void SetVolume(int volume)
    {
        if (audio == null) throw new Exception("Audio service was not provided");
        audio.Volume = volume;
    }

    public static int GetVolume()
    {
        if (audio == null) throw new Exception("Audio service was not provided");
        return audio.Volume;
    }

    public static void IncreaseVolume(int volume)
    {
        if (audio == null) throw new Exception("Audio service was not provided");
        audio.Volume += volume;
    }

    public static void DecreaseVolume(int volume)
    {
        if (audio == null) throw new Exception("Audio service was not provided");
        audio.Volume -= volume;
    }
}