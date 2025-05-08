using System.Text;
using TTvActionHub.Logs;
using TTvActionHub.Twitch;
using System.Security.Cryptography;

namespace TTvActionHub.Managers
{
    public class AuthManager
    {
        public (string Login, string ID, string Token, string RefreshToken) TwitchInfo;
        private static string ServiceName => "AuthManager";

        private static string AuthDir => "auth";
        private static string FileName => "data";

        private readonly string _secret;
        private readonly TwitchApi _api;

        public AuthManager(TwitchApi api, string secret)
        {
            ArgumentNullException.ThrowIfNull(api, nameof(api));
            ArgumentException.ThrowIfNullOrWhiteSpace(secret, nameof(secret));
            _api = api;
            _secret = secret;
            // Initialize TwitchInfo with default/empty values
            TwitchInfo = (Login: string.Empty, ID: string.Empty, Token: string.Empty, RefreshToken: string.Empty);
        }

        public bool LoadTwitchInfo()
        {
            var path = Path.Combine(AuthDir, FileName);
            
            try
            {
                if (!File.Exists(path))
                {
                    Logger.Log(LOGTYPE.INFO, ServiceName, $"Authorization file not found at {path}. No data loaded.");
                    return false;
                }
                var encryptedData = File.ReadAllText(path);
                if (string.IsNullOrEmpty(encryptedData))
                {
                    Logger.Log(LOGTYPE.WARNING, ServiceName, $"Authorization file at {path} is empty. No data loaded.");
                    return false;
                }
                var decryptedData = Decrypt(encryptedData, _secret);
                var data = decryptedData.Split('\n');
                if (data.Length != 4)
                {
                    Logger.Log(LOGTYPE.WARNING, ServiceName, $"Invalid data format in authorization file at {path}. Expected 4 parts, found {data.Length}.");
                    return false;
                }
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
                Logger.Log(LOGTYPE.ERROR, ServiceName, "An unexpected error occurred while loading authorization information from {path}:", ex);
                return false;
            }
        }

        public bool SaveTwitchInfo()
        {
            try
            {
                Directory.CreateDirectory(AuthDir);
                var path = Path.Combine(AuthDir, FileName);
                var (login, id, token, rtoken) = TwitchInfo;
                if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(id) || string.IsNullOrEmpty(token) || string.IsNullOrEmpty(rtoken))
                {
                    Logger.Log(LOGTYPE.WARNING, ServiceName, "Attempted to save incomplete Twitch info. Aborting save.");
                    return false;
                }
                var data = string.Join('\n', [login, id, token, rtoken]);
                var encryptedData = Encrypt(data, _secret);
                File.WriteAllText(path, encryptedData);
                Logger.Log(LOGTYPE.INFO, ServiceName, $"Authorization information saved successfully at {path}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, "An unexpected error occurred while saving authorization information:", ex);
                return false;
            }
        }

        public async Task<bool> IsValidTokensAsync()
        {
            if (string.IsNullOrEmpty(TwitchInfo.Token))
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, "Attempted to validate an empty token.");
                return false;
            }
            try
            {
                var isValid = await _api.ValidateTokenAsync(TwitchInfo.Token).ConfigureAwait(false);
                if (!isValid)
                {
                    Logger.Log(LOGTYPE.WARNING, ServiceName, "Twitch API reported the current access token is invalid.");
                }
                return isValid;
            }
            catch (Exception ex)
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, "An error  occurred while validating the Twitch token:", ex);
                return false;
            }
        }

        public async Task<bool> UpdateAuthInfoAsync()
        {
            if (string.IsNullOrEmpty(TwitchInfo.RefreshToken))
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, "Unable to refresh authentication: Refresh token is missing.");
                TwitchInfo = new();
                return false;
            }
            try
            {
                Logger.Log(LOGTYPE.INFO, ServiceName, "Attempting to refresh Twitch tokens.");
                var (newAccessToken, newRefreshToken) =
                    await _api.RefreshAccessTokenAsync(TwitchInfo.RefreshToken).ConfigureAwait(false);
                if (string.IsNullOrEmpty(newAccessToken) || string.IsNullOrEmpty(newRefreshToken))
                {
                    Logger.Log(LOGTYPE.ERROR, ServiceName,
                        "Failed to refresh tokens: Received empty or invalid tokens from Twitch API (Refresh token might be expired or revoked).");
                    TwitchInfo = new();
                    return false;
                }
                TwitchInfo.Token = newAccessToken;
                TwitchInfo.RefreshToken = newRefreshToken;
                Logger.Log(LOGTYPE.INFO, ServiceName, "Twitch tokens refreshed successfully.");

                Logger.Log(LOGTYPE.INFO, ServiceName, "Attempting to update user info with new token.");
                var (login, id) = await _api.GetChannelInfoAsync(TwitchInfo.Token).ConfigureAwait(false);
                if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(id))
                {
                    Logger.Log(LOGTYPE.ERROR, ServiceName, "Refreshed token successfully, but failed to get user info with the new token.");
                    TwitchInfo.Login = string.Empty; // Clear potentially stale info
                    TwitchInfo.ID = string.Empty;
                    return true;
                }
                TwitchInfo.Login = login;
                TwitchInfo.ID = id;
                Logger.Log(LOGTYPE.INFO, ServiceName, $"Twitch authentication info fully updated for login: {login}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log(LOGTYPE.ERROR, ServiceName, "An error occurred during the authentication information update process:", ex);
                TwitchInfo = new();
                return false;
            }
        }

        private static string Encrypt(string plainText, string secret)
        {
            ArgumentNullException.ThrowIfNull(plainText);
            ArgumentNullException.ThrowIfNull(secret); 

            using var aes = Aes.Create(); 
            aes.Key = DeriveKey(secret); 
            aes.GenerateIV(); 

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            
            using var ms = new MemoryStream();

            ms.Write(aes.IV, 0, aes.IV.Length);

            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs, Encoding.UTF8)) 
            {
                sw.Write(plainText);
            } 
            // StreamWriter Dispose() flushes and closes, which causes CryptoStream to flush final block.

            return Convert.ToBase64String(ms.ToArray());
        }

        private static string Decrypt(string cipherTextBase64, string secret)
        {
            ArgumentNullException.ThrowIfNull(cipherTextBase64);
            ArgumentNullException.ThrowIfNull(secret);

            // Convert the Base64 string back into bytes
            byte[] buffer = Convert.FromBase64String(cipherTextBase64);

            using Aes aes = Aes.Create();
            int ivLength = aes.IV.Length; // Get the expected IV length (usually 16 bytes for AES)

            // Ensure the buffer is large enough to contain at least the IV
            if (buffer.Length < ivLength)
            {
                throw new CryptographicException("Invalid cipher text buffer: too short to contain IV.");
            }

            aes.Key = DeriveKey(secret); 

            // Extract the IV from the beginning of the buffer
            var iv = new byte[ivLength];
            Array.Copy(buffer, 0, iv, 0, iv.Length);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(buffer, ivLength, buffer.Length - ivLength);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs, Encoding.UTF8); // Specify UTF8 encoding

            // Read the decrypted plaintext
            return sr.ReadToEnd();
        }

        private static byte[] DeriveKey(string secret) 
        {
            byte[] saltBytes = Encoding.UTF8.GetBytes(secret);
            const int iterations = 1000;

            using var deriveBytes = new Rfc2898DeriveBytes(
               password: Encoding.UTF8.GetBytes(secret), // Use the secret as the password
               salt: saltBytes,                          // Use the derived (or a separate) salt
               iterations: iterations,                   // Number of iterations
               hashAlgorithm: HashAlgorithmName.SHA256); // Specify the hash algorithm
            // Generate a 32-byte (256-bit) key, suitable for AES-256
            return deriveBytes.GetBytes(32);
        }
    }
}