using System.Collections.Concurrent;
using TTvActionHub.Items;
using TTvActionHub.Twitch;

namespace TTvActionHub
{
    public interface IConfig
    {
        public string RefreshToken { get; }
        public string Token { get; }
        public string Login { get; }
        public string ID { get; }
        
        public TwitchApi TwitchApi { get; }

        public bool LogState { get; }
    }
}
