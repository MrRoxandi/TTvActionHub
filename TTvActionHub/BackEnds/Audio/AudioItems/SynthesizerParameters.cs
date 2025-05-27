using System.Speech.Synthesis;

namespace TTvActionHub.BackEnds.Audio.AudioItems;

public struct SynthesizerParameters
{
    public int Rate { get; set; }
    public int Volume { get; set; }
    public VoiceAge Age { get; set; }
    public VoiceGender Gender { get; set; }
}