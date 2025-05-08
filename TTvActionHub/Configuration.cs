using TTvActionHub.Logs;
using TTvActionHub.Twitch;
using TwitchLib.Api.Core.Exceptions;
using TTvActionHub.Managers;

namespace TTvActionHub
{
    public class Configuration : IConfig
    {
        private static string ClientId { get => "--"; }
        private static string ClientSecret { get => "--"; }
        private static string RedirectUrl { get => @"http://localhost:6969/"; } // 6969 just for fun

        public string ID => _ttvInfo.ID;
        public string Login => _ttvInfo.Login;
        public string Token => _ttvInfo.Token;
        public string RefreshToken => _ttvInfo.RefreshToken;

        private readonly bool _forceRelog;
        public bool LogState { get; private set; }
        public TwitchApi TwitchApi { get; private set; } 

        private (string Login, string ID, string Token, string RefreshToken) _ttvInfo;
        
        public Configuration(LuaConfigManager manager)
        {
            TwitchApi = new TwitchApi(ClientId, ClientSecret, RedirectUrl);
            LogState = manager.MoreLogs;
            _forceRelog = manager.ForceRelog;
            _ttvInfo = AuthWithTwitch();
        }

        private (string Login, string ID, string Token, string RefreshToken) GetAuthInfoFromAPI()
        {
            (string Login, string ID, string Token, string RefreshToken) authInfo = new();
            var authTask = TwitchApi.GetAuthorizationInfo();
            authTask.Wait();
            if (authTask.IsCompleted)
            {
                var (Token, RefreshToken) = authTask.Result;
                if (string.IsNullOrEmpty(Token) || string.IsNullOrEmpty(RefreshToken))
                    throw new BadRequestException("Unable to get Authorization information");
                authInfo.Token = Token;
                authInfo.RefreshToken = RefreshToken;
            }
            var channelInfoTask = TwitchApi.GetChannelInfoAsync(authInfo.Token!);
            channelInfoTask.Wait();
            if (!channelInfoTask.IsCompleted) return authInfo;
            var (Login, ID) = channelInfoTask.Result;
            if (string.IsNullOrEmpty(Login) || string.IsNullOrEmpty(ID))
                throw new BadRequestException("Unable to get channel information");
            authInfo.Login = Login;
            authInfo.ID = ID;

            return authInfo;
        }

        private (string Login, string ID, string Token, string RefreshToken) AuthWithTwitch()
        {
            AuthManager manager = new(TwitchApi, ClientSecret);
            if (_forceRelog || !manager.LoadTwitchInfo())
            {
                var result = GetAuthInfoFromAPI();
                manager.TwitchInfo = result;
                manager.SaveTwitchInfo();
                return result;
            }
            var validationTask = manager.IsValidTokensAsync();
            validationTask.Wait();
            if (validationTask.Result) return manager.TwitchInfo;
            try
            {
                var refreshTask = manager.UpdateAuthInfoAsync();
                refreshTask.Wait();
                if (!refreshTask.Result)
                {
                    manager.TwitchInfo = GetAuthInfoFromAPI();
                }
            } catch (Exception ex)
            {
                Logger.Error($"Unable to update Twitch Info with manager due to error", ex);
                Logger.Info($"Getting new Twitch Info from browser");
                manager.TwitchInfo = GetAuthInfoFromAPI();
            }
            finally
            {
                manager.SaveTwitchInfo();
            }
            return manager.TwitchInfo;
        }
    
    }
}
