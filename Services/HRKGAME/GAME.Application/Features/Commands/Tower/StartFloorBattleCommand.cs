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
using GAME.Application.Features.Shared.Tower;
using GAME.Application.Events;

namespace GAME.Application.Features.Commands.Tower;

public sealed record StartFloorBattleCommand(

    string UserId,

    int FloorNumber,

    StartTowerBattleRequestDto Request

)

    : ICommand<BaseResponse<StartTowerBattleResultDto>>;

public sealed class StartFloorBattleCommandHandler(
    TowerBattleExecutor executor,
    IUnitOfWork unitOfWork,
    IGamePlayerService gamePlayerService,
    IFormationSnapshotService formationSnapshotService,
    IFormationPowerQueryService formationPowerQueryService,
    ITowerEventService towerEvents,
    TowerOperationRunner runner,
    TowerRewardService rewards,
    IPlayerActivityEvents activityEvents)
    : TowerHandlerSupport(executor, unitOfWork, gamePlayerService, formationSnapshotService, formationPowerQueryService, towerEvents), ICommandHandler<StartFloorBattleCommand, BaseResponse<StartTowerBattleResultDto>>
{
    private readonly TowerOperationRunner _runner = runner;
    private readonly TowerRewardService _rewards = rewards;
    public async Task<BaseResponse<StartTowerBattleResultDto>> Handle(StartFloorBattleCommand message, CancellationToken ct) => BaseResponse<StartTowerBattleResultDto>.SuccessResponse(await ExecuteAsync(message.UserId, message.FloorNumber, message.Request, ct));
    private Task<StartTowerBattleResultDto> ExecuteAsync(string userId, int floorNumber, StartTowerBattleRequestDto request, CancellationToken ct = default) => _runner.RunAsync(userId, () => StartFloorBattleAsyncCore(userId, floorNumber, request, ct), ct);
    private async Task<StartTowerBattleResultDto> StartFloorBattleAsyncCore(string userId, int floorNumber, StartTowerBattleRequestDto request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.ClientRequestId))
            throw new BusinessException("INVALID_REQUEST_ID", "ClientRequestId là bắt buộc.", BusinessErrorKind.Validation);
        var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, ct, tracked: true) ?? throw new BusinessException("PLAYER_NOT_FOUND", "Không tìm thấy người chơi.", BusinessErrorKind.NotFound);
        // Idempotency: verify ClientRequestId duplicate
        var duplicateBattle = await _unitOfWork.ReadOnlyRepository<HrkPlayerTowerBattle>().Query().AnyAsync(b => b.PlayerId == player.Id && b.BattleId == request.ClientRequestId, ct);
        if (duplicateBattle)
            return await GetBattleDetailAsync(userId, request.ClientRequestId, ct) ?? throw new InvalidOperationException("Không đọc được kết quả trận đã xử lý.");
        var ev = await GetTowerEventAsync(ct);
        EventAccessPolicy.EnsureCanEnter(ev, player.Level, DateTime.UtcNow);
        var currentPeriod = await _towerEvents.GetOrCreateCurrentPeriodAsync(ev, ct);
        var progress = await _towerEvents.GetOrCreatePlayerPeriodProgressAsync(player.Id, ev, currentPeriod, ct);
        // Check active quick climb job: cannot do manual battle while quick climb is active
        var activeQuickClimb = await _unitOfWork.ReadOnlyRepository<HrkTowerQuickClimbJob>().Query().AnyAsync(j => j.PlayerId == player.Id && (j.Status == "QUEUED" || j.Status == "PROCESSING"), ct);
        if (activeQuickClimb)
            throw new BusinessException("QUICK_CLIMB_ACTIVE", "Đang có phiên leo nhanh hoạt động. Vui lòng dừng leo nhanh trước khi khiêu chiến thủ công.", BusinessErrorKind.Conflict);
        // Validate floor progression (no skipping)
        if (progress.RemainingLives <= 0)
            throw new BusinessException("NO_REMAINING_LIVES", "Đã hết mạng trong lượt leo hiện tại. Vui lòng bắt đầu lượt mới.", BusinessErrorKind.Conflict);
        if (floorNumber != progress.CurrentFloor)
            throw new BusinessException("FLOOR_LOCKED", $"Không thể đánh tầng {floorNumber}. Tầng hợp lệ hiện tại là {progress.CurrentFloor}.", BusinessErrorKind.Conflict);
        if (progress.IsCompleted)
            throw new BusinessException("TOWER_COMPLETED", "Đã hoàn thành toàn bộ tầng tháp trong kỳ này.", BusinessErrorKind.Conflict);
        // Snapshot formation
        var snapshot = await _formationSnapshotService.BuildAsync(player.Id, request.FormationCode, request.Positions, ct);
        if (snapshot.Heroes.Count == 0)
            throw new BusinessException("EMPTY_FORMATION", "Đội hình xuất chiến không có võ tướng nào.", BusinessErrorKind.Conflict);
        // Load floor & enemies
        var floor = await _unitOfWork.ReadOnlyRepository<HrkTowerFloor>().Query()
            .Where(f => f.EventId == ev.Id && f.FloorNumber == floorNumber && f.IsActive)
            .Include(f => f.Enemies)
            .FirstOrDefaultAsync(ct) ?? throw new KeyNotFoundException($"Không tìm thấy cấu hình tầng {floorNumber}.");
        StartTowerBattleResultDto? completedResult = null;
        {
            // Player-scoped SQL application lock is owned by TowerOperationRunner.
            var txProgress = await _unitOfWork.Repository<HrkPlayerEventPeriodProgress>().Query()
                .SingleAsync(p => p.Id == progress.Id, ct);
            var allTime = await _unitOfWork.Repository<HrkPlayerEventAllTimeRecord>().Query()
                .SingleAsync(a => a.PlayerId == player.Id && a.EventId == ev.Id, ct);
            if (txProgress.RemainingLives <= 0 || txProgress.CurrentFloor != floorNumber)
                throw new BusinessException("TOWER_STATE_CHANGED", "Trạng thái tầng hoặc số mạng đã thay đổi.", BusinessErrorKind.Conflict);
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
                bool alreadyClaimed = await _unitOfWork.ReadOnlyRepository<HrkPlayerEventRewardClaim>().Query().AnyAsync(c => c.PlayerId == player.Id && c.EventPeriodId == currentPeriod.Id && c.ClaimType == "FLOOR_REWARD" && c.TargetId == floorNumber, ct);
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
                var chest = (await GetPeriodChestsAsync(currentPeriod, ct)).FirstOrDefault(c => c.FloorNumber == floorNumber);
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
            activityEvents.RaiseEvent(player, currentPeriod, "TowerBattleCompleted", battleRecord.BattleId,
                new { battleRecord.BattleId, battleRecord.FloorNumber, battleRecord.Result, battleRecord.LivesBefore, battleRecord.LivesAfter }, battleRecord.CreatedOnUtc);
            await _executor.UpdateRunAsync(txProgress, floorNumber, ct);
        }

        return completedResult ?? throw new InvalidOperationException("Giao dịch trận đánh tháp không hoàn tất.");
    }
}
