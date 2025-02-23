using NAudio.Vorbis;
using NAudio.Wave;
using TwitchController.Services;

namespace TwitchController.LuaTools.Audio
{
    public static class Sounds
    {
        public static AudioService? audio;
        
        public static async Task PlaySoundFromDiscAsync(string path)
        {
            if (audio == null) { throw new Exception("Audio service was not provided"); };

            await audio.PlaySoundFromDiskAsync(path);
        }

        public static async Task PlaySoundFromUrlAsync(string url)
        {
            if (audio == null) { throw new Exception("Audio service was not provided"); };

            await audio.PlaySoundFromUrlAsync(url);
        }
    }
}
