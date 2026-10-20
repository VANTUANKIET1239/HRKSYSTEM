using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAME.Application.Templates;

/// <summary>Hydrates detached read models only. Never use this with tracked command entities.</summary>
public static class TemplateHydration
{
    public static async Task<Dictionary<int, HrkItemTemplate>> ItemTemplatesAsync(IGameTemplateCatalog catalog, CancellationToken ct) =>
        (await catalog.GetAllAsync<HrkItemTemplate>(ct)).ToDictionary(t => t.Id);

    public static async Task<HrkPlayerInventory> InventoryViewAsync(IGameTemplateCatalog catalog, HrkPlayerInventory item, CancellationToken ct)
    {
        // Calculation-only copy: cached navigation objects never become part of a tracked write graph.
        var copy = new HrkPlayerInventory
        {
            Id = item.Id, PlayerId = item.PlayerId, ItemTemplateId = item.ItemTemplateId, Count = item.Count,
            Enhancement = item.Enhancement, Stars = item.Stars, IsEquipped = item.IsEquipped,
            EquippedHeroId = item.EquippedHeroId, IsLocked = item.IsLocked, IsActive = item.IsActive,
            EnhancementGrowthPercent = item.EnhancementGrowthPercent,
            Attributes = item.Attributes.Select(a => new HrkPlayerInventoryAttribute
            {
                AttributeTypeId = a.AttributeTypeId, BaseRolledValue = a.BaseRolledValue, CurrentValue = a.CurrentValue
            }).ToList()
        };
        await InventoryAsync(catalog, new[] { copy }, ct);
        return copy;
    }

    public static async Task<List<HeroBonusAttributeDto>> HeroBonusesAsync(IUnitOfWork store, IGameTemplateCatalog catalog, long heroId, CancellationToken ct)
    {
        var bonuses = await store.ReadOnlyRepository<HrkPlayerHeroBonusAttribute>().Query()
            .Where(b => b.PlayerHeroId == heroId).OrderBy(b => b.UnlockedAtStar).ToListAsync(ct);
        var attributes = (await catalog.GetAllAsync<HrkAttributeType>(ct)).ToDictionary(x => x.Id);
        return bonuses.Where(b => attributes.ContainsKey(b.AttributeTypeId)).Select(b => new HeroBonusAttributeDto
        {
            UnlockedAtStar = b.UnlockedAtStar, Code = attributes[b.AttributeTypeId].Code,
            Name = attributes[b.AttributeTypeId].Name, Value = b.Value, IsPercentage = b.IsPercentage
        }).ToList();
    }

    public static async Task HeroesAsync(IGameTemplateCatalog catalog, IEnumerable<HrkPlayerHero> heroes, CancellationToken ct)
    {
        var templates = (await catalog.GetAllAsync<HrkHeroTemplate>(ct)).ToDictionary(x => x.Id);
        foreach (var hero in heroes) hero.HeroTemplate = templates[hero.HeroTemplateId];
    }

    public static async Task InventoryAsync(IGameTemplateCatalog catalog, IEnumerable<HrkPlayerInventory> items, CancellationToken ct)
    {
        var templates = (await catalog.GetAllAsync<HrkItemTemplate>(ct)).ToDictionary(x => x.Id);
        var attributes = (await catalog.GetAllAsync<HrkAttributeType>(ct)).ToDictionary(x => x.Id);
        foreach (var item in items)
        {
            item.ItemTemplate = templates[item.ItemTemplateId];
            foreach (var attribute in item.Attributes) attribute.AttributeType = attributes[attribute.AttributeTypeId];
        }
    }

    public static async Task<HrkPlayerFormation?> FindPlayerFormationAsync(IUnitOfWork store, IGameTemplateCatalog catalog,
        long playerId, string? code, CancellationToken ct)
    {
        var templates = (await catalog.GetAllAsync<HrkFormationTemplate>(ct)).ToDictionary(x => x.Id);
        var query = store.ReadOnlyRepository<HrkPlayerFormation>().Query().Where(f => f.PlayerId == playerId && f.IsActive);
        if (string.IsNullOrWhiteSpace(code)) query = query.Where(f => f.IsSelected);
        else
        {
            var ids = templates.Values.Where(t => t.Code == code).Select(t => t.Id).ToArray();
            query = query.Where(f => ids.Contains(f.FormationTemplateId));
        }
        var formation = await query.FirstOrDefaultAsync(ct);
        if (formation != null) formation.FormationTemplate = templates[formation.FormationTemplateId];
        return formation;
    }
}
