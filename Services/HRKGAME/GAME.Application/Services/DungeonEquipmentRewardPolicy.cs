using GAME.Application.Interfaces;
using GAME.Domain.Entities;

namespace GAME.Application.Services;

public sealed class DungeonEquipmentRewardPolicy : IDungeonEquipmentRewardPolicy
{
    public bool CanReward(HrkDungeonMap map, HrkItemTemplate itemTemplate)
    {
        if (map.MaxEquipmentRarity == null || itemTemplate.Rarity == null)
        {
            return false;
        }

        return itemTemplate.Rarity.DisplayOrder <= map.MaxEquipmentRarity.DisplayOrder;
    }
}
