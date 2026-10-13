using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.AUTH.Jwt.Entities
{
    public class AccessTokenResponse
    {
        public string AccessToken { get; init; } = string.Empty;
        public DateTime ExpiresAt { get; init; }

        public AccessTokenResponse(string accessToken, DateTime expiresAt )
        {
            this.AccessToken = accessToken;
            this.ExpiresAt = expiresAt;
        }
    }

}
