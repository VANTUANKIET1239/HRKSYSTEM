using AUTH.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
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
using System.Security;
using System.Security.Claims;

namespace AUTH.Application.Features.Commands.RefreshToken
{
    public class RefreshTokenCommandHandler : HRKBaseCommand, ICommandHandler<RefreshTokenCommand, BaseResponse<RefreshTokenResponse>>
    {
        private readonly IJwtCoreService _jwtCoreService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IOptions<JwtSettings> _jwtOptions;
        private readonly ICoreCookieService _coreCookieService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IApplicationRouteConfigService _applicationRouteConfigs;

        public RefreshTokenCommandHandler(
            IJwtCoreService jwtCoreService,
            IUnitOfWork unitOfWork,
            IHttpContextAccessor httpContextAccessor,
            IOptions<JwtSettings> options,
            ICoreCookieService coreCookieService,
            IRefreshTokenService refreshTokenService,
            IApplicationRouteConfigService applicationRouteConfigs
        ) : base(httpContextAccessor)
        {
            _jwtCoreService = jwtCoreService;
            _unitOfWork = unitOfWork;
            _jwtOptions = options;
            this._coreCookieService = coreCookieService;
            this._refreshTokenService = refreshTokenService;
            _applicationRouteConfigs = applicationRouteConfigs;
        }

        public async Task<BaseResponse<RefreshTokenResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext == null || !httpContext.Request.Cookies.TryGetValue(Constants.JSON_WEB_TOKEN.REFRESHTOKEN, out var presentedRaw))
                return BaseResponse<RefreshTokenResponse>.FailResponse("Invalid or expired refresh token.", statusCode: 401);

            if (!await _applicationRouteConfigs.IsAudienceAllowedAsync(request.Audience, cancellationToken))
            {
                return BaseResponse<RefreshTokenResponse>.FailResponse("Unauthorized", statusCode: 401);
            };

            RotatedSession? rotatedSession = null;

            try
            {
                await _unitOfWork.ExecuteStrategyAsync(async () =>
                {
                    await _unitOfWork.BeginTransactionAsync();

                    rotatedSession = await _refreshTokenService.RotateRefreshAsync(
                        presentedRaw,
                        IpAddress,
                        _jwtOptions.Value.GetRefreshTokenLifetime(),
                        cancellationToken);

                    await _unitOfWork.CommitTransactionAsync();
                });

                if (rotatedSession == null)
                {
                    return BaseResponse<RefreshTokenResponse>.FailResponse("Failed to rotate refresh token.");
                }

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

                var refreshCookieUsesMinutes = _jwtOptions.Value.RefreshTokenExpiryMinutes is > 0;
                _coreCookieService.SetCookie(
                    Constants.JSON_WEB_TOKEN.REFRESHTOKEN,
                    rotatedSession.NewRefreshToken,
                    refreshCookieUsesMinutes ? Expiration.Minute : Expiration.Day,
                    refreshCookieUsesMinutes
                        ? _jwtOptions.Value.RefreshTokenExpiryMinutes!.Value
                        : _jwtOptions.Value.RefreshTokenDays,
                    true);

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
