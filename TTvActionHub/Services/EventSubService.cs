using TTvActionHub.Items;
using TTvActionHub.Logs;
using TwitchLib.Api.Helix.Models.EventSub;
using TwitchLib.EventSub.Websockets;

namespace TTvActionHub.Services
{
    public class EventSubService(IConfig config) : IService
    {
        public event EventHandler<ServiceStatusEventArgs>? StatusChanged;
        public string ServiceName => "EventSubService"; 
        public bool IsRunning => !(_client == null) ;

        private volatile bool _stopRequested = false;

        private readonly IConfig _configuration = config;
        private EventSubWebsocketClient? _client;

        public void Run()
        {
            _stopRequested = false;
            if(_client == null)
            {
                _client = new();
                _client.WebsocketConnected += WebsocketConnectedHandler;
                _client.WebsocketDisconnected += WebsocketDisconnectedHandler;
                _client.ErrorOccurred += ErrorOccurredHandler;
                _client.ChannelPointsCustomRewardRedemptionAdd += ChannelPointsCustomRewardRedemptionAddHandler;
                Logger.Log(LOGTYPE.INFO, ServiceName, "Initializing and connecting...");
                try
                {
                    _client.ConnectAsync();
                }
                catch (Exception ex)
                {
                    Logger.Log(LOGTYPE.ERROR, ServiceName, "Failed to start.", ex);
                    OnStatusChanged(false, $"Startup failed: {ex.Message}");
                }
            } else 
            {
                Logger.Log(LOGTYPE.WARNING, ServiceName, "Look like already connected...");
                OnStatusChanged(true);
            }
            
        }

        public void Stop()
        {
            _stopRequested = true;
            Logger.Log(LOGTYPE.INFO, ServiceName, "Disconnecting...");
            if (_client != null) 
            {
                try
                {
                    _client.DisconnectAsync();
                    _client.WebsocketConnected -= WebsocketConnectedHandler;
                    _client.WebsocketDisconnected -= WebsocketDisconnectedHandler;
                    _client.ErrorOccurred -= ErrorOccurredHandler;
                    _client.ChannelPointsCustomRewardRedemptionAdd -= ChannelPointsCustomRewardRedemptionAddHandler;
                    _client = null;
                }
                catch (Exception ex)
                {
                    Logger.Log(LOGTYPE.ERROR, ServiceName, "Failed to stop.", ex);
                    OnStatusChanged(true, $"Shutdown failed: {ex.Message}");
                }
            } else
            {
                Logger.Log(LOGTYPE.WARNING, ServiceName, "Look like already disconnected...");
                OnStatusChanged(false);
            }
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

        private Task ErrorOccurredHandler(object sender, TwitchLib.EventSub.Websockets.Core.EventArgs.ErrorOccuredArgs args) =>
            Task.Run(() => Logger.Log(LOGTYPE.ERROR, ServiceName, "While working, occured an error", args.Exception));
            // Maybe should to restart, but for now will kip it like this

        private async Task WebsocketDisconnectedHandler(object sender, EventArgs args)
        {
            Logger.Log(LOGTYPE.INFO, ServiceName, $"Service has disconnected");
            OnStatusChanged(false, "Disconnected");
            await HandleReconnect();
        } 

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

        private Task HandleReconnect()
        {
            if (!_stopRequested)
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, "Attempting to reconnect in 5 seconds...");
                Task.Delay(5000).ContinueWith(_ =>
                {
                    if (!_stopRequested)
                    {
                        Logger.Log(LOGTYPE.INFO, ServiceName, "Reconnecting...");
                        try
                        {
                            _client?.ReconnectAsync();
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(LOGTYPE.ERROR, ServiceName, "Reconnect failed.", ex);
                            OnStatusChanged(false, $"Reconnect failed: {ex.Message}");
                        }
                    }
                }
                );
            }
            return Task.CompletedTask;
        }

        protected virtual void OnStatusChanged(bool isRunning, string? message = null)
        {
            try
            {
                StatusChanged?.Invoke(this, new ServiceStatusEventArgs(ServiceName, isRunning, message));
            }
            catch (Exception ex)
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, "Error invoking StatusChanged event handler.", ex);
            }

        }
    }
}
