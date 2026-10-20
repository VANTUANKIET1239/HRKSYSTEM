using GAME.Application.Templates;
using Core.Common.Errors;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Core.Persistence.Repositories;
using GAME.Application.Common.Mappings;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;

namespace GAME.Application.Services;
// All entities below are transient. Never add them to a repository.
public sealed class BattleLabBuildResolver(

    IUnitOfWork store,

    IHeroProgressionStatService progression,

    IHeroStatCalculationService heroStats,

    IItemStatCalculationService itemStats,

    ICombatPowerService power

, IGameTemplateCatalog? templates = null)
{
    private readonly IGameTemplateCatalog _templates = templates ?? new DatabaseTemplateCatalog(store);
    public async Task<BattleLabResolvedBuildDto> ResolveAsync(PlayerHeroDto hero, BattleLabBuildDto build, int team, CancellationToken ct)
    {
        var template = (await _templates.GetAllAsync<HrkHeroTemplate>(ct))
            .Single(h => h.Id == hero.HeroTemplateId);
        var rarity = (await _templates.GetAllAsync<HrkHeroRarityUpgradeConfig>(ct))
            .SingleOrDefault(c => c.RarityId == template.RarityId) ?? throw new ArgumentException($"Missing rarity growth configuration for {hero.Name}.");
        if (build.Level > rarity.MaxLevel)
            throw new BusinessException("INVALID_REQUEST", $"{hero.Name}: maximum level is {rarity.MaxLevel}.", BusinessErrorKind.Validation);
        var starConfigs = (await _templates.GetAllAsync<HrkHeroStarUpgradeConfig>(ct))
            .Where(c => c.RarityId == template.RarityId && c.IsEnabled && c.NextStar <= build.Stars)
            .OrderBy(c => c.NextStar)
            .ToList();
        var starConfig = starConfigs.SingleOrDefault(c => c.NextStar == build.Stars);
        if (build.Stars > 1 && starConfig == null)
            throw new ArgumentException("Missing star growth configuration.");
        var bonuses = new List<HeroBonusAttributeDto>();
        var pool = (await _templates.GetAllAsync<HrkHeroStarAttributePool>(ct))
            .Where(c => c.RarityId == template.RarityId && c.IsEnabled)
            .OrderBy(c => c.Id)
            .ToList();
        var attributes = (await _templates.GetAllAsync<HrkAttributeType>(ct))
            .ToDictionary(a => a.Id);
        // Same weighted/hash roll rule as star upgrades, with a reproducible lab seed.
        foreach (var config in starConfigs)
        {
            var seed = new Guid(SHA256.HashData(Encoding.UTF8.GetBytes($"{build.RollSeed}:{config.NextStar}"))[..16]);
            for (var i = 0; i < config.ExtraAttributeRollCount; i++)
            {
                var (selected, value) = HeroStarRollCalculator.Roll(pool, seed, i);
                bonuses.Add(new HeroBonusAttributeDto { Code = attributes[selected.AttributeTypeId].Code, Value = value });
            }
        }

        var current = progression.Calculate(template, build.Level, rarity.StatGrowthRate, build.Stars <= 1 ? 0m : starConfig?.GrowthBonusPercent ?? 0m, bonuses.Select(b => (b.Code, b.Value)));
        var entity = new HrkPlayerHero
        {
            Id = hero.Id,
            PlayerId = 0,
            HeroTemplate = template,
            Level = build.Level,
            Stars = build.Stars,
            AuraTier = build.AuraTier,
            CurrentStats = JsonSerializer.Serialize(current)
        };
        var equipped = new List<HrkPlayerInventory>();
        var result = new BattleLabResolvedBuildDto
        {
            Team = team,
            Position = hero.Position!.Value,
            StarBonuses = bonuses
        };
        var categories = new HashSet<int>();
        foreach (var choice in build.Equipment.OrderBy(e => e.ItemTemplateId))
        {
            var item = (await _templates.GetAllAsync<HrkItemTemplate>(ct))
                .SingleOrDefault(i => i.Id == choice.ItemTemplateId);
            if (item == null || !item.Category.IsEquipment || item.LevelReq > build.Level)
                throw new BusinessException("INVALID_REQUEST", $"Equipment {choice.ItemTemplateId} is unavailable for this level.", BusinessErrorKind.Validation);
            if (!categories.Add(item.CategoryId))
                throw new BusinessException("INVALID_REQUEST", "Only one equipment item per category/slot is allowed.", BusinessErrorKind.Validation);
            // Stable attribute order and independent item seed: changing another slot does not reroll this item.
            item.Attributes = item.Attributes.OrderBy(a => a.AttributeTypeId).ToList();
            var factory = new EquipmentInstanceFactory(store, new SeededRandom(unchecked(build.RollSeed * 397 ^ item.Id)), itemStats, power);
            var created = await factory.CreateAsync(0, item, new EquipmentAcquisitionContext { Source = "BATTLE_LAB" }, ct);
            var instance = created.InventoryItem;
            instance.Id = equipped.Count + 1;
            instance.ItemTemplate = item;
            instance.IsEquipped = true;
            instance.EquippedHeroId = hero.Id;
            instance.Enhancement = choice.Enhancement;
            instance.Stars = choice.Stars;
            foreach (var attr in instance.Attributes)
                attr.AttributeType = item.Attributes.Single(a => a.AttributeTypeId == attr.AttributeTypeId).AttributeType;
            created.DroppedDto.CurrentStats = itemStats.CalculateCurrentStats(instance);
            foreach (var attribute in created.DroppedDto.RolledAttributes)
            {
                attribute.CurrentValue = created.DroppedDto.CurrentStats.GetValueOrDefault(attribute.AttributeCode);
                attribute.EnhancementValue = attribute.CurrentValue - attribute.BaseRolledValue;
            }

            created.DroppedDto.CombatPower = 0; // Factory power described +0; final hero power is calculated below.
            equipped.Add(instance);
            result.Equipment.Add(created.DroppedDto);
        }

        var calculated = heroStats.CalculateStats(entity, equipped);
        hero.Stats = calculated.FinalStats;
        hero.Level = build.Level;
        hero.Stars = build.Stars;
        hero.AuraTier = build.AuraTier;
        hero.Power = await power.CalculateAsync(hero.Stats, ct);
        result.Breakdown = calculated.Breakdowns;
        var aura = (await _templates.GetAllAsync<HrkHeroStarAuraConfig>(ct))
            .SingleOrDefault(a => a.HeroTemplateId == hero.HeroTemplateId && a.StarLevel == build.Stars && a.IsActive);
        hero.StarAura = GameDtoMapper.MapStarAura(aura);
        return result;
    }

    private sealed class SeededRandom(int seed) : IRandomService
    {
        private readonly System.Random random = new(seed);
        public double NextDouble() => random.NextDouble();
        public int Next(int minValue, int maxValue) => random.Next(minValue, maxValue);
        public decimal NextDecimal(decimal minValue, decimal maxValue, int decimals = 4) => Math.Round(minValue + (maxValue - minValue) * (decimal)random.NextDouble(), decimals);
    }
}
