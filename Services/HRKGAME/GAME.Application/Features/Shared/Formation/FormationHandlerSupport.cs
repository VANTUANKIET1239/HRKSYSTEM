using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.Common.Mappings;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using GAME.Application.Services;

namespace GAME.Application.Features.Shared.Formation;

public abstract class FormationHandlerSupport
{
    protected readonly IUnitOfWork _unitOfWork;
    protected readonly IGamePlayerService _gamePlayerService;
    protected readonly IFormationStatService _formationStatService;
    protected readonly ICombatPowerService _combatPowerService;
    protected readonly IHeroStatCalculationService _heroStatCalculationService;
    protected FormationHandlerSupport(IUnitOfWork unitOfWork, IGamePlayerService gamePlayerService, IFormationStatService formationStatService, ICombatPowerService combatPowerService, IHeroStatCalculationService heroStatCalculationService)
    {
        this._unitOfWork = unitOfWork;
        this._gamePlayerService = gamePlayerService;
        this._formationStatService = formationStatService;
        this._combatPowerService = combatPowerService;
        this._heroStatCalculationService = heroStatCalculationService;
    }

    protected async Task<HrkPlayer> GetPlayerRequiredAsync(string userId, CancellationToken cancellationToken)
    {
        return await _gamePlayerService.GetPlayerByUserIdAsync(userId, cancellationToken) ?? throw new BusinessException("PLAYER_NOT_FOUND", "Không tìm thấy thông tin người chơi tương ứng với tài khoản đăng nhập.", BusinessErrorKind.NotFound);
    }

    protected async Task<(Dictionary<long, HrkPlayerHero> Heroes, Dictionary<long, HrkPlayerEquipment> Equipments)> LoadHeroesAndEquipmentsAsync(long playerId, List<long> heroIds, CancellationToken cancellationToken)
    {
        if (heroIds.Count == 0)
            return (new Dictionary<long, HrkPlayerHero>(), new Dictionary<long, HrkPlayerEquipment>());
        var heroes = await _unitOfWork.ReadOnlyRepository<HrkPlayerHero>().Query()
            .Where(h => h.PlayerId == playerId && h.IsActive && heroIds.Contains(h.Id))
            .Include(h => h.HeroTemplate).ThenInclude(ht => ht.Faction)
            .Include(h => h.HeroTemplate).ThenInclude(ht => ht.Class)
            .Include(h => h.HeroTemplate).ThenInclude(ht => ht.Rarity)
            .Include(h => h.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.EffectType)
            .Include(h => h.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.TargetType)
            .Include(h => h.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.Parameters)
            .Include(h => h.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.Scalings).ThenInclude(sc => sc.AttributeType)
            .Include(h => h.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.StatModifiers).ThenInclude(sm => sm.AttributeType)
            .AsSplitQuery()
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var equipments = await EquipmentBatchLoader.LoadForHeroesAsync(_unitOfWork, playerId, heroIds, cancellationToken);
        return (heroes.ToDictionary(h => h.Id), equipments);
    }

    protected async Task<FormationUpgradeCostDto?> BuildUpgradeCostAsync(long playerId, HrkFormationTemplate tmpl, int currentLevel, HrkFormationLevelConfig? currentCfg, CancellationToken cancellationToken)
    {
        if (currentLevel >= tmpl.MaxLevel || currentCfg == null)
        {
            return new FormationUpgradeCostDto
            {
                GoldCost = 0,
                StoneCost = 0,
                StoneItemTemplateId = 0,
                PlayerGold = 0,
                PlayerStones = 0,
                CanUpgrade = false,
                CannotUpgradeReason = "Trận pháp đã đạt cấp độ tối đa."
            };
        }

        var wallet = await _gamePlayerService.GetWalletByPlayerIdAsync(playerId, cancellationToken);
        long playerGold = wallet?.Gold ?? 0;
        int stoneCount = await _unitOfWork.ReadOnlyRepository<HrkPlayerInventory>().Query()
            .Where(i => i.PlayerId == playerId && i.ItemTemplateId == currentCfg.StoneItemTemplateId && i.IsActive).SumAsync(i => i.Count, cancellationToken);
        bool canUpgrade = true;
        string? reason = null;
        if (playerGold < currentCfg.GoldCost)
        {
            canUpgrade = false;
            reason = $"Không đủ Vàng ({playerGold:N0}/{currentCfg.GoldCost:N0}).";
        }
        else if (stoneCount < currentCfg.StoneCost)
        {
            canUpgrade = false;
            reason = $"Không đủ Đá Trận Pháp ({stoneCount}/{currentCfg.StoneCost}).";
        }

        return new FormationUpgradeCostDto
        {
            GoldCost = currentCfg.GoldCost,
            StoneCost = currentCfg.StoneCost,
            StoneItemTemplateId = currentCfg.StoneItemTemplateId,
            PlayerGold = playerGold,
            PlayerStones = stoneCount,
            CanUpgrade = canUpgrade,
            CannotUpgradeReason = reason
        };
    }

    protected static List<long> GetPlacedHeroIds(HrkPlayerFormation pf)
    {
        var list = new List<long>();
        if (pf.Position1.HasValue && pf.Position1.Value > 0)
            list.Add(pf.Position1.Value);
        if (pf.Position2.HasValue && pf.Position2.Value > 0)
            list.Add(pf.Position2.Value);
        if (pf.Position3.HasValue && pf.Position3.Value > 0)
            list.Add(pf.Position3.Value);
        if (pf.Position4.HasValue && pf.Position4.Value > 0)
            list.Add(pf.Position4.Value);
        if (pf.Position5.HasValue && pf.Position5.Value > 0)
            list.Add(pf.Position5.Value);
        return list;
    }

    protected static long? GetHeroIdBySlot(HrkPlayerFormation pf, int slot) => slot switch
    {
        1 => pf.Position1,
        2 => pf.Position2,
        3 => pf.Position3,
        4 => pf.Position4,
        5 => pf.Position5,
        _ => null
    };
    protected async Task<FormationDetailDto?> GetFormationDetailAsync(string userId, string formationCode, CancellationToken cancellationToken = default)
    {
        var player = await GetPlayerRequiredAsync(userId, cancellationToken);
        var tmpl = await _unitOfWork.ReadOnlyRepository<HrkFormationTemplate>().Query()
            .Where(t => t.Code == formationCode && t.IsEnabled)
            .Include(t => t.Slots.OrderBy(s => s.Slot))
            .Include(t => t.LevelConfigs.OrderBy(c => c.Level))
            .FirstOrDefaultAsync(cancellationToken);
        if (tmpl == null)
            return null;
        var pf = await _unitOfWork.ReadOnlyRepository<HrkPlayerFormation>().Query()
            .FirstOrDefaultAsync(f => f.PlayerId == player.Id && f.FormationTemplateId == tmpl.Id && f.IsActive, cancellationToken);
        int currentLevel = pf?.Level ?? 1;
        bool isSelected = pf?.IsSelected ?? false;
        var currentCfg = tmpl.LevelConfigs.FirstOrDefault(c => c.Level == currentLevel);
        var nextCfg = tmpl.LevelConfigs.FirstOrDefault(c => c.Level == currentLevel + 1);
        var currentBonus = _formationStatService.ParseBonus(currentCfg?.StatBonusJson);
        var nextBonus = nextCfg != null ? _formationStatService.ParseBonus(nextCfg.StatBonusJson) : null;
        // Load hero & equipment
        var placedHeroIds = pf != null ? GetPlacedHeroIds(pf) : new List<long>();
        var (heroes, equipments) = await LoadHeroesAndEquipmentsAsync(player.Id, placedHeroIds, cancellationToken);
        var powerConfigs = await _combatPowerService.GetConfigsAsync(cancellationToken);
        var heroEquipmentList = placedHeroIds.Where(id => heroes.ContainsKey(id))
            .Select(id => (heroes[id], equipments.GetValueOrDefault(id))).ToList();
        var powerResult = _formationStatService.CalculateFormationPower(heroEquipmentList, currentBonus, powerConfigs);
        // Xây dựng 5 slots
        var slotDtos = new List<FormationSlotDto>();
        for (int slotNum = 1; slotNum <= 5; slotNum++)
        {
            var slotTmpl = tmpl.Slots.FirstOrDefault(s => s.Slot == slotNum);
            long? heroId = pf != null ? GetHeroIdBySlot(pf, slotNum) : null;
            PlayerHeroDto? heroDto = null;
            if (heroId.HasValue && heroes.TryGetValue(heroId.Value, out var heroEntity))
            {
                heroDto = GameDtoMapper.MapPlayerHero(heroEntity);
                if (heroDto != null)
                {
                    heroDto.Stats = powerResult.HeroFormationStats.GetValueOrDefault(heroId.Value) ?? _formationStatService.ApplyFormationBonus(_heroStatCalculationService.CalculateStats(heroEntity, equipments.GetValueOrDefault(heroId.Value)).FinalStats, currentBonus);
                    heroDto.Power = powerResult.HeroTotalPowers.GetValueOrDefault(heroId.Value);
                }
            }

            slotDtos.Add(new FormationSlotDto { Slot = slotNum, RowType = slotTmpl?.RowType ?? (slotNum % 2 == 1 ? "FRONT" : "BACK"), Lane = slotTmpl?.Lane ?? slotNum, DisplayX = slotTmpl?.DisplayX ?? slotNum, DisplayY = slotTmpl?.DisplayY ?? (slotNum % 2 == 1 ? 2 : 1), Hero = heroDto });
        }

        // Chi phí nâng cấp
        var upgradeCost = await BuildUpgradeCostAsync(player.Id, tmpl, currentLevel, currentCfg, cancellationToken);
        return new FormationDetailDto
        {
            TemplateId = tmpl.Id,
            Code = tmpl.Code,
            Name = tmpl.Name,
            Description = tmpl.Description,
            ImagePath = tmpl.ImagePath,
            Level = currentLevel,
            MaxLevel = tmpl.MaxLevel,
            IsSelected = isSelected,
            IsUnlocked = true,
            Slots = slotDtos,
            BaseHeroPower = powerResult.BaseHeroPower,
            FormationBonusPower = powerResult.FormationBonusPower,
            TotalPower = powerResult.TotalPower,
            CurrentBonus = currentBonus,
            NextLevelBonus = nextBonus,
            UpgradeCost = upgradeCost
        };
    }
}
