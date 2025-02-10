using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TwitchController.Twitch
{
    public class TwitchApiService
    {
        private readonly string clientId;
        private readonly string clientSecret;
        private readonly string redirectUri;
        private static readonly HttpClient httpClient = new HttpClient();

        public TwitchApiService(string clientId, string clientSecret, string redirectUri)
        {
            this.clientId = clientId;
            this.clientSecret = clientSecret;
            this.redirectUri = redirectUri;
        }

        public void OpenAuthorizationUrl()
        {
            string url = $"https://id.twitch.tv/oauth2/authorize?client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&response_type=code&scope=chat:read+chat:edit";
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        public async Task<string?> StartAuthFlowAsync()
        {
            try
            {
                using var listener = new HttpListener();
                listener.Prefixes.Add(redirectUri);
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
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("client_secret", clientSecret),
                    new KeyValuePair<string, string>("code", authorizationCode),
                    new KeyValuePair<string, string>("grant_type", "authorization_code"),
                    new KeyValuePair<string, string>("redirect_uri", redirectUri)
                });

                var response = await httpClient.PostAsync("https://id.twitch.tv/oauth2/token", content);

                var responseString = await response.Content.ReadAsStringAsync();
                //Console.WriteLine($"[DEBUG] Full response: {responseString}");

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
