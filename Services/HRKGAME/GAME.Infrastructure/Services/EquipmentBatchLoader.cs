using Core.Common.Repositories;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GAME.Infrastructure.Services
{
    /// <summary>
    /// Loads equipped inventory instances in one batch instead of repeating the
    /// inventory/template attribute graph for every equipment slot.
    /// </summary>
    internal static class EquipmentBatchLoader
    {
        public static async Task<Dictionary<long, HrkPlayerEquipment>> LoadForHeroesAsync(
            IUnitOfWork unitOfWork,
            long playerId,
            IReadOnlyCollection<long> heroIds,
            CancellationToken cancellationToken)
        {
            if (heroIds.Count == 0)
                return new Dictionary<long, HrkPlayerEquipment>();

            // Deliberately load only the slot rows first. Six Include branches here
            // generate many repeated joins/split queries and were the main timeout source.
            var equipments = await unitOfWork.ReadOnlyRepository<HrkPlayerEquipment>().Query()
                .Where(e => e.PlayerId == playerId && heroIds.Contains(e.HeroId))
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var inventoryIds = equipments
                .SelectMany(GetInventoryIds)
                .Distinct()
                .ToList();

            if (inventoryIds.Count == 0)
                return equipments.ToDictionary(e => e.HeroId);

            // Every inventory instance is materialized once. AsSplitQuery keeps the two
            // attribute collections from producing a cartesian result set.
            var inventoryItems = await unitOfWork.ReadOnlyRepository<HrkPlayerInventory>().Query()
                .Where(i => i.PlayerId == playerId && inventoryIds.Contains(i.Id) && i.IsActive)
                .Include(i => i.Attributes).ThenInclude(a => a.AttributeType)
                .Include(i => i.ItemTemplate).ThenInclude(t => t.Rarity)
                .Include(i => i.ItemTemplate).ThenInclude(t => t.Category)
                .Include(i => i.ItemTemplate).ThenInclude(t => t.Attributes).ThenInclude(a => a.AttributeType)
                .AsSplitQuery()
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var inventoryById = inventoryItems.ToDictionary(i => i.Id);
            foreach (var equipment in equipments)
            {
                equipment.Weapon = Resolve(equipment.WeaponId, inventoryById);
                equipment.Armor = Resolve(equipment.ArmorId, inventoryById);
                equipment.Helmet = Resolve(equipment.HelmetId, inventoryById);
                equipment.Boots = Resolve(equipment.BootsId, inventoryById);
                equipment.Ring = Resolve(equipment.RingId, inventoryById);
                equipment.Artifact = Resolve(equipment.ArtifactId, inventoryById);
            }

            return equipments.ToDictionary(e => e.HeroId);
        }

        private static IEnumerable<long> GetInventoryIds(HrkPlayerEquipment equipment)
        {
            if (equipment.WeaponId.HasValue) yield return equipment.WeaponId.Value;
            if (equipment.ArmorId.HasValue) yield return equipment.ArmorId.Value;
            if (equipment.HelmetId.HasValue) yield return equipment.HelmetId.Value;
            if (equipment.BootsId.HasValue) yield return equipment.BootsId.Value;
            if (equipment.RingId.HasValue) yield return equipment.RingId.Value;
            if (equipment.ArtifactId.HasValue) yield return equipment.ArtifactId.Value;
        }

        private static HrkPlayerInventory? Resolve(
            long? inventoryId,
            IReadOnlyDictionary<long, HrkPlayerInventory> inventoryById)
            => inventoryId.HasValue && inventoryById.TryGetValue(inventoryId.Value, out var item)
                ? item
                : null;
    }
}
