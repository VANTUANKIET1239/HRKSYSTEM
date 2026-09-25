using Core.Common.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAME.Infrastructure.Services;

public sealed class DungeonStarService : IDungeonStarService
{
    private readonly IUnitOfWork _unitOfWork;

    public DungeonStarService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<(int earnedStars, decimal remainingHpRate)> CalculateEarnedStarsAsync(
        int mapId, StartBattleResultDto battle, CancellationToken cancellationToken = default)
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
        var effectiveConfigs = mapConfigs.Count > 0
            ? mapConfigs
            : configs.Where(x => x.DungeonMapId == null).ToList();

        if (effectiveConfigs.Count == 0)
        {
            throw new InvalidOperationException("Chưa cấu hình ngưỡng xếp sao phó bản.");
        }

        int earnedStars = effectiveConfigs
            .Where(x => remainingHpRate >= x.MinRemainingHpRate)
            .OrderByDescending(x => x.MinRemainingHpRate)
            .ThenByDescending(x => x.Stars)
            .Select(x => x.Stars)
            .FirstOrDefault();

        if (earnedStars <= 0)
        {
            earnedStars = effectiveConfigs.Min(x => x.Stars);
        }

        return (earnedStars, remainingHpRate);
    }

    public async Task<(int earnedStars, int previousBestStars, int bestStars, bool isNewRecord, decimal remainingHpRate)> RecordStageProgressAsync(
        long playerId, int stageId, StartBattleResultDto battle, CancellationToken cancellationToken = default)
    {
        var mapId = await _unitOfWork.ReadOnlyRepository<HrkDungeonStage>().Query()
            .Where(x => x.Id == stageId)
            .Select(x => x.DungeonMapId)
            .SingleAsync(cancellationToken);
        var (earnedStars, remainingHpRate) = await CalculateEarnedStarsAsync(mapId, battle, cancellationToken);

        var progress = await _unitOfWork.Repository<HrkPlayerDungeonStageProgress>().Query()
            .SingleOrDefaultAsync(x => x.PlayerId == playerId && x.StageId == stageId, cancellationToken);

        int battleTurns = battle.Events.Count > 0 ? battle.Events.Max(x => x.Turn) : 1;
        int previousBestStars = progress?.BestStars ?? 0;
        bool isNewRecord = earnedStars > previousBestStars;

        if (progress == null)
        {
            progress = new HrkPlayerDungeonStageProgress
            {
                PlayerId = playerId,
                StageId = stageId,
                ClearCount = 1,
                BestTurns = battleTurns,
                BestStars = earnedStars,
                BestRemainingHpRate = remainingHpRate,
                FirstClearedOn = DateTime.UtcNow,
                LastClearedOn = DateTime.UtcNow
            };
            await _unitOfWork.Repository<HrkPlayerDungeonStageProgress>().AddAsync(progress);
        }
        else
        {
            progress.ClearCount++;
            progress.LastClearedOn = DateTime.UtcNow;
            progress.BestTurns = Math.Min(progress.BestTurns > 0 ? progress.BestTurns : battleTurns, battleTurns);
            if (isNewRecord)
            {
                progress.BestStars = earnedStars;
            }
            if (remainingHpRate > progress.BestRemainingHpRate)
            {
                progress.BestRemainingHpRate = remainingHpRate;
            }
        }

        return (earnedStars, previousBestStars, progress.BestStars, isNewRecord, remainingHpRate);
    }

    public async Task<int> GetMapTotalStarsAsync(long playerId, int mapId, CancellationToken cancellationToken = default)
    {
        var stageIds = await _unitOfWork.ReadOnlyRepository<HrkDungeonStage>().Query()
            .Where(x => x.DungeonMapId == mapId && x.IsActive)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (stageIds.Count == 0) return 0;

        return await _unitOfWork.ReadOnlyRepository<HrkPlayerDungeonStageProgress>().Query()
            .Where(x => x.PlayerId == playerId && stageIds.Contains(x.StageId))
            .SumAsync(x => x.BestStars, cancellationToken);
    }

    public async Task<Dictionary<int, int>> GetStageBestStarsAsync(long playerId, IEnumerable<int> stageIds, CancellationToken cancellationToken = default)
    {
        var idList = stageIds.Distinct().ToList();
        if (idList.Count == 0) return new Dictionary<int, int>();

        var progresses = await _unitOfWork.ReadOnlyRepository<HrkPlayerDungeonStageProgress>().Query()
            .Where(x => x.PlayerId == playerId && idList.Contains(x.StageId))
            .Select(x => new { x.StageId, x.BestStars })
            .ToListAsync(cancellationToken);

        return progresses.ToDictionary(x => x.StageId, x => x.BestStars);
    }
}
