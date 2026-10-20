using GAME.Application.Templates;
using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.Common.Mappings;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAME.Application.Services;

public sealed class PlayerReadModels : IPlayerReadModels
{
    private readonly IGameTemplateCatalog _templates;
    private readonly IGamePlayerService _players;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHeroStatCalculationService _heroStatCalculationService;
    private readonly ICombatPowerService _combatPowerService;
    private readonly IFormationPowerQueryService _formationPowerQueryService;
    public PlayerReadModels(IUnitOfWork unitOfWork, IGamePlayerService players, IHeroStatCalculationService heroStatCalculationService, ICombatPowerService combatPowerService, IFormationPowerQueryService formationPowerQueryService, IGameTemplateCatalog? templates = null)
    {
        _templates = templates ?? new DatabaseTemplateCatalog(unitOfWork);
        _unitOfWork = unitOfWork;
        _players = players;
        _heroStatCalculationService = heroStatCalculationService;
        _combatPowerService = combatPowerService;
        _formationPowerQueryService = formationPowerQueryService;
    }

    public async Task<PlayerProfileDto?> GetPlayerProfileAsync(string userId, CancellationToken cancellationToken = default)
    {
        var player = await _players.GetPlayerByUserIdAsync(userId, cancellationToken);
        if (player == null)
            return null;
        return await BuildProfileAsync(player, cancellationToken);
    }

    public async Task<PlayerWalletDto?> GetPlayerWalletAsync(string userId, CancellationToken cancellationToken = default)
    {
        var player = await _players.GetPlayerByUserIdAsync(userId, cancellationToken);
        if (player == null)
            return null;
        var wallet = await _players.GetWalletByPlayerIdAsync(player.Id, cancellationToken) ?? new HrkPlayerWallet
        {
            PlayerId = player.Id,
            Gold = 0,
            Diamonds = 0,
            UpgradeMaterials = 0,
            MaxCapacity = 200,
            UpdatedOn = DateTime.UtcNow
        };
        return new PlayerWalletDto
        {
            PlayerId = wallet.PlayerId,
            Gold = wallet.Gold,
            Diamonds = wallet.Diamonds,
            UpgradeMaterials = wallet.UpgradeMaterials,
            MaxCapacity = wallet.MaxCapacity,
            UpdatedOn = wallet.UpdatedOn
        };
    }

    public async Task<PlayerGameInfoDto?> GetPlayerGameInfoAsync(string userId, CancellationToken cancellationToken = default)
    {
        var player = await _players.GetPlayerByUserIdAsync(userId, cancellationToken);
        if (player == null)
            return null;
        var wallet = await _players.GetWalletByPlayerIdAsync(player.Id, cancellationToken);
        var formationPower = await _formationPowerQueryService.GetDefaultFormationPowerAsync(player.Id, cancellationToken);
        var profile = await BuildProfileAsync(player, cancellationToken, formationPower.TotalPower);
        return new PlayerGameInfoDto
        {
            Profile = profile,
            Wallet = wallet == null ? null : new PlayerWalletDto
            {
                PlayerId = wallet.PlayerId,
                Gold = wallet.Gold,
                Diamonds = wallet.Diamonds,
                UpgradeMaterials = wallet.UpgradeMaterials,
                MaxCapacity = wallet.MaxCapacity,
                UpdatedOn = wallet.UpdatedOn
            },
            SelectedFormationId = formationPower.FormationId,
            SelectedFormationCode = formationPower.FormationCode,
            SelectedFormationName = formationPower.FormationName,
            FormationPower = formationPower.TotalPower
        };
    }

    public async Task<List<PlayerHeroDto>> GetPlayerHeroesAsync(string userId, CancellationToken cancellationToken = default)
    {
        var player = await _players.GetPlayerByUserIdAsync(userId, cancellationToken);
        if (player == null)
            return new List<PlayerHeroDto>();
        var playerHeroes = await _unitOfWork.ReadOnlyRepository<HrkPlayerHero>().Query()
            .Where(ph => ph.PlayerId == player.Id && ph.IsActive)
            .AsSplitQuery()
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        // Batch load every equipped item once. Calling GetPlayerHeroDetailAsync for
        // every card used to cause 2+ SQL queries per hero (the N+1 bottleneck).
        await TemplateHydration.HeroesAsync(_templates, playerHeroes, cancellationToken);
        var heroIds = playerHeroes.Select(h => h.Id).ToList();
        var equipmentByHeroId = await EquipmentBatchLoader.LoadForHeroesAsync(_unitOfWork, player.Id, heroIds, cancellationToken, _templates);
        var powerConfigs = await _combatPowerService.GetConfigsAsync(cancellationToken);
        // Batch load star auras for player heroes (no N+1 query)
        var templateIds = playerHeroes.Select(h => h.HeroTemplateId).Distinct().ToList();
        var starAuraConfigs = (await _templates.GetAllAsync<HrkHeroStarAuraConfig>(cancellationToken))
            .Where(c => c.IsActive && templateIds.Contains(c.HeroTemplateId))
            .ToList();
        var auraLookup = starAuraConfigs.ToDictionary(c => (c.HeroTemplateId, c.StarLevel));
        var result = new List<PlayerHeroDto>(playerHeroes.Count);
        foreach (var hero in playerHeroes)
        {
            var clampedStars = (byte)Math.Clamp(hero.Stars, 0, 5);
            auraLookup.TryGetValue((hero.HeroTemplateId, clampedStars), out var auraCfg);
            var dto = GameDtoMapper.MapPlayerHero(hero, auraCfg, includeSkills: false);
            if (dto == null)
                continue;
            equipmentByHeroId.TryGetValue(hero.Id, out var equipment);
            var stats = _heroStatCalculationService.CalculateStats(hero, equipment).FinalStats;
            dto.Stats = stats;
            dto.Power = _combatPowerService.Calculate(stats, powerConfigs);
            result.Add(dto);
        }

        return result;
    }

    public async Task<PlayerHeroDetailDto?> GetPlayerHeroDetailAsync(string userId, long heroId, CancellationToken cancellationToken = default)
    {
        var player = await _players.GetPlayerByUserIdAsync(userId, cancellationToken);
        if (player == null)
            return null;
        var ph = await _unitOfWork.ReadOnlyRepository<HrkPlayerHero>().Query()
            .Where(x => x.Id == heroId && x.PlayerId == player.Id && x.IsActive)
            .AsSplitQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
        if (ph == null)
            return null;
        await TemplateHydration.HeroesAsync(_templates, new[] { ph }, cancellationToken);
        var equipments = await EquipmentBatchLoader.LoadForHeroesAsync(_unitOfWork, player.Id, new[] { ph.Id }, cancellationToken, _templates);
        equipments.TryGetValue(ph.Id, out var eq);
        var detailClampedStars = (byte)Math.Clamp(ph.Stars, 0, 5);
        var starAura = (await _templates.GetAllAsync<HrkHeroStarAuraConfig>(cancellationToken))
            .FirstOrDefault(c => c.HeroTemplateId == ph.HeroTemplateId && c.StarLevel == detailClampedStars && c.IsActive);
        var baseHero = GameDtoMapper.MapPlayerHero(ph, starAura);
        if (baseHero == null)
            return null;
        var statResult = _heroStatCalculationService.CalculateStats(ph, eq);
        var starBonusAttributes = await TemplateHydration.HeroBonusesAsync(_unitOfWork, _templates, ph.Id, cancellationToken);
        // CurrentStats already contains star bonus values. Move those values from
        // the generic growth bucket into a dedicated STAR source so the hero stat
        // tooltip explains the real origin without changing the calculated total.
        foreach (var bonusGroup in starBonusAttributes.GroupBy(x => x.Code))
        {
            var breakdown = statResult.Breakdowns.FirstOrDefault(x => string.Equals(x.StatCode, bonusGroup.Key, StringComparison.OrdinalIgnoreCase));
            if (breakdown == null)
                continue;
            var starValue = bonusGroup.Sum(x => x.Value);
            breakdown.HeroGrowthValue = Math.Max(0, breakdown.HeroGrowthValue - starValue);
            breakdown.OtherValue += starValue;
            breakdown.Sources.RemoveAll(x => x.SourceType == "HERO_GROWTH");
            if (breakdown.HeroGrowthValue > 0)
            {
                breakdown.Sources.Add(new HeroStatSourceDto { SourceType = "HERO_GROWTH", SourceName = "Tăng trưởng cấp", Value = breakdown.HeroGrowthValue });
            }

            foreach (var bonus in bonusGroup)
            {
                breakdown.Sources.Add(new HeroStatSourceDto { SourceType = "STAR", SourceName = $"Tăng sao {bonus.UnlockedAtStar}", Value = bonus.Value });
            }
        }

        return new PlayerHeroDetailDto
        {
            Id = baseHero.Id,
            HeroTemplateId = baseHero.HeroTemplateId,
            Name = baseHero.Name,
            Avatar = baseHero.Avatar,
            FactionCode = baseHero.FactionCode,
            FactionName = baseHero.FactionName,
            ClassCode = baseHero.ClassCode,
            ClassName = baseHero.ClassName,
            RarityId = baseHero.RarityId,
            RarityCode = baseHero.RarityCode,
            RarityName = baseHero.RarityName,
            RarityColorHex = baseHero.RarityColorHex,
            Level = baseHero.Level,
            Exp = baseHero.Exp,
            MaxExp = baseHero.MaxExp,
            Stars = baseHero.Stars,
            Power = await _combatPowerService.CalculateAsync(statResult.FinalStats, cancellationToken),
            AuraTier = baseHero.AuraTier,
            IsLocked = baseHero.IsLocked,
            IsFavorite = baseHero.IsFavorite,
            Stats = statResult.FinalStats,
            StatBreakdowns = statResult.Breakdowns,
            StarBonusAttributes = starBonusAttributes,
            Skills = baseHero.Skills,
            Equipment = GameDtoMapper.MapHeroEquipment(eq, ph.Id)
        };
    }

    public async Task<List<PlayerAvatarTemplateDto>> GetAvatarTemplatesAsync(string userId, CancellationToken cancellationToken = default)
    {
        var player = await GetRequiredPlayerAsync(userId, cancellationToken);
        return (await _templates.GetAllAsync<HrkAvatarTemplate>(cancellationToken))
            .Where(x => x.IsEnabled)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id)
            .Select(x => new PlayerAvatarTemplateDto { Id = x.Id, Code = x.Code, Name = x.Name, ImagePath = x.ImagePath, IsSelected = player.AvatarType == "TEMPLATE" && player.AvatarTemplateId == x.Id })
            .ToList();
    }

    public async Task<PlayerCustomAvatarDto?> GetCustomAvatarAsync(long playerId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ReadOnlyRepository<HrkPlayerCustomAvatar>().Query()
            .Where(x => x.PlayerId == playerId)
            .Select(x => new PlayerCustomAvatarDto { ImageData = x.ImageData, ContentType = x.ContentType, ContentHash = x.ContentHash })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PlayerProfileDto> BuildProfileAsync(HrkPlayer player, CancellationToken cancellationToken, int? knownFormationPower = null)
    {
        string? avatarUrl = null;
        if (player.AvatarType == "TEMPLATE")
        {
            avatarUrl = (await _templates.GetAllAsync<HrkAvatarTemplate>(cancellationToken))
                .Where(x => x.IsEnabled && (x.Id == player.AvatarTemplateId || (player.AvatarTemplateId == null && x.IsDefault)))
                .OrderByDescending(x => x.Id == player.AvatarTemplateId).ThenByDescending(x => x.IsDefault)
                .Select(x => x.ImagePath)
                .FirstOrDefault();
        }

        var power = knownFormationPower;
        if (!power.HasValue)
        {
            var formationPower = await _formationPowerQueryService.GetDefaultFormationPowerAsync(player.Id, cancellationToken);
            power = formationPower.TotalPower;
        }

        return new PlayerProfileDto
        {
            Id = player.Id,
            UserId = player.UserId,
            PlayerName = player.PlayerName,
            Level = player.Level,
            Exp = player.Exp,
            MaxExp = player.MaxExp,
            Power = power.Value,
            AvatarType = player.AvatarType,
            AvatarTemplateId = player.AvatarTemplateId,
            AvatarUrl = avatarUrl,
            AvatarVersion = new DateTimeOffset(player.UpdatedOn).ToUnixTimeSeconds(),
            CreatedOn = player.CreatedOn,
            UpdatedOn = player.UpdatedOn
        };
    }

    private async Task<HrkPlayer> GetRequiredPlayerAsync(string userId, CancellationToken cancellationToken, bool tracked = false)
    {
        return await _players.GetPlayerByUserIdAsync(userId, cancellationToken, tracked) ?? throw new BusinessException("PLAYER_NOT_FOUND", "Không tìm thấy thông tin người chơi.", BusinessErrorKind.NotFound);
    }
}
