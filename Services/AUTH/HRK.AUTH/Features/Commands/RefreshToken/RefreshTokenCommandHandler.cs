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
using Microsoft.Extensions.Options;
using System.Data.Entity;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Security;
using System.Security.Claims;

namespace HRK.AUTH.Features.Commands.RefreshToken
{
    public class RefreshTokenCommandHandler : HRKBaseCommand, ICommandHandler<RefreshTokenCommand, BaseResponse<RefreshTokenResponse>>
    {
        private readonly IJwtCoreService _jwtCoreService;
        private readonly IUnitOfWork<ApplicationDbContext> _unitOfWork;
        private readonly IOptions<JwtSettings> _jwtOptions;
        private readonly ICoreCookieService _coreCookieService;
        public RefreshTokenCommandHandler(
            IJwtCoreService jwtCoreService,
            IUnitOfWork<ApplicationDbContext> unitOfWork,
            IHttpContextAccessor httpContextAccessor,
            IOptions<JwtSettings> options,
              ICoreCookieService coreCookieService
        ) : base(httpContextAccessor)
        {
            _jwtCoreService = jwtCoreService;
            _unitOfWork = unitOfWork;
            _jwtOptions = options;
            this._coreCookieService = coreCookieService;
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

            await _unitOfWork.BeginTransactionAsync();


            try
            {


                RotatedSession rotatedSession = await RotateRefreshAsync(presentedRaw, IpAddress, TimeSpan.FromDays(_jwtOptions.Value.RefreshTokenDays), cancellationToken);


                var claims = new List<Claim>
                {
                    new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, rotatedSession.UserId),
                    new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(Constants.JSON_WEB_TOKEN.SESSIONID, rotatedSession.SessionId.ToString()),
                    new Claim(Constants.JSON_WEB_TOKEN.USERID, rotatedSession.UserId)
                };




                var newAccessToken = _jwtCoreService.GenerateToken(claims);
                if (string.IsNullOrEmpty(newAccessToken.AccessToken))
                {
                    return BaseResponse<RefreshTokenResponse>.FailResponse("Failed to generate new access token.");
                }



                await _unitOfWork.CommitTransactionAsync();


               // _coreCookieService.SetCookie(Constants.JSON_WEB_TOKEN.JWT, newAccessToken.AccessToken, Expiration.Minute, _jwtOptions.Value.ExpiryMinutes, true);
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


        public async Task<RotatedSession> RotateRefreshAsync(
          string refreshTokenRaw,
          string? ip,
          TimeSpan? refreshTtl = null,
          CancellationToken ct = default)
        {

            var tokens = await _unitOfWork.Repository<HRK_RefreshToken>().Query()
                .OrderByDescending(x => x.CreatedAt)
                .Take(500)
                .ToListAsync(ct);

            var rt = tokens.FirstOrDefault(t => TokenHelper.VerifyToken(refreshTokenRaw, t.TokenHash));
            if (rt is null) throw new SecurityException("Invalid refresh token.");

            // 2) Basic checks
            if (rt.ExpiresAt <= DateTime.UtcNow)
                throw new SecurityException("Refresh token expired.");


            if (!rt.IsActive)
            {
                throw new SecurityException("Invalid or expired refresh token.");
            }


            var session = await _unitOfWork.Repository<HRK_LoginSession>().Query().FirstOrDefaultAsync(s => s.SessionId == rt.SessionId);
            if (session is null || !session.IsActive) 
            {
                throw new SecurityException("Invalid Session");
            }


            // 3) Reuse detection: if already revoked, nuke the chain and fail
            if (rt.RevokedAt != null)
            {
                var chain = _unitOfWork.Repository<HRK_RefreshToken>().Query().Where(x => x.SessionId == rt.SessionId && x.RevokedAt == null);

                await chain.ForEachAsync(s =>
                {
                    s.RevokedAt = DateTime.UtcNow;
                    s.RevokedByIp = ip;
                });

                throw new SecurityException("Refresh token reuse detected; session revoked.");
            }

            // 4) Rotate: create new refresh token, revoke the presented one and link
            var newRaw = TokenHelper.NewSecureRandomToken();
            var newHash = TokenHelper.HashToken(newRaw);
            var ttl = refreshTtl ?? TimeSpan.FromDays(_jwtOptions.Value.RefreshTokenDays);

            var newRt = new HRK_RefreshToken
            {
                UserId = rt.UserId,
                SessionId = rt.SessionId,
                TokenHash = newHash,
                CreatedAt = DateTime.UtcNow,
                CreatedByIp = ip ?? string.Empty,
                ExpiresAt = DateTime.UtcNow.Add(ttl)
            };

            rt.RevokedAt = DateTime.UtcNow;
            rt.RevokedByIp = ip;
            rt.ReplacedByTokenHash = newHash;

            await _unitOfWork.Repository<HRK_RefreshToken>().AddAsync(newRt);
            await _unitOfWork.SaveChangesAsync(ct);

            return new RotatedSession(rt.UserId, rt.SessionId, newRaw);
        }
    }
}



