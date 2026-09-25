using AUTH.Application.Interfaces;
using AUTH.Domain.Entities;
using AUTH.Infrastructure.Data;
using Core.Common.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AUTH.Infrastructure.Services;

public sealed class ApplicationRouteConfigService : IApplicationRouteConfigService
{
    private readonly IUnitOfWork _unitOfWork;

    public ApplicationRouteConfigService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<HRK_ApplicationRouteConfig>> GetActiveAsync(
        CancellationToken cancellationToken = default) =>
        await _unitOfWork.ReadOnlyRepository<HRK_ApplicationRouteConfig>()
            .Query()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

    public Task<bool> IsAudienceAllowedAsync(
        string audience,
        CancellationToken cancellationToken = default) =>
        _unitOfWork.ReadOnlyRepository<HRK_ApplicationRouteConfig>()
            .Query()
            .AnyAsync(x => x.IsActive && x.Audience == audience, cancellationToken);
}
