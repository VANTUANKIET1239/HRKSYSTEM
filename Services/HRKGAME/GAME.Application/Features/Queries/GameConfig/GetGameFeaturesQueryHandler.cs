using Core.Persistence.Repositories;
using GAME.Application.Templates;
using GAME.Application.DTOs;
using GAME.Domain.Entities;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;

namespace GAME.Application.Features.Queries.GameConfig;

public sealed class GetGameFeaturesQueryHandler(IUnitOfWork unitOfWork, IGameTemplateCatalog? templates = null) : IQueryHandler<GetGameFeaturesQuery, BaseResponse<List<GameFeatureConfigDto>>>
{
    private readonly IGameTemplateCatalog _templates = templates ?? new DatabaseTemplateCatalog(unitOfWork);
    public async Task<BaseResponse<List<GameFeatureConfigDto>>> Handle(GetGameFeaturesQuery message, CancellationToken ct) => BaseResponse<List<GameFeatureConfigDto>>.SuccessResponse(await ExecuteAsync(ct));
    private async Task<List<GameFeatureConfigDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var configs = (await _templates.GetAllAsync<HrkGameFeatureConfig>(cancellationToken))
            .Where(x => x.IsEnabled)
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new GameFeatureConfigDto { Id = x.Id, Code = x.Code, Name = x.Name, Icon = x.Icon, ParentFeatureId = x.ParentFeatureId, Placement = x.Placement, ActionCode = x.ActionCode, DisplayOrder = x.DisplayOrder, IsLocked = x.IsLocked, HasNotification = x.HasNotification })
            .ToList();
        var byId = configs.ToDictionary(x => x.Id);
        foreach (var config in configs.Where(x => x.ParentFeatureId.HasValue))
            if (byId.TryGetValue(config.ParentFeatureId!.Value, out var parent))
                parent.Children.Add(config);
        var result = configs.Where(x => !x.ParentFeatureId.HasValue).ToList();
        return result;
    }
}
