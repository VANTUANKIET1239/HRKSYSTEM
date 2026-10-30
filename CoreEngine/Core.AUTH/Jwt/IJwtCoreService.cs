using Core.AUTH.Jwt.Entities;
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

namespace Core.AUTH.Jwt
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
}
