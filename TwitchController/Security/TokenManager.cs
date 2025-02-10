using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace TwitchController.Security 
{
    public static class TokenManager 
    {
        private static readonly string TokenDir = "Tokens";

        public static void SaveToken(string channel, string token) 
        {
            Directory.CreateDirectory(TokenDir);

            try 
            {
                string channelHash = ComputeHash(channel);
                string path = Path.Combine(TokenDir, $"{channelHash}.token");
                
                string encrypted = Encrypt(token, channel);
                File.WriteAllText(path, encrypted);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Token save error: {ex.Message}");
            }
        }

        public static string? LoadToken(string channel) 
        {
            try 
            {
                string channelHash = ComputeHash(channel);
                string path = Path.Combine(TokenDir, $"{channelHash}.token");

                if (File.Exists(path))
                {
                    if((DateTime.Now.Date - File.GetCreationTime(path).Date).TotalDays >= 28)
                    {
                        File.Delete(path);
                        return null;
                    }
                    return Decrypt(File.ReadAllText(path), channel);
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Token load error: {ex.Message}");
                return null;
            }
        }

        private static string ComputeHash(string input) 
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            byte[] hash = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hash);
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