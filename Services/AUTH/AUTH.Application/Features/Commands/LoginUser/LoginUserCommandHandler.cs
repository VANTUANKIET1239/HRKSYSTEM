using AUTH.Domain.Entities;
using AUTH.Application.Interfaces;
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
using MediatR;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace AUTH.Application.Features.Commands.LoginUser
{
    public class LoginUserCommandHandler : HRKBaseCommand, ICommandHandler<LoginUserCommand, BaseResponse<LoginUserResponse>>
    {
        private readonly IIdentityService _identityService;
        private readonly IJwtCoreService _jwtCoreService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly ICoreCookieService _coreCookieService;
        private readonly IOptions<JwtSettings> _jwtOptions;
        private readonly IApplicationRouteConfigService _applicationRouteConfigs;

        public LoginUserCommandHandler(IIdentityService identityService,
            IJwtCoreService jwtCoreService,
            IHttpContextAccessor httpContextAccessor,
            IUnitOfWork unitOfWork,
            IConfiguration configuration,
            ICoreCookieService coreCookieService,
            IOptions<JwtSettings> options,
            IApplicationRouteConfigService applicationRouteConfigs
            ) : base(httpContextAccessor)
        {
            _identityService = identityService;
            this._jwtCoreService = jwtCoreService;
            this._httpContextAccessor = httpContextAccessor;
            this._unitOfWork = unitOfWork;
            this._configuration = configuration;
            this._coreCookieService = coreCookieService;
            this._jwtOptions = options;
            _applicationRouteConfigs = applicationRouteConfigs;
        }

        public async Task<BaseResponse<LoginUserResponse>> Handle(LoginUserCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var audience = !string.IsNullOrWhiteSpace(request.Audience)
                    ? request.Audience
                    : _jwtOptions.Value.Audience;
                if (!await _applicationRouteConfigs.IsAudienceAllowedAsync(audience, cancellationToken))
                {
                    return BaseResponse<LoginUserResponse>.FailResponse("Unsupported application audience.", statusCode: 401);
                }

                var user = await _identityService.FindByNameOrEmailAsync(request.Email);

                var validation = await OnValidatingUser(user, request);

                if (!validation.Success)
                {
                    return validation;
                }   

                Guid sessionId = Guid.NewGuid();
                Guid Jti = Guid.NewGuid();

                var claims = new List<Claim>
                {
                    new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, user!.Id),
                    new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti, Jti.ToString()),
                    new Claim(Constants.JSON_WEB_TOKEN.SESSIONID, sessionId.ToString()),
                    new Claim(Constants.JSON_WEB_TOKEN.USERID, user.Id)
                };

                var rawRefreshToken = _jwtCoreService.NewSecureRandomToken();
                await _identityService.SignInWithClaimsAsync(user.Id, isPersistent: true, claims);

                await _unitOfWork.ExecuteStrategyAsync(async () =>
                {
                    await _unitOfWork.BeginTransactionAsync();

                    var now = DateTime.UtcNow;
                    var refreshTokenLifetime = _jwtOptions.Value.GetRefreshTokenLifetime();

                    // Deactivate previous active login sessions for this user
                    var oldActiveSessions = await _unitOfWork.Repository<HRK_LoginSession>()
                        .Query()
                        .Where(s => s.UserId == user.Id && s.LogoutTime == null)
                        .ToListAsync(cancellationToken);

                    foreach (var oldSession in oldActiveSessions)
                    {
                        oldSession.LogoutTime = now;
                    }

                    //// Revoke previous active refresh tokens for this user
                    //var oldActiveTokens = await _unitOfWork.Repository<HRK_RefreshToken>()
                    //    .Query()
                    //    .Where(t => t.UserId == user.Id && t.RevokedAt == null)
                    //    .ToListAsync(cancellationToken);

                    //foreach (var oldToken in oldActiveTokens)
                    //{
                    //    oldToken.RevokedAt = now;
                    //}

                    await _unitOfWork.Repository<HRK_RefreshToken>().AddAsync(new HRK_RefreshToken
                    {
                        TokenHash = _jwtCoreService.HashToken(rawRefreshToken),
                        UserId = user.Id,
                        CreatedAt = now,
                        CreatedByIp = IpAddress,
                        ExpiresAt = now.Add(refreshTokenLifetime),
                        SessionId = sessionId
                    });

                    await _unitOfWork.Repository<HRK_LoginSession>().AddAsync(new HRK_LoginSession
                    {
                        UserId = user.Id,
                        IPAddress = IpAddress,
                        SessionId = sessionId,
                        UserAgent = _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString(),
                        LoginTime = now
                    });

                    await _identityService.ResetAccessFailedCountAsync(user.Id);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    await _unitOfWork.CommitTransactionAsync();
                });

                var refreshCookieUsesMinutes = _jwtOptions.Value.RefreshTokenExpiryMinutes is > 0;
                _coreCookieService.SetCookie(
                    Constants.JSON_WEB_TOKEN.REFRESHTOKEN,
                    rawRefreshToken,
                    refreshCookieUsesMinutes ? Expiration.Minute : Expiration.Day,
                    refreshCookieUsesMinutes
                        ? _jwtOptions.Value.RefreshTokenExpiryMinutes!.Value
                        : _jwtOptions.Value.RefreshTokenDays,
                    true);

                var tokenResult = _jwtCoreService.GenerateToken(claims, audience);

                return new BaseResponse<LoginUserResponse>
                {
                    Data = new LoginUserResponse
                    {
                        UserEmail = user.Email,
                        UserId = user.Id,
                        UserName = user.UserName,
                        AccessToken = tokenResult.AccessToken,
                        AccessTokenExpiresAt = tokenResult.ExpiresAt
                    },
                    Success = true,
                    StatusCode = 200
                };
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();

                return new BaseResponse<LoginUserResponse>
                {
                    Data = new LoginUserResponse(),
                    Success = false,
                    StatusCode = 500,
                    Message = ex.Message
                };
            }
        }

        public async Task<BaseResponse<LoginUserResponse>> OnValidatingUser(IdentityUserDto? user, LoginUserCommand request)
        {
            BaseResponse<LoginUserResponse> response = new() { Success = true };   
            if (user == null)
            {
                response = new BaseResponse<LoginUserResponse> { Data = new LoginUserResponse(), Success = false, StatusCode = 404, Message = "User not found." };
                return response;
            }

            var result = await _identityService.CheckPasswordSignInAsync(user.Id, request.Password, lockoutOnFailure: true);

            if (result.IsLockedOut)
            {
                response = new BaseResponse<LoginUserResponse> { Data = new LoginUserResponse(), Success = false, StatusCode = 403, Message = "Account locked. Try again later." };
                return response;
            }

            if (result.RequiresTwoFactor)
            {
                response = new BaseResponse<LoginUserResponse> { Data = new LoginUserResponse(), Success = false, StatusCode = 401, Message = "Two-factor required." };
                return response;
            }

            if (!result.Succeeded)
            {
                response = new BaseResponse<LoginUserResponse> { Data = new LoginUserResponse(), Success = false, StatusCode = 401, Message = "Invalid credentials." };
                return response;
            }

            return response;
        }
    }
}
