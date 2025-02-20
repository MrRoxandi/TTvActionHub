using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.PubSub;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchController.Items;
using TwitchController.Stuff;

namespace TwitchController.Twitch
{
    public class TwitchRewardService
    {
        private readonly Configuration _configuration;
        private readonly TwitchPubSub Client;

        public TwitchRewardService(Configuration configuration)
        {
            _configuration = configuration;
            Client = new TwitchPubSub();
            Client.OnPubSubServiceConnected += (sender, args) =>
            {
                Client.SendTopics(_configuration.AuthorizationInfo.Token);
                Console.WriteLine($"[INFO] Rewards service has connected to channel {_configuration.AuthorizationInfo.TwitchChannel}.");
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

                Console.WriteLine($"Received command: {rewardTitle} from {rewardResiever} with args: {rewardArgs}");
                
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

                var executionResult = value.Execute(rewardResiever, rewardArgs.Split(' '));
                if(executionResult == null)
                {
                    Chat.SendMessage($"@{_configuration.AuthorizationInfo.TwitchChannel} for some reason reward {rewardTitle} was not executed");
                }
            };
        }

        public void Run()
        {
            Client.ListenToChannelPoints(_configuration.AuthorizationInfo.TwitchID);
            Client.Connect();
        }
    }
}
