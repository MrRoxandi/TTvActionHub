using TTvActionHub.Items;

namespace TTvActionHub
{
    public interface IConfig
    {
        public Dictionary<string, Command> Commands { get; }
        public Dictionary<string, Reward> Rewards { get; }
        public List<TActions> TActions { get; }
        
        public bool LogState { get; }
        public (string Login, string ID, string Token, string RefreshToken) TwitchInfo { get; }

        public (string obr, string cbr) Brackets { get; }

        //static abstract void CreateConfig(string path);

    }
}
