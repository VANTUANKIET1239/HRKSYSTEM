using GAME.Application.Templates;
using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Features.Shared.Dungeon;

namespace GAME.Application.Features.Commands.Dungeon;

public sealed record StartStageCommand(

    string UserId,

    int StageId,

    StartDungeonStageRequestDto Request

)

    : ICommand<BaseResponse<StartDungeonStageResultDto>>;

public sealed class StartStageCommandHandler(
    IUnitOfWork unitOfWork,
    IGamePlayerService gamePlayerService,
    IBattleExecutor battleService,
    IDungeonStarService starService,
    IDungeonRewardService rewardService,
    IFormationSnapshotService formationSnapshotService,
    ILevelExperienceService levelExperienceService, IGameTemplateCatalog? templates = null)
    : DungeonHandlerSupport(unitOfWork), ICommandHandler<StartStageCommand, BaseResponse<StartDungeonStageResultDto>>
{
    private readonly IGameTemplateCatalog _templates = templates ?? new DatabaseTemplateCatalog(unitOfWork);
    private readonly IGamePlayerService _gamePlayerService = gamePlayerService;
    private readonly IBattleExecutor _battleService = battleService;
    private readonly IDungeonStarService _starService = starService;
    private readonly IDungeonRewardService _rewardService = rewardService;
    private readonly IFormationSnapshotService _formationSnapshotService = formationSnapshotService;
    private readonly ILevelExperienceService _levelExperienceService = levelExperienceService;
    public async Task<BaseResponse<StartDungeonStageResultDto>> Handle(StartStageCommand message, CancellationToken ct) => BaseResponse<StartDungeonStageResultDto>.SuccessResponse(await ExecuteAsync(message.UserId, message.StageId, message.Request, ct));
    private async Task<StartDungeonStageResultDto> ExecuteAsync(string userId, int stageId, StartDungeonStageRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ClientRequestId))
            throw new BusinessException("INVALID_REQUEST_ID", "ClientRequestId là bắt buộc.", BusinessErrorKind.Validation);
        var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, cancellationToken, tracked: true) ?? throw new BusinessException("PLAYER_NOT_FOUND", "Không tìm thấy người chơi.", BusinessErrorKind.NotFound);
        ApplyStaminaRecovery(player);
        var duplicate = await _unitOfWork.ReadOnlyRepository<HrkDungeonRun>().Query().AnyAsync(x => x.PlayerId == player.Id && x.ClientRequestId == request.ClientRequestId, cancellationToken);
        if (duplicate)
            throw new BusinessException("DUPLICATE_REQUEST", "Yêu cầu chiến đấu này đã được xử lý.", BusinessErrorKind.Conflict);
        var stage = await _unitOfWork.ReadOnlyRepository<HrkDungeonStage>().Query()
            .Include(x => x.DungeonMap)
            .Include(x => x.Enemies)
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == stageId && x.IsActive, cancellationToken) ?? throw new BusinessException("DUNGEON_STAGE_NOT_FOUND", "Không tìm thấy màn phó bản.", BusinessErrorKind.NotFound);
        var cleared = await ClearedStageIds(player.Id, cancellationToken);
        await ValidateStageAccess(stage, player, cleared, cancellationToken);
        if (player.Stamina < stage.StaminaCost)
            throw new BusinessException("INSUFFICIENT_STAMINA", "Không đủ thể lực để vào phó bản.", BusinessErrorKind.Conflict);
        // Check if inventory is full before battle
        bool isBagFullBeforeBattle = await _rewardService.IsBagFullAsync(player.Id, cancellationToken);
        if (isBagFullBeforeBattle)
        {
            throw new BusinessException("INVENTORY_FULL", "Hành trang đã đầy. Vui lòng dọn dẹp hoặc mở rộng hành trang trước khi vào phó bản.", BusinessErrorKind.Conflict);
        }

        // Build snapshot from formation draft (validates formation, slots, hero ownership)
        var snapshot = await _formationSnapshotService.BuildAsync(player.Id, request.FormationCode, request.Positions, cancellationToken);
        if (snapshot.Heroes.Count == 0)
        {
            throw new BusinessException("EMPTY_FORMATION", "Đội hình xuất chiến không có võ tướng nào.", BusinessErrorKind.Conflict);
        }

        StartDungeonStageResultDto? completedResult = null;
        await _unitOfWork.ExecuteStrategyAsync(async () =>
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var battle = await _battleService.ExecuteAsync(userId, new StartBattleRequestDto { BattleType = "CAMPAIGN", StageId = stage.Id, FormationCode = request.FormationCode, Positions = request.Positions }, cancellationToken);
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
                    var wallet = await _gamePlayerService.GetWalletByPlayerIdAsync(player.Id, cancellationToken) ?? throw new InvalidOperationException("Không tìm thấy ví người chơi.");
                    result.GoldGained = stage.GoldReward + (firstClear ? stage.FirstClearGoldReward : 0);
                    wallet.Gold += result.GoldGained;
                    wallet.UpdatedOn = DateTime.UtcNow;
                    result.PlayerExpGained = stage.PlayerExpReward;
                    await AddPlayerExpAsync(player, stage.PlayerExpReward, cancellationToken);
                    result.NewPlayerLevel = player.Level;
                    result.NewPlayerExp = player.Exp;
                    result.Heroes = await AddParticipantHeroExp(player.Id, snapshot.ParticipantHeroIds, stage.HeroExpReward, cancellationToken);
                    // 1. Calculate & record stage stars (non-regressive)
                    var (earnedStars, prevBestStars, bestStars, isNewRecord, remainingHpRate) = await RecordStageProgressAsync(player.Id, stage.Id, battle, cancellationToken);
                    result.EarnedStars = earnedStars;
                    result.PreviousBestStars = prevBestStars;
                    result.BestStars = bestStars;
                    result.IsNewStarRecord = isNewRecord;
                    result.RemainingHpRate = remainingHpRate;
                    // 2. Roll boss equipment drop
                    var (droppedEquip, bagFullDuringDrop) = await _rewardService.RollEquipmentDropAsync(player.Id, stage.Id, firstClear, cancellationToken);
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
                    result.BestStars = (await _starService.GetStageBestStarsAsync(player.Id, new[] { stage.Id }, cancellationToken)).GetValueOrDefault(stage.Id, 0);
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
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                completedResult = new StartDungeonStageResultDto
                {
                    Battle = battle,
                    Result = result
                };
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }, cancellationToken);
        return completedResult ?? throw new InvalidOperationException("Dungeon battle transaction did not complete.");
    }

    private async Task ValidateStageAccess(HrkDungeonStage stage, HrkPlayer player, HashSet<int> cleared, CancellationToken ct)
    {
        if (player.Level < stage.DungeonMap.RequiredPlayerLevel)
            throw new BusinessException("PLAYER_LEVEL_REQUIRED", $"Cần đạt cấp {stage.DungeonMap.RequiredPlayerLevel}.", BusinessErrorKind.Conflict);
        if (stage.StageNumber > 1)
        {
            var previousStageId = await _unitOfWork.ReadOnlyRepository<HrkDungeonStage>().Query()
                .Where(x => x.DungeonMapId == stage.DungeonMapId && x.StageNumber == stage.StageNumber - 1)
                .Select(x => x.Id)
                .SingleAsync(ct);
            if (!cleared.Contains(previousStageId))
                throw new BusinessException("PREVIOUS_STAGE_NOT_CLEARED", "Bạn chưa vượt qua màn trước.", BusinessErrorKind.Conflict);
        }

        if (stage.DungeonMap.PreviousMapId.HasValue && stage.StageNumber == 1)
        {
            var previousBossId = await _unitOfWork.ReadOnlyRepository<HrkDungeonStage>().Query()
                .Where(x => x.DungeonMapId == stage.DungeonMap.PreviousMapId.Value)
                .OrderByDescending(x => x.StageNumber)
                .Select(x => x.Id).FirstAsync(ct);
            if (!cleared.Contains(previousBossId))
                throw new BusinessException("PREVIOUS_MAP_NOT_CLEARED", "Bạn chưa hoàn thành bản đồ trước.", BusinessErrorKind.Conflict);
        }
    }

    private async Task<List<HeroExpResultDto>> AddParticipantHeroExp(long playerId, IReadOnlyCollection<long> participantHeroIds, int exp, CancellationToken ct)
    {
        if (participantHeroIds == null || participantHeroIds.Count == 0)
            return new();
        var heroes = await _unitOfWork.Repository<HrkPlayerHero>().Query()
            .Where(x => x.PlayerId == playerId && participantHeroIds.Contains(x.Id))
            .ToListAsync(ct);
        var heroTemplates = (await _templates.GetAllAsync<HrkHeroTemplate>(ct)).ToDictionary(t => t.Id);
        var results = new List<HeroExpResultDto>();
        foreach (var hero in heroes)
        {
            var template = heroTemplates[hero.HeroTemplateId];
            var oldMaxExp = hero.MaxExp;
            var item = new HeroExpResultDto
            {
                PlayerHeroId = hero.Id,
                HeroName = template.Name,
                Avatar = template.Avatar ?? string.Empty,
                ExpGained = exp,
                OldLevel = hero.Level,
                OldExp = hero.Exp,
                OldMaxExp = oldMaxExp
            };
            var requirement = await _levelExperienceService.GetHeroRequirementAsync(hero.Level, ct);
            hero.MaxExp = requirement.IsMaxLevel ? 0 : requirement.RequiredExp;
            if (!requirement.IsMaxLevel)
                hero.Exp += exp;
            while (!requirement.IsMaxLevel && hero.MaxExp > 0 && hero.Exp >= hero.MaxExp)
            {
                hero.Exp -= hero.MaxExp;
                hero.Level++;
                requirement = await _levelExperienceService.GetHeroRequirementAsync(hero.Level, ct);
                hero.MaxExp = requirement.IsMaxLevel ? 0 : requirement.RequiredExp;
                if (requirement.IsMaxLevel)
                    hero.Exp = 0;
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
        if (!requirement.IsMaxLevel)
            player.Exp += exp;
        while (!requirement.IsMaxLevel && player.MaxExp > 0 && player.Exp >= player.MaxExp)
        {
            player.Exp -= player.MaxExp;
            player.Level++;
            requirement = await _levelExperienceService.GetPlayerRequirementAsync(player.Level, ct);
            player.MaxExp = requirement.IsMaxLevel ? 0 : requirement.RequiredExp;
            if (requirement.IsMaxLevel)
                player.Exp = 0;
        }

        player.UpdatedOn = DateTime.UtcNow;
    }

    private async Task<int?> FindNextStageId(HrkDungeonStage stage, CancellationToken ct) => await _unitOfWork.ReadOnlyRepository<HrkDungeonStage>().Query()
        .Where(x => x.IsActive && (x.DungeonMapId == stage.DungeonMapId && x.StageNumber == stage.StageNumber + 1 || x.DungeonMap.DisplayOrder == stage.DungeonMap.DisplayOrder + 1 && x.StageNumber == 1))
        .OrderBy(x => x.DungeonMap.DisplayOrder).ThenBy(x => x.StageNumber)
        .Select(x => (int?)x.Id)
        .FirstOrDefaultAsync(ct);
    private async Task<(int earnedStars, int previousBestStars, int bestStars, bool isNewRecord, decimal remainingHpRate)> RecordStageProgressAsync(long playerId, int stageId, StartBattleResultDto battle, CancellationToken cancellationToken = default)
    {
        var mapId = await _unitOfWork.ReadOnlyRepository<HrkDungeonStage>().Query()
            .Where(x => x.Id == stageId)
            .Select(x => x.DungeonMapId)
            .SingleAsync(cancellationToken);
        var (earnedStars, remainingHpRate) = await _starService.CalculateEarnedStarsAsync(mapId, battle, cancellationToken);
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
}
