using Lua;
using TTvActionHub.BackEnds.Abstractions;

namespace TTvActionHub.LuaWrappers.Services;

[LuaObject]
public partial class LuaAudio
{
    [LuaMember]
    public static void PlaySound(string uri) => _ = Audio.PlaySound(uri);
    
    [LuaMember]
    public static void PlayText(string text) => _ = Audio.PlayText(text);
    
    [LuaMember]
    public static void SkipSound() => Audio.SkipSound();
    
    [LuaMember]
    public static void SetVolume(int volume) => Audio.SetVolume(volume);
    
    [LuaMember]
    public static int GetVolume() => Audio.GetVolume();
    
    [LuaMember]
    public static void IncreaseVolume(int volume) => Audio.IncreaseVolume(volume);
    
    [LuaMember]
    public static void DecreaseVolume(int volume) => Audio.DecreaseVolume(volume);
}