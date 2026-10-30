using GAME.Application.DTOs;
using GAME.Domain.Entities;
using System.Collections.Generic;

namespace GAME.Application.Interfaces
{

    public interface IHeroStatCalculationService
    {
        HeroStatCalculationResult CalculateStats(HrkPlayerHero? ph, HrkPlayerEquipment? equipment);
        HeroStatCalculationResult CalculateStats(HrkPlayerHero? ph, IEnumerable<HrkPlayerInventory>? equippedItems);
    }
}
