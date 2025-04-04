using TTvActionHub.Items;
using TTvActionHub.Logs;
using TwitchLib.Api.Helix.Models.EventSub;
using TwitchLib.EventSub.Websockets;

namespace TTvActionHub.Services
{
    public class EventSubService : IService
    {
        private readonly IConfig _configuration;
        private readonly EventSubWebsocketClient _client;

        public event EventHandler<ServiceStatusEventArgs> StatusChanged;

        public EventSubService(IConfig configuration)
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

                if (!_configuration.Rewards.TryGetValue(rewardTitle, out TwitchReward? value))
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
                Logger.Log(LOGTYPE.INFO, ServiceName, $"Received reward: {rewardTitle} from {rewardResiever} with args: {rewardArgsStr}");

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
            await Task.Run(() => Logger.Log(LOGTYPE.INFO, ServiceName, "Connected to Twitch EventSub"));
            if (!args.IsRequestedReconnect)
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, "Subscribing to topics...");
                await RegisterEvents();
            } else
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, "Reconnected. Subscriptions should persist.");
            }
        }

        private async Task RegisterEvents()
        {
            // Subbing to rewards.
            var result = await SubscribeToEvent("channel.channel_points_custom_reward_redemption.add", "1");
            if (result is not null)
            {
                foreach (var sub in result.Subscriptions)
                {
                    Logger.Log(LOGTYPE.INFO, ServiceName, $"Subscription to [{sub.Type}:{sub.Version}] has status: {sub.Status}");
                }
            }
            else
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, "For some reason all subscribtion types failed. Report it if u see this");
            }
        }
        
        private async Task<CreateEventSubSubscriptionResponse?> SubscribeToEvent(string type, string version)
        {
            try
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, $"Trying to subscribe to [{type}:{version}]");
                var result = await _configuration.TwitchApi.InnerAPI.Helix.EventSub.CreateEventSubSubscriptionAsync(
                    type: type,
                    version: version,
                    condition: new()
                    {
                        {"broadcaster_user_id", _configuration.TwitchInfo.ID }
                    },
                    method: TwitchLib.Api.Core.Enums.EventSubTransportMethod.Websocket,
                    websocketSessionId: _client.SessionId
                    );
                return result;
            }
            catch (Exception ex)
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, $"Unable to subscribe to [{type}:{version}] due to error:", ex);
                return null;
            }
        }

        public void Run()
        {
            _client.ConnectAsync();
        }

        public void Stop() {
            _client.DisconnectAsync();
        }

        public string ServiceName { get => "EventSubService"; }

        public bool IsRunning => throw new NotImplementedException();
    }
}
