using System.Diagnostics;
using System.Net;
using TwitchLib.Api;
using TTvActionHub.Logs;
using TwitchLib.Api.Core.Enums;

namespace TTvActionHub.Twitch
{
    public class TwitchApi(string clientId, string clientSecret, string redirectUri)
    {
        public TwitchAPI InnerApi { get; } = new()
        {
            Settings =
            {
                ClientId = clientId,
                Secret = clientSecret
            }
        };

        /*private static readonly HttpClient HttpClient = new();*/

        public async Task<(string? Token, string? RefreshToken)> GetAuthorizationInfo()
        {
            Logger.Log(LOGTYPE.INFO, "TwitchApi", "Requesting new Twitch authentication token.");
            var authInfo = await RequestAuthorizationInfo();

            return authInfo;
        }

        private async Task<(string? Token, string? RefreshToken)> RequestAuthorizationInfo()
        {
            using HttpListener listener = new();
            listener.Prefixes.Add(redirectUri);
            listener.Start();

            Process.Start(new ProcessStartInfo(TokenUri) { UseShellExecute = true });

            var context = await listener.GetContextAsync();
            var request = context.Request;
            
            var code = request.QueryString["code"];
            var error = request.QueryString["error"];

            if (!string.IsNullOrEmpty(error))
            {
                throw new Exception($"Twitch authorization error: {error}");
            }

            if (string.IsNullOrEmpty(code))
            {
                throw new Exception("Twitch authorization code is missing.");
            }

            var (accessToken, refreshToken) = await GetAccessTokenAsync(code);

            if (accessToken == null || refreshToken == null) return (null, null);

            return (accessToken, refreshToken);
        }

        private string TokenUri => InnerApi.Auth.GetAuthorizationCodeUrl(redirectUri, 
            [AuthScopes.Helix_Channel_Read_Redemptions,
            AuthScopes.Helix_Channel_Manage_Redemptions,
            AuthScopes.Chat_Edit, AuthScopes.Chat_Read,
            AuthScopes.Helix_User_Edit, AuthScopes.Helix_Moderator_Read_Chatters], clientId: clientId);

        private static string ServiceName => "TwitchAPI";

        public async Task<(string? Login, string? ID)> GetChannelInfoAsync(string token)
        {
            InnerApi.Settings.AccessToken = token;

            try
            {
                var usersResponse = await InnerApi.Helix.Users.GetUsersAsync();
                var user = usersResponse.Users.FirstOrDefault();
                if (user != null)
                {
                    return (user.Login, user.Id);
                }
                Logger.Log(LOGTYPE.WARNING, ServiceName, "Could not retrieve user information from Twitch API.");
                return (null, null);
            }
            catch (Exception ex)
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, "Error getting channel info from Twitch API.", ex);
                return (null, null);
            }
        }

        private async Task<(string? AccessToken, string? RefreshToken)> GetAccessTokenAsync(string authorizationCode)
        {
            try
            {
                var result = await InnerApi.Auth.GetAccessTokenFromCodeAsync(authorizationCode, clientSecret, redirectUri, clientId);
                return (result.AccessToken, result.RefreshToken);
            }
            catch (Exception ex)
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, "Unable to get access token due to error: ", ex);
                return (null, null);
            }
        }

        public async Task<(string? AccessToken, string? RefreshToken)> RefreshAccessTokenAsync(string refreshToken)
        {
            try
            {
                var result = await InnerApi.Auth.RefreshAuthTokenAsync(refreshToken, clientSecret, clientId);
                return (result.AccessToken, result.RefreshToken);
            }
            catch (Exception ex)
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, "Unable to refresh token due to error: ", ex);
                return (null, null);
            }
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            InnerApi.Settings.AccessToken = token;
            try
            {
                var usersResponse = await InnerApi.Helix.Users.GetUsersAsync();
                return usersResponse.Users.Length != 0; // If we get a user, token is valid
            }
            catch (Exception ex)
            {
                Logger.Log(LOGTYPE.WARNING, "TwitchApi", "Token validation failed.", ex);
                return false;
            }
        }
    }
}