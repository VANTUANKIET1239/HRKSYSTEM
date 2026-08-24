using Core.Common.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using CoreEngine.CQRS;

namespace AUTH.Application.Features.Commands.RefreshToken
{

    public class RefreshTokenResponse
    {
        public string AccessToken { get; set; }
        public DateTime AccessTokenExpiresAt { get; set; }   
    }

    public class RefreshTokenCommand : ICommand<BaseResponse<RefreshTokenResponse>>
    {
        public required string Audience { get; set; }
    }
}