using System;
using System.Collections.Generic;
using System.Linq;

namespace GAME.Domain.ValueObjects
{
    public sealed record StoneMaterialItem(
        long InventoryItemId,
        int ItemTemplateId,
        string Code,
        string Name,
        int Quantity,
        decimal SuccessRateBonus);

    public sealed record CharmMaterialItem(
        long InventoryItemId,
        int ItemTemplateId,
        string Code,
        string Name,
        decimal SuccessRateBonus,
        bool PreventLevelDrop);

    /// <summary>
    /// Value Object đại diện cho tập hợp nguyên liệu (đá cường hóa và bùa hộ mệnh/may mắn) được đưa vào phiên rèn.
    /// </summary>
    public sealed class EnhancementMaterials
    {
        public IReadOnlyList<StoneMaterialItem> Stones { get; }
        public CharmMaterialItem? Charm { get; }

        public decimal TotalStoneSuccessBonus { get; }
        public decimal CharmSuccessBonus => Charm?.SuccessRateBonus ?? 0m;
        public bool HasProtectionCharm => Charm?.PreventLevelDrop ?? false;
        public decimal TotalBonus => TotalStoneSuccessBonus + CharmSuccessBonus;

        public EnhancementMaterials(
            IEnumerable<StoneMaterialItem>? stones,
            CharmMaterialItem? charm,
            int maxStoneSlots = 3)
        {
            var stoneList = stones?.ToList() ?? new List<StoneMaterialItem>();
            int totalStones = stoneList.Sum(s => s.Quantity);

            if (totalStones > maxStoneSlots)
            {
                throw new InvalidOperationException($"Số lượng đá cường hóa vượt quá giới hạn cho phép ({totalStones}/{maxStoneSlots}).");
            }

            Stones = stoneList.AsReadOnly();
            Charm = charm;

            TotalStoneSuccessBonus = stoneList.Sum(s => s.SuccessRateBonus * s.Quantity);
        }

        public static EnhancementMaterials Empty => new(null, null, 3);
    }
}
