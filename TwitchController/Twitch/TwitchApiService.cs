using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Api.Core;
using TwitchLib.Api.Core.Enums;
using TwitchLib.PubSub.Models.Responses.Messages.Redemption;

namespace TwitchController.Twitch
{
    public class TwitchApiService
    {
        private readonly TwitchAPI API;
        private readonly string RedirectUrl;
        private static readonly HttpClient httpClient = new();

        public TwitchApiService(string clientId, string clientSecret, string redirectUri)
        {
            API = new();
            API.Settings.ClientId = clientId;
            API.Settings.Secret = clientSecret;
            RedirectUrl = redirectUri;
        }

        public async Task<(string? Login, string? ID, string? Token)> GetAuthorizationInfo()
        {
            using HttpListener listener = new();
            listener.Prefixes.Add(RedirectUrl);
            listener.Start();

            Process.Start(new ProcessStartInfo(TokenURL) { UseShellExecute = true });

            var context = await listener.GetContextAsync();
            var request = context.Request;
            var response = context.Response;

            var code = request.QueryString["code"];
            var error = request.QueryString["error"];

            if (string.IsNullOrEmpty(code))
            {
                throw new Exception(error);
            }

            var token = await GetAccessTokenAsync(code);

            if (token == null) return (null, null, null);

            var (login, id) = await GetChannelInfoAsync(token);

            if (login == null || id == null) return (null, null, null);

            return (login, id, token);
        }

        private string TokenURL => $"https://id.twitch.tv/oauth2/authorize?client_id=" +
                $"{API.Settings.ClientId}&redirect_uri={Uri.EscapeDataString(RedirectUrl)}" +
                $"&response_type=code&scope=" +
                $"{string.Join("+", [
                    "channel:read:redemptions", "channel:manage:redemptions",
                    "user:edit","chat:edit", "chat:read"])}";

        private async Task<(string? Login, string? ID)> GetChannelInfoAsync(string token)
        {
            API.Settings.AccessToken = token;

            var usersResponse = await API.Helix.Users.GetUsersAsync();
            var user = usersResponse.Users.FirstOrDefault();
            if(user != null) 
                return (user.Login, user.Id);
            return (null, null);

        }

        private async Task<string?> GetAccessTokenAsync(string authorizationCode)
        {
            var content = new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("client_id", API.Settings.ClientId),
                new KeyValuePair<string, string>("client_secret", API.Settings.Secret),
                new KeyValuePair<string, string>("code", authorizationCode),
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("redirect_uri", RedirectUrl)
            ]);

            var response = await httpClient.PostAsync("https://id.twitch.tv/oauth2/token", content);

            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Token request failed. Status: {response.StatusCode}");
            }

            var jsonDoc = JsonDocument.Parse(responseString);
            return jsonDoc.RootElement.GetProperty("access_token").GetString();
        }
    }
}
