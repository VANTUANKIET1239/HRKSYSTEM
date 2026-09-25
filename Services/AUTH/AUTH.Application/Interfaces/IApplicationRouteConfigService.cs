using AUTH.Domain.Entities;

namespace AUTH.Application.Interfaces;

public interface IApplicationRouteConfigService
{
    Task<IReadOnlyList<HRK_ApplicationRouteConfig>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<bool> IsAudienceAllowedAsync(string audience, CancellationToken cancellationToken = default);
}
