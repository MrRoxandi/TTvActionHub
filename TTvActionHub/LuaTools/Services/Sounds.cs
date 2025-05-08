using TTvActionHub.Services;

namespace TTvActionHub.LuaTools.Services
{
    public static class Sounds
    {
        public static AudioService? audio;
        
        public static async Task PlaySound(string uri)
        {
            if (string.IsNullOrEmpty(uri)) throw new ArgumentNullException(nameof(uri));
            if (audio == null) throw new FieldAccessException("Audio service was not provided");
            await audio.PlaySoundAsync(new Uri(uri));
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
