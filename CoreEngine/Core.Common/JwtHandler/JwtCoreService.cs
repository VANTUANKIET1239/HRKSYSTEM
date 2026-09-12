using Core.Common.JwtHandler.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Core.Common.JwtHandler
{

   

    public interface IJwtCoreService
    {
        AccessTokenResponse GenerateToken(IEnumerable<Claim> claims, string audience);
        ClaimsPrincipal? ValidateToken(string token);

        //string CreateRefreshToken(string userId);

        public string NewSecureRandomToken(int bytes = 64);

        public bool VerifyToken(string raw, string stored);

        public string HashToken(string raw);
    }
    class JwtCoreService : IJwtCoreService
    {

        private readonly JwtSettings _settings;
        private readonly byte[] _key;

        public JwtCoreService(IOptions<JwtSettings> options)
        {
            _settings = options.Value;
            _key = Encoding.UTF8.GetBytes(_settings.SecretKey);
        }

        public AccessTokenResponse GenerateToken(IEnumerable<Claim> claims, string audience)
        {

            var expiredTime = DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expiredTime,
                Issuer = _settings.Issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(_key), SecurityAlgorithms.HmacSha256Signature)
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return new AccessTokenResponse(tokenHandler.WriteToken(token),expiredTime);
        }


        //private static string Hash(string raw)
        //{
        //    using var sha = SHA256.Create();
        //    var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(raw));
        //    return Convert.ToHexString(bytes);
        //}

        //public string CreateRefreshToken(string userId)
        //{
        //    // 256-bit random token
        //    var bytes = new byte[32];
        //    RandomNumberGenerator.Fill(bytes);
        //    var tokenString = Hash(Convert.ToBase64String(bytes));

        //    return tokenString;
        //}


        public string NewSecureRandomToken(int bytes = 64) =>
            Core.Common.Helpers.TokenHelper.NewSecureRandomToken(bytes);

        public string HashToken(string raw) =>
            Core.Common.Helpers.TokenHelper.HashToken(raw);

        public bool VerifyToken(string raw, string stored) =>
            Core.Common.Helpers.TokenHelper.VerifyToken(raw, stored);


        public ClaimsPrincipal? ValidateToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                var validationParams = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = _settings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _settings.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(_key),
                    ClockSkew = TimeSpan.Zero
                };

                return tokenHandler.ValidateToken(token, validationParams, out _);
            }
            catch
            {
                return null;
            }
        }

      
    }
}
