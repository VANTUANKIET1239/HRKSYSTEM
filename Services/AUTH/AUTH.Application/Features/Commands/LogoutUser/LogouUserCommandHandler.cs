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
using Microsoft.EntityFrameworkCore;

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
                Guid? currentSession = null;
                try
                {
                    currentSession = GetCurrentLoginSession();
                }
                catch
                {
                    // Session claim may not be present if token was anonymous/expired
                }

                if (currentSession.HasValue && !string.IsNullOrEmpty(UserId))
                {

                    await _unitOfWork.ExecuteStrategyAsync(async () =>
                    {
                        await _unitOfWork.BeginTransactionAsync();
                        var currrentLoginSession = await _unitOfWork.Repository<HRK_LoginSession>()
                                            .Query()
                                            .FirstOrDefaultAsync(x => x.UserId == UserId && x.SessionId == currentSession.Value, cancellationToken);

                        if (currrentLoginSession != null)
                        {
                            currrentLoginSession.LogoutTime = DateTime.UtcNow;
                        }

                        await _refreshTokenService.RevokeAllUserRefreshTokensAsync(UserId, cancellationToken);

                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                        await _unitOfWork.CommitTransactionAsync();
                    });
                }
                else if (!string.IsNullOrEmpty(UserId))
                {
                    await _refreshTokenService.RevokeAllUserRefreshTokensAsync(UserId, cancellationToken);
                }

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

                try
                {
                    _coreCookieService.DeleteCookie(Constants.JSON_WEB_TOKEN.REFRESHTOKEN, true);
                    await _identityService.SignOutAsync();
                }
                catch
                {
                    // ignored
                }

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
