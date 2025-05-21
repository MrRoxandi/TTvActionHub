using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using TTvActionHub.Logs;
using TTvActionHub.Managers.AuthManagerItems;
using TTvActionHub.Twitch;

namespace TTvActionHub.Managers;

public class AuthManager
{
    public (string Login, string ID, string Token, string RefreshToken) TwitchInfo { get; set; }
    private readonly AuthDbContext _db;
    private static string ServiceName => "AuthManager";

    private readonly string _secret;
    private readonly TwitchApi _api;

    public AuthManager(TwitchApi api, string secret)
    {
        ArgumentNullException.ThrowIfNull(api, nameof(api));
        ArgumentException.ThrowIfNullOrWhiteSpace(secret, nameof(secret));
        _api = api;
        _secret = secret;
        _db = new AuthDbContext();
        _db.EnsureCreated();
        // Initialize TwitchInfo with default/empty values
        TwitchInfo = (Login: string.Empty, ID: string.Empty, Token: string.Empty, RefreshToken: string.Empty);
    }

    public async Task<bool> LoadTwitchInfoAsync()
    {
        try
        {
            var authData = await _db.AuthenticationData.FirstOrDefaultAsync();
            if (authData == null)
            {
                Logger.Log(LogType.Info, ServiceName,
                    "No authorization information found in the database. No data loaded.");
                return false;
            }

            if (string.IsNullOrEmpty(authData.EncryptedAccessToken) ||
                string.IsNullOrEmpty(authData.EncryptedRefreshToken))
            {
                Logger.Log(LogType.Warning, ServiceName,
                    "Stored authorization information is incomplete (missing tokens). No data loaded.");
                return false;
            }

            string decryptedToken;
            string decryptedRefreshToken;
            try
            {
                decryptedToken = Decrypt(authData.EncryptedAccessToken, _secret);
                decryptedRefreshToken = Decrypt(authData.EncryptedRefreshToken, _secret);
            }
            catch (CryptographicException ex)
            {
                Logger.Log(LogType.Error, ServiceName,
                    "Failed to decrypt stored authorization information. Data might be corrupted or secret key changed.",
                    ex);
                return false;
            }
            catch (FormatException ex)
            {
                Logger.Log(LogType.Error, ServiceName,
                    "Failed to decrypt stored authorization information due to invalid format. Data might be corrupted.",
                    ex);
                return false;
            }

            TwitchInfo = (authData.Login, authData.TwitchUserId, decryptedToken, decryptedRefreshToken);
            Logger.Log(LogType.Info, ServiceName, "Authorization information loaded successfully from database.");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Log(LogType.Error, ServiceName,
                "An unexpected error occurred while loading authorization information from database:", ex);
            return false;
        }
    }

    public async Task<bool> SaveTwitchInfoAsync()
    {
        try
        {
            var (login, id, token, refreshToken) = TwitchInfo;
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(id) || string.IsNullOrEmpty(token) ||
                string.IsNullOrEmpty(refreshToken))
            {
                Logger.Log(LogType.Warning, ServiceName, "Attempted to save incomplete Twitch info. Aborting save.");
                return false;
            }

            string encryptedToken;
            string encryptedRefreshToken;
            try
            {
                encryptedToken = Encrypt(token, _secret);
                encryptedRefreshToken = Encrypt(refreshToken, _secret);
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, ServiceName, "Failed to encrypt tokens before saving:", ex);
                return false;
            }


            // Ищем существующую запись или создаем новую
            var authData = await _db.AuthenticationData.FirstOrDefaultAsync();
            if (authData == null)
            {
                authData = new TwitchAuthData();
                _db.AuthenticationData.Add(authData);
            }

            authData.Login = login;
            authData.TwitchUserId = id;
            authData.EncryptedAccessToken = encryptedToken;
            authData.EncryptedRefreshToken = encryptedRefreshToken;

            await _db.SaveChangesAsync();
            Logger.Log(LogType.Info, ServiceName, "Authorization information saved successfully to database.");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Log(LogType.Error, ServiceName,
                "An unexpected error occurred while saving authorization information to database:", ex);
            return false;
        }
    }

    public async Task ClearTwitchInfoAsync()
    {
        try
        {
            var authData = await _db.AuthenticationData.FirstOrDefaultAsync();
            if (authData != null)
            {
                _db.AuthenticationData.Remove(authData);
                await _db.SaveChangesAsync();
                Logger.Log(LogType.Info, ServiceName, "Authorization information cleared from database.");
            }

            TwitchInfo = (Login: string.Empty, ID: string.Empty, Token: string.Empty, RefreshToken: string.Empty);
        }
        catch (Exception ex)
        {
            Logger.Log(LogType.Error, ServiceName,
                "An error occurred while clearing authorization information from database:", ex);
        }
    }

    public async Task<bool> IsValidTokensAsync()
    {
        if (string.IsNullOrEmpty(TwitchInfo.Token))
        {
            Logger.Log(LogType.Info, ServiceName,
                "Attempted to validate an empty token. No token loaded or previously cleared.");
            return false;
        }

        try
        {
            var isValid = await _api.ValidateTokenAsync(TwitchInfo.Token).ConfigureAwait(false);
            if (!isValid)
                Logger.Log(LogType.Warning, ServiceName, "Twitch API reported the current access token is invalid.");
            return isValid;
        }
        catch (Exception ex)
        {
            Logger.Log(LogType.Error, ServiceName, "An error occurred while validating the Twitch token:", ex);
            return false;
        }
    }

    public async Task<bool> UpdateAuthInfoAsync()
    {
        if (string.IsNullOrEmpty(TwitchInfo.RefreshToken))
        {
            Logger.Log(LogType.Error, ServiceName, "Unable to refresh authentication: Refresh token is missing.");
            TwitchInfo = (Login: string.Empty, ID: string.Empty, Token: string.Empty,
                RefreshToken: string.Empty); // Сброс
            await ClearTwitchInfoAsync();
            return false;
        }

        try
        {
            Logger.Log(LogType.Info, ServiceName, "Attempting to refresh Twitch tokens.");
            var (newAccessToken, newRefreshToken) =
                await _api.RefreshAccessTokenAsync(TwitchInfo.RefreshToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(newAccessToken) || string.IsNullOrEmpty(newRefreshToken))
            {
                Logger.Log(LogType.Error, ServiceName,
                    "Failed to refresh tokens: Received empty or invalid tokens from Twitch API (Refresh token might be expired or revoked). Clearing stored tokens.");
                TwitchInfo = (Login: string.Empty, ID: string.Empty, Token: string.Empty,
                    RefreshToken: string.Empty);
                await ClearTwitchInfoAsync();
                return false;
            }

            TwitchInfo = (TwitchInfo.Login, TwitchInfo.ID, newAccessToken, newRefreshToken);
            Logger.Log(LogType.Info, ServiceName, "Twitch tokens refreshed successfully.");

            Logger.Log(LogType.Info, ServiceName, "Attempting to update user info with new token.");
            var (login, id) = await _api.GetChannelInfoAsync(TwitchInfo.Token).ConfigureAwait(false);

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(id))
            {
                Logger.Log(LogType.Error, ServiceName,
                    "Refreshed token successfully, but failed to get user info with the new token. Using previous user info if available, but tokens are updated.");
                // Сохраняем обновленные токены, даже если инфо о пользователе не получено
                // Login и ID остаются прежними из TwitchInfo, если они были, или пустыми, если их не было
                TwitchInfo = (TwitchInfo.Login, TwitchInfo.ID, newAccessToken, newRefreshToken);
            }
            else
            {
                TwitchInfo = (login, id, newAccessToken, newRefreshToken);
                Logger.Log(LogType.Info, ServiceName,
                    $"Twitch authentication info fully updated for login: {login}");
            }

            await SaveTwitchInfoAsync();
            return true;
        }
        catch (Exception ex)
        {
            Logger.Log(LogType.Error, ServiceName,
                "An error occurred during the authentication information update process:", ex);
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
        var buffer = Convert.FromBase64String(cipherTextBase64);

        using var aes = Aes.Create();
        var ivLength = aes.IV.Length; // Get the expected IV length (usually 16 bytes for AES)

        // Ensure the buffer is large enough to contain at least the IV
        if (buffer.Length < ivLength)
            throw new CryptographicException("Invalid cipher text buffer: too short to contain IV.");

        aes.Key = DeriveKey(secret);

        // Extract the IV from the beginning of the buffer
        var iv = new byte[ivLength];
        Array.Copy(buffer, 0, iv, 0, iv.Length);
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(buffer, ivLength, buffer.Length - ivLength);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs, Encoding.UTF8);

        return sr.ReadToEnd();
    }

    private static byte[] DeriveKey(string secret)
    {
        var saltBytes = Encoding.UTF8.GetBytes(secret);
        const int iterations = 1000;

        using var deriveBytes = new Rfc2898DeriveBytes(
            Encoding.UTF8.GetBytes(secret), // Use the secret as the password
            saltBytes, // Use the derived (or a separate) salt
            iterations, // Number of iterations
            HashAlgorithmName.SHA256); // Specify the hash algorithm
        // Generate a 32-byte (256-bit) key, suitable for AES-256
        return deriveBytes.GetBytes(32);
    }
}