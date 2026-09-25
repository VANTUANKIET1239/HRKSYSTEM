using AUTH.Domain.Entities;
using AUTH.Infrastructure.Data;
using Core.Common.Helpers;
using Core.Common.JwtHandler;
using Core.Common.JwtHandler.Entities;
using Core.Common.Repositories;
using AUTH.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security;

namespace AUTH.Infrastructure.Services
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly IUnitOfWork<ApplicationDbContext> _unitOfWork;
        private readonly IJwtCoreService _jwtCoreService;
        private readonly IOptions<JwtSettings> _jwtOptions;

        public RefreshTokenService(IUnitOfWork<ApplicationDbContext> unitOfWork, IJwtCoreService jwtCoreService, IOptions<JwtSettings> options)
        {
            this._unitOfWork = unitOfWork;
            this._jwtCoreService = jwtCoreService;
            this._jwtOptions = options;
        }


        public async Task RevokeAllUserRefreshTokensAsync(string userId, CancellationToken cancellationToken = default) 
        {
            var tokens = await _unitOfWork.Repository<HRK_RefreshToken>().Query()
           .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > DateTime.UtcNow)
           .ToListAsync(cancellationToken);

            if (tokens.Count == 0)
                return;

            foreach (var token in tokens)
            {
                token.RevokedAt = DateTime.UtcNow;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }


        public async Task<RotatedSession> RotateRefreshAsync(
         string refreshTokenRaw,
         string? ip,
         TimeSpan? refreshTtl = null,
         CancellationToken ct = default)
        {
            var presentedHash = _jwtCoreService.HashToken(refreshTokenRaw);

            // 1) Fast lookup directly via database index on TokenHash (~1ms)
            var rt = await _unitOfWork.Repository<HRK_RefreshToken>().Query()
                .FirstOrDefaultAsync(t => t.TokenHash == presentedHash, ct);

            // Fallback for any legacy PBKDF2 tokens during migration
            if (rt is null)
            {
                var legacyCandidates = await _unitOfWork.Repository<HRK_RefreshToken>().Query()
                    .Where(x => x.ExpiresAt > DateTime.UtcNow && x.RevokedAt == null)
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(10)
                    .ToListAsync(ct);

                rt = legacyCandidates.FirstOrDefault(t => TokenHelper.VerifyToken(refreshTokenRaw, t.TokenHash));
            }

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
            var newRaw = _jwtCoreService.NewSecureRandomToken();
            var newHash = _jwtCoreService.HashToken(newRaw);

            var ttl = refreshTtl ?? _jwtOptions.Value.GetRefreshTokenLifetime();

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
