using GAME.Application.DTOs;
using GAME.Domain.Entities;
using System.Collections.Generic;

namespace GAME.Application.Interfaces
{
    public class HeroStatCalculationResult
    {
        public CalculatedStatsDto FinalStats { get; set; } = new();
        public List<HeroStatBreakdownDto> Breakdowns { get; set; } = new();
    }

    public interface IHeroStatCalculationService
    {
        HeroStatCalculationResult CalculateStats(HrkPlayerHero? ph, HrkPlayerEquipment? equipment);
        HeroStatCalculationResult CalculateStats(HrkPlayerHero? ph, IEnumerable<HrkPlayerInventory>? equippedItems);
    }
}
