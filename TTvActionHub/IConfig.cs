using System.Collections.Concurrent;
using TTvActionHub.Items;
using TTvActionHub.Twitch;

namespace TTvActionHub
{
    public interface IConfig
    {
        public ConcurrentDictionary<string, Command> Commands { get; }
        public ConcurrentDictionary<string, TwitchReward> Rewards { get; }
        public ConcurrentDictionary<string, TimerAction> TActions { get; }
        
        public bool LogState { get; }
        public (string Login, string ID, string Token, string RefreshToken) TwitchInfo { get; }

        public (string obr, string cbr) Brackets { get; }
        public TwitchApi TwitchApi { get; }

    }
}
