using TwitchLib.PubSub;
using TTvActionHub.Items;
using TTvActionHub.Logs;

namespace TTvActionHub.Services
{
    public class RewardsService : IService
    {
        private readonly IConfig _configuration;
        private readonly TwitchPubSub _client;
        
        public RewardsService(IConfig configuration)
        {
            _configuration = configuration;
            _client = new TwitchPubSub();
            _client.OnPubSubServiceConnected += (sender, args) =>
            {
                _client.SendTopics(_configuration.TwitchInfo.Token);
                Logger.Log(LOGTYPE.INFO,  ServiceName, $"Service has connected to channel {_configuration.TwitchInfo.Login}.");
            };

            _client.OnListenResponse += (sender, args) =>
            {
                if (!args.Successful)
                {
                    Logger.Log(LOGTYPE.ERROR,  ServiceName, $"Failed to listen!, {args.Response.Error}");
                }
                else if (_configuration.LogState)
                {
                    Logger.Log(LOGTYPE.INFO,  ServiceName, $"{args.Topic}");
                }
            };

            _client.OnChannelPointsRewardRedeemed += (sender, args) =>
            {
                var rewardTitle = args.RewardRedeemed.Redemption.Reward.Title;
                var rewardResiever = args.RewardRedeemed.Redemption.User.DisplayName;
                var rewardArgsStr = args.RewardRedeemed.Redemption.UserInput;


                if (!_configuration.Rewards.TryGetValue(rewardTitle, out Reward? value)) return;

                var (obr, cbr) = _configuration.Brackets;
                if (!string.IsNullOrEmpty(obr) && !string.IsNullOrEmpty(cbr))
                {
                    var start = rewardArgsStr.IndexOf(obr, StringComparison.Ordinal);
                    var stop = rewardArgsStr.IndexOf(cbr, StringComparison.Ordinal);
                    if (start == -1 || stop == -1)
                        rewardArgsStr = "";
                    else
                        rewardArgsStr = rewardArgsStr.Substring(start + 1, stop - start - 1);
                }
                rewardArgsStr = rewardArgsStr.Replace("\U000e0000", "").Trim();
                Logger.Log(LOGTYPE.ERROR,  ServiceName, $"Received reward: {rewardTitle} from {rewardResiever} with args: {rewardArgsStr}");

                string[]? rewardArgs = string.IsNullOrEmpty(rewardArgsStr) ? null : rewardArgsStr.Split(' ');
                value.Execute(rewardResiever, rewardArgs);
            };
        }

        public void Run()
        {
            _client.ListenToChannelPoints(_configuration.TwitchInfo.ID);
            _client.Connect();
        }

        public void Stop() {
            _client.Disconnect();
        }

        public string ServiceName { get => "RewardService"; }
    }
}
