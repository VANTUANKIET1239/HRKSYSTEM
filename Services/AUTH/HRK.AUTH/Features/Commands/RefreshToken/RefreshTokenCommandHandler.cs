using AUTH.Domain.Entities;
using AUTH.Infrastructure.Data;
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
using HRK.AUTH.Helpers;
using HRK.AUTH.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Security;
using System.Security.Claims;

namespace HRK.AUTH.Features.Commands.RefreshToken
{
    public class RefreshTokenCommandHandler : HRKBaseCommand, ICommandHandler<RefreshTokenCommand, BaseResponse<RefreshTokenResponse>>
    {
        private const string AudienceKey = "HrkAudiences";
        private readonly IJwtCoreService _jwtCoreService;
        private readonly IUnitOfWork<ApplicationDbContext> _unitOfWork;
        private readonly IOptions<JwtSettings> _jwtOptions;
        private readonly ICoreCookieService _coreCookieService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IEnumerable<string> _audiences;
        public RefreshTokenCommandHandler(
            IJwtCoreService jwtCoreService,
            IUnitOfWork<ApplicationDbContext> unitOfWork,
            IHttpContextAccessor httpContextAccessor,
            IOptions<JwtSettings> options,
            ICoreCookieService coreCookieService,
            IConfiguration configuration,
            IRefreshTokenService refreshTokenService
        ) : base(httpContextAccessor)
        {
            _jwtCoreService = jwtCoreService;
            _unitOfWork = unitOfWork;
            _jwtOptions = options;
            this._coreCookieService = coreCookieService;
            this._refreshTokenService = refreshTokenService;
            _audiences = configuration.GetSection(AudienceKey).Get<IEnumerable<string>>();
        }

        private bool OnCheckValidAudience(RefreshTokenCommand refreshTokenCommand)
        {
            if (_audiences.Any(x => x.Equals(refreshTokenCommand.Audience, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            return false;
        }

        public async Task<BaseResponse<RefreshTokenResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {

            var httpContext = _httpContextAccessor.HttpContext;


            if (!httpContext.Request.Cookies.TryGetValue(Constants.JSON_WEB_TOKEN.REFRESHTOKEN, out var presentedRaw))
                return BaseResponse<RefreshTokenResponse>.FailResponse("Invalid or expired refresh token.");



            var currentJti = User?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;

            if (currentJti is null)
            {
                return BaseResponse<RefreshTokenResponse>.FailResponse("Invalid token");
            };

            if (!OnCheckValidAudience(request))
            {
                return BaseResponse<RefreshTokenResponse>.FailResponse("Unauthorized");
            };

            await _unitOfWork.BeginTransactionAsync();


            try
            {


                RotatedSession rotatedSession = await _refreshTokenService.RotateRefreshAsync(presentedRaw, IpAddress, TimeSpan.FromDays(_jwtOptions.Value.RefreshTokenDays), cancellationToken);


                var claims = new List<Claim>
                                {
                                    new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, rotatedSession.UserId),
                                    new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                                    new Claim(Constants.JSON_WEB_TOKEN.SESSIONID, rotatedSession.SessionId.ToString()),
                                    new Claim(Constants.JSON_WEB_TOKEN.USERID, rotatedSession.UserId)
                                };



                var newAccessToken = _jwtCoreService.GenerateToken(claims, request.Audience);
                if (string.IsNullOrEmpty(newAccessToken.AccessToken))
                {
                    return BaseResponse<RefreshTokenResponse>.FailResponse("Failed to generate new access token.");
                }



                await _unitOfWork.CommitTransactionAsync();

                _coreCookieService.SetCookie(Constants.JSON_WEB_TOKEN.REFRESHTOKEN, rotatedSession.NewRefreshToken, Expiration.Day, _jwtOptions.Value.RefreshTokenDays, true);

                return BaseResponse<RefreshTokenResponse>.SuccessResponse(new RefreshTokenResponse
                {
                    AccessToken = newAccessToken.AccessToken,
                    AccessTokenExpiresAt = newAccessToken.ExpiresAt,
                });




            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();

                return BaseResponse<RefreshTokenResponse>.FailResponse(ex.Message);
            }
        }


       
    }
}



