using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using TwitchController.Logs;

namespace TwitchController.Security 
{
    internal static class AuthorizationManager 
    {
        private static string TokenDir => "Users";

        public static void SaveInfo(string secret, string Login, string ID, string Token, string RefreshToken)
        {
            try
            {
                Directory.CreateDirectory(TokenDir);
                string path = Path.Combine(TokenDir, "user.info");
                string data = string.Join("\n", [Login, ID, Token, RefreshToken]);
                string encrypted = Encrypt(data, secret);

                File.WriteAllText(path, encrypted);
                Logger.Info($"Authorization info saved successfully in: {path}");
            }
            catch (Exception ex)
            {
                Logger.Warn($"Token save failed: {ex.Message}");
            }

        }

        public static (string Login, string ID, string Token, string RefreshToken)? LoadInfo(string secret)
        {
            try
            {
                string path = Path.Combine(TokenDir, "user.info");

                if (File.Exists(path))
                {
                    if ((DateTime.Now - File.GetCreationTime(path)).TotalDays >= 14)
                    {
                        File.Delete(path);
                        return null;
                    }
                    var data = Decrypt(File.ReadAllText(path), secret).Split("\n");
                    if (data.Length != 4) return null;
                    return (data[0], data[1], data[2], data[3]);
                }
                else { return null; }
            }
            catch (Exception ex) {
                Logger.Error($"Failed to load Authorization info: {ex.Message}");
                return null;
            }
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