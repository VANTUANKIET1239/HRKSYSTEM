using AUTH.Domain.Entities;
using AUTH.Infrastructure.Data;
using AUTH.Infrastructure.Identity;
using Azure.Core;
using Core.Common.Constants.Common;
using Core.Common.Cookie;
using Core.Common.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Common.Helpers;
using Core.Common.JwtHandler;
using Core.Common.JwtHandler.Entities;
using Core.Common.Repositories;
using CoreEngine.CQRS;
using HRK.AUTH.Features.Commands.RefreshToken;
using HRK.AUTH.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace HRK.AUTH.Features.Commands.LoginUser
{
    public class LogouUserCommandHandler : HRKBaseCommand, ICommandHandler<LogouUserCommand, BaseResponse<LogoutUserResponse>>
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IUnitOfWork<ApplicationDbContext> _unitOfWork;
        private readonly ICoreCookieService _coreCookieService;
        private readonly IRefreshTokenService _refreshTokenService;

        //     private readonly string _apiKey;

        public LogouUserCommandHandler(
            SignInManager<ApplicationUser> signInManager,
            IHttpContextAccessor httpContextAccessor,
            IUnitOfWork<ApplicationDbContext> unitOfWork,
            ICoreCookieService coreCookieService,
            IOptions<JwtSettings> options,
            IRefreshTokenService refreshTokenService
            ) : base(httpContextAccessor)
        {
            this._signInManager = signInManager;
            this._unitOfWork = unitOfWork;
            this._coreCookieService = coreCookieService;
            this._refreshTokenService = refreshTokenService;
        }

        public async Task<BaseResponse<LogoutUserResponse>> Handle(LogouUserCommand request, CancellationToken cancellationToken)
        {
            // set logout time
            // revoke refresh token 
            // logout by using identity 
            try
            {
                await _unitOfWork.BeginTransactionAsync();


                var currentSession = GetCurrentLoginSession();
                var currrentLoginSession = await _unitOfWork.Repository<HRK_LoginSession>().Table().FirstOrDefaultAsync(x => x.UserId == UserId && x.SessionId == currentSession, cancellationToken);

                if (currrentLoginSession == null)
                {
                    return BaseResponse<LogoutUserResponse>.FailResponse("Failed to logout");
                }

                currrentLoginSession.LogoutTime = DateTime.UtcNow;
                await _refreshTokenService.RevokeAllUserRefreshTokensAsync(UserId, cancellationToken);

          

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await _unitOfWork.CommitTransactionAsync();



                _coreCookieService.DeleteCookie(Constants.JSON_WEB_TOKEN.REFRESHTOKEN, true);

                await _signInManager.SignOutAsync();

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
