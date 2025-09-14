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
            using var pbkdf2 = new Rfc2898DeriveBytes(raw, 16, 100_000, HashAlgorithmName.SHA256);
            var salt = pbkdf2.Salt;
            var hash = pbkdf2.GetBytes(32);
            return Convert.ToBase64String(salt.Concat(hash).ToArray());
        }

        public static bool VerifyToken(string raw, string stored)
        {
            var data = Convert.FromBase64String(stored);
            var salt = data.AsSpan(0, 16).ToArray();
            var hash = data.AsSpan(16, 32).ToArray();
            using var pbkdf2 = new Rfc2898DeriveBytes(raw, salt, 100_000, HashAlgorithmName.SHA256);
            var test = pbkdf2.GetBytes(32);
            return CryptographicOperations.FixedTimeEquals(test, hash);
        }
    }
}
