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
    private readonly IGamePlayerService _players;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHeroStatCalculationService _heroStatCalculationService;
    private readonly ICombatPowerService _combatPowerService;
    private readonly IFormationPowerQueryService _formationPowerQueryService;
    public PlayerReadModels(IUnitOfWork unitOfWork, IGamePlayerService players, IHeroStatCalculationService heroStatCalculationService, ICombatPowerService combatPowerService, IFormationPowerQueryService formationPowerQueryService)
    {
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
            .Include(ph => ph.HeroTemplate).ThenInclude(ht => ht.Faction)
            .Include(ph => ph.HeroTemplate).ThenInclude(ht => ht.Class)
            .Include(ph => ph.HeroTemplate).ThenInclude(ht => ht.Rarity)// List/card endpoint intentionally excludes the skill-effect graph.
            .AsSplitQuery()
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        // Batch load every equipped item once. Calling GetPlayerHeroDetailAsync for
        // every card used to cause 2+ SQL queries per hero (the N+1 bottleneck).
        var heroIds = playerHeroes.Select(h => h.Id).ToList();
        var equipmentByHeroId = await EquipmentBatchLoader.LoadForHeroesAsync(_unitOfWork, player.Id, heroIds, cancellationToken);
        var powerConfigs = await _combatPowerService.GetConfigsAsync(cancellationToken);
        // Batch load star auras for player heroes (no N+1 query)
        var templateIds = playerHeroes.Select(h => h.HeroTemplateId).Distinct().ToList();
        var starAuraConfigs = await _unitOfWork.ReadOnlyRepository<HrkHeroStarAuraConfig>().Query()
            .Where(c => c.IsActive && templateIds.Contains(c.HeroTemplateId))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
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
            .Include(x => x.HeroTemplate).ThenInclude(ht => ht.Faction)
            .Include(x => x.HeroTemplate).ThenInclude(ht => ht.Class)
            .Include(x => x.HeroTemplate).ThenInclude(ht => ht.Rarity)
            .Include(x => x.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.EffectType)
            .Include(x => x.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.TargetType)
            .Include(x => x.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.Parameters)
            .Include(x => x.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.Scalings).ThenInclude(sc => sc.AttributeType)
            .Include(x => x.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.StatModifiers).ThenInclude(sm => sm.AttributeType)
            .AsSplitQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
        if (ph == null)
            return null;
        var equipments = await EquipmentBatchLoader.LoadForHeroesAsync(_unitOfWork, player.Id, new[] { ph.Id }, cancellationToken);
        equipments.TryGetValue(ph.Id, out var eq);
        var detailClampedStars = (byte)Math.Clamp(ph.Stars, 0, 5);
        var starAura = await _unitOfWork.ReadOnlyRepository<HrkHeroStarAuraConfig>().Query()
            .FirstOrDefaultAsync(c => c.HeroTemplateId == ph.HeroTemplateId && c.StarLevel == detailClampedStars && c.IsActive, cancellationToken);
        var baseHero = GameDtoMapper.MapPlayerHero(ph, starAura);
        if (baseHero == null)
            return null;
        var statResult = _heroStatCalculationService.CalculateStats(ph, eq);
        var starBonusAttributes = await (
            from bonus in _unitOfWork.ReadOnlyRepository<HrkPlayerHeroBonusAttribute>().Query()
            join attribute in _unitOfWork.ReadOnlyRepository<HrkAttributeType>().Query() on bonus.AttributeTypeId equals attribute.Id
            where bonus.PlayerHeroId == ph.Id
            orderby bonus.UnlockedAtStar
            select new HeroBonusAttributeDto
            {
                UnlockedAtStar = bonus.UnlockedAtStar,
                Code = attribute.Code,
                Name = attribute.Name,
                Value = bonus.Value,
                IsPercentage = bonus.IsPercentage
            }

        ).ToListAsync(cancellationToken);
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
        return await _unitOfWork.ReadOnlyRepository<HrkAvatarTemplate>().Query()
            .Where(x => x.IsEnabled)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id)
            .Select(x => new PlayerAvatarTemplateDto { Id = x.Id, Code = x.Code, Name = x.Name, ImagePath = x.ImagePath, IsSelected = player.AvatarType == "TEMPLATE" && player.AvatarTemplateId == x.Id })
            .ToListAsync(cancellationToken);
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
            avatarUrl = await _unitOfWork.ReadOnlyRepository<HrkAvatarTemplate>().Query()
                .Where(x => x.IsEnabled && (x.Id == player.AvatarTemplateId || (player.AvatarTemplateId == null && x.IsDefault)))
                .OrderByDescending(x => x.Id == player.AvatarTemplateId).ThenByDescending(x => x.IsDefault)
                .Select(x => x.ImagePath)
                .FirstOrDefaultAsync(cancellationToken);
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
