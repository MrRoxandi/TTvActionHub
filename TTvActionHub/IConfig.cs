using TTvActionHub.Twitch;

namespace TTvActionHub
{
    public interface IConfig
    {
        public string RefreshToken { get; }
        public string Token { get; }
        public string Login { get; }
        public string Id { get; }
        
        public TwitchApi TwitchApi { get; }

        public bool LogState { get; }
    }
}
