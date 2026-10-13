using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAME.Application.Services;

public sealed class DungeonStarService : IDungeonStarService
{
    private readonly IUnitOfWork _unitOfWork;
    public DungeonStarService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<(int earnedStars, decimal remainingHpRate)> CalculateEarnedStarsAsync(int mapId, StartBattleResultDto battle, CancellationToken cancellationToken = default)
    {
        var victory = battle.Winner.Equals("LEFT", StringComparison.OrdinalIgnoreCase);
        if (!victory)
        {
            return (0, 0m);
        }

        var leftTeam = battle.InitialState.LeftTeam;
        if (leftTeam == null || leftTeam.Count == 0)
        {
            return (1, 0m);
        }

        long totalMaxHp = leftTeam.Sum(h => (long)Math.Max(0, h.Stats.Hp));
        if (totalMaxHp <= 0)
        {
            return (1, 0m);
        }

        // Track final HP of each left team hero from events
        // Combatant ID for left team heroes in simulation is hero.Id
        var finalHpMap = leftTeam.ToDictionary(h => h.Id, h => Math.Max(0, h.Stats.Hp));
        foreach (var evt in battle.Events)
        {
            if (evt.TargetId.HasValue && finalHpMap.ContainsKey(evt.TargetId.Value) && evt.HpAfter.HasValue)
            {
                finalHpMap[evt.TargetId.Value] = Math.Max(0, evt.HpAfter.Value);
            }
        }

        long totalCurrentHp = finalHpMap.Values.Where(hp => hp > 0).Sum();
        decimal remainingHpRate = Math.Clamp((decimal)totalCurrentHp / totalMaxHp, 0m, 1m);
        var configs = await _unitOfWork.ReadOnlyRepository<HrkDungeonStarRatingConfig>().Query()
            .Where(x => x.IsActive && (x.DungeonMapId == mapId || x.DungeonMapId == null))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var mapConfigs = configs.Where(x => x.DungeonMapId == mapId).ToList();
        var effectiveConfigs = mapConfigs.Count > 0 ? mapConfigs : configs.Where(x => x.DungeonMapId == null).ToList();
        if (effectiveConfigs.Count == 0)
        {
            throw new InvalidOperationException("Chưa cấu hình ngưỡng xếp sao phó bản.");
        }

        int earnedStars = effectiveConfigs.Where(x => remainingHpRate >= x.MinRemainingHpRate)
            .OrderByDescending(x => x.MinRemainingHpRate).ThenByDescending(x => x.Stars)
            .Select(x => x.Stars).FirstOrDefault();
        if (earnedStars <= 0)
        {
            earnedStars = effectiveConfigs.Min(x => x.Stars);
        }

        return (earnedStars, remainingHpRate);
    }

    public async Task<int> GetMapTotalStarsAsync(long playerId, int mapId, CancellationToken cancellationToken = default)
    {
        var stageIds = await _unitOfWork.ReadOnlyRepository<HrkDungeonStage>().Query()
            .Where(x => x.DungeonMapId == mapId && x.IsActive)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
        if (stageIds.Count == 0)
            return 0;
        return await _unitOfWork.ReadOnlyRepository<HrkPlayerDungeonStageProgress>().Query()
            .Where(x => x.PlayerId == playerId && stageIds.Contains(x.StageId)).SumAsync(x => x.BestStars, cancellationToken);
    }

    public async Task<Dictionary<int, int>> GetStageBestStarsAsync(long playerId, IEnumerable<int> stageIds, CancellationToken cancellationToken = default)
    {
        var idList = stageIds.Distinct().ToList();
        if (idList.Count == 0)
            return new Dictionary<int, int>();
        var progresses = await _unitOfWork.ReadOnlyRepository<HrkPlayerDungeonStageProgress>().Query()
            .Where(x => x.PlayerId == playerId && idList.Contains(x.StageId))
            .Select(x => new { x.StageId, x.BestStars })
            .ToListAsync(cancellationToken);
        return progresses.ToDictionary(x => x.StageId, x => x.BestStars);
    }
}
