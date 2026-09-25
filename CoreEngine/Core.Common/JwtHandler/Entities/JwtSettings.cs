using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Common.JwtHandler.Entities
{
    public class JwtSettings
    {
        public string SecretKey { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int ExpiryMinutes { get; set; } = 60;

        /// <summary>
        /// Optional refresh-token lifetime in minutes. When configured with a
        /// positive value it takes precedence over RefreshTokenDays.
        /// </summary>
        public int? RefreshTokenExpiryMinutes { get; set; }

        public int RefreshTokenDays { get; set; } = 7;

        public TimeSpan GetRefreshTokenLifetime() =>
            RefreshTokenExpiryMinutes is > 0
                ? TimeSpan.FromMinutes(RefreshTokenExpiryMinutes.Value)
                : TimeSpan.FromDays(RefreshTokenDays);
    }
}
