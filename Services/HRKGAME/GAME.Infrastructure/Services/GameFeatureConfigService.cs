using Core.Common.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Core.Common.Caching;
using Microsoft.EntityFrameworkCore;

namespace GAME.Infrastructure.Services
{
    public class GameFeatureConfigService : IGameFeatureConfigService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cache;
        private const string FeatureConfigCacheKey = "config:features:v1";

        public GameFeatureConfigService(IUnitOfWork unitOfWork, ICacheService cache)
        {
            _unitOfWork = unitOfWork;
            _cache = cache;
        }

        public async Task<List<GameFeatureConfigDto>> GetFeatureTreeAsync(CancellationToken cancellationToken = default)
        {
            var cached = await _cache.GetAsync<List<GameFeatureConfigDto>>(FeatureConfigCacheKey, cancellationToken);
            if (cached != null) return cached;

            var configs = await _unitOfWork.ReadOnlyRepository<HrkGameFeatureConfig>().Query()
                .Where(x => x.IsEnabled)
                .OrderBy(x => x.DisplayOrder)
                .Select(x => new GameFeatureConfigDto
                {
                    Id = x.Id, Code = x.Code, Name = x.Name, Icon = x.Icon,
                    ParentFeatureId = x.ParentFeatureId, Placement = x.Placement,
                    ActionCode = x.ActionCode, DisplayOrder = x.DisplayOrder,
                    IsLocked = x.IsLocked, HasNotification = x.HasNotification
                }).ToListAsync(cancellationToken);

            var byId = configs.ToDictionary(x => x.Id);
            foreach (var config in configs.Where(x => x.ParentFeatureId.HasValue))
                if (byId.TryGetValue(config.ParentFeatureId!.Value, out var parent)) parent.Children.Add(config);

            var result = configs.Where(x => !x.ParentFeatureId.HasValue).ToList();
            await _cache.SetAsync(FeatureConfigCacheKey, result, TimeSpan.FromMinutes(30), cancellationToken);
            return result;
        }
    }
}
