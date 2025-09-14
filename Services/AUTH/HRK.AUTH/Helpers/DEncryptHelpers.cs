using System.Security.Cryptography;

namespace HRK.AUTH.Helpers
{
    public static class DEncryptHelpers
    {
        public static string Hash(string raw)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(raw));
            return Convert.ToHexString(bytes); // store this
        }
    }
}
