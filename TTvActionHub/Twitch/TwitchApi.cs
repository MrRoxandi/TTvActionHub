using System.Diagnostics;
using System.Net;
using System.Text.Json;
using TwitchLib.Api;
using TTvActionHub.Logs; // Assuming this is your custom logging
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace TTvActionHub.Twitch
{
    public class TwitchApi
    {
        private readonly TwitchAPI _api;
        private readonly string _redirectUrl;
        private readonly string _clientId;
        private readonly string _clientSecret;

        private static readonly HttpClient _httpClient = new();

        public TwitchApi(string clientId, string clientSecret, string redirectUri)
        {
            _clientId = clientId;
            _clientSecret = clientSecret;
            _redirectUrl = redirectUri;

            _api = new TwitchAPI();
            _api.Settings.ClientId = clientId;
            _api.Settings.Secret = clientSecret;
        }

        public async Task<(string? Login, string? ID, string? Token, string? RefreshToken)> GetAuthorizationInfo()
        {
            Logger.Log(LOGTYPE.INFO, "TwitchApi", "Requesting new Twitch authentication token.");
            var authInfo = await RequestAuthorizationInfo();

            return authInfo;
        }

        private async Task<(string? Login, string? ID, string? Token, string? RefreshToken)> RequestAuthorizationInfo()
        {
            using HttpListener listener = new();
            listener.Prefixes.Add(_redirectUrl);
            listener.Start();

            Process.Start(new ProcessStartInfo(TokenURL) { UseShellExecute = true });

            var context = await listener.GetContextAsync();
            var request = context.Request;
            var response = context.Response;

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

            if (accessToken == null || refreshToken == null) return (null, null, null, null);

            var (login, id) = await GetChannelInfoAsync(accessToken);

            if (login == null || id == null) return (null, null, null, null);

            return (login, id, accessToken, refreshToken);
        }


        private string TokenURL => $"https://id.twitch.tv/oauth2/authorize?client_id=" +
                                   $"{_clientId}&redirect_uri={Uri.EscapeDataString(_redirectUrl)}" +
                                   $"&response_type=code&scope=" +
                                   $"{string.Join("+", [
                                        "channel:read:redemptions",
                                        "channel:manage:redemptions",
                                        "user:edit",
                                        "chat:edit",
                                        "chat:read"
                                   ])}";

        private async Task<(string? Login, string? ID)> GetChannelInfoAsync(string token)
        {
            _api.Settings.AccessToken = token;

            try
            {
                var usersResponse = await _api.Helix.Users.GetUsersAsync();
                var user = usersResponse.Users.FirstOrDefault();
                if (user != null)
                {
                    return (user.Login, user.Id);
                }
                Logger.Log(LOGTYPE.WARNING, "TwitchApi", "Could not retrieve user information from Twitch API.");
                return (null, null);
            }
            catch (Exception ex)
            {
                Logger.Log(LOGTYPE.ERROR, "TwitchApi", "Error getting channel info from Twitch API.", ex.Message);
                return (null, null);
            }
        }

        private async Task<string?> GetLoginAsync(string token)
        {
            _api.Settings.AccessToken = token;

            try
            {
                var usersResponse = await _api.Helix.Users.GetUsersAsync();
                var user = usersResponse.Users.FirstOrDefault();
                if (user != null)
                {
                    return user.Login;
                }
                Logger.Log(LOGTYPE.WARNING, "TwitchApi", "Could not retrieve user login from Twitch API.");
                return null;
            }
            catch (Exception ex)
            {
                Logger.Log(LOGTYPE.ERROR, "TwitchApi", "Error getting channel login from Twitch API.", ex.Message);
                return null;
            }
        }

        private async Task<string?> GetIdAsync(string token)
        {
            _api.Settings.AccessToken = token;

            try
            {
                var usersResponse = await _api.Helix.Users.GetUsersAsync();
                var user = usersResponse.Users.FirstOrDefault();
                if (user != null)
                {
                    return user.Id;
                }
                Logger.Log(LOGTYPE.WARNING, "TwitchApi", "Could not retrieve user Id from Twitch API.");
                return null;
            }
            catch (Exception ex)
            {
                Logger.Log(LOGTYPE.ERROR, "TwitchApi", "Error getting channel Id from Twitch API.", ex.Message);
                return null;
            }
        }

        private async Task<(string? AccessToken, string? RefreshToken)> GetAccessTokenAsync(string authorizationCode)
        {
            var content = new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("client_secret", _clientSecret),
                new KeyValuePair<string, string>("code", authorizationCode),
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("redirect_uri", _redirectUrl)
            ]);

            try
            {
                var response = await _httpClient.PostAsync("https://id.twitch.tv/oauth2/token", content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Logger.Log(LOGTYPE.ERROR, "TwitchApi", $"Token request failed. Status: {response.StatusCode}, Response: {responseString}");
                    return (null, null);
                }

                var jsonDoc = JsonDocument.Parse(responseString);
                string? accessToken = jsonDoc.RootElement.GetProperty("access_token").GetString();
                string? refreshToken = jsonDoc.RootElement.GetProperty("refresh_token").GetString();

                return (accessToken, refreshToken);
            }
            catch (Exception ex)
            {
                Logger.Log(LOGTYPE.ERROR, "TwitchApi", "Error getting access token from Twitch API.", ex.Message);
                return (null, null);
            }
        }

        public async Task<(string? AccessToken, string? RefreshToken)> RefreshAccessTokenAsync(string refreshToken)
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("client_secret", _clientSecret),
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("refresh_token", refreshToken),
                new KeyValuePair<string, string>("redirect_uri", _redirectUrl)
            });

            try
            {
                var response = await _httpClient.PostAsync("https://id.twitch.tv/oauth2/token", content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Logger.Log(LOGTYPE.ERROR, "TwitchApi", $"Refresh token request failed. Status: {response.StatusCode}, Response: {responseString}");
                    return (null, null);
                }

                var jsonDoc = JsonDocument.Parse(responseString);
                string? newAccessToken = jsonDoc.RootElement.GetProperty("access_token").GetString();
                string? newRefreshToken = jsonDoc.RootElement.GetProperty("refresh_token").GetString();  // Twitch *might* send a new refresh token

                return (newAccessToken, newRefreshToken);
            }
            catch (Exception ex)
            {
                Logger.Log(LOGTYPE.ERROR, "TwitchApi", "Error refreshing access token from Twitch API.", ex.Message);
                return (null, null);
            }
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            _api.Settings.AccessToken = token;
            try
            {
                // Call an endpoint that requires authentication
                var usersResponse = await _api.Helix.Users.GetUsersAsync();
                return usersResponse.Users.Any(); // If we get a user, token is valid

            }
            catch (Exception ex)
            {
                Logger.Log(LOGTYPE.WARNING, "TwitchApi", "Token validation failed. Token may be expired.", ex.Message);
                return false; // Token invalid
            }
        }
    }
}