using GAME.Application.Templates;
using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.Common;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Services;
using GAME.Application.Features.Shared.Summon;

namespace GAME.Application.Features.Commands.Summon;

public sealed class SummonCommandHandler(
    IUnitOfWork unitOfWork,
    IGamePlayerService players,
    ISummonRandom random,
    SummonRewardGranter granter, IDiamondSpendingService spending, IGameTransactionLock locks, IGameTemplateCatalog? templates = null)
    : SummonHandlerSupport(unitOfWork, templates), ICommandHandler<SummonCommand, BaseResponse<SummonResultDto>>
{
    private readonly IGamePlayerService _players = players;
    private readonly ISummonRandom _random = random;
    private readonly SummonRewardGranter _granter = granter;
    public async Task<BaseResponse<SummonResultDto>> Handle(SummonCommand request, CancellationToken ct)
    {
        return BaseResponse<SummonResultDto>.SuccessResponse(await ExecuteAsync(request.UserId, request.Request, ct));
    }

    private async Task<SummonResultDto> ExecuteAsync(string userId, SummonRequestDto request, CancellationToken ct)
    {
        SummonPolicy.ValidateRequest(request.RequestId, request.PoolCode, request.Count);
        var code = request.PoolCode.Trim().ToUpperInvariant();
        SummonResultDto? result = null;
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            _unitOfWork.ClearTracking();
            var player = await _players.GetPlayerByUserIdAsync(userId, ct) ?? throw new BusinessException("PLAYER_NOT_FOUND", "Không tìm thấy người chơi.", BusinessErrorKind.NotFound);
            var playerId = player.Id;
            // Serializes acquisition and duplicate checks per player across API instances.
            // RowVersion also rejects stale writes from other wallet-consuming features.
            await locks.LockWalletAsync(playerId, ct);
            var wallet = await _players.GetWalletByPlayerIdAsync(playerId, ct) ?? throw new BusinessException("PLAYER_WALLET_NOT_FOUND", "Không tìm thấy ví người chơi.", BusinessErrorKind.NotFound);
            var previous = await _unitOfWork.ReadOnlyRepository<HrkSummonBatch>().Query()
                .SingleOrDefaultAsync(x => x.PlayerId == playerId && x.RequestId == request.RequestId, ct);
            if (previous != null)
            {
                if (previous.PoolCode != code || previous.Count != request.Count)
                    throw new BusinessException("SUMMON_REQUEST_CONFLICT", "Mã yêu cầu đã được dùng cho lượt chiêu mộ khác.", BusinessErrorKind.Conflict);
                result = ReadResult(previous);
                return;
            }

            var pool = (await GetPoolsAsync(ct))
                .SingleOrDefault(x => x.Code == code && x.IsEnabled) ?? throw new BusinessException("SUMMON_POOL_UNAVAILABLE", "Rương chiêu mộ hiện không khả dụng.", BusinessErrorKind.Conflict);
            var entries = EnabledEntries(pool);
            var total = SummonPolicy.TotalWeight(entries);
            var cost = SummonPolicy.Cost(pool, request.Count);
            if (pool.Currency == "GOLD")
                wallet.DeductGold(cost);
            else
            {
                if (!await spending.SpendAsync(wallet, cost, "SUMMON", code, request.RequestId, ct))
                    throw new InvalidOperationException("A summon spend receipt exists without its batch.");
            }

            wallet.UpdatedOn = DateTime.UtcNow;
            await _granter.EnsureReadyAsync(playerId, entries.Select(x => x.HeroTemplate).ToList(), request.Count, wallet.MaxCapacity, ct);
            var heroes = Enumerable.Range(0, request.Count)
                .Select(_ => SummonPolicy.Select(entries, _random.Next(total)).HeroTemplate).ToList();
            var rewards = await _granter.GrantAsync(playerId, heroes, request.RequestId, wallet.MaxCapacity, ct);
            result = new(request.RequestId, code, request.Count, pool.Currency, cost, new(wallet.Gold, wallet.Diamonds), rewards);
            await _unitOfWork.Repository<HrkSummonBatch>().AddAsync(new HrkSummonBatch { PlayerId = playerId, RequestId = request.RequestId, PoolCode = code, Count = request.Count, Currency = pool.Currency, Cost = cost, ResultJson = JsonSerializer.Serialize(result), Items = rewards.Select(x => new HrkSummonResultItem { DrawIndex = x.DrawIndex, HeroTemplateId = x.HeroTemplateId, WasDuplicate = !x.IsNew, ConvertedStoneQuantity = x.StoneQuantity }).ToList() });
            // ExecuteInTransactionAsync owns the single final save and commit.
        }, ct);
        return result ?? throw new InvalidOperationException("Summon transaction did not produce a result.");
    }
}
