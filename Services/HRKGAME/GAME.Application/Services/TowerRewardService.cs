using GAME.Application.Templates;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GAME.Application.Services;

public sealed class TowerRewardService
{
    private readonly IGameTemplateCatalog _templates;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGamePlayerService _gamePlayerService;
    private readonly IEquipmentInstanceFactory _equipmentInstanceFactory;
    private readonly ILevelExperienceService _levelExperienceService;
    private readonly IHeroProgressionStatService _progressionStats;
    private readonly ICombatPowerService _combatPower;
    private readonly GameRewardGranter _rewards;
    public TowerRewardService(IUnitOfWork unitOfWork, IGamePlayerService gamePlayerService, IEquipmentInstanceFactory equipmentInstanceFactory, ILevelExperienceService levelExperienceService, IHeroProgressionStatService progressionStats, ICombatPowerService combatPower, IGameTemplateCatalog? templates = null, GameRewardGranter? rewards = null)
    {
        _templates = templates ?? new DatabaseTemplateCatalog(unitOfWork);
        _unitOfWork = unitOfWork;
        _gamePlayerService = gamePlayerService;
        _equipmentInstanceFactory = equipmentInstanceFactory;
        _levelExperienceService = levelExperienceService;
        _progressionStats = progressionStats;
        _combatPower = combatPower;
        _rewards = rewards ?? new GameRewardGranter(unitOfWork, gamePlayerService, equipmentInstanceFactory, _templates);
    }

    public Task<(List<GenericRewardItemDto> granted, List<GenericRewardItemDto> pending, bool isBagFull)> GrantGenericRewardsAsync(long playerId, List<GenericRewardItemDto> rewards, long eventPeriodId, string sourceDesc, CancellationToken ct, bool createPending = true, bool experienceHandled = false) =>
        _rewards.GrantAsync(playerId, rewards, "TOWER_REWARD", ct, eventPeriodId, sourceDesc, createPending, experienceHandled);
    public static (int Player, int Hero) GetExperience(IEnumerable<GenericRewardItemDto> rewards) => (rewards.Where(r => r.Type == "PLAYER_EXP").Sum(r => r.Quantity), rewards.Where(r => r.Type == "HERO_EXP").Sum(r => r.Quantity));
    public static List<GenericRewardItemDto> ParseRewards(string? json) => GameRewardGranter.ParseRewards(json);
    public static string? ResolveItemCode(GenericRewardItemDto reward) => GameRewardGranter.ResolveItemCode(reward);
    public async Task<List<HeroExpResultDto>> AddParticipantHeroExp(long playerId, IReadOnlyCollection<long> participantHeroIds, int exp, CancellationToken ct)
    {
        if (participantHeroIds == null || participantHeroIds.Count == 0 || exp <= 0)
            return new();
        var heroes = await _unitOfWork.Repository<HrkPlayerHero>().Query()
            .Where(x => x.PlayerId == playerId && participantHeroIds.Contains(x.Id))
            .ToListAsync(ct);
        var templates = (await _templates.GetAllAsync<HrkHeroTemplate>(ct)).ToDictionary(x => x.Id);
        var results = new List<HeroExpResultDto>();
        foreach (var hero in heroes)
        {
            var template = templates[hero.HeroTemplateId];
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

            if (hero.Level != item.OldLevel)
            {
                var growth = (await _templates.GetAllAsync<HrkHeroRarityUpgradeConfig>(ct))
                    .Single(c => c.RarityId == template.RarityId);
                var starGrowth = (await _templates.GetAllAsync<HrkHeroStarUpgradeConfig>(ct))
                    .Where(c => c.RarityId == template.RarityId && c.NextStar == hero.Stars && c.IsEnabled)
                    .Select(c => c.GrowthBonusPercent)
                    .FirstOrDefault();
                var bonuses = await TemplateHydration.HeroBonusesAsync(_unitOfWork, _templates, hero.Id, ct);
                var stats = _progressionStats.Calculate(template, hero.Level, growth.StatGrowthRate, starGrowth, bonuses.Select(b => (b.Code, b.Value)));
                hero.CurrentStats = JsonSerializer.Serialize(stats);
                hero.Power = await _combatPower.CalculateAsync(stats, ct);
            }

            item.NewLevel = hero.Level;
            item.NewExp = hero.Exp;
            item.NewMaxExp = hero.MaxExp;
            hero.UpdatedOn = DateTime.UtcNow;
            results.Add(item);
        }

        return results;
    }

    public async Task AddPlayerExpAsync(HrkPlayer player, int exp, CancellationToken ct)
    {
        if (exp <= 0)
            return;
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
}
