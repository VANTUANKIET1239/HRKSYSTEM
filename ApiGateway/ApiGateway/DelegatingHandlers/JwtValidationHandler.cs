using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;

namespace ApiGateway.DelegatingHandlers
{
    public class JwtValidationHandler : DelegatingHandler
    {
        private readonly ILogger<JwtValidationHandler> _logger;
        private readonly IConfiguration _configuration;

        public JwtValidationHandler(ILogger<JwtValidationHandler> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!request.Headers.Contains("Authorization"))
            {
                return new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("Authorization header is missing.")
                };
            }

            var token = request.Headers.Authorization?.Parameter;

            if (string.IsNullOrEmpty(token))
            {
                return new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("JWT token is missing.")
                };
            }

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidAudience = _configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                }, out SecurityToken validatedToken);

                // Optionally, you can extract claims here
                var jwtToken = (JwtSecurityToken)validatedToken;
                var userId = jwtToken.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value;

                _logger.LogInformation($"JWT validated for user: {userId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JWT validation failed.");
                return new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("Invalid JWT token.")
                };
            }

            return await base.SendAsync(request, cancellationToken);
        }

        public async Task<HttpResponseMessage> ValidateAndSendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return await SendAsync(request, cancellationToken);
        }
    }
}