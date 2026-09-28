using Core.Common.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAME.Infrastructure.Services;

public sealed class DungeonService : IDungeonService
{
    private const int StaminaRecoveryMinutes = 6;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBattleService _battleService;
    private readonly IDungeonStarService _starService;
    private readonly IDungeonRewardService _rewardService;
    private readonly IDungeonChestService _chestService;
    private readonly IFormationSnapshotService _formationSnapshotService;
    private readonly IFormationPowerQueryService _formationPowerQueryService;
    private readonly ILevelExperienceService _levelExperienceService;

    public DungeonService(
        IUnitOfWork unitOfWork,
        IBattleService battleService,
        IDungeonStarService starService,
        IDungeonRewardService rewardService,
        IDungeonChestService chestService,
        IFormationSnapshotService formationSnapshotService,
        IFormationPowerQueryService formationPowerQueryService,
        ILevelExperienceService levelExperienceService)
    {
        _unitOfWork = unitOfWork;
        _battleService = battleService;
        _starService = starService;
        _rewardService = rewardService;
        _chestService = chestService;
        _formationSnapshotService = formationSnapshotService;
        _formationPowerQueryService = formationPowerQueryService;
        _levelExperienceService = levelExperienceService;
    }

    public async Task<List<DungeonMapDto>> GetMapsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var player = await GetPlayerAsync(userId, cancellationToken);
        ApplyStaminaRecovery(player);
        ResetDailyPurchaseCount(player);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var maps = await _unitOfWork.ReadOnlyRepository<HrkDungeonMap>().Query()
            .Where(x => x.IsActive).Include(x => x.Stages).OrderBy(x => x.DisplayOrder)
            .AsNoTracking().ToListAsync(cancellationToken);
        var cleared = await ClearedStageIds(player.Id, cancellationToken);
        return maps.Select(map => ToMapDto(map, player, cleared, maps)).ToList();
    }

    public async Task<DungeonMapDetailDto> GetMapAsync(string userId, int mapId, CancellationToken cancellationToken = default)
    {
        var player = await GetPlayerAsync(userId, cancellationToken);
        // Load lightweight map progression data separately from the selected map.
        // The previous implementation loaded enemies for every map before filtering by mapId.
        var maps = await _unitOfWork.ReadOnlyRepository<HrkDungeonMap>().Query()
            .Where(x => x.IsActive)
            .Include(x => x.Stages.Where(s => s.IsActive))
            .OrderBy(x => x.DisplayOrder)
            .AsSplitQuery()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (!maps.Any(x => x.Id == mapId))
            throw new KeyNotFoundException("Không tìm thấy bản đồ phó bản.");

        var map = await _unitOfWork.ReadOnlyRepository<HrkDungeonMap>().Query()
            .Where(x => x.Id == mapId && x.IsActive)
            .Include(x => x.Stages.Where(s => s.IsActive))
                .ThenInclude(s => s.Enemies)
                .ThenInclude(e => e.HeroTemplate)
            .AsSplitQuery()
            .AsNoTracking()
            .SingleAsync(cancellationToken);
        var cleared = await ClearedStageIds(player.Id, cancellationToken);
        var playerPower = await GetSelectedFormationPower(player.Id, cancellationToken);
        var baseDto = ToMapDto(map, player, cleared, maps);

        int totalStars = await _starService.GetMapTotalStarsAsync(player.Id, map.Id, cancellationToken);
        var starChests = await _chestService.GetMapChestsAsync(player.Id, map.Id, totalStars, cancellationToken);
        var stageBestStars = await _starService.GetStageBestStarsAsync(player.Id, map.Stages.Select(s => s.Id), cancellationToken);

        var stageIds = map.Stages.Select(s => s.Id).ToList();
        var stagePossibleDrops = await _rewardService.GetPossibleDropsForStagesAsync(stageIds, cancellationToken);

        return new DungeonMapDetailDto
        {
            Id = baseDto.Id,
            Code = baseDto.Code,
            Name = baseDto.Name,
            Description = baseDto.Description,
            ImagePath = baseDto.ImagePath,
            BackgroundPath = baseDto.BackgroundPath,
            RequiredPlayerLevel = baseDto.RequiredPlayerLevel,
            ClearedStages = baseDto.ClearedStages,
            TotalStages = baseDto.TotalStages,
            State = baseDto.State,
            TotalStars = totalStars,
            StarChests = starChests,
            Stages = map.Stages.OrderBy(x => x.StageNumber)
                .Select(stage => ToStageDto(
                    stage,
                    map,
                    cleared,
                    baseDto.State is "AVAILABLE" or "COMPLETED",
                    playerPower,
                    stageBestStars.GetValueOrDefault(stage.Id, 0),
                    stagePossibleDrops.GetValueOrDefault(stage.Id, new List<DungeonPossibleDropDto>())))
                .ToList()
        };
    }

    public async Task<DungeonStaminaDto> GetStaminaAsync(string userId, CancellationToken cancellationToken = default)
    {
        var player = await GetPlayerAsync(userId, cancellationToken);
        ApplyStaminaRecovery(player);
        ResetDailyPurchaseCount(player);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ToStaminaDto(player);
    }

    public async Task<DungeonStaminaDto> PurchaseStaminaAsync(string userId, CancellationToken cancellationToken = default)
    {
        var player = await GetPlayerAsync(userId, cancellationToken);
        ApplyStaminaRecovery(player);
        ResetDailyPurchaseCount(player);
        if (player.DailyStaminaPurchaseCount >= 10) throw new InvalidOperationException("Đã đạt giới hạn mua thể lực hôm nay.");
        var wallet = await _unitOfWork.Repository<HrkPlayerWallet>().Query()
            .SingleAsync(x => x.PlayerId == player.Id, cancellationToken);
        var cost = PurchaseCost(player.DailyStaminaPurchaseCount);
        if (wallet.Diamonds < cost) throw new InvalidOperationException($"Không đủ kim cương. Cần {cost} kim cương.");
        wallet.Diamonds -= cost;
        wallet.UpdatedOn = DateTime.UtcNow;
        player.Stamina += 50;
        player.DailyStaminaPurchaseCount++;
        player.StaminaPurchaseDate = DateTime.UtcNow.Date;
        player.UpdatedOn = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ToStaminaDto(player);
    }

    public async Task<ClaimStarChestResultDto> ClaimStarChestAsync(string userId, int chestId, CancellationToken cancellationToken = default)
    {
        return await _chestService.ClaimStarChestAsync(userId, chestId, cancellationToken);
    }

    public async Task<StartDungeonStageResultDto> StartStageAsync(string userId, int stageId, StartDungeonStageRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ClientRequestId)) throw new ArgumentException("ClientRequestId là bắt buộc.");
        var player = await GetPlayerAsync(userId, cancellationToken);
        ApplyStaminaRecovery(player);
        var duplicate = await _unitOfWork.ReadOnlyRepository<HrkDungeonRun>().Query()
            .AnyAsync(x => x.PlayerId == player.Id && x.ClientRequestId == request.ClientRequestId, cancellationToken);
        if (duplicate) throw new InvalidOperationException("Yêu cầu chiến đấu này đã được xử lý.");

        var stage = await _unitOfWork.ReadOnlyRepository<HrkDungeonStage>().Query()
            .Include(x => x.DungeonMap).Include(x => x.Enemies).AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == stageId && x.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy màn phó bản.");
        var cleared = await ClearedStageIds(player.Id, cancellationToken);
        await ValidateStageAccess(stage, player, cleared, cancellationToken);
        if (player.Stamina < stage.StaminaCost) throw new InvalidOperationException("Không đủ thể lực để vào phó bản.");

        // Check if inventory is full before battle
        bool isBagFullBeforeBattle = await _rewardService.IsBagFullAsync(player.Id, cancellationToken);
        if (isBagFullBeforeBattle)
        {
            throw new InvalidOperationException("Hành trang đã đầy. Vui lòng dọn dẹp hoặc mở rộng hành trang trước khi vào phó bản.");
        }

        // Build snapshot from formation draft (validates formation, slots, hero ownership)
        var snapshot = await _formationSnapshotService.BuildAsync(
            player.Id,
            request.FormationCode,
            request.Positions,
            cancellationToken);

        if (snapshot.Heroes.Count == 0)
        {
            throw new InvalidOperationException("Đội hình xuất chiến không có võ tướng nào.");
        }

        StartDungeonStageResultDto? completedResult = null;
        await _unitOfWork.ExecuteStrategyAsync(async () =>
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var battle = await _battleService.StartBattleAsync(userId, new StartBattleRequestDto
                {
                    BattleType = "CAMPAIGN",
                    StageId = stage.Id,
                    FormationCode = request.FormationCode,
                    Positions = request.Positions
                }, cancellationToken);

                player.Stamina -= stage.StaminaCost;
                var victory = battle.Winner.Equals("LEFT", StringComparison.OrdinalIgnoreCase);
                var firstClear = victory && !cleared.Contains(stage.Id);

                var result = new DungeonResultDto
                {
                    Result = victory ? "VICTORY" : "DEFEAT",
                    StaminaSpent = stage.StaminaCost,
                    StaminaRemaining = player.Stamina,
                    IsFirstClear = firstClear,
                    OldPlayerLevel = player.Level,
                    OldPlayerExp = player.Exp
                };

                if (victory)
                {
                    var wallet = await _unitOfWork.Repository<HrkPlayerWallet>().Query().SingleAsync(x => x.PlayerId == player.Id, cancellationToken);
                    result.GoldGained = stage.GoldReward + (firstClear ? stage.FirstClearGoldReward : 0);
                    wallet.Gold += result.GoldGained;
                    wallet.UpdatedOn = DateTime.UtcNow;
                    result.PlayerExpGained = stage.PlayerExpReward;
                    await AddPlayerExpAsync(player, stage.PlayerExpReward, cancellationToken);
                    result.NewPlayerLevel = player.Level;
                    result.NewPlayerExp = player.Exp;
                    result.Heroes = await AddParticipantHeroExp(player.Id, snapshot.ParticipantHeroIds, stage.HeroExpReward, cancellationToken);

                    // 1. Calculate & record stage stars (non-regressive)
                    var (earnedStars, prevBestStars, bestStars, isNewRecord, remainingHpRate) =
                        await _starService.RecordStageProgressAsync(player.Id, stage.Id, battle, cancellationToken);

                    result.EarnedStars = earnedStars;
                    result.PreviousBestStars = prevBestStars;
                    result.BestStars = bestStars;
                    result.IsNewStarRecord = isNewRecord;
                    result.RemainingHpRate = remainingHpRate;

                    // 2. Roll boss equipment drop
                    var (droppedEquip, bagFullDuringDrop) =
                        await _rewardService.RollEquipmentDropAsync(player.Id, stage.Id, firstClear, cancellationToken);

                    result.DroppedEquipment = droppedEquip;
                    result.IsBagFull = bagFullDuringDrop;

                    // 3. Map total stars
                    result.TotalMapStars = await _starService.GetMapTotalStarsAsync(player.Id, stage.DungeonMapId, cancellationToken);

                    result.UnlockedStageId = await FindNextStageId(stage, cancellationToken);
                }
                else
                {
                    result.NewPlayerLevel = player.Level;
                    result.NewPlayerExp = player.Exp;
                    result.EarnedStars = 0;
                    result.BestStars = (await _starService.GetStageBestStarsAsync(player.Id, new[] { stage.Id }, cancellationToken))
                        .GetValueOrDefault(stage.Id, 0);
                    result.PreviousBestStars = result.BestStars;
                    result.TotalMapStars = await _starService.GetMapTotalStarsAsync(player.Id, stage.DungeonMapId, cancellationToken);
                }

                var run = new HrkDungeonRun
                {
                    BattleId = battle.BattleId,
                    ClientRequestId = request.ClientRequestId,
                    PlayerId = player.Id,
                    StageId = stage.Id,
                    Result = result.Result,
                    StaminaSpent = stage.StaminaCost,
                    PlayerPower = snapshot.TotalPower,
                    EnemyPower = battle.InitialState.RightTeam.Sum(x => x.Power),
                    FormationCode = snapshot.FormationCode,
                    FormationSnapshotJson = System.Text.Json.JsonSerializer.Serialize(snapshot),
                    ParticipantHeroIdsJson = System.Text.Json.JsonSerializer.Serialize(snapshot.ParticipantHeroIds),
                    RandomSeed = battle.RandomSeed,
                    GoldReward = result.GoldGained,
                    PlayerExpReward = result.PlayerExpGained,
                    HeroExpReward = victory ? stage.HeroExpReward : 0
                };
                await _unitOfWork.Repository<HrkDungeonRun>().AddAsync(run);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                result.RunId = run.Id;
                await _unitOfWork.CommitTransactionAsync();
                completedResult = new StartDungeonStageResultDto { Battle = battle, Result = result };
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        });

        return completedResult ?? throw new InvalidOperationException("Dungeon battle transaction did not complete.");
    }

    private async Task<HrkPlayer> GetPlayerAsync(string userId, CancellationToken ct) =>
        await _unitOfWork.Repository<HrkPlayer>().Query().SingleOrDefaultAsync(x => x.UserId == userId && x.IsActive, ct)
        ?? throw new KeyNotFoundException("Không tìm thấy người chơi.");

    private async Task<HashSet<int>> ClearedStageIds(long playerId, CancellationToken ct) =>
        (await _unitOfWork.ReadOnlyRepository<HrkPlayerDungeonStageProgress>().Query()
            .Where(x => x.PlayerId == playerId).Select(x => x.StageId).ToListAsync(ct)).ToHashSet();

    private static DungeonMapDto ToMapDto(HrkDungeonMap map, HrkPlayer player, HashSet<int> cleared, IReadOnlyList<HrkDungeonMap> maps)
    {
        var previousCompleted = map.PreviousMapId == null || maps.Single(x => x.Id == map.PreviousMapId).Stages.All(s => cleared.Contains(s.Id));
        var state = !previousCompleted ? "NOT_REACHED" : player.Level < map.RequiredPlayerLevel ? "LEVEL_LOCKED" :
            map.Stages.All(s => cleared.Contains(s.Id)) ? "COMPLETED" : "AVAILABLE";
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

    private static DungeonStageDto ToStageDto(
        HrkDungeonStage stage,
        HrkDungeonMap map,
        HashSet<int> cleared,
        bool mapAccessible,
        int playerPower,
        int bestStars,
        List<DungeonPossibleDropDto> possibleDrops)
    {
        var previous = map.Stages.SingleOrDefault(x => x.StageNumber == stage.StageNumber - 1);
        var available = stage.StageNumber == 1 || previous != null && cleared.Contains(previous.Id);
        var state = cleared.Contains(stage.Id) ? "CLEARED" : mapAccessible && available ? "AVAILABLE" : "LOCKED";
        var enemies = stage.Enemies.OrderBy(x => x.Position).Select(enemy => new DungeonEnemyDto
        {
            Position = enemy.Position,
            Name = enemy.DisplayName ?? enemy.HeroTemplate.Name,
            ImagePath = enemy.ImagePath ?? enemy.HeroTemplate.Avatar,
            Level = enemy.Level,
            Stars = enemy.Stars,
            IsBoss = enemy.IsBoss,
            Power = (int)Math.Round((enemy.HeroTemplate.BaseHp * .25m + enemy.HeroTemplate.BaseAtk * 3.5m + enemy.HeroTemplate.BaseDef * 2m + enemy.HeroTemplate.BaseSpd) * enemy.StatMultiplier)
        }).ToList();

        return new DungeonStageDto
        {
            Id = stage.Id,
            StageNumber = stage.StageNumber,
            Name = stage.Name,
            StageType = stage.StageType,
            State = state,
            BestStars = bestStars,
            StaminaCost = stage.StaminaCost,
            RecommendedPower = stage.RecommendedPower,
            PlayerPower = playerPower,
            EnemyPower = enemies.Sum(x => x.Power),
            GoldReward = stage.GoldReward,
            FirstClearGoldReward = stage.FirstClearGoldReward,
            PlayerExpReward = stage.PlayerExpReward,
            HeroExpReward = stage.HeroExpReward,
            BackgroundPath = stage.BackgroundPath,
            Enemies = enemies,
            PossibleDrops = possibleDrops
        };
    }

    private async Task<int> GetSelectedFormationPower(long playerId, CancellationToken ct)
    {
        var result = await _formationPowerQueryService.GetDefaultFormationPowerAsync(playerId, ct);
        return result.TotalPower;
    }

    private async Task ValidateStageAccess(HrkDungeonStage stage, HrkPlayer player, HashSet<int> cleared, CancellationToken ct)
    {
        if (player.Level < stage.DungeonMap.RequiredPlayerLevel) throw new InvalidOperationException($"Cần đạt cấp {stage.DungeonMap.RequiredPlayerLevel}.");
        if (stage.StageNumber > 1)
        {
            var previousStageId = await _unitOfWork.ReadOnlyRepository<HrkDungeonStage>().Query()
                .Where(x => x.DungeonMapId == stage.DungeonMapId && x.StageNumber == stage.StageNumber - 1)
                .Select(x => x.Id).SingleAsync(ct);
            if (!cleared.Contains(previousStageId)) throw new InvalidOperationException("Bạn chưa vượt qua màn trước.");
        }
        if (stage.DungeonMap.PreviousMapId.HasValue && stage.StageNumber == 1)
        {
            var previousBossId = await _unitOfWork.ReadOnlyRepository<HrkDungeonStage>().Query()
                .Where(x => x.DungeonMapId == stage.DungeonMap.PreviousMapId.Value)
                .OrderByDescending(x => x.StageNumber).Select(x => x.Id).FirstAsync(ct);
            if (!cleared.Contains(previousBossId)) throw new InvalidOperationException("Bạn chưa hoàn thành bản đồ trước.");
        }
    }

    private async Task<List<HeroExpResultDto>> AddParticipantHeroExp(long playerId, IReadOnlyCollection<long> participantHeroIds, int exp, CancellationToken ct)
    {
        if (participantHeroIds == null || participantHeroIds.Count == 0) return new();
        var heroes = await _unitOfWork.Repository<HrkPlayerHero>().Query()
            .Include(x => x.HeroTemplate)
            .Where(x => x.PlayerId == playerId && participantHeroIds.Contains(x.Id))
            .ToListAsync(ct);

        var results = new List<HeroExpResultDto>();
        foreach (var hero in heroes)
        {
            var oldMaxExp = hero.MaxExp;
            var item = new HeroExpResultDto
            {
                PlayerHeroId = hero.Id,
                HeroName = hero.HeroTemplate.Name,
                Avatar = hero.HeroTemplate.Avatar ?? string.Empty,
                ExpGained = exp,
                OldLevel = hero.Level,
                OldExp = hero.Exp,
                OldMaxExp = oldMaxExp
            };
            var requirement = await _levelExperienceService.GetHeroRequirementAsync(hero.Level, ct);
            hero.MaxExp = requirement.IsMaxLevel ? 0 : requirement.RequiredExp;
            if (!requirement.IsMaxLevel) hero.Exp += exp;

            while (!requirement.IsMaxLevel && hero.MaxExp > 0 && hero.Exp >= hero.MaxExp)
            {
                hero.Exp -= hero.MaxExp;
                hero.Level++;
                requirement = await _levelExperienceService.GetHeroRequirementAsync(hero.Level, ct);
                hero.MaxExp = requirement.IsMaxLevel ? 0 : requirement.RequiredExp;
                if (requirement.IsMaxLevel) hero.Exp = 0;
            }
            item.NewLevel = hero.Level;
            item.NewExp = hero.Exp;
            item.NewMaxExp = hero.MaxExp;
            hero.UpdatedOn = DateTime.UtcNow;
            results.Add(item);
        }
        return results;
    }

    private async Task AddPlayerExpAsync(HrkPlayer player, int exp, CancellationToken ct)
    {
        var requirement = await _levelExperienceService.GetPlayerRequirementAsync(player.Level, ct);
        player.MaxExp = requirement.IsMaxLevel ? 0 : requirement.RequiredExp;
        if (!requirement.IsMaxLevel) player.Exp += exp;

        while (!requirement.IsMaxLevel && player.MaxExp > 0 && player.Exp >= player.MaxExp)
        {
            player.Exp -= player.MaxExp;
            player.Level++;
            requirement = await _levelExperienceService.GetPlayerRequirementAsync(player.Level, ct);
            player.MaxExp = requirement.IsMaxLevel ? 0 : requirement.RequiredExp;
            if (requirement.IsMaxLevel) player.Exp = 0;
        }
        player.UpdatedOn = DateTime.UtcNow;
    }

    private static void ApplyStaminaRecovery(HrkPlayer player)
    {
        if (player.Stamina >= player.MaxStamina) { player.LastStaminaRegeneratedOn = DateTime.UtcNow; return; }
        var elapsed = DateTime.UtcNow - player.LastStaminaRegeneratedOn;
        var recovered = (int)(elapsed.TotalMinutes / StaminaRecoveryMinutes);
        if (recovered <= 0) return;
        player.Stamina = Math.Min(player.MaxStamina, player.Stamina + recovered);
        player.LastStaminaRegeneratedOn = player.LastStaminaRegeneratedOn.AddMinutes(recovered * StaminaRecoveryMinutes);
    }

    private static void ResetDailyPurchaseCount(HrkPlayer player)
    {
        if (player.StaminaPurchaseDate?.Date != DateTime.UtcNow.Date) player.DailyStaminaPurchaseCount = 0;
    }

    private static int PurchaseCost(int count) => count switch { 0 => 20, 1 => 30, 2 => 50, 3 => 80, _ => 100 };
    private static DungeonStaminaDto ToStaminaDto(HrkPlayer player) => new()
    {
        Current = player.Stamina,
        Max = player.MaxStamina,
        PurchaseCount = player.DailyStaminaPurchaseCount,
        NextPurchaseCost = PurchaseCost(player.DailyStaminaPurchaseCount),
        PurchaseAmount = 50,
        NextRecoverySeconds = player.Stamina >= player.MaxStamina ? 0 : Math.Max(0, StaminaRecoveryMinutes * 60 - (int)(DateTime.UtcNow - player.LastStaminaRegeneratedOn).TotalSeconds)
    };

    private async Task<int?> FindNextStageId(HrkDungeonStage stage, CancellationToken ct) =>
        await _unitOfWork.ReadOnlyRepository<HrkDungeonStage>().Query()
            .Where(x => x.IsActive && (x.DungeonMapId == stage.DungeonMapId && x.StageNumber == stage.StageNumber + 1 ||
                                      x.DungeonMap.DisplayOrder == stage.DungeonMap.DisplayOrder + 1 && x.StageNumber == 1))
            .OrderBy(x => x.DungeonMap.DisplayOrder).ThenBy(x => x.StageNumber).Select(x => (int?)x.Id).FirstOrDefaultAsync(ct);
}
