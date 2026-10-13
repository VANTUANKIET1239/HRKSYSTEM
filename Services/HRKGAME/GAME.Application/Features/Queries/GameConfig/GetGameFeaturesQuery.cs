using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Domain.Entities;
using Core.Caching;
using Microsoft.EntityFrameworkCore;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;

namespace GAME.Application.Features.Queries.GameConfig;

public sealed record GetGameFeaturesQuery() : IQuery<BaseResponse<List<GameFeatureConfigDto>>>;

public sealed class GetGameFeaturesQueryHandler(IUnitOfWork unitOfWork, ICacheService cache) : IQueryHandler<GetGameFeaturesQuery, BaseResponse<List<GameFeatureConfigDto>>>
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ICacheService _cache = cache;
    private const string FeatureConfigCacheKey = "config:features:v1";
    public async Task<BaseResponse<List<GameFeatureConfigDto>>> Handle(GetGameFeaturesQuery message, CancellationToken ct) => BaseResponse<List<GameFeatureConfigDto>>.SuccessResponse(await ExecuteAsync(ct));
    private async Task<List<GameFeatureConfigDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var cached = await _cache.GetAsync<List<GameFeatureConfigDto>>(FeatureConfigCacheKey, cancellationToken);
        if (cached != null)
            return cached;
        var configs = await _unitOfWork.ReadOnlyRepository<HrkGameFeatureConfig>().Query()
            .Where(x => x.IsEnabled)
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new GameFeatureConfigDto { Id = x.Id, Code = x.Code, Name = x.Name, Icon = x.Icon, ParentFeatureId = x.ParentFeatureId, Placement = x.Placement, ActionCode = x.ActionCode, DisplayOrder = x.DisplayOrder, IsLocked = x.IsLocked, HasNotification = x.HasNotification })
            .ToListAsync(cancellationToken);
        var byId = configs.ToDictionary(x => x.Id);
        foreach (var config in configs.Where(x => x.ParentFeatureId.HasValue))
            if (byId.TryGetValue(config.ParentFeatureId!.Value, out var parent))
                parent.Children.Add(config);
        var result = configs.Where(x => !x.ParentFeatureId.HasValue).ToList();
        await _cache.SetAsync(FeatureConfigCacheKey, result, TimeSpan.FromMinutes(30), cancellationToken);
        return result;
    }
}
