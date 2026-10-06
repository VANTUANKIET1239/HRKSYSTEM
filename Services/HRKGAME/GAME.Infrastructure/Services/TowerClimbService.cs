using Core.Common.Repositories;
using GAME.Application.Common;
using GAME.Application.Common.Helpers;
using GAME.Application.Common.Mappings;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Battle;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GAME.Infrastructure.Services;

public class TowerClimbService : ITowerClimbService
{
    private readonly TowerOperationRunner _runner;
    private readonly TowerBattleExecutor _executor;
    private readonly TowerRewardService _rewards;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFormationSnapshotService _formationSnapshotService;
    private readonly IFormationPowerQueryService _formationPowerQueryService;
    private readonly IEventPeriodService _eventPeriodService;
    private readonly ILogger<TowerClimbService> _logger;

    public TowerClimbService(
        TowerOperationRunner runner, TowerBattleExecutor executor, TowerRewardService rewards,
        IUnitOfWork unitOfWork,
        IFormationSnapshotService formationSnapshotService,
        IFormationPowerQueryService formationPowerQueryService,
        IEventPeriodService eventPeriodService,
        ILogger<TowerClimbService> logger)
    {
        _runner = runner;
        _executor = executor;
        _rewards = rewards;
        _unitOfWork = unitOfWork;
        _formationSnapshotService = formationSnapshotService;
        _formationPowerQueryService = formationPowerQueryService;
        _eventPeriodService = eventPeriodService;
        _logger = logger;
    }

    public Task<List<GameEventDto>> GetEventsAsync(string userId, CancellationToken ct = default) =>
        _runner.RunAsync(userId, () => GetEventsAsyncCore(userId, ct), ct);

    private async Task<List<GameEventDto>> GetEventsAsyncCore(string userId, CancellationToken ct = default)
    {
        var player = await GetPlayerAsync(userId, ct);
        var events = await _unitOfWork.ReadOnlyRepository<HrkGameEvent>().Query()
            .OrderBy(e => e.DisplayOrder)
            .ToListAsync(ct);

        var result = new List<GameEventDto>();
        foreach (var ev in events)
        {
            var period = await _eventPeriodService.GetOrCreateCurrentPeriodAsync(ev, ct);
            var progress = await _eventPeriodService.GetOrCreatePlayerPeriodProgressAsync(player.Id, ev, period, ct);
            var allTime = await _unitOfWork.ReadOnlyRepository<HrkPlayerEventAllTimeRecord>().Query()
                .FirstOrDefaultAsync(a => a.PlayerId == player.Id && a.EventId == ev.Id, ct);

            int pendingCount = await _unitOfWork.ReadOnlyRepository<HrkPlayerPendingReward>().Query()
                .CountAsync(r => r.PlayerId == player.Id && r.Status == "PENDING", ct);
            var rules = TowerRules.Parse(ev.RulesJson);

            bool isLevelMet = player.Level >= ev.MinPlayerLevel;
            string status = !EventAccessPolicy.IsOpen(ev, DateTime.UtcNow) ? "ENDED" : (!isLevelMet ? "LOCKED" : "OPEN");
            string? lockReason = !isLevelMet ? $"Yêu cầu cấp độ {ev.MinPlayerLevel} (Hiện tại: Lv.{player.Level})" : null;

            var highlightRewards = new List<GenericRewardItemDto>
            {
                new() { Type = "GOLD", Name = "Vàng Phong Phú", ImagePath = "/assets/images/dcs-game/items/gold.png", Quantity = 1000000 },
                new() { Type = "DIAMOND", Name = "Kim Cương Quý", ImagePath = "/assets/images/dcs-game/items/diamond.png", Quantity = 2500 },
                new() { Type = "UNIVERSAL_STAR_STONE", Name = "Đá Tăng Sao Mythic", ImagePath = "/assets/images/dcs-game/materials/hero-star-stone.png", Quantity = 5 },
                new() { Type = "ENHANCEMENT_STONE", Name = "Thần Thạch Cường Hóa V", ImagePath = "/assets/images/dcs-game/materials/enhancement-stone-v.png", Quantity = 15 }
            };

            result.Add(new GameEventDto
            {
                Id = ev.Id,
                Code = ev.Code,
                Name = ev.Name,
                EventType = ev.EventType,
                Description = ev.Description,
                BannerImagePath = ev.BannerImagePath,
                Icon = ev.Icon,
                DisplayOrder = ev.DisplayOrder,
                MinPlayerLevel = ev.MinPlayerLevel,
                IsOpen = ev.IsOpen,
                Status = status,
                LockReason = lockReason,
                RemainingSecondsToReset = _eventPeriodService.CalculateRemainingSecondsToReset(period),
                NextResetTimeUtc = period.EndAtUtc,
                InitialLives = ev.InitialLives,
                MaxFloor = await _executor.MaxFloorAsync(ev.Id, ct),
                HighlightRewards = highlightRewards,
                PlayerProgress = new PlayerEventProgressSummaryDto
                {
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
                    CanStartNewRun = progress.RemainingLives <= 0 && !progress.IsCompleted && (!ev.MaxDailyRuns.HasValue || progress.CurrentRunNumber < ev.MaxDailyRuns.Value),
                    PeriodKey = period.PeriodKey,
                    PendingRewardsCount = pendingCount
                }
            });
        }

        return result;
    }

    public Task<GameEventDto> GetEventDetailAsync(string userId, string eventCode, CancellationToken ct = default) =>
        _runner.RunAsync(userId, () => GetEventDetailAsyncCore(userId, eventCode, ct), ct);

    private async Task<GameEventDto> GetEventDetailAsyncCore(string userId, string eventCode, CancellationToken ct = default)
    {
        var events = await GetEventsAsync(userId, ct);
        var ev = events.FirstOrDefault(e => e.Code.Equals(eventCode, StringComparison.OrdinalIgnoreCase))
            ?? throw new KeyNotFoundException($"Không tìm thấy sự kiện '{eventCode}'.");
        return ev;
    }

    public Task<PlayerEventProgressDto> GetPlayerProgressAsync(string userId, CancellationToken ct = default) =>
        _runner.RunAsync(userId, () => GetPlayerProgressAsyncCore(userId, ct), ct);

    private async Task<PlayerEventProgressDto> GetPlayerProgressAsyncCore(string userId, CancellationToken ct = default)
    {
        var player = await GetPlayerAsync(userId, ct);
        var ev = await GetTowerEventAsync(ct);
        var period = await _eventPeriodService.GetOrCreateCurrentPeriodAsync(ev, ct);
        var progress = await _eventPeriodService.GetOrCreatePlayerPeriodProgressAsync(player.Id, ev, period, ct);
        var allTime = await _unitOfWork.ReadOnlyRepository<HrkPlayerEventAllTimeRecord>().Query()
            .FirstOrDefaultAsync(a => a.PlayerId == player.Id && a.EventId == ev.Id, ct);
        var rules = TowerRules.Parse(ev.RulesJson);

        var chests = await _unitOfWork.ReadOnlyRepository<HrkTowerChestConfig>().Query()
            .Where(c => c.EventId == ev.Id && c.IsActive)
            .OrderBy(c => c.FloorNumber)
            .ToListAsync(ct);

        var claimedChestIds = await _unitOfWork.ReadOnlyRepository<HrkPlayerEventRewardClaim>().Query()
            .Where(c => c.PlayerId == player.Id && c.EventPeriodId == period.Id && c.ClaimType == "MILESTONE_CHEST")
            .Select(c => c.TargetId)
            .ToListAsync(ct);

        var claimedSet = new HashSet<int>(claimedChestIds);

        var chestDtos = chests.Select(c => new TowerChestDto
        {
            Id = c.Id,
            FloorNumber = c.FloorNumber,
            ChestName = c.ChestName,
            ChestIcon = c.ChestIcon,
            Description = c.Description,
            State = claimedSet.Contains(c.Id) ? "CLAIMED" : (progress.HighestFloorInPeriod >= c.FloorNumber ? "UNLOCKED" : "LOCKED"),
            Rewards = ParseRewardsJson(c.RewardsJson)
        }).ToList();

        var pending = await GetPendingRewardsAsync(userId, ct);

        var activeJob = await _unitOfWork.ReadOnlyRepository<HrkTowerQuickClimbJob>().Query()
            .FirstOrDefaultAsync(j => j.PlayerId == player.Id && j.EventPeriodId == period.Id &&
                (j.Status == "QUEUED" || j.Status == "PROCESSING"), ct);

        return new PlayerEventProgressDto
        {
            EventId = ev.Id,
            EventName = ev.Name,
            MaxFloor = await _executor.MaxFloorAsync(ev.Id, ct),
            SecondsUntilReset = _eventPeriodService.CalculateRemainingSecondsToReset(period),
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

    public Task<List<TowerFloorDto>> GetFloorsAsync(string userId, CancellationToken ct = default) =>
        _runner.RunAsync(userId, () => GetFloorsAsyncCore(userId, ct), ct);

    private async Task<List<TowerFloorDto>> GetFloorsAsyncCore(string userId, CancellationToken ct = default)
    {
        var player = await GetPlayerAsync(userId, ct);
        var ev = await GetTowerEventAsync(ct);
        var period = await _eventPeriodService.GetOrCreateCurrentPeriodAsync(ev, ct);
        var progress = await _eventPeriodService.GetOrCreatePlayerPeriodProgressAsync(player.Id, ev, period, ct);

        var maxFloor = await _executor.MaxFloorAsync(ev.Id, ct);
        var floors = await _unitOfWork.ReadOnlyRepository<HrkTowerFloor>().Query()
            .Where(f => f.EventId == ev.Id && f.IsActive && f.FloorNumber <= maxFloor)
            .Include(f => f.Enemies).ThenInclude(e => e.HeroTemplate)
            .Include(f => f.RepresentativeHeroTemplate)
            .OrderBy(f => f.FloorNumber)
            .ToListAsync(ct);

        var chests = await _unitOfWork.ReadOnlyRepository<HrkTowerChestConfig>().Query()
            .Where(c => c.EventId == ev.Id && c.IsActive)
            .ToDictionaryAsync(c => c.FloorNumber, ct);

        var claimedChestIds = (await _unitOfWork.ReadOnlyRepository<HrkPlayerEventRewardClaim>().Query()
            .Where(c => c.PlayerId == player.Id && c.EventPeriodId == period.Id && c.ClaimType == "MILESTONE_CHEST")
            .Select(c => c.TargetId)
            .ToListAsync(ct)).ToHashSet();

        var result = new List<TowerFloorDto>();
        foreach (var floor in floors)
        {
            string state = progress.IsCompleted || floor.FloorNumber < progress.CurrentFloor
                ? "CLEARED"
                : (floor.FloorNumber == progress.CurrentFloor ? "CURRENT" : "LOCKED");

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
            var representative = enemies.FirstOrDefault(e => e.HeroTemplateId == floor.RepresentativeHeroTemplateId)
                ?? enemies.OrderByDescending(e => e.Power).First();
            var repEnemyDto = ToEnemyDto(representative, floor.FloorType == "BOSS");

            result.Add(new TowerFloorDto
            {
                FloorNumber = floor.FloorNumber,
                Name = floor.Name,
                FloorType = floor.FloorType,
                RecommendedPower = floor.RecommendedPower,
                BackgroundPath = floor.BackgroundPath,
                State = state,
                RepresentativeEnemy = repEnemyDto,
                HasMilestoneChest = chest != null,
                ChestInfo = chestDto,
                Rewards = ParseRewardsJson(floor.RewardsJson)
            });
        }

        return result;
    }

    public Task<TowerFloorDetailDto> GetFloorDetailAsync(string userId, int floorNumber, CancellationToken ct = default) =>
        _runner.RunAsync(userId, () => GetFloorDetailAsyncCore(userId, floorNumber, ct), ct);

    private async Task<TowerFloorDetailDto> GetFloorDetailAsyncCore(string userId, int floorNumber, CancellationToken ct = default)
    {
        var player = await GetPlayerAsync(userId, ct);
        var ev = await GetTowerEventAsync(ct);
        var period = await _eventPeriodService.GetOrCreateCurrentPeriodAsync(ev, ct);
        var progress = await _eventPeriodService.GetOrCreatePlayerPeriodProgressAsync(player.Id, ev, period, ct);

        var floor = await _unitOfWork.ReadOnlyRepository<HrkTowerFloor>().Query()
            .Where(f => f.EventId == ev.Id && f.FloorNumber == floorNumber && f.IsActive)
            .Include(f => f.Enemies).ThenInclude(e => e.HeroTemplate)
            .Include(f => f.RepresentativeHeroTemplate)
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Không tìm thấy tầng {floorNumber}.");

        var chests = await _unitOfWork.ReadOnlyRepository<HrkTowerChestConfig>().Query()
            .Where(c => c.EventId == ev.Id && c.FloorNumber == floorNumber && c.IsActive)
            .FirstOrDefaultAsync(ct);

        var claimedChestIds = (await _unitOfWork.ReadOnlyRepository<HrkPlayerEventRewardClaim>().Query()
            .Where(c => c.PlayerId == player.Id && c.EventPeriodId == period.Id && c.ClaimType == "MILESTONE_CHEST")
            .Select(c => c.TargetId)
            .ToListAsync(ct)).ToHashSet();

        string state = progress.IsCompleted || floor.FloorNumber < progress.CurrentFloor
            ? "CLEARED"
            : (floor.FloorNumber == progress.CurrentFloor ? "CURRENT" : "LOCKED");

        TowerChestDto? chestDto = null;
        if (chests != null)
        {
            chestDto = new TowerChestDto
            {
                Id = chests.Id,
                FloorNumber = chests.FloorNumber,
                ChestName = chests.ChestName,
                ChestIcon = chests.ChestIcon,
                Description = chests.Description,
                State = claimedChestIds.Contains(chests.Id) ? "CLAIMED" : (progress.HighestFloorInPeriod >= chests.FloorNumber ? "UNLOCKED" : "LOCKED"),
                Rewards = ParseRewardsJson(chests.RewardsJson)
            };
        }

        var playerPower = await GetSelectedFormationPower(player.Id, ct);
        var enemyDtos = (await _executor.BuildEnemiesAsync(floor, ct, includeSkills: false))
            .Select(e => ToEnemyDto(e, floor.FloorType == "BOSS" && e.Position == 1)).ToList();

        bool canStart = floorNumber <= await _executor.MaxFloorAsync(ev.Id, ct) && state == "CURRENT" && progress.RemainingLives > 0 && !progress.IsCompleted && EventAccessPolicy.IsOpen(ev, DateTime.UtcNow) && player.Level >= ev.MinPlayerLevel;
        string? lockedReason = !EventAccessPolicy.IsOpen(ev, DateTime.UtcNow) ? "Sự kiện chưa mở hoặc đã kết thúc."
            : player.Level < ev.MinPlayerLevel ? $"Cần cấp {ev.MinPlayerLevel} để tham gia." : null;
        if (progress.RemainingLives <= 0)
        {
            lockedReason = "Đã hết mạng trong lượt leo hiện tại. Hãy bắt đầu lượt mới!";
        }
        else if (floorNumber > progress.CurrentFloor)
        {
            lockedReason = $"Chưa mở khóa. Bạn cần vượt qua Tầng {progress.CurrentFloor} trước!";
        }
        else if (floorNumber < progress.CurrentFloor)
        {
            lockedReason = "Tầng này đã hoàn thành trong lượt hiện tại.";
        }
        else if (progress.IsCompleted)
        {
            lockedReason = "Bạn đã chinh phục đỉnh tháp hôm nay!";
        }

        return new TowerFloorDetailDto
        {
            FloorNumber = floor.FloorNumber,
            Name = floor.Name,
            FloorType = floor.FloorType,
            RecommendedPower = floor.RecommendedPower,
            BackgroundPath = floor.BackgroundPath,
            State = state,
            HasMilestoneChest = chests != null,
            ChestInfo = chestDto,
            Rewards = ParseRewardsJson(floor.RewardsJson),
            PlayerPower = playerPower,
            EnemyPower = enemyDtos.Sum(e => e.Power),
            Enemies = enemyDtos,
            RemainingLives = progress.RemainingLives,
            MaxLives = ev.InitialLives,
            CanStart = canStart,
            LockedReason = lockedReason
        };
    }

    public Task<StartTowerBattleResultDto> StartFloorBattleAsync(
        string userId, int floorNumber, StartTowerBattleRequestDto request, CancellationToken ct = default) =>
        _runner.RunAsync(userId, () => StartFloorBattleAsyncCore(userId, floorNumber, request, ct), ct);

    private async Task<StartTowerBattleResultDto> StartFloorBattleAsyncCore(
        string userId, int floorNumber, StartTowerBattleRequestDto request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.ClientRequestId))
            throw new ArgumentException("ClientRequestId là bắt buộc.");

        var player = await GetPlayerAsync(userId, ct);
        // Idempotency: verify ClientRequestId duplicate
        var duplicateBattle = await _unitOfWork.ReadOnlyRepository<HrkPlayerTowerBattle>().Query()
            .AnyAsync(b => b.PlayerId == player.Id && b.BattleId == request.ClientRequestId, ct);
        if (duplicateBattle)
            return await GetBattleDetailAsync(userId, request.ClientRequestId, ct)
                ?? throw new InvalidOperationException("Không đọc được kết quả trận đã xử lý.");

        var ev = await GetTowerEventAsync(ct);
        EventAccessPolicy.EnsureCanEnter(ev, player.Level, DateTime.UtcNow);

        // Check active quick climb job: cannot do manual battle while quick climb is active
        var activeQuickClimb = await _unitOfWork.ReadOnlyRepository<HrkTowerQuickClimbJob>().Query()
            .AnyAsync(j => j.PlayerId == player.Id && (j.Status == "QUEUED" || j.Status == "PROCESSING"), ct);
        if (activeQuickClimb)
            throw new InvalidOperationException("Đang có phiên leo nhanh hoạt động. Vui lòng dừng leo nhanh trước khi khiêu chiến thủ công.");

        var currentPeriod = await _eventPeriodService.GetOrCreateCurrentPeriodAsync(ev, ct);
        var progress = await _eventPeriodService.GetOrCreatePlayerPeriodProgressAsync(player.Id, ev, currentPeriod, ct);

        // Validate floor progression (no skipping)
        if (progress.RemainingLives <= 0)
            throw new InvalidOperationException("Đã hết mạng trong lượt leo hiện tại. Vui lòng bắt đầu lượt mới.");
        if (floorNumber != progress.CurrentFloor)
            throw new InvalidOperationException($"Không thể đánh tầng {floorNumber}. Tầng hợp lệ hiện tại là {progress.CurrentFloor}.");
        if (progress.IsCompleted)
            throw new InvalidOperationException("Đã hoàn thành toàn bộ tầng tháp trong kỳ này.");

        // Snapshot formation
        var snapshot = await _formationSnapshotService.BuildAsync(player.Id, request.FormationCode, request.Positions, ct);
        if (snapshot.Heroes.Count == 0)
            throw new InvalidOperationException("Đội hình xuất chiến không có võ tướng nào.");

        // Load floor & enemies
        var floor = await _unitOfWork.ReadOnlyRepository<HrkTowerFloor>().Query()
            .Where(f => f.EventId == ev.Id && f.FloorNumber == floorNumber && f.IsActive)
            .Include(f => f.Enemies).ThenInclude(e => e.HeroTemplate)
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Không tìm thấy cấu hình tầng {floorNumber}.");

        StartTowerBattleResultDto? completedResult = null;

        {
            // Player-scoped SQL application lock is owned by TowerOperationRunner.
            var txProgress = await _unitOfWork.Repository<HrkPlayerEventPeriodProgress>().Query()
                .SingleAsync(p => p.Id == progress.Id, ct);
            var allTime = await _unitOfWork.Repository<HrkPlayerEventAllTimeRecord>().Query()
                .SingleAsync(a => a.PlayerId == player.Id && a.EventId == ev.Id, ct);

            if (txProgress.RemainingLives <= 0 || txProgress.CurrentFloor != floorNumber)
                throw new InvalidOperationException("Trạng thái tầng hoặc số mạng đã thay đổi.");

            int livesBefore = txProgress.RemainingLives;
            await _executor.FreezeSkillsAsync(snapshot, ct, force: true);
            var (initialState, simulation, randomSeed) = await _executor.SimulateTowerFloorBattle(player, snapshot, floor, ct);
            var heroStats = BattleStatisticsCalculator.Calculate(initialState, simulation.Events);

            bool isVictory = simulation.Winner.Equals("LEFT", StringComparison.OrdinalIgnoreCase);
            int livesAfter = EventAccessPolicy.LivesAfter(ev, livesBefore, isVictory);
            bool isRunEnded = false;
            bool isTowerCompleted = false;
            int? nextFloorNumber = null;

            var earnedRewards = new List<GenericRewardItemDto>();
            var pendingRewards = new List<GenericRewardItemDto>();
            bool milestoneUnlocked = false;
            TowerChestDto? unlockedChestDto = null;
            int playerExpGained = 0;
            int heroExpGained = 0;

            if (isVictory)
            {
                txProgress.RemainingLives = livesAfter;
                txProgress.HighestFloorInPeriod = Math.Max(txProgress.HighestFloorInPeriod, floorNumber);
                allTime.HighestFloorAllTime = Math.Max(allTime.HighestFloorAllTime, floorNumber);

                if (floorNumber >= await _executor.MaxFloorAsync(ev.Id, ct))
                {
                    txProgress.IsCompleted = true;
                    isTowerCompleted = true;
                    allTime.TotalClears++;
                }
                else
                {
                    txProgress.CurrentFloor = floorNumber + 1;
                    nextFloorNumber = floorNumber + 1;
                }

                // Check floor reward: claimable only once per period
                bool alreadyClaimed = await _unitOfWork.ReadOnlyRepository<HrkPlayerEventRewardClaim>().Query()
                    .AnyAsync(c => c.PlayerId == player.Id && c.EventPeriodId == currentPeriod.Id && c.ClaimType == "FLOOR_REWARD" && c.TargetId == floorNumber, ct);

                if (!alreadyClaimed)
                {
                    var configuredRewards = ParseRewardsJson(floor.RewardsJson);
                    var (granted, pending, bagFull) = await _rewards.GrantGenericRewardsAsync(player.Id, configuredRewards, currentPeriod.Id, $"Thưởng vượt Tầng {floorNumber}", ct, experienceHandled: true);
                    earnedRewards.AddRange(granted);
                    pendingRewards.AddRange(pending);

                    (playerExpGained, heroExpGained) = TowerRewardService.GetExperience(configuredRewards);

                    var claim = new HrkPlayerEventRewardClaim
                    {
                        PlayerId = player.Id,
                        EventPeriodId = currentPeriod.Id,
                        ClaimType = "FLOOR_REWARD",
                        TargetId = floorNumber,
                        ClaimedAtUtc = DateTime.UtcNow,
                        RewardSummaryJson = JsonSerializer.Serialize(granted)
                    };
                    await _unitOfWork.Repository<HrkPlayerEventRewardClaim>().AddAsync(claim);
                }

                // Check milestone chest unlocked at floors 15, 30, 45, 60
                var chest = await _unitOfWork.ReadOnlyRepository<HrkTowerChestConfig>().Query()
                    .FirstOrDefaultAsync(c => c.EventId == ev.Id && c.FloorNumber == floorNumber && c.IsActive, ct);

                if (chest != null)
                {
                    milestoneUnlocked = true;
                    unlockedChestDto = new TowerChestDto
                    {
                        Id = chest.Id,
                        FloorNumber = chest.FloorNumber,
                        ChestName = chest.ChestName,
                        ChestIcon = chest.ChestIcon,
                        Description = chest.Description,
                        State = "UNLOCKED",
                        Rewards = ParseRewardsJson(chest.RewardsJson)
                    };
                }
            }
            else
            {
                // Defeat: lose 1 life, stay on floor
                txProgress.RemainingLives = livesAfter;
                if (livesAfter <= 0)
                {
                    // Out of lives: reset run to floor 1
                    txProgress.CurrentFloor = 1;
                    isRunEnded = true;
                }
                else
                {
                    nextFloorNumber = floorNumber; // Can retry
                }
            }

            if (livesAfter == 0 && !isTowerCompleted)
            {
                txProgress.CurrentFloor = 1;
                isRunEnded = true;
                nextFloorNumber = null;
            }
            txProgress.UpdatedOn = DateTime.UtcNow;
            allTime.UpdatedOn = DateTime.UtcNow;

            // Record battle history
            var battleRecord = new HrkPlayerTowerBattle
            {
                BattleId = request.ClientRequestId,
                PlayerId = player.Id,
                EventPeriodId = currentPeriod.Id,
                FloorNumber = floorNumber,
                Result = isVictory ? "VICTORY" : "DEFEAT",
                LivesBefore = livesBefore,
                LivesAfter = livesAfter,
                PlayerPower = snapshot.TotalPower,
                EnemyPower = initialState.RightTeam.Sum(e => e.Power),
                RandomSeed = randomSeed,
                HeroStatisticsJson = JsonSerializer.Serialize(heroStats),
                CreatedOnUtc = DateTime.UtcNow
            };
            await _unitOfWork.Repository<HrkPlayerTowerBattle>().AddAsync(battleRecord);

            // Add participant hero EXP if victory
            List<HeroExpResultDto> heroExpResults = new();
            if (isVictory && heroExpGained > 0)
            {
                heroExpResults = await _rewards.AddParticipantHeroExp(player.Id, snapshot.ParticipantHeroIds, heroExpGained, ct);
            }

            if (isVictory && playerExpGained > 0)
            {
                await _rewards.AddPlayerExpAsync(player, playerExpGained, ct);
            }

            await _unitOfWork.SaveChangesAsync(ct);


            var startBattleResult = TowerBattleExecutor.ToBattleResult(request.ClientRequestId, initialState, simulation, randomSeed, heroStats);

            completedResult = new StartTowerBattleResultDto
            {
                Battle = startBattleResult,
                FloorNumber = floorNumber,
                NextFloorNumber = nextFloorNumber,
                LivesBefore = livesBefore,
                LivesAfter = livesAfter,
                IsVictory = isVictory,
                IsRunEnded = isRunEnded,
                IsTowerCompleted = isTowerCompleted,
                IsMilestoneChestUnlocked = milestoneUnlocked,
                UnlockedChest = unlockedChestDto,
                EarnedRewards = earnedRewards,
                PendingRewards = pendingRewards,
                PlayerExpGained = playerExpGained,
                HeroExpGained = heroExpGained,
                NewPlayerLevel = player.Level,
                NewPlayerExp = player.Exp,
                Heroes = heroExpResults
            };
            battleRecord.ResultJson = JsonSerializer.Serialize(completedResult);
            await _executor.UpdateRunAsync(txProgress, floorNumber, ct);
        }

        return completedResult ?? throw new InvalidOperationException("Giao dịch trận đánh tháp không hoàn tất.");
    }

    public Task<PlayerEventProgressDto> StartNewRunAsync(string userId, CancellationToken ct = default) =>
        _runner.RunAsync(userId, () => StartNewRunAsyncCore(userId, ct), ct);

    private async Task<PlayerEventProgressDto> StartNewRunAsyncCore(string userId, CancellationToken ct = default)
    {
        var player = await GetPlayerAsync(userId, ct);
        var ev = await GetTowerEventAsync(ct);
        var period = await _eventPeriodService.GetOrCreateCurrentPeriodAsync(ev, ct);
        var progress = await _eventPeriodService.GetOrCreatePlayerPeriodProgressAsync(player.Id, ev, period, ct);

        EventAccessPolicy.EnsureCanEnter(ev, player.Level, DateTime.UtcNow);
        EventAccessPolicy.EnsureNewRun(ev, progress);
        if (await _unitOfWork.ReadOnlyRepository<HrkTowerQuickClimbJob>().Query()
            .AnyAsync(j => j.PlayerId == player.Id && (j.Status == "QUEUED" || j.Status == "PROCESSING"), ct))
            throw new InvalidOperationException("Hãy dừng phiên leo nhanh trước.");

        progress.CurrentFloor = 1;
        progress.RemainingLives = ev.InitialLives;
        progress.CurrentRunNumber++;
        progress.IsCompleted = false;
        progress.UpdatedOn = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(ct);
        return await GetPlayerProgressAsync(userId, ct);
    }

    public Task<ClaimRewardResultDto> ClaimMilestoneChestAsync(string userId, int chestId, CancellationToken ct = default) =>
        _runner.RunAsync(userId, () => ClaimMilestoneChestAsyncCore(userId, chestId, ct), ct);

    private async Task<ClaimRewardResultDto> ClaimMilestoneChestAsyncCore(string userId, int chestId, CancellationToken ct = default)
    {
        var player = await GetPlayerAsync(userId, ct);
        var ev = await GetTowerEventAsync(ct);
        var period = await _eventPeriodService.GetOrCreateCurrentPeriodAsync(ev, ct);
        var progress = await _eventPeriodService.GetOrCreatePlayerPeriodProgressAsync(player.Id, ev, period, ct);

        var chest = await _unitOfWork.ReadOnlyRepository<HrkTowerChestConfig>().Query()
            .FirstOrDefaultAsync(c => c.Id == chestId && c.EventId == ev.Id && c.IsActive, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy cấu hình rương mốc.");

        if (progress.HighestFloorInPeriod < chest.FloorNumber)
            throw new InvalidOperationException($"Chưa đạt mốc Tầng {chest.FloorNumber} trong kỳ hiện tại để mở rương.");

        bool alreadyClaimed = await _unitOfWork.ReadOnlyRepository<HrkPlayerEventRewardClaim>().Query()
            .AnyAsync(c => c.PlayerId == player.Id && c.EventPeriodId == period.Id && c.ClaimType == "MILESTONE_CHEST" && c.TargetId == chest.Id, ct);

        if (alreadyClaimed)
            throw new InvalidOperationException("Rương này đã được nhận trong kỳ hiện tại.");

        var configuredRewards = ParseRewardsJson(chest.RewardsJson);
        var (granted, pending, bagFull) = await _rewards.GrantGenericRewardsAsync(player.Id, configuredRewards, period.Id, $"Rương mốc Tầng {chest.FloorNumber}", ct);

        var claim = new HrkPlayerEventRewardClaim
        {
            PlayerId = player.Id,
            EventPeriodId = period.Id,
            ClaimType = "MILESTONE_CHEST",
            TargetId = chest.Id,
            ClaimedAtUtc = DateTime.UtcNow,
            RewardSummaryJson = JsonSerializer.Serialize(granted)
        };

        await _unitOfWork.Repository<HrkPlayerEventRewardClaim>().AddAsync(claim);
        await _unitOfWork.SaveChangesAsync(ct);

        return new ClaimRewardResultDto
        {
            Success = true,
            Message = bagFull ? "Đã nhận một phần thưởng, các vật phẩm còn lại chuyển vào Thưởng Chờ do túi đầy." : "Nhận rương mốc thành công!",
            ClaimedRewards = granted,
            IsBagFull = bagFull
        };
    }

    public Task<List<PlayerPendingRewardDto>> GetPendingRewardsAsync(string userId, CancellationToken ct = default) =>
        _runner.RunAsync(userId, () => GetPendingRewardsAsyncCore(userId, ct), ct);

    private async Task<List<PlayerPendingRewardDto>> GetPendingRewardsAsyncCore(string userId, CancellationToken ct = default)
    {
        var player = await GetPlayerAsync(userId, ct);
        var pending = await _unitOfWork.ReadOnlyRepository<HrkPlayerPendingReward>().Query()
            .Where(r => r.PlayerId == player.Id && r.Status == "PENDING")
            .OrderByDescending(r => r.CreatedOnUtc)
            .ToListAsync(ct);

        return pending.Select(r => new PlayerPendingRewardDto
        {
            Id = r.Id,
            SourceType = r.SourceType,
            SourceRefId = r.SourceRefId,
            Description = r.Description,
            RewardItems = ParseRewardsJson(r.RewardItemsJson),
            Status = r.Status,
            CreatedOnUtc = r.CreatedOnUtc,
            ClaimedOnUtc = r.ClaimedOnUtc
        }).ToList();
    }

    public Task<ClaimRewardResultDto> ClaimPendingRewardAsync(string userId, long pendingRewardId, CancellationToken ct = default) =>
        _runner.RunAsync(userId, () => ClaimPendingRewardAsyncCore(userId, pendingRewardId, ct), ct);

    private async Task<ClaimRewardResultDto> ClaimPendingRewardAsyncCore(string userId, long pendingRewardId, CancellationToken ct = default)
    {
        var player = await GetPlayerAsync(userId, ct);
        var pending = await _unitOfWork.Repository<HrkPlayerPendingReward>().Query()
            .FirstOrDefaultAsync(r => r.Id == pendingRewardId && r.PlayerId == player.Id && r.Status == "PENDING", ct)
            ?? throw new KeyNotFoundException("Không tìm thấy phần thưởng chờ hợp lệ.");

        var rewards = ParseRewardsJson(pending.RewardItemsJson);
        var (granted, stillPending, bagFull) = await _rewards.GrantGenericRewardsAsync(player.Id, rewards, pending.EventPeriodId, pending.Description, ct, createPending: false);

        if (stillPending.Count == 0)
        {
            pending.Status = "CLAIMED";
            pending.ClaimedOnUtc = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(ct);
            return new ClaimRewardResultDto
            {
                Success = true,
                Message = "Đã nhận toàn bộ phần thưởng chờ!",
                ClaimedRewards = granted,
                IsBagFull = false
            };
        }
        else
        {
            pending.RewardItemsJson = JsonSerializer.Serialize(stillPending);
            await _unitOfWork.SaveChangesAsync(ct);
            return new ClaimRewardResultDto
            {
                Success = granted.Count > 0,
                Message = "Hành trang đầy. Vui lòng dọn dẹp để nhận tiếp phần còn lại.",
                ClaimedRewards = granted,
                IsBagFull = true
            };
        }
    }

    public Task<StartTowerBattleResultDto?> GetBattleDetailAsync(string userId, string battleId, CancellationToken ct = default) =>
        _runner.RunAsync(userId, () => GetBattleDetailAsyncCore(userId, battleId, ct), ct);

    private async Task<StartTowerBattleResultDto?> GetBattleDetailAsyncCore(string userId, string battleId, CancellationToken ct = default)
    {
        var player = await GetPlayerAsync(userId, ct);
        var battle = await _unitOfWork.ReadOnlyRepository<HrkPlayerTowerBattle>().Query()
            .SingleOrDefaultAsync(b => b.BattleId == battleId && b.PlayerId == player.Id, ct);
        if (battle == null) return null;
        if (string.IsNullOrWhiteSpace(battle.ResultJson))
            throw new InvalidOperationException("Trận cũ chưa lưu dữ liệu phát lại đầy đủ.");
        return JsonSerializer.Deserialize<StartTowerBattleResultDto>(battle.ResultJson);
    }

    private static TowerEnemyDto ToEnemyDto(PlayerHeroDto hero, bool isBoss) => new()
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

    #region Internal Simulation & Reward Helpers

    private static List<GenericRewardItemDto> ParseRewardsJson(string? json) =>
        TowerRewardService.ParseRewards(json);

    private async Task<HrkPlayer> GetPlayerAsync(string userId, CancellationToken ct) =>
        await _unitOfWork.Repository<HrkPlayer>().Query().SingleOrDefaultAsync(x => x.UserId == userId && x.IsActive, ct)
        ?? throw new KeyNotFoundException("Không tìm thấy người chơi.");

    private async Task<HrkGameEvent> GetTowerEventAsync(CancellationToken ct) =>
        await _unitOfWork.ReadOnlyRepository<HrkGameEvent>().Query()
            .FirstOrDefaultAsync(e => e.Code == "TOWER_CLIMB", ct)
            ?? throw new KeyNotFoundException("Không tìm thấy cấu hình sự kiện 'TOWER_CLIMB'.");

    private async Task<int> GetSelectedFormationPower(long playerId, CancellationToken ct)
    {
        var result = await _formationPowerQueryService.GetDefaultFormationPowerAsync(playerId, ct);
        return result.TotalPower;
    }


    #endregion
}
