using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAME.Application.Features.Shared.Dungeon;

public abstract class DungeonHandlerSupport
{
    protected const int StaminaRecoveryMinutes = 6;
    protected readonly IUnitOfWork _unitOfWork;
    protected DungeonHandlerSupport(IUnitOfWork unitOfWork)
    {
        this._unitOfWork = unitOfWork;
    }

    protected async Task<HashSet<int>> ClearedStageIds(long playerId, CancellationToken ct) => (await _unitOfWork.ReadOnlyRepository<HrkPlayerDungeonStageProgress>().Query()
        .Where(x => x.PlayerId == playerId)
        .Select(x => x.StageId)
        .ToListAsync(ct)).ToHashSet();
    protected static DungeonMapDto ToMapDto(HrkDungeonMap map, HrkPlayer player, HashSet<int> cleared, IReadOnlyList<HrkDungeonMap> maps)
    {
        var previousCompleted = map.PreviousMapId == null || maps.Single(x => x.Id == map.PreviousMapId).Stages.All(s => cleared.Contains(s.Id));
        var state = !previousCompleted ? "NOT_REACHED" : player.Level < map.RequiredPlayerLevel ? "LEVEL_LOCKED" : map.Stages.All(s => cleared.Contains(s.Id)) ? "COMPLETED" : "AVAILABLE";
        return new DungeonMapDto
        {
            Id = map.Id,
            Code = map.Code,
            Name = map.Name,
            Description = map.Description,
            ImagePath = map.ImagePath,
            BackgroundPath = map.BackgroundPath,
            RequiredPlayerLevel = map.RequiredPlayerLevel,
            TotalStages = map.Stages.Count,
            ClearedStages = map.Stages.Count(s => cleared.Contains(s.Id)),
            State = state
        };
    }

    protected static void ApplyStaminaRecovery(HrkPlayer player)
    {
        if (player.Stamina >= player.MaxStamina)
        {
            player.LastStaminaRegeneratedOn = DateTime.UtcNow;
            return;
        }

        var elapsed = DateTime.UtcNow - player.LastStaminaRegeneratedOn;
        var recovered = (int)(elapsed.TotalMinutes / StaminaRecoveryMinutes);
        if (recovered <= 0)
            return;
        player.Stamina = Math.Min(player.MaxStamina, player.Stamina + recovered);
        player.LastStaminaRegeneratedOn = player.LastStaminaRegeneratedOn.AddMinutes(recovered * StaminaRecoveryMinutes);
    }

    protected static void ResetDailyPurchaseCount(HrkPlayer player)
    {
        if (player.StaminaPurchaseDate?.Date != DateTime.UtcNow.Date)
            player.DailyStaminaPurchaseCount = 0;
    }

    protected static int PurchaseCost(int count) => count switch
    {
        0 => 20,
        1 => 30,
        2 => 50,
        3 => 80,
        _ => 100
    };
    protected static DungeonStaminaDto ToStaminaDto(HrkPlayer player) => new()
    {
        Current = player.Stamina,
        Max = player.MaxStamina,
        PurchaseCount = player.DailyStaminaPurchaseCount,
        NextPurchaseCost = PurchaseCost(player.DailyStaminaPurchaseCount),
        PurchaseAmount = 50,
        NextRecoverySeconds = player.Stamina >= player.MaxStamina ? 0 : Math.Max(0, StaminaRecoveryMinutes * 60 - (int)(DateTime.UtcNow - player.LastStaminaRegeneratedOn).TotalSeconds)
    };
}
