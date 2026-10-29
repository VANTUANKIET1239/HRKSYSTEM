using Core.Persistence.Repositories;
using GAME.Application.Interfaces;
using GAME.Application.Templates;
using GAME.Domain.Entities;
using System.Text.Json;

namespace GAME.Application.Services;

// Both summon and account provisioning use the same level-1/star-1 creation rules.
public sealed class NewPlayerHeroFactory(IUnitOfWork unitOfWork, IHeroProgressionStatService stats,
    ICombatPowerService power, ILevelExperienceService levels, ISummonRandom random,
    IGameTemplateCatalog? templates = null)
{
    private readonly IGameTemplateCatalog _templates = templates ?? new DatabaseTemplateCatalog(unitOfWork);

    public async Task<HrkPlayerHero> CreateAsync(long playerId, HrkHeroTemplate template, CancellationToken ct)
    {
        var growth = (await _templates.GetAllAsync<HrkHeroRarityUpgradeConfig>(ct))
            .SingleOrDefault(x => x.RarityId == template.RarityId)
            ?? throw new InvalidOperationException("Missing hero progression configuration.");
        var pool = (await _templates.GetAllAsync<HrkHeroStarAttributePool>(ct))
            .Where(x => x.IsEnabled && x.RarityId == template.RarityId).OrderBy(x => x.Id).ToList();
        var codes = (await _templates.GetAllAsync<HrkAttributeType>(ct)).ToDictionary(x => x.Id, x => x.Code);
        if (pool.Count == 0 || pool.Any(x => x.Weight <= 0 || x.MinValue > x.MaxValue ||
            !codes.TryGetValue(x.AttributeTypeId, out var code) ||
            code is not ("CRIT_RATE" or "CRIT_DAMAGE" or "LIFESTEAL" or "ACCURACY" or "RESISTANCE")))
            throw new InvalidOperationException("Missing or invalid initial star attribute pool.");
        var roll = random.Next(checked(pool.Sum(x => x.Weight)));
        var attribute = pool[0];
        foreach (var candidate in pool)
        {
            if (roll < candidate.Weight) { attribute = candidate; break; }
            roll -= candidate.Weight;
        }
        var value = Math.Round(attribute.MinValue + (attribute.MaxValue - attribute.MinValue) * random.Next(1_000_000) / 1_000_000m, 4);
        var calculated = stats.Calculate(template, 1, growth.StatGrowthRate, 0, [(codes[attribute.AttributeTypeId], value)]);
        var exp = await levels.GetHeroRequirementAsync(1, ct);
        var hero = new HrkPlayerHero
        {
            PlayerId = playerId, HeroTemplateId = template.Id, Level = 1, Stars = 1, AuraTier = 1,
            MaxExp = exp.IsMaxLevel ? 0 : exp.RequiredExp, CurrentStats = JsonSerializer.Serialize(calculated),
            Power = await power.CalculateAsync(calculated, ct)
        };
        await unitOfWork.Repository<HrkPlayerHero>().AddAsync(hero);
        await unitOfWork.Repository<HrkPlayerHeroBonusAttribute>().AddAsync(new HrkPlayerHeroBonusAttribute
        {
            PlayerHero = hero, UnlockedAtStar = 1, AttributeTypeId = attribute.AttributeTypeId,
            Value = value, IsPercentage = attribute.IsPercentage, RollSeed = Guid.NewGuid()
        });
        return hero;
    }
}
