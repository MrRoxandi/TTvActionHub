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

        public static void SetVolume(float volume) 
        {
            if (audio == null) { throw new Exception("Audio service was not provided"); };

            audio.SetVolume(volume);
        }

        public static float GetVolume()
        {
            if (audio == null) { throw new Exception("Audio service was not provided"); };

            return audio.GetVolume();
        }

        public static void IncreeseVolume(float volume)
        {
            if (audio == null) { throw new Exception("Audio service was not provided"); };
            var res = audio.GetVolume() + volume;

            audio.SetVolume((float)(res > 1.0 ? 1.0 : res));
        }

        public static void DecreeseVolume(float volume)
        {
            if (audio == null) { throw new Exception("Audio service was not provided"); };

            var res = audio.GetVolume() - volume;

            audio.SetVolume((float)(res < 0.0 ? 0.0 : res));
        }

        public static void SkipSound()
        {
            if (audio == null) { throw new Exception("Audio service was not provided"); };
            audio.SkipSound();
        }
    }
}
