using System.Collections.Concurrent;
using TTvActionHub.Items;
using TTvActionHub.Twitch;

namespace TTvActionHub
{
    public interface IConfig
    {
        public ConcurrentDictionary<string, Command> Commands { get; }
        public ConcurrentDictionary<string, Reward> Rewards { get; }
        public List<TActions> TActions { get; }
        
        public bool LogState { get; }
        public (string Login, string ID, string Token, string RefreshToken) TwitchInfo { get; }

        public (string obr, string cbr) Brackets { get; }
        public TwitchApi TwitchApi { get; }
        //static abstract void CreateConfig(string path);

    }
}
