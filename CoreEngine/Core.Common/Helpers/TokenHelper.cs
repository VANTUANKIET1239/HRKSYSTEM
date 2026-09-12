using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Core.Common.Helpers
{
    public static class TokenHelper
    {
        public static string NewSecureRandomToken(int bytes = 64) =>
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(bytes));

        public static string HashToken(string raw)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
            return Convert.ToBase64String(bytes);
        }

        public static bool VerifyToken(string raw, string stored)
        {
            if (string.IsNullOrEmpty(raw) || string.IsNullOrEmpty(stored)) return false;

            var currentHash = HashToken(raw);
            if (CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(currentHash), Encoding.UTF8.GetBytes(stored)))
            {
                return true;
            }

            // Fallback for legacy PBKDF2 hashed tokens
            try
            {
                var data = Convert.FromBase64String(stored);
                if (data.Length == 48) // 16 bytes salt + 32 bytes hash
                {
                    var salt = data.AsSpan(0, 16).ToArray();
                    var hash = data.AsSpan(16, 32).ToArray();
                    using var pbkdf2 = new Rfc2898DeriveBytes(raw, salt, 100_000, HashAlgorithmName.SHA256);
                    var test = pbkdf2.GetBytes(32);
                    return CryptographicOperations.FixedTimeEquals(test, hash);
                }
            }
            catch
            {
                // Not legacy base64 format
            }

            return false;
        }
    }
}
