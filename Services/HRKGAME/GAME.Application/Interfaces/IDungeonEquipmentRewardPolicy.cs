using GAME.Domain.Entities;

namespace GAME.Application.Interfaces;

public interface IDungeonEquipmentRewardPolicy
{
    bool CanReward(HrkDungeonMap map, HrkItemTemplate itemTemplate);
}
