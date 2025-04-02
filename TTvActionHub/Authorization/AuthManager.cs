using Microsoft.VisualBasic.Logging;
using System.Security.Cryptography;
using System.Text;
using TTvActionHub.Logs;
using TTvActionHub.Twitch;

namespace TTvActionHub.Authorization
{
    public class AuthManager(TwitchApi api, string secret)
    {
        public (string Login, string ID, string Token, string RefreshToken) TwitchInfo;
        public static string ServiceName => "AuthManager";

        private static string AuthDir => "auth";
        private static string FileName => "data";

        private readonly TwitchApi _api = api;
        private readonly string _secret = secret;

        public bool SaveTwitchInfo()
        {
            Directory.CreateDirectory(AuthDir);
            string path = Path.Combine(AuthDir, FileName);
            try
            {
                var (login, id, token, rtoken) = TwitchInfo;
                string data = string.Join("\n", [login, id, token, rtoken]);
                File.WriteAllText(path, Encrypt(data, _secret));
                Logger.Log(LOGTYPE.INFO, ServiceName, $"Authorization information saved at {path}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, "Unable to save authorization information due to error:", ex);
                return false;
            }
        }

        public bool LoadTwitchInfo()
        {
            try
            {
                string path = Path.Combine(AuthDir, FileName);
                if (!File.Exists(path)) return false;
                var data = Decrypt(File.ReadAllText(path), _secret).Split("\n");
                if (data.Length != 4) return false;
                else
                {
                    TwitchInfo.Login = data[0];
                    TwitchInfo.ID = data[1];
                    TwitchInfo.Token = data[2];
                    TwitchInfo.RefreshToken = data[3];
                    Logger.Log(LOGTYPE.INFO, ServiceName, "Authorization information loaded succesfully");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, "Unable to load authorization information due to error:", ex);
                return false;
            }
        }

        public bool IsValidAuthTokens()
        {
            var validationTask = _api.ValidateTokenAsync(TwitchInfo.Token);
            validationTask.Wait();
            return validationTask.Result;
        }

        public void UpdateTwitchInfo()
        {
            var task = _api.GetChannelInfoAsync(TwitchInfo.Token);
            task.Wait();
            var (Login, Id) = task.Result;
            if (string.IsNullOrEmpty(Login) || string.IsNullOrEmpty(Id)) {
                Logger.Log(LOGTYPE.ERROR, ServiceName, "Unable to update login and id due to bad token.");
            } else
            {
                TwitchInfo.Login = Login;
                TwitchInfo.ID = Id;
            }
        }

        public void UpdateAuthInfo()
        {
            var tokentask = _api.RefreshAccessTokenAsync(TwitchInfo.RefreshToken);
            tokentask.Wait();
            var (AccessToken, RefreshToken) = tokentask.Result;
            if (string.IsNullOrEmpty(AccessToken) || string.IsNullOrEmpty(RefreshToken)) {
                Logger.Log(LOGTYPE.ERROR, ServiceName, "Unable to update tokens due to bad refresh token.");
                TwitchInfo = new();
                return;
            }
            else
            {
                TwitchInfo.Token = AccessToken;
                TwitchInfo.RefreshToken = RefreshToken;
            }
            var channelinfotask = _api.GetChannelInfoAsync(TwitchInfo.Token);
            channelinfotask.Wait();
            var (login, id) = channelinfotask.Result;
            if(string.IsNullOrEmpty(login) || string.IsNullOrEmpty(id))
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, "Unable to updated login and id due to bad token");
                TwitchInfo = new();
                return;
            }
            TwitchInfo.Login = login;
            TwitchInfo.ID = id;
        }

        private static string Encrypt(string plainText, string salt) 
        {
            using Aes aes = Aes.Create();
            aes.Key = DeriveKey(salt);
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            using var ms = new MemoryStream();
            
            ms.Write(aes.IV, 0, aes.IV.Length);
            
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(plainText);
            }

            return Convert.ToBase64String(ms.ToArray());
        }

        private static string Decrypt(string cipherText, string salt) 
        {
            byte[] buffer = Convert.FromBase64String(cipherText);
            
            using Aes aes = Aes.Create();
            aes.Key = DeriveKey(salt);
            
            byte[] iv = new byte[aes.IV.Length];
            Array.Copy(buffer, iv, iv.Length);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            using var ms = new MemoryStream(buffer, iv.Length, buffer.Length - iv.Length);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);
            
            return sr.ReadToEnd();
        }

        private static byte[] DeriveKey(string salt) 
        {
            using var deriveBytes = new Rfc2898DeriveBytes(
                Encoding.UTF8.GetBytes(salt), 
                Encoding.UTF8.GetBytes(salt), 
                iterations: 10000, 
                HashAlgorithmName.SHA256);
            
            return deriveBytes.GetBytes(32); // 256-bit key
        }
    }
}