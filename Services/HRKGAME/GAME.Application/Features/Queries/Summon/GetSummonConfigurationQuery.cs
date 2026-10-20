using GAME.Application.Templates;
using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.Common;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Features.Shared.Summon;

namespace GAME.Application.Features.Queries.Summon;

public sealed record GetSummonConfigurationQuery(string UserId) : IQuery<BaseResponse<SummonConfigurationDto>>;

public sealed class GetSummonConfigurationQueryHandler(IUnitOfWork unitOfWork, IGamePlayerService players, IGameTemplateCatalog? templates = null) : SummonHandlerSupport(unitOfWork, templates), IQueryHandler<GetSummonConfigurationQuery, BaseResponse<SummonConfigurationDto>>
{
    private readonly IGamePlayerService _players = players;
    public async Task<BaseResponse<SummonConfigurationDto>> Handle(GetSummonConfigurationQuery request, CancellationToken ct)
    {
        return BaseResponse<SummonConfigurationDto>.SuccessResponse(await ExecuteAsync(request.UserId, ct));
    }

    private async Task<SummonConfigurationDto> ExecuteAsync(string userId, CancellationToken ct)
    {
        var player = await _players.GetPlayerByUserIdAsync(userId, ct) ?? throw new BusinessException("PLAYER_NOT_FOUND", "Không tìm thấy người chơi.", BusinessErrorKind.NotFound);
        var playerId = player.Id;
        var wallet = await _players.GetWalletByPlayerIdAsync(playerId, ct) ?? throw new InvalidOperationException("Không tìm thấy ví người chơi.");
        var pools = (await GetPoolsAsync(ct))
            .Where(x => x.IsEnabled)
            .OrderBy(x => x.DisplayOrder)
            .ToList();
        var result = pools.Select(pool =>
        {
            SummonPolicy.Cost(pool, 1);
            SummonPolicy.Cost(pool, 10);
            var entries = EnabledEntries(pool);
            var total = SummonPolicy.TotalWeight(entries);
            return new SummonPoolDto(pool.Code, pool.Name, pool.Currency, pool.SingleCost, pool.TenCost, entries.Select(x => new SummonHeroDto(x.HeroTemplateId, x.HeroTemplate.Name, x.HeroTemplate.Avatar, x.HeroTemplate.Rarity.Code, x.HeroTemplate.Rarity.Name, x.HeroTemplate.Rarity.ColorHex, Math.Round(100m * x.Weight / total, 6))).ToList());
        }).ToList();
        return new(playerId, new(wallet.Gold, wallet.Diamonds), result);
    }
}
