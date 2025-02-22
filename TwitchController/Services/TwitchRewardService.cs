using TwitchLib.PubSub;
using TwitchController.Items;

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
                Console.WriteLine($"[INFO] Rewards service has connected to channel {_configuration.TwitchInfo.Login}.");
            };

            Client.OnListenResponse += (sender, args) =>
            {
                if (!args.Successful)
                {
                    Console.WriteLine($"Failed to listen! Response: {args.Response.Error}");
                }
            };

            Client.OnChannelPointsRewardRedeemed += (sender, args) =>
            {
                var rewardTitle = args.RewardRedeemed.Redemption.Reward.Title;
                var rewardResiever = args.RewardRedeemed.Redemption.User.DisplayName;
                var rewardArgs = args.RewardRedeemed.Redemption.UserInput;

                Console.WriteLine($"Received reward: {rewardTitle} from {rewardResiever} with args: {rewardArgs}");

                if (!_configuration.Rewards.TryGetValue(rewardTitle, out Reward? value)) return;

                if (_configuration.OpeningBracket is not null && _configuration.ClosingBracket is not null)
                {
                    var start = rewardArgs.IndexOf(_configuration.OpeningBracket, StringComparison.Ordinal);
                    var stop = rewardArgs.IndexOf(_configuration.ClosingBracket, StringComparison.Ordinal);
                    if (start == -1 || stop == -1)
                        rewardArgs = "";
                    else
                        rewardArgs = rewardArgs.Substring(start + 1, stop - start - 1);
                }

                value.Execute(rewardResiever, rewardArgs.Replace("\U000e0000", "").Trim().Split(' '));
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
    }
}
