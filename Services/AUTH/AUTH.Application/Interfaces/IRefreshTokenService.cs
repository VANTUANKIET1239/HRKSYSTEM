using AUTH.Domain.Entities;

namespace AUTH.Application.Interfaces
{
    public interface IRefreshTokenService
    {
        public Task RevokeAllUserRefreshTokensAsync(string userId, CancellationToken cancellationToken = default);

        public Task<RotatedSession> RotateRefreshAsync(
        string refreshTokenRaw,
        string? ip,
        TimeSpan? refreshTtl = null,
        CancellationToken ct = default);
    }
}
