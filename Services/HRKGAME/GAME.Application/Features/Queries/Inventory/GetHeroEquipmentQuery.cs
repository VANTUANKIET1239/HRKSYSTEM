using Core.Persistence.Repositories;
using GAME.Application.Common.Mappings;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Services;

namespace GAME.Application.Features.Queries.Inventory;

public record GetHeroEquipmentQuery(long HeroId) : IQuery<BaseResponse<HeroEquipmentDto>>;

public sealed class GetHeroEquipmentQueryHandler(
    IUnitOfWork unitOfWork,
    IGamePlayerService gamePlayerService,
    IGameRequestContext actor)
    : IQueryHandler<GetHeroEquipmentQuery, BaseResponse<HeroEquipmentDto>>
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IGamePlayerService _gamePlayerService = gamePlayerService;
    private readonly IGameRequestContext _actor = actor;
    public async Task<BaseResponse<HeroEquipmentDto>> Handle(GetHeroEquipmentQuery request, CancellationToken cancellationToken)
    {
        var userId = _actor.UserId;
        var eq = await ExecuteAsync(userId, request.HeroId, cancellationToken);
        return BaseResponse<HeroEquipmentDto>.SuccessResponse(eq, "Retrieved hero equipment successfully.");
    }

    private async Task<HeroEquipmentDto> ExecuteAsync(string userId, long heroId, CancellationToken cancellationToken = default)
    {
        var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, cancellationToken);
        if (player == null)
        {
            return new HeroEquipmentDto
            {
                HeroId = heroId
            };
        }

        var equipments = await EquipmentBatchLoader.LoadForHeroesAsync(_unitOfWork, player.Id, new[] { heroId }, cancellationToken);
        equipments.TryGetValue(heroId, out var eq);
        return GameDtoMapper.MapHeroEquipment(eq, heroId);
    }
}
