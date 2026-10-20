using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.Common;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using GAME.Application.Services;

namespace GAME.Application.Features.Shared.Tower;

public abstract class TowerHandlerSupport
{
    protected readonly TowerBattleExecutor _executor;
    protected readonly IUnitOfWork _unitOfWork;
    protected readonly IGamePlayerService _gamePlayerService;
    protected readonly IFormationSnapshotService _formationSnapshotService;
    protected readonly IFormationPowerQueryService _formationPowerQueryService;
    protected readonly ITowerEventService _towerEvents;
    protected TowerHandlerSupport(TowerBattleExecutor executor, IUnitOfWork unitOfWork, IGamePlayerService gamePlayerService, IFormationSnapshotService formationSnapshotService, IFormationPowerQueryService formationPowerQueryService, ITowerEventService towerEvents)
    {
        this._executor = executor;
        this._unitOfWork = unitOfWork;
        this._gamePlayerService = gamePlayerService;
        this._formationSnapshotService = formationSnapshotService;
        this._formationPowerQueryService = formationPowerQueryService;
        this._towerEvents = towerEvents;
    }

    protected async Task<List<GameEventDto>> GetEventsAsyncCore(string userId, CancellationToken ct = default)
    {
        var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, ct) ?? throw new BusinessException("PLAYER_NOT_FOUND", "Không tìm thấy người chơi.", BusinessErrorKind.NotFound);
        var events = await _unitOfWork.ReadOnlyRepository<HrkGameEvent>().Query()
            .OrderBy(e => e.DisplayOrder)
            .ToListAsync(ct);
        var result = new List<GameEventDto>();
        foreach (var ev in events)
        {
            var period = await _towerEvents.ReadCurrentPeriodAsync(ev, ct);
            var progress = await _towerEvents.ReadPlayerPeriodProgressAsync(player.Id, ev, period, ct);
            var allTime = await _unitOfWork.ReadOnlyRepository<HrkPlayerEventAllTimeRecord>().Query()
                .FirstOrDefaultAsync(a => a.PlayerId == player.Id && a.EventId == ev.Id, ct);
            int pendingCount = await _unitOfWork.ReadOnlyRepository<HrkPlayerPendingReward>().Query().CountAsync(r => r.PlayerId == player.Id && r.Status == "PENDING", ct);
            var rules = TowerRules.Parse(ev.RulesJson);
            bool isLevelMet = player.Level >= ev.MinPlayerLevel;
            string status = !EventAccessPolicy.IsOpen(ev, DateTime.UtcNow) ? "ENDED" : (!isLevelMet ? "LOCKED" : "OPEN");
            string? lockReason = !isLevelMet ? $"Yêu cầu cấp độ {ev.MinPlayerLevel} (Hiện tại: Lv.{player.Level})" : null;
            var highlightRewards = new List<GenericRewardItemDto>
            {
                new()
                {
                    Type = "GOLD",
                    Name = "Vàng Phong Phú",
                    ImagePath = "images/dcs-game/items/currencies/gold.svg",
                    Quantity = 1000000
                },
                new()
                {
                    Type = "DIAMOND",
                    Name = "Kim Cương Quý",
                    ImagePath = "images/dcs-game/items/currencies/diamond.svg",
                    Quantity = 2500
                },
                new()
                {
                    Type = "UNIVERSAL_STAR_STONE",
                    Name = "Đá Tăng Sao Mythic",
                    ImagePath = "/assets/images/dcs-game/materials/hero-star-stone.png",
                    Quantity = 5
                },
                new()
                {
                    Type = "ENHANCEMENT_STONE",
                    Name = "Thần Thạch Cường Hóa V",
                    ImagePath = "/assets/images/dcs-game/materials/enhancement-stone-v.png",
                    Quantity = 15
                }
            };
            result.Add(new GameEventDto { Id = ev.Id, Code = ev.Code, Name = ev.Name, EventType = ev.EventType, Description = ev.Description, BannerImagePath = ev.BannerImagePath, Icon = ev.Icon, DisplayOrder = ev.DisplayOrder, MinPlayerLevel = ev.MinPlayerLevel, IsOpen = ev.IsOpen, Status = status, LockReason = lockReason, RemainingSecondsToReset = EventPeriodCalculator.CalculateRemainingSecondsToReset(period.EndAtUtc), NextResetTimeUtc = period.EndAtUtc, InitialLives = ev.InitialLives, MaxFloor = await _executor.MaxFloorAsync(ev.Id, ct), HighlightRewards = highlightRewards, PlayerProgress = new PlayerEventProgressSummaryDto { CurrentFloor = progress.CurrentFloor, RemainingLives = progress.RemainingLives, InitialLives = ev.InitialLives, CurrentRunNumber = progress.CurrentRunNumber, QuickClimbRunsUsed = progress.QuickClimbRunsUsed, QuickClimbDailyLimit = rules.QuickClimbDailyLimit, QuickClimbRunsRemaining = Math.Max(0, rules.QuickClimbDailyLimit - progress.QuickClimbRunsUsed), CanStartQuickClimb = progress.QuickClimbRunsUsed < rules.QuickClimbDailyLimit, HighestFloorInPeriod = progress.HighestFloorInPeriod, HighestFloorAllTime = allTime?.HighestFloorAllTime ?? 0, IsCompleted = progress.IsCompleted, CanStartNewRun = progress.RemainingLives <= 0 && !progress.IsCompleted && (!ev.MaxDailyRuns.HasValue || progress.CurrentRunNumber < ev.MaxDailyRuns.Value), PeriodKey = period.PeriodKey, PendingRewardsCount = pendingCount } });
        }

        return result;
    }

    protected async Task<PlayerEventProgressDto> GetPlayerProgressAsyncCore(string userId, CancellationToken ct = default)
    {
        var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, ct) ?? throw new BusinessException("PLAYER_NOT_FOUND", "Không tìm thấy người chơi.", BusinessErrorKind.NotFound);
        var ev = await GetTowerEventAsync(ct);
        var period = await _towerEvents.ReadCurrentPeriodAsync(ev, ct);
        var progress = await _towerEvents.ReadPlayerPeriodProgressAsync(player.Id, ev, period, ct);
        var allTime = await _unitOfWork.ReadOnlyRepository<HrkPlayerEventAllTimeRecord>().Query()
            .FirstOrDefaultAsync(a => a.PlayerId == player.Id && a.EventId == ev.Id, ct);
        var rules = TowerRules.Parse(ev.RulesJson);
        var chests = await GetPeriodChestsAsync(period, ct);
        var claimedChestIds = await _unitOfWork.ReadOnlyRepository<HrkPlayerEventRewardClaim>().Query()
            .Where(c => c.PlayerId == player.Id && c.EventPeriodId == period.Id && c.ClaimType == "MILESTONE_CHEST")
            .Select(c => c.TargetId)
            .ToListAsync(ct);
        var claimedSet = new HashSet<int>(claimedChestIds);
        var chestDtos = chests.Select(c => new TowerChestDto { Id = c.Id, FloorNumber = c.FloorNumber, ChestName = c.ChestName, ChestIcon = c.ChestIcon, Description = c.Description, State = claimedSet.Contains(c.Id) ? "CLAIMED" : (progress.HighestFloorInPeriod >= c.FloorNumber ? "UNLOCKED" : "LOCKED"), Rewards = ParseRewardsJson(c.RewardsJson) }).ToList();
        var pending = await GetPendingRewardsAsync(userId, ct);
        var activeJob = await _unitOfWork.ReadOnlyRepository<HrkTowerQuickClimbJob>().Query()
            .FirstOrDefaultAsync(j => j.PlayerId == player.Id && j.EventPeriodId == period.Id && (j.Status == "QUEUED" || j.Status == "PROCESSING"), ct);
        return new PlayerEventProgressDto
        {
            EventId = ev.Id,
            EventName = ev.Name,
            MaxFloor = await _executor.MaxFloorAsync(ev.Id, ct),
            SecondsUntilReset = EventPeriodCalculator.CalculateRemainingSecondsToReset(period.EndAtUtc),
            Floors = await GetFloorsAsync(userId, ct),
            LeadHero = (await _formationSnapshotService.BuildAsync(player.Id, null, null, ct)).Heroes.OrderByDescending(h => h.Power).FirstOrDefault(),
            PlayerPower = await GetSelectedFormationPower(player.Id, ct),
            CurrentFloor = progress.CurrentFloor,
            RemainingLives = progress.RemainingLives,
            InitialLives = ev.InitialLives,
            CurrentRunNumber = progress.CurrentRunNumber,
            QuickClimbRunsUsed = progress.QuickClimbRunsUsed,
            QuickClimbDailyLimit = rules.QuickClimbDailyLimit,
            QuickClimbRunsRemaining = Math.Max(0, rules.QuickClimbDailyLimit - progress.QuickClimbRunsUsed),
            CanStartQuickClimb = progress.QuickClimbRunsUsed < rules.QuickClimbDailyLimit,
            HighestFloorInPeriod = progress.HighestFloorInPeriod,
            HighestFloorAllTime = allTime?.HighestFloorAllTime ?? 0,
            IsCompleted = progress.IsCompleted,
            CanStartNewRun = progress.RemainingLives <= 0,
            PeriodKey = period.PeriodKey,
            PeriodStartUtc = period.StartAtUtc,
            PeriodEndUtc = period.EndAtUtc,
            PendingRewardsCount = pending.Count,
            MilestoneChests = chestDtos,
            PendingRewards = pending,
            HasActiveQuickClimb = activeJob != null,
            ActiveQuickClimbJobId = activeJob?.JobId
        };
    }

    protected async Task<List<TowerFloorDto>> GetFloorsAsyncCore(string userId, CancellationToken ct = default)
    {
        var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, ct) ?? throw new BusinessException("PLAYER_NOT_FOUND", "Không tìm thấy người chơi.", BusinessErrorKind.NotFound);
        var ev = await GetTowerEventAsync(ct);
        var period = await _towerEvents.ReadCurrentPeriodAsync(ev, ct);
        var progress = await _towerEvents.ReadPlayerPeriodProgressAsync(player.Id, ev, period, ct);
        var maxFloor = await _executor.MaxFloorAsync(ev.Id, ct);
        var floors = await _unitOfWork.ReadOnlyRepository<HrkTowerFloor>().Query()
            .Where(f => f.EventId == ev.Id && f.IsActive && f.FloorNumber <= maxFloor)
            .Include(f => f.Enemies)
            .OrderBy(f => f.FloorNumber)
            .ToListAsync(ct);
        var chests = (await GetPeriodChestsAsync(period, ct)).ToDictionary(c => c.FloorNumber);
        var claimedChestIds = (await _unitOfWork.ReadOnlyRepository<HrkPlayerEventRewardClaim>().Query()
            .Where(c => c.PlayerId == player.Id && c.EventPeriodId == period.Id && c.ClaimType == "MILESTONE_CHEST")
            .Select(c => c.TargetId)
            .ToListAsync(ct)).ToHashSet();
        var result = new List<TowerFloorDto>();
        foreach (var floor in floors)
        {
            string state = progress.IsCompleted || floor.FloorNumber < progress.CurrentFloor ? "CLEARED" : (floor.FloorNumber == progress.CurrentFloor ? "CURRENT" : "LOCKED");
            chests.TryGetValue(floor.FloorNumber, out var chest);
            TowerChestDto? chestDto = null;
            if (chest != null)
            {
                chestDto = new TowerChestDto
                {
                    Id = chest.Id,
                    FloorNumber = chest.FloorNumber,
                    ChestName = chest.ChestName,
                    ChestIcon = chest.ChestIcon,
                    Description = chest.Description,
                    State = claimedChestIds.Contains(chest.Id) ? "CLAIMED" : (progress.HighestFloorInPeriod >= chest.FloorNumber ? "UNLOCKED" : "LOCKED"),
                    Rewards = ParseRewardsJson(chest.RewardsJson)
                };
            }

            var enemies = await _executor.BuildEnemiesAsync(floor, ct, includeSkills: false);
            var representative = enemies.FirstOrDefault(e => e.HeroTemplateId == floor.RepresentativeHeroTemplateId) ?? enemies.OrderByDescending(e => e.Power).First();
            var repEnemyDto = ToEnemyDto(representative, floor.FloorType == "BOSS");
            result.Add(new TowerFloorDto { FloorNumber = floor.FloorNumber, Name = floor.Name, FloorType = floor.FloorType, RecommendedPower = floor.RecommendedPower, BackgroundPath = floor.BackgroundPath, State = state, RepresentativeEnemy = repEnemyDto, HasMilestoneChest = chest != null, ChestInfo = chestDto, Rewards = ParseRewardsJson(floor.RewardsJson) });
        }

        return result;
    }

    protected async Task<List<PlayerPendingRewardDto>> GetPendingRewardsAsyncCore(string userId, CancellationToken ct = default)
    {
        var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, ct) ?? throw new BusinessException("PLAYER_NOT_FOUND", "Không tìm thấy người chơi.", BusinessErrorKind.NotFound);
        var pending = await _unitOfWork.ReadOnlyRepository<HrkPlayerPendingReward>().Query()
            .Where(r => r.PlayerId == player.Id && r.Status == "PENDING")
            .OrderByDescending(r => r.CreatedOnUtc)
            .ToListAsync(ct);
        return pending.Select(r => new PlayerPendingRewardDto { Id = r.Id, SourceType = r.SourceType, SourceRefId = r.SourceRefId, Description = r.Description, RewardItems = ParseRewardsJson(r.RewardItemsJson), Status = r.Status, CreatedOnUtc = r.CreatedOnUtc, ClaimedOnUtc = r.ClaimedOnUtc }).ToList();
    }

    protected async Task<StartTowerBattleResultDto?> GetBattleDetailAsyncCore(string userId, string battleId, CancellationToken ct = default)
    {
        var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, ct) ?? throw new BusinessException("PLAYER_NOT_FOUND", "Không tìm thấy người chơi.", BusinessErrorKind.NotFound);
        var battle = await _unitOfWork.ReadOnlyRepository<HrkPlayerTowerBattle>().Query()
            .SingleOrDefaultAsync(b => b.BattleId == battleId && b.PlayerId == player.Id, ct);
        if (battle == null)
            return null;
        if (string.IsNullOrWhiteSpace(battle.ResultJson))
            throw new BusinessException("REPLAY_UNAVAILABLE", "Trận cũ chưa lưu dữ liệu phát lại đầy đủ.", BusinessErrorKind.Conflict);
        return JsonSerializer.Deserialize<StartTowerBattleResultDto>(battle.ResultJson);
    }

    protected static TowerEnemyDto ToEnemyDto(PlayerHeroDto hero, bool isBoss) => new()
    {
        HeroTemplateId = hero.HeroTemplateId,
        Name = hero.Name,
        ImagePath = hero.Avatar,
        Level = hero.Level,
        Stars = (byte)hero.Stars,
        Position = hero.Position ?? 1,
        Power = hero.Power,
        IsBoss = isBoss,
        RarityCode = hero.RarityCode,
        RarityColorHex = hero.RarityColorHex,
        ClassCode = hero.ClassCode,
        FactionCode = hero.FactionCode
    };
    protected static List<GenericRewardItemDto> ParseRewardsJson(string? json) => TowerRewardService.ParseRewards(json);
    protected async Task<List<HrkTowerChestConfig>> GetPeriodChestsAsync(HrkEventPeriod period, CancellationToken ct)
    {
        if (period.ChestSnapshotJson is not null)
            return (JsonSerializer.Deserialize<List<EventChestSnapshot>>(period.ChestSnapshotJson)
                ?? throw new InvalidOperationException("Invalid period chest snapshot."))
                .Select(x => x.ToConfiguration(period.EventId)).OrderBy(x => x.FloorNumber).ToList();
        return await _unitOfWork.ReadOnlyRepository<HrkTowerChestConfig>().Query()
            .Where(x => x.EventId == period.EventId && x.IsActive).OrderBy(x => x.FloorNumber).ToListAsync(ct);
    }
    protected async Task<HrkGameEvent> GetTowerEventAsync(CancellationToken ct) => await _unitOfWork.ReadOnlyRepository<HrkGameEvent>().Query()
        .FirstOrDefaultAsync(e => e.Code == "TOWER_CLIMB", ct) ?? throw new KeyNotFoundException("Không tìm thấy cấu hình sự kiện 'TOWER_CLIMB'.");
    protected async Task<int> GetSelectedFormationPower(long playerId, CancellationToken ct)
    {
        var result = await _formationPowerQueryService.GetDefaultFormationPowerAsync(playerId, ct);
        return result.TotalPower;
    }

    protected Task<StartTowerBattleResultDto?> GetBattleDetailAsync(string userId, string battleId, CancellationToken ct = default) => GetBattleDetailAsyncCore(userId, battleId, ct);
    protected Task<List<GameEventDto>> GetEventsAsync(string userId, CancellationToken ct = default) => GetEventsAsyncCore(userId, ct);
    protected Task<List<TowerFloorDto>> GetFloorsAsync(string userId, CancellationToken ct = default) => GetFloorsAsyncCore(userId, ct);
    protected Task<List<PlayerPendingRewardDto>> GetPendingRewardsAsync(string userId, CancellationToken ct = default) => GetPendingRewardsAsyncCore(userId, ct);
    protected Task<PlayerEventProgressDto> GetPlayerProgressAsync(string userId, CancellationToken ct = default) => GetPlayerProgressAsyncCore(userId, ct);
}
