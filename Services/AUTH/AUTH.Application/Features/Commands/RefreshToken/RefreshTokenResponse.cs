using Core.Web.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Application.CQRS;

namespace AUTH.Application.Features.Commands.RefreshToken
{

    public class RefreshTokenResponse
    {
        public string AccessToken { get; set; }
        public DateTime AccessTokenExpiresAt { get; set; }   
    }
}
