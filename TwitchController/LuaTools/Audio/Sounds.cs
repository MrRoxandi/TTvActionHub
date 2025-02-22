using Microsoft.VisualBasic.FileIO;
using NAudio.Vorbis;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using TwitchController.Services;

namespace TwitchController.LuaTools.Audio
{
    public static class Sounds
    {
        private static IWaveProvider GetAudioReader(Stream stream, string fileExtension)
        {
            return fileExtension switch
            {
                ".mp3" => new Mp3FileReader(stream),
                ".wav" => new WaveFileReader(stream),
                ".ogg" => new VorbisWaveReader(stream),
                _ => throw new Exception("Format of this audio file is not supporting")
            };
        }
        public static async Task PlaySoundFromDiscAsync(string path)
        {
            if(!File.Exists(path))
            {
                throw new Exception($"File {path} not existing");
            }
            string fileExtension = Path.GetExtension(path).ToLowerInvariant();
            using (var audioStream = File.OpenRead(path))
            using (var waveOut = new WaveOutEvent())
            {
                var reader = GetAudioReader(audioStream, fileExtension);
                waveOut.Init(reader);
                waveOut.Play();

                // Ждем окончания воспроизведения
                while (waveOut.PlaybackState == PlaybackState.Playing)
                {
                    await Task.Delay(500);
                }
            }
        }

        public static async Task PlaySoundFromUrlAsync(string url)
        {
            string tempFilePath = Path.GetTempFileName();

            string? fileExtension;

            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                fileExtension = Path.GetExtension(url).ToLowerInvariant();

                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = File.Create(tempFilePath))
                {
                    await stream.CopyToAsync(fileStream);
                }
            }

            using (var audioStream = File.OpenRead(tempFilePath))
            using (var waveOut = new WaveOutEvent())
            {
                var reader = GetAudioReader(audioStream, fileExtension);
                waveOut.Init(reader);
                waveOut.Play();

                // Ждем окончания воспроизведения
                while (waveOut.PlaybackState == PlaybackState.Playing)
                {
                    await Task.Delay(500);
                }
            }

            File.Delete(tempFilePath);
        }
    }
}
