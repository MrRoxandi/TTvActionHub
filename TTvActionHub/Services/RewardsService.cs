using TTvActionHub.Items;
using TTvActionHub.Logs;
using TwitchLib.Api.Helix;
using TwitchLib.Api.Helix.Models.EventSub;
using TwitchLib.EventSub.Websockets;

namespace TTvActionHub.Services
{
    public class RewardsService : IService
    {
        private readonly IConfig _configuration;
        //private readonly TwitchPubSub _client;
        private readonly EventSubWebsocketClient _client;

        public RewardsService(IConfig configuration)
        {
            _configuration = configuration;
            _client = new();
            _client.WebsocketConnected += WebsocketConnectedHandler;
            _client.WebsocketDisconnected += WebsocketDisconnectedHandler;
            _client.ErrorOccurred += ErrorOccurredHandler;
            _client.ChannelPointsCustomRewardRedemptionAdd += ChannelPointsCustomRewardRedemptionAddHandler;
            
        }

        private Task ChannelPointsCustomRewardRedemptionUpdate(object sender, TwitchLib.EventSub.Websockets.Core.EventArgs.Channel.ChannelPointsCustomRewardRedemptionArgs args)
        {
            throw new NotImplementedException();
        }

        private Task ChannelPointsCustomRewardRedemptionAddHandler(object sender, TwitchLib.EventSub.Websockets.Core.EventArgs.Channel.ChannelPointsCustomRewardRedemptionArgs args)
            => Task.Run(() =>
            {
                var _event = args.Notification.Payload.Event;
                var rewardTitle = _event.Reward.Title;
                var rewardResiever = _event.UserName;
                var rewardArgsStr = _event.UserInput;

                if (!_configuration.Rewards.TryGetValue(rewardTitle, out Reward? value))
                    return;

                var (obr, cbr) = _configuration.Brackets;
                if (!string.IsNullOrEmpty(rewardArgsStr) && !string.IsNullOrEmpty(obr) && !string.IsNullOrEmpty(cbr))
                {
                    var start = rewardArgsStr.IndexOf(obr, StringComparison.Ordinal);
                    var stop = rewardArgsStr.IndexOf(cbr, StringComparison.Ordinal);
                    if (start == -1 || stop == -1)
                        rewardArgsStr = "";
                    else
                        rewardArgsStr = rewardArgsStr.Substring(start + 1, stop - start - 1);
                }
                rewardArgsStr = rewardArgsStr.Replace("\U000e0000", "").Trim();
                Logger.Log(LOGTYPE.ERROR, ServiceName, $"Received reward: {rewardTitle} from {rewardResiever} with args: {rewardArgsStr}");

                string[]? rewardArgs = string.IsNullOrEmpty(rewardArgsStr) ? null : rewardArgsStr.Split(' ');
                value.Execute(rewardResiever, rewardArgs);
            });

        private Task ErrorOccurredHandler(object sender, TwitchLib.EventSub.Websockets.Core.EventArgs.ErrorOccuredArgs args)
        {
            throw new NotImplementedException();
        }

        private async Task WebsocketDisconnectedHandler(object sender, EventArgs args) => await Task.Run(() => Logger.Log(LOGTYPE.INFO, ServiceName, "Disconected from EventSub"));

        private async Task WebsocketConnectedHandler(object sender, TwitchLib.EventSub.Websockets.Core.EventArgs.WebsocketConnectedArgs args) 
        {
            await Task.Run(() => Logger.Log(LOGTYPE.INFO, ServiceName, "connected to EventSub"));
            if (!args.IsRequestedReconnect)
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, "Subscribing to topics...");
                await SubscribeTopics();
            } else
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, "Reconnected. Subscriptions should persist.");
            }
        }

        private async Task SubscribeTopics()
        {
            var id = _configuration.TwitchInfo.ID;
            var API = _configuration.TwitchApi.InnerAPI;

            try
            {
                var result = await CreateEventSubSubscription("channel.channel_points_custom_reward_redemption.add", "1");
                foreach (var item in result.Subscriptions)
                {
                    Logger.Log(LOGTYPE.INFO, ServiceName, $"Subscribed to {item.Type} event with status {item.Status}");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LOGTYPE.ERROR, "Unable to subscribe to events: ", ex.Message);
            }
            
        }

        private async Task<CreateEventSubSubscriptionResponse> CreateEventSubSubscription(string type, string version) =>
            await _configuration.TwitchApi.InnerAPI.Helix.EventSub.CreateEventSubSubscriptionAsync(
                    type: type,
                    version: version,
                    condition: new()
                    {
                        {"broadcaster_user_id", _configuration.TwitchInfo.ID }
                    },
                    method: TwitchLib.Api.Core.Enums.EventSubTransportMethod.Websocket,
                    websocketSessionId: _client.SessionId
                );

        public void Run()
        {
            _client.ConnectAsync();
        }

        public void Stop() {
            _client.DisconnectAsync();
        }

        public string ServiceName { get => "RewardService"; }
    }
}
