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
        private readonly string redirectUrl;
        private static readonly HttpClient httpClient = new();

        public TwitchApiService(string clientId, string clientSecret, string redirectUri)
        {
            API = new();
            API.Settings.ClientId = clientId;
            API.Settings.Secret = clientSecret;
            this.redirectUrl = redirectUri;
        }
        
        public void OpenAuthorizationUrl()
        {
            string url = $"https://id.twitch.tv/oauth2/authorize?client_id=" +
                $"{API.Settings.ClientId}&redirect_uri={Uri.EscapeDataString(redirectUrl)}" +
                $"&response_type=code&scope=" +
                $"{string.Join("+", [
                    "channel:read:redemptions", "channel:manage:redemptions",
                    "user:edit","chat:edit", "chat:read"])}";
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        public async Task<String> GetTwitchIdAsync(string channelName)
        {
            var result = await API.Helix.Users.GetUsersAsync(logins: [channelName]);
            return result.Users[0].Id;
        }

        public async Task<string?> RunAuthFlowAsync()
        {
            try
            {
                using var listener = new HttpListener();
                listener.Prefixes.Add(redirectUrl);
                listener.Start();
                OpenAuthorizationUrl();

                var context = await listener.GetContextAsync();
                var request = context.Request;
                var response = context.Response;

                var code = request.QueryString["code"];
                var error = request.QueryString["error"];

                if (string.IsNullOrEmpty(code))
                {
                    Console.WriteLine($"[ERROR] Authorization failed. Error: {error}");
                    return null;
                }

                return await GetAccessTokenAsync(code);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}");
                return null;
            }
        }

        public async Task<string?> GetAccessTokenAsync(string authorizationCode)
        {
            try
            {
                var content = new FormUrlEncodedContent(
                [
                    new KeyValuePair<string, string>("client_id", API.Settings.ClientId),
                    new KeyValuePair<string, string>("client_secret", API.Settings.Secret),
                    new KeyValuePair<string, string>("code", authorizationCode),
                    new KeyValuePair<string, string>("grant_type", "authorization_code"),
                    new KeyValuePair<string, string>("redirect_uri", redirectUrl)
                ]);

                var response = await httpClient.PostAsync("https://id.twitch.tv/oauth2/token", content);

                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[ERROR] Token request failed. Status: {response.StatusCode}");
                    return null;
                }

                var jsonDoc = JsonDocument.Parse(responseString);
                return jsonDoc.RootElement.GetProperty("access_token").GetString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Token retrieval failed: {ex.Message}");
                return null;
            }
        }
    }
}
