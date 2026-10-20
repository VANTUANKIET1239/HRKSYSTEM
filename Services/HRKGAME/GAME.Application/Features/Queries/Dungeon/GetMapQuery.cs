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

namespace GAME.Application.Features.Queries.Dungeon;

public sealed record GetMapQuery(string UserId, int MapId) : IQuery<BaseResponse<DungeonMapDetailDto>>;

public sealed class GetMapQueryHandler(
    IUnitOfWork unitOfWork,
    IGamePlayerService gamePlayerService,
    IDungeonStarService starService,
    IDungeonRewardService rewardService,
    IDungeonChestReader chestService,
    IFormationPowerQueryService formationPowerQueryService, IGameTemplateCatalog? templates = null)
    : DungeonHandlerSupport(unitOfWork), IQueryHandler<GetMapQuery, BaseResponse<DungeonMapDetailDto>>
{
    private readonly IGameTemplateCatalog _templates = templates ?? new DatabaseTemplateCatalog(unitOfWork);
    private readonly IGamePlayerService _gamePlayerService = gamePlayerService;
    private readonly IDungeonStarService _starService = starService;
    private readonly IDungeonRewardService _rewardService = rewardService;
    private readonly IDungeonChestReader _chestService = chestService;
    private readonly IFormationPowerQueryService _formationPowerQueryService = formationPowerQueryService;
    public async Task<BaseResponse<DungeonMapDetailDto>> Handle(GetMapQuery message, CancellationToken ct) => BaseResponse<DungeonMapDetailDto>.SuccessResponse(await ExecuteAsync(message.UserId, message.MapId, ct));
    private async Task<DungeonMapDetailDto> ExecuteAsync(string userId, int mapId, CancellationToken cancellationToken = default)
    {
        var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, cancellationToken) ?? throw new BusinessException("PLAYER_NOT_FOUND", "Không tìm thấy người chơi.", BusinessErrorKind.NotFound);
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
            throw new BusinessException("DUNGEON_MAP_NOT_FOUND", "Không tìm thấy bản đồ phó bản.", BusinessErrorKind.NotFound);
        var map = await _unitOfWork.ReadOnlyRepository<HrkDungeonMap>().Query()
            .Where(x => x.Id == mapId && x.IsActive)
            .Include(x => x.Stages.Where(s => s.IsActive)).ThenInclude(s => s.Enemies)
            .AsSplitQuery()
            .AsNoTracking()
            .SingleAsync(cancellationToken);
        var heroTemplates = (await _templates.GetAllAsync<HrkHeroTemplate>(cancellationToken)).ToDictionary(t => t.Id);
        foreach (var enemy in map.Stages.SelectMany(s => s.Enemies)) enemy.HeroTemplate = heroTemplates[enemy.HeroTemplateId];
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
            .Select(stage => ToStageDto(stage, map, cleared, baseDto.State is "AVAILABLE" or "COMPLETED", playerPower, stageBestStars.GetValueOrDefault(stage.Id, 0), stagePossibleDrops.GetValueOrDefault(stage.Id, new List<DungeonPossibleDropDto>()))).ToList()
        };
    }

    private static DungeonStageDto ToStageDto(HrkDungeonStage stage, HrkDungeonMap map, HashSet<int> cleared, bool mapAccessible, int playerPower, int bestStars, List<DungeonPossibleDropDto> possibleDrops)
    {
        var previous = map.Stages.SingleOrDefault(x => x.StageNumber == stage.StageNumber - 1);
        var available = stage.StageNumber == 1 || previous != null && cleared.Contains(previous.Id);
        var state = cleared.Contains(stage.Id) ? "CLEARED" : mapAccessible && available ? "AVAILABLE" : "LOCKED";
        var enemies = stage.Enemies.OrderBy(x => x.Position)
            .Select(enemy => new DungeonEnemyDto { Position = enemy.Position, Name = enemy.DisplayName ?? enemy.HeroTemplate.Name, ImagePath = enemy.ImagePath ?? enemy.HeroTemplate.Avatar, Level = enemy.Level, Stars = enemy.Stars, IsBoss = enemy.IsBoss, Power = (int)Math.Round((enemy.HeroTemplate.BaseHp * .25m + enemy.HeroTemplate.BaseAtk * 3.5m + enemy.HeroTemplate.BaseDef * 2m + enemy.HeroTemplate.BaseSpd) * enemy.StatMultiplier) }).ToList();
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
}
