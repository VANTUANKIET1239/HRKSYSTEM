using AUTH.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Core.Common.Constants.Common;
using Core.Common.Cookie;
using Core.Common.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Common.Helpers;
using Core.Common.JwtHandler;
using Core.Common.JwtHandler.Entities;
using Core.Common.Repositories;
using CoreEngine.CQRS;
using AUTH.Application.Interfaces;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Data.Entity;

namespace AUTH.Application.Features.Commands.LogoutUser
{
    public class LogouUserCommandHandler : HRKBaseCommand, ICommandHandler<LogouUserCommand, BaseResponse<LogoutUserResponse>>
    {
        private readonly IIdentityService _identityService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICoreCookieService _coreCookieService;
        private readonly IRefreshTokenService _refreshTokenService;

        public LogouUserCommandHandler(
            IIdentityService identityService,
            IHttpContextAccessor httpContextAccessor,
            IUnitOfWork unitOfWork,
            ICoreCookieService coreCookieService,
            IRefreshTokenService refreshTokenService
            ) : base(httpContextAccessor)
        {
            this._identityService = identityService;
            this._unitOfWork = unitOfWork;
            this._coreCookieService = coreCookieService;
            this._refreshTokenService = refreshTokenService;
        }

        public async Task<BaseResponse<LogoutUserResponse>> Handle(LogouUserCommand request, CancellationToken cancellationToken)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var currentSession = GetCurrentLoginSession();
                var currrentLoginSession = await _unitOfWork.Repository<HRK_LoginSession>().Query().FirstOrDefaultAsync(x => x.UserId == UserId && x.SessionId == currentSession, cancellationToken);

                if (currrentLoginSession == null)
                {
                    return BaseResponse<LogoutUserResponse>.FailResponse("Failed to logout");
                }

                currrentLoginSession.LogoutTime = DateTime.UtcNow;
                await _refreshTokenService.RevokeAllUserRefreshTokensAsync(UserId, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync();

                _coreCookieService.DeleteCookie(Constants.JSON_WEB_TOKEN.REFRESHTOKEN, true);

                await _identityService.SignOutAsync();

                return new BaseResponse<LogoutUserResponse>
                {
                    Data = new LogoutUserResponse
                    {
                        LoggedOut = true
                    },
                    Success = true,
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();

                return new BaseResponse<LogoutUserResponse>
                {
                    Data = new LogoutUserResponse()
                    {
                        LoggedOut = false
                    },
                    Success = false,
                    StatusCode = 500,
                    Message = ex.Message
                };
            }
        }
    }
}
