using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Core.Common.Repositories;
using GAME.Application.Common.Mappings;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAME.Infrastructure.Services;

// All entities below are transient. Never add them to a repository.
public sealed class BattleLabBuildResolver(IUnitOfWork store, IHeroProgressionStatService progression,
    IHeroStatCalculationService heroStats, IItemStatCalculationService itemStats, ICombatPowerService power)
{
    public async Task<BattleLabResolvedBuildDto> ResolveAsync(PlayerHeroDto hero, BattleLabBuildDto build, int team, CancellationToken ct)
    {
        var template = await store.ReadOnlyRepository<HrkHeroTemplate>().Query().AsNoTracking()
            .SingleAsync(h => h.Id == hero.HeroTemplateId, ct);
        var rarity = await store.ReadOnlyRepository<HrkHeroRarityUpgradeConfig>().Query().AsNoTracking()
            .SingleOrDefaultAsync(c => c.RarityId == template.RarityId, ct)
            ?? throw new ArgumentException($"Missing rarity growth configuration for {hero.Name}.");
        if (build.Level > rarity.MaxLevel) throw new ArgumentException($"{hero.Name}: maximum level is {rarity.MaxLevel}.");
        var starConfigs = await store.ReadOnlyRepository<HrkHeroStarUpgradeConfig>().Query().AsNoTracking()
            .Where(c => c.RarityId == template.RarityId && c.IsEnabled && c.NextStar <= build.Stars)
            .OrderBy(c => c.NextStar).ToListAsync(ct);
        var starConfig = starConfigs.SingleOrDefault(c => c.NextStar == build.Stars);
        if (build.Stars > 1 && starConfig == null) throw new ArgumentException("Missing star growth configuration.");
        var bonuses = new List<HeroBonusAttributeDto>();
        var pool = await store.ReadOnlyRepository<HrkHeroStarAttributePool>().Query().AsNoTracking()
            .Where(c => c.RarityId == template.RarityId && c.IsEnabled).OrderBy(c => c.Id).ToListAsync(ct);
        var attributes = await store.ReadOnlyRepository<HrkAttributeType>().Query().AsNoTracking().ToDictionaryAsync(a => a.Id, ct);
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
        var current = progression.Calculate(template, build.Level, rarity.StatGrowthRate,
            build.Stars <= 1 ? 0m : starConfig?.GrowthBonusPercent ?? 0m, bonuses.Select(b => (b.Code, b.Value)));
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
        var result = new BattleLabResolvedBuildDto { Team = team, Position = hero.Position!.Value, StarBonuses = bonuses };
        var categories = new HashSet<int>();
        foreach (var choice in build.Equipment.OrderBy(e => e.ItemTemplateId))
        {
            var item = await store.ReadOnlyRepository<HrkItemTemplate>().Query()
                .Include(i => i.Category).Include(i => i.Rarity).Include(i => i.Attributes).ThenInclude(a => a.AttributeType)
                .AsNoTracking().SingleOrDefaultAsync(i => i.Id == choice.ItemTemplateId, ct);
            if (item == null || !item.Category.IsEquipment || item.LevelReq > build.Level)
                throw new ArgumentException($"Equipment {choice.ItemTemplateId} is unavailable for this level.");
            if (!categories.Add(item.CategoryId)) throw new ArgumentException("Only one equipment item per category/slot is allowed.");
            // Stable attribute order and independent item seed: changing another slot does not reroll this item.
            item.Attributes = item.Attributes.OrderBy(a => a.AttributeTypeId).ToList();
            var factory = new EquipmentInstanceFactory(store, new SeededRandom(unchecked(build.RollSeed * 397 ^ item.Id)), itemStats, power);
            var created = await factory.CreateAsync(0, item, new EquipmentAcquisitionContext { Source = "BATTLE_LAB" }, ct);
            var instance = created.InventoryItem;
            instance.Id = equipped.Count + 1; instance.ItemTemplate = item;
            instance.IsEquipped = true; instance.EquippedHeroId = hero.Id;
            instance.Enhancement = choice.Enhancement; instance.Stars = choice.Stars;
            foreach (var attr in instance.Attributes)
                attr.AttributeType = item.Attributes.Single(a => a.AttributeTypeId == attr.AttributeTypeId).AttributeType;
            created.DroppedDto.CurrentStats = itemStats.CalculateCurrentStats(instance);
            foreach (var attribute in created.DroppedDto.RolledAttributes)
            {
                attribute.CurrentValue = created.DroppedDto.CurrentStats.GetValueOrDefault(attribute.AttributeCode);
                attribute.EnhancementValue = attribute.CurrentValue - attribute.BaseRolledValue;
            }
            created.DroppedDto.CombatPower = 0; // Factory power described +0; final hero power is calculated below.
            equipped.Add(instance); result.Equipment.Add(created.DroppedDto);
        }
        var calculated = heroStats.CalculateStats(entity, equipped);
        hero.Stats = calculated.FinalStats; hero.Level = build.Level; hero.Stars = build.Stars; hero.AuraTier = build.AuraTier;
        hero.Power = await power.CalculateAsync(hero.Stats, ct);
        result.Breakdown = calculated.Breakdowns;
        var aura = await store.ReadOnlyRepository<HrkHeroStarAuraConfig>().Query().AsNoTracking()
            .SingleOrDefaultAsync(a => a.HeroTemplateId == hero.HeroTemplateId && a.StarLevel == build.Stars && a.IsActive, ct);
        hero.StarAura = GameDtoMapper.MapStarAura(aura);
        return result;
    }

    private sealed class SeededRandom(int seed) : IRandomService
    {
        private readonly System.Random random = new(seed);
        public double NextDouble() => random.NextDouble();
        public int Next(int minValue, int maxValue) => random.Next(minValue, maxValue);
        public decimal NextDecimal(decimal minValue, decimal maxValue, int decimals = 4) =>
            Math.Round(minValue + (maxValue - minValue) * (decimal)random.NextDouble(), decimals);
    }
}
