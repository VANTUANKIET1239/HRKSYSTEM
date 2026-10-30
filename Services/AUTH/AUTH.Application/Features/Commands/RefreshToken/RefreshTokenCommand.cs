using Core.Web.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Application.CQRS;

namespace AUTH.Application.Features.Commands.RefreshToken
{

    public class RefreshTokenCommand : ICommand<BaseResponse<RefreshTokenResponse>>
    {
        public required string Audience { get; set; }
    }
}
