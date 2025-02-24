using TwitchLib.PubSub;
using TwitchController.Items;
using TwitchController.Logs;

namespace TwitchController.Services
{
    internal class TwitchRewardService : IService
    {
        private readonly Configuration _configuration;
        private readonly TwitchPubSub Client;

        public TwitchRewardService(Configuration configuration)
        {
            _configuration = configuration;
            Client = new TwitchPubSub();
            Client.OnPubSubServiceConnected += (sender, args) =>
            {
                Client.SendTopics(_configuration.TwitchInfo.Token);
                Logger.External(LOGTYPE.INFO, ServiceName(), $"Rewards service has connected to channel {_configuration.TwitchInfo.Login}.");
            };

            Client.OnListenResponse += (sender, args) =>
            {
                if (!args.Successful)
                {
                    Logger.External(LOGTYPE.ERROR, ServiceName(), $"Failed to listen!, {args.Response.Error}");
                }
            };

            Client.OnChannelPointsRewardRedeemed += (sender, args) =>
            {
                var rewardTitle = args.RewardRedeemed.Redemption.Reward.Title;
                var rewardResiever = args.RewardRedeemed.Redemption.User.DisplayName;
                var rewardArgsStr = args.RewardRedeemed.Redemption.UserInput;


                if (!_configuration.Rewards.TryGetValue(rewardTitle, out Reward? value)) return;


                if (!string.IsNullOrEmpty(_configuration.OpeningBracket) && !string.IsNullOrEmpty(_configuration.ClosingBracket))
                {
                    var start = rewardArgsStr.IndexOf(_configuration.OpeningBracket, StringComparison.Ordinal);
                    var stop = rewardArgsStr.IndexOf(_configuration.ClosingBracket, StringComparison.Ordinal);
                    if (start == -1 || stop == -1)
                        rewardArgsStr = "";
                    else
                        rewardArgsStr = rewardArgsStr.Substring(start + 1, stop - start - 1);
                }
                rewardArgsStr = rewardArgsStr.Replace("\U000e0000", "").Trim();
                Logger.External(LOGTYPE.ERROR, ServiceName(), $"Received reward: {rewardTitle} from {rewardResiever} with args: {rewardArgsStr}");

                string[]? rewardArgs = string.IsNullOrEmpty(rewardArgsStr) ? null : rewardArgsStr.Split(' ');
                value.Execute(rewardResiever, rewardArgs);
            };
        }

        public void Run()
        {
            Client.ListenToChannelPoints(_configuration.TwitchInfo.ID);
            Client.Connect();
        }

        public void Stop() {
            Client.Disconnect();
        }

        public string ServiceName() => "RewardService";
    }
}
